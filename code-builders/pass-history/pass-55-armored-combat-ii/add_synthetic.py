"""Pass 55, task 55.6: the four counters' illustrative entries in the synthetic catalog (rows 2370 on, as the worksheet numbers them)."""
import io

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/units/catalog/scenario-a1.synthetic.catalog.json"


def gun(name, nationality, designation, caliber, row):
    values = [
        '"attribute": "identity", "value": "S"', '"attribute": "designation", "value": "%s"' % designation, '"attribute": "gun-type", "value": "at"',
        '"attribute": "caliber", "value": %d' % caliber, '"attribute": "caliber-suffix", "value": "l"', '"attribute": "rate-of-fire", "value": 2',
        '"attribute": "ife", "printed": false', '"attribute": "breakdown", "value": 11', '"attribute": "range-minimum", "printed": false',
        '"attribute": "range-maximum", "value": 50', '"attribute": "special-ammo", "printed": false', '"attribute": "manhandling", "value": 9',
        '"attribute": "target-size", "value": "small"', '"trait": "asl:no-ap", "present": false', '"trait": "asl:no-he", "present": false',
        '"trait": "asl:mount-360", "present": false', '"trait": "asl:qsu", "present": true', '"trait": "asl:nm", "present": false',
        '"trait": "asl:rfnm", "present": false', '"attribute": "bpv", "value": 20', '"attribute": "dates", "value": "40-45"',
        '"trait": "asl:animal-pack", "present": false',
    ]
    faces = ["front"] * len(values) + ["malfunctioned", "malfunctioned"]
    values += ['"attribute": "repair", "value": 2', '"attribute": "removal", "value": 6']
    return entry(name, "asl:gun", nationality, None, faces, values, row)


def crew(name, nationality, row):
    values = [
        '"attribute": "firepower", "value": 1', '"attribute": "range", "value": 1', '"attribute": "morale", "value": 6', '"attribute": "identity", "value": "S"',
        '"attribute": "class", "value": "elite"', '"attribute": "class-variant", "printed": false', '"attribute": "smoke-exponent", "printed": false',
        '"trait": "asl:assault-fire", "present": false', '"trait": "asl:spraying-fire", "present": false', '"trait": "asl:elr-5", "present": false',
        '"trait": "asl:assault-engineer", "present": false',
    ]
    faces = ["front"] * len(values) + ["broken"] * 3
    values += ['"attribute": "broken-morale", "value": 5', '"attribute": "bpv", "value": 3', '"trait": "asl:self-rally", "present": true']
    return entry(name, "asl:crew", nationality, "elite", faces, values, row)


def entry(name, kind, nationality, unit_class, faces, values, row):
    lines = ["    {", '      "id": "%s",' % name, '      "kind": "%s", "kindRow": %d,' % (kind, row), '      "nationality": "%s", "nationalityRow": %d,' % (nationality, row + 1)]
    if unit_class:
        lines.append('      "class": "%s",' % unit_class)
    lines += ['      "counter": { "source": "synthetic", "sheet": "synthetic", "counter": "%s" },' % name, '      "applicability": { "status": "unreviewed" },',
              '      "slots": [],', '      "values": [']
    body = ['        { "face": "%s", %s, "row": %d }' % (face, value, row + 2 + index) for index, (face, value) in enumerate(zip(faces, values))]
    lines.append(",\n".join(body))
    lines += ["      ]", "    }"]
    return "\n".join(lines), row + 2 + len(values)


with io.open(PATH, "r", encoding="utf-8", newline="") as f:
    text = f.read()
assert "\r" not in text and '"id": "british-at-gun"' not in text
tail = "\n    }\n  ]\n}\n"
assert text.endswith(tail)
row = 2370
added = []
for make in (lambda r: gun("british-at-gun", "british", "Synthetic AT gun", 47, r), lambda r: gun("american-at-gun", "american", "Synthetic AT gun", 40, r),
             lambda r: crew("british-crew", "british", r), lambda r: crew("american-crew", "american", r)):
    block, row = make(row)
    added.append(block)
assert row == 2454, row
text = text[:-len(tail)] + "\n    },\n" + ",\n".join(added) + "\n  ]\n}\n"
with io.open(PATH, "w", encoding="utf-8", newline="") as f:
    f.write(text)
print("next row", row)
