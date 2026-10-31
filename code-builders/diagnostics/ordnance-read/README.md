# Ordnance fact-gathering probe

This read-only probe replays a saved game to just before its first ordnance shot,
then repeatedly calls `LiveOrdnance.FromState` at that same revision. It reports
elapsed time, process CPU time and current-thread allocated bytes for five trials.
The initial read, 100 warm-up reads, replay and output formatting are not timed.
The reflection call is included on both builds; this accommodates appended
optional parameters without maintaining two versions of the probe.

Set `ASL_GATE_WORK` to the agent's own working folder. Run `build.ps1` with
`-BaselineBin` and `-CurrentBin` pointing to the completed Studio builds. It uses
locked restore and registers its output under `ASL_GATE_WORK/ordnance-read`.
Run each output's `OrdnanceRead.dll` with these arguments:

1. Repository root.
2. The same boards root for both runs.
3. The same proof store for both runs.
4. Game id, for example `p72-hero-mortar`.
5. Iterations per trial, normally 10000.

Run builds and tests to completion before measuring. Use separate fresh processes
and the same runtime settings, fixture and revision on both sides. For stable JIT
conditions, set `DOTNET_TieredCompilation=0` for both measured processes. Retain
all trials and record noise, source commits and output folders with the result.

This measures synchronous fact gathering, including the reflection overhead. It
does not measure map rendering, command confirmation, event-file writes, whole
replay speed, retained heap size, or concurrent browser activity. It creates no
game events and changes no saved game.
