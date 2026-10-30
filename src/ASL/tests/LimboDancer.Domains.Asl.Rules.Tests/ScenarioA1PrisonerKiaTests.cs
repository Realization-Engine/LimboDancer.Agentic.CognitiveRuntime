using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The A20.54 claim, the #KIA survivor (A7.301, p. 55; A20.54, p. 88; rulings RA20.2, RA20.9, RA20.12): <see cref="ScenarioA1PrisonerCalculator.KiaSurvivor"/>,
/// its contract, the fire calculator's forward equal to its old test, and the two readings of <see cref="FireAttack.KiaUnarmedReduction"/>.
/// </summary>
public sealed class ScenarioA1PrisonerKiaTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    [Theory]
    [InlineData(false, false, false, false, KiaSurvivorEffect.Broken)]
    [InlineData(true, false, false, false, KiaSurvivorEffect.CasualtyReduction)]
    [InlineData(false, true, false, false, KiaSurvivorEffect.CasualtyReduction)]
    [InlineData(false, false, true, false, KiaSurvivorEffect.CasualtyReduction)]
    [InlineData(false, false, false, true, KiaSurvivorEffect.CasualtyReduction)]
    public void KiaSurvivorBreaksOnlyAUnitThatCanBeBroken(bool broken, bool heroic, bool berserk, bool unarmed, KiaSurvivorEffect expected)
    {
        // An armed Good Order unit breaks; a broken one, a hero or heroic leader, a berserk one, an Unarmed unit, and a prisoner (unarmed, A20.5) do not.
        Assert.Equal(expected, ScenarioA1PrisonerCalculator.KiaSurvivor(broken, heroic, berserk, unarmed));
    }

    [Fact]
    public void TheForwardEqualsTheOldTestOverEveryCombinationWithUnarmedFalse()
    {
        foreach (var broken in new[] { false, true })
        {
            foreach (var heroType in new[] { false, true })
            {
                foreach (var berserk in new[] { false, true })
                {
                    // The fire calculator's old test, copied: a survivor breaks unless broken, hero-type, or berserk.
                    var old = !broken && !heroType && !berserk;
                    Assert.Equal(old, ScenarioA1PrisonerCalculator.KiaSurvivor(broken, heroType, berserk, unarmed: false) == KiaSurvivorEffect.Broken);
                }
            }
        }
    }

    private static FireTarget Target(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireAttack Kia(bool? reading, FireTarget[] targets, IReadOnlyDictionary<string, int> selection) =>
        new FireAttack("PFPh", "phasing", true, From, At, [new FireFirer("ru-1", "defender-squad", From, false, false, false, false, false),
            new FireFirer("ru-2", "defender-squad", From, false, false, false, false, false)],
            null, 1, true, new FireLos(false, 0, true, false), null, "open-ground", targets, 3, new FireRolls([1, 1], null, null, null) { RandomSelection = selection })
        {
            FiringSideElr = 3,
            KiaUnarmedReduction = reading,
        };

    private static FireResolution Resolve(FireAttack attack)
    {
        // Any further check is passed (2 and 2), so only the #KIA's own effects show.
        var rolls = attack.Rolls!;
        for (var step = 0; step < 20; step++)
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

            rolls = kind switch
            {
                "checks" => rolls with { Checks = Add(rolls.Checks, (IReadOnlyList<int>)[2, 2]) },
                "leaderLoss" => rolls with { LeaderLoss = Add(rolls.LeaderLoss, (IReadOnlyList<int>)[2, 2]) },
                "heatOfBattle" => rolls with { HeatOfBattle = Add(rolls.HeatOfBattle, (IReadOnlyList<int>)[2, 2]) },
                "woundSeverity" => rolls with { WoundSeverity = Add(rolls.WoundSeverity, 1) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void APrisonerSurvivingAKiaBreaksInAnOlderRecordAndIsCasualtyReducedUnderTheReading()
    {
        // Two 4-4-7 at PBF (16 FP), DR 2: a 1KIA. The German Guard draws the highest dr and is eliminated; the Russian prisoner and the German
        // Unarmed squad survive it, with the other German squad.
        FireTarget[] targets = [Target("de-g"), Target("ru-p", "defender-squad") with { Friendly = true, GuardId = "de-g" }, Target("de-u") with { Unarmed = true },
            Target("de-s")];
        var selection = new Dictionary<string, int>(StringComparer.Ordinal) { ["de-g"] = 6, ["ru-p"] = 1, ["de-u"] = 1, ["de-s"] = 1 };

        var older = Resolve(Kia(null, targets, selection));
        Assert.Equal("1KIA", older.Arithmetic!.Result);
        Assert.True(Effect(older, "de-g").Eliminated);
        Assert.Contains("broken-kia", Effect(older, "ru-p").Events);
        Assert.Contains("broken-kia", Effect(older, "de-u").Events);
        Assert.Contains("broken-kia", Effect(older, "de-s").Events);
        // The record serializes as before: the switch is omitted when null.
        Assert.DoesNotContain("kiaUnarmedReduction", System.Text.Json.JsonSerializer.Serialize(Kia(null, targets, selection)), StringComparison.OrdinalIgnoreCase);

        var reading = Resolve(Kia(true, targets, selection));
        Assert.Equal(older.Arithmetic!.Result, reading.Arithmetic!.Result);
        Assert.Contains("casualty-reduced-kia", Effect(reading, "ru-p").Events);
        Assert.DoesNotContain("broken-kia", Effect(reading, "ru-p").Events);
        Assert.Contains("casualty-reduced-kia", Effect(reading, "de-u").Events);
        Assert.Contains("broken-kia", Effect(reading, "de-s").Events);
    }

    private static FireUnitEffect Effect(FireResolution result, string id) => result.Effects.Single(item => item.UnitId == id);

    [Theory]
    [InlineData(false, false, false, false, "broken")]
    [InlineData(false, true, false, false, "casualty-reduction")]
    [InlineData(false, false, false, true, "casualty-reduction")]
    public void TheContractGivesTheSurvivorsVerdict(bool broken, bool heroic, bool berserk, bool unarmed, string outcome)
    {
        var facts = $$"""{"broken":{{Json(broken)}},"heroic":{{Json(heroic)}},"berserk":{{Json(berserk)}},"unarmed":{{Json(unarmed)}}}""";
        Assert.Contains($"\"outcome\":\"{outcome}\"", ScenarioA1RuleContracts.Evaluate("a7.301-kia-survivor", facts), StringComparison.Ordinal);
        Assert.Contains("\"outcome\":\"not-evaluated\"", ScenarioA1RuleContracts.Evaluate("a7.301-kia-survivor", """{"broken":false}"""), StringComparison.Ordinal);
    }

    private static string Json(bool value) => value ? "true" : "false";
}
