// Board viewport (Architecture and Rendering Design, sections 5.3 and 6). Layer fragments are fetched as SVG and imported
// into inline <svg> elements; pan and zoom only change the viewBox, so the browser never rasterizes anything we send.
// Editing gestures are collected here and sent to .NET complete; the server snaps coordinates and decides geometry.

const svgNamespace = "http://www.w3.org/2000/svg";
const hoverInterval = 100;

export function create(host, dotnet) {
    const svg = document.createElementNS(svgNamespace, "svg");
    svg.setAttribute("class", "board-svg");
    host.replaceChildren(svg);
    host.tabIndex = 0;

    const state = {
        host, svg, dotnet, side: null, box: null, home: null, drag: null, generation: 0, highlight: null,
        tool: "select", points: [], overlay: null, lastHover: 0, comparison: { mode: "none", value: 0.5 },
    };

    svg.addEventListener("wheel", event => {
        if (!state.box) {
            return;
        }

        event.preventDefault();
        zoomAt(state, toBoard(state, event.clientX, event.clientY), event.deltaY < 0 ? 0.8 : 1.25);
    }, { passive: false });

    svg.addEventListener("pointerdown", event => {
        if (!state.box || event.button !== 0) {
            return;
        }

        host.focus({ preventScroll: true });

        // Pointer capture retargets pointerup to the SVG root. Remember the actual counter hit here.
        const placementId = event.target.closest?.("[data-placement-id]")?.getAttribute("data-placement-id");
        state.drag = {
            id: event.pointerId, x: event.clientX, y: event.clientY, box: { ...state.box }, moved: false, placementId,
            start: toBoard(state, event.clientX, event.clientY),
        };
        svg.setPointerCapture(event.pointerId);
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

        const unitsPerPixel = drag.box.width / svg.clientWidth;
        setBox(state, { ...drag.box, x: drag.box.x - dx * unitsPerPixel, y: drag.box.y - dy * unitsPerPixel });
    });

    svg.addEventListener("pointerup", event => {
        const drag = state.drag;
        state.drag = null;
        if (!drag || drag.id !== event.pointerId) {
            return;
        }

        const point = toBoard(state, event.clientX, event.clientY);
        if (drag.moved) {
            if (state.tool === "move") {
                clearOverlay(state);
                state.dotnet.invokeMethodAsync("OnBoardDrag", drag.start.x, drag.start.y, point.x, point.y);
            }

            return;
        }

        if (drag.placementId && state.svg.querySelector("#layer-units")) {
            state.dotnet.invokeMethodAsync("OnUnitClick", drag.placementId);
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
        const counter = event.target.closest?.("[data-placement-id]");
        if (counter && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
            event.stopPropagation();
            state.dotnet.invokeMethodAsync("OnUnitClick", counter.getAttribute("data-placement-id"));
        }
    });

    // Keyboard zoom and editing shortcuts while the board has focus (section 5.3).
    host.addEventListener("keydown", event => {
        if (event.target.closest?.("[data-placement-id]")) {
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
    });

    return {
        load: (baseUrl, query, viewBox, layers, visible) => load(state, baseUrl, query, viewBox, layers, visible),
        reloadLayers: (baseUrl, query, layers) => reloadLayers(state, baseUrl, query, layers),
        setVisible: visible => setVisible(state, visible),
        setUnits: markup => setUnits(state, markup),
        highlight: points => highlight(state, points),
        setTool: tool => setTool(state, tool),
        applyPatch: patch => applyPatch(state, patch),
        setComparison: (mode, value) => setComparison(state, mode, value),
        focusPoint: (x, y) => focusPoint(state, x, y),
        copyText: text => navigator.clipboard?.writeText(text),
        reset: () => reset(state),
        dispose: () => host.replaceChildren(),
    };
}

async function fetchGroups(baseUrl, query, layers) {
    const texts = await Promise.all(layers.map(async layer => {
        const response = await fetch(`${baseUrl}/${layer}.svg${query}`);
        if (!response.ok) {
            throw new Error(`Layer ${layer} failed: ${response.status}`);
        }

        return response.text();
    }));
    const parser = new DOMParser();
    return texts.map(text => document.importNode(parser.parseFromString(text, "image/svg+xml").documentElement.firstElementChild, true));
}

async function load(state, baseUrl, query, viewBox, layers, visible) {
    const generation = ++state.generation;
    const groups = await fetchGroups(baseUrl, query, layers);
    if (generation !== state.generation) {
        return;
    }

    const [x, y, width, height] = viewBox.split(" ").map(Number);
    const home = { x, y, width, height };
    const keepBox = state.home && state.home.width === width && state.home.height === height;
    state.home = home;
    state.svg.replaceChildren(...groups);
    state.highlight = null;
    state.overlay = null;
    setVisible(state, visible);
    setBox(state, keepBox ? state.box : { ...home });
}

// Replaces the named layers in place, keeping the view: after an edit, for layers the patch does not cover.
async function reloadLayers(state, baseUrl, query, layers) {
    const generation = state.generation;
    const groups = await fetchGroups(baseUrl, query, layers);
    if (generation !== state.generation) {
        return;
    }

    for (const group of groups) {
        const existing = state.svg.querySelector(`#${CSS.escape(group.id)}`);
        if (existing) {
            group.style.display = existing.style.display;
            existing.replaceWith(group);
        }
    }

    applyComparison(state);
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
            group.style.display = visible.includes(layer) ? "" : "none";
        }
    }

    applyComparison(state);
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
    state.svg.querySelector("#comparison-clip")?.remove();
    for (const group of groups) {
        group.style.opacity = "";
        group.style.visibility = "";
        group.removeAttribute("clip-path");
    }

    state.host.classList.toggle("side-by-side", mode === "side");
    if (mode !== "side" && state.side) {
        state.side.remove();
        state.side = null;
    }

    if (mode === "overlay") {
        for (const group of groups) {
            group.style.opacity = value;
        }
    } else if (mode === "swipe" && state.home) {
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
        for (const group of groups) {
            group.style.visibility = "hidden";
        }

        if (state.box) {
            state.side.setAttribute("viewBox", `${state.box.x} ${state.box.y} ${state.box.width} ${state.box.height}`);
        }
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

function zoomAt(state, point, factor) {
    const width = clamp(state.box.width * factor, state.home.width / 40, state.home.width * 2);
    const scale = width / state.box.width;
    setBox(state, {
        x: point.x - (point.x - state.box.x) * scale,
        y: point.y - (point.y - state.box.y) * scale,
        width,
        height: state.box.height * scale,
    });
}

function zoomCenter(state, factor) {
    if (state.box) {
        zoomAt(state, { x: state.box.x + state.box.width / 2, y: state.box.y + state.box.height / 2 }, factor);
    }
}

function reset(state) {
    if (state.home) {
        setBox(state, { ...state.home });
    }
}

function setUnits(state, markup) {
    state.svg.querySelector("#layer-units")?.remove();
    if (!markup) {
        return;
    }

    const fragment = new DOMParser().parseFromString(
        `<svg xmlns="${svgNamespace}">${markup}</svg>`, "image/svg+xml");
    if (fragment.querySelector("parsererror")) {
        throw new Error("Invalid demo unit SVG");
    }
    const group = fragment.documentElement.firstElementChild;
    if (group?.id !== "layer-units") {
        throw new Error("Invalid demo unit layer");
    }
    state.svg.appendChild(document.importNode(group, true));
    if (state.highlight?.isConnected) {
        state.svg.appendChild(state.highlight);
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

function setBox(state, box) {
    state.box = box;
    const value = `${box.x} ${box.y} ${box.width} ${box.height}`;
    state.svg.setAttribute("viewBox", value);
    state.side?.setAttribute("viewBox", value);
}

function toBoard(state, clientX, clientY) {
    const point = state.svg.createSVGPoint();
    point.x = clientX;
    point.y = clientY;
    const board = point.matrixTransform(state.svg.getScreenCTM().inverse());
    return { x: board.x, y: board.y };
}

function clamp(value, min, max) {
    return Math.min(max, Math.max(min, value));
}
