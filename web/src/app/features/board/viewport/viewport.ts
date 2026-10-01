import { Position, Size } from '../../../core/data/models';

/**
 * A "câmera" sobre o canvas: quanto ele está ampliado (`zoom`) e onde está a origem do canvas na tela (`pan`,
 * em pixels da tela). Um ponto do canvas (cx, cy) aparece na tela em (pan.x + cx * zoom, pan.y + cy * zoom).
 */
export interface Viewport {
  zoom: number;
  pan: Position;
}

export const ZOOM_LIMITS = { min: 0.25, max: 2.5 } as const;

export const DEFAULT_VIEWPORT: Viewport = { zoom: 1, pan: { x: 0, y: 0 } };

/** Converte um ponto da tela (relativo à área do canvas) para coordenadas do canvas. */
export function screenToCanvas(viewport: Viewport, point: Position): Position {
  return { x: (point.x - viewport.pan.x) / viewport.zoom, y: (point.y - viewport.pan.y) / viewport.zoom };
}

/**
 * Aplica um zoom mantendo fixo o ponto sob o cursor: o que estava embaixo do mouse continua embaixo dele,
 * como em mapas e editores gráficos.
 */
export function zoomAt(viewport: Viewport, point: Position, factor: number): Viewport {
  const zoom = clampZoom(viewport.zoom * factor);
  const anchor = screenToCanvas(viewport, point);
  return { zoom, pan: { x: point.x - anchor.x * zoom, y: point.y - anchor.y * zoom } };
}

/** Enquadra todos os cartões na área visível, com uma margem; sem ampliar além de 100%. */
export function fitToCards(cards: { position: Position; size: Size }[], area: Size, margin = 48): Viewport {
  if (cards.length === 0) {
    return DEFAULT_VIEWPORT;
  }
  const left = Math.min(...cards.map((c) => c.position.x));
  const top = Math.min(...cards.map((c) => c.position.y));
  const right = Math.max(...cards.map((c) => c.position.x + c.size.width));
  const bottom = Math.max(...cards.map((c) => c.position.y + c.size.height));
  const zoom = clampZoom(
    Math.min(1, (area.width - 2 * margin) / (right - left), (area.height - 2 * margin) / (bottom - top)),
  );
  // Centraliza o conjunto de cartões na área.
  return {
    zoom,
    pan: {
      x: (area.width - (right - left) * zoom) / 2 - left * zoom,
      y: (area.height - (bottom - top) * zoom) / 2 - top * zoom,
    },
  };
}

function clampZoom(zoom: number): number {
  return Math.min(ZOOM_LIMITS.max, Math.max(ZOOM_LIMITS.min, zoom));
}
