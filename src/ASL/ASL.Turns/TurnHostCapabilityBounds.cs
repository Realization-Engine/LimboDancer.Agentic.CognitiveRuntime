using System.Collections.Immutable;

namespace ASL.Turns;

/// <summary>Audited integration boundary, never a topic availability verdict or action route.</summary>
public sealed record TurnHostCapabilityBound(string Id, TurnEvidenceKind Kind, string Scope,
    ImmutableArray<string> RequiredFacts, ImmutableArray<string> Pending, string Permission);

public static class TurnHostCapabilityBounds
{
    public static ImmutableArray<TurnHostCapabilityBound> ForProfile(string id)
    {
        var profile = TurnGuideProfiles.Get(id);
        var bounds = ImmutableArray.CreateBuilder<TurnHostCapabilityBound>();
        bounds.Add(new("status", TurnEvidenceKind.GovernedSubset, "Stun/CX lifecycle and existing cleanup; not every status rule.",
            ["Recorded origin and stable original owner", "Current supported Rules verdict", "Authorized origin/outcome and subject disclosure"],
            ["Missing origin remains unknown", "Exact topic decision routing is pending"], "asl.game.play"));
        if (profile.IntegratedDependencies.Contains("placedDc"))
        {
            bounds.Add(new("placedDc", TurnEvidenceKind.GovernedSubset, "Placed DC ownership/operability, obligation and existing detonation/cleanup path.",
                ["Recorded placement owner and MPh origin", "Current operability and disclosed attack/removal evidence", "Normal command eligibility and confirmation", "Opening review separately requires asl.game.adjudicate-window; play permission does not grant it"],
                ["Legacy missing owner remains unknown", "Exact topic route requests a governed check after a current permissioned opening review; passive guidance does not reveal private command readiness; other topic routes remain pending"], "asl.game.play"));
        }
        if (id is "existing-engine-fire-v1" or "existing-engine-atmosphere-v1" or "existing-engine-work-v1")
        {
            bounds.Add(new("fireHistory", TurnEvidenceKind.ObservedHistory, "Disclosed shots, recorded ROF, observed Spotter use and retention.",
                ["Actual phase occurrence", "Event-time ownership and visibility", "Current disclosed Spotter facts"],
                ["Not a firing permission", "Original designation, replacement and Opportunity Fire breakdown remain pending"], "read"));
        }
        if (id is "existing-engine-atmosphere-v1" or "existing-engine-work-v1")
        {
            bounds.Add(new("atmosphere", TurnEvidenceKind.ObservedHistory, "Infantry grenade attempts/cleanup and recorded Wind/Gust history.",
                ["Causal counter identity and creation", "Event visibility and historical owner", "Recorded cleanup boundary"],
                ["White SMOKE, drift, cave WP and dust execution remain pending", "A visible counter is not proof of a missing origin"], "read"));
        }
        if (id == "existing-engine-work-v1")
        {
            bounds.Add(new("work", TurnEvidenceKind.AdjudicatedEffect, "Audited work lifecycle and bound packing/replenishment/supply relocation effects.",
                ["Separate adjudication permission and explicit rule basis", "Active own participants and first-RPh start/resume", "Frozen effect roles and current same-Location facts",
                 "Known CE for replenishment effects; supported Immobile source for relocation", "Revalidated effects on resume, each event and completion", "System Ammunition DR only for replenishment"],
                ["Automatic G10/E10 eligibility remains pending", "Legacy work is read/cancel only; no progress migration", "Lifecycle-only adjudications do not imply effects",
                 "Animal transport, Accessible Locations, dump use, supply destruction and movement restrictions remain pending"], "asl.game.adjudicate-work"));
        }
        return bounds.ToImmutable();
    }
}
