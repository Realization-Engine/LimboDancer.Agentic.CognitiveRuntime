using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The packages as the backlog pass 5 revises them: CX adds one to the IFT DR once (A4.51) and changes CC and Ambush (R5.2); a unit takes a
/// second Heat of Battle DR in one attack under its own roll key (A15.1, R5.10); Battle Hardening and the Unlikely Kill dr are the owners'
/// options once answers are declared (A15.3, A7.309, R5.8, R5.9); and a side faced with No Quarter treats a Surrender as Berserk (A20.3,
/// A15.5, R5.6).
/// </summary>
public sealed class ScenarioA1Pass5Tests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static readonly FireDirector Leader = new("ru-l", "defender-leader", From, false, false, false, false, false);

    private static FireFirer Firer(string id, bool cx = false) => new(id, "defender-squad", From, false, false, false, false, false) { Cx = cx ? true : null };

    private static FireTarget German(string id, string definition = "attacker-squad") => new(id, definition, At, false, false, false, false, false, false, false);

    private static FireTarget Russian(string id, string definition) => new(id, definition, From, false, false, false, false, false, false, false);

    private static FireAttack Attack(int[] dice, FireFirer[] firers, FireTarget[] targets, FireDirector? director = null, int hindrance = 0) =>
        new("PFPh", "phasing", true, From, At, firers, director, 2, true, new FireLos(false, hindrance, true, false), null, "open-ground", targets, 3,
            new FireRolls(dice, null, null, null));

    [Fact]
    public void ACxFirerOrDirectorAddsOneToTheIftDrOnce()
    {
        // A4.51, R5.2: two CX firers and a CX leader add +1 once; with no CX unit the DR has no such DRM.
        var cx = ScenarioA1FireCalculator.Resolve(Attack([6, 6], [Firer("ru-1", cx: true), Firer("ru-2", cx: true)], [German("de-s")], Leader with
        {
            Cx = true
        }), Reference);
        Assert.Equal(FireResolution.Resolved, cx.Disposition);
        Assert.Single(cx.Arithmetic!.Drm, item => item.Name.StartsWith("cx:", StringComparison.Ordinal) && item.Value == 1 && item.Rule == "A4.51");

        var fresh = ScenarioA1FireCalculator.Resolve(Attack([6, 6], [Firer("ru-1"), Firer("ru-2")], [German("de-s")], Leader), Reference);
        Assert.DoesNotContain(fresh.Arithmetic!.Drm, item => item.Name.StartsWith("cx:", StringComparison.Ordinal));
        Assert.Equal(cx.Arithmetic.FinalDr, fresh.Arithmetic.FinalDr + 1);
    }

    [Fact]
    public void ASecondOriginalTwoInOneAttackCallsForASecondHeatOfBattleDr()
    {
        // K/1 on the Russian squad and leader: the leader's mortal wound eliminates him; the squad's MC and LLMC are both Original 2s, so it
        // takes two Heat of Battle DRs, the second under the key "ru-squad:2" (A15.1, R5.10). 1+1 = 2 +2 Russian: a hero; then 2+3 = 5 +2:
        // Battle Hardening of the unit the first left.
        var attack = new FireAttack("PFPh", "phasing", true, At, From,
            [new FireFirer("de-1", "attacker-squad", At, false, false, false, false, false), new FireFirer("de-2", "attacker-squad", At, false, false, false, false, false)], null,
            1, true, new FireLos(false, 0, true, false), null, "open-ground", [Russian("ru-squad", "defender-squad"), Russian("ru-leader", "defender-leader")], 2,
            new FireRolls([1, 3], new Dictionary<string, int> { ["ru-squad"] = 1, ["ru-leader"] = 6 },
                new Dictionary<string, IReadOnlyList<int>> { ["ru-squad"] = [1, 1] },
                new Dictionary<string, IReadOnlyList<int>> { ["ru-squad"] = [1, 1] },
                new Dictionary<string, int> { ["ru-leader"] = 5 })
            {
                HeatOfBattle = new Dictionary<string, IReadOnlyList<int>> { ["ru-squad"] = [1, 1], ["ru-squad:2"] = [2, 3] },
            })
        {
            Targets = [Russian("ru-squad", "defender-squad") with { KnownEnemyInLos = true, Captors = [] }, Russian("ru-leader", "defender-leader") with { KnownEnemyInLos = true, Captors = [] }],
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        var squad = result.Effects.Single(item => item.UnitId == "ru-squad");
        Assert.Equal(["MC", "LLMC"], squad.Checks.Select(check => check.Kind));
        Assert.NotNull(squad.HeatOfBattle);
        Assert.NotNull(squad.SecondHeatOfBattle);
        Assert.Equal(2, squad.Events.Count(item => item.StartsWith("heat-of-battle:", StringComparison.Ordinal)));
    }

    [Fact]
    public void WithAnswersDeclaredBattleHardeningWaitsForItsOwnerAndMayBeRefused()
    {
        // 8 FP, 3+4 = 7: a 1MC; the German squad's Original 2 passes, and its Heat of Battle DR of 3+3 = 6 is a hero and Battle Hardening
        // into the 4-6-8 (A15.21, A15.3).
        var attack = Attack([3, 4], [Firer("ru-1"), Firer("ru-2")], [German("de-s") with { KnownEnemyInLos = true, Captors = [] }], Leader) with
        {
            Rolls = new FireRolls([3, 4], null, new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = [1, 1] }, null)
            {
                HeatOfBattle = new Dictionary<string, IReadOnlyList<int>> { ["de-s"] = [3, 3] },
            },
            Choices = new Dictionary<string, string>(),
        };
        Assert.Equal(["asl.a1.fire.choice-missing:battleHardening:de-s"], ScenarioA1FireCalculator.Resolve(attack, Reference).Reasons);

        var refused = ScenarioA1FireCalculator.Resolve(attack with
        {
            Choices = new Dictionary<string, string> { ["battleHardening:de-s"] = "decline" }
        }, Reference);
        var effect = Assert.Single(refused.Effects);
        Assert.Equal(("attacker-squad", true), (effect.FinalDefinitionId, effect.HeatOfBattle!.HardeningRefused));
        Assert.Contains("hero-created:attacker-hero", effect.Events);
        Assert.Contains("battle-hardening-refused", effect.Events);

        var taken = ScenarioA1FireCalculator.Resolve(attack with
        {
            Choices = new Dictionary<string, string> { ["battleHardening:de-s"] = "take" }
        }, Reference);
        Assert.Equal("attacker-elite-squad", Assert.Single(taken.Effects).FinalDefinitionId);

        // An answer the attack never reaches is refused.
        Assert.Contains("asl.a1.fire.extra-choice:unlikelyKill:de-t",
            ScenarioA1FireCalculator.Resolve(attack with
            {
                Choices = new Dictionary<string, string> { ["battleHardening:de-s"] = "take", ["unlikelyKill:de-t"] = "take" }
            }, Reference).Reasons);
    }

    [Theory]
    [InlineData("decline", null, FireVehicleEffect.Immobilized)]
    [InlineData("take", 1, FireVehicleEffect.BurningWreck)]
    [InlineData("take", 2, FireVehicleEffect.Eliminated)]
    [InlineData("take", 5, FireVehicleEffect.Immobilized)]
    public void TheUnlikelyKillDrIsTheFirersOptionEvenAfterAnImmobilization(string answer, int? dr, string expected)
    {
        // One 4-4-7 at two hexes: 4 FP, Kill Number 5; a +3 Hindrance makes the Original 2 a 5: immobilized. The firer may still make the dr, and
        // a worse dr leaves the immobilization (A7.309, R5.8).
        var attack = Attack([1, 1], [Firer("ru-1")], [], Leader, hindrance: 3) with
        {
            Vehicles = [new FireVehicle("de-t", "attacker-truck", At, true, false, false, false)],
            Choices = new Dictionary<string, string>(),
        };
        Assert.Equal(["asl.a1.fire.choice-missing:unlikelyKill:de-t"], ScenarioA1FireCalculator.Resolve(attack, Reference).Reasons);

        var answered = attack with
        {
            Choices = new Dictionary<string, string> { ["unlikelyKill:de-t"] = answer },
            Rolls = attack.Rolls! with
            {
                UnlikelyKill = dr is { } value ? new Dictionary<string, int> { ["de-t"] = value } : null
            },
        };
        var result = ScenarioA1FireCalculator.Resolve(answered, Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        var effect = Assert.Single(result.VehicleEffects!);
        Assert.Equal((expected, dr, answer == "decline" ? true : (bool?)null), (effect.Result, effect.UnlikelyKillDr, effect.UnlikelyKillDeclined));
    }

    [Fact]
    public void ASideFacedWithNoQuarterTreatsASurrenderAsBerserk()
    {
        // A15.5 EXC and the Heat of Battle table note: 6+6 = 12 is a Surrender, but Berserk for a unit subject to No Quarter (A20.3).
        var squad = Reference.Definitions["attacker-squad"];
        var (surrender, _) = ScenarioA1HeatOfBattle.Resolve(squad, false, null, false, [6, 6], Reference.Definitions, true, ["r1"]);
        Assert.Equal(HeatOfBattleOutcome.Surrender, surrender!.Result);
        var (berserk, _) = ScenarioA1HeatOfBattle.Resolve(squad, false, null, false, [6, 6], Reference.Definitions, true, ["r1"], noQuarter: true);
        Assert.Equal(HeatOfBattleOutcome.Berserk, berserk!.Result);
        Assert.Null(berserk.Captors);
    }

    [Fact]
    public void CxChangesCloseCombatAndAmbush()
    {
        static CloseCombatUnit Unit(string id, string definition, string side, bool cx = false) =>
            new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false)
            {
                Cx = cx ? true : null
            };

        // A4.51, R5.2: +1 to the attack of a CX attacker; -1 to an attack against a CX defender, for that unit.
        var facts = new CloseCombatFacts("CCPh", At, "open-ground", "german", CloseCombatFacts.Simultaneous, null,
            [Unit("g1", "attacker-squad", "german", cx: true), Unit("r1", "defender-squad", "russian", cx: true)],
            [], [], [new CloseCombatDeclaration(["g1"], ["r1"]), new CloseCombatDeclaration(["r1"], ["g1"])],
            new CloseCombatRolls(new Dictionary<string, IReadOnlyList<int>> { ["0"] = [6, 6], ["1"] = [6, 6] }));
        var result = ScenarioA1CloseCombatCalculator.Resolve(facts, CloseCombat);
        Assert.True(result.Disposition == CloseCombatResolution.Resolved, string.Join("; ", result.Reasons));
        var german = result.Attacks[0];
        Assert.Single(german.Drm, item => item.Name.StartsWith("cx:", StringComparison.Ordinal) && item.Value == 1);
        Assert.Contains(german.Defending.Single().Drm, item => item.Name == "vs-cx" && item.Value == -1);

        var ambush = ScenarioA1CloseCombatCalculator.ResolveAmbush(new AmbushFacts("CCPh", At, "woods", "german",
            [Unit("g1", "attacker-squad", "german", cx: true) with { Advanced = true }, Unit("r1", "defender-squad", "russian")],
            new Dictionary<string, int> { ["german"] = 3, ["russian"] = 3 }), CloseCombat);
        Assert.Contains(ambush.Sides.Single(side => side.Side == "german").Drm, item => item.Name == "cx" && item.Value == 1);
        Assert.DoesNotContain(ambush.Sides.Single(side => side.Side == "russian").Drm, item => item.Name == "cx");
    }

    [Fact]
    public void ASurrenderWithNoCaptorLeavesTheUnitSubjectToASecondHeatOfBattleDr()
    {
        // As above, but the squad's first Heat of Battle DR is 6+6: a Surrender with no enemy to surrender to, which only Disrupts it (A15.5),
        // so its LLMC's Original 2 still calls for the second DR (R5.10).
        var attack = new FireAttack("PFPh", "phasing", true, At, From,
            [new FireFirer("de-1", "attacker-squad", At, false, false, false, false, false), new FireFirer("de-2", "attacker-squad", At, false, false, false, false, false)], null,
            1, true, new FireLos(false, 0, true, false), null, "open-ground", [Russian("ru-squad", "defender-squad"), Russian("ru-leader", "defender-leader")], 2,
            new FireRolls([1, 3], new Dictionary<string, int> { ["ru-squad"] = 1, ["ru-leader"] = 6 },
                new Dictionary<string, IReadOnlyList<int>> { ["ru-squad"] = [1, 1] },
                new Dictionary<string, IReadOnlyList<int>> { ["ru-squad"] = [1, 1] },
                new Dictionary<string, int> { ["ru-leader"] = 5 })
            {
                HeatOfBattle = new Dictionary<string, IReadOnlyList<int>> { ["ru-squad"] = [6, 6], ["ru-squad:2"] = [2, 3] },
            })
        {
            Targets = [Russian("ru-squad", "defender-squad") with { KnownEnemyInLos = true, Captors = [] }, Russian("ru-leader", "defender-leader") with { KnownEnemyInLos = true, Captors = [] }],
        };
        var result = ScenarioA1FireCalculator.Resolve(attack, Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        var squad = result.Effects.Single(item => item.UnitId == "ru-squad");
        Assert.Equal(HeatOfBattleOutcome.Surrender, squad.HeatOfBattle!.Result);
        Assert.NotNull(squad.SecondHeatOfBattle);
    }
}
