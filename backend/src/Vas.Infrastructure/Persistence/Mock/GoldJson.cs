using System.Text;
using System.Text.Json;

namespace Vas.Infrastructure.Persistence.Mock;

/// <summary>
/// How the mock files are written and read: one file per Gold object, snake_case column names,
/// one row per line so a change shows up as a readable diff.
/// </summary>
public static class GoldJson
{
    public const string ResourcePrefix = "Vas.Infrastructure.MockData.";

    public const string CountryFile = "dim_country.json";
    public const string CentreFile = "dim_centre.json";
    public const string VisaTypeFile = "dim_visa_type.json";
    public const string VisaApplicationFile = "fact_visa_application.json";
    public const string ApplicationStageFile = "fact_application_stage.json";
    public const string VasRevenueFile = "fact_vas_revenue.json";
    public const string RefreshLogFile = "refresh_log.json";

    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string Serialize<T>(IEnumerable<T> rows)
    {
        var text = new StringBuilder("[\n");
        text.AppendJoin(",\n", rows.Select(row => JsonSerializer.Serialize(row, Options)));
        text.Append("\n]\n");
        return text.ToString();
    }

    public static IReadOnlyDictionary<string, string> SerializeAll(MockGoldDataSet data) =>
        new Dictionary<string, string>
        {
            [CountryFile] = Serialize(data.Countries),
            [CentreFile] = Serialize(data.Centres),
            [VisaTypeFile] = Serialize(data.VisaTypes),
            [VisaApplicationFile] = Serialize(data.VisaApplications),
            [ApplicationStageFile] = Serialize(data.ApplicationStages),
            [VasRevenueFile] = Serialize(data.VasRevenue),
            [RefreshLogFile] = Serialize(data.RefreshLogs),
        };
}
