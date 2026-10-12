namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The facts of a step for a Dash (A4.63, pp. 51 to 52; pass 40, task 40.3). <paramref name="PendingText"/> is the Location a Dash already declared
/// leads to, null when none; <paramref name="DeclaredText"/> the Location this step declares a Dash to, null when it declares none. The rest are
/// the step's own facts: its Location, whether it is the stack's first step with no MF spent, the options chosen with it, whether the Location
/// entered has a road at its center, and how the Location Dashed to lies to it and to the Location left.
/// </summary>
public sealed record DashStepFacts(
    string? PendingText,
    string? DeclaredText,
    string ToText,
    bool FirstStep,
    bool AnyMfSpent,
    bool Assault,
    bool MinimumMove,
    bool Bypassing,
    bool Occupy,
    bool Pushed,
    bool Entering,
    bool Berserk,
    bool TerrainRate,
    bool SpecialAction,
    bool ToHasRoad,
    bool DeclaredAtGroundLevel,
    bool DeclaredAdjacentToRoad,
    bool DeclaredIsOrigin,
    bool DeclaredAdjacentToOrigin);

/// <summary>
/// Dash (A4.63, pp. 51 to 52; pass 40, task 40.3): Infantry declares, before it moves, a move from a Location on one side of a road directly into
/// the road and directly on into a Location on the other side. While it is in the road Location the DEFENDER's non-ordnance Defensive First Fire
/// at it is Area Fire, for each firer to whom both of those Locations are not Open Ground; ordnance takes Case J and may not change its CA.
/// </summary>
public static class ScenarioA1Dash
{
    /// <summary>
    /// Why a step is barred by a Dash, declared with it or pending; null when it is not. "Once a Dash is declared, no other movement/MF-expenditure
    /// options are available": the only step is into the Location Dashed to, at its cost of entry. A Dash is declared "prior to moving", by a unit
    /// that has spent no MF, as a step into a road Location; the Location Dashed to is "on the other side of the road" (ruling R40.7: adjacent to the
    /// road Location and neither the Location left nor adjacent to it).
    /// </summary>
    public static string? StepBar(DashStepFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var options = facts.Assault || facts.MinimumMove || facts.Bypassing || facts.Occupy || facts.Pushed || facts.TerrainRate || facts.SpecialAction;
        if (facts.PendingText is { } pending)
        {
            return facts.DeclaredText is not null || options || facts.ToText != pending
                ? $"play.move-dash: the stack Dashes to {pending}; once a Dash is declared its only step is into that Location, at its cost of entry (A4.63)"
                : null;
        }

        if (facts.DeclaredText is not { } declared)
        {
            return null;
        }

        if (!facts.FirstStep || facts.AnyMfSpent || options || facts.Entering || facts.Berserk)
        {
            return "play.move-dash: a Dash is declared before the unit moves, with its first step and no MF spent, not from off board, by a berserk unit, or with Assault Movement, a Minimum Move, Bypass, a pushed Gun, or the other terrain's cost (A4.63)";
        }

        if (!facts.ToHasRoad)
        {
            return $"play.move-dash: a Dash is through a road Location, and {facts.ToText} has no road (A4.63)";
        }

        return !facts.DeclaredAtGroundLevel || !facts.DeclaredAdjacentToRoad || facts.DeclaredIsOrigin || facts.DeclaredAdjacentToOrigin
            ? $"play.move-dash: {declared} is not on the other side of the road: a Dash goes directly into the road Location and directly on into a ground-level Location beside it that is neither the Location left nor beside it (A4.63)"
            : null;
    }

    /// <summary>The summary's clause for a step that declares a Dash.</summary>
    public static string DeclaredText(string dashText) =>
        $", Dashing to {dashText}: the DEFENDER's small arms fire at it in the road Location is Area Fire where both sides of the road are not Open Ground to the firer, and its move ends in {dashText} (A4.63)";

    /// <summary>
    /// A4.63: a Dashing unit "must end its MPh in the non-Open Ground Location it Dashed to", unless it "becomes berserk, pinned, broken or wounded in
    /// the road Location, or otherwise unable to enter". Null when the move may end where it stands.
    /// </summary>
    public static string? EndBar(string? pendingText, bool benefitsHold) => pendingText is { } pending && benefitsHold
        ? $"play.end-move-dash: the stack Dashes to {pending} and ends its MPh there, not in the road Location (A4.63)"
        : null;

    /// <summary>A4.63: the Dash's benefits cease when a Dashing unit is pinned, broken, or berserk in the road Location.</summary>
    public static bool BenefitsHold(IEnumerable<(bool Broken, bool Pinned, bool Berserk)> movers)
    {
        ArgumentNullException.ThrowIfNull(movers);
        return movers.All(unit => !unit.Broken && !unit.Pinned && !unit.Berserk);
    }

    /// <summary>
    /// A4.63 and its example (p. 51): whether an attack on the unit in the road Location is against a Dash. "It is possible that a declared Dash
    /// might not qualify as such vs one or more DEFENDING units": it qualifies against a firing Location to which neither the Location left nor the
    /// Location Dashed to is Open Ground, a Location out of that firer's LOS being not Open Ground. A7.52: a penalty of any member of a Fire Group
    /// is the group's, so one qualifying Location makes the group's fire Area Fire.
    /// </summary>
    public static bool QualifiesAgainst(IEnumerable<(bool OriginOpenGround, bool DestinationOpenGround)> firingLocations)
    {
        ArgumentNullException.ThrowIfNull(firingLocations);
        return firingLocations.Any(location => !location.OriginOpenGround && !location.DestinationOpenGround);
    }

    /// <summary>
    /// Whether a Location is Open Ground to a firer, as Interdiction reads it (A10.531; ruling R35.1): Open Ground terrain, a clear LOS, no Hindrance
    /// along it, and no TEM the unit there could claim against that firer.
    /// </summary>
    public static bool OpenGroundTo(bool openGroundTerrain, bool losClear, int losHindrance, RoutCoverFacts cover)
    {
        ArgumentNullException.ThrowIfNull(cover);
        return openGroundTerrain && losClear && (cover.MapHindrance ?? losHindrance) + cover.OtherHindrance == 0 && !cover.HexsideTem && !cover.HeightAdvantage && !cover.InHexCover;
    }
}
