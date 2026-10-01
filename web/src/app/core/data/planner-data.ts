import { Card, CardChanges, CardDetails, CardLocation, Category, Position } from './models';

/**
 * Erro devolvido pela fonte de dados, espelhando o Problem Details da API: `code` é estável e é o que o código
 * usa para decidir o que fazer; `message` é só para humanos.
 */
export class PlannerDataError extends Error {
  constructor(
    readonly code: string,
    readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = 'PlannerDataError';
  }
}

/**
 * O contrato de acesso aos dados do planner. Os services dependem só desta abstração; a implementação é escolhida
 * em app.config.ts (fase 1: dados falsos em memória; fase 2: a API via HTTP). Cada método corresponde a um
 * endpoint seguindo docs/api-design.md.
 */
export abstract class PlannerData {
  /** GET /categories */
  abstract listCategories(): Promise<Category[]>;

  /** POST /categories */
  abstract createCategory(name: string): Promise<Category>;

  /** PATCH /categories/{id} */
  abstract updateCategory(id: string, changes: { name?: string; sortOrder?: number }): Promise<Category>;

  /** DELETE /categories/{id} */
  abstract deleteCategory(id: string): Promise<void>;

  /** GET /categories/{id}/cards — cartões na raiz do quadro da categoria. */
  abstract listRootCards(categoryId: string): Promise<Card[]>;

  /** GET /cards/{id}/children — cartões no quadro de um cartão. */
  abstract listChildren(cardId: string): Promise<Card[]>;

  /** GET /cards/{id} */
  abstract getCard(id: string): Promise<CardDetails>;

  /** POST /categories/{id}/cards ou POST /cards/{id}/children */
  abstract createCard(location: CardLocation, input: { title: string; position: Position }): Promise<Card>;

  /** PATCH /cards/{id} */
  abstract updateCard(id: string, changes: CardChanges): Promise<Card>;

  /** DELETE /cards/{id} — apaga também tudo o que está dentro dele. */
  abstract deleteCard(id: string): Promise<void>;
}
