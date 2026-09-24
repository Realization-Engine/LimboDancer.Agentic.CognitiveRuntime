namespace LimboDancer.Abstractions.Domain;

public enum DomainPackageResolutionOutcome
{
    Resolved,
    Unavailable,
}

public sealed class DomainPackageResolution
{
    public DomainPackageResolution(
        DomainPackageRef requested,
        DomainPackageResolutionOutcome outcome,
        DomainPackageDescriptor? package,
        string reasonCode)
    {
        ArgumentNullException.ThrowIfNull(requested);
        if (!Enum.IsDefined(outcome))
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reasonCode);
        if (outcome == DomainPackageResolutionOutcome.Resolved)
        {
            ArgumentNullException.ThrowIfNull(package);
            if (package.Identity != requested)
            {
                throw new ArgumentException("Resolved package must exactly match the request.", nameof(package));
            }
        }
        else if (package is not null)
        {
            throw new ArgumentException("An unavailable package result cannot carry a package.", nameof(package));
        }

        Requested = requested;
        Outcome = outcome;
        Package = package;
        ReasonCode = reasonCode;
    }

    public DomainPackageRef Requested
    {
        get;
    }

    public DomainPackageResolutionOutcome Outcome
    {
        get;
    }

    public DomainPackageDescriptor? Package
    {
        get;
    }

    public string ReasonCode
    {
        get;
    }
}
