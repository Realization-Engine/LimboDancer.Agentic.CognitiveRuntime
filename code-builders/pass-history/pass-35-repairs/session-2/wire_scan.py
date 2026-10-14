import re, sys, collections
import fitz

PDF = sys.argv[1]
doc = fitz.open(PDF)
toc = doc.get_toc()
# Chapter by page, from the outline's top-level entries.
tops = [(page, title) for level, title, page in toc if level == 1]


def chapter(page):
    name = "?"
    for start, title in tops:
        if start <= page:
            name = title
    return name


rule = re.compile(r"^(\d{1,2}\.\d{1,4}[A-Z]?)\b")
head = re.compile(r"^(\d{1,2})\. [A-Z][A-Z &/'-]{3,}")
hits = collections.OrderedDict()
total = 0
for index in range(len(doc)):
    page = index + 1
    text = doc[index].get_text()
    if "wire" not in text.lower():
        continue
    current = None
    for line in text.splitlines():
        stripped = line.strip()
        m = rule.match(stripped) or head.match(stripped)
        if m:
            current = m.group(1)
        count = len(re.findall(r"wire", stripped, re.I))
        if count:
            total += count
            key = (chapter(page), page, current or "(no rule number on the page above it)")
            hits[key] = hits.get(key, 0) + count

print("pages in the PDF:", len(doc), "| instances of 'wire':", total, "| page-and-rule places:", len(hits))
by_chapter = collections.OrderedDict()
for (chap, page, number), count in hits.items():
    by_chapter.setdefault(chap, []).append((page, number, count))
for chap, rows in by_chapter.items():
    print()
    print("==", chap, "| places:", len(rows), "| instances:", sum(c for _, _, c in rows))
    line = ", ".join(f"p{page} {number}" + (f" x{count}" if count > 1 else "") for page, number, count in rows)
    print(line[:2600])
