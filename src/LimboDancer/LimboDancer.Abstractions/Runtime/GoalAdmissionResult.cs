using LimboDancer.Abstractions.Execution;

namespace LimboDancer.Abstractions.Runtime;

public sealed class GoalAdmissionResult
{
    public GoalAdmissionResult(
        bool admitted,
        string reasonCode,
        RuntimePrincipal? principal = null,
        RuntimeBudget? budget = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if ((admitted && (principal is null || budget is null))
            || (!admitted && (principal is not null || budget is not null)))
        {
            throw new ArgumentException("Admitted Goals require a principal and budget; denied Goals cannot carry them.");
        }

        Admitted = admitted;
        ReasonCode = reasonCode;
        Principal = principal;
        Budget = budget;
    }

    public bool Admitted
    {
        get;
    }

    public string ReasonCode
    {
        get;
    }

    public RuntimePrincipal? Principal
    {
        get;
    }

    public RuntimeBudget? Budget
    {
        get;
    }
}
