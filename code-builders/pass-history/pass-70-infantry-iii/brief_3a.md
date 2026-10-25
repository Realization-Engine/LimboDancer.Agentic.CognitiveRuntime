# Increment 3a: who sees, and the Right of Inspection by LOS (tasks 70.3, 70.10)

Read `brief_build.md` beside this file first; it binds you. The rulings are R70.16 to R70.19 of the design (section 6, F7 and F8) and section 7 (memory and performance). Frame B's report (`frame_sight.md`) maps every read this increment replaces.

## Pages
- p. 76: A12.1 (a "broken" vehicle: no inherent crew and no PRC).
- p. 77 and 78: A12.12, A12.121, A12.122, A12.14, A12.141 (read the whole list and its exceptions), A12.15.
- p. 79: A12.16; A2.9 (wherever it is) for the before-play inspection the view already models.
- p. 80: A12.34 (a Hidden Gun's reveal).

## Build
1. **The Rules member (R70.16).** `ScenarioA1Sight` in Rules:
   - a seer's facts (active, side, Dummy, hidden, aboard, Good Order, unbroken, an empty vehicle, its Location, and whatever the purposes need);
   - `Sees(facts, purpose)` for three purposes: Loss (Good Order, not a Dummy, not hidden, not a Passenger or Rider, not an empty or Abandoned vehicle, A12.14), GainDenial (unbroken, Dummies included, not hidden, not an empty vehicle, A12.122), Inspection (the viewing side's Good Order ground units, hidden ones included, Dummies not, A12.16);
   - the nearest seer: given the seer Locations and a LOS reader (a fact reader declared in Rules, implemented in Play), the nearest range with a LOS or null. "Within 16 and in LOS" is the nearest at 16 or less.
   - Take each LOS read as each caller takes it today (a Nondefinitive, null, or unsupported read is not reinterpreted).
2. **The Play read.** In `PlayStepMemo`, two stores: the distinct seer Locations per (side, purpose), built once per state from `ObjectIndex` (one pass over the units, no `state.Location` per unit); and the nearest seer per (side, purpose, Location). A planner member such as `NearestSeer(state, watchingSide, at, purpose)`.
3. **The callers.** Replace the six reads of frame B's table with the one read, keeping each caller's purpose: movement's `Unmask`, the advance, Rally, a Gun's CA change, ordnance's crew reveal, fire's `WithSeen`, `NearestGoodOrderEnemyInLos` (A12.34; it must no longer count hidden viewers, R35.5), `Watching`/`SeenByEnemy` for Case H, vehicle movement, Opportunity Fire reveals, and the OVR PAATC subjects (Loss: it must no longer count hidden units or Passengers), and the gain watchers (GainDenial; no hidden units, no empty vehicles; drop the duplicate Concealment Terrain list at `ScenarioA1FireFollowUps.cs:78` for `IsConcealmentTerrain`, and the per-candidate `state.At` lambdas). Where a caller's old condition differs from its purpose's, say so in the report: each such difference changes what a new proposal does (never a recorded one; reveals are recorded events).
4. **The actions that cost a "?" (R70.17).** Each built action named in R70.17 loses the acting unit's "?" when the Loss read sees it within 16 (write `concealment-lost` the way fire does); A12.141's exceptions stay free. Find each action's planner and add the loss there; list in the report which you reached and which you could not.
5. **The Right of Inspection (R70.18, R70.19).**
   - Rules decides whether a stack is inspectable: after setup closes, an enemy stack not concealed is inspectable unless no Inspection seer of the viewer has a LOS to it.
   - Play gives the set of uninspected Locations per (state, viewer), memoized in `PlayStepMemo`, with an early exit at the first seer in LOS.
   - `GameView.Of` (Units) takes that set as it takes its out-of-sight set, and shows such a stack as it shows one before play: its top counter, the rest a sealed count (`SealedUninspected`), its equipment withheld. A null set means today's view (Units tests have no map).
   - One Play entry builds every view: find every caller that builds a `GameView` (frame B, section 4: `Play.razor`'s `View` and `SideView`, `GameLibrary.ViewOf`, `UnitNames`, `CaseRead`, the proof tool's sweep in `code-builders/proof-tool`) and route them through it. For the proof tool, change it only as far as it must call the new entry; say what you changed there, since the gate depends on it.
   - The texts: a stack shown this way says it is out of the LOS of the side's Good Order units (A12.16), not "not inspectable before play"; `UnitNames`' wording for such a counter is checked.
   - Events and records do not change.
6. **The page.** What the side that must not see is shown: the top counter of an uninspected enemy stack, and a count. Check the map layer, the unit table, the inspector, the hand-over screen, the target lists (a target list may still name a Location to fire at; it must not name the units beneath the top one).

## Gating
- Nothing recorded changes: reveals are events; the view is derived. Say how you checked that a recorded game replays to the same states.
- The sweep's view lines will differ for 70.10; name the cause in the report.

## Tests (by method)
- **Rules:** each purpose's seer (a Dummy, a hidden unit, a broken unit, a Passenger, an empty vehicle); the nearest seer within and beyond 16; inspectable before and after setup, in and out of LOS.
- **Play:** an unconcealed German stack out of every Russian Good Order unit's LOS shows its top counter only to the Russian view, and in full once a Russian unit sees it; a Hidden Gun not revealed by a hidden enemy (A12.34); a concealed mover not revealed by a hidden unit; one action of R70.17 losing its "?".
- **Blast radius:** frame B's section 7 lists the methods that assert today's view and who sees; run those that your change reaches, by method.

## Studio
`games_3a.py`, on board 3 (board facts: the standing rules, section 4):
- German squads unconcealed in [O10] (wooden building) and [P10] (open), a Russian squad in the woods of [P8] (sees [P10]), and a Russian unit placed where it sees neither (check a pair on `/boards/bd03`'s LOS tab or with the board facts; say which pair you used);
- the same game in a later phase after setup closes, so the Russian view shows the [O10] stack's top only if out of LOS.
Give the hex pairs you rely on for LOS and how you checked them.
