"""Step 0, question 8: the rule inventory's rows beyond Chapter E, read from the registered PDF (eASLRB v3.01).

The rows of A to E were made from the PDF's outline, checked against the rule heads printed on each page (section 1 of the
inventory). The same is done here for F (pp. 254 to 270), G (pp. 276 to 322), H1, the DYO purchase (pp. 328 to 335), and W
(pp. 656 to 669); the Chapter H Notes get their rows as track 1 brings each counter into the catalog (question 8), and the chapters'
footnotes and tables are not rules. Appendix 2, the ASOP (pp. 678 to 681), has no outline entries; its steps come from the heads.

A head is a line whose first span is bold Times (Times-Bold in F, W, and the ASOP; TimesNewRoman,Bold in G and H) at 8.5 to 12.5 points and begins with a rule number followed by text (page running
heads are numbers alone, and margin markers are another font). A row takes the page of its head; an outline entry with no head found
keeps the outline's page and says so.

    python rows_beyond_e.py                dry run: counts and every disagreement between the outline and the heads
    python rows_beyond_e.py --apply        writes the rows into the inventory, before its section 5
"""
import collections
import os
import re
import string
import sys

import fitz

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(HERE, "..", "..", "documents"))
from paths import RULEBOOK_PDF  # noqa: E402
import inventory_table  # noqa: E402

EDITION = "eASLRB v3.01"
TODAY = "2026-10-28"
CHAPTERS = [
    # key, outline chapter title prefix, heading, scanned pages, the claim wave's Pass or ruling cell
    ("F", "F.", "Chapter F: North Africa", range(254, 271)),
    ("G", "G.", "Chapter G: Pacific Theater", range(276, 323)),
    ("H", "H.", "Chapter H: Design Your Own (H1, the purchase)", range(328, 336)),
    ("W", "W.", "Chapter W: Korean War", range(656, 670)),
    ("ASOP", None, "Appendix 2: Advanced Sequence of Play", range(678, 682)),
]
WAVE_1 = "Claim wave 1 (question 8)"
WAVE_2 = "Claim wave 2 (question 8)"
DYO = "Passes 190 and 195, the DYO purchase (question 8)"
NUMBER = r"(?:[A-Z]\.\d+[A-Z]{0,2}(?![A-Za-z])|\d{1,2}(?:\.\d+)+[A-Z]{0,2}(?![A-Za-z])|\d{1,2})"
OUTLINE_TITLE = re.compile(rf"^({NUMBER})\.?(?:\s+(.*))?$")
HEAD = re.compile(rf"^({NUMBER})\.?\s+(\S.*)$")
# The outline's number where the page prints another (read on the page, 2026-10-28).
OUTLINE_CORRECTIONS = {("W", "W.6BG"): "W.6B"}
# Titles of rules the outline lacks, where the text layer runs the head's words together or the head has no title before its colon.
TITLES = {"G18.62": "CC", "W10.132": "DYO", "H1.49": "Ammo Vehicles", "H1.441": "(none)", "G17.111": "(none)", "G18.1": "(none)"}


# The Turns track's G10 calculators (package D3: D3.1 done, D3.2 in progress), by the rules each file cites; recorded as in record_turns.py.
G10_FILES = {
    "TurnHorseRules.cs": ["G10.1"],
    "TurnPackingCasualtyRules.cs": ["G10.4", "G10.41"],
    "TurnPackingPeriod.cs": ["G10.11", "G10.3", "G10.31", "G10.41"],
    "TurnPackingRules.cs": ["G10.1", "G10.11", "G10.2", "G10.3", "G10.33", "G10.4", "G10.41", "G10.61", "G10.7"],
    "TurnPackingVulnerabilityRules.cs": ["G10.3", "G10.31", "G10.33", "G10.4", "G10.41", "G10.7"],
    "TurnWorkRules.cs": ["G10.11", "G10.3"],
}


def rules_calculator(rule):
    files = [name for name, rules in G10_FILES.items() if rule in rules]
    return f"Turns work, D3.1 and D3.2 (not read against the PDF): {'; '.join(files)}" if files else ""


def wave(key, number):
    if key == "H":
        return DYO
    if key == "ASOP" or (key == "G" and (number == "10" or number.startswith("10."))):
        return WAVE_1
    return WAVE_2


def rule_id(key, number):
    if key == "ASOP":
        return f"ASOP {number}"
    return number if number[0].isalpha() else key + number


def order(number):
    """Rulebook order: the chapter's introduction (F.1A) first, then by section, then the decimal part as text (1.1 < 1.101 < 1.11)."""
    intro = number[0].isalpha()
    body = number[2:] if intro else number
    m = re.match(r"^(\d+)(?:\.(\d+))?([A-Z]*)$", body) or re.match(r"^(\d+)(?:\.([\d.]+))?([A-Z]*)$", body)
    section, frac, suffix = int(m.group(1)), m.group(2) or "", m.group(3)
    return (0 if intro else 1, section, frac, suffix)


def outline(doc):
    entries = collections.defaultdict(dict)
    chapter = None
    for level, title, page in doc.get_toc():
        if level == 2:
            chapter = next((c for c in CHAPTERS if c[1] and title.startswith(c[1])), None)
            continue
        if chapter is None or level < 3:
            continue
        key, _, _, pages = chapter
        if page not in pages:
            continue
        m = OUTLINE_TITLE.match(title.strip())
        if not m:
            continue
        number, text = m.group(1), (m.group(2) or "").strip()
        number = OUTLINE_CORRECTIONS.get((key, number), number)
        entries[key].setdefault(number, (text, page))
    return entries


def heads(doc):
    """Every head line of each chapter's pages: number -> [(title, page, size)], in page order."""
    found = collections.defaultdict(lambda: collections.defaultdict(list))
    for key, _, _, pages in CHAPTERS:
        for pno in pages:
            for block in doc[pno - 1].get_text("dict")["blocks"]:
                for line in block.get("lines", []):
                    spans = [span for span in line["spans"] if span["text"].strip()]
                    if not spans:
                        continue
                    first = spans[0]
                    if not first["font"].startswith("Times") or "Bold" not in first["font"] or not 8.5 <= first["size"] <= 12.5:
                        continue
                    # A number printed as its own bold span runs into the text with no space ("3.32AMay move", ASOP p. 679).
                    text = (spans[0]["text"].strip() + " " + "".join(span["text"] for span in spans[1:])).replace(" ", " ").strip()
                    m = HEAD.match(text)
                    if not m:
                        continue
                    number, rest = m.group(1), m.group(2)
                    if key == "ASOP":
                        title = rest.rstrip(":").strip() if len(rest) <= 60 else rest[:60].rsplit(" ", 1)[0] + " ..."
                    elif ":" in rest[:60]:
                        title = string.capwords(rest.split(":")[0].strip().rstrip("†*0123456789").strip().lower())
                    else:
                        title = "(none)"
                    found[key][number].append((title, pno, round(first["size"], 1)))
    return found


def pick(occurrences, outline_page):
    """The head of a number seen more than once: the one on the outline's page, else the first at body size (9 points), else the first."""
    for occurrence in occurrences:
        if occurrence[1] == outline_page:
            return occurrence
    return next((occurrence for occurrence in occurrences if occurrence[2] == 9.0), occurrences[0])


def build(doc):
    out = collections.OrderedDict()
    report = []
    toc, scanned = outline(doc), heads(doc)
    for key, _, heading, _ in CHAPTERS:
        rows = {}
        for number, (title, page) in toc.get(key, {}).items():
            occurrences = scanned.get(key, {}).get(number)
            head = pick(occurrences, page) if occurrences else None
            if head is None:
                rows[number] = (title, page, "the outline's page; no head was found on the page")
                if key != "ASOP":
                    report.append(f"{key}: outline only {number} {title!r} p.{page}")
            else:
                rows[number] = (title or head[0], head[1], "")
                if head[1] != page:
                    report.append(f"{key}: page {number}: outline {page}, head {head[1]}")
        for number, occurrences in scanned.get(key, {}).items():
            if number not in rows:
                title, page, _ = pick(occurrences, None)
                title = TITLES.get(rule_id(key, number), title)
                rows[number] = (title, page, "" if key == "ASOP" else "a rule head the outline lacks")
                if key != "ASOP":
                    report.append(f"{key}: head only {number} {title!r} p.{page}")
        try:
            out[key] = (heading, sorted(rows.items(), key=lambda item: order(item[0])))
        except AttributeError as error:
            sys.exit(f"{key}: a number does not sort: {error}")
    return out, report


def section_of(number):
    return number[:1] + "." if number[0].isalpha() else number.split(".")[0]


def markdown(chapters):
    lines = []
    for key, (heading, rows) in chapters.items():
        lines += [f"### {heading}", ""]
        groups = collections.OrderedDict()
        for number, row in rows:
            groups.setdefault(section_of(number), []).append((number, row))
        for section, members in groups.items():
            pages = sorted({row[1] for _, row in members})
            span = f"page {pages[0]}" if len(pages) == 1 else f"pages {pages[0]} to {pages[-1]}"
            first_number, (first_title, _, _) = members[0]
            if section.endswith("."):
                name = f"{key}. Introduction"
            else:
                head = next((row[0] for number, row in members if number == section), first_title)
                name = f"{rule_id(key, section)} {head}".rstrip()
            lines += [f"#### {name} ({span})", "", f"{len(members)} rows, not audited.", "",
                      "| § | Title | Page | Status | Where | Rules calculator | Test | Pass or ruling | Owner | Claim state | Minigames | Note | By |",
                      "|---|---|---:|---|---|---|---|---|---|---|---|---|---|"]
            for number, (title, page, how) in members:
                note = f"Added from the PDF ({EDITION}) on {TODAY}, not audited" + (f": {how}." if how else ".")
                title = (title or "(none)").replace("|", "/")
                rule = rule_id(key, number)
                lines.append("| " + " | ".join([rule, title, str(page), "not audited", "", rules_calculator(rule), "", wave(key, number), "", "open", "",
                                                note, "script"]) + " |")
            lines.append("")
    return lines


def main():
    doc = fitz.open(RULEBOOK_PDF)
    chapters, report = build(doc)
    for key, (heading, rows) in chapters.items():
        waves = collections.Counter(wave(key, number) for number, _ in rows)
        print(f"{heading}: {len(rows)} rows {dict(waves)}")
    g10 = {rule for rules in G10_FILES.values() for rule in rules}
    missing = g10 - {rule_id("G", number) for number, _ in chapters["G"][1]}
    if missing:
        sys.exit(f"G10 rules the Turns files cite and the rows lack: {sorted(missing)}")
    print(f"disagreements between the outline and the heads: {len(report)}")
    for item in report:
        print("  " + item)
    if "--apply" not in sys.argv:
        print("dry run: nothing written")
        return
    lines, nl = inventory_table.read_lines()
    if any(line.startswith("### Chapter F") for line in lines):
        sys.exit("the inventory already has the rows beyond Chapter E")
    at = next(i for i, line in enumerate(lines) if line.startswith("## 5. "))
    lines[at:at] = markdown(chapters)
    open(inventory_table.PATH, "w", encoding="utf-8", newline="").write(nl.join(lines))
    print("written")


if __name__ == "__main__":
    main()
