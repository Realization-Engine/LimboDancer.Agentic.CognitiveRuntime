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
if __name__=="__main__": unittest.main()
