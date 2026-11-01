using System.Collections.Immutable;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>C6.17 (pp. 173-174): per-target, per-Location and per-weapon personnel Defensive First Fire history.</summary>
public static class ScenarioA1DefensiveFirstFireHistoryCalculator
{
    private const string Unknown = "The target's Defensive First Fire history is not known (C6.17, pp. 173-174).";
    private static readonly DefensiveFirstFireVisit EmptyVisit = new(0, ImmutableDictionary<string, int>.Empty);

    /// <summary>The unchanged R8.1 scalar reading, including the vehicle MP-claimed clause.</summary>
    public static bool LegacyLimit(int shots, int spent, bool vehicle, int? mpClaimed) =>
        shots >= Math.Max(spent, 1) || (shots > 0 && vehicle && (mpClaimed ?? 0) >= spent);

    /// <summary>Null means unknown; zero explicitly preserves the approved excluded reading, one selects the supported reading.</summary>
    public static int? Reading(IReadOnlyList<DefensiveFirstFireRelationship> relationships)
    {
        ArgumentNullException.ThrowIfNull(relationships);
        if (relationships.Count == 0 || relationships.Any(x => !Enum.IsDefined(x) || x == DefensiveFirstFireRelationship.Unknown))
        {
            return null;
        }
        return relationships.Any(x => x != DefensiveFirstFireRelationship.OrdinaryPersonnel) ? 0 : 1;
    }

    /// <summary>Initialize only from a known MPh-start position/counter, never from an arbitrary mid-phase snapshot.</summary>
    public static DefensiveFirstFireHistory Start(string phaseId, string targetId, string location, int? startHalfMf,
        DefensiveFirstFireRelationship relationship)
    {
        var known = Text(phaseId) && Text(targetId) && Text(location) && startHalfMf is >= 0
            && Enum.IsDefined(relationship) && relationship != DefensiveFirstFireRelationship.Unknown;
        return new(phaseId, targetId, location, startHalfMf ?? 0, startHalfMf ?? 0, known, relationship,
            ImmutableDictionary<string, DefensiveFirstFireVisit>.Empty.Add(location, EmptyVisit));
    }

    /// <summary>Observe every accepted expenditure/Location transition. Unobserved gaps invalidate, rather than inventing MF.</summary>
    public static DefensiveFirstFireHistory Observe(DefensiveFirstFireHistory history, DefensiveFirstFireExpenditure facts)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(facts);
        if (!history.Known || facts.PhaseId != history.PhaseId || facts.TargetId != history.TargetId
            || facts.PreviousLocation != history.ActiveLocation || !Text(facts.Location)
            || facts.BeforeHalfMf != history.LastTotal || facts.AfterHalfMf is not { } after || after < history.LastTotal)
        {
            return history with
            {
                Known = false
            };
        }
        if (facts.Location == history.ActiveLocation)
        {
            return history with
            {
                LastTotal = after
            };
        }
        var finalized = FinalizeVisit(history);
        if (!finalized.Known)
        {
            return finalized;
        }
        return finalized with
        {
            ActiveLocation = facts.Location,
            ActiveBaseline = history.LastTotal,
            LastTotal = after,
            Locations = finalized.Locations.ContainsKey(facts.Location) ? finalized.Locations
                : finalized.Locations.Add(facts.Location, EmptyVisit),
        };
    }

    /// <summary>Read the active Location only. Window membership is independently established by the movement rules (A8.1/.14, p. 59).</summary>
    public static DefensiveFirstFireTarget Read(DefensiveFirstFireHistory history, string phaseId, string location,
        string weaponId, int? currentHalfMf, bool? inCurrentWindow)
    {
        ArgumentNullException.ThrowIfNull(history);
        var known = history.Known && history.Relationship == DefensiveFirstFireRelationship.OrdinaryPersonnel
            && phaseId == history.PhaseId && location == history.ActiveLocation && Text(weaponId) && currentHalfMf == history.LastTotal;
        int? spent = null;
        int? shots = null;
        if (known && history.Locations.TryGetValue(location, out var visit))
        {
            var total = (long)visit.HalfMf + history.LastTotal - history.ActiveBaseline;
            if (total is >= 0 and <= int.MaxValue)
            {
                spent = (int)total;
                shots = visit.Shots.GetValueOrDefault(weaponId);
            }
        }
        return new(phaseId, history.TargetId, location, weaponId, spent, shots, inCurrentWindow);
    }

    public static DefensiveFirstFireAllowance Allowance(DefensiveFirstFireTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (!Text(target.PhaseId) || !Text(target.TargetId) || !Text(target.Location) || !Text(target.WeaponId)
            || target.HalfMfSpent is not >= 0 || target.ShotsHere is not >= 0 || target.InCurrentWindow is null)
        {
            return new(false, null, null, Unknown);
        }
        var ceiling = Math.Max(1, target.HalfMfSpent.Value / 2);
        var remaining = Math.Max(0, ceiling - target.ShotsHere.Value);
        return new(true, ceiling, remaining, target.InCurrentWindow != true
            ? "The target is outside the current Defensive First Fire window (A8.1, p. 59)."
            : remaining == 0 ? "This weapon has used the target's MF allowance in this Location (C6.17, pp. 173-174)." : null);
    }

    /// <summary>Validate the versioned set before evaluating any ceiling. Excluded relationships are selected before these facts are built.</summary>
    public static string? FactsRefusal(DefensiveFirstFirePersonnelFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (facts.Version != 1 || facts.Targets is null || facts.Targets.Count == 0)
        {
            return Unknown;
        }
        var first = facts.Targets[0];
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in facts.Targets)
        {
            if (target is null || !Allowance(target).Evaluated || !ids.Add(target.TargetId)
                || first is null || target.PhaseId != first.PhaseId || target.Location != first.Location || target.WeaponId != first.WeaponId)
            {
                return Unknown;
            }
        }
        return null;
    }

    public static string? ShotRefusal(DefensiveFirstFirePersonnelFacts facts)
    {
        if (FactsRefusal(facts) is { } invalid)
        {
            return invalid;
        }
        foreach (var target in facts.Targets)
        {
            if (Allowance(target).Refusal is { } refusal)
            {
                return refusal;
            }
        }
        return null;
    }

    /// <summary>Bind the supplement to its actual ordnance shot. Missing facts never fall through to the scalar limit.</summary>
    public static string? MovementFactsRefusal(OrdnanceShot shot, IReadOnlyList<FireTarget>? attackedTargets)
    {
        ArgumentNullException.ThrowIfNull(shot);
        if (shot.Movement?.PersonnelHistory is not { } facts)
        {
            return null;
        }
        if (FactsRefusal(facts) is not null || shot.Phase != "MPh" || shot.FireKind != ScenarioA1FireCalculator.FirstFire
            || shot.VehicleTarget is not null || shot.Movement.SpentHere is not null || shot.Movement.ShotsHere is not null
            || shot.Movement.MpClaimed is not null || attackedTargets is not { Count: > 0 } targets)
        {
            return Unknown;
        }
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var target in facts.Targets)
        {
            if (target.Location != shot.TargetLocationId || target.WeaponId != shot.Gun?.GunId)
            {
                return Unknown;
            }
            ids.Add(target.TargetId);
        }
        return ids.SetEquals(targets.Select(x => x.UnitId!)) ? null : Unknown;
    }

    /// <summary>Fold one accepted actual shot for this target. Non-shots and un-attacked members keep the same snapshot.</summary>
    public static DefensiveFirstFireHistory RecordShot(DefensiveFirstFireHistory history, string phaseId, string location,
        string weaponId, bool? actuallyFired, IReadOnlyList<string> attackedTargets)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(attackedTargets);
        if (actuallyFired == false || !attackedTargets.Contains(history.TargetId, StringComparer.Ordinal))
        {
            return history;
        }
        if (actuallyFired is null || phaseId != history.PhaseId || location != history.ActiveLocation || !Text(weaponId)
            || attackedTargets.Count(x => x == history.TargetId) != 1 || !history.Locations.TryGetValue(location, out var visit))
        {
            return history with
            {
                Known = false
            };
        }
        var count = visit.Shots.GetValueOrDefault(weaponId);
        if (count == int.MaxValue)
        {
            return history with
            {
                Known = false
            };
        }
        return history with
        {
            Locations = history.Locations.SetItem(location, visit with
            {
                Shots = visit.Shots.SetItem(weaponId, count + 1)
            })
        };
    }

    /// <summary>A7.302/A19.13 (pp. 55, 86): a one-to-one continuing target inherits shots and MF, not fresh expenditure.</summary>
    public static DefensiveFirstFireHistory ContinueTarget(DefensiveFirstFireHistory predecessor, string successorId, string location,
        int? successorHalfMf, bool? oneToOneContinuation)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        var finalized = FinalizeVisit(predecessor);
        return finalized with
        {
            TargetId = successorId,
            ActiveBaseline = successorHalfMf ?? 0,
            LastTotal = successorHalfMf ?? 0,
            Known = finalized.Known && Text(successorId) && location == predecessor.ActiveLocation
                && successorHalfMf is >= 0 && oneToOneContinuation == true,
            Relationship = oneToOneContinuation == false ? DefensiveFirstFireRelationship.ComplexLineage : predecessor.Relationship,
        };
    }

    /// <summary>Retain exclusions for this MPh, even after a carried/created relationship ends. Unknown facts invalidate supported history.</summary>
    public static DefensiveFirstFireHistory Classify(DefensiveFirstFireHistory history, DefensiveFirstFireRelationship relationship)
    {
        ArgumentNullException.ThrowIfNull(history);
        if (!Enum.IsDefined(relationship) || relationship == DefensiveFirstFireRelationship.Unknown)
        {
            return history with
            {
                Known = false,
                Relationship = DefensiveFirstFireRelationship.Unknown
            };
        }
        return history.Relationship == DefensiveFirstFireRelationship.OrdinaryPersonnel
            ? history with
            {
                Relationship = relationship
            } : history;
    }

    /// <summary>A8.14: pinning, breaking or MovementEnded alone never retire history. Apply continuation before terminal removal.</summary>
    public static bool Retire(bool phaseEnded, bool terminallyRemoved) => phaseEnded || terminallyRemoved;

    private static DefensiveFirstFireHistory FinalizeVisit(DefensiveFirstFireHistory history)
    {
        if (!history.Known || !history.Locations.TryGetValue(history.ActiveLocation, out var visit))
        {
            return history with
            {
                Known = false
            };
        }
        var total = (long)visit.HalfMf + history.LastTotal - history.ActiveBaseline;
        return total is < 0 or > int.MaxValue ? history with
        {
            Known = false
        }
            : history with
            {
                ActiveBaseline = history.LastTotal,
                Locations = history.Locations.SetItem(history.ActiveLocation, visit with
                {
                    HalfMf = (int)total
                }),
            };
    }

    private static bool Text(string? value) => !string.IsNullOrWhiteSpace(value);
}
