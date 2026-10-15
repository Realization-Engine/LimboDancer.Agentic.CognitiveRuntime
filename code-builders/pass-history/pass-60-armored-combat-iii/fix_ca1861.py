"""CA1861 in the pass's test files (the strict build of 2026-10-15): constant arrays passed inline become static readonly fields."""
from pathlib import Path

T = Path(__file__).resolve().parents[3] / "src" / "ASL" / "tests"
PLAY = T / "LimboDancer.Domains.Asl.Play.Tests" / "BacklogPass60Tests.cs"
PAGE = T / "LimboDancer.Domains.Asl.MapStudio.Tests" / "PlayPagePass60Tests.cs"

play = PLAY.read_text(encoding="utf-8")
page = PAGE.read_text(encoding="utf-8")
assert "\r\n" not in play and "\r\n" not in page
anchor = '    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();\n'
assert play.count(anchor) == 1
assert play.count('firers = new[] { "r1" },') == 2
assert play.count('firers = new[] { "r1", "r2" },') == 1
assert play.count('unitIds = new[] { "g1" },') == 2
page_anchor = "public sealed class PlayPagePass60Tests : IDisposable\n{\n"
assert page.count(page_anchor) == 1
assert page.count('unitIds = new[] { "g1" },') == 1

play = play.replace(anchor, anchor + '    private static readonly string[] R1 = ["r1"];\n    private static readonly string[] R1R2 = ["r1", "r2"];\n'
                    '    private static readonly string[] G1 = ["g1"];\n')
play = play.replace('firers = new[] { "r1" },', "firers = R1,").replace('firers = new[] { "r1", "r2" },', "firers = R1R2,")
play = play.replace('unitIds = new[] { "g1" },', "unitIds = G1,")
page = page.replace(page_anchor, page_anchor + '    private static readonly string[] G1 = ["g1"];\n\n')
page = page.replace('unitIds = new[] { "g1" },', "unitIds = G1,")

PLAY.write_text(play, encoding="utf-8", newline="")
PAGE.write_text(page, encoding="utf-8", newline="")
print("BacklogPass60Tests: 5 arrays -> 3 fields; PlayPagePass60Tests: 1 array -> 1 field")
