namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>Versioned aggregate storage contract for testing an atomic return transition.</summary>
public interface IScenarioA1ReturnStateStore
{
    ValueTask<ScenarioA1ReturnState?> ReadAsync(Guid tenantId, string gameId, string unitId,
        CancellationToken cancellationToken = default);

    ValueTask<bool> TryCommitAsync(ScenarioA1ReturnState expected,
        ScenarioA1ReturnAttempt attempt, ScenarioA1ReturnState updated,
        CancellationToken cancellationToken = default);
}

/// <summary>Process-local CAS store for isolated conformance; not a durable game-state backend.</summary>
public sealed class ScenarioA1InMemoryReturnStore : IScenarioA1ReturnStateStore
{
    private readonly object sync = new();
    private readonly Dictionary<(Guid TenantId, string GameId, string UnitId),
        ScenarioA1ReturnState> states = new();

    public ScenarioA1InMemoryReturnStore(IEnumerable<ScenarioA1ReturnState> initialStates)
    {
        ArgumentNullException.ThrowIfNull(initialStates);
        foreach (var state in initialStates)
        {
            ArgumentNullException.ThrowIfNull(state);
            if (state.TenantId == Guid.Empty || string.IsNullOrWhiteSpace(state.GameId)
                || string.IsNullOrWhiteSpace(state.UnitId) || state.Version < 0
                || !states.TryAdd((state.TenantId, state.GameId, state.UnitId), state))
                throw new ArgumentException("Initial game states must have unique valid keys and versions.",
                    nameof(initialStates));
        }
    }

    public ValueTask<ScenarioA1ReturnState?> ReadAsync(Guid tenantId, string gameId,
        string unitId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync)
        {
            states.TryGetValue((tenantId, gameId, unitId), out var state);
            return ValueTask.FromResult(state);
        }
    }

    public ValueTask<bool> TryCommitAsync(ScenarioA1ReturnState expected,
        ScenarioA1ReturnAttempt attempt, ScenarioA1ReturnState updated,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(attempt);
        ArgumentNullException.ThrowIfNull(updated);
        cancellationToken.ThrowIfCancellationRequested();
        var candidate = ScenarioA1SecondDefenderReturnTransition.Evaluate(expected, attempt);
        if (candidate.Status != ScenarioA1ReturnTransitionStatus.Applied
            || candidate.State != updated)
            return ValueTask.FromResult(false);
        var key = (expected.TenantId, expected.GameId, expected.UnitId);
        lock (sync)
        {
            if (!states.TryGetValue(key, out var current)
                || current != expected
                || expected.Version == long.MaxValue
                || updated.Version != expected.Version + 1
                || (updated.TenantId, updated.GameId, updated.UnitId) != key)
                return ValueTask.FromResult(false);
            states[key] = updated;
            return ValueTask.FromResult(true);
        }
    }
}

public sealed record ScenarioA1ReturnSimulationResult(
    ScenarioA1ReturnTransitionStatus Status, ScenarioA1ReturnState? State,
    string ReasonCode);

/// <summary>Runs the pure transition against a process-local aggregate with atomic CAS.</summary>
public sealed class ScenarioA1ReturnSimulation(IScenarioA1ReturnStateStore store)
{
    public async ValueTask<ScenarioA1ReturnSimulationResult> ApplyAsync(
        Guid tenantId, string gameId, string unitId, ScenarioA1ReturnAttempt attempt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(store);
        ArgumentNullException.ThrowIfNull(attempt);
        cancellationToken.ThrowIfCancellationRequested();
        var current = await store.ReadAsync(tenantId, gameId, unitId, cancellationToken);
        if (current is null)
            return new ScenarioA1ReturnSimulationResult(ScenarioA1ReturnTransitionStatus.Denied,
                null, "asl.a1.return.aggregate-unavailable");
        var candidate = ScenarioA1SecondDefenderReturnTransition.Evaluate(current, attempt);
        if (candidate.Status != ScenarioA1ReturnTransitionStatus.Applied)
            return new ScenarioA1ReturnSimulationResult(candidate.Status,
                candidate.State, candidate.ReasonCode);
        if (await store.TryCommitAsync(current, attempt, candidate.State, cancellationToken))
            return new ScenarioA1ReturnSimulationResult(candidate.Status,
                candidate.State, candidate.ReasonCode);
        var latest = await store.ReadAsync(tenantId, gameId, unitId, cancellationToken);
        if (latest is null)
            return new ScenarioA1ReturnSimulationResult(ScenarioA1ReturnTransitionStatus.Stale,
                null, "asl.a1.return.aggregate-changed");
        var lostRace = ScenarioA1SecondDefenderReturnTransition.Evaluate(latest, attempt);
        return new ScenarioA1ReturnSimulationResult(lostRace.Status,
            lostRace.State, lostRace.ReasonCode);
    }
}
