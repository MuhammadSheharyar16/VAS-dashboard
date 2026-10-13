# Entra ID setup

How sign-in works in the VAS dashboard, what has to be created in Microsoft Entra ID, and how its values reach the application. Do the setup once per environment (Dev, UAT, Production).

Nothing here is a secret. There is no client secret, certificate or password anywhere in this project.

**What has been tried:** single-tenant sign-in has been run with the Dev registrations in the LogicEra tenant. The multi-organisation steps in section 8 and the Azure CLI token steps in section 7 have not been run against real tenants yet, so treat those two as a starting point.

## 1. The idea in one minute

- The **React app** never checks a password. It sends the user to Microsoft to sign in and gets back an *access token*.
- The **API** never talks to the user about passwords either. It only checks the token on every request.
- **Entra ID** decides who the user is. **You** decide who may use the dashboard, by assigning a role.

Two things must be true for a request to succeed: the token is genuine and meant for this API (otherwise **401**), and the user holds the role (otherwise **403**).

## 2. How a sign-in flows

```text
 Browser (React app)                     API (App Service)                  Microsoft Entra ID
        |                                       |                                    |
  1.    |--- GET /config.json ----------------->|                                    |
        |<-- SPA client ID, authority, scope ---|                                    |
        |                                       |                                    |
  2.    |--- redirect: sign in, asking for the scope Dashboard.Read ---------------->|
        |                                       |          user signs in (and MFA)   |
  3.    |<-- redirect back to the app with a one-time code --------------------------|
  4.    |--- code + PKCE proof ----------------------------------------------------->|
        |<-- access token (kept in the browser tab's sessionStorage) ----------------|
        |                                       |                                    |
  5.    |--- GET /api/v1/... + Bearer token --->|                                    |
  6.    |                                       |--- fetch signing keys (cached) --->|
        |                                       |    check signature, issuer,        |
        |                                       |    audience, expiry, tenant        |
  7.    |                                       |    check scope and role            |
        |<-- 200 data, or 401, or 403 ----------|                                    |
```

| Step | What happens | Where in the code |
| --- | --- | --- |
| 1 | The app loads its sign-in settings at start-up, so one build works in every environment | `frontend/src/core/config/runtimeConfig.ts`, `backend/src/Vas.Api/Startup/SpaExtensions.cs` |
| 2 to 4 | Authorization code flow with PKCE, by redirect. No secret is involved | `frontend/src/core/auth/LiveAuthProvider.tsx` |
| 5 | Every API call carries the token. On a 401 the app asks for a fresh token once and retries once | `frontend/src/core/http/apiClient.ts` |
| 6 | Token validation | `backend/src/Vas.Api/Startup/AuthenticationExtensions.cs` |
| 7 | Scope and role | `backend/src/Vas.Infrastructure/Authorization/PermissionAuthorizationHandler.cs` |

The token is renewed silently while the user works. A role change takes effect at the user's next token: at most about an hour later, or at once after signing out and in.

## 3. What you need before you start

| You need | Why |
| --- | --- |
| An Entra tenant | It holds the registrations and the users |
| The role *Application Administrator* or *Cloud Application Administrator* in it | To create app registrations and grant admin consent |
| The address the app will run at | It becomes the redirect URI. Locally: `http://localhost:5173` (dev server) and `http://localhost:8080` (container). Deployed: `https://<app-service-host>` |
| The list of executives, or a security group that holds them | They get the role |

## 4. What you create in Entra ID

| Item | Purpose |
| --- | --- |
| **API** app registration | Stands for the protected API. Owns the scope `Dashboard.Read` and the role `VasDashboard.Viewer` |
| **SPA** app registration | Stands for the React app. A public client: no secret |
| Role assignment | Says which executives may use the dashboard |

### 4.1 API app registration

1. **App registrations > New registration.** Name it `AEGI VAS Dashboard API (<env>)`. Supported account types: *Accounts in this organizational directory only*. (Choose *Multitenant* only if section 8 applies.) No redirect URI.
2. **Expose an API > Add** next to *Application ID URI*. Accept the default `api://<api-client-id>`.
3. **Expose an API > Add a scope.**
   - Scope name: `Dashboard.Read`
   - Who can consent: *Admins only*
   - Display name and description: "Read the VAS dashboard"
   - State: *Enabled*
4. **App roles > Create app role.**
   - Display name: `VAS Dashboard Viewer`
   - Allowed member types: *Users/Groups*
   - Value: `VasDashboard.Viewer` (must match exactly; the API compares it case-sensitively)
   - Enable the role.
5. **Manifest:** set `"requestedAccessTokenVersion": 2` (under `api`) so the API receives v2 tokens.
6. Note the **Application (client) ID** and the **Directory (tenant) ID**.

### 4.2 SPA app registration

1. **New registration.** Name it `AEGI VAS Dashboard (<env>)`. Same account type as the API registration.
2. **Authentication > Add a platform > Single-page application.** Add the redirect URIs for this environment (section 3). The platform must be *Single-page application*, not *Web*. Leave both *implicit grant* boxes unticked.
3. **API permissions > Add a permission > My APIs >** the API registration **> Delegated permissions >** `Dashboard.Read`.
4. **Grant admin consent** for the tenant.
5. Note the **Application (client) ID**.

### 4.3 Assign the executives

1. **Enterprise applications >** open the **API** application (not the SPA).
2. **Properties > Assignment required?** set to **Yes**. Without it any user in the tenant can obtain a token. They would still be refused by the API (403), but this keeps the sign-in itself closed.
3. **Users and groups > Add user/group.** Pick the executives, or a security group that holds them, and the role `VAS Dashboard Viewer`.

## 5. Give the values to the application

The API reads them as settings. The React app has none built in: it gets them from `/config.json`, which the API serves.

| Setting | Value | Needed? |
| --- | --- | --- |
| `AzureAd__ClientId` | Client ID of the **API** registration | Always |
| `AzureAd__SpaClientId` | Client ID of the **SPA** registration | Always |
| `AzureAd__TenantId` | Directory (tenant) ID. Limits sign-in to this tenant | Recommended wherever real data is shown (section 8) |
| `AzureAd__AllowedTenantIds` | More tenant IDs that may sign in, comma-separated | Only for several organisations |
| `AzureAd__RequiredScope` | `Dashboard.Read` | Only if you named the scope differently |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` | Only for a national cloud |

Where to put them:

| Where the app runs | Where the settings go |
| --- | --- |
| Azure App Service | **Settings > Environment variables > App settings**, one entry per row above. Then restart |
| Local, API by itself | A root `.env` (copy `.env.example`), started with `./run-dev.ps1` |
| Local, the container | The same root `.env`; `docker compose up --build` reads it |

Outside Development the API refuses to start while either client ID is still the all-zero placeholder, and in every environment it refuses to start when `AzureAd__AllowedTenantIds` holds anything that is not a tenant ID. The start-up log names the setting that is wrong.

## 6. Deploying on Azure App Service

1. Deploy the container and note its address, `https://<app-service-host>`.
2. Add that address to the **SPA** registration's redirect URIs (section 4.2, step 2). It must match exactly: `https`, no path, no trailing slash.
3. Add the app settings from section 5 and restart.
4. Keep **HTTPS only** on. Entra ID accepts `http` redirect URIs only for `localhost`.
5. Run the checks in section 7.

The App Service's *managed identity* is not part of sign-in. It is used only for the connection to Fabric ([fabric-setup.md](fabric-setup.md)).

## 7. Check it (milestone M1)

1. Open `/config.json`. It shows the SPA client ID, the authority and `api://<api-client-id>/Dashboard.Read`. The authority ends in the tenant ID when sign-in is limited to one tenant, and in `/organizations` otherwise.
2. Call `/api/v1/auth/me` with no token. Expect **401**.
3. Sign in as a user who is **not** assigned the role. Expect the "You do not have access" page (the API answered **403**).
4. Sign in as an assigned executive. Expect the home screen with their name.

To get a token by hand, without the React app, you can temporarily let the Azure CLI request one:

1. On the **API** registration: **Expose an API > Add a client application**, client ID `04b07795-8ddb-461a-bbee-02f9e1bf7b46` (the Azure CLI), and tick the `Dashboard.Read` scope.
2. Request the token:

   ```bash
   az login --tenant <tenant-id>
   az account get-access-token --scope api://<api-client-id>/Dashboard.Read --query accessToken -o tsv
   ```

3. Remove the Azure CLI client application again when you are done. Never do this in Production.

## 8. Which organisations can sign in

The application takes the organisation from the token itself. The tenant settings only narrow that down.

| `AzureAd__TenantId` and `AzureAd__AllowedTenantIds` | Who can sign in |
| --- | --- |
| One tenant ID between them | Users of that tenant only. **The normal choice** |
| Several tenant IDs | Users of those tenants only; a token from any other tenant gets **401** |
| Both unset | A user of **any** Entra organisation whose administrator has consented to the application and assigned the user the role |

Every signed-in user sees the same data; organisations are not separated from each other in the reports.

**With both settings unset, the role is the only gate, and each organisation's own administrator assigns it.** Both registrations are then multitenant and their client IDs are public (`/config.json`), so the administrator of any Entra tenant, including one created for the purpose, can consent to the application, assign themselves `VAS Dashboard Viewer` and read the dashboard. Use that mode only where it is acceptable, for example Development on mock data. Wherever the dashboard shows real data, list the tenants that are meant to have access.

To let other organisations in:

**In the tenant that owns the registrations, once:**

1. On **both** registrations: **Authentication > Supported account types >** *Accounts in any organizational directory (Multitenant)*. With the single-tenant type Entra ID refuses other organisations at the sign-in page, whatever the application settings say. Do this before changing the settings: a single-tenant registration cannot sign anyone in through the multi-organisation address.
2. Either list the tenants that may sign in (`AzureAd__TenantId` plus `AzureAd__AllowedTenantIds`, including your own), or leave both unset.
3. Assign your own executives as in section 4.3.

**In each other organisation, by one of its administrators:**

1. Grant admin consent to the API, then to the SPA, by opening each address and accepting:

   ```text
   https://login.microsoftonline.com/<their-tenant-id>/adminconsent?client_id=<api-client-id>
   https://login.microsoftonline.com/<their-tenant-id>/adminconsent?client_id=<spa-client-id>
   ```

   This creates the two enterprise applications in their tenant.
2. **Enterprise applications >** the **API** application **> Properties > Assignment required?** set to **Yes**.
3. **Users and groups > Add user/group.** Assign their executives the role `VAS Dashboard Viewer`. Role assignments are made per tenant; an assignment in one tenant does not cover users of another.

Check it as in section 7 with a user of the other organisation: the home screen with the role, the no-access page without it, and 401 for a tenant that is not listed.

## 9. What the API checks on every request

- The token's signature, issuer, audience and expiry (30 seconds of clock skew allowed).
- The token's tenant, when `AzureAd__TenantId` or `AzureAd__AllowedTenantIds` is set. When neither is set, the issuer only has to be an Entra organisation.
- The `Dashboard.Read` scope.
- A role that grants the screen being requested. The mapping lives in one file: `backend/src/Vas.Application/Features/Auth/Rules/RolePermissionMap.cs`.

The executive's token is only ever used to call this API. It is never passed on to Fabric.

## 10. When something goes wrong

| What you see | Likely cause | Fix |
| --- | --- | --- |
| Microsoft page: `AADSTS50011` redirect URI mismatch | The address in the browser is not among the SPA's redirect URIs | Add it exactly (section 4.2) |
| Microsoft page: `AADSTS9002326` cross-origin token redemption | The redirect URI was added under *Web*, not *Single-page application* | Move it to the SPA platform |
| Microsoft page: `AADSTS65001` consent required | Admin consent was not granted for `Dashboard.Read` | Section 4.2, step 4 |
| Microsoft page: `AADSTS50105` user not assigned | *Assignment required* is on and the user has no role | Section 4.3 |
| Microsoft page: `AADSTS50194` not configured as multi-tenant | The tenant settings are unset but the registrations are single-tenant | Set `AzureAd__TenantId`, or make both registrations multitenant |
| "Sign-in could not be started" in the app | `/config.json` could not be loaded or is incomplete, or the browser is showing an old copy of the page | Open `/config.json`; then reload with Ctrl+Shift+R |
| "You do not have access" in the app | Signed in, but no role in the token | Assign the role; sign out and in |
| API answers 401 with a token | Wrong audience (`AzureAd__ClientId` is not the API registration), or the token's tenant is not allowed | Compare the settings with section 5 |
| The API stops at start-up | A client ID is still the placeholder, or a tenant ID is not valid | Read the first error in the log; it names the setting |
