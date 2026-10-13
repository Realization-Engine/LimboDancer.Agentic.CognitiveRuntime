"use strict";
(()=>{
 if(!document.getElementById('ardennesWorkspace'))return;
 const R=window.CampaignRepository,M=ArdennesCommand,screen=window.CampaignSituationScreen;
 const main=document.querySelector('#ardennesWorkspace .operation-map'),host=document.createElement('section');host.id='campaignSituationSurface';host.hidden=true;main.append(host);
 let active=null;
 function restore(){active=null;host.hidden=true;main.classList.remove('showing-situation');location.href='index.html?exercise=ardennes';}
 const api={get active(){return active;},close(){screen.close();},async continueToSetup(id){if(active!==id&&!await api.open(id))return;window.CampaignSituationWorkflow.openMap();host.scrollTop=0;},refreshCard(cards){screen.refreshCard(cards);},async open(id){
  try{
   const world=R.get();if(!world?.operation.missions[id]?.choice)throw Error('Commit this mission at its headquarters first.');
   const source=window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===(id==='M14'?14:15));
   const hash=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',new TextEncoder().encode(JSON.stringify(source))))).map(b=>b.toString(16).padStart(2,'0')).join('');
   if(hash!==world.operation.sourceHashes[id])throw Error('Source card changed since allocation. Start a new campaign before using it.');
   const pkg=CampaignSupplyState.attach(M.variant(world.operation,id,source),world.operation,id,world.id);ArdennesWorkspace.selectMission(id);active=id;
   const cards=document.getElementById('operationSituations');cards.replaceChildren();
   const plan=world.situationPlans?.[id];
   screen.mount(pkg,{host,cards,plan,exit:restore,squadStorage:{
    load(){return R.get().squadPlans?.[id]||null;},
    save(squad){const current=R.get();if(current.id!==world.id)throw Error('Campaign changed. Reopen this Situation.');R.save({...current,squadPlans:{...current.squadPlans,[id]:squad}});},
    clear(){const current=R.get();if(current.id!==world.id)throw Error('Campaign changed. Reopen this Situation.');const plans={...current.squadPlans};delete plans[id];R.save({...current,squadPlans:plans});}
   },save(plan){const current=R.get();R.save({...current,situationPlans:{...current.situationPlans,[id]:plan}});}});
   main.classList.add('showing-situation');cards.scrollTop=0;for(const control of document.querySelectorAll('#operationTime button:not(#timelineToggle),#operationTime input,#operationTime select')){control.disabled=true;control.title='Return to campaign command to change timeline view.';}
   const context=document.createElement('p');context.className='campaign-board-context';context.textContent='Campaign-linked Situation | '+M.clock(world.operation.missions[id].committedAt)+' | '+world.operation.missions[id].choice+' defense. Command and supply share the joined source boards. Authored placement, not historical georeferencing.';cards.prepend(context);return true;
  }catch(e){active=null;const warning=document.createElement('p');warning.setAttribute('role','alert');warning.textContent=e.message;document.getElementById('operationSituations').prepend(warning);return false;}
 }};window.CampaignSituationIntegration=api;
 // Old generated-setup links now enter the same campaign workspace and established card flow.
 const old=new URLSearchParams(location.search).get('worldMission');if(old)location.replace('index.html?exercise=ardennes&situation='+encodeURIComponent(old));
 const selected=new URLSearchParams(location.search).get('situation');if(selected)api.open(selected);
})();
