# ASL Play Action Interface Analysis

Status: reviewed analysis; implementation authorized by the user on 2026-10-31.

Date: 2026-10-31. Source baseline: `96fcf297` (PR 90 merged).

## 1. Purpose and scope

The user wants the Play tab's phase-specific actions to retain a free-flow layout while becoming easier to understand and operate. The supplied Fire screenshot is an example of a wider problem, not a request to redesign only Fire. This analysis covers the shared presentation pattern, location entry, map picking, optional choices, and proposal readiness. The implementation design must account for other action families without forcing them into the same fields or adding a wizard.

The previous UI work is the baseline: phase-end action beneath the turn heading, view selector beside the card link, Evidence within Selection, and a separate current-phase-guide tab. Those decisions remain in place. Routine actions retain their existing confirmation policy; phase endings require confirmation. This work does not change game rules, event formats, action eligibility, disclosure, or replay results.

## 2. Evidence and limits

Paths below are relative to `src/ASL/LimboDancer.Domains.Asl.MapStudio/`.

| Source | Finding |
|---|---|
| `Components/Play/SmallArmsFirePanel.razor` | Origin, extra origins, firers, weapons, director, target methods, range, optional fire choices, and proposal controls largely share one `.toolbar`. Optional fields appear as the draft develops. |
| `wwwroot/site.css`, `.toolbar` | A wrapping flex row with center alignment and uniform gaps determines placement, rather than action semantics. |
| `Components/Play/LocationField.razor` | The input, available levels, map-picking toggle, use-picked action, and route edits form a vertical inline-flex group. Its height differs from ordinary select labels. |
| `Components/Play/PickedOption.razor` | A select can accept matching locations from an already selected map hex through separate Use buttons. This is different from arming a field to consume the next map click. |
| `Components/Pages/Play.razor`, `FireTarget` and Fire callbacks | `fireFreeTarget` takes precedence when nonempty; changing the target dropdown does not clear it. Two populated inputs can disagree about the effective target. |
| `Services/LocationPicking.cs` | Picking is page-local, has one armed field, and distinguishes new clicks by a serial. Picked hexes initially represent ground-level locations. |
| `Services/DisplayText.cs`, `ReadPlace` | Typed input is normalized using existing board/location conventions. An unqualified hex is not resolved to an arbitrary board when multiple boards exist. Unrecognized input may remain text. |
| `Components/Play/ActionSection.razor` | Closing a section hides its children but keeps them mounted. Disposal alone cannot cancel picking in every hidden-field case. |
| Rally, OrdnanceFire, InfantryMovement, Advance, Rout, and CloseCombat components | Actions differ materially: short forms, optional equipment, routes, grouped obligations, alternate targeting, and multi-party declarations all need different arrangements. |

This is a source review plus the user's Fire screenshot and the preceding Studio observations. It is not a claim that every action, phase, viewport, or uncommon option has been visually audited. Existing rule citations in components were not re-adjudicated. The recommendations concern presentation and draft state, not new interpretations of the rulebook.

## 3. What makes the Fire form difficult

1. **The grouping is accidental.** From and Director happen to share a row. Selected units and weapons can later insert themselves between controls, changing the reading order and rhythm.
2. **One target appears to be two decisions.** The dropdown and Or any Location field look like independent inputs. The current precedence rule is invisible to the player.
3. **Map picking has too little prominence.** The links are real buttons styled as links. Repeated generic wording makes it harder to tell whether the next click chooses the primary target or the sprayed location.
4. **Optional inputs resemble required inputs.** Spray also occupies space before the player has even selected the primary attack. Other options add more visual competition as they become available.
5. **The proposal has no stable place.** It wraps beside optional inputs and Clear. A disabled button alone does not explain the missing information or distinguish an incomplete draft from a refused action.
6. **Explanation precedes orientation.** A large paragraph competes with the first useful choice. The full rule disclosure already exists; the visible introduction can be shorter without deleting access to the existing explanation.

The two-column-looking screenshot is not an intentional two-column form. It is the result of differently sized controls wrapping within one row.

## 4. Recommended visual structure

Use four semantic groups, with whitespace and small legends rather than a box around every field:

| Group | Question answered | Fire example |
|---|---|---|
| Acting units | Who acts, and with what? | Origin, selected units and weapons, director, extra origins |
| Target or destination | Where is the action directed? | One effective target, map picking, level, any target-specific choice |
| Additional choices | Which relevant variations are included? | Spraying fire and other options already offered by the host |
| Proposal | What will be proposed, and what is missing? | Short draft summary, readiness/refusal text, Propose: fire, Clear |

These are a reading order, not mandatory four-step navigation. Short forms may combine groups or omit empty ones. Related fields can share a line when the inspector is wide enough; complete groups stack when it is narrow. Labels should remain adjacent to their controls, preferably above variable-width selectors. The width of the inspector, not the width of the whole browser, governs wrapping.

A conceptual Fire arrangement:

```text
Fire                                      Rules >
Choose the firing group and its target.

Firing group
From [Choose location]       Director [None] optional
Units and weapons, grouped by their location

Target
One selected location       [Pick target on map]
Choose a suggested location or enter another location
Level selector when relevant

+ Add spraying fire

Draft summary or missing-input explanation
[Propose: fire]   Clear
```

The sketch does not prescribe a custom searchable combobox. Native selection and explicit manual-entry controls can achieve the interaction more reliably, provided they operate on one effective value and clearly expose the active entry method.

## 5. Location and map-picking requirements

- **A1: One effective value.** List selection, typing, and map picking are alternate entry methods for one location. A later explicit selection replaces the earlier value. No hidden field may silently override it.
- **A2: Clear ownership.** Use purpose-specific button text, such as Pick target on map, Pick destination, or Pick spray location. Keep each button inside its location group.
- **A3: Visible picking mode.** The field and the map instruction must identify what the next click does. Provide Cancel picking. Keyboard users must have equivalent typed/list access; Escape cancellation is a proposed enhancement, not a claim about current behavior.
- **A4: Preserve full location identity.** Keep board and level visible when needed. A ground-level map pick is not automatically the intended building level. Multiple candidate levels require an explicit selection; multiple boards must not be guessed.
- **A5: Preserve different selection policies.** A free target field and a destination restricted to supplied choices are not interchangeable. Free entry remains available only where the host currently offers it. In a restricted field, a map click cannot bypass disabled or unavailable supplied choices. A disabled suggestion is not a new global blacklist for a field that already supports arbitrary entry; the normal proposal still checks that entered target.
- **A6: Safe lifecycle.** Close, tab change, view handover, game/phase change, and removal of an optional field must cancel its active picking mode. Hiding a field normally preserves its draft; explicitly removing an option clears that option's submitted values.
- **A7: No hidden-information expansion.** Suggestions, summaries, and reasons use the existing viewer-scoped inputs. Picking a hex does not establish LOS, eligibility, or permission to reveal units there.

## 6. Optional choices and proposal feedback

Use named, inline disclosures for unused optional choices. Add spraying fire reveals its second location field and map control. Once configured, the option remains visibly represented, including its current value and Remove action. Merely collapsing an editor must not silently turn the option off; removing it must clear its submitted value and disarm picking.

Show only options the existing host offers. Presentation may organize those offers but must not invent eligibility calculations. Existing component-level rule checks need preservation during this presentation work, not additional copies in new shared components.

Give each action a stable proposal area at its end. Preserve existing button verbs and confirmation behavior. A concise reason should accompany disabled controls. Distinguish missing input, malformed location text, busy/waiting state, and an existing host-provided refusal. Do not invent a legality verdict when the engine has not provided one. The draft summary describes selected inputs; the existing proposal review remains authoritative about outcomes.

## 7. Adaptation across actions

| Family | Adaptation |
|---|---|
| Small-arms fire | Firing group, target, optional fire choices, proposal |
| Ordnance and vehicle fire | Weapon and crew context, target or existing alternate aim, ammunition/options, proposal |
| Infantry and vehicle movement | Moving units, destination or route, movement choices, proposal; preserve route order and vehicle-specific controls |
| Rally, repair, deployment, support weapons | Compact unit/equipment and assistance groups; no empty Target section |
| Rout and advance | Obligations and units first, destinations/routes next; preserve per-unit and shared routes and separate voluntary-break actions |
| Close combat | Preserve declaration order, participants, existing attacks, and separate resolution actions; no single universal submit button |
| Other contextual actions | Apply the same grouping and location conventions where applicable, without moving mandatory answers into optional disclosures |

The full action inventory must be accounted for during implementation. Fire is the first proving form, not the entire scope. The design must distinguish a completed rollout from a first milestone.

## 8. Acceptance and review

Success means a player can identify who acts, the effective location, any enabled options, and the next required input without reconstructing layout relationships. Verify empty, partial, complete, blocked, busy, and map-picking states, including long labels and building levels. Preserve hex-qualified unit labels and keyboard access.

The design must specify a rollout inventory, shared presentation contracts, draft transitions, disclosure and cancellation behavior, focused tests after Studio verification, output cleanup, and traceability to A1-A7 and sections 4, 6, and 7.

### Analysis review, 2026-10-31

Reviewed against the source baseline for accuracy, correctness, internal consistency, and scope. Corrections incorporated before the implementation design:

- Replaced the implied custom combobox commitment with a one-value interaction requirement and native-control option.
- Distinguished arming the next map click from using a hex already selected on the map.
- Made board/level identity and free-entry versus restricted-choice policies explicit.
- Identified mounted-but-hidden controls as a cancellation risk; disposal is insufficient.
- Clarified that rules, readiness verdicts, action availability, and confirmation policy are not redesigned here.
- Required an inventory and staged rollout across action families rather than implying Fire alone completes the request.

No unresolved contradiction was found in this revised analysis. Visual effectiveness remains a hypothesis to verify in Studio, not a tested result of this document.

### Close Combat review, 2026-10-31

The live review found equal visual emphasis on location, exceptional rules, attack building and completion. Empty Ambush and Director controls, free-flowing unit rows and always-visible infiltration inputs obscured the main sequence. More seriously, checked attackers and targets were not yet added attacks: the first side could still choose "Declare no attack and hand over", and the second side could propose resolution while its checked selection was omitted.

The approved correction separates first-side declaration, second-side declaration, combined review and resolution proposal. Unfinished selections block completion; Add or Clear is explicit. Preserve the existing side visibility, first-side storage, sequential-round handling and rule verdicts. Optional rules belong in disclosures, while required stacking and withdrawal inputs stay available. Map inspection selects only an offered location, never guesses a level, and protects drafts before changing locations. The implementation design section 14 records the approved contract.

The subsequent Support Weapons question exposed a separate correctness issue. A4.43 (PDF p. 50) permits the start-of-CC weapon drop in order to withdraw from Melee; A11.21 (p. 73) limits what a withdrawing unit carries. The current generic offer checks phase and enemy presence; the command checks possession, an unbroken holder and no previous CC, without requiring withdrawal. Consequently it offers Drop in the fresh-CC demo. Generic receiving-unit and cross-phase help also distract here. Record the eligibility correction separately, keep its rule in Rules, and place permitted weapon-drop choices with withdrawal. Removing all CC weapon handling would omit legitimate cases. Backlog section 91 tracks this finding; the approved presentation change does not silently change that rule.

### Rally review, 2026-10-31

The user deferred Support Weapons to a separate evaluation across every phase. That includes its Rally presentation; this review does not approve weapon handling changes. The following Rally proposals await approval.

The separate `ui-rally-demo-02` position contains broken units in multiple hexes on both sides, nearby leaders, an isolated broken leader, and a squad and two half-squads for organization actions in [J5]. Studio confirmed an assisted Rally preview for the German squad in [F5]; it passed every check and was cancelled without rolling or committing. Selecting [D10] on the map left [F5] selected in Rally and retained the full broken-unit list. Thus inspection and action selection still diverge in this phase.

Selecting [F5] initially defaults to Self-Rally and displays only "The Rally package refuses this attempt as proposed". Selecting the available leader instead enables the valid preview. A10.6 and A10.63 (rulebook PDF p. 68) distinguish assisted Rally and Self-Rally; A10.71 (p. 69) covers a broken leader's own attempt and subsequent assistance. The UI must present the existing Rules verdicts, not duplicate these conditions. Its default must not imply an unavailable method is ready, and a refusal should identify the missing requirement rather than name an internal package.

Proposed interaction: selecting a hex scopes the current side's Rally candidates to that board and hex, preserving level-qualified labels. Selecting another applicable hex replaces the scope; selecting an unrelated hex or choosing "Show all rally units" releases it. Inspection alone does not attempt a rally. Clear stale unit and assistance selections when the scope excludes them, and invalidate stale choices after an action or side change. Show assistance after unit selection, use Rules-backed eligibility for the options, and retain a concise explanation where an unavailable option needs explaining. Do not turn nondefinitive verdicts into legal choices.

Deploy and Recombine currently expose empty assistance/partner controls before the first unit is chosen. Give the two workflows separate headings, show only the applicable forms, and reveal their dependent fields after selection. Extend the same map-selection contract to their own candidate lists without mixing their eligibility with Rally eligibility. Keep a release control reachable even if the resulting list is empty. Retain detailed rules in the disclosure and remove the Rally help's implementation commentary and unrelated repair description.

DM retention is an end-of-phase choice, not an immediate Rally attempt. Its existing planner filter and submission timing must remain authoritative; shorten its presentation separately from the unit's Rally method. No retention control appeared in this demo, so this review does not claim that interaction was verified. Shock recovery and special-case eligibility likewise still need representative follow-up checks before claiming full Rally coverage.

Review for accuracy and consistency: distinguish the Studio observations above from source findings and proposed changes; preserve both sides' access and handover; retain confirmation before rolling; keep eligibility in Rules; do not introduce Close Combat's simultaneous declaration sequence into Rally. No application code was changed for this review.

Subsequent correction: the user's map-scoping report authorized that specific interaction. Design section 15 and the implementation record document the completed Rally scope filter and Studio checks. The other proposals above remain pending.
