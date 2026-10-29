import { Injectable, inject, signal } from '@angular/core';
import { CardBounds } from '../../core/data/card-size';
import { Card, CardChanges, CardLocation, CardRef, Position } from '../../core/data/models';
import { newId } from '../../core/data/ids';
import { PlannerData, PlannerDataError } from '../../core/data/planner-data';

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

  async openCategory(categoryId: string, silent = false): Promise<void> {
    await this.open(
      { categoryId },
      async () => {
        const cards = await this.data.listRootCards(categoryId);
        return { cards, categoryId, trail: [] };
      },
      silent,
    );
  }

  async openCard(cardId: string, silent = false): Promise<void> {
    await this.open(
      { parentId: cardId },
      async () => {
        const [card, cards] = await Promise.all([this.data.getCard(cardId), this.data.listChildren(cardId)]);
        return { cards, categoryId: card.categoryId, trail: [...card.ancestors, { id: card.id, title: card.title }] };
      },
      silent,
    );
  }

  async create(title: string, position: Position): Promise<Card> {
    const created = await this.data.createCard(this.requireLocation(), { id: newId(), title, position });
    this.cards.update((list) => [...list, created]);
    return created;
  }

  /** Título e descrição são texto: vão com If-Match, para não sobrescrever o que foi escrito em outra aba. */
  async rename(id: string, title: string): Promise<void> {
    await this.saveText(id, { title });
  }

  async changeColor(id: string, color: string): Promise<void> {
    await this.change(id, { color });
  }

  async changeDescriptionFontSize(id: string, descriptionFontSize: number): Promise<void> {
    await this.change(id, { descriptionFontSize });
  }

  async changeContent(id: string, content: Card['content']): Promise<void> {
    await this.saveText(id, { content });
  }

  async setProperties(id: string, properties: Card['properties']): Promise<void> {
    await this.change(id, { properties });
  }

  /** Mover é otimista: o cartão fica onde foi solto já; se salvar falhar, volta para onde estava. */
  async move(id: string, position: Position): Promise<void> {
    await this.optimistic(id, { position });
  }

  /** Redimensionar também é otimista; posição e tamanho vão juntos (as bordas de cima e da esquerda movem o cartão). */
  async resize(id: string, bounds: CardBounds): Promise<void> {
    await this.optimistic(id, bounds);
  }

  async delete(id: string): Promise<void> {
    await this.data.deleteCard(id);
    this.cards.update((list) => list.filter((c) => c.id !== id));
  }

  /**
   * O "Desfazer" de apagar: recria o cartão no quadro onde ele estava, com o mesmo conteúdo (num id novo). Só serve
   * para cartões sem nada dentro: o que estava dentro de um cartão apagado não volta.
   */
  async restore(card: Card): Promise<Card> {
    const location: CardLocation = card.parentId ? { parentId: card.parentId } : { categoryId: card.categoryId };
    const created = await this.data.createCard(location, { id: newId(), title: card.title, position: card.position });
    const restored = await this.data.updateCard(created.id, {
      color: card.color,
      size: card.size,
      descriptionFontSize: card.descriptionFontSize,
      properties: card.properties,
      content: card.content,
    });
    if (sameLocation(this.location, location)) {
      this.cards.update((list) => [...list, restored]);
    }
    return restored;
  }

  /** Move o cartão para outro quadro (com o que está dentro dele); aqui ele simplesmente sai da tela. */
  async moveTo(id: string, location: CardLocation, position: Position): Promise<void> {
    await this.data.updateCard(id, { location, position });
    this.cards.update((list) => list.filter((c) => c.id !== id));
  }

  /**
   * Busca de novo o quadro aberto sem mostrar "carregando": traz o que mudou fora desta tela (outra aba, um agente
   * criando cartões pela API).
   */
  async refresh(): Promise<void> {
    if (this.status() === 'ready') {
      await this.reload(true);
    }
  }

  private async open(
    location: CardLocation,
    load: () => Promise<{ cards: Card[]; categoryId: string; trail: CardRef[] }>,
    silent = false,
  ): Promise<void> {
    const request = ++this.request;
    this.location = location;
    if (!silent) {
      this.status.set('loading');
    }
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
      // Numa atualização silenciosa, uma falha (ex.: sem rede) mantém o quadro que já está na tela.
      if (request === this.request && !silent) {
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

  private async change(id: string, changes: CardChanges, ifMatch?: string): Promise<void> {
    const updated = await this.data.updateCard(id, changes, ifMatch);
    this.replace(id, () => updated);
  }

  /** Se o cartão mudou em outro lugar (`concurrency.stale`), recarrega o quadro antes de repassar o erro. */
  async saveText(id: string, changes: Pick<CardChanges, 'title' | 'content'>): Promise<void> {
    try {
      await this.change(id, changes, this.cards().find((c) => c.id === id)?.version);
    } catch (error) {
      if (error instanceof PlannerDataError && error.code === 'concurrency.stale') {
        await this.reload();
      }
      throw error;
    }
  }

  private async reload(silent = false): Promise<void> {
    const location = this.requireLocation();
    await ('parentId' in location
      ? this.openCard(location.parentId, silent)
      : this.openCategory(location.categoryId, silent));
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

/** Se dois lugares são o mesmo quadro. */
export function sameLocation(a: CardLocation | null, b: CardLocation | null): boolean {
  if (!a || !b) {
    return false;
  }
  return 'parentId' in a
    ? 'parentId' in b && a.parentId === b.parentId
    : 'categoryId' in b && a.categoryId === b.categoryId;
}
