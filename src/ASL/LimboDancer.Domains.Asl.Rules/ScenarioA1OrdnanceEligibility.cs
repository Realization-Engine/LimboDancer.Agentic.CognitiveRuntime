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

    /// <summary>
    /// The squad equivalents (FRU) by which a side's Personnel overstack a Location for a shot, a crew or HS counting half (A5.1, A5.12). The
    /// Ordnance copy: no SMC term (the pass 32 design, section 12; it stays its own function).
    /// </summary>
    public static int OverstackExcessForOrdnance(int squads, int halfSquadsAndCrews)
    {
        var equivalents = squads + (halfSquadsAndCrews / 2m);
        return equivalents > 3 ? (int)Math.Ceiling(equivalents - 3) : 0;
    }

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
            facts.Motion || facts.MovedThisPlayerTurn, facts.Motion,
            facts.Concealed || facts.Hidden,
            facts.Stunned || facts.Shocked || facts.UnconfirmedKill || facts.Recalled,
            // D5.5: a Stunned or Shocked crew takes no Immobilization TC, nor does an absent crew or one already immobilized.
            !facts.Stunned && !facts.Shocked && !facts.UnconfirmedKill && !facts.Abandoned && !facts.Immobilized)
        {
            Abandoned = facts.Abandoned ? true : null,
            StunRecovery = facts.StunRecovery ? true : null,
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

    /// <summary>A firing vehicle's facts (D5.2, D5.34, C7.42, D5.341, D2.4; rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed.</summary>
    public static OrdnanceVehicleFirer VehicleFirer(bool crewExposed, bool motion, bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool stunRecovery,
        bool moved) =>
        new(!crewExposed, motion, stunned, shocked || unconfirmedKill, recalled)
        {
            StunRecovery = stunRecovery ? true : null,
            Moved = moved ? true : null,
        };

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
                    : [.. expected.Hit.Targets!.Zip(recorded.Hit.Targets, (fact, record) => fact with { KnownEnemyInLos = record.KnownEnemyInLos, Captors = record.Captors })],

                // The owners' answers are declared, and the projector checks them against the choices made (ruling R5.8).
                Choices = recorded.Hit.Choices,

                // Backlog pass 16 (rulings R16.12, R16.13): the weather cushion of the hit's attack, from the SSRs.
                CushionedOpenGround = recorded.Hit.CushionedOpenGround,
            },

            // Backlog pass 16 (rulings R16.12 to R16.14): the weather cushion and the Extreme Winter B#, from the SSRs.
            CushionedOpenGround = recorded.CushionedOpenGround,
            BreakdownReduction = recorded.BreakdownReduction,
            LowVisibilityDrm = recorded.LowVisibilityDrm,
            VehicleTarget = expected.VehicleTarget is null ? null : expected.VehicleTarget with
            {
                HullFacing = recorded.VehicleTarget?.HullFacing,
                TurretFacing = recorded.VehicleTarget?.TurretFacing,
            },

            // The map reads of pass 8: the MP in the firer's LOS and Open Ground (C6.11, C6.14), the own-hex shot (C5.5), Bore Sighting (C6.4),
            // overstacking (A5.12, A5.131), and the Gun in the target Location with its Emplacement and gunshield (C11).
            Movement = expected.Movement is null ? null : expected.Movement with
            {
                MpInLos = recorded.Movement?.MpInLos,
                OpenGround = recorded.Movement?.OpenGround,
                Hazardous = recorded.Movement?.Hazardous,
            },
            SameHex = recorded.SameHex,
            CrewSeen = recorded.CrewSeen,

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
