import json,unittest,hashlib
from pathlib import Path
from shapely.geometry import shape,Point
ROOT=Path(__file__).resolve().parent
class TheaterTests(unittest.TestCase):
 def test_every_theater_has_complete_workspace(self):
  data=json.loads((ROOT/'theater-workspaces.json').read_text(encoding='utf-8'))
  defs=json.loads((ROOT/'theaters.json').read_text(encoding='utf-8'))['theaters']
  self.assertEqual(set(data),{t['id'] for t in defs})
  coordinates={}
  for id,p in data.items():
   self.assertTrue(p['cells'] and p['towns'] and p['rivers'] and p['sites'],id)
   self.assertAlmostEqual(p['gridAcrossFlatsKm'],25.980762,places=5)
   footprint=shape(p['hexFootprint']);self.assertTrue(footprint.is_valid,id);buffered=footprint.buffer(.001)
   for c in p['cells']:
    self.assertTrue(c['id'].startswith(id+':26km:'))
    self.assertTrue(buffered.covers(Point(c['x'],c['y'])))
    if c['coordinateId'] in coordinates:self.assertEqual(coordinates[c['coordinateId']],c['vertices'])
    coordinates[c['coordinateId']]=c['vertices']
   for site in p['sites']:
    self.assertTrue(site['source'].startswith('https://'))
    self.assertTrue(site['period'])
  for s in next(iter(data.values()))['sourceManifest']['sources']:
   self.assertEqual(hashlib.sha256((ROOT/'sources'/s['file']).read_bytes()).hexdigest(),s['sha256'])
if __name__=='__main__':unittest.main()
