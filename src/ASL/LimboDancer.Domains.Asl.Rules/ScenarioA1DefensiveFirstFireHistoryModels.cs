using System.Collections.Immutable;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The supported C6.17 reading (pp. 173-174). Unknown is never a request to use the legacy reading.</summary>
public enum DefensiveFirstFireRelationship
{
    Unknown,
    OrdinaryPersonnel,
    Carried,
    AdditionalCreatedUnit,
    ComplexLineage,
}

/// <summary>A full Location's finalized expenditure and this phase's shots, keyed by exact weapon identity.</summary>
public sealed record DefensiveFirstFireVisit(int HalfMf, ImmutableDictionary<string, int> Shots);

/// <summary>
/// Immutable history for one target in one MPh instance (turn plus phasing side). Location strings include board, hex and level.
/// The active visit's unfinalized expenditure is LastTotal minus ActiveBaseline. No unit state or map crosses the Rules boundary.
/// </summary>
public sealed record DefensiveFirstFireHistory(string PhaseId, string TargetId, string ActiveLocation,
    int ActiveBaseline, int LastTotal, bool Known, DefensiveFirstFireRelationship Relationship,
    ImmutableDictionary<string, DefensiveFirstFireVisit> Locations);

/// <summary>Accepted expenditure facts. Before/after totals are cumulative half-MF, not the moving stack's scalar allowance.</summary>
public sealed record DefensiveFirstFireExpenditure(string PhaseId, string TargetId, string PreviousLocation, string Location,
    int? BeforeHalfMf, int? AfterHalfMf);

/// <summary>Facts for one target, tied to the weapon, complete Location and MPh being checked.</summary>
public sealed record DefensiveFirstFireTarget(string PhaseId, string TargetId, string Location, string WeaponId,
    int? HalfMfSpent, int? ShotsHere, bool? InCurrentWindow);

/// <summary>Nullable recorded supplement: absence is the legacy reading, version 1 uses each target's own history.</summary>
public sealed record DefensiveFirstFirePersonnelFacts(int Version, IReadOnlyList<DefensiveFirstFireTarget> Targets);

/// <summary>Known allowance or an explicit unavailable answer. A refusal never grants a shot.</summary>
public sealed record DefensiveFirstFireAllowance(bool Evaluated, int? Ceiling, int? Remaining, string? Refusal);
