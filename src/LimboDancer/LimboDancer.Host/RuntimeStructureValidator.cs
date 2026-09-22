using System.Collections.ObjectModel;
using LimboDancer.Runtime.Actions;
using LimboDancer.Runtime.Diagnostics;

namespace LimboDancer.Host;

public sealed class RuntimeStructureValidator
{
    private readonly IActionRegistry actionRegistry;
    private readonly IActionBindingRegistry bindingRegistry;
    private readonly IActionExecutorResolver executorResolver;
    private readonly IDiagnosticProfileValidator diagnosticProfileValidator;

    public RuntimeStructureValidator(
        IActionRegistry actionRegistry,
        IActionBindingRegistry bindingRegistry,
        IActionExecutorResolver executorResolver,
        IDiagnosticProfileValidator diagnosticProfileValidator)
    {
        this.actionRegistry = actionRegistry ?? throw new ArgumentNullException(nameof(actionRegistry));
        this.bindingRegistry = bindingRegistry ?? throw new ArgumentNullException(nameof(bindingRegistry));
        this.executorResolver = executorResolver ?? throw new ArgumentNullException(nameof(executorResolver));
        this.diagnosticProfileValidator = diagnosticProfileValidator
            ?? throw new ArgumentNullException(nameof(diagnosticProfileValidator));
    }

    public RuntimeStructureValidationResult Validate()
    {
        var failures = new List<string>();
        var descriptors = actionRegistry.List();
        foreach (var required in BuiltInActionCatalog.CreateDescriptors())
        {
            if (!actionRegistry.TryGet(required.Id, required.Version, out _))
            {
                failures.Add($"Required action '{required.Id}' version '{required.Version}' is not published.");
            }
        }

        foreach (var descriptor in descriptors)
        {
            try
            {
                diagnosticProfileValidator.EnsureRequiredChecksResolvable(descriptor.Diagnostics);
            }
            catch (InvalidOperationException exception)
            {
                failures.Add(exception.Message);
            }

            if (!executorResolver.TryResolve(descriptor.Executor, out var executor)
                || executor.ActionId != descriptor.Id)
            {
                failures.Add($"Action '{descriptor.Id}' has no matching executor for '{descriptor.Executor}'.");
            }
        }

        foreach (var required in BuiltInActionCatalog.CreateMcpBindings())
        {
            if (!bindingRegistry.TryResolve(required.Protocol, required.ExternalName, out var registered)
                || registered.ActionId != required.ActionId
                || registered.Version != required.Version)
            {
                failures.Add($"Required MCP binding '{required.ExternalName}' is missing or invalid.");
            }
        }

        foreach (var binding in bindingRegistry.List(WellKnownActions.McpProtocol))
        {
            if (!actionRegistry.TryGet(binding.ActionId, binding.Version, out _))
            {
                failures.Add($"MCP binding '{binding.ExternalName}' references an unpublished action.");
            }
        }

        return new RuntimeStructureValidationResult(failures);
    }
}

public sealed class RuntimeStructureValidationResult
{
    public RuntimeStructureValidationResult(IEnumerable<string> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        Failures = new ReadOnlyCollection<string>(failures.ToArray());
    }

    public IReadOnlyList<string> Failures
    {
        get;
    }

    public bool IsValid => Failures.Count == 0;
}
