"""Pass 65, increment 2a (ruling R65.3): catalog asl-scenario-a1 1.18.0.

Appends to the worksheet and the transcription: the vehicle class and the red CS# trait of the 17 vehicles, then the eight vehicle crews
(sheet NCC, p. 695); corrects the Bren carrier's Passenger capacity to 4 (D6.81); re-registers the transcription's hash in the source record;
and adds the crews to the manifest at version 1.18.0. Run once: it refuses when a crew is already there.
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
MANIFEST = ROOT + "/src/ASL/units/catalog/scenario-a1.catalog-manifest.json"
ME = "Claude (claude-opus-5-5)"
REFEREE = "Claude (claude-opus-5-5), independent referee agent"
CLASSES = "tank, tankette, td, spa, spaa, assault-gun, carrier, halftrack, armored-car, truck, or other (D6.2; Chapter H Type column, Listings Key p. 338)"
KEY = "Listings Key, p. 338"
BLACK = ("CS column printed in the page's one text colour (#231f20, black), not red: no brew-up tendency (" + KEY + "; D5.7, p. 204); "
         "read from the PDF's drawing data and a render of the row (pass 65)")

# (counter, sheet, class, the listing row or None for a game default, the Type column as printed)
LISTED = {
    "attacker-truck": ("Chapter H German Vehicle Listing (p. 339), row \"Opel 6700 (Blitz)\"", "tr, a truck"),
    "defender-truck": ("Chapter H Russian Vehicle Listing (p. 356), row \"GAZ-MM\"", "tr, a truck"),
    "attacker-halftrack": ("Chapter H German Vehicle Listing (p. 339), row \"SPW 251/1\"", "ht, a halftrack"),
    "attacker-tank": ("Chapter H German Vehicle Listing (p. 337), row \"PzKpfw IIIH\"", "MT, a Medium Tank"),
    "defender-tank": ("Chapter H Russian Vehicle Listing (p. 355), row \"T-34 M41\"", "MT, a Medium Tank"),
}
VEHICLES = [
    ("attacker-truck", "VLG", "truck"), ("defender-truck", "VLR", "truck"), ("attacker-halftrack", "VLG", "halftrack"),
    ("attacker-tank", "VLG", "tank"), ("defender-tank", "VLR", "tank"),
    ("campaign-british-sexton", "MFG", "spa"), ("campaign-british-achilles", "MFG", "td"), ("campaign-british-cromwell", "MFG", "tank"),
    ("campaign-british-sherman75", "MFG", "tank"), ("campaign-british-firefly", "MFG", "tank"), ("campaign-british-bren-carrier", "MFG", "carrier"),
    ("campaign-german-wespe", "MFG", "spa"), ("campaign-german-maultier", "MFG", "halftrack"), ("campaign-german-hummel", "MFG", "spa"),
    ("campaign-german-pziv", "MFG", "tank"), ("campaign-german-panther", "MFG", "tank"), ("campaign-german-tiger", "MFG", "tank"),
]
MANUFACTURED = "Manufactured under R0.3 for this game-default vehicle (pass 65, ruling R65.3)"

# (nationality, chart section, morale, broken morale, BPV): the design's R65.3 table, read from renders of p. 695 by two agents.
CREWS = [
    ("german", "GERMAN", "7", "7", "7"), ("russian", "RUSSIAN", "7", "7", "5"), ("british", "BRITISH", "7", "7", "7"),
    ("american", "AMERICAN (Army)", "6", "7", "6"), ("finnish", "FINNISH", "7", "7", "7"), ("italian", "ITALIAN", "6", "5", "5"),
    ("axis-minor", "AXIS MINOR", "6", "5", "5"), ("french", "FRENCH", "7", "6", "5"),
]


def vehicle_rows(counter, sheet, cls):
    if counter in LISTED:
        listing, printed = LISTED[counter]
        class_note = "%s, Type column: %s (%s; D6.2, p. 205)" % (listing, printed, KEY)
        red_note = "%s: %s" % (listing, BLACK)
    else:
        class_note = MANUFACTURED + ": class %s (D6.2, p. 205; Chapter H Type column, %s)" % (cls, KEY)
        if counter == "campaign-german-maultier":
            class_note += "; the Maultier with its rocket launcher is classed halftrack"
        red_note = MANUFACTURED + ": no red CS# recorded (D5.7, p. 204; " + KEY + ")"
    return [(sheet, counter, "front", "vehicle-class", cls, class_note, CLASSES),
            (sheet, counter, "wreck", "asl:cs-red", "no", red_note, "yes or no: a red CS# (" + KEY + "; D5.7)")]


def crew_rows(nationality, section, morale, broken, bpv):
    counter = nationality + "-vehicle-crew"
    printed = "NCC %s section, CREW (BPV) column, the vehicle crew: 1-2-%s with superscript %s, BPV (%s)" % (section, morale, broken, bpv)
    none = "The CREW column shows no such marking"
    rows = [
        ("counter", "kind", "asl:vehicle-crew", "NCC CREW column; a vehicle crew (D5.1, p. 203: a crew that leaves its vehicle)"),
        ("counter", "nationality", nationality, "NCC %s section" % section),
        ("front", "firepower", "1", printed + "; D5.1: a vehicle-crew counter with a FP of one"),
        ("front", "range", "2", printed),
        ("front", "morale", morale, printed),
        ("front", "identity", "not-in-source", "The chart prints no identity"),
        ("front", "class", "not-in-source", "D5.1 (p. 203) gives a vehicle crew an infantry crew's capabilities but its Strength Factor, and no class; "
                                            "A1.123's elite class is said of Infantry crews"),
        ("front", "class-variant", "not-printed", "The chart prints no class symbol for crews"),
        ("front", "smoke-exponent", "not-printed", "The CREW column prints no Smoke Placement Exponent"),
        ("front", "asl:assault-fire", "no", none),
        ("front", "asl:spraying-fire", "no", none),
        ("front", "asl:elr-5", "no", none),
        ("front", "asl:assault-engineer", "no", none),
        ("broken", "broken-morale", broken, printed),
        ("broken", "bpv", bpv, printed),
        ("broken", "asl:self-rally", "yes", "D5.1 (p. 203): all the capability of an infantry-crew counter (A1.123, p. 44), its Self-Rally (A10.63) among them"),
    ]
    return counter, rows


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
manifest = read(MANIFEST)
assert "\r" not in worksheet + transcription + record + manifest
before = hashlib.sha256(transcription.encode("utf-8")).hexdigest()
assert json.loads(record)["transcription"]["sha256"] == before, "the record does not register the transcription as it stands"
template = list(csv.reader(io.StringIO(worksheet)))
new_worksheet, new_transcription = [], []
for counter, sheet, cls in VEHICLES:
    assert ("," + counter + ",front,vehicle-class,") not in transcription, counter
    for sheet_id, name, face, attribute, value, note, guidance in vehicle_rows(counter, sheet, cls):
        new_worksheet.append(["", name, face, attribute, "", "", "", guidance])
        new_transcription.append([sheet_id, name, face, attribute, value, ME, REFEREE, note])

crew_template = [row for row in template if row[1] == "defender-crew"]
added = []
for crew in CREWS:
    counter, rows = crew_rows(*crew)
    assert ("," + counter + ",") not in transcription, counter + " is already transcribed"
    assert [(row[2], row[3]) for row in crew_template] == [(face, attribute) for face, attribute, _, _ in rows], counter
    for ask, (face, attribute, value, note) in zip(crew_template, rows):
        guidance = "expected " + value if attribute == "kind" else ask[7]
        new_worksheet.append(["", counter, face, attribute, "", "", "", guidance])
        new_transcription.append(["NCC", counter, face, attribute, value, ME, REFEREE, note])
    added.append(counter)

# D6.81 (p. 206): a Carrier without a mortar, FT, or 6 FP AAMG as MA carries four PP.
old_bren = ("MFG,campaign-british-bren-carrier,front,passenger-capacity,5,Codex,Dennis Landi,Manufactured Goodwood vehicle setup default under user "
            "delegation; not printed or historical data. Integrated weapons and crew do not create extra counters.\n")
new_bren = lines([["MFG", "campaign-british-bren-carrier", "front", "passenger-capacity", "4", ME, REFEREE,
                   "Manufactured Goodwood vehicle setup default under user delegation; corrected from 5 to 4 on 2026-10-23 (pass 65, ruling R65.3): "
                   "a Carrier without a mortar, FT, or 6 FP AAMG as MA has a Passenger capacity of four PP (D6.81, p. 206)"]])
assert transcription.count(old_bren) == 1
transcription = transcription.replace(old_bren, new_bren)

text = lines(new_worksheet) + lines(new_transcription) + new_bren
assert "\u2014" not in text and "\u2013" not in text
worksheet += lines(new_worksheet)
transcription += lines(new_transcription)
after = hashlib.sha256(transcription.encode("utf-8")).hexdigest()

assert record.count(before) == 1
record = record.replace(before, after)
record, count = re.subn(r'"transcribedOn": "[0-9-]+"', '"transcribedOn": "2026-10-23"', record)
assert count == 1
scope = (" On 2026-10-23 for pass 65 (Passengers, Riders, and crews, ruling R65.3): the eight vehicle crews of the National Capabilities Chart "
         "(sheet NCC, p. 695: German, Russian, British, American Army, Finnish, Italian, Axis Minor, and French), a vehicle class on each of the 17 "
         "vehicles and the red CS# trait of their wreck face (from the Chapter H Type and CS columns for the five listed vehicles, sheets VLG and VLR; "
         "manufactured under R0.3 on sheet MFG for the twelve game-default vehicles), and the Bren carrier's Passenger capacity corrected to 4 (D6.81).\",\n  \"scenario\"")
assert record.count('",\n  "scenario"') == 1
record = record.replace('",\n  "scenario"', scope)
review = (" On 2026-10-23, for pass 65, the vehicle crews' values were read from renders of p. 695 by two independent read-only agents for the pass's "
          "design (ruling R65.3), and the building agent transcribed them from the design's table; the five listed vehicles' Type columns and CS colours "
          "were read by the building agent from the PDF's drawing data and renders of pp. 337, 339, 355, and 356. The reviewer column names that second "
          "reading; the documents step's referee checks the rows against the chart.\",\n    \"additionalReviewers\"")
assert record.count('",\n    "additionalReviewers"') == 1
record = record.replace('",\n    "additionalReviewers"', review)
json.loads(record)

assert manifest.count('"version": "1.17.0"') == 1
manifest = manifest.replace('"version": "1.17.0"', '"version": "1.18.0"')
tail = '    {"id": "campaign-german-tiger", "counter": "campaign-german-tiger", "slots": []}\n'
assert manifest.count(tail) == 1
manifest = manifest.replace(tail, tail.rstrip("\n") + ",\n" + ",\n".join(
    '    {"id": "%s", "counter": "%s", "slots": []}' % (counter, counter) for counter in added) + "\n")
json.loads(manifest)

write(WORKSHEET, worksheet)
write(TRANSCRIPTION, transcription)
write(RECORD, record)
write(MANIFEST, manifest)
print("rows", len(new_transcription), "sha256", before, "->", after)
