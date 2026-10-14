"""The standing rules after the tools came into the repository: where the gate and the tools are. Every anchor is checked first.

Usage: python standing_rules_paths.py <repository root>
"""
import sys

PATH = sys.argv[1].rstrip("/") + "/src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md"
with open(PATH, encoding="utf-8", newline="") as f:
    text = f.read()
assert "\r\n" not in text
EDITS = [
    ("2. **The gate is one script:** `bash E:/Archive/GitHub/dlandi/pass32-tools/gate/gate.sh <pass> <base commit> <stage>`, with the pass's games and JSON keys in `gate/pass<pass>.conf`.",
     "2. **The gate is one script:** `bash code-builders/merge-gate/gate.sh <pass> <base commit> <stage>`, with the pass's games and JSON keys in `code-builders/merge-gate/pass-configs/pass<pass>.conf`."),
    ("`E:/Archive/GitHub/dlandi/pass32-tools/pass35/counter_scan.py`", "`code-builders/rulebook/counter_scan.py`"),
    ("- **The rules projects** are `LimboDancer.Domains.Asl.Rules` and its tests; both are in the solution.",
     "- **The tools are in the repository, under `code-builders`** (the user, 2026-10-13, reversing an early rule not to commit them). Its README has the layout: the merge gate, the proof tool, the diagnostics, the build and test helpers, the test game library, the rulebook and document tools, and `pass-history`, which holds each pass's one-time scripts as they ran. What the tools write goes to a working folder outside the repository, `E:/Archive/GitHub/dlandi/asl-gate-work`, which was named `pass32-tools` until that day. A lesson of section 4 that names a script as `pass50/dump_fire.sh` or `pass55/base.sh` means the file of that name in the pass's folder of `code-builders/pass-history`; a lesson that names `gate/...` means `code-builders/merge-gate`. A new tool, and every script a pass writes, is written into `code-builders` and committed with the pass.\n"
     "- **The rules projects** are `LimboDancer.Domains.Asl.Rules` and its tests; both are in the solution."),
    ("- **Scripts and edits.** Never a Bash heredoc for a script or an edit. Write the script with the Write tool into the scratchpad and run it.",
     "- **Scripts and edits.** Never a Bash heredoc for a script or an edit. Write the script with the Write tool and run it: a script that makes or checks part of the codebase goes into the pass's folder of `code-builders/pass-history` and is committed; only a throwaway look goes into the scratchpad."),
]
for old, new in EDITS:
    assert text.count(old) == 1, (old[:70], text.count(old))
    text = text.replace(old, new)
with open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("written")
