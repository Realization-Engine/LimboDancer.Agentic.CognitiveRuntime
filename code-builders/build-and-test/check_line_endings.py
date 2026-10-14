"""Count CR bytes, BOMs, and em-dashes in the files the working tree changed or added: eol_2e.py"""
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.join(_os.path.dirname(_os.path.abspath(__file__)), ".."))
from paths import REPO, WORK  # noqa: E402
import subprocess

REPO = REPO
names = subprocess.run(["git", "-C", REPO, "diff", "--name-only"], capture_output=True, text=True).stdout.split()
names.append("src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1ArmorLeaders.cs")
bad = 0
for name in names:
    data = open(REPO + "/" + name, "rb").read()
    cr = data.count(b"\r")
    bom = data.startswith(b"\xef\xbb\xbf")
    dash = data.count("\u2014".encode("utf-8"))
    if cr or bom:
        bad += 1
        print("CR %d BOM %s %s" % (cr, bom, name))
    if dash:
        print("em-dashes %d %s" % (dash, name))
print("files", len(names), "with CR or BOM", bad)
