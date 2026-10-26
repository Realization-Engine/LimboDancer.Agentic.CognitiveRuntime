# Frame agent D: 71.2 (wounds, Field Promotions, the SMC crew's Stun wound) and the code map

(The agent's closing report, kept as it came, 2026-10-26 about 08:3x.) Paths relative to `src/ASL/`; Rules/, Play/, Units/ are `LimboDancer.Domains.Asl.{Rules,Play,Units}/`, MS/ MapStudio. Read: p. 85 (A17, A18), pp. 203-204 (D5.34 to D5.343), p. 48 (A4.11); whole rulebook searched for "wounded", "4 MF", "G.M.D.".

## 1. A17.2
**(a) Pin after more than 3 MF.** `UnitInstance.MfSpent`, `HalfMfSpent` (Units/State/GameTypes.cs:482, :497), reset at every phase change (GameProjector.cs:668-670). The moving stack `state.Movement` (GameProjector.cs:2293). Wounds land in the MPh at: Fire (DFF, FPF, Residual FP) `ScenarioA1FireCalculator.Wound` (Rules/ScenarioA1FireCalculator.cs:4988), `TargetState.Wound` (:5226); Sniper `SnipeUnit` (Play/GamePlanner.Snipers.cs:249-260) with `ScenarioA1Sniper.WoundedConditions()` (:181); Searching casualties (GamePlanner.Searching.cs:206); Mopping Up casualties (GamePlanner.MoppingUp.cs:204). Nothing pins a wounded man today.

Proposal: `ScenarioA1Wounds.PinnedByWound(bool mph, int mfSpent, bool halfMfSpent)`, true past 3 MF (3 1/2 counts). Fire through an absent-safe fact on `FireTarget` (Rules/ScenarioA1FireModels.cs:1176; built in LiveFire.cs about :881), JsonIgnore when null; Sniper, Searching, Mopping Up: the planner writes Pinned with the wound. MPh only; the RtPh's Interdiction wound to the backlog. 1:00.

**(b) Carrying a wounded man, 5 PP.** Nothing exists. Portage counts only `HoldingRole.Possessed` SW (Play/GamePlanner.Movement.cs:42-57); Rules `MfAllotment` (Rules/ScenarioA1MovementCalculator.cs:18-41), `Laden` (:44); `ContainmentRole` (GameTypes.cs:360) has no carried role. Nearest model: prisoners (`MovePrisoners`, GameProjector.cs:3296; `KeepGuards` :4888). Smallest shape: an optional `carried` argument on `asl.game.move`, recorded on the step, absent-safe; a Rules bar (both Good Order, the man wounded with no SW, the carrier a MMC); +5 PP through `Portage()`; the projector moves him with the carrier; `StepMoverFacts` (GameProjector.cs:2295-2302) accepts a carried mover. Small value in play. 2:30. If the pass runs long, move it to pass 72.

**(c) 4 MF on a conveyance.** Built: `ScenarioA1PassengerCalculator.ConveyanceAllowance = 4` (Rules/ScenarioA1PassengerCalculator.cs:58-62); boarding GamePlanner.Passengers.cs:338-339; after unloading `ConveyedAllowance` Movement.cs:341, :397 (passes 40, 65, R65.15). A4.11 (p. 48). Documents (0:05). Other conveyances (cavalry 165, motorcycles and wagons 175, boats E5.21 p. 233) get backlog rows. The wounded leader's 2 MF bonus is built (`LeaderBonus`, ScenarioA1MovementCalculator.cs:54-71).

## 2. A18.2's remainder
- "-1 CC vs AFV": missing. `ScenarioA1VehicleCloseCombat` calls `ScenarioA1FieldPromotion.Create(..., [], ...)` with no drm (Rules/ScenarioA1VehicleCloseCombat.cs:393); its R70.34 comment reads the cell as "no odds column against a vehicle". Read as two causes: -1 vs an AFV, and -1 per odds column below 1-1 (built, ScenarioA1CloseCombatCalculator.cs:702). Proposal: a fact `AfvDrm` (bool?, JsonIgnore null) written beside `LeaderCreation = true` (Play/GamePlanner.VehicleCloseCombat.cs:601); Rules adds `cc-vs-afv` -1 only when true and `definition.Unarmored != true`. Pass 70's records replay unchanged; `BacklogPass70Tests.ASquadsOriginalTwoAgainstATankCreatesALeaderWhoJoinsTheAttack` may change. 0:30.
- G.M.D. (Chinese, p. 109, p. 253): no catalog has them; R27.1's refusal stands; documents and a backlog row (0:05).

## 3. The SMC crew's Stun wound (Backlog.md:1366)
D5.343 (p. 204): a SMC Temporary crew "becomes Wounded if he suffers a Stun result", a Recall eliminates him. A17.11 (p. 85): Severity dr, +1 if already wounded. Today the projector wounds him with no dr and a second wound eliminates (`SmcCrewAfter`, Units/State/GameProjector.cs:4343-4380; `ScenarioA1CapturedVehicleRules.SmcCrewOutcome(..., alreadyWounded)`, Rules/ScenarioA1CapturedVehicleRules.cs:187-192). Only Fire's `VehicleConditionEvents` (Play/GamePlanner.Fire.cs:1298-1316) writes a vehicle Stun; 71.1's Sniper Stun a second writer. Proposal: the planner rolls `wound-severity` (`ScenarioA1Wounds.SeverityPurpose`), id `{attempt}-wound-{crew}`, before the vehicle's `conditions-changed` (crew by `ObjectIndex.CrewOf`); `SmcCrewAfter` reads that dr from the projector's `rolls` (GameProjector.cs:67) and passes `SmcCrewOutcome(..., int? severityDr)`: a wound or mortal elimination (`ScenarioA1Wounds.Mortal`); no dr, today's behaviour. Recall unchanged. `PlayRecords` names the man of a severity dr from the event after it (MS/Services/PlayRecords.cs:1197-1203); here take him from the roll id. 1:15.

## 4. Estimates for 71.2
| Part | Build |
|---|---|
| Pin after more than 3 MF | 1:00 |
| Carrying a wounded man | 2:30 |
| 4 MF on a conveyance (documents) | 0:05 |
| A18.2 -1 vs an AFV | 0:30 |
| G.M.D. and unlisted (documents) | 0:05 |
| SMC crew's Stun wound | 1:15 |
| Total | 5:25 (plan 0:20) |

Move out with rows: the RtPh wound pin; G.M.D.; other conveyances (165, 175, water); optionally the carry to 72.

## 5. Test helpers
- Rules: `ScenarioA1Pass70RulesTests.cs` helpers `Unit` (:181), `Rolls` (:189), `Dice` (:196), `Resolved` (:198); SMC crew models at `ScenarioA1Pass65RulesTests.cs:1201`, `:1760`.
- Play Sniper model: `BacklogPass15Tests` `Sniper(id, hex, side)` (:142, a setup placement `asl:sniper` with no definition), `Sides(germanSan, russianSan)` (:183, `sides[].san`), `SetupAt(advances, firstSide, ssrs, sides, placements)` (:160, catalog 1.13.0), `Then(fallback, values...)` (:85); model test `ADrEqualToTheEnemySanCallsForASniperAttack` (:376: IFT DR, Sniper dr, Random Location DR). `BacklogPass15TablePlayerTests` the same (:148, :189, :91).
- Pass 70 Play file: `BacklogPass70Tests.SetupAt` (:227) fixes sides at :235-239 with no `san`; `Once(values)` (:141) throws when the dice run out; `Do` (:264), `Since` (:157), `History` (:153).
- MapStudio: `PlayPagePass70Tests` `Start` (:84, no SAN), `Do` (:67), `Open(view)` (:106), `dice.Enqueue([...])` (:22); record lines via `new PlayRecords(live.History(Game)!, view, long.MaxValue).Since(0)` (:157).
- Test games: `code-builders/test-games/game_file.py` `CATALOG` 1.18.0 (:29), `GameFile` (:60), `unit` (:95), `weapon` (:100), `vehicle` (:106), `gun` (:112), `phases` (:120), `write` (:125); no SAN or Sniper helper; sides from `game_started_template.json` (german ELR 3, russian ELR 2, no san). Set `started["sides"][i]["san"]` and add `instance-created` `{"instance": {"id", "kind": "asl:sniper", "side", "position": {"at"}, "conditions": {}}}` (not verified that it accepts a Sniper with no definition).
- Scripts: `run_test_classes.sh <Rules|Play|Units|MapStudio> <filter>` (builds into `$WORK/test-classes-bin`, registers it); `build_studio.sh`.

## 6. The Play page
- Sniper records: `ActionRecordList` "Snipers", `#play-snipers` (MS/Components/Pages/Play.razor:376), text from `PlayRecords.SniperRecords` (PlayRecords.cs:1629-1637): "The {side} Sniper ({id}) attacks after the SAN roll ...: dr N, at {BoardLocation}: {unit} is {result} (A14.3)": raw Location, unit with no hex: breaks the hex UI rule; 71.1 should fix it.
- Replay step "A Sniper attacks in {place}" (MS/Services/ReplaySteps.cs:515). Latest `#play-latest` in `#play-latest-panel`, `#play-latest-toggle` (Play.razor:1288-1315).
- Choice panel `PendingChoicePanel` (MS/Components/Play/PendingChoicePanel.razor:3): `<div id="play-choice" data-key>` with `button.choose[data-option]` "Propose: ..." (:9), at Play.razor:464; option texts `ChoiceOptionText` (:5093), choose arguments :5079; then `#play-confirm`, `#play-outcome`.
- Setup `SetupPlacementEditor`: `#place-definition`, `#place-id`, `#place-group`, `#place-concealed`, `#place-hidden`, `#place-add`; the Sniper choice `sniper:{side}` (Play.razor:1711-1719, `SniperPrefix` :3754, added :7231-7237 as `asl:sniper`).

## 7. Registries
- Actions: `GameActions.All` GameActions.cs:844; dispatch GamePlanner.cs:351-375; `PlayTests.TheActionsAreRegisteredWritesThatNeedConfirmation` PlayTests.cs:442.
- A new event: the record (GameEvent.cs, ChoiceRecords.cs where `SniperAttacked` is :83, or LiveRecords.cs); `GameEventReader` (search :749, sniper :992, conditions :248); `GameEventWriter` (switch :82, sniper :1195, conditions :280); the projector switch (GameProjector.cs:107; `Snipe` :4871); `PlayRecords`, `ReplaySteps` (:714); Play.razor's latest text (:9403); `GameGate` (:146). No separate event verifier; the projector's `Verify*` calls into Rules.
- `ConditionName`: projector :730, planner Fire.cs:1411.
- A new choice kind: `ChoicePending` constants (ChoiceRecords.cs:36-50), `ChoiceKindOf` (GamePlanner.Choices.cs:71), `DescribeChoice` (:81), the resume in `PlanChoose`, `ChoiceOptionText` in Play.razor.
- Icons: regenerate `wwwroot/icons/icon-coverage.json` with `ASL_ICONS_UPDATE_COVERAGE=1` (IconCoverageTests.cs:20).
- Memo: `MemoOf` :20, `PlayStepMemo` :23, `ObjectIndex` :196, `Of` :223, `CrewOf` :329, `Aboard` :335/:338, `At` :345, `OccupiedIn` :351.
- `Snipe` replays the whole game up to four times an attack (Snipers.cs:23, :78, :209, :219) and scans `state.Units` with `state.Location` per unit (:123-142): through `ObjectIndex`.

## Not read
`GameView`'s filtering of Sniper records; the Rider path of `ConveyanceAllowance`; whether eliminating a SMC Temporary crew clears `TemporaryCrew` outside `SmcCrewAfter`; the PDF render of A18.2's drm cell; the water pass number.

Slip: two Grep calls with a `!boards/**` glob listed file names and matching lines under `src/ASL/boards` (`"type": "choice-made"`, one game file name); nothing else read there.
