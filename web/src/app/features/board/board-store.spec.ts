import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { FakePlannerData } from '../../core/data/fake-planner-data';
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
});
