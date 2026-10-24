"""Pass 65, increment 2a: the text colours used on the four Chapter H listing pages, so a red CS# would show as a colour of its own.

Prints each colour with its count and a few of the spans drawn in it.
"""
import collections

import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"

document = fitz.open(PDF)
for number in (337, 339, 355, 356):
    colors = collections.defaultdict(list)
    for block in document[number - 1].get_text("dict")["blocks"]:
        for line in block.get("lines", []):
            for span in line["spans"]:
                if span["text"].strip():
                    colors["#%06x" % span["color"]].append(span["text"].strip())
    print("== p. %d" % number)
    for color, texts in sorted(colors.items(), key=lambda item: -len(item[1])):
        print("   %s %5d %s" % (color, len(texts), texts[:12]))
