# Goodwood gun game defaults

Catalog 1.16.0 adds six MFG definitions under the user's authorization to manufacture plausible data. These are designed setup values, not transcribed ASL counter values or historical research. Existing catalog definitions remain unchanged. The source record identifies Codex as the manufacturer; its reviewer field records delegated authority, not independent verification.

| Definition | Nationality | Category | Caliber | ROF | IFE | Maximum range | Manhandling | BPV |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| campaign-british-25pdr | British | ART | 88 | 2 | none | 300 | 10 | 40 |
| campaign-german-75mm-at | German | AT | 75L | 2 | none | 160 | 10 | 50 |
| campaign-german-88mm-at | German | AT | 88L | 2 | none | 200 | 8 | 65 |
| campaign-german-20mm-aa | German | AA | 20L | 3 | 4 | 100 | 12 | 25 |
| campaign-german-quad20-aa | German | AA | 20L | 3 | 8 | 100 | 9 | 40 |
| campaign-german-nebelwerfer | German | ART | 150* | 1 | none | 175 | 8 | 45 |

Range is in ASL hexes. The 25-pounder's nominal 87.6 mm bore is represented as 88 in the integer-caliber vocabulary. Heavier mounts have lower manhandling numbers; the quadruple mount increases IFE rather than bore diameter. The Nebelwerfer is explicitly named as a rocket launcher and uses the existing artillery setup category. This is not a claim that rocket salvo or reload rules are implemented.

All profiles use B12/R2/X6, dates 44-45, no special ammunition, no inferred Animal Pack capability, and no hidden SSR. The launcher has no AP. QSU and 360-degree mount choices are recorded individually in the source CSV. Crew identities and allotments belong to campaign conversion, not extra implicit catalog units. Preserve separate operators when integrating these profiles.

Validation: catalog source reproduction, vocabulary and gun tests, four dependent catalog-integrity pins, and ASL Studio unit inspection. Catalog publication does not itself establish complete formation conversion or combat support.
