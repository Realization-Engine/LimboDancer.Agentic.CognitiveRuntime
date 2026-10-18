"""Renders the figure parts composed as the renderer composes them (Glyphs.ArtFigures), for every side with kit and every pose, at a large
size and at the far detail level's size, into one PNG; for looking at the drawing between builds.

Run from the repository root:  python code-builders/unit-figures/preview_figures.py <out.png> [height in pixels of the small row]
Needs PyMuPDF (fitz).
"""

import json
import math
import sys
from pathlib import Path

import fitz

ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "src" / "ASL" / "units" / "figures" / "figures.json"
INK = {"german": "#1d2a36", "russian": "#5a2a1a"}


def figure(art, pose_name, side, x, y, height, color):
    pose = art["poses"][pose_name]
    scale = height / 1000
    parts = [f'<g transform="translate({x} {y}) scale({scale})" fill="{color}">', f'<path d="{pose["path"]}"/>']
    head = pose["head"]
    if side in art["helmets"]:
        parts.append(f'<path d="{art["helmets"][side]["path"]}" transform="translate({head["x"]} {head["y"]}) scale({head["radius"] / 100})"/>')
    grip = pose.get("grip")
    if grip and pose.get("weapon") in art["weapons"].get(side, {}):
        angle = math.degrees(math.atan2(grip["y2"] - grip["y1"], grip["x2"] - grip["x1"]))
        parts.append(f'<path d="{art["weapons"][side][pose["weapon"]]["path"]}" transform="translate({grip["x1"]} {grip["y1"]}) rotate({angle})"/>')
    parts.append("</g>")
    return "".join(parts), pose["width"] * scale


def main():
    out = Path(sys.argv[1])
    small = float(sys.argv[2]) if len(sys.argv) > 2 else 12
    art = json.loads(MANIFEST.read_text(encoding="utf-8"))
    sides = sorted(art["helmets"])
    poses = list(art["poses"])
    rows, y, width = [], 10, 0
    for side in sides:
        x = 10
        for pose in poses:
            markup, w = figure(art, pose, side, x, y, 160, INK.get(side, "#222"))
            rows.append(markup)
            x += w + 30
        width = max(width, x)
        y += 180
        x = 10
        for pose in poses:
            for count in range(3):
                markup, w = figure(art, pose, side, x + count * small * 0.6 * 0.78, y, small, INK.get(side, "#222"))
                rows.append(markup)
            x += small * 2.2 + 12
        y += small + 20
    svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{y}"><rect width="100%" height="100%" fill="#e9eef3"/>{"".join(rows)}</svg>'
    document = fitz.open(stream=svg.encode("utf-8"), filetype="svg")
    document[0].get_pixmap(dpi=96).save(out)
    print(f"wrote {out}")


if __name__ == "__main__":
    main()
