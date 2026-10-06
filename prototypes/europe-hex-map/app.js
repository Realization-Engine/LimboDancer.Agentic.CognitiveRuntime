"use strict";
const DATA=window.CAMPAIGN_MAP_DATA;
const [mapLeft,mapTop,mapRight,mapBottom]=DATA.metadata.bounds;
const continentalView=[mapLeft,mapTop,mapRight-mapLeft,mapBottom-mapTop];
const svg=document.getElementById("map"), NS="http://www.w3.org/2000/svg";
function element(name,attrs,parent){const e=document.createElementNS(NS,name);Object.entries(attrs).forEach(([k,v])=>e.setAttribute(k,v));(parent||svg).append(e);return e;}
function path(g){const line=coords=>"M"+coords.map(p=>p[0].toFixed(2)+","+p[1].toFixed(2)).join("L");switch(g.type){case"Polygon":return g.coordinates.map(r=>line(r)+"Z").join("");case"MultiPolygon":return g.coordinates.map(p=>path({type:"Polygon",coordinates:p})).join("");case"LineString":return line(g.coordinates);case"MultiLineString":return g.coordinates.map(line).join("");case"GeometryCollection":return g.geometries.map(path).join("");default:return"";}}
const land=element("g",{}), forest=element("g",{}), mountains=element("g",{}), water=element("g",{}), roads=element("g",{display:"none"}), railways=element("g",{display:"none"}), grid=element("g",{}), terrainLabels=element("g",{"pointer-events":"none"}), cities=element("g",{"pointer-events":"none"});
const layerStyle={saltBasins:[water,"#d6c5ad","#a68d75",2],land:[land,"#d8ddc3","#a4b7a4",2],lakes:[water,"#accbd0","none",0],rivers:[water,"none","#729fae",5],forest:[forest,"#69976d","none",0],mountains:[mountains,"#9c8970","#877862",2],roads:[roads,"none","#b17c42",4],railways:[railways,"none","#535760",4]};
for(const f of [...DATA.features,...DATA.researchTransport.features]){const s=layerStyle[f.properties.layer];if(!s)continue;const attrs={d:path(f.geometry),fill:s[1],stroke:s[2],"stroke-width":s[3],"fill-rule":"evenodd"};if(f.properties.layer==="saltBasins")attrs["stroke-dasharray"]="7 5";if(f.properties.layer==="mountains")attrs.opacity=.48;if(f.properties.layer==="forest")attrs.opacity=.60;if(f.properties.layer==="railways")attrs["stroke-dasharray"]="13 8";element("path",attrs,s[0]);}
for(const t of DATA.terrainLabels){const e=element("text",{x:t.x,y:t.y,"text-anchor":"middle",fill:"#77654e","font-size":33,"font-style":"italic","paint-order":"stroke",stroke:"#eee8da","stroke-width":5},terrainLabels);e.textContent=t.name;}
const historicalRail=element("g",{"pointer-events":"none"});
const railTiers=[0,1,2].map(t=>element("path",{d:DATA.historicalRailNetwork.features.filter(f=>f.properties.displayTier===t).map(f=>path(f.geometry)).join(""),fill:"none",stroke:["#71384c","#8a5163","#a77786"][t],"stroke-width":[1.3,1,.7][t],"vector-effect":"non-scaling-stroke",opacity:.9},historicalRail));
document.getElementById("historicalRailToggle").onchange=e=>{historicalRail.style.display=e.target.checked?"":"none";};
const southernTransport=element("g",{"pointer-events":"none"}), southernNodes=[];
for(const f of DATA.southernTransport.features){const p=f.properties;const node=element("path",{d:path(f.geometry),fill:"none",stroke:p.mode==="rail"?"#71384c":p.mode==="track"?"#94744b":"#bc591c","stroke-width":p.displayTier===0?1.8:1.2,"stroke-dasharray":p.mode==="track"?"2 5":"7 3","vector-effect":"non-scaling-stroke"},southernTransport);southernNodes.push({f,node});}
for(const id of ["southRail","southRoad"])document.getElementById(id).checked=true;
for(const id of ["southRail","southRoad","southLater"])document.getElementById(id).onchange=()=>renderView();
function updateSouthern(level){
 const later=document.getElementById("southLater").checked;
 let visible=0;const list=document.getElementById("southRoutes");list.replaceChildren();
 for(const {f,node} of southernNodes){const p=f.properties;const enabled=(p.evidenceYear<=1939||later)&&document.getElementById(p.mode==="rail"?"southRail":"southRoad").checked;const show=enabled&&p.displayTier<=level;node.style.display=show?"":"none";if(show)visible++;if(!enabled)continue;
 const b=document.createElement("button");b.textContent=p.name+" ("+p.evidenceYear+")";b.style.display="block";
 b.onclick=()=>{const coords=f.geometry.coordinates;const xs=coords.map(c=>c[0]),ys=coords.map(c=>c[1]);const w=Math.max(300,Math.max(...xs)-Math.min(...xs)+100),h=Math.max(300,Math.max(...ys)-Math.min(...ys)+100);view=[Math.min(...xs)-50,Math.min(...ys)-50,w,h];renderView();const d=document.getElementById("southEvidence");d.replaceChildren();const src=DATA.southernTransport.sources.find(s=>s.id===p.sourceId);const para=document.createElement("p");para.textContent=p.name+". Evidence: "+p.evidenceYear+". "+p.note+" "+src.finding+" Schematic connection; service, crossings and capacity unverified.";const link=document.createElement("a");link.textContent=src.title;link.href=src.url;link.target="_blank";link.rel="noopener";d.append(para,link);};list.append(b);}
 document.getElementById("southCount").textContent=visible+" connections at this scale. "+(later?"Includes separately dated wartime evidence.":"Evidence dated 1939 or earlier only.");
}
const pilot=element("g",{"pointer-events":"none"}), pilotPaths=new Map(), pilotPlaceNodes=new Map();
for(const f of DATA.historicalPilot.features){
 const color=f.properties.mode==="road"?"#bc591c":"#773f86";
 const e=element("path",{d:path(f.geometry),fill:"none",stroke:color,"stroke-width":3,"vector-effect":"non-scaling-stroke","stroke-dasharray":"6 4"},pilot);pilotPaths.set(f.properties.id,e);
}
pilot.style.display="none";
const pilotLabels=element("g",{"pointer-events":"none"},pilot);
for(const [name,[x,y]] of Object.entries(DATA.historicalPilot.places)){
 const group=element("g",{},pilotLabels);
 element("circle",{cx:x,cy:y,r:1.7,fill:"#773f86"},group);
 const offset={"The Hague":[-25,-4],"Voorburg":[-24,5],"Zoetermeer":[4,5],"Rotterdam":[3,4]}[name]||[3,-3];
 const e=element("text",{x:x+offset[0],y:y+offset[1],"font-size":4,fill:"#452b4c","paint-order":"stroke",stroke:"#f5f4ed","stroke-width":1},group);e.textContent=name;pilotPlaceNodes.set(name,group);
}
const nodes=new Map();
for(const c of DATA.cells){const e=element("polygon",{points:c.vertices.map(p=>p.join(",")).join(" "),class:"hex","data-id":c.id},grid);nodes.set(c.id,e);e.addEventListener("click",()=>{if(!dragged)select(c);});}
const cityNodes=DATA.cities.map(c=>{
 const group=element("g",{},cities);
 element("circle",{cx:0,cy:0,r:3,fill:"#40594b",stroke:"#f7f4e9","stroke-width":1},group);
 const label=element("text",{x:6,y:-5,class:"city"},group);
 label.style.fontSize="14px";label.style.strokeWidth="3px";label.textContent=c.name;
 return {city:c,node:group};
});
function focusConnections(features){
 if(!features.length)return;
 const pts=features.flatMap(f=>f.geometry.coordinates), xs=pts.map(p=>p[0]),ys=pts.map(p=>p[1]);
 const w=Math.max(60,Math.max(...xs)-Math.min(...xs)+35),h=Math.max(60,Math.max(...ys)-Math.min(...ys)+35);
 view=[(Math.max(...xs)+Math.min(...xs)-w)/2,(Math.max(...ys)+Math.min(...ys)-h)/2,w,h];renderView();
 document.getElementById("pilotToggle").checked=true;pilot.style.display="";
}
let regionFeatures=DATA.historicalPilot.features;
function showConnections(features){
 const ids=new Set(features.map(f=>f.properties.id)),places=new Set(features.flatMap(f=>f.properties.places));
 for(const [id,node] of pilotPaths)node.style.display=ids.has(id)?"":"none";
 for(const [name,node] of pilotPlaceNodes)node.style.display=places.has(name)?"":"none";
}
function routeList(){
 const list=document.getElementById("pilotRoutes");list.replaceChildren();
 for(const f of regionFeatures){
  const b=document.createElement("button");b.textContent=(f.properties.mode==="rail"?"Rail: ":"Road: ")+f.properties.name;b.style.margin="4px 0";
  b.onclick=()=>{showConnections([f]);focusConnections([f]);const d=document.getElementById("pilotEvidence");d.replaceChildren();const src=DATA.historicalPilot.sources.find(s=>s.id===f.properties.sourceId);const p=document.createElement("p");p.textContent="Existence by "+f.properties.existenceByYear+". "+f.properties.dateNote+" "+src.finding+" Schematic connection only; baseline service and capacity unverified. Source review: "+(src.access||"page-read")+".";d.append(p);const a=document.createElement("a");a.textContent=src.title;a.href=src.url;a.target="_blank";a.rel="noopener";d.append(a);for(const id of f.properties.corroboratingSourceIds||[]){const other=DATA.historicalPilot.sources.find(s=>s.id===id);const para=document.createElement("p");para.textContent=other.finding;d.append(para);const link=document.createElement("a");link.textContent=other.title;link.href=other.url;link.target="_blank";link.rel="noopener";d.append(link);}};list.append(b);
 }
}
for(const region of DATA.historicalPilot.regions){const option=document.createElement("option");option.value=region.name;option.textContent=region.name;document.getElementById("pilotRegion").append(option);}
document.getElementById("pilotRegion").onchange=e=>{
 const region=DATA.historicalPilot.regions.find(r=>r.name===e.target.value);
 regionFeatures=region?DATA.historicalPilot.features.filter(f=>f.properties.region===region.name):DATA.historicalPilot.features;
 document.getElementById("regionGaps").textContent=region?region.gaps:"Selected examples in every listed region. Coverage is partial; no complete national network is claimed.";
 document.getElementById("pilotEvidence").replaceChildren();routeList();showConnections(regionFeatures);focusConnections(regionFeatures);
};
document.getElementById("pilotCount").textContent=DATA.historicalPilot.features.length;
document.getElementById("regionCount").textContent=DATA.historicalPilot.regions.length;
document.getElementById("regionGaps").textContent="Selected examples only. Region names are navigation groups, not 1939 borders. Select a region to see remaining gaps.";
routeList();
document.getElementById("focusPilot").onclick=()=>{showConnections(regionFeatures);focusConnections(regionFeatures);};
document.getElementById("pilotToggle").onchange=e=>{pilot.style.display=e.target.checked?"":"none";};
let activeRegion=null;
// Western Europe is a distinct reference and planning presentation of shared geography.
const westernLayer=element("g",{}), westernWater=element("g",{"pointer-events":"none"},westernLayer), westernGrid=element("g",{},westernLayer), westernPlaces=element("g",{},westernLayer), westernSites=element("g",{},westernLayer);
westernLayer.style.display="none";
// A non-interactive veil emphasizes the active footprint while retaining context.
const theaterFocus=element("g",{"pointer-events":"none","aria-hidden":"true"});
const theaterShade=element("path",{fill:"#6c7378",opacity:.32,"fill-rule":"evenodd"},theaterFocus);
let theaterData=DATA.westernTheater, loadedTheater=null;
let westernFootprintPath=path(theaterData.hexFootprint);
const theaterPerimeter=element("path",{d:westernFootprintPath,fill:"none",stroke:"#59665d","stroke-width":1.3,"vector-effect":"non-scaling-stroke","stroke-linejoin":"round"},theaterFocus);
theaterFocus.style.display="none";
const theaterGridDefs=element("defs",{});
const theaterGridClip=element("clipPath",{id:"theater-grid-clip",clipPathUnits:"userSpaceOnUse"},theaterGridDefs);
const theaterGridBoundary=element("path",{},theaterGridClip);

let westernMode="geography",westernSelected=null;
const westernCells=new Map(), westernLabels=[],siteLabels=[];
function loadWorkspace(id){
 if(loadedTheater===id)return;
 loadedTheater=id;theaterData=id===DATA.regionalCampaign.id?{...DATA.regionalCampaign,sites:[]}:DATA.theaterWorkspaces[id];westernFootprintPath=path(theaterData.hexFootprint);
 for(const group of [westernWater,westernGrid,westernPlaces,westernSites])group.replaceChildren();
 westernCells.clear();westernLabels.length=0;siteLabels.length=0;westernSelected=null;westernMode="geography";
 document.getElementById("savePlan").disabled=true;document.getElementById("planText").value="";document.getElementById("planNotice").textContent="";
 document.getElementById("westernDetail").textContent="Select a sector or a historical logistics point.";
 document.getElementById("workspaceTitle").textContent=theaterData.name;
for(const river of theaterData.rivers)element("path",{d:path(river.geometry),fill:"none",stroke:"#558ca0","stroke-width":1,"vector-effect":"non-scaling-stroke"},westernWater);
for(const cell of theaterData.cells){
 const node=element("path",{d:path({type:"Polygon",coordinates:[[...cell.vertices,cell.vertices[0]]]}),class:"hex"},westernGrid);
 node.addEventListener("click",()=>{if(!dragged)selectWestern(cell);});westernCells.set(cell.id,node);
}
for(const town of theaterData.towns){
 const node=element("g",{"pointer-events":"none"},westernPlaces);
 element("circle",{r:2.5,fill:"#374b3e"},node);
 const label=element("text",{x:5,y:-4,fill:"#263c3b","font-size":12,"paint-order":"stroke",stroke:"#f5f4ed","stroke-width":3},node);label.textContent=town.name;
 westernLabels.push({town,node});
}
for(const site of theaterData.sites){
 const node=element("g",{tabindex:0,role:"button","aria-label":site.name+" historical "+site.kind},westernSites);
 element("rect",{x:-6,y:-6,width:12,height:12,fill:site.kind==="port"?"#71384c":"#99651f",stroke:"#fff","stroke-width":1},node);
 const label=element("text",{x:9,y:4,fill:"#553923","font-size":13,"paint-order":"stroke",stroke:"#fff9e9","stroke-width":3},node);label.textContent=site.name;
 function inspect(){const detail=document.getElementById("westernDetail");detail.replaceChildren();const p=document.createElement("p");p.textContent=site.name+" · "+site.kind+". "+site.note+" "+"Approximate town center, not a facility position.";detail.append(p);const date=document.createElement("code");date.textContent=site.period;detail.append(date);const a=document.createElement("a");a.href=site.source;a.target="_blank";a.rel="noopener";a.textContent="Historical source";detail.append(a);}
 node.addEventListener("click",()=>{if(!dragged)inspect();});node.addEventListener("keydown",e=>{if(e.key==="Enter"){inspect();}});siteLabels.push({site,node});
}
}
function selectWestern(cell){
 if(westernSelected)westernCells.get(westernSelected.id)?.classList.remove("selected");
 westernSelected=cell;westernCells.get(cell.id).classList.add("selected");
 if(activeRegion){regionalSector=cell;renderRegional();return;}
 const detail=document.getElementById("westernDetail");detail.replaceChildren();const identity=document.createElement("code");identity.textContent=cell.id;detail.append(identity);const description=document.createElement("p");description.textContent="26 km across. Settlements: "+(cell.towns.join(", ")||"None in the reference source")+". Ranges: "+((cell.mountainRegions||[]).join(", ")||"None mapped")+". Lakes and salt basins: "+((cell.waterNames||[]).join(", ")||"None mapped")+". Terrain and route availability are not adjudicated at this resolution.";detail.append(description);
 document.getElementById("savePlan").disabled=false;
}
function planKey(){return "theater-plans-v1-"+seed;}
function allPlans(){try{const p=JSON.parse(localStorage.getItem(planKey())||"null");if(Array.isArray(p))return p.filter(n=>typeof n.text==="string"&&typeof n.cellId==="string"&&typeof n.theaterId==="string");const old=JSON.parse(localStorage.getItem("western-plans-v1-"+seed)||"[]");return Array.isArray(old)?old.filter(n=>typeof n.text==="string"&&typeof n.cellId==="string").map(n=>({...n,theaterId:"western"})):[];}catch{return [];}}
function readPlans(){return allPlans().filter(n=>n.theaterId===(activeTheater?.id||"western"));}
function showPlans(){const list=document.getElementById("planList");list.replaceChildren();for(const item of readPlans()){const b=document.createElement("button");b.textContent=item.cellId+": "+item.text;b.style.display="block";b.style.margin="6px 0";b.onclick=()=>{const c=theaterData.cells.find(c=>c.id===item.cellId);if(c){selectWestern(c);view=[c.x-180,c.y-150,360,300];renderView();}};list.append(b);}}
document.getElementById("savePlan").onclick=()=>{if(!westernSelected)return;const text=document.getElementById("planText").value.trim();if(!text)return;const plans=allPlans();plans.push({theaterId:activeTheater.id,cellId:westernSelected.id,text:text.slice(0,500)});try{localStorage.setItem(planKey(),JSON.stringify(plans));document.getElementById("planText").value="";showPlans();document.getElementById("planNotice").setAttribute("data-state","success");document.getElementById("planNotice").textContent="Planning note saved for this campaign. No order issued.";}catch{document.getElementById("planNotice").setAttribute("data-state","error");document.getElementById("planNotice").textContent="Browser storage is unavailable. Copy the note before leaving.";}};
for(const mode of ["geography","logistics","planning"])document.getElementById("mode-"+mode).onclick=()=>{westernMode=mode;renderView();};
function updateWestern(){
 const active=!!activeTheater;
 if(active)loadWorkspace(activeRegion?activeRegion.id:activeTheater.id);
 westernLayer.style.display=active?"":"none";
 theaterFocus.style.display=activeTheater?"":"none";
 if(activeTheater){
  const footprint=westernFootprintPath;
  theaterPerimeter.setAttribute("d",footprint);
  theaterGridBoundary.setAttribute("d",footprint);
  // Extend beyond the viewBox to cover letterboxing and remain seamless when panning.
  const [x,y,w,h]=view,pad=Math.max(w,h)*2;
  theaterShade.setAttribute("d",path({type:"Polygon",coordinates:[[[x-pad,y-pad],[x+w+pad,y-pad],[x+w+pad,y+h+pad],[x-pad,y+h+pad],[x-pad,y-pad]]]})+footprint);
 }
 document.getElementById("westernWorkspace").style.display=active&&!activeRegion?"":"none";
 document.getElementById("workspaceControls").style.display=activeTheater&&!formationOpen?"":"none";
 // Responsive workspace columns are defined in site.css.
 for(const id of ["coarseSelection","selectionControls"])document.getElementById(id).style.display=active?"none":"";
 if(activeTheater&&!active)grid.setAttribute("clip-path","url(#theater-grid-clip)");
 else grid.removeAttribute("clip-path");
 grid.style.display=active||document.getElementById("gridToggle").checked===false?"none":"";
 document.getElementById("gridScale").textContent=active?"26 km theater sectors, anchored to shared coordinates. Reference detail, not tactical terrain.":"Approximately 104 km across each hex. Coastlines remain geographic rather than snapping to hex edges.";
 if(!active){water.style.display=document.getElementById("riverToggle").checked===false?"none":"";return;}
 cities.style.display="none";water.style.display=document.getElementById("riverToggle").checked===false?"none":"";
 westernWater.style.display=document.getElementById("riverToggle").checked===false?"none":"";
 westernGrid.style.display=document.getElementById("gridToggle").checked===false?"none":"";
 westernSites.style.display=westernMode==="logistics"?"":"none";
 westernPlaces.style.display=westernMode==="logistics"||document.getElementById("cityToggle").checked===false?"none":"";
 document.getElementById("planPanel").style.display=westernMode==="planning"?"":"none";
 document.getElementById("westernModeSummary").textContent={geography:"26 km sectors · settlements · river barriers. Select a sector to inspect or plan.",logistics:"Dated logistics references: ports, transfer points and transport connections. Select a square for its source and date. Coverage is partial; no live supply state is inferred.",planning:"Select a sector and record a planning note. Notes belong to this campaign; they do not execute orders."}[westernMode];
 for(const mode of ["geography","logistics","planning"])document.getElementById("mode-"+mode).setAttribute("aria-pressed",mode===westernMode?"true":"false");
 const scale=Math.max(view[2]/(svg.clientWidth||1200),view[3]/(svg.clientHeight||900));const used=[];
 for(const {town,node} of [...westernLabels].sort((a,b)=>a.town.rank-b.town.rank||a.town.name.localeCompare(b.town.name))){
  const x=(town.x-view[0])/scale,y=(town.y-view[1])/scale;
  const visible=x>=0&&y>=0&&x<(svg.clientWidth||1200)&&y<(svg.clientHeight||900)&&!used.some(p=>Math.abs(p[0]-x)<85&&Math.abs(p[1]-y)<22);
  node.style.display=visible?"":"none";if(visible)used.push([x,y]);node.setAttribute("transform",`translate(${town.x} ${town.y}) scale(${scale})`);
 }
 for(const {site,node} of siteLabels)node.setAttribute("transform",`translate(${site.x} ${site.y}) scale(${scale})`);
 if(westernMode==="planning")showPlans();
}

// Regional workspace shares the theater renderer and adds explicit command state.
let regionalState=null,regionalHQ="first-army",regionalMission="beachhead",regionalSector=null;
function regionalKey(){return "regional-exercise-v1-"+seed+"-"+DATA.regionalCampaign.sourceHash+"-"+DATA.metadata.baseHash;}
function loadRegionalState(){
 const raw=localStorage.getItem(regionalKey());
 regionalState=raw?RegionalModel.validate(JSON.parse(raw),DATA.regionalCampaign,seed,DATA.metadata.baseHash):RegionalModel.create(DATA.regionalCampaign,seed,DATA.metadata.baseHash);
}
function regionalForExport(){
 if(regionalState&&regionalState.seed===seed)return regionalState;
 const raw=localStorage.getItem(regionalKey());
 return raw?RegionalModel.validate(JSON.parse(raw),DATA.regionalCampaign,seed,DATA.metadata.baseHash):null;
}
function regionalNotice(text,state="pending"){const n=document.getElementById("regionalNotice");n.textContent=text;n.setAttribute("data-state",state);}
function enterRegional(){
 if(!activeTheater||activeTheater.id!==DATA.regionalCampaign.theaterId)return;
 try{loadRegionalState();}catch(error){regionalState=null;document.getElementById("regionalWorkspace").style.display="";document.getElementById("regionalWorkspace").open=true;regionalNotice("Could not load saved exercise: "+error.message,"error");return;}
 activeRegion=DATA.regionalCampaign;regionalSector=null;regionalHQ="first-army";regionalMission="beachhead";
 view=[...activeRegion.view];document.getElementById("regionChoice").value=activeRegion.id;
 document.getElementById("regionalEntry").open=true;document.getElementById("regionalWorkspace").open=true;
 document.getElementById("mapHeading").textContent=activeRegion.name;document.getElementById("breadcrumb").textContent="Whole Map › "+activeTheater.name+" › Normandy, 8 June 1944";document.getElementById("viewLevel").textContent="REGIONAL CAMPAIGN";renderView();
}
function changeRegional(action){
 try{
  if(!regionalState)throw new Error("Open the regional exercise first.");
  const next=JSON.parse(JSON.stringify(regionalState));
  if(action==="advance")RegionalModel.advance(next);
  else RegionalModel.act(next,regionalMission,action,regionalHQ,document.getElementById("regionalText").value,regionalSector?.id||null);
  localStorage.setItem(regionalKey(),JSON.stringify(next));regionalState=next;
  document.getElementById("regionalText").value="";regionalNotice("Exercise saved. No combat or real order has been executed.","success");renderRegional();updateTheaterCounters();
 }catch(error){regionalNotice(error.message,"error");}
}
function renderRegional(){
 renderCampaignActions();
 const eligible=activeTheater?.id===DATA.regionalCampaign.theaterId;
 document.getElementById("regionalEntry").style.display=activeTheater?"":"none";
 document.getElementById("regionChoice").disabled=!eligible;
 document.getElementById("campaignBrief").textContent=eligible?DATA.regionalCampaign.campaign.windowLabel+". "+DATA.regionalCampaign.campaign.purpose+" "+DATA.regionalCampaign.campaign.timeBasis+" "+DATA.regionalCampaign.campaign.deploymentStatus+" "+DATA.regionalCampaign.campaign.geographyStatus:"No dated campaign package is available for this theater yet. Geography browsing remains available; deployments and campaign dates are not inferred from another theater.";
 document.getElementById("regionalWorkspace").style.display=activeRegion?"":"none";
 if(formationOpen){document.getElementById("regionalWorkspace").style.display="none";document.getElementById("regionalEntry").style.display="none";renderFormation();return;}
 if(!activeRegion||!regionalState)return;
 const c=DATA.regionalCampaign,s=regionalState;
 document.getElementById("regionalStep").textContent="Campaign time: "+s.clock.current.replace("T"," ").replace("Z"," UTC")+" | Ends: "+c.campaign.endExclusive+" | Communication demonstration step "+s.step+" (no clock advance)";
 const hierarchy=document.getElementById("regionalHierarchy");hierarchy.replaceChildren();
 const current=s.forces.find(f=>f.id===regionalHQ);
 const trail=[];for(let node=current;node;node=s.forces.find(f=>f.id===node.parentId))trail.unshift(node);
 const navigate=id=>{regionalHQ=id;regionalMission=null;regionalSector=null;document.getElementById("regionalText").value="";renderRegional();};
 const crumbs=document.getElementById("regionalBreadcrumb");crumbs.replaceChildren();
 for(const node of trail){const b=document.createElement("button");b.textContent=node.name;b.disabled=node.id===regionalHQ;b.onclick=()=>navigate(node.id);crumbs.append(b);}
 const incoming=s.missions.filter(m=>m.recipient===regionalHQ);
 const planned=incoming.length===0||incoming.every(m=>["planned","executing","reported","assessed"].includes(m.status));
 const children=s.forces.filter(f=>f.parentId===regionalHQ);
 const outgoing=s.missions.filter(m=>m.issuer===regionalHQ);
 const ready=planned&&(incoming.length>0||outgoing.every(m=>!["draft","assigned"].includes(m.status)));
 if(ready)for(const child of children){const orders=outgoing.filter(m=>m.recipient===child.id),delivered=orders.length>0&&orders.every(m=>!["draft","assigned"].includes(m.status));const b=document.createElement("button");b.textContent=child.name+(delivered?"":" (assign and deliver order first)");b.disabled=!delivered;b.onclick=()=>navigate(child.id);hierarchy.append(b);}
 document.getElementById("regionalCurrent").textContent=current.name+" | "+current.echelon;
 document.getElementById("regionalNext").textContent=!planned?"Next: save your plan for the received mission. Subordinate orders unlock afterward.":outgoing.some(m=>m.status==="draft")?"Next: select a subordinate mission and Assign it, then advance communications to deliver the order.":outgoing.some(m=>m.status==="assigned")?"Next: advance communications to deliver the assigned order.":children.length?"Next: choose a direct subordinate in the command panel. Use the command path to return for reports or another branch.":"Next: select a map sector and create your Situation Card. Execution and reporting remain available for this headquarters.";
 const force=s.forces.find(f=>f.id===regionalHQ);
 document.getElementById("regionalForce").textContent=force.name+". Strength, readiness, supply and position: unknown. Formation identity persists; no force movement is simulated. "+(force.organizationNote||"")+(force.openingReport?" Opening context: "+force.openingReport.text+" ("+force.openingReport.asOf+").":"");
 const available=s.missions.filter(m=>(m.issuer===regionalHQ&&planned)||(m.recipient===regionalHQ&&!["draft","assigned"].includes(m.status)));
 if(!available.some(m=>m.id===regionalMission))regionalMission=available[0]?.id||null;
 const list=document.getElementById("regionalMissions");list.replaceChildren();
 for(const m of available){const b=document.createElement("button");b.textContent=m.title+" · "+RegionalModel.status(m,regionalHQ);b.setAttribute("aria-pressed",m.id===regionalMission?"true":"false");b.onclick=()=>{regionalMission=m.id;document.getElementById("regionalText").value="";renderRegional();};list.append(b);}
 const m=s.missions.find(m=>m.id===regionalMission),detail=document.getElementById("regionalMissionDetail");detail.replaceChildren();
 if(m){const p=document.createElement("p");p.textContent=m.intent+" Issuer: "+m.issuer+". Recipient: "+m.recipient+". Parent mission: "+(m.parentId||"none")+". Known status: "+RegionalModel.status(m,regionalHQ)+".";detail.append(p);
  for(const [label,key] of [["Planning area","area"],["Boundary","boundary"],["Timing","timing"],["Support","support"]]){if(m[key]){const note=document.createElement("p");note.textContent=label+": "+m[key];detail.append(note);}}
  const children=s.missions.filter(x=>x.parentId===m.id);
  if(children.length){const note=document.createElement("p");note.textContent="Subordinate missions: "+children.length+". Complete and assess their reports before reporting this mission.";detail.append(note);}
  if(m.recipient===regionalHQ&&m.plan){const plan=document.createElement("p");plan.textContent="Your plan: "+m.plan;detail.append(plan);}
  const source=c.sources.find(x=>x.id===m.sourceId);const a=document.createElement("a");a.textContent="Historical context (exercise wording is authored)";a.href=source.url;a.target="_blank";a.rel="noopener";detail.append(a);
 }else detail.textContent="No delivered mission at this headquarters. Advance communications or select the issuing headquarters.";
 for(const action of ["assign","plan","execute","report","assess"]){let enabled=false;if(m){const recipient=m.recipient===regionalHQ,issuer=m.issuer===regionalHQ;enabled=action==="assign"?issuer&&m.status==="draft"&&(!m.parentId||s.missions.some(p=>p.id===m.parentId&&["planned","executing"].includes(p.status))):action==="plan"?recipient&&m.status==="received":action==="execute"?recipient&&m.status==="planned":action==="report"?recipient&&m.status==="executing"&&s.missions.filter(x=>x.parentId===m.id).every(x=>x.status==="assessed"):issuer&&m.status==="reported"&&m.knownToIssuer==="reported";}document.getElementById("regional-"+action).disabled=!enabled;document.getElementById("regional-"+action).style.display=enabled?"":"none";}
 const writing=["plan","report","assess"].some(a=>!document.getElementById("regional-"+a).disabled);
 document.getElementById("regionalText").style.display=writing?"":"none";
 document.getElementById("regionalTextLabel").style.display=writing?"":"none";
 document.getElementById("regionalAdvance").style.display=s.messages.some(m=>m.deliveredAt===null&&(m.from===regionalHQ||m.to===regionalHQ))?"":"none";
 document.getElementById("regionalSector").textContent=regionalSector?"Selected sector: "+regionalSector.id+". Settlements: "+(regionalSector.towns.join(", ")||"none mapped"):"Select a 13 km sector to attach a geographic reference to a plan.";
 const messages=document.getElementById("regionalMessages");messages.replaceChildren();
 for(const message of RegionalModel.messages(s,regionalHQ)){const p=document.createElement("p");p.textContent=message.kind+" | "+message.from+" → "+message.to+" | "+(message.deliveredAt===null?"queued, due step "+message.dueAt:"delivered step "+message.deliveredAt)+": "+message.text;messages.append(p);}
 const blockers=document.getElementById("regionalAdmission");blockers.replaceChildren();for(const text of RegionalModel.admission()){const li=document.createElement("li");li.textContent=text;blockers.append(li);}
 renderSituation();
 document.getElementById("gridScale").textContent="13 km regional sectors. Shared reference geography; no tactical terrain or unit locations inferred.";
}
const regionalObjectives=element("g",{}),regionalObjectiveNodes=[];
for(const o of DATA.regionalCampaign.objectives){const node=element("g",{role:"button",tabindex:0,"aria-label":o.name+" objective reference"},regionalObjectives);element("circle",{r:5,fill:"#b77725",stroke:"#fff","stroke-width":1},node);const label=element("text",{x:9,y:4,"font-size":12,fill:"#594018","paint-order":"stroke",stroke:"#fff","stroke-width":3},node);label.textContent=o.name;const inspect=()=>{regionalNotice(o.name+": approximate objective reference, not a unit location.");};node.addEventListener("click",inspect);node.addEventListener("keydown",e=>{if(e.key==="Enter")inspect();});regionalObjectiveNodes.push({o,node});}
function updateRegional(){
 renderRegional();regionalObjectives.style.display=activeRegion?"":"none";
 if(!activeRegion)return;
 const scale=Math.max(view[2]/(svg.clientWidth||1200),view[3]/(svg.clientHeight||900));for(const {o,node} of regionalObjectiveNodes)node.setAttribute("transform",`translate(${o.x} ${o.y}) scale(${scale})`);
}
document.getElementById("regionChoice").onchange=e=>{if(e.target.value)enterRegional();else{activeRegion=null;regionalSector=null;enterTheater();}};
for(const a of ["assign","plan","execute","report","assess"])document.getElementById("regional-"+a).onclick=()=>changeRegional(a);
document.getElementById("regionalAdvance").onclick=()=>changeRegional("advance");
document.getElementById("regionalExport").onclick=()=>{if(regionalState)download("normandy-planning-exercise.json",{kind:"regional-planning-exercise",executable:false,referenceId:DATA.regionalCampaign.id,referenceSources:DATA.regionalCampaign.sources,state:regionalState,aslAdmissionRequirements:RegionalModel.admission()});};

function clearRegionalExercise(){
 try{
  if(!activeRegion||!regionalState)return;
  if(formationOpen)closeFormation();
  // Remove current and earlier reference-package versions for this campaign only.
  const prefix="regional-exercise-v1-"+seed+"-",keys=[];
  for(let i=0;i<localStorage.length;i++){const key=localStorage.key(i);if(key?.startsWith(prefix))keys.push(key);}
  for(const key of keys)localStorage.removeItem(key);
  if(westernSelected)westernCells.get(westernSelected.id)?.classList.remove("selected");
  westernSelected=null;regionalSector=null;regionalState=null;
  document.getElementById("regionalText").value="";
  enterRegional();
  regionalNotice("Cleared. Start again at First Army by assigning the beachhead mission.","success");
 }catch(error){regionalNotice("Could not finish clearing saved exercise data: "+error.message,"error");}
}
document.getElementById("regionalClear").onclick=clearRegionalExercise;

function renderCampaignActions(){
 const active=!!activeRegion&&!!regionalState,choice=document.getElementById("savedSituationChoice"),selected=choice.value;
 document.getElementById("regionalClear").disabled=!active;
 choice.replaceChildren();
 const cards=active?(regionalState.situations||[]):[];
 for(const card of cards){const option=document.createElement("option");option.value=card.id;option.textContent=card.title+" | "+card.parentFormationId;choice.append(option);}
 choice.value=cards.some(c=>c.id===selected)?selected:(cards[0]?.id||"");choice.disabled=!cards.length||formationOpen;
 document.getElementById("openSavedSituation").disabled=!cards.length||formationOpen;
 document.getElementById("campaignActionHint").textContent=!active?"Choose a theater, then a dated campaign inside Theater Workspace.":formationOpen?"Situation map is open. Clear campaign data restarts this campaign, keeping the map seed.":cards.length?"Open a saved Situation from any headquarters. Clear campaign data removes this campaign's plans, messages and Situations; the map seed is kept.":"No Situations in this campaign version. Follow the command workflow to a battalion, save its plan, select a sector and choose Create Situation Card. Older reference-version saves are kept separately and are not loaded here.";
}
document.getElementById("openSavedSituation").onclick=()=>{
 const card=activeRegion&&regionalState?.situations?.find(s=>s.id===document.getElementById("savedSituationChoice").value);
 if(!card||formationOpen)return;
 regionalHQ=card.parentFormationId;regionalMission=card.parentMissionId;regionalSector=null;
 document.getElementById("regionalText").value="";openFormation();
};

const theaterAreas=element("g",{}), theaterNodes=new Map();
let chosenTheater=null,activeTheater=null;
for(const t of DATA.theaterDefinitions.theaters){
 const option=document.createElement("option");option.value=t.id;option.textContent=t.name;document.getElementById("theaterChoice").append(option);
 const node=element("path",{d:path({type:"Polygon",coordinates:[t.boundary]}),fill:"#b68a3920",stroke:"#86602f","stroke-width":2,"stroke-dasharray":"7 5","vector-effect":"non-scaling-stroke",tabindex:0,role:"button","aria-label":"Select "+t.name},theaterAreas);
 const title=element("title",{},node);title.textContent=t.name;
 node.addEventListener("click",()=>{if(!dragged)chooseTheater(t.id);});
 node.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();chooseTheater(t.id);}});
 theaterNodes.set(t.id,node);
}
theaterAreas.style.display="none";
function chooseTheater(id){
 activeRegion=null;regionalSector=null;document.getElementById("regionChoice").value="";
 chosenTheater=DATA.theaterDefinitions.theaters.find(t=>t.id===id)||null;
 document.getElementById("theaterChoice").value=chosenTheater?chosenTheater.id:"";
 document.getElementById("theaterInfo").textContent=chosenTheater?chosenTheater.name+". "+(chosenTheater.scope||"")+" Campaign area; command and objectives unassigned. ":"Whole Map. Choose a theater to explore its geography.";
 for(const [key,node] of theaterNodes)node.setAttribute("fill",key===id?"#b68a3950":"#b68a3920");
 if(chosenTheater)enterTheater();else returnEurope();
}
function enterTheater(){
 document.getElementById("workspaceControls").open=true;
 if(!chosenTheater)return;
 activeTheater=chosenTheater;view=[...activeTheater.view];
 document.getElementById("theaterToggle").checked=false;theaterAreas.style.display="none";
 document.getElementById("mapHeading").textContent=activeTheater.name;
 document.getElementById("breadcrumb").textContent="Overview › "+activeTheater.name;
 document.getElementById("viewLevel").textContent="THEATER VIEW";renderView();
}
function returnEurope(){
 activeRegion=null;regionalSector=null;document.getElementById("regionChoice").value="";
 activeTheater=null;chosenTheater=null;document.getElementById("theaterChoice").value="";view=[...continentalView];
 document.getElementById("mapHeading").textContent="Europe & North Africa";
 document.getElementById("breadcrumb").textContent="Overview";
 document.getElementById("viewLevel").textContent="EUROPE & NORTH AFRICA · CONTINENTAL VIEW";renderView();
}
document.getElementById("theaterChoice").onchange=e=>chooseTheater(e.target.value);
document.getElementById("theaterToggle").onchange=e=>{theaterAreas.style.display=e.target.checked?"":"none";};


let view=[...continentalView];function renderView(){svg.setAttribute("viewBox",view.join(" "));pilotLabels.style.display=view[2]>900?"none":"";cities.style.display=document.getElementById("cityToggle").checked===false?"none":"";
 const railLevel=Math.max(activeTheater?1:0,view[2]>2400?0:view[2]>900?1:2);
 updateSouthern(railLevel);
 railTiers.forEach((node,t)=>node.style.display=t<=railLevel?"":"none");
 document.getElementById("railDetail").textContent=["Europe: major corridor candidates","Regional: corridors and through routes","Local: all mapped railway detail"][railLevel];
 const unitsPerPixel=Math.max(view[2]/(svg.clientWidth||1200),view[3]/(svg.clientHeight||900));
 for(const {city,node} of cityNodes)node.setAttribute("transform",`translate(${city.x} ${city.y}) scale(${unitsPerPixel})`);updateWestern();updateRegional();updateTheaterCounters();}renderView();
window.addEventListener("resize",renderView);
function zoom(f){const [x,y,w,h]=view;const nw=Math.max(20,Math.min(9000,w*f)),nh=nw*h/w;view=[x+(w-nw)/2,y+(h-nh)/2,nw,nh];renderView();}
document.getElementById("zoomIn").onclick=()=>zoom(.7);document.getElementById("zoomOut").onclick=()=>zoom(1/.7);document.getElementById("reset").onclick=()=>{view=activeRegion?[...activeRegion.view]:activeTheater?[...activeTheater.view]:[...continentalView];renderView();};
svg.addEventListener("wheel",e=>{e.preventDefault();zoom(e.deltaY>0?1.15:1/1.15);},{passive:false});
let down=null,dragged=false;
svg.addEventListener("pointerdown",e=>{down=[e.clientX,e.clientY,...view];dragged=false;});
svg.addEventListener("pointermove",e=>{if(!down)return;const dx=e.clientX-down[0],dy=e.clientY-down[1];if(Math.abs(dx)+Math.abs(dy)>5)dragged=true;if(dragged){const scale=Math.max(down[4]/svg.clientWidth,down[5]/svg.clientHeight);view=[down[2]-dx*scale,down[3]-dy*scale,down[4],down[5]];renderView();}});
window.addEventListener("pointerup",()=>{down=null;});
for(const [id,layer] of [["gridToggle",grid],["riverToggle",water],["cityToggle",cities],["forestToggle",forest],["mountainToggle",mountains],["roadToggle",roads],["railToggle",railways]])document.getElementById(id).onchange=e=>{layer.removeAttribute("display");layer.style.display=e.target.checked?"":"none";if(id==="mountainToggle")terrainLabels.style.display=layer.style.display;renderView();};
document.getElementById("researchPanel").ontoggle=e=>{if(!e.target.open){for(const [id,layer] of [["roadToggle",roads],["railToggle",railways]]){document.getElementById(id).checked=false;layer.style.display="none";}}};
let selected=null,selectSerial=0;
const storageKey="europe-hex-prototype-campaign-v1";
function freshSeed(){return Array.from(crypto.getRandomValues(new Uint32Array(4))).map(x=>x.toString(16).padStart(8,"0")).join("");}
let seed;try{seed=localStorage.getItem(storageKey);}catch{}
if(!seed||!/^[0-9a-f]{32}$/.test(seed))seed=freshSeed();
function saveSeed(){document.getElementById("seed").value=seed;try{localStorage.setItem(storageKey,seed);}catch{}}
saveSeed();
async function localSeed(c){const s=[DATA.metadata.generator,DATA.metadata.baseHash,seed,c.id,"future-refinement"].join("|");return Array.from(new Uint8Array(await crypto.subtle.digest("SHA-256",new TextEncoder().encode(s)))).map(x=>x.toString(16).padStart(2,"0")).join("");}
async function select(c){const serial=++selectSerial;if(selected)nodes.get(selected.id).classList.remove("selected");selected=c;nodes.get(c.id).classList.add("selected");const token=await localSeed(c);if(serial!==selectSerial)return;const d=document.getElementById("details");d.replaceChildren();function row(a,b){const div=document.createElement("div");div.className="fact";const label=document.createElement("span");label.textContent=a;const value=document.createElement("strong");value.textContent=b;div.append(label,value);d.append(div);}row("Hex",c.id);row("Surface",c.surface);row("Dry land",Math.round(c.landFraction*100)+"%");row("Forest footprint (2000)",c.forestReferenceFraction===null?"Outside reference coverage":Math.round(c.forestReferenceFraction*100)+"%");row("1939 transport","Not validated");row("Latitude",c.lat.toFixed(3)+"°");row("Longitude",c.lon.toFixed(3)+"°");const p=document.createElement("p");p.className="note";p.textContent="Ranges: "+(c.mountainRegions.join(", ")||"None mapped")+" · Waterways: "+(c.rivers.join(", ")||"None in source")+" · Lakes: "+((c.lakes||[]).join(", ")||"None mapped")+" · Salt basins: "+((c.saltBasins||[]).join(", ")||"None mapped")+" · Cities: "+(c.cities.join(", ")||"None selected");d.append(p);for(const id of c.periodTerrainContext||[]){const r=DATA.terrainReview.records.find(r=>r.id===id);if(!r)continue;const note=document.createElement("p");note.className="note";note.textContent="Regional historical context ("+r.period+"): "+r.finding+" ";const a=document.createElement("a");a.href=r.url;a.target="_blank";a.rel="noopener";a.textContent=r.title;note.append(a);d.append(note);}const small=document.createElement("p");small.className="note";small.textContent="Local refinement seed: "+token.slice(0,16)+"…";d.append(small);document.getElementById("exportHex").disabled=false;}
function download(name,obj){const url=URL.createObjectURL(new Blob([JSON.stringify(obj,null,2)],{type:"application/json"}));const a=document.createElement("a");a.href=url;a.download=name;a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);}
document.getElementById("newCampaign").onclick=()=>{seed=freshSeed();saveSeed();document.getElementById("regionalText").value="";if(activeRegion){regionalState=RegionalModel.create(DATA.regionalCampaign,seed,DATA.metadata.baseHash);regionalSector=null;}if(selected)select(selected);if(activeTheater)renderView();};
document.getElementById("exportCampaign").onclick=()=>{const {researchTransport,historicalPilot,historicalRailNetwork,southernTransport,theaterDefinitions,westernTheater,theaterWorkspaces,regionalCampaign,...campaignData}=DATA;download("europe-campaign-map.json",{campaign:{mapSeed:seed,geographyHash:DATA.metadata.baseHash,theaterDefinitions,planningNotes:allPlans(),regionalExercise:regionalForExport()},...campaignData});};
document.getElementById("exportHex").onclick=async()=>{const c=selected,s=seed;const token=await localSeed(c);download(c.id.replaceAll(":","-")+".json",{mapSeed:s,baseHash:DATA.metadata.baseHash,cell:c,refinementSeed:token});};
document.getElementById("sourceId").textContent="Source revision "+DATA.metadata.sourceManifest.commit.slice(0,12)+" · Base "+DATA.metadata.baseHash.slice(0,12);
document.getElementById("mapStatus").textContent=DATA.cells.length.toLocaleString()+" hexes · Equal-area Europe projection";

document.getElementById("exportImage").onclick=async()=>{
 const button=document.getElementById("exportImage"),status=document.getElementById("imageExportStatus");
 button.disabled=true;status.setAttribute("data-state","pending");status.textContent="Preparing map image...";
 const caption=document.getElementById("imageCaption").checked?[
 activeRegion?activeRegion.name:activeTheater?activeTheater.name:"Europe & North Africa",
 "Reference geography; tree cover: 2000; European rail: 1920-1940 (when shown).",
 "Southern transport evidence: "+(document.getElementById("southLater").checked?"through 1942":"by 1939")+". Operational status unverified.",
 "Sources: Natural Earth; EC JRC GLC2000; B. Polo Martin / NAKALA, CC-BY-NC-4.0 (rail)."
 ]:[];
 try{await exportMapPng(svg,(activeRegion?activeRegion.id:activeTheater?activeTheater.id:"europe-north-africa")+"-map.png",caption);status.setAttribute("data-state","success");status.textContent="Map image exported as PNG.";}
 catch(error){status.setAttribute("data-state","error");status.textContent="Image export failed: "+error.message;}
 finally{button.disabled=false;}
};
