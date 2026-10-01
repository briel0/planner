import { Injectable, computed, inject, signal } from '@angular/core';
import { Category } from '../../core/data/models';
import { newId } from '../../core/data/ids';
import { PlannerData, PlannerDataError } from '../../core/data/planner-data';

/** Estado das categorias (as abas do rodapé), compartilhado por todas as telas. */
@Injectable({ providedIn: 'root' })
export class CategoriesStore {
  private readonly data = inject(PlannerData);

  private readonly state = signal<Category[]>([]);
  private readonly loaded = signal(false);

  /** Categoria do quadro aberto (de um cartão em qualquer profundidade, inclusive); destaca a aba no rodapé. */
  readonly activeCategoryId = signal<string | null>(null);

  /** Categorias na ordem das abas. */
  readonly categories = computed(() => [...this.state()].sort((a, b) => a.sortOrder - b.sortOrder));
  readonly isLoaded = this.loaded.asReadonly();

  async load(): Promise<void> {
    this.state.set(await this.data.listCategories());
    this.loaded.set(true);
  }

  byId(id: string): Category | undefined {
    return this.state().find((c) => c.id === id);
  }

  async create(name: string): Promise<Category> {
    const created = await this.data.createCategory(newId(), name);
    this.state.update((list) => [...list, created]);
    return created;
  }

  /**
   * Renomear é editar texto: vai com a versão conhecida (If-Match), para não sobrescrever uma mudança feita em outra
   * aba. Se a categoria mudou antes, recarrega as abas e repassa o erro (`concurrency.stale`).
   */
  async rename(id: string, name: string): Promise<void> {
    try {
      this.replace(await this.data.updateCategory(id, { name }, this.byId(id)?.version));
    } catch (error) {
      if (error instanceof PlannerDataError && error.code === 'concurrency.stale') {
        await this.load();
      }
      throw error;
    }
  }

  async delete(id: string): Promise<void> {
    await this.data.deleteCategory(id);
    this.state.update((list) => list.filter((c) => c.id !== id));
  }

  /**
   * Reordena as abas a partir da nova ordem dos ids. Só as categorias cuja posição mudou são enviadas, e a tela é
   * atualizada antes (otimista); se algo falhar, o estado é recarregado da fonte de dados.
   */
  async reorder(orderedIds: string[]): Promise<void> {
    const changed = orderedIds
      .map((id, sortOrder) => ({ id, sortOrder }))
      .filter(({ id, sortOrder }) => this.byId(id)?.sortOrder !== sortOrder);
    if (changed.length === 0) {
      return;
    }

    this.state.update((list) =>
      list.map((c) => {
        const change = changed.find((x) => x.id === c.id);
        return change ? { ...c, sortOrder: change.sortOrder } : c;
      }),
    );
    try {
      for (const { id, sortOrder } of changed) {
        await this.data.updateCategory(id, { sortOrder });
      }
    } catch (error) {
      await this.load();
      throw error;
    }
  }

  private replace(updated: Category): void {
    this.state.update((list) => list.map((c) => (c.id === updated.id ? updated : c)));
  }
}
