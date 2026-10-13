using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vas.Application;
using Vas.Application.Common.Interfaces;
using Vas.Application.Common.Options;
using Vas.Application.Features.Auth.Services;
using Vas.Application.Features.Centres.DTOs;
using Vas.Application.Features.Centres.Services;
using Vas.Application.Features.ExecutiveSummary.DTOs;
using Vas.Application.Features.ExecutiveSummary.Services;
using Vas.Application.Features.Lookups.DTOs;
using Vas.Application.Features.Lookups.Services;
using Vas.Application.Features.VasApplications.DTOs;
using Vas.Application.Features.VasApplications.Services;
using Vas.Application.Features.VasOutcomes.DTOs;
using Vas.Application.Features.VasOutcomes.Services;
using Vas.Application.Features.VasProcessing.DTOs;
using Vas.Application.Features.VasProcessing.Services;
using Vas.Application.Features.VasRevenue.DTOs;
using Vas.Application.Features.VasRevenue.Services;
using Vas.Application.Features.VisaTypes.DTOs;
using Vas.Application.Features.VisaTypes.Services;
using Vas.Infrastructure.Persistence.Mock;
using Vas.Shared.Constants;

// Writes the frontend's test fixtures by running the real services over the embedded mock data,
// so the fixtures always have the shape and the figures the API returns. Run from the repository root:
//   dotnet run --project backend/mock-data/fixture-exporter -- frontend/tests/fixtures backend/src/Vas.Api/appsettings.json

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Vas.FixtureExporter <output-directory> <path-to-api-appsettings.json>");
    return 1;
}

var outputDirectory = Path.GetFullPath(args[0]);
Directory.CreateDirectory(outputDirectory);

var configuration = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(args[1])).Build();
var data = MockGoldDataSet.FromEmbeddedFiles();

var services = new ServiceCollection();
services.AddApplication();
services.AddSingleton<IReportingDbContext>(new JsonReportingSource(data));
services.AddSingleton<IDateTimeProvider>(new FixedClock(data.RefreshLogs.Max(log => log.RefreshedAtUtc)));
services.AddSingleton<ICurrentUser>(new SampleExecutive());
services.AddSingleton(Options.Create(
    configuration.GetSection(KpiThresholdOptions.SectionName).Get<KpiThresholdOptions>() ?? new KpiThresholdOptions()));
services.AddSingleton(Options.Create(new PaginationOptions()));
services.AddSingleton(Options.Create(new EntraOptions()));

using var provider = services.BuildServiceProvider();
using var scope = provider.CreateScope();
T Service<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
};

void Write(string name, object value)
{
    File.WriteAllText(Path.Combine(outputDirectory, $"{name}.json"), JsonSerializer.Serialize(value, json) + "\n");
    Console.WriteLine($"{name}.json");
}

var none = CancellationToken.None;
var auth = Service<IAuthService>();
var lookups = Service<ILookupService>();

Write("auth-me", auth.GetCurrentUser());
Write("auth-permissions", auth.GetPermissions());
Write("auth-menu", auth.GetMenu());
Write("executive-summary", await Service<IExecutiveSummaryService>().GetAsync(new ExecutiveSummaryQueryParams(), none));
Write("vas-applications", await Service<IVasApplicationsService>().GetAsync(new VasApplicationsQueryParams(), none));
Write("vas-outcomes", await Service<IVasOutcomesService>().GetAsync(new VasOutcomesQueryParams(), none));
Write("vas-processing", await Service<IVasProcessingService>().GetAsync(new VasProcessingQueryParams(), none));
Write("vas-revenue", await Service<IVasRevenueService>().GetAsync(new VasRevenueQueryParams(), none));
Write("vas-visa-types", await Service<IVisaTypesService>().GetAsync(new VisaTypesQueryParams(), none));
Write("vas-centres", await Service<ICentresService>().GetAsync(new CentresQueryParams { PageSize = 100 }, none));
Write("lookups-countries", await lookups.GetCountriesAsync(none));
Write("lookups-centres", await lookups.GetCentresAsync(new CentreLookupQueryParams(), none));
Write("lookups-visa-types", await lookups.GetVisaTypesAsync(none));

return 0;

internal sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
}

internal sealed class SampleExecutive : ICurrentUser
{
    public bool IsAuthenticated => true;
    public string UserId => "00000000-0000-0000-0000-00000000mock";
    public string? DisplayName => "Sample Executive";
    public string? Username => "sample.executive@example.test";
    public IReadOnlyCollection<string> Roles => [AppRoles.Viewer];
    public IReadOnlyCollection<string> Scopes => ["Dashboard.Read"];
}
