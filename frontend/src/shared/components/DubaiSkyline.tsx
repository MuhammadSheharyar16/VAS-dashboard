import { useId, useMemo } from 'react';
import './skyline.css';

const WIDTH = 1600;
const GROUND = 520;

/** A plain tower: where it stands, how wide and how tall. */
type Block = readonly [x: number, width: number, height: number];

const FAR_BLOCKS: Block[] = [
  [20, 46, 96], [78, 34, 140], [124, 52, 84], [330, 40, 150], [382, 30, 110], [424, 56, 128],
  [560, 34, 170], [604, 48, 104], [880, 44, 160], [934, 30, 122], [976, 52, 92], [1170, 38, 176],
  [1218, 46, 118], [1340, 34, 148], [1384, 52, 96], [1500, 40, 132], [1548, 44, 88],
];

const MID_BLOCKS: Block[] = [
  [128, 44, 150], [330, 38, 196], [376, 50, 132], [438, 34, 224], [586, 46, 178], [706, 40, 214],
  [776, 44, 206], [884, 42, 232], [1004, 44, 168], [1180, 46, 206], [1238, 32, 150], [1500, 46, 164],
];

const NEAR_BLOCKS: Block[] = [
  [0, 70, 44], [74, 46, 70], [150, 54, 38], [300, 60, 56], [366, 40, 82], [612, 50, 64],
  [668, 64, 42], [860, 56, 66], [920, 70, 40], [1010, 44, 76], [1160, 66, 48], [1232, 48, 72],
  [1336, 72, 42], [1470, 52, 60], [1528, 72, 44],
];

/** Where the tallest tower stands: to one side, so it rises clear of whatever is centred on the page. */
const BURJ_KHALIFA_X = 1330;

/** The landmarks, each drawn as one silhouette standing on the ground line. */
const LANDMARKS = {
  // Burj Khalifa: setbacks that spiral up to the spire.
  burjKhalifa:
    'M1282,520 L1282,432 L1291,432 L1291,352 L1299,352 L1299,282 L1306,282 L1306,222 L1312,222 L1312,172 L1318,172 L1318,132 L1323,132 L1323,102 L1327,102 L1327,64 L1330,22 L1333,64 L1333,112 L1338,112 L1338,146 L1343,146 L1343,192 L1349,192 L1349,246 L1356,246 L1356,302 L1363,302 L1363,382 L1371,382 L1371,452 L1380,452 L1380,520 Z',
  // Burj Al Arab: the sail and its mast.
  burjAlArab: 'M214,520 L214,300 L218,300 L218,338 C292,372 306,462 306,520 Z M222,352 L262,376 L262,382 L222,360 Z',
  // Emirates Towers: two blades with slanted crowns.
  emiratesTowers: 'M1062,520 L1062,292 L1070,292 L1070,236 L1073,236 L1073,286 L1098,250 L1098,520 Z M1112,520 L1112,322 L1136,288 L1136,264 L1139,264 L1139,330 L1146,330 L1146,520 Z',
  // Cayan Tower: the quarter turn from foot to crown.
  cayanTower: 'M938,520 L950,286 L982,278 L978,520 Z',
  // Museum of the Future: the ring on its mound.
  museum: 'M486,520 L500,496 L580,496 L594,520 Z M540,436 C574,436 596,452 596,470 C596,488 574,500 540,500 C506,500 484,488 484,470 C484,452 506,436 540,436 Z M540,452 C522,452 510,460 510,470 C510,480 522,486 540,486 C558,486 570,480 570,470 C570,460 558,452 540,452 Z',
  // Dubai Frame.
  frame: 'M1416,520 L1416,348 L1470,348 L1470,520 Z M1426,520 L1426,360 L1460,360 L1460,520 Z',
  // The twin spires of a marina hotel.
  twinSpires: 'M632,520 L632,330 L640,318 L644,268 L648,318 L656,330 L656,520 Z M666,520 L666,344 L674,332 L678,286 L682,332 L690,344 L690,520 Z',
} as const;

/** A small repeatable generator, so the lit windows and the stars are the same on every render. */
function sequence(seed: number): () => number {
  let state = seed;
  return () => {
    state = (state * 1664525 + 1013904223) % 4294967296;
    return state / 4294967296;
  };
}

interface Light {
  x: number;
  y: number;
  group: number;
}

function windowsFor(blocks: Block[], seed: number, density: number): Light[] {
  const next = sequence(seed);
  const lights: Light[] = [];

  for (const [x, width, height] of blocks) {
    for (let column = x + 6; column < x + width - 6; column += 7) {
      for (let row = GROUND - height + 10; row < GROUND - 8; row += 10) {
        if (next() < density) {
          lights.push({ x: column, y: row, group: Math.floor(next() * 3) });
        }
      }
    }
  }

  return lights;
}

function starField(count: number): Light[] {
  const next = sequence(7);
  return Array.from({ length: count }, () => ({
    x: Math.round(next() * WIDTH),
    y: Math.round(next() * 300),
    group: Math.floor(next() * 3),
  }));
}

function Blocks({ blocks }: { blocks: Block[] }) {
  return (
    <>
      {blocks.map(([x, width, height]) => (
        <rect key={x} x={x} y={GROUND - height} width={width} height={height} rx={1.5} />
      ))}
    </>
  );
}

/**
 * The Dubai skyline as a drawn backdrop: Burj Khalifa to one side and the Burj Al Arab to the other,
 * with the Museum of the Future, Emirates Towers, Cayan Tower and the Dubai Frame between, over water.
 * It is decoration only, takes its colours from the theme tokens, and stands still: only its lights move.
 */
export function DubaiSkyline({ className = '' }: { className?: string }) {
  const id = useId();

  const midWindows = useMemo(() => windowsFor(MID_BLOCKS, 11, 0.24), []);
  const nearWindows = useMemo(() => windowsFor(NEAR_BLOCKS, 29, 0.3), []);
  const stars = useMemo(() => starField(70), []);

  const sky = `${id}-sky`;
  const sun = `${id}-sun`;
  const water = `${id}-water`;
  const fade = `${id}-fade`;
  const haze = `${id}-haze`;
  const city = `${id}-city`;

  return (
    <div className={`skyline ${className}`} aria-hidden="true">
      <svg className="skyline__art" viewBox={`0 0 ${WIDTH} 600`} preserveAspectRatio="xMidYMax slice" focusable="false">
        <defs>
          <linearGradient id={sky} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" className="skyline__stop-top" />
            <stop offset="0.55" className="skyline__stop-middle" />
            <stop offset="1" className="skyline__stop-horizon" />
          </linearGradient>
          <radialGradient id={sun} cx="0.5" cy="0.5" r="0.5">
            <stop offset="0" className="skyline__stop-sun" />
            <stop offset="0.35" className="skyline__stop-sun-halo" />
            <stop offset="1" className="skyline__stop-clear" />
          </radialGradient>
          <linearGradient id={water} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" className="skyline__stop-water-top" />
            <stop offset="1" className="skyline__stop-water-deep" />
          </linearGradient>
          <linearGradient id={haze} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" className="skyline__stop-haze-clear" />
            <stop offset="1" className="skyline__stop-haze" />
          </linearGradient>
          <linearGradient id={fade} x1="0" y1="0" x2="0" y2="1">
            <stop offset="0" stopColor="white" stopOpacity="0.5" />
            <stop offset="1" stopColor="white" stopOpacity="0" />
          </linearGradient>
          <mask id={`${fade}-mask`}>
            <rect x="0" y={GROUND} width={WIDTH} height="80" fill={`url(#${fade})`} />
          </mask>
        </defs>

        <rect width={WIDTH} height={GROUND} fill={`url(#${sky})`} />

        <g className="skyline__stars">
          {stars.map((star, index) => (
            <circle key={index} className={`skyline__twinkle skyline__twinkle--${star.group}`} cx={star.x} cy={star.y} r={index % 5 === 0 ? 1.5 : 1} />
          ))}
        </g>

        <g>
          <circle cx="1418" cy="150" r="240" fill={`url(#${sun})`} />
          <circle className="skyline__disc" cx="1418" cy="150" r="32" />
        </g>

        <g className="skyline__far">
          <Blocks blocks={FAR_BLOCKS} />
        </g>

        <g id={city}>
          <g className="skyline__mid">
            <Blocks blocks={MID_BLOCKS} />
            {Object.entries(LANDMARKS).map(([name, path]) => (
              <path key={name} d={path} fillRule="evenodd" />
            ))}
          </g>
          <g className="skyline__windows">
            {midWindows.map((light, index) => (
              <rect key={index} className={`skyline__twinkle skyline__twinkle--${light.group}`} x={light.x} y={light.y} width="2" height="4" />
            ))}
          </g>
          {/* The lit edge of the tallest tower, and the beacon at its tip. */}
          <path className="skyline__edge" d={`M${BURJ_KHALIFA_X},22 V${GROUND}`} />
          <circle className="skyline__beacon" cx={BURJ_KHALIFA_X} cy="22" r="3" />
        </g>

        {/* Haze gathers at the foot of the towers and lifts the nearest row off them. */}
        <rect x="0" y="300" width={WIDTH} height={GROUND - 300} fill={`url(#${haze})`} opacity="0.6" />

        <g>
          <g className="skyline__near">
            <Blocks blocks={NEAR_BLOCKS} />
          </g>
          <g className="skyline__windows">
            {nearWindows.map((light, index) => (
              <rect key={index} className={`skyline__twinkle skyline__twinkle--${light.group}`} x={light.x} y={light.y} width="2" height="4" />
            ))}
          </g>
        </g>

        {/* Water: the city mirrored in it, fading with depth, under a slow shimmer. */}
        <rect x="0" y={GROUND} width={WIDTH} height="80" fill={`url(#${water})`} />
        <g mask={`url(#${fade}-mask)`}>
          <use className="skyline__mirror" href={`#${city}`} transform={`translate(0 ${GROUND * 2}) scale(1 -1)`} />
        </g>
        <g className="skyline__shimmer">
          <path d="M120,538 h220 M520,548 h300 M980,540 h260 M1320,552 h180 M260,566 h340 M760,572 h420 M60,584 h260 M1180,588 h320" />
        </g>
        <path className="skyline__shore" d={`M0,${GROUND} H${WIDTH}`} />
      </svg>
      <span className="skyline__veil" />
    </div>
  );
}
