"""Append increment 2a's section (task 45.8) to the pass 45 design, and the task's row to the time log."""
DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
SECTION = """
## 7. The shape of an attack: 45.8, 45.9 (increment 2a)

Built on 2026-10-12 under the user's word of that morning: "Implement all parts of Pass 45 with out my intervention running all Studio tests as appropriate and then stop before the gate." Each task is committed without a stop, and each ruling below is decided by the proposal the frame put (F1 to F10, answered yes) or, where the frame put none, marked **decided under the user's word of 2026-10-12**.

### 7.1 Task 45.8

**A fire kind for each firer (A8.3, A8.31, A8.312, pp. 61 to 62; question F4).** The attack keeps one recorded kind, the heaviest among its firers, so recorded games keep their shape. Inside an attack of Subsequent First Fire or FPF each firer fires by its own counter:

- a unit marked Final Fire uses FPF: Area Fire, the NMC on the Original DR, its MG in Sustained Fire;
- a unit marked First Fire uses Subsequent First Fire: Area Fire, the range limits of A8.3, its MG in Sustained Fire;
- a unit with no counter fires First Fire: its FP whole, its MG's ROF kept on the colored dr, and it is marked First Fire, not Final Fire;
- a unit with no counter that fires a MG marked First Fire uses Subsequent First Fire with the MG alone: its own FP whole, the MG halved and in Sustained Fire, and unit and MG both marked Final Fire. This is the page's example: "{4 [4-6-7 FP] + 1 1/2 [LMG Area Fire]} x 2 [PBF] = 11 ... the 4-6-7 and LMG would then both be marked with a Final Fire counter".

The resolution carries the firers whose counter differs from the attack's (`FirerCounters`), written only when one does. The planner's refusal "a group mixes firers marked for different kinds of Defensive fire" is gone.

**Ruling R45.1** (question F4, approved in the frame): each firer of a Defensive fire attack fires by its own counter, and the attack's recorded kind is the heaviest. It replaces the "one kind an attack" part of ruling R12.3 and closes ruling R40.17: the Mandatory FG of A8.312 is every unit that owes the attack. The texts that cite R40.17 stay for a group the fire rules still refuse for another reason.

**Ruling R45.2** (question F4, approved in the frame; replaces R40.19): while the Mandatory FG's attack is owed, a Gun's shot, a thrown DC, and a Starshell by the DEFENDER wait for it (`play.act-mandatory`). As with the fire action, the bar is on a proposal that names a unit or a weapon of the side that is not moving, so no refusal tells the ATTACKER who owes the attack; and it stands only while the attack can be made at all.

**The FPF whose NMC replaces its firer (backlog 56.1).** Reproduced in a Play test with an Original DR of 11: the firer was Replaced by ELR, and the fire marker was then written to the unit the Replacement had consumed, which the gate refused. A firer that its own NMC eliminates, Reduces, or Replaces now takes no fire marker; the unit in its place keeps the Final Fire counter it had. Fixed.

**The group as one chain (A7.5, p. 57).** The Locations of a group are reached one from another, starting at the first; two pairs apart are refused as before by `firer-outside`. Vertical ADJACENT and ADJACENT across a wall or hedge were built by pass 35 and are unchanged.

**No berserk unit in a multi-Location group (A7.54).** The firer facts carry the berserk state; the refusal is `berserk-group-outside`, with its sentence for the page.

**A crew's FP after its Gun fired (A7.353, p. 56).** "Subsequent First Fire (8.3), FPF (8.31), and Final Fire (8.4) vs adjacent units retain halved inherent FP". The crew, marked First Fire by its Gun's shot, fires its own FP in Subsequent First Fire and in Final Fire against an adjacent unit, as it did in FPF since pass 40. **Correction to the frame's point 5.7:** the "halved" of A7.353 is the Area Fire halving those three kinds of fire already carry, not a further one. The code halves once, and ruling R40.18's figure needs no amendment.

**The MG's Bore Sighting (C6.41 to C6.44, p. 174; question F6).** The setup accepts a MMG or HMG the Scenario Defender possesses, for a Location outside its hex, in its LOS, within its Normal Range and 16 hexes; the record names its possessor and their setup Location. The setup editor shows the Bore Sighted field for a MG. The attack deducts 2 when every firer fires its MG alone, each MG has its sighting on the target Location, and the possessor is the unit that set up with it, in its setup Location.

**Ruling R45.3, decided under the user's word of 2026-10-12:** against Infantry the minus 2 is given in an attack of the kind First Fire only. The page says "only such an attack conducted as Defensive First Fire (A8.1)", and cites A8.1, not A8.3 or A8.31. A squad's own FP has Bore Sighted nothing, so a MG firing with its squad's FP has no DRM ("only if all elements of that FG have Bore Sighted that same Location").

**Left out, each with a backlog row:**

- A MG that leaves its setup Location and comes back claims its Bore Sighting again. C6.43 loses it for good; the game tests where the MG stands when it fires.
- "In a building hex only the ground-level Location ... may be Bore Sighted" (C6.42) is not checked, for a Gun or a MG.
- The CC counter on the co-occupants after a TPBF FPF (A8.31): nothing in the game reads it.
- A crew that fired its Gun and carries no fire counter (its Gun's shot kept its ROF) has no FP in a First Fire attack, as before.
- A light mortar's Bore Sighting (pass 110).

**The games,** on board 3, played in my Studio on 6671 in the adjudicator's view and read back from their files:

| Game | What it shows | Recorded |
|---|---|---|
| `p45-fg-mixed` | Two Russian squads in [C9], one marked First Fire; a berserk squad enters. The page's note asks for one Mandatory FG of both. The attack is recorded as subsequent first fire: 12 FP (TPBF) and 6 FP (TPBF, Area Fire), the 16 column; the unmarked squad is marked First Fire and the other Final Fire; 8 Residual FP. | yes |
| `p45-fg-plain` | The control: neither squad marked. First fire, 24 FP. | yes |
| `p45-fg-bore` | A Russian MMG in [F10] Bore Sighted on [D10] fires alone at a squad that enters it: DRM "- 2 (bore sighting, C6.44) - 1 (FFNAM) - 1 (FFMO)". | yes |
| `p45-fg-unsighted` | The control: no sighting, DRM -2 in all. | yes |

The games are written by `pass45/make_458.py` in the tools folder. The setup editor's field for a MG is checked with the whole pass's Studio check, in a game set up by hand.

**The tests.** Rules: `ScenarioA1Pass45RulesTests` (ten tests of the kinds, the chain, the berserk bar, the crew, Bore Sighting, and the held acts); `ScenarioA1Pass12Tests` has one assertion turned, from "an unmarked unit does not join FPF" to its First Fire counter. Play: `CloseCombatStepsTests` gains the mixed Mandatory FG with the held Gun's shot, and the FPF whose firer is Replaced; `BacklogPass8Tests` gains the crew's Subsequent First Fire. Classes run: those three, and `ScenarioA1Pass40RulesTests`, `BacklogPass12Tests`, `BacklogPass30Tests`, `FireTests`, `RallyAndFireStepsTests`, `RefusalReasonsTests`, `FireOwnReasonsTests`. No record of the state changed, so the Units project was not run.
"""
ROW = "| The frame committed at the user's word with yes to F1 through F10 (8f0aeb50); then the user's word: \"Implement all parts of Pass 45 with out my intervention running all Studio tests as appropriate and then stop before the gate.\" Increment 2a, task 45.8: the fire kinds read in the calculator and the planner; a kind for each firer (ruling R45.1), every other act held behind the attack owed (R45.2), the FPF refusal of backlog 56.1 reproduced and fixed, the group as one chain, the berserk bar, a crew's FP in Subsequent First Fire, a MMG's and HMG's Bore Sighting with the setup's field (R45.3); MapStudio built three times into bin/p31b; ten Rules tests and three Play tests; four games on board 3 played in my Studio | 08:49 | 09:12 | 23 min | Estimate 0:40 and 0:20 for the setup change. One older test turned (pass 12) and one split (pass 8). The frame's point 5.7 corrected: A7.353's halving is the Area Fire's own. |\n"

with open(DOCS + "Passes/ASL Unit Backlog Pass 45 Design.md", encoding="utf-8", newline="") as f:
    design = f.read()
assert "## 7. The shape of an attack" not in design
with open(DOCS + "Passes/ASL Unit Backlog Pass 45 Design.md", "w", encoding="utf-8", newline="") as f:
    f.write(design.rstrip("\n") + "\n" + SECTION)
with open(DOCS + "ASL Unit Time Log.md", encoding="utf-8", newline="") as f:
    log = f.read()
with open(DOCS + "ASL Unit Time Log.md", "w", encoding="utf-8", newline="") as f:
    f.write(log.rstrip("\n") + "\n" + ROW)
print("ok")
