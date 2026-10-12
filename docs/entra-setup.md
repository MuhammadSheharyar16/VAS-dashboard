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

1. **App registrations > New registration.** Name it `AEGI VAS Dashboard API (<env>)`. Supported account types: *Accounts in this organizational directory only*. No redirect URI.
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

## 2. SPA app registration

1. **New registration.** Name it `AEGI VAS Dashboard (<env>)`. Same single-tenant account type.
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
| `AzureAd__TenantId` | Directory (tenant) ID |
| `AzureAd__ClientId` | Client ID of the **API** registration |
| `AzureAd__SpaClientId` | Client ID of the **SPA** registration |
| `AzureAd__RequiredScope` | `Dashboard.Read` (the default; set only if you named the scope differently) |
| `AzureAd__Instance` | `https://login.microsoftonline.com/` (the default) |

The React app does not have these built in. It loads them at start-up from `/config.json`, which the API serves from the settings above. That is what lets one container image run in every environment.

Outside Development the API refuses to start while any of the three IDs is still the all-zero placeholder.

## 5. Check it (M1)

1. Open `/config.json`. It shows the tenant ID, the SPA client ID, the authority and `api://<api-client-id>/Dashboard.Read`.
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

## What the API checks on every request

- The token's signature, issuer, audience and expiry (30 seconds of clock skew allowed).
- The `Dashboard.Read` scope.
- A role that grants the screen being requested. The mapping lives in one file: `backend/src/Vas.Application/Features/Auth/Rules/RolePermissionMap.cs`.

The executive's token is only ever used to call this API. The connection to Fabric uses the App Service managed identity, which is set up separately.
