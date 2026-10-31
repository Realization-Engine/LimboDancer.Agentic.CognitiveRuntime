using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The second SMC actually firing a Gun or mortar (A21.13 p. 88, C9.2 p. 179). Relations compare the complete Location and playing side,
/// not nationality or hex alone. The caller reads these facts from the same revision as the primary crew. Unknown facts are not permission.
/// </summary>
public sealed record OrdnanceAdditionalFirer(
    OrdnanceCrew? Crew,
    bool? ActiveWithDefinition,
    bool? SameSide,
    bool? AtFirer,
    bool? Melee,
    bool? Captured,
    bool? Ti,
    bool? UsedOtherWeapon,
    bool? DirectedFire,
    bool? Spotted)
{
    /// <summary>A leader under Heroic status; a hero counter is already identified by its definition. Null means no additional Heroic status.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic
    {
        get; init;
    }

    /// <summary>A7.25: declared Opportunity Fire bars Prep Fire. Null retains the earlier participant reading.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? OpportunityFireDeclared
    {
        get; init;
    }
}

/// <summary>
/// Opt-in participation evidence. Version 1 records either a lone primary crew or its real second SMC. A null property on OrdnanceShot
/// keeps the earlier single-crew reading; a non-null record with no second firer explicitly selects the new solo reading.
/// </summary>
public sealed record OrdnanceParticipation(int Version, OrdnanceAdditionalFirer? Second)
{
    /// <summary>Capture and prior Game Turn use from the firing revision. Null retains the earlier state-verifier evidence boundary.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceParticipationUseHistory? UseHistory
    {
        get; init;
    }
}

/// <summary>Facts which cannot be recovered from a weapon's nationality or the current phase's shot count.</summary>
public sealed record OrdnanceParticipationUseHistory(bool Captured, bool FiredThisGameTurn);
