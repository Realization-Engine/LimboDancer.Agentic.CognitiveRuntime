"""Pass 55, task 55.6: append the four counters' rows to the worksheet and the transcription, and re-register the transcription's hash.

Run once. It refuses when a counter is already there.
"""
import csv
import hashlib
import io
import json
import re

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
SHEETS = ROOT + "/docs/ASL/SourceRegistry/CounterSheets"
WORKSHEET = SHEETS + "/scenario-a1.counter-worksheet.csv"
TRANSCRIPTION = SHEETS + "/scenario-a1.counter-transcription.csv"
RECORD = SHEETS + "/asl-counter-sheets.scenario-a1.source-record.json"
ME = "Claude (claude-opus-5-5)"
REFEREE = "Claude (claude-opus-5-5), independent referee agent"

B = 'Chapter H British Ordnance Listing (p. 426), row "OQF 6-Pounder 7-cwt"'
A = 'Chapter H U.S. Ordnance Listing (p. 392), row "M3A1 37mm AT Gun"'


def gun(listing, values):
    """The 26 rows of a Gun, in the worksheet's order: (face, attribute, value, note)."""
    v = values
    return [
        ("counter", "kind", "asl:gun", listing + ": a Gun (C2.1, p. 167)"),
        ("counter", "nationality", v["nationality"], listing),
        ("front", "identity", "not-in-source", "The listing prints no counter identity"),
        ("front", "designation", v["designation"], listing + ", Name column"),
        ("front", "gun-type", "at", listing + ", Type column"),
        ("front", "caliber", v["caliber"], listing + ", CSize column"),
        ("front", "caliber-suffix", v["suffix"], listing + ", CSize column suffix (C2.21, C4.1)"),
        ("front", "rate-of-fire", v["rof"], listing + ", ROF(IFE) column (C2.24)"),
        ("front", "ife", "not-printed", listing + ": the ROF(IFE) column shows no IFE (C2.29)"),
        ("front", "breakdown", "12", listing + ": the B# column is blank, so the inherent B# is 12 (C2.28, p. 168)"),
        ("front", "range-minimum", "not-printed", listing + ": the Range column shows one number, the maximum (C2.25)"),
        ("front", "range-maximum", v["range"], listing + ", Range column (C2.25)"),
        ("front", "special-ammo", v["ammo"], listing + ", Special column (C2.26); " + v["ammoNote"]),
        ("front", "manhandling", v["manhandling"], listing + ", M# column, not bold (C2.27)"),
        ("front", "target-size", "small", listing + ", TSize column +1: a Small Target (C2.271)"),
        ("front", "asl:no-ap", "no", listing + ": the CSize is not overscored (C2.21)"),
        ("front", "asl:no-he", v["noHe"], listing + ": " + v["noHeNote"]),
        ("front", "asl:mount-360", "no", listing + ", Special column: NT, not T (C2.3)"),
        ("front", "asl:qsu", "yes", listing + ", Special column: QSU (C10.23)"),
        ("front", "asl:nm", "no", listing + ", Special column: no NM"),
        ("front", "asl:rfnm", "no", listing + ", Special column: no RFNM"),
        ("front", "bpv", v["bpv"], listing + ", BPV column (Listings Key, p. 351)"),
        ("front", "dates", v["dates"], listing + ", Dates column: " + v["datesNote"]),
        ("front", "asl:animal-pack", "no", listing + ", Notes column " + v["notes"]),
        ("malfunctioned", "repair", "not-in-source", "The listing does not give the malfunctioned side (C2.2)"),
        ("malfunctioned", "removal", "not-in-source", "The listing does not give the malfunctioned side (C2.2)"),
    ]


def crew(section, nationality, morale, broken, bpv):
    printed = "NCC %s section, CREW (BPV) column: 2-2-%s with superscript %s, BPV (%s)" % (section, morale, broken, bpv)
    none = "The CREW column shows no such marking"
    return [
        ("counter", "kind", "asl:crew", "NCC CREW column; an Infantry crew (A1.123, p. 44)"),
        ("counter", "nationality", nationality, "NCC %s section" % section),
        ("front", "firepower", "2", printed + "; A1.123: FP and Range factors of two"),
        ("front", "range", "2", printed + "; A1.123"),
        ("front", "morale", morale, printed),
        ("front", "identity", "not-in-source", "The chart prints no identity"),
        ("front", "class", "elite", "A1.123 (p. 44): Infantry crew counters are always considered elite troops"),
        ("front", "class-variant", "not-printed", "The chart prints no class symbol for crews"),
        ("front", "smoke-exponent", "not-printed", "The CREW column prints no Smoke Placement Exponent"),
        ("front", "asl:assault-fire", "no", none),
        ("front", "asl:spraying-fire", "no", none),
        ("front", "asl:elr-5", "no", none),
        ("front", "asl:assault-engineer", "no", none),
        ("broken", "broken-morale", broken, printed),
        ("broken", "bpv", bpv, printed),
        ("broken", "asl:self-rally", "yes", "A1.123 (p. 44): a crew's Self-Rally capability (A10.63)"),
    ]


COUNTERS = [
    ("OLB", "british-at-gun", "defender-at-gun", gun(B, {
        "nationality": "british", "designation": "OQF 6-Pounder 7-cwt", "caliber": "57", "suffix": "l", "rof": "3", "range": "225",
        "ammo": "HE7^3m2 HE8^4+ D6^4m6E D7^5",
        "ammoNote": 'the listing prints "HE7" with superscript F3, "/8" with superscript 4+, "D6" with superscript J4E, and "/7" with superscript 5; '
                    "British Ordnance Note 6 (p. 426): HE Depletion Number 7 from February 1943 and 8 for 1944 and 1945, APDS from June 1944 in the ETO alone; "
                    "the month is written as a number (ruling R55.4); NT and QSU are their own rows",
        "manhandling": "10", "noHe": "yes",
        "noHeNote": "the CSize is underscored, no HE (C2.21), but for the HE Depletion Number of the Special column (C8.8; British Ordnance Note 6)",
        "bpv": "34", "dates": "5/42-45",
        "datesNote": 'printed "5/42-45" with a dagger: in use from May 1942 to 1945; British Ordnance Note 6 gives the dates by theater',
        "notes": "6, B, N, P: no Animal-Pack note (British Ordnance Note D)",
    })),
    ("OLA", "american-at-gun", "defender-at-gun", gun(A, {
        "nationality": "american", "designation": "M3A1 37mm AT Gun", "caliber": "37", "suffix": "ll", "rof": "2", "range": "292",
        "ammo": "C7^2m8+",
        "ammoNote": 'the listing prints "C7" with superscript A2+; U.S. Ordnance Note C (p. 395): 37mm canister has 12 FP and is available only after 7/42, '
                    '"August 1942 on"; the month is written as a number (ruling R55.4); NT and QSU are their own rows',
        "manhandling": "12", "noHe": "no", "noHeNote": "the CSize is not underscored (C2.21)",
        "bpv": "30", "dates": "39-45",
        "datesNote": 'printed "39-45" with a dagger: in use from 1939 to 1945; U.S. Ordnance Note 6 (p. 391) gives the dates by theater',
        "notes": "6, C, N: no Animal-Pack note (U.S. Ordnance Note F)",
    })),
    ("NCC", "british-crew", "defender-crew", crew("BRITISH", "british", "8", "8", "8")),
    ("NCC", "american-crew", "defender-crew", crew("AMERICAN (Army)", "american", "7", "8", "7")),
]


def read(path):
    with io.open(path, "r", encoding="utf-8", newline="") as f:
        return f.read()


def write(path, text):
    with io.open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)


def lines(rows):
    out = io.StringIO()
    writer = csv.writer(out, lineterminator="\n", quoting=csv.QUOTE_MINIMAL)
    for row in rows:
        writer.writerow(row)
    return out.getvalue()


worksheet = read(WORKSHEET)
transcription = read(TRANSCRIPTION)
record = read(RECORD)
assert "\r" not in worksheet + transcription + record
before = hashlib.sha256(transcription.encode("utf-8")).hexdigest()
assert json.loads(record)["transcription"]["sha256"] == before, "the record does not register the transcription as it stands"
template = list(csv.reader(io.StringIO(worksheet)))
new_worksheet, new_transcription = [], []
for sheet, counter, model, rows in COUNTERS:
    assert ("," + counter + ",") not in transcription, counter + " is already transcribed"
    asked = [row for row in template if row[1] == model]
    assert [(row[2], row[3]) for row in asked] == [(face, attribute) for face, attribute, _, _ in rows], counter
    for ask, (face, attribute, value, note) in zip(asked, rows):
        guidance = "expected " + value if attribute == "kind" else ask[7]
        new_worksheet.append(["", counter, face, attribute, "", "", "", guidance])
        new_transcription.append([sheet, counter, face, attribute, value, ME, REFEREE, note])

text = "".join(lines(new_worksheet)) + "".join(lines(new_transcription))
assert "\u2014" not in text and "\u2013" not in text
worksheet += lines(new_worksheet)
transcription += lines(new_transcription)
after = hashlib.sha256(transcription.encode("utf-8")).hexdigest()

assert record.count(before) == 1
record = record.replace(before, after)
record, count = re.subn(r'"transcribedOn": "[0-9-]+"', '"transcribedOn": "2026-10-13"', record)
assert count == 1
PDF = "eASLRB_v3_01.pdf, SHA-256 957de75be52c34a7de4c20e875d33145e6b7d4ff8f19384c68818e385d41a247"
sheets = """    },
    {
      "id": "OLB",
      "title": "British Ordnance Listing (Chapter H)",
      "publisher": "Multi-Man Publishing",
      "product": "%s",
      "printing": "Physical PDF page 426, row \\"OQF 6-Pounder 7-cwt\\", with its Listing Note 6 on the same page and the British Multi-Applicable Ordnance Notes on page 429"
    },
    {
      "id": "OLA",
      "title": "U.S. Ordnance Listing (Chapter H)",
      "publisher": "Multi-Man Publishing",
      "product": "%s",
      "printing": "Physical PDF page 392, row \\"M3A1 37mm AT Gun\\", with its Listing Note 6 on page 391 and U.S. Multi-Applicable Ordnance Note C on page 395"
    }
  ],
  "extraction": {""" % (PDF, PDF)
anchor = '    }\n  ],\n  "extraction": {'
assert record.count(anchor) == 1
record = record.replace(anchor, sheets)
scope = (" And, on 2026-10-13 for pass 55 (Armored combat II, ruling R55.3), four counters, none manufactured: the British OQF 6-Pounder 7-cwt from the "
         "British Ordnance Listing (sheet OLB, p. 426), the American M3A1 37mm AT Gun from the U.S. Ordnance Listing (sheet OLA, p. 392), and an Infantry "
         "crew of each from the National Capabilities Chart (sheet NCC: the British 2-2-8 and the American Army 2-2-7). Their Depletion Numbers are written "
         "in the notation of ruling R55.4, with a month as a number.\",\n  \"scenario\"")
assert record.count('",\n  "scenario"') == 1
record = record.replace('",\n  "scenario"', scope)
review = (" On 2026-10-13, for pass 55, the 84 rows of the two AT Guns and the two crews were read by the building agent from its own renderings of "
          "the listing rows (pp. 392 and 426), the notes (pp. 391, 395, 426, 429), and the chart (p. 695), and agree value for value with what an "
          "independent read-only agent read from its own renderings for the pass's design (ruling R55.3); the reviewer column names that second "
          "reading. The Guns' malfunctioned sides are not in any registered source and stay not-in-source.\",\n    \"additionalReviewers\"")
assert record.count('",\n    "additionalReviewers"') == 1
record = record.replace('",\n    "additionalReviewers"', review)
json.loads(record)

write(WORKSHEET, worksheet)
write(TRANSCRIPTION, transcription)
write(RECORD, record)
print("rows", len(new_transcription), "sha256", before, "->", after)
