# Acronym catalog builder

Run from the repository root. The source documents are reconciled by exact identity, definition, and citation fields; the tool never changes them.

```powershell
dotnet run --project code-builders/asl-acronyms/AslAcronyms.csproj -- generate "src/ASL/docs/ASL Rulebook Acronym Scan.md" "src/ASL/docs/ASL Acronym Expanded-Term Search Ledger.csv" src/ASL/LimboDancer.Domains.Asl.MapStudio/Data/acronyms.catalog.json
```

Rebuild MapStudio after generation to embed the new catalog. The builder can compile when the catalog file is absent, but the resource test and verification command must pass before delivery.

```powershell
dotnet run --project code-builders/asl-acronyms/AslAcronyms.csproj -- verify "src/ASL/docs/ASL Rulebook Acronym Scan.md" "src/ASL/docs/ASL Acronym Expanded-Term Search Ledger.csv" src/ASL/LimboDancer.Domains.Asl.MapStudio/Data/acronyms.catalog.json
```

Verification does not write files and exits unsuccessfully for missing or stale output. Console output is the validation report; canonical input hashes and original PDF metadata are in the catalog. PDF metadata is taken from the scan, not independently reverified by this tool. Text hashes normalize an optional BOM and CRLF/CR line endings to UTF-8/LF. Output is deterministic UTF-8/LF JSON.

Feature tests live in `src/ASL/tests/ASL.Acronyms.Tests`, namespace `ASL.Acronyms`. Run only relevant tests, using a class filter during changes and the dedicated project for feature integration. Do not run the whole ASL solution for this feature.

```powershell
dotnet test src/ASL/tests/ASL.Acronyms.Tests/ASL.Acronyms.Tests.csproj
```

`ci_scope.py` selects the acronym job for isolated feature changes and skips tests for changes limited to the three planning documents. It keeps existing jobs for mixed changes. The few shared integration files are compared with their acronym additions removed, so unrelated changes in those files retain the existing checks.
