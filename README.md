# AEGI VAS Executive Dashboard

A React dashboard and an ASP.NET Core reporting API, shipped as one container on Azure App Service. Executives sign in with Microsoft Entra ID and see VAS KPIs read from the Microsoft Fabric Gold layer. The application only reads from Fabric.

[project-specs.md](project-specs.md) is the authority for scope and decisions. Read it before changing anything.

## Status

| Part | State |
| --- | --- |
| Backend scaffold, pipeline, health | Done |
| Entra ID token validation and permissions | Done against test tokens; real sign-in waits for the tenant ([docs/entra-setup.md](docs/entra-setup.md)) |
| Mock data layer | Done: Gold-shaped JSON files, served through the same queries Fabric will run |
| Executive Summary endpoint | Done on mock data, with provisional KPI definitions ([docs/kpi-mapping.md](docs/kpi-mapping.md)) |
| VAS report endpoints and lookups | Done on mock data: applications, outcomes, processing, revenue, visa types, centres |
| Container image | Builds and runs; `/healthz` verified inside it |
| React frontend (`web/`) | Not started |
| Fabric connection | Waits for the Gold layer |

## Layout

```
backend/                 everything .NET
  VasDashboard.slnx
  Directory.Build.props      .NET 10, nullable, warnings as errors
  Directory.Packages.props   every package version, in one place
  src/
    Vas.Api/                 controllers, middleware, startup
    Vas.Application/         features (DTOs, validators, services), options, exceptions
    Vas.Domain/              read models and enums
    Vas.Infrastructure/      identity, authorization, data access
    Vas.Shared/              response envelope, constants
  tests/
    Vas.UnitTests/           pure logic
    Vas.ApplicationTests/    services
    Vas.ApiTests/            the running API: auth, envelopes, status codes
  mock-data/generator/       writes the mock JSON files from a fixed seed
docs/                    setup guides and the KPI mapping
Dockerfile               the one image
docker-compose.yml       run that image locally
.github/workflows/ci.yml build, test, vulnerability check, image build
```

## Prerequisites

- .NET SDK 10
- Docker Desktop, only if you want to run the container

## Run the API

```bash
dotnet run --project backend/src/Vas.Api
```

It listens on `http://localhost:5080`.

| URL | What you get |
| --- | --- |
| `/healthz` | `Healthy` |
| `/config.json` | The sign-in settings the React app loads at start-up |
| `/openapi/v1.json` | The API description (Development only) |
| `/api/v1/auth/me` | 401 until you send a valid Entra token |
| `/api/v1/executive-summary` | The Executive Summary report; same token needed |
| `/api/v1/vas/applications`, `/outcomes`, `/processing`, `/revenue`, `/visa-types`, `/centres` | The six VAS reports |
| `/api/v1/lookups/countries`, `/centres`, `/visa-types` | The choices for the filter dropdowns |

Every report accepts `from`, `toExclusive` (dates as `yyyy-MM-dd`), `countryCode`, `centreCode`, `visaTypeCode` and `compare` (`PreviousPeriod` or `None`). The centres report also accepts `page`, `pageSize`, `sortBy` and `sortDirection`.

## Mock data

Until the Fabric Gold layer exists, the API reads Gold-shaped JSON files embedded from `backend/src/Vas.Infrastructure/MockData/`: 20,742 applications over 18 months, 6 countries, 15 centres, 8 visa types. The data is synthetic. The three large fact files are gzip-compressed (`.json.gz`), which keeps the folder under 1 MB; inside they are plain JSON, one row per line.

The files are generated, so do not edit them by hand. To change the data, change the generator and run:

```bash
dotnet run --project backend/mock-data/generator -- backend/src/Vas.Infrastructure/MockData
```

A test fails if the files and the generator disagree.

`Reporting__DataSource` chooses the source. `Mock` is accepted only when `ASPNETCORE_ENVIRONMENT` is `Development`; the API refuses to start with it anywhere else. Every report response says which source it came from in `meta.dataSource`.

## Test

```bash
dotnet test backend/VasDashboard.slnx
```

The API tests start the real application in memory. They replace only the token issuer and signing key, so audience, expiry, scope and role checks run as they do in production.

## Run the container

```bash
docker compose up --build
```

It serves on `http://localhost:8080`. Copy `.env.example` to `.env` to set values; without it the container starts in Development with placeholder Entra IDs.

**Start it with compose, not with the Run button in Docker Desktop or a bare `docker run`.** Started on its own, the container has no settings, so it runs as Production and stops at once with a message naming what is missing (the Fabric endpoint and the Entra IDs). That is the startup guard working, not a fault. Compose sets `ASPNETCORE_ENVIRONMENT=Development` and maps port 8080. The equivalent by hand is:

```bash
docker run --rm -p 8080:8080 -e ASPNETCORE_ENVIRONMENT=Development vas-dashboard:local
```

Until the React app is built into the image, `http://localhost:8080/` shows a one-line message saying the API is running. To see why a container stopped, run `docker compose logs` (or `docker logs <name>`).

## Configuration

Settings come from `backend/src/Vas.Api/appsettings.json` and are overridden by environment variables (use `__` between levels). None of them are secrets, and the project has no Key Vault.

| Setting | Purpose | Default |
| --- | --- | --- |
| `AzureAd__TenantId` | Entra tenant | All-zero placeholder |
| `AzureAd__ClientId` | The API app registration | All-zero placeholder |
| `AzureAd__SpaClientId` | The React app registration | All-zero placeholder |
| `AzureAd__RequiredScope` | Scope the token must carry | `Dashboard.Read` |
| `Reporting__DataSource` | `Mock` or `Fabric` | `Fabric` (`Mock` in Development) |
| `Fabric__SqlEndpoint` | Host name of the SQL analytics endpoint | Empty; required for `Fabric` |
| `Fabric__Database` | Database to connect to | Empty; required for `Fabric` |
| `Fabric__CommandTimeoutSeconds` | Query timeout | `30` |
| `KpiThresholds__...` | Status thresholds per KPI | Placeholders |
| `RateLimiting__PermitLimit` | Requests per user per window | `120` |
| `RateLimiting__WindowSeconds` | Window length | `60` |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Turns telemetry on | Unset (off) |
| `ASPNETCORE_FORWARDEDHEADERS_ENABLED` | Trust the proxy's scheme and client address | `true` in the image |

Outside Development the API refuses to start while an Entra ID is still the placeholder, or while a setting is invalid.

## Conventions

- Every response under `/api` uses one envelope: `success`, `message`, `data`, `errors`, `traceId`.
- Every endpoint needs a signed-in user unless it is marked anonymous. Controllers add `[RequirePermission(screen, action)]`.
- What each role may do is defined in one file: `backend/src/Vas.Application/Features/Auth/Rules/RolePermissionMap.cs`.
- Each response returns an `X-Correlation-Id` header; the same value is the `traceId` in error bodies and in the logs.
- Logs carry the user's Entra object ID. They never carry names, tokens or request bodies.
- HTTPS is enforced by App Service ("HTTPS only"). The application adds HSTS and the security headers.
