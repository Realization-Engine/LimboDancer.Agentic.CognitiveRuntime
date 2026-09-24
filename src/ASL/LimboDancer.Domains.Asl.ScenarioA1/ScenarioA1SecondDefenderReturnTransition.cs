using System.Security.Cryptography;
using System.Text.Json;
using LimboDancer.Abstractions.Domain;

namespace LimboDancer.Domains.Asl.ScenarioA1;

public enum ScenarioA1ReturnStage
{
    Unknown,
    PendingA1215Return,
    Returned,
}

public enum ScenarioA1ReturnHazard
{
    Unknown,
    Clear,
    ResidualFirepower,
    Ffe,
    Minefield,
    SpecialPlacement,
}

/// <summary>One versioned, hypothetical movement aggregate; not a board-state adapter.</summary>
public sealed record ScenarioA1ReturnState(
    Guid TenantId, string GameId, string UnitId, long Version,
    string ObservationVersion, string UnitLocationId, string TargetLocationId,
    string PreviousLocationId, int RemainingMf)
{
    public ScenarioA1ReturnStage Stage { get; init; }
    public ScenarioA1ReturnHazard ReturnHazard { get; init; }
    public bool IsMovementPhase { get; init; }
    public bool IsBerserk { get; init; }
    public bool IsConcealed { get; init; }
    public bool HasOtherEntryException { get; init; }
    public bool OvrEntryResolved { get; init; }
    public bool MovementEnded { get; init; }
    public int? AttemptedEntryMfRecorded { get; init; }
    public bool AttemptedEntryMfAlreadyDebited { get; init; }
    public string? MfExpenditureLocationId { get; init; }
    public string? CompletedAttemptId { get; init; }
    public string? CompletedAttemptFingerprint { get; init; }
}

public sealed record ScenarioA1ReturnAttempt(
    string AttemptId, long ExpectedVersion, DomainConclusion Conclusion);

public enum ScenarioA1ReturnTransitionStatus
{
    Applied,
    Replay,
    Stale,
    Conflict,
    Denied,
}

public sealed record ScenarioA1ReturnTransitionResult(
    ScenarioA1ReturnTransitionStatus Status, ScenarioA1ReturnState State,
    string ReasonCode);

/// <summary>Pure evaluation of the reviewed clear-return subset; does not authorize or commit.</summary>
public static class ScenarioA1SecondDefenderReturnTransition
{
    private const string CasePrefix = "A1-second-defender-consequence-";

    public static ScenarioA1ReturnTransitionResult Evaluate(
        ScenarioA1ReturnState state, ScenarioA1ReturnAttempt attempt)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(attempt.Conclusion);
        ScenarioA1ReturnTransitionResult Result(ScenarioA1ReturnTransitionStatus status,
            string code) => new(status, state, code);

        if (string.IsNullOrWhiteSpace(attempt.AttemptId) || attempt.ExpectedVersion < 0
            || string.IsNullOrWhiteSpace(state.GameId) || string.IsNullOrWhiteSpace(state.UnitId)
            || state.TenantId == Guid.Empty || state.Version < 0
            || string.IsNullOrWhiteSpace(state.ObservationVersion)
            || string.IsNullOrWhiteSpace(state.UnitLocationId)
            || string.IsNullOrWhiteSpace(state.TargetLocationId)
            || string.IsNullOrWhiteSpace(state.PreviousLocationId)
            || state.TargetLocationId == state.PreviousLocationId)
            return Result(ScenarioA1ReturnTransitionStatus.Denied, "asl.a1.return.invalid-input");

        var conclusion = attempt.Conclusion;
        var question = conclusion.Question;
        var value = conclusion.Value;
        var parameters = question.Parameters;
        var caseId = String(parameters, "caseId");
        var allowedCase = caseId == CasePrefix + "smc-revealed"
            || caseId == CasePrefix + "mmc-revealed";
        var valid = conclusion.Disposition == ConclusionDisposition.Definitive
            && question.Package == ScenarioA1SecondDefenderConsequencePackage.Identity
            && question.Kind.Value == ScenarioA1SecondDefenderConsequenceConclusionResolver.QuestionKind
            && question.TenantId == state.TenantId
            && parameters.ValueKind == JsonValueKind.Object
            && parameters.EnumerateObject().Count() == 5
            && allowedCase
            && String(parameters, "unitId") == state.UnitId
            && String(parameters, "locationId") == state.TargetLocationId
            && String(parameters, "previousLocationId") == state.PreviousLocationId
            && String(parameters, "observationVersion") == state.ObservationVersion
            && value is { ValueKind: JsonValueKind.Object }
            && value.Value.EnumerateObject().Count() == 8
            && String(value.Value, "caseId") == caseId
            && String(value.Value, "returnToLocationId") == state.PreviousLocationId
            && String(value.Value, "mfExpenditureLocationId") == state.PreviousLocationId
            && Int(value.Value, "attemptedEntryMf") == 2
            && Flag(value.Value, "additionalOvrMfResolved") == false
            && Flag(value.Value, "defensiveAttackResolved") == false
            && Flag(value.Value, "responseOrCcResolved") == false
            && Flag(value.Value, "executed") == false
            && conclusion.ApplicableRules.Count == 4
            && conclusion.Evidence.Any(item => item.Kind == EvidenceKind.CanonicalSource
                && item.ResourceId == "asl-scenario-a1.second-defender-consequence-package.json"
                && item.Version == ScenarioA1SecondDefenderConsequencePackage.ManifestSha256)
            && conclusion.Evidence.Any(item => item.Kind == EvidenceKind.CanonicalSource
                && item.ResourceId == "asl-scenario-a1.second-defender-consequence-case-matrix.json"
                && item.Version == ScenarioA1SecondDefenderConsequencePackage.MatrixSha256)
            && conclusion.Evidence.Any(item => item.Kind == EvidenceKind.Observation
                && item.ResourceId == state.TargetLocationId
                && item.Version == state.ObservationVersion);
        if (!valid)
            return Result(ScenarioA1ReturnTransitionStatus.Denied,
                "asl.a1.return.conclusion-outside-exact-contract");

        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(
            JsonSerializer.SerializeToUtf8Bytes(new
            {
                attempt.AttemptId, attempt.ExpectedVersion, state.TenantId, state.GameId,
                state.UnitId, state.TargetLocationId, state.PreviousLocationId,
                state.ObservationVersion, conclusion.ConclusionId, caseId,
                value = value!.Value.GetRawText(),
            })));
        if (state.CompletedAttemptId == attempt.AttemptId)
            return state.CompletedAttemptFingerprint == fingerprint
                ? Result(ScenarioA1ReturnTransitionStatus.Replay, "asl.a1.return.already-applied")
                : Result(ScenarioA1ReturnTransitionStatus.Conflict,
                    "asl.a1.return.duplicate-attempt-conflict");
        if (state.CompletedAttemptId is not null || state.CompletedAttemptFingerprint is not null)
            return Result(ScenarioA1ReturnTransitionStatus.Denied,
                "asl.a1.return.already-completed");
        if (state.Version != attempt.ExpectedVersion
            || state.Version == long.MaxValue)
            return Result(ScenarioA1ReturnTransitionStatus.Stale, "asl.a1.return.state-stale");
        if (state.Stage != ScenarioA1ReturnStage.PendingA1215Return
            || state.UnitLocationId != state.TargetLocationId
            || !state.IsMovementPhase || state.IsBerserk || state.IsConcealed
            || state.HasOtherEntryException || state.OvrEntryResolved || state.MovementEnded
            || state.AttemptedEntryMfRecorded != 2
            || state.MfExpenditureLocationId is not null
            || state.ReturnHazard != ScenarioA1ReturnHazard.Clear
            || state.RemainingMf < (state.AttemptedEntryMfAlreadyDebited ? 0 : 2))
            return Result(ScenarioA1ReturnTransitionStatus.Denied,
                "asl.a1.return.preconditions-unmet-or-hazard-unresolved");

        var updated = state with
        {
            Version = state.Version + 1,
            UnitLocationId = state.PreviousLocationId,
            RemainingMf = state.RemainingMf - (state.AttemptedEntryMfAlreadyDebited ? 0 : 2),
            AttemptedEntryMfAlreadyDebited = true,
            MfExpenditureLocationId = state.PreviousLocationId,
            IsConcealed = false,
            MovementEnded = true,
            Stage = ScenarioA1ReturnStage.Returned,
            CompletedAttemptId = attempt.AttemptId,
            CompletedAttemptFingerprint = fingerprint,
        };
        return new ScenarioA1ReturnTransitionResult(ScenarioA1ReturnTransitionStatus.Applied,
            updated, "asl.a1.return.clear-location-transition");
    }

    private static string? String(JsonElement value, string name) =>
        value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var property)
        && property.ValueKind == JsonValueKind.String ? property.GetString() : null;

    private static int? Int(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number
        && property.TryGetInt32(out var number) ? number : null;

    private static bool? Flag(JsonElement value, string name) =>
        value.TryGetProperty(name, out var property) && property.ValueKind is
            JsonValueKind.True or JsonValueKind.False ? property.GetBoolean() : null;
}
