namespace LimboDancer.Tests.Unit;

public sealed class BaselineTests
{
    [Fact]
    public void BaselineAssembliesLoad()
    {
        Assert.Equal("LimboDancer.Abstractions", typeof(LimboDancer.Abstractions.AssemblyMarker).Assembly.GetName().Name);
        Assert.Equal("LimboDancer.Runtime", typeof(LimboDancer.Runtime.AssemblyMarker).Assembly.GetName().Name);
    }
}
