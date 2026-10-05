"""Prepare a research-only historical rail overlay; never infer exact 1939 operation."""
import hashlib, json
from pathlib import Path
import shapefile
from shapely.geometry import shape, mapping
from shapely.ops import transform
ROOT=Path(__file__).resolve().parent
SRC=ROOT/'sources/europe-railways-1920-1940'
def prepare():
    metadata=json.loads((SRC/'metadata.json').read_text(encoding='utf-8-sig'))
    hashes={}
    for f in metadata['files']:
        content=(SRC/f['name']).read_bytes()
        assert hashlib.sha1(content).hexdigest()==f['sha1'],f['name']
        hashes[f['name']]=hashlib.sha256(content).hexdigest()
    reader=shapefile.Reader(str(SRC/'europe_1940'))
    features=[]
    for i,item in enumerate(reader.iterShapeRecords()):
        props=item.record.as_dict()
        # Preserve source classifications; NE/FO are not plotted as active rail.
        if props['TYPE_1940'] not in ('ML','SL'): continue
        geom=shape(item.shape.__geo_interface__)
        local=transform(lambda x,y,z=None:((x-4321000)/1000,(3210000-y)/1000),geom)
        features.append({'type':'Feature','properties':{'sourceRecord':i,**props},'geometry':mapping(local)})
    from rail_display import rank
    hierarchy=rank(features)
    for f in features:
        f["geometry"]=mapping(shape(f["geometry"]).simplify(.5,preserve_topology=True))
    result={'source':'Bárbara Polo Martín, Shapefile of railways in Europe between 1920 and 1940 (2023)',
      'url':'https://doi.org/10.34847/nkl.6296qx69','license':'CC-BY-NC-4.0','gameAdmitted':False,
      'period':[1920,1940],'sourceRecordCount':len(reader),'sourceHashes':hashes,
      'displayFilter':'TYPE_1940 in ML, SL; this is a source classification, not verified September 1939 service',
      'geometry':'Source EPSG:3035 geometry transformed to local km and simplified at 0.5 km; no topology repair or capacity inference',
      'displayHierarchy':hierarchy,'features':features}
    (ROOT/'historical-rail-network.json').write_text(json.dumps(result,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    return result
if __name__=='__main__': print('Prepared',len(prepare()['features']),'historical railway segments')
