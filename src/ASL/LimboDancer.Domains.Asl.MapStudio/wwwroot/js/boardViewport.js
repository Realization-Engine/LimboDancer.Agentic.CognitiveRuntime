// Board viewport (Architecture and Rendering Design, section 4.5). Layer fragments are fetched as SVG and imported
// into one inline <svg>; pan and zoom only change its viewBox, so the browser never rasterizes anything we send.

const svgNamespace = "http://www.w3.org/2000/svg";

export function create(host, dotnet) {
    const svg = document.createElementNS(svgNamespace, "svg");
    svg.setAttribute("class", "board-svg");
    host.replaceChildren(svg);

    const state = { host, svg, dotnet, box: null, home: null, drag: null, generation: 0, highlight: null };

    svg.addEventListener("wheel", event => {
        if (!state.box) {
            return;
        }

        event.preventDefault();
        const point = toBoard(state, event.clientX, event.clientY);
        const factor = event.deltaY < 0 ? 0.8 : 1.25;
        const width = clamp(state.box.width * factor, state.home.width / 40, state.home.width * 2);
        const scale = width / state.box.width;
        setBox(state, {
            x: point.x - (point.x - state.box.x) * scale,
            y: point.y - (point.y - state.box.y) * scale,
            width,
            height: state.box.height * scale,
        });
    }, { passive: false });

    svg.addEventListener("pointerdown", event => {
        if (!state.box || event.button !== 0) {
            return;
        }

        state.drag = { id: event.pointerId, x: event.clientX, y: event.clientY, box: { ...state.box }, moved: false };
        svg.setPointerCapture(event.pointerId);
    });

    svg.addEventListener("pointermove", event => {
        const drag = state.drag;
        if (!drag || drag.id !== event.pointerId) {
            return;
        }

        const dx = event.clientX - drag.x;
        const dy = event.clientY - drag.y;
        if (!drag.moved && Math.hypot(dx, dy) < 4) {
            return;
        }

        drag.moved = true;
        const unitsPerPixel = drag.box.width / svg.clientWidth;
        setBox(state, { ...drag.box, x: drag.box.x - dx * unitsPerPixel, y: drag.box.y - dy * unitsPerPixel });
    });

    svg.addEventListener("pointerup", event => {
        const drag = state.drag;
        state.drag = null;
        if (!drag || drag.id !== event.pointerId || drag.moved) {
            return;
        }

        const point = toBoard(state, event.clientX, event.clientY);
        state.dotnet.invokeMethodAsync("OnBoardClick", point.x, point.y);
    });

    return {
        load: (baseUrl, query, viewBox, layers, visible) => load(state, baseUrl, query, viewBox, layers, visible),
        setVisible: visible => setVisible(state, visible),
        highlight: points => highlight(state, points),
        reset: () => state.home && setBox(state, { ...state.home }),
        dispose: () => host.replaceChildren(),
    };
}

async function load(state, baseUrl, query, viewBox, layers, visible) {
    const generation = ++state.generation;
    const texts = await Promise.all(layers.map(async layer => {
        const response = await fetch(`${baseUrl}/${layer}.svg${query}`);
        if (!response.ok) {
            throw new Error(`Layer ${layer} failed: ${response.status}`);
        }

        return response.text();
    }));

    if (generation !== state.generation) {
        return;
    }

    const parser = new DOMParser();
    const groups = texts.map(text => {
        const fragment = parser.parseFromString(text, "image/svg+xml").documentElement;
        return document.importNode(fragment.firstElementChild, true);
    });

    const [x, y, width, height] = viewBox.split(" ").map(Number);
    const home = { x, y, width, height };
    const keepBox = state.home && state.home.width === width && state.home.height === height;
    state.home = home;
    state.svg.replaceChildren(...groups);
    state.highlight = null;
    setVisible(state, visible);
    setBox(state, keepBox ? state.box : { ...home });
}

function setVisible(state, visible) {
    for (const group of state.svg.children) {
        const layer = group.id.startsWith("layer-") ? group.id.substring(6) : null;
        if (layer && layer !== "defs") {
            group.style.display = visible.includes(layer) ? "" : "none";
        }
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
    state.svg.setAttribute("viewBox", `${box.x} ${box.y} ${box.width} ${box.height}`);
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
