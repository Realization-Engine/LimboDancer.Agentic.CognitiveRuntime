using System.Globalization;

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

    /// <summary>Whether a Location is Concealment Terrain for a vehicle (A12.2; ruling R6.7): grain in season, June to September (B15.6).</summary>
    public static bool ConcealmentTerrain(int? month, Func<string?> terrain) => month is >= 6 and <= 9 && terrain() == "grain";

    /// <summary>Whether a unit is a Good Order enemy ground unit that can see (A12.2): Good Order Personnel, or a vehicle whose crew is not Stunned or Recalled.</summary>
    public static bool Watching(bool active, bool vehicle, bool stunned, bool shocked, bool unconfirmedKill, bool recalled, bool abandoned, Func<bool> personnel,
        bool broken, bool berserk, bool captured, bool melee) => active
        && (vehicle ? !stunned && !shocked && !unconfirmedKill && !recalled && !abandoned
            : personnel() && !broken && !berserk && !captured && !melee);

    /// <summary>
    /// Whether a concealed or hidden vehicle loses its "?" (A12.2; ruling R6.7): the moving vehicle moved within 16 hexes and in the LOS of a Good
    /// Order enemy ground unit, or it is not in Concealment Terrain and in the LOS of one (Case H).
    /// </summary>
    public static bool ConcealmentLost(bool movedNow, Func<int?, bool> seen, Func<bool> concealmentTerrain) =>
        (movedNow && seen(16)) || (!concealmentTerrain() && seen(null));

    /// <summary>
    /// The steps a moving vehicle may try now (rulings R11.1, R11.2, R11.7): forward outright into a hex of its VCA, or from Bypass into the hex beyond
    /// its CAFP; forward in VBM along the hexside its VCA runs along (from its hex center, the hexside its VCA hexes share; after a VCA change at its
    /// CAFP, the hexside ahead); in Reverse outright into a rear hex, from Bypass the hex beyond its rear vertex; and in Reverse VBM from its hex center
    /// along the hexside its rear hexes share (D2.22).
    /// </summary>
    public static IReadOnlyList<VehicleStepOption> MoveOptions(IVehicleStepFactReader reader, int here, int? lane, int facing, bool reverse, Func<bool> turnedAtCafp)
    {
        var options = new List<VehicleStepOption>();
        void Outright(int to, int from) => options.Add(new VehicleStepOption(to, from, null));

        void Lane(int one, int two)
        {
            foreach (var (obstacle, other) in new[] { (one, two), (two, one) })
            {
                if (reader.Bypassable(obstacle))
                {
                    options.Add(new VehicleStepOption(obstacle, here, other));
                }
            }
        }

        var direction = reverse ? (facing + 3) % 6 : facing;
        if (lane is { } straddling)
        {
            if (turnedAtCafp())
            {
                // D2.33: after a VCA change at its CAFP the vehicle goes on in Bypass along the hexside it now faces along.
                if (!reverse)
                {
                    foreach (var (one, two) in new[] { (here, straddling) }.SelectMany(pair => reader.SharedNeighbors(pair.Item1, pair.Item2)
                        .SelectMany(third => new[] { (pair.Item1, third), (pair.Item2, third) })))
                    {
                        if (reader.LaneEnds(one, two, facing) is ({ } _, { } behind) && (behind == here || behind == straddling))
                        {
                            Lane(one, two);
                        }
                    }
                }

                return options;
            }

            var (front, rear) = reader.LaneEnds(here, straddling, facing);
            if ((reverse ? rear : front) is { } beyond)
            {
                Outright(beyond, straddling);
            }

            return options;
        }

        foreach (var to in reader.VcaHexes(here, direction))
        {
            Outright(to, here);
        }

        if (reader.VcaHexes(here, direction) is [{ } first, { } second])
        {
            Lane(first, second);
        }

        return options;
    }

    /// <summary>A vehicle entry's bar (rulings R11.6, R25.10): the step's own reason first, else the Location's.</summary>
    public static string? EntryOptionBar(string? reason, Func<string?> locationBar) => reason ?? locationBar();

    /// <summary>
    /// A.4.2, E1.52, D.3, D.7, D5.34, C7.42, D5.41, C10.11, D5.341, A8.1, D7.1, D7.2, A15.43, D8.2, D8.3: why a vehicle on the map may not spend this MP,
    /// or null. Checked in this order: move ended, BU at NVR 0, its conditions (Passengers still Unload from one that Prep Fired, is immobilized, or
    /// Abandoned), Recalled and not leaving, another unit's move, the DEFENDER's window, an OVR to resolve, a berserk charge first, and a bog.
    /// </summary>
    public static string? MoveBar(string id, string kind, bool moveEnded, Func<bool> nightBuBlind, Func<string, bool> has, bool leaving,
        IReadOnlyList<string>? otherMovers, bool windowOpen, bool reaction, string? overrun, Func<string?> berserk, bool started)
    {
        ArgumentNullException.ThrowIfNull(nightBuBlind);
        ArgumentNullException.ThrowIfNull(has);
        ArgumentNullException.ThrowIfNull(berserk);
        if (moveEnded)
        {
            return $"play.move-vehicle: {id} has ended its move this MPh (A4.2)";
        }

        if (kind != "stop" && nightBuBlind())
        {
            return $"play.night-bu: {id} is BU with an NVR of 0 and spends no MP but to Stop (E1.52)";
        }

        if (new[] { ("asl:prep-fire", "it Prep Fired (D.3)"), ("asl:immobilized", "it is immobilized (D.7)"),
            ("asl:stunned", "its crew is Stunned (D5.34)"), ("asl:shocked", "it is Shocked (C7.42)"),
            ("asl:unconfirmed-kill", "it is an Unconfirmed Kill, still Shocked (C7.42)"), ("asl:abandoned", "it is Abandoned (D5.41)"),
            ("asl:ti", "it is TI after hooking up a Gun (C10.11)") }
            .Where(item => kind != "unload" || item.Item1 is not ("asl:prep-fire" or "asl:immobilized" or "asl:abandoned"))
            .FirstOrDefault(item => has(item.Item1)) is { Item2: { } why })
        {
            return $"play.move-vehicle: {id} may not move: {why}";
        }

        if (has("asl:recalled") && !leaving)
        {
            return $"play.move-vehicle: {id} may not move: it is Recalled and stopped for the rest of this Player Turn (D5.341)";
        }

        if (otherMovers is not null)
        {
            return $"play.move-order: {string.Join(", ", otherMovers)} moves until its move ends (A4.2)";
        }

        if (windowOpen)
        {
            return reaction
                ? "play.move-window: the DEFENDER may still make Reaction Fire at the OVRing vehicle (D7.2)"
                : "play.move-window: the DEFENDER may still fire at the vehicle's last MP expenditure (A8.1, A8.11)";
        }

        if (overrun is { } declared)
        {
            return $"play.move-vehicle-ovr: {id} resolves its OVR of {declared} before it spends more MP (D7.1)";
        }

        if (berserk() is { } charging)
        {
            return $"play.berserk-first: {charging} is berserk and charges before any other unit moves (A15.43)";
        }

        if (has("asl:bogged") && (kind != "start" || started))
        {
            return $"play.move-vehicle-bog: {id} is bogged; it may only attempt Bog Removal as its first expenditure of its MPh (D8.2, D8.3)";
        }

        return null;
    }

    /// <summary>D2.12, D2.7: why a vehicle may not spend a moving MP, or null: it must be moving, and after an ALL entry it may only Stop.</summary>
    public static string? MovingBar(string id, string kind, bool moving, bool afterAll) =>
        !moving ? $"play.move-vehicle: {id} is not moving; it must start first (D2.12)"
        : afterAll && kind != "stop" ? $"play.move-vehicle-all: {id} made an ALL entry, so it may only Stop or end its move in Motion (D2.7)"
        : null;

    /// <summary>D2.11, D2.33 (ruling R11.2): a VCA change turns one hexspine, and not again after one at the CAFP in Bypass.</summary>
    public static string? TurnBar(string id, int? next, int facing, bool straddling, Func<bool> turnedAtCafp)
    {
        ArgumentNullException.ThrowIfNull(turnedAtCafp);
        if (next is not { } value || Math.Abs((value - facing + 6) % 6) is not (1 or 5))
        {
            return "play.move-vehicle: a VCA change turns one hexspine (D2.11)";
        }

        return straddling && turnedAtCafp() ? $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)" : null;
    }

    /// <summary>
    /// B13.41, D2.7, D2.15, D2.24 (ruling R11.5): why an entry's cost bars it, or null. An ALL entry is the first after the Start MP; a Minimum Move
    /// enters only a hex costing more than the printed allotment, as the only forward non-VBM entry; otherwise the entry costs no more than the allotment.
    /// </summary>
    public static string? EntryCostBar(string id, string to, bool all, int halfMp, int printed, bool first, bool minimumMove, bool reverse, bool bypass)
    {
        if (all && !first)
        {
            return $"play.move-vehicle-all: entering {to} takes {id}'s whole MP allotment, so it is its first expenditure after its Start MP (B13.41, D2.7; ruling R11.5)";
        }

        if (minimumMove)
        {
            if (halfMp <= printed || all)
            {
                return $"play.move-vehicle-minimum: {to} costs {Mp(halfMp)} MP, no more than {id}'s allotment, so it needs no Minimum Move (D2.15)";
            }

            if (!first || reverse || bypass)
            {
                return "play.move-vehicle-minimum: a Minimum Move is a vehicle's only entry of its MPh, made forward and not in VBM, with no VCA change (D2.15, D2.24; ruling R11.5)";
            }
        }
        else if (halfMp > printed && !all)
        {
            return $"play.move-vehicle-mp: {to} costs {Mp(halfMp)} MP, more than {id}'s allotment; only a Minimum Move enters it (D2.15)";
        }

        return null;
    }

    /// <summary>D2.7, D7.1: an ALL entry or a Minimum Move leaves no MP for an OVR.</summary>
    public static string? OverrunAllBar(string id, bool all, bool minimumMove) =>
        all || minimumMove ? $"play.move-vehicle-ovr: an ALL entry or a Minimum Move spends {id}'s whole allotment, leaving no MP for an OVR (D2.7, D7.1)" : null;

    /// <summary>D5.341, D2.6, D2.33: why a vehicle may not Stop, or null: a leaving AFV goes on in Motion; one that may move on may not Stop in an enemy AFV's Location or after a VCA change at its CAFP.</summary>
    public static string? StopBar(string id, bool leaving, Func<string?> enemyAfvBar, Func<bool> mayMoveOn, bool straddling, Func<bool> turnedAtCafp)
    {
        ArgumentNullException.ThrowIfNull(enemyAfvBar);
        ArgumentNullException.ThrowIfNull(mayMoveOn);
        ArgumentNullException.ThrowIfNull(turnedAtCafp);
        if (leaving)
        {
            return $"play.recall-motion: {id} is Recalled and leaves in Motion, so it does not Stop (D5.341)";
        }

        if (enemyAfvBar() is { } blocked && mayMoveOn())
        {
            return "play.move-vehicle-enemy-afv: " + blocked;
        }

        return straddling && turnedAtCafp() && mayMoveOn()
            ? $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)"
            : null;
    }

    /// <summary>
    /// D2.1, D2.4, D2.7, D2.24 (rulings R11.1, R11.5; referee, pass 11): why the MP left cannot pay this expenditure, or null. A Stop after an ALL
    /// entry, a Minimum Move, or an ALL entry may exceed them; in Reverse an entry or VCA change keeps one MP to Stop.
    /// </summary>
    public static string? ExpenditureBar(string id, string kind, bool afterAll, bool minimumMove, bool? entryAll, bool reverse, int spent, int allotment, int cost,
        bool tracked)
    {
        var mayExceed = (kind == "stop" && afterAll) || minimumMove || entryAll == true;
        if (!mayExceed && spent + cost > allotment)
        {
            return $"play.move-vehicle-mp: {id} has {Mp(Math.Max(0, allotment - spent))} of its {Mp(allotment)} MP left, and this costs {Mp(cost)}"
                + (tracked ? "; an ESB DR may add MP (D2.5)" : string.Empty) + " (D2.1)";
        }

        if (reverse && ((kind == "enter" && entryAll == false) || kind == "turn") && spent + cost + 2 > allotment)
        {
            return $"play.move-vehicle-reverse: {id} in Reverse keeps one MP to Stop, since Reverse Motion is not built (D2.24; ruling R11.1)";
        }

        return null;
    }

    /// <summary>Half MP as MP for the texts: whole MP, or with a half.</summary>
    public static string Mp(int halfMp) => halfMp % 2 == 0 ? (halfMp / 2).ToString(CultureInfo.InvariantCulture)
        : halfMp == 1 ? "½" : $"{(halfMp / 2).ToString(CultureInfo.InvariantCulture)}½";

    /// <summary>
    /// D2.12, D2.13, D2.2, D2.4, D5.341: why a vehicle may not take its Start MP, or null. Not while moving; not in Reverse when leaving or towing;
    /// not without 1 MP left unless its Start MP is its Bog Removal.
    /// </summary>
    public static string? StartBar(string id, bool moving, bool inMotionUnmoved, bool reverse, bool leaving, Func<bool> towing, bool bogged, int spent,
        int allotment)
    {
        ArgumentNullException.ThrowIfNull(towing);
        if (moving)
        {
            return inMotionUnmoved
                ? $"play.move-vehicle: {id} is in Motion and needs no Start MP (D2.4)"
                : $"play.move-vehicle: {id} is already moving; it starts again only after it stops (D2.12, D2.13)";
        }

        if (reverse && (leaving || towing()))
        {
            return $"play.move-vehicle-reverse: {id} {(leaving ? "leaves forward by its Friendly Board Edge (D5.341)" : "tows a Gun and may not use Reverse movement (D2.2)")}";
        }

        if (!bogged && spent + 2 > allotment)
        {
            return $"play.move-vehicle-mp: {id} has {Mp(Math.Max(0, allotment - spent))} MP left, and starting costs 1 (D2.12)";
        }

        return null;
    }

    /// <summary>D8.3, D8.31: a Bog Removal's half MP, the colored dr times the white dr, doubled for a truck.</summary>
    public static int BogRemovalHalfMp(int colored, int white, bool truck) => colored * white * (truck ? 2 : 1) * 2;

    /// <summary>
    /// D2.5, D2.6, D2.7 (ruling R11.3): why a vehicle may not attempt ESB, or null. Tracked, moving, once per MPh, not after an ALL entry, not in an
    /// enemy AFV's Location it could not remain in, and for 1 to a quarter of its printed MP (FRD).
    /// </summary>
    public static string? EsbBar(string id, bool tracked, bool moving, Func<bool> attempted, bool afterAll, Func<string?> enemyAfvBar, int? mp, int maximum)
    {
        ArgumentNullException.ThrowIfNull(attempted);
        ArgumentNullException.ThrowIfNull(enemyAfvBar);
        if (!tracked)
        {
            return $"play.move-vehicle-esb: only a tracked vehicle attempts ESB; {id} is a truck (D2.5)";
        }

        if (!moving)
        {
            return $"play.move-vehicle-esb: {id} attempts ESB while moving: after its Start MP, or in Motion (D2.5; ruling R11.3)";
        }

        if (attempted())
        {
            return $"play.move-vehicle-esb: {id} has attempted ESB this MPh (D2.5)";
        }

        if (afterAll)
        {
            return $"play.move-vehicle-esb: {id} made an ALL entry, after which ESB is not allowed (D2.7)";
        }

        if (enemyAfvBar() is { } blocked)
        {
            return "play.move-vehicle-esb: " + blocked;
        }

        if (mp is not { } value || value < 1 || value > maximum)
        {
            return $"play.move-vehicle-esb: ESB seeks from 1 to {maximum} MP for {id}, a quarter of its printed allotment (FRD) (D2.5)";
        }

        return null;
    }

    /// <summary>D2.5: the ESB DR's manufacturer DRM, +2 German, +1 Russian, otherwise +3.</summary>
    public static int EsbNationalDrm(string? nationality) => nationality switch
    {
        "german" => 2,
        "russian" => 1,
        _ => 3,
    };

    /// <summary>
    /// D2.4 (rulings R5.14, R5.17, R11.1): why a moving vehicle may not end in Motion, or null. A halted vehicle may; never in Reverse; a Recalled one
    /// when it cannot go on along its route; otherwise when under 1 MP is left or the named next hex costs more than it has left.
    /// </summary>
    public static string? MotionBar(string id, bool halted, bool reverse, int left, bool mustLeave, Func<bool> recallMayGoOn, string? intended,
        Func<int?> intendedCost)
    {
        ArgumentNullException.ThrowIfNull(recallMayGoOn);
        ArgumentNullException.ThrowIfNull(intendedCost);
        if (halted)
        {
            return null;
        }

        if (reverse)
        {
            return $"play.vehicle-motion: {id} is moving in Reverse and Stops to end its move, since Reverse Motion is not built (D2.24; ruling R11.1)";
        }

        if (mustLeave)
        {
            return recallMayGoOn()
                ? $"play.recall-route: {id} is Recalled and has the MP to go on along its route to its Friendly Board Edge (D5.341)"
                : null;
        }

        if (left < 2)
        {
            return null;
        }

        if (intended is null)
        {
            return $"play.vehicle-motion: {id} has {Mp(left)} MP left, so it Stops (1 MP), moves on, or names the hex it wished to enter next to end in Motion (D2.4)";
        }

        return intendedCost() is not { } cost
            ? $"play.vehicle-motion: {intended} is not an ADJACENT hex {id} could enter over reviewed terrain, so it cannot be the next hex it wished to enter (D2.4)"
            : cost <= left
                ? $"play.vehicle-motion: {id} has {Mp(left)} MP left, enough to enter {intended} for {Mp(cost)}, so it Stops or moves on (D2.4)"
                : null;
    }

    /// <summary>
    /// D2.1, D2.6, D2.33, D7.1: why a vehicle may not end its move yet, or null: its MP left are already spent, its OVR is unresolved, it may move on
    /// from an enemy AFV's Location or after a VCA change at its CAFP, or it may not end in Motion.
    /// </summary>
    public static string? EndMoveBar(string id, bool ending, string? overrun, bool halted, Func<string?> enemyAfvBar, Func<bool> mayMoveOn, bool straddling,
        Func<bool> turnedAtCafp, Func<string?> motionBar)
    {
        ArgumentNullException.ThrowIfNull(enemyAfvBar);
        ArgumentNullException.ThrowIfNull(mayMoveOn);
        ArgumentNullException.ThrowIfNull(turnedAtCafp);
        ArgumentNullException.ThrowIfNull(motionBar);
        if (ending)
        {
            return "play.end-move: the vehicle has spent its MP left, and its move ends when the DEFENDER passes (D2.1)";
        }

        if (overrun is { } declared)
        {
            return $"play.end-move: {id} resolves its OVR of {declared} first (D7.1)";
        }

        if (!halted && enemyAfvBar() is { } blocked && mayMoveOn())
        {
            return "play.end-move: " + blocked;
        }

        if (!halted && straddling && turnedAtCafp() && mayMoveOn())
        {
            return $"play.end-move: {id} changed its VCA at its CAFP and must move on in Bypass before its move ends (D2.33)";
        }

        return motionBar();
    }

    /// <summary>
    /// D5.2, D5.3, D5.33, D5.34 (ruling R25.8): why an AFV's BU counter may not be placed or removed, or null. An active AFV, its owner's MPh or APh,
    /// not Stunned or Recalled, not after Prep Fire in the MPh, not while the DEFENDER may fire at its MP, a change, and once per phase.
    /// </summary>
    public static string? ButtonUpBar(string id, bool activeAfv, bool ownPhase, bool stunned, bool prepFiredInMph, bool windowOpen, bool already,
        bool buttonedUp, Func<bool> changedThisPhase)
    {
        ArgumentNullException.ThrowIfNull(changedThisPhase);
        if (!activeAfv)
        {
            return "play.button-up: only an AFV's crew buttons up or exposes itself (D5.2, D5.3)";
        }

        if (!ownPhase)
        {
            return "play.button-up: a BU counter is placed or removed only in its owner's MPh or APh (D5.33)";
        }

        if (stunned)
        {
            return $"play.button-up: {id}'s crew is Stunned and stays BU this Player Turn (D5.34)";
        }

        if (prepFiredInMph)
        {
            return $"play.button-up: {id} Prep Fired, so it may not change its CE status this MPh (D5.33)";
        }

        if (windowOpen)
        {
            return "play.button-up: the DEFENDER may still fire at its last MP expenditure (D5.33)";
        }

        if (already)
        {
            return $"play.button-up: {id} is already {(buttonedUp ? "BU" : "CE")}";
        }

        if (changedThisPhase())
        {
            return $"play.button-up: {id}'s BU counter was already placed or removed this phase (D5.33)";
        }

        return null;
    }
}
