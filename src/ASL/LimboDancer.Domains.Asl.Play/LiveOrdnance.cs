using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live Gun's shot that the game state decides (unit step 24): the fire phase and side, the Gun manned by its crew
/// with their conditions, its shots this phase and Multiple ROF (C2.24), its fire marker, its Acquisition of the target Location
/// (C6.5), and the enemy units there. The map reads (range, the Covered Arc, the LOS, the terrain) are the planner's.
/// </summary>
public static class LiveOrdnance
{
    /// <summary>The state's part of a shot, or the reason the Gun cannot fire from this state.</summary>
    public static (OrdnanceShot? Shot, string? Reason) FromState(GameState state, string gunId, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(target);
        if (state.Catalog.Catalog != LiveFire.Catalog || state.Catalog.Version != LiveFire.CatalogVersion)
        {
            return (null, $"play.ordnance-catalog: the Ordnance package reads {LiveFire.Catalog}@{LiveFire.CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            "afph" => "AFPh",
            _ => null,
        };
        if (phase is null)
        {
            return (null, "play.ordnance-phase: a Gun fires in the PFPh, DFPh, or AFPh (C5.2; Defensive First Fire by ordnance is not reviewed)");
        }

        if (state.Find(gunId) is not EquipmentInstance { Status: InstanceStatus.Active, Kind: "asl:gun", Definition: not null, Position: MapPosition } gun
            || gun.Holding is not { Role: HoldingRole.Manned } manning || state.Unit(manning.Holder) is not { Status: InstanceStatus.Active, Definition: not null } crew)
        {
            return (null, $"play.ordnance-gun: '{gunId}' is not an active Gun from the catalog on the map, manned by an active unit (A21.13, C2.1)");
        }

        // A11.15: a unit held in Melee fires only in CC; a prisoner does not fire.
        if (Is(crew, Conditions.Melee) || Is(crew, Conditions.Captured))
        {
            return (null, $"play.ordnance-crew: '{crew.Id}' is held in Melee or captured, so it does not fire its Gun (A11.15, A20.5)");
        }

        var side = crew.Side;
        UnitInstance[] targets = [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && !Is(unit, Conditions.Captured)).OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (targets.Any(unit => unit.Definition is null && unit.Kind != UnitKinds.Dummy))
        {
            return (null, "play.ordnance-target: the target Location holds a unit outside the catalog");
        }

        var firingSide = side == state.PhasingSide ? "phasing" : "non-phasing";
        var targetSide = targets.FirstOrDefault()?.Side ?? state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
        var shots = state.OrdnanceShots.FirstOrDefault(item => item.Gun == gun.Id);
        // C6.5, C6.51 (ruling R5.13): the Acquisition applies at its Location, or, while its units are apart, at any Location holding one of them.
        var acquisition = state.Acquisitions.FirstOrDefault(item => item.Gun == gun.Id
            && (item.Location == target || item.Units.Any(id => state.Location(id)?.Location == target)))?.Level ?? 0;
        var hit = new FireAttack(phase, firingSide, null, null, target.ToString(), [], null, null, null, null, state.ScenarioMonth, null,
            [.. targets.Select(unit => LiveFire.Target(unit, target))], targetSide is null ? null : state.Side(targetSide)?.Elr, null)
        {
            TargetSideNoQuarter = targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
        };
        return (new OrdnanceShot(phase, firingSide, state.Side(side)?.Nationality,
            new OrdnanceGun(gun.Id, gun.Definition.Definition, Is(gun, Conditions.Malfunctioned), shots?.Shots ?? 0, shots?.RateOfFireKept ?? false, LiveFire.Fired(gun)),
            new OrdnanceCrew(crew.Id, crew.Definition.Definition, Is(crew, Conditions.Broken), Is(crew, Conditions.Pinned), Is(crew, Conditions.Berserk),
                Is(crew, Conditions.Concealed) || Is(crew, Conditions.Hidden), false)
            {
                Cx = Is(crew, Conditions.Cx) ? true : null,
            },
            target.ToString(), null, null, null, null, acquisition, hit, null), null);
    }

    /// <summary>The rolls of a record, rebuilt from its roll ids and the recorded dice, in the package's shape; null when one is misshapen.</summary>
    public static OrdnanceRolls? Rolls(OrdnanceFired fired, OrdnanceShot shot, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(fired);
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? toHit = null;
        int? subsequent = null;
        Dictionary<string, int>? selection = null;
        var hit = new Dictionary<string, string>(StringComparer.Ordinal);
        var critical = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, rollId) in fired.Rolls)
        {
            if (!rolls.TryGetValue(rollId, out var roll))
            {
                return null;
            }

            if (key == "toHit" && roll.Count == 2)
            {
                toHit = roll.Values;
            }
            else if (key == "subsequent" && roll.Count == 1)
            {
                subsequent = roll.Values[0];
            }
            else if (key.StartsWith("criticalSelection:", StringComparison.Ordinal) && key["criticalSelection:".Length..].Split(',') is var ids && ids.Length == roll.Count)
            {
                selection = ids.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => roll.Values[item.index], StringComparer.Ordinal);
            }
            else if (key.StartsWith("hit:", StringComparison.Ordinal))
            {
                hit[key["hit:".Length..]] = rollId;
            }
            else if (key.StartsWith("critical-hit:", StringComparison.Ordinal))
            {
                critical[key["critical-hit:".Length..]] = rollId;
            }
            else
            {
                return null;
            }
        }

        FireRolls? Nested(Dictionary<string, string> ids) => ids.Count == 0 ? null
            : LiveFire.Rolls(new FireResolved([], null, fired.Target.ToString(), fired.Target.ToString(), ids, fired.Facts, fired.Resolution), shot.Hit!, rolls);
        var hitRolls = Nested(hit);
        var criticalRolls = Nested(critical);
        return (hit.Count > 0 && hitRolls is null) || (critical.Count > 0 && criticalRolls is null)
            ? null
            : new OrdnanceRolls(toHit, subsequent, selection, hitRolls, criticalRolls);
    }

    private static bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;
}

/// <summary>
/// Replays an ordnance record through the Ordnance package: the recorded facts must agree with the state the record is made in,
/// and the package must reproduce the recorded resolution, ROF, and Acquisition from those facts and the recorded dice. The map
/// facts are the planner's, recorded with the shot.
/// </summary>
public sealed class OrdnanceRecordVerifier(ScenarioA1OrdnanceReference reference) : IOrdnanceRecordVerifier
{
    private static readonly Lazy<OrdnanceRecordVerifier> Instance = new(() => new OrdnanceRecordVerifier(new ScenarioA1OrdnancePackage().Reference));

    public static OrdnanceRecordVerifier Shared => Instance.Value;

    public string? Verify(GameState state, OrdnanceFired fired, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(fired);
        ArgumentNullException.ThrowIfNull(rolls);
        OrdnanceShot? recorded;
        try
        {
            recorded = fired.Facts.Deserialize<OrdnanceShot>(LiveFire.StrictJson);
        }
        catch (JsonException exception)
        {
            return "The ordnance record's facts cannot be read: " + exception.Message;
        }

        if (recorded is null || recorded.Rolls is not null || recorded.Hit is null)
        {
            return "The ordnance record's facts are incomplete.";
        }

        var (expected, reason) = LiveOrdnance.FromState(state, fired.Gun, fired.Target);
        if (expected is null)
        {
            return reason;
        }

        // The map facts are taken as recorded: range, the Covered Arc, the firer's terrain, the elevation limit, levels, LOS, the target
        // terrain, and each target's LOS to a Known enemy and its captors.
        var merged = expected with
        {
            Range = recorded.Range,
            HexspinesToTurn = recorded.HexspinesToTurn,
            FirerInWoodsOrBuilding = recorded.FirerInWoodsOrBuilding,
            ElevationAllowed = recorded.ElevationAllowed,
            Hit = expected.Hit! with
            {
                SameLevel = recorded.Hit.SameLevel,
                Los = recorded.Hit.Los,
                TargetTerrain = recorded.Hit.TargetTerrain,
                Targets = recorded.Hit.Targets is null ? expected.Hit.Targets
                    : [.. expected.Hit.Targets!.Zip(recorded.Hit.Targets, (fact, record) => fact with { KnownEnemyInLos = record.KnownEnemyInLos, Captors = record.Captors })],

                // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
                Choices = recorded.Hit.Choices,
            },
        };
        if (JsonSerializer.Serialize(merged, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json)
            || fired.Crew != recorded.Crew?.UnitId || fired.Target.ToString() != recorded.TargetLocationId)
        {
            return "The ordnance record's facts do not match the game state.";
        }

        // C3.21, C5.1: the recorded facing lies exactly the recorded number of hexspines from the Gun's facing now.
        var current = state.Find(fired.Gun) is EquipmentInstance { Position: MapPosition { Facing: { } facing } } ? facing : (Units.Documents.UnitFacing?)null;
        var steps = fired.Facing is { } turned && current is { } from ? Math.Min(Math.Abs((int)turned - (int)from), 6 - Math.Abs((int)turned - (int)from)) : 0;
        if ((recorded.HexspinesToTurn > 0) != fired.Facing.HasValue || steps != (recorded.HexspinesToTurn ?? 0))
        {
            return "The ordnance record turns the Gun exactly the hexspines its facts say the shot changes its Covered Arc by.";
        }

        if (LiveOrdnance.Rolls(fired, recorded, rolls) is not { } dice)
        {
            return "The ordnance record names a roll of the wrong shape.";
        }

        var resolution = ScenarioA1OrdnanceCalculator.Resolve(recorded with
        {
            Rolls = dice
        }, reference);
        if (resolution.Disposition != OrdnanceResolution.Resolved)
        {
            return "The ordnance record's facts and rolls do not resolve: " + string.Join("; ", resolution.Reasons);
        }

        if (!JsonElement.DeepEquals(JsonSerializer.SerializeToElement(resolution, LiveFire.Json), fired.Resolution))
        {
            return "The ordnance record's resolution differs from what its facts and rolls give.";
        }

        var gun = resolution.Gun!;
        return gun.RateOfFireKept != fired.RateOfFireKept || gun.Acquisition != fired.Acquisition || gun.AcquiredLocationId != fired.Acquired?.ToString()
            ? "The ordnance record's ROF or Acquisition differs from its resolution."
            : null;
    }
}
