# ASL Map Studio Counter Layer Design

**Written:** 2026-10-18, for the second group of the SVG memory analysis: options C, D, E, and Q, with F decided after them.
**Source:** `src/ASL/docs/Designs/ASL Map Studio SVG Memory Analysis.md` (the analysis; on branch `svg-memory-analysis`, pull request 19, not yet on `main`): its sections 5 (options C, D, E, F, Q), 7 (the order), 8 (the rulings), and 10 (the results of the first group).
**Code read:** `Units.Rendering` (`UnitRenderer.WriteUnit` and `WriteFace`, `Glyphs.ArtFigures` and `FigureId`, `UnitOverlayBuilder.Build`, `UnitPreview.Document`), `MapStudio` (`BoardWorkspace` and its `sent` record, `BoardViewport.SetUnits`, `GameMaps.Layers`, `GameMaps.Named` and `Led`, `UnitLibrary.Renderer` and `Overlay`), `wwwroot/js/boardViewport.js` (`setUnits`, `applyPatch`, `updateUnitTier`, `setVisible`, `applyComparison`), `site.css` (the counter rules), and the tests that read the layer.
**Status:** proposed; not approved. No code is changed. The implementation plan is `src/ASL/docs/Plans/ASL Map Studio Counter Layer Plan.md`.
**Scope:** rendering only. Nothing here touches the Rules project, a projection, or what a counter shows; every ASL rule stays where it is.

## 1. Goal

Make a replay step, or a play action, cost what changed on the map, not the whole counter layer.

Where it starts, after the first group (PR 19):

- Every projection builds the whole counter layer: 696,691 characters for 42 counters at step 13 of cd-guards-5, of which 329,481 (47%) are figure outline `<defs>` (243 paths, 6 distinct), and each counter is written three times, once per detail tier.
- `BoardWorkspace` compares the new layer string with the one it sent and, on any difference, sends all of it; `boardViewport.js` `setUnits` removes the old layer, parses the new string, and imports 4,425 elements.
- Most steps change 0 to 6 of the 42 counters (analysis, section 2.2).
- Task 11 put the counter layer at about 14 MB of a 21 MB replay step before the first group, and about 3 MB of a 9.4 MB step after it.

Two costs the analysis did not count, found while reading for this design:

- `GameMaps.Named`, through `GameMaps.Led`, rewrites the whole layer with a regular expression on every step to put each counter's hex and name in front of its accessible name and title. That is one more full copy of the layer string per step, and a match object per counter.
- `BoardWorkspace` compares the 700,000-character strings themselves (`Units?.Svg != sent.Units`) on every render of the component, not only on a step.

## 2. Decisions

| | Decision | Why |
|---|---|---|
| L1 | **Two modes of drawing a counter.** Standalone (a counter in its own document: `UnitPreview`, the Unit Lab, the Art tab, the 138 counter goldens) stays exactly as today, byte for byte. Layer mode (the map's counter layer) shares definitions and is drawn in parts. `WriteUnit` takes an optional layer context; without one it writes what it writes today. | The goldens remain a complete check of the counter drawing, and the previews keep their own `<defs>` (analysis, option C, risk). |
| L2 | **The unit of change is the hex.** A layer fragment holds every level's stack in one hex, with its count badges and level tabs. | A unit entering another level of a hex moves every level's stack in that hex (`offsetY` depends on the number of levels), so a hex-and-level fragment would change together with its siblings anyway. |
| L3 | **Shared definitions are named by their content.** A figure outline is `fig-{hash of its path}`; a face definition is `face-{hash of its markup}`. The hash is SHA-256 over the UTF-16 text, truncated to 96 bits (24 hex digits). | A content name never goes stale, needs no registry to stay unique across steps, and lets the browser keep a definition for as long as anything refers to it. 96 bits makes a collision not worth handling. |
| L4 | **The layer is a list of fragments; the string is assembled only when asked.** `UnitOverlay` carries its fragments in paint order and the layer's own attributes; `Svg` stays, built lazily, for the hosts that send the whole layer and for the tests. | D needs the parts; the board viewer, the hand-over map, and the tests keep working from the whole. |
| L5 | **A whole send stays the fallback.** Any doubt (nothing sent yet, a board load, another unit set, a patch the browser rejects, a patch larger than half the layer) sends the whole layer as today. | A patch that misses is never shown; the next step corrects nothing that a whole send would not. |
| L6 | **The leads go into the builder.** The text before each counter's name (its hex and the unit's own name, `GameMaps.Named`; the setup map and hand-over map's leads, `GameMaps.Led`) is passed to `UnitOverlayBuilder.Build`, which writes it once; the regular expression over the layer is retired. | A fragment must be final when built, or D and E would have to rewrite it after; and it removes a full copy of the layer per step. |

## 3. The keys

| Key | Form | Made from | Where it lives |
|---|---|---|---|
| Figure outline (C) | `fig-{24 hex}` | The outline's path data alone: one key per body, helmet, and weapon outline, not per combination | Layer `<defs>` (C1), then the viewport's figure sprite (C2) |
| Face definition (Q) | `face-{24 hex}` | The face's markup as drawn at the origin, for one tier | `<defs id="unit-faces">`, first child of the layer |
| Hex fragment (D) | element id `units-{column}-{row}` | The hex index of the composed map or board | The layer, in hex order |
| Fragment content (D) | 96-bit hash of the fragment's markup | Computed when the fragment is built | `UnitOverlay`, and the workspace's record of what the browser holds |
| Hex fragment cache (E) | the renderer's fingerprint, then the hex's layout inputs and each member's inputs, as one string | See section 7 | `UnitFragmentCache`, bounded |
| Face cache (E with Q) | the renderer's fingerprint, the document's JSON, the tier | See section 8 | The same cache |
| Layer version (D) | an integer, `data-units-version` on the layer | Incremented by the workspace for each whole send or patch | The layer element, and the workspace |

The renderer's fingerprint is the style sheet's hash (`StyleSheet.Hash`, already the key of `UnitLibrary.renderers`), the palette's name, `UnitRenderer.Version`, the vocabulary's content hash, and the figure art's content hash (both added where missing).

Today's `FigureId` (`fig-{32-bit FNV}-{unit}-{x}-{y}`) stays for standalone documents (L1). It carries the unit and the position because two previews of one unit in one page may draw different outlines; in layer mode every id is the content alone, so that reason does not apply.

## 4. C: each figure outline once

### C1: one `<defs>` per layer (first, per ruling 2)

- A layer context (`UnitLayerContext`, internal to `Units.Rendering`) collects outlines as the counters are written: `Glyphs.ArtFigures` asks it for the id of each outline instead of writing its own `<defs>`, and its `<use>` elements point at `#fig-{hash}`.
- After the last counter, `UnitOverlayBuilder` writes one `<defs data-figures>` at the end of the layer, holding each collected outline once. A `<use>` may refer to an element later in the document, so the order costs nothing, and the counters need not be buffered.
- The 243 outline paths of step 13 become 6; the layer falls from 696,691 to about 376,000 characters (analysis, option C).
- C1 is built before D, while the layer is still sent whole. It is replaced by C2 when D is built (section 6).

### C2: the viewport's figure sprite (when D is built, per ruling 2)

- With D, a step sends only some hexes, and a hex can bring an outline the browser does not have. Rather than patching the outlines too, the browser is given all of them once: the built-in figure art is 36,376 bytes of `figures.json` in all, every pose, helmet, and weapon of every side.
- The sprite is `<g id="unit-figures"><defs>` with one `<path id="fig-…">` per outline of the renderer's `FigureArt`, built once per figure art version and cached.
- It lives in the viewport beside the board's layers, not inside `layer-defs`: `layer-defs` comes from the board renderer (`Maps.Rendering`), which must not know counters, and is replaced on every board load. Its id has no `layer-` prefix, so `setVisible` and the comparison modes leave it alone.
- The viewport receives it through a new call, `setUnitFigures(markup)`, before the first unit layer after a load; a load removes it with everything else, and the workspace's record of what was sent is reset then, so it is sent again.
- In C2 the layer carries no outlines at all. The counter layer at step 13 falls by a further 8,856 characters; more to the point, no step ever sends an outline.

## 5. The leads in the builder (L6)

- `UnitOverlayBuilder.Build` takes an optional `Func<PlacedUnit, string>? lead`. Each counter's accessible name and title become `lead + AccessibleName + level suffix`, which is what `Led` produces today by rewriting.
- `UnitLibrary.Overlay` passes it through. `GameMaps.Layers` passes `Named`'s text; the Play page's setup map and the hand-over map pass theirs. `Led` is removed when no caller needs it.
- Escaping differs: `Led` encodes with `WebUtility.HtmlEncode` (which also encodes `'` and the characters from 160 to 255), the writer escapes only `&`, `<`, `>`, and `"`. Both are the same text to an XML parser. The check is therefore that the parsed attribute and title values are equal to `Led`'s for every fixture, not the bytes.

## 6. D: send only the hexes that changed

### On the server

- `UnitOverlayBuilder` writes each hex into its own fragment: `<g id="units-{c}-{r}" data-hex="{c},{r}">` around the hex's `data-stack` groups, in the order of today (levels ascending; members by `stackOrder`, then id; the count badge and the level tab after the members).
- `UnitOverlay` gains `Fragments` (id, markup, content hash, in paint order) and `Attributes` (`data-face-size`, `data-unit-set`). `Svg` is assembled from them on first use (L4).
- The browser's state is kept in one place, a `UnitLayerSync` in `MapStudio` that `BoardViewport` owns. Every host (`BoardWorkspace`, the board viewer, the hand-over map) shows units through `viewport.ShowUnits(overlay)` instead of `SetUnits(string)`, so the figure sprite and the version bookkeeping are never forgotten by one of them.
- `UnitLayerSync` records the version it sent, the set id, the face size, and the fragment ids with their hashes in order. For a new overlay it sends:
  - nothing, when the attributes and every fragment hash are the same (a step that changed no counter costs a comparison of 42 or fewer hashes, not of 700,000 characters);
  - a patch, otherwise: removals (ids gone), upserts (id, markup, and the id of the fragment it goes before), the new attributes, and `base` and `version`;
  - the whole layer, in the cases of L5.
- `BoardWorkspace.sent.Units` becomes this record. The comparison `Units?.Svg != sent.Units` goes away with it, and with it the assembling of `Svg` on every render.

### In the browser

- `applyUnitPatch(state, patch)` beside `setUnits`:
  1. If `#layer-units` is missing or its `data-units-version` is not `patch.base`, return `false`; the server then sends the whole layer.
  2. Note the focused counter, if focus is inside the layer (`document.activeElement.closest("[data-unit-id]")`).
  3. Remove each removal; parse each upsert (one `DOMParser` call for all of them, wrapped in one `<svg>`), and replace the fragment of that id or insert it before its `before`, or at the end of the layer.
  4. Set the layer attributes and `data-units-version`.
  5. Give focus back to the counter of the same `data-unit-id`, if one is still there (`focus({ preventScroll: true })`).
  6. Keep the highlight polygon last, as `setUnits` does; call `updateUnitTier`.
- The existing `applyPatch` for board layers (per-feature upserts and removals by id) is the model; the units get their own call because they need the version check and the focus step, and because they live in one layer that is never reloaded from a URL.
- `reportMeasuredScene` is not called by a patch; it reports the first scene only.

## 7. E: cache counter fragments on the server

- `UnitFragmentCache`, in `Units.Rendering`, one per `UnitLibrary` (a singleton in the Studio), passed to `Build`. Without it, `Build` renders everything as today.
- **The hex key** is one string: the renderer's fingerprint; the hex height; the hex center (as fixed-point text); the number of levels; and for each level and each member in order: the member's document as `UnitDocumentJson` writes it, whether it is shown, its lead, and its index in the stack. That is everything `Build` reads for a hex, so the fragment is a function of the key.
- **The value** is the fragment's markup, its content hash, and the warnings its counters raised. A hit replays the warnings with the same unit ids; the diagnostics and the `PlacedUnit` list are not cached, since they cost nothing to make.
- **The bound** is by characters: least recently used entries leave when the total passes 4 million characters (about 8 MB). One game view at step 13 needs about 0.4 million before Q and well under 0.1 million after it.
- **Cost of a key:** about 1 to 2 KB of JSON per counter per step, some 100 KB a step for 42 counters, against about 3 MB of rendering today.
- **Risk** (analysis, option E): a key that misses an input draws a counter in an old state. The key is built from the whole document rather than chosen fields, so a new document property is covered without a change here; what the document does not hold (sheet, palette, vocabulary, figure art) is in the fingerprint. Section 11 has the tests that would catch a miss.

## 8. Q: one definition per distinct face

### What changes in a counter's markup

Today (layer mode, after C):

```
<g data-unit-id="g3" role="button" tabindex="0" aria-label="…"><title>…</title>
  <g data-tier="far"> …the face, every coordinate offset by the counter's position… </g>
  <g data-tier="mid"> … </g>
  <g data-tier="near"> … </g>
</g>
```

With Q:

```
<defs id="unit-faces">
  <g id="face-…" data-face-def="near"> …the face drawn at the origin… </g>
</defs>
…
<g data-unit-id="g3" role="button" tabindex="0" aria-label="…"><title>…</title>
  <use data-tier="far" href="#face-…" x="263.515625" y="410.25"/>
  <use data-tier="mid" href="#face-…" x="263.515625" y="410.25"/>
  <use data-tier="near" href="#face-…" x="263.515625" y="410.25"/>
</g>
```

- `WriteFace` already takes the center as arguments; in Q's layer mode it is called with `(0, 0)` into a scratch writer, the markup is hashed, and it is written into `unit-faces` only if that id is new to the layer.
- The definitions are `<g>` in `<defs>`, not `<symbol>`: a symbol establishes a viewport that clips its content by default, and a covered arc or a badge reaches outside the face. A `<g>` neither clips nor needs a size.
- The definition must not carry `data-tier`, nor anything the page's style sheet selects on: document styles match the referenced element, not the instance, so `#layer-units [data-tier] { display: none }` would hide every copy. Hence `data-face-def`.
- A face definition must not name a unit. `data-attached-id` (attached equipment drawn inside its owner's face) is written in standalone mode only; nothing in the Studio reads it from the map, and the tests that read it draw standalone counters. The figure outlines already have content ids (C).
- Per counter, what remains is its group, its name, its title, and three `<use>` elements: about 300 to 450 characters, most of it the accessible name.
- Numbers: today each coordinate is rounded to 1/64 pixel after the position is added; with Q the position and the offset are rounded separately. The difference is at most 1/64 pixel, which no zoom of the Studio shows.

### The focus ring

`site.css` draws a focused counter's face edge in gold: `#layer-units [data-unit-id]:focus-visible [data-face] { stroke: #f0b400; stroke-width: 3px; }`. Inside a `<use>`, a selector cannot reach the copy of one instance. Inherited custom properties do reach it. So, in Q's layer mode only, the face shape (and an attached face's) writes its stroke as `style="stroke: var(--unit-focus-stroke, {stroke}); stroke-width: var(--unit-focus-width, {width})"`, and the rule becomes `#layer-units [data-unit-id]:focus-visible { --unit-focus-stroke: #f0b400; --unit-focus-width: 3px; }`. The look is unchanged; the standalone counter keeps its attributes.

The draft rule (`[data-unit-id^="draft:"] { opacity: 0.85 }`) and the cursor rule stay on the counter's group, outside the definition, and work as they do.

### With D

- A fragment records the face ids its counters use. `UnitLayerSync` records the face ids the browser holds.
- A patch carries `facesAdd` (id and markup, for faces used by an upsert and not yet held) and `facesRemove` (ids no fragment refers to any longer). `applyUnitPatch` adds faces before the upserts and removes the unused ones after them.
- A step that only moves a counter sends its hexes' fragments, a few hundred characters per counter in them, and no face. A step that breaks a squad sends one new face triple (the broken side) unless another counter already shows it.

### With E

A hex hit renders nothing. On a hex miss, each member's three faces come from the face cache, keyed by the renderer's fingerprint, the document's JSON, and the tier. The key holds the whole document, unit id included, because a sheet may print `attr(id)` on a face; two units with the same face therefore have two cache entries but one content id, so the page still holds the face once.

## 9. Stacking order

- Paint order is today's: hexes by column, then row; within a hex, levels ascending; within a level, `stackOrder`, then id; the count badge and the level tab after the members of their level. Q and C change none of it.
- Fragments are never reordered in place: a patch upsert of an existing id replaces it where it stands, and a new fragment is inserted before the next fragment in paint order that the browser holds (the server knows the list it sent). So the layer after any patch is in the same order as a whole send of the same overlay; the tests check exactly this (section 11).
- A big stack's offsets can reach over a neighbour hex; that overlap is decided by paint order between hexes, which is kept.
- The face definitions are the layer's first child; the figure sprite sits before the layer in the viewport; the LOS layer, the marks layer, and the highlight polygon stay above the units as today (the highlight is re-appended after each patch).

## 10. Selection, highlight, and tiers across patches

- **The selected unit** is the inspector's state on the server (`selectedUnit`, kept by unit id in `BoardWorkspace`), not a mark in the SVG; a patch does not touch it. The selected hex is the highlight polygon, a child of the board's SVG after the layer, which a patch leaves in place.
- **Keyboard focus** is the one piece of browser state inside a fragment; `applyUnitPatch` restores it by unit id (section 6). If the counter left the map, focus falls to the document as it does today on a whole send.
- **A drag in progress** keeps its unit id as text (`drag.unitId`), so replacing its node does not break the click that ends it.
- **Hover titles** are the browser's own and follow the new node.
- **The tier switch** stays in the browser: `updateUnitTier` sets `data-active-tier` on the layer element, which a patch never replaces, so a patch keeps the tier in view. A patch can change `data-face-size` (the smallest face on the map), so `applyUnitPatch` calls `updateUnitTier` after it. With Q, `[data-tier]` is on the `<use>` elements and the existing CSS selects them unchanged.
- **The side-by-side comparison** clones only the board's groups (`layer-defs`, `layer-grid`, `layer-labels`, the styled layers), never the counters or the sprite, so it is unaffected.

## 11. Tests

### The goldens

| Goldens | Impact |
|---|---|
| 138 counter goldens (`Units.Rendering.Tests/Golden`, 69 per sheet), all standalone | None. A changed counter golden is a defect, never a golden to update (L1). |
| 4 board goldens (`Maps.Rendering.Tests`) | None. |
| The counter layer | There are no layer goldens today, only structural assertions (`OverlayTests`, `DirectionTests`, `UnitStudioTests`, `GameStatesTests`). This design adds four: two fixtures (the nine demo counters on bd01; a crowded hex with levels, a cellar, more than six counters, attachments, facings, and a draft), each in both sheets. They are added with C1 and change on purpose at the leads, D, C2, and Q, each change reviewed in its commit; what proves the drawing unchanged is the equivalence test below, not the golden. |

### The equivalence oracle

A test helper expands a layer: every `<use>` is replaced by a copy of what it refers to (from the layer's own `<defs>`, the figure sprite, or the face definitions), translated by its `x` and `y`, and the definition ids are dropped. The expanded layer is compared, element by element and attribute by attribute, with the same counters drawn by the standalone path (L1), which does not change. Numbers compare exactly for C, and within 1/32 pixel for Q (section 8). This runs over both layer fixtures and over every revision of the game fixtures `MapStudio.Tests` already holds.

### By part

- **C:** every `href` in a layer resolves (to the layer's `<defs>` in C1, to the sprite in C2); each outline is written once; the sprite holds every outline of the built-in figure art once, with distinct ids.
- **Leads:** for each fixture, the parsed names and titles from `Build` with a lead equal those from today's `Led` (kept in the test project as the oracle).
- **D:** the fragments joined equal the whole `Svg`; for consecutive overlays A and B from the game fixtures, A's fragments with the patch from A to B applied equal B's fragments, in the same order (a C# model of `applyUnitPatch`); an unchanged overlay gives no patch; a set change or a face-size change is handled.
- **D in the browser** (`boardViewport.pointer.test.mjs`, which CI runs and the gate runs by hand): upserts land before their `before`; removals remove; a wrong `base` returns `false` and changes nothing; focus is restored; the highlight stays last; `data-active-tier` survives and follows a new face size.
- **E:** the layer with the cache equals the layer without it, at every revision of the game fixtures, stepping forward, back, and jumping; the key changes with each document state a counter can show (facing, turret facing, concealed, attachments, states, side, level, the lead) and with each fingerprint input (sheet, palette, figure art); the bound holds; warnings come back on a hit.
- **Q:** the equivalence oracle; one definition for two counters with the same face; three `<use>` per counter; no definition contains `data-unit-id`, `data-attached-id`, or `data-tier`; faces no longer used leave the page after a patch; the focus custom properties are written in layer mode only.
- **Allocation:** the benchmark in `code-builders/diagnostics/svg-memory` gains a layer workload: a sequence of overlays of one map with two counters moved per step, built with and without the cache, reporting characters sent and bytes allocated per step.

### In the Studio

Scope proposed under the case-by-case rule: the cd-guards-5 replay stepped forward and back at one width, step 13 compared with a screenshot taken before the work at the far, mid, and near tiers; a counter focused by keyboard (the gold edge) and clicked (the inspector); the Play page's setup map with a draft counter; the hand-over map; the board viewer with a unit set; the side-by-side comparison once. No five-width check: no layout changes.

## 12. Expected gains

Estimates from the analysis's measurements; each is measured in its task, and the replay of task 11 is repeated at the end.

| After | Layer at step 13 (whole send) | A typical step sends | Server work per step |
|---|---|---|---|
| PR 19 (today) | 696,691 characters | the whole layer on any change: 696,691 | the whole layer built, copied by the leads, compared, serialized: about 3 MB of the step's 9.4 MB allocation |
| C1 | about 376,000 (-46%) | the whole layer: about 376,000 | about 1.5 to 2 MB less: the built string, the leads' copy, and the interop copy each halve |
| Leads in the builder | the same | the same | one copy of the layer and the regular expression's matches fewer |
| D with C2 | about 367,000 | nothing on 4 of the 11 sampled steps; about 16,000 to 60,000 on the others (the changed counters' hexes, outlines gone); about 215,000 at setup | every hex still built and hashed; the interop copy falls to the patch |
| E | the same | the same | only changed hexes are rendered: about 3 MB down to a few hundred KB, plus about 100 KB of keys |
| Q | about 175,000 (-75% from today): 18 face triples at about 8,600 characters, 42 counters at about 450 | a move: about 500 to 6,000 characters; a new state: plus about 8,600 per face the page did not hold | a miss renders only faces not in the face cache |

In the browser, a typical step parses and imports a few hexes instead of 0.7 million characters and 4,425 elements, and the page holds about 175,000 characters of counters instead of 0.56 to 0.96 million.

Overall, a replay step's allocation should fall from about 9.4 MB to about 4 to 6 MB; the rest of a step (projection, rules, render tree, serialization of the page) is outside this design and becomes the larger part. As task 11 showed, the collector's committed memory need not fall with allocation; the measures are bytes allocated per step, collections, characters sent, and the live heap from a dump.

## 13. F, after D and Q

Per ruling 3, all three tiers stay until D and Q are built. With them, a tier costs its face definitions once per face the page has not seen, not a third of every step: about 115,000 of the 175,000 characters at a whole send, and about 5,700 per new face triple. F1 (send only the tier in view) would save those, at the price of a round trip when zoom crosses a tier. The plan's last task measures the replay again and puts F to the user with those numbers; my expectation is that F3 (keep all three) stays, but it is the user's decision on the measurement.

## 14. Risks

| Risk | Handling |
|---|---|
| A patch applied to a layer that is not the one the server thinks the browser holds | The version check (`base`); a rejected patch brings a whole send (L5) |
| The cache key misses an input | The key holds the whole document and a fingerprint of everything else; the cache-on equals cache-off test over every fixture revision |
| A style rule hides or styles face definitions through the referenced element | Definitions carry `data-face-def` only; a test asserts the attributes they must not carry |
| Focus lost when the focused counter's hex is patched | Restored by unit id; a Node test |
| A host that sets units without the sprite (after C2) | Every host goes through `ShowUnits` on `BoardViewport`, which sends the sprite first |
| `<use>` hit testing | Pointer events on a `<use>` copy are retargeted to the `<use>`, so `closest("[data-unit-id]")` finds the counter as today; a Node test of the click path |
| Exported or previewed counters change | They are standalone and do not change (L1); the 138 goldens guard it |

## 15. For the user to rule on

Each with my proposal.

| | Question | Proposal |
|---|---|---|
| U1 | Where C2's sprite lives: ruling 2 says "the viewport's defs layer"; `layer-defs` is built by the board renderer, which does not know counters, and is replaced on every load | A viewport-held group `unit-figures` beside the board's layers, holding the whole built-in figure set (about 36 KB), sent once per board load (section 4) |
| U2 | The unit of a patch: the analysis said hex and level | The hex, all levels (L2) |
| U3 | Q's definitions as `<symbol>` (the analysis's word) or `<g>` | `<g>` in `<defs>`: no clipping, no size (section 8) |
| U4 | The focus ring under Q | CSS custom properties, the same gold edge (section 8) |
| U5 | The cache's bound | 4 million characters, about 8 MB, shared by all renderers |
| U6 | Layer goldens | Add the four of section 11 |
| U7 | When to start | After PR 19 merges into `main`: this work builds on its span writer (`SvgValue`, the pooled `SvgWriter`); one branch from `main` then, one pull request at the end |
