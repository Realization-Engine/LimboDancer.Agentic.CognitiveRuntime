namespace LimboDancer.Runtime.Diagnostics;

public interface IRequiredSemanticMappingResolver
{
    public bool CanResolve(string mappingId);
}
