import { Injectable } from '@angular/core';
import { Category } from './models';
import { PlannerData, PlannerDataError } from './planner-data';

const CATEGORY_NAME_MAX_LENGTH = 100;

/**
 * Fase 1 do MVP: dados em memória, imitando as regras e os erros da API real (mesmos códigos de erro).
 * Tudo se perde ao recarregar a página.
 */
@Injectable()
export class FakePlannerData extends PlannerData {
  private categories: Category[] = [seed('Faculdade', 0), seed('Trabalho', 1)];

  async listCategories(): Promise<Category[]> {
    return [...this.categories].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async createCategory(name: string): Promise<Category> {
    const normalized = this.validateName(name);
    const sortOrder = Math.max(-1, ...this.categories.map((c) => c.sortOrder)) + 1;
    const category = seed(normalized, sortOrder);
    this.categories.push(category);
    return category;
  }

  async updateCategory(id: string, changes: { name?: string; sortOrder?: number }): Promise<Category> {
    const category = this.find(id);
    const name = changes.name === undefined ? category.name : this.validateName(changes.name, id);
    const updated: Category = {
      ...category,
      name,
      sortOrder: changes.sortOrder ?? category.sortOrder,
      updatedAt: new Date().toISOString(),
    };
    this.categories = this.categories.map((c) => (c.id === id ? updated : c));
    return updated;
  }

  async deleteCategory(id: string): Promise<void> {
    this.find(id);
    this.categories = this.categories.filter((c) => c.id !== id);
  }

  private find(id: string): Category {
    const category = this.categories.find((c) => c.id === id);
    if (!category) {
      throw new PlannerDataError('not-found', 404, 'Category not found.');
    }
    return category;
  }

  /** Mesmas regras do domínio e do banco: sem espaços nas pontas, 1 a 100 caracteres, único ignorando maiúsculas. */
  private validateName(name: string, ignoreId?: string): string {
    const trimmed = name.trim();
    if (trimmed.length === 0 || trimmed.length > CATEGORY_NAME_MAX_LENGTH) {
      throw new PlannerDataError(
        'category.invalid-name',
        400,
        `A category name must have between 1 and ${CATEGORY_NAME_MAX_LENGTH} characters.`,
      );
    }
    const taken = this.categories.some((c) => c.id !== ignoreId && c.name.toLowerCase() === trimmed.toLowerCase());
    if (taken) {
      throw new PlannerDataError('category.name-taken', 409, `A category named '${trimmed}' already exists.`);
    }
    return trimmed;
  }
}

function seed(name: string, sortOrder: number): Category {
  const now = new Date().toISOString();
  return { id: crypto.randomUUID(), name, sortOrder, createdAt: now, updatedAt: now };
}
