"""Draws the Studio's eight toolbar and inspector-tab icons as SVG, and the page that previews them.

Writes one .svg a icon and "Studio Icon Preview.html" to src/ASL/docs/Designs/Studio Icons. Each icon is drawn on a
24-unit grid; its colors are CSS variables (--i-ink, --i-olive, ...) with the Midjourney palette as fallbacks, so
the same markup takes the Studio's tokens where a page sets them. Hexes are flat-topped, as on the boards.

    python code-builders/studio-icons/make_icons.py
"""
import math
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "src" / "ASL" / "docs" / "Designs" / "Studio Icons"

# The Midjourney palette: the fallback of every color variable.
MJ = {
    "ink": "#203446", "olive": "#788848", "sage": "#7c9c7c", "sage-pale": "#dfe6d9", "red": "#a04030",
    "lens": "#cbd6de", "page": "#ebe2c9", "eye": "#fbf8f0",
}
# The Studio's own tokens (site.css) for the same roles. The Studio has no mid green, so sage keeps the Midjourney
# value; it would be a new token.
STUDIO = {
    "ink": "#1b2025", "olive": "#4a6629", "sage": "#7c9c7c", "sage-pale": "#dce5c4", "red": "#8e2e1f",
    "lens": "#dce7f2", "page": "#f6ecd6", "eye": "#ffffff",
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


def palette_css(name, colors):
    return f".{name} {{ " + " ".join(f"--i-{k}: {v};" for k, v in colors.items()) + " }"


def sized(markup, px):
    return markup.replace('width="24" height="24"', f'width="{px}" height="{px}"')


def page():
    sizes = [16, 20, 24, 32, 96]
    head_cells = "".join(f"<th>{s} px</th>" for s in sizes)
    rows = ""
    for _, label, where, markup in ICONS:
        cells = "".join(f'<td class="cell">{sized(markup, s)}</td>' for s in sizes)
        rows += f"<tr><th scope=\"row\">{label}<span>{where}</span></th>{cells}</tr>\n"
    toolbar = "".join(
        f'<button type="button" class="tool" title="{label}" aria-label="{label}"{" aria-pressed=\"false\"" if key == "rotate" else ""}>{sized(m, 20)}</button>'
        for key, label, where, m in ICONS if where == "Map toolbar")
    tabs = "".join(
        f'<button type="button" role="tab" class="tab{" active" if key == "selection" else ""}" title="{label}" aria-label="{label}" aria-selected="{"true" if key == "selection" else "false"}">{sized(m, 20)}</button>'
        for key, label, where, m in ICONS if where == "Inspector tab")
    tabs = '<button type="button" role="tab" class="tab text" aria-selected="false">Proposal</button>' + tabs
    return f"""<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Studio Icon Preview</title>
<style>
:root {{ --ground: #f3f1ea; --surface: #ffffff; --ink: #1b2025; --muted: #5e656c; --line: #dad5c8; --control-line: #8f897a; --accent: #4a6629; --accent-tint: #dce5c4; }}
{palette_css("mj", MJ)}
{palette_css("studio", STUDIO)}
body {{ margin: 0; padding: 24px 16px 48px; background: var(--ground); color: var(--ink); font: 15px/1.45 "IBM Plex Sans", "Segoe UI", system-ui, sans-serif; }}
main {{ max-width: 1100px; margin: 0 auto; }}
h1 {{ font-size: 1.4rem; margin: 0 0 4px; }}
h2 {{ font-size: 1.05rem; margin: 28px 0 8px; }}
p {{ margin: 4px 0 10px; color: var(--muted); max-width: 70ch; }}
.palettes {{ display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 16px; }}
.panel {{ background: var(--surface); border: 1px solid var(--line); border-radius: 6px; padding: 12px; overflow-x: auto; }}
table {{ border-collapse: collapse; }}
th, td {{ padding: 6px 10px; text-align: center; vertical-align: middle; }}
thead th {{ font-size: 12px; color: var(--muted); font-weight: 600; }}
tbody th {{ text-align: left; font-weight: 600; white-space: nowrap; }}
tbody th span {{ display: block; font-weight: 400; font-size: 12px; color: var(--muted); }}
tbody tr + tr td, tbody tr + tr th {{ border-top: 1px solid var(--line); }}
.cell svg {{ display: block; margin: 0 auto; }}
.bar {{ display: flex; flex-wrap: wrap; gap: 16px; align-items: center; }}
.toolbar {{ display: flex; gap: 4px; }}
.tool {{ display: inline-flex; align-items: center; justify-content: center; width: 32px; height: 32px; padding: 0; border: 1px solid var(--control-line); border-radius: 6px; background: var(--surface); cursor: pointer; }}
.tool:hover {{ background: var(--accent-tint); }}
.tool[aria-pressed="true"] {{ background: var(--accent-tint); border-color: var(--accent); }}
.tabs {{ display: flex; border-bottom: 1px solid var(--line); }}
.tab {{ display: inline-flex; align-items: center; justify-content: center; min-width: 36px; height: 34px; padding: 0 8px; border: 0; border-bottom: 2px solid transparent; background: none; cursor: pointer; font: inherit; font-size: 13px; color: var(--muted); }}
.tab.active {{ border-bottom-color: var(--accent); color: var(--ink); }}
.tab:hover {{ background: var(--ground); }}
.label {{ font-size: 12px; color: var(--muted); min-width: 9em; }}
</style>
</head>
<body>
<main>
<h1>Studio icon preview</h1>
<p>Eight icons for the map toolbar and the inspector tabs, drawn from the chosen Midjourney references. They are not in the Studio yet. Each is a 24-unit SVG whose colors are variables, shown here in two palettes: the Midjourney palette, and the Studio's own tokens from site.css.</p>

<h2>In place</h2>
<p>The toolbar and the tab strip as they would look, with the label moved to the tooltip and the accessible name. Click Rotate map to see its pressed state; the tabs switch on click.</p>
<div class="palettes">
  <div class="panel mj"><div class="bar"><span class="label">Midjourney palette</span><div class="toolbar">{toolbar}</div></div><div class="tabs" role="tablist" style="margin-top:12px">{tabs}</div></div>
  <div class="panel studio"><div class="bar"><span class="label">Studio tokens</span><div class="toolbar">{toolbar}</div></div><div class="tabs" role="tablist" style="margin-top:12px">{tabs}</div></div>
</div>

<h2>Sizes, Midjourney palette</h2>
<div class="panel mj"><table><thead><tr><th></th>{head_cells}</tr></thead><tbody>
{rows}</tbody></table></div>

<h2>Sizes, Studio tokens</h2>
<div class="panel studio"><table><thead><tr><th></th>{head_cells}</tr></thead><tbody>
{rows}</tbody></table></div>

<h2>Pixels, magnified</h2>
<p>Each icon as the screen draws it at 16, 20, and 24 px on a 1x display, enlarged six times so the pixels show. This is the honest test of what survives at tab size. Midjourney palette.</p>
<div class="panel" id="pixels"></div>
</main>
<script>
const pixels = document.getElementById('pixels');
const svgs = [...document.querySelectorAll('.panel.mj tbody tr')].map(r => r.querySelector('td').innerHTML);
for (const px of [16, 20, 24]) {{
  const row = document.createElement('div');
  row.className = 'bar';
  row.style.margin = '8px 0';
  row.innerHTML = `<span class="label">${{px}} px</span>`;
  pixels.appendChild(row);
  for (const markup of svgs) {{
    const canvas = document.createElement('canvas');
    canvas.width = px; canvas.height = px;
    canvas.style.width = canvas.style.height = (px * 6) + 'px';
    canvas.style.imageRendering = 'pixelated';
    canvas.style.background = '#ffffff';
    canvas.style.border = '1px solid var(--line)';
    row.appendChild(canvas);
    const img = new Image();
    img.onload = () => canvas.getContext('2d').drawImage(img, 0, 0, px, px);
    img.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(markup.replace(/width="\\d+" height="\\d+"/, `width="${{px}}" height="${{px}}"`));
  }}
}}
document.querySelectorAll('.tool[aria-pressed]').forEach(b => b.addEventListener('click', () => b.setAttribute('aria-pressed', b.getAttribute('aria-pressed') === 'true' ? 'false' : 'true')));
document.querySelectorAll('.tabs').forEach(list => list.addEventListener('click', e => {{
  const t = e.target.closest('.tab'); if (!t) return;
  list.querySelectorAll('.tab').forEach(x => {{ x.classList.toggle('active', x === t); x.setAttribute('aria-selected', x === t ? 'true' : 'false'); }});
}}));
</script>
</body>
</html>
"""


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    for key, _, _, markup in ICONS:
        (OUT / f"{key}.svg").write_text(markup + "\n", encoding="utf-8", newline="\n")
    (OUT / "Studio Icon Preview.html").write_text(page(), encoding="utf-8", newline="\n")
    print(f"wrote {len(ICONS)} icons and the preview to {OUT}")


if __name__ == "__main__":
    main()
