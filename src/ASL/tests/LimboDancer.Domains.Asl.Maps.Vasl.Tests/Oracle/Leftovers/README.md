# Step 15 LOS leftovers

The five terrain groups are exercised with controlled maps and VASL's own `Map.LOS`. Production changes remain in Maps LOS. Historical-board ingestion and counter placement are not added.

## Board survey

`board-survey.json` records a read-only survey of VASL checkout `33324f9adb3b9b97dd93700685103b6940dc9c3c`: 297 `bd*` source directories, 248 decoded LOS grids, and five candidate boards. Missing files, unknown terrain codes, and malformed metadata are recorded rather than silently skipped. Board 79 has malformed metadata; its LOS grid was still scanned. Counts are terrain pixels or actual XML elements, excluding comments and unused catalog entries.

| Board | Relevant terrain or annotations | Import limitation |
| --- | --- | --- |
| Dinant | Partial orchards, tunnels, railroad embankment terrain | Custom hex dimensions and origin |
| RBv2 | Railroad embankment annotations, roofless factories | Custom origin |
| RBv3 | Railroad embankment annotations, roofless factories | HASL construction |
| RO | Railroad embankment annotations, roofless factories | HASL construction |
| VotG | Gutted building walls | HASL construction |

No decoded board contains entrenchment terrain. VASL normally supplies entrenchment locations through counters. The user approved controlled oracle fixtures on 2026-09-26 to avoid expanding board ingestion.

Regenerate the survey with Python 3:

```powershell
python ./survey-boards.py E:/Archive/GitHub/dlandi/vasl > board-survey.json
```

## Controlled fixtures

`maps.xml` describes 33 authored maps with 43,044 directed LOS pairs. Each starts with open ground in standard geomorphic geometry. Rectangles are painted in order, with exclusive right and bottom bounds. Annotations are applied before VASL resets hex terrain twice. Explicit `location` entries then replace center-location terrain to exercise resolved entrenchment endpoints. They do not simulate counter handling.

The fixtures include:

- Partial orchards at several endpoint elevations, over water, and from hexside LOS and auxiliary points.
- Railroad annotation crossings and inherent embankment terrain, including raised endpoints, bridge terrain, and cellar endpoints.
- Entrenchments at either end, with walls, railroad annotations, and hillocks.
- Tunnel terrain and both directions between a tunnel and its above-tunnel location.
- Roofless and gutted factories, gutted buildings and walls, hexside hindrances, elevated blind-hex cases, rooftops, and rubble.

`LeftoverLosOracle.java` uses VASL classes and reuses the established oracle harness's setup and serialization. It contains no replacement LOS algorithm. Fixtures contain derived results and hex facts, not VASL artwork or terrain grids. `bd01` is only the controlled map's coordinate namespace, not a claim that its terrain is board 01.

Regenerate with PowerShell 7 and JDK 17 or later:

```powershell
./generate.ps1 -VaslRoot E:/Archive/GitHub/dlandi/vasl
```

Fixture headers record the VASL commit, shared terrain catalog blob, harness version, and the current specification's Git blob hash with LF line endings. Temporary compilation files are placed in a newly named system temporary directory. Nothing in the VASL checkout is edited.

`LosLeftoverTests` pins every map name and pair count, verifies fixture input identities, checks derived locations and hexside annotations against VASL, and compares all pairs through `LosFidelity`. Every pair must be answered; agreement includes the blocked hex, range, reason, hindrance total, breakdown, and first hindrance point. Smaller `LosTests` regressions run without VASL.

The port deliberately preserves VASL's asymmetric same-hex tunnel result, its partial-orchard exit-spine reassignment even for hexside sources, and its factory rooftop height adjustments. Historical geometry, counter integration, Deir, sand dunes, and the other named unsupported rule groups remain outside this change.

## Validation

- Maps: all 244 tests passed.
- Existing LOS corpus: all 19 fixtures, covering 239,480 pairs, passed with their answered counts preserved. The ten existing VASL failures remain explicitly unsupported.
- Controlled fixtures: all 43,044 pairs agreed and were answered; all 74 fixture, survey-scope, and comparison checks passed.
- Temporarily disabling each of the five rule groups made its portable regression fail. The implementation was restored before the passing runs.
- The bridge and its control both derive an actual bridge at road level zero. VASL reports zero hindrance with the embankment side and one without it on C3 to G5.
