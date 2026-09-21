using System.Collections.ObjectModel;
using System.Text.Json;

namespace LimboDancer.Runtime.Execution;

public sealed class ActionExecutionResult
{
    public ActionExecutionResult(
        bool succeeded,
        string code,
        JsonElement? output = null,
        IEnumerable<string>? producedResourceIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var resourceIds = (producedResourceIds ?? []).ToArray();
        if (resourceIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Produced resource identifiers cannot be empty.", nameof(producedResourceIds));
        }

        Succeeded = succeeded;
        Code = code;
        Output = output?.Clone();
        ProducedResourceIds = new ReadOnlyCollection<string>(resourceIds);
    }

    public bool Succeeded
    {
        get;
    }

    public string Code
    {
        get;
    }

    public JsonElement? Output
    {
        get;
    }

    public IReadOnlyList<string> ProducedResourceIds
    {
        get;
    }
}
