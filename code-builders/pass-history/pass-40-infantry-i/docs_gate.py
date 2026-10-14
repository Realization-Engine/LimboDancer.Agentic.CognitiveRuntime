"""Pass 40 documents: the gate's row in the time log, and the pass's lessons in the standing rules file. Usage: python docs_gate.py <start> <end>"""
import sys

DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
start, end = sys.argv[1], sys.argv[2]
minutes = ((int(end[:2]) * 60 + int(end[3:])) - (int(start[:2]) * 60 + int(start[3:]))) % (24 * 60)

row = ("| The reviews' fixes and the documents committed (c661dcc7). The gate, under the user's word, with main unchanged at 3c660afc. Side by side: the baseline of main at 3c660afc from "
       "its own sources (the store brought up to date, 380 to 411 games; the text list; the replay digest 619 s; the sweep of ten games), the gate's own side at c661dcc7 (the solution "
       "build with warnings as errors; the suite of eleven projects: Rules 783, Units 408 of 409, Maps 244, Studio 390, Play 735, Authoring 167, and the rest, all passing but one; "
       "the Node viewport test; the chart supplement EQUAL; the three proofs), and the Docker Linux check on the branch's head (every step exit 0). The one failure was a Units test "
       "whose expected movement state was stale since task 40.7; corrected in 3cbc1f10, the solution built again with warnings as errors and the Units project run from that build "
       "(409 of 409), and Docker's run of every project was on that commit. The three comparisons read and every difference named: the text list, 3 texts changed and 50 added, "
       "each a task's sentence; the replay digest, identical for every older game once the state's list of moved weapons is set aside (it now holds a carried MG, task 40.7), and "
       "differing in nine of the pass's own games (six that main cannot read, three in which main stops at a record with a new fact); the sweep, 108 of 515,010 lines, all "
       f"`asl.game.move`: 104 sentences that now charge the overstack MF in the MPh, 2 moves that MF now refuses or prices, and 2 of the reworded refusal | {start} | {end} | {minutes} min | "
       "The digest hashes every property of the state, and the pass added eight to the state and more to the fire plan, so the first comparison differed in nearly every line. "
       "The proof tool now leaves named properties out (PROOF_SKIP) and gives a game the build cannot read one line; the digest was run three times and the sweep three times for it. "
       "The whole local suite was not run again on 3cbc1f10: the change was to one test file, that project was run, and Docker ran them all. The Units test project had been "
       "outside every task's filtered runs. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + row)

LESSONS = """- **A pass that adds a property to the state, a plan, or an event (pass 40):** the gate's digest and sweep hash every property, so every line differs and nothing is proved. Run the gate's side with `PROOF_SKIP` (Type.Property, comma-separated) naming the new properties, as `gate40-proofs-again.sh` and `gate40-sweep-again.sh` do; `pass40/new_properties.py` lists the candidates from the diff. A property that existed and changes its value (pass 40: the list of moved weapons) is set aside on both sides in a further run, to show nothing else moved.
- **A pass that adds an event type:** main cannot read a game that holds it, and the baseline's replay used to stop there. The proof tool now writes one `UNREADABLE` line for such a game and goes on.
- **The Units test project belongs to the blast radius of any change to the state's records.** Pass 40 added a fact to the moving stack's state in task 40.7 and ran Rules and Play classes only; a Units test that compares the whole record failed at the gate, two tasks and a review later.
- **A script of many edits must check every anchor before it writes any file.** One of pass 40's stopped on an anchor found twice after writing half its files, and the rest had to be split off into a second script. Collect the new texts first and write at the end.
- **Under a word to go on without stopping (pass 40):** each ruling is written in the design as decided under that word, numbered with the rest, and carried into the Passes Plan with those words, so a veto can find it. What departs from an answer the user gave (pass 40: two things question F10 named as built) goes first in the design's section, the review, and the report.
- **The attack a rule forces (A8.312):** before refusing a pass for it, ask the fire plan whether the attack can be made at all, or a game can stand still. A trial plan draws no dice and writes nothing.
"""
path = DOCS + "Prompts/Pass Standing Rules and Harness Lessons.md"
text = open(path, encoding="utf-8", newline="").read()
assert "A pass that adds a property to the state, a plan, or an event (pass 40)" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + LESSONS)
print("ok", minutes)
