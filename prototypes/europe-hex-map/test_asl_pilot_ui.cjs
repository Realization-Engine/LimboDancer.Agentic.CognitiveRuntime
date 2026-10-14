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
function harness(initial,{failSave=false,formation=null}={}){
 const body=new Element('body'),writes=[],requests=[];let saved=structuredClone(initial);
 const document={body,activeElement:null,createElement:t=>new Element(t),createElementNS:(_,t)=>new Element(t),createTextNode:t=>new Element('text',t)};
 const sandbox={window:{},document,URLSearchParams,location:{search:''},performance,console,setTimeout,clearTimeout,
 CampaignSupplyDeployment:{refs:(_,id)=>[{boardId:'A',hexId:id.startsWith('uk-rifle')?'X-6':'V-7'}]},
 DOMParser:class {parseFromString(){return {documentElement:new Element('svg')};}},
 fetch:async(url,options)=>{requests.push({url,options});let value={};
  if(url.endsWith('/generate'))value={id:'b'.repeat(32)};
  if(url.endsWith('/generate/'+'b'.repeat(32)))value={status:'ready'};
  if(url.endsWith('/open'))value={session:'session',bundle,setup,roster:[{id:'british-1',side:'Allied',label:'British squad'}],cells:[{hex:'C3',terrain:'Open Ground',parent:'A/X-6',admitted:true,reason:'Admitted',polygon:[],x:10,y:10}],width:1800,height:1290,svg:'<svg/>'};
  if(url.endsWith('/edit'))value={...setup,revision:1,placements:[{unitId:'british-1',hex:'C3'}]};
  return {ok:true,status:200,text:async()=>JSON.stringify(value)};
 }};
 vm.runInNewContext(source,sandbox);
 const storage={campaignId:'fixture',seed:'0'.repeat(32),load:()=>saved,save:next=>{if(failSave)throw Error('disk full');writes.push(structuredClone(next));saved=next;}};
 const find=(tag,text)=>{const walk=n=>n.tag===tag&&(!text||n.textContent===text)?n:n.children.map(walk).find(Boolean);return walk(body);};
 return {open:()=>sandbox.window.AslPilot.open({instances:[{id:'uk-rifle-02',counterTypeId:'uk-rifle'}],counters:[{id:'uk-rifle',side:'Allied'}]}, {placements:{}},storage,formation),writes,requests,find};
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
 console.log('PASS read-only pilot resume, first setup persistence, deployment save and failed-write preservation');
})().catch(e=>{console.error(e);process.exitCode=1;});
