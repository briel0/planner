import { DEFAULT_VIEWPORT, fitToCards, screenToCanvas, zoomAt } from './viewport';

describe('viewport', () => {
  it('converts screen points to canvas coordinates', () => {
    const viewport = { zoom: 2, pan: { x: 100, y: 50 } };

    expect(screenToCanvas(viewport, { x: 300, y: 250 })).toEqual({ x: 100, y: 100 });
  });

  it('keeps the point under the cursor fixed while zooming', () => {
    const cursor = { x: 400, y: 300 };
    const before = screenToCanvas(DEFAULT_VIEWPORT, cursor);

    const zoomed = zoomAt(DEFAULT_VIEWPORT, cursor, 2);

    expect(zoomed.zoom).toBe(2);
    expect(screenToCanvas(zoomed, cursor)).toEqual(before);
  });

  it('never zooms beyond the limits', () => {
    expect(zoomAt(DEFAULT_VIEWPORT, { x: 0, y: 0 }, 100).zoom).toBe(2.5);
    expect(zoomAt(DEFAULT_VIEWPORT, { x: 0, y: 0 }, 0.001).zoom).toBe(0.25);
  });

  it('fits all cards in the visible area, centered', () => {
    const cards = [
      { position: { x: 0, y: 0 }, size: { width: 200, height: 100 } },
      { position: { x: 1800, y: 900 }, size: { width: 200, height: 100 } },
    ];

    const fitted = fitToCards(cards, { width: 1096, height: 596 }, 48);

    expect(fitted.zoom).toBe(0.5); // 2000 × 1000 de cartões em 1000 × 500 úteis
    expect(fitted.pan).toEqual({ x: 48, y: 48 });
  });
});
