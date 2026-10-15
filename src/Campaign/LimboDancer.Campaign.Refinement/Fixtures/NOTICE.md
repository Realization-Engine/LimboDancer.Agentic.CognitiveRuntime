# Fixture provenance

SharedBoardMetadata.xml is from the local vasl-developers/vasl checkout at commit 33324f9adb3b9b97dd93700685103b6940dc9c3c, path dist/boardData/SharedBoardMetadata.xml. Its text is normalized to LF with one trailing newline. VASL is licensed under LGPL 2.1; the accompanying VASL-LICENSE.txt is copied verbatim. The pilot uses the existing ASL parser, not an independently invented terrain catalog.

The Goodwood source remains at prototypes/europe-hex-map/sources/situations/panzer-leader-05.json. goodwood.json pins its UTF-8 content after CRLF-to-LF normalization, the catalog SHA-256, the primary crop, and all numeric generation parameters. Seeds are 128-bit lowercase hexadecimal values. The source hash covers scenario data as well as terrain, intentionally requiring review when any source content changes.

Classification: formation open/woods/water polygons and traced road/stream centerlines are preserved as source interpretations. Town hexes constrain generated building parcels. Decorative roofs and woodland symbols are omitted; bridge polygons are explicitly promoted to crossing constraints with assumed stone material. Nothing here establishes historical building footprints or ASL gameplay admission. Nonzero source elevations and unsupported terrain in a requested crop are rejected.

The primary crop is columns 52 through 84 and nominal rows 90 through 109 on the continuous lattice. BoardGeometry adds boundary half hexes in staggered columns. The overlap fixture starts at (60,92). The A/C seam is y=5,000,000 mm; source board A (u=1,v) and C (u=0,v), both rotated 90 degrees, project identically. A crop at (52,116) is a negative admission fixture because source hills are unsupported in this pilot.
