"""Pass 40: carry the design's rulings R40.1 to R40.25 into the Passes Plan's table of rulings, after R35.10's row.

Each row's text is taken from the design, so the plan and the design say the same thing. Usage: python docs_rulings.py [--check]
"""
import re
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
design = open(DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md", encoding="utf-8", newline="").read()

QUESTIONS = {
    1: "Are a marsh hex and dry ground ADJACENT? (with R35.9)",
    2: "How is a stack counted? (amends R26.3 and R8.10)",
    3: "What do vehicles above one in a Location cost their side? (carries out R11.6)",
    4: "Which hexes are woods-road hexes?",
    5: "Who chooses the road's rate? (amends R10.1)",
    6: "Where is the point a road-rate mover is fired on at?",
    7: "What is the other side of the road for a Dash?",
    8: "Is a Dash refused for its terrain?",
    9: "Does a Fire Group of several Locations halve its FP against a Dash?",
    10: "How long does Hazardous Movement hold? (amends R10.8)",
    11: "Which MMC takes the leader's IPC? (amends R10.8)",
    12: "How is the NTC of an Infantry OVR taken?",
    13: "What does a MMC lose by failing its own NTC?",
    14: "Where may the SMC of an Infantry OVR be sent?",
    15: "Who makes the CC attack of an Infantry OVR?",
    16: "How is Good Order read where a record states only some conditions?",
    17: "What if the units that owe the attack of A8.312 cannot fire as one group?",
    18: "Has a crew that fired its Gun any FP of its own? (amends R8.4)",
    19: "Which of the DEFENDER's acts wait behind the attack owed?",
    20: "Which step owes the attack of A8.312?",
    21: "Who Deploys and Recombines without a leader? (with R31.4)",
    22: "When is the start of the RPh for Recombining?",
    23: "Which board names a hex two boards share? (A2.2)",
    24: "When are provisional turns built? (A3.9)",
    25: "What does the US# answer for a vehicle or a Gun?",
    26: "Whose overstacking is the target's penalty, and over which units is it counted?",
}
FIXED = {
    1: "A.8 (p. 43), B16.4 (p. 130); question F3, answered yes by the user on 2026-10-11. A marsh hex and the dry ground beside it are ADJACENT, as pass 35 "
       "built it: an advance out of the marsh is possible, and ruling R35.9 reads the step in either direction. No official answer was found either way.",
}

rows = []
for number in range(1, 27):
    if number in FIXED:
        text = FIXED[number]
    else:
        match = re.search(r"^- \*\*R40\.%d(?: \([^)]*\))?:\*\* (.+)$" % number, design, re.M) or re.search(r"^\*\*Ruling R40\.%d \([^)]*\)\.\*\* (.+)$" % number, design, re.M)
        assert match, number
        text = match.group(1).strip().rstrip("\r")
        text = text[0].upper() + text[1:]
    decided = "approved by the user on 2026-10-11" if number <= 11 else "decided under the user's word of 2026-10-11 (\"Complete Pass 40 without my intervention\"), open to the user's veto"
    assert "|" not in text, number
    rows.append(f"| R40.{number} | {QUESTIONS[number]} | Pass 40; {decided}. {text} |")

if "--check" in sys.argv:
    for row in rows:
        print(row[:150])
    sys.exit(0)

path = DOCS + "ASL Unit Backlog Passes Plan.md"
plan = open(path, encoding="utf-8", newline="").read()
assert "| R40.1 |" not in plan
lines = plan.split("\n")
at = max(index for index, line in enumerate(lines) if line.startswith("| R35.10 |"))
lines[at + 1:at + 1] = rows
open(path, "w", encoding="utf-8", newline="").write("\n".join(lines))
print("ok", len(rows))
