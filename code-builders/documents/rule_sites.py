"""Where a rule's code is today: every line of the ASL code that cites the rule's numbers or names its words, by project and member.

    python rule_sites.py C9.3 [C9.31 ...] [--words spotter,spotting] [--at <commit>]

A claim runs it first, before its design (AGENTS.md, step 2; the Rules project's CONVENTIONS.md, "Building a claim's calculator"). A rule
cites its subrules too: C9.3 covers C9.31. --words adds words the code uses for the rule (case-insensitive), since many sites name no rule
number: the C9.3 claim's map found the Play page's Spotter list and Play's Deployment check only by the word. --at reads a commit instead of
the working tree.

It prints the Rules project first (the calculators that decide the rule now: one rule, one calculator), then every other project, the Studio
pages included, where a line that decides the rule is a fault to move into Rules. Tests are counted, not listed. Each site is named by the
nearest member declared above it, so a reader sees which methods to open. It only finds; reading each site decides what it is.
"""
import collections
import re
import subprocess
import sys
import os as _os
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.dirname(_os.path.abspath(__file__))))
from paths import REPO  # noqa: E402

ROOT = "src/ASL/"
RULES = "src/ASL/LimboDancer.Domains.Asl.Rules/"
KINDS = ("*.cs", "*.razor", "*.js")
MEMBER = re.compile(r"^\s*(?:\[[^\]]*\]\s*)*(?:(?:public|private|internal|protected|static|override|virtual|async|sealed|readonly|partial)\s+)+"
                    r"[\w<>\[\]?,. ()]*?\b(\w+)\s*(?:<[^>]*>)?\s*\(")
TYPE = re.compile(r"^\s*(?:(?:public|private|internal|protected|static|sealed|abstract|partial|readonly)\s+)*"
                  r"(?:class|record|struct|interface|enum)\s+(\w+)")


def git(*args):
    out = subprocess.run(["git", "-C", REPO, *args], capture_output=True, text=True, encoding="utf-8")
    return out.stdout


def pattern(rule):
    """The rule and its subrules (C9.3, C9.31, C9.312), never a neighbour (C9.4) or a longer section (C19.3)."""
    return re.compile(r"(?<![\w.])" + re.escape(rule) + r"\d*(?!\d)(?![\w])")


def project(path):
    rest = path[len(ROOT):]
    return rest.split("/")[0] if "/" in rest else rest


def is_test(path):
    return "/tests/" in path or ".Tests/" in path or path.endswith("Tests.cs")


KEYWORDS = {"new", "readonly", "static", "const", "return", "if", "for", "foreach", "while", "switch", "using", "nameof", "typeof", "base", "this"}


def member_of(lines, index):
    for i in range(index, -1, -1):
        if lines[i].lstrip().startswith(("//", "*", "/*", "@*")):
            continue
        m = MEMBER.match(lines[i])
        if m and m.group(1) not in KEYWORDS and '"' not in lines[i][:m.end()]:
            return m.group(1)
        t = TYPE.search(lines[i])
        if i < index and t and not lines[i].lstrip().startswith(("//", "*", "@*")):
            return t.group(1)
    return ""


def main():
    args = sys.argv[1:]

    def take(flag):
        if flag in args:
            i = args.index(flag)
            value = args[i + 1]
            del args[i:i + 2]
            return value
        return None

    at = take("--at")
    words = [w.strip() for w in (take("--words") or "").split(",") if w.strip()]
    rules = args
    if not rules and not words:
        sys.exit(__doc__)
    tests = [pattern(rule) for rule in rules] + [re.compile(re.escape(word), re.I) for word in words]
    grep = ["grep", "-n", "-I", "-i", "-F"]
    for needle in rules + words:
        grep += ["-e", needle]
    grep += [at] if at else []
    grep += ["--", *[f"{ROOT}**/{kind}" for kind in KINDS]]
    hits = collections.defaultdict(list)
    for line in git(*grep).splitlines():
        if at:
            line = line[len(at) + 1:]
        path, number, text = line.split(":", 2)
        if any(t.search(text) for t in tests):
            hits[path].append(int(number))
    by_project = collections.OrderedDict()
    test_count = collections.Counter()
    for path in sorted(hits):
        if is_test(path):
            test_count[project(path)] += len(hits[path])
            continue
        by_project.setdefault(project(path), []).append(path)
    order = sorted(by_project, key=lambda name: (not (ROOT + name + "/").startswith(RULES), name))
    for name in order:
        inside = (ROOT + name + "/") == RULES
        print(f"\n{name}{'  (the calculators that decide it now)' if inside else '  (outside Rules: a decision here is a fault)'}")
        for path in by_project[name]:
            source = git("show", f"{at or ''}:{path}").splitlines() if at else open(_os.path.join(REPO, path), encoding="utf-8").read().splitlines()
            members = collections.OrderedDict()
            for number in hits[path]:
                members.setdefault(member_of(source, number - 1) or "(file)", []).append(number)
            listed = ", ".join(f"{m} ({','.join(map(str, n[:4]))}{'...' if len(n) > 4 else ''})" for m, n in members.items())
            print(f"  {path[len(ROOT):]}: {listed}")
    if test_count:
        print("\ntests that cite it: " + ", ".join(f"{name} {n}" for name, n in sorted(test_count.items())))
    files = sum(len(paths) for paths in by_project.values())
    outside = sum(len(paths) for name, paths in by_project.items() if (ROOT + name + "/") != RULES)
    print(f"\nfiles: {files} ({outside} outside Rules); search: {', '.join(rules + words)}{f' at {at}' if at else ''}")


if __name__ == "__main__":
    main()
