namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Ordnance rules that Play decided over the state before pass 32.d (slice S7): what a live Gun's, mortar's, or PF's shot may be, from facts
/// Play reads. The recorded records of a shot keep their shape (the pass 32 design, D5); the map reads stay in Play.
/// </summary>
public static class ScenarioA1OrdnanceEligibility
{
    /// <summary>Whether a vehicle has a MA the Ordnance package reviews (D1.3; ruling R7.1): a tank of the catalog, its Gun type read only for a vehicle.</summary>
    public static bool IsTank(bool vehicle, Func<string?> gunType)
    {
        ArgumentNullException.ThrowIfNull(gunType);
        return vehicle && gunType() == "vehicle";
    }

    /// <summary>
    /// The PF shots a side may take in a scenario (C13.31; ruling R9.7): its squad equivalents at setup before 1944, one and a half times as many
    /// (FRD) in 1944, and twice as many in 1945, from its half-squads at setup; a scenario with no year counts as before 1944.
    /// </summary>
    public static int PanzerfaustAllowance(int halfSquads, int? scenarioYear) => scenarioYear switch
    {
        >= 1945 => halfSquads,
        1944 => halfSquads * 3 / 4,
        _ => halfSquads / 2,
    };

    /// <summary>Whether a Gun fires at its Bore Sighted Location with its original crew from its setup Location (C6.43); Locations compare by their own equality.</summary>
    public static bool BoreSighted<TLocation>(IEnumerable<(string Gun, TLocation Location, string Crew, TLocation SetupLocation)> sightings, string gun, string crew, TLocation from, TLocation target)
    {
        ArgumentNullException.ThrowIfNull(sightings);
        var equality = EqualityComparer<TLocation>.Default;
        return sightings.Any(item => item.Gun == gun && equality.Equals(item.Location, target) && item.Crew == crew && equality.Equals(item.SetupLocation, from));
    }

    /// <summary>Whether a Gun is Emplaced (C11.2, C11.3): manned by a crew (the manning unit's kind, null when not manned), never moved or hooked up.</summary>
    public static bool Emplaced(string? mannedByKind, Func<bool> unemplaced)
    {
        ArgumentNullException.ThrowIfNull(unemplaced);
        return mannedByKind == "asl:crew" && !unemplaced();
    }

    /// <summary>A turreted AFV's TCA (D3.12; ruling R7.10): its recorded turret facing, or its VCA; facings as ints.</summary>
    public static int? TurretFacing(int? recorded, int? hull) => recorded ?? hull;

    /// <summary>
    /// A vehicle target's state facts (C6.1 Case J, C.8, D5.5, D5.6): a vehicle that entered a new hex or moved in Motion this Player Turn, or is in
    /// Motion, is moving; a Stunned, Shocked, or Recalled crew adds one to Crew Survival. The Target Facings are the planner's map reads.
    /// </summary>
    public static OrdnanceVehicleTarget VehicleTarget(OrdnanceVehicleTargetStateFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return new OrdnanceVehicleTarget(facts.Id, facts.Definition, null, null,
            // C6.3, D2.13 (pass 35, task 35.13): Non-Stopped is under a Motion counter, or started in its MPh and not stopped since; never while
            // bogged or immobilized, whose Bog Removal is its Start MP (D8.3).
            facts.Motion || facts.MovedThisPlayerTurn, facts.Motion || (facts.MovingUnstopped && !facts.Bogged && !facts.Immobilized),
            facts.Concealed || facts.Hidden,
            facts.Stunned || facts.Shocked || facts.UnconfirmedKill || facts.Recalled,
            // D5.5: a Stunned or Shocked crew takes no Immobilization TC, nor does an absent crew or one already immobilized.
            !facts.Stunned && !facts.Shocked && !facts.UnconfirmedKill && !facts.Abandoned && !facts.Immobilized)
        {
            Abandoned = facts.Abandoned ? true : null,
            StunRecovery = facts.StunRecovery ? true : null,
            Bypass = facts.Bypass ? true : null,
            CrewMorale = facts.CrewMorale,
            CrewExposed = facts.CrewExposed ? true : null,
        };
    }

    /// <summary>
    /// A light mortar's Spotter and directing leader (C9.3, A7.531; rulings R9.2, R9.4): the Spotter is an active unit of the firer's side, the one
    /// already spotting for the mortar while it is Good Order (<paramref name="keptSpotter"/>), unless it is the firer; the leader is an active leader
    /// in the firer's Location. Adjacency and LOS are the planner's map reads.
    /// </summary>
    public static ((OrdnanceSpotter? Spotter, FireDirector? Director)? Support, string? Reason) MortarSupport(string gun, string? keptSpotter, string? spotter,
        Func<OrdnanceSpotterFacts> spotterFacts, string? director, Func<OrdnanceDirectorFacts> directorFacts, string phase)
    {
        ArgumentNullException.ThrowIfNull(spotterFacts);
        ArgumentNullException.ThrowIfNull(directorFacts);
        OrdnanceSpotter? spotting = null;
        if (spotter is not null)
        {
            var unit = spotterFacts();
            if (!unit.ActiveWithDefinition || !unit.SameSide || unit.Captured || unit.Melee)
            {
                return (null, $"play.ordnance-spotter: '{spotter}' is not an active unit of the firer's side (C9.3)");
            }

            // C9.3 (ruling R9.4): a new Spotter is named only when the original is broken, eliminated, or captured.
            if (keptSpotter is not null && keptSpotter != unit.Id)
            {
                return (null, $"play.ordnance-spotter-kept: {keptSpotter} spots for {gun} while it is Good Order (C9.3)");
            }

            // C9.31 EX, A7.352 (table player, pass 9): a unit spots for one mortar a phase; a HS, crew, or SMC that has fired does not spot.
            if (unit.SpottedOtherMortar || (!unit.Squad && unit.FiredOrFirstFire && !unit.SpottedThisPhase))
            {
                return (null, $"play.ordnance-spotter-used: {unit.Id} has spotted for another mortar or fired this phase (C9.31, A7.352)");
            }

            if (unit.Squad && !unit.SpottedThisPhase && SquadSupportRefusal(unit.Support, phase, false) is { } spotterRefusal)
            {
                return (null, spotterRefusal);
            }

            spotting = new OrdnanceSpotter(unit.Id, unit.Definition, unit.Broken, unit.Pinned);
        }

        FireDirector? directing = null;
        if (director is not null)
        {
            var leader = directorFacts();
            if (!leader.ActiveWithDefinition || !leader.SameSide || leader.At is null || !leader.AtFirer)
            {
                return (null, $"play.ordnance-director: '{director}' is not an active unit of the firer's side in its Location (A7.531)");
            }

            // A7.53 (referee, pass 9): the leader directing a SW this phase goes on directing its further ROF shots.
            directing = new FireDirector(leader.Id, leader.Definition, leader.At, leader.Broken, leader.Pinned, leader.Concealed,
                leader.FiredOrFirstFire && !leader.Continuing, leader.Wounded);
        }

        return ((spotting, directing), null);
    }

    /// <summary>
    /// Why a squad may not use a SW now (A7.351, C13.31; table player, pass 9): it fired in an earlier phase (a Prep or Final Fire counter it did
    /// not get this phase), it fired its inherent FP with a SW this phase, or, for a PF, it is marked First Fire in the MPh (no PF Check in
    /// Subsequent First Fire). Null when it may.
    /// </summary>
    public static string? SquadSupportRefusal(SquadSupportFacts squad, string phase, bool panzerfaust)
    {
        ArgumentNullException.ThrowIfNull(squad);
        if (!squad.Squad)
        {
            return null;
        }

        if (phase == "MPh" ? squad.FirstFire && (panzerfaust || (!squad.FiredHere && !squad.UsedHere))
            : squad.Fired && !squad.FiredHere && !squad.UsedHere)
        {
            return $"play.sw-fired: {squad.Id} fired earlier{(phase == "MPh" ? " this MPh" : string.Empty)} and may not use a SW now (A7.351, C13.31)";
        }

        return squad.FiredHere && squad.FiredHereWeapon is not "0"
            ? $"play.sw-limit: {squad.Id} fired its inherent FP with a SW this phase and uses no other (A7.351)"
            : null;
    }

    /// <summary>The fire phase a Gun, mortar, LATW, or PF fires in (C5.2, C13.31), from the state's phase; null outside the PFPh, MPh, DFPh, and AFPh.</summary>
    public static string? FirePhase(string? phase) => phase switch
    {
        "pfph" => "PFPh",
        "dfph" => "DFPh",
        "afph" => "AFPh",
        "mph" => "MPh",
        _ => null,
    };

    /// <summary>Why the Ordnance package does not read this game's catalog; null when it does.</summary>
    public static string? CatalogRefusal(string packageCatalog, string packageVersion, string gameCatalog, string gameVersion) =>
        gameCatalog != packageCatalog || gameVersion != packageVersion
            ? $"play.ordnance-catalog: the Ordnance package reads {packageCatalog}@{packageVersion}, and this game uses {gameCatalog}@{gameVersion}"
            : null;

    /// <summary>The refusal of a Gun shot outside a fire phase (C5.2).</summary>
    public const string PhaseRefusal = "play.ordnance-phase: a Gun fires in the PFPh, DFPh, or AFPh (C5.2; Defensive First Fire by ordnance is not reviewed)";

    /// <summary>The refusal of a shot by what is not a Gun, a manned Gun, a possessed light mortar or LATW, nor a tank (A21.13, C2.1, D1.3).</summary>
    public static string GunRefusal(string gunId) =>
        $"play.ordnance-gun: '{gunId}' is not an active Gun from the catalog on the map, manned by an active unit, nor a tank (A21.13, C2.1, D1.3)";

    /// <summary>Why a tank's MA does not fire (D1.3, D5.41): an Abandoned AFV has no crew. Null when it may.</summary>
    public static string? TankRefusal(string tank, bool abandoned) =>
        abandoned ? $"play.ordnance-abandoned: {tank} is Abandoned, and its MA has no crew to fire it (D5.41)" : null;

    /// <summary>
    /// Why a light mortar's possessor does not fire it (C9.2, A4.41; rulings R9.2): not in the AFPh after it moved, nor by a squad barred from a
    /// SW (<paramref name="squadRefusal"/>, read only when the mortar did not move). Null when it may.
    /// </summary>
    public static string? LightMortarRefusal(string mortar, string phase, Func<bool> moved, Func<string?> squadRefusal)
    {
        ArgumentNullException.ThrowIfNull(moved);
        ArgumentNullException.ThrowIfNull(squadRefusal);
        if (phase == "AFPh" && moved())
        {
            return $"play.ordnance-mortar-moved: {mortar} moved in the MPh, so it does not fire in the AFPh (A4.41)";
        }

        return squadRefusal();
    }

    /// <summary>
    /// Why an ATR or PSK is not fired by its possessor (C13.8, C13.2, C13.4, C13.41; rulings R9.10, R9.11; table player, pass 9b): not from inside a
    /// vehicle, not again after it fired (no Multiple ROF), nor by a squad barred from a SW. The later facts are read only when asked. Null when it may.
    /// </summary>
    public static string? LatwRefusal(string latw, string carrier, bool carrierOnMap, Func<bool> fired, Func<string?> squadRefusal)
    {
        ArgumentNullException.ThrowIfNull(fired);
        ArgumentNullException.ThrowIfNull(squadRefusal);
        if (!carrierOnMap)
        {
            return $"play.latw-passenger: {carrier} is not on the map, and a LATW is not fired from inside a vehicle (C13.8)";
        }

        if (fired())
        {
            return $"play.latw-fired: {latw} has fired and has no Multiple ROF (C13.2, C13.4)";
        }

        return squadRefusal();
    }

    /// <summary>
    /// Why a Gun and its crew do not fire (A4.8, C10.3, C10.12, C3.22, A11.15, A20.5; table player, pass 8): a TI Gun or crew, a Gun turned
    /// without firing this phase, and a crew held in Melee or captured. Null when they may.
    /// </summary>
    public static string? HaltedRefusal(string gun, string crew, bool gunTi, bool crewTi, Func<bool> turned, bool crewMelee, bool crewCaptured)
    {
        ArgumentNullException.ThrowIfNull(turned);
        if (gunTi || crewTi || turned())
        {
            return $"play.ordnance-halted: '{gun}' is TI, or changed its CA without firing this phase, and does not fire (A4.8, C3.22, C10.3)";
        }

        return crewMelee || crewCaptured
            ? $"play.ordnance-crew: '{crew}' is held in Melee or captured, so it does not fire its Gun (A11.15, A20.5)"
            : null;
    }

    /// <summary>The refusal of a Gun's or tank's MPh shot outside the DEFENDER's open window on the moving stack's Location (A8.1, C6.1; ruling R8.1).</summary>
    public const string MphWindowRefusal =
        "play.ordnance-window: in the MPh a Gun or tank of the DEFENDER fires at the moving stack in its Location, while the window on its MF or MP expenditure is open (A8.1, C6.1)";

    /// <summary>The refusal of Canister in the MPh (C8.4; pass 55): Defensive First Fire with Canister is not built; the fire phases are.</summary>
    public const string CanisterMphRefusal =
        "play.canister-movement-phase: Canister as Defensive First Fire in the MPh is not built; fire it in the PFPh, AFPh, or DFPh, or fire HE at the moving stack (C8.4)";

    /// <summary>The refusal of a Canister shot that names no aim of 1 to 5 (C8.4 and its EX; ruling R55.8).</summary>
    public const string CanisterAimRefusal =
        "play.canister-aim: a Canister shot names one of the five aims of the Gun's Covered Arc, 1 to 5 from left to right (C8.4)";

    /// <summary>The refusal of a Canister shot none of whose Locations can be attacked (C8.4, C8.41).</summary>
    public static string CanisterEmptyRefusal(int aim, string facing) =>
        $"play.canister-aim: no Location of the three-hex cluster of {ScenarioA1CanisterCalculator.AimWords(aim, facing)} is on the map, at the Gun's elevation, and in its LOS, "
            + "so Canister attacks nothing there, the adjacent hex on its LOS included (C8.4, C8.41)";

    /// <summary>
    /// Whether a ground-level Location of a hex is at the elevation a Canister shot is fired at (C8.4: "a common vertex and elevation shared by
    /// three hexes", attacking "all occupants of all hexes ... at that elevation"; ruling R55.9): the hex's Base Level is the firer's elevation.
    /// </summary>
    public static bool CanisterElevation(int firerElevation, int hexBaseLevel) => firerElevation == hexBaseLevel;

    /// <summary>
    /// Why a Canister record's aim does not fit the Gun (C8.4; ruling R55.9): its facing is the one the record turns the Gun to, or the Gun's own
    /// when it turns none. Null when it does.
    /// </summary>
    public static string? CanisterFacingRefusal(string? recordedFacing, string? facingAfterShot) =>
        recordedFacing is not null && recordedFacing != facingAfterShot ? "The Canister record's aim is not in the Covered Arc the Gun fires in." : null;

    /// <summary>Whether an MPh shot is outside the DEFENDER's open window on the target Location (A8.1, C6.1); the window's Location is read only when open.</summary>
    public static bool OutsideMphWindow(bool phasing, bool windowOpen, Func<bool> windowAtTarget)
    {
        ArgumentNullException.ThrowIfNull(windowAtTarget);
        return phasing || !windowOpen || !windowAtTarget();
    }

    /// <summary>The refusal of a named vehicle target that is not an active enemy vehicle in the target Location (C3.31).</summary>
    public static string VehicleTargetRefusal(string targetVehicle, string target) =>
        $"play.ordnance-vehicle-target: '{targetVehicle}' is not an active enemy vehicle in {target} (C3.31)";

    /// <summary>
    /// Whether a unit in the target Location is a target of a shot at it (C3.33, C3.4; ruling R9.3): an active non-vehicle unit not captured, of the
    /// enemy side or any side for a mortar's Area Target Type, and in the MPh only a moving unit. The later facts are read only when asked.
    /// </summary>
    public static bool AreaTarget(bool active, bool enemy, bool mortar, Func<bool> vehicle, Func<bool> captured, Func<bool>? mover)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(captured);
        return active && (enemy || mortar) && !vehicle() && !captured() && (mover is null || mover());
    }

    /// <summary>
    /// Whether a vehicle in the target Location is attacked by an Infantry Target Type shot's hit (C3.32, C3.41, p. 169; pass 60, ruling R60.8): an
    /// active enemy vehicle, outside the MPh. In the MPh only the moving units are a shot's targets (C3.4), and a moving vehicle beside them is
    /// refused. The vehicle fact is read only when asked.
    /// </summary>
    public static bool LocationVehicle(bool infantryTargetType, bool active, bool enemy, bool mph, Func<bool> vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return infantryTargetType && active && enemy && !mph && vehicle();
    }

    /// <summary>
    /// Why a shot that names no vehicle is refused at a Location holding one (C3.31, C3.41, D.6; pass 60, ruling R60.8), or null: in the MPh a moving
    /// vehicle beside the moving Infantry is not built; a PF's or an ATR's hit there is not built; and a Location of AFVs alone, with no Infantry
    /// and no unarmored vehicle, is the Vehicle Target Type's, since the Infantry Target Type "cannot be used to attack an AFV".
    /// </summary>
    public static string? LocationVehicleRefusal(string vehicle, string target, bool mph, bool panzerfaustOrAtr, bool personnel, bool unarmoredVehicle) =>
        mph ? $"play.ordnance-vehicle: {vehicle} moves in {target}; fire at it on the Vehicle Target Type by naming it, since a Defensive First Fire hit's "
                + "attack on a moving vehicle beside the moving Infantry is not built (C3.31; rulings R25.10, R60.8)"
            : panzerfaustOrAtr ? $"play.ordnance-vehicle: {vehicle} is in {target}; a PF's or an ATR's hit on a Location that holds a vehicle is not built, "
                + "so fire at the vehicle by naming it (C13.31, C13.23; ruling R60.8)"
            : !personnel && !unarmoredVehicle ? $"play.ordnance-vehicle: {target} holds only AFVs, which the Infantry Target Type cannot attack; fire at one on "
                + "the Vehicle Target Type by naming it (C3.41, D.6)"
            : null;

    /// <summary>
    /// Whether a vehicle of the target hex is attacked by a mortar's Area Target Type hit (C3.332, C1.55, p. 170; pass 60, ruling R60.10): an active
    /// vehicle of either side, the firer's own refused. The vehicle fact is read only when asked.
    /// </summary>
    public static bool AreaVehicle(bool mortar, bool active, Func<bool> vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return mortar && active && vehicle();
    }

    /// <summary>
    /// Why a mortar's shot is refused at a hex holding a vehicle (C3.332; pass 60, ruling R60.10), or null: a vehicle in Bypass, whose terrain is not the
    /// hex's, and Defensive First Fire at a moving vehicle are not built.
    /// </summary>
    public static string? AreaVehicleRefusal(string vehicle, bool bypass, bool mph) =>
        bypass ? $"play.ordnance-area-vehicle: {vehicle} is in Bypass in the target hex; a mortar's hit on a vehicle in Bypass is not built (B13.3, D2.3; ruling R60.12)"
            : mph ? $"play.ordnance-area-vehicle: {vehicle} is in the target hex; a mortar's Defensive First Fire hit beside a moving vehicle is not built (C3.332; ruling R60.10)"
            : null;

    /// <summary>The refusal of a target Location holding a unit outside the catalog.</summary>
    public const string TargetOutsideCatalogRefusal = "play.ordnance-target: the target Location holds a unit outside the catalog";

    /// <summary>
    /// Whether a crew has fired its inherent FP and so does not fire its Gun (A7.352, A7.351; rulings R8.4, R9.2): a counter's crew with a fire counter
    /// it did not get from its Gun, a squad with a SW excepted. The later facts are read only when asked.
    /// </summary>
    public static bool CrewFiredInherent(bool equipment, Func<bool> firedOrFirstFire, Func<bool> firedItsGun, bool sw, bool squad)
    {
        ArgumentNullException.ThrowIfNull(firedOrFirstFire);
        ArgumentNullException.ThrowIfNull(firedItsGun);
        return equipment && firedOrFirstFire() && !firedItsGun() && !(sw && squad);
    }

    /// <summary>
    /// Whether a shot records the scenario year (pass 55): a shot at a vehicle or with a named ammunition always did, for the Depletion Number
    /// (C8.9); a shot of a nationality whose Ordnance TH# color turns with the year (the American, red before 1944 and black from 1944; the
    /// National Capabilities Chart, p. 695) does too. No other shot gains the fact, so a record made before the pass reads as it did.
    /// </summary>
    public static bool RecordsYear(bool vehicleTarget, bool ammunitionNamed, string? firingNationality) =>
        vehicleTarget || ammunitionNamed || firingNationality == "american";

    /// <summary>Whether the Gun's crew is non-qualified (C5.8; ruling R8.8): a squad or HS manning a Gun, not a SW.</summary>
    public static bool? NonQualified(bool equipment, string? crewKind, bool sw) => equipment && crewKind is not "asl:crew" && !sw ? true : null;

    /// <summary>The refusal of a Spotter or director named for what is not a light mortar or SW (C9.3, A7.531).</summary>
    public const string SupportRefusal = "play.ordnance-support: only a light mortar has a Spotter, and only a SW a directing leader (C9.3, A7.531)";

    /// <summary>
    /// The MPh movement facts of a shot at a moving stack (C6.13, C6.16, C6.17): non-Assault Movement for a shot at Personnel, the MF or MP spent in
    /// the Location (FRD), and the Gun's shots there so far; for a vehicle target, the MP it claimed.
    /// </summary>
    public static OrdnanceMovement MphMovement(bool vehicleTarget, bool assault, int halfMfInLocation, int? shotsHere, int? mpHere) =>
        new(null, vehicleTarget ? null : !assault, null, halfMfInLocation / 2, shotsHere ?? 0)
        {
            MpClaimed = vehicleTarget && mpHere is > 0 ? mpHere : null,
        };

    /// <summary>A7.25 (p. 55; pass 35, task 35.10): ordnance uses Opportunity Fire only if fired by Infantry, in the AFPh, from beneath its Bounding Fire counter.</summary>
    public static bool? OpportunityFirer(string? phase, bool infantry, bool boundingFire) => phase == "afph" && infantry && boundingFire ? true : null;

    /// <summary>
    /// A tank's MA beside its other weapons, from the vehicle's shots of this Player Turn (D3.5, D3.51; pass 50): a shot in another fire phase
    /// forfeits the MA's; a fire counter is the other weapons' alone when the MA has no shot yet; and the MA's first shot of the fire phase pays
    /// the Case A the CMG paid before it.
    /// </summary>
    public static OrdnanceVehicleWeapons VehicleWeapons(IReadOnlyList<VehicleWeaponShotFacts> shots, string firePhase, string? maType, bool boundingFireMarked = false)
    {
        ArgumentNullException.ThrowIfNull(shots);

        // D3.32 (p. 200; pass 50): "if the vehicle did not exhaust its Multiple ROF during its MPh, and did not fire any other weapons ... during its
        // MPh, it may fire that Multiple ROF weapon (only) again once during its AFPh". A Bounding Fire counter says the ROF is spent (D3.3).
        var afterBounding = firePhase == "afph" && !boundingFireMarked && shots.Count > 0 && shots.All(item => item.Weapon == "ma" && item.FirePhase == "mph");
        // D3.51 (review of pass 50): the MA pays the CMG's Case A on its first shot after the CMG turned the turret, whether or not it fired before.
        var lastTurn = shots.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.FirePhase == firePhase && pair.item.Weapon != "ma" && pair.item.Turn > 0);
        var paid = lastTurn.item is null || shots.Skip(lastTurn.index + 1).Any(item => item.Weapon == "ma" && item.FirePhase == firePhase);
        return new OrdnanceVehicleWeapons(!afterBounding && shots.Any(item => item.FirePhase != firePhase), shots.Count > 0 && shots.All(item => item.Weapon != "ma"),
            paid ? null : ScenarioA1FireEligibility.MaintainedCaseA([lastTurn.item!], firePhase, maType))
        {
            BoundingFireSpent = boundingFireMarked && shots.Any(item => item.Weapon == "ma" && item.FirePhase == "mph") ? true : null,
        };
    }

    /// <summary>
    /// A firing vehicle's facts (D5.2, D5.34, C7.42, D5.341, D2.4; rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed. Pass 50 (D2.42,
    /// D3.32): the moving vehicle that has not stopped is a Motion firer too, and one that fired its MA in this MPh is a Bounding Firer in the AFPh.
    /// </summary>
    public static OrdnanceVehicleFirer VehicleFirer(bool crewExposed, bool motion, bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery,
        bool moved, bool movingUnstopped = false, bool boundingFirer = false, int? armorLeadership = null) =>
        new(!crewExposed, motion || movingUnstopped, stunned, shocked || unconfirmedKill, recalled)
        {
            StunRecovery = stunRecovery ? true : null,
            Moved = moved ? true : null,
            BoundingFirer = boundingFirer ? true : null,

            // D3.44, D3.45 (p. 200; pass 55, rulings R55.16, R55.18): the Armor Leader's modifier on the MA's To Hit DR, or an Inexperienced Crew's +1.
            ArmorLeadership = armorLeadership,
        };

    /// <summary>
    /// The kind of a shot in the MPh (D3.3, A8.1, C6.1; pass 50, ruling R8.1): the phasing side's vehicle fires its MA as Bounding First Fire when it
    /// may (at the outset of its MPh, or once the DEFENDER has passed on its last MP expenditure); the other side fires as Defensive First Fire at
    /// the moving stack while its window is open. The refusal otherwise.
    /// </summary>
    public static (string? Kind, string? Refusal) MphShotKind(bool phasing, bool vehicle, Func<bool> mayBoundingFire, bool windowOpen, Func<bool> windowAtTarget)
    {
        ArgumentNullException.ThrowIfNull(mayBoundingFire);
        ArgumentNullException.ThrowIfNull(windowAtTarget);
        if (phasing)
        {
            return vehicle && mayBoundingFire() ? (ScenarioA1FireCalculator.BoundingFirstFire, null)
                : (null, vehicle ? "play.ordnance-bounding: in its own MPh a vehicle's MA fires as Bounding First Fire, at the outset of its move or once the DEFENDER has passed on its last MP expenditure (D3.3)"
                    : MphWindowRefusal);
        }

        return OutsideMphWindow(false, windowOpen, windowAtTarget) ? (null, MphWindowRefusal) : ("first-fire", null);
    }

    /// <summary>
    /// C5.3 (p. 172; pass 50): "a vehicle with a Multiple ROF may not fire again until it has expended another MP (even if only a Delay MP)". The
    /// movement step of its last Bounding First Fire shot against the step now; null when it has made none.
    /// </summary>
    public static string? BoundingShotBar(string vehicleId, int? lastShotStep, int stepNow, bool rofSpent = false) =>
        // A vehicle under a Bounding Fire counter has spent its ROF: the calculator says so, in its own words.
        !rofSpent && lastShotStep is { } last && stepNow <= last
            ? $"play.ordnance-bounding-mp: {vehicleId} spends another MP, a Delay MP will do, before its MA fires again in Bounding First Fire (C5.3)"
            : null;

    /// <summary>The refusal of a PF fired by what is not an active unit on the map in a fire phase or the MPh (C13.31).</summary>
    public const string PanzerfaustFirerRefusal = "play.panzerfaust-firer: a PF is fired by an active unit on the map in a fire phase or the MPh (C13.31)";

    /// <summary>Why a PF's firer does not fire (A11.15, A4.8): held in Melee, captured, or TI. Null when it may.</summary>
    public static string? PanzerfaustHaltedRefusal(string unit, bool melee, bool captured, bool ti) =>
        melee || captured || ti ? $"play.panzerfaust-firer: '{unit}' is held in Melee, captured, or TI, and does not fire (A11.15, A4.8)" : null;

    /// <summary>The refusal of a PF shot at what is not an enemy AFV named in the target Location (C13.3; ruling R9.8).</summary>
    public static string PanzerfaustTargetRefusal(string target) => $"play.panzerfaust-target: a PF fires at an enemy AFV named in {target} (C13.3; ruling R9.8)";

    /// <summary>The refusal of a PF's MPh shot outside the DEFENDER's open window on the moving vehicle's Location (A8.1, C13.31).</summary>
    public const string PanzerfaustWindowRefusal =
        "play.ordnance-window: in the MPh a PF of the DEFENDER fires at the moving vehicle in its Location, while the window on its MP expenditure is open (A8.1, C13.31)";

    /// <summary>
    /// Whether a PF's firer counts as having fired (C13.31, A7.351; referee, pass 9): a squad once it made a PF Check, unless that is its only fire this
    /// phase; any other unit once it fired, or in the MPh once marked First Fire. The other unit's marks are read only when asked.
    /// </summary>
    public static bool PanzerfaustFired(bool squad, int checks, bool onlyPanzerfaust, Func<bool> fired, string phase, Func<bool> firstFire)
    {
        ArgumentNullException.ThrowIfNull(fired);
        ArgumentNullException.ThrowIfNull(firstFire);
        return squad ? checks > 0 && !onlyPanzerfaust : fired() || (phase == "MPh" && firstFire());
    }

    /// <summary>
    /// The expected shot with the recorded map facts taken as recorded (C6.11, C6.14, C5.5, C6.4, C11, C13.8; rulings R5.8, R16.12 to R16.14):
    /// range, the Covered Arc, the firer's terrain, the elevation limit, levels, LOS, the target terrain, each target's LOS to a Known enemy and its
    /// captors, the owners' answers, the weather cushion and Extreme Winter B#, the Target Facings, the MP in LOS and Open Ground, the own-hex shot,
    /// and a PF fired from a ground-level building.
    /// </summary>
    public static OrdnanceShot WithRecordedMapFacts(OrdnanceShot expected, OrdnanceShot recorded)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(recorded);
        return expected with
        {
            Range = recorded.Range,
            HexspinesToTurn = recorded.HexspinesToTurn,
            FirerInWoodsOrBuilding = recorded.FirerInWoodsOrBuilding,
            ElevationAllowed = recorded.ElevationAllowed,
            Hit = expected.Hit! with
            {
                SameLevel = recorded.Hit!.SameLevel,
                Los = recorded.Hit.Los,
                TargetTerrain = recorded.Hit.TargetTerrain,
                Targets = recorded.Hit.Targets is null ? expected.Hit.Targets
                    : [.. expected.Hit.Targets!.Zip(recorded.Hit.Targets, (fact, record) => fact with
                    {
                        KnownEnemyInLos = record.KnownEnemyInLos,
                        Captors = record.Captors,

                        // A7.72 (pass 60): an upper-level target's Encirclement is the planner's read of the map, recorded with the shot, as an IFT
                        // attack's is (LiveFire); a mortar's hit on a building's upper level reads it.
                        Encircled = fact.Encircled ?? record.Encircled,
                    })],

                // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
                Choices = recorded.Hit.Choices,

                // Backlog pass 16 (rulings R16.12, R16.13): the weather cushion of the hit's attack, from the SSRs.
                CushionedOpenGround = recorded.Hit.CushionedOpenGround,

                // D9.3 (pass 60, ruling R60.10): the AFV or wreck a mortar's target Infantry shelter beside, a map read; none in a record made before it.
                AfvCover = recorded.Hit.AfvCover,
            },

            // The facts of pass 60 that gate its rules for older records (ruling R60.3): C3.71's reversal of Height Advantage, a mortar's building
            // Locations (B23.32), and its Area Acquisition of the hex (C6.521); a record made before the pass has none and resolves as it did.
            ReversesHeightAdvantage = recorded.ReversesHeightAdvantage,
            AreaLocations = recorded.AreaLocations,
            AreaAcquisition = recorded.AreaAcquisition,

            // Backlog pass 16 (rulings R16.12 to R16.14): the weather cushion and the Extreme Winter B#, from the SSRs.
            CushionedOpenGround = recorded.CushionedOpenGround,
            BreakdownReduction = recorded.BreakdownReduction,
            LowVisibilityDrm = recorded.LowVisibilityDrm,
            VehicleTarget = expected.VehicleTarget is null ? null : expected.VehicleTarget with
            {
                HullFacing = recorded.VehicleTarget?.HullFacing,
                TurretFacing = recorded.VehicleTarget?.TurretFacing,

                // D.8A (pass 60, ruling R60.3): the crew's exposure is read from the state only for a shot whose record carries the rule's fact.
                CrewExposed = recorded.SpecificCollateral == true ? expected.VehicleTarget.CrewExposed : null,
            },

            // The fact of pass 60 that gates the Specific Collateral Attack (D.8A); a record made before it has none and attacks no crew.
            SpecificCollateral = recorded.SpecificCollateral,

            // The map reads of pass 8: the MP in the firer's LOS and Open Ground (C6.11, C6.14), the own-hex shot (C5.5), Bore Sighting (C6.4),
            // overstacking (A5.12, A5.131), and the Gun in the target Location with its Emplacement and gunshield (C11).
            Movement = expected.Movement is null ? null : expected.Movement with
            {
                MpInLos = recorded.Movement?.MpInLos,
                OpenGround = recorded.Movement?.OpenGround,
                Hazardous = recorded.Movement?.Hazardous,
                Dashing = recorded.Movement?.Dashing,
                InBogHex = recorded.Movement?.InBogHex,
            },
            SameHex = recorded.SameHex,

            // C8.4 (pass 55): a Canister shot's aim and the Locations it attacks are the planner's reads of the map.
            Canister = recorded.Canister,
            CrewSeen = recorded.CrewSeen,
            HazardousTarget = recorded.HazardousTarget,

            // The map read of pass 55: Height Advantage (B10.31); a record made before it has none.
            HeightAdvantage = recorded.HeightAdvantage,

            // The map read of pass 60: a wall's TEM for HEAT at Infantry (C8.31); a record made before it has none, and was no such shot.
            WallTem = recorded.WallTem,

            // The map read of pass 9: a PF fired from a ground-level building (C13.8).
            Panzerfaust = expected.Panzerfaust is null ? null : expected.Panzerfaust with
            {
                FromBuilding = recorded.Panzerfaust?.FromBuilding
            },
        };
    }

    /// <summary>
    /// The merged shot with the state reads of pass 8 recomputed (C6.43, A5.12, A5.131, C11; table player, pass 8): Bore Sighting, the firer's and,
    /// for a shot at Personnel, the target's overstacking, and the recorded Gun target's Emplacement when the Gun is in the state
    /// (<paramref name="gunTargetEmplaced"/> null otherwise). The target's overstacking and the Emplacement are read only when asked.
    /// </summary>
    public static OrdnanceShot WithRecomputedStateFacts(OrdnanceShot merged, OrdnanceShot recorded, bool boreSighted, int firerExcess, Func<int> targetExcess,
        Func<string?, bool?> gunTargetEmplaced)
    {
        ArgumentNullException.ThrowIfNull(merged);
        ArgumentNullException.ThrowIfNull(recorded);
        ArgumentNullException.ThrowIfNull(targetExcess);
        ArgumentNullException.ThrowIfNull(gunTargetEmplaced);
        return merged with
        {
            BoreSighted = boreSighted ? true : null,
            FirerOverstack = firerExcess is var over and > 0 ? over : null,
            TargetOverstack = merged.VehicleTarget is null && targetExcess() is var crowded and > 0 ? crowded : null,
            Hit = merged.Hit! with
            {
                GunTarget = recorded.Hit!.GunTarget is { } gunTarget && gunTargetEmplaced(gunTarget.GunId) is { } emplaced
                    ? gunTarget with
                    {
                        Emplaced = emplaced
                    }
                    : recorded.Hit.GunTarget,
            },
        };
    }

    /// <summary>
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
        return rest.Length == 0 ? head : rest == "maintained" ? head + " (the CA change its turret already made)"
            : rest == "height-advantage" ? head + " (Height Advantage, B10.31)" : $"{head} ({rest.Replace('-', ' ')})";
    }

    /// <summary>The hexspines between a Gun's facing now and the facing it is turned to (C3.21, C5.1, D3.12), facings as ints; zero when either is unknown.</summary>
    public static int HexspinesTurned(int? turned, int? from) =>
        turned is { } to && from is { } now ? Math.Min(Math.Abs(to - now), 6 - Math.Abs(to - now)) : 0;

    /// <summary>
    /// Why the recorded turn does not match its facts (C3.21, C5.1, D3.12): the record turns the Gun exactly the hexspines its facts say the shot
    /// changes its Covered Arc by. Null when it does.
    /// </summary>
    public static string? TurnRefusal(int? recordedHexspines, bool turnedFacing, int steps) =>
        (recordedHexspines > 0) != turnedFacing || steps != (recordedHexspines ?? 0)
            ? "The ordnance record turns the Gun exactly the hexspines its facts say the shot changes its Covered Arc by."
            : null;
}
