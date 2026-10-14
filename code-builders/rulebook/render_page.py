"""Render a region of a PDF page to a PNG in the scratchpad: render_page.py <page> <out.png> [x0 y0 x1 y1 as fractions] [zoom]"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import RULEBOOK_PDF  # noqa: E402
import sys

import fitz

PDF = RULEBOOK_PDF
doc = fitz.open(PDF)
page = doc[int(sys.argv[1]) - 1]
rect = page.rect
if len(sys.argv) >= 7:
    x0, y0, x1, y1 = [float(value) for value in sys.argv[3:7]]
    rect = fitz.Rect(rect.x0 + rect.width * x0, rect.y0 + rect.height * y0, rect.x0 + rect.width * x1, rect.y0 + rect.height * y1)
zoom = float(sys.argv[7]) if len(sys.argv) >= 8 else 2.5
page.get_pixmap(matrix=fitz.Matrix(zoom, zoom), clip=rect).save(sys.argv[2])
print("rendered", sys.argv[2], rect)
