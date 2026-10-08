// Render a bounded planning bridge without claiming tactical engine admission.
function renderSituation(){
 const panel=document.getElementById("situationPanel");panel.replaceChildren();
 if(!activeRegion||!regionalState)return;
 const mission=regionalState.missions.find(m=>m.id===regionalMission),force=regionalState.forces.find(f=>f.id===mission?.recipient);
 if(force?.echelon!=="Battalion"||mission?.recipient!==regionalHQ)return;
 const add=(tag,text)=>{const n=document.createElement(tag);n.textContent=text;panel.append(n);return n;};
 add("h3","Campaign Situation Card");
 let situation=regionalState.situations?.find(s=>s.parentMissionId===regionalMission);
 if(!situation){
  add("p","At a battalion headquarters, receive and plan its mission, select a map sector, then create a Situation Card. This example uses an illustrative company roster, not reconstructed historical strengths.");
  const b=add("button","Create Campaign Situation Card");b.disabled=force?.echelon!=="Battalion"||mission?.recipient!==regionalHQ||!["planned","executing"].includes(mission?.status)||!regionalSector;
  b.onclick=()=>changeSituation("create");return;
 }
 if(situation.formation||situation.tacticalTest){
  const openButton=add("button",situation.formation?"Resume campaign map":"Open campaign map");openButton.onclick=openFormation;
 }else{
  add("p","Choose your setup before opening the map. Maneuver only has no opposing counter. To include the authored opponent, prepare the engagement drafts and catalog-backed tactical test below first. Setup cannot change after the formation map has been created.");
  const maneuver=add("button","Open campaign map (maneuver only)");maneuver.onclick=openFormation;
 }
 add("h4",situation.title);add("p","Parent: "+situation.parentFormationId+" | Area: "+situation.area.sectorId+" | Campaign interval: "+situation.campaignInterval.start+" to "+situation.campaignInterval.endExclusive);
 add("p",situation.objective.intent);add("p","Situation success: "+situation.objective.success);add("p",situation.objective.evaluation);
 add("p",situation.provenance.roster);
 for(const platoon of situation.platoons){const assets=situation.assets.filter(a=>a.formationId===platoon.id);add("p",platoon.name+": "+assets.filter(a=>a.kind==="squad").length+" squads, "+assets.filter(a=>a.kind==="leader").length+" leaders | "+(assets.every(a=>situation.reserveIds.includes(a.id))?"Reserve":assets.some(a=>situation.reservations.some(r=>r.assetId===a.id))?"Held for engagement drafts":"Available for engagement planning"));}
 add("p","Sector anchor only. Tactical footprints, movement and contact are not simulated. Support is unallocated.");
 if(!situation.engagements.length){const b=add("button","Prepare two engagement drafts");b.disabled=regionalHQ!==mission.recipient;b.onclick=()=>changeSituation("decompose");}
 for(const e of situation.engagements){add("h4",e.title);add("p",e.objective+" Window: "+e.window.startMinute+"-"+e.window.endMinute+" minutes. "+e.assetIds.length+" reserved asset identities. "+(e.dependsOn.length?"Conditional on the preceding engagement; admission must evaluate its results.":"Initial engagement candidate."));
  add("p","Persistent assets: "+e.assetIds.join(", "));
  const draft=SituationModel.draft(situation,e.id);add("p","ASL admission blocked: "+draft.admission.requirements.join(" "));
  const b=add("button","Export ASL handoff draft");b.onclick=()=>download(e.id.replaceAll(":","-")+".draft.json",draft);
 }
 if(situation.engagements.length&&!situation.tacticalTest){const b=add("button","Prepare catalog-backed tactical test");b.disabled=!!situation.formation;b.onclick=()=>changeSituation("tactical-test");add("p","Optional next step: map the company roster to ASL catalog units and prepare the access engagement on representative board 04. This is not generated Normandy terrain; results cannot update the campaign.");}
 if(situation.tacticalTest){
  add("h3","Catalog-backed tactical test");
  add("p","First engagement: 3 American squads and an 8-0 leader versus 2 German squads and an 8-0 leader. Six turns on representative board 04. Exit 4 American VP through the specified bottom-edge area. ASL catalog: "+situation.catalog);
  add("p","Engine-tested card template. Export the card for ASL Studio validation and play. Campaign admission and reconciliation remain disabled because the board is not the actual engagement terrain.");
  const cardButton=add("button","Export ASL test Scenario Card");cardButton.onclick=()=>download(situation.tacticalTest.scenarioCard.id+".scenario-card.json",situation.tacticalTest.scenarioCard);
  const bundleButton=add("button","Export test identity manifest");bundleButton.onclick=async()=>{try{download("formation-tactical-test.json",await TacticalHandoff.bundle(situation));}catch(error){regionalNotice(error.message,"error");}};
 }
 const exportButton=add("button","Export Campaign Situation Card");exportButton.onclick=()=>download("formation-situation.json",situation);
}
function changeSituation(action){
 try{
  const next=JSON.parse(JSON.stringify(regionalState));next.situations??=[];
  if(action==="create"){
   if(next.situations.some(s=>s.parentMissionId===regionalMission))throw new Error("Situation already exists for this mission.");
   const m=next.missions.find(m=>m.id===regionalMission);if(m?.recipient!==regionalHQ)throw new Error("Only the assigned battalion can create this situation.");
   next.situations.push(SituationModel.create(next,regionalMission,regionalSector,DATA.regionalCampaign));
  }else{
   const situation=next.situations.find(s=>s.parentMissionId===regionalMission);if(!situation||situation.parentFormationId!==regionalHQ)throw new Error("Only the assigned battalion can decompose this situation.");if(action==="tactical-test")TacticalHandoff.prepare(situation);else SituationModel.decompose(situation);
  }
  localStorage.setItem(regionalKey(),JSON.stringify(next));regionalState=next;regionalNotice("Situation planning saved. ASL execution remains blocked pending admission.","success");renderSituation();renderCampaignActions();
 }catch(error){regionalNotice(error.message,"error");}
}
