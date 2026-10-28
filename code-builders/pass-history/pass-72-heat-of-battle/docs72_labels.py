"""Pass 72 documents: the inventory's count-table labels the counting script does not write (pass 35's, which it rewrites from a fixed string, and pass 72's)."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
from edit_files import Edits  # noqa: E402

e = Edits()
INV = "docs/ASL Rule Inventory A to E.md"
e.edit(INV, "| Pass 35: Repairs: wrong results in rules already built (built 2026-10-09: 18 of its rows are built, 26 moved to the pass that takes what is left, and B14.2 moved to pass 135) | 0 |",
       "| Pass 35: Repairs: wrong results in rules already built (built 2026-10-09: 18 of its rows are built, 26 moved to the pass that takes what is left, and B14.2 moved to pass 135, pass 130 since 2026-10-16) | 0 |")
e.edit(INV, "| Pass 72: Infantry V: Heat of Battle and Unit Substitution (proposed; pass 75 until the split of 2026-10-26) | 0 |",
       "| Pass 72: Infantry V: Heat of Battle and Unit Substitution (built to the gate, 2026-10-27: 5 of its rows are built, 3 of them with a deviation, and 5 moved to the pass that takes what is left; pass 75 until the split of 2026-10-26) | 0 |")
e.apply()
