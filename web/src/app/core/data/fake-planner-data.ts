import { Injectable } from '@angular/core';
import { CARD_SIZE } from './card-size';
import { Card, CardChanges, CardDetails, CardLocation, CardProperties, Category, Position, Size } from './models';
import { PlannerData, PlannerDataError } from './planner-data';

const CATEGORY_NAME_MAX_LENGTH = 100;
const CARD_TITLE_MAX_LENGTH = 200;
const DEFAULT_CARD_COLOR = '#e5e7eb';
const HEX_COLOR = /^#[0-9a-fA-F]{6}$/;

type StoredCard = Omit<Card, 'childCount'>;

/**
 * Fase 1 do MVP: dados em memória, imitando as regras e os erros da API real (mesmos códigos de erro, mesmas
 * cascatas). Tudo se perde ao recarregar a página.
 */
@Injectable()
export class FakePlannerData extends PlannerData {
  private categories: Category[] = [];
  private cards: StoredCard[] = [];

  constructor() {
    super();
    this.seed();
  }

  // ---- Categorias ------------------------------------------------------------------------------------------

  async listCategories(): Promise<Category[]> {
    return [...this.categories].sort((a, b) => a.sortOrder - b.sortOrder);
  }

  async createCategory(name: string): Promise<Category> {
    const normalized = this.validateCategoryName(name);
    const sortOrder = Math.max(-1, ...this.categories.map((c) => c.sortOrder)) + 1;
    const category: Category = { id: crypto.randomUUID(), name: normalized, sortOrder, ...timestamps() };
    this.categories.push(category);
    return category;
  }

  async updateCategory(id: string, changes: { name?: string; sortOrder?: number }): Promise<Category> {
    const category = this.findCategory(id);
    const updated: Category = {
      ...category,
      name: changes.name === undefined ? category.name : this.validateCategoryName(changes.name, id),
      sortOrder: changes.sortOrder ?? category.sortOrder,
      updatedAt: now(),
    };
    this.categories = this.categories.map((c) => (c.id === id ? updated : c));
    return updated;
  }

  async deleteCategory(id: string): Promise<void> {
    this.findCategory(id);
    this.categories = this.categories.filter((c) => c.id !== id);
    this.cards = this.cards.filter((c) => c.categoryId !== id); // ON DELETE CASCADE
  }

  // ---- Cartões ---------------------------------------------------------------------------------------------

  async listRootCards(categoryId: string): Promise<Card[]> {
    this.findCategory(categoryId);
    return this.cards.filter((c) => c.categoryId === categoryId && c.parentId === null).map((c) => this.toCard(c));
  }

  async listChildren(cardId: string): Promise<Card[]> {
    this.findCard(cardId);
    return this.cards.filter((c) => c.parentId === cardId).map((c) => this.toCard(c));
  }

  async getCard(id: string): Promise<CardDetails> {
    const card = this.findCard(id);
    const ancestors = [];
    for (let parentId = card.parentId; parentId !== null;) {
      const parent = this.findCard(parentId);
      ancestors.unshift({ id: parent.id, title: parent.title });
      parentId = parent.parentId;
    }
    return { ...this.toCard(card), ancestors };
  }

  async createCard(location: CardLocation, input: { title: string; position: Position }): Promise<Card> {
    const parent = 'parentId' in location ? this.findCard(location.parentId) : null;
    const categoryId = parent
      ? parent.categoryId
      : this.findCategory((location as { categoryId: string }).categoryId).id;
    const siblings = this.cards.filter((c) => c.categoryId === categoryId && c.parentId === (parent?.id ?? null));
    const card: StoredCard = {
      id: crypto.randomUUID(),
      categoryId,
      parentId: parent?.id ?? null,
      title: this.validateTitle(input.title),
      color: DEFAULT_CARD_COLOR,
      position: this.validatePosition(input.position),
      size: { ...CARD_SIZE.default },
      layer: Math.max(-1, ...siblings.map((c) => c.layer)) + 1, // nasce na frente dos outros
      properties: {},
      content: null,
      ...timestamps(),
    };
    this.cards.push(card);
    return this.toCard(card);
  }

  async updateCard(id: string, changes: CardChanges): Promise<Card> {
    const card = this.findCard(id);
    const updated: StoredCard = {
      ...card,
      title: changes.title === undefined ? card.title : this.validateTitle(changes.title),
      color: changes.color === undefined ? card.color : this.validateColor(changes.color),
      position: changes.position === undefined ? card.position : this.validatePosition(changes.position),
      size: changes.size === undefined ? card.size : this.validateSize(changes.size),
      properties: changes.properties === undefined ? card.properties : withoutEmpty(changes.properties),
      content: changes.content === undefined ? card.content : this.validateContent(changes.content),
      updatedAt: now(),
    };
    this.cards = this.cards.map((c) => (c.id === id ? updated : c));
    return this.toCard(updated);
  }

  async deleteCard(id: string): Promise<void> {
    this.findCard(id);
    const doomed = new Set([id]);
    // Apaga a subárvore inteira (ON DELETE CASCADE em parent_id).
    for (let grew = true; grew;) {
      grew = false;
      for (const c of this.cards) {
        if (c.parentId && doomed.has(c.parentId) && !doomed.has(c.id)) {
          doomed.add(c.id);
          grew = true;
        }
      }
    }
    this.cards = this.cards.filter((c) => !doomed.has(c.id));
  }

  // ---- Regras (as mesmas do domínio e do banco) ------------------------------------------------------------

  private toCard(card: StoredCard): Card {
    return { ...card, childCount: this.cards.filter((c) => c.parentId === card.id).length };
  }

  private findCategory(id: string): Category {
    const category = this.categories.find((c) => c.id === id);
    if (!category) {
      throw new PlannerDataError('not-found', 404, 'Category not found.');
    }
    return category;
  }

  private findCard(id: string): StoredCard {
    const card = this.cards.find((c) => c.id === id);
    if (!card) {
      throw new PlannerDataError('not-found', 404, 'Card not found.');
    }
    return card;
  }

  private validateCategoryName(name: string, ignoreId?: string): string {
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

  private validateTitle(title: string): string {
    const trimmed = title.trim();
    if (trimmed.length === 0 || trimmed.length > CARD_TITLE_MAX_LENGTH) {
      throw new PlannerDataError(
        'card.invalid-title',
        400,
        `A card title must have between 1 and ${CARD_TITLE_MAX_LENGTH} characters.`,
      );
    }
    return trimmed;
  }

  private validateColor(color: string): string {
    if (!HEX_COLOR.test(color)) {
      throw new PlannerDataError('card.invalid-color', 400, `'${color}' is not a color in the #rrggbb format.`);
    }
    return color.toLowerCase();
  }

  private validatePosition(position: Position): Position {
    if (!Number.isFinite(position.x) || !Number.isFinite(position.y)) {
      throw new PlannerDataError('card.invalid-position', 400, 'Card coordinates must be finite numbers.');
    }
    return { x: position.x, y: position.y };
  }

  private validateContent(content: CardChanges['content']): StoredCard['content'] {
    const isObject = typeof content === 'object' && !Array.isArray(content);
    if (content !== null && !isObject) {
      throw new PlannerDataError('card.invalid-content', 400, 'Card content must be a JSON object.');
    }
    return content ?? null;
  }

  private validateSize(size: Size): Size {
    const { min, max } = CARD_SIZE;
    const fits = (value: number, low: number, high: number) => value >= low && value <= high;
    if (!fits(size.width, min.width, max.width) || !fits(size.height, min.height, max.height)) {
      throw new PlannerDataError('card.invalid-size', 400, 'Card size is outside the allowed limits.');
    }
    return { width: size.width, height: size.height };
  }

  // ---- Dados iniciais --------------------------------------------------------------------------------------

  private seed(): void {
    const add = (category: Category) => (this.categories.push(category), category);
    const faculdade = add({ id: crypto.randomUUID(), name: 'Faculdade', sortOrder: 0, ...timestamps() });
    const trabalho = add({ id: crypto.randomUUID(), name: 'Trabalho', sortOrder: 1, ...timestamps() });

    const card = (categoryId: string, parentId: string | null, title: string, x: number, y: number, extra = {}) => {
      const stored: StoredCard = {
        id: crypto.randomUUID(),
        categoryId,
        parentId,
        title,
        color: DEFAULT_CARD_COLOR,
        position: { x, y },
        size: { ...CARD_SIZE.default },
        layer: 0,
        properties: {},
        content: null,
        ...timestamps(),
        ...extra,
      };
      this.cards.push(stored);
      return stored;
    };

    const fisica = card(faculdade.id, null, 'Física Quântica', 80, 80, {
      color: '#bfdbfe',
      size: { width: 260, height: 150 },
      content: {
        type: 'doc',
        content: [
          { type: 'paragraph', content: [{ type: 'text', text: 'Quinta, 10h — sala 402' }] },
          {
            type: 'bulletList',
            content: ['Lista 3 até sexta', 'Revisar oscilador harmônico'].map((text) => ({
              type: 'listItem',
              content: [{ type: 'paragraph', content: [{ type: 'text', text }] }],
            })),
          },
        ],
      },
    });
    card(faculdade.id, fisica.id, 'Lista 3', 60, 60, { properties: { dueOn: inDays(3) } });
    card(faculdade.id, fisica.id, 'Prova 1', 320, 60, { color: '#fecaca', properties: { dueOn: inDays(10) } });
    card(faculdade.id, null, 'AED', 360, 80, { color: '#bbf7d0' });
    card(faculdade.id, null, 'Ler capítulo 4', 220, 240, { properties: { done: true } });
    card(trabalho.id, null, 'Deploy', 80, 80, { color: '#1e3a8a', properties: { dueOn: inDays(1) } });
  }
}

function now(): string {
  return new Date().toISOString();
}

function timestamps(): { createdAt: string; updatedAt: string } {
  const at = now();
  return { createdAt: at, updatedAt: at };
}

/** YYYY-MM-DD daqui a `days` dias, no fuso local. */
function inDays(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toLocaleDateString('sv-SE'); // o formato sueco é exatamente YYYY-MM-DD
}

/** Propriedades ausentes não são guardadas (como no banco: `{}` em vez de `{"dueOn": null}`). */
function withoutEmpty(properties: CardProperties): CardProperties {
  return Object.fromEntries(Object.entries(properties).filter(([, value]) => value !== undefined && value !== null));
}
