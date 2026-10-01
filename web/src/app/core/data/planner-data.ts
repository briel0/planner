import { Category } from './models';

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
 * em app.config.ts (fase 1: dados falsos em memória; fase 2: a API via HTTP).
 */
export abstract class PlannerData {
  abstract listCategories(): Promise<Category[]>;

  abstract createCategory(name: string): Promise<Category>;

  abstract updateCategory(id: string, changes: { name?: string; sortOrder?: number }): Promise<Category>;

  abstract deleteCategory(id: string): Promise<void>;
}
