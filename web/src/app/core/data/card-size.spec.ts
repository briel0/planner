import { clampSize, resizeBounds } from './card-size';

describe('clampSize', () => {
  it('keeps sizes within the limits', () => {
    expect(clampSize({ width: 300, height: 120 })).toEqual({ width: 300, height: 120 });
    expect(clampSize({ width: 10, height: -5 })).toEqual({ width: 120, height: 48 });
    expect(clampSize({ width: 5000, height: 5000 })).toEqual({ width: 1200, height: 900 });
  });
});

describe('resizeBounds', () => {
  const start = { position: { x: 100, y: 80 }, size: { width: 300, height: 200 } };

  it('keeps the opposite corner fixed when resizing from north-west', () => {
    expect(resizeBounds(start, 'nw', { x: 40, y: 30 })).toEqual({
      position: { x: 140, y: 110 },
      size: { width: 260, height: 170 },
    });
  });

  it('changes only the matching dimension when using a side', () => {
    expect(resizeBounds(start, 'e', { x: 50, y: 90 })).toEqual({
      position: start.position,
      size: { width: 350, height: 200 },
    });
  });

  it('adjusts north-west position when the minimum size is reached', () => {
    expect(resizeBounds(start, 'nw', { x: 1000, y: 1000 })).toEqual({
      position: { x: 280, y: 232 },
      size: { width: 120, height: 48 },
    });
  });

  it('honors the maximum size while preserving the south-east corner', () => {
    expect(resizeBounds(start, 'nw', { x: -2000, y: -2000 })).toEqual({
      position: { x: -800, y: -620 },
      size: { width: 1200, height: 900 },
    });
  });
});
