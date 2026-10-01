import { Component, effect, inject } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { Router } from '@angular/router';
import { CategoriesStore } from '../../categories/categories-store';

/** Página inicial: abre a primeira categoria, ou explica como criar uma quando ainda não há nenhuma. */
@Component({
  selector: 'app-home-page',
  imports: [MatIcon],
  template: `
    @if (store.isLoaded() && store.categories().length === 0) {
      <section class="empty">
        <mat-icon aria-hidden="true">folder_open</mat-icon>
        <h1>Nenhuma categoria ainda</h1>
        <p>Use o botão <strong>+</strong> no rodapé para criar a primeira.</p>
      </section>
    }
  `,
  styleUrl: './home-page.scss',
})
export class HomePage {
  protected readonly store = inject(CategoriesStore);
  private readonly router = inject(Router);

  constructor() {
    effect(() => {
      const first = this.store.categories()[0];
      if (first) {
        void this.router.navigate(['/categories', first.id], { replaceUrl: true });
      }
    });
  }
}
