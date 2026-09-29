using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.ScenarioA1;
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

        var ids = Strings(arguments, "unitIds").ToArray();
        if (ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length || !Text(arguments, "to", out var toText)
            || !BoardLocation.TryParse(toText, out var to))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an advance names its units and the Location they enter");
        }

        if (state.Phase != "aph")
        {
            return Refused(scope, label, expected, "play.advance-phase: units advance in their side's APh (A3.7, p. 47)");
        }

        var units = ids.Select(state.Unit).ToArray();
        if (units.Any(unit => unit is not { Status: InstanceStatus.Active } || unit.Side != state.PhasingSide || unit.Definition is null))
        {
            return Refused(scope, label, expected, "play.advance-unit: every unit is an active unit of the phasing side from the catalog");
        }

        if (units.Select(unit => state.Location(unit!.Id)?.Location).Distinct().ToArray() is not [{ } from])
        {
            return Refused(scope, label, expected, "play.advance-unit: the units advance from one Location");
        }

        // A4.7: neither broken, pinned, nor TI; A15.431: a berserk unit does not advance; A11.15: nor one held in Melee; A12.14:
        // concealed movement is not reviewed.
        // Table player, pass 14: the refusal names the condition; a concealed unit may advance (A12.14; ruling R14.2), a hidden one may not (A12.3).
        foreach (var unit in units)
        {
            if (new[] { Conditions.Broken, Conditions.Pinned, Conditions.Berserk, Conditions.Melee, Conditions.Captured, Conditions.Hidden, "asl:ti" }
                .FirstOrDefault(condition => Is(unit!, condition)) is { } condition)
            {
                return Refused(scope, label, expected,
                    $"play.advance-unit: {unit!.Id} is {condition.Replace("asl:", string.Empty, StringComparison.Ordinal)}, so it may not advance (A4.7, A15.431, A11.15, A12.3)");
            }

            if (unit!.MovementEnded)
            {
                return Refused(scope, label, expected, $"play.advance-unit: {unit.Id} has advanced or moved as far as it may this phase (A4.7)");
            }
        }

        if (units.FirstOrDefault(unit => Mans(state, unit!)) is { } gunner)
        {
            return Refused(scope, label, expected, $"play.advance-crew-mans-gun: {gunner.Id} mans a Gun; abandoning or moving a Gun is not reviewed (C10, A21.13)");
        }

        // A4.7 (ruling R25.3): Infantry advance; a vehicle does not, and CC against an enemy vehicle (A11.5) is not reviewed.
        if (units.FirstOrDefault(unit => LiveFire.IsVehicle(unit!)) is { } driven)
        {
            return Refused(scope, label, expected, $"play.advance-unit: {driven.Id} is a vehicle; only Infantry advance (A4.7)");
        }

        // A11.6 (ruling R11.17): a MMC advancing into the Location of a manned, unconcealed enemy AFV passes a PAATC first, aided by a leader in its
        // Location; SMC, Fanatic, and berserk units are exempt. Concealed vehicles are refused below with other concealed units.
        var afv = state.At(to).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide && IsAfv(unit)
            && !Is(unit, Conditions.Abandoned) && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden));

        // A4.7 (rulings R10.1 to R10.3): one hex, or one level up or down in a stairwell hex, at the MF cost the move would pay; B16.4: never into marsh.
        var (entry, stepReason) = InfantryStep(state, from, to);
        if (entry is null)
        {
            return Refused(scope, label, expected, stepReason!.Replace("play.move-", "play.advance-", StringComparison.Ordinal));
        }

        if (entry.AllMf)
        {
            return Refused(scope, label, expected, "play.advance-marsh: a marsh hex cannot be entered in the APh (B16.4)");
        }

        var terrain = entry.Terrain;

        // A5.11 (ruling R14.10): entering a Location the advance overstacks costs one more MF per excess squad-equivalent.
        var arriving = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && !Is(unit, Conditions.Captured))
            .Concat(units.Select(unit => unit!)).Distinct().ToArray();
        var excess = OverstackExcess(arriving);

        // A7.7 (ruling R12.11): the first Location an Encircled unit enters costs twice its MF.
        var halfMf = (entry.HalfMf * (units.Any(unit => state.Encircled(unit!)) ? 2 : 1)) + (2 * excess);

        // A4.72 EX, A4.12, A4.42 (ruling R10.8): a Good Order leader of its nationality advancing with a MMC adds two MF and one IPC to it.
        var unitList = units.Select(unit => unit!).ToArray();
        // Table player, pass 10: as in the MPh, the leader's IPC goes to the one laden MMC, and his two MF to every MMC of his nationality.
        bool Aided(UnitInstance unit) => vocabulary.IsA(unit.Kind, "asl:mmc") && Nationality(unit) is { } nationality && unitList.Any(leader => leader.Id != unit.Id
            && vocabulary.IsA(leader.Kind, "asl:leader") && !Is(leader, Conditions.Broken) && Nationality(leader) == nationality);
        var ipcTo = unitList.Where(unit => Aided(unit) && Laden(state, unit)).ToArray() is [{ } onlyLaden]
            && unitList.Any(leader => vocabulary.IsA(leader.Kind, "asl:leader") && !Is(leader, Conditions.Broken) && !Is(leader, Conditions.Wounded)
                && Nationality(leader) == Nationality(onlyLaden)) ? onlyLaden.Id : null;

        // A4.72 (ruling R5.5): an advance into a Location costing at least four MF, or all of the unit's non-Double Time allotment after
        // portage, makes it CX, and an already CX unit may not make it; a unit left with no MF after portage does not advance.
        var tiring = new List<string>();
        foreach (var unit in units)
        {
            if (DifficultAdvance(state, unit!, halfMf, Aided(unit!), unit!.Id == ipcTo) is not { } difficult)
            {
                return Refused(scope, label, expected, $"play.advance-mf: {unit!.Id} has no MF allotment the catalog decides, or none left after portage (A4.7, A4.72)");
            }

            if (difficult && Is(unit!, Conditions.Cx))
            {
                return Refused(scope, label, expected, $"play.advance-difficult-terrain: {unit!.Id} is CX and may not advance into Difficult Terrain (A4.72)");
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
        if (enemies.Any(KnownEnemy) && units.FirstOrDefault(unit => Is(unit!, Conditions.Unarmed)) is { } unarmed)
        {
            return Refused(scope, label, expected, $"play.advance-unarmed: {unarmed.Id} is Unarmed and may not enter a Location of a Known enemy unit (A20.54)");
        }

        // A crew in CC, and the Gun it mans, are not reviewed (ruling R24.3).
        if (enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))
        {
            return Refused(scope, label, expected, "play.advance-crew: CC with a Gun's crew is not reviewed (C11, ruling R24.3)");
        }

        // A19.12 (ruling R14.11): a Disrupted unit there, not in Melee, surrenders to the Good Order armed Known units advancing in, unless No Quarter.
        // E1.54 (backlog pass 16, ruling R16.6): at night a unit surrenders only in CC.
        var surrendering = enemies.Where(unit => !state.Night && Is(unit, Conditions.Disrupted) && !state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal).ToArray();
        string[] takers = [.. unitList.Where(unit => !Is(unit, Conditions.Unarmed) && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden)
            && (vocabulary.IsA(unit.Kind, "asl:mmc") || vocabulary.IsA(unit.Kind, "asl:smc"))).Select(unit => unit.Id).Order(StringComparer.Ordinal)];
        var summary = $"play.advance: {string.Join(", ", ids)} advance into {to} ({terrain})" + (enemies.Length > 0 ? $", with {string.Join(", ", enemies.Select(item => item.Id))}: CC follows (A3.7)" : string.Empty)
            + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} become CX advancing into Difficult Terrain (A4.72)" : string.Empty)
            + (excess > 0 ? $"; the Location is overstacked by {excess} squad-equivalent(s), which costs {excess} more MF (A5.11)" : string.Empty)
            + (surrendering.Length > 0 && takers.Length > 0 ? $"; {string.Join(", ", surrendering.Select(unit => unit.Id))} is Disrupted and surrenders (A19.12)" : string.Empty);
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
                [summary, $"play.advance-paatc: {string.Join(", ", testing.Select(unit => unit.Id))} each pass a PAATC before advancing on {afv.Id}; a unit that fails is pinned and stays (A11.6)"])
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
        if (!state.Night && terrain == "open-ground" && EnemyGoodOrderInLosWithin16(state, state.PhasingSide, to))
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

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events, [summary]);
    }

    /// <summary>
    /// Whether an advance into a Location of this half MF cost is into Difficult Terrain for the unit (A4.72): at least four MF, or all of its
    /// non-Double Time allotment after portage, whichever is less. Null when the allotment is not decided or none is left after portage.
    /// </summary>
    private bool? DifficultAdvance(GameState state, UnitInstance unit, int halfMf, bool aided = false, bool lent = false) =>
        MfAllotment(state, unit, 0, Is(unit, Conditions.Cx), aided ? 2 : 0, lent ? 1 : 0) is { } allotment && allotment > 0 ? halfMf >= 2 * Math.Min(4, allotment) : null;

    /// <summary>
    /// A5.1, A5.5 (ruling R14.10): the squad-equivalents of one side's units above three, rounded up, a HS or crew half and five SMC a HS (four or fewer
    /// none).
    /// </summary>
    private int OverstackExcess(IEnumerable<UnitInstance> units)
    {
        var list = units.ToArray();
        var smc = list.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc"));
        var squads = list.Count(unit => vocabulary.IsA(unit.Kind, "asl:squad"))
            + (list.Count(unit => vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew")) / 2m) + (Math.Floor(smc / 5m) / 2m);
        return squads > 3 ? (int)Math.Ceiling(squads - 3) : 0;
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

        if (state.Phase != "ccph")
        {
            return Refused(scope, label, expected, "play.cc-phase: CC is resolved in the CCPh (A3.8, A11.1)");
        }

        if (state.CloseCombats.Any(item => item.Location == location || !item.Closed))
        {
            return Refused(scope, label, expected, "play.ambush-order: the Ambush drs come before any CC in the Location, with no other Location's CC open (A11.4, A11.12)");
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
            DarkNight = state.Night && !Illuminated(state, location) ? true : null
        };

        var reference = CloseCombatReference.Value;
        var first = ScenarioA1CloseCombatCalculator.ResolveAmbush(facts, reference);
        if (first.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc.roll-missing:ambush:", StringComparison.Ordinal))
        {
            return Refused(scope, label, expected, ["play.ambush-refused: the Close Combat package does not decide the Ambush here", .. first.Reasons]);
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
                if (resolution.Reasons is not [{ } key] || !key.StartsWith("asl.a1.cc.roll-missing:ambush:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The Close Combat package left an Ambush it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var side = key["asl.a1.cc.roll-missing:ambush:".Length..];
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
            foreach (var unit in resolution.Ambusher is { } ambusher ? facts.Units!.Where(unit => unit.Side != ambusher && unit.Concealed == true && unit.Captured != true) : [])
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(unit.UnitId!, new Dictionary<string, ConditionState>
                {
                    [Conditions.Concealed] = ConditionState.False,
                    [Conditions.Hidden] = ConditionState.False,
                }), package, null, [recordId]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.ambush: each side makes an Ambush dr for {location}; one at least three below the other ambushes (A11.4)"])
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

        if (state.Phase != "ccph")
        {
            return Refused(scope, label, expected, "play.cc-phase: CC is resolved in the CCPh (A3.8, A11.1)");
        }

        // A11.31 (ruling R11.16): CC in a Location holding a vehicle is sequential, one attack at a time, with the vehicle CC action.
        if (state.At(location).OfType<UnitInstance>().FirstOrDefault(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)) is { } present)
        {
            return Refused(scope, label, expected, $"play.cc-vehicle: {present.Id} is in {location}, so its CC is sequential, one attack at a time (asl.game.vehicle-close-combat; A11.31)");
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

        // A11.14: the SMC stacking is declared once, before either side's attacks; the ambushed side's round keeps the first round's.
        var stacking = FirstRoundStacking(existing, state, location) ?? Map(arguments, "stacking");

        // A11.2, A11.21: a unit held in Melee may withdraw to an ADJACENT Location it could advance into that holds no Known enemy unit;
        // A11.16: a broken unit held in Melee that can withdraw must attempt it, unless Disrupted or a Guard.
        var withdrawals = Map(arguments, "withdrawals") ?? new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (unitId, destination) in withdrawals)
        {
            // A11.21, A4.43 (ruling R5.5): a withdrawing unit carries no more than its IPC; dropping the SW beyond it is not built.
            if (state.Unit(unitId) is { Status: InstanceStatus.Active } laden && Laden(state, laden))
            {
                return Refused(scope, label, expected, $"play.cc-withdrawal-portage: {unitId} carries more than its IPC and may not withdraw with it; dropping a SW is not built (A11.21, A4.43)");
            }

            if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location != location || !Is(unit, Conditions.Melee)
                || !BoardLocation.TryParse(destination, out var to) || !WithdrawalDestinations(state, unit, location).Contains(to))
            {
                return Refused(scope, label, expected, $"play.cc-withdrawal: {unitId} withdraws only from Melee, to an ADJACENT Location it could advance into that holds no Known enemy unit (A11.2, A11.21)");
            }
        }

        if (state.Units.FirstOrDefault(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id)?.Location == location && MustWithdraw(state, unit, location)
            && !withdrawals.ContainsKey(unit.Id)) is { } broken)
        {
            return Refused(scope, label, expected, $"play.cc-withdraw-required: {broken.Id} is broken in Melee and must attempt to withdraw (A11.16)");
        }

        // A11.21, A4.72 (ruling R5.5): a withdrawal an advance could make only by becoming CX makes the unit CX.
        var tiring = withdrawals.Where(item => state.Unit(item.Key) is { } unit && WithdrawalTires(state, unit, location, BoardLocation.Parse(item.Value)))
            .Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
        // A11.22 (ruling R14.8): each Infiltration destination is one a withdrawal could reach.
        var infiltrations = Map(arguments, "infiltrations") ?? new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (unitId, destination) in infiltrations)
        {
            if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || state.Location(unit.Id)?.Location != location
                || !BoardLocation.TryParse(destination, out var to) || !WithdrawalDestinations(state, unit, location).Contains(to))
            {
                return Refused(scope, label, expected, $"play.cc-infiltration: {unitId} may infiltrate only to an ADJACENT Location a withdrawal could reach (A11.22, A11.21)");
            }
        }

        tiring.UnionWith(infiltrations.Where(item => state.Unit(item.Key) is { } unit && WithdrawalTires(state, unit, location, BoardLocation.Parse(item.Value)))
            .Select(item => item.Key));

        // J2.31 (ruling R14.1): Hand-to-Hand is declared only where an SSR allows it, with the Location's first round.
        var handToHand = arguments.TryGetProperty("handToHand", out var hand) && hand.ValueKind == JsonValueKind.True;
        if (handToHand && !state.SpecialRules.Contains(HandToHandRule, StringComparer.Ordinal))
        {
            return Refused(scope, label, expected, "play.cc-hand-to-hand: Hand-to-Hand CC is declared only where an SSR allows it (J2.31, G1.64)");
        }

        // Referee, pass 14 (A25.43, G1.64): the ATTACKER declares it with the Location's first round other than the prisoners', unless it was ambushed.
        var begun = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (handToHand && ((Text(arguments, "round", out var handRound) && handRound == CloseCombatFacts.PrisonersRound)
            || begun?.Rounds.Any(item => item != CloseCombatResolved.PrisonersRound) == true || (begun?.Ambusher is { } ambushed && ambushed != state.PhasingSide)))
        {
            return Refused(scope, label, expected,
                "play.cc-hand-to-hand: the ATTACKER declares Hand-to-Hand with the Location's first round, not in the prisoners' round, and not after being ambushed (J2.31, A25.43)");
        }

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        var (facts, reason) = LiveCloseCombat.FromState(state, location, terrain, attacks, stacking, withdrawals, Text(arguments, "round", out var round) ? round : null,
            infiltrations, handToHand);
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
                if (resolution.Reasons is not [{ } missing] || !missing.StartsWith("asl.a1.cc.roll-missing:", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("The Close Combat package left a round it had accepted undecided: " + string.Join("; ", resolution.Reasons));
                }

                var key = missing["asl.a1.cc.roll-missing:".Length..];
                var split = key.IndexOf(':', StringComparison.Ordinal);
                var (kind, rest) = (key[..split], key[(split + 1)..]);
                string[] selected = kind == "randomSelection" ? rest[(rest.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',') : [];
                if (kind == "leaderStack")
                {
                    selected = rest[(rest.IndexOf(':', StringComparison.Ordinal) + 1)..].Split(',');
                }

                var (count, purpose) = kind switch
                {
                    "attack" => (2, "cc-attack"),
                    "escapeNtc" => (2, "cc-escape-ntc"),
                    "leaderStack" => (selected.Length, "cc-leader-stack"),
                    "randomSelection" => (selected.Length, "cc-random-selection"),
                    "woundSeverity" => (1, "cc-wound-severity"),
                    "leaderCreation" => (1, "cc-leader-creation"),
                    _ => (1, "cc-weapon-loss"),
                };
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
                            (map, pair) => With(map, rest[..rest.IndexOf(':', StringComparison.Ordinal)] + ":" + pair.id, drawn.Values[pair.index]))
                    },
                    "woundSeverity" => rolls with { WoundSeverity = With(rolls.WoundSeverity, rest, drawn.Values[0]) },
                    "leaderCreation" => rolls with { LeaderCreation = With(rolls.LeaderCreation, rest, drawn.Values[0]) },
                    "escapeNtc" => rolls with { EscapeNtc = With(rolls.EscapeNtc, rest, (IReadOnlyList<int>)drawn.Values) },
                    "leaderStack" => rolls with
                    {
                        LeaderStack = selected.Select((id, index) => (id, index)).Aggregate(rolls.LeaderStack,
                            (map, pair) => With(map, rest[..rest.IndexOf(':', StringComparison.Ordinal)] + ":" + pair.id, drawn.Values[pair.index]))
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

            return events;
        }

        var described = attacks.Count == 0 ? "no attacks"
            : string.Join("; ", attacks.Select(item => $"{string.Join(", ", item.Attackers!)} {(item.Capture == true ? "attempt to capture" : "attack")} {string.Join(", ", item.Defenders!)}"
                + (item.Director is { } director ? $", directed by {director}" : string.Empty)));
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [$"play.cc: {facts.Round} round in {location}{(facts.HandToHand == true ? ", Hand-to-Hand" : string.Empty)}: {described} (A11.11, A11.12)"])
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
        : [.. Neighbors(state, from).Where(to => InfantryStep(state, from, to).Entry is { AllMf: false } entry
            && DifficultAdvance(state, unit, entry.HalfMf) is { } difficult && !(difficult && Is(unit, Conditions.Cx))
            && !state.At(to).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured)))];

    /// <summary>Whether a unit carries more PP than its IPC (A4.42): three for a MMC, one for a SMC, none for a wounded SMC, one less while CX.</summary>
    private bool Laden(GameState state, UnitInstance unit)
    {
        var ipc = (vocabulary.IsA(unit.Kind, "asl:smc") ? (Is(unit, Conditions.Wounded) ? 0 : 1) : 3) - (Is(unit, Conditions.Cx) ? 1 : 0);
        return Portage(state, unit) is { } carried && carried.Sum() > Math.Max(ipc, 0);
    }

    /// <summary>Whether a withdrawal to a Location makes the unit CX (A11.21, A4.72; ruling R5.5).</summary>
    private bool WithdrawalTires(GameState state, UnitInstance unit, BoardLocation from, BoardLocation to) =>
        InfantryStep(state, from, to).Entry is { AllMf: false } entry && DifficultAdvance(state, unit, entry.HalfMf) == true;

    /// <summary>A11.16: a broken unit held in Melee, not Disrupted and not a Guard, must attempt to withdraw when it can.</summary>
    private bool MustWithdraw(GameState state, UnitInstance unit, BoardLocation at) =>
        Is(unit, Conditions.Broken) && Is(unit, Conditions.Melee) && !Is(unit, Conditions.Disrupted) && !Is(unit, Conditions.Captured)
        && !IsGuard(state, unit) && WithdrawalDestinations(state, unit, at).Count > 0;

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
            if (state.CloseCombats.Any(item => item.Location == location))
            {
                continue;
            }

            var here = active.Where(unit => state.Location(unit.Id)!.Location == location).ToArray();

            // CC with a crew (ruling R24.3) or a vehicle (ruling R25.10) is not reviewed, so it cannot be required.
            if (here.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew") || LiveFire.IsVehicle(unit)))
            {
                continue;
            }

            var berserk = here.FirstOrDefault(unit => Is(unit, Conditions.Berserk) && here.Any(other => other.Side != unit.Side && KnownEnemy(other)));
            var reinforcing = here.Any(unit => Is(unit, Conditions.Melee))
                ? here.FirstOrDefault(unit => state.Advances.Any(item => item.Unit == unit.Id && item.To == location) && !Is(unit, Conditions.Broken))
                : null;
            if ((berserk ?? reinforcing) is not { } unit)
            {
                continue;
            }

            var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
            if (LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is not null && MandatoryAttackDecidable(state, location, terrain, unit, here))
            {
                return berserk is not null
                    ? $"play.cc-required: {unit.Id} is berserk with a Known enemy unit in {location}, so it attacks in CC before the CCPh ends (A15.43)"
                    : $"play.cc-required: {unit.Id} advanced into the Melee in {location}, so it attacks in CC before the CCPh ends (A11.15)";
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
            var kept = facts?.Units!.Where(item => item.UnitId == unit.Id || item.Side != unit.Side || item.Captured == true).ToList();
            kept?.AddRange(facts!.Units!.Where(item => item.Side == unit.Side && item.UnitId != unit.Id && kept.Any(prisoner => prisoner.GuardId == item.UnitId)));
            if (facts is not null && ScenarioA1CloseCombatCalculator.Precheck(facts with { Units = kept }, CloseCombatReference.Value) is { Count: 0 })
            {
                return true;
            }
        }

        return false;
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
        return state.CloseCombats.FirstOrDefault(item => item.Location == location) is { Ambusher: { } ambusher } entry
            ? entry.Rounds.Count == 0 || ambusherAgain ? [ambusher] : [.. sides.Where(side => side != ambusher)]
            : sides;
    }

    /// <summary>Whether a Location's Ambush drs are due (A11.4): no CC there yet, and the Close Combat package allows an Ambush.</summary>
    public bool AmbushDue(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != "ccph" || state.CloseCombats.Any(item => item.Location == location))
        {
            return false;
        }

        // Ruling R11.16: no Ambush dr is made in a Location holding a vehicle.
        if (state.At(location).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)))
        {
            return false;
        }

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        return LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is { Units: { } units } && ScenarioA1CloseCombatCalculator.AmbushPossible(terrain, units);
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
        foreach (var open in state.CloseCombats.Where(item => !item.Closed))
        {
            // Ruling R11.16 (table-player finding): a Location holding a vehicle has no Ambush; its sides attack in turn.
            due.Add(open.Next is { } next ? $"{open.Location}: the {next} side attacks or passes (A11.31)"
                : open.Ambusher is null ? $"{open.Location}: its round after the Ambush drs (A11.12)"
                : $"{open.Location}: more attacks by the {open.Ambusher} side, then the ambushed side's round, which may declare no attacks (A11.3, A11.32)");
        }

        var locations = state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id) is not null)
            .Select(unit => state.Location(unit.Id)!.Location).Distinct().OrderBy(item => item.ToString(), StringComparer.Ordinal).ToArray();
        due.AddRange(locations.Where(location => AmbushDue(state, location)).Select(location => $"{location}: the Ambush drs (A11.4)"));
        if (CloseCombatRequired(state) is { } required)
        {
            due.Add(required);
        }

        due.AddRange(state.Units.Where(unit => unit.Status == InstanceStatus.Active && state.Location(unit.Id) is { } held
                && !state.CloseCombats.Any(item => item.Location == held.Location) && MustWithdraw(state, unit, held.Location))
            .Select(unit => $"{state.Location(unit.Id)!.Location}: {unit.Id} is broken in Melee and must attempt to withdraw (A11.16)"));
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
    /// Why a berserk unit may not step into a Location (A15.431, A15.432, A20.55): it holds prisoners (CC with prisoners present is pass 14), a concealed
    /// or hidden enemy unit or a Dummy (concealment in CC is not reviewed), or, as the charge's target, only a lone enemy SMC (an
    /// Infantry OVR, not reviewed). Null when the step is allowed.
    /// </summary>
    private string? ChargeBarred(GameState state, string side, BoardLocation to)
    {
        var there = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        var enemies = there.Where(unit => unit.Side != side).ToArray();

        // Ruling R10.15: a charge into concealed units reveals them, one into prisoners enters, and one onto a lone SMC is an Infantry OVR; a Gun's
        // crew in CC stays unreviewed (ruling R24.3).
        if (enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))
        {
            return $"play.berserk-crew: a charge into {to}, which holds a Gun's crew, is CC with a crew, which is not reviewed (C11, R24.3); the charge ends in place (ruling R30.5)";
        }

        if (enemies.FirstOrDefault(LiveFire.IsVehicle) is { } vehicle)
        {
            return $"play.berserk-vehicle: a charge into {to}, which holds the enemy vehicle {vehicle.Id}, is CC or OVR against a vehicle (A11.5, D7), which is not reviewed; the charge ends in place (rulings R25.3, R30.5)";
        }

        return null;
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
        if (state.CloseCombats.FirstOrDefault(item => item.Location == location) is not { Rounds.Count: > 0 })
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
            var id = $"{attemptId}-leader-{leader.Attack.ToString(CultureInfo.InvariantCulture)}";
            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Broken] = ConditionState.False,
                [Conditions.Pinned] = ConditionState.False,
                [Conditions.Wounded] = ConditionState.False,
                [Conditions.Concealed] = ConditionState.False,
                [Conditions.Hidden] = ConditionState.False,
            };
            if (state.Unit(leader.StackedWith) is { } mmc && GameState.Condition(mmc, Conditions.Fanatic) == ConditionState.True)
            {
                conditions[Conditions.Fanatic] = ConditionState.True;
            }

            // A11.22, A18.12 (ruling R14.8): a leader created by an attack whose MMC infiltrates goes with it.
            var placedAt = resolution.Effects.FirstOrDefault(item => item.UnitId == leader.StackedWith)?.InfiltratedTo is { } gone ? BoardLocation.Parse(gone) : location;
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

            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
            if (effect.Armed == true)
            {
                conditions[Conditions.Unarmed] = ConditionState.False;
            }

            // A11.19, A12.14 (ruling R14.2): the unit loses its "?".
            if (effect.ConcealmentLost == true)
            {
                conditions[Conditions.Concealed] = ConditionState.False;
                conditions[Conditions.Hidden] = ConditionState.False;
            }

            // A20.551 (ruling R14.6): an Unarmed MMC is rearmed as a Conscript MMC of its size and nationality.
            if (effect.RearmedAs is { } rearmed)
            {
                var conscript = CloseCombatReference.Value.Definitions[rearmed];
                var armed = unit.Conditions.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
                foreach (var (name, value) in conditions)
                {
                    armed[name] = value;
                }

                armed[Conditions.Unarmed] = ConditionState.False;

                // Table player, pass 14: a rearmed prisoner is no longer captured.
                if (effect.Escaped == true)
                {
                    armed[Conditions.Captured] = ConditionState.False;
                }
                yield return ("lineage", new LineageRecorded(LineageAction.Replaced, [unit.Id],
                    [new NewInstance($"{attemptId}-{unit.Id}", conscript.Kind, conscript.Id, unit.Side, unit.Position, null, armed)]));
                continue;
            }

            if (effect.Wounded && GameState.Condition(unit, Conditions.Wounded) != ConditionState.True)
            {
                conditions[Conditions.Wounded] = ConditionState.True;
            }

            if (effect.BerserkEnded == true)
            {
                conditions[Conditions.Berserk] = ConditionState.False;
            }

            if (effect.FinalDefinitionId != effect.DefinitionId)
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
        foreach (var effect in resolution.Effects.Where(item => (item.WithdrewTo ?? item.InfiltratedTo) is not null && item.RearmedAs is null))
        {
            yield return ("instance-moved", new InstanceMoved(effect.UnitId, new MapPosition(BoardLocation.Parse((effect.WithdrewTo ?? effect.InfiltratedTo)!))));
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
        foreach (var weapon in effect.CapturedHalf == true ? Enumerable.Empty<EquipmentInstance>()
            : state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            yield return ("equipment-transferred", new EquipmentTransferred(weapon.Id, null, new MapPosition(at)));
        }

        var captive = unit.Id;
        if (effect.CapturedHalf == true)
        {
            var half = CloseCombatReference.Value.Definitions[ScenarioA1FireReference.HalfSquadOf(unit.Definition!.Definition)!];
            captive = $"{attemptId}-{unit.Id}-a";
            var free = $"{attemptId}-{unit.Id}-b";
            yield return ("lineage", new LineageRecorded(LineageAction.Deployed, [unit.Id],
                [new NewInstance(captive, half.Kind, half.Id, unit.Side, unit.Position, null, unit.Conditions),
                    new NewInstance(free, half.Kind, half.Id, unit.Side, unit.Position, null, unit.Conditions)]));

            // Referee, pass 14: the HS that is not captured keeps the squad's SW (A20.24 takes only the captured unit's).
            foreach (var weapon in state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding?.Holder == unit.Id).OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                yield return ("equipment-transferred", new EquipmentTransferred(weapon.Id, new Holding(free, HoldingRole.Possessed), null));
            }
        }

        yield return ("conditions-changed", new ConditionsChanged(captive, new Dictionary<string, ConditionState>
        {
            [Conditions.Broken] = ConditionState.False,
            [Conditions.Disrupted] = ConditionState.False,
            [Conditions.Pinned] = ConditionState.False,
            [Conditions.DesperationMorale] = ConditionState.False,
            [Conditions.Concealed] = ConditionState.False,
            [Conditions.Hidden] = ConditionState.False,
            [Conditions.Melee] = ConditionState.False,
            [Conditions.Unarmed] = ConditionState.True,
        }));
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
            return Refused(scope, label, expected, $"play.no-surrender: {unitId} has not surrendered");
        }

        // A20.21, A20.51 (ruling R14.5): the unit goes to a captor with Guard capacity; when none has any, it is freed as Unarmed.
        var able = pending.Captors.Where(id => state.Unit(id) is { Status: InstanceStatus.Active } captor && state.Unit(unitId) is { } surrendered
            && GuardLoad(state, captor) + UnitSize(surrendered) <= 5 * UnitSize(captor)).ToArray();
        var free = arguments.TryGetProperty("free", out var freeing) && freeing.ValueKind == JsonValueKind.True;
        if (free || (!reject && able.Length > 0 && !able.Contains(captorId, StringComparer.Ordinal) && pending.Captors.Contains(captorId, StringComparer.Ordinal)))
        {
            if (able.Length > 0)
            {
                return Refused(scope, label, expected, $"play.captor-capacity: {string.Join(", ", able)} can guard {unitId}, so it is taken by one of them (A20.21, A20.51)");
            }

            if (state.Unit(unitId) is not { } loose || state.Location(loose.Id) is not { } looseAt)
            {
                return Refused(scope, label, expected, $"play.no-surrender: {unitId} has not surrendered");
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
                [$"play.freed-unarmed: no captor can guard {unitId}, so it abandons its SW and is freed as an Unarmed unit (A20.21, A20.51)"]);
        }

        // A20.3 (ruling R5.6): the captor's side may reject the surrender, eliminating the unit and facing its side with No Quarter.
        if (reject)
        {
            var side = state.Unit(unitId)?.Side;
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [Event(scope, attemptId, 1, expected, "surrender-rejected", new SurrenderRejected(unitId), ScenarioA1FirePackage.Identity.ToString(), null, [pending.Event])],
                [$"play.no-quarter: the surrender of {unitId} is rejected and it is eliminated; the {side} side is faced with No Quarter from now on: its units never surrender (A20.3, A15.5)"]);
        }

        if (!pending.Captors.Contains(captorId, StringComparer.Ordinal) || state.Unit(captorId) is not { Status: InstanceStatus.Active } captor
            || state.Location(captor.Id) is not { } guardAt || state.Unit(unitId) is not { } prisoner || state.Location(prisoner.Id) is not { } prisonerAt)
        {
            return Refused(scope, label, expected, $"play.captor: {unitId} surrenders to one of {string.Join(", ", pending.Captors)} (A15.5)");
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

        events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed", new ConditionsChanged(prisoner.Id, new Dictionary<string, ConditionState>
        {
            [Conditions.Broken] = ConditionState.False,
            [Conditions.Disrupted] = ConditionState.False,
            [Conditions.Pinned] = ConditionState.False,
            [Conditions.DesperationMorale] = ConditionState.False,
        }), package, null, [pending.Event]));
        events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-captured", new InstanceCaptured(prisoner.Id, captor.Id), package, null, [pending.Event]));
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.capture: {prisoner.Id} surrenders to {captor.Id} and is its prisoner in {guardAt.Location} (A15.5, A20.5)"]);
    }

    /// <summary>
    /// Whether a Known enemy unit is in LOS of a Location (A15.44): an enemy unit neither concealed, hidden, a Dummy, nor a
    /// prisoner (A20.4), in the Location or with a clear LOS to it. Null when an LOS cannot be read.
    /// </summary>
    public bool? KnownEnemyInLos(GameState state, string side, BoardLocation at)
    {
        var locations = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && KnownEnemy(unit))
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().ToArray();
        if (locations.Contains(at))
        {
            return true;
        }

        var unknown = false;
        foreach (var location in locations)
        {
            switch (Los(state, location, at)?.Status)
            {
                case LosStatus.Clear:
                    return true;
                case LosStatus.Blocked:
                    break;
                default:
                    unknown = true;
                    break;
            }
        }

        return unknown ? null : false;
    }

    private static bool KnownEnemy(UnitInstance unit) => unit.Kind != UnitKinds.Dummy && !Is(unit, Conditions.Concealed) && !Is(unit, Conditions.Hidden)
        && !Is(unit, Conditions.Captured);

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

        var adjacent = state.Units.Where(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && other.Definition is not null
                && (KnownEnemy(other) || (revealed?.Contains(other.Id) == true && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured)))
                && !Is(other, Conditions.Broken) && !Is(other, Conditions.Berserk) && !Is(other, Conditions.Melee) && !Is(other, Conditions.Unarmed)
                && (vocabulary.IsA(other.Kind, "asl:mmc") || vocabulary.IsA(other.Kind, "asl:smc"))
                && state.Location(other.Id)?.Location is { } there && IsAdjacent(state, there, at)).ToArray();

        // A20.21, A20.51 (ruling R14.5): the captors with Guard capacity; with none, every captor, and the captor's side frees the unit as Unarmed.
        string[] guards = [.. adjacent.Where(other => GuardLoad(state, other) + UnitSize(unit) <= 5 * UnitSize(other)).Select(other => other.Id).Order(StringComparer.Ordinal)];
        return guards.Length > 0 ? guards : [.. adjacent.Select(other => other.Id).Order(StringComparer.Ordinal)];
    }

    /// <summary>The US# of a unit's prisoners (A20.51).</summary>
    private int GuardLoad(GameState state, UnitInstance guard) =>
        state.Units.Where(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == guard.Id).Sum(UnitSize);

    /// <summary>A unit's US# (A1.6, p. 45; A20.51): a squad 3, a HS or crew 2, a SMC 1.</summary>
    private int UnitSize(UnitInstance unit) => vocabulary.IsA(unit.Kind, "asl:squad") ? 3 : vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew") ? 2 : 1;

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

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431): the first step of each
    /// shortest route in MF to that unit's Location, over reviewed terrain, around other Locations holding enemy units. Equidistant
    /// targets are the ATTACKER's choice, so their steps are all allowed. With no Known enemy unit in LOS, the charge keeps to the
    /// Location it charged (<paramref name="previous"/>). Terrain the game refuses to enter is on no route (ruling R10.15); a route never
    /// counts Bypass, a recorded deviation.
    /// </summary>
    private (IReadOnlyDictionary<BoardLocation, (BoardLocation Target, int HalfMf)> Steps, BoardLocation? Target, string? Undecided) ChargeSteps(GameState state,
        string side, BoardLocation from, BoardLocation? previous)
    {
        var enemies = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && KnownEnemy(unit))
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct().ToArray();
        if (enemies.Contains(from))
        {
            return (new Dictionary<BoardLocation, (BoardLocation, int)>(), from, null);
        }

        var inLos = enemies.Where(location => Los(state, from, location) is { Status: LosStatus.Clear }).ToArray();
        BoardLocation[] targets;

        // A15.431: a charge keeps to its Location until a closer Known enemy unit comes into its LOS (ruling R30.5).
        if (previous is not null && HexDistance(state, from, previous) is { } kept)
        {
            var closer = inLos.Select(location => (location, Distance: HexDistance(state, from, location))).Where(item => item.Distance < kept).ToArray();
            inLos = [.. closer.Select(item => item.location)];
        }

        if (inLos.Length > 0)
        {
            var distances = inLos.Select(location => (location, Distance: HexDistance(state, from, location))).ToArray();
            if (distances.Any(item => item.Distance is null))
            {
                return (new Dictionary<BoardLocation, (BoardLocation, int)>(), null, "play.charge-undecided: a distance to a Known enemy unit cannot be read");
            }

            var nearest = distances.Min(item => item.Distance!.Value);
            targets = [.. distances.Where(item => item.Distance == nearest).Select(item => item.location)];
        }
        else if (previous is not null)
        {
            targets = [previous];
        }
        else
        {
            return (new Dictionary<BoardLocation, (BoardLocation, int)>(), null, null);
        }

        var steps = new Dictionary<BoardLocation, (BoardLocation Target, int HalfMf)>();
        var undecided = new List<string>();
        foreach (var target in targets)
        {
            var blocked = enemies.Where(location => location != target).ToHashSet();
            // Ruling R10.15: every entry the game allows is costed, so the shortest route is decided; terrain it refuses is on no route.
            var exact = RouteCosts(state, target, blocked, lowerBound: false);
            if (!exact.TryGetValue(from, out var best))
            {
                undecided.Add($"play.charge-no-route: no route the game allows leads from {from} to {target}; the charge ends in place (A15.431)");
                continue;
            }

            foreach (var next in Neighbors(state, from))
            {
                if (EntryCost(state, from, next) is { } cost && exact.TryGetValue(next, out var rest) && cost + rest == best && !steps.ContainsKey(next))
                {
                    // A step the model cannot take leaves the charge undecided, so it may end in place (ruling R30.5).
                    if (ChargeBarred(state, side, next) is { } barred)
                    {
                        undecided.Add(barred);
                        continue;
                    }

                    steps[next] = (target, cost);
                }
            }
        }

        return steps.Count == 0 && undecided.Count > 0
            ? (steps, targets[0], undecided[0])
            : (steps, targets.Length == 1 ? targets[0] : null, null);
    }

    /// <summary>
    /// The least half MF from each Location to a target Location (A15.431: the shortest route in MF), by Dijkstra from the target
    /// over entry costs; Locations holding other enemy units are not passed through. With <paramref name="lowerBound"/> an entry
    /// the review does not decide costs one MF, the least any entry costs, so the result bounds every route from below.
    /// </summary>
    private Dictionary<BoardLocation, int> RouteCosts(GameState state, BoardLocation target, HashSet<BoardLocation> blocked, bool lowerBound)
    {
        var best = new Dictionary<BoardLocation, int> { [target] = 0 };
        var queue = new PriorityQueue<BoardLocation, int>();
        queue.Enqueue(target, 0);
        while (queue.TryDequeue(out var node, out var cost))
        {
            if (cost > best[node] || (node != target && blocked.Contains(node)))
            {
                continue;
            }

            foreach (var neighbor in Neighbors(state, node))
            {
                // Entering node from neighbor.
                var entry = EntryCost(state, neighbor, node) ?? (lowerBound ? 2 : (int?)null);
                if (entry is not { } value)
                {
                    continue;
                }

                var total = cost + value;
                if (total < best.GetValueOrDefault(neighbor, int.MaxValue))
                {
                    best[neighbor] = total;
                    queue.Enqueue(neighbor, total);
                }
            }
        }

        return best;
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
            var (steps, target, undecided) = ChargeSteps(state, unit.Side, state.Location(unit.Id)!.Location,
                state.Movement?.Members.Contains(unit.Id) == true ? state.Movement.Charge : null);
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
    /// The berserk units of the phasing side that must still charge this MPh (A15.43): not held in Melee, not done moving, not in a
    /// Location with a Known enemy unit, and with a step they can afford on a decided route.
    /// </summary>
    private IReadOnlyList<UnitInstance> MustCharge(GameState state) =>
        [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && Is(unit, Conditions.Berserk) && !Is(unit, Conditions.Melee)
            && !unit.MovementEnded && state.Location(unit.Id) is { } at
            && ChargeSteps(state, unit.Side, at.Location, state.Movement?.Members.Contains(unit.Id) == true ? state.Movement.Charge : null) is { Steps.Count: > 0 } charge
            && MfAllotment(state, unit, unit.DoubleTimeMf, Is(unit, Conditions.Cx)) is { } allowance
            && charge.Steps.Values.Any(step => (allowance * 2) - (unit.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0) >= step.HalfMf))];
}
