# Campaign mortar game default

Implemented 14 October 2026. This is manufactured game data authorized by the user, not a historical transcription or a claim of independently reviewed ASL counter values. It follows R0.3 and the existing MFG sheet convention.

## Profile and comparisons

Catalog `asl-scenario-a1` version `1.15.0` adds `campaign-german-81mm-mortar`, an `asl:gun` with type `mtr`. Existing catalog definitions are unchanged.

| Property | Default | Design rationale |
| --- | --- | --- |
| Caliber | 81 mm, star suffix | Medium mortar with short-barrel ordnance model, not infantry firepower. |
| Range | 3-60 hexes | At 40 m/hex, about 120 m to 2.4 km. Deliberately longer than existing German 50 mm (2-13) and British 51 mm (2-11) light mortars. |
| Rate of fire | 2 | Conservative sustained-fire choice, matching the British light mortar; below the German light mortar's 3. |
| Reliability | B12, R2, X6 | Matches the catalog's common breakdown convention; repair/removal are explicit manufactured defaults. |
| Manhandling | M11 | Easier to handle than the British 57 mm AT gun's M10; not a movement allowance. |
| Target and mount | Small, 360 degrees, QSU | A compact crew-served emplacement. No NM, RFNM or animal-pack capability. |
| Ammunition | HE available, no AP, no special ammunition | No invented AP, smoke or depletion allotments. |
| BPV | 30 | Provisional balance value below the British 57 mm AT gun's 34; not a researched purchase cost. |
| Availability | 1939-45 | Broad game-default availability, not a historical applicability assertion. |
| Operator | Existing German 2-2-8 crew | Existing crew factors stay unchanged. No infantry attack, defense or movement factors are assigned to the weapon itself. |

Each selected German 81 mm formation retains six weapons and six crews, one crew per weapon. This is an explicit game allotment, not an inferred historical strength. Campaign inspection identifies the operator, definition and manufactured status.

## Handoff and compatibility

`campaign-asl-handoff-2` includes every weapon as an equipment instance with an ASL `manned` holding link to its crew. Identity mappings, sides, catalog references and ground positions are preserved. Weapons and crews remain separate counters at the same location. Setup validation and import use the existing ASL engine.

Existing paired campaign deployments load without changing their terrain, placements, completion state or revision, including those recording catalog 1.14.0. Older single-detachment deployments retain the previously implemented migration. Old prepared handoffs are invalidated and can be prepared again; existing Studio games are not overwritten. Regenerating a handoff for the new catalog creates a new game identity. Full-Situation campaign exports now include later Studio game history; the bounded selected-formation pilot keeps its existing save contract. See the Goodwood transition release report for full-map transfer acceptance.

## Remaining boundary

Medium mortar firing is not implemented by the current ASL combat adapter. The profile retains `mtr`, which is outside the admitted firing types, rather than being mislabeled as direct-fire artillery or a light mortar. Setup readiness means that the data and crew link are valid for setup, not that every combat action is implemented. Dismantling, transport, medium-mortar firing and campaign outcome return remain follow-up work. No historical research is required to use these setup defaults.

Verification results are recorded in the Campaign README.
