namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Victory Conditions (pass 32.j, S10; A26; passes 21 and 24, rulings R21.1 to R21.5, R24.1 to R24.6): the levels of a hex that count for Control, a
/// unit's and a vehicle's VP, the inherent crew, and the game's end at once on an immediate condition. Play reads the state, the catalog, and the map,
/// hands the facts over, and writes the events; the reads a decision makes only on some path cross as delegates.
/// </summary>
public static class ScenarioA1VictoryCalculator
{
    /// <summary>
    /// The levels of a hex's Locations that count for Control (A26.14, B23.41; ruling R24.1): ground 0 and the upper levels, rooftops and cellars left out
    /// (cellars have no other use in the game; table player, pass 24); ground level when the hex is unread.
    /// </summary>
    public static IReadOnlyList<int> HexLevels(IEnumerable<(string? Terrain, int Level)>? locations) =>
        locations is null ? [0] : [.. locations.Where(item => item.Terrain is not "Rooftop" && item.Level >= 0).Select(item => item.Level).Distinct().Order()];

    /// <summary>
    /// A unit's VP (A26.211, A26.212; rulings R21.2, R24.3): a squad or crew two, a HS one, a leader one plus one for each negative leadership modifier (read
    /// only for a leader), a Hero none, a vehicle its own count (read only for a vehicle), and others none.
    /// </summary>
    public static int VictoryPoints(string kind, Func<int?> leadership, Func<int> vehicleVictoryPoints)
    {
        ArgumentNullException.ThrowIfNull(leadership);
        ArgumentNullException.ThrowIfNull(vehicleVictoryPoints);
        return kind switch
        {
            "asl:squad" or "asl:crew" => 2,
            "asl:half-squad" => 1,
            "asl:leader" => 1 + Math.Max(0, -(leadership() ?? 0)),
            "asl:vehicle" => vehicleVictoryPoints(),
            _ => 0,
        };
    }

    /// <summary>
    /// A26.212 (ruling R24.3): one VP, one for a MA not malfunctioned (a MG MA also loses its point to Disabled, a vehicle MG's condition), one per multiple
    /// of five AF of the vehicle's single strongest AF, rounded up (a 0 AF one; an unarmored vehicle none), and the inherent crew's two (A26.211) unless it
    /// has left the vehicle, read last.
    /// </summary>
    public static int VehicleVictoryPoints(bool hasMainArmament, bool mainArmamentIsGun, bool malfunctioned, bool disabled, bool armored, IReadOnlyList<int?> armorFactors, Func<bool> inherentCrew)
    {
        ArgumentNullException.ThrowIfNull(armorFactors);
        ArgumentNullException.ThrowIfNull(inherentCrew);
        var value = 1;
        if (hasMainArmament && !malfunctioned && !(!mainArmamentIsGun && disabled))
        {
            value++;
        }

        if (armored)
        {
            var strongest = armorFactors.Max() ?? 0;
            value += strongest == 0 ? 1 : (strongest + 4) / 5;
        }

        if (inherentCrew())
        {
            value += 2;
        }

        return value;
    }

    /// <summary>A26.211, D5.1 (rulings R24.3, R24.5): an armed vehicle (a MA or any MG) has an inherent crew; an unarmed vehicle only an Inherent Driver.</summary>
    public static bool VehicleArmed(bool mainArmamentIsGun, bool mainArmament, bool antiAircraftMg, bool bowMg, bool coaxialMg) =>
        mainArmamentIsGun || mainArmament || antiAircraftMg || bowMg || coaxialMg;

    /// <summary>A26.211 (rulings R24.3, R24.5): the inherent crew is gone once the vehicle is Abandoned or its crew is a counter of its own (read last).</summary>
    public static bool HasInherentCrew(bool armed, bool abandoned, Func<bool> crewCounterExists)
    {
        ArgumentNullException.ThrowIfNull(crewCounterExists);
        return armed && !abandoned && !crewCounterExists();
    }

    /// <summary>
    /// Ruling R21.4: whether a plan's events are read for an immediate Victory: there are events, the last does not already end the game, the game started from
    /// a card, and that card has an immediate outcome (only such a card is read after every action: Gambit's Exit VP; the card is read only then).
    /// </summary>
    public static bool ImmediateVictoryRead(bool anyEvents, bool lastEndsGame, bool startedFromCard, Func<bool> cardHasImmediateOutcome)
    {
        ArgumentNullException.ThrowIfNull(cardHasImmediateOutcome);
        return anyEvents && !lastEndsGame && startedFromCard && cardHasImmediateOutcome();
    }

    /// <summary>A26.222 (referee, pass 21; UNIT-STATE-045): the game ends at once only with play started and nothing left open: no entry attempt, choice, surrender, or CC.</summary>
    public static bool NothingLeftOpen(bool setupClosed, int openAttempts, bool choicePending, int pendingSurrenders, bool anyOpenCloseCombat) =>
        setupClosed && openAttempts == 0 && !choicePending && pendingSurrenders == 0 && !anyOpenCloseCombat;
}
