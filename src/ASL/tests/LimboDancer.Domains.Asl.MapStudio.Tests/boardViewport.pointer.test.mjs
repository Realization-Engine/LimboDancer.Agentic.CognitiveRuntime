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

function viewportFixture() {
    const handlers = new Map();
    const calls = [];
    const svg = {
        children: [],
        clientWidth: 100,
        setAttribute() {},
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
    const host = { replaceChildren() {}, addEventListener() {}, focus() {}, classList: { toggle() {} } };
    const viewport = create(host, { invokeMethodAsync(...args) { calls.push(args); } });
    return { viewport, svg, handlers, calls };
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
