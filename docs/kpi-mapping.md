# KPI mapping

How each dashboard figure is produced from the Gold layer.

**Everything here is provisional.** The *AEGI Intelligence Hub v3 – KPI Mapping* document has not been received, so the Gold object names, the column names and the KPI definitions below are assumptions made to build on mock data. Each one has to be confirmed or replaced by the data team and AEGI before UAT.

The backend only filters and aggregates (counts, sums, ratios). If the confirmed definition of a KPI needs business rules, that KPI should be delivered as a Gold view and this file updated to read it.

## Gold objects (assumed)

The names live in one file: `backend/src/Vas.Infrastructure/Persistence/Configurations/GoldConfigurations.cs`.

| Object | Grain | Used for |
| --- | --- | --- |
| `gold.dim_country` | One row per country | Country filter, country names |
| `gold.dim_centre` | One row per centre | Centre filter |
| `gold.dim_visa_type` | One row per visa type | Visa-type filter |
| `gold.fact_visa_application` | One row per application | Volumes, outcomes, turnaround, trend |
| `gold.fact_application_stage` | One row per application per stage | Not read yet (VAS processing report) |
| `gold.fact_vas_revenue` | One row per transaction | Revenue |
| `gold.refresh_log` | One row per refresh | Data freshness, default date range |

## Filters

| Filter | Applied to applications | Applied to revenue |
| --- | --- | --- |
| Date range `[from, toExclusive)` | `submitted_date` | `transaction_date` |
| Country | `country_code` | `country_code` |
| Centre | `centre_code` | `centre_code` |
| Visa type | `visa_type_code` | `visa_type_code` |

- Every application figure counts the applications **submitted** in the range. An application decided in the range but submitted before it is not counted.
- With no dates given, the range is the 30 days ending at the day of the last successful refresh in `gold.refresh_log`. That day is excluded, so the newest day shown is one the Gold layer has fully loaded.
- The comparison period is the range of the same length immediately before.

## Executive Summary

Endpoint: `GET /api/v1/executive-summary`

| KPI key | Definition | Source |
| --- | --- | --- |
| `applicationsReceived` | Count of applications in scope | `fact_visa_application` |
| `applicationsProcessed` | Count where `status` is not `Pending` | `fact_visa_application.status` |
| `applicationsPending` | Count where `status` is `Pending` | `fact_visa_application.status` |
| `approvalRate` | Approved ÷ (Approved + Rejected), as a percentage with one decimal. Withdrawn and pending applications are left out of both sides | `fact_visa_application.outcome` |
| `averageTurnaroundDays` | Average of `turnaround_days` over applications that have one, to one decimal | `fact_visa_application.turnaround_days` |
| `vasRevenue` | Sum of `amount`, one figure per `currency`. Currencies are never converted or added together | `fact_vas_revenue` |

Each KPI also carries the previous period's value, the percentage change and a status.

| Chart | Definition |
| --- | --- |
| Applications trend | Received and decided (`outcome` not null) per day for ranges up to 62 days, per month for longer ranges. Days or months with no rows appear as zero |
| Outcomes | Approved, Rejected, Withdrawn and Pending, each as a count and a share of received |
| Top countries | The five countries with the most applications received, with their share of the total |

## Status thresholds

Thresholds are in API configuration (`KpiThresholds`), not in code and not in React. The current values are **placeholders**; every response says so through `meta.thresholdsArePlaceholders`.

| KPI key | Warning | Critical | Better when |
| --- | --- | --- | --- |
| `approvalRate` | 75% or lower | 60% or lower | Higher |
| `averageTurnaroundDays` | 10 days or more | 15 days or more | Lower |

A KPI without a threshold has the status `Neutral`.

## Questions this raises for the data team and AEGI

1. Should "processed" and "approved" count by decision date instead of submission date?
2. Is "pending" the backlog at the end of the period, or the pending share of what was received in it?
3. Does the approval rate include withdrawn applications in its base?
4. Is `turnaround_days` already calculated in Gold, and in calendar or working days?
5. Is revenue ever in more than one currency, and if so is a reporting currency expected?
6. Does `refresh_log` (or an equivalent) exist, and what marks a successful refresh?
