using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    // Routes are process-local opaque state handles, not durable evidence or authorization credentials.
    private static readonly JsonSerializerOptions TurnRouteJson = new() { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
    private static readonly byte[] TurnRouteKey = RandomNumberGenerator.GetBytes(32);

    private static string DcRouteToken(GameState state, string subject, string profile, string owner) =>
        Convert.ToHexString(HMACSHA256.HashData(TurnRouteKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            state.Stamp,
            subject,
            profile,
            version = TurnGuideProfiles.Get(profile).Version,
            owner
        }))));

    private static (TurnGuideSnapshot Guide, ImmutableArray<TurnPlacedDcAction> Actions) AddPlacedDcRoute(
        TurnGuideSnapshot guide, TurnEvaluationContext context, GameState state, Perspective perspective,
        ImmutableArray<TurnDecisionWindow> windows)
    {
        if (guide.BrowsedPhaseId != "afph" || !TurnGuideProfiles.Get(context.Profile).IntegratedDependencies.Contains("placedDc"))
        {
            return (guide, []);
        }
        var scoped = context with
        {
            WindowId = TurnPlacedDcGuidance.WindowId
        };
        // Never describe an AFPh decision as current when merely browsing another actual phase.
        if (state.Phase != "afph")
        {
            return (guide, []);
        }
        var actions = ImmutableArray.CreateBuilder<TurnPlacedDcAction>();
        var verdicts = ImmutableArray.CreateBuilder<TurnGuideVerdict>();
        foreach (var window in windows)
        {
            var charge = state.PlacedCharges.Single(item => item.Charge == window.SubjectId);
            var command = TurnCommandReadiness.NotEvaluated;
            string? token = null;
            if (!guide.Historical && !perspective.IsAdjudicator && charge.OwnerSide == perspective.Name
                && window.Timing == Rules.TurnDecisionTiming.ReadyForCommandAssessment)
            {
                // Passive side reads must not reveal private command blockers through status or route counts.
                // A route requests the existing governed check; it does not certify command readiness.
                command = TurnCommandReadiness.GovernedCheckRequired;
                token = DcRouteToken(state, charge.Charge, context.Profile, perspective.Name);
                actions.Add(new(token, charge.Charge, context.Profile, TurnPlacedDcGuidance.ActionId));
            }
            verdicts.Add(TurnPlacedDcGuidance.Describe(scoped,
                new(charge.Charge, charge.OwnerSide ?? "unknown", window.Timing, command, token)));
        }
        var described = GuideReader.Describe(new(scoped, "afph", guide.Historical), new(scoped, verdicts.ToImmutable()));
        // Only this topic receives the subject-scoped window; the overall phase context stays phase-only.
        var option = described.Options.Single(item => item.Topic.Id == TurnPlacedDcGuidance.TopicId);
        return (guide with
        {
            Options = [.. guide.Options.Select(item => item.Topic.Id == TurnPlacedDcGuidance.TopicId ? option : item)]
        }, actions.ToImmutable());
    }

    /// <summary>
    /// Trusted host only: supply the authenticated perspective, not a client-selected side. Prepare once, then pass the
    /// same arguments to GamePlay.ProposeAsync and ConfirmAsync with DetonateDc and the authenticated principal.
    /// Returns null for stale, foreign, unoffered or unsupported routes. Never draws dice or writes a journal event.
    /// </summary>
    public async Task<JsonElement?> PreparePlacedDcDecisionAsync(GameScope scope, Perspective perspective,
        TurnPlacedDcAction route, string attemptId, string? vehicle = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptId);
        if (route.ActionId != TurnPlacedDcGuidance.ActionId || perspective.IsAdjudicator
            || !TurnPlacedDcGuidance.Registrations.Any(item => item.Profile == route.Profile))
        {
            return null;
        }
        var current = await ReadTurnGuideProfileAsync(scope, perspective, "afph", route.Profile, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!current.Actions.Contains(route))
        {
            return null;
        }
        var state = Replay(store.Read(scope)!.Events).Current!;
        if (route.Token != DcRouteToken(state, route.SubjectId, route.Profile, perspective.Name))
        {
            return null;
        }
        return JsonSerializer.SerializeToElement(new
        {
            gameId = scope.Game,
            attemptId,
            expectedRevision = state.Revision,
            equipmentId = route.SubjectId,
            proposedBy = perspective.Name,
            vehicle,
            guideRoute = route.Token,
            guideProfile = route.Profile,
        }, TurnRouteJson);
    }

    private static string GuidedDcRequestHash(JsonElement arguments)
    {
        string? Value(string name) => Text(arguments, name, out var value) ? value : null;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            action = TurnPlacedDcGuidance.ActionId,
            game = Value("gameId"),
            attempt = Value("attemptId"),
            revision = arguments.GetProperty("expectedRevision").GetInt64(),
            subject = Value("equipmentId"),
            vehicle = Value("vehicle"),
            owner = Value("proposedBy"),
            profile = Value("guideProfile"),
            token = Value("guideRoute"),
        }))));
    }

    private static bool GuidedDcMatches(GameState state, JsonElement arguments, string subject)
    {
        if (!Text(arguments, "guideRoute", out var token) || !Text(arguments, "guideProfile", out var profile)
            || !TurnPlacedDcGuidance.Registrations.Any(item => item.Profile == profile)
            || ProposedBy(arguments) is not { } owner || state.PlacedCharges.SingleOrDefault(item => item.Charge == subject) is not { } charge)
        {
            return false;
        }
        return charge.OwnerSide == owner
            && Rules.TurnPlacedDcWindowRules.Assess(PlacedDcWindowFacts.Read(state, charge)) == Rules.TurnDecisionTiming.ReadyForCommandAssessment
            && token == DcRouteToken(state, subject, profile, owner);
    }
}
