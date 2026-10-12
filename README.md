# AEGI VAS Executive Dashboard

A React dashboard and an ASP.NET Core reporting API, shipped as one container on Azure App Service. Executives sign in with Microsoft Entra ID and see VAS KPIs read from the Microsoft Fabric Gold layer. The application only reads from Fabric.

[project-specs.md](project-specs.md) is the authority for scope and decisions. Read it before changing anything.

## Status

| Part | State |
| --- | --- |
| Backend scaffold, pipeline, health | Done |
| Entra ID token validation and permissions | Done against test tokens; real sign-in waits for the tenant ([docs/entra-setup.md](docs/entra-setup.md)) |
| Mock data layer and KPI endpoints | Not started |
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
docs/                    setup guides
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

## Configuration

Settings come from `backend/src/Vas.Api/appsettings.json` and are overridden by environment variables (use `__` between levels). None of them are secrets, and the project has no Key Vault.

| Setting | Purpose | Default |
| --- | --- | --- |
| `AzureAd__TenantId` | Entra tenant | All-zero placeholder |
| `AzureAd__ClientId` | The API app registration | All-zero placeholder |
| `AzureAd__SpaClientId` | The React app registration | All-zero placeholder |
| `AzureAd__RequiredScope` | Scope the token must carry | `Dashboard.Read` |
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
