# The proof tool's repairs after pass 55's gate (the user, 2026-10-13): output written a game at a time, a game's exception as an ERROR line,
# the replay passing over cut files, and a name that matches one game exactly. Every anchor is checked before the file is written.
PATH = "E:/Archive/GitHub/dlandi/pass32-tools/proof/Program.cs"
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
crlf = "\r\n" in text
text = text.replace("\r\n", "\n")

EDITS = [
    # A name with a leading '=' matches that game alone; any other name matches the game and its cuts.
    ('''    : [.. store.List(tenant).Where(scope => named.Any(name => scope.Game == name || scope.Game.StartsWith(name + "-r", StringComparison.Ordinal)))];''',
     '''    : [.. store.List(tenant).Where(scope => named.Any(name => name.StartsWith('=') ? scope.Game == name[1..] : scope.Game == name || scope.Game.StartsWith(name + "-r", StringComparison.Ordinal)))];'''),
    ('''var timings = new Dictionary<string, (long Ms, int Count)>(StringComparer.Ordinal);

if (mode == "replay")
{
    foreach (var scope in games)
    {
        var record = store.Read(scope);
        if (record is null)
        {
            lines.Add($"{scope.Game}\\tUNREADABLE");
            continue;
        }
''',
     '''var timings = new Dictionary<string, (long Ms, int Count)>(StringComparer.Ordinal);

// After pass 55's gate: the output is written a game at a time, so a run that stops loses one game; and a game that throws is one ERROR
// line, which the gate's trial and proofs refuse.
using var writer = new StreamWriter(output, false, new UTF8Encoding(false));
var flushed = 0;
async Task Guarded(GameScope scope, Func<Task> body)
{
    var mark = lines.Count;
    try
    {
        await body();
    }
    catch (Exception exception)
    {
        lines.RemoveRange(mark, lines.Count - mark);
        lines.Add($"{scope.Game}\\tERROR\\t{exception.GetType().Name}: {exception.Message.ReplaceLineEndings(" ")}");
    }

    for (; flushed < lines.Count; flushed++)
    {
        writer.WriteLine(lines[flushed]);
    }

    writer.Flush();
}

if (mode == "replay")
{
    // A cut is the first events of its game, and the game's own replay hashes every state the cut has: the replay passes over cuts
    // (73,175 of 93,401 lines at pass 55's gate). PROOF_REPLAY_CUTS=1 replays them as before.
    var replayCuts = Environment.GetEnvironmentVariable("PROOF_REPLAY_CUTS") == "1";
    foreach (var entry in games.Where(item => replayCuts || !System.Text.RegularExpressions.Regex.IsMatch(item.Game, "-r[0-9]+$")))
    {
        await Guarded(entry, () =>
        {
            ReplayOne(entry);
            return Task.CompletedTask;
        });
    }

    void ReplayOne(GameScope scope)
    {
        var record = store.Read(scope);
        if (record is null)
        {
            lines.Add($"{scope.Game}\\tUNREADABLE");
            return;
        }
'''),
    ('''else if (mode == "sweep")
{
    foreach (var scope in games)
    {
        // A game with an event type this side cannot read (pass 55's gate: the baseline's sweep ended in a null reference after 53 minutes).
        var record = store.Read(scope);
        if (record is null)
        {
            lines.Add($"{scope.Game}\\tUNREADABLE");
            continue;
        }

        var history = planner.Replay(record.Events);
        if (history.Current is not { } state)
        {
            lines.Add($"{scope.Game}\\tNOSTATE");
            continue;
        }
''',
     '''else if (mode == "sweep")
{
    foreach (var entry in games)
    {
        await Guarded(entry, () => SweepOne(entry));
    }

    async Task SweepOne(GameScope scope)
    {
        // A game with an event type this side cannot read (pass 55's gate: the baseline's sweep ended in a null reference after 53 minutes).
        var record = store.Read(scope);
        if (record is null)
        {
            lines.Add($"{scope.Game}\\tUNREADABLE");
            return;
        }

        var history = planner.Replay(record.Events);
        if (history.Current is not { } state)
        {
            lines.Add($"{scope.Game}\\tNOSTATE");
            return;
        }
'''),
    ('''File.WriteAllLines(output, lines);
''', '''writer.Flush();
'''),
]
for old, new in EDITS:
    assert text.count(old) == 1, (old[:70], text.count(old))
    text = text.replace(old, new)
if crlf:
    text = text.replace("\n", "\r\n")
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("written")
