# Scenario A1 Backlog Pass 28b: The Rest of Play

**Status:** Reviewed and fixed. Pass 28b of the [ASL Card Play and Map Studio Redesign Plan](<ASL Card Play and Map Studio Redesign Plan.md>); its [design](<ASL Unit Backlog Pass 28b Design.md>).

**Date:** 2026-10-02

**Commits:** the build (`d33c5d5`) and the review fixes (`d9ff080`) on `feature/asl-backlog-pass-28b`.

## The user's answers

On 2026-10-01 the user took every recommendation:

- Both backlog section 23 additions are built here.
- A09, C17, and R08 to R10 are extracted, and A03 stays inline.
- RuleHelp covers the Play page and this pass's panels.
- RuleHelp shows a summary, with the full text collapsed beneath it.

Before the pass, the user committed someone else's uncommitted work on the main checkout to main (`2aefeed`, saved games replaying with an archived catalog), and the branch started from there.

Two standing rules were added during the pass:

- My Studio runs on port 6670, never on 5178. The user first asked for 6666, but browsers refuse ports 6665 to 6669.
- A Studio instance that blocks a build is stopped without asking.

## Visual checks

Both checks ran in the Studio on port 6670, with games written as event files on a minimal user card (`p28b-minimal`) under `src/ASL/boards/`.

**First check (`p28b-visual`).** These were committed and read in the page:

- A Deploy giving one SW to each HS.
- A SW transfer, refused for a HS that had Deployed (A1.31, its sole RPh action) and accepted between r3 and r4.
- DM gained.
- Small arms fire into the fire history.
- The mortar's Spotter and leader fields, refused for range and LOS.
- The Rout panel and its obligation.
- A Failure to Rout turned into a surrender (A20.21).

RuleHelp's summaries and collapsed text lay out cleanly, and the empty toolbar is gone. The check was clean.

**Second check (`p28b-visual2`), after the fixes.** All of these showed:

- The new Deploy legend, and the record naming who took which SW.
- The SW note, with Recover disabled for a held SW.
- The DM fieldset.
- "r1 keeps DM as the RPh ends (A10.62)" after the owner kept it.
- The records, latest attempt first, with each attempt's records in order.

Screenshots timed out while the app window was hidden, so the second check was made through the DOM.

## Referee findings

| # | Finding | Response |
|---|---|---|
| 1 | High: DM kept as the RPh ends (A10.62 EXC, E1.54) was recorded as "comes under DM", since the phase change clears DM and sets it again. | Fixed: a unit that had DM when its attempt began reads "keeps DM as the RPh ends", with E1.54 at night; tested. |
| 2 | Rout summary: read as if every broken unit routs, and as if any unit in the open is eliminated; surrender left out. | Fixed with the referee's wording, which also covers Low Crawl (the table player's point). |
| 3 | SW summary: read as if a drop is mandatory, and left out "Good Order unpinned". | Fixed. |
| 4 | Ordnance summary: gave a light mortar and a LATW HE at Infantry or a vehicle. | Fixed: a light mortar on the Area Target Type; a Panzerfaust or Panzerschreck HEAT and an ATR AP at a vehicle. |
| 5 | Rally: "or by Self-Rally" suggests any unit may. | Fixed in the summary and the paragraph ("if able, A10.63"); the summary also gives the +4 under DM. |
| 6 | Deploy summary: left out the leaderless Guards and said "of its nationality", which A1.31 does not ask. | Fixed in the summary; the full paragraph is the planner's text and is unchanged. |
| 7 | Starshell: "after a Usage dr" should say a successful one. | Fixed, with the limits in the summary. |
| 8 to 10, 12, 13 | The Move, Shock, and non-OB summaries; the Failure to Rout and transfer detection; no disclosure regression (the ordnance vehicle list is tighter); the Deploy split. | Sound; no change. |
| 11 | `Open` re-implements part of the view (it ignores containers, out-of-sight setup groups, and the before-play top-counter rule) and errs on hiding when a fire both breaks and reveals a unit. | Left: it never discloses more than the view. Backlog section 43 holds deciding it by the view at each revision. |
| 13 (planner) | Deployment does not reassign a Gun the squad mans. | Backlog section 43. |

## Table player findings

| # | Finding | Response |
|---|---|---|
| 1 | Rout summary wrong at the table. | Fixed (referee 2). |
| 2 | Starshell: the hidden text decides the method and the target. | Fixed: each method names its limit, the Usage numbers are in the summary, and the target is disabled for the own hex. |
| 3 | Deploy: "Second HS takes" half the story, and the record does not say who took what. | Fixed: the legend reads "SW for the second HS (the rest stay with the first)", and the record names each HS's SW. |
| 4 | Records reversed event by event put the effect above its cause. | Fixed: the latest attempt first, each attempt in order. |
| 5 | SW: four buttons, no hint why one is grey; Recover enabled for a held SW. | Fixed: a note under the buttons; Recover disabled while held. |
| 6 | Rally summary omits the DM DRM and the Self-Rally limits. | Fixed (referee 5). |
| 7 | DM retention: nothing says when the checks apply. | Fixed: "sent when the phase is advanced". |
| 8 | The records' heading covers more than SW. | Fixed: "Rally, Rout, and other actions". |
| 9 to 14 | Deploy and Recombine leaders default to "none (Guards)"; no turn or phase in the records; DM removed and DM's cause not recorded; Failure to Rout without its reason; the RPh heading and the "Gun" label; `onchange` on the route and target fields. | Backlog section 43, each for pass 28c or later. |
| Resets | `ordnanceTurn` survived a game or phase change; the method's name misled. | Fixed: reset, and renamed `ResetPanelChoices`. |
| Resets | Choosing a view on the picker resets drafts before the hand-over is confirmed. | Kept: ruling R23.2's hand-over clears what the last view prepared as soon as another view is chosen. Backlog section 43 holds reconsidering it. |

## UI and Blazor findings

| # | Finding | Response |
|---|---|---|
| 1 | A weapon change kept a hidden vehicle and Intensive Fire, and both were sent. | Fixed: a weapon change clears both, and a value the panel hides for the weapon is never sent. A focused page test is in backlog section 43. |
| 2 | The Gun facing was never reset. | Fixed (table player). |
| 3 | The resets were split across three places, with duplicates. | Fixed: one method. |
| 4 | Several lists read the full state or skipped `SeenOnly` (Passengers). | Fixed: every list passes through `SeenOnly`, and the firing side's weapons through the view. |
| 5 | Every disclosure was named "The rule in full". | Fixed: the disclosure is described by its summary. |
| 6 | `DescribedBy="move-help"` pointed at hidden text. | Fixed: `move-help-summary`. |
| 7 | An unknown SW kind proposed a dismantle. | Fixed: each kind maps explicitly, and anything else proposes nothing. |
| 8 | `FireText` need not be public. | Fixed: internal. |
| 9 | Duplicated DRM formatting; a literal "own-hex". | The breakdown and the literal fixed; the page's other three copies are in backlog section 43. |
| 10 | DM retention grouped under a span. | Fixed: a fieldset with a legend, keeping its id. |
| 11 | Buttons enabled on any id; no ready notes. | Ready only for an offered choice; the ready-note pattern is in backlog section 43 (pass 28c's hardening). |
| 12 | The `Target` slot is nullable though required, and can take any markup. | Made non-nullable; a parameter record instead of the slot is in backlog section 43. |
| 13 | The Rally panel owned the heading over Repair too. | Fixed: the heading is the page's. |

## Components

The pass had 20 candidates. Nineteen were extracted; A03 `RoutObligations` stays inline in A02 because it is a single `<ul>` with no state of its own. Every candidate of sections 16.10 to 16.13 and 16.15 is now extracted, except R11 `PlayMapPanel`, which belongs to pass 28c.

## Tests

| Project | Tests |
|---|---|
| MapStudio | 252, including `PlayPagePass28bTests` (4) and `RallyAndRecordComponentTests` (6) |

The full local suite and the Docker check are reported in the time log and the merge.
