using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Reporting;
using Vas.Application.Features.VisaTypes.DTOs;

namespace Vas.Application.Features.VisaTypes.Services;

/// <summary>Application volumes and performance by visa type.</summary>
public sealed class VisaTypesService(
    IReportingDbContext db,
    IReportContextService reportContext,
    IValidator<VisaTypesQueryParams> validator) : IVisaTypesService
{
    public async Task<VisaTypesDto> GetAsync(VisaTypesQueryParams query, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);

        var context = await reportContext.ResolveAsync(query, cancellationToken);
        var scope = context.Scope;

        var stats = await db.VisaApplications.InScope(scope.Current, scope)
            .StatsBy(a => a.VisaTypeCode)
            .ToListAsync(cancellationToken);

        var names = await db.VisaTypeNamesAsync(cancellationToken);

        // A visa type with no applications in the period still gets a row, with zeros.
        var withoutData = names.Keys
            .Where(code => scope.VisaTypeCode is null || scope.VisaTypeCode == code)
            .Except(stats.Select(row => row.Key))
            .Select(code => ApplicationStats.Empty with { Key = code });

        return new VisaTypesDto(context.Meta, Breakdowns.Build(stats.Concat(withoutData), names));
    }
}
