# W-C9.3 fire wiring design

**Status:** Proposed for user approval, 2026-10-29. No implementation authorized yet. Claim `wire-fire-1` (Codex, reviewer Claude), branch `codex/wire-fire-1`, takes W-C9.3 only. The inventory sections 7 and 8 are the ownership ledger. The [read-only reviews](<ASL Wiring Prompt 001 Rule Review.md>) leave both rule claims at built.

## 1. Scope, prerequisites and exclusions

Wire a governed replacement of an eliminated, broken or captured mortar Spotter at the owner's following MPh opening, and replace the divergent mortar-to-Spotter range reads with one geometry read. C9.3-.31 are on physical PDF p. 179; ASOP 3.11A is on p. 679. C9.3 allows replacement at the start of the owner's MPh following loss; the timing calculator already represents this as `MovementStart`.

**Missing prerequisite first:** `ScenarioA1SpotterCalculator.DesignationRefusal` deliberately rejects every replacement window and `FireCounter` throws for MPh. A wiring branch is not authorized to invent replacement eligibility or marker effects outside Rules, or to extend Rules under a row whose old-members column says None. Before implementation, Claude's C9.3 follow-up must supply a replacement refusal/effects verdict and a replacement contract, preserving the old original-designation member's answer. It must also specify which recorded actions close the ASOP 3.11A checkpoint. This design requests that contract explicitly; it does not claim the existing API is sufficient.

Proposed Rules shape: a replacement facts record carrying `SpotterTimingFacts`, candidate facts, mortar/holder availability and TI facts, integer hex range, sharing facts, pending-work facts and relevant use facts; a verdict carrying the refusal and permitted state effects. It reuses `Timing`, `CandidateRefusal`, `InReach` and `SharesLocation`. Proposed effect reading for Claude's rule review: replacement establishes an assignment at ASOP 3.11A, and does not manufacture a PFPh/DFPh fire counter or a firing permission. Actual fire still records SW use through its existing Rules verdict. No caller will decide this reading itself.

No changes are planned to Claude's shared ordnance Rules files. C9 Mortars (C9.1, C9.2, C9.4, C9.5) remains Claude's. Any additional held file is added to the inventory on main before editing it.

Excluded: Opportunity Fire and its breakdown check; night designation/Gunflash; hidden setup activation; multinational-shot validation; acquisition and leader-direction changes; new Berserk/Melee loss semantics; general Workbench review repairs; the preparatory broad projector/services split; PR 58 fixture repairs. Existing backlog rows remain open, and the PR 58 failures gain a separate row. The replacement route continues to refuse unsupported night cases through Rules. This is not acceptance of a whole phase or all of C9.

## 2. What decides, gathers and records today

| Site | Role today | Proposed change |
|---|---|---|
| `GamePlanner.Spotters.cs:PlanSpotterDesignation` | Gathers identity and disclosure, emits an event and replays it to obtain the projector's refusal | Gather current assignment and checkpoint facts, ask the same replacement verdict as projection, keep revision-bound proposal/confirmation |
| `GameProjector.SpotterDesignation.cs:DesignateSpotter` | Gathers original-designation facts, calls Rules, writes assignment and phase marker | Preserve v1 behavior; use the new Rules verdict for a versioned replacement event and apply only its effects |
| `GameProjector.Spotters.cs:MaintainSpotterLosses` | Records Rules' loss/successor answers | Bind losses and successors to the current assignment occurrence; retain historical records |
| `LiveOrdnance.cs`, `GamePlanner.SupportWeapons.cs`, projector assignment readers | Pass a gun/Spotter-pair loss flag to Rules | Gather the current occurrence's loss flag so an old loss does not bar a new assignment |
| `GamePlanner.Ordnance.cs:SupportWeaponMapFacts` | Reads range via same-hex, Step adjacency, then LOS | Read geometry range and hand it to `InReach`; preserve the old range path for old recorded readings |
| `GameProjector.SpotterDesignation.cs:SpotterHexRange` | Reads placed geometry or board geometry, with same-hex fallback | Call the shared Maps range primitive with the same state-specific map context |
| `GamePlanner.FireHistory.cs`, `ASL.Turns/TurnFireHistory.cs` | Gather disclosed history and display calculator answers | Include replacement declarations and active occurrence without exposing private IDs or obsolete losses |
| Play controls and `TurnGuideEvidence.razor` | No replacement control; declarations labelled original | Add a small designation panel and distinguish original/replacement history; candidate filtering uses the command's refusal |
| `GamePlanner.RuleContracts.cs` | No C9.3 gathered fact source | Gather replacement-contract facts through the same read used by the command, with viewer authorization |

## 3. Event, assignment and opening semantics

1. Reuse the existing governed designation action and `MortarSpotterDesignated` payload if its gun/Spotter fields suffice. Emit replacements with a distinct provenance version (`asl.spotter.v2` proposed), leaving `asl.spotter.v1` original records on their existing replay route. If the event needs more evidence than the existing journal can reconstruct, add an optional versioned payload only after extending the claim's held-file list. Do not infer a new reading merely from today's code version.
2. Reconstruct the active assignment occurrence from the latest designation or legacy observed assignment and its successor chain. Use event identity/revision as the occurrence identity, not `(gun, Spotter)` alone. Preserve old losses in the journal. Replacing A by B and later re-designating A must not inherit A's old loss; losing the new A must create a new loss. A replacement updates the single active assignment for the gun instead of appending a duplicate active entry.
3. Preserve legacy shot-created assignments. A witnessed shot plus a recorded loss can supply origin evidence; a current broken counter alone cannot supply when it was lost. Missing origin/loss history produces a refusal or not-evaluated verdict, never an invented window.
4. Derive the opening from the actual MPh occurrence and recorded progress. Ask Rules whether intervening actions have passed ASOP 3.11A. Consecutive replacements can share the opening while no later step has occurred. Once movement or another later ASOP step starts, it stays closed even after that move ends. A phase label and `Movement == null` are insufficient evidence.
5. Projection independently checks the same facts and Rules verdict; a hand-edited event cannot bypass timing, side, candidate, reach or sharing. Proposal is read-only and consumes no dice. Confirmation rechecks the expected revision. Cancellation and stale confirmation write nothing.
6. Use the existing private-event path when a participant or holder is concealed/hidden. Choice lists and refusals read the acting viewer's authorized view. Replacement never grants the opponent a private identity, loss cause, assignment count or refusal derived from a hidden candidate. Clear selections on revision/side changes.

## 4. Shared range read

Add `Maps/Read/MapHexRange.cs` as a geometry-only primitive. It takes board/hex identities plus already-resolved placed layout or board geometry, returning `int?`. It decides neither eligibility nor the range threshold. Both Play and Units adapt their existing map contexts to it.

Placed geometry takes precedence and accounts for reversed boards and seam ownership. A single-board fallback is used only under the established same-board geometry policy; unknown endpoints or unavailable geometry stay unknown. Levels, terrain, obstructions and LOS do not affect C9.3's hex range. Do not silently reinterpret a failed cross-board location as local coordinates. Rules alone applies `InReach`.

Play gets geometry through `MemoOf(state)`, `Handle` and `Composed`, preserving pinned versions and missing results. Projection uses the layout/chains already resolved for its replay. No board catalog fetch, LOS calculation or composed-map rebuild is added per candidate. The legacy event path retains its old range interpretation where replay equivalence needs it; new proposals use the shared read. Gate differences must identify changed cross-board/level range answers explicitly.

## 5. User interface and facts

The Action panel offers replacement only through a planner read that uses the Rules replacement refusal. A dedicated `SpotterDesignationPanel` presents the owner's affected mortar and eligible candidate choices with hex-first labels using shared unit-choice controls. It uses the ordinary propose/confirm flow and explains when history or map facts are missing. The panel adds no rule thresholds or phase checks of its own.

The Turn Guide displays the same authorized verdict and labels replacement declarations correctly. It does not mark all fire or movement capabilities complete. The new contract can be inspected on the Workbench once its facts have a viewer-safe source; unresolved Workbench disclosure review findings must be repaired before using it as a side-view acceptance proof.

## 6. Proof and implementation sequence after approval

1. Obtain the Rules follow-up and its tests under Claude's claim, including a precise opening/marker contract and disposition of the C9.3 review findings. Merge current main into this branch, never rebase; rerun overlap checks before caller edits. Refresh the held-file list if necessary.
2. Establish a wiring-specific gate configuration and baseline from current main using Codex's working folder. Add representative minigame positions before relying on a sweep. Existing records contain no designation proof game according to C9.3's as-built account, so old-game equivalence alone cannot prove this feature.
3. Implement shared geometry and assignment/checkpoint gathering, then projector and planner calls, then the small UI and history read. Keep the event version and old recorded reading explicit. Build only the isolated Studio while developing.
4. Play the `c9-spotter` minigame through original designation, a recorded break/elimination/capture, the next own MPh opening, replacement, and subsequent spotted fire. Also show refusal in the intervening phases, the opponent's MPh, and after movement has begun and ended. Demonstrate two replacements at the same opening and a recovered original Spotter designated again. Include a Good Order reduction successor that needs no replacement.
5. Check owner/opponent/adjudicator views, hidden/concealed participants, keyboard and mouse, hex labels, stale/cancelled proposals, and a narrow screen. Proposed whole-pass UI scope: the new panel and affected history at 1920, 1024 and 320 pixels, with a side-switch disclosure check. No unrelated Studio walkthrough is proposed.
6. Run only methods asserting the changed behavior. Rules tests belong to the Rules follow-up; wiring tests cover planner/projection parity, direct-event refusal, assignment occurrences, revision races, geometry 0/1/2 and unknown, differing levels, reversed boards and seams. Share expensive map inputs once per run. No new test project is planned.
7. Run the wiring merge gate, strict checks and CI. Classify every replay/sweep difference as replacement availability, occurrence-correct loss handling or shared-range behavior, with legacy records unchanged. Persist the played acceptance games in the bundle and update claims, coverage/status documents and backlog only when their completion conditions hold. Claude reviews the wiring. The user gives the word for each merge.

## 7. Memory and performance checklist

- **Added work:** one assignment-history read per affected state and one range read per candidate considered. Avoid a full history scan for every candidate; build/reuse the current assignment facts once in the existing state memo. No per-hex rendering work is added.
- **Ownership/lifetime:** any added memo belongs to `PlayStepMemo` and the immutable state object. Viewer-specific results also key by viewer and selected mortar/assignment. No process-wide revision dictionary. Immutable results; concurrent fills have no side effects. Missing geometry is retained only for that state's existing map lifetime.
- **Disclosure and identity:** preserve viewer authorization, snapshot/revision consistency, pinned board versions, seam ownership and failed reads. This work does not touch rout settlement.
- **Rendering:** reuse the existing map and incremental counter overlays. Assignment history/panel changes do not force a full-layer SVG or map transmission. Changes of viewer invalidate private choices.
- **Regression evidence:** state/side/version/seam/missing-geometry tests plus replay and sweep gate. Count range/context resolutions in a focused diagnostic if necessary. No speed gain is claimed. If representative Play timings regress materially, investigate before acceptance rather than adding caches speculatively.
- **Test cost:** share immutable expensive board inputs. Keep fresh-state tests genuinely fresh so memo hits cannot hollow out the assertions. Existing projects remain in their existing CI shares.
- **Results and limitations:** report exact logs, intended differences, unbuilt dependencies and backlog rows. No runtime verification is claimed at design time.

## 8. Build outputs outside the repository

No builds are performed for this design. After approval, set `ASL_GATE_WORK=E:/Archive/GitHub/dlandi/asl-gate-work-codex` before every `code-builders` invocation. Studio uses port 6680 and an isolated build under that folder, never MapStudio's nested `bin` folders. Gate and filtered-test outputs stay under the same Codex work folder.

Register each outside output when created with `outside_outputs.sh`; stop and remove this task's Studio/build outputs as soon as they are no longer needed, at the latest after CI passes. The final cleanup checks the registry and leaves other tasks' outputs untouched. The source worktree is `C:/Users/dland/.codex/worktrees/wire-fire-c93/LimboDancer.MCP`.

## 9. Approval requested

Approve the scope and sequence above: obtain the missing Rules verdict first, then wire replacement with occurrence-aware history and a shared geometry read, preserving old replay readings. The user explicitly required a design approval stop. No implementation, rule-review status change or merge is included in this approval request.
