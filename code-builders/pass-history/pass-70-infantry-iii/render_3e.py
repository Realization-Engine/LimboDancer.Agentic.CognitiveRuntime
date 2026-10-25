"""Render the Concealment Loss/Gain Table (A12.121, physical page 107) into the scratch folder for increment 3e."""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
from paths import RULEBOOK_PDF  # noqa: E402

import fitz  # noqa: E402

OUT = "C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/f12c93b0-4f22-4bda-84a5-33ccd7c35641/scratchpad/build"
os.makedirs(OUT, exist_ok=True)
doc = fitz.open(RULEBOOK_PDF)
page = doc[107 - 1]
r = page.rect
for name, (y0, y1) in {"table_top": (0.0, 0.45), "table_cases": (0.40, 0.75)}.items():
    clip = fitz.Rect(r.x0, r.y0 + r.height * y0, r.x1, r.y0 + r.height * y1)
    pix = page.get_pixmap(matrix=fitz.Matrix(3, 3), clip=clip)
    pix.save("%s/%s.png" % (OUT, name))
    print(name, pix.width, pix.height)
