using System.Globalization;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Domains.Asl.ScenarioA1;

namespace LimboDancer.Domains.Asl.Execution;

/// <summary>Opt-in binding for the exact clear-return subset; no host registration.</summary>
public static class ScenarioA1ReturnAction
{
    public const string Permission = "asl.game.return";
    public const string PreconditionId = "asl.return.exact-current-state";
    public static readonly ActionId Id = new("asl.scenario-a1.second-defender-return");
    public static readonly ActionVersion Version = new("1.0.0");
    public static readonly ExecutorBinding Binding = new("asl.scenario-a1.return.executor.v1");

    public static readonly ActionDescriptor Descriptor = new(
        Id, Version, "Return after second defender reveal",
        "Commit only the reviewed A12.15 clear-return transition for a versioned game aggregate.",
        Schema(), null,
        new ActionRiskProfile(ActionMutability.Write, ActionIdempotency.Idempotent,
            ActionReversibility.Irreversible, ActionBoundary.Internal,
            ActionPrivilege.Normal),
        [Permission],
        [new PreconditionDescriptor(PreconditionId, PreconditionKind.Operational,
            "asl.return.current-state-v1", JsonSerializer.SerializeToElement(new
            {
                package = ScenarioA1SecondDefenderConsequencePackage.Identity.PackageId,
            }), required: true)],
        [], IdempotencyMode.KeyRequired, Binding);

    public static string VersionKey(Guid tenantId, string gameId, string unitId) =>
        "asl.return:" + tenantId.ToString("N") + ":" + gameId + ":" + unitId;

    public static bool TryParse(JsonElement value, out ScenarioA1ReturnArguments? arguments)
    {
        arguments = null;
        if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 5
            || !Text(value, "gameId", out var gameId)
            || !Text(value, "unitId", out var unitId)
            || !Text(value, "attemptId", out var attemptId)
            || !Text(value, "conclusionId", out var conclusionId)
            || !value.TryGetProperty("expectedVersion", out var number)
            || number.ValueKind != JsonValueKind.Number
            || !number.TryGetInt64(out var version) || version < 0)
            return false;
        arguments = new ScenarioA1ReturnArguments(gameId, unitId, attemptId,
            conclusionId, version);
        return true;
    }

    private static bool Text(JsonElement value, string name, out string text)
    {
        text = string.Empty;
        if (!value.TryGetProperty(name, out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
            return false;
        text = property.GetString()!;
        return true;
    }

    private static JsonElement Schema()
    {
        using var document = JsonDocument.Parse(
            """
            {
              "type": "object",
              "additionalProperties": false,
              "required": ["gameId", "unitId", "attemptId", "expectedVersion", "conclusionId"],
              "properties": {
                "gameId": { "type": "string" },
                "unitId": { "type": "string" },
                "attemptId": { "type": "string" },
                "expectedVersion": { "type": "integer", "minimum": 0 },
                "conclusionId": { "type": "string" }
              }
            }
            """);
        return document.RootElement.Clone();
    }
}

public sealed record ScenarioA1ReturnArguments(string GameId, string UnitId,
    string AttemptId, string ConclusionId, long ExpectedVersion)
{
    public ScenarioA1ReturnAttempt WithConclusion(DomainConclusion conclusion) =>
        new(AttemptId, ExpectedVersion, conclusion);

    public string ExpectedVersionText => ExpectedVersion.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Server-owned conclusion source; caller supplies only an opaque identifier.</summary>
public interface IScenarioA1ReturnConclusionSource
{
    ValueTask<DomainConclusion?> ReadAsync(Guid tenantId, string conclusionId,
        CancellationToken cancellationToken = default);
}
