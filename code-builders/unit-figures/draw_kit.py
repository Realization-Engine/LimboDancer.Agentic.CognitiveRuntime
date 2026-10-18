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
import sys
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


def british_helmet():
    """Mk II "Brodie": a shallow dome sitting high, and a wide flat brim standing out all round, the soup-plate outline."""
    dome = ellipse_arc(0, 10, 104, 120, 0, -180)
    return [polygon([(204, 4), (196, 40), (-196, 40), (-204, 4), (-104, 10)] + dome[::-1][1:-1] + [(104, 10)])]


def american_helmet():
    """M1: a smooth deep dome coming well down the head, and a short brim all round, the rim level."""
    dome = ellipse_arc(0, 36, 120, 158, 0, -180)
    return [polygon([(144, 36), (144, 56), (-144, 56), (-144, 36), (-120, 36)] + dome[::-1][1:-1] + [(120, 36)])]


def finnish_cap():
    """M36 field cap: a low soft crown, flat on top and a little higher at the front, with a short peak over the brow."""
    return [polygon([(196, 30), (150, 4), (120, -96), (40, -112), (-90, -104), (-122, -60), (-128, 22), (-90, 30)])]


def italian_helmet():
    """M33: a shallow rounded bowl, low and wide, with a small even brim that turns out a little at the back."""
    bowl = ellipse_arc(0, 20, 122, 132, 0, -180)
    return [polygon([(150, 22), (150, 40), (-160, 50), (-158, 28), (-122, 20)] + bowl[::-1][1:-1] + [(122, 20)])]


def axis_minor_helmet():
    """One shared figure for the Axis Minors (F5): a German-pattern steel helmet with a rounder, gentler skirt and no visor step, as the
    Hungarian M38 and the Romanian M39 read in profile."""
    crown = ellipse_arc(0, 6, 124, 138, 0, -180)
    return [polygon([(136, 44), (124, 6)] + crown[1:] + [(-150, 60), (-164, 104), (-112, 108), (-70, 66), (60, 56)])]


def french_helmet():
    """Adrian M26: a rounded skull with a tall crest along its top, a peak in front and a peak behind, the brim drooping a little."""
    skull = ellipse_arc(0, 14, 112, 118, 0, -180)
    crest = [(70, -90), (46, -124), (-56, -128), (-86, -88)]
    return [
        polygon([(178, 40), (112, 14)] + skull[1:-1] + [(-112, 14), (-180, 52), (-110, 46), (110, 42)]),
        polygon(crest),
    ]


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


def sten():
    """Sten: a skeletal wire stock, a thin tube, and a short barrel; its magazine stands out to the side, so the profile shows a stub."""
    return [
        polygon([(-200, -10), (-180, -10), (-180, 40), (-200, 40)]),
        polygon([(-200, -10), (-20, -14), (-20, 2), (-200, 6)]),
        polygon([(-30, -26), (280, -22), (280, -6), (-30, 0)]),
        polygon([(70, -4), (110, -4), (110, 40), (70, 40)]),
    ]


def thompson():
    """Thompson M1A1: a wooden butt, the receiver, the barrel with no compensator, a straight stick magazine and the grip below."""
    return [
        polygon([(-240, -4), (-20, -22), (-20, 14), (-240, 50)]),
        polygon([(-20, -28), (290, -24), (290, -6), (-20, 4)]),
        polygon([(80, -4), (114, -4), (118, 130), (84, 130)]),
        polygon([(-10, 0), (24, 0), (14, 66), (-20, 66)]),
    ]


def suomi():
    """Suomi KP/-31: a full wooden stock to the long slotted barrel jacket, and the large drum under it."""
    return [
        polygon([(-250, -2), (0, -22), (330, -22), (330, -4), (60, 4), (0, 24), (-250, 46)]),
        circle(90, 72, 70),
    ]


def mab38():
    """Beretta MAB 38: a long rifle-like wooden stock, the slotted jacket with its compensator, a magazine below ahead of the trigger."""
    return [
        polygon([(-250, -6), (0, -20), (330, -20), (330, -4), (40, 10), (0, 24), (-250, 46)]),
        polygon([(70, 6), (100, 6), (100, 120), (70, 120)]),
    ]


def carbine():
    """A short carbine: a rifle shortened, the leader's weapon where no SMG is drawn."""
    return rifle(330, -220)


def mas38():
    """MAS-38: a slim stock in line with the angled receiver, a short barrel, and a long thin magazine below."""
    return [
        polygon([(-210, 4), (-10, -14), (-10, 6), (-210, 34)]),
        polygon([(-10, -22), (230, -18), (230, -4), (-10, 6)]),
        polygon([(50, 4), (78, 4), (92, 130), (64, 130)]),
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
    "british": (british_helmet, lambda: rifle(420, -240), sten,
                "Mk II helmet; Lee-Enfield No. 4, 1.13 m; Sten. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "american": (american_helmet, lambda: rifle(410, -240), thompson,
                 "M1 helmet; M1 Garand, 1.10 m; Thompson M1A1. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "finnish": (finnish_cap, lambda: rifle(450, -240), suomi,
                "M36 field cap; Mosin-Nagant M/39, 1.18 m; Suomi KP/-31 with its drum. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "italian": (italian_helmet, lambda: rifle(500, -240), mab38,
                "M33 helmet; Carcano M91, 1.29 m; Beretta MAB 38. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "axis-minor": (axis_minor_helmet, lambda: rifle(410, -240), carbine,
                   "One figure for the Axis Minors (F5): a German-pattern helmet with a rounder skirt; a Mannlicher-pattern rifle, about 1.10 m; a carbine. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "french": (french_helmet, lambda: rifle(360, -230, bayonet=150), mas38,
               "Adrian M26; MAS-36, 1.02 m, its spike bayonet fixed; MAS-38. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "german": (german_helmet, lambda: rifle(470, -240), mp40,
               "M35 Stahlhelm; Kar98k 1.11 m; MP40. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
    "russian": (russian_helmet, lambda: rifle(490, -250, bayonet=180), ppsh,
                "SSh-40; Mosin-Nagant 91/30 with its bayonet fixed, 1.66 m; PPSh-41 with its drum. Drawn from the documented profiles (plan section 6), exaggerated for size (F9)."),
}

STAND_INS = {"standing": standing, "leader": leader, "pinned": pinned}


def main():
    for side, (helmet, long_arm, smg, source) in SIDES.items():
        # The helmets were accepted by the user on 2026-10-18; the weapons are not, since no traced pose uses them.
        write(f"kit/{side}-helmet.svg", "-160 -160 320 320", helmet(), source, ' data-accepted="true"')
        write(f"kit/{side}-rifle.svg", "-300 -160 1000 320", long_arm(), source)
        write(f"kit/{side}-smg.svg", "-300 -160 700 360", smg(), source)
    # The stand-in poses are no longer written: every pose is now traced from the user's picks and accepted (2026-10-18), and writing
    # a stand-in would overwrite one. Pass --stand-ins to write them to a scratch folder for comparison.
    if "--stand-ins" in sys.argv:
        for pose, draw in STAND_INS.items():
            shapes, view_box, anchors = draw()
            write(f"stand-ins/{pose}.svg", view_box.split('"')[1], shapes, "Stand-in: a geometric body, for comparison only.", anchors)
    print("wrote the kit")


if __name__ == "__main__":
    main()
