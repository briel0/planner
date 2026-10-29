import { Position, Size } from '../../core/data/models';

/** Folga entre o cartão que chega e o último cartão do quadro, e margem de um quadro vazio. */
const GAP = 24;

/**
 * Onde colocar um cartão que chega a um quadro sem posição escolhida (ex.: movido de outro quadro): logo abaixo
 * do cartão mais baixo, alinhado à esquerda do conjunto, para nunca cair por cima de outro.
 */
export function positionBelow(cards: { position: Position; size: Size }[]): Position {
  if (cards.length === 0) {
    return { x: GAP, y: GAP };
  }
  const left = Math.min(...cards.map((c) => c.position.x));
  const bottom = Math.max(...cards.map((c) => c.position.y + c.size.height));
  return { x: left, y: bottom + GAP };
}
