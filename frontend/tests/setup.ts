import '@testing-library/jest-dom/vitest';
import { cleanup } from '@testing-library/react';
import { afterEach, vi } from 'vitest';

afterEach(() => {
  cleanup();
  window.localStorage.clear();
});

// jsdom has no layout engine, so the chart library's size observer has nothing to observe.
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

vi.stubGlobal('ResizeObserver', ResizeObserverStub);

// The 3D globe needs WebGL, which jsdom does not have. It is decoration; the figures are in the list beside it.
vi.mock('@/shared/components/Globe3D', () => ({ default: () => null }));
