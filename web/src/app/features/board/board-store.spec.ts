import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { FakePlannerData } from '../../core/data/fake-planner-data';
import { newId } from '../../core/data/ids';
import { PlannerData, PlannerDataError } from '../../core/data/planner-data';
import { BoardStore } from './board-store';

describe('BoardStore', () => {
  let store: BoardStore;
  let data: PlannerData;

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [BoardStore, { provide: PlannerData, useClass: FakePlannerData }] });
    store = TestBed.inject(BoardStore);
    data = TestBed.inject(PlannerData);
    const [faculdade] = await data.listCategories();
    await store.openCategory(faculdade.id);
  });

  const titled = (title: string) => {
    const card = store.cards().find((c) => c.title === title);
    if (!card) throw new Error(`no card '${title}'`);
    return card;
  };

  it('opens a card board with the trail down to it', async () => {
    await store.openCard(titled('Física Quântica').id);

    expect(store.trail().map((r) => r.title)).toEqual(['Física Quântica']);
    expect(
      store
        .cards()
        .map((c) => c.title)
        .sort(),
    ).toEqual(['Lista 3', 'Prova 1']);
  });

  it('creates cards inside the open board', async () => {
    const fisica = titled('Física Quântica');
    await store.openCard(fisica.id);

    const created = await store.create('Lista 4', { x: 10, y: 20 });

    expect(created.parentId).toBe(fisica.id);
    expect(created.categoryId).toBe(fisica.categoryId);
    expect(store.cards()).toContainEqual(created);
  });

  it('puts a moved card back where it was when saving fails', async () => {
    const aed = titled('AED');
    vi.spyOn(data, 'updateCard').mockRejectedValue(new PlannerDataError('not-found', 404, 'gone'));

    await expect(store.move(aed.id, { x: 999, y: 999 })).rejects.toBeInstanceOf(PlannerDataError);

    expect(titled('AED').position).toEqual(aed.position);
  });

  it('reloads the board when a text edit hits a stale version', async () => {
    const aed = titled('AED');
    await data.updateCard(aed.id, { title: 'AED (renomeado em outra aba)' }); // o store ainda tem a versão antiga

    await expect(store.rename(aed.id, 'AED 2')).rejects.toMatchObject({ code: 'concurrency.stale' });

    expect(titled('AED (renomeado em outra aba)').id).toBe(aed.id);
  });

  it('ignores a slow response from a board that is no longer open', async () => {
    const fisica = titled('Física Quântica');
    const [, trabalho] = await data.listCategories();

    const slow = store.openCard(fisica.id);
    await store.openCategory(trabalho.id);
    await slow;

    expect(store.cards().map((c) => c.title)).toEqual(['Deploy']);
  });

  it('moves a card with everything inside it to another category', async () => {
    const fisica = titled('Física Quântica');
    const [, trabalho] = await data.listCategories();

    await store.moveTo(fisica.id, { categoryId: trabalho.id }, { x: 24, y: 300 });

    expect(store.cards().map((c) => c.id)).not.toContain(fisica.id);
    const moved = await data.getCard(fisica.id);
    expect(moved).toMatchObject({ categoryId: trabalho.id, parentId: null, position: { x: 24, y: 300 } });
    expect((await data.listChildren(fisica.id)).every((c) => c.categoryId === trabalho.id)).toBe(true);
  });

  it('refuses to move a card into something inside it', async () => {
    const fisica = titled('Física Quântica');
    const [lista] = await data.listChildren(fisica.id);

    await expect(store.moveTo(fisica.id, { parentId: lista.id }, { x: 0, y: 0 })).rejects.toMatchObject({
      code: 'card.cycle',
    });
    expect(store.cards().map((c) => c.id)).toContain(fisica.id);
  });

  it('restores a deleted card with its content', async () => {
    const aed = titled('AED');
    const content = { type: 'doc', content: [{ type: 'paragraph', content: [{ type: 'text', text: 'Revisar' }] }] };
    await data.updateCard(aed.id, { color: '#ffcc00', content, properties: { done: true } });
    await store.openCategory(aed.categoryId);
    const before = titled('AED');

    await store.delete(aed.id);
    const restored = await store.restore(before);

    expect(titled('AED')).toEqual(restored);
    expect(restored).toMatchObject({
      color: '#ffcc00',
      content,
      properties: { done: true },
      position: before.position,
      size: before.size,
    });
  });

  it('refreshes silently, picking up cards created elsewhere', async () => {
    const statuses: string[] = [];
    const category = titled('AED').categoryId;
    await data.createCard(
      { categoryId: category },
      { id: newId(), title: 'Veio do Telegram', position: { x: 0, y: 0 } },
    );

    const refreshing = store.refresh();
    statuses.push(store.status());
    await refreshing;

    expect(statuses).toEqual(['ready']);
    expect(titled('Veio do Telegram')).toBeTruthy();
  });

  it('keeps the board on screen when a silent refresh fails', async () => {
    const titles = store.cards().map((c) => c.title);
    vi.spyOn(data, 'listRootCards').mockRejectedValue(new PlannerDataError('network', 0, 'offline'));

    await expect(store.refresh()).rejects.toBeInstanceOf(PlannerDataError);

    expect(store.status()).toBe('ready');
    expect(store.cards().map((c) => c.title)).toEqual(titles);
  });
});
