using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace LimboDancer.Host;

public sealed class RuntimeTelemetry : IDisposable
{
    public const string SourceName = "LimboDancer.Host";
    private readonly Meter meter = new(SourceName);
    private readonly Counter<long> toolCalls;
    private readonly Counter<long> toolCallFailures;

    public RuntimeTelemetry()
    {
        ActivitySource = new ActivitySource(SourceName);
        toolCalls = meter.CreateCounter<long>("limbodancer.mcp.tool_calls");
        toolCallFailures = meter.CreateCounter<long>("limbodancer.mcp.tool_call_failures");
    }

    public ActivitySource ActivitySource
    {
        get;
    }

    public void RecordToolCall(bool failed)
    {
        toolCalls.Add(1);
        if (failed)
        {
            toolCallFailures.Add(1);
        }
    }

    public void Dispose()
    {
        ActivitySource.Dispose();
        meter.Dispose();
    }
}
