import { clampSize } from './card-size';

describe('clampSize', () => {
  it('keeps sizes within the limits', () => {
    expect(clampSize({ width: 300, height: 120 })).toEqual({ width: 300, height: 120 });
    expect(clampSize({ width: 10, height: -5 })).toEqual({ width: 120, height: 48 });
    expect(clampSize({ width: 5000, height: 5000 })).toEqual({ width: 1200, height: 900 });
  });
});
