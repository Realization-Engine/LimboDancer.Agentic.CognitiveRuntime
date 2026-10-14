"""Append pass 55's section to the time log: docs_timelog.py <end HH:MM of the documents>"""
import io
import sys

LOG = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Time Log.md"
END = sys.argv[1]
ROWS = [
    ("One session, 2026-10-13, under the user's word of 2026-10-12 \"I want you to run without my intervention up to the Gate\". The start: the standing rules and pass 50's design read; the fetch, origin/main at 846eb5a3 with three green CI runs; the branch feature/asl-backlog-pass-55 cut from it; no Studio running. Increment 1, the frame, read-only: the 35 rows listed; the rules read on pages 125, 167, 168, 171, 176 to 179, 198, 200, and 207, and the whole book scanned for the pass's terms; one agent mapped the code (seven minutes), three answered the frame's questions in parallel (six to ten minutes each); the design's sections 1 to 6 with rulings R55.1 to R55.21",
     "06:43", "07:07", "24 min", "No estimate in the plan for a frame. Committed 587102ed with the session's prompt and the sub-agent practice in the standing rules."),
    ("Increment 2a, tasks 55.1 (first part) and 55.6, built by an agent to a brief: the Depletion Number's notation, the four definitions from the listings, the catalog at 1.14.0 with its four packages pinned again, the To Hit color; the diff read; three games played. Then the replay digest's tooling: the first trial differed in every state's line (the catalog's identity), a state dump named the two properties, a base built from main's sources, the trial clean",
     "07:07", "07:48", "41 min", "Estimate 1:25. Committed b2edd620 at 07:42."),
    ("Increment 2b, task 55.4, by an agent: fire at another level for ordnance and a vehicle's MGs, Height Advantage as Case Q, the mortar and LATW under C2.6, a cliff's refusal; five games played after their LOS was read on the board viewer",
     "07:48", "08:11", "23 min", "Estimate 1:10. Committed 71988340. The agent's own time overlapped the tooling above."),
    ("Increment 2c, the rest of task 55.1, by an agent: APDS, C8.9 for any ammunition with a number, HE by number on the Infantry Target Type, C2.21, Elite, the page's ammunition list, the Supply Chart as a test's data; seven games, two sets spoiled by the session's own script and played again under new ids",
     "08:11", "08:46", "35 min", "Estimate 1:05. Committed 069400a8."),
    ("Increment 2d, task 55.2, by an agent: Canister, with its aim table, the one IFT DR for every Location, the halvings, the page's facing and aim lists; five games",
     "08:47", "09:41", "54 min", "Estimate 2:15. Committed 8b2aff97."),
    ("Increment 2e, task 55.5, by an agent: Armor Leaders on a vehicle, hidden from the enemy's view until used, their modifier and morale, their loss; Inexperienced Crews; six games",
     "09:41", "10:24", "43 min", "Estimate 1:15. Committed 47338b59."),
    ("Increment 2f, task 55.3, by an agent: the Motion attempt and the DEFENDER's Motion firer; four games",
     "10:25", "10:55", "30 min", "Estimate 1:10. Committed 9c7abca8."),
    ("Increment 2g, task 55.7, by an agent: the Gun Duel slice on the pending-choice mechanism, with one change sent back (an unrevealed Armor Leader left out of the declaration); four games; the review briefs written meanwhile",
     "10:56", "11:57", "61 min", "Estimate 1:30. Committed 1f123a87, which ended the build at 4:50 by the clock (07:07 to 11:57) against 9:50 estimated and the plan's 3:40 for six tasks."),
    ("The five reviews in one message (six to fourteen minutes each); the findings judged against their pages and the code: 46 in all, one a BLOCKER (a hidden vehicle's Motion attempt); five fix lists written; one agent applied about thirty-five fixes and two more sent after it (a duel answer's summary, C13.8's bar); the pass's own games replayed on the fixed build, none stale",
     "11:57", "13:52", "1:55", "Committed 99456de6."),
    ("The Studio check of the whole pass at five widths in three views, a Player Turn at 320, real Enter keys on a proposal and its commit; the documents: an agent wrote the design's sections 14 and 15, the review, rulings R55.1 to R55.33 in the Passes Plan, the backlog's section 59, the inventory's 58 rows and its count tables, the coverage; the session added the Studio check, the two late fixes, eight lessons in the standing rules, the gate's pass55.conf and BOTH_SKIP, and the next session's prompt; the last replay digest",
     "13:25", END, "", "The documents' commit is the branch's head. The plan's overhead for the reviews, the Studio check, and the documents was 1:15."),
]


def minutes(text):
    h, m = text.split(":")
    return int(h) * 60 + int(m)


last = ROWS[-1]
span = minutes(END) - minutes(last[1])
ROWS[-1] = (last[0], last[1], END, "%d min" % span, last[4])
total = minutes(END) - minutes("06:43")
section = ["", "## Pass 55: Armored combat II. To the merge gate", "", "| Sub-task | Start | End | Duration | Notes |", "|---|---|---|---|---|"]
for row in ROWS:
    section.append("| " + " | ".join(row) + " |")
section.append("")
section.append("The pass by the clock on 2026-10-13: 06:43 to %s, %d:%02d in all, with no stop for the user. The replay digest over the proof store ran nine times beside the work (the base once, then after each of the seven increments and after the reviews' fixes; ten to twelve minutes each): no line of 93,266 differed in any. The gate is not run; ask before starting it." % (END, total // 60, total % 60))
text = io.open(LOG, encoding="utf-8", newline="").read()
assert "\r\n" not in text
assert "## Pass 55: Armored combat II" not in text
io.open(LOG, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + "\n".join(section) + "\n")
print("time log: pass 55 appended, total %d:%02d" % (total // 60, total % 60))
