let formationOpen=false,formationCounter=null;
let formationSceneKey=null;
const formationCounterNodes=new Map();
function currentFormationSituation(){return regionalState?.situations?.find(s=>s.parentMissionId===regionalMission);}
function openFormation(){
 const s=currentFormationSituation();if(!s)return;
 try{
  if(s.formation&&s.formation.sourceRevision!==s.revision)throw new Error("The Situation Card roster changed after this map was created. Clear the exercise and rebuild it before opening this map.");
  if(!s.formation){const next=JSON.parse(JSON.stringify(regionalState));next.situations.find(x=>x.id===s.id).formation=FormationModel.create(s);localStorage.setItem(regionalKey(),JSON.stringify(next));regionalState=next;}
  formationOpen=true;updateTheaterCounters();formationCounter=null;document.getElementById("mapControls").style.display="none";document.getElementById("theaterChoice").disabled=true;document.getElementById("regionalEntry").style.display="none";document.getElementById("map").style.display="none";document.getElementById("formationMap").style.display="";document.getElementById("regionalWorkspace").style.display="none";document.getElementById("formationControls").style.display="";document.getElementById("formationControls").open=true;document.getElementById("workspaceControls").style.display="none";renderCampaignActions();renderFormation();
 }catch(error){regionalNotice(error.message,"error");}
}
function closeFormation(){formationOpen=false;document.getElementById("mapControls").style.display="";document.getElementById("theaterChoice").disabled=false;formationCounter=null;document.getElementById("formationMap").style.display="none";document.getElementById("formationControls").style.display="none";document.getElementById("map").style.display="";if(activeRegion){document.getElementById("mapHeading").textContent=activeRegion.name;document.getElementById("breadcrumb").textContent="Whole Map / "+activeTheater.name+" / Normandy";document.getElementById("viewLevel").textContent="REGIONAL CAMPAIGN";}document.getElementById("workspaceControls").style.display=activeTheater?"":"none";renderRegional();updateTheaterCounters();}
function formationAction(action,target){
 try{const next=JSON.parse(JSON.stringify(regionalState)),s=next.situations.find(s=>s.parentMissionId===regionalMission),f=s.formation;
  if(Date.parse(f.clock.current)!==Date.parse(next.clock.current))throw new Error("Another situation has advanced campaign time. Concurrent-situation scheduling is not yet supported.");
  if(action==="move")FormationModel.move(f,formationCounter,target);else if(action==="order")FormationModel.order(f,formationCounter);else FormationModel.next(f);
  next.clock.current=f.clock.current;
  localStorage.setItem(regionalKey(),JSON.stringify(next));regionalState=next;document.getElementById("formationNotice").textContent="Saved.";renderFormation();
 }catch(error){document.getElementById("formationNotice").textContent=error.message;}
}
function renderFormation(){
 if(!formationOpen)return;const s=currentFormationSituation(),f=s.formation,svg=document.getElementById("formationMap");
 const sceneKey=[s.id,f.seed,f.geographyHash,f.sourceRevision,f.version].join("|");
 const make=(tag,attrs,text)=>{const n=document.createElementNS("http://www.w3.org/2000/svg",tag);for(const [k,v] of Object.entries(attrs))n.setAttribute(k,v);if(text)n.textContent=text;svg.append(n);return n;};
 if(formationSceneKey!==sceneKey){
 svg.replaceChildren();formationCounterNodes.clear();formationSceneKey=sceneKey;
 const left=Math.min(...f.cells.map(c=>c.x))-.2,top=Math.min(...f.cells.map(c=>c.y))-.2,right=Math.max(...f.cells.map(c=>c.x))+.2,bottom=Math.max(...f.cells.map(c=>c.y))+.2;
 svg.setAttribute("viewBox",[left,top,right-left,bottom-top].join(" "));
 const radius=.25/Math.sqrt(3);
 for(const c of f.cells){const points=Array.from({length:6},(_,i)=>[c.x+radius*Math.cos((60*i-30)*Math.PI/180),c.y+radius*Math.sin((60*i-30)*Math.PI/180)].join(",")).join(" ");
  const n=make("polygon",{class:"formation-hex",points,fill:c.terrain==="woods"?"#849c67":c.terrain==="fields"?"#d8cc94":"#e2dfc5",stroke:"#8c927d","stroke-width":.006,tabindex:0,role:"button","aria-label":c.id+", "+c.terrain});
  n.onclick=()=>formationAction("move",c.id);n.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();formationAction("move",c.id);}});
 }
 const goal=f.cells.find(c=>c.id===f.objectiveCellId);make("circle",{"pointer-events":"none",cx:goal.x,cy:goal.y,r:.11,fill:"none",stroke:"#b77725","stroke-width":.018});make("text",{"pointer-events":"none",x:goal.x,y:goal.y-.15,"font-size":.07,"text-anchor":"middle"},"Objective");
 for(const unit of f.counters){
  const n=make("rect",{class:"formation-counter",width:.19,height:.16,rx:.012,fill:unit.side==="american"?"#4a6326":"#66666a",tabindex:0,role:"button","aria-label":unit.name});
  const selectCounter=()=>{formationCounter=unit.id;renderFormation();};
  n.onclick=selectCounter;n.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();selectCounter();}});
  const label=make("text",{"font-size":.065,fill:"white","text-anchor":"middle","pointer-events":"none"},unit.side==="american"?"P"+(s.platoons.findIndex(p=>p.id===unit.id)+1):"DE");
  formationCounterNodes.set(unit.id,{node:n,label});
 }
 }
 for(const unit of f.counters){const c=f.cells.find(c=>c.id===unit.cellId),{node,label}=formationCounterNodes.get(unit.id);node.setAttribute("x",c.x-.095);node.setAttribute("y",c.y-.08);node.setAttribute("stroke",unit.id===formationCounter?"#b77725":"#fff");node.setAttribute("stroke-width",.012);node.setAttribute("aria-pressed",unit.id===formationCounter?"true":"false");label.setAttribute("x",c.x);label.setAttribute("y",c.y+.018);}
 document.getElementById("mapHeading").textContent="Formation Situation";document.getElementById("breadcrumb").textContent=s.title+" / 250 m hexes";document.getElementById("viewLevel").textContent="FORMATION VIEW";
 document.getElementById("formationPhase").textContent=f.clock.current.replace("T"," ").replace(".000Z"," UTC")+" | Turn "+f.turn+" | "+f.side+" | "+FormationModel.phases[f.phase];
 const selected=f.counters.find(c=>c.id===formationCounter);document.getElementById("formationSelection").textContent=selected?selected.name+" | "+selected.posture+" | "+(4-selected.spent)+" MP left | Assets: "+selected.assetIds.join(", "):"Select a counter. During Movement, click an adjacent empty hex to move it.";
 document.getElementById("formationOrder").disabled=!selected||selected.side!==f.side||f.phase!==0;
 document.getElementById("formationContact").textContent=f.contacts.length?"Contact recorded. Movement and phase progression are paused pending ASL engagement admission. Return to the Situation Card to inspect its drafts. No battle has been resolved.":"No contact. Objective occupancy does not automatically win the situation.";
 document.getElementById("formationNext").disabled=f.contacts.length>0;
}

document.getElementById("formationBack").onclick=closeFormation;
document.getElementById("formationNext").onclick=()=>formationAction("next");
document.getElementById("formationOrder").onclick=()=>formationAction("order");
