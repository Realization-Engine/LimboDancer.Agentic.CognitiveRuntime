"""Writes the prompt for Codex's track 3 wiring at a batch gate (the Rules-first plan, sections 4.3 and 9.1; the user, 2026-10-29).

    python wiring_prompt.py [--note "<Claude's note>"] [--dry]

Claude runs it at each batch gate, once the inventory's section 6 records the gate's commit. It writes the next numbered file in
src/ASL/docs/Prompts/Codex Wiring/ ("Wiring Prompt 001.md", "002", ...), so the folder is the history of what Codex was handed; --dry prints
the prompt instead. The prompt is written from the board, never by hand: every open wiring row grouped by capability (rows whose rule claim
merged since the previous prompt's batch are marked new), the wiring claims in progress, and the rule claims waiting for Codex's review.
It names AGENTS.md and the plan for how to claim and wire, and gives no procedure of its own. A --note is Claude's, added as it stands.
"""
import collections
import glob
import os
import re
import subprocess
import sys
import sys as _sys
_sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import claim_check  # noqa: E402
import inventory_table  # noqa: E402
from paths import REPO  # noqa: E402

FOLDER = REPO + "/src/ASL/docs/Prompts/Codex Wiring"
DESIGNS = REPO + "/src/ASL/docs/Designs"
BATCH = re.compile(r"^\*\*Batch gate:\*\* `([0-9a-f]{7,40})`")
ORDER = ["fire", "movement", "rout", "rally", "close combat", "setup and work", "the turn's boundaries"]


def git(*args):
    return subprocess.run(["git", "-C", REPO, *args], capture_output=True, text=True, encoding="utf-8").stdout.strip()


def previous_prompt():
    files = sorted(glob.glob(FOLDER + "/Wiring Prompt *.md"))
    if not files:
        return 0, None
    number = int(re.search(r"(\d+)\.md$", files[-1]).group(1))
    with open(files[-1], encoding="utf-8") as f:
        batch = next((m.group(1) for line in f if (m := BATCH.match(line))), None)
    return number, batch


def design_link(claim):
    found = sorted(glob.glob(f"{DESIGNS}/ASL {claim} *Claim Design.md"))
    return f"[{claim}](<../../Designs/{os.path.basename(found[0])}>)" if found else claim


def merged_since(commit, claims):
    if not commit:
        return set()
    branches = {claim["branch"]: name for name, claim in claims.items()}
    out = git("log", "--merges", "--first-parent", "--format=%s", f"{commit}..HEAD")
    return {branches[m.group(1)] for line in out.splitlines() if (m := claim_check.MERGE.match(line)) and m.group(1) in branches}


def main():
    args = sys.argv[1:]
    note = args[args.index("--note") + 1] if "--note" in args else ""
    lines, _ = inventory_table.read_lines()
    faults = []
    claims = claim_check.check_ledger(lines, faults)
    wiring = claim_check.check_wiring(lines, claims, faults)
    if faults:
        sys.exit("the board has faults; run claim_check.py first:\n" + "\n".join(faults))
    batch = next((m.group(1) for line in lines if (m := claim_check.LAST_BATCH.match(line))), None)
    if not batch:
        sys.exit("the inventory's section 6 records no last batch gate")
    number, previous = previous_prompt()
    if previous == batch:
        sys.exit(f"Wiring Prompt {number:03d} already covers the batch gate {batch}; record the new gate's commit first")
    new = merged_since(previous, claims)
    rows = claim_check.table(lines, claim_check.WIRING_HEADER, 8, [], "wiring")
    open_rows = collections.defaultdict(list)
    for row, rule, capability, sites, old, changes, owner, state in rows:
        if state == "open":
            open_rows[capability].append((row, rule, sites, old, changes))
    out = [f"# Wiring Prompt {number + 1:03d}", "",
           f"**Batch gate:** `{batch}` ({git('log', '-1', '--format=%cs', batch)}). Written by Claude with "
           "`code-builders/documents/wiring_prompt.py` from the [rule inventory](<../../ASL Rule Inventory A to E.md>), sections 6 to 8; "
           + (f"the previous prompt was Wiring Prompt {number:03d}, at `{previous}`." if previous else "the first prompt."), "",
           "## For Codex", "",
           "You take track 3, the wiring (the user, 2026-10-29): Play, the Units projector, ASL.Turns, and the Studio's services moved onto "
           "the calculators the rule claims built. How to claim, build, gate, and review a wiring claim is in [AGENTS.md](<../../../../../AGENTS.md>) "
           "(\"Wiring claims (track 3)\") and the [Rules-first plan](<../../Plans/ASL Rules-First Four-Track Plan.md>), section 9.1; this prompt "
           "says only what is open on the board at this batch gate. Each rule claim's design (linked) has its map of the sites and its rulings.", ""]
    total = sum(len(v) for v in open_rows.values())
    out += [f"## Open wiring rows ({total})", ""]
    if not total:
        out += ["None.", ""]
    for capability in sorted(open_rows, key=lambda c: ORDER.index(c) if c in ORDER else len(ORDER)):
        out += [f"### {capability[0].upper() + capability[1:]}", "",
                "| Wiring | Rule claim | Sites to rewire | Old members to remove | Behavior changes |", "|---|---|---|---|---|"]
        for row, rule, sites, old, changes in sorted(open_rows[capability], key=lambda r: (r[1] not in new, r[0])):
            mark = " (new)" if rule in new else ""
            out.append(f"| {row}{mark} | {design_link(rule)} | {sites} | {old} | {changes} |")
        out.append("")
    in_progress = [(name, claim) for name, claim in wiring.items() if claim["state"] in ("claimed", "wired")]
    out += ["## Wiring claims in progress", ""]
    out += [f"- {name} ({claim['agent']}, {claim['state']}): {', '.join(claim['rows'])}" for name, claim in in_progress] or ["None."]
    review = [(name, claim) for name, claim in claims.items() if claim["state"] == "built"]
    out += ["", "## Rule claims waiting for review", "",
            "The agent that did not build a claim reviews it (AGENTS.md, step 4). Built and not yet reviewed:", ""]
    out += [f"- {design_link(name)} ({claim['agent']})" for name, claim in review] or ["None."]
    if note:
        out += ["", "## Claude's note", "", note]
    text = "\n".join(out) + "\n"
    if "--dry" in args:
        print(text)
        return
    os.makedirs(FOLDER, exist_ok=True)
    path = f"{FOLDER}/Wiring Prompt {number + 1:03d}.md"
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written:", path[len(REPO) + 1:])


if __name__ == "__main__":
    main()
