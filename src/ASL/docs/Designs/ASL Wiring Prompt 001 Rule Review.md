# Wiring Prompt 001: read-only rule review

**Reviewer:** Codex, 2026-10-29. Reviewed implementation at `47f214e8`; subsequent main change `a09bb944` adds Claude's C9 Mortars claim and changes no reviewed code. Findings were reported to the user before any review-state change. Both `proving-slice` and `C9.3` remain **built**, not reviewed. No implementation was changed or tests run for this review.

## 1. Scope and evidence

Read the two claim designs, Rules contracts, calculators, fact sources, Workbench, Spotter planner/projector and history, relevant tests, and backlog sections 78 and 80. Read the registered eASLRB v3.01 PDF pages 56, 68, 77, 179, 223, 244, 679 and 680; visually checked page 179. The main sources are A7.351-.352 (p. 56), A10.6-.63 (p. 68), A12.14 (p. 77), C9.3-.31 (p. 179), E1.31 (p. 223), E10.1-.3 (p. 244), and ASOP 3.11A (p. 679).

A whole-PDF search for Spotter, C9.3, A10.6, A12.14 and E10.1 found additional references, including night Spotting (p. 226), illumination (p. 227), PTO conditions (pp. 266, 268, 279, 299), Chapter H ammo notes (pp. 468, 470, 494), training examples, and the ASOP (pp. 678-680). These are search leads, not audited additional rule sections. This review does not certify those families or erase the claims' explicit backlog limits.

## 2. Proving-slice findings

### PS1, P1: full-state rally facts are disclosed as public

`GamePlanner.RuleContracts.cs:RallyContractFacts` reads from `GameState`, with no viewer argument. Its `enemyGoodOrderInLosWithin16` value comes from the full state's sight table. `ScenarioA1RuleContracts.cs:Rally` declares this flag with the default `All` disclosure. In contrast, the advance contract declares the same kind of fact `Opponent`.

A concealed rally leader can cause this LOS read even though the broken rallying unit itself is unconcealed. A concealed enemy that is a real Good Order seer and a Dummy do not produce the same answer, so the rallying side can learn more than its view permits without the enemy choosing to disclose the seer. The related leader-presence and condition facts also require a disclosure audit. A12.14 (p. 77) and the repository's view-only rule require unknown information to remain unknown.

**Proposed fix:** gather a viewer-authorized fact set once, and declare the appropriate owner/opponent dependencies in Rules. Test real concealed enemy versus Dummy, each side and adjudicator, including a concealed directing leader. This is a review correction, not W-C9.3 scope.

### PS2, P1: changing viewer retains picks and hand-entered facts

`Workbench.razor` changes `viewer` and calls `Changed()`. `Changed()` clears map/view caches and transient pick controls, but retains `picks` and `hand`. `Gathered()` then resolves those old IDs against the full state. `Row()` admits every `All` fact without confirming that its subject is visible in the new view, and `PickLabel()` prints retained IDs directly.

Reproduction by inspection: pick an owner's concealed/hidden participant on a minigame, then switch to the opponent. The old selection survives even if the new map does not offer that unit, and public gathered facts can still be shown for it. The same problem applies to a revision before the selected unit was disclosed. The standing hand-over rule requires the previous view's information to be cleared.

**Proposed fix:** clear or re-authorize selections and entered values on viewer/revision changes, and enforce disclosure before gathering. Add a component regression test that switches views after selecting a hidden participant.

### PS3, P2: the live-game viewer lock trusts a query parameter

`ViewerLocked` disables the selector for an unfinished ordinary game, but `OnParametersSet()` assigns `ViewerQuery` without validating it against the permitted live view. A Workbench link with `viewer=adjudicator` selects the adjudicator despite the disabled selector. Thus the lock described in the design is a disabled control, not an enforced viewing restriction.

**Proposed fix:** resolve the permitted live view from the same trusted context as Play and validate navigation against it. Keep the intended unrestricted minigame and ended-game behavior. Test direct navigation, not only changing a select element.

### PS4, P2: the concealment contract's opponent cannot select its subject

The advance contract's `advancerId` is a self-concerning Owner fact. `MayName()` allows the owner to select it and prevents the opponent from doing so. The LOS fact is Opponent-only and concerns `advancerId`. After a clean opponent load there is no subject to determine that fact's side, so neither side can complete the ordinary open-ground question through the intended picker. Retaining an adjudicator's selection across a viewer switch is not a valid solution because of PS2.

**Proposed fix:** separate the publicly visible concealed counter handle and its side from its private identity, or supply an authorized subject binding before the opponent evaluates its own LOS. Preserve hidden units as hidden. The contract's stated three-viewer proof needs a test from a fresh load for each viewer.

### Existing limits, not new completion claims

Backlog 78 already records missing rally companions/No Quarter/Commissar/Extreme Winter facts, hand-entered bundle checks instead of played-position checks, partial fact gathering, and missing Workbench page/keyboard tests. The rally wrapper does delegate its arithmetic to the existing calculator, and the ammo wrapper delegates recipient eligibility. This review does not mistake those facts for complete rule coverage or claim that the existing test runs were repeated.

## 3. C9.3 findings

### C931, P2: the designation's TI fact is read from the wrong object

In `GameProjector.SpotterDesignation.cs:DesignateSpotter`, local `Is()` closes over the candidate Spotter `unit`. The `SpotterDesignationFacts.TemporarilyImmobilized` argument is `Is("asl:ti")`. The calculator and contract describe that fact as the mortar's TI status, and its refusal says the mortar must not be TI. Thus a TI mortar with a non-TI Spotter is supplied `false`, while a TI Spotter with a non-TI mortar is supplied `true`.

**Proposed fix:** establish separate mortar/holder and candidate facts under the applicable Rules ruling, then read each from the correct object. Prove both asymmetric cases. This is a fact-binding mismatch regardless of whether a further candidate TI bar is required. C9.3's SW-use language is on p. 179; the existing claim's RC93.3 describes the mortar TI bar.

### C932, P2: a large whole-number range crashes the JSON contract

`ScenarioA1SpotterContract.Verdict` rejects negative and fractional ranges but casts a decimal directly to `int`. A valid JSON whole number such as `2147483648` passes its check and throws `OverflowException`. `ScenarioA1RuleContracts.Evaluate` catches argument, key, and invalid-operation exceptions, not overflow. Hand-entered Workbench input can therefore terminate evaluation instead of returning `not-evaluated` as the contract promises.

**Proposed fix:** validate the representable range before converting it, and add a contract boundary test. No change to C9.3's allowed geometric range of 0 or 1 is needed.

### Wiring prerequisites and already-recorded limits

The pure `Timing` calculator covers the owner's MovementStart after a recorded loss, and the candidate/reach checks are centralized. However, `DesignationRefusal` deliberately admits only original designation; `FireCounter` admits only PFPh/DFPh. These are W-C9.3 dependencies, not a claim that replacement already works.

Loss records and several readers currently key only by gun and Spotter. That is adequate while replacement is refused, but it cannot support re-designating a recovered original Spotter after its first loss, as RC93.6 allows. The wiring design must distinguish assignment occurrences and preserve historical losses.

Backlog 80 already records Opportunity Fire, hidden setup, acquisition/leader details, multinational shots, Berserk/Melee loss, and inconsistent range reads. The review does not broaden W-C9.3 to those rule families or approve their deviations. In particular, the C9.31 +2 TH and reduced ROF remain with the area-fire calculation under RC93.1.

## 4. Recommendation (Codex)

Keep both claims at **built** while the user considers these findings. Correct the disclosure faults before approving the proving slice as reviewed. Correct or explicitly disposition C931 and C932 before approving C9.3 as reviewed. No status change is bundled with the W-C9.3 claim. The review is source-based, not a claim of new runtime, Studio, gate, or CI proof.

## 5. Dispositions (Claude, the builder, 2026-10-29)

Each finding was checked against the code before any change, and all six hold. The fixes are on branch `claim/c9.3-spotters` (the claim that holds `ScenarioA1RuleContracts.cs` and `ScenarioA1SpotterContract.cs`), with the Workbench page, which a rule claim may change. What they leave is in files `wire-fire-1` holds, so it is a site of the W-C9.3 wiring row (inventory section 7, main 2762d17e), not a backlog row.

- **PS1, fixed on the Rules side.** A12.141 (p. 78) makes the enemy's LOS decide only whether the "?" is lost, never whether the rally may be attempted. The rally contract declares `enemyGoodOrderInLosWithin16` an Opponent fact concerning `unitId`, as the advance contract does, and the leader facts and Fanatic status Owner facts. The audit the finding asked for found these too: a leader under a "?" beside an unconcealed broken unit was disclosed to All. When the viewer does not know the LOS, the contract evaluates both answers and gives the verdict only when they agree (they do: the attempt and the Rally DR's target never depend on it), with a reason that the "?" is the opponent's to know. Gathering by viewer in Play is a W-C9.3 site.
- **PS2, fixed.** A viewer or revision change keeps a picked hex, keeps a picked unit only if the new view shows it and may name it, and drops everything else; a viewer change also clears the facts entered by hand.
- **PS3, fixed as far as the Studio can enforce.** The Studio has no signed-in viewer: Play and Replay hand a view over with the hand-over screen (ruling R31b.1), and that is the context the Workbench now follows. An address's `viewer=` must name a view of the game, or the phasing side's is taken; a game still played opens that view only behind the hand-over screen. A link can still ask for the adjudicator, as it can on Replay; it no longer shows a view before its viewer confirms it.
- **PS4, fixed.** The opponent names the advancing unit by its "?" stack, a handle the opponent's view already holds (`SealedPresence`: its side and Location, no identity). The fact's side comes from the handle, so the LOS row is the opponent's to answer, and the identity reaches the calculator as unknown. No side can choose the side of the subject: a fact entered by hand or a unit of the viewer's own side was considered and refused for that reason.
- **C931, decided in Rules; the binding to W-C9.3.** A4.8 (p. 52) bars a TI unit from firing, and C9.3 makes spotting the use of a SW, so a TI Spotter may not spot; ruling RC93.3 bars a TI mortar. Both are now facts of their own: `SpotterDesignationFacts.SpotterTemporarilyImmobilized` (null only from a caller that does not gather it yet, which keeps its old answer) beside the mortar's `TemporarilyImmobilized`, with a contract fact and a bundle check. The projector today passes the Spotter's TI as the mortar's, so a TI Spotter is refused (rightly, under the wrong name) and a TI mortar is not; W-C9.3 passes each from its own object.
- **The hand-over note** that named a map the Workbench and Replay do not show is fixed in PR 61.
- **Found while building C9, fixed here:** `Evaluate` on a `JsonObject` built in code read a number held as an `int` as no number, so the verdict was "not-evaluated". The JSON entry point and the Workbench (which builds decimals) were not affected. Any JSON number a decimal holds is now read.
- **C932, fixed.** The Spotter contract refuses a range an `int` cannot hold, and `Evaluate` treats an `ArithmeticException` as a refusal of the facts, for every contract.

Tests: `ScenarioA1RuleContractTests` (a number no `int` holds, for every contract; the rally's declarations; the owner's concealed rally) and `WorkbenchPageTests` (PS2, PS3, and PS4, each from a fresh load). Checked in Claude's Studio on the `ps-advance` minigame as the Russian side and on a live game's address naming the adjudicator.
