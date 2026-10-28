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
