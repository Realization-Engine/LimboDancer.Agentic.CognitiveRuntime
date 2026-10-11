"use strict";
// A distinct active-operation pointer prevents Normandy from replacing Ardennes.
const NormandyRepository=(()=>{
 const KEY='campaign-atlas:normandy-stlo:v1:',spatial={nodes:{HQ:{name:'Normandy Operation HQ',echelon:'Division',x:160,y:150},M04:{name:'St. Lo Regiment HQ',echelon:'Regiment',x:700,y:220},depot:{name:'Normandy Forward Depot',x:200,y:340}},routes:{M04:[[200,340],[380,300],[510,210],[700,285]]}};
 function repository(storage){let current=null;const M={...NormandyCommand,...NormandySituation};
  const read=id=>CampaignWorld.validate(JSON.parse(storage.getItem(KEY+id)),M);
  function save(w){if(current){const live=read(current.id);if(live.revision!==current.revision)throw Error('Normandy changed in another tab. Reload before editing.');}const next=CampaignWorld.validate(w,M);next.revision++;storage.setItem(KEY+next.id,JSON.stringify(next));storage.setItem(KEY+'active',next.id);current=next;return current;}
  return {get:()=>current,load(){const id=storage.getItem(KEY+'active');return current=id?read(id):null;},save,
   start(profile,staff){const op=NormandyCommand.initial(crypto.randomUUID(),profile,staff),seed=Array.from(crypto.getRandomValues(new Uint8Array(16))).map(n=>n.toString(16).padStart(2,'0')).join('');const w=CampaignWorld.create(op,seed,spatial);w.title='Normandy inland operations';w.campaignId='normandy-inland-june-1944';w.operationDate='1944-06-29';w.atlasOrigin=[-420,260];return save(w);},
   execute(c,source){if(!current)throw Error('Start the Normandy operation first.');const w=JSON.parse(JSON.stringify(current));w.operation=NormandyCommand.execute(w.operation,c);const next=c.type==='commit'?CampaignWorld.bindMission(w,'M04',source,M):w;return save(next);}
  };
 }
 return {repository,KEY,spatial};
})();
if(typeof module!=='undefined')module.exports=NormandyRepository;
