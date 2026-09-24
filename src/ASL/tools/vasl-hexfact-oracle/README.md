# VASL hex-fact oracle

This development tool produces the reference Hex Facts for the F2 fidelity check (ASL-MAP-041 and ASL-MAP-042). It runs VASL's own `Map`, `Hex`, and `Terrain` classes, so the C# derivation in `LimboDancer.Domains.Asl.Maps` is compared with VASL itself rather than with a second reading of VASL's code.

It is not referenced by any .NET project and is not distributed. VASL is licensed under LGPL 2.1; the tool compiles against a local VASL checkout and never copies VASL code or data into this repository. The fixtures it writes contain derived facts only (ASL-MAP-073).

## What it does

`src/HexFactOracle.java` reproduces `BoardArchive.addLOSDatatoVASLMap` for one uncropped, unrotated standard geomorphic board with no overlay or SSR:

1. parse `SharedBoardMetadata.xml` and the board's `BoardMetadata.xml` with VASL's parsers;
2. construct `VASL.LOS.Map.Map` with the standard geometry and the runtime grid configuration `HalfHexWidthLeftHexFullHeight`;
3. read `LOSData` with VASL's loop, then apply railroad embankments, partial orchards, and slopes;
4. run `resetHexTerrain` twice, as VASL's runtime does.

For each hex it writes the center location, the full location chain, base level, stairway, bridge, and all six hexsides as canonical JSON, with keys in ordinal order. The header records the VASL commit, the harness version, and the committed Git blob ids of `LOSData`, `BoardMetadata.xml`, and `SharedBoardMetadata.xml`, read from the checkout's git index. The F2 comparison refuses any fixture whose recorded source differs from the board being checked (`F2-SOURCE-MISMATCH`).

## Regenerating the fixtures

Requirements: a JDK 17 or later (`java` and `javac` on the path), PowerShell 7, and a VASL checkout. The first run downloads Maven and VASL's dependencies, including VASSAL, into `~/.m2` through the checkout's Maven wrapper.

```powershell
./generate-fixtures.ps1 -VaslRoot E:/Archive/GitHub/dlandi/vasl            # the boards that already have fixtures
./generate-fixtures.ps1 -VaslRoot E:/Archive/GitHub/dlandi/vasl 01 02 BFPA  # or -Boards '01','02','BFPA'
```

Fixtures are written gzipped to `src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle/bdNN.hexfacts.json.gz`. VASL is not built with its own Maven configuration, because its pom compiles for Java 11 and cannot read VASSAL 3.7's Java 17 classes. `javac` instead compiles only the VASL classes the harness uses, from the checkout's `src` directory.

## Running F2

```powershell
$env:AslMaps__VaslRoot = 'E:/Archive/GitHub/dlandi/vasl'
dotnet test src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests
```

Without `AslMaps__VaslRoot`, the F2 comparisons are skipped and only the fixture integrity checks run.
