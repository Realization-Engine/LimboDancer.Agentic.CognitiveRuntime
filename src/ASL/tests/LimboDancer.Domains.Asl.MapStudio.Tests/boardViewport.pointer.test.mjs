import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";

const source = await readFile(new URL("../../LimboDancer.Domains.Asl.MapStudio/wwwroot/js/boardViewport.js", import.meta.url));
const { create, unitTier, summarizeFrames, labelsInView } = await import(`data:text/javascript;base64,${source.toString("base64")}`);

let currentSvg = null;

// A fake element with the attribute members the viewport reads and writes.
function element(id, attributes = {}) {
    const values = new Map(Object.entries(attributes));
    return {
        id,
        style: {},
        remove() { currentSvg.children.splice(currentSvg.children.indexOf(this), 1); },
        getAttribute(name) { return values.has(name) ? values.get(name) : null; },
        setAttribute(name, value) { values.set(name, String(value)); },
    };
}

function viewportFixture(factory = create, viewKey) {
    const handlers = new Map();
    const calls = [];
    const svg = {
        children: [],
        clientWidth: 100,
        attributes: {},
        setAttribute(name, value) { this.attributes[name] = value; },
        addEventListener(name, handler) { handlers.set(name, handler); },
        setPointerCapture() {},
        replaceChildren(...groups) { this.children = groups; },
        appendChild(group) { this.children.push(group); },
        querySelector(selector) { return this.children.find(group => "#" + group.id === selector); },
        createSVGPoint() { return { matrixTransform() { return { x: 10, y: 20 }; } }; },
        getScreenCTM() { return { inverse() { return {}; } }; },
    };
    currentSvg = svg;
    globalThis.document = {
        createElementNS() { return svg; },
        importNode(group) { return group; },
    };
    globalThis.DOMParser = class {
        parseFromString(markup) {
            const group = markup.includes("layer-units")
                ? element("layer-units", { "data-face-size": /data-face-size="([^"]+)"/.exec(markup)?.[1] ?? "35" })
                : element("layer-grid");
            return { documentElement: { firstElementChild: group }, querySelector() { return null; } };
        }
    };
    globalThis.fetch = async () => ({ ok: true, async text() { return "<svg><g id=\"layer-grid\"/></svg>"; } });
    // Pass 31c (design D16): the viewport reads the host's data-view-key for the view it keeps; a host with none keeps no view.
    const host = { replaceChildren() {}, addEventListener() {}, focus() {}, removeEventListener() {}, classList: { toggle() {} }, dataset: { viewKey } };
    const viewport = factory(host, { invokeMethodAsync(...args) { calls.push(args); } });
    return { viewport, svg, handlers, calls, host };
}

const unitHit = { closest() { return { getAttribute() { return "demo-a"; } }; } };

test("pointer capture still selects the counter hit on pointerdown", async () => {
    const { viewport, svg, handlers, calls } = viewportFixture();
    await viewport.load("render/board/version/exact", "", "0 0 100 100", ["grid"], ["grid"]);
    viewport.setUnits("<g id=\"layer-units\" data-face-size=\"35\"/>");
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 10, clientY: 20, target: unitHit });
    handlers.get("pointerup")({ pointerId: 1, clientX: 10, clientY: 20, target: svg });
    assert.deepEqual(calls, [["OnUnitClick", "demo-a"]]);
});

test("dragging a counter pans without selecting it", async () => {
    const { viewport, svg, handlers, calls } = viewportFixture();
    await viewport.load("render/board/version/exact", "", "0 0 100 100", ["grid"], ["grid"]);
    viewport.setUnits("<g id=\"layer-units\" data-face-size=\"35\"/>");
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 10, clientY: 20, target: unitHit });
    handlers.get("pointermove")({ pointerId: 1, clientX: 20, clientY: 20 });
    handlers.get("pointerup")({ pointerId: 1, clientX: 20, clientY: 20, target: svg });
    assert.deepEqual(calls, []);
});

test("Enter on a focused unit selects it", async () => {
    const { viewport, handlers, calls } = viewportFixture();
    await viewport.load("render/board/version/exact", "", "0 0 100 100", ["grid"], ["grid"]);
    viewport.setUnits("<g id=\"layer-units\" data-face-size=\"35\"/>");
    let prevented = false;
    handlers.get("keydown")({ key: "Enter", target: unitHit, preventDefault() { prevented = true; }, stopPropagation() {} });
    assert.equal(prevented, true);
    assert.deepEqual(calls, [["OnUnitClick", "demo-a"]]);
});

test("the unit tier follows the face size on screen", () => {
    assert.equal(unitTier(35, 100, 1000), "far");
    assert.equal(unitTier(35, 1000, 1000), "mid");
    assert.equal(unitTier(35, 2000, 1000), "near");
    assert.equal(unitTier(20, 0, 0), "mid");
});

test("the pan probe summarizes frames from their times", () => {
    assert.deepEqual(summarizeFrames([0, 16, 32, 132, 148]),
        { frames: 4, spanMs: 148, framesPerSecond: 27, longestFrameMs: 100, medianFrameMs: 16 });
    assert.deepEqual(summarizeFrames([5]), { frames: 0, spanMs: 0, framesPerSecond: 0, longestFrameMs: 0, medianFrameMs: 0 });
});

// Large Campaign Map Plan, A2: a pointer is read from the drawing's rectangle and observed size, not the screen matrix. A 200 by 100 drawing
// of a 1000 by 1000 box fits at a tenth of a pixel per unit, letterboxed 50 pixels on each side.
async function observedFixture(rect, rotated) {
    const previous = globalThis.ResizeObserver;
    let watch = null;
    globalThis.ResizeObserver = class { constructor(callback) { watch = callback; } observe() {} disconnect() {} };
    try {
        const fixture = viewportFixture();
        fixture.svg.getBoundingClientRect = () => rect;
        await fixture.viewport.load("render/board/version/exact", "", "0 0 1000 1000", ["grid"], ["grid"]);
        if (rotated) fixture.viewport.setRotation(true);
        watch([{ target: fixture.svg, contentRect: { width: 200, height: 100 } }]);
        return fixture;
    } finally {
        globalThis.ResizeObserver = previous;
    }
}

test("a click is read from the drawing's rectangle, letterboxed, without the screen matrix", async () => {
    const { svg, handlers, calls } = await observedFixture({ left: 10, top: 20, right: 210, bottom: 120 }, false);
    svg.getScreenCTM = () => { throw new Error("the screen matrix lays out the map"); };
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 85, clientY: 50, target: svg });
    handlers.get("pointerup")({ pointerId: 1, clientX: 85, clientY: 50, target: svg });
    assert.deepEqual(calls.at(-1), ["OnBoardClick", 250, 300]);
});

test("a click on a turned map is read through the quarter turn", async () => {
    // Turned, the 200 by 100 drawing covers 100 by 200 pixels of screen; its x runs down the screen and its y to the left.
    const { svg, handlers, calls } = await observedFixture({ left: 10, top: 20, right: 110, bottom: 220 }, true);
    svg.getScreenCTM = () => { throw new Error("the screen matrix lays out the map"); };
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 80, clientY: 95, target: svg });
    handlers.get("pointerup")({ pointerId: 1, clientX: 80, clientY: 95, target: svg });
    assert.deepEqual(calls.at(-1), ["OnBoardClick", 250, 300]);
});

test("a burst of wheel events compounds its zoom and draws once a frame", async () => {
    const previousFrame = globalThis.requestAnimationFrame;
    const frames = [];
    try {
        const { viewport, svg, handlers } = viewportFixture();
        await viewport.load("render/board/version/exact", "", "0 0 1000 1000", ["grid"], ["grid"]);
        globalThis.requestAnimationFrame = callback => frames.push(callback);
        const before = svg.attributes.viewBox;
        for (let index = 0; index < 3; index++) {
            handlers.get("wheel")({ deltaY: -100, clientX: 10, clientY: 20, preventDefault() {} });
        }
        assert.equal(frames.length, 1);
        assert.equal(svg.attributes.viewBox, before);
        frames.shift()();
        const width = Number(svg.attributes.viewBox.split(" ")[2]);
        assert.ok(Math.abs(width - 1000 * 0.8 ** 3) < 1e-9);
    } finally {
        globalThis.requestAnimationFrame = previousFrame;
    }
});

test("only the labels in view and its margin are drawn", () => {
    const index = { names: ["A1", "A2", "B1", "Z9"], x: [5, 5, 25, 500], y: [5, 40, 5, 500] };
    // A 20 by 20 box widened by a tenth on each side: x and y from -2 to 22.
    assert.deepEqual(labelsInView(index, { x: 0, y: 0, width: 20, height: 20 }), [0]);
    assert.deepEqual(labelsInView(index, { x: 0, y: 0, width: 30, height: 40 }), [0, 1, 2]);
    assert.deepEqual(labelsInView(index, { x: 1000, y: 1000, width: 10, height: 10 }), []);
});

test("zooming switches the tier the units layer shows", async () => {
    const { viewport, svg } = viewportFixture();
    await viewport.load("render/board/version/exact", "", "0 0 1000 1000", ["grid"], ["grid"]);
    viewport.setUnits("<g id=\"layer-units\" data-face-size=\"35\"/>");
    const layer = svg.querySelector("#layer-units");
    assert.equal(layer.getAttribute("data-active-tier"), "far");
    svg.clientWidth = 2000;
    viewport.reset();
    assert.equal(layer.getAttribute("data-active-tier"), "near");
    viewport.setUnits(null);
    assert.equal(svg.querySelector("#layer-units"), undefined);
});


test("saved views survive a fresh module, isolate viewers and versions, and retain explicit fit", async () => {
    const storage = new Map();
    globalThis.localStorage = {
        getItem: key => storage.get(key) ?? null,
        setItem: (key, value) => storage.set(key, value),
        removeItem: key => storage.delete(key),
    };
    const first = viewportFixture(create, "campaign-test/allied");
    await first.viewport.load("render/board/v1/exact", "", "0 0 1000 1000", [], []);
    first.viewport.zoom(0.8);
    const zoomed = first.svg.attributes.viewBox;
    assert.notEqual(zoomed, "0 0 1000 1000");
    // No dispose: a reload or service disconnect must not need graceful cleanup to retain the view.
    const fresh = await import(`data:text/javascript;base64,${source.toString("base64")}#reload`);
    const restored = viewportFixture(fresh.create, "campaign-test/allied");
    await restored.viewport.load("render/board/v1/exact", "", "0 0 1000 1000", [], []);
    assert.equal(restored.svg.attributes.viewBox, zoomed);
    assert.equal(restored.viewport.hasRestoredView(), true);
    const enemy = viewportFixture(fresh.create, "campaign-test/german");
    await enemy.viewport.load("render/board/v1/exact", "", "0 0 1000 1000", [], []);
    assert.equal(enemy.svg.attributes.viewBox, "0 0 1000 1000");
    await restored.viewport.load("render/board/v2/exact", "", "0 0 1000 1000", [], []);
    assert.equal(restored.svg.attributes.viewBox, "0 0 1000 1000");
    assert.equal(restored.viewport.hasRestoredView(), false);
    first.viewport.reset();
    const fitted = viewportFixture(fresh.create, "campaign-test/allied");
    await fitted.viewport.load("render/board/v1/exact", "", "0 0 1000 1000", [], []);
    assert.equal(fitted.svg.attributes.viewBox, "0 0 1000 1000");
    assert.equal(fitted.viewport.hasRestoredView(), true);
    delete globalThis.localStorage;
});

test("damaged saved views are ignored and blocked storage keeps map controls working", async () => {
    globalThis.localStorage = { getItem: () => '{"home":{"x":0,"y":0,"width":100,"height":100},"box":{"x":0,"y":0,"width":-1,"height":100},"fitted":false}', setItem() {}, removeItem() {} };
    const invalid = viewportFixture(create, "invalid/allied");
    await invalid.viewport.load("render/board/v1/exact", "", "0 0 100 100", [], []);
    assert.equal(invalid.svg.attributes.viewBox, "0 0 100 100");
    assert.equal(invalid.viewport.hasRestoredView(), false);
    globalThis.localStorage = { getItem() { throw Error("blocked"); }, setItem() { throw Error("blocked"); }, removeItem() { throw Error("blocked"); } };
    invalid.viewport.zoom(0.8);
    const zoomed = invalid.svg.attributes.viewBox;
    const another = viewportFixture(create, "invalid/allied");
    await another.viewport.load("render/board/v1/exact", "", "0 0 100 100", [], []);
    assert.equal(another.svg.attributes.viewBox, zoomed);
    delete globalThis.localStorage;
});


test("optional scene timing waits for counters and paint opportunities and ignores superseded loads", async () => {
    const previousLocation = globalThis.location;
    const previousFrame = globalThis.requestAnimationFrame;
    const frames = [];
    globalThis.location = { search: "?mapMetrics=1" };
    globalThis.requestAnimationFrame = callback => frames.push(callback);
    try {
        const { viewport, host } = viewportFixture();
        await viewport.load("render/board/v1/exact", "", "0 0 100 100", ["grid"], ["grid"]);
        assert.equal(host.dataset.mapMetricStatus, "loading");
        // The loaded box is drawn at the next frame (Large Campaign Map Plan, A1); the scene's two paint opportunities follow the counters.
        frames.shift()();
        viewport.setUnits('<g id="layer-units" data-face-size="35"/>');
        assert.equal(host.dataset.mapMetricStatus, "loading");
        frames.shift()();
        assert.equal(host.dataset.mapMetricStatus, "loading");
        frames.shift()();
        assert.equal(host.dataset.mapMetricStatus, "ready");
        assert.ok(Number(host.dataset.mapSceneLoadMs) >= 0);
        assert.ok(Number(host.dataset.mapNavigationToSceneMs) >= Number(host.dataset.mapSceneLoadMs));
        await viewport.load("render/board/v2/exact", "", "0 0 100 100", [], []);
        viewport.setUnits('<g id="layer-units" data-face-size="35"/>');
        await viewport.load("render/board/v3/exact", "", "0 0 100 100", [], []);
        frames.shift()();
        frames.shift()();
        assert.equal(host.dataset.mapMetricStatus, "loading");
        globalThis.location = { search: "" };
        const ordinary = viewportFixture();
        await ordinary.viewport.load("render/board/v1/exact", "", "0 0 100 100", [], []);
        ordinary.viewport.setUnits('<g id="layer-units" data-face-size="35"/>');
        assert.equal(ordinary.host.dataset.mapMetricStatus, undefined);
    } finally {
        globalThis.location = previousLocation;
        globalThis.requestAnimationFrame = previousFrame;
    }
});


test("hex label detail follows zoom without changing the user's hidden-layer choice", async () => {
    const { viewport, svg } = viewportFixture();
    await viewport.load("render/board/v1/exact", "", "0 0 1000 1000", [], []);
    const labels = element("layer-labels");
    svg.children.push(labels);
    viewport.zoom(1);
    assert.equal(labels.getAttribute("data-detail-visible"), "false");
    viewport.zoom(0.1);
    assert.equal(labels.getAttribute("data-detail-visible"), "true");
    viewport.setVisible([]);
    viewport.zoom(1);
    assert.equal(labels.getAttribute("data-detail-visible"), "true");
    assert.equal(labels.getAttribute("data-layer-hidden"), "true");
    viewport.zoom(10);
    assert.equal(labels.getAttribute("data-detail-visible"), "false");
    assert.equal(labels.getAttribute("data-layer-hidden"), "true");
});

// The counter layer's patches (the Counter Layer Design, sections 6 and 10): a small tree of fake nodes, enough for the layer, its fragments, and
// the counters in them. Markup in these tests is <g id="..." data-units="a,b" data-face-size="..."> groups, one level deep, and a fragment's
// counters are made from data-units.
function node(id, attributes = {}, children = []) {
    const values = new Map(Object.entries(attributes));
    const self = {
        id, children: [], parent: null,
        get firstElementChild() { return this.children[0] ?? null; },
        get firstChild() { return this.children[0] ?? null; },
        getAttribute(name) { return values.has(name) ? values.get(name) : null; },
        setAttribute(name, value) { values.set(name, String(value)); },
        remove() { if (this.parent) { this.parent.children.splice(this.parent.children.indexOf(this), 1); this.parent = null; } },
        replaceWith(other) { const at = this.parent.children.indexOf(this); other.parent = this.parent; this.parent.children[at] = other; this.parent = null; },
        insertBefore(child, before) {
            child.parent = this;
            const at = before ? this.children.indexOf(before) : -1;
            this.children.splice(at < 0 ? this.children.length : at, 0, child);
            return child;
        },
        appendChild(child) { return this.insertBefore(child, null); },
        contains(other) { for (let at = other; at; at = at.parent) if (at === this) return true; return false; },
        closest(selector) { for (let at = this; at; at = at.parent) if (selector === "[data-unit-id]" && at.getAttribute("data-unit-id")) return at; return null; },
        focus() { globalThis.document.activeElement = this; },
        querySelector(selector) {
            const direct = /^:scope > #(.+)$/.exec(selector);
            if (direct) return this.children.find(child => child.id === direct[1]) ?? null;
            const byId = /^#(.+)$/.exec(selector);
            const unit = /^\[data-unit-id="(.+)"\]$/.exec(selector);
            const all = []; const walk = at => at.children.forEach(child => { all.push(child); walk(child); }); walk(this);
            return all.find(child => byId ? child.id === byId[1] : unit ? child.getAttribute("data-unit-id") === unit[1] : false) ?? null;
        },
    };
    children.forEach(child => self.appendChild(child));
    return self;
}

function parseGroups(markup) {
    return [...markup.matchAll(/<g id="([^"]+)"([^>]*)>/g)].map(([, id, rest]) => {
        const attributes = Object.fromEntries([...rest.matchAll(/([\w-]+)="([^"]*)"/g)].map(([, name, value]) => [name, value]));
        const units = (attributes["data-units"] ?? "").split(",").filter(Boolean);
        return node(id, attributes, units.map(unit => node(`unit-${unit}`, { "data-unit-id": unit })));
    });
}

function patchFixture() {
    const svg = node("svg");
    Object.assign(svg, {
        clientWidth: 1000,
        addEventListener() {}, setPointerCapture() {},
        replaceChildren(...groups) { svg.children = []; groups.forEach(group => svg.appendChild(group)); },
        createSVGPoint() { return { matrixTransform() { return { x: 0, y: 0 }; } }; },
        getScreenCTM() { return { inverse() { return {}; } }; },
    });
    const polygon = node("highlight");
    globalThis.document = {
        activeElement: null,
        createElementNS(_, name) { return name === "polygon" ? polygon : svg; },
        importNode(group) { return group; },
    };
    globalThis.CSS = { escape: text => text };
    globalThis.DOMParser = class {
        parseFromString(markup) {
            const inner = markup.replace(/^<svg[^>]*>/, "").replace(/<\/svg>$/, "");
            // A layer (setUnits) holds its fragments; a patch's upserts are siblings.
            const groups = parseGroups(inner);
            const layer = groups[0]?.id === "layer-units" ? node("layer-units", Object.fromEntries(["data-face-size", "data-unit-set"].map(name => [name, groups[0].getAttribute(name)])), groups.slice(1)) : null;
            const root = node("svg", {}, layer ? [layer] : groups);
            return { documentElement: root, querySelector() { return null; } };
        }
    };
    globalThis.fetch = async () => ({ ok: true, async text() { return "<svg><g id=\"layer-grid\"/></svg>"; } });
    const host = { replaceChildren() {}, addEventListener() {}, focus() {}, removeEventListener() {}, classList: { toggle() {} }, dataset: {} };
    const viewport = create(host, { invokeMethodAsync() {} });
    return { viewport, svg, polygon };
}

const layerOf = svg => svg.children.find(child => child.id === "layer-units");
const ids = svg => layerOf(svg).children.map(child => child.id);
const patch = (base, removals, upserts, faceSize = "35") => ({ base, version: base + 1, removals, upserts, faceSize, unitSet: "game-a" });

async function patchedLayer() {
    const fixture = patchFixture();
    await fixture.viewport.load("render/board/v1/exact", "", "0 0 1000 1000", [], []);
    fixture.viewport.setUnits('<g id="layer-units" data-face-size="35" data-unit-set="game-a"><g id="units-1-1" data-units="a"></g><g id="units-1-3" data-units="b,c"></g><g id="units-2-2" data-units="d"></g>', 7);
    return fixture;
}

test("a unit patch removes, replaces in place, and inserts before the next fragment the page holds", async () => {
    const { viewport, svg } = await patchedLayer();
    assert.equal(layerOf(svg).getAttribute("data-units-version"), "7");
    assert.equal(viewport.applyUnitPatch(patch(7, ["units-1-1"], [
        { id: "units-1-2", markup: '<g id="units-1-2" data-units="a"></g>', before: "units-1-3" },
        { id: "units-1-3", markup: '<g id="units-1-3" data-units="c"></g>', before: null },
        { id: "units-3-1", markup: '<g id="units-3-1" data-units="e"></g>', before: null },
    ])), true);
    assert.deepEqual(ids(svg), ["units-1-2", "units-1-3", "units-2-2", "units-3-1"]);
    assert.deepEqual(layerOf(svg).children[1].children.map(unit => unit.getAttribute("data-unit-id")), ["c"]);
    assert.equal(layerOf(svg).getAttribute("data-units-version"), "8");
});

test("a unit patch made from another version is refused and changes nothing", async () => {
    const { viewport, svg } = await patchedLayer();
    assert.equal(viewport.applyUnitPatch(patch(6, ["units-1-1"], [])), false);
    assert.deepEqual(ids(svg), ["units-1-1", "units-1-3", "units-2-2"]);
    assert.equal(layerOf(svg).getAttribute("data-units-version"), "7");
    // A page with no layer refuses too.
    const empty = patchFixture();
    assert.equal(empty.viewport.applyUnitPatch(patch(7, [], [])), false);
});

test("a unit patch keeps the focused counter focused, the highlight last, and the tier in view", async () => {
    const { viewport, svg, polygon } = await patchedLayer();
    viewport.highlight("0,0 1,1 2,2");
    layerOf(svg).querySelector('[data-unit-id="b"]').focus();
    const tier = layerOf(svg).getAttribute("data-active-tier");
    assert.ok(tier);
    // The fragment holding the focused counter is replaced, and the smallest face on the map grows tenfold.
    assert.equal(viewport.applyUnitPatch(patch(7, [], [{ id: "units-1-3", markup: '<g id="units-1-3" data-units="b,c"></g>', before: null }], "350")), true);
    assert.equal(globalThis.document.activeElement.getAttribute("data-unit-id"), "b");
    assert.ok(layerOf(svg).contains(globalThis.document.activeElement));
    assert.equal(svg.children.at(-1), polygon);
    assert.equal(layerOf(svg).getAttribute("data-face-size"), "350");
    assert.notEqual(layerOf(svg).getAttribute("data-active-tier"), tier);
});

test("the unit figures go first in the map and are replaced, not repeated", async () => {
    const { viewport, svg } = await patchedLayer();
    viewport.setUnitFigures('<g id="unit-figures"></g>');
    viewport.setUnitFigures('<g id="unit-figures"></g>');
    assert.equal(svg.children[0].id, "unit-figures");
    assert.equal(svg.children.filter(child => child.id === "unit-figures").length, 1);
    // The layer choice leaves it alone.
    viewport.setVisible([]);
    assert.equal(svg.children[0].getAttribute("data-layer-hidden"), null);
});
