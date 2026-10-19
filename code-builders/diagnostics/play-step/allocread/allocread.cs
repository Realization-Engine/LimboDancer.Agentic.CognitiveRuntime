#:package Microsoft.Diagnostics.Tracing.TraceEvent@3.1.13
// Reads a nettrace with gc-verbose: allocation ticks by type, by the nearest LimboDancer frames, over time; and the collections.
// Usage: dotnet run allocread.cs -- <file.nettrace>
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Etlx;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

var path = args[0];
var etlx = TraceLog.CreateFromEventPipeDataFile(path);
using var log = new TraceLog(etlx);
var byType = new Dictionary<string, double>();
var byPath = new Dictionary<string, double>();
var bySecond = new SortedDictionary<int, double>();
var gcs = new List<string>();
double total = 0;
foreach (var ev in log.Events)
{
    if (ev is GCAllocationTickTraceData tick)
    {
        var bytes = (double)tick.AllocationAmount64;
        total += bytes;
        var type = tick.TypeName ?? "?";
        byType[type] = byType.GetValueOrDefault(type) + bytes;
        var sec = (int)(ev.TimeStampRelativeMSec / 1000);
        bySecond[sec] = bySecond.GetValueOrDefault(sec) + bytes;
        var frames = new List<string>();
        for (var cs = ev.CallStack(); cs is not null && frames.Count < 4; cs = cs.Caller)
        {
            var name = cs.CodeAddress.FullMethodName;
            if (!string.IsNullOrEmpty(name) && name.Contains("LimboDancer"))
            {
                frames.Add(Short(name));
            }
        }

        var key = frames.Count == 0 ? "(no LimboDancer frame)" : string.Join(" < ", frames);
        byPath[key] = byPath.GetValueOrDefault(key) + bytes;
    }
    else if (ev is GCStartTraceData start)
    {
        gcs.Add($"{ev.TimeStampRelativeMSec / 1000:F1}s gen{start.Depth} {start.Reason}");
    }
}

Console.WriteLine($"Sampled allocation (ticks, about every 100 KB): {total / 1e6:F0} MB");
Console.WriteLine("\nBy second of the trace (MB):");
foreach (var (sec, b) in bySecond.Where(kv => kv.Value > 5e6))
{
    Console.WriteLine($"  {sec,3}s {b / 1e6,8:F0}");
}

Console.WriteLine("\nBy type (MB):");
foreach (var (type, b) in byType.OrderByDescending(kv => kv.Value).Take(15))
{
    Console.WriteLine($"  {b / 1e6,8:F0}  {type}");
}

Console.WriteLine("\nBy the nearest LimboDancer frames (MB):");
foreach (var (key, b) in byPath.OrderByDescending(kv => kv.Value).Take(25))
{
    Console.WriteLine($"  {b / 1e6,8:F0}  {key}");
}

Console.WriteLine($"\nCollections: {gcs.Count}");
foreach (var g in gcs.Where(g => !g.Contains("gen0")).Take(20))
{
    Console.WriteLine($"  {g}");
}

static string Short(string name)
{
    var paren = name.IndexOf('(');
    var head = paren > 0 ? name[..paren] : name;
    var parts = head.Split('.');
    return string.Join('.', parts.Skip(Math.Max(0, parts.Length - 2)));
}
