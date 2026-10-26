"""Pass 70 documents: the inventory's pass 70 row label in section 2, before the count tables are rewritten."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders")
from edit_files import Edits  # noqa: E402

e = Edits()
e.edit("docs/ASL Rule Inventory A to E.md",
       "| Pass 70: Infantry III: morale, Close Combat, and concealment (proposed; A11.52, A15.1, A17.1, A18.12, and A19.11 moved here from pass 75 on 2026-10-25) | 48 |",
       "| Pass 70: Infantry III: morale, Close Combat, and concealment (built to the gate, 2026-10-25: 29 of its rows are built, 6 of them with a "
       "deviation, and 19 moved to the pass that takes what is left; A11.52, A15.1, A17.1, A18.12, and A19.11 had moved here from pass 75 that day) | 48 |")
e.apply()
