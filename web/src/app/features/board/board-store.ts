import { Injectable, inject, signal } from '@angular/core';
import { Card, CardChanges, CardLocation, CardRef, Position, Size } from '../../core/data/models';
import { PlannerData } from '../../core/data/planner-data';

/**
 * Estado do quadro aberto: o de uma categoria ou o de um cartão. Uma instância por página de quadro
 * (fornecida no componente, não na raiz).
 */
@Injectable()
export class BoardStore {
  private readonly data = inject(PlannerData);

  readonly cards = signal<Card[]>([]);
  /** Categoria do quadro (também quando o quadro é de um cartão). */
  readonly categoryId = signal<string | null>(null);
  /** Trilha de cartões até o quadro aberto (vazia no quadro da categoria; o último é o cartão aberto). */
  readonly trail = signal<CardRef[]>([]);
  readonly status = signal<'loading' | 'ready' | 'not-found'>('loading');

  private location: CardLocation | null = null;
  /** Descarta respostas de um quadro anterior quando a navegação é mais rápida que o carregamento. */
  private request = 0;

  async openCategory(categoryId: string): Promise<void> {
    await this.open({ categoryId }, async () => {
      const cards = await this.data.listRootCards(categoryId);
      return { cards, categoryId, trail: [] };
    });
  }

  async openCard(cardId: string): Promise<void> {
    await this.open({ parentId: cardId }, async () => {
      const [card, cards] = await Promise.all([this.data.getCard(cardId), this.data.listChildren(cardId)]);
      return { cards, categoryId: card.categoryId, trail: [...card.ancestors, { id: card.id, title: card.title }] };
    });
  }

  async create(title: string, position: Position): Promise<Card> {
    const created = await this.data.createCard(this.requireLocation(), { title, position });
    this.cards.update((list) => [...list, created]);
    return created;
  }

  async rename(id: string, title: string): Promise<void> {
    await this.change(id, { title });
  }

  async changeColor(id: string, color: string): Promise<void> {
    await this.change(id, { color });
  }

  async setProperties(id: string, properties: Card['properties']): Promise<void> {
    await this.change(id, { properties });
  }

  /** Mover é otimista: o cartão fica onde foi solto já; se salvar falhar, volta para onde estava. */
  async move(id: string, position: Position): Promise<void> {
    await this.optimistic(id, { position });
  }

  /** Redimensionar também é otimista, pelo mesmo motivo. */
  async resize(id: string, size: Size): Promise<void> {
    await this.optimistic(id, { size });
  }

  async delete(id: string): Promise<void> {
    await this.data.deleteCard(id);
    this.cards.update((list) => list.filter((c) => c.id !== id));
  }

  private async open(
    location: CardLocation,
    load: () => Promise<{ cards: Card[]; categoryId: string; trail: CardRef[] }>,
  ): Promise<void> {
    const request = ++this.request;
    this.location = location;
    this.status.set('loading');
    try {
      const board = await load();
      if (request !== this.request) {
        return;
      }
      this.cards.set(board.cards);
      this.categoryId.set(board.categoryId);
      this.trail.set(board.trail);
      this.status.set('ready');
    } catch (error) {
      if (request === this.request) {
        this.cards.set([]);
        this.status.set('not-found');
      }
      throw error;
    }
  }

  private async optimistic(id: string, changes: Pick<CardChanges, 'position' | 'size'>): Promise<void> {
    const before = this.cards().find((c) => c.id === id);
    this.replace(id, (card) => ({ ...card, ...changes }));
    try {
      await this.change(id, changes);
    } catch (error) {
      if (before) {
        this.replace(id, (card) => ({ ...card, position: before.position, size: before.size }));
      }
      throw error;
    }
  }

  private async change(id: string, changes: CardChanges): Promise<void> {
    const updated = await this.data.updateCard(id, changes);
    this.replace(id, () => updated);
  }

  private replace(id: string, update: (card: Card) => Card): void {
    this.cards.update((list) => list.map((c) => (c.id === id ? update(c) : c)));
  }

  private requireLocation(): CardLocation {
    if (!this.location) {
      throw new Error('No board is open.');
    }
    return this.location;
  }
}
