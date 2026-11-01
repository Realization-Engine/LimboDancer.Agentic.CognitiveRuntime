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
    };

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
            }
        }

        applyShape(state);
        if (state.fitted) {
            reset(state);
        } else {
            updateUnitTier(state);
            updateLabelDetail(state);
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

        // Turned a quarter clockwise (pass 29), the map's x runs down the screen and its y runs to the left.
        const unitsPerPixel = drag.box.width / paneSize(state).width;
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

async function fetchGroups(baseUrl, query, layers) {
    let labelIndex = null;
    const texts = await Promise.all(layers.map(async layer => {
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
    const groups = texts.map((text, index) => text === null
        ? labelGroup(labelIndex)
        : document.importNode(parser.parseFromString(text, "image/svg+xml").documentElement.firstElementChild, true));
    return { groups, labelIndex };
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
    const wanted = shown ? labelsInView(index, state.box) : [];
    for (const svg of [state.svg, state.side]) {
        const texts = svg?.querySelector("#layer-labels")?.firstElementChild;
        if (texts) {
            fillLabels(texts, index, wanted);
        }
    }
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
    const { groups, labelIndex } = await fetchGroups(baseUrl, query, layers);
    if (generation !== state.generation) {
        return;
    }

    state.labelIndex = labelIndex;
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
    const { groups, labelIndex } = await fetchGroups(baseUrl, query, layers);
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

async function runProbe(state, options = {}) {
    if (!state.box || !state.home) throw new Error("No board is loaded.");
    const frame = () => new Promise(resolve => requestAnimationFrame(resolve));
    const rect = state.svg.getBoundingClientRect();
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

    // A drag of a fixed screen distance over about a second: one move per animation frame.
    const dragMs = options.dragMs ?? 1000;
    const distance = Math.min(rect.width, rect.height) / 4;
    await frame();
    const dragStart = performance.now();
    const dragTimes = [dragStart];
    pointer("pointerdown", cx, cy);
    for (let elapsed = 0; elapsed < dragMs; elapsed = performance.now() - dragStart) {
        pointer("pointermove", cx + distance * elapsed / dragMs, cy);
        dragTimes.push(await frame());
    }
    pointer("pointerup", cx + distance, cy);
    dragTimes.push(await frame());
    await new Promise(resolve => setTimeout(resolve, 50));
    const drag = { ...summarizeFrames(dragTimes), ...loafSince(dragStart) };

    // A burst of wheel events in one task, as a trackpad sends them, then the frames until the map settles.
    setBox(state, saved.box, saved.fitted);
    await frame();
    const wheelStart = performance.now();
    for (let index = 0; index < (options.wheelEvents ?? 20); index++) {
        state.svg.dispatchEvent(new WheelEvent("wheel", { bubbles: true, cancelable: true, deltaY: index % 2 ? 100 : -100, clientX: cx, clientY: cy }));
    }
    const wheelTimes = [wheelStart, await frame(), await frame()];
    // Long Animation Frame entries are delivered after their frame: one more frame and a short wait before they are read.
    await frame();
    await new Promise(resolve => setTimeout(resolve, 50));
    const wheel = { settleMs: Math.round(wheelTimes[2] - wheelStart), ...loafSince(wheelStart) };

    // The label layer switch: off and on again, as the page's layer choice does it.
    setBox(state, saved.box, saved.fitted);
    const shown = [...state.svg.children].filter(group => group.id.startsWith("layer-") && group.getAttribute("data-layer-hidden") === "false")
        .map(group => group.id.substring(6));
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
    const record = {
        when: new Date().toISOString(), board: state.baseUrl, elements: state.svg.getElementsByTagName("*").length,
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
    const value = `${state.box.x} ${state.box.y} ${state.box.width} ${state.box.height}`;
    if (state.svg.getAttribute?.("viewBox") !== value) {
        state.svg.setAttribute("viewBox", value);
    }
    state.side?.setAttribute("viewBox", value);
    updateUnitTier(state);
    updateLabelDetail(state);
    updateLabels(state);
}

function gestureEnded(state) {
    clearTimeout(state.wheelRest);
    rememberView(state);
}

// The drawing's size in CSS pixels, before any turn: from the size observer (A2), or read once where there is none.
function paneSize(state) {
    return state.size ?? { width: state.svg.clientWidth, height: state.svg.clientHeight };
}

function takeRect(state) {
    state.rect = typeof state.svg.getBoundingClientRect === "function" ? state.svg.getBoundingClientRect() : null;
}

// From a pointer to board coordinates without the screen matrix, which would lay out the map (A2): the drawing's rectangle on screen, the
// quarter turn when the map is turned (site.css, .board-host.rotated: translateX(100cqw) rotate(90deg) from the top left), and the viewBox's
// default fit (xMidYMid meet). The screen matrix stays the answer when no rectangle or size is known yet.
function toBoard(state, clientX, clientY) {
    const rect = state.rect;
    const size = state.size;
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
