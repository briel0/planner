import { Component, ElementRef, computed, effect, input, output, viewChild } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { Card } from '../../../core/data/models';
import { readableTextColor } from '../../../shared/ui/color/readable-text-color';

/** Uma ação de teclado pedida sobre o cartão (esquema "explorador de arquivos"). */
export type CardKeyAction = 'open' | 'rename' | 'delete';

/** Um cartão no canvas. Só apresenta e avisa; quem decide o que fazer é o quadro. */
@Component({
  selector: 'app-card-view',
  imports: [MatIcon],
  templateUrl: './card-view.html',
  styleUrl: './card-view.scss',
  host: {
    role: 'button',
    tabindex: '0',
    '[attr.aria-label]': 'card().title',
    '[attr.aria-selected]': 'selected()',
    '[class.selected]': 'selected()',
    '[class.done]': 'card().properties.done === true',
    '[class.light-text]': "textColor() === 'light'",
    '[style.background-color]': 'card().color',
    '(click)': 'onClick($event)',
    '(dblclick)': 'onDoubleClick($event)',
    '(contextmenu)': 'onContextMenu($event)',
    '(keydown)': 'onKeydown($event)',
  },
})
export class CardView {
  readonly card = input.required<Card>();
  readonly selected = input(false);
  readonly editing = input(false);

  readonly selectedChange = output<void>();
  readonly keyAction = output<CardKeyAction>();
  readonly menuRequested = output<MouseEvent>();
  readonly renamed = output<string>();
  readonly renameCancelled = output<void>();

  protected readonly textColor = computed(() => readableTextColor(this.card().color));
  protected readonly due = computed(() => describeDue(this.card().properties.dueOn, this.card().properties.done));

  private readonly titleInput = viewChild<ElementRef<HTMLInputElement>>('titleInput');

  constructor() {
    effect(() => {
      const input = this.titleInput()?.nativeElement;
      input?.focus();
      input?.select();
    });
  }

  protected onClick(event: MouseEvent): void {
    event.stopPropagation(); // não deseleciona pelo canvas
    this.selectedChange.emit();
  }

  protected onDoubleClick(event: MouseEvent): void {
    event.stopPropagation(); // não cria um cartão novo pelo canvas
    if (!this.editing()) {
      this.keyAction.emit('open');
    }
  }

  protected onContextMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectedChange.emit();
    this.menuRequested.emit(event);
  }

  protected onKeydown(event: KeyboardEvent): void {
    if (this.editing()) {
      return;
    }
    const action = KEY_ACTIONS[event.key];
    if (action) {
      event.preventDefault();
      this.keyAction.emit(action);
    }
  }

  protected commit(value: string): void {
    if (this.editing()) {
      this.renamed.emit(value);
    }
  }
}

const KEY_ACTIONS: Record<string, CardKeyAction> = { Enter: 'open', F2: 'rename', Delete: 'delete' };

/** "30/09" e se está atrasado (prazo no passado e não concluído). */
function describeDue(dueOn: string | undefined, done: boolean | undefined) {
  if (!dueOn) {
    return null;
  }
  const [year, month, day] = dueOn.split('-').map(Number);
  const label = new Date(year, month - 1, day).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' });
  const today = new Date().toLocaleDateString('sv-SE'); // YYYY-MM-DD local
  return { label, overdue: !done && dueOn < today };
}
