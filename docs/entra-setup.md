# Entra ID setup

What has to exist in Microsoft Entra ID before an executive can sign in (milestone M1), and how its values reach the application. Do this once per environment (Dev, UAT, Production).

Nothing here is a secret. There is no client secret anywhere in this project.

## What you create

| Item | Purpose |
| --- | --- |
| API app registration | Represents the protected dashboard API. Exposes the scope and owns the app role |
| SPA app registration | Represents the React app. A public client: no secret |
| Role assignment | Says which executives may use the dashboard |

You need permission to create app registrations and to grant admin consent in the tenant (Application Administrator or Cloud Application Administrator is enough).

## 1. API app registration

1. **App registrations > New registration.** Name it `AEGI VAS Dashboard API (<env>)`. Supported account types: *Accounts in this organizational directory only* for one tenant, or *Accounts in any organizational directory (Multitenant)* when users of other organisations sign in (see "Which organisations can sign in"). No redirect URI.
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
6. Note the **Application (client) ID**. Note the **Directory (tenant) ID** too if sign-in is to be limited to named tenants.

## 2. SPA app registration

1. **New registration.** Name it `AEGI VAS Dashboard (<env>)`. Same account type as the API registration.
2. **Authentication > Add a platform > Single-page application.** Add the redirect URIs for this environment:
   - Dev (local): `http://localhost:5173` and `http://localhost:8080`
   - Deployed: `https://<app-service-host>`

   Leave both *implicit grant* boxes unticked. The app uses the authorization code flow with PKCE.
3. **API permissions > Add a permission > My APIs >** the API registration **> Delegated permissions >** `Dashboard.Read`.
4. **Grant admin consent** for the tenant.
5. Note the **Application (client) ID**.

## 3. Assign executives

1. **Enterprise applications >** open the **API** application (not the SPA).
2. **Properties > Assignment required?** set to **Yes**. Without it, any user in the tenant can obtain a token; they would still be refused by the API (403), but assignment keeps the sign-in itself closed.
3. **Users and groups > Add user/group.** Pick the executives, or a security group that holds them, and the role `VAS Dashboard Viewer`.

A role change takes effect at the user's next access token, which is at most about an hour later, or immediately after they sign out and in again.

## 4. Give the values to the application

The API reads them from configuration. In App Service they are application settings; locally they go in `backend/src/Vas.Api/appsettings.Development.json` or environment variables.

| Setting | Value |
| --- | --- |
| `AzureAd__ClientId` | Client ID of the **API** registration |
| `AzureAd__SpaClientId` | Client ID of the **SPA** registration |
| `AzureAd__TenantId` | Optional. A Directory (tenant) ID to limit sign-in to. Leave unset to accept any organisation (see "Which organisations can sign in") |
| `AzureAd__AllowedTenantIds` | Optional. More tenant IDs to limit sign-in to, comma-separated |
| `AzureAd__RequiredScope` | `Dashboard.Read` (the default; set only if you named the scope differently) |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` (the default) |

The React app does not have these built in. It loads them at start-up from `/config.json`, which the API serves from the settings above. That is what lets one container image run in every environment.

Outside Development the API refuses to start while either client ID is still the all-zero placeholder. It also refuses to start, in any environment, when `AzureAd__AllowedTenantIds` holds anything that is not a tenant ID.

## 5. Check it (M1)

1. Open `/config.json`. It shows the SPA client ID, the authority and `api://<api-client-id>/Dashboard.Read`. The authority ends in `/organizations` unless sign-in is limited to exactly one tenant, in which case it ends in that tenant's ID.
2. Call `/api/v1/auth/me` with no token. Expect **401**.
3. Sign in as a user who is **not** assigned the role and call it again. Expect **403**.
4. Sign in as an assigned executive. Expect **200** with their name and the role `VasDashboard.Viewer`.

Steps 3 and 4 need a real access token. The normal way to get one is to sign in through the React app.

To get a token by hand before the React app exists, you can temporarily let the Azure CLI request one. This procedure has not been run against a tenant for this project yet, so treat it as a starting point:

1. On the **API** registration: **Expose an API > Add a client application**, client ID `04b07795-8ddb-461a-bbee-02f9e1bf7b46` (the Azure CLI), and tick the `Dashboard.Read` scope.
2. Request the token:

   ```bash
   az login --tenant <tenant-id>
   az account get-access-token --scope api://<api-client-id>/Dashboard.Read --query accessToken -o tsv
   ```

3. Remove the Azure CLI client application again when you are done. Never do this in Production.

## Which organisations can sign in

No tenant ID has to be configured. The application takes the organisation from the token itself, and the tenant settings only narrow that down.

| `AzureAd__TenantId` and `AzureAd__AllowedTenantIds` | Who can sign in |
| --- | --- |
| Both unset (the default) | A user of **any** Entra organisation whose administrator has consented to the application and assigned the user the role |
| One tenant ID between them | Users of that tenant only |
| Several tenant IDs | Users of those tenants only; a token from any other tenant gets **401** |

Every signed-in user sees the same data; organisations are not separated from each other in the reports.

**With the tenant settings unset, the role is the only gate, and each organisation's own administrator assigns it.** Both registrations are multitenant and their client IDs are public (`/config.json`), so the administrator of any Entra tenant, including one created for the purpose, can consent to the application, assign themselves `VAS Dashboard Viewer` and read the dashboard. Use this mode only where that is acceptable (for example Development on mock data). Wherever the dashboard shows real data, list the tenants that are meant to have access.

This procedure has not been run against real tenants for this project yet, so treat it as a starting point.

**In the tenant that owns the registrations, once:**

1. On **both** registrations: **Authentication > Supported account types >** *Accounts in any organizational directory (Multitenant)*. With the single-tenant account type, Entra ID refuses users of other organisations at the sign-in page, whatever the application settings say.
2. Either leave `AzureAd__TenantId` and `AzureAd__AllowedTenantIds` unset, or list the tenants that may sign in. When you list tenants, include your own if its users need access.
3. Assign your own executives as in section 3.

**In each other organisation, by one of its administrators:**

1. Grant admin consent to the API, then to the SPA, by opening each address and accepting:

   ```text
   https://login.microsoftonline.com/<their-tenant-id>/adminconsent?client_id=<api-client-id>
   https://login.microsoftonline.com/<their-tenant-id>/adminconsent?client_id=<spa-client-id>
   ```

   This creates the two enterprise applications in their tenant.
2. **Enterprise applications >** the **API** application **> Properties > Assignment required?** set to **Yes**.
3. **Users and groups > Add user/group.** Assign their executives the role `VAS Dashboard Viewer`. Role assignments are made per tenant; an assignment in one tenant does not cover users of another.

To check it, repeat the M1 steps with a user of another organisation: 200 with the role and 403 without it. If tenants are listed, a user of an unlisted tenant gets 401.

## What the API checks on every request

- The token's signature, issuer, audience and expiry (30 seconds of clock skew allowed).
- The token's tenant, when `AzureAd__TenantId` or `AzureAd__AllowedTenantIds` is set. When neither is set, the issuer only has to be an Entra organisation.
- The `Dashboard.Read` scope.
- A role that grants the screen being requested. The mapping lives in one file: `backend/src/Vas.Application/Features/Auth/Rules/RolePermissionMap.cs`.

The executive's token is only ever used to call this API. The connection to Fabric uses the App Service managed identity, which is set up separately.
