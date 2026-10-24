namespace LimboDancer.Domains.Asl.Rules;

/// <summary>What a counter is for the stacking limits (A5.1, A5.2, A5.5).</summary>
public enum StackUnitKind
{
    /// <summary>A counter the limits do not count: a Dummy, a SW, a Gun, a wreck.</summary>
    Other,
    Squad,
    HalfSquadOrCrew,
    Smc,
    Vehicle,
}

/// <summary>
/// One counter of a side in a Location as the stacking limits read it: its kind, whether it is a crew or HS manning a Gun (A5.5), and whether it
/// is a prisoner, which never counts (A20.51). Passengers are not in the Location's stack (A5.3) and are not handed over.
/// </summary>
public sealed record StackUnitFacts(StackUnitKind Kind, bool Manning = false, bool Captured = false);

/// <summary>
/// One side's stack in a Location, counted (A5.1, A5.2, A5.5, pp. 52 to 53; rulings R40.2 and R40.3). <paramref name="HalfSquadsAndCrews"/> leaves
/// out the crews and HS that man a Gun, which are counted in <paramref name="Manning"/>.
/// </summary>
public sealed record StackCount(int Squads, int HalfSquadsAndCrews, int Manning, int Smc, int Vehicles)
{
    /// <summary>
    /// A5.5: a squad is one; a crew or HS manning a Gun is a squad; two other HS or crews are a squad; five SMC are a HS and four or fewer nothing.
    /// </summary>
    public decimal SquadEquivalents => Squads + Manning + (HalfSquadsAndCrews / 2m) + (Math.Floor(Smc / 5m) / 2m);

    /// <summary>A5.1, A5.11: the squad-equivalents above three, rounded up (the example of A5.11: an excess of one and a half is two).</summary>
    public int PersonnelExcess => SquadEquivalents > ScenarioA1Stacking.PersonnelLimit ? (int)Math.Ceiling(SquadEquivalents - ScenarioA1Stacking.PersonnelLimit) : 0;

    /// <summary>A5.2: the vehicles above one.</summary>
    public int VehicleExcess => Math.Max(0, Vehicles - ScenarioA1Stacking.VehicleLimit);

    /// <summary>A5.12: +1 to an attack from or within the Location "for each vehicle and squad equivalent (FRU)" above the limits.</summary>
    public int AttackPenalty => PersonnelExcess + VehicleExcess;

    /// <summary>A5.131: -1 to an attack on Personnel in the Location for each squad-equivalent (FRU) above the Personnel limit; vehicles add nothing.</summary>
    public int DefensePenalty => PersonnelExcess;
}

/// <summary>
/// The stacking limits (A5, pp. 52 to 53): the one count every phase reads. Pass 40 merged here the five counts pass 32 kept apart (the advance's,
/// Close Combat's, ordnance's, setup's, and the entering stack's).
/// </summary>
public static class ScenarioA1Stacking
{
    /// <summary>
    /// A5.13, A5.131, A5.132 (p. 52; pass 40): whether the target's overstacking penalty is read for an attack. "During the MPh, this penalty
    /// applies only to moving units", so not to the DEFENDER's stack under the ATTACKER's own fire then; and it is Personnel's, never a vehicle's.
    /// </summary>
    public static bool TargetPenaltyApplies(bool mph, bool targetsOfPhasingSide, bool anyPersonnelTarget) => (!mph || targetsOfPhasingSide) && anyPersonnelTarget;

    public const int PersonnelLimit = 3;

    public const int VehicleLimit = 1;

    /// <summary>The stacking kind of a unit's kind, as the vocabulary names it. Every kind the limits count is listed; anything else is not counted.</summary>
    public static StackUnitKind KindOf(string? kind) => kind switch
    {
        "asl:squad" => StackUnitKind.Squad,
        "asl:half-squad" => StackUnitKind.HalfSquadOrCrew,
        _ when ScenarioA1Definitions.IsCrew(kind) => StackUnitKind.HalfSquadOrCrew,
        "asl:smc" or "asl:leader" or "asl:hero" => StackUnitKind.Smc,
        "asl:vehicle" => StackUnitKind.Vehicle,
        _ => StackUnitKind.Other,
    };

    /// <summary>The count of one side's counters in a Location. Prisoners never count (A20.51); a squad manning a Gun is a squad either way.</summary>
    public static StackCount Count(IEnumerable<StackUnitFacts> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        var counted = units.Where(unit => !unit.Captured).ToArray();
        return new StackCount(
            counted.Count(unit => unit.Kind == StackUnitKind.Squad),
            counted.Count(unit => unit.Kind == StackUnitKind.HalfSquadOrCrew && !unit.Manning),
            counted.Count(unit => unit.Kind == StackUnitKind.HalfSquadOrCrew && unit.Manning),
            counted.Count(unit => unit.Kind == StackUnitKind.Smc),
            counted.Count(unit => unit.Kind == StackUnitKind.Vehicle));
    }

    /// <summary>A5.1, A2.9: whether a stack is over the Personnel limit, which setup and offboard setup never are.</summary>
    public static bool Overstacked(StackCount count)
    {
        ArgumentNullException.ThrowIfNull(count);
        return count.SquadEquivalents > PersonnelLimit;
    }

    /// <summary>
    /// A7.52: a detrimental DRM of any member of a FG applies to the whole FG, so a group firing from several Locations takes the largest
    /// attack penalty among them (A5.12). Null when none is overstacked.
    /// </summary>
    public static int? GroupAttackPenalty(IEnumerable<StackCount> firingLocations)
    {
        ArgumentNullException.ThrowIfNull(firingLocations);
        var worst = firingLocations.Select(count => count.AttackPenalty).DefaultIfEmpty(0).Max();
        return worst > 0 ? worst : null;
    }
}
