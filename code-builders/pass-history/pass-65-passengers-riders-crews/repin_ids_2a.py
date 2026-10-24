"""Pass 65, increment 2a: the eight vehicle crews in the three tests that list the catalogs' definition ids (ruling R65.3)."""
import io

A = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"
OLD = '"campaign-german-tiger",\n'
NEW = ('"campaign-german-tiger", "german-vehicle-crew", "russian-vehicle-crew", "british-vehicle-crew", "american-vehicle-crew", '
       '"finnish-vehicle-crew", "italian-vehicle-crew", "axis-minor-vehicle-crew", "french-vehicle-crew",\n')
for name in ("LimboDancer.Domains.Asl.MapStudio.Tests/UnitStudioTests.cs", "LimboDancer.Domains.Asl.Units.CounterSheets.Tests/CounterSheetCatalogBuilderTests.cs",
             "LimboDancer.Domains.Asl.Units.Tests/CatalogTests.cs"):
    with io.open(A + name, "r", encoding="utf-8", newline="") as f:
        text = f.read()
    assert "\r" not in text and text.count(OLD) == 1, name
    with io.open(A + name, "w", encoding="utf-8", newline="") as f:
        f.write(text.replace(OLD, NEW))
    print(name)
