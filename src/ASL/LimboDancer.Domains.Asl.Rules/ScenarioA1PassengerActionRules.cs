namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A SW a Passenger names in its Mounted Fire, with its reviewed definition (null when the catalog does not review it).</summary>
public sealed record MountedWeaponFacts(string EquipmentId, FireDefinition? Definition);

/// <summary>
/// The facts of a Passenger's Mounted Fire (D6.1, D6.62, D6.63, D6.72; pass 65, ruling R65.10): the vehicle it rides and that vehicle's state, the
/// Passenger's own exposure (a pinned or broken Passenger is BU, D6.61), the attack's phase as the Fire package names it, and the SW it fires.
/// <paramref name="Moved"/> is a vehicle that entered a new hex or hexside or was in Motion this phase (D6.65, C.8).
/// </summary>
public sealed record MountedFireFacts(string UnitId, string VehicleId, FireDefinition Vehicle, bool CrewExposed, bool PassengerExposed, bool Moved, bool Motion,
    bool Bypass, string? Phase, IReadOnlyList<MountedWeaponFacts> Weapons)
{
    /// <summary>Whether the unit rides as a Rider (D6.22; pass 65, ruling R65.17): always exposed, firing no SW.</summary>
    public bool Rider
    {
        get; init;
    }
}

/// <summary>A Mounted Fire's verdict: the multipliers of the Passenger's FP, or why it may not fire.</summary>
public sealed record MountedFireVerdict(IReadOnlyList<FireModifier> Multipliers, string? Refusal);

/// <summary>
/// A member of a fire group as D6.64 reads it (pass 65, ruling R65.12): its vehicle and role when aboard (<see cref="FirePrc.Passenger"/> or
/// <see cref="FirePrc.Rider"/>), and whether that vehicle is a CE armored halftrack or Carrier, whose Passengers may join Infantry.
/// </summary>
public sealed record FireGroupMemberFacts(string UnitId, string? VehicleId, string? Role, bool CeHalftrack);

/// <summary>
/// A leader directing a fire group as D6.65 and D3.4 read him (pass 65, ruling R65.12): his vehicle when he is a Passenger, whether he is CE or otherwise
/// exposed, whether his vehicle is an armored halftrack or Carrier, and whether it moved this phase; an Armor Leader is inherent in his vehicle.
/// </summary>
public sealed record FireGroupLeaderFacts(string UnitId, string? VehicleId, bool Exposed, bool Halftrack, bool Moved, bool ArmorLeader)
{
    /// <summary>Whether the leader is pinned (A7.8): a pinned leader directs no attack.</summary>
    public bool Pinned
    {
        get; init;
    }
}

/// <summary>A Passenger adding its FP to its vehicle's OVR (D7.11; pass 65, ruling R65.13), with its reviewed definition.</summary>
public sealed record OverrunPassengerFacts(string UnitId, FireDefinition Definition, bool Wounded);

/// <summary>
/// A unit aboard a vehicle as its OVR is declared (D7.11; pass 65, ruling R65.13): whether it rides as a Passenger (not a Rider), and what keeps it from
/// firing: broken, pinned, a Bounding Fire counter, or having fired.
/// </summary>
public sealed record OverrunAboardFacts(string UnitId, bool Passenger, bool Broken, bool Pinned, bool BoundingFire, bool Fired);

/// <summary>
/// The actions of Passengers and Riders (pass 65, task 65.2; rulings R65.10 to R65.13): Mounted Fire, the SW a Passenger passes, the makeup of a fire
/// group holding PRC and the leaders who direct it, a Passenger leader's reach, the Passengers' share of an OVR, and their part in CC. Rout beneath a
/// vehicle is <see cref="ScenarioA1RoutCalculator.RoutBeneath"/>.
/// </summary>
public static class ScenarioA1PassengerActionRules
{
    /// <summary>A Passenger attacks in CC (D6.1, A11.611).</summary>
    public const string Attacks = "attacks";

    /// <summary>A BU halftrack Passenger does not attack in CC (D6.61, D6.62).</summary>
    public const string Idle = "idle";

    /// <summary>A Rider attacks apart, +1 to its CC DR (A11.611, D6.22).</summary>
    public const string Apart = "apart";

    /// <summary>
    /// D6.1, D6.62, D6.63, D6.72 (pp. 204 to 206; ruling R65.10): whether a Passenger may fire from its vehicle, and its FP's multipliers. Fire from a
    /// vehicle in Bypass is pass 100's. An armored halftrack's (or Carrier's) Passengers fire only while CE; any other Passengers fire with their FP
    /// halved as Mounted Fire. The SW a Passenger fires are a LMG, a PIAT, and a DC.
    /// </summary>
    public static MountedFireVerdict MountedFire(MountedFireFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.Vehicle);
        if (facts.Bypass)
        {
            return new([], $"play.mounted-fire: {facts.UnitId} rides {facts.VehicleId} in Bypass, and fire from a vehicle in Bypass, traced from its CAFP, is not built (D6.65, D2.36; ruling R65.24)");
        }

        // D6.22, D6.23 (pass 65, ruling R65.17): a Rider is always exposed and uses no SW.
        if (facts.Rider)
        {
            return facts.Weapons.Count > 0
                ? new([], $"play.mounted-fire: {facts.UnitId} rides {facts.VehicleId} as a Rider, and Riders use no SW; it fires its inherent FP alone (D6.22)")
                : new(Multipliers(facts.Vehicle.Unarmored == true, facts.Moved, facts.Motion, facts.Phase, rider: true), null);
        }

        if (facts.Vehicle.Unarmored != true && (!facts.CrewExposed || !facts.PassengerExposed))
        {
            return new([], $"play.mounted-fire: {facts.UnitId} rides {facts.VehicleId} BU; an armored halftrack's Passengers fire only while CE, unpinned and unbroken (D6.62); its crew may take off the BU counter first (D5.33)");
        }

        if (facts.Weapons.FirstOrDefault(weapon => !Allowed(weapon.Definition)) is { } barred)
        {
            return new([], $"play.mounted-fire: {facts.UnitId} may not fire {barred.EquipmentId} from {facts.VehicleId}: a Passenger fires only a LMG, a PIAT, or a Thrown DC (D6.1)");
        }

        return new(Multipliers(facts.Vehicle.Unarmored == true, facts.Moved, facts.Motion, facts.Phase), null);
    }

    /// <summary>
    /// The multipliers of a Passenger's FP (D6.1, D6.63, D6.72; ruling R65.10): halved as Mounted Fire unless its vehicle is armored; halved for
    /// Bounding First Fire by a vehicle that moved in its MPh (the AFPh's halving is the Advancing Fire one, A7.24, which D6.63 names); halved again
    /// while the vehicle is in Motion. A Rider's inherent FP is halved as Mounted Fire, and quartered when its AFV has moved or is in Motion, an OVR
    /// included (D6.22; pass 65, ruling R65.17).
    /// </summary>
    public static IReadOnlyList<FireModifier> Multipliers(bool unarmored, bool moved, bool motion, string? phase, bool rider = false)
    {
        if (rider)
        {
            return moved || motion
                ? [new FireModifier("mounted-fire", 0.5m, "D6.22"), new FireModifier("rider-moved", 0.5m, "D6.22")]
                : [new FireModifier("mounted-fire", 0.5m, "D6.22")];
        }

        var multipliers = new List<FireModifier>();
        if (unarmored)
        {
            multipliers.Add(new FireModifier("mounted-fire", 0.5m, "D6.1"));
        }

        if (moved && phase == "MPh")
        {
            multipliers.Add(new FireModifier("bounding-fire", 0.5m, unarmored ? "D6.1" : "D6.63"));
        }

        if (motion)
        {
            multipliers.Add(new FireModifier("motion-fire", 0.5m, "D6.1"));
        }

        return multipliers;
    }

    /// <summary>D6.1 (p. 205): "LMG, PIAT, and Thrown DC are the only SW which can be fired by Passengers".</summary>
    public static bool Allowed(FireDefinition? weapon) =>
        weapon is not null && ((weapon.IsMg && weapon.Size == "light") || (weapon.Kind == "asl:latw" && weapon.LatwType == ScenarioA1HeEquivalency.Piat) || weapon.IsDc);

    /// <summary>
    /// D6.64, D6.65, D3.4 (pp. 206, 200; ruling R65.12): why a fire group holding PRC, or directed by a leader aboard, may not fire together, or null. A
    /// Passenger fires in a group only with other Passengers of its vehicle, unless it rides a CE halftrack, whose Passengers may join Infantry and the
    /// Passengers of another CE halftrack; a Rider only with Riders of its vehicle. A Passenger leader, CE or otherwise exposed, directs the Passengers
    /// of his vehicle, and the Location's other units only from a halftrack that has not moved this phase; an Armor Leader directs Passengers only in his
    /// halftrack's group; a leader on foot directs Passengers only where they may join Infantry.
    /// </summary>
    public static string? FireGroupBar(IReadOnlyList<FireGroupMemberFacts> firers, IReadOnlyList<FireGroupLeaderFacts> leaders, IReadOnlyCollection<string> vehiclesFiring)
    {
        ArgumentNullException.ThrowIfNull(firers);
        ArgumentNullException.ThrowIfNull(leaders);
        ArgumentNullException.ThrowIfNull(vehiclesFiring);
        foreach (var firer in firers.Where(item => item.VehicleId is not null))
        {
            var others = firers.Where(item => item.UnitId != firer.UnitId).ToArray();
            if (firer.Role == FirePrc.Rider)
            {
                if (others.FirstOrDefault(item => item.Role != FirePrc.Rider || item.VehicleId != firer.VehicleId) is { } stranger)
                {
                    return $"play.fire-group-prc: {firer.UnitId} rides {firer.VehicleId} as a Rider and fires in a group only with other Riders of it, not with {stranger.UnitId} (D6.64)";
                }

                continue;
            }

            var stray = firer.CeHalftrack
                ? others.FirstOrDefault(item => item.VehicleId is not null && !(item.Role == FirePrc.Passenger && item.CeHalftrack))
                : others.FirstOrDefault(item => item.VehicleId != firer.VehicleId || item.Role != FirePrc.Passenger);
            if (stray is not null)
            {
                return firer.CeHalftrack
                    ? $"play.fire-group-prc: {firer.UnitId}, a Passenger of the CE halftrack {firer.VehicleId}, fires in a group with Infantry and the Passengers of a CE halftrack, not with {stray.UnitId} (D6.64)"
                    : $"play.fire-group-prc: {firer.UnitId} rides {firer.VehicleId} and fires in a group only with its other Passengers, not with {stray.UnitId} (D6.64)";
            }
        }

        foreach (var leader in leaders)
        {
            var aboard = firers.Where(item => item.VehicleId is not null).ToArray();
            if (leader.ArmorLeader)
            {
                if (aboard.Length > 0 && (leader.VehicleId is null || !vehiclesFiring.Contains(leader.VehicleId)))
                {
                    return $"play.fire-leader-prc: the Armor Leader of {leader.VehicleId} directs Passengers only when their FP is added to his halftrack's own in one group (D6.65, D3.4)";
                }

                continue;
            }

            if (leader.VehicleId is null)
            {
                if (aboard.FirstOrDefault(item => !(item.Role == FirePrc.Passenger && item.CeHalftrack)) is { } apart)
                {
                    return $"play.fire-leader-prc: {leader.UnitId} on foot does not direct {apart.UnitId}, aboard {apart.VehicleId}, which fires in no group with Infantry (D6.64, D6.65)";
                }

                continue;
            }

            if (leader.Pinned)
            {
                return $"play.fire-leader-prc: {leader.UnitId} rides {leader.VehicleId} pinned, and a pinned leader directs no attack (A7.8)";
            }

            if (!leader.Exposed)
            {
                return $"play.fire-leader-prc: {leader.UnitId} rides {leader.VehicleId} BU and directs no fire; a Passenger leader directs only while CE (D6.62, D6.65)";
            }

            if ((!leader.Halftrack || leader.Moved) && firers.FirstOrDefault(item => item.VehicleId != leader.VehicleId) is { } outside)
            {
                return $"play.fire-leader-prc: {leader.UnitId}, a Passenger of {leader.VehicleId}, directs only its other Passengers, not {outside.UnitId}"
                    + (leader.Halftrack ? ", since the halftrack moved this phase (D6.65)" : "; only a halftrack's Passenger leader directs other units of the hex (D6.65)");
            }
        }

        return null;
    }

    /// <summary>
    /// D6.651 (p. 206; ruling R65.12): whether a leader's Rally, MC, TC, and LLMC reach a unit. A Passenger leader reaches the other Passengers of his
    /// vehicle, and the Location's other units only while CE in a halftrack that has not moved this phase; a leader on foot reaches Passengers only of a
    /// vehicle that has not moved (C.8).
    /// </summary>
    public static bool LeaderReaches(string? leaderVehicle, bool leaderCeInUnmovedHalftrack, string? unitVehicle, bool unitVehicleMoving) =>
        leaderVehicle is not null ? unitVehicle == leaderVehicle || leaderCeInUnmovedHalftrack
            : unitVehicle is null || !unitVehicleMoving;

    /// <summary>
    /// D6.64 (p. 206; ruling R65.12): whether a leader may be offered to direct a fire group before its FG bar is read. A leader aboard directs only the
    /// Passengers of his own vehicle, so every firer rides it; a leader on foot directs a group with a firer in his Location.
    /// </summary>
    public static bool DirectorOffered(string? leaderVehicle, IReadOnlyCollection<string?> firerVehicles, bool firerInHisLocation)
    {
        ArgumentNullException.ThrowIfNull(firerVehicles);
        return leaderVehicle is not null ? firerVehicles.Count > 0 && firerVehicles.All(vehicle => vehicle == leaderVehicle) : firerInHisLocation;
    }

    /// <summary>
    /// D7.11 (p. 207; ruling R65.13): the share of a Passenger's printed FP its vehicle's OVR takes, before TPBF: one-half from a CE armored halftrack
    /// (only, D7.11) or Carrier (D6.8), one-fourth from any other vehicle, none from a BU armored vehicle, whose Passengers do not fire (D6.62).
    /// </summary>
    public static decimal OverrunShare(bool unarmored, bool crewExposed, string? vehicleClass) =>
        unarmored ? 0.25m : !crewExposed ? 0m : vehicleClass == "halftrack" || ScenarioA1CarrierRules.IsCarrier(vehicleClass) ? 0.5m : 0.25m;

    /// <summary>
    /// D6.62, D6.84 (pp. 206; the boundary review): whether a vehicle's Passengers are exposed with its crew: an unarmored vehicle's always; an armored
    /// one's while its crew is CE, which a Carrier's always is (<see cref="ScenarioA1CarrierRules.CrewExposed"/>, read into
    /// <paramref name="crewExposed"/>).
    /// </summary>
    public static bool PassengersExposed(bool unarmored, bool crewExposed) => unarmored || crewExposed;

    /// <summary>
    /// D6.22, D6.65, C.8 (the boundary review): whether a PRC's vehicle has moved, as its Mounted Fire and its leader read it. A Rider's AFV has moved when
    /// it entered a new hex or moved under Motion this Player Turn (the state's list); a Passenger's vehicle when it did so in this MPh, or is in Motion.
    /// </summary>
    public static bool VehicleMoved(bool rider, bool mph, bool movedThisPlayerTurn, bool motion) =>
        rider ? movedThisPlayerTurn : (mph && movedThisPlayerTurn) || motion;

    /// <summary>
    /// D7.11, D6.62, D6.5 (p. 207; ruling R65.13; the boundary review): the Passengers who add their FP to a vehicle's OVR as it is declared: none when the
    /// vehicle's share is none (<see cref="OverrunShare"/>); else those named, or every one that may fire when none is named: a Passenger, not a Rider, not
    /// broken or pinned, with no Bounding Fire counter, that has not fired. Null when none adds, so the record keeps its older shape.
    /// </summary>
    public static string[]? OverrunPassengers(decimal share, IEnumerable<OverrunAboardFacts> aboard, IReadOnlyCollection<string>? named)
    {
        ArgumentNullException.ThrowIfNull(aboard);
        if (share == 0)
        {
            return null;
        }

        string[] able = [.. aboard.Where(unit => unit.Passenger && !unit.Broken && !unit.Pinned && !unit.BoundingFire && !unit.Fired
            && (named is null || named.Contains(unit.UnitId, StringComparer.Ordinal))).Select(unit => unit.UnitId)];
        return able.Length == 0 ? null : able;
    }

    /// <summary>
    /// D7.11, D7.16 (p. 207; ruling R65.13): each Passenger's FP added to its vehicle's OVR, its share tripled for TPBF and halved with the OVR's own
    /// <paramref name="common"/> halvings. A leader adds no FP.
    /// </summary>
    public static IReadOnlyList<FirerFirepower> OverrunFirepower(IReadOnlyList<OverrunPassengerFacts> passengers, bool unarmored, bool crewExposed,
        IReadOnlyList<FireModifier> common, string? vehicleClass)
    {
        ArgumentNullException.ThrowIfNull(passengers);
        ArgumentNullException.ThrowIfNull(common);
        var share = OverrunShare(unarmored, crewExposed, vehicleClass);
        var firepower = new List<FirerFirepower>();
        foreach (var passenger in passengers.Where(item => !item.Definition.IsLeader))
        {
            var printed = passenger.Wounded && passenger.Definition.WoundedFirepower is { } wounded ? wounded : passenger.Definition.Firepower ?? 0;
            List<FireModifier> multipliers = [new FireModifier(share == 0.5m ? "passenger-half" : "passenger-quarter", share, "D7.11"), new FireModifier("tpbf", 3m, "D7.11"), .. common];
            firepower.Add(new FirerFirepower(passenger.UnitId, printed, multipliers, multipliers.Aggregate((decimal)printed, (value, item) => value * item.Value)));
        }

        return firepower;
    }

    /// <summary>
    /// D7.16, D6.65 (p. 207; ruling R65.13): the one leadership modifier of an OVR with Passengers aboard: the Passenger leader's, when another Passenger
    /// adds FP and his is better than the Armor Leader's (or the crew's), else the Armor Leader's. Null when neither applies.
    /// </summary>
    public static FireModifier? OverrunLeadership(IReadOnlyList<OverrunPassengerFacts> passengers, bool sharesFp, int? armorLeadership)
    {
        ArgumentNullException.ThrowIfNull(passengers);
        var leader = sharesFp && passengers.Any(item => !item.Definition.IsLeader)
            ? passengers.Where(item => item.Definition.IsLeader && item.Definition.Leadership is not null)
                .Select(item => (item.UnitId, Value: item.Definition.Leadership!.Value + (item.Wounded ? 1 : 0))).OrderBy(item => item.Value).FirstOrDefault()
            : default;
        return leader.UnitId is not null && (armorLeadership is null || leader.Value < armorLeadership) ? new FireModifier("leadership:" + leader.UnitId, leader.Value, "D7.16")
            : ScenarioA1ArmorLeaders.Modifier(armorLeadership);
    }

    /// <summary>
    /// A11.611, A11.71, D6.61, D6.62 (pp. 75, 76, 205, 206; ruling R65.13): a PRC's part in CC: a Rider attacks apart with +1 to its DR; a Passenger of an
    /// armored halftrack attacks only while CE; others attack. A Passenger of a Mobile vehicle that survives the first round is not held in Melee.
    /// </summary>
    public static (string Part, bool HeldInMelee) CloseCombat(string role, bool armored, bool exposed, bool vehicleMobile) =>
        (role == FirePrc.Rider ? Apart : armored && !exposed ? Idle : Attacks, HeldInMelee(vehicleMobile));

    /// <summary>
    /// A11.71 (p. 76: "Passengers/Riders of Immobile vehicles are held in Melee"; pass 70, ruling R70.31): whether a PRC is held in Melee at the CCPh's end,
    /// read by the projector from its vehicle's own state.
    /// </summary>
    public static bool HeldInMelee(bool vehicleMobile) => !vehicleMobile;

    /// <summary>
    /// A11.71 (ruling R70.31): a vehicle is Mobile, for its PRC's part in CC, while neither Immobilized nor Bogged (fix round C: one reading for Play and the
    /// projector).
    /// </summary>
    public static bool VehicleMobile(bool immobilized, bool bogged) => !immobilized && !bogged;

    /// <summary>
    /// D6.5, D6.24, A7.211 (pp. 205, 55; pass 70, ruling R70.32): PRC that unload, Bail Out, or survive TPBF in a Location holding a Known enemy unit are in
    /// an enemy Location and not held in Melee; the CC counter that shows it is no stored condition, since units are held in Melee only at the CCPh's end.
    /// </summary>
    public static string EnemyLocationText(string locationText, string rule) =>
        $"; {locationText} holds a Known enemy unit, so they are in an enemy Location and not held in Melee until the CCPh ends ({rule})";

    /// <summary>
    /// D6.5, D6.24 (ruling R70.32): whether a unit makes a Location an enemy Location for PRC of another side that unload or Bail Out there: an active enemy
    /// unit, not a vehicle, Known (<see cref="ScenarioA1PrisonerCalculator.KnownEnemy"/>: no Dummy, "?", hidden unit, or prisoner), which every view may know.
    /// One reading for the proposals and the record lines (fix round C).
    /// </summary>
    public static bool MakesEnemyLocation(bool active, bool enemy, bool vehicle, bool dummy, bool concealed, bool hidden, bool captured) =>
        active && enemy && !vehicle && ScenarioA1PrisonerCalculator.KnownEnemy(dummy, concealed, hidden, captured);

    /// <summary>A7.211 (p. 55; ruling R70.32): PRC attacked by TPBF in their own Location that survive are in an enemy Location, not held in Melee until after the CCPh.</summary>
    public static string TpbfPrcText() =>
        "; the PRC that survive are in an enemy Location and not held in Melee until after the CCPh, those of a Mobile vehicle never (A7.211, A11.71)";

    /// <summary>
    /// D6.31 (p. 205; ruling R65.10): why a SW may not pass between these units, or null. A PRC passes a SW it possesses to friendly Infantry in its
    /// Location or to another PRC of its vehicle; a unit on foot passes none to a PRC, which takes a SW only as it loads.
    /// </summary>
    public static string? TransferBar(string giverId, string? giverVehicle, string takerId, string? takerVehicle) =>
        takerVehicle is null || takerVehicle == giverVehicle ? null
            : giverVehicle is null ? $"play.transfer-prc: {takerId} rides {takerVehicle}; a Passenger takes a SW from Infantry only as it loads with it (D6.31, D6.4)"
            : $"play.transfer-prc: {giverId} rides {giverVehicle} and passes a SW to Infantry in its Location or to a PRC of its own vehicle, not to {takerId} aboard {takerVehicle} (D6.31)";

    /// <summary>
    /// D6.5, D.3 (pp. 205, 192; ruling R65.10): why a Passenger may not unload, or null: one that fired this phase, or that Prep Fired, or added its FP to
    /// its vehicle's OVR in this MPh, does not unload in it.
    /// </summary>
    public static string? UnloadAfterFireBar(string unitId, bool fired, bool overran) =>
        overran ? $"play.unload: {unitId} added its FP to the OVR this MPh, so it does not unload in it (D6.5)"
            : fired ? $"play.unload: {unitId} fired from its vehicle this Player Turn, so it does not unload in this phase (D6.5, D.3)"
            : null;
}
