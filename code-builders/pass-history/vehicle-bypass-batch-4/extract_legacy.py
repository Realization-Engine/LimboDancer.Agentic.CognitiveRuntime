"""Move only the existing Bypass decisions behind compatibility calls, preserving evaluation order."""
from pathlib import Path

root = Path(__file__).resolve().parents[3]
folder = root / 'src/ASL/LimboDancer.Domains.Asl.Rules'
path = folder / 'ScenarioA1VehicleMovementCalculator.cs'
text = path.read_text(encoding='utf-8')
target = folder / 'ScenarioA1BypassVcaCalculator.cs'
helpers = []

def replace(old, new):
    global text
    assert text.count(old) == 1, old[:80]
    text = text.replace(old, new)

old = '''return entries().Any(item => item.HalfMp is { } cost && !item.All && cost <= left)
            || (!(straddling && turnedAtCafp()) && turnHalfMp() + 2 <= left);'''
replace(old, 'return ScenarioA1BypassVcaCalculator.LegacyMayMoveOn(left, entries, straddling, turnedAtCafp, turnHalfMp);')
helpers.append('''    internal static bool LegacyMayMoveOn(int left, Func<IEnumerable<(int? HalfMp, bool All)>> entries,
        bool straddling, Func<bool> turnedAtCafp, Func<int> turnHalfMp) =>
        ''' + old[:-1] + ';')
helpers[-1] = helpers[-1].replace('=>\n        return ', '=>\n        ')

old = '''return straddling && turnedAtCafp() ? $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)" : null;'''
replace(old, 'return ScenarioA1BypassVcaCalculator.LegacyTurnRefusal(id, straddling, turnedAtCafp);')
helpers.append('    internal static string? LegacyTurnRefusal(string id, bool straddling, Func<bool> turnedAtCafp) =>\n        ' + old.removeprefix('return '))

old = '''return straddling && turnedAtCafp() && mayMoveOn()
            ? $"play.move-vehicle-bypass: {id} changed its VCA at its CAFP and must now move on in Bypass (D2.33)"
            : null;'''
replace(old, 'return ScenarioA1BypassVcaCalculator.LegacyStopRefusal(id, straddling, turnedAtCafp, mayMoveOn);')
helpers.append('    internal static string? LegacyStopRefusal(string id, bool straddling, Func<bool> turnedAtCafp, Func<bool> mayMoveOn) =>\n        ' + old.removeprefix('return '))

old = '''if (!halted && straddling && turnedAtCafp() && mayMoveOn())
        {
            return $"play.end-move: {id} changed its VCA at its CAFP and must move on in Bypass before its move ends (D2.33)";
        }'''
replace(old, '''if (ScenarioA1BypassVcaCalculator.LegacyEndMoveRefusal(id, halted, straddling, turnedAtCafp, mayMoveOn) is { } bypassBar)
        {
            return bypassBar;
        }''')
helpers.append('''    internal static string? LegacyEndMoveRefusal(string id, bool halted, bool straddling, Func<bool> turnedAtCafp, Func<bool> mayMoveOn) =>
        !halted && straddling && turnedAtCafp() && mayMoveOn()
            ? $"play.end-move: {id} changed its VCA at its CAFP and must move on in Bypass before its move ends (D2.33)" : null;''')

start = text.index('        if (lane is { } straddling)', text.index('public static IReadOnlyList<VehicleStepOption> MoveOptions'))
end = text.index('\n        foreach (var to in reader.VcaHexes', start)
block = text[start:end]
body = block[block.index('            if (turnedAtCafp())'):block.rfind('        }')]
body = '\n'.join(line[4:] if line.startswith('    ') else line for line in body.splitlines())
replace(block, '''        if (lane is { } straddling)
        {
            return ScenarioA1BypassVcaCalculator.LegacyMoveOptions(reader, here, straddling, facing, reverse, turnedAtCafp);
        }
''')
helpers.append('''    internal static IReadOnlyList<VehicleStepOption> LegacyMoveOptions(IVehicleStepFactReader reader, int here, int straddling,
        int facing, bool reverse, Func<bool> turnedAtCafp)
    {
        var options = new List<VehicleStepOption>();
        void Outright(int to, int from) => options.Add(new VehicleStepOption(to, from, null));
        void Lane(int one, int two)
        {
            foreach (var (obstacle, other) in new[] { (one, two), (two, one) })
            {
                if (reader.Bypassable(obstacle)) { options.Add(new VehicleStepOption(obstacle, here, other)); }
            }
        }
''' + body + '\n    }')
target_text = target.read_text(encoding='utf-8')
target_text = target_text.rstrip()[:-1] + '\n    // Compatibility readings only. New callers use TurnAtCafp, Continuation and EndMovement.\n' + '\n\n'.join(helpers) + '\n}\n'
target.write_text(target_text, encoding='utf-8', newline='\n')
text = text.replace('/// The steps a moving vehicle may try now', '/// Legacy Bypass decisions delegate without changing answers; new callers use ScenarioA1BypassVcaCalculator.\n    /// The steps a moving vehicle may try now')
text = text.replace('    public static string? TurretStepBar(', '    /// <remarks>Legacy Bypass refusal retained. New Bypass turret components are in ScenarioA1BypassTurretCalculator; movement wiring remains separate.</remarks>\n    public static string? TurretStepBar(')
path.write_text(text, encoding='utf-8', newline='\n')
path = folder / 'ScenarioA1MotionAttempts.cs'
text = path.read_text(encoding='utf-8').replace('    public static string? ChangeBar(', '    /// <remarks>Legacy Bypass refusal retained. ScenarioA1BypassTurretCalculator supplies local turret components, not Motion-attempt integration.</remarks>\n    public static string? ChangeBar(')
path.write_text(text, encoding='utf-8', newline='\n')
