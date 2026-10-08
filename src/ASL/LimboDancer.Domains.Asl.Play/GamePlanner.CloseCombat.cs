using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Advance in the APh, Close Combat in the CCPh, and the capture of a surrendering unit (unit steps 29 and 30), with the map
/// reads Heat of Battle and the berserk charge need: a Known enemy unit in LOS (A15.44), the ADJACENT captors (A15.5), and
/// the shortest route of a charge (A15.43).
/// </summary>
public sealed partial class GamePlanner
{
    private static readonly Lazy<ScenarioA1CloseCombatReference> CloseCombatReference = new(() => new ScenarioA1CloseCombatPackage().Reference);

    /// <summary>
    /// An advance (A3.7, A4.7): Infantry of the phasing side in one Location enter one ADJACENT Location at the same level, even
    /// one holding Known enemy units, in reviewed terrain (ruling R29.2).
    /// </summary>
    private GamePlan PlanAdvanceUnits(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        // A2.6 (ruling R25.5): an advance off the map.
        if (Text(arguments, "exit", out var exitEdge) && !arguments.TryGetProperty("to", out _))
        {
            return PlanExit(scope, arguments, existing, attemptId, expected, label, exitEdge, "unknown");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || !Text(arguments, "to", out var toText)
            || !BoardLocation.TryParse(toText, out var to))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an advance names its units and the Location they enter");
        }

        if (ScenarioA1AdvanceCalculator.AdvancePhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        // A2.1 (ruling R20.6): never out of the card's playable area.
        if (PlayableBar(state, to) is { } outside)
        {
            return Refused(scope, label, expected, outside);
        }

        var units = ids.Select(state.Unit).ToArray();
        if (units.Any(unit => !ScenarioA1AdvanceCalculator.AdvanceUnitAllowed(unit is { Status: InstanceStatus.Active }, unit?.Side == state.PhasingSide, unit?.Definition is not null)))
        {
            return Refused(scope, label, expected, ScenarioA1AdvanceCalculator.AdvanceUnitText());
        }

        // A2.5 (ruling R25.1): units whose entry turn has come and that did not enter in the MPh enter by advance, as one advance into a hex of their edge.
        var offBoard = units.Count(unit => unit!.Position is OffMapPosition && state.Location(unit.Id) is null);
        HexsideDirection? entering = null;
        if (offBoard > 0)
        {
            if (ScenarioA1AdvanceCalculator.EntryStackBar(offBoard, units.Length) is { } stackBar)
            {
                return Refused(scope, label, expected, stackBar);
            }

            var (edge, crossing, barred) = EntryCheck(state, [.. units.Select(unit => unit!)], to, true);
            if (edge is null)
            {
                return Refused(scope, label, expected, barred!);
            }

            entering = crossing;
        }

        // An entering stack has no Location yet; it advances from the mirror-image hex beyond the edge (ruling R25.1).
        BoardLocation from;
        if (entering is not null)
        {
            from = to;
        }
        else if (units.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray() is [{ } origin])
        {
            from = origin;
        }
        else
        {
            return Refused(scope, label, expected, ScenarioA1AdvanceCalculator.OneOriginText());
        }

        // A4.7: neither broken, pinned, nor TI; A15.431: a berserk unit does not advance; A11.15: nor one held in Melee; A12.14:
        // concealed movement is not reviewed.
        // Table player, pass 14: the refusal names the condition; a concealed unit may advance (A12.14; ruling R14.2), a hidden one may not (A12.3).
        foreach (var unit in units)
        {
            // Rules names the condition (pass 32.g); the text stays here, since its hole on a Play local is cut by the text list at a nested quote.
            if (ScenarioA1AdvanceCalculator.AdvanceBarringCondition(Is(unit!, Conditions.Broken), Is(unit!, Conditions.Pinned), Is(unit!, Conditions.Berserk), Is(unit!, Conditions.Melee),
                Is(unit!, Conditions.Captured), Is(unit!, Conditions.Hidden), Is(unit!, "asl:ti")) is { } condition)
            {
                return Refused(scope, label, expected,
                    $"play.advance-unit: {unit!.Id} is {condition.Replace("asl:", string.Empty, StringComparison.Ordinal)}, so it may not advance (A4.7, A15.431, A11.15, A12.3)"
                    + (condition == "hidden" ? "; a hidden unit is first placed beneath \"?\" (A12.32; ruling R23.5)" : string.Empty));
            }

            if (ScenarioA1AdvanceCalculator.MovementEndedBar(unit!.Id, unit.MovementEnded) is { } endedBar)
            {
                return Refused(scope, label, expected, endedBar);
            }
        }

        if (units.FirstOrDefault(unit => Mans(state, unit!)) is { } gunner)
        {
            return Refused(scope, label, expected, ScenarioA1AdvanceCalculator.GunnerText(gunner.Id));
        }

        // A4.7 (ruling R25.3): Infantry advance; a vehicle does not, and CC against an enemy vehicle (A11.5) is not reviewed.
        if (units.FirstOrDefault(unit => LiveFire.IsVehicle(unit!)) is { } driven)
        {
            return Refused(scope, label, expected, ScenarioA1AdvanceCalculator.VehicleText(driven.Id));
        }

        // A11.6 (ruling R11.17): a MMC advancing into the Location of a manned, unconcealed enemy AFV passes a PAATC first, aided by a leader in its
        // Location; SMC, Fanatic, and berserk units are exempt. Concealed vehicles are refused below with other concealed units.
        var afv = state.At(to).OfType<UnitInstance>().FirstOrDefault(unit => ScenarioA1AdvanceCalculator.PaatcAfv(unit.Status == InstanceStatus.Active, unit.Side != state.PhasingSide, IsAfv(unit),
            Is(unit, Conditions.Abandoned), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden)));

        // A4.7 (rulings R10.1 to R10.3): one hex, or one level up or down in a stairwell hex, at the MF cost the move would pay; B16.4: never into marsh.
        var (entry, stepReason) = entering is { } edgeSide ? EntryGround(state, to, edgeSide) : InfantryStep(state, from, to);
        if (entry is null)
        {
            return Refused(scope, label, expected, ScenarioA1AdvanceCalculator.AdvanceStepReason(stepReason!));
        }

        if (ScenarioA1AdvanceCalculator.MarshBar(entry.AllMf) is { } marshBar)
        {
            return Refused(scope, label, expected, marshBar);
        }

        var terrain = entry.Terrain;

        // A5.11 (ruling R14.10): entering a Location the advance overstacks costs one more MF per excess squad-equivalent.
        var arriving = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && !Is(unit, Conditions.Captured))
            .Concat(units.Select(unit => unit!)).Distinct().ToArray();
        var excess = OverstackExcess(arriving);

        // A7.7 (ruling R12.11): the first Location an Encircled unit enters costs twice its MF.
        var halfMf = ScenarioA1AdvanceCalculator.AdvanceHalfMf(entry.HalfMf, units.Any(unit => state.Encircled(unit!)), excess);

        // A4.72 EX, A4.12, A4.42 (ruling R10.8): a Good Order leader of its nationality advancing with a MMC adds two MF and one IPC to it.
        var unitList = units.Select(unit => unit!).ToArray();
        // Table player, pass 10: as in the MPh, the leader's IPC goes to the one laden MMC, and his two MF to every MMC of his nationality.
        var (aided, ipcTo) = AdvanceAid(state, unitList);
        bool Aided(UnitInstance unit) => aided.Contains(unit.Id);

        // A4.72 (ruling R5.5): an advance into a Location costing at least four MF, or all of the unit's non-Double Time allotment after
        // portage, makes it CX, and an already CX unit may not make it; a unit left with no MF after portage does not advance.
        var tiring = new List<string>();
        foreach (var unit in units)
        {
            if (DifficultAdvance(state, unit!, halfMf, Aided(unit!), unit!.Id == ipcTo) is not { } difficult)
            {
                return Refused(scope, label, expected, ScenarioA1AdvanceCalculator.NoMfText(unit!.Id));
            }

            if (ScenarioA1AdvanceCalculator.CxDifficultBar(unit!.Id, difficult, Is(unit!, Conditions.Cx)) is { } cxBar)
            {
                return Refused(scope, label, expected, cxBar);
            }

            if (difficult)
            {
                tiring.Add(unit!.Id);
            }
        }

        // A12.14, A11.19 (ruling R14.2): an advance may enter a Location of concealed or hidden enemy units or Dummies, and one holding prisoners;
        // A20.53 (ruling R14.5): a Guard's prisoners go with it.
        var enemies = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && !Is(unit, Conditions.Captured))
            .ToArray();

        // A20.54 (ruling R14.5): an Unarmed unit that is not a prisoner never enters a Location of a Known enemy unit.
        if (ScenarioA1AdvanceCalculator.UnarmedBar(enemies.Any(KnownEnemy), units.FirstOrDefault(unit => Is(unit!, Conditions.Unarmed))?.Id) is { } unarmedBar)
        {
            return Refused(scope, label, expected, unarmedBar);
        }

        // A crew in CC, and the Gun it mans, are not reviewed (ruling R24.3).
        if (ScenarioA1AdvanceCalculator.CrewBar(enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew"))) is { } crewBar)
        {
            return Refused(scope, label, expected, crewBar);
        }

        // A19.12 (ruling R14.11): a Disrupted unit there, not in Melee, surrenders to the Good Order armed Known units advancing in, unless No Quarter.
        // E1.54 (backlog pass 16, ruling R16.6): at night a unit surrenders only in CC.
        var surrendering = enemies.Where(unit => ScenarioA1AdvanceCalculator.Surrenders(state.Night, Is(unit, Conditions.Disrupted), state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal)))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        string[] takers = [.. unitList.Where(unit => ScenarioA1AdvanceCalculator.Taker(Is(unit, Conditions.Unarmed), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden),
            vocabulary.IsA(unit.Kind, "asl:mmc"), vocabulary.IsA(unit.Kind, "asl:smc"))).Select(unit => unit.Id).Order(StringComparer.Ordinal)];
        var summary = ScenarioA1AdvanceCalculator.AdvanceSummary(ids, to.ToString(), terrain, entering is not null, Opposing(enemies), tiring, excess, [.. surrendering.Select(unit => unit.Id)], takers.Length > 0);

        // Pass 31d (design D5; A11.19, p. 73): the proposer's own Dummies that advance into a Location with an enemy counter are removed as the CCPh
        // begins. It is said of the advancing side's own counters and of counters it sees there; nothing is said of what the other side's "?" holds.
        string[] dummiesWarning = ScenarioA1AdvanceCalculator.DummiesWarned(unitList.Any(unit => unit.Kind == UnitKinds.Dummy), enemies.Any(unit => !Is(unit, Conditions.Hidden)))
            ? [$"play.dummies: {(unitList.All(unit => unit.Kind == UnitKinds.Dummy) ? "these Dummies" : $"the Dummies among {string.Join(", ", ids)}")} are removed in {to} as the Close Combat Phase begins, before any attack (A11.19)"]
            : [];
        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        var testing = afv is null ? [] : unitList.Where(unit => NeedsPaatc(state, unit, afv)).ToArray();
        if (afv is not null && testing.Length > 0)
        {
            // A11.6: each testing unit passes its own PAATC before it advances, and does not wait on another's; a failure pins it and it stays.
            IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
            {
                var built = new List<GameEvent>();
                var going = unitList.Where(unit => !testing.Contains(unit)).Select(unit => unit.Id).ToList();
                foreach (var unit in testing)
                {
                    AddPaatc(scope, attemptId, expected, "system", state, [unit], afv, from, built, draw, out var passed);
                    if (passed)
                    {
                        going.Add(unit.Id);
                    }
                }

                if (going.Count > 0)
                {
                    var advanced = EventId(attemptId, built.Count + 1);
                    built.Add(Event(scope, attemptId, built.Count + 1, expected, "advanced", new AdvanceMoved([.. ids.Where(going.Contains)], to), package, null));
                    foreach (var id in tiring.Where(going.Contains))
                    {
                        built.Add(Event(scope, attemptId, built.Count + 1, expected, "conditions-changed",
                            new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Cx] = ConditionState.True }), package, null, [advanced]));
                    }
                }

                return built;
            }

            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
                [summary, .. dummiesWarning, ScenarioA1AdvanceCalculator.PaatcText([.. testing.Select(unit => unit.Id)], afv.Id)])
            {
                Roll = new PlannedRoll("paatc", Build),
                FirstEventId = EventId(attemptId, 1),
            };
        }

        List<GameEvent> events = [Event(scope, attemptId, 1, expected, "advanced", new AdvanceMoved(ids, to), package, null)];
        foreach (var id in tiring)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Cx] = ConditionState.True }), package, null, [EventId(attemptId, 1)]));
        }

        // A12.14 (ruling R14.2; table player, pass 14): a concealed unit keeps its "?" advancing, even into CC, unless it enters Open Ground in the LOS of a
        // Good Order enemy ground unit within 16 hexes.
        // E1.31 (backlog pass 16, ruling R16.4): not at night.
        if (ScenarioA1AdvanceCalculator.LosesConcealment(state.Night, terrain, () => EnemyGoodOrderInLosWithin16(state, state.PhasingSide, to)))
        {
            foreach (var seen in unitList.Where(unit => Is(unit, Conditions.Concealed)))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                    new ConditionsChanged(seen.Id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False }), package, null, [EventId(attemptId, 1)]));
            }
        }

        if (takers.Length > 0)
        {
            foreach (var disrupted in surrendering)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "surrender-pending", new SurrenderPending(disrupted.Id, takers), package, null, [EventId(attemptId, 1)]));
            }
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary, .. dummiesWarning]);
    }

    /// <summary>
    /// The enemy counters an advance meets, as the advancing side may read them (pass 31d; rulings R23.1, R31d.3): a Known unit by its id, every
    /// other counter that is on the map as one "concealed stack", whatever its count and whether it holds a unit, and a hidden unit not at all.
    /// </summary>
    private static string Opposing(UnitInstance[] enemies) =>
        ScenarioA1AdvanceCalculator.Opposing([.. enemies.Select(unit => new OpposingCounterFacts(unit.Id, KnownEnemy(unit), Is(unit, Conditions.Hidden)))]);

    /// <summary>
    /// Whether an advance into a Location of this half MF cost is into Difficult Terrain for the unit (A4.72): at least four MF, or all of its
    /// non-Double Time allotment after portage, whichever is less. Null when the allotment is not decided or none is left after portage.
    /// </summary>
    private bool? DifficultAdvance(GameState state, UnitInstance unit, int halfMf, bool aided = false, bool lent = false) =>
        ScenarioA1AdvanceCalculator.DifficultAdvance(MfAllotment(state, unit, 0, Is(unit, Conditions.Cx), aided ? 2 : 0, lent ? 1 : 0), halfMf);

    /// <summary>
    /// A5.1, A5.5 (ruling R14.10): the squad-equivalents of one side's units above three, rounded up, a HS or crew half and five SMC a HS (four or fewer
    /// none).
    /// </summary>
    private int OverstackExcess(IEnumerable<UnitInstance> units)
    {
        var list = units.ToArray();
        return ScenarioA1AdvanceCalculator.OverstackExcess(list.Count(unit => vocabulary.IsA(unit.Kind, "asl:squad")),
            list.Count(unit => vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew")), list.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc")));
    }

    /// <summary>The Ambush drs of a CC Location (A11.4): one dr for each side, the ATTACKER's first, resolved by the Close Combat package.</summary>
    private GamePlan PlanAmbush(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "location", out var locationText) || !BoardLocation.TryParse(locationText, out var location))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: the Ambush names its Location");
        }

        if (ScenarioA1CloseCombatRules.CcPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        if (ScenarioA1CloseCombatRules.AmbushOrderBar(state.CloseCombats.Any(item => item.Location == location || !item.Closed)) is { } orderBar)
        {
            return Refused(scope, label, expected, orderBar);
        }

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        var (facts, reason) = LiveCloseCombat.AmbushFromState(state, location, terrain);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // E1.77 (backlog pass 16, ruling R16.7): at night, unless Illuminated, an Ambush needs a Final dr only two lower.
        facts = facts with
        {
            DarkNight = ScenarioA1CloseCombatRules.DarkNight(state.Night, Illuminated(state, location))
        };

        var reference = CloseCombatReference.Value;
        var first = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, reference);
        if (ScenarioA1CloseCombatRules.AmbushRollSide(first.Reasons) is null)
        {
            // Pass 31d (design D9): each reason in its sentence, as a round's refusal is worded ("Prisoner guard outside" was a code's last words).
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.ambush-refused", "Close Combat", "Ambush", first.Reasons));
        }

        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var drs = new Dictionary<string, int>(StringComparer.Ordinal);
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            AmbushResolution resolution;
            while ((resolution = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts with
            {
                Rolls = drs
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                if (ScenarioA1CloseCombatRules.AmbushRollSide(resolution.Reasons) is not { } side)
                {
                    throw new InvalidOperationException("The Close Combat package left an Ambush it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var drawn = draw(new RollRequest(1, 6));
                var rollId = $"{attemptId}-roll-{(rollIds.Count + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[side] = rollId;
                drs[side] = drawn.Values[0];
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "cc-ambush", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
            }

            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "ambush-rolled", new AmbushRolled(location, rollIds, resolution.Ambusher,
                JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)), package, null));

            // A11.4 (ruling R14.2): the ambushed side loses all its concealment.
            foreach (var unit in facts.Units!.Where(unit => ScenarioA1CloseCombatRules.AmbushedLosesConcealment(resolution.Ambusher, unit.Side!, unit.Concealed, unit.Captured)))
            {
                var lost = new Dictionary<string, ConditionState>();
                foreach (var (condition, value) in ScenarioA1CloseCombatRules.AmbushedConditions())
                {
                    lost[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;
                }

                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.UnitId!, lost), package, null, [recordId]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1CloseCombatRules.AmbushSummary(location.ToString())])
        {
            Roll = new PlannedRoll("cc-ambush", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// A round of CC in a Location (A11.11, A11.12): the declared attacks, with the SMC stacking declared before them (A11.14),
    /// resolved by the Close Combat package, and refused before any roll unless every outcome the dice can reach is decided.
    /// </summary>
    private GamePlan PlanCloseCombat(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "location", out var locationText) || !BoardLocation.TryParse(locationText, out var location)
            || !arguments.TryGetProperty("attacks", out var attackList) || attackList.ValueKind != JsonValueKind.Array)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: CC names its Location and its attacks (which may be none)");
        }

        // A4.152, A15.432 (ruling R27.3): the CC of a berserk Infantry OVR onto a lone SMC is resolved at once in the MPh.
        var overrun = ScenarioA1CloseCombatRules.OverrunCc(state.Phase, state.Phase == "mph" && BerserkOverrunPending(state) == location);
        if (ScenarioA1CloseCombatRules.RoundPhaseBar(state.Phase, overrun) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        // A11.31 (ruling R11.16): CC in a Location holding a vehicle is sequential, one attack at a time, with the vehicle CC action.
        if (ScenarioA1CloseCombatRules.VehiclePresentBar(state.At(location).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))?.Id, location.ToString()) is { } vehicleBar)
        {
            return Refused(scope, label, expected, vehicleBar);
        }

        var attacks = new List<CloseCombatDeclaration>();
        foreach (var item in attackList.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                return Refused(scope, label, expected, "play.invalid-arguments: each attack names its attackers and defenders");
            }

            attacks.Add(new CloseCombatDeclaration([.. Strings(item, "attackers")], [.. Strings(item, "defenders")])
            {
                Director = Text(item, "director", out var director) ? director : null,

                // A20.22 (ruling R14.4): a capture attempt, the defender's choice at the Kill Number, and the Guard.
                Capture = item.TryGetProperty("capture", out var capture) && capture.ValueKind == JsonValueKind.True ? true : null,
                Yield = Strings(item, "yield").ToArray() is { Length: > 0 } yielded ? yielded : null,
                Guard = Text(item, "guard", out var guard) ? guard : null,
            });
        }

        // A15.432 (ruling R27.3): every berserk unit of the OVR attacks the SMC; the SMC may attack them back; nothing else happens in that CC.
        if (overrun)
        {
            var berserkHere = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide
                && Is(unit, Conditions.Berserk)).Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
            var phasing = attacks.Where(item => item.Attackers!.All(berserkHere.Contains)).ToArray();
            if (ScenarioA1CloseCombatRules.OverrunAttacksBar(phasing.Length, phasing.Length == 1 && phasing[0].Attackers!.ToHashSet(StringComparer.Ordinal).SetEquals(berserkHere), attacks.Count,
                attacks.Any(item => item.Capture == true), arguments.TryGetProperty("withdrawals", out _), arguments.TryGetProperty("infiltrations", out _)) is { } overrunBar)
            {
                return Refused(scope, label, expected, overrunBar);
            }
        }

        // A11.14: the SMC stacking is declared once, before either side's attacks; the ambushed side's round keeps the first round's.
        var stacking = FirstRoundStacking(existing, state, location) ?? Map(arguments, "stacking");

        // A11.2, A11.21: a unit held in Melee may withdraw to an ADJACENT Location it could advance into that holds no Known enemy unit;
        // A11.16: a broken unit held in Melee that can withdraw must attempt it, unless Disrupted or a Guard.
        var withdrawals = Map(arguments, "withdrawals") ?? new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (unitId, destination) in withdrawals)
        {
            // A11.21, A4.43 (ruling R5.5): a withdrawing unit carries no more than its IPC; dropping the SW beyond it is not built.
            if (ScenarioA1CloseCombatRules.WithdrawalPortageBar(unitId, state.Unit(unitId) is { Status: InstanceStatus.Active } laden && Laden(state, laden)) is { } portageBar)
            {
                return Refused(scope, label, expected, portageBar);
            }

            if (ScenarioA1CloseCombatRules.WithdrawalBar(unitId, state.Unit(unitId) is { Status: InstanceStatus.Active } unit && state.Location(unit.Id)?.Location == location && Is(unit, Conditions.Melee)
                && BoardLocation.TryParse(destination, out var to) && WithdrawalDestinations(state, unit, location).Contains(to)) is { } withdrawalBar)
            {
                return Refused(scope, label, expected, withdrawalBar);
            }
        }

        if (state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location == location && MustWithdraw(state, unit, location)
            && !withdrawals.ContainsKey(unit.Id)) is { } broken)
        {
            return Refused(scope, label, expected, ScenarioA1CloseCombatRules.WithdrawRequiredText(broken.Id));
        }

        // A11.21, A4.72 (ruling R5.5): a withdrawal an advance could make only by becoming CX makes the unit CX.
        var tiring = withdrawals.Where(item => state.Unit(item.Key) is { } unit && WithdrawalTires(state, unit, location, BoardLocation.Parse(item.Value)))
            .Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        // A11.22 (ruling R14.8): each Infiltration destination is one a withdrawal could reach.
        var infiltrations = Map(arguments, "infiltrations") ?? new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (unitId, destination) in infiltrations)
        {
            if (ScenarioA1CloseCombatRules.InfiltrationBar(unitId, state.Unit(unitId) is { Status: InstanceStatus.Active } unit && state.Location(unit.Id)?.Location == location
                && BoardLocation.TryParse(destination, out var to) && WithdrawalDestinations(state, unit, location).Contains(to)) is { } infiltrationBar)
            {
                return Refused(scope, label, expected, infiltrationBar);
            }
        }

        tiring.UnionWith(infiltrations.Where(item => state.Unit(item.Key) is { } unit && WithdrawalTires(state, unit, location, BoardLocation.Parse(item.Value)))
            .Select(item => item.Key));

        // J2.31 (ruling R14.1): Hand-to-Hand is declared only where an SSR allows it, with the Location's first round.
        var handToHand = arguments.TryGetProperty("handToHand", out var hand) && hand.ValueKind == JsonValueKind.True;
        if (ScenarioA1CloseCombatRules.HandToHandSsrBar(handToHand, state.SpecialRules.Contains(HandToHandRule, StringComparer.Ordinal)) is { } ssrBar)
        {
            return Refused(scope, label, expected, ssrBar);
        }

        // Referee, pass 14 (A25.43, G1.64): the ATTACKER declares it with the Location's first round other than the prisoners', unless it was ambushed.
        var begun = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (ScenarioA1CloseCombatRules.HandToHandTimingBar(handToHand, Text(arguments, "round", out var handRound) && handRound == CloseCombatFacts.PrisonersRound,
            begun?.Rounds.Any(item => item != CloseCombatResolved.PrisonersRound) == true, begun?.Ambusher is { } ambushed && ambushed != state.PhasingSide) is { } timingBar)
        {
            return Refused(scope, label, expected, timingBar);
        }

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        var (facts, reason) = LiveCloseCombat.FromState(state, location, terrain, attacks, stacking, withdrawals, Text(arguments, "round", out var round) ? round : null,
            infiltrations, handToHand, overrun);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1CloseCombatCalculator.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.cc-refused", "Close Combat", "round", precheck));
        }

        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var rollIds = new Dictionary<string, string>(StringComparer.Ordinal);
            var rolls = new CloseCombatRolls(null);
            CloseCombatResolution resolution;
            while ((resolution = ScenarioA1CloseCombatCalculator.Resolve(facts with
            {
                Rolls = rolls
            }, reference)).Disposition != CloseCombatResolution.Resolved)
            {
                var next = ScenarioA1CloseCombatRules.NextStep(resolution.Disposition, resolution.Reasons);
                if (next.Undecided is { } undecided)
                {
                    throw new InvalidOperationException("The Close Combat package left a round it had accepted undecided: " + undecided);
                }

                var (kind, rest, selected, count, purpose) = (next.Kind!, next.Rest!, next.Selected, next.Count, next.Purpose!);
                var key = kind + ":" + rest;
                var drawn = draw(new RollRequest(count, 6));
                var rollId = $"{attemptId}-roll-{(rollIds.Count + 1).ToString(CultureInfo.InvariantCulture)}";
                rollIds[key] = rollId;
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, purpose, count, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                rolls = kind switch
                {
                    "attack" => rolls with { Attacks = With(rolls.Attacks, rest, (IReadOnlyList<int>)drawn.Values) },
                    "randomSelection" => rolls with
                    {
                        RandomSelection = selected.Select((id, index) => (id, index)).Aggregate(rolls.RandomSelection,
                            (map, pair) => With(map, ScenarioA1CloseCombatRules.SelectionKey(rest, pair.id), drawn.Values[pair.index]))
                    },
                    "woundSeverity" => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },
                    "leaderCreation" => rolls with { LeaderCreation = With(rolls.LeaderCreation, rest, drawn.Values[0]) },
                    "escapeNtc" => rolls with { EscapeNtc = With(rolls.EscapeNtc, rest, (IReadOnlyList<int>)drawn.Values) },
                    "leaderStack" => rolls with
                    {
                        LeaderStack = selected.Select((id, index) => (id, index)).Aggregate(rolls.LeaderStack,
                            (map, pair) => With(map, ScenarioA1CloseCombatRules.SelectionKey(rest, pair.id), drawn.Values[pair.index]))
                    },
                    _ => rolls with { WeaponLoss = With(rolls.WeaponLoss, rest, drawn.Values[0]) },
                };
            }

            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "close-combat-resolved",
                new CloseCombatResolved(location, facts.Round!, [.. attacks.SelectMany(item => item.Attackers!)], [.. attacks.SelectMany(item => item.Defenders!)], rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)), package, null));
            foreach (var (type, payload) in CloseCombatEffects(state, location, resolution, attemptId, tiring))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            // A4.152 (ruling R27.3): an SMC that survives the OVR's CC and the units that OVR it are held in Melee, to fight again in the CCPh.
            if (overrun && Replay([.. existing, .. events]).Current is { } after)
            {
                var left = after.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).ToArray();
                if (ScenarioA1CloseCombatRules.OverrunHoldsInMelee(overrun, left.Select(unit => unit.Side).Distinct(StringComparer.Ordinal).Count()))
                {
                    foreach (var unit in left.OrderBy(unit => unit.Id, StringComparer.Ordinal))
                    {
                        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                            new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Melee] = ConditionState.True }), package, null, [recordId]));
                    }
                }
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [ScenarioA1CloseCombatRules.RoundSummary(facts, location.ToString(), ScenarioA1CloseCombatRules.DescribedAttacks(attacks))])
        {
            Roll = new PlannedRoll("cc", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    private static Dictionary<string, T> With<T>(IReadOnlyDictionary<string, T>? existing, string key, T value)
    {
        var next = existing?.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal) ?? new Dictionary<string, T>(StringComparer.Ordinal);
        next[key] = value;
        return next;
    }

    /// <summary>
    /// The Locations a unit held in Melee may withdraw to (A11.21): ADJACENT Locations at ground level an advance could enter, in terrain
    /// the movement review admits, across any hexside but a cliff and at any level, holding no enemy unit other than a prisoner (a
    /// concealed one is not reviewed). A withdrawal an advance could make only by becoming CX makes the unit CX, and an already CX unit may
    /// not make it (A4.72; ruling R5.5).
    /// </summary>
    public IReadOnlyList<BoardLocation> WithdrawalDestinations(GameState state, UnitInstance unit, BoardLocation from) => Laden(state, unit) ? []
        : [.. Neighbors(state, from).Where(to => PlayableBar(state, to) is null && InfantryStep(state, from, to).Entry is { } entry
            && ScenarioA1CloseCombatRules.WithdrawalDestination(true, !entry.AllMf, entry.AllMf ? null : DifficultAdvance(state, unit, entry.HalfMf), Is(unit, Conditions.Cx),
                !entry.AllMf && state.At(to).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured))))];

    /// <summary>Whether a unit carries more PP than its IPC (A4.42): three for a MMC, one for a SMC, none for a wounded SMC, one less while CX.</summary>
    /// <summary>
    /// A4.72 EX, A4.12, A4.42 (ruling R10.8): the MMC advancing with a Good Order leader of their nationality, who add his two MF, and the one laden MMC that
    /// takes his IPC; used by the advance and by an exit by advance (referee, pass 25).
    /// </summary>
    private (HashSet<string> Aided, string? IpcTo) AdvanceAid(GameState state, UnitInstance[] units) =>
        ScenarioA1AdvanceCalculator.AdvanceAid([.. units.Select(unit => new AdvancingUnitFacts(unit.Id, vocabulary.IsA(unit.Kind, "asl:mmc"), vocabulary.IsA(unit.Kind, "asl:leader"), Nationality(unit),
            Is(unit, Conditions.Broken), Is(unit, Conditions.Wounded), Laden(state, unit)))]);

    // A4.42: moved to Rules with the MF allotment (pass 32.b), a step ahead of its slice.
    private bool Laden(GameState state, UnitInstance unit) =>
        ScenarioA1MovementCalculator.Laden(vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Wounded), Is(unit, Conditions.Cx), Portage(state, unit));

    /// <summary>Whether a withdrawal to a Location makes the unit CX (A11.21, A4.72; ruling R5.5).</summary>
    private bool WithdrawalTires(GameState state, UnitInstance unit, BoardLocation from, BoardLocation to) =>
        InfantryStep(state, from, to).Entry is { } entry && ScenarioA1CloseCombatRules.WithdrawalTires(!entry.AllMf, entry.AllMf ? null : DifficultAdvance(state, unit, entry.HalfMf));

    /// <summary>A11.16: a broken unit held in Melee, not Disrupted and not a Guard, must attempt to withdraw when it can.</summary>
    private bool MustWithdraw(GameState state, UnitInstance unit, BoardLocation at) =>
        ScenarioA1CloseCombatRules.MustWithdraw(Is(unit, Conditions.Broken), Is(unit, Conditions.Melee), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Captured),
            () => IsGuard(state, unit), () => WithdrawalDestinations(state, unit, at).Count > 0);

    /// <summary>
    /// The first Location whose CC must still be resolved this CCPh (A15.43, A11.15): it holds a berserk unit with a Known enemy unit, or
    /// a unit that advanced this APh into a Melee, has had no CC, and its units are ones the package reviews. Null when there is none.
    /// </summary>
    private string? CloseCombatRequired(GameState state)
    {
        var active = state.Units.Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)
            && state.Location(unit.Id) is not null).ToArray();
        foreach (var location in active.Select(unit => state.Location(unit.Id)!.Location).Distinct().OrderBy(item => item.ToString(), StringComparer.Ordinal))
        {
            // A15.43, A11.31 (ruling R27.2): a berserk unit that charged a vehicle attacks it in the Location's sequential CC.
            if (BerserkOwingVehicleAttack(state, location, null) is { } owing)
            {
                return ScenarioA1CloseCombatRules.CcRequiredVehicleText(owing.Id, location.ToString());
            }

            if (state.CloseCombats.Any(item => item.Location == location))
            {
                continue;
            }

            var here = active.Where(unit => state.Location(unit.Id)!.Location == location).ToArray();

            // CC with a crew (ruling R24.3) is not reviewed, so it cannot be required; a Location holding a vehicle has its sequential CC (A11.31). Rules
            // picks the unit (pass 32.g).
            if (ScenarioA1CloseCombatRules.RequiredUnit([.. here.Select(unit => new RequiredCcUnitFacts(unit.Id, unit.Side, Is(unit, Conditions.Berserk), Is(unit, Conditions.Melee), Is(unit, Conditions.Broken),
                KnownEnemy(unit), state.Advances.Any(item => item.Unit == unit.Id && item.To == location), vocabulary.IsA(unit.Kind, "asl:crew"), LiveFire.IsVehicle(unit)))])
                is not { } required || here.First(item => item.Id == required.Id) is not { } unit)
            {
                continue;
            }

            var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
            if (LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is not null && MandatoryAttackDecidable(state, location, terrain, unit, here))
            {
                return ScenarioA1CloseCombatRules.CcRequiredText(unit.Id, location.ToString(), required.Berserk);
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the Close Combat package accepts some attack the unit must make (ruling R14.14): the unit alone, with the unit its stacking would take along
    /// left out, against each Known enemy unit there in turn; when it refuses them all, the requirement lapses.
    /// </summary>
    private static bool MandatoryAttackDecidable(GameState state, BoardLocation location, string? terrain, UnitInstance unit, IReadOnlyList<UnitInstance> here)
    {
        if (!state.CloseCombats.Any(item => item.Location == location) && LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is { Units: { } units }
            && ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units, state.HiddenPlaced.Any(id => units.Any(item => item.UnitId == id))))
        {
            return true;
        }

        foreach (var enemy in here.Where(other => other.Side != unit.Side && KnownEnemy(other)))
        {
            var (facts, _) = LiveCloseCombat.FromState(state, location, terrain, [new CloseCombatDeclaration([unit.Id], [enemy.Id])], null);
            // Referee, pass 14: the prisoners kept keep their Guards.
            if (facts is not null && ScenarioA1CloseCombatCalculator.Precheck(facts with { Units = ScenarioA1CloseCombatRules.TrialUnits(facts.Units!, unit.Id, unit.Side) }, CloseCombatReference.Value) is { Count: 0 })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Pass 31 (play test P-03; A4.7, B23.4, B23.421, B23.422): the Locations an advance from a Location may enter, as the movement rules read them:
    /// the ADJACENT hexes at ground level, the same level of an ADJACENT hex of the same building, and the levels a stairwell joins in the hex.
    /// </summary>
    public IReadOnlyList<BoardLocation> AdvanceLocations(GameState state, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(state);
        return [.. ChargeNeighbors(state, at).Distinct().Where(to => InfantryStep(state, at, to) is { Entry: { AllMf: false } })];
    }

    /// <summary>The ADJACENT ground-level Locations of a Location, such as an advance may enter (A4.7).</summary>
    public IReadOnlyList<BoardLocation> AdjacentLocations(GameState state, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(state);
        return [.. Neighbors(state, at).Distinct()];
    }

    /// <summary>
    /// The sides that may declare CC attacks in a Location now, phasing side first: none before its due Ambush drs (A11.4); after an
    /// Ambush, the ambusher until its first round is resolved or when it attacks again, and otherwise the ambushed side (A11.3,
    /// A11.32); both in a simultaneous round (A11.11).
    /// </summary>
    public IReadOnlyList<string> DeclaringSides(GameState state, BoardLocation location, bool ambusherAgain)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (AmbushDue(state, location))
        {
            return [];
        }

        var sides = state.At(location).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && !Is(unit, Conditions.Captured)).Select(unit => unit.Side)
            .Distinct(StringComparer.Ordinal).OrderBy(side => side == state.PhasingSide ? 0 : 1).ThenBy(side => side, StringComparer.Ordinal).ToArray();
        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        return ScenarioA1CloseCombatRules.DeclaringSides(sides, entry?.Ambusher, entry?.Rounds.Count ?? 0, ambusherAgain);
    }

    /// <summary>Whether a Location's Ambush drs are due (A11.4): no CC there yet, and the Close Combat package allows an Ambush.</summary>
    public bool AmbushDue(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        // Ruling R11.16: no Ambush dr is made in a Location holding a vehicle.
        if (!ScenarioA1CloseCombatRules.AmbushDueCandidate(state.Phase, state.CloseCombats.Any(item => item.Location == location),
            state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit))))
        {
            return false;
        }

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        return LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is { Units: { } units } && ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units);
    }

    /// <summary>Why the Close Combat package would refuse a Location's Ambush drs, in its sentence; null when it would roll them.</summary>
    private string? AmbushUndecided(GameState state, BoardLocation location)
    {
        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        if (LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is not { } facts)
        {
            return null;
        }

        var first = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, CloseCombatReference.Value);
        return ScenarioA1CloseCombatRules.AmbushRollSide(first.Reasons) is not null ? null : ScenarioA1CloseCombatRules.AmbushUndecidedText(first.Reasons, RefusalReasons.Explain);
    }

    /// <summary>
    /// What CC is still due before the CCPh ends, in words: open Locations, Ambush drs, rounds a berserk or reinforcing unit requires
    /// (A15.43, A11.15), and broken units in Melee that must attempt to withdraw (A11.16).
    /// </summary>
    public IReadOnlyList<string> CloseCombatDue(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != "ccph")
        {
            return [];
        }

        var due = new List<string>();

        // Pass 31d (design D9): a Location with nothing left after an Ambush (ruling R31c.5) holds the phase no longer, and has no line.
        bool NothingLeft(CloseCombatLocation item) => ScenarioA1CloseCombatRules.NothingLeft(item.Ambusher is not null, item.Rounds.Count, item.Ambusher is { } ambusher
            && state.Units.Any(unit => unit.Status == InstanceStatus.Active && unit.Side != ambusher && !Is(unit, Conditions.Captured) && state.Location(unit.Id)?.Location == item.Location));
        foreach (var open in state.CloseCombats.Where(item => !item.Closed && !NothingLeft(item)))
        {
            // Ruling R11.16 (table-player finding): a Location holding a vehicle has no Ambush; its sides attack in turn.
            due.Add(ScenarioA1CloseCombatRules.DueOpenLine(open.Location.ToString(), open.Next, open.Ambusher));
        }

        var locations = state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location).Distinct().OrderBy(item => item.ToString(), StringComparer.Ordinal).ToArray();
        // Pass 31d (design D9): where the package would refuse the Ambush, the line says so, and no longer asks for drs that cannot be made.
        due.AddRange(locations.Where(location => AmbushDue(state, location)).Select(location => ScenarioA1CloseCombatRules.DueAmbushLine(location.ToString(), AmbushUndecided(state, location))));
        if (CloseCombatRequired(state) is { } required)
        {
            due.Add(required);
        }

        due.AddRange(state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id) is { } held
                && !state.CloseCombats.Any(item => item.Location == held.Location) && MustWithdraw(state, unit, held.Location))
            .Select(unit => ScenarioA1CloseCombatRules.DueWithdrawLine(state.Location(unit.Id)!.Location.ToString(), unit.Id)));
        return due;
    }

    /// <summary>Whether a unit is broken in Melee and must attempt to withdraw this CCPh (A11.16).</summary>
    public bool MustWithdraw(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return state.Location(unit.Id) is { } at && MustWithdraw(state, unit, at.Location);
    }

    /// <summary>Whether a unit mans an active Gun (A21.13, C2.1).</summary>
    private static bool Mans(GameState state, UnitInstance unit) =>
        state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Manned } holding && holding.Holder == unit.Id);

    /// <summary>A Guard: a unit with an active prisoner (A20.5).</summary>
    private static bool IsGuard(GameState state, UnitInstance unit) =>
        state.Units.Any(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == unit.Id);

    /// <summary>
    /// Why a berserk unit may not step into a Location (A15.431, A15.432): it holds a Gun's crew, whose CC is not built (backlog). A charge at a vehicle
    /// enters its Location for the sequential CC there (ruling R27.2). Null when the step is allowed.
    /// </summary>
    private string? ChargeBarred(GameState state, string side, BoardLocation to)
    {
        var there = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        var enemies = there.Where(unit => unit.Side != side).ToArray();

        // Ruling R10.15: a charge into concealed units reveals them, one into prisoners enters, and one onto a lone SMC is an Infantry OVR; a Gun's
        // crew in CC stays unreviewed (ruling R24.3). Table player, pass 27: CC between Infantry in a Location holding a vehicle is not built (R11.16).
        return ScenarioA1CloseCombatRules.ChargeBarred(enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")), enemies.Any(LiveFire.IsVehicle), enemies.Any(unit => !LiveFire.IsVehicle(unit)), to.ToString());
    }

    /// <summary>A map of unit ids in the arguments, such as each SMC's MMC or each withdrawing unit's destination.</summary>
    private static Dictionary<string, string>? Map(JsonElement arguments, string name) =>
        arguments.TryGetProperty(name, out var map) && map.ValueKind == JsonValueKind.Object
            ? map.EnumerateObject().Where(item => item.Value.ValueKind == JsonValueKind.String)
                .ToDictionary(item => item.Name, item => item.Value.GetString()!, StringComparer.Ordinal)
            : null;

    /// <summary>The stacking the Location's first round declared this CCPh, which the ambushed side's round keeps (A11.14).</summary>
    private static Dictionary<string, string>? FirstRoundStacking(IReadOnlyList<GameEvent> existing, GameState state, BoardLocation location)
    {
        if (!ScenarioA1CloseCombatRules.KeepsFirstRoundStacking(state.CloseCombats.FirstOrDefault(item => item.Location == location)?.Rounds.Count ?? 0))
        {
            return null;
        }

        var first = existing.Select(item => item.Payload).OfType<CloseCombatResolved>().Last(item => item.Location == location);
        return first.Facts.Deserialize<CloseCombatFacts>(LiveFire.Json)?.Units?.Where(unit => unit.StackedWith is not null)
            .ToDictionary(unit => unit.UnitId!, unit => unit.StackedWith!, StringComparer.Ordinal);
    }

    /// <summary>
    /// The events a CC round's resolution records (A11.11 to A11.13, A18.12, A15.46): each created leader, the eliminations, the
    /// Reductions to HS, the wounds, the end of berserk status, and each SW lost.
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> CloseCombatEffects(GameState state, BoardLocation location, CloseCombatResolution resolution,
        string attemptId, HashSet<string> tiring)
    {
        foreach (var leader in resolution.CreatedLeaders)
        {
            var id = ScenarioA1PrisonerCalculator.CreatedLeaderId(attemptId, leader.Attack);
            // Pass 31 (play test P-26): a created leader is neither berserk nor a prisoner, so his Good Order is known and he may direct fire; the
            // Melee of his Location is recorded for him with its other units. Rules gives the conditions in the record's order (pass 32.g).
            var conditions = ConditionChanges(ScenarioA1PrisonerCalculator.CreatedCcLeaderConditions(state.Unit(leader.StackedWith) is { } mmc && GameState.Condition(mmc, Conditions.Fanatic) == ConditionState.True));

            // A11.22, A18.12 (ruling R14.8): a leader created by an attack whose MMC infiltrates goes with it.
            var placedAt = BoardLocation.Parse(ScenarioA1PrisonerCalculator.CreatedLeaderPlacedAt(resolution.Effects.FirstOrDefault(item => item.UnitId == leader.StackedWith)?.InfiltratedTo, location.ToString()));
            yield return ("instance-created", new InstanceCreated(new NewInstance(id, "asl:leader", leader.DefinitionId, leader.Side, new MapPosition(placedAt), null, conditions)));
            if (leader.Eliminated)
            {
                yield return ("instance-eliminated", new InstanceEliminated(id));
            }
            else if (leader.Wounded)
            {
                yield return ("conditions-changed", new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Wounded] = ConditionState.True }));
            }
        }

        foreach (var effect in resolution.Effects)
        {
            var unit = state.Unit(effect.UnitId)!;
            if (effect.Eliminated)
            {
                yield return ("instance-eliminated", new InstanceEliminated(unit.Id));
                continue;
            }

            // A20.22, A20.24, A20.5 (ruling R14.4): a captured unit abandons its SW and is placed with its Guard, or is freed as Unarmed; a squad captured
            // at the Kill Number is exchanged for two HS, one of them captured.
            if (effect.Captured == true)
            {
                foreach (var (type, payload) in CaptureEffects(state, unit, effect, attemptId))
                {
                    yield return (type, payload);
                }

                continue;
            }

            // A20.55 (ruling R14.6): a prisoner that attacked is no longer guarded; a SMC among them is Armed again.
            if (effect.Escaped == true)
            {
                yield return ("prisoner-freed", new PrisonerFreed(unit.Id));
            }

            // A20.551, A11.19, A12.14 (rulings R14.2, R14.6): Armed again and the "?" lost, in the record's order (pass 32.g).
            var conditions = ConditionChanges(ScenarioA1PrisonerCalculator.RoundConditions(effect.Armed == true, effect.ConcealmentLost == true));

            // A20.551 (ruling R14.6): an Unarmed MMC is rearmed as a Conscript MMC of its size and nationality.
            if (effect.RearmedAs is { } rearmed)
            {
                var conscript = CloseCombatReference.Value.Definitions[rearmed];
                var armed = unit.Conditions.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
                foreach (var (name, value) in conditions)
                {
                    armed[name] = value;
                }

                // Table player, pass 14: a rearmed prisoner is no longer captured.
                foreach (var (condition, value) in ScenarioA1PrisonerCalculator.RearmedConditions(effect.Escaped == true))
                {
                    armed[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;
                }

                yield return ("lineage", new LineageRecorded(LineageAction.Replaced, [unit.Id],
                    [new NewInstance($"{attemptId}-{unit.Id}", conscript.Kind, conscript.Id, unit.Side, unit.Position, null, armed)]));
                continue;
            }

            foreach (var (condition, value) in ScenarioA1PrisonerCalculator.AfterRoundConditions(effect.Wounded, GameState.Condition(unit, Conditions.Wounded) == ConditionState.True, effect.BerserkEnded == true))
            {
                conditions[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;
            }

            if (ScenarioA1PrisonerCalculator.Reduced(effect.DefinitionId, effect.FinalDefinitionId))
            {
                // A11.11, A7.302: Casualty Reduction makes the squad's HS, with the same status.
                var half = CloseCombatReference.Value.Definitions[effect.FinalDefinitionId];
                var produced = unit.Conditions.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
                foreach (var (name, value) in conditions)
                {
                    produced[name] = value;
                }

                yield return ("lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                    [new NewInstance($"{attemptId}-{unit.Id}", half.Kind, half.Id, unit.Side, unit.Position, null, produced)]));
            }
            else if (conditions.Count > 0)
            {
                yield return ("conditions-changed", new ConditionsChanged(unit.Id, conditions));
            }
        }

        foreach (var weapon in resolution.WeaponEffects.Where(item => item.Eliminated))
        {
            yield return ("instance-eliminated", new InstanceEliminated(weapon.EquipmentId));
        }

        // A11.2: a withdrawing unit neither eliminated nor Reduced leaves the Melee for the Location it declared, CX when the withdrawal needs it
        // (A11.21, A4.72; ruling R5.5); A11.22 (ruling R14.8): so does an infiltrating unit.
        foreach (var (effect, moved) in resolution.Effects.Select(item => (item, ScenarioA1PrisonerCalculator.MovesAfterRound(item.WithdrewTo, item.InfiltratedTo, item.RearmedAs))).Where(pair => pair.Item2 is not null))
        {
            yield return ("instance-moved", new InstanceMoved(effect.UnitId, new MapPosition(BoardLocation.Parse(moved!))));
            if (tiring.Contains(effect.UnitId))
            {
                yield return ("conditions-changed", new ConditionsChanged(effect.UnitId, new Dictionary<string, ConditionState> { [Conditions.Cx] = ConditionState.True }));
            }
        }
    }

    /// <summary>The name of the SSR that allows Hand-to-Hand CC (J2.31; ruling R14.1).</summary>
    public const string HandToHandRule = "hand-to-hand";

    /// <summary>
    /// A captured unit's events (A20.22, A20.24, A20.5; ruling R14.4): its SW left in the Location, its status cleared (a prisoner is never broken), and its
    /// Guard, or its freeing as an Unarmed unit of its side; a squad captured at the Kill Number is first exchanged for two HS, the first captured.
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> CaptureEffects(GameState state, UnitInstance unit, CloseCombatUnitEffect effect, string attemptId)
    {
        var at = state.Location(unit.Id)!.Location;
        foreach (var weapon in !ScenarioA1PrisonerCalculator.CaptiveDropsWeapons(effect.CapturedHalf) ? Enumerable.Empty<EquipmentInstance>()
            : state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            yield return ("equipment-transferred", new EquipmentTransferred(weapon.Id, null, new MapPosition(at)));
        }

        var captive = unit.Id;
        if (effect.CapturedHalf == true)
        {
            var half = CloseCombatReference.Value.Definitions[ScenarioA1FireReference.HalfSquadOf(unit.Definition!.Definition)!];
            var (captiveHalf, free) = ScenarioA1PrisonerCalculator.CapturedHalfIds(attemptId, unit.Id);
            captive = captiveHalf;
            yield return ("lineage", new LineageRecorded(LineageAction.Deployed, [unit.Id],
                [new NewInstance(captive, half.Kind, half.Id, unit.Side, unit.Position, null, unit.Conditions),
                    new NewInstance(free, half.Kind, half.Id, unit.Side, unit.Position, null, unit.Conditions)]));

            // Referee, pass 14: the HS that is not captured keeps the squad's SW (A20.24 takes only the captured unit's).
            foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                yield return ("equipment-transferred", new EquipmentTransferred(weapon.Id, new Holding(free, HoldingRole.Possessed), null));
            }
        }

        var captured = new Dictionary<string, ConditionState>();
        foreach (var (condition, value) in ScenarioA1PrisonerCalculator.CapturedConditions())
        {
            captured[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;
        }

        yield return ("conditions-changed", new ConditionsChanged(captive, captured));
        if (effect.GuardId is { } guard)
        {
            yield return ("instance-captured", new InstanceCaptured(captive, guard));
        }
    }

    /// <summary>
    /// The captor's choice for a pending surrender (A15.5, A20.21): the unit abandons its SW in its Location (A20.24), is placed with
    /// the Guard, and becomes its prisoner, no longer broken or Disrupted (A20.54: prisoners are never broken). With no captor able to guard it, the
    /// captor's side frees it as an Unarmed unit instead (A20.21, A20.51; ruling R14.5).
    /// </summary>
    private GamePlan PlanTakePrisoner(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var reject = arguments.TryGetProperty("reject", out var rejecting) && rejecting.ValueKind == JsonValueKind.True;
        if (!Text(arguments, "unitId", out var unitId) || (!reject && !Text(arguments, "captorId", out _)))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a capture names the surrendering unit and its captor, or rejects the surrender");
        }

        var captorId = Text(arguments, "captorId", out var named) ? named : string.Empty;
        if (state.PendingSurrenders.FirstOrDefault(item => item.Unit == unitId) is not { } pending)
        {
            return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.NoSurrenderText(unitId));
        }

        // A20.21, A20.51 (ruling R14.5): the unit goes to a captor with Guard capacity; when none has any, it is freed as Unarmed.
        var able = pending.Captors.Where(id => state.Unit(id) is { Status: InstanceStatus.Active } captor && state.Unit(unitId) is { } surrendered
            && ScenarioA1PrisonerCalculator.CanGuard(GuardLoad(state, captor), UnitSize(surrendered), UnitSize(captor))).ToArray();
        var free = arguments.TryGetProperty("free", out var freeing) && freeing.ValueKind == JsonValueKind.True;
        if (ScenarioA1PrisonerCalculator.FreesOrRedirects(free, reject, able.Length > 0, able.Contains(captorId, StringComparer.Ordinal), pending.Captors.Contains(captorId, StringComparer.Ordinal)))
        {
            if (able.Length > 0)
            {
                return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.CaptorCapacityText(able, unitId));
            }

            if (state.Unit(unitId) is not { } loose || state.Location(loose.Id) is not { } looseAt)
            {
                return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.NoSurrenderText(unitId));
            }

            var releasing = ScenarioA1FirePackage.Identity.ToString();
            var released = new List<GameEvent>();
            foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == loose.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                released.Add(Event(scope, attemptId, released.Count + 1, expected, "equipment-transferred", new EquipmentTransferred(weapon.Id, null, new MapPosition(looseAt.Location)),
                    releasing, null, [pending.Event]));
            }

            released.Add(Event(scope, attemptId, released.Count + 1, expected, "prisoner-freed", new PrisonerFreed(loose.Id), releasing, null, [pending.Event]));
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected, released,
                [ScenarioA1PrisonerCalculator.FreedUnarmedText(unitId)]);
        }

        // A20.3 (ruling R5.6): the captor's side may reject the surrender, eliminating the unit and facing its side with No Quarter.
        if (reject)
        {
            var side = state.Unit(unitId)?.Side;
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "surrender-rejected", new SurrenderRejected(unitId), ScenarioA1FirePackage.Identity.ToString(), null, [pending.Event])],
                [ScenarioA1PrisonerCalculator.NoQuarterText(unitId, side)]);
        }

        if (!pending.Captors.Contains(captorId, StringComparer.Ordinal) || state.Unit(captorId) is not { Status: InstanceStatus.Active } captor
            || state.Location(captor.Id) is not { } guardAt || state.Unit(unitId) is not { } prisoner || state.Location(prisoner.Id) is not { } prisonerAt)
        {
            return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.CaptorText(unitId, pending.Captors));
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        var events = new List<GameEvent>();
        foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == prisoner.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred", new EquipmentTransferred(weapon.Id, null, new MapPosition(prisonerAt.Location)),
                package, null, [pending.Event]));
        }

        if (guardAt.Location != prisonerAt.Location)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(prisoner.Id, new MapPosition(guardAt.Location)), package, null,
                [pending.Event]));
        }

        var taken = new Dictionary<string, ConditionState>();
        foreach (var (condition, value) in ScenarioA1PrisonerCalculator.TakenPrisonerConditions())
        {
            taken[ConditionName(condition)] = value ? ConditionState.True : ConditionState.False;
        }

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(prisoner.Id, taken), package, null, [pending.Event]));
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-captured", new InstanceCaptured(prisoner.Id, captor.Id), package, null, [pending.Event]));
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [ScenarioA1PrisonerCalculator.CaptureSummary(prisoner.Id, captor.Id, guardAt.Location.ToString())]);
    }

    /// <summary>
    /// Whether a Known enemy unit is in LOS of a Location (A15.44): an enemy unit neither concealed, hidden, a Dummy, nor a
    /// prisoner (A20.4), in the Location or with a clear LOS to it. Null when an LOS cannot be read.
    /// </summary>
    public bool? KnownEnemyInLos(GameState state, string side, BoardLocation at)
    {
        // Pass 32.g: the Known enemies' Locations cross as indexes into a table, the LOS read as Rules asks for it, in their order.
        var locations = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && KnownEnemy(unit))
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().ToList();
        var table = locations.Contains(at) ? locations : [.. locations, at];
        return ScenarioA1PrisonerCalculator.KnownEnemyInLos([.. Enumerable.Range(0, locations.Count)], table.IndexOf(at), index => Los(state, table[index], at)?.Status switch
        {
            LosStatus.Clear => true,
            LosStatus.Blocked => false,
            _ => null,
        });
    }

    private static bool KnownEnemy(UnitInstance unit) => ScenarioA1PrisonerCalculator.KnownEnemy(unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden), Is(unit, Conditions.Captured));

    /// <summary>
    /// The units a unit may surrender to (A15.5, A20.21): ADJACENT (A.8) Known, Good Order, armed enemy Infantry that can guard
    /// it, their prisoners' US# with it at most five times their own (A20.51; US#: squad 3, HS 2, SMC 1, A1.6).
    /// </summary>
    public IReadOnlyList<string>? Captors(GameState state, UnitInstance unit, IReadOnlyCollection<string>? revealed = null)
    {
        if (state.Location(unit.Id)?.Location is not { } at)
        {
            return [];
        }

        // Pass 32.g: the captor test is Rules'; the ADJACENCY (a LOS read) is read only for a unit that passes it, as before.
        var adjacent = state.Units.Where(other => ScenarioA1PrisonerCalculator.Captor(other.Status == InstanceStatus.Active, other.Side != unit.Side, other.Definition is not null, KnownEnemy(other),
                revealed?.Contains(other.Id) == true && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured), Is(other, Conditions.Broken), Is(other, Conditions.Berserk),
                Is(other, Conditions.Melee), Is(other, Conditions.Unarmed), vocabulary.IsA(other.Kind, "asl:mmc") || vocabulary.IsA(other.Kind, "asl:smc"))
                && state.Location(other.Id)?.Location is { } there && IsAdjacent(state, there, at)).ToArray();

        // A20.21, A20.51 (ruling R14.5): the captors with Guard capacity; with none, every captor, and the captor's side frees the unit as Unarmed.
        return ScenarioA1PrisonerCalculator.Captors([.. adjacent.Select(other => new CaptorFacts(other.Id, true, ScenarioA1PrisonerCalculator.CanGuard(GuardLoad(state, other), UnitSize(unit), UnitSize(other))))]);
    }

    /// <summary>The US# of a unit's prisoners (A20.51).</summary>
    private int GuardLoad(GameState state, UnitInstance guard) =>
        state.Units.Where(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == guard.Id).Sum(UnitSize);

    /// <summary>A unit's US# (A1.6, p. 45; A20.51): a squad 3, a HS or crew 2, a SMC 1.</summary>
    private int UnitSize(UnitInstance unit) => ScenarioA1PrisonerCalculator.UnitSize(vocabulary.IsA(unit.Kind, "asl:squad"), vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew"));

    /// <summary>The ADJACENT Location across each hexside of a Location's hex, at ground level, on one board or across a seam.</summary>
    private IEnumerable<BoardLocation> Neighbors(GameState state, BoardLocation at)
    {
        if (Composed(state) is { } composed)
        {
            foreach (var side in Enum.GetValues<HexsideDirection>())
            {
                if (composed.Neighbor(at.Board, at.Hex, side) is { } across)
                {
                    yield return new BoardLocation(across.Board, across.Hex, 0);
                }
            }

            yield break;
        }

        if (state.Map.Board(at.Board) is not { } placed || boards.TryGetBoard(at.Board, placed.Version).Board is not { } handle)
        {
            yield break;
        }

        foreach (var side in Enum.GetValues<HexsideDirection>())
        {
            if (handle.Neighbor(at.Hex, side) is { } hex)
            {
                yield return new BoardLocation(at.Board, hex, 0);
            }
        }
    }

    /// <summary>The distance in hexes between two Locations, or null when the map cannot give it.</summary>
    private int? HexDistance(GameState state, BoardLocation one, BoardLocation two)
    {
        if (Composed(state) is { } composed)
        {
            return composed.Distance(one.Board, one.Hex, two.Board, two.Hex);
        }

        return one.Board == two.Board && state.Map.Board(one.Board) is { } placed && boards.TryGetBoard(one.Board, placed.Version).Board is { } handle
            ? handle.Distance(one.Hex, two.Hex)
            : null;
    }

    /// <summary>
    /// The half MF to enter a Location from an ADJACENT one as the movement rules of step 22 decide it, or null when the entry is not
    /// reviewed (a level change, hexside terrain, or terrain the review does not admit).
    /// </summary>
    private int? EntryCost(GameState state, BoardLocation from, BoardLocation to) => InfantryStep(state, from, to).Entry switch
    {
        null => null,

        // B16.4 (ruling R10.15): marsh takes a berserk unit's whole eight MF; from below only by Minimum Move, which a berserk unit does not make.
        { MinimumMoveOnly: true } => null,
        { AllMf: true } => 16,
        { } entry => entry.HalfMf,
    };

    /// <summary>A step of a berserk charge (A15.431; ruling R27.2): the Location charged, the least half MF of the step, whether it may be taken
    /// as an ordinary step, and the Bypass lanes (hexsides, lowercase, comma-separated) it may be taken in.</summary>
    private sealed record ChargeStep(BoardLocation Target, int HalfMf, bool Plain, IReadOnlyList<string> Lanes);

    /// <summary>A Bypass lane's key: its hexsides, lowercase, comma-separated.</summary>
    internal static string LaneKey(IEnumerable<HexsideDirection> lane) => string.Join(",", lane.Select(side => side.ToString().ToLowerInvariant()));

    internal static List<HexsideDirection> Lane(string key) => [.. key.Split(',').Select(name => Enum.Parse<HexsideDirection>(name, ignoreCase: true))];

    /// <summary>
    /// The Locations a charge may step to from a Location (B23.4, B23.421; ruling R27.2): the ADJACENT hexes at ground level and, from an upper
    /// level, at that level; and the levels above and below in the same hex. Which of them can be entered, and at what cost, is the movement
    /// rules' (rulings R10.1 to R10.3).
    /// </summary>
    private IEnumerable<BoardLocation> ChargeNeighbors(GameState state, BoardLocation at)
    {
        foreach (var hex in Neighbors(state, at))
        {
            yield return hex;
            if (at.Level != 0)
            {
                yield return hex with
                {
                    Level = at.Level
                };
            }
        }

        foreach (var level in HexLevels(state, at).Where(level => Math.Abs(level - at.Level) == 1))
        {
            yield return at with
            {
                Level = level
            };
        }
    }

    /// <summary>
    /// The charge search's fact reader (pass 32.g, S6): Locations as indexes into a table built as the search asks for them, hexsides as 0 to 5, the map
    /// reads, the entry and Bypass costs (for the movers), and the LOS and distances, all Play's; the search is Rules'.
    /// </summary>
    private sealed class ChargeScan : IChargeFactReader
    {
        private readonly GamePlanner planner;
        private readonly GameState state;
        private readonly UnitInstance[] movers;
        private readonly Func<UnitInstance, bool> crew;
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        public ChargeScan(GamePlanner planner, GameState state, UnitInstance[] movers, Func<UnitInstance, bool> crew)
        {
            this.planner = planner;
            this.state = state;
            this.movers = movers;
            this.crew = crew;
        }

        /// <summary>The index of a Location in the table, added when it is new.</summary>
        public int Index(BoardLocation location)
        {
            if (!indexes.TryGetValue(location, out var index))
            {
                index = locations.Count;
                locations.Add(location);
                indexes[location] = index;
            }

            return index;
        }

        /// <summary>The Location at an index of the table.</summary>
        public BoardLocation At(int index) => locations[index];

        public string Name(int location) => locations[location].ToString();

        public int Level(int location) => locations[location].Level;

        public IEnumerable<int> Neighbors(int location) => planner.ChargeNeighbors(state, locations[location]).Select(Index);

        public bool Playable(int location) => planner.PlayableBar(state, locations[location]) is null;

        public int? EntryCost(int fromLocation, int toLocation) => planner.EntryCost(state, locations[fromLocation], locations[toLocation]);

        public int? Across(int location, int side) => planner.Across(state, locations[location], (HexsideDirection)side) is { } exit ? Index(exit) : null;

        public int? SideToward(int fromLocation, int toLocation) => planner.SideToward(state, locations[fromLocation], locations[toLocation]) is { } side ? (int)side : null;

        public bool Reads(int location) => planner.ReadLocation(state, locations[location]) is not null;

        /// <summary>A Bypass of an obstacle hex from a Location the map reads (A4.3, A4.31): the wall on the hexside entered counts, and the movers' entry decides.</summary>
        public int? BypassHalfMf(int obstacle, int side, IReadOnlyList<int> lane, int fromLocation)
        {
            var from = locations[fromLocation];
            var wall = planner.SideToward(state, from, locations[obstacle]) is { } toward ? WallOn(planner.HexsideAt(state, from, toward)) : null;
            return planner.BypassStep(state, movers, locations[obstacle], (HexsideDirection)side, planner.ReadLocation(state, from)!.Hex.BaseLevel, wall,
                [.. lane.Select(item => (HexsideDirection)item)]).Entry?.HalfMf;
        }

        public string LaneKey(IReadOnlyList<int> lane) => GamePlanner.LaneKey(lane.Select(item => (HexsideDirection)item));

        public IReadOnlyList<int> Lane(string key) => [.. GamePlanner.Lane(key).Select(item => (int)item)];

        public bool LosClear(int fromLocation, int toLocation) => planner.Los(state, locations[fromLocation], locations[toLocation]) is { Status: LosStatus.Clear };

        public int? Distance(int one, int two) => planner.HexDistance(state, locations[one], locations[two]);

        public ChargeOccupantFacts Occupants(int location)
        {
            var enemies = state.At(locations[location]).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != movers[0].Side).ToArray();
            return new ChargeOccupantFacts(enemies.Any(crew), enemies.Any(LiveFire.IsVehicle), enemies.Any(unit => !LiveFire.IsVehicle(unit)));
        }
    }

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431; ruling R27.2): the Known enemy units' Locations
    /// and the stack's start (its Location, or its Bypass lane, A4.32) are read here, Rules searches through a <see cref="ChargeScan"/>, and the steps come
    /// back by Location. <paramref name="current"/> is the stack's move this MPh, when the movers are in it.
    /// </summary>
    private (IReadOnlyDictionary<BoardLocation, ChargeStep> Steps, BoardLocation? Target, string? Undecided) ChargeSteps(GameState state,
        UnitInstance[] movers, BoardLocation from, MovementState? current)
    {
        var side = movers[0].Side;
        var scan = new ChargeScan(this, state, movers, unit => vocabulary.IsA(unit.Kind, "asl:crew"));

        // Table player, pass 27: an Abandoned vehicle is no unit to charge.
        int[] enemies = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && ScenarioA1ChargeCalculator.ChargeTarget(KnownEnemy(unit), LiveFire.IsVehicle(unit), Is(unit, Conditions.Abandoned)))
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().Select(scan.Index)];
        ChargeNodeFacts Start()
        {
            var at = scan.Index(from);
            return current is { Bypass: { Count: > 0 } lane } && current.Location == from && movers.All(unit => current.Movers.Contains(unit.Id, StringComparer.Ordinal))
                && BypassEntered(state, from, current.From, lane) is { } entered
                ? new ChargeNodeFacts(at, (int)entered, LaneKey(lane))
                : new ChargeNodeFacts(at, null, null);
        }

        var (steps, target, undecided) = ScenarioA1ChargeCalculator.Steps(scan, scan.Index(from), enemies, current?.Charge is { } previous ? scan.Index(previous) : null, Start);
        return (steps.ToDictionary(item => scan.At(item.Key), item => new ChargeStep(scan.At(item.Value.Target), item.Value.HalfMf, item.Value.Plain, item.Value.Lanes)),
            target is { } found ? scan.At(found) : null, undecided);
    }

    /// <summary>
    /// The phasing side's berserk units that are to charge in this MPh (A15.43): each with the Location it charges and the next
    /// Locations on a shortest route to it (A15.431), for the page to mark; or, when the model cannot decide its charge, why not, so
    /// it may end in place (ruling R30.5). Units held in Melee, done moving, or already with a Known enemy unit are not listed.
    /// </summary>
    public IReadOnlyList<(UnitInstance Unit, BoardLocation? Target, IReadOnlyList<BoardLocation> Next, string? Undecided)> Charges(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != "mph")
        {
            return [];
        }

        var must = MustCharge(state);
        var charges = new List<(UnitInstance, BoardLocation?, IReadOnlyList<BoardLocation>, string?)>();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && Is(unit, Conditions.Berserk)
            && !Is(unit, Conditions.Melee) && !unit.MovementEnded && state.Location(unit.Id) is not null).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            var (steps, target, undecided) = ChargeSteps(state, [unit], state.Location(unit.Id)!.Location,
                state.Movement?.Members.Contains(unit.Id) == true ? state.Movement : null);
            if (must.Contains(unit))
            {
                charges.Add((unit, steps.Values.First().Target, [.. steps.Keys.OrderBy(item => item.ToString(), StringComparer.Ordinal)], null));
            }
            else if (undecided is not null)
            {
                charges.Add((unit, target, [], undecided));
            }
        }

        return charges;
    }

    /// <summary>
    /// The Bypass lanes a berserk unit's next steps may take (ruling R27.2; table player, pass 27): by step, each lane's hexsides in the order a move names
    /// them. Empty when no step is in Bypass.
    /// </summary>
    public IReadOnlyDictionary<BoardLocation, IReadOnlyList<string>> ChargeLanes(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return state.Location(unit.Id) is not { } at ? new Dictionary<BoardLocation, IReadOnlyList<string>>()
            : ChargeSteps(state, [unit], at.Location, state.Movement?.Members.Contains(unit.Id) == true ? state.Movement : null).Steps
                .Where(step => step.Value.Lanes.Count > 0).ToDictionary(step => step.Key, step => step.Value.Lanes);
    }

    /// <summary>
    /// The berserk units of the phasing side that must still charge this MPh (A15.43): not held in Melee, not done moving, not in a
    /// Location with a Known enemy unit, and with a step they can afford on a decided route.
    /// </summary>
    private IReadOnlyList<UnitInstance> MustCharge(GameState state) =>
        [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && Is(unit, Conditions.Berserk) && !Is(unit, Conditions.Melee)
            && !unit.MovementEnded && state.Location(unit.Id) is { } at
            && ChargeSteps(state, [unit], at.Location, state.Movement?.Members.Contains(unit.Id) == true ? state.Movement : null) is { Steps.Count: > 0 } charge
            && MfAllotment(state, unit, unit.DoubleTimeMf, Is(unit, Conditions.Cx)) is { } allowance
            && charge.Steps.Values.Any(step => ScenarioA1ChargeCalculator.Affordable(allowance, unit.MfSpent, unit.HalfMfSpent, step.HalfMf)))];
}
