import { Position, Size } from './models';

/** Limites do tamanho de um cartão: os mesmos do domínio e do banco (docs/data-modeling/04-physical-model.md). */
export const CARD_SIZE = {
  min: { width: 120, height: 48 },
  max: { width: 1200, height: 900 },
  default: { width: 208, height: 64 },
} as const;

/** Borda ou canto pelo qual o cartão é redimensionado, como pontos cardeais. */
export type ResizeDirection = 'n' | 'ne' | 'e' | 'se' | 's' | 'sw' | 'w' | 'nw';

/** Posição e tamanho juntos: redimensionar por cima ou pela esquerda também move o cartão. */
export interface CardBounds {
  position: Position;
  size: Size;
}

/** Mantém um tamanho dentro dos limites (usado enquanto o usuário arrasta uma borda do cartão). */
export function clampSize(size: Size): Size {
  const clamp = (value: number, low: number, high: number) => Math.min(high, Math.max(low, value));
  return {
    width: clamp(size.width, CARD_SIZE.min.width, CARD_SIZE.max.width),
    height: clamp(size.height, CARD_SIZE.min.height, CARD_SIZE.max.height),
  };
}

/**
 * Redimensiona arrastando uma borda ou canto (`delta` em pixels do canvas), mantendo a borda oposta parada, inclusive
 * quando o tamanho bate num limite.
 */
export function resizeBounds(start: CardBounds, direction: ResizeDirection, delta: Position): CardBounds {
  const west = direction.includes('w');
  const north = direction.includes('n');
  const dx = west ? -delta.x : direction.includes('e') ? delta.x : 0;
  const dy = north ? -delta.y : direction.includes('s') ? delta.y : 0;
  const size = clampSize({ width: start.size.width + dx, height: start.size.height + dy });
  return {
    position: {
      x: west ? start.position.x + start.size.width - size.width : start.position.x,
      y: north ? start.position.y + start.size.height - size.height : start.position.y,
    },
    size,
  };
}
