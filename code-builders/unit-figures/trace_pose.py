"""Traces the user's Midjourney pick of a body pose into a pose source, src/ASL/units/figures/poses/<pose>.svg (ASL Unit Figure Art Plan,
sections 3 and 5).

The image is a black silhouette on white, one figure, or a 2x2 grid of four (Midjourney's set or its four tries). The figure is cut out,
traced outline by outline with the icon tracer's edge-following and simplification (code-builders/studio-icons/trace_figure.py), and
scaled to body units: an upright figure's height becomes 1000, a lying figure's length becomes 1150 (a man prone with his arms forward).
The head's center and radius are estimated from the top of an upright figure; a lying figure's head, and every figure's grip, are given
by hand after looking at the traced figure (--head, --grip). The pose is a stand-in until --accept, which marks the user's acceptance.

    python code-builders/unit-figures/trace_pose.py <image> <pose> [--cell 1-4] [--crop x,y,w,h] [--mirror] [--lying]
        [--head cx,cy,r] [--grip x1,y1,x2,y2] [--weapon rifle|smg] [--source "batch 2026-10-18-1, top right"] [--accept]
        [--tolerance 2.0] [--close pixels] [--bare] [--bare-width radii] [--bare-back radii] [--frame x0,y0,x1,y1] [--own-helmet] [--body x0,x1] [--out file]
Then:  python code-builders/unit-figures/build_figures.py
"""
import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "studio-icons"))
from trace_figure import area, closed, loops, simplify  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
POSES = ROOT / "src" / "ASL" / "units" / "figures" / "poses"
NAMES = ["standing", "crew", "leader", "hero", "wounded", "broken", "pinned", "berserk", "prisoner", "vehicle-crew"]
ENLARGE = 3
DARK = 110
LYING_LENGTH = 1150


def numbers(text, count, name):
    values = [float(part) for part in text.split(",")]
    if len(values) != count:
        raise SystemExit(f"--{name} takes {count} numbers")
    return values


def head_numbers(text):
    """The head anchor: its center and radius in body units, and how far it is tilted, in degrees clockwise (a bowed head is
    positive); the tilt may be left out."""
    values = [float(part) for part in text.split(",")]
    if len(values) not in (3, 4):
        raise SystemExit("--head takes cx,cy,r or cx,cy,r,tilt")
    return (*values[:3], values[3] if len(values) == 4 else 0.0)


def cut(image, cell, crop):
    if crop:
        x, y, w, h = (int(v) for v in numbers(crop, 4, "crop"))
        return image.crop((x, y, x + w, y + h))
    if cell:
        half_w, half_h = image.width // 2, image.height // 2
        column, row = (cell - 1) % 2, (cell - 1) // 2
        return image.crop((column * half_w, row * half_h, (column + 1) * half_w, (row + 1) * half_h))
    return image


def head_of(mask, top, bottom):
    """The head of an upright figure: the dark pixels of the top tenth, their middle, and half their widest run."""
    height = bottom - top
    band = mask[top:top + max(1, int(height * 0.12))]
    ys, xs = np.nonzero(band)
    widths = [int(np.ptp(row.nonzero()[0])) + 1 for row in band if row.any()]
    radius = max(widths) / 2
    return xs.mean(), top + radius * 1.05, radius


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("image")
    parser.add_argument("pose", choices=NAMES)
    parser.add_argument("--cell", type=int, choices=[1, 2, 3, 4])
    parser.add_argument("--crop")
    parser.add_argument("--mirror", action="store_true")
    parser.add_argument("--lying", action="store_true")
    parser.add_argument("--head")
    parser.add_argument("--grip")
    parser.add_argument("--weapon", choices=["rifle", "smg"])
    parser.add_argument("--source", default="")
    parser.add_argument("--accept", action="store_true")
    parser.add_argument("--tolerance", type=float, default=2.0)
    parser.add_argument("--body", help="x0,x1 in body units: where the body lies across the pose, apart from a weapon trailed beyond it; a file of men is spaced by it")
    parser.add_argument("--own-helmet", action="store_true",
                        help="keep the drawn helmet and draw no side's helmet on it, for a pose whose arm or weapon lies over the helmet")
    parser.add_argument("--bare", action="store_true",
                        help="remove the drawn helmet: everything above the head anchor around the head, leaving a skull for the side's helmet")
    parser.add_argument("--bare-width", type=float, default=2.0,
                        help="how many head radii to each side of the head --bare clears; less when a slung weapon rises close behind the head")
    parser.add_argument("--bare-back", type=float, default=0.3,
                        help="how many head radii below the head anchor --bare clears behind the head, where a helmet's rim hangs lower")
    parser.add_argument("--frame", help="x0,y0,x1,y1 in source pixels: scale from this box, not the figure's own, so variants of one pose (the same body with a side's grenade) come out alike")
    parser.add_argument("--close", type=int, default=0,
                        help="fill light gaps up to twice this many source pixels wide (the thin lines between an arm and the body)")
    parser.add_argument("--out", help="write here instead of the pose source, to try a trace")
    args = parser.parse_args()

    picture = cut(Image.open(args.image).convert("L"), args.cell, args.crop)
    if args.mirror:
        picture = picture.transpose(Image.FLIP_LEFT_RIGHT)
    picture = picture.resize((picture.width * ENLARGE, picture.height * ENLARGE), Image.LANCZOS)
    mask = np.asarray(picture) < DARK
    if args.close:
        # A thin light line inside the figure would trace as a hole that turns to specks at counter size; it is filled.
        mask = closed(mask, args.close * ENLARGE)

    traced = []
    for loop in loops(mask):
        if len(loop) >= 8 * ENLARGE and len(simple := simplify(loop, args.tolerance * ENLARGE)) >= 3:
            traced.append((area(simple), simple))
    outlines = [points for turn, points in traced if turn > 0]
    if not outlines:
        raise SystemExit("no figure found: is it dark on light?")
    # Specks apart from the figure (Midjourney's noise) are dropped: an outline under a hundredth of the largest one's area.
    largest = max(abs(turn) for turn, _ in traced)
    kept = [(turn, points) for turn, points in traced if abs(turn) >= largest / 100]
    xs = [x for turn, points in kept if turn > 0 for x, _ in points]
    ys = [y for turn, points in kept if turn > 0 for _, y in points]
    left, top, right, bottom = min(xs), min(ys), max(xs), max(ys)
    if args.frame:
        left, top, right, bottom = (value * ENLARGE for value in numbers(args.frame, 4, "frame"))
    scale = LYING_LENGTH / (right - left) if args.lying else 1000 / (bottom - top)
    width, height = (right - left) * scale, (bottom - top) * scale

    def place(x, y):
        return (x - left) * scale, (y - top) * scale

    if args.bare:
        # The picked image's own helmet would show around the side's helmet, both filled alike: everything above a line a little under the
        # head anchor (where a brim sits) within two head radii is removed, a skull is put back under where the side's helmet sits, and the figure is traced again.
        if not args.head:
            raise SystemExit("--bare needs --head")
        hx, hy, hr, tilt = head_numbers(args.head)
        px, py, pr = left + hx / scale, top + hy / scale, hr / scale
        rows, columns = np.ogrid[:mask.shape[0], :mask.shape[1]]
        # Across and down the head as tilted: a bowed head's brim runs downhill to the front, and the cut follows it.
        angle = np.radians(tilt)
        across = (columns - px) * np.cos(angle) + (rows - py) * np.sin(angle)
        down = -(columns - px) * np.sin(angle) + (rows - py) * np.cos(angle)
        mask = mask.copy()
        mask[(down < 0.3 * pr) & (np.abs(across) < args.bare_width * pr)] = False
        mask[(down < args.bare_back * pr) & (across < 0) & (across > -args.bare_width * pr)] = False
        mask |= (rows - py) ** 2 + (columns - px) ** 2 <= (0.95 * pr) ** 2
        traced = [(area(simple), simple) for loop in loops(mask)
                  if len(loop) >= 8 * ENLARGE and len(simple := simplify(loop, args.tolerance * ENLARGE)) >= 3]
        kept = [(turn, points) for turn, points in traced if abs(turn) >= largest / 100]

    # Whole body units: a unit is a thousandth of a figure's height, far finer than a pixel on any counter.
    shapes = ["M" + "L".join(f"{px:.0f} {py:.0f}" for px, py in (place(x, y) for x, y in points)) + "Z" for _, points in kept]
    tilt = 0.0
    if args.head:
        cx, cy, r, tilt = head_numbers(args.head)
    elif args.lying:
        raise SystemExit("a lying figure's head is given by hand: --head cx,cy,r in body units")
    else:
        hx, hy, hr = head_of(mask, int(top), int(bottom))
        (cx, cy), r = place(hx, hy), hr * scale
    attributes = f' data-head="{cx:.0f} {cy:.0f} {r:.0f}' + (f' {tilt:g}' if tilt else '') + '"'
    if args.own_helmet:
        attributes += ' data-own-helmet="true"'
    if args.body:
        body_left, body_right = numbers(args.body, 2, "body")
        attributes += f' data-body="{body_left:.0f} {body_right:.0f}"'
    if args.grip:
        if not args.weapon:
            raise SystemExit("--grip needs --weapon")
        x1, y1, x2, y2 = numbers(args.grip, 4, "grip")
        attributes += f' data-grip="{x1:.0f} {y1:.0f} {x2:.0f} {y2:.0f}" data-weapon="{args.weapon}"'
    if args.accept:
        attributes += ' data-accepted="true"'
    source = args.source or f"Traced from the user's Midjourney pick ({Path(args.image).name})."
    body = "\n".join(f'  <path d="{shape}"/>' for shape in shapes)
    out = Path(args.out) if args.out else POSES / f"{args.pose}.svg"
    out.write_text(f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {width:.0f} {height:.0f}"{attributes}>\n'
                   f"  <desc>{source}</desc>\n{body}\n</svg>\n", encoding="utf-8", newline="\n")
    print(f"{out}: {width:.0f} x {height:.0f}, {len(kept)} outlines and holes, head {cx:.0f},{cy:.0f} r {r:.0f}"
          + ("" if args.grip else "; no grip yet: give --grip and --weapon after looking at it"))


if __name__ == "__main__":
    main()
