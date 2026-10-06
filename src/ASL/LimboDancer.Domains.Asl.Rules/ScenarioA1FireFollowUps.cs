namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What follows a fire attack and its records (A7.7, A8.2, A9.22, A9.223, A10.62, A15.21, A23.6, C6.5; pass 32.c, slice S4, from the planner's Fire,
/// FireExtensions, and Acquisition files and the Units projector): a Fire Lane's attacks, the Encirclement placed, the effects on units and vehicles
/// as condition changes, the follow-ups of an attack, and the Acquisition that follows its units. The caller reads the state and the records and hands
/// the facts over; it writes the events in the order the verdicts give them.
/// </summary>
public static class ScenarioA1FireFollowUps
{
    /// <summary>
    /// A Fire Lane's attack on the moving stack in one of its Locations (A9.22, A9.222; ruling R12.7): Residual FP, never reduced, with no CX, leader, or
    /// hero DRM and no Cowering, taking the Hindrance of the LOS from its MG added to the Location's own.
    /// </summary>
    public static FireAttack FireLaneAttack(FireAttack facts, int laneHindranceDrm)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts with
        {
            FireLane = true,
            Los = (facts.Los ?? new FireLos(false, 0, true, false)) with
            {
                HindranceDrm = (facts.Los?.HindranceDrm ?? 0) + laneHindranceDrm
            },
        };
    }

    /// <summary>A9.22, A9.223: a Fire Lane attacks the moving stack still in its Location, once the other attacks there are made, while the lane is in place.</summary>
    public static bool FireLaneAttacks(bool laneInPlace, bool movingStackInLocation, int movers) => laneInPlace && movingStackInLocation && movers > 0;

    /// <summary>A9.223: an Original DR at least the MG's Breakdown Number (12 when the catalog gives none; two less for a captured MG, A21.11) malfunctions the MG, which ends the lane.</summary>
    public static bool FireLaneMalfunctions(int originalDr, int? breakdown, bool captured) => originalDr >= (breakdown ?? 12) - (captured ? 2 : 0);
}
