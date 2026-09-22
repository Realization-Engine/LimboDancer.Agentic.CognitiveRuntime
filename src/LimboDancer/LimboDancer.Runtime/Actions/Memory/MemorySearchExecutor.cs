using System.Text.Json;
using LimboDancer.Abstractions.Actions;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.State;
using LimboDancer.Abstractions.State.Memory;
using LimboDancer.Runtime.Execution;

namespace LimboDancer.Runtime.Actions.Memory;

public sealed class MemorySearchExecutor : IActionExecutor
{
    private readonly IMemorySearch memorySearch;

    public MemorySearchExecutor(IMemorySearch memorySearch)
    {
        this.memorySearch = memorySearch ?? throw new ArgumentNullException(nameof(memorySearch));
    }

    public ActionId ActionId => WellKnownActions.MemorySearch;

    public ExecutorBinding Binding => WellKnownActions.MemorySearchExecutor;

    public async Task<ActionExecutionResult> ExecuteAsync(
        AuthorizedAction action,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        if (!ActionExecutorJson.IsExpectedAction(action, ActionId))
        {
            return Failed("memory.search.action_mismatch");
        }

        var arguments = action.Selected.Candidate.Arguments;
        if (arguments.ValueKind != JsonValueKind.Object
            || !ActionExecutorJson.TryGetOptionalString(arguments, "queryText", out var queryText)
            || !ActionExecutorJson.TryGetOptionalString(arguments, "vectorBase64", out var vectorBase64)
            || !ActionExecutorJson.TryGetOptionalInt32(arguments, "k", 8, 1, 100, out var limit)
            || !ActionExecutorJson.TryGetOptionalString(arguments, "ontologyClass", out var ontologyClass)
            || !ActionExecutorJson.TryGetOptionalString(arguments, "uriEquals", out var uri)
            || !ActionExecutorJson.TryGetStringArray(arguments, "tagsAny", out var tags)
            || !TryDecodeVector(vectorBase64, out var vector)
            || (string.IsNullOrWhiteSpace(queryText) && vector.Count == 0))
        {
            return Failed("memory.search.invalid_arguments");
        }

        try
        {
            var tenant = new TenantScope(action.TenantId);
            var items = await memorySearch
                .SearchAsync(
                    tenant,
                    new MemorySearchQuery(queryText, limit, ontologyClass, uri, tags, vector),
                    cancellationToken)
                .ConfigureAwait(false);
            var output = ActionExecutorJson.Serialize(new
            {
                tenantId = tenant.ToString(),
                count = items.Count,
                items = items.Select(static item => new
                {
                    id = item.Id,
                    title = item.Title,
                    source = item.Source,
                    ontologyClass = item.OntologyClass,
                    uri = item.Uri,
                    tags = item.Tags,
                    score = item.Score,
                    preview = item.Content.Length > 240 ? item.Content[..240] + "…" : item.Content,
                }),
            });
            return new ActionExecutionResult(succeeded: true, "memory.search.succeeded", output);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Failed("memory.search.failed");
        }
    }

    private static bool TryDecodeVector(string? base64, out IReadOnlyList<float> vector)
    {
        vector = [];
        if (string.IsNullOrWhiteSpace(base64))
        {
            return true;
        }

        try
        {
            var bytes = Convert.FromBase64String(base64);
            if (bytes.Length == 0 || bytes.Length % sizeof(float) != 0)
            {
                return false;
            }

            var values = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
            if (values.Any(static value => !float.IsFinite(value)))
            {
                return false;
            }

            vector = Array.AsReadOnly(values);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static ActionExecutionResult Failed(string code) => new(succeeded: false, code);
}
