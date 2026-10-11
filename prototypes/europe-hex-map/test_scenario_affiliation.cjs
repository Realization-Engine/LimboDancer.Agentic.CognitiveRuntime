const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const ctx=vm.createContext({window:{}});vm.runInContext(fs.readFileSync('tactical/scenario-affiliation.js','utf8'),ctx);
const A=ctx.window.ScenarioAffiliation;
function data(n){return JSON.parse(fs.readFileSync(`sources/situations/panzer-leader-${String(n).padStart(2,'0')}.json`));}
for(const n of [4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20]){
 const d=data(n),before=JSON.stringify(d),ss=d.instances.filter(i=>A.resolve(d,i).affiliation==='SS');
 assert.equal(JSON.stringify(d),before);for(const i of d.instances)assert.deepEqual(A.resolve(d,i),A.resolve(d,{parentId:i.id}));
 if([12,13,16,17,18,19].includes(n))assert.equal(ss.length,0);
 if(n===15){for(const i of d.instances.filter(i=>i.id.startsWith('de-')))assert.equal(A.resolve(d,i).affiliation,i.setupGroup==='A'?'SS':'Heer');assert.equal(ss.filter(i=>['de-rifle','de-smg','de-engineer'].includes(i.counterTypeId)).length,10);}
 if(n===5){assert.equal(ss.filter(i=>i.counterTypeId==='de-rifle').length,8);assert.equal(A.group(d,'de-rifle'),undefined);}
 if(n===8)assert.match(A.resolve(d,'de-rifle-01').note,/conflicts/);
 console.log(n,'SS formations:',ss.length);
}
console.log('Scenario affiliations, inherited identities, mixed groups and source immutability passed.');
