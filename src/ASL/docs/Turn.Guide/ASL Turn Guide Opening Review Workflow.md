# Authorized opening review in the live guide

Date: 2026-10-27

This completes the previously external review step for the admitted Placed DC route. The guide now lets an authorized adjudicator propose and confirm AFPh opening evidence, then hand the screen back to the owning side for the existing governed detonation and phase-close actions. No opening task is executed by the form, and no pending rule becomes automated.

## Player flow

1. Open the live guide in AFPh and select **Resolve every operably Placed DC**. The owning side sees its timing assessment and how to obtain authorized review.
2. Hand the screen to the adjudicator. A server-authorized reviewer sees a separate form for each currently reviewable charge. Without authorization, the guide explains that access is disabled. Historical views and other phases never offer review submission.
3. Record Drift (5.11B), Dispersed WP (5.12A), Gusts (5.13A), and glider contents (5.21A) independently. Each starts **Unknown**. **Resolved** means the reviewer has checked the task and its consequences, including any optional choice declined; **Not applicable** is an explicit assessment, never the default.
4. Enter the rule basis and reason. Propose the review. The existing confirmation panel displays the charge, owner, all four assessments and the reason before committing anything. Cancel writes nothing. An intervening journal event makes the proposal stale.
5. Confirm the evidence, then return to the owning side. Unknown evidence is durable but does not open the guided attack route. Complete evidence allows the existing command check, subject to the ordinary game rules, permission and separate confirmation.
6. Resolve the charge, then separately review and confirm phase close. Considered checkboxes and opening review never resolve the DC obligation themselves.

A review applies to one charge at one game state. Every subsequent event invalidates its currency. With multiple charges, review and resolve them one at a time, rechecking the remaining opening evidence as needed. Form drafts reset when the offer changes; stale values are not silently reused. Missing legacy placement ownership or phase occurrence is not repaired or guessed by this form.

## Headless preparation and authorization

`GamePlanner` adds `OpeningReviews` to `TurnHostGuide`: opaque state-bound offers with disclosed subject and owner only. Offers are limited to live adjudicator reads in the actual and browsed AFPh, profiles admitting the DC history, and facts accepted by the existing Rules-owned match predicate. `PrepareOpeningReview` revalidates the complete offer against the current store and constructs the typed review with server-owned placement, phase-occurrence and revision identities. Forged, stale, foreign-side, missing-source and malformed evidence cannot become a prepared command.

The form raises an intent. Play uses the same proposal, cancellation, confirmation, revision recheck and audit path as other actions. The existing `ReviewPlacedDcWindow` gate retains `WindowAdjudicationPermission`; an offer is not a credential. No new event schema, timing rule or ASL.Turns capability is introduced.

Studio receives a reviewer principal only from trusted server configuration. `LivePlay.CanReviewOpening` requires an authenticated principal for the Studio tenant with the review permission. `PrincipalFor` selects it only for the review action in the adjudicator view. Ordinary setup/play principals retain their existing permissions; the reviewer does not obtain play or work-adjudication permission through this feature. Tests exercise missing permission, wrong tenant, unauthenticated principal, side view and unrelated actions.

## Deployment

`Play:OpeningReviewEnabled` defaults to false. No checked-in application setting enables it. For an explicitly authorized local shared-table installation, the host operator can enable this setting at startup (environment variable `Play__OpeningReviewEnabled=true`). This supplies the narrowly scoped `studio-opening-reviewer` identity. It applies to the local Studio tenant and becomes available through the adjudicator handover; selecting that view alone cannot grant access when the option is disabled.

This is the existing local shared-table trust model, not multi-user authentication. A network deployment must supply authenticated role mapping and must not use a browser-selected adjudicator name as authority. The UI does not contain a switch that grants this permission. Enabling access on the user's running instance is a separate host configuration action.

## Acceptance

The Tractor Works success test now performs opening review entirely through the Play UI using an explicitly supplied test reviewer, followed by owning-side detonation and phase close. A second UI path checks durable Unknown evidence, cancellation, a stale review confirmation and view-change cleanup. Existing tests still check stale detonation after another review, missing review, denied ordinary-principal submissions, and complete pending-topic visibility.

Headless tests cover offer disclosure, historical and wrong-phase suppression, malformed or forged offers, missing reason, invalid evidence values, stale preparation, ordinary-principal denial, authorized confirmation, and Unknown evidence retaining the closed attack route. Component tests cover default Unknown selections, reason requirements, busy state, draft reset and permission/context restrictions.

All 538 topics, 38 dependency chains, 191 links and nine B5-D packages remain available. The all-Passes target is unchanged. Verification: 43 focused MapStudio tests pass, including all three real-board scenario cases (not skipped). All 45 focused Play tests pass, including review preparation, timing, replay and disclosure acceptance. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). No ASL.Turns or Rules source changed.

Live browser verification after explicit user approval confirms the reviewer form on localhost:5178. The guide uses its containing panel width to stack checklist and detail content, so review fields use the available width inside Play even on a wide screen. The enabled reviewer is scoped to the current local server process; checked-in defaults remain disabled.

## Full live browser acceptance

The user-authorized local reviewer completed the route in the browser on 2026-10-27 (America/New_York), using `turn-guide-opening-demo` on localhost:5178. This isolated Tractor Works continuation began at revision 142 in German AFPh, with `g-dc-3` operably Placed in unoccupied BB3. No review was seeded in this continuation.

| Browser action | Observed result |
|---|---|
| Propose four Unknown assessments with a reason, then cancel | Confirmation displays all four values; cancellation returns to Play without a journal event. |
| Set all four opening tasks to Not applicable with a scenario-specific ASOP reason; propose and confirm as adjudicator | Exactly one owner-visible review event at revision 143. The charge remains due; review does not roll dice or detonate it. |
| Hand over to German and request phase close | The gate refuses because the Placed DC is still due, despite complete opening evidence. |
| Request the guided DC check, then explicitly confirm | 30 FP detonation; system dice 1,6, fire resolution and removal at revisions 144-146. The obligation becomes Resolved and the offered route disappears. |
| Review and separately confirm phase close | Revision 147 begins German Rout. The guide follows Rout and retains pending-rule labels and the resolved obligation. |

BB3 has no targets, so the 4MC result produces no casualty effects. This accepts opening-review, decision, obligation and phase-close integration; it does not claim browser coverage of occupied-target consequences, malfunction, or every ASL opening rule. Automated backend tests retain those separate responsibilities.

The browser exposed a presentation gap: confirmation displayed internal enum spelling (`NotApplicable`) and abbreviated the reviewed tasks. `TurnOpeningReviewSummary` now lists the four full task names and ASOP steps with readable evidence labels. Its evidence-only wording also remains accurate on the committed receipt. It renders supplied evidence and performs no rule evaluation.

Regression acceptance additionally refuses phase close after a complete UI review and reopens the game after Rout to verify resolved obligations, pending disclosure and absence of obsolete review/attack/confirmation controls. The mixed-evidence summary test prevents independent task assessments from being conflated. All 538 topics, 38 chains, 191 links and nine B5-D packages remain unchanged.

Verification for this follow-up: 44 focused MapStudio tests pass, with no skips; all three real-board scenarios also pass after strengthening the no-write-on-reopen assertion. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered (100%, no exclusions). Restarting the local server and reloading the browser preserves German Rout, the resolved DC obligation and pending disclosures, with no obsolete review, detonation or confirmation controls. No ASL.Turns, Rules, permission defaults or journal contracts changed.
