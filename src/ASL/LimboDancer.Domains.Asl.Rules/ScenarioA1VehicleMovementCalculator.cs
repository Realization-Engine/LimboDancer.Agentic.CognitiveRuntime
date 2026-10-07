namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of a vehicle's movement (S8; backlog pass 11, rulings R11.1 to R11.12; unit step 25, rulings R25.3 to R25.10): what a vehicle is,
/// its MP, its VCA, the bars on its entries and its setup. The caller reads the state and the map and writes the events.
/// </summary>
public static class ScenarioA1VehicleMovementCalculator
{
    /// <summary>D1.2: a unit is an AFV when it is a vehicle whose definition is not unarmored.</summary>
    public static bool IsAfv(bool vehicle, bool? unarmored) => vehicle && unarmored == false;

    /// <summary>D1.23 (ruling R7.11): a closed-topped AFV is armored and not open-topped.</summary>
    public static bool IsClosedTopped(bool afv, bool? openTopped) => afv && openTopped != true;

    /// <summary>D1.1, D2.5: the half MP spent this MPh and the allotment, with the MP a successful ESB added.</summary>
    public static (int Spent, int Allotment) HalfMp(int mfSpent, bool halfMfSpent, int movementPoints, int esbMp) =>
        ((mfSpent * 2) + (halfMfSpent ? 1 : 0), (movementPoints + esbMp) * 2);

    /// <summary>D2.11, C3.2 (rulings R8.7, R26.6): a neighbor is in the VCA when its bearing is 30 degrees either side of the facing hexspine.</summary>
    public static bool InVca(double bearing, double facing) => Math.Abs(Math.Abs(((bearing - facing + 540) % 360) - 180) - 30) < 1;

    /// <summary>Ruling R5.17: a Recall route takes an outright forward entry only with no Bog DR and no ALL entry.</summary>
    public static int? RecallEntryHalfMp(bool all, int? bogDrm, int halfMp) => !all && bogDrm is null ? halfMp : null;

    /// <summary>Ruling R25.7, D5.1: a vehicle has a MG on the IFT when its MA is an AAMG and an AAMG value is printed.</summary>
    public static bool HasVehicleMg(bool vehicle, string? mainArmament, bool antiAircraftMgPrinted) =>
        vehicle && mainArmament == "aamg" && antiAircraftMgPrinted;

    /// <summary>Rulings R11.6, R25.10, D2.1: a Location with units held in Melee is not reviewed for a vehicle's entry; enemy units alone do not bar it.</summary>
    public static string? EntryBar(bool meleeThere) =>
        meleeThere ? "units there are held in Melee, and a vehicle's entry into a Melee Location is not reviewed" : null;

    /// <summary>D2.15, D2.7 (ruling R11.5): only a vehicle that spent no MP this MPh but a Start MP, not a Bog Removal, may make an ALL entry or a Minimum Move.</summary>
    public static bool FirstEntry(IEnumerable<(bool Start, bool BogRemoval)> steps) => steps.All(step => step.Start && !step.BogRemoval);

    /// <summary>D2.7 (ruling R11.5): after an ALL entry this MPh the vehicle may only Stop or end in Motion.</summary>
    public static bool AfterAllEntry(IEnumerable<(bool Enter, bool All)> steps) => steps.Any(step => step.Enter && step.All);

    /// <summary>D.7, D5.34, D5.341, D8.2: a vehicle that may spend no more MP: not active, immobilized, Stunned, Shocked, UK, Abandoned, bogged, or Recalled without Stun recovery.</summary>
    public static bool Halted(bool active, bool immobilized, bool stunned, bool shocked, bool unconfirmedKill, bool abandoned, bool bogged, bool recalled,
        bool stunRecovery) =>
        !active || immobilized || stunned || shocked || unconfirmedKill || abandoned || bogged || (recalled && !stunRecovery);

    /// <summary>
    /// D2.6, D2.33 (table-player finding, pass 11): a vehicle may move on when it can pay for a non-ALL entry now or, unless it changed its VCA at its
    /// CAFP in Bypass, a VCA change and a 1 MP entry.
    /// </summary>
    public static bool MayMoveOn(int spent, int allotment, Func<IEnumerable<(int? HalfMp, bool All)>> entries, bool straddling, Func<bool> turnedAtCafp,
        Func<int> turnHalfMp)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(turnedAtCafp);
        ArgumentNullException.ThrowIfNull(turnHalfMp);
        var left = allotment - spent;
        return entries().Any(item => item.HalfMp is { } cost && !item.All && cost <= left)
            || (!(straddling && turnedAtCafp()) && turnHalfMp() + 2 <= left);
    }

    /// <summary>D2.11: the half MP to enter an ADJACENT hex, ALL as the printed allotment, plus a VCA change's cost for each hexspine the VCA must turn.</summary>
    public static int IntendedEntryHalfMp(bool all, int entryHalfMp, int printedHalfMp, int turnHalfMp, int turns) =>
        (all ? printedHalfMp : entryHalfMp) + (turnHalfMp * turns);

    /// <summary>
    /// Rulings R25.3, R25.10, R11.6, R11.7, A2.52, D2.11, A12.2: why a set-up vehicle is outside the reviewed cases, or null. Its tow and Passengers first;
    /// off board it needs nothing more; on the map with a VCA, not concealed or hidden outside Concealment Terrain, at ground level in the reviewed
    /// terrain, and with no enemy unit in its Location.
    /// </summary>
    public static string? VehicleSetupBar(string vehicle, Func<string?> loadBar, bool offBoard, string? location, bool concealedOrHidden,
        Func<bool> inConcealmentTerrain, int level, Func<string?> terrain, Func<bool> enemyThere)
    {
        ArgumentNullException.ThrowIfNull(loadBar);
        ArgumentNullException.ThrowIfNull(inConcealmentTerrain);
        ArgumentNullException.ThrowIfNull(terrain);
        ArgumentNullException.ThrowIfNull(enemyThere);
        if (loadBar() is { } bar)
        {
            return bar;
        }

        if (offBoard)
        {
            return null;
        }

        if (location is null)
        {
            return $"play.setup-vehicle: {vehicle} is set up on the map with its VCA facing a hexspine, or off board to enter (D2.11, A2.52)";
        }

        if (concealedOrHidden && !inConcealmentTerrain())
        {
            return $"play.setup-vehicle: {vehicle} sets up concealed or hidden only in Concealment Terrain, which for a vehicle here is grain in season (A12.2, A12.12, B15.6; ruling R6.7)";
        }

        if (level != 0 || terrain() is not ("open-ground" or "grain" or "brush" or "woods"))
        {
            return $"play.setup-vehicle: {vehicle} sets up at ground level in Open Ground, Grain, brush, woods, or a road hex, the terrain the Vehicle rules reviewed (rulings R25.3, R11.7)";
        }

        if (enemyThere())
        {
            return $"play.setup-vehicle: {vehicle} shares {location} with an enemy unit at setup, which is not reviewed (ruling R25.3)";
        }

        return null;
    }

    /// <summary>
    /// A2.52, D6.1 (ruling R26.2): why a vehicle's Passengers at setup are refused, or null: Personnel of its side and OB group, neither concealed nor
    /// hidden, within its capacity.
    /// </summary>
    public static string? PassengerSetupBar(string side, string? group, IReadOnlyList<PassengerSetupFacts> riding, string vehicle, Func<string?> capacityBar)
    {
        ArgumentNullException.ThrowIfNull(riding);
        ArgumentNullException.ThrowIfNull(capacityBar);
        if (riding.Count == 0)
        {
            return null;
        }

        if (riding.FirstOrDefault(unit => unit.Side != side || unit.Group != group || unit.Vehicle || !unit.Personnel) is { } stranger)
        {
            return $"play.setup-passenger: {stranger.Id} sets up as a Passenger of {vehicle}, which takes Personnel of its own side and OB group (A2.52; ruling R26.2)";
        }

        if (riding.FirstOrDefault(unit => unit.ConcealedOrHidden) is { } hiding)
        {
            return $"play.setup-passenger: {hiding.Id} sets up as a Passenger, neither under \"?\" nor hidden (ruling R26.2)";
        }

        return capacityBar() is { } full ? $"play.setup-passenger: {full}" : null;
    }

    /// <summary>
    /// C10.1, C10.2, C10.13 (ruling R26.1): why a Gun set up in tow is refused, or null: one Gun, a T# no greater than its M#, and QSU (limbering is not
    /// built).
    /// </summary>
    public static string? TowSetupBar(string vehicle, IReadOnlyList<string> towed, int? towing, int? manhandling, bool quickSetUp)
    {
        ArgumentNullException.ThrowIfNull(towed);
        if (towed.Count == 0)
        {
            return null;
        }

        if (towed.Count > 1)
        {
            return $"play.setup-tow: {vehicle} tows one Gun (C10.1)";
        }

        var gun = towed[0];
        if (towing is not { } t || manhandling is not { } m || t > m)
        {
            return $"play.setup-tow: {vehicle} tows {gun} only when it has a T# no greater than the Gun's M# (C10.1)";
        }

        if (!quickSetUp)
        {
            return $"play.setup-tow: {gun} is not QSU, and limbering is not built, so it is not set up in tow (C10.2; ruling R26.1)";
        }

        return null;
    }
}
