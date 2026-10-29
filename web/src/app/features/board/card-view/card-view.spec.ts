import { ComponentFixture, TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { FakePlannerData } from '../../../core/data/fake-planner-data';
import { Card } from '../../../core/data/models';
import { CardView } from './card-view';

describe('CardView', () => {
  let fixture: ComponentFixture<CardView>;
  let card: Card;
  let element: HTMLElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CardView] }).compileComponents();
    const data = new FakePlannerData();
    const [category] = await data.listCategories();
    card = (await data.listRootCards(category.id)).find((item) => item.title === 'AED')!;
    fixture = TestBed.createComponent(CardView);
    fixture.componentRef.setInput('card', card);
    fixture.detectChanges();
    element = fixture.nativeElement;
  });

  it('edits an empty description from the card body and opens only from the title', () => {
    const edit = vi.fn();
    const open = vi.fn();
    fixture.componentInstance.descriptionEditRequested.subscribe(edit);
    fixture.componentInstance.keyAction.subscribe(open);

    element.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    expect(edit).toHaveBeenCalledOnce();
    expect(open).not.toHaveBeenCalled();

    element.querySelector('.title')!.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    expect(open).toHaveBeenCalledWith('open');
  });

  it('changes only the description font without opening the card or starting editing', () => {
    const resize = vi.fn();
    const open = vi.fn();
    const edit = vi.fn();
    fixture.componentInstance.descriptionFontSizeChanged.subscribe(resize);
    fixture.componentInstance.keyAction.subscribe(open);
    fixture.componentInstance.descriptionEditRequested.subscribe(edit);

    const [minus, plus] = element.querySelectorAll<HTMLButtonElement>('.font-controls button');
    plus.click();
    minus.click();
    plus.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    plus.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

    expect(resize.mock.calls).toEqual([[16], [12]]);
    expect(open).not.toHaveBeenCalled();
    expect(edit).not.toHaveBeenCalled();
  });

  it('disables controls at their limits and while a save is pending', () => {
    fixture.componentRef.setInput('card', { ...card, descriptionFontSize: 10 });
    fixture.detectChanges();
    const [minus, plus] = element.querySelectorAll<HTMLButtonElement>('.font-controls button');
    expect(minus.disabled).toBe(true);
    expect(plus.disabled).toBe(false);

    fixture.componentRef.setInput('card', { ...card, descriptionFontSize: 32 });
    fixture.detectChanges();
    expect(plus.disabled).toBe(true);

    fixture.componentRef.setInput('fontSizeSaving', true);
    fixture.detectChanges();
    expect(minus.disabled).toBe(true);
    expect(plus.disabled).toBe(true);
  });

  it('previews the background and readable text without changing the saved card', () => {
    const originalColor = card.color;
    fixture.componentRef.setInput('previewColor', '#000000');
    fixture.detectChanges();
    expect(element.style.backgroundColor).toBe('rgb(0, 0, 0)');
    expect(element.classList.contains('light-text')).toBe(true);
    expect(card.color).toBe(originalColor);

    fixture.componentRef.setInput('previewColor', '#ffffff');
    fixture.detectChanges();
    expect(element.style.backgroundColor).toBe('rgb(255, 255, 255)');
    expect(element.classList.contains('light-text')).toBe(false);

    fixture.componentRef.setInput('previewColor', null);
    fixture.detectChanges();
    expect(element.style.backgroundColor).not.toBe('rgb(255, 255, 255)');
    expect(card.color).toBe(originalColor);
  });

  it('offers an explicit description action and expands only while editing', () => {
    const edit = vi.fn();
    fixture.componentInstance.descriptionEditRequested.subscribe(edit);
    const placeholder = element.querySelector('.description-placeholder') as HTMLButtonElement;
    placeholder.click();
    expect(edit).toHaveBeenCalledOnce();

    const size = { ...card.size };
    fixture.componentRef.setInput('editingDescription', true);
    fixture.detectChanges();
    expect(element.classList.contains('editing-description')).toBe(true);
    expect(element.querySelector('.editor-actions button')?.textContent).toContain('Concluir');
    expect(card.size).toEqual(size);
  });
});
