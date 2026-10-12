import { animate } from 'motion/react';
import { useEffect, useRef } from 'react';
import { useReducedMotion } from '@/shared/hooks/useMotionPreference';

interface AnimatedNumberProps {
  value: number | null;
  format(value: number | null): string;
}

/**
 * Counts up to its value. The final text is always what is rendered, so screen readers,
 * tests and reduced-motion users get the real number immediately; the count-up only
 * rewrites the visible text on its way there.
 */
export function AnimatedNumber({ value, format }: AnimatedNumberProps) {
  const ref = useRef<HTMLSpanElement>(null);
  const previous = useRef(0);
  const reducedMotion = useReducedMotion();

  useEffect(() => {
    const element = ref.current;
    if (!element || value === null || reducedMotion) {
      previous.current = value ?? 0;
      return;
    }

    const controls = animate(previous.current, value, {
      duration: 0.9,
      ease: [0.22, 1, 0.36, 1],
      onUpdate: (latest) => {
        element.textContent = format(latest);
      },
      onComplete: () => {
        element.textContent = format(value);
      },
    });

    previous.current = value;
    return () => controls.stop();
  }, [value, format, reducedMotion]);

  return <span ref={ref}>{format(value)}</span>;
}
