# ASL units: vocabulary, style sheets, palettes, and examples

Data for the [ASL Unit Display Design](../docs/ASL%20Unit%20Display%20Design.md). All of it is original LimboDancer content: vocabulary, styles, and colors, with no rulebook text and no counter artwork (ASL-UNIT-072).

| Folder | Content |
|---|---|
| `vocabulary/` | Vocabulary packs (`{pack}.vocab.json`). `asl.vocab.json` covers Personnel, support weapons, Guns, vehicles, and entities that are not units (section 4). |
| `styles/` | Unit style sheets (`{name}.uss`): `asl-classic` follows the printed counter conventions, `asl-digital` a digital form. |
| `palettes/` | Side palette sets (`{set}.palette.json`): `asl-customary` and `limbodancer`. |
| `catalog/` | Definition catalogs (`{name}.catalog.json`) and the Scenario A1 catalog manifest, per the [Scenario A1 Catalog Design](../docs/ASL%20Scenario%20A1%20Catalog%20Design.md). `scenario-a1.synthetic.catalog.json` holds illustrative values for tests, never counter data; `scenario-a1.catalog.json` is built from the counter transcription, a draft until the transcription is reviewed. |
| `examples/` | Synthetic unit documents and placement sets. Their values are illustrative, not catalog entries, and they are never game state. |

The Unit Lab in Map Studio saves documents, sheets, and placement sets under the configured boards folder, in `units/`.
