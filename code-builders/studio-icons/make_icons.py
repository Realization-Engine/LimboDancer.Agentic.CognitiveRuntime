"""Draws the Studio's toolbar and inspector-tab icons as SVG, for the Icon Lab and the pages that use them.

Writes one .svg a icon and the manifest icons.json to the Studio's wwwroot/icons. Each icon is drawn on a 24-unit
grid; its colors are CSS variables (--i-ink, --i-olive, ...) with the Midjourney palette as fallbacks, so the same
markup takes whichever icon palette site.css sets around it. Hexes are flat-topped, as on the boards.

    python code-builders/studio-icons/make_icons.py
"""
import json
import math
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "src" / "ASL" / "LimboDancer.Domains.Asl.MapStudio" / "wwwroot" / "icons"

# The Midjourney palette: the fallback of every color variable. site.css repeats it as .icon-palette-mj.
MJ = {
    "ink": "#203446", "olive": "#788848", "sage": "#7c9c7c", "red": "#a04030",
    "lens": "#cbd6de", "page": "#ebe2c9", "eye": "#fbf8f0", "paper": "#f2ecdc",
}


def c(role):
    return f"var(--i-{role}, {MJ[role]})"


def f(v):
    return f"{v:.2f}".rstrip("0").rstrip(".")


def hex_points(cx, cy, r):
    return " ".join(f"{f(cx + r * math.cos(math.radians(a)))},{f(cy + r * math.sin(math.radians(a)))}" for a in range(0, 360, 60))


def hexagon(cx, cy, r, fill, stroke=None, width=0):
    s = f' stroke="{stroke}" stroke-width="{f(width)}" stroke-linejoin="round"' if stroke else ""
    return f'<polygon points="{hex_points(cx, cy, r)}" fill="{fill}"{s}/>'


def cluster(cx, cy, spacing, r, fills):
    """Seven flat-topped hexes: the center, then its six neighbors clockwise from the upper right."""
    offsets = [(0, 0), (1.5, -0.866), (1.5, 0.866), (0, 1.732), (-1.5, 0.866), (-1.5, -0.866), (0, -1.732)]
    return "".join(hexagon(cx + dx * spacing, cy + dy * spacing, r, fills[i]) for i, (dx, dy) in enumerate(offsets))


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" width="24" height="24" aria-hidden="true" focusable="false">{body}</svg>'


def zoom(sign):
    ink = c("ink")
    bar = f'<path d="M7 10H13{"M10 7V13" if sign == "+" else ""}" stroke="{ink}" stroke-width="2.2" stroke-linecap="round"/>'
    return svg(
        f'<circle cx="10" cy="10" r="6.6" fill="{c("lens")}" stroke="{ink}" stroke-width="2.2"/>'
        f'<path d="M15.6 15.6L20.8 20.8" stroke="{ink}" stroke-width="3.2" stroke-linecap="round"/>' + bar)


def fit():
    olive = c("olive")
    return svg(
        f'<path d="M3 8V3H8M16 3H21V8M21 16V21H16M8 21H3V16" fill="none" stroke="{c("ink")}" stroke-width="2.2"/>'
        + cluster(12, 12, 2.2, 1.9, [c("sage")] + [olive] * 6))


def rotate():
    olive = c("olive")
    ink = c("ink")
    cx = cy = 12.0
    radius = 8.8

    def at(deg, rad=radius):
        a = math.radians(deg)
        return cx + rad * math.cos(a), cy + rad * math.sin(a)

    sx, sy = at(190)
    ex, ey = at(14)
    # The arrowhead: its base across the circle at 12 degrees, its tip on the circle at 34; clockwise on screen.
    b1, b2 = at(12, radius - 2.4), at(12, radius + 2.4)
    tip = at(34)
    head = f'<polygon points="{f(b1[0])},{f(b1[1])} {f(b2[0])},{f(b2[1])} {f(tip[0])},{f(tip[1])}" fill="{ink}" stroke="{ink}" stroke-width="0.6" stroke-linejoin="round"/>'
    return svg(
        cluster(cx, cy, 2.0, 1.72, [olive, c("red")] + [olive] * 5)
        + f'<path d="M{f(sx)} {f(sy)}A{f(radius)} {f(radius)} 0 1 1 {f(ex)} {f(ey)}" fill="none" stroke="{ink}" stroke-width="2.2"/>'
        + head)


def play():
    """A squad counter: an olive-framed counter whose off-white face bears a WWII infantryman advancing with his rifle, white piping
    between his parts (Play, with piping, 2026-10-17-5). The figure is traced from the chosen image by trace_figure.py: the whole figure
    is laid in white, and the figure, cut into parts by its piping, in ink over it, so the piping shows white on the off-white face."""
    whole, parts = (REPO / "code-builders" / "studio-icons" / "traced" / "play-figure.txt").read_text(encoding="utf-8").splitlines()
    return svg(
        f'<rect x="2.6" y="2.6" width="18.8" height="18.8" rx="3.2" fill="{c("paper")}" stroke="{c("olive")}" stroke-width="2.2"/>'
        # The image's frame is thinner than the icon's, so the figure is drawn 10% smaller about the centre and clears the frame.
        '<g transform="translate(12 12) scale(0.9) translate(-12 -12)">'
        f'<path d="{whole}" fill="#ffffff"/>'
        f'<path d="{parts}" fill="{c("ink")}" fill-rule="evenodd"/>'
        '</g>')


def pips(points, color, radius=0.6):
    return "".join(f'<circle cx="{f(x)}" cy="{f(y)}" r="{f(radius)}" fill="{color}"/>' for x, y in points)


def records():
    """Two ruled record sheets with a pair of black dice on the front sheet's corner (Inspector tabs, second set, top left)."""
    ink, sheet = c("ink"), c("eye")
    lines = "".join(f"M9.6 {y}H17.6" for y in (9.6, 11.8, 14.0))
    return svg(
        f'<rect x="4.4" y="3.6" width="12" height="13.4" rx="0.6" fill="{sheet}" stroke="{ink}" stroke-width="1.2"/>'
        f'<rect x="7.4" y="6.4" width="12.2" height="13.4" rx="0.6" fill="{sheet}" stroke="{ink}" stroke-width="1.2"/>'
        f'<path d="{lines}" stroke="{ink}" stroke-width="1"/>'
        f'<rect x="11.2" y="14.6" width="5.6" height="5.6" rx="1.1" fill="{ink}" stroke="{sheet}" stroke-width="0.6"/>'
        + pips([(12.6, 16), (14, 17.4), (15.4, 18.8)], sheet, 0.5)
        + f'<rect x="15.6" y="16.6" width="5" height="5" rx="1" fill="{ink}" stroke="{sheet}" stroke-width="0.6"/>'
        + pips([(16.9, 17.9), (19.3, 20.3)], sheet, 0.5))


def units():
    """A stack of three olive counters, the top one bearing the Play soldier (Inspector tabs, second set, top right): the plural of the
    Play counter, as ASL stacks its units in a hex."""
    olive = c("olive")
    face = c("eye")
    body = ""
    for offset in (6.0, 3.0, 0.0):
        top = [(12, 4.4 + offset), (20, 8.2 + offset), (12, 12 + offset), (4, 8.2 + offset)]
        side = [(4, 8.2 + offset), (12, 12 + offset), (20, 8.2 + offset), (20, 10.4 + offset), (12, 14.2 + offset), (4, 10.4 + offset)]
        body += (f'<polygon points="{" ".join(f"{f(x)},{f(y)}" for x, y in side)}" fill="{olive}" stroke="{face}" stroke-width="0.4" stroke-linejoin="round"/>'
                 f'<polygon points="{" ".join(f"{f(x)},{f(y)}" for x, y in side)}" fill="{c("ink")}" fill-opacity="0.35"/>'
                 f'<polygon points="{" ".join(f"{f(x)},{f(y)}" for x, y in top)}" fill="{olive}" stroke="{face}" stroke-width="0.4" stroke-linejoin="round"/>')
    _, figure = (REPO / "code-builders" / "studio-icons" / "traced" / "play-figure.txt").read_text(encoding="utf-8").splitlines()
    body += f'<g transform="translate(12 7.6) scale(0.27) translate(-12 -12)"><path d="{figure}" fill="{c("ink")}" fill-rule="evenodd"/></g>'
    return svg(body)


def scripted_dice():
    """A white die and a red die, ASL's two, before a small ruled list (Inspector tabs, second set, bottom left)."""
    ink, white, red = c("ink"), c("eye"), c("red")
    lines = "".join(f"M13.6 {y}H19.4" for y in (6.6, 8.6, 10.6))
    shade = f'fill="{ink}" fill-opacity="0.3"'
    return svg(
        f'<rect x="12.2" y="3.8" width="8.6" height="9.6" rx="0.5" fill="{white}" stroke="{ink}" stroke-width="1"/>'
        f'<path d="{lines}" stroke="{ink}" stroke-width="0.9"/>'
        # The red die, behind and to the right.
        f'<polygon points="11.4,11.8 13.2,10 20.2,10 18.4,11.8" fill="{red}" stroke="{ink}" stroke-width="0.8" stroke-linejoin="round"/>'
        f'<polygon points="18.4,11.8 20.2,10 20.2,17 18.4,18.8" fill="{red}" stroke="{ink}" stroke-width="0.8" stroke-linejoin="round"/>'
        f'<polygon points="18.4,11.8 20.2,10 20.2,17 18.4,18.8" {shade}/>'
        f'<rect x="11.4" y="11.8" width="7" height="7" fill="{red}" stroke="{ink}" stroke-width="0.8"/>'
        + pips([(13.3, 13.7), (16.5, 16.9)], white)
        # The white die, in front and to the left.
        + f'<polygon points="3.6,12.6 5.4,10.8 12.4,10.8 10.6,12.6" fill="{white}" stroke="{ink}" stroke-width="0.8" stroke-linejoin="round"/>'
        f'<polygon points="10.6,12.6 12.4,10.8 12.4,17.8 10.6,19.6" fill="{white}" stroke="{ink}" stroke-width="0.8" stroke-linejoin="round"/>'
        f'<polygon points="10.6,12.6 12.4,10.8 12.4,17.8 10.6,19.6" fill="{ink}" fill-opacity="0.15"/>'
        f'<rect x="3.6" y="12.6" width="7" height="7" fill="{white}" stroke="{ink}" stroke-width="0.8"/>'
        + pips([(5.4, 14.4), (7.1, 16.1), (8.8, 17.8)], ink))


def standing():
    """A red pennant on a dark pole planted on an olive tile: who holds what, as the Victory Conditions stand (Inspector tabs, second
    set, bottom right)."""
    ink = c("ink")
    return svg(
        f'<polygon points="4.6,19.2 12,16 19.4,19.2 12,22.4" fill="{c("olive")}"/>'
        f'<polygon points="4.6,19.2 12,22.4 12,23.2 4.6,20" fill="{ink}" fill-opacity="0.35"/>'
        f'<polygon points="12,22.4 19.4,19.2 19.4,20 12,23.2" fill="{ink}" fill-opacity="0.2"/>'
        f'<rect x="11.1" y="2.8" width="1.8" height="16.8" rx="0.4" fill="{ink}"/>'
        f'<polygon points="12.9,3.2 21,6.2 12.9,9.2" fill="{c("red")}"/>')

def step():
    """A turn record track with a marker: four square record boxes, the third filled olive with a red diamond resting on it, the moment
    of the game the replay is on (Step, 2026-10-17-7, the user's pick)."""
    ink = c("ink")
    # Large boxes and thick lines, so the track holds its own beside the solid icons of the strip at 24 px.
    size, gap, top, line = 5.0, 0.35, 12.0, 1.7
    boxes = ""
    for index in range(4):
        x = 1.5 + index * (size + gap)
        fill = c("olive") if index == 2 else "none"
        boxes += f'<rect x="{f(x + line / 2)}" y="{f(top + line / 2)}" width="{f(size - line)}" height="{f(size - line)}" fill="{fill}" stroke="{ink}" stroke-width="{f(line)}"/>'
    centre = 1.5 + 2 * (size + gap) + size / 2
    marker = f'<polygon points="{f(centre)},{f(top - 4.6)} {f(centre + 2.3)},{f(top - 2.3)} {f(centre)},{f(top)} {f(centre - 2.3)},{f(top - 2.3)}" fill="{c("red")}"/>'
    return svg(boxes + marker)

def proposal():
    """A clipboard with one ticked box and a pencil across its corner: an order proposed, not yet confirmed (Proposal, one icon,
    top row 3rd)."""
    ink = c("ink")
    # The pencil runs up and to the right at 45 degrees: its tip at the lower left, the red eraser at the upper right.
    along = (1 / math.sqrt(2), -1 / math.sqrt(2))
    across = (1 / math.sqrt(2), 1 / math.sqrt(2))
    half = 1.6
    start, end, eraser = (12.6, 16.4), (18.4, 10.6), (20.0, 9.0)
    tip = (start[0] - along[0] * 3, start[1] - along[1] * 3)
    base = [(start[0] + across[0] * half, start[1] + across[1] * half), (start[0] - across[0] * half, start[1] - across[1] * half)]
    point = " ".join(f"{f(x)},{f(y)}" for x, y in [tip, *base])
    return svg(
        f'<rect x="4" y="4" width="13" height="17" rx="0.6" fill="{c("eye")}" stroke="{ink}" stroke-width="2"/>'
        f'<rect x="7.8" y="2.6" width="5.4" height="2.8" rx="0.7" fill="{c("olive")}"/>'
        f'<circle cx="10.5" cy="2.7" r="1" fill="{c("olive")}"/>'
        f'<rect x="6.4" y="7.4" width="5.2" height="5.2" fill="none" stroke="{ink}" stroke-width="1.4"/>'
        f'<path d="M7.6 10.1L8.9 11.3L10.6 8.9" fill="none" stroke="{ink}" stroke-width="1.3" stroke-linecap="round" stroke-linejoin="round"/>'
        f'<path d="M{f(start[0])} {f(start[1])}L{f(end[0])} {f(end[1])}" stroke="{c("olive")}" stroke-width="{f(half * 2)}"/>'
        f'<path d="M{f(end[0])} {f(end[1])}L{f(end[0] + 0.7)} {f(end[1] - 0.7)}" stroke="{ink}" stroke-width="{f(half * 2)}"/>'
        f'<path d="M{f(end[0] + 0.7)} {f(end[1] - 0.7)}L{f(eraser[0])} {f(eraser[1])}" stroke="{c("red")}" stroke-width="{f(half * 2)}"/>'
        f'<polygon points="{point}" fill="{ink}"/>')


def selection():
    pointer = [(0, 0), (0, 13), (3.3, 10), (5.6, 15), (8, 14), (5.7, 9.1), (10, 9.1)]
    scale, tx, ty = 0.74, 12.4, 11.2
    pts = " ".join(f"{f(tx + x * scale)},{f(ty + y * scale)}" for x, y in pointer)
    ink = c("ink")
    return svg(
        hexagon(10, 10.5, 8, c("olive"))
        + f'<polygon points="{pts}" fill="{ink}" stroke="{ink}" stroke-width="0.8" stroke-linejoin="round"/>')


def los():
    ink = c("ink")
    # Three dashes that fill the sight line exactly, so no sliver of a fourth shows at its end.
    length = math.hypot(18.2 - 15.3, 15.9 - 9.4)
    gap = 1.3
    dash = (length - 2 * gap) / 3
    return svg(
        f'<path d="M1.5 7.5Q8 0 14.5 7.5Q8 15 1.5 7.5Z" fill="{c("eye")}" stroke="{ink}" stroke-width="1.9" stroke-linejoin="round"/>'
        f'<circle cx="8" cy="7.5" r="2.9" fill="{ink}"/>'
        f'<circle cx="8" cy="7.5" r="1.3" fill="{c("red")}"/>'
        f'<path d="M15.3 9.4L18.2 15.9" stroke="{ink}" stroke-width="2" stroke-dasharray="{f(dash)} {f(gap)}"/>'
        + hexagon(18.6, 18.8, 3.2, c("olive")))


def range_():
    big = 3.23
    xs = [1.5 + big + i * 1.5 * big for i in range(4)]
    ys = [10 - 0.433 * big if i % 2 == 0 else 10 + 0.433 * big for i in range(4)]
    body = ""
    for i in range(4):
        # The two ends olive, the hexes counted between them sage: an outline alone washes out at 16 px.
        body += hexagon(xs[i], ys[i], big - 0.3, c("olive") if i in (0, 3) else c("sage"))
    body += f'<path d="M2.5 16V19.5H21.5V16" fill="none" stroke="{c("ink")}" stroke-width="2"/>'
    return svg(body)


def evidence():
    ink = c("ink")
    page = c("page")
    bars = "".join(f"M4.6 {y}H9.6M14.4 {y}H19.4" for y in (10, 12.8, 15.6))
    return svg(
        f'<path d="M12 7C9 5.2 5.5 5 2 6V19C5.5 18 9 18.2 12 20Z" fill="{page}" stroke="{ink}" stroke-width="1.8" stroke-linejoin="round"/>'
        f'<path d="M12 7C15 5.2 18.5 5 22 6V19C18.5 18 15 18.2 12 20Z" fill="{page}" stroke="{ink}" stroke-width="1.8" stroke-linejoin="round"/>'
        f'<path d="{bars}" stroke="{ink}" stroke-width="1.5"/>'
        f'<polygon points="15,17.4 17.8,17.4 17.8,23 16.4,21.5 15,23" fill="{c("red")}"/>')


def layers():
    """Three flat-topped hexes seen at a slant, stacked: the board's layers, one over another, the top one the page they show."""
    ink = c("ink")

    def slab(cy, fill):
        rx, ry = 9.6, 4.2
        pts = [(12 + rx, cy), (12 + rx / 2, cy + ry), (12 - rx / 2, cy + ry), (12 - rx, cy), (12 - rx / 2, cy - ry), (12 + rx / 2, cy - ry)]
        points = " ".join(f"{f(x)},{f(y)}" for x, y in pts)
        return f'<polygon points="{points}" fill="{fill}" stroke="{ink}" stroke-width="1.6" stroke-linejoin="round"/>'

    return svg(slab(17.2, c("sage")) + slab(12.4, c("olive")) + slab(7.6, c("page")))


ICONS = [
    ("zoom-in", "Zoom in", "Map toolbar", zoom("+")),
    ("zoom-out", "Zoom out", "Map toolbar", zoom("-")),
    ("fit", "Fit", "Map toolbar", fit()),
    ("rotate", "Rotate map", "Map toolbar", rotate()),
    ("play", "Play", "Inspector tab", play()),
    ("proposal", "Proposal", "Inspector tab", proposal()),
    ("records", "Records", "Inspector tab", records()),
    ("units", "Units", "Inspector tab", units()),
    ("scripted-dice", "Scripted dice", "Inspector tab", scripted_dice()),
    ("standing", "Standing", "Inspector tab", standing()),
    ("step", "Step", "Inspector tab", step()),
    ("selection", "Selection", "Inspector tab", selection()),
    ("los", "LOS", "Inspector tab", los()),
    ("range", "Range", "Inspector tab", range_()),
    ("evidence", "Evidence", "Inspector tab", evidence()),
    ("layers", "Layers", "Inspector tab", layers()),
]

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    manifest = []
    for key, label, group, markup in ICONS:
        (OUT / f"{key}.svg").write_text(markup + "\n", encoding="utf-8", newline="\n")
        manifest.append({"key": key, "label": label, "group": group})
    (OUT / "icons.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")
    print(f"wrote {len(ICONS)} icons and icons.json to {OUT}")


if __name__ == "__main__":
    main()