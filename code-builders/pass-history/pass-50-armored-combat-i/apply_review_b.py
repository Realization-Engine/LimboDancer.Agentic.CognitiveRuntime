"""Pass 50, the reviews' fixes, part B: the Studio's page and its sentences."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
S = "LimboDancer.Domains.Asl.MapStudio/"
PAGE = S + "Components/Pages/Play.razor"
BFF = S + "Components/Play/BoundingFireAction.razor"
PANEL = S + "Components/Play/SmallArmsFirePanel.razor"
REC = S + "Services/PlayRecords.cs"

# UI 1, 2, 6; Rules boundary 5: the turret choice is sent only for the vehicle it was made for, with a step it turns with, and kept until a commit.
e.edit(PAGE, """        if (vehicleTurret.Length > 0 && kind is "delay" or "stop" or "turn" or "enter")
        {
            arguments["turret"] = vehicleTurret;
            vehicleTurret = string.Empty;
        }
""", """        if (vehicleTurret.Length > 0 && ScenarioA1VehicleMovementCalculator.TurretTurnsWith(kind) && state?.Unit(vehicleUnit) is { } turning && TurretChoiceShown(turning))
        {
            arguments["turret"] = vehicleTurret;
        }
""")
e.edit(PAGE, """                                @if (chosen.Position is MapPosition && LiveOrdnance.IsTank(chosen) && LiveFire.MaTypeOf(chosen.Definition?.Definition) is "t" or "st" or "rst" or "1mt"
                                    && state.Movement is not { WindowOpen: true } && !chosen.MovementEnded)
                                {""", """                                @if (TurretChoiceShown(chosen))
                                {""")
e.edit(PAGE, """    private void ChooseVehicle(string id)
    {
        vehicleUnit = id;
""", """    /// <summary>Whether the "Turret with the next MP" choice is shown for a vehicle (D3.12; pass 50): a turreted tank on the map that may still spend MP.</summary>
    private bool TurretChoiceShown(UnitInstance vehicle) => state is not null && vehicle.Position is MapPosition && LiveOrdnance.IsTank(vehicle)
        && LiveFire.MaTypeOf(vehicle.Definition?.Definition) is "t" or "st" or "rst" or "1mt" && state.Movement is not { WindowOpen: true } && !vehicle.MovementEnded;

    private void ChooseVehicle(string id)
    {
        vehicleUnit = id;
        vehicleTurret = string.Empty;
        boundingVehicle = string.Empty;
        boundingWeapons.Clear();
""")
e.edit(PAGE, """    private void ResetDrafts(bool keepCloseCombat)
    {
        (Picking.Armed, Picking.ArmedLabel) = (null, null);
""", """    private void ResetDrafts(bool keepCloseCombat)
    {
        (Picking.Armed, Picking.ArmedLabel) = (null, null);
        vehicleTurret = string.Empty;
        boundingVehicle = string.Empty;
        boundingWeapons.Clear();
""")
# UI 3: the vehicle named for the MA's shot must still be offered.
e.edit(PAGE, """        if (boundingVehicle.Length > 0)
        {
            arguments["targetVehicle"] = boundingVehicle;
        }
""", """        if (boundingVehicle.Length > 0 && state?.Unit(vehicleUnit) is { } firer && BoundingVehicleChoices(firer.Side).Any(choice => choice.Id == boundingVehicle))
        {
            arguments["targetVehicle"] = boundingVehicle;
        }
""")
# UI 7: the MA's button only for a MA that can fire.
e.edit(PAGE, """MainArmament="@(LiveOrdnance.IsTank(chosen) && GameState.Condition(chosen, Conditions.BoundingFire) != ConditionState.True)" Vehicles""",
       """MainArmament="@(LiveOrdnance.IsTank(chosen) && new[] { Conditions.BoundingFire, Conditions.Malfunctioned, Conditions.Disabled, Conditions.Recalled, Conditions.Stunned, Conditions.Shocked, Conditions.UnconfirmedKill }.All(name => GameState.Condition(chosen, name) != ConditionState.True))" Vehicles""")
# UI 8: the tank's label names its weapons' states.
e.edit(PAGE, """            (Conditions.Recalled, "Recalled"), (Conditions.Motion, "in Motion"), (Conditions.Immobilized, "immobilized"), (Conditions.Malfunctioned, "MA malfunctioned") }
            .Where(item => Marked(item.Item1)).Select(item => ", " + item.Item2));""", """            (Conditions.Recalled, "Recalled"), (Conditions.Motion, "in Motion"), (Conditions.Immobilized, "immobilized"), (Conditions.Malfunctioned, "MA malfunctioned"),
            (Conditions.Disabled, "MA disabled"), (Conditions.BmgMalfunctioned, "BMG malfunctioned"), (Conditions.BmgDisabled, "BMG disabled"),
            (Conditions.CmgMalfunctioned, "CMG malfunctioned"), (Conditions.CmgDisabled, "CMG disabled") }
            .Where(item => Marked(item.Item1)).Select(item => ", " + item.Item2));""")
e.edit(PAGE, """                        ? ", its MGs have fired" : ", fired this Player Turn\"""", """                        ? ", " + string.Join(" and ", state.VehicleWeaponShots.Where(item => item.Vehicle == tank.Id).Select(item => item.Weapon.ToUpperInvariant()).Distinct()) + " fired; the MA has not"
                        : ", fired this Player Turn\"""")

# UI 4, table player 12: the MGs' button says it is theirs, and why it waits.
e.edit(BFF, """<button id="vehicle-bff" disabled="@(Target.Length == 0 || (Weapons.Count > 0 && !Weapons.Any(weapon => weapon.Bar is null && SelectedWeapons.Contains(weapon.Id))))" @onclick="@(() => OnPropose.InvokeAsync())">Propose: Bounding First Fire</button>""",
       """<button id="vehicle-bff" disabled="@(MgReason is not null)" title="@(MgReason ?? "D3.3, D3.31: the MG fires at half FP, halved again while not stopped.")" aria-describedby="@(MgReason is not null ? "vehicle-bff-reason" : null)"
        @onclick="@(() => OnPropose.InvokeAsync())">@(Weapons.Count > 0 ? "Propose: MG Bounding First Fire" : "Propose: Bounding First Fire")</button>
@if (MgReason is { } waits)
{
    <span id="vehicle-bff-reason" class="toolbar-note">@waits</span>
}""")
e.edit(BFF, """    <button id="vehicle-bff-ma" disabled="@(Target.Length == 0)" title=""", """    <button id="vehicle-bff-ma" disabled="@(Target.Length == 0)" aria-describedby="@(Target.Length == 0 ? "vehicle-bff-reason" : null)" title=""")
e.edit(BFF, """@weapon.Label@(weapon.Bar is { } bar ? $": {bar}" : "")</label>""", """@weapon.Label@(weapon.Bar is { } bar ? $": {bar}" : ", halved in Bounding First Fire (D3.31)")</label>""")
e.edit(BFF, """    /// <summary>Whether the vehicle's MA is a Gun that fires as ordnance (D3.3, C5.3; pass 50).</summary>""", """    /// <summary>Why the MGs' button waits, or null: no target chosen, or a tank with neither MG ticked.</summary>
    private string? MgReason => Target.Length == 0 ? "Choose the target Location."
        : Weapons.Count > 0 && !Weapons.Any(weapon => weapon.Bar is null && SelectedWeapons.Contains(weapon.Id)) ? "Tick the BMG or the CMG that fires." : null;

    /// <summary>Whether the vehicle's MA is a Gun that fires as ordnance (D3.3, C5.3; pass 50).</summary>""")

# UI 5: a tank ticked with neither MG ticked.
e.edit(PANEL, """    @if (FtWithOthers)
    {
        <span id="fire-ft-alone" class="toolbar-note" role="status">A FT fires alone: untick the other units and weapons, and choose no director (A22.31).</span>
    }
    <button id="propose-fire" disabled="@(SelectedFirers.Count == 0 || FtWithOthers || (FreeTarget.Trim().Length == 0 && Target.Length == 0))" aria-describedby="@(FtWithOthers ? "fire-ft-alone" : null)" @onclick="@(() => OnPropose.InvokeAsync())">Propose: fire</button>""",
       """    @if (FtWithOthers)
    {
        <span id="fire-ft-alone" class="toolbar-note" role="status">A FT fires alone: untick the other units and weapons, and choose no director (A22.31).</span>
    }
    @if (TankWithoutMg)
    {
        <span id="fire-tank-mg" class="toolbar-note" role="status">Tick the BMG or the CMG that fires: a tank's MA fires as ordnance (D3.5).</span>
    }
    <button id="propose-fire" disabled="@(SelectedFirers.Count == 0 || FtWithOthers || TankWithoutMg || (FreeTarget.Trim().Length == 0 && Target.Length == 0))" aria-describedby="@(FtWithOthers ? "fire-ft-alone" : TankWithoutMg ? "fire-tank-mg" : null)" @onclick="@(() => OnPropose.InvokeAsync())">Propose: fire</button>""")
e.edit(PANEL, """    /// <summary>The firing side.</summary>""", """    /// <summary>Whether a ticked tank has neither of its MGs ticked (D3.5; pass 50): the gate refuses it, so Propose waits.</summary>
    private bool TankWithoutMg => SelectedFirers.Any(id => Weapons.Any(weapon => weapon.Holder == id && weapon.Label is not null)
        && !Weapons.Any(weapon => weapon.Holder == id && weapon.Label is not null && weapon.Bar is null && SelectedWeapons.Contains(weapon.Id)));

    /// <summary>The firing side.</summary>""")

# Table player 5; UI 8: the Recall as what happened; a turret turned with a VCA change.
e.edit(REC, """                            + (repair.Result == RepairAttempted.Eliminated ? "disabled (a 6; D3.7)" + (repairedWeapon == "ma" ? ", and the vehicle is Recalled if it carries no Passengers and tows nothing" : "") : result)""",
       """                            + (repair.Result == RepairAttempted.Eliminated
                                ? "disabled (a 6; D3.7)" + (attempts[attempt].Any(other => other.Payload is ConditionsChanged changed
                                    && changed.Conditions.TryGetValue(Conditions.Recalled, out var recalled) && recalled == ConditionState.True)
                                    ? ", and the vehicle is Recalled: buttoned up and treated as stunned until the Player Turn ends, then it must leave (D3.7, D5.341)" : "")
                                : result)""")
e.edit(REC, """                // D2.17, D3.12 (pass 50): a Delay MP, and a turret turned with an MP expenditure.
""", """                // D2.17, D3.12 (pass 50): a Delay MP, and a turret turned with an MP expenditure.
                VehicleStepped { Kind: VehicleStepped.Turn, Turret: { } turnTurret } turnedBoth => $"{turnedBoth.Vehicle} turns its turret to {UnitFacings.Name(turnTurret)} with its VCA change (D3.12)",
""")
# Table player 7: the MA's Bounding First Fire by its name, and what follows it.
e.edit(REC, """                    check + $"{fired.Gun}{In(history.At(item.Revision - 1), fired.Gun)} fires {aim}\"""",
       """                    check + $"{fired.Gun}{In(history.At(item.Revision - 1), fired.Gun)} fires {aim}" + (shot?.FireKind == ScenarioA1FireCalculator.BoundingFirstFire ? " as Bounding First Fire (D3.3)" : "\")""")
e.edit(REC, """                parts.Add((gun.Malfunctioned ? "the Gun malfunctions (C2.28)" : gun.RateOfFireKept ? "it may fire again this phase (C2.24)" : "no more fire this phase")""",
       """                var boundingShot = shot?.FireKind == ScenarioA1FireCalculator.BoundingFirstFire;
                parts.Add((gun.Malfunctioned ? "the Gun malfunctions (C2.28)"
                    : gun.RateOfFireKept ? boundingShot ? "it may fire again after another MP, and a Delay MP will do (C2.24, C5.3)" : "it may fire again this phase (C2.24)"
                    : boundingShot ? "a Bounding Fire counter: no more fire this Player Turn (D3.3)" : "no more fire this phase")""")

# Table player 6: a vehicle's MG by its name, with its Random Selection dr.
e.edit(S + "Services/FireText.cs", """        return weapon.Removed == true ? $"{weapon.EquipmentId} is removed: the DR reached its X# (A.11, A9.7)"
""", """        // D3.7, A9.71 (pass 50): a vehicle's BMG or CMG by its name, with the Random Selection dr when two fired together.
        if (weapon.VehicleWeapon is { } mg)
        {
            var selection = weapon.SelectionDr is { } dr ? $"; Random Selection dr {dr} (A9.71)" : string.Empty;
            return weapon.Malfunctioned ? $"{weapon.EquipmentId}'s {mg.ToUpperInvariant()} malfunctioned (D3.7){selection}"
                : selection.Length > 0 ? $"{weapon.EquipmentId}'s {mg.ToUpperInvariant()} did not malfunction{selection}" : null;
        }

        return weapon.Removed == true ? $"{weapon.EquipmentId} is removed: the DR reached its X# (A.11, A9.7)"
""")
# Table player 4: the To Hit cases in words.
e.edit(S + "Services/DisplayText.cs", """        ["ffnam"] = "FFNAM",
""", """        ["case-c4:lower-dr"] = "Case C4, the lower dr doubled",
        ["case-a:maintained"] = "Case A, the CA change its turret already made",
        ["ffnam"] = "FFNAM",
""")
# Table player 10: the Replay page.
RS = S + "Services/ReplaySteps.cs"
e.edit(RS, """                RepairAttempted.Eliminated => "the weapon is eliminated",
""", """                RepairAttempted.Eliminated => repair.Weapon is null ? "the weapon is eliminated" : "the weapon is disabled",
""")
e.edit(RS, """            return ("move", side, $"{A(side, "vehicle")} moves in {Place(before, drove.At)}{end}", false);
""", """            return ("move", side, $"{A(side, "vehicle")} {(drove.Kind == VehicleStepped.Delay ? "spends a Delay MP" : "moves")} in {Place(before, drove.At)}{end}", false);
""")

# Table player 4, 8: the proposal names the cases in words, the MGs, and the turret's turn.
O = "LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs"
e.edit(O, """                item.Name == "case-c4:lower-dr" ? "case-c4: the lower dr of the roll doubled" : $"{item.Name} {item.Value:+0;-0}")))}";""",
       """                item.Name == "case-c4:lower-dr" ? "Case C4: the lower dr of the roll doubled (C5.35)" : $"{ScenarioA1OrdnanceEligibility.CaseWords(item.Name)} {item.Value:+0;-0}")))}";""")
e.edit("LimboDancer.Domains.Asl.Rules/ScenarioA1OrdnanceEligibility.cs", """    /// <summary>The hexspines between a Gun's facing now and the facing it is turned to""", """    /// <summary>
    /// A To Hit DRM's recorded name in a proposal's words (review of pass 50): "case-c2" reads "Case C2", "case-q:woods" reads "Case Q (woods)",
    /// and "case-a:maintained" reads "Case A (the CA change its turret already made)"; any other name is left as it is.
    /// </summary>
    public static string CaseWords(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!name.StartsWith("case-", StringComparison.Ordinal))
        {
            return name;
        }

        var colon = name.IndexOf(':', StringComparison.Ordinal);
        var head = "Case " + (colon < 0 ? name["case-".Length..] : name["case-".Length..colon]).ToUpperInvariant();
        var rest = colon < 0 ? string.Empty : name[(colon + 1)..];
        return rest.Length == 0 ? head : rest == "maintained" ? head + " (the CA change its turret already made)" : $"{head} ({rest.Replace('-', ' ')})";
    }

    /// <summary>The hexspines between a Gun's facing now and the facing it is turned to""")
# Table player 8: the fire proposal names a tank's MGs.
e.edit("LimboDancer.Domains.Asl.Play/GamePlanner.Fire.cs", """facts.VehicleFire is { } byVehicle ? [byVehicle.VehicleId] : facts.Firers!.Select(item => item.UnitId))} fire at""",
       """facts.VehicleFire is { } byVehicle ? [byVehicle.Weapons is { Count: > 0 } mgs ? $"{byVehicle.VehicleId}'s {string.Join(" and ", mgs.Select(item => (item.Weapon ?? string.Empty).ToUpperInvariant()))}" : byVehicle.VehicleId] : facts.Firers!.Select(item => item.UnitId))} fire at""")
e.apply()
