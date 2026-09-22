using LimboDancer.Abstractions.Actions;

namespace LimboDancer.Runtime.Actions;

public static class WellKnownActions
{
    public const string McpProtocol = "mcp";

    public static ActionVersion InitialVersion
    {
        get;
    } = new("1");

    public static ActionId HistoryRead
    {
        get;
    } = new("ldm:action/HistoryRead");

    public static ActionId HistoryAppend
    {
        get;
    } = new("ldm:action/HistoryAppend");

    public static ActionId GraphQuery
    {
        get;
    } = new("ldm:action/GraphQuery");

    public static ActionId MemorySearch
    {
        get;
    } = new("ldm:action/MemorySearch");

    public static ExecutorBinding HistoryReadExecutor
    {
        get;
    } = new("runtime:executor/HistoryRead");

    public static ExecutorBinding HistoryAppendExecutor
    {
        get;
    } = new("runtime:executor/HistoryAppend");

    public static ExecutorBinding GraphQueryExecutor
    {
        get;
    } = new("runtime:executor/GraphQuery");

    public static ExecutorBinding MemorySearchExecutor
    {
        get;
    } = new("runtime:executor/MemorySearch");
}
