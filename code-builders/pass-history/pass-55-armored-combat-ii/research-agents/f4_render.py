"""Render the C8.4 illustration on page 178 (and the C3.2 CA example on 169)."""
import sys
import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"
OUT = "E:/Archive/GitHub/dlandi/pass32-tools/pass55/agents"
doc = fitz.open(PDF)
for page in (178, 169):
    p = doc[page - 1]
    r = p.rect
    print(page, r)
    # lower half of the page, where the examples sit
    clip = fitz.Rect(r.x0, r.y0 + r.height * 0.55, r.x1, r.y1)
    pix = p.get_pixmap(matrix=fitz.Matrix(2.2, 2.2), clip=clip)
    pix.save("%s/f4_p%d_lower.png" % (OUT, page))
    for b in p.get_text("blocks"):
        t = b[4].strip().replace("\n", " ")
        if t.startswith("EX:") or "<image" in t:
            print(page, [round(x) for x in b[:4]], t[:60])
    for img in p.get_image_info():
        print(page, "image", [round(x) for x in img["bbox"]])
