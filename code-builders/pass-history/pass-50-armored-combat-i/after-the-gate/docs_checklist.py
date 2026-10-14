"""The standing rules after pass 50: a checklist and the rule for session prompts at the top, the gate step pointed at gate.sh, and the hook named.

Usage: python docs_checklist.py <docs root>
"""
import sys

PATH = sys.argv[1].rstrip("/") + "/Prompts/Pass Standing Rules and Harness Lessons.md"
text = open(PATH, encoding="utf-8", newline="").read()
assert "\r\n" not in text

CHECKLIST = """## 0. The checklist

Added on 2026-10-12 at the user's word, after two passes in which lessons already written below were broken again. These are the ones that cost time. Section 4 is the history they come from; this list is what is read before each step.

1. **Before a long run, a short one.** Nothing that runs more than five minutes starts until the same command has passed on a few games. For the gate that is the trial stage of `gate.sh`, which the proofs stage enforces.
2. **The gate is one script:** `bash E:/Archive/GitHub/dlandi/pass32-tools/gate/gate.sh <pass> <base commit> <stage>`, with the pass's games and JSON keys in `gate/pass<pass>.conf`. Never copy a past pass's gate script or its skip list. `PROOF_SKIP` is computed from the diff between the baseline and the head.
3. **A digest and a sweep that differ in nothing, or in every line, are a void run,** not a result. Stop and find out why before reading anything else.
4. **After changing a tool, build it and run it small at once,** not when its long run is due.
5. **One response, one step of a chain:** never an edit and the build or run that depends on it in the same response.
6. **Scripts are files.** Write a script with the Write tool and run the file: no heredoc, no multi-line `python -c`, no `sed -i`, no code or Markdown inside a quoted shell string.
7. **No `cd` into a folder of the repository.** Absolute paths, or `git -C`.
8. **`git fetch` and read `origin/main` before the baseline and again before the merge.** Main moves: a Codex session commits to it. A file changed on both sides is the user's to hear of first.
9. **`git branch --show-current` before every commit; stage explicit paths; no force push.**
10. **Read the clock before writing a time.**
11. **What the frame placed in a task and the build left out goes first** in the design's section, the review, and the report.
12. **When a step fails, say what it cost and what stops it next time,** and put the stop into a script or the hook, not only into section 4.

**The hook.** In this repository a PreToolUse hook (`.claude/settings.local.json`, script `C:/Users/dland/.claude/hooks/asl_bash_guard.py`, with its test beside it) refuses a Bash call that holds a heredoc, `sed -i`, a multi-line `python -c`, a bare `python -`, a `cd` into a folder of the repository, a `git add` of everything, or a force push. It cannot see point 5. When a slip of a new kind costs time, add a rule and a test case to it.

## 0.1 What a session prompt holds

A prompt written for the next session (the user, 2026-10-12) holds what only that session needs: the state (branch, head, what is committed and pushed), the user's word and where it ends, what is open to a veto, what the last session knows will fail or was not run, and where to start. It names this file and the gate script and does not restate them. It gives no command, no skip list, and no step of a procedure: pass 50's second prompt carried pass 45's skip list as an instruction, the list was wrong for that gate's baseline, and the sweep was run twice. If a procedure is missing or wrong, correct it here or in the script, and say in the prompt that it changed.

"""

EDITS = [
    ("## 1. How a pass runs\n", CHECKLIST + "## 1. How a pass runs\n"),
    ("6. **The merge gate**, only once step 4 passes: the pass's tests written, the solution build with warnings as errors, the full local suite, CI's Node viewport test by hand, the chart supplement regenerated, the Docker Linux check.",
     "6. **The merge gate**, only once step 4 passes, by `gate.sh` (section 0, point 2), in this order: `baseline` in the background; `build`; `trial`, which must pass; `suite` in the background; `proofs`; `docker` on the committed branch. Every differing line of the text list, the replay digest, and the sweep is named as one task's intended change or it is a fault."),
    ("**Status:** Standing. Every session prompt for a pass cites this file.",
     "**Status:** Standing. Every session prompt for a pass cites this file. Section 0 is the checklist; read it before each step, not only at the start."),
]
for old, new in EDITS:
    assert text.count(old) == 1, (old[:60], text.count(old))
    text = text.replace(old, new)
open(PATH, "w", encoding="utf-8", newline="").write(text)
print("ok")
