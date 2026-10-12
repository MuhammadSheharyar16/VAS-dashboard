import { useEffect, useRef } from 'react';
import {
  BufferGeometry,
  Color,
  Float32BufferAttribute,
  Group,
  Line,
  LineBasicMaterial,
  LineLoop,
  Mesh,
  MeshBasicMaterial,
  PerspectiveCamera,
  Points,
  PointsMaterial,
  RingGeometry,
  Scene,
  SphereGeometry,
  Vector3,
  WebGLRenderer,
  type Material,
} from 'three';
import { useReducedMotion } from '@/shared/hooks/useMotionPreference';
import { COUNTRY_COORDINATES } from '@/shared/utils/countryCoordinates';

export interface GlobeMarker {
  /** ISO 3166-1 alpha-2 country code. */
  code: string;
  /** Relative size from 0 to 1 (the country's share of the largest). */
  weight: number;
}

interface Globe3DProps {
  markers: GlobeMarker[];
  /** Changes when the colour theme changes, so the globe repaints in the new colours. */
  themeKey: string;
  ariaLabel: string;
}

const DOT_COUNT = 1700;
const RADIANS = Math.PI / 180;

function toVector(latitude: number, longitude: number, radius: number): Vector3 {
  const phi = (90 - latitude) * RADIANS;
  const theta = (longitude + 180) * RADIANS;

  return new Vector3(
    -radius * Math.sin(phi) * Math.cos(theta),
    radius * Math.cos(phi),
    radius * Math.sin(phi) * Math.sin(theta),
  );
}

/** Points spread evenly over a sphere (a Fibonacci lattice). */
function spherePoints(count: number): Float32Array {
  const positions = new Float32Array(count * 3);
  const golden = Math.PI * (3 - Math.sqrt(5));

  for (let index = 0; index < count; index++) {
    const y = 1 - (index / (count - 1)) * 2;
    const ring = Math.sqrt(1 - y * y);
    const angle = golden * index;
    positions.set([Math.cos(angle) * ring, y, Math.sin(angle) * ring], index * 3);
  }

  return positions;
}

function ringLoop(latitude: number, colour: Color): LineLoop {
  const points: Vector3[] = [];
  for (let longitude = -180; longitude < 180; longitude += 6) {
    points.push(toVector(latitude, longitude, 1.002));
  }

  return new LineLoop(
    new BufferGeometry().setFromPoints(points),
    new LineBasicMaterial({ color: colour, transparent: true, opacity: 0.22 }),
  );
}

/**
 * A slowly turning globe of dots, with a pulsing marker and a rising beam on each top country.
 * It is decoration: the figures it hints at are listed in text beside it, and its label says so.
 * It pauses when off screen or in a background tab, stays still for reduced motion, and takes up
 * no space where WebGL is unavailable.
 */
export default function Globe3D({ markers, themeKey, ariaLabel }: Globe3DProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const reducedMotion = useReducedMotion();

  useEffect(() => {
    const container = containerRef.current;
    if (!container) {
      return;
    }

    let renderer: WebGLRenderer;
    try {
      renderer = new WebGLRenderer({ antialias: true, alpha: true });
    } catch {
      // No WebGL here: leave the space out and let the list beside it carry the figures.
      container.hidden = true;
      return;
    }

    const styles = getComputedStyle(container);
    const colourOf = (token: string, fallback: string) => new Color(styles.getPropertyValue(token).trim() || fallback);
    const dotColour = colourOf('--globe-dot', '#6b7390');
    const markerColour = colourOf('--globe-marker', '#2f9fd0');
    const haloColour = colourOf('--globe-halo', '#4f5ee0');

    const scene = new Scene();
    const camera = new PerspectiveCamera(36, 1, 0.1, 50);
    camera.position.set(0, 0, 3.5);

    const globe = new Group();
    globe.rotation.x = 0.32;
    scene.add(globe);

    const dotGeometry = new BufferGeometry();
    dotGeometry.setAttribute('position', new Float32BufferAttribute(spherePoints(DOT_COUNT), 3));
    globe.add(
      new Points(
        dotGeometry,
        new PointsMaterial({ color: dotColour, size: 0.016, sizeAttenuation: true, transparent: true, opacity: 0.85 }),
      ),
    );

    for (const latitude of [-45, 0, 45]) {
      globe.add(ringLoop(latitude, dotColour));
    }

    const halos: { mesh: Mesh; phase: number }[] = [];
    let facing: Vector3 | null = null;
    let heaviest = -1;

    markers.forEach((marker, index) => {
      const coordinates = COUNTRY_COORDINATES[marker.code];
      if (!coordinates) {
        return;
      }

      const weight = Math.min(Math.max(marker.weight, 0), 1);
      const surface = toVector(coordinates[0], coordinates[1], 1.01);

      if (weight > heaviest) {
        heaviest = weight;
        facing = surface;
      }

      const dot = new Mesh(
        new SphereGeometry(0.018 + 0.026 * weight, 16, 16),
        new MeshBasicMaterial({ color: markerColour }),
      );
      dot.position.copy(surface);
      globe.add(dot);

      // The beam rises out of the surface; its height is the country's share.
      const beam = new Line(
        new BufferGeometry().setFromPoints([surface, surface.clone().multiplyScalar(1.08 + 0.34 * weight)]),
        new LineBasicMaterial({ color: markerColour, transparent: true, opacity: 0.9 }),
      );
      globe.add(beam);

      const halo = new Mesh(
        new RingGeometry(0.03, 0.042, 40),
        new MeshBasicMaterial({ color: haloColour, transparent: true, opacity: 0.8, side: 2, depthWrite: false }),
      );
      halo.position.copy(surface);
      halo.lookAt(surface.clone().multiplyScalar(2));
      globe.add(halo);
      halos.push({ mesh: halo, phase: index * 0.37 });
    });

    // Start with the largest market facing the viewer.
    const front = facing as Vector3 | null;
    if (front) {
      globe.rotation.y = Math.atan2(-front.x, front.z);
    }

    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    container.appendChild(renderer.domElement);

    const resize = () => {
      const size = Math.max(container.clientWidth, 1);
      renderer.setSize(size, size, false);
    };
    resize();

    const resizeObserver = new ResizeObserver(resize);
    resizeObserver.observe(container);

    let visible = true;
    const visibilityObserver = new IntersectionObserver(([entry]) => {
      visible = entry?.isIntersecting ?? true;
    });
    visibilityObserver.observe(container);

    let pointerTilt = 0;
    const handlePointer = (event: PointerEvent) => {
      const bounds = container.getBoundingClientRect();
      pointerTilt = ((event.clientY - bounds.top) / bounds.height - 0.5) * 0.35;
    };
    const handlePointerLeave = () => {
      pointerTilt = 0;
    };
    container.addEventListener('pointermove', handlePointer);
    container.addEventListener('pointerleave', handlePointerLeave);

    const paintHalos = (seconds: number) => {
      for (const { mesh, phase } of halos) {
        const progress = (seconds * 0.5 + phase) % 1;
        mesh.scale.setScalar(1 + progress * 2.4);
        (mesh.material as MeshBasicMaterial).opacity = 0.8 * (1 - progress);
      }
    };

    let frame = 0;
    let last = performance.now();

    const tick = (now: number) => {
      frame = requestAnimationFrame(tick);
      const delta = Math.min((now - last) / 1000, 0.05);
      last = now;

      if (!visible || document.hidden) {
        return;
      }

      globe.rotation.y += delta * 0.16;
      globe.rotation.x += (0.32 + pointerTilt - globe.rotation.x) * 0.06;
      paintHalos(now / 1000);
      renderer.render(scene, camera);
    };

    if (reducedMotion) {
      paintHalos(0.4);
      renderer.render(scene, camera);
    } else {
      frame = requestAnimationFrame(tick);
    }

    return () => {
      cancelAnimationFrame(frame);
      resizeObserver.disconnect();
      visibilityObserver.disconnect();
      container.removeEventListener('pointermove', handlePointer);
      container.removeEventListener('pointerleave', handlePointerLeave);

      scene.traverse((object) => {
        const disposable = object as { geometry?: BufferGeometry; material?: Material | Material[] };
        disposable.geometry?.dispose();
        for (const material of [disposable.material].flat()) {
          material?.dispose();
        }
      });

      renderer.dispose();
      renderer.domElement.remove();
    };
  }, [markers, themeKey, reducedMotion]);

  return <div ref={containerRef} className="globe" role="img" aria-label={ariaLabel} />;
}
