// Dated command-counter display. Geographic anchors are authored callouts, not deployment evidence.
let theaterCounterLayer=null,theaterCounterSelected=null;
const theaterCounterNodes=[];
function initializeTheaterCounters(){
 if(theaterCounterLayer)return;
 theaterCounterLayer=element("g",{id:"theaterCommandCounters"});
 const specs=[{id:"1-id",label:"XX  1 ID",objective:"trevieres"},{id:"29-id",label:"XX  29 ID",objective:"isigny"},{id:"v-corps",label:"XXX  V HQ",objective:"omaha"}];
 for(const [index,spec] of specs.entries()){
  const objective=DATA.regionalCampaign.objectives.find(o=>o.id===spec.objective);
  const anchor=DATA.regionalCampaign.cells.reduce((best,c)=>Math.hypot(c.x-objective.x,c.y-objective.y)<Math.hypot(best.x-objective.x,best.y-objective.y)?c:best);
  const group=element("g",{},theaterCounterLayer),line=element("path",{fill:"none",stroke:"#4a6326","stroke-width":1.2,"stroke-dasharray":"3 3","pointer-events":"none"},group);
  element("circle",{r:3,fill:"#4a6326","pointer-events":"none"},group);
  const badge=element("g",{transform:`translate(22 ${index*39-50})`,tabindex:0,role:"button","aria-label":DATA.regionalCampaign.forces.find(f=>f.id===spec.id).name+", illustrative placement",class:"theater-command-counter"},group);
  const rect=element("rect",{width:106,height:30,rx:3,fill:spec.id==="v-corps"?"#303d27":"#4a6326",stroke:"#fff","stroke-width":2},badge);
  const text=element("text",{x:53,y:20,"text-anchor":"middle","font-size":14,fill:"#fff","pointer-events":"none"},badge);text.textContent=spec.label;
  line.setAttribute("d",`M0,0 L22,${index*39-35}`);
  const inspect=()=>{theaterCounterSelected=spec.id;renderTheaterCounterDetail();updateTheaterCounters();};
  badge.addEventListener("click",()=>{if(!dragged)inspect();});badge.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();inspect();}});
  theaterCounterNodes.push({spec,anchor,group,badge,rect});
 }
}
function renderTheaterCounterDetail(){
 const panel=document.getElementById("theaterCounterDetail");panel.replaceChildren();
 if(!theaterCounterSelected){panel.textContent="Select a division or headquarters counter on the map, or in the list above.";return;}
 const c=DATA.regionalCampaign,f=regionalState.forces.find(f=>f.id===theaterCounterSelected),entry=theaterCounterNodes.find(n=>n.spec.id===f.id);
 const add=(tag,text)=>{const n=document.createElement(tag);n.textContent=text;panel.append(n);return n;};
 add("h3",f.name);add("p",f.echelon+" | "+regionalState.clock.current.replace("T"," ")+" | Parent: "+(c.forces.find(p=>p.id===f.parentId)?.name||"none"));
 add("p","Illustrative anchor: "+entry.anchor.id+". Dashed callouts separate counters for readability. They are not deployment positions or formation footprints.");
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
 add("p","The command workflow retains your current headquarters and progress. Selecting a map counter does not issue orders or bypass the hierarchy.");
}
function updateTheaterCounters(){
 initializeTheaterCounters();
 const campaign=DATA.regionalCampaign.campaign;
 const visible=activeRegion?.id===DATA.regionalCampaign.id&&activeTheater?.id===DATA.regionalCampaign.theaterId&&!formationOpen&&regionalState?.clock.campaignId===campaign.id&&Date.parse(regionalState.clock.current)>=Date.parse(campaign.opening)&&Date.parse(regionalState.clock.current)<Date.parse(campaign.endExclusive);
 const controls=document.getElementById("theaterCounterControls");if(visible&&controls.style.display==="none")controls.open=true;
 theaterCounterLayer.style.display=visible?"":"none";document.getElementById("theaterCounterControls").style.display=visible?"":"none";
 if(!visible){theaterCounterSelected=null;renderTheaterCounterDetail();return;}
 if(theaterCounterSelected)renderTheaterCounterDetail();
 const scale=Math.max(view[2]/(svg.clientWidth||1200),view[3]/(svg.clientHeight||900));
 for(const {spec,anchor,group,badge,rect} of theaterCounterNodes){group.setAttribute("transform",`translate(${anchor.x} ${anchor.y}) scale(${scale})`);rect.setAttribute("stroke",theaterCounterSelected===spec.id?"#b77725":"#fff");badge.setAttribute("aria-pressed",theaterCounterSelected===spec.id?"true":"false");}
}
document.getElementById("focusTheaterCounters").onclick=()=>{view=[...DATA.regionalCampaign.view];renderView();};
for(const id of ["1-id","29-id","v-corps"]){const button=document.createElement("button");button.textContent=id==="1-id"?"1st Infantry Division":id==="29-id"?"29th Infantry Division":"V Corps HQ";button.onclick=()=>{theaterCounterSelected=id;renderTheaterCounterDetail();updateTheaterCounters();};document.getElementById("theaterCounterList").append(button);}
