"""Pass 50, task 50.3: the page: a Delay MP, the turret turned with an MP expenditure, and the MA's Bounding First Fire."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
PAGE = "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
BFF = "LimboDancer.Domains.Asl.MapStudio/Components/Play/BoundingFireAction.razor"

e.edit(PAGE, """            steps.Add(new(new("start"), "Propose: start (1 MP)", Id: "vehicle-start"));
            steps.Add(new(new("start", Reverse: true), "Propose: start in Reverse (1 MP; D2.23)", Id: "vehicle-start-reverse"));
            return steps;
""", """            steps.Add(new(new("start"), "Propose: start (1 MP)", Id: "vehicle-start"));
            steps.Add(new(new("start", Reverse: true), "Propose: start in Reverse (1 MP; D2.23)", Id: "vehicle-start-reverse"));

            // D2.17, D3.3 (pass 50): a Delay MP while stopped or before starting: to lengthen its LOS before a shot, to turn the turret, or to draw fire.
            steps.Add(new(new("delay"), "Propose: Delay (1 MP; D2.17)", Id: "vehicle-delay",
                Title: "One MP spent without moving, while stopped or before starting: a longer LOS before a shot (Case C in place of C1 or C2), a turret change, or drawn fire (D2.17, D3.3)"));
            return steps;
""")
e.edit(PAGE, """            ["vehicleId"] = vehicleUnit,
            ["kind"] = kind,
        };
        if (reverse)
        {
            arguments["reverse"] = true;
        }
""", """            ["vehicleId"] = vehicleUnit,
            ["kind"] = kind,
        };

        // D3.12 (pass 50): the turret turns with a Delay MP, a stop, a VCA change, or an entry.
        if (vehicleTurret.Length > 0 && kind is "delay" or "stop" or "turn" or "enter")
        {
            arguments["turret"] = vehicleTurret;
            vehicleTurret = string.Empty;
        }

        if (reverse)
        {
            arguments["reverse"] = true;
        }
""")
e.edit(PAGE, """    private string boundingTarget = string.Empty;
""", """    private string boundingTarget = string.Empty;
    private string boundingVehicle = string.Empty;
    private string vehicleTurret = string.Empty;
""")
e.edit(PAGE, """                                @if (chosen.Position is MapPosition && LiveFire.MayBoundingFire(state, chosen.Id)
                                    && (GamePlanner.HasVehicleMg(chosen) ? LiveFire.CrewExposed(chosen) && GameState.Condition(chosen, Conditions.BoundingFire) != ConditionState.True
                                        : VehicleMgChoices(chosen).Any(weapon => weapon.Bar is null)))
                                {
                                    <BoundingFireAction Targets="@([.. FireTargets(state.PhasingSide)])" Target="@boundingTarget" OnTarget="@(target => boundingTarget = target)"
                                        Weapons="@VehicleMgChoices(chosen)" SelectedWeapons="@boundingWeapons" OnWeapon="@(pick => Toggle(boundingWeapons, pick.Id, pick.On))"
                                        OnPropose="@ProposeBoundingFire" />
                                }
""", """                                @if (chosen.Position is MapPosition && LiveOrdnance.IsTank(chosen) && LiveFire.MaTypeOf(chosen.Definition?.Definition) is "t" or "st" or "rst" or "1mt"
                                    && state.Movement is not { WindowOpen: true } && !chosen.MovementEnded)
                                {
                                    <label title="D3.12: the TCA may change with each MP expended. The turret turns with the next Delay MP, stop, VCA change, or entry proposed; not in woods, a building, or rubble.">
                                        Turret with the next MP
                                        <select id="vehicle-turret" value="@vehicleTurret" @onchange="@(args => vehicleTurret = args.Value as string ?? string.Empty)">
                                            <option value="">as it is (@(LiveOrdnance.TurretFacing(state, chosen) is { } tcaNow ? UnitFacings.Name(tcaNow) : "no facing"))</option>
                                            @foreach (var spine in Enum.GetValues<UnitFacing>())
                                            {
                                                <option value="@UnitFacings.Name(spine)">@UnitFacings.Name(spine)</option>
                                            }
                                        </select>
                                    </label>
                                }
                                @if (chosen.Position is MapPosition && LiveFire.MayBoundingFire(state, chosen.Id)
                                    && (GamePlanner.HasVehicleMg(chosen) ? LiveFire.CrewExposed(chosen) && GameState.Condition(chosen, Conditions.BoundingFire) != ConditionState.True
                                        : LiveOrdnance.IsTank(chosen) || VehicleMgChoices(chosen).Any(weapon => weapon.Bar is null)))
                                {
                                    <BoundingFireAction Targets="@([.. FireTargets(state.PhasingSide)])" Target="@boundingTarget" OnTarget="@(target => { boundingTarget = target; boundingVehicle = string.Empty; })"
                                        Weapons="@VehicleMgChoices(chosen)" SelectedWeapons="@boundingWeapons" OnWeapon="@(pick => Toggle(boundingWeapons, pick.Id, pick.On))"
                                        MainArmament="@LiveOrdnance.IsTank(chosen)" Vehicles="@BoundingVehicleChoices(chosen.Side)" Vehicle="@boundingVehicle"
                                        OnVehicle="@(id => boundingVehicle = id)" OnProposeMa="@ProposeBoundingMa" OnPropose="@ProposeBoundingFire" />
                                }
""")
e.edit(PAGE, """    private readonly HashSet<string> boundingWeapons = new(StringComparer.Ordinal);
""", """    private readonly HashSet<string> boundingWeapons = new(StringComparer.Ordinal);

    /// <summary>The enemy vehicles the view sees in the Bounding First Fire target (C3.31; pass 50), for the MA's Vehicle Target Type.</summary>
    private IReadOnlyList<PlayChoice> BoundingVehicleChoices(string side) => state is null || !BoardLocation.TryParse(boundingTarget, out var at) ? []
        : [.. SeenOnly(state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && LiveFire.IsVehicle(unit))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)).Select(vehicle => new PlayChoice(vehicle.Id, $"{vehicle.Id} ({vehicle.Definition?.Definition})"))];

    // D3.3, C5.3 (pass 50): the moving vehicle's MA in Bounding First Fire, at a vehicle named or at the Location's Infantry.
    private Task ProposeBoundingMa()
    {
        var arguments = new Dictionary<string, object>
        {
            ["gameId"] = gameId, ["attemptId"] = Attempt("bffma"), ["expectedRevision"] = (long)history!.Events.Count, ["gunId"] = vehicleUnit, ["target"] = boundingTarget,
        };
        if (boundingVehicle.Length > 0)
        {
            arguments["targetVehicle"] = boundingVehicle;
        }

        return Propose(GameActions.FireOrdnance, JsonSerializer.SerializeToElement(arguments));
    }
""")

e.edit(BFF, """<button id="vehicle-bff" disabled=""", """@if (MainArmament)
{
    @if (Vehicles.Count > 0)
    {
        <label title="C3.31: the MA fires at one vehicle of the Location on the Vehicle Target Type; with none chosen, at its Infantry with HE.">
            MA at the vehicle
            <select id="vehicle-bff-vehicle" value="@Vehicle" @onchange="@(args => OnVehicle.InvokeAsync(args.Value as string ?? string.Empty))">
                <option value="">none: the Location's Infantry</option>
                @foreach (var vehicle in Vehicles)
                {
                    <option value="@vehicle.Id">@Say(vehicle.Label)</option>
                }
            </select>
        </label>
    }
    <button id="vehicle-bff-ma" disabled="@(Target.Length == 0)" title="D3.3, C5.3 to C5.35: the MA fires within its TCA with Case C (2 and the mount's 2 or 3), C1 or C2 for a LOS of three MP or less, and the lower dr doubled while not stopped; another MP is spent before it fires again."
            @onclick="@(() => OnProposeMa.InvokeAsync())">Propose: MA Bounding First Fire</button>
}
<button id="vehicle-bff" disabled=""")
e.edit(BFF, """    [Parameter]
    public EventCallback<UnitSelectionList.Toggle> OnWeapon { get; set; }
""", """    [Parameter]
    public EventCallback<UnitSelectionList.Toggle> OnWeapon { get; set; }

    /// <summary>Whether the vehicle's MA is a Gun that fires as ordnance (D3.3, C5.3; pass 50).</summary>
    [Parameter]
    public bool MainArmament { get; set; }

    /// <summary>The enemy vehicles the view sees in the target Location.</summary>
    [Parameter]
    public IReadOnlyList<PlayChoice> Vehicles { get; set; } = [];

    [Parameter]
    public string Vehicle { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> OnVehicle { get; set; }

    [Parameter]
    public EventCallback OnProposeMa { get; set; }
""")
e.apply()
