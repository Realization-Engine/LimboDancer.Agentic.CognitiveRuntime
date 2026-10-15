import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";

const source = await readFile(new URL("../../LimboDancer.Domains.Asl.MapStudio/wwwroot/js/boardViewport.js", import.meta.url));
const { create, unitTier } = await import(`data:text/javascript;base64,${source.toString("base64")}`);

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
        querySelector(selector) { return selector === "#layer-units" ? this.children.find(group => group.id === "layer-units") : null; },
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
