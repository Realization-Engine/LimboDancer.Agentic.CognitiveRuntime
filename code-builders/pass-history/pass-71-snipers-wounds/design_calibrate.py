"""The design's estimates calibrated (the user, 2026-10-26, through the session that started this one): the agents' raw estimates divided by 4.5,
between pass 70's ratio (33:05 estimated, 8:07 built, 4.1) and 4a's (5:45 estimated, 1:01 built, 5.7); 4a as measured. One forecast for the pass.

    python design_calibrate.py < /dev/null
"""
from pathlib import Path

DESIGN = Path(__file__).resolve().parents[3] / "src" / "ASL" / "docs" / "Passes" / "ASL Unit Backlog Pass 71 Design.md"
text = DESIGN.read_text(encoding="utf-8")

edits = [
    (
        "- **The pass is many times the plan's size by the agents' estimates.** The plan gives 1:20 of building. The agents' estimates sum to about 28:45 "
        "(section 6, F1). Pass 70 built in 8:07 against its frame's 33:05, so the build may run seven to nine hours. The pass stays one pass (ruling R71.1), "
        "built in the order of the table below; the last increment (carrying a wounded man) is the one a session's end would leave out, to pass 72.",
        "- **The forecast: about 8 hours to the gate, of which about 5:40 is building, against the plan's 1:20 of building (2:35 in all).** The estimates "
        "below are calibrated: the frame agents' raw estimates divided by 4.5, between pass 70's ratio (its frame estimated 33:05, the build took 8:07, "
        "4.1) and 4a's (estimated 5:45, built in 1:01, 5.7); 4a's row is its time by the clock. The rest of the forecast is the reviews, their fixes, the "
        "Studio check of the whole pass, and the documents, about 2:30 (pass 70's took 3:22 for ten increments; the plan's 1:15 has been short in every "
        "pass since 55). The pass stays one pass (ruling R71.1), built in the order of the table below; the last increment (carrying a wounded man) is the "
        "one a session's end would leave out, to pass 72.",
    ),
    ("| 5:45 |\n| 4b |", "| 1:01 (built) |\n| 4b |"),
    ("(`san-reduced`, E1.76's floor) and Sniper Check (A14.4). | 4:30 |", "(`san-reduced`, E1.76's floor) and Sniper Check (A14.4). | 1:00 |"),
    ("The counter given at setup and placed by A14.2. | 4:00 |", "The counter given at setup and placed by A14.2. | 0:55 |"),
    ("the first tied target; A12.14's exception. | 5:30 |", "the first tied target; A12.14's exception. | 1:15 |"),
    ("ordnance included; a Sniper's own checks chaining. | 1:35 |", "ordnance included; a Sniper's own checks chaining. | 0:20 |"),
    ("A18.2's -1 against an AFV. | 2:45 |", "A18.2's -1 against an AFV. | 0:35 |"),
    ("| Carrying a wounded man for 5 PP. | 2:30 |", "| Carrying a wounded man for 5 PP. | 0:35 |"),
    (
        "| | Overhead | The reviews, the Studio check, the documents (the plan's 1:15). | 1:15 |\n| | **Total** | build 26:35 | **27:50** |",
        "| | Overhead | The reviews, their fixes, the Studio check of the whole pass, the documents (the plan's 1:15; pass 70's took 3:22). | 2:30 |\n"
        "| | **Forecast** | build 5:41 | **about 8:10** |\n\n"
        "(The frame agents' raw estimates, before calibration: 4a 5:45, 4b 4:30, 4c 4:00, 4d 5:30, 4e 1:35, 4f 2:45, 4g 2:30, a build of 26:35. They are "
        "kept for the calibration only; pass 70's ran about four times its build.)",
    ),
    (
        "**F1. Is the pass kept whole?** The agents' estimates sum to about 28:45 of building (A 1:35 with ordnance, B 14:40, C 7:45, D 5:25, less what is "
        "moved out and what two agents counted twice) against the plan's 1:20.",
        "**F1. Is the pass kept whole?** The forecast is about 8 hours to the gate, about 5:40 of it building, against the plan's 1:20 of building "
        "(section 2: the frame agents' estimates divided by 4.5, between pass 70's ratio of 4.1 and 4a's of 5.7).",
    ),
]
for old, new in edits:
    assert text.count(old) == 1, old[:80]
for old, new in edits:
    text = text.replace(old, new)
DESIGN.write_text(text, encoding="utf-8", newline="\n")
print("ok")
