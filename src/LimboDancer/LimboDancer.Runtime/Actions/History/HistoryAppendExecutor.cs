using System.Collections.ObjectModel;
using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.History;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Actions.History;

public sealed class HistoryAppendExecutor : IActionExecutor
{
    private readonly IHistoryWriter historyWriter;
    private readonly TimeProvider timeProvider;

    public HistoryAppendExecutor(IHistoryWriter historyWriter, TimeProvider? timeProvider = null)
    {
        this.historyWriter = historyWriter ?? throw new ArgumentNullException(nameof(historyWriter));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public ActionId ActionId => WellKnownActions.HistoryAppend;

    public ExecutorBinding Binding => WellKnownActions.HistoryAppendExecutor;

    public async Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ActionExecutorJson.IsExpectedAction(action, ActionId))
        {
            return Failed("history.append.action_mismatch");
        }

        var arguments = action.Selected.Candidate.Arguments;
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            return Failed("history.append.invalid_arguments");
        }

        if (arguments.TryGetProperty("preconditions", out _)
            || arguments.TryGetProperty("effects", out _))
        {
            return Failed("history.append.caller_authority_rejected");
        }

        if (!ActionExecutorJson.TryGetRequiredString(arguments, "sessionId", out var sessionId)
            || !ActionExecutorJson.TryGetRequiredString(arguments, "sender", out var sender)
            || !ActionExecutorJson.TryGetRequiredString(arguments, "text", out var text)
            || !ActionExecutorJson.TryGetRequiredString(arguments, "subjectVertexId", out _)
            || !TryGetTimestamp(arguments, out var timestamp)
            || !TryGetMetadata(arguments, out var metadata))
        {
            return Failed("history.append.invalid_arguments");
        }

        try
        {
            var entry = await historyWriter
                .AppendAsync(
                    new TenantScope(action.TenantId),
                    new HistoryAppendRequest(sessionId, sender, text, timestamp ?? timeProvider.GetUtcNow(), metadata),
                    cancellationToken)
                .ConfigureAwait(false);
            var output = ActionExecutorJson.Serialize(new
            {
                messageId = entry.MessageId,
                sessionId = entry.SessionId,
                timestamp = entry.Timestamp,
            });
            return new ActionExecutionResult(
                succeeded: true,
                "history.append.succeeded",
                output,
                [entry.MessageId]);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed("history.append.failed");
        }
    }

    private static bool TryGetTimestamp(JsonElement arguments, out DateTimeOffset? timestamp)
    {
        timestamp = null;
        if (!arguments.TryGetProperty("timestamp", out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.String
            || !property.TryGetDateTimeOffset(out var parsed))
        {
            return false;
        }

        timestamp = parsed;
        return true;
    }

    private static bool TryGetMetadata(
        JsonElement arguments,
        out IReadOnlyDictionary<string, string?> metadata)
    {
        metadata = new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>());
        if (!arguments.TryGetProperty("metadata", out var property)
            || property.ValueKind == JsonValueKind.Null)
        {
            return true;
        }

        if (property.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var values = property.EnumerateObject().ToDictionary(
            static item => item.Name,
            static item => item.Value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => item.Value.GetString(),
                _ => item.Value.GetRawText(),
            },
            StringComparer.Ordinal);
        metadata = new ReadOnlyDictionary<string, string?>(values);
        return true;
    }

    private static ActionExecutionResult Failed(string code) => new(succeeded: false, code);
}
