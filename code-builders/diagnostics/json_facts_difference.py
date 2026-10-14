"""Print where two JSON texts (one a line) differ, by path. Usage: python facts_diff.py <file>"""
import json
import os
import sys

if not os.path.exists(sys.argv[1]):
    sys.exit("no dump: the facts matched")
first, second = [json.loads(line) for line in open(sys.argv[1], encoding="utf-8").read().split("\n")[:2]]


def walk(a, b, path):
    if isinstance(a, dict) and isinstance(b, dict):
        for key in sorted(set(a) | set(b)):
            walk(a.get(key, "<absent>"), b.get(key, "<absent>"), path + "." + key)
    elif isinstance(a, list) and isinstance(b, list) and len(a) == len(b):
        for index, (x, y) in enumerate(zip(a, b)):
            walk(x, y, f"{path}[{index}]")
    elif a != b:
        print(path, "| rebuilt:", json.dumps(a)[:200], "| recorded:", json.dumps(b)[:200])


walk(first, second, "facts")
