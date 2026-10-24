using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>The hexes of one Canister aim on the map (C8.4, C8.41; ruling R55.8), at ground level: those of its cluster and the adjacent hex or hexes on its LOS that the map holds.</summary>
public sealed record CanisterAimRead(int Aim, IReadOnlyList<BoardLocation> Cluster, IReadOnlyList<BoardLocation> Adjacent)
{
    /// <summary>Whether the LOS runs along the hexside of two adjacent hexes, each of which is attacked at a quarter of the FP (C8.41).</summary>
    public bool Quartered
    {
        get; init;
    }
}

/// <summary>
/// A Gun's Canister shot (backlog pass 55; C8.4 to C8.42, p. 178; rulings R55.8 to R55.10): the planner turns the aim Rules gives as steps across
/// the two hexsides of the Covered Arc into hexes of the map, reads each Location as the fire planner reads a target (its elevation, the LOS and
/// its Hindrance, its terrain), hands the facts to the Ordnance package for the shot and to the Fire package for each Location's attack, and
/// writes the events: the IFT DR, the ordnance record, the Gun's counters, and a fire record a Location on that same DR.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The Location and Covered Arc a Gun or a turreted MA fires Canister from; null when it is not on the map or has no facing.</summary>
    private static (BoardLocation From, UnitFacing Facing)? CanisterFirer(GameState state, string gunId) => state.Find(gunId) switch
    {
        EquipmentInstance { Position: MapPosition { Facing: { } facing } at } => (at.Location, facing),
        UnitInstance vehicle when LiveOrdnance.TurretFacing(state, vehicle) is { } turret && state.Location(vehicle.Id)?.Location is { } at => (at, turret),
        _ => null,
    };

    /// <summary>
    /// The five aims of a Gun's Covered Arc as hexes of the map (C8.4 and its EX; ruling R55.8), for its own facing or the one it would turn to;
    /// empty when the Gun or the map cannot be read. A hex off the map is left out, and an aim with no hex of its cluster on it is left out too
    /// (C8.41: the adjacent hex is attacked in addition to a cluster; Rules says so).
    /// </summary>
    public IReadOnlyList<CanisterAimRead> CanisterAims(GameState state, string gunId, UnitFacing? facing = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (CanisterFirer(state, gunId) is not { } firer)
        {
            return [];
        }

        var (left, right) = ScenarioA1CanisterCalculator.ArcSides((int)(facing ?? firer.Facing));
        BoardLocation? At(CanisterOffset offset)
        {
            // The steps in each order: near a map edge one order may leave the map where another does not.
            foreach (var order in StepOrders(offset.Left, offset.Right))
            {
                BoardLocation? at = new BoardLocation(firer.From.Board, firer.From.Hex, 0);
                foreach (var toLeft in order)
                {
                    at = at is null ? null : Across(state, at, (HexsideDirection)(toLeft ? left : right));
                }

                if (at is not null)
                {
                    return at;
                }
            }

            return null;
        }

        return [.. ScenarioA1CanisterCalculator.Aims.Select(aim => new CanisterAimRead(aim.Aim, [.. aim.Cluster.Select(At).OfType<BoardLocation>()],
            [.. aim.Adjacent.Select(At).OfType<BoardLocation>()])
        {
            Quartered = aim.Adjacent.Count > 1,
        }).Where(read => ScenarioA1CanisterCalculator.Attacked(read.Cluster, read.Adjacent).Count > 0)];
    }

    /// <summary>Every order of so many steps to the left hex and so many to the right (true is a step to the left), the left steps first.</summary>
    private static IEnumerable<bool[]> StepOrders(int left, int right)
    {
        if (left == 0 || right == 0)
        {
            yield return [.. Enumerable.Repeat(left > 0, left + right)];
            yield break;
        }

        foreach (var rest in StepOrders(left - 1, right))
        {
            yield return [true, .. rest];
        }

        foreach (var rest in StepOrders(left, right - 1))
        {
            yield return [false, .. rest];
        }
    }

    private GamePlan PlanFireCanister(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, GameState state, string gunId, string attemptId, long expected,
        string label, string actor)
    {
        if (!arguments.TryGetProperty("aim", out var aimArgument) || aimArgument.ValueKind != JsonValueKind.Number || !aimArgument.TryGetInt32(out var aim)
            || ScenarioA1CanisterCalculator.Aim(aim) is not { } aimed)
        {
            return Refused(scope, label, expected, ScenarioA1OrdnanceEligibility.CanisterAimRefusal);
        }

        UnitFacing? aimFacing = null;
        if (Text(arguments, "aimFacing", out var facingText))
        {
            if (!UnitFacings.TryParse(facingText, out var parsed))
            {
                return Refused(scope, label, expected, "play.invalid-arguments: a Canister shot's facing is one of the six hexspines, such as 'west'");
            }

            aimFacing = parsed;
        }

        var intensive = arguments.TryGetProperty("intensive", out var intensiveArgument) && intensiveArgument.ValueKind == JsonValueKind.True;
        if (CanisterFirer(state, gunId) is not { } firer)
        {
            return Refused(scope, label, expected, state.Find(gunId) is null ? ScenarioA1OrdnanceEligibility.GunRefusal(gunId)
                : $"play.ordnance-facing: '{gunId}' has no facing, so its Covered Arc is unknown (C3.2)");
        }

        // The state's part first, with the Gun's own Location standing in for a target: it says whether the Gun may fire at all.
        var (from, current) = firer;
        var (able, reason) = LiveOrdnance.FromState(state, gunId, from, null, ScenarioA1CanisterCalculator.Ammunition, intensive);
        if (able is null)
        {
            return Refused(scope, label, expected, reason!);
        }

        // A7.25, A15.432 (table player, pass 12): an Opportunity Firer or a berserk unit fires no ordnance in its PFPh.
        var shooter = state.Find(gunId) is EquipmentInstance { Holding: { } holding } ? state.Unit(holding.Holder) : null;
        if (state.Phase == "pfph" && shooter is not null && (Is(shooter, Conditions.BoundingFire) || Is(shooter, Conditions.Berserk)))
        {
            return Refused(scope, label, expected, $"play.fire-barred: {shooter.Id} {GamePlanner.FireBar(state, shooter)}");
        }

        // C3.2, C5.1: the hexspines the CA turns to the facing the aim is of; a non-turreted MA would pivot the vehicle, which is not built.
        var facing = aimFacing ?? current;
        var steps = ScenarioA1OrdnanceEligibility.HexspinesTurned((int)facing, (int)current);
        var tank = state.Find(gunId) as UnitInstance;
        var turreted = tank is not null && OrdnanceReference.Value.Guns.GetValueOrDefault(able.Gun!.DefinitionId!)?.MaType is "t" or "st" or "rst" or "1mt";
        if (ScenarioA1OrdnanceMapRules.VcaRefusal(tank?.Id, turreted, steps) is { } vca)
        {
            return Refused(scope, label, expected, vca);
        }

        if (ReadLocation(state, from) is not { } gunRead || TerrainKey(gunRead) is not { } gunTerrain)
        {
            return Refused(scope, label, expected, "play.ordnance-map: the Gun's Location cannot be read");
        }

        // C8.4, C8.41 (ruling R55.9): the Locations of the cluster, then of the adjacent hex or hexes on the LOS, that are at the firer's
        // elevation and in its LOS, each read as the fire planner reads a target. A LOS an obstacle blocks, in the first hex of the cluster
        // or anywhere else, leaves its Location out; LOS to the vertex itself is not required.
        var elevation = gunRead.Hex.BaseLevel + gunRead.Level.Level;
        var hexes = CanisterAims(state, gunId, facing).FirstOrDefault(read => read.Aim == aimed.Aim);
        var inCluster = new List<(OrdnanceCanisterLocation Location, FireAttack Read)>();
        var beside = new List<(OrdnanceCanisterLocation Location, FireAttack Read)>();
        foreach (var (at, adjacent) in (hexes?.Cluster ?? []).Select(at => (at, (int?)null)).Concat((hexes?.Adjacent ?? []).Select(at => (at, (int?)aimed.Adjacent.Count))))
        {
            if (ReadLocation(state, at) is not { } read || !ScenarioA1OrdnanceEligibility.CanisterElevation(elevation, read.Hex.BaseLevel))
            {
                continue;
            }

            var (targets, targetsReason) = LiveFire.CanisterFromState(state, gunId, at);
            if (targets is null)
            {
                return Refused(scope, label, expected, targetsReason!);
            }

            var (mapped, mapReason) = FireMapFacts(state, targets with
            {
                Firers = [new FireFirer(able.Crew!.UnitId, able.Crew.DefinitionId, from.ToString(), false, false, false, false, false)],
            }, at, existing);
            if (mapped is null)
            {
                return Refused(scope, label, expected, mapReason!);
            }

            // E1.81: a Location beyond the firer's NVR is not in its LOS; the clause for SMOKE, LV, and NVR is not built.
            if (mapped.Los?.Blocked == true || mapped.BeyondNvr == true)
            {
                continue;
            }

            if (state.At(at).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured))))
            {
                return Refused(scope, label, expected, "play.fire-melee: fire at a Location holding units in Melee or prisoners is not reviewed (A11.15, A20.54)");
            }

            (adjacent is null ? inCluster : beside).Add((new OrdnanceCanisterLocation(at.ToString()) { AdjacentHexes = adjacent }, HeatOfBattleFacts(state, mapped)));
        }

        // C8.41 (the referee's review): the adjacent hex is attacked in addition to a cluster that is; Rules says what the shot attacks.
        var attacked = ScenarioA1CanisterCalculator.Attacked(inCluster, beside);
        if (attacked.Count == 0)
        {
            return Refused(scope, label, expected, ScenarioA1OrdnanceEligibility.CanisterEmptyRefusal(aimed.Aim, facing.Name()));
        }

        // The record names the first Location attacked as its target; the shot's own facts follow the state's.
        var nominal = BoardLocation.Parse(attacked[0].Location.LocationId!);
        var (stated, statedReason) = LiveOrdnance.FromState(state, gunId, nominal, null, ScenarioA1CanisterCalculator.Ammunition, intensive);
        if (stated is null)
        {
            return Refused(scope, label, expected, statedReason!);
        }

        var shot = PassEightFacts(state, stated with
        {
            Range = attacked[0].Read.Range,
            HexspinesToTurn = steps,
            FirerInWoodsOrBuilding = WoodsOrBuilding.Contains(gunTerrain),
            ElevationAllowed = true,
            BreakdownReduction = ExtremeWinterReduction(state, state.Unit(stated.Crew?.UnitId ?? string.Empty)?.Side),
            Canister = new OrdnanceCanister(aimed.Aim, facing.Name(), [.. attacked.Select(item => item.Location)]),
        }, nominal);
        shot = shot with
        {
            // C11 is the Infantry Target Type's: a Gun in a Location Canister attacks is no target of the shot itself.
            Hit = shot.Hit! with
            {
                GunTarget = null
            },
        };
        var reference = OrdnanceReference.Value;
        var precheck = ScenarioA1OrdnanceCalculator.Precheck(shot, reference);
        if (precheck.Count != 0)
        {
            return Refused(scope, label, expected, RefusalReasons.Refusal("play.ordnance-refused", "Ordnance", "shot", precheck));
        }

        var gun = reference.Guns[shot.Gun!.DefinitionId!];
        FireAttack[] attacks = [.. attacked.Select(item => item.Read with
        {
            Firers = [],
            Canister = ScenarioA1CanisterCalculator.AttackFact(shot, gun, item.Location),
        })];
        foreach (var attack in attacks)
        {
            var check = ScenarioA1FireCalculator.Precheck(attack, FireReference.Value);
            if (check.Count != 0)
            {
                return Refused(scope, label, expected, [$"play.canister-location: the attack on {attack.TargetLocationId} is refused, and with it the shot (C8.4)",
                    .. RefusalReasons.Refusal("play.fire-refused", "Fire", "attack", check)]);
            }
        }

        var firingSide = state.Unit(shot.Crew!.UnitId!)!.Side;
        var targetSide = state.Sides.FirstOrDefault(item => item.Id != firingSide)?.Id ?? string.Empty;
        UnitFacing? turnTo = steps > 0 ? facing : null;
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            AddOrdnanceEvents(scope, attemptId, expected, actor, state, shot, turnTo, events, draw);

            // C8.4 (ruling R55.9): each Location is attacked on the shot's own DR, unless the Gun had no Canister (C8.9).
            if (events.Select(item => item.Payload).OfType<OrdnanceFired>().LastOrDefault() is { } fired && fired.Rolls.TryGetValue("canister", out var rollId)
                && fired.Resolution.TryGetProperty("canister", out var recorded) && recorded.TryGetProperty("attacks", out var made) && made.GetArrayLength() > 0
                && events.Select(item => item.Payload).OfType<DiceRolled>().FirstOrDefault(item => item.Roll == rollId) is { } dice)
            {
                AddCanisterAttacks(scope, attemptId, expected, actor, existing, targetSide, events, draw, rollId, dice.Values, attacks);
            }

            return events;
        }

        // The proposal: each Location with its FP before any halving for concealment, its terrain, and its Hindrance, read from the map alone.
        var preview = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = new OrdnanceRolls(null, null, null, null, null) { Canister = [1, 1] }
        }, reference);
        var block = preview.Canister!;
        var lines = attacks.Zip(block.Attacks, (attack, fp) => $"{attack.TargetLocationId} with {fp.Firepower.ToString("0.##", CultureInfo.InvariantCulture)} FP"
            + (fp.Multipliers.Count > 0 ? $" ({string.Join(", ", fp.Multipliers.Select(item => ScenarioA1CanisterCalculator.HalvingWords(item.Name)))})" : string.Empty)
            + $", {attack.TargetTerrain}"
            + (ScenarioA1FireReference.Tem.GetValueOrDefault(attack.TargetTerrain!) is var tem and not 0 ? $" {tem:+0;-0}" : string.Empty)
            + (attack.HexsideTem is { Tem: > 0 } wall ? $", {wall.Terrain} {wall.Tem:+0;-0}" : string.Empty)
            + (attack.Los!.HindranceDrm is > 0 and var hindrance ? $", Hindrance +{hindrance.ToString(CultureInfo.InvariantCulture)}" : string.Empty));
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [$"play.canister: {shot.Gun.GunId} fires Canister at {ScenarioA1CanisterCalculator.AimWords(block.Aim, block.Facing)} in the {shot.Phase}"
                + (steps > 0 ? $", turning {steps.ToString(CultureInfo.InvariantCulture)} hexspine(s) to {facing.Name()}: Case A +{block.CaseA.ToString(CultureInfo.InvariantCulture)} on the IFT DR (D3.52)" : string.Empty)
                + $"; no To Hit DR: one IFT DR attacks {string.Join("; ", lines)}; FP is halved again against concealed units (C8.4)"
                + BailOutPreview(TurretTurnBailOut(state, gunId, turnTo)),
             $"play.canister-dr: the same DR is the check of the {ScenarioA1Definitions.DepletionWords(shot)} (above it the Gun had no Canister, attacks nothing, has not fired, and has none "
                + $"for the rest of the scenario, C8.9), of a malfunction on {preview.Gun!.BreakdownNumber.ToString(CultureInfo.InvariantCulture)} or more (C2.28), "
                + "and, by its colored dr, of the ROF (C2.24); the Gun loses any Acquisition (C6.5)"])
        {
            Roll = new PlannedRoll("ordnance", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>
    /// The attacks of a Canister shot on its Locations, in their order, each a fire record of its own on the shot's IFT DR (C8.4; ruling R55.9), as
    /// Spraying Fire's second Location takes the first's DR. An owner's option one of them reaches stops the rest, which its resume carries.
    /// </summary>
    private void AddCanisterAttacks(GameScope scope, string attemptId, long expected, string actor, IReadOnlyList<GameEvent> existing, string targetSide, List<GameEvent> events,
        Func<RollRequest, RollResult> draw, string rollId, IReadOnlyList<int> dice, IReadOnlyList<FireAttack> attacks)
    {
        for (var index = 0; index < attacks.Count; index++)
        {
            var after = Replay([.. existing, .. events]).Current!;
            FireAttack[] rest = [.. attacks.Skip(index + 1)];
            AddFireEvents(scope, attemptId, expected, actor, after, attacks[index], targetSide, null, events, draw,
                new ResumedRolls(new Dictionary<string, string>(StringComparer.Ordinal) { ["attack"] = rollId }, [("attack", dice)]),
                new FireFollowUps(attacks[index].TargetLocationId!)
                {
                    Canister = rest.Length > 0 ? rest : null,
                });
            if (events.Any(item => item.Payload is ChoicePending))
            {
                return;
            }
        }
    }
}
