"""Add the 295th group's three setup plans to the Tractor Works' setups file (2026-10-19).

The placements and terrain facts come from the LOS survey (Program.cs here, modes setup, lanes, and levels) over board 1.
Usage: python add_295th_plans.py <setups.json> [--check]   (--check only reports whether a rewrite keeps the file's bytes)
"""
import json
import sys

PATH = sys.argv[1]
CARD_SHA = "ed9c2df4350546dd3fc20214b3b7eaf036d4503c01a27751bfeb1cc3d5bdb19e"
GROUP = "russian-2"

original = open(PATH, encoding="utf-8", newline="").read()
data = json.loads(original)


def dump(value):
    return json.dumps(value, indent=2, ensure_ascii=False) + "\n"


if "--check" in sys.argv:
    print("round trip keeps the bytes:", dump(data) == original)
    sys.exit(0)

if any(plan["id"] in ("north-hammer", "centre-punch", "two-prongs") for plan in data["plans"]):
    sys.exit("STOP: the 295th plans are already in the file")


def plan(plan_id, name, idea, gives_up, terrain, stacks):
    """stacks: (Location, squads, leaders, LMG count); squads are numbered in order, and each LMG goes to a squad of its stack."""
    placements, weapons = [], []
    squad = lmg = 0
    for at, squads, leaders, lmgs in stacks:
        ids = []
        for _ in range(squads):
            squad += 1
            ids.append(f"r2-squad-{squad}")
            placements.append({"id": ids[-1], "definition": "defender-line-squad", "group": GROUP, "at": "bd01:" + at})
        for leader in leaders:
            placements.append({"id": leader[0], "definition": leader[1], "group": GROUP, "at": "bd01:" + at})
        for index in range(lmgs):
            lmg += 1
            weapons.append({"id": f"r2-lmg-{lmg}", "definition": "defender-lmg", "group": GROUP, "holder": ids[index]})
    assert squad == 26 and lmg == 4, (plan_id, squad, lmg)
    names = [p["id"] for p in placements if p["definition"].startswith("defender-leader")]
    assert sorted(names) == ["r2-leader-8-0-1", "r2-leader-8-0-2", "r2-leader-9-1"], names
    return {"id": plan_id, "cardSha256": CARD_SHA, "side": "russian", "name": name, "idea": idea, "givesUp": gives_up,
            "terrain": terrain, "placements": placements + weapons}


L91 = ("r2-leader-9-1", "defender-leader-9-1")
L80A = ("r2-leader-8-0-1", "defender-leader")
L80B = ("r2-leader-8-0-2", "defender-leader")

COMMON = [
    "P3 at ground level and level 1 has no LOS to or from any German setup Location, so a stack there takes the non-OB \"?\" once both sides have set up, whatever the German setup (A12.12; ruling R23.6). Every other Location of the group's buildings is seen from at least one German setup Location, and the group has no \"?\" of its own, so those stacks show as they are.",
    "The first floors of Y7 and Z6, where both of the card's German plans put Kampfgruppe Tienham's MGs, see Q4 and Q3 at level 2, Q6 at level 1, and P5 at both levels, at ranges 8 to 10; Q4 and Q3 at level 1 see only the second floors of Y7, Y8, and Z6, which neither German plan uses.",
    "Sewer Movement (B8.4) and Human Wave (A25.23) are not built yet; the plan uses neither.",
]

north = plan(
    "north-hammer", "North hammer",
    "The weight goes through building U3, the way the card's own commentary expects. Twelve squads and the 9-1 stand in R1 and S1, both levels, two and three hexes from T2 and U2; a fire base of six squads with three LMG and an 8-0 stands on the first floor of Q4 and Q3, three and four hexes from T2 and U3; two more squads wait at Q3's ground level, and six squads with the other 8-0 stand unseen in P3 as the second wave. Once U3 breaks, the group goes along hexrow 1 by T1, V1, and W1 to the Works' north-west face at X2 and W4.",
    "The south is empty: P5, P7, P8, and Q6 hold no one, so Kampfgruppe Stahler's men at R7, T7, and U8 may turn west or north on the group's flank, and the centre building, S5 and T4, is fired on from Q4 and Q3 but not attacked.",
    [
        "R1 and S1 see T2 at range 2, U2 at ranges 2 and 3, and T4 at range 4; T2 and U2 are the west end of building U3, Kampfgruppe Stahler's largest.",
        "Q4 at level 1 sees T2 at range 3, U3 at 4, S5 at 2, and T4's first floor at 3; Q3 at level 1 sees the same at ranges 3 and 4, and T7 at 6.",
        "The lane by T0, T1, V0, and V1 is seen only from building U3, six of its Locations; once U3 is cleared, nothing else of the German setup sees it. W1, at its end, is also seen from building AA4.",
    ] + COMMON,
    [("R1:0", 3, [L91], 0), ("R1:1", 3, [], 1), ("S1:0", 3, [], 0), ("S1:1", 3, [], 0),
     ("Q4:1", 3, [L80A], 2), ("Q3:1", 3, [], 1), ("Q3:0", 2, [], 0), ("P3:0", 3, [L80B], 0), ("P3:1", 3, [], 0)])

centre = plan(
    "centre-punch", "Centre punch",
    "The weight goes through the centre building, S5 and T4, the shortest way to the Works. Eighteen squads stand in Q4, Q6, and Q3 at both levels: Q4 and Q6 two hexes from S5, Q3 three; the 9-1 leads from Q4's ground level, an 8-0 and two LMG fire from Q4's first floor, and the other two LMG from Q6 and Q3 above. Two squads stand at P5 and six with the other 8-0 wait unseen in P3. Once S5 and T4 break, the group goes by U4 and V4 to the Works' west face at W4 and W5.",
    "Building U3 is fired on from Q3 and Q4 at ranges 3 and 4 but not attacked, so Kampfgruppe Stahler's stacks there keep the north flank and see the centre lanes; R1, S1, P7, and P8 hold no one.",
    [
        "Q4 and Q6 see S5 at range 2 at both levels; Q4 at level 1 and Q3 at level 1 see T4's first floor at range 3.",
        "Q6 sees R7 at range 2 and U3 at 5; Q6 at level 1 sees T7's first floor at 3.",
        "The lanes past the centre building, U4, U5, and V4, are each seen from 10 to 15 German setup Locations, all of them in buildings U3, T4, and T7.",
        "P5 at ground level sees no German setup Location but Z6's, at range 10.",
    ] + COMMON,
    [("Q4:0", 3, [L91], 0), ("Q4:1", 3, [L80A], 2), ("Q6:0", 3, [], 0), ("Q6:1", 3, [], 1), ("Q3:0", 3, [], 0),
     ("Q3:1", 3, [], 1), ("P5:0", 2, [], 0), ("P3:0", 3, [L80B], 0), ("P3:1", 3, [], 0)])

prongs = plan(
    "two-prongs", "Two prongs",
    "Two attacks split Kampfgruppe Stahler. In the north, nine squads and the 9-1 in R1 and S1 go for building U3 at range 2; in the south, nine squads and an 8-0 in P8 and Q6 go for R7, two hexes away, and then T7 and U8. Between them, six squads with three LMG and the other 8-0 fire from the first floor of Q4 and Q3 on S5 and T4, and two squads wait unseen in P3.",
    "Neither attack has the weight to break through alone, and the southern one moves under Kampfgruppe Tienham's guns: the lanes U6 and V6 are the most watched ground between the group and the Works. The card's commentary expects a commitment to the south to end in a draw.",
    [
        "R1 and S1 see T2 at range 2 and U2 at ranges 2 and 3.",
        "P8 sees R7 at range 2, T7 at 4, and U8 at 5; Q6 sees S5 and R7 at range 2.",
        "Q4 at level 1 sees S5 at range 2 and T4's first floor at 3; Q3 at level 1 sees T4's first floor at 3.",
        "The southern lanes U6 and V6 are seen from 24 and 18 German setup Locations, the most of any ground between the group and the Works; U6 alone is seen from seven Locations of building Y8.",
    ] + COMMON,
    [("R1:0", 3, [L91], 0), ("R1:1", 3, [], 1), ("S1:0", 3, [], 0), ("P8:0", 3, [L80B], 0), ("P8:1", 3, [], 0),
     ("Q6:0", 3, [], 0), ("Q4:1", 3, [L80A], 2), ("Q3:1", 3, [], 1), ("P3:0", 2, [], 0)])

data["plans"].extend([north, centre, prongs])
open(PATH, "w", encoding="utf-8", newline="").write(dump(data))
print("added", [p["id"] for p in (north, centre, prongs)])
