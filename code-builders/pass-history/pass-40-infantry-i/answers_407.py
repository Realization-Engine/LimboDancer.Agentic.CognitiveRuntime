"""Record the user's answers on task 40.7 and the four games staged at their word (exact blocks, each asserted once)."""
DOCS = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/docs/"
path = DOCS + "Passes/ASL Unit Backlog Pass 40 Design.md"
text = open(path, encoding="utf-8", newline="").read()

start = text.index("**No Studio game** for corrections 5, 6, and 7")
end = text.index("Seen in the Studio and not changed:")
GAMES = '''**The games of corrections 5, 6, and 7 and of Recovery in Bypass,** staged at the user's word of 2026-10-11 (`make_407_games.py`, `make_boarding.py`), with the dice scripted:

| Game | What it holds |
|---|---|
| `p40-assault` (**recorded**) | With Assault Movement declared, the squad in [D10] tries a SMOKE grenade in its own Location for one MF (dr 1: placed), then enters [D9] "for 1 MF, by Assault Movement". After the DEFENDER's pass a step on to [C9] is refused: "Assault Movement is declared at the start of the move and enters one Location (A4.61)" (read and cancelled). |
| `p40-smoke` (**recorded**) | A burning wreck's SMOKE in the woods of [C9]. The squad with three MF advances in: "advances into [C9] (woods)", no CX, though the entry costs three MF with the SMOKE. The squad with two MF, for which the woods alone are all it has: "becomes CX advancing into Difficult Terrain (A4.72)". |
| `p40-boarding` (**recorded**) | Two leaders beside a halftrack in [D10]. Each failed Recovery attempt (dr 6) costs one MF. After four, the first leader's boarding is refused: "has no MF left to board, which costs one (D6.4)"; with six MF he would have had two. After two, the second leader "boards vehicle G1 in [D10] for one MF". |
| `p40-bypass` (**recorded**) | The squad Bypasses the woods of [F5] along its south hexside; a LMG lies in the hex. Its Recovery is refused: "is in Bypass, and a Bypassing unit Recovers nothing (A4.44)" (read and cancelled). |

**What could not be staged, and why.** The Double Time bar on boarding: the page offers boarding only while no Infantry move is open, and Double Time is declared with a step of a move, so a unit that has Double Timed has an open move and is not offered to board. The bar is reached through the gate alone, and rests on its Rules test. For the same reason a unit that walked before boarding cannot be shown on the page; the game above spends the MF in place. The loss of Assault Movement by a mover that breaks was not staged either: it needs a First Fire that breaks the squad and a second firer, and rests on its Rules test.

The first boarding game, `p40-board`, holds one Double Time step from the attempt that found this; it is not a fixture.

'''
text = text[:start] + GAMES + text[end:]

edits = [
    ("Built on 2026-10-11 and waiting for the user's word before its commit. Seven corrections are built; two things the frame said it would take are put back to the user in 9.4.",
     "Built on 2026-10-11 and committed at the user's word of the same day, with yes to ruling R40.11 and to both items of 9.4, and with the four further games staged first. Seven corrections are built; two things the frame said it would take are moved on (9.4)."),
    ("### 9.3 Ruling proposed", "### 9.3 Ruling (approved 2026-10-11)"),
    ("### 9.4 Put back to the user", "### 9.4 Moved on (the user, 2026-10-11)"),
]
for old, new in edits:
    assert text.count(old) == 1, old[:60]
    text = text.replace(old, new)
open(path, "w", encoding="utf-8", newline="").write(text)

ROW = ("| At the user's word, the games of 40.7 that were missing: four generators' worth on board 3 (Assault Movement with a SMOKE attempt in place; "
       "the advance into woods with a burning wreck's SMOKE; boarding on four MF; Recovery in Bypass), each played with scripted dice and read back. "
       "The boarding game was staged twice: the page offers boarding only when no Infantry move is open, so the MF were spent in place by failed Recovery "
       "attempts. The design's 9.5 rewritten "
       "| 21:33 | 21:53 | 20 min | The Double Time bar on boarding and the loss of Assault Movement by a broken mover could not be staged through the "
       "page and rest on Rules tests. |\n")
path = DOCS + "ASL Unit Time Log.md"
log = open(path, encoding="utf-8", newline="").read()
assert log.rstrip("\n").endswith("the page's component tests. |"), log[-80:]
open(path, "w", encoding="utf-8", newline="").write(log + ROW)
print("ok")
