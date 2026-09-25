# ASL Unit State Model Design

**Status:** Built with a synthetic game fixture; no live game source (D2 deferred)

**Date:** 2026-09-26

**Requirements:** [ASL Unit Requirements](<LimboDancer.Agentic.CognitiveRuntime ASL Unit Requirements.md>), section 13, step 4: ASL-UNIT-020 to 026, 030, 031, 040, and 041, under decisions D2 and D3 (section 12). ASL-UNIT-042 (governed writes) and 050 (a live source) stay out of scope.

**Related documents:** the [Scenario A1 Catalog Design](<ASL Scenario A1 Catalog Design.md>) for the definitions instances refer to; the [ASL Unit Domain Model Analysis](<ASL Unit Domain Model Analysis.md>); the [ASL Unit Display Design](<ASL Unit Display Design.md>) for the documents the display draws; and the [Map Model and Authoring Design](<LimboDancer.Agentic.CognitiveRuntime ASL Map Model and Authoring Design.md>), section 4.3, for the location chain.

Rule citations give physical pages of the registered rulebook PDF, `eASLRB_v3_01.pdf` (SHA-256 `957de75b...a247`), checked against the PDF and the Markdown page markers.

## 1. Outcome

A game is an ordered list of events. Its state at any revision is what those events produce, and every read names a perspective, which decides what the reader receives. This step delivers, in `LimboDancer.Domains.Asl.Units` under `State/`:

- game scope, sides, the map in play, turn and phase (ASL-UNIT-020);
- unit, equipment, and entity instances, with lineage (021, 022, 026);
- conditions as separate dimensions with five states, and Good Order derived (023);
- positions against the location chains of the exact board versions in play (024);
- relationships and their invariants (025);
- named perspectives and filtering at the source (030, 031);
- the event envelope and replay (040), and stamps that go stale (041);
- a synthetic game fixture on board 01, and a Game states page in Map Studio.

Nothing writes game state. Events come only from fixtures, which are synthetic (ASL-UNIT-050); writes wait for the governed path (ASL-UNIT-042, step 7).

## 2. Game scope and sides

`GameScope` is the tenant and game. `GameState` records the sides (`SideState`: id, nationality, ELR, SAN), the `MapInPlay` (its reference and version and the placed boards, each with its exact board version), the catalog identity, the turn, phase, and phasing side, and the revision and time of the last event. ELR (A19.1, p. 86) and SAN (A14.1, p. 82) belong to the side, not a unit. The phases are the eight of A3.1 to A3.8 (p. 47): `rph`, `pfph`, `mph`, `dfph`, `afph`, `rtph`, `aph`, `ccph`.

## 3. Instances

Units, equipment, and entities that are not units are three separate records (ASL-UNIT-026), all readable as `IGameObject`:

| Record | Holds |
|---|---|
| `UnitInstance` | id, kind, `DefinitionReference` (catalog identity and definition id), owning side, position, conditions, status, `From` (the instances it was produced from), and `Custodian` when a prisoner |
| `EquipmentInstance` | id, kind, side, position, `Holding` (holder and role), conditions, status |
| `EntityInstance` | id, kind (a Sniper, fortification, or marker), side, position, conditions, status |

The kind decides which record an instance is: below `asl:equipment` it is equipment, below `asl:entity` an entity, otherwise a unit. A unit must name a definition in the game's catalog, of the same kind; a definition of another nationality serving a side gives a warning, not an error. Equipment and entities have no catalog definitions yet, so they are recorded by kind alone.

**Lineage** (ASL-UNIT-021) is one event type with an action: `reduced` (squad to half-squad), `deployed` (squad to two half-squads, A1.31, p. 45), `recombined` (two half-squads to a squad, A1.32, p. 45), and `replaced` (a unit of the same size, A19.13, p. 86). The consumed instances become `Consumed`; the produced ones record `From`, keep the side, and start where the consumed unit was unless placed. Capture (A20.2, p. 86) sets the `asl:captured` condition and the custodian; elimination sets `Eliminated`.

## 4. Conditions and Good Order

Each condition is its own dimension (ASL-UNIT-023), keyed by name, with one of five states: `true`, `false`, `unknown` (the default, when nothing is recorded), `withheld` (exists, but this perspective may not know it), and `inapplicable`. The names are the vocabulary's states plus three the display does not draw: `asl:hidden` (Hidden Initial Placement, A12.3, p. 80), `asl:captured` (A20.2, p. 86), and `asl:melee` (A11.15, p. 72). States the vocabulary groups as exclusive, such as broken and berserk, cannot both be true.

Good Order is derived, never stored: a Personnel unit neither broken, berserk, captured, nor held in Melee (Index and Glossary, Good Order, p. 23). It is `false` if any of those is true, `true` if all are known false, and `unknown` otherwise. For other kinds it is `inapplicable`, because a vehicular crew's stun and shock are not yet modelled.

## 5. Positions

A position (ASL-UNIT-024) is one of:

- `MapPosition`: a `BoardLocation` (placed board, hex, level, and a hexside where needed), or the hex's bridge, with a facing for kinds that face;
- `ContainedPosition`: inside another instance, as `passenger` or `rider` of a vehicle, or `in-fortification`;
- `OffMapPosition` or `NotEnteredPosition`.

Equipment is either held (possessed or towed, and then where its holder is), manned (a Gun at its own map position, with its crew in the same Location), or left at a map position with no holder.

**Location chains.** `ILocationChains` gives, for each placed board, the board version it was derived from and each hex's location levels and bridge. `HexFactLocationChains` reads them from the map model's `HexFactSet`. The projector refuses a game whose placed board version differs from the chains' version, and any map position whose hex is not on the board, whose level is not in the hex's chain, or whose bridge the hex lacks. Without chains, positions are not checked and the history says so (UNIT-STATE-020). Positions keep the placed board's reference; a composed map translates them (ASL-MAP-024).

`GameState.Location` follows containment and holding to a map position, so a passenger is where its vehicle is and a possessed SW where its holder is. `GameState.At` lists a stack: the active instances at one location.

## 6. Relationships and invariants

After every event the projector checks (ASL-UNIT-022, 025):

| Relationship | Invariant |
|---|---|
| Containment | The container is active and of the right kind (a vehicle for passengers and riders, a fortification for units in it); no cycles. A contained instance's location is its container's. |
| Holding | Exactly one holder, which is an active unit: Personnel to possess or man, a vehicle to tow. A manned Gun and its crew share a Location. |
| Custody | A prisoner's custodian is an active enemy unit in the same Location (A20.5, p. 87). Berserk units cannot be captured (A20.2). |
| Status | An eliminated or consumed instance cannot act. Equipment its holder held is left at the holder's last location. |
| Stack membership | Derived from positions, never stored. |

Not yet modelled: inherent crews (vehicles are outside the first slice), leadership participation, and transport beyond containment. They are added with the slices that read them.

## 7. Events

Every change is a `GameEvent` (ASL-UNIT-040) with the envelope scope, event id, revision, time, source, type, rule package reference, causes, and visibility, and a typed payload:

| Type | Payload |
|---|---|
| `game-started` | sides, map in play, catalog, opening turn, phase, and phasing side; must be synthetic |
| `phase-changed` | turn, phase, phasing side |
| `instance-created` | a new unit, equipment, or entity |
| `instance-moved` | a unit or entity's new position |
| `equipment-transferred` | a new holder, or a map position with none |
| `conditions-changed` | new values for some condition dimensions |
| `lineage` | the action, the consumed ids, the produced instances |
| `instance-eliminated` | the id |
| `instance-captured` | the id and the custodian |

`GameProjector` replays the events and returns a `GameHistory` with the state after each one. The first event is `game-started`, revisions rise by exactly one, event ids are unique, causes name earlier events, and visibility names the game's perspectives. The first event that breaks a rule stops the replay; the history then has no current state.

A game record is a JSON file (`GameEventReader`): the tenant, game, label, whether it is synthetic, and the events. Positions are written `{ "at": "bd01:E4:0" }`, `{ "in": "f1", "role": "in-fortification" }`, `{ "offMap": true }`, or `{ "notEntered": true }`; conditions as an object of names to `true`, `false`, or a state name.

| Code | Refused when |
|---|---|
| UNIT-STATE-001 | The record is not well formed, or an event type is unknown. |
| UNIT-STATE-002 | An event belongs to another tenant or game. |
| UNIT-STATE-003 | A revision is not the previous plus one, or an event id repeats. |
| UNIT-STATE-004 | The first event is not `game-started`, or the game starts twice. |
| UNIT-STATE-005 | A side or visibility names something that is not a side or perspective of the game. |
| UNIT-STATE-006 | An instance id is unknown, repeated, or not a slug. |
| UNIT-STATE-007 | An eliminated or consumed instance acts. |
| UNIT-STATE-008 | The catalog is not loaded, a unit has no definition, or the definition is of another kind (a nationality difference is a warning). |
| UNIT-STATE-009 | A condition is undeclared, or exclusive conditions are both true. |
| UNIT-STATE-010 | A position is off the map in play, off the board, outside the hex's chain, faces for a kind without facing, or the board version differs from the chains'. |
| UNIT-STATE-011 | A container is missing, inactive, of the wrong kind, or in a cycle. |
| UNIT-STATE-012 | A holding breaks its invariant. |
| UNIT-STATE-013 | Lineage consumes or produces the wrong number or kind, or changes side. |
| UNIT-STATE-014 | Custody breaks its invariant, or a berserk unit is captured. |
| UNIT-STATE-015 | A phase, phasing side, or turn is invalid. |
| UNIT-STATE-016 | A cause is not an earlier event. |
| UNIT-STATE-017 | A game is not synthetic while no live source is chosen. |
| UNIT-STATE-020 | (Warning) No location chains were given. |

## 8. Perspectives and filtering

A `Perspective` is a name (ASL-UNIT-030, D3): each side's id, and `adjudicator`. `GameState.Perspectives` is the game's closed set; reading with any other name is refused. Adding a perspective is a change to a game's sides, not to the model.

`GameView.Of(history, revision, perspective)` builds what that perspective receives (ASL-UNIT-031):

- the adjudicator receives the whole state and every event;
- a side receives its own instances in full, and the enemy's except that:
  - an enemy instance with `asl:hidden` true, and everything inside or held by it, is absent;
  - an enemy unit with `asl:concealed` true becomes a `SealedPresence`: a placement id (`sealed-1`, numbered by location), its side, and its location, every condition withheld (A12.11, p. 76); what it holds is absent;
- a side receives only the events whose visibility names it (or all perspectives).

Nothing withheld is in the view, so neither the display nor a log can leak it. The view also carries each visible instance's resolved location.

## 9. Stamps and staleness

`GameState.Stamp` is the scope, revision, and map version a conclusion used (ASL-UNIT-041). `Check(stamp)` returns `Current` when both are unchanged, `Stale` after any later event or a different map version, and `OtherGame` for another scope. Staleness is by revision; narrowing it to the events that affect a conclusion comes with the read contract (step 5).

## 10. Display and the Game states page

`GameDocuments.For(view)` turns a view into unit documents: each unit is its definition's document (Catalog Design, section 8) with its true vocabulary states as states, a sealed presence is a concealed placeholder, and an entity is its kind at its location. Equipment has no definitions and is not drawn. This is step 6's display from projections, built only for the synthetic fixtures; it reads nothing but the view.

Map Studio's **Game states** page (`/units/games`) replays each fixture, against the location chains of the boards the Studio can load. It shows a perspective picker and a revision slider; the turn, phase, and stamp; the units with a preview, their location, conditions, derived Good Order, lineage, and custody; the sealed presences; equipment and entities; and the events the perspective is entitled to. **Show on board** registers the view as a generated, read-only placement set and opens the board viewer with it, so the German view of revision 8 shows three German counters and a Russian "?" at E4, and nothing of the hidden leader.

## 11. The fixture

`src/ASL/units/games/a1-village.synthetic.game.json`, 18 events: a German platoon (two 4-6-7 squads, a 2-4-7 half-squad, and an LMG) at `bd01:D4:0` attacks a concealed Russian 4-4-7 in `bd01:E4:0`, while an 8-0 leader waits hidden in a foxhole in `bd01:E5:0`. The Russian squad is revealed in the MPh, one German squad is reduced in the DFPh, the Russian squad breaks in the AFPh, a German squad advances into E4 in the APh, and captures it in the CCPh. The definitions come from the published catalog; every event, position, condition, ELR, and SAN is invented.

The fixture plays board 01 at version `8d77d26222b7bb21d8c1fdda6ba05b447f63c317`, the Git blob of `boards/src/bd01/LOSData` in the VASL checkout, which is the version the Studio derives its hex facts from. With the checkout configured, the Studio checks every position against those chains.

## 12. Tests

- `Units.Tests/StateTests`: the fixture replays into 18 states; lineage, capture, containment and holding, Good Order, and every revision read back; each perspective's view, including that the German view at revision 8 contains no Russian id at all; the documents the display receives; staleness; and each refusal code, each refused event stopping the replay.
- `MapStudio.Tests/GameStatesTests`: the game library replays the fixture and says when boards cannot be checked; the page shows the adjudicator everything and a side only its view; a view becomes a generated placement set.

## 13. Not in this step

- A live game source and governed writes (D2, ASL-UNIT-042, 050).
- The read contract `ReadCase` and nondefinitive reads (ASL-UNIT-060, 061), with narrower staleness: step 5.
- Equipment and vehicle definitions, inherent crews, leadership participation, and transport beyond containment.
- Redacting parts of an event for a side; events are shown or withheld whole.
