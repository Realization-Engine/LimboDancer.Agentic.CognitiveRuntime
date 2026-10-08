// Dated command-counter display. Geographic anchors are authored callouts, not deployment evidence.
let theaterCounterLayer=null,theaterCounterSelected=null;
const theaterCounterNodes=[];
function initializeTheaterCounters(){
 if(theaterCounterLayer)return;
 theaterCounterLayer=element("g",{id:"theaterCommandCounters"});
 // Organization remains available as dated reference data. The operational
 // parent map contains Situations, not these illustrative deployment badges.
 theaterCounterLayer.style.display="none";

}
function renderTheaterCounterDetail(){
 const panel=document.getElementById("theaterCounterDetail");panel.replaceChildren();
 if(!theaterCounterSelected){panel.textContent="Select a formation in the reference list above. These are 8 June command records, not deployed map counters.";return;}
 const c=DATA.regionalCampaign,f=regionalState.forces.find(f=>f.id===theaterCounterSelected);
 const add=(tag,text)=>{const n=document.createElement(tag);n.textContent=text;panel.append(n);return n;};
 add("h3",f.name);add("p",f.echelon+" | "+regionalState.clock.current.replace("T"," ")+" | Parent: "+(c.forces.find(p=>p.id===f.parentId)?.name||"none"));
 add("p","8 June command organization reference. This record does not place a counter on the parent campaign map or determine a Situation deployment.");
 if(f.openingReport)add("p","Opening context: "+f.openingReport.text+" "+f.openingReport.asOf+". "+f.openingReport.precision+".");
 if(f.echelon==="Division"){
  add("h4","Infantry organization and orders");
  add("p","3 infantry regiments / 9 infantry battalions. Counts describe organization, not current fighting strength. Order status is what this division headquarters knows.");
  for(const r of RegionalModel.divisionSummary(regionalState,f.id)){
   add("h4",r.name+" | "+r.battalions+" battalions | "+r.status);
   if(r.openingReport)add("p",r.openingReport.text+" Report: "+r.openingReport.asOf+".");
   add("p",regionalState.forces.filter(x=>x.parentId===r.id).map(x=>x.name).join("; "));
  }
  const ids=new Set(regionalState.forces.filter(x=>x.parentId===f.id).map(x=>x.id));
  for(const a of c.historicalAttachments||[])if(ids.has(a.toId))add("p","Historical attachment, "+a.reportedDate+": "+c.forces.find(x=>x.id===a.unitId).name+" to "+c.forces.find(x=>x.id===a.toId).name+". "+a.status);
 }else add("p","Subordinate divisions: "+c.forces.filter(x=>x.parentId===f.id).map(x=>x.name).join(", "));
 add("p","Actual personnel, available equipment, readiness and exact footprint remain unknown. Supporting arms are not allocated by this infantry roster. No fixed number of platoon counters is inferred from hex area.");
 const source=c.sources.find(s=>s.id===f.sourceId),link=add("a","Historical organizational context");link.href=source.url;link.target="_blank";link.rel="noopener";
 const button=add("button","Show command workflow");button.onclick=()=>{const workspace=document.getElementById("regionalWorkspace");workspace.open=true;workspace.scrollIntoView?.({block:"start",behavior:"smooth"});};
 add("p","The command workflow retains your current headquarters and progress. Selecting an organization reference does not issue orders or bypass the hierarchy.");
}
function updateTheaterCounters(){
 initializeTheaterCounters();
 const campaign=DATA.regionalCampaign.campaign;
 const visible=activeRegion?.id===DATA.regionalCampaign.id&&activeTheater?.id===DATA.regionalCampaign.theaterId&&!formationOpen&&regionalState?.clock.campaignId===campaign.id&&Date.parse(regionalState.clock.current)>=Date.parse(campaign.opening)&&Date.parse(regionalState.clock.current)<Date.parse(campaign.endExclusive);
 const controls=document.getElementById("theaterCounterControls");
 theaterCounterLayer.style.display="none";document.getElementById("theaterCounterControls").style.display=visible?"":"none";
 if(!visible){theaterCounterSelected=null;renderTheaterCounterDetail();return;}
 if(theaterCounterSelected)renderTheaterCounterDetail();

}
document.getElementById("focusTheaterCounters").onclick=()=>{view=[...DATA.regionalCampaign.view];renderView();};
for(const id of ["1-id","29-id","v-corps"]){const button=document.createElement("button");button.textContent=id==="1-id"?"1st Infantry Division":id==="29-id"?"29th Infantry Division":"V Corps HQ";button.onclick=()=>{theaterCounterSelected=id;renderTheaterCounterDetail();updateTheaterCounters();};document.getElementById("theaterCounterList").append(button);}
