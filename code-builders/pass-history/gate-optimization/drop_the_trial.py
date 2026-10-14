"""The gate without its trial stage (the user, 2026-10-13).

The trial was built to protect proofs of fifty minutes. They take seven now. Its one check that nothing else makes, a plan whose hash
alone differs in a game made before the pass, moves to the proofs' first half minute (early_sweep_check.py); the pass's own games are
already replayed and swept on the baseline's side by the baseline stage, which stops on a game that ends in an error.

Usage: python drop_the_trial.py <repository root>
"""
import sys

ROOT = sys.argv[1].rstrip("/")
GATE = ROOT + "/code-builders/merge-gate/gate.sh"
README = ROOT + "/code-builders/README.md"
RULES = ROOT + "/src/ASL/docs/Prompts/Pass Standing Rules and Harness Lessons.md"


def read(path):
    with open(path, encoding="utf-8", newline="") as f:
        text = f.read()
    assert "\r\n" not in text, path
    return text


gate = read(GATE)
start = gate.index("trial)\n")
end = gate.index("  ;;\n", start) + len("  ;;\n")
assert gate[start:end].count("\n") == 12, gate[start:end].count("\n")
gate = gate[:start] + gate[end:]
EDITS = {
    GATE: [
        ("#   trial     the trial games, the pass's own games, and its extra sweep games on both sides; must pass       (about 2 min)\n", "", 1),
        ("#   proofs    text list, replay digest, sweep on the gate's side, each compared with the baseline            (about 7 min; refuses without a\n"
         "#             passed trial of this very build and these very skip lists)\n",
         "#   proofs    text list, replay digest, sweep on the gate's side, each compared with the baseline            (about 7 min; it first sweeps\n"
         "#             a few older games and stops if a plan's hash alone differs, which is a fault of the skip lists)\n", 1),
        ("its recorded games, extra sweep and trial games, the JSON keys it added,", "its recorded games, extra sweep games, the JSON keys it added,", 1),
        ('TRIAL_BASE="pass16-demo2,p26-demo,p25-visual,p31c-tw-r459,p31c-tw-r471"\n',
         "# The few games of the proofs' early check: three small games and two cuts of p31c-tw that plan moves and fire.\n"
         'EARLY="=pass16-demo2,=p26-demo,=p25-visual,p31c-tw-r459,p31c-tw-r471"\n', 1),
        ('TRIAL="$TRIAL_BASE${TRIAL_EXTRA:+,$TRIAL_EXTRA}"\n', "", 1),
        ('BEFORE="$S/before-$PASS"; AFTER="$S/after-$PASS"; TRIALDIR="$S/trial-$PASS"', 'BEFORE="$S/before-$PASS"; AFTER="$S/after-$PASS"', 1),
        ("# The skip lists of the gate's side, and the signature a passed trial is bound to: the build and both lists.",
         "# The skip lists of the gate's side, and the signature a finished proofs stage is marked with: the build and both lists.", 1),
        ('  if [ ! -f "$TRIALDIR/OK" ] || [ "$(cat "$TRIALDIR/OK")" != "$SIGNATURE" ]; then stop "no passed trial of this build and these skip lists. Run the trial stage first."; fi\n', "", 1),
        ('  rm -rf "$AFTER"; mkdir -p "$AFTER"\n  python "$G/text_list.py" "$AFTER/textlist.txt" < /dev/null\n',
         '  rm -rf "$AFTER"; mkdir -p "$AFTER"\n'
         '  run gate_side sweep "$AFTER/sweep-early.txt" "$EARLY"\n'
         '  python "$G/early_sweep_check.py" "$BEFORE/sweep.txt" "$AFTER/sweep-early.txt" < /dev/null || exit 1\n'
         '  python "$G/text_list.py" "$AFTER/textlist.txt" < /dev/null\n', 1),
        ('sed -n 2,30p "$0"', 'sed -n 2,29p "$0"', 2),
    ],
    README: [("at the gate `baseline`, `build`, `trial`, and `proofs` on this machine, about a quarter of an hour.",
              "at the gate `baseline`, `build`, and `proofs` on this machine, about a quarter of an hour.", 1)],
    RULES: [
        ("For the gate that is the trial stage of `gate.sh`, which the proofs stage enforces; it holds the pass's own games and its extra sweep games on both sides.",
         "For the gate that is the first half minute of the `proofs` stage, which sweeps a few games made before the pass and stops if a plan's hash alone differs; the `baseline` stage has by then replayed and swept the pass's own games on main's side, and stops on a game that ends in an error. (The trial was a stage of its own until 2026-10-13, when the proofs took seven minutes and not fifty.)", 1),
        ("`baseline`, which reuses what `base` made; `build`; `trial`, which must pass; `proofs`. Every differing line",
         "`baseline`, which reuses what `base` made; `build`; `proofs`. Every differing line", 1),
        ("at the gate `baseline`, `build`, `trial`, `proofs`, then CI on the pass's pull request (`ci`); `suite` and `docker` on demand.",
         "at the gate `baseline`, `build`, `proofs`, then CI on the pass's pull request (`ci`); `suite` and `docker` on demand.", 1),
    ],
}
out = {}
for path, edits in EDITS.items():
    text = gate if path == GATE else read(path)
    for old, new, count in edits:
        assert text.count(old) == count, (path[-30:], old[:70], text.count(old))
        text = text.replace(old, new)
    out[path] = text
assert "TRIAL" not in out[GATE].replace("TRIAL_EXTRA", ""), "a mention of the trial is left in gate.sh"
for path, text in out.items():
    with open(path, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", path.rsplit("/", 1)[1])
