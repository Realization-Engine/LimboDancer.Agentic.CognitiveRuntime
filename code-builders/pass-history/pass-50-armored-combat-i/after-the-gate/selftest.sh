#!/bin/bash
# The gate script's own check, on pass 50's finished gate: a trial with pass 50's lists passes; a trial with one of pass 45's properties added
# (the fault of pass 50's first run) fails; and the proofs stage refuses to start after the failed trial. Needs before-50/TOOL, main-bin of
# b4ce31ef, and bin/gate of pass 50. About one minute.
S="/e/Archive/GitHub/dlandi/pass32-tools"
BASE=b4ce31ef
cd "$S" || exit 1
rm -rf before-50x trial-50x after-50x; mkdir -p before-50x
cp before-50/TOOL before-50x/TOOL
cp gate/pass50.conf gate/pass50x.conf
echo 'SKIP_ADD="FireFirer.Berserk"' >> gate/pass50x.conf
bash gate/gate.sh 50 $BASE trial | grep -E "TRIAL|STOP"
echo "-- with a property the baseline also holds (expect TRIAL FAILED):"
bash gate/gate.sh 50x $BASE trial | grep -E "FAULT|TRIAL|STOP" | cut -c1-200
echo "-- the proofs after it (expect STOP):"
bash gate/gate.sh 50x $BASE proofs | grep -E "STOP|replay" | cut -c1-200
rm -rf before-50x trial-50x after-50x gate/pass50x.conf
