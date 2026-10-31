using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The wiring contract of B25.2 (p. 143; the B25.2 claim's design, ruling RB25.8): what does this Blaze add to a LOS through its Location, as
/// <see cref="ScenarioA1BlazeSmokeCalculator"/> decides it. Every fact is disclosed to All: a Blaze, a wreck, a Fire Lane, the weather, and the wind are on
/// the map or in the scenario for both sides. Asked at any LOS check: fire, Defensive First Fire, Interdiction, a Sniper.
/// </summary>
internal static class ScenarioA1BlazeSmokeContract
{
    private static readonly string[] Kinds = [.. Enum.GetNames<BlazeKind>()];

    private static readonly string[] Winds = [nameof(WindForce.None), nameof(WindForce.MildBreeze), nameof(WindForce.HeavyWinds)];

    [RuleContract]
    private static RuleContract BlazeSmoke() => new("b25.2-blaze-smoke", "Blaze SMOKE", [new("B25.2", 143), new("B25.63", 144), new("A9.222", 60)],
        [
            new("kind", ContractFactType.Choice, false, ContractDisclosure.All, "What burns: a Wreck Blaze, a terrain Blaze, or a Flame") { Choices = Kinds },
            new("wind", ContractFactType.Choice, false, ContractDisclosure.All, "The Wind Force") { Choices = Winds },
            ScenarioA1RuleContracts.Flag("interiorNonRooftop", "The Location is a non-rooftop Location of an Interior Building hex"),
            ScenarioA1RuleContracts.Flag("fireLaneResidualAttack", "The LOS is a Fire Lane's Residual FP attack (A9.22)"),
            ScenarioA1RuleContracts.Flag("rain", "It is raining (E3.5)"),
        ],
        ["smoke", "wreck-only", "none", ContractVerdict.NotEvaluated],
        [],
        new("PFPh", "2.22A"))
    {
        Decide = Verdict,
    };

    private static ContractVerdict Verdict(ContractFacts facts)
    {
        var kind = Enum.Parse<BlazeKind>(facts.Text("kind")!);
        var wind = ScenarioA1BlazeSmokeCalculator.WindFelt(Enum.Parse<WindForce>(facts.Text("wind")!), facts.Flag("interiorNonRooftop")!.Value);
        var fireLane = facts.Flag("fireLaneResidualAttack")!.Value;

        // A terrain Blaze or a Flame has no wreck: a Fire Lane's attack asks nothing of it (the design's review, finding 6).
        if (fireLane && kind != BlazeKind.WreckBlaze)
        {
            return new(ContractVerdict.NotEvaluated, [new("only a burning wreck has a wreck's Hindrance for a Fire Lane's attack", "B25.2", 143)], []);
        }

        if (kind == BlazeKind.WreckBlaze && ScenarioA1BlazeSmokeCalculator.WreckBlazeLos(fireLane, wind) is { Smoke: false })
        {
            return new("wreck-only", [new("Only the wreck's own Hindrance applies (D9.4), on a Fire Lane's attack or in Heavy Winds", "B25.2", 143)], []);
        }

        var hindrance = ScenarioA1BlazeSmokeCalculator.Hindrance(kind, wind)!.Value;
        if (hindrance == 0)
        {
            return new("none", [new(kind == BlazeKind.Flame ? "A Flame makes no smoke" : "Heavy Winds leave its SMOKE no effect (B25.63)", "B25.2", 143)], []);
        }

        var height = ScenarioA1BlazeSmokeCalculator.Height(kind, wind)!.Value;
        var drift = ScenarioA1BlazeSmokeCalculator.DriftCounters(kind, wind, facts.Flag("rain")!.Value)!.Value;
        var text = string.Create(CultureInfo.InvariantCulture, $"+{hindrance}, {height} levels above the Fire")
            + (drift > 0 ? string.Create(CultureInfo.InvariantCulture, $"; it can drift {drift} Dispersed SMOKE counters (A24.61)") : string.Empty);
        return new("smoke", [new(text, "B25.2", 143)], []);
    }
}
