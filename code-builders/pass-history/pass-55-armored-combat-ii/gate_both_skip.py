"""Give gate.sh a BOTH_SKIP list: properties left out on both sides (pass 55: the catalog's identity, hashed into every state, went to 1.14.0)."""
import io

PATH = "E:/Archive/GitHub/dlandi/pass32-tools/gate/gate.sh"
EDITS = [
    ('OWN=""; SWEEP_EXTRA=""; TRIAL_EXTRA=""; SKIP_JSON=""; SKIP_ADD=""; PRESENT=""; ABSENT=""',
     'OWN=""; SWEEP_EXTRA=""; TRIAL_EXTRA=""; SKIP_JSON=""; SKIP_ADD=""; BOTH_SKIP=""; PRESENT=""; ABSENT=""'),
    ('  GATE_SKIP=$(python "$S/gate/new_properties.py" "$BASEFULL" < /dev/null)${SKIP_ADD:+,$SKIP_ADD}',
     '  GATE_SKIP=$(python "$S/gate/new_properties.py" "$BASEFULL" < /dev/null)${SKIP_ADD:+,$SKIP_ADD}${BOTH_SKIP:+,$BOTH_SKIP}'),
    ('  SIGNATURE="$(cat "$GATE/LimboDancer.Domains.Asl.Play.dll" "$GATE/LimboDancer.Domains.Asl.Rules.dll" "$GATE/LimboDancer.Domains.Asl.Units.dll" | sha256sum | cut -c1-16) $(echo "$GATE_SKIP|$GATE_SKIP_JSON|$BASEFULL" | sha256sum | cut -c1-16)"',
     '  SIGNATURE="$(cat "$GATE/LimboDancer.Domains.Asl.Play.dll" "$GATE/LimboDancer.Domains.Asl.Rules.dll" "$GATE/LimboDancer.Domains.Asl.Units.dll" | sha256sum | cut -c1-16) $(echo "$GATE_SKIP|$GATE_SKIP_JSON|$BASEFULL" | sha256sum | cut -c1-16)"\n  echo "BOTH_SKIP (left out on the baseline\'s side too): $BOTH_SKIP"'),
    ('main_side() { ( unset PROOF_SKIP PROOF_SKIP_JSON; dotnet "$MAINTOOL/Proof.dll" "$@" ); }',
     '# BOTH_SKIP (pass 55): what differs in every state for a reason that is no rule, such as the catalog\'s identity after a version change, is\n'
     '# left out on the baseline\'s side as well; without it every line differs and the run is void.\n'
     'main_side() { ( unset PROOF_SKIP PROOF_SKIP_JSON; export PROOF_SKIP="$BOTH_SKIP"; dotnet "$MAINTOOL/Proof.dll" "$@" ); }'),
    ('# What a pass supplies is gate/pass<pass>.conf (see pass50.conf): its recorded games, extra sweep and trial games, and the JSON keys it added.',
     '# What a pass supplies is gate/pass<pass>.conf (see pass50.conf): its recorded games, extra sweep and trial games, the JSON keys it added,\n'
     '# and BOTH_SKIP, the properties left out on both sides (a changed catalog identity).'),
]
text = io.open(PATH, encoding="utf-8", newline="").read()
for old, new in EDITS:
    assert text.count(old) == 1, (text.count(old), old[:60])
for old, new in EDITS:
    text = text.replace(old, new)
io.open(PATH, "w", encoding="utf-8", newline="").write(text)
print("gate.sh: BOTH_SKIP added")
