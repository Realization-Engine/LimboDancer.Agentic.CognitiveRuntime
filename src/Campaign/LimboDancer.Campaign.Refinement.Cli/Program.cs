using System.Diagnostics;
using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Rendering;

if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: pilot <panzer-leader-05.json> <output-directory> [32-character-seed]");
    return 2;
}
try
{
    var inputs = new PilotInputs(File.ReadAllText(args[0]));
    var output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
    var seedPath = Path.Combine(output, "seed.txt");
    var seed = args.Length == 3 ? args[2] : File.Exists(seedPath) ? File.ReadAllText(seedPath).Trim() : SeedRandom.NewSeed();
    SeedRandom.ValidateSeed(seed);
    if (File.Exists(seedPath) && File.ReadAllText(seedPath).Trim() != seed)
        throw new InvalidDataException("This output directory belongs to another seed. Choose a new directory for a new world.");
    if (!File.Exists(seedPath))
    {
        using var file = new FileStream(seedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(file); writer.WriteLine(seed);
    }
    var watch = Stopwatch.StartNew();
    var result = new PilotGenerator(inputs).Generate(seed);
    var directory = PackagePublisher.Publish(result, inputs, output);
    var render = BoardRenderInput.Create(result.Package.Board, result.Package.Name, result.Grid, inputs.Catalog, result.Facts,
        new StyledSource(result.Package.Model, result.Grid, result.Facts));
    File.WriteAllText(Path.Combine(directory, "preview.svg"), BoardRenderer.Document(render, BoardView.Exact));
    File.WriteAllText(Path.Combine(directory, "exact.svg"), BoardRenderer.Document(render, BoardView.Exact));
    File.WriteAllText(Path.Combine(directory, "facts.json"), JsonSerializer.Serialize(result.Facts.Hexes, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Pilot package: {directory}");
    Console.WriteLine($"Seed: {seed}");
    Console.WriteLine($"Features: {result.Package.Model.Features.Count}; hexes: {result.Facts.Hexes.Count}; warnings: {result.Validation.Summary.Warnings}");
    Console.WriteLine($"Generated, reloaded, compiled and rendered in {watch.Elapsed.TotalSeconds:F2}s. Inspection only; gameplay not admitted.");
    return 0;
}
catch (Exception e) when (e is IOException or ArgumentException or JsonException or InvalidOperationException)
{
    Console.Error.WriteLine("Pilot rejected: " + e.Message); return 1;
}
