# U1 audit against preview and accepted backend

Date: 2026-10-27

Baseline: U1 commit `1f537bc0`, the frozen `turn-phase-guide.html` preview, the B5 acceptance/handoff, and the typed ASL.Turns/Play response contracts. The audit uses the local reviewed source inventory; it does not admit additional rule implementations.

| Area | Gap in initial U1 | Correction and evidence |
|---|---|---|
| Cross-phase preview audit | Only dependencies of the currently selected topic could be read, with no links back to related topics. | Add the global dependency selector with origin, due timing, checks, example, citations, source pages, pending integration and related phase/topic buttons. `AuditRetainsEveryDependencyLinkAndNavigatesAcrossPhases` verifies all 38 chains and 191 links and actual cross-phase selection. |
| Browsing continuity | Returning to a phase lost the selected topic. Reminder count combined all phases while appearing beside one phase's option count. | Remember selections by phase within the current component scope and count reminders for the displayed phase. `PhaseSelectionAndRemindersSurviveBrowsingButDoNotLeakAcrossPhases` verifies both directions. Scope changes still clear private state. |
| Glossary and topic context | Only the selected topic's terms were visible; its authored kind was omitted. | Restore the phase glossary from the supplied snapshot and display the topic kind. Acronym search links remain available. |
| Attention | Checklist rows presented all options with the same pending label even when the backend supplied a required or optional decision. | Display distinct supplied attention labels beside retained pending reminders. No applicability is inferred from prose, consideration or source citations. |
| Work evidence | Working, Completed, Cancelled, CompletionDue, SupplyDepleted and effect kind were omitted, leaving completed/cancelled/in-progress work ambiguous. | Render the supplied flags and effect kind separately from effect status and Pending rule automation. `EvidenceShowsUnknownsOpeningPrerequisitesAndWorkLifecycleWithoutPromotingRules` verifies cancellation/depletion and no command creation. |
| Decision and history evidence | Opening-review prerequisites, earliest deadline, first observed Spotter timing, current-phase flags and spent grenade attempts were omitted; nullable booleans rendered blank. | Display the supplied evidence, not inferred permission. Nullable firing/Spotter observations explicitly say Unknown. Missing opening review explicitly says not supplied. Tests exercise all of these distinctions. |
| Integration limits | Profile bounds and the general coverage note were not displayed. | Add a read-only support disclosure listing supplied scopes and their pending remainders. No profile permissions become UI authorization. |
| Refresh and stale results | Refresh appeared only after failure, although stale route recovery asks the user to refresh. | Add an always-present refresh control, disabled during reads or host actions. Phase changes also invalidate the loader input. Existing generation protection rejects late reads; old content is hidden while loading and cleared on failure. `ExplicitRefreshHidesOldGuidanceAndRejectsLateSameScopeReads` checks a manual refresh overlapping a newer revision. |
| Action identity | The panel matched only opaque tokens. The host still rejected invalid actions, but malformed mixed data could display a misleading request button. | Match topic, subject, owner, profile, action ID and live actual/browsed phase as well as token. Four malformed-response cases suppress the request without changing required/pending explanations. The backend remains the execution authority. |

## Preserved boundaries

All eight phases and 538 topics remain available, including pending rules, missing-source and interpretation-open records. Historical inputs remain read-only. The new dependency links are navigation only. The reference guide never invokes Play. Live topic actions still prepare through the current Studio perspective and use existing proposal/confirmation. Phase close remains the existing governed action; private marks never resolve obligations or authorize closure.

Selections and consideration remain temporary, scoped preferences. There is no browser storage or shared-game persistence. Displaying adjudicated effects does not promote RuleAutomation from Pending. No Rules, Units, Play library or ASL.Turns source, profile version, inventory count or event schema changes are required.

The preview's compact timing-group selector is represented by the existing expandable groups plus search and Show all options; this keeps every option available without a second implicit filter. Its selected-term control is represented by inline definitions and the full phase glossary. The preview's host widget persistence is intentionally not copied into Studio: the accepted U1 scope uses transient private preferences. Existing reference/source content remains authoritative; preview prose does not become an executor.

## Verification

30 focused MapStudio component, page, workspace and navigation tests pass. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). Browser inspection confirms readable attention rows and the expanded cross-phase audit with its source evidence and related topic controls. No backend library source changed.
