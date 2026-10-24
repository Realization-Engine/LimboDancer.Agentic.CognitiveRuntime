"""Pass 65, increment 2a: the synthetic catalog in step with catalog 1.18.0 (ruling R65.3).

Each synthetic vehicle gains its vehicle class and the red CS# trait (absent), and the eight vehicle crews are added with illustrative values.
Rows are the worksheet's line numbers of the rows add_counters_2a.py appended. Run once, after add_counters_2a.py.
"""
import csv
import io
import json

ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
PATH = ROOT + "/src/ASL/units/catalog/scenario-a1.synthetic.catalog.json"
WORKSHEET = ROOT + "/docs/ASL/SourceRegistry/CounterSheets/scenario-a1.counter-worksheet.csv"
CLASSES = {
    "attacker-truck": "truck", "defender-truck": "truck", "attacker-halftrack": "halftrack", "attacker-tank": "tank", "defender-tank": "tank",
    "campaign-british-sexton": "spa", "campaign-british-achilles": "td", "campaign-british-cromwell": "tank", "campaign-british-sherman75": "tank",
    "campaign-british-firefly": "tank", "campaign-british-bren-carrier": "carrier", "campaign-german-wespe": "spa", "campaign-german-maultier": "halftrack",
    "campaign-german-hummel": "spa", "campaign-german-pziv": "tank", "campaign-german-panther": "tank", "campaign-german-tiger": "tank",
}
NATIONS = ["german", "russian", "british", "american", "finnish", "italian", "axis-minor", "french"]

with io.open(WORKSHEET, "r", encoding="utf-8", newline="") as f:
    rows = list(csv.reader(f))
line = {}
for number, row in enumerate(rows, start=1):
    line.setdefault((row[1], row[2], row[3]), number)

with io.open(PATH, "r", encoding="utf-8", newline="") as f:
    text = f.read()
assert "\r" not in text and '"german-vehicle-crew"' not in text and "vehicle-class" not in text
ids = {item["id"] for item in json.loads(text)["definitions"] if item["kind"] == "asl:vehicle"}
assert ids == set(CLASSES), ids ^ set(CLASSES)

for vehicle, cls in CLASSES.items():
    cls_row, red_row = line[(vehicle, "front", "vehicle-class")], line[(vehicle, "wreck", "asl:cs-red")]
    multi = '      "id": "%s",\n' % vehicle
    if multi in text:
        start = text.index(multi)
        end = text.index("\n      ]\n    }", start)
        added = (',\n        { "face": "front", "attribute": "vehicle-class", "value": "%s", "row": %d },\n'
                 '        { "face": "wreck", "trait": "asl:cs-red", "present": false, "row": %d }') % (cls, cls_row, red_row)
        # The last value line ends with " }"; the new values follow it.
        text = text[:end] + added + text[end:]
    else:
        start = text.index('    {"id": "%s",' % vehicle)
        end = text.index("}]}", start)
        added = (', {"face": "front", "attribute": "vehicle-class", "value": "%s", "row": %d}, '
                 '{"face": "wreck", "trait": "asl:cs-red", "present": false, "row": %d}') % (cls, cls_row, red_row)
        text = text[:end + 1] + added + text[end + 1:]


def crew(nationality):
    name = nationality + "-vehicle-crew"
    values = [
        ("front", '"attribute": "firepower", "value": 1'), ("front", '"attribute": "range", "value": 2'), ("front", '"attribute": "morale", "value": 6'),
        ("front", '"attribute": "identity", "value": "S"'), ("front", '"attribute": "class", "recorded": false'),
        ("front", '"attribute": "class-variant", "printed": false'), ("front", '"attribute": "smoke-exponent", "printed": false'),
        ("front", '"trait": "asl:assault-fire", "present": false'), ("front", '"trait": "asl:spraying-fire", "present": false'),
        ("front", '"trait": "asl:elr-5", "present": false'), ("front", '"trait": "asl:assault-engineer", "present": false'),
        ("broken", '"attribute": "broken-morale", "value": 5'), ("broken", '"attribute": "bpv", "value": 3'), ("broken", '"trait": "asl:self-rally", "present": true'),
    ]
    body = []
    for face, value in values:
        key = value.split('"')[3]
        body.append('        { "face": "%s", %s, "row": %d }' % (face, value, line[(name, face, key)]))
    return "\n".join([
        "    {", '      "id": "%s",' % name, '      "kind": "asl:vehicle-crew", "kindRow": %d,' % line[(name, "counter", "kind")],
        '      "nationality": "%s", "nationalityRow": %d,' % (nationality, line[(name, "counter", "nationality")]),
        '      "counter": { "source": "synthetic", "sheet": "synthetic", "counter": "%s" },' % name, '      "applicability": { "status": "unreviewed" },',
        '      "slots": [],', '      "values": [', ",\n".join(body), "      ]", "    }"])


tail = "\n]\n}\n"
assert text.endswith(tail), repr(text[-20:])
text = text[:-len(tail)] + ",\n" + ",\n".join(crew(nation) for nation in NATIONS) + tail
json.loads(text)
with io.open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("vehicles", len(CLASSES), "crews", len(NATIONS))
