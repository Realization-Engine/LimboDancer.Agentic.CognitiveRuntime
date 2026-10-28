"""The claim board's check (the Rules-first plan, section 4; questions 1 to 3, ruled 2026-10-28): the rule inventory's claims table
(section 6) against its rows' Owner and Claim state cells, and, for a branch, the Rules files the branch changes against its claims.

    python claim_check.py                                      the ledger alone
    python claim_check.py --base <commit> --branch <name>      the ledger, and the branch's changes from <base> to HEAD
    python claim_check.py --branch <name> --files a.cs,b.cs    the ledger, and the named Rules files as if the branch changed them

The ledger:
- each claim has a unique id, an agent (Claude or Codex), a reviewer that is the other agent or "sub-agent", a branch, and a state of
  claimed, built, reviewed, wired, or accepted;
- each Owner cell reads "claim (agent)" and names a claim of that agent; each claim owns at least one row;
- each owned row's Claim state is its claim's state; a row with no owner is open (or blank, when not applicable);
- no Rules file is listed by two claims that hold files (claimed or built).
The branch: every changed file of the Rules project (src/ASL/LimboDancer.Domains.Asl.Rules, its tests excluded) is listed by a claim of
that branch that holds files. Prints every fault and exits 1 if there is any.
"""
import collections
import os
import os as _os
import re
import subprocess
import sys
import sys as _sys
_sys.path.insert(0, _os.path.dirname(_os.path.abspath(__file__)))
import inventory_table  # noqa: E402
from paths import REPO  # noqa: E402

AGENTS = {"Claude", "Codex"}
STATES = ["claimed", "built", "reviewed", "wired", "accepted"]
HOLDING = {"claimed", "built"}
RULES_DIR = "src/ASL/LimboDancer.Domains.Asl.Rules/"
CLAIMS_HEADER = "| Claim | Agent | Reviewer | Branch | Calculator files | State | Opened | Note |"
OWNER = re.compile(r"^(\S+) \((Claude|Codex)\)$")


def read_claims(lines, faults):
    claims = collections.OrderedDict()
    at = next((i for i, line in enumerate(lines) if line == CLAIMS_HEADER), None)
    if at is None:
        faults.append("the inventory has no claims table (section 6)")
        return claims
    for line in lines[at + 2:]:
        if not line.startswith("|"):
            break
        c = inventory_table.cells(line)
        if len(c) != 8:
            faults.append(f"a claims row has {len(c)} cells: {line[:80]}")
            continue
        claim, agent, reviewer, branch, files, state, opened, note = c
        if claim in claims:
            faults.append(f"claim {claim} appears twice")
        if agent not in AGENTS:
            faults.append(f"claim {claim}: agent {agent!r} is not Claude or Codex")
        if reviewer != "sub-agent" and (reviewer not in AGENTS or reviewer == agent):
            faults.append(f"claim {claim}: reviewer {reviewer!r} must be the other agent or sub-agent (question 3)")
        if state not in STATES:
            faults.append(f"claim {claim}: state {state!r} is not one of {', '.join(STATES)}")
        if not branch:
            faults.append(f"claim {claim}: no branch")
        claims[claim] = {"agent": agent, "branch": branch, "state": state,
                         "files": [name.strip() for name in files.split(",") if name.strip()], "rows": []}
    return claims


def check_ledger(lines, faults):
    claims = read_claims(lines, faults)
    for row in inventory_table.rows(lines):
        owner, state = row.get("Owner", ""), row.get("Claim state", "")
        if not owner:
            want = "" if row["Status"] == "not applicable" else "open"
            if state != want:
                faults.append(f"{row['§']}: no owner, so its claim state is {want or 'blank'}, not {state!r}")
            continue
        m = OWNER.match(owner)
        if not m:
            faults.append(f"{row['§']}: Owner {owner!r} does not read 'claim (agent)'")
            continue
        claim = claims.get(m.group(1))
        if claim is None:
            faults.append(f"{row['§']}: Owner names claim {m.group(1)}, which is not in the claims table")
            continue
        if claim["agent"] != m.group(2):
            faults.append(f"{row['§']}: Owner says {m.group(2)}, the claims table says {claim['agent']}")
        if state != claim["state"]:
            faults.append(f"{row['§']}: Claim state {state!r}, its claim {m.group(1)} is {claim['state']}")
        claim["rows"].append(row["§"])
    holders = collections.defaultdict(list)
    for name, claim in claims.items():
        if not claim["rows"]:
            faults.append(f"claim {name} owns no row of the inventory")
        if claim["state"] in HOLDING:
            for file in claim["files"]:
                holders[file].append(name)
    for file, names in holders.items():
        if len(names) > 1:
            faults.append(f"{file} is held by two claims at once: {', '.join(names)}")
    return claims


def changed_rules_files(base):
    out = subprocess.run(["git", "-C", REPO, "diff", "--name-only", f"{base}...HEAD", "--", RULES_DIR], capture_output=True, text=True, check=True)
    return [os.path.basename(path) for path in out.stdout.splitlines() if path.endswith(".cs")]


def check_branch(claims, branch, files, faults):
    held = {file for claim in claims.values() if claim["branch"] == branch and claim["state"] in HOLDING for file in claim["files"]}
    for file in files:
        if file not in held:
            faults.append(f"{file} is a Rules file this branch changes, and no claim of branch {branch} holds it (claim it first: AGENTS.md)")


def main():
    args = sys.argv[1:]

    def value(flag):
        return args[args.index(flag) + 1] if flag in args else None

    faults = []
    lines, _ = inventory_table.read_lines()
    claims = check_ledger(lines, faults)
    branch = value("--branch")
    files = None
    if value("--files") is not None:
        files = [name.strip() for name in value("--files").split(",") if name.strip()]
    elif value("--base") is not None:
        files = changed_rules_files(value("--base"))
    if files is not None:
        if not branch:
            sys.exit("--base or --files needs --branch")
        check_branch(claims, branch, files, faults)
    for fault in faults:
        print("FAULT:", fault)
    held = sum(1 for claim in claims.values() if claim["state"] in HOLDING)
    print(f"claims: {len(claims)} ({held} holding files); Rules files checked: {'none' if files is None else len(files)}; faults: {len(faults)}")
    sys.exit(1 if faults else 0)


if __name__ == "__main__":
    main()
