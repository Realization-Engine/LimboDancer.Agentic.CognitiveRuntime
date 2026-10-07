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
}
