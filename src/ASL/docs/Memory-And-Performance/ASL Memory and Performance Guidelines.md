# ASL Memory and Performance Guidelines

**Status:** Standing guidance for future ASL implementation and review, requested by the user on 2026-10-20. Applies to rules integration, planner reads, Map Studio pages, rendering, caches, storage access, and measurement tools.

Future features must preserve the reuse, bounded lifetimes, and incremental rendering established by the October 18 and 19 performance work. Compute an answer at the lifetime of its inputs, reuse it through the existing access paths, and rebuild or transmit only what changed. Correct rules, disclosure, replay behavior, and drawing remain the acceptance conditions.

Read this guidance when designing a pass and review the relevant sections before completing it. The [Pass Standing Rules and Harness Lessons](<../Prompts/Pass Standing Rules and Harness Lessons.md>) govern the work and merge procedure. These guidelines do not authorize deferred features or change existing product decisions.

## Definitions

The following meanings apply throughout these guidelines. Code names appear in backticks so they can be found in the implementation.

### Game state and map access

| Term | Meaning |
|---|---|
| ASL | Advanced Squad Leader, the game whose rules and maps this domain package implements. |
| Counter | A game piece drawn on the map, representing a unit, weapon, or other game information. |
| Hex and level | A hex is one cell of the map's hexagonal grid. A level distinguishes locations vertically within that hex, such as ground and upper floors. |
| State or snapshot | The game's recorded condition at a particular revision. `GameState` is treated as immutable: later changes produce another state rather than altering that snapshot. |
| Revision and step | A revision identifies a position in the game's event history. A step is one operation advanced by Play or Replay and can produce or consume several events, so one step can span several revisions. |
| Play and Replay | Play runs a live game and commits actions. Replay displays a recorded game's history at selected steps. Their performance workloads differ because Play also calculates available actions and obligations. |
| Rout Phase | The phase in which affected broken units must move or otherwise resolve their rout obligations. Its route searches make it a particularly demanding measured workload. |
| Disclosure and viewer | Disclosure determines what information a player is allowed to know. The viewer is the selected side or adjudicator perspective; results suitable for one viewer may be invalid for another. |
| Projection | A derived representation of game state for a particular consumer, such as a player's unit table or map display. |
| Planner | `GamePlanner`, the component that derives game answers, choices, and map facts used by play. |
| State map context | Board handles, the composed map read, and resolved locations retained with one state's planner memo. |
| Board handle | A `BoardHandle` object providing access to a particular board's map data and read operations. |
| Pinned board version | The specific board version named by the game state. A newer board version must not silently replace it. |
| Placed map and composed read | A placed map positions boards in a common layout, possibly with boards reversed. A composed read answers map questions across that layout, including board boundaries. A placed map can contain only one board. |
| Location resolution | Turning a board-and-location identifier into its readable map facts. `BoardLocation` identifies the board, hex, and level; the resolved result is a `LocationRead`. |
| Seam and seam ownership | A seam is the boundary where placed boards meet. Ownership determines which board's identifier must name a shared hex. |
| Footprint | A campaign's published restriction describing the playable portion of a board. Here this means map coverage, not the amount of memory a process uses. |
| LOS | Line of sight, the map-based determination of visibility between locations. |
| Rout settlement boundary | The point after the state's surrender decisions have been established, when settled rout answers can safely be retained. Answers computed during the surrender decision window must not contaminate those stores. |
| A3 and A3b | Names from the optimization plan. A3 retains board handles and composed reads per state; A3b additionally retains resolved location reads. |
| Map Studio | The ASL application's interface for viewing and editing maps and for Play and Replay. A Studio instance is one running copy of that application. |
| Adjudicator | The full-information viewing perspective used to inspect game behavior, as distinct from either player's restricted view. |
| Event history and game commit | The ordered record of game changes. Committing a game action records its accepted changes in that history; this is distinct from committing source files to Git. |
| Proposal | A candidate game action being considered or checked before it is confirmed and committed. |
| Board catalog | The service through which callers request board data by identity and version. Repeatedly consulting it can cost more than reusing the state's retained board handle. |
| Map-query helpers | The planner's shared access functions. `Handle` obtains the state's board handle; `Composed` obtains its combined map read; `ReadLocation` obtains location facts; `Step` obtains the two endpoint reads and their adjacency and crossing facts. `Step` here names a function, not an advance through the game record. |
| Traversal and route search | Visiting successive map locations or connected neighbors to find a route or answer a map question. Work inside the traversal can be repeated thousands of times during one game step. |
| Encirclement answer | A derived determination of whether a unit meets the applicable encirclement conditions. The performance changes retain this answer without changing those game rules. |
| Concealed, uninspected, and Dummy | Distinctions affecting what a counter represents or reveals. Concealed units hide information; uninspected is a display/document flag for information not inspected; a Dummy represents apparent presence rather than an actual unit. Cache keys must preserve the distinctions used by the renderer. |
| Attachment and facing | An attachment is an item associated with another counter in its representation. Facing specifies orientation, such as the direction of a vehicle or turret. Both can change the drawing. |
| Reference workloads | `bd01` identifies the board used in rendering measurements. Since 2026-10-25 the reference game is `dl-tractor-works-02`, the user's most recent full game, cut at a revision with `perf_cut.py` when a step must be repeated (`p65-perf-tw` is its cut at revision 1316). `cd-guards-5` and `a3-gambit-rout`, the earlier workloads, were retired with the other games on 2026-10-25 and are kept in `asl-retired-2026-10-25` beside the repository. |

### Reuse and concurrency

| Term | Meaning |
|---|---|
| Cache and memoization | A cache retains a result for reuse. Memoization is caching a computed answer by the inputs that determine it. `PlayStepMemo` holds such answers for one state; `MemoOf(state)` retrieves that memo. |
| Cache key and complete key | The values used to identify a cached answer. A complete key distinguishes every input change that can change the answer, including the viewer when relevant. |
| Reference identity | Equality based on being the same in-memory object, rather than having equal fields or the same game ID. |
| Owner, lifetime, and bound | The owner manages the cache. Lifetime determines how long its entries remain valid or reachable. A bound limits retained entries or size. |
| Invalidation and eviction | Invalidation stops reuse because an input or validity condition changed. Eviction removes an entry to enforce storage limits, even if it remains correct. |
| Least recently used eviction | Removing the entries that have gone longest without use when the cache reaches its bound. |
| Missing-result policy | The rule for whether and how long absence or failure is cached. Remembering an absent footprint briefly avoids repeated disk checks without hiding later publication indefinitely. |
| Lazy value | A result computed only when requested. Reading a lazy whole-layer SVG can trigger substantial work even if the caller only wanted to log or compare it. |
| `ConditionalWeakTable` | A .NET association between objects and attached values that does not, by itself, keep its key objects alive. It lets a planner memo follow its state's lifetime. |
| Circuit | A Blazor server session associated with a connected browser interface. Multiple circuits can use the same shared planner. |
| Concurrency and race | Concurrency means operations can overlap. A race occurs when their relative timing affects which operation observes or stores a value first. |
| `GetOrAdd` and factory | `GetOrAdd` retrieves an existing entry or supplies one using a factory, the function that creates the value. Competing callers can run the factory more than once even when only one result is retained. |
| Side effect and lock | A side effect changes something beyond a computation's returned answer, such as writing a file. A lock restricts access to shared work or data so competing operations cannot enter the protected section together. |
| Index and full-state scan | An index provides a direct way to find matching objects. A full-state scan walks the state's collection to find them. An index also costs time to build and memory to retain. |
| Immutable and mutable | Immutable data cannot be changed after construction. Mutable data can change in place. A cached answer based on mutable inputs needs a way to detect those changes before reuse. |
| Reachable and weak association | An object is reachable while the program can still access it through live references. A weak association does not by itself prevent its target from being collected when other references disappear. |
| Stale entry | A retained answer that no longer matches the inputs or validity conditions for the current request. It must not be reused merely because an entry exists. |
| Contention | Competition between concurrent operations for a shared resource, such as a lock, which can cause waiting or repeated work. |
| Exactly-once guarantee | A promise that a particular operation executes only once for the relevant request or key. Retaining one cache entry does not prove that its factory executed only once. |
| Null result and fallback | A null result indicates that no value was obtained. A fallback is the alternative path used when the preferred path cannot supply an answer; it must preserve the established meaning of failure and success. |
| Dictionary | A collection that associates keys with values for lookup. A process-wide dictionary remains available across requests and can retain entries far longer than a single state needs them. |
| Renderer identity | The identity of the particular renderer object used to draw output. In the fragment key it stands for the inputs fixed when that renderer was constructed. |

#### Cache hits, misses, and fills

**Hit:** A cache lookup that finds an existing entry valid for reuse, avoiding the work of computing or reading the result again.

**Miss:** A cache lookup that finds no entry valid for reuse. The caller must obtain the result through another path, such as computation or a file read.

**Fill:** The operation that obtains a result after a cache miss and stores it in the cache for later reuse. Concurrent callers may attempt to fill the same entry, so this operation must remain safe when their work overlaps.

### Drawing and transport

| Term | Meaning |
|---|---|
| SVG and markup | SVG means Scalable Vector Graphics, the text-based drawing format used for boards and counters. Markup is its structured text of elements and attributes. |
| Element and attribute | An element is a structured drawing item, such as a path or group. Attributes specify properties such as position, color, and identifier. |
| Counter layer or overlay | The drawing of game pieces placed over the board. `UnitOverlayBuilder` constructs its representation. |
| Fragment | A reusable portion of the overlay. A hex fragment contains the counters for that hex across its levels; face definitions can also travel as fragments. |
| Sprite, definition, and reference | A sprite collects reusable drawing definitions. Other elements reference those definitions instead of repeating their drawing instructions. The viewport's figure sprite holds shared outlines. |
| Face and face-origin transformation | A face is a counter's drawn appearance at a detail tier. Drawing it at a common origin and translating it into position lets multiple counters share the same definition. |
| Viewport and zoom tier | The viewport is the visible map area and its browser drawing controls. Far, mid, and near tiers select the amount of counter detail shown at different zoom levels. |
| Patch, patch base, and patch folding | A patch describes changes to an existing layer. Its base identifies the layer version it expects. Patch folding applies those changes to the old representation to check that the result equals the next complete representation. |
| Full send and recovery | A full send transmits the complete layer rather than changes. Recovery uses that complete representation when a patch cannot safely apply. |
| Paint order, focus, and selection | Paint order determines which overlapping elements appear on top. Focus identifies the keyboard interaction target; selection identifies the item the user has chosen. They need not be the same item. |
| Placement-set identity and lead | Placement-set identity names the logical collection of counter placements independently of its revision. A lead is the naming prefix supplied when building counter names and titles. |
| `UnitLayerSync` and `BoardViewport.ShowUnits` | The synchronization component that tracks layer changes, and the viewport entry point used to show the overlay through the shared drawing path. |
| Style cascade | The ordered matching and combination of style rules that determine a counter's appearance. |
| Escaping and trusted output | Escaping encodes characters that would otherwise be interpreted as markup. Trusted output bypasses that encoding only after the relevant validation contract has established that the content is safe for that use. |
| UTF-8, gzip, and compression negotiation | UTF-8 encodes text as bytes. Gzip compresses those bytes. Negotiation chooses compressed or uncompressed output according to what the requesting browser accepts. |
| ETag and conditional response | An ETag identifies a version of an HTTP resource. A conditional request lets the server tell the browser to reuse its cached copy when that version is unchanged. |
| `Vary: Accept-Encoding` | An HTTP response instruction telling caches that the selected representation depends on the request's accepted compression formats. |
| Serialization and render tree | Serialization converts objects into a form that can be stored or transmitted. A render tree is the interface framework's structured representation of what the page should display. |
| Server round trip | A request from the browser to the server and the response back. Requiring one during a zoom change can introduce visible delay. |
| Incremental rendering | Updating only the parts whose inputs changed while retaining the other rendered parts. Here it includes reusing hex fragments and applying browser patches. |
| `UnitLibrary` | The shared Studio service through which counter rendering and its fragment cache are accessed. |
| `SvgWriter` and `SvgValue` | `SvgWriter` produces SVG text using the project's writing and buffer rules. `SvgValue` carries an attribute value or coordinate in a form the writer can format without first making an intermediate string. |
| Path and outline | In SVG, a path describes geometry through drawing commands and coordinates. An outline is the shape of a figure expressed using that geometry; it is unrelated to a filesystem path. |
| Standalone output | A drawing that includes the definitions it needs to render independently, such as an exported counter or a preview outside the map viewport. |
| Regular expression | A pattern used to find or replace portions of text. Rewriting an already completed SVG with one can require scanning and copying the whole document. |
| HTTP endpoint | An address through which the server handles a browser request, such as a request for a board's rendered bytes. HTTP is the request-and-response protocol used for that exchange. |
| Blazor | The .NET interface framework used by Map Studio. In this application, browser interaction is connected to server-side interface processing through a circuit. |
| Painted frame and latency | A painted frame is an image the browser has actually drawn. Latency is the delay to a specified outcome; a server result or updated page state may arrive before the corresponding frame is painted. |

### Memory and measurement

| Term | Meaning |
|---|---|
| Allocation and allocation churn | Allocation obtains memory for new objects or buffers. Churn is repeated creation and disposal of temporary objects, even when little remains afterward. |
| Retained memory and live heap | Retained memory is memory still held for use. The live managed heap is the portion occupied by managed objects that remain reachable; it excludes unused capacity reserved for future allocations. |
| GC and Server GC | GC means garbage collection, the .NET runtime's reclamation of managed memory no longer needed. Server GC is the runtime's server-oriented collection mode, which can use multiple heaps. |
| Adaptive heap count | The runtime's adjustment of how many managed heaps it uses as workload changes. The number of heaps can affect committed capacity even when the live data stays similar. |
| Large-object heap | The part of the managed heap used for large allocations, including large SVG strings and arrays. Free space within it can remain committed after objects are reclaimed. |
| GC-committed and private memory | GC-committed memory is capacity committed for the managed collector's use, including unused space. Private memory is process-private committed memory, including managed and other allocations. Neither directly measures live game data. |
| Buffer, pool, and span | A buffer temporarily holds data. A pool lends buffers for reuse instead of allocating a fresh one each time. A span provides access to a contiguous region of data without requiring a new string or array for that region. |
| Fixed-point formatting | Converting the project's fixed-point numeric coordinates to text. Span formatting writes that text into an existing destination instead of creating intermediate strings. |
| Capturing lambda | An inline function that uses variables from its surrounding scope; retaining those values can require additional objects. |
| Boxed interface enumerator | A value-type collection iterator wrapped as an object to use an interface, potentially allocating during each traversal. |
| LINQ and materialization | LINQ is .NET's collection-query API. Materialization turns a query into a concrete collection or result, often allocating storage. Repeating it can repeat both traversal and allocation. |
| Hot loop and cold code | A hot loop runs often enough to contribute materially to cost. Cold code runs rarely or contributes little to the measured workload. |
| Workload, baseline, and regression | A workload is the set of operations being measured. A baseline is its reference implementation and measurement. A regression makes a relevant behavior, cost, or resource measure worse. |
| Median, tail timing, and noise | The median is the middle observation. Tail timings describe the slow end of the distribution, such as the 90th percentile. Noise is variation unrelated to the intended code change. |
| CPU time and elapsed time | CPU time measures processor work. Elapsed time measures time passing, including waiting and scheduling delays. Both differ from the time at which the browser paints the result. |
| Instrumentation and probe overhead | Instrumentation adds observation, such as counters or timers. A probe is a targeted observation point; its overhead is the extra work caused by measuring. |
| Trace, frame, and inclusive time | A trace records execution observations. A frame represents a function on the call stack. Inclusive time includes the function's callees, so summing a parent and its nested frames double-counts work. |
| Allocation ticks and heap evidence | Allocation ticks are runtime trace events used to estimate allocation volume and attribute it to types or execution paths. Heap evidence, such as a dump, describes objects retained at a particular time. |
| Server pacing, follower, and throttling | Server pacing advances steps using the server's timing. The Play follower copies a recorded game's steps into a live game for measurement. Throttling is the browser reducing timer or drawing activity, particularly for hidden pages. |
| Runtime and managed memory | The runtime is the software that executes .NET code and provides services such as garbage collection. Managed memory is memory whose object lifetime is handled by that runtime. |
| API | An application programming interface: the functions and types a component exposes for other code to use. |
| Collection iterator and value type | An iterator tracks progress while walking a collection. A value type stores its value directly in its containing storage; converting it to an object representation can require boxing and a new allocation. |
| Fixed-point number | A number represented using a fixed scale between its stored integer and its numeric value. The project uses such numbers for coordinates whose text must be formatted consistently. |
| Sampled trace, outer frame, and leaf | A sampled trace observes execution periodically rather than recording every call. An outer frame is a caller higher in the observed stack; a leaf is the deepest observed function. Attribution at a leaf can be misleading when observations occur after earlier work. |
| Native call | A call into code outside the managed .NET execution model, such as an operating-system function. |
| Counter, cumulative counter, and allocation tick | In measurement, a counter is a numerical observation rather than a game piece. A cumulative counter reports a running total; differences between readings estimate activity during the interval. Allocation ticks are trace events rather than that periodically read total. |
| `allocread.cs` | The repository's diagnostic reader for allocation trace events, used to summarize allocation volume and its sources. |
| Percentile | A value below which a stated proportion of observations falls. The 90th percentile indicates that about 90% of measured steps were at or below that value. |
| Stall and machine load | A stall is an unusually long delay in progress. Machine load is the competing demand on the computer's processing and other resources, which can affect timings independently of code changes. |
| Paired run and fresh-process run | A paired run compares before and after versions under comparable conditions. A fresh-process run starts a new application instance, reducing differences caused by previous activity and retained caches. |
| Scalability | How time and resource use change as workload size grows, such as with more units, boards, revisions, or simultaneous sessions. |

### Verification and delivery

| Term | Meaning |
|---|---|
| Contract and acceptance threshold | A contract is behavior an implementation must preserve. An acceptance threshold is the measurable condition a proposed improvement must meet. |
| Fixture and golden | A fixture is repeatable test data or setup. A golden is an approved expected output, such as a counter's exact SVG, checked for unintended changes. |
| Oracle and equivalence | An oracle provides the expected answer or comparison. Equivalence means two implementations give the same required result, even if their internal representation differs. |
| Hash and layer manifest | A hash is a compact fingerprint of content. A layer manifest records expected layer hashes for test cases. Changing the expected fingerprint requires separate evidence that the drawing is still correct. |
| Property-coverage test | A test that checks whether the cache-key implementation accounts for the document properties it must distinguish, helping expose omitted inputs when fields are added. |
| Text list, replay digest, and sweep | The gate's comparison outputs: readable recorded results, a compact representation of replay results, and checks over cuts of a recorded game. A cut ends the history at a chosen point so behavior can be checked there. |
| Gate, strict build, and CI | A gate is the required set of checks before delivery. A strict build enforces the repository's formatting and warning requirements. CI means continuous integration, the automated checks run for pushed changes. |
| Pass, PR, and commit | A pass is a planned batch of ASL work. A PR is a pull request proposing repository changes for review and merge. A commit is a recorded version of those changes. |
| Harness and build artifact | A harness runs repeatable checks or measurements. A build artifact is generated output, such as compiled binaries, kept separately from source. |
| Atomicity, append, and write-behind | Atomicity prevents readers from observing a partly completed write. Append adds records to a history, although the current store rewrites its file to do so. Write-behind delays persistence after accepting an in-memory change and therefore needs explicit durability and consistency rules. |
| Audit log and bounded tail | The audit log records operations and their outcomes. Its tail is the most recent portion; bounded-tail reading obtains the requested recent records without reading the entire log. |
| Persistence, durability, and consistency | Persistence stores data beyond its in-memory lifetime. Durability describes whether accepted writes survive failures. Consistency means readers and writers observe data according to the storage contract, including when several Studios share it. |
| Refactor | A change to code structure that preserves the required externally observable behavior. Performance refactors still need evidence that their results remain equivalent. |
| Tolerance | The permitted difference in a comparison, such as the established 1/32-pixel allowance for transformed face coordinates. It does not authorize unrelated drawing differences. |
| Build configuration and Web SDK | Build configuration selects how source is compiled and packaged. The .NET Web SDK supplies web-application build rules, including content copying; those rules caused the nested-output problem described below. |
| Outside-output registry | The record maintained by `outside_outputs.sh` of build outputs created outside the repository, used to identify and clean up the current work's generated files. |
| Backlog | The project's recorded work that remains deferred or unresolved, with its dependencies and scope decisions. |

## Completed results to preserve

The SVG work merged in PR 19, footprint and stable placement identifiers in PR 20, both counter-layer parts in PR 22, Play Step work in PR 23, and A3 with A3b in PR 25 (`3e5259d7`). Build-output management merged in PR 24; its path-matching repair merged in PR 26. A3 is complete, including its whole-game measurement and equivalence gate.

| Workload and comparison | Before | Completed result |
|---|---|---|
| Exact bd01 render allocation, SVG pass | 9.51 MB | 1.18 MB, 88% less |
| 80 counters with figures, render allocation, SVG pass | 56.24 MB | 11.16 MB, 80% less |
| Whole Replay allocation per step, SVG pass, three paired runs | 20.95 MB | 9.40 MB, 55% less |
| Replay allocation per step, counter-layer comparison on steps 100 to 160 | 13.1 MB | 4.3 MB, 67% less |
| Representative counter layer, counter-layer comparison | 696,687 characters; about 4,425 elements | 125,652 characters; 1,431 elements |
| Replay forward/backward click to step held in page, counter-layer comparison | 187/165 ms median | 63/77 ms median; excludes waiting for a painted frame |
| Rout Phase median, Play Step pass | 567 ms | 258 ms |
| Rout Phase median, A3 whole-game comparison | 258 ms morning baseline; 485 ms baseline under later load | 229 ms first run; 365 ms under the later load |
| Heavy Play step allocation, revisions 621 to 622 | About 315 MB before Play Step; 101 MB after it | 68 MB after A3b |
| Median Rout Phase board lookups and location resolutions, A3 probe | 27,990 lookups; 34,472 resolutions | 1 lookup; 180 resolutions |
| Gambit placed-map Rout steps, A3 measurements | 22.5 to 49.5 s | About 0.05 to 0.7 s |
| Berserk charge step, `dl-tractor-works-02` revision 1316, Berserk Charge Plan | 12.8 s; 5 searches of 2.2 s; about 1.4 GB | 0.37 s; 1 search of 0.29 s; about 102 MB |
| ASL CI run, one job against five parallel jobs (PR 35, 2026-10-24) | 11:54 | 5:37 |
| Authoring tests (PR 36, 2026-10-24) | 7:58 on this Windows machine; 4:00 in CI | 0:18 on this machine; 0:17 in CI |
| MapStudio tests in CI: the layer oracle's comparison, then the counter layer tests in three classes (PR 37, 2026-10-24) | 3:11; whole run 5:16 | 2:07 after the oracle; 1:33 after the split; whole run 3:27 |

These are reference measurements, not universal timing limits. Workloads, instrumentation, and machine load differ; do not multiply their percentages into a cumulative speedup. The heavy-step sequence of approximately 315 to 101 to 68 MB does provide a recorded allocation progression for that step.

A3's ordinary steps measured 114 ms against 104 ms in the morning comparison, and 216 against 207 ms under later load. The plan records the difference as unresolved within measurement noise, not as a passed no-regression target. Keep ordinary phases visible alongside Rout in future measurements.

Allocation and retained memory are separate. After the first SVG pass, the live heap remained about 64 to 66 MB while peak committed and private memory rose. The investigation associated the additional large-object-heap capacity with Server GC's adaptive heap count. Later allocation and markup reductions do not establish a final reduction in process memory. Keep the default collector settings unless a separately measured change justifies altering them.

## Planner reads belong to a state

Use `GamePlanner`'s existing `MemoOf(state)` and `PlayStepMemo` for repeated derived answers about an immutable `GameState`. The `ConditionalWeakTable` keys by the state object and permits the memo to be collected when that state is no longer retained. Do not add runtime caches to serialized state or replace this lifetime with a process-wide dictionary retaining every revision.

- Identify every input before caching an answer. State identity alone does not distinguish a side, view, unit argument, starting location, or other caller-specific input.
- Preserve reference identity where the existing memo uses it. A modified unit object must not reuse the state's unit answer merely because the IDs match.
- Share view-independent facts only when they are independent of disclosure. Key side-specific answers by side; key page projections by state and viewer.
- Preserve the rout settlement boundary. Do not use the settled-state rout stores during the surrender decision window. Check settlement before computing, as the current implementation does.
- Keep cached results immutable to callers. A retained collection must not become another search's mutable working storage.
- Keep fills safe across circuits. The shared planner permits concurrent fills to compute equivalent answers more than once during a race. `GetOrAdd` is not an exactly-once guarantee; factories must have no externally visible side effects. Add stricter coordination only for correctness or measured contention.

Avoid repeated full-state scans inside per-unit or per-neighbor loops. Reuse enemy lists, encirclement answers, rout advice, and map reads. Add another index only after its benefit justifies construction and retention cost. The state-object index (C2) is built in the memo (`ObjectIndex`, read through `GamePlanner.ObjectsAt` and `LocationOf`) and used by the berserk charge search and `BypassStep` only ([Berserk Charge Plan](<ASL Map Studio Berserk Charge Plan.md>), ruling C1). Extend its use only with a measurement; backlog 70 names the hand-over's victory and view reads as the next candidates.

## Use the state map context

New repeated planner map queries must use the existing helpers: `Handle(state, board)`, `Composed(state)`, `ReadLocation(state, location)`, `Step`, and the LOS helpers as appropriate. Do not add direct catalog calls, composed-map construction, or location resolution inside traversal loops when these helpers already provide the answer.

Preserve pinned board versions, retained handle results, lazy composed reads, and location reads keyed by the complete `BoardLocation`, including board and level. Preserve missing results as well as successful ones, and existing LOS reuse tied to map or handle identity.

A footprint published while a state lives is observed from the next state onward. Tests for newly visible footprints must create a genuinely new state; replaying the same events may return the existing state.

Preserve seam ownership, reversed-board coordinates, null results, and fallback behavior. In particular, `Step` still resolves directly through the board handle in its established fallback when a placed map cannot be composed. Do not replace refusal with success under a different coordinate interpretation.

One-off setup, proposal, and replay-initialization reads were deliberately left outside parts of A3. Assess their actual frequency and semantics when extending them; these guidelines do not require rewriting every catalog call.

## Cache ownership and invalidation

Every new cache must state its owner, lifetime or bound, complete key, invalidation rule, missing-result policy, and concurrency behavior in the design. Test the inputs whose omission would return stale or incorrectly disclosed answers.

| Existing cache or retained value | Contract to preserve |
|---|---|
| Planner memo | State object lifetime; side and unit distinctions in the relevant stores |
| Charge answers and the object index (Berserk Charge Plan) | State object lifetime; a charge kept only for the state's own unit objects and its own move, keyed by movers, start, and move; the index built from the state's own `Location` and `Aboard`, read only |
| Page unit rows | Reuse for the same state and viewer; recompute when either changes |
| Unit fragment cache | Shared through `UnitLibrary`; four-million-character bound including keys and markup; least-recently-used eviction; no rendering while holding its lock |
| Fragment key | Complete drawing inputs, renderer identity, and lead; warnings returned on hits and misses |
| Render cache | Last requested version of each board; UTF-8 bytes and gzip copy; no accumulation of authoring revisions |
| Placement-map cache | Existing bounded recent-map policy; save and delete invalidate all builds of the affected map |
| Missing campaign footprint | Five-second absence window; local publish invalidates immediately; another Studio's publication becomes discoverable after expiry |
| Built-in scenario text | Reuse as immutable embedded content; do not apply this lifetime to mutable user files |
| TIR payload digest and validation (`TirCanonicalJson`) | `TirDocument` object lifetime; only a document that passed validation is kept, so an invalid one still fails each time |
| Extracted TIR of a verification batch (`AslScenarioA1VerificationBatchBuilder`) | `GeneratedManifests` object lifetime; keyed by the attestation's time and source, the only attestation inputs of the extraction |
| Shared test manifests (`SharedManifests.AtSourceCommit`) | One test run; generated once at the pinned source commit; read only by the tests |

When adding a drawable field, update the fragment key and its property-coverage test together. Include concealed, uninspected, Dummy, attachment, facing, level, and state distinctions where they affect output. Renderer identity works because renderer inputs are fixed at construction; making a renderer mutable would invalidate that assumption.

Cached hex fragments retain their face definitions. A separate face cache was not implemented and is not required by the completed design.

## Preserve incremental counter rendering

Extend `UnitOverlayBuilder`, overlay fragments, `UnitLayerSync`, and `BoardViewport.ShowUnits`. Normal steps reuse unchanged hex fragments and send patches for changed hexes and face definitions. Do not force the lazy whole-layer `Svg` merely to compare, rename, log, or transmit an overlay.

Keep placement-set identity independent of revision. Labels can show the revision without changing identity and triggering replacement. Build leads and titles through the renderer rather than rewriting completed SVG with regular expressions.

Keep outlines shared through the viewport sprite and identical faces shared through definitions. Standalone exports and previews remain self-contained. New figures and counter states must maintain definition availability before references are drawn and remove unused faces through the patch mechanism.

Patches must preserve paint order, all levels within a hex, removals, selection, keyboard focus, highlight order, and active zoom tier. A mismatched patch base must reject the patch and permit full-send recovery. Board loads and view changes may require full sends; ordinary changes should not force them.

All three zoom tiers remain available without a server round trip. This decision followed measurement of the completed patch and shared-face design. Do not introduce tier-fetch latency as an incidental optimization.

## Allocation and file access in frequently called code

Use `SvgWriter`, `SvgValue`, fixed-point span formatting, and the existing path-writing APIs. Escape ordinary text. Trusted output is reserved for inputs validated by the established path contract. Preserve pooled-buffer ownership and never retain a buffer reference after return to the pool.

In measured hot loops, avoid capturing lambdas, boxed interface enumerators, intermediate strings, and repeated materialization. Follow the plain loops in `MapInPlay.Board` and the style cascade where appropriate. This is not a ban on LINQ or a reason to rewrite cold code without evidence.

Reuse cached board bytes at HTTP endpoints. Preserve gzip negotiation, `Vary: Accept-Encoding`, ETags, and conditional responses. The Blazor connection was already compressed; duplicate application compression is not an established optimization.

Keep disk checks and whole-file reads outside repeated map and page queries. Read audit tails through the bounded-tail path, preserving record boundaries and ordering. Do not restore whole-log reads to select the last few lines.

The game store still rewrites the whole file on append. Changing that requires its own storage design preserving atomicity, ordering, replay, and multiple Studios sharing the directory. An unplanned write-behind or process-local file cache can violate those contracts.

## Measure the behavior a feature changes

Before a performance change, state the workload, baseline commit, expected benefit, acceptance threshold, and behavior that must remain equal. For a new feature, identify added per-state, per-unit, per-hex, and per-step work and choose representative regression checks. A3's 15 ms and 25% thresholds were specific to that work; derive new thresholds from the proposed change.

Use the [Play Step measurement instructions](<../../../../code-builders/diagnostics/play-step/README.md>) and existing tools. Record build configuration, data and board versions, view, revision range, pacing, instrumentation, and machine load.

1. Compare identical games and revisions in before and after Studios built the same way. Use `dl-tractor-works-02`, or a cut of it, for play; restore `a3-gambit-rout` from the retired games when placed-map reads change. Add a representative scenario for newly introduced units or rules; these fixtures alone do not establish larger-game scalability.
2. Use server-paced Replay or the Play follower, with the measured tab in front. Hidden-pane frame throttling and browser timers made earlier comparisons invalid.
3. Keep builds, tests, and avoidable heavy work out of timed runs. Record competing load and stalls; repeat a comparable pair rather than calling a small difference a gain.
4. Split Play by phase. Report Rout and ordinary phases separately, with counts, medians, tail timings, and stalls. Short runs diagnose; whole-game runs confirm overall results.
5. Measure per-step allocation with allocation ticks and `allocread.cs`. The cumulative allocation counter can stall and report accumulated gigabytes in one interval. That jump does not measure one step.
6. Attribute time through outer trace frames. Sampled leaf timings can charge preceding loop work to a clock or native call. Use targeted counts and timers when deciding whether repeated calls justify optimization; account for probe overhead.
7. Separate allocation, live heap, GC-committed memory, private memory, CPU, server work, and time to painted output. A reduction in one does not prove a reduction in another. Do not sum nested inclusive frames.

Keep historical measurements attached to their original methods. Use the improved allocation method for new claims. Investigate retained memory with comparable fresh-process runs and heap evidence. Exercise long-lived cache bounds through repeated revisions, edits, and games.

## Prove correctness before accepting the gain

Follow the established task order: implement, build, check behavior in the Studio, repair until correct, then run the targeted test classes reached by the change. Use the full gate and CI according to the standing rules. Scope verification to the change; prose-only edits do not need performance runs.

| Change | Evidence required |
|---|---|
| Rule memo or map read | Equal answers on repeated reads and a new state; affected view, side, version, missing-read, seam, and footprint boundaries |
| Counter cache | Cached output equals uncached; keys cover drawing inputs; eviction, warnings, and concurrency remain correct |
| SVG writer | Unchanged standalone goldens, escaping, and number formatting |
| Shared definitions or placement | Rendering-equivalence oracle; resolved references; existing 1/32-pixel tolerance where face-origin transformation requires it |
| Browser patches | Patch folding equals the next overlay; forward, backward, jump, rejected-base recovery, focus, selection, order, and tiers |
| Planner refactor without rule changes | Text-list, replay-digest, and sweep equivalence, including the representative game |
| New rule behavior | Every proof difference explained by the intended rule change; unchanged cases retain their answers |

Keep standalone goldens unchanged for performance-only work. Explain intentional layer manifest or golden changes and prove equivalent drawing through the oracle. Updating a hash alone is not proof of equivalence.

Measure counts as well as elapsed time when claiming eliminated work. A3's probe established reuse despite noisy timings. Keep temporary probes out of production code unless explicitly designed as supported diagnostics.

## Build and measurement hygiene

Follow the standing output-location rules. Do not add build folders under MapStudio's `bin`; nested Web SDK content copies caused apparent hangs lasting 20 to 35 minutes. Use the established gate path and external Studio builds, leaving the user's running Studio undisturbed.

Register outside outputs when created with `outside_outputs.sh`, list them in the implementation plan, and clean them at the earliest opportunity. Confirm the registry is clear at completion. Remove only artifacts owned by the current work. Keep reusable scripts under `code-builders` and commit them with the change rather than copying an old pass's harness.

## Fast unit tests and CI time

Test time is paid on every CI run, about two per pass, and on this machine at the gate. In October 2026 the Authoring tests took 4:00 in CI and 7:58 here, not because they checked much but because each test rebuilt the same expensive inputs: fifty tests generated the same manifests, about 109 builder calls each extracted the full rulebook TIR (3.6 s), and every verification record hashed that whole document again (0.21 s a record). PR 36 removed the repetition without changing what any test asserts, and the project now runs in 18 seconds here.

- **Build an expensive, read-only test input once per test run.** Put it behind a `Lazy<T>` in an internal static class and have the tests read it; `SharedManifests.AtSourceCommit` is the model. Share only inputs no test changes. A test that needs a variant makes its own copy with `with`, never alters the shared one.
- **Do not repeat whole-document work for each item.** A digest, a validation, or an extraction over an immutable document belongs to that document's lifetime: key it by the instance with `ConditionalWeakTable`, as `TirCanonicalJson` and `AslScenarioA1VerificationBatchBuilder` do, never with a process-wide dictionary. Keep only successes, so an invalid input still fails every time. The section on cache ownership applies to these caches as to any other.
- **Do not let a cache hollow out a test.** A test that proves output reproducible by computing it twice must compute from two fresh inputs; reading the second answer from a cache proves nothing.
- **Measure before changing anything.** Run the project once with `--logger trx` and rank the tests with `code-builders/build-and-test/trx_rank.py <file.trx>`. xUnit runs test classes in parallel, so a class's time includes contention and the slowest class sets the project's length. Then time the suspected steps in a small harness outside the test runner before deciding what to share or cache. Time a slow class alone as well: in October 2026 two MapStudio tests took 93 s in the full run and 13 s alone, slowed only by the classes beside them.
- **Skip work whose answer is already known, and keep the answer the same.** The layer oracle tokenized every path of two identical layers to compare them within a tolerance; identical text with no move is equal under any tolerance, so it now returns at once (71 s to 6 s). A comparison that gains a fast path also gains a test that it still finds a difference.
- **Do not let one class hold many slow tests.** xUnit runs a class's tests one after another, so a class of heavy tests sets its project's length. Split it into classes of similar weight; each may keep its own fixture when that costs seconds. The three counter layer classes took MapStudio's tests in CI from 2:07 to 1:33, though on this machine, where every core was already busy, they gained 10 s.
- **Know how CI runs the tests.** Since PR 35, `asl-authoring-ci.yml` runs five parallel jobs, each on its own machine, and names each test project in one share. A new test project must be added to a share, or CI does not run it. Put it in "the rest" unless its time would make that job the longest.

## Design and review checklist

Each implementation design should answer the applicable questions briefly, with evidence links:

- What work is added per state, unit, hex, or step, and which existing read or rendering path performs it?
- Are ownership, key inputs, lifetime, bounds, invalidation, and concurrent fills correct?
- Are disclosure, rout settlement, state snapshots, pinned versions, and seams preserved?
- Do unchanged counters reuse fragments and avoid whole-layer construction and transmission?
- Which behavioral checks and measurements detect regression?
- Do new tests build each expensive input once per run, and is any new test project named in a CI share?
- Does the benefit exceed noise without unexplained regressions elsewhere? If not, what is corrected or explicitly unresolved?
- Are results, limitations, backlog items, and outside-output cleanup recorded before completion?

Keep unresolved work visible: ordinary-step timing after A3, the setup-closing delay, whole-file appends and intermittent append stalls, remaining route-search cost, Replay's other page work, and final process-memory behavior. These do not authorize expanding a pass without a scope decision.

## Evidence and implementation references

- [SVG Memory Analysis](<ASL Map Studio SVG Memory Analysis.md>), sections 10 to 14: writer, cascade, caches, compression, memory investigation, footprint checks, and stable identifiers.
- [Counter Layer Design](<ASL Map Studio Counter Layer Design.md>), sections 7.2, 7.3, and 16: actual cache key, bounded fragments, patches, shared faces, results, and tiers.
- [Play Step Analysis](<ASL Map Studio Play Step Analysis.md>), sections 2 and 8: phase costs, measurement corrections, and results.
- [A3 Map Context Plan](<ASL Map Studio A3 Map Context Plan.md>), sections 10.2 to 10.5: retained reads, final measurements, ordinary-step qualification, and gate.
- [Planner memo](<../../LimboDancer.Domains.Asl.Play/GamePlanner.PlayStepMemo.cs>) and [map reads](<../../LimboDancer.Domains.Asl.Play/GamePlanner.Map.cs>): state lifetime, concurrency, rout settlement, and map reuse.
- [ASL Unit Backlog](<../ASL Unit Backlog.md>), sections 62 and 64 to 66: deferred performance, tooling, and diagnostic work.

Update these guidelines when an implemented and verified change supersedes a contract. Update measurements and references alongside it so later passes can distinguish current behavior from historical experiments.
