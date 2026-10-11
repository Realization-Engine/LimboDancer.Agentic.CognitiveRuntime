"use strict";
// Campaign adaptations are copies; the archived St. Lo source remains unchanged.
const NormandySituation=(()=>{
 function variant(s,id,source){if(id!=='M04'||source.situation.number!==4||!s.missions.M04.choice)throw Error('Commit the St. Lo mission first.');
  const d=JSON.parse(JSON.stringify(source)),m=s.missions.M04,changes=[];d.id=s.id+'-st-lo';delete d.parentMapLocation;
  d.situation.title='St. Lo [Campaign '+m.choice+']';
  if(m.choice==='baseline'){d.instances=d.instances.filter(i=>i.id!=='us-57mm-02');changes.push('One 57 mm gun withheld pending ammunition.');}
  else changes.push('Full source gun roster supplied with ammunition.');
  if(m.choice==='reinforced'){const template=d.instances.find(i=>i.counterTypeId==='us-m4-75');for(let n=1;n<=2;n++)d.instances.push({...template,id:'reserve-stlo-m4-'+n,formationId:null});changes.push('Two authored reserve Sherman formation counters attached after fuel delivery.');}
  for(const c of d.counters)c.quantity=d.instances.filter(i=>i.counterTypeId===c.id).length;
  d.totals={Allied:d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side==='Allied').length,German:d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side==='German').length,types:d.counters.length};
  d.deployment={allowedHexes:{},instructions:{},loads:[]};
  for(const i of d.instances){const side=d.counters.find(c=>c.id===i.counterTypeId).side,boardId=d.setupBoards[side];d.deployment.allowedHexes[i.id]=d.boards.find(b=>b.id===boardId).hexes.filter(h=>h.setupAllowed).map(h=>({boardId,hexId:h.id}));d.deployment.instructions[i.id]='Deploy on board '+boardId+' under the original Situation setup rules.';}
  d.parentCampaign={...source.parentCampaign,title:'Normandy inland operations: St. Lo, 29 June',commandSnapshotId:s.id};
  d.campaignAssignment={campaignId:source.parentCampaign.id,situationId:d.id,number:1};d.campaign.parentCampaignId=source.parentCampaign.id;
  d.briefing='CAMPAIGN VARIANT. '+changes.join(' ')+' Committed '+NormandyCommand.clock(m.committedAt)+'. Source: '+source.briefing;
  return d;
 }
 return {variant};
})();
if(typeof module!=='undefined')module.exports=NormandySituation;
