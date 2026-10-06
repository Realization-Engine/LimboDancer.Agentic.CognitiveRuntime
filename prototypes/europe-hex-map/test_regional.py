import json,math,unittest
from pathlib import Path
from shapely.geometry import shape,Polygon
class RegionalReferenceTests(unittest.TestCase):
 def test_geometry_and_evidence(self):
  d=json.loads(Path('regional-campaign.json').read_text());self.assertGreater(len(d['cells']),20)
  footprint=shape(d['hexFootprint']);self.assertTrue(footprint.is_valid)
  self.assertEqual(len({c['id'] for c in d['cells']}),len(d['cells']))
  for c in d['cells']:
   _,q,r=c['id'].split(':');self.assertAlmostEqual(c['x'],math.sqrt(3)*7.5*(int(q)+int(r)/2));self.assertAlmostEqual(c['y'],11.25*int(r));self.assertLess(Polygon(c['vertices']).difference(footprint.buffer(.00001)).area,.00001)
  sources={s['id'] for s in d['sources']};forces={f['id'] for f in d['forces']}
  for m in d['missions']:self.assertIn(m['sourceId'],sources);self.assertIn(m['issuer'],forces);self.assertIn(m['recipient'],forces)
  self.assertTrue(all(f['strength'] is None and f['position'] is None for f in d['forces']))
  self.assertEqual(d['referenceDate'],'1944-06-08')
if __name__=='__main__':unittest.main()
