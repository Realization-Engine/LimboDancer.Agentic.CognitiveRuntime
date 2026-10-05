// Western Europe is a distinct reference and planning presentation of shared geography.
const westernLayer=element("g",{}), westernWater=element("g",{"pointer-events":"none"},westernLayer), westernGrid=element("g",{},westernLayer), westernPlaces=element("g",{},westernLayer), westernSites=element("g",{},westernLayer);
westernLayer.style.display="none";
// A non-interactive veil emphasizes the active footprint while retaining context.
const theaterFocus=element("g",{"pointer-events":"none","aria-hidden":"true"});
const theaterShade=element("path",{fill:"#6c7378",opacity:.32,"fill-rule":"evenodd"},theaterFocus);
const westernFootprintPath=path(DATA.westernTheater.hexFootprint);
const theaterPerimeter=element("path",{d:westernFootprintPath,fill:"none",stroke:"#59665d","stroke-width":1.3,"vector-effect":"non-scaling-stroke","stroke-linejoin":"round"},theaterFocus);
theaterFocus.style.display="none";
const theaterGridDefs=element("defs",{});
const theaterGridClip=element("clipPath",{id:"theater-grid-clip",clipPathUnits:"userSpaceOnUse"},theaterGridDefs);
const theaterGridBoundary=element("path",{},theaterGridClip);

let westernMode="geography",westernSelected=null;
const westernCells=new Map(), westernLabels=[],siteLabels=[];
for(const river of DATA.westernTheater.rivers)element("path",{d:path(river.geometry),fill:"none",stroke:"#558ca0","stroke-width":1,"vector-effect":"non-scaling-stroke"},westernWater);
for(const cell of DATA.westernTheater.cells){
 const node=element("path",{d:path({type:"Polygon",coordinates:[[...cell.vertices,cell.vertices[0]]]}),class:"hex"},westernGrid);
 node.addEventListener("click",()=>{if(!dragged)selectWestern(cell);});westernCells.set(cell.id,node);
}
for(const town of DATA.westernTheater.towns){
 const node=element("g",{"pointer-events":"none"},westernPlaces);
 element("circle",{r:2.5,fill:"#374b3e"},node);
 const label=element("text",{x:5,y:-4,fill:"#263c3b","font-size":12,"paint-order":"stroke",stroke:"#f5f4ed","stroke-width":3},node);label.textContent=town.name;
 westernLabels.push({town,node});
}
for(const site of DATA.westernTheater.sites){
 const node=element("g",{tabindex:0,role:"button","aria-label":site.name+" historical "+site.kind},westernSites);
 element("rect",{x:-6,y:-6,width:12,height:12,fill:site.kind==="port"?"#71384c":"#99651f",stroke:"#fff","stroke-width":1},node);
 const label=element("text",{x:9,y:4,fill:"#553923","font-size":13,"paint-order":"stroke",stroke:"#fff9e9","stroke-width":3},node);label.textContent=site.name;
 function inspect(){const detail=document.getElementById("westernDetail");detail.replaceChildren();const p=document.createElement("p");p.textContent=site.name+" · "+site.kind+". "+site.note+" "+site.period+". Approximate town center, not a facility position.";detail.append(p);const a=document.createElement("a");a.href=site.source;a.target="_blank";a.rel="noopener";a.textContent="U.S. Army official history: source";detail.append(a);}
 node.addEventListener("click",()=>{if(!dragged)inspect();});node.addEventListener("keydown",e=>{if(e.key==="Enter"){inspect();}});siteLabels.push({site,node});
}
function selectWestern(cell){
 if(westernSelected)westernCells.get(westernSelected.id).classList.remove("selected");
 westernSelected=cell;westernCells.get(cell.id).classList.add("selected");
 document.getElementById("westernDetail").textContent="Sector "+cell.id+" · 26 km across. Settlements: "+(cell.towns.join(", ")||"None in the reference source")+". Terrain and route availability are not adjudicated at this resolution.";
 document.getElementById("savePlan").disabled=false;
}
function planKey(){return "western-plans-v1-"+seed;}
function readPlans(){try{const p=JSON.parse(localStorage.getItem(planKey())||"[]");return Array.isArray(p)?p.filter(n=>typeof n.text==="string"&&typeof n.cellId==="string"):[];}catch{return [];}}
function showPlans(){const list=document.getElementById("planList");list.replaceChildren();for(const item of readPlans()){const b=document.createElement("button");b.textContent=item.cellId+": "+item.text;b.style.display="block";b.style.margin="6px 0";b.onclick=()=>{const c=DATA.westernTheater.cells.find(c=>c.id===item.cellId);if(c){selectWestern(c);view=[c.x-180,c.y-150,360,300];renderView();}};list.append(b);}}
document.getElementById("savePlan").onclick=()=>{if(!westernSelected)return;const text=document.getElementById("planText").value.trim();if(!text)return;const plans=readPlans();plans.push({cellId:westernSelected.id,text:text.slice(0,500)});try{localStorage.setItem(planKey(),JSON.stringify(plans));document.getElementById("planText").value="";showPlans();document.getElementById("planNotice").textContent="Planning note saved for this campaign. No order issued.";}catch{document.getElementById("planNotice").textContent="Browser storage is unavailable. Copy the note before leaving.";}};
for(const mode of ["geography","logistics","planning"])document.getElementById("mode-"+mode).onclick=()=>{westernMode=mode;renderView();};
function updateWestern(){
 const active=activeTheater&&activeTheater.id==="western";
 westernLayer.style.display=active?"":"none";
 theaterFocus.style.display=activeTheater?"":"none";
 if(activeTheater){
  const footprint=active?westernFootprintPath:path({type:"Polygon",coordinates:[activeTheater.boundary]});
  theaterPerimeter.setAttribute("d",footprint);
  theaterGridBoundary.setAttribute("d",footprint);
  // Extend beyond the viewBox to cover letterboxing and remain seamless when panning.
  const [x,y,w,h]=view,pad=Math.max(w,h)*2;
  theaterShade.setAttribute("d",path({type:"Polygon",coordinates:[[[x-pad,y-pad],[x+w+pad,y-pad],[x+w+pad,y+h+pad],[x-pad,y+h+pad],[x-pad,y-pad]]]})+footprint);
 }
 document.getElementById("westernWorkspace").style.display=active?"":"none";
 document.getElementById("shell").style.gridTemplateColumns=active?"350px 1fr":"";
 for(const id of ["pilotSection","coarseSelection"])document.getElementById(id).style.display=active?"none":"";
 if(activeTheater&&!active)grid.setAttribute("clip-path","url(#theater-grid-clip)");
 else grid.removeAttribute("clip-path");
 grid.style.display=active||document.getElementById("gridToggle").checked===false?"none":"";
 document.getElementById("gridScale").textContent=active?"26 km theater sectors, anchored to shared coordinates. Reference detail, not tactical terrain.":"Approximately 104 km across each hex. Coastlines remain geographic rather than snapping to hex edges.";
 if(!active){water.style.display=document.getElementById("riverToggle").checked===false?"none":"";return;}
 cities.style.display="none";water.style.display="none";
 westernWater.style.display=document.getElementById("riverToggle").checked===false?"none":"";
 westernGrid.style.display=document.getElementById("gridToggle").checked===false?"none":"";
 westernSites.style.display=westernMode==="logistics"?"":"none";
 westernPlaces.style.display=westernMode==="logistics"||document.getElementById("cityToggle").checked===false?"none":"";
 document.getElementById("planPanel").style.display=westernMode==="planning"?"":"none";
 document.getElementById("westernModeSummary").textContent={geography:"26 km sectors · settlements · river barriers. Select a sector to inspect or plan.",logistics:"1944–1945 logistics reference: ports, transfer points and depot areas. Click a square for its source. These are not 1939 operational states.",planning:"Select a sector and record a planning note. Notes belong to this campaign; they do not execute orders."}[westernMode];
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
