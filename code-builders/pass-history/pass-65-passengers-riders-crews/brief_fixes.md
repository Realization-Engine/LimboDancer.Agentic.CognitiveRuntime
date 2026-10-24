# The fixes of pass 65's reviews

Read `brief_build.md` beside this file first; it binds a fixing agent as it bound a building one (rules in Rules, absent-safety, memory and performance, tests by method, no `cd` into the repository, no games script, no commit). The five reviews' findings are in `fixes_referee1.md` (R1), `fixes_referee2.md` (R2), `fixes_table.md` (R3), `fixes_ui.md` (R4), and `fixes_boundary.md` (R5). The fixes run in four rounds, one agent each, in this order, because they touch the same files; each round starts from the commit of the one before.

## Decisions made for the fixes (rulings at the reviews, 2026-10-24, open to the user's veto)

- **R65.31 (R1 8, R3 11): D5.311's broken crew.** A crew that breaks under Air Bursts stays inside its AFV, broken, in the Crew role. In the RtPh it must rout beneath, through 2c's rout-beneath path (as a Passenger must when its crew is gone), and the AFV is Abandoned when it does. The fire's text says "broken; it routs beneath in the RtPh (D5.311)". An older record holds no such crew.
- **R65.32 (R3 9): a squad as a Temporary crew.** A21.22's free exchange of a capturing squad for a HS is not built in this pass. The refusal says so plainly ("a squad's exchange for a HS (A21.22) is not built: Deploy it, or let a HS or crew enter"), not as a rule, and a backlog row records it.
- **R65.33 (R2 10): Scrounging's leadership.** The best Good Order leader's modifier in the Location is a drm on the Scrounging dr (D10.5's ∆, A.10), recorded on `vehicle-scrounged` (absent means none).

## Round A: the referee's faults, Passengers (R1)

All of `fixes_referee1.md`, findings 1 to 9, with R65.31 for finding 8. Findings 10 and 11 (NOTEs) also, if each is a few lines; else a backlog row each (say which).

Each fix that changes a result an older record could hold is gated on that record's pass fact (`prc`, or the fact the finding names), so older records replay as before.

## Round B: the referee's faults, Riders, Carriers, crews, captures, wrecks (R2)

All of `fixes_referee2.md`, findings 1 to 11, 13, 14, with R65.33 for finding 10. Finding 1 (MAJOR): Riders Bail Out on every turret turn, an ordnance shot, Canister, CMG fire, and a Motion attempt alike. Finding 15 is a backlog row.

## Round C: the Rules boundary, and memory and performance (R5; R2 12; R3's boundary)

- **Finding 1 (MAJOR).** The five projector arms (`Abandon`, `Recrew`, `CaptureVehicle`, `Scrounge`, `boardingAfterMove`) take a Rules projection verdict over facts, as `ScenarioA1VehicleProjection` does, so the projector applies the same rule the planner's bars do. Where the projector cannot know a fact the planner's bar reads (a fire counter earlier in the Player Turn, say), the Rules member for the projector checks what the record can show, and says so in its summary. The Blaze's id is named by one shared member, not spelled out in Units.
- **Then:**
  - findings 2 to 7, 11, and R2's 12 (each a Rules member fed by facts, the page and planner calling it);
  - findings 14 to 18 (the event-history scans per plan, `CrewInside` through the index, `KeepPassengers`, `FireAboard`);
  - finding 13's `? true : null`.
- Findings 8, 9, 12, 19, 21: a Rules comment, a backlog row, or nothing, as each says.

## Round D: the page and the record lines (R4, R3, and the two known faults)

- **R4:** findings 1 to 8 and 11, 12. Finding 9's page pre-filters go through the planner members Round C made.
- **R3:** findings 1 to 8 and 10 to 15 (record lines, wordings, the Rider check's dice, `RollWords`, the capture attempt's arithmetic line). R3's 9 by R65.32.
- **The two known faults:**
  - A unit the plan creates (2f's crew unload, 2g's Abandonment and MG Removal) is named in its proposal by its kind and definition ("its inherent 2-4-7 half-squad", "a 1-2-7 vehicle crew", "the LMG"), never by its raw id.
  - Every vehicle choice names its hex first: the vehicle CC panel's, the ordnance and the FT/MOL lists' ("[F8]: PzKpfw IIIH, a wreck").
- **R4's finding 10:** add page tests for the new controls. One bUnit test per section is enough (crew acts, Scrounging, the Rider and crew-unload checkboxes, the rout beneath, the capture attempt), each checking the hex in its labels, the arguments it sends, and its draft cleared on commit and on a view change.
- Run `IconCoverageTests.TheCoverageFileMatchesTheSources` if a button or id is added or renamed.

## For every round

- Your report lists each finding by its review and number, with fixed, a backlog row (the row's text, for me to file), or not changed and why.
- Tests by method, as `brief_build.md` says, the methods that assert each fix and the existing ones the fix changes.
- The strict build of the test projects you touch, outside the repository, before you report.
