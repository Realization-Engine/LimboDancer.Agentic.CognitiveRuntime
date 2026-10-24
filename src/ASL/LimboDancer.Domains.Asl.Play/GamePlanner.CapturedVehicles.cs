using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Captured vehicles (pass 65, task 65.8; ruling R65.22): an Abandoned AFV captured at the end of the CCPh (A21.2), an unarmed vehicle captured with its
/// Temporary Driver (A11.52, A21.21), and the Temporary crew's penalties (A21.22, D5.343). Rules decides each; the planner reads the state's index and
/// writes <c>vehicle-captured</c>.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>A unit of a capture's Location as Rules reads it (A21.2): Personnel on foot, Good Order (able to attack in CC), a SMC.</summary>
    private CaptureUnitFacts CaptureUnit(UnitInstance unit) => new(unit.Id, unit.Side, !LiveFire.IsVehicle(unit) && vocabulary.IsA(unit.Kind, "asl:personnel") && !Is(unit, Conditions.Captured),
        CcAttacker(unit), vocabulary.IsA(unit.Kind, "asl:smc"));

    /// <summary>
    /// A21.2: the Abandoned AFVs captured as the CCPh ends, each with its captor's side and Location: an armed vehicle, Abandoned, not marked captured by
    /// ruling R11.16, with Good Order Infantry of one other side and no Personnel of its own in its Location. Read from the state's index.
    /// </summary>
    private IEnumerable<(UnitInstance Vehicle, string Side, BoardLocation At)> AutomaticCaptures(GameState state)
    {
        var index = MemoOf(state).Objects(state);
        foreach (var vehicle in state.Units.Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit) && unit.Position is MapPosition && Is(unit, Conditions.Abandoned))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (index.Location(vehicle.Id)?.Location is { } at && ScenarioA1CapturedVehicleRules.AutomaticCaptor(vehicle.Side, Armed(VehicleDefinition(vehicle)), true,
                Is(vehicle, Conditions.Captured), [.. index.At(at).OfType<UnitInstance>().Where(unit => unit.Id != vehicle.Id).Select(CaptureUnit)]) is { } side)
            {
                yield return (vehicle, side, at);
            }
        }
    }

    /// <summary>
    /// A11.52, A21.21: the record of an unarmed vehicle's capture as the CCPh begins: the captor's side, and the SMC removed into it as its Temporary Driver
    /// when every captor is a SMC. Since the pass it replaces ruling R11.16's Captured and Abandoned conditions, which older records keep.
    /// </summary>
    private VehicleCaptured UnarmedCapture(GameState state, UnitInstance vehicle)
    {
        var index = MemoOf(state).Objects(state);
        var at = index.Location(vehicle.Id)!.Location;
        var captors = index.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != vehicle.Side && CcAttacker(unit)).ToArray();
        return new VehicleCaptured(vehicle.Id, at, captors[0].Side, VehicleCaptured.Unarmed)
        {
            Driver = ScenarioA1CapturedVehicleRules.TemporaryDriver([.. captors.Select(CaptureUnit)]),
        };
    }

    /// <summary>A21.22: the nationality of the unit that crews a vehicle from inside, read from the state's index; null for an inherent crew.</summary>
    private string? CrewNationality(GameState state, UnitInstance vehicle) =>
        CrewInsideOf(state, vehicle.Id)?.Definition is { } crew ? FireReference.Value.Definitions.GetValueOrDefault(crew.Definition)?.Nationality : null;

    /// <summary>
    /// D2.51, A21.22: whether an AFV makes a Mechanical Reliability DR as it starts: its MP are printed red, or a captured vehicle's Temporary crew is an
    /// enemy vehicle crew, which takes the red MP penalty.
    /// </summary>
    private static bool RedMp(UnitInstance vehicle) => IsAfv(vehicle)
        && (VehicleDefinition(vehicle)?.MechanicallyUnreliable == true || ScenarioA1CapturedVehicleRules.RedMp(vehicle.TemporaryCrew?.Manning, vehicle.TemporaryCrew?.Captured == true));

    /// <summary>A21.22: why a vehicle manned by a SMC may not fire this weapon now (<c>aamg</c>, <c>bmg</c>, <c>cmg</c>, or <c>ma</c>), or null.</summary>
    public static string? SmcCrewFireBar(GameState state, UnitInstance vehicle, string weapon)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return vehicle.TemporaryCrew is { Manning: ScenarioA1CapturedVehicleRules.Smc }
            ? ScenarioA1CapturedVehicleRules.SmcFireBar(vehicle.Id, LiveFire.CrewExposed(vehicle), weapon, VehicleDefinition(vehicle)?.MaType == "1mt",
                VehicleMovedThisPlayerTurn(state, vehicle))
            : null;
    }
}
