# ASL Dice Display and Action Outcome Analysis

Date: 2026-10-31.

Status: Reviewed and approved for implementation on 2026-10-31. This analysis records the source inspection and proposal. Implementation and Studio evidence are recorded in the [implementation design](<Designs/ASL Shared Action Outcome and Dice Integration Design.md>).

## 1. Request and recommendation

The user requested an analysis of every place the UI uses or displays die rolls, with a view to showing the dice display component at those sites. The user then expanded the request to presenting the whole action outcome more clearly and coherently.

Treat these as one presentation change: a shared action-outcome card, with `AslDice` displaying each recorded roll inside it. Adding dice pictures alone would still leave the player assembling an outcome from several separate panels.

The existing Dice Lab supplies the SVG faces, accessible labels, reduced-motion animation, and an interactive example. Its current component owns random generation and modifier entry. It needs a display-only mode before it can safely appear in game results.

This analysis covers the active ASL Map Studio UI, its shared record components, and the services that prepare their data. It is a presentation analysis, not an audit of the underlying ASL rules.

## 2. What makes the current outcome difficult to read

### Action status and game outcome are different

An action can be successfully committed while its rally or attack fails. The UI should distinguish the action's lifecycle, such as "Awaiting confirmation" or "Recorded", from its game result, such as "Rally failed; unit remains broken."

[`ProposalReviewPanel`](../LimboDancer.Domains.Asl.MapStudio/Components/Play/ProposalReviewPanel.razor) shows the action title, outcome status, reasons, consequences, and evidence. The parent [`Play`](../LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor) supplies these separately from the recorded results shown elsewhere on the page.

### Related information is separated

Play's Records panel groups rolls, ordnance, fire, close combat, night/weather, snipers, and other actions into separate lists. One action's resolution can involve several of those categories. The player must connect its dice, arithmetic, and effects across sections.

[`ReplayStepPanel`](../LimboDancer.Domains.Asl.MapStudio/Components/Replay/ReplayStepPanel.razor) uses the same record components but also separates the step's changes from its rolls and categorized results.

### Presentation varies by action

Fire has a detailed card, close combat has tables, and many other outcomes are formatted sentences. Play's latest-outcome area special-cases fire and close combat. [`PlayRecords.Latest`](../LimboDancer.Domains.Asl.MapStudio/Services/PlayRecords.cs) also limits some summaries to the first four record entries and sends the reader to Records for the remainder. These are formatted entries joined into one string, not a limit on sentences or visual lines.

These existing components contain useful detail. The proposal is to organize that detail consistently around the action that produced it.

## 3. UI inventory and proposed treatment

| UI location | Current presentation and source | Proposed treatment |
|---|---|---|
| Play: latest action | `Play.razor`, `PlayRecords.Latest`, `LatestFires`, `LatestTables`, and `LatestOthers`; summary text with special fire and close-combat layouts | One outcome card containing the complete disclosed action, with a compact collapsed summary |
| Play: Records | `DiceRollHistory`, `FireHistory`, and `ActionRecordList`; rolls and outcomes separated into category sections, with turn/phase grouping and replay links | Shared outcome cards grouped by action; preserve access to each category, turn/phase context, and replay links |
| Replay: step details | `ReplayStepPanel`; changes, rolls, and categorized records | Reuse the same outcome presentation for the recorded step and selected viewer |
| Fire results | `FireResolutionCard`, `FireArithmeticBreakdown`, `InfantryFireEffectsTable`, and `VehicleFireEffectsTable` | Compact dice beside each associated calculation and consequence |
| Close combat | `CloseCombatResultTable`; formatted dice text and separate defender results | Recorded dice per attack, followed by the existing per-defender outcomes |
| Ordnance, rally, rout, support weapons, snipers, night/weather | Mostly sentences produced by `PlayRecords` and rendered through `ActionRecordList` | Structured roll-and-result rows within the action card |
| Since you last looked | Event sentences from `PlayRecords.Since`, rendered in `Play.razor` | Compact action summaries that expand to the same outcome card |
| Game inspector | `Games.razor` and `GameText.EventText`; visible event types with limited event descriptions | Display the recorded values for visible dice events while retaining event details |
| Scripted dice queue | `ScriptedDicePanel`; a sequence of numbers | Small individual die faces in queue order, explicitly labelled as queued rather than rolled |
| Dice Lab | `DiceLab` hosting the interactive `AslDice` | Keep the full interactive roller and worked example |
| Proposal review | `ProposalReviewPanel` and `FireArithmeticPreview`; known facts and arithmetic before resolution | Use the common visual structure with an explicit pending state; show actual dice only when the existing game flow supplies them |
| Rule Workbench | `Workbench.razor`; required-roll notices and hand-entered facts | Where useful, show an explicitly unrolled display beside a required-roll notice; never invent values or infer dice from arbitrary numeric facts |
| Turn Guide | `TurnGuideEvidence`; notices that a governed action requires a roll | Preserve the notice and use a pending display only where supported by supplied roll metadata |
| Scenario setup and card editing | `CardStartSummary` and `CardTurnRecordFields`; explanatory text or a choice that first move is determined by rolling | Keep the distinction between configuring a future roll and displaying a recorded result; show actual startup dice through the action outcome once recorded |

A reference to dice in rule help or explanatory prose is not itself a recorded-roll display. The integration should distinguish actual values, queued values, pending rolls, and explanatory text.

### Nested rolls must be included

Updating only the attack's primary dice would leave important results as text. The fire presentation also contains morale/task checks, Heat of Battle, vehicle and crew checks, and passenger/rider results. Some of these retain structured values; others are flattened into strings by [`FireText`](../LimboDancer.Domains.Asl.MapStudio/Services/FireText.cs).

Likewise, a single action may have a primary roll, subsequent rolls, checks for several subjects, and related consequences. Each displayed roll needs its own purpose and association with the result it explains. Existing attempt, event, and roll identifiers should establish those associations; text matching should not.

## 4. Proposed action-outcome card

The card should answer these questions in order:

1. **What happened?** A short result headline, with the actor, target, and hexes.
2. **What changed?** Separate effects for each affected unit or weapon.
3. **How was it resolved?** Each roll's purpose, SVG dice, original value, modifiers, final value, and recorded result.
4. **What needs attention now?** An existing pending choice or required response, when supplied by the game.
5. **Why?** Expandable arithmetic, rule references, and detailed evidence.

For example, a rally card could lead with "Rally failed; unit remains broken." Its "Recorded" lifecycle status would be secondary. The dice and calculation would appear immediately below the result. A multi-target attack would have one action heading and separate outcome rows for its targets.

Use the same structure for actions without dice, omitting the roll section. Do not introduce empty dice trays for movement, declarations, or other actions that have no roll.

For a proposal whose supplied outcome is `NeedsConfirmation`, show "Awaiting confirmation", the facts currently known, and consequences requiring attention. Preserve distinct denied, stale, and failed states rather than labelling every uncommitted proposal as awaiting confirmation. Preserve the existing confirmation flow and its conditions for offering Confirm. A planned action must not appear to have resolved, and placeholder calculation values must never be drawn as actual dice.

Historical outcome cards in Replay remain read-only. They may describe a choice that was pending at the recorded revision, but must not offer a live confirmation or response action. Only the live proposal/action path supplies actionable controls.

Preserve the existing event order and action boundaries. Related follow-up actions may be linked, but should not be silently combined into a different recorded action. Keep replay navigation and the user's collapsed/expanded preference.

## 5. Changes needed in AslDice

The component needs an explicit display-only mode that:

- Accepts supplied values, purpose labels, die roles, and recorded arithmetic.
- Generates no random values when showing history, Replay, or an action outcome.
- Supports compact inline and expanded layouts.
- Supports independent dice and longer collections in addition to the current single die, paired dice, and pair with a separate subsequent die.
- Distinguishes a pair's total from a separate subsequent die or a collection used for selection.
- Shows pending or unavailable values without substituting an invented face.
- Animates only a newly resolved roll, not every rerender, history expansion, or Replay navigation.
- Preserves accessible names, text equivalents, and reduced-motion support.

The lab can keep its interactive controls around the shared display. Production result displays should receive their values from the existing game path.

### Explicit die roles and grouping

The current lab displays white first. Existing game calculations use the first stored attack die as colored; for example, `ScenarioA1FireCalculator` reads `arithmetic.Dice[0]` as the colored die. Passing the stored array directly into the lab's positional assumptions would mislabel it.

The display contract must identify roles explicitly. The adapter may arrange the visual order without changing the recorded order or values.

The number of values alone also cannot decide how to display a roll. Random Selection can be a collection of independent dice. Two independent side rolls must not automatically become one summed DR. A third value is not automatically a subsequent die. Use supplied purpose and grouping metadata; use a neutral labelled presentation where a role is unknown.

Do not depict an unsupported die shape as a six-sided die. Preserve a numeric representation when recorded dice cannot be represented accurately by the available SVG faces.

## 6. Preserve structured outcome data

Several UI models lose structure before rendering:

- [`RollRow`](../LimboDancer.Domains.Asl.MapStudio/Services/RollRow.cs) retains a roll identifier, formatted text, and provenance text, but not individual values.
- [`ActionRecordList.Entry`](../LimboDancer.Domains.Asl.MapStudio/Components/Play/ActionRecordList.razor) retains an event identifier, kind, and text.
- [`CloseCombatTableAttack`](../LimboDancer.Domains.Asl.MapStudio/Services/CloseCombatTable.cs) stores its dice as a string.
- `FireView` retains structured attack arithmetic and effects, but some additional vehicle and weapon outcomes are already lists of strings.

Extend the presentation models so dice, arithmetic, subject labels, effects, and associations remain available to components. Do not parse numbers out of finished sentences. Keep existing readable text available for summaries, accessibility, and cases that lack richer data.

Two adapters should feed the shared outcome presentation:

- **Recorded outcomes:** Extend the existing `PlayRecords` path, which already serves Play and Replay and groups records by attempt. It supplies the disclosed action summary, effect rows, recorded roll groups, and historical context.
- **Proposal outcomes:** Adapt the existing proposal state held by `Play.razor`. It supplies the lifecycle status, disclosed reasons and evidence, consequences, and existing confirmation eligibility. `PlayRecords` cannot supply pending, denied, stale, or failed proposal states from recorded history alone.

Preserve the distinction between `NeedsConfirmation`, `Committed`, `Replay` (an already-committed attempt), `Stale`, `Denied`, and `Failed`. The `Replay` proposal outcome is distinct from viewing history on the Replay page. Neither adapter decides rules or invents lifecycle transitions. Live follow-up controls remain supplied by the existing live action path; historical cards remain read-only. Final field names and component boundaries belong in the implementation design.

## 7. Disclosure and rule boundaries

Build the display from data already filtered for the selected viewer and revision. Hidden rolls, concealed identities, and undisclosed consequences must remain absent. Do not fetch a hidden roll merely because a visible record refers to its identifier.

The current fire presentation distinguishes withheld effects from an empty result. Preserve that distinction: "not disclosed" must not become "nothing happened."

Recorded arithmetic and calculator results remain authoritative. The UI must not decide success, legality, eligibility, timing, or thresholds again. It may format the supplied values and describe supplied effects. Any required change to Rules-owned metadata must follow the repository's claim process and be identified before implementation.

Workbench and Turn Guide currently provide some roll requirements without complete display metadata. The design must account for those gaps explicitly rather than deriving rules from prose or assuming every number is a die value.

## 8. Memory and performance considerations

- Reuse presentation data for the same game view and revision through the existing record reader.
- Preserve the current latest-six-roll bound for the compact roll history.
- Avoid repeated full-history scans for every outcome card or die face. Resolve event and roll associations once per presentation snapshot.
- Keep SVG faces small and keyed by stable roll identity. Expanding history must not generate new rolls or restart animations for old results.
- Keep state and caches scoped to the view/revision or component that owns them. Do not retain an unbounded cross-game outcome cache.
- Leave map and counter rendering paths unchanged. No per-hex or per-unit rendering work is required to draw a roll.
- Clear or replace the outcome presentation during handover so the previous viewer's information cannot remain visible.

No performance improvement is claimed by this analysis. The implementation should check that richer outcome cards do not make ordinary Play steps or long histories noticeably less responsive.

## 9. Proposed implementation sequence

1. Make `AslDice` suitable for supplied, recorded values and explicit die roles. Keep the Dice Lab working through the same display.
2. Introduce the shared outcome presentation with separate recorded-outcome and proposal adapters, then connect Play's proposal review, latest result, Records, and Replay. Preserve proposal statuses and confirmation eligibility, and keep historical cards read-only. Cover the nested rolls and multi-target effects as part of this stage.
3. Cover the Game inspector, scripted queue, and supported pending-roll displays. Reconcile the generated Icon Lab control inventory after any control changes.

Start implementation from current main on a new branch. Recheck shared-file changes and applicable claims before editing the large Play and record-reader files.

## 10. Verification and acceptance

The user's instruction is to prioritize Studio verification and avoid unnecessary tests.

Use representative Studio examples to verify:

- Single rolls, paired rolls, subsequent rolls, and independent collections.
- Correct colored/white mapping from recorded values.
- Positive and negative modifiers without recalculating recorded outcomes.
- Multi-roll actions and actions affecting several targets.
- A committed action with an unsuccessful game result.
- Actions without dice and proposals that have not rolled yet.
- Distinct awaiting-confirmation, denied, stale, failed, committed, and already-committed proposal states, with controls offered only through the existing live flow.
- Both player perspectives, adjudicator view, and handover, including withheld effects.
- The same action in latest outcome, Records, and Replay.
- Historical cards remaining read-only, including when the recorded revision contains a pending choice.
- Keyboard access, narrow layouts, reduced motion, and readable collapsed summaries.
- Scripted queue values remaining distinct from recorded rolls.
- Stable recorded events and no extra dice consumption from rendering or navigation.

Run only focused checks justified by changed behavior or an actual failure. The generated icon inventory must match the new controls, as the Dice Lab integration demonstrated. The repository's required CI and merge process still applies.

The next step is approval of an implementation design based on this analysis. No dice integration or outcome-layout changes are authorized by saving this document alone.
