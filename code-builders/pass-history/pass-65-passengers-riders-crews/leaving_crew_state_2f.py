"""Increment 2f: LeavingCrew and AbandonEvents take the state (a Carrier's towed Gun decides its HS, D6.82). Each replacement must match exactly once."""
import os

PLAY = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "src", "ASL", "LimboDancer.Domains.Asl.Play")
EDITS = [
    ("GamePlanner.Fire.cs", "AbandonEvents(stuck, attemptId)", "AbandonEvents(state, stuck, attemptId)"),
    ("GamePlanner.Ordnance.cs", "AbandonEvents(stuck, attemptId)", "AbandonEvents(state, stuck, attemptId)"),
    ("GamePlanner.Ordnance.cs", "&& LeavingCrew(vehicle, attemptId) is { } survivors", "&& LeavingCrew(state, vehicle, attemptId) is { } survivors"),
    ("GamePlanner.Ordnance.cs", "if (LeavingCrew(vehicle, attemptId) is { } crew)", "if (LeavingCrew(state, vehicle, attemptId) is { } crew)"),
    ("GamePlanner.Recall.cs", "AbandonEvents(UnitInstance vehicle, string attemptId)", "AbandonEvents(GameState state, UnitInstance vehicle, string attemptId)"),
    ("GamePlanner.Recall.cs", "LeavingCrew(vehicle, attemptId, stunRecovery: true)", "LeavingCrew(state, vehicle, attemptId, stunRecovery: true)"),
    ("GamePlanner.cs", "AbandonEvents(vehicle, attemptId)", "AbandonEvents(state, vehicle, attemptId)"),
]

for name, old, new in EDITS:
    path = os.path.join(PLAY, name)
    with open(path, encoding="utf-8", newline="") as source:
        text = source.read()
    count = text.count(old)
    if count != 1:
        raise SystemExit(f"{name}: '{old}' found {count} times")
    with open(path, "w", encoding="utf-8", newline="") as target:
        target.write(text.replace(old, new))
    print(name, "ok")
