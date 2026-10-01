import { CdkDrag, CdkDragEnd } from '@angular/cdk/drag-drop';
import { Component, ElementRef, computed, effect, inject, input, signal, untracked, viewChild } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { Router, RouterLink } from '@angular/router';
import { Card, Position } from '../../../core/data/models';
import { Notifier } from '../../../core/feedback/notifier';
import { confirmAction } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { CategoriesStore } from '../../categories/categories-store';
import { BoardStore } from '../board-store';
import { CardKeyAction, CardView } from '../card-view/card-view';

const NEW_CARD_TITLE = 'Novo cartão';
/** Metade do tamanho de um cartão novo, para centralizá-lo no ponto escolhido. */
const CARD_HALF = { width: 104, height: 32 };

/**
 * Um quadro: o de uma categoria (/categories/:categoryId) ou o de um cartão (/cards/:cardId). Interação no
 * esquema "explorador de arquivos": dois cliques no vazio criam, dois cliques num cartão abrem, botão direito
 * abre o menu, F2 renomeia, Delete apaga.
 */
@Component({
  selector: 'app-board-page',
  imports: [CdkDrag, CardView, MatButton, MatIcon, MatMenu, MatMenuItem, MatMenuTrigger, RouterLink],
  providers: [BoardStore],
  templateUrl: './board-page.html',
  styleUrl: './board-page.scss',
})
export class BoardPage {
  protected readonly store = inject(BoardStore);
  protected readonly categories = inject(CategoriesStore);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  /** Vêm da URL: um dos dois está presente. */
  readonly categoryId = input<string>();
  readonly cardId = input<string>();

  protected readonly selectedId = signal<string | null>(null);
  protected readonly editingId = signal<string | null>(null);
  protected readonly menuCard = signal<Card | null>(null);
  protected readonly menuPosition = signal<Position>({ x: 0, y: 0 });

  protected readonly category = computed(() =>
    this.categories.categories().find((c) => c.id === this.store.categoryId()),
  );

  private readonly viewport = viewChild.required<ElementRef<HTMLElement>>('viewport');
  private readonly menuTrigger = viewChild.required<MatMenuTrigger>('menuTrigger');
  private readonly colorInput = viewChild.required<ElementRef<HTMLInputElement>>('colorInput');
  private readonly dateInput = viewChild.required<ElementRef<HTMLInputElement>>('dateInput');

  constructor() {
    // Abre o quadro sempre que a URL muda.
    effect(() => {
      const categoryId = this.categoryId();
      const cardId = this.cardId();
      untracked(() => this.openBoard(categoryId, cardId));
    });
    // A aba da categoria fica destacada também dentro dos quadros dos cartões.
    effect(() => this.categories.activeCategoryId.set(this.store.categoryId()));
  }

  // ---- Criar -----------------------------------------------------------------------------------------------

  /** Botão "+ Cartão": cria no centro da área visível. */
  protected async createAtCenter(): Promise<void> {
    const viewport = this.viewport().nativeElement;
    await this.create({
      x: viewport.scrollLeft + viewport.clientWidth / 2,
      y: viewport.scrollTop + viewport.clientHeight / 2,
    });
  }

  /** Dois cliques num espaço vazio: cria exatamente ali. */
  protected async createAtPointer(event: MouseEvent): Promise<void> {
    const canvas = (event.currentTarget as HTMLElement).getBoundingClientRect();
    await this.create({ x: event.clientX - canvas.left, y: event.clientY - canvas.top });
  }

  private async create(center: Position): Promise<void> {
    const position = { x: Math.max(0, center.x - CARD_HALF.width), y: Math.max(0, center.y - CARD_HALF.height) };
    try {
      const card = await this.store.create(NEW_CARD_TITLE, position);
      this.selectedId.set(card.id);
      this.editingId.set(card.id);
    } catch (error) {
      this.notifier.error(error);
    }
  }

  // ---- Interações com um cartão ----------------------------------------------------------------------------

  protected async onKeyAction(card: Card, action: CardKeyAction): Promise<void> {
    switch (action) {
      case 'open':
        await this.router.navigate(['/cards', card.id]);
        break;
      case 'rename':
        this.editingId.set(card.id);
        break;
      case 'delete':
        await this.delete(card);
        break;
    }
  }

  protected async onRenamed(card: Card, title: string): Promise<void> {
    this.editingId.set(null);
    if (title.trim() === card.title) {
      return;
    }
    await this.run(() => this.store.rename(card.id, title));
  }

  protected cancelRename(): void {
    this.editingId.set(null);
  }

  protected async onDragEnded(card: Card, event: CdkDragEnd): Promise<void> {
    const position = event.source.getFreeDragPosition();
    if (position.x !== card.position.x || position.y !== card.position.y) {
      await this.run(() => this.store.move(card.id, position));
    }
  }

  // ---- Menu do botão direito -------------------------------------------------------------------------------

  protected openMenu(card: Card, event: MouseEvent): void {
    this.selectedId.set(card.id);
    this.menuCard.set(card);
    this.menuPosition.set({ x: event.clientX, y: event.clientY });
    this.menuTrigger().openMenu();
  }

  /** Cor livre: o seletor de cores nativo do navegador. */
  protected pickColor(card: Card): void {
    const input = this.colorInput().nativeElement;
    input.value = card.color;
    input.showPicker();
  }

  protected async onColorPicked(value: string): Promise<void> {
    const card = this.menuCard();
    if (card && value !== card.color) {
      await this.run(() => this.store.changeColor(card.id, value));
    }
  }

  /** Prazo: o seletor de datas nativo, que já devolve YYYY-MM-DD. */
  protected pickDueDate(card: Card): void {
    const input = this.dateInput().nativeElement;
    input.value = card.properties.dueOn ?? '';
    input.showPicker();
  }

  protected async onDueDatePicked(value: string): Promise<void> {
    const card = this.menuCard();
    if (card && value) {
      await this.run(() => this.store.setProperties(card.id, { ...card.properties, dueOn: value }));
    }
  }

  protected async removeDueDate(card: Card): Promise<void> {
    await this.run(() => this.store.setProperties(card.id, { ...card.properties, dueOn: undefined }));
  }

  protected async toggleDone(card: Card): Promise<void> {
    const done = card.properties.done === true ? undefined : true;
    await this.run(() => this.store.setProperties(card.id, { ...card.properties, done }));
  }

  /** Apagar pede confirmação só quando o cartão tem outros dentro. */
  protected async delete(card: Card): Promise<void> {
    if (card.childCount > 0) {
      const confirmed = await confirmAction(this.dialog, {
        title: `Apagar "${card.title}"?`,
        message: `Os ${card.childCount} cartões dentro dele (e o que houver dentro deles) serão apagados junto.`,
        confirmLabel: 'Apagar',
      });
      if (!confirmed) {
        return;
      }
    }
    await this.run(() => this.store.delete(card.id));
    this.selectedId.set(null);
  }

  // ---- Apoio -----------------------------------------------------------------------------------------------

  private async openBoard(categoryId: string | undefined, cardId: string | undefined): Promise<void> {
    this.selectedId.set(null);
    this.editingId.set(null);
    try {
      if (cardId) {
        await this.store.openCard(cardId);
      } else if (categoryId) {
        await this.store.openCategory(categoryId);
      }
    } catch {
      // O status 'not-found' do store já mostra a mensagem na tela.
    }
  }

  private async run(action: () => Promise<void>): Promise<void> {
    try {
      await action();
    } catch (error) {
      this.notifier.error(error);
    }
  }
}
