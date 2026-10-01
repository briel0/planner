import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { MatTooltip } from '@angular/material/tooltip';
import { Category } from '../../../core/data/models';
import { Notifier } from '../../../core/feedback/notifier';
import { confirmAction } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { CategoriesStore } from '../categories-store';

const NEW_CATEGORY_NAME = 'Nova categoria';

/**
 * As abas do rodapé, como as planilhas do Excel: "+" cria, dois cliques renomeiam, botão direito abre o menu,
 * arrastar reordena.
 */
@Component({
  selector: 'app-category-tabs',
  imports: [CdkDropList, CdkDrag, RouterLink, MatIconButton, MatIcon, MatMenu, MatMenuItem, MatMenuTrigger, MatTooltip],
  templateUrl: './category-tabs.html',
  styleUrl: './category-tabs.scss',
})
export class CategoryTabs {
  protected readonly store = inject(CategoriesStore);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  /** Categoria cujo nome está sendo editado (no lugar da aba aparece um campo de texto). */
  protected readonly editingId = signal<string | null>(null);
  /** Categoria do menu de contexto aberto, e a posição do clique (o menu abre onde o mouse está). */
  protected readonly menuCategory = signal<Category | null>(null);
  protected readonly menuPosition = signal({ x: 0, y: 0 });

  private readonly menuTrigger = viewChild.required<MatMenuTrigger>('menuTrigger');
  private readonly nameInput = viewChild<ElementRef<HTMLInputElement>>('nameInput');

  constructor() {
    // Quando o campo de edição aparece, ele já vem com o foco e o texto selecionado.
    effect(() => {
      const input = this.nameInput()?.nativeElement;
      input?.focus();
      input?.select();
    });
  }

  protected async createCategory(): Promise<void> {
    try {
      const created = await this.store.create(this.uniqueNewName());
      await this.router.navigate(['/categories', created.id]);
      this.editingId.set(created.id);
    } catch (error) {
      this.notifier.error(error);
    }
  }

  protected startRename(category: Category): void {
    this.editingId.set(category.id);
  }

  protected cancelRename(): void {
    this.editingId.set(null);
  }

  protected async commitRename(category: Category, name: string): Promise<void> {
    if (this.editingId() !== category.id) {
      return; // já confirmado (ex.: Enter seguido do blur do campo)
    }
    this.editingId.set(null);
    if (name.trim() === category.name) {
      return;
    }
    try {
      await this.store.rename(category.id, name);
    } catch (error) {
      this.notifier.error(error);
    }
  }

  protected openMenu(event: MouseEvent, category: Category): void {
    event.preventDefault();
    this.menuCategory.set(category);
    this.menuPosition.set({ x: event.clientX, y: event.clientY });
    this.menuTrigger().openMenu();
  }

  protected async onDrop(event: CdkDragDrop<Category[]>): Promise<void> {
    const ids = this.store.categories().map((c) => c.id);
    moveItemInArray(ids, event.previousIndex, event.currentIndex);
    try {
      await this.store.reorder(ids);
    } catch (error) {
      this.notifier.error(error);
    }
  }

  protected async confirmDelete(category: Category): Promise<void> {
    const confirmed = await confirmAction(this.dialog, {
      title: `Apagar "${category.name}"?`,
      message: 'Todos os cartões desta categoria serão apagados junto. Não dá para desfazer.',
      confirmLabel: 'Apagar',
    });
    if (!confirmed) {
      return;
    }
    try {
      await this.store.delete(category.id);
      if (this.store.activeCategoryId() === category.id) {
        await this.router.navigate(['/']);
      }
    } catch (error) {
      this.notifier.error(error);
    }
  }

  /** "Nova categoria", ou "Nova categoria 2", "3"... se o nome já existir. */
  private uniqueNewName(): string {
    const taken = new Set(this.store.categories().map((c) => c.name.toLowerCase()));
    let name = NEW_CATEGORY_NAME;
    for (let n = 2; taken.has(name.toLowerCase()); n++) {
      name = `${NEW_CATEGORY_NAME} ${n}`;
    }
    return name;
  }
}
