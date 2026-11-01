"""Focused checks for approved combined delivery records and their held paths."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "documents"))
import claim_check as c


def check_record(rules="R", wires="wire-fire", approved="2026-10-31", state="approved", branch="feature"):
    claims = {"R": {"branch": "feature"}}
    wiring = {"wire-fire": {"branch": branch, "rules": [("W-R", "R", "claimed")],
                             "files": {"LimboDancer.Domains.Asl.Play/Held.cs": False}}}
    lines = [c.COMBINED_HEADER, "|---|", f"| EX-1 | {rules} | {wires} | {approved} | {state} | pass-test.conf | fresh merge approval |", ""]
    faults = []
    return c.combined_deliveries(lines, claims, wiring, faults), faults, wiring


combined, faults, wiring = check_record()
assert not faults and combined == {("wire-fire", "R"): "EX-1"}, faults
assert c.combined_path("feature", "src/ASL/LimboDancer.Domains.Asl.Play/Held.cs", wiring, combined)
assert not c.combined_path("feature", "src/ASL/LimboDancer.Domains.Asl.Play/Other.cs", wiring, combined)
assert not c.combined_path("other", "src/ASL/LimboDancer.Domains.Asl.Play/Held.cs", wiring, combined)
for arguments in ({"rules": "missing"}, {"wires": "missing"}, {"approved": ""}, {"branch": "other"}):
    combined, faults, _ = check_record(**arguments)
    assert not combined and faults, arguments
combined, faults, _ = check_record(state="proposed")
assert not combined and not faults
print("Combined-delivery approval and held-file checks passed.")
