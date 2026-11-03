import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";

const source = await readFile(new URL("../../LimboDancer.Domains.Asl.MapStudio/wwwroot/js/boardViewport.js", import.meta.url));
const { create, unitTier, summarizeFrames, labelsInView, tilesInView, tileVariant, layersAfterHide, visibleBox, tilesOffInAddress,
    committedBox, gestureTransform, needsCommit, transformOffInAddress, gestureCss, tilesSharp, tileLines, boundedInAddress, tilesAtLevel, hexCentre, fragmentBounds, pointsBounds, boxesMeet, nearGridPath,
    nearTilesInAddress, cullOn, probePlan, frameBreakdown, withGridInTiles } = await import(`data:text/javascript;base64,${source.toString("base64")}`);

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

// Screen-Bounded Map Analysis, section 5, step 1: the probe waits until every tile kept in view shows its own image.
test("the map is sharp when every pooled tile has its file", () => {
    const tile = href => ({ hasAttribute: name => name === "href" && href !== null });
    const loaded = tile("tile-te-0-0-0.png");
    const loading = tile(null);
    assert.equal(tilesSharp({ pool: new Map([["a", loaded]]) }), true);
    assert.equal(tilesSharp({ pool: new Map([["a", loaded], ["b", loading]]) }), false);
    assert.equal(tilesSharp({ pool: new Map() }), true);
});

// Screen-Bounded Map Analysis, section 3: the tiles' lines under ?present=bounded, the grid left to the vectors at near zoom (decision N2).
test("bounded tiles ask for the grid and the boundaries the view shows", () => {
    assert.equal(tileLines(false, "te", true, false, "campaign-1"), "");
    assert.equal(tileLines(true, "te", true, false, null), "+g");
    assert.equal(tileLines(true, "te", true, false, "campaign-1"), "+gb");
    assert.equal(tileLines(true, "te", true, true, "campaign-1"), "+b");
    assert.equal(tileLines(true, "t", false, false, null), "");
    assert.equal(tileLines(true, "e", true, false, "campaign-1"), "");
    assert.equal(boundedInAddress(["http://localhost/games/play?game=g&present=bounded"]), true);
    assert.equal(boundedInAddress(["http://localhost/games/play?game=g", undefined]), false);
});

// The Goodwood Lab Techniques Analysis, G1: with ?near=tiles the grid stays in the tiles at near zoom, as the Map Lab draws it.
test("near tiles keep the grid in the tiles at near zoom", () => {
    assert.equal(tileLines(true, "te", true, true, null, true), "+g");
    assert.equal(tileLines(true, "te", true, true, "campaign-1", true), "+gb");
    assert.equal(tileLines(true, "te", false, true, null, true), "");
    assert.equal(tileLines(false, "te", true, true, null, true), "");
    assert.equal(nearTilesInAddress(["http://localhost/games/play?game=g&present=bounded&near=tiles"]), true);
    assert.equal(nearTilesInAddress(["http://localhost/games/play?present=bounded"]), false);
});

// Item 4: with no whole-map grid in the page, the layers the probe puts back keep the grid the switch shows.
test("the grid carried by the tiles stays among the shown layers", () => {
    assert.deepEqual(withGridInTiles(["exact-tiles", "labels"], true, false), ["exact-tiles", "labels", "grid"]);
    assert.deepEqual(withGridInTiles(["exact-tiles", "labels"], false, false), ["exact-tiles", "labels"]);
    assert.deepEqual(withGridInTiles(["exact-tiles", "labels"], true, true), ["exact-tiles", "labels"]);
    assert.deepEqual(withGridInTiles(["grid", "labels"], true, false), ["grid", "labels"]);
});

// Item 2: counters out of view are hidden where a page asks for it, else with ?present=bounded.
test("culling follows the page's choice, else the bounded map", () => {
    assert.equal(cullOn(null, true), true);
    assert.equal(cullOn(null, false), false);
    assert.equal(cullOn(true, false), true);
    assert.equal(cullOn(false, true), false);
});

// The user, 2026-11-03: the Measure button tests every option; mapProbeSuite runs the same plan on Goodwood.
test("the probe plan runs every zoom in each counter mode, and the near runs on both grids when bounded", () => {
    const bare = probePlan(false, false);
    assert.deepEqual(bare.map(run => run.name), ["Fit", "5 in", "labels, long drag", "near, long drag", "deep zoom from Fit"]);
    assert.ok(bare.every(run => run.drawn === "none" && run.nearTiles === undefined && run.nearRun === undefined));

    const lab = probePlan(true, false);
    assert.equal(lab.length, 15);
    assert.deepEqual(lab.slice(0, 3).map(run => [run.drawn, run.cull, run.units]), [["all", false, undefined], ["in view", true, undefined], ["none", undefined, false]]);

    const goodwood = probePlan(true, true);
    // Three zooms in three modes, and two near runs in three modes on both grids.
    assert.equal(goodwood.length, 9 + 12);
    const near = goodwood.filter(run => run.name === "near, long drag");
    assert.deepEqual(near.map(run => `${run.drawn}/${run.nearTiles}`), ["all/false", "all/true", "in view/false", "in view/true", "none/false", "none/true"]);
    assert.equal(near[0].zoom, "near");
    assert.equal(goodwood.find(run => run.name === "deep zoom from Fit").wheel, "in");
});

// Item 5: a long frame's time split into scripts, by name, and rendering.
test("the long frames' breakdown sums scripts by name and the rendering after them", () => {
    const breakdown = frameBreakdown([
        { startTime: 0, duration: 100, renderStart: 40, scripts: [{ sourceFunctionName: "flushBox", duration: 30 }, { invoker: "pointermove", duration: 5 }] },
        { startTime: 200, duration: 60, renderStart: 210, scripts: [{ sourceFunctionName: "flushBox", duration: 8 }] },
        { startTime: 300, duration: 55, renderStart: 0 },
    ]);
    assert.equal(breakdown.scriptMs, 43);
    assert.equal(breakdown.renderMs, 60 + 50);
    assert.deepEqual(breakdown.scripts, [{ name: "flushBox", ms: 38 }, { name: "pointermove", ms: 5 }]);
    assert.deepEqual(frameBreakdown([]), { scriptMs: 0, renderMs: 0, scripts: [] });
});

// Screen-Bounded Map Analysis, sections 3.3 and 3.4: counters and marks are placed from their hex or their polygon, and hidden outside the box.
test("counters and marks are placed for the view", () => {
    const index = { hexWidth: 56.25, hexHeight: 64.5, a1x: 0, a1y: 32.25 };
    assert.deepEqual(hexCentre(index, 0, 0), { x: 0, y: 32.25 });
    assert.deepEqual(hexCentre(index, 1, 0), { x: 56.25, y: 0 });
    const counter = fragmentBounds(index, "10,4");
    assert.deepEqual(counter, { x: 562.5 - 84.375, y: 290.25 - 96.75, width: 168.75, height: 193.5 });
    assert.equal(fragmentBounds(index, null), null);
    assert.deepEqual(pointsBounds("10,20 30,5 25,40"), { x: 10, y: 5, width: 20, height: 35 });
    assert.equal(pointsBounds(undefined), null);
    const view = { x: 0, y: 0, width: 500, height: 300 };
    assert.equal(boxesMeet(counter, view), true);
    assert.equal(boxesMeet({ x: 600, y: 0, width: 10, height: 10 }, view), false);
});

// Screen-Bounded Map Analysis, step 4: at near zoom the grid is drawn for the hexes around the box only, each hex a closed outline.
test("the near grid outlines only the hexes about the box", () => {
    // A 33 by 10 board: 1,800 by 645 cells.
    const index = { width: 1800, height: 645, hexWidth: 56.25, hexHeight: 64.5, a1x: 0, a1y: 32.25 };
    const small = nearGridPath(index, { x: 500, y: 200, width: 100, height: 100 });
    const hexes = small.split("Z").filter(Boolean);
    // Columns 7 to 12 (a column either side), rows of each about the box: far fewer than the board's 335 hexes.
    assert.ok(hexes.length > 0 && hexes.length < 60, `${hexes.length} hexes`);
    assert.match(hexes[0], /^M[\d.]+,[\d.]+H[\d.]+L/);
    const whole = nearGridPath(index, { x: -100, y: -100, width: 2000, height: 900 }).split("Z").filter(Boolean);
    assert.equal(whole.length, 33 * 10 + 16);
    assert.equal(nearGridPath({}, { x: 0, y: 0, width: 1, height: 1 }), "");
});

// Screen-Bounded Map Analysis, section 3.5: the next finer level is fetched ahead for the same box, at twice the tiles each way.
test("the tiles of a set level cover the box widened by a tile", () => {
    const index = { width: 4096, height: 2048, tileSize: 512, levels: 4 };
    const box = { x: 1024, y: 0, width: 1024, height: 512 };
    const coarse = tilesAtLevel(index, box, 1);
    const fine = tilesAtLevel(index, box, 0);
    assert.equal(coarse.level, 1);
    assert.equal(fine.level, 0);
    // Level 1 tiles are 1,024 cells: the box widened by one, 0 to 3,072 by -1,024 to 1,536, meets columns 0 to 3 and rows 0 to 1; level 0
    // tiles are 512: 512 to 2,560 by -512 to 1,024 meets columns 1 to 5 and rows 0 to 2.
    assert.deepEqual(coarse.tiles.map(tile => `${tile.column},${tile.row}`), ["0,0", "0,1", "1,0", "1,1", "2,0", "2,1", "3,0", "3,1"]);
    assert.equal(fine.tiles.length, 5 * 3);
    assert.deepEqual(tilesInView(index, box, 0.5), coarse);
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

test("a drag on a letterboxed map follows the pointer", async () => {
    // The 1000 by 1000 box in the 200 by 100 drawing shows 2000 by 1000 units, ten units a pixel: ten pixels of drag move the box 100.
    const { svg, handlers } = await observedFixture({ left: 10, top: 20, right: 210, bottom: 120 }, false);
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 100, clientY: 70, target: svg });
    handlers.get("pointermove")({ pointerId: 1, clientX: 110, clientY: 70, target: svg });
    assert.equal(svg.attributes.viewBox, "-100 0 1000 1000");
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

// Raster Tiles Plan, task 4. Goodwood's grid: 11,700 by 12,449 cells, tiles of 512, six levels.
const goodwood = { width: 11700, height: 12449, tileSize: 512, levels: 6 };

test("the tile level is the coarsest whose pixel is no larger than a device pixel", () => {
    // Fit in a 2,033 by 803 pane: 0.064 pixels a cell, so 15.6 cells a pixel, and level 3 (8 cells a tile pixel).
    assert.equal(tilesInView(goodwood, { x: -16, y: -16, width: 11732, height: 12481 }, 0.064).level, 3);
    // Twice the device pixels: one level finer.
    assert.equal(tilesInView(goodwood, { x: -16, y: -16, width: 11732, height: 12481 }, 0.128).level, 2);
    // Zoomed past a pixel a cell: level 0; zoomed far out: never beyond the top level.
    assert.equal(tilesInView(goodwood, { x: 0, y: 0, width: 500, height: 300 }, 3).level, 0);
    assert.equal(tilesInView(goodwood, { x: 0, y: 0, width: 11700, height: 12449 }, 0.001).level, 5);
});

test("only the tiles meeting the box and a tile's margin are drawn, cut to the grid", () => {
    // Fit at level 3: tiles of 4,096 cells, 3 columns by 4 rows, all in view.
    const fit = tilesInView(goodwood, { x: -16, y: -16, width: 11732, height: 12481 }, 0.064);
    assert.equal(fit.tiles.length, 12);
    const corner = fit.tiles.find(tile => tile.column === 2 && tile.row === 3);
    assert.deepEqual(corner, { level: 3, column: 2, row: 3, x: 8192, y: 12288, width: 3508, height: 161 });
    // A small box at level 0 (512 cells a tile): the tile it is in and its neighbours, none off the grid.
    const near = tilesInView(goodwood, { x: 100, y: 100, width: 200, height: 100 }, 2);
    assert.deepEqual(near.tiles.map(tile => `${tile.column},${tile.row}`), ["0,0", "0,1", "1,0", "1,1"]);
});

test("a pane wider than the box shows strips beside it, and labels and tiles are chosen from all of it", () => {
    // A 100 by 100 box in a 300 by 100 pane: one board unit a pixel, so 100 units more on each side.
    assert.deepEqual(visibleBox({ x: 0, y: 0, width: 100, height: 100 }, { width: 300, height: 100 }), { x: -100, y: 0, width: 300, height: 100 });
    // A taller pane: the strips are above and below.
    assert.deepEqual(visibleBox({ x: 10, y: 10, width: 100, height: 50 }, { width: 200, height: 200 }), { x: 10, y: -15, width: 100, height: 100 });
    // A hex beside the box is labelled once the pane's strips count (the user's screenshot of 2026-11-02).
    const index = { names: ["A1"], x: [-60], y: [50] };
    assert.deepEqual(labelsInView(index, { x: 0, y: 0, width: 100, height: 100 }), []);
    assert.deepEqual(labelsInView(index, visibleBox({ x: 0, y: 0, width: 100, height: 100 }, { width: 300, height: 100 })), [0]);
});

test("?tiles=0 turns tiles off from the address or the address the page was opened with", () => {
    assert.equal(tilesOffInAddress(["http://localhost/games/play?game=x&tiles=0", undefined]), true);
    // Replay rewrote its address; the navigation entry still has the switch.
    assert.equal(tilesOffInAddress(["http://localhost/games/replay?game=x&step=4", "http://localhost/games/replay?game=x&tiles=0"]), true);
    assert.equal(tilesOffInAddress(["http://localhost/games/replay?game=x&tiles=1", "http://localhost/"]), false);
    assert.equal(tilesOffInAddress([undefined, "not an address"]), false);
});

// Pan by Transform Plan, task 2. A 200 by 100 pane showing board 0..200 by 0..100 at one pixel a unit, painted half a pane beyond it.
const pane = { width: 200, height: 100 };
const onScreen = { x: 0, y: 0, width: 200, height: 100 };
const painted = committedBox(onScreen);

test("a commit paints the board on screen and half a pane on every side", () => {
    assert.deepEqual(painted, { x: -100, y: -50, width: 400, height: 200 });
});

test("the gesture transform shows the painted drawing as the board now on screen", () => {
    // Unmoved: nothing to do, since the drawing already sits half a pane up and left.
    assert.deepEqual(gestureTransform(painted, onScreen, pane), { k: 1, x: 0, y: 0 });
    // Dragged 20 units right on the board: the drawing moves 20 pixels left.
    assert.deepEqual(gestureTransform(painted, { ...onScreen, x: 20 }, pane), { k: 1, x: -20, y: 0 });
    // Zoomed in twice about board point (100, 50), the pane's centre: that point stays at the centre, pixel (100, 50).
    const zoomed = gestureTransform(painted, { x: 50, y: 25, width: 100, height: 50 }, pane);
    assert.deepEqual(zoomed, { k: 2, x: -200, y: -100 });
    // Board (100, 50) is drawing pixel (200, 100) at the commit; moved, -100 + 200 x 2 - 200 = 100 and -50 + 100 x 2 - 100 = 50.
    assert.deepEqual([-100 + 200 * zoomed.k + zoomed.x, -50 + 100 * zoomed.k + zoomed.y], [100, 50]);
});

test("a gesture repaints near the painted edge, on a zoom out about any point, and past twice the scale", () => {
    assert.equal(needsCommit(painted, onScreen), false);
    // A tenth of the pane is 20 units: a drag of 75 left keeps 25 of margin, one of 85 keeps 15.
    assert.equal(needsCommit(painted, { ...onScreen, x: -75 }), false);
    assert.equal(needsCommit(painted, { ...onScreen, x: -85 }), true);
    // Two wheel steps out (1.5625) about the left edge reach the right edge; the same zoom about the centre stays inside.
    assert.equal(needsCommit(painted, { x: 0, y: -28.125, width: 312.5, height: 156.25 }), true);
    assert.equal(needsCommit(painted, { x: -56.25, y: -28.125, width: 312.5, height: 156.25 }), false);
    assert.equal(needsCommit(painted, { x: 50, y: 25, width: 100, height: 50 }), false);
    assert.equal(needsCommit(painted, { x: 60, y: 30, width: 80, height: 40 }), true);
});

test("the gesture's transform repeats the turned map's own before the move", () => {
    const move = { k: 1.25, x: -50, y: -25 };
    assert.equal(gestureCss(false, move), "translate(-50px, -25px) scale(1.25)");
    assert.equal(gestureCss(true, move), "translateX(100cqw) rotate(90deg) translate(-50cqh, -50cqw) translate(-50px, -25px) scale(1.25)");
});

test("?transform=0 turns pan by transform off from the address or the address the page was opened with", () => {
    assert.equal(transformOffInAddress(["http://localhost/games/play?transform=0", undefined]), true);
    assert.equal(transformOffInAddress(["http://localhost/games/replay?step=3", "http://localhost/games/replay?transform=0"]), true);
    assert.equal(transformOffInAddress(["http://localhost/games/play?tiles=0", "http://localhost/"]), false);
});

// A viewport whose host can pan by transform: a full class list and its own rectangle, sized by the observer.
async function panFixture(rect, hostSize, rotated) {
    const previous = globalThis.ResizeObserver;
    let watch = null;
    globalThis.ResizeObserver = class { constructor(callback) { watch = callback; } observe() {} disconnect() {} };
    try {
        const fixture = viewportFixture();
        const classes = new Set();
        Object.assign(fixture.host, {
            classList: { toggle: (name, on) => (on ?? !classes.has(name)) ? classes.add(name) : classes.delete(name), add: name => classes.add(name),
                remove: name => classes.delete(name), contains: name => classes.has(name) },
            getBoundingClientRect: () => rect,
        });
        // The viewport reads the host when it is made, so it is made again on the full host.
        const viewport = create(fixture.host, { invokeMethodAsync(...args) { fixture.calls.push(args); } });
        const style = new Map();
        fixture.svg.style = { setProperty: (name, value) => style.set(name, value), removeProperty: name => style.delete(name) };
        await viewport.load("render/board/version/exact", "", "0 0 1000 1000", ["grid"], ["grid"]);
        if (rotated) viewport.setRotation(true);
        watch([{ target: fixture.host, contentRect: hostSize }]);
        return { ...fixture, viewport, classes, style };
    } finally {
        globalThis.ResizeObserver = previous;
    }
}

test("panned, a click is read from the host's rectangle and the view, and the drawing paints the margin", async () => {
    const { svg, handlers, calls, classes } = await panFixture({ left: 10, top: 20, right: 210, bottom: 120 }, { width: 200, height: 100 }, false);
    svg.getScreenCTM = () => { throw new Error("the screen matrix lays out the map"); };
    assert.ok(classes.has("pan"));
    // The 1000 by 1000 box in a 200 by 100 pane: 2000 by 1000 on screen, letterboxed, and half a pane more on each side.
    assert.equal(svg.attributes.viewBox, "-1500 -500 4000 2000");
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 85, clientY: 50, target: svg });
    handlers.get("pointerup")({ pointerId: 1, clientX: 85, clientY: 50, target: svg });
    assert.deepEqual(calls.at(-1), ["OnBoardClick", 250, 300]);
});

test("panned and turned, a click is read through the quarter turn", async () => {
    const { svg, handlers, calls } = await panFixture({ left: 10, top: 20, right: 110, bottom: 220 }, { width: 100, height: 200 }, true);
    svg.getScreenCTM = () => { throw new Error("the screen matrix lays out the map"); };
    handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 80, clientY: 95, target: svg });
    handlers.get("pointerup")({ pointerId: 1, clientX: 80, clientY: 95, target: svg });
    assert.deepEqual(calls.at(-1), ["OnBoardClick", 250, 300]);
});

test("a drag moves the painted drawing without a new view box, and its end commits once", async () => {
    const previousFrame = globalThis.requestAnimationFrame;
    const frames = [];
    try {
        const { svg, handlers, classes, style } = await panFixture({ left: 0, top: 0, right: 200, bottom: 100 }, { width: 200, height: 100 }, false);
        globalThis.requestAnimationFrame = callback => frames.push(callback);
        const before = svg.attributes.viewBox;
        handlers.get("pointerdown")({ pointerId: 1, button: 0, clientX: 100, clientY: 50, target: svg });
        handlers.get("pointermove")({ pointerId: 1, clientX: 110, clientY: 50, target: svg });
        frames.shift()();
        assert.equal(svg.attributes.viewBox, before);
        assert.ok(classes.has("gesturing"));
        // Ten pixels of pointer move the drawing ten, on this 2:1 letterbox too: the map follows the pointer (backlog section 96, fixed).
        assert.equal(style.get("transform"), "translate(10px, 0px) scale(1)");
        handlers.get("pointerup")({ pointerId: 1, clientX: 110, clientY: 50, target: svg });
        frames.shift()();
        assert.notEqual(svg.attributes.viewBox, before);
        assert.equal(style.has("transform"), false);
        assert.equal(classes.has("gesturing"), false);
    } finally {
        globalThis.requestAnimationFrame = previousFrame;
    }
});

test("a wheel step scales the painted drawing, and a zoom past twice its scale commits", async () => {
    const previousFrame = globalThis.requestAnimationFrame;
    const frames = [];
    try {
        const { svg, handlers, style } = await panFixture({ left: 0, top: 0, right: 200, bottom: 100 }, { width: 200, height: 100 }, false);
        globalThis.requestAnimationFrame = callback => frames.push(callback);
        const before = svg.attributes.viewBox;
        // About the pane's centre, board (500, 500): one step in is a scale of 1.25 with the centre held.
        handlers.get("wheel")({ deltaY: -100, clientX: 100, clientY: 50, preventDefault() {} });
        frames.shift()();
        assert.equal(svg.attributes.viewBox, before);
        // Board (500, 500) is drawing pixel (200, 100) at the commit; 200 x 1.25 - 50 and 100 x 1.25 - 25 put it back at (200, 100), the
        // pane's centre once the drawing's half-pane offset is taken off.
        assert.equal(style.get("transform"), "translate(-50px, -25px) scale(1.25)");
        // Three more steps: 1.25 ^ 4 = 2.44, past twice the painted scale, so the drawing is painted again.
        for (let step = 0; step < 3; step++) {
            handlers.get("wheel")({ deltaY: -100, clientX: 100, clientY: 50, preventDefault() {} });
            frames.shift()?.();
        }
        assert.notEqual(svg.attributes.viewBox, before);
        assert.equal(style.has("transform"), false);
    } finally {
        globalThis.requestAnimationFrame = previousFrame;
    }
});

test("side by side, pan by transform rests: the drawing is the pane's and the view box is the view", async () => {
    const { svg, viewport, classes } = await panFixture({ left: 0, top: 0, right: 200, bottom: 100 }, { width: 200, height: 100 }, false);
    assert.equal(svg.attributes.viewBox, "-1500 -500 4000 2000");
    // The fixture's styled-less map has no second drawing to make; the switch alone is under test.
    viewport.setComparison("side", 0.5);
    assert.equal(classes.has("pan"), false);
    assert.equal(svg.attributes.viewBox, "0 0 1000 1000");
    viewport.setComparison("none", 0.5);
    assert.equal(classes.has("pan"), true);
    assert.equal(svg.attributes.viewBox, "-1500 -500 4000 2000");
});

test("a host whose height comes from its drawing keeps the drawing in the flow", async () => {
    // The card editor's map: once the drawing is placed absolutely, the host has width and no height.
    const { svg, classes } = await panFixture({ left: 0, top: 0, right: 600, bottom: 0 }, { width: 600, height: 0 }, false);
    assert.equal(classes.has("pan"), false);
    assert.equal(svg.attributes.viewBox, "0 0 1000 1000");
});

test("the two terrain switches choose the tile variant", () => {
    assert.equal(tileVariant(["exact-terrain", "exact-elevation", "grid"]), "te");
    assert.equal(tileVariant(["exact-terrain", "grid"]), "t");
    assert.equal(tileVariant(["exact-elevation"]), "e");
    assert.equal(tileVariant(["grid"]), null);
});

test("the probe hides only the layers it names", () => {
    assert.deepEqual(layersAfterHide(["exact-tiles", "grid", "labels"], ["grid"]), ["exact-tiles", "labels"]);
    assert.deepEqual(layersAfterHide(["grid"], []), ["grid"]);
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
