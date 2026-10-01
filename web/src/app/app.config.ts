import { ApplicationConfig, inject, provideAppInitializer, provideBrowserGlobalErrorListeners } from '@angular/core';
import { MatIconRegistry } from '@angular/material/icon';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { routes } from './app.routes';
import { FakePlannerData } from './core/data/fake-planner-data';
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
    // Fase 1 do MVP: dados falsos em memória. Na fase 2, trocar por HttpPlannerData.
    { provide: PlannerData, useClass: FakePlannerData },
  ],
};
