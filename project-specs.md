# AEGI VAS Executive Dashboard: Project Specs

Status: **Updated with the Q&A decisions of 12 October 2026. Awaiting plan approval before implementation starts.**
Last updated: 12 October 2026 (Q&A batches 1 to 3 applied; see section 11).

## 0. Precedence

This file is the top authority for the project. When anything conflicts, follow this order:

1. **This file (`project-specs.md`)** and the decisions recorded here.
2. The client documents it is built from:
   - *AEGI VAS Executive Dashboard – Phase 1 Proposition & Requirements v2* (the requirements doc)
   - *AEGI VAS Executive Dashboard Architecture and Delivery Roadmap v2.0, 9 October 2026* (the roadmap)
   - *AEGI Intelligence Hub v3 – KPI Mapping* (not in the project yet)
3. **Designs produced by Claude and approved by Sheharyar.** No Figma designs exist. If AEGI later supplies approved Figma designs, they replace these.
4. The magicbox factory guidelines:
   - `Backend Development/Dotnet/Monolith/BACKEND-Guide.md`
   - `Frontend Development/FrontendGuidebook/` (Operating System, Prompt Chain, Web, React, Testing, Security guides)
   - `QA Engineering/` guidebooks, non-AI parts only (section 9)
5. *AEGI VAS Executive Dashboard: Implementation Guide* (12 Oct 2026). It is input for structure and steps. Wherever it disagrees with this file, this file wins (the known disagreements are listed in section 9).

The factory guides are **guidelines**. They decide structure and code quality only where this file is silent. Every place where this file overrides a guide is listed in section 9. Anything covered by neither is asked, not invented.

## 1. Product

| Item | Spec |
| --- | --- |
| Product | Custom Executive Dashboard giving AEGI leadership one consolidated view of VAS business performance |
| Users | AEGI executives assigned access in Entra ID |
| Reporting areas | Executive Summary, VAS, Operations, Finance, Alfit (plus CSAT placeholder) |
| Phase 1 active | **Executive Summary** and **VAS** |
| Phase 1 placeholders | **Operations, Finance, Alfit** as "Coming Soon" navigation, no reporting; **CSAT** shown as Coming Soon |
| Delivery parties | AEGI (client) and LogicEra (delivery) |
| Repository | New build in this repository. No starter project and no code from `VAS-demo-project` is used |
| Repository layout | Everything backend-related lives in `backend/` (solution file, build props, `src/`, `tests/`, mock-data generator). The frontend lives in `frontend/` |

## 2. Phase 1 functional requirements

- **Navigation:** one dashboard with Executive Summary and VAS, plus the placeholder areas, all reached from the home screen's carousel (decision 30).
- **KPI presentation:** clearly labelled KPI cards and supporting charts.
- **Filters and comparisons:** date range, country, visa type and other agreed filters; period-on-period comparison where validated data supports it.
- **Breakdowns:** visa, application, country, centre and journey views of the agreed KPIs.
- **Performance indicators:** trends, status and changes based on agreed KPI definitions and thresholds.
- **States:** loading, empty and error states; show the data's freshness timestamp.
- **No customisation:** users cannot add, remove, rearrange or configure KPIs or layouts.

### VAS reporting areas

| Area | Phase 1 content |
| --- | --- |
| Visa applications and volumes | Received, processed, pending; breakdowns and trends |
| Visa outcomes | Approved, rejected, other agreed outcomes; approval and rejection rates |
| Visa processing and journey | Application status, processing stages, digital journey, turnaround times |
| VAS revenue | Revenue and breakdowns by country, centre or visa type where supported |
| Visa types | Volumes and performance by visa type |
| Centre performance | Centre activity, processing volumes, centre journey, turnaround, agreed capacity measures |

The final KPI set is what AEGI confirms **and** validated VAS data supports. Unsupported KPIs are reviewed jointly, not built.

### 2.1 Behaviour decisions (12 Oct 2026)

| Item | Decision |
| --- | --- |
| Default view | Last 30 days compared with the previous 30 days, all countries, all visa types |
| Date boundaries | `[from, toExclusive)` in UTC, until the data team confirms the Gold time zone |
| Freshness timestamp | Shown in UTC with the label "UTC" |
| Currency | The API never converts currencies. Revenue is totalled per currency |
| KPI status and thresholds | Thresholds live in API configuration (`KpiThresholds`). The API returns each KPI's status; React never computes it |
| Filter limits | Dates as `yyyy-MM-dd`; country, centre and visa-type codes are letters, digits, `-` and `_`, up to 20 characters, and must exist in the lookups; maximum date range 24 months |
| Data reload | On filter change or manual refresh. No automatic polling |
| Export | None (no CSV or PDF) |
| Screen widths | Designed for desktop and tablet. Phone widths are usable but not specially designed |

## 3. Data

- **Source:** the agreed KPI data will be delivered in the **Fabric Gold layer** and read through the **Fabric SQL analytics endpoint** (confirmed by Sheharyar, 12 Oct 2026). It is not available yet; until then the project runs on mock data (3.1).
- **Ownership:** the data team owns Fabric, Gold and refresh. This project **only reads**; it never changes Fabric or triggers ingestion.
- **Access:** parameterised, read-only `SELECT` queries (generated by EF Core from the services' LINQ) over approved reporting objects, through `Microsoft.Data.SqlClient` over encrypted TDS (port 1433).
- **KPI logic:** business rules and formulas belong to the data layer. The backend **only filters and aggregates** (counts, sums, ratios) exactly as the KPI Mapping defines. A KPI that needs business rules is delivered as a Gold view, not rebuilt in the API. Each KPI's source and aggregation is recorded in `docs/kpi-mapping.md`. **React never implements KPI formulas.** If formulas turn out to live in a semantic model, the data-access approach is reassessed (open question).
- **Responses carry:** KPI values, status, supported breakdowns, units and currency, the applied filter, `generatedAtUtc`, `dataSource` and data-freshness metadata.
- **Aggregation is server-side;** whole source tables are never sent to the browser.
- **No cache.** Performance is measured first (section 5).

### 3.1 Mock data (until the real Gold layer exists)

The Fabric Gold layer is **not available yet**. Development, demos and automated tests run on mock data shaped like the expected Gold objects. Switching to Fabric later then needs only a configuration change and a mapping review, not a rewrite.

**How it works** (decided 12 Oct 2026: JSON mock files in the API, no mock database)

| Layer | Mock approach | Switch to live |
| --- | --- | --- |
| Mock files | Gold-shaped rows as JSON, one file per Gold object (`dim_country.json`, `fact_visa_application.json.gz`, ...; the three fact files are gzip-compressed), embedded in `backend/src/Vas.Infrastructure/MockData/`. A fixed-seed generator in `backend/mock-data/generator/` writes them | Not used |
| Backend | `IReportingDbContext` exposes one `IQueryable<T>` per Gold object. Services write their KPI queries once in LINQ. With `DataSource=Mock`, `JsonReportingSource` serves the JSON rows in memory through an async-capable query provider, so the same async LINQ runs on both sources; with `DataSource=Fabric`, the EF Core `ReportingDbContext` serves the same `IQueryable<T>` from the SQL endpoint | Set `DataSource=Fabric` and the Fabric connection; adjust the `ToView`/column mappings in `Infrastructure/Persistence/Configurations/` to the real Gold names |
| Frontend | No mock mode (decision 28). The frontend always signs in with Entra ID and calls the API; the mock data it shows comes from the API running with `DataSource=Mock`. Fixtures exported from the real backend live in `frontend/tests/fixtures/` for the tests only | Nothing to switch in the frontend |
| Tests | Unit and application tests run every service against the JSON source | Add SQL translation tests against the real endpoint once Fabric is available |

**What this does and doesn't prove:** the KPI logic (the LINQ in each service) runs on mock data from day one. The SQL that EF Core generates from it, and its performance on Fabric, are only exercised once the Gold layer is available. In-memory LINQ can also differ from SQL on nulls, text casing and division, so passing mock tests do not prove Fabric results. That is milestone M2 in section 8, and the first thing to test when Fabric arrives.

**Expected Gold shape (assumed; replace when the KPI Mapping and real Gold names arrive)**

| Object | Grain | Columns |
| --- | --- | --- |
| `gold.dim_country` | one row per country | `country_code`, `country_name`, `region` |
| `gold.dim_centre` | one row per centre | `centre_code`, `centre_name`, `country_code`, `city`, `daily_capacity` |
| `gold.dim_visa_type` | one row per visa type | `visa_type_code`, `visa_type_name`, `category` |
| `gold.fact_visa_application` | one row per application | `application_id`, `submitted_date`, `country_code`, `centre_code`, `visa_type_code`, `channel` (Digital / Centre), `current_stage`, `status` (Pending / Processed / Closed), `outcome` (Approved / Rejected / Withdrawn / null), `decision_date`, `turnaround_days` |
| `gold.fact_application_stage` | one row per application per stage | `application_id`, `stage`, `entered_at_utc`, `exited_at_utc` |
| `gold.fact_vas_revenue` | one row per VAS transaction | `transaction_id`, `transaction_date`, `country_code`, `centre_code`, `visa_type_code`, `service_name`, `amount`, `currency` |
| `gold.refresh_log` | one row per refresh | `refreshed_at_utc`, `status` |

**Mock data content:** about 18 months of data (around 20,000 applications), 6 countries, 15 centres and 8 visa types, generated with a fixed random seed so numbers repeat in tests and demos. Revenue uses a single currency, **AED**. KPI thresholds use clearly labelled placeholder values until AEGI confirms the real ones. The data includes seasonality, a mix of outcomes, a pending backlog, varied turnaround times, at least one centre near capacity, and a few days with no data so empty states can be tested.

**Rules**

- Mock data is synthetic: no real applicant data or PII, ever.
- The UI shows a visible "Mock data" badge whenever the API reports `dataSource: Mock`.
- The API refuses to start with `DataSource=Mock` in UAT or Production. **Dev may run `DataSource=Mock`**, including the deployed Dev App Service, so the dashboard can be demonstrated before Fabric exists.
- Mock and live data run the same backend queries.
- When the real Gold layer arrives, the assumed names above are replaced in one place (the EF configurations) and recorded in `docs/kpi-mapping.md`.

## 4. Identity and access

- **Executive sign-in:** Microsoft Entra ID, MSAL React, authorization code flow with PKCE. Frontend is a public SPA client; backend is a protected API with its own app registration and delegated permission.
- **Token cache in the browser:** MSAL `sessionStorage`. Never `localStorage`. Tokens are never logged.
- **API checks:** signature, issuer, audience, expiry, required scope and the dashboard access policy. Filters are validated before any data access. A client's country or centre selection is never treated as proof of permission.
- **Access policy:** only assigned executives, through one Entra app role, `VasDashboard.Viewer`. All authorised executives see every country and centre (planning assumption; data owner must confirm).
- **Tenants (decision 29):** no tenant ID has to be configured. By default the API accepts a token from any Entra organisation and the scope and role decide access; the role is assigned in each organisation by its own administrator. Setting `AzureAd__TenantId` and/or `AzureAd__AllowedTenantIds` limits sign-in to those tenants, and a token from any other tenant is refused (401). Sign-in only: every authorised user sees the same data, whatever their organisation.
- **No authentication bypass in the API,** in any environment. Until an Entra tenant and app registrations are available, the API is verified with test tokens in `Vas.ApiTests`; real sign-in (M1) is verified when the IDs arrive.
- **Backend to Fabric:** the App Service **system-assigned managed identity**. No Fabric client secret, no separate credential-based Fabric app registration.
- **Local development to Fabric:** the developer's own Entra account through `az login`. No service principal secret.
- **Two separate tokens:** the executive's token calls our API; the managed identity's token calls Fabric SQL. They are never interchanged.
- **Fabric grants:** item-level connection access and `SELECT` on approved objects in delegated identity access mode (if the endpoint uses user identity mode, OneLake security is defined instead).
- **Secrets:** **no Key Vault**; no application secrets identified. Tenant/client IDs, API scope, SQL endpoint and database are non-secret per-environment settings.
- **Rate limiting:** one per-user limiter on `/api`, configurable, starting at 120 requests a minute.
- **Access audit:** no audit table. Application Insights request logs carry the user's Entra object ID, path, status and duration; no names, tokens or bodies.

## 5. Hosting and operations

- **Hosting:** one custom **Linux container** on **Azure App Service**, one origin: ASP.NET Core serves the compiled React app and the API. No CORS.
- **Build:** multi-stage, cache-friendly Dockerfile, written in this project. No credentials embedded in the image.
- **Source control and CI/CD:** this repository on GitHub, GitHub Actions, OIDC federation to Azure (no deployment secret). CI builds, runs all tests, and runs `npm audit` and `dotnet list package --vulnerable`.
- **Registry:** approved registry; Azure Container Registry proposed, not selected. Managed-identity image pulls if ACR is chosen.
- **Environments:** Dev, UAT, Production with separate config and separate data access. The same image is promoted through all of them.
- **Release:** staging slot and controlled swap where the plan supports it; previous image kept for rollback.
- **Runtime:** health endpoint (`/healthz`), HTTPS, HSTS and security headers, startup readiness, Always On, stateless API.
- **Telemetry and logs:** Serilog to console and Application Insights for health, errors and latency. No rolling log files.
- **Caching:** none. Measure performance first; add caching only if measurements need it, and any cache must respect permissions and freshness.
- **Performance:** latency is measured and reported in phases 5 and 6. There is no pass or fail line until AEGI supplies targets.

## 6. Technology

| Area | Choice | Source |
| --- | --- | --- |
| Frontend | React + TypeScript strict (Vite SPA), React Router, TanStack Query, MSAL React | Roadmap + factory React guide |
| Frontend architecture | `app/` (thin routes), `features/` (screen + `useXxxViewModel`), `shared/`, `core/` | Factory React guide |
| Backend | ASP.NET Core API on .NET 10 | Roadmap + factory Monolith guide |
| Backend architecture | Layered monolith: Api, Application, Domain, Infrastructure, Shared | Factory Monolith guide |
| Data access | EF Core 10 read-only context over Fabric SQL; Dapper/SqlClient only if EF Core proves incompatible with the endpoint (decided in the M2 proof) | Roadmap + guide |
| Charts | **Recharts** (approved 12 Oct 2026). No second chart library | Decision |
| Languages | **English only** in Phase 1. Logical CSS properties so RTL can be added later | Decision |
| Theme | Semantic tokens in one file, **light and dark**, neutral professional palette until AEGI brand colours and logo are supplied | Decision |
| Tests | xUnit (backend); Vitest + Testing Library + Playwright (frontend) | Factory guides |

## 7. Out of scope (Phase 1)

- Operations, Finance and Alfit reporting (placeholders only)
- CSAT reporting
- Non-VAS data sources
- KPIs or breakdowns outside the agreed set or not supported by validated data
- Dashboard customisation by users or admins
- ODIN integration and voice features
- Data writes of any kind to Fabric
- Export (CSV, PDF) and automatic polling
- Arabic and RTL
- An audit table, user management, settings and uploads

## 8. Timeline, milestones and acceptance

| Date | Event |
| --- | --- |
| 13 Oct 2026 | AEGI signs off scope, KPIs, definitions, breakdowns and designs |
| 23 Oct 2026 | UAT completion target |
| 30 Oct 2026 | Production release target |

| Milestone | Demonstrable result |
| --- | --- |
| M1 Access path | Assigned executive signs in and calls a protected API; unauthorised caller rejected |
| M2 Fabric proved | Deployed App Service reads an approved KPI from the real SQL endpoint with managed identity, no secret; an unapproved object is denied |
| M3 Vertical slice | React shows that KPI for an agreed filter with verified values |
| M4 Phase 1 complete | Executive Summary and VAS match signed-off scope; future areas are placeholders |
| M5 Reporting accepted | AEGI accepts KPI accuracy, design, access and performance through UAT |
| M6 Production operational | Approved image, production identity and grants, sizing, rollback, monitoring, support ownership verified |

**Acceptance:** both active areas match approved designs and requirements; KPIs, breakdowns, filters and visuals validated against VAS data; placeholders in place; testing and UAT completed; formal AEGI acceptance. UAT acceptance is not automatic production approval.

### 8.1 Delivery phases

One phase is built at a time. After each phase: changed files, tests run, and gaps are reported.

| Phase | Work | Acceptance check |
| --- | --- | --- |
| 0 Setup | Solution, five projects, three test projects, `frontend/` scaffold, `frontend/PROJECT_LOCK.md`, Dockerfile, compose, GitHub Actions, `docs/`, rate limiter, sanitiser stubs | `docker compose up --build` serves the shell; `/healthz` returns 200; `dotnet test` and `npm test` pass; production web build succeeds |
| 1 Entra auth | Token validation, permission policy over the app role, `/auth/me`, `/auth/me/permissions`, `/auth/me/menu`, MSAL guard, 403 and 404 pages | No token gets 401; a signed-in user without the role gets 403; an assigned user sees the shell (M1 once Entra IDs exist) |
| 2 Data layer | Mock generator, JSON source, `IReportingDbContext`, `KpiThresholds`, Executive Summary slice end to end, then the six VAS services and lookups | Every service has unit and application tests on the JSON source; responses report `dataSource`; Mock is refused in UAT and Production |
| 3 Dashboard UI | Design mockup approved first; then tokens, shared components, filter bar in the URL, Executive Summary, six VAS sections, Coming Soon, Mock badge | Every Phase 1 KPI renders with filters at desktop and tablet widths; loading, empty and error states work |
| 4 Testing | API tests, component tests, Playwright path, authorisation negative tests, security headers, dependency scans, accessibility pass, requirement-to-test traceability table | All three test levels green on both sides; no `console.*` in app code |
| 5 Fabric cutover (gated on the data team) | Real Gold mappings, managed identity, SQL translation tests, measurements | M2 and M3 |
| 6 UAT and production | UAT deployment, reconciliation with data-owner samples, load measurements, runbook, rollback, slot swap | M4, M5, M6 and formal AEGI acceptance |

### 8.2 Risks to track

- **UAT needs real data.** Mock is refused in UAT, so the 23 Oct target depends on the Gold layer, its grants and M2 being done before then.
- **Design sign-off.** AEGI's 13 Oct sign-off and the acceptance criteria refer to "approved designs". Claude's designs must be shown to AEGI, or acceptance has nothing to measure against.
- **Assumed Gold shape.** Every KPI query is written against assumed names and grain until the KPI Mapping arrives.

## 9. Where these specs override the factory guidelines

| Guideline rule | Project spec | Reason |
| --- | --- | --- |
| Monolith s.10: own JWT login, refresh tokens, BCrypt | Entra ID sign-in; API only validates Entra tokens | Section 4 |
| Monolith s.11: roles and screen permissions in the database | Entra app-role assignment checked by the API's permission policy. A role change applies at the user's next token | No app database |
| Monolith s.2, s.16: one owned SQL Server with migrations | Read-only access to Fabric Gold; no migrations, seeding, soft delete or audit tables | Section 3 |
| Monolith s.4: context exposes `DbSet<T>`, `Database`, `Entry`, `SaveChangesAsync` | `IReportingDbContext` exposes `IQueryable<T>` only | Read-only; two interchangeable sources (3.1) |
| Monolith s.16: `TransactionFilter` on non-GET requests | Not built; no `Filters/` folder | The API never writes |
| Monolith s.12, 13, 19, 21: users, settings, uploads, audit | Not built. Access is traced by request logs in Application Insights | Out of scope; no writes |
| Monolith s.9: rolling file logs in production | Console plus Application Insights | Stateless container (section 5) |
| Monolith s.25: secrets from a secret manager | No secrets and no Key Vault | Section 4 |
| Monolith s.25: CORS with exact origins; rate limiting on login endpoints | No CORS (one origin); one per-user limiter on `/api` | Sections 4 and 5 |
| Guide default: add caching | No cache until measurements justify it | Section 5 |
| Monolith s.27: application tests against a real SQL Server | Application tests run against the JSON mock source; SQL tests added when Fabric is available | Section 3.1 |
| Web guide s.2: refresh token in httpOnly cookie | MSAL silent token renewal | Entra SPA flow |
| Security guide s.6: access token in memory | MSAL `sessionStorage` cache | Memory forces a new sign-in round trip on every page reload |
| Security guide: config only from build env | API base URL from env; Entra IDs served at runtime by `/config.json` | One image for every environment |
| Frontend OS s.1, s.3: UI comes from Figma and is never invented | Claude designs the screens; Sheharyar approves them | No Figma exists (explicit instruction, 12 Oct 2026) |
| Frontend OS s.3 rule 8 and WebApps guide s.2: no animation package without approval | `motion` for transitions and `three` for one 3D globe | Explicit instruction: "3D and animated" (12 Oct 2026) |
| Frontend OS s.3 rule 1: no code before the lock is confirmed | The screens were built straight from the Q&A decisions; `frontend/PROJECT_LOCK.md` records them | Explicit instruction to create the frontend |
| Implementation Guide: frontend in `web/` | `frontend/` | Explicit instruction |
| Frontend OS s.4: EN + AR with RTL recommended for MENA products | English only; logical CSS for a later RTL | Section 6 |
| Security guide s.4: sanitisation rules come from the BRD | The requirements give none; the filter limits in section 2.1 apply | Decision, 12 Oct 2026 |
| QA baseline s.5, QAB-001: lock a QA profile | No profile fits a non-AI dashboard. Exception: apply only the non-AI parts (baseline traceability and release gates, functional testing, API contracts, authorisation and privacy tests, performance testing). Owner and approver: Sheharyar | The QA profiles are for AI systems |
| Local dev with a service principal secret | Developers sign in with their own Entra account (`az login`) for Fabric reads; no client secret is ever added | Spec forbids Fabric secrets |

**Where these specs override the Implementation Guide (12 Oct 2026)**

| Implementation Guide | Project spec |
| --- | --- |
| "Start from the starter in `vas-dashboard/`"; move its `/healthz`, `/config.json`, SPA fallback; replace `FabricSqlConnectionFactory` | New build; everything is created here |
| Short in-memory cache keyed by filter; `Caching/`; `ReportingCacheOptions` | No cache |
| Application tests on SQL Server in Testcontainers | Application tests on the JSON mock source |
| Dev service principal in `.env` | Developer `az login` |
| `IReportingDbContext` exposes `DbSet` | `IQueryable<T>` |
| Dockerfile owned by a separate Docker setup thread | Written in this project |
| Design system "from Figma" | Claude's approved designs |
| Access token "in memory" | MSAL `sessionStorage` |

## 10. Open questions

| # | Question | Owner |
| --- | --- | --- |
| 1 | Gold view/table names and columns for each agreed KPI; KPI Mapping document (mock shape in 3.1 is assumed until then) | Data team |
| 2 | Fabric workspace, SQL analytics endpoint and database; Lakehouse or Warehouse | Data team |
| 3 | Are KPI formulas in Gold tables/views or in a semantic model | Data team |
| 4 | Gold refresh schedule, time zone, and a last-refresh timestamp for the dashboard (UTC assumed until then) | Data team (Jaffery) |
| 5 | Confirm that all executives see every country and centre (assumed yes) | AEGI data owner |
| 6 | PII, masking and data security requirements | AEGI |
| 10 | Users, peak concurrency, response-time targets, availability | AEGI / product |
| 11 | Container registry, region, App Service plans, network policy (public or private) | AEGI cloud team |
| 14 | Outstanding DXC and other source-system dependencies | AEGI |
| 15 | Entra tenant, the two app registrations, and who may create them | Sheharyar / AEGI identity team |
| 16 | Real KPI status thresholds (placeholders until then) | AEGI |
| 17 | AEGI logo and brand colours (neutral palette until then) | AEGI |
| 18 | Who presents Claude's designs to AEGI for the design sign-off | Sheharyar |

Closed on 12 Oct 2026: 7 (no Figma; own designs), 8 (English only), 9 (Recharts), 12 (GitHub + GitHub Actions), 13 (`az login`).

## 11. Decision log (Q&A, 12 Oct 2026)

| # | Decision |
| --- | --- |
| 1 | GitHub repository, GitHub Actions, OIDC federation to Azure |
| 2 | New build in this repository; `VAS-demo-project` is not used |
| 3 | Local Fabric identity is developer `az login` |
| 4 | Recharts |
| 5 | English only; RTL-ready CSS |
| 6 | No Figma; Claude designs, Sheharyar approves, before screens are built |
| 7 | One `VasDashboard.Viewer` role; all countries and centres |
| 8 | Backend only filters and aggregates; business-rule KPIs come as Gold views |
| 9 | Neutral palette in tokens until AEGI brand is supplied |
| 10 | Light and dark themes |
| 11 | MSAL `sessionStorage` cache |
| 12 | Per-user rate limiter on `/api`, 120 requests a minute, configurable |
| 13 | No audit table; Application Insights request logs |
| 14 | QA exception: non-AI parts of the QA guidebooks only |
| 15 | Filter limits as in section 2.1 |
| 16 | Thresholds in API configuration; API returns status; placeholders in mock |
| 17 | Performance measured and reported; no pass or fail line until AEGI gives targets |
| 18 | Data-team and AEGI items stay open; work proceeds on mock |
| 19 | Default view: last 30 days against the previous 30 days |
| 20 | UTC date boundaries and UTC freshness label |
| 21 | No currency conversion; totals per currency; mock uses AED |
| 22 | Without an Entra tenant, Phase 1 is verified with test tokens; M1 when IDs arrive |
| 23 | ~~Fake signed-in user in frontend mock mode only~~; no API bypass. Replaced by decision 28 |
| 24 | Dev App Service may run `DataSource=Mock` for demos |
| 25 | No export, no automatic polling, phone widths not specially designed |
| 26 | This file lives at the repository root; no commits or pushes unless asked |
| 27 | Backend code lives in a `backend/` folder, not at the repository root |
| 28 | 13 Oct 2026: the frontend has no mock mode and no fake user. Every run signs in with Entra ID and calls the API; mock data comes only from the API in Development (`DataSource=Mock`). Local development uses Dev app registrations in the LogicEra tenant, with the IDs supplied at run time from a git-ignored `.env` |
| 29 | 13 Oct 2026: multi-tenant sign-in with no tenant ID configured. Any Entra organisation whose user signs in and holds `VasDashboard.Viewer` is accepted; `AzureAd__TenantId` and `AzureAd__AllowedTenantIds` are optional and limit sign-in to the tenants they name. Sign-in only: no per-tenant data separation. `/config.json` no longer carries a tenant ID |
| 30 | 13 Oct 2026: the app opens on a home screen instead of the Executive Summary. It shows a Dubai skyline backdrop and the reporting areas on a 3D carousel; a click brings a card to the front and the front card opens its area. The skyline does not move with the pointer. The sidebar is removed: every page has a top bar with an "All areas" button back to the home screen. The skyline is a drawn illustration built into the app, not a photograph |

**Made by Claude during implementation, 12 Oct 2026. Not yet confirmed by Sheharyar.**

| # | Decision | Why |
| --- | --- | --- |
| A | The default 30-day view ends at the day of the last successful Gold refresh, not at today's date. If there is no refresh record it ends today | A nightly refresh means today has no data; mock data has a fixed end date and would otherwise show an empty default view |
| B | The applications trend is by day for ranges up to 62 days and by month beyond that | A 24-month range by day is unreadable |
| C | `DataSource=Mock` is accepted only when the environment is `Development`. A deployed Dev App Service that should demo on mock data must set `ASPNETCORE_ENVIRONMENT=Development` | An allow-list is safer than naming the environments to refuse |
| D | The Executive Summary KPI set and definitions in `docs/kpi-mapping.md` are provisional | No KPI Mapping document yet |
| E | Unknown `/api` routes return 401 to callers without a token, and 404 only to signed-in callers | Anonymous callers cannot probe which routes exist |
| F | The three large mock fact files are stored gzip-compressed (`fact_*.json.gz`); the dimension files and the refresh log stay plain JSON. The folder is 0.9 MB instead of 24 MB | Section 3.1 still holds: one JSON file per Gold object, embedded in the API. Only the storage of three of them changed |
| G | The six VAS reports use provisional definitions too (`docs/kpi-mapping.md`). Time spent per stage is not reported | No KPI Mapping yet; a stage duration needs a Gold column, because date arithmetic cannot run from one query on both sources |
| H | The centres table is aggregated per centre in the database, then sorted and paged in the API | Capacity use is calculated from the aggregate and the centre dimension; the row count is the number of centres |
| J | Every report accepts `lastDays` (1 to 730) as an alternative to a date range: that many complete days ending at the last data refresh | The frontend's period presets need the same anchor as the default view; a browser cannot know the refresh date before its first request |
| K | The frontend lives in `frontend/`, not `web/` (Sheharyar's instruction) | |
| L | The design has depth and motion (Sheharyar's instruction: "3D and animated"): pointer-tilt KPI cards, a rotating 3D globe of the top countries, animated numbers and transitions. All of it switches off under `prefers-reduced-motion`. Adds `motion` and `three`; three.js is downloaded only with the globe panel | Explicit instruction. Recorded in `frontend/PROJECT_LOCK.md` |
| M | Light is the default theme; dark is available from the toggle (Sheharyar's preference) | |
| N | Withdrawn by decision 28: there is no frontend mock mode. The fixed export of the backend's output is now test data only (`frontend/tests/fixtures/`) | |
| O | Withdrawn by decision 28: `docker compose` builds the same image as every environment; it signs in with Entra ID and needs the Entra IDs in `.env` | |
| P | The VAS page shows its six areas as tabs and requests only the open one | Six reports at once is six requests per filter change |
| Q | Icons are `lucide-react`; the font is Inter, bundled with the app | No request to an external font or icon host |
| I | Every `/api/v1/vas/...` report needs the `VAS` screen permission; the lookups and `/auth/me` need the `DASHBOARD` permission. The Viewer role has both | One role today, but the screens stay separable |
| – | The Implementation Guide ranks below this file |
