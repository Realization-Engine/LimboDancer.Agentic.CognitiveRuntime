// How much does a TimeProvider.System.GetUtcNow() call cost on this machine? (Play Step Analysis, the board lookup.)
using System.Diagnostics;

var clock = TimeProvider.System;
const int N = 10_000_000;
long sink = 0;
for (var warm = 0; warm < 1_000_000; warm++) sink += clock.GetUtcNow().Ticks;

var sw = Stopwatch.StartNew();
for (var i = 0; i < N; i++) sink += clock.GetUtcNow().Ticks;
sw.Stop();
Console.WriteLine($"TimeProvider.System.GetUtcNow: {sw.Elapsed.TotalMilliseconds * 1e6 / N:F1} ns a call");

sw.Restart();
for (var i = 0; i < N; i++) sink += clock.GetTimestamp();
sw.Stop();
Console.WriteLine($"TimeProvider.System.GetTimestamp: {sw.Elapsed.TotalMilliseconds * 1e6 / N:F1} ns a call");

sw.Restart();
for (var i = 0; i < N; i++) sink += Environment.TickCount64;
sw.Stop();
Console.WriteLine($"Environment.TickCount64: {sw.Elapsed.TotalMilliseconds * 1e6 / N:F1} ns a call");
Console.WriteLine(sink == 42 ? "" : "done");
