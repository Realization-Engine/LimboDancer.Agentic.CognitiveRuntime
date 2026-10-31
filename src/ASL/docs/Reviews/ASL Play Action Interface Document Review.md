# ASL Play Action Interface Document Review

Date: 2026-10-31. Reviewer: Codex, self-review. Source baseline: `96fcf297`.

Documents: [analysis](<../ASL Play Action Interface Analysis.md>) and [implementation design](<../Designs/ASL Play Action Interface Implementation Design.md>).

## Method

Compared the analysis with the Fire screenshot and current component/page code. Reviewed the design for accuracy, correctness, internal consistency, and coverage of the revised analysis. Followed location ownership through `SmallArmsFirePanel`, `Play.FireTarget`, the target callbacks, `LocationField`, `PickedOption`, `LocationPicking`, and `DisplayText.ReadPlace`. Checked visibility against mounted action sections and the inspector's retained tabs. Compared the rollout against representative rally, movement, rout, advance, ordnance, and close-combat forms and the Play component inventory.

This was a document and source review, not an independent reviewer, executable test, exhaustive phase audit, or new Studio verification. The documents make that boundary explicit.

## Findings and disposition

| Finding | Correction | Status |
|---|---|---|
| A combined-input sketch could imply an unnecessary custom combobox and omit its keyboard contract. | Analysis specifies one effective value, not one custom widget. Design uses native selection and explicit manual mode. | Fixed |
| Preventing bypass of disabled destinations could accidentally prohibit existing arbitrary fire targets. | Analysis A5 and design 3.1 distinguish restricted fields from suggestions in a free-entry field. Existing proposal checks remain authoritative. | Fixed |
| A single target string loses the distinction needed to preserve existing list pruning versus free-entry behavior. | Design 3.3 retains Suggested/Free provenance beside one authoritative value and requires migration of every prune/reset/read path. | Fixed |
| A text draft may leave a previous valid target active until blur. | Design 3.2 requires immediate pending-edit status, host notification, and a proposal-handler guard. It also addresses Enter/blur duplication. | Fixed |
| Comparing the bound value alone misses resets to the same value. | Design requires an explicit context/reset identity and separate handling of host resets and ordinary rerenders. | Fixed |
| Hidden editors can remain mounted and armed. | Analysis and design require explicit visibility cancellation for section close, tab changes, option removal, and context resets. Disposal is only a fallback. | Fixed |
| Map selection could silently flatten board or level identity. | Both documents preserve board qualification, ground-level pick semantics, and explicit candidate-level selection. | Fixed |
| Optional controls could be hidden while still affecting submission, or collapsing could silently clear them. | Active summaries remain visible; Remove explicitly clears values and picking state. Collapse preserves the draft. | Fixed |
| Applying a uniform four-group template would inflate short forms and distort routes or declarations. | Groups are optional; routes retain their own editor and multi-stage actions keep distinct commands. | Fixed |
| Fire-only verification would not establish completion of the wider request. | Design includes staged family coverage and a mandatory full action-entry inventory, including inline and conditional controls. | Fixed |
| Presentation changes could accidentally alter confirmation or introduce rule decisions. | Existing guard and confirmation behavior is preserved; shared components own only layout, input syntax, and presentation state. | Fixed |

## Coverage and remaining limits

Design section 9 maps all numbered location requirements and the analysis's grouping, optional-choice, and phase-adaptation requirements to implementation sections. The performance checklist, viewer disclosure boundaries, Studio-first checks, focused-test policy, external outputs, and cleanup are included.

No unresolved document defect was identified after these corrections. Implementation still must establish the detailed per-action inventory against refreshed main, verify all conditional states in Studio, and record actual validation results. Those are explicitly planned implementation checks, not claimed completed work. The new components and interaction details remain proposals pending the user's approval.

## Second review

The user authorized implementation through merge after green CI on 2026-10-31. Rechecked the interaction transitions and source contracts. Two further omissions were corrected in design sections 3.1 and 4: a map pick awaiting level disambiguation must block submission of the old target, and section cancellation must be limited to its owned picker. Hiding an editor must preserve pending typed edits while cancelling map input; resetting it clears both. These distinctions prevent cancellation from silently re-enabling an obsolete draft. Authorization statuses were updated. No further document-level contradiction was identified; implementation and Studio validation remain outstanding.
