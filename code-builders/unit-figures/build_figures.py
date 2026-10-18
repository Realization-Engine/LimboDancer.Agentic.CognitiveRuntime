"""Builds src/ASL/units/figures/figures.json from the figure parts' SVG sources (ASL Unit Figure Art Plan, section 3).

Sources, under src/ASL/units/figures:
  poses/<pose>.svg           a body pose in body units (poses/<pose>.<side>.svg: that side's version, such as a hero with its
                             side's grenade; a side without one gets the general pose) (a man standing is 1000 high): viewBox "0 0 <width> <height>", 1000 high when
                             upright and cropped to the body when lying; data-head="cx cy r [tilt]"; data-grip="x1 y1 x2 y2" and
                             data-weapon="rifle|smg" when it holds a weapon
  kit/<side>-helmet.svg      a helmet about a head of radius 100 at the origin
  kit/<side>-<weapon>.svg    a weapon in body units, the rear hand at the origin, the muzzle toward +x
Every source has data-accepted="true" once the user accepts it (until then it is a stand-in, drawn only in the Unit Lab's Art tab)
and a <desc> saying where it comes from. Every <path d> in a source is one filled shape; they are joined into one path.

Run from the repository root:  python code-builders/unit-figures/build_figures.py [--check]
--check writes nothing and fails when figures.json is not what the sources make.
"""

import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FIGURES = ROOT / "src" / "ASL" / "units" / "figures"
OUTPUT = FIGURES / "figures.json"
SVG = "{http://www.w3.org/2000/svg}"
POSES = ["standing", "crew", "leader", "hero", "wounded", "broken", "pinned", "berserk", "prisoner"]
WEAPONS = ["rifle", "smg"]


def numbers(text, count, where):
    values = [float(part) for part in text.replace(",", " ").split()]
    if len(values) != count:
        raise SystemExit(f"{where}: expected {count} numbers, found {text!r}")
    return values


def read(path):
    root = ET.parse(path).getroot()
    paths = [element.get("d", "").strip() for element in root.iter(f"{SVG}path")]
    if not paths or not all(paths):
        raise SystemExit(f"{path}: no path data")
    desc = root.find(f"{SVG}desc")
    return root, " ".join(paths), (desc.text or "").strip() if desc is not None else "", root.get("data-accepted") == "true"


def pose(path):
    root, d, source, accepted = read(path)
    box = numbers(root.get("viewBox", ""), 4, path)
    if box[0] != 0 or box[1] != 0 or box[2] <= 0 or box[3] <= 0 or box[3] > 1000:
        raise SystemExit(f"{path}: a pose's viewBox starts at 0 0 and is at most 1000 high")
    head = [float(part) for part in root.get("data-head", "").split()]
    if len(head) not in (3, 4):
        raise SystemExit(f"{path} data-head: expected cx cy r, or cx cy r tilt")
    entry = {"path": d, "width": box[2], "height": box[3], "head": {"x": head[0], "y": head[1], "radius": head[2]}}
    if len(head) == 4 and head[3]:
        entry["head"]["tilt"] = head[3]
    if root.get("data-body"):
        body_left, body_right = numbers(root.get("data-body"), 2, f"{path} data-body")
        entry["body"] = {"left": body_left, "right": body_right}
    if root.get("data-own-helmet") == "true":
        entry["ownHelmet"] = True
    if root.get("data-grip"):
        x1, y1, x2, y2 = numbers(root.get("data-grip"), 4, f"{path} data-grip")
        weapon = root.get("data-weapon", "")
        if weapon not in WEAPONS:
            raise SystemExit(f"{path}: data-weapon must be one of {WEAPONS}")
        entry["grip"] = {"x1": x1, "y1": y1, "x2": x2, "y2": y2}
        entry["weapon"] = weapon
    entry["accepted"] = accepted
    entry["source"] = source
    return entry


def kit(path):
    _, d, source, accepted = read(path)
    return {"path": d, "accepted": accepted, "source": source}


def build():
    poses, helmets, weapons = {}, {}, {}
    for path in sorted((FIGURES / "poses").glob("*.svg")):
        if path.stem.split(".")[0] not in POSES:
            raise SystemExit(f"{path}: not a pose ({', '.join(POSES)}), or a pose and a side")
        poses[path.stem] = pose(path)
    for path in sorted((FIGURES / "kit").glob("*.svg")):
        side, _, part = path.stem.rpartition("-")
        if part == "helmet":
            helmets[side] = kit(path)
        elif part in WEAPONS:
            weapons.setdefault(side, {})[part] = kit(path)
        else:
            raise SystemExit(f"{path}: name a kit file <side>-helmet or <side>-<weapon>")
    return {"poses": poses, "helmets": helmets, "weapons": weapons}


def main():
    text = json.dumps(build(), indent=2, ensure_ascii=False) + "\n"
    if "--check" in sys.argv:
        current = OUTPUT.read_text(encoding="utf-8") if OUTPUT.exists() else ""
        if current != text:
            raise SystemExit("figures.json is not what the sources make; run build_figures.py")
        print("figures.json matches its sources")
        return
    OUTPUT.write_text(text, encoding="utf-8", newline="\n")
    print(f"wrote {OUTPUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
