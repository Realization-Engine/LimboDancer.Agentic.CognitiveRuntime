"""CA1861 in BacklogPass65Tests.cs (the strict build of 2026-10-24 reported ten, from 2b to 2d): constant arrays passed inline become static readonly
fields, as pass 60's fix_ca1861.py did. Each replacement must match the stated number of times."""
from pathlib import Path

PLAY = Path(__file__).resolve().parents[3] / "src" / "ASL" / "tests" / "LimboDancer.Domains.Asl.Play.Tests" / "BacklogPass65Tests.cs"
text = PLAY.read_text(encoding="utf-8")
assert "\r\n" not in text

anchor = '    private static readonly string[] Bd01 = ["bd01"];\n'
EDITS = [
    (anchor, anchor + '    private static readonly string[] B9Route = ["bd01:B9:0"];\n    private static readonly string[] HalfSquadOnly = ["de-h"];\n'
             '    private static readonly string[] LmgOnly = ["de-lmg"];\n', 1),
    ('firers = new[] { "ru-1", "ru-2" },', "firers = RussianSquads,", 3),
    ('firers = new[] { "de-s" },', "firers = SquadOnly,", 3),
    ('units = new[] { "de-s" },', "units = SquadOnly,", 1),
    ('route = new[] { "bd01:B9:0" },', "route = B9Route,", 1),
    ('unitIds = new[] { "de-h" },', "unitIds = HalfSquadOnly,", 1),
    ('leave = new[] { "de-lmg" },', "leave = LmgOnly,", 1),
]
for old, new, times in EDITS:
    count = text.count(old)
    if count != times:
        raise SystemExit(f"'{old}' found {count} times, expected {times}")
    text = text.replace(old, new)

PLAY.write_text(text, encoding="utf-8", newline="")
print("BacklogPass65Tests: 10 arrays -> 3 new fields and 2 of 2d and 2e")
