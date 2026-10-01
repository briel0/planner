import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch } from '@angular/common/http';
import { MatIconRegistry } from '@angular/material/icon';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { FAKE_CARDS_FOR_HTTP, HttpPlannerData } from './core/data/http-planner-data';
import { PlannerData } from './core/data/planner-data';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    // withComponentInputBinding: parâmetros da URL (ex.: :categoryId) chegam como inputs dos componentes.
    provideRouter(routes, withComponentInputBinding()),
    // <mat-icon> usa os Material Symbols (instalados localmente pelo pacote material-symbols).
    provideAppInitializer(() => {
      inject(MatIconRegistry).setDefaultFontSetClass('material-symbols-outlined');
    }),
    provideHttpClient(withFetch()),
    // Fase 2: dados da API (categorias). Os cartões ainda são falsos, em memória, até ganharem endpoints.
    { provide: PlannerData, useClass: HttpPlannerData },
    ...FAKE_CARDS_FOR_HTTP,
  ],
};
