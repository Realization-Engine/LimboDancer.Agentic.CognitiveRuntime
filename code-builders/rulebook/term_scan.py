"""Scan every page of the rulebook for pass 55's terms: for each term, the pages and the nearest rule number above each mention."""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import RULEBOOK_PDF  # noqa: E402
import collections
import re
import sys

import fitz

PDF = RULEBOOK_PDF
TERMS = collections.OrderedDict([
    ("Canister", r"\bcann?ister\b"),
    ("APDS", r"\bAPDS\b"),
    ("Armor Leader", r"\barmor leaders?\b"),
    ("Motion attempt", r"\bmotion (status )?attempts?\b"),
    ("Gun Duel", r"\bgun duels?\b"),
    ("Inexperienced Crew", r"\binexperienced crews?\b"),
    ("Height Advantage", r"\bheight advantage\b"),
    ("Limited Stowage", r"\blimited stowage\b|\bAP ?# ?|\bHE ?#"),
    ("Elite depletion", r"\bC8\.2\b|\bdepletion numbers? .{0,40}elite|elite.{0,60}depletion"),
])
doc = fitz.open(PDF)
rule = re.compile(r"^(\d{1,2}\.\d{1,4})\b")
found = {t: collections.OrderedDict() for t in TERMS}
for index in range(len(doc)):
    page = index + 1
    lines = doc[index].get_text().splitlines()
    header = lines[0].strip() if lines else ""
    current = None
    for line in lines:
        s = line.strip()
        m = rule.match(s)
        if m:
            current = m.group(1)
        for term, pattern in TERMS.items():
            if re.search(pattern, s, re.I):
                found[term].setdefault((page, header, current), 0)
                found[term][(page, header, current)] += 1
out = open(sys.argv[1], "w", encoding="utf-8", newline="\n")
for term, hits in found.items():
    out.write("== %s: %d places\n" % (term, len(hits)))
    by_page = collections.OrderedDict()
    for (page, header, current), n in hits.items():
        by_page.setdefault((page, header), []).append("%s x%d" % (current, n))
    for (page, header), where in by_page.items():
        out.write("  p.%d [%s] %s\n" % (page, header, ", ".join(where)))
out.close()
