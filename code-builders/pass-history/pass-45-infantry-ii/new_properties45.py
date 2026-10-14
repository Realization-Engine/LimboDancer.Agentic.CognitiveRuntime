"""List, as Type.Property, the properties pass 45 added to the records of Rules, Units, Play, and Maps: for PROOF_SKIP at the gate.

A property is an added line inside a type: an auto-property, or a positional parameter of a record. The type is the nearest record, class, or
struct declared above the line in the file as it stands. Usage: python new_properties45.py
"""
import re
import subprocess

REPO = "E:/Archive/GitHub/dlandi/LimboDancer.MCP"
BASE = "5982767e"
PATHS = ["src/ASL/LimboDancer.Domains.Asl.Rules", "src/ASL/LimboDancer.Domains.Asl.Units", "src/ASL/LimboDancer.Domains.Asl.Play", "src/ASL/LimboDancer.Domains.Asl.Maps"]
diff = subprocess.run(["git", "-C", REPO, "diff", "-U0", BASE + "..HEAD", "--"] + PATHS, capture_output=True, text=True, encoding="utf-8", errors="replace").stdout
added = {}
name = None
for line in diff.split("\n"):
    line = line.rstrip("\r")
    if line.startswith("+++ b/"):
        name = line[6:]
    elif line.startswith("@@"):
        at = int(re.search(r"\+(\d+)", line).group(1))
    elif line.startswith("+") and not line.startswith("+++"):
        added.setdefault(name, set()).add(at)
        at += 1

TYPE = re.compile(r"\b(?:record(?: struct| class)?|class|struct|interface) (\w+)")
PROPERTY = re.compile(r"^\s+public (?!static|const|sealed|record|class|override|readonly|event|interface)[\w<>?\[\]., ()]+? ([A-Z]\w*)\s*\{ get; (?:init|set); \}")
PARAMETER = re.compile(r"(?:^|[,(]\s*)(?:[\w<>?\[\]., ()]+?) ([A-Z]\w*)(?:\s*=\s*[^,)]+)?\s*(?=[,)]|$)")
names = set()
for path, numbers in added.items():
    if not path.endswith(".cs"):
        continue
    lines = open(REPO + "/" + path, encoding="utf-8").read().split("\n")
    current, in_record = None, False
    for number, line in enumerate(lines, 1):
        declared = TYPE.search(line) if re.match(r"^\s*(public|internal|private|file)?\s*(sealed |static |abstract |partial |readonly )*(record|class|struct|interface)\b", line) else None
        if declared:
            current = declared.group(1)
            in_record = "record" in line and "(" in line and ")" not in line.split("(", 1)[1]
            if number in numbers and "record" in line and "(" in line:
                for match in PARAMETER.finditer(line.split("(", 1)[1]):
                    names.add(f"{current}.{match.group(1)}")
            continue
        if number in numbers and current:
            match = PROPERTY.match(line)
            bare = re.match(r"^\s+public (?!static|const|sealed|record|class|override|readonly|event|interface)[\w<>?\[\]., ()]+? ([A-Z]\w*)\s*$", line)
            if bare and number < len(lines) and lines[number].strip() == "{":
                names.add(f"{current}.{bare.group(1)}")
            elif match:
                names.add(f"{current}.{match.group(1)}")
            elif in_record:
                for match in PARAMETER.finditer(line.strip()):
                    names.add(f"{current}.{match.group(1)}")
        if in_record and ")" in line and line.rstrip().endswith((")", ");", ") :", "){")):
            in_record = False
print(",".join(sorted(names)))
print(len(names))
