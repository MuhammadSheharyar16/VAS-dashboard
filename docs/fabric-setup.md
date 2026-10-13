# Microsoft Fabric connection setup

How the VAS dashboard reads its figures from Microsoft Fabric, what has to be set up in Fabric and in Azure, and how its values reach the application. Do the setup once per environment (Dev, UAT, Production).

Nothing here is a secret. The connection uses no password, no client secret and no connection-string credential.

**What has been tried:** nothing in this document has been run against a real Fabric workspace yet. The application's side (settings, token, queries, error handling) is built and tested on mock data. The Fabric and Azure steps describe what the code needs; confirm the exact grants with the data team, who own Fabric. This is milestone M2.

## 1. The idea in one minute

- The data team keeps the agreed KPI data in the **Gold layer** of a Fabric Lakehouse or Warehouse.
- Fabric exposes that data through a **SQL analytics endpoint**, which speaks the same protocol as SQL Server.
- The API connects to that endpoint as **itself**, using the App Service's *managed identity*. Azure gives the identity a token; there is nothing to store or rotate.
- The API only runs `SELECT`. It never writes to Fabric and never starts a refresh.

The executive who is signed in plays no part in this connection. Their token stops at the API; Fabric only ever sees the App Service.

## 2. How a report request flows

```text
 Browser               API (App Service)               Microsoft Entra ID        Fabric SQL analytics endpoint
    |                         |                                |                              |
 1. |-- GET /api/v1/... ----->|                                |                              |
    |   (executive's token)   |                                |                              |
 2. |                         | check the executive's token    |                              |
    |                         | and role (entra-setup.md)      |                              |
    |                         |                                |                              |
 3. |                         |-- "I am this App Service:  --->|                              |
    |                         |    a token for SQL, please"    |                              |
    |                         |<-- token for the managed ------|                              |
    |                         |    identity                    |                              |
    |                         |                                |                              |
 4. |                         |-- open connection on port 1433, encrypted, with that token -->|
    |                         |                                |     is this identity allowed |
    |                         |                                |     to connect and SELECT?   |
 5. |                         |-- SELECT ... FROM gold.fact_visa_application WHERE ... ------>|
    |                         |<-- aggregated rows -------------------------------------------|
    |                         |                                |                              |
 6. |<-- KPIs, breakdowns, ---|                                |                              |
    |    meta.dataSource:     |                                |                              |
    |    "Fabric"             |                                |                              |
```

| Step | What happens | Where in the code |
| --- | --- | --- |
| 2 | The request is authorised before any data access | `backend/src/Vas.Infrastructure/Authorization/` |
| 3 | A token for the scope `https://database.windows.net/.default` is requested for each connection | `backend/src/Vas.Infrastructure/Persistence/Interceptors/FabricAccessTokenInterceptor.cs` |
| 4 | The connection string holds the server and database only | `BuildConnectionString` in `backend/src/Vas.Infrastructure/DependencyInjection.cs` |
| 5 | Each report's query is written once in LINQ and translated to SQL by EF Core | `backend/src/Vas.Application/Features/` |
| 6 | Every response says which source it came from | `meta.dataSource` in the response |

Two different tokens are in play and they are never swapped: the executive's token calls this API, and the managed identity's token calls Fabric.

## 3. What you need before you start

| You need | Who has it | Why |
| --- | --- | --- |
| A Fabric capacity and a workspace | Data team | The Gold layer lives there |
| A Lakehouse or Warehouse with the Gold objects (section 5) | Data team | The data the reports read |
| Its **SQL analytics endpoint** host name and the **database** name | Data team | The two settings the API needs |
| An Azure App Service running the dashboard container | Cloud team | Its managed identity is the one that connects |
| A Fabric administrator | AEGI | To allow service principals to use Fabric (section 4.3) |
| Outbound access from the App Service to the endpoint on TCP **1433** | Cloud team | The connection uses the SQL protocol (TDS), encrypted |

## 4. Set it up

### 4.1 Find the endpoint and the database name (Fabric)

1. Open the workspace and the Lakehouse (or Warehouse).
2. Open its **SQL analytics endpoint**, then **Settings > SQL endpoint** (or **Copy SQL connection string**).
3. Note the host name. It looks like `xxxxxxxx.datawarehouse.fabric.microsoft.com`. This is `Fabric__SqlEndpoint`.
4. Note the name of the Lakehouse or Warehouse. This is `Fabric__Database`.

Give the host name only: no `tcp:` in front, no port, no `https://`.

### 4.2 Turn on the managed identity (Azure)

1. Open the App Service **> Settings > Identity > System assigned**.
2. Set **Status** to **On** and save.
3. Note the **Object (principal) ID**. The identity's name is the App Service's name.

### 4.3 Let that identity use Fabric (Fabric admin)

1. In the Fabric **Admin portal > Tenant settings > Developer settings**, enable **Service principals can use Fabric APIs**. Apply it to a security group, not to the whole organisation.
2. Add the App Service's managed identity to that security group.

Without this, the connection is refused even when the grants below are right.

### 4.4 Grant it read access to the Gold objects (Fabric)

The aim is the least access that works: the identity can connect, and can `SELECT` from the approved objects only.

1. **Share the Lakehouse or Warehouse** with the managed identity, with no extra permissions ticked. That gives it the right to connect and nothing else.
2. In the SQL analytics endpoint, run the grants as an administrator of the item:

   ```sql
   GRANT SELECT ON OBJECT::gold.dim_country           TO [<app-service-name>];
   GRANT SELECT ON OBJECT::gold.dim_centre            TO [<app-service-name>];
   GRANT SELECT ON OBJECT::gold.dim_visa_type         TO [<app-service-name>];
   GRANT SELECT ON OBJECT::gold.fact_visa_application TO [<app-service-name>];
   GRANT SELECT ON OBJECT::gold.fact_application_stage TO [<app-service-name>];
   GRANT SELECT ON OBJECT::gold.fact_vas_revenue      TO [<app-service-name>];
   GRANT SELECT ON OBJECT::gold.refresh_log           TO [<app-service-name>];
   ```

Do not add the identity as a workspace Member or Contributor: that would let it change data. A workspace *Viewer* can read every item in the workspace, which is wider than this project needs.

If the endpoint is in *user identity* mode, access is defined with OneLake security instead of SQL grants. The data team decides which mode the endpoint uses.

### 4.5 Give the values to the application (Azure)

In the App Service: **Settings > Environment variables > App settings**. Then restart.

| Setting | Value | Needed? |
| --- | --- | --- |
| `Reporting__DataSource` | `Fabric` | Always outside Development. It is the default |
| `Fabric__SqlEndpoint` | The host name from 4.1 | Always for `Fabric` |
| `Fabric__Database` | The Lakehouse or Warehouse name from 4.1 | Always for `Fabric` |
| `Fabric__CommandTimeoutSeconds` | Seconds a query may run. Default `30`, allowed 1 to 600 | Only to change the default |

The API refuses to start when the data source is `Fabric` and either of the two Fabric settings is empty. It also refuses to start with `Reporting__DataSource=Mock` anywhere except Development, so UAT and Production cannot show mock figures by mistake.

## 5. The Gold objects the application expects

The names below are **assumed** until the data team supplies the real ones and the KPI Mapping. They are in the schema `gold`.

| Object | One row per | Columns |
| --- | --- | --- |
| `gold.dim_country` | country | `country_code`, `country_name`, `region` |
| `gold.dim_centre` | centre | `centre_code`, `centre_name`, `country_code`, `city`, `daily_capacity` |
| `gold.dim_visa_type` | visa type | `visa_type_code`, `visa_type_name`, `category` |
| `gold.fact_visa_application` | application | `application_id`, `submitted_date`, `country_code`, `centre_code`, `visa_type_code`, `channel`, `current_stage`, `status`, `outcome`, `decision_date`, `turnaround_days` |
| `gold.fact_application_stage` | application per stage | `application_id`, `stage`, `entered_at_utc`, `exited_at_utc` |
| `gold.fact_vas_revenue` | VAS transaction | `transaction_id`, `transaction_date`, `country_code`, `centre_code`, `visa_type_code`, `service_name`, `amount`, `currency` |
| `gold.refresh_log` | refresh | `refreshed_at_utc`, `status` |

When the real names differ, they change in **one file**: `backend/src/Vas.Infrastructure/Persistence/Configurations/GoldConfigurations.cs`. The schema name is the constant `GoldSchema.Name` at the top of it; a Lakehouse without schemas puts its tables in `dbo`. Record what each KPI reads in [kpi-mapping.md](kpi-mapping.md).

The API only filters and aggregates (counts, sums, ratios). A KPI that needs business rules should arrive as a Gold view, not be rebuilt in the API.

## 6. Running against Fabric from a developer machine

On a developer machine there is no managed identity, so the same code uses the developer's own sign-in.

1. The developer's own Entra account needs the same access as in 4.4.
2. Sign in:

   ```bash
   az login --tenant <tenant-id>
   ```

3. In the root `.env` (copy `.env.example`):

   ```text
   Reporting__DataSource=Fabric
   Fabric__SqlEndpoint=xxxxxxxx.datawarehouse.fabric.microsoft.com
   Fabric__Database=<lakehouse-or-warehouse-name>
   ```

4. Start the API with `./run-dev.ps1`.

Use the script, not `docker compose`: the container cannot see the `az login` on your machine, so it has no identity to connect with. Without the `.env` lines above, Development keeps using the built-in mock data.

## 7. Check it (milestone M2)

1. The App Service starts and `/healthz` answers `Healthy`. This proves the settings are valid; it does not open a connection to Fabric.
2. Sign in and open the Executive Summary. The figures load, and the page shows **no** "Mock data" badge.
3. Call a report and look at the response: `meta.dataSource` is `"Fabric"`, and `meta.freshness` carries the last refresh time from `gold.refresh_log`.
4. Compare a few KPI values with the data owner's own figures for the same period and filters.
5. Ask the data team to confirm that the identity is refused on an object outside the approved list.
6. Note the response times. In-memory mock results do not prove how the same queries perform as SQL on Fabric.

## 8. What the application does when Fabric has a problem

- A transient fault is retried up to three times, a few seconds apart. Every query is a read, so repeating one is safe.
- If it still fails, the API answers **503** in the usual envelope and the page shows its error state with a "Try again" button. The user's filters are kept.
- Nothing is cached. Every report request is a fresh query.

## 9. When something goes wrong

| What you see | Likely cause | Fix |
| --- | --- | --- |
| The API stops at start-up, naming `Fabric:SqlEndpoint` | A Fabric setting is empty | Section 4.5 |
| The API stops at start-up, naming `Reporting:DataSource=Mock` | Mock was set outside Development | Set it to `Fabric` |
| 503, log says *Login failed* for a token-identified principal | The identity cannot connect: the tenant setting (4.3) or the share (4.4) is missing | Sections 4.3 and 4.4 |
| 503, log says *Cannot open server* or a network timeout | Wrong host name, or port 1433 is blocked on the way out | Check 4.1 and the network rules |
| 503, log says *Cannot open database* | Wrong `Fabric__Database` | Section 4.1, step 4 |
| 503, log says *Invalid object name* or *Invalid column name* | The real Gold names differ from the assumed ones | Section 5 |
| 503, log says *SELECT permission was denied* | A grant is missing for that object | Section 4.4 |
| 503 on the first request after a quiet period, then fine | The endpoint was waking up | Raise `Fabric__CommandTimeoutSeconds` if it recurs |
| Locally: *DefaultAzureCredential failed to retrieve a token* | No `az login`, or signed in to another tenant | Section 6, step 2 |
| The page shows the "Mock data" badge in a deployed environment | That App Service runs as Development | Set `ASPNETCORE_ENVIRONMENT` correctly and `Reporting__DataSource=Fabric` |
