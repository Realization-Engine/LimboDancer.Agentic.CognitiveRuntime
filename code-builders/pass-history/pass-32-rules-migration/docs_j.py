"""Pass 32.j documents: the coverage's "Lives in" column gains Rules for the sections whose inventory rows changed, and the backlog gains section 54."""
import pathlib
import re

DOCS = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs")


def rewrite(path, changes):
    text = path.read_bytes().decode("utf-8")
    assert "\r\n" not in text, path
    for old, new, count in changes:
        found = text.count(old)
        assert found == count, (path.name, old[:60], found)
        text = text.replace(old, new)
    path.write_bytes(text.encode("utf-8"))


coverage = DOCS / "ASL Rule Coverage.md"
lines = coverage.read_bytes().decode("utf-8").split("\n")
wanted = {"A2": "Play, Maps, Rules", "A3": "Units, Play, Rules", "A14": "Play, Rules", "A16": "Play, Rules (card check only)", "A26": "Play, Units, Rules", "C10": "Play, Rules"}
changed = 0
for index, line in enumerate(lines):
    match = re.match(r"\| (A2|A3|A14|A16|A26|C10) \| ", line)
    if not match:
        continue
    cells = line.split(" | ")
    if len(cells) < 5:
        continue
    section = match.group(1)
    if "Rules" in cells[4]:
        continue
    cells[4] = wanted[section]
    lines[index] = " | ".join(cells)
    changed += 1
    print(section, "->", cells[4])
coverage.write_bytes("\n".join(lines).encode("utf-8"))
print(f"coverage: {changed} rows changed")

backlog = DOCS / "ASL Unit Backlog.md"
text = backlog.read_bytes().decode("utf-8")
assert "## 54." not in text
section = """
## 54. Added by pass 32.j

Pass 32.j (2026-10-08, the design's section 24) moved S10 into Rules, the last slice of the migration. What it left in place, by decision:

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| The card records and their reading: `ScenarioCard` and its parts, `ScenarioCards.Parse`, `Read`, `Start`, `BuildingHexes`, `SameCard`, `EarlierRevisions`, `GroupId`, `Describe`, `Compass`, `DateText`, and the format checks of `Validate` (format, id, catalog, source, date) | Pass 32.j, 2026-10-08 | None (the JSON format of a card; the design's D11) | Nothing | They stay in Play. Every rule a card states (the boards, the playable area, the Turn Record Chart, a side, an area, Battlefield Integrity, the Scenario Defender, the SSRs, the Victory Conditions) is checked in `ScenarioA1SetupCalculator` over facts Play hands over. |
| The card library reads: `GamePlanner.CardOf`, `CachedCard`, `Valid`, `Gone`, and the two `play.scenario` texts (the card gone or changed) in `CardSetupIncomplete` and `PlanSetup` | Pass 32.j | Ruling R19.1 | Nothing | The texts stay in Play, since their hole reads `Gone`, a library read; Rules decides when they apply (`SetupIncompleteBar`, `CardStartBar`). |
| The Victory fold's cache: `VictoryCache`, `VictoryFold`, the cache key, and the `VictoryCaches` and `Validity` dictionaries of the planner | Pass 32.j | Ruling R24.6 | Nothing | A record kept across calls stays in Play; Rules takes the cached fold as facts and says where to store it (`VictoryFoldFacts`). |
| `SetupPlanMatch.Standing`, `Footprint`, `Seen`, and `Score` | Pass 32.j | None (a comparison of a plan's footprint with a view; `Look` moved) | Nothing | They stay in Play; `Standing` filters by the Rules list of stack counter kinds. |
| The request parsing of `PlanExit`, `PlanPlaceHidden`, and `PlanNonObConcealment` (the `play.invalid-arguments` texts), `PushPlan`, and the events they write | Pass 32.j | None | Nothing | They stay in Play, as every request's parsing and every event does. |
| The 56 members of S10 against the design's 57 | Pass 32.j | None | Nothing | The Appendix's distinct names less the WHOLE rows moved in 32.a, plus the projector's `SetupConceal`, give 56; the design's 57 could not be reconstructed, as for S9 and S2. |
"""
backlog.write_bytes((text.rstrip("\n") + "\n" + section).encode("utf-8"))
print("backlog: section 54 added")
