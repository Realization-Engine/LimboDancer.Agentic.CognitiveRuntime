using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Fire package's tables for the German squads the scenario cards added (pass 19b, ruling R19.7): the circled-E 5-4-8 and its 2-3-8 HS, and the
/// plain-E 8-3-8 and its 3-3-8 HS (National Capabilities Chart, p. 695), in Casualty Reduction, Replacement beyond ELR, and Battle Hardening.
/// </summary>
public sealed class ScenarioA1Pass19bTests
{
    private const string Circled = "attacker-elite-squad-5-4-8";
    private const string CircledHs = "attacker-elite-half-squad-2-3-8";
    private const string Engineer = "attacker-elite-squad-8-3-8";
    private const string EngineerHs = "attacker-elite-half-squad-3-3-8";
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireFirer Firer(string id) => new(id, "defender-squad", From, false, false, false, false, false);

    private static FireTarget Target(string definition) => new("de-t", definition, At, false, false, false, false, false, false, false);

    // Two Russian 4-4-7 squads at a German unit in the open, Point Blank: 16 FP.
    private static FireAttack Attack(int[] dice, string definition, int elr) =>
        new FireAttack("PFPh", "phasing", true, From, At, [Firer("ru-1"), Firer("ru-2")], null, 1, true,
            new FireLos(false, 0, true, false), null, "open-ground", [Target(definition)], 3, new FireRolls(dice, null, null, null)) with
        {
            TargetSideElr = elr,
        };

    /// <summary>Resolves an attack, answering each check with the given dice and every other roll with 1.</summary>
    private static FireUnitEffect Resolve(FireAttack attack, int[] checks)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with { Rolls = rolls }, Reference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                Assert.Equal(FireResolution.Resolved, result.Disposition);
                return Assert.Single(result.Effects);
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
                "randomSelection" => rolls with { RandomSelection = Add(rolls.RandomSelection, 1) },
                "checks" => rolls with { Checks = Add<IReadOnlyList<int>>(rolls.Checks, checks) },
                "heatOfBattle" => rolls with { HeatOfBattle = Add<IReadOnlyList<int>>(rolls.HeatOfBattle, [3, 4]) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void TheTablesKnowTheCardSquads()
    {
        // A1.31, A7.302: each squad's HS, and the squad two of them Recombine into.
        Assert.Equal(CircledHs, ScenarioA1FireReference.HalfSquadOf(Circled));
        Assert.Equal(EngineerHs, ScenarioA1FireReference.HalfSquadOf(Engineer));
        Assert.Equal(Circled, ScenarioA1FireReference.SquadOf(CircledHs));
        Assert.Equal(Engineer, ScenarioA1FireReference.SquadOf(EngineerHs));

        // A19.13: the 5-4-8 falls to the 2nd Line 4-4-7 (none of its factors rises, its Class drops), its HS to the 2-3-7. The underscored 3-3-8
        // is never Replaced, but a Casualty MC beyond the 8-3-8's ELR leaves the 2-3-7 (referee, pass 19b).
        Assert.Equal("attacker-2nd-line-squad", ScenarioA1FireReference.ReplacementOf(Circled));
        Assert.Equal("attacker-2nd-line-half-squad", ScenarioA1FireReference.ReplacementOf(CircledHs));
        Assert.Null(ScenarioA1FireReference.ReplacementOf(EngineerHs));
        Assert.Equal("attacker-2nd-line-half-squad", ScenarioA1FireReference.CasualtyHalfSquadOf(EngineerHs));
        Assert.Equal("attacker-2nd-line-half-squad", ScenarioA1FireReference.CasualtyHalfSquadOf(CircledHs));

        // A15.3: elite MMC become Fanatic; nothing is higher.
        foreach (var id in new[] { Circled, CircledHs, Engineer, EngineerHs })
        {
            Assert.True(ScenarioA1FireReference.IsHighestQuality(id));
            Assert.Null(ScenarioA1FireReference.HardenedOf(id));
            Assert.True(Reference.Definitions.ContainsKey(id));
        }
    }

    /// <summary>Every definition the embedded scenario cards field, read from the cards themselves (referee, pass 19b).</summary>
    private static IReadOnlyList<string> CardDefinitions()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src", "ASL", "units", "scenarios")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        var definitions = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(Path.Combine(directory.FullName, "src", "ASL", "units", "scenarios"), "*.scenario-card.json"))
        {
            using var card = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var group in card.RootElement.GetProperty("sides").EnumerateArray().SelectMany(side => side.GetProperty("groups").EnumerateArray()))
            {
                foreach (var unit in group.GetProperty("units").EnumerateArray())
                {
                    definitions.Add(unit.GetProperty("definition").GetString()!);
                }
            }
        }

        return [.. definitions];
    }

    [Fact]
    public void EveryPackageKnowsTheCardCounters()
    {
        // Every counter the cards field is a definition of the Fire, Rally, and Close Combat packages, with its HS for a squad; every MMC has a CC BPV
        // (A18.2; referee, pass 19b); and the SW that fire as ordnance are Guns of the Ordnance package.
        var rally = new ScenarioA1RallyPackage().Reference.Definitions;
        var closeCombat = new ScenarioA1CloseCombatPackage().Reference;
        var ordnance = new ScenarioA1OrdnancePackage().Reference.Guns;
        var definitions = CardDefinitions();
        Assert.Contains(Circled, definitions);
        Assert.Contains(Engineer, definitions);
        foreach (var id in definitions.Concat([CircledHs, EngineerHs]))
        {
            var definition = Reference.Definitions[id];
            Assert.True(rally.ContainsKey(id), id);
            Assert.True(closeCombat.Definitions.ContainsKey(id), id);
            if (definition.Kind == "asl:squad")
            {
                Assert.NotNull(ScenarioA1FireReference.HalfSquadOf(id));
            }

            if (definition.IsMmc)
            {
                Assert.True(closeCombat.Bpv.ContainsKey(id), id);
            }

            if (definition.Kind is "asl:light-mortar" or "asl:latw")
            {
                Assert.True(ordnance.ContainsKey(id), id);
            }
        }

        Assert.Equal(13, closeCombat.Bpv[Circled]);
        Assert.Equal(6, closeCombat.Bpv[EngineerHs]);
    }

    [Fact]
    public void AnEngineerSquadWithItsHalfSquadAndAFirstLineSquadMayAttackInCloseCombat()
    {
        // A18.2 (ruling R14.12; referee, pass 19b): an attack mixing definitions needs each MMC's BPV; the 8-3-8 (16) founds a created leader.
        var reference = new ScenarioA1CloseCombatPackage().Reference;
        static CloseCombatUnit Unit(string id, string definition, string side) =>
            new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);
        CloseCombatUnit[] units = [Unit("g1", Engineer, "german"), Unit("g2", EngineerHs, "german"), Unit("g3", "attacker-squad", "german"),
            Unit("r1", "defender-squad", "russian")];
        var facts = new CloseCombatFacts("CCPh", "bd01:G5:0", "open-ground", "german", CloseCombatFacts.Simultaneous, null, units, [], [],
            [new CloseCombatDeclaration(["g1", "g2", "g3"], ["r1"])], new CloseCombatRolls(new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["0"] = [1, 1] })
            {
                LeaderCreation = new Dictionary<string, int>(StringComparer.Ordinal) { ["0"] = 6 },
            });
        var result = ScenarioA1CloseCombatCalculator.Resolve(facts, reference);
        Assert.DoesNotContain(result.Reasons, reason => reason.Contains("field-promotion-bpv-missing", StringComparison.Ordinal));
        Assert.Contains(result.Attacks[0].LeaderCreation!.Drm, item => item.Name == "morale-8-or-more");
    }

    [Theory]
    [InlineData(Circled, CircledHs)]
    [InlineData(Engineer, EngineerHs)]
    public void CasualtyReductionLeavesTheSquadsOwnHalfSquad(string squad, string half)
    {
        // A7.302: a K/3 result (DR 4, not doubles: Russians Cower on doubles) Reduces the squad to its HS, which passes its MC.
        var effect = Resolve(Attack([1, 3], squad, 3), [1, 2]);
        Assert.Equal(half, effect.FinalDefinitionId);
        Assert.False(effect.Broken);
    }

    [Fact]
    public void TheCircledESquadIsReplacedBeyondItsElr()
    {
        // A19.13: ELR 1, a 1MC (DR 9) failed by four Replaces the 5-4-8 by a broken 4-4-7, and its HS by a broken 2-3-7. An Original 12 would
        // be a Casualty MC (A10.31), so the check is 11.
        var squad = Resolve(Attack([4, 5], Circled, 1), [6, 5]);
        Assert.Equal(("attacker-2nd-line-squad", true), (squad.FinalDefinitionId, squad.Broken));
        Assert.Contains("replaced-elr", squad.Events);
        var half = Resolve(Attack([4, 5], CircledHs, 1), [6, 5]);
        Assert.Equal(("attacker-2nd-line-half-squad", true), (half.FinalDefinitionId, half.Broken));
    }

    [Fact]
    public void ACasualtyMcBeyondTheCircledESquadsElrLeavesABrokenHalfSquadOfLesserQuality()
    {
        // A7.302, A19.13: a K/3 Reduces the 5-4-8 to its 2-3-8, whose MC failed by six, beyond ELR 1, Replaces it by a broken 2-3-7.
        var reduced = Resolve(Attack([1, 3], Circled, 1), [6, 5]);
        Assert.Equal(("attacker-2nd-line-half-squad", true), (reduced.FinalDefinitionId, reduced.Broken));
        Assert.Equal(["casualty-reduced-k", "replaced-elr"], reduced.Events);

        // A10.31, A19.13 EXC: an Original 12 on a 1MC is a Casualty MC; failed beyond ELR 1, it Reduces the 5-4-8 to a broken 2-3-7.
        var casualty = Resolve(Attack([4, 5], Circled, 1), [6, 6]);
        Assert.Equal(("attacker-2nd-line-half-squad", true), (casualty.FinalDefinitionId, casualty.Broken));
        Assert.Contains("casualty-reduced-beyond-elr", casualty.Events);

        // The 8-3-8's underscored 3-3-8 is never Replaced: a Casualty MC beyond its ELR 5 (Encircled, so an Original 12 fails by six) leaves a broken
        // 2-3-7 (ruling R19.7).
        var engineer = Resolve(Attack([4, 5], Engineer, 1) with { Targets = [Target(Engineer) with { Encircled = true }] }, [6, 6]);
        Assert.Equal(("attacker-2nd-line-half-squad", true), (engineer.FinalDefinitionId, engineer.Broken));
        Assert.Contains("casualty-reduced-beyond-elr", engineer.Events);
    }

    [Fact]
    public void TheUnderscoredEngineerSquadIsReplacedByItsTwoBrokenHalfSquads()
    {
        // A19.13 EXC (ruling R15.9): the 8-3-8's underscored Morale Factor gives it ELR 5, whatever its side's. Not Encircled, a 2MC (DR 7) on 11 fails
        // by five: beyond the side's ELR 1 but not its own 5, so it only breaks (referee, pass 19b).
        var byFive = Resolve(Attack([3, 4], Engineer, 1), [6, 5]);
        Assert.Equal((Engineer, true), (byFive.FinalDefinitionId, byFive.Broken));
        Assert.DoesNotContain("replaced-by-two-half-squads", byFive.Events);

        // Encircled (morale 7, A7.7), the same check fails by six and Replaces it by its two broken 3-3-8.
        var effect = Resolve(Attack([3, 4], Engineer, 1) with { Targets = [Target(Engineer) with { Encircled = true }] }, [6, 5]);
        Assert.Equal((EngineerHs, true), (effect.FinalDefinitionId, effect.Broken));
        Assert.Contains("replaced-by-two-half-squads", effect.Events);

        // A 3-3-8 is Disrupted instead.
        var half = Resolve(Attack([3, 4], EngineerHs, 1) with { Targets = [Target(EngineerHs) with { Encircled = true }] }, [6, 5]);
        Assert.Equal((EngineerHs, true, true), (half.FinalDefinitionId, half.Broken, half.Disrupted));
    }

    [Theory]
    [InlineData(Circled)]
    [InlineData(Engineer)]
    public void BattleHardeningMakesTheCardSquadsFanatic(string squad)
    {
        // A15.3: an already elite MMC that is Battle Hardened becomes Fanatic.
        var outcome = ScenarioA1HeatOfBattle.Resolve(Reference.Definitions[squad], false, null, false, [4, 4], Reference.Definitions, true, []).Outcome!;
        Assert.True(outcome.Fanatic);
    }
}
