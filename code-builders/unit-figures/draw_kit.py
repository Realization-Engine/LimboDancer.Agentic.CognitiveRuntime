"""Draws the figure kit and the stand-in poses as SVG sources under src/ASL/units/figures (ASL Unit Figure Art Plan, sections 3 and 6).

Helmets are drawn about a head of radius 100 at the origin, facing right (+x is the front). Weapons are drawn in body units (a figure is
1000 high) with the rear hand at the origin and the muzzle toward +x. Shapes are polygons only, each wound the same way, so the renderer's
nonzero fill joins overlapping pieces without holes. Thicknesses are exaggerated over true scale so a figure about ten pixels tall still
shows its helmet and weapon (decision F9).

Stand-in poses are geometric bodies with the anchors a traced pose will have; they hold the place of the Midjourney poses until the user's
picks are traced, and are never marked accepted.

Run from the repository root:  python code-builders/unit-figures/draw_kit.py
Then:                          python code-builders/unit-figures/build_figures.py
"""

import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FIGURES = ROOT / "src" / "ASL" / "units" / "figures"


def area(points):
    return sum(x1 * y2 - x2 * y1 for (x1, y1), (x2, y2) in zip(points, points[1:] + points[:1])) / 2


def polygon(points):
    """One closed subpath, wound clockwise on screen (y down), the same for every piece."""
    if area(points) < 0:
        points = list(reversed(points))
    head, *rest = points
    return f"M{fmt(head[0])} {fmt(head[1])}" + "".join(f"L{fmt(x)} {fmt(y)}" for x, y in rest) + "Z"


def circle(cx, cy, r, steps=28):
    return polygon([(cx + r * math.cos(2 * math.pi * i / steps), cy + r * math.sin(2 * math.pi * i / steps)) for i in range(steps)])


def ellipse_arc(cx, cy, rx, ry, start, end, steps=14):
    """Points along an ellipse from angle start to end, in degrees, y down (0 is +x, -90 is up)."""
    return [(cx + rx * math.cos(math.radians(start + (end - start) * i / steps)),
             cy + ry * math.sin(math.radians(start + (end - start) * i / steps))) for i in range(steps + 1)]


def fmt(value):
    text = f"{value:.1f}".rstrip("0").rstrip(".")
    return "0" if text == "-0" else text


def write(relative, view_box, shapes, desc, extra=""):
    path = FIGURES / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    body = "\n".join(f'  <path d="{shape}"/>' for shape in shapes)
    path.write_text(
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="{view_box}"{extra}>\n  <desc>{desc}</desc>\n{body}\n</svg>\n',
        encoding="utf-8", newline="\n")


# Helmets, head radius 100 at the origin, front to the right.

def german_helmet():
    """M35 Stahlhelm: a rounded crown, a short visor over the brow, and the skirt sweeping down and out over the ears and the neck."""
    crown = ellipse_arc(-6, 4, 126, 140, 0, -180)
    return [polygon([(168, 30), (122, 8)] + crown[1:] + [(-150, 46), (-196, 118), (-128, 124), (-84, 76), (-10, 60), (70, 50), (120, 42)])]


def russian_helmet():
    """SSh-40: a tall rounded dome with a slight outward flare at the rim, no visor, the rim nearly level."""
    dome = ellipse_arc(0, 30, 118, 168, 0, -180)
    return [polygon([(150, 52), (118, 30)] + dome[1:-1] + [(-118, 30), (-150, 52), (-118, 60), (118, 60)])]


# Weapons, rear hand at the origin, muzzle toward +x, in body units.

def rifle(barrel_end, butt, bayonet=0):
    """A bolt-action rifle: the butt behind the rear hand, the stock thinning to the fore-end, the barrel to the muzzle; with a fixed
    bayonet, a spike beyond it."""
    points = [(butt, -16), (40, -16), (60, -22), (barrel_end, -22), (barrel_end, 6)]
    if bayonet:
        points = [(butt, -16), (40, -16), (60, -22), (barrel_end, -22), (barrel_end + bayonet, -10), (barrel_end, 2), (barrel_end, 6)]
    points += [(70, 10), (0, 26), (butt + 110, 44), (butt, 56)]
    return [polygon(points)]


def mp40():
    """MP40: a thin tube receiver, the long straight magazine below ahead of the grip, the stock a thin bar to the rear."""
    return [
        polygon([(-190, -8), (-40, -14), (-40, 4), (-190, 12)]),
        polygon([(-40, -26), (300, -26), (300, -4), (-40, -2)]),
        polygon([(96, -6), (130, -6), (130, 170), (96, 170)]),
        polygon([(-10, -6), (24, -6), (12, 70), (-22, 70)]),
    ]


def ppsh():
    """PPSh-41: a wooden stock to the rear, the perforated barrel jacket forward, and the round drum under it."""
    return [
        polygon([(-250, -6), (0, -24), (300, -24), (300, -4), (0, 22), (-250, 44)]),
        circle(110, 66, 64),
    ]


# Stand-in poses, 1000 high, facing right.

def standing():
    """A rifleman advancing: a stride, the rear hand at the hip and the front hand forward and up, so the rifle points ahead and up."""
    shapes = [
        circle(214, 104, 86),
        polygon([(178, 170), (252, 170), (282, 210), (296, 340), (276, 528), (154, 528), (142, 340), (152, 210)]),
        polygon([(156, 500), (230, 512), (164, 760), (128, 1000), (52, 1000), (94, 760)]),
        polygon([(206, 512), (280, 500), (334, 760), (392, 1000), (316, 1000), (262, 772)]),
        polygon([(160, 214), (222, 214), (218, 478), (160, 478)]),
        polygon([(236, 212), (290, 206), (352, 372), (308, 402)]),
    ]
    return shapes, 'viewBox="0 0 600 1000"', ' data-head="214 104 86" data-grip="190 462 334 382" data-weapon="rifle"'


def leader():
    """A leader: standing upright, the front arm pointing ahead, the weapon held low in the rear hand."""
    shapes = [
        circle(214, 104, 86),
        polygon([(178, 170), (252, 170), (282, 210), (292, 340), (272, 528), (156, 528), (146, 340), (154, 210)]),
        polygon([(160, 500), (232, 512), (204, 760), (190, 1000), (116, 1000), (130, 760)]),
        polygon([(204, 512), (272, 500), (286, 760), (310, 1000), (236, 1000), (220, 770)]),
        polygon([(236, 206), (282, 214), (470, 214), (470, 254), (276, 268)]),
        polygon([(160, 214), (214, 214), (230, 380), (184, 396)]),
    ]
    return shapes, 'viewBox="0 0 520 1000"', ' data-head="214 104 86" data-grip="160 420 300 360" data-weapon="smg"'


def pinned():
    """A soldier prone, head to the right, the rifle along the ground ahead of him; the box cropped to the body (a man lying is about a
    third as high as he is long)."""
    shapes = [
        circle(1020, 150, 86),
        polygon([(420, 180), (950, 100), (970, 210), (420, 280)]),
        polygon([(40, 230), (430, 182), (430, 278), (40, 320)]),
        polygon([(900, 190), (990, 180), (1060, 290), (980, 315)]),
    ]
    return shapes, 'viewBox="0 0 1460 340"', ' data-head="1020 150 86" data-grip="960 250 1200 262" data-weapon="rifle"'


SIDES = {
    "german": (german_helmet, lambda: rifle(470, -240), mp40,
               "M35 Stahlhelm; Kar98k 1.11 m; MP40. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "russian": (russian_helmet, lambda: rifle(490, -250, bayonet=180), ppsh,
                "SSh-40; Mosin-Nagant 91/30 with its bayonet fixed, 1.66 m; PPSh-41 with its drum. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
}

STAND_INS = {"standing": standing, "leader": leader, "pinned": pinned}


def main():
    for side, (helmet, long_arm, smg, source) in SIDES.items():
        write(f"kit/{side}-helmet.svg", "-160 -160 320 320", helmet(), source)
        write(f"kit/{side}-rifle.svg", "-300 -160 1000 320", long_arm(), source)
        write(f"kit/{side}-smg.svg", "-300 -160 700 360", smg(), source)
    for pose, draw in STAND_INS.items():
        shapes, view_box, anchors = draw()
        write(f"poses/{pose}.svg", view_box.split('"')[1], shapes,
              "Stand-in: a geometric body holding the anchors until the user's Midjourney pick is traced.", anchors)
    print("wrote the kit and the stand-in poses")


if __name__ == "__main__":
    main()
