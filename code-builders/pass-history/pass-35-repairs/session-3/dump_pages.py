"""Dump the text layer of the rulebook's pages 43 to 253 (Chapters A to E), one file a physical page, for the read-only reviews."""
import os
import fitz

PDF = "E:/RPG/Dennis-Landi-Games/Squad Leader Apocalypse/References/AdvancedSquadLeader/wgv-2025-08-07_06-00am/eASLRB_v3_01.pdf"
OUT = "E:/Archive/GitHub/dlandi/pass32-tools/pass35/session3/pages"
os.makedirs(OUT, exist_ok=True)
doc = fitz.open(PDF)
count = 0
for number in range(43, 254):
    text = doc[number - 1].get_text()
    with open(f"{OUT}/p{number:03d}.txt", "w", encoding="utf-8", newline="\n") as out:
        out.write(text)
    count += 1
print(count, "pages ->", OUT)
