"""The ASOP steps (Appendix 2, pages 678 to 681) that cite a rule, read from the registered rulebook PDF, with the other rules each cites.

    python asop_steps.py C9.3 [C9.31 ...]      the steps that cite any of these rules (a rule cites its subrules too: C9.3 covers C9.31)

A claim takes, in its claim commit, every ASOP row whose step cites only the claim's own rules (AGENTS.md, the plan's section 4.1). A step that
cites rules of other sections too is a step of its phase, not of one rule: it stays open until track 3 accepts that phase. The tool prints, for
each step, its inventory row's owner and which of the two it is. It needs PyMuPDF (`fitz`) and the PDF (paths.RULEBOOK_PDF).
"""
import os as _os
import re
import sys
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
from paths import RULEBOOK_PDF  # noqa: E402

PAGES = range(677, 681)  # physical pages 678 to 681, counted from 0
STEP = re.compile(r"^(\d\.\d\d[A-Z]?) ", re.M)
CITE = re.compile(r"\b([A-HW]\d+(?:\.\d+)?)|-\.(\d+)")


def text():
    import fitz
    document = fitz.open(RULEBOOK_PDF)
    parts = []
    for number in PAGES:
        page = document[number]
        middle = page.rect.width / 2
        blocks = sorted(page.get_text("blocks"), key=lambda b: (b[0] >= middle, b[1]))
        parts += [block[4] for block in blocks]
    return "\n".join(parts).replace("-\n", "").replace("\n", " \n")


def steps():
    body = text()
    marks = list(STEP.finditer(body))
    for i, mark in enumerate(marks):
        chunk = body[mark.end():marks[i + 1].start() if i + 1 < len(marks) else len(body)]
        cites, last = [], None
        for m in CITE.finditer(chunk):
            if m.group(1):
                last = m.group(1)
                cites.append(last)
            elif last and "." in last:
                cites.append(last.split(".")[0] + "." + m.group(2))
        yield mark.group(1), " ".join(chunk.split())[:90], list(dict.fromkeys(cites))


def covers(rule, cite):
    return cite == rule or (cite.startswith(rule) and cite[len(rule)] in ".0123456789")


def main():
    rules = sys.argv[1:]
    if not rules:
        sys.exit(__doc__)
    owners = {row["§"]: row.get("Owner", "") for row in inventory_table.rows(inventory_table.read_lines()[0])}
    found = 0
    for step, words, cites in steps():
        if not any(covers(rule, cite) for rule in rules for cite in cites):
            continue
        found += 1
        other = [cite for cite in cites if not any(covers(rule, cite) for rule in rules)]
        row = f"ASOP {step}"
        print(f"{row}: {words}")
        print(f"    owner: {owners.get(row, 'no such row') or 'none'}; cites {', '.join(cites)}")
        print("    only these rules: the claim takes it" if not other else f"    cites {len(other)} other rules: a phase step, left to track 3")
    print(f"steps that cite {', '.join(rules)}: {found}")


if __name__ == "__main__":
    main()
