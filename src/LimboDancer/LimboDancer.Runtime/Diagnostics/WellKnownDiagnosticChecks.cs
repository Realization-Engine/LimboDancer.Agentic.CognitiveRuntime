using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public static class WellKnownDiagnosticChecks
{
    public const string InitialVersion = "1";

    public static DiagnosticCheckId TenantContextPresent
    {
        get;
    } = new("ldm:diagnostic/TenantContextPresent");

    public static DiagnosticCheckId ActionDescriptorCurrent
    {
        get;
    } = new("ldm:diagnostic/ActionDescriptorCurrent");

    public static DiagnosticCheckId ExecutorBindingResolvable
    {
        get;
    } = new("ldm:diagnostic/ExecutorBindingResolvable");

    public static DiagnosticCheckId RequiredSemanticMappingsResolvable
    {
        get;
    } = new("ldm:diagnostic/RequiredSemanticMappingsResolvable");
}
