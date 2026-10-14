'use strict';
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('tactical/asl-pilot.js', 'utf8');
class Element {
 constructor(tag,text=''){this.tag=tag;this.textContent=text;this.children=[];this.attributes={};this.hidden=false;}
 append(...nodes){this.children.push(...nodes);}
 replaceChildren(...nodes){this.children=nodes;}
 setAttribute(k,v){this.attributes[k]=v;}
 get lastChild(){return this.children.at(-1);}
 get options(){return this.children.flatMap(c=>c.tag==='option'?[c]:c.options);}
 showModal(){} close(){this.onclose?.();} remove(){} focus(){} addEventListener(){}
 dataset={};
}
const context={campaignId:'fixture',formationSignature:JSON.stringify({placements:[],supply:null}),alliedParents:['A/X-6'],germanParents:['A/V-7']};
const setup={complete:false,placements:[],revision:0};
const bundle={packageVersion:'accepted'};
function harness(initial,{failSave=false,formation=null,failGeneration=false,cancelGeneration=false,counterType='uk-rifle',kind='rifle',unitCatalog=undefined}={}){
 const body=new Element('body'),writes=[],requests=[];let saved=structuredClone(initial);
 const document={body,activeElement:null,createElement:t=>new Element(t),createElementNS:(_,t)=>new Element(t),createTextNode:t=>new Element('text',t)};
 const sandbox={window:{},document,URLSearchParams,location:{search:''},performance,console,setTimeout,clearTimeout,
 CampaignSupplyDeployment:{refs:(_,id)=>[{boardId:'A',hexId:id.startsWith('uk-rifle')?'X-6':'V-7'}]},
 DOMParser:class {parseFromString(){return {documentElement:new Element('svg')};}},
 fetch:async(url,options)=>{requests.push({url,options});let value={};
  if(url.endsWith('/generate')){value={id:'b'.repeat(32)};if(cancelGeneration)await find('button','Return to formation map').onclick();}
  if(url.endsWith('/generate/'+'b'.repeat(32)))value=failGeneration?{status:'failed',error:'Unsupported source terrain'}:{status:'ready'};
  if(url.endsWith('/open'))value={session:'session',bundle,setup:{...setup,...(unitCatalog?{unitCatalog}:{})},roster:[{id:'british-1',side:'Allied',label:'British squad',kind,symbol:kind==='mortar'?'M':'E'}],cells:[{hex:'C3',terrain:'Open Ground',parent:'A/X-6',admitted:true,reason:'Admitted',polygon:[],x:10,y:10}],width:1800,height:1290,svg:'<svg/>'};
  if(url.endsWith('/edit'))value={...setup,revision:1,placements:[{unitId:'british-1',hex:'C3'}]};
  return {ok:true,status:200,text:async()=>JSON.stringify(value)};
 }};
 vm.runInNewContext(source,sandbox);
 const storage={campaignId:'fixture',seed:'0'.repeat(32),load:()=>saved,save:next=>{if(failSave)throw Error('disk full');writes.push(structuredClone(next));saved=next;}};
 const find=(tag,text)=>{const walk=n=>n.tag===tag&&(!text||n.textContent===text)?n:[...n.children].reverse().map(walk).find(Boolean);return walk(body);};
 return {open:()=>sandbox.window.AslPilot.open({instances:[{id:formation?.unitId||'uk-rifle-02',counterTypeId:counterType}],counters:[{id:counterType,side:counterType.startsWith('de-')?'German':'Allied'}]}, {placements:{}},storage,formation),writes,requests,find};
}
(async()=>{
 const accepted={version:1,seed:'0'.repeat(32),context,bundle,setup};
 const resume=harness(accepted);await resume.open();
 assert(resume.find('select'),'Saved pilot opens successfully');
 assert.equal(resume.writes.length,0,'Read-only resume must not rewrite a campaign');
 assert.equal(resume.requests.length,1,'Resume should directly validate and open, without a redundant health round trip');
 await resume.find('button','Deploy pilot roster').onclick();
 assert.equal(resume.writes.length,1,'A deployment edit must still be saved');
 assert.equal(resume.writes[0].setup.placements.length,1);
 const catalogUpgrade=harness(accepted,{unitCatalog:'asl-scenario-a1@1.14.0'});await catalogUpgrade.open();
 assert.equal(catalogUpgrade.writes.length,1,'Catalog provenance upgrade must be saved even with unchanged context');
 assert.equal(catalogUpgrade.writes[0].setup.unitCatalog,'asl-scenario-a1@1.14.0');
 const catalogResume=harness(catalogUpgrade.writes[0],{unitCatalog:'asl-scenario-a1@1.14.0'});await catalogResume.open();
 assert.equal(catalogResume.writes.length,0,'Unchanged catalog provenance must not rewrite saves');
 const failedCatalog=harness(accepted,{unitCatalog:'asl-scenario-a1@1.14.0',failSave:true});await failedCatalog.open();
 assert.equal(failedCatalog.writes.length,0,'Failed provenance write must preserve the original save');
 const firstSetup=harness({...accepted,setup:undefined});await firstSetup.open();
 assert.equal(firstSetup.writes.length,1,'Creating setup for accepted terrain must be saved');
 const blocked=harness(accepted,{failSave:true});await blocked.open();
 await blocked.find('button','Deploy pilot roster').onclick();
 assert.equal(blocked.writes.length,0);
 assert(blocked.find('p','disk full'),'Failed persistence must report the error');
 assert(blocked.find('span','0/1 deployed'),'Failed persistence must retain original visible deployment');
 const formation={unitId:'uk-rifle-02',anchor:'A/X-6'};
 const selected=harness(undefined,{formation});await selected.open();
 assert.equal(JSON.parse(selected.requests[0].options.body).formation.unitId,'uk-rifle-02');
 assert(selected.requests.some(r=>r.url.endsWith('/generate/'+'b'.repeat(32))),'Poll returned formation job ID, not campaign seed');
 const openRequest=JSON.parse(selected.requests.find(r=>r.url.endsWith('/open')).options.body);
 assert.equal(openRequest.context.formation.anchor,'A/X-6');
 assert.deepEqual(openRequest.context.germanParents,[]);
 assert.equal(selected.writes.at(-1).context.formation.unitId,'uk-rifle-02');
 const changedContext={...context,formationSignature:'unrelated change',formation,germanParents:[]};
 const scoped=harness({...accepted,context:changedContext},{formation});await scoped.open();
 assert(scoped.find('select'),'Unrelated context changes reach server validation');
 assert.deepEqual(JSON.parse(scoped.requests[0].options.body).savedContext,changedContext);
 const moved={...accepted,context:{...changedContext,formation:{...formation,anchor:'A/W-6'}}};
 const replacement=harness(moved,{formation});await replacement.open();
 assert.equal(replacement.writes.length,0,'Opening moved formation retains accepted save');
 assert(replacement.find('button','Rebuild local map'));
 await replacement.find('button','Rebuild local map').onclick();
 assert.equal(replacement.writes.length,1,'Replacement is published only after generation and open validation');
 assert.equal(replacement.writes[0].context.formation.anchor,'A/X-6');
 assert.equal(replacement.writes[0].setup.placements.length,0,'Replacement starts fresh setup');
 for(const options of [{failGeneration:true},{failSave:true},{cancelGeneration:true}]){
  const failed=harness(moved,{formation,...options});await failed.open();
  await failed.find('button','Rebuild local map').onclick();
  assert.equal(failed.writes.length,0,'Failed or cancelled replacement retains accepted save');
 }
 for(const [counterType,kind] of [['uk-engineer','engineer'],['de-81mm','mortar']]){
  const typed=harness(undefined,{formation:{unitId:counterType+'-01',anchor:'A/X-6'},counterType,kind});await typed.open();
  assert(typed.find('select'),'Supported composition must reach the setup UI');
  assert(typed.find('button','Place selected unit'));
  assert.equal(typed.writes.at(-1).context.formation.unitId,counterType+'-01');
 }
 console.log('PASS scoped resume, rebuild success/failure/cancellation, read-only pilot resume, first setup persistence, deployment save and failed-write preservation');
})().catch(e=>{console.error(e);process.exitCode=1;});
