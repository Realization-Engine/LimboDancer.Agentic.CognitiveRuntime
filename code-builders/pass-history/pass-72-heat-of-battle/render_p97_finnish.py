"""Render A25.72's Finnish progression chart (physical page 97) into the scratch build folder: python render_p97_finnish.py <x0> <y0> <x1> <y1> <zoom> <name>."""
import os
import sys

import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"
OUT = "C:/Users/dland/AppData/Local/Temp/claude/E--Archive-GitHub-dlandi-LimboDancer-MCP/acd6eee1-21d2-4e35-af57-4af8cdd9304b/scratchpad/build"
x0, y0, x1, y1, zoom, name = float(sys.argv[1]), float(sys.argv[2]), float(sys.argv[3]), float(sys.argv[4]), float(sys.argv[5]), sys.argv[6]
os.makedirs(OUT, exist_ok=True)
doc = fitz.open(PDF)
p = doc[97 - 1]
r = p.rect
clip = fitz.Rect(r.x0 + r.width * x0, r.y0 + r.height * y0, r.x0 + r.width * x1, r.y0 + r.height * y1)
pix = p.get_pixmap(matrix=fitz.Matrix(zoom, zoom), clip=clip)
pix.save("%s/%s.png" % (OUT, name))
print(r, pix.width, pix.height)
