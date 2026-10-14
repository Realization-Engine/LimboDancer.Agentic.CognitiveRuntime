# Goodwood vehicle game defaults

Catalog 1.17.0 adds twelve explicitly manufactured MFG vehicle profiles. These values are plausible design choices for campaign setup, not verified printed counters or historical claims. Existing profiles are unchanged. No movement or combat rules are added.

| Profile | MP | Front/side armor | Main armament | Traverse | ROF | Open top | BMG/CMG/AAMG | Passenger capacity |
| --- | ---: | --- | --- | --- | ---: | --- | --- | ---: |
| Sexton | 14 | 3/1 | 88* | nt | 1 | yes | 0/0/2 | 0 |
| Achilles | 16 | 4/2 | 76LL | st | 2 | yes | 0/0/4 | 0 |
| Cromwell | 24 | 8/4 | 75 | t | 1 | no | 3/5/0 | 0 |
| Sherman 75 | 15 | 8/4 | 75 | t | 1 | no | 3/5/0 | 0 |
| Sherman Firefly | 14 | 8/4 | 76LL | st | 2 | no | 0/5/0 | 0 |
| Bren carrier | 18 | 1/1 | MG | not-printed | 1 | yes | 2/0/0 | 5 |
| Wespe | 14 | 2/1 | 105* | nt | 1 | yes | 0/0/2 | 0 |
| Maultier rocket launcher | 12 | 1/1 | 150* | nt | 1 | yes | 0/0/0 | 0 |
| Hummel | 14 | 2/1 | 150* | nt | 1 | yes | 0/0/2 | 0 |
| Panzer IV H | 13 | 8/3 | 75L | t | 2 | no | 3/5/0 | 0 |
| Panther | 15 | 18/6 | 75LL | st | 2 | no | 3/5/0 | 0 |
| Tiger | 11 | 11/8 | 88L | st | 2 | no | 3/5/0 | 0 |

All have B12, no special ammunition or secondary gun, and no inferred mechanical-unreliability or special movement traits. Crew survival is 5 for open-topped designs and 6 otherwise. Zero MG/capacity entries in the table mean absent, not a zero-firepower weapon. Maultier and Hummel have no AP in this default set. Maultier is deliberately represented as a lightly armored, open-topped half-tracked rocket-launcher vehicle; this is a game-default interpretation of the source label, not an identification claim. Rocket salvo/reload behavior remains limited by the existing engine.

The design differentiates the faster Cromwell, heavier Tiger and stronger Panther frontal armor. Firefly trades the Sherman 75 profile for a longer gun and slower traverse. Artillery vehicles retain non-turreted mounts and lighter armor. Bren carriers retain an integrated MG and capacity rather than receiving free infantry counters. Armor and weapon values are not copied from Panzer Leader attack/defense factors.

Formation conversion must retain three Sherman 75 vehicles and one Firefly per Sherman formation, and preserve the different source vehicle counts for the German W/SS labels. Integrated vehicle crews and MGs are not extra dismounted counters. No passengers are automatically created or assigned by these definitions.

Validation: all 184 existing catalog definitions remain byte-equivalent as parsed records. Twenty source-builder tests, 89 catalog/vehicle tests, 16 ASL Studio inspection tests and four catalog-integrity tests pass in Release.
