"""A self-test of documents/settle_documents.py: two claims edit the shared documents in a scratch repository, one branch merges main,
and the tool must settle every conflict but the one a person has to decide.

    python settle_selftest.py        prints each check and exits 1 if any fails
"""
import os
import subprocess
import sys
import tempfile

TOOL = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "documents", "settle_documents.py")
INVENTORY = "src/ASL/docs/ASL Rule Inventory A to E.md"
BACKLOG = "src/ASL/docs/ASL Unit Backlog.md"
PLAN = "src/ASL/docs/ASL Card Play and Map Studio Redesign Plan.md"
TIMELOG = "src/ASL/docs/ASL Unit Time Log.md"
PROSE = "src/ASL/docs/Notes.md"

BASE = {
    INVENTORY: """# Inventory
## 2. The counts
| Chapter | built | total |
|---|---|---|
| C | 10 | 20 |
## 4. The rules
| § | Title | Page | Status | Minigames | Owner | Claim state |
|---|---|---|---|---|---|---|
| C9.3 | Spotters | 186 | built |  | C9.3 (Claude) | claimed |
| C9.31 | Spotter loss | 186 | built |  | C9.3 (Claude) | claimed |
| C9.4 | Mortar fire | 186 | not built |  |  | open |
## 6. Claims
| Claim | Agent | Calculator files | State |
|---|---|---|---|
| C9.3 | Claude | A.cs, Ordnance.cs | claimed |
""",
    PLAN: """**Pass 110: Ordnance**

> **Rules status** (written): 3 rows: 2 claimed, 1 open.
""",
    BACKLOG: """# Backlog
## 80. Added by the C9.3 claim
| Item |
|---|
| one |
""",
    TIMELOG: """| Task | Start |
|---|---|
| task 5 | 20:34 |
""",
    PROSE: """The board opens with task 6.
""",
}

OURS = {  # the claim branch: C9.3 built
    INVENTORY: BASE[INVENTORY]
    .replace("| C 10 |", "| C 10 |")
    .replace("| C | 10 | 20 |", "| C | 12 | 20 |")
    .replace("| C9.3 | Spotters | 186 | built |  | C9.3 (Claude) | claimed |", "| C9.3 | Spotters | 186 | built | c9-spotter | C9.3 (Claude) | built |")
    .replace("| C9.31 | Spotter loss | 186 | built |  | C9.3 (Claude) | claimed |", "| C9.31 | Spotter loss | 186 | built |  | C9.3 (Claude) | built |")
    .replace("| C9.3 | Claude | A.cs, Ordnance.cs | claimed |", "| C9.3 | Claude | A.cs, Ordnance.cs | built |"),
    PLAN: BASE[PLAN].replace("2 claimed, 1 open", "2 built, 1 open"),
    BACKLOG: BASE[BACKLOG] + "## 81. Added by the A24.5 claim\n| Item |\n|---|\n| smoke |\n",
    TIMELOG: BASE[TIMELOG] + "| C9.3 claim | 22:55 |\n",
    PROSE: "The board opens with task 6, after the proving slice.\n",
}

THEIRS = {  # main: Codex claims C9.4; C9.3's ordnance file is marked shared
    INVENTORY: BASE[INVENTORY]
    .replace("| C | 10 | 20 |", "| C | 11 | 20 |")
    .replace("| C9.4 | Mortar fire | 186 | not built |  |  | open |", "| C9.4 | Mortar fire | 186 | not built |  | C9.4 (Codex) | claimed |")
    .replace("| C9.3 | Claude | A.cs, Ordnance.cs | claimed |",
             "| C9.3 | Claude | A.cs, Ordnance.cs (shared) | claimed |\n| C9.4 | Codex | B.cs, Ordnance.cs (shared) | claimed |"),
    PLAN: BASE[PLAN].replace("2 claimed, 1 open", "3 claimed"),
    BACKLOG: BASE[BACKLOG] + "## 81. Added by the C9.4 claim\n| Item |\n|---|\n| mortar |\n",
    TIMELOG: BASE[TIMELOG] + "| C9.4 claim | 23:10 |\n",
    PROSE: "The board is open.\n",
}


def run(folder, *args):
    return subprocess.run(args, cwd=folder, capture_output=True, text=True, encoding="utf-8")


def write(folder, files):
    for path, text in files.items():
        full = os.path.join(folder, path)
        os.makedirs(os.path.dirname(full), exist_ok=True)
        with open(full, "w", encoding="utf-8", newline="") as file:
            file.write(text)


def main():
    failures = 0

    def check(name, ok):
        nonlocal failures
        print(("ok    " if ok else "FAIL  ") + name)
        failures += 0 if ok else 1

    with tempfile.TemporaryDirectory() as folder:
        for args in (["git", "init", "-q", "-b", "main"], ["git", "config", "user.email", "t@t"], ["git", "config", "user.name", "t"],
                     ["git", "config", "core.autocrlf", "false"]):
            run(folder, *args)
        write(folder, BASE)
        run(folder, "git", "add", ".")
        run(folder, "git", "commit", "-q", "-m", "base")
        run(folder, "git", "switch", "-q", "-c", "claim")
        write(folder, OURS)
        run(folder, "git", "commit", "-q", "-am", "ours")
        run(folder, "git", "switch", "-q", "main")
        write(folder, THEIRS)
        run(folder, "git", "commit", "-q", "-am", "theirs")
        run(folder, "git", "switch", "-q", "claim")
        merge = run(folder, "git", "merge", "-q", "main")
        check("the merge stops with conflicts", merge.returncode != 0)
        out = run(folder, sys.executable, TOOL, "--repo", folder, "--no-generate")
        print(out.stdout.strip())

        def read(path):
            with open(os.path.join(folder, path), encoding="utf-8") as file:
                return file.read()

        inventory = read(INVENTORY)
        check("this branch's rows keep their state", "| C9.31 | Spotter loss | 186 | built |  | C9.3 (Claude) | built |" in inventory)
        check("this branch's Minigames cell is kept", "| C9.3 | Spotters | 186 | built | c9-spotter | C9.3 (Claude) | built |" in inventory)
        check("main's new claim on the next row is kept", "| C9.4 | Mortar fire | 186 | not built |  | C9.4 (Codex) | claimed |" in inventory)
        check("a claims row changed on both sides takes each side's cells", "| C9.3 | Claude | A.cs, Ordnance.cs (shared) | built |" in inventory)
        check("main's appended claim is kept", "| C9.4 | Codex | B.cs, Ordnance.cs (shared) | claimed |" in inventory)
        check("a count table conflict takes main's (the generator rewrites it)", "| C | 11 | 20 |" in inventory)
        check("the inventory has no markers", "<<<<<<<" not in inventory)
        check("a status line takes main's", "3 claimed" in read(PLAN))
        backlog = read(BACKLOG)
        check("both backlog sections are kept", "## 81. Added by the C9.4 claim" in backlog and "Added by the A24.5 claim" in backlog)
        check("this branch's backlog section moves to the next number", "## 82. Added by the A24.5 claim" in backlog)
        timelog = read(TIMELOG)
        check("both time log rows are kept, main's first", timelog.index("C9.4 claim") < timelog.index("C9.3 claim"))
        check("prose both sides rewrote is left for a person", "<<<<<<<" in read(PROSE))
        check("the tool says one conflict is left", out.returncode == 1 and "1 conflicts left" in out.stdout)
    print(f"failures: {failures}")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
