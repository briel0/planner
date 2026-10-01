import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Api } from '../../api/api';
import { createCategory } from '../../api/fn/categories/create-category';
import { deleteCategory } from '../../api/fn/categories/delete-category';
import { listCategories } from '../../api/fn/categories/list-categories';
import { updateCategory } from '../../api/fn/categories/update-category';
import { FAKE_PLANNER_DATA_OPTIONS, FakePlannerData } from './fake-planner-data';
import { Card, CardChanges, CardDetails, CardLocation, Category, Position } from './models';
import { PlannerData, PlannerDataError } from './planner-data';

/**
 * Fase 2: os dados vêm da API, pelo cliente gerado do OpenAPI (src/app/api, nunca editado à mão).
 * Por enquanto só as categorias estão ligadas; os cartões continuam falsos, em memória, até ganharem endpoints.
 */
@Injectable()
export class HttpPlannerData extends PlannerData {
  private readonly api = inject(Api);
  private readonly fakeCards = inject(FakePlannerData);

  // ---- Categorias: API ----------------------------------------------------------------------------------------

  listCategories(): Promise<Category[]> {
    return call(() => this.api.invoke(listCategories));
  }

  createCategory(id: string, name: string): Promise<Category> {
    return call(() => this.api.invoke(createCategory, { body: { id, name } }));
  }

  updateCategory(id: string, changes: { name?: string; sortOrder?: number }, ifMatch?: string): Promise<Category> {
    return call(() =>
      this.api.invoke(updateCategory, {
        id,
        body: changes,
        'If-Match': ifMatch === undefined ? undefined : `"${ifMatch}"`,
      }),
    );
  }

  deleteCategory(id: string): Promise<void> {
    return call(() => this.api.invoke(deleteCategory, { id }));
  }

  // ---- Cartões: ainda falsos -----------------------------------------------------------------------------------

  listRootCards(categoryId: string): Promise<Card[]> {
    return this.fakeCards.listRootCards(categoryId);
  }

  listChildren(cardId: string): Promise<Card[]> {
    return this.fakeCards.listChildren(cardId);
  }

  getCard(id: string): Promise<CardDetails> {
    return this.fakeCards.getCard(id);
  }

  createCard(location: CardLocation, input: { title: string; position: Position }): Promise<Card> {
    return this.fakeCards.createCard(location, input);
  }

  updateCard(id: string, changes: CardChanges): Promise<Card> {
    return this.fakeCards.updateCard(id, changes);
  }

  deleteCard(id: string): Promise<void> {
    return this.fakeCards.deleteCard(id);
  }
}

/** Os cartões falsos usados pelo HttpPlannerData: sem dados de exemplo, aceitando as categorias reais da API. */
export const FAKE_CARDS_FOR_HTTP = [
  FakePlannerData,
  { provide: FAKE_PLANNER_DATA_OPTIONS, useValue: { seed: false, categoriesElsewhere: true } },
];

/**
 * Traduz os erros HTTP para PlannerDataError: o `code` do Problem Details (docs/api-design.md) é o que o resto do
 * app usa para decidir o que fazer.
 */
export async function call<T>(request: () => Promise<T>): Promise<T> {
  try {
    return await request();
  } catch (error) {
    if (error instanceof HttpErrorResponse) {
      const problem = typeof error.error === 'object' && error.error !== null ? error.error : {};
      throw new PlannerDataError(
        typeof problem.code === 'string' ? problem.code : error.status === 0 ? 'network' : 'unexpected',
        error.status,
        typeof problem.title === 'string' ? problem.title : error.message,
      );
    }
    throw error;
  }
}
