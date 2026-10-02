#!/bin/bash
# The Docker Linux check of the ASL merge gate (ASL Unit Backlog Passes Plan, section 1 and appendix A).
#
# Clones the committed branch into a clean Linux container and runs what the GitHub CI runs: a locked restore, a Release
# build with warnings as errors, every test project of the solution, the ScenarioA1 tests (not in the solution), and the
# source verification and pending comparison regenerations, each compared byte for byte with the committed file. Every
# step must report "== exit 0". The chart supplement regeneration needs jq, so the merge gate runs it locally instead.
#
# Usage (from Git Bash, with Docker Desktop running): bash src/ASL/tools/docker-check/docker_check.sh <branch>
# Commit first: the container clones the branch, so uncommitted changes are not checked.
BRANCH="$1"
if [ -z "$BRANCH" ]; then
  echo "Usage: docker_check.sh <branch>" >&2
  exit 2
fi

# The repository root, four directories above this script, in the Windows form Docker Desktop mounts.
ROOT="$(cd "$(dirname "$0")/../../../.." && (pwd -W 2>/dev/null || pwd))"

MSYS_NO_PATHCONV=1 docker run --rm -v "$ROOT:/repo:ro" mcr.microsoft.com/dotnet/sdk:10.0 bash -c '
  git config --global --add safe.directory "*"
  git clone -q --branch '"$BRANCH"' /repo /work || exit 1
  cd /work
  step() { echo "== $1"; shift; "$@"; echo "== exit $?"; }
  step restore dotnet restore src/ASL/LimboDancer.Domains.Asl.sln --locked-mode -v:minimal
  step build dotnet build src/ASL/LimboDancer.Domains.Asl.sln --configuration Release --no-restore --warnaserror -v:minimal
  step test dotnet test src/ASL/LimboDancer.Domains.Asl.sln --configuration Release --no-build --no-restore
  step a1 dotnet test src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests/LimboDancer.Domains.Asl.Rules.Tests.csproj --configuration Release --warnaserror -p:RestoreLockedMode=false
  CLI="dotnet run --project src/ASL/LimboDancer.Domains.Asl.Authoring.Cli --configuration Release --no-build --no-restore -- --source-commit a3254ff1d492dbdd28483d86f5b42437b48e80d4 --registry-output /tmp/reg.json --verification-output /tmp/ver.json --a1-attestation docs/ASL/SourceRegistry/asl-scenario-a1.source-attestation.json"
  step ci-verification bash -c "$CLI --a1-verification-output /tmp/a1ver.json >/dev/null && cmp docs/ASL/SourceRegistry/asl-scenario-a1.source-verification.json /tmp/a1ver.json"
  step ci-pending bash -c "$CLI --a1-comparison docs/ASL/SourceRegistry/asl-scenario-a1.first-case-pdf-comparison.json --a1-comparison-output /tmp/pending.json >/dev/null && cmp docs/ASL/SourceRegistry/asl-scenario-a1.pending-comparison-records.json /tmp/pending.json"
  echo "== done"'
