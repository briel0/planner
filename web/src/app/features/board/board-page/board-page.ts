import { Component, computed, inject, input } from '@angular/core';
import { CategoriesStore } from '../../categories/categories-store';

/** O quadro de uma categoria. Provisório: o canvas com os cartões é o próximo passo do MVP. */
@Component({
  selector: 'app-board-page',
  template: `
    @if (category(); as category) {
      <section class="placeholder">
        <h1>{{ category.name }}</h1>
        <p>O quadro desta categoria aparece aqui.</p>
      </section>
    } @else if (store.isLoaded()) {
      <section class="placeholder">
        <h1>Categoria não encontrada</h1>
        <p>Ela pode ter sido apagada.</p>
      </section>
    }
  `,
  styleUrl: './board-page.scss',
})
export class BoardPage {
  protected readonly store = inject(CategoriesStore);

  /** Vem da URL (/categories/:categoryId). */
  readonly categoryId = input.required<string>();

  protected readonly category = computed(() => this.store.categories().find((c) => c.id === this.categoryId()));
}
