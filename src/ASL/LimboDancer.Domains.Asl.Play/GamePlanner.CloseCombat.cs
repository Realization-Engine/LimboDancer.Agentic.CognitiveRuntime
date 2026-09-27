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
        if (units.FirstOrDefault(unit => new[] { Conditions.Broken, Conditions.Pinned, Conditions.Berserk, Conditions.Melee, Conditions.Captured, Conditions.Concealed, Conditions.Hidden, "asl:ti" }
            .Any(condition => Is(unit!, condition)) || unit!.MovementEnded) is { } barred)
        {
            return Refused(scope, label, expected,
                $"play.advance-unit: {barred.Id} is broken, pinned, TI, berserk, in Melee, captured, concealed, or has advanced this APh (A4.7, A15.431, A11.15)");
        }

        if (units.FirstOrDefault(unit => Mans(state, unit!)) is { } gunner)
        {
            return Refused(scope, label, expected, $"play.advance-crew-mans-gun: {gunner.Id} mans a Gun; abandoning or moving a Gun is not reviewed (C10, A21.13)");
        }

        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null)
        {
            return Refused(scope, label, expected, $"play.advance-step: {to} is not an ADJACENT Location the map reads");
        }

        if (fromRead.Hex.BaseLevel + fromRead.Level.Level != toRead.Hex.BaseLevel + toRead.Level.Level || to.Level != 0
            || crossed.HexsideTerrain is not null || crossed.Cliff || crossed.Slope)
        {
            return Refused(scope, label, expected, "play.advance-terrain: level changes and hexside terrain are not reviewed (ruling R22.3)");
        }

        if (TerrainKey(toRead) is not { } terrain || !EntryHalfMf.TryGetValue(terrain, out var halfMf))
        {
            return Refused(scope, label, expected, $"play.advance-terrain: {toRead.Level.Terrain?.Name ?? "the terrain"} is not a reviewed entry (ruling R22.3)");
        }

        if (crossed.Terrain?.IsRoad == true)
        {
            halfMf = 2;
        }

        // A4.72: an advance costing at least four MF, or all of the unit's MF, makes it CX, which is not built (ruling R29.2).
        foreach (var unit in units)
        {
            if (Experience.MoveAllowance(state, unit!, catalogs, vocabulary) is not { } allowance || halfMf >= 2 * Math.Min(4, allowance))
            {
                return Refused(scope, label, expected, $"play.advance-difficult-terrain: the advance would make {unit!.Id} CX, which is not reviewed (A4.72)");
            }
        }

        // The enemy units there are Known (A11.19: concealment in CC is not reviewed), and none is a prisoner (A20.55).
        var enemies = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != state.PhasingSide).ToArray();
        if (enemies.Any(unit => unit.Kind == UnitKinds.Dummy || Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden) || Is(unit, Conditions.Captured)))
        {
            return Refused(scope, label, expected, "play.advance-concealed: an advance into concealed enemy units or prisoners is not reviewed (A11.19, A20.55)");
        }

        if (state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && Is(unit, Conditions.Captured)))
        {
            return Refused(scope, label, expected, "play.advance-prisoners: a Location holding prisoners is not reviewed for CC (A20.55)");
        }

        // A crew in CC, and the Gun it mans, are not reviewed (ruling R24.3).
        if (enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))
        {
            return Refused(scope, label, expected, "play.advance-crew: CC with a Gun's crew is not reviewed (C11, ruling R24.3)");
        }

        // A20.53, A20.55: a Guard's prisoners advance with it, and CC in a Location holding prisoners is not reviewed.
        if (enemies.Length > 0 && units.FirstOrDefault(unit => IsGuard(state, unit!)) is { } guard)
        {
            return Refused(scope, label, expected, $"play.advance-guard: {guard.Id} guards prisoners, who would enter CC with it, which is not reviewed (A20.53, A20.55)");
        }

        // A5.1: three squad-equivalents and four SMC per side; overstacking is not reviewed.
        var side = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && !Is(unit, Conditions.Captured))
            .Concat(units.Select(unit => unit!)).Distinct().ToArray();
        if (Overstacked(side))
        {
            return Refused(scope, label, expected, "play.advance-overstacked: the advance would overstack the Location, which is not reviewed (A5.1, A5.12)");
        }

        var summary = $"play.advance: {string.Join(", ", ids)} advance into {to} ({terrain})" + (enemies.Length > 0 ? $", with {string.Join(", ", enemies.Select(item => item.Id))}: CC follows (A3.7)" : string.Empty);
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "advanced", new AdvanceMoved(ids, to), ScenarioA1CloseCombatPackage.Identity.ToString(), null)], [summary]);
    }

    /// <summary>A5.1, A5.5: more than three squad-equivalents (two HS or crews each) or more than four SMC of one side.</summary>
    private bool Overstacked(IEnumerable<UnitInstance> units)
    {
        var list = units.ToArray();
        var squads = list.Count(unit => vocabulary.IsA(unit.Kind, "asl:squad"))
            + (list.Count(unit => vocabulary.IsA(unit.Kind, "asl:half-squad") || vocabulary.IsA(unit.Kind, "asl:crew")) / 2m);
        return squads > 3 || list.Count(unit => vocabulary.IsA(unit.Kind, "asl:smc")) > 4;
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

            events.Add(Event(scope, attemptId, events.Count + 1, expected, "ambush-rolled", new AmbushRolled(location, rollIds, resolution.Ambusher,
                JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)), package, null));
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
            });
        }

        // A11.14: the SMC stacking is declared once, before either side's attacks; the ambushed side's round keeps the first round's.
        var stacking = FirstRoundStacking(existing, state, location) ?? Map(arguments, "stacking");

        // A11.2, A11.21: a unit held in Melee may withdraw to an ADJACENT Location it could advance into that holds no Known enemy unit;
        // A11.16: a broken unit held in Melee that can withdraw must attempt it, unless Disrupted or a Guard.
        var withdrawals = Map(arguments, "withdrawals") ?? new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (unitId, destination) in withdrawals)
        {
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

        var terrain = ReadLocation(state, location) is { } read ? TerrainKey(read) : null;
        var (facts, reason) = LiveCloseCombat.FromState(state, location, terrain, attacks, stacking, withdrawals, Text(arguments, "round", out var round) ? round : null);
        if (facts is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        var reference = CloseCombatReference.Value;
        var precheck = ScenarioA1CloseCombatCalculator.Precheck(facts, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, ["play.cc-refused: the Close Combat package does not decide every outcome of this round", .. precheck]);
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
                var (count, purpose) = kind switch
                {
                    "attack" => (2, "cc-attack"),
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
                    _ => rolls with { WeaponLoss = With(rolls.WeaponLoss, rest, drawn.Values[0]) },
                };
            }

            var recordId = EventId(attemptId, events.Count + 1);
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "close-combat-resolved",
                new CloseCombatResolved(location, facts.Round!, [.. attacks.SelectMany(item => item.Attackers!)], [.. attacks.SelectMany(item => item.Defenders!)], rollIds,
                    JsonSerializer.SerializeToElement(facts, LiveFire.Json), JsonSerializer.SerializeToElement(resolution, LiveFire.Json)), package, null));
            foreach (var (type, payload) in CloseCombatEffects(state, location, resolution, attemptId))
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [recordId]));
            }

            return events;
        }

        var described = attacks.Count == 0 ? "no attacks"
            : string.Join("; ", attacks.Select(item => $"{string.Join(", ", item.Attackers!)} attack {string.Join(", ", item.Defenders!)}"
                + (item.Director is { } director ? $", directed by {director}" : string.Empty)));
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [$"play.cc: {facts.Round} round in {location}: {described} (A11.11, A11.12)"])
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
    /// concealed one is not reviewed). A11.21 allows the withdrawal even where it makes the unit CX, which is not built, so no CX
    /// counter is placed (ruling R29.11).
    /// </summary>
    public IReadOnlyList<BoardLocation> WithdrawalDestinations(GameState state, UnitInstance unit, BoardLocation from) =>
        [.. Neighbors(state, from).Where(to => Step(state, from, to) is ({ } _, { } toRead, true, { Cliff: false }) && TerrainKey(toRead) is { } terrain
            && EntryHalfMf.ContainsKey(terrain)
            && !state.At(to).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side && !Is(other, Conditions.Captured)))];

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

            // CC with a crew is not reviewed (ruling R24.3), so it cannot be required.
            if (here.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))
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
            if (LiveCloseCombat.AmbushFromState(state, location, terrain).Facts is not null)
            {
                return berserk is not null
                    ? $"play.cc-required: {unit.Id} is berserk with a Known enemy unit in {location}, so it attacks in CC before the CCPh ends (A15.43)"
                    : $"play.cc-required: {unit.Id} advanced into the Melee in {location}, so it attacks in CC before the CCPh ends (A11.15)";
            }
        }

        return null;
    }

    /// <summary>The ADJACENT ground-level Locations of a Location, such as an advance may enter (A4.7).</summary>
    public IReadOnlyList<BoardLocation> AdjacentLocations(GameState state, BoardLocation at)
    {
        ArgumentNullException.ThrowIfNull(state);
        return [.. Neighbors(state, at).Distinct()];
    }

    /// <summary>Whether a Location's Ambush drs are due (A11.4): no CC there yet, and the Close Combat package allows an Ambush.</summary>
    public bool AmbushDue(GameState state, BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != "ccph" || state.CloseCombats.Any(item => item.Location == location))
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
            due.Add(open.Ambusher is null ? $"{open.Location}: its round after the Ambush drs (A11.12)"
                : $"{open.Location}: more attacks by the {open.Ambusher} side, then the ambushed side's round (A11.3, A11.32)");
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
    /// Why a berserk unit may not step into a Location (A15.431, A15.432, A20.4): it holds prisoners (Massacre is not built), a concealed
    /// or hidden enemy unit or a Dummy (concealment in CC is not reviewed), or, as the charge's target, only a lone enemy SMC (an
    /// Infantry OVR, not reviewed). Null when the step is allowed.
    /// </summary>
    private string? ChargeBarred(GameState state, string side, BoardLocation to)
    {
        var there = state.At(to).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active).ToArray();
        var enemies = there.Where(unit => unit.Side != side).ToArray();
        if (enemies.Any(unit => !KnownEnemy(unit) && !Is(unit, Conditions.Captured)) || there.Any(unit => Is(unit, Conditions.Captured))
            || enemies.Any(unit => vocabulary.IsA(unit.Kind, "asl:crew")))
        {
            return $"play.berserk-concealed: a charge into {to}, which holds concealed enemy units, prisoners, or a Gun's crew, is not reviewed (A15.431, A20.4, R24.3); the charge ends in place (ruling R30.5)";
        }

        return enemies is [{ } lone] && vocabulary.IsA(lone.Kind, "asl:smc")
            ? $"play.berserk-ovr: a charge onto the lone SMC in {to} is an Infantry OVR (A15.432), which is not reviewed; the charge ends in place (ruling R30.5)"
            : null;
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
        string attemptId)
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

            yield return ("instance-created", new InstanceCreated(new NewInstance(id, "asl:leader", leader.DefinitionId, leader.Side, new MapPosition(location), null, conditions)));
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

            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal);
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

        // A11.2: a withdrawing unit neither eliminated nor Reduced leaves the Melee for the Location it declared.
        foreach (var effect in resolution.Effects.Where(item => item.WithdrewTo is not null))
        {
            yield return ("instance-moved", new InstanceMoved(effect.UnitId, new MapPosition(BoardLocation.Parse(effect.WithdrewTo!))));
        }
    }

    /// <summary>
    /// The captor's choice for a pending surrender (A15.5, A20.21): the unit abandons its SW in its Location (A20.24), is placed with
    /// the Guard, and becomes its prisoner, no longer broken or Disrupted (A20.54: prisoners are never broken).
    /// </summary>
    private GamePlan PlanTakePrisoner(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || !Text(arguments, "captorId", out var captorId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a capture names the surrendering unit and its captor");
        }

        if (state.PendingSurrenders.FirstOrDefault(item => item.Unit == unitId) is not { } pending)
        {
            return Refused(scope, label, expected, $"play.no-surrender: {unitId} has not surrendered");
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
                && !Is(other, Conditions.Broken) && !Is(other, Conditions.Berserk) && !Is(other, Conditions.Melee)
                && (vocabulary.IsA(other.Kind, "asl:mmc") || vocabulary.IsA(other.Kind, "asl:smc"))
                && state.Location(other.Id)?.Location is { } there && IsAdjacent(state, there, at)).ToArray();
        string[] guards = [.. adjacent.Where(other => state.Units.Where(prisoner => prisoner.Status == InstanceStatus.Active && prisoner.Custodian == other.Id).Sum(UnitSize)
            + UnitSize(unit) <= 5 * UnitSize(other)).Select(other => other.Id).Order(StringComparer.Ordinal)];

        // A20.21, A20.5: a unit surrendering to captors with no Guard capacity left is freed as Unarmed, which is not built: undecided.
        return adjacent.Length > 0 && guards.Length == 0 ? null : guards;
    }

    /// <summary>A unit's US# (A1.6, p. 45): a squad 3, a HS 2, a SMC 1.</summary>
    private int UnitSize(UnitInstance unit) => vocabulary.IsA(unit.Kind, "asl:squad") ? 3 : vocabulary.IsA(unit.Kind, "asl:half-squad") ? 2 : 1;

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
    private int? EntryCost(GameState state, BoardLocation from, BoardLocation to)
    {
        var (fromRead, toRead, adjacent, crossed) = Step(state, from, to);
        if (fromRead is null || toRead is null || !adjacent || crossed is null
            || fromRead.Hex.BaseLevel + fromRead.Level.Level != toRead.Hex.BaseLevel + toRead.Level.Level || crossed.HexsideTerrain is not null || crossed.Cliff
            || crossed.Slope || TerrainKey(toRead) is not { } terrain || !EntryHalfMf.TryGetValue(terrain, out var halfMf))
        {
            return null;
        }

        return crossed.Terrain?.IsRoad == true ? 2 : halfMf;
    }

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431): the first step of each
    /// shortest route in MF to that unit's Location, over reviewed terrain, around other Locations holding enemy units. Equidistant
    /// targets are the ATTACKER's choice, so their steps are all allowed. With no Known enemy unit in LOS, the charge keeps to the
    /// Location it charged (<paramref name="previous"/>). A route that unreviewed terrain could shorten (at one MF an entry, the
    /// least any terrain costs) leaves the step undecided.
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
            var exact = RouteCosts(state, target, blocked, lowerBound: false);
            var lower = RouteCosts(state, target, blocked, lowerBound: true);
            if (!exact.TryGetValue(from, out var best) || lower.GetValueOrDefault(from, int.MaxValue) < best)
            {
                undecided.Add($"play.charge-undecided: the shortest route from {from} to {target} may cross terrain the review does not admit (ruling R30.5)");
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
    /// The berserk units of the phasing side that must still charge this MPh (A15.43): not held in Melee, not done moving, not in a
    /// Location with a Known enemy unit, and with a step they can afford on a decided route.
    /// </summary>
    private IReadOnlyList<UnitInstance> MustCharge(GameState state) =>
        [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side == state.PhasingSide && Is(unit, Conditions.Berserk) && !Is(unit, Conditions.Melee)
            && !unit.MovementEnded && state.Location(unit.Id) is { } at
            && ChargeSteps(state, unit.Side, at.Location, state.Movement?.Members.Contains(unit.Id) == true ? state.Movement.Charge : null) is { Steps.Count: > 0 } charge
            && Experience.MoveAllowance(state, unit, catalogs, vocabulary) is { } allowance
            && charge.Steps.Values.Any(step => (allowance * 2) - (unit.MfSpent * 2) - (unit.HalfMfSpent ? 1 : 0) >= step.HalfMf))];
}
