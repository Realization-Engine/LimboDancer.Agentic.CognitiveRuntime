using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The D5.34 claim's Stun increment (D5.34 p. 203, D5.342 p. 204; the claim's design, rulings RD5.2 to RD5.6, RD5.14, RD5.16): the Stun calculator's
/// checks and verdicts, the forwards of the old members (each equal to a copy of its old expression over its inputs), the new readings each reading
/// switch or fact turns on (null as before; set as the rule), and the two wiring contracts.
/// </summary>
public sealed class ScenarioA1StunCalculatorTests
{
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";
    private const string From = "bd01:F5:0";
    private const string Bren = "campaign-british-bren-carrier";
    private static readonly bool[] Both = [false, true];
    private static readonly string[] KillResults = [OrdnanceKill.Dud, OrdnanceKill.None, OrdnanceKill.Burn, OrdnanceKill.Eliminated, OrdnanceKill.Immobilized,
        OrdnanceKill.Shock, OrdnanceKill.PossibleShock, OrdnanceKill.Stun];
    private static readonly string[] CrewResults = [FireVehicleEffect.None, FireVehicleEffect.Stunned, FireVehicleEffect.Recalled, FireVehicleEffect.Pinned];
    private static readonly StunDr[] Listed = [StunDr.ToHit, StunDr.MachineGunIft, StunDr.IfeIft, StunDr.FlameThrowerIft, StunDr.CloseCombat, StunDr.TaskCheck,
        StunDr.MoraleCheck, StunDr.CrewSurvival, StunDr.Overrun];
    private static readonly StunDr[] Unlisted = [StunDr.ExcessiveSpeedBreakdown, StunDr.Bog, StunDr.Other];

    private static StunFacts Facts(StunCause cause, bool? exposed = true, bool? qualifies = true, bool? armored = true, bool? closed = true, bool? failed = true,
        int? sniper = 2, int? dr = 7, int? tk = 7, bool stunned = false, bool recovery = false) =>
        new(cause, exposed, qualifies, armored, closed, failed, sniper, dr, tk, stunned, recovery)
        {
            OneManTurret = false,
            SmcTemporaryCrew = false,
        };

    // The four causes (D5.34, p. 203).

    [Fact]
    public void AFailedMcStunsOnlyACeCrewThatQualifiesForItsCeDrm()
    {
        Assert.True(ScenarioA1StunCalculator.FailedMcStuns(true, true, true));
        Assert.False(ScenarioA1StunCalculator.FailedMcStuns(true, true, false));
        Assert.False(ScenarioA1StunCalculator.FailedMcStuns(false, true, true));

        // D5.311: an Unprotected crew is "not susceptible to Stun/Recall".
        Assert.False(ScenarioA1StunCalculator.FailedMcStuns(true, false, true));
        Assert.False(ScenarioA1StunCalculator.FailedMcStuns(true, null, true));
        Assert.Contains("Unprotected", ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, qualifies: false)).Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void ASniperDrTwoStunsOnlyTheCeCrewOfAFullyArmoredAfv()
    {
        Assert.True(ScenarioA1StunCalculator.SniperStuns(true, true, 2));
        Assert.False(ScenarioA1StunCalculator.SniperStuns(false, true, 2));
        Assert.False(ScenarioA1StunCalculator.SniperStuns(true, false, 2));
        Assert.False(ScenarioA1StunCalculator.SniperStuns(true, true, 1));
        Assert.False(ScenarioA1StunCalculator.SniperStuns(true, true, 3));
        Assert.False(ScenarioA1StunCalculator.SniperStuns(true, true, null));
    }

    [Fact]
    public void AMgsFinalTkDrEqualToTheFinalTkStunsEvenABuAfv()
    {
        Assert.False(ScenarioA1StunCalculator.MachineGunStuns(6, 7));
        Assert.True(ScenarioA1StunCalculator.MachineGunStuns(7, 7));
        Assert.False(ScenarioA1StunCalculator.MachineGunStuns(8, 7));

        // A9.61: "even if BU": the crew's CE is not read.
        Assert.Equal(StunVerdict.Stunned, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, exposed: false)).Outcome);
    }

    [Fact]
    public void FallingRubbleStunsACeClosedToppedAfvOnly()
    {
        Assert.True(ScenarioA1StunCalculator.FallingRubbleStuns(true, true));
        Assert.False(ScenarioA1StunCalculator.FallingRubbleStuns(true, false));
        Assert.False(ScenarioA1StunCalculator.FallingRubbleStuns(false, true));
    }

    [Fact]
    public void AnUnknownFactIsNoStunAndLeavesTheResultNotEvaluated()
    {
        Assert.False(ScenarioA1StunCalculator.Stuns(Facts(StunCause.MachineGunToKill, dr: null)));
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, tk: null)).Outcome);
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, failed: null)).Outcome);
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, sniper: null)).Outcome);
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.FallingRubble, closed: null)).Outcome);
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr) with
        {
            OneManTurret = null
        }).Outcome);
    }

    // D5.342 and the verdict.

    [Fact]
    public void ASecondStunOnEitherSideOfTheCounterIsARecall()
    {
        Assert.False(ScenarioA1StunCalculator.SecondStun(false, false));
        Assert.True(ScenarioA1StunCalculator.SecondStun(true, false));
        Assert.True(ScenarioA1StunCalculator.SecondStun(false, true));
        Assert.True(ScenarioA1StunCalculator.SecondStun(true, true));

        Assert.Equal(StunVerdict.Stunned, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr)).Outcome);
        Assert.Equal(StunVerdict.Recalled, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, stunned: true)).Outcome);
        Assert.Equal(StunVerdict.Recalled, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, recovery: true)).Outcome);

        // A result that does not Stun leaves a Stunned crew as it is.
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, failed: false, recovery: true)).Outcome);
    }

    [Fact]
    public void A1mtAfvAndASmcTemporaryCrewAreLeftToTheirOwnRules()
    {
        var oneManTurret = ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr) with
        {
            OneManTurret = true
        });
        Assert.Equal((StunVerdict.NotEvaluated, "D5.341"), (oneManTurret.Outcome, oneManTurret.Rule));
        var smc = ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc) with
        {
            SmcTemporaryCrew = true
        });
        Assert.Equal((StunVerdict.NotEvaluated, "D5.343"), (smc.Outcome, smc.Rule));

        // Without a Stun, neither matters.
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, sniper: 4) with
        {
            OneManTurret = true
        }).Outcome);
    }

    [Fact]
    public void TheStunPlusOneModifiesTheDrD534ListsAndNothingElse()
    {
        Assert.All(Listed, dr => Assert.Equal(1, ScenarioA1StunCalculator.Drm(dr, true)));
        Assert.All(Listed, dr => Assert.Equal(0, ScenarioA1StunCalculator.Drm(dr, false)));
        Assert.All(Unlisted, dr => Assert.Equal(0, ScenarioA1StunCalculator.Drm(dr, true)));
    }

    [Fact]
    public void AStunSetsBuAndStopsAndFlipsAtThePlayerTurnsEnd()
    {
        Assert.Equal([(VehicleCondition.Stunned, true), (VehicleCondition.ButtonedUp, true), (VehicleCondition.Motion, false)],
            ScenarioA1StunCalculator.StunnedConditions());
        Assert.Equal([(UnitCondition.Stunned, false), (UnitCondition.StunRecovery, true)], ScenarioA1StunCalculator.PlayerTurnEnd(true, true));
        Assert.Empty(ScenarioA1StunCalculator.PlayerTurnEnd(true, false));
        Assert.Empty(ScenarioA1StunCalculator.PlayerTurnEnd(false, true));
    }

    // The forwards: each old member equals a copy of its old expression over its inputs.

    [Fact]
    public void MachineGunKillResultIsUnchanged()
    {
        static string Old(int dr, int finalTk) =>
            dr == 12 ? OrdnanceKill.Dud : dr * 2 <= finalTk ? OrdnanceKill.Burn : dr < finalTk ? OrdnanceKill.Eliminated : dr == finalTk ? OrdnanceKill.Stun : OrdnanceKill.None;
        for (var dr = -2; dr <= 16; dr++)
        {
            for (var tk = -4; tk <= 20; tk++)
            {
                Assert.Equal(Old(dr, tk), ScenarioA1MachineGunKill.Result(dr, tk));
            }
        }

        // The Dud test comes first: an Original 12 against a Final TK# of 12 is a Dud, not a Stun.
        Assert.Equal(OrdnanceKill.Dud, ScenarioA1MachineGunKill.Result(12, 12));
    }

    [Fact]
    public void SniperCrewResultIsUnchanged()
    {
        static string Old(int dr, bool fullyArmored, bool stunRecovery, bool pinned) =>
            fullyArmored ? dr == 1 || stunRecovery ? ScenarioA1Sniper.Recalled : ScenarioA1Sniper.Stunned
            : dr == 1 ? ScenarioA1Sniper.CrewBroken
            : pinned ? ScenarioA1Sniper.NoEffect : ScenarioA1Sniper.CrewPinned;
        for (var dr = 0; dr <= 7; dr++)
        {
            foreach (var armored in Both)
            {
                foreach (var recovery in Both)
                {
                    foreach (var pinned in Both)
                    {
                        Assert.Equal(Old(dr, armored, recovery, pinned), ScenarioA1Sniper.CrewResult(dr, armored, recovery, pinned));
                    }
                }
            }
        }

        // The calculator's verdict agrees on dr 2, the Sniper's Stun.
        Assert.Equal(StunVerdict.Recalled, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, recovery: true)).Outcome);
    }

    [Fact]
    public void VehicleConditionsAreUnchanged()
    {
        static List<(VehicleCondition, bool)> Old(string result, string crew)
        {
            var changes = new List<(VehicleCondition, bool)>();
            if (result == FireVehicleEffect.Immobilized)
            {
                changes.Add((VehicleCondition.Immobilized, true));
                changes.Add((VehicleCondition.Motion, false));
            }

            switch (crew)
            {
                case FireVehicleEffect.Stunned:
                    changes.Add((VehicleCondition.Stunned, true));
                    changes.Add((VehicleCondition.ButtonedUp, true));
                    changes.Add((VehicleCondition.Motion, false));
                    break;
                case FireVehicleEffect.Recalled:
                    changes.Add((VehicleCondition.Recalled, true));
                    changes.Add((VehicleCondition.Stunned, false));
                    changes.Add((VehicleCondition.StunRecovery, false));
                    changes.Add((VehicleCondition.ButtonedUp, true));
                    changes.Add((VehicleCondition.Motion, false));
                    break;
                case FireVehicleEffect.Pinned:
                    changes.Add((VehicleCondition.Pinned, true));
                    break;
            }

            return changes;
        }

        foreach (var result in new[] { FireVehicleEffect.None, FireVehicleEffect.Immobilized })
        {
            foreach (var crew in CrewResults)
            {
                var effect = new FireVehicleEffect(string.Empty, string.Empty, result, null, [], 0, null, null, crew);
                Assert.Equal(Old(result, crew), ScenarioA1ResultTables.VehicleConditions(effect));
            }
        }
    }

    [Fact]
    public void StunEndsIsUnchanged()
    {
        static IReadOnlyList<(UnitCondition, bool)> Old(bool active, bool recalled, bool stunRecovery, bool stunned) =>
            !active ? []
            : recalled ? stunRecovery ? [] : [(UnitCondition.StunRecovery, true)]
            : stunned ? [(UnitCondition.Stunned, false), (UnitCondition.StunRecovery, true)] : [];
        foreach (var active in Both)
        {
            foreach (var recalled in Both)
            {
                foreach (var recovery in Both)
                {
                    foreach (var stunned in Both)
                    {
                        Assert.Equal(Old(active, recalled, recovery, stunned), ScenarioA1SequenceProjection.StunEnds(active, recalled, recovery, stunned).Conditions);
                    }
                }
            }
        }
    }

    [Fact]
    public void TaskCheckAndSurvivalDrmAreUnchanged()
    {
        foreach (var recovery in Both)
        {
            for (var a = 1; a <= 6; a++)
            {
                for (var b = 1; b <= 6; b++)
                {
                    for (var morale = 5; morale <= 10; morale++)
                    {
                        var plusOne = recovery ? 1 : 0;
                        var check = ScenarioA1CrewSurvivalRules.TaskCheck([a, b], morale, recovery, null);
                        Assert.Equal((a + b + plusOne, a + b + plusOne <= morale), (check.FinalDr, check.Passed));
                        Assert.Equal(recovery ? 1 : 0, check.Drm.Count);
                        Assert.All(check.Drm, item => Assert.Equal(("stun", 1m, "D5.34"), (item.Name, item.Value, item.Rule)));
                    }
                }
            }
        }

        foreach (var broken in Both)
        {
            foreach (var stunned in Both)
            {
                foreach (var shocked in Both)
                {
                    foreach (var recovery in Both)
                    {
                        foreach (var apart in Both)
                        {
                            var old = apart ? (broken || stunned || shocked ? 1 : 0) + (recovery ? 1 : 0) : broken || stunned || shocked || recovery ? 1 : 0;
                            Assert.Equal(old, ScenarioA1CrewSurvivalRules.SurvivalDrm(broken, stunned, shocked, recovery, apart));
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void TheStunPlusOneSitesTakeTheValueTheyHadBefore()
    {
        // The TH (armor), MG IFT, OVR, CC, TC and NTC sites read Drm where they read "StunRecovery ? 1 : 0" before.
        foreach (var dr in new[] { StunDr.ToHit, StunDr.MachineGunIft, StunDr.Overrun, StunDr.CloseCombat, StunDr.TaskCheck, StunDr.MoraleCheck, StunDr.CrewSurvival })
        {
            foreach (var recovery in Both)
            {
                Assert.Equal(recovery ? 1 : 0, ScenarioA1StunCalculator.Drm(dr, recovery));
            }
        }
    }

    // KillConditions: the old overload keeps its answer; the new one Recalls a second Stun.

    [Fact]
    public void TheOldKillConditionsKeepItsAnswer()
    {
        static List<(UnitCondition, bool)> Old(string result, bool shocked, bool concealedOrHidden)
        {
            var changed = new List<(UnitCondition, bool)>();
            if (concealedOrHidden)
            {
                changed.Add((UnitCondition.Concealed, false));
                changed.Add((UnitCondition.Hidden, false));
            }

            if (result == OrdnanceKill.Immobilized)
            {
                changed.Add((UnitCondition.Immobilized, true));
                changed.Add((UnitCondition.Motion, false));
            }

            if (result == OrdnanceKill.Stun)
            {
                changed.Add((UnitCondition.Stunned, true));
                changed.Add((UnitCondition.ButtonedUp, true));
                changed.Add((UnitCondition.Motion, false));
            }

            if (shocked)
            {
                changed.Add((UnitCondition.Shocked, true));
                changed.Add((UnitCondition.UnconfirmedKill, false));
                changed.Add((UnitCondition.ButtonedUp, true));
                changed.Add((UnitCondition.Motion, false));
            }

            return changed;
        }

        foreach (var result in KillResults)
        {
            foreach (var shocked in Both)
            {
                foreach (var concealed in Both)
                {
                    Assert.Equal(Old(result, shocked, concealed), ScenarioA1OrdnanceEventRules.KillConditions(result, shocked, concealed));
                    Assert.Equal(ScenarioA1OrdnanceEventRules.KillConditions(result, shocked, concealed),
                        ScenarioA1OrdnanceEventRules.KillConditions(result, shocked, concealed, false, false));
                }
            }
        }

        // An AFV under Stun +1 is still only Stunned by the old overload.
        Assert.Contains((UnitCondition.Stunned, true), ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Stun, false, false));
    }

    [Fact]
    public void AMgStunOfAnAfvAlreadyStunnedOrUnderPlusOneIsARecall()
    {
        (UnitCondition, bool)[] recall = [(UnitCondition.Recalled, true), (UnitCondition.Stunned, false), (UnitCondition.StunRecovery, false),
            (UnitCondition.ButtonedUp, true), (UnitCondition.Motion, false)];
        Assert.Equal(recall, ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Stun, false, false, true, false));
        Assert.Equal(recall, ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Stun, false, false, false, true));
        Assert.Equal([(UnitCondition.Stunned, true), (UnitCondition.ButtonedUp, true), (UnitCondition.Motion, false)],
            ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Stun, false, false, false, false));

        // Other results ignore the counter.
        Assert.Equal(ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Immobilized, true, true),
            ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Immobilized, true, true, true, true));
    }

    // The crew check of a FireAttack (ScenarioA1FireCalculator, VehicleEffects) under FireAttack.StunReading.

    private static FireAttack AtBren(bool stunned, bool recovering, int[] crewDice, bool? reading) =>
        new FireAttack("PFPh", "phasing", true, From, At, [new FireFirer("ru-1", "defender-squad", From, false, false, false, false, false),
                new FireFirer("ru-2", "defender-squad", From, false, false, false, false, false)],
            new FireDirector("ru-l", "defender-leader", From, false, false, false, false, false), 2, true, new FireLos(false, 0, true, false), null, "open-ground", [], 3,
            new FireRolls([2, 3], null, null, null) { CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["uk-bren"] = crewDice } })
        {
            Vehicles = [new FireVehicle("uk-bren", Bren, At, true, stunned, recovering, false) { Prc = [] }],
            StunReading = reading,
        };

    private static FireVehicleEffect Crew(FireAttack attack)
    {
        var result = ScenarioA1FireCalculator.Resolve(attack, Ordnance.Fire);
        Assert.True(result.VehicleEffects is { Count: 1 }, string.Join("; ", result.Reasons));
        return Assert.Single(result.VehicleEffects!);
    }

    [Fact]
    public void UnderStunReadingAMerelyStunnedCarrierCrewsMcTakesNoPlusOne()
    {
        // D6.84: a Carrier's crew stays CE while Stunned and takes the Collateral Attack; the 8 column's 7 (2 + 3, +2 CE) calls for a MC.
        var check = Assert.IsType<FireCheck>(Crew(AtBren(true, false, [1, 1], true)).CrewCheck);
        Assert.EndsWith("MC", check.Kind, StringComparison.Ordinal);
        var modifier = check.FinalDr - check.OriginalDr;
        var exact = check.MoraleLevel - modifier;
        Assert.InRange(exact, 3, 11);
        int[] dice = [exact / 2, exact - (exact / 2)];

        // Before the switch: a +1 "stun-recovery" and a failure by one, a Recall.
        var before = Crew(AtBren(true, false, dice, null));
        Assert.Contains(before.CrewCheck!.Drm, item => item.Name == "stun-recovery");
        Assert.Equal(FireVehicleEffect.Recalled, before.CrewResult);

        // Under it: no +1, so the check passes.
        var after = Crew(AtBren(true, false, dice, true));
        Assert.DoesNotContain(after.CrewCheck!.Drm, item => item.Name == "stun-recovery");
        Assert.Equal(FireVehicleEffect.None, after.CrewResult);

        // A failed MC of a Stunned crew is a second Stun, a Recall, under either reading; under Stun +1 the +1 stays.
        Assert.Equal(FireVehicleEffect.Recalled, Crew(AtBren(true, false, [6, 5], true)).CrewResult);
        var recovering = Crew(AtBren(false, true, [6, 5], true));
        Assert.Equal(FireVehicleEffect.Recalled, recovering.CrewResult);
        Assert.Contains(recovering.CrewCheck!.Drm, item => item.Name == "stun-recovery");
        Assert.Equal(FireVehicleEffect.Stunned, Crew(AtBren(false, false, [6, 5], true)).CrewResult);
    }

    // The ordnance paths under OrdnanceShot.StunEncircledReading.

    private static OrdnanceShot TankAtInfantry(bool recovering, bool? reading) =>
        new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("defender-tank-1", "defender-tank", false, 0, false, false),
            new OrdnanceCrew("defender-tank-1", "defender-tank", false, false, false, false, false), At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground",
                [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null),
            new OrdnanceRolls([6, 5], null, null, null, null))
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false) { StunRecovery = recovering ? true : null },
            Ammunition = "he",
            ScenarioYear = 1942,
            StunEncircledReading = reading,
        };

    [Fact]
    public void TheInfantryTargetTypeTakesTheStunPlusOneOnlyUnderTheSwitch()
    {
        var before = ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(true, null), Ordnance);
        Assert.True(before.ToHit is not null, string.Join("; ", before.Reasons));
        Assert.DoesNotContain(before.ToHit.Drm, item => item.Name == "stun");

        var after = ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(true, true), Ordnance);
        Assert.Contains(after.ToHit!.Drm, item => item is { Name: "stun", Value: 1m, Rule: "D5.34" });
        Assert.Equal(before.ToHit.FinalDr + 1, after.ToHit.FinalDr);

        // An AFV not under the counter adds nothing under the switch.
        Assert.DoesNotContain(ScenarioA1OrdnanceCalculator.Resolve(TankAtInfantry(false, true), Ordnance).ToHit!.Drm, item => item.Name == "stun");
    }

    private static OrdnanceShot MortarFromAfv(bool? reading) =>
        new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("de-mtr", "attacker-light-mortar", false, 0, false, false),
            new OrdnanceCrew("de-s", "attacker-squad", false, false, false, false, false), At, 5, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground",
                [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], 2, null),
            new OrdnanceRolls([6, 5], null, null, null, null))
        {
            TargetType = OrdnanceTargetTypes.Area,
            Vehicle = new OrdnanceVehicleFirer(false, false, false, false, false) { StunRecovery = true },
            StunEncircledReading = reading,
        };

    // No shot fired by a vehicle reaches the Area Target Type today (only a mortar fires it, and a vehicle fires only its own Gun), so the Area
    // calculator, internal to Rules, is asked directly.
    private static OrdnanceResolution AreaRun(OrdnanceShot shot) =>
        (OrdnanceResolution)typeof(ScenarioA1OrdnanceCalculator).Assembly.GetType("LimboDancer.Domains.Asl.Rules.ScenarioA1AreaCalculator")!
            .GetMethod("Run", [typeof(OrdnanceShot), typeof(GunDefinition), typeof(ScenarioA1OrdnanceReference)])!
            .Invoke(null, [shot, Ordnance.Guns["attacker-light-mortar"], Ordnance])!;

    [Fact]
    public void TheAreaTargetTypeTakesTheStunPlusOneOnlyUnderTheSwitch()
    {
        var before = AreaRun(MortarFromAfv(null));
        Assert.True(before.ToHit is not null, string.Join("; ", before.Reasons));
        Assert.DoesNotContain(before.ToHit.Drm, item => item.Name == "stun");
        var after = AreaRun(MortarFromAfv(true));
        Assert.Contains(after.ToHit!.Drm, item => item is { Name: "stun", Value: 1m, Rule: "D5.34" });
    }

    private static OrdnanceShot TankAtBren(int[] toKill, bool stunned, bool? reading) =>
        new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("firer", "attacker-tank", false, 0, false, false),
            new OrdnanceCrew("firer", "attacker-tank", false, false, false, false, false), At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 3, null),
            new OrdnanceRolls([2, 1], null, null, null, null) { ToKill = toKill, CollateralCheck = [6, 5], CrewSurvival = [6, 6] })
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false),
            VehicleTarget = new OrdnanceVehicleTarget("uk-bren", Bren, "front", null, false, false, false, false, false)
            {
                CrewExposed = true,
                Prc = [],
                Stunned = stunned ? true : null,
            },
            Ammunition = "he",
            ScenarioYear = 1942,
            SpecificCollateral = true,
            StunEncircledReading = reading,
        };

    [Fact]
    public void UnderTheSwitchACarrierStunnedBeforeTheHitIsRecalledByAFailedCollateralMc()
    {
        // D5.342, D6.84: a Carrier's crew, Stunned before this hit, fails its Collateral Attack's MC: a second Stun. The search finds a To Kill DR that
        // leaves the Carrier standing and calls for a MC.
        var found = 0;
        for (var total = 2; total <= 11; total++)
        {
            int[] toKill = [total / 2, total - (total / 2)];
            var effect = ScenarioA1OrdnanceCalculator.Resolve(TankAtBren(toKill, true, true), Ordnance).SpecificCollateral?.Effect;
            if (effect?.CrewCheck is not { Kind: var kind } || !Regex.IsMatch(kind, "^[1-4]MC$|^NMC$"))
            {
                continue;
            }

            found++;
            Assert.Equal(FireVehicleEffect.Recalled, effect.CrewResult);
            Assert.DoesNotContain(effect.CrewCheck.Drm, item => item.Name == "stun-recovery");

            // Before the switch the shot carried no Stunned fact: the same failure Stuns.
            var before = ScenarioA1OrdnanceCalculator.Resolve(TankAtBren(toKill, true, null), Ordnance).SpecificCollateral!.Effect;
            Assert.Equal(FireVehicleEffect.Stunned, before.CrewResult);
        }

        Assert.True(found > 0, "no To Kill DR called for a Collateral MC");
    }

    // A10.532: the Stun +1 bars Interdiction through RoutVehicleFacts.StunRecovery.

    [Fact]
    public void TheStunPlusOneBarsInterdictionWhenGiven()
    {
        var tank = new RoutVehicleFacts(false, false, false, false, false, false, true, 1, 1, true, true, false, false, true, 12, "t");
        var free = ScenarioA1RoutCalculator.VehicleInterdiction(tank);
        Assert.NotEmpty(free.ArcWeapons);
        Assert.Equal(free.ArcWeapons, ScenarioA1RoutCalculator.VehicleInterdiction(tank with
        {
            StunRecovery = false
        }).ArcWeapons);
        Assert.Equal(0, ScenarioA1RoutCalculator.VehicleInterdiction(tank with
        {
            StunRecovery = true
        }).Range);
        Assert.Empty(ScenarioA1RoutCalculator.VehicleInterdiction(tank with
        {
            StunRecovery = true
        }).ArcWeapons);
    }

    // The contracts.

    private static ContractVerdict Evaluate(string contract, string json) =>
        ScenarioA1RuleContracts.Evaluate(ScenarioA1RuleContracts.Find(contract)!, JsonNode.Parse(json)!.AsObject());

    private const string Owned = "\"stunned\": false, \"stunRecovery\": false, \"smcTemporaryCrew\": false";

    [Fact]
    public void TheStunContractAnswersEachOutcome()
    {
        const string Base = "\"vehicleId\": \"de-tank\", \"crewExposed\": true, \"qualifiesForCeDrm\": true, \"fullyArmored\": true, \"closedTopped\": true, \"oneManTurret\": false";
        Assert.Equal(StunVerdict.Stunned, Evaluate("d5.34-stun", "{" + Base + ", \"cause\": \"failed-mc\", \"mcFailed\": true, " + Owned + "}").Outcome);
        Assert.Equal(StunVerdict.None, Evaluate("d5.34-stun", "{" + Base + ", \"cause\": \"failed-mc\", \"mcFailed\": false, " + Owned + "}").Outcome);
        Assert.Equal(StunVerdict.Recalled, Evaluate("d5.34-stun",
            "{" + Base + ", \"cause\": \"sniper-dr\", \"sniperDr\": 2, \"stunned\": false, \"stunRecovery\": true, \"smcTemporaryCrew\": false}").Outcome);
        Assert.Equal(StunVerdict.Stunned, Evaluate("d5.34-stun", "{" + Base + ", \"cause\": \"mg-to-kill\", \"finalDr\": 8, \"finalTk\": 8, " + Owned + "}").Outcome);
        Assert.Equal(StunVerdict.None, Evaluate("d5.34-stun", "{" + Base + ", \"cause\": \"mg-to-kill\", \"finalDr\": 9, \"finalTk\": 8, " + Owned + "}").Outcome);

        // A 1MT AFV and a SMC Temporary crew: their rules decide.
        var oneManTurret = Evaluate("d5.34-stun", "{" + Base.Replace("\"oneManTurret\": false", "\"oneManTurret\": true", StringComparison.Ordinal)
            + ", \"cause\": \"mg-to-kill\", \"finalDr\": 8, \"finalTk\": 8, " + Owned + "}");
        Assert.Equal((ContractVerdict.NotEvaluated, "D5.341"), (oneManTurret.Outcome, oneManTurret.Reasons[0].Rule));

        // A fact the cause reads, left out, and a number that is not whole.
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("d5.34-stun", "{" + Base + ", \"cause\": \"mg-to-kill\", \"finalDr\": 8, " + Owned + "}").Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("d5.34-stun", "{" + Base + ", \"cause\": \"sniper-dr\", \"sniperDr\": 2.5, " + Owned + "}").Outcome);
    }

    [Fact]
    public void TheOpponentOfAConcealedAfvCannotEvaluateItsStun()
    {
        // Under the "?" the Stun counters are the owner's: the other side's viewer gives them as unknown.
        var verdict = Evaluate("d5.34-stun", "{\"vehicleId\": \"de-tank\", \"cause\": \"sniper-dr\", \"crewExposed\": true, \"fullyArmored\": true, \"sniperDr\": 2, "
            + "\"oneManTurret\": false, \"stunned\": null, \"stunRecovery\": null, \"smcTemporaryCrew\": null}");
        Assert.Equal(ContractVerdict.NotEvaluated, verdict.Outcome);
        Assert.All(ScenarioA1RuleContracts.Find("d5.34-stun")!.Facts.Where(fact => fact.Name is "stunned" or "stunRecovery" or "smcTemporaryCrew"),
            fact => Assert.Equal((ContractDisclosure.Owner, "vehicleId"), (fact.Disclosure, fact.Concerns)));
    }

    [Fact]
    public void ThePlusOneContractAnswersEachDr()
    {
        var toHit = Evaluate("d5.34-stun-plus-one", "{\"vehicleId\": \"de-tank\", \"dr\": \"to-hit\", \"stunRecovery\": true}");
        Assert.Equal("plus-one", toHit.Outcome);
        Assert.Contains("its TH DR", toHit.Reasons[0].Text, StringComparison.Ordinal);
        Assert.Equal("none", Evaluate("d5.34-stun-plus-one", "{\"vehicleId\": \"de-tank\", \"dr\": \"excessive-speed-breakdown\", \"stunRecovery\": true}").Outcome);
        Assert.Equal("none", Evaluate("d5.34-stun-plus-one", "{\"vehicleId\": \"de-tank\", \"dr\": \"to-hit\", \"stunRecovery\": false}").Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("d5.34-stun-plus-one", "{\"vehicleId\": \"de-tank\", \"dr\": \"to-hit\"}").Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("d5.34-stun-plus-one", "{\"vehicleId\": \"de-tank\", \"dr\": \"Motion\", \"stunRecovery\": true}").Outcome);

        // Every choice names the StunDr whose Drm it answers.
        var choices = ScenarioA1RuleContracts.Find("d5.34-stun-plus-one")!.Facts.Single(fact => fact.Name == "dr").Choices!;
        Assert.Equal(Enum.GetValues<StunDr>().Length, choices.Count);
        Assert.Equal(Listed.Length, choices.Count(choice =>
            Evaluate("d5.34-stun-plus-one", "{\"vehicleId\": \"de-tank\", \"dr\": \"" + choice + "\", \"stunRecovery\": true}").Outcome == "plus-one"));
    }

    // The review's fixes (findings 2, 3, 5, 6, 7, 12).

    [Fact]
    public void AnOriginal12IsADudAndNeverStuns()
    {
        // C7.35 (p. 177): "Any Original TK DR of 12 (regardless of ammunition or Target Type) has no effect".
        var dud = ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, dr: 12, tk: 12) with
        {
            OriginalDr = 12
        });
        Assert.Equal((StunVerdict.None, "C7.35"), (dud.Outcome, dud.Rule));
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, dr: null, tk: null) with
        {
            OriginalDr = 12
        }).Outcome);

        // Null reads as no Original 12, and any other Original DR leaves the Final DR to decide.
        Assert.Equal(StunVerdict.Stunned, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, dr: 12, tk: 12)).Outcome);
        Assert.Equal(StunVerdict.Stunned, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, dr: 12, tk: 12) with
        {
            OriginalDr = 11
        }).Outcome);

        // The contract agrees with MachineGunKill.Result on an Original 12.
        Assert.Equal(OrdnanceKill.Dud, ScenarioA1MachineGunKill.Result(12, 12));
        const string Base = "\"vehicleId\": \"de-tank\", \"cause\": \"mg-to-kill\", \"oneManTurret\": false, \"finalDr\": 12, \"finalTk\": 12, ";
        var verdict = Evaluate("d5.34-stun", "{" + Base + "\"originalDr\": 12, " + Owned + "}");
        Assert.Equal((StunVerdict.None, "C7.35"), (verdict.Outcome, verdict.Reasons[0].Rule));
        Assert.Equal(StunVerdict.Stunned, Evaluate("d5.34-stun", "{" + Base + "\"originalDr\": 10, " + Owned + "}").Outcome);
        Assert.Equal(ContractVerdict.NotEvaluated, Evaluate("d5.34-stun", "{" + Base + "\"originalDr\": 11.5, " + Owned + "}").Outcome);
    }

    [Fact]
    public void AKnownFactThatRulesTheStunOutDecidesNoneThoughAnotherIsUnknown()
    {
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, exposed: false, qualifies: null, failed: null)).Outcome);
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, exposed: null, qualifies: false)).Outcome);
        Assert.Contains("Unprotected", ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, exposed: null, qualifies: false)).Reason, StringComparison.Ordinal);
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, exposed: null, failed: false)).Outcome);
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, exposed: false, sniper: null)).Outcome);
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, armored: false, exposed: null)).Outcome);
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, exposed: null, sniper: 5)).Outcome);
        Assert.Equal(StunVerdict.None, ScenarioA1StunCalculator.Result(Facts(StunCause.FallingRubble, exposed: null, closed: false)).Outcome);

        // A fact the answer still needs leaves it not evaluated.
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.FailedMc, exposed: null)).Outcome);
        Assert.Equal(StunVerdict.NotEvaluated, ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr, exposed: null)).Outcome);
    }

    [Fact]
    public void ASmcTemporaryCrewInA1mtAfvIsD5343s()
    {
        // D5.343 (p. 204): Wounded "even while in a 1MT AFV".
        var smc = ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr) with
        {
            OneManTurret = true,
            SmcTemporaryCrew = true
        });
        Assert.Equal((StunVerdict.NotEvaluated, "D5.343"), (smc.Outcome, smc.Rule));
        Assert.Equal("D5.343", ScenarioA1StunCalculator.Result(Facts(StunCause.SniperDr) with
        {
            OneManTurret = null,
            SmcTemporaryCrew = true
        }).Rule);
    }

    [Fact]
    public void TheFrontOfARecallCountsAsStunnedForASecondStun()
    {
        // D5.341: "Recall is treated the same as a Stun". The caller gives Stunned or Recalled as stunned; the MG's Stun then gives the Recall list,
        // which leaves Recalled set and Stunned false (no mixed state).
        var conditions = ScenarioA1OrdnanceEventRules.KillConditions(OrdnanceKill.Stun, false, false, true, false);
        Assert.Contains((UnitCondition.Recalled, true), conditions);
        Assert.Contains((UnitCondition.Stunned, false), conditions);
        Assert.DoesNotContain((UnitCondition.Stunned, true), conditions);
        Assert.Equal(StunVerdict.Recalled, ScenarioA1StunCalculator.Result(Facts(StunCause.MachineGunToKill, stunned: true)).Outcome);
    }

    [Fact]
    public void TheOrdnanceKillOverloadWithTheStunCounterReadsTheResultAsTheStringOverload()
    {
        foreach (var result in KillResults)
        {
            foreach (var shocked in new bool?[] { null, false, true })
            {
                var kill = new OrdnanceKill("mg", "hull", "front", 8, [], 8, null, 8, [4, 4], 8, result) { Shocked = shocked };
                foreach (var concealed in Both)
                {
                    Assert.Equal(ScenarioA1OrdnanceEventRules.KillConditions(kill, concealed),
                        ScenarioA1OrdnanceEventRules.KillConditions(kill, concealed, false, false));
                    foreach (var stunned in Both)
                    {
                        foreach (var recovery in Both)
                        {
                            Assert.Equal(ScenarioA1OrdnanceEventRules.KillConditions(result, shocked == true, concealed, stunned, recovery),
                                ScenarioA1OrdnanceEventRules.KillConditions(kill, concealed, stunned, recovery));
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void TheOldCrewOutcomeForwardsWithItsAnswerOverEveryIftResultAndDr()
    {
        // CrewOutcome is internal: both overloads are reached by reflection.
        var methods = typeof(ScenarioA1FireCalculator).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            .Where(method => method.Name == "CrewOutcome").ToArray();
        var old = methods.Single(method => method.GetParameters()[2].ParameterType == typeof(bool));
        var apart = methods.Single(method => method.GetParameters()[2].ParameterType == typeof(int));
        string[] outcomes = ["1MC", "2MC", "3MC", "4MC", "NMC", "PTC", "1KIA", "K/2", "-"];
        foreach (var outcome in outcomes)
        {
            foreach (var recovery in Both)
            {
                for (var a = 1; a <= 6; a++)
                {
                    for (var b = 1; b <= 6; b++)
                    {
                        int[] dice = [a, b];
                        var before = ((FireCheck? Check, string Result))old.Invoke(null, [outcome, dice, recovery, 8, null])!;
                        var after = ((FireCheck? Check, string Result))apart.Invoke(null, [outcome, dice, recovery ? 1 : 0, recovery, 8, null])!;
                        Assert.Equal(before.Result, after.Result);
                        Assert.Equal(before.Check is null, after.Check is null);
                        if (before.Check is { } check)
                        {
                            Assert.Equal((check.Kind, check.OriginalDr, check.FinalDr, check.Passed, check.Consequence),
                                (after.Check!.Kind, after.Check.OriginalDr, after.Check.FinalDr, after.Check.Passed, after.Check.Consequence));
                            Assert.Equal(check.Drm, after.Check.Drm);
                        }
                    }
                }
            }
        }
    }
}
