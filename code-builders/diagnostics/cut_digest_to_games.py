"""Cut a replay digest down to the games named in a list: ref_cut.py <digest> <games file> <out>"""
import io
import sys

games = set(line.strip() for line in io.open(sys.argv[2], encoding="utf-8") if line.strip())
n = 0
with io.open(sys.argv[1], encoding="utf-8", newline="") as source, io.open(sys.argv[3], "w", encoding="utf-8", newline="") as out:
    for line in source:
        if line.split("\t", 1)[0] in games:
            out.write(line)
            n += 1
print("reference lines kept:", n, "for", len(games), "games")
