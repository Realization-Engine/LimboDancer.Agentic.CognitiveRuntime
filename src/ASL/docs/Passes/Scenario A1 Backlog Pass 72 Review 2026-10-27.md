# Scenario A1 Backlog Pass 72: Infantry V: Heat of Battle and Unit Substitution

The review of pass 72, written on 2026-10-27 after the four reviews, their two rounds of fixes, and the Studio check of the whole pass. The design is [ASL Unit Backlog Pass 72 Design](<ASL Unit Backlog Pass 72 Design.md>); its sections 8 to 12 say what each increment built, its section 13 holds the reviews, their fixes, and the Studio check, and its sections 14 and 15 the pass as built and the gate.

## Status

Built to the merge gate on branch `feature/asl-backlog-pass-72`, cut from b0a36132 (main): the frame (6787a7c4), five increments in five commits (ce16561e, 955321db, c5cd9717, 993f3169, db15561b), the reviews (3c928950), two rounds of fixes (76cac041, and 1fa0a214 with the whole-pass check), and these documents. Everything was done under the user's word of 2026-10-27, "Run Pass 72 without my intervention up to the merge gate." The rulings R72.1 to R72.22 are decided under that word and open to the user's veto, as are the departures and the passes given to the rows left open. **The gate has not been run:** the word ends at the gate, and the gate, the push, the pull request, and the merge are each the user's to start. The gate's strict stage passed on db15561b and again on 1fa0a214, after the fixes (0 warnings, 0 errors each).

## What departs from the plan's entry and the frame

- **The pass is about 1.7 times the plan's size in building** (design section 2; ruling R72.1). The plan names 10 rows, two tasks, and 1:40 of building (2:55 in all); the calibrated forecast was about 2:50 of building, about 5:20 to the gate. It stayed one pass, in five increments, built in 3:15 by the clock (19:10 to 22:25); no increment was left out, so nothing went to pass 73 for want of time.
- **Moved on** (ruling R72.2): a berserk Dash across a road and a heroic leader's heroic qualities in fire to pass 73; the circled B# of vehicles and Guns and the Low Ammo counter under Ammunition Shortage, and a hero firing an AFV's AAMG as a Rider, to pass 105; a hero firing a Gun, a bazooka, a MOL-Projector, or a RCL to pass 110; Wire and minefields in a charge's route to pass 120; an FFE and OBA's Scarce Ammo to pass 150; a Blaze as a charge bar to pass 185; to the backlog: the ordnance package's Inexperienced B# read by class only.
- **Smaller than the rows said** (design section 5): A15.432's "not verified" items, DC Thrown only, and A19.31's conveyance exception were built before the pass, and the Gun crew's Location no longer ends a charge (R70.27); a hero already fired a light mortar, a PF, and an ATR at a vehicle, with no -1. These were document corrections.
- **Two rulings were made at the build,** R72.15 (5c: an underscored unit with no table entry is Replaced by its HS where it has one) and R72.16 (5d: a tie in the Shortage's Random Selection Replaces every tied MMC, A.9).
- **Six rulings were made at the reviews,** R72.17 to R72.22 (design section 13): R72.17 amends R72.3 and R72.4 (the charge's bars and comparison), R72.18 amends R72.12 (the X# under the Shortage).
- **5a's claim that an ordinary charge charges as before was wrong as built** (the Heat of Battle referee's finding 1): the bar test reached building connections and any refused step on the map. R72.17 narrowed it; older charges re-plan under it, and the gate must name what the swept charge does.
- **TPBF in the firer's own Location was refused in Play** before the pass (5b; frame A had read it as built): it reaches Play now, the halved AFPh TPBF of a berserk unit included.
- **Left out, with a backlog row each (section 75.1 of the backlog):** the moves above.
- **The Studio check of the whole pass was scoped under the user's word,** with pass 71's as the model: the section below.

**Departures of sections 8.1 to 12.1 that the reviews closed,** and so have no row: the bar test's reach (5a; R72.17); the X# under the Shortage, which 5d had lowered with the B# (R72.18); the FT and A19.32, which 5c asked the referee about (the FT's -1 is `AssaultWeaponRemoval`'s, so none is added); the morale check's unstamped `underscoredElr` (5c; R72.19); a PRC killed with its vehicle, which 5b left counted (R72.20); the "The PSK -" name and the proposal's raw DRM names (5e); the LMG's malfunction line with no A19.32 (5c).

## What was built, by task

- **5a, 72.1 part I: the charge** (design section 8; A15.43, A15.431, A15.44, A15.45, E1.533). One search reads every Known enemy target by range group; the next nearest past a cliff or an unbridged Water Obstacle (A15.45); A15.44's Battle Hardening at the MPh's end when only bars keep every Known enemy in LOS out of reach, its owner able to refuse (kind `battle-hardening`, key `berserkHardening:<unit>`); a charge into a Location holding an enemy vehicle and enemy Infantry (`vehicleSides` keeps the unit berserk while the vehicle survives); a unit made berserk in mid move; the dead code, the eager read, the stale comment.
- **5b, 72.1 part II: the return to normal** (design section 9; A15.46, A7.21, A7.212). After AFPh TPBF or a FT that eliminates every Known enemy in the Location, and after CC that destroys an enemy vehicle and leaves no other; TPBF in the firer's own Location reaches Play.
- **5c, 72.2 part I: the SSR tokens, A19.132, and A19.32** (design section 10). `ammo-shortage:<side>` and `underscored-elr:<side>[:<group>]:<n>`; an underscored MMC's SSR ELR (`underscoredElr`), the German 8-3-8 to the 4-3-6 and the 3-3-8 to its HS; the pass's stamp (`unitSubstitution`) and a MG's B# one lower in Inexperienced hands under it (R9.10 lifted for stamped records).
- **5d, 72.2 part II: Ammunition Shortage** (design section 11; A19.131, A.9, A.11, A21.11, A9.22). The Original 12's Unit Replacement by Random Selection (`shortageSelection`, `shortageEffects`); the SW B# one lower, the Original B# its X#; the Depletion Numbers one lower; no Fire Lane.
- **5e, 72.1 part III: the hero's weapons** (design section 12; A15.23, A15.24, C13.25, C13.45). The -1 on a non-MG SW's To Hit DR only; a lone hero's PSK with the two-man +1 negated; the ATR as Small Arms; behind `heroWeapons` on the shot.

## The reviews

Four read-only agents in one message on db15561b: two referees (Heat of Battle; Unit Substitution), a table player who read the events of every `p72-*` game, and a UI, Rules boundary, and memory and performance review. Their brief is `brief_reviews.md` in `code-builders/pass-history/pass-72-heat-of-battle`; each finding was judged by the session against its page and the code, and the findings as reported are `review_r1_heat_of_battle.md`, `review_r2_substitution.md`, `review_r3_table.md`, and `review_r4_ui_boundary.md` in the same folder. Two agents fixed them in two rounds, one commit each (design section 13.2); the session made one more after Round B.

**No BLOCKER.** **Five MAJOR findings, all fixed:**

- **The charge's bar test reached building connections and any refused step on the map** (Heat of Battle 1): bars are a cliff and an unbridged Water Obstacle only, compared only where one lies across the route (ruling R72.17).
- **The comparison used the next group's range even when that group could not be reached** (Heat of Battle 2): against the next group that has a reachable target; with none, the nearest (R72.17).
- **Under the Shortage the X# was lowered with the B#,** so every malfunction of a B11 LMG removed it (Unit Substitution 1; table 2): the Original B# is the X# (A.11's example, p. 43; R72.18).
- **No record line said what a Battle Hardening answer did** (table 1): the record names the unit, its hex, taken or refused, and what it became; Replay names the kind.

**Fixed, the other findings:**

- **The charge** (Heat of Battle 3, 4, 5; table 5 to 8): no hardening without a Known enemy in LOS, nor where other enemy Locations or a gap cut the target off; "still closer" strict, a tie the ATTACKER's choice; the texts name the bar met and cite R30.5 for a gap; the choice names the unit.
- **Unit Substitution** (Unit Substitution 3, 4, 6): a captured Gun's Depletion Number not lowered; the morale check's stamp (R72.19); an underscored squad under both SSRs Replaced by its two HS (R72.22).
- **Heat of Battle's return** (Heat of Battle 7, 8, 10): a vehicle's range 0 fire into its own Location kept as TPBF (R72.21); a destroyed vehicle's PRC not Known enemies left, and CC's check counting Known enemies only (R72.20).
- **The boundary** (Heat of Battle 6; table 11; UI and boundary 4, 9, 11): the bridge, TPBF selection, the hero's weapons, and the Shortage's Replaced firers decided in Rules.
- **The page and the texts** (table 3, 4, 9, 10; UI and boundary 1, 2, 13; the notes' known items): B# and X# words with their causes; the PSK's +1 citing A15.23; "by its TPBF" or "by its FT"; every candidate's dr in the shortage record line; "PSK" with no dash; the hero DRM worded in the proposal; the Fire Lane fields hidden under the Shortage.
- **Memory and performance** (UI and boundary 5 to 8): the berserk test before the index; a lazy dictionary for the cleared-by lookup; the prefix test before a token split; the MPh and berserk tested before a Location is read.
- **Made by the session after Round B** (UI and boundary 3): `vehicleSides` leaves out a hidden or captured vehicle.
- **Read on its page, no code** (Unit Substitution 5): A25.72's Finnish chart (p. 97) holds no Sissi unit.

**Not fixed: a row each in the backlog's section 75.2:**

| Review | What |
|---|---|
| The build (5a), and fix Round A | A bridged hex and a pond hexside read as gaps in a charge |
| The build (5c) | A PRC keeps R15.9's ELR 5 under an SSR ELR |
| The build (5d) | Ordnance has no X# removal under Ammunition Shortage |
| The build (5e) | A7.353's same-phase forfeit of a hero who used a SW; a MG fired as ordnance by a hero |
| Fix Round B (R72.20) | A destroyed vehicle's PRC stay in the state |
| Fix Round B (UI and boundary 13) | A chosen Fire Lane weapon is still sent when its field hides |
| Table player (note 10), seen at the whole-pass check | The Replacement's latest line names only the chosen dr |
| Referee, Heat of Battle (note 9) | The CC and ordnance verifiers accept a post-pass record that drops `vehicleSides` or `heroWeapons` |

Accepted as judged, with no row: one stamp for 5c and 5d (Unit Substitution 7; session games only); the ordnance shot's unstamped `ammunitionShortage` (UI and boundary 10; no token existed before 5c); no unit test of the ordnance SW's lowered B# (5d); no page test of the undecided-charge line (5a).

## The games

In the session's Studio on port 6670, scripted dice; each played game's file read back. The pass's 15 games are `OWN` in `pass72.conf`; the measurement copy `p70-perf-tw` is not.

- **5a** (design 8.3): `p72-charge-next-nearest` (board 7), `p72-charge-unreachable` (board 7), `p72-charge-tank-and-squad`.
- **5b** (design 9.3): `p72-berserk-tpbf-return`, `p72-berserk-vehicle-cc-return`.
- **5c** (design 10.3): `p72-underscored-elr`, `p72-inexperienced-mg`.
- **5d** (design 11.3): `p72-ammo-shortage-replacement`, `p72-ammo-shortage-mmg`, `p72-ammo-shortage-fire-lane`.
- **5e** (design 12.3): `p72-hero-psk`, `p72-hero-atr`, `p72-hero-mortar`.
- **Fix Round B:** `p72-fixb-mmg-x11`, `p72-fixb-rider-cc-return`.

At the whole-pass check every games script was run again and each of the 15 games played to its result in the Studio.

## The tests and the build

Each increment's and each fix round's new Rules, Play, and page tests were run by method only, the user's rule of 2026-10-23, with one slip (5b ran one whole class); the counts are in the design's sections 8 to 13 and the notes. The pass's own classes are `ScenarioA1Pass72RulesTests` (42 methods) and `BacklogPass72Tests` (18); no page test class was needed. The ten new token rows of `BacklogPass17Tests.TheSsrTokensAreCheckedAsANewGameChecksThem` and `ScenarioA1Pass19bTests.EveryPackageKnowsTheCardCounters` read the repository's root and so first run in the gate's suite. The Rules, Play, Units, and MapStudio projects were not run whole: that is the gate's. The strict stage passed on db15561b; each building agent's and each round's build of MapStudio ended with exit 0, and its strict builds of the test projects it changed exit 0.

## The replay digest

Not run on the older games after the tasks; the gate's proofs make it. Every effect that changes what a record made before the pass does is gated by a fact on the record: `unitSubstitution` (the stamp) on a fire attack and a morale check, `underscoredElr`, `ammunitionShortage` with `shortageSelection` and `shortageEffects`, `vehicleSides` and `knownEnemiesOnly` on CC facts, and `heroWeapons` on an ordnance shot; the Battle Hardening choice and the returns to normal are written by new plans only. One change reaches an older game's new plans with no fact: the charge's search (R72.17), which re-plans an older game's berserk charge. Play tests that replay an older record or hold to it: `AFireRecordMadeBeforeTheShortageFactVerifiesAsItWasRecorded`, `ACcRoundRecordsThePassStampAndAnOlderRoundReplaysWithoutIt`, and the Rules test `AConscriptsMgBreaksDownOneLowerWithThePassStampAndAsRecordedWithout`.

## What is open to a veto

- **The rulings R72.1 to R72.22,** in the [Passes Plan](<../ASL Unit Backlog Passes Plan.md>), section 5. All were decided without the user, each by the proposal that would have been put, under the user's word of 2026-10-27. Those most likely to be read again: R72.4 and R72.17 (when a berserk unit is Battle Hardened, and what a rule bar is), R72.9 (R9.10 lifted for stamped records), R72.11, R72.16, and R72.22 (who the Shortage Replaces), R72.13 and R72.15 (the German 8-3-8 to the 4-3-6 Conscript and the 3-3-8 to the 2-3-7), and R72.18 (the X# under the Shortage).
- **The departures** listed above, and the passes this document gives the rows the pass did not finish (the inventory has each row's own pass).

## What the gate will meet

- **New facts in a record** (design section 14): eight keys on records kept as JSON of kinds that existed before, each checked against the store's older records and named in `SKIP_JSON` (`vehicleSides`, `unitSubstitution`, `underscoredElr`, `ammunitionShortage`, `shortageSelection`, `shortageEffects`, `heroWeapons`, `knownEnemiesOnly`). No new event type.
- **Intended differences** (design section 15): new plans of a berserk charge past a bar or with none reachable; a charge into a vehicle-and-Infantry Location; the text list's new and reworded texts.
- **The swept game:** whether the user's newest game, `dl-tractor-works-03`, is finished and becomes the swept game, which costs a new baseline; and, if `dl-tractor-works-02` stays, r-squad-10's charge (revision 1316 to 1317) may differ under R72.17 and must be named in the report.
- **The store** holds none of the pass's games until the gate copies the 15 named in `OWN`.

## The Studio check

At about 23:31 to 23:46 on 2026-10-27, in my Studio on port 6670, on the tree after Round B, its scope decided under the user's word (design 13.4):

- **Every pass game played to its result** (15 games), from the scripts run again.
- **The hardening** declined: "... refuses Battle Hardening, so it is Good Order"; taken at Round A's check: "Replaced by an unbroken 4-5-8 squad (A15.44, A15.3)".
- **The numbers:** the LMG at B10 "less 1 in Inexperienced hands, A19.32"; the MMG under the Shortage malfunctioned on a 10 and removed on an 11.
- **The hex audit,** both views: no counter choice without a hex.
- **320 pixels:** no horizontal scroll on a fire panel and the records.
- **The measurement,** each Studio fresh and the page visible: hand-over median about 320 ms against main's 510, the proposal about 70 against the base's 76; the first readings, taken with the page hidden, set aside.
- **Found:** the Replacement's latest line names only the chosen dr (backlog 75.2).
- **Not done,** as the scope allowed: the five widths in each side's view for every control, a whole Player Turn at 320, and the Replay page.
