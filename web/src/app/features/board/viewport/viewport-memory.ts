import { Injectable } from '@angular/core';
import { DEFAULT_VIEWPORT, Viewport } from './viewport';

/** Lembra o zoom e a posição de cada quadro enquanto o app está aberto, para voltar a um quadro do jeito que estava. */
@Injectable({ providedIn: 'root' })
export class ViewportMemory {
  private readonly viewports = new Map<string, Viewport>();

  recall(boardKey: string): Viewport {
    return this.viewports.get(boardKey) ?? DEFAULT_VIEWPORT;
  }

  remember(boardKey: string, viewport: Viewport): void {
    this.viewports.set(boardKey, viewport);
  }
}
