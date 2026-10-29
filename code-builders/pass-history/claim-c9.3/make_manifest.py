"""The C9.3 claim's minigame manifest (ruling RC93.10): its checks on the contract c9.3-spotter-designation. Each check is the passing case with the
facts that make it refused, or unknown, changed.

    python make_manifest.py
"""
import json
import os

UNITS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "src", "ASL", "units", "minigames")

# A German squad on the slope at [E7] spots for the German light mortar below it at [E6], in the German PFPh.
BASE = {
    "phase": "PFPh",
    "ownerPhasing": True,
    "mortar": True,
    "mortarTemporarilyImmobilized": False,
    "hasDesignation": False,
    "spotterId": "g-squad",
    "spotterActive": True,
    "spotterSameSide": True,
    "spotterPersonnel": True,
    "spotterGoodOrder": True,
    "spotterCaptured": False,
    "spotterMelee": False,
    "spotterIsMortarCrew": False,
    "spotterSquad": True,
    "spotterFired": False,
    "spotterFirstFire": False,
    "spotterFiredThisPhase": False,
    "spotterUsedSupportWeapon": False,
    "spotterSpottedThisPhase": False,
    "hexRange": 1,
    "sharesLocation": True,
    "night": False,
    "pendingDecision": False,
}

CHECKS = [
    ("A Good Order squad on the slope, adjacent to the mortar, may be designated in its owner's PFPh", {}, "may-designate"),
    ("The squad two hexes away at [E8] is out of reach", {"hexRange": 2}, "refused"),
    ("A broken squad does not spot", {"spotterGoodOrder": False}, "refused"),
    ("The mortar's own crew observes for itself and is not its Spotter", {"spotterIsMortarCrew": True, "spotterId": "g-crew"}, "refused"),
    ("An original designation waits for the owner's PFPh or DFPh, not the MPh", {"phase": "MPh"}, "refused"),
    ("A Spotter already serving a mortar in another Location may not take this one", {"sharesLocation": False}, "refused"),
    ("A squad that fired its inherent FP this phase may still spot for one mortar (C9.31's example)",
     {"spotterFired": True, "spotterFiredThisPhase": True}, "may-designate"),
    ("A squad that fired in an earlier phase may not use a SW, and so may not spot (A7.351)", {"spotterFired": True}, "refused"),
    ("A squad marked First Fire may still be designated in its owner's DFPh (ASOP 4.21D)",
     {"phase": "DFPh", "ownerPhasing": False, "spotterFirstFire": True}, "may-designate"),
    ("With the range not read, the question stays open", {"hexRange": None}, "not-evaluated"),
    ("A range that is not a whole number of hexes is not read as one", {"hexRange": 1.5}, "not-evaluated"),
]

manifest = {
    "id": "c9-spotter",
    "title": "C9.3 Spotters",
    "card": "c9-spotter",
    "rules": ["C9.3"],
    "contracts": ["c9.3-spotter-designation"],
    "example": None,
    "checks": [],
}
for name, changes, outcome in CHECKS:
    facts = {**BASE, **changes}
    manifest["checks"].append({
        "name": name,
        "position": "setup",
        "viewer": "german",
        "contract": "c9.3-spotter-designation",
        "facts": {key: value for key, value in facts.items() if value is not None},
        "expect": {"outcome": outcome},
    })

with open(os.path.join(UNITS, "c9-spotter", "c9-spotter.minigame.json"), "w", encoding="utf-8", newline="\n") as out:
    out.write(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n")
print("written", len(CHECKS), "checks")
