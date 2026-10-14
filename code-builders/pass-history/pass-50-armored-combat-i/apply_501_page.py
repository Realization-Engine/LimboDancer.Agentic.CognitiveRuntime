"""Pass 50, tasks 50.1 and 50.2: the page offers a tank's BMG and CMG, and the repair of each of its weapons."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/pass32-tools/pass50")
from edits import Edits

e = Edits()
RULES = "LimboDancer.Domains.Asl.Rules/"
PLAY = "LimboDancer.Domains.Asl.Play/"
PAGE = "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
PANEL = "LimboDancer.Domains.Asl.MapStudio/Components/Play/SmallArmsFirePanel.razor"
BFF = "LimboDancer.Domains.Asl.MapStudio/Components/Play/BoundingFireAction.razor"

# --- Rules -----------------------------------------------------------------------------------------------------------
e.edit(RULES + "ScenarioA1FireEligibility.cs", """    /// <summary>
    /// Why a vehicle's MG may not fire at a target its CA does not cover""", """    /// <summary>
    /// Why a vehicle's BMG or CMG may not be chosen to fire now, in a few words, or null when it may (D3.5, D3.7, D1.321; pass 50): it has
    /// malfunctioned or is disabled, it has fired this Player Turn, a weapon of the vehicle fired in another fire phase, or it is the CMG of a
    /// CE tank whose turret is a RST or 1MT.
    /// </summary>
    public static string? VehicleMgBar(bool malfunctioned, bool disabled, bool fired, bool otherPhase, bool turretBarred) =>
        disabled ? "is disabled" : malfunctioned ? "has malfunctioned" : fired ? "has fired"
        : otherPhase ? "the vehicle fired in another phase (D3.5)" : turretBarred ? "a CE crew does not fire this turret's weapons (D1.321)" : null;

    /// <summary>D3.5 (pass 50): a tank that fires on the IFT names the BMG or CMG that fires; its MA fires as ordnance.</summary>
    public static string? VehicleWeaponsNamedBar(string vehicleId, bool mainArmamentIsMg, int named) =>
        mainArmamentIsMg || named > 0 ? null
            : $"play.fire-vehicle-weapons: name the BMG or the CMG of {vehicleId} that fires (D3.5); its MA fires as ordnance";

    /// <summary>
    /// Why a vehicle's MG may not fire at a target its CA does not cover""")
e.edit(RULES + "ScenarioA1FireEligibility.cs", """        if (facts.Vehicle)
        {
            return !facts.RateOfFireKept();
        }
""", """        if (facts.Vehicle)
        {
            // D3.5 (pass 50): nor while one of its other weapons may still fire in this fire phase.
            return !facts.RateOfFireKept() && facts.VehicleMgUnfired?.Invoke() != true;
        }
""")
e.edit(RULES + "ScenarioA1FireRuleModels.cs", """public sealed record FireSpentFacts(string? Phase, bool Fired, bool FirstFire, bool Vehicle, Func<bool> RateOfFireKept, Func<bool> SupportWeaponUsed, Func<bool> UnfiredWeaponHeld);
""", """public sealed record FireSpentFacts(string? Phase, bool Fired, bool FirstFire, bool Vehicle, Func<bool> RateOfFireKept, Func<bool> SupportWeaponUsed, Func<bool> UnfiredWeaponHeld)
{
    /// <summary>Whether the vehicle has a BMG or CMG that may still fire in this fire phase (D3.5; pass 50), read only when asked; null for none.</summary>
    public Func<bool>? VehicleMgUnfired
    {
        get; init;
    }
}
""")

# --- Play ------------------------------------------------------------------------------------------------------------
e.edit(PLAY + "LiveFire.cs", """    /// <summary>A vehicle's weapon shots of this Player Turn, as Rules reads them (D3.5, D3.51; pass 50).</summary>
""", """    /// <summary>
    /// The BMG and CMG a vehicle's counter prints, each with its FP and the reason it may not fire now, or null when it may (D1.8, D3.5, D3.7;
    /// pass 50). Empty for a vehicle that prints neither.
    /// </summary>
    public static IReadOnlyList<(string Weapon, int Fp, string? Bar)> VehicleMgs(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (!IsVehicle(vehicle) || vehicle.Definition is not { } named || CatalogReference.Value.Definitions.GetValueOrDefault(named.Definition) is not { } definition)
        {
            return [];
        }

        var shots = VehicleShots(state, vehicle.Id);
        var otherPhase = shots.Any(item => item.FirePhase != ScenarioA1FireEligibility.FirePhase(state.Phase, vehicle.Side == state.PhasingSide));
        return [.. new[] { FireVehicleWeapon.BowMg, FireVehicleWeapon.CoaxialMg }
            .Where(weapon => ScenarioA1FireCalculator.VehicleWeaponFp(definition, weapon) is not null)
            .Select(weapon => (weapon, ScenarioA1FireCalculator.VehicleWeaponFp(definition, weapon)!.Value, ScenarioA1FireEligibility.VehicleMgBar(
                Is(vehicle, weapon == FireVehicleWeapon.BowMg ? Conditions.BmgMalfunctioned : Conditions.CmgMalfunctioned),
                Is(vehicle, weapon == FireVehicleWeapon.BowMg ? Conditions.BmgDisabled : Conditions.CmgDisabled),
                shots.Any(item => item.Weapon == weapon), otherPhase,
                weapon == FireVehicleWeapon.CoaxialMg && definition.TurretBarredWhileCe(CrewExposed(vehicle)))))];
    }

    /// <summary>A vehicle's weapon shots of this Player Turn, as Rules reads them (D3.5, D3.51; pass 50).</summary>
""")
e.edit(PLAY + "LiveFire.cs", """                && !Fired(item) && !Is(item, Conditions.Malfunctioned))));
    }
""", """                && !Fired(item) && !Is(item, Conditions.Malfunctioned)))
        {
            VehicleMgUnfired = () => VehicleMgs(state, unit).Any(item => item.Bar is null),
        });
    }
""")
e.edit(PLAY + "LiveFire.cs", """        var fire = MainArmamentFire(state, vehicle, at);
""", """        var fire = MainArmamentFire(state, vehicle, at);
""")
e.edit(PLAY + "LiveFire.cs", """            return (null, grouped);
        }

        var firers = new List<(UnitInstance Unit, BoardLocation At)>();
""", """            return (null, grouped);
        }

        if (vehicleFirers.Length > 0 && ScenarioA1FireEligibility.VehicleWeaponsNamedBar(vehicleFirers[0].Id, !LiveOrdnance.IsTank(vehicleFirers[0]),
            weapons?.GetValueOrDefault(vehicleFirers[0].Id)?.Count ?? 0) is { } unnamed)
        {
            return (null, unnamed);
        }

        var firers = new List<(UnitInstance Unit, BoardLocation At)>();
""")

# --- The fire panel --------------------------------------------------------------------------------------------------
e.edit(PANEL, """@Say(weapon.Id) of @Say(weapon.Holder)@(weapon.Bar is { } bar""", """@(weapon.Label ?? Say(weapon.Id)) of @Say(weapon.Holder)@(weapon.Bar is { } bar""")
e.edit(PANEL, """    @foreach (var id in AloneCandidates.Union(SelectedFirers.Where(id => Weapons.Any(weapon => weapon.Holder == id && !weapon.Ft && weapon.Bar is null))).Order(StringComparer.Ordinal))
    {
        var ready = SelectedFirers.Contains(id) && Weapons.Any(weapon => weapon.Holder == id && !weapon.Ft && weapon.Bar is null && SelectedWeapons.Contains(weapon.Id));
""", """    @foreach (var id in AloneCandidates.Union(SelectedFirers.Where(id => Weapons.Any(weapon => weapon.Holder == id && !weapon.Ft && weapon.Label is null && weapon.Bar is null))).Order(StringComparer.Ordinal))
    {
        var ready = SelectedFirers.Contains(id) && Weapons.Any(weapon => weapon.Holder == id && !weapon.Ft && weapon.Label is null && weapon.Bar is null && SelectedWeapons.Contains(weapon.Id));
""")
e.edit(PANEL, """    /// <summary>A MG, FT, or ATR a chosen firer possesses, and its holder.</summary>
    public sealed record Weapon(string Id, string Holder, string? Bar = null, bool Ft = false);
""", """    /// <summary>A MG, FT, or ATR a chosen firer possesses, and its holder; or a vehicle's BMG or CMG (D3.5; pass 50), with the words it is listed under.</summary>
    public sealed record Weapon(string Id, string Holder, string? Bar = null, bool Ft = false, string? Label = null);
""")

# --- The Bounding First Fire action ----------------------------------------------------------------------------------
e.edit(BFF, """<label title="The vehicle's MG fires at the outset""", """@foreach (var weapon in Weapons)
{
    <label class="@(weapon.Bar is null ? null : "muted")" title="D3.5: a tank's BMG and CMG fire once a Player Turn each, added together or apart; in Bounding First Fire within the current CA (C5.13)."><input class="vehicle-bff-weapon" type="checkbox" data-weapon="@weapon.Id" disabled="@(weapon.Bar is not null)" checked="@(weapon.Bar is null && SelectedWeapons.Contains(weapon.Id))" @onchange="@(args => OnWeapon.InvokeAsync(new UnitSelectionList.Toggle(weapon.Id, args.Value is true)))" /> @weapon.Label@(weapon.Bar is { } bar ? $": {bar}" : "")</label>
}
<label title="The vehicle's MG fires at the outset""")
e.edit(BFF, """<button id="vehicle-bff" disabled="@(Target.Length == 0)" """, """<button id="vehicle-bff" disabled="@(Target.Length == 0 || (Weapons.Count > 0 && !Weapons.Any(weapon => weapon.Bar is null && SelectedWeapons.Contains(weapon.Id))))" """)
e.edit(BFF, """    [Parameter]
    public IReadOnlyList<string> Targets { get; set; } = [];
""", """    [Parameter]
    public IReadOnlyList<string> Targets { get; set; } = [];

    /// <summary>A tank's BMG and CMG (D3.5; pass 50); empty for a vehicle whose MA is its MG.</summary>
    [Parameter]
    public IReadOnlyList<SmallArmsFirePanel.Weapon> Weapons { get; set; } = [];

    [Parameter]
    public IReadOnlySet<string> SelectedWeapons { get; set; } = new HashSet<string>();

    [Parameter]
    public EventCallback<UnitSelectionList.Toggle> OnWeapon { get; set; }
""")

# --- The page --------------------------------------------------------------------------------------------------------
e.edit(PAGE, """                                @if (chosen.Position is MapPosition && LiveFire.MayBoundingFire(state, chosen.Id) && GamePlanner.HasVehicleMg(chosen) && LiveFire.CrewExposed(chosen)
                                    && GameState.Condition(chosen, Conditions.BoundingFire) != ConditionState.True)
                                {
                                    <BoundingFireAction Targets="@([.. FireTargets(state.PhasingSide)])" Target="@boundingTarget" OnTarget="@(target => boundingTarget = target)"
                                        OnPropose="@ProposeBoundingFire" />
                                }
""", """                                @if (chosen.Position is MapPosition && LiveFire.MayBoundingFire(state, chosen.Id)
                                    && (GamePlanner.HasVehicleMg(chosen) ? LiveFire.CrewExposed(chosen) && GameState.Condition(chosen, Conditions.BoundingFire) != ConditionState.True
                                        : VehicleMgChoices(chosen).Any(weapon => weapon.Bar is null)))
                                {
                                    <BoundingFireAction Targets="@([.. FireTargets(state.PhasingSide)])" Target="@boundingTarget" OnTarget="@(target => boundingTarget = target)"
                                        Weapons="@VehicleMgChoices(chosen)" SelectedWeapons="@boundingWeapons" OnWeapon="@(pick => Toggle(boundingWeapons, pick.Id, pick.On))"
                                        OnPropose="@ProposeBoundingFire" />
                                }
""")
e.edit(PAGE, """Weapons="@([.. FireWeapons.Select(weapon => new SmallArmsFirePanel.Weapon(weapon.Id, weapon.Holding!.Holder, LiveFire.WeaponBar(weapon), weapon.Kind == "asl:ft"))])\"""",
       """Weapons="@([.. FireWeapons.Select(weapon => new SmallArmsFirePanel.Weapon(weapon.Id, weapon.Holding!.Holder, LiveFire.WeaponBar(weapon), weapon.Kind == "asl:ft")), .. FireVehicleMgs])\"""")
e.edit(PAGE, """    private Task ProposeBoundingFire() => Propose(GameActions.Fire, JsonSerializer.SerializeToElement(new
    {
        gameId, attemptId = Attempt("bff"), expectedRevision = (long)history!.Events.Count, firers = new[] { vehicleUnit }, target = boundingTarget,
    }));
""", """    // D3.5 (pass 50): a tank names the BMG and CMG that fire; a vehicle whose MA is its MG names none.
    private Task ProposeBoundingFire() => Propose(GameActions.Fire, JsonSerializer.SerializeToElement(
        state?.Unit(vehicleUnit) is { } tank && VehicleMgChoices(tank) is { Count: > 0 } mgs
            ? new Dictionary<string, object>
            {
                ["gameId"] = gameId, ["attemptId"] = Attempt("bff"), ["expectedRevision"] = (long)history!.Events.Count, ["firers"] = new[] { vehicleUnit },
                ["target"] = boundingTarget,
                ["weapons"] = new Dictionary<string, string[]>
                {
                    [vehicleUnit] = [.. mgs.Where(weapon => weapon.Bar is null && boundingWeapons.Contains(weapon.Id)).Select(weapon => VehicleMgName(weapon.Id))],
                },
            }
            : new Dictionary<string, object>
            {
                ["gameId"] = gameId, ["attemptId"] = Attempt("bff"), ["expectedRevision"] = (long)history!.Events.Count, ["firers"] = new[] { vehicleUnit },
                ["target"] = boundingTarget,
            }));

    private readonly HashSet<string> boundingWeapons = new(StringComparer.Ordinal);

    /// <summary>A tank's BMG and CMG as the fire panels list them (D1.8, D3.5; pass 50): each under the vehicle's id and its own name, with its FP.</summary>
    private IReadOnlyList<SmallArmsFirePanel.Weapon> VehicleMgChoices(UnitInstance vehicle) => state is null ? []
        : [.. LiveFire.VehicleMgs(state, vehicle).Select(item => new SmallArmsFirePanel.Weapon(vehicle.Id + ":" + item.Weapon, vehicle.Id, item.Bar, false,
            $"{item.Weapon.ToUpperInvariant()} ({item.Fp} FP)"))];

    private static string VehicleMgName(string id) => id[(id.LastIndexOf(':') + 1)..];

    /// <summary>The BMG and CMG of the vehicles ticked as firers.</summary>
    private IReadOnlyList<SmallArmsFirePanel.Weapon> FireVehicleMgs => state is null ? []
        : [.. fireFirers.Order(StringComparer.Ordinal).Select(state.Unit).OfType<UnitInstance>().Where(LiveFire.IsVehicle).SelectMany(VehicleMgChoices)];
""")
e.edit(PAGE, """            && (Library.Vocabulary.IsA(unit.Kind, "asl:mmc") || unit.Kind == "asl:hero" || GamePlanner.HasVehicleMg(unit)) && MayFire(unit))
""", """            && (Library.Vocabulary.IsA(unit.Kind, "asl:mmc") || unit.Kind == "asl:hero" || GamePlanner.HasVehicleMg(unit) || VehicleMgChoices(unit).Count > 0) && MayFire(unit))
""")
e.edit(PAGE, """        && (!LiveFire.IsVehicle(unit) || (GamePlanner.HasVehicleMg(unit) && LiveFire.CrewExposed(unit)));
""", """        // D3.5 (pass 50): a tank is a firer while its BMG or CMG may fire; neither needs a CE crew (D1.83).
        && (!LiveFire.IsVehicle(unit) || (GamePlanner.HasVehicleMg(unit) ? LiveFire.CrewExposed(unit) : VehicleMgChoices(unit).Any(weapon => weapon.Bar is null)));
""")
e.edit(PAGE, """        if (weapons.Count > 0)
        {
            arguments["weapons"] = weapons;
""", """        // D3.5 (pass 50): the BMG and CMG each ticked tank fires.
        foreach (var group in FireVehicleMgs.Where(weapon => weapon.Bar is null && fireWeapons.Contains(weapon.Id)).GroupBy(weapon => weapon.Holder))
        {
            weapons[group.Key] = [.. group.Select(weapon => VehicleMgName(weapon.Id))];
        }

        if (weapons.Count > 0)
        {
            arguments["weapons"] = weapons;
""")
e.edit(PAGE, """        fireWeapons.IntersectWith(FireWeapons.Select(item => item.Id));
        fireAlone.IntersectWith(fireFirers);
""", """        fireWeapons.IntersectWith(FireWeapons.Select(item => item.Id).Concat(FireVehicleMgs.Select(item => item.Id)));
        fireAlone.IntersectWith(fireFirers);
""")
e.edit(PAGE, """        .. ActingUnits.Where(unit => unit.Status == InstanceStatus.Active && Mine(unit) && LiveFire.IsVehicle(unit) && LiveFire.CrewExposed(unit)
                && GameState.Condition(unit, Conditions.Malfunctioned) == ConditionState.True && GameState.Condition(unit, Conditions.Disabled) != ConditionState.True)
            .Select(vehicle => new PlayChoice(vehicle.Id, $"{vehicle.Id}'s AAMG (a dr of 1 repairs it, a 6 disables it; D3.7)")),
    ];
""", """        .. ActingUnits.Where(unit => unit.Status == InstanceStatus.Active && Mine(unit) && GamePlanner.HasVehicleMg(unit) && LiveFire.CrewExposed(unit)
                && GameState.Condition(unit, Conditions.Malfunctioned) == ConditionState.True && GameState.Condition(unit, Conditions.Disabled) != ConditionState.True)
            .Select(vehicle => new PlayChoice(vehicle.Id, $"{vehicle.Id}'s AAMG (a dr of 1 repairs it, a 6 disables it; D3.7)")),

        // D3.7 (pass 50): a tank's Gun MA, BMG, and CMG, each by its own dr; a disabled MA Recalls a tank.
        .. ActingUnits.Where(unit => unit.Status == InstanceStatus.Active && Mine(unit) && LiveOrdnance.IsTank(unit))
            .SelectMany(tank => new[]
            {
                (Weapon: "ma", Name: "MA", Broken: Conditions.Malfunctioned, Gone: Conditions.Disabled),
                (Weapon: "bmg", Name: "BMG", Broken: Conditions.BmgMalfunctioned, Gone: Conditions.BmgDisabled),
                (Weapon: "cmg", Name: "CMG", Broken: Conditions.CmgMalfunctioned, Gone: Conditions.CmgDisabled),
            }.Where(item => GameState.Condition(tank, item.Broken) == ConditionState.True && GameState.Condition(tank, item.Gone) != ConditionState.True)
                .Select(item => new PlayChoice(tank.Id + "|" + item.Weapon,
                    $"{tank.Id}'s {item.Name} (a dr of 1 repairs it, a 6 disables it{(item.Weapon == "ma" ? " and Recalls the tank" : "")}; D3.7)"))),
    ];
""")
e.edit(PAGE, """    private Task ProposeRepair() => state?.Find(repairWeapon) switch
    {
""", """    private Task ProposeRepair() => repairWeapon.Split('|') is [var tankId, var tankWeapon]
        // D3.7 (pass 50): a tank's weapon by its name.
        ? Propose(GameActions.Repair, JsonSerializer.SerializeToElement(new
        {
            gameId, attemptId = Attempt("repair"), expectedRevision = (long)history!.Events.Count, unitId = tankId, equipmentId = tankId, weapon = tankWeapon,
        }))
        : state?.Find(repairWeapon) switch
    {
""")

e.apply()
