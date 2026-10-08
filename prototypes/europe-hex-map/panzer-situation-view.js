"use strict";
(()=>{
 const d=window.PANZER_SITUATION_DATA,base="assets/panzer-leader-04/",key="campaign-atlas:"+d.id+":v2";
 const campaignRecord=window.CAMPAIGN_REGISTRY.campaigns.find(c=>c.id===d.campaignAssignment.campaignId);
 const situationRecord=campaignRecord.situations.find(c=>c.id===d.campaignAssignment.situationId);
 const designation=String(situationRecord.number).padStart(2,"0");
 const el=(tag,text)=>{const n=document.createElement(tag);if(text!==undefined)n.textContent=text;return n;};
 const dialog=el("dialog");dialog.id="panzerSituation";dialog.setAttribute("aria-labelledby","panzerTitle");document.body.append(dialog);
 let s=PanzerSituationModel.create(d),selected=null,board="A",error="",selectedEngagement=new Set(),openTypes=new Set(),openSides=new Set(["German","Allied"]),terrainView=true,inspectedHex=null,hexDetails=false,boardZoom=1,spaceDown=false,suppressPanClick=false,activePan=null,mapMode="setup";
 try{const saved=localStorage.getItem(key);if(saved)s=PanzerSituationModel.validate(JSON.parse(saved),d);}catch(e){error="Saved plan could not be loaded: "+e.message+" Clear this Situation to start again.";}
 const editable=target=>!!target?.closest?.("input,textarea,select,[contenteditable]:not([contenteditable='false'])");
 const updatePanCursor=()=>dialog.setAttribute("data-pan-ready",String(spaceDown));
 const stopPan=()=>{if(activePan){const {viewport,id}=activePan;activePan=null;viewport.setAttribute("data-panning","false");if(viewport.hasPointerCapture?.(id))viewport.releasePointerCapture(id);}};
 dialog.addEventListener("keydown",e=>{if(e.code==="Space"&&!editable(e.target)){e.preventDefault();spaceDown=true;updatePanCursor();}});
 const releaseSpace=e=>{if(e.code==="Space"){spaceDown=false;updatePanCursor();}};
 dialog.addEventListener("keyup",releaseSpace);
 window.addEventListener?.("keyup",releaseSpace);
 window.addEventListener?.("blur",()=>{spaceDown=false;updatePanCursor();stopPan();});
 dialog.addEventListener("close",()=>{spaceDown=false;updatePanCursor();stopPan();});
 const blockPanClick=e=>{if(!suppressPanClick&&!spaceDown)return false;e.preventDefault?.();e.stopPropagation?.();suppressPanClick=false;return true;};
 const save=()=>{try{localStorage.setItem(key,JSON.stringify(s));}catch(e){error="Unable to save locally. Export your plan before closing.";}};
 const act=fn=>{try{error="";fn();save();}catch(e){error=e.message;}render();};
 const button=(label,fn)=>{const b=el("button",label);b.type="button";b.onclick=fn;return b;};
 const download=(obj,name)=>{const u=URL.createObjectURL(new Blob([JSON.stringify(obj,null,2)],{type:"application/json"})),a=el("a");a.href=u;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(u),1000);};
 function render(){
  const game=mapMode==="game"&&s.stage==="ready";dialog.setAttribute("data-map-mode",game?"game":"setup");
  const oldY=dialog.scrollTop||0,oldTray=dialog.querySelector?.(".panzer-tray")?.scrollTop||0,oldX=dialog.querySelector?.(".panzer-joined-scroll")?.scrollLeft||0,oldMapY=dialog.querySelector?.(".panzer-joined-scroll")?.scrollTop||0;
  dialog.replaceChildren();const top=el("div");top.className="panzer-toolbar";const title=el("h2",game?"St. Lo | Game map":"St. Lo | Campaign Map Setup");title.id="panzerTitle";top.append(title,button("Return to Normandy campaign",()=>campaignFlow.returnParent()),button("Clear this Situation",()=>{if(confirm("Clear all St. Lo setup placements, notes and engagement drafts? Other campaigns are unaffected.")){s=PanzerSituationModel.create(d);mapMode="setup";selected=null;selectedEngagement.clear();board="A";error="";save();render();}}),button("Export Situation plan",()=>download({package:d,plan:s},"panzer-leader-04-plan.json")));if(game){top.children[2].hidden=true;top.append(button("Review deployment",()=>{mapMode="setup";render();}));}dialog.append(top);
  if(!game)dialog.append(el("p","29 June 1944 (printed game date) | 15 turns | Allies move first | German setup first on A; Allied setup second on C."));
  const brief=el("details"),summary=el("summary","Campaign Situation Card, objectives and evidence");brief.append(summary,el("p",d.briefing));for(const v of d.victory)brief.append(el("p",v));brief.append(el("p","Special rules: "+d.specialRules));const scan=el("img");scan.src=base+"original-card.png";scan.alt="Original Situation 4 card, including board orientation, forces, setup and victory conditions";scan.className="panzer-card-scan";brief.append(scan);dialog.append(brief);
  if(!game)dialog.append(el("p","Data admission: "+d.admission.status+". Board "+board+": "+d.boards.find(b=>b.id===board).grid.coverage+"; "+d.boards.find(b=>b.id===board).hexes.length+" transcribed hexes. First-pass terrain requires review before combat admission."));
  if(!game)dialog.append(el("p","Setup planning workspace. Both boards are joined in Situation orientation. Counters snap to interior setup hexes. The JSON terrain view is a first-pass transcription; combat and victory adjudication are not implemented. Earlier freehand plans are retained separately."));
  const notice=el("p",error);notice.setAttribute("role","status");dialog.append(notice);
  const status=game?"Deployment complete. Inspect units and terrain. Turn execution and combat are not implemented.":s.stage==="ready"?"Both sides prepared. Launch the game map when ready.":s.stage+" setup: select a counter, then click its position on board "+d.setupBoards[s.stage]+". You can reposition it until setup is complete.";dialog.append(el("h3",status));
  if(!game){const launch=button(s.gameLaunched?"Resume game map":"Launch situation game map",()=>act(()=>{PanzerSituationModel.launch(s,d);mapMode="game";selected=null;boardZoom=1;stopPan();}));launch.disabled=s.stage!=="ready";launch.className="primary";dialog.append(launch);}
  const controls=el("div");controls.className="panzer-toolbar";if(!game&&s.stage!=="ready"){const next=button(s.stage==="German"?"Complete German setup":"Complete Allied setup",()=>act(()=>{PanzerSituationModel.next(s,d);selected=null;board="C";}));next.disabled=!PanzerSituationModel.complete(s,d,s.stage);controls.append(next);}if(!game)controls.append(button(terrainView?"Show source artwork":"Show JSON terrain",()=>{terrainView=!terrainView;render();}));const detailToggle=button(hexDetails?"Hide hex details":"Show hex details",()=>{hexDetails=!hexDetails;render();});detailToggle.setAttribute("aria-pressed",String(hexDetails));detailToggle.disabled=!(game||terrainView);controls.append(detailToggle);const zoomButton=button(boardZoom>1?"Fit both boards":"Enlarge boards",()=>{boardZoom=boardZoom>1?1:2;render();});controls.append(zoomButton);dialog.append(controls);
  const inspected=d.boards.find(b=>b.id===board).hexes.find(h=>h.id===inspectedHex);
  dialog.append(el("p",inspected?inspected.label+": "+inspected.terrain.base+"; "+(inspected.terrain.features.join(", ")||"no additional feature recorded")+"; elevation category "+inspected.terrain.elevationLevel+". "+(inspected.setupAllowed?"Interior setup hex.":"Boundary fragment; setup excluded.")+" Evidence: "+inspected.terrain.evidence.status:game?"Click a hex or counter to inspect the deployed situation.":"Click a hex to inspect its transcription. With a counter selected, the click also places it."));
  const layout=el("div");layout.className="panzer-layout";const tray=el("section");tray.className="panzer-tray";tray.setAttribute("aria-label","Situation counter roster");
  for(const side of ["German","Allied"]){const sideGroup=el("details");sideGroup.className="panzer-side-group";sideGroup.setAttribute("data-side",side);sideGroup.open=openSides.has(side);sideGroup.ontoggle=()=>{if(sideGroup.isConnected===false)return;if(sideGroup.open)openSides.add(side);else openSides.delete(side);};sideGroup.append(el("summary",(side==="Allied"?"American":side)+" ("+d.totals[side]+")"));tray.append(sideGroup);for(const type of d.counters.filter(t=>t.side===side)){const group=el("details");group.open=openTypes.has(type.id);group.ontoggle=()=>{if(group.isConnected===false)return;if(group.open)openTypes.add(type.id);else openTypes.delete(type.id);};const heading=el("summary",type.label+" x "+type.quantity),placedCount=d.instances.filter(i=>i.counterTypeId===type.id&&s.placements[i.id]).length,badge=el("span",placedCount+" placed");badge.className="panzer-placed-count";badge.setAttribute("data-complete",String(placedCount===type.quantity));badge.setAttribute("data-counter-type",type.id);badge.setAttribute("aria-label",placedCount+" of "+type.quantity+" placed");heading.append(badge);group.append(heading);const art=el("img");art.src=base+type.artwork;art.alt=type.label;const pick=button("",()=>{const candidates=d.instances.filter(i=>i.counterTypeId===type.id);selected=(candidates.find(i=>!s.placements[i.id])||candidates[0]).id;board=d.setupBoards[side];error="";render();});pick.setAttribute("aria-label","Select "+type.label+" counter");pick.disabled=s.stage!==side;pick.append(art);group.append(pick,el("p",Object.entries(type.factors).map(([k,v])=>k+": "+v).join(" | ")));for(const note of type.notes)group.append(el("small",note));for(const i of d.instances.filter(i=>i.counterTypeId===type.id)){const b=button(i.id+(s.placements[i.id]?" (placed)":""),()=>{selected=i.id;error="";if(s.stage==="ready"){if(selectedEngagement.has(i.id))selectedEngagement.delete(i.id);else selectedEngagement.add(i.id);}else board=d.setupBoards[side];render();});b.disabled=s.stage!=="ready"&&s.stage!==side;b.setAttribute("aria-pressed",String(s.stage==="ready"?selectedEngagement.has(i.id):selected===i.id));group.append(b);}sideGroup.append(group);}}
  const feedback=el("p",game?(selected?"Selected unit: "+selected:"Select a deployed unit to inspect it."):error||(selected?"Selected: "+selected+". "+(s.stage==="ready"?"Setup complete.":"Click an interior hex on Board "+d.setupBoards[s.stage]+" to place or move it."):"Select a counter image or an individual counter in the roster, then click its board."));feedback.className="panzer-placement-status";feedback.setAttribute("role","status");layout.append(feedback);
  const viewport=el("div");viewport.className="panzer-joined-scroll";
  viewport.addEventListener("pointerdown",e=>{
   suppressPanClick=false;
   if(e.button!==1&&!(e.button===0&&spaceDown))return;
   e.preventDefault();e.stopPropagation();suppressPanClick=true;
   activePan={viewport,id:e.pointerId,x:e.clientX,y:e.clientY,left:viewport.scrollLeft,top:viewport.scrollTop};
   viewport.setPointerCapture?.(e.pointerId);viewport.setAttribute("data-panning","true");
  },true);
  viewport.addEventListener("pointermove",e=>{if(!activePan||activePan.viewport!==viewport||activePan.id!==e.pointerId)return;e.preventDefault();viewport.scrollLeft=activePan.left+activePan.x-e.clientX;viewport.scrollTop=activePan.top+activePan.y-e.clientY;});
  const endPan=e=>{if(activePan?.viewport===viewport&&activePan.id===e.pointerId){e.preventDefault();stopPan();}};
  viewport.addEventListener("pointerup",endPan);viewport.addEventListener("pointercancel",endPan);
  viewport.addEventListener("lostpointercapture",()=>{if(activePan?.viewport===viewport)stopPan();});
  viewport.addEventListener("click",blockPanClick,true);
  viewport.addEventListener("auxclick",e=>{if(e.button===1){e.preventDefault();e.stopPropagation();}});
  viewport.addEventListener("dragstart",e=>{if(activePan||spaceDown)e.preventDefault();});
  const joined=el("div");joined.className="panzer-joined";joined.setAttribute("data-detail",String(boardZoom>1.4));joined.style.width=(boardZoom*100)+"%";
  for(const placement of d.illustration.joinedLayout){
   const boardId=placement.boardId,rotation=placement.clockwiseDegrees,bd=d.boards.find(b=>b.id===boardId);
   const sheet=el("section");sheet.className="panzer-sheet";
   sheet.append(el("h3","Board "+boardId+" | "+(boardId==="A"?"German setup":"Allied setup")));
   const map=el("div");map.className="panzer-board panzer-board-joined";
   const svg=document.createElementNS("http://www.w3.org/2000/svg","svg");svg.setAttribute("viewBox","0 0 3138 1130");svg.setAttribute("aria-label","Joined Situation board "+boardId);svg.setAttribute("class","panzer-joined-svg");
   const drawing=document.createElementNS("http://www.w3.org/2000/svg","g");drawing.setAttribute("transform",rotation===90?"translate(3138 0) rotate(90)":"translate(0 1130) rotate(-90)");
   if(game||terrainView)PanzerMapArt.draw(drawing,d,bd,board===boardId?inspectedHex:null,hexDetails,true);
   else{const img=document.createElementNS("http://www.w3.org/2000/svg","image");img.setAttribute("href",base+"board-"+boardId+".png");img.setAttribute("width","1130");img.setAttribute("height","3138");drawing.append(img);}
   svg.append(drawing);map.append(svg);const rose=boardId===d.illustration.joinedLayout[0].boardId?PanzerMapArt.northRose(d,boardId,true):null;if(rose)map.append(rose);
   map.addEventListener("wheel",e=>{
    e.preventDefault();e.stopPropagation();
    const before=svg.getBoundingClientRect();if(!before.width||!before.height)return;
    const u=(e.clientX-before.left)/before.width,v=(e.clientY-before.top)/before.height;
    const delta=e.deltaY*(e.deltaMode===1?16:e.deltaMode===2?viewport.clientHeight:1);
    const next=Math.max(1,Math.min(6,boardZoom*Math.exp(-Math.max(-300,Math.min(300,delta))*.002)));
    if(next===boardZoom)return;boardZoom=next;
    joined.style.width=(boardZoom*100)+"%";joined.setAttribute("data-detail",String(boardZoom>1.4));
    const after=svg.getBoundingClientRect();
    viewport.scrollLeft+=after.left+u*after.width-e.clientX;
    viewport.scrollTop+=after.top+v*after.height-e.clientY;
    zoomButton.textContent=boardZoom>1?"Fit both boards":"Enlarge boards";
   },{passive:false});
   map.onclick=e=>{if(blockPanClick(e))return;let point;const matrix=drawing.getScreenCTM?.();if(matrix&&svg.createSVGPoint){const cursor=svg.createSVGPoint();cursor.x=e.clientX;cursor.y=e.clientY;const local=cursor.matrixTransform(matrix.inverse());point={u:local.x/1130,v:local.y/3138};}else{const rect=svg.getBoundingClientRect();point=PanzerMapArt.unproject(rotation,(e.clientX-rect.left)/rect.width,(e.clientY-rect.top)/rect.height);}board=boardId;const h=PanzerSituationModel.hexAt(d,boardId,point.u,point.v);inspectedHex=h?.id||null;if(!game&&selected&&s.stage!=="ready")act(()=>PanzerSituationModel.place(s,d,selected,boardId,point.u,point.v));else render();};
   for(const [id,p] of Object.entries(s.placements)){if(p.board!==boardId)continue;const instance=d.instances.find(i=>i.id===id),token=button(id,()=>{}),position=PanzerMapArt.project(rotation,p.x,p.y);token.onclick=e=>{if(blockPanClick(e))return;e.stopPropagation();if(!game&&selected&&selected!==id&&s.stage!=="ready"){board=boardId;inspectedHex=p.hexId;act(()=>PanzerSituationModel.place(s,d,selected,boardId,p.x,p.y));}else{selected=id;board=boardId;render();}};token.className="panzer-token";token.style.left=position.x*100+"%";token.style.top=position.y*100+"%";token.title=id;token.setAttribute("aria-pressed",String(selected===id));token.setAttribute("aria-label",id);token.textContent="";const art=el("img");art.src=base+instance.artwork;art.alt=id;token.append(art);map.append(token);}
   sheet.append(map,el("p",boardId==="A"?"Allied approach from C (east)  \u2190":"\u2192  German opposition from A (west)"));joined.append(sheet);
  }
  viewport.append(joined);if(!game)layout.append(tray);layout.append(viewport);dialog.append(layout);
  if(game){dialog.scrollTop=oldY;viewport.scrollLeft=boardZoom===1?0:oldX;viewport.scrollTop=boardZoom===1?0:oldMapY;return;}
  dialog.append(el("p","Joined setup view: A rotated clockwise, C counter-clockwise, meeting at A's east edge. Approach arrows describe the opposing side of the battlefield, not fixed positions or compulsory movement. Use the mouse wheel over the map to zoom toward the pointer. Hold Space and drag with the left mouse button, or drag with the middle button, to pan; Fit both boards restores the overview. Terrain and cross-board movement rules still require review."));
  const noteLabel=el("label","Situation planning notes"),notes=el("textarea");notes.id="panzerNotes";noteLabel.htmlFor=notes.id;notes.value=s.notes;notes.onchange=()=>{s.notes=notes.value;save();};dialog.append(noteLabel,notes);
  const intentLabel=el("label","Local objective for the selected counters"),intent=el("textarea");intent.id="panzerIntent";intentLabel.htmlFor=intent.id;intent.placeholder="For example: Secure the eastern approach to Grancelles while retaining the stream crossing.";
  const draft=button("Create ASL engagement draft",()=>act(()=>PanzerSituationModel.draft(s,d,[...selectedEngagement],intent.value)));draft.disabled=s.stage!=="ready";dialog.append(el("h3","ASL Scenario Card handoff"),el("p",selectedEngagement.size+" counters selected. Select participants from both sides in the roster. Each draft holds those counters; their squad-level composition still needs to be resolved."),intentLabel,intent,draft);
  dialog.scrollTop=oldY;tray.scrollTop=oldTray;viewport.scrollLeft=boardZoom===1?0:oldX;viewport.scrollTop=boardZoom===1?0:oldMapY;
  for(const e of s.engagements)dialog.append(button("Export blocked draft: "+e.intent,()=>download(e,e.id.replaceAll(":","-")+".json")));
 }
 const open=()=>{if(!campaignFlow.active||!campaignFlow.reviewed)return;mapMode=s.gameLaunched?"game":"setup";board=s.stage==="Allied"?"C":"A";render();dialog.showModal();};
 const cardPanel=el("section");cardPanel.id="campaignSituationStep";cardPanel.hidden=true;document.getElementById("workflowInspector").append(cardPanel);
 const node=id=>document.getElementById(id);
 const parentPanel=el("section");parentPanel.id="campaignSituations";parentPanel.hidden=true;node("workflowInspector").append(parentPanel);
 const parent=d.parentCampaign;
 const inParent=()=>typeof activeRegion!=="undefined"&&activeRegion?.id===parent.commandSnapshotId&&!!regionalState;
 let situationSelected=false,marker=null,markerTitle=null,markerStatus=null,markerCircle=null,markerTooltip=null;
 const situationLabelMaxKmPerPixel=0.10;
 const progress=()=>s.gameLaunched?"Game map opened":s.stage==="ready"?"Ready to launch":Object.keys(s.placements).length?"Deploying":"Not started";
 const svgNode=(tag,attrs,text)=>{const n=document.createElementNS("http://www.w3.org/2000/svg",tag);for(const [k,v] of Object.entries(attrs))n.setAttribute(k,String(v));if(text)n.textContent=text;return n;};
 const selectSituation=()=>{situationSelected=true;campaignFlow.renderParent();parentPanel.scrollIntoView?.({block:"nearest"});};
 const updateMarker=()=>{
  if(!marker){marker=svgNode("g",{id:"campaignSituationMarker",role:"button",tabindex:0,class:"campaign-situation-marker"});
   markerTooltip=svgNode("title",{});marker.append(markerTooltip);
   markerCircle=svgNode("circle",{r:15,fill:"#f7f5ee",stroke:"#4a6326","stroke-width":2});marker.append(markerCircle,svgNode("text",{x:0,y:5,"text-anchor":"middle","font-size":13,"font-weight":700,fill:"#1e2719"},designation));
   markerTitle=svgNode("text",{x:23,y:-3,"font-size":14,"font-weight":700,fill:"#1e2719","paint-order":"stroke",stroke:"#f7f5ee","stroke-width":4},"St. Lo · 29 June");markerStatus=svgNode("text",{x:23,y:15,"font-size":11,fill:"#1e2719","paint-order":"stroke",stroke:"#f7f5ee","stroke-width":3});marker.append(markerTitle,markerStatus);
   marker.addEventListener("click",e=>{e.stopPropagation();if(typeof dragged==="undefined"||!dragged)selectSituation();});marker.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();e.stopPropagation();selectSituation();}});node("map").append(marker);
  }
  marker.style.display=inParent()&&!campaignFlow.active&&!(typeof formationOpen!=="undefined"&&formationOpen)?"":"none";
  const scale=typeof view!=="undefined"?Math.max(view[2]/(node("map").clientWidth||1200),view[3]/(node("map").clientHeight||900)):1;
  const showLabels=scale<=situationLabelMaxKmPerPixel;
  markerTitle.style.display=showLabels?"":"none";markerStatus.style.display=showLabels?"":"none";
  markerTitle.setAttribute("data-situation-label","title");markerStatus.setAttribute("data-situation-label","status");
  markerTooltip.textContent="Situation "+designation+" · St. Lo · 29 June 1944\n"+progress()+"\nApproximate situation location. Select to review.";
  marker.setAttribute("transform",`translate(${d.parentMapLocation.x} ${d.parentMapLocation.y}) scale(${scale})`);marker.setAttribute("aria-pressed",String(situationSelected));marker.setAttribute("aria-label","Situation "+designation+", St. Lo, 29 June 1944. "+progress()+". Approximate situation location.");markerCircle.setAttribute("stroke",situationSelected?"#b77725":"#4a6326");markerCircle.setAttribute("stroke-width",situationSelected?3:2);markerStatus.textContent=progress();
 };
 const campaignFlow={active:false,stage:"prepare",reviewed:false,
  renderParent(){
   updateMarker();
   parentPanel.hidden=!inParent()||this.active;if(parentPanel.hidden)return;
   node("mapHeading").textContent=parent.title;
   node("breadcrumb").textContent="Whole Map / Western Europe / Normandy / 8-30 June 1944";
   node("workflowContext").textContent="Normandy / 8-30 June 1944 / Command snapshot: 8 June";
   parentPanel.replaceChildren();parentPanel.append(el("h2","Campaign Situations"),el("p","Normandy inland operations | 8-30 June 1944"),el("p","Select a Situation to review its card before opening its company/platoon map."));
   const entry=button(designation+" · St. Lo | 29 June 1944 | "+progress(),selectSituation);entry.setAttribute("data-situation-id",d.id);entry.setAttribute("aria-pressed",String(situationSelected));parentPanel.append(entry);
   if(situationSelected){const preview=el("section");preview.className="campaign-situation-preview";preview.setAttribute("aria-label","St. Lo Situation preview");preview.append(el("h3",designation+" · St. Lo"),el("p","29 June 1944 | "+progress()),el("p",d.victory[0]),el("p","German: "+d.totals.German+" counters | American: "+d.totals.Allied+" counters"),el("p",Object.keys(s.placements).length+" of "+d.instances.length+" deployed"),el("p","Approximate situation location. The original geomorphic boards do not define a geographic footprint."));
    const action=s.gameLaunched?"Resume game map":s.stage==="ready"?"Launch game map":Object.keys(s.placements).length?"Resume deployment":"Review Situation Card";
    const primary=button(action,()=>{this.choose();if(action!=="Review Situation Card"){this.openMap();if(s.stage==="ready"&&!s.gameLaunched)act(()=>{PanzerSituationModel.launch(s,d);mapMode="game";selected=null;});}});primary.className="primary";preview.append(primary);
    if(action!=="Review Situation Card")preview.append(button("Review Situation Card",()=>this.choose()));parentPanel.append(preview);
   }
   for(const card of regionalState.situations||[]){const start=card.campaignInterval?.start||"8 June 1944";parentPanel.append(button(card.title+" | "+start+" | Review card",()=>{if(typeof formationOpen!=="undefined"&&formationOpen)closeFormation();regionalHQ=card.parentFormationId;regionalMission=card.parentMissionId;renderRegional();showWorkflowStage("prepare");}));}
   parentPanel.append(el("p","Additional Situations created through battalion mission planning appear here. The 8 June command snapshot and each Situation retain their own dated forces; selecting a later card does not advance that snapshot."));
  },
  returnParent(){this.active=false;dialog.close();this.hide();if(typeof workflowIdentity!=="undefined")workflowIdentity=null;if(typeof renderWorkflow==="function"){renderWorkflow();showWorkflowStage("command");}this.renderParent();},
  hide(){cardPanel.hidden=true;},
  choose(){
   if(!inParent())return;
   if(typeof formationOpen!=="undefined"&&formationOpen)closeFormation();
   this.active=true;this.reviewed=false;this.stage="prepare";parentPanel.hidden=true;this.render();
  },
  showStage(stage){
   if(["choose","brief","command"].includes(stage)){this.returnParent();return;}
   if(!["brief","prepare","maneuver","review"].includes(stage))return;
   if(stage==="maneuver"&&!this.reviewed)return;
   if(stage!=="maneuver")dialog.close();this.stage=stage;this.render();if(stage==="maneuver")open();
  },
  openMap(){if(!this.active||this.stage!=="prepare")return;this.reviewed=true;this.showStage("maneuver");},
  render(){
   if(!this.active)return;updateMarker();if(typeof workflowStage!=="undefined")workflowStage=this.stage;
   node("shell").setAttribute("data-workflow-stage",this.stage);
   for(const id of ["workflowChoose","workflowBrief","preparationPanel"])node(id).hidden=true;
   for(const id of ["regionalWorkspace","formationControls","commandNavigation"])node(id).style.display="none";
   node("workflowContext").textContent="Normandy / Campaign Situations / St. Lo / 29 June 1944";
   node("workflowStatus").textContent="Campaign Situation Card "+designation+" | "+(s.stage==="ready"?"Both sides prepared":s.stage+" setup");
   const enabled={choose:true,brief:true,command:true,prepare:true,maneuver:this.reviewed,review:true};
   for(const stage of ["choose","brief","command","prepare","maneuver","review"]){const b=node("stage-"+stage);b.disabled=!enabled[stage];b.setAttribute("aria-current",stage===this.stage?"step":"false");b.title=stage==="command"?"Return to the Normandy parent campaign.":stage==="maneuver"&&!this.reviewed?"Review the Campaign Situation Card and open its map first.":"Open "+stage;}
   node("workflowHint").textContent=this.stage==="brief"?"Read the campaign briefing, then review its Situation Card.":this.stage==="prepare"?"This card defines the forces, joined boards, setup, objectives and turn limit for the campaign map.":this.stage==="maneuver"?s.gameLaunched?"Review the deployed situation on the game map.":"Place both sides in Campaign Map Setup, then launch the game map.":"Export or resume this campaign's saved setup.";
   cardPanel.hidden=false;cardPanel.replaceChildren();
   cardPanel.append(el("h2",this.stage==="brief"?"St. Lo campaign briefing":"Campaign Situation Card "+designation+": St. Lo"));
   cardPanel.append(el("p","29 June 1944 | 15 turns | Allies move first"),el("p",d.briefing));
   cardPanel.append(el("p","Source: Avalon Hill Panzer Leader, Situation 4. This Situation belongs to Normandy inland operations (8-30 June). Its 29 June roster does not replace the parent workspace's 8 June command snapshot."));
   if(this.stage==="brief")cardPanel.append(button("Review Campaign Situation Card",()=>this.showStage("prepare")));
   else{
    cardPanel.append(el("h3","Forces and setup"),el("p","German: "+d.totals.German+" counters, set up first on Board A. American: "+d.totals.Allied+" counters, set up second on Board C."));
    for(const side of ["German","Allied"])cardPanel.append(el("p",(side==="Allied"?"American":side)+": "+d.counters.filter(c=>c.side===side).map(c=>c.label+" x "+c.quantity).join(", ")));
    cardPanel.append(el("h3","Campaign map"),el("p","Boards A and C joined: A clockwise 90 degrees; C counter-clockwise 90 degrees. North is up. Company/platoon counters decompose into bounded ASL Scenario Cards."),el("h3","Objectives"));
    for(const text of d.victory)cardPanel.append(el("p",text));
    cardPanel.append(el("p","Special rules: "+d.specialRules));
    const source=el("details");source.append(el("summary","Original source card"));const scan=el("img");scan.src=base+"original-card.png";scan.alt="Panzer Leader Situation 4 source card";scan.className="panzer-card-scan";source.append(scan);cardPanel.append(source);
    cardPanel.append(el("p","Setup planning is available. Combat, victory adjudication and ASL admission remain unimplemented."));
    if(this.stage==="prepare")cardPanel.append(button(s.gameLaunched?"Resume game map":Object.keys(s.placements).length?"Resume map setup":"Set up campaign map",()=>this.openMap()));
    else cardPanel.append(button("Return to Campaign Situation Card",()=>this.showStage("prepare")));
    cardPanel.append(button("Export campaign plan",()=>download({package:d,plan:s},"st-lo-campaign-plan.json")));
   }
   cardPanel.append(button("Return to Normandy campaign",()=>this.returnParent()));
  }
 };
 window.CampaignSituationWorkflow=campaignFlow;
 dialog.addEventListener("close",()=>{if(campaignFlow.active&&campaignFlow.stage==="maneuver"){campaignFlow.returnParent();}});
 campaignFlow.renderParent();
 for(const id of ["workflowStart","workflowExplore"]){const previous=node(id).onclick;node(id).onclick=()=>{campaignFlow.active=false;campaignFlow.hide();dialog.close();previous?.();};}
})();
