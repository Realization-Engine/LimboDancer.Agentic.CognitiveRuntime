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
    public void ABrokenUnitAndAUnitInMeleeInTheEnemysLocationInspectIt()
    {
        // Ruling R70.40 (A11.12, A11.14, A11.19): a broken unit and one held in Melee (not Good Order) are no Inspection seers, yet a unit of the viewer's
        // own in the enemy stack's Location inspects it whatever its state, with no seer read; before play none does (A2.9).
        Assert.False(ScenarioA1Sight.Sees(Seer(goodOrder: false, unbroken: false), SightPurpose.Inspection));
        Assert.False(ScenarioA1Sight.Sees(Seer(goodOrder: false), SightPurpose.Inspection));
        Assert.True(ScenarioA1Sight.Inspectable(true, true, () => throw new InvalidOperationException("the seers were read.")));
        Assert.False(ScenarioA1Sight.Inspectable(true, false, () => false));
        Assert.False(ScenarioA1Sight.Inspectable(false, true, () => true));

        // A prisoner (A20.5) and a Dummy (A12.11) of the viewer's in the Location inspect nothing (the referee's finding at the reviews).
        Assert.True(ScenarioA1Sight.InspectsByPresence(false, false));
        Assert.False(ScenarioA1Sight.InspectsByPresence(false, true));
        Assert.False(ScenarioA1Sight.InspectsByPresence(true, false));
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

    // Increment 3e: the owner's and the viewer's choices on a "?", and concealment for a vehicle and an Emplaced Gun (tasks 70.4, 70.5; rulings R70.20 to
    // R70.22).

    [Fact]
    public void AQuestionMarkIsRemovedVoluntarilyOnceInPlayADummyEliminatedAndAHiddenUnitRevealed()
    {
        // A12.14 (p. 78): "The owning player can voluntarily remove any concealment at any time during his or his opponent's Player Turn"; during setup it
        // is not, nor of units of two sides, off the map, or under no "?". A Dummy is removed (A12.11); a hidden unit is revealed in full (A12.31).
        Assert.StartsWith("play.remove-concealment: a \"?\" is removed voluntarily once play has begun", ScenarioA1ConcealmentChoices.RemovalBar(true, 1, false, 1),
            StringComparison.Ordinal);
        Assert.StartsWith("play.remove-concealment: the units are units of one side", ScenarioA1ConcealmentChoices.RemovalBar(false, 2, false, 2), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1ConcealmentChoices.RemovalBar(false, 1, true, 1));
        Assert.NotNull(ScenarioA1ConcealmentChoices.RemovalBar(false, 0, false, 0));
        Assert.Null(ScenarioA1ConcealmentChoices.RemovalBar(false, 2, false, 1));
        Assert.True(ScenarioA1ConcealmentChoices.RemovalEliminates(dummy: true));
        Assert.False(ScenarioA1ConcealmentChoices.RemovalEliminates(dummy: false));
        Assert.Equal("play.remove-concealment: g1 gives up its \"?\" voluntarily, revealed in full (A12.14, A12.31); 1 Dummy is removed (A12.11) (ruling R70.20)",
            ScenarioA1ConcealmentChoices.RemovalText(["g1"], 1));
    }

    [Fact]
    public void TheViewerMayDeclineOnlyWhenEveryLossSeerWithinSixteenIsConcealed()
    {
        // A12.14 (p. 78): "If the only Good Order enemy ground unit in LOS is itself concealed ... that enemy unit must completely forfeit its '?' momentarily
        // ... if it opts to force the friendly unit to lose his". A concealed Loss seer is a Loss seer, not one in view; a hidden one is neither.
        Assert.True(ScenarioA1Sight.Sees(Seer() with
        {
            Concealed = true
        }, SightPurpose.Loss));
        Assert.False(ScenarioA1Sight.Sees(Seer() with
        {
            Concealed = true
        }, SightPurpose.LossInView));
        Assert.True(ScenarioA1Sight.Sees(Seer(), SightPurpose.LossInView));
        Assert.False(ScenarioA1Sight.Sees(Seer(hidden: true), SightPurpose.LossInView));

        // Offered with a Loss seer within 16 and none unconcealed within 16; not with an unconcealed one there, nor with no seer near (the loss is not
        // forced at all), nor for a loss that needs no seer (a forced back, A12.15), whose unconcealed seers are never read.
        Assert.True(ScenarioA1Sight.ViewerMayDecline(false, 5, () => null));
        Assert.True(ScenarioA1Sight.ViewerMayDecline(false, 5, () => 18));
        Assert.False(ScenarioA1Sight.ViewerMayDecline(false, 5, () => 9));
        Assert.False(ScenarioA1Sight.ViewerMayDecline(false, 17, () => throw new InvalidOperationException("not read")));
        Assert.False(ScenarioA1Sight.ViewerMayDecline(true, 5, () => throw new InvalidOperationException("not read")));
    }

    // The gain search's map: the candidate at Location 0, every enemy at Location 1, at the range and with the LOS a test gives.
    private sealed class GainMap(int range, bool los) : IConcealmentFactReader
    {
        public int? Range(int from, int target) => range;

        public bool LosOpen(int from, int target) => los;

        public int? Nvr(int unit) => null;

        public bool Illuminated(int location) => false;
    }

    private static ConcealmentCandidateFacts Candidate(string id, string kind = "asl:squad", bool vehicle = false, string terrain = "open-ground", bool gun = false) =>
        new(id, kind, true, true, vehicle, !vehicle, false, true, kind == "asl:squad", kind == "asl:half-squad", 0, false, false, false, false, false, false,
            () => gun, () => false, () => terrain, () => false, () => 0);

    private static List<ConcealmentGain> Gains(int range, bool los, params ConcealmentCandidateFacts[] units) =>
        ScenarioA1FireFollowUps.ConcealmentGains(units, [new WatchingEnemyFacts(0, true, Seer(at: 1, side: "russian"))], false, 7, new GainMap(range, los));

    private static readonly string[] GunOfTheCrew = ["de-gun"];
    private static readonly string[] PassengerOfTheTruck = ["de-p"];

    [Fact]
    public void AnEmplacedGunsCrewAndAVehicleGainByCasesJAndKOnly()
    {
        // The Concealment Table (A12.121, p. 107, read from its render): an Emplaced Gun (and manning Infantry) and a vehicle (and PRC) gain "?" only out of
        // the LOS of every unbroken enemy ground unit, Case J in Concealment Terrain, Case K out of it (a dr within 16 hexes); never in such a LOS, at any
        // range, where Infantry in Concealment Terrain beyond 16 has Case I.
        var crew = Candidate("de-gc", "asl:crew", gun: true) with
        {
            HoldsEmplacedGun = true,
            Shared = () => [("de-gun", true)]
        };
        var truck = Candidate("de-truck", "asl:vehicle", vehicle: true) with
        {
            VehicleConcealmentTerrain = () => false,
            Shared = () => [("de-p", true)]
        };
        var k = Gains(10, false, crew, truck);
        Assert.Equal(("de-gc", (int?)4), (k[0].Id, k[0].Drm));
        Assert.Equal(GunOfTheCrew, k[0].With);
        Assert.Equal(("de-truck", (int?)4), (k[1].Id, k[1].Drm));
        Assert.Equal(PassengerOfTheTruck, k[1].With);

        // Case J: Concealment Terrain (for the vehicle, as it reads it in play) gains with no dr; beyond 16 out of it, Case K needs none either.
        Assert.Null(Gains(10, false, truck with
        {
            VehicleConcealmentTerrain = () => true
        }).Single().Drm);
        Assert.Null(Gains(20, false, crew with
        {
            TerrainKey = () => "woods"
        }).Single().Drm);
        Assert.Null(Gains(20, false, truck).Single().Drm);

        // In a LOS at 20 hexes: the squad in woods takes Case I's dr; the crew and the vehicle gain nothing.
        Assert.Equal("de-s", Gains(20, true, Candidate("de-s", terrain: "woods"), crew with
        {
            TerrainKey = () => "woods"
        }, truck with
        {
            VehicleConcealmentTerrain = () => true
        }).Single().Id);

        // A crew manning a Gun not Emplaced, a unit aboard, and a vehicle whose Passenger is broken gain nothing; a large vehicle's US# is 5 (A1.6).
        Assert.Empty(Gains(10, false, Candidate("de-gc", "asl:crew", gun: true), Candidate("de-p") with
        {
            Aboard = true
        },
            truck with
            {
                Shared = () => [("de-p", false)]
            }));
        Assert.Equal(5, Gains(10, false, truck with
        {
            Large = true
        }).Single().Drm);
    }

    [Fact]
    public void TheConcealmentDrTakesLaxAndStealthy()
    {
        // A12.122 (p. 77): +1 Lax, -1 Stealthy, with the US# (the example's Hero: "6 [dr] +1 [US#] -1 [Leadership] -1 [Stealthy] = 5").
        Assert.Equal(4, Gains(10, false, Candidate("de-s") with
        {
            Lax = true
        }).Single().Drm);
        Assert.Equal(0, Gains(10, false, Candidate("de-h", "asl:hero") with
        {
            Stealthy = true
        }).Single().Drm);
        Assert.Equal(3, Gains(10, false, Candidate("de-s")).Single().Drm);
    }

    private static readonly string?[] OrchardRoad = ["Orchard", "Dirt Road", null, "Orchard", "Orchard", "Dirt Road"];
    private static readonly string[] GrainRoad = ["Grain", "Dirt Road"];
    private static readonly string[] OpenRoad = ["Open Ground", "Dirt Road"];
    private static readonly string[] VehicleHip = ["hip:german:1"];

    private static SetupCardFacts HipCard(params string[] tokens) => new(
        [new SetupSideFacts("german", null, true, [], [new SetupGroupFacts("german-1", "Panzers", null, 1,
            [new SetupAreaFacts("area", "hex-numbers", null, "bd01", 1, 9, null, null, null, null, null), new SetupAreaFacts("entry", "entry", null, null, null, null, 2, "top", null, null, null)],
            [new SetupLineFacts("tank", 1, null), new SetupLineFacts("squad", 2, "entry")])]),
         new SetupSideFacts("russian", null, false, [], [new SetupGroupFacts("russian-1", "Guards", null, null,
            [new SetupAreaFacts("area", "hex-numbers", null, "bd01", 1, 9, null, null, null, null, null)], [new SetupLineFacts("rsquad", 1, null)])])],
        1, "bd01", false, null, null, null, null, tokens);

    private static SetupCounterFacts Placed(string id, string kind, string definition, bool concealed = false, bool hidden = false, bool road = false, bool offBoard = false,
        bool manning = false, bool equipment = false, string? side = null) =>
        new(id, side ?? "german", side == "russian" ? "russian-1" : "german-1", definition, kind,
            offBoard ? null : new SetupLocationFacts("bd01:C5:0", "bd01", "C5", 5) { RoadConcealment = road },
            concealed, hidden, false, equipment, true, offBoard, false, false, offBoard ? "entry" : null, null, manning, false);

    [Fact]
    public void AVehicleOnAWoodsOrchardBrushOrGrainRoadSetsUpUnderQuestionMarkOrHidden()
    {
        // A12.2 (p. 79): "A vehicle that sets up in a woods-road (or orchard-/brush-/grain- road) hex is considered to be in Concealment Terrain for the
        // purposes of placing OB-designated '?' (12.12) and of using SSR-allowed HIP"; grain only in season (B15.6); an open road is none.
        Assert.True(ScenarioA1ConcealmentChoices.RoadConcealmentHex(true, OrchardRoad, 3));
        Assert.True(ScenarioA1ConcealmentChoices.RoadConcealmentHex(true, GrainRoad, 7));
        Assert.False(ScenarioA1ConcealmentChoices.RoadConcealmentHex(true, GrainRoad, 3));
        Assert.False(ScenarioA1ConcealmentChoices.RoadConcealmentHex(true, OpenRoad, 7));
        Assert.False(ScenarioA1ConcealmentChoices.RoadConcealmentHex(false, OrchardRoad, 7));
        Assert.True(ScenarioA1ConcealmentChoices.SetupConcealmentTerrain("open-ground", 7, true, () => true));
        Assert.False(ScenarioA1ConcealmentChoices.SetupConcealmentTerrain("open-ground", 7, false, () => throw new InvalidOperationException("not read")));
        Assert.True(ScenarioA1ConcealmentChoices.SetupConcealmentTerrain("woods", 7, false, () => throw new InvalidOperationException("not read")));

        // The setup: the tank under its group's OB "?" on an orchard road; on an open road it is refused, and a squad on the orchard road is refused too.
        string[] Concealment(params SetupCounterFacts[] counters) => [.. ScenarioA1SetupCalculator.Check(HipCard(), counters, _ => "open-ground", _ => true, _ => null, 7, null)
            .Reasons.Where(reason => reason.StartsWith("play.setup-concealment", StringComparison.Ordinal))];
        Assert.Empty(Concealment(Placed("de-t", "asl:vehicle", "tank", concealed: true, road: true)));
        Assert.Single(Concealment(Placed("de-t", "asl:vehicle", "tank", concealed: true)));
        Assert.Single(Concealment(Placed("de-s", "asl:squad", "squad", concealed: true, road: true)));
    }

    [Fact]
    public void AVehicleSetsUpHiddenByItsSsr()
    {
        // A12.3 (p. 80; ruling R70.22): "A SSR may allow HIP for one or more units", vehicles included, in Concealment Terrain (its road hex, A12.2); with
        // no SSR, or out of Concealment Terrain, it is refused. A hidden vehicle takes one squad-equivalent of the SSR's HIP (a reading).
        string[] Hidden(SetupCardFacts card, params SetupCounterFacts[] counters) => [.. ScenarioA1SetupCalculator.Check(card, counters, _ => "open-ground", _ => true, _ => null, 7, null)
            .Reasons.Where(reason => reason.StartsWith("play.setup-hidden", StringComparison.Ordinal))];
        Assert.Empty(Hidden(HipCard(VehicleHip), Placed("de-t", "asl:vehicle", "tank", hidden: true, road: true)));
        Assert.Contains("no SSR gives german HIP", Assert.Single(Hidden(HipCard(), Placed("de-t", "asl:vehicle", "tank", hidden: true, road: true))), StringComparison.Ordinal);
        Assert.Contains("not Concealment Terrain", Assert.Single(Hidden(HipCard(VehicleHip), Placed("de-t", "asl:vehicle", "tank", hidden: true))), StringComparison.Ordinal);
    }

    [Fact]
    public void AnEmplacedGunSetsUpHiddenOutOfConcealmentTerrainAndIsPlacedBeneathQuestionMarkOnceSeen()
    {
        // A12.34 (p. 80): "An Emplaced Gun may also always use HIP if not in Concealment Terrain, provided it sets up out of the LOS of all enemy ground
        // units--but must be placed onboard under a '?' as soon as a Good Order enemy ground unit has a LOS to it regardless of range". The setup takes it
        // hidden in Open Ground with its crew and no SSR.
        var reasons = ScenarioA1SetupCalculator.Check(HipCard(), [Placed("de-gun", "asl:gun", "gun", hidden: true, manning: true, equipment: true),
            Placed("de-gc", "asl:crew", "crew", hidden: true, manning: true)], _ => "open-ground", _ => true, _ => null, 7, null).Reasons;
        Assert.DoesNotContain(reasons, reason => reason.StartsWith("play.setup-hidden", StringComparison.Ordinal));

        // In play: out of Concealment Terrain and seen, it is placed beneath "?"; unseen, or in Concealment Terrain (where it keeps HIP even in LOS), it is
        // not; a Gun not hidden or not Emplaced reads nothing.
        Assert.True(ScenarioA1ConcealmentChoices.EmplacedHipLost(true, true, () => false, () => true));
        Assert.False(ScenarioA1ConcealmentChoices.EmplacedHipLost(true, true, () => false, () => false));
        Assert.False(ScenarioA1ConcealmentChoices.EmplacedHipLost(true, true, () => true, () => throw new InvalidOperationException("not read")));
        Assert.False(ScenarioA1ConcealmentChoices.EmplacedHipLost(false, true, () => throw new InvalidOperationException("not read"), () => true));
        Assert.False(ScenarioA1ConcealmentChoices.EmplacedHipLost(true, false, () => throw new InvalidOperationException("not read"), () => true));
    }

    // Off board, every counter waits under one "?" or none: the OB reasons a test reads.
    private static string[] OffBoardReasons(SetupCardFacts card, params SetupCounterFacts[] counters) => [.. ScenarioA1SetupCalculator.Check(card, counters, _ => "woods", _ => true,
        _ => null, 7, null).Reasons.Where(reason => reason.StartsWith("play.setup-offboard", StringComparison.Ordinal) || reason.StartsWith("play.setup-concealment", StringComparison.Ordinal))];

    [Fact]
    public void AnyReinforcementWaitsOffBoardUnderANonObQuestionMark()
    {
        // A12.12 (p. 77; ruling R70.35, lifting R20.5's refusal): "Thereafter, a '?' may be placed on top of any non-concealed Good Order unit/stack before
        // it enters the mapboard for the first time", whatever the OB gives: the Russian group with no "?" may, and the German group's OB "?" are not used
        // by it (the tank under its one OB "?" on the map still fits). Never hidden off board, and never a Dummy (no card gives an entering group Dummies).
        var none = HipCard() with
        {
            Sides = [HipCard().Sides[1]]
        };
        Assert.Empty(OffBoardReasons(none, Placed("r-1", "asl:squad", "rsquad", concealed: true, offBoard: true, side: "russian")));
        Assert.Empty(OffBoardReasons(HipCard(), Placed("de-t", "asl:vehicle", "tank", concealed: true), Placed("de-1", "asl:squad", "squad", concealed: true, offBoard: true)));
        Assert.Contains("never hidden nor a Dummy", Assert.Single(OffBoardReasons(HipCard(), Placed("de-1", "asl:squad", "squad", hidden: true, offBoard: true))),
            StringComparison.Ordinal);
    }

    [Fact]
    public void AStackOfTwoWaitingOffBoardTakesOneQuestionMark()
    {
        // A12.12's EXC (p. 77; ruling R70.35): "only one non-OB-designated '?' can be placed per stack": the two Russian squads of one entry area wait under
        // one "?", in a group whose OB gives none, and nothing is refused.
        var none = HipCard() with
        {
            Sides = [HipCard().Sides[1]]
        };
        Assert.Empty(OffBoardReasons(none, Placed("r-1", "asl:squad", "rsquad", concealed: true, offBoard: true, side: "russian"),
            Placed("r-2", "asl:squad", "rsquad", concealed: true, offBoard: true, side: "russian")));
    }

    // Increment 3f: Bypass and detection (task 70.8; ruling R70.23).

    private static BypassLocationUnitFacts AtBypass(string id, string side = "russian", bool concealed = true, bool hidden = false, bool dummy = false, bool captured = false,
        bool active = true) => new(id, active, side, captured, concealed, hidden, dummy);

    private static readonly string[] OneMover = ["g1"];
    private static readonly string[] RevealedInLane = ["r1", "g2"];
    private static readonly string[] RevealedInObstacle = ["r1", "rh"];
    private static readonly string[] DummyThere = ["rd"];
    private static readonly string[] ConcealedOne = ["r1"];
    private static readonly int[] Lane = [2, 1];
    private static readonly BypassHexsideFacts[] OpenLane = [new("Open Ground", false), new("Open Ground", false)];

    private static MoveStepFacts StepFacts(bool minimumMove = false, bool obstacleHeld = false, MoveRevealUnitFacts[]? there = null) =>
        new(OneMover, "bd03:E6:0", "bd03:F5:0", false, null, obstacleHeld, "woods", null, 4, false, false, minimumMove, [], false, false, false, false, there ?? [], [], ObstacleHeld: obstacleHeld);

    [Fact]
    public void AMinimumMoveIntoAConcealedUnitIsForcedBackAndPinsNone()
    {
        // A12.15 (p. 78; ruling R70.23): the refusal of a Minimum Move into concealed units is lifted, and the normal forced back applies. A4.134 (p. 48):
        // "After a unit has entered a hex by Minimum Move ... all unbroken survivors become both pinned and CX"; forced back, it entered none.
        UnitAtTargetFacts[] there = [new("r1", true, "russian", false, false, false)];
        var enemies = ScenarioA1MovementCalculator.EnemiesAtTarget(there, "german", false, false, false, false, "g1", "bd03:F5:0");
        Assert.Null(enemies.Refusal);
        Assert.Equal(ConcealedOne, enemies.EnemiesThere);
        var verdict = ScenarioA1MovementCalculator.MoveStep(StepFacts(minimumMove: true, there: [new("r1", false, false)]));
        Assert.True(verdict.ForcedBack);
        Assert.Contains(", a Minimum Move (A4.134)", verdict.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain("pinned and CX", verdict.Summary, StringComparison.Ordinal);
        Assert.False(ScenarioA1MovementCalculator.MinimumMoveEntered(true, true));
        Assert.True(ScenarioA1MovementCalculator.MinimumMoveEntered(true, false));
        Assert.Equal(OneMover, ScenarioA1MovementCalculator.EndingMembers(true, true, OneMover, null, null));
    }

    [Fact]
    public void AStackForcedBackFromItsBypassExitKeepsItsLaneAndEveryQuestionMarkThereIsLost()
    {
        // A12.151 (p. 79): "should ... the presence of other unknown units in the planned exit hex ... force a Bypass Infantry unit ... to end its MPh
        // prematurely in the Bypass Location with a concealed enemy unit, all concealment in that Location is lost". The lane is kept; every "?" there of
        // both sides goes, a Dummy's by its removal; the mover's went with the forced back; a hidden unit, under no "?", stays hidden.
        Assert.True(ScenarioA1BypassDetection.KeepsLane(true, true));
        Assert.False(ScenarioA1BypassDetection.KeepsLane(true, false));
        Assert.False(ScenarioA1BypassDetection.KeepsLane(false, true));
        BypassLocationUnitFacts[] there = [AtBypass("g1", "german"), AtBypass("r1"), AtBypass("rd", dummy: true), AtBypass("rh", concealed: false, hidden: true),
            AtBypass("g2", "german"), AtBypass("rc", captured: true)];
        var lost = ScenarioA1BypassDetection.AllConcealmentLost(there, "german", false, OneMover);
        Assert.Equal(RevealedInLane, lost.Revealed);
        Assert.Equal(DummyThere, lost.DummiesRemoved);

        // With no concealed enemy unit in the Location nothing is lost, the moving side's own "?" neither.
        Assert.False(ScenarioA1BypassDetection.AllConcealmentLost([AtBypass("g2", "german"), AtBypass("rh", concealed: false, hidden: true)], "german", false, OneMover).Any);

        // Nor with enemy Dummies alone: "with a concealed enemy unit", and a Dummy is no unit (A12.11; the referee's finding at the reviews).
        Assert.False(ScenarioA1BypassDetection.AllConcealmentLost([AtBypass("g2", "german"), AtBypass("rd", dummy: true)], "german", false, OneMover).Any);
    }

    [Fact]
    public void ABypassMemberBrokenOrPinnedEndsItsMphWhereTheQuestionMarksAreLost()
    {
        // A12.151, A4.33 (pp. 79, 50): "Any Infantry unit that becomes pinned ... while using Bypass ... Concealed enemy units in the Location lose that status
        // (12.151) at the end of the moving unit's MPh"; A4.32 for a broken one. A vehicle's move, and a stack not in Bypass, read nothing.
        Assert.True(ScenarioA1BypassDetection.EndsInBypass(true, false, [(true, false, true)]));
        Assert.True(ScenarioA1BypassDetection.EndsInBypass(true, false, [(true, false, false), (true, true, false)]));
        Assert.False(ScenarioA1BypassDetection.EndsInBypass(true, false, [(true, false, false), (false, true, false)]));
        Assert.False(ScenarioA1BypassDetection.EndsInBypass(false, false, [(true, false, true)]));
        Assert.False(ScenarioA1BypassDetection.EndsInBypass(true, true, [(true, false, true)]));
    }

    [Fact]
    public void AnAttemptToOccupyAnObstacleWithConcealedUnitsRevealsThemAndForcesTheStackBack()
    {
        // A12.151 (p. 78): "The opposing Infantry unit must actually attempt to occupy the concealed unit's obstacle and Location (not using Bypass) to cause
        // loss of concealment"; with A12.15, the stack is forced back into its lane, where every "?" of the Location is lost, hidden units placed first.
        BypassLocationUnitFacts[] held = [AtBypass("g1", "german"), AtBypass("r1"), AtBypass("rh", concealed: false, hidden: true), AtBypass("rd", dummy: true)];
        var forced = ScenarioA1BypassDetection.Occupy(held, "german", OneMover, false);
        Assert.Equal(ObstacleAttempt.ForcedBack, forced.Attempt);
        Assert.Equal(RevealedInObstacle, forced.Reveal.Revealed);
        Assert.Equal(DummyThere, forced.Reveal.DummiesRemoved);

        // Only Dummies there: they are removed and the stack occupies the obstacle; a moving stack of Dummies alone is removed; no "?" there, no reveal.
        var dummies = ScenarioA1BypassDetection.Occupy([AtBypass("g1", "german"), AtBypass("rd", dummy: true)], "german", OneMover, false);
        Assert.Equal((ObstacleAttempt.Occupies, DummyThere.Length), (dummies.Attempt, dummies.Reveal.DummiesRemoved.Count));
        Assert.Equal(ObstacleAttempt.MoversRemoved, ScenarioA1BypassDetection.Occupy(held, "german", OneMover, true).Attempt);
        var none = ScenarioA1BypassDetection.Occupy([AtBypass("g1", "german")], "german", OneMover, false);
        Assert.Equal((ObstacleAttempt.Occupies, false), (none.Attempt, none.Reveal.Any));

        // A Known enemy unit in the obstacle still bars the step (A4.14); concealed ones do not, and the step reads the obstacle's cost.
        MoveEntryVerdict Entry(bool known) => ScenarioA1TerrainCosts.MoveEntry(new MoveEntryFacts(true, Lane, true, true, false, true, known, false, "woods", 0, true, true, 3, null, "x"),
            () => 3, _ => false, () => 0);
        Assert.StartsWith("play.move-bypass: Infantry may not occupy an obstacle that holds a Known enemy unit", Entry(true).Refusal, StringComparison.Ordinal);
        Assert.Equal("woods", Entry(false).Occupy!.Terrain);

        // The step's record: forced back with the MF spent, its move ending.
        var verdict = ScenarioA1MovementCalculator.MoveStep(StepFacts(obstacleHeld: true));
        Assert.True(verdict.ForcedBack);
        Assert.Contains("g1 attempts to occupy the obstacle of bd03:F5:0", verdict.Summary, StringComparison.Ordinal);
        Assert.Contains("(A12.15, A12.151)", verdict.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void AFirerInTheBypassedObstacleMayFireTpbfAtTheBypassingStack()
    {
        // A12.151 (p. 79): "The previously concealed DEFENDING unit(s) may use TPBF against any units in the same Location"; the lane's terrain is the
        // target's, with no TEM of the obstacle. A Snap Shot at a Bypass step stays refused. Their TPBF is an option, never Mandatory Fire (A8.312).
        var tpbf = ScenarioA1FireMapRules.BypassTerrain(OpenLane, true, false, false);
        Assert.Equal((true, "open-ground"), (tpbf.Refusal is null, tpbf.Terrain));
        Assert.StartsWith("play.fire-bypass: a Snap Shot", ScenarioA1FireMapRules.BypassTerrain(OpenLane, true, true, false).Refusal, StringComparison.Ordinal);
        MandatoryDefenderFacts[] defenders = [new("r1", true, false, true, 0, false, false, false, false, false, false)];
        Assert.Single(ScenarioA1MandatoryFire.Owed(true, true, true, true, defenders, _ => true));
        Assert.Empty(ScenarioA1MandatoryFire.Owed(true, true, true, true, defenders, _ => true, inBypass: true));
    }

    [Fact]
    public void AVehicleEndingItsMphInBypassRevealsTheConcealedUnitsThereUnlessBroken()
    {
        // A12.42 (p. 80): concealed units are revealed "if the vehicle/its-Passenger(s)/Rider(s) end its MPh in that Location [EXC: a 'broken' vehicle
        // cannot cause concealment loss; 12.1]"; A12.1 (p. 76): "neither an inherent crew, nor Passenger(s), nor Rider(s)". Hidden units stay hidden.
        Assert.False(ScenarioA1BypassDetection.VehicleEndRevealsInBypass(true, false, () => false));
        Assert.True(ScenarioA1BypassDetection.VehicleEndRevealsInBypass(true, false, () => true));
        Assert.True(ScenarioA1BypassDetection.VehicleEndRevealsInBypass(true, true, () => throw new InvalidOperationException("not read")));
        Assert.False(ScenarioA1BypassDetection.VehicleEndRevealsInBypass(false, true, () => throw new InvalidOperationException("not read")));
        var reveal = ScenarioA1BypassDetection.VehicleReveals([AtBypass("r1"), AtBypass("rh", concealed: false, hidden: true), AtBypass("rd", dummy: true), AtBypass("g2", "german")],
            "german");
        Assert.Equal(ConcealedOne, reveal.Revealed);
        Assert.Equal(DummyThere, reveal.DummiesRemoved);
    }

    // Increment 3g (task 70.9): Searching (A12.152, A12.154; ruling R70.24) and Mopping Up in any building (A12.153; ruling R70.25).
    private static SearcherFacts Searcher(string id = "g1", bool mmc = true, bool leader = false, int halfSquads = 2, int? leadership = null, bool member = true, bool pinned = false,
        bool broken = false, bool infantry = true, bool hero = false, bool lax = false, bool cx = false, bool wounded = false) =>
        new(id, true, true, member, infantry, mmc, leader, broken, false, pinned, false, false, hero, false, halfSquads, leadership, wounded, lax, cx);

    private static readonly string[] TwoHexes = ["bd01:L4:0", "bd01:M4:0"];
    private static readonly string[] SameHexTwice = ["bd01:L4:0", "bd01:L4:0"];
    private static readonly string[] SquadAndLeaderNotes = ["-1 for 2 HS-equivalents", "-1 leadership"];
    private static readonly string[] OwnAndL4 = ["bd01:M5:0", "bd01:L4:0"];
    private static readonly string[] NoPlace = [];

    [Fact]
    public void TheSearchDrTakesStealthHalfSquadsTheBestLeaderLaxAndCx()
    {
        // A12.152 (p. 79): -1 per Stealthy Searching unit, -1 per HS equivalent > one HS, + the best participating leader's leadership, +1 per Lax or CX
        // Searching unit (per each condition). A squad and an 8-1 leader: -1 for two HS-equivalents and -1 leadership.
        var (drm, notes) = ScenarioA1Searching.Drm([Searcher(), Searcher("l1", mmc: false, leader: true, halfSquads: 0, leadership: -1)]);
        Assert.Equal(-2, drm);
        Assert.Equal(SquadAndLeaderNotes, notes);
        Assert.True(ScenarioA1Searching.LeadershipApplied([Searcher("l1", mmc: false, leader: true, halfSquads: 0, leadership: -1)]));

        // A Lax CX HS adds one for each condition; a hero is Stealthy; a wounded 8-0 leader's leadership is +1, the best when he is the only one.
        Assert.Equal(2, ScenarioA1Searching.Drm([Searcher(halfSquads: 1, lax: true, cx: true)]).Drm);
        Assert.Equal(-1, ScenarioA1Searching.Drm([Searcher(halfSquads: 1), Searcher("h1", mmc: false, halfSquads: 0, hero: true)]).Drm);
        Assert.Equal(1, ScenarioA1Searching.Drm([Searcher(halfSquads: 1), Searcher("l1", mmc: false, leader: true, halfSquads: 0, leadership: 0, wounded: true)]).Drm);
        Assert.False(ScenarioA1Searching.LeadershipApplied([Searcher(), Searcher("l1", mmc: false, leader: true, halfSquads: 0, leadership: 0)]));
    }

    [Fact]
    public void TheFinalDrLeavesThatManyAccessibleHexesUnsearched()
    {
        // A12.152: "The Final dr indicates the number of Accessible hexes other than its own of the ATTACKER's choice which the unit/stack may not Search";
        // ruling R70.24: the first of those he named, in his order of preference, are searched, never more than he named.
        Assert.Equal(4, ScenarioA1Searching.SearchedCount(5, 1, 5));
        Assert.Equal(2, ScenarioA1Searching.SearchedCount(5, 1, 2));
        Assert.Equal(0, ScenarioA1Searching.SearchedCount(5, 6, 5));
        Assert.Equal(3, ScenarioA1Searching.SearchedCount(5, -1, 3));
        Assert.Null(ScenarioA1Searching.VerifySearched("mph", true, true, true, 5, 1, 4));
        Assert.Equal("UNIT-STATE-051", ScenarioA1Searching.VerifySearched("mph", true, true, true, 5, 2, 4)?.Code);
        Assert.NotNull(ScenarioA1Searching.VerifySearched("mph", true, false, true, 5, 1, 1));
        Assert.NotNull(ScenarioA1Searching.VerifySearched("mph", true, true, false, 5, 1, 1));
        Assert.Null(ScenarioA1Searching.HexesBar(TwoHexes, _ => true));
        Assert.StartsWith("play.search-hexes: bd01:M4:0 is not an Accessible hex", ScenarioA1Searching.HexesBar(TwoHexes, hex => hex == "bd01:L4:0"), StringComparison.Ordinal);
        Assert.StartsWith("play.search-hexes: a Search names each hex", ScenarioA1Searching.HexesBar(SameHexTwice, _ => true), StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyGoodOrderInfantryOfTheMovingStackWithAMmcNotPinnedNorUsingAssaultMovementSearches()
    {
        // A12.152: "each Good Order Infantry/Cavalry MMC, or moving stack that contains >= one MMC, ends its move ... provided that all units making the
        // attempt are neither Pinned nor using Assault Movement"; ruling R70.24: once the DEFENDER's window on its last MF is closed.
        Assert.Null(ScenarioA1Searching.UnitBar(Searcher()));
        Assert.StartsWith("play.search-units: g1 is pinned", ScenarioA1Searching.UnitBar(Searcher(pinned: true)), StringComparison.Ordinal);
        Assert.StartsWith("play.search-units: g1 is not a Good Order", ScenarioA1Searching.UnitBar(Searcher(broken: true)), StringComparison.Ordinal);
        Assert.StartsWith("play.search-units: 'g1' is not a member", ScenarioA1Searching.UnitBar(Searcher(member: false)), StringComparison.Ordinal);
        Assert.StartsWith("play.search-units: g1 is not Infantry", ScenarioA1Searching.UnitBar(Searcher(infantry: false)), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1Searching.MmcBar([Searcher("l1", mmc: false, leader: true, halfSquads: 0, leadership: -1)]));
        Assert.Null(ScenarioA1Searching.MmcBar([Searcher(), Searcher("l1", mmc: false, leader: true, halfSquads: 0, leadership: -1)]));
        Assert.StartsWith("play.search-assault", ScenarioA1Searching.MoveBar(true, false, false, true), StringComparison.Ordinal);
        Assert.StartsWith("play.search-move: the DEFENDER's window", ScenarioA1Searching.MoveBar(true, false, true, false), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1Searching.MoveBar(false, false, false, false));
        Assert.NotNull(ScenarioA1Searching.MoveBar(true, true, false, false));
        Assert.Null(ScenarioA1Searching.MoveBar(true, false, false, false));
        Assert.NotNull(ScenarioA1Searching.PhaseBar("aph"));
    }

    [Fact]
    public void ASearchRevealsConcealedPlacesHiddenRemovesDummiesAndAnArmedGoodOrderDefenderDrawsTheCasualtyDr()
    {
        // A12.152: "All enemy concealed units revealed lose their '?' (or if hidden are placed on board with a '?')"; Searched hexes reveal their contents,
        // so Dummies go. A12.154: a Location Searched that holds an armed Good Order unit draws the DEFENDER's Casualty dr, with the drm of the concealed
        // ones: a concealed squad's -1 for its two HS-equivalents makes an Original 2 Reduce a searcher, a 3 not.
        Assert.Equal("revealed", ScenarioA1Searching.Reveal(false, true, false));
        Assert.Equal("placed", ScenarioA1Searching.Reveal(false, false, true));
        Assert.Equal("removed", ScenarioA1Searching.Reveal(true, true, false));
        Assert.Null(ScenarioA1Searching.Reveal(false, false, false));
        Assert.True(ScenarioA1Searching.CasualtyTrigger(false, false, false, false, false));
        Assert.False(ScenarioA1Searching.CasualtyTrigger(false, false, true, false, false));
        Assert.False(ScenarioA1Searching.CasualtyTrigger(true, false, false, false, false));
        Assert.False(ScenarioA1Searching.CasualtyTrigger(false, true, false, false, false));
        Assert.True(ScenarioA1Searching.CasualtyDue(false, true));
        Assert.False(ScenarioA1Searching.CasualtyDue(false, false));
        var (drm, _) = ScenarioA1MoppingUp.CasualtyDrm([new MopUpDefenderFacts(false, false, false, 2, null, false, false)]);
        Assert.True(ScenarioA1MoppingUp.CasualtyReduces(2, drm));
        Assert.False(ScenarioA1MoppingUp.CasualtyReduces(3, drm));
        Assert.Equal("searching", ScenarioA1MoraleCheck.Searching);
        Assert.Equal("g1 Searches from bd01:M5:0 and becomes TI: the Search dr 3 - 2 = 1; bd01:M5:0, bd01:L4:0 are searched: nothing is found (A12.152)",
            ScenarioA1Searching.RecordText(OneMover, "bd01:M5:0", 3, -2, OwnAndL4, 0, NoPlace, 0));
    }

    [Fact]
    public void MoppingUpIsBarredOnlyForANameThatIsNoBuildingAndABlazeIsNoPartOfOne()
    {
        // A12.153 (ruling R70.25): any multi-hex or multi-level building of the map, named by the card's id or by its board and lowest hex; a Blazing
        // building Location is not part of the building.
        Assert.Null(ScenarioA1MoppingUp.BuildingBar("bd03:M2", true));
        Assert.StartsWith("play.mop-up-building: 'bd03:X9' is not a building of the map", ScenarioA1MoppingUp.BuildingBar("bd03:X9", false), StringComparison.Ordinal);
        Assert.True(ScenarioA1MoppingUp.MopUpBuilding(3, false));
        Assert.False(ScenarioA1MoppingUp.MopUpBuilding(1, false));
        Assert.True(ScenarioA1MoppingUp.MopUpBuilding(1, true));
        Assert.False(ScenarioA1MoppingUp.PartOfBuilding(true));
        Assert.True(ScenarioA1MoppingUp.PartOfBuilding(false));
    }

    // Increment 3h (task 70.6): Withdrawal completed (A11.21; ruling R70.26), a Gun's crew in CC (A11.13, A1.123; ruling R70.27), and a Carrier HS
    // (A1.32, D6.82; ruling R70.28).
    private static WithdrawalLocationFacts There(string id, string side = "russian", bool concealed = false, bool hidden = false, bool dummy = false, bool captured = false,
        bool equipment = false) => new(id, side, captured, concealed, hidden, dummy, equipment);

    private static readonly string[] ConcealedPair = ["r1", "r2"];
    private static readonly string[] SecondOfPair = ["r2"];
    private static readonly string[] HiddenCrewAndGun = ["rc", "rg"];
    private static readonly string[] DummyOnly = ["rd"];
    private static readonly string[] FirstCrew = ["rc"];
    private static readonly string[] GunOnly = ["rg"];
    private static readonly string[] FirstOnly = ["r1"];
    private static readonly int[] ThreeFive = [3, 5];
    private static readonly int[] FourFour = [4, 4];
    private static readonly string[] Lmg = ["g-lmg"];
    private static readonly string[] LmgAndMmg = ["g-lmg", "g-mmg"];
    private static readonly string[] CrewsGun = ["r-gun"];

    [Fact]
    public void AWithdrawalIsBarredOnlyByAKnownEnemyAndMeetsWhatIsThere()
    {
        // A11.21 (p. 73): "The Location withdrawn to cannot be currently occupied by a Known enemy unit"; a "?" or a hidden unit does not bar it.
        Assert.True(ScenarioA1CloseCombatRules.WithdrawalDestination(true, true, false, false, false));
        Assert.False(ScenarioA1CloseCombatRules.WithdrawalDestination(true, true, false, false, true));

        // "If a unit withdraws into a concealed enemy's (not Dummy) Location it is eliminated automatically but an enemy unit(s) in that Location
        // (determined by Random Selection) must forfeit its concealment"; Dummies beside real units are not named and stay.
        var concealed = ScenarioA1Withdrawal.Entry([There("r1", concealed: true), There("r2", concealed: true), There("rd", concealed: true, dummy: true)], "german");
        Assert.Equal((true, 0, 0), (concealed.Eliminated, concealed.Placed.Count, concealed.DummiesRemoved.Count));
        Assert.Equal(ConcealedPair, concealed.Selection);

        // The p. 74 example: a HIP AT Gun and crew in G4 "are placed on board unconcealed (11.21) and the 2-4-7 is eliminated".
        var hidden = ScenarioA1Withdrawal.Entry([There("rc", hidden: true), There("rg", hidden: true, equipment: true)], "german");
        Assert.Equal((true, 0), (hidden.Eliminated, hidden.Selection.Count));
        Assert.Equal(HiddenCrewAndGun, hidden.Placed);

        // "(If one had been a dummy stack, it would be removed now.)": Dummies alone are removed and the Location entered.
        var dummies = ScenarioA1Withdrawal.Entry([There("rd", concealed: true, dummy: true)], "german");
        Assert.False(dummies.Eliminated);
        Assert.Equal(DummyOnly, dummies.DummiesRemoved);

        // A Gun under its crew's "?" is placed unconcealed with the reveal, never selected itself.
        var gunUnderQuestionMark = ScenarioA1Withdrawal.Entry([There("rc", concealed: true), There("rg", concealed: true, equipment: true)], "german");
        Assert.Equal(FirstCrew, gunUnderQuestionMark.Selection);
        Assert.Equal(GunOnly, gunUnderQuestionMark.Placed);

        // Friends, prisoners, and an empty Location meet nothing; a hidden Gun alone holds no enemy unit.
        Assert.Same(WithdrawalEntryVerdict.None, ScenarioA1Withdrawal.Entry([There("g9", "german", concealed: true), There("rp", captured: true, concealed: true)], "german"));
        Assert.False(ScenarioA1Withdrawal.Entry([There("rg", hidden: true, equipment: true)], "german").Eliminated);

        // A.9's Random Selection: the highest dr, ties all; one concealed unit needs no dr.
        Assert.Equal(SecondOfPair, ScenarioA1Withdrawal.Revealed(ConcealedPair, ThreeFive));
        Assert.Equal(ConcealedPair, ScenarioA1Withdrawal.Revealed(ConcealedPair, FourFour));
        Assert.Equal(FirstOnly, ScenarioA1Withdrawal.Revealed(FirstOnly, null));
        Assert.False(ScenarioA1Withdrawal.SelectionRolled(1));
        Assert.Throws<ArgumentException>(() => ScenarioA1Withdrawal.Revealed(ConcealedPair, null));
    }

    [Fact]
    public void AUnitAboveItsIpcWithdrawsByDroppingItsOwnSw()
    {
        // A11.21 (ruling R70.26): "may carry only <= its IPC"; the SW it drops stay unpossessed in the Location, and are its own.
        Assert.Null(ScenarioA1Withdrawal.ForeignDrop(Lmg, _ => true));
        Assert.Equal("g-mmg", ScenarioA1Withdrawal.ForeignDrop(LmgAndMmg, weapon => weapon == "g-lmg"));
        Assert.StartsWith("play.cc-withdrawal-drop: g-mmg is not a SW possessed by a unit that withdraws", ScenarioA1CloseCombatRules.DropBarText("g-mmg"), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CloseCombatRules.WithdrawalPortageBar("g1", false));
        Assert.Equal("play.cc-withdrawal-portage: g1 carries more than its IPC, so it withdraws only by dropping SW down to its IPC, which stay unpossessed in its Location (A11.21, A4.43)",
            ScenarioA1CloseCombatRules.WithdrawalPortageBar("g1", true));
        Assert.Equal("play.cc-withdrawal-drop: g-lmg is dropped, unpossessed in the Location, if its unit withdraws (A11.21, A4.43; ruling R70.26)", ScenarioA1Withdrawal.DropsText(Lmg));
    }

    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;

    private static CloseCombatUnit CcUnit(string id, string definition, string side) => new(id, definition, side, false, false, false, false, false, false, false, false, false, false, false);

    private static CloseCombatFacts CcRound(CloseCombatUnit[] units, CloseCombatDeclaration[] attacks, int[] dice, IReadOnlyDictionary<string, int>? weaponLoss = null) =>
        new("CCPh", "bd03:D10:0", "open-ground", "german", CloseCombatFacts.Simultaneous, null, units, [], [], attacks,
            new CloseCombatRolls(new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["0"] = dice }) { WeaponLoss = weaponLoss });

    private static readonly int[] ColoredOne = [1, 2];
    private static readonly int[] ColoredTwo = [2, 1];

    [Fact]
    public void AGunsCrewFightsInCcWithItsInherentFpAndItsGunIsLostOnAColoredOneAndTheKillNumber()
    {
        // A1.123 (p. 44) and A11.13 (p. 72; ruling R70.27): the crew that mans a Gun is a CC unit with its inherent FP, the Gun never used in CC; "Each
        // Gun/SW counter possessed by a unit eliminated in CC may also be eliminated if the Original colored dr of that CC DR is a 1 ... if this dr is <=
        // the black Kill Number".
        // A German HS (2 FP) attacks the Russian crew (2 FP, its inherent FP) at 1-1, Kill Number 5: an Original 3 eliminates it.
        CloseCombatUnit[] units = [CcUnit("g1", "attacker-half-squad", "german"), CcUnit("rc", "defender-crew", "russian") with { Manning = true, Guns = CrewsGun }];
        CloseCombatDeclaration[] attacks = [new(["g1"], ["rc"])];
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(CcRound(units, attacks, ColoredOne), CloseCombat));
        Assert.Equal(["asl.a1.cc.roll-missing:weaponLoss:r-gun"], ScenarioA1CloseCombatCalculator.Resolve(CcRound(units, attacks, ColoredOne), CloseCombat).Reasons);
        var lost = ScenarioA1CloseCombatCalculator.Resolve(CcRound(units, attacks, ColoredOne, new Dictionary<string, int> { ["r-gun"] = 5 }), CloseCombat);
        Assert.Equal(("1-1", 5, 2m), (lost.Attacks[0].Odds, lost.Attacks[0].KillNumber, lost.Attacks[0].DefenseFirepower));
        Assert.Equal(("r-gun", "rc", true), (Assert.Single(lost.WeaponEffects).EquipmentId, lost.WeaponEffects[0].HolderId, lost.WeaponEffects[0].Eliminated));
        var kept = ScenarioA1CloseCombatCalculator.Resolve(CcRound(units, attacks, ColoredOne, new Dictionary<string, int> { ["r-gun"] = 6 }), CloseCombat);
        Assert.False(Assert.Single(kept.WeaponEffects).Eliminated);

        // A colored dr other than 1 rolls nothing for the Gun; a record made before the pass lists no Guns and rolls none either.
        Assert.Empty(ScenarioA1CloseCombatCalculator.Resolve(CcRound(units, attacks, ColoredTwo), CloseCombat).WeaponEffects);
        Assert.Empty(ScenarioA1CloseCombatCalculator.Resolve(CcRound([units[0], units[1] with { Guns = null }], attacks, ColoredOne), CloseCombat).WeaponEffects);
    }

    [Fact]
    public void ACarrierHalfSquadRecombinesWithNoLeader()
    {
        // A1.32 (p. 45): "Finns, Guards, Carrier HS and unarmed HS do not need leader presence to Recombine"; D6.82 (p. 206): Carrier HS "need not pass a
        // TC nor have a leader present". A Carrier HS is a HS whose creation recorded its Carrier (ruling R70.28).
        Assert.True(ScenarioA1CarrierRules.CarrierHalfSquad(true, "gb-carrier"));
        Assert.False(ScenarioA1CarrierRules.CarrierHalfSquad(true, null));
        Assert.False(ScenarioA1CarrierRules.CarrierHalfSquad(false, "gb-carrier"));
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, false, true));
        Assert.False(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, false));
        Assert.True(ScenarioA1CarrierRules.RecordsCarrier("asl:half-squad"));
        Assert.False(ScenarioA1CarrierRules.RecordsCarrier("asl:vehicle-crew"));
    }

    // Increment 3i (task 70.7, part I): Infantry against Infantry in a Location with a vehicle (A11.31; ruling R70.30), its Ambush (A11.4, A11.34, A11.51),
    // and PRC in CC (A11.611, A11.62, A11.71; ruling R70.31).
    private static CloseCombatFacts Sequential(CloseCombatUnit[] units, CloseCombatDeclaration[] attacks, string? ambusher = null) =>
        new("CCPh", "bd03:F8:0", "stone-building", "russian", CloseCombatFacts.Sequential, ambusher, units, [], [], attacks,
            new CloseCombatRolls(new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal) { ["0"] = Sevens }));

    private static readonly int[] Sevens = [3, 4];

    private static readonly CloseCombatUnit[] SquadAgainstSquad = [CcUnit("ru-1", "defender-squad", "russian"), CcUnit("de-1", "attacker-squad", "german")];

    [Fact]
    public void ASequentialRoundIsOneAttackWithNoReplyAndKeepsTheAmbushDrm()
    {
        // A11.31 (p. 73): in a Location holding a vehicle "each side alternates taking its CC attacks one at a time"; the H3 example (p. 74): "the 6-2-8 attacks
        // the German Infantry at 1:1 ... Subsequent attack opportunities alternate". One attack is the round; two are outside it.
        CloseCombatDeclaration[] one = [new(["ru-1"], ["de-1"])];
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(Sequential(SquadAgainstSquad, one), CloseCombat));
        var resolved = ScenarioA1CloseCombatCalculator.Resolve(Sequential(SquadAgainstSquad, one), CloseCombat);
        Assert.Equal(("russian", "1-1"), (Assert.Single(resolved.Attacks).Side, resolved.Attacks[0].Odds));
        Assert.Contains("asl.a1.cc.round-outside", ScenarioA1CloseCombatCalculator.Precheck(Sequential(SquadAgainstSquad, [.. one, new(["de-1"], ["ru-1"])]), CloseCombat));

        // A berserk unit's attack is one of the sequence, not owed in this attack (A15.43 holds it to its own turn).
        Assert.Empty(ScenarioA1CloseCombatCalculator.Precheck(Sequential([SquadAgainstSquad[0], SquadAgainstSquad[1] with { Berserk = true }], one), CloseCombat));

        // A11.34 and its example: "Should an Ambush occur in a CC Location containing a vehicle, the ambushing side gets to resolve all of its CC attacks in
        // that Location first"; A11.4: -1 to the ambusher's attacks and +1 to attacks against it.
        Assert.Contains(ScenarioA1CloseCombatCalculator.Resolve(Sequential(SquadAgainstSquad, one, "russian"), CloseCombat).Attacks[0].Drm, item => item.Name == "ambush" && item.Value == -1m);
        Assert.Contains(ScenarioA1CloseCombatCalculator.Resolve(Sequential(SquadAgainstSquad, [new(["de-1"], ["ru-1"])], "russian"), CloseCombat).Attacks[0].Drm,
            item => item.Name == "vs-ambush" && item.Value == 1m);
    }

    [Fact]
    public void ARiderAttacksWithPlusOneAndIsAttackedWithMinusOne()
    {
        // A11.611 (p. 75): "Riders can be attacked separately as an Infantry-vs-Infantry CC attack. Riders are susceptible to a +1 CC DRM when attacking and a
        // -1 CC DRM when defending" (ruling R70.31).
        CloseCombatUnit[] units = [SquadAgainstSquad[0], SquadAgainstSquad[1] with { Rider = true }];
        var against = ScenarioA1CloseCombatCalculator.Resolve(Sequential(units, [new(["ru-1"], ["de-1"])]), CloseCombat);
        Assert.Equal([("vs-rider", -1m)], Assert.Single(against.Attacks[0].Defending).Drm.Select(item => (item.Name, item.Value)));
        Assert.DoesNotContain(against.Attacks[0].Drm, item => item.Name.StartsWith("rider", StringComparison.Ordinal));
        var by = ScenarioA1CloseCombatCalculator.Resolve(Sequential(units, [new(["de-1"], ["ru-1"])]), CloseCombat);
        Assert.Contains(by.Attacks[0].Drm, item => item.Name == "rider:de-1" && item.Value == 1m);

        // A record made before the pass marks no Rider: nothing changes.
        Assert.Empty(ScenarioA1CloseCombatCalculator.Resolve(Sequential(SquadAgainstSquad, [new(["ru-1"], ["de-1"])]), CloseCombat).Attacks[0].Defending[0].Drm);
    }

    [Fact]
    public void TheAmbushDrTakesTheVehicleDrmOfEachSide()
    {
        // A11.4's table (p. 73): "+2 Cavalry, Vehicle, or in Pillbox"; "+1 BU or Stunned AFV (each)". The H3 example (p. 74): "the German Ambush dr receives
        // +2 drm (+2 vehicle, +1 BU, -1 Stealthy [Hero])". Here the German side has a BU AFV: +3; the Russian none. A Russian 4 against a German 3 (6
        // Final): no Ambush.
        CloseCombatUnit[] units = [CcUnit("ru-1", "defender-squad", "russian") with { Advanced = true }, CcUnit("de-1", "attacker-squad", "german")];
        var facts = new AmbushFacts("CCPh", "bd03:F8:0", "stone-building", "russian", units, new Dictionary<string, int>(StringComparer.Ordinal) { ["russian"] = 4, ["german"] = 3 })
        {
            Vehicles = [new AmbushVehicleFacts("german", 1)],
        };
        var resolution = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, CloseCombat);
        var german = resolution.Sides.Single(side => side.Side == "german");
        Assert.Equal([("vehicle", 2m), ("bu-or-stunned-afv", 1m)], german.Drm.Select(item => (item.Name, item.Value)));
        Assert.Equal((6, 4, (string?)null), (german.FinalDr, resolution.Sides.Single(side => side.Side == "russian").FinalDr, resolution.Ambusher));

        // A dr of 1 against the German 4 (7 Final) is at least three lower: the Russians ambush. Where no vehicle stands, no vehicle drm (null facts).
        Assert.Equal("russian", ScenarioA1CloseCombatCalculator.ResolveAmbush(facts with
        {
            Rolls = new Dictionary<string, int>(StringComparer.Ordinal) { ["russian"] = 1, ["german"] = 4 }
        }, CloseCombat).Ambusher);
        Assert.Empty(ScenarioA1CloseCombatCalculator.ResolveAmbush(facts with
        {
            Vehicles = null
        }, CloseCombat).Sides.Single(side => side.Side == "german").Drm);

        // The builder counts each side's AFV that is BU or Stunned; an unarmored vehicle, or a CE AFV, adds only the +2.
        var built = ScenarioA1CloseCombatFactBuilders.AmbushVehicles([("german", true, false, false), ("german", false, false, false), ("russian", true, true, false)])!;
        Assert.Equal([("german", 1), ("russian", 0)], built.Select(item => (item.Side, item.ButtonedOrStunnedAfvs)));
        Assert.Null(ScenarioA1CloseCombatFactBuilders.AmbushVehicles([]));
        Assert.True(ScenarioA1CloseCombatRules.AmbushDueCandidate("ccph", false));
        Assert.False(ScenarioA1CloseCombatRules.AmbushDueCandidate("ccph", true));
    }

    private static VehicleCloseCombatVehicle CcTank() => new("de-v", "attacker-tank", "german", false, false, false, false, false, false, false, false, false);

    [Fact]
    public void TheAmbushDrmAppliesBothWaysInCcWithAVehicle()
    {
        // A11.51 (p. 74): "An Ambush DRM (11.4/11.8) ... can also apply to CC vs a vehicle"; the I5 example (p. 75): the T34/85 "attacks with its CMG ... with
        // +1 DRM for Street Fighting Ambush". Ruling R70.30: the ambusher's attacks -1, attacks against it +1, either way.
        CloseCombatUnit[] units = [CcUnit("ru-1", "defender-squad", "russian")];
        VehicleCloseCombatFacts On(string? ambusher) => new("CCPh", "bd03:F8:0", CcTank(), units, ["ru-1"], [], false, false, new VehicleCloseCombatRolls(Sevens))
        {
            Ambusher = ambusher
        };
        Assert.Contains(ScenarioA1VehicleCloseCombat.Resolve(On("russian"), CloseCombat).Drm, item => item.Name == "ambush" && item.Value == -1m);
        Assert.Contains(ScenarioA1VehicleCloseCombat.Resolve(On("german"), CloseCombat).Drm, item => item.Name == "vs-ambush" && item.Value == 1m);
        Assert.DoesNotContain(ScenarioA1VehicleCloseCombat.Resolve(On(null), CloseCombat).Drm, item => item.Name.Contains("ambush", StringComparison.Ordinal));
        VehicleCloseCombatFacts By(string? ambusher) => new("CCPh", "bd03:F8:0", CcTank(), units, [], ["ru-1"], true, false, new VehicleCloseCombatRolls(Sevens))
        {
            Ambusher = ambusher
        };
        Assert.Contains(Assert.Single(ScenarioA1VehicleCloseCombat.Resolve(By("german"), CloseCombat).Defending!).Drm, item => item.Name == "ambush" && item.Value == -1m);
        Assert.Contains(Assert.Single(ScenarioA1VehicleCloseCombat.Resolve(By("russian"), CloseCombat).Defending!).Drm, item => item.Name == "vs-ambush" && item.Value == 1m);
    }

    [Fact]
    public void ACeHalftracksPassengersJoinItsAttackAndOnlyAnImmobileVehiclesPrcAreHeldInMelee()
    {
        // A11.62 (p. 76): "The only other ways an AFV may attack in CC are with a RMG factor, Riders, CE Passengers in a halftrack"; the BU halftrack of
        // scenario A1 has no CC armament of its own, so its Passenger squad's 4 FP is the attack (ruling R70.31), halved for a pinned Passenger.
        CloseCombatUnit[] units = [CcUnit("ru-1", "defender-squad", "russian")];
        var halftrack = new VehicleCloseCombatVehicle("de-ht", "attacker-halftrack", "german", true, false, false, false, false, false, false, false, false);
        VehicleCloseCombatFacts Facts(IReadOnlyList<CloseCombatUnit>? passengers) =>
            new("CCPh", "bd03:F8:0", halftrack, units, [], ["ru-1"], true, false, new VehicleCloseCombatRolls(Sevens))
            {
                Passengers = passengers
            };
        Assert.Contains("asl.a1.cc-vehicle.no-cc-armament", ScenarioA1VehicleCloseCombat.Precheck(Facts(null) with
        {
            Vehicle = halftrack with
            {
                CrewExposed = false
            }
        }, CloseCombat));
        var joined = ScenarioA1VehicleCloseCombat.Resolve(Facts([CcUnit("de-p", "attacker-squad", "german")]), CloseCombat);
        Assert.Equal(CloseCombatResolution.Resolved, joined.Disposition);
        var withPassenger = joined.AttackFirepower!.Value - ScenarioA1VehicleCloseCombat.Resolve(Facts(null), CloseCombat).AttackFirepower!.Value;
        Assert.Equal(4m, withPassenger);
        Assert.Equal(2m, ScenarioA1VehicleCloseCombat.Resolve(Facts([CcUnit("de-p", "attacker-squad", "german") with { Pinned = true }]), CloseCombat).AttackFirepower!.Value
            - ScenarioA1VehicleCloseCombat.Resolve(Facts(null), CloseCombat).AttackFirepower!.Value);

        // A11.611, A11.62: a Rider attacks apart; a CE halftrack's Passenger joins; a BU one does nothing; an unarmored vehicle's always join.
        Assert.Equal(ScenarioA1PassengerActionRules.Apart, ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Rider, true, false, true).Part);
        Assert.Equal(ScenarioA1PassengerActionRules.Attacks, ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Passenger, true, true, true).Part);
        Assert.Equal(ScenarioA1PassengerActionRules.Idle, ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Passenger, true, false, true).Part);
        Assert.Equal(ScenarioA1PassengerActionRules.Attacks, ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Passenger, false, false, true).Part);

        // A11.71 (p. 76): "Passengers/Riders of Immobile vehicles are held in Melee, but ... Passengers/Riders of Mobile vehicles who survive their initial round
        // of CC are not required to remain in Melee". A unit aboard is read for Melee only when the CCPh's record names it; one before the pass names none.
        Assert.True(ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Rider, true, false, false).HeldInMelee);
        Assert.False(ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Rider, true, false, true).HeldInMelee);
        Assert.True(ScenarioA1CloseCombatProjection.PrcMeleeCandidate(true, true, true));
        Assert.False(ScenarioA1CloseCombatProjection.PrcMeleeCandidate(true, true, false));
        Assert.True(ScenarioA1CloseCombatProjection.PrcMeleeCandidate(true, false, false));
        Assert.True(ScenarioA1CloseCombatProjection.PrcMeleeCandidate(false, true, false));
    }

    [Fact]
    public void TheSidesWithAttacksCountInfantryAgainstInfantryAndTheAmbusherAttacksFirst()
    {
        // A11.12's EXC (p. 72): units attack or are attacked once per CCPh "[EXC: ... CC vs/by a vehicle]": being attacked by Infantry and by a vehicle are
        // counted apart (ruling R70.30). The German squad beside its own tank faces the Russian squad, which no Infantry has attacked: the Germans have an
        // attack; once the Russian squad is attacked by Infantry, the German squad has none (the tank attacked already).
        VehicleCcUnitFacts[] here =
        [
            new("de-v", "german", true, true, false, false, false, false, true), new("de-1", "german", false, false, true, true, false, false, true),
            new("ru-1", "russian", false, false, true, true, false, false, true),
        ];
        Assert.Equal(["russian"], ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(here, ["de-v", "de-1"], []));
        Assert.Equal(["german", "russian"], ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(here, ["de-v"], [], []).Order(StringComparer.Ordinal));
        Assert.Equal(["russian"], ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(here, ["de-v"], [], ["ru-1"]));

        // A Rider attacks and is attacked in Infantry against Infantry CC only: never the vehicle, nor by the vehicle (A11.611).
        VehicleCcUnitFacts[] rider = [here[0], new VehicleCcUnitFacts("ru-r", "russian", false, false, true, true, false, false, true) { Rider = true }];
        Assert.Empty(ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(rider, [], [], []));

        // A11.34: after an Ambush the ambusher attacks first and keeps the attack while it has attacks left.
        Assert.Equal("german", ScenarioA1VehicleCloseCombatRules.FirstCcSide([("german", false), ("russian", true)], "russian", "german"));
        Assert.Equal(("german", false), ScenarioA1VehicleCloseCombatRules.NextCcSide(["german", "russian"], "german", "german"));
        Assert.Equal(("russian", false), ScenarioA1VehicleCloseCombatRules.NextCcSide(["russian"], "german", "german"));

        // UNIT-STATE-030: the round is the side's named next, and no unit is attacked twice by Infantry.
        Assert.Null(ScenarioA1CloseCombatProjection.SequentialRoundRefusal("ccph", false, "russian", "russian", false, false, false));
        Assert.NotNull(ScenarioA1CloseCombatProjection.SequentialRoundRefusal("ccph", false, "german", "russian", false, false, false));
        Assert.NotNull(ScenarioA1CloseCombatProjection.SequentialRoundRefusal("ccph", false, "russian", "russian", false, true, false));
    }

    // Increment 3j (task 70.7, part II): Street Fighting (A11.8; ruling R70.33), A11.52's Passengers, A18.12 against a vehicle, and D5.42's entry at a capture
    // (ruling R70.34).
    private static readonly string[] F3Side = ["F3"];
    private static readonly string[] G3Side = ["G3"];
    private static readonly string[] NoBuilding = [];
    private static readonly string[] RussianSquad = ["ru-1"];
    private static readonly string[] SquadAndLeader = ["ru-1", "ru-l"];

    [Fact]
    public void ARoadHexFlankedByBuildingsOnBothSidesQualifiesTheInfantryThatAdvancedFromTheirGroundLevel()
    {
        // A11.8 (p. 76): "Any vehicle in a road hex and ADJACENT to a building hex on both sides of that road ... All Infantry advancing into that road
        // Location from ground level of one or more of those adjacent building hexes qualify". A side of the road with no building does not flank it.
        IReadOnlyList<string>[] both = [F3Side, G3Side];
        IReadOnlyList<string>[] one = [F3Side, NoBuilding];
        Assert.True(ScenarioA1StreetFighting.Flanked(both));
        Assert.False(ScenarioA1StreetFighting.Flanked(one));
        Assert.True(ScenarioA1StreetFighting.Qualifies(both, true, true, "F3", 0));
        Assert.False(ScenarioA1StreetFighting.Qualifies(both, true, true, "F3", 1));
        Assert.False(ScenarioA1StreetFighting.Qualifies(both, true, true, "E3", 0));
        Assert.False(ScenarioA1StreetFighting.Qualifies(both, true, false, "F3", 0));
        Assert.False(ScenarioA1StreetFighting.Qualifies(one, true, true, "F3", 0));
        Assert.False(ScenarioA1StreetFighting.Qualifies(both, false, true, "F3", 0));
    }

    [Fact]
    public void StreetFightingGivesMinusOneAgainstTheVehicleAndPlusOneToItsAttackButNotWithAnActualAmbush()
    {
        // A11.8: "a Streetfighting Ambush -1/+1 DRM in CC vs the vehicle (not cumulative with the normal -1/+1 CC DRM for Ambush)"; the I5 example (p. 75):
        // "DRM are -2 (-1 Street Fighting Ambush, -1 CE)" and the T34/85 attacks "with +1 DRM for Street Fighting Ambush".
        CloseCombatUnit[] units = [CcUnit("ru-1", "defender-squad", "russian")];
        VehicleCloseCombatFacts On(string? ambusher) => new("CCPh", "bd03:F4:0", CcTank(), units, RussianSquad, [], false, false, new VehicleCloseCombatRolls(Sevens))
        {
            Ambusher = ambusher,
            StreetFighters = RussianSquad,
        };
        Assert.Contains(ScenarioA1VehicleCloseCombat.Resolve(On(null), CloseCombat).Drm, item => item.Name == "street-fighting" && item.Value == -1m && item.Rule == "A11.8");
        var ambushed = ScenarioA1VehicleCloseCombat.Resolve(On("russian"), CloseCombat).Drm;
        Assert.DoesNotContain(ambushed, item => item.Name == "street-fighting");
        Assert.Contains(ambushed, item => item.Name == "ambush");
        Assert.DoesNotContain(ScenarioA1VehicleCloseCombat.Resolve(On(null) with
        {
            StreetFighters = null
        }, CloseCombat).Drm, item => item.Name == "street-fighting");
        VehicleCloseCombatFacts By(IReadOnlyList<string>? fighters) => new("CCPh", "bd03:F4:0", CcTank(), units, [], RussianSquad, true, false, new VehicleCloseCombatRolls(Sevens))
        {
            StreetFighters = fighters,
        };
        Assert.Contains(Assert.Single(ScenarioA1VehicleCloseCombat.Resolve(By(RussianSquad), CloseCombat).Defending!).Drm, item => item.Name == "vs-street-fighting" && item.Value == 1m);
        Assert.Empty(Assert.Single(ScenarioA1VehicleCloseCombat.Resolve(By(null), CloseCombat).Defending!).Drm);
    }

    [Fact]
    public void AStreetFighterReturnsOnceTheCcIsOverUnlessBrokenOrPinned()
    {
        // A11.8: "following any CC attack returned to the same Locations they came from ... or at their option remain in the road Location. If broken or
        // pinned ... they would not be returned"; the I5 example: "Since the 2-3-8 is pinned, it cannot return to its building Location".
        Assert.Null(ScenarioA1StreetFighting.ReturnBar("ru-1", "ccph", true, true, true, false, false, false));
        Assert.Contains("pinned", ScenarioA1StreetFighting.ReturnBar("ru-1", "ccph", true, true, true, false, true, false), StringComparison.Ordinal);
        Assert.Contains("broken", ScenarioA1StreetFighting.ReturnBar("ru-1", "ccph", true, true, true, true, false, false), StringComparison.Ordinal);
        Assert.Contains("is not over", ScenarioA1StreetFighting.ReturnBar("ru-1", "ccph", true, true, false, false, false, false), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1StreetFighting.ReturnBar("ru-1", "ccph", false, true, true, false, false, false));
        Assert.NotNull(ScenarioA1StreetFighting.ReturnBar("ru-1", "mph", true, true, true, false, false, false));
    }

    [Fact]
    public void PassengersAreAttackedOnlyToCaptureThemAtMinusTwoAndTheirCaptorsDefendAtPlusTwo()
    {
        // A11.52 (p. 74): "The attacker must engage these Passengers in normal Infantry vs Infantry CC, but is eligible for both a -2 DRM for his attacks vs these
        // Passengers and a +2 DRM in defending against attacks from those Passengers. This -2 DRM is combined with the +1 DRM for attempting to capture".
        CloseCombatUnit[] units = [CcUnit("ru-1", "defender-squad", "russian"), CcUnit("de-p", "attacker-squad", "german") with { PassengerOf = "de-t" }];
        var capture = new CloseCombatDeclaration(RussianSquad, ["de-p"]) { Capture = true };
        var resolved = ScenarioA1CloseCombatCalculator.Resolve(Sequential(units, [capture]), CloseCombat);
        Assert.Equal(CloseCombatResolution.Resolved, resolved.Disposition);
        Assert.Equal([("capture", 1m), ("vs-passenger-capture", -2m)], Assert.Single(resolved.Attacks[0].Defending).Drm.Select(item => (item.Name, item.Value)));
        Assert.Contains("asl.a1.cc.passenger-outside:0", ScenarioA1CloseCombatCalculator.Precheck(Sequential(units, [new(RussianSquad, ["de-p"])]), CloseCombat));
        Assert.Contains("asl.a1.cc.passenger-outside:0", ScenarioA1CloseCombatCalculator.Precheck(Sequential(units, [new(["de-p"], RussianSquad)]), CloseCombat));

        // The truck has no CC armament; its Passenger's attack is its attack (A11.62), at +2 against a captor.
        var truck = new VehicleCloseCombatVehicle("de-t", "attacker-truck", "german", false, false, false, false, false, false, false, false, false);
        VehicleCloseCombatFacts ByTruck(IReadOnlyList<string>? captors) =>
            new("CCPh", "bd03:F4:0", truck, [units[0]], [], RussianSquad, true, false, new VehicleCloseCombatRolls(Sevens))
            {
                Passengers = [units[1] with { PassengerOf = null }],
                Captors = captors
            };
        Assert.Contains(Assert.Single(ScenarioA1VehicleCloseCombat.Resolve(ByTruck(RussianSquad), CloseCombat).Defending!).Drm, item => item.Name == "vs-passenger-captor" && item.Value == 2m);
        Assert.Empty(Assert.Single(ScenarioA1VehicleCloseCombat.Resolve(ByTruck(null), CloseCombat).Defending!).Drm);

        // "If all the Passengers are captured, so is the vehicle", once no enemy Personnel is left; an unarmored stationary vehicle's only.
        Assert.True(ScenarioA1CapturedVehicleRules.CapturedWithPassengers(1, false, 0, false));
        Assert.False(ScenarioA1CapturedVehicleRules.CapturedWithPassengers(1, false, 1, false));
        Assert.False(ScenarioA1CapturedVehicleRules.CapturedWithPassengers(1, true, 0, false));
        Assert.False(ScenarioA1CapturedVehicleRules.CapturedWithPassengers(1, false, 0, true));
        Assert.False(ScenarioA1CapturedVehicleRules.CapturedWithPassengers(0, false, 0, false));
        Assert.True(ScenarioA1CapturedVehicleRules.PassengersCapturable(true, false));
        Assert.False(ScenarioA1CapturedVehicleRules.PassengersCapturable(true, true));
        Assert.False(ScenarioA1CapturedVehicleRules.PassengersCapturable(false, false));
        Assert.True(ScenarioA1VehicleProjection.CaptureStands(true, true, true, true, true, ScenarioA1CapturedVehicleRules.Passengers, false));
    }

    [Fact]
    public void AMmcsOriginalTwoAgainstAVehicleCreatesALeaderWhoAddsHisLeadershipAndACcvOnlyWithoutASmc()
    {
        // A18.12 (p. 85): "Anytime a MMC attacking in CC rolls an Original 2 DR, the player makes an immediate dr on the Leader Creation Table. If this results
        // in the creation of a leader, that leader must add his leadership to the Original 2 CC DR that created him (even if the modifier is a +1)". A Russian
        // 4-4-7 (Morale 7) and a dr of 5, +1 Russian: Final 6, a 6+1. As a SMC combining he adds one to the CCV (A11.5): Kill Number 6, Final DR 2 + 1 = 3.
        CloseCombatUnit[] units = [CcUnit("ru-1", "defender-squad", "russian"), CcUnit("ru-l", "defender-leader-7-0", "russian")];
        VehicleCloseCombatFacts Attack(string[] attackers, int? creation, bool? planned = true) =>
            new("CCPh", "bd03:F4:0", CcTank(), units, attackers, [], false, false, new VehicleCloseCombatRolls(SnakeEyes) { LeaderCreation = creation })
            {
                LeaderCreation = planned
            };
        Assert.Equal(["asl.a1.cc-vehicle.roll-missing:leaderCreation"], ScenarioA1VehicleCloseCombat.Resolve(Attack(RussianSquad, null), CloseCombat).Reasons);
        var created = ScenarioA1VehicleCloseCombat.Resolve(Attack(RussianSquad, 5), CloseCombat);
        Assert.Equal(("defender-leader-6-plus-1", 6), (created.LeaderCreation!.LeaderDefinitionId, created.LeaderCreation.FinalDr));
        Assert.Equal((6, 3), (created.KillNumber!.Value, created.FinalDr));
        Assert.Contains(created.KillNumberModifiers, item => item.Name.StartsWith("created-leader-combining:", StringComparison.Ordinal) && item.Rule == "A18.12");
        Assert.Contains(created.Drm, item => item.Name == "created-leader:defender-leader-6-plus-1" && item.Value == 1m);

        // With a SMC in the attack ("No more than two units") the created leader adds his leadership only; the Unlikely Kill still follows.
        var withLeader = ScenarioA1VehicleCloseCombat.Resolve(Attack(SquadAndLeader, 5), CloseCombat);
        Assert.DoesNotContain(withLeader.KillNumberModifiers, item => item.Name.StartsWith("created-leader", StringComparison.Ordinal));
        Assert.Contains(withLeader.Drm, item => item.Name.StartsWith("created-leader:", StringComparison.Ordinal));

        // A record made before the pass declares no Leader Creation: its Original 2 rolls none.
        var older = ScenarioA1VehicleCloseCombat.Resolve(Attack(RussianSquad, null, null), CloseCombat);
        Assert.Equal((CloseCombatResolution.Resolved, (LeaderCreationOutcome?)null), (older.Disposition, older.LeaderCreation));
    }

    [Fact]
    public void ACaptorEntersTheVehicleItCapturedInThatCcphWhateverMfItSpent()
    {
        // D5.42 (p. 204): "only if it occupies that vehicle's Location at the start of its MPh (not APh) and expends all of its MF to do so [EXC: CCPh; A21.2]
        // ... A vehicle may not be entered during the same Player Turn in which its previous crew left [EXC: CCPh; A21.2]."
        var inCcph = new RecrewFacts("ru-1", "de-tank", false, true, true, true, true, true, true, true, false, false);
        Assert.Contains("only in its own MPh", ScenarioA1CrewActsRules.RecrewBar(inCcph), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CrewActsRules.RecrewBar(inCcph with
        {
            CapturedThisCcph = true
        }));
        Assert.Contains("Good Order", ScenarioA1CrewActsRules.RecrewBar(inCcph with
        {
            CapturedThisCcph = true,
            GoodOrder = false
        }), StringComparison.Ordinal);
        Assert.Contains("only from its Location", ScenarioA1CrewActsRules.RecrewBar(inCcph with
        {
            CapturedThisCcph = true,
            InLocation = false
        }), StringComparison.Ordinal);
    }

    // The fixes of the reviews, round A (hidden information).
    private static readonly (string Id, bool ConcealedToEnemy, bool Aboard)[] RiderOnTop = [("de-r", false, true), ("de-tank", false, false), ("de-s", false, false)];

    [Fact]
    public void TheViewersPendingOptionIsRefusedToTheMoverAsTheOpenWindowIs()
    {
        // A12.14 (ruling R70.21; the referee's finding): another side's action meets the open window's refusal, code and words; the viewing side, and a
        // proposal of no side, read R5.8's; a choice of another kind is unchanged.
        const string Bar = "play.choice-pending: the german side answers first: the moving stack waits for the DEFENDER's answer at its step (A8.1)";
        Assert.Equal("play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)",
            ScenarioA1ConcealmentChoices.PendingLossBar(Bar, true, "german", "russian"));
        Assert.Equal(Bar, ScenarioA1ConcealmentChoices.PendingLossBar(Bar, true, "german", "german"));
        Assert.Equal(Bar, ScenarioA1ConcealmentChoices.PendingLossBar(Bar, true, "german", null));
        Assert.Equal(Bar, ScenarioA1ConcealmentChoices.PendingLossBar(Bar, false, "german", "russian"));
        Assert.Null(ScenarioA1ConcealmentChoices.PendingLossBar(null, true, "german", "russian"));
    }

    [Fact]
    public void AStacksTopIsNeverAUnitAboardAndOnlyAnImmobileVehiclesPrcAreHeld()
    {
        // Rulings R23.3, R70.18 (the referee's finding): a Rider listed first is not the stack's top; its vehicle is. A11.71 (p. 76): the PRC of an Immobile
        // vehicle are held in Melee, a Mobile one's not.
        Assert.Equal("de-tank", ScenarioA1Visibility.TopCounter(RiderOnTop));
        Assert.True(ScenarioA1PassengerActionRules.HeldInMelee(false));
        Assert.False(ScenarioA1PassengerActionRules.HeldInMelee(true));
        Assert.Equal(ScenarioA1PassengerActionRules.HeldInMelee(false), ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Rider, true, false, false).HeldInMelee);
    }

    [Fact]
    public void TheMoppingUpProposalSaysOnlyWhatTheDeclarerKnows()
    {
        // A12.153, A12.154 (the referee's finding): the proposal names no result: what the Mopping Up finds is said when it is made.
        var text = ScenarioA1MoppingUp.ProposalText(["g1"], "bd01:E5", "german", "g1");
        Assert.StartsWith("play.mop-up: g1 Mops Up building bd01:E5 and becomes TI (A12.153; ruling R24.2)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("no concealed enemy unit is left", text, StringComparison.Ordinal);
        Assert.DoesNotContain("HS-equivalents", text, StringComparison.Ordinal);
    }
}
