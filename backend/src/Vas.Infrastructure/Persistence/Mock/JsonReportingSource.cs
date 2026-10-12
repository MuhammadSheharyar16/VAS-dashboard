using Vas.Application.Common.Interfaces;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Infrastructure.Persistence.Mock;

/// <summary>Serves the Gold-shaped JSON rows from memory. Used until the real Gold layer exists.</summary>
public sealed class JsonReportingSource(MockGoldDataSet data) : IReportingDbContext
{
    public ReportingDataSource DataSource => ReportingDataSource.Mock;

    public IQueryable<Country> Countries => Query(data.Countries);

    public IQueryable<Centre> Centres => Query(data.Centres);

    public IQueryable<VisaType> VisaTypes => Query(data.VisaTypes);

    public IQueryable<VisaApplication> VisaApplications => Query(data.VisaApplications);

    public IQueryable<ApplicationStage> ApplicationStages => Query(data.ApplicationStages);

    public IQueryable<VasRevenueTransaction> VasRevenue => Query(data.VasRevenue);

    public IQueryable<RefreshLog> RefreshLogs => Query(data.RefreshLogs);

    private static IQueryable<T> Query<T>(IEnumerable<T> rows) => new InMemoryAsyncQueryable<T>(rows);
}
