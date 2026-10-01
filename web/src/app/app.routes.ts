import { Routes } from '@angular/router';

// loadComponent: cada página só é baixada quando a rota é visitada (lazy loading).
export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/board/home-page/home-page').then((m) => m.HomePage) },
  {
    path: 'categories/:categoryId',
    loadComponent: () => import('./features/board/board-page/board-page').then((m) => m.BoardPage),
  },
  { path: '**', redirectTo: '' },
];
