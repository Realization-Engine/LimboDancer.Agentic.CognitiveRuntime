#!/bin/bash
# Pass 55's reference for the replay digest during the build: main's sources at <commit> built into pass55/base-bin, the proof tool bound to
# that build (proof-base55), and its digest over <store> (default: the whole store) into after-55-base<suffix>/replay.txt.
#   base.sh build <commit>          archive, build, bind the tool
#   base.sh replay [store] [suffix] run the digest
# Both sides of a comparison run with one and the same PROOF_SKIP: GameState.Catalog (the catalog's identity went to 1.14.0 in increment 2a, and
# it is hashed into every state), and whatever pass 55 has added, which main's side does not hold and so does not miss.
S="/e/Archive/GitHub/dlandi/pass32-tools"
R="/e/Archive/GitHub/dlandi/LimboDancer.MCP"
P="$S/pass55"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 MSBUILDDISABLENODEREUSE=1
case "$1" in
build)
  rm -rf "$P/base-src" "$P/base-bin" "$S/proof-base55"; mkdir -p "$P/base-src" "$S/proof-base55"
  git -C "$R" archive --format=tar -o "$P/base.tar" "$2" && tar -xf "$P/base.tar" -C "$P/base-src" || { echo "STOP: the archive failed"; exit 1; }
  if grep -rqs "ParseAmmunition" "$P/base-src/src/ASL" --include=*.cs; then echo "STOP: the base holds pass 55"; exit 1; fi
  if ! grep -rqs "VehicleWeaponShots" "$P/base-src/src/ASL" --include=*.cs; then echo "STOP: the base lacks pass 50"; exit 1; fi
  dotnet build "$P/base-src/src/ASL/LimboDancer.Domains.Asl.MapStudio/LimboDancer.Domains.Asl.MapStudio.csproj" -c Debug -o "$P/base-bin" --disable-build-servers -m:1 -v:q > "$P/base-build.log" 2>&1
  if ! grep -q "Build succeeded" "$P/base-build.log"; then echo "STOP: the base did not build"; tail -5 "$P/base-build.log"; exit 1; fi
  cp "$S/proof/Program.cs" "$S/proof/Proof.csproj" "$S/proof-base55/"
  dotnet build "$S/proof-base55/Proof.csproj" "-p:StudioBin=$P/base-bin/" "-p:OutDir=$S/proof-base55/out/" --disable-build-servers -m:1 -v:q > "$P/base-proof-build.log" 2>&1
  if ! grep -q "Build succeeded" "$P/base-proof-build.log"; then echo "STOP: the proof tool did not build against the base"; grep -m5 error "$P/base-proof-build.log"; exit 1; fi
  cmp -s "$S/proof-base55/out/LimboDancer.Domains.Asl.Play.dll" "$P/base-bin/LimboDancer.Domains.Asl.Play.dll" || { echo "STOP: the tool's copy is not the base's build"; exit 1; }
  echo "[$(date +%H:%M:%S)] base built from $2"
  ;;
replay)
  STORE="${2:-$S/store}"
  OUT="$S/after-55-base$3"
  mkdir -p "$OUT"
  export PROOF_SKIP="GameState.Catalog,DefinitionReference.Catalog${PROOF_SKIP_55:+,$PROOF_SKIP_55}"
  export PROOF_SKIP_JSON="$PROOF_SKIP_JSON_55"
  echo "PROOF_SKIP=$PROOF_SKIP"
  dotnet "$S/proof-base55/out/Proof.dll" replay "$OUT/replay.txt" "$STORE" > "$OUT/replay.log" 2>&1 || echo "replay exit $?"
  tail -1 "$OUT/replay.log" | head -c 300; echo
  echo "[$(date +%H:%M:%S)] base replay done: $(wc -l < "$OUT/replay.txt") lines"
  ;;
esac
