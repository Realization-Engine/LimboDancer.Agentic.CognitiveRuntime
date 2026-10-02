# Catalogs retained for saved games

These snapshots preserve the catalog identity and definitions used by existing game records. Studio replay loads them through `UnitCatalogs.ReplayNames`; current unit selection still uses `UnitCatalogs.Names`.

Do not update an archived catalog to match current definitions. Add a separate snapshot when retaining another version.

`scenario-a1-1.12.0.catalog.json` is the exact Git blob `50c8038a4a4430d006c5ca91015c6f626d50d050`, recovered from `b0fded2^:src/ASL/units/catalog/scenario-a1.catalog.json`, before the catalog advanced to 1.13.0. It restores replay of games such as `guards-map-tour-01` without editing their event records.
