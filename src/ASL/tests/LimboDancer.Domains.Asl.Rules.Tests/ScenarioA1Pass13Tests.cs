using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Rally and Fire packages as revised for backlog pass 13: marsh and rubble give no rally terrain DRM (ruling R13.2), a German or Russian MMC has
/// no Self-Rally capability the catalog leaves unrecorded (ruling R13.8), and a captured MG fires with its B# two lower and its Multiple ROF one lower
/// (ruling R13.7).
/// </summary>
public sealed class ScenarioA1Pass13Tests
{
    private static readonly ScenarioA1RallyReference RallyReference = new ScenarioA1RallyPackage().Reference;
    private static readonly ScenarioA1FireReference FireReference = new ScenarioA1FirePackage().Reference;
    private const string At = "bd01:E4:0";
    private const string From = "bd01:F5:0";
    private const string Target = "bd01:G5:0";

    private static RallyAttempt Rally(string terrain, int[] dice) =>
        new("RPh", "phasing", new RallyUnit("ru-1", "defender-squad", At, true, false, false, false, false, false, false),
            new RallyLeader("ru-leader", "defender-leader", At, false, false, false), At, terrain, true, false, false, null, new RallyRolls(dice, null))
        {
            KnownEnemyInLos = true,
            Captors = [],
        };

    [Theory]
    [InlineData("marsh")]
    [InlineData("wooden-rubble")]
    [InlineData("stone-rubble")]
    public void MarshAndRubbleGiveNoRallyTerrainDrm(string terrain)
    {
        // Ruling R13.2: 3+4 = 7 with no DRM rallies against broken morale 7; no terrain modifier is listed.
        var result = ScenarioA1RallyCalculator.Resolve(Rally(terrain, [3, 4]), RallyReference);
        Assert.Empty(result.Reasons);
        Assert.DoesNotContain(result.Arithmetic!.Drm, item => item.Name.StartsWith("terrain:", StringComparison.Ordinal));
        Assert.True(result.Effect!.Rallied);
    }

    [Fact]
    public void AGermanOrRussianMmcHasNoUnrecordedSelfRally()
    {
        // Ruling R13.8: no leader and not the first MMC attempt, so the squad would need Self-Rally capability, which A10.6 gives only Finns.
        foreach (var definition in new[] { "defender-squad", "attacker-squad" })
        {
            var attempt = Rally("open-ground", [2, 2]) with
            {
                Unit = new RallyUnit("ru-1", definition, At, true, false, false, false, false, false, false),
                Leader = null,
                GoodOrderLeaderInLocation = false,
            };
            Assert.Contains("asl.a1.rally.self-rally-not-capable", ScenarioA1RallyCalculator.Precheck(attempt, RallyReference));
        }
    }

    private static FireAttack Fire(int[] dice, FireWeapon weapon) =>
        new("PFPh", "phasing", true, From, Target, [new FireFirer("ru-1", "defender-squad", From, false, false, false, false, false) { Weapons = [weapon] }], null, 1, true,
            new FireLos(false, 0, true, false), null, "open-ground", [new FireTarget("de-s", "attacker-squad", Target, false, false, false, false, false, false, false)], 3,
            new FireRolls(dice, null, null, null));

    private static FireResolution Complete(FireAttack attack)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with { Rolls = rolls }, FireReference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:checks:", StringComparison.Ordinal))
            {
                return result;
            }

            var ids = reason["asl.a1.fire.roll-missing:checks:".Length..].Split(',');
            var checks = rolls.Checks?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new(StringComparer.Ordinal);
            foreach (var id in ids)
            {
                checks[id] = [2, 2];
            }

            rolls = rolls with { Checks = checks };
        }

        throw new InvalidOperationException("Too many rolls.");
    }

    [Fact]
    public void ACapturedMgBreaksDownTwoSoonerAndLosesOneRof()
    {
        // The German MMG (B12, ROF 3) fired by a Russian squad: captured, B10 and ROF 2. An Original 10 malfunctions it; a colored 3 keeps no ROF.
        var captured = new FireWeapon("ge-mmg", "attacker-mmg", false, false, false) { Captured = true };
        var broken = Complete(Fire([4, 6], captured)).WeaponEffects!.Single();
        Assert.True(broken.Malfunctioned);
        Assert.False(Complete(Fire([3, 5], captured)).WeaponEffects!.Single().RateOfFireRetained);
        Assert.True(Complete(Fire([2, 5], captured)).WeaponEffects!.Single().RateOfFireRetained);

        // Not captured, it is outside the package: a unit fires only its own nationality's MG (A21.1).
        var uncaptured = ScenarioA1FireCalculator.Resolve(Fire([4, 6], captured with { Captured = null }), FireReference);
        Assert.Contains(uncaptured.Reasons, reason => reason.StartsWith("asl.a1.fire.weapon-outside", StringComparison.Ordinal));
    }
}
