"""Pass 70 documents: restore the inventory's pass 35 label, which inventory_count_tables.py rewrites from a fixed string older than the
renumbering of 2026-10-16 (it drops ", pass 130 since 2026-10-16")."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders")
from edit_files import Edits  # noqa: E402

e = Edits()
e.edit("docs/ASL Rule Inventory A to E.md",
       "26 moved to the pass that takes what is left, and B14.2 moved to pass 135) | 0 |",
       "26 moved to the pass that takes what is left, and B14.2 moved to pass 135, pass 130 since 2026-10-16) | 0 |")
e.apply()
