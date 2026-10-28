# U1 live scenario acceptance

Date: 2026-10-27

## Scenario and boundary

`TurnGuideScenarioTests` continues the recorded **The Tractor Works** setup (`p31c-tw.game.json`, revision 133) under a separate game name in a temporary store. It uses the real VASL board 01, its pinned terrain version, the published unit catalog, the real Play page, LivePlay, GamePlanner, governed proposal/confirmation, dice and file journal. It does not substitute guide snapshots or manually inject new game events.

The older setup predates mandatory Sniper placement. The fixture completes that step using today's legal placement candidates and governed setup command. German engineer `g-engineer-3`, carrying `g-dc-3`, Places the charge from BB4 ground level into adjacent BB3 ground level. The chosen engineer has the required MF; the engineer carrying both a FT and DC does not. The scenario branch is a test continuation, not a reproduction of the original recorded play or an assertion that every scenario SSR is integrated.

The opening review is deliberately a separate authorized host operation. Studio's ordinary principal cannot submit it. The test first proves that denial, then proposes and confirms the review as a referee holding `WindowAdjudicationPermission`. At the time of this acceptance there was no Studio review-authoring screen. The later [opening-review workflow](ASL%20Turn%20Guide%20Opening%20Review%20Workflow.md) adds it behind explicit host authorization. No new permission is granted to the Studio user. Drift, WP, Gusts and gliders are recorded as not applicable for this branch; the interruption case replaces WP evidence with Unknown.

## Acceptance path

1. Replay the original setup and complete the current Sniper setup requirement.
2. Open the actual Play page as German. Browse all eight phases and all 538 topics without changing the journal; retain pending-rule disclosures in every phase.
3. Close Rally and Prep Fire through the guide and the existing confirmation panel. Place the DC through the movement UI, hand over to Russian for the defensive-fire pass, and return to German to end the move. Close Movement and hand over to Russian to close Defensive Fire.
4. In Advancing Fire, keep the missing-review explanation visible and offer no guided DC action. Attempt phase close: the governed action refuses while the DC obligation is due, without writing.
5. Submit the separately authorized opening review. Refresh the guide and request its DC command check. Proposal changes neither the journal nor the dice queue. Confirm detonation through the existing review panel.
6. Observe that the placed charge is removed, its obligation is Resolved, the route disappears, pending rules remain visible, and the journal records the guided decision identity. Confirm phase close into Rout and replay the resulting journal without errors.
7. In a separate branch, replace the opening evidence after the DC proposal. Confirm must be Stale, with no journal write and the charge still present. Refresh shows Unknown, removes the route, and retains the phase-close block and pending rules.

## Gaps fixed

| Gap | Correction |
|---|---|
| The guide could prepare a route using a newly committed external review, while the Play panel stamped its proposal with an older rendered revision. Confirmation then falsely reported Stale. | The panel retains the `expectedRevision` actually submitted to the gate. The existing confirmation re-read and backend checks still reject intervening changes. Both paths run through the real scenario test. |
| The live guide remained on the phase just closed, requiring manual phase navigation during ordinary play. | Follow a changed actual phase when browsing the previously actual phase. Preserve a deliberately browsed different phase. A focused component regression checks both behaviors. |
| U1 had component and basic phase-close tests, but no complete real-scenario DC decision path through Play. | Add success and interruption scenarios using real board data, real components, governed writes and persisted replay. |

## Reproduction

Set `AslMaps__VaslRoot` to a local VASL checkout containing the pinned board 01 and run:

```powershell
dotnet test src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/LimboDancer.Domains.Asl.MapStudio.Tests.csproj --filter FullyQualifiedName~TurnGuideScenarioTests
./src/ASL/tests/ASL.Turns.Tests/Test-Coverage.ps1
```

The two real-board tests explicitly skip when VASL is unavailable; ordinary component and Play-page regressions continue to run without it. A changed board version causes failure rather than silently substituting terrain.

For a browser rehearsal, optionally set `ASL_TURN_GUIDE_BROWSER_ROOT` to a new empty directory before the successful-scenario test. It saves a separate AFPh game after authorized review and before detonation, under that directory's `units/live` tree. Existing files are not overwritten. Run Map Studio with `AslMaps:BoardsRoot` pointing there and the same VASL source. Open `games/play?game=turn-guide-tractor`, choose German, open the Turn Phase Guide and select Placed DC. The ordinary proposal and confirmation controls execute the same route. This is disposable test data, independent of user games.

## Pending rules and coverage

All 538 topics, 38 dependency chains, 191 related-topic links and nine B5-D packages remain visible. This work changes no ASL.Turns, Rules or journal contract, does not widen a capability profile, and does not promote adjudicated evidence into rule automation. The eventual all-Passes design remains the target.

33 focused MapStudio tests pass, including both real-board scenario cases (not skipped), the component regressions, existing stale-confirmation tests, one-click behavior, workspace and navigation. All 179 ASL.Turns tests pass with 821/821 lines and 466/466 branches covered: 100% of both, without exclusions.

A browser rehearsal on a separate development instance also passes: refused close with the due DC; guided proposal and explicit confirmation; resolved obligation and disappearance of the action; proposed and confirmed phase close into Rout. The persisted continuation records review at revision 143, dice/fire/removal at 144-146 and Rout at 147. The controlled IFT DR is 5,5; BB3 is unoccupied, so this checks DC lifecycle and phase-close integration, not casualty or chained consequence handling. Existing backend consequence tests remain responsible for those branches. The original record and user games are unchanged.
