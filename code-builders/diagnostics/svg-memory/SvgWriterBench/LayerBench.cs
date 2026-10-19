using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.Rendering.Palettes;
using LimboDancer.Domains.Asl.Units.Vocabulary;

/// <summary>
/// The counter layer workload of the Counter Layer Design (section 11, "Allocation"; option E, section 7.1): one map, a sequence of overlays with two
/// counters moved per step, built as Replay and Play build them. Reports per step the layer's characters, the build's time and allocation, and a copy
/// of <c>GameMaps.Led</c>'s rewrite timed apart, since E leaves it in place. Needs no VASL checkout: <c>SvgWriterBench layer [repository root]</c>.
/// </summary>
internal static class LayerBench
{
    private const int Steps = 30;
    private const int Rounds = 5;

    public static string Run(string repo)
    {
        var vocabulary = UnitVocabulary.Asl();
        var sheet = UnitStyles.Load(UnitStyles.Digital);
        var renderer = new UnitRenderer(vocabulary, sheet, PaletteSetReader.Embedded(sheet.Palette!));
        var target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);
        var set = UnitPlacementSetReader.Read(File.ReadAllBytes(Path.Combine(repo, "src", "ASL", "units", "examples", "bd01-infantry-figures.units.json")),
            vocabulary).Set!;
        var sequence = Sequence(set.Units);
        var report = new StringBuilder();
        report.AppendLine($"## counter layer workload: {set.Units.Count} counters on bd01, {Steps} steps, two counters moved a step, {Rounds} rounds");
        foreach (var (label, build) in Builders(target, set.SetId, renderer))
        {
            // One warm round, then the measured ones; a round walks the whole sequence, as a Replay run forward does.
            var layers = sequence.Select(units => build(units).Svg).ToArray();
            GC.Collect();
            var before = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (var round = 0; round < Rounds; round++)
            {
                foreach (var units in sequence)
                {
                    build(units);
                }
            }

            timer.Stop();
            var allocated = (GC.GetAllocatedBytesForCurrentThread() - before) / (Rounds * Steps);
            var characters = layers.Average(layer => layer.Length);
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"  {label,-24} build {timer.Elapsed.TotalMilliseconds / (Rounds * Steps),7:F2} ms  allocated {allocated,12:N0} bytes a step  layer {characters,9:N0} chars"));
        }

        var layer = UnitOverlayBuilder.Build(target, set.SetId, sequence[0], renderer);
        var leads = layer.Units.ToDictionary(unit => unit.Document.Id, unit => "[" + unit.Location.Hex + "]: ", StringComparer.Ordinal);
        Led(layer.Svg, leads);
        GC.Collect();
        var b = GC.GetAllocatedBytesForCurrentThread();
        var t = Stopwatch.StartNew();
        for (var run = 0; run < Rounds * Steps; run++)
        {
            Led(layer.Svg, leads);
        }

        t.Stop();
        report.AppendLine(string.Create(CultureInfo.InvariantCulture,
            $"  {"Led's rewrite (a copy)",-24} rewrite {t.Elapsed.TotalMilliseconds / (Rounds * Steps),5:F2} ms  allocated {(GC.GetAllocatedBytesForCurrentThread() - b) / (Rounds * Steps),12:N0} bytes a step"));
        return report.ToString();
    }

    /// <summary>The ways of building the layer that the slice compares; option E adds the cache.</summary>
    private static IEnumerable<(string Label, Func<IReadOnlyList<UnitDocument>, UnitOverlay> Build)> Builders(UnitMapTarget target, string setId,
        UnitRenderer renderer)
    {
        yield return ("without a cache", units => UnitOverlayBuilder.Build(target, setId, units, renderer));
    }

    /// <summary>Step 0 is the set as it is; each later step moves two more counters one hex down their column.</summary>
    private static IReadOnlyList<UnitDocument>[] Sequence(IReadOnlyList<UnitDocument> units)
    {
        var steps = new IReadOnlyList<UnitDocument>[Steps];
        var current = units.ToArray();
        for (var step = 0; step < Steps; step++)
        {
            if (step > 0)
            {
                foreach (var index in new[] { (step * 2) % current.Length, ((step * 2) + 37) % current.Length })
                {
                    var location = BoardLocation.Parse(current[index].Location!);
                    var moved = location with { Hex = location.Hex with { RowNumber = (location.Hex.RowNumber % 10) + 1 } };
                    current[index] = current[index] with { Location = moved.ToString() };
                }
            }

            steps[step] = [.. current];
        }

        return steps;
    }

    /// <summary>A copy of <c>GameMaps.Led</c>'s rewrite of the layer's names and titles.</summary>
    private static string Led(string svg, Dictionary<string, string> lead) =>
        Regex.Replace(svg, "(<g data-unit-id=\"([^\"]+)\"[^>]*aria-label=\")([^\"]*\"[^>]*>\\s*<title>)",
            match => lead.TryGetValue(match.Groups[2].Value, out var said)
                ? match.Groups[1].Value + System.Net.WebUtility.HtmlEncode(said) + match.Groups[3].Value + System.Net.WebUtility.HtmlEncode(said)
                : match.Value);
}
