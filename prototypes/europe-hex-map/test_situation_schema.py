import copy,json,unittest
from pathlib import Path
from jsonschema import ValidationError
from build_situation import validate,validate_registry,ROOT
class SituationSchemaTests(unittest.TestCase):
 def setUp(self):self.data=json.loads((ROOT/'sources/situations/panzer-leader-04.json').read_text(encoding='utf-8'))
 def test_campaign_scoped_numbering(self):
  registry=json.loads((ROOT/'sources/campaign-registry.json').read_text(encoding='utf-8'))
  validate_registry(registry,self.data)
  other=copy.deepcopy(registry['campaigns'][0]);other['id']='second-campaign';other['situations']=[{**other['situations'][0],'id':'second-campaign:another-situation'}]
  registry['campaigns'].append(other);validate_registry(registry,self.data) # 01 is valid in another campaign.
  duplicate=copy.deepcopy(registry['campaigns'][0]['situations'][0]);duplicate['id']='duplicate-number'
  registry['campaigns'][0]['situations'].append(duplicate)
  with self.assertRaisesRegex(ValueError,'Duplicate Situation number'):validate_registry(registry,self.data)
 def test_campaign_registry_identity(self):
  registry=json.loads((ROOT/'sources/campaign-registry.json').read_text(encoding='utf-8'))
  registry['campaigns'][0]['nextSituationNumber']=1
  with self.assertRaisesRegex(ValueError,'must not reuse'):validate_registry(registry,self.data)
 def test_reference(self):validate(self.data)
 def test_quantity(self):
  self.data['counters'][0]['quantity']+=1
  with self.assertRaisesRegex(ValueError,'quantity'):validate(self.data)
 def test_unknown_type(self):
  self.data['instances'][0]['counterTypeId']='missing'
  with self.assertRaisesRegex(ValueError,'counter type'):validate(self.data)
 def test_false_admission(self):
  self.data['admission']['status']='engine-ready'
  with self.assertRaisesRegex(ValueError,'complete board'):validate(self.data)
 def test_unknown_objective_zone(self):
  self.data['rules']['objectives'][0]['predicate']['children'][0]['zoneId']='missing'
  with self.assertRaisesRegex(ValueError,'objective zone'):validate(self.data)
 def test_hex_terrain_and_topology(self):
  b=self.data['boards'][0];b['grid']['coverage']='partial'
  ev={'status':'authored','sourceIds':[],'note':'Synthetic validator fixture, not St. Lo terrain'}
  h={'id':'A1','q':0,'r':0,'label':'A1','terrain':{'base':'woods','features':[],'elevationMeters':None,'elevationLevel':0,'evidence':ev},'edges':[{'direction':i,'neighbor':None,'barrier':'none','crossing':'none','routeIds':[],'sourceObstacle':'none','evidence':ev} for i in range(6)],'placeIds':[],'playable':True,'imageCenter':None,'imagePolygon':[{'u':0,'v':0},{'u':1,'v':0},{'u':0,'v':1}],'boundaryHalfHex':False,'setupAllowed':True}
  b['hexes']=[h];self.data['mapModel']['routes']=[route for route in self.data['mapModel']['routes'] if route['geometry']['boardId']!='A'];self.data['mapModel']['places']=[dict(p,hexes=[h for h in p['hexes'] if h['boardId']!='A']) for p in self.data['mapModel']['places']];self.data['mapModel']['zones']=[dict(z,hexes=[]) for z in self.data['mapModel']['zones']];validate(self.data)
  h['terrain']['base']='invented'
  with self.assertRaises(ValidationError):validate(self.data)
  h['terrain']['base']='woods';h['edges'][0]['neighbor']={'boardId':'A','hexId':'missing'}
  with self.assertRaisesRegex(ValueError,'hex reference'):validate(self.data)
 def test_reject_extra_fields(self):
  self.data['unrecognizedField']=True
  with self.assertRaises(ValidationError):validate(self.data)
 def test_transcribed_grid(self):
  for b in self.data['boards']:
   self.assertEqual(len(b['hexes']),346)
   self.assertEqual(sum(h['boundaryHalfHex'] for h in b['hexes']),52)
   self.assertEqual(len({(h['q'],h['r']) for h in b['hexes']}),346)
   for h in b['hexes']:
    self.assertEqual(h['setupAllowed'],not h['boundaryHalfHex'])
  a=self.data['boards'][0]
  self.assertEqual(next(h for h in a['hexes'] if h['id']=='Q-10')['placeIds'],['kuhn'])
  self.assertTrue(any(e['sourceObstacle']=='woods' for h in a['hexes'] for e in h['edges']))
 def test_route_geometry(self):
  routes=self.data['mapModel']['routes']
  self.assertGreater(len(routes),20)
  for route in routes:
   self.assertGreater(len(route['geometry']['coordinates']),2)
  routes[0]['geometry']['coordinates'][0][0]=1.5
  with self.assertRaises(ValidationError):validate(self.data)
 def test_route_frame(self):
  self.data['mapModel']['routes'][0]['geometry']['boardId']='missing'
  with self.assertRaisesRegex(ValueError,'geometry board'):validate(self.data)
if __name__=='__main__'  :unittest.main()
