using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a live Gun's shot that the game state decides (unit step 24): the fire phase and side, the Gun manned by its crew
/// with their conditions, its shots this phase and Multiple ROF (C2.24), its fire marker, its Acquisition of the target Location
/// (C6.5), and the enemy units there. The map reads (range, the Covered Arc, the LOS, the terrain, a vehicle target's Target Facings)
/// are the planner's. Backlog pass 7: a tank fires its MA as the Gun and its own crew (D1.3), at the Infantry of a Location or at one
/// named vehicle on the Vehicle Target Type with a declared ammunition (C3.31, C8.1; rulings R7.2, R7.6, R7.10).
/// </summary>
public static class LiveOrdnance
{
    private static readonly Lazy<ScenarioA1OrdnanceReference> Reference = new(() => new ScenarioA1OrdnancePackage().Reference);

    /// <summary>
    /// The ammunition a Gun or MA of this definition may carry (C8.1): AP and HE unless its listing denies them, APCR and HEAT where it lists a
    /// Depletion Number. The year and depletion still decide each shot.
    /// </summary>
    public static IReadOnlyList<string> Ammunitions(string definition)
    {
        if (!Reference.Value.Guns.TryGetValue(definition, out var gun))
        {
            return [];
        }

        bool Lists(char letter) => gun.SpecialAmmo.Any(item => item.Length > 1 && item[0] == letter && char.IsDigit(item[1]));
        return [.. new[] { ("ap", !gun.NoAp), ("apcr", Lists('A')), ("heat", Lists('H')), ("he", !gun.NoHe) }.Where(item => item.Item2).Select(item => item.Item1)];
    }

    /// <summary>A LATW definition's type (C13.1): <c>pf</c>, <c>psk</c>, or <c>atr</c>; null for any other.</summary>
    public static string? LatwType(string definition) => Reference.Value.Guns.GetValueOrDefault(definition)?.LatwType;

    /// <summary>Whether a vehicle has a MA the Ordnance package reviews (D1.3; ruling R7.1): a tank of the catalog.</summary>
    public static bool IsTank(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return LiveFire.IsVehicle(vehicle) && vehicle.Definition is { } definition && Reference.Value.Guns.TryGetValue(definition.Definition, out var gun) && gun.GunType == "vehicle";
    }

    /// <summary>The id a PF shot is recorded under (C13.3; ruling R9.7): its firer's, with <c>:pf</c>.</summary>
    public static string PanzerfaustId(string unit) => unit + ":pf";

    /// <summary>
    /// The PF shots a side may take in a scenario (C13.31; ruling R9.7): its squad equivalents at setup before 1944, one and a half times as many
    /// (FRD) in 1944, and twice as many in 1945.
    /// </summary>
    public static int PanzerfaustAllowance(GameState state, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        var halves = state.SetupHalfSquads.GetValueOrDefault(side);
        return state.ScenarioYear switch
        {
            >= 1945 => halves,
            1944 => halves * 3 / 4,
            _ => halves / 2,
        };
    }

    /// <summary>The state's part of a shot, or the reason the Gun cannot fire from this state.</summary>
    public static (OrdnanceShot? Shot, string? Reason) FromState(GameState state, string gunId, BoardLocation target, string? targetVehicle = null,
        string? ammunition = null, bool intensive = false, string? spotter = null, string? director = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(target);
        if (gunId.EndsWith(":pf", StringComparison.Ordinal))
        {
            return Panzerfaust(state, gunId, target, targetVehicle, director);
        }

        if (state.Catalog.Catalog != LiveFire.Catalog || state.Catalog.Version != LiveFire.CatalogVersion)
        {
            return (null, $"play.ordnance-catalog: the Ordnance package reads {LiveFire.Catalog}@{LiveFire.CatalogVersion}, and this game uses {state.Catalog.Catalog}@{state.Catalog.Version}");
        }

        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            "afph" => "AFPh",
            "mph" => "MPh",
            _ => null,
        };
        if (phase is null)
        {
            return (null, "play.ordnance-phase: a Gun fires in the PFPh, DFPh, or AFPh (C5.2; Defensive First Fire by ordnance is not reviewed)");
        }

        IGameObject gun;
        UnitInstance crew;
        if (state.Find(gunId) is UnitInstance { Status: InstanceStatus.Active, Definition: not null, Position: MapPosition } tank && LiveFire.IsVehicle(tank))
        {
            // D1.3: the tank is the Gun and the crew; its MA must be a reviewed Gun (a caliber in the catalog). D5.41: an Abandoned AFV has no crew.
            if (Is(tank, Conditions.Abandoned))
            {
                return (null, $"play.ordnance-abandoned: {tank.Id} is Abandoned, and its MA has no crew to fire it (D5.41)");
            }

            gun = tank;
            crew = tank;
        }
        else if (state.Find(gunId) is EquipmentInstance { Status: InstanceStatus.Active, Kind: "asl:gun", Definition: not null, Position: MapPosition } piece
            && piece.Holding is { Role: HoldingRole.Manned } manning && state.Unit(manning.Holder) is { Status: InstanceStatus.Active, Definition: not null } holder)
        {
            gun = piece;
            crew = holder;
        }
        else if (state.Find(gunId) is EquipmentInstance { Status: InstanceStatus.Active, Kind: "asl:light-mortar", Definition: not null } lightMortar
            && lightMortar.Holding is { Role: HoldingRole.Possessed } possession && state.Unit(possession.Holder) is { Status: InstanceStatus.Active, Definition: not null } possessor)
        {
            // C9.2 (ruling R9.2): a light mortar is fired by the unit possessing it; A4.41 (referee, pass 9): not in the AFPh after it moved.
            if (phase == "AFPh" && state.MovedWeapons.Contains(lightMortar.Id, StringComparer.Ordinal))
            {
                return (null, $"play.ordnance-mortar-moved: {lightMortar.Id} moved in the MPh, so it does not fire in the AFPh (A4.41)");
            }

            if (SquadSupportRefusal(state, possessor, phase, false) is { } refusal)
            {
                return (null, refusal);
            }

            gun = lightMortar;
            crew = possessor;
        }
        else if (state.Find(gunId) is EquipmentInstance { Status: InstanceStatus.Active, Kind: "asl:latw", Definition: not null } counterLatw
            && counterLatw.Holding is { Role: HoldingRole.Possessed } carried && state.Unit(carried.Holder) is { Status: InstanceStatus.Active, Definition: not null } carrier)
        {
            // C13.8 (table player, pass 9b): not fired from inside a vehicle; its possessor stands on the map.
            if (carrier.Position is not MapPosition)
            {
                return (null, $"play.latw-passenger: {carrier.Id} is not on the map, and a LATW is not fired from inside a vehicle (C13.8)");
            }

            // Table player, pass 9b: an ATR or PSK that has fired this phase has no ROF.
            if (LiveFire.Fired(counterLatw) || Is(counterLatw, Conditions.FirstFire))
            {
                return (null, $"play.latw-fired: {counterLatw.Id} has fired and has no Multiple ROF (C13.2, C13.4)");
            }

            // C13.2, C13.41 (rulings R9.10, R9.11): an ATR or PSK is fired by the unit possessing it.
            if (SquadSupportRefusal(state, carrier, phase, false) is { } refusal)
            {
                return (null, refusal);
            }

            gun = counterLatw;
            crew = carrier;
        }
        else
        {
            return (null, $"play.ordnance-gun: '{gunId}' is not an active Gun from the catalog on the map, manned by an active unit, nor a tank (A21.13, C2.1, D1.3)");
        }

        // A4.8, C10.3, C10.12 (table player, pass 8): a TI Gun or crew does not fire; C3.22: a Gun turned without firing fires no more that phase.
        if (Is(gun, "asl:ti") || Is(crew, "asl:ti") || state.GunsTurnedThisPhase.Contains(gun.Id, StringComparer.Ordinal))
        {
            return (null, $"play.ordnance-halted: '{gun.Id}' is TI, or changed its CA without firing this phase, and does not fire (A4.8, C3.22, C10.3)");
        }

        // A11.15: a unit held in Melee fires only in CC; a prisoner does not fire.
        if (Is(crew, Conditions.Melee) || Is(crew, Conditions.Captured))
        {
            return (null, $"play.ordnance-crew: '{crew.Id}' is held in Melee or captured, so it does not fire its Gun (A11.15, A20.5)");
        }

        var side = crew.Side;

        // A8.1, C6.1 (ruling R8.1): in the MPh, Defensive First Fire by the non-phasing side at the moving stack in its Location, while the
        // DEFENDER's window on its MF or MP expenditure is open; only the moving units are its targets.
        IReadOnlyList<string>? movers = null;
        if (phase == "MPh")
        {
            if (side == state.PhasingSide || state.Movement is not { WindowOpen: true } window || window.Location != target)
            {
                return (null, "play.ordnance-window: in the MPh a Gun or tank of the DEFENDER fires at the moving stack in its Location, while the window on its MF or MP expenditure is open (A8.1, C6.1)");
            }

            movers = window.Movers;
        }

        UnitInstance? vehicleTarget = null;
        if (targetVehicle is not null && (state.Unit(targetVehicle) is not { Status: InstanceStatus.Active } named || !LiveFire.IsVehicle(named)
            || state.Location(named.Id)?.Location != target || (vehicleTarget = named).Side == side || (movers is not null && !movers.Contains(named.Id))))
        {
            return (null, $"play.ordnance-vehicle-target: '{targetVehicle}' is not an active enemy vehicle in {target} (C3.31)");
        }

        // C3.31: a Vehicle Target Type shot attacks only the named vehicle. C3.33 (ruling R9.3): a mortar's Area Target Type shot attacks every unit
        // there, friendly ones too; C3.4: in the MPh only the moving units.
        var mortar = gun.Kind == "asl:light-mortar";
        var sw = mortar || gun.Kind == "asl:latw";
        UnitInstance[] targets = vehicleTarget is not null ? [] : [.. state.At(target).OfType<UnitInstance>()
            .Where(unit => unit.Status == InstanceStatus.Active && (unit.Side != side || mortar) && !LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Captured)
                && (movers is null || movers.Contains(unit.Id)))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (targets.Any(unit => unit.Definition is null && unit.Kind != UnitKinds.Dummy))
        {
            return (null, "play.ordnance-target: the target Location holds a unit outside the catalog");
        }

        var firingSide = side == state.PhasingSide ? "phasing" : "non-phasing";
        var targetSide = vehicleTarget?.Side ?? targets.FirstOrDefault()?.Side ?? state.Sides.FirstOrDefault(item => item.Id != side)?.Id;
        var shots = state.OrdnanceShots.FirstOrDefault(item => item.Gun == gun.Id);
        // C6.5, C6.51 (ruling R5.13): the Acquisition applies at its Location, or, while its units are apart, at any Location holding one of them.
        var acquisition = state.Acquisitions.FirstOrDefault(item => item.Gun == gun.Id
            && (item.Location == target || item.Units.Any(id => state.Location(id)?.Location == target)))?.Level ?? 0;
        var hit = new FireAttack(phase, firingSide, null, null, target.ToString(), [], null, null, null, null, state.ScenarioMonth, null,
            [.. targets.Select(unit => LiveFire.Target(state, unit, target))], targetSide is null ? null : state.Side(targetSide)?.Elr, null)
        {
            TargetSideNoQuarter = targetSide is not null && state.NoQuarter.Contains(targetSide, StringComparer.Ordinal) ? true : null,
        };
        var definition = gun is UnitInstance vehicleGun ? vehicleGun.Definition!.Definition : ((EquipmentInstance)gun).Definition!.Definition;
        var fired = gun is UnitInstance firingVehicle ? LiveFire.Fired(firingVehicle) : LiveFire.Fired((EquipmentInstance)gun);
        var depleted = state.DepletedAmmunition.Where(item => item.Gun == gun.Id).Select(item => item.Ammunition).Order(StringComparer.Ordinal).ToArray();
        var shot = new OrdnanceShot(phase, firingSide, state.Side(side)?.Nationality,
            new OrdnanceGun(gun.Id, definition, Is(gun, Conditions.Malfunctioned), shots?.Shots ?? 0, shots?.RateOfFireKept ?? false, fired)
            {
                Depleted = depleted.Length == 0 ? null : depleted,
                FirstFire = Is(gun, Conditions.FirstFire) ? true : null,
                FinalFire = Is(gun, Conditions.FinalFire) ? true : null,
                IntensiveFired = Is(gun, Conditions.IntensiveFire) ? true : null,
            },
            // A7.352 (ruling R8.4): a crew that fired its inherent FP (a fire counter it did not get from its Gun) does not fire the Gun.
            new OrdnanceCrew(crew.Id, crew.Definition!.Definition, Is(crew, Conditions.Broken), Is(crew, Conditions.Pinned), Is(crew, Conditions.Berserk),
                Is(crew, Conditions.Concealed) || Is(crew, Conditions.Hidden),
                // A7.351 (ruling R9.2): a squad fires a light mortar after its inherent FP; a HS, crew, or SMC does not.
                gun is EquipmentInstance && (LiveFire.Fired(crew) || Is(crew, Conditions.FirstFire)) && !state.GunCrewsFired.Contains(crew.Id, StringComparer.Ordinal)
                    && !(sw && crew.Kind == "asl:squad"))
            {
                Cx = Is(crew, Conditions.Cx) ? true : null,
            },
            target.ToString(), null, null, null, null, acquisition, hit, null)
        {
            Ammunition = ammunition ?? (vehicleTarget is not null ? "ap" : null),
            ScenarioYear = vehicleTarget is not null || ammunition is not null ? state.ScenarioYear : null,
            IntensiveFire = intensive ? true : null,
            // C5.8 (ruling R8.8): a squad or HS manning a Gun is non-qualified.
            NonQualified = gun is EquipmentInstance && crew.Kind is not "asl:crew" && !sw ? true : null,
            TargetType = mortar ? OrdnanceTargetTypes.Area : null,
        };
        if (mortar)
        {
            var (supported, supportReason) = MortarSupport(state, gun.Id, crew, spotter, director, phase);
            if (supported is null)
            {
                return (null, supportReason);
            }

            shot = shot with
            {
                Spotter = supported.Value.Spotter,
                Director = supported.Value.Director,
            };
        }
        else if (gun.Kind == "asl:latw" && spotter is null)
        {
            // Rulings R9.10, R9.11: an ATR or PSK fires its own ammunition, may be directed by a leader, and a PSK takes the Backblast.
            var (supported, supportReason) = MortarSupport(state, gun.Id, crew, null, director, phase);
            if (supported is null)
            {
                return (null, supportReason);
            }

            var psk = LatwType(((EquipmentInstance)gun).Definition!.Definition) == "psk";
            shot = shot with
            {
                Director = supported.Value.Director,
                Ammunition = psk ? "heat" : "ap",
                Panzerfaust = psk ? new OrdnancePanzerfaust(null, null, null, null) : null,
            };
        }
        else if (spotter is not null || director is not null)
        {
            return (null, "play.ordnance-support: only a light mortar has a Spotter, and only a SW a directing leader (C9.3, A7.531)");
        }
        if (phase == "MPh")
        {
            // C6.13, C6.16, C6.17: non-Assault Movement and the MF or MP spent in the Location (FRD); the Gun's shots there so far; the MP in the
            // firer's continuous LOS and a move into Open Ground are the planner's map reads.
            var window = state.Movement!;
            var here = state.OrdnanceShotsHere.FirstOrDefault(item => item.Gun == gun.Id);
            shot = shot with
            {
                FireKind = "first-fire",
                Movement = new OrdnanceMovement(null, vehicleTarget is null ? !window.Assault : null, null, window.HalfMfInLocation / 2, here?.Shots ?? 0)
                {
                    MpClaimed = vehicleTarget is not null && here is { Mp: > 0 } ? here.Mp : null,
                },
                // C3.71: a Critical Hit keeps FFNAM and FFMO, so the hit carries the stack's movement.
                Hit = shot.Hit! with
                {
                    TargetMovement = vehicleTarget is null ? new FireMovement(window.Assault) : null
                },
            };
        }
        if (gun is UnitInstance firer)
        {
            // D5.2, D5.34, C7.42, D5.341, D2.4 (rulings R7.10, R7.11): a CT AFV is BU unless its crew is exposed.
            shot = shot with
            {
                Vehicle = new OrdnanceVehicleFirer(!LiveFire.CrewExposed(firer), Is(firer, Conditions.Motion), Is(firer, Conditions.Stunned),
                    Is(firer, Conditions.Shocked) || Is(firer, Conditions.UnconfirmedKill), Is(firer, Conditions.Recalled))
                {
                    StunRecovery = Is(firer, Conditions.StunRecovery) ? true : null,
                    Moved = state.MovedVehicles.Contains(firer.Id, StringComparer.Ordinal) ? true : null,
                },
            };
        }

        if (vehicleTarget is not null)
        {
            shot = shot with
            {
                VehicleTarget = VehicleTargetFacts(state, vehicleTarget)
            };
        }

        return (shot, null);
    }

    /// <summary>
    /// A vehicle target's state facts (C6.1 Case J, C.8, D5.5, D5.6): a vehicle that entered a new hex or moved in Motion this Player Turn, or is in
    /// Motion, is moving; a Stunned, Shocked, or Recalled crew adds one to Crew Survival. The Target Facings are the planner's map reads.
    /// </summary>
    private static OrdnanceVehicleTarget VehicleTargetFacts(GameState state, UnitInstance vehicleTarget)
    {
        var inMotion = Is(vehicleTarget, Conditions.Motion);
        return new OrdnanceVehicleTarget(vehicleTarget.Id, vehicleTarget.Definition?.Definition, null, null,
            inMotion || state.MovedVehicles.Contains(vehicleTarget.Id, StringComparer.Ordinal), inMotion,
            Is(vehicleTarget, Conditions.Concealed) || Is(vehicleTarget, Conditions.Hidden),
            Is(vehicleTarget, Conditions.Stunned) || Is(vehicleTarget, Conditions.Shocked) || Is(vehicleTarget, Conditions.UnconfirmedKill)
                || Is(vehicleTarget, Conditions.Recalled),
            // D5.5: a Stunned or Shocked crew takes no Immobilization TC, nor does an absent crew or one already immobilized.
            !Is(vehicleTarget, Conditions.Stunned) && !Is(vehicleTarget, Conditions.Shocked) && !Is(vehicleTarget, Conditions.UnconfirmedKill)
                && !Is(vehicleTarget, Conditions.Abandoned) && !Is(vehicleTarget, Conditions.Immobilized))
        {
            Abandoned = Is(vehicleTarget, Conditions.Abandoned) ? true : null,
            StunRecovery = Is(vehicleTarget, Conditions.StunRecovery) ? true : null,
        };
    }

    /// <summary>
    /// A light mortar's Spotter and directing leader (C9.3, A7.531; rulings R9.2, R9.4): the Spotter is an active unit of the firer's side, the one
    /// already spotting for the mortar while it is Good Order, unless it is the firer; the leader is an active leader in the firer's Location.
    /// Adjacency and LOS are the planner's map reads.
    /// </summary>
    private static ((OrdnanceSpotter? Spotter, FireDirector? Director)? Support, string? Reason) MortarSupport(GameState state, string gun, UnitInstance firer, string? spotter,
        string? director, string phase)
    {
        OrdnanceSpotter? spotting = null;
        var kept = state.MortarSpotters.FirstOrDefault(item => item.Gun == gun)?.Spotter is { } recorded && state.Unit(recorded) is { Status: InstanceStatus.Active } still
            && !Is(still, Conditions.Broken) && !Is(still, Conditions.Captured) && still.Side == firer.Side ? still : null;
        if (spotter is not null)
        {
            if (state.Unit(spotter) is not { Status: InstanceStatus.Active, Definition: not null } unit || unit.Side != firer.Side || Is(unit, Conditions.Captured) || Is(unit, Conditions.Melee))
            {
                return (null, $"play.ordnance-spotter: '{spotter}' is not an active unit of the firer's side (C9.3)");
            }

            // C9.3 (ruling R9.4): a new Spotter is named only when the original is broken, eliminated, or captured.
            if (kept is not null && kept.Id != unit.Id)
            {
                return (null, $"play.ordnance-spotter-kept: {kept.Id} spots for {gun} while it is Good Order (C9.3)");
            }

            // C9.31 EX, A7.352 (table player, pass 9): a unit spots for one mortar a phase; a HS, crew, or SMC that has fired does not spot.
            if (state.SpottedThisPhase.Any(item => item.Spotter == unit.Id && item.Gun != gun)
                || (unit.Kind != "asl:squad" && (LiveFire.Fired(unit) || Is(unit, Conditions.FirstFire)) && !state.SpottedThisPhase.Any(item => item.Spotter == unit.Id)))
            {
                return (null, $"play.ordnance-spotter-used: {unit.Id} has spotted for another mortar or fired this phase (C9.31, A7.352)");
            }

            if (unit.Kind == "asl:squad" && !state.SpottedThisPhase.Any(item => item.Spotter == unit.Id) && SquadSupportRefusal(state, unit, phase, false) is { } spotterRefusal)
            {
                return (null, spotterRefusal);
            }

            spotting = new OrdnanceSpotter(unit.Id, unit.Definition.Definition, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned));
        }

        FireDirector? directing = null;
        if (director is not null)
        {
            if (state.Unit(director) is not { Status: InstanceStatus.Active, Definition: not null } leader || leader.Side != firer.Side
                || state.Location(leader.Id)?.Location is not { } leaderAt || leaderAt != state.Location(firer.Id)?.Location)
            {
                return (null, $"play.ordnance-director: '{director}' is not an active unit of the firer's side in its Location (A7.531)");
            }

            // A7.53 (referee, pass 9): the leader directing a SW this phase goes on directing its further ROF shots.
            var continuing = state.SupportWeaponDirectors.Any(item => item.Gun == gun && item.Leader == leader.Id);
            directing = new FireDirector(leader.Id, leader.Definition.Definition, leaderAt.ToString(), Is(leader, Conditions.Broken), Is(leader, Conditions.Pinned),
                Is(leader, Conditions.Concealed), (LiveFire.Fired(leader) || Is(leader, Conditions.FirstFire)) && !continuing, Is(leader, Conditions.Wounded));
        }

        return ((spotting, directing), null);
    }

    /// <summary>
    /// Why a squad may not use a SW now (A7.351, C13.31; table player, pass 9): it fired in an earlier phase (a Prep or Final Fire counter it did
    /// not get this phase), it fired its inherent FP with a SW this phase, or, for a PF, it is marked First Fire in the MPh (no PF Check in
    /// Subsequent First Fire). Null when it may.
    /// </summary>
    private static string? SquadSupportRefusal(GameState state, UnitInstance squad, string phase, bool panzerfaust)
    {
        if (squad.Kind != "asl:squad")
        {
            return null;
        }

        var firedHere = state.PhaseFirers.FirstOrDefault(item => item.Unit == squad.Id);
        var usedHere = state.SupportWeaponUses.Any(item => item.Unit == squad.Id);
        if (phase == "MPh" ? Is(squad, Conditions.FirstFire) && (panzerfaust || (firedHere is null && !usedHere))
            : LiveFire.Fired(squad) && firedHere is null && !usedHere)
        {
            return $"play.sw-fired: {squad.Id} fired earlier{(phase == "MPh" ? " this MPh" : string.Empty)} and may not use a SW now (A7.351, C13.31)";
        }

        return firedHere is { Weapon: not "0" }
            ? $"play.sw-limit: {squad.Id} fired its inherent FP with a SW this phase and uses no other (A7.351)"
            : null;
    }

    /// <summary>
    /// The state's part of a PF shot (C13.3 to C13.36; rulings R9.7, R9.8): the firing unit, its PF Checks this phase, whether it may still fire
    /// (not marked, or a squad whose only fire is one PF Check), the side's shots taken and allowed, and the AFV it fires at. Whether it fires
    /// from a ground-level building is the planner's map read.
    /// </summary>
    private static (OrdnanceShot? Shot, string? Reason) Panzerfaust(GameState state, string gunId, BoardLocation target, string? targetVehicle, string? director)
    {
        var phase = state.Phase switch
        {
            "pfph" => "PFPh",
            "dfph" => "DFPh",
            "afph" => "AFPh",
            "mph" => "MPh",
            _ => null,
        };
        var unitId = gunId[..^":pf".Length];
        if (phase is null || state.Unit(unitId) is not { Status: InstanceStatus.Active, Definition: not null, Position: MapPosition } unit)
        {
            return (null, "play.panzerfaust-firer: a PF is fired by an active unit on the map in a fire phase or the MPh (C13.31)");
        }

        if (Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured) || Is(unit, "asl:ti"))
        {
            return (null, $"play.panzerfaust-firer: '{unit.Id}' is held in Melee, captured, or TI, and does not fire (A11.15, A4.8)");
        }

        if (targetVehicle is null || state.Unit(targetVehicle) is not { Status: InstanceStatus.Active } vehicle || !LiveFire.IsVehicle(vehicle) || vehicle.Side == unit.Side
            || state.Location(vehicle.Id)?.Location != target)
        {
            return (null, $"play.panzerfaust-target: a PF fires at an enemy AFV named in {target} (C13.3; ruling R9.8)");
        }

        if (phase == "MPh" && (unit.Side == state.PhasingSide || state.Movement is not { WindowOpen: true } window || window.Location != target || !window.Movers.Contains(vehicle.Id)))
        {
            return (null, "play.ordnance-window: in the MPh a PF of the DEFENDER fires at the moving vehicle in its Location, while the window on its MP expenditure is open (A8.1, C13.31)");
        }

        var firingSide = unit.Side == state.PhasingSide ? "phasing" : "non-phasing";
        var checks = state.OrdnanceShots.FirstOrDefault(item => item.Gun == gunId)?.Shots ?? 0;
        // C13.31, A7.351 (referee, pass 9): a squad makes one PF Check whatever it fired, and a second only while its PF Check is its only fire this
        // phase; any other unit only while it has not fired (in the MPh, not in Subsequent First Fire).
        var onlyPanzerfaust = state.SupportWeaponUses.Any(item => item.Unit == unit.Id && item.Weapon == "panzerfaust");
        var fired = unit.Kind == "asl:squad" ? checks > 0 && !onlyPanzerfaust : LiveFire.Fired(unit) || (phase == "MPh" && Is(unit, Conditions.FirstFire));
        if (unit.Kind == "asl:squad" && checks == 0 && SquadSupportRefusal(state, unit, phase, true) is { } refusal)
        {
            return (null, refusal);
        }
        var hit = new FireAttack(phase, firingSide, null, null, target.ToString(), [], null, null, null, null, state.ScenarioMonth, null, [],
            state.Side(vehicle.Side!)?.Elr, null);
        var shot = new OrdnanceShot(phase, firingSide, state.Side(unit.Side!)?.Nationality,
            new OrdnanceGun(gunId, OrdnanceTargetTypes.Panzerfaust, false, checks, false, fired),
            new OrdnanceCrew(unit.Id, unit.Definition.Definition, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk),
                Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden), false)
            {
                Cx = Is(unit, Conditions.Cx) ? true : null,
            },
            target.ToString(), null, 0, null, null, 0, hit, null)
        {
            Ammunition = "heat",
            ScenarioYear = state.ScenarioYear,
            VehicleTarget = VehicleTargetFacts(state, vehicle),
            Panzerfaust = new OrdnancePanzerfaust(state.PanzerfaustShots.GetValueOrDefault(unit.Side!), PanzerfaustAllowance(state, unit.Side!), null, Is(unit, Conditions.Heroic)),
        };
        if (director is not null)
        {
            var (support, reason) = MortarSupport(state, gunId, unit, null, director, phase);
            if (support is null)
            {
                return (null, reason);
            }

            shot = shot with
            {
                Director = support.Value.Director
            };
        }

        if (phase == "MPh")
        {
            var moving = state.Movement!;
            var here = state.OrdnanceShotsHere.FirstOrDefault(item => item.Gun == gunId);
            shot = shot with
            {
                FireKind = "first-fire",
                Movement = new OrdnanceMovement(null, null, null, moving.HalfMfInLocation / 2, here?.Shots ?? 0)
                {
                    MpClaimed = here is { Mp: > 0 } ? here.Mp : null,
                },
            };
        }

        return (shot, null);
    }

    /// <summary>The squad equivalents (FRU) by which a side's Personnel overstack a Location, a crew or HS counting half (A5.1, A5.12).</summary>
    public static int Excess(GameState state, BoardLocation at, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        var units = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side && !Is(unit, Conditions.Captured)).ToArray();
        var squads = units.Count(unit => unit.Kind == "asl:squad") + (units.Count(unit => unit.Kind is "asl:half-squad" or "asl:crew") / 2m);
        return squads > 3 ? (int)Math.Ceiling(squads - 3) : 0;
    }

    /// <summary>Whether a Gun fires at its Bore Sighted Location with its original crew from its setup Location (C6.43).</summary>
    public static bool BoreSighted(GameState state, string gun, string crew, BoardLocation from, BoardLocation target)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.BoreSights.Any(item => item.Gun == gun && item.Location == target && item.Crew == crew && item.SetupLocation == from);
    }

    /// <summary>Whether a Gun is Emplaced: manned by a crew, never moved or hooked up (C11.2, C11.3).</summary>
    public static bool Emplaced(GameState state, EquipmentInstance gun)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(gun);
        return gun.Holding is { Role: HoldingRole.Manned } manning && state.Unit(manning.Holder)?.Kind == "asl:crew" && !state.UnemplacedGuns.Contains(gun.Id, StringComparer.Ordinal);
    }

    /// <summary>A turreted AFV's TCA: its recorded turret facing, or its VCA (D3.12; ruling R7.10).</summary>
    public static UnitFacing? TurretFacing(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        return state.TurretFacings.FirstOrDefault(item => item.Vehicle == vehicle.Id)?.Facing ?? (vehicle.Position as MapPosition)?.Facing;
    }

    /// <summary>The rolls of a record, rebuilt from its roll ids and the recorded dice, in the package's shape; null when one is misshapen.</summary>
    public static OrdnanceRolls? Rolls(OrdnanceFired fired, OrdnanceShot shot, IReadOnlyDictionary<string, DiceRolled> rolls)
    {
        ArgumentNullException.ThrowIfNull(fired);
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(rolls);
        IReadOnlyList<int>? toHit = null;
        int? subsequent = null, panzerfaustCheck = null;
        Dictionary<string, int>? selection = null;
        var hit = new Dictionary<string, string>(StringComparer.Ordinal);
        var critical = new Dictionary<string, string>(StringComparer.Ordinal);
        var extra = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
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
            else if (key == "panzerfaustCheck" && roll.Count == 1)
            {
                panzerfaustCheck = roll.Values[0];
            }
            else if (key.StartsWith("criticalSelection:", StringComparison.Ordinal) && key["criticalSelection:".Length..].Split(',') is var ids && ids.Length == roll.Count)
            {
                selection = ids.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => roll.Values[item.index], StringComparer.Ordinal);
            }
            else if (key is "toKill" or "shockCheck" or "crewCheck" or "crewSurvival" && roll.Count == 2)
            {
                extra[key] = roll.Values;
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
            : new OrdnanceRolls(toHit, subsequent, selection, hitRolls, criticalRolls)
            {
                ToKill = extra.GetValueOrDefault("toKill"),
                ShockCheck = extra.GetValueOrDefault("shockCheck"),
                CrewCheck = extra.GetValueOrDefault("crewCheck"),
                CrewSurvival = extra.GetValueOrDefault("crewSurvival"),
                PanzerfaustCheck = panzerfaustCheck,
            };
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

        var (expected, reason) = LiveOrdnance.FromState(state, fired.Gun, fired.Target, recorded.VehicleTarget?.VehicleId, recorded.Ammunition, recorded.IntensiveFire == true,
            recorded.Spotter?.UnitId, recorded.Director?.UnitId);
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
            },
            SameHex = recorded.SameHex,
            CrewSeen = recorded.CrewSeen,

            // The map read of pass 9: a PF fired from a ground-level building (C13.8).
            Panzerfaust = expected.Panzerfaust is null ? null : expected.Panzerfaust with
            {
                FromBuilding = recorded.Panzerfaust?.FromBuilding
            },
        };

        // The state reads of pass 8 are recomputed (table player, pass 8): Bore Sighting (C6.43), overstacking (A5.12, A5.131), and Emplacement.
        // A SW is where its possessor is; a PF where its firer is.
        var gunAt = state.Location(fired.Gun)?.Location ?? state.Location(fired.Crew)?.Location ?? fired.Target;
        var firingSide = state.Unit(fired.Crew)?.Side ?? string.Empty;
        var enemy = state.Sides.FirstOrDefault(item => item.Id != firingSide)?.Id ?? string.Empty;
        merged = merged with
        {
            BoreSighted = LiveOrdnance.BoreSighted(state, fired.Gun, fired.Crew, gunAt, fired.Target) ? true : null,
            FirerOverstack = LiveOrdnance.Excess(state, gunAt, firingSide) is var over and > 0 ? over : null,
            TargetOverstack = merged.VehicleTarget is null && LiveOrdnance.Excess(state, fired.Target, enemy) is var crowded and > 0 ? crowded : null,
            Hit = merged.Hit! with
            {
                GunTarget = recorded.Hit.GunTarget is { } gunTarget && state.Find(gunTarget.GunId ?? string.Empty) is EquipmentInstance targetGun
                    ? gunTarget with
                    {
                        Emplaced = LiveOrdnance.Emplaced(state, targetGun)
                    }
                    : recorded.Hit.GunTarget,
            },
        };
        if (JsonSerializer.Serialize(merged, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json)
            || fired.Crew != recorded.Crew?.UnitId || fired.Target.ToString() != recorded.TargetLocationId)
        {
            return "The ordnance record's facts do not match the game state.";
        }

        // C3.21, C5.1: the recorded facing lies exactly the recorded number of hexspines from the Gun's facing now, or the turret's (D3.12).
        var current = state.Find(fired.Gun) switch
        {
            EquipmentInstance { Position: MapPosition { Facing: { } facing } } => facing,
            UnitInstance vehicle => LiveOrdnance.TurretFacing(state, vehicle),
            _ => (Units.Documents.UnitFacing?)null,
        };
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

        // C13.31 (ruling R9.7): a PF Check that gave no shot keeps no ROF and acquires nothing.
        var gun = resolution.Gun ?? new OrdnanceGunEffect(0, false, 0, false, null, 0, null);
        return gun.RateOfFireKept != fired.RateOfFireKept || gun.Acquisition != fired.Acquisition || gun.AcquiredLocationId != fired.Acquired?.ToString()
            ? "The ordnance record's ROF or Acquisition differs from its resolution."
            : null;
    }
}
