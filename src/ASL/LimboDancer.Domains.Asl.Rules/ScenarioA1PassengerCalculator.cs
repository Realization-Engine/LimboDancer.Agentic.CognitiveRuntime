namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of Passengers and a vehicle's entry from off board (S8; pass 26 of the Card Play and Map Studio Redesign Plan, rulings R26.1 and
/// R26.2): Passenger capacity and PP (D6.1, A5.5, C10.13), boarding (D6.4), unloading (D6.5), and the entry's MP (A2.52, D2.4). The caller reads the
/// state, the catalogs, and the map and writes the events.
/// </summary>
public static class ScenarioA1PassengerCalculator
{
    /// <summary>D6.1, C10.13: a vehicle's Passenger capacity in PP, less four PP, or eight at 100mm or more, for each Gun it tows; null when it carries none.</summary>
    public static int? PassengerCapacity(int? printed, IEnumerable<int?> towedCalibers)
    {
        ArgumentNullException.ThrowIfNull(towedCalibers);
        return printed is { } capacity ? capacity - towedCalibers.Sum(caliber => caliber >= 100 ? 8 : 4) : null;
    }

    /// <summary>D6.1: the PP a unit takes as a Passenger: a squad ten, a HS or crew five, a SMC none, plus the SW it carries; null when a SW's PP are not recorded.</summary>
    public static int? PassengerPp(bool squad, bool halfSquadOrCrew, IEnumerable<int>? carried) =>
        carried is null ? null : (squad ? 10 : halfSquadOrCrew ? 5 : 0) + carried.Sum();

    /// <summary>
    /// D6.1, A5.5, C10.13 (ruling R26.2): why a vehicle may not carry the units aboard, or null: it has no Passenger capacity, more than four SMC ride, a
    /// unit's PP are not recorded, or their PP exceed it. Each unit's PP is read in order, and only until one is not recorded.
    /// </summary>
    public static string? CapacityBar(string vehicle, int? capacity, int? printed, int smc, IEnumerable<(string Id, Func<int?> Pp)> aboard)
    {
        ArgumentNullException.ThrowIfNull(aboard);
        if (capacity is not { } held)
        {
            return $"{vehicle} carries no Passengers (D6.1)";
        }

        if (smc > 4)
        {
            return $"more than four SMC would ride {vehicle}; up to four count as zero PP (D6.1, A5.5)";
        }

        var total = 0;
        foreach (var (id, pp) in aboard)
        {
            if (pp() is not { } each)
            {
                return $"the PP of what {id} carries are not recorded (A4.4)";
            }

            total += each;
        }

        return total > held
            ? $"its Passengers would take {total} PP, and {vehicle} carries {held} PP{(held < (printed ?? 0) ? " with its towed Gun's ammunition (C10.13)" : string.Empty)} (D6.1)"
            : null;
    }

    /// <summary>A2.52, D2.1 (ruling R26.1): an entry from off board costing more than the vehicle's printed allotment, not as ALL, is barred.</summary>
    public static string? EntryAllotmentBar(string id, string at, int halfMp, bool all, int printed) =>
        !all && halfMp > printed ? $"entering {at} costs {ScenarioA1VehicleMovementCalculator.Mp(halfMp)} MP, more than {id}'s allotment" : null;

    /// <summary>
    /// A4.11 (p. 48; pass 40): a unit that mounts a conveyance has four MF that Player Turn: a SMC four in place of six, and a wounded SMC or
    /// Inexperienced Infantry four in place of three.
    /// </summary>
    public const int ConveyanceAllowance = 4;

    /// <summary>A4.5 (p. 51; pass 40): Double Time "may not be used by a unit that will mount, ride, or dismount any form of conveyance during that Player Turn".</summary>
    public static string? BoardingDoubleTimeBar(string unitId, int doubleTimeMf) =>
        doubleTimeMf > 0 ? $"play.load: {unitId} has Double Timed this MPh, and a unit that Double Times does not board (A4.5)" : null;

    /// <summary>D6.4: the half MF a unit has left after the one MF boarding costs; negative when it cannot board.</summary>
    public static int BoardingHalfMfLeft(int allotment, int mfSpent, bool halfMfSpent) => (allotment * 2) - ((mfSpent * 2) + (halfMfSpent ? 1 : 0)) - 2;

    /// <summary>D6.4 (ruling R26.2): the half MP a vehicle keeps after boarding: a quarter of its allotment for each MF left to the unit that spent the most, FRD.</summary>
    public static int LoadKeptHalfMp(int printed, int leastMf) => printed * Math.Min(leastMf, 4) / 4;

    /// <summary>D6.5: Passengers may not unload once their vehicle has spent more than three-fourths of its printed MP.</summary>
    public static bool UnloadTooLate(int spent, int printed) => spent * 4 > printed * 3;

    /// <summary>D6.5: unloading costs the vehicle a quarter of its printed MP allotment (FRU), in half MP.</summary>
    public static int UnloadHalfMp(int printed) => ((printed / 2) + 3) / 4 * 2;

    /// <summary>D6.5: each unloading unit spends one MF plus one for each quarter (FRU) of its printed MP the vehicle spent before.</summary>
    public static int UnloadMf(int spent, int printed) => 1 + (printed == 0 ? 0 : ((spent * 4) + printed - 1) / printed);

    /// <summary>
    /// D6.5 (p. 205; pass 65, ruling R65.14): "FFNAM always applies vs loading or unloading units (A4.6); FFMO applies only if unloading Passengers from
    /// an unarmored conveyance in an Open Ground hex". Whether the FFMO of an Open Ground attack applies to the (un)loaders.
    /// </summary>
    public static bool EmbarkingFfmo(bool unloading, bool unarmoredConveyance) => unloading && unarmoredConveyance;

    /// <summary>
    /// D5.43 (p. 204; pass 65, ruling R65.14): "Infantry attempting to enter a vehicle succeed only if they survive all such attacks unpinned and in Good
    /// Order". The loaders an attack on them left pinned or broken, which are back on foot in the Location; an eliminated one is gone, and an unloader
    /// "does succeed" whatever befalls it. The D5.43 EX (p. 204): a loader also fails "if ... the vehicle is destroyed", so when the same attack eliminates
    /// or burns the vehicle they board (<paramref name="conveyanceResult"/>, its Vehicle line's result), every loader it did not eliminate is back on foot.
    /// Empty for any other attack.
    /// </summary>
    public static IReadOnlyList<string> LoadFailures(FireEmbarking? embarking, IEnumerable<FireUnitEffect> effects, string? conveyanceResult = null)
    {
        ArgumentNullException.ThrowIfNull(effects);
        // A load writes no unloading flag since the boundary review's fixes, and an earlier record of this pass writes false: both are loads.
        if (embarking is not { Units: { } loaders } || embarking.Unloading == true)
        {
            return [];
        }

        if (conveyanceResult is FireVehicleEffect.Eliminated or FireVehicleEffect.BurningWreck)
        {
            var eliminated = effects.Where(effect => effect.Eliminated).Select(effect => effect.UnitId).ToHashSet(StringComparer.Ordinal);
            return [.. loaders.Where(id => !eliminated.Contains(id)).Distinct(StringComparer.Ordinal)];
        }

        return [.. effects.Where(effect => loaders.Contains(effect.UnitId, StringComparer.Ordinal) && !effect.Eliminated && (effect.Pinned || effect.Broken)).Select(effect => effect.UnitId)];
    }

    /// <summary>
    /// D6.4, A4.2 (pass 65, ruling R65.15): a unit may board as the last step of its own move, once the DEFENDER's window on its last step has closed,
    /// with every unit of its moving stack that may still move, and no unit outside that stack boards while it moves; null when it may (or no Infantry
    /// stack moves).
    /// </summary>
    public static string? EndOfMoveLoadBar(string vehicleId, bool infantryMoving, bool windowOpen, IReadOnlyList<string> members, IReadOnlyCollection<string> boarding)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(boarding);
        if (!infantryMoving)
        {
            return null;
        }

        if (windowOpen)
        {
            return "play.load: the DEFENDER may still fire at the moving stack's last MF expenditure; it boards once the DEFENDER passes (A8.1, D6.4)";
        }

        if (boarding.FirstOrDefault(id => !members.Contains(id, StringComparer.Ordinal)) is { } other)
        {
            return $"play.load: {other} is not of the moving stack, and no other unit acts until its move ends (A4.2)";
        }

        return members.FirstOrDefault(id => !boarding.Contains(id, StringComparer.Ordinal)) is { } left
            ? $"play.load: {left} moves with the stack and boards {vehicleId} with it, or ends its move first (A4.2, D6.4)"
            : null;
    }

    /// <summary>
    /// A4.42, D6.4 (pp. 50, 205; pass 65, ruling R65.15): a leader "can add his IPC to one such unit's IPC to increase its movement capability": the IPC
    /// a boarding unit gains (the MMC the leader lends his to), or loses (the leader, whose own IPC is spent), as the movement's lending has it.
    /// </summary>
    public static int BoardingIpcBonus(string unitId, string? recipient, string? leader) =>
        unitId == recipient ? 1 : unitId == leader ? -1 : 0;

    /// <summary>
    /// A4.11 (p. 48; pass 65, ruling R65.15): a unit that mounted or dismounted a conveyance this Player Turn moves on with four MF (a SMC four in place
    /// of six); null leaves its usual allotment.
    /// </summary>
    public static int? ConveyedAllowance(bool conveyed) => conveyed ? ConveyanceAllowance : null;

    /// <summary>A4.12 (p. 48; pass 65, ruling R65.15): no leader bonus for a unit that expends MF to mount, ride, or dismount a conveyance.</summary>
    public static bool ConveyedLeaderBonus(bool bonus, bool conveyed) => bonus && !conveyed;

    /// <summary>
    /// A4.5 (p. 51; pass 65, ruling R65.15): "Double Time may not be used by a unit that will mount, ride, or dismount any form of conveyance during that
    /// Player Turn": a unit that boarded or unloaded this MPh does not Double Time on.
    /// </summary>
    public static string? ConveyedDoubleTimeBar(bool doubleTime, IEnumerable<(string Id, bool Conveyed)> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);
        return doubleTime && movers.FirstOrDefault(unit => unit.Conveyed) is { Id: { } id }
            ? $"play.move-double-time: {id} rode or dismounted a vehicle this Player Turn and does not Double Time (A4.5)"
            : null;
    }

    /// <summary>
    /// D6.4 (p. 205; pass 65, ruling R65.15): "any halftrack or Carrier may retain any SW placed in it if those SW apply to that vehicle's Passenger PP
    /// capacity". Why the named SW may not be left aboard as the units holding them unload, or null: only a halftrack or Carrier keeps them, and each
    /// must be held by a unit that unloads.
    /// </summary>
    public static string? LeaveAboardBar(string vehicleId, string? vehicleClass, IEnumerable<(string Weapon, bool HeldByUnloader)> weapons)
    {
        ArgumentNullException.ThrowIfNull(weapons);
        if (weapons.FirstOrDefault(item => !item.HeldByUnloader) is { Weapon: { } stray })
        {
            return $"play.unload: {stray} is not held by a Passenger that unloads, so it is not left aboard (D6.4)";
        }

        return weapons.Any() && !KeepsSwAboard(vehicleClass)
            ? $"play.unload: only a halftrack or a Carrier keeps a SW aboard without Passengers, and {vehicleId} is neither; the SW unloads with its holder (D6.4)"
            : null;
    }

    /// <summary>D6.4 (p. 205; ruling R65.15): whether a vehicle keeps SW aboard as their holders unload: a halftrack or a Carrier.</summary>
    public static bool KeepsSwAboard(string? vehicleClass) => vehicleClass == "halftrack" || ScenarioA1CarrierRules.IsCarrier(vehicleClass);

    /// <summary>
    /// D6.5 (p. 205; pass 65, ruling R65.15): "no unit other than the conveying vehicle and any other disembarking PRC of that vehicle may move in the
    /// interim". Once the vehicle's move ends, the units it disembarked that may still move are the moving stack's members, who move or end their move
    /// before any other unit moves: active, not done moving, Good Order and unpinned (A7.821: a pinned unloader stays in its Dismount Location).
    /// </summary>
    public static IReadOnlyList<string> InterimMembers(IEnumerable<(string Id, bool Active, bool MoveEnded, bool Pinned, bool Broken)> disembarked)
    {
        ArgumentNullException.ThrowIfNull(disembarked);
        return [.. disembarked.Where(unit => unit.Active && !unit.MoveEnded && !unit.Pinned && !unit.Broken).Select(unit => unit.Id).Distinct(StringComparer.Ordinal)];
    }
}
