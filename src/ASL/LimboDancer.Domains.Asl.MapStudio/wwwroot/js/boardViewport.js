// Board viewport (Architecture and Rendering Design, sections 5.3 and 6). Layer fragments are fetched as SVG and imported
// into inline <svg> elements; pan and zoom only change the viewBox, so the browser never rasterizes anything we send.
// Editing gestures are collected here and sent to .NET complete; the server snaps coordinates and decides geometry.

const svgNamespace = "http://www.w3.org/2000/svg";
const hoverInterval = 100;
// The quiet time after the last wheel event at which a gesture is taken as ended (Large Campaign Map Plan, A4 and B3).
const gestureRest = 200;
const rotationKey = "studio.map.rotated";

// Views are scoped to game/viewer and the exact rendered board version. Storage failure falls back to this page.
const keptViews = new Map();
const viewPrefix = "studio.map.view.v1.";

function rememberView(state) {
    if (!state.viewKey || !state.box || !state.home) return;
    const saved = { home: state.home, box: state.box, fitted: state.fitted };
    keptViews.set(state.viewKey, saved);
    try { localStorage.setItem(viewPrefix + state.viewKey, JSON.stringify(saved)); } catch { /* Page-only fallback. */ }
}

function readView(key, home) {
    if (!key) return null;
    let saved = keptViews.get(key);
    try { saved = JSON.parse(localStorage.getItem(viewPrefix + key)) || saved; } catch { /* Retain page-only view. */ }
    if (!saved || typeof saved.fitted !== "boolean" ||
        !["x", "y", "width", "height"].every(name => saved.home?.[name] === home[name] && Number.isFinite(saved.box?.[name])) ||
        saved.box.width <= 0 || saved.box.height <= 0) return null;
    return saved;
}

export function create(host, dotnet) {
    const svg = document.createElementNS(svgNamespace, "svg");
    svg.setAttribute("class", "board-svg");
    host.replaceChildren(svg);
    host.tabIndex = 0;

    const state = {
        host, svg, dotnet, side: null, box: null, home: null, drag: null, generation: 0, highlight: null,
        tool: "select", points: [], overlay: null, lastHover: 0, comparison: { mode: "none", value: 0.5 }, rotated: false, shape: null,
        // Pan by Transform Plan: a gesture moves the painted drawing; the view box is committed when it rests. Off with ?transform=0.
        pan: typeof host.classList?.add === "function" && typeof host.getBoundingClientRect === "function" && !transformOffInAddress(),
        committed: null, gesturing: false,
        // Screen-Bounded Map Analysis, decision N6: the new presentation behind ?present=bounded until it is measured the better.
        bounded: boundedInAddress(), boundaryGame: null, near: false, gridShown: true,
    };
    state.panShown = state.pan;
    if (state.pan) {
        host.classList.toggle("pan", true);
    }

    // Pass 29: the turn is remembered for each shape of the map's pane, so turning a tall pane never turns a wide one. A pane resized into the
    // other shape takes that shape's choice.
    // A view still at its fit is fitted again when the pane changes size, such as when the context above it grows.
    // Large Campaign Map Plan, A2: the drawing's own size comes from this observer, never from clientWidth read in an event, since that read
    // forces the browser to lay out the whole map. The SVG is observed itself: side by side it is half the host.
    const shapeWatcher = typeof ResizeObserver === "undefined" ? null : new ResizeObserver(entries => {
        for (const entry of entries) {
            if (entry.target === svg) {
                state.size = { width: entry.contentRect.width, height: entry.contentRect.height };
                state.rect = null;
            } else if (entry.target === host) {
                state.hostSize = { width: entry.contentRect.width, height: entry.contentRect.height };
                state.rect = null;
                // A host whose height comes from its drawing (the card editor's map) collapses once the drawing is placed absolutely: that
                // viewport keeps the drawing in the flow and pans as before (Pan by Transform Plan, deviation E6).
                if (state.pan && entry.contentRect.width > 0 && entry.contentRect.height === 0) {
                    state.pan = false;
                    state.panShown = false;
                    host.classList.toggle("pan", false);
                }
            }
        }

        applyShape(state);
        if (state.fitted) {
            reset(state);
        } else if (panOn(state) && state.box) {
            // A new pane size paints a new margin.
            setBox(state, state.box, false);
        } else {
            updateUnitTier(state);
            updateLabelDetail(state);
            updateTiles(state);
        }
    });
    shapeWatcher?.observe(host);
    shapeWatcher?.observe(svg);

    // The view is saved when a gesture ends, and when the page is hidden (A4).
    const onHide = () => rememberView(state);
    globalThis.addEventListener?.("pagehide", onHide);
    const onScroll = () => { state.rect = null; };
    globalThis.addEventListener?.("scroll", onScroll, { capture: true, passive: true });
    svg.addEventListener("pointerenter", () => takeRect(state));
    try {
        localStorage.removeItem(rotationKey);
    } catch {
        // Storage is blocked: nothing to remove.
    }

    applyShape(state);

    svg.addEventListener("wheel", event => {
        if (!state.box) {
            return;
        }

        event.preventDefault();
        gestureStarted(state);
        zoomAt(state, toBoard(state, event.clientX, event.clientY), event.deltaY < 0 ? 0.8 : 1.25, true);
        clearTimeout(state.wheelRest);
        state.wheelRest = setTimeout(() => gestureEnded(state), gestureRest);
    }, { passive: false });

    svg.addEventListener("pointerdown", event => {
        if (!state.box || event.button !== 0) {
            return;
        }

        host.focus({ preventScroll: true });
        takeRect(state);

        // Pointer capture retargets pointerup to the SVG root. Remember the actual unit hit here.
        const unitId = event.target.closest?.("[data-unit-id]")?.getAttribute("data-unit-id");
        state.drag = {
            id: event.pointerId, x: event.clientX, y: event.clientY, box: { ...state.box }, moved: false, unitId,
            start: toBoard(state, event.clientX, event.clientY),
        };
        try {
            svg.setPointerCapture(event.pointerId);
        } catch {
            // A pointer the browser does not know (the pan probe's dispatched events) cannot be captured; the drag works without it.
        }
    });

    svg.addEventListener("pointermove", event => {
        const drag = state.drag;
        const point = toBoard(state, event.clientX, event.clientY);
        if (!drag || drag.id !== event.pointerId) {
            const now = performance.now();
            if (now - state.lastHover >= hoverInterval) {
                state.lastHover = now;
                state.dotnet.invokeMethodAsync("OnBoardHover", point.x, point.y);
            }

            drawGesture(state, point);
            return;
        }

        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (!drag.moved && Math.hypot(dx, dy) < 4) {
            return;
        }

        drag.moved = true;
        if (state.tool === "move") {
            drawLine(state, [drag.start, point], false);
            return;
        }

        gestureStarted(state);
        // Turned a quarter clockwise (pass 29), the map's x runs down the screen and its y runs to the left.
        // Board units per screen pixel from the part of the board on screen, not the view box: a letterboxed map shows more than its box,
        // so the box's width over the pane's moved the map at half the pointer's speed on a 2:1 letterbox (backlog section 96).
        const size = paneSize(state);
        const unitsPerPixel = visibleBox(drag.box, size).width / size.width;
        const [mx, my] = state.rotated ? [dy, -dx] : [dx, dy];
        setBox(state, { ...drag.box, x: drag.box.x - mx * unitsPerPixel, y: drag.box.y - my * unitsPerPixel }, false, true);
    });

    svg.addEventListener("pointerup", event => {
        const drag = state.drag;
        state.drag = null;
        if (!drag || drag.id !== event.pointerId) {
            return;
        }

        const point = toBoard(state, event.clientX, event.clientY);
        if (drag.moved) {
            gestureEnded(state);
            if (state.tool === "move") {
                clearOverlay(state);
                state.dotnet.invokeMethodAsync("OnBoardDrag", drag.start.x, drag.start.y, point.x, point.y);
            }

            return;
        }

        if (drag.unitId && state.svg.querySelector("#layer-units")) {
            state.dotnet.invokeMethodAsync("OnUnitClick", drag.unitId);
            return;
        }

        if (state.tool === "polygon" || state.tool === "polyline") {
            state.points.push(point);
            drawGesture(state, point);
            return;
        }

        state.dotnet.invokeMethodAsync("OnBoardClick", point.x, point.y);
    });

    svg.addEventListener("dblclick", event => {
        if (state.tool === "polygon" || state.tool === "polyline") {
            event.preventDefault();
            finishGesture(state);
        }
    });

    svg.addEventListener("keydown", event => {
        const unit = event.target.closest?.("[data-unit-id]");
        if (unit && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
            event.stopPropagation();
            state.dotnet.invokeMethodAsync("OnUnitClick", unit.getAttribute("data-unit-id"));
        }
    });

    // Keyboard zoom and editing shortcuts while the board has focus (section 5.3). The listener is on the host, which outlives the
    // viewport, so dispose removes it: a host given a new viewport never answers a key twice.
    const onKey = event => {
        if (event.target.closest?.("[data-unit-id]")) {
            return;
        }

        if (event.key === "+" || event.key === "=") {
            zoomCenter(state, 0.8);
        } else if (event.key === "-" || event.key === "_") {
            zoomCenter(state, 1.25);
        } else if (event.key === "0") {
            reset(state);
        } else if (event.key === "Escape") {
            state.points = [];
            clearOverlay(state);
            state.dotnet.invokeMethodAsync("OnBoardKey", "Escape");
        } else if ((event.key === "[" || event.key === "]") && !event.ctrlKey && !event.metaKey && !event.altKey) {
            state.dotnet.invokeMethodAsync("OnBoardKey", event.key);
        } else if (event.key === "Enter" && state.points.length > 0) {
            finishGesture(state);
        } else if (event.key === "Backspace" && state.points.length > 0) {
            state.points.pop();
            drawGesture(state, null);
        } else if (event.key === "Delete") {
            state.dotnet.invokeMethodAsync("OnBoardKey", "Delete");
        } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "z") {
            state.dotnet.invokeMethodAsync("OnBoardKey", event.shiftKey ? "Redo" : "Undo");
        } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "y") {
            state.dotnet.invokeMethodAsync("OnBoardKey", "Redo");
        } else {
            return;
        }

        event.preventDefault();
    };
    host.addEventListener("keydown", onKey);

    return {
        load: (baseUrl, query, viewBox, layers, visible) => load(state, baseUrl, query, viewBox, layers, visible),
        reloadLayers: (baseUrl, query, layers) => reloadLayers(state, baseUrl, query, layers),
        setVisible: visible => setVisible(state, visible),
        setUnits: (markup, version) => setUnits(state, markup, version),
        applyUnitPatch: patch => applyUnitPatch(state, patch),
        setUnitFigures: markup => setUnitFigures(state, markup),
        setLos: markup => setLos(state, markup),
        setMarks: markup => setMarks(state, markup),
        setBoundaries: game => setBoundaries(state, game),
        isBounded: () => state.bounded,
        highlight: points => highlight(state, points),
        setTool: tool => setTool(state, tool),
        applyPatch: patch => applyPatch(state, patch),
        setComparison: (mode, value) => setComparison(state, mode, value),
        focusPoint: (x, y) => focusPoint(state, x, y),
        copyText: text => navigator.clipboard?.writeText(text),
        reset: () => reset(state),
        zoom: factor => zoomCenter(state, factor),
        reveal: (x, y) => revealPoint(state, x, y),
        fitRect: (x, y, width, height) => fitRect(state, x, y, width, height),
        setRotation: on => setRotation(state, on, true),
        isRotated: () => state.rotated,
        hasRestoredView: () => state.restoredView === true,
        dispose: () => {
            rememberView(state);

            clearTimeout(state.wheelRest);
            globalThis.removeEventListener?.("pagehide", onHide);
            globalThis.removeEventListener?.("scroll", onScroll, { capture: true });
            shapeWatcher?.disconnect();
            host.removeEventListener("keydown", onKey);
            host.replaceChildren();
        },
    };
}

// Large Campaign Map Plan, task 4: the Exact view's hex labels come as data (labels.json) and are drawn only for the hexes in view, so a large
// map no longer holds one element per hex. Other views keep their SVG label layer.
function labelsAsData(baseUrl, layer) {
    return layer === "labels" && baseUrl.endsWith("/exact");
}

async function fetchGroups(baseUrl, query, layers, tiled = false) {
    let labelIndex = null;
    let tileIndex = null;
    // Raster Tiles Plan, section 2.5: with tiles, one group stands where the two Exact terrain layers stood, and neither is fetched.
    const wanted = tiled ? layers.filter(layer => !tileLayers.includes(layer) || layer === tileLayers[0]) : layers;
    const texts = await Promise.all(wanted.map(async layer => {
        if (tiled && layer === tileLayers[0]) {
            const response = await fetch(`${baseUrl}/tiles.json${query}`);
            if (!response.ok) {
                throw new Error(`Tile index failed: ${response.status}`);
            }

            tileIndex = await response.json();
            return undefined;
        }

        const asData = labelsAsData(baseUrl, layer);
        const response = await fetch(asData ? `${baseUrl}/labels.json${query}` : `${baseUrl}/${layer}.svg${query}`);
        if (!response.ok) {
            throw new Error(`Layer ${layer} failed: ${response.status}`);
        }

        if (asData) {
            labelIndex = await response.json();
            return null;
        }

        return response.text();
    }));
    const parser = new DOMParser();
    const groups = texts.map(text => text === undefined ? tileGroup()
        : text === null ? labelGroup(labelIndex)
            : document.importNode(parser.parseFromString(text, "image/svg+xml").documentElement.firstElementChild, true));
    return { groups, labelIndex, tileIndex };
}

// Raster Tiles Plan: the Exact view's terrain and elevation layers, drawn as raster tiles from the grid.
const tileLayers = ["exact-terrain", "exact-elevation"];
const tileSettleMs = 150;
const tilePoolBound = 200;

// Whether a load draws tiles: an Exact view, in a viewport that has not turned them off (the map author page), and not ?tiles=0.
function tilesWanted(state, baseUrl, layers) {
    return typeof globalThis.Image === "function" && state.host.dataset?.tiles !== "off" && baseUrl.endsWith("/exact")
        && layers.includes(tileLayers[0]) && !tilesOffInAddress();
}

// ?tiles=0 in the address, or in the address the page was opened with: Replay rewrites its address to its own parameters before the map
// loads, which dropped the switch; the browser's navigation entry keeps the address as typed.
export function tilesOffInAddress(addresses = [globalThis.location?.href, globalThis.performance?.getEntriesByType?.("navigation")?.[0]?.name]) {
    return addresses.some(address => {
        try {
            return address ? new URL(address).searchParams.get("tiles") === "0" : false;
        } catch {
            return false;
        }
    });
}

function tileGroup() {
    const layer = document.createElementNS(svgNamespace, "g");
    layer.setAttribute("id", "layer-exact-tiles");
    layer.setAttribute("pointer-events", "none");
    return layer;
}

// The tile level for a drawing scale (screen pixels per cell, times the device pixel ratio): the coarsest whose tile pixel is no larger
// than a device pixel. Then the tiles meeting the box widened by one tile, each with its place and size in cells.
export function tilesInView(index, box, devicePixelsPerCell) {
    const top = index.levels - 1;
    const level = Math.max(0, Math.min(top, Math.floor(Math.log2(1 / devicePixelsPerCell))));
    return tilesAtLevel(index, box, level);
}

// The tiles of one level meeting the box widened by one tile, each with its place and size in cells.
export function tilesAtLevel(index, box, level) {
    const span = index.tileSize * 2 ** level;
    const columns = Math.ceil(index.width / span);
    const rows = Math.ceil(index.height / span);
    const first = value => Math.max(0, Math.floor(value / span));
    const tiles = [];
    for (let column = first(box.x - span); column <= Math.min(columns - 1, Math.floor((box.x + box.width + span) / span)); column++) {
        for (let row = first(box.y - span); row <= Math.min(rows - 1, Math.floor((box.y + box.height + span) / span)); row++) {
            tiles.push({
                level, column, row, x: column * span, y: row * span,
                width: Math.min(span, index.width - column * span), height: Math.min(span, index.height - row * span),
            });
        }
    }

    return { level, tiles };
}

// The variant the two layer switches ask for: both, terrain alone, or elevation alone; null when both are hidden.
export function tileVariant(visible) {
    const terrain = visible.includes(tileLayers[0]);
    const elevation = visible.includes(tileLayers[1]);
    return terrain && elevation ? "te" : terrain ? "t" : elevation ? "e" : null;
}

function updateTiles(state) {
    const tiles = state.tiles;
    if (!tiles || !state.box || !tiles.variant || !tiles.group.isConnected || tiles.group.getAttribute("data-layer-hidden") === "true") {
        return;
    }

    const size = paneSize(state);
    if (!size.width || !size.height) {
        return;
    }

    const scale = Math.min(size.width / state.box.width, size.height / state.box.height);
    const ratio = globalThis.devicePixelRatio || 1;
    const view = tilesInView(tiles.index, drawnBox(state), scale * ratio);
    // Near zoom: level 0 magnified, where tile lines would thicken (decision N2).
    const near = view.level === 0 && scale * ratio > 1;
    if (state.near !== near) {
        state.near = near;
        refreshTileLines(state);
    }

    // While a zoom is still moving, keep the level drawn: asking for every level the zoom passes through paints tiles nobody sees.
    const sinceZoom = performance.now() - (state.lastZoom ?? -Infinity);
    if (tiles.level !== null && view.level !== tiles.level && sinceZoom < tileSettleMs) {
        clearTimeout(tiles.settle);
        tiles.settle = setTimeout(() => updateTiles(state), tileSettleMs - sinceZoom + 10);
        return;
    }

    const top = tiles.index.levels - 1;
    // The coarsest level's single tile stays under the rest for the whole visit, so a tile still loading shows softly, never blank.
    const wanted = [{ level: top, column: 0, row: 0, x: 0, y: 0, width: tiles.index.width, height: tiles.index.height },
        ...view.tiles.filter(tile => tile.level !== top)];
    const keep = new Set();
    let loading = false;
    for (const tile of wanted) {
        const key = `${tiles.variant}${tiles.lines ?? ""}-${tile.level}-${tile.column}-${tile.row}`;
        keep.add(key);
        if (!tiles.pool.has(key)) {
            tiles.pool.set(key, tileImage(state, tiles, tile, key));
        }

        loading ||= !tiles.pool.get(key).hasAttribute("href");
    }

    // Tiles no longer wanted stay under the new ones until those have decoded, so a level change never flashes the coarse tile; the pool's
    // bound drops them regardless.
    for (const [key, image] of tiles.pool) {
        if (!keep.has(key) && (!loading || tiles.pool.size > tilePoolBound)) {
            image.remove();
            tiles.pool.delete(key);
        }
    }

    // Sharp cells only when a cell is larger than a device pixel; shrunk tiles keep the browser's smoothing, so they do not shimmer.
    // A data attribute and a site.css rule, not a style attribute (site.css: viewport state without style attributes).
    const sharp = view.level === 0 && scale * ratio > 1;
    if (tiles.sharp !== sharp) {
        tiles.sharp = sharp;
        tiles.group.setAttribute("data-sharp", String(sharp));
    }

    tiles.level = view.level;
    prefetchFiner(state, tiles, view.level, size);
}

// Screen-Bounded Map Analysis, section 3.5: once the level in view is drawn, the next finer level's tiles for the part of the board on screen
// are fetched at low priority, so a zoom in finds them in the browser's cache. At most a few dozen a time, each once.
const prefetchBound = 48;

function prefetchFiner(state, tiles, level, size) {
    if (!state.bounded || level === 0 || typeof globalThis.Image !== "function"
        || [...tiles.pool.values()].some(image => !image.hasAttribute("href"))) {
        return;
    }

    tiles.prefetched ??= new Set();
    let started = 0;
    for (const tile of tilesAtLevel(tiles.index, visibleBox(state.box, size), level - 1).tiles) {
        const url = tileUrl(state, tiles, tile);
        if (started >= prefetchBound || tiles.prefetched.has(url)) {
            continue;
        }

        tiles.prefetched.add(url);
        started++;
        const image = new Image();
        image.fetchPriority = "low";
        image.src = url;
    }
}

function tileUrl(state, tiles, tile) {
    const lines = tiles.lines ?? "";
    const game = lines.includes("b") && state.boundaryGame
        ? `${tiles.query ? "&" : "?"}game=${encodeURIComponent(state.boundaryGame)}` : "";
    return `${tiles.baseUrl}/tile-${tiles.variant}${lines}-${tile.level}-${tile.column}-${tile.row}.png${tiles.query}${game}`;
}

// An image for one tile, placed in cells. Its file is fetched and decoded off-screen first, so a frame never waits on a decode.
function tileImage(state, tiles, tile, key) {
    const image = document.createElementNS(svgNamespace, "image");
    for (const [name, value] of [["x", tile.x], ["y", tile.y], ["width", tile.width], ["height", tile.height], ["preserveAspectRatio", "none"]]) {
        image.setAttribute(name, String(value));
    }

    const url = tileUrl(state, tiles, tile);
    const show = () => {
        if (tiles.pool.get(key) === image) {
            image.setAttribute("href", url);
            // Once the last new tile shows, the next pass drops the old ones kept under it.
            if (!tiles.sweep) {
                tiles.sweep = true;
                requestAnimationFrame(() => {
                    tiles.sweep = false;
                    updateTiles(state);
                });
            }
        }
    };
    const loader = new Image();
    loader.src = url;
    loader.decode().then(show, show);
    // The coarse tile first, then finer levels above it.
    if (tile.level === tiles.index.levels - 1) {
        tiles.group.insertBefore(image, tiles.group.firstChild);
    } else {
        tiles.group.appendChild(image);
    }

    return image;
}

function setTileVariant(state, visible) {
    state.gridShown = visible.includes("grid");
    const tiles = state.tiles;
    if (!tiles) {
        return;
    }

    const variant = tileVariant(visible) ?? (visible.includes("exact-tiles") ? tiles.variant ?? "te" : null);
    tiles.group.setAttribute("data-layer-hidden", String(variant === null));
    tiles.variant = variant ?? tiles.variant;
    refreshTileLines(state);
    updateTiles(state);
    // The grid switch governs the near grid too.
    updateNearGrid(state);
}

// Screen-Bounded Map Analysis, section 3: with ?present=bounded the grid and the Formation boundaries are painted into the tiles. The grid
// goes into them except at near zoom, where a tile line would be magnified and the vector grid of the hexes in view is drawn instead
// (decision N2); the boundaries go into them once the page names its game (setBoundaries).
export function tileLines(bounded, variant, gridShown, near, boundaryGame) {
    if (!bounded || !variant || variant === "e") {
        return "";
    }

    const grid = gridShown && !near;
    const boundaries = Boolean(boundaryGame);
    return grid && boundaries ? "+gb" : grid ? "+g" : boundaries ? "+b" : "";
}

// ?present=bounded in the address, or in the address the page was opened with, as ?tiles=0.
export function boundedInAddress(addresses = [globalThis.location?.href, globalThis.performance?.getEntriesByType?.("navigation")?.[0]?.name]) {
    return addresses.some(address => {
        try {
            return address ? new URL(address).searchParams.get("present") === "bounded" : false;
        } catch {
            return false;
        }
    });
}

// The tiles' lines for the present view. A change of variant or lines asks for other tiles; the old ones stay under them until those have
// decoded, as at a level change (updateTiles drops them then), so the map never shows blank (Screen-Bounded Map Analysis, section 5.5).
// With ?present=bounded the whole-map vector grid stands aside: the tiles carry the grid, and at near zoom the grid of the hexes in view is
// drawn instead (updateNearGrid).
function refreshTileLines(state) {
    const tiles = state.tiles;
    if (!tiles) {
        return;
    }

    const lines = tileLines(state.bounded, tiles.variant, state.gridShown, state.near, state.boundaryGame);
    const key = tiles.variant + lines;
    tiles.lines = lines;
    if (key !== tiles.key) {
        tiles.key = key;
        askAhead(state, tiles);
    }

    const grid = state.svg.querySelector("#layer-grid");
    const tiled = String(state.bounded);
    if (grid && grid.getAttribute("data-tiled") !== tiled) {
        grid.setAttribute("data-tiled", tiled);
    }
}

// Step 4 (decision N2): at near zoom, the grid of the hexes in the drawn box as one path, in the vector grid's style (BoardRenderer.WriteGrid:
// black outlines at 0.75 and opacity 0.45), built again only when the box changes.
export function nearGridPath(index, box) {
    if (!index?.hexWidth) {
        return "";
    }

    const columns = Math.round(index.width / index.hexWidth) + 1;
    const rows = Math.round(index.height / index.hexHeight);
    const side = index.hexWidth * 2 / 3;
    const half = index.hexHeight / 2;
    const first = Math.max(0, Math.floor((box.x - index.a1x) / index.hexWidth) - 1);
    const last = Math.min(columns - 1, Math.ceil((box.x + box.width - index.a1x) / index.hexWidth) + 1);
    const parts = [];
    const n = value => Math.round(value * 100) / 100;
    for (let column = first; column <= last; column++) {
        const offset = half * (column % 2);
        const top = Math.max(0, Math.floor((box.y - index.a1y + offset) / index.hexHeight) - 1);
        const bottom = Math.min(rows - 1 + (column % 2), Math.ceil((box.y + box.height - index.a1y + offset) / index.hexHeight) + 1);
        const x = index.a1x + index.hexWidth * column;
        for (let row = top; row <= bottom; row++) {
            const y = index.a1y + index.hexHeight * row - offset;
            parts.push(`M${n(x - side / 2)},${n(y - half)}H${n(x + side / 2)}L${n(x + side)},${n(y)}L${n(x + side / 2)},${n(y + half)}`
                + `H${n(x - side / 2)}L${n(x - side)},${n(y)}Z`);
        }
    }

    return parts.join("");
}

function updateNearGrid(state) {
    const grid = state.svg.querySelector("#layer-grid");
    let near = state.svg.querySelector("#near-grid");
    const wanted = state.bounded && state.near && state.gridShown && state.tiles && grid;
    if (!wanted) {
        near?.remove();
        return;
    }

    if (!near) {
        near = document.createElementNS(svgNamespace, "path");
        near.setAttribute("id", "near-grid");
        for (const [name, value] of [["fill", "none"], ["stroke", "#000000"], ["stroke-width", "0.75"], ["stroke-opacity", "0.45"],
            ["pointer-events", "none"]]) {
            near.setAttribute(name, value);
        }

        grid.after(near);
    }

    const box = drawnBox(state);
    const key = `${box.x} ${box.y} ${box.width} ${box.height}`;
    if (near.getAttribute("data-box") !== key) {
        near.setAttribute("data-box", key);
        near.setAttribute("d", nearGridPath(state.tiles.index, box));
    }
}

// Screen-Bounded Map Analysis, section 3.5: the server paints the shown variant's pyramid ahead, once per variant and visit (bounded only).
function askAhead(state, tiles) {
    if (!state.bounded || typeof globalThis.fetch !== "function") {
        return;
    }

    tiles.aheadAsked ??= new Set();
    const name = `${tiles.variant}${tiles.lines ?? ""}`;
    if (tiles.aheadAsked.has(name)) {
        return;
    }

    tiles.aheadAsked.add(name);
    const game = (tiles.lines ?? "").includes("b") && state.boundaryGame
        ? `${tiles.query ? "&" : "?"}game=${encodeURIComponent(state.boundaryGame)}` : "";
    globalThis.fetch(`${tiles.baseUrl}/ahead-${name}.json${tiles.query}${game}`).catch(() => { /* Painting ahead is an aid; a page works without it. */ });
}

// Screen-Bounded Map Analysis, sections 3.3 and 3.4: with ?present=bounded, counter fragments and setup marks outside the part of the board
// drawn (the committed box) are hidden by a data attribute and a site.css rule, so a commit lays out and paints only those in view. They stay
// in the page, so patches and the marks' replacement work as before; only their display changes, and only when it changes.
export function hexCentre(index, column, row) {
    return {
        x: index.a1x + index.hexWidth * column,
        y: index.a1y + index.hexHeight * row - index.hexHeight / 2 * (column % 2),
    };
}

// A counter fragment's box: its hex (data-hex "column,row") widened by a hex each way, room for a stack's offsets.
export function fragmentBounds(index, dataHex) {
    const [column, row] = (dataHex ?? "").split(",").map(Number);
    if (!Number.isFinite(column) || !Number.isFinite(row)) {
        return null;
    }

    const centre = hexCentre(index, column, row);
    return { x: centre.x - 1.5 * index.hexWidth, y: centre.y - 1.5 * index.hexHeight, width: 3 * index.hexWidth, height: 3 * index.hexHeight };
}

// A mark's box from the numbers of its first polygon's points (GameMaps.HexMark), or null when it has none: a mark of another shape is
// always shown.
export function pointsBounds(points) {
    const numbers = (points ?? "").split(/[\s,]+/).filter(Boolean).map(Number);
    if (numbers.length < 4 || numbers.some(value => !Number.isFinite(value))) {
        return null;
    }

    let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
    for (let i = 0; i + 1 < numbers.length; i += 2) {
        minX = Math.min(minX, numbers[i]);
        maxX = Math.max(maxX, numbers[i]);
        minY = Math.min(minY, numbers[i + 1]);
        maxY = Math.max(maxY, numbers[i + 1]);
    }

    return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
}

export function boxesMeet(a, b) {
    return a.x < b.x + b.width && b.x < a.x + a.width && a.y < b.y + b.height && b.y < a.y + a.height;
}

const cullBounds = new WeakMap();

function cullToView(state) {
    const index = state.tiles?.index;
    if (!state.bounded || !index?.hexWidth || !state.box) {
        return;
    }

    const box = drawnBox(state);
    const units = state.svg.querySelector("#layer-units");
    for (const fragment of units?.children ?? []) {
        if (!cullBounds.has(fragment)) {
            cullBounds.set(fragment, fragmentBounds(index, fragment.getAttribute("data-hex")));
        }

        markOut(fragment, cullBounds.get(fragment), box);
    }

    const marks = state.svg.querySelector("#layer-marks");
    for (const mark of marks?.children ?? []) {
        if (!cullBounds.has(mark)) {
            cullBounds.set(mark, pointsBounds(mark.querySelector("polygon")?.getAttribute("points")));
        }

        markOut(mark, cullBounds.get(mark), box);
    }
}

function markOut(element, bounds, box) {
    const out = String(bounds !== null && !boxesMeet(bounds, box));
    if ((element.getAttribute("data-out") ?? "false") !== out) {
        element.setAttribute("data-out", out);
    }
}

function setBoundaries(state, game) {
    state.boundaryGame = state.bounded && game ? game : null;
    refreshTileLines(state);
    updateTiles(state);
}

// The same group the SVG labels layer has (BoardRenderer.WriteLabels), empty: its texts are the labels in view, filled each frame.
function labelGroup(index) {
    const layer = document.createElementNS(svgNamespace, "g");
    layer.setAttribute("id", "layer-labels");
    const texts = document.createElementNS(svgNamespace, "g");
    for (const [name, value] of [["font-family", "Arial, sans-serif"], ["font-size", String(index.fontSize ?? 9)], ["text-anchor", "middle"],
        ["fill", "#000000"], ["fill-opacity", "0.75"], ["pointer-events", "none"]]) {
        texts.setAttribute(name, value);
    }

    layer.appendChild(texts);
    return layer;
}

// The indexes of the labels whose positions lie in the box widened by a margin on each side, in the index's order.
export function labelsInView(index, box, margin = 0.1) {
    const left = box.x - box.width * margin;
    const right = box.x + box.width * (1 + margin);
    const top = box.y - box.height * margin;
    const bottom = box.y + box.height * (1 + margin);
    const found = [];
    for (let i = 0; i < index.x.length; i++) {
        const x = index.x[i];
        const y = index.y[i];
        if (x >= left && x <= right && y >= top && y <= bottom) {
            found.push(i);
        }
    }

    return found;
}

function updateLabels(state) {
    const index = state.labelIndex;
    const layer = state.svg.querySelector("#layer-labels");
    if (!index || !layer) {
        return;
    }

    const shown = state.box && layer.getAttribute("data-detail-visible") === "true" && layer.getAttribute("data-layer-hidden") !== "true";
    const wanted = shown ? labelsInView(index, drawnBox(state)) : [];
    for (const svg of [state.svg, state.side]) {
        const texts = svg?.querySelector("#layer-labels")?.firstElementChild;
        if (!texts) {
            continue;
        }

        // With ?present=bounded the main map's labels are pixels in one canvas (drawLabelCanvas); the side view keeps its texts.
        if (svg === state.svg && state.bounded && typeof document.createElement === "function") {
            fillLabels(texts, index, []);
            drawLabelCanvas(state, layer, index, wanted);
            continue;
        }

        fillLabels(texts, index, wanted);
    }
}

// The hex labels as one canvas (the user's finding of 2026-11-03: with labels shown, about a thousand SVG texts were laid out and painted at
// every commit). At each commit the labels of the drawn box are painted into a canvas the size of that box on screen, inside the label group,
// so the layer switch and the readable rule hold as before, and a gesture moves its pixels with the rest of the map. On a turned map each
// label is painted upright, as site.css turns the texts. Painted again only when the box, the scale, or the turn changes.
const labelCanvasLimit = 4096;

function drawLabelCanvas(state, layer, index, wanted) {
    let holder = layer.querySelector(":scope > #label-canvas");
    if (wanted.length === 0) {
        holder?.remove();
        return;
    }

    const size = paneSize(state);
    if (!size.width || !size.height) {
        return;
    }

    const box = drawnBox(state);
    const ratio = globalThis.devicePixelRatio || 1;
    const scale = Math.min(size.width / state.box.width, size.height / state.box.height) * ratio;
    const fit = Math.min(1, labelCanvasLimit / (box.width * scale), labelCanvasLimit / (box.height * scale));
    const pixels = scale * fit;
    const width = Math.max(1, Math.round(box.width * pixels));
    const height = Math.max(1, Math.round(box.height * pixels));
    const key = `${box.x} ${box.y} ${box.width} ${box.height} ${width} ${height} ${state.rotated} ${wanted.length}`;
    if (!holder) {
        holder = document.createElementNS(svgNamespace, "foreignObject");
        holder.setAttribute("id", "label-canvas");
        holder.setAttribute("pointer-events", "none");
        holder.appendChild(document.createElement("canvas"));
        layer.appendChild(holder);
    }

    if (holder.getAttribute("data-key") === key) {
        return;
    }

    holder.setAttribute("data-key", key);
    for (const [name, value] of [["x", box.x], ["y", box.y], ["width", box.width], ["height", box.height]]) {
        holder.setAttribute(name, String(value));
    }

    // Painted off the page and handed over as a bitmap: setting the font of a canvas in the page makes the browser work out the page's style
    // there and then (the user's run of 2026-11-03: forced style and layout rose from 38 to 120 ms in a deep zoom). Without OffscreenCanvas,
    // the canvas in the page is painted as before.
    const shown = holder.firstChild;
    const offscreen = typeof OffscreenCanvas === "function" ? new OffscreenCanvas(width, height) : null;
    const canvas = offscreen ?? shown;
    if (!offscreen) {
        canvas.width = width;
        canvas.height = height;
    }

    const context = canvas.getContext("2d");
    if (!context) {
        return;
    }

    const font = index.fontSize ?? 9;
    context.setTransform(pixels, 0, 0, pixels, -box.x * pixels, -box.y * pixels);
    context.font = `${font}px Arial, sans-serif`;
    context.fillStyle = "rgba(0, 0, 0, 0.75)";
    context.textAlign = "center";
    for (const i of wanted) {
        if (state.rotated) {
            // Upright on a turned map: about the label's centre, a quarter turn back, as site.css turns each text.
            const centreY = index.y[i] - font * 0.35;
            context.save();
            context.translate(index.x[i], centreY);
            context.rotate(-Math.PI / 2);
            context.fillText(index.names[i], 0, font * 0.35);
            context.restore();
        } else {
            context.fillText(index.names[i], index.x[i], index.y[i]);
        }
    }

    if (offscreen) {
        // The bitmap renderer shows the painted bitmap without copying it; the canvas takes the bitmap's size.
        (shown.getContext("bitmaprenderer") ?? null)?.transferFromImageBitmap(offscreen.transferToImageBitmap());
    }
}

// The part of the board on screen. The drawing fits the view box with xMidYMid meet, so a pane of another shape shows more than the box,
// centred, on its long side: labels and tiles chosen from the box alone left those strips bare (the user's screenshot, 2026-11-02).
export function visibleBox(box, size) {
    if (!size?.width || !size?.height) {
        return box;
    }

    const scale = Math.min(size.width / box.width, size.height / box.height);
    const width = size.width / scale;
    const height = size.height / scale;
    return { x: box.x - (width - box.width) / 2, y: box.y - (height - box.height) / 2, width, height };
}

// Pan by Transform Plan: the drawing is painted larger than the pane by this share of the pane on every side (decision P1), so a gesture
// moves painted map; site.css sizes .board-host.pan > .board-svg to match (1 + 2 x 0.5 = twice the pane).
const panMargin = 0.5;

// The box a commit paints: the part of the board on screen, widened by the margin on every side.
export function committedBox(visible, margin = panMargin) {
    return {
        x: visible.x - visible.width * margin, y: visible.y - visible.height * margin,
        width: visible.width * (1 + 2 * margin), height: visible.height * (1 + 2 * margin),
    };
}

// The CSS transform that shows the committed drawing as the board now on screen: the drawing's own pixels (origin 0 0) scaled by k and
// moved by (x, y), in a drawing that sits the margin's share of the pane up and left of the pane's corner.
export function gestureTransform(committed, visible, size, margin = panMargin) {
    const painted = committed.width / (1 + 2 * margin);
    const scaleAtCommit = size.width / painted;
    const scaleNow = size.width / visible.width;
    return {
        k: scaleNow / scaleAtCommit,
        x: margin * size.width + (committed.x - visible.x) * scaleNow,
        y: margin * size.height + (committed.y - visible.y) * scaleNow,
    };
}

// Whether a gesture must repaint: the board on screen within a tenth of the pane of the painted edge, or zoomed in past twice the painted
// scale, where the drawing would blur (decision P3).
export function needsCommit(committed, visible, margin = panMargin) {
    const edgeX = visible.width * 0.1;
    const edgeY = visible.height * 0.1;
    const painted = committed.width / (1 + 2 * margin);
    return visible.x - edgeX < committed.x || visible.y - edgeY < committed.y
        || visible.x + visible.width + edgeX > committed.x + committed.width
        || visible.y + visible.height + edgeY > committed.y + committed.height
        || painted / visible.width > 2;
}

// The drawing's transform during a gesture: site.css's at-rest transform (the turn and the margin, for a turned map), then the move.
export function gestureCss(rotated, move) {
    const gesture = `translate(${move.x}px, ${move.y}px) scale(${move.k})`;
    return rotated ? `translateX(100cqw) rotate(90deg) translate(-50cqh, -50cqw) ${gesture}` : gesture;
}

// ?transform=0 in the address or in the address the page was opened with, as ?tiles=0.
export function transformOffInAddress(addresses = [globalThis.location?.href, globalThis.performance?.getEntriesByType?.("navigation")?.[0]?.name]) {
    return addresses.some(address => {
        try {
            return address ? new URL(address).searchParams.get("transform") === "0" : false;
        } catch {
            return false;
        }
    });
}

function panOn(state) {
    return state.pan && state.comparison.mode !== "side";
}

// The part of the board drawn now: with pan by transform the committed box (the margin included), otherwise the part on screen.
function drawnBox(state) {
    return panOn(state) && state.committed ? state.committed : visibleBox(state.box, paneSize(state));
}

// Reuses the group's text elements: each keeps the index it shows, and is changed only when that index changes.
function fillLabels(texts, index, wanted) {
    const nodes = texts.children;
    for (let slot = 0; slot < wanted.length; slot++) {
        const i = wanted[slot];
        let node = nodes[slot];
        if (!node) {
            node = document.createElementNS(svgNamespace, "text");
            texts.appendChild(node);
        }

        if (node.dataset?.label !== String(i)) {
            node.setAttribute("x", index.x[i]);
            node.setAttribute("y", index.y[i]);
            node.textContent = index.names[i];
            node.setAttribute("data-label", String(i));
        }
    }

    while (nodes.length > wanted.length) {
        texts.lastElementChild.remove();
    }
}

async function load(state, baseUrl, query, viewBox, layers, visible) {
    const generation = ++state.generation;
    state.loadMetrics = new URLSearchParams(globalThis.location?.search ?? "").get("mapMetrics") === "1"
        ? { generation, started: performance.now(), reported: false } : null;
    if (state.loadMetrics) state.host.dataset.mapMetricStatus = "loading";
    const tiled = tilesWanted(state, baseUrl, layers);
    const { groups, labelIndex, tileIndex } = await fetchGroups(baseUrl, query, layers, tiled);
    if (generation !== state.generation) {
        return;
    }

    state.labelIndex = labelIndex;
    const tileLayer = groups.find(group => group.id === "layer-exact-tiles");
    state.tiles = tileLayer && tileIndex
        ? { index: tileIndex, baseUrl, query, group: tileLayer, pool: new Map(), variant: null, level: null, sharp: false }
        : null;
    if (state.loadMetrics) {
        probeStates.add(state);
        globalThis.mapProbe = options => runProbe(probeTarget(), options);
    }
    const [x, y, width, height] = viewBox.split(" ").map(Number);
    const home = { x, y, width, height };
    const viewKey = state.host.dataset.viewKey ? state.host.dataset.viewKey + "/" + baseUrl : null;
    const keepBox = state.baseUrl === baseUrl && state.home && state.home.x === x && state.home.y === y && state.home.width === width && state.home.height === height;
    state.baseUrl = baseUrl;
    state.viewKey = viewKey;
    state.home = home;
    // Suppress unreadable text before insertion, avoiding overview layout of every label.
    for (const group of groups) {
        if (group.id === "layer-labels") group.setAttribute("data-detail-visible", "false");
    }
    state.svg.replaceChildren(...groups);
    state.highlight = null;
    state.overlay = null;
    setVisible(state, visible);
    const kept = keepBox ? null : readView(viewKey, home);
    state.restoredView = Boolean(kept);
    if (kept) {
        setBox(state, kept.fitted ? fitBox(state) : kept.box, kept.fitted);
        return;
    }

    setBox(state, keepBox ? state.box : fitBox(state), keepBox ? state.fitted : true);
}

// Replaces the named layers in place, keeping the view: after an edit, for layers the patch does not cover.
async function reloadLayers(state, baseUrl, query, layers) {
    const generation = state.generation;
    // The tiles follow the board version in their URL; a reload of the vector terrain layers they replace has nothing to replace.
    const { groups, labelIndex } = await fetchGroups(baseUrl, query, state.tiles ? layers.filter(layer => !tileLayers.includes(layer)) : layers);
    if (generation !== state.generation) {
        return;
    }

    if (labelIndex) {
        state.labelIndex = labelIndex;
    }

    for (const group of groups) {
        const existing = state.svg.querySelector(`#${CSS.escape(group.id)}`);
        if (existing) {
            group.setAttribute("data-layer-hidden", existing.getAttribute("data-layer-hidden") ?? "false");
            existing.replaceWith(group);
        }
    }

    applyComparison(state);
    updateLabelDetail(state);
    updateLabels(state);
}

// Applies a render patch (section 5.4): per-feature upserts in paint order and removals, by element id.
function applyPatch(state, patch) {
    const parser = new DOMParser();
    for (const removal of patch.removals) {
        state.svg.querySelector(`#${CSS.escape(removal.id)}`)?.remove();
    }

    for (const upsert of patch.upserts) {
        const layer = state.svg.querySelector(`#layer-${CSS.escape(upsert.layer)}`);
        if (!layer) {
            continue;
        }

        const node = document.importNode(parser.parseFromString(upsert.svg, "image/svg+xml").documentElement.firstElementChild, true);
        const existing = layer.querySelector(`#${CSS.escape(upsert.id)}`);
        if (existing) {
            existing.replaceWith(node);
        } else {
            const before = upsert.before ? layer.querySelector(`#${CSS.escape(upsert.before)}`) : null;
            layer.insertBefore(node, before);
        }
    }

    applyComparison(state);
}

function setVisible(state, visible) {
    for (const group of state.svg.children) {
        const layer = group.id.startsWith("layer-") ? group.id.substring(6) : null;
        if (layer && layer !== "defs" && layer !== "units") {
            group.setAttribute("data-layer-hidden", String(!visible.includes(layer)));
        }
    }

    setTileVariant(state, visible);
    applyComparison(state);
    updateLabels(state);
}

function setTool(state, tool) {
    state.tool = tool;
    state.points = [];
    clearOverlay(state);
    state.svg.classList.toggle("drawing", tool === "polygon" || tool === "polyline");
}

function finishGesture(state) {
    if (state.points.length === 0) {
        return;
    }

    const points = state.points.map(point => [point.x, point.y]);
    state.points = [];
    clearOverlay(state);
    state.dotnet.invokeMethodAsync("OnBoardGesture", points);
}

function overlay(state) {
    if (!state.overlay || !state.overlay.isConnected) {
        state.overlay = document.createElementNS(svgNamespace, "g");
        state.overlay.setAttribute("class", "board-overlay");
        state.svg.appendChild(state.overlay);
    }

    return state.overlay;
}

function clearOverlay(state) {
    state.overlay?.replaceChildren();
}

function drawGesture(state, pointer) {
    if (state.points.length === 0) {
        return;
    }

    drawLine(state, pointer ? [...state.points, pointer] : state.points, state.tool === "polygon");
}

function drawLine(state, points, closed) {
    const group = overlay(state);
    const line = document.createElementNS(svgNamespace, closed ? "polygon" : "polyline");
    line.setAttribute("points", points.map(point => `${point.x},${point.y}`).join(" "));
    line.setAttribute("class", "gesture");
    group.replaceChildren(line);
    for (const point of state.points) {
        const dot = document.createElementNS(svgNamespace, "circle");
        dot.setAttribute("cx", point.x);
        dot.setAttribute("cy", point.y);
        dot.setAttribute("r", 2);
        dot.setAttribute("class", "gesture-point");
        group.appendChild(dot);
    }
}

// Comparison presentation (section 3.7): overlay with Styled opacity, a swipe divider, or side by side, switched in the
// browser without re-rendering.
function setComparison(state, mode, value) {
    state.comparison = { mode, value };
    applyComparison(state);
}

function styledGroups(svg) {
    return [...svg.children].filter(group => group.id.startsWith("layer-styled-"));
}

function applyComparison(state) {
    const { mode, value } = state.comparison;
    const groups = styledGroups(state.svg);
    state.svg.setAttribute("data-comparison-mode", mode);
    // The UI slider uses whole percentages. All corresponding opacity rules live in site.css.
    state.svg.setAttribute("data-comparison-opacity", Math.round(clamp(value, 0, 1) * 100));
    state.svg.querySelector("#comparison-clip")?.remove();
    for (const group of groups) {
        group.removeAttribute("clip-path");
    }

    state.host.classList.toggle("side-by-side", mode === "side");
    // Side by side lays two drawings in a row, so pan by transform rests while it shows (Pan by Transform Plan, deviation E2).
    if (state.pan && state.panShown !== panOn(state)) {
        state.panShown = panOn(state);
        state.host.classList.toggle("pan", state.panShown);
        if (state.box) {
            setBox(state, state.box, state.fitted);
        }
    }

    if (mode !== "side" && state.side) {
        state.side.remove();
        state.side = null;
    }

    if (mode === "swipe" && state.home) {
        const clipPath = document.createElementNS(svgNamespace, "clipPath");
        clipPath.id = "comparison-clip";
        const rect = document.createElementNS(svgNamespace, "rect");
        rect.setAttribute("x", state.home.x);
        rect.setAttribute("y", state.home.y);
        rect.setAttribute("width", state.home.width * value);
        rect.setAttribute("height", state.home.height);
        clipPath.appendChild(rect);
        state.svg.insertBefore(clipPath, state.svg.firstChild);
        for (const group of groups) {
            group.setAttribute("clip-path", "url(#comparison-clip)");
        }
    } else if (mode === "side" && groups.length > 0) {
        if (!state.side) {
            state.side = document.createElementNS(svgNamespace, "svg");
            state.side.setAttribute("class", "board-svg board-side");
            state.host.appendChild(state.side);
        }

        const keep = id => id === "layer-defs" || id === "layer-grid" || id === "layer-labels";
        const shared = [...state.svg.children].filter(group => keep(group.id)).map(group => group.cloneNode(true));
        state.side.replaceChildren(...shared.slice(0, 1), ...groups.map(group => group.cloneNode(true)), ...shared.slice(1));
        if (state.box) {
            state.side.setAttribute("viewBox", `${state.box.x} ${state.box.y} ${state.box.width} ${state.box.height}`);
        }

        updateLabels(state);
    }
}

function focusPoint(state, x, y) {
    if (!state.home) {
        return;
    }

    const width = state.home.width / 6;
    const height = state.home.height / 6;
    setBox(state, { x: x - width / 2, y: y - height / 2, width, height });
}

function zoomAt(state, point, factor, gesture = false) {
    const width = clamp(state.box.width * factor, state.home.width / 40, state.home.width * 2);
    const scale = width / state.box.width;
    state.lastZoom = performance.now();
    setBox(state, {
        x: point.x - (point.x - state.box.x) * scale,
        y: point.y - (point.y - state.box.y) * scale,
        width,
        height: state.box.height * scale,
    }, false, gesture);
}

// Brings a point into view at the same zoom, only when it lies outside the view or close to its edge (Claude Design's review: a hex picked
// outside the map, such as from the units table, may be off screen on a zoomed or composed map).
function revealPoint(state, x, y) {
    if (!state.box) {
        return;
    }

    const box = state.box;
    const marginX = box.width * 0.1;
    const marginY = box.height * 0.1;
    if (x >= box.x + marginX && x <= box.x + box.width - marginX && y >= box.y + marginY && y <= box.y + box.height - marginY) {
        return;
    }

    setBox(state, { ...box, x: x - box.width / 2, y: y - box.height / 2 });
}

// Shows an area of the board whole, at the pane's own shape (pass 30: the setup areas of the side setting up). A turned map keeps its fit.
function fitRect(state, x, y, width, height) {
    if (!state.home || state.rotated) {
        return;
    }

    const { width: paneWidth, height: paneHeight } = paneSize(state);
    const aspect = paneWidth > 0 && paneHeight > 0 ? paneWidth / paneHeight : state.home.width / state.home.height;
    const boxWidth = Math.max(width, height * aspect);
    const boxHeight = boxWidth / aspect;

    // An area as wide as the board, in a pane of another shape, would show the board smaller than its fit: the fit is kept then.
    if (boxWidth >= state.home.width || boxHeight >= state.home.height) {
        reset(state);
        return;
    }

    setBox(state, { x: x + width / 2 - boxWidth / 2, y: y + height / 2 - boxHeight / 2, width: boxWidth, height: boxHeight });
}

function zoomCenter(state, factor) {
    if (state.box) {
        zoomAt(state, { x: state.box.x + state.box.width / 2, y: state.box.y + state.box.height / 2 }, factor);
    }
}

function reset(state) {
    if (state.home) {
        setBox(state, fitBox(state), true);
    }
}

// The whole board, or, turned in a tall pane (pass 29), the board's turned width across the pane's width from its turned top, to pan along its
// length; a turned board that fits whole, or one in a wide pane, is shown whole.
function fitBox(state) {
    const home = { ...state.home };
    const { width, height } = paneSize(state);
    if (!state.rotated || width === 0 || height === 0 || paneShape(state) !== "portrait") {
        return home;
    }

    const span = home.height * width / height;
    return span >= home.width ? home : { x: home.x, y: home.y, width: span, height: home.height };
}

// The LOS layer (LOS Design, section 6): a line and the blocking hex, drawn above the units.
function setLos(state, markup) {
    state.svg.querySelector("#layer-los")?.remove();
    if (!markup) {
        return;
    }

    const fragment = new DOMParser().parseFromString(
        `<svg xmlns="${svgNamespace}">${markup}</svg>`, "image/svg+xml");
    const group = fragment.documentElement.firstElementChild;
    if (fragment.querySelector("parsererror") || group?.id !== "layer-los") {
        throw new Error("Invalid LOS layer");
    }
    state.svg.appendChild(document.importNode(group, true));
}

// Picked hexes (pass 28, the card editor's map): outlines drawn on top, kept apart from the LOS layer.
function setMarks(state, markup) {
    state.svg.querySelector("#layer-marks")?.remove();
    if (!markup) {
        return;
    }

    const fragment = new DOMParser().parseFromString(
        `<svg xmlns="${svgNamespace}">${markup}</svg>`, "image/svg+xml");
    const group = fragment.documentElement.firstElementChild;
    if (fragment.querySelector("parsererror") || group?.id !== "layer-marks") {
        throw new Error("Invalid marks layer");
    }
    state.svg.appendChild(document.importNode(group, true));
    cullToView(state);
}

function setUnits(state, markup, version) {
    state.svg.querySelector("#layer-units")?.remove();
    if (!markup) {
        return;
    }

    const fragment = new DOMParser().parseFromString(
        `<svg xmlns="${svgNamespace}">${markup}</svg>`, "image/svg+xml");
    if (fragment.querySelector("parsererror")) {
        throw new Error("Invalid unit SVG");
    }
    const group = fragment.documentElement.firstElementChild;
    if (group?.id !== "layer-units") {
        throw new Error("Invalid unit layer");
    }
    // The version a patch must be made from (the Counter Layer Design, section 6): the server's record of what this layer holds.
    if (version !== undefined && version !== null) {
        group.setAttribute("data-units-version", String(version));
    }
    state.svg.appendChild(document.importNode(group, true));
    if (state.highlight?.isConnected) {
        state.svg.appendChild(state.highlight);
    }

    updateUnitTier(state);
    cullToView(state);
    reportMeasuredScene(state);
}

// Option D (the Counter Layer Design, section 6): applies the changes between the layer this page holds and the server's new one. Refused,
// changing nothing, when the layer is not the version the patch was made from; the server then sends the layer whole.
function applyUnitPatch(state, patch) {
    const layer = state.svg.querySelector("#layer-units");
    if (!layer || layer.getAttribute("data-units-version") !== String(patch.base)) {
        return false;
    }

    let parsed = [];
    if (patch.upserts.length > 0) {
        // One parse for every upsert of the patch.
        const upserted = new DOMParser().parseFromString(
            `<svg xmlns="${svgNamespace}">${patch.upserts.map(upsert => upsert.markup).join("")}</svg>`, "image/svg+xml");
        if (upserted.querySelector("parsererror")) {
            return false;
        }
        parsed = [...upserted.documentElement.children];
        if (parsed.length !== patch.upserts.length || parsed.some((node, index) => node.id !== patch.upserts[index].id)) {
            return false;
        }
    }

    // Keyboard focus is the one state of the browser inside a fragment: kept by unit id across the replacement (section 10).
    const focused = layer.contains(globalThis.document.activeElement)
        ? globalThis.document.activeElement.closest("[data-unit-id]")?.getAttribute("data-unit-id") : null;
    for (const removal of patch.removals) {
        layer.querySelector(`#${CSS.escape(removal)}`)?.remove();
    }

    patch.upserts.forEach((upsert, index) => {
        const node = globalThis.document.importNode(parsed[index], true);
        const existing = layer.querySelector(`:scope > #${CSS.escape(upsert.id)}`);
        if (existing) {
            existing.replaceWith(node);
            return;
        }

        const before = upsert.before ? layer.querySelector(`:scope > #${CSS.escape(upsert.before)}`) : null;
        layer.insertBefore(node, before);
    });

    layer.setAttribute("data-face-size", patch.faceSize);
    layer.setAttribute("data-unit-set", patch.unitSet);
    layer.setAttribute("data-units-version", String(patch.version));
    if (focused && !layer.contains(globalThis.document.activeElement)) {
        layer.querySelector(`[data-unit-id="${CSS.escape(focused)}"]`)?.focus({ preventScroll: true });
    }

    if (state.highlight?.isConnected) {
        state.svg.appendChild(state.highlight);
    }

    updateUnitTier(state);
    cullToView(state);
    return true;
}

// Option C2 (the Counter Layer Design, section 4; ruling U1): the figure outlines every counter layer refers to, held beside the board's layers
// from a board load on. Its id has no "layer-" prefix, so the layer choice and the comparison modes leave it alone.
function setUnitFigures(state, markup) {
    state.svg.querySelector("#unit-figures")?.remove();
    if (!markup) {
        return;
    }

    const fragment = new DOMParser().parseFromString(`<svg xmlns="${svgNamespace}">${markup}</svg>`, "image/svg+xml");
    const group = fragment.documentElement.firstElementChild;
    if (fragment.querySelector("parsererror") || group?.id !== "unit-figures") {
        throw new Error("Invalid unit figures");
    }
    state.svg.insertBefore(document.importNode(group, true), state.svg.firstChild);
}

// Optional development diagnostics. The scene includes terrain and counters; two animation frames
// allow a paint opportunity. This is not a GPU completion or continuous-interactivity measurement.
function reportMeasuredScene(state) {
    const metric = state.loadMetrics;
    if (!metric || metric.reported) return;
    metric.reported = true;
    requestAnimationFrame(() => requestAnimationFrame(() => {
        if (metric.generation !== state.generation) return;
        const now = performance.now();
        state.host.dataset.mapSceneLoadMs = (now - metric.started).toFixed(2);
        state.host.dataset.mapNavigationToSceneMs = now.toFixed(2);
        state.host.dataset.mapMetricStatus = "ready";
    }));
}

// The pan probe (Large Campaign Map Plan, task 1): with ?mapMetrics=1, mapProbe() in the console drags the map for a second and sends a burst
// of wheel events, both through the real handlers, times a label layer switch, and reports frames, the longest frame, and the browser's
// Long Animation Frames. It restores the view and the layer choice it found. The record is logged and kept on the host as data-map-probe.
// The maps a page has loaded with metrics on; the probe measures the largest one still on the page (a hand-over or hidden map may have loaded
// last).
const probeStates = new Set();

function probeTarget() {
    let best = null;
    let bestArea = 0;
    for (const state of probeStates) {
        if (!state.svg.isConnected) {
            probeStates.delete(state);
            continue;
        }

        const rect = state.svg.getBoundingClientRect();
        if (rect.width * rect.height > bestArea) {
            best = state;
            bestArea = rect.width * rect.height;
        }
    }

    if (!best) throw new Error("No map is on the page.");
    return best;
}

// The time from `since` until every tile the map keeps in view shows its own image, checked each frame; null when the map draws no tiles,
// -1 when it is not sharp within the wait (ten seconds by default).
async function untilSharp(state, since, waitMs = 10000) {
    if (!state.tiles) {
        return null;
    }

    const frame = () => new Promise(resolve => requestAnimationFrame(resolve));
    for (;;) {
        const now = await frame();
        if (tilesSharp(state.tiles)) {
            return Math.round(now - since);
        }

        if (now - since > waitMs) {
            return -1;
        }
    }
}

// Whether every tile image in the pool has its file: a tile still loading has no href (tileImage).
export function tilesSharp(tiles) {
    for (const image of tiles.pool.values()) {
        if (!image.hasAttribute("href")) {
            return false;
        }
    }

    return true;
}

export function summarizeFrames(times) {
    const gaps = times.slice(1).map((time, index) => time - times[index]);
    const span = times.length > 1 ? times[times.length - 1] - times[0] : 0;
    return {
        frames: gaps.length,
        spanMs: Math.round(span),
        framesPerSecond: span > 0 ? Math.round(gaps.length * 10000 / span) / 10 : 0,
        longestFrameMs: Math.round(Math.max(0, ...gaps)),
        medianFrameMs: gaps.length ? Math.round([...gaps].sort((a, b) => a - b)[Math.floor(gaps.length / 2)]) : 0,
    };
}

// The layers left shown when a probe hides some (Raster Tiles Plan, task 1: how much of a frame each layer takes).
export function layersAfterHide(shown, hide) {
    return shown.filter(layer => !hide.includes(layer));
}

function shownLayers(state) {
    return [...state.svg.children].filter(group => group.id.startsWith("layer-") && group.getAttribute("data-layer-hidden") === "false")
        .map(group => group.id.substring(6));
}

async function runProbe(state, options = {}) {
    if (!state.box || !state.home) throw new Error("No board is loaded.");
    const frame = () => new Promise(resolve => requestAnimationFrame(resolve));
    const shownBefore = shownLayers(state);
    const hide = options.hide ?? [];
    if (hide.length) {
        setVisible(state, layersAfterHide(shownBefore, hide));
        await frame();
        await frame();
    }

    // Pan by Transform Plan, task 1: a run at a set zoom (steps of the + key from Fit), so runs at Fit and at playing zoom repeat exactly.
    const viewBefore = { box: { ...state.box }, fitted: state.fitted };
    if (options.zoom !== undefined) {
        reset(state);
        for (let step = 0; step < options.zoom; step++) {
            zoomCenter(state, 0.8);
        }

        await frame();
        await new Promise(resolve => setTimeout(resolve, options.settleWaitMs ?? 1500));
    }

    // The map's container, not the drawing: with pan by transform the drawing is larger than the pane.
    const rect = state.host.getBoundingClientRect();
    const cx = rect.left + rect.width / 2;
    const cy = rect.top + rect.height / 2;
    const saved = { box: { ...state.box }, fitted: state.fitted };
    const animationFrames = [];
    const observer = typeof PerformanceObserver !== "undefined"
        && PerformanceObserver.supportedEntryTypes?.includes("long-animation-frame")
        ? new PerformanceObserver(list => animationFrames.push(...list.getEntries())) : null;
    observer?.observe({ type: "long-animation-frame", buffered: false });
    const pointer = (type, x, y) => state.svg.dispatchEvent(new PointerEvent(type, {
        bubbles: true, cancelable: true, pointerId: 91, pointerType: "mouse", button: 0, buttons: type === "pointerup" ? 0 : 1, clientX: x, clientY: y,
    }));
    const loafSince = start => {
        animationFrames.push(...(observer?.takeRecords() ?? []));
        const entries = animationFrames.filter(entry => entry.startTime >= start);
        const forced = entries.reduce((sum, entry) => sum + (entry.scripts ?? []).reduce((s, script) => s + (script.forcedStyleAndLayoutDuration ?? 0), 0), 0);
        return { longAnimationFrames: entries.length, longestAnimationFrameMs: Math.round(Math.max(0, ...entries.map(entry => entry.duration))),
            forcedStyleAndLayoutMs: Math.round(forced) };
    };

    // A drag of a fixed screen distance: by default a quarter of the pane's short side over a second, inside the painted margin; with
    // `dragPanes` that many pane widths (negative to the left), so the drag commits on the way, as a user's long pan does (Screen-Bounded
    // Map Analysis, section 5). A long drag needs a zoom at which the map is wider than the pane, or it drags past the map's edge.
    const dragMs = options.dragMs ?? (options.dragPanes ? 1000 * Math.max(1, Math.abs(options.dragPanes)) : 1000);
    const distance = options.dragPanes ? rect.width * options.dragPanes : Math.min(rect.width, rect.height) / 4;
    await frame();
    const dragStart = performance.now();
    const dragTimes = [dragStart];
    const commitsBeforeDrag = state.commits ?? 0;
    pointer("pointerdown", cx, cy);
    for (let elapsed = 0; elapsed < dragMs; elapsed = performance.now() - dragStart) {
        pointer("pointermove", cx + distance * elapsed / dragMs, cy);
        dragTimes.push(await frame());
    }
    pointer("pointerup", cx + distance, cy);
    // The settling after the drag: from pointer up to the second frame after it, the first painted after any repaint the drag left.
    const upAt = performance.now();
    dragTimes.push(await frame());
    const settledAt = await frame();
    const dragCommits = (state.commits ?? 0) - commitsBeforeDrag;
    const dragSharpMs = await untilSharp(state, upAt, options.sharpWaitMs);
    await new Promise(resolve => setTimeout(resolve, 50));
    const drag = { ...summarizeFrames(dragTimes), settleMs: Math.round(settledAt - upAt), commits: dragCommits, sharpMs: dragSharpMs, ...loafSince(dragStart) };

    // A burst of wheel events in one task, as a trackpad sends them, then the frames until the map settles. By default it alternates in
    // and out; `wheel: "in"` or `"out"` zooms one way, so it passes the painted scale and commits as a real zoom does.
    setBox(state, saved.box, saved.fitted);
    await frame();
    const wheelStart = performance.now();
    const commitsBeforeWheel = state.commits ?? 0;
    for (let index = 0; index < (options.wheelEvents ?? 20); index++) {
        const deltaY = options.wheel === "in" ? -100 : options.wheel === "out" ? 100 : index % 2 ? 100 : -100;
        state.svg.dispatchEvent(new WheelEvent("wheel", { bubbles: true, cancelable: true, deltaY, clientX: cx, clientY: cy }));
    }
    const wheelTimes = [wheelStart, await frame(), await frame()];
    // The burst rests after `gestureRest`; the commit and the tiles follow it.
    await new Promise(resolve => setTimeout(resolve, gestureRest + 20));
    await frame();
    const wheelSharpMs = await untilSharp(state, wheelStart, options.sharpWaitMs);
    // Long Animation Frame entries are delivered after their frame: one more frame and a short wait before they are read.
    await frame();
    await new Promise(resolve => setTimeout(resolve, 50));
    const wheel = { settleMs: Math.round(wheelTimes[2] - wheelStart), commits: (state.commits ?? 0) - commitsBeforeWheel, sharpMs: wheelSharpMs,
        ...loafSince(wheelStart) };

    // The label layer switch: off and on again, as the page's layer choice does it.
    setBox(state, saved.box, saved.fitted);
    const shown = shownLayers(state);
    let labelSwitchMs = null;
    if (shown.includes("labels")) {
        await frame();
        const switchStart = performance.now();
        setVisible(state, shown.filter(layer => layer !== "labels"));
        await frame();
        setVisible(state, shown);
        await frame();
        labelSwitchMs = Math.round(performance.now() - switchStart);
    }

    observer?.disconnect();
    setBox(state, saved.box, saved.fitted);
    if (hide.length) {
        setVisible(state, shownBefore);
    }

    if (options.zoom !== undefined) {
        setBox(state, viewBefore.box, viewBefore.fitted);
    }

    const record = {
        when: new Date().toISOString(), board: state.baseUrl, zoom: options.zoom ?? null, hidden: hide, elements: state.svg.getElementsByTagName("*").length,
        tiles: state.tiles ? { level: state.tiles.level, shown: state.tiles.pool.size } : null,
        counters: state.svg.querySelectorAll("#layer-units [data-unit-id]").length, viewport: [Math.round(rect.width), Math.round(rect.height)],
        boxWidth: Math.round(saved.box.width), drag, wheel, labelSwitchMs, userAgent: globalThis.navigator?.userAgent,
    };
    state.host.dataset.mapProbe = JSON.stringify(record);
    console.info("map probe", record);
    return record;
}

// Unit detail tiers (Unit Display Design, section 7.1): the overlay carries every tier, and zoom picks one from the
// face size on screen, so zooming never asks the server again.
export function unitTier(faceSize, clientWidth, boxWidth) {
    const onScreen = clientWidth > 0 && boxWidth > 0 ? faceSize * clientWidth / boxWidth : faceSize;
    return onScreen < 16 ? "far" : onScreen <= 40 ? "mid" : "near";
}

function updateUnitTier(state) {
    const layer = state.svg.querySelector("#layer-units");
    if (!layer || !state.box) {
        return;
    }

    const tier = unitTier(Number(layer.getAttribute("data-face-size")) || 0, paneSize(state).width, state.box.width);
    if (layer.getAttribute("data-active-tier") !== tier) {
        layer.setAttribute("data-active-tier", tier);
    }
}

// Labels below six screen pixels cannot be read. Keep the explicit layer choice separate
// from this zoom-dependent detail, so zooming never re-enables a layer the user hid.
function updateLabelDetail(state) {
    const labels = state.svg.querySelector("#layer-labels");
    if (!labels || !state.box) return;
    const fontSize = Number(labels.firstElementChild?.getAttribute("font-size")) || 9;
    const readable = String(fontSize * paneSize(state).width / state.box.width >= 6);
    // Written only on change (Large Campaign Map Plan, A3): rewriting it invalidates the style of every label beneath.
    if (labels.getAttribute("data-detail-visible") !== readable) {
        labels.setAttribute("data-detail-visible", readable);
    }
}

function highlight(state, points) {
    if (!state.highlight) {
        state.highlight = document.createElementNS(svgNamespace, "polygon");
        state.highlight.setAttribute("class", "board-highlight");
    }

    if (!points) {
        state.highlight.remove();
        return;
    }

    state.highlight.setAttribute("points", points);
    state.svg.appendChild(state.highlight);
}

// Large Campaign Map Plan, A1: the box changes at once, so every caller (and a burst of wheel events) works from the newest box, while the
// drawing is written once per animation frame. A gesture's view is saved when it ends (A4); any other change is saved at once.
function setBox(state, box, fitted = false, gesture = false) {
    state.box = box;
    state.fitted = fitted;
    if (!gesture) {
        rememberView(state);
    }

    if (typeof requestAnimationFrame !== "function") {
        flushBox(state);
        return;
    }

    if (!state.flushPending) {
        state.flushPending = true;
        requestAnimationFrame(() => flushBox(state));
    }
}

function flushBox(state) {
    state.flushPending = false;
    if (!state.box) {
        return;
    }

    // Taken before any write, while the layout of the last frame still holds, so reading it costs nothing (A2).
    takeRect(state);
    let shown = state.box;
    if (panOn(state)) {
        const size = paneSize(state);
        if (!size.width || !size.height) {
            return;
        }

        const visible = visibleBox(state.box, size);
        // During a gesture the painted drawing is moved, not painted again, until the board on screen nears its painted edge.
        if (state.gesturing && state.committed && !needsCommit(state.committed, visible)) {
            const move = gestureTransform(state.committed, visible, size);
            // The transform itself, not an inherited custom property: a custom property restyles every element of the map each frame, and
            // Chrome repainted the map (Pan by Transform Plan, deviation E3); a change of transform alone only moves the painted layer.
            state.svg.style?.setProperty?.("transform", gestureCss(state.rotated, move));
            return;
        }

        state.committed = committedBox(visible);
        shown = state.committed;
    } else {
        state.committed = null;
    }

    // The probe counts the repaints a gesture causes (Screen-Bounded Map Analysis, section 5, step 1).
    state.commits = (state.commits ?? 0) + 1;

    // The one exception to site.css's rule of no style attributes (as the first trial's, Large Campaign Map Plan task 6): the gesture's
    // transform, cleared at every commit, which hands the drawing back to site.css's rule.
    state.svg.style?.removeProperty?.("transform");
    const value = `${shown.x} ${shown.y} ${shown.width} ${shown.height}`;
    if (state.svg.getAttribute?.("viewBox") !== value) {
        state.svg.setAttribute("viewBox", value);
    }
    state.side?.setAttribute("viewBox", `${state.box.x} ${state.box.y} ${state.box.width} ${state.box.height}`);
    updateUnitTier(state);
    updateLabelDetail(state);
    updateLabels(state);
    updateTiles(state);
    updateNearGrid(state);
    cullToView(state);
}

// A gesture starts: the drawing gets its own compositor layer for the gesture's length (site.css, .board-host.gesturing).
function gestureStarted(state) {
    if (panOn(state) && !state.gesturing) {
        state.gesturing = true;
        state.host.classList.add("gesturing");
    }
}

function gestureEnded(state) {
    clearTimeout(state.wheelRest);
    rememberView(state);
    if (state.gesturing) {
        state.gesturing = false;
        state.host.classList?.remove("gesturing");
        // The rest: one commit paints the view and its margin.
        setBox(state, state.box, state.fitted);
    }
}

// The drawing's size in CSS pixels, before any turn: from the size observer (A2), or read once where there is none.
function paneSize(state) {
    if (panOn(state)) {
        // With pan by transform the drawing is larger than the pane: the pane is the host, before any turn.
        const host = state.hostSize ?? { width: state.host.clientWidth, height: state.host.clientHeight };
        return state.rotated ? { width: host.height, height: host.width } : host;
    }

    return state.size ?? { width: state.svg.clientWidth, height: state.svg.clientHeight };
}

function takeRect(state) {
    const target = panOn(state) ? state.host : state.svg;
    state.rect = typeof target.getBoundingClientRect === "function" ? target.getBoundingClientRect() : null;
}

// From a pointer to board coordinates without the screen matrix, which would lay out the map (A2): the drawing's rectangle on screen, the
// quarter turn when the map is turned (site.css, .board-host.rotated: translateX(100cqw) rotate(90deg) from the top left), and the viewBox's
// default fit (xMidYMid meet). The screen matrix stays the answer when no rectangle or size is known yet.
function toBoard(state, clientX, clientY) {
    const rect = state.rect;
    // With pan by transform the pane is the host, and the view box, never the moved drawing, says what is under the pointer.
    const size = panOn(state) && state.hostSize ? paneSize(state) : state.size;
    const box = state.box;
    if (rect && size && box && size.width > 0 && size.height > 0) {
        const [lx, ly] = state.rotated ? [clientY - rect.top, rect.right - clientX] : [clientX - rect.left, clientY - rect.top];
        const scale = Math.min(size.width / box.width, size.height / box.height);
        const offsetX = (size.width - box.width * scale) / 2;
        const offsetY = (size.height - box.height * scale) / 2;
        return { x: box.x + (lx - offsetX) / scale, y: box.y + (ly - offsetY) / scale };
    }

    const point = state.svg.createSVGPoint();
    point.x = clientX;
    point.y = clientY;
    const board = point.matrixTransform(state.svg.getScreenCTM().inverse());
    return { x: board.x, y: board.y };
}

// Pass 29: the whole map turned a quarter clockwise as one, as if walking to the side of the table. The turn is a CSS transform on the drawing
// (site.css, .board-host.rotated); the board's coordinates do not change, and the pointer is read through the screen matrix, which includes it.
// The browser remembers the choice for each shape of pane: taller than wide, or not.
function paneShape(state) {
    return state.host.clientHeight > state.host.clientWidth ? "portrait" : "landscape";
}

function readRotation(shape) {
    try {
        return localStorage.getItem(`${rotationKey}.${shape}`) === "true";
    } catch {
        return false;
    }
}

function applyShape(state) {
    // A hidden pane (a narrow tab not shown) has no size, so no shape: its choice waits until it shows.
    if (state.host.clientWidth === 0 || state.host.clientHeight === 0) {
        return;
    }

    const shape = paneShape(state);
    if (shape === state.shape) {
        return;
    }

    state.shape = shape;
    const on = readRotation(shape);
    if (on !== state.rotated) {
        setRotation(state, on, false);
        state.dotnet.invokeMethodAsync("OnRotationChanged", state.rotated);
    }
}

function setRotation(state, on, remember) {
    state.rotated = Boolean(on);
    state.host.classList.toggle("rotated", state.rotated);
    if (remember) {
        state.shape = paneShape(state);
        try {
            localStorage.setItem(`${rotationKey}.${state.shape}`, String(state.rotated));
        } catch {
            // Storage is blocked: the turn holds for this page only.
        }
    }

    reset(state);
    updateUnitTier(state);
    return state.rotated;
}

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}
