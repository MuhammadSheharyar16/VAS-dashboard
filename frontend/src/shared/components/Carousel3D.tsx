import { ChevronLeft, ChevronRight } from 'lucide-react';
import {
  useCallback,
  useEffect,
  useRef,
  useState,
  type CSSProperties,
  type FocusEvent,
  type KeyboardEvent,
  type MouseEvent,
  type PointerEvent,
  type ReactNode,
} from 'react';
import { useFinePointer, useReducedMotion } from '@/shared/hooks/useMotionPreference';
import { usePointerParallax } from '@/shared/hooks/usePointerParallax';
import './carousel3d.css';

export interface Carousel3DItemState {
  /** True for the item at the front. */
  isActive: boolean;
  /** Places from the front: 0 at the front, negative to the left, positive to the right. */
  offset: number;
}

interface Carousel3DProps<T> {
  items: T[];
  getKey(item: T): string;
  /** A short name for the item, used by the position dots and the screen-reader announcement. */
  getLabel(item: T): string;
  renderItem(item: T, state: Carousel3DItemState): ReactNode;
  /** Names the carousel for assistive technology. */
  label: string;
  previousLabel: string;
  nextLabel: string;
  /** Label of a position dot, given the item's name. */
  showLabel(name: string): string;
  initialIndex?: number;
  className?: string;
}

interface Pose {
  x: number;
  z: number;
  rotateY: number;
  scale: number;
  opacity: number;
}

/** Where an item sits for each distance from the front. Beyond the last pose it is hidden behind. */
const POSES: Pose[] = [
  { x: 0, z: 0, rotateY: 0, scale: 1, opacity: 1 },
  { x: 312, z: -170, rotateY: -30, scale: 0.9, opacity: 1 },
  { x: 672, z: -390, rotateY: -46, scale: 0.8, opacity: 1 },
  { x: 780, z: -600, rotateY: -54, scale: 0.7, opacity: 0 },
];

const SWIPE_DISTANCE = 48;

/** How long the mouse must hold still on an item behind before the ring turns to it. Passing over does nothing. */
const HOVER_REST_MS = 200;
/** After a turn the ring waits this long, and for the pointer to travel this far, before turning again. */
const HOVER_PAUSE_MS = 750;
const HOVER_TRAVEL = 28;

function poseFor(offset: number): Pose {
  const distance = Math.min(Math.abs(offset), POSES.length - 1);
  const pose = POSES[distance] ?? POSES[0]!;
  const side = Math.sign(offset);

  return { ...pose, x: pose.x * side, rotateY: pose.rotateY * side };
}

/**
 * The pose as plain CSS. The stylesheet eases between poses, so the ring still turns, at once,
 * for anyone who asked for reduced motion.
 */
function slotStyle(pose: Pose, depth: number, order: number): CSSProperties {
  return {
    transform: `translate3d(${pose.x}px, 0, ${pose.z}px) rotateY(${pose.rotateY}deg) scale(${pose.scale})`,
    opacity: pose.opacity,
    zIndex: depth,
    '--carousel-order': order,
  } as CSSProperties;
}

/** The shortest way round the ring from the front item to this one. */
export function ringOffset(index: number, active: number, count: number): number {
  const half = Math.floor(count / 2);
  return ((((index - active + half) % count) + count) % count) - half;
}

/**
 * Items on a ring in 3D: one at the front, its neighbours turned away behind it. Moving the mouse
 * onto an item behind, or clicking it, brings it to the front, and only the front item acts on a
 * click. The ring also turns with the arrow buttons, the arrow keys, a swipe or drag, and the
 * position dots, and follows keyboard focus so a focused item is always at the front.
 */
export function Carousel3D<T>({
  items,
  getKey,
  getLabel,
  renderItem,
  label,
  previousLabel,
  nextLabel,
  showLabel,
  initialIndex = 0,
  className = '',
}: Carousel3DProps<T>) {
  const count = items.length;
  const [active, setActive] = useState(() => Math.min(Math.max(initialIndex, 0), Math.max(count - 1, 0)));
  const stageRef = useRef<HTMLDivElement>(null);
  const slotRefs = useRef<(HTMLLIElement | null)[]>([]);
  const drag = useRef({ startX: 0, pointerId: -1, moved: false });
  const hover = useRef({ timer: 0, turnedAt: 0, turnedX: 0, turnedY: 0, seenX: -1, seenY: -1 });
  const finePointer = useFinePointer();
  const reducedMotion = useReducedMotion();
  const turnsOnHover = finePointer && !reducedMotion;

  usePointerParallax(stageRef);

  const turn = useCallback(
    (step: number) => setActive((current) => (((current + step) % count) + count) % count),
    [count],
  );

  const handleKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = event.key === 'ArrowRight' ? 1 : event.key === 'ArrowLeft' ? -1 : 0;
    if (step === 0) {
      return;
    }

    event.preventDefault();
    const next = (((active + step) % count) + count) % count;
    setActive(next);

    // Focus goes with the turn only when it was already on an item, so the arrow buttons keep theirs.
    if (slotRefs.current.some((slot) => slot?.contains(document.activeElement))) {
      slotRefs.current[next]?.querySelector<HTMLElement>('a, button')?.focus({ preventScroll: true });
    }
  };

  /**
   * Tabbing to an item brings it to the front. A press with the pointer also focuses the item, and
   * turning the ring then would move the item away before the click lands, so that focus is ignored.
   */
  const followKeyboardFocus = (event: FocusEvent<HTMLLIElement>, index: number) => {
    if (event.target.matches(':focus-visible')) {
      setActive(index);
    }
  };

  const handlePointerDown = (event: PointerEvent<HTMLUListElement>) => {
    drag.current = { startX: event.clientX, pointerId: event.pointerId, moved: false };
  };

  const handlePointerUp = (event: PointerEvent<HTMLUListElement>) => {
    if (drag.current.pointerId !== event.pointerId) {
      return;
    }

    const distance = event.clientX - drag.current.startX;
    drag.current.pointerId = -1;

    if (Math.abs(distance) >= SWIPE_DISTANCE) {
      drag.current.moved = true;
      turn(distance < 0 ? 1 : -1);
    }
  };

  /** A swipe that ends on an item must not also open it. */
  const handleClickCapture = (event: MouseEvent<HTMLUListElement>) => {
    if (drag.current.moved) {
      drag.current.moved = false;
      event.preventDefault();
      event.stopPropagation();
    }
  };

  const cancelHover = useCallback(() => {
    window.clearTimeout(hover.current.timer);
    hover.current.timer = 0;
  }, []);

  useEffect(() => cancelHover, [cancelHover]);

  /**
   * The mouse resting on an item behind turns the ring to it. The turn carries another item under
   * the pointer, so the next turn waits for a pause and for the pointer to really move again;
   * otherwise the ring would keep spinning under a mouse that is standing still.
   */
  const handleHover = (event: PointerEvent<HTMLLIElement>, index: number) => {
    const state = hover.current;
    if (!turnsOnHover || event.pointerType !== 'mouse' || event.buttons !== 0 || index === active) {
      return;
    }

    // When an item slides under a mouse that is standing still, the browser reports a movement
    // with the same position as before. That is the ring moving, not the mouse.
    const stoodStill = event.clientX === state.seenX && event.clientY === state.seenY;
    state.seenX = event.clientX;
    state.seenY = event.clientY;
    if (stoodStill) {
      return;
    }

    const travelled = Math.hypot(event.clientX - state.turnedX, event.clientY - state.turnedY);
    if (event.timeStamp - state.turnedAt < HOVER_PAUSE_MS || travelled < HOVER_TRAVEL) {
      return;
    }

    // Every movement starts the wait again, so the turn comes only once the mouse has stopped.
    const { clientX, clientY } = event;
    window.clearTimeout(state.timer);
    state.timer = window.setTimeout(() => {
      hover.current = { ...hover.current, timer: 0, turnedAt: performance.now(), turnedX: clientX, turnedY: clientY };
      setActive(index);
    }, HOVER_REST_MS);
  };

  /** A click on an item that is not at the front turns the ring to it and goes no further. */
  const bringToFront = (event: MouseEvent<HTMLLIElement>, index: number) => {
    if (index !== active) {
      event.preventDefault();
      event.stopPropagation();
      cancelHover();
      setActive(index);
    }
  };

  if (count === 0) {
    return null;
  }

  const activeItem = items[active];

  return (
    <div
      className={`carousel3d ${className}`}
      role="group"
      aria-roledescription="carousel"
      aria-label={label}
      onKeyDown={handleKeyDown}
    >
      <div ref={stageRef} className="carousel3d__stage">
        <span className="carousel3d__floor" aria-hidden="true" />

        <ul
          className="carousel3d__ring"
          onPointerDown={handlePointerDown}
          onPointerUp={handlePointerUp}
          onPointerCancel={() => (drag.current.pointerId = -1)}
          onClickCapture={handleClickCapture}
          onDragStart={(event) => event.preventDefault()}
        >
          {items.map((item, index) => {
            const offset = ringOffset(index, active, count);
            const pose = poseFor(offset);
            const isActive = offset === 0;

            return (
              <li
                key={getKey(item)}
                ref={(element) => {
                  slotRefs.current[index] = element;
                }}
                className="carousel3d__slot"
                data-active={isActive}
                data-hidden={pose.opacity === 0}
                style={slotStyle(pose, count - Math.abs(offset), Math.abs(offset))}
                onClickCapture={(event) => bringToFront(event, index)}
                onPointerMove={(event) => handleHover(event, index)}
                onPointerLeave={cancelHover}
                onFocus={(event) => followKeyboardFocus(event, index)}
              >
                <div className="carousel3d__enter">
                  <div className="carousel3d__float">{renderItem(item, { isActive, offset })}</div>
                </div>
              </li>
            );
          })}
        </ul>
      </div>

      <div className="carousel3d__controls">
        <button type="button" className="carousel3d__arrow" onClick={() => turn(-1)} aria-label={previousLabel}>
          <ChevronLeft size={20} aria-hidden="true" />
        </button>

        <div className="carousel3d__dots">
          {items.map((item, index) => (
            <button
              key={getKey(item)}
              type="button"
              className="carousel3d__dot"
              aria-label={showLabel(getLabel(item))}
              aria-current={index === active ? 'true' : undefined}
              onClick={() => setActive(index)}
            />
          ))}
        </div>

        <button type="button" className="carousel3d__arrow" onClick={() => turn(1)} aria-label={nextLabel}>
          <ChevronRight size={20} aria-hidden="true" />
        </button>
      </div>

      <p className="visually-hidden" aria-live="polite">
        {activeItem === undefined ? '' : getLabel(activeItem)}
      </p>
    </div>
  );
}
