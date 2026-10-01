import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Notifier } from './core/feedback/notifier';
import { CategoriesStore } from './features/categories/categories-store';
import { CategoryTabs } from './features/categories/category-tabs/category-tabs';

/** O esqueleto do app: o quadro ocupa a tela, e as abas das categorias ficam no rodapé. */
@Component({
  imports: [RouterOutlet, CategoryTabs],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  constructor() {
    const notifier = inject(Notifier);
    inject(CategoriesStore)
      .load()
      .catch((error: unknown) => notifier.error(error));
  }
}
