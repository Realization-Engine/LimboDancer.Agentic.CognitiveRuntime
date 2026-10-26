# ASL Unit Backlog Pass 70 Design

**Status:** The frame, written on 2026-10-25 under the user's word of that day ("Start Pass 70 (Infantry III: morale, Close Combat, and concealment). Run it without my intervention, up to the merge gate. Questions go to read-only sub-agents; each decision is a numbered ruling R70.n, open to my veto."). The gate, the push of the pass's branch, its pull request, and the merge are each the user's to start. The frame's questions were put to four read-only agents, as the standing rules ask in this mode. Their answers were judged against the pages and the code. Each decision below is a ruling decided under that word, and each is open to the user's veto.

**Date:** 2026-10-25

**Related documents:**
- the [redesign plan](<../ASL Card Play and Map Studio Redesign Plan.md>), the entry "Pass 70: Infantry III: morale, Close Combat, and concealment";
- the [rule inventory](<../ASL Rule Inventory A to E.md>) (48 rows give pass 70);
- the [backlog](<../ASL Unit Backlog.md>), the rows that name pass 70;
- the [pass 65 design](<ASL Unit Backlog Pass 65 Design.md>), sections 2 to 6;
- the [standing rules](<../Prompts/Pass Standing Rules and Harness Lessons.md>);
- the [Memory and Performance Guidelines](<../Memory-And-Performance/ASL Memory and Performance Guidelines.md>);
- the session's prompt, `Prompts/Pass 70 Prompt I.md`.

**The rulebook.** Four read-only agents read it for this frame, from the page texts of the working folder: pages 55, 65 to 80, 83, 85 to 87, 200, and 203 to 206. I read on their pages the sentences the rulings lean on: A10.2 (p. 65), A10.41 (p. 66), A10.5 (p. 66), A10.711 (p. 69), A11.13 (p. 72), A12.152 (p. 79), A12.16 and A12.2 (p. 79), and A15.1 (p. 83). Each task reads its own pages again before it builds.

**What the frame rests on.** The four agents' reports are kept in `code-builders/pass-history/pass-70-infantry-iii/`: `frame_morale.md` (A: morale outside fire and rout), `frame_sight.md` (B: who sees, concealment, HIP, the Right of Inspection), `frame_cc.md` (C: Close Combat), and `frame_search_codemap.md` (D: Bypass and detection, Searching and Mopping Up, `CheckInvariants`, and the code map). The session's running record is `notes.md` in the same folder.

## 1. Outline of this design

1. This outline.
2. What the pass is, what departs from the plan's entry, and its increments.
3. The rows, by task and increment.
4. What is certain, and what needs a read.
5. What the frame found outside the rows, and corrections to the prompt.
6. The frame's questions, each with its ruling.
7. Memory and performance: the checklist and the measurement.

Then, as each is built: one section per increment, then the reviews, the Studio check, as built, and the gate.

## 2. What the pass is

Pass 70 is three things Infantry do that the engine has only in part:
- **morale outside fire:** one Morale Check member for every MC made outside the Fire and Rally packages, the Voluntary Break and Voluntary Rout, and rout by level, by vehicles and Guns, and by SSR Fanaticism;
- **concealment:** one read of who sees, the owner's choices on a "?", concealment for vehicles and an Emplaced Gun's crew, Bypass and detection, Searching and Mopping Up anywhere, and the Right of Inspection by LOS;
- **Close Combat:** Withdrawal completed, CC with a Gun's crew, Infantry against Infantry in a Location with a vehicle, its Ambush, Street Fighting, and the CC rows pass 65 and the 2026-10-25 move put here.

**What departs from the plan's entry, first:**

- **The pass is four times the plan's size by the agents' estimates.** The plan gives 7:20 of building. The agents' estimates sum to about 33:00 (section 6, F1). Pass 65 built in 7:13 against its frame's 15:35, so the build may run about 16 hours. The pass stays one pass (ruling R70.1), built in the order of the table below, with the last increment (Street Fighting, A11.52's Passengers, A18.12 against a vehicle, and D5.42) the one a session's end would leave out.
- **Moved out of the pass** (ruling R70.2):
  - to pass 110: CC against a wreck (D10.1), with the rest of D10.1, which the inventory already gives pass 110;
  - to pass 135: Withdrawal to another level of a building;
  - to pass 75 (task 75.6): naming an AFV in a Location that also holds concealed or hidden units or a Dummy (pass 60's row), since it is a fault of the FT, MOL, and DC records 75.6 rebuilds;
  - to pass 75 (task 75.3): the SMC crew's Stun wound in the projector (D5.34 with A17.11);
  - to the backlog: an Infantry OVR offered on a lone SMC revealed outside the reviewed building case (A12.15); 5/8" Dummies, Cavalry, horses, and bicycles under concealment, a vehicle's secret Target Size, and the Smoke Dispenser (A12.2); entrenching, setting a Fire, and Clearance as actions that cost a "?" (A12.141), with the passes that build those actions; Searching for minefields and Booby Traps and the Fortified Building's reveal; a rout's destination refigured when an enemy becomes newly Known; the minefield, FFE, shellhole, entrenchment, and Blaze rout options and Spotted Fire Interdiction.
- **The documents correct five inventory rows** the frame found stale (section 5).

**The increments,** in the order they are built:

| Increment | Tasks | What | Estimate |
|---|---|---|---|
| 1 | The frame | This document. | |
| 3a | 70.3, 70.10 | One read of who sees (`ScenarioA1Sight`, memoized per state); its callers; the actions that cost a "?"; the Right of Inspection by LOS in every side's view. | 4:30 |
| 3b | 70.1 (I) | One Morale Check member for every MC outside Fire and Rally; Unarmed in Fire (A15.1, A19.11); the Interdiction NMC's Heat of Battle and LLMC; LLMC outside fire; the Bail Out's leadership, wound, and ELR; PRC leadership and LLMC; D5.311's broken crew. | 6:05 |
| 3c | 70.1 (II) | Voluntary Break (A10.41) and Voluntary Rout (A10.711). | 2:00 |
| 3d | 70.2 | Rout from the building it starts in; Interdiction by vehicles and Guns; Fanaticism by SSR. | 2:25 |
| 3e | 70.4, 70.5 | Voluntary removal of a "?" and the viewer's option; "?" for an Emplaced Gun's crew and for vehicles; entering concealed; HIP for vehicles and an Emplaced Gun out of Concealment Terrain. | 5:30 |
| 3f | 70.8 | Bypass endings and detection: Minimum Move entry, the forced-back lane, broken or pinned in Bypass, occupying the obstacle, TPBF inside the hex, a vehicle's Bypass ending. | 1:45 |
| 3g | 70.9 | Searching; Mopping Up in any building of the map; one pass over the occupied Locations. | 4:15 |
| 3h | 70.6 | Withdrawal completed (dropping SW, a concealed destination, the view's bar); CC with a Gun's crew and A11.13's Gun loss; a Carrier HS recombining (D6.82). | 1:35 |
| 3i | 70.7 (I) | Infantry against Infantry in a Location with a vehicle, in sequence; its Ambush both ways; PRC in CC and the Melee of PRC; the CC counter's wording. | 3:00 |
| 3j | 70.7 (II) | Street Fighting; A11.52's capture of Passengers; A18.12 against a vehicle; D5.42's entry at a capture. | 2:00 |
| | Overhead | The reviews, the Studio check, the documents (the plan's 1:15). | 1:15 |
| | **Total** | build 33:05 | **34:20** |

## 3. The rows, by task and increment

| Task | Rule (page) | What the row asks | Placed |
|---|---|---|---|
| 70.1 | A10.2 (65) | LLMC and LLTC outside fire | 3b |
| 70.1 | A10.41 (66) | Voluntary Break | 3c |
| 70.1 | A10.711 (69) | Voluntary Rout | 3c |
| 70.1 | A15.1 (83) | Heat of Battle's Unarmed and PRC exemptions; its DR in MCs outside Fire and Rally | 3b |
| 70.1 | A17.1 (85) | The wound procedure in every Casualty Reduction | 3b |
| 70.1 | A19.11 (86) | An Unarmed unit's ELR immunity | 3b |
| 70.2 | A10.5, A10.51 (66) | Rout by level and inside a building | 3d (largely built; the starting-building exception) |
| 70.2 | A10.53, A10.532 (67) | Interdiction by vehicles and Guns | 3d |
| 70.2 | A10.8 (69) | Fanaticism by SSR | 3d |
| 70.3 | A12.1, A12.14, A12.141 (76 to 78) | One Good Order read; the actions that cost a "?" | 3a |
| 70.4 | A12.14 (77) | Voluntary removal; the viewer's option | 3e |
| 70.5 | A12.11, A12.12, A12.121 (76, 77) | "?" for an Emplaced Gun's crew and vehicles | 3e |
| 70.5 | A12.2, A12.3, A12.34 (79, 80) | Entering concealed; HIP for vehicles and out of Concealment Terrain | 3e |
| 70.6 | A11.1, A11.21 (72, 73) | Withdrawal completed; CC with a Gun's crew | 3h |
| 70.7 | A11.31, A11.34, A11.4 (73) | Infantry against Infantry with a vehicle; Ambush | 3i |
| 70.7 | A11.5, A11.51 (74) | CC DRM with a vehicle; the Ambush -1 and +1 | 3i |
| 70.7 | A11.52 (74) | Capture of a vehicle, its Passengers included | 3j |
| 70.7 | A11.8 (76) | Street Fighting | 3j |
| 70.7 | A18.12 (85) | Leader Creation in CC against a vehicle | 3j |
| 70.8 | A12.13 (77) | Ordnance at a mixed Location | built in pass 60 (R60.9); documents only |
| 70.8 | A12.15, A12.151 (78) | Detection; Bypass | 3f |
| 70.8 | A12.42 (80) | A vehicle's Bypass ending | 3f |
| 70.9 | A12.152, A12.153, A12.154 (79) | Searching; Mopping Up; Search Casualties | 3g |
| 70.10 | A12.16 (79) | Right of Inspection by LOS | 3a |

The items pass 65 moved under the entry are placed with their task:
- with 3b: Heat of Battle, leadership, and LLMC for PRC, and D6.651's reach of a Passenger leader; the Bail Out NMC's leadership, Wound Severity, and ELR; D5.311's crew;
- with 3h: a Carrier HS recombining without a TC or a leader (D6.82);
- with 3i: PRC in CC (A11.611, A11.71); the CC counter on an unload or a Bail Out into an enemy Location (D6.5, D6.24), and TPBF's at PRC (A7.211);
- with 3j: entering a vehicle at its capture in the CCPh (D5.42);
- to pass 110: CC against a wreck (D10.1).

The older rows: the LLMC after a leader's loss outside fire (pass 35's) with 3b; naming an AFV in a mixed Location (pass 60's) to pass 75; `GameProjector.CheckInvariants` (pass 32.i's) closed by ruling R70.29.

## 4. What is certain, and what needs a read

**Certain: the page and the code against each other.**

- **The morale checks outside fire are ten paths, each with its own gaps** (frame A, section 1): the rout Interdiction NMC has no Heat of Battle, no leadership, and no LLMC; the Bail Out NMC has no leadership, no wound, and no ELR, and a SMC Rider's Casualty Reduction throws (GamePlanner.Riders.cs:164 passes a null severity to RoutCalculator.cs:312); D5.311's crew is checked as unbroken when already broken; Mopping Up, a Sniper, Rally Fate, a PF firer's Casualty Reduction, and Failure to Rout lose a leader with no LLMC. The Fire calculator already resolves Heat of Battle, ELR, wounds, the LLMC cascade, prisoners, berserk units, heroes, and PRC.
- **A15.1 (p. 83) exempts** Unarmed units, Cavalry, PRC, Heroes, crews, and already berserk units from Heat of Battle. So the Bail Out NMC and D5.311's crew owe none, and pass 65's backlog rows that ask for it are closed by that reading.
- **Unarmed exists** as the condition `asl:unarmed` (GameTypes.cs:308); the Fire facts carry no Unarmed fact, so a freed Unarmed unit takes Heat of Battle and Replacement today.
- **Who sees is read in six ways** (frame B, section 1). `EnemyGoodOrderInLosWithin16` and `WithSeen` agree with A12.14 since pass 35's R35.5; `NearestGoodOrderEnemyInLos` counts hidden viewers; `Watching` counts hidden units and Passengers; the gain watchers count hidden units; each scans every unit with a `state.Location` read per call.
- **The view reads no LOS.** `GameView.Of` (Units) shows an unconcealed enemy stack in full whatever the LOS. Before play it already shows a stack's top counter and counts the rest as `SealedUninspected` (A2.9, R23.3). Units references Maps and Rules; the LOS map lives in `GamePlanner`. No planner proposal builds a view; the page, the library, Replay, `UnitNames`, and the sweep do.
- **The replay digest hashes states only;** the text list is a scan of source literals. So 70.10 changes only the sweep's view lines, and only if the sweep builds the view through the new read.
- **CC with a Gun's crew is refused in five places** (frame C, Q1), though no rule of A11 or C bars it; A11.13 (p. 72) eliminates a Gun of a unit eliminated in CC on a colored dr of 1 and a dr at or below the black Kill Number.
- **Infantry CC in a vehicle's Location is refused** (`VehiclePresentBar`, GamePlanner.CloseCombat.cs:425); only the vehicle sequence runs, and no Ambush is given where a vehicle stands.
- **A vehicle's PRC are probably marked in Melee** at the CCPh's end: `GameProjector.InMelee` groups by `state.Location`, which follows containment. A11.71 holds only the PRC of an Immobile vehicle. To be confirmed by a test at 3i.
- **Mopping Up is limited to a card's building** because Play reads no building from the map; only the Studio's `CardMaps.Building` flood fill finds one.
- **Withdrawal's bar counts enemy units the side cannot see,** in the refusal and on the page (Play.razor:4371, :4548, :4603).

**Needs a read at its task.**

- 3b: Fire's `Replace` and A19.12: whether a PRC with no lesser unit is Disrupted (A19.12 says PRC are never Disrupted); the Sniper's and Rally Fate's leader loss paths.
- 3d: a vehicle's weapon ranges from the armor reference, and how a positive DRM bars Interdiction (D3.44, A10.532's Gun CA change).
- 3e: the Concealment Loss/Gain Table's Cases J and K for a crew and a vehicle, rendered from the PDF (the text layer scrambles it).
- 3g: the Index's "Accessible" (R70.22 takes ADJACENT hexes the stack could enter until it is read).
- 3i: how the Infantry CC calculator's simultaneous rounds and the vehicle sequence share a Location.

**New facts in a record, planned now** (the gate computes `PROOF_SKIP` from the diff; these are for `SKIP_JSON` and the absent-safety of each):
- on a fire attack: `unarmed` and the PRC's leadership facts for each target (3b);
- on a Bail Out: its `drm` and `elr` (3b);
- on a vehicle target: a crew already broken (3b);
- on a CC resolution: the Guns each unit possesses (3h); on a vehicle CC: `leaderCreation` (3j); on an Ambush: the vehicle drm per side (3i);
- on the CCPh-ending `phase-changed`: the PRC held in Melee (3i);
- on a forced-back move step: the Bypass lane kept (3f);
- on a leaving Carrier crew's `instance-created`: the vehicle it left (3h).
New events: `morale-check-resolved`, `voluntary-break`, `units-searched`, and the owner's removal of a "?" (which may reuse `concealment-lost`; see R70.17).

## 5. What the frame found outside the rows, and corrections

1. **A crash:** a SMC Rider's Casualty Reduction in a Bail Out throws (GamePlanner.Riders.cs:164, a null severity). Repaired in 3b.
2. **Stale inventory notes,** corrected in the documents step: A12.1 and A12.14 (movement, advance, and Rally read a Good Order enemy since R35.5); A10.5 (rout by level is built since 2026-10-08); A12.13 (built in pass 60, R60.9); A12.15 (a routing unit's entry is built in pass 35, task 35.4).
3. **`NearestGoodOrderEnemyInLos` counts hidden viewers** (A12.34's reveal of a Hidden Gun), against R35.5. Repaired in 3a.
4. **Corrections to the prompt.** Main is at 25aa97fd (pull request 39), not 7a734fc3; the user said PR 39 merged after the prompt was written. The user's Studio was not running on 5178 at the start. A third worktree, `.claude/worktrees/replay-reenact-scope-9c9f9a`, exists beside the two Codex worktrees; it touches nothing of this pass.

## 6. The frame's questions, each with its ruling

**F1. Is the pass kept whole?** The agents' estimates sum to 33:05 of building against the plan's 7:20. Pass 65 built at about half its frame's estimate.
- **R70.1:** the pass stays one pass, as R65.1 kept pass 65. The increments run in the table's order; whatever a session's end leaves out is the last increment's (3j), and it goes to the backlog with its rows' pass.

**F2. What leaves the pass?**
- **R70.2:** the moves of section 2, each with a backlog row in the documents step. CC against a wreck goes with the rest of D10.1 to pass 110: it needs a wreck's own CC sequence (no PAATC, no Ambush, only a burning result), which is D10's, not A11's.

**F3. Morale outside fire: one member or several?**
- **R70.3:** one Rules member, `ScenarioA1MoraleCheck`, built on the Fire calculator: an internal check-only branch of `ScenarioA1FireCalculator` runs the existing resolution of a check on its subjects and then the leader-loss cascade, with no IFT roll. Every MC, NMC, PTC, LLMC, and LLTC made outside the Fire and Rally packages calls it: the rout Interdiction NMC, the Bail Out NMC, D5.311's crew attack, and the LLMC after a leader lost to Mopping Up, a Sniper, Rally Fate, a PF firer's Casualty Reduction, Failure to Rout, or an Interdiction. Its facts are the Fire calculator's (`FireTarget` with the new optional facts, the rolls, the choices, the side's ELR), so Heat of Battle, ELR, wounds, and the cascade are decided once. The OVR NTC, the Deployment NTC, and PAATC are Task Checks with no cascade of their own and keep their paths.
- **R70.4:** a new event `morale-check-resolved` carries the facts and the verdict of a check made outside an attack, as fire records do; its effects are written through the Fire planner's own effect events. A check appended after an existing record (Heat of Battle after an Interdiction NMC's Original 2, every LLMC outside fire) is new, so it needs no gating fact: an older game holds none and replays as it did.
- **R70.5:** a Heat of Battle that rallies a routing unit or makes it berserk ends its rout in the Location where it was interdicted.
- **R70.6:** only the Voluntary Rout leader adds his leadership DRM to an Interdiction NMC (A10.711); a leader already in the hex a unit routs into gives none.

**F4. Unarmed and PRC under fire.**
- **R70.7:** a new optional fact `unarmed` on each target of a fire attack and a morale check, read by Heat of Battle's subject (A15.1) and by ELR immunity (A19.11). Absent means as before, so an older attack resolves as it did.
- **R70.8:** PRC leadership and LLMC in Fire (D6.651, A10.2): `LeaderReaches` is read in the check both ways; a Passenger leader's loss checks fellow Passengers of his vehicle only; Riders neither take nor cause LLMC. The facts are new optional ones on the PRC entries, so an older attack resolves as it did. Heat of Battle for PRC stays none (A15.1).
- **R70.9:** the Bail Out NMC takes a Rider leader's DRM (A10.7's EXC), Wound Severity for a SMC, and ELR Replacement, never Disruption (A19.12), through R70.3's member. Its `drm` and `elr` are recorded on `rider-bailed-out`; the projector's check compares them only when present. The pass 65 backlog row's Heat of Battle is closed as not owed (A15.1).
- **R70.10:** D5.311's attack on a crew already broken eliminates it by Casualty Reduction; a recorded fact on the vehicle target says the crew was broken, absent meaning as before. Its Heat of Battle, ELR, and leadership are not owed (A15.1, A19.11, D3.44).

**F5. Voluntary Break and Voluntary Rout.**
- **R70.11:** Voluntary Break is an action of the RtPh's start, before any rout step of the phase, by the owner of units (even pinned) in the LOS and Normal Range of an armed, unbroken Known enemy ground unit or ADJACENT to any unbroken enemy ground unit, refused when breaking would Reduce or eliminate the unit at once and for heroes, berserk units, and Unarmed units (A10.3). It writes `voluntary-break`, then the units broken and under DM (A10.62). A leader's Voluntary Break causes a LLTC (A10.2, p. 65: "Should an unbroken leader break").
- **R70.12:** Voluntary Rout is a `withLeader` argument on the rout action: a non-berserk, non-pinned, unbroken leader stacked with the broken unit before it routs moves with it, takes no NMC, adds his DRM to its Interdiction NMC (recorded in `RoutInterdicted.Drm` with the leader named), and is eliminated if it fails, which causes an LLMC through R70.3.

**F6. Rout (70.2).**
- **R70.13:** a routing unit may ignore the hexes of the building it starts in when choosing its destination (A10.51's EXC, pp. 66 to 67). (Its citation corrected at the documents step: the frame wrote "A10.532's EXC, p. 67"; the 3d agent read on the page that the exception is A10.51's, which runs from p. 66 to the top of p. 67, and the code cites A10.51. Section 11.1.)
- **R70.14:** a vehicle and a manned Gun Interdict within their Normal Range (the longest of their weapons, at most 16) when no positive DRM would apply to their attack, and they enter must-rout and RtPh DM as "unit/its-SW/Gun" (A10.5). Interdiction is decided at plan time and recorded, so no record fact is needed.
- **R70.15:** Fanaticism by SSR is a `game-started` special rule `fanatic:<side>` (optionally `:<group>`), setting `asl:fanatic` at setup; absent-safe.

**F7. Who sees (70.3).**
- **R70.16:** one Rules member, `ScenarioA1Sight`, with three purposes: Loss (Good Order, not a Dummy, not hidden, not a Passenger, not an empty vehicle, A12.14), GainDenial (unbroken, Dummies included, not hidden, not an empty vehicle, A12.122), and Inspection (Good Order ground units, A12.16). One Play read, the nearest seer's range by (side, purpose, Location), memoized per state in `PlayStepMemo`, serves movement's Unmask, the advance, Rally, a Gun's CA change, ordnance's crew reveal, fire's `WithSeen`, A12.34's reveal, Case H and vehicle movement, Opportunity Fire reveals, the OVR PAATC subjects, and the gain watchers. A Nondefinitive, null, or unsupported LOS read is taken as each caller takes it today (the frame does not change how the LOS is read, only who is a seer). Reveals are recorded events, so no older record changes; only new proposals do, as R35.5's did.
- **R70.17:** the actions that cost a "?" (A12.141, p. 78) add every built "other activity" a concealed unit engages in while in the LOS and within 16 hexes of a Good Order enemy ground unit (the Loss read): Deploy, Recombine, SMOKE grenades, a LLTC or LLMC, a PF or MOL check, a leadership modifier applied to anything but a PAATC (a MC, a Rally, a Search), Recover, Transfer, Dismantle, Scrounge, Hook Gun, and the Mopping Up declaration. A12.141's exceptions stay free: taking a PAATC or a Set DC NTC, dropping or repairing a SW or Gun, Spotting, a radio or field phone, and a leadership modifier applied to another concealed unit's PAATC. (The frame first wrote that a check costs none; A12.141 names LLTC/LLMC among its examples. Corrected on the page before any build, the lesson of pass 60.)

**F8. The Right of Inspection (70.10).**
- **R70.18:** after setup closes, an enemy stack not concealed and out of the LOS of every Good Order ground unit of the viewing side (R70.16's Inspection purpose) is shown as a stack is before play: its top counter, the rest a sealed count, its equipment withheld. Play gives the set of such Locations per (state, viewer), memoized; `GameView.Of` takes it as it takes the set out of sight. One Play entry builds every view (the page, the library, Replay, `UnitNames`, the sweep). A null or unsupported LOS counts as in LOS, so it shows as today. Events and records are unchanged: A12.16 allows verification of actions.
- **R70.19:** the change applies to every game, with no marker on the record. The view is derived and never recorded, A12.16 is a gap the plan says to build (plan, the withdrawn ruling), and a marker would keep the fault in the user's games. The gate's sweep builds the view through the new entry, and each differing view line is named as 70.10's.

- **R70.40** (added at 3h, after a read-only agent read A11 in full): a side's units in the same Location as an enemy stack inspect it whatever their state (held in Melee, broken, pinned); concealed and hidden enemy units stay as they are. A.7 (p. 43) leaves units held in Melee out of Good Order, so a literal A12.16 would hide the stack a side is fighting; but CC cannot be played without knowing it: A11.12 (p. 72) lets units attack "any unit or combination of units in the same Location", A11.14 and its example stack leaders against the opponent's exact units, A11.19 (p. 73) has even a concealed unit reveal its Strength Factor before declarations, and A11.15 makes every unit in Melee unconcealed. A12.16 is written for viewers at a distance.

**F9. The owner's choices on a "?" (70.4).**
- **R70.20:** voluntary removal of a "?" is an action of the owner at any time in his own or the enemy's phase: it writes `concealment-lost` for the units; a Dummy so removed is eliminated; a hidden unit is placed and revealed in full (A12.31). The Sniper exception is noted and not built (no Sniper state to read).
- **R70.21:** the viewer's option not to force a loss (A12.14) is built where every Loss seer of the viewing side within 16 is itself concealed: a pending choice to that side at the one read of R70.16, its brief reveal shown to the moving side only. If its resumes in the move, advance, and fire paths cannot all be built inside 3e's estimate, R31d.2's deviation stays and the rest goes to the backlog.

**F10. 70.5.**
- **R70.22:** an Emplaced Gun's crew and a vehicle gain "?" under the Concealment Table's Cases J and K, read from a render of the table; A12.122's +1 Lax, -1 Stealthy, and a possessed Gun's US# apply. A vehicle's Concealment Terrain gains the woods-, orchard-, brush-, and grain-road hexes at setup only (A12.2). Units entering as reinforcements may enter concealed where the card's OB gives "?" (A12.12), lifting R20.5's refusal. HIP is allowed for vehicles by SSR (A12.3), and an Emplaced Gun may set up hidden out of Concealment Terrain when out of every enemy LOS, revealed when that changes (A12.34), read through R70.16's memo after each step and at each phase.

- **R70.35** (added at 3e, 2026-10-25, under the same word): R70.22's "units entering as reinforcements may enter concealed where the card's OB gives "?"" is too narrow. A12.12 (p. 77, read on the page): "Thereafter, a "?" may be placed on top of any non-concealed Good Order unit/stack before it enters the mapboard for the first time." So any non-concealed Good Order reinforcement stack may take one non-OB "?" (one per stack, never on top of another, A12.12's EXC) before it first enters; OB-designated "?" and Dummies stay as a card gives them (no card gives an entering group either today).

- **R70.36** (added at 3e): a hidden vehicle takes one squad-equivalent of a side's SSR HIP allowance. A12.3 (p. 80) gives the allowance in "squad-capacity" and prices no vehicle; a card whose SSR names the units it hides is read as it names them.

**F11. Bypass and detection (70.8).**
- **R70.23:** a Minimum Move into a concealed enemy's Location is no longer refused: the normal forced back applies, all MF spent. A stack forced back from a Bypass exit keeps its lane (a fact on the step, absent meaning as before) and reveals every concealed unit in the Location. A Bypass member broken or pinned with concealed enemy units in the Location reveals them. Attempting to occupy an obstacle that holds concealed units reveals them and forces the stack back into its lane, MF spent. A firer in a Bypassed hex may fire TPBF at a unit in Bypass there. A vehicle ending its MPh in Bypass of a Location with concealed enemy units reveals them unless it is "broken" (no crew and no PRC, A12.1); hidden units stay hidden (A12.42 names concealed ones).

- **R70.37** (added at 3f): lifting the refusal of TPBF inside a Bypassed hex does not make A8.312's Mandatory Fire owed against a stack entering Bypass of a Location that holds units of the other side: A12.151 says those units "may" use TPBF, and forcing it would reveal concealed units. `ScenarioA1MandatoryFire.Owed` owes nothing against a stack in Bypass, as before the pass.
- **R70.38** (added at 3f): a stack forced back by a Minimum Move is neither pinned nor CX (A4.134, p. 48, pins and CXes units that "entered a hex by Minimum Move"; a unit forced back entered none).

**F12. Searching and Mopping Up (70.9).**
- **R70.24:** Searching is an action of the moving stack as it ends its move: one more MF in its hex, a Search dr with A12.152's drm, the hexes named in the ATTACKER's order of preference before the dr (the same choice he would make after it), and the first (Accessible count less the Final dr) searched. Accessible hexes are, until the Index is read, its own hex and the ADJACENT hexes the stack could enter. The searchers are TI for the rest of the Player Turn. Concealed enemy units revealed lose "?"; hidden ones are placed beneath "?"; Dummies are removed. Search Casualties (A12.154) roll at once, one dr per Search dr where a Searched Location holds an armed Good Order DEFENDER unit, reusing Mopping Up's Casualty members. What each side learns is public: the dr, the hexes, and every reveal.
- **R70.25:** Mopping Up applies to any multi-hex or multi-level building of the map. Play finds a building's hexes by the Studio's flood fill moved into Play, memoized per board handle; a building a card names keeps the card's id, so older records keep theirs, and any other takes "board:lowest hex". Control folds for any building. One enumerator of the occupied Locations on `ObjectIndex` serves Searching and Mopping Up: one pass over the occupied stacks, never a lookup per Location.

- **R70.39** (added at 3g): the Search dr, its drm, and the hexes searched are public, though the drm shows a concealed searching stack's HS-equivalents and whether a leader leads it: A12.152 makes the dr in the open with its listed drm, and a leadership modifier applied to the Search costs the searchers' "?" (A12.141, R70.17). Accessible hexes stay R70.24's reading: the text layer has no Index definition of "Accessible".

**F13. Withdrawal and a Gun's crew (70.6).**
- **R70.26:** a withdrawal is barred only by a Known enemy unit, read from the side's view. Into a concealed (not Dummy) enemy's Location the withdrawer is eliminated, a Random Selection of the enemy units there loses "?", and hidden units are placed (A11.21, p. 73, with its example); into a Dummy-only Location the Dummies are removed. A unit above its IPC may withdraw by dropping SW, which stay unpossessed in the Location.
- **R70.27:** a Gun's crew is advanced against and fights in CC with its inherent FP (A1.123); the Gun is never used in CC (A11.13), and each Gun of a unit eliminated in CC is eliminated on a colored dr of 1 followed by a dr at or below the black Kill Number, otherwise left unpossessed. The five refusals are lifted. The Guns each unit possesses are a recorded fact of the CC resolution; absent, the Gun loss is not rolled, as before.
- **R70.28:** a Carrier HS recombines with no leader and no TC (A1.32, p. 45, names "Carrier HS"): the leaving crew's `instance-created` records the vehicle it left, absent meaning an ordinary HS.

**F14. Plumbing.**
- **R70.29:** `GameProjector.CheckInvariants` (UNIT-STATE-011, 012, 014) are plumbing, not rules: they guard the object graph and decide no outcome; A20.5's decisions are already in Rules. They stay in Units, and the backlog row is closed with this ruling. Their 12% of a replay (pass 31d) stays a performance item.

**F15. Close Combat with a vehicle (70.7).**
- **R70.30:** in a Location with a vehicle, Infantry against Infantry CC joins the vehicle sequence (A11.31, the H3 example): a sequential round, one attack by the side named next, no simultaneous reply; each unit attacks once per CCPh, being attacked by Infantry and by a vehicle counted apart (A11.12's EXC). An Ambush there takes the vehicle drm (+2 Vehicle, +1 for each BU or Stunned AFV, A11.4), the ambusher resolving all its attacks first (A11.34), and the -1 and +1 in vehicle CC both ways (A11.51). The Ambush's vehicle drm is a recorded fact, null where no vehicle stands.
- **R70.31:** PRC in CC are wired from `ScenarioA1PassengerActionRules.CloseCombat`: a Rider attacks apart with +1 and defends with -1 (A11.611); a CE halftrack's Passengers join the vehicle's attack (A11.62); the rest are offered nothing. Only the PRC of an Immobile vehicle are held in Melee at the CCPh's end (A11.71); the CCPh-ending record carries the PRC so held, absent meaning as before.
- **R70.32:** the CC counter on an unload, a Bail Out, or TPBF into an enemy Location (D6.5, D6.24, A7.211) is no stored condition: units are held in Melee only at the CCPh's end already. The record line says the units are in an enemy Location and not held in Melee, citing the rule.
- **R70.33:** Street Fighting (A11.8): a vehicle in a road hex ADJACENT to building hexes on both sides of the road may be attacked in CC by Infantry advancing from the ground level of those buildings with -1, the vehicle's attack on them +1, not with an actual Ambush; afterwards they return to the Location they came from or stay, unless broken or pinned. The building flank is a Maps read; where the unit came from is a state fact the projector sets from the advance. Its Reaction Fire (D7.211) stays with pass 105, Bypass with pass 100.
- **R70.34:** A11.52's capture of Passengers: ordinary Infantry CC at the Passengers of an unarmored stationary vehicle, -2 on attacks at them with the capture's +1 and +2 on their attacks at the captors; when all are captured and no enemy Personnel remain, the vehicle is captured (`vehicle-captured`). A18.12 against a vehicle: a MMC's Original 2 in vehicle CC draws a Leader Creation dr, the created leader adding his leadership and +1 CCV only if the attack had no SMC; `leaderCreation` is taken from the record, absent meaning no roll. D5.42: a captor may enter the captured vehicle as its crew in the CCPh of its capture.

**F16. Decided at the reviews (2026-10-25, under the same word; the review files are in the pass's folder).**
- **R70.41:** the Ambush record's facts count each side's vehicles and BU or Stunned AFVs in the open: the Ambush dr is made with its listed drm (A11.4, p. 73), and A11.19 (p. 73) has a concealed unit in CC reveal its strength before declarations. The counterpart of R70.39.
- **R70.42:** a Search searches the hexes the player names, in his order, and then the other Accessible hexes in map order, up to the Accessible count less the Final dr. A12.152 (p. 79) lets the ATTACKER choose which hexes the dr leaves unsearched; searching more costs nothing (one Casualty dr per Search dr), so no hex is given up that the dr does not take. (R70.24's "the first of the named" left the unnamed ones unsearched; the table player's finding.)
- **R70.43:** the Voluntary Break's ADJACENT clause counts any Known unbroken enemy ground unit, armed or not (A10.41, p. 66: "ADJACENT to any unbroken enemy ground unit"; "armed" belongs to the LOS clause). A concealed or hidden enemy unit does not allow the break: a deviation, so a refusal tells nothing of a "?" (the table would verify by A12.16); the refusal cites this ruling, and the backlog keeps the rest.
- **R70.44:** a leader lost in a rout causes a LLMC of the units of lower Morale Level in the Location where he is lost, bystanders included. A10.2's (p. 65) exception names Defensive First Fire only; a rout is not DFF. R70.6's other way (a bystander leader adds no DRM to a routing unit's NMC) stands, since only A10.711's leader routs with the unit.
- **R70.45:** the Search Casualty dr (A12.154, p. 79) and Mopping Up's are always made when due; the DEFENDER's "may" is not offered, as R24.2 decided for Mopping Up. A deviation, with a backlog row.
- **R70.46:** in vehicle CC, a leader created by a MMC's Original 2 adds his leadership in place of a combining leader's; when a SMC that is no leader (a hero) combines, he adds none; with no SMC he adds his leadership and the +1 CCV (R29.12's reading of A18.12, p. 85, with A11.5's "No more than two units"). The referee's finding; the three cases worded at fix Round C.
- **R70.47:** an Unarmed unit that fails a MC suffers Casualty Reduction instead of breaking (A20.54, p. 88: "A prisoner/unarmed unit which fails a MC is not broken; it suffers Casualty Reduction instead"; A10.3's EXC), where the attack or check carries R70.7's `unarmed` fact.

- **R70.48** (added at fix Round E): a Search's or a Mopping Up's Casualty dr is recorded with its drm (`casualtyDrm`); the record lines give the drm to the DEFENDER's and the adjudicator's views only, the ATTACKER's line the dr and its effect. For a Search the DEFENDER's units in a Searched hex are revealed anyway (A12.152, p. 79); for Mopping Up the drm may tell a Stealthy unit or a leader beneath a "?". The fact stays on a public record, since an owner-only record would need a new event type; a backlog row holds the rest.

## 7. Memory and performance

**The checklist** (the guidelines' section "Design and review checklist").

- **Work added per state:** R70.16's seer Locations per (side, purpose), built once from `ObjectIndex` (one pass over the units); R70.18's uninspected set per (state, viewer), one LOS read per pair of an enemy unconcealed stack Location and a seer Location, with an early exit at the first seer in LOS; R70.25's occupied-Location enumerator (built with the index, no extra pass) and the building flood fill per board handle (fixed for the map).
- **Per unit and per hex:** none outside the memo. The six who-sees reads stop rebuilding every unit's facts per call; Searching and Mopping Up read the occupied stacks once.
- **Per step and per render:** the view is built as today, once per (history, revision, viewer) on the page; the set it takes is the memo's.
- **Ownership and keys:** both memo stores are owned by `PlayStepMemo` (state lifetime, `ConditionalWeakTable`), keyed by side and purpose, and by viewer; read only; concurrent fills may compute equal answers twice, with no side effect. The building map is owned by the board handle's memo. Disclosure: the uninspected set is keyed by viewer; the seer reads by side.
- **LOS:** each LOS read goes through the planner's existing reads, which cache a (from, to) answer per map; the cold `LosCalculator.Check` is unmeasured and is the measurement's first item.

**The measurement** (stated before building, as the user asked on 2026-10-19).
- **Workload:** a copy of `dl-tractor-works-02` cut at revision 1316 (`perf_cut.py` of pass 65, under a new id; the user's game is not touched): the hand-over (German to Russian) and an "end the phase" proposal, three warm runs each; and the time to open the whole `dl-tractor-works-02` in Play from a cold Studio (the page's view and `UnitNames` over its 1,441 revisions, which R70.18 reaches).
- **Baseline:** the gate's `main-bin` build of 25aa97fd on port 6671, measured alone; then the pass's Studio on 6670, alone (the lesson of pass 65).
- **Expected:** the hand-over and the proposal level with the base (the who-sees reads get cheaper, the view adds a few cached LOS reads per state); the cold open at most 20% slower (one uninspected set per revision per viewer).
- **Stop threshold:** a warm median more than 20% over the base's, or a cold open more than 25% slower, stops the next increment until the cause is found by a trace.
- **When:** after 3a (the view and the seer memo) in the Studio first, before any costly test; again after 3g and at the whole-pass check.

**Build outputs outside the repository.** My Studio in `asl-gate-work/studio-6670` (registered by `build_studio.sh`); the gate's own (the base build, the proof tools, registered by `gate.sh`); the filtered test runs' `asl-gate-work/test-classes-bin/<project>` (registered by `run_test_classes.sh`); any strict build of a test project by an agent, registered as it is made. All are removed at the latest by the gate's `ci` stage; the last task confirms `outside_outputs.sh list` is empty.

## 8. Who sees, and the Right of Inspection by LOS: 70.3 and 70.10 (increment 3a)

Built on 2026-10-25, 12:17 to about 12:55, in one commit (fc150800) by a building agent to a brief (`brief_build.md` and `brief_3a.md` in the pass's folder of `code-builders/pass-history`), its diff read and its games played in the Studio by the session.

### 8.1 Departures from the frame, first

Each decided under the user's word of 2026-10-25; each is open to a veto.

- **R70.17 was reached only in part:** Deploy, Recombine, Transfer, Recover, Dismantle and assemble, Scrounge, Hook Gun, the Mopping Up declaration, and SMOKE grenades cost a "?" (a Rally, a leader's Rally DRM, and a Gun's CA change already did). A LLTC or LLMC, the PF and MOL checks, and a leadership modifier applied to a MC sit inside the Fire calculator, so they were given to 3b, whose member decides them.
- **The gain watchers read the seer table, not the nearest-seer store:** Case K counts an unbroken enemy within 16 hexes whatever the LOS (A12.122), and a watcher's LOS at night depends on its own NVR. Right.
- **The agent's own readings, accepted:** an "empty vehicle" is an Abandoned one with no crew inside and no PRC (an unarmed truck's Inherent Driver left as before); a seer in the same Location counts at range 0 with no LOS read, for every purpose; Inspection seers include PRC and are the viewer's own units; for Inspection a blocked read (Nondefinitive included) is out of LOS, and a null or unsupported read is in LOS (R70.18).
- **Each caller's old condition differs from its purpose's, in new proposals only:** A12.34's reveal of a Hidden Gun no longer counts hidden viewers (section 5, item 3); Case H, vehicle moves, Motion, Opportunity Fire, and the OVR PAATC no longer count hidden units or PRC, and now count a Recalled vehicle and an Abandoned one carrying PRC; a gain is no longer denied by hidden units or empty vehicles. Reveals are recorded events, so no older record changes.
- **`CaseRead` (Units, no planner) still showed stacks in full,** and `UnitNames` kept "a concealed unit" for an uninspected counter. Both were fixed at Round A (section 18.2).
- **Two Rules members became test-only** (`ScenarioA1VehicleSightRules.Watching`, `WithinRange`), with the older who-sees members of the movement calculator; Round B deleted them and pointed their tests at `ScenarioA1Sight`.
- **Found and fixed by the session, outside the increment:** the test-game library was broken on main. `game_file.py` took its `game-started` template from the live game `p35-rst-cc`, which pull request 38 retired, so every games script failed. The payload is kept beside the library (`code-builders/test-games/game_started_template.json`).

### 8.2 What is built

`ScenarioA1Sight` (Rules) holds R70.16's three purposes, `Sees`, `SeerLocations`, `NearestSeer`, `Inspectable`, and `ActionLosesConcealment`. `GamePlanner.Sight.cs` builds the sight table once per state from `ObjectIndex` and answers `NearestSeer`, `WithSeen`, `Uninspected(state, viewer)`, and `ViewOf`; three memo stores in `PlayStepMemo` hold the seer Locations by (side, purpose), the nearest seer by (side, purpose, reading, Location), and the uninspected set by viewer. Every caller R70.16 names reads it: movement's Unmask, the advance, Rally, a Gun's CA change, ordnance's crew reveal, fire's `WithSeen`, A12.34's reveal, Case H and vehicle movement, Opportunity Fire, the OVR PAATC subjects, and the gain watchers. `GameView.Of(..., uninspected)` shows an uninspected stack as a stack before play, with `SealedPresence.OutOfLos` (in the fragment key and `UnitLabels` too); every view of the page, the library, `LivePlay`, and `UnitNames` goes through `ViewOf` (R70.18). The proof tool binds `GamePlanner.ViewOf` by reflection and falls back where a build lacks it. The actions of R70.17 add the loss of "?" through `AddActionConcealmentLoss`, with a proposal line naming the units and A12.141.

**New in a record:** nothing. The view is derived (R70.19), and the losses of "?" an action causes are `concealment-lost` events of the kind every game holds. **An older record** replays as it did; only its view lines differ, which are 70.10's.

### 8.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-inspect-out` | The Russian view of a German stack beyond the hill: the Units tab reads "4-6-7 squad in [O10]" and one row "? German not inspectable: out of the LOS of this side's Good Order units (A12.16) [O10]"; the [P10] stack, seen from [Q8], in full; no LMG listed. Played. |
| `p70-inspect-seen` | The same stacks seen from [P8]: both in full. Played. |
| `p70-transfer-seen` | The German RPh: transferring the LMG from G1 to "[O10]: 2-4-7 half-squad G2" proposes that both "lose their "?" by this action, in the LOS of a Good Order enemy ground unit within 16 hexes (A12.141)"; the file holds `equipment-transferred` then `concealment-lost` for g1 and g2. Played. |
| `p70-transfer-unseen`, `p70-inspect-before` | The control (the transfer with no loss line, cancelled); the view before setup closes. |

Rules: `EachPurposeCountsItsOwnSeers`, `TheNearestSeerIsReadWithinAndBeyondSixteen`, `AStackIsInspectedOnlyAfterSetupAndInTheLosOfASeer`, `AnActionCostsAConcealedUnitsQuestionMarkInTheLosOfASeerWithinSixteen`, `AStackOutOfInspectionShowsItsTopAndCountsTheRest`. Play: `AnUnconcealedGermanStackOutOfEveryRussianGoodOrderUnitsLosShowsItsTopOnly`, `TheStackIsInspectedInFullOnceARussianUnitSeesIt`, `AHiddenGunThatFiresIsNotRevealedByAHiddenEnemy`, `AConcealedMoverIsNotRevealedByAHiddenUnit`, `ATransferCostsBothUnitsTheirQuestionMarkInTheLosOfAGoodOrderEnemy`. Page: `PlayPagePass70Tests.AStackBeyondTheHillShowsTheRussianItsTopAndACountNamedForTheRule`.

**The measurement** (section 7) began here, on `p70-perf-tw` (`dl-tractor-works-02` cut at revision 1316), each Studio alone: the base (the gate's `main-bin` build of 25aa97fd on port 6671, 12:14) hand-over 487 (cold), 348, 284, 393, 289 ms, the refused "end the phase" proposal 60, 127, 110, 85, 111 ms; after 3a, 341, 292, 274, 291 ms and 183 (the first), 73, 59, 59, 96 ms. Level. The first hand-over into the whole `dl-tractor-works-02` from a fresh process: pass 1,783 ms against the base's 1,485 (20% slower, inside the 25% threshold); the warm ones faster (390 and 302 against 448 and 349). `tool_check_3a.sh` (the proof tool changed): a sweep of `dl-tractor-works-02` and `p65-crew-gun` from each build, 1,606 lines each, no ERROR, UNREADABLE, or NOSTATE, 2 lines differing, both the swept game's GameView: 70.10's.

## 9. One Morale Check member: 70.1, part I (increment 3b)

Built on 2026-10-25, 13:04 to about 14:16, in one commit (65ebcec8) by a building agent to a brief (`brief_3b.md`), rulings R70.3 to R70.10 and the A12.141 checks 3a left, its diff read and its games played in the Studio by the session.

### 9.1 Departures from the frame, first

- **A19.12 verified and a fault found:** Fire's `Replace` Disrupted a PRC with no lesser unit, though A19.12 says PRC are never Disrupted. Fixed behind the PRC facts (`prc-not-disrupted`), so an older attack resolves as it did.
- **A pre-existing fault fixed in `GameGate.EffectHolds`:** a vehicle step whose last event was a Bail Out's conditions change failed its readback (`play.effect-readback-failed`); a pinned terrain Bail Out had always failed. Outside the task, small, found by its tests.
- **Battle Hardening is always taken** in a check outside fire: A15.3 allows a refusal, and the member's checks have no choice resume. Backlog row (72.1).
- **R70.8 is narrower in Fire than in the member:** a CE leader in an unmoved halftrack leading the Location's units on foot works in the member, not in Fire, since the PRC attack resolves after the Location's checks. Backlog row (72.1).
- **The PF and MOL checks cost no "?" through the member:** a seen firer already loses its "?" by firing (R31d.2). Accepted: the check is part of the firing.
- **A side with no declared ELR takes no LLMC through the member;** its Bail Out keeps pass 65's check with the wound dr drawn. Backlog row (72.1): a LLMC does not need an ELR.
- **No `unarmed` fact on PRC entries:** an Unarmed unit cannot board here.
- **One older test's dice changed as intended:** `BacklogPass35Tests.ACommissarRoutsThroughInterdictionRatherThanSurrender` (its Original 2 now draws Heat of Battle).
- **Found in the Studio:** the Records tab's "Rally, rout, and other actions" list left out the LLMC line the Latest line had; fixed at Round E.

### 9.2 What is built

`ScenarioA1MoraleCheck` (Rules) on a check-only branch of the Fire calculator (`ScenarioA1FireCalculator.ResolveCheck`, `Resolution.RunCheck`): Fire's own resolution of a check on its subjects, then the leader-loss cascade, with no IFT roll (R70.3). The event `morale-check-resolved` with its verifier (R70.4), and `GamePlanner.MoraleChecks.cs` (`AddMoraleCheck`, `AddLeaderLossChecks`, `LeaderLossDue`). Its callers: the Interdiction NMC (Heat of Battle on an Original 2, R70.5's end of the rout, the LLMC of a routing leader), Failure to Rout, Mopping Up, a Sniper, Rally Fate, and the PF or ATR firer (LLMC, or LLTC). The Bail Out NMC through the member, with a Rider leader's DRM, Wound Severity for a SMC, and ELR Replacement, never Disruption (R70.9); the SMC Rider's crash of section 5, item 1, fixed. A15.1's exemptions and A19.11's immunity read the new `unarmed` fact (R70.7); `LeaderReaches` is read both ways for PRC, a Passenger leader's loss checks his own vehicle's Passengers only, and Riders neither take nor cause LLMC (R70.8). D5.311's attack on a crew already broken eliminates it by Casualty Reduction (R70.10).

**New in a record:** `unarmed`, `aboard`, and `seenByGoodOrderEnemy` on fire targets; `vehicleMoving` and `ceInUnmovedHalftrack` on PRC entries; `crewBroken` on a vehicle target; `drm` and `elr` on `rider-bailed-out`; the event `morale-check-resolved` (`units`, `location`, `cause`, `rolls`, `facts`, the member's resolution). **An older record** carries none: its attack resolves as it did (absent means as before), its Bail Out is compared on `drm` and `elr` only when present, and a check appended after an existing record (Heat of Battle after an Interdiction NMC's Original 2, every LLMC outside fire) is new, so an older game holds none and replays as it did (R70.4).

### 9.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-llmc-failure-to-rout` | The German RtPh, dice 4 3: "end the phase" warns that the 8-1 leader G1 is eliminated for Failure to Rout and that his loss calls for a LLMC (worded at Round A: "may call for a LLMC ... rolled at the commit if any is due"); committed: "[Q10]: 4-6-7 squad G1 takes LLMC after the loss of 8-1 leader G1 to Failure to Rout (A10.5): LLMC 4, 3 + 1 (leadership reversed, 8-1 leader G1) = 8 against 7: failed, broken (A10.2)". The file holds `instance-eliminated`, `dice-rolled` (fire-leader-loss), `morale-check-resolved` (cause failure-to-rout), `conditions-changed`. Played. |
| `p70-interdiction-hob` | The German RtPh, dice 1 1 then 4 4: the Interdiction NMC's Original 2 calls for Heat of Battle (A15.1), Final DR 9, berserk (A15.4), and the rout ends in [P10] (R70.5). Played. |

Rules: `AnInterdictionNmcsOriginalTwoCallsForHeatOfBattle`, `AnUnarmedUnitTakesNoHeatOfBattleAndNoReplacement`, `ALlmcTakesTheReversedDrmAndRidersAndOtherVehiclesPassengersTakeNone`, `AnUnbrokenLeaderWhoBreaksCausesALltc`, `APassengerLeaderAidsAndChecksOnlyHisVehiclesPassengers`, `AFirePassengerLeaderAidsHisFellowPassengersWhenTheRecordCarriesThePassFacts`, `ASmcsCasualtyReductionTakesItsWoundSeverityDr`, `ABailOutTakesARiderLeadersDrmAndElrReplacementNeverDisruption`, `ABrokenUnprotectedCrewThatFailsAMcIsEliminated`. Play: `AnInterdictionNmcsOriginalTwoDrawsHeatOfBattleAndABerserkUnitRoutsNoFurther`, `ALeaderLostToInterdictionCausesALlmcOfTheUnitsInHisLocation`, `ALeaderEliminatedForFailureToRoutCausesALlmcBeforeThePhaseEnds`, `ASmcRidersBailOutTakesItsWoundSeverityDrAndAnOlderRecordReplaysAsItWas`, `ARiderLeadersDrmReachesTheBailOutNmcOfHisVehiclesRiders`. Page: `TheRecordsSayALlmcAfterALeaderLostToFailureToRout`. `tool_check_3b.sh`: `dl-tractor-works-02` and every p65 game swept on both sides, 6,620 lines each, no ERROR, UNREADABLE, or NOSTATE; 142 differ, 3a's views and fire plans whose hash includes the new null members (the computed `PROOF_SKIP` takes the new properties; the JSON keys are in `SKIP_JSON`).

## 10. Voluntary Break and Voluntary Rout: 70.1, part II (increment 3c)

Built on 2026-10-25, 14:20 to about 14:53, in one commit (4189b73e) by a building agent to a brief (`brief_3c.md`), rulings R70.11 and R70.12, its diff read and its games played in the Studio by the session.

### 10.1 Departures from the frame, first

- **A12.14 read on its page (p. 77) and the brief corrected:** a concealed unit that breaks loses its "?" "regardless of the range ... or even if out of the LOS of all enemy units", so a Voluntary Break always strips it. The brief had asked for the Loss read.
- **Breaking clears the pin** (A7.83; the p. 69 example's pinned HS routs).
- **The agent's own refusals, accepted:** a unit in Melee (A10.5 bars rout from Melee); a unit aboard (backlog row, 72.1); prisoners as Unarmed. ADJACENT counted Known enemy units only, so a refusal tells nothing of a "?" (ruling R70.43 since the reviews). Vehicles allowed a break only when ADJACENT until 3d gave them a Normal Range.
- **The Voluntary Rout:** the MF limit is the lower of the two; the leader's own portage is not checked (backlog row, 72.1); a concealed leader loses his "?" when he joins in a Good Order enemy's LOS within 16, and at an Interdiction that names him.
- **A fix beside the leader:** after a Reduction, if the LLMC eliminates, rallies, berserks, or surrenders the HS, its rout ends (R70.5).

### 10.2 What is built

`ScenarioA1VoluntaryRout` (Rules: `Threat`, `BreakPhaseBar`, `BreakUnitBar`, `LeaderBar`, `LeaderDrm`, `LeaderEliminated`, the texts; since Round B also `BreakEffects`, `RoutLimit`, `InterdictionRevealsLeader`) and `GamePlanner.VoluntaryRout.cs` (`PlanVoluntaryBreak`, `VoluntaryBreakBar`, `VoluntaryRoutLeaders`). The action `asl.game.voluntary-break` writes the event `voluntary-break`, then the units broken under DM (A10.62); a leader's break calls for a LLTC through the member (R70.11). The rout action takes `withLeader` (R70.12): the leader moves with the broken unit, takes no NMC, adds his DRM to its Interdiction NMC, and is eliminated if it fails, which calls for a LLMC. The Rout panel's Voluntary Break list (`input.voluntary-break-unit`, `#propose-voluntary-break`) and `#rout-with-leader`; the Icon Lab's coverage file regenerated.

**New in a record:** the event `voluntary-break` (`units`); `with` on `rout-stepped`; `leader` on `rout-interdicted`, whose `drm` carries his DRM. **An older record** names no leader: its rout steps replay as before the pass (`ARoutWithNoLeaderIsRecordedAndReplaysAsBeforeThePass`).

### 10.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-voluntary-break` | The German RtPh: the list offers the 4-6-7 and the pinned 2-4-7 of [O10]; ticking the HS proposes "2-4-7 half-squad G2 breaks voluntarily at the start of the RtPh, under DM, so as to rout (A10.41, A10.62)" and "... loses its pin by breaking (A7.83)"; committed. Played. |
| `p70-voluntary-rout` | The German RtPh, dice 5 4 then 4 3: the leader choice offers "[Q10]: 9-1 leader G1"; committed: "NMC DR 9 - 1 (leadership, 9-1 leader G1, A10.711) = 8 against broken morale 7: reduced (A10.53); 9-1 leader G1, routing with it, is eliminated (A10.711)", then the HS's LLMC, failed, eliminated (A10.2). As the rule and its example give. Played. |
| `p70-voluntary-break-leader` | A leader's Voluntary Break and its LLTC (played at the whole-pass check, section 18.4). |

Rules: `AUnitMayBreakInTheLosAndNormalRangeOfAKnownArmedUnbrokenEnemyOrAdjacentToOne`, `AVoluntaryBreakIsRefusedWhereBreakingWouldReduceTheUnitOrItNeverBreaksAndAfterARoutStep`, `AVoluntaryRoutLeaderAddsHisDrmToTheNmcAndIsEliminatedWhenItFails`, `ALeadersLossOutsideFireCallsOnTheOtherUnitsOfHisSideThereAndTheVoluntaryRoutLeadersLossSaysA10711`. Play: `APinnedHalfSquadBreaksVoluntarilyAndRoutsAsInTheExampleOnPage69`, `AUnitOutOfTheLosOfEveryKnownEnemyMayNotBreakVoluntarily`, `ALeadersVoluntaryBreakCallsForALltcOfTheSquadWithHim`, `ALeaderRoutsWithABrokenSquadAndHisDrmPassesItsInterdiction`, `ALeaderRoutingWithASquadThatFailsItsInterdictionIsEliminatedAndTheHalfSquadTakesTheLlmc`, `ARoutWithNoLeaderIsRecordedAndReplaysAsBeforeThePass`. Page: `TheRoutPanelOffersAVoluntaryBreakByHexAndALeaderToRoutWith`.

## 11. Rout from a building, Interdiction by vehicles and Guns, Fanaticism by SSR: 70.2 (increment 3d)

Built on 2026-10-25, 14:57 to about 15:24, in one commit (cb3110fb) by a building agent to a brief (`brief_3d.md`), rulings R70.13 to R70.15, its diff read and its games played in the Studio by the session.

### 11.1 Departures from the frame, first

- **R70.13's citation corrected on the page:** the exception is A10.51's (it runs from p. 66 to the top of p. 67), not A10.532's. The code cites A10.51; the ruling is corrected in place (section 6, F6).
- **Its building read was the rout's own until 3g's;** 3g replaced it with one read (section 14).
- **When a vehicle cannot Interdict:** Abandoned, Shocked, in Motion, or Stunned; its MA only with a CE crew, never RST or 1MT; an OT AFV's BMG unless CE. A plain MG as MA held to the VCA, the agent's choice. Accepted. A mortar's minimum range, a restricted AAMG, a VCA-only CMG, and Rear MGs are not read: backlog row (72.1).
- **Fanaticism applies to Personnel placed through `asl.game.setup` only;** units created later (reinforcements, Deployment) are not covered: backlog row (72.1).
- **One older test changed as intended:** `BacklogPass10Tests.ABrokenUnitUpstairsRoutsDownItsStairwell` (every cover is in its own building, so "any legal route" now).
- **The rout advice of `p70-rout-own-building` was not judged from the panel** and went to the referee; its words were untrue and were reworded at Round B (section 18.2).
- **Expected at the gate:** enemy AFVs and Gun crews now cause must-rout, RtPh DM, and Interdiction, so the sweep may show rout differences where an enemy vehicle sees a broken unit in Open Ground (R70.14).

### 11.2 What is built

Vehicles and manned Guns as Interdictors, and in must-rout and RtPh DM as "unit/its-SW/Gun" (R70.14): `ScenarioA1RoutCalculator.InterdictionReach`, `VehicleNormalRange`, `VehicleInterdiction`, `GunNormalRange`, `GunArcWeapon` (with `VehicleWeapons` and `GunWorks` since Round D), the reader's `Bearing`. The starting-building exception (R70.13) in `RoutTargets`, `RoutDestinationBar`, and the advice, with a building flood fill (`GamePlanner.BuildingHexes`, the join decided in Rules by `ScenarioA1RoutCalculator.BuildingGoesOn`). Fanaticism by SSR (R70.15): `fanatic:<side>` and `fanatic:<side>:<group>` on `game-started`, set at setup (`ScenarioA1GameStart.FanaticBySsr`), with the card's diagnostics. 3c's `Threat` reads the vehicles' Normal Range with no change.

**New in a record:** nothing. Interdiction is decided at plan time and recorded; the SSR token is a card's. **An older record** of a rout past an enemy AFV replays as it did (`AnOlderRecordedRoutPastAnEnemyAfvReplaysAsItDid`).

### 11.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-afv-interdiction` | The German RtPh: the panel says "[B10]: 4-6-7 squad G1 must rout: it is in Open Ground in the LOS and Normal Range of vehicle in [F10]" (R70.14); the agent's suggested route toward the AFV is refused, rightly (A10.51); routing by [B9], [B8] to [C8] (dice 3 3) commits "Interdiction of 4-6-7 squad G1 entering [B9] by vehicle in [F10]: NMC DR 6 against broken morale 7: passed (A10.53)". Played. |
| `p70-rout-own-building` | A broken 4-6-7 on level 1 of [M5] above an enemy squad. Its advice was reworded at Round B (section 18.2). |
| `p70-fanatic-ssr` | A start only: the condition is set by setup from a card carrying the SSR, and no card does. Asserted by its Play test, which sets up through the gate; not staged in the Studio. |

Rules: `AUnitInABuildingMayIgnoreItsOwnBuildingsHexesWhenChoosingWhereToRout`, `AVehicleInterdictsWithinItsMgsRangeAndCoveredArcAndNotWithAPositiveDrm`, `AGunThatMustChangeItsCaDoesNotInterdict`, `TheSsrsTokenSetsFanaticism`. Play: `ABrokenSquadInOpenGroundInRangeOfAnEnemyAfvMustRoutAndIsInterdictedByIt`, `AnOlderRecordedRoutPastAnEnemyAfvReplaysAsItDid`, `ABrokenUnitRoutsOutOfItsOwnBuildingPastItsOtherHex`, `AFanaticSsrMakesTheSidesPersonnelFanaticAsTheySetUp`.

## 12. The owner's choices on a "?", and concealment for crews and vehicles: 70.4 and 70.5 (increment 3e)

Built on 2026-10-25, 15:28 to about 16:13, in one commit (e7f60a75) by a building agent to a brief (`brief_3e.md`), rulings R70.20 to R70.22, sent back once to widen R70.22 under R70.35; its diff read and its games played in the Studio by the session.

### 12.1 Departures from the frame, first

- **R70.21 built for the move only** (by day, on a step that rolls nothing); the advance, fire, night, and rolling steps keep R31d.2's forced loss, as R70.21 allowed. Backlog row (72.1).
- **Entering concealed: R70.22 was too narrow.** The agent built it to the card's OB "?" and flagged it. A12.12 (p. 77) lets any non-concealed Good Order stack take a "?" before it first enters. **Ruling R70.35** added; the agent widened it (16:09 to about 16:13).
- **HIP for a vehicle:** A12.3 (p. 80) prices HIP in squad-capacity and names no vehicle. **Ruling R70.36:** one squad-equivalent.
- **The Concealment Table read from a render** (p. 107 at three times): an Emplaced Gun and a vehicle gain only out of every LOS, Case J in Concealment Terrain and Case K out of it.
- **The agent's readings, accepted:** a vehicle whose PRC are not in Good Order gains no "?"; PRC gain only with their vehicle; a crew with a non-Emplaced Gun gains nothing (backlog row, 72.1); a vehicle's gain reads R6.7's Concealment Terrain. A12.34's setup LOS is not checked: a Gun hidden out of Concealment Terrain is placed beneath "?" at the next step or phase that finds a Good Order enemy in LOS (backlog row, 72.1).
- **Two older tests changed as intended:** `BacklogPass23Tests.OnlyInfantrySetsUpHidden`, `BacklogPass26Tests.AGunCrewStacksAsASquad...`.
- **Seen by the agent, for the fixes:** `UnitSelectionList` and `UnitGrouping` showed no hex for a one-unit list (fixed at Round E); a vehicle in a non-road brush hex passes the setup "?" check, but R6.7 then treats it as out of Concealment Terrain.
- **The game `p70-hidden-gun` showed nothing:** the board viewer's LOS tab reads [L8] to [G7] "Blocked", so the game, not the code, was wrong. `p70-hidden-gun-seen` puts the Russian in [H8] ("Clear" to [G7]). The lesson (check every LOS pair a games script relies on) is in `brief_build.md`.

### 12.2 What is built

`ScenarioA1ConcealmentChoices` (Rules: the removal's bar and texts, `RemovalEliminates`, `RoadConcealmentHex`, `SetupConcealmentTerrain`, `EmplacedHipLost`, the option's texts, a verification check UNIT-STATE-050; since the fixes `PendingLossBar` and `SharesConcealment`). The Loss purpose in view (`SightPurpose.LossInView`), `SightSeerFacts.Concealed`, and `ScenarioA1Sight.ViewerMayDecline`. Gains for vehicles and an Emplaced Gun's crew with +1 Lax, -1 Stealthy, and a 5/8" US# (R70.22). Setup: the road-concealment hexes for vehicles, HIP for vehicles by SSR (R70.36), an Emplaced Gun hidden out of Concealment Terrain, and any reinforcement waiting off board under one non-OB "?" a stack, never on top of another (R70.35; `#place-concealed`). The action `asl.game.remove-concealment` (R70.20: `concealment-lost`; a Dummy eliminated; a hidden unit placed and revealed in full, A12.31), the page's "Give up "?"" list. The viewer's option (R70.21): `ChoicePending.ConcealmentLoss` to the viewing side only, the event `concealment-verified` to the moving side only, the option's buttons. `hidden-placed` after steps and phases for a hidden Gun once seen (A12.34).

**New in a record:** the event `concealment-verified` (`units`); the choice kind `concealment-loss` on `choice-pending` and `choice-made`, each visible to the viewer only. **An older record** holds neither; an older move replays with its forced loss, as R31d.2 had it.

### 12.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-viewer-option` | The Russian MPh: r1 moves from [D6] to [E6] in the LOS of the concealed German in [F5]. The Russian view shows only the DEFENDER's window ("The DEFENDER may fire at the moving stack or pass (A8.1)"); the German view shows `#play-choice`, "A concealed enemy stack in [E6] has done something that loses its "?" in the LOS of your concealed units only. Take ... Decline ... (A12.14; ruling R70.21)". Declined and committed; the Russian view's records hold nothing of it; the file holds `choice-pending` and `choice-made` with visibility ["german"]. Played. |
| `p70-remove-concealment` | The German PFPh: "Give up "?"" offers, grouped by hex, "[C6] Dummy G1", "[C7] 4-6-7 squad G2", "[F5] 4-6-7 squad G1"; all three proposed and committed (A12.14, A12.31, A12.11; ruling R70.20). Played. |
| `p70-vehicle-gain` | The German CCPh: "end the phase" proposes "Vehicle G1 gains "?"" and "Vehicle G2 gains "?" (A12.12, the Concealment Table)"; committed. Played. |
| `p70-hidden-gun-seen` | Ending the Russian RPh with the Russian in [H8]: the Russian Units tab shows "? German concealed [G7]", and the proposal names nothing of it. Played. `p70-hidden-gun` keeps its blocked pair. |

Rules: `AQuestionMarkIsRemovedVoluntarilyOnceInPlayADummyEliminatedAndAHiddenUnitRevealed`, `TheViewerMayDeclineOnlyWhenEveryLossSeerWithinSixteenIsConcealed`, `AnEmplacedGunsCrewAndAVehicleGainByCasesJAndKOnly`, `TheConcealmentDrTakesLaxAndStealthy`, `AVehicleOnAWoodsOrchardBrushOrGrainRoadSetsUpUnderQuestionMarkOrHidden`, `AVehicleSetsUpHiddenByItsSsr`, `AnEmplacedGunSetsUpHiddenOutOfConcealmentTerrainAndIsPlacedBeneathQuestionMarkOnceSeen`, `AnyReinforcementWaitsOffBoardUnderANonObQuestionMark`, `AStackOfTwoWaitingOffBoardTakesOneQuestionMark`. Play: `AVoluntaryRemovalShowsAUnitRevealsAHiddenGunWithItsCrewAndRemovesADummy`, `ATankGivingUpItsQuestionMarkRevealsItsRiderAndTheRiderMayNotGiveItsUpAlone`, `AConcealedViewerMayDeclineToForceAMoversLossAndTheMoverLearnsNothing`, `AnUnconcealedViewerForcesTheLossWithNoChoice`, `AVehicleAndItsRiderGainQuestionMarkAsItsPlayerTurnEnds`, `AHiddenGunOutOfConcealmentTerrainIsPlacedBeneathQuestionMarkOnceAnEnemySeesIt`, `AnOffBoardReinforcementWaitsConcealedWhateverItsOb`. Page: `TheOwnerGivesUpAQuestionMarkFromThePlayPage`, `TheViewersOptionIsShownToItsSideAloneAndTheMoverSeesTheWindow`.

## 13. Bypass endings and detection: 70.8 (increment 3f)

Built on 2026-10-25, 16:19 to about 16:48, in one commit (45d52583) by a building agent to a brief (`brief_3f.md`), ruling R70.23, its diff read and one of its games played in the Studio by the session.

### 13.1 Departures from the frame, first

- **Mandatory Fire (ruling R70.37):** lifting the refusal of TPBF inside a Bypassed hex made A8.312 owed against every Bypass entry with an enemy unit in the obstacle, which would force concealed units to fire and reveal themselves, against A12.151's "may use TPBF". `ScenarioA1MandatoryFire.Owed` owes nothing against a stack in Bypass, as before.
- **A forced-back Minimum Move pins nothing and sets no CX (ruling R70.38):** A4.134 (p. 48) pins and CXes a unit "after a unit has entered a hex by Minimum Move", and a stack forced back entered none.
- **Hidden units:** at an obstacle attempt (R70.23's occupation) they are placed and revealed (A12.15); in the forced back from a Bypass exit, a broken or pinned member, and a vehicle's ending, they stay hidden. A Dummy counts as a "?" for every trigger and is removed; an obstacle of Dummies only is occupied. Since Round A the loss of every "?" needs a real concealed enemy unit (A12.151).
- **A broken or pinned member** reveals the "?" for the mover of the latest step at the window's close; A12.151's "or any other Infantry unit which legally ends its MPh" there is not built: backlog row (72.1).
- **A plumbing fix:** `GameGate.EffectHolds` read any `attempted == to` as an off-board forced back; it now requires `Bypass: null`.
- **The forced back's record line** read "g1 moves from [F5] to [F5]" (older than the pass); reworded at Round E.
- **The agent's scratch LOS tool** was built in the session's scratchpad, not registered; the session registered it (section 0, point 14 of the standing rules).

### 13.2 What is built

`ScenarioA1BypassDetection` (Rules: `KeepsLane`, `EndsInBypass`, `AllConcealmentLost`, `Occupy`, `VehicleEndRevealsInBypass`, `VehicleReveals`, and the texts) and `GamePlanner.Bypass.cs` (`AddBypassReveal`, `EndedInBypassReveal`, `VehicleBypassReveal`). The Minimum Move refusal removed from `EnemiesAtTarget`: the normal forced back applies, all MF spent. `ObstacleHeld` in `MoveStep`: an attempt to occupy an obstacle that holds concealed units reveals them and forces the stack back into its lane, MF spent. The TPBF refusal inside a Bypassed hex lifted (R70.23, R70.37). `PlanEndVehicle` reveals the concealed units of a Location where a vehicle ends its MPh in Bypass, unless it is "broken" (no crew and no PRC, A12.1, A12.42).

**New in a record:** none by name: the forced-back lane is kept in the step's existing `bypass` field, absent meaning as before. **An older record** carries a forced back with `bypass` null and replays as it did.

### 13.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-bypass-occupy-forced-back` | The German MPh: g1 Bypasses [F5] along "south" past r1 (concealed) and r2 (hidden); the Russian passes; g1's attempt to occupy [F5] commits; the file holds `movement-step` with `to` and `attempted` both [F5] and `bypass` kept, then `concealment-lost` for r1 and r2. Played. |
| `p70-minimum-move-forced-back`, `p70-bypass-exit-forced-back`, `p70-bypass-pinned`, `p70-vbm-ends-in-bypass` | The other four cases; asserted by their Play tests, not walked. |

Rules: `AMinimumMoveIntoAConcealedUnitIsForcedBackAndPinsNone`, `AStackForcedBackFromItsBypassExitKeepsItsLaneAndEveryQuestionMarkThereIsLost`, `ABypassMemberBrokenOrPinnedEndsItsMphWhereTheQuestionMarksAreLost`, `AnAttemptToOccupyAnObstacleWithConcealedUnitsRevealsThemAndForcesTheStackBack`, `AFirerInTheBypassedObstacleMayFireTpbfAtTheBypassingStack`, `AVehicleEndingItsMphInBypassRevealsTheConcealedUnitsThereUnlessBroken`. Play: `AMinimumMoveIntoAConcealedSquadIsForcedBackAndEndsItsMphUnpinned`, `AStackForcedBackFromItsBypassExitStaysInItsLaneAndEveryQuestionMarkThereIsLost`, `APinnedBypassMemberRevealsTheQuestionMarksOfItsLocationAsTheWindowCloses`, `AVehicleEndingItsMphInBypassRevealsTheConcealedSquadThereButNotTheHiddenOne`.

## 14. Searching, and Mopping Up in any building: 70.9 (increment 3g)

Built on 2026-10-25, 16:53 to about 17:27, in one commit (293dd0b1) by a building agent to a brief (`brief_3g.md`), rulings R70.24 and R70.25, its diff read and its games played in the Studio by the session.

### 14.1 Departures from the frame, first

- **"Accessible":** the text layer holds no Index definition, so R70.24's reading holds (its own hex and the ADJACENT hexes the stack could enter). **Ruling R70.39:** the Search dr, its drm, and the hexes are public, though the drm shows a concealed searching stack's HS-equivalents and whether a leader leads it.
- **Who may search:** members of a moving stack once the DEFENDER's window on their last MF has closed; a stack that has not moved cannot; berserk, captured, and Unarmed units are refused.
- **Search Casualties:** the trigger is an armed Good Order DEFENDER Personnel unit or Residual FP; the drm counts the units concealed or hidden when the dr was made. Minefields, HE FFE, Booby Traps, Fortified Buildings, Cavalry, and the Japanese +2 are not built (R70.2's rows).
- **Buildings stay on one board,** as the Studio's fill: 3d's rout read crossed board seams on a placed map and now uses the per-board map.
- **The record holds the roll's id, not the dr value,** and `accessible` for the projector's check.
- **Seen in the Studio, for the fixes:** the Latest line left out the Casualty dr (fixed at Round E), and the hexes not named were not searched (ruling R70.42 at the reviews, fixed at Round B).

### 14.2 What is built

`ScenarioA1Searching` (Rules: who may search, the drm, how many hexes, the reveals, the casualty check, UNIT-STATE-051; since the fixes `Searched<T>`, `HalfMfCost`, `SearcherConditions`). The event `units-searched` and the projector's Search (1 MF, TI for the rest of the Player Turn, the window, the ending members). `GamePlanner.Buildings.cs`: one flood fill shared with the Studio's `CardMaps.Building`, joins decided by `BuildingGoesOn`, a building map per board handle (a static `ConditionalWeakTable` keyed by `BoardHandle`), a card's building id kept and "board:lowest hex" otherwise (R70.25); the rout reads it. `ObjectIndex.Occupied` and `UnitsInHexes` (one pass over the occupied stacks; `OccupiedIn(hexes)` since Round D). `GamePlanner.Searching.cs` (`PlanSearch`, `SearchHexes`). Mopping Up in any multi-hex or multi-level building of the map, Control folded from the map, `ScenarioA1MoppingUp.CardBar` removed. The action `asl.game.search` and `SearchAction.razor` (hexes ticked in order of preference).

**New in a record:** the event `units-searched` (`units`, `location`, `roll`, `drm`, `accessible`, `searched`; `casualtyDrm` since Round E). **An older record** holds none; an older Mopping Up of a card's building replays with the card's id (`AnOlderRecordedMoppingUpOfACardsBuildingReplaysAsItDid`).

### 14.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-search` | The German MPh, dice 3 then 6: g1 and l1 move into [M5], the Russian passes, the Search box lists "[L4] [L5] [M4] [M6] [N4] [N5]"; ticking [L4] then [M4] proposes the Search "(-1 for 2 HS-equivalents, -1 leadership)"; committed: "the Search dr 3 - 2 = 1; [M5], [L4], [M4] are searched: 1 concealed enemy unit loses its "?"; hidden enemy units are placed beneath "?" in [L4] (A12.152)". The file holds `units-searched`, `concealment-lost` r1, `hidden-placed` r2 (visibility ["russian"]), and the `search-casualty` roll. Played (before R70.42, which now searches the other hexes too). |
| `p70-mop-up-any-building`, `p70-mop-up-hidden` | Mopping Up of a stone building no card names; the same with a hidden Russian squad on level 1 of [N1] (played at Round A's check, section 18.2). |

Rules: `TheSearchDrTakesStealthHalfSquadsTheBestLeaderLaxAndCx`, `TheFinalDrLeavesThatManyAccessibleHexesUnsearched`, `OnlyGoodOrderInfantryOfTheMovingStackWithAMmcNotPinnedNorUsingAssaultMovementSearches`, `ASearchRevealsConcealedPlacesHiddenRemovesDummiesAndAnArmedGoodOrderDefenderDrawsTheCasualtyDr`, `MoppingUpIsBarredOnlyForANameThatIsNoBuildingAndABlazeIsNoPartOfOne`. Play: `ASquadAndItsLeaderEndingTheirMoveSearchAndRevealAConcealedAndAHiddenSquad`, `ASearchCasualtyDrReducesTheSearchingSquad`, `ASearchWaitsForTheDefendersWindowOnTheLastMfToClose`, `TheMapsFloodFillSplitsARowhouseAtItsWall`, `AStoneBuildingNoCardNamesIsMoppedUpAndItsBrokenEnemySurrenders`, `AnOlderRecordedMoppingUpOfACardsBuildingReplaysAsItDid`. Page: `TheMovementPanelOffersASearchOfTheHexesTickedAndBothSidesReadIt`.

**The measurement after 3g:** my Studio, hand-over 481, 455, 546, 505, 430 ms, the proposal 72, 60, 57, 63, 72 ms. The hand-over was past the 20% threshold on its face against 3a's, but the user's Studio was running on 5178 by then (other Studios on the shared boards folder inflate every timing, the lesson of pass 65). The base, measured again at once under the same load: 447, 550, 557, 531, 312, 577 ms. Level (medians 540 base, 481 pass). The cold open was left for the whole-pass check.

## 15. Withdrawal completed, CC with a Gun's crew, a Carrier HS: 70.6 (increment 3h)

Built on 2026-10-25, 17:35 to about 18:35, in one commit (077e6de5) by a building agent to a brief (`brief_3h.md`), rulings R70.26 to R70.28, sent back once to build ruling R70.40; its diff read and one of its games played in the Studio by the session.

### 15.1 Departures from the frame, first

- **Ruling R70.40, from a failing page test:** `PlayPageCloseCombatTests.U33...` lost an enemy leader's row after the CCPh, since 3a's read left a side's own units held in Melee out of Good Order (A.7), so they did not inspect the enemy stack in their own Location. A read-only agent read A11 in full; no rule says it in words, but CC cannot be played without it. R70.40: a side's units in the same Location as an enemy stack inspect it whatever their state. Built (`ScenarioA1Sight.Inspectable(setupClosed, ownUnitThere, anySeerInLos)`); since Round A, Dummies and prisoners are left out.
- **The attack builder names every participant hex first,** not only the crew.
- **A concealed Gun in the destination** is never in the Random Selection and is revealed whenever the withdrawers are eliminated, which may over-reveal when two concealed units are there: backlog row (72.1).
- **Dummies beside real units stay;** the rule names a Dummy-only Location.
- **Ambush Withdrawal drops in the planner,** but the page offers only withdrawals that drop nothing; D6.82's Carrier squads Deploying to mount; an A18.12 leader whose infiltrating MMC is eliminated on entry: backlog rows (72.1).
- **The destination reads Known enemy units only** (`KnownEnemy`); no view is built.
- **Older tests changed as intended:** the Gun's crew refusals and the IPC withdrawal.
- **A slip with the user's game:** a Grep over `src/ASL` that did not exclude `boards` returned two lines of the live `dl-tractor-works-03`; the file was not opened or written. `brief_build.md` gained the line that bars it.

### 15.2 What is built

`ScenarioA1Withdrawal` (Rules: `Entry`, the destination's verdict; `Revealed`, the Random Selection of the "?" lost; `SelectionRolled`; `DropsText`, `ForeignDrop`) and `GamePlanner.Withdrawal.cs` (`WithdrawalEntries`, `AddWithdrawalReveals`). A withdrawal is barred only by a Known enemy unit (R70.26); into a concealed or hidden enemy's Location the withdrawer is eliminated, the selected "?" lost, and hidden units placed; into a Dummy-only Location the Dummies are removed; a unit above its IPC drops SW, unpossessed in the Location (`.cc-drop`, `.cc-infiltration-drop`). A Gun's crew in CC (R70.27): the five refusals removed (`CrewBar`, `GunCrew`, the crew facts and clause), A11.13's loss covering Guns (`CloseCombatUnit.Guns`, read only where the record lists them). A Carrier HS (R70.28): `NewInstance.CrewOf` and `ScenarioA1SupportWeaponRules.NeedsNoLeader(..., carrierHalfSquad)`.

**New in a record:** `guns` on each CC unit (written `"guns": []` for a unit that mans none, which marks a pass 70 record); `crewOf` on a leaving Carrier crew's `instance-created`. **An older record** lists no Guns, so the Gun loss is not rolled (`AnOlderCcRecordThatListsNoGunsReplaysAsItDid`); an older HS has no `crewOf` and needs its leader to recombine.

### 15.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-gun-crew-cc` | The German APh: the advance onto the 2-2-8 crew in [D10] proposes "CC follows (A3.7)" (refused before the pass); committed; in the CCPh the defenders read "[D10]: 2-2-8 crew in [D10]", the attackers "[D10]: 4-6-7 squad G1" and "... G2". Played. |
| `p70-withdraw-concealed`, `p70-withdraw-hidden-gun`, `p70-withdraw-drop`, `p70-carrier-hs` | The withdrawals into a concealed Location, a hidden Gun's Location, and above the IPC; the Carrier HS. Asserted by their tests; `p70-withdraw-drop` played at Round C's check. |

Rules: `AWithdrawalIsBarredOnlyByAKnownEnemyAndMeetsWhatIsThere`, `AUnitAboveItsIpcWithdrawsByDroppingItsOwnSw`, `AGunsCrewFightsInCcWithItsInherentFpAndItsGunIsLostOnAColoredOneAndTheKillNumber`, `ACarrierHalfSquadRecombinesWithNoLeader`, `ABrokenUnitAndAUnitInMeleeInTheEnemysLocationInspectIt`. Play: `AWithdrawalIntoAConcealedEnemysLocationEliminatesTheWithdrawerAndARandomSelectionLosesItsQuestionMark`, `AWithdrawalIntoAHiddenGunsLocationPlacesTheGunAndItsCrewUnconcealed`, `AUnitAboveItsIpcWithdrawsByDroppingItsMmgAndAWithdrawalRemovesTheDummiesItEnters`, `AnAdvanceOntoAGunsCrewLeadsToCcAndItsGunIsLostOnAColoredOneAndTheKillNumber`, `AnOlderCcRecordThatListsNoGunsReplaysAsItDid`, `CarrierHalfSquadsRecombineWithNoLeaderAndOrdinaryOnesStillNeedOne`. Page: `TheCcPanelOffersADropAboveTheIpcWithdrawalsTheSideMayKnowOfAndAGunsCrewHexFirst`; `PlayPageCloseCombatTests.U33OnThePageASquadAdvancesAndItsCloseCombatLeavesTheSurvivorsInMelee` passes again.

## 16. Infantry against Infantry in a Location with a vehicle, its Ambush, PRC in CC: 70.7, part I (increment 3i)

Built on 2026-10-25, 18:37 to about 19:24, in one commit (9601a3b6) by a building agent to a brief (`brief_3i.md`), rulings R70.30 to R70.32, its diff read and its game played in the Studio by the session.

### 16.1 Departures from the frame, first

- **`VehiclePresentBar` is kept,** routing the case to the vehicle CC action, which takes Infantry against Infantry with no vehicle named: R70.30's "joins the vehicle sequence".
- **A crew inside is never held in Melee:** A11.71 names Passengers and Riders, and A11.7 holds no vehicle's crew (R70.31 said "PRC").
- **Passengers' FP in the vehicle's attack:** a MMC its printed FP, a SMC 1, pinned half, broken none, then the vehicle's halvings.
- **Not built in the sequential attack:** withdrawal, Infiltration, and capture (3j builds the capture of Passengers). An Ambush needs Infantry of both sides; a side whose force is a vehicle alone gets none. Ambush drs rolled where no side has an attack left may leave the Location open and block the others (A11.12). Backlog rows (72.1).
- **R70.32's texts** are on the unload, Bail Out, and TPBF proposal lines and the unload and Bail Out records; the TPBF fire record line is not built (backlog row, 72.1). The unload line named any enemy there, hidden ones included; it now names only a Known one (a leak closed).
- **Confirmed by test:** before the pass the projector held a vehicle's PRC in Melee, mobile vehicles' included (the frame's finding); an older record still does.
- **Two public facts, for the referee:** `prcHeldInMelee` on the public `phase-changed` named Passenger ids (replaced by a flag at Round A); the Ambush facts count a side's vehicles and BU AFVs in a public record (ruling R70.41 at the reviews).
- **Seen by the agent:** `ChargeBarred` (R30.5) still said CC between Infantry beside a vehicle "is not built" (reworded at Round E); the vehicle CC panel's header showed a raw Location id, now through the page's words.

### 16.2 What is built

`CloseCombatFacts.Sequential`, `CloseCombatUnit.Rider` (+1 attacking, -1 attacked, A11.611), `AmbushFacts.Vehicles` (+2 vehicle, +1 for each BU or Stunned AFV, A11.4), `VehicleCloseCombatFacts.Ambusher` and `.Passengers`; `ScenarioA1VehicleCloseCombatRules.SidesWithAttacks(..., infantryAttacked)`, `FirstCcSide` and `NextCcSide(..., ambusher)`; Ambush drs in a vehicle's Location; `CloseCombatResolved.Next` and `.Closed` on sequential rounds; `PhaseChanged.PrcHeldInMelee` (Round A: `prcMelee`, with `ScenarioA1PassengerActionRules.HeldInMelee`); `PlanInfantrySequential`; the panel's `.propose-vehicle-cc-infantry`; `VehicleCcRows` through the index (closing pass 65's backlog row of per-render scans for it).

**New in a record:** `sequential` on CC facts and the round `sequential` on `close-combat-resolved` with its `next` and `closed`; `rider` on a CC unit; `vehicles` on Ambush facts; `ambusher` and `passengers` on vehicle CC facts; on the CCPh-ending `phase-changed`, `prcHeldInMelee` (3i) and `prcMelee` (since Round A, written only when true). **An older record** carries none: an Ambush or an Infantry round in a vehicle's Location was refused before the pass, and an older CCPh end holds every unit aboard as one on foot, as it did (`AnOlderRecordedVehicleCcSequenceKeepsItsRecordedNextAndClose`).

### 16.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-vehicle-cc-h3` | The Russian APh, PAATC dice 3 4, 3 4: the advance onto the PzKpfw IIIH and the 4-6-7 in [F8] proposes the PAATC and "CC follows (A3.7)"; in the CCPh the panel reads "[F8]: the russian side attacks next" (since Round E "the Russian side") and offers "Propose: the Infantry attack the checked enemy Infantry (A11.31)"; r1 attacking the 4-6-7 (dice 6 6) commits `close-combat-resolved` with `round` "sequential" and `next` "german". Played. |
| `p70-vehicle-cc-ambush`, `p70-vehicle-cc-prc` | The Ambush in a vehicle's Location and the PRC; asserted by their tests. |

Rules: `ASequentialRoundIsOneAttackWithNoReplyAndKeepsTheAmbushDrm`, `ARiderAttacksWithPlusOneAndIsAttackedWithMinusOne`, `TheAmbushDrTakesTheVehicleDrmOfEachSide`, `TheAmbushDrmAppliesBothWaysInCcWithAVehicle`, `ACeHalftracksPassengersJoinItsAttackAndOnlyAnImmobileVehiclesPrcAreHeldInMelee`, `TheSidesWithAttacksCountInfantryAgainstInfantryAndTheAmbusherAttacksFirst`. Play: `TheH3ExamplesShapeAlternatesInfantryAndATankAgainstInfantryWithNoReply`, `AnAmbushInAVehiclesLocationTakesTheVehicleDrmAndTheAmbusherAttacksFirstWithItsDrmBothWays`, `AnOlderRecordedVehicleCcSequenceKeepsItsRecordedNextAndClose`, `ARiderIsAttackedApartAndOnlyAnImmobileVehiclesPrcAreHeldInMelee`. Page: `TheVehicleCcPanelOffersAnInfantryAttackOnARiderNamedHexFirst`.

## 17. Street Fighting, A11.52's Passengers, A18.12 against a vehicle, D5.42: 70.7, part II (increment 3j)

Built on 2026-10-25, 19:28 to about 20:20, in one commit (3384c2b2) by a building agent to a brief (`brief_3j.md`), rulings R70.33 and R70.34, its diff read and its game played in the Studio by the session. With it the build of the increments was done, 3a to 3j: no increment was left out (R70.1).

### 17.1 Departures from the frame, first

- **A11.52's +2 for the captors** applies to the vehicle's own attack, since Passengers attack only inside it (A11.62, 3i); on an armed unarmored vehicle it covers its MG FP too. A reading; the CC referee proposed the attack offered with or without its Passengers: backlog row (72.2), for the user's ruling.
- **The capture's record:** an unarmed vehicle takes cause `unarmed` (A21.21's Temporary Driver); an armed unarmored vehicle a new cause `passengers`, marked Abandoned so a captor can enter (a reading of A21.22, which the referee found right).
- **An unarmed vehicle carrying Passengers** is no longer captured automatically at the CCPh's start: A11.52 asks the Passengers captured first. Older records replay.
- **Street Fighting's readings, accepted:** a road hex has a road across at least two hexsides; at a three-road junction any two sides with buildings; every attacker must qualify; the return needs the Location's CC closed and an enemy vehicle or wreck there; `streetFighters` taken as recorded. Wire and mines on the return, and a HS returning after a Reduction: backlog rows (72.1).
- **A18.12 in the CCPh only,** not in CC Reaction Fire; the created leader is named (`createdLeader`) and counted as having attacked. 3i's created leader in a sequential Infantry round is not counted as having attacked: backlog row (72.2).
- **Outside the increment:** a pre-existing xUnit2031 in `PlayPagePass70Tests.cs` (3c's) that failed the strict MapStudio test build was fixed; `dotnet format` re-wrapped 3i's initializers (whitespace).

### 17.2 What is built

Maps: `RoadFlanks.Of` and `HexFactSet.Contains`. `ScenarioA1StreetFighting` (Rules: `Flanked`, `Qualifies`, `Applies`, `ReturnBar`, the texts) and `GamePlanner.StreetFighting.cs` (R70.33: -1 on the attack, +1 on the vehicle's, not with an actual Ambush; the return to the building or staying, unless broken or pinned; the action `asl.game.street-fighting-return`). A11.52's capture of Passengers (R70.34): `CloseCombatUnit.PassengerOf` (any attack at a Passenger but a capture refused; `vs-passenger-capture` -2), `ScenarioA1CapturedVehicleRules.Passengers`, `PassengersCapturable`, `CapturedWithPassengers`. Vehicle CC facts `StreetFighters`, `LeaderCreation`, `Captors`; A18.12 against a vehicle (R70.34, worded at Round C as R70.46). D5.42: `RecrewFacts.CapturedThisCcph`, a captor entering the vehicle it captured in that CCPh whatever MF it spent. State: `AdvanceRecord.AdvancedFrom`, `CloseCombatLocation.Captors`, `GameState.CapturedThisPhase`, each computed from records every game holds. The page: `.propose-vehicle-cc-capture-passengers`, `.street-fighting-return[data-unit]`, the CCPh's `.vehicle-recrew`.

**New in a record:** `passengerOf` on a CC unit; `streetFighters`, `leaderCreation`, and `captors` on vehicle CC facts; `captors` on `close-combat-resolved`; `createdLeader` on `vehicle-close-combat-resolved`; the cause `passengers` on `vehicle-captured`. **An older record** carries none: no Street Fighting, no capture of Passengers, and no Leader Creation roll (`AnOlderRecordedVehicleCcWithAnOriginalTwoReplaysWithNoLeader`).

### 17.3 The games and the tests

| Game | What it shows |
|---|---|
| `p70-street-fighting` | The Russian APh, PAATC 3 4: r1 advances from the building [M5] onto the BU PzKpfw IIIH in the road hex [N5] (buildings [M5] and [O5] on its sides); in the CCPh the attack on the vehicle (dice 5 5) proposes "... the attackers advanced from an ADJACENT building, so Street Fighting gives their attack on the vehicle -1 (A11.8)"; committed. Played. |
| `p70-capture-passengers`, `p70-leader-creation-vehicle`, `p70-recrew-captured` | The capture of a truck's Passenger, Leader Creation against a tank, and the CCPh re-crew; asserted by their tests. |

Rules: `ARoadHexFlankedByBuildingsOnBothSidesQualifiesTheInfantryThatAdvancedFromTheirGroundLevel`, `StreetFightingGivesMinusOneAgainstTheVehicleAndPlusOneToItsAttackButNotWithAnActualAmbush`, `AStreetFighterReturnsOnceTheCcIsOverUnlessBrokenOrPinned`, `PassengersAreAttackedOnlyToCaptureThemAtMinusTwoAndTheirCaptorsDefendAtPlusTwo`, `AMmcsOriginalTwoAgainstAVehicleCreatesALeaderWhoAddsHisLeadershipAndACcvOnlyWithoutASmc`, `ACaptorEntersTheVehicleItCapturedInThatCcphWhateverMfItSpent`. Play: `AVehicleInARoadHexBetweenBuildingsIsFoughtByStreetFightingAndTheSquadReturnsToItsBuilding`, `ATrucksPassengerIsCapturedInCcAndTheTruckWithIt`, `ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack`, `AnOlderRecordedVehicleCcWithAnOriginalTwoReplaysWithNoLeader`, `AHalfSquadThatCapturedAnAbandonedTankEntersItAsItsCrewInThatCcph`. Page: `TheVehicleCcPanelOffersTheCaptureOfATrucksPassengerNamedHexFirst`.

## 18. The six reviews, their fixes, and the Studio check

Six read-only reviews ran in parallel on 3384c2b2, 20:25 to 20:38 (briefs in `brief_reviews.md`): three referees by domain (R1, morale and rout, 3b to 3d; R2, who sees and hidden information, read from the side that must not see; R3, Close Combat, 3h to 3j), a table player (R4) who read the events of every `p70-*` game, a UI and Blazor review (R5), and a Rules boundary and memory review (R6). Beside them the gate's strict stage (`gate.sh 70 25aa97fd strict`, 20:25 to 20:29) passed: 0 warnings, 0 errors, and its formatting left no file changed. In sum: no BLOCKER, 10 MAJOR (R2 5, R5 2, R1 1, R6 1, R4 1), about 45 MINOR, and about 25 NOTE. No review found an older record whose replay changes. The findings as reported are `fixes_referee_morale.md`, `fixes_referee_sight.md`, `fixes_referee_cc.md`, `fixes_table.md`, `fixes_ui.md`, and `fixes_boundary.md` in the pass's folder; `brief_fixes.md` ordered the fixes in five rounds, one agent each.

### 18.1 What departs from the plan's entry and the frame, first

- **Eight rulings were made at the reviews and the fixes** (F16): R70.41 (the Ambush facts public), R70.42 (a Search searches the named hexes, then the others up to the dr's count), R70.43 (the Voluntary Break's ADJACENT clause counts any Known unbroken enemy; a "?" does not: a deviation), R70.44 (a leader lost in a rout causes a LLMC of bystanders too), R70.45 (the Search and Mopping Up Casualty drs always made: a deviation), R70.46 (a created leader's DRM in vehicle CC, its three cases worded at Round C), R70.47 (an Unarmed unit failing a MC is Casualty Reduced, A20.54), and at Round E R70.48 (the Casualty dr's drm recorded, its line given to the DEFENDER's and the adjudicator's views only).
- **R70.43's premise was corrected by the Round B agent:** `RoutEnemyFacts.Armed` is false only for an Abandoned vehicle, so an unarmed truck ADJACENT already allowed the break; the real gap was the LOS clause counting Unarmed Personnel as armed. A fact read by the Voluntary Break alone (`Unarmed`) now leaves them out; the rout's own reading of Unarmed Personnel as "armed" is a backlog row (72.2).
- **A risk Round B named:** a pass 70 record in which an Unarmed unit failed a check would now fail verification (R70.47). The user's live `dl-tractor-works-03` cannot hold one (it is recorded by main's build, which writes no pass 70 fact); the `p70-*` games were replayed at the whole-pass check, every one to its last event.
- **Round D found the hook's gap:** its first shell call began with a `cd` into the repository root, which the hook did not refuse; the hook's rule names folders of the repository. For the tools' backlog.
- **Three slips with the user's game** across the pass (3h, the table player, Round A): a Grep pointed at `src/ASL`, once with a `glob` of `!boards/**`, which does not exclude the folder, read lines of the live game files; nothing was opened or written. `brief_build.md` says a glob does not exclude it and names the three.
- **R3's finding 9** (A1.32's "Guards" read as prisoners' Guards since R31.4, older than the pass) is for the user as a separate ruling, not this pass's.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 65's as the model: section 18.4.

### 18.2 Fixed

**Round A, hidden information** (20:41 to about 21:10, 8f92714a; R2 1 to 8 and 10, R1 1, R3 1, R5 14, R6 16 and 2):
- the mover reads the open window's own refusal while the viewer's option waits (`ScenarioA1ConcealmentChoices.PendingLossBar`), and `MovementPlans` treats the wait as the DEFENDER's turn (R2 1, MAJOR);
- `prcHeldInMelee` replaced by a flag `prcMelee`, the projector reading A11.71 from the vehicle (`ScenarioA1PassengerActionRules.HeldInMelee`), so the public record names no unit aboard (R2 2, MAJOR);
- a LLMC's record leaves out "?" units that took no check (`ScenarioA1MoraleCheck.Unnamed`, used only where the check resolves the same without them; otherwise withheld), its dice take the record's visibility, and the Failure to Rout warning says "may call for a LLMC ... rolled at the commit if any is due" (R2 3, R1 1, MAJOR);
- the case read takes the planner's view (R2 4, R6 16, MAJOR);
- Mopping Up and Ambush Withdrawal always a `PlannedRoll`, everything found built at commit (R2 5, MAJOR; R3 1);
- R70.40 leaves out Dummies and prisoners; units aboard are never the top counter; "an uninspected unit"; A12.151's loss needs a real concealed enemy; the hand-over line names no leader; `GameView` skips the Location read when the set is empty.
`GamePlanner.PrcHeldInMelee` and the unused Mopping Up summary texts were removed. Older tests changed as intended (the Rider and PRC Melee method; two `UnitNamesTests` methods). Rules 9 of 9, Play 32 of 32 and 2 of 2, Units 10 of 10, MapStudio 4 of 6 then the fixed two 2 of 2 and 1 of 1, by method; build exit 0. Checked in the Studio in `p70-mop-up-hidden` (German PFPh, a hidden Russian squad on level 1 of [N1]): the Mopping Up proposal of [M2] ends "What it finds is said when it is made", naming nothing of [N1].

**Round B, morale, rout, and Searching** (21:14 to about 21:37, 62e01dbf; R1 2 to 5, R4 1, 3, 5, R6 5, 6, 8, 15, R70.45):
- the rout advice (`RoutIgnoredCovers`): "It may ignore ..., so any legal route will do, one ending there included (A10.51 EXC; the example on p. 69, S3)", and with no legal step "surrenders if it can (A20.21) or is eliminated for Failure to Rout (A10.5)";
- R70.47: an Unarmed target that fails a MC goes down the prisoner path, Casualty Reduced, the Bail Out included, read only on records with the fact;
- R70.44 kept, with its reason in the comments; R70.43's refusal ends "; a concealed or hidden enemy unit does not count (ruling R70.43)";
- R70.42 (`Searched<T>`: the named hexes first, then the other Accessible hexes in map order; R4 1, MAJOR); the Voluntary Rout's LLMC cause "voluntary-rout"; R70.45's texts "which is always made";
- `BreakEffects`, `RoutLimit`, `InterdictionRevealsLeader`, `EndsRoutAfterLeaderLoss`, and `LeaderLossSubjects` into Rules (the LLMC subjects read from the index); the old who-sees members deleted and their tests pointed at `ScenarioA1Sight`.
Five tests changed as intended (the Unarmed squad now Reduced, the Search's counts, the Voluntary Rout's cause). Rules 14 of 14, Play 22 of 22; build exit 0. Checked in the Studio in `p70-rout-own-building` (German RtPh): the Rout panel gives the new advice. `games_fixes_b.py` wrote `p70-break-unarmed-adjacent`, `p70-break-unarmed-far`, and `p70-unarmed-llmc`, asserted by their tests (`AnUnarmedEnemyAllowsAVoluntaryBreakOnlyAdjacentAndTheBreakIsDecidedInRules`, `ALeaderLostWithOnlyAConcealedUnitBesideHimStillRollsAndNamesNoVerdict`, `AnUnarmedEnemyAllowsAVoluntaryBreakOnlyWhenAdjacent`, `AnUnarmedSquadThatFailsItsLlmcIsCasualtyReducedNotBroken`, `ABrokenUnitWhoseOnlyCoverInReachIsItsOwnBuildingIsToldItMayIgnoreIt`, `AUnitThatMayIgnoreEveryCoverWithinReachIsToldSoAndWhereItMayEnd`).

**Round C, Close Combat** (21:39 to about 22:05, 03af0037; R3 2, 3, 6, 7, R5 2, R6 11, 13, 14, R4 9):
- R70.46 in `ScenarioA1VehicleCloseCombat`, most specific first: a combining leader's DRM replaced; a hero combining, none; no SMC, his leadership and +1 CCV;
- a berserk unit or a reinforcement where a Melee is held owes an attack on a Known enemy vehicle or on Known enemy Infantry no Infantry has attacked (`OwingAttack`; the pass and the CCPh end refused; a concealed reinforcement exempt as R14.2; a concealed enemy no owed target);
- `CaptureStands` checks that no uncaptured Passenger is aboard, for cause `passengers` only (cause `unarmed` could be captured with Passengers before the pass);
- the drops made before the withdrawer's elimination, in both paths;
- a laden unit's row keeps its drop boxes, its destinations once enough are ticked (`MayWithdrawByDropping`; R5 2, MAJOR);
- vehicle CC conditions, `MakesEnemyLocation` (one predicate for `PlayRecords`, the unload, and the Bail Out, Dummies now left out of the line too), and `RecrewCandidate` into Rules.
No recorded fact changed. Rules 12 of 12, Play 22 of 22 and 3 of 3, MapStudio 6 of 6; build exit 0. Checked in the Studio in `p70-withdraw-drop` (German CCPh): the withdrawal for g1 reads "stays" only, with the LMG and MMG to drop; ticking the MMG adds six destinations.

**Round D, the boundary, and memory and performance** (22:14 to about 22:30, ab082dc0; R6 1, 3, 4, 7, 9, 10, 12, 18, 20, R5 3, 12):
- R6 1 (MAJOR): `GamePlanner.HistoryViewOf`: a state with no memo gets its uninspected set from a transient `PlayStepMemo` dropped after the read (the LOS, handle, composed, and sight reads take the memo as a parameter so none creates one), read by `LivePlay.NamesOf` and `GameLibrary.NamesOf`;
- into Rules: `ScenarioA1Sight.EmptyVehicle` (3); the gain's raw facts, Large, Lax, and Stealthy (4); `VehicleWeapons` and `GunWorks`, with `PlayStepMemo.MannedGuns` once per state (7); `ScenarioA1Sight.ConcealedLossSeers` and `RemovalEliminates` (9); `SharesConcealment` both ways, so a crew inside now loses its "?" with its vehicle (10); `HalfMfCost` and `SearcherConditions` (12);
- `OccupiedIn(hexes)`, read-only, with no boxing (18); the road sides once per state (20); the CC lists and Mopping Up choices kept on the page by their keys (R5 3, 12).
Rules 5 of 5, Play 17 of 18 then 1 of 1 and 4 of 4 older records, MapStudio 6 of 6; build exit 0 after one CA1859. **The memory reading** (`studio_memory.ps1`, private and working-set memory, no forced collection; the cold open of the whole `dl-tractor-works-02`, then one hand-over to each side; each Studio alone, the user's Studio running throughout): before the round the pass held 255 MB private and 322 MB working set against main's 231 and 291 (about 24 MB, 10%, more: a memo kept per revision, as R6 1 predicted); after it 225 and 297. Level. R6 19 was done by the session: the proof tool writes the view path it took on its log ("sweep view: GamePlanner.ViewOf", or "GameView.Of (no planner entry in this build)"); `tool_check_3a.sh` run again, the sweep of `dl-tractor-works-02` and `p65-crew-gun` 1,606 lines each, no ERROR, UNREADABLE, or NOSTATE, 32 lines differing, all of kinds the pass changes (6 GameView, 3 WithdrawalDestinations, 21 fire and 2 advance-phase plans whose hashes include the new facts, which the gate's computed `PROOF_SKIP` and `SKIP_JSON` take; this check ran with no skip list).

**Round E, the page and the record lines** (22:36 to about 23:10, 4d53f29f; R5 1, 4 to 11, 13, R4 2, 6, 8, the notes' known items):
- the page: a one-unit list keeps its hex and `Chosen` says "[F5]: name" (R5 1); the Search's `Busy` (R5 4, MAJOR) and its order keyed on game, view, and Location (5); disabled controls' titles give the reason (6); the repeated "in [N5]" stripped (7); the CCPh's crew help with D5.42's EXC, "crew-acts" and "street-fighting" open in the CCPh (8); `DisplayText.Side` (9, R4 6); "Give up "?"" an `ActionSection`, closed at first, refused while an answer waits (10); `@key` (11);
- the record lines: a "?" given up, lost by an A12.141 action (a whitelist of event types), and a "?" placed over hidden units, with Replay's title (R4 2); "an 8-0 leader" (R4 8); the LLMC line in the Records tab's list; one citation, not two, on the Interdiction's Heat of Battle line; the choice text's merged citation; the Search's and Mopping Up's Casualty dr in their lines; the forced back's line; `ChargeBarred`'s words; vehicles read by their printed designation ("PzKpfw IIIH G1"), not "vehicle", everywhere;
- found and fixed by the agent: a transfer's line vanished when the transfer cost a "?";
- the Casualty dr's drm recorded as `casualtyDrm` on `units-searched` and `building-mopped-up`, the lines giving it to the DEFENDER and the adjudicator only: ruling R70.48.
Older tests changed as intended (`UnitGroupingTests`, `CloseCombatComponentTests`, `PlayPagePass26Tests` ("SPW 251/1 G1"), `AStepIntoAVehicleAndInfantryLocationLeavesTheChargeUndecided`). Rules 4 of 4, Play 9 of 9, MapStudio 24 of 25 then the corrected Pass26 test 1 of 1, the icon coverage regenerated then 1 of 1; build exit 0. Page tests: `ALoneUnitInAListKeepsItsHexAndTheClosedLineNamesItAfterIt`, `TheSearchButtonWaitsWhileAProposalIsMade`, `TheCcphReCrewSaysTheCcphsRuleAndAChoiceEndingWithItsRulingCitesR58Once`; Rules `TheCasualtyDrsRecordLineGivesItsDrmOnlyToTheViewThatMayKnowIt`.

The tests the rounds added beside these: Rules `TheViewersPendingOptionIsRefusedToTheMoverAsTheOpenWindowIs`, `AStacksTopIsNeverAUnitAboardAndOnlyAnImmobileVehiclesPrcAreHeld`, `TheMoppingUpProposalSaysOnlyWhatTheDeclarerKnows`, `TheReadsTheBoundaryReviewMovedIntoRulesDecideAsThePlannerDid`, `ABerserkOrReinforcingUnitOwesItsAttackOnEnemyInfantryWhereAVehicleStands`; Play `ACaseReadShowsAStackItMayNotInspectAsTheViewDoes`, `AHistoryViewShowsWhatThePlannersViewShowsWithOrWithoutAMemo`, `ARussianDummyInTheStacksLocationInspectsNothing`, `AConcealedUnitThatTakesNoLlmcIsNotNamedInThePublicRecord`, `AnMmgDroppedToWithdrawStaysBehindWhenItsUnitDiesOnEntry`, `ABerserkSquadBesideItsTankOwesItsAttackOnTheEnemyInfantryThere`.

### 18.3 Not fixed: a row each in the backlog

The backlog's section 72.2 has each: a move step's proposal tells what an entry into a "?" Location finds (`ObstacleDummiesText`; R2 5, its MINOR part); a side's view without a live planner shows an out-of-LOS stack in full (the fallbacks; R6 16); a withheld morale check record tells that a "?" took part (R1 1, R2 3); the DEFENDER's choice not to make a Casualty dr (R70.45); a concealed or hidden enemy allowing a Voluntary Break (R70.43); the rout's "Known armed enemy unit" reading Unarmed Personnel as armed (Round B); A11.13's loss after a vehicle's CC attack (R3 4); the vehicle's attack with or without its Passengers, and A11.52's +2 on that attack only (R3 5; the 3j reading); the morale check verifier's whole-target comparison (R6 17, for a ruling); `ObjectIndex.Of`'s O(n^2) (R6 1's note); `"guns": []` kept (R6 20); the "vice versa" of A12.2's shared "?" for a PRC or a Gun (R6 10); page tests of four controls the page test board cannot hold (R5 13); a "?" lost by a move, an advance, a rout, Smoke, a Withdrawal, or a Bypass with no record line (R4 2's rest); the `casualtyDrm` raw field on a public record (R70.48); a created leader in a sequential Infantry round not counted as having attacked (3j); a Location whose Ambush drs were rolled with no attack left (3i); and the hook's gap at the repository root (Round D). The proof tool's silent binding (R6 19) was fixed by the session.

### 18.4 The Studio check

At 23:11 to 23:20, in my Studio on port 6670 on the build of 4d53f29f. Its scope, decided under the user's word, with pass 65's as the model (the standing rules, section 1, point 4): every pass game replayed; the hidden-information view from the side that must not see; the new controls at 1920, 1024, and 320; a real Enter key on a proposal and its commit; the hex audit in three phases; the measurement once the strict build was done. `gate.sh 70 25aa97fd strict` ran again beside it, 23:12 to 23:16: 0 warnings, 0 errors; the formatter re-wrapped `ScenarioA1Pass70RulesTests.cs` (whitespace only), committed with the notes (9d2e364a).

- **Every pass game replayed:** `tool_check_3a.sh` rebuilt the proof tool against the final build, and `replay_p70.sh` replayed the 41 games, every one to its last event, with no ERROR, UNREADABLE, or NOSTATE line, those recorded before the fix rounds included (from copies in `asl-gate-work/replay-70`, registered; the live folder never read by the tool).
- **The side that must not see,** `p70-inspect-out` in the Russian view at 1920: the hand-over screen names nothing of the [O10] stack; the Units tab "4-6-7 squad in [O10]" and "? German not inspectable: out of the LOS of this side's Good Order units (A12.16) [O10]"; the LMG and the second squad nowhere on the page.
- **1024,** `p70-voluntary-break-leader` (German RtPh): the Voluntary Break list grouped under [O10], the leader first, inside the page (document 1,009 wide); the disabled button's title "Tick the units that break first.". The break, by script (dice 2 3): "9-1 leader G1 ... breaks voluntarily ... (A10.41, A10.62)" and "[O10]: 4-6-7 squad G1 takes LLTC after the break of 9-1 leader G1 to a Voluntary Break (A10.41): LLTC 2, 3 + 1 (leadership reversed, 9-1 leader G1) = 6 against 7: passed, unaffected (A10.2)".
- **320,** `p70-break-unarmed-adjacent` (German RtPh, the Actions tab): document width 320; `#propose-voluntary-break` and `#propose-rout` inside it.
- **The hex audit** (`code-builders/play-ui/hex_audit.js`, every section opened, each view): `p70-mop-up-any-building` (PFPh) German 2 counter choices, Russian 0; `p70-withdraw-concealed` (CCPh) German 0, Russian 1; `p70-vehicle-cc-prc` (APh) Russian 1, German 0: none without a hex.
- **Not done: the real Enter key.** The pane was hidden and its active tab a stale one of main's Studio (closed), so the key went nowhere and nothing was proposed or written (the file read back, 10 events, unchanged); the break above was made by script. Said here as the standing rules ask: a script's click is not a key.
- **Not staged:** `p70-fanatic-ssr` (no card carries the SSR; its Play test sets up through the gate). Not done, as the scope allowed: the five widths in each side's view for every control; a whole Player Turn at 320; the Replay page.
- **Seen by Round E, older than the pass:** `MoppingUpAction`'s unit names read "{unit} in {place}", not hex first; the audit found no choice without a hex.

## 19. As built

**The pass in commits,** on `feature/asl-backlog-pass-70`, cut from 25aa97fd (main, pull request 39): the frame (7d38f69d); 3a (fc150800); 3b (65ebcec8); 3c (4189b73e); 3d (cb3110fb); 3e (e7f60a75); 3f (45d52583); 3g (293dd0b1); 3h (077e6de5); 3i (9601a3b6); 3j (3384c2b2); the reviews (37191207); fix Rounds A to E (8f92714a, 62e01dbf, 03af0037, ab082dc0, 4d53f29f); the whole-pass check (9d2e364a); these documents. Each increment was built by an agent to a brief, its diff read, its game played in the Studio by the session, and its record facts added to `SKIP_JSON` and its games to `OWN` in `pass70.conf`.

**What the plan's entry asked, and where it went.** The plan names 35 inventory rows; the inventory named 48 for pass 70 (the five moved from pass 75 on 2026-10-25 and the eight pass 65 moved). After the pass 29 of them are built (6 of them with a deviation) and 19 moved to the pass that takes what is left: pass 115 5, pass 105 4, pass 120 3, pass 135 2, pass 75 1, pass 80 1, pass 100 1, pass 165 1, pass 180 1. The ten tasks are built as sections 8 to 17 say, each with its departures; R70.2's moves (CC against a wreck to pass 110, Withdrawal to another level to pass 135, the AFV-naming row to pass 75's 75.6, the SMC crew's Stun wound to 75.3, and the backlog's rows) and every departure have a row in the backlog's section 72.1, and the findings not fixed in 72.2. The inventory rows of other rules the pass's code touched have a note of it, and the five stale notes of section 5, item 2 are corrected. The prompt's three wrong lines are corrected in place (section 5, item 4).

**The rulings:** R70.1 to R70.48, decided under the user's word of 2026-10-25 and open to a veto: R70.1 to R70.34 in the frame (R70.17 corrected on the page before any build; R70.13's citation corrected at 3d); R70.35 and R70.36 at 3e, R70.37 and R70.38 at 3f, R70.39 at 3g, R70.40 at 3h after a read-only agent's answer; R70.41 to R70.47 at the reviews (R70.46's three cases worded at Round C); R70.48 at Round E. They are in the Passes Plan's section 5.

**What a record gained** (each absent from every older record, or written only when true, and taken from the record at replay): on a fire attack's targets `unarmed`, `aboard`, `seenByGoodOrderEnemy`; on its PRC entries `vehicleMoving`, `ceInUnmovedHalftrack`; on a vehicle target `crewBroken`; on `rider-bailed-out` `drm` and `elr`; on `rout-stepped` `with`; on `rout-interdicted` `leader`; on a movement step a forced back's `bypass`; on a CC unit `guns`, `rider`, `passengerOf`; on CC facts `sequential`; on Ambush facts `vehicles`; on vehicle CC facts `ambusher`, `passengers`, `streetFighters`, `leaderCreation`, `captors`; on `close-combat-resolved` the round `sequential` with `next`, `closed`, and `captors`; on `vehicle-close-combat-resolved` `createdLeader`; on `vehicle-captured` the cause `passengers`; on a Carrier crew's `instance-created` `crewOf`; on the CCPh-ending `phase-changed` `prcHeldInMelee` (3i) and `prcMelee` (Round A); on `units-searched` and `building-mopped-up` `casualtyDrm`. The new events `morale-check-resolved`, `voluntary-break`, `concealment-verified`, and `units-searched`; the choice kind `concealment-loss`; the actions `asl.game.voluntary-break`, `asl.game.remove-concealment`, `asl.game.search`, and `asl.game.street-fighting-return`; the SSR token `fanatic:<side>`. New state properties computed from records every game holds: `AdvanceRecord.AdvancedFrom`, `CloseCombatLocation.Captors`, `GameState.CapturedThisPhase` (3j).

**What a view gained, never recorded:** the Right of Inspection by LOS (R70.18, R70.19, R70.40) in every view built through `GamePlanner.ViewOf` and, for a history, `HistoryViewOf`; units aboard never the top counter of a sealed stack, before play too (Round A).

**The tests:** `ScenarioA1Pass70RulesTests` (68 methods), `BacklogPass70Tests` (62), and `PlayPagePass70Tests` (12), each increment's and each round's run by method only (the user's rule of 2026-10-23); the Rules, Play, Units, and MapStudio projects were not run whole, which is the gate's.

**The measurement** (section 7), on `p70-perf-tw`, hand-over and "end the phase" proposal in ms, each Studio alone:

| When | The pass | The base |
|---|---|---|
| 3a (12:14 base, 13:00 pass) | hand-over 341, 292, 274, 291; proposal 183 (first), 73, 59, 59, 96 | hand-over 487 (cold), 348, 284, 393, 289; proposal 60, 127, 110, 85, 111 |
| 3a, the cold open of the whole `dl-tractor-works-02` | 1,783, then 390, 302 | 1,485, then 448, 349 |
| 3g, the user's Studio running (17:30) | hand-over 481, 455, 546, 505, 430 (median 481); proposal 72, 60, 57, 63, 72 | hand-over 447, 550, 557, 531, 312, 577 (median 540), taken again at once |
| The whole-pass check, the user's Studio running (23:17) | hand-over 294, 305, 346, 300, 272 (median 300); proposal 551 (first), 74, 56, 68, 63 | hand-over 610, 515, 433, 351, 378, 429, 399, 415 (median about 415); proposal 134, 73, 69, 66, 81 |

The memory of a cold open and two hand-overs (Round D): before the round 255 MB private and 322 MB working set against main's 231 and 291; after it 225 and 297. The pass is level with main or faster, and the design's thresholds (a warm median at most 20% over the base's, a cold open at most 25% slower) hold; the cold open was 20% slower at 3a, the only reading of it.

**The time:** the frame 11:52 to 12:14 on 2026-10-25; the build 12:17 to 20:24 (8:07 by the clock) against the frame's 33:05 and the plan's 7:20; the reviews and their rulings 20:25 to 20:41; the fixes 20:41 to 23:11; the whole-pass check and the measurement 23:11 to 23:20; then these documents. The time log has each row.

**The strict build** (`gate.sh 70 25aa97fd strict`) passed on 3384c2b2 (20:25 to 20:29) and again on the tree after Round E (23:12 to 23:16), 0 warnings and 0 errors each time.

## 20. The gate

Not run. The user's word of 2026-10-25 ("Run it without my intervention, up to the merge gate") ends at the gate: the gate, the push of the pass's branch, its pull request, and the merge are each the user's to start. `pass70.conf` names the pass's 41 recorded games (`OWN`; the measurement copy `p70-perf-tw` is not one), no older games beyond the swept game and the store's (`SWEEP_EXTRA`), the record facts whose keys the proofs skip (`SKIP_JSON`: `unarmed`, `aboard`, `seenByGoodOrderEnemy`, `vehicleMoving`, `ceInUnmovedHalftrack`, `crewBroken`, `guns`, `sequential`, `rider`, `vehicles`, `ambusher`, `passengers`, `passengerOf`, `streetFighters`, `leaderCreation`, `captors`), `BOTH_SKIP` with `GameEvent.RulePackage`, `PRESENT="IsCrew"`, and `ABSENT="VoluntaryBreak"`. The baseline of 25aa97fd was made at the cut (10f070d46067d289: 114 older games); the proof tool changed at 3a and at Round D, and the baseline key holds the tool, so the gate's `baseline` stage remakes it (about 5 minutes). What the gate should expect to see differ, as intended changes: the swept views of stacks out of every Good Order seer's LOS (70.10; R70.18, R70.19) and units aboard no longer a sealed stack's top; new plans whose hashes include the pass's null members (the computed `PROOF_SKIP` and `SKIP_JSON`); who sees in new proposals (R70.16: hidden units, PRC, and empty vehicles no longer seers); must-rout, RtPh DM, and Interdiction by enemy vehicles and Guns (R70.14); the starting building ignored in rout advice (R70.13); refusals the pass turned into plans (a Gun's crew in CC, a Minimum Move into a concealed Location, TPBF in a Bypassed hex, a withdrawal into a concealed Location or above the IPC, Infantry CC in a vehicle's Location, Mopping Up of a building no card names); the withdrawal destinations read from Known enemy units only; vehicles named by their designation in record lines; and the text list's new texts. A pass 70 game holds events main cannot read (`morale-check-resolved`, `voluntary-break`, `concealment-verified`, `units-searched`): on main's side the tool writes UNREADABLE for it and goes on, as expected. The user's `dl-tractor-works-03`, in play during the pass, becomes the swept game only once the user says it is finished (the standing rule on proof games).
