"""Appends the last rows of the pass 32.j time log section."""
import pathlib

PATH = pathlib.Path("E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/ASL Unit Time Log.md")
text = PATH.read_bytes().decode("utf-8")
assert "## Pass 32.j" in text and "given in advance while they ran" not in text
rows = """| The gate's long runs beside each other: the working-tree sweep (513,542 lines, 2,014 s) equal; the local suite (Dice 24, Rules 703, Units 409, Maps 244, Maps.Vasl 121 with 28 skipped, Maps.Rendering 34 with 2 skipped, Units.Rendering 352, CounterSheets 19, MapStudio 381, Play 692, Authoring 167: all pass, 00:08 to 00:54); Docker on the committed branch, every project passes, five steps exit 0; the gate-build proofs (the proof rebuilt with -p:OutDir into proof-gate, copies dated with the build): text list, replay (1,420 s), and sweep (1,471 s) equal; the user's word, given in advance while they ran, to merge once the gates pass | 00:09 | 00:58 | 0:49 | The first gate proof script named a project file that is not there; rewritten and rerun |
| The documents: the inventory's Where column (41 rows, by script), the coverage's Lives in column (6 sections), backlog section 54, the plan's 22.2 (the migration complete), the standing rules' lessons, the design's section 24 with the migration's closing paragraph, this table | 23:40 | 01:02 | 0:25 | Written beside the runs |
"""
PATH.write_bytes((text.rstrip("\n") + "\n" + rows).encode("utf-8"))
print("rows appended")
