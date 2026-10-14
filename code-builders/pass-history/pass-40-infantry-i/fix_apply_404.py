"""Corrections to apply_404.py before its first run (exact blocks, each asserted once)."""
path = "E:/Archive/GitHub/dlandi/pass32-tools/pass40/apply_404.py"
text = open(path, encoding="utf-8", newline="").read()
edits = [
    # A pattern variable declared in an || chain is unassigned afterwards (CS0165): read the taker first.
    ('     "            if (ntc.Units.Count == 0 || ntc.Units.Any(id => Active(state, id) is not UnitInstance) || !ntc.Units.Contains(ntc.Taker, StringComparer.Ordinal)\\n"\n'
     '     "                || Active(state, ntc.Taker) is not UnitInstance taker)\\n"\n',
     '     "            var taker = Active(state, ntc.Taker) as UnitInstance;\\n"\n'
     '     "            if (ntc.Units.Count == 0 || ntc.Units.Any(id => Active(state, id) is not UnitInstance) || !ntc.Units.Contains(ntc.Taker, StringComparer.Ordinal) || taker is null)\\n"\n'),
    # The page resets the OVR box with the other one-step options, in the four places the Dash field is reset.
    ('    ("\\n        moveTerrainRate = false;\\n        moveDash = string.Empty;\\n        moveBypass = string.Empty;\\n\\n        moveTo = moveExit",\n'
     '     "\\n        moveTerrainRate = false;\\n        moveDash = string.Empty;\\n        moveBypass = string.Empty;\\n\\n        moveTo = moveExit"),\n',
     '    ("\\n        moveDash = string.Empty;\\n", "\\n        moveDash = string.Empty;\\n        moveOverrun = false;\\n", 2),\n'
     '    ("\\n                moveDash = string.Empty;\\n", "\\n                moveDash = string.Empty;\\n                moveOverrun = false;\\n", 2),\n'),
    ('    for old, new in edits:\n        assert text.count(old) == 1, (path, old[:90], text.count(old))\n        text = text.replace(old, new)\n',
     '    for edit in edits:\n        old, new = edit[0], edit[1]\n        count = edit[2] if len(edit) > 2 else 1\n'
     '        assert text.count(old) == count, (path, old[:90], text.count(old))\n        text = text.replace(old, new)\n'),
]
for old, new in edits:
    assert text.count(old) == 1, old[:80]
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)
print("ok")
