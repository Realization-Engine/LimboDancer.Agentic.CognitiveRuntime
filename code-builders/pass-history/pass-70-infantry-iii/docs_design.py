"""Pass 70 documents: correct R70.13's citation in the design (3d's finding). Every anchor is checked before any file is written."""
import sys

sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders/documents")
sys.path.insert(0, "E:/Archive/GitHub/dlandi/LimboDancer.MCP/code-builders")
from edit_files import Edits  # noqa: E402

e = Edits()
DESIGN = "docs/Passes/ASL Unit Backlog Pass 70 Design.md"
e.edit(DESIGN,
       "- **R70.13:** a routing unit may ignore the hexes of the building it starts in when choosing its destination (A10.532's EXC, p. 67).",
       "- **R70.13:** a routing unit may ignore the hexes of the building it starts in when choosing its destination (A10.51's EXC, pp. 66 to 67). "
       "(Its citation corrected at the documents step: the frame wrote \"A10.532's EXC, p. 67\"; the 3d agent read on the page that the exception is "
       "A10.51's, which runs from p. 66 to the top of p. 67, and the code cites A10.51. Section 11.1.)")
e.apply()
