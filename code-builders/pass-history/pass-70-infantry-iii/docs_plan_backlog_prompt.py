"""Pass 70 documents: the Passes Plan's rulings R70.1 to R70.48, the backlog's section 72 and its older rows that named pass 70, the redesign plan's
pass 70 status and the moved items under passes 75, 110, and 135, and the prompt's three wrong lines. Every anchor is checked before any file is written."""
import io
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders")
from edit_files import Edits  # noqa: E402

SCRATCH = "C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/f12c93b0-4f22-4bda-84a5-33ccd7c35641/scratchpad/"
WORD = ("Pass 70; decided under the user's word of 2026-10-25 (\"Start Pass 70 ... Run it without my intervention, up to the merge gate\"), "
        "open to a veto. ")


def read(name):
    return io.open(SCRATCH + name, encoding="utf-8", newline="").read().replace("\r\n", "\n").strip("\n")


rulings = read("rulings70.md").split("\n")
assert len(rulings) == 48, len(rulings)
rows = []
for n, line in enumerate(rulings, 1):
    assert line.startswith(f"| R70.{n} | "), line[:20]
    assert line.count(" | W ") == 1, n
    rows.append(line.replace(" | W ", " | " + WORD))
backlog72 = read("backlog72.md")

e = Edits()

# 1. The Passes Plan: the rulings after the R65 rows.
PLAN = "docs/ASL Unit Backlog Passes Plan.md"
r6533_end = "read the ∆ backwards; the fixing agent caught it on the page, and the session confirmed it there. |\n\n## Appendix A."
e.edit(PLAN, r6533_end,
       "read the ∆ backwards; the fixing agent caught it on the page, and the session confirmed it there. |\n" + "\n".join(rows) + "\n\n## Appendix A.")

# 2. The backlog: older rows that named pass 70.
B = "docs/ASL Unit Backlog.md"
older = [
    ("it moves to pass 70 (section 71.1).** |",
     "it moves to pass 70 (section 71.1).** **Pass 70 (2026-10-25): wired into Play: a Rider attacks apart with +1 and is attacked with -1, a CE "
     "halftrack's Passengers join its attack, and only an Immobile vehicle's PRC are held in Melee (design 16.2; rulings R70.30, R70.31).** |"),
    ("kept as the invariant it is. |",
     "kept as the invariant it is. **Closed by pass 70 (2026-10-25; ruling R70.29): they are plumbing, not rules, and stay in Units; their share of "
     "a replay stays a performance item.** |"),
    ("| Pass 70 (morale) | The leader is lost and no LLMC is made, as in the paths that already rolled the dr. |",
     "| Pass 70 (morale) | The leader is lost and no LLMC is made, as in the paths that already rolled the dr. **Built in pass 70 (design 9.2; "
     "rulings R70.3, R70.4): the LLMC is made through `ScenarioA1MoraleCheck` after a leader lost to an Interdiction, Mopping Up, a Sniper, Rally "
     "Fate, a PF firer's Casualty Reduction, or Failure to Rout, and recorded as `morale-check-resolved`.** |"),
    ("Refused (`target-vehicle-concealed`), counting only what the firer can see. |",
     "Refused (`target-vehicle-concealed`), counting only what the firer can see. **Moved on by pass 70 (2026-10-25; ruling R70.2) to pass 75 "
     "(task 75.6), which rebuilds the FT, MOL, and DC records; section 72.1.** |"),
    ("| Pass 70 (task 70.7; moved from pass 75 on 2026-10-25) | No capture names a Passenger. |",
     "| Pass 70 (task 70.7; moved from pass 75 on 2026-10-25) | No capture names a Passenger. **Built in pass 70 (design 17.2; ruling R70.34): "
     "ordinary Infantry CC at the Passengers of an unarmored stationary vehicle, -2 at them and +2 on their attacks at the captors; the vehicle "
     "captured with them. The +2 on the vehicle's whole attack is a reading for the user (section 72.2).** |"),
    ("| Pass 70 | Refused with its reason (`ScenarioA1WreckRules.CloseCombatBar`). |",
     "| Pass 70 | Refused with its reason (`ScenarioA1WreckRules.CloseCombatBar`). **Moved on by pass 70 (2026-10-25; ruling R70.2) to pass 110, "
     "with the rest of D10.1; section 72.1.** |"),
    ("| Pass 70 | It recombines as any other HS does. |",
     "| Pass 70 | It recombines as any other HS does. **Built in pass 70 (design 15.2; ruling R70.28): the leaving crew's `instance-created` "
     "records the vehicle it left (`crewOf`), and such a HS Recombines with no leader (A1.32).** |"),
    ("decides D6.651 in Rules, with its test, and no Play path reads it. |",
     "decides D6.651 in Rules, with its test, and no Play path reads it. **Built in pass 70 (design 9.2; ruling R70.8): leadership both ways and "
     "the LLMC for PRC through the Morale Check member; Heat of Battle is not owed (A15.1). In Fire a CE leader in an unmoved halftrack leads only "
     "his fellow Passengers (section 72.1).** |"),
    ("| Pass 70 (task 70.7) | Decided in Rules (`ScenarioA1PassengerActionRules.CloseCombat`), with its test; a CC action that names a PRC is refused in Play. |",
     "| Pass 70 (task 70.7) | Decided in Rules (`ScenarioA1PassengerActionRules.CloseCombat`), with its test; a CC action that names a PRC is "
     "refused in Play. **Built in pass 70 (design 16.2; ruling R70.31): a Rider attacks apart with +1 and defends with -1, a CE halftrack's "
     "Passengers join its attack, a BU Passenger is offered nothing, and only an Immobile vehicle's PRC are held in Melee. A Rider's CC against a "
     "vehicle stays refused (section 72.1).** |"),
    ("| Pass 70: a condition that holds them in Melee | None is placed; the proposal says the units are not held in Melee. |",
     "| Pass 70: a condition that holds them in Melee | None is placed; the proposal says the units are not held in Melee. **Decided by pass 70 "
     "(2026-10-25; ruling R70.32): no stored condition, since units are held in Melee only at the CCPh's end; the unload and Bail Out records say "
     "the units are in an enemy Location and not held in Melee (design 16.2). A map badge is a row of section 72.1.** |"),
    ("| Pass 70; the ELR as `FirePrc.Elr` carries it | The NMC at the unit's Morale Level alone. |",
     "| Pass 70; the ELR as `FirePrc.Elr` carries it | The NMC at the unit's Morale Level alone. **Built in pass 70 (design 9.2; ruling R70.9): a "
     "Rider leader's DRM, Wound Severity for a SMC, and ELR Replacement, never Disruption, recorded as `drm` and `elr` on `rider-bailed-out`; "
     "Heat of Battle is not owed (A15.1).** |"),
    ("is built; no CC counter is placed. |",
     "is built; no CC counter is placed. **Decided by pass 70 (2026-10-25; ruling R70.32): no stored condition; the fire proposal says the PRC "
     "that survive are in an enemy Location and not held in Melee (A7.211, A11.71). The fire record line is a row of section 72.1.** |"),
    ("| Pass 70 | A crew enters by re-crewing in its own MPh. |",
     "| Pass 70 | A crew enters by re-crewing in its own MPh. **Built in pass 70 (design 17.2; ruling R70.34): a captor may enter the vehicle it "
     "captured as its crew in the CCPh of its capture, whatever MF it spent (`RecrewFacts.CapturedThisCcph`).** |"),
    ("does not know a crew is already broken; a Failure to Rout does not Abandon the AFV. |",
     "does not know a crew is already broken; a Failure to Rout does not Abandon the AFV. **Pass 70 (2026-10-25; ruling R70.10): a crew already "
     "broken that the attack reaches is eliminated by Casualty Reduction (`crewBroken` on the vehicle target); its ELR, Heat of Battle, and "
     "leadership are not owed (A15.1, A19.11, D3.44). Left open: a broken crew's Failure to Rout.** |"),
]
for old, new in older:
    e.edit(B, old, new)

# 3. The backlog's section 72, after section 71 (the last row of 71.2 ends the file).
last71 = "| Captured as the CCPh ends whenever the bar holds. |\n"
e.edit(B, last71, last71 + "\n" + backlog72 + "\n")

# 4. The redesign plan: pass 70's status, and the moved items under passes 75, 110, and 135.
RP = "docs/ASL Card Play and Map Studio Redesign Plan.md"
e.edit(RP, "**Pass 70: Infantry III: morale, Close Combat, and concealment** (new; 35 rows of the inventory). Counters: none. Needs first: 40 (Infantry OVR, ADJACENT).",
       "**Pass 70: Infantry III: morale, Close Combat, and concealment** (new; 35 rows of the inventory). Counters: none. Needs first: 40 (Infantry "
       "OVR, ADJACENT). **Status, 2026-10-25: built to the merge gate on `feature/asl-backlog-pass-70`, cut from 25aa97fd; the gate not run. The "
       "user's word of 2026-10-25 (\"Run it without my intervention, up to the merge gate\") ends at the gate, and the gate, the push, the pull "
       "request, and the merge are each the user's to start.** The ten tasks are built, in ten increments (3a to 3j) in ten commits and five rounds "
       "of the reviews' fixes (8f92714a to 4d53f29f); no increment was left out (ruling R70.1). The plan names 35 inventory rows; with the items "
       "moved here (the paragraphs after the table) the inventory named 48: 29 are built (6 of them with a deviation) and the remainders of 19 went to the pass that takes what is left "
       "(passes 75, 80, 100, 105, 115, 120, 135, 165, and 180). The frame's estimate, made from the code, was 33:05 of building against the plan's "
       "7:20 (34:20 in all against 8:35); the build took 8:07 by the clock. Rulings R70.1 to R70.48 are open to a veto. What the pass moved went to "
       "pass 75 (naming an AFV in a mixed Location, with 75.6; the SMC crew's Stun wound, with 75.3; an Infantry OVR on a lone SMC revealed), pass "
       "110 (CC against a wreck, with the rest of D10.1), pass 135 (Withdrawal to another level of a building), and the backlog (5/8\" Dummies and "
       "Cavalry under concealment, a vehicle's secret Target Size and Smoke Dispenser, the actions not yet built that cost a \"?\", Searching for "
       "mines and Booby Traps, and the rout's fortification and Blaze options), each with its reason in the [design](<Passes/ASL Unit Backlog Pass "
       "70 Design.md>), sections 2, 8.1 to 17.1, and 18, the [review](<Passes/Scenario A1 Backlog Pass 70 Review 2026-10-25.md>), and the "
       "backlog's section 72.")
e.edit(RP, "a DC thrown or Placed by a Passenger (D6.72, with 75.6).\n",
       "a DC thrown or Placed by a Passenger (D6.72, with 75.6).\n\n"
       "Moved here by pass 70 (2026-10-25), with no task renumbered: naming an AFV in a Location that also holds concealed or hidden units or a "
       "Dummy (A22.34, A22.612, A23.5, with 75.6; ruling R70.2); the SMC crew's Stun wound in the projector, with no Wound Severity dr and a second "
       "wound eliminating (D5.34, A17.11, with 75.3); an Infantry OVR offered on a lone SMC revealed outside the reviewed building case (A12.15); "
       "the voluntary removal's Sniper exception (A12.14, A14.2, with 75.1); a berserk charge into a Location holding an enemy vehicle and Infantry "
       "(A15.431, A15.432, with 75.2); the rout's reading of Unarmed Personnel as armed (A10.5, A20.54, with 75.5).\n")
e.edit(RP, "the Specific Collateral Attack after a Gun's HEAT with no HE FP (D.8A).\n",
       "the Specific Collateral Attack after a Gun's HEAT with no HE FP (D.8A).\n\n"
       "Moved here by pass 70 (2026-10-25), with no task renumbered: CC against a wreck, with the rest of D10.1 (D10.1, A11; ruling R70.2); a "
       "mortar's minimum range in Interdiction (A10.532); a crew manning a Gun that is not Emplaced, under concealment, and a non-Emplaced Gun's "
       "Case H loss (A12.2).\n")
e.edit(RP, "a CE crew entering a building (D5.3).\n",
       "a CE crew entering a building (D5.3).\n\n"
       "Moved here by pass 70 (2026-10-25), with no task renumbered: Withdrawal to another level of a building (A11.21; ruling R70.2); Mopping Up "
       "of rubbled Locations (A12.153).\n")

# 5. The prompt's wrong lines.
PR = "docs/Prompts/Pass 70 Prompt I.md"
e.edit(PR, "- **Main is at 7a734fc3** (pull request 38, proof games trimmed to the essentials), green in CI (run 40206829533, 3:58). Nothing is running in CI.",
       "- **Main is at 25aa97fd** (pull request 39: this prompt and the five rows moved from pass 75), not 7a734fc3 (pull request 38, proof games "
       "trimmed to the essentials) as first written here: pull request 39 merged after the prompt was written, and its CI run on main "
       "(40208728075) was in progress when the pass began (corrected 2026-10-25, design section 5). Pull request 38's run (40206829533, 3:58) is "
       "green.")
e.edit(PR, "The Codex worktrees (`asl-narrative-extensions`, `codex/formation-asl-map-alignment`) are weeks behind main and touch nothing of this pass.",
       "The Codex worktrees (`asl-narrative-extensions`, `codex/formation-asl-map-alignment`) are weeks behind main and touch nothing of this pass. "
       "A third worktree, `.claude/worktrees/replay-reenact-scope-9c9f9a` (on `claude/replay-reenact-scope-9c9f9a` at 1f621cf4, the parked Replay "
       "re-enactment scope), exists beside them and touches nothing of this pass either (corrected 2026-10-25, design section 5).")
e.edit(PR, "no Studio on 6670 or 6671; the user's Studio on 5178;",
       "no Studio on 6670 or 6671; the user's Studio not running on 5178 when the pass began (it said \"the user's Studio on 5178\"; the user's "
       "Studio was running by 16:15) (corrected 2026-10-25, design section 5);")

e.apply()
