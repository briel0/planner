import { Size } from './models';

/** Limites do tamanho de um cartão: os mesmos do domínio e do banco (docs/data-modeling/04-physical-model.md). */
export const CARD_SIZE = {
  min: { width: 120, height: 48 },
  max: { width: 1200, height: 900 },
  default: { width: 208, height: 64 },
} as const;

/** Mantém um tamanho dentro dos limites (usado enquanto o usuário arrasta o canto do cartão). */
export function clampSize(size: Size): Size {
  const clamp = (value: number, low: number, high: number) => Math.min(high, Math.max(low, value));
  return {
    width: clamp(size.width, CARD_SIZE.min.width, CARD_SIZE.max.width),
    height: clamp(size.height, CARD_SIZE.min.height, CARD_SIZE.max.height),
  };
}
