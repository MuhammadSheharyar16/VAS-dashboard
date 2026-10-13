using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Domain.Enums;
using Vas.Domain.Reporting;

namespace Vas.Infrastructure.Persistence;

/// <summary>
/// Read-only context over the Fabric Gold layer. No migrations, no tracking, and saving throws:
/// this application never writes to Fabric.
/// </summary>
public sealed class ReportingDbContext(DbContextOptions<ReportingDbContext> options)
    : DbContext(options), IReportingDbContext
{
    private const string ReadOnlyMessage = "The reporting context is read-only. This application never writes to Fabric.";

    public ReportingDataSource DataSource => ReportingDataSource.Fabric;

    public IQueryable<Country> Countries => Set<Country>();

    public IQueryable<Centre> Centres => Set<Centre>();

    public IQueryable<VisaType> VisaTypes => Set<VisaType>();

    public IQueryable<VisaApplication> VisaApplications => Set<VisaApplication>();

    public IQueryable<ApplicationStage> ApplicationStages => Set<ApplicationStage>();

    public IQueryable<VasRevenueTransaction> VasRevenue => Set<VasRevenueTransaction>();

    public IQueryable<RefreshLog> RefreshLogs => Set<RefreshLog>();

    // Fabric text columns are varchar. Sending filters as varchar keeps the comparison on the
    // column's own type instead of converting every row to nvarchar.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<string>().AreUnicode(false);

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(ReadOnlyMessage);
}
