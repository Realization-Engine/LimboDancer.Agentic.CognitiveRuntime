"use strict";
(()=>{
 const library=window.PANZER_SITUATION_LIBRARY||[window.PANZER_SITUATION_DATA];
 let d=window.PANZER_SITUATION_DATA,base=d.assetBase||"assets/panzer-leader-04/",key="campaign-atlas:"+d.id+":v2";
 let campaignRecord=window.CAMPAIGN_REGISTRY.campaigns.find(c=>c.id===d.campaignAssignment.campaignId);
 let situationRecord=campaignRecord.situations.find(c=>c.id===d.campaignAssignment.situationId),designation=String(situationRecord.number).padStart(2,"0");
 const sideLabel=side=>{if(side!=="Allied")return side;const names=[...new Set(d.counters.filter(c=>c.side===side).map(c=>({us:"American",uk:"British",ca:"Canadian",am:"Belgian"})[c.nationality]||"American"))];return names.length===1?names[0]:"Allied ("+names.join(" and ")+")";};
 const dateLabel=()=>d.situation.printedDate+(d.situation.printedEndDate?" to "+d.situation.printedEndDate:"");
 const returnLabel=()=>d.situation.number===4?"Return to Normandy campaign":"Return to "+d.parentCampaign.title;
 const setupSummary=()=>d.rules.setup.map(x=>x.side+": "+x.instructions).join(" ");
 let libraryContext=false,embedded=null;
 const el=(tag,text)=>{const n=document.createElement(tag);if(text!==undefined)n.textContent=text;return n;};
 const dialog=el("dialog");dialog.id="panzerSituation";dialog.setAttribute("aria-labelledby","panzerTitle");document.body.append(dialog);
 let s=PanzerSituationModel.create(d),selected=null,board="A",error="",selectedEngagement=new Set(),openTypes=new Set(),openSides=new Set(["German","Allied"]),terrainView=true,inspectedHex=null,hexDetails=false,boardZoom=1,resetMapScroll=false,spaceDown=false,suppressPanClick=false,activePan=null,mapMode="setup",rosterCollapsed=false,notesExpanded=false,handoffExpanded=false,bulkType=null,bulkHint=null;
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
 const save=()=>{try{if(embedded)embedded.save(JSON.parse(JSON.stringify(s)));else localStorage.setItem(key,JSON.stringify(s));}catch(e){error="Unable to save locally. Export your plan before closing.";}};
 const act=fn=>{try{error="";fn();save();}catch(e){error=e.message;}render();};
 const button=(label,fn)=>{const b=el("button",label);b.type="button";b.onclick=fn;return b;};
 const download=(obj,name)=>{const u=URL.createObjectURL(new Blob([JSON.stringify(obj,null,2)],{type:"application/json"})),a=el("a");a.href=u;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(u),1000);};
 function render(){
  const resetView=resetMapScroll;resetMapScroll=false;
  if(embedded)window.ArdennesWorkspaceHandle?.lockSetupTimeline?.(true);
  const game=mapMode==="game"&&s.stage==="ready";dialog.setAttribute("data-map-mode",game?"game":"setup");
  const oldY=dialog.scrollTop||0,oldTray=dialog.querySelector?.(".panzer-tray")?.scrollTop||0,oldX=dialog.querySelector?.(".panzer-joined-scroll")?.scrollLeft||0,oldMapY=dialog.querySelector?.(".panzer-joined-scroll")?.scrollTop||0;
  dialog.replaceChildren();const top=el("div");top.className="panzer-toolbar";const title=el("h2",d.situation.title+(game?" | Game map":" | Campaign Map Setup"));title.id="panzerTitle";top.append(title,button(returnLabel(),()=>campaignFlow.returnParent()),button("Clear this Situation",()=>{if(confirm("Clear all "+d.situation.title+" setup placements, notes and engagement drafts? Other Situations are unaffected.")){s=PanzerSituationModel.create(d);mapMode="setup";selected=null;selectedEngagement.clear();board=d.setupBoards[d.setupOrder[0]];error="";save();render();}}),button("Export Situation plan",()=>download({package:d,plan:s},d.id+"-plan.json")));if(game){top.children[2].hidden=true;top.append(button("Review deployment",()=>{mapMode="setup";render();}));}dialog.append(top);
  if(!game)dialog.append(el("p",dateLabel()+" (printed game date) | "+d.turnLimit+" turns | "+d.firstSide+" moves first. "+setupSummary()));
  const brief=el("details"),summary=el("summary","Campaign Situation Card, objectives and evidence");brief.append(summary,el("p",d.briefing));for(const v of d.victory)brief.append(el("p",v));brief.append(el("p","Special rules: "+d.specialRules));const scan=el("img");scan.src=base+(d.sourceCardImage||"original-card.png");scan.alt="Original Situation "+d.situation.number+" card, including orientation, forces, setup and victory conditions";scan.className="panzer-card-scan";brief.append(scan);dialog.append(brief);
  if(!game)dialog.append(el("p","Data admission: "+d.admission.status+". Board "+board+": "+d.boards.find(b=>b.id===board).grid.coverage+"; "+d.boards.find(b=>b.id===board).hexes.length+" transcribed hexes. First-pass terrain requires review before combat admission."));
  if(!game)dialog.append(el("p","Setup planning workspace. Boards are displayed in Situation orientation. Counters snap to interior setup hexes. The JSON terrain view is a first-pass transcription; combat and victory adjudication are not implemented. Earlier freehand plans are retained separately."));
  const notice=el("p",error);notice.setAttribute("role","alert");notice.hidden=!error;dialog.append(notice);
  const controls=el("div");controls.className="panzer-toolbar panzer-setup-controls";
  if(!game){const launch=button(s.gameLaunched?"Resume game map":"Launch situation game map",()=>act(()=>{PanzerSituationModel.launch(s,d);mapMode="game";selected=null;boardZoom=1;stopPan();}));launch.disabled=s.stage!=="ready";launch.className="primary";controls.append(launch);}
  if(d.airSupport){const support=el("details"),head=el("summary","Air support | "+d.airSupport.groups.reduce((n,g)=>n+g.instanceIds.length,0)+" aircraft");support.className="panzer-air-support";support.append(head,el("p",d.airSupport.instructions),el("p","Support roster only. Aircraft stay off the ground setup map; arrival, flight activation and air attacks are not implemented."));for(const g of d.airSupport.groups){const row=el("section");row.append(el("h4",g.id+" | "+g.role+" | "+g.instanceIds.length+" aircraft | earliest turn "+g.availableFromTurn));const i=d.instances.find(i=>i.id===g.instanceIds[0]),type=d.counters.find(c=>c.id===i.counterTypeId);const art=CampaignAppearance.counterImage(base+type.artwork,type.side,type.label,type.nationality);art.style.width="64px";art.style.height="64px";row.append(art,el("p",type.label+": "+g.instanceIds.join(", ")));support.append(row);}dialog.append(support);}
  if(!game&&s.stage!=="ready"){const next=button("Complete "+s.stage+" setup",()=>act(()=>{PanzerSituationModel.next(s,d);PanzerSituationModel.autoDeploySide(s,d);selected=null;board=d.setupBoards[s.stage]||d.boards[0].id;}));next.disabled=!PanzerSituationModel.complete(s,d,s.stage);controls.prepend(next);}
  const detailLabel=el("label"),detailToggle=el("input");detailToggle.type="checkbox";detailToggle.checked=hexDetails;detailToggle.disabled=!(game||terrainView);detailToggle.onchange=()=>{hexDetails=detailToggle.checked;render();};detailLabel.append(detailToggle,el("span","Show hex details"));controls.append(detailLabel);dialog.append(controls);
  const sourceControl=document.getElementById("situationSourceArtwork");if(sourceControl){sourceControl.hidden=game;sourceControl.textContent=terrainView?"Show source artwork":"Show generated terrain";sourceControl.setAttribute("aria-pressed",String(!terrainView));}
  if(bulkType){const hint=el("span","Choose a starting hex for remaining "+d.counters.find(c=>c.id===bulkType).label+" units.");controls.append(hint,button("Cancel",()=>{bulkType=null;bulkHint=null;selected=null;render();}));}
  const inspected=d.boards.find(b=>b.id===board).hexes.find(h=>h.id===inspectedHex);
  dialog.append(el("p",inspected?inspected.label+": "+inspected.terrain.base+"; "+(inspected.terrain.features.join(", ")||"no additional feature recorded")+"; elevation category "+inspected.terrain.elevationLevel+". "+(inspected.setupAllowed?"Interior setup hex.":"Boundary fragment; setup excluded.")+" Evidence: "+inspected.terrain.evidence.status:game?"Click a hex or counter to inspect the deployed situation.":"Click a hex to inspect its transcription. With a counter selected, the click also places it."));
  const layout=el("div");layout.className="panzer-layout";const tray=el("section");tray.className="panzer-tray";tray.setAttribute("aria-label","Situation counter roster");
  for(const side of d.setupOrder){const sideGroup=el("details");sideGroup.className="panzer-side-group";sideGroup.setAttribute("data-side",side);sideGroup.open=openSides.has(side);sideGroup.ontoggle=()=>{if(sideGroup.isConnected===false)return;if(sideGroup.open)openSides.add(side);else openSides.delete(side);};sideGroup.append(el("summary",sideLabel(side)+" ("+PanzerSituationModel.ground(d).filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side===side).length+(d.airSupport?" ground pieces)":")")));tray.append(sideGroup);for(const type of d.counters.filter(t=>t.side===side&&t.kind!=="aircraft")){const group=el("details");group.open=openTypes.has(type.id);group.ontoggle=()=>{if(group.isConnected===false)return;if(group.open)openTypes.add(type.id);else openTypes.delete(type.id);};const heading=el("summary",type.label+" x "+type.quantity),placedCount=d.instances.filter(i=>i.counterTypeId===type.id&&s.placements[i.id]).length,badge=el("span",placedCount+" placed");badge.className="panzer-placed-count";badge.setAttribute("data-complete",String(placedCount===type.quantity));badge.setAttribute("data-counter-type",type.id);badge.setAttribute("aria-label",placedCount+" of "+type.quantity+" placed");heading.append(badge);group.append(heading);const art=CampaignAppearance.counterImage(base+type.artwork,type.side,type.label,type.nationality);const pick=button("",()=>{bulkType=null;bulkHint=null;const candidates=d.instances.filter(i=>i.counterTypeId===type.id);selected=(candidates.find(i=>!s.placements[i.id])||candidates[0]).id;selected=(d.deployment?.loads||[]).find(l=>l.passengerId===selected)?.carrierId||selected;board=PanzerSituationModel.boards(d,selected)[0];error="";render();});pick.setAttribute("aria-label","Select "+type.label+" counter");pick.disabled=s.stage!==side;pick.append(art);group.append(pick);if(type.factors){const factors=el("table");factors.className="panzer-unit-factors";factors.setAttribute("aria-label",type.label+" unit factors");const body=el("tbody"),labels={attack:"Attack",weaponType:"Weapon type",range:"Range",defense:"Defense",movement:"Movement"};for(const [key,value] of Object.entries(type.factors)){const row=el("tr"),label=el("th",labels[key]||key);label.setAttribute("scope","row");row.append(label,el("td",String(value)));body.append(row);}factors.append(body);group.append(factors);}else{const note=el("p",type.kind==="bridge-equipment"?"Un-emplaced bridge equipment. No combat factors; construction is not implemented.":"Positional obstacle. No combat factors; not a combat unit.");note.className="panzer-unit-note";group.append(note);}for(const text of type.notes){const note=el("small",text);note.className="panzer-unit-note";group.append(note);}const typeTools=el("div");typeTools.className="panzer-type-tools";
 const deploy=button("\u25ce",()=>{try{const pending=PanzerSituationModel.bulkUnits(s,d,type.id).filter(i=>!s.placements[i.id]);if(!pending.length)return;if(pending.every(i=>d.deployment?.allowedHexes[i.id]?.length)){act(()=>{PanzerSituationModel.autoDeploy(s,d,type.id);bulkType=null;bulkHint=null;selected=null;});return;}selected=pending[0].id;bulkType=type.id;bulkHint=null;for(const bd of d.boards){if(!PanzerSituationModel.boards(d,selected).includes(bd.id))continue;const h=bd.hexes.find(h=>PanzerSituationModel.allowed(d,selected,bd.id,h.id,s));if(h){bulkHint={board:bd.id,hexId:h.id};board=bd.id;break;}}if(!bulkHint)throw Error("No legal starting hex is available.");error="";render();dialog.querySelector?.("[data-bulk-origin]")?.scrollIntoView?.({block:"nearest",inline:"nearest"});}catch(e){bulkType=null;error=e.message;render();}});
 const clear=button("\u232b",()=>act(()=>{PanzerSituationModel.clearType(s,d,type.id);bulkType=null;bulkHint=null;selected=null;}));
 for(const [control,name]of [[deploy,"Deploy All"],[clear,"Clear All"]]){control.title=name+" - "+type.label;control.setAttribute("aria-label",name+" "+type.label);typeTools.append(control);}
 let restriction="";try{PanzerSituationModel.bulkUnits(s,d,type.id);}catch(e){restriction=e.message;}
 if(d.instances.filter(i=>i.counterTypeId===type.id).every(i=>d.deployment?.allowedHexes[i.id]?.length))deploy.title="Deploy All - "+type.label+": automatically use prescribed setup areas";deploy.disabled=!!restriction||placedCount===type.quantity;clear.disabled=!!restriction||placedCount===0;if(restriction){deploy.title=restriction;clear.title=restriction;}group.append(typeTools);
 for(const i of d.instances.filter(i=>i.counterTypeId===type.id)){const b=button(i.id+(s.placements[i.id]?" (placed)":""),()=>{bulkType=null;bulkHint=null;selected=(s.stage!=="ready"?(d.deployment?.loads||[]).find(l=>l.passengerId===i.id)?.carrierId:null)||i.id;error="";if(s.stage==="ready"){if(selectedEngagement.has(i.id))selectedEngagement.delete(i.id);else selectedEngagement.add(i.id);}else board=PanzerSituationModel.boards(d,selected)[0];render();});b.disabled=s.stage!=="ready"&&s.stage!==side;b.setAttribute("aria-pressed",String(s.stage==="ready"?selectedEngagement.has(i.id):selected===i.id));group.append(b);}sideGroup.append(group);}}
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
  const joined=el("div");joined.className="panzer-joined";joined.setAttribute("data-axis",d.illustration.layoutAxis||"horizontal");joined.setAttribute("data-detail",String(boardZoom>1.4));joined.style.width=(boardZoom*100)+"%";
  for(const placement of d.illustration.joinedLayout){
   const boardId=placement.boardId,rotation=placement.clockwiseDegrees,bd=d.boards.find(b=>b.id===boardId);
   const sheet=el("section");sheet.className="panzer-sheet";
   sheet.setAttribute("aria-label","Board "+boardId);
   const map=el("div");map.className="panzer-board panzer-board-joined";
   const svg=document.createElementNS("http://www.w3.org/2000/svg","svg");const portrait=rotation%180===0;map.style.aspectRatio=portrait?"1130 / 3138":"3138 / 1130";svg.setAttribute("viewBox",portrait?"0 0 1130 3138":"0 0 3138 1130");svg.setAttribute("aria-label","Joined Situation board "+boardId);svg.setAttribute("class","panzer-joined-svg");
   const drawing=document.createElementNS("http://www.w3.org/2000/svg","g");drawing.setAttribute("transform",rotation===0?"translate(0 0)":rotation===180?"translate(1130 3138) rotate(180)":rotation===90?"translate(3138 0) rotate(90)":"translate(0 1130) rotate(-90)");
   if(game||terrainView){PanzerMapArt.draw(drawing,d,bd,board===boardId?inspectedHex:null,hexDetails,true);CampaignAppearance.terrain(drawing);}
   else{const img=document.createElementNS("http://www.w3.org/2000/svg","image");img.setAttribute("href",base+"board-"+boardId+".png");img.setAttribute("width","1130");img.setAttribute("height","3138");drawing.append(img);}
   if(!game&&selected&&s.stage!=="ready"&&d.deployment){for(const h of bd.hexes.filter(h=>PanzerSituationModel.allowed(d,selected,boardId,h.id,s))){const zone=document.createElementNS("http://www.w3.org/2000/svg","polygon");zone.setAttribute("points",h.imagePolygon.map(p=>[p.u*1130,p.v*3138].join(",")).join(" "));zone.setAttribute("fill","#b77725");zone.setAttribute("fill-opacity","0.14");zone.setAttribute("stroke","#b77725");zone.setAttribute("stroke-width","2");zone.setAttribute("pointer-events","none");zone.setAttribute("data-setup-zone",selected);drawing.append(zone);}}
   if(bulkHint?.board===boardId){const h=bd.hexes.find(h=>h.id===bulkHint.hexId);if(h){const hint=document.createElementNS("http://www.w3.org/2000/svg","polygon");hint.setAttribute("points",h.imagePolygon.map(p=>[p.u*1130,p.v*3138].join(" ")).join(" "));hint.setAttribute("fill","#b77725");hint.setAttribute("fill-opacity",".55");hint.setAttribute("stroke","#8a3b2a");hint.setAttribute("stroke-width","8");hint.setAttribute("pointer-events","none");hint.setAttribute("data-bulk-origin",h.id);drawing.append(hint);}}
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

   },{passive:false});
   map.onclick=e=>{if(blockPanClick(e))return;let point;const matrix=drawing.getScreenCTM?.();if(matrix&&svg.createSVGPoint){const cursor=svg.createSVGPoint();cursor.x=e.clientX;cursor.y=e.clientY;const local=cursor.matrixTransform(matrix.inverse());point={u:local.x/1130,v:local.y/3138};}else{const rect=svg.getBoundingClientRect();point=PanzerMapArt.unproject(rotation,(e.clientX-rect.left)/rect.width,(e.clientY-rect.top)/rect.height);}board=boardId;const h=PanzerSituationModel.hexAt(d,boardId,point.u,point.v);inspectedHex=h?.id||null;if(!game&&selected&&s.stage!=="ready")act(()=>{if(bulkType){PanzerSituationModel.deployType(s,d,bulkType,boardId,h?.id);bulkType=null;bulkHint=null;selected=null;}else PanzerSituationModel.place(s,d,selected,boardId,point.u,point.v);});else render();};
   for(const [id,p] of Object.entries(s.placements)){if(p.board!==boardId)continue;const instance=d.instances.find(i=>i.id===id),token=button(id,()=>{}),position=PanzerMapArt.project(rotation,p.x,p.y);token.onclick=e=>{if(blockPanClick(e))return;e.stopPropagation();if(!game&&selected&&selected!==id&&s.stage!=="ready"){board=boardId;inspectedHex=p.hexId;act(()=>{if(bulkType){PanzerSituationModel.deployType(s,d,bulkType,boardId,p.hexId);bulkType=null;bulkHint=null;selected=null;}else PanzerSituationModel.place(s,d,selected,boardId,p.x,p.y);});}else{selected=id;board=boardId;render();}};let counterDrag=null;
 token.addEventListener("pointerdown",ev=>{if(ev.button!==0||spaceDown||game||s.stage!==d.counters.find(c=>c.id===instance.counterTypeId).side)return;ev.preventDefault();ev.stopPropagation();counterDrag={x:ev.clientX,y:ev.clientY,moved:false,left:parseFloat(token.style.left),top:parseFloat(token.style.top)};token.setPointerCapture(ev.pointerId);});
 token.addEventListener("pointermove",ev=>{if(!counterDrag)return;const dx=ev.clientX-counterDrag.x,dy=ev.clientY-counterDrag.y;if(!counterDrag.moved&&Math.hypot(dx,dy)<4)return;counterDrag.moved=true;const rect=map.getBoundingClientRect();token.style.left=(counterDrag.left+dx/rect.width*100)+"%";token.style.top=(counterDrag.top+dy/rect.height*100)+"%";});
 token.addEventListener("pointerup",ev=>{if(!counterDrag)return;const moved=counterDrag.moved;counterDrag=null;if(!moved)return;ev.preventDefault();ev.stopPropagation();suppressPanClick=true;bulkType=null;bulkHint=null;selected=id;const point=svg.createSVGPoint();point.x=ev.clientX;point.y=ev.clientY;const local=point.matrixTransform(drawing.getScreenCTM().inverse());act(()=>PanzerSituationModel.place(s,d,id,boardId,local.x/1130,local.y/3138));});
 token.addEventListener("pointercancel",()=>{if(counterDrag){counterDrag=null;render();}});
 token.className="panzer-token";token.style.left=position.x*100+"%";token.style.top=position.y*100+"%";token.title=id;token.setAttribute("aria-pressed",String(selected===id));token.setAttribute("aria-label",id);token.textContent="";const art=CampaignAppearance.counterImage(base+instance.artwork,d.counters.find(c=>c.id===instance.counterTypeId).side,id,d.counters.find(c=>c.id===instance.counterTypeId).nationality);token.append(art);map.append(token);}
   sheet.append(map);joined.append(sheet);
  }
  const boardLegend=el("span",d.illustration.joinedLayout.map(p=>"Board "+p.boardId).join(d.illustration.layoutAxis==="vertical"?" / ":" | "));boardLegend.className="panzer-board-legend";boardLegend.title=(d.illustration.layoutAxis==="vertical"?"Top to bottom: ":"Left to right: ")+d.illustration.joinedLayout.map(p=>p.boardId+" ("+p.clockwiseDegrees+" degrees clockwise)").join(", ")+". "+setupSummary();controls.append(boardLegend);
  viewport.append(joined);if(!game){
   tray.id="situationCounterRoster";tray.hidden=rosterCollapsed;layout.setAttribute("data-roster-collapsed",String(rosterCollapsed));
   const toggle=button("\u2630",()=>{rosterCollapsed=!rosterCollapsed;tray.hidden=rosterCollapsed;layout.setAttribute("data-roster-collapsed",String(rosterCollapsed));toggle.setAttribute("aria-expanded",String(!rosterCollapsed));toggle.title=rosterCollapsed?"Show counter roster":"Hide counter roster";toggle.setAttribute("aria-label",toggle.title);});
   toggle.className="panzer-roster-toggle";toggle.setAttribute("aria-controls",tray.id);toggle.setAttribute("aria-expanded",String(!rosterCollapsed));toggle.title=rosterCollapsed?"Show counter roster":"Hide counter roster";toggle.setAttribute("aria-label",toggle.title);controls.prepend(toggle);layout.append(tray);
  }layout.append(viewport);dialog.append(layout);
  if(game){dialog.scrollTop=oldY;viewport.scrollLeft=resetView?0:oldX;viewport.scrollTop=resetView?0:oldMapY;return;}
  dialog.append(el("p","Board layout follows this Situation Card. Use the wheel to zoom; Space plus left drag or middle drag to pan. Scroll out to restore the overview. Highlighted hexes show the selected unit's permitted setup zone. Terrain and cross-board movement require review before combat admission."));
  const noteLabel=el("label","Situation planning notes"),notes=el("textarea");notes.id="panzerNotes";noteLabel.htmlFor=notes.id;notes.value=s.notes;notes.onchange=()=>{s.notes=notes.value;save();};const notesGroup=el("details");notesGroup.className="panzer-planning-notes";notesGroup.open=notesExpanded;notesGroup.append(el("summary","Situation planning notes"),noteLabel,notes);notesGroup.ontoggle=()=>{if(notesGroup.isConnected===false)return;notesExpanded=notesGroup.open;};dialog.append(notesGroup);
  const intentLabel=el("label","Local objective for the selected counters"),intent=el("textarea");intent.id="panzerIntent";intentLabel.htmlFor=intent.id;intent.placeholder="Describe the local objective for this engagement draft.";
  const draft=button("Create ASL engagement draft",()=>act(()=>PanzerSituationModel.draft(s,d,[...selectedEngagement],intent.value)));draft.disabled=s.stage!=="ready";const handoff=el("details");handoff.className="panzer-asl-handoff";handoff.open=handoffExpanded;handoff.ontoggle=()=>{if(handoff.isConnected===false)return;handoffExpanded=handoff.open;};handoff.append(el("summary","ASL Scenario Card handoff"),el("p",selectedEngagement.size+" counters selected. Select participants from both sides in the roster. Each draft holds those counters; their squad-level composition still needs to be resolved."),intentLabel,intent,draft);dialog.append(handoff);
  dialog.scrollTop=oldY;tray.scrollTop=oldTray;viewport.scrollLeft=resetView?0:oldX;viewport.scrollTop=resetView?0:oldMapY;
  for(const e of s.engagements)handoff.append(button("Export blocked draft: "+e.intent,()=>download(e,e.id.replaceAll(":","-")+".json")));
 }
 const open=()=>{if(!campaignFlow.active||!campaignFlow.reviewed)return;try{PanzerSituationModel.autoDeploySide(s,d);save();}catch(e){error=e.message;}mapMode=s.gameLaunched?"game":"setup";board=d.setupBoards[s.stage]||d.boards[0].id;render();if(embedded){embedded.host.hidden=false;dialog.setAttribute("open","");}else dialog.showModal();};
 const cardPanel=el("section");cardPanel.id="campaignSituationStep";cardPanel.hidden=true;document.getElementById("workflowInspector").append(cardPanel);
 const node=id=>document.getElementById(id);
 const parentPanel=el("section");parentPanel.id="campaignSituations";parentPanel.hidden=true;node("workflowInspector").append(parentPanel);
 let parent=d.parentCampaign;
 const inParent=()=>libraryContext||(typeof activeRegion!=="undefined"&&activeRegion?.id===parent.commandSnapshotId&&!!regionalState);
 let situationSelected=false,marker=null,markerTitle=null,markerStatus=null,markerCircle=null,markerTooltip=null;
 const situationLabelMaxKmPerPixel=0.10;
 const progress=()=>s.gameLaunched?"Game map opened":s.stage==="ready"?"Ready to launch":Object.keys(s.placements).length?"Deploying":"Not started";
 const svgNode=(tag,attrs,text)=>{const n=document.createElementNS("http://www.w3.org/2000/svg",tag);for(const [k,v] of Object.entries(attrs))n.setAttribute(k,String(v));if(text)n.textContent=text;return n;};
 const selectPackage=id=>{
  bulkType=null;bulkHint=null;const next=library.find(x=>x.id===id);if(!next)throw Error("Unknown Situation package.");
  stopPan();d=next;base=d.assetBase||"assets/panzer-leader-04/";key="campaign-atlas:"+d.id+":v2";parent=d.parentCampaign;
  campaignRecord=window.CAMPAIGN_REGISTRY.campaigns.find(c=>c.id===d.campaignAssignment.campaignId);situationRecord=campaignRecord.situations.find(x=>x.sourcePackageId===d.id);designation=String(situationRecord.number).padStart(2,"0");
  s=PanzerSituationModel.create(d);error="";try{const raw=localStorage.getItem(key);if(raw)s=PanzerSituationModel.validate(JSON.parse(raw),d);}catch(e){error="Saved plan could not be loaded: "+e.message;}
  selected=null;inspectedHex=null;selectedEngagement.clear();openTypes.clear();board=d.setupBoards[s.stage]||d.boards[0].id;boardZoom=1;resetMapScroll=true;mapMode="setup";situationSelected=true;libraryContext=true;
 };
 const selectSituation=()=>{situationSelected=true;campaignFlow.renderParent();parentPanel.scrollIntoView?.({block:"nearest"});};
 const updateMarker=()=>{
  if(!d.parentMapLocation){if(marker)marker.style.display="none";return;}
  if(!marker){marker=svgNode("g",{id:"campaignSituationMarker",role:"button",tabindex:0,class:"campaign-situation-marker"});
   markerTooltip=svgNode("title",{});marker.append(markerTooltip);
   markerCircle=svgNode("circle",{r:15,fill:"#f7f5ee",stroke:"#4a6326","stroke-width":2});marker.append(markerCircle,svgNode("text",{x:0,y:5,"text-anchor":"middle","font-size":13,"font-weight":700,fill:"#1e2719"},designation));
   markerTitle=svgNode("text",{x:23,y:-3,"font-size":14,"font-weight":700,fill:"#1e2719","paint-order":"stroke",stroke:"#f7f5ee","stroke-width":4},d.situation.title+" \u00b7 "+dateLabel());markerStatus=svgNode("text",{x:23,y:15,"font-size":11,fill:"#1e2719","paint-order":"stroke",stroke:"#f7f5ee","stroke-width":3});marker.append(markerTitle,markerStatus);
   marker.addEventListener("click",e=>{e.stopPropagation();if(typeof dragged==="undefined"||!dragged)selectSituation();});marker.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();e.stopPropagation();selectSituation();}});node("map").append(marker);
  }
  marker.style.display=inParent()&&!campaignFlow.active&&!(typeof formationOpen!=="undefined"&&formationOpen)?"":"none";
  const scale=typeof view!=="undefined"?Math.max(view[2]/(node("map").clientWidth||1200),view[3]/(node("map").clientHeight||900)):1;
  const showLabels=scale<=situationLabelMaxKmPerPixel;
  markerTitle.style.display=showLabels?"":"none";markerStatus.style.display=showLabels?"":"none";
  markerTitle.setAttribute("data-situation-label","title");markerStatus.setAttribute("data-situation-label","status");
  markerTooltip.textContent="Situation "+designation+" \u00b7 "+d.situation.title+" \u00b7 "+dateLabel()+"\n"+progress()+"\nApproximate situation location. Select to review.";
  marker.setAttribute("transform",`translate(${d.parentMapLocation.x} ${d.parentMapLocation.y}) scale(${scale})`);marker.setAttribute("aria-pressed",String(situationSelected));marker.setAttribute("aria-label","Situation "+designation+", "+d.situation.title+", "+dateLabel()+". "+progress()+". Approximate situation location.");markerCircle.setAttribute("stroke",situationSelected?"#b77725":"#4a6326");markerCircle.setAttribute("stroke-width",situationSelected?3:2);markerStatus.textContent=progress();
 };
 const campaignFlow={active:false,stage:"prepare",reviewed:false,
  renderParent(){
   updateMarker();
   parentPanel.hidden=!inParent()||this.active;if(parentPanel.hidden)return;
   node("mapHeading").textContent=d.parentMapLocation?parent.title:"Campaign Atlas | Reference geography";
   node("breadcrumb").textContent=d.parentMapLocation?"Whole Map / Western Europe / "+parent.title:"Situation library / "+parent.title+" / battlefield available in setup";
   node("workflowContext").textContent=parent.title+" / "+parent.startDate+" to "+parent.endDate;
   if(libraryContext){
    node("workflowChoose").hidden=true;node("workflowBrief").hidden=true;
    for(const id of ["regionalWorkspace","formationControls","commandNavigation"])node(id).style.display="none";
    node("shell").setAttribute("data-workflow-stage","command");
    node("workflowStatus").textContent="Choose a Campaign Situation";
    node("workflowHint").textContent="Select a card, review its deployment rules, then open map setup.";
    for(const stage of ["choose","brief","command","prepare","maneuver","review"]){node("stage-"+stage).disabled=stage!=="choose";node("stage-"+stage).setAttribute("aria-current","false");}
   }
   parentPanel.replaceChildren();parentPanel.append(el("h2","Campaign Situations"),el("p",parent.title+" | "+parent.startDate+" to "+parent.endDate),el("p","Select a Situation to review its card before opening its company/platoon map."));
   const switchLabel=el("label","Campaign collection"),switcher=el("select");switcher.id="situationCampaignChoice";switchLabel.htmlFor=switcher.id;
   for(const c of window.CAMPAIGN_REGISTRY.campaigns){const option=el("option",c.title+" | "+c.startDate+" to "+c.endDate);option.value=c.id;switcher.append(option);}switcher.value=campaignRecord.id;
   switcher.onchange=()=>{const c=window.CAMPAIGN_REGISTRY.campaigns.find(c=>c.id===switcher.value);selectPackage(c.situations[0].sourcePackageId);situationSelected=false;this.renderParent();};parentPanel.append(switchLabel,switcher);if(libraryContext)parentPanel.append(button("Return to campaign selection",leaveLibrary));
   for(const record of campaignRecord.situations){const pkg=library.find(x=>x.id===record.sourcePackageId);const entry=button(String(record.number).padStart(2,"0")+" \u00b7 "+record.title+" | "+pkg.situation.printedDate+(pkg.id===d.id?" | "+progress():""),()=>{selectPackage(pkg.id);selectSituation();});entry.setAttribute("data-situation-id",pkg.id);entry.setAttribute("aria-pressed",String(situationSelected&&pkg.id===d.id));parentPanel.append(entry);}
   if(situationSelected){const preview=el("section");preview.className="campaign-situation-preview";preview.setAttribute("aria-label",d.situation.title+" Situation preview");preview.append(el("h3",designation+" \u00b7 "+d.situation.title),el("p",dateLabel()+" | "+progress()),el("p",d.victory[0]),el("p","German: "+d.totals.German+" counters | Allied: "+d.totals.Allied+" counters"),el("p",Object.keys(s.placements).length+" of "+PanzerSituationModel.ground(d).length+(d.airSupport?" ground pieces deployed":" deployed")),el("p","Legacy boards describe a local battlefield, not a surveyed geographic footprint. This is a dated Situation collection; no additional historical command snapshot is asserted."));
    const action=s.gameLaunched?"Resume game map":s.stage==="ready"?"Launch game map":Object.keys(s.placements).length?"Resume deployment":"Review Situation Card";
    const primary=button(action,()=>{this.choose();if(action!=="Review Situation Card"){this.openMap();if(s.stage==="ready"&&!s.gameLaunched)act(()=>{PanzerSituationModel.launch(s,d);mapMode="game";selected=null;});}});primary.className="primary";preview.append(primary);
    if(action!=="Review Situation Card")preview.append(button("Review Situation Card",()=>this.choose()));parentPanel.append(preview);
   }
   for(const card of ((typeof activeRegion!=="undefined"&&activeRegion?.id===parent.commandSnapshotId&&typeof regionalState!=="undefined"&&regionalState?.situations)||[])){const start=card.campaignInterval?.start||"8 June 1944";parentPanel.append(button(card.title+" | "+start+" | Review card",()=>{if(typeof formationOpen!=="undefined"&&formationOpen)closeFormation();regionalHQ=card.parentFormationId;regionalMission=card.parentMissionId;renderRegional();showWorkflowStage("prepare");}));}
   parentPanel.append(el("p","Each Situation retains its own dated forces and saved deployment. Selecting a card does not advance another command snapshot or transfer its troops."));
  },
  returnParent(){if(embedded){const exit=embedded.exit;campaignFlow.active=false;dialog.removeAttribute("open");dialog.hidden=true;cardPanel.hidden=true;embedded=null;exit();return;}if(window.ARDENNES_COMMAND_PACKAGE_ID===d.id){location.href="index.html?exercise=ardennes";return;}this.active=false;dialog.close();this.hide();if(typeof workflowIdentity!=="undefined")workflowIdentity=null;if(typeof renderWorkflow==="function"){renderWorkflow();showWorkflowStage("command");}this.renderParent();},
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
   if(stage!=="maneuver"){if(embedded){window.ArdennesWorkspaceHandle?.lockSetupTimeline?.(false);dialog.removeAttribute("open");embedded.host.hidden=true;}else dialog.close();}this.stage=stage;this.render();if(stage==="maneuver")open();
  },
  openMap(){if(!this.active||this.stage!=="prepare")return;this.reviewed=true;this.showStage("maneuver");},
  render(){
   if(!this.active)return;if(embedded){embedded.cards.append(cardPanel);embedded.host.hidden=this.stage!=="maneuver";}updateMarker();if(typeof workflowStage!=="undefined")workflowStage=this.stage;
   node("shell").setAttribute("data-workflow-stage",this.stage);
   for(const id of ["workflowChoose","workflowBrief","preparationPanel"])node(id).hidden=true;
   for(const id of ["regionalWorkspace","formationControls","commandNavigation"])node(id).style.display="none";
   node("workflowContext").textContent=parent.title+" / Campaign Situations / "+d.situation.title+" / "+dateLabel();
   node("workflowStatus").textContent="Campaign Situation Card "+designation+" | "+(s.stage==="ready"?"Both sides prepared":s.stage+" setup");
   const enabled={choose:true,brief:true,command:true,prepare:true,maneuver:this.reviewed,review:true};
   for(const stage of ["choose","brief","command","prepare","maneuver","review"]){const b=node("stage-"+stage);b.disabled=!enabled[stage];b.setAttribute("aria-current",stage===this.stage?"step":"false");b.title=stage==="command"?"Return to the parent campaign.":stage==="maneuver"&&!this.reviewed?"Review the Campaign Situation Card and open its map first.":"Open "+stage;}
   node("workflowHint").textContent=this.stage==="brief"?"Read the campaign briefing, then review its Situation Card.":this.stage==="prepare"?"This card defines the forces, joined boards, setup, objectives and turn limit for the campaign map.":this.stage==="maneuver"?s.gameLaunched?"Review the deployed situation on the game map.":"Place both sides in Campaign Map Setup, then launch the game map.":"Export or resume this campaign's saved setup.";
   cardPanel.hidden=false;cardPanel.replaceChildren();
   cardPanel.append(el("h2",this.stage==="brief"?d.situation.title+" briefing":"Campaign Situation Card "+designation+": "+d.situation.title));
   if(mapMode!=="game"){const artwork=button(terrainView?"Show source artwork":"Show generated terrain",()=>{terrainView=!terrainView;if(this.stage!=="maneuver")this.openMap();else render();});artwork.id="situationSourceArtwork";artwork.setAttribute("aria-pressed",String(!terrainView));cardPanel.append(artwork);}
   cardPanel.append(el("p",dateLabel()+" | "+d.turnLimit+" turns | "+d.firstSide+" moves first"),el("p",d.briefing));
   cardPanel.append(el("p","Source: Avalon Hill Panzer Leader, Situation "+d.situation.number+". Parent campaign: "+parent.title+". Dated source forces do not replace a command exercise snapshot."));
   if(this.stage==="brief")cardPanel.append(button("Review Campaign Situation Card",()=>this.showStage("prepare")));
   else{
    cardPanel.append(el("h3","Forces and setup"),el("p","German: "+d.totals.German+" counters. Allied: "+d.totals.Allied+" counters. "+setupSummary()));
    for(const side of d.setupOrder)cardPanel.append(el("p",sideLabel(side)+": "+d.counters.filter(c=>c.side===side).map(c=>c.label+" x "+c.quantity).join(", ")));
    cardPanel.append(el("h3","Campaign map"),el("p",d.illustration.joinedLayout.map(x=>"Board "+x.boardId+": "+x.clockwiseDegrees+" degrees clockwise").join("; ")+". North is up. Company/platoon counters decompose into bounded ASL Scenario Cards."),el("h3","Objectives"));
    for(const text of d.victory)cardPanel.append(el("p",text));
    cardPanel.append(el("p","Special rules: "+d.specialRules));
    const source=el("details");source.append(el("summary","Original source card"));const scan=el("img");scan.src=base+(d.sourceCardImage||"original-card.png");scan.alt="Panzer Leader Situation "+d.situation.number+" source card";scan.className="panzer-card-scan";source.append(scan);cardPanel.append(source);
    cardPanel.append(el("p","Setup planning is available. Combat, victory adjudication and ASL admission remain unimplemented."));
    if(this.stage==="prepare")cardPanel.append(button(s.gameLaunched?"Resume game map":Object.keys(s.placements).length?"Resume map setup":"Set up campaign map",()=>this.openMap()));
    else cardPanel.append(button("Return to Campaign Situation Card",()=>this.showStage("prepare")));
    cardPanel.append(button("Export campaign plan",()=>download({package:d,plan:s},d.id+"-campaign-plan.json")));
   }
   cardPanel.append(button(returnLabel(),()=>this.returnParent()));if(embedded)embedded.cards.scrollTop=0;
  }
 };
 campaignFlow.selectPackage=id=>{selectPackage(id);campaignFlow.renderParent();};
 campaignFlow.selectedPackage=()=>d.id;
 const leaveLibrary=()=>{libraryContext=false;campaignFlow.active=false;campaignFlow.hide();parentPanel.hidden=true;dialog.close();if(typeof workflowStage!=="undefined")workflowStage="choose";if(typeof renderWorkflow==="function")renderWorkflow();};
 const previousChoose=node("stage-choose").onclick;node("stage-choose").onclick=()=>{if(libraryContext)leaveLibrary();else previousChoose?.();};
 const catalog=el("section");catalog.id="legacyCampaignLibrary";catalog.append(el("h2","Historical Campaign Situations"),el("p","Choose a dated collection. Each Situation includes its source card, deployment and game map."));
 const campaignLabel=el("label","Campaign"),campaignChoice=el("select");campaignChoice.id="legacyCampaignChoice";campaignLabel.htmlFor=campaignChoice.id;
 for(const c of window.CAMPAIGN_REGISTRY.campaigns){const opt=el("option",c.title+" | "+c.startDate+" to "+c.endDate);opt.value=c.id;campaignChoice.append(opt);}
 campaignChoice.value=campaignRecord.id;catalog.append(campaignLabel,campaignChoice,button("Open campaign situations",()=>{const record=window.CAMPAIGN_REGISTRY.campaigns.find(c=>c.id===campaignChoice.value);selectPackage(record.situations[0].sourcePackageId);situationSelected=false;campaignFlow.active=false;campaignFlow.renderParent();parentPanel.scrollIntoView?.({block:"nearest"});}));
 node("workflowChoose").append(catalog);
 window.CampaignSituationWorkflow=campaignFlow;
 window.CampaignSituationScreen={
  mount(pkg,options){if(!library.some(x=>x.id===pkg.id))library.push(pkg);let record=window.CAMPAIGN_REGISTRY.campaigns.find(c=>c.id===pkg.campaignAssignment.campaignId);if(!record){record={...pkg.parentCampaign,situations:[]};window.CAMPAIGN_REGISTRY.campaigns.push(record);}if(!record.situations.some(x=>x.sourcePackageId===pkg.id))record.situations.push({id:pkg.campaignAssignment.situationId,number:pkg.campaignAssignment.number,sourcePackageId:pkg.id,title:pkg.situation.title});selectPackage(pkg.id);embedded=options;if(options.plan)s=PanzerSituationModel.validate(JSON.parse(JSON.stringify(options.plan)),d);dialog.classList.add('campaign-embedded');dialog.hidden=false;options.host.append(dialog);campaignFlow.choose();},
  refreshCard(host){if(embedded){embedded.cards=host;campaignFlow.render();}},
  close(){if(embedded)campaignFlow.returnParent();}
 };

 const commandLink=el("a","Ardennes command exercise: supply and reserve decisions");commandLink.href="index.html?exercise=ardennes";commandLink.onclick=()=>{location.href="index.html?exercise=ardennes";};commandLink.className="primary";node("workflowChoose").append(commandLink);
 if(window.ARDENNES_VARIANT_READY)window.ARDENNES_VARIANT_READY.then(id=>{if(!id)return;selectPackage(id);campaignFlow.choose();const back=el("a","Return to Ardennes command exercise");back.href="index.html?exercise=ardennes";cardPanel.prepend(back);}).catch(e=>{const warning=el("p",e.message);warning.setAttribute("role","alert");node("workflowChoose").append(warning);});

 dialog.addEventListener("close",()=>{if(!embedded&&campaignFlow.active&&campaignFlow.stage==="maneuver"){campaignFlow.returnParent();}});
 campaignFlow.renderParent();
 for(const id of ["workflowStart","workflowExplore"]){const previous=node(id).onclick;node(id).onclick=()=>{campaignFlow.active=false;campaignFlow.hide();dialog.close();if(id==="workflowStart")selectPackage(window.PANZER_SITUATION_DATA.id);libraryContext=false;parentPanel.hidden=true;previous?.();};}
})();
