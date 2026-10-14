"""Pass 55: the catalog's version and its four new ids in the code and tests that state the catalog's identity."""
import io

A = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
IDS = '"axis-minor-leader-10-3", "axis-minor-hero",\n'
EDITS = [
    ("LimboDancer.Domains.Asl.Play/LiveFire.cs", 'CatalogVersion = "1.13.0"', 'CatalogVersion = "1.14.0"', 1),
    ("tests/LimboDancer.Domains.Asl.Units.Tests/StateTests.cs", "asl-scenario-a1@1.13.0+sha256:", "asl-scenario-a1@1.14.0+sha256:", 1),
    ("tests/LimboDancer.Domains.Asl.MapStudio.Tests/UnitStudioTests.cs", "asl-scenario-a1@1.13.0", "asl-scenario-a1@1.14.0", 2),
    ("tests/LimboDancer.Domains.Asl.MapStudio.Tests/UnitStudioTests.cs", IDS,
     IDS + '                "british-at-gun", "american-at-gun", "british-crew", "american-crew",\n', 1),
    ("tests/LimboDancer.Domains.Asl.Units.Tests/CatalogTests.cs", IDS,
     IDS + '                "british-at-gun", "american-at-gun", "british-crew", "american-crew",\n', 1),
    ("tests/LimboDancer.Domains.Asl.Units.CounterSheets.Tests/CounterSheetCatalogBuilderTests.cs", IDS,
     IDS + '            "british-at-gun", "american-at-gun", "british-crew", "american-crew",\n', 1),
    ("tests/LimboDancer.Domains.Asl.Play.Tests/OlderGamesTests.cs", '    [InlineData("asl-scenario-a1@1.12.0")]\n',
     '    [InlineData("asl-scenario-a1@1.12.0")]\n    [InlineData("asl-scenario-a1@1.13.0")]\n', 1),
]
for name, old, new, count in EDITS:
    with io.open(A + name, "r", encoding="utf-8", newline="") as f:
        text = f.read()
    crlf = "\r\n" in text
    if crlf:
        old, new = old.replace("\n", "\r\n"), new.replace("\n", "\r\n")
    assert text.count(old) == count, (name, old, text.count(old))
    with io.open(A + name, "w", encoding="utf-8", newline="") as f:
        f.write(text.replace(old, new))
    print(name, count, "crlf" if crlf else "lf")
