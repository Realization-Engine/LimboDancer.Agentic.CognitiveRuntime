"""Provenance and export boundaries for the historical research overlay."""
import json, unittest
from pathlib import Path
import build
ROOT=Path(__file__).resolve().parent
class TransportTests(unittest.TestCase):
    def setUp(self):
        self.source=json.loads((ROOT/"sources/historical-transport-pilot.json").read_text(encoding="utf-8"))
        self.generated=json.loads((ROOT/"historical-transport-pilot.json").read_text(encoding="utf-8"))
    def test_provenance_and_region_coverage(self):
        source_ids={s["id"] for s in self.source["sources"]}
        self.assertEqual(len(source_ids),len(self.source["sources"]))
        ids=[f["properties"]["id"] for f in self.source["features"]]
        self.assertEqual(len(ids),len(set(ids)))
        for r in self.source["regions"]:
            self.assertTrue(r["gaps"])
            self.assertTrue(any(f["properties"]["region"]==r["name"] for f in self.source["features"]))
        for f in self.source["features"]:
            p=f["properties"]
            self.assertIn(p["sourceId"],source_ids)
            self.assertTrue(all(id in source_ids for id in p.get("corroboratingSourceIds",[])))
            self.assertLess(p["existenceByYear"],1939)
            self.assertEqual(p["baselineOperation"],"unverified")
            self.assertFalse(p["gameAdmitted"])
            self.assertEqual(f["geometry"]["coordinates"],[self.source["places"][n] for n in p["places"]])
    def test_projection_and_hash(self):
        self.assertEqual(self.generated["sourceHash"],build.digest(self.source))
        for original,generated in zip(self.source["features"],self.generated["features"]):
            self.assertEqual(original["properties"],generated["properties"])
            for a,b in zip(original["geometry"]["coordinates"],generated["geometry"]["coordinates"]):
                lon,lat=build.unproject(*b)
                self.assertAlmostEqual(a[0],lon,places=5)
                self.assertAlmostEqual(a[1],lat,places=5)
    def test_gameplay_exclusion(self):
        data=json.loads((ROOT/"map.json").read_text(encoding="utf-8"))
        self.assertNotIn("historicalPilot",data)
        self.assertTrue(all(c["historicalTransport"]["roadKm"] is None and c["historicalTransport"]["railKm"] is None for c in data["cells"]))
        self.assertFalse(data["metadata"]["transportBaseline"]["features"])
    def test_southern_dated_connections(self):
        source=json.loads((ROOT/"sources/southern-transport.json").read_text(encoding="utf-8"))
        generated=json.loads((ROOT/"southern-transport.json").read_text(encoding="utf-8"))
        self.assertEqual(generated["sourceHash"],build.digest(source))
        self.assertEqual(len(source["features"]),28)
        ids={s["id"] for s in source["sources"]}
        self.assertEqual(len({f["properties"]["id"] for f in source["features"]}),28)
        for f,g in zip(source["features"],generated["features"]):
            p=f["properties"]
            self.assertIn(p["sourceId"],ids)
            self.assertFalse(p["gameAdmitted"])
            self.assertEqual(p["alignmentStatus"],"schematic")
            self.assertIn(p["displayTier"],[0,1,2])
            self.assertEqual(p,g["properties"])
            for a,b in zip(f["geometry"]["coordinates"],g["geometry"]["coordinates"]):
                lon,lat=build.unproject(*b)
                self.assertAlmostEqual(a[0],lon,places=5)
                self.assertAlmostEqual(a[1],lat,places=5)
        by_id={f["properties"]["id"]:f["properties"] for f in source["features"]}
        self.assertEqual(by_id["na26"]["evidenceYear"],1941)
        self.assertEqual(by_id["na16"]["evidenceYear"],1942)
        self.assertEqual(by_id["na22"]["places"][-1],"Mersa Matruh")
        self.assertNotIn("southernTransport",json.loads((ROOT/"map.json").read_text(encoding="utf-8")))

if __name__=="__main__": unittest.main()
