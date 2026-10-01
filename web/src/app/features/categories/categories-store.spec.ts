import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { FakePlannerData } from '../../core/data/fake-planner-data';
import { PlannerData, PlannerDataError } from '../../core/data/planner-data';
import { CategoriesStore } from './categories-store';

describe('CategoriesStore', () => {
  let store: CategoriesStore;
  let data: PlannerData;

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [{ provide: PlannerData, useClass: FakePlannerData }] });
    store = TestBed.inject(CategoriesStore);
    data = TestBed.inject(PlannerData);
    await store.load(); // dados iniciais do fake: Faculdade (0), Trabalho (1)
  });

  const names = () => store.categories().map((c) => c.name);

  it('reorders the tabs and saves only the categories whose position changed', async () => {
    await store.create('Pessoal');
    const update = vi.spyOn(data, 'updateCategory');
    const [faculdade, trabalho, pessoal] = store.categories();

    await store.reorder([pessoal.id, faculdade.id, trabalho.id]);

    expect(names()).toEqual(['Pessoal', 'Faculdade', 'Trabalho']);
    expect(update).toHaveBeenCalledTimes(3);
    update.mockClear();

    await store.reorder([pessoal.id, faculdade.id, trabalho.id]);
    expect(update).not.toHaveBeenCalled();
  });

  it('keeps the current tabs when a rename is rejected', async () => {
    const [faculdade] = store.categories();

    await expect(store.rename(faculdade.id, 'trabalho')).rejects.toMatchObject({ code: 'category.name-taken' });

    expect(names()).toEqual(['Faculdade', 'Trabalho']);
  });

  it('reloads the real order when saving a reorder fails', async () => {
    const [faculdade, trabalho] = store.categories();
    vi.spyOn(data, 'updateCategory').mockRejectedValue(new PlannerDataError('not-found', 404, 'gone'));

    await expect(store.reorder([trabalho.id, faculdade.id])).rejects.toBeInstanceOf(PlannerDataError);

    expect(names()).toEqual(['Faculdade', 'Trabalho']);
  });
});
