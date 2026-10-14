"""Pass 40 task 40.7: the design's section 9 and the time log's row. Appends; asserts the anchors."""
DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"

SECTION = r'''
## 9. Portage and movement corrections: 40.7 (increment 2c)

Built on 2026-10-11 and waiting for the user's word before its commit. Seven corrections are built; two things the frame said it would take are put back to the user in 9.4.

### 9.1 The rules, as read on their pages

- **A4.11 (p. 48):** a SMC's allotment is four MF if it mounts, rides, or dismounts a conveyance in that Player Turn, and a wounded SMC's or Inexperienced Infantry's rises to four.
- **A4.4 (p. 50):** portage is assessed by item carried, not distance; a unit that carries a SW for one MF expenditure and drops it "may not recoup the portage cost"; "no item can be portaged more than once per phase".
- **A4.41 (p. 50):** no MMG, HMG, mortar, INF or RCL SW, or 5/8 inch non-vehicular ordnance that moved in the MPh, other than by being Recovered, fires in the ensuing AFPh; the German dismantled MMG or HMG may fire as a LMG.
- **A4.42 (p. 50), with its example:** a MMC's IPC is three and a SMC's one; one MF lost for each PP over it; "one SMC can add its IPC to that of any one Good Order Infantry unit ... provided the two units start the phase together and move together as a stack".
- **A4.43 (p. 50):** a SW dropped before any MF is spent was not portaged in that phase; unpossessed SW are removed in marsh, water, a Blaze, or a rubbled building.
- **A4.44 (p. 50):** Recovery only by an unpinned, Good Order, non-Bypassing unit; a SMC recovers at once, whatever the phase, when a unit is eliminated, surrenders, or routs away without its SW.
- **A4.5 (p. 51):** no Double Time for a unit that will mount, ride, or dismount in that Player Turn.
- **A4.51 (p. 51):** when the CX counter leaves.
- **A4.61 (p. 51), with its examples:** Assault Movement is one Location without all the MF; MF spent within a Location beside the change of Location do not prevent it, unless all MF are spent or CX is needed; a unit that breaks or goes berserk is no longer using it.
- **A4.72 (p. 52), with its example:** Difficult Terrain is a hex whose MF cost "(excluding SMOKE)" is four or all the unit's allotment.

### 9.2 The seven corrections

| # | Rule | What the code did | The change |
|---|---|---|---|
| 1 | A4.4, no recouped portage | The allotment read what the unit held at that moment, so a drop gave the MF back. | The state keeps the items portaged in the phase, each with the unit that first spent MF holding it (`GameState.PortagedThisPhase`, by `ScenarioA1MovementCalculator.Portaged`), emptied at every phase change. The portage read counts what the unit holds and what it portaged and dropped. A drop before any MF is spent records nothing, as A4.43 has it. |
| 2 | A4.4, one portage a phase | Nothing stopped a second unit carrying the item on. | `PortagedTwiceBar`: a step by a unit holding an item another unit portaged in this phase is refused. It may Recover it and stay. |
| 3 | A4.41, a moved MMG or HMG | Only a light mortar was barred. | The list of moved weapons now takes MGs too; the Fire reference reads a MG's size from the catalog; `MovedWeaponBar` refuses a medium or heavy MG that was carried into a new Location, in the AFPh. A LMG fires. A pushed Gun was already barred, by its TI. |
| 4 | A4.42, the leader's IPC | It went by itself to the one laden MMC, and to neither when two were laden (ruling R10.8). | The mover may name the MMC (the move's argument `ipcTo`, a select on the Movement panel when more than one unit is checked). The step records the choice and the movement state keeps it; a different name on a later step is refused, and so is a unit that is not a laden MMC with the leader. With one laden MMC it still goes there unasked. |
| 5 | A4.61, MF spent inside a Location | Any second step of an Assault move was refused. | `MoveTiming` allows a second step when both are Assault Movement and at most one of them changes the Location (the movement state keeps whether a step has). The step's own check still keeps the move short of all its MF, and Double Time is still barred with it. Fire reads Assault Movement as ended once a mover is broken or berserk (`AssaultHolds`), so FFNAM applies again. |
| 6 | A4.72, SMOKE left out | The cost tested for Difficult Terrain held SMOKE's MF. | The advance reads the step again without SMOKE for that test alone; the MF charged are unchanged. |
| 7 | A4.11 and A4.5, boarding | A unit boarded on its usual allotment, and after Double Time. | Boarding reads an allotment of four (`ConveyanceAllowance`), and is refused to a unit that has Double Timed this MPh. |

Recovery by a Bypassing unit (A4.44) is refused too (`RecoveryBypassBar`); it is counted with correction 2's row of the inventory.

**A4.51, read and left as it is.** The game removes the CX counter at the start of the unit's next MPh and when it breaks. The page's earlier moments (after its Prep Fire, as an Opportunity Firer, when it moves, when it becomes TI) all fall in or before that MPh, and the page itself says "a CX counter is removed at the start of a MPh". I found no effect the earlier removal would have that the game does not already give. The row's two missing drs (Search, and the +1 on SW Availability Checks) were not examined and stay in the row.

### 9.3 Ruling proposed

- **R40.11 (amends R10.8):** the leader's IPC goes to the laden MMC the mover names, for the whole move; unnamed, to the only laden MMC of his stack. Only a leader lends it (the page says "one SMC"; a hero's IPC is a backlog row). In the APh it still goes to the only laden MMC, since the advance has no field for it.

### 9.4 Put back to the user

Question F8 was answered yes to both of these. Reading them against the code, each is a feature and not a correction, so they are not built:

| Item | Why it is larger | Proposal |
|---|---|---|
| A SMC's Recovery at once, "regardless of phase", when a unit is eliminated, surrenders, or routs away (A4.44) | It is an act by one side in the middle of the other's phase, or its own fire phase: a new interruption of the kind the game has only for the DEFENDER's window. It also needs each loss to say which SW it left behind, which the events do not record apart from a Drop. | A backlog row for pass 45 (Infantry II: MGs and SW), which reworks possession. |
| Unpossessed SW removed in a marsh or a Blaze (A4.43), which section 5.1 placed here | No board in the library has a marsh, and a Blaze Location exists only as a burning wreck's hex; the removal is a projector rule with nothing to stage. | Its row moves to pass 185 (Fire), with the marsh and water cases to pass 125. |

Also left out, each for a backlog row: the four MF of a unit after it dismounts, and the Double Time bar for a unit that boards later in the turn (pass 65); a hero lending his IPC; the leader's IPC as a choice in the APh; the German dismantled MMG or HMG firing as a LMG (pass 45); INF and RCL SW, which the catalog does not hold.

### 9.5 The games

On board 3, written by `make_portage.py`.

| Game | What it holds |
|---|---|
| `p40-portage` (**recorded**) | A squad with a MMG (4 PP, so three MF) carries it from [D10] to [D9] for one MF and drops it. It then enters the woods of [C9] for two MF. A further step of one MF is refused: "has 0 MF left" (with the portage recouped it would have had one). The second squad Recovers the MMG in [D9]; its step to [D10] is refused: "The MMG was portaged by ... in this phase, and no item is portaged more than once a phase (A4.4)". In the AFPh its fire with the MMG is refused for A4.41. The file holds the steps, the Drop, the Recovery, and the phase changes; the three refusals were read and leave nothing in it. |
| `p40-ipc` (**recorded**) | Two squads, each with a MMG, and a leader in [D10]. The Movement panel offers "Leader's IPC to". With the first squad named, the stack moves to [D9]; naming the second squad on the next step is refused ("went to ... with this move's earlier step and stays with it"). After five MF the next step is refused for the second squad alone, "has 0 MF left": the named squad has six MF and the other five, as the example of A4.42 counts them. |

**No Studio game** for corrections 5, 6, and 7, nor for Recovery in Bypass or the loss of Assault Movement by a broken unit. Each needs its own staging (a SMOKE grenade's squad, SMOKE in a hex beside an advance, a vehicle with a boarding SMC, a Bypass with a SW on the ground) and rests on its Rules test below. The standing rule says a game is not skipped for cost without the user's word, so this is a question, not a decision: say so and I stage them before the commit.

Seen in the Studio and not changed: the "Leader's IPC to" list shows the units as the mover list does in the adjudicator's view, "g-carry (bd03:D10:0)", with the hex not in brackets.

### 9.6 Tests

- **Rules, new,** in `ScenarioA1Pass40RulesTests`: the leader's IPC by the mover's name and the bar on changing it; the phase's portage list and the bar on a second portage; Assault Movement with MF inside a Location, and its end by a break or berserk; a moved MMG or HMG in the AFPh, with the catalog's size of the MMG; Recovery in Bypass and the boarding allotment and Double Time bar.
- **Play, new:** `BacklogPass13Tests.PortageIsNotRecoupedByADropAndNoItemIsPortagedTwiceInAPhase`.
- **Run:** 156 Rules tests in six classes and 112 Play tests in seven classes (SW and portage, movement, Double Time and the advance, the mortar, fire, passengers, Close Combat), all passed.
- **No Play test** for the leader's IPC choice, the Assault step, the advance through SMOKE, boarding, Recovery in Bypass, or the AFPh bar: the first and the last were seen in the Studio, the others rest on Rules tests alone. Each would need a fixture the classes do not have ready (two laden squads with a leader, a SMOKE counter, a vehicle).

### 9.7 Recorded games

A move's MF are in its event, so a recorded game replays as it was played. A record made before the pass in which a unit dropped a SW mid-move and then spent the MF it got back would now find the later step over its allotment only if the projector checks a step's MF against the allotment; that was not read. The gate's replay of the live games will say.

**Not run:** the replay digest, the sweep, the text list, and the page's component tests. Texts added on purpose: the refusals `play.move-ipc` (two), `play.move-portage` (the second portage), `play.fire-weapon-moved`, `play.recover-bypass`, and `play.load` for Double Time; the panel's "Leader's IPC to" with its title.
'''

path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()
assert "## 9. Portage and movement corrections" not in text
open(path, "w", encoding="utf-8", newline="").write(text.rstrip("\n") + "\n" + SECTION)

ROW = ("| 40.8 committed at the user's word (fd036fa9), which ends increment 2b. Increment 2c, task 40.7, the portage and movement corrections: the MF "
       "allotment, the leader's IPC, the move's timing, Drop and Recovery, the moved weapons, the advance's Difficult Terrain, and boarding read in the "
       "code; seven corrections built (no recouped portage, one portage a phase, a moved MMG or HMG in the AFPh, the leader's IPC as a choice, Assault "
       "Movement with MF inside a Location, SMOKE left out of Difficult Terrain, four MF and no Double Time for boarding) with Recovery barred in Bypass; "
       "MapStudio built with warnings as errors at the first try; two games on board 3 played with every commit read back; five Rules tests and one Play "
       "test; the design's section 9 "
       "| 21:13 | 21:33 | 20 min | Estimate 0:40 in the plan, 1:00 in the frame. Two items the frame took are put back to the user as larger than a "
       "correction (a SMC's Recovery at once in any phase; SW removed in a marsh or Blaze). No Studio game for three corrections, put to the user as a "
       "question. A4.51 read and left. Not run: the replay, the sweep, the text list, the page's component tests. |\n")
path = DOCS + "ASL Unit Time Log.md"
text = open(path, encoding="utf-8", newline="").read()
assert text.rstrip("\n").endswith("the text list. |"), text[-80:]
open(path, "w", encoding="utf-8", newline="").write(text + ROW)
print("ok")
