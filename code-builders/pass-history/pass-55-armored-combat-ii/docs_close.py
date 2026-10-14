"""Pass 55's documents, the session's part: the Studio check, the two findings fixed after the documents were drafted (the duel answer's
summary; C13.8's bar), and the replay digest after the reviews' fixes. Usage: docs_close.py "<digest sentence>"."""
import io
import sys

D = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
DESIGN = D + "Passes/ASL Unit Backlog Pass 55 Design.md"
REVIEW = D + "Passes/Scenario A1 Backlog Pass 55 Review 2026-10-13.md"
BACKLOG = D + "ASL Unit Backlog.md"
DIGEST = sys.argv[1]
STUDIO = io.open("E:/Archive/GitHub/dlandi/pass32-tools/pass55/studio_check.md", encoding="utf-8").read().strip()

OPEN_D = ("**One finding was open when this was written.** The fixing agent found that the answer to a duel's choice may show the DEFENDER's "
          "unrevealed Armor Leader in the declared shot's summary, and it was sent back to be checked and fixed. (Its outcome is written by the session.)")
OPEN_R = ("**One finding was open when this was written:** the answer to a duel's choice may show the DEFENDER's unrevealed Armor Leader in the "
          "declared shot's summary. It was sent back to the fixing agent. (Its outcome is written by the session.)")
CLOSED = ("**Two findings came after the five reviews, and both are fixed.** The fixing agent found that the answer to a duel's choice showed the "
          "DEFENDER's unrevealed Armor Leader in the declared shot's summary: the leak was real, and both answers now word the declared shot "
          "without his line (`ScenarioA1ArmorLeaders.ToldToTheEnemy`); the shot is still fired with his modifier, and its record reveals him. "
          "And the documents' agent found that C13.8's bar (p. 185) was missing: with levels opened, a PF or PSK, which C2.6 frees of its limit, "
          "could fire at a target two or more levels higher in an adjacent hex. It is refused there and at a higher Location of its own hex, "
          "with the rule in the refusal (`BackblastElevationRefusal`); an ATR is not barred. Before the pass every ordnance shot between levels "
          "was refused, so no record holds such a shot. A LATW fired down within its own hex stays refused, which is stricter than the rules; "
          "it has a backlog row.")
C138 = ("| (the documents) | The C13.8 bar (p. 185) on a PF or PSK at a target two or more levels higher in an adjacent hex, or directly above. "
        "A LATW is free of C2.6's limit, and a search of Rules and Play for the bar found none; before the pass every ordnance shot at another "
        "level was refused. Found while the documents were written, and not tried in a game |")
C138_NEW = ("| (the session) | A LATW fired at a lower Location of its own hex is refused as out of range, though neither C13.8 nor C2.6 bars "
            "it. Stricter than the rules; found with the C13.8 fix, and not tried in a game |")
PLACE = "(written by the session after the check)"
DIGEST_OLD = "The run after the reviews' fixes: (written by the session)."
B215 = ("| The C13.8 bar on a PF or PSK firing at a target two or more levels higher in an adjacent hex, or directly above | R9.8, R9.11 | "
        "C13.8, p. 185 | Levels in fire (pass 10.2) | Not reached while fire at another level is undecided. |")
B215_NEW = ("| The C13.8 bar on a PF or PSK firing at a target two or more levels higher in an adjacent hex, or directly above | R9.8, R9.11 | "
            "C13.8, p. 185 | Levels in fire (pass 10.2) | Not reached while fire at another level is undecided. **Built in pass 55 (design "
            "section 14.2):** refused with the rule in the refusal, once fire at another level was opened. |")
B1105_START = "| The C13.8 bar on a PF or PSK at a target two or more levels higher in an adjacent hex, or directly above | Pass 55, found while the documents were written"
B1105_NEW = ("| A LATW fired at a lower Location of its own hex | Pass 55, found with the C13.8 fix, 2026-10-13; not tried in a game | C13.8, "
             "p. 185; C2.6, p. 168 | Pass 110 | Refused as out of range (range 0), above and below; C13.8 bars only a target directly above, "
             "and C2.6 exempts SW. Stricter than the rules. |")

files = {}
for path in (DESIGN, REVIEW, BACKLOG):
    files[path] = io.open(path, encoding="utf-8", newline="").read()
    assert "\r\n" not in files[path], path

checks = [(DESIGN, OPEN_D, 1), (DESIGN, C138, 1), (DESIGN, PLACE, 1), (DESIGN, DIGEST_OLD, 1),
          (REVIEW, OPEN_R, 1), (REVIEW, C138, 1), (REVIEW, PLACE, 1), (REVIEW, DIGEST_OLD, 1),
          (BACKLOG, B215, 1)]
for path, old, count in checks:
    assert files[path].count(old) == count, (path[-40:], files[path].count(old), old[:70])
lines = [line for line in files[BACKLOG].split("\n") if line.startswith(B1105_START)]
assert len(lines) == 1, len(lines)

for path, open_text in ((DESIGN, OPEN_D), (REVIEW, OPEN_R)):
    text = files[path]
    text = text.replace(open_text, CLOSED).replace(C138, C138_NEW).replace(PLACE, STUDIO).replace(DIGEST_OLD, DIGEST)
    files[path] = text
files[BACKLOG] = files[BACKLOG].replace(B215, B215_NEW).replace(lines[0], B1105_NEW)
for path, text in files.items():
    io.open(path, "w", encoding="utf-8", newline="").write(text)
    print("written", path[-48:])
