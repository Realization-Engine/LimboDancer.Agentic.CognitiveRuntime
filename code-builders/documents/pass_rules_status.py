"""Each rule pass's Rules status line in the redesign plan, written from the rule inventory (the Rules-first plan, section 11.1).

The line stands under the pass's heading in section 22.1, as a quote line that begins "> **Rules status**". It is written for every pass
that has rows in the inventory (rows whose "Pass or ruling" cell is "Pass N") and for no other. It counts the pass's rows by status, by
claim state (with the owners), the rows a rule is still decided outside Rules on, the rows a Turns calculator decides, the wired rows,
and the minigame bundles. When every row's claim is built or later, the line ends "Rules part built."

    python pass_rules_status.py            rewrites the lines
    python pass_rules_status.py --check    lists every line that differs from the inventory and exits 1 if any does

A claim runs it as its last step; the merge gate's strict stage runs --check, so the plan cannot drift from the ledger.
"""
import collections
import os as _os
import re
import sys
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
from paths import REPO  # noqa: E402

PLAN = REPO + "/src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md"
PREFIX = "> **Rules status**"
CLAIM_ORDER = ["open", "claimed", "built", "reviewed", "wired", "accepted"]
BUILT_OR_LATER = {"built", "reviewed", "wired", "accepted"}


def plural(n, word):
    return f"{n} {word}{'' if n == 1 else 's'}"


def status_line(rows):
    statuses = collections.Counter(row["Status"] for row in rows)
    claims = collections.Counter(row["Claim state"] for row in rows)
    owners = sorted({row["Owner"] for row in rows if row["Owner"]})
    outside = sum(1 for row in rows if "outside Rules" in row["Rules calculator"])
    turns = sum(1 for row in rows if "Turns work" in row["Rules calculator"])
    wired = claims["wired"] + claims["accepted"]
    bundles = sorted({bundle.strip() for row in rows for bundle in row["Minigames"].split(",") if bundle.strip()})
    parts = [f"{plural(len(rows), 'row')}: " + ", ".join(f"{statuses[s]} {s}" for s in inventory_table.STATUSES if statuses[s]) + "."]
    parts.append("Claims: " + ", ".join(f"{claims[s]} {s}" for s in CLAIM_ORDER if claims[s])
                 + (f" (owners: {', '.join(owners)})" if owners else "") + ".")
    if outside:
        parts.append(f"A rule decided outside Rules on {plural(outside, 'row')}.")
    if turns:
        parts.append(f"A Turns calculator on {plural(turns, 'row')}.")
    parts.append(f"Wiring: {wired} of {len(rows)}. Minigames: {len(bundles)}.")
    if all(row["Claim state"] in BUILT_OR_LATER for row in rows):
        parts.append("Rules part built.")
    return f"{PREFIX} (written from the [rule inventory](<ASL Rule Inventory A to E.md>) by `code-builders/documents/pass_rules_status.py`): " + " ".join(parts)


def main():
    check = "--check" in sys.argv
    inventory_lines, _ = inventory_table.read_lines()
    by_pass = collections.defaultdict(list)
    for row in inventory_table.rows(inventory_lines):
        if re.match(r"^Pass \d+$", inventory_table.pass_of(row)):
            by_pass[int(inventory_table.pass_of(row)[5:])].append(row)
    lines, nl = inventory_table.read_lines(PLAN)
    start = next(i for i, line in enumerate(lines) if line.startswith("### 22.1 "))
    end = next(i for i, line in enumerate(lines) if line.startswith("### 22.2 "))
    headers = {int(m.group(1)): i for i in range(start, end) if (m := re.match(r"^\*\*Pass (\d+): ", lines[i]))}
    missing = sorted(set(by_pass) - set(headers))
    differ = []
    for number in sorted(missing):
        differ.append(f"pass {number}: {len(by_pass[number])} rows in the inventory and no section in 22.1")
    for number, at in sorted(headers.items(), key=lambda item: -item[1]):
        has_line = at + 2 < len(lines) and lines[at + 2].startswith(PREFIX)
        want = status_line(by_pass[number]) if number in by_pass else None
        have = lines[at + 2] if has_line else None
        if have == want:
            continue
        differ.append(f"pass {number}: {'no line' if have is None else 'the line differs' if want else 'a line with no rows'}")
        if check:
            continue
        if has_line and want is None:
            del lines[at + 1:at + 3]
        elif has_line:
            lines[at + 2] = want
        else:
            lines[at + 1:at + 1] = ["", want]
    for item in reversed(differ):
        print(item)
    print(f"passes with rows: {len(by_pass)}; lines that differ: {len(differ)}")
    if check:
        sys.exit(1 if differ else 0)
    if missing:
        sys.exit("a pass with rows has no section; nothing written")
    open(PLAN, "w", encoding="utf-8", newline="").write(nl.join(lines))
    print("written")


if __name__ == "__main__":
    main()
