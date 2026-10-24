"""Pass 65, increment 2a: move the pins that name catalog 1.17.0 and vocabulary asl@1.18.0 (rulings R65.3, R65.4).

Each edit states how many times its old text stands; the script refuses when a count differs.
"""
import io

R = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/"
A = R + "src/ASL/"
CREWS = ('"german-vehicle-crew", "russian-vehicle-crew", "british-vehicle-crew", "american-vehicle-crew", "finnish-vehicle-crew", '
         '"italian-vehicle-crew", "axis-minor-vehicle-crew", "french-vehicle-crew",')
EDITS = [
    (A + "LimboDancer.Domains.Asl.Play/LiveFire.cs", 'CatalogVersion = "1.17.0"', 'CatalogVersion = "1.18.0"', 1),
    (R + "code-builders/test-games/game_file.py", 'CATALOG = "asl-scenario-a1@1.17.0"', 'CATALOG = "asl-scenario-a1@1.18.0"', 1),
    (A + "tests/LimboDancer.Domains.Asl.MapStudio.Tests/UnitStudioTests.cs", "asl-scenario-a1@1.17.0", "asl-scenario-a1@1.18.0", 2),
    (A + "tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass60Tests.cs", '["catalog"] = "asl-scenario-a1@1.17.0"',
     '["catalog"] = "asl-scenario-a1@1.18.0"', 1),
    (A + "tests/LimboDancer.Domains.Asl.Units.Tests/VocabularyTests.cs", '"asl@1.18.0"', '"asl@1.19.0"', 1),
    (A + "tests/LimboDancer.Domains.Asl.Units.Tests/VocabularyTests.cs", '"asl@1.18.0+sla@0.1.0"', '"asl@1.19.0+sla@0.1.0"', 1),
    (A + "tests/LimboDancer.Domains.Asl.Units.Tests/GunTests.cs", '    [InlineData("asl@1.19.0", false)]\n',
     '    [InlineData("asl@1.19.0", true)]\n    [InlineData("asl@1.20.0", false)]\n', 1),
]
for name, old, new, count in EDITS:
    with io.open(name, "r", encoding="utf-8", newline="") as f:
        text = f.read()
    crlf = "\r\n" in text
    if crlf:
        old, new = old.replace("\n", "\r\n"), new.replace("\n", "\r\n")
    assert text.count(old) == count, (name, old, text.count(old))
    with io.open(name, "w", encoding="utf-8", newline="") as f:
        f.write(text.replace(old, new))
    print(name[len(R):], count, "crlf" if crlf else "lf")
