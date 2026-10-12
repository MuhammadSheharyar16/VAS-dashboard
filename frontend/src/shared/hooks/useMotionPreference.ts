import { useSyncExternalStore } from 'react';

function subscribe(query: string) {
  return (onChange: () => void) => {
    const media = window.matchMedia?.(query);
    media?.addEventListener('change', onChange);
    return () => media?.removeEventListener('change', onChange);
  };
}

function useMediaQuery(query: string, whenUnsupported: boolean): boolean {
  return useSyncExternalStore(
    subscribe(query),
    () => window.matchMedia?.(query).matches ?? whenUnsupported,
    () => whenUnsupported,
  );
}

/**
 * True when decorative motion should be skipped: the user asked for reduced motion, or the
 * environment cannot say (which is treated as "reduce").
 */
export function useReducedMotion(): boolean {
  return useMediaQuery('(prefers-reduced-motion: reduce)', true);
}

/** True on devices with a mouse or trackpad, where a pointer-driven tilt makes sense. */
export function useFinePointer(): boolean {
  return useMediaQuery('(hover: hover) and (pointer: fine)', false);
}
