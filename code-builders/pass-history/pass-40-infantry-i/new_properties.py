"""List the property names the pass added to the records of Rules, Units, Play, and Maps, from the diff against the branch point.

Auto-properties and the positional parameters of records added or widened by the pass. Usage: python new_properties.py
"""
import re
import subprocess

REPO = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
PATHS = ["src/ASL/LimboDancer.Domains.Asl.Rules", "src/ASL/LimboDancer.Domains.Asl.Units", "src/ASL/LimboDancer.Domains.Asl.Play", "src/ASL/LimboDancer.Domains.Asl.Maps"]
diff = subprocess.run(["git", "-C", REPO, "diff", "-U0", "3c660afc..HEAD", "--"] + PATHS, capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
names = set()
removed = []
for line in diff.split("\n"):
    line = line.rstrip("\r")
    if line.startswith("-") and not line.startswith("---"):
        removed.append(line[1:])
    if not line.startswith("+") or line.startswith("+++"):
        continue
    body = line[1:]
    match = re.match(r"^\s+public (?!static|const|sealed|record|class|override|readonly|event)[\w<>?\[\]., ()]+? ([A-Z]\w*)\s*(\{ get; (init|set); \}.*)?$", body)
    if match and "(" not in body.split(match.group(1))[0].replace("(string", "").replace("IReadOnlyList<(", ""):
        names.add(match.group(1))
    record = re.match(r"^\s*public sealed record \w+\((.*)\)", body)
    if record:
        for parameter in re.split(r",\s*(?![^<(]*[>)])", record.group(1)):
            piece = parameter.strip().split("=")[0].strip().split(" ")
            if piece and re.match(r"^[A-Z]\w*$", piece[-1]):
                names.add(piece[-1])
print(",".join(sorted(names)))
print(len(names))
