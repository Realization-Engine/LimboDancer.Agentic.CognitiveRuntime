"""The claim board's check (the Rules-first plan, section 4; questions 1 to 3, ruled 2026-10-28): the rule inventory's claims table
(section 6) against its rows' Owner and Claim state cells, and, for a branch, the Rules files the branch changes against its claims.

    python claim_check.py                                      the ledger alone
    python claim_check.py --base <commit> --branch <name>      the ledger, and the branch's changes from <base> to HEAD
    python claim_check.py --branch <name> --files a.cs,b.cs    the ledger, and the named Rules files as if the branch changed them
    python claim_check.py --touched [--base <commit>]          the ledger, and the files each holding claim's branch changes (default base origin/main)

The ledger:
- each claim has a unique id, an agent (Claude or Codex), a reviewer that is the other agent or "sub-agent", a branch, and a state of
  claimed, built, reviewed, wired, or accepted;
- each Owner cell reads "claim (agent)" and names a claim of that agent; each claim owns at least one row;
- each owned row's Claim state is its claim's state; a row with no owner is open (or blank, when not applicable);
- no Rules file is held by two claims that hold files (claimed or built), unless every one of them lists it "(shared)": a shared hold is for a
  file the claim changes only at its edges (a call into its own calculator, a field on a record), not where its rule is decided.
The branch: every changed file of the Rules project (src/ASL/LimboDancer.Domains.Asl.Rules, its tests excluded) is listed by a claim of
that branch that holds files. Prints every fault and exits 1 if there is any.

Wiring (the plan's section 9.1; the inventory's sections 7 and 8): each wiring row is "W-<rule claim>", names a rule claim of section 6, and is
open with no owner or carries its wiring claim's state; a row is taken only once its rule claim is built; a rule claim at wired or later has
every wiring row wired or later; each wiring claim ("wire-<capability>") owns a row, and no path is held whole by two wiring claims. A wiring
claim's branch changes only the paths it holds (under src/ASL/), their tests, and Rules files (a warning: only to remove the old members its
rows name).

Scope (faults; the Rules conventions, section 2): for a rule claim's branch, every code file it changes outside the Rules project, its
tests, the minigame bundles, and the Rule Workbench, since callers are track 3's, and a rule claim merges without a gate of its own only
while it stays in Rules (the plan's section 4.3, 2026-10-29).

The batch gate (the plan's section 4.3): the rule claims merged into main since the commit the inventory's section 6 records as the last
batch gate, by the merge commits' branches. At five, a rule claim's branch is a fault until the batch gate runs on main
(`gate.sh rules-batch <commit> ...`) and its new commit is recorded; any other branch gets a warning.

Overlaps (warnings, never faults; the plan's section 10): with --base, every file outside the Rules project that this branch changes and that
the branch of another holding claim changes too, by that branch's commits as this checkout sees them (a local branch, else origin's). Play,
Units, MapStudio, and the like are not held, so two claims may touch one of their files; the warning says so early, while it is cheap to
agree who goes first. Documents (*.md) are left out: settle_documents.py merges them.
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
HELD_FILE = re.compile(r"^(\S+\.cs)( \(shared\))?$")


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
        held = collections.OrderedDict()
        for entry in (name.strip() for name in files.split(",")):
            if not entry:
                continue
            m = HELD_FILE.match(entry)
            if not m:
                faults.append(f"claim {claim}: held file {entry!r} must read 'Name.cs' or 'Name.cs (shared)'")
                continue
            held[m.group(1)] = bool(m.group(2))
        claims[claim] = {"agent": agent, "branch": branch, "state": state, "files": held, "rows": []}
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
            for file, shared in claim["files"].items():
                holders[file].append((name, shared))
    for file, held in holders.items():
        if len(held) > 1 and not all(shared for _, shared in held):
            whole = ", ".join(name for name, shared in held if not shared)
            faults.append(f"{file} is held by two claims at once: {', '.join(name for name, _ in held)} ({whole} holding it whole)")
    return claims


WIRING_HEADER = "| Wiring | Rule claim | Sites to rewire | Old members to remove | Behavior changes | Owner | State |"
WIRING_CLAIMS_HEADER = "| Claim | Agent | Reviewer | Branch | Held files | Capability | State | Opened | Note |"
WIRING_ROW_STATES = ["open", "claimed", "wired", "reviewed", "accepted"]
WIRING_STATES = ["claimed", "wired", "reviewed", "accepted"]
WIRED_OR_LATER = {"wired", "reviewed", "accepted"}
BUILT_OR_LATER = {"built", "reviewed", "wired", "accepted"}
CAPABILITIES = {"fire", "movement", "rout", "rally", "close combat", "setup and work", "the turn's boundaries"}
HELD_PATH = re.compile(r"^(\S+)( \(shared\))?$")


def table(lines, header, width, faults, name):
    """The rows of the table under this header, as lists of cells; a row of another width is a fault."""
    at = next((i for i, line in enumerate(lines) if line == header), None)
    if at is None:
        faults.append(f"the inventory has no {name} table")
        return []
    rows = []
    for line in lines[at + 2:]:
        if not line.startswith("|"):
            break
        c = inventory_table.cells(line)
        if len(c) != width:
            faults.append(f"a {name} row has {len(c)} cells: {line[:80]}")
            continue
        rows.append(c)
    return rows


def check_wiring(lines, claims, faults):
    """The wiring rows (section 7) and the wiring claims (section 8) of the plan's section 9.1."""
    wiring = collections.OrderedDict()
    for claim, agent, reviewer, branch, files, capability, state, opened, note in table(lines, WIRING_CLAIMS_HEADER, 9, faults, "wiring claims"):
        if claim in wiring or claim in claims:
            faults.append(f"wiring claim {claim} appears twice, or shares a rule claim's id")
        if not claim.startswith("wire-"):
            faults.append(f"wiring claim {claim}: its id begins 'wire-'")
        if agent not in AGENTS:
            faults.append(f"wiring claim {claim}: agent {agent!r} is not Claude or Codex")
        if reviewer != "sub-agent" and (reviewer not in AGENTS or reviewer == agent):
            faults.append(f"wiring claim {claim}: reviewer {reviewer!r} must be the other agent or sub-agent")
        if state not in WIRING_STATES:
            faults.append(f"wiring claim {claim}: state {state!r} is not one of {', '.join(WIRING_STATES)}")
        if capability not in CAPABILITIES:
            faults.append(f"wiring claim {claim}: capability {capability!r} is not one of the plan's section 9")
        if not branch:
            faults.append(f"wiring claim {claim}: no branch")
        held = collections.OrderedDict()
        for entry in (name.strip() for name in files.split(",")):
            m = HELD_PATH.match(entry) if entry else None
            if entry and not m:
                faults.append(f"wiring claim {claim}: held file {entry!r} must read 'path' or 'path (shared)'")
            elif m:
                held[m.group(1)] = bool(m.group(2))
        wiring[claim] = {"agent": agent, "branch": branch, "state": state, "files": held, "rows": []}
    seen = set()
    for row, rule_claim, sites, old, changes, owner, state in table(lines, WIRING_HEADER, 7, faults, "wiring"):
        if row in seen:
            faults.append(f"wiring row {row} appears twice")
        seen.add(row)
        if row != f"W-{rule_claim}":
            faults.append(f"wiring row {row}: its id is 'W-' and its rule claim's id ({rule_claim})")
        rule = claims.get(rule_claim)
        if rule is None:
            faults.append(f"wiring row {row}: rule claim {rule_claim} is not in the claims table")
        else:
            rule.setdefault("wiring", []).append((row, state))
        if state not in WIRING_ROW_STATES:
            faults.append(f"wiring row {row}: state {state!r} is not one of {', '.join(WIRING_ROW_STATES)}")
        if not owner:
            if state != "open":
                faults.append(f"wiring row {row}: no owner, so its state is open, not {state!r}")
            continue
        m = OWNER.match(owner)
        holder = wiring.get(m.group(1)) if m else None
        if holder is None:
            faults.append(f"wiring row {row}: Owner {owner!r} does not name a wiring claim as 'claim (agent)'")
            continue
        if holder["agent"] != m.group(2):
            faults.append(f"wiring row {row}: Owner says {m.group(2)}, the wiring claims table says {holder['agent']}")
        if state != holder["state"]:
            faults.append(f"wiring row {row}: state {state!r}, its wiring claim {m.group(1)} is {holder['state']}")
        if rule is not None and rule["state"] not in BUILT_OR_LATER:
            faults.append(f"wiring row {row} is taken while its rule claim {rule_claim} is {rule['state']}, not built")
        holder["rows"].append(row)
    for name, claim in claims.items():
        if claim["state"] in WIRED_OR_LATER:
            for row, state in claim.get("wiring", []):
                if state not in WIRED_OR_LATER:
                    faults.append(f"rule claim {name} is {claim['state']}, and its wiring row {row} is {state}")
    holders = collections.defaultdict(list)
    for name, claim in wiring.items():
        if not claim["rows"]:
            faults.append(f"wiring claim {name} owns no wiring row")
        if claim["state"] == "claimed":
            for path, shared in claim["files"].items():
                holders[path].append((name, shared))
    for path, held in holders.items():
        if len(held) > 1 and not all(shared for _, shared in held):
            faults.append(f"{path} is held by two wiring claims at once: {', '.join(name for name, _ in held)}")
    return wiring


def check_wiring_branch(wiring, branch, paths, faults):
    """A wiring claim's branch changes only the files it holds and their tests; a Rules file only to remove the old members its rows name."""
    held = {f"src/ASL/{path}" for claim in wiring.values() if claim["branch"] == branch and claim["state"] == "claimed" for path in claim["files"]}
    for path in paths:
        if not path.startswith("src/") or path.endswith(".md") or path.startswith("src/ASL/tests/") or path.startswith("src/ASL/units/minigames/"):
            continue
        if path.startswith(RULES_DIR):
            print(f"WARNING: {path} is a Rules file; a wiring branch changes Rules only to remove the old members its rows name")
        elif path not in held:
            faults.append(f"{path} is changed by wiring branch {branch} and held by none of its wiring claims (add it by a commit to main first)")


LAST_BATCH = re.compile(r"^\*\*Last batch gate:\*\* `([0-9a-f]{7,40})`")
MERGE = re.compile(r"^Merge pull request #\d+ from [^/\s]+/(\S+)$")
BATCH_SIZE = 5


def batch_gate(lines, claims, branch, faults):
    """The rule claims merged into main since the last batch gate (the plan's section 4.3). At five, the next rule claim waits for one."""
    m = next((LAST_BATCH.match(line) for line in lines if LAST_BATCH.match(line)), None)
    if m is None:
        faults.append("the inventory's section 6 records no last batch gate ('**Last batch gate:** `<commit>`')")
        return
    main = "origin/main" if git("rev-parse", "--verify", "--quiet", "origin/main").returncode == 0 else "HEAD"
    out = git("log", "--merges", "--first-parent", "--format=%s", f"{m.group(1)}..{main}")
    if out.returncode != 0:
        print(f"NOTE: the last batch gate {m.group(1)} is not in this checkout's history; merges since it are not counted")
        return
    branches = {claim["branch"] for claim in claims.values()}
    merged = [b.group(1) for subject in out.stdout.splitlines() if (b := MERGE.match(subject)) and b.group(1) in branches]
    print(f"rule claims merged since the last batch gate {m.group(1)}: {len(merged)} of {BATCH_SIZE}" + (f" ({', '.join(merged)})" if merged else ""))
    if len(merged) >= BATCH_SIZE:
        message = (f"{len(merged)} rule claims merged since the last batch gate {m.group(1)}: run it on main "
                   f"(gate.sh rules-batch {m.group(1)} ...) and record the new commit in the inventory's section 6")
        if any(claim["branch"] == branch for claim in claims.values()):
            faults.append(message + " before this rule claim merges")
        else:
            print("WARNING:", message)


def git(*args):
    return subprocess.run(["git", "-C", REPO, *args], capture_output=True, text=True)


def changed_files(base, ref="HEAD"):
    out = git("diff", "--name-only", f"{base}...{ref}")
    return out.stdout.splitlines() if out.returncode == 0 else None


def changed_rules_files(base):
    return [os.path.basename(path) for path in changed_files(base) or [] if path.startswith(RULES_DIR) and path.endswith(".cs")]


# What a claim may change (the Rules conventions, section 2): the Rules library, its tests, its minigame bundle, and the Rule Workbench.
CLAIM_SCOPE = (RULES_DIR, "src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests/", "src/ASL/units/minigames/",
               "src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Workbench.razor",
               "src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/MinigameBundleTests.cs")


def out_of_scope(paths):
    """The code files a claim's branch changes outside its scope: Play, Units, ASL.Turns, Maps, and the Studio's other pages are track 3's."""
    return sorted(path for path in paths if path.startswith("src/") and not path.endswith(".md") and not path.startswith(CLAIM_SCOPE))


def outside_rules(paths):
    return {path for path in paths if path.startswith("src/") and not path.startswith(RULES_DIR) and not path.endswith(".md")}


def branch_ref(branch):
    for ref in (f"refs/heads/{branch}", f"refs/remotes/origin/{branch}"):
        if git("rev-parse", "--verify", "--quiet", ref).returncode == 0:
            return ref
    return None


def check_branch(claims, branch, files, faults):
    held = {file for claim in claims.values() if claim["branch"] == branch and claim["state"] in HOLDING for file in claim["files"]}
    for file in files:
        if file not in held:
            faults.append(f"{file} is a Rules file this branch changes, and no claim of branch {branch} holds it (claim it first: AGENTS.md)")


def others(claims, branch):
    """The other holding claims' branches this checkout can see, with the files each changes from the base."""
    seen = {}
    for name, claim in claims.items():
        if claim["state"] in HOLDING and claim["branch"] != branch and claim["branch"] not in seen:
            seen[claim["branch"]] = (name, branch_ref(claim["branch"]))
    return seen


def overlaps(claims, branch, base):
    mine = outside_rules(changed_files(base) or [])
    warnings = []
    for other, (name, ref) in others(claims, branch).items():
        if ref is None:
            continue
        theirs = changed_files(base, ref)
        for path in sorted(mine & outside_rules(theirs or [])):
            warnings.append(f"{path} is changed by this branch and by {other} (claim {name}); it is not held, so agree who goes first")
    return warnings


def touched(claims, base):
    for name, (claim_name, ref) in others(claims, None).items():
        if ref is None:
            print(f"{name} (claim {claim_name}): no such branch in this checkout or on origin")
            continue
        paths = changed_files(base, ref) or []
        print(f"{name} (claim {claim_name}, {ref}): {len(paths)} files")
        for path in sorted(p for p in paths if p.startswith("src/") and not p.endswith(".md")):
            print("   ", path)


def main():
    args = sys.argv[1:]

    def value(flag):
        return args[args.index(flag) + 1] if flag in args else None

    faults = []
    lines, _ = inventory_table.read_lines()
    claims = check_ledger(lines, faults)
    wiring = check_wiring(lines, claims, faults)
    branch = value("--branch")
    wiring_branch = any(claim["branch"] == branch for claim in wiring.values())
    files = None
    if value("--files") is not None:
        files = [name.strip() for name in value("--files").split(",") if name.strip()]
    elif value("--base") is not None and "--touched" not in args:
        files = changed_rules_files(value("--base"))
    if files is not None:
        if not branch:
            sys.exit("--base or --files needs --branch")
        if wiring_branch:
            if value("--base") is not None:
                check_wiring_branch(wiring, branch, changed_files(value("--base")) or [], faults)
        else:
            check_branch(claims, branch, files, faults)
    if "--touched" in args:
        touched(claims, value("--base") or "origin/main")
    elif value("--base") is not None:
        for warning in overlaps(claims, branch, value("--base")):
            print("WARNING:", warning)
        if any(claim["branch"] == branch for claim in claims.values()):
            # A rule claim merges without a gate of its own only because it stays in its scope (the plan's section 4.3, 2026-10-29).
            for path in out_of_scope(changed_files(value("--base")) or []):
                faults.append(f"{path} is outside a rule claim's scope (the Rules conventions, section 2): its callers are track 3's, "
                              "and a rule claim merges without a gate only while it stays in Rules")
    batch_gate(lines, claims, branch, faults)
    for fault in faults:
        print("FAULT:", fault)
    held = sum(1 for claim in claims.values() if claim["state"] in HOLDING)
    open_rows = sum(1 for claim in claims.values() for _, state in claim.get("wiring", []) if state == "open")
    print(f"claims: {len(claims)} ({held} holding files); wiring claims: {len(wiring)}; open wiring rows: {open_rows}; "
          f"Rules files checked: {'none' if files is None else len(files)}; faults: {len(faults)}")
    sys.exit(1 if faults else 0)


if __name__ == "__main__":
    main()
