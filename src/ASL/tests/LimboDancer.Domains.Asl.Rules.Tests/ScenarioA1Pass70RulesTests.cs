using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 70, Infantry III. Increment 3a: who sees (A12.1, A12.14, A12.122, A12.16; ruling R70.16), the actions that cost a "?" (A12.141; ruling R70.17),
/// and the Right of Inspection (A12.16; ruling R70.18), read in Rules through its public members.
/// </summary>
public sealed class ScenarioA1Pass70RulesTests
{
    private static SightSeerFacts Seer(int at = 1, bool goodOrder = true, bool unbroken = true, bool dummy = false, bool hidden = false, bool aboard = false,
        bool captured = false, bool emptyVehicle = false, string side = "german") =>
        new(true, side, dummy, hidden, aboard, goodOrder, unbroken, captured, emptyVehicle, at);

    // Each seer at its index; the range is the index itself, the Location asked about being at 0. Reads at the blocked indexes are blocked, at the
    // unclear ones not definitive (clear but not definitively so), and at the missing ones not given.
    private sealed class RangeLos(int[]? blocked = null, int[]? unclear = null, int[]? missing = null) : ISightLosReader
    {
        public int Reads
        {
            get; private set;
        }

        public SightLosFacts? Los(int fromLocation, int toLocation)
        {
            Reads++;
            return missing?.Contains(fromLocation) == true ? null
                : blocked?.Contains(fromLocation) == true ? new SightLosFacts(false, true, fromLocation)
                : new SightLosFacts(unclear?.Contains(fromLocation) != true, false, fromLocation);
        }
    }

    private static readonly int[] Near = [20, 17, 5];
    private static readonly int[] Far = [20, 17];
    private static readonly int[] Five = [5];
    private static readonly int[] Seventeen = [17];
    private static readonly int[] Here = [0, 9];
    private static readonly int[] TwoSeers = [3, 4];
    private static readonly int[] Three = [3];

    [Fact]
    public void EachPurposeCountsItsOwnSeers()
    {
        // A12.1 (p. 76): "Good Order for '?' loss; unbroken (and Dummies) for denial of '?' gain"; a vehicle with no crew, Passengers, or Riders does
        // neither. A12.16 (p. 79): the Good Order ground units of the inspecting side, so Dummies are not ("not being Good order").
        (SightSeerFacts Seer, bool Loss, bool Gain, bool Inspection)[] cases =
        [
            (Seer(), true, true, true),
            (Seer(dummy: true, goodOrder: false), false, true, false),
            (Seer(hidden: true), false, false, true),
            (Seer(goodOrder: false, unbroken: false), false, false, false),
            (Seer(goodOrder: false), false, true, false),
            (Seer(aboard: true), false, true, true),
            (Seer(emptyVehicle: true), false, false, false),
            (Seer(goodOrder: false, captured: true), false, false, false),
            (Seer(at: 0) with { Location = null }, false, false, false),
            (Seer() with { Active = false }, false, false, false),
        ];
        foreach (var (seer, loss, gain, inspection) in cases)
        {
            Assert.Equal(loss, ScenarioA1Sight.Sees(seer, SightPurpose.Loss));
            Assert.Equal(gain, ScenarioA1Sight.Sees(seer, SightPurpose.GainDenial));
            Assert.Equal(inspection, ScenarioA1Sight.Sees(seer, SightPurpose.Inspection));
        }

        // The seers of a watched side are the other side's; the Right of Inspection's are the viewer's own, each Location once.
        SightSeerFacts[] units = [Seer(at: 3), Seer(at: 3), Seer(at: 4, hidden: true), Seer(at: 5, side: "russian")];
        Assert.Equal(TwoSeers, ScenarioA1Sight.OwnSeerLocations(units, "german", SightPurpose.Inspection));
        Assert.Equal(Three, ScenarioA1Sight.SeerLocations(units, "russian", SightPurpose.Loss));
        Assert.Equal(Five, ScenarioA1Sight.SeerLocations(units, "german", SightPurpose.Loss));
    }

    [Fact]
    public void TheNearestSeerIsReadWithinAndBeyondSixteen()
    {
        // A12.121: "the range from the nearest such enemy having a LOS"; A12.14 and A12.141: within 16 hexes and in LOS is the nearest at 16 or less.
        Assert.Equal(5, ScenarioA1Sight.NearestSeer(Near, 0, new RangeLos(), SightLosReading.Clear));
        Assert.True(ScenarioA1Sight.SeenWithin(5, 16));
        Assert.Equal(17, ScenarioA1Sight.NearestSeer(Far, 0, new RangeLos(), SightLosReading.Clear));
        Assert.False(ScenarioA1Sight.SeenWithin(17, 16));
        Assert.True(ScenarioA1Sight.SeenWithin(17, null));
        Assert.False(ScenarioA1Sight.SeenWithin(null, null));

        // A blocked LOS is none; a seer in the Location itself is at 0, with no read.
        Assert.Equal(17, ScenarioA1Sight.NearestSeer(Near, 0, new RangeLos(blocked: Five), SightLosReading.Clear));
        var reader = new RangeLos();
        Assert.Equal(0, ScenarioA1Sight.NearestSeer(Here, 0, reader, SightLosReading.Clear));
        Assert.Equal(0, reader.Reads);

        // Each caller's reading as it was (ruling R70.16): a read not definitive counts for the vehicle's and the gain's reading, not for the move's; a
        // missing read counts only for the Right of Inspection, and gives no range.
        Assert.Null(ScenarioA1Sight.NearestSeer(Five, 0, new RangeLos(unclear: Five), SightLosReading.Clear));
        Assert.Equal(5, ScenarioA1Sight.NearestSeer(Five, 0, new RangeLos(unclear: Five), SightLosReading.NotBlocked));
        Assert.Null(ScenarioA1Sight.NearestSeer(Five, 0, new RangeLos(missing: Five), SightLosReading.NotShownBlocked));
        Assert.True(ScenarioA1Sight.InLos(null, SightLosReading.NotShownBlocked));
        Assert.False(ScenarioA1Sight.InLos(null, SightLosReading.NotBlocked));
        Assert.False(ScenarioA1Sight.InLos(new SightLosFacts(false, true, 3), SightLosReading.NotShownBlocked));
    }

    [Fact]
    public void AStackIsInspectedOnlyAfterSetupAndInTheLosOfASeer()
    {
        // A12.16 (p. 79): after play begins an unconcealed stack is inspected unless out of the LOS of all the viewer's Good Order ground units; before
        // play none is (A2.9), and its seers are not read.
        Assert.False(ScenarioA1Sight.Inspectable(false, () => throw new InvalidOperationException("the seers were read.")));
        Assert.True(ScenarioA1Sight.Inspectable(true, () => true));
        Assert.False(ScenarioA1Sight.Inspectable(true, () => false));

        // In LOS of one seer of two, read up to the first that sees; out of the LOS of both; a missing read counts as in LOS (ruling R70.18).
        var reader = new RangeLos(blocked: Seventeen);
        Assert.True(ScenarioA1Sight.AnySeerInLos(Near, 0, reader, SightLosReading.NotShownBlocked));
        Assert.Equal(1, reader.Reads);
        Assert.False(ScenarioA1Sight.AnySeerInLos(Far, 0, new RangeLos(blocked: Near), SightLosReading.NotShownBlocked));
        Assert.True(ScenarioA1Sight.AnySeerInLos(Far, 0, new RangeLos(missing: Far), SightLosReading.NotShownBlocked));
    }

    [Fact]
    public void AnActionCostsAConcealedUnitsQuestionMarkInTheLosOfASeerWithinSixteen()
    {
        // A12.141 (p. 78; ruling R70.17): the nearest Loss seer is read only for a concealed unit.
        Assert.True(ScenarioA1Sight.ActionLosesConcealment(true, () => 16));
        Assert.False(ScenarioA1Sight.ActionLosesConcealment(true, () => 17));
        Assert.False(ScenarioA1Sight.ActionLosesConcealment(true, () => null));
        Assert.False(ScenarioA1Sight.ActionLosesConcealment(false, () => throw new InvalidOperationException("the seers were read.")));
    }

    [Fact]
    public void AStackOutOfInspectionShowsItsTopAndCountsTheRest()
    {
        // A12.16 (ruling R70.18): after play, a counter in a stack out of the viewer's inspection is shown as before play: the top itself, the rest
        // counted, and the equipment beneath withheld; a stack in LOS is shown in full.
        var beneath = new CounterFacts(true, false, false, false, false, true, false, false) { OutOfInspection = true };
        var top = beneath with
        {
            IsTop = true
        };
        Assert.Equal(CounterPresence.SealedUninspected, ScenarioA1Visibility.UnitPresence(beneath, beforePlay: false));
        Assert.Equal(CounterPresence.Shown, ScenarioA1Visibility.UnitPresence(top, beforePlay: false));
        Assert.Equal(CounterPresence.Shown, ScenarioA1Visibility.UnitPresence(beneath with
        {
            OutOfInspection = false
        }, beforePlay: false));
        Assert.True(ScenarioA1Visibility.Withheld([beneath], beforePlay: false, sealAllowed: false));
        Assert.False(ScenarioA1Visibility.Withheld([beneath], beforePlay: false, sealAllowed: true));
        var weapon = new CounterFacts(true, false, false, false, false, false, false, true) { OutOfInspection = true };
        Assert.True(ScenarioA1Visibility.Withheld([weapon], beforePlay: false, sealAllowed: false));
        Assert.False(ScenarioA1Visibility.Withheld([weapon with { OutOfInspection = false }], beforePlay: false, sealAllowed: false));
    }

    // Increment 3b: the Morale Check member (task 70.1, part I; rulings R70.3 to R70.10).
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string At = "bd03:P10:0";
    private static readonly int[] SnakeEyes = [1, 1];
    private static readonly int[] Threes = [3, 3];
    private static readonly int[] TwoThree = [2, 3];
    private static readonly int[] TwoTwo = [2, 2];
    private static readonly int[] Fives = [5, 5];
    private static readonly int[] FiveSix = [5, 6];
    private static readonly int[] Boxcars = [6, 6];
    private static readonly string[] LeaderLossOfSquad = ["asl.a1.fire.roll-missing:leaderLoss:ru-s"];
    private static readonly FireAboard InHalftrack = new("ru-ht", FirePrc.Passenger, false, null);

    private static FireTarget Unit(string id, string definition, bool broken = false, FireAboard? aboard = null) =>
        new(id, definition, At, broken, false, false, false, false, false, false)
        {
            KnownEnemyInLos = true,
            Captors = [],
            Aboard = aboard,
        };

    private static FireRolls Rolls(IReadOnlyDictionary<string, IReadOnlyList<int>>? checks = null, IReadOnlyDictionary<string, IReadOnlyList<int>>? leaderLoss = null,
        IReadOnlyDictionary<string, int>? wounds = null, IReadOnlyDictionary<string, IReadOnlyList<int>>? heat = null) =>
        new(null, null, checks, leaderLoss, wounds)
        {
            HeatOfBattle = heat,
        };

    private static Dictionary<string, IReadOnlyList<int>> Dice(string id, int[] dice) => new(StringComparer.Ordinal) { [id] = dice };

    private static FireResolution Resolved(MoraleCheckFacts facts)
    {
        var result = ScenarioA1MoraleCheck.Resolve(facts, Reference);
        Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
        return result;
    }

    [Fact]
    public void AnInterdictionNmcsOriginalTwoCallsForHeatOfBattle()
    {
        // A10.53 (p. 67): the Interdiction NMC is "subject to ... everything that normally entails"; A15.1 (p. 83): an Original 2 calls for the Heat of
        // Battle DR, +1 for a broken unit even if the 2 rallied it, +2 for the Russians. The member asks for it once the NMC is passed.
        Assert.True(ScenarioA1MoraleCheck.InterdictionHeatOfBattleDue(2));
        Assert.False(ScenarioA1MoraleCheck.InterdictionHeatOfBattleDue(3));
        var facts = new MoraleCheckFacts(MoraleCheckFacts.Nmc, [Unit("ru-s", "defender-squad", broken: true)], 3, Rolls(Dice("ru-s", SnakeEyes)));
        Assert.Equal(["asl.a1.fire.roll-missing:heatOfBattle:ru-s"], ScenarioA1MoraleCheck.Resolve(facts, Reference).Reasons);

        // A Final DR of 3 + 3 + 3 = 9 is Berserk (A15.4): the unit rallies, so its rout ends where it was interdicted (ruling R70.5).
        var effect = Assert.Single(Resolved(facts with
        {
            Rolls = Rolls(Dice("ru-s", SnakeEyes), heat: Dice("ru-s", Threes))
        }).Effects);
        Assert.Equal((9, HeatOfBattleOutcome.Berserk, true, false), (effect.HeatOfBattle!.FinalDr, effect.HeatOfBattle.Result, effect.Berserk == true, effect.Broken));
        Assert.True(Assert.Single(effect.Checks).Passed);
        Assert.True(ScenarioA1MoraleCheck.EndsRout(effect));
        Assert.True(ScenarioA1MoraleCheck.AnyCheck(Resolved(facts with
        {
            Rolls = Rolls(Dice("ru-s", SnakeEyes), heat: Dice("ru-s", Threes))
        })));

        // A unit still broken (a hero created beside it, say) routs on; an eliminated one ends with no rout to end.
        Assert.False(ScenarioA1MoraleCheck.EndsRout(effect with
        {
            Berserk = null,
            Broken = true
        }));
        Assert.False(ScenarioA1MoraleCheck.EndsRout(effect with
        {
            Eliminated = true
        }));
    }

    [Fact]
    public void AnUnarmedUnitTakesNoHeatOfBattleAndNoReplacement()
    {
        // A15.1 (p. 83): "Unarmed units ... are not subject to Heat of Battle"; A19.11 (p. 86): "unarmed ... units are never subject to Replacement".
        var unarmed = Unit("ru-s", "defender-squad") with
        {
            Unarmed = true
        };
        Assert.False(ScenarioA1HeatOfBattle.Subject(Reference.Definitions["defender-squad"], false, false, unarmed: true));
        Assert.Null(Assert.Single(Resolved(new MoraleCheckFacts(MoraleCheckFacts.Nmc, [unarmed], 3, Rolls(Dice("ru-s", SnakeEyes)))).Effects).HeatOfBattle);

        // An 11 fails a 7 by 4: an Unarmed squad is only broken, with no ELR asked for; the armed squad of ELR 2 is Replaced (A19.13).
        var broken = Assert.Single(Resolved(new MoraleCheckFacts(MoraleCheckFacts.Nmc, [unarmed], null, Rolls(Dice("ru-s", FiveSix)))).Effects);
        Assert.Equal((true, "defender-squad", false), (broken.Broken, broken.FinalDefinitionId, broken.Disrupted));
        var armed = Assert.Single(Resolved(new MoraleCheckFacts(MoraleCheckFacts.Nmc, [unarmed with { Unarmed = null }], 2, Rolls(Dice("ru-s", FiveSix)))).Effects);
        Assert.True(armed.Broken);
        Assert.Equal(ScenarioA1FireReference.ReplacementOf("defender-squad") ?? "defender-squad", armed.FinalDefinitionId);
    }

    [Fact]
    public void ALlmcTakesTheReversedDrmAndRidersAndOtherVehiclesPassengersTakeNone()
    {
        // A10.2 (p. 65): the units of the Location with a lower Morale Level take a LLMC, the eliminated leader's negative modifier reversed; "Riders ...
        // [and] Passengers ... neither take or cause LLMC/LLTC", but to fellow Passengers of the same vehicle.
        var facts = new MoraleCheckFacts(MoraleCheckFacts.None, [], 3, Rolls())
        {
            Others = [Unit("ru-s", "defender-squad"), Unit("ru-r", "defender-squad", aboard: new FireAboard("ru-t", FirePrc.Rider, false, null)),
                Unit("ru-p", "defender-squad", aboard: InHalftrack)],
            LostLeaders = [Unit("ru-l", "defender-leader-8-1")],
        };
        Assert.Equal(LeaderLossOfSquad, ScenarioA1MoraleCheck.Resolve(facts, Reference).Reasons);
        var resolved = Resolved(facts with
        {
            Rolls = Rolls(leaderLoss: Dice("ru-s", TwoThree))
        });
        var check = Assert.Single(resolved.Effects.Single(item => item.UnitId == "ru-s").Checks);
        Assert.Equal(("LLMC", 6, true), (check.Kind, check.FinalDr, check.Passed));
        Assert.Equal(1m, check.Drm.Single(item => item.Name == "reversed-leadership:ru-l").Value);
        Assert.All(resolved.Effects.Where(item => item.UnitId != "ru-s"), item => Assert.Empty(item.Checks));

        // A11.141: none in Melee; with no check at all the member says so.
        var melee = Resolved(facts with
        {
            InMelee = true
        });
        Assert.False(ScenarioA1MoraleCheck.AnyCheck(melee));
    }

    [Fact]
    public void AnUnbrokenLeaderWhoBreaksCausesALltc()
    {
        // A10.2 (p. 65): "Should an unbroken leader break ... must take a LLTC", pinned rather than broken if failed.
        var facts = new MoraleCheckFacts(MoraleCheckFacts.None, [], 3, Rolls(leaderLoss: Dice("ru-s", Fives)))
        {
            Others = [Unit("ru-s", "defender-squad")],
            BrokenLeaders = [Unit("ru-l", "defender-leader-8-1")],
        };
        var effect = Resolved(facts).Effects.Single(item => item.UnitId == "ru-s");
        Assert.Equal(("LLTC", false), (effect.Checks[0].Kind, effect.Checks[0].Passed));
        Assert.Equal((true, false), (effect.Pinned, effect.Broken));
    }

    [Fact]
    public void APassengerLeaderAidsAndChecksOnlyHisVehiclesPassengers()
    {
        // D6.651 (p. 206): a Passenger leader affects only the MC of the Passengers of his vehicle; a leader on foot only the Passengers of a vehicle that
        // has not moved this phase; his loss checks only his fellow Passengers (A10.2).
        var aided = Resolved(new MoraleCheckFacts(MoraleCheckFacts.Nmc, [Unit("ru-p", "defender-squad", aboard: InHalftrack), Unit("ru-g", "defender-squad")], 3,
            Rolls(new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["ru-p"] = Threes, ["ru-g"] = Threes }))
        {
            Others = [Unit("ru-l", "defender-leader-8-1", aboard: InHalftrack)],
        });
        Assert.Contains(aided.Effects.Single(item => item.UnitId == "ru-p").Checks[0].Drm, item => item.Name == "leadership:ru-l");
        Assert.DoesNotContain(aided.Effects.Single(item => item.UnitId == "ru-g").Checks[0].Drm, item => item.Name.StartsWith("leadership:", StringComparison.Ordinal));

        MoraleCheckFacts OnFoot(bool moving) => new(MoraleCheckFacts.Nmc, [Unit("ru-p", "defender-squad", aboard: InHalftrack with { VehicleMoving = moving })], 3,
            Rolls(Dice("ru-p", Threes)))
        {
            Others = [Unit("ru-l", "defender-leader-8-1")],
        };
        Assert.Contains(Resolved(OnFoot(false)).Effects[0].Checks[0].Drm, item => item.Name == "leadership:ru-l");
        Assert.DoesNotContain(Resolved(OnFoot(true)).Effects[0].Checks[0].Drm, item => item.Name == "leadership:ru-l");

        // A CE Passenger leader in a halftrack that has not moved reaches the units on foot (D6.651).
        var exposed = Resolved(new MoraleCheckFacts(MoraleCheckFacts.Nmc, [Unit("ru-g", "defender-squad")], 3, Rolls(Dice("ru-g", Threes)))
        {
            Others = [Unit("ru-l", "defender-leader-8-1", aboard: InHalftrack with { CeInUnmovedHalftrack = true })],
        });
        Assert.Contains(exposed.Effects[0].Checks[0].Drm, item => item.Name == "leadership:ru-l");

        // His loss checks his fellow Passengers alone.
        var lost = new MoraleCheckFacts(MoraleCheckFacts.None, [], 3, Rolls())
        {
            Others = [Unit("ru-p", "defender-squad", aboard: InHalftrack), Unit("ru-q", "defender-squad", aboard: InHalftrack with { VehicleId = "ru-truck" }),
                Unit("ru-s", "defender-squad")],
            LostLeaders = [Unit("ru-l", "defender-leader-8-1", aboard: InHalftrack)],
        };
        Assert.Equal(["asl.a1.fire.roll-missing:leaderLoss:ru-p"], ScenarioA1MoraleCheck.Resolve(lost, Reference).Reasons);
    }

    [Fact]
    public void AFirePassengerLeaderAidsHisFellowPassengersWhenTheRecordCarriesThePassFacts()
    {
        // Ruling R70.8: in a Collateral Attack at a CE SPW 251/1, its Passenger 8-1 leads the MC of the 4-6-7 aboard; an attack recorded before the pass
        // carries no such fact, and its PRC take no leadership, as before.
        static FirePrc Prc(string id, string definition, bool? moving) => new(id, definition, FirePrc.Passenger, null, null)
        {
            Elr = 3,
            VehicleMoving = moving,
        };

        FireResolution Fired(bool? moving)
        {
            var attack = new FireAttack("PFPh", "phasing", true, "bd01:G7:0", "bd01:G5:0",
                [new FireFirer("ru-1", "defender-squad", "bd01:G7:0", false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] },
                 new FireFirer("ru-2", "defender-squad", "bd01:G7:0", false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }], null,
                2, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 3, null)
            {
                Vehicles = [new FireVehicle("de-ht", "attacker-halftrack", "bd01:G5:0", true, false, false, false)
                {
                    Prc = [Prc("de-s", "attacker-squad", moving), Prc("de-l", "attacker-leader-8-1", moving)],
                }],
            };
            var rolls = new FireRolls(TwoTwo, null, null, null);
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

                var key = reason["asl.a1.fire.roll-missing:".Length..].Split(':');
                rolls = key[0] switch
                {
                    "checks" => rolls with { Checks = new Dictionary<string, IReadOnlyList<int>>(rolls.Checks ?? new Dictionary<string, IReadOnlyList<int>>()) { [key[1]] = TwoThree } },
                    "crewCheck" => rolls with { CrewChecks = Dice(key[1], TwoThree) },
                    "randomSelection" => rolls with { RandomSelection = key[1].Split(',').ToDictionary(id => id, _ => 3) },
                    _ => throw new InvalidOperationException("An unexpected roll: " + reason),
                };
            }

            throw new InvalidOperationException("The attack asked for too many rolls.");
        }

        Assert.Contains(PrcCheck(Fired(false)).Drm, item => item.Name == "leadership:de-l");
        Assert.DoesNotContain(PrcCheck(Fired(null)).Drm, item => item.Name == "leadership:de-l");

        static FireCheck PrcCheck(FireResolution result)
        {
            Assert.Equal(FireResolution.Resolved, result.Disposition);
            return Assert.Single(result.VehicleEffects!).PrcAttacks!.SelectMany(item => item.Effects).Single(item => item.UnitId == "de-s").Checks[0];
        }
    }

    [Fact]
    public void ASmcsCasualtyReductionTakesItsWoundSeverityDr()
    {
        // A10.31, A17.11 (pp. 63, 85): an Original 12 on the 8-1's NMC Casualty Reduces him, a wound and its Wound Severity dr; a 5 is mortal, and his loss
        // calls for the squad's LLMC (A10.2); a 1 wounds and breaks him, an LLTC for the unbroken squad.
        var facts = new MoraleCheckFacts(MoraleCheckFacts.Nmc, [Unit("ru-l", "defender-leader-8-1")], 5, Rolls(Dice("ru-l", Boxcars)))
        {
            Others = [Unit("ru-s", "defender-squad")],
        };
        Assert.Equal(["asl.a1.fire.roll-missing:woundSeverity:ru-l"], ScenarioA1MoraleCheck.Resolve(facts, Reference).Reasons);
        var mortal = ScenarioA1MoraleCheck.Resolve(facts with
        {
            Rolls = Rolls(Dice("ru-l", Boxcars), wounds: new Dictionary<string, int> { ["ru-l"] = 5 })
        }, Reference);
        Assert.Equal(LeaderLossOfSquad, mortal.Reasons);
        var resolved = Resolved(facts with
        {
            Rolls = Rolls(Dice("ru-l", Boxcars), Dice("ru-s", TwoThree), new Dictionary<string, int> { ["ru-l"] = 5 })
        });
        Assert.True(resolved.Effects.Single(item => item.UnitId == "ru-l").Eliminated);
        Assert.Equal("LLMC", resolved.Effects.Single(item => item.UnitId == "ru-s").Checks[0].Kind);
        var wounded = Resolved(facts with
        {
            Rolls = Rolls(Dice("ru-l", Boxcars), Dice("ru-s", TwoThree), new Dictionary<string, int> { ["ru-l"] = 1 })
        });
        Assert.Equal((true, true), (wounded.Effects[0].Wounded, wounded.Effects[0].Broken));
        Assert.Equal("LLTC", wounded.Effects.Single(item => item.UnitId == "ru-s").Checks[0].Kind);
    }

    [Fact]
    public void ABailOutTakesARiderLeadersDrmAndElrReplacementNeverDisruption()
    {
        // D6.24, A10.7 EXC (pp. 205, 68): the Rider 8-1 leads the Bail Out NMC of his vehicle's Riders; A19.13: a failure by more than the ELR Replaces the
        // squad; A19.12: a PRC is never Disrupted; A15.1 and A10.2: no Heat of Battle, no LLMC.
        var rider = new FireAboard("ru-tank", FirePrc.Rider, false, null);
        MoraleCheckFacts Bail(string definition, int[] dice) => new(MoraleCheckFacts.Nmc, [Unit("ru-r", definition, aboard: rider)], 2, Rolls(Dice("ru-r", dice)))
        {
            Others = [Unit("ru-l", "defender-leader-8-1", aboard: rider)],
        };
        var morale = Reference.Definitions["defender-squad"].Morale!.Value;
        var broken = Resolved(Bail("defender-squad", Fives)).Effects[0];
        var check = broken.Checks[0];
        Assert.Equal((-1m, 9), (check.Drm.Sum(item => item.Value), check.FinalDr));
        var brokenResult = ScenarioA1MoraleCheck.BailOutResult(check, false, false);
        Assert.Equal((ScenarioA1RiderRules.Broken, true), (brokenResult, broken.Broken));
        Assert.True(ScenarioA1RiderRules.CheckAgrees(5, 5, morale, false, brokenResult, -1, 2));

        var replaced = Resolved(Bail("defender-squad", FiveSix)).Effects[0];
        var lesser = ScenarioA1FireReference.ReplacementOf("defender-squad");
        Assert.Equal(lesser ?? "defender-squad", replaced.FinalDefinitionId);
        var replacedResult = ScenarioA1MoraleCheck.BailOutResult(replaced.Checks[0], false, lesser is not null);
        Assert.True(ScenarioA1RiderRules.CheckAgrees(5, 6, morale, false, replacedResult, -1, 2));
        Assert.False(ScenarioA1RiderRules.CheckAgrees(5, 6, morale, false, ScenarioA1RiderRules.Passed, -1, 2));

        // An Original 2 calls for no Heat of Battle aboard; a unit with no lesser one is only broken, never Disrupted.
        Assert.Null(Resolved(Bail("defender-squad", SnakeEyes)).Effects[0].HeatOfBattle);
        var unreplaceable = Reference.Definitions.Values.First(item => item.IsMmc && item.Morale is <= 7 && item.BrokenMorale is not null && item.UnderscoredMorale != true
            && ScenarioA1FireReference.ReplacementOf(item.Id) is null && ScenarioA1FireReference.HalfSquadOf(item.Id) is not null);
        var notDisrupted = Resolved(Bail(unreplaceable.Id, FiveSix)).Effects[0];
        Assert.Equal((true, false), (notDisrupted.Broken, notDisrupted.Disrupted));
        Assert.Contains("prc-not-disrupted", notDisrupted.Events);
    }

    [Fact]
    public void ABrokenUnprotectedCrewThatFailsAMcIsEliminated()
    {
        // D5.311 (p. 203; ruling R70.10): a mortar's Area hit in woods at the BU OT SPW 251/1 attacks its Inherent crew as Personnel; a crew already broken
        // that fails its MC is eliminated by Casualty Reduction (A7.302); an older record, which says nothing of the crew, breaks it as before.
        FireVehicleEffect? Hit(int[] dice, bool? crewBroken)
        {
            var hit = new FireAttack("PFPh", "phasing", true, "bd01:G7:0", "bd01:G5:0", [], null, 2, true, new FireLos(false, 0, true, false), 7, "woods", [], 3, null)
            {
                Vehicles = [new FireVehicle("de-ht", "attacker-halftrack", "bd01:G5:0", false, false, false, false) { Prc = [], CrewBroken = crewBroken }],
                OrdnanceHit = new FireOrdnanceHit("ru-mtr", 2, false) { Area = true },
            };
            var result = ScenarioA1FireCalculator.Resolve(hit with
            {
                Rolls = new FireRolls(dice, null, Dice("de-ht", FiveSix), null)
            }, Reference);
            return result.Disposition == FireResolution.Resolved ? Assert.Single(result.VehicleEffects!) : null;
        }

        // The first IFT DR that calls on the crew for a MC, which its 11 fails.
        var dice = Enumerable.Range(1, 6).SelectMany(first => Enumerable.Range(1, 6).Select(second => new[] { first, second }))
            .First(pair => Hit(pair, null) is { CrewCheck: { Kind: not "NTC", Passed: false } });
        Assert.Equal(FireVehicleEffect.Broken, Hit(dice, null)!.CrewResult);
        var broken = Hit(dice, true)!;
        Assert.Equal((false, FireVehicleEffect.Eliminated), (broken.CrewCheck!.Passed, broken.CrewResult));
    }

    // Increment 3c: Voluntary Break and Voluntary Rout (task 70.1, part II; rulings R70.11, R70.12).

    /// <summary>A row of Locations by index, each ADJACENT to the next, the LOS between two clear at their distance unless the pair is blocked.</summary>
    private sealed class Line(params (int, int)[] blocked) : IRoutFactReader
    {
        public string Name(int location) => "bd01:A" + location + ":0";

        public IEnumerable<int> Neighbors(int location) => [location - 1, location + 1];

        public bool Playable(int location) => true;

        public RoutLocationFacts? Location(int location) => new("open-ground", false);

        public RoutLosFacts? Los(int fromLocation, int toLocation) =>
            new(!blocked.Contains((fromLocation, toLocation)) && !blocked.Contains((toLocation, fromLocation)), 0, Math.Abs(fromLocation - toLocation));

        public int? Distance(int one, int two) => Math.Abs(one - two);

        public bool Adjacent(int one, int two) => Math.Abs(one - two) == 1;

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) => (null, "not read");
    }

    private static RoutEnemyFacts Enemy(int at, int normalRange = 4, bool broken = false, bool armed = true, string id = "ru-1") =>
        new(id, at, armed, broken, false, !armed, false, false, false, normalRange);

    private static VoluntaryBreakFacts Breaker(bool broken = false, bool hero = false, bool berserk = false, bool unarmed = false, bool brokenSide = true, bool melee = false,
        bool aboard = false, bool personnel = true) =>
        new("de-h", true, true, personnel, broken, hero, berserk, unarmed, false, melee, aboard, brokenSide);

    private static VoluntaryRoutLeaderFacts Guide(bool broken = false, bool pinned = false, bool berserk = false, bool stacked = true, bool leader = true, bool routed = false) =>
        new("de-l", true, true, leader, broken, pinned, berserk, false, false, false, stacked, routed);

    [Fact]
    public void AUnitMayBreakInTheLosAndNormalRangeOfAKnownArmedUnbrokenEnemyOrAdjacentToOne()
    {
        // A10.41 (p. 66; ruling R70.11): the unit in 0 sees the 4-4-7 in 3, within its Normal Range of 4; not beyond it, not through a blocked LOS, and not
        // a broken one. ADJACENT, any unbroken enemy unit allows it, whatever the LOS; an Abandoned vehicle does not.
        var line = new Line();
        Assert.Equal("ru-1", ScenarioA1VoluntaryRout.Threat(line, [Enemy(3)], 0));
        Assert.Null(ScenarioA1VoluntaryRout.Threat(line, [Enemy(3, normalRange: 2)], 0));
        Assert.Null(ScenarioA1VoluntaryRout.Threat(new Line((3, 0)), [Enemy(3)], 0));
        Assert.Null(ScenarioA1VoluntaryRout.Threat(line, [Enemy(3, broken: true)], 0));
        Assert.Equal("ru-1", ScenarioA1VoluntaryRout.Threat(new Line((1, 0)), [Enemy(1, normalRange: 0)], 0));
        Assert.Null(ScenarioA1VoluntaryRout.Threat(line, [Enemy(1, normalRange: 0, armed: false)], 0));
        Assert.Null(ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(), () => "ru-1"));
        Assert.StartsWith("play.voluntary-break: de-h is neither in the LOS", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(), () => null), StringComparison.Ordinal);
    }

    [Fact]
    public void AVoluntaryBreakIsRefusedWhereBreakingWouldReduceTheUnitOrItNeverBreaksAndAfterARoutStep()
    {
        // A10.3, A10.41 (ruling R70.11): a broken unit would be Reduced and one with no broken side eliminated; heroes, berserk and Unarmed units never
        // break; the enemy is not read for any of them. The break is made at the RtPh's start, before any rout step.
        static string Never() => throw new InvalidOperationException("the enemy was read.");
        Assert.Contains("already broken", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(broken: true), Never), StringComparison.Ordinal);
        Assert.Contains("a hero", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(hero: true), Never), StringComparison.Ordinal);
        Assert.Contains("berserk", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(berserk: true), Never), StringComparison.Ordinal);
        Assert.Contains("Unarmed", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(unarmed: true), Never), StringComparison.Ordinal);
        Assert.Contains("no broken side", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(brokenSide: false), Never), StringComparison.Ordinal);
        Assert.Contains("in Melee", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(melee: true), Never), StringComparison.Ordinal);
        Assert.Contains("only Personnel", ScenarioA1VoluntaryRout.BreakUnitBar(Breaker(personnel: false), Never), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1VoluntaryRout.BreakPhaseBar("rph", false));
        Assert.Contains("a unit has routed", ScenarioA1VoluntaryRout.BreakPhaseBar("rtph", true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1VoluntaryRout.BreakPhaseBar("rtph", false));
        Assert.True(ScenarioA1VoluntaryRout.BreakRecordAllowed("rtph", false, [(true, false)]));
        Assert.False(ScenarioA1VoluntaryRout.BreakRecordAllowed("rtph", true, [(true, false)]));
        Assert.False(ScenarioA1VoluntaryRout.BreakRecordAllowed("rtph", false, [(true, true)]));
    }

    [Fact]
    public void AVoluntaryRoutLeaderAddsHisDrmToTheNmcAndIsEliminatedWhenItFails()
    {
        // A10.711 (p. 69; ruling R70.12): an unbroken, unpinned, non-berserk leader stacked with the broken unit routs with it; his -1 turns its NMC DR of 7
        // against 7 from a pin into a pass; he is eliminated when it Reduces or eliminates the unit, not when it pins it.
        Assert.Null(ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide()));
        Assert.Contains("pinned", ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide(pinned: true)), StringComparison.Ordinal);
        Assert.Contains("berserk", ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide(berserk: true)), StringComparison.Ordinal);
        Assert.Contains("is broken", ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide(broken: true)), StringComparison.Ordinal);
        Assert.Contains("not stacked", ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide(stacked: false)), StringComparison.Ordinal);
        Assert.Contains("not a leader", ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide(leader: false)), StringComparison.Ordinal);
        Assert.Contains("has routed", ScenarioA1VoluntaryRout.LeaderBar("de-s", Guide(routed: true)), StringComparison.Ordinal);
        Assert.Equal((-1, 0), (ScenarioA1VoluntaryRout.LeaderDrm(-1, false), ScenarioA1VoluntaryRout.LeaderDrm(-1, true)));
        Assert.True(ScenarioA1RoutRallyProjection.InterdictionAgrees(3, 4, -1, 7, ScenarioA1ResultTables.InterdictionPassed));
        Assert.True(ScenarioA1RoutRallyProjection.InterdictionAgrees(3, 4, 0, 7, ScenarioA1ResultTables.InterdictionPinned));
        Assert.True(ScenarioA1VoluntaryRout.LeaderEliminated(InterdictionOutcome.Reduced));
        Assert.True(ScenarioA1VoluntaryRout.LeaderEliminated(InterdictionOutcome.Eliminated));
        Assert.False(ScenarioA1VoluntaryRout.LeaderEliminated(InterdictionOutcome.Pinned));
        Assert.False(ScenarioA1VoluntaryRout.LeaderEliminated(InterdictionOutcome.Passed));
        Assert.True(ScenarioA1VoluntaryRout.LeaderStepAllowed(false, false, false, false, true, 2, false, 0, false, 12, false));
        Assert.False(ScenarioA1VoluntaryRout.LeaderStepAllowed(false, false, false, false, false, 2, false, 0, false, 12, false));
        Assert.False(ScenarioA1VoluntaryRout.LeaderStepAllowed(true, false, false, false, true, 2, false, 0, false, 12, false));
        Assert.True(ScenarioA1VoluntaryRout.InterdictionLeaderAgrees(true, false, true, true));
        Assert.False(ScenarioA1VoluntaryRout.InterdictionLeaderAgrees(true, false, false, true));
    }

    // Increment 3d: rout from a building, Interdiction by vehicles and Guns, Fanaticism by SSR (task 70.2; rulings R70.13 to R70.15).

    /// <summary>
    /// A street of four Locations, 0 to 3, each ADJACENT to the next: 0 and 1 one stone building, 2 Open Ground, 3 another, wooden building; every LOS clear.
    /// The reader says 0 and 1 are one building only when asked to.
    /// </summary>
    private sealed class Street(bool oneBuilding) : IRoutFactReader
    {
        private static readonly string[] Terrain = ["stone-building", "stone-building", "open-ground", "wooden-building"];

        public string Name(int location) => "bd01:" + "ABCD"[location] + "1:0";

        public IEnumerable<int> Neighbors(int location) => new[] { location - 1, location + 1 }.Where(next => next is >= 0 and <= 3);

        public bool Playable(int location) => true;

        public RoutLocationFacts? Location(int location) => new(Terrain[location], false);

        public RoutLosFacts? Los(int fromLocation, int toLocation) => new(true, 0, Math.Abs(fromLocation - toLocation));

        public int? Distance(int one, int two) => Math.Abs(one - two);

        public bool Adjacent(int one, int two) => Math.Abs(one - two) == 1;

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) =>
            (new InfantryEntry(Terrain[toLocation] == "open-ground" ? 2 : 4, Terrain[toLocation], false, false, false, false), null);

        public bool SameBuilding(int one, int two) => oneBuilding && one <= 1 && two <= 1;
    }

    /// <summary>A row of open Locations like <see cref="Line"/>, whose bearings run toward 0 at 90 degrees (north) and away from it at 270.</summary>
    private sealed class Bearings : IRoutFactReader
    {
        public string Name(int location) => "bd01:A" + location + ":0";

        public IEnumerable<int> Neighbors(int location) => [location - 1, location + 1];

        public bool Playable(int location) => true;

        public RoutLocationFacts? Location(int location) => new("open-ground", false);

        public RoutLosFacts? Los(int fromLocation, int toLocation) => new(true, 0, Math.Abs(fromLocation - toLocation));

        public int? Distance(int one, int two) => Math.Abs(one - two);

        public bool Adjacent(int one, int two) => Math.Abs(one - two) == 1;

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) => (null, "not read");

        public double? Bearing(int fromLocation, int toLocation) => toLocation < fromLocation ? 90 : 270;
    }

    private static readonly int[] OwnHex = [1];
    private static readonly int[] OwnHexAndTheOther = [1, 3];
    private static readonly int[] OutThroughItsBuilding = [1, 2, 3];
    private static readonly Dictionary<int, int> StreetReach = new() { [0] = 0, [1] = 4, [2] = 6, [3] = 10 };

    // Hexspine facings as the planner hands them (UnitFacing): 1 points at 60 degrees, 2 at 120, 4 at 240; north (90) lies on the spine between 1 and 2.
    private static RoutVehicleFacts Tank(bool crewExposed = false, bool openTopped = false, bool motion = false, bool stunned = false, bool abandoned = false, int hull = 1,
        int? turret = null, bool bmg = true, bool cmg = false, bool aamg = false, bool mainGun = false, int? gunRange = null, string? maType = "t") =>
        new(abandoned, motion, stunned, false, false, openTopped, crewExposed, hull, turret, bmg, cmg, aamg, false, mainGun, gunRange, maType);

    private static RoutEnemyFacts Armor(RoutVehicleFacts tank, int at = 5)
    {
        var (range, weapons) = ScenarioA1RoutCalculator.VehicleInterdiction(tank);
        return new RoutEnemyFacts("ru-t", at, true, false, false, true, false, false, false, ScenarioA1RoutCalculator.VehicleNormalRange(tank), range) { ArcWeapons = weapons };
    }

    [Fact]
    public void AUnitInABuildingMayIgnoreItsOwnBuildingsHexesWhenChoosingWhereToRout()
    {
        // A10.51's EXC (p. 67; ruling R70.13): "A broken unit in a building need not consider a hex of the same building in which it begins the RtPh ... as
        // its closest building hex if it prefers to rout out of that building altogether ... even if it must cross Open Ground or another building hex of the
        // same building to do so." The unit in 0 may make for 1 or for the other building in 3, by way of 1 and the street; read as two buildings, 1 binds it.
        Assert.Equal(OwnHex, ScenarioA1RoutCalculator.RoutTargets(new Street(false), [], 0, StreetReach));
        Assert.Equal(OwnHexAndTheOther, ScenarioA1RoutCalculator.RoutTargets(new Street(true), [], 0, StreetReach));
        Assert.Null(ScenarioA1RoutCalculator.RoutDestinationBar(new Street(true), [], "de-1", 0, OutThroughItsBuilding, false, 12, false, 7));
        Assert.Contains("goes on only into woods or building Locations", ScenarioA1RoutCalculator.RoutDestinationBar(new Street(false), [], "de-1", 0, OutThroughItsBuilding,
            false, 12, false, 7), StringComparison.Ordinal);

        // B23.71, B23.74 (the card editor's join, ruling R17.8): a building goes on across a building hexside into a building hex, not across a Rowhouse wall,
        // a Breach, or a Factory's outside wall.
        Assert.True(ScenarioA1RoutCalculator.BuildingGoesOn(true, true, "Stone Building", false, null, false));
        Assert.False(ScenarioA1RoutCalculator.BuildingGoesOn(true, false, "Stone Building", false, null, false));
        Assert.False(ScenarioA1RoutCalculator.BuildingGoesOn(true, true, "Stone Building", false, "Rowhouse Wall", false));
        Assert.False(ScenarioA1RoutCalculator.BuildingGoesOn(true, true, "Stone Factory", true, null, false));
    }

    [Fact]
    public void AVehicleInterdictsWithinItsMgsRangeAndCoveredArcAndNotWithAPositiveDrm()
    {
        // A10.532 (pp. 67, 68; ruling R70.14): the tank in 5 Interdicts the Open Ground in 0, five hexes north, with its BMG (Normal Range eight, D1.81) through
        // its VCA, facing 1 (60 degrees). Faced away (4), it would change its VCA; ten hexes off, it is beyond the BMG's range.
        var line = new Bearings();
        Assert.Equal("ru-t", ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank())], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(hull: 4))], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(), at: 10)], 0, 7));

        // "An AFV cannot Interdict if it would ... use armament which is currently penalized by the +1 BU or +1 Stun DRM", nor "Any unit whose FP is halved
        // (... Motion ...)". A BU tank's MA Gun takes Case I's +1 (C5.9: it "prevents Interdiction"); with its crew CE, the MA Interdicts to 16 hexes.
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(stunned: true))], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(motion: true))], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(bmg: false, mainGun: true), at: 12)], 0, 7));
        Assert.Equal("ru-t", ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(crewExposed: true, bmg: false, mainGun: true), at: 12)], 0, 7));

        // D1.82, D1.321: the CMG reaches twelve hexes through the TCA, but not from a RST turret whose crew is CE; D5.3: an open-topped AFV fires no weapon but
        // its BMG unless CE. D1.83: the AAMG fires in any direction, CE.
        Assert.Equal("ru-t", ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(cmg: true, turret: 2), at: 10)], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(crewExposed: true, cmg: true, turret: 2, maType: "rst"), at: 10)], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(openTopped: true, cmg: true, turret: 2), at: 10)], 0, 7));
        Assert.Equal("ru-t", ScenarioA1RoutCalculator.Interdictor(line, [Armor(Tank(crewExposed: true, hull: 4, aamg: true))], 0, 7));

        // A10.5 "unit/its-SW/Gun": the vehicle's Normal Range, whatever its arc, makes a broken unit in Open Ground rout and brings DM (A10.62).
        Assert.Equal((12, 16, 0), (ScenarioA1RoutCalculator.VehicleNormalRange(Tank(cmg: true)), ScenarioA1RoutCalculator.VehicleNormalRange(Tank(mainGun: true)),
            ScenarioA1RoutCalculator.VehicleNormalRange(Tank(abandoned: true))));
        Assert.Equal("ru-t", ScenarioA1RoutCalculator.ExposedInOpenGround(line, [Armor(Tank(hull: 4))], 0, 7));
        Assert.Equal("ru-t", ScenarioA1VoluntaryRout.Threat(line, [Armor(Tank(hull: 4))], 0));
    }

    [Fact]
    public void AGunThatMustChangeItsCaDoesNotInterdict()
    {
        // A10.532 (p. 67): "If a Gun would have to change its CA in order to fire ... a positive DRM would usually come into effect and thereby disallow its
        // Interdiction." The crew in 5 has its own range of 2; its Gun Interdicts to 16 hexes through its CA (1), not when faced away (4) or not working.
        var line = new Bearings();
        RoutEnemyFacts Crew(RoutArcWeapon? gun) => new("ru-gc", 5, true, false, false, false, false, false, false, Math.Max(2, gun is null ? 0 : gun.Range), 2)
        {
            ArcWeapons = gun is null ? null : [gun]
        };

        Assert.Equal("ru-gc", ScenarioA1RoutCalculator.Interdictor(line, [Crew(ScenarioA1RoutCalculator.GunArcWeapon(true, null, 1))], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(line, [Crew(ScenarioA1RoutCalculator.GunArcWeapon(true, null, 4))], 0, 7));
        Assert.Null(ScenarioA1RoutCalculator.GunArcWeapon(false, null, 1));
        Assert.Null(ScenarioA1RoutCalculator.Interdictor(new Line(), [Crew(ScenarioA1RoutCalculator.GunArcWeapon(true, null, 1))], 0, 7));
        Assert.Equal((16, 12, 0), (ScenarioA1RoutCalculator.GunNormalRange(true, null), ScenarioA1RoutCalculator.GunNormalRange(true, 12),
            ScenarioA1RoutCalculator.GunNormalRange(false, null)));
        Assert.Equal("ru-gc", ScenarioA1RoutCalculator.ExposedInOpenGround(line, [Crew(ScenarioA1RoutCalculator.GunArcWeapon(true, null, 4))], 0, 7));
    }

    private static readonly string[] RussianFanatics = ["fanatic:russian"];
    private static readonly string[] SecondGroupFanatics = ["fanatic:russian:russian-2"];
    private static readonly string[] MalformedFanatics = ["fanatic:"];
    private static readonly string[] TokenStatus = ["token"];

    [Fact]
    public void TheSsrsTokenSetsFanaticism()
    {
        // A10.8 (p. 69; ruling R70.15): "Fanaticism is evoked only by SSR or Battle Hardening": fanatic:<side>, or fanatic:<side>:<group>, makes the Personnel of
        // that side, or OB group, set up Fanatic; never a Dummy, a vehicle, or the other side's.
        Assert.Equal<(string, string?)?>(("russian", null), ScenarioA1GameStart.FanaticToken("fanatic:russian"));
        Assert.Equal<(string, string?)?>(("russian", "russian-2"), ScenarioA1GameStart.FanaticToken("fanatic:russian:russian-2"));
        Assert.Null(ScenarioA1GameStart.FanaticToken("fanatic:"));
        Assert.Null(ScenarioA1GameStart.FanaticToken("fanatic:russian:a:b"));
        Assert.True(ScenarioA1GameStart.FanaticBySsr(RussianFanatics, "russian", "russian-1", true, false));
        Assert.False(ScenarioA1GameStart.FanaticBySsr(RussianFanatics, "german", null, true, false));
        Assert.False(ScenarioA1GameStart.FanaticBySsr(RussianFanatics, "russian", null, true, true));
        Assert.False(ScenarioA1GameStart.FanaticBySsr(RussianFanatics, "russian", null, false, false));
        Assert.True(ScenarioA1GameStart.FanaticBySsr(SecondGroupFanatics, "russian", "russian-2", true, false));
        Assert.False(ScenarioA1GameStart.FanaticBySsr(SecondGroupFanatics, "russian", "russian-1", true, false));

        // A new game refuses a malformed token; a card's token names a side of the card, and an OB group of it.
        Assert.StartsWith("play.fanatic-rule:", ScenarioA1NightAndWeather.RulesBar(MalformedFanatics, (_, _) => true, true, true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1NightAndWeather.RulesBar(RussianFanatics, (_, _) => true, true, true));
        var rule = new CardRuleFacts(1, "token", 1, null, "The Russians are Fanatic.", [], []) { FanaticTokens = [("fanatic:soviet", false)] };
        Assert.Contains("card.ssr: SSR 1's 'fanatic:soviet' names no side of the card", Assert.Single(ScenarioA1SetupCalculator.RuleDiagnostics([rule], TokenStatus, [])),
            StringComparison.Ordinal);
    }
}
