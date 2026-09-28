using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

/// <summary>
/// The Fire package as revised for backlog pass 10 (rulings R10.1 to R10.14): walls and hedges, Height Advantage, fire across levels with PBF,
/// TPBF, Snap Shots, Hazardous Movement, and the marsh and rubble TEM.
/// </summary>
public sealed class ScenarioA1Pass10Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string location = From) => new(id, "defender-squad", location, false, false, false, false, false);

    private static FireTarget Target(string id) => new(id, "attacker-squad", At, false, false, false, false, false, false, false);

    private static FireAttack Attack(int[] dice, string terrain = "open-ground", int range = 3, string target = At, string from = From) =>
        new("PFPh", "phasing", true, from, target, [Firer("ru-1", from)], null, range, true, new FireLos(false, 0, true, false), null,
            terrain, [Target("de-s") with { LocationId = target }], 3, new FireRolls(dice, null, null, null));

    private static FireAttack Moving(FireAttack attack, bool assault = false) => attack with
    {
        Phase = "MPh",
        FiringSide = "non-phasing",
        FireKind = ScenarioA1FireCalculator.FirstFire,
        TargetMovement = new FireMovement(assault),
    };

    /// <summary>Resolves an attack, answering each check with 2 and 2 and each dr with 1.</summary>
    private static FireResolution Complete(FireAttack attack)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 50; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Reference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            var key = reason["asl.a1.fire.roll-missing:".Length..];
            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, ids) = (key[..split], key[(split + 1)..].Split(','));
            Dictionary<string, T> Add<T>(IReadOnlyDictionary<string, T>? existing, T value)
            {
                var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
                foreach (var id in ids)
                {
                    next[id] = value;
                }

                return next;
            }

            IReadOnlyList<int> dice = [2, 2];
            rolls = kind switch
            {
                "randomSelection" => rolls with { RandomSelection = Add(rolls.RandomSelection, 1) },
                "checks" => rolls with { Checks = Add(rolls.Checks, dice) },
                "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, dice) },
                "woundSeverity" => rolls with { WoundSeverity = Add(rolls.WoundSeverity, 1) },
                "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, dice) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    private static IReadOnlyList<(string Name, decimal Value)> Drm(FireResolution result) => [.. result.Arithmetic!.Drm.Select(item => (item.Name, item.Value))];

    private static IReadOnlyList<string> Multipliers(FireResolution result) => [.. result.Arithmetic!.Firers[0].Multipliers.Select(item => item.Name)];

    [Fact]
    public void AWallReplacesALowerInHexTemAndLeavesNoFfmo()
    {
        // B9.3, B9.31 (ruling R10.5): a moving squad in Open Ground behind a wall takes +2, not FFMO.
        var wall = Complete(Moving(Attack([4, 4])) with { HexsideTem = new FireHexsideTem("wall", 2) });
        Assert.Contains(("wall", 2m), Drm(wall));
        Assert.DoesNotContain(Drm(wall), item => item.Name == "ffmo");
        Assert.Contains(("ffnam", -1m), Drm(wall));

        // Not cumulative: the wooden building's +2 stands against a hedge's +1, and a wall's +2 replaces the woods' +1.
        var hedge = Complete(Attack([4, 4], "wooden-building") with { HexsideTem = new FireHexsideTem("hedge", 1) });
        Assert.Equal([("tem:wooden-building", 2m)], Drm(hedge).Where(item => item.Name.StartsWith("tem:", StringComparison.Ordinal) || item.Name is "wall" or "hedge"));
        var woods = Complete(Attack([4, 4], "woods") with { HexsideTem = new FireHexsideTem("wall", 2) });
        Assert.Contains(("wall", 2m), Drm(woods));
        Assert.DoesNotContain(Drm(woods), item => item.Name == "tem:woods");

        // More than the printed TEM is outside the package.
        var wrong = ScenarioA1FireCalculator.Resolve(Attack([4, 4]) with { HexsideTem = new FireHexsideTem("hedge", 2) }, Reference);
        Assert.Contains("asl.a1.fire.terrain-fact-outside", wrong.Reasons);
    }

    [Fact]
    public void HeightAdvantageIsPlusOneOnlyWithNoOtherPositiveTem()
    {
        // B10.31 (ruling R10.4): +1 in Open Ground, and no FFMO against a moving target.
        var open = Complete(Moving(Attack([4, 4])) with { HeightAdvantage = true, SameLevel = false, TargetLevelAbove = 1 });
        Assert.Contains(("height-advantage", 1m), Drm(open));
        Assert.DoesNotContain(Drm(open), item => item.Name == "ffmo");

        var woods = Complete(Attack([4, 4], "woods") with { HeightAdvantage = true, SameLevel = false, TargetLevelAbove = 1 });
        Assert.DoesNotContain(Drm(woods), item => item.Name == "height-advantage");
        var walled = Complete(Attack([4, 4]) with { HeightAdvantage = true, HexsideTem = new FireHexsideTem("hedge", 1), SameLevel = false, TargetLevelAbove = 1 });
        Assert.DoesNotContain(Drm(walled), item => item.Name == "height-advantage");
    }

    [Fact]
    public void FireAcrossLevelsIsDecidedWithPbfAtMostOneLevelUp()
    {
        // A7.21 (ruling R10.4): an adjacent target one level up takes PBF; two levels up does not.
        var oneUp = Complete(Attack([4, 4], "stone-building", range: 1) with { SameLevel = false, TargetLevelAbove = 1 });
        Assert.Equal("resolved", oneUp.Disposition);
        Assert.Contains("point-blank-fire", Multipliers(oneUp));
        var twoUp = Complete(Attack([4, 4], "stone-building", range: 1) with { SameLevel = false, TargetLevelAbove = 2 });
        Assert.DoesNotContain("point-blank-fire", Multipliers(twoUp));
        var below = Complete(Attack([4, 4], "stone-building", range: 1) with { SameLevel = false, TargetLevelAbove = -2 });
        Assert.Contains("point-blank-fire", Multipliers(below));

        // A target at another level names how far above the firer it is.
        var unnamed = ScenarioA1FireCalculator.Resolve(Attack([4, 4]) with { SameLevel = false }, Reference);
        Assert.Contains("asl.a1.fire.fact-missing:targetLevelAbove", unnamed.Reasons);
    }

    [Fact]
    public void TpbfTriplesTheFpInTheFirersOwnLocation()
    {
        // A7.21 (ruling R10.14): a squad firing at the enemy stack that entered its Location.
        var tpbf = Complete(Moving(Attack([5, 5], range: 0, target: From)));
        Assert.Equal("resolved", tpbf.Disposition);
        Assert.Contains("triple-point-blank-fire", Multipliers(tpbf));
        Assert.Equal(12m, tpbf.Arithmetic!.TotalFirepower);

        // Range 0 at another Location is out of range.
        var elsewhere = ScenarioA1FireCalculator.Resolve(Attack([5, 5], range: 0), Reference);
        Assert.Contains("asl.a1.fire.out-of-range", elsewhere.Reasons);
    }

    [Fact]
    public void ASnapShotIsAreaFireWithNoTemFfmoOrFfnam()
    {
        // A8.15 (ruling R10.13): 4 FP halved to 2, and the woods' TEM, FFMO, and FFNAM do not apply.
        var snap = Complete(Moving(Attack([3, 3], "woods")) with { SnapShot = true });
        Assert.Contains("snap-shot", Multipliers(snap));
        Assert.DoesNotContain(Drm(snap), item => item.Name is "tem:woods" or "ffmo" or "ffnam");

        // Only as Defensive First Fire in the MPh.
        var prep = ScenarioA1FireCalculator.Resolve(Attack([3, 3]) with { SnapShot = true }, Reference);
        Assert.Contains("asl.a1.fire.terrain-fact-outside", prep.Reasons);
    }

    [Fact]
    public void HazardousMovementIsMinusTwoWithNoFfmoOrFfnam()
    {
        // A4.62 (ruling R10.8): a crew pushing its Gun.
        var push = Complete(Moving(Attack([5, 5])) with { HazardousMovement = true });
        Assert.Contains(("hazardous-movement", -2m), Drm(push));
        Assert.DoesNotContain(Drm(push), item => item.Name is "ffmo" or "ffnam");
    }

    [Fact]
    public void MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem()
    {
        // B16.3, B24.3 (ruling R10.1).
        var marsh = Complete(Moving(Attack([4, 4], "marsh")));
        Assert.DoesNotContain(Drm(marsh), item => item.Name is "ffmo" || item.Name.StartsWith("tem:", StringComparison.Ordinal));
        var rubble = Complete(Attack([4, 4], "stone-rubble"));
        Assert.Contains(("tem:stone-rubble", 3m), Drm(rubble));
    }

    [Fact]
    public void AWallTemLowersTheResidualFpLeft()
    {
        // B9.31 (ruling R10.5): as a Hindrance does, a claimed wall TEM lowers the Residual FP counter left.
        var open = Complete(Moving(Attack([6, 6])));
        var walled = Complete(Moving(Attack([6, 6])) with { HexsideTem = new FireHexsideTem("wall", 2) });
        Assert.NotNull(open.Arithmetic!.ResidualFp);
        Assert.True(walled.Arithmetic!.ResidualFp is null || walled.Arithmetic.ResidualFp < open.Arithmetic.ResidualFp);

        // A8.223 (referee, pass 10): a Snap Shot leaves no Residual FP.
        Assert.Null(Complete(Moving(Attack([6, 6])) with { SnapShot = true }).Arithmetic!.ResidualFp);
    }
}
