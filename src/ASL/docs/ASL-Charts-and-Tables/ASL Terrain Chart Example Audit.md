# ASL Terrain Chart Example Audit

Date: 2026-09-26.

## Scope and interpretation

This first-pass audit checks every explicit map reference in `ASL B Terrain Chart.md` against the corresponding local VASL map artwork, imported terrain grid where available, and derived hex facts. It accounts for all 50 chart rows. It is a representation audit, not verification of every movement, LOS, TEM, concealment, rally, fire, or fortification rule.

There are 38 distinct referenced hexes across 15 maps: 35 hexes on 13 numbered boards, plus PB C9, St MM11, and St LL14. Both ends of referenced hexsides were inspected. Repeated references, such as the Hedge and Bocage example, are shared evidence rather than separate map locations.

The source maps are in the local `E:/Archive/GitHub/dlandi/vasl` checkout. Numbered boards were read from `boards/src/bdNN`; PB and ST were read from the `boards/bdFiles/bdPB` and `bdST` archives. These last two archives contain artwork but no `BoardMetadata.xml` or `LOSData`; the importer rejects them as legacy V5 boards. The ST artwork identifies itself as Stoumont, KGP I. The PB artwork is Pegasus Bridge.

All numbered-board examples were run through the current `VaslBoardImporter` and `HexFactFidelity.Derive` implementation. All 13 numbered boards passed F1 and F2 against the existing oracle fixtures. Passing those checks establishes agreement with the encoded VASL source and oracle, not independent agreement with the terrain chart.

Support needs to be evaluated separately at these levels:

1. **Artwork:** whether the referenced map depicts the feature.
2. **Grid and rendering:** whether the imported terrain/elevation cells retain the feature. The exact renderer draws these cells, including cells away from the hex center.
3. **Derived facts:** whether the feature is represented at the center, on hexsides, in additional Locations, or in a bridge record.
4. **Rules:** whether the relevant movement, LOS, TEM, and other behavior works. This audit does not establish complete rules support.

A center terrain name alone cannot decide support. Wall and cliff examples are hexside features. A bridge can be above a gully Location. A hill can have Open Ground as its surface terrain. Likewise, a road through shellholes or through a sunken-road depiction can have a Dirt Road center sample while other grid cells retain the additional feature.

## Correction: Board 14 T3

The chart explicitly identifies **14T3 as Sunken Road**, with **Depression** in its LOS column. The user's Studio screenshot visibly shows the sunken-road strips. The original Board 14 artwork also depicts them.

The current implementation retains Sunken Road as catalog code 68, category `Depression`. Board 14 contains 15,764 cells with this code. The T3 hex region, selected by the geometry's nearest-center hit test, contains 1,871 Sunken Road cells, 924 Dirt Road cells, and 815 Open Ground cells. These pixel counts describe this source and this region-selection method, not an ASL rules definition of the hex.

The derived result is:

| Field | Current value |
| --- | --- |
| Center terrain | Dirt Road, code 65 |
| Base elevation | -1 |
| Center depression | null |
| Additional Locations | None |
| Hexside depression fields | All null |
| F1 / F2 | Both pass |

The exact renderer does not rely on the center's terrain name. It draws terrain/elevation regions from the grid, using each terrain code's color. This explains why the rendering shows the strips while the hex inspector's derived facts omit the depression.

`CenterTerrainSampler.Sample` chooses the near-center road cell. `VaslCompatibleHexFactDerivation.SetDepressionTerrain` records a depression when the center terrain is itself classified as a depression, or preserves an existing depression. It does not infer a sunken-road depression from the surrounding code-68 region when the center is Dirt Road.

**Finding:** Sunken Road is present in the catalog, grid, and rendering. The derived facts for the chart's canonical example do not expose its Sunken Road/depression identity. Specific LOS or movement errors have not yet been demonstrated. Adding a missing terrain name or merely changing the drawing would not address this finding.

## Complete chart-row coverage

“Represented” below means the relevant map feature is present in the inspected data fields. It does not mean all associated rules are implemented. Entries without a fixed map reference are explicitly marked as requiring another kind of test.

| Chart terrain | Referenced map example | Observed representation and audit result |
| --- | --- | --- |
| 1. Open Ground | 1B1 | Artwork and center facts agree: Open Ground, base 0. Represented. |
| 2. Shellholes | 2U6 | Shellholes are visible and present in the grid. Center facts report Dirt Road; no separate shellhole Location is derived. Compound/occupancy representation needs review; shellholes are not absent from the map. |
| 3. Road | 1Y10 / 1Z8 | Dirt Road at Y10 and Paved Road at Z8 appear in artwork, grid, and center facts. Represented. |
| 4. Sunken Road | 14T3 | Visible in Studio and source artwork; Sunken Road cells retained. Center is Dirt Road at base -1 with no depression field. Confirmed derived-fact omission. |
| 5. Elevated Road | 13L5 | Raised-road artwork and Elevated Road cells are present. Center is Dirt Road at base +1. Elevation is retained; explicit elevated-road identity is absent from the center facts. Compound representation needs review. |
| 6. Bridge | 5Y8 | Artwork and grid show the bridge. Center is Gully at base -1, with a Single Hex Stone Bridge Location at relative level 1 and a bridge record. Represented in additional fields. |
| 7. Runway | 14M6 | Source artwork visibly shows the runway. Imported M6 cells are Open Ground; Board 14 contains no Runway code anywhere. Confirmed source-grid coverage gap. |
| 8. Sewer | 1D5 / 1E4 | Surface artwork is a road junction/building. D5 derives Paved Road only; E4 derives building, cellar, and rooftop Locations. Neither derives a Sewer Location. Rules-defined subterranean representation needs review; surface artwork alone cannot establish it. |
| 9. Wall | 2H1 / 2I1 | Common boundary visibly has a wall. Wall is present in the grid and the corresponding hexside facts. Center road labels do not indicate a wall-support failure. |
| 9. Hedge | 2T1 / 2U2 | Hedge appears on the common boundary and in hexside facts. Represented. |
| 9.5 Bocage | 2T1 / 2U2 | The unmodified map and facts show Hedge. This reuses the hedge example; a Bocage conversion/SSR case must be evaluated separately. No failure inferred from the base board's hedge label. |
| 9.6 Hillside Wall/Hedge | 25X4-X5 / 25U3-U4 | Both boundaries visually inspected. Wall and Hedge are in hexside facts. X4/X5 bases are 0/+1; U3/U4 bases are 0/-1. Component terrain and elevation differences are represented; hillside-specific rules remain untested. |
| 9.7 Cactus Hedge | SSR | No fixed map example. Requires an SSR or authored fixture. Not assessed by this map-example audit. |
| 10. Hill | 2E8 | Hill coloration visible; center surface is Open Ground and base is +1. Represented through elevation. |
| 11. Cliff | 2W5 / 2V4 | Cliff depiction visible on shared boundary. Cliff flags are present; bases are +2/0. Represented as a hexside feature. |
| 12. Brush | 12AA10 | Brush artwork and center facts agree. Represented. |
| 12.7 Vineyard | SSR | No fixed map example. Requires an SSR or authored fixture. Not assessed here. |
| 13. Woods | 1C9 | Woods artwork and center facts agree. Represented. |
| 14. Orchard | 6F5 | Orchard artwork and center facts agree. Represented; seasonal rules not tested. |
| 14.7 Cactus Patch | SSR | No fixed map example. Requires an SSR or authored fixture. Not assessed here. |
| 14.8 Olive Grove | SSR | No fixed map example. Requires an SSR or authored fixture. Not assessed here. |
| 15. Grain | 3K9 | Grain artwork and center facts agree. Represented; seasonal rules not tested. |
| 16. Marsh [Mudflat] | 7G2 | Marsh artwork and center facts agree. Mudflat is a qualified alternative, not the unmodified map's center result; it needs a separate conditions/SSR test. |
| 17. Crag | 15X9 | Crag artwork and `Crags` center facts agree. Represented. |
| 18. Graveyard | 12W4 | Graveyard artwork and center facts agree. Represented. |
| 19. Gully | 5Y3 | Gully artwork agrees with center and depression fields; base -1. Represented. |
| 20. Stream | 13N6 | Stream artwork agrees with Shallow Stream center and depression fields; base -1. Dry/deep alternatives require separate conditions tests. |
| 21. Water Obstacle | 7E2 | Water artwork agrees with Water center terrain; base -1. Represented; fordability not verified. |
| 22. Valley | 24P8 | Valley-area coloration is visible. Center is Open Ground at base 0, while the chart lists Level -1 for Valley. Record as an unresolved elevation interpretation; local elevation conventions and B22 need checking before calling it a defect. |
| 23. Wooden Building | 1C7 | Wooden building artwork agrees with Wooden Building center terrain. Represented. |
| 23. Stone Building | 1J2 | Stone building artwork agrees with Stone Building center terrain. Represented. |
| 24. Rubble | Counter | No fixed map example. Requires counter placement or terrain-change testing. Not assessed here. |
| 25. Fire (Blaze) | Counter | No fixed map example. Requires state/overlay testing. Not assessed here. |
| 26. Wire | Counter | No fixed map example. Requires counter placement and rule testing. Not assessed here. |
| 27. Entrenchment [Trench] | Counter | No fixed map example. Requires counter/Location testing. Not assessed here. |
| 28. Minefield | Recorded | No fixed map example. Requires recorded-state and rule testing. Not assessed here. |
| 29. Roadblock | Counter | No fixed map example. Requires counter/hexside testing. Not assessed here. |
| 30. Pillbox | Counter | No fixed map example. Requires counter/Location testing. Not assessed here. |
| 32. Railroads | Overlays | No fixed map example. Requires specific overlay fixtures and the reverse-side railroad chart. Not assessed here. |
| 32.5 Rail Cars | Overlays/Counter | No fixed map example. Requires overlay/counter fixtures. Not assessed here. |
| 32.57 Wrecked Rail Cars | Counter | No fixed map example. Requires counter/terrain-change fixtures. Not assessed here. |
| 33. Stream-Woods | 47F6 | Artwork shows stream through woods; both types exist in the grid. Center/depression are Shallow Stream, base -2; several hexside terrain fields are Woods. Components are retained, but no combined Stream-Woods field is derived. Combined-terrain rules need review. |
| 33. Stream-Brush | StMM11 | ST artwork visually confirms stream and brush in the referenced hex. Current importer rejects the available legacy ST archive; derived-fact and Studio representation checks are unavailable. |
| 33. Stream-Orchard | StLL14 | ST artwork visually confirms stream and orchard in the referenced hex. Current importer rejects the available legacy ST archive; derived-fact and Studio representation checks are unavailable. |
| 34. Tower hex [Tower Location] | PB C9 | PB artwork visibly shows the circular Chateau d'Eau tower at C9. Current importer rejects the available legacy PB archive; derived-fact and Studio representation checks are unavailable. |
| 35. Light Woods | SSR | No fixed map example. Requires an SSR or authored fixture. Not assessed here. |
| 36. PFZ Vineyard | Counter | No fixed map example. Requires Prepared Fire Zone state/terrain-change testing. Not assessed here. |
| 36. PFZ Open Ground | Counter | No fixed map example. Requires Prepared Fire Zone state/terrain-change testing. Not assessed here. |
| 37. Debris | Counter | No fixed map example. Requires counter/terrain-change testing. Not assessed here. |
| D10. Wreck | Counter | No fixed map example. Requires vehicle/wreck state and rule testing. Not assessed here. |

## Follow-up: is VASL already supplying the complete hex terrain type?

### Conclusion

For an unmodified Board 14, **no separate per-hex Sunken Road classification was found that our importer is discarding**. VASL supplies the ingredients: Sunken Road terrain definitions, roadbed and contour codes, elevations, and artwork. Its ordinary hex/Location derivation does not combine those ingredients into a Sunken Road center identity at T3.

This conclusion is scoped to the inspected local VASL checkout, commit `33324f9adb3b9b97dd93700685103b6940dc9c3c`, and the current Board 14 source. It is based on the serialized format, metadata, runtime loading and derivation code, editor generation code, overlay code, and the existing matching oracle result. It does not assert that every VASL version or scenario configuration behaves identically.

The earlier description of “surrounding Sunken Road cells” described the encoding, not the physical terrain. **The roadbed through the center of T3 is itself part of the Sunken Road terrain type.** The chart and B4 establish that identity; the central Dirt Road code is only part of its representation.

### What the metadata and stored data actually contain

| Source | Observed contents | Significance for T3 |
| --- | --- | --- |
| Shared terrain catalog | `Sunken Road`, code 68, `LOSCategory="DEPRESSION"`, non-inherent; Dirt Road is a separate type. | Sunken Road is already a recognized terrain type, not a missing catalog entry. |
| Shared color palette | `DirtRdL_1`, RGB `(220,205,122)`, maps to `Dirt Road` with elevation `-1`. | The roadbed's surface code and depressed elevation are deliberately encoded together. |
| Shared color palette | `SunkRoad1` and `SunkRoad2`, RGB `(146,110,30)` and `(94,57,23)`, map to `Sunken Road`, initially with unknown elevation. | The contour colors carry a separate Sunken Road terrain code. |
| Board 14 metadata | Board geometry/version, an empty building-override list, overlay declarations, and color/SSR sections. No per-hex Sunken Road assignment. | There is no `T3 = Sunken Road` declaration to expose from this file. The `NoSunkElevRoads` overlay declaration is a transformation, not such an assignment. |
| `LOSData` | Four dimensions, an elevation byte and terrain-code byte per grid cell, then stairway bytes per hex. | There is no serialized per-hex composite terrain field or serialized depression-Location list hidden after the grids. |

The source artwork pixel at the actual near-center probe `(1069,192)` is RGB `(220,205,122)`. It exactly matches `DirtRdL_1`. This explains the Dirt Road/-1 result without guessing from the rendered color.

The Board 14 archive and source directory contain byte-identical `LOSData` and `BoardMetadata.xml` files. Using the packaged archive instead of the unpacked source would not provide a richer classification. The checked LOSData blob is `36eabe2131733ca6f1f81ea0042c8de3edd8ea0d`, matching the oracle fixture's recorded LOSData identity.

### VASL paths investigated

| Path | Behavior found | Does it supply the missing ordinary T3 classification? |
| --- | --- | --- |
| `BoardArchive.addLOSDatatoVASLMap` | Reads grid elevation/code bytes, stairways and board annotations, then resets hex terrain. | No separate classification field is loaded. |
| `Hex.resetTerrain` | Samples terrain and elevation near the center, applies building fallbacks, derives hexside terrain, sets base elevation, then processes depression/inherent/bridge cases. | Produces the same center-based result our derivation reproduces. |
| `Hex.setDepressionTerrain()` | Sets depression from a depression-classified center, or preserves an existing depression. Its null-depression branch does nothing. | A Dirt Road center with no existing depression remains without one. |
| `Hex.getDepressionTerrain()` | Private helper scans cells inside the hex border and returns the first depression terrain found. | This is a broader terrain lookup, but its callers are in the bridge-center branch of `fixBridgesTunnelWater`; it is not called to classify an ordinary road center. |
| `Hex.fixBridgesTunnelWater` | Uses the scan to create the depression Location beneath a bridge. | The path is guarded by bridge/tunnel presence and does not solve the ordinary T3 road case. Our C# bridge derivation already mirrors this scan. |
| `Hex.resetHexsideTerrain` | Assigns depression to a hexside Location when its sampled terrain is a depression. | T3's sampled road crossings are Dirt Road; its six depression fields remain null. |
| `LOSDataEditor.fixElevatedSunkenRoads` | Processes cells already coded Sunken Road. Converts them to Elevated Road at elevation 1 when the hex base is 1; otherwise sets their elevation to -1. | Fixes grid codes/elevations during generation. It does not create a complete per-hex terrain identity or relabel Dirt Road cells. |
| `ASLMap.setOverlayTerrainType` | Can assign center terrain and a depression field while processing an overlay, based on the overlay terrain and nearest Location. | Relevant to explicit overlay scenarios, not evidence of a missed baseline Board 14 classification. No such overlay was applied in this investigation. |
| `Hex.isDepressionTerrain` / `Location.isDepressionTerrain` | The hex delegates to its center Location; the Location checks whether its depression field is non-null. | These predicates do not independently scan for a sunken road elsewhere in the hex. |

The public depression setters are mutation operations, not queries that discover the missing terrain identity. The private scan's “first depression found” behavior also should not be adopted as a general complex-hex classifier without specifying which features apply to the selected Location.

### Comparison with our implementation

Our `LosDataCodec` reads the same header, per-cell elevation/code pairs, and per-hex stairways. It checks the expected payload size, rather than silently skipping an unmodeled per-hex terrain section.

Our `CenterTerrainSampler` reproduces VASL's near-center sample and building fallbacks. `VaslCompatibleHexFactDerivation.SetDepressionTerrain` reproduces the ordinary depression assignment. The bridge branch already uses `BorderContains(... IsDepression ...)`, corresponding to VASL's private hex-wide scan.

The stored Board 14 fixture was generated through VASL's own Java map/hex classes. Its T3 center is Dirt Road, its base is -1, and its depression is null, matching the current C# result and passing F2. The fixture's VASL commit matches the checkout investigated here. No new Java fixture was generated during this follow-up; this is a code/data trace supported by the existing oracle and the earlier current C# import probe, not a newly launched interactive VASL session.

**Therefore, the evidence points to an upstream representation limitation for the requested chart-level identity, rather than a dropped metadata field or an omitted ordinary VASL classification step.**

### Consequences for the Hex Property Editor

The proposed **Terrain type** field should not simply rename **Center terrain**. For this example, those fields answer different questions:

| Proposed information | T3 value | Basis |
| --- | --- | --- |
| ASL terrain type | Sunken Road | B4 and the chart's explicit 14T3 example; a general classifier is not yet implemented. |
| Sampled center terrain | Dirt Road | Imported grid code and the VASL-compatible center derivation. |
| Base elevation | -1 | Imported elevation grid. |
| VASL-derived depression field | None | Current derivation and matching oracle. This is not a statement that T3 physically lacks a depression. |

The existing VASL data is sufficient evidence that Sunken Road terrain is present in the hex; determining its relationship to a selected point or Location requires an explicit interpretation beyond the ordinary center sample. A general rule must account for complex hexes rather than infer “Sunken Road” from every Dirt Road at elevation -1, or from any depression pixel anywhere in the hex.

Recommended next design step: define a separate, provenance-bearing ASL terrain classification for the inspector, using the imported terrain/elevation data and explicit combination rules or reviewed annotations. Retain the raw VASL-derived fields so disagreements remain inspectable. Validate the new classification against the chart examples and appropriate counter/SSR/overlay cases before connecting it to gameplay rules.

This is a design recommendation, not an implementation change. An inspector label and an LOS correction are separate changes. A null depression flag may affect specific depression checks, but the LOS engine also reads elevations and terrain along the line; it does not follow that every LOS from T3 is wrong. The clear T3-to-W5 line in the user's screenshot is consistent with the B4 example. Rule-specific LOS tests are still needed before changing those calculations.

### Reproducible source references

The following VASL paths are relative to `E:/Archive/GitHub/dlandi/vasl` at the commit above. Line numbers refer to that inspected checkout.

| File and line | Evidence |
| --- | --- |
| `dist/boardData/SharedBoardMetadata.xml:731` | `DirtRdL_1` maps the central roadbed color to Dirt Road at elevation -1. |
| `dist/boardData/SharedBoardMetadata.xml:1269` | Sunken Road contour palette mappings. |
| `dist/boardData/SharedBoardMetadata.xml:1809` | Sunken Road terrain definition, code 68, Depression category. |
| `boards/src/bd14/BoardMetadata.xml:26` | Board 14 metadata; no per-hex Sunken Road assignment. |
| `src/VASL/build/module/map/boardArchive/BoardArchive.java:272` | Current board-loading path, `addLOSDatatoVASLMap`. |
| `src/VASL/build/module/map/boardArchive/BoardArchive.java:639` | `writeLOSData`, showing the complete serialized fields. |
| `src/VASL/LOS/Map/Hex.java:656` | `resetTerrain`, including near-center sampling and subsequent derivation. |
| `src/VASL/LOS/Map/Hex.java:783` | `getnearcenterLocationTerrain`. |
| `src/VASL/LOS/Map/Hex.java:857` | Bridge/tunnel-presence guard. |
| `src/VASL/LOS/Map/Hex.java:881` | Bridge/depression Location construction. |
| `src/VASL/LOS/Map/Hex.java:1099` | Private hex-wide depression scan. |
| `src/VASL/LOS/Map/Hex.java:1119` | Ordinary center depression assignment. |
| `src/VASL/LOS/Map/Hex.java:1158` | Hexside depression assignment. |
| `src/VASL/LOS/Map/Location.java:85` | Depression predicate checks the stored field. |
| `src/VASL/LOS/LOSDataEditor.java:360` | Editor's Sunken/Elevated Road grid post-processing. |
| `src/VASL/build/module/ASLMap.java:1468` | Overlay-specific terrain/depression assignment. |
| `src/VASL/LOS/Map/Map.java:2261` | LOS depression-exit setup depends on the source depression predicate. |

Corresponding local project evidence: `LosDataCodec.cs`, `CenterTerrainSampler.cs`, `VaslCompatibleHexFactDerivation.cs` (ordinary depression assignment and bridge branch), `tools/vasl-hexfact-oracle/README.md`, and `tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bd14.hexfacts.json.gz`, all under `src/ASL` in their respective project directories.

## Findings to carry forward

1. **Sunken Road: chart-level identity.** 14T3 retains and renders the terrain but omits the depression from derived facts. The VASL follow-up found no dropped per-hex classification; this is also the ordinary upstream derivation's result. Define the chart-level classification separately, with provenance, and test relevant rules independently of the existing VASL oracle.
2. **Runway: imported grid omission.** 14M6's runway is in the source artwork but absent from the terrain grid. Rendering the existing grid cannot recreate an absent Runway classification.
3. **Legacy map ingestion.** The supplied local PB and ST archives cannot enter the current import pipeline. Their chart examples have been visually confirmed, but not evaluated through Studio's facts or rendering.
4. **Compound and additional-Location cases.** Elevated Road, Shellholes, Stream-Woods, and Sewer need appropriate representation/rules checks. Their center names alone are insufficient to classify overall support.
5. **Conditional examples.** Bocage, Mudflat, seasonal terrain, SSR terrain, counters, recorded terrain, and overlays require explicit test conditions. A base-map comparison is not sufficient evidence of a missing implementation.
6. **Valley elevation.** Keep 24P8 open for rule/elevation interpretation rather than assuming every Open Ground/base-0 result contradicts valley terrain.

## Implementation evidence

- `LimboDancer.Domains.Asl.Maps.Rendering/BoardRenderer.cs`, `WriteExactTerrain`: draws grid outline regions by terrain code and elevation, independently of center facts.
- `LimboDancer.Domains.Asl.Maps/Derivation/CenterTerrainSampler.cs`, `Sample`: near-center terrain sampling with building fallbacks.
- `LimboDancer.Domains.Asl.Maps/Derivation/VaslCompatibleHexFactDerivation.cs`, `SetDepressionTerrain`: center depression assignment/preservation.
- `LimboDancer.Domains.Asl.Maps/Derivation/HexFacts.cs`: separate center, Locations, hexsides, elevation, and bridge fields.
- `LimboDancer.Domains.Asl.Maps/Features/FeatureCompiler.cs`: existing Sunken Road handling, including elevation and conversion to Elevated Road in the applicable compiled hexes.
- `LimboDancer.Domains.Asl.Maps.Vasl/HexFactFidelity.cs`: F2 compares derived facts with VASL oracle facts.
- `LimboDancer.Domains.Asl.Maps.Vasl/VaslBoardImporter.cs`, `CheckScope`: rejects boards without `BoardMetadata.xml` as legacy V5 boards.

No implementation behavior or live game data was changed for this audit.
