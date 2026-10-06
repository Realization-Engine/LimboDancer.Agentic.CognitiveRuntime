"""Pin the small tactical-test reference to the repository card and unit catalog."""
import hashlib,json
from pathlib import Path
ROOT=Path(__file__).resolve().parent

def prepare():
    catalog_path=ROOT.parents[1]/'src/ASL/units/catalog/scenario-a1.catalog.json'
    catalog=json.loads(catalog_path.read_text(encoding='utf-8'))
    card=json.loads((ROOT/'sources/formation-access-test.scenario-card.json').read_text(encoding='utf-8'))
    ids={u['definition'] for side in card['sides'] for group in side['groups'] for u in group['units']}
    definitions=[{k:d[k] for k in ('id','kind','nationality','values')} for d in catalog['definitions'] if d['id'] in ids]
    if len(definitions)!=len(ids) or card['catalog']!=catalog['catalog']+'@'+catalog['version']:
        raise ValueError('Tactical test and catalog are inconsistent')
    payload=dict(catalog=card['catalog'],catalogSha256=hashlib.sha256(catalog_path.read_bytes()).hexdigest(),definitions=definitions,card=card)
    (ROOT/'tactical-reference.js').write_text('const TACTICAL_REFERENCE='+json.dumps(payload,ensure_ascii=True)+';\n',encoding='utf-8',newline='\n')
if __name__=='__main__':prepare()
