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
    "lens": "#cbd6de", "page": "#ebe2c9", "eye": "#fbf8f0",
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


ICONS = [
    ("zoom-in", "Zoom in", "Map toolbar", zoom("+")),
    ("zoom-out", "Zoom out", "Map toolbar", zoom("-")),
    ("fit", "Fit", "Map toolbar", fit()),
    ("rotate", "Rotate map", "Map toolbar", rotate()),
    ("selection", "Selection", "Inspector tab", selection()),
    ("los", "LOS", "Inspector tab", los()),
    ("range", "Range", "Inspector tab", range_()),
    ("evidence", "Evidence", "Inspector tab", evidence()),
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