"""Dump pages of the rulebook PDF to text files, one a page, in the working folder's rulebook-pages: dump_pages.py <page>..."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import RULEBOOK_PDF, WORK  # noqa: E402
import io
import os
import sys

import fitz

PDF = RULEBOOK_PDF
OUT = WORK + "/rulebook-pages"
os.makedirs(OUT, exist_ok=True)
doc = fitz.open(PDF)
for arg in sys.argv[1:]:
    page = int(arg)
    with io.open("%s/p%03d.txt" % (OUT, page), "w", encoding="utf-8", newline="\n") as f:
        f.write(doc[page - 1].get_text())
print("dumped", len(sys.argv) - 1)
