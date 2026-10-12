using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Vas.Infrastructure.Persistence.Mock;

/// <summary>
/// How the mock files are written and read: one file per Gold object, snake_case column names,
/// one row per line. The three large fact files are gzip-compressed to keep the repository small;
/// the content inside is the same line-per-row JSON.
/// </summary>
public static class GoldJson
{
    public const string ResourcePrefix = "Vas.Infrastructure.MockData.";

    public const string CountryFile = "dim_country.json";
    public const string CentreFile = "dim_centre.json";
    public const string VisaTypeFile = "dim_visa_type.json";
    public const string VisaApplicationFile = "fact_visa_application.json.gz";
    public const string ApplicationStageFile = "fact_application_stage.json.gz";
    public const string VasRevenueFile = "fact_vas_revenue.json.gz";
    public const string RefreshLogFile = "refresh_log.json";

    private const string CompressedExtension = ".gz";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

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

    /// <summary>The JSON text of every file, keyed by file name, before any compression.</summary>
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

    public static void WriteFile(string path, string json)
    {
        using var file = File.Create(path);
        using var target = IsCompressed(path) ? new GZipStream(file, CompressionLevel.SmallestSize) : (Stream)file;
        target.Write(Utf8.GetBytes(json));
    }

    /// <summary>Wraps a stored file so that reading it yields the JSON text, whether or not it is compressed.</summary>
    public static Stream OpenJson(Stream stored, string fileName) =>
        IsCompressed(fileName) ? new GZipStream(stored, CompressionMode.Decompress) : stored;

    private static bool IsCompressed(string fileName) =>
        fileName.EndsWith(CompressedExtension, StringComparison.OrdinalIgnoreCase);
}
