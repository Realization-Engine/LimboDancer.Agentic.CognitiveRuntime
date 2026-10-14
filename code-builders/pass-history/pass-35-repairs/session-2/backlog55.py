import io, json, sys

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
PATH = ROOT + "ASL Unit Backlog.md"
data = json.load(open(sys.argv[1], encoding="utf-8"))
text = io.open(PATH, encoding="utf-8", newline="").read()
assert "\r\n" not in text and "## 55." not in text and text.endswith("\n")

rows = []
for term, entry in data.items():
    counts = entry["instances"]
    total = sum(counts.values())
    rows.append("| %s | %d | %d | %d | %d | %d | %s |" % (
        term, total, counts["index"], counts["after E"], entry["rules"], len(entry["silent"]),
        ", ".join(entry["silent"]) + (" (marked built: %s)" % ", ".join(entry["silentBuilt"]) if entry["silentBuilt"] else "")))

SECTION = '''
## 55. Added by pass 35

Pass 35 writes its other rows here with its documents, at the pass's end. This first part was added on 2026-10-09 at the user's word, after task 35.15.

### 55.1 The counters with no rules, and what the inventory does not say of them

Task 35.15 (commit 37124f3) refuses a game that places or holds a fortification, rubble, or Flame counter. It is a stopgap: none of their rules is built, and passes 115 and 120 build them. When the user asked what a Wire counter lacks, the answer was first given from B26 alone. The rules of each counter are spread through the book, and the rule inventory records only part of that.

**How the list was made.** A script read the text of all 716 pages of the registered PDF, counted each word, and gave each mention the nearest rule number above it on its page. The chapter came from the inventory's own page range for it. Each rule so found was then looked up in the inventory, and a row was called silent when its text does not use the word. Every rule the script found in Chapters A to E has a row. The rule numbers are where the word stands on the page, by a script: only B26 was read by Claude. A mention may belong to the rule before or after, and "mines", "fortified", "flame", and above all "rubble" (which is also a terrain, built in part) catch more than the counter.

| Counter | Mentions in the PDF | In the index | After Chapter E | Rules in A to E | Silent rows | The silent rows |
|---|---:|---:|---:|---:|---:|---|
%s

| Item | Deferred by | Rules | Depends on | What happens now |
|---|---|---|---|---|
| The inventory's clauses for the counters with no rules: each silent row above read on its page, and the clause recorded in the row with its status and pass | Pass 35, 2026-10-09 (the user) | The rows above | The preparation of passes 115 and 120, before their designs | A row may be marked built for its main rule and say nothing of its Wire, mine, or pillbox clause. Tasks 35.13 e (D7.21) and 35.13 (D8.21) were built without their Wire clauses being seen (D7.211, D8.21); no game has Wire, so no result is wrong. |
| Chapters F, G, H and what follows them: the mentions after Chapter E in the table (142 for Wire, the tetrahedrons of G14.5 and the Barbed-Wire Fences of the campaign games among them) | Pass 35, 2026-10-09 | Not inventoried: the inventory covers A to E | An inventory of those chapters | Nothing reads them. The user's ruling of 2026-09-27 is that the whole rulebook is used. |
| The refusal of 35.15 is all or nothing: a card with one foxhole in a corner is refused outright | Pass 35, 2026-10-09 | B23.9, B24, B25.15, B26 to B30 | Passes 115 and 120 | Such a game is not set up and not played. It names the kinds it found, which tells both sides that the game holds, for one, a minefield, though not where. |
| Before a counter's rules are said to be known, the whole PDF is searched for it | The user, 2026-10-09 | None | Nothing | A lesson for the standing rules, written with the pass's documents. |
''' % "\n".join(rows)

io.open(PATH, "w", encoding="utf-8", newline="").write(text + SECTION)
print("ok", len(rows))
