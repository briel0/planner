import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Api } from '../../api/api';
import { createChildCard } from '../../api/fn/cards/create-child-card';
import { createRootCard } from '../../api/fn/cards/create-root-card';
import { deleteCard } from '../../api/fn/cards/delete-card';
import { getCard } from '../../api/fn/cards/get-card';
import { listChildCards } from '../../api/fn/cards/list-child-cards';
import { listRootCards } from '../../api/fn/cards/list-root-cards';
import { updateCard } from '../../api/fn/cards/update-card';
import { createCategory } from '../../api/fn/categories/create-category';
import { deleteCategory } from '../../api/fn/categories/delete-category';
import { listCategories } from '../../api/fn/categories/list-categories';
import { updateCategory } from '../../api/fn/categories/update-category';
import { CardResponse } from '../../api/models/card-response';
import { ContentDoc } from '../content/card-content';
import { Card, CardChanges, CardDetails, CardLocation, Category, Position } from './models';
import { PlannerData, PlannerDataError } from './planner-data';

/** Os dados vêm da API, pelo cliente gerado do OpenAPI (src/app/api, nunca editado à mão). */
@Injectable()
export class HttpPlannerData extends PlannerData {
  private readonly api = inject(Api);

  // ---- Categorias ----------------------------------------------------------------------------------------------

  listCategories(): Promise<Category[]> {
    return call(() => this.api.invoke(listCategories));
  }

  createCategory(id: string, name: string): Promise<Category> {
    return call(() => this.api.invoke(createCategory, { body: { id, name } }));
  }

  updateCategory(id: string, changes: { name?: string; sortOrder?: number }, ifMatch?: string): Promise<Category> {
    return call(() => this.api.invoke(updateCategory, { id, body: changes, 'If-Match': quoted(ifMatch) }));
  }

  deleteCategory(id: string): Promise<void> {
    return call(() => this.api.invoke(deleteCategory, { id }));
  }

  // ---- Cartões -------------------------------------------------------------------------------------------------

  async listRootCards(categoryId: string): Promise<Card[]> {
    return (await call(() => this.api.invoke(listRootCards, { categoryId }))).map(toCard);
  }

  async listChildren(cardId: string): Promise<Card[]> {
    return (await call(() => this.api.invoke(listChildCards, { id: cardId }))).map(toCard);
  }

  async getCard(id: string): Promise<CardDetails> {
    const card = await call(() => this.api.invoke(getCard, { id }));
    return { ...toCard(card), ancestors: card.ancestors ?? [] };
  }

  async createCard(location: CardLocation, input: { id: string; title: string; position: Position }): Promise<Card> {
    const created = await call(() =>
      'parentId' in location
        ? this.api.invoke(createChildCard, { id: location.parentId, body: input })
        : this.api.invoke(createRootCard, { categoryId: location.categoryId, body: input }),
    );
    return toCard(created);
  }

  async updateCard(id: string, changes: CardChanges, ifMatch?: string): Promise<Card> {
    return toCard(await call(() => this.api.invoke(updateCard, { id, body: changes, 'If-Match': quoted(ifMatch) })));
  }

  deleteCard(id: string): Promise<void> {
    return call(() => this.api.invoke(deleteCard, { id }));
  }
}

/** Do formato da API para o modelo do app (propriedades nulas viram ausentes; o conteúdo é um documento do Tiptap). */
function toCard(card: CardResponse): Card {
  return {
    id: card.id,
    categoryId: card.categoryId,
    parentId: card.parentId,
    title: card.title,
    color: card.color,
    position: card.position,
    size: card.size,
    layer: card.layer,
    properties: {
      ...(card.properties.dueOn ? { dueOn: card.properties.dueOn } : {}),
      ...(card.properties.done != null ? { done: card.properties.done } : {}),
    },
    content: (card.content as ContentDoc | null) ?? null,
    childCount: card.childCount,
    createdAt: card.createdAt,
    updatedAt: card.updatedAt,
    version: card.version,
  };
}

/** O If-Match leva a versão entre aspas, como manda o HTTP. */
function quoted(version: string | undefined): string | undefined {
  return version === undefined ? undefined : `"${version}"`;
}

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
