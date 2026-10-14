"""Render the two illustrations by their image boxes."""
import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"
OUT = "E:/Archive/GitHub/dlandi/pass32-tools/pass55/agents"
doc = fitz.open(PDF)
for page, box in ((178, (295, 250, 550, 480)), (169, (12, 475, 280, 730))):
    p = doc[page - 1]
    pix = p.get_pixmap(matrix=fitz.Matrix(4, 4), clip=fitz.Rect(*box))
    pix.save("%s/f4_p%d_fig.png" % (OUT, page))
    print(page, pix.width, pix.height)
