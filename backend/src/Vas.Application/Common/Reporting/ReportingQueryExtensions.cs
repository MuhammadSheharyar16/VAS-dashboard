using Vas.Domain.Reporting;

namespace Vas.Application.Common.Reporting;

public static class ReportingQueryExtensions
{
    /// <summary>Applications submitted in the range, narrowed by the scope's country, centre and visa type.</summary>
    public static IQueryable<VisaApplication> InScope(
        this IQueryable<VisaApplication> applications,
        DateRange range,
        ReportScope scope)
    {
        var from = range.From;
        var toExclusive = range.ToExclusive;
        var query = applications.Where(a => a.SubmittedDate >= from && a.SubmittedDate < toExclusive);

        if (scope.CountryCode is { } countryCode)
        {
            query = query.Where(a => a.CountryCode == countryCode);
        }

        if (scope.CentreCode is { } centreCode)
        {
            query = query.Where(a => a.CentreCode == centreCode);
        }

        if (scope.VisaTypeCode is { } visaTypeCode)
        {
            query = query.Where(a => a.VisaTypeCode == visaTypeCode);
        }

        return query;
    }

    /// <summary>Transactions dated in the range, narrowed by the scope's country, centre and visa type.</summary>
    public static IQueryable<VasRevenueTransaction> InScope(
        this IQueryable<VasRevenueTransaction> transactions,
        DateRange range,
        ReportScope scope)
    {
        var from = range.From;
        var toExclusive = range.ToExclusive;
        var query = transactions.Where(t => t.TransactionDate >= from && t.TransactionDate < toExclusive);

        if (scope.CountryCode is { } countryCode)
        {
            query = query.Where(t => t.CountryCode == countryCode);
        }

        if (scope.CentreCode is { } centreCode)
        {
            query = query.Where(t => t.CentreCode == centreCode);
        }

        if (scope.VisaTypeCode is { } visaTypeCode)
        {
            query = query.Where(t => t.VisaTypeCode == visaTypeCode);
        }

        return query;
    }
}
