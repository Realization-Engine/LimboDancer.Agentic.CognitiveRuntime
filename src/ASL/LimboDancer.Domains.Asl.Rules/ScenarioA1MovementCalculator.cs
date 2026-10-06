namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Hex-by-hex Infantry movement in the MPh (A4; pass 32.b, slice S3, from Play's planner): the MF a unit may spend, portage and the leader's bonus
/// and IPC loan, and the checks of a move. The caller reads the state, the map, and the catalog and hands the facts over; a catalog read it makes only
/// when a function here asks, as the planner read it.
/// </summary>
public static class ScenarioA1MovementCalculator
{
    /// <summary>
    /// The MF a unit may spend this phase (A4.11, A4.42, A4.5, A4.52; rulings R5.1 and R5.4): its allotment (a MMC's four or three, a SMC's
    /// six or three, a berserk unit's eight), plus the MF Double Time adds, at most eight (seven for Conscripts), less one MF for each PP it
    /// carries beyond its IPC (three for a MMC, one for a SMC, none for a wounded SMC; one less while CX). A berserk unit counts only its 1PP SW,
    /// since it abandons the others before it charges (A15.431). Null when the catalog does not decide it. <paramref name="allowance"/> is the
    /// unit's allotment as the catalog gives it, read for a real unit; <paramref name="portage"/> the PP of each SW it possesses, null when one is
    /// not recorded, read once the allotment is known.
    /// </summary>
    public static int? MfAllotment(MfAllotmentFacts facts, int doubleTimeMf, bool cx, int bonusMf, int ipcBonus, Func<int?> allowance, Func<IReadOnlyList<int>?> portage)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(allowance);
        ArgumentNullException.ThrowIfNull(portage);

        // A12.11 (ruling R10.10): a Dummy stack moves as if it holds a real unit, with four MF.
        if ((facts.Dummy ? 4 : allowance()) is not { } allotment || portage() is not { } carried)
        {
            return null;
        }

        if (doubleTimeMf > 0)
        {
            var conscript = facts.Class == "conscript";
            allotment = Math.Min(allotment + doubleTimeMf, conscript ? 7 : 8);
        }

        var ipc = (facts.Smc ? (facts.Wounded ? 0 : 1) : 3) - (cx ? 1 : 0) + ipcBonus;
        var pp = facts.Berserk ? carried.Where(item => item == 1).Sum() : carried.Sum();

        // B3.4, A4.12 (ruling R10.8): the Road Bonus and a leader's bonus add to the allotment.
        return allotment + bonusMf - Math.Max(0, pp - Math.Max(ipc, 0));
    }

    /// <summary>Whether a unit carries more PP than its IPC (A4.42): MMC three, SMC one, wounded SMC none, one less while CX. <paramref name="carried"/> is the PP of each SW it possesses, null when one is not recorded.</summary>
    public static bool Laden(bool smc, bool wounded, bool cx, IReadOnlyList<int>? carried)
    {
        var ipc = (smc ? (wounded ? 0 : 1) : 3) - (cx ? 1 : 0);
        return carried is not null && carried.Sum() > Math.Max(ipc, 0);
    }

    /// <summary>
    /// Whether a Good Order MMC moving with a Good Order leader of its nationality, who began the MPh with it and has moved with it at every step, has
    /// the leader's two MF bonus (A4.12; ruling R10.8). A berserk unit's MF are never increased but by the Road Bonus (A15.431).
    /// </summary>
    public static bool LeaderBonus(MovingUnitFacts unit, IReadOnlyList<MovingUnitFacts> movers)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(movers);

        // A12.11 (table player, pass 10): a Dummy moves with a leader's bonus as a real MMC would.
        var dummy = unit.Dummy;
        if ((!dummy && !unit.Mmc) || unit.Berserk || unit.Broken
            || (dummy ? movers.FirstOrDefault(item => item.Leader && item.Side == unit.Side) is not { } guide ? null : guide.Nationality : unit.Nationality) is not { } nationality)
        {
            return false;
        }

        var first = unit.MovedWith is null && unit.MfSpent == 0 && !unit.HalfMfSpent;
        return movers.Any(leader => leader.Id != unit.Id && leader.Leader && !leader.Broken && !leader.Berserk
            && leader.Nationality == nationality
            && (first ? leader.MfSpent == 0 && !leader.HalfMfSpent && leader.MovedWith is null : unit.MovedWith?.Contains(leader.Id, StringComparer.Ordinal) == true));
    }

    /// <summary>
    /// The MMC a leader lends his IPC to (A4.42; ruling R10.8): the only MMC of the stack with his leader bonus that carries more than its own IPC; the
    /// leader's own IPC is then spent. Nulls when there is none, or more than one. <paramref name="laden"/> is read by a mover's index for the movers
    /// with the bonus, as the planner read it.
    /// </summary>
    public static (string? Recipient, string? Leader) LeaderIpcRecipient(IReadOnlyList<MovingUnitFacts> movers, Func<int, bool> laden)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(laden);
        var heavy = movers.Select((unit, index) => (Unit: unit, Index: index)).Where(item => LeaderBonus(item.Unit, movers) && laden(item.Index)).Select(item => item.Unit).ToArray();
        var leader = heavy.Length != 1 ? null : movers.FirstOrDefault(item => item.Leader && !item.Broken && !item.Wounded
            && item.Nationality == heavy[0].Nationality);
        return leader is not null ? (heavy[0].Id, leader.Id) : (null, null);
    }

    /// <summary>
    /// ADJACENT (A.8, p. 43): the Locations share a hexside at the same level, with a clear LOS and no hexside terrain or
    /// cliff between them, so Infantry could advance from one to the other. The review reads it this way for the
    /// terrain it admits. <paramref name="losClear"/> is read once the geometry allows it, as the planner read it.
    /// </summary>
    public static bool IsAdjacent(AdjacencyFacts facts, Func<bool> losClear)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(losClear);
        return facts.Adjacent && facts.FromRead && facts.ToRead && facts.Crossed is { } crossed
            && facts.FromElevation == facts.ToElevation
            && crossed.HexsideTerrain is null && !crossed.Cliff
            && losClear();
    }

    /// <summary>
    /// The range to the nearest Good Order enemy ground unit with a clear LOS to a Location (A12.34); null when none has one. A Passenger is not counted:
    /// its vehicle is (ruling R26.2). The units are read in the state's order.
    /// </summary>
    public static int? NearestGoodOrderEnemyInLos(IReadOnlyList<EnemyUnitFacts> units, string side, int at, ILosFactReader los)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(los);
        return units.Where(unit => unit.Active && unit.Side != side && !unit.Dummy && !unit.Aboard && unit.Broken != true)
            .Select(unit => unit.Location).OfType<int>().Distinct()
            .Select(location => los.Los(location, at) is { Clear: true } result ? result.Range : (int?)null).Where(range => range is not null).Min();
    }

    /// <summary>
    /// An attack with, for each concealed unit that fires or directs, whether a Good Order enemy ground unit within 16 hexes has a LOS to it (A12.14,
    /// read in the PDF, p. 77; pass 31d, ruling R31d.2). The Fire package sees the target Location alone, and refused fire by concealed units at a
    /// Location with no Good Order unit as undecided; a refusal marks no firer, so a side could try a "?" stack and read from the refusal that it
    /// held Dummies. With this read the attack is made, and the firer's "?" is lost or kept as the rule has it. A hidden unit, which would have to
    /// show itself to force the loss, does not force it. At night the read is not given (E1.31), and the package decides as before.
    /// <paramref name="subjects"/> gives each firer or director the state finds, by unit id.
    /// </summary>
    public static FireAttack WithSeen(FireAttack attack, bool night, IReadOnlyDictionary<string, SeenSubjectFacts> subjects, IReadOnlyList<EnemyUnitFacts> units, ILosFactReader los)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(los);
        if (night)
        {
            return attack;
        }

        bool? Seen(string? unitId, bool? concealed) => concealed == true && unitId is not null && subjects.GetValueOrDefault(unitId) is { Side: { } side, Location: { } at }
            ? units.Where(other => other.Side != side && !other.Dummy && other.GoodOrder && !other.Hidden && !other.Aboard)
                .Select(other => other.Location).OfType<int>().Distinct()
                .Any(location => location == at || los.Los(location, at) is { Clear: true, Range: <= 16 })
            : null;
        FireFirer Firer(FireFirer item) => Seen(item.UnitId, item.Concealed) is not { } seen ? item : item with
        {
            SeenByGoodOrderEnemy = seen
        };
        FireDirector Leader(FireDirector item) => Seen(item.UnitId, item.Concealed) is not { } seen ? item : item with
        {
            SeenByGoodOrderEnemy = seen
        };
        return attack with
        {
            Firers = attack.Firers is null ? null : [.. attack.Firers.Select(Firer)],
            Director = attack.Director is null ? null : Leader(attack.Director),
            OtherDirectors = attack.OtherDirectors is null ? null : [.. attack.OtherDirectors.Select(Leader)],
        };
    }

    /// <summary>A move's first checks (A3.3): units move only in their side's MPh, and every mover is an active unit of the phasing side.</summary>
    public static string? MoveStart(string? phase, IReadOnlyList<MoveStartUnitFacts> movers, string? phasingSide)
    {
        ArgumentNullException.ThrowIfNull(movers);
        if (phase != "mph")
        {
            return "play.move-phase: units move in their side's MPh (A3.3, p. 47)";
        }

        if (movers.Any(unit => !unit.Active || unit.Side != phasingSide))
        {
            return "play.move-stack: every mover is an active unit of the phasing side";
        }

        return null;
    }

    /// <summary>
    /// Rulings R20.5, R25.3: a stack waiting off board enters along its entry edge as its first step, which is a step like any other; it never moves
    /// with units on the map, and takes no action off board (A2.52).
    /// </summary>
    public static string? OffBoardEntry(int offBoard, int movers, bool actionGiven)
    {
        if (offBoard > 0)
        {
            if (offBoard < movers)
            {
                return "play.entry-stack: units waiting off board enter apart from units on the map (A2.51; ruling R20.5)";
            }

            if (actionGiven)
            {
                return "play.entry-offboard-action: no action is allowed by units waiting off board; a stack places SMOKE or a DC once it is on the map (A2.52; ruling R25.3)";
            }
        }

        return null;
    }

    /// <summary>A4.2: a stack on the map moves from one Location: <paramref name="distinctOrigins"/> counts the movers' distinct Locations (a mover with none counts), and <paramref name="originKnown"/> says the one Location is on the map.</summary>
    public static string? Origin(bool entering, int distinctOrigins, bool originKnown) =>
        entering || (distinctOrigins == 1 && originKnown) ? null : "play.move-stack: the stack moves from one Location (A4.2)";

    /// <summary>The bars of a move (the planner's PlanMove, blocks 3 and 4), in their order; null when none applies.</summary>
    public static string? MoveBars(MoveBarsFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var movers = facts.Movers;

        // C10.3, C10.111 (ruling R8.6): a crew pushes the Gun it mans, alone, Good Order and unpinned, a QSU Gun; a crew that moves otherwise
        // abandons its Gun. C3.22 (ruling R8.9): a Gun that changed its CA in the PFPh, and its crew, do not move; A4.8: nor a TI unit.
        if (facts.PushGiven && (!facts.SinglePusher || !facts.GunMannedByPusher || !facts.PusherCrewOrHalfSquad || facts.PusherPinned || facts.PusherBroken || !facts.GunManhandlingKnown))
        {
            return "play.move-push: a Good Order, unpinned crew or HS alone pushes the Gun it mans (C10.3, C10.111)";
        }

        // C10.3: a crew pushing its Gun in the moving stack may push on though its last push made it TI; A4.61: pushing is never Assault Movement.
        if (facts.PushGiven && facts.Assault)
        {
            return "play.move-push: pushing a Gun prevents Assault Movement (C10.3)";
        }

        if (movers.FirstOrDefault(unit => unit.NoMoveThisPlayerTurn || (unit.Ti && !facts.PushingOn)) is { } halted)
        {
            return halted.BoundingFire
                ? $"play.move-halted: {halted.Id} is an Opportunity Firer and does not move this MPh (A7.25)"
                : $"play.move-halted: {halted.Id} is TI, fired a SW in the PFPh, or changed its Gun's CA there, and does not move this Player Turn (A4.8, A3.3, C3.22)";
        }

        // D2.1 (ruling R25.3): a vehicle spends its MP one expenditure at a time, by its own action; Infantry may not enter an enemy vehicle's
        // Location, since OVR (D7) and CC against a vehicle (A11.5) are not reviewed.
        if (movers.FirstOrDefault(unit => unit.Vehicle) is { } driven)
        {
            return $"play.move-vehicle-kind: {driven.Id} is a vehicle and moves by its MP expenditures (D2.1)";
        }

        // A15.43 (ruling R27.2): a berserk charge enters a vehicle's Location for the sequential CC there (A11.31); its route decides whether it may.
        if (facts.EnemyVehicleId is { } blocking && !movers.All(unit => unit.Berserk))
        {
            return $"play.move-enemy-vehicle: the enemy vehicle {blocking} is in {facts.ToText}; Infantry OVR of a vehicle is not built, and only a berserk charge enters its Location in the MPh (D7, A11.5, A15.43; rulings R25.3, R27.2)";
        }

        // A3.3 (p. 47): a unit that fired in the PFPh does not move in the MPh.
        if (movers.FirstOrDefault(unit => unit.PrepFire) is { } fired)
        {
            return $"play.move-prep-fire: {fired.Id} fired in the PFPh, so it may not move this MPh (A3.3, p. 47)";
        }

        // A20.4, A7.351 (ruling R5.7): a Massacre in the PFPh is made as if using a SW, so the unit has Prep Fired.
        if (movers.FirstOrDefault(unit => unit.MassacredInPrepFire) is { } massacring)
        {
            return $"play.move-prep-fire: {massacring.Id} massacred a prisoner in the PFPh, as if using a SW, so it may not move this MPh (A20.4, A3.3)";
        }

        // A4.61: Assault Movement is not used where the move requires CX status.
        if (facts.Assault && facts.DoubleTime)
        {
            return "play.move-assault: Assault Movement may not be combined with Double Time (A4.61, A4.5)";
        }

        // A11.15: a unit held in Melee does not leave its Location; a prisoner moves only with its Guard (A20.53).
        if (movers.FirstOrDefault(unit => unit.Melee || unit.Captured) is { } held)
        {
            return $"play.move-melee: {held.Id} is held in Melee or captured, so it does not move (A11.15, A20.53)";
        }

        // A4.1, A7.83: broken, pinned, or already-ended units do not move; A12.11, A12.14 (ruling R10.10): concealed units and Dummies move, a
        // hidden unit does not.
        if (movers.Any(unit => (!unit.Dummy && unit.Broken != false) || unit.Pinned || unit.MovementEnded || unit.Hidden))
        {
            return "play.move-unit: every mover is Good Order, unpinned, not hidden, and not done moving; a hidden unit is first placed beneath \"?\" (A4.1, A7.83, A12.32; ruling R23.5)";
        }

        // A4.2: once a stack moves, only its members move, together or apart, until the ATTACKER ends them all.
        if (facts.CurrentExists && movers.Any(unit => !facts.CurrentMembers.Contains(unit.Id, StringComparer.Ordinal)))
        {
            return facts.CurrentMembers.Count == 0
                ? $"play.move-order: {string.Join(", ", facts.CurrentMovers)} can move no farther; end their move first (A4.2, A8.11)"
                : $"play.move-order: only {string.Join(", ", facts.CurrentMembers)} of the moving stack may move until its move ends (A4.2)";
        }

        if (facts.CurrentExists && facts.WindowOpen)
        {
            return "play.move-window: the DEFENDER may still fire at the stack's last MF expenditure (A8.1, A8.11)";
        }

        return null;
    }

    /// <summary>A15.43: at the start of the MPh every berserk unit charges before any other unit moves. <paramref name="firstCharger"/> is read only when the rule asks.</summary>
    public static string? BerserkFirst(int berserk, bool currentExists, Func<string?> firstCharger)
    {
        ArgumentNullException.ThrowIfNull(firstCharger);
        return berserk == 0 && !currentExists && firstCharger() is { } charging
            ? $"play.berserk-first: {charging} is berserk and charges before any other unit moves (A15.43)"
            : null;
    }

    /// <summary>
    /// A berserk stack's step (A15.43, A15.431): every berserk unit of the Location that is not done moving, with the same wounded
    /// status, moves together and alone, never by Assault Movement, onto a shortest route to the nearest Known enemy unit in its
    /// LOS, in Bypass when the route takes it so (<paramref name="lane"/>, ruling R27.2); it enters that unit's Location. Once in a
    /// Location with a Known enemy unit it moves no farther. Returns the charged Location's text, or why the step is refused.
    /// <paramref name="atFrom"/> are the units of the Location the stack charges from; <paramref name="route"/> is read once the stack is in order.
    /// </summary>
    public static (bool? Allowed, string? Target, string? Reason) BerserkStep(IReadOnlyList<BerserkMoverFacts> movers, bool assault, bool currentExists,
        IReadOnlyList<BerserkNeighbourFacts> atFrom, string toText, string? lane, Func<ChargeRouteFacts> route)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(atFrom);
        ArgumentNullException.ThrowIfNull(route);
        if (movers.Any(unit => !unit.Berserk))
        {
            return (null, null, "play.berserk-stack: berserk units charge apart from units that are not berserk (A15.43)");
        }

        if (assault)
        {
            return (null, null, "play.berserk-assault: a berserk unit never uses Assault Movement (A15.431)");
        }

        var wounded = movers[0].Wounded;
        if (!currentExists && atFrom.Any(unit => unit.Active && unit.Side == movers[0].Side
            && unit.Berserk && !unit.MovementEnded && unit.Wounded == wounded && !movers.Any(mover => mover.Id == unit.Id)))
        {
            return (null, null, "play.berserk-stack: berserk units in one Location charge together unless one is wounded and one is not (A15.43)");
        }

        var (steps, undecided) = route();
        if (steps.TryGetValue(toText, out var step))
        {
            // Ruling R27.2: the step is taken as the shortest route takes it, in the open or in one of its Bypass lanes.
            if (lane is null ? step.Plain : step.Lanes.Contains(lane, StringComparer.Ordinal))
            {
                return (true, step.Target, null);
            }

            return (null, null, $"play.berserk-charge: the shortest route enters {toText} "
                + string.Join(" or ", (step.Plain ? ["as an ordinary step"] : Array.Empty<string>()).Concat(step.Lanes.Select(item => $"in Bypass along {item.Replace(",", " and ", StringComparison.Ordinal)}")))
                + " (A15.431, A4.3; ruling R27.2)");
        }

        if (undecided is not null)
        {
            return (null, null, undecided);
        }

        return (null, null, steps.Count == 0
            ? $"play.berserk-charge: {string.Join(", ", movers.Select(unit => unit.Id))} has no Known enemy unit to charge, or is already in its Location (A15.43)"
            : $"play.berserk-charge: {string.Join(", ", movers.Select(unit => unit.Id))} charges the nearest Known enemy unit in its LOS by a shortest route: "
                + $"{string.Join(", ", steps.Keys.Order(StringComparer.Ordinal))}, not {toText} (A15.43, A15.431)");
    }

    /// <summary>
    /// A15.431: before its charge a berserk unit abandons every SW of more than one PP; its 1PP SW beyond its IPC (A4.42: three for
    /// a MMC, one for a SMC) are its own choice, which is not reviewed. Backlog pass 15 (ruling R15.14): the owner names the 1PP SW it keeps
    /// within its IPC; with none named, those first in id order are kept. Returns the first refusal, or the SW abandoned in order.
    /// </summary>
    public static (string? Refusal, IReadOnlyList<string> Abandoned) BerserkAbandons(IReadOnlyList<BerserkAbandonFacts> movers, IReadOnlyList<string> keepNamed)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(keepNamed);
        var abandoned = new List<string>();
        foreach (var unit in movers.Where(unit => unit.FirstStep))
        {
            if (unit.Carried.Any(item => item.Portage is null))
            {
                return ($"play.berserk-sw: the portage of a SW {unit.Id} holds is not recorded (A15.431)", abandoned);
            }

            abandoned.AddRange(unit.Carried.Where(item => item.Portage > 1).Select(item => item.ItemId));

            var light = unit.Carried.Where(item => item.Portage == 1).OrderBy(item => item.ItemId, StringComparer.Ordinal).ToArray();
            var capacity = unit.Smc ? 1 : 3;
            if (light.Length > capacity)
            {
                string[] keep = [.. keepNamed.Where(id => light.Any(item => item.ItemId == id))];
                if (keep.Length > capacity)
                {
                    return ($"play.berserk-sw: {unit.Id} keeps at most {capacity} 1PP SW within its IPC (A15.431, A4.42)", abandoned);
                }

                var kept = keep.Concat(light.Select(item => item.ItemId).Where(id => !keep.Contains(id))).Take(capacity).ToHashSet(StringComparer.Ordinal);
                abandoned.AddRange(light.Where(item => !kept.Contains(item.ItemId)).Select(item => item.ItemId));
            }
        }

        return (null, abandoned);
    }

    /// <summary>Assault Movement, Double Time, and a SMC's portage (A4.61, A4.5, A4.51, A4.42, E1.51; rulings R5.1, R16.5), in their order; null when none refuses.</summary>
    public static string? MoveTiming(bool currentExists, bool currentAssault, bool assault, bool doubleTime, int? nvr, IReadOnlyList<MoveTimingUnitFacts> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);

        // A4.61: Assault Movement is declared before the stack moves, and moves it no more than one Location.
        if (currentExists && (currentAssault || assault))
        {
            return "play.move-assault: Assault Movement is declared at the start of the move and enters one Location (A4.61)";
        }

        // A4.5 (ruling R5.1): Double Time by Infantry neither broken, wounded, berserk, nor already CX, nor whose CX counter left at this MPh's start.
        // E1.51 (backlog pass 16, ruling R16.5): no Double Time for a unit whose NVR is 0.
        if (doubleTime && nvr == 0)
        {
            return "play.night-double-time: with an NVR of 0 a unit may not Double Time (E1.51)";
        }

        if (doubleTime && movers.FirstOrDefault(unit => unit.Wounded || unit.Berserk || unit.Cx || unit.NoDoubleTime) is { } tired)
        {
            return tired.Cx || tired.NoDoubleTime
                ? $"play.move-double-time: {tired.Id} is CX, or its CX counter left at the start of this MPh, so it may not Double Time (A4.5, A4.51)"
                : $"play.move-double-time: {tired.Id} is wounded or berserk and may not Double Time (A4.5, A17.2, A15.431)";
        }

        // A4.42: a SMC never portages more than two PP.
        if (movers.FirstOrDefault(unit => unit.Smc && unit.PortageSum is { } carried && carried > 2) is { } laden)
        {
            return $"play.move-portage: {laden.Id} carries more than two PP, which a SMC never portages (A4.42)";
        }

        return null;
    }

    /// <summary>A DC Placement (A23.3; ruling R15.2) or a SMOKE grenade attempt (A24.1; ruling R9.5) named with a move spends the stack's MF in its own Location.</summary>
    public static MoveActionVerdict SpecialAction(bool placeDc, bool placeDcAtGiven, bool smoke, bool smokeByGiven, int berserk, bool inBypass, bool toIsFrom)
    {
        if (placeDc)
        {
            return berserk > 0 || inBypass || !placeDcAtGiven || !toIsFrom
                ? new MoveActionVerdict("play.dc-arguments: a DC Placement names its DC and Location, the stack stays where it is, not in Bypass; a berserk stack charges (A23.3, A15.431)", false, false)
                : new MoveActionVerdict(null, true, false);
        }

        if (smoke)
        {
            // Table player, pass 10: a SMOKE attempt in Bypass is not built.
            if (inBypass)
            {
                return new MoveActionVerdict("play.smoke-bypass: a SMOKE attempt by a stack in Bypass is not built (A24.1, A4.3; ruling R10.7)", false, false);
            }

            return berserk > 0 || !smokeByGiven || !toIsFrom
                ? new MoveActionVerdict("play.smoke-arguments: a SMOKE attempt names its squad and Location, and the stack stays where it is; a berserk stack charges (A24.1, A15.43)", false, false)
                : new MoveActionVerdict(null, false, true);
        }

        return new MoveActionVerdict(null, false, false);
    }

    /// <summary>
    /// The entry's cost in half MF (A7.7, C10.3, A4.134, B16.4; rulings R8.6, R10.9, R12.11): doubled for an Encircled mover and for a pushed Gun; a
    /// Gun pushed only into Open Ground or grain at its level; a Minimum Move the stack's first and only step; marsh by Minimum Move from below and
    /// at the whole allotment. <paramref name="allotment"/> is read by a mover's index where the planner read it.
    /// </summary>
    public static (string? Refusal, int HalfMf) EntryCost(EntryCostFacts facts, Func<int, int, bool, int?> allotment)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(allotment);
        var entry = facts.Entry;
        var terrain = entry.Terrain;

        // A7.7 (ruling R12.11): the first Location an Encircled unit enters costs twice its MF.
        var halfMf = entry.HalfMf * (facts.AnyEncircled ? 2 : 1);

        // C10.3 (ruling R8.6): a Gun is pushed only into Open Ground or grain (a road hex counted as its terrain), at double the MF, never up a
        // level, into Bypass, or by Minimum Move (A4.134).
        if (facts.Pushed && (terrain is not ("open-ground" or "grain") || entry.LevelChange || facts.Bypassing || facts.Occupy || facts.MinimumMove))
        {
            return ($"play.move-push-terrain: a Gun is pushed only into Open Ground or grain at its own level in the review, not {terrain} (C10.3, A4.134; ruling R8.6)", 0);
        }

        if (facts.Pushed)
        {
            halfMf *= 2;
        }

        // A4.134 (ruling R10.9): a Minimum Move is the stack's first and only step, by units with at least one MF after portage.
        if (facts.MinimumMove && (facts.CurrentExists || facts.Assault || facts.Berserk > 0 || facts.Occupy || facts.Bypassing
            || facts.Movers.Select((unit, index) => (Unit: unit, Index: index)).Any(item => item.Unit.Spent || allotment(item.Index, 0, item.Unit.Cx) is not >= 1)))
        {
            return ("play.move-minimum: a Minimum Move is the stack's only step this MPh, by units left with at least one MF after portage, not by Assault Movement, Bypass, or a berserk charge (A4.134; ruling R10.9)", 0);
        }

        if (entry.MinimumMoveOnly && !facts.MinimumMove)
        {
            return ("play.move-marsh: marsh is entered from a lower elevation only by Minimum Move (B16.4)", 0);
        }

        // B16.4 (ruling R10.1): marsh costs each mover its whole allotment, so only units that have spent no MF this MPh enter it.
        if (entry.AllMf)
        {
            // A4.61 (table player, pass 10): marsh takes all the MF, so it is never Assault Movement.
            if (facts.Assault)
            {
                return ("play.move-assault: entering marsh uses all of a unit's MF, which Assault Movement may not (A4.61, B16.4)", 0);
            }

            if (!facts.MinimumMove && facts.Movers.Any(unit => unit.Spent))
            {
                return ("play.move-marsh: entering marsh costs a unit's whole MF allotment, so only units that have spent no MF this MPh enter it (B16.4)", 0);
            }

            // A4.134 EX (referee, pass 10): from a lower level it costs twice the allotment.
            halfMf = facts.Movers.Select((unit, index) => allotment(index, facts.DoubleTime ? 2 : unit.DoubleTimeMf, facts.DoubleTime || unit.Cx) ?? 0).Max() * (entry.MinimumMoveOnly ? 4 : 2);
            if (halfMf <= 0)
            {
                return ("play.move-mf: no mover has an MF allotment the catalog decides", 0);
            }
        }

        return (null, halfMf);
    }

    /// <summary>
    /// A4.14, A12.15 (rulings R10.11, R10.15): a Location with a Known enemy unit is not entered in the MPh, but by a berserk charge; one with only
    /// concealed or hidden enemy units or Dummies reveals one, forcing the stack back unless it charges or they were all Dummies. A12.15 (referee,
    /// pass 10): a moving stack of Dummies that tries to enter concealed enemy units is asked to show a real unit, and is removed.
    /// </summary>
    public static MoveEnemiesVerdict EnemiesAtTarget(IReadOnlyList<UnitAtTargetFacts> there, string? phasingSide, bool charge, bool occupy, bool bypassing, bool allDummies, bool minimumMove,
        string idsText, string toText)
    {
        ArgumentNullException.ThrowIfNull(there);
        string[] enemiesThere = charge || occupy || bypassing ? []
            : [.. there.Where(unit => unit.Active && unit.Side != phasingSide && !unit.Captured).Select(unit => unit.Id)];
        string[] hiddenEnemies = !charge || occupy ? []
            : [.. there.Where(unit => unit.Active && unit.Side != phasingSide && !unit.Captured && !unit.Known).Select(unit => unit.Id)];
        var known = there.Where(unit => enemiesThere.Contains(unit.Id, StringComparer.Ordinal)).Any(unit => unit.Known);
        if (known)
        {
            return new MoveEnemiesVerdict("play.move-occupied: Infantry may not enter a Location holding a Known enemy unit in the MPh; Infantry OVR outside the reviewed building case is not built (A4.14, A4.15; ruling R10.11)", false, null, enemiesThere, hiddenEnemies);
        }

        if (enemiesThere.Length > 0 && allDummies)
        {
            return new MoveEnemiesVerdict(null, true, $"play.move: {idsText} attempt {toText}, holding concealed enemy units; the moving stack shows no real unit, so its Dummies are removed (A12.15)", enemiesThere, hiddenEnemies);
        }

        if (enemiesThere.Length > 0 && minimumMove)
        {
            return new MoveEnemiesVerdict("play.move-occupied: a Minimum Move into concealed enemy units is not reviewed (A4.134, A12.15)", false, null, enemiesThere, hiddenEnemies);
        }

        return new MoveEnemiesVerdict(null, false, null, enemiesThere, hiddenEnemies);
    }

    /// <summary>
    /// A4.11, A4.42, A4.5, A4.12, B3.4: the MF each mover has left, with Double Time, portage, and the leader and Road bonuses; A4.61: Assault Movement
    /// may not use all of the allotment without Double Time. A Minimum Move needs none (A4.134), and marsh takes it all (B16.4). E1.51 (backlog pass
    /// 16, ruling R16.5): no road bonus with an NVR of 0. Null when every mover has the MF.
    /// </summary>
    public static string? MfLeft(IReadOnlyList<MfLeftMoverFacts> movers, bool minimumMove, bool allMf, bool doubleTime, bool roadRate, bool pushed, int? nvr, bool assault, int halfMf,
        (string? Recipient, string? Leader) leaderIpc, MfAllotmentRead allotment)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(allotment);
        for (var index = 0; index < movers.Count; index++)
        {
            var unit = movers[index];
            if (minimumMove || allMf)
            {
                continue;
            }

            var extra = doubleTime ? (unit.FirstStep ? 2 : 1) : unit.DoubleTimeMf;
            var exhausted = doubleTime || unit.Cx;
            var bonus = (roadRate && !unit.OffRoad && !pushed && nvr != 0 ? 1 : 0) + (unit.LeaderBonus ? 2 : 0);
            var ipc = unit.Id == leaderIpc.Recipient ? 1 : unit.Id == leaderIpc.Leader ? -1 : 0;
            if (allotment(index, extra, exhausted, bonus, ipc) is not { } allowance || allotment(index, 0, exhausted, bonus, ipc) is not { } plain)
            {
                return $"play.move-mf: {unit.Id} has no MF allowance the catalog decides";
            }

            var spent = unit.HalfMfSpent;
            var left = (allowance * 2) - spent;
            if (left < halfMf || (assault && (plain * 2) - spent <= halfMf))
            {
                return $"play.move-mf: {unit.Id} has {left / 2m} MF left, and the entry costs {halfMf / 2m}"
                    + (assault && left >= halfMf ? "; Assault Movement may not use all of a unit's MF (A4.61)" : " (A4.11, A4.42, A4.61; Minimum Move, A4.134)");
            }
        }

        return null;
    }

    /// <summary>
    /// The DEFENDER passes on the moving stack's latest MF expenditure (A8.11). Pass 25 (table player, pass 15): with every member broken, pinned, or
    /// eliminated by the DEFENDER's fire, passing also ends the stack's move. <paramref name="movement"/> is null when no stack is moving.
    /// </summary>
    public static (string? Refusal, bool Gone, string Summary) PassFire(PassFireFacts? movement)
    {
        if (movement is not { WindowOpen: true })
        {
            return ("play.pass-window: no moving stack awaits the DEFENDER", false, string.Empty);
        }

        var gone = movement.Members == 0 && !movement.Vehicle && !movement.Ending && !movement.OverrunPending;
        return (null, gone, gone
            ? $"play.pass: the DEFENDER fires no more in {movement.LocationText}; no member of the moving stack is left to move"
                + (movement.Living.Count > 0 ? $" ({string.Join(", ", movement.Living)} broken or pinned)" : " (every mover eliminated)") + ", so its move is over (A4.2, A8.1)"
            : $"play.pass: the DEFENDER does not fire at {string.Join(", ", movement.Movers)} in {movement.LocationText}"
                + (movement.Ending ? $"; {string.Join(", ", movement.Movers)} ends its move (D2.1)" : string.Empty));
    }

    /// <summary>
    /// The ATTACKER ends the move of the moving stack's members (A4.2, A8.11): the named ones, or all of them. Those units may
    /// not move again this MPh; the stack's move is over when none is left. With no member left, because each broke or was
    /// pinned, the units of its latest step are ended. <paramref name="berserk"/> is read for each ending unit, null when it is not an active berserk
    /// unit with a Location. <paramref name="movement"/> is null when no stack is moving.
    /// </summary>
    public static EndMoveVerdict EndMove(EndMoveFacts? movement, Func<string, EndMoveBerserkFacts?> berserk)
    {
        ArgumentNullException.ThrowIfNull(berserk);
        if (movement is null)
        {
            return new EndMoveVerdict("play.end-move: no stack is moving", false, [], [], string.Empty);
        }

        // Pass 25 (table player, pass 15): the refusal says what to do, and that no member is left when the DEFENDER's fire broke or eliminated them all.
        if (movement.WindowOpen)
        {
            return new EndMoveVerdict($"play.end-move: the DEFENDER's window at {movement.LocationText} is open; the DEFENDER fires or passes first (A8.11)"
                + (movement.Members.Count == 0 ? "; no member of the moving stack is left to move, so passing ends its move" : string.Empty), false, [], [], string.Empty);
        }

        var ending = movement.Named.Count > 0 ? movement.Named : movement.Members.Count > 0 ? movement.Members : movement.Movers;
        if (ending.Distinct(StringComparer.Ordinal).Count() != ending.Count
            || ending.Any(id => !movement.Members.Contains(id, StringComparer.Ordinal) && !movement.Movers.Contains(id, StringComparer.Ordinal)))
        {
            return new EndMoveVerdict($"play.end-move: only the moving stack's members ({string.Join(", ", movement.Members)}) end their move (A4.2)", false, [], [], string.Empty);
        }

        // D2.1, D2.4: a vehicle ends its move in Motion or stopped, spending its MP left in its hex first (rulings R5.14, R5.15).
        if (movement.Vehicle)
        {
            return new EndMoveVerdict(null, true, ending, [], string.Empty);
        }

        // A4.32 (ruling R10.7): no unit ends its move in Bypass; it leaves or occupies the obstacle first (a unit that broke or was pinned there has left
        // the stack).
        if (movement.InBypass && ending.Any(id => movement.Members.Contains(id, StringComparer.Ordinal) && movement.Movers.Contains(id, StringComparer.Ordinal)))
        {
            return new EndMoveVerdict("play.end-move-bypass: Infantry may not end its move in Bypass; it leaves the hex or pays to occupy the obstacle (A4.32)", false, [], [], string.Empty);
        }

        // A15.43, A15.431: a berserk unit keeps charging while it has the MF for a step on its route; when the model cannot decide the
        // route it may end its move, a recorded deviation (ruling R30.5).
        var note = string.Empty;
        foreach (var id in ending)
        {
            if (berserk(id) is not { } charge)
            {
                continue;
            }

            if (charge.StepHalfMfs.Any(step => step <= charge.Left))
            {
                return new EndMoveVerdict($"play.berserk-charge: {id} still has the MF to charge on (A15.43, A15.431)", false, [], [], string.Empty);
            }

            if (charge.Undecided is not null)
            {
                note = $"; {id}'s charge is not decided by the model, so it ends in place (ruling R30.5: {charge.Undecided})";
            }
        }

        string[] remaining = [.. movement.Members.Where(id => !ending.Contains(id, StringComparer.Ordinal))];
        return new EndMoveVerdict(null, false, ending, remaining,
            $"play.end-move: {string.Join(", ", ending)} end their move" + (remaining.Length > 0 ? $"; {string.Join(", ", remaining)} may move on (A4.2)" : string.Empty) + note);
    }

    /// <summary>Whether any Good Order enemy ground unit within 16 hexes has a clear LOS to a Location (A12.14, A12.141). The units are read in the state's order.</summary>
    public static bool EnemyGoodOrderInLosWithin16(IReadOnlyList<EnemyUnitFacts> units, string side, int at, ILosFactReader los)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(los);
        return units.Where(unit => unit.Active && unit.Side != side && !unit.Dummy && unit.Broken != true)
            .Select(unit => unit.Location).OfType<int>().Distinct()
            .Any(location => los.Los(location, at) is { Clear: true, Range: <= 16 });
    }
}
