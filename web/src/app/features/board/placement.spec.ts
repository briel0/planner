import { positionBelow } from './placement';

describe('positionBelow', () => {
  it('starts near the corner of an empty board', () => {
    expect(positionBelow([])).toEqual({ x: 24, y: 24 });
  });

  it('goes under the lowest card, aligned with the leftmost one', () => {
    const cards = [
      { position: { x: 100, y: 0 }, size: { width: 200, height: 64 } },
      { position: { x: 40, y: 200 }, size: { width: 200, height: 100 } },
    ];

    expect(positionBelow(cards)).toEqual({ x: 40, y: 324 });
  });
});
