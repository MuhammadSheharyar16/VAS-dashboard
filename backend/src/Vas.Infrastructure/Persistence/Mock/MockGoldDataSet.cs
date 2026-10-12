using System.Reflection;
using System.Text.Json;
using Vas.Domain.Reporting;

namespace Vas.Infrastructure.Persistence.Mock;

/// <summary>The rows of every Gold object, as the mock source holds them in memory.</summary>
public sealed record MockGoldDataSet(
    IReadOnlyList<Country> Countries,
    IReadOnlyList<Centre> Centres,
    IReadOnlyList<VisaType> VisaTypes,
    IReadOnlyList<VisaApplication> VisaApplications,
    IReadOnlyList<ApplicationStage> ApplicationStages,
    IReadOnlyList<VasRevenueTransaction> VasRevenue,
    IReadOnlyList<RefreshLog> RefreshLogs)
{
    private static readonly Lazy<MockGoldDataSet> Embedded = new(LoadEmbedded);

    public static MockGoldDataSet Empty { get; } = new([], [], [], [], [], [], []);

    /// <summary>The synthetic data set shipped inside this assembly. Parsed once per process.</summary>
    public static MockGoldDataSet FromEmbeddedFiles() => Embedded.Value;

    private static MockGoldDataSet LoadEmbedded() => new(
        Read<Country>(GoldJson.CountryFile),
        Read<Centre>(GoldJson.CentreFile),
        Read<VisaType>(GoldJson.VisaTypeFile),
        Read<VisaApplication>(GoldJson.VisaApplicationFile),
        Read<ApplicationStage>(GoldJson.ApplicationStageFile),
        Read<VasRevenueTransaction>(GoldJson.VasRevenueFile),
        Read<RefreshLog>(GoldJson.RefreshLogFile));

    private static List<T> Read<T>(string fileName)
    {
        using var stream = OpenEmbedded(fileName);
        return JsonSerializer.Deserialize<List<T>>(stream, GoldJson.Options)
            ?? throw new InvalidOperationException($"Mock data file {fileName} is empty.");
    }

    /// <summary>Opens an embedded mock file as JSON text, decompressing it when it is stored compressed.</summary>
    public static Stream OpenEmbedded(string fileName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var stored = assembly.GetManifestResourceStream($"{GoldJson.ResourcePrefix}{fileName}")
            ?? throw new InvalidOperationException($"Mock data file {fileName} is not embedded in {assembly.GetName().Name}.");

        return GoldJson.OpenJson(stored, fileName);
    }
}
