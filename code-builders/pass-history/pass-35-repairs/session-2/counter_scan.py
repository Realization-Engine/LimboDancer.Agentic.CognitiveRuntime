import re, sys, collections, json
import fitz

PDF, INVENTORY, OUT = sys.argv[1], sys.argv[2], sys.argv[3]
TERMS = collections.OrderedDict([
    ("Wire", r"\bwire\b"),
    ("Foxhole", r"\bfoxholes?\b"),
    ("Trench", r"\btrench(es)?\b"),
    ("Minefield", r"\b(minefields?|mines?|A-T mines?)\b"),
    ("Roadblock", r"\broadblocks?\b"),
    ("Pillbox", r"\bpillbox(es)?\b"),
    ("Fortified Building Location", r"\bfortified\b"),
    ("Flame", r"\bflames?\b"),
    ("Rubble", r"\brubble\b"),
])

# The inventory: each row's rule, page, status, pass, and whole text.
rows = {}
for line in open(INVENTORY, encoding="utf-8"):
    if line.startswith("| ") and re.match(r"\| [A-E]\.?\d", line):
        cells = [cell.strip() for cell in line.split("|")]
        rows[cells[1]] = (int(cells[3]) if cells[3].isdigit() else None, cells[4], cells[7], line)
pages = collections.defaultdict(list)
for number, (page, _, _, _) in rows.items():
    if page:
        pages[number[0]].append(page)
span = {letter: (min(found), max(found)) for letter, found in pages.items()}
print("chapter pages from the inventory:", span)


def chapter(page):
    for letter, (first, last) in sorted(span.items(), key=lambda item: item[1][0], reverse=True):
        if page >= first:
            return letter if page <= last else "after E"
    return "index"


doc = fitz.open(PDF)
rule = re.compile(r"^(\d{1,2}\.\d{1,4})\b")
found = {term: collections.OrderedDict() for term in TERMS}
counts = collections.Counter()
for index in range(len(doc)):
    page = index + 1
    letter = chapter(page)
    current = None
    for line in doc[index].get_text().splitlines():
        stripped = line.strip()
        m = rule.match(stripped)
        if m:
            current = m.group(1)
        for term, pattern in TERMS.items():
            n = len(re.findall(pattern, stripped, re.I))
            if n:
                counts[(term, letter)] += n
                if letter in "ABCDE" and current:
                    found[term][letter + current] = page

result = {}
for term in TERMS:
    rules = found[term]
    known = [number for number in rules if number in rows]
    noted = [number for number in known if re.search(TERMS[term], rows[number][3], re.I)]
    silent = [number for number in known if number not in noted]
    unknown = [number for number in rules if number not in rows]
    result[term] = {"instances": {letter: counts[(term, letter)] for letter in ("index", "A", "B", "C", "D", "E", "after E")},
                    "rules": len(rules), "inInventory": len(known), "noted": len(noted), "silent": silent, "notRows": unknown,
                    "silentBuilt": [number for number in silent if rows[number][1] in ("built", "built with a deviation")]}
    print()
    print(term, result[term]["instances"])
    print("  rules A to E that mention it:", len(rules), "| rows found:", len(known), "| rows that note it:", len(noted), "| silent rows:", len(silent),
          "| of them marked built:", len(result[term]["silentBuilt"]), "| numbers with no row:", len(unknown))
    print("  silent:", ", ".join(silent)[:900])
json.dump(result, open(OUT, "w", encoding="utf-8"), indent=1)
