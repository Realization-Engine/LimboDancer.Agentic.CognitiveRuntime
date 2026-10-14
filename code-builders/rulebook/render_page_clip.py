"""Render a clip of a PDF page to PNG: f_render.py <page> <x0> <y0> <x1> <y1> <zoom> <name> (fractions of the page)."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, RULEBOOK_PDF, WORK  # noqa: E402
import sys

import fitz

PDF = RULEBOOK_PDF
OUT = WORK + "/page-renders"
page, x0, y0, x1, y1, zoom, name = int(sys.argv[1]), float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5]), float(sys.argv[6]), sys.argv[7]
doc = fitz.open(PDF)
p = doc[page - 1]
r = p.rect
clip = fitz.Rect(r.x0 + r.width * x0, r.y0 + r.height * y0, r.x0 + r.width * x1, r.y0 + r.height * y1)
pix = p.get_pixmap(matrix=fitz.Matrix(zoom, zoom), clip=clip)
pix.save("%s/%s.png" % (OUT, name))
print(r, pix.width, pix.height)
