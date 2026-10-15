"""Renders the C7.11 AF Table of p. 190 (physical page) into the scratchpad, to read the Aerial AF row (increment 2d)."""
import sys

import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"
OUT = sys.argv[1]
page = fitz.open(PDF)[189]
for block in page.get_text("blocks"):
    if "AERI" in block[4] or "C7.11" in block[4]:
        print(block[:4], block[4][:40].replace("\n", " "))
pix = page.get_pixmap(dpi=400, clip=fitz.Rect(155, 400, 345, 485))
pix.save(OUT + "/p190.png")
