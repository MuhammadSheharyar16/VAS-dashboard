import { useCallback, useRef, type PointerEvent, type ReactNode } from 'react';
import { useFinePointer, useReducedMotion } from '@/shared/hooks/useMotionPreference';

const MAX_TILT_DEGREES = 9;

interface TiltCardProps {
  children: ReactNode;
  className?: string;
}

/**
 * A surface that leans toward the pointer in 3D, with a soft light that follows it.
 * It stays flat for touch devices and for anyone who asked for reduced motion.
 */
export function TiltCard({ children, className = '' }: TiltCardProps) {
  const ref = useRef<HTMLDivElement>(null);
  const finePointer = useFinePointer();
  const reducedMotion = useReducedMotion();
  const enabled = finePointer && !reducedMotion;

  const handleMove = useCallback(
    (event: PointerEvent<HTMLDivElement>) => {
      const element = ref.current;
      if (!enabled || !element) {
        return;
      }

      const bounds = element.getBoundingClientRect();
      const x = (event.clientX - bounds.left) / bounds.width;
      const y = (event.clientY - bounds.top) / bounds.height;

      element.classList.add('tilt--active');
      element.style.setProperty('--tilt-x', `${((0.5 - y) * MAX_TILT_DEGREES).toFixed(2)}deg`);
      element.style.setProperty('--tilt-y', `${((x - 0.5) * MAX_TILT_DEGREES * 1.2).toFixed(2)}deg`);
      element.style.setProperty('--glare-x', `${(x * 100).toFixed(1)}%`);
      element.style.setProperty('--glare-y', `${(y * 100).toFixed(1)}%`);
    },
    [enabled],
  );

  const handleLeave = useCallback(() => {
    const element = ref.current;
    if (!element) {
      return;
    }

    element.classList.remove('tilt--active');
    element.style.setProperty('--tilt-x', '0deg');
    element.style.setProperty('--tilt-y', '0deg');
  }, []);

  return (
    <div ref={ref} className={`tilt ${className}`} onPointerMove={handleMove} onPointerLeave={handleLeave}>
      <div className="tilt__inner">
        {children}
        <span className="tilt__glare" aria-hidden="true" />
      </div>
    </div>
  );
}
