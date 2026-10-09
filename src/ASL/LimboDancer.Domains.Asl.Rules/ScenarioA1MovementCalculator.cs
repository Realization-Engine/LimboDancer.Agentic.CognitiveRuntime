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
    /// ADJACENT (A.8, p. 43; pass 35): two Locations are ADJACENT when there is a LOS between them and a hypothetical Infantry unit could move from one
    /// into the other in the APh, enemy presence ignored. So a hex one level up a hill, a hex across a wall or hedge, and the next level of a stairwell
    /// hex are ADJACENT; a hex across a cliff, and the upper level of the next hex from the ground, are not. The advance is read as the Infantry step the
    /// movement rules allow, in either direction, and the LOS only when a step exists; the two levels of one hex that a stairwell joins have their LOS by it.
    /// Until pass 35 the game asked for the same level and no hexside terrain, which was narrower than the rule.
    /// </summary>
    public static bool IsAdjacent(bool sameHex, Func<bool> couldAdvanceEitherWay, Func<bool> losClear)
    {
        ArgumentNullException.ThrowIfNull(couldAdvanceEitherWay);
        ArgumentNullException.ThrowIfNull(losClear);
        return couldAdvanceEitherWay() && (sameHex || losClear());
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

    /// <summary>
    /// A step's reveal and record (A12.15, A.9, A12.11, A2.51; rulings R10.11, R10.15, R25.3, R27.3, R31d.3): a Location with concealed or hidden
    /// enemy units reveals one, forcing the stack back unless it charges or they were all Dummies; a charge draws among every counter there, Dummies
    /// too; a stack forced back off board is beyond every attack; a stack of Dummies alone that moves without Assault Movement, or into Open Ground,
    /// is warned with "if", whatever the game knows, and at night nothing is said (E1.31).
    /// </summary>
    public static MoveStepVerdict MoveStep(MoveStepFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var ids = facts.Ids;
        var revealing = facts.EnemiesThere.Count > 0 ? facts.EnemiesThere : facts.HiddenEnemies;
        string[] realOnes = [.. revealing.Where(unit => !unit.Dummy).OrderBy(unit => unit.Id, StringComparer.Ordinal).Select(unit => unit.Id)];
        var forcedBack = facts.EnemiesThere.Count > 0 && realOnes.Length > 0;
        var charge = facts.ChargeText is not null;
        var summary = $"{(facts.Entering ? "play.enter" : "play.move")}: {string.Join(", ", ids)} " + (forcedBack ? $"{(ids.Count == 1 ? "attempts" : "attempt")} {facts.ToText}" : facts.Occupy ? $"{(ids.Count == 1 ? "occupies" : "occupy")} the obstacle of {facts.ToText}" : $"{(ids.Count == 1 ? "enters" : "enter")} {facts.ToText}")
            + $" ({facts.Terrain}{(facts.BypassTexts is not null ? " in Bypass along " + string.Join(", ", facts.BypassTexts) : string.Empty)}) for {facts.HalfMf / 2m} MF"
            + (facts.Assault ? ", by Assault Movement" : string.Empty)
            + (facts.DoubleTime ? ", Double Timing and now CX (A4.5)" : string.Empty)
            + (facts.MinimumMove ? ", a Minimum Move: pinned and CX once the DEFENDER's fire is done (A4.134)" : string.Empty)
            + (charge ? $", charging {facts.ChargeText} (A15.43)" : string.Empty)
            + (facts.AbandonedIds.Count > 0 ? $"; {string.Join(", ", facts.AbandonedIds)} abandoned before the charge (A15.431)" : string.Empty);

        // Pass 31d (design D5; A12.11, read in the PDF, p. 76; ruling R31d.3): a stack of Dummies alone that moves without Assault Movement, or into
        // Open Ground, is removed in the LOS of a Good Order enemy unit. The mover is told so with "if", whatever the game knows: whether an enemy
        // "?" that sees the hex is a real unit is not the mover's to learn before the move. At night the rule is another (E1.31), and nothing is said.
        string[] dummyWarning = !facts.Night && !forcedBack && facts.AllDummies && (!facts.Assault || facts.Terrain == "open-ground")
            ? [$"play.dummies: this stack holds no real unit; it is removed if a Good Order enemy unit within 16 hexes has a LOS to it in {facts.ToText} (A12.11)"]
            : [];

        // A2.51 (ruling R25.3): a stack forced back off board is beyond every attack.
        var offMap = facts.Entering && forcedBack;

        // A12.15 (rulings R10.11, R10.15): the reveal. Hidden units first go beneath a "?"; a Random Selection among several real units reveals the
        // highest dr (ties all); Dummies alone are removed and the stack enters.
        // A.9 (ruling R27.3): a charge draws among every counter there, Dummies too; each Dummy drawn above the first real unit is eliminated, and
        // the Dummies drawn below it stay. Ties are all drawn together.
        string[] pool = charge && realOnes.Length > 0 ? [.. revealing.OrderBy(unit => unit.Id, StringComparer.Ordinal).Select(unit => unit.Id)] : realOnes;
        var needsSelection = revealing.Count > 0 && realOnes.Length > 0 && pool.Length > 1;

        // C10.3: a Gun's push plan takes the summary as it stands here, before the entry's and the reveal's sentences are added.
        var pushSummary = summary;
        if (facts.Entering)
        {
            summary += " from off board, the stack's first MF expenditure (A2.51; ruling R25.3)";
        }

        if (revealing.Count > 0)
        {
            summary += offMap
                ? $"; a concealed unit at {facts.ToText} is revealed and the stack is forced back off board with the MF spent, its MPh over; it may still enter by advance in the APh (A12.15, A2.5; ruling R25.3)"
                : forcedBack
                ? $"; a concealed unit at {facts.ToText} is revealed and the stack stays in {facts.FromText} with the MF spent, its move ending (A12.15)"
                : charge ? $"; the charge enters {facts.ToText} and draws among the counters there by Random Selection: each Dummy drawn before a real unit is eliminated (A15.431, A12.15, A.9)"
                : $"; only Dummies were at {facts.ToText}, and they are removed (A12.15)";
        }

        return new MoveStepVerdict(revealing, realOnes, forcedBack, offMap, facts.RoadRate && !facts.Pushed && !forcedBack, summary, pushSummary, dummyWarning, pool, needsSelection,
            revealing.Count > 0 && facts.EnemiesThere.Count > 0, revealing.Count > 0 && facts.EnemiesThere.Count == 0, needsSelection ? "random-selection" : "residual");
    }

    /// <summary>
    /// The reveal's outcome (A12.15, A.9): the hidden units that first go beneath a "?", the units shown (the first real unit with no dice; with dice,
    /// the highest group that holds a real unit), and the Dummies drawn above them, or every Dummy when no real unit is there.
    /// </summary>
    public static (IReadOnlyList<string> ToConceal, IReadOnlyList<string> Shown, IReadOnlyList<string> DrawnDummies) RevealDraw(MoveStepVerdict verdict, IReadOnlyList<int>? dice)
    {
        ArgumentNullException.ThrowIfNull(verdict);
        var revealing = verdict.Revealing;
        var realOnes = verdict.RealOnes;
        var dummies = revealing.Where(unit => unit.Dummy).Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
        string[] toConceal = [.. revealing.Where(unit => unit.Hidden).Select(unit => unit.Id)];
        var shown = new List<string>();
        var drawnDummies = realOnes.Count == 0 ? [.. revealing.Where(unit => unit.Dummy).Select(unit => unit.Id)] : new List<string>();
        if (realOnes.Count > 0 && dice is null)
        {
            shown.Add(realOnes[0]);
        }
        else if (realOnes.Count > 0)
        {
            foreach (var draw in verdict.Pool.Select((id, index) => (Id: id, Dr: dice![index])).GroupBy(item => item.Dr).OrderByDescending(group => group.Key))
            {
                drawnDummies.AddRange(draw.Where(item => dummies.Contains(item.Id)).Select(item => item.Id));
                shown.AddRange(draw.Where(item => !dummies.Contains(item.Id)).Select(item => item.Id));
                if (shown.Count > 0)
                {
                    break;
                }
            }
        }

        return (toConceal, shown, drawnDummies);
    }

    /// <summary>
    /// A12.14 (ruling R10.10): a concealed mover or Dummy loses "?" when it moves without Assault Movement, or into Open Ground, in the LOS of a
    /// Good Order enemy ground unit within 16 hexes; a forced back reveals the whole stack (A12.15). E1.31 (backlog pass 16, ruling R16.4): at night
    /// a mover loses "?" only by Non-Assault Movement in an Illuminated Location. <paramref name="enemySees"/> and <paramref name="illuminated"/>
    /// are read where the planner read them. Returns each mover that loses its "?", a Dummy to be removed and a real unit to be revealed.
    /// </summary>
    public static IReadOnlyList<MoverConcealmentFacts> Unmasked(IReadOnlyList<MoverConcealmentFacts> movers, bool forcedBack, bool assault, bool night, string terrain,
        Func<bool> enemySees, Func<bool> illuminated)
    {
        ArgumentNullException.ThrowIfNull(movers);
        ArgumentNullException.ThrowIfNull(enemySees);
        ArgumentNullException.ThrowIfNull(illuminated);
        var seen = forcedBack || (enemySees() && (night ? !assault && illuminated() : !assault || terrain == "open-ground"));
        return [.. movers.Where(unit => seen && (unit.Dummy || unit.Concealed))];
    }

    /// <summary>
    /// What follows the step's record (A8.22, A9.22, C10.3, A8.2; ruling R12.7): a pushed Gun never enters Residual FP; with no Random Selection, no
    /// Residual FP, and no Fire Lane the plan needs no roll.
    /// </summary>
    public static (string? Refusal, bool Push, bool ReadyWithoutRoll) StepGate(bool needsSelection, bool residualPresent, int lanes, bool pushed)
    {
        if (pushed)
        {
            return residualPresent || lanes > 0
                ? ("play.move-push-residual: pushing a Gun into Residual FP is not reviewed (C10.3, A8.2)", false, false)
                : (null, true, false);
        }

        return (null, false, !needsSelection && residualPresent is false && lanes == 0);
    }

    /// <summary>A8.22, A12.15: the Residual FP attack on the entering stack could not be read from the state after the step.</summary>
    public static string ResidualUnreadable => "play.move-residual: the Residual FP attack on the entering stack cannot be read";

    /// <summary>The reasons the Residual FP attack is refused when the Fire package's precheck finds any; null when it finds none.</summary>
    public static IReadOnlyList<string>? ResidualUndecided(IReadOnlyList<string> precheck)
    {
        ArgumentNullException.ThrowIfNull(precheck);
        return precheck.Count != 0 ? ["play.move-residual: the Fire package does not decide the Residual FP attack this entry would suffer", .. precheck] : null;
    }

    /// <summary>A9.22: a Fire Lane attack the Fire package does not decide refuses the entry.</summary>
    public static string FireLaneUndecided => "play.move-fire-lane: the Fire package does not decide the Fire Lane attack this entry would suffer (A9.22)";

    /// <summary>The reasons of a plan whose step rolls: the summary, the Dummy warning, the Residual FP attack (A8.22), and each Fire Lane's (A9.22).</summary>
    public static IReadOnlyList<string> StepReasons(string summary, IReadOnlyList<string> dummyWarning, int? residualFp, string landedText, IReadOnlyList<FireLaneAttackFacts> lanes)
    {
        ArgumentNullException.ThrowIfNull(dummyWarning);
        ArgumentNullException.ThrowIfNull(lanes);
        return [summary, .. dummyWarning, .. residualFp is { } fp ? [$"play.move: {fp} Residual FP in {landedText} attacks the stack first (A8.22)"] : Array.Empty<string>(),
            .. lanes.Select(item => $"play.move: the Fire Lane of {item.Weapon} attacks the stack in {landedText} with {item.Fp} Residual FP (A9.22)")];
    }

    /// <summary>
    /// A recorded movement step's checks as the projector makes them (the projector's StepMovement, blocks 1 to 4 and 6 to 8; UNIT-STATE-029), in
    /// their order: a stack of the phasing side that may still move, in one Location, in the MPh, at a positive cost; no Prep Firer, no unit held in
    /// Melee or captured; the moving stack's members continue after the window closes with the same Assault declaration, on the next step; Double
    /// Time by Personnel that may; a stack entering from off board that is forced back stays there; a Minimum Move the first and only step, a forced
    /// back in place, a Bypass of one or two hexsides; an exit from the stack's own Location. The SMOKE attempt's check is its own calculator's.
    /// </summary>
    public static RecordRefusal? VerifyStepStart(StepRecordFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var movers = facts.Movers;
        if (facts.Phase != "mph" || movers.Count == 0 || movers.Any(unit => !unit.Active || unit.Side != facts.PhasingSide || unit.MovementEnded)
            || movers.Select(unit => unit.LocationText).Distinct().Count() != 1 || facts.HalfMf <= 0)
        {
            return new RecordRefusal("UNIT-STATE-029", "A movement step moves a stack of the phasing side that may still move, in one Location, in the MPh.");
        }

        // A3.3 (p. 47): a unit that fired in the PFPh does not move in the MPh.
        if (movers.FirstOrDefault(unit => unit.PrepFire) is { } fired)
        {
            return new RecordRefusal("UNIT-STATE-029", $"'{fired.Id}' fired in the PFPh, so it may not move in the MPh (A3.3, p. 47).");
        }

        // A11.15: a unit held in Melee does not leave its Location; a prisoner moves only with its Guard (A20.53).
        if (movers.FirstOrDefault(unit => unit.Melee || unit.Captured) is { } held)
        {
            return new RecordRefusal("UNIT-STATE-029", $"'{held.Id}' is held in Melee or captured, so it does not move (A11.15, A20.53).");
        }

        // A4.2: the stack's members may move on together or apart, but only they may move until every one has ended.
        if (facts.CurrentExists && (movers.Any(unit => !facts.CurrentMembers.Contains(unit.Id, StringComparer.Ordinal)) || facts.CurrentWindowOpen || facts.CurrentAssault != facts.Assault))
        {
            return new RecordRefusal("UNIT-STATE-029",
                "The moving stack's members continue, together or apart, only after the DEFENDER's window closes; another stack moves after every member ends (A4.2, A8.11).");
        }

        var nextStep = (facts.CurrentExists ? facts.CurrentStep : 0) + 1;
        if (facts.Step != nextStep)
        {
            return new RecordRefusal("UNIT-STATE-029", $"The next movement step is {nextStep}.");
        }

        // A4.5 (ruling R5.1): Double Time by Infantry neither broken, wounded, berserk, nor CX, nor rested from CX at this MPh's start.
        if (facts.DoubleTime && movers.FirstOrDefault(unit => !unit.Personnel || unit.NoDoubleTime || unit.Broken || unit.Wounded || unit.Berserk || unit.Cx) is { } tired)
        {
            return new RecordRefusal("UNIT-STATE-029", $"'{tired.Id}' may not Double Time: it is broken, wounded, berserk, or CX, or its CX counter left at this MPh's start (A4.5, A4.51).");
        }

        return null;
    }

    /// <summary>The checks of a recorded movement step that follow the SMOKE attempt's (the projector's StepMovement, blocks 6 to 8), in their order; see <see cref="VerifyStepStart"/>.</summary>
    public static StepRecordVerdict VerifyStepRecord(StepRecordFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var movers = facts.Movers;

        // A12.15, A2.51 (ruling R25.3): a stack entering from off board that is forced back stays off board, its MF spent and its move over; no fire
        // reaches it there, so no window opens.
        if (facts.AttemptedText is { } offBoardAttempt && offBoardAttempt == facts.ToText && !facts.CurrentExists && movers.All(unit => unit.OffMapWaiting))
        {
            return new StepRecordVerdict(null, true);
        }

        // A4.134 (ruling R10.9): a Minimum Move is a stack's first and only step; A12.15 (ruling R10.11): a forced-back stack stays in its Location.
        if ((facts.MinimumMove && (facts.CurrentExists || movers.Any(unit => unit.MfSpent != 0 || unit.HalfMfSpent)))
            || (facts.AttemptedText is { } attempted && (attempted == facts.ToText || movers.Any(unit => unit.LocationText != facts.ToText)))
            || (facts.BypassCount is < 1 or > 2))
        {
            return new StepRecordVerdict(new RecordRefusal("UNIT-STATE-029", "A Minimum Move is a stack's only step, a forced back leaves the stack in its Location, and a Bypass follows one or two hexsides (A4.134, A12.15, A4.31)."), false);
        }

        // A2.6 (ruling R21.5): an exit leaves the map from the stack's Location; the units are Exited, not eliminated, and the move ends.
        if (facts.Exit && movers.Any(unit => unit.LocationText != facts.ToText))
        {
            return new StepRecordVerdict(new RecordRefusal("UNIT-STATE-029", "An exit leaves the map from the moving stack's own Location (A2.6)."), false);
        }

        return new StepRecordVerdict(null, false);
    }

    /// <summary>C10.3 (ruling R26.4): a pushed Gun leaves the map with its crew when the crew is among the movers and still mans it.</summary>
    public static bool PushedGunExits(bool gunActiveAndManned, bool holderAmongMovers) => gunActiveAndManned && holderAmongMovers;

    /// <summary>
    /// A mover's bookkeeping after a step (A4.5, B3.4, A4.12; ruling R10.8): MF accumulate in halves; Double Time makes it CX and records the MF Double
    /// Time added (two on its first step, one after); the Road Bonus needs every step at the road rate; the leader bonus a leader at every step, so
    /// the leaders it moves with are those it began with (every leader of the stack on its first step) that are still among the leaders.
    /// </summary>
    public static StepMoverUpdate StepMover(int mfSpent, bool halfMfSpent, int doubleTimeMf, bool offRoad, IReadOnlyList<string>? movedWith, string id,
        int halfMf, bool doubleTime, bool road, IReadOnlyList<string> leaders)
    {
        ArgumentNullException.ThrowIfNull(leaders);
        var halves = (mfSpent * 2) + (halfMfSpent ? 1 : 0) + halfMf;
        var first = mfSpent == 0 && !halfMfSpent;
        return new StepMoverUpdate(halves / 2, halves % 2 == 1, doubleTime, doubleTime ? (first ? 2 : 1) : doubleTimeMf, offRoad || !road,
            [.. (movedWith ?? (first ? leaders : [])).Where(leader => leader != id && leaders.Contains(leader, StringComparer.Ordinal))]);
    }

    /// <summary>
    /// The members whose move ends once the DEFENDER's window closes (A4.134, A12.15, A24.1; rulings R9.5, R10.9): every mover after a Minimum Move
    /// or a forced back; the squad whose SMOKE dr was a 6; else none.
    /// </summary>
    public static IReadOnlyList<string> EndingMembers(bool minimumMove, bool forcedBack, IReadOnlyList<string> movers, string? smokeUnit, int? smokeDr)
    {
        ArgumentNullException.ThrowIfNull(movers);
        return minimumMove || forcedBack ? movers : smokeUnit is { } sixed && smokeDr == 6 ? [sixed] : [];
    }

    /// <summary>A4.41 (referee, pass 9): a light mortar carried into a new Location does not fire in the AFPh; the ones newly marked moved, in the state's order.</summary>
    public static IReadOnlyList<string> MovedLightMortars(IReadOnlyList<CarriedWeaponFacts> carried, bool locationChanged)
    {
        ArgumentNullException.ThrowIfNull(carried);
        return [.. carried.Where(item => item.LightMortar && locationChanged && !item.AlreadyMoved).Select(item => item.Id)];
    }

    /// <summary>
    /// The DEFENDER's window closes (UNIT-STATE-029). Ruling R5.15: a vehicle that spent its MP left in its final hex ends its move when the DEFENDER
    /// passes; A24.1 (ruling R9.5): so does a squad whose SMOKE dr was a 6. D7.1 (ruling R11.11): a vehicle that bogs or is immobilized entering the
    /// Location it OVRs still resolves the OVR, and its move ends only after the Reaction window that follows. A4.134 (ruling R10.9): once all First
    /// Fire at a Minimum Move is done, its unbroken survivors are pinned and CX; a vehicle's Minimum Move (D2.15) is not Infantry's and leaves it in
    /// Motion only.
    /// </summary>
    public static CloseWindowVerdict VerifyCloseWindow(CloseWindowFacts facts, Func<string, bool> mayStillMove)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(mayStillMove);
        if (!facts.WindowOpen || facts.Step != facts.ClosedStep)
        {
            return new CloseWindowVerdict(new RecordRefusal("UNIT-STATE-029", $"No DEFENDER window is open on step {facts.ClosedStep}."), null, false);
        }

        string[] ending = [.. facts.EndingMembers.Where(id => facts.Members.Contains(id, StringComparer.Ordinal) && mayStillMove(id))];
        IReadOnlyList<string>? ended = facts.OverrunPending ? null
            : facts.Ending ? (facts.Members.Count > 0 ? facts.Members : facts.Movers)
            : ending.Length > 0 ? ending : null;
        return new CloseWindowVerdict(null, ended, facts.MinimumMove && !facts.Vehicle);
    }

    /// <summary>A4.134 (ruling R10.9): once all First Fire at a Minimum Move is done, each unbroken survivor among its movers is pinned and CX.</summary>
    public static bool MinimumMoveSurvivorPinned(bool active, bool broken) => active && !broken;

    /// <summary>The ATTACKER ends the move of some or all of the stack's members once the DEFENDER's window closes (A4.2, A8.11; UNIT-STATE-029).</summary>
    public static RecordRefusal? VerifyEndMovement(bool movementExists, bool windowOpen, IReadOnlyList<string> ended, IReadOnlyList<string> members, IReadOnlyList<string> movers)
    {
        ArgumentNullException.ThrowIfNull(ended);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(movers);
        return !movementExists || windowOpen || ended.Count == 0
            || ended.Distinct(StringComparer.Ordinal).Count() != ended.Count
            || ended.Any(id => !members.Contains(id, StringComparer.Ordinal) && !movers.Contains(id, StringComparer.Ordinal))
            ? new RecordRefusal("UNIT-STATE-029", "The ATTACKER ends the move of the moving stack's members once the DEFENDER's window closes (A4.2, A8.11).")
            : null;
    }

    /// <summary>D2.4: a vehicle that ends its move without stopping is in Motion; one that stopped, or was stopped by a Stun or immobilization (D5.34), is not.</summary>
    public static bool InMotionAfterMove(bool stopped, bool stillMember) => !(stopped || !stillMember);

    /// <summary>The moving stack after the ATTACKER ends some of its members: while a member may still move it goes on; when none may, its move is over (A4.2).</summary>
    public static bool MoveIsOver(int members) => members == 0;

    /// <summary>A7.55: the step-keyed fire records of a move, which concern only its own MF expenditures, go with it; the others stay.</summary>
    public static bool FireRecordStays(bool stepKeyed) => !stepKeyed;

    /// <summary>
    /// A4.2, A7.8: a member of the moving stack that is broken, pinned, or no longer active leaves the stack and ends its MPh, and the others may move
    /// on; a member Reduced to a HS is followed by the HS (A7.302). D5.34, A7.82 (unit step 25): a moving vehicle stops when its crew is Stunned or
    /// Recalled or it is immobilized, but a pin never stops it; D8.2 (ruling R11.9): a Bog, or a Bog Removal that does not free it, stops it too.
    /// <paramref name="member"/> gives a member's facts by id, null when it is not in the state.
    /// </summary>
    public static KeepMovingStackVerdict KeepMovingStack(string? phase, IReadOnlyList<string> currentMembers, IReadOnlyList<string> currentMovers, bool vehicle,
        IReadOnlyList<string>? consumed, IReadOnlyList<string>? produced, bool vehicleCheck, Func<string, StackMemberFacts?> member)
    {
        ArgumentNullException.ThrowIfNull(currentMembers);
        ArgumentNullException.ThrowIfNull(currentMovers);
        ArgumentNullException.ThrowIfNull(member);
        if (phase != "mph")
        {
            return new KeepMovingStackVerdict(true, currentMembers, currentMovers, []);
        }

        var members = currentMembers.ToList();
        var movers = currentMovers.ToList();
        if (consumed is not null && produced is not null && consumed.Any(id => members.Contains(id, StringComparer.Ordinal) || movers.Contains(id, StringComparer.Ordinal)))
        {
            if (consumed.Any(id => members.Contains(id, StringComparer.Ordinal)))
            {
                members = [.. members.Where(id => !consumed.Contains(id, StringComparer.Ordinal)), .. produced];
            }

            if (consumed.Any(id => movers.Contains(id, StringComparer.Ordinal)))
            {
                movers = [.. movers.Where(id => !consumed.Contains(id, StringComparer.Ordinal)), .. produced];
            }
        }

        var leaving = members.Where(id => member(id) is not { Active: true } unit
            || (vehicle
                ? unit.Stunned || unit.Shocked || unit.UnconfirmedKill || unit.Immobilized || unit.Abandoned
                    || (unit.Bogged && vehicleCheck)
                    || (unit.Recalled && !unit.StunRecovery)
                : unit.Broken || unit.Pinned))
            .ToArray();
        if (leaving.Length == 0 && members.Count == currentMembers.Count && movers.SequenceEqual(currentMovers))
        {
            return new KeepMovingStackVerdict(true, currentMembers, currentMovers, []);
        }

        return new KeepMovingStackVerdict(false, [.. members.Where(id => !leaving.Contains(id, StringComparer.Ordinal))], movers, leaving);
    }

    /// <summary>A unit whose move ended may not spend MF (A4.1, p. 48); none moves with an open entry attempt; a move spends no negative MF; only units spend MF (UNIT-STATE-018, UNIT-STATE-010).</summary>
    public static RecordRefusal? VerifyMove(string id, bool unit, bool movementEnded, bool openAttempt, int? mf)
    {
        if (unit && movementEnded && mf is > 0)
        {
            return new RecordRefusal("UNIT-STATE-018", $"'{id}' may not move again this phase (A4.1, p. 48).");
        }

        if (openAttempt)
        {
            return new RecordRefusal("UNIT-STATE-018", $"'{id}' has an open entry attempt, so it may not move.");
        }

        if (mf is < 0)
        {
            return new RecordRefusal("UNIT-STATE-010", "A move cannot spend negative MF.");
        }

        if (mf is not null && !unit)
        {
            return new RecordRefusal("UNIT-STATE-010", "Only units spend MF.");
        }

        return null;
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
