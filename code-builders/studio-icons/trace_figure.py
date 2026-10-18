"""Traces the dark figure of a chosen Midjourney counter icon into SVG path data on the Studio's 24-unit icon grid.

The image is a counter: an olive frame around a light face bearing a dark figure, with light piping inside it. The image is enlarged so
curves trace smoothly, the frame is found (its outer edge maps onto the icon frame's outer edge, 1.5 to 22.5) and only its inside is read,
and the dark figure's boundary is followed pixel edge by pixel edge into closed loops, simplified, and written out. An outline runs one way
and a hole the other. The file has two lines: the whole figure with its piping closed (laid under it in white, so piping that cuts the
figure into pieces shows white), and the figure's outlines and holes for an even-odd fill.

    python code-builders/studio-icons/trace_figure.py <image> <out.txt> [tolerance in enlarged pixels]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ENLARGE = 4
DARK = 110          # luminance below this is the figure (the face and the piping are far lighter)
OUTER = (1.5, 22.5)


def frame(rgb):
    """The frame's outer box and its thickness: olive pixels, greenish and mid-dark."""
    r, g, b = (rgb[..., i].astype(int) for i in range(3))
    olive = (g > 60) & (g < 150) & (r - b > 12) & (g - b > 12)
    ys, xs = np.nonzero(olive)
    left, top, right, bottom = xs.min(), ys.min(), xs.max() + 1, ys.max() + 1
    row = olive[(top + bottom) // 2, left:right]
    thickness = int(np.argmin(row)) if not row.all() else 0
    return left, top, right, bottom, thickness


def loops(inside):
    """The closed boundaries of a mask, clockwise on screen around the inside, so outer outlines and holes turn opposite ways."""
    height, width = inside.shape
    pad = np.zeros((height + 2, width + 2), dtype=bool)
    pad[1:-1, 1:-1] = inside
    core = pad[1:-1, 1:-1]
    ys, xs = np.nonzero(core)
    edges = {}

    def add(start, end):
        edges.setdefault(start, []).append(end)

    for y, x in zip(ys.tolist(), xs.tolist()):
        if not pad[y, x + 1]:          # above is outside
            add((x, y), (x + 1, y))
        if not pad[y + 1, x + 2]:      # right
            add((x + 1, y), (x + 1, y + 1))
        if not pad[y + 2, x + 1]:      # below
            add((x + 1, y + 1), (x, y + 1))
        if not pad[y + 1, x]:          # left
            add((x, y + 1), (x, y))
    result = []
    while edges:
        start = next(iter(edges))
        loop, point = [start], start
        while True:
            ends = edges.get(point)
            if not ends:
                break
            following = ends.pop()
            if not ends:
                del edges[point]
            if following == start:
                break
            loop.append(following)
            point = following
        result.append(loop)
    return result


def closed(mask, radius):
    """The mask with gaps up to twice the radius filled (a dilation, then an erosion, each with a square of that radius)."""
    def spread(values, grow):
        result = values.copy()
        for axis in (0, 1):
            step = result.copy()
            for shift in range(1, radius + 1):
                for sign in (1, -1):
                    moved = np.roll(result, sign * shift, axis=axis)
                    step = (step | moved) if grow else (step & moved)
            result = step
        return result

    return spread(spread(mask, True), False)


def in_corner(coords):
    """Whether an outline lies wholly in a corner of the frame, where its rounding leaves dark slivers inside the box."""
    low, high = OUTER[0] + 2.5, OUTER[1] - 2.5
    return all((x < low or x > high) and (y < low or y > high) for x, y in coords)


def area(points):
    return 0.5 * sum(x1 * y2 - x2 * y1 for (x1, y1), (x2, y2) in zip(points, points[1:] + points[:1]))


def simplify(points, tolerance):
    """Ramer-Douglas-Peucker on a closed loop, split at the point farthest from its start."""
    def rdp(pts):
        if len(pts) < 3:
            return pts
        (x1, y1), (x2, y2) = pts[0], pts[-1]
        dx, dy = x2 - x1, y2 - y1
        norm = (dx * dx + dy * dy) ** 0.5 or 1.0
        distances = [abs(dy * (x - x1) - dx * (y - y1)) / norm for x, y in pts[1:-1]]
        index = int(np.argmax(distances)) + 1
        if distances[index - 1] > tolerance:
            return rdp(pts[: index + 1])[:-1] + rdp(pts[index:])
        return [pts[0], pts[-1]]

    far = max(range(len(points)), key=lambda i: (points[i][0] - points[0][0]) ** 2 + (points[i][1] - points[0][1]) ** 2)
    return rdp(points[: far + 1])[:-1] + rdp(points[far:] + [points[0]])[:-1]


def main():
    image, out = Path(sys.argv[1]), Path(sys.argv[2])
    tolerance = float(sys.argv[3]) if len(sys.argv) > 3 else 2.0
    source = Image.open(image).convert("RGB")
    source = source.resize((source.width * ENLARGE, source.height * ENLARGE), Image.LANCZOS)
    rgb = np.asarray(source)
    left, top, right, bottom, thickness = frame(rgb)
    lum = rgb.astype(float) @ np.array([0.299, 0.587, 0.114])
    inset = thickness + 2 * ENLARGE
    figure = np.zeros(lum.shape, dtype=bool)
    figure[top + inset:bottom - inset, left + inset:right - inset] = lum[top + inset:bottom - inset, left + inset:right - inset] < DARK
    scale = (OUTER[1] - OUTER[0]) / (right - left), (OUTER[1] - OUTER[0]) / (bottom - top)
    def traced(mask):
        for loop in loops(mask):
            if len(loop) >= 6 * ENLARGE and len(simple := simplify(loop, tolerance)) >= 3:
                coords = [(OUTER[0] + (x - left) * scale[0], OUTER[0] + (y - top) * scale[1]) for x, y in simple]
                if not in_corner(coords):
                    yield area(simple), "M" + "L".join(f"{x:.2f} {y:.2f}" for x, y in coords) + "Z"

    # The whole figure with the piping closed, laid under it in white so the piping shows white on an off-white face.
    whole = "".join(data for turn, data in traced(closed(figure, 4 * ENLARGE)) if turn > 0)
    outer, every = [], []
    for turn, data in traced(figure):
        every.append(data)
        if turn > 0:
            outer.append(data)
    out.write_text(whole + "\n" + "".join(every) + "\n", encoding="utf-8", newline="\n")
    print(f"frame {left},{top} to {right},{bottom}, {thickness} thick; {len(outer)} outlines, {len(every) - len(outer)} holes; "
          f"{sum(d.count('L') + 1 for d in every)} points -> {out}")


if __name__ == "__main__":
    main()
