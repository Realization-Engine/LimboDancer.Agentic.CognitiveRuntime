"""The gate's result in the documents: the design's section 15 replaced (a fragment file), the design's status and the prompt's notes on the
baseline corrected, a backlog row for the halftrack's refusal, and the time log's row. docs_gate.py <section 15 fragment>."""
import sys
from pathlib import Path

D = Path(__file__).resolve().parents[3] / "src" / "ASL" / "docs"
DESIGN, PROMPT, BACKLOG, LOG = (D / "Passes" / "ASL Unit Backlog Pass 60 Design.md", D / "Prompts" / "Pass 60 Prompt II.md",
                                D / "ASL Unit Backlog.md", D / "ASL Unit Time Log.md")
files = {path: path.read_text(encoding="utf-8") for path in (DESIGN, PROMPT, BACKLOG, LOG)}
assert all("\r\n" not in text for text in files.values())


def replace(path, old, new):
    assert files[path].count(old) == 1, (path.name, files[path].count(old), old[:70])
    files[path] = files[path].replace(old, new)


design = files[DESIGN]
start = design.index("## 15. The gate")
files[DESIGN] = design[:start] + Path(sys.argv[1]).read_text(encoding="utf-8")
replace(DESIGN, "**The gate is not run:** it is the user's to start (section 15).",
        "**The gate passed** on 2026-10-15 at the user's word (section 15); the push, the pull request, and the merge are the user's to start.")

replace(PROMPT, "`pass60.conf` now names the pass's 23 recorded games, and the baseline's key holds that list, so the `baseline` stage will make the older games' baseline again once (about eight minutes) before its own part.",
        "`pass60.conf` names the pass's 23 recorded games.")
replace(PROMPT, "On 2026-10-15 the user said: \"this is my word to start pass 60, run without my intervention up to the gate.\" That word is used up at the gate.",
        "On 2026-10-15 the user said: \"this is my word to start pass 60, run without my intervention up to the gate.\" That word is used up at the gate. "
        "Later that day the user said \"Start the gate: run baseline, build, and proofs for pass 60, and stop before the push\": the gate passed "
        "(design, section 15), its `SKIP_JSON` named after the first proofs run stopped at the early check.")
replace(PROMPT, "The gate's stages (`baseline`, `build`, `proofs`), the push of the branch and its pull request, CI's run of it, the merge, and any closing commit are each the user's to start by name.",
        "The push of the branch and its pull request, CI's run of it, the merge, and any closing commit are each the user's to start by name.")
replace(PROMPT, "- The proofs' text list will differ in the pass's new texts and changed refusal sentences; the sweep may show refusals turning into plans in older games where a pass 60 refusal stood (design, section 15). Each such line is read as a task's intended change or it is a fault.\n",
        "- The gate's one line that is not an intended change: a SPW 251/1's MA proposed as ordnance at a Location with no vehicle is refused with the package's generic words (backlog section 60.2).\n")

row = ("| A MG-armed vehicle's MA proposed as ordnance at a Location with no AFV is refused with the package's generic \"the Gun is not one the "
       "package reviews\", where before the pass the VCA bar refused it in words of its own | The gate's sweep, 2026-10-15 (`p35-bog`, two lines) | "
       "A9.61, D3.54 | A fix in `LiveOrdnance.FromState` or `ScenarioA1MachineGunKill.Outside`: the MG's own refusal, as a SW MG gets "
       "(`play.mg-to-kill`) | Refused, with poorer words. |\n")
backlog = files[BACKLOG]
head = backlog.index("## 60. Added by pass 60")
sub = backlog.index("### 60.2", head)
end = backlog.find("\n## ", sub + 1)
end = len(backlog) if end < 0 else end
block = backlog[sub:end].rstrip("\n") + "\n"
assert block.rstrip("\n").split("\n")[-1].startswith("| "), "the 60.2 table does not end the block"
files[BACKLOG] = backlog[:sub] + block + row + ("\n" + backlog[end:].lstrip("\n") if end < len(backlog) else "")

files[LOG] = files[LOG].rstrip("\n") + "\n\n## Pass 60: the gate\n\n| Sub-task | Start | End | Duration | Notes |\n|---|---|---|---|---|\n" + (
    "| The gate, at the user's word (\"Start the gate: run baseline, build, and proofs for pass 60, and stop before the push\"): the fetch "
    "(origin/main still 380b4a7b); baseline (20 s, the cut's baseline reused) and build (3:49) side by side; proofs stopped by the early check "
    "(five Fire plans of older games differing by the planned events' hash alone: `randomDestruction` in a JSON record); `SKIP_JSON` named, "
    "proofs again (5 min): text list 31 lines, replay digest no older line, sweep 269 answers in three groups, each read; the documents | 15:58 "
    "| 16:16 | 18 min | The gate's first whole run since its repairs of 2026-10-13: about a quarter of an hour, as the standing rules say, with "
    "the early check's stop costing two minutes. |\n")
for path, text in files.items():
    path.write_text(text, encoding="utf-8", newline="")
print("written:", ", ".join(path.name for path in files))
