namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A vehicle's Rider capacity (D6.2, D6.81): its PP, or, for an AFV not otherwise granted Riders, one SMC and the two PP he possesses
/// (<paramref name="OneSmc"/>).
/// </summary>
public sealed record RiderCapacity(int Pp, bool OneSmc);

/// <summary>
/// The terrain of a vehicle's entry as D6.21 and B13.43 read it (pass 65, ruling R65.17): the terrain keys of the Location it leaves and of the one it
/// enters (as <see cref="ScenarioA1Definitions.FireTerrain"/> names them), the map's name of the terrain entered (for a Water Obstacle), the terrain on
/// the hexside crossed (for bocage), whether it crosses a road hexside, enters in Bypass, or is in a stream, and whether a building entered is entered
/// by a friendly Vehicular-Sized Entrance.
/// </summary>
public sealed record RiderTerrainFacts(string? Left, string? Entered, string? EnteredName, string? Hexside, bool Road, bool Bypass, bool Stream, bool FriendlyVse);

/// <summary>
/// Riders and Bailing Out (pass 65, task 65.4; rulings R65.16, R65.17): the Rider capacity of a vehicle class (D6.2, D6.81), the dates Riders are
/// allowed (D6.2), the terrain and the TCA change that make them Bail Out (D6.21, B13.43), a pin or break that does (D6.23), and the Bail Out's NMC,
/// SW drs, and fire counter (D6.24). Their Mounted Fire is <see cref="ScenarioA1PassengerActionRules.MountedFire"/>; their part in CC is
/// <see cref="ScenarioA1PassengerActionRules.CloseCombat"/>. Play reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1RiderRules
{
    /// <summary>The causes of a Bail Out as the <c>rider-bailed-out</c> record names them: terrain (D6.21, B13.43), a TCA change (D6.21), a pin or a break (D6.23).</summary>
    public const string Terrain = "terrain";

    public const string Turret = "turret";

    public const string Pin = "pin";

    public const string Break = "break";

    /// <summary>The results of a Bail Out's NMC (D6.24, A10.3, A7.8, A10.31).</summary>
    public const string Passed = "passed";

    public const string Pinned = "pinned";

    public const string Broken = "broken";

    /// <summary>Casualty Reduced, and broken.</summary>
    public const string Reduced = "reduced";

    public const string Eliminated = "eliminated";

    /// <summary>Failed by more than its ELR and Replaced by a broken unit of lesser quality (A19.13; pass 70, ruling R70.9); never Disrupted (A19.12).</summary>
    public const string Replaced = "replaced";

    /// <summary>The fate of a SW a Bailing Out Rider carries (D6.24).</summary>
    public const string Kept = "kept";

    public const string Malfunctioned = "malfunctioned";

    /// <summary>
    /// D6.2, D6.81 (pp. 205, 206; ruling R65.16): a vehicle's Rider capacity from its class. A tank, TD, SPA, SPAA, or Assault Gun carries 14 PP; a Carrier
    /// 8 PP; any other AFV (a tankette, halftrack, armored car, or other) one SMC and the two PP he possesses; a truck or any other unarmored vehicle none.
    /// Cavalry and motorcycle Riders are the backlog's (ruling R65.24).
    /// </summary>
    public static RiderCapacity? Capacity(string? vehicleClass, bool unarmored) =>
        unarmored || vehicleClass == "truck" ? null
            : vehicleClass switch
            {
                "tank" or "td" or "spa" or "spaa" or "assault-gun" => new RiderCapacity(14, false),
                "carrier" => new RiderCapacity(8, false),
                _ => new RiderCapacity(2, true),
            };

    /// <summary>
    /// D6.2, D6.4 (p. 205; ruling R65.16): whether units board a vehicle as Riders: when the player asks it of one that takes Riders, and always on one that
    /// takes Riders and no Passengers (its Passenger PP capacity is none).
    /// </summary>
    public static bool BoardsAsRiders(bool takesRiders, bool takesPassengers, bool asked) => takesRiders && (asked || !takesPassengers);

    /// <summary>
    /// D6.2 (p. 205; ruling R65.16): "AFV Riders cannot be used until 1942, and even then only by the Russians. In 1943 and thereafter, all nationalities
    /// may use Riders." Why a side may not use Riders in the game's year, or null; a game that names no year allows none.
    /// </summary>
    public static string? DateBar(string vehicleId, int? year, string? nationality) =>
        year is not { } scenarioYear ? $"the game names no scenario year, and Riders are allowed only from 1942 for the Russians and from 1943 for every nationality, so {vehicleId} takes no Riders (D6.2)"
            : scenarioYear >= 1943 || (scenarioYear == 1942 && nationality == "russian") ? null
            : $"Riders are allowed only from 1942 for the Russians and from 1943 for every nationality, so in {scenarioYear} {vehicleId} takes no {nationality} Riders; they may board as Passengers where it carries any (D6.2)";

    /// <summary>
    /// D6.2, A5.5 (pp. 205, 53; ruling R65.16): why the Riders aboard, with those boarding, exceed the vehicle's Rider capacity, counted apart from its
    /// Passenger PP, or null. Each unit's PP is read in order, and only until one is not recorded.
    /// </summary>
    public static string? CapacityBar(string vehicleId, RiderCapacity? capacity, IEnumerable<(string Id, bool Smc, Func<int?> Pp)> riders)
    {
        ArgumentNullException.ThrowIfNull(riders);
        if (capacity is null)
        {
            return $"{vehicleId} carries no Riders: only an AFV does (D6.2)";
        }

        var aboard = riders.ToArray();
        if (capacity.OneSmc && (aboard.Length > 1 || aboard.Any(unit => !unit.Smc)))
        {
            return $"{vehicleId} carries one SMC as a Rider, and the two PP he possesses, and no other (D6.2)";
        }

        if (aboard.Count(unit => unit.Smc) > 4)
        {
            return $"more than four SMC would ride {vehicleId}; up to four count as zero PP (D6.2, A5.5)";
        }

        var total = 0;
        foreach (var (id, _, pp) in aboard)
        {
            if (pp() is not { } each)
            {
                return $"the PP of what {id} carries are not recorded (A4.4)";
            }

            total += each;
        }

        return total > capacity.Pp
            ? capacity.OneSmc ? $"{vehicleId} carries one SMC as a Rider with the two PP he possesses, and he would take {total} PP (D6.2)"
                : $"its Riders would take {total} PP, and {vehicleId} carries {capacity.Pp} Rider PP (D6.2{(capacity.Pp == 8 ? ", D6.81" : string.Empty)})"
            : null;
    }

    /// <summary>
    /// D6.21, B13.43 (pp. 205, 128; ruling R65.17): whether a vehicle's entry makes its Riders Bail Out: it enters or leaves woods, orchard, a building
    /// (but by a friendly Vehicular-Sized Entrance), rubble, or a Water Obstacle, or crosses bocage (but by a Breach, which the map names apart), unless it
    /// moves across a road hexside, in Bypass, or in a stream.
    /// </summary>
    public static bool TerrainBailOut(RiderTerrainFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Road || facts.Bypass || facts.Stream)
        {
            return false;
        }

        var entering = Prohibited(facts.Entered, facts.EnteredName) && !(ScenarioA1Definitions.IsBuildingTerrain(facts.Entered) && facts.FriendlyVse);
        return entering || Prohibited(facts.Left, null) || facts.Hexside == "Bocage";
    }

    /// <summary>The terrain D6.21 bars Riders from: woods, orchard, a building, rubble, or a Water Obstacle.</summary>
    private static bool Prohibited(string? key, string? name) =>
        key is "woods" or "orchard" || ScenarioA1Definitions.IsBuildingTerrain(key) || ScenarioA1Definitions.IsRubbleTerrain(key)
        || (name is not null && (name == "Water" || name.Contains("River", StringComparison.Ordinal) || name.Contains("Canal", StringComparison.Ordinal)
            || name.Contains("Pond", StringComparison.Ordinal)));

    /// <summary>
    /// D6.21, B13.43 (pp. 205, 128; pass 65, the referee's review): why Riders may not board or set up on a vehicle in its Location, or null: woods, orchard,
    /// a building, rubble, or a Water Obstacle, unless the vehicle is on a road there, in a stream, or in Bypass outside the obstacle. A Vehicular-Sized
    /// Entrance is not on the map read, so a building always bars them.
    /// </summary>
    public static string? LocationBar(string vehicleId, string? terrainKey, string? terrainName, bool road, bool stream, bool bypass) =>
        road || stream || bypass || !Prohibited(terrainKey, terrainName) ? null
            : $"{vehicleId} is in {terrainName ?? terrainKey}, where Riders are not allowed but on a road, in a stream, or in Bypass (D6.21, B13.43)";

    /// <summary>
    /// D6.24 (p. 205; pass 65, the referee's review): a Bail Out's NMC is taken against the unit's Morale Level, its broken side's when broken. Why a unit
    /// may not ride as a Rider because the catalog gives it no such Morale Level, or null.
    /// </summary>
    public static string? MoraleBar(string unitId, int? morale, int? brokenMorale) =>
        morale is not null && brokenMorale is not null ? null
            : $"{unitId}'s definition is incomplete: the catalog gives it no {(morale is null ? "Morale Level" : "broken Morale Level")}, which a Rider's Bail Out NMC is taken against (D6.24)";

    /// <summary>
    /// D6.21 (p. 205; ruling R65.17): "A Rider on a turreted AFV must Bail Out if the AFV changes its TCA", whether by an MP expenditure, an ordnance shot, a
    /// Canister shot, its CMG's fire, or a Motion attempt (the referee's review).
    /// </summary>
    public static bool TurretBailOut(bool turreted, int? tca, int? turnedTo) => turreted && turnedTo is { } turned && turned != tca;

    /// <summary>
    /// D6.21 (p. 205; ruling R65.17; the boundary review): where Riders Bail Out: "in the last hex occupied prior to that prohibited terrain", and in
    /// the AFV's Location for a TCA change, the Location it enters when the change comes with an entry. True for the Location the AFV was in.
    /// </summary>
    public static bool BailsOutWhereItWas(string cause, bool entry) => cause == Terrain || !entry;

    /// <summary>D6.23 (p. 205; ruling R65.17): "AFV Riders must Bail Out if pinned or broken"; an eliminated one does not.</summary>
    public static bool CombatBailOut(bool rider, bool eliminated, bool pinned, bool broken) => rider && !eliminated && (pinned || broken);

    /// <summary>
    /// D6.24, A10.3, A7.8, A10.31 (pp. 205, 63, 58; ruling R65.17): the result of a Bail Out's NMC against the unit's Morale Level (its broken side's
    /// when broken). A Final DR above it breaks an unbroken unit, and Casualty Reduces a broken one; an Original 12 Casualty Reduces an unbroken unit
    /// as it breaks and eliminates a broken one; a Final DR equal to it pins an unbroken unit, and leaves a broken one as it was (A7.8: "Pinning affects broken units
    /// only during Interdiction").
    /// </summary>
    public static string Check(int first, int second, int morale, bool broken)
    {
        var dr = first + second;
        return broken ? dr == 12 ? Eliminated : dr > morale ? Reduced : Passed
            : dr > morale ? dr == 12 ? Reduced : Broken : dr == morale ? Pinned : Passed;
    }

    /// <summary>
    /// D6.24, A10.7 EXC, A19.13 (pp. 205, 68, 86; pass 70, ruling R70.9): the result of a Bail Out's NMC with a Rider leader's <paramref name="drm"/> and the
    /// unit's <paramref name="elr"/>, as <see cref="ScenarioA1MoraleCheck"/> resolves it: an unbroken unit that fails by more than its ELR is Replaced when it can
    /// be (<paramref name="replaceable"/>), and only broken otherwise.
    /// </summary>
    public static string Check(int first, int second, int morale, bool broken, int drm, int? elr, bool replaceable)
    {
        var dr = first + second;
        var final = dr + drm;
        return broken ? dr == 12 ? Eliminated : final > morale ? Reduced : Passed
            : dr == 12 ? Reduced
            : final > morale ? replaceable && elr is { } limit && final - morale > limit ? Replaced : Broken
            : final == morale ? Pinned : Passed;
    }

    /// <summary>Whether a Bail Out's NMC leaves the unit broken, reduced, replaced, or eliminated: it then keeps no SW (D6.24).</summary>
    public static bool LeavesBroken(string result, bool broken) => broken || result is Broken or Reduced or Eliminated or Replaced;

    /// <summary>
    /// D6.24 (p. 205; ruling R65.17): the fate of a SW a Bailing Out Rider carries: eliminated if the unit breaks or is eliminated, before or after the
    /// Bail Out; otherwise its dr: 1 to 3 kept, 4 to 6 malfunctioned, or eliminated for an X# SW.
    /// </summary>
    public static string WeaponFate(int? dr, bool removesOnBreakdown, bool carrierBroken) =>
        carrierBroken || dr is not { } rolled ? Eliminated : rolled <= 3 ? Kept : removesOnBreakdown ? Eliminated : Malfunctioned;

    /// <summary>D6.24 (p. 205): an unbroken Rider that Bails Out is marked with a Prep Fire counter in its own Player Turn, a Final Fire counter in the enemy's.</summary>
    public static bool PrepFireCounter(bool phasing) => phasing;

    /// <summary>D6.24 (UNIT-STATE-034): a <c>rider-bailed-out</c> record's NMC result agrees with its DR and Morale Level.</summary>
    public static bool CheckAgrees(int first, int second, int morale, bool broken, string result) => Check(first, second, morale, broken) == result;

    /// <summary>
    /// D6.24 (UNIT-STATE-034; pass 70, ruling R70.9): a record that carries the pass's <paramref name="drm"/> agrees with its DR, Morale Level, DRM, and ELR;
    /// a unit that could not be Replaced is only broken (A19.11, A19.12), which the record's lineage after it shows. An <paramref name="unarmed"/> unit's failure
    /// is a Casualty Reduction and its pass leaves it as it was (ruling R70.47; A20.54, p. 88).
    /// </summary>
    public static bool CheckAgrees(int first, int second, int morale, bool broken, string result, int drm, int? elr, bool unarmed = false) =>
        unarmed ? result == (first + second + drm <= morale && first + second != 12 ? Passed : Reduced)
            : Check(first, second, morale, broken, drm, elr, replaceable: true) == result || Check(first, second, morale, broken, drm, elr, replaceable: false) == result;

    /// <summary>D6.24 (UNIT-STATE-034): a SW's recorded fate agrees with its dr.</summary>
    public static bool WeaponAgrees(int? dr, bool removesOnBreakdown, bool carrierBroken, string fate) => WeaponFate(dr, removesOnBreakdown, carrierBroken) == fate;

    /// <summary>D6.24 (UNIT-STATE-034): the refusal of a Bail Out that names no Rider of the vehicle or disagrees with its dice.</summary>
    public static RecordRefusal BailOutRefusal() =>
        new("UNIT-STATE-034", "A Bail Out names a Rider of the vehicle, its NMC's result agrees with its DR, and each SW's fate with its dr (D6.24).");

    /// <summary>The words of a Bail Out's cause, for the proposal and the record (D6.21, D6.23).</summary>
    public static string CauseText(string cause) => cause switch
    {
        Terrain => "its AFV moves through terrain Riders may not ride through (D6.21)",
        Turret => "its AFV changes its TCA (D6.21)",
        Pin => "it is pinned (D6.23)",
        _ => "it is broken (D6.23)",
    };
}
