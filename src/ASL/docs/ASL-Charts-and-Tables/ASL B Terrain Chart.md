# ASL B. Terrain Chart

## Source and scope

This is a transcription of the two supplied images: **B. TERRAIN CHART** and its symbol/abbreviation legend. They appear together on PDF page 160 (one-based) of `eASLRB_v3_01.pdf`, 716 pages, in the local rulebook at `E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf`. The PDF production label identifies the chart as `DB4_Terrain Chart_2024`, dated April 5, 2024.

The attached images are the transcription source; the PDF provides a cross-check. This document preserves the chart for analysis, rather than replacing the referenced rules. The reverse-side Railroad Movement Costs Chart and other reference tables on PDF page 161 are outside these two attachments and are not transcribed here.

Related analysis: [Terrain Chart Example Audit](ASL%20Terrain%20Chart%20Example%20Audit.md) checks the referenced maps against the current grid, rendering approach, and derived hex facts.

## How to read this transcription

- Terrain numbers refer to Chapter B unless another chapter precedes the number, as in `D10. Wreck`. Repeated numbers are intentional.
- The original wide table is split into three tables below. Match rows by their complete terrain labels, not just their rule numbers.
- `-` represents a dash in the source, meaning no value is stated in that cell. It is not substituted for `0`, `NA`, or `DOT`.
- Asterisks, brackets, slashes, symbols, and case-sensitive letter codes are retained. Asterisks are escaped where necessary so Markdown displays them literally.
- `<br>` within a table cell separates stacked source lines. Square brackets retain the source's alternative or qualified entries; they are not optional values selected by this transcription.
- Fractions are written as `1/2` and `1 1/2`. Ranges such as `1-3 1/2 Levels` retain their source meaning. A slash can separate costs, alternatives, or direct/indirect TEM, depending on the column.
- The source groups Infantry, Cavalry, and Horse Drawn under **MF entrance costs**; Motorcycle, Armored Car, Fully Tracked, Halftrack, and Truck fall under **MP entrance cost**.
- Red and underlined terrain labels carry rules information. Their meanings and the affected label text are recorded explicitly below, so they do not depend on Markdown color or underline support.
- Blank source Notes cells are represented by `-`. Local notes remain attached to their terrain row; their asterisks are not global footnote numbers.

## Legend supplied with the chart

| Mark or code | Meaning |
| --- | --- |
| Red terrain text | Concealment Terrain (A12.12). See the formatting table below. |
| Underlined terrain text | Confers -1 Rally DRM (A10.61). See the formatting table below. |
| † | Indirect Fire TEM is listed following a `/` only if different from Direct Fire TEM. |
| \*, \*\*, \*\*\* | See the terrain row's Notes entry. |
| ■ | Whole hex affects LOS, not the terrain depiction (Inherent Terrain; B.6). |
| @ | May not enter during APh. |
| ◆ | Deep Stream: Infantry must become CX; Motorcycles may not enter. |
| © | Not cumulative with terrain in same hex [EXC: LOS Hindrance DRM]. |
| B | Requires Bog DR to enter/change-VCA-within. |
| BB | Requires Bog DR to exit via non-depression hexside. |
| C | Cavalry may not charge. |
| COT | Cost of Terrain. |
| D | All MP penalties for entering a hex containing a wreck/vehicle or changing VCA are doubled. |
| DOT | Dependent on Other Terrain in hex. |
| f | +2 DRM for Entrenching Attempt on Desert Board (F.1) unless Sand is present; F1B. |
| FFMO | -1 DRM vs Moving Infantry in Open Ground. |
| h | MF cost of each full level higher elevation entered is doubled [EXC: changing levels within a building costs 1 MF]. |
| H | Add 4 MP for each full level higher elevation entered [EXC: via road add 2 MP]. |
| M | Minimum Move required. |
| NA | Not Allowed. |
| P | May be Pushed. |
| Pv | If Paved. |
| R | Or per road cost if through Road/Runway, or track cost if through track, hexside. |
| W | Entry as per wall/hedge. |
| X | Requires Wreck Check dr. |
| Y | Crossable only via Minimum Move, Low Crawl, or Advance vs Difficult Terrain. |
| Z | Half of MP allotment. |
| z | One-third of MP allotment. |
| zz | One-quarter of MP allotment. |

### Terrain-name formatting with rules meaning

The following table records the visible formatting of the terrain-name text. Labels not listed have neither red nor underlined terrain-name text in the supplied chart. For compound labels, the styled portion is identified explicitly. These are source-formatting observations, not an independent ruling about every possible terrain combination.

| Terrain row | Red label text: Concealment Terrain | Underlined label text: -1 Rally DRM |
| --- | --- | --- |
| 12. Brush | Brush | - |
| 12.7 Vineyard | Vineyard | - |
| 13. Woods | Woods | Woods |
| 14. Orchard | Orchard | - |
| 14.7 Cactus Patch | Cactus Patch | - |
| 14.8 Olive Grove | Olive Grove | - |
| 15. Grain | Grain | - |
| 16. Marsh [Mudflat] | Marsh | - |
| 23. Wooden Building | Wooden Building | Wooden Building |
| 23. Stone Building | Stone Building | Stone Building |
| 24. Rubble | Rubble | Rubble |
| 30. Pillbox | Pillbox | Pillbox |
| 32.5 Rail Cars | Rail Cars | - |
| 32.57 Wrecked Rail Cars | Wrecked Rail Cars | - |
| 33. Stream-Woods | Woods | Woods |
| 33. Stream-Brush | Brush | - |
| 33. Stream-Orchard | Orchard | - |
| 35. Light Woods | Light Woods | Light Woods |
| 36. PFZ Vineyard | PFZ Vineyard | - |
| 37. Debris | Debris | - |

## Terrain, LOS, and protection

The `TEM/Indirect†` column preserves the chart notation. The slash convention in the legend applies to this column, not to movement costs or kindling/spread numbers.

| Terrain | Example | LOS obstacle / hindrance | TEM/Indirect† | Kindle # / Spread # | Fortifiable |
| --- | --- | --- | --- | --- | --- |
| 1. Open Ground | 1B1 | - | FFMO: -1\* | - | Yes |
| 2. Shellholes | 2U6 | - | +1 ©\* | - | Yes |
| 3. Road | 1Y10/1Z8 | - | DOT\* | - | Pv No Ent/HIP Mines |
| 4. Sunken Road | 14T3 | Depression | FFMO: -1\* | - | No Entrench |
| 5. Elevated Road | 13L5 | Level-One | FFMO: -1\* | - | No Entrench |
| 6. Bridge | 5Y8 | Hindrance | FFMO: -1\*/+1 | - | No Ent/HIP Mines |
| 7. Runway | 14M6 | - | -1\* | - | Wire & Roadblock only |
| 8. Sewer | 1D5/1E4 | -\* | -2/NA | - | No |
| 9. Wall | 2H1/2I1 | Half-Level | +2/+1 © | - | - |
| 9. Hedge | 2T1/2U2 | Half-Level | +1/0 © | - | - |
| 9.5 Bocage | 2T1/2U2 | Level-One | +2/+1 © | - | - |
| 9.6 Hillside Wall/Hedge | 25X4-X5<br>25U3-U4 | Half-Level | +2 or +1\* ©<br>/+1 or 0\* © | - | - |
| 9.7 Cactus Hedge | SSR | Half-Level | +1/0 © | - | - |
| 10. Hill | 2E8 | 1-4 Levels | DOT\* | - | Yes |
| 11. Cliff | 2W5/2V4 | - | -2/NA\* | - | - |
| 12. Brush | 12AA10 | Hindrance | 0 | 9/6 | Yes |
| 12.7 Vineyard | SSR | ■ Hindrance | 0 | 9/6 | Yes f |
| 13. Woods | 1C9 | Level-One | +1/-1 | 9/7 | Yes |
| 14. Orchard | 6F5 | ■ Level-One\*<br>or Hindrance\*\* | 0 | 11/9 | Yes |
| 14.7 Cactus Patch\* | SSR | ■ Half-Level | +1 | 12/10 | Yes f |
| 14.8 Olive Grove\* | SSR | ■ Level-One\*\*<br>or Hindrance | +1 | 11/9 | Yes f |
| 15. Grain | 3K9 | Hindrance\* | 0 | 10/6 | - |
| 16. Marsh [Mudflat] | 7G2 | Hindrance | 0\* | - | No |
| 17. Crag | 15X9 | ■ Hindrance | +1 | - | Wire only |
| 18. Graveyard | 12W4 | ■ Hindrance | +1 | - | Yes |
| 19. Gully | 5Y3 | Depression | DOT | - | Yes |
| 20. Stream | 13N6 | Depression | DOT | - | Mine/Wire only |
| 21. Water Obstacle | 7E2 | Level -1 | FFMO: -1\* | - | No |
| 22. Valley | 24P8 | Level -1 | DOT | - | Yes |
| 23. Wooden Building | 1C7 | 1-3 1/2 Levels | +2(+1\*) | 7/8 | Mines only |
| 23. Stone Building | 1J2 | 1-3 1/2 Levels | +3(+1\*) | 8/9 | Mines only |
| 24. Rubble | Counter | ■ Half-Level | +2 or +3\* | \* | No |
| 25. Fire (Blaze) | Counter | ■ Smoke | DOT\* | - | - |
| 26. Wire | Counter | - | DOT | - | - |
| 27. Entrenchment [Trench] | Counter | - | +2/+4 © | - | - |
| 28. Minefield | Recorded | - | DOT\* | - | - |
| 29. Roadblock | Counter | Half-Level | +2/+1 | - | - |
| 30. Pillbox | Counter | - | LOS\* | - | - |
| 32. Railroads (GLRR/EmRR/ElRR/SuRR) | Overlays | -/Half-Level/Level-One/Depression | FFMO: -1\* | - | No Entrench |
| 32.5 Rail Cars | Overlays/Counter | Level-One/<br>■ Level-One\* | +2 | 8/9 | No Entrench |
| 32.57 Wrecked Rail Cars | Counter | ■ Half-Level | +2 | 7/8 | No |
| 33. Stream-Woods | 47F6 | Level-One<br>[-] | +1/-1\*<br>[FFMO: -1] | 9/7 | Mine/Wire only |
| 33. Stream-Brush | StMM11 | Hindrance<br>[-] | 0\*<br>[FFMO: -1] | 9/6 | Mine/Wire only |
| 33. Stream-Orchard | StLL14 | Level-One\* or Hindrance\*\*<br>[-] | 0\*\*\*<br>[FFMO: -1] | 11/9 | Mine/Wire only |
| 34. Tower hex [Tower Location] | PB C9 | SSR | DOT<br>[0\*] | DOT<br>[7/8] | Mines only<br>[No] |
| 35. Light Woods | SSR | Level-One\*<br>or +2 Hindrance | +1/-1 | 9/7 | Yes |
| 36. PFZ Vineyard | Counter | ■ Hindrance | 0 | 9/6 | Yes f |
| 36. PFZ Open Ground | Counter | - | FFMO: -1\* | - | Yes |
| 37. Debris | Counter | ■ Hindrance | +1 | - | Yes |
| D10. Wreck | Counter | ■ Hindrance | +1 © | - | - |

## Movement entrance costs

The first three cost columns use MF; the remaining five use MP. The source's `36. Prepared Fire Zone` heading is represented by its two child rows, `36. PFZ Vineyard` and `36. PFZ Open Ground`.

| Terrain | Infantry (MF) | Cavalry (MF) | Horse Drawn (MF) | Motorcycle (MP) | Armored Car (MP) | Fully Tracked (MP) | Halftrack (MP) | Truck (MP) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 1. Open Ground | 1 | 1 | 1 | 3 | 3 | 1 | 1 | 4 |
| 2. Shellholes | 1 or 2\* | 2 | 2 | 2 + COT X | 2 + COT | COT | COT | 4 + COT |
| 3. Road | 1 | 1 | 1 | 1/2 | 1/2 [BU:1] | 1/2 [BU:1] | 1/2 [BU:1] | 1/2 [BU:1] |
| 4. Sunken Road | 2 R | 2 R | NA R D | NA R | NA R D | NA R D | NA R D | NA R D |
| 5. Elevated Road | 2 R | 2 R | NA R D | 6 X R P | NA R D | 5 R D | 5 R D | NA R D |
| 6. Bridge | NA R | NA R | NA R | NA R | NA R D | NA R D | NA R D | NA R D |
| 7. Runway | 1 R | 1 R | 1 R | 3 R | 3 R | 1 R | 1 R | 4 R |
| 8. Sewer | ALL@ | NA | NA | NA | NA | NA | NA | NA |
| 9. Wall | 1 + COT | 1 + COT | NA | NA P | NA | 1 + COT | NA | NA |
| 9. Hedge | 1 + COT | 1 + COT | NA | NA P | 3 + COT B | 1 + COT | 2 + COT B | NA |
| 9.5 Bocage | 2 + COT | NA | NA | NA | NA | Z + COT B | NA | NA |
| 9.6 Hillside Wall/Hedge | W | W | NA | W | W | W | W | NA |
| 9.7 Cactus Hedge | Y | NA | NA | W | W | W | W | NA |
| 10. Hill | DOT h | DOT h | DOT h | DOT H | DOT H | DOT H | DOT H | DOT H |
| 11. Cliff | CLIMB | NA | NA | NA | NA | NA | NA | NA |
| 12. Brush | 2 R | 2 R | 2 R | 4 R | 4 R | 2 R | 2 R | 6 R |
| 12.7 Vineyard | 2 R | 2 R | 2 R | 4 R | 4 B R | 2 B R | 2 B R | 6 B R |
| 13. Woods | 2 R | 4 C R | ALL B R | NA P R | ALL B R | ALL B\*/Z D R | ALL B R | ALL B R |
| 14. Orchard | 1 | 1 | 1 | 3 R | 3 R | 1 R | 1 R | 4 R |
| 14.7 Cactus Patch\* | 3 R | 3 R | 3 R | 9 R | 9 R | 3 R | 3 R | 12 R |
| 14.8 Olive Grove\* | 2 R | 2 R | 2 R | 6 R | 6 R | 2 R | 2 R | 8 R |
| 15. Grain | 1 1/2 | 1 1/2 | 1 1/2 | 4 | 4 | 1 | 1 | 5 |
| 16. Marsh [Mudflat] | ALL@ [2] | ALL C [2] | NA | NA [P] | NA | NA | NA | NA |
| 17. Crag | 2 | 4 C | NA | NA | NA | NA | NA | NA |
| 18. Graveyard | 1 | 2 C | NA [1] | 4 [1] | NA [1] | Z B [1] | NA [1] | NA [1] |
| 19. Gully | 2\* | 2\* | ALL | 4 + COT | 4 + COT | 2 + COT | 3 + COT | 6 + COT BB |
| 20. Stream | \*2/3/4 ◆ | \*2/3/4 | ALL BB | 4 + COT X ◆ | 4 + COT BB | 2 + COT BB | 3 + COT BB | 6 + COT BB |
| 21. Water Obstacle | ALL\*@ | ALL\* | ALL\* | NA | NA | NA | NA | NA |
| 22. Valley | DOT | DOT | DOT | DOT | DOT | DOT | DOT | DOT |
| 23. Wooden Building | 2 | NA | NA | NA P | NA | Z B | NA | NA |
| 23. Stone Building | 2 | NA | NA | NA P | NA | Z B | NA | NA |
| 24. Rubble | 3 | NA | NA | NA P | NA | Z B | NA | NA |
| 25. Fire (Blaze) | NA | NA | NA | NA | NA | NA | NA | NA |
| 26. Wire | COT | NA | NA | NA | 4 + COT B | 2 + COT B | 4 + COT B | 4 + COT B |
| 27. Entrenchment [Trench] | COT\* | 1 + COT | 1 + COT<br>[NA] | 2 + COT<br>[NA] | 2 + COT<br>[NA] | COT<br>[B] | COT<br>[NA] | 4 + COT<br>[NA] |
| 28. Minefield | COT | COT | COT | COT | COT | COT | COT | COT |
| 29. Roadblock | 1 + COT | 1 + COT | NA | NA | NA | NA | NA | NA |
| 30. Pillbox | COT\*\* | COT | COT | COT | COT | COT | COT | COT |
| 32. Railroads (GLRR/EmRR/ElRR/SuRR) | See RR chart | See RR chart | See RR chart | See RR chart | See RR chart | See RR chart | See RR chart | See RR chart |
| 32.5 Rail Cars | 2 | NA | NA | NA P | NA | NA | NA | NA |
| 32.57 Wrecked Rail Cars | 3 | NA | NA | NA P | NA | Z B | NA | NA |
| 33. Stream-Woods | 4/5/6<br>[2/3/4]◆ | 4/5/6<br>[2/3/4] | M<br>[ALL] | NA P ◆<br>[7] | M<br>[7] | M B/Z + 3<br>[3] | M<br>[4] | M<br>[10] |
| 33. Stream-Brush | 4/5/6<br>[2/3/4]◆ | 4/5/6<br>[2/3/4] | M<br>[ALL] | 11 ◆<br>[7] | 11<br>[7] | 5<br>[3] | 6<br>[4] | 16<br>[10] |
| 33. Stream-Orchard | 3/4/5<br>[2/3/4]◆ | 3/4/5<br>[2/3/4] | M<br>[ALL] | 10 ◆<br>[7] | 10<br>[7] | 4<br>[3] | 5<br>[4] | 14<br>[10] |
| 34. Tower hex [Tower Location] | 1 + COT<br>[1 per level] | NA | NA | NA P | NA | Z B | NA | NA |
| 35. Light Woods | 2 R | 4 C R | ALL B R | NA P R | ALL B R | ALL B\*\*/z D R | ALL B\*\*/z D R | ALL B R |
| 36. PFZ Vineyard | 2 R | 2 R | 2 R | 4 R | 4 B R | 2 B R | 2 B R | 6 B R |
| 36. PFZ Open Ground | 1 | 1 | 1 | 3 | 3 | 1 | 1 | 4 |
| 37. Debris | 2 | NA | NA | NA P | NA | zz B | NA | NA |
| D10. Wreck | COT | COT | DOT +1\* | DOT | DOT +1\* | DOT +1\* | DOT +1\* | DOT +1\* |

`See RR chart` preserves the source's instruction spanning all movement columns: "See Railroad Movement Chart on the reverse side of this chart." It supplies no numeric railroad movement costs here.

## Terrain-specific notes

| Terrain | Notes from the chart |
| --- | --- |
| 1. Open Ground | NA if Height Advantage applies. |
| 2. Shellholes | Treat as OG if entered at 1 MF. |
| 3. Road | FFMO if entered at road rate. |
| 4. Sunken Road | vs unit without Crest status. |
| 5. Elevated Road | If Height Advantage NA. |
| 6. Bridge | FFMO if LOS is thru road depiction; otherwise +1; TEM: +1. |
| 7. Runway | In any fire phase; NA vs armor. |
| 8. Sewer | LOS to adjacent sewer hex only. |
| 9. Wall | - |
| 9. Hedge | - |
| 9.5 Bocage | - |
| 9.6 Hillside Wall/Hedge | Wall/hedge respectively. Wall Advan and TEM NA to lower unit. |
| 9.7 Cactus Hedge | Wall/hedge hexsides. |
| 10. Hill | +1 HA TEM if no other TEM. |
| 11. Cliff | vs climber; otherwise DOT. |
| 12. Brush | Deep Snow becomes Open Ground. |
| 12.7 Vineyard | - |
| 13. Woods | If no road, VBM, or TB. |
| 14. Orchard | \*To higher LOS only in Apr-Oct.<br>\*\*Max. Hindrance +1 with 2 Level advantage. |
| 14.7 Cactus Patch\* | "Ex-orchard" hexes. Always in season. |
| 14.8 Olive Grove\* | "Ex-orchard" hexes. Always in season.<br>\*\*To higher LOS only. |
| 15. Grain | June-Sept only; MF/MP Apr-Sept. |
| 16. Marsh [Mudflat] | HE FP halved; [Mudflat only]. |
| 17. Crag | - |
| 18. Graveyard | [via Graveyard road hexside only]. |
| 19. Gully | +COT if not Open Ground. |
| 20. Stream | Dry/Shallow/Deep. |
| 21. Water Obstacle | Only if Fordable (B20.8). |
| 22. Valley | Note h and H when moving higher. |
| 23. Wooden Building | Indirect Fire adds +1/level above target. |
| 23. Stone Building | Move assumes no road or VBM. |
| 24. Rubble | Same as Wood or Stone Building. |
| 25. Fire (Blaze) | +3 for Smoke; +2 if Burning Wreck. |
| 26. Wire | Exit only in MPh/RtPh. |
| 27. Entrenchment [Trench] | 1 MF enter/exit beneath. |
| 28. Minefield | TEM NA to mine attack. |
| 29. Roadblock | Connects to adjacent building/woods. |
| 30. Pillbox | \*Based on type & LOS.<br>\*\*Costs 1 MF extra to enter/exit beneath. |
| 32. Railroads (GLRR/EmRR/ElRR/SuRR) | If Height-Advantage/Crest-status are NA. |
| 32.5 Rail Cars | Move assumes no VBM.<br>\*Exclusive of hexsides (unless adjoined). |
| 32.57 Wrecked Rail Cars | Represented by a Wooden Rubble counter. |
| 33. Stream-Woods | Vs unit without Crest status [if IN stream (and LOS crosses stream hexside)]. |
| 33. Stream-Brush | Vs unit without Crest status [if IN stream (and LOS crosses stream hexside)]. |
| 33. Stream-Orchard | \*To higher LOS only in Apr-Oct.<br>\*\*Max. Hindrance +1 with 2 level advantage.<br>\*\*\*Vs unit without Crest status [if IN stream (and LOS crosses stream hexside)]. |
| 34. Tower hex [Tower Location] | If Height Advantage NA. |
| 35. Light Woods | \*To higher LOS only.<br>\*\*If no road, VBM, or TB. |
| 36. PFZ Vineyard | - |
| 36. PFZ Open Ground | NA if Height Advantage applies. |
| 37. Debris | - |
| D10. Wreck | Per Vehicle/Wreck; +2 if enter via road. |

## Reading aids and limits for later analysis

These aids are separate from the supplied legend. They expand common abbreviations appearing in the chart without adding new terrain rules.

| Abbreviation | Reading aid |
| --- | --- |
| LOS | Line of Sight. |
| TEM | Terrain Effects Modifier. |
| MF / MP | Movement Factors / Movement Points. |
| DRM / dr / DR | Dice Roll Modifier / one die roll / two dice roll. Preserve the distinction between `dr` and `DR`. |
| APh / MPh / RtPh | Advance Phase / Movement Phase / Rout Phase. |
| BU / CX | Buttoned Up / Counter Exhausted. |
| VCA / VBM | Vehicular Covered Arc / Vehicular Bypass Movement. |
| TB | Trail Break. |
| OG / HA | Open Ground / Height Advantage. |
| HE / FP | High Explosive / Firepower. |
| SSR / EXC | Scenario Special Rule / Exception. |
| Ent / Entrench / HIP | Entrenchment / Entrenchment / Hidden Initial Placement. |
| PFZ | Prepared Fire Zone. |
| GLRR / EmRR / ElRR / SuRR | Ground-Level Railroad / Embankment Railroad / Elevated Railroad / Sunken Railroad. |

Analysis should retain these distinctions:

- `NA R`, `NA P`, and similar combinations are qualified entries. Do not discard the suffixes when interpreting `NA`.
- `Z`, `z`, and `zz` are different fractions of MP allotment. `h` and `H` also encode different movement effects.
- `ALL`, `M`, `CLIMB`, and numeric costs are distinct source entries. The supplied legend defines `M`; it does not spell out the complete rules for `ALL` or `CLIMB`.
- A `■` marker changes how LOS treats the terrain's extent. Its presence is independent of the concealment and rally formatting.
- Seasonal, elevation, road, stream, and other conditions belong with their entries. They should not be flattened into unconditional terrain properties.
- The table includes hex terrain, hexside terrain, counters, and Locations. It should not be assumed that every row describes a mutually exclusive hex type.
- The chart prints the wooden-building and stone-building notes on separate rows. Their visible placement is preserved here; consult B23 before deciding the scope of those notes across building types.
- The chart does not fully explain every bracketed movement alternative in its legend. Preserve those alternatives and consult the referenced rules before deriving executable conditions from them.
