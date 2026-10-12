using Vas.Application.Features.ExecutiveSummary.DTOs;

namespace Vas.Application.Features.ExecutiveSummary.Services;

public interface IExecutiveSummaryService
{
    Task<ExecutiveSummaryDto> GetAsync(ExecutiveSummaryQueryParams query, CancellationToken cancellationToken);
}
