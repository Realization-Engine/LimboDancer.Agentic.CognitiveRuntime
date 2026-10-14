
## 7. The notation and the counters: 55.1 (first part), 55.6 (increment 2a)

Built on 2026-10-13 in one commit by a building agent to a brief (`pass55/brief_build.md` and the task's own), its diff read and its work checked in the Studio by the session.

### 7.1 Departures from the frame, first

Each decided under the user's word of 2026-10-12; each gets a backlog row in section 59.

- **The British light mortar keeps red To Hit numbers,** though the chart of p. 695 gives the British black. Its shots are recorded with red, and a recorded shot must not change; the chart's color is read for a Gun and a vehicle's MA only (ruling R55.22).
- **No second reading of the 84 transcription rows was made.** The source record names the frame's agent as the reviewer of the values, which the building agent matched one by one against its own renders of pp. 392, 426, and 695. The reviews of the pass read them again.
- **APDS, Canister, and the 6-Pounder's HE are listed and not yet fired:** the page offers them for the new Guns and the gate refuses them until increments 2c and 2d.

### 7.2 What is built

**The notation (ruling R55.4).** `ParseAmmunition` reads one item of a counter's special ammunition, `<type><number>[^<year digit>[m<month>][+][E]]`, with the two-letter types (HE, AP) tried before the one-letter ones (A, D, H, C); anything else a counter lists, such as `s7`, is not an item and is passed over. `Depletion` answers for all six types: a number holds from its year, and from its month where the counter gives one, until the next listed one; a number with no year holds at any date (C8.8); undated HEAT keeps C8.3's dates; Elite adds one (C8.2), which increment 2c sets. A test pins the Depletion Number of every item of every older counter for each year from 1939 to 1945, run against the old code before the rewrite and against the new after it: nothing moved.

**The counters (ruling R55.3).** `british-at-gun`, `american-at-gun`, `british-crew`, and `american-crew`, every value as the frame gives it and none manufactured; the malfunctioned side's repair and removal numbers are "not in source", as the older Guns have them. 84 rows in each of the two counter sheets, two new sheets in the source record (OLB, OLA), the catalog rebuilt at 1.14.0, the four packages' digests pinned again. A game recorded under 1.13.0 loads and replays against 1.14.0 (`OlderGamesTests` has the row); nothing compares the package digest an event names at replay; the cards, the fixtures, and the tests that name 1.13.0 are untouched. A new game is recorded under 1.14.0.

**The To Hit color (C3.3, p. 169; the chart of p. 695).** British black; American red before 1944 and black from 1944. An American shot always records the scenario's year, which a shot at Infantry with no named ammunition did not; no American ordnance fired before the pass.

### 7.3 Ruling, the new facts, the games, and the tests

- **R55.22.** The chart's To Hit color is read for a Gun and a vehicle's MA. A light mortar and a LATW keep the color they had before the pass, red for every nationality but the German.

**New in a record:** `scenarioYear` on an American shot at Infantry. No record made before the pass can lack it.

| Game | What it shows |
|---|---|
| `p55-6pdr` | June 1944. A 6-Pounder in [D10] is offered AP, APDS, and HE against a PzKpfw IIIH in [F10]; APDS, and HE at the squad in [G10], are refused for now. AP: Basic TH# 10 (black), a hull hit, TK# 15 + 1 less AF 6 = 10, a wreck. Recorded. |
| `p55-37mm-1943` | An American 37mm AT Gun fires HE at [H10]: "Basic TH# 8 (red)". Recorded. |
| `p55-37mm-1944` | The same shot a year later: "Basic TH# 8 (black)", a hit, 4 FP. Recorded. |

Rules tests: `ScenarioA1Pass55RulesTests`, nine tests. Run by the agent: Rules 262 (the pass's class, pass 7's, the packages', and the classes the catalog reaches), Authoring's matrix tests 21, Units 409 and its counter sheets 19 and rendering 352 whole, Play 104, MapStudio 41: none failed.
