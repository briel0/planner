import { readableTextColor } from './readable-text-color';

describe('readableTextColor', () => {
  it.each([
    ['#ffffff', 'dark'],
    ['#e5e7eb', 'dark'], // cinza padrão dos cartões
    ['#fde047', 'dark'], // amarelo claro
    ['#000000', 'light'],
    ['#1e3a8a', 'light'], // azul-marinho
    ['#b91c1c', 'light'], // vermelho escuro
  ])('uses %s → %s text', (background, expected) => {
    expect(readableTextColor(background)).toBe(expected);
  });
});
