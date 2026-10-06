/* Logic smoke test with a minimal DOM double; not a browser rendering test. */
const fs=require("node:fs"),vm=require("node:vm"),assert=require("node:assert/strict"),{webcrypto}=require("node:crypto");
const html=fs.readFileSync(__dirname+"/index.html","utf8"),script=fs.readFileSync(__dirname+"/app.js","utf8");
const mapData=fs.readFileSync(__dirname+"/map-data.js","utf8");
assert.ok(html.indexOf('<script src="map-data.js"></script>')<html.indexOf('<script src="app.js"></script>'));
assert.ok(!html.includes("application/json"));
assert.ok(html.includes('<option value="">Whole Map</option>'));
assert.ok(!html.includes('id="enterTheater"'));assert.ok(!html.includes('id="returnEurope"'));
assert.ok(!/<details[^>]*\sopen(?:\s|>)/.test(html));
assert.ok(Buffer.byteLength(html)<20000);
assert.ok(html.includes('<script src="app.js"></script>'));
assert.ok(html.includes('<link rel="stylesheet" href="site.css">'));
assert.ok(!html.includes('<script>'));
assert.equal(script.replace(/\r\n/g,"\n"),fs.readFileSync(__dirname+"/app.template.js","utf8").replace("__WESTERN_SCRIPT__",fs.readFileSync(__dirname+"/theater-view.js","utf8").replace(/^\uFEFF/,"" )).replace("__REGIONAL_SCRIPT__",fs.readFileSync(__dirname+"/regional-view.js","utf8")).replace(/\r\n/g,"\n"));

const ids=new Map(), polygons=[], saved=new Map(), blobs=[];
class Element{
 constructor(){this.attrs={};this.children=[];this.style={};this.events={};this.clientWidth=1200;this.clientHeight=900;this.classList={add(){},remove(){}};}
 setAttribute(k,v){this.attrs[k]=v;} removeAttribute(k){delete this.attrs[k];} append(...e){this.children.push(...e);} addEventListener(k,f){this.events[k]=f;}
 replaceChildren(){this.children=[];} click(){if(this.onclick)this.onclick();}
}
const document={getElementById(id){if(!ids.has(id))ids.set(id,new Element());return ids.get(id);},createElement(){return new Element();},
createElementNS(ns,tag){const e=new Element();if(tag==="polygon")polygons.push(e);return e;}};
const context=vm.createContext({document,window:{addEventListener(){}},crypto:webcrypto,TextEncoder,Uint8Array,Uint32Array,Blob,
 localStorage:{getItem:k=>saved.get(k),setItem:(k,v)=>saved.set(k,v),removeItem:k=>saved.delete(k),key:i=>[...saved.keys()][i],get length(){return saved.size;}},URL:{createObjectURL(b){blobs.push(b);return"blob:test";},revokeObjectURL(){}},setTimeout:f=>f(),console});
vm.runInContext(mapData,context);
vm.runInContext(fs.readFileSync(__dirname+"/regional-state.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/situation-state.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/tactical-reference.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/tactical-handoff.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/formation-state.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/formation-view.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/situation-view.js","utf8"),context);
vm.runInContext(fs.readFileSync(__dirname+"/theater-counters.js","utf8"),context);
vm.runInContext(script,context);
(async()=>{
 assert.equal(polygons.length,vm.runInContext("DATA.cells.length",context));
 const tiers=()=>JSON.parse(vm.runInContext("JSON.stringify(railTiers.map(n=>n.style.display))",context));
 assert.deepEqual(tiers(),["","none","none"]);
 vm.runInContext("view=[0,0,2400,2000];renderView()",context);assert.deepEqual(tiers(),["","","none"]);
 vm.runInContext("view=[0,0,900,800];renderView()",context);assert.deepEqual(tiers(),["","",""]);
 ids.get("historicalRailToggle").onchange({target:{checked:false}});
 ids.get("zoomOut").onclick();assert.equal(vm.runInContext("historicalRail.style.display",context),"none");
 ids.get("historicalRailToggle").onchange({target:{checked:true}});
 ids.get("reset").onclick();assert.deepEqual(tiers(),["","none","none"]);

 for(const id of ["mountainToggle","forestToggle","roadToggle","railToggle","historicalRailToggle"]){assert.ok(ids.has(id));ids.get(id).onchange({target:{checked:true}});ids.get(id).onchange({target:{checked:false}});}
 const features=JSON.parse(fs.readFileSync(__dirname+"/historical-transport-pilot.json","utf8")).features;
 assert.equal(ids.get("pilotRoutes").children.length,features.length);
 ids.get("pilotRegion").onchange({target:{value:"Italy"}});
 assert.equal(ids.get("pilotRoutes").children.length,features.filter(f=>f.properties.region==="Italy").length);
 ids.get("focusPilot").onclick();assert.ok(Number(ids.get("map").attrs.viewBox.split(" ")[2])<900);
 ids.get("pilotRoutes").children[3].onclick();assert.ok(ids.get("pilotEvidence").children.length>0);
 const pilotWidth=Number(ids.get("map").attrs.viewBox.split(" ")[2]);ids.get("zoomIn").onclick();assert.ok(Number(ids.get("map").attrs.viewBox.split(" ")[2])<pilotWidth);
 ids.get("pilotRegion").onchange({target:{value:"all"}});assert.equal(ids.get("pilotRoutes").children.length,features.length);
 ids.get("reset").onclick();
 // City visibility must survive the former 900 km cutoff and manual toggling.
 ids.get("cityToggle").checked=true;
 for(let i=0;i<8;i++)ids.get("zoomIn").onclick();
 assert.ok(Number(ids.get("map").attrs.viewBox.split(" ")[2])<900);
 assert.equal(vm.runInContext("cities.style.display",context),"");
 const cityScale=vm.runInContext("cityNodes[0].node.attrs.transform",context);
 ids.get("zoomIn").onclick();assert.notEqual(vm.runInContext("cityNodes[0].node.attrs.transform",context),cityScale);
 ids.get("cityToggle").checked=false;ids.get("cityToggle").onchange({target:ids.get("cityToggle")});
 ids.get("zoomOut").onclick();assert.equal(vm.runInContext("cities.style.display",context),"none");
 ids.get("cityToggle").checked=true;ids.get("cityToggle").onchange({target:ids.get("cityToggle")});
 ids.get("reset").onclick();assert.equal(vm.runInContext("cities.style.display",context),"");
 const seed=ids.get("seed").value;assert.match(seed,/^[a-f0-9]{32}$/);
 const before=ids.get("map").attrs.viewBox;ids.get("zoomIn").onclick();assert.notEqual(ids.get("map").attrs.viewBox,before);
 ids.get("reset").onclick();assert.equal(ids.get("map").attrs.viewBox,before);
 // Distinct Western presentation and campaign-local notes.
 ids.get("theaterChoice").onchange({target:{value:"western"}});
 assert.equal(ids.get("westernWorkspace").style.display,"");
 assert.equal(vm.runInContext("theaterFocus.style.display",context),"");
 assert.equal(vm.runInContext('theaterFocus.attrs["pointer-events"]',context),"none");
 assert.equal(vm.runInContext('theaterShade.attrs["fill-rule"]',context),"evenodd");
 const shadeBefore=vm.runInContext("theaterShade.attrs.d",context);
 ids.get("zoomIn").onclick();assert.notEqual(vm.runInContext("theaterShade.attrs.d",context),shadeBefore);
 assert.equal(vm.runInContext("grid.style.display",context),"none");
 assert.ok(vm.runInContext("westernCells.size",context)>1000);
 ids.get("mode-logistics").onclick();assert.equal(vm.runInContext("westernSites.style.display",context),"");
 vm.runInContext('siteLabels[0].node.events.click()',context);assert.ok(ids.get("westernDetail").children.length>=2);
 ids.get("mode-planning").onclick();
 vm.runInContext('selectWestern(DATA.westernTheater.cells[0])',context);
 document.getElementById("planText").value="Assess supply access";ids.get("savePlan").onclick();
 assert.equal(vm.runInContext("readPlans()[0].text",context),"Assess supply access");assert.equal(ids.get("planNotice").attrs["data-state"],"success");
 const notesSeed=ids.get("seed").value;
 ids.get("newCampaign").onclick();assert.equal(vm.runInContext("readPlans().length",context),0);
 vm.runInContext('seed='+JSON.stringify(notesSeed)+';saveSeed();renderView()',context);
 assert.equal(vm.runInContext("readPlans().length",context),1);
 ids.get("theaterChoice").onchange({target:{value:""}});assert.equal(ids.get("westernWorkspace").style.display,"none");
 assert.equal(vm.runInContext("westernLayer.style.display",context),"none");assert.equal(vm.runInContext("theaterFocus.style.display",context),"none");
 const originalSeed=ids.get("seed").value;
 const originalHash=vm.runInContext("DATA.metadata.baseHash",context);
 await vm.runInContext("select(DATA.cells[20])",context);
 const originalSelection=vm.runInContext("selected.id",context);
 ids.get("theaterToggle").onchange({target:{checked:true}});
 assert.equal(vm.runInContext("theaterAreas.style.display",context),"");
 for(const id of ["western","eastern","mediterranean","northern","northwest-africa","libya-egypt"]){
  ids.get("theaterChoice").onchange({target:{value:id}});


  assert.equal(vm.runInContext("activeTheater.id",context),id);ids.get("reset").onclick();assert.equal(vm.runInContext("activeTheater.id",context),id);
  assert.equal(vm.runInContext('grid.attrs["clip-path"]',context),undefined);
  assert.equal(vm.runInContext('theaterGridBoundary.attrs.d',context),vm.runInContext('theaterPerimeter.attrs.d',context));
  assert.equal(vm.runInContext("theaterFocus.style.display",context),"");
  assert.equal(vm.runInContext('theaterPerimeter.attrs.d',context),vm.runInContext('westernFootprintPath',context));
  assert.equal(ids.get("westernWorkspace").style.display,"");
  assert.equal(ids.get("workspaceTitle").textContent,vm.runInContext("activeTheater.name",context));
  assert.equal(vm.runInContext("grid.style.display",context),"none");
  assert.ok(vm.runInContext("westernCells.size",context)>100);
  assert.ok(vm.runInContext("westernLabels.length",context)>0);
  ids.get("mode-logistics").onclick();assert.equal(vm.runInContext("westernSites.style.display",context),"");
  assert.ok(vm.runInContext("siteLabels.length",context)>0);
  vm.runInContext("siteLabels[0].node.events.click()",context);assert.ok(ids.get("westernDetail").children.length>=2);
  ids.get("mode-planning").onclick();assert.equal(ids.get("planPanel").style.display,"");
  vm.runInContext("selectWestern(theaterData.cells[0])",context);
  ids.get("planText").value="Plan for "+id;ids.get("savePlan").onclick();
  assert.ok(vm.runInContext("readPlans().every(n=>n.theaterId===activeTheater.id)",context));
  assert.equal(vm.runInContext("readPlans().at(-1).text",context),"Plan for "+id);
  ids.get("gridToggle").checked=false;vm.runInContext("renderView()",context);assert.equal(vm.runInContext("westernGrid.style.display",context),"none");
  ids.get("gridToggle").checked=true;ids.get("cityToggle").checked=false;ids.get("mode-geography").onclick();assert.equal(vm.runInContext("westernPlaces.style.display",context),"none");
  ids.get("cityToggle").checked=true;
  const theaterShadeBefore=vm.runInContext("theaterShade.attrs.d",context);
  ids.get("zoomIn").onclick();assert.notEqual(vm.runInContext("theaterShade.attrs.d",context),theaterShadeBefore);

  assert.equal(vm.runInContext("railTiers[1].style.display",context),"");
  assert.equal(vm.runInContext("theaterAreas.style.display",context),"none");
  assert.equal(ids.get("seed").value,originalSeed);
  assert.equal(vm.runInContext("DATA.metadata.baseHash",context),originalHash);
  assert.equal(vm.runInContext("selected.id",context),originalSelection);
  ids.get("theaterChoice").onchange({target:{value:""}});
  assert.equal(vm.runInContext("activeTheater",context),null);
  assert.equal(vm.runInContext('grid.attrs["clip-path"]',context),undefined);
  assert.equal(vm.runInContext("theaterFocus.style.display",context),"none");
  assert.equal(ids.get("map").attrs.viewBox,"-2400 -2500 5200 5800");
 }
 ids.get("exportCampaign").onclick();const multi=JSON.parse(await blobs.pop().text());
 assert.equal(new Set(multi.campaign.planningNotes.map(n=>n.theaterId)).size,6);
 assert.equal(multi.theaterWorkspaces,undefined);
 // Legacy Western notes remain readable, and do not leak to another theater.
 const currentSeed=vm.runInContext("seed",context);
 saved.set("western-plans-v1-legacy-test",JSON.stringify([{cellId:"western:26km:0:0",text:"Legacy note"}]));
 vm.runInContext('seed="legacy-test"',context);
 assert.equal(vm.runInContext('allPlans()[0].theaterId',context),"western");
 vm.runInContext('seed='+JSON.stringify(currentSeed),context);
 ids.get("theaterChoice").onchange({target:{value:""}});
 vm.runInContext('theaterNodes.get("western").events.click()',context);
 assert.equal(ids.get("theaterChoice").value,"western");assert.equal(vm.runInContext("activeTheater.id",context),"western");ids.get("theaterChoice").onchange({target:{value:""}});
 const token1=await vm.runInContext("localSeed(DATA.cells[100])",context);
 const token2=await vm.runInContext("localSeed(DATA.cells[100])",context);assert.equal(token1,token2);
 await vm.runInContext("select(DATA.cells[100])",context);assert.equal(ids.get("exportHex").disabled,false);
 await ids.get("exportHex").onclick();const cell=JSON.parse(await blobs.pop().text());assert.equal(cell.mapSeed,seed);assert.equal(cell.refinementSeed,token1);
 ids.get("newCampaign").onclick();assert.notEqual(ids.get("seed").value,seed);
 const token3=await vm.runInContext("localSeed(DATA.cells[100])",context);assert.notEqual(token1,token3);
 ids.get("exportCampaign").onclick();const out=JSON.parse(await blobs.pop().text());assert.equal(out.cells.length,polygons.length);assert.equal(out.researchTransport,undefined);assert.equal(out.historicalPilot,undefined);assert.equal(out.historicalRailNetwork,undefined);assert.equal(out.westernTheater,undefined);assert.ok(Array.isArray(out.campaign.planningNotes));assert.ok(out.features.every(f=>!["roads","railways"].includes(f.properties.layer)));assert.equal(out.metadata.transportBaseline.baselineDate,"1939-09-01");
 assert.equal(out.campaign.mapSeed,ids.get("seed").value);assert.equal(out.campaign.theaterDefinitions.theaters.length,6);
 assert.equal(out.southernTransport,undefined);assert.equal(out.theaterWorkspaces,undefined);
 ids.get("southLater").checked=false;vm.runInContext("view=[0,0,600,600];renderView()",context);
 assert.equal(vm.runInContext(`southernNodes.filter(x=>x.f.properties.evidenceYear>1939 && x.node.style.display!=="none").length`,context),0);
 ids.get("southLater").checked=true;ids.get("southLater").onchange();
 assert.ok(vm.runInContext(`southernNodes.some(x=>x.f.properties.id==="na26" && x.node.style.display!=="none")`,context));
 vm.runInContext("returnEurope()",context);
 assert.equal(vm.runInContext(`southernNodes.filter(x=>x.f.properties.displayTier>0 && x.node.style.display!=="none").length`,context),0);
 ids.get("theaterChoice").onchange({target:{value:"western"}});
 ids.get("regionChoice").onchange({target:{value:"normandy-1944-06-08"}});
 assert.equal(vm.runInContext("activeRegion.id",context),"normandy-1944-06-08");
 assert.equal(ids.get("regionalWorkspace").style.display,"");
 assert.equal(ids.get("workspaceControls").style.display,"");
 assert.equal(ids.get("regionalHierarchy").children.length,0);
 assert.equal(ids.get("regionalBreadcrumb").children.length,1);
 vm.runInContext('regionalHQ="18-ir";regionalMission="1-18-approaches";regionalState.missions.find(m=>m.id==="18-advance").status="planned";renderRegional()',context);
 assert.ok(ids.get("regionalMissionDetail").children.some(n=>(n.textContent||"").includes("Engranville")));
 vm.runInContext('regionalHQ="first-army";regionalMission="beachhead";renderRegional()',context);
 ids.get("regional-assign").onclick();assert.equal(vm.runInContext("regionalState.missions[0].status",context),"assigned");
 ids.get("regionalAdvance").onclick();assert.equal(vm.runInContext("regionalState.missions[0].status",context),"received");
 vm.runInContext("regionalState=null;loadRegionalState()",context);assert.equal(vm.runInContext("regionalState.step",context),1);
 ids.get("reset").onclick();assert.equal(vm.runInContext("view.join(',')===activeRegion.view.join(',')",context),true);
 ids.get("regionChoice").onchange({target:{value:""}});assert.equal(vm.runInContext("activeRegion",context),null);
 assert.equal(ids.get("workspaceControls").style.display,"");
 ids.get("theaterChoice").onchange({target:{value:"eastern"}});assert.equal(ids.get("regionChoice").disabled,true);
 ids.get("exportCampaign").onclick();const regionalExport=JSON.parse(await blobs.pop().text());assert.equal(regionalExport.campaign.regionalExercise.step,1);assert.equal(regionalExport.regionalCampaign,undefined);
 ids.get("theaterChoice").onchange({target:{value:"western"}});
 ids.get("regionChoice").onchange({target:{value:"normandy-1944-06-08"}});
 vm.runInContext('regionalHQ="1-18";regionalMission="1-18-approaches";regionalState.missions.find(m=>m.id===regionalMission).status="planned";regionalSector=DATA.regionalCampaign.cells[0];renderRegional()',context);
 assert.equal(ids.get("regionalClear").disabled,false);assert.equal(ids.get("openSavedSituation").disabled,true);
 const createSituation=ids.get("situationPanel").children.find(n=>n.textContent==="Create Situation Card");assert.equal(createSituation.disabled,false);createSituation.onclick();
 assert.equal(ids.get("openSavedSituation").disabled,false);assert.equal(ids.get("savedSituationChoice").children.length,1);
 ids.get("situationPanel").children.find(n=>n.textContent==="Prepare two engagement drafts").onclick();
 assert.equal(vm.runInContext("regionalState.situations[0].engagements.length",context),2);
 ids.get("situationPanel").children.find(n=>n.textContent==="Export ASL handoff draft").onclick();
 const handoff=JSON.parse(await blobs.pop().text());assert.equal(handoff.executable,false);assert.equal(handoff.assetBindings.length,3);
 vm.runInContext('regionalState=null;loadRegionalState()',context);assert.equal(vm.runInContext('regionalState.situations[0].reservations.length',context),6);
 ids.get("situationPanel").children.find(n=>n.textContent==="Prepare catalog-backed tactical test").onclick();
 assert.equal(vm.runInContext('regionalState.situations[0].assets.length',context),15);
 ids.get("situationPanel").children.find(n=>n.textContent==="Export ASL test Scenario Card").onclick();
 const cardBytes=await blobs.pop().text();assert.equal(JSON.parse(cardBytes).sides.length,2);
 await ids.get("situationPanel").children.find(n=>n.textContent==="Export test identity manifest").onclick();
 const identityManifest=JSON.parse(await blobs.pop().text());assert.equal(identityManifest.cardText,cardBytes);assert.equal(identityManifest.assetBindings.length,7);assert.equal(identityManifest.campaignExecutable,false);
 vm.runInContext('regionalState=null;loadRegionalState()',context);assert.equal(vm.runInContext('regionalState.situations[0].tacticalTest.campaignAdmission',context),'blocked');
 ids.get("situationPanel").children.find(n=>n.textContent==="Open Situation").onclick();
 assert.equal(vm.runInContext('formationOpen',context),true);assert.equal(ids.get("map").style.display,"none");assert.equal(ids.get("formationControls").style.display,"");
 assert.equal(vm.runInContext('currentFormationSituation().formation.counters.length',context),4);
 const formationRoot=ids.get("formationMap"),stableHex=formationRoot.children.find(n=>n.attrs.class==="formation-hex"),stableCounter=formationRoot.children.find(n=>n.attrs.class==="formation-counter"),stableView=formationRoot.attrs.viewBox;
 stableCounter.onclick();assert.ok(formationRoot.children.includes(stableHex));assert.ok(formationRoot.children.includes(stableCounter));assert.equal(formationRoot.attrs.viewBox,stableView);
 let prevented=false;stableCounter.events.keydown({key:" ",preventDefault(){prevented=true;}});assert.equal(prevented,true);assert.ok(formationRoot.children.includes(stableCounter));
 ids.get("formationNext").onclick();ids.get("formationNext").onclick();assert.equal(vm.runInContext('currentFormationSituation().formation.phase',context),2);
 vm.runInContext('formationCounter=currentFormationSituation().formation.counters[0].id;var f=currentFormationSituation().formation;var c=f.cells.find(c=>c.id===f.counters[0].cellId);var dest=f.cells.find(t=>FormationModel.distance(c,t)===1&&!f.counters.some(u=>u.cellId===t.id));formationAction("move",dest.id)',context);
 assert.ok(formationRoot.children.includes(stableHex));assert.ok(formationRoot.children.includes(stableCounter));assert.equal(formationRoot.attrs.viewBox,stableView);
 vm.runInContext('regionalState=null;loadRegionalState()',context);assert.equal(vm.runInContext('currentFormationSituation().formation.phase',context),2);
 ids.get("formationBack").onclick();assert.equal(vm.runInContext('formationOpen',context),false);assert.equal(ids.get("map").style.display,"");
 vm.runInContext('regionalHQ="first-army";regionalMission="beachhead";renderRegional()',context);
 ids.get("openSavedSituation").onclick();assert.equal(vm.runInContext('formationOpen',context),true);assert.equal(vm.runInContext('regionalHQ',context),'1-18');
 assert.equal(ids.get("regionalClear").disabled,false);assert.equal(ids.get("openSavedSituation").disabled,true);
 ids.get("formationBack").onclick();assert.equal(ids.get("workspaceControls").style.display,"");

 ids.get("theaterChoice").onchange({target:{value:"western"}});
 assert.equal(ids.get("theaterCounterControls").style.display,"none");assert.equal(vm.runInContext('theaterCounterLayer.style.display',context),"none");
 ids.get("regionChoice").onchange({target:{value:"normandy-1944-06-08"}});
 assert.equal(ids.get("theaterCounterControls").style.display,"");assert.equal(vm.runInContext('theaterCounterNodes.length',context),3);
 assert.equal(vm.runInContext('theaterCounterNodes.every(n=>DATA.regionalCampaign.cells.some(c=>c.id===n.anchor.id))',context),true);
 ids.get("theaterCounterList").children[0].onclick();assert.ok(ids.get("theaterCounterDetail").children.some(n=>(n.textContent||"").includes('18th Infantry Regiment')));
 ids.get("focusTheaterCounters").onclick();assert.equal(vm.runInContext('view.join()===DATA.regionalCampaign.view.join()',context),true);
 const beforeCounterHQ=vm.runInContext('regionalHQ',context);ids.get("theaterCounterDetail").children.find(n=>n.textContent==="Show command workflow").onclick();assert.equal(vm.runInContext('regionalHQ',context),beforeCounterHQ);
 vm.runInContext('var counterClock=regionalState.clock.current;regionalState.clock.current=regionalState.clock.endExclusive;updateTheaterCounters()',context);assert.equal(ids.get("theaterCounterControls").style.display,"none");
 vm.runInContext('regionalState.clock.current=counterClock;updateTheaterCounters()',context);assert.equal(ids.get("theaterCounterControls").style.display,"");
 ids.get("regionChoice").onchange({target:{value:""}});assert.equal(ids.get("theaterCounterControls").style.display,"none");
 ids.get("regionChoice").onchange({target:{value:"normandy-1944-06-08"}});
 // Follow the visible guided controls from Army through Battalion.
 vm.runInContext('regionalState=RegionalModel.create(DATA.regionalCampaign,seed,DATA.metadata.baseHash);regionalHQ="first-army";regionalMission="beachhead";renderRegional()',context);
 assert.equal(ids.get("regionalHierarchy").children.length,0);
 assert.equal(ids.get("regionalText").style.display,"none");
 ids.get("regional-assign").onclick();ids.get("regionalAdvance").onclick();
 assert.equal(ids.get("regionalHierarchy").children.length,1);
 ids.get("regionalHierarchy").children[0].onclick();
 assert.equal(vm.runInContext('regionalHQ',context),'v-corps');
 assert.equal(ids.get("regionalHierarchy").children.length,0);
 assert.equal(ids.get("regionalMissions").children.length,1);
 for(const [missionTitle,childName] of [["Develop the inland advance","1st Infantry Division"],["Coordinate the 18th Infantry advance","18th Infantry Regiment"],["Plan the western approaches","1st Battalion, 18th Infantry"]]){
  ids.get("regionalText").value="Coordinate objectives and support";ids.get("regional-plan").onclick();
  ids.get("regionalMissions").children.find(b=>b.textContent.startsWith(missionTitle)).onclick();
  ids.get("regional-assign").onclick();ids.get("regionalAdvance").onclick();
  const child=ids.get("regionalHierarchy").children.find(b=>b.textContent===childName);assert.ok(child);assert.equal(child.disabled,false);child.onclick();
  assert.equal(ids.get("regionalHierarchy").children.length,0);
 }
 assert.equal(vm.runInContext('regionalHQ',context),'1-18');assert.equal(ids.get("regionalBreadcrumb").children.length,5);
 ids.get("regionalBreadcrumb").children[2].onclick();assert.equal(vm.runInContext('regionalHQ',context),'1-id');
 assert.equal(ids.get("regionalHierarchy").children.length,3);
 for(const name of ['16th Infantry Regiment','18th Infantry Regiment','26th Infantry Regiment']){
  const child=ids.get("regionalHierarchy").children.find(b=>b.textContent.startsWith(name));assert.ok(child);assert.equal(child.disabled,name!=='18th Infantry Regiment');
 }
 ids.get("theaterCounterList").children[1].onclick();
 assert.ok(ids.get("theaterCounterDetail").children.some(n=>(n.textContent||'').includes('175th Infantry Regiment')));
 assert.ok(ids.get("theaterCounterDetail").children.some(n=>(n.textContent||'').includes('9 infantry battalions')));

 const clearSeed=vm.runInContext('seed',context);
 saved.set('regional-exercise-v1-'+clearSeed+'-old-reference','old');
 saved.set('regional-exercise-v1-other-campaign-old-reference','keep');
 ids.get("regionalText").value="Unsaved draft";
 ids.get("regionalClear").onclick();
 assert.equal(vm.runInContext('regionalHQ',context),'first-army');
 assert.equal(vm.runInContext('regionalState.step',context),0);
 assert.equal(vm.runInContext('regionalState.messages.length',context),0);
 assert.equal(vm.runInContext('regionalState.situations',context),undefined);
 assert.equal(ids.get("openSavedSituation").disabled,true);assert.equal(ids.get("savedSituationChoice").children.length,0);
 assert.equal(vm.runInContext('regionalState.missions.every(m=>m.status==="draft"&&!m.plan&&!m.report&&!m.assessment)',context),true);
 assert.equal(vm.runInContext('seed',context),clearSeed);assert.equal(ids.get("regionalText").value,"");
 assert.equal(vm.runInContext('regionalSector',context),null);
 assert.ok(![...saved.keys()].some(k=>k.startsWith('regional-exercise-v1-'+clearSeed+'-')));
 assert.equal(saved.get('regional-exercise-v1-other-campaign-old-reference'),'keep');
 vm.runInContext('loadRegionalState()',context);assert.equal(vm.runInContext('regionalState.step',context),0);
 assert.equal(ids.get("regional-assign").style.display,"");
 console.log("Viewer logic passed: initialization, zoom/reset, selection, stable seeds, new campaign and both exports.");
})().catch(e=>{console.error(e);process.exitCode=1;});
