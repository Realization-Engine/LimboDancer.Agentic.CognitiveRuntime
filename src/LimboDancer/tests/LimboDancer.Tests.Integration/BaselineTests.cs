namespace LimboDancer.Tests.Integration;

public sealed class BaselineTests
{
    [Fact]
    public void HostAssemblyLoads()
    {
        var hostAssembly = System.Reflection.Assembly.Load("LimboDancer.Host");

        Assert.Equal("LimboDancer.Host", hostAssembly.GetName().Name);
    }
}
