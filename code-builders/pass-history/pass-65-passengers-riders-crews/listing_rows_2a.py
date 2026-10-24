"""Pass 65, increment 2a: the Chapter H listing rows of the five non-campaign vehicles, span by span with each span's colour and font.

Reads the registered rulebook PDF (physical pages) and prints every span on the row's baseline, so the Type column and whether the CS# is
printed red can be read from the PDF's own drawing data. Renders each row to the scratchpad given as the first argument, to be viewed.
"""
import sys

import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"
ROWS = [(337, "PzKpfw IIIH"), (339, "Opel 6700"), (339, "SPW 251/1"), (355, "T-34 M41"), (356, "GAZ-MM")]
OUT = sys.argv[1] if len(sys.argv) > 1 else None

document = fitz.open(PDF)
for number, name in ROWS:
    page = document[number - 1]
    hits = page.search_for(name)
    print("== p. %d %r: %d hits" % (number, name, len(hits)))
    # The listings are printed sideways: a vehicle's row is a vertical strip of the page, read from the spans that share its x band.
    for hit in hits:
        x0, x1 = hit.x0 - 1, hit.x1 + 1
        spans = []
        for block in page.get_text("dict")["blocks"]:
            for line in block.get("lines", []):
                for span in line["spans"]:
                    box = fitz.Rect(span["bbox"])
                    if box.x1 > x0 and box.x0 < x1:
                        spans.append((round(box.y0), span["text"], "#%06x" % span["color"], span["font"]))
        for y, text, color, font in sorted(spans, reverse=True):
            print("   y=%4d %-10s %-28s %r" % (y, color, font, text))
        if OUT:
            clip = fitz.Rect(hit.x0 - 30, page.rect.y0, hit.x1 + 6, page.rect.y1)
            page.get_pixmap(dpi=220, clip=clip).save("%s/p%d-%s.png" % (OUT, number, name.replace(" ", "_").replace("/", "-")))
