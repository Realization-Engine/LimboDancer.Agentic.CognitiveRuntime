const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const ctx=vm.createContext({window:{}});
for(const name of ['asl-counter-definitions','organization-profiles','library-profiles','source-composition','scenario-affiliation','tactical-estimates','scenario-troop-quality']){vm.runInContext(fs.readFileSync('tactical/'+name+'.js','utf8'),ctx);Object.assign(ctx,ctx.window);}
let upgraded=0;
for(let n=4;n<=20;n++){
 const d=JSON.parse(fs.readFileSync('sources/situations/panzer-leader-'+String(n).padStart(2,'0')+'.json')),before=JSON.stringify(d);let count=0;
 for(const i of d.instances){
  const u={id:i.id+'/squad-1',parentId:i.id,definitionId:i.counterTypeId,hex:{q:3,r:4}},saved=JSON.stringify(u),base=ctx.SquadTactical.definition(i.counterTypeId,u),def=ctx.ScenarioTroopQuality.definition(d,i.counterTypeId,u);
  if(!base){assert.equal(def,undefined);continue;}
  const expected=[9,10,14].includes(n)&&i.counterTypeId==='de-rifle';
  if(expected){assert.equal(def.values.morale,8);assert.equal(def.values.firepower,4);assert.equal(def.values.range,6);assert.equal(def.values['smoke-exponent'],2);assert.equal(def.quality.applied,true);count++;}
  else assert.deepEqual(def.values,base.values,'Unexpected upgrade '+n+' '+i.id);
  assert.equal(JSON.stringify(u),saved);
 }
 assert.equal(JSON.stringify(d),before);
 assert.equal(count,({9:9,10:12,14:9})[n]||0);upgraded+=count;console.log('Situation',n,'upgraded rifle parents:',count);
}
const catalog=JSON.parse(fs.readFileSync('../../src/ASL/units/catalog/scenario-a1.catalog.json','utf8'));
function find(x){if(!x||typeof x!=='object')return null;if(x.id==='attacker-elite-squad')return x;for(const v of Object.values(x)){const hit=find(v);if(hit)return hit;}return null;}
const elite=find(catalog);assert.ok(elite);
for(const [k,v] of Object.entries({firepower:4,range:6,morale:8,class:'elite','smoke-exponent':2}))assert.equal(elite.values.find(x=>x.face==='front'&&x.attribute===k).value,v);
assert.equal(upgraded,30);console.log('All 17 Situations checked; 30 rifle parents upgraded. Catalog parity and source/save immutability passed.');
