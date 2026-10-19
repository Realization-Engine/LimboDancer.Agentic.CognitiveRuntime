using System.Globalization;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>The counter layer fixtures of the Counter Layer Design (section 11), shared by the rendering tests and the Studio's manifest tests.</summary>
internal static class LayerFixtures
{
    /// <summary>
    /// Every example document on bd01: eight in E4 (a count badge), two upstairs and one in the cellar there, the rest one to a hex with a facing
    /// each; and a concealed counter, an uninspected one, a Dummy, and a draft beside them.
    /// </summary>
    public static IReadOnlyList<UnitDocument> Crowded(IReadOnlyList<UnitDocument> examples)
    {
        var units = new List<UnitDocument>();
        for (var index = 0; index < examples.Count; index++)
        {
            var location = index switch
            {
                < 8 => "bd01:E4:0",
                < 10 => "bd01:E4:1",
                10 => "bd01:E4:-1",
                _ => $"bd01:{(char)('A' + ((index - 11) % 10))}{((index - 11) / 10) + 6}:0",
            };
            units.Add(examples[index] with
            {
                Id = $"c{index.ToString(CultureInfo.InvariantCulture)}-{examples[index].Id}",
                Location = location,
                Facing = index > 10 ? (UnitFacing)(index % 6) : examples[index].Facing,
                Attached = [.. examples[index].Attached.Select(attached => attached with { Location = location })],
            });
        }

        var squad = examples.First(example => example.Kind == "asl:squad");
        units.Add(squad with { Id = "concealed-1", Location = "bd01:H2:0", Concealed = true });
        units.Add(squad with { Id = "uninspected-1", Location = "bd01:H2:0", Concealed = true, Uninspected = true });
        units.Add(squad with { Id = "dummy-1", Location = "bd01:I2:0", Concealed = true, Dummy = true });
        units.Add(squad with { Id = "draft:german-1", Location = "bd01:J2:0" });
        return units;
    }
}
