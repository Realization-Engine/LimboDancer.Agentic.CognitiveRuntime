using System.Diagnostics;
using System.Text.Json;
using LimboDancer.Campaign.Refinement;
using LimboDancer.Domains.Asl.Maps.Rendering;

// Diagnose an accepted deployment without modifying its terrain or saved positions.
if (args.Length == 4 && args[0] == "--profile-deployment")
{
    var inputs = new PilotInputs(File.ReadAllText(args[1]));
    using var saved = JsonDocument.Parse(File.ReadAllText(args[3]));
    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
    var setup = saved.RootElement.GetProperty("Setup").Deserialize<SituationDeploymentDraft>(options)!;
    var map = new SituationMap(inputs, args[2]);
    var addresses = new SituationAddresses(inputs, map.Manifest.Seed);
    var service = new SituationDeployment(inputs, map);
    var watch = Stopwatch.StartNew();
    var filled = service.Assemble(setup.Formations, setup.Positions);
    Console.WriteLine(JsonSerializer.Serialize(new { Seconds = watch.Elapsed.TotalSeconds,
        Preserved = setup.Positions.Length, Placed = filled.Positions.Length, Review = filled.Review.Length,
        Unchanged = setup.Positions.All(p => filled.Positions.Contains(p)) }));
    var roster = SituationRoster.Create(inputs);
    foreach (var formation in roster.Ground.Where(f => f.Units.Any(u => filled.Review.Any(r => r.UnitId == u.Id))))
    {
        var anchor = setup.Formations.Single(f => f.UnitId == formation.Id).Anchor;
        var terrain = addresses.Cells.Where(c => c.Parent == anchor).Select(c =>
        {
            var facts = map.Tile(c.Tile).Facts[LimboDancer.Domains.Asl.Maps.Coordinates.HexName.Parse(c.Hex)];
            return facts.Center.Terrain?.Name ?? "unknown";
        }).GroupBy(name => name).ToDictionary(g => g.Key, g => g.Count());
        Console.WriteLine(JsonSerializer.Serialize(new { formation.Id, Anchor = anchor, Terrain = terrain,
            Issues = filled.Review.Where(r => formation.Units.Any(u => u.Id == r.UnitId)).ToArray() }));
    }
    return 0;
}

// Diagnose full-board transition stages without regenerating or changing accepted terrain.
if (args.Length == 3 && args[0] == "--profile-asl-board")
{
    T Measure<T>(string stage, Func<T> work)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { Stage = stage, Status = "started" }));
        var timer = Stopwatch.StartNew();
        var allocated = GC.GetTotalAllocatedBytes();
        var result = work();
        using var process = Process.GetCurrentProcess();
        Console.WriteLine(JsonSerializer.Serialize(new { Stage = stage, Seconds = timer.Elapsed.TotalSeconds,
            AllocatedBytes = GC.GetTotalAllocatedBytes() - allocated, PeakWorkingSetBytes = process.PeakWorkingSet64 }));
        return result;
    }
    var source = new PilotInputs(File.ReadAllText(args[1]));
    var map = new SituationMap(source, args[2]);
    var board = Measure("assemble", () => SituationAslBoard.Assemble(source, map));
    var grid = Measure("compile", () => LimboDancer.Domains.Asl.Maps.Features.FeatureCompiler.Compile(board.Model, source.Catalog).Grid);
    var facts = Measure("derive", () => LimboDancer.Domains.Asl.Maps.Derivation.VaslCompatibleHexFactDerivation.Derive(
        grid, source.Catalog, LimboDancer.Domains.Asl.Maps.Derivation.HexsideAnnotations.None));
    var report = Measure("validate", () => LimboDancer.Domains.Asl.Maps.Features.BoardValidator.Validate(board.Model, facts, source.Catalog));
    Console.WriteLine(JsonSerializer.Serialize(new { BoardVersion = board.Version, GridSha256 = PilotGenerator.GridHash(grid),
        Hexes = facts.Hexes.Count, report.Summary }));
    return report.HasErrors ? 1 : 0;
}

var wholeSituation = args.LastOrDefault() == "--situation";
if (wholeSituation) args = args[..^1];
if (args.Length is < 2 or > 3)
{
    Console.Error.WriteLine("Usage: pilot <panzer-leader-05.json> <output-directory> [32-character-seed] [--situation]");
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
    if (wholeSituation)
    {
        var manifest = SituationPublisher.Publish(inputs, seed, output,
            (done, total) => Console.WriteLine($"Published {done}/{total} packages."));
        var generationSeconds = watch.Elapsed.TotalSeconds;
        watch.Restart();
        var accepted = SituationPublisher.Verify(inputs, manifest);
        var reloadSeconds = watch.Elapsed.TotalSeconds;
        using var process = Process.GetCurrentProcess();
        Console.WriteLine(JsonSerializer.Serialize(new { Manifest = manifest, Packages = accepted.Packages.Length,
            OwnedCells = accepted.Packages.Sum(p => p.OwnedCells), GenerationSeconds = generationSeconds,
            ReloadVerificationSeconds = reloadSeconds, PeakWorkingSetBytes = process.PeakWorkingSet64,
            StoredBytes = Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) }));
        return 0;
    }
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
