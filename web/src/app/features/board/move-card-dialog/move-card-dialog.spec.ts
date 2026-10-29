import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { vi } from 'vitest';
import { FakePlannerData } from '../../../core/data/fake-planner-data';
import { Card } from '../../../core/data/models';
import { PlannerData } from '../../../core/data/planner-data';
import { Notifier } from '../../../core/feedback/notifier';
import { CategoriesStore } from '../../categories/categories-store';
import { positionBelow } from '../placement';
import { MoveCardDialog } from './move-card-dialog';

describe('MoveCardDialog', () => {
  let fixture: ComponentFixture<MoveCardDialog>;
  let data: PlannerData;
  let close: ReturnType<typeof vi.fn>;
  let aed: Card;

  const options = () =>
    [...fixture.nativeElement.querySelectorAll('.option-title')].map((b: HTMLElement) => b.textContent!.trim());
  const moveButton = () =>
    [...fixture.nativeElement.querySelectorAll('mat-dialog-actions button')].at(-1) as HTMLButtonElement;
  const click = async (title: string) => {
    const button = [...fixture.nativeElement.querySelectorAll('.option')].find((b: HTMLElement) =>
      b.textContent!.includes(title),
    ) as HTMLButtonElement;
    button.click();
    await fixture.whenStable();
    fixture.detectChanges();
  };

  beforeEach(async () => {
    close = vi.fn();
    TestBed.configureTestingModule({
      imports: [MoveCardDialog],
      providers: [
        { provide: PlannerData, useClass: FakePlannerData },
        { provide: Notifier, useValue: { error: vi.fn() } },
        { provide: MatDialogRef, useValue: { close } },
        { provide: MAT_DIALOG_DATA, useFactory: () => ({ card: aed }) },
      ],
    });
    data = TestBed.inject(PlannerData);
    const [faculdade] = await data.listCategories();
    aed = (await data.listRootCards(faculdade.id)).find((c) => c.title === 'AED')!;
    await TestBed.inject(CategoriesStore).load();
    fixture = TestBed.createComponent(MoveCardDialog);
    fixture.detectChanges();
  });

  it('browses into a card and moves there, below what is already inside', async () => {
    await click('Faculdade');

    expect(options()).not.toContain('AED'); // o próprio cartão não é destino
    expect(moveButton().disabled).toBe(true); // ele já está neste quadro

    await click('Física Quântica');
    expect(moveButton().disabled).toBe(false);
    moveButton().click();

    const fisica = (await data.listRootCards(aed.categoryId)).find((c) => c.title === 'Física Quântica')!;
    const inside = await data.listChildren(fisica.id);
    expect(close).toHaveBeenCalledWith({ location: { parentId: fisica.id }, position: positionBelow(inside) });
  });
});
