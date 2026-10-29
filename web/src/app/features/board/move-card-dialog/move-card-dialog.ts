import { Component, computed, inject, signal } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { Card, CardLocation, Position } from '../../../core/data/models';
import { PlannerData } from '../../../core/data/planner-data';
import { Notifier } from '../../../core/feedback/notifier';
import { CategoriesStore } from '../../categories/categories-store';
import { sameLocation } from '../board-store';
import { positionBelow } from '../placement';

export interface MoveCardDialogData {
  card: Card;
}

/** O destino escolhido, já com a posição livre no quadro de lá. */
export interface MoveCardResult {
  location: CardLocation;
  position: Position;
}

/** Um nível da navegação: a lista de categorias (`location` nulo) ou o quadro de uma categoria ou cartão. */
interface Level {
  location: CardLocation | null;
  title: string;
}

interface Option {
  id: string;
  title: string;
  icon: string;
  location: CardLocation;
}

/**
 * Escolhe para onde mover um cartão, navegando como num explorador de arquivos: categorias, depois os cartões de
 * cada quadro. O próprio cartão não aparece na lista, então não dá para movê-lo para dentro dele mesmo.
 */
@Component({
  selector: 'app-move-card-dialog',
  imports: [MatDialogModule, MatButton, MatIcon],
  template: `
    <h2 mat-dialog-title>Mover "{{ data.card.title }}"</h2>
    <mat-dialog-content>
      <nav class="path" aria-label="Destino">
        @for (level of path(); track $index; let last = $last) {
          @if (last) {
            <span class="crumb current" aria-current="location">{{ level.title }}</span>
          } @else {
            <button type="button" class="crumb" (click)="goBackTo($index)">{{ level.title }}</button>
            <mat-icon class="separator" aria-hidden="true">chevron_right</mat-icon>
          }
        }
      </nav>
      @if (loading()) {
        <p class="hint">Carregando…</p>
      } @else {
        <ul class="options">
          @for (option of options(); track option.id) {
            <li>
              <button type="button" class="option" (click)="enter(option)">
                <mat-icon aria-hidden="true">{{ option.icon }}</mat-icon>
                <span class="option-title">{{ option.title }}</span>
                <mat-icon class="chevron" aria-hidden="true">chevron_right</mat-icon>
              </button>
            </li>
          } @empty {
            <li class="hint">{{ here() ? 'Nenhum cartão neste quadro.' : 'Nenhuma categoria.' }}</li>
          }
        </ul>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button matButton mat-dialog-close>Cancelar</button>
      <button matButton="filled" [disabled]="!canMoveHere()" (click)="moveHere()">
        {{ here() ? (isCurrentBoard() ? 'Já está aqui' : 'Mover para cá') : 'Escolha um destino' }}
      </button>
    </mat-dialog-actions>
  `,
  styles: `
    .path {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.125rem;
      margin-bottom: 0.5rem;
    }
    .crumb {
      border: none;
      background: none;
      padding: 0.125rem 0.25rem;
      font: inherit;
      color: var(--mat-sys-primary);
      cursor: pointer;
    }
    .crumb.current {
      color: inherit;
      font-weight: 600;
      cursor: default;
    }
    .separator {
      font-size: 1.125rem;
      width: 1.125rem;
      height: 1.125rem;
      opacity: 0.6;
    }
    .options {
      list-style: none;
      margin: 0;
      padding: 0;
      max-height: 18rem;
      overflow-y: auto;
    }
    .option {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      width: 100%;
      padding: 0.5rem;
      border: none;
      background: none;
      font: inherit;
      text-align: left;
      cursor: pointer;
    }
    .option:hover,
    .option:focus-visible {
      background: var(--mat-sys-surface-container-high);
    }
    .option-title {
      flex: 1;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .chevron {
      opacity: 0.6;
    }
    .hint {
      padding: 0.5rem;
      opacity: 0.7;
    }
  `,
})
export class MoveCardDialog {
  protected readonly data = inject<MoveCardDialogData>(MAT_DIALOG_DATA);
  private readonly ref = inject<MatDialogRef<MoveCardDialog, MoveCardResult>>(MatDialogRef);
  private readonly planner = inject(PlannerData);
  private readonly categories = inject(CategoriesStore);
  private readonly notifier = inject(Notifier);

  protected readonly path = signal<Level[]>([{ location: null, title: 'Categorias' }]);
  protected readonly loading = signal(false);
  /** Cartões do quadro que está sendo visto (sem o cartão que está sendo movido). */
  private readonly levelCards = signal<Card[]>([]);

  /** O quadro que está sendo visto; nulo na lista de categorias. */
  protected readonly here = computed(() => this.path().at(-1)!.location);
  protected readonly isCurrentBoard = computed(() => sameLocation(this.here(), this.cardLocation()));
  protected readonly canMoveHere = computed(() => this.here() !== null && !this.isCurrentBoard() && !this.loading());

  protected readonly options = computed<Option[]>(() =>
    this.here() === null
      ? this.categories.categories().map((c) => ({
          id: c.id,
          title: c.name,
          icon: 'folder',
          location: { categoryId: c.id },
        }))
      : this.levelCards().map((c) => ({
          id: c.id,
          title: c.title,
          icon: c.childCount > 0 ? 'folder_open' : 'sticky_note_2',
          location: { parentId: c.id },
        })),
  );

  protected async enter(option: Option): Promise<void> {
    if (await this.load(option.location)) {
      this.path.update((path) => [...path, { location: option.location, title: option.title }]);
    }
  }

  protected async goBackTo(index: number): Promise<void> {
    const level = this.path()[index];
    if (level.location === null || (await this.load(level.location))) {
      this.path.update((path) => path.slice(0, index + 1));
    }
  }

  protected moveHere(): void {
    const location = this.here();
    if (location && this.canMoveHere()) {
      this.ref.close({ location, position: positionBelow(this.levelCards()) });
    }
  }

  private cardLocation(): CardLocation {
    const card = this.data.card;
    return card.parentId ? { parentId: card.parentId } : { categoryId: card.categoryId };
  }

  /** Busca os cartões de um quadro; devolve `false` (e avisa) se não deu. */
  private async load(location: CardLocation): Promise<boolean> {
    this.loading.set(true);
    try {
      const cards =
        'parentId' in location
          ? await this.planner.listChildren(location.parentId)
          : await this.planner.listRootCards(location.categoryId);
      this.levelCards.set(cards.filter((c) => c.id !== this.data.card.id));
      return true;
    } catch (error) {
      this.notifier.error(error);
      return false;
    } finally {
      this.loading.set(false);
    }
  }
}
