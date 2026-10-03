# ASL personnel name pools

These JSON files supply candidate names for the [Named Counters design](<../../docs/ASL Named Counters Design.md>). They cover the listed ASL nationality contexts, including the Korean War, through shared cultural pools and explicit choices for multilingual forces.

**Status:** Editorial drafts. Structural checks do not establish that each name was used by a serviceman in the scenario's period. The lists are AI-assisted editorial compilations, with convention references recorded in the manifest. No real person's service or identity is implied by a generated combination.

## Files and entry points

- `manifest.json` is the lookup entry point: pool IDs and files, nationality contexts, force affiliations, sources, review status, counts, and content hashes.
- `*.names.json` holds each pool's weighted given names and second components, naming convention, subgroup filters, and scope limitations.
- `name-pool.schema.json` and `name-manifest.schema.json` specify the Draft 7 JSON contracts.
- [INVENTORY.md](INVENTORY.md) reports actual counts and smaller pools.

The existing `LimboDancer.Domains.Asl.Units` resource pattern is the intended deployment mechanism. These new assets are not yet embedded or consumed by runtime code. C# readers, Scenario Card serialization, and UI integration are subsequent implementation work.

## Query and selection contract

1. If selecting by force, resolve `forceAffiliations[].nationalityContextId`. A force's rules context does not change its personnel's culture.
2. Look up `nationalityContexts[]` by `id`. Use its non-null `defaultPoolId`, or require an explicit choice from `poolIds`.
3. Find that pool in `pools[]`, read its `file`, and validate its format/version and content hash.
4. If `requiresSubgroup` is true, choose one of `subgroups[].id` and filter tagged entries before sampling. Untagged given names are shared within that pool. Every subgroup-restricted second component has explicit membership.
5. Select whole entries using their positive integer `weight`. Weights are editorial preferences, not historical frequency estimates. Uniform weights are intentional where no preference was assigned.
6. Preserve name components. Follow `convention.componentOrder` and `separator` when constructing a short display name. Never split a compound name on spaces or treat its final word as a surname automatically.
7. Reject identical first/second text, compare normalized full names for duplicates within the card, and bound retries. `availableCombinations` counts distinct valid short display names per subgroup, after those constraints. Sampling weights do not guarantee unique results.
8. Save the selected entries and concrete display name into the card. Reopening a card or starting/replaying a game must not rerun selection.

Missing context or an unsupported population requires authored names or an explicit supported choice. There is no fallback to English or any other pool. An authored name always takes precedence. Support weapons receive no person name or assignment.

This read-only example queries a pool without generating a person:

```python
import json
from pathlib import Path

root = Path('src/ASL/units/names')
manifest = json.loads((root / 'manifest.json').read_text(encoding='utf-8'))
context = next(c for c in manifest['nationalityContexts'] if c['id'] == 'polish')
pool_id = context['defaultPoolId']
record = next(p for p in manifest['pools'] if p['id'] == pool_id)
pool = json.loads((root / record['file']).read_text(encoding='utf-8'))
given_names = [entry['text'] for entry in pool['givenNames']]
family_names = [entry['text'] for entry in pool['familyNames']]
```

For a multilingual context, inspect `poolIds` instead of assuming `defaultPoolId` exists. Lookup maps can be built once by ID and cached in the future C# reader.

## Naming conventions and limits

- Chinese, Japanese, Korean, and Hungarian pools use family-first display. Romanized East Asian forms are a display choice; native characters are not inferred. Chinese and Korean compound given-name candidates need per-entry period review.
- Tamil, Amharic, Eritrean, and the narrow Kikuyu pool store second components in `patronymics` with role `father-given`. `familyNames` is empty. These are not fabricated hereditary surnames. The display convention is a project short form, not a universal civil-record layout.
- Sikh short forms use `communityNames` with Singh and role `community`. Additional clan or family names can be authored. This pool intentionally has one second component and only 100 short-form combinations.
- Gurkha names keep compounds such as Ram Bahadur intact. The pool has 12 short family/community labels and requires a recruiting subgroup. It does not fabricate 100 surnames to satisfy a numerical target.
- Thai entries require a Thai or Sino-Thai surname subgroup and local review. Transliteration and historical suitability remain editorial questions.
- Colombian short forms use one family surname; full civil names may carry another authored family component. Philippine short forms likewise do not automatically construct a maternal surname.
- These initial lists target male personnel. Women can have authored identities; dedicated female pools are not part of this data pass.

Country coverage is not comprehensive ethnic coverage. British India, the Soviet Union, Yugoslavia, Ethiopia, Eritrea, French colonial forces, and the King's African Rifles contain populations beyond the included pools. The New Zealand Anglophone pool is not a Māori pool, and European Dutch names must not silently stand in for indigenous Dutch East Indies personnel. The manifest describes the available choices and gaps explicitly.

## American personnel

`american.names.json` is the default for both `us-army` and `us-marines` through the `american` nationality context. It combines 100 given names from the SSA 1900-1909 male top-100 table with a broader editorial surname collection (target: at least 200). The source represents one relevant birth cohort, not all servicemen's ages. Uniform weights do not claim historical frequency or demographic proportions.

American given names can combine with surnames from multiple naming traditions without choosing a European nationality for each person. Names do not establish ethnicity, race, religion, or immigration status. The general pool does not reproduce any historical unit's composition. Unit-specific profiles and date-sensitive formation restrictions remain future authoring work; use authored identities where the general pool is unsuitable, including Japanese American formations. Authored and previously saved names remain unchanged. KATUSA and Philippine forces retain their separate contexts.

## Validation and maintenance

The ASL Authoring CI workflow runs both schema and integrity checks in a separate job on applicable pull requests and pushes to `main`, `decision-plane`, and `asl-narrative-extensions`.

Versions use `major.minor.patch`. Major `1` identifies the supported format contract; minor and patch numbers identify compatible data revisions, so `1.0.1` and `1.1.0` are accepted. Breaking format changes require a new major and reader support. Manifest and pool versions evolve independently, but each pool's version must exactly match its manifest registration. Schema validation checks structure; the integrity validator enforces supported version compatibility. Updating a version does not bypass content-hash checks.

### Planned regression tests (documented only)

No regression test suite is added in this change. A future suite should cover:

- Accept compatible manifest and pool revisions such as `1.0.1` and `1.1.0`, with matching registrations and updated hashes.
- Reject unsupported major versions, malformed versions, and pool/registration version mismatches.
- Reject duplicate JSON keys, duplicate component IDs or normalized names, invalid weights, and incompatible component order.
- Reject stale hashes, false counts or capacities, missing pool references, and invalid subgroup mappings.
- Preserve validation of the complete valid inventory, including intentionally smaller pools and patronymic conventions.

### Running the checks

From the worktree root, using PowerShell 7 and Python 3:

```powershell
./src/ASL/tools/name-pools/validate.ps1
```

For cross-file integrity checks alone:

```text
python src/ASL/tools/name-pools/validate.py
```

Checks include schema conformance, duplicate JSON keys, ID and normalized text uniqueness, positive weights, component roles, ordering, subgroup membership, valid combination capacities, pool and force references, actual counts, target-completion claims, and manifest SHA-256 values.

The validator does not verify historical popularity, linguistic correctness, or a person's ethnicity. `reviewStatus` stays `editorial-draft` until a recorded content review supports changing it. The convention sources support structural choices, not every name in these inventories. The American given-name list uses the public SSA 1900-1909 male top-100 table; its surname list and the other pools remain editorial compilations.

Names have stable content-derived entry IDs within each pool. Keep existing IDs stable when reordering or changing weights. A spelling correction may require a new entry ID; do not overwrite a saved person's display name. After editing a pool, update its version as appropriate, actual counts, combination capacities, and manifest registration/hash. Existing cards and saved games retain their concrete names and source version.
