"""Pass 65 documents: the backlog's rows that named pass 65, corrected where they stand, then section 71 appended from a fragment.

    python docs_backlog.py <fragment>

Every row is found by the start of its line, exactly once, before any file is written. LF, UTF-8, no BOM.
"""
import io
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", ".."))
from paths import REPO  # noqa: E402

P = REPO + "/src/ASL/docs/ASL Unit Backlog.md"
fragment = sys.argv[1]

ROWS = [
    ("| Fire at a vehicle reaching its Passengers: an unarmored or open-topped vehicle's Passengers,",
     "**Built in pass 65 (design 8.2; rulings R65.5 to R65.7): a halftrack's Passengers are CE or BU with its crew, a truck's PRC and Riders are always Vulnerable, and every Vulnerable PRC takes the General Collateral Attack on the attack's DR and the Specific after a hit that leaves the vehicle standing.**"),
    ("| Crew Survival for Passengers of a destroyed vehicle | R26.2 |",
     "**Built in pass 65 (design 8.2; ruling R65.9): each Passenger and Rider makes its own Survival DR against the CS# or cs#, +1 if broken, and a survivor is placed beneath the wreck.**"),
    ("| Passengers' fire, rout beneath a Stopped vehicle, OVR, and CC; Riders | R26.2 |",
     "**Built in pass 65 (design 9.2, 11.2; rulings R65.10 to R65.13, R65.16, R65.17): Mounted Fire, passing a SW, rout beneath a Stopped vehicle, leaders aboard, Passengers in an OVR, and Riders with Bailing Out. CC with PRC is decided in Rules and not wired into Play; it moves to pass 70 (section 71.1).**"),
    ("| Boarding as a step of the Infantry move, after moving (the D6.4 EX),",
     "**Built in pass 65 (design 10.2; ruling R65.15): a unit boards as the last step of its own move, with a leader's IPC (A4.42). A second stack boarding the same vehicle in one MPh was not taken up.**"),
    ("| Four MF after dismounting; the Double Time bar for a unit that boards later in the turn |",
     "**Built in pass 65 (design 10.2; ruling R65.15): after unloading a unit has four MF, no Double Time, and no leader's bonus. The Road Bonus can still lift it above four (section 71.1, pass 105).**"),
    ("| A CE or BU choice at setup | Pass 40 (design 11.4)",
     "**Not built by pass 65 but for the Carrier, which always sets up CE (D6.84, design 12.2); it moves to pass 105.**"),
    ("| A group whose firers stand in the target's own Location and at the next level of its hex;",
     "**Pass 65 built A7.211's TPBF at a vehicle's Vulnerable PRC (design 8.2); a group that mixes TPBF with other fire stays refused, and IFE moves to pass 105.**"),
    ("| TPBF against PRC from a higher Location | Pass 45 reviews",
     "**Built in pass 65 (design 8.2; ruling R65.6): TPBF at the Vulnerable PRC of a vehicle in the firer's Location or from a higher one (`ScenarioA1PrcRules.Exposure`).**"),
    ("| Height Advantage on top of a CE crew's DRM under Infantry fire |",
     "**Built in pass 65 (design 8.2; ruling R65.7): under Infantry fire the CE DRM is not added to a positive TEM or to Height Advantage where the attack carries the `prc` fact; an older attack resolves as it did.**"),
    ("| A Passenger's time aboard is not read when it later moves on foot",
     "**Built in pass 65 (design 10.2; ruling R65.15): a Passenger's time aboard counts toward a later Motion attempt against it.**"),
    ("| More unarmored vehicles in a Location than the column's highest KIA# | Pass 60 (design 8.1)",
     "**Built in pass 65 for the IFT (design 17.2; ruling R65.26): Random Selection picks the vehicles the column's highest KIA# affects, with its dr on the record, where the attack carries the `prc` fact. The ordnance Infantry Target Type and a mortar's hit keep the refusal; they move to pass 110 (section 71.1).**"),
    ("| No Specific Collateral Attack after the crew Abandons on a failed Immobilization Task Check,",
     "**Built in pass 65 (design 8.2, 18.2; ruling R65.7; fix Round A): the Specific Collateral Attack reaches Passengers, Riders, and an unarmored vehicle's Vulnerable PRC, and an Abandonment, a Shock, or a Stun stops only the crew's. A Gun's HEAT with no HE FP to step from moves to pass 110.**"),
    ("| A CE AFV in terrain with a positive TEM under an IFT attack that is not an ordnance hit",
     "**Built in pass 65 (design 8.2; ruling R65.7): ruling R25.6's refusal is lifted where the attack carries the `prc` fact, and the crew takes the CE DRM alone.**"),
    ("| No Immobilization Task Check after a mortar immobilizes an AFV |",
     "**Built in pass 65 (design 14.2; ruling R65.21): the Immobilization TC on the mortar path and on the IFT's Vehicle line.**"),
    ("| An OT AFV hit by Air Bursts: its crew, which D5.311 has break and rout like Infantry |",
     "**Built in pass 65 (design 17.2, 18.2; rulings R65.26, R65.31): the crew is attacked as unarmored with the Air Burst DRM; a crew that breaks stays inside, the AFV Stops, and it routs beneath in the RtPh, Abandoning it. The unarmored Target Facing moves to pass 105 (section 71.1).**"),
    ("| Crew Survival's +1 for a Shocked or Abandoned vehicle |",
     "**Built in pass 65 (design 14.2; ruling R65.21): `shocked` and `abandoned` on the vehicle's facts, and one Crew Survival member (`ScenarioA1CrewSurvivalRules`) over every path.**"),
    ("| `vehicles-beyond-kia` refuses a mortar's shot that C1.55 only limits |",
     "**Not built by pass 65, whose A7.308 is the IFT's (design 17.1); it moves to pass 110.**"),
]

text = io.open(P, encoding="utf-8", newline="").read()
assert "\r" not in text and not text.startswith("﻿")
assert "## 71. Added by pass 65" not in text
lines = text.split("\n")
found = []
for prefix, sentence in ROWS:
    hits = [i for i, line in enumerate(lines) if line.startswith(prefix)]
    assert len(hits) == 1, (prefix, len(hits))
    assert lines[hits[0]].endswith(" |"), prefix
    found.append(hits[0])
assert len(set(found)) == len(found)
for (prefix, sentence), i in zip(ROWS, found):
    lines[i] = lines[i][:-2] + " " + sentence + " |"
more = io.open(fragment, encoding="utf-8", newline="").read().replace("\r\n", "\n").strip("\n")
assert "—" not in more
body = "\n".join(lines).rstrip("\n") + "\n\n" + more + "\n"
assert "—" not in body
io.open(P, "w", encoding="utf-8", newline="").write(body)
print("corrected", len(ROWS), "rows; appended", len(more.splitlines()), "lines")
