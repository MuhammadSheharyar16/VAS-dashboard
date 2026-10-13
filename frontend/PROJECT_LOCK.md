# PROJECT_LOCK: AEGI VAS Executive Dashboard (frontend)

Frozen frontend intake answers, per the factory Frontend Operating System. `project-specs.md` at the repository root stays the top authority; this file records how the frontend applies it. Change this file only with a decision recorded in the specs.

Locked: 12 October 2026. Source of each answer: the Q&A recorded in `project-specs.md` section 11, or `agent-default` where Claude chose.

## A. Product and platforms

| Item | Lock |
| --- | --- |
| Product | AEGI VAS Executive Dashboard: a fixed, read-only reporting dashboard |
| Platform | Web only |
| Repository | Greenfield, in `frontend/` |
| Users | AEGI executives assigned the `VasDashboard.Viewer` role |
| Devices | Desktop and tablet are designed for. Phone widths are usable but not specially designed |

## B. Framework and architecture

| Item | Lock |
| --- | --- |
| Framework | React 19 + TypeScript strict, Vite SPA |
| Architecture | `app/` (thin routes) + `features/` (Screen + `useXxxViewModel`) + `shared/` + `core/` |
| State | TanStack Query for server data. React context for theme and session only. No Redux |
| Router | React Router |
| API | REST, the backend under `/api/v1`. Mock-first behind the same repository interface |

## C. Design and requirements

| Item | Lock |
| --- | --- |
| Figma | None exists. Claude designs the screens; Sheharyar approves them (explicit instruction, overrides the "never invent UI" rule) |
| Design direction | Elegant, with depth and motion: glass surfaces, pointer-tilt 3D cards, an animated 3D globe, animated numbers and transitions (explicit instruction, 12 Oct 2026) |
| Home and navigation | The app opens on a home screen: a drawn Dubai skyline behind a 3D carousel of the reporting areas. A click brings a card to the front, and the front card opens its area. There is no sidebar; every page has a top bar whose "All areas" button returns to the home screen, and the report filters travel with the user (specs decision 30, 13 Oct 2026) |
| Requirements | `project-specs.md`; KPI definitions in `docs/kpi-mapping.md` |
| Icons | `lucide-react` (agent-default) |

## D. Theme and languages

| Item | Lock |
| --- | --- |
| Colours | Semantic tokens in `src/core/theme/tokens.css`. Neutral professional palette until AEGI supplies a brand. No hex outside the token file |
| Modes | Light and dark. Light is the default (Sheharyar's preference, 12 Oct 2026); the user can switch and the choice is remembered |
| Chart colours | The validated eight-slot categorical palette, assigned in fixed order; status colours are separate and always carry an icon and a label |
| Font | Inter Variable, bundled with the app (`@fontsource-variable/inter`); no request to a font host |
| Languages | English only. Every visible string is in `src/core/i18n/en.json` |
| RTL | Not in Phase 1. Logical CSS properties are used so it can be added |
| Motion | Decorative animation is on. `prefers-reduced-motion` switches off the tilt, the globe rotation, the count-up, the transitions, and the home screen's parallax, floating card and skyline lights; the carousel still turns, at once |

## E. Runtime and quality

| Item | Lock |
| --- | --- |
| Auth | Microsoft Entra ID through MSAL React, authorization code flow with PKCE. Token cache: `sessionStorage` |
| Authorization | Screen codes from `/auth/me/permissions`; menu from `/auth/me/menu`. UX only; the backend enforces |
| Data | Always from the API. The app has no mock mode and no fake user (Sheharyar's instruction, 13 Oct 2026). Mock or live is the API's setting; fixtures exported from the real backend live in `tests/fixtures/` for the tests only |
| Configuration | `VITE_API_BASE_URL` from env; Entra IDs at runtime from `/config.json`. The app shows a configuration error screen if the env value is missing |
| Sanitisation | Dates `yyyy-MM-dd`; codes of letters, digits, `-`, `_`, up to 20 characters; date range up to 24 months (specs 2.1) |
| KPI logic | None in React. The API returns values, changes and statuses ready to show |
| Print, PWA, offline, export | None |
| Security | No hosts, keys or tokens in source. No `console.*` in app code (ESLint `no-console`). No `dangerouslySetInnerHTML` |

## Packages added beyond the React guide's baseline

| Package | Why | Approval |
| --- | --- | --- |
| `recharts` | The one chart library | Specs decision 4 |
| `motion` | Transitions, staggered entrances, animated numbers | "Animated" instruction, 12 Oct 2026 |
| `three` | The 3D globe on the Executive Summary. Loaded only when that panel renders | "3D" instruction, 12 Oct 2026 |
| `lucide-react` | The one icon family | agent-default |
| `@fontsource-variable/inter` | Self-hosted font | agent-default |

## Commands

| Purpose | Command (run in `frontend/`) |
| --- | --- |
| Run in development | `npm run dev` |
| Type-check and production build | `npm run build` |
| Lint | `npm run lint` |
| Unit and component tests | `npm test` |
| Integration test (Playwright; Entra ID and the API are answered at the network) | `npm run test:integration` |

## Open gaps

- AEGI brand colours and logo (specs open question 17).
- Real KPI thresholds and the KPI Mapping (the labels and the set of KPIs may change).
- Real sign-in has been exercised only against the Dev registrations in the LogicEra tenant; AEGI's tenant is still to come.
