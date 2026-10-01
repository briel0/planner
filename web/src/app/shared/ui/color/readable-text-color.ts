/**
 * Cor de texto legível sobre um fundo #rrggbb: escura em fundos claros, clara em fundos escuros.
 * Usa a luminância relativa da WCAG (a mesma fórmula das regras de contraste de acessibilidade).
 */
export function readableTextColor(background: string): 'dark' | 'light' {
  const [r, g, b] = [1, 3, 5].map((i) => toLinear(parseInt(background.slice(i, i + 2), 16) / 255));
  const luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  // Ponto em que o contraste com preto e com branco se iguala.
  return luminance > 0.179 ? 'dark' : 'light';
}

function toLinear(channel: number): number {
  return channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4;
}
