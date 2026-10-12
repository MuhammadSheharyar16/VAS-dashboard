using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.ApplicationTests.Support;
using Vas.Infrastructure.Persistence;

namespace Vas.ApplicationTests.Persistence;

/// <summary>
/// Runs the services through the real EF Core SQL Server provider with the connection switched off.
/// This proves every query translates to SQL and shows the SQL that would be sent. It does not prove
/// that Fabric accepts that SQL or returns the right rows; that needs the real endpoint (milestone M2).
/// </summary>
public class SqlTranslationTests
{
    private sealed class NoDatabaseInterceptor : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());

        public override InterceptionResult ConnectionClosing(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            InterceptionResult.Suppress();

        public override ValueTask<InterceptionResult> ConnectionClosingAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class CaptureSqlInterceptor : DbCommandInterceptor
    {
        public List<string> Sql { get; } = [];

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result) =>
            Capture(command);

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(Capture(command));

        private InterceptionResult<DbDataReader> Capture(DbCommand command)
        {
            Sql.Add(command.CommandText);
            return InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader());
        }
    }

    private static (ReportingDbContext Db, CaptureSqlInterceptor Captured) CreateContext()
    {
        var captured = new CaptureSqlInterceptor();
        var options = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseSqlServer("Server=no-database;Database=none")
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .AddInterceptors(new NoDatabaseInterceptor(), captured)
            .Options;

        return (new ReportingDbContext(options), captured);
    }

    public static TheoryData<ExecutiveSummaryQueryParams> Queries => new()
    {
        new ExecutiveSummaryQueryParams(),
        new ExecutiveSummaryQueryParams { From = new DateOnly(2026, 1, 1), ToExclusive = new DateOnly(2026, 10, 1) },
    };

    [Theory]
    [MemberData(nameof(Queries))]
    public async Task Executive_summary_queries_translate_to_read_only_sql_over_the_gold_views(
        ExecutiveSummaryQueryParams query)
    {
        var (db, captured) = CreateContext();
        await using var _ = db;

        await ReportingTestHost.ExecutiveSummary(db).GetAsync(query, CancellationToken.None);

        Assert.True(captured.Sql.Count >= 7, $"Expected at least 7 queries, saw {captured.Sql.Count}.");
        Assert.All(captured.Sql, sql =>
        {
            Assert.StartsWith("SELECT", sql.TrimStart());
            Assert.Contains("[gold].", sql);
            Assert.DoesNotContain("INSERT", sql);
            Assert.DoesNotContain("UPDATE", sql);
            Assert.DoesNotContain("DELETE", sql);
        });
        Assert.Contains(captured.Sql, sql => sql.Contains("[gold].[fact_visa_application]") && sql.Contains("COUNT("));
        Assert.Contains(captured.Sql, sql => sql.Contains("[gold].[fact_vas_revenue]") && sql.Contains("SUM("));
        Assert.Contains(captured.Sql, sql => sql.Contains("[gold].[refresh_log]"));
    }

    [Fact]
    public async Task Filter_values_are_sent_as_parameters_never_inlined_into_the_sql()
    {
        var (db, captured) = CreateContext();
        await using var _ = db;

        await ReportingTestHost.ExecutiveSummary(db).GetAsync(
            new ExecutiveSummaryQueryParams { From = new DateOnly(2026, 6, 1), ToExclusive = new DateOnly(2026, 6, 11) },
            CancellationToken.None);

        var applicationQueries = captured.Sql.Where(sql => sql.Contains("[fact_visa_application]")).ToList();
        Assert.NotEmpty(applicationQueries);
        Assert.All(applicationQueries, sql =>
        {
            Assert.Contains("@", sql);
            Assert.DoesNotContain("2026-06-01", sql);
        });
    }

    [Fact]
    public async Task Saving_is_refused_because_the_context_is_read_only()
    {
        var (db, _) = CreateContext();
        await using var __ = db;

        Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
