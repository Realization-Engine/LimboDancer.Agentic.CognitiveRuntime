# ASL Unit Read Contract Design

**Status:** Built, read-only, over the synthetic game fixture; cross-checked against the Scenario A1 snapshots, which are unchanged

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 5: ASL-UNIT-060 and 061, with the map read API of ASL-MAP-080 and the compatibility rule of ASL-MAP-081. ASL-UNIT-041 is narrowed here from any later event to the events that affect a case.

**Related documents:** the [ASL Unit State Model Design](<ASL Unit State Model Design.md>) for games, views, and perspectives; the [Map Model and Authoring Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design.md>), section 11, for the map read API; and the [Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>), section 11, for the earlier cross-check of kinds.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`).

## 1. Outcome

A Scenario A1 case is a unit attempting something at a location. The read contract answers one question: what does the case read, now, for this perspective? It returns one consistent snapshot or an explicit nondefinitive result, never a partial answer. This step delivers:

- the map read API in `LimboDancer.Domains.Asl.Maps` (`Read/BoardCatalog.cs`): `IBoardCatalog`, `BoardHandle`, `LocationRead`;
- the read contract in `LimboDancer.Domains.Asl.Units` (`State/CaseRead.cs`): `CaseRequest`, `CaseReader`, `CaseSnapshot`, `CaseReadResult`;
- movement expenditure in the state model: a move may spend MF, and a new phase resets them;
- staleness by the events that affect a case;
- cross-check hooks against the unchanged Scenario A1 snapshots;
- a Read a case panel on Map Studio's Game states page, reading through the Studio's real boards.

## 2. The map read API

`IBoardCatalog.TryGetBoard(board, version)` returns a `BoardHandle` for that exact version, or a diagnostic (MAP-READ-001) when the board is unknown or at another version. It never substitutes a version. A `BoardHandle` carries:

- the board reference, version, status, and provenance;
- the derived `HexFactSet`, its geometry, `HexFacts(hex)`, `Neighbor(hex, side)`, and `Distance(from, to)`;
- `Resolve(location)`, which returns a `LocationRead` or a diagnostic (MAP-READ-002) when the location is on another board, off the board, or not in the hex's location chain.

A `LocationRead` holds the location, the hex's facts, the facts of its level (terrain and depression terrain), the board version and status, and an evidence reference such as `board:bd01@8d77d262...#E5:0` that an observation can cite. `BoardReadStatus` is `Verified`, `AuthoredValid`, `Ingested`, or `Authored`. Only the first two are definitive: unverified terrain is nondefinitive evidence (ASL-MAP-044).

`InMemoryBoardCatalog` serves boards already loaded. In Map Studio, `StudioBoardCatalog` serves every board the Studio can load, mapping its board status and recording the VASL commit and LOS data blob as provenance. The API has no ASL runtime dependency. The grid view for a later LOS executor (ASL-MAP-082) is not part of this step.

`Board01TerrainCatalog` and the Scenario A1 providers are unchanged (ASL-MAP-081). Backing them with `IBoardCatalog` is a separate reviewed change.

## 3. The request

`CaseRequest(scope, package, attacker, location, expectedRevision, perspective)` is ASL-UNIT-060's `ReadCase(tenant, game, package, attacker, location, expectedRevision, perspective)`:

- the scope is the tenant and game;
- the package is the domain package the question is asked under, as text; the read records it and does not interpret it, so `Units` needs no runtime reference (ASL-UNIT-002);
- the attacker is a unit instance id, and the location a `BoardLocation`;
- the expected revision is the revision the caller believes is current;
- the perspective is one of the game's (State Model Design, section 8).

A game source (`IGameSource`) finds the game's history by scope. Until a live source is chosen (D2), the only source is the synthetic fixtures (`HistoryGameSource`).

## 4. The snapshot

`CaseSnapshot` is built from one revision of one game, as the perspective may know it:

| Part | Content |
|---|---|
| Stamp, source, time | The game's scope, revision, and map version; the source and time of the event that made the revision |
| Phase | The turn, phase, and phasing side |
| Attacker | The unit instance, its catalog definition, its derived Good Order, and the MF it has spent this phase |
| Location | The target, as a `LocationRead` through the map read API: level facts, board version, status, evidence |
| Previous location | The attacker's current location, read the same way: the location it moves from and would return to |
| Distance | Hexes between the two, through the map read API |
| Occupancy | The visible units, sealed presences, and entities at the target, and whether that is complete |
| Events | The events of the current phase that concern the attacker, the occupants, or either location, in order, and among them the reveals |

**Occupancy.** Only the adjudicator's occupancy is complete. A side can never rule out hidden units (A12.3, p. 80), and a sealed presence is a unit it cannot identify (A12.11, p. 76). So a side's occupancy is always incomplete, with the reason. `SoleEnemy(side)` gives the one enemy unit only when occupancy is complete and exactly one enemy is there; otherwise it gives the reason instead. This is how the read never reports a sole defender from incomplete visible information (ASL-UNIT-061).

**Reveals.** A reveal is a `conditions-changed` event that sets `asl:concealed` or `asl:hidden` to false (A12.15, p. 78; A12.3, p. 80). Its causes say what revealed it, such as the attacker's move.

**Movement expenditure.** `instance-moved` may carry `mf`, the MF the move spent. A unit's `MfSpent` adds them up and returns to zero at every `phase-changed`. The read reports MF spent, not MF remaining: the allowance is a rule (A4.1, p. 48), not state.

**Responses.** The state model has no event type for Defensive responses yet, so a case that needs to know whether a response or immediate CC is resolved cannot learn it from the read. It stays unknown.

## 5. Nondefinitive results

Every failure is a `CaseReadResult` with a status, a code, and a reason, and no snapshot unless stated (ASL-UNIT-061):

| Code | Status | When |
|---|---|---|
| CASE-000 | Definitive | One consistent snapshot on verified terrain |
| CASE-001 | Unauthorized | The game exists under another tenant |
| CASE-002 | Unavailable | There is no such game |
| CASE-003 | Inconsistent | The game's events do not replay into a state |
| CASE-004 | Unauthorized | The perspective is not one of the game's |
| CASE-005 | Stale | The game is at another revision than expected |
| CASE-006 | Unavailable | No such unit is visible to the perspective; a hidden or concealed enemy is reported exactly like a missing one |
| CASE-007 | Inconsistent | The attacker is not on the map |
| CASE-008 | Inconsistent | The attacker's definition is not in a loaded catalog |
| CASE-009 | Inconsistent or Unavailable | A location is off the map in play, its board version cannot be read, or it is not in the hex's location chain |
| CASE-010 | Unverified | A snapshot is returned, but its terrain comes from a board that is not verified |

## 6. Staleness

`CaseReader.Affects(snapshot, event)` is true for a later event that moves, changes, reduces, captures, or eliminates the attacker or an occupant; puts something at the target or previous location; or changes the phase. `IsStale(snapshot, history)` asks that of every later event. Only events the snapshot's perspective is entitled to count, so a side's snapshot does not go stale on an event it may not see, and staleness itself reveals nothing. An event about other units at other locations leaves a case current (ASL-UNIT-041).

## 7. Cross-check against the Scenario A1 snapshots

`ScenarioA1CaseReadCrossCheckTests` maps case snapshots onto the Scenario A1 snapshot records, which are unchanged. The facts the unit model supplies are filled in: locations, phase, attacker kind and concealment, Good Order, reveal and its cause, and occupancy. The facts it does not supply stay null: entry mode, A4.14 exceptions, elections, NTC, remaining MF, special modifiers, and responses. The records are compared with the reviewed cases' baselines field by field and are never given to a provider or resolver. The fixture is synthetic and must not reach Scenario A1 adjudication (ASL-UNIT-050).

| Read | Reviewed case | Result |
|---|---|---|
| Adjudicator, revision 10: g1 in `bd01:D4:0` into `bd01:E4:0`, after the Russian squad is revealed | Post-reveal, non-dummy | Location, previous location, reveal, MPh, ordinary Infantry, and unconcealed attacker all match; the other facts stay null |
| The same at revision 9, before the reveal | Post-reveal | The reveal is unknown: the read does not claim one |
| Adjudicator, revision 21: gh1 into `bd01:E5:0`, after the hidden leader is revealed by its move | Concealed SMC OVR | Revealed enemy SMC, A12.15 reveal by the move, SMC outside an AFV, MPh, Good Order unconcealed MMC, and verified sole SMC all match; the initial occupancy is hidden, not concealed, so it is not the reviewed case |
| German, revision 21, the same case | Concealed SMC OVR | Sole SMC occupancy is not verified and the revealed occupant stays unknown |

## 8. The Read a case panel

Map Studio's Game states page has a Read a case panel. It reads the game as it stood at the revision on the slider, for the chosen perspective, through `StudioBoardCatalog`. You choose an attacker (a side's own units, or any unit for the adjudicator), a location, and the expected revision. It shows the result's status, code, and reason, and for a snapshot: the stamp and phase, the attacker, both locations with terrain, level, board version, and status, the distance, the occupants and whether occupancy is complete, the sole enemy answer, and the events of the phase with the reveals marked.

With the VASL checkout configured, board 01 is verified, so an adjudicator read of gh1 into `bd01:E5:0` at revision 21 is definitive, with the Russian leader as sole enemy. The German read of the same case gets the same occupants but no sole enemy. Expecting revision 20 gives Stale.

## 9. The fixture

The fixture gains a second Player Turn (revisions 19 to 21): the MPh of turn 2, the German half-squad gh1 moving from `bd01:D4:0` to `bd01:D5:0` for 1 MF, and the reveal of the hidden leader in `bd01:E5:0`, caused by that move. Every value remains synthetic.

## 10. Tests

- `Units.Tests/CaseReadTests`: the map read API (resolution, version, evidence, status, distance, neighbor, catalog); an adjudicator snapshot; a side never getting a sole defender; a concealed occupant as a sealed presence; a hidden unit reported like a missing one; every nondefinitive code; staleness by affecting events; staleness judged only from entitled events.
- `ScenarioA1.Tests/ScenarioA1CaseReadCrossCheckTests`: section 7.
- `MapStudio.Tests/GameStatesTests`: the panel's attacker choice by perspective, an unavailable board, and a stale read.

## 11. Not in this step

- Defensive response and CC events, so that response status can be read.
- Backing `Board01TerrainCatalog` with `IBoardCatalog` (ASL-MAP-081 requires its own reviewed change).
- The LOS grid view of the map read API (ASL-MAP-082).
- Feeding case reads into Scenario A1 adjudication: it needs a live source (D2) and the governed path.
