using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Application.Common.Interfaces;

/// <summary>
/// Read-only view of the Gold layer. Services write each query once in LINQ; the JSON mock source
/// and the Fabric SQL endpoint both serve it.
/// </summary>
/// <remarks>
/// Both sources must be able to run every query, so end a query with ToListAsync or
/// FirstOrDefaultAsync over a projection. Avoid scalar terminators (CountAsync, SumAsync, AnyAsync):
/// express totals as GroupBy(_ => 1).Select(...) instead.
/// </remarks>
public interface IReportingDbContext
{
    ReportingDataSource DataSource { get; }

    IQueryable<Country> Countries { get; }

    IQueryable<Centre> Centres { get; }

    IQueryable<VisaType> VisaTypes { get; }

    IQueryable<VisaApplication> VisaApplications { get; }

    IQueryable<ApplicationStage> ApplicationStages { get; }

    IQueryable<VasRevenueTransaction> VasRevenue { get; }

    IQueryable<RefreshLog> RefreshLogs { get; }
}
