import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { FakePlannerData } from '../../../core/data/fake-planner-data';
import { PlannerData, PlannerDataError } from '../../../core/data/planner-data';
import { Notifier } from '../../../core/feedback/notifier';
import { CardView } from '../card-view/card-view';
import { BoardStore } from '../board-store';
import { BoardPage } from './board-page';

describe('BoardPage text saving', () => {
  let fixture: ComponentFixture<BoardPage>;
  let data: PlannerData;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BoardPage],
      providers: [
        provideRouter([]),
        { provide: PlannerData, useClass: FakePlannerData },
        { provide: Notifier, useValue: { error: vi.fn() } },
      ],
    }).compileComponents();
    data = TestBed.inject(PlannerData);
    const [category] = await data.listCategories();
    fixture = TestBed.createComponent(BoardPage);
    fixture.componentRef.setInput('categoryId', category.id);
    fixture.detectChanges();
    await fixture.debugElement.injector.get(BoardStore).openCategory(category.id);
    await fixture.whenStable();
    fixture.detectChanges();
  });

  it('keeps a failed description draft visible and saves it on retry', async () => {
    const view = fixture.debugElement
      .queryAll(By.directive(CardView))
      .find((item) => (item.componentInstance as CardView).card().title === 'AED')!;
    const card = view.componentInstance as CardView;
    const id = card.card().id;
    const content = {
      type: 'doc',
      content: [{ type: 'paragraph', content: [{ type: 'text', text: 'Não perder este texto' }] }],
    };
    const update = vi.spyOn(data, 'updateCard').mockRejectedValueOnce(new PlannerDataError('network', 0, 'offline'));

    card.descriptionChanged.emit(content);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(card.card().content).toEqual(content);
    expect(card.textSaveState()).toBe('error');
    expect((await data.getCard(id)).content).toBeNull();
    const retry = view.nativeElement.querySelector('.save-feedback button') as HTMLButtonElement;
    retry.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(update).toHaveBeenCalledTimes(2);
    expect((await data.getCard(id)).content).toEqual(content);
    expect(card.textSaveState()).toBe('saved');
  });

  it('preserves the local title after a conflict without overwriting the server automatically', async () => {
    const view = fixture.debugElement
      .queryAll(By.directive(CardView))
      .find((item) => (item.componentInstance as CardView).card().title === 'AED')!;
    const card = view.componentInstance as CardView;
    const id = card.card().id;
    await data.updateCard(id, { title: 'Título da outra aba' });

    card.renamed.emit('Meu título ainda não salvo');
    await fixture.whenStable();
    fixture.detectChanges();

    const current = fixture.debugElement
      .queryAll(By.directive(CardView))
      .map((item) => item.componentInstance as CardView)
      .find((item) => item.card().id === id)!;
    expect(current.card().title).toBe('Meu título ainda não salvo');
    expect(current.textSaveState()).toBe('error');
    expect((await data.getCard(id)).title).toBe('Título da outra aba');
  });
});
