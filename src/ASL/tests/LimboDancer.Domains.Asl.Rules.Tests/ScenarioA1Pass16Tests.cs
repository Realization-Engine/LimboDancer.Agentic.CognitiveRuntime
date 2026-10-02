using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The packages as revised for backlog pass 16 (rulings R16.2, R16.3, R16.7, R16.11 to R16.14): the Low Visibility DRM, fire at a Gunflash beyond
/// NVR, the Mud and Deep Snow cushion, the Extreme Winter B# and Fate, and the night Ambush.
/// </summary>
public sealed class ScenarioA1Pass16Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id, string definition = "defender-squad") => new(id, definition, From, false, false, false, false, false);

    private static FireTarget Target(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireAttack Attack(int[] dice, FireFirer[]? firers = null, FireTarget[]? targets = null, FireDirector? director = null, int range = 1,
        string terrain = "stone-building", int hindrance = 0) =>
        new("PFPh", "phasing", true, From, At, firers ?? [Firer("ru-1")], director, range, true, new FireLos(false, hindrance, true, false), null,
            terrain, targets ?? [Target("de-s")], 3, new FireRolls(dice, null, null, null));

    private static FireFirer Flamethrower(string id = "ru-1", string definition = "defender-squad") =>
        Firer(id, definition) with { Weapons = [new FireWeapon("ru-ft", "defender-ft", false, false, false)], UsesInherentFp = false, UsesSupportWeapon = true };

    /// <summary>Resolves an attack, supplying each further roll with a fixed value: checks as given (2 and 2 by default), drs of 1.</summary>
    private static FireResolution Complete(FireAttack attack, int[]? checks = null, int molCheck = 1)
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
            if (key == "molCheck")
            {
                rolls = rolls with { MolCheck = molCheck };
                continue;
            }

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

            IReadOnlyList<int> dice = checks ?? [2, 2];
            rolls = kind switch
            {
                "randomSelection" => rolls with { RandomSelection = Add(rolls.RandomSelection, 1) },
                "checks" => rolls with { Checks = Add(rolls.Checks, dice) },
                "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, dice) },
                "woundSeverity" => rolls with { WoundSeverity = Add(rolls.WoundSeverity, 1) },
                "weaponSelection" => rolls with { WeaponSelection = Add(rolls.WeaponSelection, 1) },
                "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, dice) },
                "berserkCheck" => rolls with { BerserkChecks = Add(rolls.BerserkChecks, dice) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void TheLowVisibilityDrmIsAddedAndBoundedAndNeverTakenByResidualFp()
    {
        // E1.7, E3.32 (rulings R16.3, R16.11): +1 at night, as the game supplies it.
        var night = Complete(Attack([3, 3]) with { LowVisibilityDrm = 1 });
        Assert.Contains(night.Arithmetic!.Drm, item => item.Name == "lv-hindrance" && item.Value == 1m);
        Assert.DoesNotContain(Complete(Attack([3, 3])).Arithmetic!.Drm, item => item.Name == "lv-hindrance");
        Assert.DoesNotContain("asl.a1.fire.weather-outside", ScenarioA1FireCalculator.Resolve(Attack([3, 3]) with { LowVisibilityDrm = 5 }, Reference).Reasons);
        Assert.Contains("asl.a1.fire.weather-outside", ScenarioA1FireCalculator.Resolve(Attack([3, 3]) with { LowVisibilityDrm = 6 }, Reference).Reasons);
    }

    [Fact]
    public void FireAtAGunflashBeyondNvrIsHalvedOnceOnly()
    {
        // E1.81 (ruling R16.2): halved as Area Fire; at a concealed target, halved once.
        var known = Complete(Attack([3, 3]) with { BeyondNvr = true });
        Assert.Contains(known.Arithmetic!.Firers.SelectMany(item => item.Multipliers), item => item.Name == "area-fire-gunflash");
        var concealed = Complete(Attack([3, 3], targets: [Target("de-s") with { Concealed = true }]) with { BeyondNvr = true });
        var multipliers = concealed.Arithmetic!.Firers.SelectMany(item => item.Multipliers).ToArray();
        Assert.Contains(multipliers, item => item.Name == "area-fire-concealed-target");
        Assert.DoesNotContain(multipliers, item => item.Name == "area-fire-gunflash");
    }

    [Fact]
    public void ExtremeWinterLowersAMgsBreakdownNumber()
    {
        // E3.741 (ruling R16.14): two lower for early Axis, so an Original DR two below the B# malfunctions the MG.
        var breakdown = Reference.Definitions["defender-lmg"].Breakdown!.Value;
        var dice = breakdown - 2 >= 7 ? new[] { breakdown - 2 - 3, 3 } : [3, 3];
        FireFirer Gunner() => Firer("ru-1") with { Weapons = [new FireWeapon("ru-lmg", "defender-lmg", false, false, false)], UsesSupportWeapon = true };
        var plain = Complete(Attack(dice, firers: [Gunner()]));
        var winter = Complete(Attack(dice, firers: [Gunner()]) with { BreakdownReduction = 2 });
        Assert.False(plain.WeaponEffects!.Single().Malfunctioned);
        Assert.True(winter.WeaponEffects!.Single().Malfunctioned);
    }
}
