"""Reconcile the Support Weapons review notes without changing generated status lines."""
from pathlib import Path

docs = Path(__file__).resolve().parents[3] / "src/ASL/docs"


def replace(relative, old, new):
    path = docs / relative
    text = path.read_text(encoding="utf-8")
    if new in text:
        return
    if old not in text:
        raise SystemExit(f"Expected text absent: {relative}: {old[:70]}")
    path.write_text(text.replace(old, new), encoding="utf-8", newline="\n")


replace("ASL Unit Backlog.md", "No implementation yet. Separate capability gaps are recorded in section 93.",
        "Implemented under the approved SW-01 combined delivery. The user approved all eight phase demos; PR 96 review corrections, the wiring gate and CI are the remaining release checks. Merge remains paused. Separate capability gaps are recorded in section 93.")
replace("Designs/ASL Support Weapons Implementation Record.md", "The user approved all phase demos and authorized merge after green CI on 2026-10-31.",
        "The user approved all phase demos, then paused merging. PR 96 must not merge or enable auto-merge without a fresh instruction.")
replace("Designs/ASL Support Weapons Implementation Record.md", "Corrected the proposed APh restriction after independent review of A4.43, PDF p. 50: dropping is allowed during APh, not only before advancing. Portage history still needs explicit verification.",
        "The initial review incorrectly widened the APh drop window. PR 96 restores the established before-advance restriction through the shared Rules verdict and an Advance fact. This supersedes that checkpoint's conclusion.")
replace("Designs/ASL Support Weapons Studio Demo.md", "Status: all eight phase demos approved, Rules contracts and focused checks complete.",
        "Status: all eight phase demos approved. PR 96 review corrections are implemented and undergoing fresh focused verification; earlier test evidence below is historical.")

path = docs / "ASL Rule Inventory A to E.md"
notes = {
    "A4.43": ("GamePlanner.SupportWeapons.cs: SupportWeaponDropRefusal; Play.razor: contextual drop offers; GameProjector.cs: versioned drop verification",
               "ScenarioA1SupportWeaponPossessionRules.DropRefusal, RecoveryMovement; ScenarioA1SupportWeaponRules.DropPhase",
               "PR 96: shared drop eligibility receives the unit's Advance fact. APh drop closes after advance. Approved ruling RSW.1 replaces standalone CCPh drops with equipment committed during valid withdrawal. Existing recorded drops retain their reading; no generic RtPh drop bypasses the enclosing best-load workflow."),
    "A4.431": ("SupportWeaponTransferRead.Refusal; GamePlanner.SupportWeapons.cs: TransferReadiness; GameProjector.cs: versioned transfer verification",
                "ScenarioA1SupportWeaponPossessionRules.TransferRefusal; ScenarioA1SupportWeaponRules.TransferPhase",
                "PR 96: transfer phase, participants, same-Location, action and advance restrictions are decided in Rules. Unknown conditions and unsupported Wire/carriage give not-evaluated. Contracts expose giver/recipient as Owner Unit facts and phase as an All Choice. Special PRC/terrain relationships remain in backlog 93."),
    "A4.44": ("SupportWeaponRecoveryRead.Facts; GamePlanner.SupportWeapons.cs: RecoveryReadiness; GameProjector.cs: versioned recovery verification and movement effects",
               "ScenarioA1SupportWeaponPossessionRules.RecoveryRefusal, RecoveryMf, RecoveryMovement; ScenarioA1SupportWeaponRules.RecoveryBypassBar, RecoveryMfShort",
               "PR 96: Rules decides recovery readiness, MF and the defensive-fire window. Same-mover recovery adds MF without clearing C6.17 shots; a new mover resets the shot context. Unsupported terrain, Wire, carriage or unknown conditions give not-evaluated. Immediate event-bound SMC recovery remains backlog 93."),
}
lines = path.read_text(encoding="utf-8").splitlines()
for index, line in enumerate(lines):
    parts = line.split("|")
    if len(parts) > 13 and parts[1].strip() in notes:
        where, calculator, note = notes[parts[1].strip()]
        parts[5], parts[6], parts[12] = f" {where} ", f" {calculator} ", f" {note} "
        parts[7] = " SupportWeaponScopeTests; BacklogPass13Tests; support-weapon-scope minigame checks "
        lines[index] = "|".join(parts)
path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def append_once(relative, marker, text):
    path = docs / relative
    old = path.read_text(encoding="utf-8")
    if marker not in old:
        path.write_text(old.rstrip() + "\n\n" + text.strip() + "\n", encoding="utf-8", newline="\n")


append_once("ASL Unit Backlog Passes Plan.md", "## Support Weapons review amendment RSW.1", """
## Support Weapons review amendment RSW.1

Approved by the user on 2026-10-31: "Approve withdrawal-only CCPh drops". This amends R13.5 for new commands. A4.43 (PDF p. 50) and A11.21 (p. 73): equipment choices are part of a valid withdrawal, and commit with it. Cancelling a draft drops nothing. Historical standalone drop records retain their recorded reading. The APh window remains before that unit advances. R31d.1's best-load result is unchanged; the approved Rout workflow commits its required drops with the rout rather than offering a separate draft action.

The SW-01 combined Rules/caller delivery and its narrow claim-lock sequence were separately approved. Wiring gate and green CI are mandatory. Merge remains paused until a fresh user instruction.
""")
append_once("ASL Rule Coverage.md", "## Support Weapons PR 96 review", """
## Support Weapons PR 96 review

Branch audit, not a merged acceptance: A4.43/.431/.44, A9.72/.8 and A11.21 now have shared Rules readiness and contracts, with caller wiring held under SW-01. The inventory's existing partial coverage classifications and totals are unchanged. Recovery/Transfer distinguish unknown and unsupported facts from refusals; capacity and configuration type decisions are in Rules. Repair retains the known-definition/missing-nationality reading. RSW.1 governs withdrawal-only CCPh drops. C6.17 recovery expenditure preserves shots for the same mover and resets for a new mover.

The bundle adds Recovery/Transfer boundaries, a concealed-enemy fact check, Russian actors/viewers and squad/half-squad capacity cases. These prove the supported slice, not complete Support Weapons coverage. Immediate SMC recovery, deliberate destruction, additional dismantled counters and exceptional terrain/PRC relationships remain in backlog 93. The wiring gate and CI evidence is recorded in the Support Weapons Studio Demo document when complete.
""")
append_once("Designs/ASL Support Weapons Phase Scope Proposed Design.md", "### PR 96 cache and contract corrections", """
### PR 96 cache and contract corrections

The generator declares unknown values only for nullable facts. Phase is a public Choice; transfer participants are self-concerning Owner Unit facts, and their dependent facts concern the appropriate participant. Configuration and repair receive unit kind and printed weapon facts, not caller-supplied capacity or type permission. Recovery and Transfer return structured readiness. Legacy helper answers remain available, and the new calculators call those helpers for the common decisions.

The page owns one latest-state/latest-view cache for equipment locations and duplicate-label ordinals. Withdrawal choices share that lifetime; per-unit load text keeps only the latest drop selection. The workflow component refreshes on a new state snapshot, context, selection or explicit scope change, not every unrelated parent render. No cache is global, persists across views, or stores asynchronous work. Unknown load results are cached for that snapshot too. Stable keyed details elements retain their browser open state. Committed outcomes survive candidate-list removal.

A7.353 (PDF p. 56) keeps earlier First Fire uses separate from the current Final Fire allowance. The restored-inherent verdict preserves the actual First Fire marker so the complete fire calculator still halves FP and enforces adjacency. Current full SW capability still prevents inherent FP, and configuration still consumes the configured weapon's entire ROF. Null-version fire records keep their old reconstruction.
""")
