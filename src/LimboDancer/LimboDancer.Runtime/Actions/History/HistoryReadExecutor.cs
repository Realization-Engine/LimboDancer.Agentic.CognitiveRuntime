using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.History;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Actions.History;

public sealed class HistoryReadExecutor : IActionExecutor
{
    private readonly IHistoryReader historyReader;

    public HistoryReadExecutor(IHistoryReader historyReader)
    {
        this.historyReader = historyReader ?? throw new ArgumentNullException(nameof(historyReader));
    }

    public ActionId ActionId => WellKnownActions.HistoryRead;

    public ExecutorBinding Binding => WellKnownActions.HistoryReadExecutor;

    public async Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ActionExecutorJson.IsExpectedAction(action, ActionId))
        {
            return Failed("history.read.action_mismatch");
        }

        var arguments = action.Selected.Candidate.Arguments;
        if (!ActionExecutorJson.TryGetRequiredString(arguments, "sessionId", out var sessionId)
            || !ActionExecutorJson.TryGetOptionalInt32(arguments, "limit", 50, 1, 200, out var limit)
            || !TryGetBefore(arguments, out var before))
        {
            return Failed("history.read.invalid_arguments");
        }

        try
        {
            var entries = await historyReader
                .ListAsync(new TenantScope(action.TenantId), sessionId, limit, before, cancellationToken)
                .ConfigureAwait(false);
            var output = ActionExecutorJson.Serialize(new
            {
                sessionId,
                messages = entries.Select(static entry => new
                {
                    id = entry.MessageId,
                    sender = entry.Sender,
                    text = entry.Text,
                    timestamp = entry.Timestamp,
                    metadata = entry.Metadata,
                }),
            });
            return new ActionExecutionResult(succeeded: true, "history.read.succeeded", output);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed("history.read.failed");
        }
    }

    private static bool TryGetBefore(JsonElement arguments, out DateTimeOffset? before)
    {
        before = null;
        if (!arguments.TryGetProperty("before", out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String
            || !property.TryGetDateTimeOffset(out var parsed))
        {
            return false;
        }

        before = parsed;
        return true;
    }

    private static ActionExecutionResult Failed(string code) => new(succeeded: false, code);
}
