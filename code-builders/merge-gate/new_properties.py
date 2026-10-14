"""The properties a branch has and its baseline lacks, as Type.Property, comma-separated: the gate's PROOF_SKIP.

Every C# file the diff base..HEAD touches in Rules, Units, Play, and Maps is parsed whole at the base and at the head; a property is new when the
head has it and the base does not. A record the diff only reformats therefore adds nothing (pass 50's hand list met that). Members of a new
record are listed too; they are reached only through a new property, so naming them is harmless.

Usage: python new_properties.py <base commit> [--count]
"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import re
import subprocess
import sys

REPO = REPO
PATHS = ["src/ASL/LimboDancer.Domains.Asl.Rules", "src/ASL/LimboDancer.Domains.Asl.Units", "src/ASL/LimboDancer.Domains.Asl.Play", "src/ASL/LimboDancer.Domains.Asl.Maps"]
DECLARATION = re.compile(r"^\s*(public|internal|private|file)?\s*(sealed |static |abstract |partial |readonly )*(record|class|struct|interface)\b")
TYPE = re.compile(r"\b(?:record(?: struct| class)?|class|struct|interface) (\w+)")
NOT_A_MEMBER = r"(?!static|const|sealed|record|class|override|readonly|event|interface)"
PROPERTY = re.compile(r"^\s+public " + NOT_A_MEMBER + r"[\w<>?\[\]., ()]+? ([A-Z]\w*)\s*\{ get; (?:init|set); \}")
BARE = re.compile(r"^\s+public " + NOT_A_MEMBER + r"[\w<>?\[\]., ()]+? ([A-Z]\w*)\s*$")
PARAMETER = re.compile(r"(?:^|[,(]\s*)(?:[\w<>?\[\]., ()]+?) ([A-Z]\w*)(?:\s*=\s*[^,)]+)?\s*(?=[,)]|$)")


def git(*arguments):
    return subprocess.run(["git", "-C", REPO, *arguments], capture_output=True, text=True, encoding="utf-8", errors="replace")


def properties(text):
    names = set()
    lines = text.replace("\r", "").split("\n")
    current, in_record = None, False
    for index, line in enumerate(lines):
        declared = TYPE.search(line) if DECLARATION.match(line) else None
        if declared:
            current = declared.group(1)
            in_record = "record" in line and "(" in line and ")" not in line.split("(", 1)[1]
            if "record" in line and "(" in line:
                for match in PARAMETER.finditer(line.split("(", 1)[1]):
                    names.add(f"{current}.{match.group(1)}")
            continue
        if current:
            match = PROPERTY.match(line)
            bare = BARE.match(line)
            if bare and index + 1 < len(lines) and lines[index + 1].strip() == "{":
                names.add(f"{current}.{bare.group(1)}")
            elif match:
                names.add(f"{current}.{match.group(1)}")
            elif in_record:
                for match in PARAMETER.finditer(line.strip()):
                    names.add(f"{current}.{match.group(1)}")
        if in_record and ")" in line and line.rstrip().endswith((")", ");", ") :", "){")):
            in_record = False
    return names


base = sys.argv[1]
changed = [name for name in git("diff", "--name-only", base + "..HEAD", "--", *PATHS).stdout.split("\n") if name.endswith(".cs")]
new = set()
for name in changed:
    head = git("show", "HEAD:" + name)
    if head.returncode != 0:
        continue
    before = git("show", base + ":" + name)
    new |= properties(head.stdout) - (properties(before.stdout) if before.returncode == 0 else set())
if "--count" in sys.argv:
    print(len(new), "new properties in", len(changed), "changed files", file=sys.stderr)
print(",".join(sorted(new)))
