"""Round C (the boundary review, finding 16): the crew helpers read the planner's index, so they become instance methods.

Drops `static` from the named private helpers' signatures only, and prints each line changed. LF, UTF-8 kept.
"""
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[3] / "src" / "ASL" / "LimboDancer.Domains.Asl.Play"
NAMES = {
    "GamePlanner.Fire.cs": ["VehicleEffectEvents"],
    "GamePlanner.Ordnance.cs": ["KillEvents", "TaskCheckAbandons", "UnprotectedCrewEvents", "CrewLeaves", "AreaVehicleEvents"],
    "GamePlanner.Recall.cs": ["AbandonEvents"],
    "GamePlanner.Victory.cs": ["VehicleVictoryPoints", "HasInherentCrew"],
}

for file, names in NAMES.items():
    path = ROOT / file
    text = path.read_bytes().decode("utf-8")
    for name in names:
        pattern = re.compile(r"(\n    private )static ([^\n=]*\b" + name + r"\()")
        text, count = pattern.subn(r"\1\2", text)
        if count != 1:
            raise SystemExit(f"{file}: {name} matched {count} times")
        print(f"{file}: {name}")
    path.write_bytes(text.encode("utf-8"))
