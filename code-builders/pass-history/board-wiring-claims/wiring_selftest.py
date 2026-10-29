"""A self-test of claim_check.py's wiring rules (the plan's section 9.1; the inventory's sections 7 and 8), on made-up tables.

    python wiring_selftest.py        prints each check and exits 1 if any fails
"""
import os
import sys

sys.path.insert(0, os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "documents"))
import claim_check as c  # noqa: E402


def rule(state):
    return {"agent": "Claude", "branch": "claim/x", "state": state, "files": {}, "rows": ["X.1"]}


def wiring_row(row, rule_claim, owner="", state="open"):
    return f"| {row} | {rule_claim} | sites | none | changes | {owner} | {state} |"


def wiring_claim(claim, files, state="claimed", agent="Codex", capability="fire", branch=None):
    reviewer = "Claude" if agent == "Codex" else "Codex"
    return f"| {claim} | {agent} | {reviewer} | {branch or 'wire/' + claim} | {files} | {capability} | {state} | 2026-10-29 | note |"


def run(rules, rows, claims):
    lines = [c.WIRING_HEADER, "|---|"] + rows + ["", c.WIRING_CLAIMS_HEADER, "|---|"] + claims + [""]
    faults = []
    wiring = c.check_wiring(lines, rules, faults)
    return wiring, faults


def main():
    failures = 0

    def check(name, ok, faults=None):
        nonlocal failures
        print(("ok    " if ok else "FAIL  ") + name + ("" if ok or not faults else f"  {faults}"))
        failures += 0 if ok else 1

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.3", "C9.3")], [])
    check("an open row of a built rule claim is sound", not f, f)

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.3", "C9.3", "wire-fire-1 (Codex)", "claimed")],
               [wiring_claim("wire-fire-1", "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs")])
    check("a claimed row of a claimed wiring claim is sound", not f, f)

    _, f = run({"C9.3": rule("claimed")}, [wiring_row("W-C9.3", "C9.3", "wire-fire-1 (Codex)", "claimed")],
               [wiring_claim("wire-fire-1", "a.cs")])
    check("a row is not taken before its rule claim is built", any("not built" in x for x in f), f)

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.3", "C9.3", "wire-fire-1 (Codex)", "wired")],
               [wiring_claim("wire-fire-1", "a.cs")])
    check("a row carries its wiring claim's state", any("its wiring claim" in x for x in f), f)

    _, f = run({"C9.3": rule("wired")}, [wiring_row("W-C9.3", "C9.3")], [])
    check("a wired rule claim needs its wiring rows wired", any("is wired, and its wiring row" in x for x in f), f)

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.4", "C9.3")], [])
    check("a row's id is W- and its rule claim", any("its id is" in x for x in f), f)

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.9", "C9.9")], [])
    check("a row names a rule claim that exists", any("not in the claims table" in x for x in f), f)

    rows = [wiring_row("W-A", "A", "wire-fire-1 (Codex)", "claimed"), wiring_row("W-B", "B", "wire-fire-2 (Claude)", "claimed")]
    rules = {"A": rule("built"), "B": rule("built")}
    _, f = run(rules, rows, [wiring_claim("wire-fire-1", "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs"),
                             wiring_claim("wire-fire-2", "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs", agent="Claude")])
    check("two wiring claims never hold one file whole", any("held by two wiring claims" in x for x in f), f)

    _, f = run(rules, rows, [wiring_claim("wire-fire-1", "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs (shared)"),
                             wiring_claim("wire-fire-2", "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs (shared)", agent="Claude")])
    check("two wiring claims may hold one file shared", not f, f)

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.3", "C9.3", "fire-1 (Codex)", "claimed")],
               [wiring_claim("fire-1", "a.cs", capability="shooting")])
    check("a wiring claim's id begins wire- and names a capability of the plan",
          any("begins 'wire-'" in x for x in f) and any("capability" in x for x in f), f)

    _, f = run({"C9.3": rule("built")}, [wiring_row("W-C9.3", "C9.3")], [wiring_claim("wire-fire-1", "a.cs")])
    check("a wiring claim owns a row", any("owns no wiring row" in x for x in f), f)

    wiring, _ = run({"C9.3": rule("built")}, [wiring_row("W-C9.3", "C9.3", "wire-fire-1 (Codex)", "claimed")],
                    [wiring_claim("wire-fire-1", "LimboDancer.Domains.Asl.Play/LiveOrdnance.cs")])
    f = []
    c.check_wiring_branch(wiring, "wire/wire-fire-1", ["src/ASL/LimboDancer.Domains.Asl.Play/LiveOrdnance.cs",
                                                      "src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/X.cs", "src/ASL/docs/A.md"], f)
    check("a wiring branch may change its held files, tests, and documents", not f, f)
    f = []
    c.check_wiring_branch(wiring, "wire/wire-fire-1", ["src/ASL/LimboDancer.Domains.Asl.Units/State/GameProjector.cs"], f)
    check("a wiring branch may not change a file it does not hold", any("held by none" in x for x in f), f)
    print(f"failures: {failures}")
    sys.exit(1 if failures else 0)


if __name__ == "__main__":
    main()
