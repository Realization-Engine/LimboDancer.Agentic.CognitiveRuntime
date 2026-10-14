"""Build apply_review_fixes2.py: the half of apply_review_fixes.py that its failed assertion left unapplied, with the CC anchor made unique."""
HERE = "E:/Archive/GitHub/dlandi/pass32-tools/pass40/"
lines = open(HERE + "apply_review_fixes.py", encoding="utf-8").read().split("\n")
head_end = next(index for index, line in enumerate(lines) if line.startswith("# --- Rules: Infantry OVR"))
rest = next(index for index, line in enumerate(lines) if line.startswith('apply("LimboDancer.Domains.Asl.Play/LiveCloseCombat.cs"'))
text = "\n".join(lines[:head_end] + lines[rest:])
old = ('    ("        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))\\n"\n'
       '     "        {\\n"\n'
       '     "            return \\"The CC record\'s facts do not match the game state.\\";",\n')
new = ('    ("            recorded.HandToHand == true, recorded.InfantryOverrun == true);\\n"\n'
       '     "        if (expected is null)\\n"\n'
       '     "        {\\n"\n'
       '     "            return reason;\\n"\n'
       '     "        }\\n\\n"\n'
       '     "        if (JsonSerializer.Serialize(expected, LiveFire.Json) != JsonSerializer.Serialize(recorded, LiveFire.Json))\\n"\n'
       '     "        {\\n"\n'
       '     "            return \\"The CC record\'s facts do not match the game state.\\";",\n'
       '     "            recorded.HandToHand == true, recorded.InfantryOverrun == true);\\n"\n'
       '     "        if (expected is null)\\n"\n'
       '     "        {\\n"\n'
       '     "            return reason;\\n"\n'
       '     "        }\\n\\n"\n')
assert text.count(old) == 1, text.count(old)
text = text.replace(old, new)
open(HERE + "apply_review_fixes2.py", "w", encoding="utf-8", newline="\n").write(text)
print("ok")
