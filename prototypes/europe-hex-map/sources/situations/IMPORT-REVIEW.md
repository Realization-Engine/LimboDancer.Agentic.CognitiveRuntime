# Western Front Situation import review

Imported batch: PL08, PL14 and PL16. Existing PL04 remains the compatibility baseline. These are deployment and inspection packages at the same prototype level as St. Lo, not completed combat scenarios.

## Source evidence

The source cards are in `1025016933-Avalon-Hill-Panzer-Leader.pdf` under the local archive's `PanzerLeader/Situations` folder. PL08 is the lower card on PDF page 41, PL14 on page 44, and PL16 on page 45. Each package records the source path, hash, page, printed date, roster, victory clauses and special rules. Cropped source cards are available within the UI.

Counter images are cropped from archived Imaginative Strategist Panzer Leader American sheets and Panzer Leader/PanzerBlitz German sheets. The imported German artwork uses matching numeric factors and weapon types where the corresponding PL sheet omits a required type. Every counter type records its source sheet/hash, printed counter identifier and crop rectangle. A PB artwork source does not change the Situation's PL rules. The importer contains the measured crop coordinates; original source PDFs remain unchanged.

| Source | US | German | Types | Setup order | First mover |
|---|---:|---:|---:|---|---|
| PL08 | 30 | 15 | 10 | German, Allied | Allied |
| PL14 | 24 | 46 | 18 | Allied, German | German |
| PL16 | 27 | 24 | 13 | German, Allied | Allied |

## Map interpretation

Boards A and C reuse the St. Lo transcription. Board D adds 346 source-aligned hex fragments, with boundary fragments excluded from deployment, interpreted terrain/elevation categories, town membership, road and stream centerlines, one bridge reference, town artwork and place labels. `board-D-review.json` retains the manual terrain lists and line control points. Generated illustrations are deterministic.

Town membership was checked against the scan: Einkel D3; Merden C7/D8; Nece S3/T3/T4/T5; Artain CC5/CC6/DD6. These are source-sheet coordinates, before display rotation. D2 is marsh, not part of Einkel. Roads and streams are manually traced approximations, not surveyed geometry or certified movement/LOS rules.

Marieulles displays A above D; Bulge displays C above D; Bastogne displays D alone. The upper A/C board is rotated 90 degrees clockwise from its archived image; D is rotated 270 degrees clockwise. One compass rose appears on the first board. New stacked layouts do not inherit the St. Lo A/C seam-woods treatment. Cross-board movement and seam topology remain admission blockers.

## Enforced deployment

- **Marieulles:** Germans in Grancelles on A; Allies on D. Ten turns and the printed timing/casualty victory bands are displayed, not evaluated. Intermediate-turn precedence requires an explicit reviewed interpretation.
- **Bulge:** Allied Group A within two hexes of Wiln, Group B in St. Athan on C; Germans east of row P on D. Group membership is retained per instance even when both groups share a counter type. Bridge-demolition permission, mandatory entry onto C by turn two, non-return to D, Allied confinement to C and the 15-combat-unit exit objective are recorded as source rules. Only initial setup restrictions are enforced.
- **Bastogne:** Germans on road hexes east of the north-south stream, excluding Artain; Allies on the road west of Artain. The two Allied mortars are assigned to the two trucks. A carrier's placement also places its passenger, and saved plans reject separated pairs. This implements the initial loaded condition, not a general transport/unloading system. The two-town-hex control objective remains unevaluated.

All permitted zones are explicit per-instance hex-reference lists. Setup coordinates are checked against those lists, including after reloading saves. Boundary fragments are excluded consistently with St. Lo. Stacking limits and source combat legality are not certified by the setup planner.

## Campaign and decision-cascade use

Lorraine assigns Marieulles local number 01. Ardennes assigns Bulge local number 01 and Bastogne local number 02. Stable package IDs, source Situation numbers and campaign-local display numbers are separate fields. Dates are printed game dates, not independently reconstructed operational timelines. New collections have no claimed live headquarters snapshot or geographic unit positions.

These cards supply three different future decision contracts: attack tempo and losses, engineers/crossing denial and deadlines, and defensive transport/reserve allocation. Importing them does not make them sequential encounters or permit units to transfer between their rosters. Supply budgets, leadership choices, mission eligibility and force continuity need authored campaign adapters and ledger validation before they can constrain these Situations.

ASL exports remain blocked drafts with the selected Situation's date and persistent parent counter IDs. They require dated force decomposition, terrain compilation, enforceable Scenario Card objectives and ASL engine admission. A blank draft is not an executable Scenario Card.

## Validation and remaining review

The builder checks schemas, references, registry membership, setup-zone coverage and transport identities. Automated tests cover source roster totals, image availability, legal/illegal setup, setup order, loaded pairs, launch, independent save keys, date propagation and shared UI handlers. St. Lo regression tests cover zoom/panning, placement, counts, launch/resume and palette application.

Live browser verification completed on 8 October 2026 using the local-only HTTP server. Tested campaign selection and return navigation, all three imported card/setup screens, generated/source artwork switching, enlarge/fit controls, Marieulles Grancelles highlights, and distinct Bulge Group A/B zones (24 and 4 highlighted hexes). Bastogne was deployed through UI clicks: invalid setup was rejected, both mortars were placed with their trucks, all 51 counters enabled launch, the game view removed setup controls, and the saved game resumed after switching campaign collections. The test uses deliberately concentrated placements to exercise the UI, not a rules-certified tactical deployment. Wheel/pan gestures retain automated regression coverage but were not rechecked in this live pass.

Browser testing confirms those presentation/workflow paths, not historical terrain or combat correctness. Rules/terrain certification remains necessary before combat admission; the packages retain explicit blockers rather than reporting engine readiness.


## Second batch source review, 8 October 2026

Original scan upper halves: PDF 44 (PL13), 45 (PL15), 46 (PL17). Source-card images and per-sheet hashes accompany each package. Board images and interpreted hex geometry reuse the reviewed A/C/D packages. New counter crops were visually checked against the source factors and sheets. In particular, the US Scout crop is 1B01 (not the neighboring engineer), the Bren carrier is Commonwealth 3 / 4D01, the Grille is PB German 4 / 7A01, and the Panther W is PL German 2 / 9O01. Celles uses the W's 13/A/12, defense 11, movement 10 throughout. Original M4/105 defense is 9, matching the existing crop.

- PL13: 88 American and 90 German counters, 39 types, 12 turns, German first; Allied setup first. C is 90 degrees clockwise, D 270, arranged vertically. Eastern boundary distance is measured in source hex steps after accounting for each board's orientation. US deployment spans both boards; German FBB and 18VG have separate board restrictions.
- PL15: 45 American units plus one roadblock, 70 German units, 26 types, 10 turns, German first; Allied setup first. C and D are both 180 degrees from archived sheet orientation, arranged side by side. Group A uses source rows before Q (display south); Group B uses rows after Q (display north). Original PDF page 23, printed page 22, identifies the X as a Block. No matched IS block crop was found; the authored X is an explicit artwork exception. It has null factors, is included in deployment counts, and is excluded from combat-draft selection.
- PL17: 58 American, 22 British and 72 German counters, 39 types, 12 turns, Allied first; German setup first. A/C/D are 270/90/270 degrees, stacked vertically. German A is within one hex of St. Athan and B within two of Wiln. US units deploy west of A's central stream; British units west of D's transverse stream. Zones use the traced source polyline, not a rectangular guess. Stream hexes and boundary fragments are excluded pending rules certification.

Per-instance `setupGroup`, per-type `nationality`, explicit setup hex lists, full printed victory clauses and special rules are retained. `setupBoards` is a default view hint; actual legality comes from the card's allowed boards and each instance's permitted hexes. Pure source-clause objectives are permitted only while admission remains reference-only. Overlapping victory grades and kill/control precedence in PL13/17 require review before scoring implementation.

The shared automated suite now completes both sides and launches all seven packages, reloads their plans, checks independent keys, validates both PL13 boards, PL15 row-Q separation and marker semantics, PL17 contingent board restrictions and Panther factors, portrait rendering, British palette use, and all four coordinate-transform round trips. Existing St. Lo tests and 15 schema/encoding tests also pass.


### Second-batch live browser acceptance

Tested against the existing `http://127.0.0.1:8080/index.html` server on 8 October 2026:

- All five Ardennes entries appeared under one campaign, with stable local numbers and correct printed dates. All three new card/setup screens opened.
- PL13: placed an American rifle on D even though C is its initial view; highlights covered both boards. An eastern excluded hex was rejected without moving the unit.
- PL15: placed the block on a rotated C hex, toggled source/generated artwork, then placed all 116 pieces through browser controls. Completion stayed disabled until each side was complete. Launched the larger game map, returned to the collection, reloaded the entire page and resumed the saved game with 116/116 placements.
- PL17: placed all 72 German pieces through the UI, including instances of common types split between Groups A/B. Completed German setup. Placed one US armored infantry on A, one British Scout and one British Sherman on D. Inspected the rendered nationality attributes and rejected a British placement on A. Enlarge/Fit all boards worked. After reload the independent save retained 75/152 placements and Allied setup.
- Browser console reported no errors. Setup/game regression tests cover wheel and drag mechanics; those physical gestures were not repeated in this acceptance pass.

The browser tests intentionally stack units to exercise selection and completion. They are test deployments, not tactically sensible or stacking-rule-certified setups. Test plans remain on the localhost origin. Existing file-URL saves were not cleared. Formation combat, obstacle effects, legal stacking and scoring remain outside this prototype's implemented scope.

The final portrait-map pass found and fixed a scroll reset during unit inspection at base zoom. Browser recheck retained a 2212.5-pixel vertical position across selection; Enlarge followed by Fit reset it to zero. A regression assertion now covers that distinction.


## Third batch source review, 8 October 2026

Visually transcribed PL18 from PDF 46 lower half, PL19 from PDF 47 upper half, and PL20 from PDF 47 lower half. Nine new Imaginative Strategist crops were visually checked: M18, M36, M24, 90 mm, 170 mm, security infantry, L-5, P-47 bombs and P-47 rockets. The German X roadblock is explicitly authored, not a claimed artwork match.

- PL18: 63 Allied instances (52 ground and 11 aircraft), 56 German; 32 types; 12 turns, German first, Allied setup first. Display D/C/A at source-image rotations 180/0/0. Aircraft enter no earlier than turn four. Two fighter-bomber flights of five, at most one flight on-board, no mixed flights or repeat runs. The observer is separately listed.
- PL19: 113 Allied instances (107 ground and six aircraft), 79 German; 39 types; 15 turns, Allied first, German setup first. Display A/C/D at 180/180/0. Printed December 31 through January 1, 1945 is represented as 1944-12-31 through 1945-01-01. US M4/76 artwork represents all nine US tanks; the printed British-counter substitution does not create British units. Ratio scoring overlaps and zero-denominator cases remain unresolved.
- PL20: 14 Allied, 11 German including one block; 14 types; eight turns, Allied first, German setup first. D remains in source-image orientation. North-bank or Artain is the interpreted German union, because Artain lies south of the transverse watercourse; the bridge block is a separate exception fixed to D-BB-6. Allied hexes are south of that watercourse and at least three hexes from Artain. The inherited stream geometry is the card's river reference; this is not a certified river/movement implementation.

Aircraft are typed assets with null ground factors and empty ground deployment lists. Every aircraft belongs to exactly one support group; availability is checked against its instance and turn limit. Ground pieces must have nonempty setup lists. Air assets cannot be placed on the ground map or allocated as ground ASL participants. Completing all ground pieces enables inspection-mode launch. This does not execute air schedules.

All ten packages build and pass the shared deployment/launch/save regression suite. Fifteen schema/encoding tests and the existing St. Lo UI regression suite pass. Live HTTP browser checks opened the three new cards, verified the Bastogne flight roster and Patton date window, and placed a Patton SMG. Remagen's 25 pieces were deployed via browser controls, including its block on D-BB-6, then launched in the game view. No browser console errors were reported. Concentrated test placements exercise UI completion and are not certified legal stacking. Air, combat, bridge effects and scoring remain unimplemented.

PL18 prints the 326th Volksgrenadier Division in its briefing. That attribution is preserved as printed, without silently substituting the historically associated 26th; independent historical reconciliation remains pending.


## Fourth batch source review, 8 October 2026

PL09/10 are the upper/lower halves of original PDF page 42; PL11 is page 43 upper half. Counts, setup order, printed objectives and special rules were transcribed visually.

- PL09 Nijmegen: 36 Allied pieces, 37 German, 19 types; 10 turns, Allied first, German setup first. D/A displayed at 180/180 degrees from archived orientation. German north and Allied south of the traced major stream, excluding stream and boundary fragments. One un-emplaced tank-bridge asset is separate from its Valentine carrier. Bridge loading/construction/control are not executable.
- PL10 Arnhem: 16 British, 31 German, 15 types; 10 turns, German first, Allied setup first. A at 180 degrees. British setup uses Grancelles town hexes; German minimum three-hex separation is evaluated against actual Allied placement, in highlights, placement and save validation. Printed victory timing clauses are preserved pending precedence review.
- PL11 Anticlimax: 35 Belgian, 30 American, 53 German, 27 types; 12 turns, Allied first, German setup first. A/D/C arranged vertically at 270/90/90 degrees. Source stream traces determine west/east zones after rotation. Belgians start on A, Americans on D; the later C/D movement permission is not confused with setup. Both contingents' release after a 3:1 condition is retained as text and a capability blocker.

Counter review caught and corrected crop positions for the Commonwealth engineer (sheet 1 / 1A01, 450,360) and Panzer III (German 2 / 9M01, 450,180). All dimensions are source PDF points. New Belgian identities reuse corresponding Commonwealth crops with Allied Minor color. Arnhem's 81 mm profile reuses the numerically matching American IS crop with British coloring. Nijmegen's printed Valentine bridge carrier is 1/I/2, defense 10, movement 6; the source-card image is retained because no exact IS variant was found. Its source crop uses fractional PDF points. The IS tank bridge's 32 is capacity, not an invented defense factor; bridge-equipment factors are null.

The schema now supports minimum-separation setup rules and bridge equipment. Builder validation requires the referenced opponent to deploy first. Aircraft and bridge equipment remain excluded from ground-combat drafts; bridge equipment still counts toward ground setup completion. Palette/roster headings distinguish British-only, US/British and Belgian/US forces.


### Fourth-batch acceptance

All thirteen packages validated and passed deployment, launch, save and shared view tests. Fifteen schema/encoding tests and the St. Lo regression suite passed. In the live HTTP browser, all three new card/setup views opened. Arnhem's 47 pieces were placed through controls, an attempted German placement on the British position was rejected, and the game map launched. Anticlimax's first 19 German pieces were placed and the Belgian artwork/palette was inspected; the longer automation batch timed out, so full Anticlimax completion is covered by the automated suite rather than claimed as a browser pass. Nijmegen's final Valentine label and separate tank-bridge equipment disclosure were checked after reload. No console errors were reported. These are concentrated UI-test deployments, not stacking-rule-certified tactical setups.


## Fifth batch source review, 8 October 2026

Original PDF 40 upper/lower halves supply PL05/06; PDF 41 upper supplies PL07; PDF 43 lower supplies PL12. The four card images were visually inspected. New IS crops were visually verified: Commonwealth 4 Sexton 6A01 and rocket-armed Typhoon; Commonwealth 1 machine gun 1D01 and 107 mm mortar L01. Canadian identities reuse the corresponding Commonwealth equipment with explicit Canadian labels. Goodwood's Cromwell (9/A/8, defense 8, movement 7) and Maultier (50/(H)/12, defense 4, movement 10) retain original-card crops as explicit exceptions to differing IS profiles.

- PL05: 68 Allied including eight Typhoons, 54 German, 25 types, ten turns, Allied first, German setup first. B/A/C are 90/90/90 degrees, stacked vertically. British armored types (Sexton, Achilles, Cromwell, Sherman, Bren) restricted to A. North/south stream zones use interpreted main source traces; German A excludes Grancelles. Aircraft have no ground setup. A typed maximum of five concurrent aircraft differs from the existing fighter-flight limit.
- PL06: 44 Canadian, 19 German including one block, 17 types, ten turns, Allied first, German setup first. D at 180 degrees. German source rows after H, Canadian rows before I. The block is not fixed to the Remagen bridge. The printed 7 September 1944 Reichswald attribution is flagged as historically unverified and isolated in a source-study collection.
- PL07: 41 American, 23 German, 17 types, ten turns, Allied first, German setup first. D/A at 270/90 degrees, horizontally. Both forces start on D, on opposite sides of H, excluding that row. The loss-conditioned German victory and explicit draw are retained.
- PL12: 41 American, 51 German, 26 types, ten turns, German first, Allied setup first. A/D/C at 0 degrees, horizontally. Americans on D, Germans on C. Four exits on A are typed references, not active movement exits. Overlapping exit/casualty victory grades need precedence review.

Board B is newly transcribed with 346 hex records. Source trace points and terrain review lists accompany the package. Coastline and settlement artwork are source-guided illustrations; slope adjacency is a first-pass interpretation. Water and coastal fragments are excluded from setup. All board terrain and joins remain reference-only. The previous A/C woods seam assumption is now restricted to its actual A90/C270 adjacency, preventing unrelated board arrangements from receiving that patch.

The source registry retains all prior local numbers. Goodwood is Normandy 02, Nancy Lorraine 02, and Reichswald/Saar each 01 in separate collections. Normandy's browsing window extends through 18 July without changing the legacy campaign ID or pretending the June command snapshot applies to July.


### Fifth-batch acceptance

All seventeen packages passed builder validation and the shared roster, deployment, launch, save and UI-handler regression suite. The existing St. Lo regression suite and all fifteen schema/encoding tests passed. The campaign-numbering fixture now deliberately copies a single Situation when testing another campaign, rather than accidentally duplicating Goodwood's identity from the expanded Normandy collection.

Live browser checks used the existing localhost server:

- Goodwood: source review and three-board setup opened, four Board B settlement labels and coastal water rendered, and the eight-aircraft support disclosure showed the five-aircraft cap. Selecting a German rifle highlighted only A/C; an attempted placement on B was rejected. The page was left on Goodwood's new coastal board for review.
- Reichswald: placed all 19 German and 44 Canadian pieces using roster/map controls, completed both sides, and launched the game view. A page reload retained the completed independent save. Canadian labels and palette were visible. The block could deploy within the German zone rather than being pinned to Remagen's bridge.
- Nancy: Lorraine retained Marieulles 01 and added Nancy 02. D/A appeared in their rotated landscape layout. A German SMG placement succeeded; a placement on excluded row H was rejected.
- The Saar: A/D/C appeared in portrait orientation. Allied-first setup highlighted only D; an American rifle placement succeeded.

No browser console errors were reported. Full completion of Goodwood, Nancy and The Saar is covered by automated state/view tests, not claimed as a full manual browser deployment. Browser test placements are deliberately concentrated and are not certified legal stacking. Test deployments remain on the localhost origin; existing file-URL saves were not cleared. Turn execution, combat, source scoring, supply continuity and ASL admission remain unimplemented.
