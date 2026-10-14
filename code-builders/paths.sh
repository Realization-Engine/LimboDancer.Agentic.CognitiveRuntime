# Sourced by every shell tool of code-builders: where the tools, the repository, and the working folder are.
#
#   TOOLS  this folder
#   R      the repository's root
#   WORK   the working folder: the proof store, baselines, digests, sweeps, logs, and the built copy of the baseline's sources.
#          It is outside the repository, on a short path (a path inside the repository is too long for the Oracle fixtures of a
#          built copy of the sources), and nothing in it is committed. Set ASL_GATE_WORK to use another folder.
TOOLS="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
R="$(cd "$TOOLS/.." && pwd)"
WORK="${ASL_GATE_WORK:-E:/Archive/GitHub/dlandi/asl-gate-work}"
export ASL_GATE_WORK="$WORK"
# The proof tool is a Windows program: it takes the repository's root in the form C:/..., from ASL_REPO.
export ASL_REPO="$(cygpath -m "$R")"
