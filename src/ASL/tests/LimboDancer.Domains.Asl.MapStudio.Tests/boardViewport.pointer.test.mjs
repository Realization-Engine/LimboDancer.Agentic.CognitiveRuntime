import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";

const source = await readFile(new URL("../../LimboDancer.Domains.Asl.MapStudio/wwwroot/js/boardViewport.js", import.meta.url));
const { create } = await import(`data:text/javascript;base64,${source.toString("base64")}`);

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
    globalThis.document = {
        createElementNS() { return svg; },
        importNode(group) { return group; },
    };
    globalThis.DOMParser = class {
        parseFromString(markup) {
            const id = markup.includes("layer-units") ? "layer-units" : "layer-grid";
            return { documentElement: { firstElementChild: { id, style: {} } }, querySelector() { return null; } };
        }
    };
    globalThis.fetch = async () => ({ ok: true, async text() { return "<svg><g id=\"layer-grid\"/></svg>"; } });
    const viewport = create({ replaceChildren() {} }, { invokeMethodAsync(...args) { calls.push(args); } });
    return { viewport, svg, handlers, calls };
}

test("pointer capture still selects the counter hit on pointerdown", async () => {
    const { viewport, svg, handlers, calls } = viewportFixture();
    await viewport.load("render/board/version/exact", "", "0 0 100 100", ["grid"], ["grid"]);
    viewport.setUnits("<g id=\"layer-units\"/>");
    const counter = { closest() { return { getAttribute() { return "demo-a"; } }; } };
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 10, clientY: 20, target: counter });
    handlers.get("pointerup")({ pointerId: 1, clientX: 10, clientY: 20, target: svg });
    assert.deepEqual(calls, [["OnUnitClick", "demo-a"]]);
});

test("dragging a counter pans without selecting it", async () => {
    const { viewport, svg, handlers, calls } = viewportFixture();
    await viewport.load("render/board/version/exact", "", "0 0 100 100", ["grid"], ["grid"]);
    viewport.setUnits("<g id=\"layer-units\"/>");
    const counter = { closest() { return { getAttribute() { return "demo-a"; } }; } };
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 10, clientY: 20, target: counter });
    handlers.get("pointermove")({ pointerId: 1, clientX: 20, clientY: 20 });
    handlers.get("pointerup")({ pointerId: 1, clientX: 20, clientY: 20, target: svg });
    assert.deepEqual(calls, []);
});
