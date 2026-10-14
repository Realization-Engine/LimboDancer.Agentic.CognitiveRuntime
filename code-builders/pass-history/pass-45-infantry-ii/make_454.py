"""Pass 45, tasks 45.4 to 45.6: the games. Board 3.

Usage: python make_454.py <game id> <mode>
  spray      a German squad (4-6-7, Range underscored) in [F10]; Russian squads in [D10] and [D9]. The German PFPh: Squad Spraying Fire (A7.34)
  nospray    the control: a Russian squad (4-4-7, no option) in [F10] and German squads in [D10] and [D9]. The Russian PFPh: refused
  field      a Russian squad with a MMG in the woods of [C9]; German squads in [D9], [C10], and [C8]. The Russian PFPh: the MMG's later
             shots stay inside the CA of its first (A9.21)
  open       the control of field: the same MMG in the Open Ground of [D9], German squads in [D10], [E10], and [C9]
  leader     a Russian leader, squad, and MMG in [F10]; a German squad in [D10]. The Russian PFPh: the leader directs the group, then the
             MMG's later shot (A7.53)
  lane       a Russian squad with a MMG in [F10]; a German squad in [D9] and a berserk one in [G10]. The German MPh: a Fire Lane along
             [F10]-[E10]-[D10], then the charge into [F10] cancels it (A9.223)
"""
import sys

from p45lib import Game

GAME, MODE = sys.argv[1], sys.argv[2]
LABELS = {
    "spray": "Pass 45 task 45.4: a 4-6-7 squad in [F10] sprays [D10] and [D9] with its own FP (A7.34; board 3)",
    "nospray": "Pass 45 task 45.4 control: a 4-4-7 squad has no Spraying Fire option (A7.34; board 3)",
    "field": "Pass 45 task 45.6: a MMG in the woods of [C9] fires again only inside the CA of its first shot (A9.21; board 3)",
    "open": "Pass 45 task 45.6 control: a MMG in the Open Ground of [D9] has no Field of Fire (A9.21; board 3)",
    "leader": "Pass 45 task 45.6: a leader in [F10] directs his group and then the MMG's later shot (A7.53; board 3)",
    "lane": "Pass 45 task 45.5: a MMG's Fire Lane from [F10] is cancelled when a berserk squad enters its Location (A9.223; board 3)",
}
phasing = "german" if MODE in ("spray", "lane") else "russian"
game = Game(GAME, LABELS[MODE], phasing=phasing)
if MODE == "spray":
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:F10:0")
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:D10:0")
    game.unit("r-two", "squad", "defender-squad", "russian", "bd03:D9:0")
elif MODE == "nospray":
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D10:0")
    game.unit("g-two", "squad", "attacker-squad", "german", "bd03:D9:0")
elif MODE in ("field", "open"):
    at, around = ("bd03:C9:0", ["D9", "C10", "C8"]) if MODE == "field" else ("bd03:D9:0", ["D10", "E10", "C9"])
    game.unit("r-one", "squad", "defender-squad", "russian", at)
    game.weapon("r-mmg", "defender-mmg", "russian", "r-one")
    for index, name in enumerate(around):
        game.unit(f"g-{index + 1}", "squad", "attacker-squad", "german", f"bd03:{name}:0")
elif MODE == "leader":
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.unit("r-lead", "leader", "defender-leader", "russian", "bd03:F10:0")
    game.weapon("r-mmg", "defender-mmg", "russian", "r-one")
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D10:0")
else:
    game.unit("r-one", "squad", "defender-squad", "russian", "bd03:F10:0")
    game.weapon("r-mmg", "defender-mmg", "russian", "r-one")
    # A berserk unit charges before any other moves, so it draws the lane itself: [D10] to [E10], where the MMG fires and lays its lane, then [F10].
    game.unit("g-one", "squad", "attacker-squad", "german", "bd03:D10:0", berserk=True)
game.phases(*(("pfph", "mph") if MODE == "lane" else ("pfph",)))
game.write()
