# ASL Map Studio SVG Memory Analysis

2026-10-18, branch `svg-memory-analysis` (cut from `unit-figure-art` at 84c39315, because the measured Studio draws that branch's figure art). Analysis only: no code is changed.

## 1. The question

The Studio's front end is SVG, and all of that SVG is strings built in the server's memory. Are those strings handled efficiently, and would turning them into constants, where practical, help?

Short answer:

- Memory does not leak. A full replay of a finished game raised the Studio's private memory from 306 MB to a 423 MB peak, and it settled at 257 MB, below where it started.
- The cost is churn, not retention. Every replay step rebuilds the whole counter layer (about 700,000 characters for 42 counters), compares it, and sends all of it to the browser when anything changed, though most steps change 0 to 6 counters.
- Nearly half of that layer is the same few figure outlines written again and again: 243 outline paths, of which 6 are distinct.
- Constants, in the sense of C# `const` or literals, would not help: the compiler already keeps one copy of every literal, and the figure outlines are already held once in memory. The duplication is in the output, so the remedy is to write repeated content once and refer to it. That is the "constant" idea applied where it pays.
- Spans instead of strings (option G, measured in section 5.1) pay well inside the writer: today it appends a new one-character string for nearly every character of output. Escaping by span alone removes 60% to 78% of the writer's allocation, with output byte-identical; a full span writer removes 91% to 95%. In counter renders the style cascade allocates as much again (option S).
- Templates with interpolated runtime values (section 5, P to R) save memory only when the template is sent once and each counter refers to it: one SVG symbol per distinct face in the page (Q). Interpolation on the server alone (P) produces the same output, at most with fewer allocations.

## 2. What was measured

All figures come from my Studio (port 6670, process 84948, .NET 10, Server GC, 16 logical processors), built from this branch's head.

### 2.1 A board on its own (bd01)

| Item | Size |
|---|---|
| Exact view document | 247,333 characters |
| Styled view document | 334,340 characters (668,680 bytes as a .NET string) |
| Hex facts view document | 107,728 characters |
| Styled document, gzip | 77,089 bytes (4.3 to 1) |
| Exact terrain layer over HTTP | 196,322 bytes, sent uncompressed |

Inside the styled document:

| Part | Share |
|---|---|
| Numbers (44,101 of them, 7,316 distinct) | 262,823 characters, 79% |
| Attribute names and their syntax (1,993 attributes) | 14,047 characters, 4% |
| Distinct attribute values | 456 of 2,034 |

Board layers are cached on the server (`RenderCache`) and in the browser (immutable URLs with an ETag), so a board is built once per version. Their bulk is unique geometry.

### 2.2 A finished game replayed (cd-guards-5)

The Guards Counterattack, 259 steps in 81 phases, 1.2 MB of events, a recorded winner. I stepped it from the first step to the last, one step at a time, while sampling the process every second and taking a heap dump before and after.

| Measure | Value |
|---|---|
| Private memory before | 306 MB (GC heap 55.6 MB live) |
| Private memory, peak while stepping | 423 MB |
| Private memory after | 257 MB |
| GC heap after | 92.5 MB live, of which 8.4 MB is three `char[]` buffers over 1 MB that were not there before |
| Time per step | 764 ms on average, 3,762 ms at most |
| Browser JS heap | 4 to 28 MB, falling back throughout |
| SVG in the page | 0.56 to 0.96 million characters |

The counter layer (`#layer-units`) at step 13, with 42 counters:

| Part | Size |
|---|---|
| Whole layer | 696,691 characters; 33,031 bytes gzipped (21 to 1) |
| Figure outline `<defs>` | 329,481 characters, 47% of the layer |
| Outline paths in those `<defs>` | 243, of which 6 distinct (8,856 characters) |
| Far, mid, near tiers | 214,395, 228,462, and 239,794 characters: each counter is written three times |
| Decimal numbers | 8,448, of which 1,112 distinct |
| Elements | 4,425 |

At the last step (33 counters) the layer was 463,345 characters, with 219,181 characters of outline paths of which 11,029 distinct.

Counters changed from one step to the next (steps 2 to 13):

| Steps | Counters changed of 42 | Characters in the changed counters |
|---|---|---|
| 4, 8, 10, 13 | 0 | 0 |
| 7, 12, 11 | 2 to 4 | 31,188 to 68,128 |
| 5, 6, 9 | 5 or 6 | 77,682 to 107,646 |
| 3 (setup) | 24 | 398,074 |

### 2.3 What the numbers mean

- The resident size is mostly the runtime and Server GC's reserve, not strings: 55 to 92 MB of live heap inside 257 to 423 MB of private memory.
- The peak comes from allocation during steps. Each step builds a string of about 1.4 MB (700,000 UTF-16 characters), the `StringBuilder` chunks behind it, and the JSON-escaped copy that Blazor's JS interop sends. All of these are large-object-heap allocations, which are collected only with generation 2.
- The 8.4 MB of `char[]` kept after the replay is most likely pooled buffers held by the serializer or the transport after the largest messages. I did not confirm what holds them (a heap dump with retention paths would; see section 7).

## 3. Where the strings come from

| Producer | Output | Built | Kept |
|---|---|---|---|
| `BoardRenderer.Document` and `Fragment` | Board layers | Once per board version, view, and layer | `RenderCache`, without a limit, as `string` |
| `UnitOverlayBuilder.Build` through `UnitRenderer.WriteUnit` | The counter layer | On every projection (every replay step, every play action) | Only the latest, in `BoardWorkspace.sent` |
| `UnitPreview.Document` | One counter for tables and the Unit Lab | Per counter, per revision in Replay and Games | Per page, as `MarkupString` |
| `Glyphs.ArtFigures` | Figure outlines inside a counter | Per counter, per tier | Inside the above |
| `SvgWriter` and `PathData` | The text itself | `StringBuilder`, one escaped `string` per character of every attribute value and text node; numbers through `FixedPoint.ToString`, which makes three or four strings per number | Not kept |

The unit layer reaches the browser through `BoardWorkspace` (`viewport.SetUnits(Units?.Svg)`) and `boardViewport.js` `setUnits`, which removes the old layer, parses the new string with `DOMParser`, and imports it whole.

Two server caches have no limit:

- `RenderCache` keeps every board version, view, and layer it has drawn. An authoring session makes a new version with every edit, so the old ones accumulate for the life of the process.
- `MapService.cache` keeps every map built from placements. `placed` beside it is cleared at 32 entries (pass 28), but `cache` is not.

Neither grew during this replay; both grow in long authoring or card editing sessions.

## 4. On constants

| Candidate | Already | Would `const` help |
|---|---|---|
| Tag and attribute names (`"path"`, `"stroke-width"`) | String literals, interned by the compiler: one instance per process | No |
| Fixed values (`"none"`, `"#ffffff"`, `"sans-serif"`) | Literals, interned | No |
| Figure outlines (`figures.json`) | Read once into `FigurePose.Path`; one instance per outline | No; moving them into C# would also lose the build pipeline from the SVG sources (`code-builders/unit-figures`) |
| Style sheet tokens and palette colors | Parsed once per sheet | No |
| Number text (`"263.515625"`) | Made fresh for every number | A cache of number text would remove repeats (1,112 distinct of 8,448 in the counter layer), but writing numbers straight into the output removes all of them (option G) |
| The output documents | Concatenations, unique by nature | Not as constants; the repeated parts inside them can be written once and referenced (options C and D) |

So the sources are already constant. What repeats is the output, and the way to treat repeated output as a constant is to emit it once (a shared definition, a cached fragment, a static file) and point to it.

## 5. The options

Gains are measured or derived from the figures above; costs are my estimates.

### A. Make the literals constants

Change nothing in behavior; declare names and fixed values as `const`.

- Gain: none in memory or speed; literals are interned already.
- Cost: churn across the renderers.
- Verdict: not worth doing for memory. Only worth it where a name is repeated for consistency, which is a style question.

### B. Move the figure outlines into C# source

Generate a C# file of `const string` outlines instead of embedding `figures.json`.

- Gain: none; the outlines are held once either way.
- Cost: the figure build writes C#; the Art tab's stand-ins, which load at run time, would need a second path.
- Verdict: no.

### C. Write each figure outline once per document

Today each counter writes its own `<defs>` for every tier (`Glyphs.ArtFigures`, id `fig-{hash}-{unit}-{x}-{y}`). Instead, collect the outlines a document uses and write them once, keyed by content hash only (`fig-{hash}-body`), with every counter's `<use>` pointing there.

Three ways to place the shared definitions:

- C1. One `<defs>` at the top of the counter layer, written by `UnitOverlayBuilder` after the counters are laid out. Touches the layer's own output only.
- C2. A figure sprite in the board viewport's `defs` layer, which the viewport already loads once per board. The counter layer then carries no outlines at all, and steps send none.
- C3. A static `figures.svg` served with an immutable URL, referenced as `figures.svg#fig-…`. The browser caches it across pages; the table previews (`UnitPreview`) could use it too. External `<use>` references need same origin, which the Studio has.

- Gain: the counter layer falls from 696,691 to about 376,000 characters at step 13 (minus 329,481 plus 8,856), 46%, on every step that sends it. C2 and C3 remove the remaining 8,856 too.
- Cost: small for C1 (Glyphs and UnitOverlayBuilder); moderate for C2 (viewport load order: the sprite must arrive before counters draw) and C3 (a new endpoint, and previews written as `MarkupString` must not inline their own copy).
- Risk: the golden SVGs for overlays change (C1 to C3), and for previews too (C3). A standalone counter document (export, Unit Lab) must stay self-contained, so it keeps its own `<defs>`.
- Note: the outline id must not depend on unit or position any more. The reason it does today (two counters, one page, different outlines) disappears when the id is the content hash alone.

### D. Send only what changed in a step

Today the whole layer is rebuilt, compared as one string, and resent. Instead, key the layer by hex stack (the counters of one hex and level, which must stay together for stacking order and offsets) and send only the stacks that changed, plus removals.

- Gain: at the observed rates, steps 2 to 13 would send 0 to 15% of the layer instead of 100%; steps with no change send nothing (today they still rebuild 700,000 characters on the server to find that out). The browser parses and imports only the changed stacks.
- Cost: moderate to large. `UnitOverlay` carries fragments by key; `BoardWorkspace` diffs keys; `boardViewport.js` gains a patch call that replaces or removes `<g>` stacks in paint order; selection, highlight, and tier state must survive a patch.
- Risk: paint order between stacks (a stack's position in the layer must follow hex order) and anything in `reportMeasuredScene` that assumes a fresh layer.
- Combines with E: unchanged stacks need not even be rebuilt.

### E. Cache counter fragments on the server

Memoize the SVG of a counter (or a stack) by a key of everything it is drawn from: document state, face, tiers, position, style sheet version, palette, figure art version.

- Gain: rebuild work falls with the share of unchanged counters (85 to 100% of counters on most observed steps); fewer large allocations per step.
- Cost: moderate. The key must be complete, or a stale counter is drawn. A bounded cache (by count or size) is needed.
- Risk: a wrong key shows a counter in an old state; tests would need a case per input that can change.

### F. Send fewer detail tiers

Each counter is written at far, mid, and near, and the viewport shows one of them by zoom (`updateUnitTier`).

- F1. Send only the tier in view; fetch another when the zoom crosses a threshold. Gain: about two thirds of the layer. Cost: a round trip when zooming across a tier boundary, so a short delay or a flash at that moment.
- F2. Send the tier in view first and the other two after, in the background. Gain: faster first paint; no saving in total.
- F3. Keep all three (today).
- This is a product decision: how much zoom latency is acceptable.

### G. Make the writer allocate less

No change to the output; `SvgWriter` and `PathData` only.

- Escape by scanning for `&`, `<`, `>`, `"` (`SearchValues` with `IndexOfAny`) and appending whole spans between them, instead of one `string` per character.
- Skip escaping for path data that `FigureArt.CheckedPath` has already proven holds only commands, digits, and separators.
- Write numbers straight into the builder (a `TryFormat` on `FixedPoint` into a `Span<char>`) instead of building three or four strings per number.
- Avoid the `params` tuple array per element where the attribute count is fixed (minor).

- Gain: measured in section 5.1: the writer's own allocation falls by 78% to 95%, and a counter layer's writing time by about three quarters.
- Cost: from two methods (G1) to every renderer call site (G2, G3); see section 5.1.
- Risk: low. The golden SVGs must stay byte-identical, which makes them a complete check.

### H. Cache number text

A table from fixed-point value to its text, for the values that repeat.

- Gain: repeated numbers reuse one string (about 87% of decimals in the counter layer repeat).
- Cost: small, but the cache needs a limit, and the lookup costs about what G saves.
- Verdict: inferior to G, which needs no strings for numbers at all. This is the closest the "constants" idea comes to helping, and G does it better.

### I. Build and send UTF-8 bytes instead of strings

Write SVG into a UTF-8 buffer (`IBufferWriter<byte>`) rather than a `StringBuilder`.

- Gain: half the memory of each built document (one byte per character for this ASCII content, against two). For JS interop, a `byte[]` argument travels as binary rather than as a JSON string, which avoids escaping every `"` (each attribute has two) and the extra copy.
- Cost: moderate. `SvgWriter` changes inside; callers that take `string` (`MarkupString`, tests, goldens) need a conversion at the edge; the JS side decodes with `TextDecoder`.
- Risk: low to moderate; output identity is checkable as in G.

### J. Bound the render cache and keep bytes in it

- Keep only the latest version of each board (or a small LRU), so authoring sessions stop accumulating old versions.
- Store UTF-8 bytes and a precompressed copy (gzip or Brotli) instead of a `string`: half the memory, no re-encoding per request, and the compressed copy served when the browser accepts it.
- Gain: long-session growth stops; cached boards take about half (bytes) or a quarter (compressed only) of today's memory.
- Cost: small.
- Risk: low; ETags stay content hashes.

### K. Bound the map cache

Give `MapService.cache` the same limit `placed` has, or evict entries made from placements when `placed` is cleared.

- Gain: card editing sessions stop accumulating built maps (each holds a whole map's facts, not only text).
- Cost: very small.

### L. Compress responses

Turn on response compression for `/render` (or serve J's precompressed copies).

- Gain: network only. bd01 styled: 334 KB to 77 KB. Memory unchanged.
- Cost: very small.

### M. Check compression on the Blazor connection

Whether the circuit's WebSocket is compressed decides whether a 700,000-character unit layer crosses the wire at 1.4 MB or about 33 KB. I did not verify the setting in this .NET 10 Studio.

- Gain: network only, possibly large.
- Cost: a check first; configuration if it is off.
- Risk: compression on a connection that carries user-entered text has a known side-channel class (BREACH-like); for a local single-user Studio that is not a concern, for a hosted one it is.

### N. Choose the garbage collector for a desktop tool

The Studio runs Server GC on 16 logical processors, so it keeps several heaps and generous budgets (.NET 10 adapts their number with DATAS). The live heap is 55 to 92 MB; private memory is 257 to 423 MB.

- N1. Workstation GC (`<ServerGarbageCollector>false</ServerGarbageCollector>`) for the Studio: one heap, smaller footprint, slightly longer pauses under load.
- N2. Keep Server GC and set `GCConserveMemory` (0 to 9) to trade some throughput for compaction.
- N3. Keep the defaults.
- Gain: likely the largest drop in resident size of any option, without touching rendering; size to be measured.
- Cost: one setting; a before and after replay like section 2.2.
- Risk: low for a single-user tool.

### O. Leave it

There is no leak, and memory returns after use.

- Gain: no work.
- Cost: step time stays at about 0.76 s; long authoring and card editing sessions keep growing through J and K; larger scenarios (more counters) scale the per-step cost linearly.

### S. Make the style cascade allocate less

Found while measuring G (section 5.1): rendering counters spends as much allocation in the style cascade as in writing SVG. `StyleCascade.Compute` walks every rule of the sheet for every face, every slot, and every tier. Three things allocate on each walk:

- `foreach (var selector in rule.Selectors)` over an `IReadOnlyList<Selector>` boxes the list's enumerator: 18.6 MB per render of 80 counters, the largest single item.
- `selector.Traits.Any(trait => …)` and `selector.States.Any(state => …)` make a closure and a delegate on every selector test: about 5 MB together.
- Every `Compute` builds a new `ComputedStyle` dictionary: about 1 MB.

Two levels of fix:

- S1. Index loops (`for (var i = 0; i < list.Count; i++)`) instead of `foreach` over interfaces, and plain loops instead of `Any` with a lambda, in `Compute`, `Rank`, and `Matches`. No change in results. Small.
- S2. Memoize computed styles by what they depend on: the sheet, the subject's kind, face, states, traits, and the attributes the sheet's selectors read, the owner, the slot, and the tier. Counters of one kind in one state share a style. Moderate; the key must be complete.

- Gain: S1 removes about 25 MB of the 56 MB that a render of 80 counters allocates (measured by type, not yet prototyped). S2 removes most of the cascade's time as well.
- Risk: S1 low (the goldens check it); S2 as for E (a stale style if the key misses an input).

### P, Q, R. Templates with runtime values

The user's suggestion (2026-10-18): treat the SVG as templates and inject the runtime values (counter values, colors, positions) by C# string interpolation. Where the template lives decides what it saves, so it is three options.

What a template has to cover: a counter is not one fixed shape. The style sheet cascade decides its shape (square or circle), its slot grid (`face-template`), which slots show, their marks, badges, patterns, arrows, and the figures; a squad, a leader, a broken squad, and a vehicle produce different element trees. A template can therefore be one primitive (a slot's text, a badge, a figure group, a covered arc), or a whole counter as already resolved by the cascade, but not one template for every counter.

Measured at step 13: 42 counters show 18 distinct faces (by accessible name with the location removed; the true visual count is lower, since level and some states do not change the face). The near tier averages 5,709 characters per counter.

#### P. Server templates through an interpolated string handler

Replace element-by-element `SvgWriter` calls with interpolated templates per primitive, for example `svg.Append($"<rect x=\"{x0}\" y=\"{y0}\" width=\"{size}\" …/>")`, written through a custom interpolated string handler.

- The handler is the essential part. With it, the literal parts are copied straight into the output and each hole is formatted into it (`ISpanFormattable`), so no intermediate strings exist. Without it, `$"…"` builds a whole string first and then copies it: worse than today.
- The handler must escape every hole that can hold text (`&`, `<`, `"`), while numbers go through the 1/64 pixel formatting; plain C# interpolation does neither.
- Gain: the same as G (fewer allocations, less CPU), and the renderers read closer to the SVG they produce.
- Not a gain: the output is the same size. Each counter still carries its own copy of the template text, because the template is copied into every instance.
- Cost: moderate; every renderer method is rewritten.
- Risk: attribute order and number text must stay byte-identical for the goldens; a forgotten escape in a hole is an injection into the page. The handler can make escaping the default, which is safer than today's per-call discipline.

#### Q. Templates in the page: one symbol per distinct face

The server writes each distinct counter face once, per tier, as an SVG `<symbol>` keyed by a hash of its content, drawn at the origin. Each counter on the map becomes a reference that carries only its runtime values: `<use href="#face-{hash}-near" x="…" y="…"/>` inside the `<g data-unit-id … aria-label …>` that already exists.

- This is the template idea where it saves memory: the template (the symbol) is sent once, and the instance carries only what varies.
- Gain: at step 13, 18 faces instead of 42 counters (2.3 to 1 before C; the figure outlines inside them are shared as in C). Each instance is a few hundred characters, mostly its accessible name. With D, a step that only moves a counter sends one `<use>` with new `x` and `y`; a step that changes a counter's state sends a new symbol only when that face was not already in the page.
- Requires the counter to be drawn relative to its own center, so that position is the only per-instance geometry. Today positions are baked into every coordinate (`x="263.515625"`).
- Cost: moderate to large. `UnitRenderer` draws at the origin; `UnitOverlayBuilder` collects symbols; the viewport keeps symbols across steps and drops unused ones; selection and the tier switch work on `<use>` elements.
- Risk: per-instance styling (a highlight, a selection ring) must stay outside the symbol; `<use>` content cannot be styled per instance except through inherited properties. Hit testing and the accessible name stay on the outer `<g>`, as now.

#### R. Templates in the browser: counters sent as data

The server sends each counter as data (face values, colors, state, position) and JavaScript fills a template per counter kind.

- Gain: the smallest messages of any option (perhaps 200 to 400 characters per counter).
- Cost: large. The layout rules of the style sheet cascade would be implemented a second time, in JavaScript, or the cascade would move to the browser. The C# renderer and its golden SVGs stop being what the page shows.
- Risk: two renderers that drift apart; the counter export and the Unit Lab still need the C# renderer.
- Verdict: not recommended. Q gets most of the gain while the C# renderer stays the single source of a counter's look.

## 5.1 Option G in depth: spans instead of strings

### Background

`Span<T>` and `ReadOnlySpan<T>` (C# 7.2 and .NET Core 2.1, 2018) are views over memory that already exists: part of a string, an array, or a stack buffer. Code can read, slice, and copy through a span without making a new string. Later releases built the parts this option uses on top of them: `ISpanFormattable` and `TryFormat` (.NET 6) write a number's text straight into a span; interpolated string handlers (C# 10) let `$"…"` write into a buffer without a string in between; `SearchValues<T>` (.NET 8) finds any of a set of characters in a span with vectorized search. The Studio runs .NET 10, so all of them are available.

### Where today's writer allocates

Tracing one attribute value through `SvgWriter` today:

1. Each number goes through `FixedPoint.ToString`: `whole.ToString`, the fraction's `ToString("D6")`, `TrimEnd`, and a concatenation, so up to four strings per number.
2. Path data collects those strings in a `PathData` `StringBuilder`, then `PathData.ToString` makes the whole path a string.
3. The renderer puts the value in a `(string Name, string Value)` tuple, and the `params` array holding the tuples is allocated per element.
4. `WriteTag` copies the value character by character, and for every character that needs no escaping it appends `character.ToString()`: a new one-character string (24 bytes) for nearly every character of the output. .NET does not cache these.
5. The output `StringBuilder` grows in chunks, and `ToString` copies everything once more.

Step 4 is the largest by far, and it is the easiest to remove.

### How it was measured

A scratch console program (not in the repository) referencing the product's projects, run in Release on .NET 10 with workstation GC:

- Real renders: bd01 from the VASL checkout (exact and hex facts documents) and the counter layer from the repository's examples (`bd01-infantry-figures`, 80 counters with figure art; `bd01-demo`, 9 counters). Time and allocated bytes per render after warm-up, and allocation by type from the runtime's allocation events.
- A replay: each render's output parsed back into the writer calls that made it, with numbers as fixed-point values and path data as commands and numbers, then written by four writers. Every writer's output was compared with the real render: all four are byte-identical on all four workloads.
  - Today's writer: a copy of `SvgWriter` and `PathData` as they are.
  - G1, today's API with span escaping: the same strings in, only the escape loop changed to append the spans between characters that need escaping (`SearchValues` and `IndexOfAny`).
  - G2, a span writer: numbers formatted straight into the output (a `TryFormat` for `FixedPoint`), path data written straight into the attribute, figure outlines (already proven to be path data by `FigureArt.CheckedPath`) copied unescaped, and escaping by span.
  - G3, the same with a pooled `char` buffer (`ArrayPool<char>`) in place of the `StringBuilder`, so the final string is the only allocation of its size.

### Results

Allocation and time of the writer layer alone, per render:

| Workload | Today | G1 span escaping | G2 span writer | G3 pooled buffer |
|---|---|---|---|---|
| bd01 exact (247,310 characters, 54,127 numbers) | 9.43 MB, 11.4 ms | 3.75 MB, 5.6 ms | 1.00 MB, 6.9 ms | 0.49 MB, 6.6 ms |
| bd01 hex facts (107,705 characters) | 3.86 MB, 6.7 ms | 2.05 MB, 3.6 ms | 0.44 MB, 3.2 ms | 0.22 MB, 3.7 ms |
| 80 counters with figures (1,089,845 characters) | 24.83 MB, 20.1 ms | 5.35 MB, 5.7 ms | 4.37 MB, 4.9 ms | 2.18 MB, 4.2 ms |
| 9 demo counters (62,571 characters) | 1.36 MB, 1.0 ms | 0.42 MB, 0.4 ms | 0.26 MB, 0.2 ms | 0.13 MB, 0.2 ms |

G3's remaining allocation is the output string itself (2 bytes per character): the floor for any writer that returns a `string` (option I goes below it with UTF-8).

The writer's share of a whole render:

| Workload | Whole render today | Writer layer today | Rest |
|---|---|---|---|
| bd01 exact | 9.51 MB, 22.2 ms | 9.43 MB (99%) | geometry |
| 80 counters with figures | 56.24 MB, 108.6 ms | 24.83 MB (44%) | style cascade (option S) and layout |

Allocation by type in the counter render: strings 24.9 MB; the cascade's boxed `Selector` enumerators 18.6 MB; delegates and closures from `Any` about 5 MB; `char[]` 1.7 MB; `StringBuilder` 1.1 MB; the cascade's dictionaries 1.1 MB; attribute tuple arrays 0.9 MB.

Number formatting alone (all 54,127 numbers of bd01 exact): today 1.69 MB and 3.5 ms; `TryFormat` into a span 0 bytes and 3.7 ms. The prototype agrees with `FixedPoint.ToString` on every value and on the edge values (zero, plus and minus one, 1/64 steps, the extremes of `int`). The time is equal within noise, so the gain from numbers is memory only.

### What this means

- The per-character `ToString` is the dominant cost. G1 alone removes 60% (board) to 78% (counters) of the writer's allocation, and halves to quarters its time, with no change to any caller: it changes `SvgWriter.Text` and `SvgWriter.WriteTag` only.
- G2 removes the number and path strings. It needs an API change: `PathData` and the attribute values must accept numbers (`FixedPoint`, or `double` rounded as `SvgWriter.Number` does now) instead of strings, so every `N(…)` and `SvgWriter.Number(…)` at a call site (about 100 across the renderers) changes. On the board, where 99% of render allocation is the writer and most of it is path numbers, G2 is worth it; on counters, G1 has already taken most of the gain.
- G3 halves what remains by keeping one pooled buffer instead of `StringBuilder` chunks. Small and internal, once G2's shape is in place.
- After G1 to G3, the counter render is dominated by the cascade (option S), not by strings.
- Expected whole-render effect (writer measured, cascade estimated from the type profile): bd01 exact from 9.5 MB and 22 ms to about 0.6 MB and 17 ms; 80 counters from 56 MB and 109 ms to about 34 MB and 93 ms after G, and to about 9 MB after G and S1.

### The staged form of G

| Stage | Change | Touches | Gain |
|---|---|---|---|
| G1 | Escape by span (`SearchValues`, `IndexOfAny`, `Append(ReadOnlySpan<char>)`) | `SvgWriter.Text`, `SvgWriter.WriteTag` | 60% to 78% of writer allocation; writer time halved or better |
| G2 | Numbers and path data written as numbers; trusted figure outlines unescaped | `SvgWriter`, `PathData`, `FixedPoint` (a `TryFormat`), every renderer call site | the rest of the per-number strings; board writer allocation to about a tenth |
| G3 | Pooled `char` buffer in place of `StringBuilder` | `SvgWriter` | about half of what is left |

Each stage is checked by the golden SVGs, which must not change by a byte.

## 6. Comparison

| Option | Memory per step | Resident size | Network per step | Effort | Output changes |
|---|---|---|---|---|---|
| A, B constants | none | none | none | small | no |
| C1 outlines once per layer | -46% | indirect | -46% | small | overlay goldens |
| C2, C3 shared sprite | -47% | indirect | -47% | moderate | overlay (and preview) goldens |
| D changed stacks only | -85% to -100% sent | indirect | -85% to -100% | moderate to large | no (same SVG, sent in parts) |
| E fragment cache | fewer rebuilds | + bounded cache | none | moderate | no |
| F1 one tier | -66% | indirect | -66% | moderate | yes, by tier |
| G1 span escaping | writer -60% to -78% (measured) | indirect | none | very small | no |
| G2, G3 span writer, pooled buffer | writer -91% to -95% (measured) | indirect | none | moderate (call sites) | no |
| S1 cascade loops | counters about -25 MB of 56 MB (by type profile) | indirect | none | small | no |
| S2 cascade memo | most cascade work | + bounded cache | none | moderate | no |
| H number text cache | small | small | none | small | no |
| I UTF-8 bytes | -50% per document | indirect | escaping removed | moderate | no |
| J bounded render cache | none | stops growth, about -50% | with L | small | no |
| K bounded map cache | none | stops growth | none | very small | no |
| L HTTP compression | none | none | board -77% | very small | no |
| M connection compression | none | none | possibly -95% | check first | no |
| N GC choice | none | probably large | none | very small | no |
| O leave it | none | none | none | none | no |
| P server templates (handler) | as G | indirect | none | moderate | no, if byte-identical |
| Q symbols per distinct face | about -55% before C and D; far more with D | indirect | same | moderate to large | yes, counters drawn at the origin |
| R counters as data, JS templates | smallest | indirect | smallest | large | the C# renderer no longer draws the page |

## 7. The order

Settled by the rulings of section 8 (2026-10-18).

On a branch from `main`, since none of these files differ on `unit-figure-art` (ruling 5):

1. N2: Server GC with memory conservation, its level chosen by measuring the cd-guards-5 replay at two or three settings against section 2.2. M: read the connection's compression setting.
2. The output-identical group: G1 and S1 first (the two cheapest changes, measured or profiled at the largest gains), then J, K, L, then G2 and G3. All are checked byte for byte by the existing goldens, and J and K end the only unbounded growth found.

After `unit-figure-art` merges, because these change `Glyphs` and `UnitRenderer`, which that branch changes:

3. The counter layer: C1, then D with E. C1 is small and halves every step; D and E make steps cost what changed.
4. Then Q, the template idea in the page: with D in place, a step sends `<use>` references and only new faces. C2 follows D (ruling 2).
5. F: all three tiers stay (F3) until D and Q are built, then the replay is measured again and F decided on that (ruling 3).
6. Not P (G gets its allocation gain with far less change; ruling 4), A, B, H, or R.

Section 5.1 measured the writer and profiled the cascade. Before step 3, one more measurement would sharpen the rest: an allocation trace of a few replay steps (`dotnet-trace` with the GC allocation events, as in the Studio profiling practice), and a heap dump with retention paths for the 8.4 MB of `char[]`.

## 8. Rulings

The user's rulings of 2026-10-18. Ruling 1 is the user's choice; rulings 2 to 5 are the user's acceptance of my recommendation.

| Ruling | Question | Decided |
|---|---|---|
| 1 | Workstation GC (N1), or Server GC with conservation (N2) | N2: the Studio keeps Server GC, with `System.GC.ConserveMemory` set; the level is measured, not guessed |
| 2 | Where the shared figure outlines live: the counter layer (C1), the viewport's defs (C2), or a static sprite file (C3) | C1 first; C2 when D is built; not C3 |
| 3 | Whether zoom may wait for a tier (F1), or all three tiers stay | All three stay (F3) until D and Q are built; then measured again and decided |
| 4 | Templates on the server (P), in the page as symbols (Q), or in the browser (R) | Q, with G in place of P; not R |
| 5 | Whether the work waits for `unit-figure-art` to merge | Split: N2, M, G, S, J, K, L go on a branch from `main` now, since `unit-figure-art` does not change their files (`SvgWriter`, `PathData`, `StyleCascade`, `RenderEndpoints`, `Maps.cs`, the Studio project file); C, D, E, Q, F wait for that merge, since they change `Glyphs` and `UnitRenderer`, which it changes |

`FixedPoint` (for G2's `TryFormat`) is in `LimboDancer.Domains.Asl.Maps`, also unchanged by `unit-figure-art`.

## 9. Not measured

- Allocation per step, by type (needs a trace).
- What holds the 8.4 MB of `char[]` after the replay (needs a dump with retention paths).
- The Play page under the same load; only Replay was stepped. Play builds the same overlay through `GameMaps.Layers`, so the counter layer findings apply.
- The board viewer, Unit Lab, and card editor beyond the bd01 board figures.
- A scenario larger than the Guards Counterattack.
