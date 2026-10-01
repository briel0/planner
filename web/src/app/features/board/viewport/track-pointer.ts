/**
 * Acompanha um gesto de "apertar e arrastar" a partir de um pointerdown: avisa o deslocamento (em pixels da tela)
 * a cada movimento e, ao soltar, se houve movimento de fato. Abaixo de `threshold` pixels o gesto ainda conta
 * como clique, para não atrapalhar cliques e duplos cliques. Serve para mover cartões, redimensionar e mover o canvas.
 */
export function trackPointer(
  event: PointerEvent,
  handlers: { move: (dx: number, dy: number) => void; end: (moved: boolean) => void },
  threshold = 3,
): void {
  const target = event.currentTarget as HTMLElement;
  const start = { x: event.clientX, y: event.clientY };
  let moved = false;
  target.setPointerCapture(event.pointerId);

  const onMove = (e: PointerEvent) => {
    const dx = e.clientX - start.x;
    const dy = e.clientY - start.y;
    if (!moved && Math.hypot(dx, dy) < threshold) {
      return;
    }
    moved = true;
    handlers.move(dx, dy);
  };
  const onEnd = () => {
    target.removeEventListener('pointermove', onMove);
    target.removeEventListener('pointerup', onEnd);
    target.removeEventListener('pointercancel', onEnd);
    handlers.end(moved);
  };
  target.addEventListener('pointermove', onMove);
  target.addEventListener('pointerup', onEnd);
  target.addEventListener('pointercancel', onEnd);
}
