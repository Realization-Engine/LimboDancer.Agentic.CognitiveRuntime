using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A DEFENDER's vehicle as a Motion attempt finds it (D2.401, p. 198; pass 55, ruling R55.13): a vehicle on the map, of the side that is not
/// phasing, with the counters it carries: immobilized, bogged, or Mired (D8.2), Stunned (D5.34), its "+1" counter, Recalled (D5.341), shocked or
/// an Unconfirmed Kill (C7.42), Abandoned (D5.41), its fire counter (<c>First Fire</c>, <c>Final Fire</c>, or <c>Intensive Fire</c>), and whether
/// it has made an attempt in this enemy MPh.
/// </summary>
public sealed record MotionAttemptVehicle(string Id, bool Vehicle, bool Defending)
{
    public bool Immobilized
    {
        get; init;
    }

    public bool Bogged
    {
        get; init;
    }

    public bool Mired
    {
        get; init;
    }

    public bool Stunned
    {
        get; init;
    }

    public bool StunRecovery
    {
        get; init;
    }

    public bool Recalled
    {
        get; init;
    }

    public bool Shocked
    {
        get; init;
    }

    public bool UnconfirmedKill
    {
        get; init;
    }

    public bool Abandoned
    {
        get; init;
    }

    public string? FireCounter
    {
        get; init;
    }

    public bool Attempted
    {
        get; init;
    }
}

/// <summary>
/// A moving enemy unit as a Motion attempt's vehicle sees it (D2.401; rulings R55.11, R55.12): whether the Location it began its move in is in
/// the vehicle's LOS, and each MF or MP expenditure of its MPh so far, oldest first, with whether the Location it was made in is in that LOS and
/// its cost in half points.
/// </summary>
public sealed record MotionAttemptMover(string Id, bool SeenAtStart, IReadOnlyList<(bool InLos, int HalfPoints)> Steps);

/// <summary>
/// The Motion attempt (D2.401, D2.51, D2.11, pp. 195 and 198; pass 55, rulings R55.11 to R55.13): "A Motion status attempt may be declared
/// during the MPh of an enemy ground unit by any DEFENDING Mobile vehicle which makes a Motion attempt dr &lt;= the number of MF/MP expended in
/// its LOS by that unit during its MPh." Play reads the state, the history, and the LOS; every verdict is here.
/// </summary>
public static class ScenarioA1MotionAttempts
{
    /// <summary>The purpose of the attempt's dr as its roll is recorded.</summary>
    public const string Purpose = "motion-attempt";

    /// <summary>The attempt's results as its record names them: the vehicle gains Motion and its change of CA.</summary>
    public const string Passed = "passed";

    /// <summary>The dr was above the number: "no penalty ... other than the inability to gain Motion status during that Player Turn".</summary>
    public const string Failed = "failed";

    /// <summary>D2.401, D2.51: the dr passed, and the Mechanical Reliability DR immobilized the AFV: "neither Motion nor VCA change".</summary>
    public const string Immobilized = "immobilized";

    /// <summary>D2.401, D2.11, D8.2: the dr passed, and the VCA change's Bog Check bogged the vehicle: neither Motion nor the change (ruling R55.13).</summary>
    public const string Bogged = "bogged";

    /// <summary>
    /// Why a vehicle may not attempt Motion now (D2.401; ruling R55.13), or null. In this order: the enemy MPh with the DEFENDER's window on an
    /// expenditure open; a vehicle of the DEFENDER on the map; Mobile; a crew able to act; no First, Final, or Intensive Fire counter; and "only
    /// once per enemy MPh". A vehicle under the "+1" counter may (D5.34: it "is no longer Stunned"), a Recalled one under "Recall; +1" may (D5.341
    /// has it leave "using Motion status", and nothing bars the attempt), and "even a vehicle in Motion may", for the free change of CA.
    /// </summary>
    public static string? Bar(string id, string? phase, bool windowOpen, MotionAttemptVehicle? vehicle)
    {
        if (phase != "mph" || !windowOpen)
        {
            return "play.motion-attempt: a Motion attempt is declared in the enemy MPh, while the DEFENDER may still answer a moving unit's latest MF or MP expenditure (D2.401)";
        }

        if (vehicle is not { Vehicle: true, Defending: true })
        {
            return $"play.motion-attempt: {id} is not a vehicle of the DEFENDER on the map, and only such a vehicle attempts Motion (D2.401)";
        }

        var why = vehicle.Immobilized ? "it is immobilized, and only a Mobile vehicle may (D2.401, D.7)"
            : vehicle.Bogged || vehicle.Mired ? "it is bogged, and only a Mobile vehicle may (D2.401, D8.2)"
            : vehicle.Abandoned ? "it is Abandoned and has no crew (D5.41)"
            : vehicle.Stunned ? "its crew is Stunned and may not move, a change of CA included (D5.34)"
            : vehicle.Recalled && !vehicle.StunRecovery ? "it was Recalled this Player Turn, which is treated as a Stun until the Player Turn ends (D5.341)"
            : vehicle.Shocked || vehicle.UnconfirmedKill ? "it is shocked, and its crew is incapable of any action (C7.42)"
            : vehicle.FireCounter is { } counter ? $"it is marked with a {counter} counter (D2.401)"
            : vehicle.Attempted ? "it has made its Motion attempt in this enemy MPh, and a vehicle attempts only once (D2.401)"
            : null;
        return why is null ? null : $"play.motion-attempt: {id} may not attempt Motion: {why}";
    }

    /// <summary>
    /// D2.401 (ruling R55.11): "the number of MF/MP expended in its LOS by that unit during its MPh", in half points: the sum over every
    /// expenditure made in a Location in the vehicle's LOS, whether or not the LOS was broken between them (Cases C1 and J1 count an unbroken LOS
    /// instead). A Start, Stop, or Delay MP spent in LOS is MP "expended in its LOS" like any other.
    /// </summary>
    public static int HalfPointsInLos(IEnumerable<(bool InLos, int HalfPoints)> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps.Where(step => step.InLos).Sum(step => step.HalfPoints);
    }

    /// <summary>The highest dr that passes: a dr is a whole number, so a fraction of a MF or MP adds nothing (a dr &lt;= 1½ is a dr of 1); a die shows no more than 6.</summary>
    public static int Number(int halfPoints) => Math.Min(6, Math.Max(0, halfPoints) / 2);

    /// <summary>D2.401: "a Motion attempt dr &lt;= the number of MF/MP expended in its LOS".</summary>
    public static bool Succeeds(int dr, int halfPoints) => 2 * dr <= halfPoints;

    /// <summary>
    /// The moving unit the attempt answers, with the half points it has spent in the vehicle's LOS, or why there is none (D2.401; rulings R55.11,
    /// R55.12). "The enemy unit must be one that had not been in the vehicle's LOS during that Player Turn prior to entering it during that MPh":
    /// nothing moves before the MPh and a unit moves once, so the LOS to the Location it began its move in decides it. The rule's "automatically
    /// fails" follows a LOS Check made after the declaration; the game makes that check first and refuses, which costs no attempt. Of several units
    /// moving as a stack, the one that has spent the most in LOS. An attempt no dr could pass is refused too, and costs none.
    /// </summary>
    public static (MotionAttemptMover? Mover, int HalfPoints, string? Refusal) Mover(string vehicle, bool moversAreVehicles, IReadOnlyList<MotionAttemptMover> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);
        if (movers.Count == 0)
        {
            return (null, 0, "play.motion-attempt: no enemy unit is moving, and a Motion attempt answers a moving enemy unit (D2.401)");
        }

        var unseen = movers.Where(mover => !mover.SeenAtStart).Select(mover => (Mover: mover, HalfPoints: HalfPointsInLos(mover.Steps)))
            .OrderByDescending(item => item.HalfPoints).ToArray();
        if (unseen.Length == 0)
        {
            return (null, 0, $"play.motion-attempt-seen: {string.Join(", ", movers.Select(mover => mover.Id))} began its move in {vehicle}'s LOS, and a Motion attempt answers only a unit that had "
                + "not been in the vehicle's LOS this Player Turn before it entered it in this MPh (D2.401); no attempt is spent");
        }

        var (best, halfPoints) = unseen[0];
        return Number(halfPoints) < 1
            ? (null, halfPoints, $"play.motion-attempt-los: {best.Id} has spent {Points(halfPoints, moversAreVehicles)} in {vehicle}'s LOS in this MPh, and no dr is that low (D2.401); "
                + "attempt once it has spent at least 1 there; no attempt is spent")
            : (best, halfPoints, null);
    }

    /// <summary>The hexspines between two CA, the shorter way round: one Bog Check for each when the VCA changes in a Bog hex (D8.2).</summary>
    public static int Hexspines(int current, int next)
    {
        var clockwise = (((next - current) % 6) + 6) % 6;
        return Math.Min(clockwise, 6 - clockwise);
    }

    /// <summary>
    /// Why the change of CA named with an attempt is refused (D2.401, D3.12; ruling R55.13), or null: "allowed to freely change its VCA/TCA", so
    /// any hexspine, and a TCA only for a vehicle with a turret; in Bypass a CA has its own positions (D2.32, D2.33), which is not built.
    /// </summary>
    public static string? ChangeBar(string id, bool facingNamed, bool facingRead, bool turretNamed, bool turretRead, bool turreted, bool bypass) =>
        (facingNamed && !facingRead) || (turretNamed && !turretRead) ? "play.motion-attempt: name the hexspine the VCA or the TCA turns to, such as north-east or west (D2.401)"
        : turretNamed && !turreted ? $"play.motion-attempt: {id} has no turret to turn (D3.12); name a VCA alone"
        : bypass && (facingNamed || turretNamed) ? $"play.motion-attempt-bypass: {id} is in Bypass, where a CA has its own positions; that is not built (D2.32, D2.33). Attempt Motion without a change of VCA or TCA"
        : null;

    /// <summary>
    /// The attempt's result (D2.401, D2.51; ruling R55.13): failed on the dr; else no Motion and no change of CA when the Mechanical Reliability DR
    /// immobilized the AFV or the VCA change's Bog Check bogged the vehicle; else passed.
    /// </summary>
    public static string Result(bool succeeded, bool immobilized, bool bogged) => !succeeded ? Failed : immobilized ? Immobilized : bogged ? Bogged : Passed;

    /// <summary>"3 MP", "1½ MF": the MF or MP a mover has spent in LOS.</summary>
    public static string Points(int halfPoints, bool vehicleMover) => $"{ScenarioA1VehicleMovementCalculator.Mp(halfPoints)} {(vehicleMover ? "MP" : "MF")}";

    /// <summary>The chance as the page says it: "3 MP in its LOS: succeeds on a dr of 3 or less".</summary>
    public static string Chance(int halfPoints, bool vehicleMover) =>
        Number(halfPoints) < 1 ? $"{Points(halfPoints, vehicleMover)} in its LOS: no dr is that low yet"
            : $"{Points(halfPoints, vehicleMover)} in its LOS: succeeds on a dr of {Number(halfPoints).ToString(CultureInfo.InvariantCulture)} or less";

    /// <summary>
    /// The proposal's line before the roll: the number, the dr that passes, the change of CA a pass brings, and the DR that may undo it: an AFV with
    /// a red MP allotment makes a Mechanical Reliability DR (D2.51), and a VCA change in woods, a building, or rubble a Bog Check for each hexspine
    /// (D2.11, D8.2).
    /// </summary>
    public static string Summary(string id, string mover, int halfPoints, bool vehicleMover, string? facing, string? turret, bool unreliable, int? bogDrm, int hexspines)
    {
        var change = (facing, turret) switch
        {
            (null, null) => string.Empty,
            ({ } vca, null) => $", and turns its VCA to {vca}",
            (null, { } tca) => $", and turns its TCA to {tca}",
            ({ } vca, { } tca) => $", and turns its VCA to {vca} and its TCA to {tca}",
        };
        return $"play.motion-attempt: {id} attempts Motion as {mover} moves: {Points(halfPoints, vehicleMover)} spent in its LOS: it gains Motion on a dr of "
            + $"{Number(halfPoints).ToString(CultureInfo.InvariantCulture)} or less{change} (D2.401)"
            + (unreliable ? "; its red MP then make it roll for Mechanical Reliability, and a 12 immobilizes it with neither Motion nor the change (D2.51)" : string.Empty)
            + (bogDrm is { } drm && hexspines > 0
                ? $"; the VCA change takes a Bog DR at {drm.ToString("+0;-0;0", CultureInfo.InvariantCulture)} for {(hexspines == 1 ? "its hexspine" : $"each of its {hexspines.ToString(CultureInfo.InvariantCulture)} hexspines")}, and bogged it gains neither (D2.11, D8.2)"
                : string.Empty);
    }

    /// <summary>The result in a record's words: "it gains Motion and turns its VCA to north-east", "it fails", and the two DR that undo a passed dr.</summary>
    public static string ResultText(string result, string? facing, string? turret) => result switch
    {
        Passed => "it gains Motion" + (facing, turret) switch
        {
            (null, null) => string.Empty,
            ({ } vca, null) => $" and turns its VCA to {vca}",
            (null, { } tca) => $" and turns its TCA to {tca}",
            ({ } vca, { } tca) => $" and turns its VCA to {vca} and its TCA to {tca}",
        },
        Immobilized => "it passes, but its Mechanical Reliability DR immobilizes it: neither Motion nor a change of CA (D2.51)",
        Bogged => "it passes, but it bogs on the Bog Check of its VCA change: neither Motion nor the change (D2.11, D8.2)",
        _ => "it fails, with no penalty but that it gains no Motion this Player Turn",
    };

    /// <summary>
    /// Whether a vehicle check is one a DEFENDER's vehicle makes in the enemy MPh (D2.401, D2.51, D2.11): the Mechanical Reliability DR or a Bog
    /// Check that follows its passed Motion attempt dr, while the DEFENDER's window is open and before the attempt's record marks it made.
    /// </summary>
    public static bool CheckInEnemyMph(string check, bool windowOpen, bool attempted) =>
        check is ScenarioA1ResultTables.MechanicalCheck or ScenarioA1ResultTables.BogCheck && windowOpen && !attempted;

    /// <summary>
    /// Why a recorded Motion attempt is refused at replay (D2.401; ruling R55.13), or null: made in the enemy MPh with the window open, by a
    /// DEFENDER's vehicle that has made none in this MPh; its result is the one its dr and the recorded number give, with the vehicle immobilized
    /// or bogged by the checks recorded before it when the result says so, and not otherwise; and only a pass names a change of CA.
    /// </summary>
    public static RecordRefusal? RecordRefusal(string? phase, bool windowOpen, bool defendingVehicle, bool attempted, int? dr, int halfPoints, string result, bool immobilized,
        bool bogged, bool changesCa)
    {
        if (phase != "mph" || !windowOpen || !defendingVehicle)
        {
            return new RecordRefusal("UNIT-STATE-048", "A Motion attempt is made by a DEFENDER's vehicle in the enemy MPh while the window on an expenditure is open (D2.401).");
        }

        if (attempted)
        {
            return new RecordRefusal("UNIT-STATE-048", "A vehicle attempts Motion only once per enemy MPh (D2.401).");
        }

        if (dr is not { } value)
        {
            return new RecordRefusal("UNIT-STATE-048", "The Motion attempt's roll is not one recorded die.");
        }

        return result != Result(Succeeds(value, halfPoints), immobilized, bogged) || (changesCa && result != Passed)
            ? new RecordRefusal("UNIT-STATE-048", "The Motion attempt's result disagrees with its dr, the MF or MP spent in its LOS, and the checks that followed it (D2.401, D2.51).")
            : null;
    }
}
