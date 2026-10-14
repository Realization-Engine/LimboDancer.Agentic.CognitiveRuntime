"""Increment 2g: the refusals of OrdnanceShotFacts become one-item lists (lines 75 to 171 of GamePlanner.Ordnance.cs), LF kept."""
import io
import re

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/LimboDancer.Domains.Asl.Play/GamePlanner.Ordnance.cs"
with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
    text = handle.read()
start = text.index("OrdnanceShotFacts(GameState state, JsonElement arguments, bool gunDuel = false)")
end = text.index("/// <summary>A shot's proposal in words")
body = text[start:end]
lines = body.split("\n")
changed = 0
for index, line in enumerate(lines):
    match = re.match(r"^(\s*)return \(null, (.*)\);$", line)
    if match and "RefusalReasons.Refusal" not in line:
        lines[index] = f"{match.group(1)}return (null, [{match.group(2)}]);"
        changed += 1
    elif line.rstrip().endswith('by naming it, "') and "return (null, " in line:
        lines[index] = line.replace("return (null, ", "return (null, [", 1)
        nxt = lines[index + 1]
        assert nxt.rstrip().endswith(");"), nxt
        lines[index + 1] = nxt.rstrip()[:-2] + "]);"
        changed += 1
body = "\n".join(lines)
text = text[:start] + body + text[end:]
with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write(text)
print("changed", changed)
