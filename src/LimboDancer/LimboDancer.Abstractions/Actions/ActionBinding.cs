namespace LimboDancer.Abstractions.Actions;

public sealed record ActionBinding
{
    public ActionBinding(
        string protocol,
        string externalName,
        ActionId actionId,
        ActionVersion? version = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protocol);
        ArgumentException.ThrowIfNullOrWhiteSpace(externalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(actionId.Value);
        if (version is { } actionVersion)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actionVersion.Value);
        }

        Protocol = protocol;
        ExternalName = externalName;
        ActionId = actionId;
        Version = version;
    }

    public string Protocol
    {
        get;
    }

    public string ExternalName
    {
        get;
    }

    public ActionId ActionId
    {
        get;
    }

    public ActionVersion? Version
    {
        get;
    }
}
