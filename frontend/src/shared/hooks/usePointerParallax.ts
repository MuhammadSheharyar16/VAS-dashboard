import { useEffect, type RefObject } from 'react';
import { useFinePointer, useReducedMotion } from './useMotionPreference';

/**
 * Follows the pointer across the window and writes its position to the element as `--parallax-x`
 * and `--parallax-y`, each from -1 to 1. CSS decides what moves and how far. Nothing is written on
 * touch devices or for anyone who asked for reduced motion.
 */
export function usePointerParallax(ref: RefObject<HTMLElement | null>): void {
  const finePointer = useFinePointer();
  const reducedMotion = useReducedMotion();
  const enabled = finePointer && !reducedMotion;

  useEffect(() => {
    const element = ref.current;
    if (!enabled || !element) {
      return;
    }

    let frame = 0;
    let x = 0;
    let y = 0;

    const write = () => {
      frame = 0;
      element.style.setProperty('--parallax-x', x.toFixed(3));
      element.style.setProperty('--parallax-y', y.toFixed(3));
    };

    const handleMove = (event: PointerEvent) => {
      x = (event.clientX / window.innerWidth) * 2 - 1;
      y = (event.clientY / window.innerHeight) * 2 - 1;
      frame ||= window.requestAnimationFrame(write);
    };

    window.addEventListener('pointermove', handleMove, { passive: true });

    return () => {
      window.removeEventListener('pointermove', handleMove);
      window.cancelAnimationFrame(frame);
      element.style.removeProperty('--parallax-x');
      element.style.removeProperty('--parallax-y');
    };
  }, [enabled, ref]);
}
