using LimboDancer.Abstractions.Diagnostics;

namespace LimboDancer.Runtime.Diagnostics;

public interface IDiagnosticCheck
{
    public DiagnosticCheckId Id
    {
        get;
    }

    public string Version
    {
        get;
    }

    public DiagnosticPosition Position
    {
        get;
    }
}
