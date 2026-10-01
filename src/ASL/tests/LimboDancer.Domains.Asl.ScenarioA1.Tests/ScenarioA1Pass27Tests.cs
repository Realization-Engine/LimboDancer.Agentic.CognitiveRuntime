using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// Pass 27 of the Card Play and Map Studio Redesign Plan (ruling R27.1): the Axis Minor counters of catalog 1.13.0, their Heat of Battle (A15.1, A25.82),
/// Leader Creation (A18.2), Replacement (A19.13, A25.84), and Battle Hardening (A15.3, A25.84); and a nationality the game has not reviewed refused.
/// </summary>
public sealed class ScenarioA1Pass27Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;

    private static FireDefinition Definition(string id) => Reference.Definitions[id];

    [Fact]
    public void TheAxisMinorCountersAreTheChartsValues()
    {
        // National Capabilities Chart (p. 695): E 4-4-7, boxed 1 5-3-7, 1 3-4-7, C 3-3-6.
        var elite = Definition("axis-minor-elite-squad");
        Assert.Equal(("axis-minor", "elite", 4, 4, 7), (elite.Nationality, elite.Class, elite.Firepower, elite.Range, elite.Morale));
        var smg = Definition("axis-minor-square-squad");
        Assert.Equal(("1st-line", 5, 3, 7), (smg.Class, smg.Firepower, smg.Range, smg.Morale));
        var line = Definition("axis-minor-squad");
        Assert.Equal((3, 4, 7), (line.Firepower, line.Range, line.Morale));
        var conscript = Definition("axis-minor-conscript-squad");
        Assert.Equal(("conscript", 3, 3, 6), (conscript.Class, conscript.Firepower, conscript.Range, conscript.Morale));
        Assert.Equal("axis-minor", Definition("axis-minor-leader-8-0").Nationality);
        Assert.Equal("axis-minor", Definition("axis-minor-hero").Nationality);
    }

    [Fact]
    public void AxisMinorHeatOfBattleTakesPlusThreeAndNonEliteMmcSurrenderOnTen()
    {
        // A15.1 (p. 83): +3 for Axis Minors; A25.82: a non-elite Axis Minor surrenders on a Final DR of 10 or more.
        var line = ScenarioA1HeatOfBattle.Resolve(Definition("axis-minor-squad"), false, null, false, [4, 3], Reference.Definitions, true, []).Outcome!;
        Assert.Contains(line.Drm, item => item.Name == "nationality:axis-minor" && item.Value == 3m);
        Assert.Equal((10, HeatOfBattleOutcome.Surrender), (line.FinalDr, line.Result));

        // The elite 4-4-7 takes +3 and -1: a Final 9 is Berserk.
        Assert.Equal(HeatOfBattleOutcome.Berserk,
            ScenarioA1HeatOfBattle.Resolve(Definition("axis-minor-elite-squad"), false, null, false, [4, 3], Reference.Definitions, true, []).Outcome!.Result);

        // A25.82, A25.8 (ruling R27.1): Hungarians fighting Romanians face No Quarter, which turns the Surrender of 10 or 11 into Berserk.
        Assert.Equal(HeatOfBattleOutcome.Berserk,
            ScenarioA1HeatOfBattle.Resolve(Definition("axis-minor-squad"), false, null, false, [4, 3], Reference.Definitions, true, [], noQuarter: true).Outcome!.Result);
    }

    [Fact]
    public void AxisMinorLeaderCreationHasNoNationalityDrmAndAnUnreviewedNationalityIsRefused()
    {
        // A18.2 (p. 694): the table lists no drm for Axis Minors, and the leader is an Axis Minor counter.
        var created = ScenarioA1FieldPromotion.Create(Definition("axis-minor-squad"), 7, 1, [], Reference.Definitions, "asl.a1.rally");
        Assert.NotNull(created.Outcome);
        Assert.DoesNotContain(created.Outcome!.Drm, item => item.Name.StartsWith("nationality:", StringComparison.Ordinal));
        Assert.StartsWith("axis-minor-leader-", created.Outcome.LeaderDefinitionId, StringComparison.Ordinal);

        // Ruling R27.1: a nationality the game has not reviewed is refused, not given no drm.
        var unknown = Definition("attacker-squad") with
        {
            Nationality = "greek"
        };
        Assert.Equal("asl.a1.rally.leader-creation-nationality-unreviewed:greek",
            ScenarioA1FieldPromotion.Create(unknown, 7, 1, [], Reference.Definitions, "asl.a1.rally").Undecided);
    }

    [Fact]
    public void AxisMinorReplacementAndBattleHardeningFollowA2584()
    {
        // A25.84: the 5-3-7 and its 2-2-7 are Replaced by Conscripts and become Fanatic when Battle Hardened; a Conscript hardens to a 3-4-7.
        Assert.Equal("axis-minor-conscript-squad", ScenarioA1FireReference.ReplacementOf("axis-minor-square-squad"));
        Assert.Equal("axis-minor-conscript-half-squad", ScenarioA1FireReference.ReplacementOf("axis-minor-square-half-squad"));
        Assert.True(ScenarioA1FireReference.IsHighestQuality("axis-minor-square-squad"));
        Assert.Equal("axis-minor-squad", ScenarioA1FireReference.HardenedOf("axis-minor-conscript-squad"));

        // A19.13, A15.3: the elite by the 3-4-7, the 3-4-7 by the Conscript; the 3-4-7 hardens to the elite, never to the 5-3-7 (its range would fall),
        // and the 5-3-7's 2-2-7 is Fanatic rather than a 2-4-7 (referee, pass 27).
        Assert.Equal("axis-minor-squad", ScenarioA1FireReference.ReplacementOf("axis-minor-elite-squad"));
        Assert.Equal("axis-minor-conscript-squad", ScenarioA1FireReference.ReplacementOf("axis-minor-squad"));
        Assert.Equal("axis-minor-elite-squad", ScenarioA1FireReference.HardenedOf("axis-minor-squad"));
        Assert.True(ScenarioA1FireReference.IsHighestQuality("axis-minor-square-half-squad"));
        Assert.Equal("axis-minor-leader-9-1", ScenarioA1FireReference.HardenedOf("axis-minor-leader-8-1"));
    }
}
