"""Appends section 24 (pass 32.j as built, and the migration's closing paragraph) to the pass 32 design from the scratchpad draft, with the placeholders filled."""
import pathlib
import sys

DESIGN = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/Passes/ASL Unit Backlog Pass 32 Design.md")
DRAFT = pathlib.Path("C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/f342de29-f1b3-4419-a071-a5a8835e2ac6/scratchpad/section24.md")
gate_sweep = sys.argv[1]
text = DESIGN.read_bytes().decode("utf-8")
assert "## 24." not in text
draft = DRAFT.read_text(encoding="utf-8")
fills = {
    "COMMIT10": "bd1595a",
    "PROOFS": ("P1: no existing test changed; the tests were only added (commit 10). The baseline was built from an archive of 042d14a, checked for "
               "`ScenarioA1SequenceProjection` (72 uses) and for no `ScenarioA1SetupCalculator`; its replay (88,728 lines over 309 games, 770 s) and its sweep over "
               "`p31c-tw`, `pass16-demo2`, `p26-demo`, and `p25-visual` (513,542 lines, 1,392 s) ran alone beside the first seven commits. The first after-proof, "
               "on commit 8's build, threw at the first `setup-concealed` event of the first game: the projector's map from the verdicts' conditions to its names had no "
               "Concealed entry (commit 9). Against the baseline, the commit 9 build's replay (1,054 s) and sweep (2,014 s beside the tests) are equal and its text "
               "list is equal. The gate build's proofs (commit 10, the proof rebuilt against `bin/gate/Debug/net10.0/`, the copies dated with the build) give the same: "
               f"the replay digest equal, the text list equal, and the sweep {gate_sweep}."),
    "TESTS": ("Rules 24 of 24 by filter, of them the 14 new; Units 409 of 409; Play 162 of 162 over passes 17, 19 to 21, 23 to 26, 30, and 30b; "
              "MapStudio 32 of 32 over the pass 25, 26, and 28 pages and the setup plans and answers"),
    "STUDIO": ("On 6671 from bin/p31b, since another chat's Studio held 6670 again: `p26-demo` opened at its end with the German win from Rules "
               "(9 Exit VP off the bottom edge near [G10], [Q10], [Y10], at least 8) and both standings; a new game `p32j-check` started from The Guards Counterattack, "
               "its German side set up from the Forward line plan (every check passed, 26 counters in 10 hexes), the stack at [F5] moved to level 2 (still passing), "
               "and three squads put under \"?\" refused by Rules: the group uses 3 \"?\" at setup and has 0 (A12.12; ruling R19.5); every proposal cancelled, "
               "the game left as untracked Studio data, the Studio stopped."),
    "GATE": ("the solution build with warnings as errors into bin/gate (0 warnings, 4:41); the Node viewport test 5 of 5; the chart supplement equal; "
             "the local suite (Dice 24, Rules 703, Units 409, Maps 244, Maps.Vasl 121 with 28 skipped, Maps.Rendering 34 with 2 skipped, Units.Rendering 352, "
             "CounterSheets 19, MapStudio 381, Play 692, Authoring 167: all pass); Docker on the committed branch, every project passes, the five steps exit 0."),
    "PACE": ("(the review, the tests, the proofs, the Studio, the gate, and the documents) about 1:40 of work across the two hours the proofs and the suite ran "
             "beside each other (23:30 to 01:10): about 2:45 for the pass in all"),
    "SUMHOURS": "47 hours (the time log's pass 32 sections, 44:09 through the block plus this pass)",
}
for key, value in fills.items():
    assert draft.count(key) == 1, key
    draft = draft.replace(key, value)
DESIGN.write_bytes((text.rstrip("\n") + "\n" + draft).encode("utf-8"))
print("section 24 appended")
