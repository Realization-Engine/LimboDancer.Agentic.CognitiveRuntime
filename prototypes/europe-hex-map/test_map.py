import hashlib, json, math, unittest
from pathlib import Path
import build
from shapely.geometry import Point
class GeographyTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.data=json.loads((build.ROOT/"map.json").read_text(encoding="utf-8"))
        _,_,cls.land,cls.lakes,_=build.load()
    def test_source_and_base_hashes(self):
        data=json.loads(json.dumps(self.data))
        stored=data["metadata"].pop("baseHash")
        self.assertEqual(stored,build.digest(data))
    def test_unique_hexes_and_real_coordinates(self):
        cells=self.data["cells"]
        self.assertEqual(len(cells),len({c["id"] for c in cells}))
        for c in cells:
            self.assertTrue(0<=c["landFraction"]<=1)
            self.assertTrue(0<=c["lakeFraction"]<=1)
            x,y=build.project(c["lon"],c["lat"])
            self.assertLess(math.hypot(x-c["x"],y-c["y"]),.01)
    def test_request_order_and_subset_invariance(self):
        keys=[(0,0),(1,0),(-15,2),(3,-12),(9,5)]
        a={k:build.cell(*k,self.land,self.lakes) for k in keys}
        b={k:build.cell(*k,self.land,self.lakes) for k in reversed(keys)}
        self.assertEqual(a,b)
        self.assertEqual(a[keys[2]],build.cell(*keys[2],self.land,self.lakes))
    def test_neighbor_edges_match(self):
        a=build.polygon(0,0); b=build.polygon(1,0)
        self.assertLess(a.intersection(b).area,1e-6)
        self.assertAlmostEqual(a.distance(b),0,places=8)
        # Opposing edge endpoints agree to numeric precision.
        va=list(a.exterior.coords)[:-1]; vb=list(b.exterior.coords)[:-1]
        self.assertEqual(sum(any(math.dist(x,y)<1e-8 for y in vb) for x in va),2)
    def test_known_land_and_sea(self):
        self.assertTrue(self.land.covers(Point(*build.project(2.35,48.86))))
        self.assertFalse(self.land.covers(Point(*build.project(-20,48))))
    def test_geographic_export(self):
        geo=json.loads((build.ROOT/"hexes.geojson").read_text())
        self.assertEqual(len(geo["features"]),len(self.data["cells"]))
        self.assertEqual(geo["features"][0]["geometry"]["coordinates"][0][0],geo["features"][0]["geometry"]["coordinates"][0][-1])
    def test_reference_overlays_and_scope(self):
        layers={f["properties"]["layer"] for f in self.data["features"]}
        self.assertTrue({"mountains","forest"}<=layers)
        self.assertIn("Alps",{t["name"] for t in self.data["terrainLabels"]})
        self.assertIn("WWII roads and railways",self.data["metadata"]["unmodeled"])
        for c in self.data["cells"]:
            self.assertTrue(0<=c["forestReferenceFraction"]<=1)
            self.assertIsNone(c["historicalTransport"]["roadKm"])
            self.assertIsNone(c["historicalTransport"]["railKm"])
        self.assertFalse({"roads","railways"} & layers)
        self.assertEqual(self.data["metadata"]["transportBaseline"]["baselineDate"],"1939-09-01")
        for f in self.data["features"]:
            if f["properties"]["layer"] in ("roads","railways"):
                self.assertIn("not WWII-validated",f["properties"]["status"])
if __name__=="__main__": unittest.main()
