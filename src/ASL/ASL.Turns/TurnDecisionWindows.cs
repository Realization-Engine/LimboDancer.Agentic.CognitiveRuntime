using System.Collections.Immutable;
using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

/// <summary>Disclosed timing only. Raw journal identities remain inside the host.</summary>
public sealed record TurnDecisionWindow(string SubjectId, TurnDecisionTiming Timing, bool Historical,
    string WindowId, TurnAfphOpening? Opening, ImmutableArray<string> SourceTiming, ImmutableArray<string> Pending);

public static class TurnDecisionWindows
{
    public static TurnDecisionWindow Describe(TurnPlacedDcWindowFacts facts, bool historical) =>
        new(facts.Charge, TurnPlacedDcWindowRules.Assess(facts), historical, "afph:asop:5.22A",
            facts.Review is { } review && TurnPlacedDcWindowRules.Matches(facts, review) ? review.Opening : null,
            ["A23.3-.4", "ASOP 5.11B", "ASOP 5.12A", "ASOP 5.13A", "ASOP 5.21A", "ASOP 5.22A"],
            ["Opening tasks are external adjudications; missing evidence is not completion.",
             "Drift, WP consequences, Gusts and glider rule integration remains pending.",
             "Timing evidence is not command permission. Current eligibility, authorization and confirmation remain required.",
             "Only the bounded Placed DC topic route is integrated. Historical evidence is read-only."]);
}
