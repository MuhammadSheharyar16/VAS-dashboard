using Vas.Infrastructure.Persistence.Mock;
using Vas.MockDataGenerator;

// Writes the Gold-shaped mock files. Run from the repository root:
//   dotnet run --project backend/mock-data/generator -- backend/src/Vas.Infrastructure/MockData

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: Vas.MockDataGenerator <output-directory>");
    return 1;
}

var outputDirectory = Path.GetFullPath(args[0]);
Directory.CreateDirectory(outputDirectory);

var data = MockGoldGenerator.Generate();

foreach (var (fileName, json) in GoldJson.SerializeAll(data))
{
    var path = Path.Combine(outputDirectory, fileName);
    GoldJson.WriteFile(path, json);
    Console.WriteLine($"{fileName,-36} {new FileInfo(path).Length / 1024.0,10:N0} KB on disk");
}

Console.WriteLine(
    $"{data.VisaApplications.Count:N0} applications, {data.ApplicationStages.Count:N0} stage rows, "
    + $"{data.VasRevenue.Count:N0} transactions, seed {MockGoldGenerator.Seed}.");

return 0;
