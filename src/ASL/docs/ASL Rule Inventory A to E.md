# ASL Rule Inventory: Chapters A to E

**Status:** First made 2026-10-05, of `main` at 16fb5f0. A working document: a pass that builds a rule changes its row here, as it changes the section's row in the [ASL Rule Coverage](<ASL Rule Coverage.md>).

**What it is.** One row for every numbered rule of Chapters A to E of the registered rulebook (eASLRB v3.01): its title, the physical page of the PDF it is printed on, its status in the code, where the code is, and the pass of the [redesign plan](<ASL Card Play and Map Studio Redesign Plan.md>), section 22.1, that takes it, or the ruling that leaves it out. Section 22.1 is a proposal awaiting the user's approval; until it is approved the pass numbers above 44 are proposals too.

## 1. How it was made

1. **The list of rules comes from the PDF.** The PDF's own outline gave 2,058 numbered entries for pages 43 to 253. A script then read every page's text layer for rule heads (a bold number at the start of a paragraph) and compared. That added 71 rules the outline lacks, removed nothing, and corrected the outline in the places section 5 lists. The page in each row is the page the rule's head is printed on.
2. **Each rule was read on its PDF page and judged against the code and the tests** by 19 read-only audits, one for each group of sections, each working from the PDF's page text (and from rendered pages where a table or figure carried the rule). The repository's transcription was a search aid only.
3. **I checked myself** what the pass assignments and the counts rest on. The rows marked "me" in the last column are the ones where I read the rule's text in the PDF and the code: A.8, A7.21, A16, A17.11, A19.12, A25.2, B1.14, B1.16, B14.4, B16.21, D2.16, and the thirteen rows of D17. I also read in the PDF, without tracing all the code: A.18, A8.2, A8.23, A7.9, A10.533, A11.11, B25.14, C10.1, E.1, and the optional-rule marks of A7.37, B10.211, C13.311, and D11.1. Every other status is an audit's, taken as reported. Every title and page is mine, by script.
4. **No test was run and nothing was built.** "Built" means logic and a test that reaches it were found by reading. Several audits named tests from their method names without opening them; their findings files say which.

**Statuses.**

| Status | Meaning |
|---|---|
| built | The rule's mechanics are in the code as the PDF states them, and a test or a clear call path reaches them. |
| built with a deviation | In the code, knowingly different from the rule: a ruling, a simplification, or a choice the game makes for the player. |
| partly | Some of the rule's mechanics are in the code and some are not. The note says which. |
| refused | The game meets the situation and refuses it with a reason. |
| not built | No implementing logic. A counter, a vocabulary word, a card's text, or a comment alone is not built. |
| not applicable | Nothing to implement: a heading with no rule text of its own, a definition, a pointer, or a table procedure the program replaces. |

## 2. The counts

By this document's rows (script `gen_inventory.py` of the session, over the rows below).

| Chapter | Rows | Built | Built with a deviation | Partly | Refused | Not built | Not applicable |
|---|---:|---:|---:|---:|---:|---:|---:|
| A. Infantry and Basic Game Rules | 579 | 192 | 24 | 157 | 3 | 145 | 58 |
| B. Terrain | 577 | 48 | 7 | 71 | 61 | 308 | 82 |
| C. Ordnance and Offboard Artillery | 324 | 76 | 4 | 69 | 19 | 130 | 26 |
| D. Vehicles | 318 | 56 | 4 | 66 | 4 | 162 | 26 |
| E. Miscellaneous | 329 | 29 | 3 | 28 | 5 | 244 | 20 |
| **A to E** | **2127** | **401** | **42** | **391** | **92** | **989** | **212** |

The 2127 rows are 105 numbered sections, 57 rules of the chapters' introductions (A.1 to A.18 and the like), and 1965 subsections. Of the 1915 rows that carry a rule, 401 are built and 1514 need a pass or a ruling.

**Where the 1514 rows go.**

| Pass or ruling | Rows |
|---|---:|
| Pass 33: Armored combat I (planned) | 33 |
| Pass 34: Armored combat II (planned) | 20 |
| Pass 34b: Night (planned) | 43 |
| Pass 37: Fortifications I (planned) | 74 |
| Pass 38: Fortifications II (planned) | 42 |
| Pass 39: Offboard artillery I (planned) | 36 |
| Pass 40: Air support (planned) | 38 |
| Pass 41: Terrain I: Depressions and water (planned) | 57 |
| Pass 42: Terrain II: buildings and rubble (planned) | 43 |
| Pass 43: Special units: Cavalry and skis (planned) | 36 |
| Pass 44: Fire (planned) | 32 |
| Pass 45: Repairs: wrong results in rules already built (proposed) | 45 |
| Pass 46: Infantry I: movement, stacking, and Locations (proposed) | 49 |
| Pass 47: Infantry II: fire, MGs, and SW (proposed) | 38 |
| Pass 48: Infantry III: morale, Close Combat, and concealment (proposed) | 35 |
| Pass 49: Infantry IV: the small sections, and Interrogation (proposed) | 56 |
| Pass 50: Armored combat III: other attacks on vehicles and Guns (proposed) | 37 |
| Pass 51: Passengers, Riders, and crews (proposed) | 63 |
| Pass 52: Position and cover (proposed) | 43 |
| Pass 53: Armored combat IV: equipment and formations (proposed) | 68 |
| Pass 54: Guns and anti-tank weapons II (proposed) | 77 |
| Pass 55: Transport and unusual vehicles (proposed) | 107 |
| Pass 56: SMOKE, wind, and Environmental Conditions (proposed) | 33 |
| Pass 57: Weather II (proposed) | 25 |
| Pass 58: Offboard artillery II: missions, Bombardment, and Barrage (proposed) | 44 |
| Pass 59: Terrain III: bridges (proposed) | 18 |
| Pass 60: Terrain IV: the refused ground terrain (proposed) | 63 |
| Pass 61: Terrain V: bocage, sewers, and the village (proposed) | 100 |
| Pass 62: Airborne and waterborne (proposed) | 61 |
| Pass 63: Nationalities I: the catalog's counters (proposed) | 14 |
| Pass 64: Nationalities II: new formations and nations (proposed) | 54 |
| Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | 7 |
| Deferred with Chapter H: a DYO chart, purchase, or dr; a card's SSR names the value | 3 |
| Deferred with Chapter F: the rule serves North Africa only | 1 |
| Deferred with Chapter G: the rule serves the Pacific only | 5 |
| Left out: an optional rule by the rulebook's own mark | 6 |
| Stands by its ruling | 8 |
| **All** | **1514** |

## 3. Reading the tables

- **§** is the rule's number with its chapter letter. **Page** is the physical page of the PDF.
- **Where** names the file and member that implement or refuse the rule; **Test** a test that reaches it. Both are empty for a rule that is not built or not applicable.
- **Pass or ruling** is empty for a rule that is built or not applicable.
- **By** says who judged the row: "me", or "audit" for one of the 19 audits.

## 4. The rules

### Chapter A: Infantry and Basic Game Rules

#### A. Introduction (pages 43 to 44)

18 rows: 3 built, 2 built with a deviation, 3 partly, 10 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A.1 | Dice | 43 | not applicable |  |  |  | Definition of dr and DR and of Original and Final; the program rolls the dice and keeps the colored die as the first die. | audit |
| A.2 | Errors | 43 | not applicable |  |  |  | Table procedure (errors stand once play has passed them); the event log already works this way and nothing is to be built. | audit |
| A.3 | Move/Advance | 43 | not applicable |  |  |  | Definition of how the rules use "move" and "advance". | audit |
| A.4 | Optional Rules | 43 | not applicable |  |  |  | Statement that rules marked with an asterisk are optional; each such rule is judged in its own row. | audit |
| A.5 | Attack DRM | 43 | built | ScenarioA1FireCalculator.cs: Resolve (pinnedFinal, per-target DRM); ScenarioA1CloseCombatCalculator.cs: own modifiers; GamePlanner.Terrain.cs: HexsideTemAt, HeightAdvantageAt | AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage; ATiUnitTakesMinusOneInCcAndPrisonersNoLongerBarARallyThatCouldGoBerserk |  | One DR gives each target its own Final DR; a firer's penalty (CX, Encircled, overstack) applies once to the whole attack. | audit |
| A.6 | IN/INTO | 43 | not applicable |  |  |  | Definition of IN and INTO for Depressions; Depression terrain itself is refused in movement (Chapter B rows). | audit |
| A.7 | Good Order | 43 | partly | GamePlanner.SupportWeapons.cs: GoodOrder; ScenarioVictory.cs: armed Good Order tests; GamePlanner.Map.cs: WithSeen | ASquadDeploysWithItsLeaderAndARussianSquadDoesNot | Pass 45 | No single definition. GoodOrder() treats a TI unit as not Good Order and a berserk unit as Good Order, both against A.7; stunned and shocked crews and the Good Order SW sense are not in it. | audit |
| A.8 | ADJACENT | 43 | partly | GamePlanner.Map.cs: IsAdjacent | U23AdvancingFireIsHalvedAndAGroupSpansAdjacentLocations | Pass 46 | Narrower than the rule: needs the same absolute level and no wall or hedge on the shared hexside, so hexes up a hill or across a wall or hedge, and two levels of one building hex, are never ADJACENT. No ruling records it. | me |
| A.9 | Random Selection | 43 | built with a deviation | GamePlanner.Movement.cs: PlanMove (Reveal); GamePlanner.cs: entry Random Selection; ScenarioA1FireCalculator.cs; ScenarioA1CloseCombatCalculator.cs; GameProjector.cs: random-selection | ARandomSelectionAmongTwoSquadsRevealsTheHighestAndForcesTheMoverBack; RandomSelectionTests | Stands by its ruling | Highest dr, all ties, is built. Rulings R10.11 and R27.3: in an ordinary entry Dummies are removed without a dr of their own (only a berserk charge draws them); a SW is never drawn; the Sniper exception is A14. | audit |
| A.10 | Leadership DRM | 43 | not applicable |  |  |  | Definition of the leadership DRM and of the triangle symbol; each marked dr is judged in its own rule (the Recovery dr of A4.44 takes no leadership, as the rule says). | audit |
| A.11 | Permanent Breakdown | 43 | partly | ScenarioA1FireCalculator.cs: WeaponEffects (breakdown table); ScenarioA1FireReference.cs: X# flag | ScenarioA1FirePackageTests (Sustained Fire cases) | Pass 47 | The B# is lowered for Sustained Fire, Inexperienced use, capture, and Extreme Winter, cumulatively. No logic was found that turns the Original B# into an X# during that use (the weapon only malfunctions). Intensive Fire and Ammunition Shortage belong to C and A19. | audit |
| A.12 | White Counters | 43 | not applicable |  |  |  | Describes counters; the removal of markers by phase is judged under A3. | audit |
| A.13 | ATTACKER/DEFENDER | 44 | not applicable |  |  |  | Definition of ATTACKER and DEFENDER (the phasing side in the state). | audit |
| A.14 | Collateral Attacks | 44 | not applicable |  |  |  | Cross-reference to D.8. | audit |
| A.15 | First/Final Fire | 44 | built | LiveFire.cs: fired-this-phase read (First Fire then Final Fire); GameProjector.cs: ChangePhase | EachFireMarkerIsRemovedByThePhaseThatEndsIt |  | A First Fire unit may still Final Fire in the DFPh; a vehicle's separate weapons are Chapter D. | audit |
| A.16 | SMOKE | 44 | not applicable |  |  |  | Definition of SMOKE. | audit |
| A.17 | DRM | 44 | built | ScenarioA1FireCalculator.cs: Resolve (drm sum); ScenarioA1CloseCombatCalculator.cs; ScenarioA1OrdnanceCalculator.cs | ScenarioA1FirePackageTests |  | Every calculator sums its DRM. | audit |
| A.18 | Morale Level Ceiling | 44 | built with a deviation | ScenarioA1FireCalculator.cs: Morale; ScenarioA1CloseCombatCalculator.cs: Leader Creation Morale Level (line 564) | none found | Pass 45 | No general ceiling of 10. A berserk Fanatic unit is given 11 (the code names ruling R30.3), and a Fanatic unit with a printed 10 would be 11; only a hero or heroic leader is capped at 10. | audit |

#### A1 Personnel Counters (pages 44 to 45)

19 rows: 5 built, 2 built with a deviation, 1 partly, 1 not built, 10 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A1 | Personnel Counters | 44 | not applicable |  |  |  | Heading. | audit |
| A1.1 | Counter Types | 44 | not applicable |  |  |  | Introduces the two kinds of Personnel counters; values come from the catalog. | audit |
| A1.11 | Single-Man Counters (SMCs) | 44 | not applicable |  |  |  | Definition of the SMC (leader and hero). | audit |
| A1.12 | Multi-Man Counters (MMCs) | 44 | not applicable |  |  |  | Definition of the MMC; an inherent crew is not a counter until it leaves its vehicle (Chapter D). | audit |
| A1.121 | Squad | 44 | not applicable |  |  |  | Definition of the squad and its Classes. | audit |
| A1.122 | Half-Squad (HS) | 44 | not applicable |  |  |  | Definition of the HS; its broken Morale Level is catalog data. | audit |
| A1.123 | Crew | 44 | not applicable |  |  |  | Definition of the crew; its values are catalog data. | audit |
| A1.2 | MMC Capabilities | 44 | not applicable |  |  |  | Introduces the Strength Factor. | audit |
| A1.21 | Firepower (FP) | 44 | built | ScenarioA1FireReference.cs: definitions (FP, Assault Fire underline, Smoke exponent); ScenarioA1FireCalculator.cs: firepower; GamePlanner.Smoke.cs: SmokeExponent | ScenarioA1FirePackageTests; BacklogPass9Tests (SMOKE) |  | Printed FP, the underlined FP for Assault Fire, the Smoke exponent, and no FP for a leader. | audit |
| A1.22 | Range | 44 | built | ScenarioA1FireCalculator.cs: NormalRange, RangeOf; GamePlanner.Map.cs: Range; FireRange.cs | FireRangeTests |  | Range is the least number of hexes; Normal Range from the catalog. The underlined range's Spraying Fire for squads is A7.34's row. | audit |
| A1.23 | Morale | 44 | built | ScenarioA1FireCalculator.cs: Morale, ELR of 5 for an underscored Morale Factor (line 2830) | ScenarioA1Pass15Tests; ScenarioA1Pass19bTests |  | Morale Level, break on a failed MC, and the underscored Morale Factor. | audit |
| A1.24 | Identity | 44 | not applicable |  |  |  | Counter identity letters; the program uses its own ids. | audit |
| A1.25 | Class | 44 | not applicable |  |  |  | Definition of Class; the catalog carries it and A19, A15, and A4.5 read it. | audit |
| A1.3 | Component Parts | 45 | built | ScenarioA1FireReference.cs: HalfSquadOf, SquadOf; GamePlanner.SupportWeapons.cs: PlanDeploy | TwoGermanSquadsOfThirteenMayDeploy; ScenarioA1Pass19bTests |  | A squad is replaced by the HS the chart names for its Class and type (ruling R19.7). | audit |
| A1.31 | Deployment | 45 | built with a deviation | GamePlanner.SupportWeapons.cs: PlanDeploy, DirectingLeader | ASquadDeploysWithItsLeaderAndARussianSquadDoesNot; RussianSquadsMayNotDeploy; AnOffBoardSquadDeploysWithAnOffBoardLeader | Pass 46 | Rulings R13.4 and R31.4. Built: RPh, leader-modified NTC, one attempt per leader and squad, SW shared out, no other RPh action. Missing: the exceptions for Finns, a Guard of prisoners, unarmed units, and U.S.M.C. 7-6-8s (all need a leader here). | audit |
| A1.32 | Recombine | 45 | built with a deviation | GamePlanner.SupportWeapons.cs: PlanRecombine | TwoHalfSquadsRecombineWithTheirLeader | Pass 46 | Ruling R13.4. Two Good Order HS of one definition with a leader, the Fanatic case, the leader's sole activity. Allowed at any point of the RPh, not only its start; the Finn, Guard, and unarmed exceptions and the "same terrain" test (entrenchments) are missing. | audit |
| A1.4 | Broken Side | 45 | built | ScenarioA1FireReference.cs: BrokenMorale, SelfRally; ScenarioA1RallyCalculator.cs; UnitPlausibility.cs | ScenarioA1RallyPackageTests |  | Broken Morale Level, Self-Rally square, no FP or range when broken; BPV is catalog data. | audit |
| A1.5 | Special Status | 45 | not built |  |  | Pass 64 | No special status by SSR or DYO purchase (H1.22 to H1.24: Assault Engineers, Sappers, Commandos); nothing found by search. | audit |
| A1.6 | Unit Size Number (US#) | 45 | partly | GamePlanner.CloseCombat.cs: UnitSize; ScenarioA1CloseCombatCalculator.cs: US#; GamePlanner.FireExtensions.cs: Concealment dr | BacklogPass14Tests (prisoners); BacklogPass12Tests (Concealment dr) | Pass 46 | Personnel US# (3, 2, 1) is built. The US# of 4 and 5 for horses, other 5/8 inch counters, and large Guns and vehicles is missing. | audit |

#### A2 The Mapboard (pages 45 to 47)

18 rows: 2 built, 3 built with a deviation, 5 partly, 5 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A2 | The Mapboard | 45 | not applicable |  |  |  | Heading. | audit |
| A2.1 | Board Configuration | 45 | built | ScenarioCards.cs: boards, North, Playable; MapLayout.cs; VaslMapBuilder.cs; GamePlanner.CardSetup.cs: PlayableBar | ThePlayableAreaIsTheCardsHexrows; CompositionTests; NoMoveLeavesThePlayableArea |  | Boards butted in the card's configuration, turned boards, North, and the playable area (rulings R17.3, R20.6). | audit |
| A2.2 | Grid Coordinates | 45 | built with a deviation | MapLayout.cs: owners; BoardLocation (Coordinates) | CompositionTests.BoardsStackedVerticallyMergeTheirSeamRowBothWays | Pass 46 | A shared seam hex is named by the board placed later (as VASL does), not by the northeasternmost board. The record suffixes (h, En, Pi, P, R, CR, ca) are replaced by the program's own Location and position notation. No ruling. | audit |
| A2.3 | Half-Hexes | 45 | partly | VaslMapBuilder.cs: half-hex seams; ScenarioSetup.cs: Within; GamePlanner.Victory.cs: PlanExit | CompositionTests | Pass 46 | Edge half-hexes are playable, and a seam hex takes the non-Open Ground terrain. The limit on setting up, entering, or counting occupation in a half-hex butted against a board where the unit is not allowed has no logic: a seam hex simply belongs to the later board. | audit |
| A2.4 | Cumulative Terrain Effects | 45 | partly | GamePlanner.Terrain.cs: GroundStep; ScenarioA1FireCalculator.cs: DRM sum | WallsHedgesAndHillsCostWhatTheTerrainChartSays | Pass 46 | Hexside terrain, SMOKE, and elevation add to the hex's cost and TEM. A hex with two in-hex features (the building and woods of 2I9) is read as one terrain only. | audit |
| A2.5 | Entry | 46 | built with a deviation | GamePlanner.CardSetup.cs: EntryFor, EntryHexesFor, EntryCheck, EntryDue; GamePlanner.Passengers.cs: vehicle entry | ABlockedEntryHexDelaysEntryAGameTurnWithinFourHexes; UnitsThatWaitedEnterByAdvanceAndTheAdvancePhaseHoldsThem | Stands by its ruling | Rulings R20.5, R25.1, R25.2. Entry on the turn, by advance if not in the MPh, blocked entry a turn later within four hexes, never past a river. Deviations: the radius counts from the entry turn whatever stopped the entry, a pond stops it, and rubble or Blaze cutting a hex off is not built. | audit |
| A2.51 | Offboard Setup | 46 | built with a deviation | GamePlanner.CardSetup.cs: EntryHexes; GamePlanner.Movement.cs: EntryStep, EntryGround; ScenarioSetup.cs | GambitsBritishWaitOffBoardAndEnterInTheirMovementPhase; BypassAndMinimumMoveAtEntryButNoSmokeFromOffBoard | Stands by its ruling | Rulings R20.5, R25.3, R25.4. No offboard board: units wait off board from setup, enter from the mirror-image hex at the edge hex's own cost, and take no fire there. Offboard stacks, movement off board, and offboard road hexes are not modelled. | audit |
| A2.52 | Offboard Actions | 46 | partly | GamePlanner.Movement.cs: PlanMove (entry-offboard-action); GamePlanner.SupportWeapons.cs: PlanDeploy; GamePlanner.Vehicles.cs: off-board vehicle in Motion | DeployingOffBoardIsAttemptedWithALeaderWaitingAlongTheEdge; AVehicleEntersFromOffBoardInMotionWithItsPassengers | Pass 46 | No action off board but Deployment; vehicles wait loaded and in Motion. Not checked by me: SW starting dismantled, ordnance limbered, an AFV starting CE or BU as a setup choice. | audit |
| A2.6 | Exit | 46 | built | GamePlanner.Victory.cs: PlanExit; GamePlanner.Recall.cs and GamePlanner.Vehicles.cs: vehicle exit; GameProjector.cs: ExitUnits | UnitsLeaveByAdvanceFromBypassAndAtTheRoadRate; GambitsExitVpWinAtOnce; AVehicleLeavingWithItsPassengersCountsExitVp |  | Exit in the MPh or APh as into the mirror-image hex, one MF more from Bypass, never in the RtPh, no return (rulings R21.5, R25.5). The exit's MF test leaves out the leader and Road bonuses, so it is a little stricter than the rule. | audit |
| A2.7 | Overlays | 46 | not built |  |  | Pass 60 | No overlay can be placed on a board; the word occurs only in the VASL board metadata reader. | audit |
| A2.71 | Cutting Out Overlays | 46 | not applicable |  |  |  | Physical procedure: cutting the overlays out. | audit |
| A2.72 | (none) | 46 | not applicable |  |  |  | Physical procedure: the printed 1 and 2 and fixing the overlay to the board; the orientation they give is A2.73's. | audit |
| A2.73 | SSR Placement | 46 | not built |  |  | Pass 60 | No SSR placement of an overlay by two hexes, no order of overlapping overlays, no "o" coordinates. | audit |
| A2.74 | (none) | 46 | not built |  |  | Pass 60 | No overlay hexsides or extraneous-terrain rule, since no overlay exists. | audit |
| A2.75 | Specific Overlays | 46 | not built |  |  | Pass 60 | No overlay ids or their allowed places. | audit |
| A2.76 | Wadis/Streams | 47 | not built |  |  | Pass 60 | No joining of wadi or stream end-hexes; wadis and streams are themselves refused terrain. | audit |
| A2.8 | Location | 47 | partly | BoardLocation (level); GamePlanner.Terrain.cs: InfantryStep; GameState.cs: At | BacklogPass10Tests (building levels) | Pass 37 | Locations exist for ground and upper building levels, each with its own stack and entry cost, and a leader acts only in his own Location. Sewers, tunnels, caves, bridges, pillboxes, and the in-Location entrenchment distinction are missing. | audit |
| A2.9 | Setup Limitations | 47 | partly | ScenarioSetup.cs: Check, MayDeploy; GamePlanner.CardSetup.cs: CardSetup | NoLocationIsOverstackedAtSetup; NoCounterSetsUpWhereItCouldNotEnter; TwoGermanSquadsOfThirteenMayDeploy; NoRussianSquadSetsUpDeployed | Pass 46 | Built: never overstacked, never in terrain it could not enter, 10 percent (FRU) of squads Deployed on board and per entry turn, only for a nationality that Deploys. Missing: setup in Crest status (B20.9) and HD status (D4.221). Enemy stacks are hidden by the page, not by the rules layer. | audit |

#### A3 Basic Sequence of Play (page 47)

10 rows: 8 built, 2 partly.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A3 | Basic Sequence of Play | 47 | built | GameProjector.cs: ChangePhase, CheckPhase; Phases | TheTractorWorksContinuedPhaseByPhaseIsTheGameReplayedWhole |  | Two Player Turns of eight phases each, in order. | audit |
| A3.1 | Rally Phase (RPh) | 47 | built | GamePlanner.SupportWeapons.cs: RallyPhaseActionBar; GamePlanner.Rally.cs; GameProjector.cs: rally and repair checks | ALeaderWhoDirectedADeploymentRalliesNoOneAndTheDefenderDoesNotDeploy |  | Rally, repair, Deployment, Recombining, Recovery, Transfer; one kind of action per unit per RPh. | audit |
| A3.2 | Prep Fire Phase (PFPh) | 47 | partly | LiveFire.cs; GamePlanner.Fire.cs; GamePlanner.FireExtensions.cs: Opportunity Fire; GamePlanner.MoppingUp.cs: TI | BacklogPass12Tests (Opportunity Fire); BacklogPass24Tests (Mopping Up) | Pass 46 | Prep Fire, Opportunity Firers, and the TI of Mopping Up are built. OBA SMOKE and the other labor tasks (entrenching, clearance) are missing. | audit |
| A3.3 | Movement Phase (MPh) | 47 | built | GamePlanner.Movement.cs: PlanMove; GameProjector.cs: movement-step checks, NoMoveThisPlayerTurn | AUnitForcedBackCannotMoveAgainThisPhase; ResidualFpAttacksAStackEnteringItsHex |  | Units that Prep Fired, were marked for Opportunity Fire, or are TI do not move; the DEFENDER's First Fire follows each MF expenditure. | audit |
| A3.4 | Defensive Fire Phase (DFPh) | 47 | built | LiveFire.cs: Fired; GameProjector.cs: ChangePhase (markers cleared) | EachFireMarkerIsRemovedByThePhaseThatEndsIt |  | Final Fire by unmarked units, and by First Fire units at adjacent targets only; markers removed at the phase's end. | audit |
| A3.5 | Advancing Fire Phase (AFPh) | 47 | built | ScenarioA1FireCalculator.cs: AFPh halving; LiveFire.cs: Prep Fired units barred | U23AdvancingFireIsHalvedAndAGroupSpansAdjacentLocations |  | Half FP except FT, DC, and Opportunity Firers; Prep and Bounding Fire markers removed. | audit |
| A3.6 | RoutPhase (RtPh) | 47 | built | GamePlanner.Rout.cs; GameProjector.cs: rout events | TheRoutPhaseIsNotEndedWhileTheOtherSideMustStillRoutAndItsEndNamesWhatIsLost |  | The phase exists and holds until the routs are made; the rout rules themselves are A10.5's rows (not read in detail by me). | audit |
| A3.7 | Advance Phase (APh) | 47 | built | GamePlanner.CloseCombat.cs: PlanAdvanceUnits; GameProjector.cs: advanced | AnAdvanceMovesUnitsOnceInTheAph; AnAdvanceFromAnUpperLevelIsOfferedNoGroundLevelLocation |  | One hex or one Location within the hex, never both, even into an enemy Location. | audit |
| A3.8 | Close Combat Phase (CCPh) | 47 | built | GamePlanner.CloseCombat.cs; GameProjector.cs: ChangePhase (pins and TI removed, Melee) | U33ASquadAdvancesIntoAKnownEnemyTheyCloseCombatAndTheSurvivorsAreInMelee |  | CC resolved per Location, survivors in Melee, Pin and TI counters removed. The "?" gained at the phase's end (A12.12) is A12's row. | audit |
| A3.9 | Turn Record Chart | 47 | partly | ScenarioCards.cs: ScenarioCardTurns, LastPlayerTurn; GamePlanner.cs: game end; GameProjector.cs: End | AHalfTurnEndsAfterTheFirstSidesPlayerTurn | Pass 46 | Turn count, the half last turn, and the end are built; the first move by a dr is manufactured (ruling R20.2). Provisional (gray) turns set by SSR and a chart that runs past END are missing. | audit |

#### A4 Infantry Movement (pages 48 to 52)

36 rows: 10 built, 4 built with a deviation, 20 partly, 1 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A4 | Infantry Movement | 48 | not applicable |  |  |  | Heading with a pointer to the Index. | audit |
| A4.1 | Basic MPh | 48 | built | GamePlanner.Movement.cs: PlanMove; GameProjector.cs: movement-step checks | BacklogPass10Tests; AUnitForcedBackCannotMoveAgainThisPhase |  | Infantry that did not Prep Fire and is not broken, TI, an Opportunity Firer, or in Melee moves up to its MF, over friendly units. | audit |
| A4.11 | Movement Factor (MF) | 48 | partly | Experience.cs: MoveAllowance, MfAllowance; GamePlanner.Movement.cs: MfAllotment | ExperienceTests; DoubleTimeAddsMfMakesTheUnitCxAndPortageCountsAgainstIts | Pass 46 | Four MF (three if Inexperienced), six for a SMC (three if wounded), the Road bonus. The exception that sets a SMC to four MF (and a wounded SMC or Inexperienced Infantry to four) when it mounts or dismounts a conveyance is not applied in the load and unload actions. | audit |
| A4.12 | Leader Bonus | 48 | built | GamePlanner.Terrain.cs: LeaderBonus; GamePlanner.CloseCombat.cs: AdvanceAid; GameProjector.cs: MovedWith | BacklogPass10Tests (leader bonus) |  | Two MF for a MMC that starts with and moves every step with a Good Order leader of its nationality, in the MPh and the APh (ruling R10.8). Wire, entrenchment, and paddy status do not exist. | audit |
| A4.13 | Terrain Effects | 48 | partly | GamePlanner.Movement.cs: EntryHalfMf, InfantryEntryHalfMf; GamePlanner.Terrain.cs: InfantryStep, GroundStep | WallsHedgesAndHillsCostWhatTheTerrainChartSays; MarshTakesTheWholeAllotment | Pass 46 | Chart costs for Open Ground, orchard, brush, woods, grain, buildings, rubble, and marsh. Every other terrain is refused with a reason (ruling R10.1), so the Desert and PTO charts and most of Chapter B are out. | audit |
| A4.131 | Hexside Costs | 48 | partly | GamePlanner.Terrain.cs: GroundStep, WallOn | AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap | Pass 46 | Wall and hedge add one MF. Any other hexside terrain, cliffs, and slopes are refused. | audit |
| A4.132 | Road | 48 | built with a deviation | GamePlanner.Terrain.cs: GroundStep (road); GamePlanner.Fire.cs: FireMapFacts (TargetTerrain) | TheRoadBonusAddsOneMfToAMoveAlongTheRoadOnly; AnAbruptElevationChangeIsCrossedByRoadAtItsB1051Cost | Pass 46 | Ruling R10.1: a road hexside is always crossed at the road rate; the mover cannot choose the other terrain's cost. Fire then reads the hex's own terrain, so the two target points and the FFMO of a road-rate entry into a woods or building road hex are not built (see the findings, part 3). | audit |
| A4.133 | Elevation Change | 48 | built | GamePlanner.Terrain.cs: GroundStep (rise) | HillsDoubleTheCostUpAndAbruptChangesAddPerLevel |  | Cost doubled one level up, nothing extra going down, Abrupt Elevation Changes (ruling R10.3). Cavalry, bicycles, skis, and Ground Snow belong to other sections. | audit |
| A4.134 | Minimum Move | 48 | built with a deviation | GamePlanner.Movement.cs: PlanMove (minimumMove); GameProjector.cs: Minimum Move checks, pinned and CX at window close | AMinimumMoveEntersOneHexAndLeavesTheUnitPinnedAndCx; AMinimumMoveEntersAHexCostingMoreThanTheAllotment | Stands by its ruling | Ruling R10.9: a Minimum Move is the unit's first and only step. Refused into a Location of concealed enemy units and in Bypass, which the rule does not forbid. | audit |
| A4.14 | Enemy Units | 49 | partly | GamePlanner.Movement.cs: PlanMove (move-occupied); GamePlanner.cs: A4.14 case; ScenarioA1OccupiedPackage.cs | AnEntryIntoAKnownEnemySquadIsProhibitedByA414; EnteringAConcealedEnemyForcesTheStackBackAndDummiesAreRemoved | Pass 46 | No MPh entry into a Known enemy unit's Location; the berserk exception is built. The Human Wave, Disrupted, and Unarmed exceptions, and PRC or Cavalry dismounting there, are missing. | audit |
| A4.15 | Infantry OVR | 49 | partly | GamePlanner.cs: PlanDeclareOverrun, OVR NTC election; ScenarioA1OvrNtcPackage.cs; GamePlanner.Movement.cs: move-occupied refusal; GamePlanner.VehicleCloseCombat.cs: berserk OVR | APassedNtcRevealsTheOtherUnitByRandomSelectionAndForcesTheMoverBack; ABerserkChargeOverrunsALoneSmcAndRevealsConcealedUnitsOnItsRoute; DeclareOverrunTests | Pass 46 | Entry onto a lone Known SMC is refused. The one reviewed building case rolls the NTC with the TEM DRM and four MF, but only where another concealed unit then forces the mover back; a passed NTC onto a truly lone SMC is refused as unreviewed. A berserk OVR (no NTC, no doubled MF) is built (ruling R27.3). The leader's NTC for his stack is missing. | audit |
| A4.151 | SMC Options | 49 | partly | GamePlanner.Fire.cs: Defensive First Fire in the DEFENDER's window; GamePlanner.VehicleCloseCombat.cs: berserk OVR | ABerserkOverrunOfALoneSmcHasItsCcAtOnceInTheMph | Pass 46 | The SMC may fire at a berserk OVR through the ordinary window. Its option to move away to an Accessible Location of the ATTACKER's choice, and the limits on that choice, are missing. | audit |
| A4.152 | CC | 49 | partly | GamePlanner.CloseCombat.cs: OVR CC in the MPh; GameProjector.cs: infantryOverrun CC; LiveCloseCombat.cs | ABerserkOverrunOfALoneSmcHasItsCcAtOnceInTheMph; ALoneBerserkLeaderEntersAnSmcsLocationWithoutAnOverrunCc | Pass 46 | Immediate CC in the MPh and Melee for the survivors are built for a berserk OVR only (ruling R27.3). Moving on with the MF left after the SMC falls, other units then transiting, and a second OVR against a SMC in Melee are missing. | audit |
| A4.2 | Mechanics of Movement | 49 | built | GamePlanner.Movement.cs: PlanMove (move-order, move-window), PlanEndMove, PlanPassFire; GameProjector.cs: MovementState | AUnitDropsAndRecoversASwDuringItsMove; MovementEndsForThePhaseOnly |  | MF spent step by step, a stack moving together or splitting, no other unit moving until the stack ends, no taking a move back. Column and Armored Assault are other sections. | audit |
| A4.3 | Bypass | 49 | partly | GamePlanner.Terrain.cs: MoveEntry, BypassStep; GamePlanner.Movement.cs: PlanEndMove | BypassMovesAlongTheOpenHexsidesOfAWoodsHex; BypassRefusesWallsFriendsSplitsAndEnemyObstacles | Pass 52 | Ruling R10.7: ground-level Bypass of woods and buildings where the obstacle does not touch the hexside, not past an armed Known enemy unit. Refused: a hex with any wall or hedge, a hex holding friendly units, a SMOKE or DC attempt in Bypass, splitting the stack, occupying an obstacle that holds enemy units. Ablaze obstacles, Wire, and mines do not exist. | audit |
| A4.31 | (none) | 49 | partly | GamePlanner.Terrain.cs: BypassStep (lane cost), MoveEntry (exit by the far vertex) | BypassMovesAlongTheOpenHexsidesOfAWoodsHex; ABerserkChargeTakesTheShorterRouteInBypass | Pass 52 | One or two contiguous hexsides at the other terrain's cost, doubled to a higher level. More than two hexsides at double cost and continuing Bypass in the same hex are refused; the building and woods hex exception is not built. | audit |
| A4.32 | Broken in Bypass | 50 | partly | GamePlanner.Movement.cs: PlanEndMove (end-move-bypass); GamePlanner.Terrain.cs: MoveEntry (occupy); GamePlanner.Fire.cs: FireMapFacts (lane terrain) | BypassRefusesWallsFriendsSplitsAndEnemyObstacles; BypassGainsNoControlButOccupyingDoes | Pass 52 | No voluntary end in Bypass; occupying costs the full obstacle cost. A unit that breaks in Bypass is fired on in the lane's terrain while the stack's move lasts; I did not confirm FFNAM along the hexsides already traversed. | audit |
| A4.33 | Pinned in Bypass | 50 | partly | GamePlanner.Fire.cs: FireMapFacts (lane terrain); GamePlanner.Movement.cs: PlanEndMove | none found | Pass 52 | A unit pinned in Bypass stays in the open portion for the MPh and is in the obstacle afterwards. The loss of concealment by enemy units in the Location at the end of that MPh (12.151) is not built (ruling R10.7 says so). | audit |
| A4.34 | Bypass LOS | 50 | partly | GamePlanner.Fire.cs: FireMapFacts (fire-bypass, fire-bypass-los) | BypassRefusesWallsFriendsSplitsAndEnemyObstacles | Pass 52 | Fire at a Bypassing stack needs a center LOS that crosses a Bypassed hexside and takes FFMO and FFNAM in the lane's terrain. The LOS to the Bypass vertices is refused, as are fire from within the hex, Snap Shots, and any hex with a wall or hedge. The Crest Line clause is missing. | audit |
| A4.4 | Portage | 50 | partly | GamePlanner.Movement.cs: Portage, PortageOf, MfAllotment; GamePlanner.SupportWeapons.cs: PlanDrop, PlanRecover | DoubleTimeAddsMfMakesTheUnitCxAndPortageCountsAgainstIts; AUnitDropsAndRecoversASwDuringItsMove | Pass 46 | PP per item from the catalog, pick up and drop during the move. Missing: the portage cost is not kept once a SW is dropped mid-move (the MF come back), and nothing stops an item being portaged twice in a phase. | audit |
| A4.41 | AFPh SW Fire Limits | 50 | partly | LiveOrdnance.cs: light mortar check; GameProjector.cs: MovedWeapons; ScenarioA1FireCalculator.cs: no Multiple ROF in the AFPh | AMortarKeepsItsMinimumRangeAndDoesNotFireFromABuildingOrAfterMoving | Pass 46 | Built for the light mortar only, and a MG keeps no ROF in the AFPh. A MMG, HMG, INF or RCL SW, or a pushed Gun that moved may still fire in the AFPh; the German dismantled MMG or HMG firing as a LMG is missing. | audit |
| A4.42 | Inherent Portage Capacity (IPC) | 50 | built with a deviation | GamePlanner.Movement.cs: MfAllotment; GamePlanner.Terrain.cs: LeaderIpcRecipient; GamePlanner.SupportWeapons.cs: RoutLoadOf | DoubleTimeAddsMfMakesTheUnitCxAndPortageCountsAgainstIts; ABrokenUnitDropsOnlyWhatItCannotCarry | Pass 46 | IPC of three and one, one MF per PP over it, a SMC never over two PP, a broken unit held to its IPC. Ruling R10.8: the leader's IPC goes by itself to the one laden MMC of his stack; the player does not choose the unit, and no other SMC lends it. | audit |
| A4.43 | Possession | 50 | partly | GamePlanner.SupportWeapons.cs: Held, PlanDrop; GameTypes.cs: Holding | ASwIsTransferredDroppedAndRecovered; PossessedEquipmentIsDrawnWithItsHolderAndDroppedEquipmentAlone | Pass 46 | Possession, firing only when possessed, dropping in the MPh, the APh, and at the start of the CCPh (ruling R13.5). Possession is recorded by holder, not by stack order. The removal of unpossessed SW in marsh, water, a Blaze, or a rubbled building is missing. | audit |
| A4.431 | Transfer | 50 | built | GamePlanner.SupportWeapons.cs: PlanTransfer, PlanDeploy (SW shared out) | ASwIsTransferredDroppedAndRecovered |  | Between Good Order unpinned units of one Location, in the RPh or at the start of the APh, never in the phase of Recovery (ruling R13.5). Wire, Panji, Crest, and Motion vehicle cases do not exist. | audit |
| A4.44 | Recovery | 50 | partly | GamePlanner.SupportWeapons.cs: PlanRecover; GameProjector.cs: recovery-attempted | AUnitDropsAndRecoversASwDuringItsMove; RecoveryAtNight | Pass 46 | dr below 6 with +1 CX and +1 night, sole RPh action or one MF in the MPh, once per SW, no armed Known enemy there, a SMC from a friendly broken unit (ruling R13.5). Missing: the SMC's immediate Recovery when a unit is eliminated, surrenders, or routs away; the bar on a Bypassing unit; Recovery aboard vehicles. | audit |
| A4.5 | Double Time | 51 | partly | GamePlanner.Movement.cs: PlanMove (doubleTime), MfAllotment; GameProjector.cs: Double Time checks | DoubleTimeAddsMfMakesTheUnitCxAndPortageCountsAgainstIts; NoDoubleTimeAtNvrZero | Pass 46 | Two MF at the start or one after MF were spent, CX, eight MF at most (seven for Conscripts), not for a broken, wounded, or CX unit (ruling R5.1). Not enforced: no Double Time for a unit that mounts, rides, or dismounts in that Player Turn. Wire does not exist. | audit |
| A4.51 | Counter Exhaustion (CX) | 51 | built with a deviation | ScenarioA1FireCalculator.cs: cx DRM; ScenarioA1CloseCombatCalculator.cs; ScenarioA1OrdnanceCalculator.cs; GamePlanner.Smoke.cs; GamePlanner.SupportWeapons.cs: Recovery drm; GameProjector.cs: CX removal | AnAdvanceIntoDifficultTerrainMakesTheUnitCxAndAnAlreadyCxUnitMayNotMakeIt; AUnitCarryingMoreThanItsIpcMayNotWithdrawAndABerserkUnitLosesItsCx | Pass 46 | Rulings R5.2, R5.3. The +1 on attacks, To Hit, CC, Ambush, SMOKE grenade, and Recovery, and -1 to CC against it, are built. The counter leaves when the unit breaks or at the start of its next MPh, not at the earlier moments the rule lists; Search and SW availability drs are missing. | audit |
| A4.52 | Portage Effects | 51 | built | GamePlanner.Movement.cs: MfAllotment (IPC one less while CX) | DoubleTimeAddsMfMakesTheUnitCxAndPortageCountsAgainstIts |  | As the rule and its examples state (ruling R5.4). | audit |
| A4.6 | Movement Modifiers | 51 | built | ScenarioA1FireCalculator.cs: Resolve (ffnam, ffmo); GamePlanner.Fire.cs: FireMapFacts | ResidualFpAttacksAStackEnteringItsHex; ScenarioA1FirePackageTests |  | FFNAM without Assault Movement and FFMO in Open Ground with no Hindrance, TEM, or AFV cover, in Defensive First Fire. Minefields and trenches do not exist; FFNAM for units loading or unloading was not checked by me. | audit |
| A4.61 | Assault Movement | 51 | partly | GamePlanner.Movement.cs: PlanMove (assault checks) | BacklogPass10Tests; ScenarioA1FirePackageTests | Pass 46 | Declared before the move, one Location, never all the MF, never with Double Time or a pushed Gun or by a berserk unit. Missing: the exception for extra MF spent inside a Location (a SMOKE or DC attempt and then the Assault move is refused, as is any second step), and the loss of Assault status when the unit breaks or goes berserk. | audit |
| A4.62 | Hazardous Movement | 51 | partly | ScenarioA1FireCalculator.cs: hazardous-movement; GamePlanner.Fire.cs: FireMapFacts (PushedGun) | BacklogPass10Tests (pushing a Gun) | Pass 46 | The -2 with no FFMO or FFNAM until pinned is built for a crew pushing its Gun, in the MPh only (ruling R10.8). Clearance, paratroops, Fording, Set DC, Climbing, sewer movement, and PRC Survival are missing, as is the DRM in later fire phases. | audit |
| A4.63 | Dash | 51 | not built |  |  | Pass 46 | No Dash: no declaration, no Area Fire against the unit in the road Location, no CA or Case J limit. Nothing found by search for the word or the rule number. | audit |
| A4.7 | Advance Phase | 52 | built | GamePlanner.CloseCombat.cs: PlanAdvanceUnits; GamePlanner.Terrain.cs: InfantryStep | AnAdvanceMovesUnitsOnceInTheAph; AnAdvanceFromAnUpperLevelIsOfferedNoGroundLevelLocation |  | One hex, or one level in a stairwell hex, by Infantry not broken, pinned, TI, or in Melee. Entrenchments do not exist. | audit |
| A4.71 | vs an AFV | 52 | built | GamePlanner.CloseCombat.cs: PlanAdvanceUnits (PAATC), AddPaatc, NeedsPaatc | InfantryAdvanceOnAnAfvAfterAPaatcAndFightItInSequentialCc |  | A PAATC before advancing onto an enemy AFV (ruling R11.17). | audit |
| A4.72 | vs Difficult Terrain | 52 | partly | GamePlanner.CloseCombat.cs: DifficultAdvance, PlanAdvanceUnits | AnAdvanceIntoDifficultTerrainMakesTheUnitCxAndAnAlreadyCxUnitMayNotMakeIt | Pass 46 | Four MF or all the non-Double Time allotment, CX, barred to a CX unit, no advance with no MF after portage (ruling R5.5). The cost tested includes the one MF of SMOKE, which the rule excludes. Climbing and Deep Stream do not exist. | audit |
| A4.8 | Temporarily Immobilized (TI) | 52 | built | GamePlanner.Movement.cs: move-halted; GamePlanner.CloseCombat.cs: advance bar; LiveFire.cs: TI bar; ScenarioA1CloseCombatCalculator.cs: TI DRM; GameProjector.cs: ChangePhase | ATiUnitTakesMinusOneInCcAndPrisonersNoLongerBarARallyThatCouldGoBerserk |  | No move, advance, or fire; +1 and -1 in CC once per attack; removed at the end of the CCPh (ruling R14.3). Removal when the unit breaks was not confirmed by me. | audit |

#### A5 Stacking Limits (pages 52 to 53)

12 rows: 1 built, 1 built with a deviation, 6 partly, 3 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A5 | Stacking Limits | 52 | not applicable |  |  |  | Heading. | audit |
| A5.1 | Infantry/Cavalry | 52 | built | ScenarioSetup.cs: Check (stacking); GamePlanner.CardSetup.cs: EntryCheck; GamePlanner.CloseCombat.cs: OverstackExcess; LiveOrdnance.cs: Excess | NoLocationIsOverstackedAtSetup; AnAdvanceMayOverstackTheLocation |  | Three squad-equivalents and four SMC per side per Location, each level its own; never exceeded at setup or entry, exceeded in play at a price (rulings R19.3, R14.10). Cavalry does not exist. | audit |
| A5.11 | Movement | 52 | partly | GamePlanner.CloseCombat.cs: PlanAdvanceUnits (excess) | AnAdvanceMayOverstackTheLocation | Pass 46 | The extra MF per excess squad-equivalent is charged in the APh only. A MPh entry that overstacks a Location costs nothing extra, and vehicles pay no MP for overstacked Personnel. | audit |
| A5.12 | Attack Penalty | 52 | partly | ScenarioA1CloseCombatCalculator.cs: Overstack; ScenarioA1OrdnanceCalculator.cs and ScenarioA1AreaCalculator.cs: overstack-firer; LiveOrdnance.cs: Excess | BacklogPass14Tests; ScenarioA1Pass8Tests | Pass 46 | The +1 per excess is built for CC and the ordnance To Hit DR. It is missing on the IFT DR (small arms, MG, vehicle MG), and vehicles beyond one are never counted, though ruling R11.6 says they are. | audit |
| A5.13 | Defense Penalties | 52 | not built |  |  | Pass 46 | No logic limits the penalties to the moving units during the MPh; the ordnance DRM is taken from the whole side's stack in the Location. | audit |
| A5.131 | Personnel | 52 | partly | ScenarioA1CloseCombatCalculator.cs: vs-overstacked; ScenarioA1OrdnanceCalculator.cs and ScenarioA1AreaCalculator.cs: overstack-target | BacklogPass14Tests; ScenarioA1Pass8Tests | Pass 46 | The -1 per excess is built for CC and the ordnance To Hit DR, not for IFT attacks. | audit |
| A5.132 | Vehicular | 52 | not built |  |  | Pass 46 | No hit on another vehicle of a vehicle-overstacked hex (rulings R8.10 and R11.6 leave it out). | audit |
| A5.2 | Vehicular | 52 | built with a deviation | GamePlanner.VehicleTerrain.cs: entry cost per vehicle and wreck | AWreckRaisesAVehiclesEntryCost | Pass 46 | Ruling R11.6: any number of vehicles share a Location at an MP cost. The attack penalty for a second vehicle is not in the code. | audit |
| A5.3 | PRC | 52 | partly | GameState.cs: At (Passengers left out); GamePlanner.Passengers.cs: CapacityBar | AVehicleEntersFromOffBoardInMotionWithItsPassengers | Pass 46 | Passengers do not count against the Location's limits and never exceed the vehicle's capacity (ruling R26.2). Riders are missing. | audit |
| A5.4 | Combined Arms | 53 | partly | GamePlanner.CloseCombat.cs: OverstackExcess; LiveOrdnance.cs: Excess; ScenarioSetup.cs: Check | NoLocationIsOverstackedAtSetup | Pass 46 | Holds by construction: vehicles, SW, and Guns are never counted against Personnel limits. The limit of three Guns in use follows from A5.5's crew counting as a squad, which is applied at setup only. | audit |
| A5.5 | Equivalents | 53 | partly | ScenarioSetup.cs: Check; GamePlanner.CloseCombat.cs: OverstackExcess; ScenarioA1CloseCombatCalculator.cs: Overstack; GamePlanner.Passengers.cs: CapacityBar | NoLocationIsOverstackedAtSetup; AnAdvanceMayOverstackTheLocation | Pass 46 | Five SMC equal a HS, two HS or crews a squad, four SMC none. A crew or HS manning a Gun counts as a squad at setup only (ruling R26.3); in play it counts half. Two HS standing in for an SSR squad is built for HIP only. | audit |
| A5.6 | Location Restrictions | 53 | not built |  |  | Pass 37 | No pillbox, entrenchment, sewer, or tunnel exists in play, so no capacity for them. | audit |

#### A6 Line of Sight (LOS) (pages 53 to 54)

15 rows: 11 built, 1 built with a deviation, 2 partly, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A6 | Line of Sight (LOS) | 53 | not applicable |  |  |  | Heading. | audit |
| A6.1 | Checking LOS | 53 | built | LosCalculator.cs: Check; GamePlanner.Map.cs: Los | LosTests |  | LOS from hex center to hex center against the terrain depictions, a port of VASL's LOS. The dr for a disputed thread is not needed. | audit |
| A6.11 | LOS Checks | 53 | built with a deviation | GamePlanner.Fire.cs: blocked firers; ScenarioA1FireCalculator.cs: Blocked; GamePlanner.Consequences.cs | FireAtABlockedLosStillCountsAsFire | Stands by its ruling | Ruling R31d.4: LOS may be checked freely at any time, against the rule. Ruling R12.2: a blocked attack still marks its firers, causes no DM, and its DR decides Multiple ROF only; Random Events (a Sniper check, a malfunction) are not made on it. The limit on LOS checks before setup is missing. | audit |
| A6.12 | Atypical LOS | 53 | partly | GamePlanner.Map.cs: LosToHexside; GamePlanner.Fire.cs: FireMapFacts (Snap Shot) | ASnapShotIsHalvedAndLeavesNoResidualFp | Pass 52 | LOS to a hexside's two ends is built for Snap Shots. The road target point (4.132), Bypass vertices (4.34, D2.32), Climbing, Underbelly Hits, and Rowhouse movement are missing or refused. | audit |
| A6.2 | Obstacles | 53 | built | LosCalculator.cs: terrain height rules | LosTests; ARowhouseWallBlocksLosThroughIt |  | Obstacles by height, LOS into and out of but not through, the firer's own hex never blocking from its center. | audit |
| A6.21 | Half-Level Obstacles | 53 | built | LosCalculator.cs: half-level terrain rule | LosTests.HalfLevelHindrancesAreCountedOncePerRange |  | Half-level obstacles block same-level LOS and make no Blind Hexes. | audit |
| A6.3 | Depressions | 53 | built | LosCalculator.cs: depression rule | AHexsideLocationInADepressionIsOneLevelHigher; DepressionsAreOneLevelDown |  | One level of height per hex of range, with the continuous Depression exception. A bridge in the Depression rule returns "unsupported". | audit |
| A6.4 | Blind Hexes | 53 | built | LosCalculator.cs: CheckBlindHexRule, IsBlindHex | LosTests.ACliffHexsideMakesTheHexBelowItBlind |  | Follows VASL's blind hex test. Bocage, some slope and hillock cases, and factory hexsides return "unsupported". | audit |
| A6.41 | (none) | 53 | built | LosCalculator.cs: IsBlindHex | LosTests |  | One more Blind Hex per five hexes of range, inside VASL's formula; I read the function's start, not every line of it. | audit |
| A6.42 | (none) | 53 | built | LosCalculator.cs: IsBlindHex (elevation advantage) | LosTests |  | One fewer Blind Hex per level of advantage over one. | audit |
| A6.43 | (none) | 53 | built | LosCalculator.cs: IsBlindHex (ground level of the hexes behind) | LosTests |  | The level of the hex behind the obstacle adds to or takes from the Blind Hexes; not read line by line by me. | audit |
| A6.5 | Reciprocity | 54 | built | LosCalculator.cs: IsBlindHex (the ends are swapped for a rising LOS) | none found |  | Holds by construction: one geometric walk serves both directions. No test asserts it, and one case is asymmetric on purpose (a tunnel Location in the same hex, kept as VASL has it). | audit |
| A6.6 | Units | 54 | built | LosCalculator.cs (terrain only); GamePlanner.Wrecks.cs: VehicleHindrance | AnAfvBetweenFirerAndTargetIsAHindrance |  | Personnel never block LOS; an AFV or wreck hinders (ruling R6.2). | audit |
| A6.7 | LOS Hindrance | 54 | partly | LosCalculator.cs: hindrance breakdown; GamePlanner.Fire.cs: FireMapFacts (mapRanges); GamePlanner.Wrecks.cs; ScenarioA1FireCalculator.cs: los-hindrance, ffmo | TheBreakdownKeepsEachRangesHindranceAndTheFirstPoint; AnAfvInAGrainHexAddsItsHindranceToTheGrains | Pass 60 | The LOS read reports every map Hindrance, one per range. Fire decides brush, grain in season, same-level marsh, an AFV or wreck, a burning wreck's smoke, and SMOKE grenades; any other Hindrance on the LOS (orchard, huts, towers, and so on) makes the attack refused as unattributed. A Hindrance negates FFMO. Spotting and Artillery accuracy are C1. | audit |
| A6.8 | (none) | 54 | built | LosCalculator.cs: building restriction rule; GamePlanner.Map.cs: IsAdjacent (needs a clear LOS) | LosTests |  | Adjacent hexes are checked for LOS like any others. The Bypass case is A4.34's row. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |

#### A7 Fire Attacks (pages 54 to 58)

53 rows: 29 built, 14 partly, 7 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A7 | Fire Attacks | 54 | not applicable |  |  |  | Heading; its rule text starts at A7.1. | audit |
| A7.1 | (none) | 54 | built | LiveFire.cs: FromState, Fired, FireSpent; ScenarioA1FireCalculator.cs: Outside, FirerMayFire, WeaponMayFire | U23ASquadMarkedPrepFireMayNotFireInTheAfph; DefensiveFireIsByTheNonPhasingSideAndMarksFinalFire |  | The four fire phases by side, and one fire phase a Player Turn for a unit, a SW, and a Gun. | audit |
| A7.2 | Firepower Modifiers | 54 | built | ScenarioA1FireCalculator.cs: Resolution.Firepower, Multipliers | MovingConcealedAndPointBlankTargetsChangeTheDr; LongRangeHalvesAndBeyondTwiceNormalRangeAbstains |  | Multipliers are cumulative and fractions are kept (decimal) and summed across the group. | audit |
| A7.21 | Point Blank Fire (PBF) | 54 | partly | ScenarioA1FireCalculator.cs: Multipliers, Outside (tpbf, out-of-range); FireRange.cs: Band; GamePlanner.Fire.cs: FireMapFacts, RangeNamed | TheFirersOwnLocationIsTpbfAndAnotherLevelOfItsHexIsNotBuilt; PointBlankFireNeedsATargetAtMostOneLevelAbove; TpbfTriplesTheFpInTheFirersOwnLocation | Pass 47 | PBF and TPBF built for Small Arms, MG, ATR (rulings R10.4, R10.14). Missing: fire between levels of the firer's own hex is refused (the rule gives PBF there, TPBF only in the same Location or under A7.211); IFE has no FP rule; a vehicle's MG at another level is undecided. | me |
| A7.211 | TPBF vs PRC | 55 | partly | GamePlanner.Fire.cs: FireMapFacts (own Location, D7.22 gate); ScenarioA1FireCalculator.cs: VehicleEffects | InTheMphUnitsFireAtAVehicleInTheirOwnLocationOnlyAfterItsOvr | Pass 51 | Infantry may fire TPBF at the CE crew of an AFV in their own Location, with the +2 CE DRM. Missing: fire from a higher Location of the hex, Passengers and OT crews that are not CE as targets (Passengers are never attacked, R26.2), the CC counter and Melee wording. | audit |
| A7.212 | Target Selection | 55 | partly | GamePlanner.Fire.cs: FireMapFacts (play.fire-target-limit) | none found | Pass 47 | IFT fire by a unit whose Location holds a Known enemy unit is limited to that Location, with the unarmed unarmored vehicle exception. Missing: the same limit on ordnance (Guns, mortars, LATW) and on Spotters; no test of the refusal was found. | audit |
| A7.22 | Long Range Fire | 55 | built | ScenarioA1FireCalculator.cs: Multipliers (long-range-fire), Outside (out-of-range) | LongRangeHalvesAndBeyondTwiceNormalRangeAbstains; AnAtrHasNoLongRangeAndAFtNoPointBlankFire |  | Half FP beyond Normal Range to twice it; ATR has none; FT has its own. | audit |
| A7.23 | Area Fire | 55 | built | ScenarioA1FireCalculator.cs: Multipliers (area-fire-concealed-target and the other Area Fire halvings) | AConcealedDefenderHalvesTheAttackAndAConcealedAttackerLosesItsConcealment; HiddenUnitsAreAttackedAsConcealedAndDummiesAreRemoved |  | Halved for a concealed target and again for each other Area Fire cause; ordnance and MOL are not halved. | audit |
| A7.24 | AFPh Fire | 55 | built | ScenarioA1FireCalculator.cs: Multipliers (advancing-fire), Firepower (FT, MOL exempt), Arithmetic (DC) | AdvancingFireIsHalvedAndMarkedPrepFire; AFlamethrowerInTheAfphIsNotHalved |  | AFPh halving of Small Arms, MG, ATR unless Opportunity Fire. IFE is not in the fire rules. | audit |
| A7.25 | Opportunity Fire | 55 | partly | GamePlanner.FireExtensions.cs: PlanOpportunityFire; ScenarioA1FireCalculator.cs: Multipliers, WeaponEffects; GamePlanner.Fire.cs: FireBar; GamePlanner.Movement.cs (play.move-halted) | OpportunityFireHoldsFireForTheAfphAtFullFp; OpportunityFireIsNotHalvedInTheAfph | Pass 47 | Built for Infantry small arms, MGs, and a Thrown DC (ruling R12.1). Missing: Opportunity Fire by ordnance, mortars, and LATW fired by Infantry (CA change at the shot, Case A), and Intensive Fire by an Opportunity Firer. | audit |
| A7.26 | Miscellaneous | 55 | not applicable |  |  |  | Cross-reference to the IFT's FP modifier list and other sections. | audit |
| A7.3 | Resolution | 55 | built | ScenarioA1FireCalculator.cs: Resolution.Arithmetic, Column; ScenarioA1FireReference.cs: Result, ColumnFp | KiaEliminatesByRandomSelectionAndBreaksTheRest; MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof |  | Column by total FP, cumulative DRM once each, results from the transcribed IFT (cells checked against the page examples). Fault: Result clamps the Final DR to 15, so a Final DR of 16 or more on the 36 column reads PTC where the IFT prints nothing. Heavy Payloads (C.7) is another section. | audit |
| A7.30 | Results | 55 | not applicable |  |  |  | Outline label only; the page prints no rule 7.30. Inventory: An outline label over 7.301 to 7.309; the page prints no rule 7.30. Its results are under A7.3 on page 55. | audit |
| A7.301 | #KIA | 55 | built | ScenarioA1FireCalculator.cs: Resolution.Apply (KIA), RandomSelection | KiaEliminatesByRandomSelectionAndBreaksTheRest |  | The number eliminated by Random Selection with ties, the rest broken, heroic, berserk, and broken units Reduced instead. Fault: a prisoner (unarmed) that survives is broken, not Casualty Reduced. | audit |
| A7.302 | K/# | 55 | partly | ScenarioA1FireCalculator.cs: Resolution.Apply (K), Reduce, Wound | CasualtyReductionLeavesTheSquadsOwnHalfSquad; ACasualtyReducedLeaderIsWoundedAndHisPlusOneApplies | Pass 47 | Squad to HS with its broken status, HS eliminated, SMC wounded with the Wound Severity dr, then the #MC on all. Missing: Reduce has no branch for a crew (the rule eliminates it); it ends as reduction-counter-missing after the roll. | audit |
| A7.303 | NMC | 55 | built | ScenarioA1FireCalculator.cs: MoraleChecks, CheckOrder, MoraleOutcome | FailingByTwoReplacesOnlyInTheLowElrGroup; U25DefensiveFirstFireAttacksTheMoverAndLeavesResidualFp |  | NMC by each target, best leaders first. | audit |
| A7.304 | #MC | 55 | built | ScenarioA1FireCalculator.cs: Check (ift-mc modifier), MoraleChecks | KiaEliminatesByRandomSelectionAndBreaksTheRest |  | The # of a #MC is added to the MC DR. | audit |
| A7.305 | PTC | 55 | built | ScenarioA1FireCalculator.cs: PinTaskChecks | PinnedMoversOfAMixedStackTakeTheirOwnFinalDr |  | NTC by unbroken, unpinned Personnel; a failure pins. Broken units take none. | audit |
| A7.306 | -- | 55 | built | ScenarioA1FireReference.cs: Result (none); ScenarioA1FireCalculator.cs: Apply | AConcealedTargetLeftConcealedIsNeverIdentifiedToTheFiringSide |  | The printed dash is transcribed as none and has no effect. | audit |
| A7.307 | vs Armored Targets | 55 | partly | ScenarioA1FireCalculator.cs: VehicleEffects; LiveFire.cs: Vehicle, CrewExposed | AKiaOrKResultRecallsTheCrewAndABuCrewIsNotVulnerable; ResidualFpAttacksAnAfvsCeCrewCollaterally | Pass 50 | Small Arms and MG never harm an AFV and its CE Inherent crew is attacked Collaterally. Missing: FT, DC, MOL, ATMM against an AFV (refused), Passengers and Riders as Vulnerable PRC, Bailing Out, and an AFV in terrain with a positive TEM (refused, R25.6). | audit |
| A7.308 | vs Unarmored Vehicles | 55 | partly | ScenarioA1FireCalculator.cs: VehicleEffects, Outside (vehicle-outside); ScenarioA1FireReference.cs: KillNumber; GamePlanner.Fire.cs: VehicleEffectEvents | AnUnarmoredTruckIsResolvedOnTheVehicleLineOfTheAttacksColumn; ResidualFpAttacksATruckOnTheVehicleLine | Pass 51 | Vehicle line on the same DR without the Personnel TEM: below the Kill Number eliminates, equal immobilizes, half or less burns. Missing: more than one vehicle in a Location (refused), the KIA# limit on vehicles affected, PRC survival and Collateral Attack for an unarmored vehicle's PRC, horses. | audit |
| A7.309 | Unlikely Kill | 56 | built | ScenarioA1FireCalculator.cs: VehicleEffects (unlikelyKill); GamePlanner.Choices.cs | AnOriginalTwoThatDoesNotHarmTheTruckRollsTheUnlikelyKill; TheFirerMayMakeTheUnlikelyKillDrAfterAnImmobilization |  | The firer's optional dr after any Original 2 (ruling R5.8); the better result stands. The HD exception has no Hull Down to meet. | audit |
| A7.31 | (none) | 56 | built | GamePlanner.Fire.cs: PlanFire, AddFireEvents (one attack resolved whole; a pending choice blocks other actions) | U25DefensiveFirstFireAttacksTheMoverAndLeavesResidualFp |  | Any number of attacks on a target, each resolved before the next. | audit |
| A7.32 | (none) | 56 | built | GamePlanner.Fire.cs: PlanFire | SprayingFireKeepsOneGroupPerTarget |  | Attacks are declared and resolved one at a time; nothing is predesignated. | audit |
| A7.33 | Multiple Targets | 56 | built | LiveFire.cs: FromState (withoutInherent, SW limits); ScenarioA1FireCalculator.cs: Marked | U27AMgAddsItsFirepowerKeepsItsRofAndFiresAgainAlone; BacklogPass12Tests (withoutInherent) |  | Inherent FP is never split; a squad fires its SW apart from it in the same phase (ruling R12.4). The exception for A7.34 is not built. | audit |
| A7.34 | Squad Spraying Fire | 56 | not built |  |  | Pass 47 | No Squad Spraying Fire. The catalog carries the trait asl:spraying-fire on squads but no rule reads it; needs the three-hex limit, the Long Range halving past Normal Range, and a reader of the trait. | audit |
| A7.35 | SW Usage | 56 | built | LiveFire.cs: FromState (possession); ScenarioA1FireCalculator.cs: Outside (weapon-outside, firer broken) | MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof |  | A SW fires only when possessed by an unbroken Personnel unit of the firing group. | audit |
| A7.351 | (none) | 56 | built | LiveFire.cs: FromState (play.fire-sw-limit); ScenarioA1FireCalculator.cs: Firepower, Outside | MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof; BacklogPass9Tests (fire-sw-limit) |  | One SW with the squad's inherent FP, joined or apart; two SW cost the inherent FP; never more than two. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A7.352 | (none) | 56 | built | ScenarioA1FireCalculator.cs: Firepower (HS and hero lose inherent FP), Outside (GunFired), Marked; GameProjector.cs (GunCrewsFired) | ScenarioA1Pass8Tests (GunFired); AHalfSquadAndAHeroFireFlamethrowers |  | A crew, HS, or SMC that fires a SW or Gun has no inherent FP for the Player Turn. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A7.353 | (none) | 56 | partly | ScenarioA1FireCalculator.cs: Firepower, Multipliers (a First-Fire-marked HS fires halved inherent FP in Subsequent First Fire, FPF, Final Fire); Outside (GunFired) | AFirstFireMarkedUnitFinalFiresOnlyAdjacentAsAreaFire | Pass 47 | A MMC that fired a SW keeps halved inherent FP in those attacks. Missing: a crew that fired its Gun is refused its halved inherent FP (ruling R8.4, backlog). Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A7.36 | Assault Fire | 56 | built | ScenarioA1FireCalculator.cs: Firepower (assault-fire) | none found |  | +1 FP after all modification, rounded up, in the AFPh, not at Long Range and not for Opportunity Fire. No test names it. | audit |
| A7.37 | Incremental IFT (IIFT) | 56 | not built |  |  | Left out: an optional rule by the rulebook's own mark | Optional rule (asterisk). No IIFT table or column logic; the game resolves on the standard IFT only. | audit |
| A7.371 | Column Shifts | 56 | not built |  |  | Left out: an optional rule by the rulebook's own mark | Part of the optional IIFT: column shifts fall to standard columns. Nothing to shift on, since only standard columns exist. | audit |
| A7.372 | Firepower Modifiers | 57 | not built |  |  | Left out: an optional rule by the rulebook's own mark | Part of the optional IIFT. The standard Residual FP of A8.2 is built; the IIFT's actual-FP columns are not. | audit |
| A7.4 | Target Determination | 57 | built | LiveFire.cs: FromState (targets, vehicles, movers); ScenarioA1FireCalculator.cs: Outside (target-outside), MoraleChecks | DefensiveFireIsByTheNonPhasingSideAndMarksFinalFire; HiddenUnitsAreAttackedAsConcealedAndDummiesAreRemoved |  | Every unit of the Location is attacked, each with its own check; friendly units only in a Melee or as prisoners. Blocked-LOF targets (entrenched behind a wall, Depression) have no terrain to meet. | audit |
| A7.5 | Fire Group | 57 | partly | GamePlanner.Fire.cs: FireMapFacts (FirerLocationsAdjacent); GamePlanner.Map.cs: IsAdjacent; ScenarioA1FireCalculator.cs: Outside (firer-outside) | AGroupAcrossLocationsUsesEachFirersRangeAndTheWorstHindrance; NoFireGroupSpansLocationsAtNight | Pass 47 | Groups across Locations, a leader alone no link, two SMC on one SW one firer. Missing: only "each Location ADJACENT to another" is checked, so two unconnected pairs pass as one group; Locations ADJACENT vertically or across a wall or hedge are refused; pillbox clause has no pillbox. | audit |
| A7.51 | Vehicles/Ordnance | 57 | partly | LiveFire.cs: FromState (play.fire-vehicle-group); GameProjector.cs (Mandatory FG with a vehicle's MG) | TheMandatoryFireGroupBindsAVehiclesMgAndItsLocationsInfantry | Pass 47 | Ordnance never joins a group (its own action). Missing: a vehicle's MG, Passengers, and Riders in a fire group (refused; D6.64 and D3.5 groups not built), and the Bypass LOS clause. | audit |
| A7.52 | (none) | 57 | built | GamePlanner.Fire.cs: PlanFire (blockedFirst split); ScenarioA1FireCalculator.cs: Arithmetic (worst Hindrance, one CX, one Encircled), Blocked; GamePlanner.Terrain.cs (highest wall TEM) | FireAtABlockedLosAffectsNothingButMarksTheFirers; AGroupAcrossLocationsUsesEachFirersRangeAndTheWorstHindrance |  | Worst Hindrance and TEM for all; blocked firers roll first and drop out, the rest fire as a smaller group (ruling R12.2). Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A7.53 | Fire Directions | 57 | partly | ScenarioA1FireCalculator.cs: Outside (director-outside, redirect); LiveFire.cs: FromState (Director); GamePlanner.Fire.cs: PlanFire (play.fire-smc) | DoublesCowerWithoutADirectorAndNotWithOne; ALeaderDirectsAMortarAndALoneSmcFiresItWithoutMultipleRof | Pass 47 | One direction a phase, again in Subsequent First Fire and Final Fire. Missing: a leader who has directed may not direct the same MG's later Multiple ROF shots on the IFT, nor direct FPF after directing First Fire; "only firers he directed in First Fire" is not checked. | audit |
| A7.531 | (none) | 57 | built | ScenarioA1FireCalculator.cs: Outside (a leader in every Location), Arithmetic (leadership, the worst of them), Marked; ScenarioA1OrdnanceCalculator.cs (To Hit DR of a SW) | AnAlliedLeaderDirectsOneWorse; ALeaderDirectsAMortarAndALoneSmcFiresItWithoutMultipleRof |  | Leadership DRM on one attack; a multi-Location group needs a director in each Location and takes the worst; the director is marked. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A7.54 | Berserk | 57 | not built |  |  | Pass 47 | Nothing bars a berserk unit from a multi-Location fire group: the firer facts carry no berserk state and FromState does not check it. Tiny: one refusal. | audit |
| A7.55 | Mandatory FG | 57 | built | GamePlanner.Fire.cs: PlanFire (play.fire-group); GameProjector.cs (UNIT-STATE-024) | SprayingFireKeepsOneGroupPerTarget; TheMandatoryFireGroupBindsAVehiclesMgAndItsLocationsInfantry |  | A Location fires at a target once a phase (once per MF expenditure in the MPh), so its units fire as one group; MG Multiple ROF shots, FT, DC, and ordnance are apart. | audit |
| A7.6 | TEM & LOS Hindrances | 57 | built | ScenarioA1FireCalculator.cs: Arithmetic (tem, los-hindrance); ScenarioA1FireReference.cs: Tem, HexsideTem; GamePlanner.Fire.cs: FireMapFacts | MovingConcealedAndPointBlankTargetsChangeTheDr; AWallTemLowersTheResidualFpLeft |  | TEM and Hindrance DRM for the reviewed terrain; other terrain is refused before the roll (Chapter B rows). | audit |
| A7.7 | Encirclement | 57 | partly | GamePlanner.FireExtensions.cs: EncirclementSeal, EncirclementShare, Encircles; ScenarioA1FireCalculator.cs: TargetState.MoraleLevel, Arithmetic (encircled +1); GamePlanner.Movement.cs (doubled MF) | TwoAttacksFromOppositeHexsidesEncircleALocation; EncirclementLowersTheTargetsMoraleAndAddsOneToItsOwnFire | Pass 47 | Built by ruling R12.11 for Infantry IFT fire at Normal Range in the fire phases. Missing: LOF from the Locations directly above and below, ordnance hits and vehicular armament as valid fire, the +1 on an Encircled unit's To Hit DR, Vulnerable PRC of an Immobile vehicle, capture effects (A20.21), Spraying Fire counted. | audit |
| A7.71 | FG | 58 | built | GamePlanner.FireExtensions.cs: EncirclementShare (every firer's entry counts), Encircles | TwoAttacksFromOppositeHexsidesEncircleALocation; AnAttackThatEncirclesAndEliminatesItsTargetsStillCommits |  | A group's entries all count; one group of two or more units can Encircle. | audit |
| A7.72 | Upper Levels | 58 | not built |  |  | Pass 47 | No upper-level Encirclement by a cut-off path to ground level (ruling R12.11 lists it as out). Needs a path search through building Locations and stairwells past enemy units and Blazes. | audit |
| A7.8 | Pin | 58 | built | ScenarioA1FireCalculator.cs: MoraleOutcome (highest passing DR), PinTaskChecks, Firepower (pinned-firer), TargetState.Break; GamePlanner.Movement.cs (play.move-unit); GameProjector.cs (pins removed at the Player Turn's end) | AHeroIsNeverPinnedOrBrokenAndACasualtyMcWoundsHimAsIfWounded; PinnedMoversOfAMixedStackTakeTheirOwnFinalDr |  | Pin on the highest passing MC DR, halved FP, no move or advance, not cumulative, removed on breaking, berserk, hardening, or the turn's end. Cavalry, Water Obstacles, Climbing are absent. Inventory: In the PDF outline; no head with this number was found in the page's text layer. | audit |
| A7.81 | Infantry Effects | 58 | built | ScenarioA1FireCalculator.cs: Firepower (PinnedMgAreaFire), WeaponEffects (no Multiple ROF), Outside (FT, MOL); GamePlanner.Fire.cs: PlanFire (Fire Lane refused); ScenarioA1OrdnanceCalculator.cs (Case D, no CA change, no Intensive Fire) | IntensiveFireIsBarredAfterFinalFireAndWithAPinnedCrew; SpottedFireAddsTwoAndLowersTheRofAndAPinnedSpotterAddsCaseD; BacklogPass31Tests (pinnedMgAreaFire) |  | Pinned Infantry: MG as Area Fire, +2 To Hit, no FT, MOL, DC, Fire Lane, CA change, Intensive Fire, or Multiple ROF (ruling R31.3). IFE and Canister are not in the fire rules. | audit |
| A7.82 | Vehicle/Crew | 58 | partly | ScenarioA1FireCalculator.cs: VehicleEffects (Pinned), VehicleFirepower (pinned-crew), VehicleWeaponEffect; GamePlanner.Fire.cs: VehicleConditions | TheAamgIsHalvedInTheAfphInMotionAndWhenPinnedAndLosesItsRof | Pass 51 | A pinned CE crew has its MG halved and loses Multiple ROF. Missing: a pinned CE crew of a CT AFV is not buttoned up for the Player Turn; the unarmored Target Facing case; FT and Canister halving. | audit |
| A7.821 | Passengers/Riders | 58 | not built |  |  | Pass 51 | Passengers are never attacked or pinned (ruling R26.2) and there are no Riders. Needs Passengers as Vulnerable PRC, BU on a pin, pinned on unloading, and Riders Bailing Out. | audit |
| A7.83 | Movement/Advance | 58 | built | ScenarioA1FireCalculator.cs: Arithmetic (ffnam, ffmo only for unpinned movers; PinnedFinalDr), TargetState.Break | PinnedMoversOfAMixedStackTakeTheirOwnFinalDr |  | Pinned movers take no FFNAM or FFMO; broken again they do (ruling R12.3). The non-moving To Hit clause is ordnance's; the entrenchment clause has no entrenchment to meet. | audit |
| A7.831 | Leaders | 58 | built | ScenarioA1FireCalculator.cs: LeaderLoss (LLTC only on a break), Outside (director pinned), Check (a pinned leader gives no DRM), ActiveCommissar; GamePlanner.Movement.cs: LeaderBonus over the unpinned movers | PinnedMoversOfAMixedStackTakeTheirOwnFinalDr |  | A pinned leader causes no LLTC, gives no MF or portage bonus, directs nothing, and aids no MC or TC. Voluntary Rout is A10.711. | audit |
| A7.9 | Cowering | 58 | partly | ScenarioA1FireCalculator.cs: Arithmetic (cowered, shift), NeverCowers, Column | DoublesCowerWithoutADirectorAndNotWithOne; BritishEliteAndFirstLineUnitsAndFinnsButConscriptsNeverCower; AResidualFpAttackIsAloneAndNeverCowers | Pass 47 | Column shift on undirected Doubles, two for Inexperienced, with the exemptions for SMC, Fanatic, British, Finns, vehicles, Residual FP, Fire Lane, DC. Missing: the cowering unit and all its SW are not marked Final or Prep Fire (a First Fire cowerer stays First Fire and its MG may keep ROF); no Random Selection of the marked unit in a group; berserk firers are not exempt; the double shift reads class, not Inexperience. | audit |

#### A8 Defensive Fire Principles (pages 59 to 61)

24 rows: 11 built, 1 built with a deviation, 6 partly, 5 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A8 | Defensive Fire Principles | 59 | not applicable |  |  |  | Heading. | audit |
| A8.1 | First Fire | 59 | built | GamePlanner.Fire.cs: PlanFire (play.fire-window); LiveFire.cs: FromState (movers only); GamePlanner.Movement.cs; GameProjector.cs (UNIT-STATE-024) | U25DefensiveFirstFireAttacksTheMoverAndLeavesResidualFp; DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp |  | The DEFENDER's window on each MF or MP expenditure, fire at the moving stack alone, First Fire counters, one mover at a time. Platoon and Convoy movement are other sections. | audit |
| A8.11 | Facing | 59 | built | GamePlanner.Movement.cs: PlanPassFire, PlanEndMove (play.move-window, play.end-move); GamePlanner.Vehicles.cs | DefensiveFirstFireAtATargetIsLimitedByTheMfItSpentThere; ControlGainedDuringMovementSurvivesDefensiveFirstFire |  | The stack waits until the DEFENDER fires or passes; no return to an earlier position. The spoken pauses are replaced by the window. | audit |
| A8.12 | Moving Within Location | 59 | built | GamePlanner.Smoke.cs; GamePlanner.DemolitionCharges.cs; GamePlanner.Vehicles.cs: VehicleStepPlan; GamePlanner.Passengers.cs | ResidualFpAttacksATruckThatSpendsMpInItsLocation; ASmokeDrOf6EndsThePlacingSquadsMph |  | MF or MP spent inside a Location (SMOKE, a DC, a VCA change, loading) opens the window like an entry. Entrenchments are absent. | audit |
| A8.13 | Defensive First Fire DRM | 59 | built | ScenarioA1FireCalculator.cs: Arithmetic (IsMovementFire gate); ScenarioA1OrdnanceCalculator.cs (Case J) | DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp; DefensiveFirstFireTakesCasesJ3AndJ4AndLeavesAFirstFireCounter |  | FFNAM and FFMO only in Defensive First Fire; Case J for ordnance. | audit |
| A8.14 | Follow-Up Attack | 59 | built | GamePlanner.Fire.cs: PlanFire (play.fire-mf-limit); LiveFire.cs: FromState (targets read in their present state); GameProjector.cs (a broken or pinned member stays a target in the open window) | TheFirstFireLimitCountsThisStacksMoveOnly; PinnedMoversOfAMixedStackTakeTheirOwnFinalDr |  | A broken or pinned mover is attacked again as it now is; the same firer again only when the MF spent allow it (ruling R31.1); a broken mover keeps FFNAM and FFMO. | audit |
| A8.15 | Snap Shot | 59 | partly | GamePlanner.Fire.cs: FireMapFacts (snapHexside), GamePlanner.Map.cs: LosToHexside; ScenarioA1FireCalculator.cs: Multipliers (snap-shot), Arithmetic, Residual | ASnapShotIsAreaFireWithNoTemFfmoOrFfnam; ASnapShotIsHalvedAndLeavesNoResidualFp | Pass 52 | Built by ruling R10.13 for Small Arms and MG. Missing: a Snap Shot where either hex has a wall, hedge, SMOKE, or rubble (refused), at a Bypass step (refused), by a Fire Lane (A9.221), and the MG CA bar (A9.21 is not built). | audit |
| A8.2 | Residual Firepower | 60 | partly | ScenarioA1FireCalculator.cs: Residual, ResidualCounters, Arithmetic (residual); GamePlanner.Fire.cs: AddFireEvents (residual-fp-placed); GamePlanner.Movement.cs; GamePlanner.Vehicles.cs: VehicleStepPlan; GameProjector.cs: PlaceResidual | DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp; ResidualFpAttacksAStackEnteringItsHex; ResidualFpTakesTheSmokeOfTheTargetLocation | Pass 47 | Half the column used, attacks movers entering with FFNAM, FFMO, in-hex TEM, and SMOKE, removed when the MPh ends. Missing: Residual FP from a To Kill or ordnance attack (A8.25), an Infantry MF expenditure inside a Residual Location (SMOKE there is refused, R9.5), FFE Hindrance. Fault: the cap of 12 is applied before the A8.26 reduction, so a 36 FP attack with a +1 DRM leaves 8, not 12. | audit |
| A8.21 | (none) | 60 | built | GamePlanner.Fire.cs: AddFireEvents (only a larger counter replaces); GameProjector.cs: PlaceResidual (UNIT-STATE-026) | AWallTemLowersTheResidualFpLeft; ResidualFpAttacksAStackEnteringItsHex |  | One counter a Location, a larger one replaces it, a Fire Lane coexists, a Bypassing stack is attacked. The Depression and Crest clause has no terrain to meet. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A8.22 | Restrictions | 60 | partly | ScenarioA1FireCalculator.cs: Outside (residual-outside); GamePlanner.Movement.cs (attacks first, alone, on entry); GamePlanner.Vehicles.cs: VehicleStepPlan | AResidualFpAttackIsAloneAndNeverCowers; ResidualFpAttacksATruckThatSpendsMpInItsLocation | Pass 47 | Residual FP attacks alone and first. Missing: no record of who was attacked, so a vehicle is attacked again at each separate MP expenditure in the Location where the rule allows once unless the FP or DRM worsened; the re-attack on a more negative DRM for Infantry; the activity-completes-first clause; pillbox, OBA, minefield clauses have nothing to meet. | audit |
| A8.221 | Malfunction | 60 | not built |  |  | Pass 47 | Residual counts the whole column even when a MG of the attack malfunctioned on that DR; the rule gives no Residual FP for the malfunctioning weapon. Ammunition Shortage is absent. Tiny: leave the malfunctioned weapon's FP out of Residual. | audit |
| A8.222 | vs AFV | 60 | built | GamePlanner.Vehicles.cs: VehicleStepPlan; ScenarioA1FireCalculator.cs: VehicleEffects | ResidualFpAttacksAnAfvsCeCrewCollaterally; ABuHalftrackMayStopInResidualFp |  | No effect on an AFV but Collaterally on its CE crew (ruling R6.6). | audit |
| A8.223 | Snap Shot | 60 | built | ScenarioA1FireCalculator.cs: Residual (SnapShot) | ASnapShotIsHalvedAndLeavesNoResidualFp |  | A Snap Shot leaves none. | audit |
| A8.224 | Cowering | 60 | built | ScenarioA1FireCalculator.cs: Arithmetic (cowered excludes residual), Residual (uses the shifted column), Arithmetic (residual never halved) | AResidualFpAttackIsAloneAndNeverCowers |  | Residual FP never Cowers and is never halved; a Cowering attack leaves less. | audit |
| A8.23 | ROF | 60 | not built |  |  | Pass 47 | No choice between keeping Multiple ROF and leaving Residual FP: a MG that keeps its ROF still adds its FP to the Residual FP placed and fires again. Needs the DEFENDER's choice after the attack and Residual from the other firers' FP alone. | audit |
| A8.24 | Spraying Fire | 60 | not built |  |  | Pass 47 | Spraying Fire is refused in the MPh (ruling R12.6), so it never leaves Residual FP in two Locations. | audit |
| A8.25 | Ordnance | 60 | not built |  |  | Pass 47 | An ordnance hit resolves with no fire kind, so Residual returns none for it (ruling R9.3, backlog section 1 row 19). The code and the backlog agree; the ordnance term inside Residual is unreachable. | audit |
| A8.26 | Effect of DRM | 60 | partly | ScenarioA1FireCalculator.cs: Residual (hindrance, positive leadership, hexside TEM, weather cushion) | AWallTemLowersTheResidualFpLeft; DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp | Pass 47 | One column less for each +1 of LOS Hindrance, positive leadership, or wall or hedge TEM; negative DRM, Height Advantage, and LV ignored. Missing: the CX +1 (named by the rule), an Encircled firer's +1, a hero's two-man +1, a vehicle's Stun +1, and Air Bursts raising it (ordnance leaves none). | audit |
| A8.3 | Subsequent First Fire | 61 | built | LiveFire.cs: FromState (kind by markers); ScenarioA1FireCalculator.cs: Outside (subsequent-first-fire-outside), Multipliers (area-fire), IsSustained, WeaponEffects; GamePlanner.Fire.cs: PlanFire (play.fire-weapons, play.fire-mf-limit), FireMapFacts (closest Known enemy) | SubsequentFirstFireIsAreaFireWithinRange; SubsequentFirstFireMarksTheFirersWithFinalFire |  | Area Fire by a First-Fire-marked unit within Normal Range and the closest armed Known enemy, all its MGs as Sustained Fire, no Multiple ROF, marked Final Fire. The Normal Range test reads the unit's range, not each MG's. | audit |
| A8.31 | Final Protective Fire (FPF) | 61 | built with a deviation | ScenarioA1FireCalculator.cs: Outside (fpf-outside), FinalProtectiveFireChecks, Multipliers, WeaponEffects; LiveFire.cs: FromState | FinalProtectiveFireChecksItsFirersOnTheOriginalDr; FpfMayBeDirectedAndMixedWithFirstFire | Pass 47 | Ruling R12.3: FPF with its NMC on the Original DR, leadership, Random Selection, all MGs as Sustained Fire, repeatable. Departs: FPF groups only with Subsequent First Fire, never with an unmarked unit's First Fire (backlog). The CC counter after TPBF FPF is not placed. | audit |
| A8.311 | Restrictions | 61 | partly | GamePlanner.Fire.cs: FireMapFacts (play.fire-target-limit) | none found | Pass 46 | The bar on FPF at an adjacent unit while a Known enemy is in the firer's Location follows from the A7.212 limit. Missing: Infantry manning ordnance adding their inherent FP to FPF (a crew that fired its Gun has no inherent FP, R8.4), and the OVR Prevention exception. | audit |
| A8.312 | TPBF | 61 | not built |  |  | Pass 46 | TPBF in the MPh at a unit that entered the firer's Location is allowed (after a berserk charge) but never required: the DEFENDER may pass. Needs the forced attack after Residual FP, the one Mandatory FG, and the bar on a SMC with a First-Fire-marked MG. | audit |
| A8.4 | Final Fire | 61 | partly | ScenarioA1FireCalculator.cs: FirerMayFire (DFPh), IsFinalFireAgain, Outside (final-fire-outside), Multipliers (area-fire A8.4) | AFirstFireMarkedUnitFinalFiresOnlyAdjacentAsAreaFire; DefensiveFireIsByTheNonPhasingSideAndMarksFinalFire | Pass 47 | Final Fire by unmarked units, and by First-Fire-marked units as Area Fire at adjacent or same-hex targets, on every unit of the Location with no FFNAM or FFMO. Missing: a vehicle's Final Fire after its First Fire (ruling R6.9), and fire into another level of the same hex. | audit |
| A8.41 | Multiple ROF | 61 | built | ScenarioA1FireCalculator.cs: FirerMayFire, WeaponMayFire, IsSustained; ScenarioA1OrdnanceCalculator.cs (Intensive Fire) | U27AMgAddsItsFirepowerKeepsItsRofAndFiresAgainAlone; AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire |  | An unmarked MG keeps firing in Final Fire at any target; a First-Fire-marked MG fires once more as Sustained Fire. Suspected fault: the adjacent-only limit reads the unit's marker, so a First-Fire-marked MG held by an unmarked squad may Sustained Fire at any range. | audit |

#### A9 Machine Guns & SW Malfunction (pages 62 to 65)

24 rows: 7 built, 10 partly, 6 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A9 | Machine Guns & SW Malfunction | 62 | not applicable |  |  |  | Heading; its bracketed note (a squad's inherent LMG is no counter) needs no logic. | audit |
| A9.1 | Counters | 62 | built | LiveFire.cs: FromState (possession); ScenarioA1FireReference.cs: Definition (FP, range); ScenarioA1FireCalculator.cs: Firepower | MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof |  | A MG is a SW with printed FP and Normal Range, fired by the Personnel possessing it. Catalog: German and Russian LMG, MMG, HMG, British LMG. | audit |
| A9.11 | MMC Usage | 62 | built | ScenarioA1FireCalculator.cs: Firepower (inherent rule), Outside (weapons count) | MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof; AnAtrAddsOneFpToItsSquadsFireGroup |  | A squad fires one MG free or two for its inherent FP; any other MMC one MG for its inherent FP. | audit |
| A9.12 | SMC Usage | 62 | built | ScenarioA1FireCalculator.cs: LeaderMg, Firepower (smc-area-fire), Arithmetic (leaders exempt from Cowering); GamePlanner.Fire.cs: PlanFire (play.fire-smc); LiveFire.cs: FromState (partner) | ALeaderFiresAMgAsAreaFireAloneAndAtFullFpWithAPartner; AHeroFiresAMgWithItsTwoManDrmAndHisHeroicDrm |  | A lone leader fires a MG as Area Fire, two SMC at full FP; he gives no leadership that phase (ruling R12.4). | audit |
| A9.2 | Multiple ROF | 62 | built | ScenarioA1FireCalculator.cs: WeaponEffects (colored dr), FirerMayFire (rate of fire shot); GamePlanner.Fire.cs: PlanFire (play.fire-mf-limit) | U27AMgAddsItsFirepowerKeepsItsRofAndFiresAgainAlone; DefensiveFirstFireAtATargetIsLimitedByTheMfItSpentThere; ACapturedMgBreaksDownTwoSoonerAndLosesOneRof |  | Each MG keeps or loses Multiple ROF on the colored dr; in the MPh no more shots at a mover than the MF it spent there (ruling R31.1). | audit |
| A9.21 | Field of Fire | 62 | not built |  |  | Pass 47 | No Field of Fire: a MMG or HMG in woods, rubble, or a building keeps free aim on its later shots, and nothing fixes a CA (also for the INF, RCL, 20L ATR SW types). Needs a per-phase CA on the SW and its pinned carry-over. | audit |
| A9.22 | Fire Lane | 63 | partly | GamePlanner.Fire.cs: PlanFire (fireLane), AddFireFollowUps; GamePlanner.FireExtensions.cs: FireLaneEntries, FireLaneFacts, AddFireLaneAttack; GamePlanner.Movement.cs; GameProjector.cs (FireLanePlaced, KeepFireLanes) | AFireLaneAttacksALaterMoverInItsHexGrain; BacklogPass12Tests (play.fire-lane-mg) | Pass 47 | Ruling R12.7: a lane along a straight Hex Grain within Normal Range at the MG's level, the column left of the MG's FP, doubled ADJACENT, attacking Infantry that enter. Missing: lanes against vehicles, attacks on later MF expenditures in a lane Location, a lane's wall or hedge TEM, the MG firing as ordnance. | audit |
| A9.221 | Alternate Hex Grain | 63 | not built |  |  | Pass 47 | No Alternate Hex Grain and no Fire Lane Snap Shot (ruling R12.7, backlog). Needs the left or right grain choice and hexside LOS. | audit |
| A9.222 | Residual FP | 63 | partly | GamePlanner.FireExtensions.cs: FireLaneFacts; ScenarioA1FireCalculator.cs: Arithmetic (FireLane: no Cowering, no Hindrance DRM, FFMO cancelled), Residual; GamePlanner.Movement.cs (other Residual FP first, then each lane) | AFireLaneAttacksALaterMoverInItsHexGrain | Pass 47 | Never reduced, no CX, leader, or hero DRM, no Cowering, resolved after other Residual FP, each lane apart. Missing: hard Hindrances (orchard, wreck) and hexside or bridge TEM as DRM, Impulse movement, SMOKE placed after the lane. | audit |
| A9.223 | Cancellation | 63 | partly | GameProjector.cs: KeepFireLanes; GamePlanner.FireExtensions.cs: AddFireLaneAttack (malfunction on the lane's DR); GamePlanner.Fire.cs: PlanFire (play.fire-lane-mg) | AFireLaneAttacksALaterMoverInItsHexGrain; BacklogPass12Tests (play.fire-lane-mg) | Pass 47 | The lane ends on a malfunction, on its manning Infantry breaking, being pinned, or eliminated, and with the MPh. Missing: the forced cancellation for TPBF or CC Reaction Fire when the MG's Location is entered. | audit |
| A9.3 | Sustained Fire | 63 | partly | ScenarioA1FireCalculator.cs: IsSustained, WeaponEffects (B# less 2, no ROF, Final Fire counter), Multipliers | SubsequentFirstFireMarksTheFirersWithFinalFire; AFirstFireMarkedUnitFinalFiresOnlyAdjacentAsAreaFire | Pass 47 | B# lowered by two, Area Fire, no more shots, Final Fire on the MG. Missing: the Original B# becoming an X# under Sustained Fire (A.11: a DR of 12 removes the MG, here it only malfunctions); the bar on a MG fired by a lone SMC; a vehicle's MA (R6.9). | audit |
| A9.4 | Mandatory Fire Direction | 64 | not built |  |  | Pass 47 | No 16-hex limit without a directing leader and no treating Infantry at 17 or more hexes as concealed; a MMG or HMG fires to twice its Normal Range undirected. Small: one range test and one halving. | audit |
| A9.5 | Spraying Fire | 64 | partly | GamePlanner.Fire.cs: PlanFire (sprayTarget), AddFireFollowUps; ScenarioA1FireCalculator.cs: Outside (spraying-fire-outside), Multipliers (spraying-fire) | SprayingFireAttacksTwoLocationsOnOneDr; SprayingFireIsAreaFireAndItsSecondRecordMarksNothing | Pass 47 | Ruling R12.6: two Locations sharing a hexside on one Original DR, each with its own TEM and Hindrance, as Area Fire, in the fire phases. Missing: Spraying Fire in the MPh, at two vertically adjacent Locations of one hex, at Bypass or road points, and counted toward Encirclement. | audit |
| A9.51 | vs Vehicle | 64 | built | LiveFire.cs: FromState (vehicles of the second Location); ScenarioA1FireCalculator.cs: VehicleEffects | none found |  | A spray reaches vehicles through the same path as other IFT fire: no effect on an AFV, the Vehicle line for an unarmored one, Collateral on a CE crew. No test of a spray at a vehicle was found. | audit |
| A9.52 | Restrictions | 64 | partly | ScenarioA1FireCalculator.cs: Outside (every firer fires a MG); GamePlanner.Fire.cs: PlanFire (First-Fire-marked units spray only ADJACENT) | SprayingFireKeepsOneGroupPerTarget; SprayingFireAttacksTwoLocationsOnOneDr | Pass 47 | A group sprays only when every firer fires a MG. Departs (R12.6): the MG's holder adds its inherent FP whether or not the squad has Spraying Fire capability, where the rule needs every member capable. Missing: a spray at a moving unit and an empty Location for Residual FP. Paratroops are absent. | audit |
| A9.6 | Vehicular Targets | 64 | partly | ScenarioA1FireCalculator.cs: VehicleEffects (unarmored: Vehicle line, no To Hit DR) | AnUnarmoredTruckIsResolvedOnTheVehicleLineOfTheAttacksColumn | Pass 33 | MG fire at an unarmored vehicle is resolved on the Vehicle line. Missing: fire at an armored Target Facing on the To Hit and AP To Kill Tables (planned pass 33). | audit |
| A9.61 | AFV Kill | 64 | not built |  |  | Pass 33 | No MG To Kill attack on an AFV: no predesignation, To Hit DR, AP To Kill number, Stun on the Kill Number, or the unarmored-facing case. | audit |
| A9.611 | Effect vs Personnel | 65 | partly | ScenarioA1FireCalculator.cs: VehicleEffects | AKiaOrKResultRecallsTheCrewAndABuCrewIsNotVulnerable | Pass 33 | A normal MG attack affects only an AFV's Vulnerable crew. Missing: the Collateral Attack of a MG To Kill attack (A9.61 is not built). | audit |
| A9.7 | Support Weapon (SW) Malfunction | 65 | built | ScenarioA1FireCalculator.cs: WeaponEffects (B#); GamePlanner.Fire.cs: AddFireEvents (Malfunctioned); ScenarioA1FireCalculator.cs: WeaponMayFire, Outside (weapon-outside) | MachineGunsAddTheirFirepowerMalfunctionAndKeepTheirRof; ACapturedMgBreaksDownTwoSoonerAndLosesOneRof |  | An Original DR at least the B# malfunctions the SW; the attack still resolves; no fire until repaired. The X# trait is read from the catalog but only a FT's removal uses it on the IFT. | audit |
| A9.71 | Multiple SW Malfunction | 65 | built | ScenarioA1FireCalculator.cs: WeaponEffects (weaponSelection), OverrunEffect | ScenarioA1FireExtensionTests (weaponSelection); AnOriginal12MalfunctionsAWeaponByRandomSelectionOrImmobilizesAnUnarmedVehicle |  | Random Selection among the SW whose B# the DR reached; the highest dr, ties included. | audit |
| A9.72 | SW/Gun Repair | 65 | partly | GamePlanner.Rally.cs: PlanRepair; GameProjector.cs (UNIT-STATE-028) | RepairInTheRphRepairsOrEliminatesAMalfunctionedMg | Pass 47 | A Good Order unit's Repair dr in the RPh for a MG: at most the R# repairs, a 6 eliminates. Missing: Gun repair by its crew and mortar repair (the catalog mortars carry no R#), repair while dismantled. Fault: a captured SW is not refused repair. | audit |
| A9.73 | SW Self-Destruction | 65 | not built |  |  | Pass 47 | No deliberate destruction or malfunction of a SW, Gun, or vehicular weapon in the PFPh or DFPh. | audit |
| A9.74 | Random SW/Gun Destruction | 65 | not built |  |  | Pass 50 | No Random SW or Gun Destruction dr after a KIA, none by Indirect Fire or OVR (backlog, R8.3). A unit eliminated by a KIA leaves its SW intact. | audit |
| A9.8 | Dismantled (dm) SW | 65 | partly | GamePlanner.SupportWeapons.cs: PlanDismantle, Dismantlable; GamePlanner.Movement.cs (halved PP); LiveFire.cs: FromState (a dismantled weapon is not fired) | AGermanMmgIsDismantledInAFirePhaseAndThenIsNotFired; DismantlingInThePfphIsAUseOfTheSwSoTheSquadDoesNotMove | Pass 47 | Ruling R13.6: the German MMG only. Missing: the German HMG and the three light mortars of 4 or 5 PP that the catalog holds and the rule names, starting dismantled, a dm German MMG or HMG firing as a LMG, repair while dm, capture, Scrounged weapons, and the number of weapons a unit may handle. | audit |

#### A10 Morale (pages 65 to 69)

26 rows: 13 built, 1 built with a deviation, 9 partly, 2 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A10 | Morale | 65 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| A10.1 | Morale Check (MC)/Task Check (TC) | 65 | built | ScenarioA1FireCalculator.cs: MoraleChecks, MoraleOutcome; GamePlanner.Overrun.cs: AddPaatc, PaatcFacts | ScenarioA1FirePackageTests; BacklogPass11Tests.InfantryAdvanceOnAnAfvAfterAPaatcAndFightItInSequentialCc |  | MC against the Morale Level with break, Reduction, elimination; TC as PAATC, Deployment NTC, OVR NTC. FPF PAATC is D7.212, outside this group. | audit |
| A10.2 | Leader Loss Morale Check (LLMC)/Leader Loss Task Check (LLTC) | 65 | partly | ScenarioA1FireCalculator.cs: CheckOrder, LeaderLoss | ScenarioA1FirePackageTests.AnEliminatedLeaderCausesALlmc; ALeaderWhoBreaksCausesALltcAndNoLongerAidsMcs | Pass 48 | Built inside a fire attack: leaders check first by Morale Level, LLMC and LLTC with the reversed modifier, none in Melee. Missing: a leader lost outside the Fire package (Rally Fate mortal wound, rout Interdiction) causes no LLMC. | audit |
| A10.21 | Leadership DRM | 66 | built | ScenarioA1FireCalculator.cs: Check | ScenarioA1FirePackageTests.ALeaderWhoBreaksCausesALltcAndNoLongerAidsMcs |  | One unbroken leader of the Location, not cumulative, the most favorable taken (R31.5), none on LLMC or LLTC. WP and Bombardment MC do not exist in the game. | audit |
| A10.22 | (none) | 66 | built | ScenarioA1FireCalculator.cs: Check | ScenarioA1FirePackageTests |  | A leader takes only a higher-morale leader's DRM, never his own. | audit |
| A10.3 | MC Failure | 66 | built | ScenarioA1FireCalculator.cs: MoraleOutcome | ScenarioA1FirePackageTests |  | A failed MC breaks; an already broken unit is Casualty Reduced; heroes, berserk units, prisoners handled. Japanese, Wading, LC are other chapters. | audit |
| A10.31 | Casualty MC | 66 | built | ScenarioA1FireCalculator.cs: MoraleOutcome; LiveRecords.cs: RoutInterdicted.For | ScenarioA1FirePackageTests.AnOriginalTwelveIsACasualtyMcWithinElr; ScenarioA1Pass19bTests.ACasualtyMcBeyondTheCircledESquadsElrLeavesABrokenHalfSquadOfLesserQuality |  | Original 12: Reduced and broken after ELR Replacement, eliminated if broken, hero wounded with +1. | audit |
| A10.4 | Broken Units | 66 | built | GamePlanner.SupportWeapons.cs: RoutLoadOf; GamePlanner.Rout.cs: PlanRout; GamePlanner.Fire.cs: fire eligibility | BacklogPass31dTests; BacklogPass13Tests |  | Broken Morale Level, no attack, no move but rout or withdrawal, and the IPC load left before a rout (R31d.1). The A11.21 withdrawal of a laden unit is refused, see A11.21. | audit |
| A10.41 | Voluntary Break | 66 | not built |  |  | Pass 48 | No Voluntary Break action exists. A unit cannot break itself at the start of the RtPh to rout or surrender. | audit |
| A10.5 | Routing | 66 | partly | GamePlanner.Rout.cs: MustRout, MayRout, PlanRout, FailureToRout, RoutHalfMf | BacklogPass13Tests.ABrokenUnitInOpenGroundRoutsToTheNearestWoodsAndIsInterdictedOnTheWay; LowCrawlEscapesInterdictionAndAUnitThatStaysInTheOpenFailsToRout | Pass 48 | Built (R13.3): who must and may rout, ATTACKER first, 6 MF (wounded SMC 3), no Bypass, Failure to Rout, surrender. Missing: rout for a Blaze, the Normal Range of enemy vehicles and Guns, the Entrenched and Emplaced Gun exemptions, routs to or from upper building levels (steps are ground level only). | audit |
| A10.51 | Direction | 66 | partly | GamePlanner.Rout.cs: RoutStepBar, RoutReach, RoutTargets, PlanRout | BacklogPass13Tests; BacklogPass20TablePlayerTests; BacklogPass31dTests | Pass 48 | Built: never toward or ADJACENT to a Known enemy, nearest woods or building in MF, the no-farther hexes ignorable, stop on reaching cover, any route when none. Missing: the EXC to ignore the hexes of the building it starts in, minefield and FFE routes, the shellhole or entrenchment option, a new destination when an enemy becomes Known, upper levels. | audit |
| A10.52 | Low Crawl | 67 | built | GamePlanner.Rout.cs: PlanRout (lowCrawl) | BacklogPass13Tests.LowCrawlEscapesInterdictionAndAUnitThatStaysInTheOpenFailsToRout; BacklogPass16TablePlayerTests.NoLowCrawlTowardAKnownEnemy |  | One Location for all MF, never Interdicted, not out of an enemy-occupied Location, not into marsh, toward the nearest cover. Water Obstacles and streams are not on the maps. | audit |
| A10.53 | Interdiction | 67 | partly | GamePlanner.Rout.cs: Interdictor, PlanRout (Build); LiveRecords.cs: RoutInterdicted.For | BacklogPass13Tests.AFailedInterdictionReducesTheSquadAndItsHalfSquadRoutsOn | Pass 48 | Built: NMC on entering Open Ground without Low Crawl, once per hex, Reduction with the HS routing on, a pin ends the rout. Faults: a Fanatic unit's +1 Morale Level is not applied, and a leader's Casualty Reduction has no Wound Severity dr. | audit |
| A10.531 | Open Ground | 67 | built with a deviation | GamePlanner.Rout.cs: OpenGround, ExposedInOpenGround | BacklogPass13Tests | Pass 45 | Ruling R13.3: Open Ground is read from the hex terrain (Open Ground or road, grain out of season, no SMOKE) and a LOS with no Hindrance, not from the FFMO a given enemy could apply; no TEM, Height Advantage, shellhole or entrenchment cases. | audit |
| A10.532 | Interdictor | 67 | partly | GamePlanner.Rout.cs: Interdictor, NormalRange | BacklogPass13Tests | Pass 48 | Infantry Interdict within Normal Range (at most 16), not when CX, pinned, Encircled, in Melee, or a leader without a SW. Missing: vehicles and Guns as Interdictors, the halved-FP bar in general (a lone SMC with a MG Interdicts here), Spotted Fire. | audit |
| A10.533 | Concealment | 68 | partly | GamePlanner.Rout.cs: KnownEnemies, RoutStepBar | none found | Pass 45 | Concealed and hidden units are ignored in the legal route, as the rule says. Missing and a fault: a rout step into a Location holding only concealed or hidden enemy units is accepted with no reveal, no repulse and no elimination; a concealed unit cannot drop its "?" to Interdict or bar a route. | audit |
| A10.6 | Rally | 68 | built | GamePlanner.Rally.cs: PlanRally; ScenarioA1RallyCalculator.cs: Outside, Run; LiveRally.cs | ScenarioA1RallyPackageTests; RallyAndFireStepsTests.U21ALeaderRalliesABrokenSquadUnderDm; LiveRecordTests.ARallyAttemptIsKeptForThePlayerTurnAndMadeOnce |  | Rally in any RPh by a Good Order leader of the Location, one attempt a Player Turn, Self-Rally where allowed. Armor Leaders and Passenger rally were not traced. | audit |
| A10.61 | Terrain Bonus | 68 | partly | ScenarioA1RallyCalculator.cs: TerrainDrm | ScenarioA1Pass13Tests.MarshAndRubbleGiveNoRallyTerrainDrm; ScenarioA1RallyPackageTests.ALeaderRalliesABrokenSquadUnderDmInABuilding | Pass 37 | -1 in woods and buildings (R13.2). Pillbox and trench are not terrain the game has; they need B27 and B30. | audit |
| A10.62 | Desperation Morale (DM) | 68 | partly | GamePlanner.Fire.cs: DM on break and on enough FP; GamePlanner.Rout.cs: AdjacentDm, RoutPhaseDm, RetainDmBar; GamePlanner.Snipers.cs; ScenarioA1RallyCalculator.cs: Run (+4) | BacklogPass13Tests; ScenarioA1RallyPackageTests.ALeaderRalliesABrokenSquadUnderDmInABuilding | Pass 37 | Built (R13.1): DM on breaking, on an attack able to give a NMC, from a Sniper, from an ADJACENT Known armed enemy, at the RtPh start in Open Ground, removal as the RPh ends with the option to keep it, +4 to Rally. Missing: Blaze, WP, FFE, keeping DM when overstacked in woods or a building. | audit |
| A10.63 | Self-Rally | 68 | built | ScenarioA1RallyCalculator.cs: SelfRally, Outside, Run | ScenarioA1Pass13Tests.AGermanOrRussianMmcHasNoUnrecordedSelfRally; RallyAndFireStepsTests.FateReducesTheSquadAndAFirstMmcSelfRallyCreatesALeader |  | Capability from the catalog trait, +1 DRM, the first MMC attempt of its own RPh, never with a Good Order leader present. | audit |
| A10.64 | Fate | 68 | built | ScenarioA1RallyCalculator.cs: Run | ScenarioA1RallyPackageTests.FateReducesTheUnitAndNeverRalliesIt |  | Original 12 Reduces and never rallies. | audit |
| A10.7 | Leadership | 68 | built | ScenarioA1RallyCalculator.cs: Run; ScenarioA1FireCalculator.cs: Check, AlliedPenalty; GamePlanner.Rally.cs: PlanRally | ScenarioA1RallyPackageTests; ScenarioA1FirePackageTests |  | Leadership as a DRM for others in the Location, not cumulative, never for himself, one worse for Allied Troops (R15.8). The Passenger or Rider leader EXC was not traced. | audit |
| A10.71 | Rally | 69 | built | ScenarioA1RallyCalculator.cs: Outside | ScenarioA1RallyPackageTests; PlayPagePass31Tests |  | A broken leader Self-Rallies unless an unbroken leader is present; units without Self-Rally wait for him; the player names the rallying leader. | audit |
| A10.711 | Voluntary Rout | 69 | not built |  |  | Pass 48 | No action lets an unbroken leader rout with a broken unit, share its Interdiction, or add his DRM to the Interdiction NMC. | audit |
| A10.72 | Mandatory Leadership | 69 | built | ScenarioA1FireCalculator.cs: Check; ScenarioA1CloseCombatCalculator.cs: AmbushDrm; GamePlanner.FireExtensions.cs: ConcealmentGains; GamePlanner.MoppingUp.cs: PlanMopUp | ScenarioA1FirePackageTests |  | A non-zero modifier is always applied in MC, Rally, Ambush, Concealment and Search Casualties, the best one chosen where several leaders are present. Integrity Checks belong to A16, which is not built. | audit |
| A10.8 | Fanaticism | 69 | partly | ScenarioA1FireCalculator.cs: Morale, Cowering; GamePlanner.Overrun.cs: PaatcExempt; GamePlanner.Rout.cs: PlanRout (no surrender); ScenarioA1RallyCalculator.cs: Run | ScenarioA1FireExtensionTests.AFanaticUnitHasAHigherMoraleAndIsNeverDisrupted; ScenarioA1Pass19bTests.BattleHardeningMakesTheCardSquadsFanatic | Pass 48 | Built for Fanaticism gained by Battle Hardening: +1 to both Morale Levels, no Cowering, no RtPh surrender, no PAATC, no Disruption, a created SMC is Fanatic. Missing: no card or SSR can make a unit Fanatic; the Interdiction NMC ignores the +1. | audit |

#### A11 Close Combat (CC) (pages 72 to 76)

36 rows: 19 built, 1 built with a deviation, 12 partly, 3 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A11 | Close Combat (CC) | 72 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| A11.1 | (none) | 72 | partly | GamePlanner.CloseCombat.cs: PlanCloseCombat, PlanAdvanceUnits; ScenarioA1CloseCombatCalculator.cs: Resolve | ScenarioA1CloseCombatTests; CloseCombatRecordTests | Pass 48 | CC in the CCPh in one Location, simultaneous, no TEM. Refused: CC with a Gun's crew (the advance and the berserk charge refuse it, citing R24.3). Pillbox CC needs B30. | audit |
| A11.11 | Resolution | 72 | built | ScenarioA1CloseCombatPackage.cs: Column, RedKill; ScenarioA1CloseCombatCalculator.cs: Figure, Odds | ScenarioA1CloseCombatTests.TheOddsRoundDownToThePrintedColumn; ScenarioA1RefereeFixesTests.OddsAboveTenButBelowElevenToOneAreTenToOne |  | Odds rounded down, every column from less than 1-8 to more than 10-1 (R14.13), kill, Partial Kill by Random Selection, red numbers for Hand-to-Hand. | audit |
| A11.12 | Mechanics | 72 | built | GamePlanner.CloseCombat.cs: PlanCloseCombat; ScenarioA1CloseCombatCalculator.cs: Outside, Round.Run | CloseCombatRecordTests.ALocationsCcIsResolvedOnceAndUnitsLeftTogetherAreHeldInMelee |  | One Location at a time, all attacks designated then resolved, ATTACKER first, no unit attacks or is attacked twice. | audit |
| A11.13 | SW | 72 | built | ScenarioA1CloseCombatCalculator.cs: Round.Run (weaponEffects) | ScenarioA1CloseCombatTests.ASupportWeaponOfAnEliminatedUnitMayBeLostOnAColoredOne |  | SW never add to CC; a colored dr of 1 and a dr at most the Kill Number loses each SW. Guns do not arise since crew CC is refused. | audit |
| A11.14 | SMCs | 72 | built | ScenarioA1CloseCombatCalculator.cs: Outside (stacking), UnitState.Firepower, Round.Run (forfeited) | ScenarioA1CloseCombatTests; CloseCombatComponentTests.StackingAndWithdrawalsRaiseEachUnitsChoice |  | SMC FP of 1, stacking declared before attacks, alone or with its MMC, a Known SMC with a concealed MMC forfeits the "?". | audit |
| A11.141 | Leader | 72 | built | ScenarioA1CloseCombatCalculator.cs: CommonDrm, Outside (director); ScenarioA1FireCalculator.cs: LeaderLoss | ScenarioA1CloseCombatTests |  | One directing leader, not alone and not with a berserk unit; no LLMC or LLTC in CC or in a Melee Location. | audit |
| A11.15 | Melee | 72 | built | GameProjector.cs: InMelee, HoldsInMelee, KeepMelee; GamePlanner.Movement.cs; GamePlanner.Fire.cs; ScenarioA1CloseCombatCalculator.cs: Outside (reinforcement-must-attack) | CloseCombatRecordTests.ALocationsCcIsResolvedOnceAndUnitsLeftTogetherAreHeldInMelee; ScenarioA1RefereeFixesTests.D5AUnitReinforcingAMeleeMustAttack; ScenarioA1Pass12Tests.FireIntoAMeleeAttacksBothSidesEachAgainstItsOwnElr |  | Melee lock, the concealed unit's exemption, reinforcements must fight, fire into a Melee hits both sides. Cavalry, cyclists and skiers are not in the catalog. | audit |
| A11.16 | Broken Units | 72 | built | ScenarioA1CloseCombatCalculator.cs: Figure (vs-broken), Outside; GamePlanner.CloseCombat.cs: MustWithdraw; GamePlanner.cs: CCPh end; GameProjector.cs | CloseCombatStepsTests.ABrokenUnitHeldInMeleeMustWithdrawAndADisruptedOneIsEliminated |  | -2 against a broken unit, it never attacks, defends at full FP, must try to withdraw, and is eliminated at the end of the CCPh if it cannot. | audit |
| A11.17 | Stealth | 73 | partly | ScenarioA1CloseCombatCalculator.cs: AmbushDrm; GamePlanner.MoppingUp.cs: PlanMopUp | ScenarioA1CloseCombatTests | Pass 63 | Only a Good Order hero or heroic leader is Stealthy, and only in the Ambush dr and the Mopping Up Casualty dr. Missing: Stealth by SSR, Elite and 1st-Line Finns (in the catalog), the -1 in the Concealment dr, Searching. | audit |
| A11.18 | Lax | 73 | partly | ScenarioA1CloseCombatCalculator.cs: Inexperienced, AmbushDrm; GamePlanner.MoppingUp.cs: PlanMopUp | ScenarioA1CloseCombatTests | Pass 63 | Inexperienced (and berserk) units are Lax in the Ambush dr and the Mopping Up Casualty dr. Missing: Lax by SSR, the +1 in the Concealment dr, Searching. | audit |
| A11.19 | Concealment | 73 | built | ScenarioA1CloseCombatCalculator.cs: Odds (vs-concealed), Concealment; LiveCloseCombat.cs: Units; GameProjector.cs (HiddenPlaced) | ScenarioA1Pass14Tests.AConcealedDefenderHalvesTheAttackAndAConcealedAttackerLosesItsConcealment |  | Halved FP against a concealed defender, Dummies removed, hidden units placed beneath "?" as the CC starts, "?" lost only by attacking or Reduction (R14.2). How the page shows a concealed unit's Strength Factor was not checked. | audit |
| A11.2 | Withdrawal From Melee | 73 | built | ScenarioA1CloseCombatCalculator.cs: Outside (withdrawal-outside), Figure (vs-withdrawing, covering); GamePlanner.CloseCombat.cs: PlanCloseCombat | ScenarioA1RefereeFixesTests.AWithdrawalIsFromMeleeByAnUnpinnedUnitThatMakesNoAttack |  | From Melee only, not pinned, berserk or Disrupted, no attack, -2 and +1 for each friendly unit that stays. | audit |
| A11.21 | Withdrawal Mechanics | 73 | partly | GamePlanner.CloseCombat.cs: WithdrawalDestinations, WithdrawalTires, Laden | CloseCombatStepsTests.AWithdrawalMayEnterALocationHeldByAFriendlyGuard; PlayPageCloseCombatTests.AUnitHeldInMeleeWithdrawsFromThePage | Pass 48 | Built: an ADJACENT Location an advance could enter, CX when needed. Missing: a unit over its IPC is refused instead of leaving its SW (R5.5); a Location with any enemy unit, concealed or hidden, is barred, so the elimination and reveal on entering a concealed enemy's Location is not built and the bar tells of a hidden unit; other building levels. | audit |
| A11.22 | Infiltration | 73 | built with a deviation | ScenarioA1CloseCombatCalculator.cs: Round.Run (departed); GamePlanner.CloseCombat.cs: PlanCloseCombat (infiltrations) | ScenarioA1Pass14Tests.AnOriginalTwoLetsTheAttackerInfiltrateBeforeTheDefenderStrikes; BacklogPass14Tests.AnInfiltratingSquadLeavesBeforeTheDefenderStrikes | Stands by its ruling | Ruling R14.8: the Infiltration and its destination are declared with the round, before the DR, not chosen after the Original 2 or 12 is seen. | audit |
| A11.3 | Sequential CC | 73 | built | LiveCloseCombat.cs: FromState; GamePlanner.CloseCombat.cs: DeclaringSides | CloseCombatRecordTests.AnAmbushOrdersTheRoundsAndHoldsThePhaseUntilTheSecond |  | Sequential rounds: a unit eliminated before its turn loses its attack; withdrawals are declared first. | audit |
| A11.31 | vs Vehicle | 73 | partly | GamePlanner.VehicleCloseCombat.cs: PlanSequentialCloseCombat, FirstCcSide, NextCcSide | BacklogPass11Tests.InfantryAdvanceOnAnAfvAfterAPaatcAndFightItInSequentialCc; ScenarioA1Pass11Tests | Pass 48 | Built: one attack at a time, the non-vehicular side first, alternating, ATTACKER first with vehicles on both sides. Refused: Infantry against Infantry in a Location holding a vehicle (R11.16). | audit |
| A11.32 | Ambush | 73 | built | LiveCloseCombat.cs: FromState; ScenarioA1CloseCombatCalculator.cs: Outside (round) | ScenarioA1CloseCombatTests.AnAmbushOccursAtThreeBelowAndGivesItsDrmAndOrder |  | The ambusher resolves all its attacks first. | audit |
| A11.33 | Prisoners | 73 | built | LiveCloseCombat.cs: FromState (PrisonersRound); ScenarioA1CloseCombatCalculator.cs: Round.Run | ScenarioA1Pass14Tests.PrisonersEscapeAfterANtcOnlyAgainstABrokenGuard |  | The prisoners' round comes first (R14.6). | audit |
| A11.34 | (none) | 73 | partly | LiveCloseCombat.cs: FromState | ScenarioA1Pass14Tests | Pass 48 | Prisoners before Ambush is kept. Missing: an Ambush in a Location holding a vehicle, since no Ambush dr is made there (R11.16), so the rule's own example cannot be played. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A11.4 | Ambush | 73 | partly | ScenarioA1CloseCombatCalculator.cs: AmbushPossible, ResolveAmbush, AmbushDrm, CommonDrm, Concealment; GamePlanner.CloseCombat.cs: PlanAmbush, AmbushDue | ScenarioA1CloseCombatTests.AnAmbushOccursAtThreeBelowAndGivesItsDrmAndOrder; ScenarioA1Pass14Tests.AnAmbushCanOccurWithAConcealedUnitInOpenGroundAndConcealmentGivesMinusTwo | Pass 48 | Built for Infantry: when it can occur, three below, the drm for CX, Lax, broken, pinned, berserk, concealed, Stealthy and leadership, -1 and +1 to attacks, the ambusher keeps its "?", the ambushed lose theirs. Missing: any Ambush with a vehicle present and the vehicle, BU and pillbox drm (R11.16); Stealthy is heroes only. | audit |
| A11.41 | Ambush Withdrawal | 73 | built | GamePlanner.Prisoners.cs: PlanAmbushWithdrawal; GameProjector.cs (close after withdrawal) | BacklogPass14Tests.TheAmbushersMayWithdrawBeforeTheFirstRound |  | Ruling R14.9: before the first round or once the CC is over, not pinned, berserk or Disrupted. It shares A11.21's destination limits. | audit |
| A11.5 | CC vs a Vehicle | 74 | partly | ScenarioA1VehicleCloseCombat.cs: AgainstVehicle, BaseCcv, Precheck; GamePlanner.VehicleCloseCombat.cs: PlanVehicleAttack | ScenarioA1Pass11Tests.InfantryAttackAVehicleWithTheirCcvAsTheKillNumber; ACombiningLeaderAddsOneCcvAndHisLeadershipAndAPinAndAReactionMarkerSubtractOne | Pass 48 | Built: CCV 5, 4, 3, 2, +1 combining SMC, -1 Inexperienced, -1 pinned, destroyed, burning at half, immobilized on the number, two units at most. Missing: +1 for an Assault Engineer (the catalog has the trait, the code does not read it), other halving penalties, Cavalry. | audit |
| A11.501 | Unlikely Kill | 74 | built | ScenarioA1VehicleCloseCombat.cs: AgainstVehicle (unlikely) | ScenarioA1Pass11Tests.AnOriginalTwoRollsTheUnlikelyKillAndAnOriginal12ReducesTheAttackers |  | The third die after an Original 2; the DR's own result stands unless the dr does better. | audit |
| A11.51 | CC DRM | 74 | partly | ScenarioA1VehicleCloseCombat.cs: AgainstVehicle, MannedMg | ScenarioA1Pass11Tests | Pass 48 | Built: -3 unarmored, -1 no manned MG, +1 or +2 for each escort, +2 Motion, hero, a combining leader. Missing: the Ambush and Street Fighting DRM (no Ambush with a vehicle), turreted MA of 15mm or less as MG armament, the "armed" test on escorts. | audit |
| A11.52 | Capture of Vehicle in CC | 74 | partly | GamePlanner.VehicleCloseCombat.cs: CapturedVehicles | BacklogPass11Tests | Pass 48 | An unarmed stopped vehicle alone with enemy Infantry is captured as the CCPh begins and marked Abandoned (R11.16). Missing: capturing Passengers with -2 and +2, an Abandoned AFV under A21.2, any use of a captured vehicle. | audit |
| A11.6 | CC vs an AFV | 75 | built | GamePlanner.CloseCombat.cs: PlanAdvanceUnits; GamePlanner.Overrun.cs: NeedsPaatc, PaatcExempt, PaatcFacts, AddPaatc | BacklogPass11Tests.InfantryAdvanceOnAnAfvAfterAPaatcAndFightItInSequentialCc |  | Ruling R11.17: PAATC before advancing on a manned unconcealed AFV, failure pins, SMC, Fanatic and berserk exempt, leader aid, 1TC when Inexperienced, each unit tests and goes on its own. | audit |
| A11.61 | CC Attack DRM | 75 | built | ScenarioA1VehicleCloseCombat.cs: AgainstVehicle | ScenarioA1Pass11Tests |  | -2 against an OT AFV, -1 against a CE CT AFV, -1 Immobile once, a BMG voids the no-MG DRM. No catalog vehicle is marked partially armored. | audit |
| A11.611 | PRC | 75 | partly | GamePlanner.VehicleCloseCombat.cs: PlanVehicleAttack (vehicle-wrecked) | ScenarioA1Pass11Tests | Pass 51 | A vehicle destroyed in CC leaves a wreck with no crew survival. Missing: Riders attacked apart with +1 and -1; no Rider logic exists in Play or Rules. | audit |
| A11.612 | AF | 75 | built | ScenarioA1VehicleCloseCombat.cs: AgainstVehicle | ScenarioA1Pass11Tests.InfantryAttackAVehicleWithTheirCcvAsTheKillNumber |  | Nothing in CC reads an AF; the Kill Number is the CCV alone. | audit |
| A11.62 | AFV CC Attacks vs Infantry | 76 | partly | ScenarioA1VehicleCloseCombat.cs: VehicleFirepower, ByVehicle, DefenseCcv; GamePlanner.VehicleCloseCombat.cs: PlanVehicleAttacksInfantry | ScenarioA1Pass11Tests.AVehicleAttacksInfantryWithItsCmgAndCeAamgAgainstTheirCcv | Pass 53 | Built: CMG plus CE AAMG in one attack against the defenders' CCV on the CCT, halved in Motion and against concealed units, none when Stunned or Shocked. Missing: RMG, IFE of a turreted MA of 15mm or less, halftrack Passengers and Riders, separate attacks by each weapon, a CMG limited to the VCA. | audit |
| A11.621 | Crew Small Arms | 76 | built | ScenarioA1VehicleCloseCombat.cs: AgainstVehicle | ScenarioA1Pass11Tests.AnOriginalTwoRollsTheUnlikelyKillAndAnOriginal12ReducesTheAttackers |  | An Original 12 against a crewed vehicle not Abandoned, Shocked or Stunned Reduces the attackers. | audit |
| A11.622 | Close Defense Weapon System | 76 | not built |  |  | Pass 56 | No sN capability in the catalog or the code. Needs D13.3 usage numbers and a German AFV that carries it. | audit |
| A11.7 | Vehicle Withdrawal From CC | 76 | built | GameProjector.cs: InMelee, HoldsInMelee; GamePlanner.Fire.cs: A7.212 limit | BacklogPass11Tests |  | A vehicle is never held in Melee and holds Known enemy Infantry unless Abandoned or in Motion (R11.16). The vehicle's own fire limit to its Location was read in the general A7.212 check, not traced for ordnance Case E. | audit |
| A11.71 | Other Withdrawal From CC | 76 | not built |  |  | Pass 51 | No rule for Passengers or Riders in Melee, dismounting into it, or the ATTACKER's declaration of who stays. Cavalry, cyclists and skiers are not in the catalog. | audit |
| A11.8 | Street Fighting | 76 | not built |  |  | Pass 48 | No Street Fighting: no advance onto the vehicle from an adjacent building with a return, no -1 and +1, no Reaction Fire from adjacent buildings, nothing for a vehicle in Bypass. | audit |

#### A12 Concealment (pages 76 to 80)

24 rows: 4 built, 17 partly, 1 refused, 1 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A12 | Concealment | 76 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| A12.1 | Definition | 76 | partly | GamePlanner.Map.cs: WithSeen, EnemyGoodOrderInLosWithin16; GamePlanner.VehicleConcealment.cs: Watching; GamePlanner.FireExtensions.cs: ConcealmentGains | ScenarioA1FirePackageTests.ConcealmentHalvesFirepowerAndIsLost | Pass 48 | "?" as a state is built. The Good Order against unbroken distinction is kept only for fire (R31d.2); movement, advance, Rally and CA change read any unbroken enemy (backlog 51), and an unmanned vehicle is not treated as "broken" there or in gain. | audit |
| A12.11 | Known/Dummy Enemy Unit | 76 | partly | GameView.cs; ScenarioSetup.cs; GamePlanner.Movement.cs: Dummy branches; LiveCloseCombat.cs: Units | PlayRecordsPass31dTests.ADummyStackRemovedByItsMoveIsSaidOnceToBothSides; ScenarioA1FireExtensionTests.HiddenUnitsAreAttackedAsConcealedAndDummiesAreRemoved | Pass 48 | Built: a "?" stack is not Known and not inspected, Dummies from the OB "?" at setup, they move as real units and are removed when seen (R10.10, R31d.3). Missing: 5/8" Dummies, Dummies among reinforcements (R20.5), the mine-attack proof, an advancing Dummy in Open Ground (backlog 51). | audit |
| A12.12 | Placement | 77 | partly | ScenarioSetup.cs; GamePlanner.Disclosure.cs: OutOfSight, NonObConcealment, PlanNonObConcealment; GamePlanner.FireExtensions.cs: ConcealmentGains | BacklogPass23Tests.AFarStackTakesANonObQuestionMark; NoNonObQuestionMarkInSightOrBeforeBothHaveSetUp; BacklogPass19Tests | Pass 48 | Built: setup out of sight, OB "?" only in Concealment Terrain (R19.5), non-OB "?" once both have set up (R23.6), gain only by Good Order Infantry at the end of its own Player Turn. Refused: a "?" on a unit waiting off board to enter (R20.5). | audit |
| A12.121 | Concealment Loss/Gain Table | 77 | partly | GamePlanner.FireExtensions.cs: ConcealmentGains; GamePlanner.Movement.cs: Unmask; GamePlanner.VehicleConcealment.cs: VehicleConcealmentLost | BacklogPass12Tests; BacklogPass6Tests | Pass 48 | The Infantry row is built for gain (NA, I, J, K) and for the common losses. Missing: gain for an Emplaced Gun's crew and for vehicles (J, K), the Fortification row, most of Case C. | audit |
| A12.122 | Concealment dr | 77 | partly | GamePlanner.FireExtensions.cs: ConcealmentGains, AddConcealmentGains | BacklogPass12Tests; BacklogPass16TablePlayerTests | Pass 48 | Both dr cases, 5 or less, +US#, leadership unless alone, -TEM and SMOKE (R12.5). Missing: +1 Lax, -1 Stealthy, the US# of a possessed Gun. | audit |
| A12.13 | Effect | 77 | partly | ScenarioA1FireCalculator.cs (concealed column); ScenarioA1OrdnanceCalculator.cs (Case K, concealment-mixed) | ScenarioA1FirePackageTests.ConcealmentHalvesFirepowerAndIsLost; ScenarioA1OrdnanceTests.AGunMayFireAgainOnAKeptRofAndMixedConcealmentIsUndecided | Pass 48 | Built for IFT fire: halved as Area Fire, one DR on two columns for concealed and Known targets. Refused: ordnance at a Location holding both concealed and Known targets (undecided "concealment-mixed"). | audit |
| A12.14 | Removal | 77 | partly | GamePlanner.Map.cs: WithSeen, EnemyGoodOrderInLosWithin16; GamePlanner.Movement.cs: Unmask; GamePlanner.CloseCombat.cs: PlanAdvanceUnits; ScenarioA1FireCalculator.cs; GamePlanner.Snipers.cs | ScenarioA1FirePackageTests.ConcealmentHalvesFirepowerAndIsLost; BacklogPass31dTests; ScenarioA1Pass14Tests | Pass 48 | Built: loss on a failed MC, break, Reduction, a PTC or worse, a Sniper, Non-Assault Movement, entering Open Ground, firing, a CC attack, an enemy entry. Deviation: the loss is always forced (R10.10, R31d.2). Missing: voluntary removal, the overstacked case, the Dummy stack's Morale Level 7 out of LOS. Fault: outside fire the viewer need only be unbroken, see A12.1. | audit |
| A12.141 | Actions | 78 | partly | GamePlanner.Rally.cs: PlanRally; ScenarioA1RallyCalculator.cs: Run; GamePlanner.Guns.cs (CA change) | ScenarioA1RallyPackageTests | Pass 48 | A Rally attempt (unit and leader) and a CA change cost "?" within 16 hexes and LOS. Not found for Deploy, Recombine, SMOKE grenades, PF or MOL checks, or a leader's DRM to a MC. | audit |
| A12.15 | Detection | 78 | partly | GamePlanner.Movement.cs: PlanMove (Reveal, Unmask, forcedBack); GamePlanner.cs: concealed entry and declaration | ConcealedEntryTests.AnEntryIntoOneConcealedOrHiddenSquadRevealsItAndForcesTheMoverBack; RallyAndFireStepsTests | Pass 48 | Built: an entry reveals one unit by Random Selection, hidden units go beneath "?" first, the mover is forced back with the MF spent and loses its own "?", Dummies alone are removed, a Dummy mover is removed, Residual FP attacks on return. Missing: the Infantry OVR offer on a lone SMC outside the reviewed building case, a Minimum Move entry (refused), a routing unit's entry (A10.533). | audit |
| A12.151 | Bypass | 78 | partly | GamePlanner.Movement.cs: PlanMove (bypassing); GamePlanner.Terrain.cs: BypassStep | none found | Pass 48 | Bypass of a hex holding a concealed unit causes no detection, as the rule says. Missing: the loss of all "?" when a Bypassing unit ends its MPh there, the TPBF and -2 that follow. Refused: occupying an obstacle that holds enemy units (R10.7). | audit |
| A12.152 | Searching | 79 | not built |  |  | Pass 48 | No Search action, Search dr or TI from it. | audit |
| A12.153 | Mopping Up | 79 | partly | GamePlanner.MoppingUp.cs: PlanMopUp, MopUpBuildings | BacklogPass24Tests.MoppingUpSecuresTheBuilding; MoppingUpPlacesHiddenUnitsBeneathQuestionMarks | Pass 48 | Built (R24.2) for the buildings a scenario card names: PFPh, TI, two-hex reach, hidden units placed, Dummies removed, Control, broken units surrender, once a Player Turn, never after No Quarter. Missing: buildings not on a card or a game without one, Rowhouse hexes, rubbled or Blazing Locations, Fortified Building reveal. | audit |
| A12.154 | Search Casualties | 79 | partly | GamePlanner.MoppingUp.cs: PlanMopUp | BacklogPass24Tests.AConcealedEnemyCostsTheMoppingUp | Pass 48 | The Casualty dr after Mopping Up is built, rolled at once for the DEFENDER (R24.2), with its drm. Missing: the Search half, and the minefield, Residual FP, FFE and Booby Trap triggers. | audit |
| A12.16 | Right of Inspection | 79 | partly | GameView.cs | StateTests.ASideDoesNotSeeHiddenUnitsAndSeesConcealedOnesAsSealedPresence | Pass 48 | A side sees an enemy stack not under "?" in full and a concealed one as a presence. Missing: the limit to verification only when the stack is out of the LOS of all its Good Order units; the view does not read LOS. | audit |
| A12.2 | Concealed 5/8" Counters | 79 | partly | GamePlanner.VehicleConcealment.cs: VehicleConcealmentTerrain, VehicleConcealmentLost; GamePlanner.Vehicles.cs; GamePlanner.Fire.cs; GamePlanner.Ordnance.cs | BacklogPass6Tests; BacklogPass7Tests.AConcealedTankThatFiresLosesItsQuestionMark; ScenarioA1Pass6Tests | Pass 48 | Built (R6.7): a vehicle sets up concealed only in grain in season, loses "?" under Case H, on moving within 16 hexes, on firing, on a hit. Missing: woods-road and like hexes at setup, Target Size kept secret, the Smoke Dispenser, vehicle gain of "?", Cavalry, horses, bicycles. | audit |
| A12.3 | Hidden Initial Placement (HIP) | 80 | partly | ScenarioSetup.cs: HipAllowance and hidden checks; GameView.cs; GamePlanner.Night.cs (hip token) | BacklogPass23Tests | Pass 48 | Built (R23.5): HIP by an SSR token for Infantry in Concealment Terrain, shown to its own side only. Refused: hidden vehicles and any other counter. | audit |
| A12.31 | (none) | 80 | built | GamePlanner.VehicleConcealment.cs: RevealEvents; GamePlanner.Movement.cs: Reveal; GameProjector.cs | BacklogPass23Tests; EntryEventTests.AHiddenUnitPlacedBeneathAQuestionMarkIsNotRevealed |  | A reveal clears hidden and "?" together; a hidden unit goes beneath "?" only in the named cases (CC start, detection, Mopping Up, its owner's placement). No action hides a unit again. | audit |
| A12.32 | (none) | 80 | built | GamePlanner.Disclosure.cs: PlanPlaceHidden; GamePlanner.Movement.cs; GamePlanner.CloseCombat.cs: PlanAdvanceUnits | BacklogPass23Tests |  | A hidden unit must be placed beneath "?" before it moves or advances and may be placed at any time (R23.5). Fault beside it: a hidden unit bars an enemy's "?" gain without being placed, see findings. | audit |
| A12.33 | Fortifications | 80 | refused | ScenarioSetup.cs: play.setup-hidden | none found | Pass 37 | A Fortification counter set up hidden is refused ("HIP is built for Infantry and Emplaced Guns only"). Fortifications have no effect in play at all, so nothing here can be built before B27 to B30. | audit |
| A12.34 | Hidden Guns | 80 | partly | ScenarioSetup.cs (emplacedHidden); GamePlanner.Ordnance.cs: reveal on the colored dr; GamePlanner.Map.cs: NearestGoodOrderEnemyInLos | BacklogPass26Tests | Pass 48 | Built (R26.5): an Emplaced Gun and its crew hide together in Concealment Terrain with no SSR, and firing reveals on a colored dr of 5 or more within 16 hexes, 6 beyond. Refused: HIP out of Concealment Terrain when out of all enemy LOS. RCL, IFE and zero range cases not traced. | audit |
| A12.4 | Vehicular Movement Through Concealment | 80 | built | GamePlanner.Vehicles.cs: vehicle entry | BacklogPass11Tests.ConcealedUnitsEnteredByAVehicleTakeACombinedPaatcAndStayConcealedWhenTheyPass |  | A vehicle may enter a Location of concealed units. | audit |
| A12.41 | OVR | 80 | built | GamePlanner.Overrun.cs: PaatcSubjects, PaatcChoice, PlanPaatcAnswer, PaatcFacts; GamePlanner.Vehicles.cs | BacklogPass11Tests.ConcealedUnitsEnteredByAVehicleTakeACombinedPaatcAndStayConcealedWhenTheyPass; AFailedCombinedPaatcPinsAndRevealsTheUnits |  | Ruling R11.12: reveal or one combined PAATC on the lowest Morale Level (a Dummy's 7), 1PAATC, best leader, failure pins and reveals, OVR only after it, none for Bypass or a woods-road or a vehicle whose crew cannot see. | audit |
| A12.42 | Bypass | 80 | partly | GamePlanner.Vehicles.cs (VBM entry exempt); GamePlanner.Overrun.cs: OverrunBar (bypass) | none found | Pass 48 | Built: a vehicle in Bypass makes no OVR and forces no reveal or PAATC. Missing: the reveal of the concealed units when the vehicle or its Passengers end the MPh in that Location. | audit |

#### A13 Cavalry (pages 80 to 82)

20 rows: 19 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A13 | Cavalry | 80 | not applicable |  |  |  | heading | audit |
| A13.1 | (none) | 80 | not built |  |  | Pass 43 | No Cavalry or Horse counter in the catalog or vocabulary and no code; needs a Rider state and a Horse counter. | audit |
| A13.2 | Stacking | 80 | not built |  |  | Pass 43 | Cavalry stacking and unlimited unmounted horses; needs A13.1. | audit |
| A13.3 | Movement | 80 | not built |  |  | Pass 43 | Cavalry MF column of the Terrain Chart, Minimum Move with CX, no leader bonus; none present. | audit |
| A13.31 | MF Costs | 80 | not built |  |  | Pass 43 | Mounting and dismounting costs and the 25 percent MF exchange; none present. | audit |
| A13.32 | Capacity | 81 | not built |  |  | Pass 43 | Horse counter capacity, Deploying and Recombining Horse counters; none present. | audit |
| A13.33 | Portage | 81 | not built |  |  | Pass 43 | Cavalry portage limits; none present. | audit |
| A13.34 | MF Allotment | 81 | not built |  |  | Pass 43 | Horse 12 MF allotment; none present. | audit |
| A13.35 | Enemy Units | 81 | not built |  |  | Pass 43 | Cavalry entering enemy Locations; none present. | audit |
| A13.351 | FPF | 81 | not built |  |  | Pass 43 | TPBF against Cavalry as FPF, and OVR Prevention by a Gun crew; none present. | audit |
| A13.36 | Gallop | 81 | not built |  |  | Pass 43 | Gallop and its terrain bars; none present. | audit |
| A13.4 | Fire Effects | 81 | not built |  |  | Pass 43 | Mounted Fire halving and the SW, Assault Fire, Spraying Fire, SMOKE bars; none present. | audit |
| A13.5 | Vulnerability | 81 | not built |  |  | Pass 43 | The -2 DRM against Cavalry and the TEM exclusions; none present. | audit |
| A13.51 | MC | 81 | not built |  |  | Pass 43 | Broken Cavalry Bails Out and its horse is removed; none present. | audit |
| A13.511 | (none) | 81 | not built |  |  | Pass 43 | Fire against horses on the H Vehicle Line of the IFT; none present. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A13.52 | Pin and Heat of Battle | 81 | not built |  |  | Pass 43 | Cavalry exempt from PTC, LLMC, LLTC, Pin, and Heat of Battle; ScenarioA1HeatOfBattle.Subject only says Cavalry is not in the catalog. | audit |
| A13.6 | Charge | 81 | not built |  |  | Pass 43 | Cavalry Charge; none present. | audit |
| A13.61 | Post Resolution | 81 | not built |  |  | Pass 43 | After a Charge: stay, dismount, or move on, CC counter, Prep Fire marker; none present. | audit |
| A13.62 | Cavalry Wave | 81 | not built |  |  | Pass 43 | Cavalry Wave; also needs Human Wave (A25.23), which is not built. | audit |
| A13.7 | Horses | 82 | not built |  |  | Pass 43 | Horses as units, their capture, leading, and elimination; none present. | audit |

#### A14 Snipers (page 82)

12 rows: 2 built, 1 built with a deviation, 6 partly, 2 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A14 | Snipers | 82 | not applicable |  |  |  | heading | audit |
| A14.01 | (none) | 82 | partly | GamePlanner.Snipers.cs: AddSniperAttacks; UnitPlausibility.cs (SAN 2 to 7) | BacklogPass15TablePlayerTests.ASideWithNoSniperCounter | Pass 49 | The Sniper is an entity, not a unit, and claims no LOS. Missing: nothing gives a side with a SAN of 2 or more its Sniper counter; with no counter placed by hand the side silently makes no Sniper attacks. | audit |
| A14.1 | Sniper Activation Number (SAN) | 82 | partly | GamePlanner.Snipers.cs: AddSniperAttacks; ScenarioCards.cs (card.san 0 to 7) | BacklogPass15Tests.ADrEqualToTheEnemySanCallsForASniperAttack; BacklogPass15TablePlayerTests.AMoraleCheckDrCallsTheEnemySniper | Pass 49 | Built for the IFT, MC, and TC DRs of fire records in the PFPh, MPh, DFPh, AFPh, prisoners excepted, in DR order, night SAN (R16.7). Missing (R15.5, backlog): To Hit, Entrenching, PAATC and other DRs outside fire records; any change or removal of a SAN. | audit |
| A14.2 | Target Selection | 82 | partly | GamePlanner.Snipers.cs: Snipe | BacklogPass15TablePlayerTests.ASniperAmongSeveralTargetsAndAConcealedStack | Pass 49 | Built: the Sniper dr, the Random Location DR from the counter's hex, the counter's move, Random Selection, a new dr for each other tied target. Missing: the setup placement rule (six hexes of six enemy hexes), forfeiting to reposition, the Sniper player's pick of Location (the game picks the fullest, R15.5), his pick of the tied target that takes the first dr, and his prior choice of an enemy Sniper, a Vulnerable crew, or an unarmored vehicle. | audit |
| A14.21 | Alternate Target | 82 | built with a deviation | GamePlanner.Snipers.cs: Snipe (hexes ordering) | BacklogPass15Tests.ADrEqualToTheEnemySanCallsForASniperAttack | Pass 49 | R15.5: closest hex with an eligible target, ties to the lowest TEM, then the first hex by name rather than the Sniper player's choice. The TEM read is the terrain's only (no SMOKE, no HA or Factory exclusion), the enemy Sniper counter does not count as a target, and hidden Fortifications are not revealed. | audit |
| A14.22 | Non-Targets | 82 | partly | GamePlanner.Snipers.cs: Snipe (eligible) | BacklogPass15TablePlayerTests.ASniperAmongSeveralTargetsAndAConcealedStack | Pass 49 | Excludes hidden units, prisoners, friendly units, and every vehicle. Missing: Interior Building Locations are not excluded (backlog), and Vulnerable PRC, CE crews, and unarmored vehicles are never targets. | audit |
| A14.23 | Concealed Targets | 82 | built | GamePlanner.Snipers.cs: Snipe (hidden stack, real count) | BacklogPass15TablePlayerTests.ASniperAmongSeveralTargetsAndAConcealedStack; ASniperEliminatesADummyStackOnATwo |  | A concealed stack is one candidate; a Dummy stack is eliminated, one real unit is the target, more are drawn by Random Selection. | audit |
| A14.3 | Resolution | 82 | partly | GamePlanner.Snipers.cs: SnipeUnit; Snipe (DM) | BacklogPass15Tests.ADrEqualToTheEnemySanCallsForASniperAttack | Pass 49 | Built: dr 1 eliminates a SMC, breaks a MMC or Reduces one that cannot break; dr 2 wounds a SMC with a Wound Severity dr and pins a MMC; DM on broken units; no DRM. Missing: every result against a CE crew, an Inherent crew, a vehicle, or a Sniper; no LLMC or LLTC follows a leader's loss (backlog, referee pass 15). | audit |
| A14.31 | vs Sniper | 82 | partly | GamePlanner.Snipers.cs: Snipe (pinned Sniper makes no attack) | EntityTests.APinnedSniperShowsItsPinnedSideAndAPinnedSquadDoesNot | Pass 49 | A pinned Sniper counter is skipped and shows its pinned side, but nothing in the planner ever pins or attacks a Sniper: it is never a target and Sniper Check is not built. | audit |
| A14.32 | vs CC | 82 | built | GamePlanner.Snipers.cs: Snipe (eligible is the attacked side only) | none found |  | A Sniper attack picks only units of the attacked side, so friendly units in a Melee Location are never harmed. No test reaches a Melee Location. | audit |
| A14.33 | vs Unarmored Vehicle | 82 | not built |  |  | Pass 49 | Vehicles are excluded from Sniper targets; needs unarmored vehicles as targets and the immobilization result on a dr of 1. | audit |
| A14.4 | Sniper Check | 82 | not built |  |  | Pass 49 | No Sniper Check, no SAN reduction, no removal of the counter at SAN 1; needs an action for the units of the target Location, TI on them, and a SAN that can change. | audit |

#### A15 Heat of Battle (pages 83 to 84)

17 rows: 9 built, 1 built with a deviation, 6 partly, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A15 | Heat of Battle | 83 | not applicable |  |  |  | heading | audit |
| A15.1 | (none) | 83 | partly | ScenarioA1HeatOfBattle.cs: Subject, Resolve; ScenarioA1FireCalculator.cs: Round.HeatOfBattle; ScenarioA1RallyCalculator.cs (heatOfBattle) | ScenarioA1FireExtensionTests.AnOriginalTwoOnAnMcCallsForHeatOfBattle; ScenarioA1RallyPackageTests.ALeaderRallysOriginalTwoCallsForHeatOfBattle; ScenarioA1Pass27Tests.AxisMinorHeatOfBattleTakesPlusThreeAndNonEliteMmcSurrenderOnTen | Pass 49 | Built: the table, the DRM for eight nationalities, elite, broken, Inexperienced, a 5 or 6 doing both. Refused: any other nationality (Japanese, Allied Minor, Partisan) with hob.nationality-unreviewed. Missing: Unarmed units and PRC are not exempted; MCs outside the Fire and Rally packages (a rout's Interdiction NMC, a Bail Out) call no Heat of Battle DR. | audit |
| A15.2 | Heroes | 83 | built | ScenarioA1FireCalculator.cs: Round (IsHeroType branch), TargetState.Morale | AHeroFiresWithTheHeroicDrmAndIsWoundedNotBroken; AHeroIsNeverPinnedOrBrokenAndACasualtyMcWoundsHimAsIfWounded |  | A hero is wounded on a failed MC with a Wound Severity dr, eliminated if already wounded, never breaks, takes no PTC, does not Cower; Morale capped at 10, 9 wounded. | audit |
| A15.21 | Creation | 83 | built | ScenarioA1HeatOfBattle.cs: Resolve, HeroOf; GamePlanner.Fire.cs (hero created, heroic leader); GamePlanner.Rally.cs | RallyAndFireStepsTests.U32AHeatOfBattleSixBothCreatesAHeroAndBattleHardensTheSquad |  | A MMC creates its nationality's hero sharing its fire and movement status (R5.11); a leader becomes heroic, rallies, keeps his leadership. | audit |
| A15.23 | Weapons Use | 83 | partly | ScenarioA1FireCalculator.cs (hero-mg, one MG or FT, inherent FP forfeited) | AHeroFiresAMgAtFullFpWithTheTwoManDrmNegated; AHeroFiresAMgWithItsTwoManDrmAndHisHeroicDrm; AHalfSquadAndAHeroFireFlamethrowers | Pass 49 | R15.11: a hero fires one MG at full FP with +1, or a FT, and forfeits his own FP. Refused (backlog): a LATW, a light mortar, a Gun. Missing: the Gun rules of 82mm, CA change, and the AAMG as a Rider. | audit |
| A15.24 | Heroic DRM | 83 | built | ScenarioA1FireCalculator.cs (heroic modifier, Normal Range, never for a FT); ScenarioA1CloseCombatCalculator.cs: Drm, AmbushDrm; ScenarioA1VehicleCloseCombat.cs | AHeroFiresWithTheHeroicDrmAndIsWoundedNotBroken; AHeroicLeaderIsStealthy |  | The -1 on IFT and CC DRs, cumulative with leadership, Stealthy in Ambush, no exemption from Cowering for the others. The DRM for Clearance attempts has no Clearance to apply to. | audit |
| A15.3 | Battle Hardening | 83 | built | ScenarioA1HeatOfBattle.cs: Resolve (hardening); ScenarioA1FireReference.cs: HardenedOf, IsHighestQuality | RallyAndFireStepsTests.U32HeatOfBattleInFireCreatesAHeroAndBattleHardens; ScenarioA1FireExtensionTests.ABattleHardenedUnitIsUnbrokenAndNoLongerDisrupted |  | Next higher quality from a reviewed table, Fanatic for an elite MMC or best leader, refusal by the owner (R5.8, R5.9). A unit with no entry in the table is refused with hob.hardening-unreviewed. | audit |
| A15.4 | Berserk | 83 | built | ScenarioA1HeatOfBattle.cs: Resolve; ScenarioA1FireCalculator.cs: TargetState (berserk); GamePlanner.Fire.cs; GamePlanner.Rally.cs | CloseCombatStepsTests.U34ABerserkResultMakesTheSquadChargeTheNearestKnownEnemyInItsNextMph |  | Berserk on 9 to 11, rallied if broken; non-elite Italian and Axis Minor MMC surrender on 10 or more (R15.13, R27.1). | audit |
| A15.41 | Leader Consequences | 83 | built | ScenarioA1FireCalculator.cs (berserk-leader NTC); ScenarioA1RallyCalculator.cs (companions); LiveFire.cs; LiveRally.cs | OrdnanceStepsTests.ABerserkLeaderTakesHisCompanionsWithHimInLiveFire; ARallyRecordReadsTheBerserkChecksOfALeadersCompanions |  | Each other friendly unit in the Location subject to Heat of Battle takes a NTC with the leader's modifier; a berserk leader then directs no fire. | audit |
| A15.42 | Morale | 84 | built | ScenarioA1FireCalculator.cs: Round (unit.Berserk branch), TargetState.Morale; GamePlanner.Overrun.cs: PaatcExempt; GamePlanner.Fire.cs (berserk leader gives no leadership) | ScenarioA1RefereeFixesTests.D1ABerserkMmcIsEliminatedAndABerserkLeaderWoundedAsIfWoundedOnAnOriginalTwelve; BacklogPass5Tests.AUnitCarryingMoreThanItsIpcMayNotWithdrawAndABerserkUnitLosesItsCx |  | Morale 10, Casualty Reduction on a failed MC, no break, pin, PAATC, or PTC; loses CX, TI, and concealment. The pins by PF Check, Minimum Move, and Wounds are not built for a berserk unit. | audit |
| A15.43 | Charge | 84 | built with a deviation | GamePlanner.CloseCombat.cs: ChargeSteps, MustCharge, ChargeBarred; GamePlanner.Movement.cs: BerserkStep; GamePlanner.cs (MPh end) | CloseCombatStepsTests.U34ABerserkResultMakesTheSquadChargeTheNearestKnownEnemyInItsNextMph; OrdnanceStepsTests.ABerserkChargeIntoACrewsLocationEndsInPlace | Pass 49 | Built: berserk units move first, charge the nearest Known enemy in LOS by hexes, equidistant targets the owner's choice, stacks together but for wounded. R27.2 and R30.5: a charge the model cannot decide (no route, a Gun crew's Location, a vehicle with Infantry) ends in place. The pillbox and Fortified Building exceptions have no terrain to apply to. | audit |
| A15.431 | (none) | 84 | partly | Experience.cs: MoveAllowance; GamePlanner.Movement.cs (SW abandoned, no Assault Movement, no Double Time); GamePlanner.CloseCombat.cs: ChargeFirstMoves, ChargeSteps | CloseCombatStepsTests.ABerserkUnitAbandonsItsMmgBeforeChargingAndChargesIntoPrisoners; BacklogPass27Tests.ABerserkChargeTakesTheShorterRouteInBypass; BacklogPass15Tests.ABerserkUnitKeepsTheOnePortagePointSwItsOwnerNames | Pass 49 | Built: eight MF, three wounded, SW abandonment, no Assault Movement or advance, the shortest route in MF with Bypass, a closer enemy takes the charge, revealed concealed units stop it, the end of berserk with no enemy in LOS. Missing: a unit made berserk in mid move charging with the rest of its MF was not found; Dash, Wire as 1 MF, and DC Thrown only are absent. | audit |
| A15.432 | (none) | 84 | partly | GamePlanner.VehicleCloseCombat.cs (berserk OVR); GamePlanner.CloseCombat.cs (OVR CC at once); GamePlanner.Fire.cs: FireBar; ScenarioA1CloseCombatCalculator.cs: AmbushDrm (Lax) | BacklogPass27Tests.ABerserkOverrunOfALoneSmcHasItsCcAtOnceInTheMph; ScenarioA1Pass14Tests.BerserkUnitsNeitherCaptureNorAreCaptured | Pass 49 | Built: must enter, the Infantry OVR of a lone SMC with its CC at once (R27.3), no PFPh fire, no prisoners, Lax. Not verified by me: the forced FPF of the DEFENDER and the halved AFPh TPBF in the charged Location; the comment in GamePlanner.Fire.cs calls berserk fire in the AFPh and DFPh not reviewed. | audit |
| A15.44 | No Enemy in LOS | 84 | built | ScenarioA1HeatOfBattle.cs: Resolve (knownEnemyInLos); GamePlanner.CloseCombat.cs: KnownEnemyInLos | CloseCombatStepsTests.ABerserkResultWithNoKnownEnemyInLosIsBattleHardening |  | A Berserk result with no Known enemy unit in LOS is Battle Hardening. | audit |
| A15.45 | Terrain Restrictions | 84 | partly | GamePlanner.CloseCombat.cs: ChargeSteps, ChargeFirstMoves; GamePlanner.Terrain.cs: InfantryStep (cliff refused) | none found | Pass 49 | A charge routes only over entries the movement model allows, so it never crosses a cliff or water. Missing: the fall back to the next nearest Known enemy unit (the charge ends in place instead, R30.5), the A15.44 fall back when there is none, any Blaze bar (no Blaze terrain), and minefields, FFE, and Wire. | audit |
| A15.46 | Return to Normal | 84 | partly | ScenarioA1CloseCombatCalculator.cs (BerserkEnded); GamePlanner.CloseCombat.cs; GamePlanner.cs (MPh end, play.berserk-ends) | ScenarioA1CloseCombatTests.ABerserkUnitMustAttackAndReturnsToNormalWhenItClearsTheLocation; ScenarioA1RefereeFixesTests.ABerserkGroupReturnsToNormalOnlyWhenItsOwnAttackClearedTheLocation | Pass 49 | Built: return to normal after CC clears the Location and at the end of a charge with no Known enemy in LOS. Missing: the return after AFPh TPBF or FT clears the Location, and after destroying a vehicle in CC (backlog, which cites A15.45 for it). | audit |
| A15.5 | Surrender | 84 | built | ScenarioA1HeatOfBattle.cs: Resolve (Surrender, noQuarter, fanatic, Commissar); GamePlanner.CloseCombat.cs: Captors, captor's choice | ScenarioA1Pass5Tests.ASideFacedWithNoQuarterTreatsASurrenderAsBerserk; ASurrenderWithNoCaptorLeavesTheUnitSubjectToASecondHeatOfBattleDr |  | Broken, Disrupted, and surrendering to an ADJACENT Known Good Order armed enemy unit, or only Disrupted; No Quarter, Fanatic, and Commissar go berserk. Japanese, Gurkha, and Partisan have no counters; SS against Russians is not read. | audit |

#### A16 Battlefield Integrity (pages 84 to 85)

7 rows: 7 not built.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A16 | Battlefield Integrity | 84 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | Optional rule (asterisk and the bracketed note on p. 84). Only the card's printed total is checked (ScenarioCards.cs, card.integrity, R17.4). | me |
| A16.1 | Integrity Base | 84 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | Only a card check exists (ScenarioCards.cs IntegrityBpv, test TheIntegrityTotalsAreTheCatalogBpvOfTheStartingMmc): the bracketed total against the BPV of the starting MMC. No Integrity Base, no Casualty Tally, no track. | audit |
| A16.11 | (none) | 84 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | Which losses count toward the tally; needs a Casualty Tally in the records. | audit |
| A16.12 | (none) | 84 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | Reinforcements raising the Integrity Base; needs A16.1. | audit |
| A16.2 | Integrity Check | 85 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | Integrity Check DR and its DRM (Unopposed Armor, Air Support, leaders); needs A16.1 and an ELR that changes in play. | audit |
| A16.21 | (none) | 85 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | ELR dropping to 0 and taking effect at once; needs A16.2. | audit |
| A16.3 | Regaining ELR | 85 | not built |  |  | Left out, if the user agrees (question 2): optional by its own text; the card's check of the printed total stays | Regaining ELR; needs A16.2. | audit |

#### A17 Wounds (page 85)

5 rows: 1 built, 3 partly, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A17 | Wounds | 85 | not applicable |  |  |  | heading | audit |
| A17.1 | Occurrence | 85 | partly | ScenarioA1FireCalculator.cs: Round.Reduce, Wound; ScenarioA1CloseCombatCalculator.cs; ScenarioA1VehicleCloseCombat.cs; ScenarioA1RallyCalculator.cs; GamePlanner.Snipers.cs: SnipeUnit | ACasualtyReducedLeaderIsWoundedAndHisPlusOneApplies; AHeroFiresWithTheHeroicDrmAndIsWoundedNotBroken | Pass 49 | A SMC is wounded by Casualty Reduction, by a Sniper dr of 2, and a hero by a failed MC, in Fire, CC, Rally, and Sniper attacks. Three other Casualty Reductions wound without the rule's procedure (see A17.11). | audit |
| A17.11 | Severity | 85 | partly | ScenarioA1FireCalculator.cs: Round.Wound; ScenarioA1CloseCombatCalculator.cs (woundSeverity); ScenarioA1RallyCalculator.cs; GamePlanner.Snipers.cs: SnipeUnit | AWoundNeedsItsSeverityRoll; AMortalWoundEliminatesTheLeaderAndCausesALlmc; ACasualtyReducedLeaderIsWoundedAndHisPlusOneApplies | Pass 45 | Built there: the dr, mortal on 5 or 6, +1 when already wounded. Skipped in three paths, where a SMC is wounded with no dr and an already wounded SMC is eliminated outright: GamePlanner.Rout.cs CasualtyReduction (Interdiction), GamePlanner.MoppingUp.cs (Search casualty, which calls the same helper), GamePlanner.Ordnance.cs FirerEffectEvents (PF firer). | me |
| A17.2 | Movement | 85 | partly | Experience.cs: MoveAllowance; GamePlanner.Rout.cs: RoutHalfMf; GamePlanner.Movement.cs (IPC 0, no Double Time) | ALeaderMovesWithSixMfAndAWoundedLeaderWithThree | Pass 49 | Built: three MF, also berserk and in the RtPh, IPC zero, no Double Time. Missing: the pin of a SMC wounded after spending more than 3 MF, carrying a wounded man for 5 PP, 4 MF on a conveyance. | audit |
| A17.3 | Effects | 85 | built | ScenarioA1FireCalculator.cs: TargetState.Morale, Leadership; ScenarioA1RallyCalculator.cs; ScenarioA1CloseCombatCalculator.cs: Leadership; GamePlanner.Rout.cs: BrokenMorale; GamePlanner.Overrun.cs: PaatcFacts | AWoundedLeaderRalliesWithAWorseModifier |  | Morale and leadership one worse, once only, not for a berserk unit's Morale; no LLMC for the wound. | audit |

#### A18 Field Promotions (page 85)

5 rows: 1 built, 2 partly, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A18 | Field Promotions | 85 | not applicable |  |  |  | heading | audit |
| A18.1 | (none) | 85 | not applicable |  |  |  | Introduces its two children; no rule text of its own. | audit |
| A18.11 | Self-Rally | 85 | built | ScenarioA1RallyCalculator.cs (FieldPromotionAttempt, leaderCreation); GamePlanner.Rally.cs | ScenarioA1RallyPackageTests.FieldPromotionCreatesALeaderFromTheTable; BacklogPass5Tests.TheSelfRallyingSideMayDeclineTheLeaderCreationDr |  | First MMC Rally attempt of the side's own RPh as Self-Rally with no Good Order leader and not Disrupted; an Original 2 rallies and offers the Leader Creation dr with +1 broken. The game also bars it while a broken leader is in the Location (a reading of A10.71 the code states). | audit |
| A18.12 | CCPh | 85 | partly | ScenarioA1CloseCombatCalculator.cs: Round (leaderCreation, re-figured attacks, created-leader DRM) | ScenarioA1CloseCombatTests.AFieldPromotionOfMixedMmcIsDecidedByBpvAndRandomSelection | Pass 49 | Built for CC between Infantry: the dr at once, the leader's modifier added to the Original 2, both attacks figured again, Infiltration excepted (R14.8, R14.12). Missing: an Original 2 by a MMC attacking a vehicle in CC creates no leader (ScenarioA1VehicleCloseCombat.cs has no Leader Creation). | audit |
| A18.2 | (none) | 85 | partly | ScenarioA1FieldPromotion.cs: Create, Applicable | ScenarioA1RallyPackageTests.FieldPromotionCreatesALeaderFromTheTable; ScenarioA1Pass27Tests.AxisMinorLeaderCreationHasNoNationalityDrmAndAnUnreviewedNationalityIsRefused | Pass 49 | Built: the table, the nationality, Morale Level, broken, and odds column drm, the highest BPV MMC as the base, Random Selection for the MMC he defends with, not for Finns or Japanese. Missing: the -1 for CC against an AFV (follows from A18.12); G.M.D. has no counters and an unlisted nationality is refused. | audit |

#### A19 Unit Substitution (page 86)

15 rows: 8 built, 1 built with a deviation, 3 partly, 2 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A19 | Unit Substitution | 86 | not applicable |  |  |  | heading | audit |
| A19.1 | Experience Level Rating (ELR) | 86 | built | ScenarioA1FireCalculator.cs: Round.ElrOf, Elr, Replace; GamePlanner.cs (play.group); ScenarioCards.cs (card.elr) | AFailureBeyondElrReplacesTheUnit; FailingByTwoReplacesOnlyInTheLowElrGroup |  | ELR by side and by OB group (R17.5, R18.3); a failure by more than the ELR Replaces the unit. The ELR track is the state's field. | audit |
| A19.11 | ELR Immunity | 86 | partly | ScenarioA1FireCalculator.cs: ElrImmune, Round.Replace | none found | Pass 49 | Crews, Commissars, heroes, and broken units are never Replaced. Missing: an Unarmed unit is not exempted (the Fire facts never read the Unarmed condition). | audit |
| A19.12 | Disruption | 86 | partly | ScenarioA1FireCalculator.cs: Round.Replace (Disrupt); ScenarioA1RallyCalculator.cs (disrupted-self-rally); GamePlanner.Rout.cs (play.rout-surrender); GamePlanner.CloseCombat.cs (surrender on an advance, R14.11); GamePlanner.cs (melee-eliminated) | ScenarioA1FirePackageTests.AUnitThatCannotBeReplacedIsDisrupted; CloseCombatStepsTests.ABrokenUnitHeldInMeleeMustWithdrawAndADisruptedOneIsEliminated; BacklogPass15TablePlayerTests.ACommissarAndADisruptedSquad | Pass 45 | Built: Disruption when no lesser unit exists, never for Fanatic or a Commissar, no Self-Rally, surrender in the RtPh when ADJACENT to captors and when enemy units advance in, elimination in Melee, rally removes it. Missing: a Disrupted unit does not stay put: MayRout and PlanRout let it rout and Low Crawl like any broken unit when no captor is ADJACENT; surrender in other phases (backlog R14.11); the SS against Russians and PRC exceptions. | me |
| A19.13 | Replacement | 86 | built | ScenarioA1FireCalculator.cs: Round.Replace, ReduceBeyondElr; ScenarioA1FireReference.cs: ReplacementOf, CasualtyHalfSquadOf | ScenarioA1Pass15Tests.AnUnderscoredHalfSquadIsDisruptedRatherThanReplaced; AnUnderscoredSquadFailingBeyondItsElrBecomesTwoBrokenHalfSquads; ACasualtyMcBeyondTheCircledESquadsElrLeavesABrokenHalfSquadOfLesserQuality |  | Replacement by a broken lesser unit of the same size, an underscored squad by its two broken HS (R15.9), an underscored HS Disrupted, a Casualty MC beyond the ELR to a lesser broken HS, leaders one grade down. | audit |
| A19.131 | Ammunition Shortages | 86 | not built |  |  | Pass 49 | No Ammunition Shortage state or SSR token: no Replacement on an Original 12 IFT DR, no lowered B#, X#, or circled B#, no bar on Fire Lanes. | audit |
| A19.132 | (none) | 86 | not built |  |  | Pass 49 | Backlog R15.9: the underscored exception always applies (ScenarioA1FireCalculator.cs ElrOf gives an underscored MMC 5); an SSR ELR of 4 or less for underscored units cannot be given. | audit |
| A19.2 | Green & Conscript Troops | 86 | built | Experience.cs: Inexperienced; LiveFire.cs (Inexperience fact); ScenarioA1CloseCombatCalculator.cs: Inexperienced | ExperienceTests.OnlyMmcAreInexperiencedAndADefinitionOutsideTheCatalogIsUnknown; ScenarioA1FireExtensionTests.AConscriptIsAlwaysInexperienced |  | Green and Conscript MMC are Inexperienced, never SMC or crews; only a hero or heroic leader is ever Stealthy in the Ambush drm, so no Green or Conscript unit is. | audit |
| A19.3 | Inexperienced Personnel Restrictions | 86 | built | Experience.cs: Inexperienced | BacklogPass15Tests.AGreenSquadIsInexperiencedAloneAndNotWithALeader |  | A Green MMC with an unbroken leader of its side in its Location is exempt; a Conscript never; Unarmed MMC are Inexperienced in CC (ScenarioA1CloseCombatCalculator.cs). | audit |
| A19.31 | Movement | 86 | partly | Experience.cs: MfAllowance, MoveAllowance | ExperienceTests (class); ALeaderMovingWithASquadAddsTwoMf | Pass 49 | Three MF, eight when berserk, the leader bonus on top. Not verified: the exception for a Player Turn of being carried or of mounting or dismounting a conveyance (GamePlanner.Passengers.cs not read for it); Human Wave is not built. | audit |
| A19.32 | SW | 86 | built with a deviation | ScenarioA1FireCalculator.cs (ATR, FT); ScenarioA1OrdnanceCalculator.cs (B# or X# one lower); ScenarioA1ArmorCalculator.cs | ScenarioA1Pass9bTests.InexperiencedHandsLowerTheBreakdownAndXNumberByOne | Pass 49 | R9.10: the ATR, mortars, LATW, and FT take the -1; a MG keeps its printed B# in Inexperienced hands (backlog). | audit |
| A19.33 | Cowering | 86 | built | ScenarioA1FireCalculator.cs (shift = cowered ? inexperienced ? 2 : 1) | none found by name |  | A cowering group with Inexperienced Personnel shifts two columns. | audit |
| A19.34 | PAATC | 86 | built | GamePlanner.Overrun.cs: PaatcFacts (inexperienced-1paatc); ScenarioA1VehicleCloseCombat.cs (CCV one less) | none found by name |  | A 1TC for the PAATC and a CCV one lower in attack and defence. | audit |
| A19.35 | Capture | 86 | built | ScenarioA1CloseCombatCalculator.cs (capture-vs-inexperienced) | none found by name |  | -1 in place of +1 on a capture attempt against an Inexperienced unit (R14.4). | audit |
| A19.36 | Lax | 86 | built | ScenarioA1CloseCombatCalculator.cs: AmbushDrm (lax) | none found by name |  | Inexperienced Personnel are Lax for the Ambush drm. | audit |

#### A20 Prisoners (pages 86 to 88)

18 rows: 6 built, 1 built with a deviation, 8 partly, 1 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A20 | Prisoners | 86 | not applicable |  |  |  | Heading. | audit |
| A20.1 | Value | 86 | built | ScenarioVictory.cs: Cvp; ScenarioVictory.cs: ExitVp | BacklogPass21Tests; BacklogPass25Tests |  | A unit still captured counts its VP during play and double once the game has ended. | audit |
| A20.2 | Capture | 86 | built | GameProjector.cs: Capture; ScenarioA1CloseCombatCalculator.cs: capture-outside check | ScenarioA1Pass14Tests |  | Berserk units neither capture nor are captured; the three ways are the rows below. | audit |
| A20.21 | Rout Phase | 86 | built with a deviation | GamePlanner.Rout.cs: PlanRout, TrappedByInterdiction, FailureToRout; GamePlanner.CloseCombat.cs: Captors, PlanTakePrisoner | BacklogPass13Tests | Pass 49 | Ruling R13.3: the surrender is taken as the RtPh ends and unit by unit, not accepted or rejected as a stack. Only Fanatic units and a side under No Quarter are exempt: no exemption for Commissars was found (Partisans, Gurkhas, SS, Japanese have no counters). | audit |
| A20.22 | CCPh | 87 | built | ScenarioA1CloseCombatCalculator.cs: Resolve (capture at and below the Kill Number), drm capture and capture-vs-inexperienced; GamePlanner.CloseCombat.cs | ScenarioA1Pass14Tests; BacklogPass14Tests |  | Ruling R14.4. Infantry against Infantry only; the defender's choice is declared with the round. | audit |
| A20.221 | (none) | 87 | partly | ScenarioA1CloseCombatCalculator.cs: Resolve (voided captures) | ScenarioA1Pass14Tests | Pass 49 | Built: a side wholly eliminated or captured in a simultaneous round captures no one. Missing: prisoners left alone exchanged for Green or Conscript units, staying in Melee, and all SW abandoned; they are freed Unarmed instead. | audit |
| A20.23 | Mopping Up | 87 | not applicable |  |  |  | Pure cross-reference to A12.153 (Mopping Up is built in GamePlanner.MoppingUp.cs). | audit |
| A20.24 | (none) | 87 | built | GamePlanner.CloseCombat.cs: capture events, PlanTakePrisoner (equipment-transferred to the Location) | BacklogPass14Tests |  | A captured or surrendering unit leaves its SW in the Location; the captor Recovers them normally. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A20.3 | No Quarter | 87 | built | GamePlanner.CloseCombat.cs: PlanTakePrisoner (reject); GameProjector.cs: surrender-rejected; GamePlanner.Rout.cs: NoQuarter checks | BacklogPass5Tests |  | Ruling R5.6. The Battlefield Integrity exclusion has nothing to act on (A16 not built). | audit |
| A20.4 | Massacre | 87 | partly | GamePlanner.Choices.cs: PlanMassacre, BerserkMassacres; GameProjector.cs: prisoners-massacred | BacklogPass5Tests | Pass 49 | Ruling R5.7. Built for Russian and berserk units against prisoners, with the ELR rise and No Quarter. Missing: SS, Japanese, Partisan counters; Massacre of an Unarmed unit that is not a prisoner; a berserk unit ignoring prisoners when it picks its charge target was not found. | audit |
| A20.5 | Guards & Unarmed Units | 87 | partly | GameProjector.cs: Capture, KeepGuards; GamePlanner.Prisoners.cs: PlanGuardPrisoners; ScenarioA1CloseCombatCalculator.cs: Guards | BacklogPass14Tests; PlayTests | Pass 49 | Rulings R14.5, R14.7. Built: Unarmed exchange as a condition, FP 1 in CC only, the Guard, transfer and abandonment in RPh or APh, a new Guard when one is lost. Missing: a Guard squad Deploying at will, a Guard forcing prisoners to entrench or clear. | audit |
| A20.51 | Stacking | 87 | partly | GamePlanner.CloseCombat.cs: GuardLoad, UnitSize; GamePlanner.Prisoners.cs: capacity check; ScenarioA1CloseCombatCalculator.cs: stacking count | ScenarioA1Pass14Tests | Pass 49 | Built: five times the Guard's US#, prisoners outside overstacking. Missing: exchanging two prisoner HS for an Unarmed squad, prisoners sharing entrenchment, prisoners as Passengers or Riders. | audit |
| A20.52 | Guard FP | 87 | built | GamePlanner.Fire.cs: FireBar; ScenarioA1CloseCombatCalculator.cs: Odds (guarding 0.5) | ScenarioA1Pass14Tests; CloseCombatStepsTests |  | Ruling R12.9. An outnumbered Guard fires at nothing and uses no SW; its CC FP is halved against non-prisoners. Kindling does not exist. | audit |
| A20.53 | Movement | 87 | partly | GameProjector.cs: prisoners move with the Guard; GamePlanner.Victory.cs: exit with prisoners; ScenarioVictory.cs: EscortEdge | BacklogPass25Tests | Pass 49 | Rulings R14.5, R17.7, R25.5. Built: prisoners move and exit with their Guard, the Friendly Board Edge exemption. Missing: the Massacre penalty when the abandoning side later eliminates an abandoned prisoner; one Friendly Board Edge per side only. | audit |
| A20.54 | Attack Effects | 87 | partly | ScenarioA1FireCalculator.cs: prisoner handling in MC, pin, LLMC; GamePlanner.Fire.cs: PlanFire; GamePlanner.Consequences.cs | ScenarioA1Pass12Tests; BacklogPass31Tests | Pass 46 | Ruling R12.9. Built: small arms fire from outside the Location at Guard and prisoners. Refused: ordnance fire at such a Location and fire from within it. Missing: recapture of Unarmed units by entry and CC in the MPh; double VP for a prisoner its own side's fire eliminates (the message promises it, the CVP count does not give it). | audit |
| A20.55 | Escape | 88 | partly | ScenarioA1CloseCombatCalculator.cs: PrisonersRound, escape NTC, EscapeSucceeds; GamePlanner.CloseCombat.cs | ScenarioA1Pass14Tests; BacklogPass14Tests | Pass 49 | Ruling R14.6. Built: the NTC, the escape round before other CC, success when no enemy is left or by Infiltration. Deviation: every prisoner attack must take in the Guard. Missing: a guarded prisoner's Withdrawal from Melee; the bar on Italian and Axis Minor prisoners escaping. | audit |
| A20.551 | Rearming | 88 | partly | ScenarioA1CloseCombatCalculator.cs: Rearm, ConscriptOf; GamePlanner.FreedSmc.cs: WithArmedSmc | ScenarioA1Pass14Tests; BacklogPass31Tests | Pass 49 | Rulings R14.6, R31.8. Built: an attacking Unarmed MMC rearmed as a Conscript MMC per enemy unit it eliminated or captured in CC; a SMC freed in any way is Armed (R31.8 goes beyond "escaped"). Missing: rearming by other means when no enemy is in the Location, and when an enemy there surrenders. | audit |
| A20.552 | Scrounging | 88 | not built |  |  | Pass 49 | No Scrounging DR anywhere. Needs the end-of-MPh check that all MF were spent and the one DR per hex per MPh. | audit |

#### A21 Captured Equipment (page 88)

8 rows: 1 built with a deviation, 4 partly, 2 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A21 | Captured Equipment | 88 | not applicable |  |  |  | Heading. | audit |
| A21.1 | Possession | 88 | built with a deviation | LiveFire.cs: CapturedBy; GamePlanner.SupportWeapons.cs: Recovery | ScenarioA1Pass13Tests | Pass 54 | Rulings R13.7, R15.8: any SW of another nationality than its possessor's counts as captured, an ally's too. Used with penalties: MG, ATR, FT, DC. A captured light mortar or LATW is refused (the firer must be of the weapon's nationality). | audit |
| A21.11 | Malfunction | 88 | partly | ScenarioA1FireCalculator.cs: breakdown (Captured 2 lower), AssaultWeaponRemoval; GamePlanner.FireExtensions.cs: lane breakdown | ScenarioA1Pass13Tests; ScenarioA1Pass15Tests | Pass 54 | Built for captured MG, ATR, FT, DC. Missing for Guns, mortars, LATW, vehicles, which cannot be used captured. Ammunition Shortage is not built, so its exclusion has nothing to act on. | audit |
| A21.12 | Performance | 88 | partly | ScenarioA1FireCalculator.cs: Multiple ROF one lower when Captured | ScenarioA1Pass13Tests | Pass 54 | Built: ROF one lower for a captured MG. Missing: captured ordnance (red To Hit Numbers and Case H +2), which is refused. | audit |
| A21.13 | Non-Qualified Use | 88 | partly | ScenarioA1OrdnanceCalculator.cs: crew-outside check, PassEightFirerDrm (case-h) | BacklogPass8Tests | Pass 54 | Ruling R8.8. Built: a squad or HS of the Gun's own nationality mans it with Case H +2 only; the B# and ROF penalties and red To Hit Numbers of A21.11 and A21.12 were not found for it. Refused: any unit of another nationality, two SMC as a MMC, a lone hero. | audit |
| A21.2 | Vehicle | 88 | partly | GamePlanner.VehicleCloseCombat.cs: CapturedVehicles; GamePlanner.cs: phase change to ccph | BacklogPass11Tests | Pass 51 | Ruling R11.16. Built: an unarmed vehicle not in Motion, alone with enemy Infantry, is captured as the CCPh begins (the rule says at its end). Missing: capture of an Abandoned AFV, automatic or by a CC capture attempt, with its -1 DRM and Personnel Escort; a captured vehicle is never used. | audit |
| A21.21 | Temporary Driver | 88 | not built |  |  | Pass 51 | A captured unarmed vehicle is marked captured and Abandoned and neither moves nor fires. Needs the Temporary Driver. | audit |
| A21.22 | Temporary Crew | 88 | not built |  |  | Pass 51 | No Temporary Crew: no removal of the entering MMC, no start dr, no halved MP. Needs A21.2 for an AFV first. | audit |

#### A22 Flamethrowers & Molotov Cocktails (pages 89 to 90)

18 rows: 10 built, 2 built with a deviation, 1 partly, 1 refused, 3 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A22 | Flamethrowers & Molotov Cocktails | 89 | not applicable |  |  |  | Heading. | audit |
| A22.1 | FP Modification | 89 | built | ScenarioA1FireCalculator.cs: FT multipliers; FireRange.cs: Band | ScenarioA1Pass15Tests; FireRangeTests |  | Ruling R15.1. 24 FP, 12 at two hexes, no PBF or TPBF, not halved in the AFPh, Area Fire halved. A vehicular FT is not built (D1.8). | audit |
| A22.2 | DRM | 89 | built | ScenarioA1FireCalculator.cs: drm (no TEM, no leadership for a FT) | ScenarioA1Pass15Tests |  | Hindrance and CX apply. The pillbox exception has no pillbox to act on. | audit |
| A22.3 | Usage | 89 | built | ScenarioA1FireCalculator.cs: AssaultWeaponRemoval; LiveFire.cs: one FT or DC a Player Turn; GameState.cs: AssaultWeaponUsers | ScenarioA1Pass15Tests; BacklogPass15Tests |  | Non-elite two lower, captured two lower again; a squad's inherent FP in a separate attack. | audit |
| A22.31 | FG | 89 | built | ScenarioA1FireCalculator.cs: flamethrower-outside | ScenarioA1Pass15Tests |  | A FT fires alone. The OVR exception needs a vehicular FT. | audit |
| A22.32 | Line of Fire (LOF) | 89 | built | ScenarioA1FireCalculator.cs: flamethrower-outside, two-levels multiplier | ScenarioA1Pass15Tests |  | Long Range refused through any Hindrance (a reading of "obstructed"); two levels away halves; more is refused. | audit |
| A22.33 | Restrictions | 89 | built | ScenarioA1FireCalculator.cs: flamethrower-outside (Pinned) | ScenarioA1Pass15Tests |  | A pinned unit fires no FT. No paratroops exist. | audit |
| A22.34 | vs AFV | 89 | not built |  |  | Pass 50 | No way to name an AFV as a FT's main target and no C7.34 Flame To Kill resolution. Needs the C7.34 table and Specific Collateral attacks. | audit |
| A22.35 | vs Terrain | 89 | not built |  |  | Pass 44 | No Flame is ever placed. Needs B25.12 and terrain Fire. | audit |
| A22.4 | Vulnerability | 89 | built | ScenarioA1FireCalculator.cs: flamethrower-possessed; LiveFire.cs: FT possessed | BacklogPass15Tests |  | One lower per FT on the attack's DR against its possessor, ordnance hits too. | audit |
| A22.5 | Malfunction | 89 | built | ScenarioA1FireCalculator.cs: breakdown for IsFt; GamePlanner.Fire.cs: FT removal | ScenarioA1Pass15Tests |  | Removed after the attack on an Original DR of 10 or more (lower by A22.3). | audit |
| A22.6 | Molotov Cocktails (MOL) | 89 | built | GamePlanner.Fire.cs: PlanFire (SSR mol:side) | BacklogPass15Tests |  | MOL only where an SSR gives it. DYO purchase is not built (Chapter H deferred). | audit |
| A22.61 | Availability | 89 | built | GamePlanner.Fire.cs: PlanFire (MOL user Good Order or berserk, unpinned) | BacklogPass15Tests |  | Inherent, no counter, never in CC. | audit |
| A22.611 | vs Unarmored Targets | 89 | built with a deviation | ScenarioA1FireCalculator.cs: mol-outside, MOL Check, four FP after modification; GamePlanner.Fire.cs: hexside bar, First Fire then Final Fire bar | ScenarioA1Pass15Tests; BacklogPass15Tests | Pass 49 | Ruling R15.4. A leader makes no MOL attack (MMC, crew, or hero only); the woods and orchard hexside bar has no road exception; refused when a vehicle is in the target Location. | audit |
| A22.6111 | (none) | 89 | partly | ScenarioA1FireCalculator.cs: MOL user broken on a colored 6; GamePlanner.Fire.cs | ScenarioA1Pass15Tests | Pass 44 | Built: the user breaks and its FP and the MOL's are void. Missing: the Flame on a colored 1 or 6 and its EC and Fortified Building dr. | audit |
| A22.612 | vs Armored Targets | 89 | refused | ScenarioA1FireCalculator.cs: mol-outside (a vehicle in the target Location) | ScenarioA1Pass15Tests | Pass 50 | A MOL attack at a Location holding any vehicle is refused. Needs the C7.34 MOL column. | audit |
| A22.613 | vs Terrain | 90 | not built |  |  | Pass 44 | No Kindling Attempt exists. Needs B25.11. | audit |
| A22.62 | Leadership | 90 | built with a deviation | ScenarioA1FireCalculator.cs: MOL Check (no leadership), group leadership | ScenarioA1Pass15Tests | Pass 49 | Ruling R15.4: leadership applies as for any group; the more-than-four-FP condition is not checked. No MOL To Kill DR exists. | audit |

#### A23 Demolition Charges (pages 90 to 91)

14 rows: 5 built, 3 partly, 1 refused, 4 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A23 | Demolition Charges | 90 | not applicable |  |  |  | Heading. | audit |
| A23.1 | (none) | 90 | built | ScenarioA1FireCalculator.cs: DC firepower, drm (no Hindrance, no hexside TEM for a Placed DC, no leadership) | ScenarioA1Pass15Tests |  | Rulings R15.2, R15.3. Halved only when every target was concealed at Placement. | audit |
| A23.2 | Usage | 90 | built | ScenarioA1FireCalculator.cs: AssaultWeaponRemoval | ScenarioA1Pass15Tests |  | Non-elite two lower, captured two lower again. | audit |
| A23.3 | Placement | 90 | partly | GamePlanner.DemolitionCharges.cs: PlanPlaceDc; GameProjector.cs: DC Placement | PlayTests; ScenarioA1Pass15Tests | Pass 49 | Ruling R15.2. Built: Placement in an ADJACENT same-level Location for its entry MF, operable Placement. Refused: a Location holding any vehicle (the PAATC), a Location whose TEM or MF is not reviewed, another level. | audit |
| A23.4 | Detonation | 90 | built | GamePlanner.DemolitionCharges.cs: PlanDetonateDc; GamePlanner.cs: AFPh hold; ScenarioA1FireCalculator.cs: dcMalfunctioned | ScenarioA1Pass15Tests; PlayTests |  | Detonates in the AFPh, removed with no effect at 12 (10 captured), CX +1; a Thrown DC's second DR never malfunctions it. | audit |
| A23.41 | vs Terrain | 91 | not built |  |  | Pass 42 | No Flame, Rubble, or Breach from a DC. Needs B25.13, B24.11, B23.9221. | audit |
| A23.5 | vs AFV | 91 | refused | GamePlanner.DemolitionCharges.cs: PlanPlaceDc (dc-vehicle); ScenarioA1FireCalculator.cs: DemolitionChargeOutside | ScenarioA1Pass15Tests | Pass 50 | A DC is not Placed or Thrown where a vehicle is. Needs the C7.34 DC column and the C7.346 Position DR. | audit |
| A23.6 | Thrown DC | 91 | partly | GamePlanner.DemolitionCharges.cs: PlanThrowDc; ScenarioA1FireCalculator.cs: thrown-dc, thrower-location | ScenarioA1Pass15Tests | Pass 49 | Ruling R15.3. Built: thrown at the thrower's level with +2 there and +3 at home, two DR. Refused: throwing down a level, a stairwell, or a cliff. No vehicle or Cavalry thrower. | audit |
| A23.61 | (none) | 91 | partly | GamePlanner.DemolitionCharges.cs: PlanPlaceDc, PlanThrowDc (target is not the own Location) | ScenarioA1Pass15Tests | Pass 49 | The bar on the own Location is built. Its exceptions (Japanese; a vehicle in Bypass in the Location) are not. | audit |
| A23.62 | AFPh | 91 | built | ScenarioA1FireCalculator.cs: thrown-dc-afph | ScenarioA1Pass15Tests |  | +1 in the AFPh on both DR unless an Opportunity Firer. The Position DRM against an AFV waits for A23.5. | audit |
| A23.63 | Final Fire | 91 | built | GamePlanner.DemolitionCharges.cs: PlanThrowDc (dc-phase) | ScenarioA1Pass15Tests |  | A unit marked First Fire throws no DC, nor in Subsequent First Fire or FPF. | audit |
| A23.7 | Set Demolition | 91 | not built |  |  | Pass 49 | No Set DC: no Setting dr, no later detonation with its NTC. Needs Hazardous Movement and an SSR token for a DC Set before play. | audit |
| A23.71 | (none) | 91 | not built |  |  | Pass 49 | No 36+ column attack with -3 and no TEM, no bridge or building level destruction. Needs B24.11 and B6. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A23.72 | Set DC Clearance | 91 | not built |  |  | Pass 49 | No Set DC to clear. Needs B24.75 and Searching. | audit |

#### A24 Smoke (pages 91 to 93)

14 rows: 4 built, 3 partly, 6 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A24 | Smoke | 91 | not applicable |  |  |  | Heading. | audit |
| A24.1 | Infantry Usage | 91 | partly | GamePlanner.Smoke.cs: PlanSmoke; GamePlanner.Movement.cs; GameProjector.cs: movement-step Smoke | BacklogPass9Tests | Pass 56 | Ruling R9.5. Built: own Location 1 MF, ADJACENT same-level Location 2 MF, the dr against the exponent, once a MPh, a 6 ends the squad's MPh, no water or marsh. Refused: any other level, a stack in Bypass, a Residual FP Location. Missing: the Mild Breeze upwind bar (no wind in the game). | audit |
| A24.11 | (none) | 91 | built | GameProjector.cs: RemoveSmokeGrenades; GamePlanner.Smoke.cs: weather refusal | BacklogPass9Tests; BacklogPass16Tests |  | The counter leaves as the MPh ends; not placed in rain, Mud, or Deep Snow outside a building. Heavy Winds do not exist. | audit |
| A24.2 | Effect | 91 | built | GamePlanner.Wrecks.cs: VehicleHindrance, SmokeSources; GamePlanner.Fire.cs: Residual FP smoke; ScenarioA1FireCalculator.cs: FFMO | BacklogPass9Tests; BacklogPass6Tests |  | Ruling R9.6. +2 per source, at most +3 a Location, cumulative along the LOS, no FFMO, none on a DC. Only +2 sources exist. The DRM is applied at any LOS height (see A24.4). | audit |
| A24.3 | White Phosphorus (WP) | 92 | not built |  |  | Pass 56 | No WP grenades and no WP exponent, though the catalog holds American and British squads. Needs a WP counter and the declaration before the dr. | audit |
| A24.31 | Casualties | 92 | not built |  |  | Pass 56 | No WP NMC. Needs A24.3. | audit |
| A24.32 | Fires | 92 | not built |  |  | Pass 44 | No WP Fire. Needs A24.3, EC, and terrain Fire (B25). | audit |
| A24.4 | Height & Duration | 92 | not built |  |  | Pass 56 | SMOKE has no height: the Hindrance applies to any LOS crossing the hex. No white or Dispersed counters, so no duration. Needs levels in the smoke Hindrance and ordnance SMOKE. | audit |
| A24.5 | Strength | 92 | partly | GamePlanner.Wrecks.cs: VehicleHindrance (+2 per source) | BacklogPass9Tests | Pass 56 | Built: the grenade's +2. Missing: full-strength +3 and +2 counters, the Dispersed side, WP grenades' +1 (no ordnance SMOKE, no WP). | audit |
| A24.6 | Weather | 92 | partly | GamePlanner.Smoke.cs: PlanSmoke (smoke-weather) | BacklogPass16Tests | Pass 56 | Built: no placement in rain, Mud, or Deep Snow except into a building hex. Missing: fog, Heavy Wind, removal of standing SMOKE; the building exception does not check that the placer is in the same building. | audit |
| A24.61 | Drift | 92 | not built |  |  | Pass 56 | No Drift. Needs wind force and direction and lasting SMOKE counters. | audit |
| A24.62 | Gusts | 93 | not built |  |  | Pass 56 | A Gust is rolled and nothing reads it. Needs lasting SMOKE counters. | audit |
| A24.7 | Movement | 93 | built | GamePlanner.Wrecks.cs: BlazeEntryHalfMf, WreckEntryHalfMp; GamePlanner.Terrain.cs: InfantryEntry; GamePlanner.VehicleTerrain.cs | BacklogPass9Tests; BacklogPass6Tests |  | One more MF or MP to enter a SMOKE Location. | audit |
| A24.8 | Outgoing LOS Hindrances | 93 | built | GamePlanner.Wrecks.cs: VehicleHindrance (+1 out of or within); GamePlanner.Overrun.cs: OverrunAttack | BacklogPass9Tests |  | Ruling R9.6. Not added to Residual FP. | audit |

#### A25 Nationality Distinctions (pages 93 to 98)

88 rows: 10 built, 9 partly, 60 not built, 9 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A25 | Nationality Distinctions | 93 | not applicable |  |  |  | Heading; its only text points at the A./G. National Capabilities Chart (p. 695). | audit |
| A25.01 | Assault Engineers | 93 | not built |  |  | Pass 64 | The catalog records the trait asl:assault-engineer (false on every counter) and no code reads it; no Assault Engineer counter exists. Designation is DYO (H1.22, deferred). Missing: AE counters and the loss of AE capability on Replacement. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A25.1 | German | 93 | built | ScenarioA1FireReference.cs: Replacements, Hardened | ScenarioA1FireExtensionTests |  | The printed progressions 4-6-8 to 4-6-7 to 4-4-7 to 4-3-6 and 5-4-8 to 4-4-7 to 4-3-6 are the code's tables (R19.7 for the 5-4-8). The rest of the text is counter-choice guidance. | audit |
| A25.11 | SS | 93 | not built |  |  | Pass 64 | No SS counter (6-5-8, 3-4-8, SS 4-6-8) in the catalog and no SS logic: no RtPh-surrender or Disruption exemption against Russians, no SS Massacre (the planner admits only Russian or berserk units), no 1944-45 Assault Fire grant, no Depletion Number +1. | audit |
| A25.111 | Late War Ss | 93 | not built |  |  | Pass 64 | SSR-invoked Late War SS substitution chart; needs the SS counters and a second Replacement and Battle Hardening table. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A25.12 | Combat Engineers | 93 | partly | ScenarioA1FireCalculator.cs: Elr, ReduceBeyondElr (UnderscoredMorale from the catalog trait asl:elr-5); ScenarioA1FireReference.cs: CasualtyHalfSquads | ScenarioA1Pass15Tests.AnUnderscoredHalfSquadIsDisruptedRatherThanReplaced | Pass 64 | The German 8-3-8 and 3-3-8 have ELR 5 through their underscored morale. Missing: SS 8-3-8 counters and SS rules; Assault Engineer designation is DYO (H1.22). | audit |
| A25.13 | Volksgrenadier | 93 | not built |  |  | Pass 64 | No Volksgrenadier 5-3-7 or its 2-3-7 HS in the catalog; a comment in ScenarioA1FireReference cites the rule only. Missing: counters, Fanatic on Battle Hardening. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A25.2 | Russian | 93 | partly | GamePlanner.SupportWeapons.cs: PlanDeploy, MayNotDeploy; ScenarioSetup.cs: MayDeploy; ScenarioA1FireReference.cs: Hardened | BacklogPass31Tests; BacklogPass13Tests; BacklogPass19Tests | Pass 45 | Russian no-Deploy is built (R31.4), in play and at setup; the 2-2-7 HS hardens to 3-2-8. Wrong: a 4-2-6 hardens to the NKVD 6-2-8, not the 5-2-7 the rule names (see findings, Faults). Missing: Guards Depletion Number +1; the A20.5 and A21.22 Deploy exceptions. | me |
| A25.21 | Entrenching | 93 | not built |  |  | Pass 37 | No entrenching attempt exists (B27), so no -1 DRM. | audit |
| A25.211 | Russian SMG Squads | 93 | not built |  |  | Pass 64 | No date-dependent Battle Hardening of the 4-2-6 to the 4-4-7 before 1941. | audit |
| A25.212 | Russian Early War Doctrine | 93 | not built |  |  | Pass 64 | SSR-invoked; none of it exists: the -1 CC DRM against 4-2-6, AFV moving before Infantry, the Armored Assault ban, OBA never Accurate, the aircraft Sighting DRM. | audit |
| A25.22 | Commissar | 94 | partly | ScenarioA1FireReference.cs: Commissars, IsCommissar | BacklogPass17Tests (R17.9) | Pass 63 | The 9-0 and 10-0 Commissar counters exist and carry their capabilities (A25.221 on). Missing: the owner's free substitution of an 8-0 or 8-1 at scenario start, its 10/42 date limit, and the cap of no more Commissars than other leaders; a card simply lists Commissars. | audit |
| A25.221 | (none) | 94 | partly | ScenarioA1FireCalculator.cs: CheckOrder, CommissarBonus, ActiveCommissar, Check, LeaderLoss; ScenarioA1RallyCalculator.cs: Outside, Run; ScenarioA1HeatOfBattle.cs: Resolve | ScenarioA1Pass15Tests.ACommissarRaisesHisLocationsMoraleAndTakesNoLeaderLossCheck | Pass 63 | Built for IFT MC order, the +1 Morale Level, sole leadership DRM, LLMC and LLTC, forced Self-Rally, and no Unit Substitution (R15.6). Missing: the +1 and the sole-leader rule in a PAATC (GamePlanner.Overrun.cs: PaatcFacts reads neither). Cavalry is not in the catalog. | audit |
| A25.222 | Rally | 94 | built | ScenarioA1RallyCalculator.cs: Run (byCommissar, replacedByCommissar); GamePlanner.cs: phase advance check play.commissar-rally; GamePlanner.Rally.cs | BacklogPass15Tests; BacklogPass15TablePlayerTests (cases 19, 20) |  | Mandatory attempt, DM immunity, Replacement on failure, Casualty Reduction or elimination at the lowest quality (R15.6). | audit |
| A25.223 | Berserk | 94 | built | ScenarioA1FireCalculator.cs: berserk companions (berserk-with-commissar) | BacklogPass15TablePlayerTests (case 21) |  | Verified in the Fire path; the Rally path's berserk companions were not traced for a Commissar. | audit |
| A25.224 | 8+1 Commissar | 94 | built | ScenarioA1FieldPromotion.cs: Commissar; ScenarioA1FireReference.cs: Commissars | ScenarioA1Pass15Tests.AnNkvdFieldPromotionUsesTheCommissarTable; BacklogPass15Tests |  | The 8+1 Commissar counter exists and is created only by NKVD Field Promotion or listed on a card. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A25.23 | Human Wave (HW) | 94 | not built |  |  | Pass 64 | No Human Wave. Also the base of Cavalry Wave (A13.62) and of Chapter G Banzai (G1.5) and Chinese Human Wave (G18.5), both deferred. | audit |
| A25.231 | Direction | 94 | not built |  |  | Pass 64 | Human Wave Direction and Hex Grain; none. | audit |
| A25.2311 | Forward and Side Locations | 94 | not built |  |  | Pass 64 | Forward and Side Locations; none. | audit |
| A25.232 | Movement | 94 | not built |  |  | Pass 64 | 8 MF Impulse Movement; needs Impulse Movement (D14.3), also absent. | audit |
| A25.2321 | Range | 94 | not built |  |  | Pass 64 | Range counter; none. | audit |
| A25.2322 | (none) | 94 | not built |  |  | Pass 64 | No re-entry of left or adjacent Locations; none. | audit |
| A25.233 | Enemy Units | 94 | not built |  |  | Pass 64 | Forced entry of enemy Locations and automatic Infantry OVR; none. | audit |
| A25.234 | Ending the Human Wave | 95 | not built |  |  | Pass 64 | Ending the Human Wave; none. | audit |
| A25.24 | Partisans | 95 | not built |  |  | Pass 64 | No Partisan 3-3-7 counter or nationality; none of Stealthy, ELR 5, no RtPh surrender, no Disruption, leader separation. | audit |
| A25.241 | Movement | 95 | not built |  |  | Pass 64 | SSR movement advantages; none. | audit |
| A25.242 | (none) | 96 | not built |  |  | Pass 64 | Red To Hit Numbers for Partisan ordnance; no Partisans. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A25.25 | NKVD MMC | 96 | built | ScenarioA1HeatOfBattle.cs: Resolve (nkvd -1); ScenarioA1FireReference.cs: Nkvd, HighestQuality, IsNkvd; ScenarioA1FieldPromotion.cs: Commissar; ScenarioA1FireCalculator.cs: Elr | ScenarioA1Pass15Tests.AnNkvdFieldPromotionUsesTheCommissarTable; ScenarioA1RallyPackageTests; ScenarioA1FireExtensionTests; BacklogPass15Tests |  | 6-2-8 and 3-2-8 counters, ELR 5 by underscored morale, -1 Heat of Battle DRM, Fanatic on Battle Hardening, the Commissar Creation table (R15.7). | audit |
| A25.3 | American | 96 | not applicable |  |  |  | Describes printed broken Morale Levels (counter values, in the catalog) and gives guidance; cross-references G17.1 and G17.2 (Chapter G, deferred). Inventory: The PDF outline points at page 95; the rule is printed on page 96. | audit |
| A25.31 | Paratroops | 96 | not built |  |  | Pass 64 | No American 7-4-7 counter. The underscored-morale mechanism would give ELR 5 once the counter is added. | audit |
| A25.32 | Ordnance | 96 | not built |  |  | Pass 64 | No U.S. ordnance or AFV in the catalog; ScenarioA1OrdnancePackage's To Hit color table holds only German (black) and Russian (red), with no date switch. | audit |
| A25.33 | Ammunition | 96 | not built |  |  | Pass 64 | OBA (C1) is absent. | audit |
| A25.34 | Smoke | 96 | not built |  |  | Pass 64 | WP grenades (A24.3) are absent; squads place Smoke only. | audit |
| A25.35 | US-Built British-Color SW | 96 | not built |  |  | Pass 64 | No (a) SW counters, no Scrounging; LiveFire.CapturedBy treats any other-nationality weapon as captured, so the U.S. and British exemptions are absent. | audit |
| A25.4 | British | 96 | not applicable |  |  |  | Definition of who "British" covers and class guidance; no mechanics. | audit |
| A25.41 | Green | 96 | not applicable |  |  |  | Definition: the British 4-3-6 is Green (the catalog's class). Green behavior is A19 logic. | audit |
| A25.42 | Airborne | 96 | not built |  |  | Pass 64 | No British 6-4-8 Airborne counter. | audit |
| A25.43 | Gurkha | 96 | not built |  |  | Pass 64 | No Gurkha identity. Hand-to-Hand exists only by SSR (GamePlanner.CloseCombat.cs cites A25.43 for its timing); the Gurkha option, the -1 DRM, Commando status, and the surrender and Disruption exemptions are absent. | audit |
| A25.44 | ANZAC | 96 | not built |  |  | Pass 64 | No ANZAC identity; Stealthy is given only to heroes and heroic leaders. A scenario card names A25.44 as adaptation text only. | audit |
| A25.45 | Cowering | 96 | built | ScenarioA1FireCalculator.cs: NeverCowers | ScenarioA1Pass15Tests.BritishEliteAndFirstLineUnitsAndFinnsButConscriptsNeverCower |  | The Free French exception has nothing to apply to (no Free French counters). | audit |
| A25.46 | WP | 96 | not built |  |  | Pass 64 | WP grenades are absent, so the 1944 date rule has nothing to govern. | audit |
| A25.5 | French | 96 | not applicable |  |  |  | Scope statement for the French rules; no mechanics of its own. | audit |
| A25.51 | Green | 96 | not applicable |  |  |  | Definition: the French 4-3-7 is Green (the catalog's class); the French Replacement chain ends at it (ScenarioA1FireReference). | audit |
| A25.52 | Ordnance | 96 | not built |  |  | Pass 64 | No French ordnance or vehicles; no To Hit color for French. | audit |
| A25.53 | Free French | 96 | not built |  |  | Pass 64 | No Free French counters; no Assault Fire or WP date rules; OBA absent. Cites DYO purchase (A25.57). | audit |
| A25.54 | Pre-12/43 Equipment | 96 | not built |  |  | Pass 64 | Free French use of British equipment without Captured penalties; none. | audit |
| A25.55 | 12/43-5/45 Equipment | 96 | not built |  |  | Pass 64 | Free French use of U.S. equipment; none. | audit |
| A25.56 | French-Built Equipment | 97 | not built |  |  | Pass 64 | (f) equipment and its Captured rules; none. | audit |
| A25.57 | DYO | 97 | not built |  |  | Deferred with Chapter H: a DYO chart, purchase, or dr; a card's SSR names the value | DYO-only (Chapter H charts, H1.463); deferred with Chapter H. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.58 | Vichy French | 97 | not built |  |  | Pass 64 | No Vichy French counters. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A25.6 | Italian | 97 | not built |  |  | Pass 64 | No Italian ordnance in the catalog and no Italian entry in the To Hit color table. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.61 | Elite | 97 | partly | ScenarioA1FireReference.cs: Replacements | ScenarioA1Pass15Tests.TheNewNationalitiesReplaceAndBattleHardenByTheirOwnClasses | Pass 63 | The 4-4-7 is Replaced by the 3-4-6. Missing: only elite Italian squads may Deploy (ScenarioSetup.MayDeploy bars Russians only, so a 3-4-7 Deploys: a fault); no Italian 2-2-7 crew counter. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.62 | 1st Line | 97 | built | ScenarioA1FireReference.cs: Replacements, Hardened | ScenarioA1Pass15Tests.TheNewNationalitiesReplaceAndBattleHardenByTheirOwnClasses |  | 3-4-7 to Conscript; Conscript hardens to 3-4-6 (R15.13). Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.63 | Surrender | 97 | not built |  |  | Pass 63 | ScenarioA1CloseCombatCalculator adds the +1 capture DRM against a non-elite Italian 1st Line defender, and an Italian prisoner may attempt escape. Both are silent omissions. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.64 | Lax | 97 | not built |  |  | Pass 63 | Lax comes only from Inexperience or berserk status (ScenarioA1CloseCombatCalculator.AmbushDrm, GamePlanner.MoppingUp); no nationality rule. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.65 | PAATC | 97 | not built |  |  | Pass 63 | GamePlanner.Overrun.cs PaatcFacts gives a 1PAATC only to Inexperienced units, so Italian Conscripts get it by A19 and Italian 1st Line do not. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.66 | Eritrean | 97 | not built |  |  | Pass 64 | No Eritrean counters or rules. Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.7 | Finnish | 97 | partly | ScenarioA1RallyCalculator.cs: SelfRally (catalog trait asl:self-rally); ScenarioA1FireCalculator.cs: NeverCowers | BacklogPass15TablePlayerTests; ScenarioA1Pass15Tests.BritishEliteAndFirstLineUnitsAndFinnsButConscriptsNeverCower | Pass 63 | Self-Rally and Cowering immunity are built, Conscripts excepted. Missing: Stealthy for Elite and 1st Line; ski capability (E4.2). Inventory: The PDF outline points at page 96; the rule is printed on page 97. | audit |
| A25.71 | Leaders | 97 | partly | ScenarioA1FieldPromotion.cs: Applicable; ScenarioA1RallyCalculator.cs: Run; ScenarioA1CloseCombatCalculator.cs; ScenarioA1FireReference.cs: OtherLeaders | BacklogPass15TablePlayerTests; ScenarioA1Pass15Tests | Pass 63 | The rank structure and no Field Promotion are built (R15.13); the milder LLMC follows from the leadership values. Missing: Deploying without a leader on a 1TC, and Recombining without a leader (PlanDeploy and PlanRecombine require a leader for Finns). | audit |
| A25.72 | Battle Hardening | 97 | built | ScenarioA1FireReference.cs: Replacements, Hardened, HighestQuality, OtherLeaders | ScenarioA1Pass15Tests.TheNewNationalitiesReplaceAndBattleHardenByTheirOwnClasses |  | Checked against the figure on p. 97: 6-4-8 to 5-3-8; 5-4-8 to 4-4-7 to 4-3-7; reversed for hardening; 1st Line becomes Fanatic. The 8+1 leader's Disruption and the rearmed Conscript rest on the general A19.12 and A20.551 logic, not traced for a Finn. | audit |
| A25.73 | Sissi | 97 | not applicable |  |  |  | Definition of the Sissi 8-3-8 and 3-3-8 counters (in the catalog). | audit |
| A25.74 | 1st Line | 97 | built | ScenarioA1FireCalculator.cs: AssaultWeaponRemoval | ScenarioA1Pass15Tests.AFinnishFirstLineUserIsEliteForItsFlamethrowerAndAnInexperiencedUserLosesItOneSooner |  | Finnish 1st Line use FT and DC without the non-elite penalty. | audit |
| A25.75 | Captured Equipment | 97 | not built |  |  | Pass 63 | LiveFire.CapturedBy makes every other-nationality weapon captured; there is no Finnish MG or PSK counter, so a Finn firing a Russian MG takes the penalty the rule waives. | audit |
| A25.76 | Panzerfaust (PF) | 97 | not built |  |  | Pass 64 | The Panzerfaust is German only (ScenarioA1OrdnancePackage.Panzerfaust; the firer must be of its nationality), so a Finnish PF is refused as crew-outside. Missing: 7/44 availability, range 1, check 2 or less, the 1.5 allowance on Elite and 1st Line. | audit |
| A25.77 | Panzerschreck (PSK) | 97 | not built |  |  | Pass 64 | No Finnish PSK date rule or class penalty; the one PSK counter is German. | audit |
| A25.78 | OBA | 97 | not built |  |  | Pass 64 | OBA (C1) is absent. | audit |
| A25.79 | Ordnance | 97 | not built |  |  | Pass 64 | No Finnish ordnance; no Finnish entry in the To Hit color table. | audit |
| A25.8 | Axis Minors | 97 | partly | GameProjector.cs: game start (Nation check, NoQuarter); GameTypes.cs: SideState.Nation, HungariansVersusRomanians; ScenarioCards.cs: card.nation | BacklogPass27Tests | Pass 63 | The side names its nation and Hungarians against Romanians start under No Quarter (R27.1). Missing: the +1 broken Morale Level in that case, and the own-borders SSR. The broken Morale Levels themselves are counter values. | audit |
| A25.81 | PAATC | 97 | not built |  |  | Pass 63 | As A25.65: only Inexperienced units take a 1PAATC, so Axis Minor 1st Line do not. The Romanian 1st Line exception from 7/43 is also absent. | audit |
| A25.82 | Escape | 98 | partly | ScenarioA1HeatOfBattle.cs: Resolve (surrenderFrom, noQuarter) | ScenarioA1Pass27Tests.AxisMinorHeatOfBattleTakesPlusThreeAndNonEliteMmcSurrenderOnTen | Pass 63 | Surrender on a Final 10 or more and Berserk when Hungarians fight Romanians are built. Missing: Axis Minor prisoners may still attempt escape. | audit |
| A25.83 | Ordnance | 98 | not built |  |  | Pass 64 | No Axis Minor ordnance; no To Hit color or HEAT date. | audit |
| A25.84 | SMG Squads | 98 | built | ScenarioA1FireReference.cs: Replacements, Hardened, HighestQuality | ScenarioA1Pass27Tests.AxisMinorReplacementAndBattleHardeningFollowA2584 |  | 5-3-7 to Conscript, Fanatic on hardening, Conscript to 3-4-7 (R27.1). The 1/43 and 10/44 availability dates are left to the card. | audit |
| A25.85 | Panzerfaust (PF) | 98 | not built |  |  | Pass 64 | The Panzerfaust is German only; Romanian and Hungarian PF use, dates, drm, and allowances are absent. | audit |
| A25.86 | Hungarian Troops | 98 | not applicable |  |  |  | Says Hungarians have their own counters and follow the Axis Minor rules; the game uses one shared Axis Minor counter set with a named nation (R27.1). | audit |
| A25.87 | Romanian ATMM | 98 | not built |  |  | Pass 64 | ATMM (C13.7) is absent. | audit |
| A25.9 | Allied Minors | 98 | not built |  |  | Pass 64 | No Allied Minor nationality or counters (Greek, Yugoslav, Polish, Belgian and others); Heat of Battle and Leader Creation refuse an unreviewed nationality. | audit |
| A25.91 | PAATC | 98 | not built |  |  | Pass 64 | No Allied Minors; no nationality 1PAATC. | audit |
| A25.92 | (none) | 98 | not applicable |  |  |  | Guidance: use British or Russian counters and rules for such forces. | audit |
| A25.93 | Ethiopian | 98 | not built |  |  | Pass 64 | No Ethiopian counters or class structure. | audit |
| A25.931 | Elite | 98 | not built |  |  | Pass 64 | Ethiopian elite, multi-Location FG and Deploy limits; none. | audit |
| A25.932 | 1st Line | 98 | not built |  |  | Pass 64 | Ethiopian 1st Line progression; none. | audit |
| A25.933 | Human Wave | 98 | not built |  |  | Pass 64 | Needs Human Wave (A25.23), itself absent. | audit |
| A25.934 | CC | 98 | not built |  |  | Deferred with Chapter G: the rule serves the Pacific only | Rests on Japanese CC (G1.64), Chapter G, deferred. | audit |
| A25.935 | Tank Flip | 98 | not built |  |  | Pass 64 | Tank Flip; none. | audit |
| A25.9351 | Resolution | 98 | not built |  |  | Pass 64 | Tank Flip resolution; none. | audit |
| A25.936 | MMG/HMG/ATR | 98 | not built |  |  | Pass 64 | Ethiopian MG and light mortar B# and ROF penalty; none. | audit |
| A25.937 | Captured Use | 98 | not built |  |  | Pass 64 | Doubled Captured Use penalties; none. | audit |
| A25.938 | No Quarter & Leader Creation | 98 | not built |  |  | Pass 64 | Ethiopian No Quarter and Leader Creation drm; none. | audit |

#### A26 Victory Conditions (pages 98 to 100)

23 rows: 13 built, 1 built with a deviation, 2 partly, 5 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| A26 | Victory Conditions | 98 | not applicable |  |  |  | Heading. | audit |
| A26.1 | Control Victory Conditions | 98 | built | ScenarioVictory.cs: Control (fold over the game's states) | BacklogPass21Tests; BacklogPass24Tests |  | Rulings R21.1, R24.1. Control is kept until the enemy gains it. Tracked only for buildings the Victory Conditions name, their Locations, and their hexes. | audit |
| A26.11 | Gaining Control | 99 | built | ScenarioVictory.cs: StartSide, Gainer, Armed; GamePlanner.MoppingUp.cs | BacklogPass21Tests; BacklogPass24Tests |  | Rulings R24.1, R24.2, R24.4. Start by setup area or board, gain by an armed Good Order Infantry MMC during any state of a move, never in Bypass, Mopping Up. A vehicle's PRC do not count (R24.5). | audit |
| A26.12 | Vehicular Control | 99 | built with a deviation | ScenarioVictory.cs: Control (vehicle hold read from the present state) | BacklogPass24Tests | Pass 51 | Ruling R24.5: a captured vehicle neither holds nor prevents Control, and PRC are not counted. Otherwise as the rule, reverting when the vehicle leaves. | audit |
| A26.13 | Hex Control | 99 | partly | ScenarioVictory.cs: Gainer (ground level for a hex) | BacklogPass21Tests | Pass 49 | Built for the hexes of a building a hex count names. Missing: Control of any other hex (a card cannot name one); the Bridge exception. | audit |
| A26.131 | Bridge Hex | 99 | not built |  |  | Pass 59 | No bridge hex Control. Needs B6 bridge and Depression Locations in play. | audit |
| A26.132 | Pillbox Hex | 99 | not built |  |  | Pass 37 | No pillbox hex Control. Needs B30 pillboxes. | audit |
| A26.14 | Building Control | 99 | built | ScenarioVictory.cs: Gainer (any level for a building); GamePlanner.Victory.cs: HexLevels | BacklogPass21Tests; BacklogPass24Tests |  | One MMC at any level with no armed enemy ground unit in the building. A Rowhouse is one building as the card lists its hexes. Rooftops and cellars are left out of the Locations only. | audit |
| A26.15 | Concealment | 99 | built | ScenarioVictory.cs: Undeclared, Armed (Dummy), Gainer | BacklogPass23Tests; BacklogPass21Tests |  | Ruling R23.4. Dummies neither gain nor prevent; a side's reading leaves out the enemy's concealed and hidden units until the game ends. | audit |
| A26.16 | Control Forfeiture | 99 | not built |  |  | Pass 44 | No Kindling, so no forfeiture. Needs B25.11 and terrain Fire with who started it. | audit |
| A26.161 | (none) | 99 | not built |  |  | Pass 44 | No terrain Blaze, so no Control of unenterable areas. Needs B25. | audit |
| A26.162 | (none) | 100 | not built |  |  | Pass 44 | Needs B25 and the record of who started each Fire. | audit |
| A26.2 | Victory Points | 100 | not applicable |  |  |  | Definition of VP; the rules are the rows below. Inventory: The PDF outline points at page 99; the rule is printed on page 100. | audit |
| A26.21 | VIctory Point Value | 100 | built | GamePlanner.Victory.cs: VictoryPoints, VehicleVictoryPoints (read from the unit as it now is) | BacklogPass24Tests |  | A unit's VP follow its present counter and state: a created or Replaced leader, a malfunctioned or disabled MA. | audit |
| A26.211 | Infantry & PRC | 100 | built | GamePlanner.Victory.cs: VictoryPoints | BacklogPass21Tests |  | Squad or crew 2, HS 1, leader 1 plus its negative modifier, hero 0. No armor leaders, LC crews, or Japanese leaders exist. | audit |
| A26.212 | Vehicles & Equipment | 100 | built | GamePlanner.Victory.cs: VehicleVictoryPoints; ScenarioVictory.cs: GunVp | BacklogPass24Tests |  | Ruling R24.3. No aircraft, wagons, or dismantled Guns exist. | audit |
| A26.213 | (none) | 100 | built | GamePlanner.Victory.cs: HasInherentCrew, VehicleVictoryPoints | BacklogPass24Tests |  | An inherent crew counts its two VP while aboard; a vehicle's MA Gun is the one MA point. No Carriers exist. | audit |
| A26.22 | Casualty Victory Points | 100 | built | ScenarioVictory.cs: Cvp, Holds (cvp) | BacklogPass21Tests; BacklogPass24Tests |  | The tally is computed from the state; the side record and VP track are replaced by the program. | audit |
| A26.221 | (none) | 100 | built | ScenarioVictory.cs: Cvp (Eliminated, Wrecked, Exited cases), EscortEdge | BacklogPass25Tests; BacklogPass26Tests |  | Exits count as eliminated except under Recall, an escorting Guard, or an exit that meets the side's exit condition. No paratroops or gliders. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| A26.222 | Capture | 100 | partly | ScenarioVictory.cs: Cvp (Captured, GunHolders) | BacklogPass24Tests; BacklogPass25Tests | Pass 49 | Built: prisoners and Guns last held by the enemy count normal VP in play and double at the end; lost when no longer captured. Missing: double CVP at once for a prisoner its own side's attack eliminates; vehicle capture is only the unarmed case of A21.2. | audit |
| A26.23 | Exit Victory Conditions | 100 | built | ScenarioVictory.cs: ExitVp, Qualifies; GamePlanner.Victory.cs: exits | BacklogPass21Tests; BacklogPass25Tests; BacklogPass26Tests |  | None for broken Personnel or Recalled vehicles; captured units exited count normal in play and double at the end. | audit |
| A26.3 | Avoidance | 100 | built | ScenarioVictory.cs: Evaluate (Otherwise) | BacklogPass21Tests |  | The side with no listed conditions wins when none of the other's holds. | audit |
| A26.4 | Balance | 100 | built | GamePlanner.cs: Balance at the start (dr when both wish the same side); ScenarioSetup.cs: Balance counters | BacklogPass20Tests |  | Rulings R20.3, R20.4. Balance provisions the game has no rule for (Sewer Movement, foxholes) are shown and applied by hand. | audit |

### Chapter B: Terrain

#### B. Introduction (pages 112 to 113)

10 rows: 1 built, 6 partly, 1 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B.1 | Symbiology | 112 | partly | VaslCompatibleHexFactDerivation.cs: SetInherentTerrain (center terrain of a hex); GamePlanner.Map.cs: TerrainKey | HexFactDerivationTests.InherentTerrainNearestTheCenterBecomesTheCenterTerrain | Pass 60 | One dominant terrain a hex is read from the VASL center Location. A hex of two terrains with neither dominant (building-woods, wooded hill) has no cumulative effect in movement or fire. | audit |
| B.2 | COT | 112 | partly | GamePlanner.Terrain.cs: GroundStep | BacklogPass10Tests.AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap | Pass 45 | COT doubled one level up and the wall's 1 + COT are built. SMOKE's MF is added after the doubling (3 MF uphill into SMOKE, where B.2 gives 2 x 2 = 4): a fault, no ruling found. | audit |
| B.3 | MP Costs | 112 | not applicable |  |  |  | Cross-reference to the Terrain Chart's MP column; the chart rows are judged with each terrain. | audit |
| B.4 | Hindrance Level | 112 | partly | LosCalculator.cs: AddHindranceHex and the height rules; GamePlanner.Wrecks.cs: VehicleHindrance | LosTests.HalfLevelHindrancesAreCountedOncePerRange; BacklogPass6Tests.AnAfvBetweenFirerAndTargetIsAHindrance | Pass 60 | Map Hindrances count only at their height, and AFV or wreck Hindrance only at the same level. SMOKE in a crossed hex is counted whatever the LOS height (no SMOKE height). | audit |
| B.5 | Continuous Slope | 112 | not built |  |  | Pass 60 | No Continuous Slope logic anywhere. The planner's same-level tests (marsh, AFV and wreck Hindrance) use equal heights only. The "Continuous Slopes are not reviewed" refusals test the VASL Slope hexside flag (F2.3), a different thing. | audit |
| B.6 | Inherent Terrain | 112 | partly | LosCalculator.cs: the center.IsInherent rule and InherentSpillTest; GamePlanner.Wrecks.cs: VehicleHindrance; GamePlanner.Fire.cs: FireMapFacts | LosTests.TheBreakdownKeepsEachRangesHindranceAndTheFirstPoint; BacklogPass6Tests.AnAfvInAGrainHexAddsItsHindranceToTheGrains | Pass 60 | Inherent printed terrain and whole-hex SMOKE, AFV, and wreck are read; one Hindrance a range. Fire through any inherent Hindrance but brush, grain, or marsh (orchard, crag, graveyard) is undecided; Bridge and rubble counters do not exist. | audit |
| B.7 | LOS & Terrain Checks | 112 | not applicable |  |  |  | Table suggestion (a duplicate mapboard); the program shows the map. | audit |
| B.8 | Random Direction | 112 | built | GamePlanner.Starshells.cs: the placement roll in the Starshell plan | BacklogPass16TablePlayerTests (the starshell placement cases) |  | dr 1 is the hexside at the grid coordinate, then clockwise. Only Starshells use it; no other Random Direction user was found. | audit |
| B.9 | Artificial Terrain | 112 | partly | GamePlanner.Wrecks.cs: CoverAt, VehicleHindrance; ScenarioA1FireCalculator.cs: Cover | ScenarioA1Pass6Tests.AWreckOrFriendlyAfvGivesInfantryOneTemAndCancelsFfmo | Pass 60 | AFV, wreck, and SMOKE act as Artificial Terrain in fire. A roadblock along the LOS is not built, and entrenchment, pillbox, and shellhole counters have no TEM. | audit |
| B.10 | LOS Hindrance Blockage | 113 | partly | LosCalculator.cs: AddHindranceHex ("Hindrance total of six or more (B.10)"); GamePlanner.Night.cs: NightAndWeatherFacts | none found for the six-total block | Pass 45 | Map Hindrance of 6 blocks the LOS; Low Visibility plus other Hindrance of 6 refuses the attack. SMOKE or vehicle Hindrance plus terrain reaching 6 with no Low Visibility is never tested and the attack is made at +6: a fault. | audit |

#### B1 Open Ground (page 113)

11 rows: 4 built, 3 partly, 3 refused, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B1 | Open Ground | 113 | not applicable |  |  |  | Heading. | audit |
| B1.1 | (none) | 113 | built | GamePlanner.Fire.cs: FireTerrain, FireMapFacts; GamePlanner.Movement.cs: EntryHalfMf | ScenarioA1FireExtensionTests.DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp |  | Open Ground is 1 MF, TEM 0, and takes FFMO. | audit |
| B1.11 | Roads | 113 | built | GamePlanner.Fire.cs: FireTerrain (Paved Road, Dirt Road as open-ground); GamePlanner.Terrain.cs: GroundStep | BacklogPass10Tests.TheRoadBonusAddsOneMfToAMoveAlongTheRoadOnly |  | A road hex is Open Ground but for the road hexside's rate. | audit |
| B1.12 | Runway | 113 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost; GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | A runway hex is not an admitted terrain name: entry "is not a reviewed entry", fire "has no TEM". Missing: the runway rules of B7. | audit |
| B1.13 | Shellholes | 113 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | Shellhole hexes are refused in movement and as a target; the 1 MF or 2 MF choice is missing. | audit |
| B1.14 | Hills | 113 | partly | GamePlanner.Terrain.cs: HeightAdvantageAt; ScenarioA1FireCalculator.cs | BacklogPass10Tests (the Height Advantage cases) | Pass 45 | In fire, Height Advantage cancels FFMO with the B10.31 Crest exception. Interdiction (GamePlanner.Rout.cs: Interdictor, OpenGround) ignores Height Advantage, so a hill hex seen from below is Interdicted: a fault. | me |
| B1.15 | Bridges | 113 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 59 | A bridge Location is refused as a target, so the road-depiction test is not made. | audit |
| B1.16 | Hexsides | 113 | partly | GamePlanner.Terrain.cs: HexsideTemAt; ScenarioA1FireCalculator.cs | ScenarioA1Pass10Tests.AWallReplacesALowerInHexTemAndLeavesNoFfmo | Pass 45 | In fire a wall or hedge TEM cancels FFMO. Interdiction across a wall or hedge hexside is not prevented (GamePlanner.Rout.cs: Interdictor reads no hexside TEM): a fault. Crest and cliff hexsides are refused. | me |
| B1.17 | Artificial Terrain | 113 | partly | GamePlanner.Wrecks.cs: CoverAt, VehicleHindrance; GamePlanner.Rout.cs: OpenGround | ScenarioA1Pass6Tests.AWreckOrFriendlyAfvGivesInfantryOneTemAndCancelsFfmo | Pass 45 | Wreck, AFV, and SMOKE cancel FFMO in fire. For Interdiction only SMOKE in the hex and the map's Hindrance count; a wreck or AFV in the hex, and SMOKE or a vehicle along the LOS, do not. Roadblocks are not built. | audit |
| B1.2 | (none) | 113 | built | LosCalculator.cs (Open category is no obstacle or Hindrance) | LosFidelityTests.EveryAnsweredPairAgreesWithVasl |  | LOS over Open Ground is clear but for hills, hexsides, and counters. | audit |
| B1.3 | (none) | 113 | built | ScenarioA1FireCalculator.cs: the ffmo modifier; ScenarioA1FireReference.cs: Tem | ScenarioA1FireExtensionTests.DefensiveFirstFireAppliesFfnamAndFfmoAndLeavesResidualFp |  | TEM 0 and FFMO -1 against moving Infantry. | audit |

#### B2 Shellholes (page 113)

5 rows: 1 built, 2 refused, 1 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B2 | Shellholes | 113 | not applicable |  |  |  | Heading. | audit |
| B2.1 | (none) | 113 | not built |  |  | Pass 58 | No shellhole counter and no creation by an HE FFE of 150mm or more (needs OBA and aerial bombs). Printed shellholes are read by the map only. | audit |
| B2.2 | (none) | 113 | built | LosCalculator.cs (read from VASL terrain data) | none found |  | A LOS through a shellhole hex takes nothing from it. No fixture with shellholes was found, so this rests on the VASL data alone. | audit |
| B2.3 | (none) | 113 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | A target in shellholes "has no TEM in the Fire package". Missing: the conditional +1, not cumulative, not for a Gun's pushers. | audit |
| B2.4 | (none) | 113 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost; GamePlanner.CardSetup.cs | none found | Pass 60 | Entry and setup refused. Missing: the 1 or 2 MF choice and its FFMO state, vehicle COT, gully shellholes. | audit |

#### B3 Roads (pages 113 to 114)

9 rows: 3 built, 1 built with a deviation, 3 partly, 1 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B3 | Roads | 113 | not applicable |  |  |  | Heading. | audit |
| B3.1 | (none) | 113 | built | GamePlanner.Night.cs: InfantryWeatherHalfMf; GamePlanner.VehicleTerrain.cs: VehicleCost (paved by the crossed hexside's name) | BacklogPass16TablePlayerTests.TruckOnAnUnpavedRoadInMud |  | Paved or dirt is read from the hexside crossed, as the rule has it. The "paved road hex" status of a mixed hex is used by nothing (B3.5). | audit |
| B3.2 | (none) | 113 | built | LosCalculator.cs; GamePlanner.Fire.cs: FireTerrain | LosFidelityTests.EveryAnsweredPairAgreesWithVasl |  | A road is no obstacle or Hindrance; a bare road hex is Open Ground. | audit |
| B3.3 | (none) | 113 | built with a deviation | GamePlanner.Terrain.cs: GroundStep; GamePlanner.Fire.cs: FireMapFacts | none found | Pass 46 | Ruling R10.1: a road hexside is always entered at the road rate, and the mover keeps the hex's TEM with no FFMO, where the rule gives FFMO and no TEM. Missing: the mover's choice and the road-rate FFMO. | audit |
| B3.4 | (none) | 114 | partly | GamePlanner.Terrain.cs: GroundStep; GamePlanner.Movement.cs (the bonus in the move plan); GameProjector.cs: OffRoad | BacklogPass10Tests.TheRoadBonusAddsOneMfToAMoveAlongTheRoadOnly | Pass 46 | 1 MF (2 uphill) and the Road Bonus are built, lost to SMOKE, a burning wreck, rubble, mud, snow, a pushed Gun. Mines, Wire, roadblocks, debris, and Panji do not cancel it (counters unread); no Cavalry or Horse-Drawn units. | audit |
| B3.41 | (none) | 114 | partly | GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass16TablePlayerTests.TruckOnARoadInClearWeather; BacklogPass11Tests.AnAbruptElevationChangeIsCrossedByRoadAtItsB1051Cost | Pass 46 | The half MP rate, 1 MP when BU or in snow, and 2 MP a level by road are built. Graveyard roads (refused terrain) and Convoy are not. | audit |
| B3.42 | (none) | 114 | built | GamePlanner.Wrecks.cs: WreckEntryHalfMp | BacklogPass6Tests (the D2.14 case, off the road); none found for the road doubling |  | Two MP a vehicle or wreck when entered by a road hexside at the road rate (ruling R6.4). | audit |
| B3.43 | Road-Negating Terrain | 114 | partly | GamePlanner.Terrain.cs: GroundStep; GamePlanner.VehicleTerrain.cs: VehicleCost (a road into rubble is no road) | none found | Pass 60 | Rubble negates the road rate and bonus. The shellhole, woods, and orchard choices cannot arise (R10.1, shellholes refused); debris, Cleared rubble, Trail Breaks, Dash, and Street Fighting are not built. | audit |
| B3.5 | (none) | 114 | not built |  |  | Pass 37 | No placement rule: hidden mines and entrenchments are not built, and nothing bars them from a paved road hex. | audit |

#### B4 Sunken Road (page 114)

9 rows: 1 partly, 4 refused, 2 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B4 | Sunken Road | 114 | not applicable |  |  |  | Heading. | audit |
| B4.1 | (none) | 114 | not applicable |  |  |  | Definition; the map reads the terrain as "Sunken Road" (a Depression in VASL's categories). | audit |
| B4.2 | (none) | 114 | partly | LosCalculator.cs: the Depression rules (IsDepressionHex, exitsSourceDepression, entersTargetDepression) | LosTests.LosMustLeaveAGullyOnlyWhenTheRangeRestrictionIsMet (a gully, not a Sunken Road) | Pass 41 | A Sunken Road is read as a generic Depression one level down; no Sunken Road rule of its own and no LOS fixture on a board with one (13 and 14 are not in the LOS oracle set). No unit can be there, so the planner never asks. | audit |
| B4.3 | (none) | 114 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 41 | A Sunken Road target "has no TEM". Missing: Open Ground TEM and Interdiction there. | audit |
| B4.4 | (none) | 114 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 41 | Entry refused even by the road hexside, since the terrain name is not admitted. | audit |
| B4.41 | (none) | 114 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 41 | Missing: 2 MF by a non-road hexside, and the exit at elevation cost. | audit |
| B4.42 | (none) | 114 | refused | GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 41 | Missing: road-hexside-only entry and exit, and the doubled wreck and VCA penalties. | audit |
| B4.43 | Sunken Lane | 114 | not built |  |  | Pass 41 | Sunken Lane by SSR; needs the One-Lane rules of B6.431. | audit |
| B4.5 | (none) | 114 | not built |  |  | Pass 37 | No entrenchment placement rule. | audit |

#### B5 Elevated Road (pages 114 to 115)

10 rows: 3 partly, 4 refused, 1 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B5 | Elevated Road | 114 | not applicable |  |  |  | Heading. | audit |
| B5.1 | (none) | 114 | not applicable |  |  |  | Definition; the map reads "Elevated Road" (a Road in VASL's categories) on a level 1 elevation. | audit |
| B5.2 | (none) | 115 | partly | LosCalculator.cs: the elevation and ground-level rules | none found | Pass 60 | The Elevated Road is read as level 1 ground from the elevation grid, so it blocks as a hill does. No fixture on board 13 in the LOS oracle set; no unit can be there. | audit |
| B5.21 | (none) | 115 | partly | LosCalculator.cs: the elevation rules | none found | Pass 60 | Crest effects follow from the painted level; not verified against VASL on an Elevated Road. Hull Down is not built at all, so its ban costs nothing. | audit |
| B5.22 | (none) | 115 | partly | LosCalculator.cs: the elevation rules | none found | Pass 60 | As B5.21: generic hill reading, unverified on this terrain. | audit |
| B5.3 | (none) | 115 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | An Elevated Road target "has no TEM". Missing: Open Ground with Height Advantage and the B10.31 exception. | audit |
| B5.4 | (none) | 115 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Entry refused even along the road. | audit |
| B5.41 | (none) | 115 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 60 | Missing: 2 MF by a non-road hexside, Abrupt Elevation Change at two levels. | audit |
| B5.42 | (none) | 115 | refused | GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Missing: tracked entry at the Crest cost, the doubled penalties; no motorcycles. | audit |
| B5.5 | (none) | 115 | not built |  |  | Pass 37 | No entrenchment placement rule. | audit |

#### B6 Bridges (pages 115 to 116)

18 rows: 2 partly, 5 refused, 10 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B6 | Bridges | 115 | not applicable |  |  |  | Heading. | audit |
| B6.1 | (none) | 115 | partly | VaslCompatibleHexFactDerivation.cs: FixBridgesTunnelWater (the bridge Location); GameTypes.cs: MapPosition.OnBridge; GameProjector.cs (UNIT-STATE-010) | HexFactDerivationTests.BridgeOverADepressionAddsABridgeLocation | Pass 59 | The map gives a printed bridge its own Location and the state can hold a unit "on the bridge", but the planner refuses every move and attack there. No bridge counters of any kind. | audit |
| B6.2 | (none) | 115 | partly | LosCalculator.cs: CheckSameHexRule, CheckBridgeHindranceRule | LosTests.ABridgeHindersLosAcrossItOffTheRoad; LosLeftoverTests.TheRailroadBridgeFixtureExercisesTheBridgeException | Pass 59 | On and beneath are blocked; the Hindrance off the road is counted only when both ends are at the bridge's level (not when one is below). A bridge in the Depression rule is refused, and fire through a bridge Hindrance is undecided (unattributed). | audit |
| B6.3 | (none) | 116 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 59 | A bridge target "has no TEM". Missing: Open Ground through the road depiction, Residual FP, pontoon. | audit |
| B6.31 | (none) | 116 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 59 | Missing: the +1 TEM off the road depiction. | audit |
| B6.32 | (none) | 116 | refused | GamePlanner.Fire.cs: FireMapFacts; GamePlanner.Ordnance.cs | none found | Pass 59 | Missing: the +1 Indirect Fire TEM on and beneath; mortar fire at the hex is refused with the rest. | audit |
| B6.33 | (none) | 116 | not built |  |  | Pass 59 | No attack on a bridge, no bridge TEM (+3, +2, +1), no destruction. | audit |
| B6.331 | (none) | 116 | not built |  |  | Pass 59 | No destroyed bridge and no rubble placed (rubble is never created). | audit |
| B6.332 | (none) | 116 | not built |  |  | Pass 59 | No pontoon bridge. | audit |
| B6.4 | (none) | 116 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 59 | The bridge hex's ground terrain (stream, gully, water) and its bridge Location are both refused. Missing: entry by road hexside only, the Location beneath. | audit |
| B6.41 | Pontoon | 116 | not built |  |  | Pass 59 | No pontoon bridge counters. | audit |
| B6.42 | Collapse | 116 | not built |  |  | Pass 59 | No wooden bridge (SSR), no vehicle weights read, no Collapse DR. | audit |
| B6.43 | Width | 116 | refused | GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 59 | Vehicles cannot be on a bridge; the doubled wreck and VCA penalties are missing. | audit |
| B6.431 | One-Lane | 116 | not built |  |  | Pass 59 | No One-Lane bridge, traffic direction, or VCA limit. | audit |
| B6.44 | Foot Bridges | 116 | not built |  |  | Pass 59 | No foot bridge counter. | audit |
| B6.45 | Underwater | 116 | not built |  |  | Pass 59 | No underwater pontoon bridge. | audit |
| B6.5 | Burning | 116 | not built |  |  | Pass 59 | No terrain Fire at all (B25). | audit |
| B6.6 | (none) | 116 | not built |  |  | Pass 37 | No entrenchment or mine placement rule. | audit |

#### B7 Runways (pages 116 to 117)

6 rows: 1 built, 2 refused, 1 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B7 | Runways | 116 | not applicable |  |  |  | Heading with a scope sentence (hard runways and SSR boulevards). | audit |
| B7.1 | (none) | 116 | not applicable |  |  |  | Definition. | audit |
| B7.2 | (none) | 117 | built | LosCalculator.cs (read from VASL terrain data) | none found |  | No obstacle or Hindrance. No fixture with a runway in the LOS oracle set (board 14 is absent). | audit |
| B7.3 | (none) | 117 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | A runway target "has no TEM". Missing: the -1 TEM in every fire phase against unarmored units, cumulative with FFMO and hexside TEM. | audit |
| B7.4 | (none) | 117 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Missing: the paved road rate by a runway hexside, Open Ground otherwise; Street Fighting and Dash are not built anywhere. | audit |
| B7.5 | (none) | 117 | not built |  |  | Pass 37 | No Fortification placement rule. | audit |

#### B8 Sewers & Tunnels (pages 117 to 118)

15 rows: 14 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B8 | Sewers & Tunnels | 117 | not applicable |  |  |  | Heading. | audit |
| B8.1 | Sewers | 117 | not built |  |  | Pass 61 | Sewers exist only as "not-enforced" SSR text and Balance text on two cards (ScenarioCards.cs lists the rule numbers). No Manhole or Sewer Location. | audit |
| B8.2 | Sewer Location | 117 | not built |  |  | Pass 61 | No Sewer Location, level, or LOS rule. | audit |
| B8.3 | Attacks | 117 | not built |  |  | Pass 61 | No attack on a unit in a sewer. | audit |
| B8.4 | Sewer Movement | 117 | not built |  |  | Pass 61 | No Sewer Movement, no 4TC entry, no capability by SSR. | audit |
| B8.41 | (none) | 117 | not built |  |  | Pass 61 | No "Sewer ?" counter, lost dr, forced movement, or vertical advance. | audit |
| B8.42 | (none) | 117 | not built |  |  | Pass 61 | No Sewer Emergence Chart. | audit |
| B8.43 | (none) | 117 | not built |  |  | Pass 61 | No attack from a sewer. | audit |
| B8.44 | APh/CCPh | 117 | not built |  |  | Pass 61 | No sewer adjacency, fire, or CC. | audit |
| B8.45 | Broken & Berserk | 117 | not built |  |  | Pass 61 | No elimination of broken or berserk units in a sewer. | audit |
| B8.5 | (none) | 117 | not built |  |  | Pass 61 | No Fortification ban, no rubble or Blaze over a Manhole, no DC rubble in a sewer. | audit |
| B8.6 | Tunnels | 118 | not built |  |  | Pass 61 | No tunnels. The LOS category "Tunnel" is VASL's printed tunnel terrain, not this rule. | audit |
| B8.61 | Movement | 118 | not built |  |  | Pass 61 | No tunnel movement. | audit |
| B8.62 | RtPh | 118 | not built |  |  | Pass 61 | No rout through a tunnel. | audit |
| B8.63 | Destruction | 118 | not built |  |  | Pass 61 | No tunnel destruction. | audit |

#### B9 Walls & Hedges (pages 118 to 124)

31 rows: 2 built, 5 built with a deviation, 7 partly, 6 refused, 9 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B9 | Walls & Hedges | 118 | not applicable |  |  |  | Heading; the rule text is in B9.1 to B9.7. | audit |
| B9.1 | (none) | 118 | not applicable |  |  |  | Definition of wall and hedge hexsides. The map read carries them, with road gaps (VaslCompatibleHexFactDerivation: CheckWallHedgeGap); nothing to play. | audit |
| B9.2 | LOS | 118 | built | LosCalculator.cs: CheckHexsideTerrainRule; IsIgnorableHexsideTerrain | LosFidelityTests; LosTests.AnnotatedOrchardsHinderAndRailroadEmbankmentsBlock |  | LOS reproduces VASL: a wall or hedge blocks same-level LOS unless part of the viewing or target hex, with the hexspine rule. Snap Shot and Bypass cases are refused in play (see B9.42). | audit |
| B9.21 | Entrenchments | 118 | partly | LosCalculator.cs: CheckHexsideTerrainRule (entrenchment ends) | LosTests.EntrenchmentEndpointsUseTheHexsideRestrictionInBothDirections | Pass 37 | The LOS rule exists for entrenchment terrain painted on a map. The game has no entrenchment counters, so play never reaches it; seeing the entrenchment but not the unit beneath is absent. | audit |
| B9.3 | TEM | 119 | partly | GamePlanner.Terrain.cs: HexsideTemAt; ScenarioA1FireReference.cs: HexsideTem; ScenarioA1FireCalculator.cs (hexside TEM branch) | BacklogPass10Tests.AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage; ScenarioA1Pass10Tests.AWallReplacesALowerInHexTemAndLeavesNoFfmo | Pass 52 | Built for Infantry IFT fire: wall +2, hedge +1, hexside or hexspine, road gap only for a non-moving unit, none for a Placed DC. Missing: ordnance and vehicle fire (refused at any hex with hexside terrain, FireMapFacts), PRC exclusion, a Thrown DC's TEM to the thrower's Location. | audit |
| B9.31 | (none) | 119 | built with a deviation | ScenarioA1FireCalculator.cs (hexside TEM branch; Residual); GamePlanner.Terrain.cs: HexsideTemAt, WallAdvantageHolder | ScenarioA1Pass10Tests.AWallReplacesALowerInHexTemAndLeavesNoFfmo; ScenarioA1Pass10Tests.AWallTemLowersTheResidualFpLeft | Pass 52 | Rulings R10.5 and R10.6: never cumulative with in-hex TEM, the target takes the better of the two, Residual FP lowered. Missing: a WA claimant losing its in-hex TEM as a kept choice, the Emplacement and friendly AFV alternatives, HEAT at Infantry behind a wall. | audit |
| B9.32 | Wall Advantage (WA) | 119 | built with a deviation | GamePlanner.Terrain.cs: WallAdvantageHolder, Arrivals | BacklogPass10Tests.AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage | Pass 52 | Ruling R10.6: WA is not a player's claim or a counter; it is read from arrival order (Scenario Defender on a tie, else the fire is refused). Missing: the claim itself, vehicles with WA, Bypass, bridge, pillbox, entrenchment, Wire, and enemy-in-Location conditions, broken units sharing WA. | audit |
| B9.321 | (none) | 119 | built with a deviation | GamePlanner.Terrain.cs: WallAdvantageHolder | BacklogPass10Tests.AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage | Pass 52 | Ruling R10.6: two ADJACENT opposing units never both hold WA over the shared hexside. The all-hexsides rule (losing WA over one hexside loses all) is not built; recorded in the backlog. | audit |
| B9.322 | (none) | 119 | not built |  |  | Pass 52 | No Wall Advan counter, no claim or forfeit, none of the five times, no bar for Pinned, TI, or Immobile units. Needs WA as kept state (backlog, R10.6). | audit |
| B9.323 | Mandatory WA | 120 | built with a deviation | GamePlanner.Terrain.cs: WallAdvantageHolder (Claimants) | BacklogPass10Tests.AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage | Pass 52 | Ruling R10.6: a unit with no positive in-hex TEM is taken to hold WA, a unit with one is taken not to. Timing and the rule against forfeiting are not modelled. | audit |
| B9.324 | Concealment | 120 | not built |  |  | Pass 52 | No WA for concealed units by momentary reveal, Dummy stacks, or HIP units; Dummies are simply left out of the claimants. | audit |
| B9.33 | Elevation Effects | 120 | partly | GamePlanner.Terrain.cs: HexsideTemAt (height less range) | none found | Pass 52 | The wall or hedge TEM reduction for a higher firer is built for Infantry fire, with FFMO back at 0. Missing: a test, the HD negation and the -1 hit location drm, the same reduction for shellholes, bridges, and entrenchments. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B9.34 | Indirect Fire | 120 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain, hexside terrain not reviewed for ordnance) | none found | Pass 52 | Indirect Fire at any hex with hexside terrain is refused, so the lowered wall TEM, the one-TEM limit, and Air Bursts with WA are not built. | audit |
| B9.35 | (none) | 120 | partly | GamePlanner.Terrain.cs: HexsideTemAt (target.Level; base level test) | none found | Pass 52 | A target above its hex's base level gets no wall TEM. A wall between hexes of different Base Levels is always refused as a Hillside wall; the test of lower terrain shown between wall and crest line is absent. | audit |
| B9.36 | Hulldown | 120 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain) | none found | Pass 52 | Ordnance fire at a hex with a wall or hedge is refused, so Hull Down behind a wall, the in-hex TEM choice, and the wall TEM of a vehicle against non-ordnance are not built. | audit |
| B9.4 | Movement | 121 | built | GamePlanner.Terrain.cs: GroundStep; GamePlanner.VehicleTerrain.cs: VehicleCost; GamePlanner.Vehicles.cs (hedge Bog in the hex left) | BacklogPass10Tests.AWallOrHedgeCostsOneMoreMfButNotThroughARoadGap; BacklogPass11Tests.WallsHedgesAndHillsCostWhatTheTerrainChartSays; BacklogPass11Tests.AHalftrackCrossingAHedgeIntoWoodsTakesBothBogDrs |  | Infantry +1 MF undoubled, fully-tracked 1 + COT, halftrack hedge 2 + COT with Bog in the hex left, truck NA, no cost through a road gap; checked against the Terrain Chart. Cavalry, armored cars, and motorcycles are not in the catalog. | audit |
| B9.41 | (none) | 121 | built with a deviation | GamePlanner.Terrain.cs: WallAdvantageHolder, Arrivals | BacklogPass10Tests.AWallGivesItsTemUnlessTheAdjacentFirerHoldsWallAdvantage | Pass 52 | Ruling R10.6: loss of WA on entering a new Location is read from arrival order. A failed exit (A12.15) does not lose WA here, and the bar on re-claiming is absent. | audit |
| B9.42 | Vertex LOS | 121 | refused | GamePlanner.Terrain.cs: BypassStep; GamePlanner.Fire.cs: FireMapFacts (play.fire-snap-shot, play.fire-bypass); GamePlanner.VehicleTerrain.cs: VehicleBypass | none found | Pass 52 | Bypass and VBM in a hex with a wall or hedge, fire at a Bypassing stack there, and a Snap Shot at a hexside of such a hex are all refused; vertex LOS and its TEM are not built. | audit |
| B9.5 | Bocage | 121 | refused | GamePlanner.Terrain.cs: WallOn, GroundStep, HexsideTemAt; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 61 | A Bocage hexside reads as other hexside terrain: crossing it and fire at a hex that has it are refused. No game can declare bocage (see B9.51). | audit |
| B9.51 | (none) | 121 | not built |  |  | Pass 61 | No SSR turns walls or hedges into bocage; the map builder has no such transform and the planner no such special rule. | audit |
| B9.52 | (none) | 121 | partly | LosCalculator.cs: CheckHexsideTerrainRule (bocage branch), IsIgnorableHexsideTerrain, IsBlindHex | LosTests.BocageAsHighAsTheHigherEndBlocks | Pass 61 | The LOS calculator blocks through bocage and treats a bocage hexspine as an obstacle; a blind hex behind bocage is refused (LosUnsupportedRule.BocageBlindHex). Play cannot reach it. | audit |
| B9.521 | Hexside LOS | 122 | not built |  |  | Pass 61 | LOS through a bocage hexside by the viewer's WA status needs WA as kept state and per-unit visibility. | audit |
| B9.53 | (none) | 122 | not built |  |  | Pass 61 | No bar on a Gun changing CA and firing through bocage in one phase. | audit |
| B9.531 | (none) | 122 | not built |  |  | Pass 61 | Elevation has no special bocage handling in TEM; the LOS calculator refuses the blind hex cases. | audit |
| B9.54 | Movement | 122 | refused | GamePlanner.Terrain.cs: GroundStep (play.move-hexside); GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 61 | Crossing a Bocage hexside is refused for Infantry and vehicles; the 2 MF cost, the AFV crossing with Underbelly Hit, Schuerzen loss, and Bog are not built. | audit |
| B9.541 | Breach | 122 | not built |  |  | Pass 61 | No Dozer, bulldozer, or Culin device in the catalog, and no Breach counter. | audit |
| B9.55 | Concealment | 122 | not built |  |  | Pass 61 | No bocage concealment, HIP, or setup rule. | audit |
| B9.6 | Hillside Wall/Hedge | 124 | partly | GamePlanner.Terrain.cs: HexsideTemAt (play.fire-hillside-wall), GroundStep | none found | Pass 61 | Movement across a wall between hexes of different levels is costed as a normal wall, as the rule allows. Fire at a target whose crossed wall lies between different Base Levels is refused. The depiction test that tells a Hillside wall from a B9.35 wall is absent. | audit |
| B9.61 | LOS | 124 | partly | LosCalculator.cs: CheckHexsideTerrainRule | LosFidelityTests | Pass 61 | The generic hexside rule blocks only when the wall's ground level equals both ends' elevation, so a Hillside wall is ignored between units a level apart. No Hillside-specific logic (entrenched exception, Crest Line ending at the wall); not settled case by case against the rule. | audit |
| B9.62 | Elevation, Tem & Wall Advantage | 124 | refused | GamePlanner.Terrain.cs: HexsideTemAt (play.fire-hillside-wall) | none found | Pass 61 | Ruling R10.5: fire across a Hillside wall or hedge is refused, so its elevation, TEM, and WA rules are not built. | audit |
| B9.7 | Cactus Hedge | 124 | not built |  |  | Pass 61 | SSR only. No cactus hedge terrain or special rule; the Minimum Move, Low Crawl, or Advance-only crossing is absent. | audit |

#### B10 Hills (pages 124 to 126)

14 rows: 6 built, 1 built with a deviation, 3 partly, 1 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B10 | Hills | 124 | not applicable |  |  |  | Heading. | audit |
| B10.1 | (none) | 124 | built | VaslCompatibleHexFactDerivation.cs (BaseLevel at the center dot); LosCalculator.cs (ground level plus terrain height) | LosFidelityTests; BacklogPass10Tests.HillsDoubleTheCostUpAndAbruptChangesAddPerLevel |  | One base level a hex from its center dot; obstacles rise from the hill level in LOS, as VASL reads the board's elevation grid. | audit |
| B10.11 | Crest Line | 125 | not applicable |  |  |  | Definition of a Crest Line. The code uses the difference of two hexes' base levels for movement and the elevation grid for LOS. | audit |
| B10.2 | Different Level LOS | 125 | built | LosCalculator.cs: CheckTerrainHeightRule (B10.2 EXC), CheckBlindHexRule | LosFidelityTests |  | Different-level LOS as VASL computes it, with the adjacent lower hex exception. | audit |
| B10.21 | Same Level LOS | 125 | built | LosCalculator.cs: CheckTerrainIsHigherRule, CheckTerrainHeightRule | LosFidelityTests |  | Same-level LOS over equal or lower ground is clear; higher ground blocks. | audit |
| B10.211 | Alpine Hill Option | 125 | not built |  |  | Left out: an optional rule by the rulebook's own mark | Optional rule (asterisk), by SSR. No Alpine Hill option. | audit |
| B10.22 | (none) | 125 | built | LosCalculator.cs: CheckTerrainIsHigherRule, CheckHalfLevelTerrainRule, CheckHexsideTerrainRule | LosFidelityTests; LosTests.OpenGroundIsClearAndWoodsBetweenBlock |  | A viewer needs a height equal to the obstacle to see past it; a level 1 viewer sees over ground-level walls and wrecks to a level 1 target. | audit |
| B10.23 | Blind Hexes | 125 | built | LosCalculator.cs: CheckBlindHexRule, IsBlindHex | LosFidelityTests; LosTests.ACliffHexsideMakesTheHexBelowItBlind |  | Crest Line Blind Hexes by range bands of five, reduced by elevation advantage, as VASL computes them. | audit |
| B10.3 | (none) | 125 | partly | ScenarioA1FireCalculator.cs (TEM by the hex's terrain); GamePlanner.Rout.cs: OpenGround | ScenarioA1Pass10Tests.HeightAdvantageIsPlusOneOnlyWithNoOtherPositiveTem | Pass 34 | TEM in fire depends on the other terrain and Height Advantage. Interdiction and the rout's Open Ground test read the hex terrain only and ignore Height Advantage (a fault, see findings). | audit |
| B10.31 | Height Advantage | 125 | partly | GamePlanner.Terrain.cs: HeightAdvantageAt; ScenarioA1FireCalculator.cs (height-advantage) | BacklogPass10Tests.HeightAdvantageExceptAcrossTheClimbedCrestLine; ScenarioA1Pass10Tests.HeightAdvantageIsPlusOneOnlyWithNoOtherPositiveTem | Pass 34 | Built for Infantry IFT fire (ruling R10.4): +1 with no other positive TEM, no FFMO, the climbed Crest Line exception. Missing: ordnance and vehicle fire at another level (refused), CE DRM, Interdiction. | audit |
| B10.4 | (none) | 126 | built | GamePlanner.Terrain.cs: GroundStep; GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass10Tests.HillsDoubleTheCostUpAndAbruptChangesAddPerLevel; BacklogPass11Tests.WallsHedgesAndHillsCostWhatTheTerrainChartSays |  | Infantry pay double COT one level up; vehicles 4 MP more, 2 by a road hexside. Cavalry, horse-drawn, bicycle, and ski units are not in the catalog. | audit |
| B10.5 | Abrupt Elevation Changes | 126 | not applicable |  |  |  | Definition of an Abrupt Elevation Change; the costs are B10.51 and B10.52. | audit |
| B10.51 | (none) | 126 | partly | GamePlanner.Terrain.cs: GroundStep; GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass10Tests.HillsDoubleTheCostUpAndAbruptChangesAddPerLevel; BacklogPass11Tests.AnAbruptElevationChangeIsCrossedByRoadAtItsB1051Cost | Pass 60 | Infantry: 2 MF per intermediate level up, 1 down, the last level at its own cost. Vehicles only by road; off road always refused as a Double-Crest (R11.7). Infantry Bypass and VBM across one are refused. | audit |
| B10.52 | Double Crests | 126 | built with a deviation | GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass11Tests.AnAbruptElevationChangeIsCrossedByRoadAtItsB1051Cost | Pass 60 | Ruling R11.7: every off-road Abrupt Elevation Change is taken to be a Double-Crest and barred to vehicles; the two-hillside-Crest-Lines test is not made. No Cavalry charge. | audit |

#### B11 Cliffs (pages 126 to 127)

15 rows: 2 built, 4 refused, 7 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B11 | Cliffs | 126 | not applicable |  |  |  | Heading. | audit |
| B11.1 | (none) | 126 | not applicable |  |  |  | Definition of cliff hexsides; the map read carries them (HexsideFacts.Cliff). | audit |
| B11.2 | (none) | 126 | built | LosCalculator.cs: LowerCliffHexLevel, CheckTerrainHeightRule, CheckBlindHexRule | LosFidelityTests |  | LOS along a cliff hexside takes the lower hex's level, as VASL does. Judged from the code's structure; the Depression cliff case was not traced. | audit |
| B11.21 | (none) | 126 | built | LosCalculator.cs: CheckBlindHexRule, IsBlindHex | LosTests.ACliffHexsideMakesTheHexBelowItBlind |  | A cliff hexside always leaves at least one Blind Hex to a non-adjacent viewer. | audit |
| B11.3 | (none) | 126 | refused | GamePlanner.Terrain.cs: HexsideTemAt (play.fire-hexside); GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | Any fire at a target hex that has a cliff hexside is refused, whatever hexside the LOS crosses, so the rule that a cliff adds no TEM never applies. | audit |
| B11.31 | (none) | 126 | refused | GamePlanner.Fire.cs: FireMapFacts; GamePlanner.Terrain.cs: HexsideTemAt | none found | Pass 60 | Fire across a cliff to an adjacent lower Location is refused for every weapon, as part of the refusal of B11.3. | audit |
| B11.32 | (none) | 126 | refused | GamePlanner.Fire.cs: FireMapFacts; GamePlanner.Terrain.cs: HexsideTemAt | none found | Pass 60 | Fire across a cliff to an adjacent higher Location is refused for every weapon; the list of weapons allowed is not built. | audit |
| B11.4 | Climbing | 127 | refused | GamePlanner.Terrain.cs: GroundStep (play.move-cliff); GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Crossing a cliff hexside is refused for Infantry and vehicles; Climbing is not built. | audit |
| B11.41 | Falling DR | 127 | not built |  |  | Pass 60 | No Falling DR, weather DRM, or CX IPC limit. | audit |
| B11.42 | (none) | 127 | not built |  |  | Pass 60 | No Climb counter, Climbing vertex, Hazardous Movement while Climbing, or Indirect Fire immunity. | audit |
| B11.43 | (none) | 127 | not built |  |  | Pass 60 | No Climb counters by level, no stacking on a cliff face. | audit |
| B11.431 | (none) | 127 | not built |  |  | Pass 60 | No fire by Climbing units. | audit |
| B11.432 | APh | 127 | not built |  |  | Pass 60 | No advance off a Climb counter. | audit |
| B11.433 | Commando | 127 | not built |  |  | Pass 60 | No Commandos or Gurkhas in the catalog. | audit |
| B11.434 | CX | 127 | not built |  |  | Pass 60 | No CX while Climbing. | audit |

#### B12 Brush (page 127)

8 rows: 3 built, 3 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B12 | Brush | 127 | not applicable |  |  |  | Heading. | audit |
| B12.1 | (none) | 127 | not applicable |  |  |  | Definition of brush; the map read names it Brush. | audit |
| B12.2 | (none) | 127 | built | LosCalculator.cs: CheckHalfLevelTerrainRule, AddHindranceHex; GamePlanner.Fire.cs: FireMapFacts (mapRanges) | LosFidelityTests; BacklogPass15TablePlayerTests (BrushLos) |  | A same-level brush hex between firer and target is +1, the largest Hindrance at each range, on the IFT and To Hit DR. No OBA. | audit |
| B12.3 | (none) | 127 | built | ScenarioA1FireReference.cs: Tem; ScenarioA1FireCalculator.cs (ffmo only in open-ground); GamePlanner.Rout.cs: OpenGround | none found |  | Brush has TEM 0 and takes neither FFMO nor Interdiction. | audit |
| B12.4 | (none) | 127 | built | GamePlanner.Movement.cs: EntryHalfMf; GamePlanner.Terrain.cs: GroundStep | none found |  | Infantry pay 2 MF; vehicles pay the Terrain Chart's 2, 2, 6 MP. No Cavalry. | audit |
| B12.5 | (none) | 127 | not built |  |  | Pass 44 | No Kindling or Spreading Fire (B25 terrain Blazes are not built). | audit |
| B12.6 | (none) | 127 | not built |  |  | Pass 57 | Brush keeps its cost and Hindrance in Deep Snow; recorded in the backlog (section 26, E3.73). | audit |
| B12.7 | Vineyard | 127 | not built |  |  | Pass 60 | SSR only. No vineyard special rule; a terrain named Vineyard would not be admitted. | audit |

#### B13 Woods (pages 127 to 128)

19 rows: 3 built, 3 partly, 11 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B13 | Woods | 127 | not applicable |  |  |  | Heading. | audit |
| B13.1 | (none) | 127 | not applicable |  |  |  | Definition of woods; the map read names it Woods. | audit |
| B13.2 | (none) | 128 | built | LosCalculator.cs: CheckTerrainIsHigherRule, CheckTerrainHeightRule | LosTests.OpenGroundIsClearAndWoodsBetweenBlock; LosFidelityTests |  | Woods are a one-level obstacle added to the hex's level. The Forest and Pine Woods exceptions are not built. | audit |
| B13.3 | Airbursts | 128 | partly | ScenarioA1FireReference.cs: Tem; ScenarioA1FireCalculator.cs (air-burst); GamePlanner.Fire.cs: FireMapFacts (Bypass lane) | ScenarioA1Pass9Tests.AMortarHitInWoodsTakesAirBurstsInsteadOfTheWoodsTem; BacklogPass9Tests.ALightMortarHitsTheAreaAndItsSquadStillFiresItsInherentFp | Pass 50 | +1 for Direct Fire, none against Bypass, -1 for a light mortar's Area hit on Infantry (R9.3). Missing: Air Bursts against CE or OT vehicles and with entrenchment or emplacement TEM, and OBA. | audit |
| B13.31 | (none) | 128 | not built |  |  | Pass 46 | No test of whether the LOS crosses a woods symbol in a woods-road hex. A hex whose center dot is on the road appears to read as Open Ground throughout (see findings, not settled on a real board). | audit |
| B13.32 | (none) | 128 | partly | GamePlanner.Overrun.cs: OverrunAttack | none found | Pass 46 | An OVR takes the Location's TEM, so woods +1 applies where the hex reads as woods. The exception for vehicles on the road is absent, and a woods-road hex may read as Open Ground. Judged from OverrunAttack alone. | audit |
| B13.4 | (none) | 128 | built | GamePlanner.Movement.cs: EntryHalfMf; GamePlanner.Terrain.cs: GroundStep | BacklogPass5Tests (line 289); BacklogPass10Tests (line 394) |  | Infantry pay 2 MF, or the road rate across a road hexside. No Cavalry. | audit |
| B13.41 | Vehicles | 128 | partly | GamePlanner.VehicleTerrain.cs: VehicleCost, VehicleTurnCost | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck; BacklogPass11Tests.AReverseAllEntryKeepsItsStopMpAndNoOvrGoesWithAnAllEntry | Pass 46 | ALL MP with a Bog DR, in Reverse too, with the wreck and VCA penalties doubled (R11.5, R11.8). Missing: moving off the road into the woods part of a woods-road hex; motorcycles. | audit |
| B13.42 | Fully-Tracked | 128 | built | GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck |  | A fully-tracked vehicle enters at half its printed allotment with a Bog DR at +3. | audit |
| B13.421 | Trail Break (TB) | 128 | not built |  |  | Pass 37 | No Trail Break counter, no TB rate, and no Bog Check on leaving a woods-road hex by a non-road hexside (recorded in the backlog). | audit |
| B13.4211 | (none) | 128 | not built |  |  | Pass 37 | No Trail Break removal by a wreck, and no multiple Trail Breaks. | audit |
| B13.4212 | (none) | 128 | not built |  |  | Pass 37 | No 1.5 MF entry by a Trail Break, no -1 Defensive First Fire DRM, no one-way rule. | audit |
| B13.43 | (none) | 128 | not built |  |  | Pass 51 | Riders are not built at all, so the bar on Riders in woods has nothing to apply to. | audit |
| B13.5 | (none) | 128 | not built |  |  | Pass 44 | No Kindling or Spreading Fire. | audit |
| B13.6 | Paths | 128 | not built |  |  | Pass 60 | No path rate. How a path hex reads was not settled; it is not among the admitted terrain names. | audit |
| B13.7 | Forest | 128 | not built |  |  | Pass 60 | SSR only. No Forest: two-level obstacle, +2 TEM, no vehicle entry off road. | audit |
| B13.8 | Pine Woods | 128 | not built |  |  | Pass 60 | SSR only. No Pine Woods special rule. | audit |
| B13.81 | Obstacle Height | 128 | not built |  |  | Pass 60 | No two-level obstacle height for Pine Woods. Inventory: The PDF outline points at page 129; the rule is printed on page 128. | audit |
| B13.82 | MF Cost | 128 | not built |  |  | Pass 60 | No 1.5 MF entry cost for Pine Woods. Inventory: The PDF outline points at page 129; the rule is printed on page 128. | audit |

#### B14 Orchard (page 129)

10 rows: 2 built, 3 partly, 4 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B14 | Orchard | 129 | not applicable |  |  |  | heading | audit |
| B14.1 | (none) | 129 | built | LosCalculator.cs: inherent terrain check in the hex walk (center.IsInherent); GamePlanner.Map.cs: TerrainKey | LosLeftoverTests (partial-orchard fixtures); none found for a whole orchard hex |  | Definition plus the whole-hex (inherent) LOS effect, which the VASL LOS port carries. Not checked against a board's data. | audit |
| B14.2 | Seasons | 129 | partly | LosCalculator.cs: CheckTerrainIsHigherRule, CheckTerrainHeightRule, IsBlindHex | LosTests (orchard and blind hex cases) | Pass 45 | LOS has both the in-season obstacle and the "Orchard, Out of Season" terrain, but nothing ties the season to GameState.ScenarioMonth, so every orchard is in season; some out-of-season LOS cases are refused (OrchardOutOfSeasonCases); fire leaves any orchard Hindrance undecided. | audit |
| B14.21 | Same Level Hindrance | 129 | partly | LosCalculator.cs: AddHindranceHex (orchard as a one level Hindrance); GamePlanner.Fire.cs: FireMapFacts (attributed is false); ScenarioA1FireCalculator.cs: Undecided | ScenarioA1FirePackageTests.UnreviewedElementsAreRefusedWithTheirReason (the refusal code only) | Pass 60 | The LOS read reports the +1 per orchard hex; fire through it is refused as hindrance-unattributed. Missing: count orchard in the fire Hindrance DRM. | audit |
| B14.3 | (none) | 129 | built | ScenarioA1FireReference.cs: Tem (orchard 0); ScenarioA1FireCalculator.cs: FFMO only when TargetTerrain is open-ground; GamePlanner.Rout.cs: open ground test for Interdiction | none found |  | TEM 0 and no FFMO or Interdiction in an orchard, by the terrain key; no test names an orchard target. | audit |
| B14.4 | (none) | 129 | partly | GamePlanner.Movement.cs: EntryHalfMf (orchard 1 MF); GamePlanner.VehicleTerrain.cs: VehicleTerrainHalfMp, VehicleCost | none found | Pass 45 | Infantry pay the Open Ground cost. Vehicles are refused ("orchard is not allowed to a ... vehicle") though the rule and the Terrain Chart give the Open Ground cost: fully tracked 1, halftrack 1, truck 4 (armored car 3), or the road rate by a road hexside. Missing: three table rows. | me |
| B14.5 | (none) | 129 | not built |  |  | Pass 44 | No Kindling or Spreading Fire logic found (kindle 11, spread 9). | audit |
| B14.6 | Orchard Road | 129 | not built |  |  | Pass 60 | No orchard road logic; how a tree-lined road hex reads on the map (road or orchard) was not verified. | audit |
| B14.7 | Cactus Patch | 129 | not built |  |  | Pass 60 | SSR terrain; no cactus patch logic. | audit |
| B14.8 | Olive Grove | 129 | not built |  |  | Pass 60 | SSR terrain; no olive grove logic. | audit |

#### B15 Grain (page 129)

7 rows: 2 built, 2 partly, 1 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B15 | Grain | 129 | not applicable |  |  |  | heading | audit |
| B15.1 | (none) | 129 | not applicable |  |  |  | definition; the map read names the terrain Grain | audit |
| B15.2 | (none) | 129 | partly | GamePlanner.Fire.cs: FireMapFacts; ScenarioA1FireCalculator.cs: Undecided; LosCalculator.cs: CheckHalfLevelTerrainRule | ScenarioA1FirePackageTests.GrainCountsWithADeclaredMonthInSeason; BacklogPass6Tests.AnAfvInAGrainHexAddsItsHindranceToTheGrains | Pass 45 | +1 per grain hex at the same level, June to September. A LOS through grain is refused when the game has no month and also when the month is out of season, where B15.6 makes it Open Ground with no Hindrance. | audit |
| B15.3 | (none) | 129 | built | ScenarioA1FireReference.cs: Tem (grain 0); ScenarioA1FireCalculator.cs: FFMO test; GamePlanner.Fire.cs: FireMapFacts (grain to open-ground out of season); GamePlanner.Rout.cs | BacklogPass5Tests (grain season tests) |  | No TEM; no FFMO or Interdiction in or through in-season grain. | audit |
| B15.4 | (none) | 129 | partly | GamePlanner.Movement.cs: EntryHalfMf, InfantryEntryHalfMf | BacklogPass5Tests.GrainCostsInfantryOneAndAHalfMfOnlyInItsSeason | Pass 43 | Infantry 1.5 MF built. Cavalry and Horse-Drawn units do not exist in the catalog. | audit |
| B15.5 | (none) | 129 | not built |  |  | Pass 44 | No Kindling or Spreading Fire logic (kindle 10, spread 6). | audit |
| B15.6 | Season | 129 | built | GamePlanner.Movement.cs: InfantryEntryHalfMf (April to September); GamePlanner.VehicleTerrain.cs: VehicleCost; GamePlanner.Fire.cs: FireMapFacts (June to September); GamePlanner.Night.cs: ConcealmentTerrain | BacklogPass5Tests.GrainCostsInfantryOneAndAHalfMfOnlyInItsSeason; BacklogPass6Tests.AVehicleSetsUpConcealedOnlyInGrainAndLosesItsQuestionMarkWhenItMovesInView |  | Season and plowed fields as the rule states; refused where the game names no month (ruling R5.19). See B15.2 for the out-of-season LOS refusal. No SSR can redefine the season. | audit |

#### B16 Marsh (page 130)

17 rows: 4 built, 1 refused, 10 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B16 | Marsh | 130 | not applicable |  |  |  | heading | audit |
| B16.1 | (none) | 130 | not built |  |  | Pass 41 | Definition, plus the loss of unpossessed portaged equipment in a marsh hex, for which no logic was found. | audit |
| B16.2 | (none) | 130 | built | GamePlanner.Fire.cs: FireMapFacts (Marsh counted only when sameLevel) | none found |  | +1 per marsh hex between same-level firer and target; read from the LOS result's Hindrances. | audit |
| B16.21 | (none) | 130 | not applicable |  |  |  | Not a rule: the PDF outline lists 16.21, but page 130 prints 16.2, 16.3, 16.31, 16.32 and no 16.21 (checked in the text layer by me and on the rendered page by the audit). Inventory: In the PDF outline; no head with this number was found in the page's text layer. | me |
| B16.3 | (none) | 130 | built | ScenarioA1FireReference.cs: Tem (marsh 0); ScenarioA1FireCalculator.cs: FFMO test; GamePlanner.Rout.cs | ScenarioA1Pass10Tests.MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem |  | TEM 0, no FFMO, no Interdiction in marsh. Settles the coverage document's unconfirmed "B16 FFMO in marsh". | audit |
| B16.31 | (none) | 130 | not built |  |  | Pass 45 | Recorded leave-out (backlog, ruling R10.1): ordnance HE into marsh keeps its full FP, which is a wrong result, not a refusal. | audit |
| B16.32 | (none) | 130 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-marsh) | none found | Pass 41 | Any direct fire from a marsh hex is refused. Missing: the weapon limits and Area Fire by firer. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B16.4 | (none) | 130 | built | GamePlanner.Terrain.cs: GroundStep; GamePlanner.Movement.cs (play.move-marsh); GamePlanner.CloseCombat.cs (play.advance-marsh); GamePlanner.Rout.cs: RoutEntry and the Low Crawl bar | BacklogPass10Tests.MarshTakesTheWholeAllotment; BacklogPass25Tests.AMarshEntryHexDoesNotHoldTheAdvancePhase |  | Whole allotment in the MPh and RtPh, no APh entry, no Low Crawl, Minimum Move from below. The bridge exception and Cavalry do not exist. | audit |
| B16.41 | (none) | 130 | built | GamePlanner.VehicleTerrain.cs: VehicleCost (marsh not in the table); GamePlanner.Movement.cs (play.move-push-terrain) | none found |  | Vehicles and pushed Guns are barred from marsh, as the rule says for non-amphibious ones. The message cites the Terrain Chart, not B16.41. No bridge exception. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B16.42 | Amphibians | 130 | not built |  |  | Pass 55 | No amphibious vehicles or boats in the catalog or code. | audit |
| B16.43 | Bog | 130 | not built |  |  | Pass 45 | A vehicle entering a hex adjacent to marsh takes no Bog Check; nothing refuses it and no ruling records it. | audit |
| B16.5 | (none) | 130 | not built |  |  | Pass 37 | No terrain bar on Fortifications in marsh was found (search only). | audit |
| B16.6 | Water Depth | 130 | not built |  |  | Pass 41 | No water depth setting; marsh is never turned to stream, river, or mudflat. | audit |
| B16.7 | Mudflats | 130 | not built |  |  | Pass 41 | No mudflat. | audit |
| B16.71 | (none) | 130 | not built |  |  | Pass 41 | No mudflat. | audit |
| B16.72 | (none) | 130 | not built |  |  | Pass 55 | No mudflat; no amphibians. | audit |
| B16.8 | Weather | 130 | not built |  |  | Pass 57 | Recorded in the backlog (referee, pass 16, E3.722): marsh keeps its normal rules in Snow, Deep Snow, and sub-zero weather. | audit |

#### B17 Crag (page 130)

6 rows: 1 partly, 2 refused, 1 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B17 | Crag | 130 | not applicable |  |  |  | heading | audit |
| B17.1 | (none) | 130 | not applicable |  |  |  | definition; the whole-hex LOS effect is in the VASL LOS port as inherent terrain | audit |
| B17.2 | (none) | 130 | partly | LosCalculator.cs: AddHindranceHex; GamePlanner.Fire.cs: FireMapFacts; ScenarioA1FireCalculator.cs: Undecided | none found | Pass 60 | The LOS read reports a crag Hindrance by the board's terrain data; fire through it is refused as hindrance-unattributed. | audit |
| B17.3 | (none) | 130 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain) | none found | Pass 60 | A target in a crag hex has no TEM in the Fire package. Missing: TEM +1. | audit |
| B17.4 | (none) | 130 | refused | GamePlanner.Terrain.cs: InfantryStep (play.move-terrain); GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Infantry entry refused (rule: 2 MF). Vehicles are refused, which matches the rule's bar. The dm mortar exception is absent. | audit |
| B17.5 | (none) | 130 | not built |  |  | Pass 37 | No terrain bar on Fortifications found; nothing can enter a crag hex in any case. | audit |

#### B18 Graveyard (pages 130 to 131)

8 rows: 1 partly, 3 refused, 2 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B18 | Graveyard | 130 | not applicable |  |  |  | heading | audit |
| B18.1 | (none) | 130 | not applicable |  |  |  | definition; whole-hex LOS effect as B17.1 | audit |
| B18.2 | (none) | 130 | partly | LosCalculator.cs: AddHindranceHex; GamePlanner.Fire.cs: FireMapFacts; ScenarioA1FireCalculator.cs: Undecided | none found | Pass 60 | As B17.2: the LOS read reports it, fire through it is refused. | audit |
| B18.3 | (none) | 130 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain) | none found | Pass 60 | No TEM for a graveyard target. Missing: TEM +1. | audit |
| B18.4 | (none) | 130 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Entry of a graveyard is refused for all units; no graveyard road is read. | audit |
| B18.41 | (none) | 130 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | Missing: Infantry 1 MF; vehicles 1 MP (2 BU) by a graveyard road hexside; fully tracked at half the allotment with a Bog DR at +3 otherwise. | audit |
| B18.42 | (none) | 131 | not built |  |  | Pass 60 | No graveyard road; moot while entry is refused. | audit |
| B18.43 | (none) | 131 | not built |  |  | Pass 60 | No Gun rule for graveyards; moot while entry is refused. | audit |

#### B19 Gullies (page 131)

9 rows: 4 partly, 2 refused, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B19 | Gullies | 131 | not applicable |  |  |  | heading | audit |
| B19.1 | (none) | 131 | not applicable |  |  |  | definition; the map read marks the hex by DepressionTerrain. The bridge counter part belongs to B6. | audit |
| B19.2 | (none) | 131 | partly | LosCalculator.cs: CheckDepressionRule, LosFollowsDepression | LosTests (gully hex at level -1 cases) | Pass 41 | LOS into and out of a gully is in the VASL LOS port. No unit can be IN a gully, since entry and setup are refused. | audit |
| B19.21 | (none) | 131 | partly | LosCalculator.cs: the depression test in the terrain height rule (comment cites B19.21) | none found | Pass 41 | LOS only: woods or brush in a gully hex still counts. Not reachable in play beyond LOS through such a hex. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B19.3 | (none) | 131 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain) | none found | Pass 41 | A gully target has no TEM in the Fire package. Missing: Open Ground when a LOS INTO it exists. | audit |
| B19.4 | (none) | 131 | refused | GamePlanner.Terrain.cs: InfantryStep (play.move-terrain, ruling R10.1) | none found | Pass 41 | Missing: 2 MF plus the other terrain, and the exit cost. | audit |
| B19.5 | Hill Depressions | 131 | not applicable |  |  |  | definition of Hill Depression and Crest Line-Depression hexside | audit |
| B19.51 | (none) | 131 | partly | LosCalculator.cs: CheckDepressionRule (blocks at a Crest Line-Depression vertex) | none found | Pass 41 | LOS only, as VASL does it; not checked case by case against the rule's examples. | audit |
| B19.52 | (none) | 131 | partly | LosCalculator.cs: CheckDepressionRule | none found | Pass 41 | LOS only; the exception to the one level per hex requirement was not verified in the port. | audit |

#### B20 Streams & Crest Status (pages 131 to 133)

26 rows: 1 partly, 6 refused, 17 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B20 | Streams & Crest Status | 131 | not applicable |  |  |  | heading | audit |
| B20.1 | (none) | 131 | not applicable |  |  |  | definition; the map read names stream terrain by depth | audit |
| B20.2 | (none) | 132 | partly | LosCalculator.cs: CheckDepressionRule, LosFollowsDepression | none found for a stream | Pass 41 | LOS as for a gully. No unit can be IN a stream. | audit |
| B20.3 | (none) | 132 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain) | none found | Pass 41 | No TEM for a stream target. | audit |
| B20.4 | Depth | 132 | not built |  |  | Pass 41 | No depth setting on the game or the card, and no DYO dr; TerrainType only names the four depths. | audit |
| B20.41 | Dry | 132 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 41 | A dry stream is refused like a gully. | audit |
| B20.42 | Shallow | 132 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 41 | Missing: 3 MF. | audit |
| B20.43 | Deep | 132 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 41 | Missing: 4 MF and CX. | audit |
| B20.44 | Flooded | 132 | not built |  |  | Pass 41 | No flooded level change, in LOS or play. | audit |
| B20.45 | Exit | 132 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 41 | A step out of a stream hex cannot arise; no exit rule. | audit |
| B20.46 | Vehicles | 132 | refused | GamePlanner.VehicleTerrain.cs: VehicleCost (not a reviewed entry for vehicles) | none found | Pass 41 | Missing: gully cost for vehicles and the Bog Check on leaving upward. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B20.5 | (none) | 132 | not built |  |  | Pass 37 | No Fortification bar by stream depth. | audit |
| B20.6 | Fire | 133 | not built |  |  | Pass 41 | No fire limits IN a stream; moot while entry is refused. | audit |
| B20.7 | Frigid/Frozen | 133 | not built |  |  | Pass 41 | No frozen or frigid stream (backlog, pass 16 row, names frozen streams). | audit |
| B20.8 | Fords | 133 | not built |  |  | Pass 41 | No ford counter or SSR; VASL's BridgeToFord rule is accepted by the map builder and does nothing. | audit |
| B20.81 | (none) | 133 | not built |  |  | Pass 41 | No ford. | audit |
| B20.82 | (none) | 133 | not built |  |  | Pass 41 | No ford. | audit |
| B20.9 | Crest Status | 133 | not built |  |  | Pass 41 | No Crest status anywhere in Play, Rules, or Units. | audit |
| B20.91 | (none) | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.92 | TEM | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.93 | MPh/APh | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.94 | Fire/CC | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.95 | SW | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.96 | Broken Units | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.97 | Fortifications | 133 | not built |  |  | Pass 41 | No Crest status. | audit |
| B20.98 | Stacking | 133 | not built |  |  | Pass 41 | No Crest status. | audit |

#### B21 Water Obstacles (pages 134 to 135)

17 rows: 1 partly, 2 refused, 10 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B21 | Water Obstacles | 134 | not applicable |  |  |  | heading | audit |
| B21.1 | (none) | 134 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 41 | Water is not a reviewed entry or target terrain; the game does not tell the four kinds apart. | audit |
| B21.11 | Canal | 134 | not applicable |  |  |  | definition. GamePlanner.CardSetup.cs: IsRiver uses a Water, River, or Canal name only for the A2.5 delayed entry radius. | audit |
| B21.12 | River | 134 | not applicable |  |  |  | definition | audit |
| B21.121 | Current | 134 | not built |  |  | Pass 41 | No Current; no boats or amphibians. | audit |
| B21.122 | Depth | 134 | not built |  |  | Pass 41 | No river depth setting. | audit |
| B21.13 | Pond | 134 | not built |  |  | Pass 41 | No pond hexside rule; VASL names ponds and rivers both Water (backlog, ruling R25.2). | audit |
| B21.14 | Lake/Ocean | 134 | not applicable |  |  |  | definition | audit |
| B21.2 | (none) | 134 | partly | LosCalculator.cs: the elevation tests and the water exception in the terrain height rule | none found | Pass 41 | LOS reads a Water Obstacle at its mapped level. No unit can be IN one. | audit |
| B21.21 | (none) | 134 | not built |  |  | Pass 41 | No flooded river or pond level change. | audit |
| B21.3 | (none) | 134 | not built |  |  | Pass 41 | No unit can be in a Water Obstacle, so nothing fires at one; no TEM entry exists. | audit |
| B21.4 | (none) | 134 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 41 | Entry is refused; bridges, boats, amphibians, and swimming do not exist. | audit |
| B21.41 | Fording | 134 | not built |  |  | Pass 41 | No fording. | audit |
| B21.42 | (none) | 135 | not built |  |  | Pass 41 | No fording. | audit |
| B21.43 | (none) | 135 | not built |  |  | Pass 41 | No fording. | audit |
| B21.5 | (none) | 135 | not built |  |  | Pass 37 | No Fortification bar in water. | audit |
| B21.6 | Ice | 135 | not built |  |  | Pass 41 | No ice. | audit |

#### B22 Valley (page 135)

5 rows: 1 built, 2 partly, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B22 | Valley | 135 | not applicable |  |  |  | heading | audit |
| B22.1 | (none) | 135 | not applicable |  |  |  | definition; a valley hex is a hex whose base level the map reads as -1 | audit |
| B22.2 | (none) | 135 | partly | LosCalculator.cs (source and target elevation from BaseLevel); GamePlanner.Terrain.cs: GroundStep (rise); HeightAdvantageAt; GamePlanner.VehicleTerrain.cs: VehicleCost | LosTests (depression hexes at level -1 only) | Pass 60 | No valley code, but levels are read as signed base levels, so a level -1 hex is handled like a hill one level down. No test has a non-Depression hex below level 0, in LOS or play. | audit |
| B22.3 | (none) | 135 | built | GamePlanner.Map.cs: TerrainKey; ScenarioA1FireCalculator.cs: FFMO test; GamePlanner.Rout.cs | none found |  | A valley hex of Open Ground reads as open-ground whatever its level, so TEM, FFMO, and Rout treat it as Open Ground. Untested at level -1. | audit |
| B22.4 | (none) | 135 | partly | GamePlanner.Terrain.cs: GroundStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 60 | The cost is the other terrain's, with the usual cost of climbing out. Bicycles and ski units do not exist. | audit |

#### B23 Buildings (pages 135 to 141)

56 rows: 11 built, 12 partly, 8 refused, 20 not built, 5 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B23 | Buildings | 135 | not applicable |  |  |  | Heading. | audit |
| B23.1 | (none) | 135 | not applicable |  |  |  | Definition of a building hex and of a multi-hex building; the map read carries it (center and level terrain, building hexside terrain). | audit |
| B23.2 | (none) | 135 | built | LosCalculator.cs: terrain height rules (CheckTerrainHeightRule and the half-level tests) | LosTests |  | A building blocks LOS by its height on its hex's base level, as VASL reads it. LOS only; nothing else in the rule. | audit |
| B23.21 | Single Story House | 135 | built | LosCalculator.cs; GamePlanner.Map.cs: TerrainKey; GamePlanner.Movement.cs: EntryHalfMf | LosTests; BacklogPass10Tests |  | One-level obstacle; units at the hex's own level. Rooftops of such a house do not exist for units, which holds since no rooftop is entered. | audit |
| B23.211 | Lumberyard | 135 | refused | GamePlanner.Terrain.cs: InfantryStep (play.move-terrain); GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain); GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 42 | Lumberyard terrain is not in the terrain key (ruling R10.1), so entry and fire at it are refused; LOS reads it. Missing: all its rules (wooden house but for Rout, Rally, VC, EC, mortar fire, no OVR, vehicles by Bypass only). | audit |
| B23.22 | Two Story House | 136 | built | VaslCompatibleHexFactDerivation.cs: Stairway (inherent for "Building, 1 Level"); GamePlanner.Terrain.cs: InfantryStep; LosCalculator.cs | BacklogPass10Tests.StairwellsConnectLevelsAndUpperLevelsStayInTheirBuilding; HexFactDerivationTests |  | 1.5 level obstacle with a level 1 Location and an inherent stairwell in each hex (ruling R10.2). | audit |
| B23.23 | Multi-Story Buildings | 136 | built | VaslCompatibleHexFactDerivation.cs: Stairway; GamePlanner.Terrain.cs: InfantryStep (play.move-stairwell); LosCalculator.cs: CheckSameHexRule | BacklogPass10Tests.StairwellsConnectLevelsAndUpperLevelsStayInTheirBuilding |  | Levels 1 and 2; levels change only in a printed stairwell hex. | audit |
| B23.24 | Third Level Structures | 136 | partly | GamePlanner.cs: OrdinaryBuildings (names to "4 Level"); LosCalculator.cs | none found | Pass 42 | A board's own three-level building is read in LOS and play. Missing: Level 3 by SSR (whole building or one hex), and the inherent stairwell of a third-level hex with no printed one (the map read marks an inherent stairwell only for one-upper-level buildings). | audit |
| B23.25 | Adjacent Building Hexes | 136 | partly | LosCalculator.cs: CheckSameHexRule and the building restriction rule; GamePlanner.Map.cs: IsAdjacent | LosTests | Pass 47 | LOS inside one building is built as VASL has it. ADJACENT is read only between two hexes at one elevation; two levels of one hex joined by a stairwell are never ADJACENT in play (Step needs a hex distance of 1). Missing: vertical ADJACENT for Fire Groups, DM, rout, and CC entry. | audit |
| B23.26 | Stairwell | 136 | refused | ScenarioA1FireCalculator.cs: range below 1 and not TPBF gives asl.a1.fire.out-of-range; FireRange.cs: SameHexOtherLevel | none found | Pass 47 | Fire between levels of one hex is refused (ruling R10.2, backlog). Missing: the attack up or down one level, PBF, and the never-ADJACENT rule for ground and level 2. | audit |
| B23.3 | (none) | 136 | built | ScenarioA1FireReference.cs: Tem; GamePlanner.cs: Infantry OVR NTC modifier | ScenarioA1 fire tests; DeclareOverrunTests |  | Stone +3, wooden +2. Material comes from the map's terrain name. | audit |
| B23.31 | (none) | 136 | built | GamePlanner.Fire.cs: FireMapFacts (Bypass lane terrain) | BacklogPass10Tests |  | TEM is the target's building's; a Bypassing stack takes none. Ruling R10.7 also denies it against Residual FP, where the rule's wording limits the denial to non-Residual-FP attacks: to check. Factory exception not built (B23.741). | audit |
| B23.32 | Indirect Fire | 136 | not built |  |  | Pass 50 | No Indirect Fire rule for buildings: a mortar hit attacks only the named Location with its plain TEM, with no +1 per level above and no attack on the other levels. OBA is absent. A silent omission (findings 3). | audit |
| B23.4 | (none) | 136 | built | GamePlanner.Movement.cs: EntryHalfMf; GamePlanner.Terrain.cs: InfantryStep, GroundStep, BypassStep; GamePlanner.VehicleTerrain.cs: VehicleCost, VehicleBypass | BacklogPass10Tests |  | Two MF to enter, one MF per level in a stairwell hex, vehicles at ground level and by VBM only. Recorded deviation (R10.1): a road hexside is always entered at the road rate. | audit |
| B23.41 | Cellars | 136 | refused | GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 42 | A fully-tracked AFV's entry of a building is refused (ruling R11.7). Missing: half-allotment cost, Bog DR +3 or +4, rubble on a colored dr of 0 or less, the cellar fall on a colored 6, crew survival. | audit |
| B23.42 | (none) | 136 | built | BoardLocation levels; GamePlanner.Terrain.cs: InfantryStep | BacklogPass10Tests |  | Each level is its own Location with its own stacking; a unit is in one level. | audit |
| B23.421 | Upper Levels | 136 | built | GamePlanner.Terrain.cs: InfantryStep (upper-level step, same building, same level) | BacklogPass10Tests; BacklogPass31Tests.AnAdvanceFromAnUpperLevelIsOfferedNoGroundLevelLocation |  | Upper levels cost as ground level. Level counters are table procedure. | audit |
| B23.422 | (none) | 137 | built | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.CloseCombat.cs: AdvanceLocations, ChargeNeighbors | BacklogPass31Tests; BacklogPass27Tests.ABerserkChargeClimbsAStairwellToAnEnemyUpstairs |  | CC is by Location; an advance is one level in a stairwell hex or one hex, never both; no step from an upper level to another building. The Split Level exception is not built (B23.72). | audit |
| B23.423 | Guns | 137 | partly | GamePlanner.Ordnance.cs: SupportWeaponMapFacts (play.ordnance-mortar-building); GamePlanner.Movement.cs: play.move-push-terrain | BacklogPass9Tests (mortar from C7 refused) | Pass 54 | A mortar does not fire from a building; a Gun is not pushed into a building (only Open Ground or grain). Missing: no setup check keeps a 5/8" weapon off upper levels or a large Gun out of a building; no Aerial targets exist. | audit |
| B23.424 | Scaling | 137 | not built |  |  | Pass 60 | No Commandos, no Climbing (B11.4), no Climb counter. | audit |
| B23.5 | (none) | 137 | not built |  |  | Pass 42 | No placement check for Fortifications in a building hex; fortification counters are placed and read by nothing. | audit |
| B23.6 | (none) | 137 | not built |  |  | Pass 44 | No Kindling or spread numbers for buildings; no burning building state. | audit |
| B23.7 | Special Building Types | 137 | not applicable |  |  |  | Heading. | audit |
| B23.71 | Rowhouse | 137 | partly | LosCalculator.cs: CheckRowhouseFactoryWallAndBreach; GamePlanner.Terrain.cs: WallOn ("other"), GroundStep (play.move-hexside), InfantryStep (upper level needs no hexside terrain), HexsideTemAt (play.fire-hexside) | LosTests.ARowhouseWallBlocksLosThroughIt | Pass 42 | The black bar blocks LOS. A step across it, and fire at a hex that has one, are refused. Missing: the 3 MF ground "bypass" between Rowhouses with its vertex fire, the Fire Group limit, and each hex as its own building for Rout and Mopping Up. | audit |
| B23.711 | Breach | 137 | not built |  |  | Pass 42 | No Breach counter, no DC attack on a hexside. LOS refuses a Breach terrain as unsupported (LosUnsupportedRule.InteriorFactoryWall). | audit |
| B23.712 | Variable Height Rowhouses | 138 | partly | LosCalculator.cs (heights and Rowhouse wall) | none found | Pass 42 | LOS only, as far as the map gives the two heights; play is refused as for B23.71. | audit |
| B23.72 | Split Level Buildings | 138 | refused | GamePlanner.Terrain.cs: InfantryStep (the upper-level step needs equal base levels: play.move-upper-level) | none found | Pass 42 | Movement inside a Split Level building above ground is refused. Missing: level 1 of the higher hex joined to level 2 of the lower, and the ground-to-ground "bypass" step. LOS follows the map's heights. | audit |
| B23.721 | (none) | 138 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 42 | As B23.72: the one-level-lower connection and the 4 MF Rowhouse step are not built. | audit |
| B23.722 | (none) | 138 | not built |  |  | Pass 42 | No SSR gives a hex more building levels than its neighbor, and no roof-between LOS bar is read. | audit |
| B23.73 | Marketplace | 139 | partly | VaslCompatibleHexFactDerivation.cs (ground level Open Ground under a Market Place level); GamePlanner.Map.cs: TerrainKey | HexFactDerivationTests | Pass 42 | The ground level plays and is fired on as Open Ground because the map read gives it so. Missing: everything about its level 1. | audit |
| B23.731 | (none) | 139 | refused | GamePlanner.Terrain.cs: InfantryStep (play.move-level: the level is not building terrain in the key) | none found | Pass 42 | The level 1 Location cannot be entered. Missing: the exterior staircase and its "in the open" movement. | audit |
| B23.732 | (none) | 139 | partly | LosCalculator.cs (Marketplace category) | none found | Pass 42 | LOS is whatever the VASL rules give the Marketplace category; not checked against this rule's three cases by a test. Fire to or from level 1 is refused (terrain not in the key). | audit |
| B23.733 | (none) | 139 | not built |  |  | Pass 42 | No Wire or roadblock placement; Interdiction at its ground level follows only from the Open Ground read. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B23.74 | Factory | 139 | partly | LosCalculator.cs (Factory category); VaslMapBuilder.cs (board SSR terrain changes) | LosTests; HexFactDerivationTests.FactoryWithoutStairwayHasNoUpperLevelOrCellar | Pass 42 | A Factory in the map data is read in LOS with no upper floors. In play Factory terrain is refused, and a Factory by scenario SSR is not applied: The Tractor Works' X3 plays as an ordinary stone building (ruling R17.13). | audit |
| B23.741 | LOS/TEM | 139 | partly | LosCalculator.cs: Factory hindrance and FactoryRooftopRule (some cases refused: LosUnsupportedRule.FactoryRooftop, InteriorFactoryWall) | LosTests.RooflessFactoryAddsTwoHindrance | Pass 42 | LOS and Hindrance inside a Factory are in the LOS engine. Missing: the +1 Factory TEM, Indirect Fire TEM, Sniper TEM, Residual FP rule; a Factory target is refused. | audit |
| B23.742 | MF/MP | 139 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: VehicleCost | none found | Pass 42 | Factory terrain is not an entry. Missing: 1 MF across a Factory hexside, the AFV's quarter-allotment move with Bog +1, Vehicular-Sized Entrances. | audit |
| B23.743 | Factory Rubble | 140 | not built |  |  | Pass 42 | No Factory rubble rule. | audit |
| B23.8 | Rooftops | 140 | refused | GamePlanner.Terrain.cs: InfantryStep (play.move-terrain); GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain) | none found | Pass 42 | A Rooftop Location is not entered or fired on (ruling R10.1). Missing: the rooftop SSR, the inherent stairwell, and its MF costs. | audit |
| B23.81 | TEM | 140 | not built |  |  | Pass 42 | No rooftop TEM, Height Advantage, or Open Ground status. | audit |
| B23.82 | Concealment | 140 | not built |  |  | Pass 42 | No rooftop concealment rule. | audit |
| B23.83 | Rally | 140 | not built |  |  | Pass 42 | No rooftop Rally or Victory rule; Victory leaves rooftops out of a building's Locations (GamePlanner.Victory.cs). | audit |
| B23.84 | Obstruction | 140 | built | LosCalculator.cs: rooftop adjustments | LosTests.ARooftopCountsHalfALevelLower |  | The building's obstacle height does not rise for its rooftop Location. | audit |
| B23.85 | Guns | 140 | not built |  |  | Pass 42 | No mortar on a rooftop. | audit |
| B23.86 | Rubble | 140 | not built |  |  | Pass 42 | No rubble creation, so no rooftop removal. | audit |
| B23.87 | Factory Rooftop Access Points | 140 | partly | LosCalculator.cs: IsRooftopLosBlocked | none found | Pass 42 | Only the LOS bar between a Factory Rooftop and the Factory below is built. Missing: Access Points, their ADJACENT rule, and the 2 or 3 MF climb. | audit |
| B23.88 | Attack Effects | 140 | not built |  |  | Pass 42 | No quasi-Location attacks. | audit |
| B23.9 | Fortified Buildings | 140 | not applicable |  |  |  | Heading with an introduction only. | audit |
| B23.91 | (none) | 140 | not built |  |  | Pass 42 | The vocabulary has asl:fortified-location; nothing in Play or Rules reads the counter. | audit |
| B23.911 | (none) | 140 | not built |  |  | Pass 42 | No secret record of Fortified Locations and no revealing. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B23.912 | (none) | 140 | not built |  |  | Pass 42 | No ground-up placement check. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B23.92 | (none) | 140 | not applicable |  |  |  | Cross-reference: treated as other buildings but for the rules below. | audit |
| B23.921 | TEM | 140 | not built |  |  | Pass 42 | No +1 TEM and no reversed CH modifier. | audit |
| B23.922 | Entry | 140 | partly | ScenarioA1BoardObservationProvider.cs (case A1-fortified-unbreached-enemy-squad); ScenarioA1OccupiedConclusionResolver.cs | Board01TerrainCatalogTests; ScenarioA1OccupiedPackageTests | Pass 42 | The bounded Occupied package concludes the entry prohibited from a supplied snapshot fact IsFortified. The live planner never sets that fact, so it cannot arise in play. Missing: live Fortified state, the lost MPh or APh, MF spent in place, the berserk rule. | audit |
| B23.9221 | Breach | 141 | partly | ScenarioA1BoardObservationProvider.cs (case A1-fortified-breached-entry); ScenarioA1OccupiedConclusionResolver.cs | ScenarioA1OccupiedPackageTests; Board01TerrainCatalogTests | Pass 42 | The bounded package qualifies an APh advance through a valid Breach from supplied facts only. Missing: Breach creation (DC or HE KIA), the Breach counter, and DC Placement on an unenterable Location. Inventory: The PDF outline points at page 140; the rule is printed on page 141. | audit |
| B23.93 | Guns | 141 | not built |  |  | Pass 42 | No Gun setup rules for Fortified Buildings. | audit |
| B23.94 | (none) | 141 | not built |  |  | Pass 44 | No Flame or spread, so no -1 DRM. | audit |

#### B24 Rubble (pages 141 to 143)

19 rows: 1 built, 3 partly, 14 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B24 | Rubble | 141 | not applicable |  |  |  | Heading. | audit |
| B24.1 | (none) | 141 | partly | GamePlanner.Map.cs: TerrainKey (wooden-rubble, stone-rubble); GamePlanner.Terrain.cs: IsRubbleTerrain | ScenarioA1Pass13Tests.MarshAndRubbleGiveNoRallyTerrainDrm | Pass 42 | Rubble printed on the map is its own terrain, not a building (no rally bonus, no Bypass). A rubble counter (asl:rubble) placed in play is read by nothing. Inventory: The PDF outline points at page 140; the rule is printed on page 141. | audit |
| B24.11 | Creation | 141 | not built |  |  | Pass 42 | No rubble creation by HE or HEAT; no collapse dr. Needs the 70mm+ HE and KIA reads of the IFT. | audit |
| B24.12 | Falling Rubble | 141 | not built |  |  | Pass 42 | No Falling Rubble DR. | audit |
| B24.121 | (none) | 141 | not built |  |  | Pass 42 | No fallen rubble effects, Bog Check, or chain collapse. | audit |
| B24.2 | (none) | 141 | partly | LosCalculator.cs (rubble terrain as a half-level obstacle, the rubble hindrance tests) | none found | Pass 42 | Ground-level rubble printed on the map is read in LOS as VASL has it. Missing: upper-level rubble, the rubble-covered ground level beneath it, gully rubble and its Crest rules. | audit |
| B24.3 | (none) | 142 | built | ScenarioA1FireReference.cs: Tem | ScenarioA1Pass10Tests.MarshHasNoTemButNoFfmoAndRubbleHasItsBuildingsTem |  | Rubble has its building's TEM (ruling R10.1). The Fortified exception and the +1 below upper rubble have nothing to apply to. | audit |
| B24.4 | (none) | 142 | partly | GamePlanner.Movement.cs: EntryHalfMf; GamePlanner.VehicleTerrain.cs: VehicleCost, Bypassable; GamePlanner.Terrain.cs: BypassStep | none found | Pass 42 | Infantry pay 3 MF; a fully-tracked vehicle pays half its allotment with a Bog DR at +3, others are barred; no Bypass or VBM in rubble; the road rate is not given into rubble. Missing: 3 MF stairwell moves at a rubble level, Manholes. | audit |
| B24.5 | (none) | 142 | not built |  |  | Pass 42 | No Fortification placement check. | audit |
| B24.6 | Fire | 142 | not built |  |  | Pass 44 | No Kindling, Blaze, or Flame for rubble. | audit |
| B24.7 | Clearance | 142 | not built |  |  | Pass 37 | No Clearance: no Task, no Clearance DR, no DRM table, no TI by labor (backlog: "Tasks that place TI"). | audit |
| B24.71 | Rubble | 142 | not built |  |  | Pass 37 | No rubble Clearance, no Trail Break counter, no bulldozer. | audit |
| B24.72 | Fire | 142 | not built |  |  | Pass 44 | No Flame to extinguish. | audit |
| B24.721 | Hamper | 142 | not built |  |  | Pass 44 | No Hamper. | audit |
| B24.73 | Wire | 142 | not built |  |  | Pass 38 | No Wire removal; Wire counters are read by nothing (B26). Inventory: The PDF outline numbers this 24.72 a second time; the page prints 24.73. | audit |
| B24.74 | Minefield | 142 | not built |  |  | Pass 38 | No minefield Clearance; mines are not built (B28). | audit |
| B24.75 | Set DC | 142 | not built |  |  | Pass 49 | No Set DC (A23.7), so none to remove. | audit |
| B24.76 | Roadblock | 142 | not built |  |  | Pass 37 | No roadblock removal; roadblocks are read by nothing (B29). | audit |
| B24.8 | Labor Status | 143 | not built |  |  | Pass 37 | No Labor counter; a failed Manhandling DR places none (ruling R8.6, backlog). | audit |

#### B25 Fire (pages 143 to 145)

22 rows: 1 built, 3 partly, 15 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B25 | Fire | 143 | not applicable |  |  |  | Heading. | audit |
| B25.1 | (none) | 143 | not built |  |  | Pass 44 | No Flame or terrain Blaze state and no Burnable Terrain data (the Kindle column of the Terrain Chart is not transcribed for play). Only a wreck's Blaze entity (asl:fire) exists. Inventory: The PDF outline points at page 142; the rule is printed on page 143. | audit |
| B25.11 | Kindling | 143 | not built |  |  | Pass 44 | No Kindling attempt. Inventory: The PDF outline points at page 142; the rule is printed on page 143. | audit |
| B25.12 | FT | 143 | not built |  |  | Pass 44 | A FT attack makes no Flame (ruling R15.1, backlog). | audit |
| B25.13 | HE | 143 | not built |  |  | Pass 44 | HE and HEAT make no Flame. | audit |
| B25.14 | Wreck Blaze | 143 | partly | ScenarioA1FireCalculator.cs: Vehicle line (half the Kill Number, Unlikely Kill dr 1); ScenarioA1ArmorCalculator.cs (half the Final TK#); GamePlanner.Fire.cs: vehicle effect events; GamePlanner.Ordnance.cs: To Kill events; ScenarioA1VehicleCloseCombat.cs | BacklogPass6Tests.ABurningWreckCarriesABlazeWhoseSmokeHindersAndGivesNoCover; BacklogPass7Tests; ScenarioA1Pass11Tests | Pass 45 | A Blaze is placed on a wreck burned by the IFT Vehicle line or a To Kill DR. A CC result of burning-wreck records the wreck as burning but places no Blaze entity, so it does not burn (fault). FT and MOL against vehicles are refused. No spread to terrain. | audit |
| B25.141 | Movement | 143 | built | GamePlanner.Wrecks.cs: BlazeEntryHalfMf, WreckEntryHalfMp | none found for the cost itself |  | One MF or MP more to enter a burning wreck's Location (ruling R6.3). The Heavy Winds exception has no wind to apply to. | audit |
| B25.15 | Flame | 143 | not built |  |  | Pass 44 | No Flame counter, no first-turn mark. | audit |
| B25.151 | Blaze Creation/Flame Extinguishing | 143 | not built |  |  | Pass 44 | No Flame to Blaze DR. | audit |
| B25.2 | Smoke | 143 | partly | GamePlanner.Wrecks.cs: VehicleHindrance, SmokeSources; GamePlanner.Fire.cs: FireMapFacts (Residual FP) | BacklogPass6Tests.ABurningWreckCarriesABlazeWhoseSmokeHindersAndGivesNoCover; ScenarioA1Pass6Tests | Pass 44 | A burning wreck's smoke is a +2 Hindrance and replaces the wreck Hindrance (ruling R6.3). Missing: a terrain Blaze's +3, the four-level height limit (the code counts it at any height), Mild Breeze and Heavy Winds, drifting Dispersed Smoke, the Fire Lane exception. | audit |
| B25.3 | (none) | 143 | not applicable |  |  |  | A negative statement: Fire changes no TEM. Nothing to implement. | audit |
| B25.4 | Entrance/Exit | 143 | not built |  |  | Pass 44 | No terrain Blaze, so no forced exit, elimination, or bar on entry (rulings R13.3, R10.7; backlog). | audit |
| B25.5 | Environmental Conditions | 143 | not built |  |  | Pass 56 | No EC state, no EC Chart dr; a card's EC SSR is shown and not enforced (ruling R17.10). | audit |
| B25.6 | Spreading Fire | 144 | not built |  |  | Pass 44 | No Spreading Fire DR or table. | audit |
| B25.61 | (none) | 144 | not built |  |  | Pass 44 | No elevation DRM. | audit |
| B25.62 | (none) | 144 | not built |  |  | Pass 44 | No attached-terrain or Wind Direction DRM. | audit |
| B25.63 | Wind Force | 144 | not built |  |  | Pass 56 | No Wind Force state or dr; a card's wind SSR is not enforced. | audit |
| B25.64 | Wind Direction | 144 | not built |  |  | Pass 56 | No Wind Direction. | audit |
| B25.65 | Wind Changes | 144 | partly | GamePlanner.Night.cs: WindChangeDue, AddWindChange; GameProjector.cs: ChangeWind | BacklogPass16Tests.TheWindChangeDrChangesTheNvr; BacklogPass16TablePlayerTests | Pass 56 | The DR is made at the start of a RPh only at night or in changing weather, never in the opening RPh, and serves only Chapter E (NVR, rain, snow). Missing: the DR in every game, and on a 2 the wind change dr and its table (ruling R16.10). | audit |
| B25.651 | Gusts | 145 | not built |  |  | Pass 56 | No Gust of this rule: a DR of 12 is not read. The Gust the code records is E3.4's (10 or more in Gusty weather); the state does not keep it and only the Play record page prints it. | audit |
| B25.66 | Collapse | 145 | not built |  |  | Pass 44 | No collapse on a Wind Change DR of 12. | audit |
| B25.7 | Extinguishing Attempts | 145 | not applicable |  |  |  | Cross-reference to B24.72 and B25.151, both not built. | audit |

#### B26 Wire (pages 145 to 146)

17 rows: 13 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B26 | Wire | 145 | not applicable |  |  |  | Heading. | audit |
| B26.1 | (none) | 145 | not built |  |  | Pass 38 | The kind asl:wire exists in the vocabulary only. Missing: setup limits (one per Location, forbidden terrain, not at Crest level), no effect on stacking, immovable. | audit |
| B26.2 | (none) | 145 | not applicable |  |  |  | Negative rule (Wire is no obstacle or Hindrance); holds today only because LOS reads no counter. Needs a test once Wire is read. | audit |
| B26.3 | (none) | 145 | not applicable |  |  |  | Negative rule (Wire has no TEM); nothing to implement beyond leaving the Location's TEM alone. | audit |
| B26.31 | (none) | 145 | not built |  |  | Pass 38 | Missing: +1 to attacks and CC by Infantry on Wire, +1 To Hit for ordnance, -1 to CC against them. | audit |
| B26.32 | Location | 145 | not built |  |  | Pass 38 | Missing: the above and beneath Wire positions as one Location; leader direction and rally across them. | audit |
| B26.4 | (none) | 145 | not built |  |  | Pass 38 | Missing: on-top placement at entry, the Wire Exit dr paid in MF, hung up, FFMO and FFNAM effects, the bars on Search, entrenching, Recovery, pillbox or vehicle entry. Only the A12.15 return to a fortification Location is refused (GamePlanner.cs: PlanConcealedEntryAsync, play.return-hazard; ConcealedEntryTests return-wire). | audit |
| B26.41 | Rout | 145 | not built |  |  | Pass 38 | Missing: Wire cost in rout, elimination or surrender when Wire stops a rout, Failure to Rout after a failed exit dr. | audit |
| B26.42 | (none) | 145 | not built |  |  | Pass 38 | Cavalry, motorcycles, horse-drawn vehicles, gliders, parachutes: none in the catalog and no logic. | audit |
| B26.43 | Vehicles | 146 | not built |  |  | Pass 38 | Missing: 4 MP plus COT (armored car, truck, halftrack) or 2 MP plus COT (fully-tracked) with a Bog Check. Backlog section 21 lists it. | audit |
| B26.44 | Bypass | 146 | not built |  |  | Pass 38 | Missing: no Infantry Bypass in a Wire hex; VBM pays the Wire MP and Bog once per hex. | audit |
| B26.45 | DC | 146 | not built |  |  | Pass 38 | Missing: no Place, Throw, or Set DC from on top of Wire. | audit |
| B26.46 | Double Time (CX) | 146 | not built |  |  | Pass 38 | Missing: no Double Time or Dash in a turn of a Wire exit attempt. | audit |
| B26.5 | Clearance | 146 | not applicable |  |  |  | Heading and cross-reference to Clearance B24.73 (another group; not built there as far as this audit saw). Inventory: The PDF outline points at page 145; the rule is printed on page 146. | audit |
| B26.51 | DC | 146 | not built |  |  | Pass 38 | Missing: Placed DC removes Wire on an Original KIA (DR 5 or less) from outside the Location; Set DC on a Final KIA. | audit |
| B26.52 | FFE | 146 | not built |  |  | Pass 38 | Needs OBA and Aerial bombs (C1, E7); neither built. | audit |
| B26.53 | Fully-Tracked Vehicles | 146 | not built |  |  | Pass 38 | Missing: fully-tracked vehicle removes Wire on a passed Bog Check with colored dr 1. Needs B26.43. | audit |

#### B27 Entrenchments (pages 146 to 147)

20 rows: 1 partly, 18 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B27 | Entrenchments | 146 | not applicable |  |  |  | Introductory definition of entrenchments; no rule text. | audit |
| B27.1 | Foxhole | 146 | not built |  |  | Pass 37 | The kind asl:foxhole and the in-fortification position exist in Units; no planner code reads them. Missing: allowed terrain, beneath or on top, 5/8 inch limit (mortar only), OB by squad capacity. | audit |
| B27.11 | Entrenching | 146 | not built |  |  | Pass 37 | Missing: Entrenching Attempt in the PFPh (Final DR 5 or less, +1 crew or HS, leadership, Labor), TI either way. Backlog section 26 names Entrenching as not built. | audit |
| B27.12 | Capacity | 146 | not built |  |  | Pass 37 | The attribute asl:capacity exists; no capacity check, no 1S to 2S to 3S replacement, no four-SMC allowance. | audit |
| B27.13 | Location | 146 | not built |  |  | Pass 37 | Missing: separate Location for Recovery and TEM only. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B27.2 | (none) | 146 | partly | LosCalculator.cs: CheckRailroadEmbankments and the hexside terrain rule (IsEntrenchment) | LosTests.EntrenchmentEndpointsUseTheHexsideRestrictionInBothDirections | Pass 37 | The wall or hedge LOS restriction is built for a hex whose printed map terrain is an entrenchment (VASL parity). A foxhole or trench counter is not read by LOS, and the planner refuses fire at such printed terrain. | audit |
| B27.3 | (none) | 146 | not built |  |  | Pass 37 | Missing: +4 TEM vs OVR and OBA, +2 vs other attacks, not cumulative with positive TEM, cumulative with Air Bursts. | audit |
| B27.4 | (none) | 146 | not built |  |  | Pass 37 | Missing: one MF more to go beneath or come from beneath, announced apart from the hex entry so Defensive First Fire can fall between. | audit |
| B27.41 | RtPh | 146 | not built |  |  | Pass 37 | Missing: Interdiction nullified by going beneath a foxhole with capacity; combined exit and entry MF in the RtPh. | audit |
| B27.42 | MPh | 147 | not built |  |  | Pass 37 | Missing: FFMO only between the hex entry MF and the foxhole MF. | audit |
| B27.43 | (none) | 147 | not built |  |  | Pass 37 | Missing: Movement Costs Chart costs of a foxhole hex for non-tracked vehicles and Cavalry (chart on the Chapter B divider, pages 160 to 161). | audit |
| B27.44 | (none) | 147 | not built |  |  | Pass 37 | Missing: entry beneath a foxhole holding the enemy; capacity per side. | audit |
| B27.5 | Trenches | 147 | not built |  |  | Pass 37 | The kind asl:trench exists in the vocabulary only. Inventory: The PDF outline points at page 146; the rule is printed on page 147. | audit |
| B27.51 | (none) | 147 | not built |  |  | Pass 37 | Missing: one per hex, not with a foxhole, three squads and four SMC, 5/8 inch counters fixed. | audit |
| B27.52 | (none) | 147 | not built |  |  | Pass 37 | Missing: vehicle beneath a trench is HD and may not change VCA or start. Needs D4 Hull Down. | audit |
| B27.53 | (none) | 147 | not built |  |  | Pass 37 | Missing: trench TEM kept against units in a connecting trench. | audit |
| B27.54 | (none) | 147 | not built |  |  | Pass 37 | Missing: connected trenches, 1 MF moves between them free of FFNAM, FFMO, Snap Shots, Interdiction, and mines; concealment kept. | audit |
| B27.55 | (none) | 147 | not built |  |  | Pass 37 | Missing: wheeled and halftrack barred; fully-tracked enters at COT with a Bog DR. | audit |
| B27.56 | Anti-Tank (A-T) Ditch | 147 | not built |  |  | Pass 37 | The trait asl:anti-tank-ditch exists in the vocabulary only. By SSR only. | audit |
| B27.6 | Lower Level Locations | 147 | not built |  |  | Pass 37 | Needs trenches, Crest status (B20.9), and Sangars (F8, Chapter F deferred). | audit |

#### B28 Minefields (pages 148 to 149)

29 rows: 2 partly, 23 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B28 | Minefields | 148 | not applicable |  |  |  | Heading. | audit |
| B28.1 | (none) | 148 | partly | UnitPlausibility.cs: Check (asl:minefield, asl:ap-strength) | EntityTests.ThePlausibilityCheckCoversSnipersAndMinefields | Pass 38 | Only the 6, 8, or 12 factor check on a counter document is built, as a warning. Missing: secret record, forbidden terrain, reveal on entry or Search, strength kept after attacks. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.2 | (none) | 148 | not applicable |  |  |  | Negative rule (no obstacle or Hindrance); holds because LOS reads no counter. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.3 | (none) | 148 | not built |  |  | Pass 38 | Missing: a minefield attack takes no TEM, FFMO, FFNAM, or FP modifier (half FP and +1 in Deep Snow). Backlog section 26 lists the Deep Snow part. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.4 | Anti-Personnel (A-P) Minefields | 148 | not applicable |  |  |  | Negative rule (no extra movement cost) and lead-in to B28.41. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.41 | (none) | 148 | not built |  |  | Pass 38 | Missing: the IFT attack on each entry and exit, reveal of the factors, the counter placed. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.411 | (none) | 148 | not built |  |  | Pass 38 | Missing: only the moving unit is attacked; a concealed unit at full strength, losing its "?" only if it breaks or is Reduced. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.412 | (none) | 148 | not built |  |  | Pass 38 | Missing: exit attack resolved in the hex left; a pinned or broken unit stays and takes no second attack. Inventory: The PDF outline points at page 147; the rule is printed on page 148. | audit |
| B28.413 | Rout | 148 | not built |  |  | Pass 38 | Missing: rout may avoid a known A-P minefield; a Reduced HS may rout on; Interdiction after the minefield attack. | audit |
| B28.42 | Vehicles | 148 | not built |  |  | Pass 38 | Missing: vehicles on the IFT, armored immobilized on a KIA only, 0 hull AF as unarmored, A-P before A-T. | audit |
| B28.43 | PRC | 148 | not built |  |  | Pass 38 | Missing: PRC immunity, survival rolls, Collateral attack, attack on unloading or Bail Out. Passengers are cargo today. | audit |
| B28.44 | Minefields in Building/Trench Hexes | 148 | not built |  |  | Pass 38 | Missing: no attack across a building hexside, in Trench movement, or on a Tower stairwell. | audit |
| B28.45 | Known Minefields | 148 | not built |  |  | Pass 38 | The trait asl:known-minefield exists in the vocabulary only. West of Alamein counters; candidate to leave out. | audit |
| B28.46 | (none) | 148 | not built |  |  | Pass 38 | Multi-hex Known minefield by arrow counters; table procedure a program can replace by naming hexes. | audit |
| B28.47 | Dummy Minefields | 148 | not built |  |  | Pass 38 | The trait asl:dummy-minefield exists in the vocabulary only. | audit |
| B28.48 | Hidden Mines | 149 | not built |  |  | Pass 38 | Marker use of Known Minefield counters for discovered hidden mines. | audit |
| B28.5 | Anti-Tank (A-T) Mines | 149 | partly | UnitPlausibility.cs: Check (asl:minefield, asl:at-strength) | EntityTests.ThePlausibilityCheckCoversSnipersAndMinefields | Pass 38 | Only the one to five factor check on a counter document is built, as a warning. Missing: the 3 A-P factors for 1 A-T exchange; only wagons and vehicles trigger. Inventory: The PDF outline points at page 148; the rule is printed on page 149. | audit |
| B28.51 | Attack | 149 | not built |  |  | Pass 38 | Missing: the dr against the factor count on vehicle entry or exit; Deep Snow; loss of "?". Inventory: The PDF outline points at page 148; the rule is printed on page 149. | audit |
| B28.52 | (none) | 149 | not built |  |  | Pass 38 | Missing: the 36+ column attack, Aerial AF as DRM (C7.12), Burning Wreck, elimination, Immobilization, PRC effects. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B28.53 | Placement | 149 | not built |  |  | Pass 38 | Missing: visible A-T Mines on bridge, paved road, ice, runway; removal by Infantry spending one MF. | audit |
| B28.531 | Daisy Chain | 149 | not built |  |  | Pass 38 | Daisy Chain, by SSR only; no counter kind exists. | audit |
| B28.6 | Clearance | 149 | not applicable |  |  |  | Pure cross-reference to Clearance B24.74 (another group). | audit |
| B28.61 | Trail Break (TB) | 149 | not built |  |  | Pass 38 | Needs Trail Breaks (B13.421, not built). | audit |
| B28.62 | FFE | 149 | not built |  |  | Pass 38 | Needs OBA and Aerial bombs (C1, E7). | audit |
| B28.7 | Flail Tanks | 149 | not built |  |  | Pass 38 | No Flail Tank in the catalog (five vehicles, none a flail). | audit |
| B28.71 | (none) | 149 | not built |  |  | Pass 38 | Needs B28.7. | audit |
| B28.72 | Flail Breakdown | 149 | not built |  |  | Pass 38 | Needs B28.7. | audit |
| B28.8 | Sappers | 149 | not built |  |  | Pass 38 | No Sapper designation; needs B24.74 Clearance. | audit |
| B28.9 | Booby Traps | 149 | not built |  |  | Pass 38 | Booby Traps: no capability record, no TC trigger. A12.154 casualties from Booby Traps are in the backlog (referee, pass 24). | audit |

#### B29 Roadblocks (page 150)

6 rows: 5 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B29 | Roadblocks | 150 | not applicable |  |  |  | Heading. | audit |
| B29.1 | (none) | 150 | not built |  |  | Pass 37 | The kind asl:roadblock carries a hexside in Units (UnitFacing.cs, EntityTests); no setup rule limits it to a road or runway Location. Inventory: The PDF outline points at page 149; the rule is printed on page 150. | audit |
| B29.2 | (none) | 150 | not built |  |  | Pass 37 | Missing: stone wall across the hexside, the extension to woods, building, or Rail Car center dots, Half-Level Obstacle to LOS. Inventory: The PDF outline points at page 149; the rule is printed on page 150. | audit |
| B29.3 | (none) | 150 | not built |  |  | Pass 37 | Missing: wall TEM; extension TEM for Direct Fire only. Inventory: The PDF outline points at page 149; the rule is printed on page 150. | audit |
| B29.4 | (none) | 150 | not built |  |  | Pass 37 | Missing: vehicles barred, Infantry cross as a wall, Hillside Wall when levels differ. A hexside terrain other than Wall or Hedge printed on a map is refused in movement (GamePlanner.Terrain.cs: WallOn), which would cover a printed roadblock; not checked against a board that prints one. Inventory: The PDF outline points at page 149; the rule is printed on page 150. | audit |
| B29.5 | Removal | 150 | not built |  |  | Pass 37 | Missing: Clearance (B24.76), removal by HE Final KIA, ordnance on the Infantry Target Type with +2, Indirect Fire, DC through the hexside. | audit |

#### B30 Pillboxes (pages 150 to 151)

27 rows: 23 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B30 | Pillboxes | 150 | not applicable |  |  |  | Heading. | audit |
| B30.1 | (none) | 150 | not built |  |  | Pass 37 | The kind asl:pillbox has a facing in Units (EntityTests); no setup terrain rule, no CA from the facing, no separate Location in the planner. | audit |
| B30.11 | (none) | 150 | not applicable |  |  |  | Heading that introduces the three Strength Factors. | audit |
| B30.111 | Stacking Capacity | 150 | not built |  |  | Pass 37 | The attribute asl:capacity exists; no stacking check, one Gun, no vehicle, no overstacking. | audit |
| B30.112 | CA Defense Modification | 150 | not built |  |  | Pass 37 | The attribute asl:ca-defense exists; no fire code reads it. | audit |
| B30.113 | NCA Defense Modification | 150 | not built |  |  | Pass 37 | The attribute asl:nca-defense exists; no fire code reads it. | audit |
| B30.12 | (none) | 150 | not built |  |  | Pass 37 | Inside and on top positions: the in-fortification position exists in Units and the planner does not read it. | audit |
| B30.2 | (none) | 150 | not built |  |  | Pass 37 | Missing: LOS only within the CA, ADJACENT across the CA hexsides, limits on fire into the own hex, no mortar or anti-air fire. | audit |
| B30.3 | (none) | 150 | not built |  |  | Pass 37 | Missing: pillbox TEM not cumulative with other TEM, cumulative with Hindrance and SMOKE. | audit |
| B30.31 | DC | 150 | not built |  |  | Pass 37 | Missing: DC modifier by the placing unit's hex, Thrown and Set DC cases. | audit |
| B30.32 | (none) | 150 | not built |  |  | Pass 37 | Missing: Infantry or Area Target Type only, no Target Size DRM, no Encirclement. | audit |
| B30.33 | (none) | 151 | not built |  |  | Pass 37 | Missing: entry and exit MF as a separate action; pinned or broken outside cannot enter. | audit |
| B30.34 | (none) | 151 | not built |  |  | Pass 37 | Missing: predesignated target Location, no SMOKE inside, WP CH, OVR has no effect. Inventory: The PDF outline points at page 150; the rule is printed on page 151. | audit |
| B30.35 | (none) | 151 | not built |  |  | Pass 37 | Missing: AP ignores the modifier when the Basic TK# is more than twice it; HE Equivalency (C8.31). Inventory: The PDF outline points at page 150; the rule is printed on page 151. | audit |
| B30.4 | Entry | 151 | not built |  |  | Pass 37 | Missing: one MF in, one MF out. Inventory: The PDF outline points at page 150; the rule is printed on page 151. | audit |
| B30.41 | RtPh | 151 | not built |  |  | Pass 37 | Missing: Interdiction as for foxholes (B27.41). Inventory: The PDF outline points at page 150; the rule is printed on page 151. | audit |
| B30.42 | (none) | 151 | not built |  |  | Pass 37 | Missing: no entry while an enemy ground unit is in the hex outside. | audit |
| B30.43 | (none) | 151 | not applicable |  |  |  | Negative rule (the pillbox does not affect entry of its hex); holds because counters are not read. | audit |
| B30.44 | (none) | 151 | not built |  |  | Pass 37 | Missing: no entry of an enemy-held pillbox; berserk stays in the hex. | audit |
| B30.45 | Guns | 151 | not built |  |  | Pass 37 | Missing: no 5/8 inch counter in or out except a dm SW; Gun HIP inside. | audit |
| B30.5 | Rout & Rally | 151 | not built |  |  | Pass 37 | Rulings R13.2 and R19.5 and backlog sections 2 and 23 record that pillboxes give no rally or rout benefit yet. | audit |
| B30.6 | Same Hex | 151 | not built |  |  | Pass 37 | Missing: ADJACENT inside and outside, exit barred by enemy in hex, CC without Hand-to-Hand or Melee, no vehicle CC. | audit |
| B30.7 | Concealment | 151 | not built |  |  | Pass 37 | Missing: Concealment Terrain with no halving and no TH DRM; contents Known within 16 hexes after an attack. | audit |
| B30.8 | Bunkers | 151 | not built |  |  | Pass 37 | Bunkers; needs trenches (B27.54). | audit |
| B30.9 | Elimination & Control | 151 | not applicable |  |  |  | Heading. | audit |
| B30.91 | Control | 151 | not built |  |  | Pass 37 | Hex Control does not ask for the pillbox Location (ScenarioVictory has no pillbox reading). | audit |
| B30.92 | Elimination | 151 | not built |  |  | Pass 37 | Missing: elimination by DC or ordnance KIA against the TEM, +2 for Placed or Set DC, Indirect Fire CH of 70mm (100mm gray), Falling Rubble, Bombardment. | audit |

#### B31 Village Terrain (pages 151 to 153)

21 rows: 1 partly, 16 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B31 | Village Terrain | 151 | not applicable |  |  |  | Heading. | audit |
| B31.1 | Narrow Street | 151 | not applicable |  |  |  | Definition of the map depiction (a road printed on a hexside). The map read has no narrow street fact. | audit |
| B31.11 | Movement | 152 | not built |  |  | Pass 61 | Needs one-lane rules (B6.43), Bypass along the hexside for Infantry and VBM at half cost, no Dash or rout along it. | audit |
| B31.12 | Movement Restrictions | 152 | not applicable |  |  |  | Heading. | audit |
| B31.121 | TCA | 152 | not built |  |  | Pass 61 | Needs TCA against VCA and Barrel Length on the vehicle counter. Inventory: The PDF outline points at page 151; the rule is printed on page 152. | audit |
| B31.122 | TCA Change | 152 | not built |  |  | Pass 61 | TCA-Change dr of 3 or less with five drm; two attempts per phase; 2 MP per failure. | audit |
| B31.123 | VCA Change | 152 | not built |  |  | Pass 61 | Motorcycles and very small vehicles; none in the catalog. | audit |
| B31.124 | Towing | 152 | not built |  |  | Pass 61 | Towing in VBM; trailers are not in the catalog. | audit |
| B31.125 | (Un)loading | 152 | not built |  |  | Pass 61 | (Un)loading into either hex of the hexside. | audit |
| B31.126 | Rubble/Blaze/Wreck | 152 | not built |  |  | Pass 61 | Bypass barred by another vehicle or wreck, a Blaze, or Rubble; no wreck removal. | audit |
| B31.13 | Attack Effects | 152 | not applicable |  |  |  | Heading. | audit |
| B31.131 | Smoke/Residual FP | 152 | not built |  |  | Pass 61 | SMOKE and Residual FP of either hex on a Bypassing unit. | audit |
| B31.132 | CC | 152 | not built |  |  | Pass 61 | Needs Street Fighting (A11.8, not built). | audit |
| B31.14 | Fortifications | 152 | not built |  |  | Pass 61 | Setup permission for Fortifications in a paved Narrow Street hex. | audit |
| B31.141 | Roadblock | 152 | not built |  |  | Pass 61 | Roadblock straddling a hexside; needs B29. | audit |
| B31.1411 | Removal | 153 | not built |  |  | Pass 61 | Needs B29.5 and B31.141. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B31.142 | Mines | 153 | not built |  |  | Pass 61 | Needs B28. | audit |
| B31.15 | Rubble | 153 | not built |  |  | Pass 61 | Needs Falling Rubble (B24.121), B29, B28. | audit |
| B31.2 | Steeple | 153 | not built |  |  | Pass 61 | No Steeple fact in the map read; no extra building Location. | audit |
| B31.21 | Stacking | 153 | not built |  |  | Pass 61 | One HS-equivalent stacking; no Gun. | audit |
| B31.3 | Single-Hex Two-Story House | 153 | partly | GamePlanner.cs: OrdinaryBuildings; GamePlanner.Terrain.cs: InfantryStep; LosCalculator.cs: terrain height rule | none found | Pass 42 | No logic of its own. The ordinary building code admits a one-level building hex, moves between levels only where the map marks a stairwell, and LOS adds the half level. Not settled: whether a map read marks the inherent stairwell of a single-hex house (boards 45 and 46 were not checked). | audit |

#### B32 Railroads (pages 153 to 155)

28 rows: 1 partly, 2 refused, 22 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B32 | Railroads | 153 | not applicable |  |  |  | Heading. | audit |
| B32.1 | Railroad Types | 153 | not applicable |  |  |  | Definitions of the four RR types and the RR hexside. The embankment half level is judged at B32.12. | audit |
| B32.11 | (none) | 153 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: terrain guard; GamePlanner.Fire.cs: FireMapFacts | none found | Pass 61 | A hex whose terrain is not on the admitted list cannot be entered ("is not a reviewed entry") or fired at ("has no TEM"); no Railroad name is on the list. No paved-road treatment with the listed exceptions is built. | audit |
| B32.12 | (none) | 153 | partly | LosCalculator.cs: SourceEmbankmentAdjustment, TargetEmbankmentAdjustment, terrain height rule | LosLeftoverTests (railroad-terrain, railroad-hexside fixtures) | Pass 61 | LOS adds the half level for "Railroad, Embankment" as VASL does. Hillock TEM and COT (F6) are not built; entry and fire are refused. | audit |
| B32.13 | (none) | 153 | not built |  |  | Pass 61 | Overlay joins and same-level LOS along one railroad; no overlay support seen for RR. | audit |
| B32.14 | RR Bridges | 153 | not built |  |  | Pass 61 | Needs bridges (B6) and depressions (B19 to B21). | audit |
| B32.2 | Other Terrain | 153 | not built |  |  | Pass 61 | Other terrain in a RR hex exists by SSR or future overlays only, as the rule itself says. | audit |
| B32.21 | (none) | 153 | not built |  |  | Pass 61 | Needs B32.2. | audit |
| B32.211 | (none) | 154 | not built |  |  | Pass 61 | Needs B32.2 and the woods-road principles. | audit |
| B32.3 | Movement | 154 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.VehicleTerrain.cs: terrain guard | none found | Pass 61 | Entry of a RR hex is refused by the terrain list. The Railroad Movement Costs Chart (Chapter B divider, pages 160 to 161) is not in the code. | audit |
| B32.31 | Bypass | 154 | not built |  |  | Pass 61 | Bypass of a woods-RR hex. | audit |
| B32.32 | Elevation Changes | 154 | not built |  |  | Pass 61 | Half-level elevation change across a RR hexside. | audit |
| B32.33 | Bog | 154 | not built |  |  | Pass 61 | Bog entering an ElRR hex across a non-RR hexside. | audit |
| B32.4 | Rr Crossings | 154 | not built |  |  | Pass 61 | RR Crossings; the choice of road or non-RR hexside cost. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B32.41 | Ground Level RR Crossing | 154 | not built |  |  | Pass 61 | Road rate across the road hexside of a ground-level crossing. | audit |
| B32.42 | Embankment Rr Crossing | 154 | not built |  |  | Pass 61 | Road and RR both at level one half; no elevation cost by the road. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B32.43 | Elevated Rr Crossing | 154 | not built |  |  | Pass 61 | Needs bridges (B6). Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B32.44 | Sunken Rr Crossing | 154 | not built |  |  | Pass 61 | Needs bridges (B6) and Sunken Roads (B4). Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| B32.5 | Rail Cars | 154 | not applicable |  |  |  | Definition (a printed Rail Car depiction makes a Rail Car hex). The map read has no such fact. | audit |
| B32.51 | Terrain | 154 | not built |  |  | Pass 61 | Level 1 obstacle, +2 TEM, Kindling 8, Spread 9, Concealment Terrain. | audit |
| B32.511 | HEAT | 154 | not built |  |  | Pass 61 | Needs HEAT against Infantry (C8.31). | audit |
| B32.52 | Movement | 154 | not built |  |  | Pass 61 | 2 MF; Bypass as a building. | audit |
| B32.53 | Guns | 155 | not built |  |  | Pass 61 | Gun limits by Target Size. | audit |
| B32.54 | CA Change & Fire Within Hex | 155 | not built |  |  | Pass 61 | Case A and Case E doubled. | audit |
| B32.55 | Fortifications | 155 | not built |  |  | Pass 61 | Fortification placement as in a GLRR hex. | audit |
| B32.56 | Ambush & Street Fighting | 155 | not built |  |  | Pass 61 | Needs Street Fighting (A11.8). | audit |
| B32.57 | Wrecked Rail Car | 155 | not built |  |  | Pass 61 | Wrecked Rail Car; needs HE of 70mm or more on the IFT and Bombardment. | audit |
| B32.6 | Rail Car Counters | 155 | not built |  |  | Pass 61 | No Rail Car counter kind in the vocabulary. | audit |

#### B33 Stream-Hex Terrain (page 155)

5 rows: 4 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B33 | Stream-Hex Terrain | 155 | not applicable |  |  |  | Heading. | audit |
| B33.1 | Stream-Woods/Brush/Orchard | 155 | not built |  |  | Pass 41 | Board 47 and the KGP maps only. Not settled: how a stream-woods hex reads in the map (see findings part 3). | audit |
| B33.11 | Entry | 155 | not built |  |  | Pass 41 | Needs streams (B20), not built. Inventory: The PDF outline points at page 154; the rule is printed on page 155. | audit |
| B33.12 | Crest | 155 | not built |  |  | Pass 41 | Needs Crest status (B20.9). | audit |
| B33.13 | TEM | 155 | not built |  |  | Pass 41 | Needs LOS within the stream depiction and Crest. | audit |

#### B34 Towers (pages 155 to 156)

13 rows: 1 partly, 3 refused, 7 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B34 | Towers | 155 | not applicable |  |  |  | Heading. | audit |
| B34.1 | (none) | 155 | not applicable |  |  |  | Definition of a Tower and its depiction (Pegasus Bridge map art). | audit |
| B34.2 | (none) | 156 | partly | LosCalculator.cs: terrain height rule ("Tower Hindrance"); VaslMapBuilder.cs: tower terrain names | none found | Pass 61 | LOS treats a printed Tower Hindrance as a Hindrance and a Tower Obstacle as an obstacle at the terrain's own height. No SSR sets height or kind, and no Tower Location exists. | audit |
| B34.21 | Stacking | 156 | not built |  |  | Pass 61 | One HS-equivalent and 5 PP in the Tower Location. | audit |
| B34.3 | TEM | 156 | refused | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 61 | Fire at a hex whose terrain is a Tower is refused ("has no TEM"). Ground-level other-terrain TEM, 0 TEM in the Tower Location, and no FFMO are not built. | audit |
| B34.31 | Air Bursts | 156 | not built |  |  | Pass 61 | Needs Air Bursts by level. | audit |
| B34.4 | Movement | 156 | refused | GamePlanner.Terrain.cs: InfantryStep | none found | Pass 61 | Entry is refused by the terrain list. 1 MF plus COT, Cavalry by Bypass only, and the Gun bars are not built. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B34.41 | (none) | 156 | refused | GamePlanner.VehicleTerrain.cs: terrain guard | none found | Pass 61 | Vehicle entry is refused by the terrain list. VBM entry and the B23.41 entry are not built. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B34.42 | (none) | 156 | not built |  |  | Pass 61 | Stairwell to the Tower Location; Scaling (B23.424). Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B34.43 | (none) | 156 | not built |  |  | Pass 61 | Quasi-Location attacks on the stairwell. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B34.5 | Concealment | 156 | not built |  |  | Pass 61 | Concealment as a Rooftop (B23.82). Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B34.6 | Rout | 156 | not built |  |  | Pass 61 | Not a building for rout. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B34.7 | (none) | 156 | not built |  |  | Pass 61 | Building Location for Victory purposes. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |

#### B35 Light Woods (page 156)

4 rows: 1 partly, 2 refused, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B35 | Light Woods | 156 | not applicable |  |  |  | Heading. | audit |
| B35.1 | (none) | 156 | refused | GamePlanner.Terrain.cs: InfantryStep; GamePlanner.Fire.cs: FireMapFacts | none found | Pass 60 | A printed Light Woods hex cannot be entered or fired at. No SSR turns woods into Light Woods; the Sighting TC DRM needs E7. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B35.2 | Hindrance | 156 | partly | LosCalculator.cs: terrain height rule and the hindrance value (2 for Light Woods) | LosTests (the grain and Light Woods case, line 234) | Pass 60 | LOS gives +2 per hex and no obstacle, as VASL does. Fire then refuses the attack as a Hindrance not attributed (GamePlanner.Fire.cs, fire.hindrance-unattributed). The Fire Lane DRM is not built. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |
| B35.3 | Vehicles | 156 | refused | GamePlanner.VehicleTerrain.cs: terrain guard | none found | Pass 60 | Vehicle entry is refused by the terrain list. One third of the MP allotment with a Bog Check +1 is not built. Inventory: The PDF outline points at page 155; the rule is printed on page 156. | audit |

#### B36 Prepared Fire Zone (PFZ) (pages 156 to 157)

10 rows: 7 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B36 | Prepared Fire Zone (PFZ) | 156 | not applicable |  |  |  | Heading. | audit |
| B36.1 | (none) | 156 | not built |  |  | Pass 61 | No PFZ counter kind, no OB field for PFZ factors. | audit |
| B36.2 | PFZ Creation | 156 | not built |  |  | Pass 61 | Conversion of terrain in the setup area by counter. | audit |
| B36.21 | (none) | 156 | not built |  |  | Pass 61 | Woods to vineyard. Vineyard (B12.7) is not on the admitted terrain list. | audit |
| B36.22 | (none) | 156 | not built |  |  | Pass 61 | Brush, orchard, grain and others to Open Ground. | audit |
| B36.23 | Gullies & Streams | 157 | not built |  |  | Pass 61 | Needs gullies and streams (B19, B20, B33). | audit |
| B36.3 | Paths & Roads | 157 | not built |  |  | Pass 61 | Paths removed, roads kept. | audit |
| B36.4 | Walls, Hedges, & Bocage | 157 | not applicable |  |  |  | Negative rule (hexsides unaffected). | audit |
| B36.5 | Water Obstacles | 157 | not applicable |  |  |  | Negative rule (Water Obstacles unaffected). | audit |
| B36.6 | DYO Cost | 157 | not built |  |  | Pass 61 | DYO cost; Chapter H is deferred. | audit |

#### B37 Debris (pages 157 to 158)

15 rows: 14 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| B37 | Debris | 157 | not applicable |  |  |  | Heading. | audit |
| B37.1 | (none) | 157 | not built |  |  | Pass 61 | No Debris counter kind in the vocabulary; Inherent Terrain and Concealment Terrain not built. | audit |
| B37.2 | TEM | 157 | not built |  |  | Pass 61 | +1 TEM and Half-Level Hindrance through the whole hex. | audit |
| B37.3 | MF/MP | 157 | not built |  |  | Pass 61 | 2 MF; fully-tracked AFV only at a quarter of its MP with a Bog Check +1. | audit |
| B37.4 | Fortifications | 157 | not built |  |  | Pass 61 | Setup permission. | audit |
| B37.5 | Creation | 157 | not built |  |  | Pass 61 | By SSR only. Needs Falling Rubble (B24.12, not built). | audit |
| B37.6 | Terrain | 157 | not built |  |  | Pass 61 | Debris supersedes the terrain it falls on. | audit |
| B37.61 | Debris-Road Hexes | 157 | not built |  |  | Pass 61 | Road treated as absent. | audit |
| B37.62 | Debris-Wide City Boulevards | 157 | not built |  |  | Pass 61 | Needs Runways and Wide City Boulevards (B7, not built). | audit |
| B37.621 | TEM | 157 | not built |  |  | Pass 61 | Needs B7.3. | audit |
| B37.63 | Debris-Bridge Hexes | 157 | not built |  |  | Pass 61 | Needs bridges (B6). | audit |
| B37.64 | Falling Debris Resolution | 158 | not built |  |  | Pass 61 | 1MC, Bog Check, Immobilization, SW malfunction. | audit |
| B37.641 | Multiple Falling Debris Occurences | 158 | not built |  |  | Pass 61 | Repeat of B37.64 on each further fall; no other cumulative effect. | audit |
| B37.7 | Clearance | 158 | not built |  |  | Pass 61 | Needs Clearance (B24.7) and Trail Breaks. | audit |
| B37.8 | Flame/Blaze | 158 | not built |  |  | Pass 61 | Needs Fire (B25) to bar spread into debris. | audit |

### Chapter C: Ordnance and Offboard Artillery

#### C. Introduction (page 162)

12 rows: 5 built, 1 built with a deviation, 2 partly, 1 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C.1 | Indirect Fire | 162 | not applicable |  |  |  | Scope statement: onboard ordnance uses Direct Fire To Hit only. The code has no onboard Indirect Fire, which agrees. | audit |
| C.2 | Ordnance | 162 | not applicable |  |  |  | Definition of ordnance. Its IFE clause waits on C2.29, which is not built. | audit |
| C.3 | To Hit/Effects DRM | 162 | built | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1FireCalculator.cs: Arithmetic | ScenarioA1OrdnanceTests.AHitAttacksTheLocationOnTheGunsHeColumnWithTheTemOnTheToHitDr |  | TEM and Hindrance go on the TH DR for the Infantry and Vehicle Target Types and TEM on the Effects DR for the Area Target Type, never both; the CH exception is C3.71. | audit |
| C.4 | Ordnance Area Fire | 162 | built | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1FireCalculator.cs: Arithmetic | ScenarioA1OrdnanceTests.EachShotAcquiresTheLocationToMinusTwoAndAConcealedOneOnlyWhenItLosesConcealment |  | FP never halved; Case K for a concealed target, Case B in the AFPh, Case D for a pinned firer. The LATW in a stream clause has no terrain to reach it. | audit |
| C.5 | Vertex Aiming Point | 162 | built with a deviation | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 52 | Ruling R10.7: LOS to a Bypassing unit is traced to the hex center and must cross a Bypassed hexside; the vertex LOS is not built and a hex with a wall or hedge is refused. Ordnance Snap Shots are not built. | audit |
| C.5A | Range | 162 | built | GamePlanner.Ordnance.cs: OrdnanceMapFacts | none found |  | Range is always read hex to hex, to the hex holding the target. No test of an ordnance shot at a Bypassing unit was found. | audit |
| C.5B | Fire Within CA | 162 | built | GamePlanner.Ordnance.cs: OrdnanceMapFacts | OrdnanceStepsTests.AShotOutsideTheCoveredArcTurnsTheGunWithCaseAAndALowerRof |  | The CA is judged on the bearing to the target's hex center, never on a vertex. Not tested against a Bypassing target. | audit |
| C.5C | (none) | 162 | partly | GamePlanner.Fire.cs: FireMapFacts | none found | Pass 52 | The Bypassed obstacle's hex is the target hex. Snap Shot by ordnance, Rowhouse Bypass, Underbelly Hit, and Climbing targets are not built. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C.6 | HE Use on IFT | 162 | partly | ScenarioA1OrdnancePackage.cs: HeFirepower; ScenarioA1OrdnanceCalculator.cs: HitAttack | ScenarioA1OrdnanceTests.AHitAttacksTheLocationOnTheGunsHeColumnWithTheTemOnTheToHitDr | Pass 39 | Built for Guns, tanks and light mortars from the IFT column headers. The OBA half of the rule is not built (no OBA). | audit |
| C.7 | Heavy Payload | 162 | not built |  |  | Pass 54 | No -1 per 50mm over 200mm, no bonus DRM for CH FP over 36. Unreachable today (largest catalog caliber 76mm); a 100mm or larger Gun would resolve a CH on the 36 column with no bonus, silently. | audit |
| C.8 | Moving Vehicular Target | 162 | built | GameProjector.cs: MovedVehicles fold; LiveOrdnance.cs: VehicleTargetFacts | ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr |  | Moving when it entered a hex or spent MP under Motion this Player Turn, or is in Motion; other MP expenditures do not count. | audit |
| C.9 | Undeclared Ordnance Target Type | 162 | not applicable |  |  |  | The action always fixes the Target Type (a named vehicle, a mortar's Area, otherwise Infantry; ruling R7.2), so an undeclared shot cannot occur. | audit |

#### C1 Offboard Artillery (pages 163 to 167)

52 rows: 1 refused, 50 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C1 | Offboard Artillery | 163 | not applicable |  |  |  | heading | audit |
| C1.1 | (none) | 163 | not built |  |  | Pass 39 | No battery, module, or radio-to-battery link. The catalog has no radio definition; only the vocabulary kind asl:radio and a glyph exist. | audit |
| C1.2 | Radio Contact Attempt | 163 | not built |  |  | Pass 39 | Radio Contact DR, Observer eligibility, once per PFPh or DFPh, dated Contact values. | audit |
| C1.21 | Battery Access | 163 | not built |  |  | Pass 39 | Battery Access draws, the extra draw for unKnown enemy units by the AR, loss of Access on the second red chit. | audit |
| C1.211 | Draw Pile | 163 | not built |  |  | Pass 39 | Draw Pile by nationality (A25 OBA ACCESS column), extra chits for Pre-Registration, Plentiful and Scarce Ammunition. | audit |
| C1.22 | Maintaining Radio Contact | 163 | not built |  |  | Pass 39 | Maintenance DR and its DRM, radio breakdown on 12 and repair, involuntary and voluntary loss of Contact. | audit |
| C1.23 | Field Phones | 163 | not built |  |  | Pass 58 | Field Phone: Contact on 11 or less, immobile, unrepairable, Security Area and line cutting. | audit |
| C1.3 | Artillery Request (AR) | 163 | not built |  |  | Pass 39 | AR placement in the Observer's LOS, Accuracy dr (2 or less for British, German, U.S.; 1 or less for others), SR placement. | audit |
| C1.31 | Direction/Extent of Error | 164 | not built |  |  | Pass 39 | Direction and Extent of Error DR along a Hex Grain. | audit |
| C1.32 | Blast Height/Area | 164 | not built |  |  | Pass 39 | Blast Height for Observer LOS, seven-hex Blast Area, offboard hexes excluded. | audit |
| C1.321 | Offboard SR/FFE | 164 | not built |  |  | Pass 39 | SR or FFE landing offboard; needs a notion of hexes beyond the map edge. | audit |
| C1.322 | End of Actions | 164 | not built |  |  | Pass 39 | End of the Observer's and battery's actions after placing or Correcting a SR. | audit |
| C1.33 | SR & FFE:1/2 Options | 164 | not built |  |  | Pass 39 | The mandatory choice among 1.331 to 1.337 at the start of each friendly PFPh and DFPh. | audit |
| C1.331 | (none) | 164 | not built |  |  | Pass 39 | Leave or Correct with LOS to the Blast Height. | audit |
| C1.332 | (none) | 164 | not built |  |  | Pass 39 | Predesignated Correct and Convert to FFE:1. | audit |
| C1.333 | (none) | 164 | not built |  |  | Pass 39 | Convert a SR to FFE:1 in place; needs Known-to-Observer units. | audit |
| C1.334 | (none) | 164 | not built |  |  | Pass 39 | Leave a FFE in place for resolution. | audit |
| C1.335 | (none) | 164 | not built |  |  | Pass 39 | Forced Correct or Cancel with no LOS. | audit |
| C1.336 | (none) | 164 | not built |  |  | Pass 39 | Voluntary Cancel of a SR and a new AR. | audit |
| C1.337 | (none) | 164 | not built |  |  | Pass 39 | Voluntary Cancel of a FFE. | audit |
| C1.34 | FFE:C | 164 | not built |  |  | Pass 39 | FFE:C counter, forced Access draw, end of the previous Fire Mission. | audit |
| C1.341 | (none) | 164 | not built |  |  | Pass 39 | Replace FFE:C with a SR. | audit |
| C1.342 | (none) | 164 | not built |  |  | Pass 39 | Convert FFE:C to FFE:1. | audit |
| C1.343 | (none) | 164 | not built |  |  | Pass 39 | Remove FFE:C and place a new AR. | audit |
| C1.35 | Cancelled SR/FFE | 164 | not built |  |  | Pass 39 | Effects of Cancelling a SR or FFE on the Fire Mission and Battery Access. | audit |
| C1.4 | Correcting OBA | 164 | not built |  |  | Pass 39 | Correction up to 18 hexes (SR) or 3 hexes (FFE), limited Extent of Error. | audit |
| C1.5 | FFE Resolution | 164 | not built |  |  | Pass 39 | FFE:1, FFE:2, FFE:C sequence; an IFT DR per Blast Area hex against all units, friendly ones too; attacks on empty hexes. | audit |
| C1.51 | Entering a FFE | 165 | not built |  |  | Pass 39 | Attack on units entering or becoming more vulnerable in a Blast Area hex in the MPh, RtPh, APh, CCPh. | audit |
| C1.52 | TEM | 165 | not built |  |  | Pass 39 | Indirect Fire TEM rules; needs B13.3 Air Bursts (built for mortars only) and B23.32 upper levels. | audit |
| C1.53 | CH | 165 | not built |  |  | Pass 39 | CH on an Original 2 FFE DR. | audit |
| C1.54 | Friendly Units | 165 | not built |  |  | Pass 39 | Morale one lower for friendly units in a friendly FFE Blast Area. | audit |
| C1.55 | vs a Vehicle | 165 | refused | GamePlanner.Ordnance.cs: PlanFireOrdnance | none found | Pass 50 | An Area Target Type shot at a hex holding a vehicle is refused (play.ordnance-area-vehicle; ruling R9.3). The whole Indirect Fire against vehicles procedure and its three DRM are not built. | audit |
| C1.56 | vs Concealed Units | 165 | not built |  |  | Pass 39 | FFE not halved against concealed units. | audit |
| C1.57 | FFE LOS Hindrance | 165 | not built |  |  | Pass 39 | FFE as a two-level +1 LOS Hindrance; the LOS calculator has no OBA input. | audit |
| C1.6 | Observer | 165 | not built |  |  | Pass 39 | Observer rules: CE in an AFV, concealed units in non-Concealment Terrain Known to him, one radio per phase, no other action, no move in the MPh. | audit |
| C1.61 | (none) | 165 | not built |  |  | Pass 39 | Loss of Contact when the Observer is not in Good Order. | audit |
| C1.62 | Vision Effects | 165 | not built |  |  | Pass 39 | Hindrance and SMOKE drm on the Accuracy dr. | audit |
| C1.63 | Offboard Observer | 166 | not built |  |  | Pass 58 | Offboard Observer by SSR: fixed hex and level, automatic Contact, Accuracy 1 or less. | audit |
| C1.7 | Fire Missions | 166 | not built |  |  | Pass 58 | Fire Mission types and the announcement of the type. | audit |
| C1.71 | Smoke | 166 | not built |  |  | Pass 58 | SMOKE and WP FFE; needs ordnance SMOKE (A24.2 to A24.8, C8.5) and weather limits. | audit |
| C1.72 | Harassing Fire | 166 | not built |  |  | Pass 58 | Harassing Fire: 19-hex Blast Area at one third FP. | audit |
| C1.73 | Pre-Registered Fire | 166 | not built |  |  | Pass 58 | Pre-Registered hexes recorded before the opponent's setup; needs a card SSR token. | audit |
| C1.731 | (none) | 166 | not built |  |  | Pass 58 | FFE:1 placed without a SR on a Pre-Registered hex. | audit |
| C1.732 | (none) | 166 | not built |  |  | Pass 58 | Accuracy on an Original dr of 4 or less; Extent of Error halved. | audit |
| C1.733 | (none) | 166 | not built |  |  | Pass 58 | Pre-Registration with any Fire Mission, bound to its Observer and battery. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C1.8 | Bombardment | 166 | not built |  |  | Pass 58 | Bombardment by SSR or DYO only; no card token for it. | audit |
| C1.81 | Area | 166 | not built |  |  | Pass 58 | Bombardment area and the six dr that spare grid coordinates. | audit |
| C1.82 | Effects | 166 | not built |  |  | Pass 58 | 2MC on Personnel with reversed TEM, Casualty Reduction on Doubles, reveal of hidden units. | audit |
| C1.821 | Equipment | 167 | not built |  |  | Pass 58 | NMC of vehicles, Guns and SW with their Morale Levels and results. | audit |
| C1.822 | Terrain | 167 | not built |  |  | Pass 58 | NMC of buildings, bridges, pillboxes, wire, roadblocks and minefields; rubble by level. Needs B24 rubble creation and Fortifications. | audit |
| C1.823 | Fire/Shellholes | 167 | not built |  |  | Pass 58 | Shellhole or Flame on an Original 12. | audit |
| C1.9 | Rocket OBA | 167 | not built |  |  | Pass 58 | Rocket OBA: no SR, automatic Error, Harassing Blast Area at full FP, one Fire Mission. | audit |

#### C2 Gun Classification (pages 167 to 169)

22 rows: 8 built, 6 partly, 1 refused, 3 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C2 | Gun Classification | 167 | not applicable |  |  |  | heading | audit |
| C2.1 | Guns/SW | 167 | built | LiveOrdnance.cs: FromState; ScenarioA1OrdnanceCalculator.cs: Outside | OrdnanceStepsTests.U28AGunFiresHeAtASquadKeepsItsRofAndItsAcquisitionLowersTheNextToHitDr; OrdnanceStepsTests.ASquadMansAGunWithCaseH |  | A Gun fires only manned; a squad or HS mans it with Case H; SW are fired by their possessor. The U.S. RCL exception has no counter. | audit |
| C2.2 | Counters | 167 | not applicable |  |  |  | Describes the counters and their reverse sides. | audit |
| C2.21 | Gun Caliber Size | 167 | partly | ScenarioA1OrdnancePackage.cs: Gun; ScenarioA1ArmorCalculator.cs: Outside; LiveOrdnance.cs: FromState | ScenarioA1Pass7Tests.SpecialAmmunitionNeedsItsYear | Pass 54 | Caliber, no-AP, no-HE and suffix built. Missing: the default vs an unarmored vehicle is AP, not HE; the limbered caliber exception (C10.24) is not built. | audit |
| C2.22 | Gun Type | 167 | partly | ScenarioA1OrdnanceCalculator.cs: Outside (AdmittedGunTypes) | ScenarioA1OrdnanceTests.ShotsOutsideTheReviewAreAbstained | Pass 54 | AT, INF and ART Guns fire at any target. MTR Guns on 5/8 inch counters, RCL and AA are refused as gun-outside; none is in the catalog. | audit |
| C2.23 | Facing | 167 | built | GamePlanner.Ordnance.cs: OrdnanceMapFacts; UnitFacing.cs | ScenarioA1OrdnanceTests.RefereeFindingsOnTheCoveredArcTargetsAndAcquisition |  | A Gun's facing is one of six hexspines; a Gun with none is refused. | audit |
| C2.24 | Rate of Fire (ROF) | 167 | built | ScenarioA1OrdnanceCalculator.cs: Run, Counter, Outside; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.TheColoredDrKeepsTheRofUnlessTheGunTurnedTheCrewIsPinnedOrItIsTheAfph |  | Colored dr against the ROF, fire counter otherwise, MF or MP limit in Defensive First Fire. MG TH DR, IFE and Bounding First Fire uses are not built (other rules). | audit |
| C2.2401 | Gun Duels | 167 | not built |  |  | Pass 33 | Gun Duels; needs Bounding First Fire by ordnance (D3.3, C5.3). Today the DEFENDER's shot resolves alone (ruling R8.1). | audit |
| C2.241 | First/Final Fire | 168 | built | ScenarioA1OrdnanceCalculator.cs: Outside (mayFire) | ScenarioA1Pass8Tests.AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire; BacklogPass8Tests.AGunThatUsedItsRofFiresOnceMoreAsIntensiveFire |  | Ruling R8.2. The OVR Prevention exception (C5.64) and the IFE clause are not built. | audit |
| C2.25 | Range Limit | 168 | built | ScenarioA1OrdnanceCalculator.cs: Outside | ScenarioA1OrdnanceTests.ShotsOutsideTheReviewAreAbstained |  | Maximum and minimum range from the catalog; a shot beyond is out-of-range. | audit |
| C2.26 | Special Ammo | 168 | partly | ScenarioA1ArmorCalculator.cs: Depletion, Outside | ScenarioA1Pass7Tests.ApcrFollowsItsDepletionNumber | Pass 54 | APCR (A) and HEAT (H) by listed code letter and year. Smoke (s), WP, Canister, APDS (D), IR and HE Depletion are not read. | audit |
| C2.27 | Manhandling Number (M#) | 168 | built | GamePlanner.Guns.cs: PushPlan, PlanHookGun | BacklogPass8Tests.ACrewPushesItsGunOrStaysOnAHighManhandlingDr; BacklogPass8Tests.ATruckHooksUpAGunTowsItAndUnhooksIt |  | The M# is read for Pushing and against the T# for towing. | audit |
| C2.271 | Gun Target Size | 168 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-p) | ScenarioA1Pass8Tests.AnEmplacedGunAddsItsTargetSizeAndEmplacementToTheToHitDr |  | Small +1, large -1 on a shot at a Gun whose crew is alone in the Location. | audit |
| C2.28 | Breakdown | 168 | partly | ScenarioA1OrdnanceCalculator.cs: Breakdown, Run; GamePlanner.Ordnance.cs: AddOrdnanceEvents | ScenarioA1OrdnanceTests.AnOriginalTwelveMalfunctionsTheGunAndLosesTheAcquisition | Pass 54 | Malfunction on an Original DR at or above the B# is built. Repair of a Gun or a tank's MA is not: the catalog Guns carry no R# or X#, and PlanRepair accepts only a possessed SW; so permanent malfunction and removal never happen. | audit |
| C2.29 | Infantry Firepower Equivalent (IFE) | 168 | not built |  |  | Pass 53 | IFE. The catalog attribute exists and is empty on both Guns; no fire path reads it. | audit |
| C2.3 | 360 Mount | 168 | refused | ScenarioA1OrdnanceCalculator.cs: Outside | none found | Pass 53 | A Gun with the asl:mount-360 trait is gun-outside. No catalog Gun has one. Needs Case A as a T weapon. | audit |
| C2.4 | Movement/Fire Limitations | 168 | not applicable |  |  |  | Cross-reference to C10 and A4.41 for RFNM, NM and QSU. | audit |
| C2.5 | Conditional ROF | 168 | partly | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run | ScenarioA1OrdnanceTests.TheColoredDrKeepsTheRofUnlessTheGunTurnedTheCrewIsPinnedOrItIsTheAfph | Pass 54 | ROF one lower after a CA change is built. Missing: the Intensive Fire counter for a Gun with no Multiple ROF (R8.2 says no catalog Gun needs it) and the 76 to 82mm mortar exception. | audit |
| C2.6 | Gun Depression/Elevation | 168 | partly | GamePlanner.Ordnance.cs: OrdnanceMapFacts, GunTargetStatus; ScenarioA1OrdnanceCalculator.cs: Undecided | none found | Pass 34 | The range against elevation difference limit is computed (ruling R8.7), but every shot at another level is then refused as levels-differ. The mortar and AA exemptions, the same-hex building rules, and vehicular MG and FT are not built. | audit |
| C2.7 | Prohibited Hexes | 168 | not built |  |  | Pass 54 | No setup check keeps a Gun out of an upper building level, a Water Obstacle, crag or marsh, or a large Gun out of a building (found by search only). Pushing is limited to Open Ground and grain by R8.6, so only setup can break it. | audit |
| C2.8 | Fire & Movement | 169 | built | GamePlanner.Guns.cs: PushPlan, PlanHookGun; LiveOrdnance.cs: FromState | BacklogPass8Tests.APushMakesTheGunAndCrewTiYetTheCrewPushesOnAndTheBoreSightingIsLost |  | A pushed or unhooked Gun and its crew are TI and do not fire that Player Turn; a tank may fire after moving in principle (its AFPh shot is refused for lack of Case C). | audit |
| C2.9 | (none) | 169 | not applicable |  |  |  | Explains the star and asterisk notes on counters. | audit |

#### C3 The To Hit Process (pages 169 to 171)

27 rows: 7 built, 2 built with a deviation, 10 partly, 2 refused, 3 not built, 3 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C3 | The To Hit Process | 169 | not applicable |  |  |  | heading | audit |
| C3.1 | (none) | 169 | not applicable |  |  |  | Describes the two-step process; the code is built that way. | audit |
| C3.2 | Covered Arc | 169 | built | GamePlanner.Ordnance.cs: OrdnanceMapFacts, Bearing, GunTargetStatus | ScenarioA1OrdnanceTests.RefereeFindingsOnTheCoveredArcTargetsAndAcquisition; OrdnanceStepsTests.AShotOutsideTheCoveredArcTurnsTheGunWithCaseAAndALowerRof |  | A 60 degree wedge on the barrel's hexspine, boundary rows included, read across boards (R8.7). The ambiguous counter placement clause is table procedure. | audit |
| C3.21 | Firing Within CA | 169 | built | GamePlanner.Ordnance.cs: OrdnanceMapFacts; ScenarioA1ArmorCalculator.cs: CaseA; LiveOrdnance.cs: OrdnanceRecordVerifier.Verify | OrdnanceStepsTests.AShotOutsideTheCoveredArcTurnsTheGunWithCaseAAndALowerRof |  | Fewest hexspines turned, Case A applied, the Gun or turret left on its new facing. A non-turreted vehicular MA that would pivot the vehicle is refused (play.ordnance-vca). | audit |
| C3.22 | Changing CA Without Fire | 169 | built with a deviation | GamePlanner.Guns.cs: PlanTurnGun; GameProjector.cs: TurnGun | BacklogPass8Tests.AGunTurnsWithoutFiringAndThenFiresAndMovesNoMore | Pass 33 | Ruling R8.9: the change is allowed at any moment of the friendly fire phase and bars further fire that phase, in place of "at the end of the phase". Non-vehicular Guns only; a tank's turret or VCA change without fire, and the limbering, Pushing and D3.51 exceptions are not built. | audit |
| C3.3 | To Hit (TH) Table | 169 | partly | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1OrdnancePackage.cs: BasicToHit, Color | ScenarioA1OrdnanceTests.TheBasicAndModifiedToHitNumbersFollowTheTableTheColorAndTheGun | Pass 54 | Table, Modified TH#, DRM and resolution on the IFT or a TK Table are built. Missing: the TH# color knows only German black and Russian red and defaults every other nationality to red; the A5.132 overstacked vehicle miss and Collateral Attacks on PRC (D.8) are not built. | audit |
| C3.31 | Vehicle Target Type | 169 | built | LiveOrdnance.cs: FromState; GamePlanner.Ordnance.cs: PlanFireOrdnance; ScenarioA1ArmorCalculator.cs: Run | ScenarioA1Pass7Tests.ATurretHitMeetsTheTurretArmorAndTheToKillDrEliminates; BacklogPass7Tests |  | One named vehicle, nothing else in the hex harmed (ruling R7.2). The alternative of HE on the Area Target Type against an AFV is not built (C3.33). | audit |
| C3.32 | Infantry Target Type | 169 | partly | ScenarioA1OrdnanceCalculator.cs: Outside, Run; GamePlanner.Ordnance.cs: PlanFireOrdnance | ScenarioA1OrdnanceTests.AHitAttacksTheLocationOnTheGunsHeColumnWithTheTemOnTheToHitDr | Pass 50 | HE at the enemy Infantry of one Location with one Effects DR is built. A Location that also holds a vehicle is refused (R25.10), so unarmored vehicles are never hit with the Infantry; AP and HEAT HE Equivalency (C8.31) is not built; an empty Location is refused. | audit |
| C3.33 | Area Target Type | 169 | partly | ScenarioA1AreaCalculator.cs: Run; ScenarioA1OrdnanceCalculator.cs: Outside, HitAttack; GamePlanner.Ordnance.cs: PlanFireOrdnance | ScenarioA1Pass9Tests.TheAreaTargetTypeJudgesEachUnitAndAttacksThoseHitAtHalfTheHeFp | Pass 56 | Light mortars only (rulings R9.2, R9.3): red Area row, half FP, one DR, not in the own hex. Refused: any other Gun on the Area Target Type, a hex with friendly units, a vehicle, a manned Gun, or units in several Locations. Not built: SMOKE and WP, units out of the firer's LOS, the loss of all ROF by a non-mortar. | audit |
| C3.331 | (none) | 170 | partly | ScenarioA1AreaCalculator.cs: Run; ScenarioA1FireCalculator.cs: Arithmetic | ScenarioA1Pass9Tests.AMortarHitInWoodsTakesAirBurstsInsteadOfTheWoodsTem; ScenarioA1Pass9Tests.ACriticalHitIsJudgedForEachUnit | Pass 56 | Cases E, L and Q left off the TH DR, in-hex TEM on the Effects DR, per-unit DRM. Hexside TEM and the HD exclusion are not built. | audit |
| C3.332 | (none) | 170 | refused | GamePlanner.Ordnance.cs: PlanFireOrdnance | none found | Pass 50 | play.ordnance-area-vehicle (ruling R9.3). Needs C1.55 and Collateral Attacks (D.8B). | audit |
| C3.4 | Multiple Targets | 170 | partly | LiveOrdnance.cs: FromState; ScenarioA1AreaCalculator.cs: Run; ScenarioA1OrdnanceCalculator.cs: Undecided | ScenarioA1Pass8Tests.DefensiveFirstFireTakesCasesJ3AndJ4AndLeavesAFirstFireCounter | Pass 50 | Only the moving units are hit in Defensive First Fire, and the Area Target Type judges each unit. On the Infantry Target Type a Location mixing concealed and Known units is refused (concealment-mixed) in place of two results; units out of LOS or too close are not handled. | audit |
| C3.41 | (none) | 170 | refused | ScenarioA1OrdnanceCalculator.cs: Outside | ScenarioA1OrdnanceTests.ShotsOutsideTheReviewAreAbstained | Pass 50 | A shot at a Location with no enemy unit is target-outside. Fire at an unmanned Gun, a building, a bridge, or a vehicle on these Target Types is not built. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C3.5 | FP Modifiers | 170 | built | ScenarioA1OrdnanceCalculator.cs: HitAttack; ScenarioA1FireCalculator.cs: Arithmetic | ScenarioA1OrdnanceTests.AHitAttacksTheLocationOnTheGunsHeColumnWithTheTemOnTheToHitDr |  | A hit attacks on its own FP column with no Infantry FP modification. | audit |
| C3.51 | (none) | 170 | built | ScenarioA1FireCalculator.cs: Arithmetic; ScenarioA1OrdnanceCalculator.cs: Run (case-l) | ScenarioA1OrdnanceTests.AHitAttacksTheLocationOnTheGunsHeColumnWithTheTemOnTheToHitDr |  | No PBF or TPBF doubling; Case L on the TH DR instead. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C3.52 | (none) | 170 | built | ScenarioA1OrdnanceCalculator.cs: Outside | ScenarioA1OrdnanceTests.ShotsOutsideTheReviewAreAbstained |  | No Long Range Fire; beyond the listed range is refused. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C3.53 | (none) | 170 | partly | ScenarioA1FireCalculator.cs: Arithmetic | ScenarioA1OrdnanceTests.EachShotAcquiresTheLocationToMinusTwoAndAConcealedOneOnlyWhenItLosesConcealment | Pass 45 | Not halved for concealment, a pinned firer, or the AFPh. Missing: HE FP is not halved into a marsh (marsh is a playable target terrain, so this is a silent omission) nor against fording units (not built). Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C3.6 | Improbable Hits | 170 | built | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run, Kill; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.WhenNoFinalDrCanHitAnOriginalTwoStillHitsOnASubsequentDrOfThreeOrLess; ScenarioA1Pass7Tests.AnImprobableHitsSubsequentDrPicksTheTurretOrTheHull |  | Subsequent dr 1 CH, 2 turret, 3 hull, 4 to 6 miss. The HD variant waits on D4.2. | audit |
| C3.7 | Critical Hits (CH) | 170 | partly | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.AFinalDrBelowHalfTheModifiedToHitIsACriticalHitWithDoubledFpAndReversedTem; ScenarioA1OrdnanceTests.AnOriginalTwoThatHitsCallsForASubsequentDrThatDecidesTheCriticalHit | Pass 50 | All three Target Types and the lowest Final DR exception are built. Not built: the FFE CH, the HD clauses, Deliberate Immobilization, and MG TK attacks (other rules). | audit |
| C3.71 | Resolution vs Non-Armored Targets | 170 | partly | ScenarioA1FireCalculator.cs: Arithmetic; ScenarioA1OrdnanceCalculator.cs: HitAttack, Run | ScenarioA1OrdnanceTests.AFinalDrBelowHalfTheModifiedToHitIsACriticalHitWithDoubledFpAndReversedTem; ScenarioA1Pass8Tests.ACriticalHitOfDefensiveFirstFireKeepsFfnamAndFfmo | Pass 50 | Full FP doubled, positive TEM reversed, Air Bursts, FFNAM and FFMO kept, a Gun destroyed. Missing: the C.7 column rule, HE Equivalency, hexside TEM reversal, and the unarmored TK doubling is in C7. | audit |
| C3.72 | Resolution vs Armored Targets | 171 | partly | ScenarioA1ArmorCalculator.cs: Kill | ScenarioA1Pass7Tests.TheRearFacingAddsOneAndACriticalHitDoublesTheBasicTk | Pass 50 | Basic TK# doubled on a Direct Fire CH. The FFE or Area Target Type HE CH against an AFV (C1.55 with doubled FP) is not built. | audit |
| C3.73 | Resolution vs Terrain | 171 | not applicable |  |  |  | States that a CH adds nothing against terrain. Ordnance damage to terrain itself (rubble B24.11, Flame B25.13) is not built anywhere, so there is nothing to add or withhold. | audit |
| C3.74 | Resolution vs Multiple Targets | 171 | partly | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.ACriticalHitAmongSeveralTargetsFallsOnTheUnitRandomSelectionPicks; ScenarioA1OrdnanceTests.ACriticalHitAndTheNormalHitShareOneEffectsDr | Pass 50 | Random Selection among the units of one Location, the rest hit normally with the same DR. Selection among several occupied Locations (Area Target Type, OBA) is not built. | audit |
| C3.75 | Harassing Fire & Barrage | 171 | not built |  |  | Pass 58 | Needs C1.72 Harassing Fire and E12 Barrage. | audit |
| C3.76 | WP | 171 | not built |  |  | Pass 56 | Needs WP (A24.3) and the WP FFE. | audit |
| C3.8 | Multiple Hits | 171 | not built |  |  | Pass 53 | Multiple Hits for Guns of 15mm to 40mm on Doubles. No catalog Gun is in the band (45L, 50, 75*, 76L); backlog row under R8.12. | audit |
| C3.9 | Location of Vehicular Hits | 171 | built with a deviation | ScenarioA1ArmorCalculator.cs: Kill | ScenarioA1Pass7Tests.ATurretHitMeetsTheTurretArmorAndTheToKillDrEliminates; ScenarioA1Pass7Tests.AHullHitEqualToTheTkImmobilizesAndTheCrewMayAbandon | Pass 50 | Ruling R7.4: colored dr below white dr strikes the turret only when the target is turreted; a non-turreted AFV (the SPW 251/1) is always hit in the hull, where the rule gives an upper-superstructure hit. The TCA facing for a turret hit is built; the HD clauses are not. | audit |

#### C4 Gun & Ammo Type Basic TH# Modifications (page 171)

9 rows: 6 built, 1 partly, 1 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C4 | Gun & Ammo Type Basic TH# Modifications | 171 | not applicable |  |  |  | heading; its bracketed note is carried by C4.5. | audit |
| C4.1 | Barrel Length | 171 | built | ScenarioA1OrdnancePackage.cs: Modifications | ScenarioA1OrdnanceTests.TheBasicAndModifiedToHitNumbersFollowTheTableTheColorAndTheGun; ScenarioA1Pass9Tests.ALightMortarFiresAtTheRedAreaRowWithItsBarrelAndCaliberModifications |  | Applied to Guns, tank MA, light mortars and the ATR, not to the PF or PSK; no effect on the TK DR. | audit |
| C4.11 | * | 171 | built | ScenarioA1OrdnancePackage.cs: Modifications | ScenarioA1Pass9Tests.ALightMortarFiresAtTheRedAreaRowWithItsBarrelAndCaliberModifications |  | -1 beyond 12 hexes, from the chart row. | audit |
| C4.12 | L | 171 | built | ScenarioA1OrdnancePackage.cs: Modifications | ScenarioA1OrdnanceTests.TheBasicAndModifiedToHitNumbersFollowTheTableTheColorAndTheGun |  | +1 beyond 12 hexes, from the chart row. | audit |
| C4.13 | LL | 171 | built | ScenarioA1OrdnancePackage.cs: Modifications | none found |  | The LL row (+1 beyond 12, +2 beyond 24) is loaded and applied, but no catalog weapon is LL and no test with one was found. | audit |
| C4.2 | Small Calibers | 171 | built | ScenarioA1OrdnancePackage.cs: Modifications | ScenarioA1OrdnanceTests.TheBasicAndModifiedToHitNumbersFollowTheTableTheColorAndTheGun |  | Both rows match the chart on page 189. MG TH DR are not built, so the "including MG" clause is unreached. | audit |
| C4.3 | APCR/APDS | 171 | partly | ScenarioA1ArmorCalculator.cs: Run; ScenarioA1ArmorReference.cs: ApcrToHit | ScenarioA1Pass7Tests.ApcrFollowsItsDepletionNumber | Pass 34 | The APCR row is applied on the Vehicle Target Type. APDS is not an ammunition the code knows; the German 28LL and 40LL rule (APCR modification even with HE, no Depletion) is not built and has no counter. | audit |
| C4.4 | Smoke | 171 | not built |  |  | Pass 56 | +2 for SMOKE at 12 hexes or less. The SMOKE row is outside every transcription file; needs ordnance SMOKE (C8.5, C3.33). | audit |
| C4.5 | Modified TH# | 171 | built | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.TheBasicAndModifiedToHitNumbersFollowTheTableTheColorAndTheGun |  | The modifications are summed into the Modified TH#, apart from the DRM. | audit |

#### C5 Firer-Based Hit Determination DRM (pages 171 to 173)

26 rows: 4 built, 9 partly, 7 refused, 5 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C5 | Firer-Based Hit Determination DRM | 171 | not applicable |  |  |  | Heading; its bracketed note only explains the chart's L, @, and dagger marks. | audit |
| C5.1 | Case A: Fire Outside CA | 171 | partly | ScenarioA1ArmorCalculator.cs: CaseA; ScenarioA1OrdnanceCalculator.cs: Run; GamePlanner.Ordnance.cs: OrdnanceMapFacts | ScenarioA1Pass7Tests.ATurretTurnCostsLessThanAGunAndKeepsTheRof; BacklogPass7Tests.ATurretTurnsForItsShotKeepsItsTcaAndAShockedAfvRollsInTheRph | Pass 45 | T, ST, and NT values are right. Missing: 360-degree mounted Guns are refused (gun-outside), bow-mounted Guns and SA are not built. | audit |
| C5.11 | (none) | 172 | partly | ScenarioA1OrdnanceCalculator.cs: Run, Outside (covered-arc-fixed); GamePlanner.Ordnance.cs: OrdnanceMapFacts | ScenarioA1OrdnanceTests (woods doubling); BacklogPass8Tests.AGunTurnsWithoutFiringAndThenFiresAndMovesNoMore | Pass 45 | Doubling and the CA lock are built for woods and buildings but not rubble (fault). A VCA pivot by a non-turreted vehicle with its Bog Check is refused (play.ordnance-vca); the woods-road exception is not built. | audit |
| C5.12 | (none) | 172 | built | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run | ScenarioA1OrdnanceTests; OrdnanceStepsTests.U28AGunFiresHeAtASquadKeepsItsRofAndItsAcquisitionLowersTheNextToHitDr |  | Case A is added only when the shot itself turns the Gun (HexspinesToTurn above 0). | audit |
| C5.13 | Bounding First Fire | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (phase-outside) | ScenarioA1Pass8Tests.DefensiveFirstFireTakesCasesJ3AndJ4AndLeavesAFirstFireCounter | Pass 33 | No ATTACKER ordnance fires in its own MPh at all, so Case A never arises there. Needs Bounding First Fire (D3.3). | audit |
| C5.2 | Case B: Fire in AFPh | 172 | partly | ScenarioA1OrdnanceCalculator.cs: Run, Outside; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests; ScenarioA1Pass8Tests.IntensiveFireNeedsTheNormalRofUsedAndNotTheAfph | Pass 45 | Plus 2, plus 3 in woods or a building, one shot in the AFPh: built. Missing: plus 3 in rubble; the Opportunity Fire exemption (an Opportunity Firer's Gun still takes Case B and fires once). | audit |
| C5.3 | Case C: Bounding Firer | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (vehicle-fire-outside) | ScenarioA1Pass7Tests.AMovingFirerIsOutside | Pass 33 | A vehicle that moved does not fire in the AFPh; Bounding Fire, Bounding First Fire, and Passenger ordnance are not built. | audit |
| C5.31 | Case C1: Bounding First Firer, Restricted Aim | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (phase-outside) | ScenarioA1Pass7Tests.AMovingFirerIsOutside | Pass 33 | No vehicle fires in its own MPh. Needs D3.3 Bounding First Fire and MP in LOS counted for the firer. | audit |
| C5.32 | Case C2: Bounding First Firer, Limited Aim | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (phase-outside) | ScenarioA1Pass7Tests.AMovingFirerIsOutside | Pass 33 | As C5.31. | audit |
| C5.33 | Bounding First Fire | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (phase-outside) | ScenarioA1Pass7Tests.AMovingFirerIsOutside | Pass 33 | As C5.31; the Gun Duel clause needs C2.2401. | audit |
| C5.34 | Case C3: LATW | 172 | partly | ScenarioA1ArmorCalculator.cs: Run (case-c3:afph, case-c3:backblast); GamePlanner.Ordnance.cs: SupportWeaponMapFacts | ScenarioA1Pass9Tests.APanzerfaustTakesCaseC3AndAnOriginal12ReducesItsFirer; ScenarioA1Pass9bTests.APanzerschreckTakesTheBackblastAndAnAtrDoesNot | Pass 45 | Plus 2 in the AFPh and plus 2 from a ground-level building, cumulative, are built. Missing: the Desperation choice (C13.81), rubble as a Backblast Location, and the Opportunity Fire exemption. | audit |
| C5.35 | Case C4: Motion Firer | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (vehicle-fire-outside) | ScenarioA1Pass7Tests.AMovingFirerIsOutside | Pass 33 | A vehicle in Motion does not fire. Needs Motion Fire, the doubled lower dr, and Gyrostabilizers. | audit |
| C5.4 | Case D: Pinned Firer | 172 | partly | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests; ScenarioA1Pass9Tests.SpottedFireAddsTwoAndLowersTheRofAndAPinnedSpotterAddsCaseD | Pass 33 | Plus 2 and the lost Multiple ROF are built. The plus 2 for each instance of Area Fire on the firer (for example a LATW in a shallow stream) is not built. | audit |
| C5.5 | Case E: Firing Within Hex | 172 | partly | ScenarioA1OrdnanceCalculator.cs: Run (case-e), Outside; GamePlanner.Ordnance.cs: OrdnanceMapFacts | ScenarioA1Pass8Tests.CasesEHAndMAndOverstackingChangeTheToHitDr; BacklogPass8Tests.AGunFiresAtEnemyInfantryInItsOwnHexWithCaseE | Pass 45 | Only a Gun at Infantry in its own Location (ruling R8.8). Missing: a vehicle as firer or target, the in-hex wreck, SMOKE, and AFV Hindrance (set to 0), doubling in rubble, Bypass and CAFP. | audit |
| C5.51 | CA Change | 172 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (out-of-range) | ScenarioA1Pass8Tests.CasesEHAndMAndOverstackingChangeTheToHitDr | Pass 53 | Defensive First Fire inside the Gun's own hex with a CA change is refused (ruling R8.8). | audit |
| C5.6 | Case F: Intensive Fire | 172 | partly | ScenarioA1OrdnanceCalculator.cs: Outside (mayFire), Counter; GamePlanner.Ordnance.cs: AddOrdnanceEvents | ScenarioA1Pass8Tests.AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire; BacklogPass8Tests.AGunThatUsedItsRofFiresOnceMoreAsIntensiveFire | Pass 54 | Built for non-vehicular Guns (ruling R8.2). A vehicle's Intensive Fire is refused; an Opportunity Firer's Intensive Fire in the AFPh is not built. | audit |
| C5.61 | To Hit Penalty | 173 | built | ScenarioA1OrdnanceCalculator.cs: PassEightFirerDrm | ScenarioA1Pass8Tests.AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire |  | Case F plus 2. | audit |
| C5.62 | Malfunction | 173 | built | ScenarioA1OrdnanceCalculator.cs: Breakdown | ScenarioA1Pass8Tests.AGunMarkedFirstFireFiresOnceMoreOnlyAsIntensiveFire |  | B# two lower while Intensive Firing. | audit |
| C5.63 | Restrictions | 173 | partly | ScenarioA1OrdnanceCalculator.cs: Outside (mayFire) | ScenarioA1Pass8Tests.IntensiveFireNeedsTheNormalRofUsedAndNotTheAfph | Pass 54 | Case B never joins Intensive Fire because the AFPh is barred. The "No IF" listing has no catalog attribute and no check. | audit |
| C5.64 | OVR Protection | 173 | not built |  |  | Pass 53 | OVR Prevention: no extra in-hex shot before an OVR, no NMC on the crew from the TH DR, no No Fire counter. Needs D7.1 timing and Case E against a vehicle. | audit |
| C5.641 | (none) | 173 | not built |  |  | Pass 53 | The unmarked Gun's OVR Prevention shot is not built either. | audit |
| C5.7 | Case G: Deliberate Immobilization | 173 | not built |  |  | Pass 53 | Case G, Deliberate Immobilization: no declaration, no plus 5. | audit |
| C5.71 | (none) | 173 | not built |  |  | Pass 53 | The conditions (Basic TK# above the lowest hull AF, hull hit, six hexes, no Acquisition DRM) are not built. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C5.72 | (none) | 173 | not built |  |  | Pass 53 | Automatic Immobilization and Crew TC from a Case G hull hit are not built. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C5.8 | Case H: Captured Gun | 173 | partly | ScenarioA1OrdnanceCalculator.cs: PassEightFirerDrm (case-h), Outside (crew-outside); LiveOrdnance.cs: FromState | ScenarioA1Pass8Tests.CasesEHAndMAndOverstackingChangeTheToHitDr; BacklogPass8Tests.ASquadMansAGunWithCaseH | Pass 54 | Non-qualified manning adds plus 2. A Captured Gun (red TH#, B# two lower, plus 4 when both apply) is refused: only the Gun's own nationality mans it. | audit |
| C5.9 | Case I: Buttoned Up | 173 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-i), Outside; ScenarioA1ArmorCalculator.cs: Run; LiveOrdnance.cs: FromState | ScenarioA1Pass7Tests.ATankFiresHeAtInfantryWithCaseI; BacklogPass7Tests.AClosedToppedTanksCrewIsButtonedUpUnlessExposed |  | Plus 1 for a BU AFV; an RST or 1MT MA fires only BU. | audit |

#### C6 Target-Based Hit Determination DRM (pages 173 to 175)

30 rows: 14 built, 10 partly, 5 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C6 | Target-Based Hit Determination DRM | 173 | built | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests |  | The one sentence (DRM are cumulative) is how every calculator sums its DRM list. | audit |
| C6.1 | Case J: Moving/Motion Vehicular Target | 173 | partly | ScenarioA1ArmorCalculator.cs: Run (case-j); LiveOrdnance.cs: VehicleTargetFacts | ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr | Pass 46 | Plus 2 against a vehicle that moved or is in Motion. Dashing Infantry (A4.63) is not built, nor VBM as a cause. | audit |
| C6.11 | Case J1: Restricted Aim | 173 | built | ScenarioA1ArmorCalculator.cs: Run (case-j1); GamePlanner.Ordnance.cs: MpInLos | ScenarioA1Pass8Tests.DefensiveFirstFireAtAVehicleTakesCaseJ2J1OrJ |  | Plus 3 at three MP or fewer in the firer's continuous LOS (ruling R8.1). | audit |
| C6.12 | Case J2: Limited Aim | 173 | built | ScenarioA1ArmorCalculator.cs: Run (case-j2); GamePlanner.Ordnance.cs: MpInLos | ScenarioA1Pass8Tests.DefensiveFirstFireAtAVehicleTakesCaseJ2J1OrJ |  | Plus 4 at one MP or fewer; J1 not added to it. | audit |
| C6.13 | Case J3: FFNAM | 173 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-j3); ScenarioA1AreaCalculator.cs: Run | ScenarioA1Pass8Tests.DefensiveFirstFireTakesCasesJ3AndJ4AndLeavesAFirstFireCounter; BacklogPass8Tests.AGunDefensiveFirstFiresAtAMovingSquadAndIsMarkedFirstFire |  | Minus 1 for non-Assault Movement in Defensive First Fire. | audit |
| C6.14 | Case J4: FFMO | 173 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-j4); GamePlanner.Ordnance.cs: PassEightFacts | ScenarioA1Pass8Tests.DefensiveFirstFireTakesCasesJ3AndJ4AndLeavesAFirstFireCounter |  | Minus 1 in Open Ground with no Hindrance. | audit |
| C6.15 | (none) | 173 | built | GamePlanner.Ordnance.cs: MpInLos | ScenarioA1Pass8Tests.MpClaimedByAnEarlierShotCountNeitherForCaseJ1NorForTheLimit |  | MP counted back to the last Location out of the firer's LOS; a vehicle seen since its MPh began takes Case J alone. | audit |
| C6.16 | (none) | 173 | partly | ScenarioA1ArmorCalculator.cs: Run (case-j when target.Moving) | ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr | Pass 33 | In the DFPh a moving vehicle takes Case J only: built. The MP left at the end of the MPh counted as spent in the last hex: no logic found (search only). | audit |
| C6.17 | (none) | 173 | built | ScenarioA1OrdnanceCalculator.cs: Outside (first-fire-limit); LiveOrdnance.cs: FromState | ScenarioA1Pass8Tests.DefensiveFirstFireAtATargetIsLimitedByTheMfItSpentThere; ScenarioA1Pass8Tests.MpClaimedByAnEarlierShotCountNeitherForCaseJ1NorForTheLimit |  | Shots limited to the MF or MP spent in the Location, minimum one; earlier shots claim MP. Target Facing changes and the Gun Duel clause are not modelled. | audit |
| C6.2 | Case K: Concealed Target | 174 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-k), Undecided; ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.RefereeFindingsOnTheCoveredArcTargetsAndAcquisition; ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr |  | Plus 2. A Location mixing concealed and Known units is refused on the Infantry Target Type (concealment-mixed); the pillbox, cave, and SMOKE exceptions have nothing to apply to. | audit |
| C6.3 | Case L: Point Blank Range | 174 | partly | ScenarioA1OrdnanceCalculator.cs: Run (case-l); ScenarioA1ArmorCalculator.cs: Run | ScenarioA1OrdnanceTests; ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr | Pass 45 | Minus 2 at one hex, minus 1 at two, none against a Non-Stopped target or in the own hex. Fault: the ATR is denied Case L though the rule exempts only non-ATR LATW. | audit |
| C6.4 | Case M: Bore Sighted Location | 174 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-m); ScenarioA1ArmorCalculator.cs: Run; LiveOrdnance.cs: BoreSighted | ScenarioA1Pass8Tests.CasesEHAndMAndOverstackingChangeTheToHitDr; BacklogPass8Tests.TheScenarioDefendersBoreSightedLocationTakesCaseM |  | Minus 2; the game takes Case M over Case N for the firer (never worse than the Acquisition). | audit |
| C6.41 | (none) | 174 | partly | GamePlanner.cs: setup bore sights; GameProjector.cs: BoreSight | BacklogPass8Tests.TheScenarioDefendersBoreSightedLocationTakesCaseM | Pass 54 | Scenario Defender's manned Guns only. Missing: a vehicle's MA and SA, MMG, HMG, and light mortars. | audit |
| C6.42 | (none) | 174 | partly | GamePlanner.cs: setup bore sights; GameProjector.cs: BoreSight | BacklogPass8Tests.TheScenarioDefendersBoreSightedLocationTakesCaseM | Pass 54 | One Location per Gun, in LOS, within 16 hexes, recorded with crew and setup Location. Missing: "outside its own hex" is tested per Location, not per hex; ground level only in a building hex; a Spotter's LOS. | audit |
| C6.43 | (none) | 174 | built | LiveOrdnance.cs: BoreSighted; GameProjector.cs: Manhandle and HookGun (BoreSights removed) | BacklogPass8Tests.APushMakesTheGunAndCrewTiYetTheCrewPushesOnAndTheBoreSightingIsLost |  | Claimed only by the original crew from the setup Location; lost when the Gun is pushed or hooked up. Limbering, dm, Crest, and VCA have no counterpart yet. | audit |
| C6.44 | (none) | 174 | not built |  |  | Pass 47 | No MG or IFE Bore Sighting: the minus 2 on the IFT DR does not exist (settles the coverage document's unconfirmed entry). | audit |
| C6.5 | Case N: Acquired Target | 174 | partly | ScenarioA1OrdnanceCalculator.cs: Run; ScenarioA1ArmorCalculator.cs: Run; GameProjector.cs: Ordnance, KeepAcquisitions; LiveOrdnance.cs: FromState | OrdnanceStepsTests.U28AGunFiresHeAtASquadKeepsItsRofAndItsAcquisitionLowersTheNextToHitDr; ScenarioA1OrdnanceTests.AnOriginalTwelveMalfunctionsTheGunAndLosesTheAcquisition | Pass 45 | Minus 1 then minus 2, per Gun, lost on malfunction, a new target, or a crew not Good Order. Not lost (faults) when the Gun turns without firing, is pushed or towed, or a tank moves; no 20mm test, no bridge target, no CMG or CC loss. | audit |
| C6.51 | (none) | 174 | partly | GamePlanner.Acquisition.cs: AcquiredUnits, AcquisitionFollowUp; GameProjector.cs: ChangeAcquisition, KeepAcquisitions, the Acquisition choice | BacklogPass5Tests.AnAcquisitionFollowsItsUnitsAndStaysWhereTheyLeftTheGunsLos | Pass 45 | Built for a Gun at Infantry (ruling R5.13): follows the Known units, stays in the last Location in LOS, the owner chooses when they split. A vehicle target and a tank's Acquisition are not followed (the counter stays on the Location). | audit |
| C6.52 | Bracketing | 174 | partly | LiveOrdnance.cs: FromState (the Acquisition is read by Location) | none found | Pass 50 | The Infantry and Vehicle Target Types share the Location's Acquisition, so that transfer falls out; no test names it. Transfers to and from the Area Target Type are not built (only light mortars use it, and they may not transfer). | audit |
| C6.521 | Area Acquisition | 174 | partly | ScenarioA1AreaCalculator.cs: Run (case-n, acquires); GameProjector.cs: KeepAcquisitions | ScenarioA1Pass9Tests.TheAreaTargetAcquisitionAppliesToConcealedUnitsAndAlwaysSteps; BacklogPass9Tests.AMortarKeepsItsAreaAcquisitionForItsNextShot | Pass 50 | Light mortars only: applies to every unit, concealed or not. Missing: the whole hex (it is keyed to one Location and multi-Location hexes are refused), an empty hex as a target, loss when the mortar is carried away. | audit |
| C6.53 | Multiple ROF | 175 | built | ScenarioA1OrdnanceCalculator.cs: Run; GameProjector.cs: Ordnance | OrdnanceStepsTests.U28AGunFiresHeAtASquadKeepsItsRofAndItsAcquisitionLowersTheNextToHitDr |  | Gained on each consecutive TH attempt and kept across fire phases (settles the unconfirmed entry). | audit |
| C6.54 | IFE | 175 | not built |  |  | Pass 53 | IFE is not built (the catalog carries an ife attribute that nothing reads), so nothing bars or removes Acquisition for it. | audit |
| C6.55 | Gyrostabilizer | 175 | not built |  |  | Pass 53 | No Gyrostabilizer and no Motion or Bounding First Fire. Fault beside it: a tank that moves keeps its Acquisition for its next shot. | audit |
| C6.56 | Smoke | 175 | not built |  |  | Pass 56 | Ordnance SMOKE is not built, so neither its bar on acquiring nor its use of an existing Acquisition. | audit |
| C6.57 | Concealed | 175 | built | ScenarioA1OrdnanceCalculator.cs: Run (acquires); ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests.RefereeFindingsOnTheCoveredArcTargetsAndAcquisition; ScenarioA1Pass9Tests.TheAreaTargetAcquisitionAppliesToConcealedUnitsAndAlwaysSteps |  | A concealed target is acquired only when the shot costs it its concealment, or on the Area Target Type. A concealed vehicle is never acquired even when the hit reveals it (small shortfall). | audit |
| C6.58 | (none) | 175 | not applicable |  |  |  | Counter lettering and colors: table procedure the program replaces by recording each Acquisition under its Gun's id. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C6.6 | Case O: Hazardous Movement | 175 | not built |  |  | Pass 45 | Case O is absent from the ordnance calculators; a Gun firing at a crew pushing a Gun takes Cases J3 and J4 instead (fault). Hazardous Movement exists only for IFT attacks (A4.62). | audit |
| C6.7 | Case P: Target Size | 175 | built | ScenarioA1ArmorCalculator.cs: Run (case-p); ScenarioA1OrdnanceCalculator.cs: Run (case-p) | ScenarioA1Pass8Tests.AnEmplacedGunAddsItsTargetSizeAndEmplacementToTheToHitDr; ScenarioA1Pass7Tests |  | Vehicle sizes minus 2 to plus 2; a Gun's size when its crew is alone in the Location. | audit |
| C6.8 | Case Q: TEM | 175 | partly | ScenarioA1OrdnanceCalculator.cs: Run (case-q); ScenarioA1ArmorCalculator.cs: Run; GamePlanner.Fire.cs: FireMapFacts | ScenarioA1OrdnanceTests; ScenarioA1Pass8Tests.AnEmplacedGunAddsItsTargetSizeAndEmplacementToTheToHitDr | Pass 52 | The Location's TEM is added; not on the Area Target Type. Wall and hedge TEM is refused for ordnance; Hull Down (D4.2) and Height Advantage are not built. | audit |
| C6.9 | Case R: Hindrance | 175 | built | ScenarioA1OrdnanceCalculator.cs: Run (case-r); ScenarioA1ArmorCalculator.cs: Run; ScenarioA1AreaCalculator.cs: Run | ScenarioA1OrdnanceTests; BacklogPass9Tests.SmokeGrenadesHinderDefensiveFireAndLeaveAtTheEndOfTheMph |  | Hindrance on the TH DR, not on the hit's IFT DR; LV Hindrance too. | audit |

#### C7 To Kill Tables (pages 175 to 177)

31 rows: 16 built, 5 partly, 2 refused, 3 not built, 5 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C7 | To Kill Tables | 175 | not applicable |  |  |  | Heading. | audit |
| C7.1 | (none) | 175 | built | ScenarioA1OrdnanceCalculator.cs: Resolve; ScenarioA1ArmorCalculator.cs: Kill; ScenarioA1ArmorReference.cs: BasicTk | ScenarioA1Pass7Tests.ATurretHitMeetsTheTurretArmorAndTheToKillDrEliminates |  | A Vehicle Target Type or LATW hit goes to the To Kill Table of its ammunition. | audit |
| C7.11 | TH# Derivation | 175 | partly | ScenarioA1ArmorCalculator.cs: Kill; ScenarioA1ArmorReference.cs: ArmorFactor, UnarmoredTk | ScenarioA1Pass7Tests.ThePanzerMeetsTheT34sArmor; ScenarioA1Pass7Tests.AnUnarmoredVehicleTakesTheUnarmoredTk | Pass 53 | Basic, Modified, and Final TK# less the AF are built. A Partially Armored AFV hit in an unarmored Facing or Aspect is not (the catalog trait is not read). | audit |
| C7.12 | Aerial AF | 176 | not built |  |  | Pass 40 | Aerial AF: no row in the transcription, no aircraft, no optimally Placed DC, no Underbelly hit. | audit |
| C7.2 | Modified TH# | 176 | not applicable |  |  |  | Heading; one sentence introducing Cases A to D. | audit |
| C7.21 | Case A: AFV Rear Target Facing | 176 | partly | ScenarioA1ArmorCalculator.cs: Kill (case-a) | ScenarioA1Pass7Tests.TheRearFacingAddsOneAndACriticalHitDoublesTheBasicTk | Pass 50 | Plus 1 for an ordnance hit on the rear Target Facing (the turret by its TCA). FT, DC, MOL, and aircraft attacks on an AFV are not built. | audit |
| C7.22 | Case B: Aerial/DC/MOL Elevation Advantage | 176 | not built |  |  | Pass 40 | Case B: no aircraft, no MOL or DC against an AFV, no elevation advantage. | audit |
| C7.23 | Case C: AFV CH | 176 | built | ScenarioA1ArmorCalculator.cs: Kill (case-c) | ScenarioA1Pass7Tests.TheRearFacingAddsOneAndACriticalHitDoublesTheBasicTk |  | A CH doubles the Basic TK# before the other modifications. | audit |
| C7.24 | Case D: Range Effects | 176 | built | ScenarioA1ArmorCalculator.cs: Kill (case-d); ScenarioA1ArmorReference.cs: CaseD | ScenarioA1Pass7Tests.ThePanzerMeetsTheT34sArmor |  | AP and APCR range rows match the chart; an NA range is refused. The APDS row is transcribed but unused. | audit |
| C7.3 | To Kill Table Types | 176 | not applicable |  |  |  | Heading. | audit |
| C7.31 | AP To Kill Table | 176 | built | ScenarioA1ArmorReference.cs: Lookup, GunSize | ScenarioA1Pass7Tests.ThePanzerMeetsTheT34sArmor; ScenarioA1Pass9bTests.AnAtrHitsOnTheBlackVehicleRowAndKillsWithTheRussianAtrTk |  | The colored listings are read by nationality; the table matches the chart (checked on the rendering of page 190). | audit |
| C7.311 | vs Unarmored | 176 | built | ScenarioA1ArmorReference.cs: UnarmoredTk; ScenarioA1ArmorCalculator.cs: Kill | ScenarioA1Pass7Tests.AnUnarmoredVehicleTakesTheUnarmoredTk |  | 7, 8, 9, 10, 11 by caliber band, no range, length, or AF. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C7.32 | APCR/APDS To Kill Table | 176 | partly | ScenarioA1ArmorReference.cs: BasicTk (apcr), CaseD | ScenarioA1Pass7Tests.ApcrFollowsItsDepletionNumber | Pass 34 | APCR is built. APDS (the D listings and its Case D row) is not; the 28LL and 40LL always-APCR rule has no logic (no such Gun in the catalog). | audit |
| C7.321 | (none) | 176 | built | ScenarioA1ArmorReference.cs: UnarmoredTk | ScenarioA1Pass7Tests.AnUnarmoredVehicleTakesTheUnarmoredTk |  | APCR against an unarmored vehicle uses the AP unarmored values. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C7.33 | HEAT To Kill Table | 176 | built | ScenarioA1ArmorReference.cs: BasicTk (heat) | ScenarioA1Pass9bTests.APanzerschreckReadsItsOwnTableAndKillsWithHeatTk26; ScenarioA1Pass9Tests.APanzerfaustCheckOf1To3GivesAShotAtTh10LessTwoPerHexAndHeatTk31 |  | By caliber, or the weapon's own row for a PF or PSK. | audit |
| C7.331 | (none) | 176 | built | ScenarioA1ArmorReference.cs: UnarmoredTk | ScenarioA1Pass7Tests.AnUnarmoredVehicleTakesTheUnarmoredTk |  | Final TK# 11. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C7.332 | (none) | 176 | built | ScenarioA1ArmorReference.cs: CaseD | ScenarioA1Pass9bTests.APanzerschreckReadsItsOwnTableAndKillsWithHeatTk26 |  | Case D is 0 for HEAT. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C7.34 | HE To Kill Table | 176 | built | ScenarioA1ArmorReference.cs: BasicTk (he) | ScenarioA1Pass7Tests.TheToKillDrBurnsShocksAndDuds |  | The highest band not above the Gun's caliber; the bands match the chart. | audit |
| C7.341 | (none) | 176 | built | ScenarioA1ArmorReference.cs: CaseD | ScenarioA1Pass7Tests.TheToKillDrBurnsShocksAndDuds |  | Case D is 0 for HE. | audit |
| C7.342 | (none) | 176 | built | ScenarioA1ArmorReference.cs: UnarmoredTk | ScenarioA1Pass7Tests.AnUnarmoredVehicleTakesTheUnarmoredTk |  | The HE unarmored value is the Final TK#. | audit |
| C7.343 | (none) | 177 | not applicable |  |  |  | A pointer to C7.41 and C7.5, where the HE results are judged. | audit |
| C7.344 | FT/MOL | 177 | not built |  |  | Pass 50 | No To Kill resolution for FT or MOL. A MOL at a Location holding a vehicle is refused (fire.mol-outside); FT against an AFV was judged from a search only. | audit |
| C7.345 | Mortars | 177 | refused | GamePlanner.Ordnance.cs: PlanFireOrdnance (play.ordnance-area-vehicle) | none found | Pass 50 | A mortar's shot at a hex holding a vehicle is refused (ruling R9.3). Needs C1.55 and C3.332. | audit |
| C7.346 | DC | 177 | refused | GamePlanner.DemolitionCharges.cs (play.dc-vehicle) | none found | Pass 50 | A DC is not Placed where a vehicle is; the Position DR and its DRM are not built. | audit |
| C7.35 | Dud | 177 | built | ScenarioA1ArmorCalculator.cs: Kill (Dud) | ScenarioA1Pass7Tests.TheToKillDrBurnsShocksAndDuds |  | An Original TK DR of 12 has no effect. | audit |
| C7.4 | Shock/Unconfirmed Kill (UK) | 177 | not applicable |  |  |  | Heading with a designer's sentence. | audit |
| C7.41 | Occurrence | 177 | built | ScenarioA1ArmorCalculator.cs: Kill (PossibleShock, Shock) | ScenarioA1Pass7Tests.OneAboveTheTkGivesAPossibleShockNtc; ScenarioA1Pass7Tests.TheToKillDrBurnsShocksAndDuds |  | Direct Fire: an NTC at one above for non-HE, automatic Shock for an HE turret hit at one above and for a turret hit equal to the TK#. The FFE and DC sources are not built. | audit |
| C7.42 | Effect | 177 | built | GamePlanner.Ordnance.cs: KillEvents; GamePlanner.Shock.cs: PlanRecoverShock; ScenarioA1OrdnanceCalculator.cs: Outside; GamePlanner.Vehicles.cs | BacklogPass7Tests.ATurretTurnsForItsShotKeepsItsTcaAndAShockedAfvRollsInTheRph |  | BU, halted, no fire, move, or CC; the RPh dr (1 or 2, then 1 to 3, else a wreck with no CS); a second Shock flips UK back (ruling R7.8). Riders unloading was not checked. | audit |
| C7.5 | Immobilization | 177 | partly | ScenarioA1ArmorCalculator.cs: Kill (Immobilized, CrewCheck); GamePlanner.Ordnance.cs: KillEvents | ScenarioA1Pass7Tests.AHullHitEqualToTheTkImmobilizesAndTheCrewMayAbandon; BacklogPass7Tests.AnImmobilizedTanksCrewMayAbandonIt | Pass 52 | A hull hit equal to the TK#, or HE one above it, immobilizes with a Crew TC. Missing: the HD exception, Indirect Fire, DC, and the FT and MOL line. | audit |
| C7.6 | Burning Wreck | 177 | built | ScenarioA1ArmorCalculator.cs: Kill (Burn); GamePlanner.Ordnance.cs: KillEvents; GameProjector.cs: Wreck | BacklogPass7Tests.ATankFiresApAtAnEnemyTanksSideAndBurnsIt |  | A Final TK DR at most half the Final TK# burns: a Blaze, no Crew Survival, Passengers gone with the vehicle. The FT and MOL sentence has nothing to apply to. | audit |
| C7.7 | AFV Destruction Table | 177 | partly | ScenarioA1ArmorCalculator.cs: Kill | ScenarioA1Pass7Tests.TheToKillDrBurnsShocksAndDuds; ScenarioA1Pass7Tests.EveryRollSequenceOfAVehicleShotEndsResolved | Pass 50 | Only the Direct Fire column (and the unarmored table's) is built. Missing: note A (minus 1 for a red CS# when judging a burning wreck), and the DC, FT, MOL, MG, Indirect Fire, and mine columns. | audit |

#### C8 Special Ammunition (pages 177 to 179)

19 rows: 5 built, 2 partly, 1 refused, 10 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C8 | Special Ammunition | 177 | not applicable |  |  |  | Heading; the bracketed note asks for a written side record, which the game state replaces. | audit |
| C8.1 | Ammunition Depletion | 177 | built | GamePlanner.Ordnance.cs: PlanFireOrdnance (ammunition argument); LiveOrdnance.cs: Ammunitions, FromState; ScenarioA1ArmorCalculator.cs: Outside | BacklogPass7Tests.ApcrAboveItsDepletionNumberIsNotFiredAndRunsOut |  | The ammunition is declared before the TH DR, shot by shot (ruling R7.6). | audit |
| C8.11 | APCR | 177 | partly | ScenarioA1ArmorCalculator.cs: Depletion | ScenarioA1Pass7Tests.ApcrFollowsItsDepletionNumber; ScenarioA1Pass7Tests.SpecialAmmunitionNeedsItsYear | Pass 34 | APCR by scenario year from the counter. APDS (D) is not built. | audit |
| C8.12 | Ammunition Supply Chart | 177 | partly | ScenarioA1ArmorCalculator.cs: Depletion | ScenarioA1Pass7Tests.SpecialAmmunitionNeedsItsYear | Pass 34 | The counter's own numbers by year are used. The chart itself is not encoded, and a parenthesized start month (Aug, Jun, Sep 1944) cannot be expressed. | audit |
| C8.2 | Elite | 178 | not built |  |  | Pass 34 | Elite plus 1 to the Depletion Number is not built (ruling R7.6 says so); C8.2 is only a citable rule on scenario cards. | audit |
| C8.3 | HEAT | 178 | built | ScenarioA1ArmorCalculator.cs: Depletion, Outside | ScenarioA1Pass7Tests.SpecialAmmunitionNeedsItsYear; ScenarioA1Pass9bTests.APanzerschreckReadsItsOwnTableAndKillsWithHeatTk26 |  | HEAT from May 1942 for the Germans and from 1943 for the U.S., Britain, and Russia; a PF or PSK fires HEAT with no Depletion Number. | audit |
| C8.31 | HE Equivalency | 178 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (gun-outside, target-type-outside); ScenarioA1ArmorCalculator.cs: Outside | ScenarioA1Pass9Tests.APanzerfaustIsBoundByDateRangeTargetAndUsage | Pass 50 | HEAT, AP, APCR, and ATR shots at anything but a vehicle are refused; HE Equivalency, Collateral Attacks, and the Residual FP bar are not built (ruling R9.8). | audit |
| C8.4 | Cannister | 178 | not built |  |  | Pass 34 | Canister: no ammunition type, no vertex aiming, no FP table. | audit |
| C8.41 | (none) | 178 | not built |  |  | Pass 34 | Canister's adjacent-hex Area Fire is not built. | audit |
| C8.42 | (none) | 178 | not built |  |  | Pass 34 | Canister's limits on rubble, Fire, and Wire are not built. | audit |
| C8.5 | Smoke | 178 | not built |  |  | Pass 56 | Ordnance SMOKE is not built (only Infantry smoke grenades exist). The British light mortar's s7 in the catalog is read by nothing. | audit |
| C8.51 | (none) | 178 | not built |  |  | Pass 56 | Full SMOKE in the PFPh and Dispersed otherwise: not built. | audit |
| C8.52 | (none) | 178 | not built |  |  | Pass 56 | SMOKE placement on an Area Target Type hit: not built. | audit |
| C8.6 | White Phosphorous (WP) | 179 | not built |  |  | Pass 56 | WP ammunition is not built. | audit |
| C8.7 | Illuminating Rounds | 179 | not built |  |  | Pass 58 | Illuminating Rounds from ordnance are not built (Starshells by leaders and AFV only). Needs E1.93. | audit |
| C8.8 | AP/HE Limited Stowage | 179 | not built |  |  | Pass 34 | AP# and HE# limited stowage: no catalog value and no check; AP and HE are unlimited unless the listing denies them. | audit |
| C8.9 | Depletion Numbers | 179 | built | ScenarioA1ArmorCalculator.cs: Run (use), Outside (ammunition-depleted); GameProjector.cs: Ordnance (DepletedAmmunition); GamePlanner.Ordnance.cs: AddOrdnanceEvents | ScenarioA1Pass7Tests.ApcrFollowsItsDepletionNumber; BacklogPass7Tests.ApcrAboveItsDepletionNumberIsNotFiredAndRunsOut; BacklogPass7Tests.AMalfunctionOnMissingApcrCountsAsFire |  | Below the number: used; at it: used and run out; above it: never fired unless the Gun malfunctioned, and gone for the scenario. Low Ammo is not built. | audit |
| C8.91 | (none) | 179 | built | ScenarioA1ArmorCalculator.cs: Depletion; ScenarioA1OrdnancePackage.cs: Ammo | ScenarioA1Pass7Tests.SpecialAmmunitionNeedsItsYear |  | Several numbers by year (catalog form A4^2 A5^3). | audit |
| C8.92 | (none) | 179 | built | ScenarioA1ArmorCalculator.cs: Depletion | none found |  | Intensive Fire leaves the Depletion Number alone: nothing in the code changes it. No test names it. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |

#### C9 Mortars (page 179)

7 rows: 3 built, 3 partly, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C9 | Mortars | 179 | not applicable |  |  |  | Heading. | audit |
| C9.1 | (none) | 179 | partly | ScenarioA1OrdnanceCalculator.cs: Outside (target-type-outside), HitAttack; ScenarioA1AreaCalculator.cs: Run; GamePlanner.Ordnance.cs: PlanFireOrdnance | ScenarioA1Pass9Tests.ALightMortarFiresAtTheRedAreaRowWithItsBarrelAndCaliberModifications; ScenarioA1Pass9Tests.AMortarHitInWoodsTakesAirBurstsInsteadOfTheWoodsTem | Pass 50 | A mortar always uses the Area Target Type and the IFT with Indirect Fire handling. A hex holding a vehicle, friendly units, or units on several levels is refused. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C9.2 | Light Mortars | 179 | partly | LiveOrdnance.cs: FromState; ScenarioA1OrdnanceCalculator.cs: Outside, Leadership; ScenarioA1AreaCalculator.cs: Run | BacklogPass9Tests.ALightMortarHitsTheAreaAndItsSquadStillFiresItsInherentFp; ScenarioA1Pass9Tests.ALeaderDirectsAMortarAndALoneSmcFiresItWithoutMultipleRof | Pass 54 | Light mortars as SW: no CA, any Personnel, leadership, a lone SMC without ROF, Area Acquisition. Missing: the 76 to 82mm mortar as a Gun and its dm counter (no such counter in the catalog). | audit |
| C9.3 | Spotters | 179 | partly | LiveOrdnance.cs: MortarSupport; GamePlanner.Ordnance.cs: SupportWeaponMapFacts, AddOrdnanceEvents; ScenarioA1OrdnanceCalculator.cs: SupportWeaponOutside | BacklogPass9Tests.ASpottedMortarFiresBeyondItsOwnLosAndKeepsItsSpotter; ScenarioA1Pass9Tests.SpottedFireIsDesignatedInThePfphOrDfphAndAPinnedPanzerfaustFirerInABuildingMayNotFire | Pass 54 | Built (ruling R9.4): a Good Order Personnel Spotter in the hex or adjacent, its LOS and Hindrance, a pinned Spotter, the SW-use marker, the kept Spotter. Missing: one Spotter for several mortars at one target, the wait until the next MPh for a new Spotter, the Opportunity Fire clauses. | audit |
| C9.31 | Spotted Fire | 179 | built | ScenarioA1AreaCalculator.cs: Run (spotted, rof) | ScenarioA1Pass9Tests.SpottedFireAddsTwoAndLowersTheRofAndAPinnedSpotterAddsCaseD |  | Plus 2 and the Multiple ROF one lower. | audit |
| C9.4 | Minimum Range | 179 | built | ScenarioA1OrdnanceCalculator.cs: Outside (out-of-range, RangeMinimum) | BacklogPass9Tests.AMortarKeepsItsMinimumRangeAndDoesNotFireFromABuildingOrAfterMoving |  | No shot nearer than the minimum range. | audit |
| C9.5 | CH | 179 | built | ScenarioA1AreaCalculator.cs: Run; ScenarioA1OrdnanceCalculator.cs: HitAttack | ScenarioA1Pass9Tests.AMortarsCriticalHitNeedsAnOriginal2AndDoublesTheFullFp; ScenarioA1Pass9Tests.ACriticalHitIsJudgedForEachUnit |  | A CH only on an Original 2; the full FP doubled instead of halved. | audit |

#### C10 Gun & Ammo Movement (pages 180 to 181)

22 rows: 1 built, 7 partly, 1 refused, 12 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C10 | Gun & Ammo Movement | 180 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| C10.1 | Towing | 180 | partly | GamePlanner.Guns.cs: PlanHookGun; GamePlanner.VehicleTerrain.cs: VehicleCost, VehicleBypass; GamePlanner.Vehicles.cs: TowSetupBar, reverse bar at start; GameProjector.cs: wreck drops towed Gun | BacklogPass8Tests.ATruckHooksUpAGunTowsItAndUnhooksIt; BacklogPass26Tests.ATruckEntersTowingAGunAndUnhooksItForItsCrew | Pass 45 | Built: T# at most M#, +1 MP per hex, no fire in tow, Reverse refused, Gun lost with its vehicle, never a separate target. Missing: the bar on Bypass (VBM is admitted at +1 MP), on crossing a wall or hedge while towing (a halftrack crosses a hedge), Deep Stream, wagons (MF), the 76-107mm MTR exception. | audit |
| C10.11 | Hooking Up | 180 | partly | GamePlanner.Guns.cs: PlanHookGun; GameProjector.cs: HookGun | BacklogPass8Tests.ATruckHooksUpAGunTowsItAndUnhooksIt; BacklogPass8Tests.AHookedUpTruckIsTiAndAnAbandonedGunCanBeHookedUp | Pass 54 | Built: Stopped vehicle, half MP (FRU), crew on foot in the hex, all three TI, crew may board (R26.2). Missing: two-thirds MP for a circled M# (no data), the not-in-Bypass check, no fire earlier that PFPh or MPh, the crew's half-MF limit, the Defensive First Fire opportunity the expenditure gives. | audit |
| C10.111 | (none) | 180 | partly | GamePlanner.Guns.cs: PlanHookGun (CanCrew); GamePlanner.Movement.cs: PlanMove push check | BacklogPass8Tests.ACrewPushesItsGunOrStaysOnAHighManhandlingDr | Pass 54 | Built: a Good Order, unpinned crew or HS. Missing: a squad through its inherent HS, five SMC as a crew, D5.43 attack effects while (un)hooking; limbering is not built at all. | audit |
| C10.12 | Unhooking | 180 | partly | GamePlanner.Guns.cs: PlanHookGun; GameProjector.cs: HookGun | BacklogPass8Tests.ATruckHooksUpAGunTowsItAndUnhooksIt; BacklogPass26Tests.ATruckEntersTowingAGunAndUnhooksItForItsCrew | Pass 54 | Built: half MP, crew disembarks free, crew and Gun TI, vehicle may go on, CA set as it unhooks. Missing: two-thirds for a circled M#, not-in-Bypass check, no fire before unhooking, unlimbering after. | audit |
| C10.13 | Ammo PP Reduction | 180 | partly | GamePlanner.Passengers.cs: PassengerCapacity, CapacityBar; GamePlanner.Guns.cs: PlanHookGun (capacity) | none found | Pass 54 | Built: 4 PP, 8 PP at 100mm or more, the empty-vehicle exception, refusal when Passengers would not fit. Missing: the dm 76-82mm mortar case (no dm mortars). No test of the capacity refusal was found. | audit |
| C10.2 | (Un)limbering | 180 | refused | GamePlanner.Guns.cs: PlanHookGun; GamePlanner.Vehicles.cs: TowSetupBar; GamePlanner.Victory.cs: exit push | none found | Pass 54 | A non-QSU Gun is refused for hook-up, setup in tow, and a push off the map (ruling R26.1). The on-map push in GamePlanner.Movement.cs has no QSU check; both catalog Guns are QSU so nothing reaches it. | audit |
| C10.21 | (none) | 180 | not built |  |  | Pass 54 | Limbering and unlimbering in a fire phase, the limbered side, Malfunction counters on limbered Guns. | audit |
| C10.22 | (none) | 180 | not built |  |  | Pass 54 | CA change with (un)limbering and the TI after unlimbering. | audit |
| C10.23 | Quick Set-Up (QSU) | 180 | built | GamePlanner.Guns.cs: PlanHookGun (QuickSetUp); ScenarioA1FireReference.cs: QuickSetUp | BacklogPass8Tests.ATruckHooksUpAGunTowsItAndUnhooksIt |  | A QSU Gun is hooked and unhooked in its normal state; both catalog Guns are QSU. | audit |
| C10.24 | Limbered Fire (LF) | 180 | not built |  |  | Pass 54 | Limbered Fire; no Gun has a limbered side. | audit |
| C10.25 | Restricted Fire, No Movement (RFNM) | 180 | not built |  |  | Pass 54 | The catalog carries the asl:rfnm trait (false on both Guns) but no code reads it: no push bar, no Case A, E, J4, L bars, no doubled Case J. | audit |
| C10.26 | No Movement (NM) | 180 | not built |  |  | Pass 54 | The catalog carries the asl:nm trait (false on both Guns) but no code reads it. | audit |
| C10.3 | Pushing | 181 | partly | GamePlanner.Movement.cs: PlanMove (push); GamePlanner.Guns.cs: PushPlan; GamePlanner.Victory.cs: exit push; GameProjector.cs: Manhandle; GamePlanner.Fire.cs: HazardousMovement | BacklogPass8Tests.ACrewPushesItsGunOrStaysOnAHighManhandlingDr; BacklogPass8Tests.APushMakesTheGunAndCrewTiYetTheCrewPushesOnAndTheBoreSightingIsLost; BacklogPass26Tests.ACrewPushesItsGunOffTheMap | Pass 54 | Ruling R8.6. Built: double MF, DR against M# with +MF and -2 road, three outcomes, TI, no Assault Movement, no road bonus, no Bypass, no Minimum Move, Hazardous Movement. Missing: any terrain but Open Ground or grain (TEM DRM), +3 mud or deep snow, extra pushers, Low Ammo -2, Labor counter, CA change while pushing, PP check, NM or RFNM bar, circled M#. | audit |
| C10.31 | (none) | 181 | partly | GamePlanner.Guns.cs: PlanHookGun | none found | Pass 54 | A hook-up after a push in the same MPh is admitted (the crew's TI is not checked), but without the rule's conditions: the push DR below the M#, the crew's MF left, the vehicle's MP limit across both. | audit |
| C10.4 | Trailers | 181 | not built |  |  | Pass 55 | No trailer counter or towing vehicle with one. | audit |
| C10.41 | (none) | 181 | not built |  |  | Pass 55 | Trailer as a separate target, +3 size, rear hull hits, miss by one, immobilized trailer. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C10.5 | En Portee | 181 | not built |  |  | Pass 55 | En Portee: no portee vehicle, no side record, no PP rule. | audit |
| C10.51 | (Un)loading | 181 | not built |  |  | Pass 55 | Loading and unloading a porteed Gun. | audit |
| C10.52 | Wreck | 181 | not built |  |  | Pass 55 | Portee wreck and elimination. | audit |
| C10.53 | CA & Gunshields | 181 | not built |  |  | Pass 55 | Porteed Gun CA and gunshield effect on Passengers. | audit |
| C10.54 | Portee Fire | 181 | not built |  |  | Pass 55 | Portee Fire. | audit |

#### C11 Guns as Targets (pages 181 to 182)

9 rows: 6 partly, 1 refused, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C11 | Guns as Targets | 181 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| C11.1 | Near Miss | 181 | not applicable |  |  |  | Definition of Near Miss and Direct Hit; the mechanics are C11.4. | audit |
| C11.2 | Emplacement | 181 | partly | ScenarioA1OrdnanceCalculator.cs: Run (case-p, case-q:emplacement); ScenarioA1FireCalculator.cs: emplacement TEM; LiveOrdnance.cs: Emplaced; GamePlanner.Ordnance.cs: GunAt | ScenarioA1Pass8Tests.AnEmplacedGunAddsItsTargetSizeAndEmplacementToTheToHitDr; BacklogPass8Tests.InfantryFireAtAnEmplacedGunsCrewTakesPlusTwoAndAnHeKiaDestroysTheGun | Pass 50 | Built: Infantry Target Type with Target Size and +2 Emplacement as To Hit DRM, +2 TEM on the IFT, never with another positive TEM. Missing: the Area Target Type option, the owner's choice (the game takes the higher), the terrains where no Gun is Emplaced, different TH# for other units in the hex (a crew with others is refused), OBA. | audit |
| C11.3 | (none) | 182 | partly | GameProjector.cs: UnemplacedGuns on push and hook; LiveOrdnance.cs: Emplaced | BacklogPass8Tests.APushMakesTheGunAndCrewTiYetTheCrewPushesOnAndTheBoreSightingIsLost | Pass 50 | Built: lost on a move, a hook-up, or non-crew manning. Missing: forfeiting Wall Advantage, a voluntary non-Emplaced setup, RCL; a Gun first manned by a HS is not recorded as lost for good. | audit |
| C11.4 | Direct Hit | 182 | partly | ScenarioA1OrdnanceCalculator.cs: Run (GunTargetFate); ScenarioA1FireCalculator.cs: gunshield on a Near Miss; GamePlanner.Ordnance.cs: AddOrdnanceEvents | ScenarioA1Pass8Tests.AnHeHitDestroysTheGunOnAKiaAndItsGunshieldProtectsTheCrewFromANearMiss; BacklogPass8Tests.InfantryFireAtAnEmplacedGunsCrewTakesPlusTwoAndAnHeKiaDestroysTheGun | Pass 50 | Ruling R8.3. Built for a direct HE hit on a crew alone in its Location: KIA destroys, K malfunctions with Casualty Reduction, CH destroys, +2 gunshield on a Near Miss. Missing: an unattended Gun, Random Selection among several units, OBA and DC, the +1 gunshield against Indirect Fire. | audit |
| C11.5 | Gunshields | 182 | partly | GamePlanner.Ordnance.cs: GunAt; ScenarioA1FireCalculator.cs: GunshieldFaces, gunshield DRM | ScenarioA1Pass8Tests.InfantryFireAtAGunCrewTakesItsGunshieldOrEmplacementAndACrewFiresItsOwnFp | Pass 50 | Built: AT and INF Guns, a Good Order crew only, fire from within the CA, not from its own hex, not while moving or pushing, +2, never with a positive TEM, none against a FT. Missing: +1 against Indirect Fire, the target's own choice between gunshield and TEM (the game takes the higher). | audit |
| C11.51 | (none) | 182 | partly | ScenarioA1FireCalculator.cs: flamethrower branch (no TEM, no gunshield); GamePlanner.Ordnance.cs: GunAt (any firer within the CA) | none found | Pass 50 | Built: no gunshield against a FT attack, and a FG faces the gunshield when any firer is within the CA. Missing: HEAT at a Gun (refused, no HE Equivalency), Gun destruction by FT or MOL through A9.74, MOL in a FG. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C11.52 | AP | 182 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (gun-outside for non-HE at a Location) | none found | Pass 50 | AP, APCR, or HEAT at a Location with no named vehicle is refused; an ATR as ordnance at a Gun is refused (target-type-outside). The A9.74 Gun destruction by MG or ATR small-arms fire is not built. | audit |
| C11.6 | Gun Destruction Table | 182 | partly | ScenarioA1OrdnanceCalculator.cs: Run (GunTargetFate); GameProjector.cs: towed Gun lost with vehicle | ScenarioA1Pass8Tests.AnHeHitDestroysTheGunOnAKiaAndItsGunshieldProtectsTheCrewFromANearMiss | Pass 50 | Built: the Ordnance column for HE (KIA, K, CH) and note 1 (in tow). Missing: the MG/IFE/Small Arms/FT/MOL/OVR column (Random SW/Gun Destruction), the DC column, Bomb and OBA, notes 3 and 5. | audit |

#### C12 Recoilless Rifles (RCL) (pages 182 to 183)

10 rows: 9 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C12 | Recoilless Rifles (RCL) | 182 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| C12.1 | (none) | 182 | not built |  |  | Pass 54 | No RCL counter; no code. | audit |
| C12.2 | (none) | 182 | not built |  |  | Pass 54 | No RCL; captured-use penalty for a squad or HS. | audit |
| C12.21 | (none) | 182 | not built |  |  | Pass 54 | Two SMC firing a RCL. | audit |
| C12.22 | (none) | 183 | not built |  |  | Pass 54 | RCL To Hit, Case A for German RCL only, US 57mm in the AFPh, concealment loss. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C12.23 | (none) | 183 | not built |  |  | Pass 54 | RCL bars on firing from a building, rubble, entrenchment, pillbox, cave, vehicle. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C12.24 | (none) | 183 | not built |  |  | Pass 54 | RCL acquisition and Bore Sighting limits. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| C12.3 | Backblast Zone | 183 | not built |  |  | Pass 54 | Backblast zone geometry. | audit |
| C12.31 | (none) | 183 | not built |  |  | Pass 54 | Backblast TI and the 1 FP attack on units in the zone. | audit |
| C12.4 | (none) | 183 | not built |  |  | Pass 54 | Backblast Flame on an Original 11. | audit |

#### C13 Light Anti-Tank Weapons (LATW) (pages 183 to 185)

48 rows: 7 built, 1 built with a deviation, 8 partly, 3 refused, 28 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| C13 | Light Anti-Tank Weapons (LATW) | 183 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| C13.1 | LATW | 183 | partly | ScenarioA1ArmorCalculator.cs: Run (case-c3:afph, LATW DRM set); ScenarioA1OrdnanceCalculator.cs: Outside (no Area Target Type) | ScenarioA1Pass9Tests.APanzerfaustTakesCaseC3AndAnOriginal12ReducesItsFirer; ScenarioA1Pass9bTests.APanzerschreckTakesTheBackblastAndAnAtrDoesNot | Pass 45 | Built for the PF, PSK, and ATR: To Hit then To Kill, the LATW DRM, +2 in the AFPh, never the Area Target Type. Missing: the Opportunity Fire exception to Case C3 (an Opportunity Firer in the AFPh still takes +2), and the BAZ, PIAT, MOL-Projector, ATMM, and PFk. | audit |
| C13.2 | Anti-Tank Rifle (ATR) | 183 | built | ScenarioA1ArmorCalculator.cs: Run (black row for an ATR); ScenarioA1ArmorReference.cs: GunSize ATR | ScenarioA1Pass9bTests.AnAtrHitsOnTheBlackVehicleRowAndKillsWithTheRussianAtrTk; BacklogPass9Tests.AnAtrHitsATankOnTheBlackVehicleRowWithAp |  | Ruling R9.10; the ATR's values are manufactured (sheet MFG). Captured use is not built. | audit |
| C13.21 | Usage | 183 | built with a deviation | LiveOrdnance.cs: FromState (asl:latw); ScenarioA1OrdnanceCalculator.cs: Outside (crew-outside) | BacklogPass9Tests.AnAtrFiresOnceAPhaseAndItsSquadStillFiresItsInherentFp | Pass 54 | Ruling R9.10: only Infantry of the ATR's own nationality fire it, and a SMC fires it only at a vehicle. | audit |
| C13.22 | Range | 183 | built | ScenarioA1OrdnanceCalculator.cs: Outside (RangeMaximum); ScenarioA1ArmorReference.cs: CaseD | ScenarioA1Pass9bTests.AnAtrHitsOnTheBlackVehicleRowAndKillsWithTheRussianAtrTk |  | 12 hexes; the 25mm-or-less Case D row. | audit |
| C13.23 | vs Guns | 183 | refused | ScenarioA1OrdnanceCalculator.cs: Outside (target-type-outside: a LATW needs a vehicle target) | none found | Pass 50 | An ATR's ordnance shot at a Gun is refused; ruling R9.10 records it as not built. Needs C11.52 and HE Equivalency. | audit |
| C13.24 | vs Personnel | 183 | partly | ScenarioA1FireCalculator.cs: ATR in a fire group, no Long Range, no Residual FP; FireRange.cs: Band | BacklogPass9Tests.AnAtrAddsOneFpToItsSquadsFireGroup | Pass 54 | Built: 1 FP Small Arms Fire in a FG, 12 hexes at most, no Residual FP. Missing: the 20L ATR on the Infantry Target Type with AP HE Equivalency (no 20L ATR in the catalog); a SMC or hero firing it as Small Arms is refused. | audit |
| C13.25 | Leadership | 183 | built | ScenarioA1OrdnanceCalculator.cs: Leadership (To Hit only); ScenarioA1ArmorCalculator.cs: Kill (no leadership) | none found |  | A leader's modifier is on the To Hit DR and never on the To Kill DR. No test of an ATR shot with a director was found. | audit |
| C13.26 | Malfunction | 183 | built | ScenarioA1OrdnanceCalculator.cs: Breakdown; GamePlanner.Rally.cs: PlanRepair | ScenarioA1Pass9bTests.AnAtrHitsOnTheBlackVehicleRowAndKillsWithTheRussianAtrTk; ScenarioA1Pass9bTests.InexperiencedHandsLowerTheBreakdownAndXNumberByOne |  | B11 malfunction (one lower for Inexperienced), repair number 2 in the catalog read by the general SW repair. No test of an ATR repair was found. | audit |
| C13.3 | Panzerfaust (PF) | 183 | partly | LiveOrdnance.cs: Panzerfaust; ScenarioA1OrdnancePackage.cs: Panzerfaust definition; ScenarioA1ArmorCalculator.cs: Run | BacklogPass9Tests.APanzerfaustCheckGivesAShotThatBurnsAT34AndCountsAgainstTheUsageLimit; BacklogPass9Tests.NoPanzerfaustBeforeOctober1943 | Pass 54 | Built: the German PF from October 1943, C3 To Hit, HEAT To Kill at a vehicle. Missing: Finnish, Romanian, and Hungarian use, the PFk by SSR. | audit |
| C13.31 | Usage | 183 | partly | ScenarioA1OrdnanceCalculator.cs: PanzerfaustCheck, Outside; LiveOrdnance.cs: Panzerfaust, PanzerfaustAllowance, SquadSupportRefusal; GamePlanner.Ordnance.cs: FirerEffectEvents | ScenarioA1Pass9Tests.APanzerfaustCheckMayGiveNoShotOrPinOrBreakItsFirer; BacklogPass9Tests.APanzerfaustCheckOf6PinsTheUnit; ScenarioA1Pass9Tests.APanzerfaustIsBoundByDateRangeTargetAndUsage | Pass 50 | Rulings R9.7, R9.8. Built: the PF Check and its drm (1945, HS or crew, SMC, CX), the Original 6, SW use, a squad's second check, no check in Subsequent First Fire, the usage limit. Missing: any target but an AFV (refused, so the +1 drm, Random Selection, and the choice of unit never arise), the Aug-Sept 1943 +1. | audit |
| C13.311 | Optional Usage | 183 | not built |  |  | Left out: an optional rule by the rulebook's own mark | Optional rule (asterisk): allocated PF carried and recorded per unit, no PF Check. | audit |
| C13.32 | Range | 183 | partly | ScenarioA1OrdnanceCalculator.cs: PanzerfaustRange | ScenarioA1Pass9Tests.APanzerfaustIsBoundByDateRangeTargetAndUsage | Pass 54 | Built: 1 hex before June 1944, 2 to December 1944, 3 after. Missing: the PFk's one-hex range. | audit |
| C13.33 | Range Effects | 184 | built | ScenarioA1ArmorCalculator.cs: Run (pf-range) | ScenarioA1Pass9Tests.APanzerfaustCheckOf1To3GivesAShotAtTh10LessTwoPerHexAndHeatTk31 |  | Basic TH# 10, less 2 per hex. | audit |
| C13.34 | TK# | 184 | partly | ScenarioA1OrdnancePackage.cs: Panzerfaust (HeatRow PF (Oct43)); ScenarioA1ArmorReference.cs: BasicTk | ScenarioA1Pass9Tests.APanzerfaustCheckOf1To3GivesAShotAtTh10LessTwoPerHexAndHeatTk31 | Pass 54 | Built: TK# 31. Missing: the PFk's TK# 22. | audit |
| C13.35 | Leadership | 184 | built | ScenarioA1OrdnanceCalculator.cs: Leadership, SupportWeaponOutside (director); LiveOrdnance.cs: Panzerfaust (director) | none found |  | One leader in the firer's Location directs a PF's To Hit DR as his only direction that phase. The directing test found uses a mortar, not a PF. | audit |
| C13.36 | Malfunction | 184 | built | ScenarioA1ArmorCalculator.cs: Run (casualty on 12, or 11 Inexperienced), Kill (dud on 12); GamePlanner.Ordnance.cs: FirerEffectEvents | BacklogPass9Tests.APanzerfaustOriginal12ReducesTheSquadToAHsThatHasFired; ScenarioA1Pass9Tests.AnInexperiencedPanzerfaustFirerIsReducedOnAnOriginal11 |  | No Breakdown or Repair; the Casualty Reduction and the Dud. The IFT case never arises (no Infantry target). | audit |
| C13.4 | Bazooka (BAZ) | 184 | not built |  |  | Pass 54 | No BAZ counter; no code. | audit |
| C13.41 | Usage | 184 | not built |  |  | Pass 54 | BAZ usage by any unbroken Infantry MMC. | audit |
| C13.42 | To Hit | 184 | not built |  |  | Pass 54 | BAZ To Hit Tables on the counter backs (the same table mechanism serves the PSK). | audit |
| C13.43 | To Kill | 184 | not built |  |  | Pass 54 | BAZ 43 and BAZ 44+ columns of the HEAT To Kill Table; the 8 FP attack on unarmored targets. | audit |
| C13.44 | Leadership | 184 | not built |  |  | Pass 54 | BAZ leadership (the PSK takes a director through C13.48). | audit |
| C13.45 | SMC Usage | 184 | not built |  |  | Pass 54 | Two SMC or a lone hero firing a BAZ; also missing for the PSK (ruling R9.11). | audit |
| C13.46 | WP | 184 | not built |  |  | Pass 54 | BAZ 45 WP. | audit |
| C13.47 | Malfunction | 184 | not built |  |  | Pass 54 | BAZ removal on its X#; built only for the PSK (GamePlanner.Ordnance.cs: AddOrdnanceEvents). | audit |
| C13.48 | Panzerschreck (PSK) | 184 | partly | ScenarioA1ArmorCalculator.cs: Run (ToHitTable, PSK row); ScenarioA1OrdnanceCalculator.cs: SupportWeaponOutside (September 1943); GamePlanner.Ordnance.cs: AddOrdnanceEvents (removal) | ScenarioA1Pass9bTests.APanzerschreckReadsItsOwnTableAndKillsWithHeatTk26; BacklogPass9Tests.APanzerschreckHitsOnItsOwnTableAndIsRemovedOnItsXNumber; BacklogPass9Tests.NoPanzerschreckBeforeSeptember1943 | Pass 54 | Ruling R9.11; manufactured table 10, 9, 8, 7. Built: own To Hit Table, TK# 26, no WP, X# removal, from September 1943, a director. Missing: the 12 FP attack on Infantry and unarmored non-vehicle targets, SMC usage (a lone SMC is refused), captured use. | audit |
| C13.5 | Molotov Projector | 184 | not built |  |  | Pass 54 | No MOL-Projector counter; no code. | audit |
| C13.51 | Usage | 184 | not built |  |  | Pass 54 | MOL-Projector usage and fire-order restrictions. | audit |
| C13.52 | CH | 184 | not built |  |  | Pass 54 | MOL-Projector CH on an Original 2. | audit |
| C13.53 | Effects | 184 | not built |  |  | Pass 54 | MOL-Projector effects. | audit |
| C13.54 | vs AFV | 184 | not built |  |  | Pass 54 | MOL-Projector against an AFV. | audit |
| C13.55 | vs Unarmored Vehicle | 184 | not built |  |  | Pass 54 | MOL-Projector against an unarmored vehicle. | audit |
| C13.56 | vs Infantry/Gun | 184 | not built |  |  | Pass 54 | MOL-Projector against Infantry or a Gun. | audit |
| C13.57 | vs Terrain | 184 | not built |  |  | Pass 54 | MOL-Projector Flame in Burnable Terrain. | audit |
| C13.58 | Smoke | 184 | not built |  |  | Pass 54 | MOL-Projector Dispersed Smoke. | audit |
| C13.59 | Malfunction | 184 | not built |  |  | Pass 54 | MOL-Projector malfunction and X12. | audit |
| C13.6 | PIAT | 185 | not built |  |  | Pass 54 | No PIAT counter; no code. | audit |
| C13.61 | Height Restrictions | 185 | not built |  |  | Pass 54 | PIAT height restriction. | audit |
| C13.62 | SMC Usage | 185 | not built |  |  | Pass 54 | PIAT SMC usage. | audit |
| C13.63 | Malfunction | 185 | not built |  |  | Pass 54 | PIAT B10 and repair. | audit |
| C13.7 | Anti-Tank Magnetic Mine (ATMM) | 185 | not built |  |  | Pass 54 | No ATMM Check in Close Combat against a vehicle; nothing in GamePlanner.VehicleCloseCombat.cs. | audit |
| C13.71 | Leadership | 185 | not built |  |  | Pass 54 | ATMM and leadership. | audit |
| C13.72 | (none) | 185 | not built |  |  | Pass 54 | ATMM and other units in the hex. | audit |
| C13.73 | SMC | 185 | not built |  |  | Pass 54 | ATMM by a SMC. | audit |
| C13.74 | Malfunction | 185 | not built |  |  | Pass 54 | ATMM malfunction on an Original CC DR of 12. | audit |
| C13.8 | Backblast | 185 | partly | GamePlanner.Ordnance.cs: SupportWeaponMapFacts; ScenarioA1ArmorCalculator.cs: Run (case-c3:backblast); ScenarioA1OrdnanceCalculator.cs: SupportWeaponOutside (pinned); LiveOrdnance.cs: FromState (latw-passenger) | ScenarioA1Pass9bTests.APanzerschreckTakesTheBackblastAndAnAtrDoesNot; ScenarioA1Pass9Tests.SpottedFireIsDesignatedInThePfphOrDfphAndAPinnedPanzerfaustFirerInABuildingMayNotFire | Pass 45 | Built for the PF and PSK: +2 from a ground-level building, refused above ground level, by a pinned firer there, or from a vehicle. Missing: rubble (a shot from rubble takes no Case C3), the Factory and rooftop exception, the Opportunity Fire alternative, pillbox, cave, sewer, the bar on a target two or more levels higher when adjacent or directly above. | audit |
| C13.81 | Desperation | 185 | refused | GamePlanner.Ordnance.cs: SupportWeaponMapFacts (panzerfaust-backblast); ScenarioA1OrdnanceCalculator.cs: SupportWeaponOutside | ScenarioA1Pass9Tests.SpottedFireIsDesignatedInThePfphOrDfphAndAPinnedPanzerfaustFirerInABuildingMayNotFire | Pass 54 | Desperation fire is refused with a reason (rulings R9.8, R9.11); the 1 FP attack on the firing Location is not built. | audit |
| C13.9 | Shape-Charge Weapons (SCW) | 185 | refused | LiveOrdnance.cs: Panzerfaust (panzerfaust-target); ScenarioA1OrdnanceCalculator.cs: Outside (target-type-outside) | ScenarioA1Pass9Tests.APanzerfaustIsBoundByDateRangeTargetAndUsage | Pass 50 | A SCW's HEAT at Personnel (C8.31 HE Equivalency) is not built: a PF or PSK shot with no vehicle target is refused. The rest of the rule is a definition. | audit |

### Chapter D: Vehicles

#### D. Introduction (page 192)

12 rows: 2 built, 3 partly, 2 not built, 5 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D.1 | Vehicle Listings | 192 | not applicable |  |  |  | Explains the Chapter H Vehicle Listings and that a Vehicle Note overrides a rule; nothing to implement beyond the catalog data. | audit |
| D.2 | Animal Movement | 192 | not built |  |  | Pass 55 | No animal-drawn transport, horses, or cavalry in the catalog; no MF-moving vehicle logic. | audit |
| D.3 | Prep Fire | 192 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle (Prep Fire bar; Unload excepted); ScenarioA1FireCalculator.cs: VehicleFireOutside; GameProjector.cs: StepVehicle | VehicleStepsTests.TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase |  | A vehicle that Prep Fired neither moves nor fires again; its Passengers may still unload. | audit |
| D.4 | CA | 192 | not applicable |  |  |  | Rule of interpretation (a CA change means VCA and TCA alike); no mechanics of its own. | audit |
| D.5 | Secret DR/dr | 192 | not applicable |  |  |  | Secret DR/dr house procedure for a table without a judge; the program rolls and records dice itself. | audit |
| D.5A | Dice Cup | 192 | not applicable |  |  |  | Physical dice cup procedure the program replaces. | audit |
| D.5B | Cards | 192 | not applicable |  |  |  | Physical card deck procedure the program replaces. | audit |
| D.6 | Vehicular-Target Hits vs PRC | 192 | partly | ScenarioA1FireCalculator.cs: vehicle effects (crew attacked without FFNAM or FFMO); GamePlanner.Ordnance.cs: ordnance plan (a vehicle in the target Location must be named) | VehicleStepsTests.ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne | Pass 51 | Built for an Inherent crew. Passengers and Riders take no fire at all (backlog, plan pass 33), so the PRC half is missing. | audit |
| D.7 | Mobile/Immobile | 192 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle, Halted; GameProjector.cs: StepVehicle | VehicleStepsTests.U29ATruckMovesOneMpExpenditureAtATimeAndDefensiveFireImmobilizesIt |  | Immobile states read: immobilized, bogged, Mired, Abandoned, Stunned, Shocked. A broken Inherent crew does not exist in the model. Prep Fire and TI bar the move without making the vehicle Immobile. | audit |
| D.8 | Collateral Attacks | 192 | partly | ScenarioA1FireCalculator.cs: vehicle effects (General Collateral on a CE crew); GamePlanner.Vehicles.cs: VehicleStepPlan (Residual FP) | VehicleStepsTests.ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne | Pass 51 | Only the General form against an AFV's CE crew is built. Missing: Specific Collateral Attacks after ordnance, MG To Kill, mines, DC, MOL; Passengers and Riders; the Collateral Attack Table's other rows. | audit |
| D.8A | Specific | 192 | not built |  |  | Pass 50 | An ordnance hit that does not destroy the vehicle does nothing to its CE crew; no Specific Collateral Attack exists. | audit |
| D.8B | General | 192 | partly | ScenarioA1FireCalculator.cs: vehicle effects (+2 CE DRM, same Original DR, every vehicle in the Location) | VehicleStepsTests.ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne | Pass 51 | Built for AFV crews against small arms, MG, and Residual FP. Missing: Passengers and Riders, OBA, Infantry and Area Target Type hits on a Location holding a vehicle (refused, R25.10, R9.3). | audit |

#### D1 Vehicle Counters (pages 193 to 195)

42 rows: 27 built, 8 partly, 5 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D1 | Vehicle Counters | 193 | not applicable |  |  |  | Heading; the bracketed text describes the physical counter. | audit |
| D1.1 | Movement Type | 193 | partly | GamePlanner.VehicleTerrain.cs: VehicleTerrainHalfMp, MovementTypeOf; ScenarioA1FireReference.cs: MovementType, MovementPoints, MechanicallyUnreliable | BacklogPass11Tests.WallsHedgesAndHillsCostWhatTheTerrainChartSays | Pass 53 | Three of the five movement types and red MP are built. Missing: motorcycle and armored car types, the amphibious MP superscript. | audit |
| D1.11 | Motorcycle | 193 | not built |  |  | Pass 53 | No motorcycle counter or movement logic. | audit |
| D1.12 | Armored Car | 193 | not built |  |  | Pass 53 | No armored car in the catalog and no armored-car column in the vehicle terrain costs. | audit |
| D1.13 | Fully Tracked | 193 | built | GamePlanner.VehicleTerrain.cs: VehicleTerrainHalfMp, VehicleCost | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck |  | Fully-tracked costs over the reviewed terrain list. | audit |
| D1.14 | Half Tracked | 193 | built | GamePlanner.VehicleTerrain.cs: VehicleTerrainHalfMp, VehicleCost | BacklogPass11Tests.AHalftrackCrossingAHedgeIntoWoodsTakesBothBogDrs |  | Half-tracked costs over the reviewed terrain list. | audit |
| D1.15 | Truck | 193 | built | GamePlanner.VehicleTerrain.cs: VehicleTerrainHalfMp, VehicleCost | VehicleStepsTests.U29ATruckMovesOneMpExpenditureAtATimeAndDefensiveFireImmobilizesIt |  | Truck costs over the reviewed terrain list. An armored car with truck-type MP (BA-20) is not in the catalog. | audit |
| D1.2 | Armor Status | 193 | built | GamePlanner.Vehicles.cs: IsAfv, IsArmoredVehicle; ScenarioA1FireCalculator.cs: vehicle effects (A7.307) | VehicleStepsTests.ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne |  | An AFV is unharmed by small arms on the IFT. The unarmored Target Facing or Aspect exception is not built (see D1.22). | audit |
| D1.21 | Unarmored | 193 | built | ScenarioA1FireReference.cs: Unarmored; ScenarioA1ArmorCalculator.cs: Kill (unarmored Final TK#) | ScenarioA1Pass7Tests.AnUnarmoredVehicleTakesTheUnarmoredTk |  | Trucks are unarmored. Treating an AFV as unarmored (D5.311, B28.42) belongs to those rules. | audit |
| D1.22 | Partially Armored | 193 | not built |  |  | Pass 53 | No partially armored vehicle in the catalog; ArmorFactor has no unarmored rear or turret-rear aspect. | audit |
| D1.23 | Open-Topped (OT) | 194 | built | ScenarioA1FireReference.cs: OpenTopped; GamePlanner.Vehicles.cs: IsClosedTopped; LiveFire.cs: CrewExposed | VehicleStepsTests.TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase |  | The SPW 251/1 is OT and CE unless marked BU. | audit |
| D1.24 | Close-Topped (CT) | 194 | built | GamePlanner.Vehicles.cs: IsClosedTopped; LiveFire.cs: CrewExposed | BacklogPass7Tests.AClosedToppedTanksCrewIsButtonedUpUnlessExposed |  | A CT AFV is BU unless a CE state is recorded (R7.11). | audit |
| D1.3 | Main Armament Type | 194 | partly | LiveOrdnance.cs: IsTank, FromState; ScenarioA1FireReference.cs: MainArmament | BacklogPass7Tests.ATankFiresApAtAnEnemyTanksSideAndBurnsIt | Pass 53 | A turreted Gun MA and the halftrack's MA AAMG are built. Missing: MA that is a FT, ATR, or ordinary MG; a bow-mounted MA firing outside its VCA. | audit |
| D1.31 | Fast Turret Traverse | 194 | built | ScenarioA1ArmorCalculator.cs: CaseA ("t"); GamePlanner.Ordnance.cs: OrdnanceMapFacts | ScenarioA1Pass7Tests.ATurretTurnCostsLessThanAGunAndKeepsTheRof |  | T type: +1 per hexspine (PzKpfw IIIH). | audit |
| D1.32 | Slow Turret Traverse | 194 | built | ScenarioA1ArmorCalculator.cs: CaseA ("st") | ScenarioA1Pass7Tests.ATurretTurnCostsLessThanAGunAndKeepsTheRof (through the RST T-34) |  | ST Case A (+2 then +1) exists; the catalog has no plain ST vehicle. | audit |
| D1.321 | Restricted Slow Traverse (RST) | 194 | partly | ScenarioA1OrdnanceCalculator.cs: Outside (RST MA fires only BU) | BacklogPass7Tests.AClosedToppedTanksCrewIsButtonedUpUnlessExposed | Pass 45 | The MA is barred while CE. The CMG is not: a CE RST tank still adds its MA base and CMG to an OVR and its CMG to CC (fault, see findings). | audit |
| D1.322 | One-Man Turret (1MT) | 194 | partly | ScenarioA1OrdnanceCalculator.cs: Outside (1MT MA fires only BU); ScenarioA1ArmorCalculator.cs: CaseA | none found | Pass 53 | Only the MA bar and the ST Case A exist. Missing: CMG bar while CE, automatic Recall on a Stun, never CE again. No 1MT vehicle in the catalog. | audit |
| D1.33 | Non-Turreted (NT) | 194 | partly | ScenarioA1ArmorCalculator.cs: CaseA (NT default); GamePlanner.Ordnance.cs: OrdnanceMapFacts (play.ordnance-vca refusal) | none found | Pass 53 | The NT Case A numbers exist, but a non-turreted MA that must pivot the vehicle is refused. No NT vehicle in the catalog. | audit |
| D1.34 | Secondary Armament | 194 | not built |  |  | Pass 53 | The catalog carries sa-mount and sa-caliber attributes; no code reads them. | audit |
| D1.4 | Identity & Ground Pressure | 194 | built | GamePlanner.VehicleTerrain.cs: VehicleBogDrm; ScenarioA1FireReference.cs: GroundPressure | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck |  | Ground Pressure feeds the Bog DRM. The ID letter is the unit id. | audit |
| D1.41 | Low Ground Pressure | 194 | built | GamePlanner.VehicleTerrain.cs: VehicleBogDrm (low: no DRM) | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck |  | The T-34 M41 is Low. | audit |
| D1.42 | Normal Ground Pressure | 194 | built | GamePlanner.VehicleTerrain.cs: VehicleBogDrm (none printed: +1) | BacklogPass11Tests.AHalftrackCrossingAHedgeIntoWoodsTakesBothBogDrs |  | Normal is the default when nothing is printed (R11.9). | audit |
| D1.43 | High Ground Pressure | 194 | built | GamePlanner.VehicleTerrain.cs: VehicleBogDrm (high: +2) | none found |  | Logic exists; no High Ground Pressure vehicle in the catalog. | audit |
| D1.5 | Towing (T#)/Portage (#PP) | 194 | built | GamePlanner.Vehicles.cs: TowSetupBar, PassengerSetupBar; GamePlanner.Passengers.cs: CapacityBar | BacklogPass26Tests.ATruckEntersTowingAGunAndUnhooksItForItsCrew |  | T# against the Gun's M# and PP capacity are checked. The towing and Passenger rules themselves are C10 and D6. | audit |
| D1.6 | Armor Factor (AF) | 194 | built | ScenarioA1ArmorReference.cs: ArmorScale, ArmorFactor | ScenarioA1Pass7Tests.ThePanzerMeetsTheT34sArmor |  | The eleven AF values and the four AF of an AFV match the PDF. | audit |
| D1.61 | Front Armor | 194 | built | ScenarioA1ArmorReference.cs: ArmorFactor (FrontAf) | ScenarioA1Pass7Tests.ThePanzerMeetsTheT34sArmor |  | Front hull AF, and the turret's unless superior or inferior. | audit |
| D1.62 | Side/Rear Armor | 194 | built | ScenarioA1ArmorReference.cs: ArmorFactor (SideAf) | ScenarioA1Pass7Tests.TheRearFacingAddsOneAndACriticalHitDoublesTheBasicTk |  | Side and rear share one AF. | audit |
| D1.63 | Superior Turret | 194 | built | ScenarioA1ArmorReference.cs: ArmorFactor ("superior") | none found for the superior step (the PzKpfw IIIH side turret uses it) |  | One step up the AF scale. | audit |
| D1.64 | Inferior Turret | 194 | built | ScenarioA1ArmorReference.cs: ArmorFactor ("inferior") | ScenarioA1Pass7Tests.ATurretHitMeetsTheTurretArmorAndTheToKillDrEliminates |  | One step down the AF scale. | audit |
| D1.7 | Target Size | 194 | built | ScenarioA1ArmorCalculator.cs: Run (case-p) | ScenarioA1Pass7Tests (the halftrack and GAZ-MM are small) |  | All five sizes map to a Case P DRM on the Vehicle Target Type. | audit |
| D1.71 | Very Large | 194 | built | ScenarioA1ArmorCalculator.cs: Run (very-large: -2) | none found |  | Logic exists; no Very Large vehicle in the catalog. | audit |
| D1.72 | Large | 194 | built | ScenarioA1ArmorCalculator.cs: Run (large: -1) | none found |  | Logic exists; no Large vehicle in the catalog. | audit |
| D1.73 | Average | 194 | built | ScenarioA1ArmorCalculator.cs: Run (no DRM) | BacklogPass7Tests.ATankFiresApAtAnEnemyTanksSideAndBurnsIt |  | Both tanks are Average. | audit |
| D1.74 | Small | 194 | built | ScenarioA1ArmorCalculator.cs: Run (small: +1) | none found by name |  | SPW 251/1 and GAZ-MM are Small in the catalog. | audit |
| D1.75 | Very Small | 195 | built | ScenarioA1ArmorCalculator.cs: Run (very-small: +2) | none found |  | Logic exists; no Very Small vehicle in the catalog. | audit |
| D1.76 | Concealment | 195 | not applicable |  |  |  | Pure cross-reference to A12.2. | audit |
| D1.8 | Vehicular MG/FT | 195 | partly | ScenarioA1FireReference.cs: BowMg, CoaxialMg, AntiAircraftMg | BacklogPass11Tests.ATankOverrunsASquadAndTheSquadReactsInCc | Pass 33 | The b/c/a FP are read from the catalog. A vehicular FT (mount, FP, X#, range) is not built. | audit |
| D1.81 | Bow MG (BMG) | 195 | partly | ScenarioA1FireCalculator.cs: OverrunWeapons | BacklogPass11Tests.ATankOverrunsASquadAndTheSquadReactsInCc | Pass 33 | The BMG only adds FP to an OVR. Missing: IFT fire within the VCA, Normal Range 8, hull RMG, Fixed-Mount +1. | audit |
| D1.82 | Coaxial MG (CMG) | 195 | partly | ScenarioA1FireCalculator.cs: OverrunWeapons; ScenarioA1VehicleCloseCombat.cs: VehicleFirepower | BacklogPass11Tests.AVehicleAttacksInfantryInCcAndHoldsItInMelee | Pass 33 | The CMG only adds FP to an OVR and CC. Missing: IFT fire through the TCA, Normal Range 12, loss of Acquisition, turret Rear MG, CMG VCA Only. | audit |
| D1.83 | Anti-Aircraft MG (AAMG) | 195 | built | ScenarioA1FireCalculator.cs: VehicleFireOutside, VehicleFirepower; LiveFire.cs: CrewExposed | VehicleStepsTests.TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase |  | The MA AAMG fires in any direction with no CA, needs a CE crew, Normal Range 8. A restricted-CA AAMG and a Hero firing it are not built; no tank AAMG in the catalog. | audit |
| D1.84 | Optional Armament | 195 | not built |  |  | Deferred with Chapter H: a DYO chart, purchase, or dr; a card's SSR names the value | No optional armament; DYO purchase (H1.41) is deferred. | audit |
| D1.9 | Wreck | 195 | built | GamePlanner.Wrecks.cs: WrecksAt, CoverAt; GamePlanner.Fire.cs and GamePlanner.Ordnance.cs: vehicle fate | BacklogPass6Tests.ADestroyedTruckBecomesAWreckThatCoversInfantryOfEitherSide |  | An eliminated vehicle becomes a wreck. A vehicle with no wreck side does not exist in the catalog. The counter-back list is a description. | audit |

#### D2 Vehicular Movement (pages 195 to 199)

34 rows: 15 built, 3 built with a deviation, 8 partly, 2 refused, 5 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D2 | Vehicular Movement | 195 | not applicable |  |  |  | Heading. | audit |
| D2.1 | (none) | 195 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle, PlanEndVehicle; GameProjector.cs: StepVehicle | VehicleStepsTests.U29ATruckMovesOneMpExpenditureAtATimeAndDefensiveFireImmobilizesIt |  | One MP expenditure at a time, each opening the DEFENDER's window; MP left are spent in the last hex as one expenditure (R5.15). Radioless AFV (D14) is not built. | audit |
| D2.11 | VCA Changes | 195 | partly | GamePlanner.Vehicles.cs: PlanMoveVehicle (Turn), VcaHexes; GamePlanner.VehicleTerrain.cs: VehicleTurnCost | VehicleStepsTests.MotionCountsTheVcaChangesAndAnUnseenEnemyIsRevealedByTheEntry | Pass 33 | Built: one MP per hexspine, two in woods, building, or rubble, Bog DR where needed, entry only within the VCA. Missing: VCA change by firing outside the CA, at the end of a fire phase, or after a Motion Attempt. | audit |
| D2.12 | Starting | 195 | built | GamePlanner.Vehicles.cs: PlanStartVehicle; GamePlanner.Passengers.cs: PlanVehicleEntry (no Start MP from off board) | VehicleStepsTests.U29ATruckMovesOneMpExpenditureAtATimeAndDefensiveFireImmobilizesIt |  | Start MP, forward unless Reverse is named. | audit |
| D2.13 | Stopping | 196 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle (Stop) | VehicleStepsTests.AVehicleEndsInMotionOnlyWhenItCannotStopOrMoveOnAndOneInMotionMustSpendAnMp |  | Stop MP, restart needs a new Start MP; still a moving target if it entered a hex or began in Motion. | audit |
| D2.14 | Wreck/Vehicles | 196 | built | GamePlanner.Wrecks.cs: WreckEntryHalfMp; GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass6Tests.AWreckRaisesAVehiclesEntryCost; BacklogPass11Tests.VehiclesShareALocationAndPayForTheVehiclesThere |  | One MP per wreck or vehicle, two by a road hexside, doubled in woods. The further doubling of Terrain Chart note D for other road types is not built. | audit |
| D2.15 | Minimum Move | 196 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle (minimumMove), FirstEntry | BacklogPass11Tests.AMinimumMoveEntersAHexCostingMoreThanTheAllotment |  | Forward Minimum Move ending in Motion, no VCA change (R11.5). The Reverse form is refused (D2.24). Stall notes are not built. | audit |
| D2.16 | Road Rate | 196 | partly | GamePlanner.VehicleTerrain.cs: VehicleCost (road branch) | none found | Pass 45 | The doubling reads the asl:bu condition only. A CT AFV that is BU by default (no condition recorded, R7.11) pays 1/2 MP. See findings, Faults. | me |
| D2.17 | Delay | 196 | not built |  |  | Pass 33 | No Delay expenditure. A stopped vehicle cannot spend single MP in place; MP left go as one lump at the end of its move (R5.15). | audit |
| D2.18 | (none) | 196 | not built |  |  | Pass 33 | The enter step always charges the computed cost; no argument declares a higher expenditure (the action's mp field is ESB only). | audit |
| D2.2 | Reverse Movement | 196 | built | GamePlanner.Vehicles.cs: PlanStartVehicle (reverse flag; towing bar); GamePlanner.VehicleTerrain.cs: VehicleMoveOptions | BacklogPass11Tests.ReverseMovementEntersARearHexAtFourTimesTheCostAndKeepsAStopMp |  | Reverse refused to a vehicle towing a Gun. Motorcycles and trailers do not exist. | audit |
| D2.21 | MP Cost | 196 | partly | GamePlanner.VehicleTerrain.cs: ReverseMultiplier, VehicleCost, VehicleBypass | BacklogPass11Tests.ReverseMovementEntersARearHexAtFourTimesTheCostAndKeepsAStopMp | Pass 53 | Built: x4 tracked, x3 truck, applied to the whole entry cost. Missing: x2 for armored cars and the counter's REV x # exceptions. | audit |
| D2.22 | Restrictions | 196 | built | GamePlanner.VehicleTerrain.cs: VehicleMoveOptions (rear hexes; Reverse VBM along the hexside they share) | BacklogPass11Tests.ReverseMovementEntersARearHexAtFourTimesTheCostAndKeepsAStopMp |  | Only the two rear hexes; only their shared hexside in Reverse Bypass. | audit |
| D2.23 | Start/Stop | 196 | built | GamePlanner.Vehicles.cs: PlanStartVehicle; GameProjector.cs: StepVehicle | BacklogPass11Tests.ReverseMovementEntersARearHexAtFourTimesTheCostAndKeepsAStopMp |  | A change of direction needs a Stop and a new Start. | audit |
| D2.24 | Reverse Motion | 196 | refused | GamePlanner.Vehicles.cs: MotionBar, PlanMoveVehicle (play.move-vehicle-reverse, play.move-vehicle-minimum) | BacklogPass11Tests.ReverseMovementEntersARearHexAtFourTimesTheCostAndKeepsAStopMp | Pass 53 | Reverse Motion and the Reverse Minimum Move are refused; a vehicle in Reverse must keep one MP to Stop (R11.1). | audit |
| D2.3 | Vehicular Bypass Movement (VBM) | 196 | built with a deviation | GamePlanner.VehicleTerrain.cs: VehicleBypass, Bypassable | BacklogPass11Tests.VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere; VbmIsRefusedAlongAHexsideTheObstacleTouches | Stands by its ruling | R11.2: cost is twice Open Ground with elevation and SMOKE doubled. The counter-edge clearance test and its dr are replaced by the map read's hexside terrain, and a failed clearance refuses the step rather than spending the MP and a Stop MP. | audit |
| D2.31 | Restrictions | 197 | built | GamePlanner.VehicleTerrain.cs: Bypassable, VehicleBypass | BacklogPass11Tests.VbmIsRefusedAlongAHexsideTheObstacleTouches |  | Rubble, a burning wreck's Blaze, non woods or building hexes, and a hexside already Bypassed are barred. The roadblock clause has no code (no roadblocks in the game); a terrain Blaze is read only from burning wrecks. | audit |
| D2.32 | VCA & Target Facing | 197 | partly | GamePlanner.VehicleTerrain.cs: LaneEnds, BypassTargetFacing; GamePlanner.Ordnance.cs: OrdnanceMapFacts | BacklogPass11Tests.VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere (position only) | Pass 52 | Built: straddling, CAFP, Target Facing from the firer's hex (R11.2). Missing: fire traced to and from the CAFP, the Bypass VCA for the vehicle's own fire, the C.5B case of a firer on a CAFP. | audit |
| D2.321 | Bypass TCA | 197 | not built |  |  | Pass 52 | A tank in Bypass fires its MA with its ordinary hexspine TCA and hexspine-counted Case A; nothing refuses it. See findings, Faults. | audit |
| D2.33 | VCA Changes | 197 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle (Turn, Stop), PlanEndVehicle; GamePlanner.VehicleTerrain.cs: TurnedAtCafp, VehicleMoveOptions | BacklogPass11Tests.VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere |  | One VCA change at the CAFP, then it must move on; never into the Bypassed obstacle. The Target Facing after that VCA change is read from the new VCA, not the position before it (see findings). | audit |
| D2.34 | Stationary Bypass | 198 | built | GamePlanner.Vehicles.cs: PlanEndVehicle; GameProjector.cs: StepVehicle (Straddling kept) | BacklogPass11Tests.VbmBypassesAWoodsHexAlongAClearHexsideAndMayEndThere |  | A vehicle may end its MPh in Bypass. | audit |
| D2.35 | Firing Restrictions | 198 | partly | GamePlanner.Ordnance.cs: OrdnanceMapFacts (non-turreted pivot refused) | none found | Pass 33 | The bar holds only because no bow weapon fires on the IFT and an NT pivot is refused everywhere. The changed TCA in Bypass (D2.321) is not built. | audit |
| D2.36 | PRC | 198 | refused | GamePlanner.Passengers.cs: PlanLoad, PlanUnload | BacklogPass26Tests.PassengersUnloadFromAStoppedVehicleAndBoardAgain (the ordinary case) | Pass 52 | Loading and unloading at a vehicle in Bypass are refused. The vehicle is held in the obstacle hex only, as the rule says. | audit |
| D2.37 | LOS | 198 | built with a deviation | GamePlanner.Fire.cs: FireMapFacts; GamePlanner.Ordnance.cs: OrdnanceMapFacts | none found | Pass 52 | R11.2: fire to and from a vehicle in Bypass is traced to the hex center, not the CAFP; the obstacle never blocks as the rule describes. Backlog row "The CAFP's LOS". | audit |
| D2.38 | TEM | 198 | partly | ScenarioA1FireCalculator.cs: vehicle effects (IFT attacks on a vehicle take no TEM) | none found | Pass 45 | Small arms and Residual FP give a vehicle no TEM anyway. Ordnance on the Vehicle Target Type gives a Bypassing vehicle the obstacle hex's Case Q TEM, against the rule and R11.2. See findings, Faults. | audit |
| D2.4 | Motion Status | 198 | built | GamePlanner.Vehicles.cs: MotionBar, PlanEndVehicle, PlanStartVehicle; GamePlanner.Fire.cs (no Prep Fire in Motion); GameProjector.cs: StepVehicle | VehicleStepsTests.AVehicleEndsInMotionOnlyWhenItCannotStopOrMoveOnAndOneInMotionMustSpendAnMp |  | Motion needs the named next hex to cost more than the MP left (R5.14); no Start MP from Motion; must spend an MP; no Prep Fire; never set up on board in Motion. | audit |
| D2.401 | (none) | 198 | not built |  |  | Pass 34 | No Motion Attempt action or dr (backlog, R11.18). | audit |
| D2.41 | Target Consequences | 198 | partly | ScenarioA1ArmorCalculator.cs: Run (Case J); ScenarioA1VehicleCloseCombat.cs (+2 vs Motion); GamePlanner.Wrecks.cs: Standing | ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr | Pass 33 | Built: Case J, the +2 in CC, no Hindrance or TEM from a Motion vehicle. Missing: the +2 for DC and MOL against a vehicle (those attacks on AFVs are not built). | audit |
| D2.42 | Firing Consequences | 198 | partly | ScenarioA1FireCalculator.cs: VehicleFirepower (motion-fire halving); ScenarioA1OrdnanceCalculator.cs: Outside (MA in Motion refused) | BacklogPass6Tests.AHalftrackFiresAsDefensiveFirstFireAndAsBoundingFirstFire; ScenarioA1Pass7Tests.AMovingFirerIsOutside | Pass 33 | The MG halving is built. Ordnance in Motion (Case C4) is refused. Passengers' and Riders' Motion Fire is not built. | audit |
| D2.5 | Excessive Speed Breakdown (ESB) | 198 | built | GamePlanner.Vehicles.cs: PlanEsb | BacklogPass11Tests.EsbAddsMpOnceOrImmobilizesTheVehicle; AFailedEsbImmobilizesTheVehicleAndEndsItsMove |  | R11.3 limits the attempt to a vehicle that has Started or is in Motion. The nationality DRM is right only for German and Russian vehicles (see findings). | audit |
| D2.51 | Mechanical Reliability | 198 | built | GamePlanner.Vehicles.cs: PlanStartVehicle, AddVehicleCheck | BacklogPass11Tests.TheRedMpT34RollsForMechanicalReliabilityWhenItStarts |  | A DR at each Start MP of a red-MP AFV, a 12 immobilizes. The Motion Attempt trigger waits for D2.401; the "forgotten DR" clause is table procedure. | audit |
| D2.52 | Axis Vehicles | 199 | not built |  |  | Deferred with Chapter F: the rule serves North Africa only | No North Africa date rule; needs F11.2. | audit |
| D2.6 | Enemy AFV | 199 | built with a deviation | GamePlanner.VehicleTerrain.cs: EnemyAfvBar, CouldKillWithFive; GamePlanner.Vehicles.cs: PlanMoveVehicle (Stop), PlanEndVehicle, PlanEsb | BacklogPass11Tests.AVehicleMayNotStopInAnEnemyAfvsHexItCouldNotKill; AVehicleThatCannotMoveOnMayStopBesideAnEnemyAfv | Pass 52 | R11.6: only the MA's AP To Kill is weighed (no IFT attack), the Bypass out of LOS exception is not built, and a vehicle that cannot move on may stay (R11.1 reading). | audit |
| D2.7 | All MP/MF Expenditure | 199 | built | GamePlanner.Vehicles.cs: PlanMoveVehicle (All entries, AfterAllEntry), PlanEsb | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck; AReverseAllEntryKeepsItsStopMpAndNoOvrGoesWithAnAllEntry |  | An ALL entry still allows Start, Stop, and towing MP; nothing after it, ESB included. Gallop does not exist. | audit |

#### D3 AFV Combat (pages 199 to 201)

22 rows: 2 built, 10 partly, 9 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D3 | AFV Combat | 199 | not applicable |  |  |  | Heading with one sentence pointing to Chapter C. | audit |
| D3.1 | Covered Arc (CA) | 199 | built | LiveOrdnance.cs: TurretFacing; GameState.TurretFacings; GamePlanner.Ordnance.cs: OrdnanceMapFacts | BacklogPass7Tests.ATurretTurnsForItsShotKeepsItsTcaAndAShockedAfvRollsInTheRph |  | A turreted AFV keeps a VCA and a TCA. | audit |
| D3.11 | Vehicular Covered Arc (VCA) | 199 | partly | GamePlanner.Vehicles.cs: VcaHexes; GamePlanner.Ordnance.cs: OrdnanceMapFacts (hull Target Facing) | BacklogPass7Tests.ATankFiresApAtAnEnemyTanksSideAndBurnsIt | Pass 33 | The VCA governs movement and the hull's Target Facing. Its use for bow-mounted weapons' fire is not built. | audit |
| D3.12 | Turret Covered Arc (TCA) | 199 | partly | GamePlanner.Ordnance.cs: OrdnanceMapFacts; LiveOrdnance.cs: TurretFacing; ScenarioA1ArmorCalculator.cs: Kill (turret facing by TCA) | BacklogPass7Tests.ATurretTurnsForItsShotKeepsItsTcaAndAShockedAfvRollsInTheRph | Pass 33 | The TCA turns only for an MA shot and is then kept (R7.10). Missing: free TCA change with each MP, change at the end of a fire phase, the doubled MP in woods or buildings, the VCA changing for an NT shot. | audit |
| D3.2 | Target Facing | 199 | partly | GamePlanner.Ordnance.cs: OrdnanceMapFacts; ScenarioA1ArmorCalculator.cs: Kill | BacklogPass7Tests.ATankFiresApAtAnEnemyTanksSideAndBurnsIt; ScenarioA1Pass7Tests.TheRearFacingAddsOneAndACriticalHitDoublesTheBasicTk | Pass 33 | Built: facing by the hexside the LOS crosses, a hexspine LOS takes the facing worse for the attacker (R7.4). Missing: the colored dr facing for fire from inside the target hex (ordnance at a vehicle in the firer's own Location is refused); FT, MOL, DC from the same hex. | audit |
| D3.3 | Bounding First Fire | 199 | partly | LiveFire.cs: FromState, MayBoundingFire; ScenarioA1FireCalculator.cs: VehicleFireOutside, FireCounter; LiveOrdnance.cs: FromState (phasing ordnance refused in the MPh) | BacklogPass6Tests.AHalftrackFiresAsDefensiveFirstFireAndAsBoundingFirstFire; AVehicleMayFireAtTheOutsetOfItsMphAndTheDefenderMayRepair | Pass 33 | Bounding First Fire is built for the halftrack's MA AAMG only. A tank's MA cannot fire in its own MPh (Cases C to C4 not built), and Passengers do not fire. | audit |
| D3.31 | MG/Canister/FT Fire | 200 | partly | ScenarioA1FireCalculator.cs: VehicleFirepower (bounding-fire halving) | BacklogPass6Tests.AHalftrackFiresAsDefensiveFirstFireAndAsBoundingFirstFire | Pass 33 | MG FP is halved. Missing: Canister, the Gyrostabilized CMG exception, and the clause on firing a non-MA weapon in the MPh (only one weapon ever fires). | audit |
| D3.32 | Final Fire | 200 | partly | ScenarioA1FireCalculator.cs: VehicleFireOutside, VehicleFirepower | BacklogPass6Tests.AHalftrackFiresAsDefensiveFirstFireAndAsBoundingFirstFire (the AFPh refusal after Bounding Fire) | Pass 33 | No Bounding Final Fire exists, as the rule says. The AFPh shot on a Multiple ROF kept in the MPh is coded for the MG; I found no test of it. Ordnance (Case C) is not built. | audit |
| D3.4 | Armor Leaders | 200 | partly | LiveFire.cs: FromState (play.fire-vehicle-group); LiveOrdnance.cs: FromState (play.ordnance-support) | VehicleStepsTests.TheMandatoryFireGroupBindsAVehiclesMgAndItsLocationsInfantry | Pass 34 | An Infantry leader never directs a vehicle's fire. Armor Leaders do not exist. | audit |
| D3.41 | (none) | 200 | not built |  |  | Pass 34 | No Armor Leader counter, record, or assignment. | audit |
| D3.42 | (none) | 200 | not built |  |  | Pass 34 | Crew morale never takes an Armor Leader's. | audit |
| D3.43 | (none) | 200 | not built |  |  | Pass 34 | No Armor Leader to follow a crew. | audit |
| D3.44 | (none) | 200 | not built |  |  | Pass 34 | No leadership DRM on the MA, OVR, CC, HD Maneuver, or Bog Removal (backlog, R11.11, R11.14). | audit |
| D3.45 | Inexperienced Crews | 200 | not built |  |  | Pass 34 | No Inexperienced Crew SSR or 6+1 quasi Armor Leader. | audit |
| D3.5 | Vehicular MG/IFE Fire | 200 | partly | ScenarioA1FireCalculator.cs: VehicleFireOutside, VehicleWeaponEffect; LiveFire.cs: FireSpent; GameProjector.cs (fire records) | VehicleStepsTests.TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase; TheMandatoryFireGroupBindsAVehiclesMgAndItsLocationsInfantry | Pass 33 | Built: one MG attack per Player Turn unless a Multiple ROF MA, Mandatory FG. One fire marker covers all of a vehicle's weapons (backlog). Missing: BMG and CMG fire, adding or splitting MG FP, the same-phase rule across weapons. | audit |
| D3.51 | Maintaining CA | 201 | partly | GamePlanner.Ordnance.cs: OrdnanceMapFacts; LiveOrdnance.cs: TurretFacing | BacklogPass7Tests.ATurretTurnsForItsShotKeepsItsTcaAndAShockedAfvRollsInTheRph | Pass 33 | An MA keeping its ROF pays Case A only from the TCA it now has. Missing: the same penalty for other turret weapons, the Known-target condition, the TCA moving with a VCA change, the same-hex rule. | audit |
| D3.52 | (none) | 201 | not built |  |  | Pass 33 | BMG and CMG never fire on the IFT, so no Case A as IFT DRM. | audit |
| D3.53 | (none) | 201 | built | ScenarioA1FireCalculator.cs: VehicleFirepower (advancing-fire halving, no Case B) | none found by name |  | A vehicle MG is halved in the AFPh. The MA MG To Kill exception waits for D3.54. | audit |
| D3.54 | vs AFV | 201 | not built |  |  | Pass 33 | No MG To Kill attack by any vehicle (A9.61 for the halftrack's MA AAMG). The bar on non-MA MGs holds only because none fires. | audit |
| D3.6 | FT | 201 | not built |  |  | Pass 53 | No vehicular FT; the catalog's vehicle-ft attributes are unread. | audit |
| D3.7 | Malfunction | 201 | partly | ScenarioA1FireCalculator.cs: VehicleWeaponEffect, overrun malfunction; ScenarioA1OrdnanceCalculator.cs: Breakdown; GamePlanner.Rally.cs: PlanVehicleRepair | BacklogPass6Tests.AVehiclesMalfunctionedAamgIsRepairedOnOneAndDisabledOnSix | Pass 33 | Built: malfunction on an Original 12, repair on 1, disabled on 6, the AAMG needing a CE crew. Missing or wrong: the repair asks a CE crew for any weapon (a BU tank cannot repair its MA), BMG and CMG malfunctions have no repair, a tank whose MA is disabled is not Recalled, SA, Hero Rider. | audit |
| D3.71 | Low Ammo B# | 201 | not built |  |  | Pass 53 | No circled B# or Low Ammo counter; no catalog vehicle prints one. | audit |

#### D4 Terrain Modifications to Anti-Vehicle Fire (pages 202 to 203)

13 rows: 1 partly, 1 refused, 10 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D4 | Terrain Modifications to Anti-Vehicle Fire | 202 | not applicable |  |  |  | Heading. | audit |
| D4.1 | Terrain Benefits | 202 | partly | ScenarioA1ArmorCalculator.cs: Run (case-q, case-r) | ScenarioA1Pass7Tests.MovingConcealedAndPointBlankTargetsChangeTheDr (DRM set) | Pass 52 | The target hex's TEM and LOS Hindrance go on the To Hit DR of a Vehicle Target Type shot. Missing: Infantry and Area Target Type against a vehicle (refused), hexside TEM (fire at a hex with any wall, hedge, or cliff hexside is refused), fire across levels (refused). | audit |
| D4.2 | Hull Down | 202 | not built |  |  | Pass 52 | No Hull Down state; nothing limits a hit to the turret. | audit |
| D4.21 | Wall/Roadblock | 202 | refused | GamePlanner.Fire.cs: FireMapFacts (play.fire-terrain, hexside terrain at the target) | none found | Pass 52 | Ordnance or vehicle fire at a target hex with any wall, hedge, or cliff hexside is refused (R10.5), so the wall HD case cannot arise. The refusal is wider than the rule's case. | audit |
| D4.22 | Height Advantage | 202 | not built |  |  | Pass 52 | No HD Maneuver Attempt, table, or two-MP expenditure. Ordnance across levels is undecided and refused (levels-differ), so the Height Advantage TEM for a vehicle is not applied either. | audit |
| D4.221 | (none) | 202 | not built |  |  | Pass 52 | No HD counter or hexside record. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D4.222 | (none) | 202 | not built |  |  | Pass 52 | Depends on D4.22. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D4.223 | Hd Firer | 202 | not built |  |  | Pass 52 | Depends on D4.22 and on BMG fire. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D4.3 | Underbelly Hits | 202 | not built |  |  | Pass 52 | No Underbelly Hit, vertex LOS, or Aerial AF. A vehicle crossing a wall or hedge can be shot at only where the target hex has no hexside terrain, so in practice such Defensive First Fire by ordnance is refused. | audit |
| D4.31 | (none) | 203 | not built |  |  | Pass 52 | Depends on D4.3; Deliberate Immobilization (C5.7) is not built either. | audit |
| D4.32 | (none) | 203 | not built |  |  | Pass 52 | Depends on D4.3. | audit |
| D4.33 | (none) | 203 | not built |  |  | Pass 52 | Depends on D4.3. | audit |
| D4.34 | (none) | 203 | not built |  |  | Pass 52 | Depends on D4.3 and bocage (B9.541). | audit |

#### D5 Inherent Crew (pages 203 to 204)

21 rows: 3 built, 9 partly, 7 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D5 | Inherent Crew | 203 | not applicable |  |  |  | Heading. | audit |
| D5.1 | (none) | 203 | partly | ScenarioA1ArmorCalculator.cs: ToKill (crew morale Elite or 1st Line); ScenarioA1FireCalculator.cs: VehicleEffects (AfvCrewMorale); GamePlanner.Ordnance.cs: CrewCounter; GamePlanner.Vehicles.cs: HasVehicleMg | ScenarioA1Pass7Tests.AHullHitEqualToTheTkImmobilizesAndTheCrewMayAbandon; BacklogPass7Tests.AnEliminatedTanksCrewMaySurvive | Pass 51 | Crew morale and the unarmed vehicle's Inherent Driver are built. The crew that leaves is the nationality's ordinary crew counter, not a vehicle crew of FP 1 (ruling R7.9); Armor Leaders, Inexperienced Crews, and Temporary crews are missing. | audit |
| D5.2 | Buttoned Up (BU) | 203 | built | LiveFire.cs: CrewExposed; ScenarioA1ArmorCalculator.cs: case-i; GamePlanner.VehicleTerrain.cs: VehicleCost (BU road rate) | BacklogPass7Tests.AClosedToppedTanksCrewIsButtonedUpUnlessExposed |  | A CT AFV is BU unless CE, Case I +1, the BU road rate of 1 MP, and a BU crew is not Vulnerable. | audit |
| D5.3 | Crew Exposed (CE) | 203 | partly | LiveFire.cs: CrewExposed; ScenarioA1FireCalculator.cs: VehicleFireOutside, VehicleEffects | ScenarioA1VehicleFireTests; VehicleStepsTests.TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase | Pass 51 | OT CE unless BU, Stunned, or Shocked; the AAMG needs CE; a CE crew takes Collateral Attacks. Missing: the building entry clause (an AFV does not enter a building at all), and the hero Rider. | audit |
| D5.31 | CE DRM | 203 | partly | ScenarioA1FireCalculator.cs: VehicleEffects (crew-exposed +2), Precheck (AFV in positive TEM refused) | ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne | Pass 51 | The +2 CE DRM is built. An AFV in positive-TEM terrain is refused (ruling R25.6). Missing: CE DRM by Target Facing from Vehicle Notes, the Side facing for Residual FP, the reduction by elevation and Air Bursts. | audit |
| D5.311 | Unprotected Crews | 203 | not built |  |  | Pass 51 | No fire through an unarmored Target Facing, no elevation or Air Burst case, no crew that breaks and routs beneath its vehicle, no Vulnerable Passengers. | audit |
| D5.32 | Ordnance | 203 | built | ScenarioA1FireCalculator.cs: Precheck (an ordnance hit on a Location holding a vehicle is outside); GamePlanner.Ordnance.cs (Vehicle Target Type resolves on the TK table only) | none found |  | Holds by construction: an ordnance shot at a vehicle is a To Kill resolution and never attacks its CE crew. No test names the rule. | audit |
| D5.33 | CE Movement | 203 | partly | GamePlanner.Vehicles.cs: PlanButtonUp; GamePlanner.Recall.cs: MayChangeExposure | VehicleStepsTests.TheHalftracksAamgFiresWithItsMultipleRofAndItsCrewButtonsUpOncePerPhase | Pass 45 | Own MPh or APh, once per phase, not after Prep Fire, not while the DEFENDER's window is open (ruling R25.8). Missing: the bar after the vehicle's own Bounding First Fire in that MPh, and Passengers becoming CE or BU with the crew. | audit |
| D5.34 | Stun | 203 | partly | ScenarioA1FireCalculator.cs: VehicleEffects; GamePlanner.Fire.cs: VehicleConditions; GameProjector.cs: EndStuns, KeepMovingStack; ScenarioA1ArmorCalculator.cs (stun +1 on TH, NTC, TC, CS) | ACollateralAttackStunsTheCeCrewWhichStaysBuThroughStunPlusOne; ScenarioA1Pass7Tests.AStunRecoveryCounterAddsOneToTheFirersDrAndTheTargetsChecks | Pass 45 | Stun by a failed MC, BU, no fire or move, the Stop, the flip to +1 at the Player Turn's end are built. Missing: the +1 on an OVR DR and on the vehicle's CC DR, Stun by a Sniper 2, by a MG TK DR equal to the TK#, by Falling Rubble; a crew that bails by TC or survives loses its +1. | audit |
| D5.341 | Recall | 203 | partly | ScenarioA1FireCalculator.cs: VehicleEffects; GamePlanner.Recall.cs: MustLeave, RecallRoute, AbandonEvents; GamePlanner.Vehicles.cs: PlanMoveVehicle; GamePlanner.cs (end of Player Turn Abandonment) | ScenarioA1VehicleFireTests.ACasualtyMcRecallsTheCrew; BacklogPass5Tests.ARecalledAfvLeavesByItsFriendlyBoardEdgeAndIsRecordedAsExited; AnImmobilizedRecalledAfvIsAbandonedAtTheEndOfThePlayerTurn | Pass 45 | Recall by K, KIA, or Casualty MC, the flip, the shortest route off the Friendly Board Edge (rulings R5.16 to R5.18), Abandonment when immobilized. Missing: Recall of a 1MT AFV on a Stun, Sniper 1, a bogged Recalled AFV, the Stop to unload Passengers (refused), Armor Leader loss; ESB is not barred. | audit |
| D5.342 | (none) | 204 | built | ScenarioA1FireCalculator.cs: VehicleEffects (StunRecovery and a failed MC gives Recalled) | ScenarioA1VehicleFireTests.ACrewUnderStunPlusOneThatFailsItsMcIsRecalled |  | A second Stun under a +1 counter is a Recall. The Disabled MA Recall belongs to D3.7. | audit |
| D5.343 | SMC Crew | 204 | not built |  |  | Pass 51 | No SMC acts as a Temporary crew (A21.22 is not built). | audit |
| D5.4 | Abandonment | 204 | not applicable |  |  |  | Definition of Abandoning; the rules are in D5.41 to D5.43. | audit |
| D5.41 | (none) | 204 | partly | GamePlanner.Vehicles.cs: PlanMoveVehicle (Abandoned may not move); LiveOrdnance.cs (Abandoned MA does not fire); GamePlanner.Ordnance.cs: KillEvents | BacklogPass7Tests.AnAbandonedTankDoesNotFire | Pass 51 | Only the Abandoned counter's effects exist, reached through a failed TC or a Recall. Missing: the voluntary act in the MPh for all MF, weapon Removal with Disabled counters, unhooking a Gun as the crew leaves, capture of the Abandoned vehicle. | audit |
| D5.411 | Self-Destruction | 204 | not built |  |  | Pass 51 | No self-destruction of a vehicle or its weapons. | audit |
| D5.42 | Entry | 204 | not built |  |  | Pass 51 | No Infantry enters an Abandoned vehicle to crew it; an Abandoned vehicle stays so. | audit |
| D5.43 | FFNAM | 204 | not built |  |  | Pass 51 | Loading and unloading open the DEFENDER's window on the vehicle only; the Personnel are not attacked with FFNAM, and a pin or break does not stop a load or a hook-up. | audit |
| D5.5 | Immobilization TC | 204 | partly | ScenarioA1ArmorCalculator.cs: ToKill (CrewCheck, Abandoned); LiveOrdnance.cs: VehicleTargetFacts (CrewMayTakeTc); GamePlanner.Ordnance.cs: KillEvents | ScenarioA1Pass7Tests.AHullHitEqualToTheTkImmobilizesAndTheCrewMayAbandon; AnAbandonedOrShockedTargetsCrewTakesNoCheckAndNoSurvival; BacklogPass7Tests.AnImmobilizedTanksCrewMayAbandonIt | Pass 51 | Built for an Immobilization by an ordnance hit. Missing: the TC of an already bogged or immobilized vehicle hit by a shot that would have killed on a 5, the TC after Immobilization by IFT, DC, or mines, and Hazardous Movement for the crew (ruling R7.9). | audit |
| D5.6 | Crew Survival (CS) | 204 | partly | ScenarioA1ArmorCalculator.cs: ToKill (CrewSurvival); GamePlanner.Ordnance.cs: KillEvents, CrewCounter | ScenarioA1Pass7Tests (CrewSurvival asserts); BacklogPass7Tests.AnEliminatedTanksCrewMaySurvive | Pass 51 | The CS DR after an ordnance kill, +1 when Stunned, Shocked, Recalled, or under +1, none when burning or Abandoned, lower-case cs. Missing: CS after an IFT kill of an armed unarmored vehicle, Hazardous Movement, the bar on Collateral and Residual attacks, a broken crew's +1. | audit |
| D5.7 | Brew Ups | 204 | not built |  |  | Pass 51 | No red CS# in the catalog read and no -1 on the Burning Wreck determination. | audit |
| D5.8 | Crew FP | 204 | not built |  |  | Pass 51 | A crew counter created by a TC or Survival is placed with no fire counter, whatever its vehicle fired or moved. | audit |

#### D6 Transporting Personnel (pages 204 to 207)

29 rows: 1 built, 1 built with a deviation, 7 partly, 18 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D6 | Transporting Personnel | 204 | not applicable |  |  |  | Heading. | audit |
| D6.1 | Passengers | 204 | partly | GamePlanner.Passengers.cs: PassengerCapacity, PassengerPp, CapacityBar, AboardBar; GamePlanner.Rout.cs: MustRout | BacklogPass26Tests.PassengersUnloadFromAStoppedVehicleAndBoardAgain; PassengersLeaveAnImmobilizedVehicleAndABrokenOneNeedNotRout | Pass 51 | PP capacity, the squad 10, HS and crew 5, four SMC 0, the towed Gun's ammunition, and a broken Passenger free of rout are built. Missing: Mounted Fire, Passenger CC, rout beneath the vehicle when the crew is lost, the SW a Passenger may fire. Five or more SMC are refused (ruling R26.2). | audit |
| D6.2 | Riders | 205 | not built |  |  | Pass 51 | A Rider role exists in the state vocabulary only; no unit becomes a Rider and no vehicle has Rider capacity. | audit |
| D6.21 | Restrictions | 205 | not built |  |  | Pass 51 | No Riders. | audit |
| D6.22 | Attacks | 205 | not built |  |  | Pass 51 | No Riders. | audit |
| D6.23 | Target Status | 205 | not built |  |  | Pass 51 | No Riders. | audit |
| D6.24 | Bailing Out | 205 | not built |  |  | Pass 51 | No Bailing Out. | audit |
| D6.3 | Transport | 205 | built | GamePlanner.Passengers.cs: PlanLoad, PlanUnload; GameProjector.cs: StepVehicle | BacklogPass26Tests.AVehicleEntersFromOffBoardInMotionWithItsPassengers |  | No MP reduction for carrying Personnel; loading and unloading cost MP; Passengers are held in the vehicle, units on foot beside it. | audit |
| D6.31 | Recovery | 205 | partly | GamePlanner.Passengers.cs: AboardBar | none found | Pass 51 | Every action naming a Passenger is refused, so a Passenger Recovers nothing, which matches the bar; the allowed transfer of a SW to Infantry in the Location or to other PRC is missing. | audit |
| D6.4 | Loading | 205 | partly | GamePlanner.Passengers.cs: PlanLoad; GameProjector.cs: StepVehicle (Load) | BacklogPass26Tests.PassengersUnloadFromAStoppedVehicleAndBoardAgain | Pass 51 | Stopped, before any MP, one MF, a quarter of the MP kept per MF left to the unit that spent most, never in the APh. Missing: boarding from an ADJACENT hex in the same move, a SW left aboard a halftrack, attacks on the boarders (D5.43), the IPC help of a leader. Bypass is refused. | audit |
| D6.5 | Unloading | 205 | partly | GamePlanner.Passengers.cs: PlanUnload; GameProjector.cs: StepVehicle (Unload); GamePlanner.Vehicles.cs: PlanMoveVehicle | BacklogPass26Tests.PassengersUnloadFromAStoppedVehicleAndBoardAgain; PassengersLeaveAnImmobilizedVehicleAndABrokenOneNeedNotRout | Pass 51 | A quarter of the MP (FRU), one MF plus one per quarter spent, the three-quarter limit, a vehicle that Prep Fired, an immobilized one, moving on with the MF left. Missing: unloading in Bypass (refused), from a bogged vehicle (refused, against D8.5), FFNAM and FFMO on the unloaders, the CC counter in an enemy Location, the OVR bar, SW left aboard. | audit |
| D6.6 | Armored Halftracks | 205 | not applicable |  |  |  | Introduction to the armored halftrack rules; the rules are in D6.61 to D6.66. | audit |
| D6.61 | BU | 205 | partly | GameState.cs: At (Passengers are not in the Location); GamePlanner.Passengers.cs: AboardBar | none found | Pass 51 | Passengers are never attacked apart from their vehicle and never fire, which is the BU case for every Passenger of every vehicle (ruling R26.2). Missing: the elevation exception, return fire, Spotting, and the vehicle's kill reaching them otherwise than by elimination. | audit |
| D6.62 | CE | 206 | not built |  |  | Pass 51 | Passengers have no CE state; they are never Vulnerable and never fire. | audit |
| D6.63 | Fire/Movement | 206 | not built |  |  | Pass 51 | No Passenger fire. | audit |
| D6.631 | SW Removal | 206 | not built |  |  | Pass 51 | No weapon is Removed from a halftrack by an abandoning crew or an unloading Passenger. | audit |
| D6.64 | FG | 206 | not built |  |  | Pass 51 | A vehicle's MG fires alone; the Fire package puts D6.64 outside (VehicleFireOutside), and no Passenger or Rider joins a FG. | audit |
| D6.65 | Leadership | 206 | not built |  |  | Pass 51 | A Passenger leader directs nothing. | audit |
| D6.651 | Rally/Mc/Tc | 206 | not built |  |  | Pass 51 | A Passenger takes no Rally, MC, or TC, so no leader's reach is decided; a broken Passenger cannot rally aboard. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D6.66 | DM | 206 | not built |  |  | Pass 51 | No DM for a broken Passenger from an attack on its halftrack. | audit |
| D6.7 | Trucks | 206 | partly | ScenarioA1FireCalculator.cs: VehicleEffects (unarmored vehicle on the Vehicle line) | ScenarioA1VehicleFireTests; BacklogPass6Tests.ADestroyedTruckBecomesAWreckThatCoversInfantryOfEitherSide | Pass 51 | The truck itself is attacked on the Vehicle line with the attack's DR (A7.308). Missing: the Collateral Attack on its Passengers with the same DR; they take no fire at all (ruling R26.2). | audit |
| D6.71 | Ordnance | 206 | partly | ScenarioA1ArmorCalculator.cs: ToKill (unarmored TK, no Shock, Immobilization only on a DR equal to the TK#) | ScenarioA1Pass7Tests | Pass 51 | The Unarmored TK line and its results are built. Missing: the Specific Collateral Attack on the PRC of a truck that survives the hit. | audit |
| D6.72 | Passenger Fire | 206 | not built |  |  | Pass 51 | No Passenger fire, no quarter FP in an OVR, no Passenger CC. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D6.8 | Carriers | 206 | not built |  |  | Pass 51 | No Carrier in the catalog. | audit |
| D6.81 | PP | 206 | not built |  |  | Pass 51 | No Carrier. | audit |
| D6.82 | Crews/HS | 206 | not built |  |  | Pass 51 | No Carrier. | audit |
| D6.83 | (none) | 206 | not built |  |  | Pass 51 | No Carrier. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D6.84 | CE | 206 | not built |  |  | Pass 51 | No Carrier. | audit |
| D6.9 | Survival | 207 | built with a deviation | GameProjector.cs: KeepPassengers | none found | Pass 51 | Ruling R26.2: Passengers of a destroyed vehicle are eliminated with it, with no Survival DR; their SW go with them. Missing: one Survival DR per unit against the CS# (the cs# too), +1 if broken. | audit |

#### D7 Overruns (OVR) (pages 207 to 208)

17 rows: 3 built, 8 partly, 1 refused, 4 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D7 | Overruns (OVR) | 207 | not applicable |  |  |  | Heading. | audit |
| D7.1 | (none) | 207 | partly | GamePlanner.Vehicles.cs: PlanMoveVehicle (overrun flag); GamePlanner.Overrun.cs: OverrunBar, PlanDeclareOverrun, PlanOverrun, OverrunHalfMp; GameProjector.cs: ResolveOverrun | BacklogPass11Tests.ATankOverrunsASquadAndTheSquadReactsInCc; ATankBoggingAsItOverrunsStillResolvesTheOvr; ScenarioA1Pass11Tests.AnOvrOutsideItsOwnLocationIsRefused | Pass 53 | A quarter of the printed MP (FRU) declared with the entry or after the A12.41 choice, the Bog DR and Defensive First Fire first, then the IFT attack. Refused: a Location with units in Melee (ruling R11.11). Human Wave and Armored Assault escorts, and wagons, do not exist. | audit |
| D7.11 | FP | 207 | partly | ScenarioA1FireCalculator.cs: OverrunFirepower, OverrunWeapons; LiveFire.cs: OverrunFromState | ScenarioA1Pass11Tests.ATankOverrunsWithFourPlusItsMachineGunsTripledAndHalved; ScenarioA1Pass11Tests.TheOvrFpIsHalvedByImmobilityAndAgainstConcealedUnitsButNotByMotion | Pass 51 | Base 1, 2, or 4, MG tripled and halved, the halving when Immobile or destroyed first and against concealed targets, none for Motion. Missing: Passenger and Rider FP, FT, IFE, the RMG exclusion and the AAMG of a tank (none in the catalog). | audit |
| D7.12 | Vehicular Targets | 207 | partly | GamePlanner.Overrun.cs: OverrunBar; ScenarioA1FireCalculator.cs: OverrunOutside, VehicleEffects | BacklogPass11Tests (OVR tests) | Pass 53 | An AFV is not OVR but its CE crew is, and other targets beside it are. Refused: an OVR of a Location holding an enemy vehicle in Motion, so the +2 DRM is not built. | audit |
| D7.13 | Restrictions | 207 | built | GamePlanner.Overrun.cs: OverrunBar; GamePlanner.Vehicles.cs: PlanMoveVehicle | BacklogPass11Tests.AnOvrIsRefusedInReverseAndFromVbm |  | Not in Reverse, not from VBM, not when marked Bounding Fire by its own fire, and only on entering the hex. VBM is refused outright, which is stricter than the rule (it bars only a DEFENDER in the obstacle). | audit |
| D7.14 | Multiple OVR | 207 | built | GamePlanner.Overrun.cs: OverrunBar (an earlier OVR this MPh lifts the Bounding Fire bar); ScenarioA1FireCalculator.cs: OverrunOutside (one vehicle, one attack) | none found |  | A vehicle with MP left may OVR again on a later entry; each vehicle attacks alone and once per entry. The call path is clear but no test makes a second OVR. | audit |
| D7.15 | TEM | 207 | partly | GamePlanner.Overrun.cs: OverrunAttack; ScenarioA1FireCalculator.cs (ffmo D7.15, HexsideTem, Cover) | ScenarioA1Pass11Tests.AnOvrTakesTheTargetsTemAndNoFfmoInWoods | Pass 53 | The Location's TEM, FFMO in Open Ground cumulative with TEM and SMOKE, the wall or hedge only across the hexside entered. The +1 of a vehicle or wreck is withheld from an OVR (Cover treats it as fire from within the Location, D9.3), though D7.15 lists vehicle and wreck TEM. FT and Crest status are missing. | audit |
| D7.16 | Leadership | 207 | not built |  |  | Pass 34 | No Armor Leader and no Passenger leader directs an OVR; the Fire package refuses any director. | audit |
| D7.17 | Malfunction | 207 | built | ScenarioA1FireCalculator.cs: OverrunEffect; GamePlanner.Fire.cs: AddFireEvents (OverrunEffect) | BacklogPass11Tests; ScenarioA1Pass11Tests |  | An Original 12 malfunctions one weapon that added FP, by Random Selection among several, or immobilizes a vehicle with none. Missing only the B# or X# below 12 and the inanimate-target case, which no catalog weapon reaches. | audit |
| D7.2 | Reaction Fire | 207 | partly | GameProjector.cs: ResolveOverrun (Reaction window); GamePlanner.VehicleCloseCombat.cs: PlanReactionFire; GamePlanner.Fire.cs (play.fire-reaction) | BacklogPass11Tests.ATankOverrunsASquadAndTheSquadReactsInCc; InTheMphUnitsFireAtAVehicleInTheirOwnLocationOnlyAfterItsOvr | Pass 53 | Reaction Fire after the OVR's resolution, CC Reaction Fire at any vehicle in the Location, Non-CC only after an OVR. Missing: the ADJACENT Location by Street Fighting, and the wait for the next MP expenditure is not enforced as a rule of its own. | audit |
| D7.21 | CC Reaction Fire | 207 | partly | GamePlanner.VehicleCloseCombat.cs: PlanReactionFire, PlanVehicleAttack; GamePlanner.Overrun.cs: AddPaatc, NeedsPaatc | BacklogPass11Tests.ATankOverrunsASquadAndTheSquadReactsInCc | Pass 45 | Unbroken, unpinned, not in Melee; the PAATC, its pin on failure, one PAATC per vehicle per phase; First or Final Fire and CC counters. Missing: the CC counter is set but nothing reads it, so it does not bar Non-CC Reaction Fire; the Unarmed bar; Cavalry. | audit |
| D7.211 | Street Fighting | 207 | not built |  |  | Pass 53 | No Street Fighting; the attackers must stand in the vehicle's Location. | audit |
| D7.212 | FPF CC Reaction Fire | 208 | refused | GamePlanner.VehicleCloseCombat.cs: PlanReactionFire | none found | Pass 53 | A unit marked Final Fire is refused CC Reaction Fire (ruling R11.13). The obligation to attack when OVR is not built. | audit |
| D7.213 | (none) | 208 | partly | ScenarioA1VehicleCloseCombat.cs (reaction-fire-marked -1; no vehicle attack as Reaction Fire; no pinned attacker) | ScenarioA1Pass11Tests.ACombiningLeaderAddsOneCcvAndHisLeadershipAndAPinAndAReactionMarkerSubtractOne | Pass 53 | No vehicle CC in the MPh, no pinned attacker, CCV -1 when marked First Fire. Missing: the Personnel Escort clause, since no escort exists. | audit |
| D7.22 | Non-CC Reaction Fire | 208 | partly | GamePlanner.Fire.cs (TPBF at the OVRing vehicle in the Reaction window) | BacklogPass11Tests.InTheMphUnitsFireAtAVehicleInTheirOwnLocationOnlyAfterItsOvr | Pass 53 | TPBF by small arms and MG on the IFT only. Missing: ordnance, LATW, FT, and Thrown DC at the OVRing AFV, the Gun's CA change, the rear Target Facing, MOL. A Gun fires in its own Location only at Infantry and a SW not at all. | audit |
| D7.221 | FPF Non-CC Reaction Fire | 208 | not built |  |  | Pass 53 | No DEFENDER marked Final Fire is made to attack an OVRing vehicle, and no attack DR doubles as its NMC here (backlog section 21, ruling R11.13). | audit |
| D7.23 | Gun Crews | 208 | not built |  |  | Pass 53 | A Gun's crew has no Reaction Fire with its Gun, and none forced. | audit |

#### D8 Immobilization & Bog (pages 208 to 209)

12 rows: 1 built, 7 partly, 3 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D8 | Immobilization & Bog | 208 | not applicable |  |  |  | Heading. | audit |
| D8.1 | Immobilization | 208 | partly | GamePlanner.Vehicles.cs: PlanMoveVehicle, Halted; GameProjector.cs: StepVehicle, CheckVehicle | VehicleStepsTests; BacklogPass11Tests.ABoggedTankIsFreedMiredOrImmobilizedByItsBogRemoval | Pass 51 | No Start MP, no exit, no VCA change; never repaired; Immobilization by IFT, CC, ordnance, OVR, Bog Removal, Mechanical Reliability, ESB. Missing: DC, mines, and the flip of an unarmed unarmored immobilized vehicle to its wreck side once unloaded. | audit |
| D8.11 | Multiple Immobilization | 208 | not built |  |  | Pass 51 | A second Immobilization gives no TC; LiveOrdnance.VehicleTargetFacts denies the TC to an immobilized target. | audit |
| D8.2 | Bog | 208 | partly | GamePlanner.VehicleTerrain.cs: VehicleCost, VehicleTurnCost; GamePlanner.Vehicles.cs: AddVehicleCheck; GameProjector.cs: CheckVehicle | BacklogPass11Tests.AWoodsEntryTakesAllMpOrHalfForATankWithABogCheck; AHalftrackCrossingAHedgeIntoWoodsTakesBothBogDrs | Pass 53 | Bog Checks on entry of woods and rubble, per hexspine turned there, and at a hedge for a halftrack; the bogged vehicle is Immobile. Missing: the Bog hexes not reviewed for vehicles (stream, gully, marsh edge, mudflat, building entry, wire, debris) and the exit checks. | audit |
| D8.21 | Bog Check | 209 | partly | GamePlanner.VehicleTerrain.cs: VehicleBogDrm, VehicleCost; LiveRecords.cs: VehicleCheckRolled.For | BacklogPass11Tests; BacklogPass16TablePlayerTests.BogDrmInMudAndDeepSnow | Pass 53 | A Final DR of 12 or more bogs and ends the MPh. Built DRM: Ground Pressure, towing, mud or snow, Deep Snow, not fully-tracked, truck MP, gaining elevation into woods, woods or rubble at half allotment. Missing: Abrupt Elevation Change, Deep Stream, Light Woods, debris, Wire, graveyard, wooden and stone building, the Factory case, soft ground, the mortar exception to towing, the paved or plowed road and building exceptions in part. | audit |
| D8.22 | Bog TC | 209 | not built |  |  | Pass 51 | Cross-reference to D5.5's second clause, which is not built: a bogged vehicle hit by threatening ordnance takes no TC. | audit |
| D8.23 | Mud & Deep Snow | 209 | not built |  |  | Pass 57 | No Secret Bog DR for the MPh in Mud or Deep Snow; only the +1 DRM on regular Bog hexes (backlog section 21, ruling R16.12). | audit |
| D8.3 | Bog Removal | 209 | partly | GamePlanner.Vehicles.cs: PlanStartVehicle (Removal); GameProjector.cs: CheckVehicle | BacklogPass11Tests.ABoggedTankIsFreedMiredOrImmobilizedByItsBogRemoval | Pass 53 | Start MP of colored dr times white dr, doubled for a truck, 1 to 4 frees, 5 Mires, 6 or more immobilizes, not after Prep Fire, freed even beyond the allotment (ruling R11.10). Missing: the -1 for an assisting AFV with both TI, the armor leader's drm; a halftrack is not doubled, which reads non-tracked as truck only. | audit |
| D8.31 | Mired | 209 | built | GamePlanner.Vehicles.cs: PlanStartVehicle (Mired +1); GameProjector.cs: CheckVehicle | BacklogPass11Tests.ABogRemovalOfFiveMiresTheTank |  | Mired adds +1 to the colored dr, once. | audit |
| D8.32 | Tow | 209 | partly | GamePlanner.Guns.cs: PlanHookGun | none found | Pass 53 | The unhook has no bar for a bogged, immobilized, or Abandoned towing vehicle in the planner, but no test does it and the vehicle's MPh bars (PlanMoveVehicle, StepVehicle) were not shown to let it through. Verify before counting it built. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| D8.4 | Target Status | 209 | partly | GameProjector.cs (MovedVehicles); LiveOrdnance.cs: VehicleTargetFacts; GamePlanner.Ordnance.cs: PassEightFacts | none found | Pass 45 | A vehicle that entered a hex or moved in Motion stays a moving target for the Player Turn, bogged or not. The second clause is not built: a Defensive First Fire shot at a vehicle spending Bog Removal MP in its Bog hex takes a Case J DRM from MpInLos. | audit |
| D8.5 | (none) | 209 | partly | GamePlanner.Vehicles.cs: PlanMoveVehicle; GameProjector.cs: StepVehicle | BacklogPass26Tests.PassengersLeaveAnImmobilizedVehicleAndABrokenOneNeedNotRout | Pass 45 | An immobilized vehicle unloads and fires as before. Missing: a bogged vehicle is refused every expenditure but Bog Removal, so it cannot unload; the bow MG limits after Immobility are not built (a BMG fires only in an OVR). | audit |

#### D9 Vehicles as Cover (pages 209 to 210)

11 rows: 4 partly, 6 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D9 | Vehicles as Cover | 209 | not applicable |  |  |  | Heading. | audit |
| D9.1 | BU/CE | 209 | partly | LiveFire.cs: CrewExposed; ScenarioA1FireCalculator.cs: VehicleEffects | ScenarioA1VehicleFireTests.AKiaOrKResultRecallsTheCrewAndABuCrewIsNotVulnerable | Pass 51 | Built for an AFV's Inherent crew. Missing: OT AFV Passengers and Riders, who are never attacked. | audit |
| D9.2 | Survival | 209 | partly | GamePlanner.Ordnance.cs: KillEvents; GamePlanner.Fire.cs: VehicleEffectEvents; GameProjector.cs: KeepPassengers | BacklogPass7Tests.AnEliminatedTanksCrewMaySurvive | Pass 51 | The crew of a vehicle destroyed by ordnance rolls for Survival and is not attacked separately. Missing: Survival for Passengers (eliminated, ruling R26.2) and after an IFT kill. | audit |
| D9.3 | AFV/Wreck TEM | 209 | partly | GamePlanner.Wrecks.cs: CoverAt, Standing; ScenarioA1FireCalculator.cs: Cover; ScenarioA1OrdnanceCalculator.cs (wreck +1 on TH) | ScenarioA1Pass6Tests.AWreckOrFriendlyAfvGivesInfantryOneTemAndCancelsFfmo; BacklogPass6Tests.AFriendlyAfvThatEnteredAHexThisTurnGivesNoCoverUntilTheAfphEnds | Pass 52 | +1 from a non-burning wreck, a friendly AFV, or an Abandoned enemy AFV; not from within the Location, not with another positive TEM, not while Case J applies, none from an unarmored vehicle (ruling R6.1). Missing: the exception for units Abandoning, Surviving, unloading, or Bailing Out, and the entrenched, Dug-In, and Depression cases. | audit |
| D9.31 | Armored Assault | 209 | not built |  |  | Pass 52 | Infantry and an AFV never move as one stack (backlog section 16, ruling R6.1). | audit |
| D9.4 | AFV/Wreck LOS Hindrance | 210 | partly | GamePlanner.Wrecks.cs: VehicleHindrance; GamePlanner.Fire.cs: FireMapFacts | BacklogPass6Tests.AnAfvBetweenFirerAndTargetIsAHindrance; AnAfvInAGrainHexAddsItsHindranceToTheGrains | Pass 45 | +1 through a hex with an AFV or non-burning wreck at the same level, seen by both ends, not while Case J applies (ruling R6.2). Missing: the Bypass exception (a Bypassing AFV always hinders, and vehicles now do Bypass), the entrenched and Dug-In cases, the concealed AFV's reveal. | audit |
| D9.5 | Armored Cupola | 210 | not built |  |  | Pass 53 | No Armored Cupola. | audit |
| D9.51 | Crew | 210 | not built |  |  | Pass 53 | No Armored Cupola. | audit |
| D9.52 | Placement | 210 | not built |  |  | Pass 53 | No Armored Cupola. | audit |
| D9.53 | Size | 210 | not built |  |  | Pass 53 | No Armored Cupola. | audit |
| D9.54 | Dug-In AFV | 210 | not built |  |  | Pass 53 | No Dug-In AFV. | audit |

#### D10 Wrecks (pages 210 to 211)

10 rows: 2 built, 1 partly, 5 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D10 | Wrecks | 210 | not applicable |  |  |  | Heading. | audit |
| D10.1 | Creation | 210 | partly | GameProjector.cs: Wreck; GamePlanner.Fire.cs: VehicleEffectEvents; GamePlanner.Ordnance.cs: KillEvents; GamePlanner.Wrecks.cs: WrecksAt, IsBurning | BacklogPass6Tests.ADestroyedTruckBecomesAWreckThatCoversInfantryOfEitherSide; AConcealedVehicleDestroyedLeavesAKnownWreck; ABurningWreckCarriesABlazeWhoseSmokeHindersAndGivesNoCover | Pass 51 | A destroyed vehicle becomes a wreck with its VCA, burning with a Blaze. Missing: attacking a non-burning wreck as the vehicle, the vehicles that leave no wreck, the TCA set to the VCA (ruling R6.5). | audit |
| D10.2 | Movement Effects | 210 | built | GamePlanner.Wrecks.cs: WreckEntryHalfMp, BlazeEntryHalfMf; GamePlanner.VehicleTerrain.cs: VehicleCost | BacklogPass6Tests.AWreckRaisesAVehiclesEntryCost |  | One MP more per wreck for a vehicle, more by road and with a Blaze; Infantry pay only for a burning wreck; a wreck never blocks entry (ruling R6.4). The Bypass, One Lane Bridge, and Sunken Lane exceptions rest on other sections. | audit |
| D10.3 | Cover | 210 | built | GamePlanner.Wrecks.cs: CoverAt, VehicleHindrance, SmokeSources | BacklogPass6Tests.ABurningWreckCarriesABlazeWhoseSmokeHindersAndGivesNoCover |  | A non-burning wreck gives the D9.3 TEM and the D9.4 Hindrance; a burning one gives neither, and its smoke hinders. The Fire Lane and Heavy Winds exceptions are outside. | audit |
| D10.4 | Removal | 210 | not applicable |  |  |  | Heading for D10.41 and D10.42. | audit |
| D10.41 | Fire | 210 | not built |  |  | Pass 44 | No terrain Blaze exists, so no wreck is removed by one. | audit |
| D10.42 | Pushing | 210 | not built |  |  | Pass 53 | No pushing of a wreck; the catalog has no vehicle weights. | audit |
| D10.5 | Scrounging | 211 | not built |  |  | Pass 51 | No Scrounging and no Scrounged or Disabled counter on a wreck. | audit |
| D10.51 | (none) | 211 | not built |  |  | Pass 51 | No Scrounging. | audit |
| D10.52 | (none) | 211 | not built |  |  | Pass 51 | No Scrounging. | audit |

#### D11 Gyrostabilizers & Schuerzen (page 211)

10 rows: 9 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D11 | Gyrostabilizers & Schuerzen | 211 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| D11.1 | Gyrostabilizer (G) | 211 | not built |  |  | Pass 53 | No Gyrostabilizer field in the catalog (the wreck face carries crew-survival only) and no SSR switch; needs a "G" trait and a per-game "in use" flag. The optional dr on the H1.42 table is in this paragraph too. | audit |
| D11.11 | (none) | 211 | not built |  |  | Pass 53 | Needs the tank MA in Bounding First Fire (backlog R7.10, R7.12, not built) and Case C to C4; then Case B+1 when stopped and +1 on Case C, C1, C2 while moving. | audit |
| D11.12 | (none) | 211 | not built |  |  | Pass 53 | Acquisition kept while firing not stopped; needs vehicle MA fire in the MPh and the LOS-kept test along the move. | audit |
| D11.13 | Stabilized CMG | 211 | not built |  |  | Pass 53 | CMG not halved for Bounding or Motion fire at the Gun's acquired target; needs vehicle MG Bounding First Fire. | audit |
| D11.2 | Schuerzen (Sz) | 211 | not built |  |  | Pass 53 | Says which Target Facings carry Schuerzen (turreted: hull sides, turret sides and rear; non-turreted: hull and superstructure sides). No Sz field or state. | audit |
| D11.21 | Availability | 211 | not built |  |  | Pass 53 | Availability list, July 1943 onward, by SSR or DYO only. None of the listed AFV is in the catalog (it has the PzKpfw IIIH, not the J, L, or N). | audit |
| D11.211 | Optional Availability | 211 | not built |  |  | Left out: an optional rule by the rulebook's own mark | Optional rule (asterisk): a DR per AFV on the H1.42 Schuerzen Availability Table. Candidate to leave out by a ruling. | audit |
| D11.22 | Loss | 211 | not built |  |  | Pass 53 | Loss on entering rubble or a building or woods obstacle, except on a Trail Break or across a road hexside; needs a per-vehicle Schuerzen state. | audit |
| D11.23 | Effect | 211 | not built |  |  | Pass 53 | Doubles the lower dr of the To Kill DR for HEAT (all SCW) against a protected facing; needs the HEAT To Kill path against AFV. | audit |

#### D12 Horse-Drawn Transport (pages 211 to 212)

6 rows: 5 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D12 | Horse-Drawn Transport | 211 | not applicable |  |  |  | Heading with a historical note only. | audit |
| D12.1 | Vehicle Class | 211 | not built |  |  | Pass 55 | No wagon counter or movement type (the vocabulary's movement types are motorcycle, armored-car, fully-tracked, half-tracked, truck). Needs a vehicle that spends MF on the MF Entrance Cost column, 1 MF per hexspine of VCA change, no start or stop cost, no wreck. | audit |
| D12.2 | Transport | 211 | not built |  |  | Pass 55 | Transport as a truck (D6.7); two wagons combined tow one Gun of M# 2 or more, T2 and Large Target. | audit |
| D12.3 | Target Status | 211 | not built |  |  | Pass 55 | Unarmored vehicle target, average size; large when combined. | audit |
| D12.4 | Gallop | 211 | not built |  |  | Pass 55 | Gallop: half again the MF, CX on wagon and Passengers, Wreck Check dr in each hex of a VCA change or of more than 1 MF; a 6 eliminates the wagon and breaks Passengers. | audit |
| D12.5 | Sledge | 212 | not built |  |  | Pass 55 | Sledge side of the wagon counter for Snow scenarios; its allotment is never reduced by snow. Needs E3.7 snow costs for vehicles. | audit |

#### D13 Vehicular Smoke Dispensers (page 212)

9 rows: 8 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D13 | Vehicular Smoke Dispensers | 212 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| D13.1 | Types | 212 | not built |  |  | Pass 56 | No dispenser type or Usage Number field in the catalog or the vehicle model (ScenarioA1FireReference reads none). The PzKpfw IIIH already in the catalog prints a dispenser, so its absence is a silent gap. | audit |
| D13.2 | Usage | 212 | not built |  |  | Pass 56 | One attempt per Player Turn in the MPh, 1 MP when it fires, not after any fire by the AFV or PRC, not when Abandoned, stunned, shocked, or broken; in the enemy MPh as if Defensive First Fire. | audit |
| D13.3 | Firing | 212 | not built |  |  | Pass 56 | Usage DR against the Usage Number, +1 when BU. | audit |
| D13.31 | sD | 212 | not built |  |  | Pass 56 | sD: white dispersed smoke in the AFV's own hex. | audit |
| D13.32 | sM | 212 | not built |  |  | Pass 56 | sM: smoke 1 to 3 hexes away in the TCA and LOS; Case A, +2 moving, Hindrance DRM on the Usage DR; not from a building. | audit |
| D13.33 | sP | 212 | not built |  |  | Pass 56 | sP: crew must be CE; smoke in the own hex. | audit |
| D13.34 | sN | 212 | not built |  |  | Pass 56 | sN: BU only; own hex; dated Usage Number (7 to 12/44 and 1945); its HE use in CC is A11.622. | audit |
| D13.35 | Vehicular Smoke Grenades | 212 | not built |  |  | Pass 56 | Vehicular smoke grenades: dr 1 (unarmored or BU OT) or 2 or less (CE AFV); a half-inch Smoke counter removed at the end of the enemy MPh. GamePlanner.Smoke.cs admits squads only. | audit |

#### D14 Radioless AFV (pages 212 to 214)

16 rows: 15 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D14 | Radioless AFV | 212 | not applicable |  |  |  | Heading with a historical note only. | audit |
| D14.1 | AFV Radio | 212 | not built |  |  | Pass 53 | No radioless trait in the catalog; the T-34 M41 in the catalog is radioless (ruling R7.1 says so and says D14 is not built). The OBA half needs C1. | audit |
| D14.2 | Platoon Movement | 212 | not built |  |  | Pass 53 | Forming a platoon of two or three AFV at setup or the start of the MPh. Nothing exists; R7.1 records the gap. | audit |
| D14.21 | Mechanics of Movement | 212 | not built |  |  | Pass 53 | The end-of-Impulse tests (adjacent, LOS, same Stopped or Motion status), no ESB or Minimum Move, one Bog, Mechanical Reliability, or Stall DR for the platoon with Random Selection. | audit |
| D14.211 | Motion Attempt | 212 | not built |  |  | Pass 53 | Motion Attempt for a platoon; the Motion attempt of D2.401 is not in the code either (no match in Play or Rules). | audit |
| D14.212 | Offboard Movement | 212 | not built |  |  | Pass 53 | A platoon partly on and partly off the board; needs vehicle entry and exit by Impulse. | audit |
| D14.22 | Gaps | 213 | not built |  |  | Pass 53 | Gaps: who leaves a platoon, the two survivors closing up, a Recalled radioless AFV treated as radio-equipped. | audit |
| D14.23 | Non-Platoon Movement | 213 | not built |  |  | Pass 53 | The NTC for a lone radioless AFV to move, Delay MP on failure, +1 on its Motion dr. A lone T-34 moves freely today (the R7.1 deviation). | audit |
| D14.24 | Radio Equipped | 214 | not built |  |  | Pass 53 | Radio-equipped AFV choosing Platoon Movement. | audit |
| D14.3 | Impulse Movement | 214 | not built |  |  | Pass 53 | Impulse Movement as a multi-Location stack. No stack that spans Locations exists; shared with Human Wave (A25.23, R15.6 not built). | audit |
| D14.31 | Impulse | 214 | not built |  |  | Pass 53 | One expenditure per unit per Impulse; the Impulse costs every unit the largest expenditure. | audit |
| D14.32 | First Fire | 214 | not built |  |  | Pass 53 | First Fire, Motion Attempts, and smoke dispensers only at the end of an Impulse; all movers one stack. | audit |
| D14.33 | Armored Assault | 214 | not built |  |  | Pass 53 | Needs Armored Assault (D9.31, R6.1 not built) and Impulse Movement. | audit |
| D14.331 | Breaking Off | 214 | not built |  |  | Pass 53 | Breaking off Armored Assault within Impulse Movement. | audit |
| D14.332 | Human Wave | 214 | not built |  |  | Pass 53 | Needs Human Wave (A25.23) and Armored Assault. | audit |
| D14.333 | Platoon | 214 | not built |  |  | Pass 53 | Platoon with Armored Assault. | audit |

#### D15 Motorcycles & Bicycles (pages 214 to 215)

27 rows: 26 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D15 | Motorcycles & Bicycles | 214 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| D15.1 | (none) | 214 | not built |  |  | Pass 55 | The head is on the page (render of p. 214; the text layer drops "15.1" beside the counter art). No motorcycle counter in the catalog; "motorcycle" exists only as a vocabulary movement type and a rendering glyph. Inventory: In the PDF outline; no head with this number was found in the page's text layer. | audit |
| D15.2 | Portage | 214 | not built |  |  | Pass 55 | Sidecar portage 3, 2, or 1 PP by size; cycles none. | audit |
| D15.3 | Stacking | 214 | not built |  |  | Pass 55 | Only Riders count for stacking; no effect on other vehicles' costs. | audit |
| D15.4 | Movement | 214 | not built |  |  | Pass 55 | No Inherent Driver; mounted or Pushed as a Gun by its M#; not in the APh; no vehicle may carry or tow one. | audit |
| D15.41 | Mount/Dismount | 214 | not built |  |  | Pass 55 | Mount or dismount: 1 MF plus a quarter of the MP; 1 MF lost per quarter of MP used. | audit |
| D15.42 | (none) | 214 | not built |  |  | Pass 55 | No Reverse; no wreck or vehicle hex penalty; normal Start, Stop, VCA costs. | audit |
| D15.43 | OVR | 214 | not built |  |  | Pass 55 | Riding through an enemy hex; a sidecar OVR adding a quarter of the Riders' FP; no voluntary dismount with a Known enemy. | audit |
| D15.44 | Splitting | 215 | not built |  |  | Pass 55 | Splitting and recombining Motorcycle counters with Deployment and losses. | audit |
| D15.45 | Terrain Restrictions | 215 | not built |  |  | Pass 55 | Terrain restrictions on entry only. | audit |
| D15.46 | Wreck Check | 215 | not built |  |  | Pass 55 | Wreck Check dr in shellholes, streams, and off-road Elevated Road, Double Crest, or Abrupt Elevation crossings; a 6 breaks and dismounts the Rider. | audit |
| D15.47 | Bog | 215 | not built |  |  | Pass 55 | Not subject to Bog. No motorcycle exists, so the Bog code has no such case. | audit |
| D15.5 | Target Status | 215 | not built |  |  | Pass 55 | Fire is at the Rider: Infantry Target Type, -1 TH or -1 IFT, ordnance hits on the IFT, no FFMO or FFNAM, vehicular TH DRM. | audit |
| D15.51 | KIA | 215 | not built |  |  | Pass 55 | A KIA removes Rider and motorcycle; no wreck. | audit |
| D15.52 | K/# | 215 | not built |  |  | Pass 55 | Casualty Reduction by Random Selection and the counter swapped down to size. | audit |
| D15.53 | #MC | 215 | not built |  |  | Pass 55 | A failed MC forces a Bail Out when moving; the motorcycle stays, recovered as a SW. | audit |
| D15.54 | PTC | 215 | not built |  |  | Pass 55 | No PTC against a motorcyclist. | audit |
| D15.55 | LLMC/LLTC | 215 | not built |  |  | Pass 55 | No LLMC or LLTC to or from a motorcyclist. | audit |
| D15.56 | Wounds | 215 | not built |  |  | Pass 55 | A wounded motorcyclist keeps 4 MF and stays mounted. | audit |
| D15.6 | Rider Fire | 215 | not built |  |  | Pass 55 | No fire from cycles; half FP from sidecars; LMG or Thrown DC only. | audit |
| D15.7 | Capture | 215 | not built |  |  | Pass 55 | Recovered as a SW, not captured; no captured-use penalty. | audit |
| D15.8 | Bicycles | 215 | not built |  |  | Pass 55 | Bicycles as SW on cycle counters; no bicycle counter in the catalog. | audit |
| D15.81 | Movement | 215 | not built |  |  | Pass 55 | Infantry MF, halved along roads with exceptions (shellhole, entrenchment, uphill, dirt road in Mud, unplowed road in Snow), downhill and road bonuses, no Assault Movement. | audit |
| D15.82 | Portage | 215 | not built |  |  | Pass 55 | No portage for bicyclists; a bicycle is 1 PP; not moved in the APh. | audit |
| D15.83 | OVR | 215 | not built |  |  | Pass 55 | No OVR and no entry of a Known enemy unit's Location. | audit |
| D15.84 | Wreck Check | 215 | not built |  |  | Pass 55 | No Wreck Check in shellholes. | audit |
| D15.85 | Target Status | 215 | not built |  |  | Pass 55 | Bicyclists are plain Infantry targets (PTC, FFMO, FFNAM apply) with the -1 mounted DRM; no Bail Out. | audit |

#### D16 DD Tanks & Amphibians (pages 215 to 216)

14 rows: 13 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D16 | DD Tanks & Amphibians | 215 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| D16.1 | (none) | 215 | not built |  |  | Pass 55 | DD screens state; lost to any ordnance HE or HEAT hit on land. No DD tank in the catalog. | audit |
| D16.11 | (none) | 216 | not built |  |  | Pass 55 | Dropping screens in a friendly MPh on land or Wading (G13.42). | audit |
| D16.12 | (none) | 216 | not built |  |  | Pass 55 | Screens erect: CE to move, no fire, VBM, Riders, or listed terrain; COT +1. | audit |
| D16.2 | (none) | 216 | not built |  |  | Pass 55 | Mixed amphibious and land MP in one MPh; always in Motion in water. The catalog carries "amphibious-mp" for its five vehicles, all unprinted, and no code reads it. | audit |
| D16.21 | (none) | 216 | not built |  |  | Pass 55 | One amphibious MP into a Water Obstacle. The planner refuses water to vehicles (backlog: water and fording, pass 41). | audit |
| D16.22 | (none) | 216 | not built |  |  | Pass 55 | Drift in the APh; needs current (B21.12). | audit |
| D16.23 | (none) | 216 | not built |  |  | Pass 55 | Bog check on the waterline hex when leaving water. | audit |
| D16.3 | (none) | 216 | not built |  |  | Pass 55 | Very Small target and HD in water. | audit |
| D16.4 | (none) | 216 | not built |  |  | Pass 55 | Unarmored on the IFT with non-ordnance FP halved (DD, DUKW); armored amphibians immune to Small Arms. | audit |
| D16.5 | (none) | 216 | not built |  |  | Pass 55 | Sunk: no wreck, crew and Passengers eliminated. | audit |
| D16.6 | (none) | 216 | not built |  |  | Pass 55 | No LOS Hindrance in water. | audit |
| D16.7 | (none) | 216 | not built |  |  | Pass 55 | No Riders in water. | audit |
| D16.8 | (none) | 216 | not built |  |  | Pass 55 | May stay offboard until the owner enters them in a friendly MPh. | audit |

#### D17 Aerosans (pages 216 to 217)

13 rows: 12 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| D17 | Aerosans | 216 | not applicable |  |  |  | Heading; footnote 14 only. The coverage document has no row for it. | me |
| D17.1 | (none) | 216 | not built |  |  | Pass 55 | A sixth land movement type (AS); the vocabulary's movement-type list has five and no Aerosan counter exists. | me |
| D17.2 | Movement | 216 | not built |  |  | Pass 55 | Moves only in Ground or Deep Snow, by the Aerosan Movement Table (p. 216): 1, 2, 3, 1+COT, DR+COT, 4+COT. Needs its own cost table. | me |
| D17.21 | Prohibited Movement | 216 | not built |  |  | Pass 55 | Prohibited: gully, stream, off-road entry of Sunken, Elevated, Woods, Brush Roads, unbreached wall or hedge, Reverse, Bypass, Trail Break. | me |
| D17.22 | Aerosan Wreck Check | 216 | not built |  |  | Pass 55 | Wreck Check dr: a 6 immobilizes, Riders Bail Out, Passengers and Crew take a NMC; Wire fails it automatically. Wire is not read by the code at all. | me |
| D17.23 | Railroad | 216 | not built |  |  | Pass 55 | Railroads (B32, not built). | me |
| D17.24 | Bog | 216 | not built |  |  | Pass 55 | Not subject to Bog. | me |
| D17.25 | VCA Change | 216 | not built |  |  | Pass 55 | VCA change of one hexspine outside the MPh, and one per hex in the MPh. | me |
| D17.26 | Towing | 217 | not built |  |  | Pass 55 | 2 extra MP per hex when towing a Gun. | me |
| D17.27 | Straying | 217 | not built |  |  | Pass 55 | Straying (E1.53) for an Aerosan; Straying itself has no match in the code. | me |
| D17.3 | Riders | 217 | not built |  |  | Pass 55 | Riders up to 7 PP on top of Passenger capacity. | me |
| D17.4 | Winter Camouflage | 217 | not built |  |  | Pass 55 | Always Winter Camouflage (E3.712). | me |
| D17.5 | Close Combat | 217 | not built |  |  | Pass 55 | -1 DRM on all CC attacks against an Aerosan. | me |

### Chapter E: Miscellaneous

#### E. Introduction (page 222)

6 rows: 1 built, 4 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E.1 | Optional/SSR | 222 | built | GamePlanner.Night.cs: NightAndWeatherRulesBar; GameState.cs: Night, Weather | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup |  | Night and weather apply only when an SSR token (night:n, weather:kind) names them; nothing of Chapter E runs otherwise. | audit |
| E.2 | Rules Order | 222 | not applicable |  |  |  | A precedence statement between rule cases; nothing to implement. | audit |
| E.3 | Random Location | 222 | not built |  |  | Pass 34b | No Random Location DR (C1.31 procedure). Needed by E2.41 and E3.75. | audit |
| E.4 | Majority Squad Type | 222 | not built |  |  | Pass 34b | No Majority Squad Type computation (US#-weighted, ties to the least advantageous). Needed by E1.23, E1.551, E1.6. | audit |
| E.5 | Aerial Range | 222 | not built |  |  | Pass 40 | No Aerial units; Aerial Range is not computed. | audit |
| E.6 | Aerial LOS Hindrances | 222 | not built |  |  | Pass 40 | No Aerial units; SMOKE as an Aerial Hindrance is not computed. | audit |

#### E1 Night (pages 222 to 227)

77 rows: 13 built, 1 built with a deviation, 15 partly, 44 not built, 4 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E1 | Night | 222 | not applicable |  |  |  | Heading. | audit |
| E1.1 | Night Visibility Range (NVR) | 222 | built | GameState.cs: NightRule, Nvr, Night; GamePlanner.Night.cs: NvrOf, NightSight | BacklogPass16Tests.FireBeyondNvrIsRefusedAndWithinItTakesTheNightDrm |  | Night only by SSR night:n; the NVR is measured in hexes of range (ruling R16.1). | audit |
| E1.101 | Beyond NVR | 222 | partly | GamePlanner.Night.cs: NightSight; GamePlanner.FireExtensions.cs (concealment gain read); GamePlanner.Starshells.cs: PlanStarshell | BacklogPass16TablePlayerTests.AUnitBeyondNvrWithoutGunflashIsRefused; AnIlluminatedFirerSeesOnlyLightAndGunflashes | Pass 34b | Built for fire, Starshells, and concealment gain. Missing: the NVR in every other LOS read (rout from a Known enemy, berserk charge, vehicle concealment, Mopping Up), which read the LOS as by day. | audit |
| E1.11 | DYO | 222 | not built |  |  | Deferred with Chapter H: a DYO chart, purchase, or dr; a card's SSR names the value | The DYO NVR Table DR and NVR Modifier are not rolled; the SSR names the Base NVR and may name the sky (ruling R16.1). | audit |
| E1.12 | NVR Change | 222 | built | GamePlanner.Night.cs: WindChangeDue, AddWindChange | BacklogPass16Tests.TheWindChangeDrChangesTheNvr; BacklogPass16TablePlayerTests.WhiteFourBeforeTheFirstStarshellRaises; ScatteredCloudsAndAFullMoon; NvrLimits |  | Colored 6, white dr direction, the further dr for Scattered clouds with a Half or Full Moon, limits 0 to 6. IR does not exist, so only a Starshell ends the white-4 rise. | audit |
| E1.13 | Zero NVR | 223 | partly | GamePlanner.Night.cs: NightSight | BacklogPass16TablePlayerTests.NvrZeroSeesOnlyItsOwnLocation | Pass 34b | NVR 0 sees only its own hex. Missing: the moving ATTACKER with NVR 0 entering a concealed DEFENDER's Location stays there, takes TPBF, and gets a CC counter (it is returned as by day). | audit |
| E1.14 | Vehicular NVR | 223 | partly | GamePlanner.Night.cs: NightSight, NvrOf | BacklogPass16TablePlayerTests.AMovingTruckIsSeenAtOneAndAHalfNvr; AMovingTankIsSeenAtTwiceNvr; NvrZeroSeesAMovingTruckOnlyOneHexAway | Pass 34b | Motion vehicle at 1.5 times (FRU) or twice NVR, NVR 0 as 1 or 2, BU AFV NVR halved (FRD). Missing: a vehicle changing its VCA without moving (ruling R16.2 records it). | audit |
| E1.15 | Snow | 223 | partly | GamePlanner.Night.cs: NvrLimits, NightAndWeatherRulesBar | BacklogPass16TablePlayerTests.NvrLimits | Pass 34b | Limits 2 to 9 with Ground or Deep Snow are built. Missing: the minimum of 0 between units in the same building. | audit |
| E1.16 | Fortifications | 223 | not built |  |  | Pass 37 | Fortifications have no effect in the game at all; none is hidden at night and no entry cost is waived. | audit |
| E1.17 | Factories | 223 | not built |  |  | Pass 42 | No Factory read; NVR 1 inside a Factory is absent. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E1.2 | Scenario Defender | 223 | not built |  |  | Pass 34b | No automatic 25 percent HIP, no Dummy allotment per squad-equivalent, no "?"/HIP outside Concealment Terrain for a night Scenario Defender. HIP exists only by the hip:side:n SSR (R23.5). | audit |
| E1.21 | Freedom of Movement | 223 | not built |  |  | Pass 34b | No Freedom of Movement: every Scenario Defender unit may move from the start; no dr for the best leader, no No Move state. | audit |
| E1.22 | ELR Loss | 223 | not applicable |  |  |  | The ELR is printed already lowered on the card's OB; the DYO assumption belongs to the DYO purchase (Chapter H), which is not built. | audit |
| E1.23 | Recon | 223 | not built |  |  | Pass 34b | No Recon dr or reveal of chosen hexes; needs E.4. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E1.3 | Concealment | 223 | not applicable |  |  |  | Introduces E1.31 to E1.33; no rule of its own. | audit |
| E1.31 | Loss | 223 | built | GamePlanner.Movement.cs: Unmask (in the move plan); GamePlanner.CloseCombat.cs (advance) | BacklogPass16TablePlayerTests.MovingInTheDarkKeepsConcealment; MovingIntoLightLosesConcealment; AdvancingAtNightKeepsConcealment |  | "?" lost only by Non-Assault Movement in an Illuminated Location or entering an enemy Location. The Cloaking half belongs to E1.4. Fire by a concealed unit at night is refused as undecided in one case (backlog, pass 31d). | audit |
| E1.32 | Gain | 223 | built | GamePlanner.FireExtensions.cs (concealment gain: needsDr) | BacklogPass16Tests.AtNightConcealmentIsGainedWithoutADr; BacklogPass16TablePlayerTests.NightConcealmentGainAndNvr |  | A case that needs a "?" dr by day gains "?" without one at night. | audit |
| E1.33 | NVR | 223 | partly | GamePlanner.Night.cs: NightSight; GamePlanner.Starshells.cs: Seen | BacklogPass16Tests.TheFirstStarshellNeedsAnEnemyUnitSeenWithinNvr | Pass 34b | A unit beyond NVR is not a target and not seen for Starshells. Missing: "Known" at night in rout, berserk charge, and other non-fire reads. | audit |
| E1.4 | Cloaking | 223 | not built |  |  | Pass 34b | No Cloaking counters at all. | audit |
| E1.41 | Contents | 223 | not built |  |  | Pass 34b | No Cloaking contents or Cloaking Box. | audit |
| E1.411 | Setup | 224 | not built |  |  | Pass 34b | No Cloaking allotment or offboard setup of the Scenario Attacker. | audit |
| E1.42 | MF & SW | 224 | not built |  |  | Pass 34b | No six MF Cloaking counter, no 4 or 5 PP SW as 3 PP. | audit |
| E1.421 | MF Costs | 224 | not built |  |  | Pass 34b | No Cloaking MF costs; Climbing (the +1 Falling DRM) is not built either. | audit |
| E1.422 | Stacking | 224 | not built |  |  | Pass 34b | No Cloaking stacking or combining. | audit |
| E1.423 | Human Wave | 224 | not built |  |  | Pass 34b | No Cloaking; Human Wave is not built either. | audit |
| E1.43 | Loss | 224 | not built |  |  | Pass 34b | No Cloaking loss. | audit |
| E1.5 | Movement | 224 | not applicable |  |  |  | Heading; introduces E1.51 to E1.56. | audit |
| E1.51 | On Foot | 224 | built | GamePlanner.Night.cs: InfantryWeatherHalfMf; GamePlanner.Terrain.cs: InfantryStep; GamePlanner.Movement.cs (Double Time and road bonus at NVR 0) | BacklogPass16Tests.NightAndMudCostMoreMf; BacklogPass16TablePlayerTests.NoDoubleTimeAtNvrZero; DeepSnowInfantryMf |  | +1 MF into Concealment Terrain after all modifications, not across a road hexside and not in Bypass. Cavalry and Gallop do not exist. | audit |
| E1.52 | Vehicular | 224 | partly | GamePlanner.Night.cs: VehicleWeatherHalfMp; GamePlanner.Vehicles.cs (play.night-bu) | BacklogPass16TablePlayerTests.TankOpenGroundMp | Pass 45 | +1 MP per hexside and the BU AFV with NVR 0 are built. Missing: the +1 MP in VBM (the VBM cost path adds no night cost); Passengers unloading from a halted BU AFV. | audit |
| E1.53 | Straying | 224 | not built |  |  | Pass 34b | No Movement DR, no Straying DR, no forced move along a Hex Grain, no TI. | audit |
| E1.531 | Exceptions | 224 | not built |  |  | Pass 34b | No Straying, so no exceptions. | audit |
| E1.532 | Friendly Contact | 224 | not built |  |  | Pass 34b | No Straying. | audit |
| E1.533 | Berserk | 224 | not built |  |  | Pass 34b | No Straying; a berserk unit's Known enemy at night is read as by day. | audit |
| E1.54 | Routing | 224 | built | GamePlanner.Rout.cs: PlanRout (play.night-rout), FailureToRout; GamePlanner.CloseCombat.cs (no surrender on advance); GamePlanner.cs (night DM kept) | BacklogPass16Tests.AtNightABrokenUnitLowCrawls; AtNightDmStaysWithoutAQualifyingRallyDr; BacklogPass16TablePlayerTests.NoFailureToRoutAtNight; NightLowCrawlNeedNotGoTowardWoods; NoLowCrawlTowardAKnownEnemy; NightDmAndTheRallyDr |  | Ruling R16.6. A broken Inherent Crew routing out of its vehicle is not built here or by day. | audit |
| E1.55 | Jitter Fire | 225 | not built |  |  | Pass 34b | No Jitter Fire (needs Straying). | audit |
| E1.551 | Unit Determination | 225 | not built |  |  | Pass 34b | No Jitter Fire. | audit |
| E1.552 | Effect | 225 | not built |  |  | Pass 34b | No Jitter Fire. | audit |
| E1.56 | Recovery | 225 | built | GamePlanner.SupportWeapons.cs: PlanRecover (drm) | BacklogPass16TablePlayerTests.RecoveryAtNight |  | +1 to the Recovery dr at night. | audit |
| E1.6 | Lax/Normal/Stealthy | 225 | not built |  |  | Pass 34b | No Lax, Normal, Stealthy classification at night. | audit |
| E1.61 | Stealthy | 225 | not built |  |  | Pass 34b | No Stealthy units at night. | audit |
| E1.62 | Lax | 225 | not built |  |  | Pass 34b | No Lax units at night (the day Lax of A11.18 exists for CC only). | audit |
| E1.63 | Normal | 225 | not built |  |  | Pass 34b | No Normal class; a Lax unit with a Good Order SMC is not made Normal. | audit |
| E1.7 | Combat | 225 | built | GamePlanner.Night.cs: NightAndWeatherFacts; ScenarioA1FireCalculator.cs (lv-hindrance); ScenarioA1OrdnanceCalculator.cs (case-r:lv) | BacklogPass16Tests.FireBeyondNvrIsRefusedAndWithinItTakesTheNightDrm; BacklogPass16TablePlayerTests.NoNightLvAtATargetInWoodsAFullLevelUp; FirerInWoodsAtOpenGroundTakesTheNightLv; ADcAttackTakesNoNightLv; ScenarioA1Pass16Tests.TheLowVisibilityDrmIsAddedAndBoundedAndNeverTakenByResidualFp |  | Ruling R16.3. The WA over bocage, mines, and OBA exceptions cannot arise (those are not built). | audit |
| E1.71 | Fire Lane | 225 | not built |  |  | Pass 34b | A Fire Lane needs the NVR as other fire; no Fire Lane counter beyond NVR and no Bore Sighted Fire Lane without a target. | audit |
| E1.72 | Snipers | 225 | not built |  |  | Pass 34b | No Cloaking counter for a Sniper to remove. | audit |
| E1.73 | To Hit | 225 | not built |  |  | Pass 34b | No Blind Hex To Hit case for a unit entering another's NVR. | audit |
| E1.74 | Target Acquisition | 225 | built with a deviation | GamePlanner.Acquisition.cs (no night read) | none found | Pass 34b | Ruling R16.2: an Acquisition applies at night as by day. The rule makes it NA unless the target is Illuminated and removes it when Illumination ends. | audit |
| E1.75 | FG | 225 | built | GamePlanner.Night.cs: NightAndWeatherFacts (play.night-fire-group) | BacklogPass16Tests.NoFireGroupSpansLocationsAtNight; BacklogPass16TablePlayerTests.OneLocationFireGroupAtNight |  | Refuses a multi-Location FG at night. | audit |
| E1.76 | Mistaken Fire | 225 | partly | GamePlanner.Snipers.cs (SAN + 2, to 7) | BacklogPass16Tests.AtNightTheSanIsTwoHigher; BacklogPass16TablePlayerTests.SanCappedAtSeven | Pass 34b | Missing: the automatic Sniper attack dr on a captured MG that fires; the Sniper Check floor (Sniper Check is not built). | audit |
| E1.77 | CC | 225 | built | GamePlanner.CloseCombat.cs (DarkNight); ScenarioA1CloseCombatCalculator.cs (Ambush margin) | BacklogPass16TablePlayerTests.NightAmbushMargin |  | ATTACKER ambushes with a dr two lower unless the Location is Illuminated (ruling R16.7). | audit |
| E1.8 | Gunflashes | 226 | partly | GamePlanner.Night.cs: Gunflash; GameProjector.cs (First and Final Fire counters kept until the end of the AFPh) | BacklogPass16TablePlayerTests.EachFireCounterIsAGunflashBeyondNvr; BacklogPass16Tests.AtNightFirstFireCountersStayUntilTheEndOfTheAfph | Pass 34b | A Gunflash is read from fire and Melee counters on units and weapons now in the Location. Missing: a flash that stays in the Location after its firer leaves, a flash for a firer that kept ROF and has no counter, the generic Gunflash counter. | audit |
| E1.81 | Beyond NVR | 226 | partly | GamePlanner.Night.cs: NightSight; ScenarioA1FireCalculator.cs (area-fire-gunflash); GamePlanner.Ordnance.cs (play.night-ordnance) | BacklogPass16Tests.AGunflashBeyondNvrIsAreaFire; BacklogPass16TablePlayerTests.AConcealedGunflashIsHalvedOnce; ScenarioA1Pass16Tests.FireAtAGunflashBeyondNvrIsHalvedOnceOnly | Pass 34b | Built for IFT fire, halved once. Ordnance at a Gunflash beyond NVR is refused (ruling R16.2). | audit |
| E1.82 | CC | 226 | built | GamePlanner.Night.cs: Gunflash (Melee condition) | BacklogPass16TablePlayerTests.AMeleeIsAGunflashBeyondNvr |  | A Melee marks a Gunflash while it lasts. | audit |
| E1.83 | Mines | 226 | not built |  |  | Pass 38 | Mines are not built. | audit |
| E1.84 | MOL/FT | 226 | partly | GamePlanner.Night.cs: Gunflash | none found | Pass 34b | A FT firer's own Location is marked only through its fire counter (judged from the counter list, not traced). Missing: the Gunflash in the Location a FT or MOL attacks. MOL was not found in the catalog. | audit |
| E1.85 | DC/ATMM | 226 | not built |  |  | Pass 54 | No Gunflash at the Location where a DC detonates; ATMM does not exist. | audit |
| E1.86 | Searching/Mopping Up | 226 | not built |  |  | Pass 34b | Mopping Up has no night read; no Gunflash when the Searcher suffers Casualty Reduction. Searching is not built. | audit |
| E1.87 | SR/FFE | 226 | not built |  |  | Pass 39 | No OBA: no FFE Blast Area or SR. | audit |
| E1.88 | PIAT | 226 | not built |  |  | Pass 54 | No PIAT in the catalog. When one is added, its fire counter would wrongly mark a Gunflash unless excepted. | audit |
| E1.89 | dm/Radio/Spotting/Ammo-Vehicle | 226 | not built |  |  | Pass 34b | No exception exists. Dismantling or assembling the MMG sets a Prep or Final Fire condition on the weapon, which the Gunflash read counts (see Faults). | audit |
| E1.9 | Illumination | 226 | built | GamePlanner.Night.cs: Illuminated, NightSight | BacklogPass16Tests.AStarshellIlluminatesATargetBeyondNvr; BacklogPass16TablePlayerTests.AnIlluminatedFirerSeesOnlyLightAndGunflashes |  | An Illuminated Location is within every NVR; an Illuminated viewer sees only Illuminated Locations and Gunflashes. | audit |
| E1.91 | Initial Use | 226 | partly | GamePlanner.Starshells.cs: PlanStarshell (play.starshell-first) | BacklogPass16Tests.TheFirstStarshellNeedsAnEnemyUnitSeenWithinNvr; BacklogPass16TablePlayerTests.TheFirstStarshellNeedsAnEnemyWithinNvr | Pass 34b | Built: an enemy unit seen, or any Gunflash on the map. Missing: the enemy motorized vehicle moving within 16 hexes; the Gunflash is not limited to one from an enemy FFE or an attack on an enemy unit. | audit |
| E1.92 | Starshells | 226 | built | GamePlanner.Starshells.cs: PlanStarshell; GameGate.cs (attempt per hex) | BacklogPass16TablePlayerTests.StarshellPhases; AFailedUsageDrUsesTheHexAttempt |  | PFPh, DFPh, or Defensive First Fire; one attempt per hex by a leader, CE AFV, or MMC. | audit |
| E1.921 | Usage Restrictions | 226 | partly | GamePlanner.Starshells.cs: PlanStarshell | BacklogPass16TablePlayerTests.AnMmcUsageDr; AfterTheFirstPlayerTurnAnMmcFiresOnlyAtTheStart; AStarshellIsNotFiring; AHiddenStarshellFirerIsConcealed | Pass 34b | Usage dr 4 or 2, timing after the first Player Turn, no Gunflash, hidden firer under "?". Missing: no fire from an Interior Building Hex; a CE Armor Leader's Usage dr of 4 (a CE AFV always needs 2). Inventory: The PDF outline points at page 227; the rule is printed on page 226. | audit |
| E1.922 | Placement | 226 | partly | GamePlanner.Starshells.cs: PlanStarshell | BacklogPass16TablePlayerTests.AtTargetNeedsAKnownUnitOrAGunflash; AStarshellAtTheMapEdge | Pass 34b | Ruling R16.8. Missing: method 2 at a hex along the LOS and with a target seven or eight hexes away; the fall back to method 3 when the free LOS check fails (it is refused); a Starshell that leaves the map stops at the edge. Inventory: The PDF outline points at page 227; the rule is printed on page 226. | audit |
| E1.923 | Effects & Duration | 227 | partly | GamePlanner.Night.cs: Illuminated; GameProjector.cs (Starshells removed at the end of the CCPh) | BacklogPass16Tests.AStarshellIlluminatesATargetBeyondNvr | Pass 34b | Three hexes, removed after the CCPh. Missing: Interior Building Locations stay dark; an offboard Starshell. | audit |
| E1.93 | Illuminating Rounds | 227 | not built |  |  | Pass 58 | No IR; no mortar in the catalog lists IR and OBA is not built. | audit |
| E1.931 | Usage | 227 | not built |  |  | Pass 58 | No IR Usage dr or No Fire counter. | audit |
| E1.932 | Placement | 227 | not built |  |  | Pass 58 | No IR placement. | audit |
| E1.933 | Effects & Duration | 227 | not built |  |  | Pass 58 | No six hex Illumination. | audit |
| E1.94 | Fires | 227 | partly | GamePlanner.Night.cs: Illuminated (burning wrecks, two hexes) | none found | Pass 44 | Only a burning wreck Illuminates, at a fixed two hexes. Missing: terrain Blazes and their range of twice the Blazing levels, Illumination at all levels, Kindling at night (loss of "?" and a Gunflash). | audit |
| E1.941 | Shadows | 227 | not built |  |  | Pass 44 | No shadows behind obstacles in a Blaze's zone (ruling R16.8 records it). | audit |
| E1.942 | Flame | 227 | not built |  |  | Pass 44 | A Flame does not Illuminate its Location. | audit |
| E1.95 | Trip Flares | 227 | not built |  |  | Deferred with Chapter G: the rule serves the Pacific only | No trip flares (1944-5 PTO, U.S. Scenario Defender; Chapter G terrain). | audit |
| E1.951 | Effects | 227 | not built |  |  | Deferred with Chapter G: the rule serves the Pacific only | No Trip Flare counter. | audit |
| E1.952 | Elimination | 227 | not built |  |  | Deferred with Chapter G: the rule serves the Pacific only | No trip flare elimination. | audit |
| E1.953 | Search & Recon | 227 | not built |  |  | Deferred with Chapter G: the rule serves the Pacific only | No Search or Recon of trip flares. | audit |

#### E2 Interrogation (page 228)

12 rows: 11 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E2 | Interrogation | 228 | not applicable |  |  |  | Heading. | audit |
| E2.1 | Incidence | 228 | not built |  |  | Pass 49 | No Interrogation DR on capture or surrender. | audit |
| E2.2 | Interrrogation Table | 228 | not built |  |  | Pass 49 | No Interrogation Table or its DRM. | audit |
| E2.21 | Concealed Unit(s) Revealed | 228 | not built |  |  | Pass 49 | No reveal of the closest concealed hex. | audit |
| E2.22 | HIP Unit(s) Revealed | 228 | not built |  |  | Pass 49 | No reveal of the closest HIP hex. | audit |
| E2.23 | Hidden Fortification(s) Revealed | 228 | not built |  |  | Pass 49 | No reveal of hidden Fortifications; Fortifications are not built. | audit |
| E2.24 | Defenses Compromised | 228 | not built |  |  | Pass 49 | No Defenses Compromised result. | audit |
| E2.3 | LOS/Range Restriction | 228 | not built |  |  | Pass 49 | No LOS or eight hex restriction. | audit |
| E2.4 | Civilians | 228 | not built |  |  | Pass 49 | No civilian information on the Wind Change DR (by SSR only). | audit |
| E2.41 | Location | 228 | not built |  |  | Pass 49 | Needs E.3 Random Location DR. | audit |
| E2.42 | Information | 228 | not built |  |  | Pass 49 | No Information Table. | audit |
| E2.43 | Clarifications | 228 | not built |  |  | Pass 49 | Clarification of E2.4; nothing built. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |

#### E3 Weather (pages 228 to 231)

49 rows: 15 built, 2 built with a deviation, 13 partly, 5 refused, 14 not built.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E3 | Weather | 228 | partly | GamePlanner.Night.cs: NightAndWeatherRulesBar; GameState.cs: Weather | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Pass 57 | Weather is Clear unless an SSR names it (ruling R16.9). Missing: the DYO Temperate Weather Chart DR; the precedence of weather over EC (EC is not modeled). | audit |
| E3.1 | Low Visibility (LV) | 228 | built | GamePlanner.Night.cs: NightAndWeatherFacts; ScenarioA1FireCalculator.cs (lv-hindrance) | ScenarioA1Pass16Tests.TheLowVisibilityDrmIsAddedAndBoundedAndNeverTakenByResidualFp; BacklogPass16TablePlayerTests.NightAndMistAtSixHexes |  | LV is cumulative with Hindrances, never negates FFMO, never touches Residual FP, and blocks the LOS at 6 with the other Hindrances. | audit |
| E3.2 | Clear | 229 | built | GameState.cs: Weather (no token is Clear) | BacklogPass16TablePlayerTests.TruckOnARoadInClearWeather |  | Clear weather has no effect. EC (B25.5) is not modeled. | audit |
| E3.3 | Fog/Mist | 229 | partly | GamePlanner.Night.cs: WeatherKinds (mist) | BacklogPass16Tests.MistHindersFireBeyondSixHexes | Pass 57 | Mist by SSR. Missing: the Fog or Mist dr, Fog itself, and EC Moist. | audit |
| E3.31 | Fog | 229 | refused | GamePlanner.Night.cs: NightAndWeatherRulesBar (play.weather-rule) | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Pass 57 | Fog is not a weather token and is refused at setup (ruling R16.9). Needs the Fog Level dr and LOS through Fog levels. | audit |
| E3.311 | Effect | 229 | refused | GamePlanner.Night.cs: NightAndWeatherRulesBar | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Pass 57 | Refused with Fog. Needs the Fog Density dr and Smoke DRM per fogged level. | audit |
| E3.312 | Wind Effects | 229 | not built |  |  | Pass 57 | No Wind Force; nothing would thin Fog. | audit |
| E3.313 | Air Support | 229 | not built |  |  | Pass 40 | No Air Support. Inventory: The PDF outline lists this as 3.13; the page prints 3.313. | audit |
| E3.32 | Mist | 229 | built | GamePlanner.Night.cs: NightAndWeatherFacts (weatherDrm) | BacklogPass16TablePlayerTests.MistByRange; BacklogPass16Tests.MistHindersFireBeyondSixHexes |  | +1 per six hexes or fraction beyond six; not for Fire Lanes or Residual FP. Aerial Range does not exist. | audit |
| E3.4 | Gusty | 229 | partly | GamePlanner.Night.cs: AddWindChange (gust) | BacklogPass16TablePlayerTests.AGust | Pass 56 | A Gust is rolled and recorded on a Wind Change DR of 10 or more; nothing reads it (no SMOKE dispersal, no fire spread). | audit |
| E3.5 | Overcast | 229 | partly | GamePlanner.Night.cs: AddWindChange, WindChangeDue | BacklogPass16Tests.RainStartsOnAWindChangeDrOfTen | Pass 57 | Overcast enables the rain check. Missing: Cloud Cover forced to Overcast for the NVR (a night-clouds SSR may contradict it unchecked); EC. | audit |
| E3.51 | Rain | 229 | built with a deviation | GamePlanner.Night.cs: AddWindChange | BacklogPass16Tests.RainStartsOnAWindChangeDrOfTen; BacklogPass16TablePlayerTests.RainIntensifiesThenStops | Pass 56 | Ruling R16.10: the Wind Change DR of the opening RPh is not made. Starts on 10, heavier on a second 10, ends on 3 or less. EC Wet is not modeled. | audit |
| E3.52 | LV | 229 | built | GamePlanner.Night.cs: NightAndWeatherFacts (mist from precipitation) | BacklogPass16TablePlayerTests.PrecipitationCausesMist; HeavyRainByRange |  | Rain causes Mist. | audit |
| E3.53 | Smoke | 229 | partly | GamePlanner.Smoke.cs: PlanSmoke (play.smoke-weather) | none found for rain (BacklogPass16Tests.NoSmokeIsPlacedInMud reaches the same check) | Pass 57 | SMOKE grenades are refused in rain but inside a building. Missing: SMOKE already on the map when rain starts is not removed; ordnance and vehicle SMOKE are not built. | audit |
| E3.54 | Movement | 229 | built | GamePlanner.Night.cs: InfantryWeatherHalfMf, VehicleWeatherHalfMp, Rain | BacklogPass16Tests.RainThatStoppedHasStillFallen |  | +1 MF or MP per level changed during and after rain, not by stairwell or paved road. Not added in Infantry Bypass or VBM (see E3.9). | audit |
| E3.55 | Air Support | 229 | not built |  |  | Pass 40 | No Air Support. | audit |
| E3.6 | Mud | 229 | partly | GamePlanner.Night.cs: InfantryWeatherHalfMf (road rate lost); GamePlanner.VehicleTerrain.cs (unpaved road as Open Ground) | BacklogPass16TablePlayerTests.TruckOnAnUnpavedRoadInMud; BacklogPass16Tests.NightAndMudCostMoreMf | Pass 57 | Road bonus lost on unpaved roads; paved roads unaffected. Missing: the limit on fire spread, EC Mud, runways. | audit |
| E3.61 | Bog/Manhandling | 230 | partly | GamePlanner.VehicleTerrain.cs (Bog DRM for mud) | BacklogPass16TablePlayerTests.BogDrmInMudAndDeepSnow | Pass 57 | +1 on a Bog DR that some other cause calls for. Missing: the D8.23 Bog check Mud itself causes; the Manhandling DRM (GamePlanner.Guns.cs has no weather read). | audit |
| E3.62 | HE Attacks | 230 | built | GamePlanner.Night.cs: NightAndWeatherFacts (CushionedOpenGround); GamePlanner.Ordnance.cs; ScenarioA1FireCalculator.cs (weather-cushion, Residual FP); ScenarioA1OrdnanceCalculator.cs (case-q:weather-cushion) | ScenarioA1OrdnanceTests.AtNightTheLowVisibilityDrmIsACaseRHindranceOfItsOwnAndMudCushi... (name cut in my search) |  | +1 TEM to an ordnance HE hit in Open Ground, Residual FP one step lower. Open Ground here is the open-ground terrain key only (see E3.65). | audit |
| E3.63 | Entrenching | 230 | not built |  |  | Pass 37 | Entrenching is not built. | audit |
| E3.64 | Movement | 230 | partly | GamePlanner.Night.cs: InfantryWeatherHalfMf, VehicleWeatherHalfMp | BacklogPass16Tests.MudCostsHalfAnMfMoreInOpenGround; BacklogPass16TablePlayerTests.TankOpenGroundMp | Pass 45 | Built for a step into Open Ground. Missing: Infantry on an unpaved road in a hex whose other terrain is not Open Ground pay no half MF; Infantry Bypass and VBM add nothing. | audit |
| E3.65 | Open Ground | 230 | partly | GamePlanner.Night.cs (terrain key open-ground); GamePlanner.VehicleTerrain.cs (unpaved road in Mud) | BacklogPass16TablePlayerTests.TruckOnAnUnpavedRoadInMud | Pass 45 | Only the open-ground key, plus an unpaved road for vehicles. Missing: gullies, dry streams, plowed fields, shellholes, trenches, unplowed roads in Deep Snow. | audit |
| E3.7 | Snow | 230 | not built |  |  | Pass 57 | No Snow Chart dr; the SSR names the snow conditions (ruling R16.9). | audit |
| E3.71 | Falling Snow | 230 | built | GamePlanner.Night.cs: AddWindChange (snowing); GameState.cs: PrecipitationRule | BacklogPass16TablePlayerTests.HeavySnowfall |  | Stops on 3 or less, restarts on 10 or more, heavier on a second 10. The opening RPh DR is not made (ruling R16.10). | audit |
| E3.711 | LV | 230 | built | GamePlanner.Night.cs: NightAndWeatherFacts | BacklogPass16TablePlayerTests.PrecipitationCausesMist |  | Falling Snow causes Mist. | audit |
| E3.712 | Winter Camouflage | 230 | not built |  |  | Pass 57 | No Winter Camouflage: no +1 LV at range, no -1 Concealment drm, no kept "?" in Open Ground. No unit carries the property. | audit |
| E3.713 | EC | 230 | not built |  |  | Pass 57 | EC is not modeled; no frigid streams. | audit |
| E3.72 | Ground Snow | 230 | not built |  |  | Pass 57 | EC Wet is not modeled and Winter Camouflage is not built. The ground-snow token exists for E3.723 and E3.724 only. | audit |
| E3.721 | Fires | 230 | not built |  |  | Pass 57 | Fire spread is not built. | audit |
| E3.722 | Terrain | 230 | not built |  |  | Pass 41 | Marsh stays marsh, streams do not freeze, no Ice, no Entrenching (ruling R16.13 records it). | audit |
| E3.723 | Infantry/Cavalry Movement | 230 | built | GamePlanner.Night.cs: InfantryWeatherHalfMf | BacklogPass16TablePlayerTests.DeepSnowInfantryMf (shared code; none found for Ground Snow alone) |  | +1 MF per level changed and no Road Bonus but on a plowed road (SSR plowed-roads). Cavalry does not exist. | audit |
| E3.724 | Vehicular Movement | 230 | built | GamePlanner.VehicleTerrain.cs (road entry at least 1 MP); GamePlanner.Night.cs: VehicleWeatherHalfMp | BacklogPass16TablePlayerTests.TruckOnAnUnplowedRoadInGroundSnow; TruckOnAPlowedRoadInGroundSnow |  | Non-tracked +1 MP per hexside, plowed or not. Not added in VBM. Sledges do not exist. | audit |
| E3.73 | Deep Snow | 230 | not built |  |  | Pass 57 | Its own text is not built: EC Snow, Winter Camouflage, fire spread, the E3.722 terrain changes, brush as Open Ground. The deep-snow token serves E3.731, E3.733, E3.7331 only. | audit |
| E3.731 | HE Attacks | 231 | built | ScenarioA1FireCalculator.cs (weather-cushion); ScenarioA1OrdnanceCalculator.cs (case-q:weather-cushion) | ScenarioA1OrdnanceTests (the Mud cushion test; same code) |  | As E3.62, for Deep Snow. | audit |
| E3.732 | Minefields | 231 | not built |  |  | Pass 38 | Mines are not built. | audit |
| E3.733 | Infantry/Cavalry Movement | 231 | built | GamePlanner.Night.cs: InfantryWeatherHalfMf | BacklogPass16TablePlayerTests.DeepSnowInfantryMf |  | Half an MF more per hexside but into woods, building, or rubble, or by a plowed road. Not added in Bypass. | audit |
| E3.7331 | Vehicular Movement | 231 | built | GamePlanner.Night.cs: VehicleWeatherHalfMp; GamePlanner.VehicleTerrain.cs | BacklogPass16TablePlayerTests.TankOpenGroundMp |  | Tracked +1, non-tracked +2, non-tracked +1 on plowed roads; road entry at least 1 MP. Not added in VBM. | audit |
| E3.7332 | Bog/Manhandling | 231 | partly | GamePlanner.VehicleTerrain.cs (Bog DRM for snow and Deep Snow) | BacklogPass16TablePlayerTests.BogDrmInMudAndDeepSnow | Pass 57 | +1 and +1 on a Bog DR another cause calls for. Missing: the D8.23 Bog check Deep Snow itself causes; the Manhandling DRM. | audit |
| E3.734 | Smoke | 231 | partly | GamePlanner.Smoke.cs: PlanSmoke | BacklogPass16Tests.NoSmokeIsPlacedInMud | Pass 57 | Grenade SMOKE refused in Mud and Deep Snow but inside a building. Ordnance and vehicle SMOKE are not built. | audit |
| E3.74 | Extreme Winter | 231 | built with a deviation | GamePlanner.Night.cs: NightAndWeatherRulesBar | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Stands by its ruling | Ruling R16.14: the SSR names Extreme Winter and its accompanying snow condition; no Snow Chart dr. EC Snow is not modeled. | audit |
| E3.741 | B#/X# | 231 | built | GamePlanner.Night.cs: ExtremeWinterReduction; ScenarioA1FireCalculator.cs; ScenarioA1OrdnanceCalculator.cs (B#) | BacklogPass16TablePlayerTests.ExtremeWinterGermanLmgBreaksOnNine; ExtremeWinterRussians; ScenarioA1Pass16Tests.ExtremeWinterLowersAMgsBreakdownNumber |  | B# and X# 1 lower for Russians before April 1941, 2 for Axis but Finns before April 1942. The ELR advice is the card's. | audit |
| E3.742 | Fate | 231 | built | LiveRally.cs (ExtremeWinterFate); ScenarioA1RallyCalculator.cs | BacklogPass16TablePlayerTests.ExtremeWinterFateOnEleven |  | Original Rally DR of 11 or more outside a building is Casualty Reduction. Pillboxes do not exist. | audit |
| E3.743 | Entrenching | 231 | not built |  |  | Pass 37 | Entrenching is not built, so nothing to forbid. | audit |
| E3.744 | Axis Vehicles | 231 | not built |  |  | Pass 57 | No Immobilization dr for an Axis Scenario Defender's vehicle before its first Start MP (ruling R16.14 leaves it to the cards). | audit |
| E3.75 | Drifts | 231 | refused | GamePlanner.Night.cs: NightAndWeatherRulesBar (play.weather-rule) | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Pass 57 | Drifts are not a weather token and are refused at setup (ruling R16.9). Needs Drift counters, E.3, and Wind Force. | audit |
| E3.751 | (none) | 231 | refused | GamePlanner.Night.cs: NightAndWeatherRulesBar | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Pass 57 | Refused with Drifts. Needs Wind Direction and Roadblock Clearance (B24.76). Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E3.752 | (none) | 231 | refused | GamePlanner.Night.cs: NightAndWeatherRulesBar | BacklogPass16Tests.TheNightAndWeatherRulesAreCheckedAtSetup | Pass 57 | Refused with Drifts. Needs a hexside that takes ALL MF or MP, a Bog Check at +2, and a hedge read. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E3.8 | Buildings | 231 | partly | GamePlanner.Night.cs: NightAndWeatherFacts (range 0) | BacklogPass16TablePlayerTests.MistByRange | Pass 57 | Weather is Clear in the firer's own hex. Missing: Clear weather between Locations of one building across a building hexside (ruling R16.11 records it). | audit |
| E3.9 | MF/MP Cost Additions | 231 | partly | GamePlanner.Terrain.cs: GroundStep; GamePlanner.VehicleTerrain.cs (cost after penalty and towing) | BacklogPass16TablePlayerTests.DeepSnowInfantryMf; TankOpenGroundMp | Pass 45 | Weather costs are added after the step's total. Missing: the same costs per hexside Bypassed (Infantry Bypass and VBM add none). A reversing vehicle multiplies the weather cost with the rest (unsettled, see findings). | audit |

#### E4 Ski Troops (pages 231 to 232)

18 rows: 16 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E4 | Ski Troops | 231 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E4.1 | DYO | 231 | not applicable |  |  |  | Pure cross-reference to DYO purchase (H1.202); Chapter H is deferred. | audit |
| E4.2 | Ski Mode | 231 | not built |  |  | Pass 43 | No ski mode state, no ski counter, no mode switch (2 MF to skis, 1 MF for Finns, free to foot, not if pinned or TI). Needs Ground or Deep Snow as a read state, which exists. | audit |
| E4.21 | Skis | 231 | not built |  |  | Pass 43 | No ski counter: 1 PP carried off skis, eliminated like a SW, ski-use dr of 1 on Recovery or Transfer. | audit |
| E4.22 | Dummy Ski Counters | 231 | not built |  |  | Pass 43 | By SSR only. No Dummy ski counter; needs Dummy stacks (A12.11) that move. | audit |
| E4.3 | Movement | 232 | not built |  |  | Pass 43 | No Skier movement: no snow or Drift surcharge, the list of Locations a Skier may not enter, Bypass of a building hex, no road bonus. | audit |
| E4.31 | Downhill | 232 | not built |  |  | Pass 43 | No +2 MF per Crest Line crossed downhill in MPh or RtPh; needs Crest Lines (B10), which the coverage document lists as a gap. | audit |
| E4.32 | APh | 232 | not built |  |  | Pass 43 | No Skier advance with a mode change counted in the A4.72 MF sum. | audit |
| E4.33 | Routing | 232 | not built |  |  | Pass 43 | No mode change in the RtPh; no bar on Low Crawl on skis. | audit |
| E4.4 | Camouflage | 232 | not built |  |  | Pass 43 | Winter Camouflage (E3.712) is itself not built, so the grant to ski-equipped units has nothing to attach to. | audit |
| E4.5 | CC | 232 | not built |  |  | Pass 43 | No +2 CC attack DRM and -2 when attacked for Skiers; no option to leave Melee or dismount in the MPh. | audit |
| E4.6 | Attack Restrictions | 232 | not built |  |  | Pass 43 | No bar on a Skier firing a Gun, ordnance SW, MMG, or HMG. | audit |
| E4.7 | Berserk | 232 | not built |  |  | Pass 43 | No bar on a berserk unit changing mode, nor the forced dismount at the end of a charge. | audit |
| E4.8 | Ahkio | 232 | not built |  |  | Pass 43 | No Ahkio counter (3 PP SW when portaged). | audit |
| E4.81 | Infantry Use | 232 | not built |  |  | Pass 43 | No +2 PP IPC for an MMC possessing an Ahkio. | audit |
| E4.82 | Movement | 232 | not built |  |  | Pass 43 | No Ahkio terrain limit nor the forced drop on rout, Berserk charge, Human Wave, or break in Bypass. | audit |
| E4.83 | Terrain Limitations | 232 | not built |  |  | Pass 43 | No Ahkio bar at walls, hedges, crag, Alpine Hills. | audit |
| E4.9 | DYO | 232 | not built |  |  | Pass 43 | A single DYO value (Ahkio BPV 1); no Ahkio in the catalog and Chapter H is deferred. | audit |

#### E5 Boats (pages 233 to 234)

25 rows: 23 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E5 | Boats | 233 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E5.1 | Counters | 233 | not applicable |  |  |  | Definition of the three boat types and their four ratings; the counters themselves are missing (see findings part 4). | audit |
| E5.11 | Assault Boats | 233 | not built |  |  | Pass 62 | No Assault Boat: Inherent Driver with 4 MP, or paddled with 2 MP by 5 PP of Personnel. | audit |
| E5.12 | Pneumatic Boats | 233 | not built |  |  | Pass 62 | No Pneumatic boat manning rule (one third of capacity; SMC counts 1 PP). | audit |
| E5.121 | Small Raft | 233 | not built |  |  | Pass 62 | No Small Raft in its three sizes (14, 7, 3 PP), flip on Casualty Reduction, split and recombine on land. | audit |
| E5.122 | Large Raft | 233 | not built |  |  | Pass 62 | No Large Raft carrying a Gun of M# 10 or more (10 PP, M# DR to load and unload). | audit |
| E5.123 | Passengers | 233 | not built |  |  | Pass 62 | No boat Passengers; the Cloaking Box is table procedure a program replaces, but four free SMC and dm SW are rules. | audit |
| E5.2 | Overland Movement | 233 | not built |  |  | Pass 62 | No overland carrying of boats by the Manhandling system; Gun Manhandling (C10.3) exists and could be extended. | audit |
| E5.21 | MPh/Loading | 233 | not built |  |  | Pass 62 | No loading at a Beached boat (1 MF plus hexside), nor MF converted to quarters of the boat's MP. | audit |
| E5.22 | APh | 233 | not built |  |  | Pass 62 | No APh loading or launch of a Beached boat. | audit |
| E5.23 | Beaching | 233 | not built |  |  | Pass 62 | No Beached state straddling a shore hexside, free unBeaching, LOS to the water hex centre, or drift Beaching dr. | audit |
| E5.3 | Water Movement | 234 | not built |  |  | Pass 62 | No movement in Water Obstacle hexes; water is refused to every mover (GamePlanner.Terrain.cs InfantryStep: not a reviewed entry). Needs B21 and current. | audit |
| E5.31 | Stacking | 234 | not built |  |  | Pass 62 | No boat stacking rule. | audit |
| E5.32 | Unloading | 234 | not built |  |  | Pass 62 | No unloading across the Beached hexside; no wait for the APh when the land hex holds an enemy. | audit |
| E5.33 | Towing/Pushing | 234 | not built |  |  | Pass 62 | A prohibition (no towing or pushing of boats); nothing to prohibit yet. | audit |
| E5.34 | Untrained | 234 | not built |  |  | Pass 62 | By SSR only. No Untrained paddling dr (6 no move, 5 half MP). | audit |
| E5.4 | Fire From Boat | 234 | not built |  |  | Pass 62 | No fire from a boat (Small Arms and LMG only, halved or quartered, no Prep or Opportunity Fire afloat). | audit |
| E5.5 | Non-Ordnance & Area Target Type Attacks vs Boats in the Water | 234 | not built |  |  | Pass 62 | No attack on boats afloat: halved FP on the Vehicle line, sink below the Kill Number, Casualty Reduction at it. | audit |
| E5.51 | Non-Ordnance & Area Target Type Attacks vs Landed/Beached Boats | 234 | not built |  |  | Pass 62 | No attack on landed or Beached boats on the Vehicle line with full FP. | audit |
| E5.52 | Ordnance Fire vs Boats | 234 | not built |  |  | Pass 62 | No ordnance fire at boats (HD target, Case J +2, sinks on a hit, no HEAT or ATR). | audit |
| E5.53 | Sinking | 234 | not built |  |  | Pass 62 | No sinking: Passengers lost, or attacked with the same DR and Hazardous Movement if Beached or in shallow water. | audit |
| E5.531 | Water Line | 234 | not built |  |  | Pass 62 | No Water Line position for survivors of a sunk Beached boat. | audit |
| E5.532 | Shallow Water | 234 | not built |  |  | Pass 62 | No Fording Infantry from a boat sunk in shallow water; fording (B21.41) is not built either. | audit |
| E5.54 | Broken/Berserk Units | 234 | not built |  |  | Pass 62 | No bar on broken or berserk units boarding; no MC or TC immunity for Passengers; no Sniper change. | audit |
| E5.6 | CC | 234 | not built |  |  | Pass 62 | No CC for boat Passengers (+2 attacking, -2 defending, no Melee hold, capture rules). | audit |

#### E6 Swimming (page 235)

9 rows: 8 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E6 | Swimming | 235 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E6.1 | Water Entry | 235 | not built |  |  | Pass 62 | No swimming TC, whole-MF water entry, jump MC from a cliff, bridge, or upper level. Water entry itself is refused generically as an unreviewed terrain. | audit |
| E6.2 | Swimming | 235 | not built |  |  | Pass 62 | No swimmer movement, APh exit to land, boarding of boats, drift, or unarmed (1) CC. | audit |
| E6.21 | Drowning | 235 | not built |  |  | Pass 62 | No Drowning DR at the end of the APh (12, 11, 10 by current). | audit |
| E6.3 | TEM | 235 | not built |  |  | Pass 62 | No swimmer target rules (no TEM, no FFMO or FFNAM, halved non-HE fire). | audit |
| E6.4 | Portage | 235 | not built |  |  | Pass 62 | No exchange of a swimming MMC for an Unarmed unit; the catalog has no Unarmed counters. | audit |
| E6.41 | Rafting | 235 | not built |  |  | Pass 62 | By SSR only. No rafting (colored dr under ELR keeps the Small Arms). | audit |
| E6.5 | Cavalry | 235 | not built |  |  | Pass 62 | No Cavalry in the catalog or the planner, so no Cavalry swimming. | audit |
| E6.6 | Fording Lines | 235 | not built |  |  | Pass 62 | By SSR only. No fording lines; fording is not built. | audit |

#### E7 Air Support (pages 235 to 239)

34 rows: 32 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E7 | Air Support | 235 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E7.1 | DYO | 235 | not applicable |  |  |  | Pure cross-reference to H1.531 (the Air Support Availability Table on page 253). | audit |
| E7.2 | Arrival | 235 | not built |  |  | Pass 40 | No Air Support: no arrival dr in the RPh under the Game Turn Number, no bar at night, Overcast, or Fog. | audit |
| E7.21 | Entry | 236 | not built |  |  | Pass 40 | No aircraft count dr, bomb dr against the table exponent, free placement, or exit. | audit |
| E7.22 | Aerial Combat | 236 | not built |  |  | Pass 40 | No Aerial Combat (Dogfights under a CC marker, sequential, Aerial Melee). | audit |
| E7.221 | Dogfight Resolution | 236 | not built |  |  | Pass 40 | No Dogfight resolution (4 or less eliminates, 5 Damages, the six DRM, the Stuka rear gunner on an Original 11). | audit |
| E7.222 | ROF | 236 | not built |  |  | Pass 40 | No Aerial Combat ROF (1 with bombs, 2 without). | audit |
| E7.223 | Malfunction | 236 | not built |  |  | Pass 40 | No MG malfunction on an Original 12 in Aerial Combat. | audit |
| E7.224 | Recall | 236 | not built |  |  | Pass 40 | No Recall option after a Final DR of 10 or more. | audit |
| E7.225 | Jettison | 236 | not built |  |  | Pass 40 | No jettison of bombs in the MPh. | audit |
| E7.226 | Damage | 236 | not built |  |  | Pass 40 | No Damaged aircraft state (Recalled unless in Melee; second Damage eliminates). | audit |
| E7.23 | Elimination & Victory Points | 236 | not built |  |  | Pass 40 | No aircraft elimination or its 2 Casualty VP; no bar on Exit VP. | audit |
| E7.24 | Recall | 236 | not built |  |  | Pass 40 | No aircraft Recall at the end of the phase. Vehicle Recall (GamePlanner.Recall.cs) is a different rule. | audit |
| E7.25 | Aerial LOS | 236 | not built |  |  | Pass 40 | No Aerial LOS: one Blind Hex behind a full-level obstacle, sight INTO Depressions, LV at Aerial Range. LosCalculator has no aerial viewer. | audit |
| E7.3 | Sighting TC | 237 | not built |  |  | Pass 40 | No Sighting TC (Morale 8, the eleven DRM of the table on page 253). | audit |
| E7.31 | Recall | 237 | not built |  |  | Pass 40 | No Recall on an Original 12 Sighting TC. | audit |
| E7.32 | Mistaken Attack | 237 | not built |  |  | Pass 40 | No Mistaken Attack on a Final 12 or more, run by the opponent. | audit |
| E7.4 | Ground Support | 237 | not built |  |  | Pass 40 | No Ground Support in the opponent's MPh or the plane's DFPh; needs the To Hit Table's Aerial DRM (C6), also absent. | audit |
| E7.401 | Strafing | 237 | not built |  |  | Pass 40 | No Strafing Run (four hexes out along a Hex Grain, one attack per hex traversed, Light AA taken in each). | audit |
| E7.402 | Point Attacks | 238 | not built |  |  | Pass 40 | No FB Point Attack (second attack at three hexes on the 0-6 column). | audit |
| E7.403 | Stuka | 238 | not built |  |  | Pass 40 | No Stuka Point Attack (adjacent hex, automatic pin, bomb from the target hex, three more hexes of flyover). | audit |
| E7.41 | MG Armament | 238 | not built |  |  | Pass 40 | No aircraft MG (6, 8, 12 FP by year; To Hit and the 39F, 42F, 44F To Kill columns against armor; Original 12 disables). | audit |
| E7.42 | Bombs | 238 | not built |  |  | Pass 40 | No bombs (HE Equivalency on the counter, one use, To Hit on the Infantry or Vehicle Target Type, ends the run). | audit |
| E7.421 | vs AFV | 238 | not built |  |  | Pass 40 | No Direct Hit or Near Miss against an AFV's Aerial AF. | audit |
| E7.422 | Area Target Type | 238 | not built |  |  | Pass 40 | No bomb by the Area Target Type (half FP, C1.55 DRM against AFV). | audit |
| E7.43 | Target Status | 238 | not built |  |  | Pass 40 | No interleaving of Sighting TC with the mover's MF or MP; no attack on moving and non-moving units alike. | audit |
| E7.5 | AA Fire | 239 | not built |  |  | Pass 40 | No AA mode or AA counter, no ROF loss on changing mode, no bar from buildings or pillboxes. Vocabulary has gun type aa and an aamg value only. | audit |
| E7.51 | Light AA | 239 | not built |  |  | Pass 40 | No Light AA fire (IFE Guns, Infantry HMG, vehicular AAMG) as Defensive First Fire at an attacking aircraft. | audit |
| E7.511 | Resolution | 239 | not built |  |  | Pass 40 | No Light AA resolution on the Vehicle line with the counter's Aerial DRM (kill, Damage, evade). | audit |
| E7.512 | Unlikely Kill | 239 | not built |  |  | Pass 40 | No Unlikely Kill dr on an Original 2. | audit |
| E7.52 | Heavy AA | 239 | not built |  |  | Pass 40 | No Heavy AA (AA Gun without IFE; Original 2, 3, 4 To Hit; Random Selection of the aircraft; CA spins by the white dr). | audit |
| E7.6 | Aerial Observation | 239 | not built |  |  | Pass 40 | No Observation Plane; needs OBA (C1), which is not built. | audit |
| E7.61 | Sighting | 239 | not built |  |  | Pass 40 | No Observation Plane as Offboard Observer with a Sighting TC before Battery Access. | audit |
| E7.62 | Mistaken Attack | 239 | not built |  |  | Pass 40 | No Mistaken Attack FFE by the opponent. | audit |

#### E8 Gliders (pages 241 to 242)

17 rows: 15 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E8 | Gliders | 241 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E8.1 | Usage | 241 | not built |  |  | Pass 62 | No gliders: weather gate, capacity by nationality (14, 19, 29 PP), contents hidden until the AFPh, Wind Direction set. | audit |
| E8.11 | DYO | 241 | not applicable |  |  |  | Pure cross-reference to H1.48. | audit |
| E8.12 | Passenger Status | 241 | not built |  |  | Pass 62 | No glider Passenger immunity to PTC, Pin, and Heat of Battle. | audit |
| E8.2 | Avenue of Approach | 241 | not built |  |  | Pass 62 | No placement in the Intended Landing Hex facing the Wind Direction, nor the five-hex Avenue of Approach. | audit |
| E8.21 | Defensive First Fire | 241 | not built |  |  | Pass 62 | No Light AA at gliders (Damage at the Kill Number, Evasive Action at it or one over). | audit |
| E8.211 | Evasive Action | 241 | not built |  |  | Pass 62 | No Evasive Action by Random Location DR, once per ILH. | audit |
| E8.22 | Landing | 241 | not built |  |  | Pass 62 | No Landing DR (colored dr 1 or less after the Avenue drm; white dr gives long or short). | audit |
| E8.221 | Offboard Landing | 241 | not built |  |  | Pass 62 | No offboard approach or landing by reversed edge terrain. | audit |
| E8.23 | Crash dr | 241 | not built |  |  | Pass 62 | No Crash dr of 6 or less with the terrain drm table (page 253). | audit |
| E8.231 | Hexside drm | 241 | not built |  |  | Pass 62 | No hexside drm limited to the hexside crossed on entry. | audit |
| E8.232 | (none) | 241 | not built |  |  | Pass 62 | No elimination in a Blaze or deep water, no minefield attack as a truck. A rule head on the page though absent from the PDF outline. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E8.24 | Crash Effect | 241 | not built |  |  | Pass 62 | No crash effect (7 Damaged; 8 or more eliminated, truck wreck). | audit |
| E8.3 | Final Fire & Glider Cover | 241 | not built |  |  | Pass 62 | No Final Fire at a landed glider as a stopped truck of size 0 and cs 7; no glider Hindrance. | audit |
| E8.4 | AFPh/CCPh | 242 | not built |  |  | Pass 62 | No unloading in the AFPh, nor later unloading of a vehicle or Gun by US Vehicle Note 51. | audit |
| E8.41 | Damage | 242 | not built |  |  | Pass 62 | No glider Damage effects (malfunctioned SW, Bogged vehicle, Casualty Reduction and NMC). | audit |
| E8.5 | Re-Entry | 242 | not built |  |  | Pass 62 | No re-entry of an offboard glider's contents (as E9.41). | audit |

#### E9 Paratroop Landings (pages 242 to 244)

16 rows: 15 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E9 | Paratroop Landings | 242 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E9.1 | Air Drop | 242 | not built |  |  | Pass 62 | No air drop: weather gate, forfeited capabilities until the APh, parachute counters. | audit |
| E9.11 | Wings & Sticks | 242 | not built |  |  | Pass 62 | No Wings of five Sticks; a Stick is a squad, an SMC, and one SW on a half-inch parachute. The Cloaking Box is table procedure. | audit |
| E9.12 | Drop Point | 242 | not built |  |  | Pass 62 | No secret Drop Point per Wing, the dr of 4 to 6 that moves it by the Drift placement of E3.75, or the five-hex line. | audit |
| E9.2 | Drift | 243 | not built |  |  | Pass 62 | No Drift DR per parachute (German white dr halved, Russian up half) nor the wind shift of 2, 3, or 4 hexes. | audit |
| E9.3 | Defensive Fire | 243 | not built |  |  | Pass 62 | No Defensive First Fire at Aerial paratroops with the Hazardous Movement DRM and one parachute per attack. | audit |
| E9.31 | LOS | 243 | not built |  |  | Pass 62 | No LOS to Aerial paratroops (blind only behind an adjacent full-level obstacle, in a pillbox, beyond NVR, or at Hindrance 6). | audit |
| E9.32 | Eligible Firers & Snipers | 243 | not built |  |  | Pass 62 | No limit to Small Arms and Light AA; no bar on To Hit, Sniper, Fire Lane; no delay of the paratroop Sniper. | audit |
| E9.33 | Resolution | 243 | not built |  |  | Pass 62 | No Stick morale of 7 with one DR for all contents and Random Selection of losses. | audit |
| E9.4 | Landing | 243 | not built |  |  | Pass 62 | No one-hex move before landing, bridge landing dr, or elimination in Blaze or water. | audit |
| E9.41 | Offboard Landing | 243 | not built |  |  | Pass 62 | No offboard landing with one hex of offboard movement per MPh. | audit |
| E9.42 | Injuries | 243 | not built |  |  | Pass 62 | No landing NMC in the listed terrain, NTC elsewhere with Deployment on failure, or the wind increase of the check. | audit |
| E9.43 | Final Fire | 243 | not built |  |  | Pass 62 | No Final Fire at landed paratroops with forced TPBF and a CC counter. | audit |
| E9.5 | AFPh/RtPh | 244 | not built |  |  | Pass 62 | No bar on attack and rout in the landing Player Turn. | audit |
| E9.6 | APh | 244 | not built |  |  | Pass 62 | No removal of parachutes in the APh in place of an advance. | audit |
| E9.7 | Pre-1942 German Paradrops | 244 | not built |  |  | Pass 62 | No partially-armed pre-1942 German paratroops (2-2-8 and 1-2-8 values, arms canister dr). | audit |

#### E10 Ammo Vehicles (page 244)

11 rows: 10 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E10 | Ammo Vehicles | 244 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E10.1 | Ammo Vehicles | 244 | not built |  |  | Pass 55 | By SSR only. No circled B# (D3.71 Low Ammo) on any catalog vehicle and no Ammo Vehicle. | audit |
| E10.11 | Selection | 244 | not built |  |  | Pass 55 | No selection of the Ammo Vehicle (15 PP capacity, or 20 PP for 100mm and over). | audit |
| E10.12 | Ammo Portage | 244 | not built |  |  | Pass 55 | No Ammo Supply counter that zeroes the vehicle's Passenger capacity. | audit |
| E10.2 | Benefit | 244 | not built |  |  | Pass 55 | No B12 in place of the circled B# for a CE vehicle with the Ammo Vehicle in an Accessible or the same Location. | audit |
| E10.21 | Restrictions | 244 | not built |  |  | Pass 55 | No bar on the Ammo Vehicle moving after a Prep Fire benefit, nor on the benefit in Bounding Fire. | audit |
| E10.3 | Replenishment | 244 | not built |  |  | Pass 55 | No Replenishment after 1, 2, or 3 complete Game Turns TI, nor the Ammunition DR of 12. | audit |
| E10.31 | Special Ammo | 244 | not built |  |  | Pass 55 | No Replenishment of depleted special ammunition; Depletion itself (C8.9) is built in ScenarioA1ArmorCalculator. | audit |
| E10.4 | Immobilization/Loss | 244 | not built |  |  | Pass 55 | No Immobilized Ammo Vehicle kept in play, nor the four-turn transfer of its supply. | audit |
| E10.5 | Burning Wreck | 244 | not built |  |  | Pass 55 | No explosion of a burning Ammo Vehicle as a Goliath (German Vehicle Note 93). | audit |
| E10.6 | Ammo Dump | 244 | not built |  |  | Pass 55 | By SSR or DYO. No Ammo Dump counter. | audit |

#### E11 Convoys (pages 245 to 246)

33 rows: 31 not built, 2 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E11 | Convoys | 245 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E11.1 | Composition | 245 | not built |  |  | Pass 55 | No Convoy: two or more motorized vehicles one per hex in an unbroken line, set only by the card. | audit |
| E11.2 | Movement | 245 | not built |  |  | Pass 55 | No Convoy movement by Impulse (D14.3, not built), +1 MP per hex, 1 MP road minimum, no Start after Stop, no Overrun. | audit |
| E11.21 | Gaps | 245 | not built |  |  | Pass 55 | No split into two Convoys at a Gap, nor the lead-then-rear order. | audit |
| E11.22 | Reverse | 245 | not built |  |  | Pass 55 | No bar on Reverse or on re-entering the last hex. | audit |
| E11.23 | Entering Vehicle/Wreck Location | 245 | not built |  |  | Pass 55 | No doubled (or fourfold) D2.14 cost into a vehicle or wreck Location. | audit |
| E11.24 | Motion | 245 | not built |  |  | Pass 55 | No Motion status as one unit; no bar on a DEFENDER Motion attempt. | audit |
| E11.25 | Non-Convoy Movement | 245 | not built |  |  | Pass 55 | No declared leaving of a Convoy before an Impulse. | audit |
| E11.251 | (none) | 245 | not built |  |  | Pass 55 | No three conditions for leaving (LOS to a Known enemy or attacked vehicle, radio AFV relay, Recall). A rule head absent from the PDF outline. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E11.252 | Radioless AFVF | 245 | not built |  |  | Pass 55 | No radioless AFV platoon leaving as one; needs D14.2 and D14.23. | audit |
| E11.253 | Recall | 245 | not built |  |  | Pass 55 | No forced leaving on Recall. Recall itself is built (GamePlanner.Recall.cs). | audit |
| E11.254 | New Convoy | 245 | not built |  |  | Pass 55 | No new rear Convoy made by a voluntary Gap. | audit |
| E11.255 | (none) | 245 | not built |  |  | Pass 55 | No separate MP account for the leaver; a lone vehicle loses Convoy status. A rule head absent from the PDF outline. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E11.26 | Combining | 245 | not built |  |  | Pass 55 | No combining of Convoys at the highest MP spent. A rule head absent from the PDF outline. Inventory: Not in the PDF outline; found as a rule head on the page. | audit |
| E11.3 | PP Capacity | 245 | not built |  |  | Pass 55 | No 0 PP Passenger capacity for a Convoy vehicle (kept after leaving; a towed Gun's crew excepted). | audit |
| E11.4 | Convoy Hindrance | 245 | not built |  |  | Pass 55 | No LOS Hindrance from a Convoy vehicle that would draw Case J. | audit |
| E11.5 | Column | 245 | not built |  |  | Pass 55 | No Column of Infantry, Cavalry, or Wagons that may only move and advance in file. | audit |
| E11.51 | SW | 246 | not built |  |  | Pass 55 | No dm requirement for SW in a Column. | audit |
| E11.52 | Movement | 246 | not built |  |  | Pass 55 | No Column Impulse movement at the slowest rate under continuous Hazardous Movement. | audit |
| E11.521 | Terrain | 246 | not built |  |  | Pass 55 | No bar on a Column entering woods or buildings off a road or path, or moving beneath a Fortification. | audit |
| E11.522 | Detection | 246 | not built |  |  | Pass 55 | No delayed Detection (A12.15) until the Impulse ends. | audit |
| E11.523 | Combining | 246 | not built |  |  | Pass 55 | No joining of a Column. | audit |
| E11.53 | Disbandment | 246 | not built |  |  | Pass 55 | No Disbandment when a Column unit is attacked or sees a Known enemy unit. | audit |
| E11.531 | MPh | 246 | not built |  |  | Pass 55 | No Disbandment check after each MF or MP expenditure's Defensive First Fire. | audit |
| E11.532 | Other Phases | 246 | not built |  |  | Pass 55 | No Disbandment check in the other phases. | audit |
| E11.533 | OBA/Aerial Attacks | 246 | not built |  |  | Pass 55 | No Disbandment within six hexes of an Aerial attack or HE OBA; needs E7 and C1. | audit |
| E11.534 | Mines & Wire | 246 | not built |  |  | Pass 55 | No minefield or hidden wire Disbandment; Fortification counters have no effect at all in play. | audit |
| E11.535 | Wagon | 246 | not built |  |  | Pass 55 | No wagons, so no Motion for them on Disbandment. | audit |
| E11.536 | Pin | 246 | not built |  |  | Pass 55 | No pin of a DEFENDER Column on Disbandment. | audit |
| E11.54 | Concealment | 246 | not built |  |  | Pass 55 | No Column concealment rule (never regained; all lost on Disbandment). | audit |
| E11.6 | Straying | 246 | not built |  |  | Pass 55 | No Straying (E1.53 is not built), so no lead-unit-only Straying. | audit |
| E11.7 | Partial Entry | 246 | not built |  |  | Pass 55 | No Convoy or Column status kept across a partial entry. | audit |
| E11.8 | Application | 246 | not applicable |  |  |  | Scope statement: Convoys and Columns exist only where the card's setup, entry, or Victory Conditions name them; no mechanics of its own. | audit |

#### E12 Barrage (pages 246 to 248)

22 rows: 21 not built, 1 not applicable.

| § | Title | Page | Status | Where | Test | Pass or ruling | Note | By |
|---|---|---:|---|---|---|---|---|---|
| E12 | Barrage | 246 | not applicable |  |  |  | Heading; no rule text of its own. | audit |
| E12.1 | Definition | 246 | not built |  |  | Pass 58 | No Barrage Fire Mission; needs all of C1 OBA and a Pre-Registered hex. Rocket batteries barred. | audit |
| E12.11 | Configuration | 246 | not built |  |  | Pass 58 | No one-by-nine hex Blast Area along a Hex Grain or Alternate Hex Grain with two Barrage counters. | audit |
| E12.2 | Setup | 247 | not built |  |  | Pass 58 | No secret record of the two adjacent Blast Area hexes that set the grain; parallel grains for one battery. | audit |
| E12.3 | Placement | 247 | not built |  |  | Pass 58 | No direct placement only (C1.731), only in a Barrage-capable Pre-Registered hex. | audit |
| E12.31 | Alignment | 247 | not built |  |  | Pass 58 | No fixed alignment parallel to the Pre-Registered hex after an error. | audit |
| E12.4 | Correctiong | 247 | not built |  |  | Pass 58 | No Correction limited to in or adjacent to a Barrage-capable Pre-Registered hex. | audit |
| E12.5 | Resolution | 247 | not built |  |  | Pass 58 | No resolution one IFT column to the left of the battery's Concentration column. | audit |
| E12.51 | Smoke | 247 | not built |  |  | Pass 58 | No SMOKE by Barrage. | audit |
| E12.52 | FFE LOS Hindrance | 247 | not built |  |  | Pass 58 | No FFE LOS Hindrance (C1.57). | audit |
| E12.6 | Conversion | 247 | not built |  |  | Pass 58 | No conversion between Barrage and Concentration or Harassing Fire between Missions. | audit |
| E12.7 | Creeping Barrage | 247 | not built |  |  | Pass 58 | No Creeping Barrage (Scenario Attacker only, automatic Correction, no Observer). | audit |
| E12.71 | Setup | 247 | not built |  |  | Pass 58 | No secret record of the Pre-Registered hex, Aiming Hex, Correction mode, and Lift turn. | audit |
| E12.72 | Timing | 247 | not built |  |  | Pass 58 | No special Battery Access draw and Timing dr with pre-Game Turns. | audit |
| E12.73 | PFPh Mechanics | 248 | not built |  |  | Pass 58 | No PFPh steps (place or Correct the FFE:1, resolve, exchange for FFE:2). | audit |
| E12.731 | DFPh Mechanics | 248 | not built |  |  | Pass 58 | No DFPh steps (Correct if recorded 1-2, resolve, flip to FFE:C). | audit |
| E12.732 | Adjustment | 248 | not built |  |  | Pass 58 | No re-recording of the Pre-Registered and Aiming Hex after a Turn 1 error. | audit |
| E12.74 | Correcting | 248 | not built |  |  | Pass 58 | No two-hex Correction toward the Aiming Hex with a one-hex error. | audit |
| E12.75 | Smoke/Hindrance | 248 | not built |  |  | Pass 58 | No +2 FFE Hindrance (+1 in rain or Deep Snow); no bar on SMOKE. | audit |
| E12.76 | Lifting | 248 | not built |  |  | Pass 58 | No Lift when it cannot Correct or its recorded turns are used. | audit |
| E12.77 | Observer/Radio/Access | 248 | not built |  |  | Pass 58 | No bar on Observer, Radio Contact, and Battery Access draws during a Creeping Barrage. | audit |
| E12.771 | Converting | 248 | not built |  |  | Pass 58 | By SSR only. No conversion to normal OBA after the Lift. | audit |

## 5. The PDF, its outline, and the transcription

**The PDF's outline against its pages.** The PDF wins; these are faults of the outline, not of the rules.

- 69 rules are printed on the pages and missing from the outline: A6.8, A7.351, A7.352, A7.353, A7.52, A7.531, A8.21, A11.34, A13.511, A20.24, A23.71, A25.01, A25.111, A25.13, A25.224, A25.242, A25.58, A26.221, B9.33, B16.32, B16.41, B19.21, B20.46, B23.733, B23.911, B23.912, B27.13, B28.52, B31.1411, B32.4, B32.42, B32.43, B32.44, C.5C, C1.733, C3.41, C3.51, C3.52, C3.53, C5.71, C5.72, C6.58, C7.311, C7.321, C7.331, C7.332, C8.92, C9.1, C10.41, C11.51, C12.22, C12.23, C12.24, D4.221, D4.222, D4.223, D6.651, D6.72, D6.83, D8.32, E1.17, E1.23, E2.43, E3.751, E3.752, E8.232, E11.251, E11.255, E11.26.
- 50 rules are printed one page from where the outline points: A25.3, A25.57, A25.6, A25.61, A25.62, A25.63, A25.64, A25.65, A25.66, A25.7, A26.2, B13.81, B13.82, B23.9221, B24.1, B25.1, B25.11, B26.5, B27.5, B28.1, B28.2, B28.3, B28.4, B28.41, B28.411, B28.412, B28.5, B28.51, B29.1, B29.2, B29.3, B29.4, B30.34, B30.35, B30.4, B30.41, B31.121, B33.11, B34.4, B34.41, B34.42, B34.43, B34.5, B34.6, B34.7, B35.1, B35.2, B35.3, E1.921, E1.922.
- The outline numbers B24.72 twice; the second is B24.73 (Wire) on page 142. It lists E3.313 as 3.13. It lists a B16.21 that page 130 does not print. Its "A7.30 Results" is a label over A7.301 to A7.309, not a rule.
- The outline's titles differ from the page in places: C5.64 is OVR PREVENTION (outline: Protection), C7.11 TK# DERIVATION and C7.2 MODIFIED TK# (outline: TH#), C8.4 CANISTER, C8.6 WHITE PHOSPHORUS, C13.9 SHAPED-CHARGE WEAPONS, A25.936 MMG/HMG/MTR (outline: ATR), E11.252 RADIOLESS AFV, E12.4 CORRECTING.
- Two heads are lost from the text layer and were read on the rendered page by the audits: A7.8 PIN (page 58) and D15.1 (page 214).

**The transcription (`docs/ASL/Rulebook_Markdown`) against the PDF.**

- **Page markers.** For 419 rows the transcription's `<!-- page N -->` marker is not the page the rule is printed on (A: 154, B: 88, C: 57, D: 49, E: 71), nearly always one page low: the marker stands where a page's text was placed, and a rule that starts in a page's second column is filed under the page before. A citation's page must come from the PDF, as in this document.
- **Wording and numbers.** Where the audits compared them, the transcription's rule text agreed with the PDF. Two audits compared whole sections sentence by sentence (A10 to A12, and C5 to C9) and found no difference. The others made spot checks. A25 was not compared.
- **Text lost or misplaced in the transcription:** B23.23 has lost its head and most of its rule (page 136). A7.8's opening words are missing, as they are from the PDF's text layer. D17.22 has the Aerosan Movement Table's heading spliced into a word. E1.93's head and B36.1 sit inside code blocks, where a search for a rule head misses them. Line-break hyphens survive inside words ("Infan-try"), which defeats a plain search.
- **Heads my script could not match in the transcription** (26; most are rules set in bold where it looked for a heading, and the outline's own errors): A16, A24.61, A7.30, A7.37, A7.8, B10.21, B10.211, B10.3, B10.31, B10.52, B16.21, B23.23, B31, B32, B33, B34, B35, B36, B36.1, B37, C13.311, C6.521, D11.211, D15.1, D17, E1.93.
- **Chapter D has 17 sections.** D17 Aerosans (pages 216 to 217) is in the PDF and in the transcription. The coverage document of 2026-10-05 had no row for it and counted 104 sections; there are 105.

**E.1 (page 222)** says Chapter E "is composed entirely of Optional rules and SSR": none of it applies unless a card cites it or the players agree. The rows of Chapter E are judged as rules all the same, since the aim is complete coverage of A to E.

