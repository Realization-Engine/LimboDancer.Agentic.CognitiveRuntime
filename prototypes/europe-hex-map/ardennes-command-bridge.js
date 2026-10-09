"use strict";
// Install a replayed committed variant in memory only. Never overwrite source packages.
window.ARDENNES_VARIANT_READY=(async()=>{
 const q=new URLSearchParams(location.search),id=q.get('commandMission');if(!id)return null;
 const M=ArdennesCommand,active=localStorage.getItem('campaign-atlas:world:v1:active'),world=active&&JSON.parse(localStorage.getItem('campaign-atlas:world:v1:'+active));const s=M.restore(world?world.operation:localStorage.getItem(M.KEY));if(s.id!==q.get('commandSession'))throw Error('This exercise is no longer active. Return to the command exercise.');
 const source=window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===(id==='M14'?14:15));
 const hash=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(JSON.stringify(source))))).map(b=>b.toString(16).padStart(2,'0')).join('');
 if(s.sourceHashes[id]!==hash)throw Error('Source package changed. A new exercise is required.');
 const d=M.variant(s,id,source);window.PANZER_SITUATION_LIBRARY.push(d);window.CAMPAIGN_REGISTRY.campaigns.push({...d.parentCampaign,nextSituationNumber:3,situations:[{id:d.campaignAssignment.situationId,number:d.campaignAssignment.number,title:d.situation.title,sourcePackageId:d.id}]});window.ARDENNES_COMMAND_PACKAGE_ID=d.id;return d.id;
})();
