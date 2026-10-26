"""Pass 71, 4c: list the test methods that start play in a game from a card (the Sniper gate's blast radius).

A test file is read when it names one of the scenario cards; a method is in the radius when it advances the phase, itself or through a helper
of its file. Prints Class.Method lines and the file counts. Reads the test sources only (never src/ASL/boards).
"""
import pathlib
import re
import sys

ROOT = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests")
PROJECTS = ["LimboDancer.Domains.Asl.Play.Tests", "LimboDancer.Domains.Asl.MapStudio.Tests"]
CARDS = re.compile(r'"(guards-counterattack|tractor-works|gambit|armor-test)"')
METHOD = re.compile(r"^    (?:\[[^\]]*\]\s*)*(?:public|private|internal|protected)[^=;(]*?\b(\w+)\s*\(", re.M)
SEED = re.compile(r"AdvancePhase|\bAdvance\(|\bAdvanceTo\(|end-phase|StartPlay|advance-phase", re.I)


def methods(text):
    starts = [(m.start(), m.group(1)) for m in METHOD.finditer(text)]
    for index, (start, name) in enumerate(starts):
        end = starts[index + 1][0] if index + 1 < len(starts) else len(text)
        yield name, start, text[start:end]


def main():
    total = 0
    for project in PROJECTS:
        for path in sorted((ROOT / project).glob("*.cs")):
            text = path.read_text(encoding="utf-8")
            if not CARDS.search(text):
                continue
            items = list(methods(text))
            reach = {name for name, _, body in items if SEED.search(body)}
            changed = True
            while changed:
                changed = False
                for name, _, body in items:
                    if name not in reach and any(re.search(r"\b" + re.escape(other) + r"\(", body) for other in reach if other != name):
                        reach.add(name)
                        changed = True
            tests = []
            for name, start, body in items:
                head = body.split("(", 1)[0]
                if name in reach and re.search(r"\[(Fact|Theory)", head):
                    tests.append(name)
            if tests:
                print(f"== {path.stem} ({len(tests)})")
                for name in tests:
                    print(f"{path.stem}.{name}")
                total += len(tests)
    print(f"total {total}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
