/* Logic smoke test with a minimal DOM double; not a browser rendering test. */
const fs=require("node:fs"),vm=require("node:vm"),assert=require("node:assert/strict"),{webcrypto}=require("node:crypto");
const html=fs.readFileSync(__dirname+"/index.html","utf8"),script=html.match(/<script>([\s\S]*)<\/script>/)[1];
const ids=new Map(), polygons=[], saved=new Map(), blobs=[];
class Element{
 constructor(){this.attrs={};this.children=[];this.style={};this.events={};this.clientWidth=1200;this.clientHeight=900;this.classList={add(){},remove(){}};}
 setAttribute(k,v){this.attrs[k]=v;} removeAttribute(k){delete this.attrs[k];} append(e){this.children.push(e);} addEventListener(k,f){this.events[k]=f;}
 replaceChildren(){this.children=[];} click(){if(this.onclick)this.onclick();}
}
const document={getElementById(id){if(!ids.has(id))ids.set(id,new Element());return ids.get(id);},createElement(){return new Element();},
createElementNS(ns,tag){const e=new Element();if(tag==="polygon")polygons.push(e);return e;}};
const context=vm.createContext({document,window:{addEventListener(){}},crypto:webcrypto,TextEncoder,Uint8Array,Uint32Array,Blob,
 localStorage:{getItem:k=>saved.get(k),setItem:(k,v)=>saved.set(k,v)},URL:{createObjectURL(b){blobs.push(b);return"blob:test";},revokeObjectURL(){}},setTimeout:f=>f(),console});
vm.runInContext(script,context);
(async()=>{
 assert.equal(polygons.length,2550);
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
 ids.get("theaterChoice").onchange({target:{value:"western"}});ids.get("enterTheater").onclick();
 assert.equal(ids.get("westernWorkspace").style.display,"");
 assert.equal(vm.runInContext("westernFocus.style.display",context),"");
 assert.equal(vm.runInContext('westernFocus.attrs["pointer-events"]',context),"none");
 assert.equal(vm.runInContext('westernShade.attrs["fill-rule"]',context),"evenodd");
 const shadeBefore=vm.runInContext("westernShade.attrs.d",context);
 ids.get("zoomIn").onclick();assert.notEqual(vm.runInContext("westernShade.attrs.d",context),shadeBefore);
 assert.equal(vm.runInContext("grid.style.display",context),"none");
 assert.ok(vm.runInContext("westernCells.size",context)>1000);
 ids.get("mode-logistics").onclick();assert.equal(vm.runInContext("westernSites.style.display",context),"");
 vm.runInContext('siteLabels[0].node.events.click()',context);assert.ok(ids.get("westernDetail").children.length>=2);
 ids.get("mode-planning").onclick();
 vm.runInContext('selectWestern(DATA.westernTheater.cells[0])',context);
 document.getElementById("planText").value="Assess supply access";ids.get("savePlan").onclick();
 assert.equal(vm.runInContext("readPlans()[0].text",context),"Assess supply access");
 const notesSeed=ids.get("seed").value;
 ids.get("newCampaign").onclick();assert.equal(vm.runInContext("readPlans().length",context),0);
 vm.runInContext('seed='+JSON.stringify(notesSeed)+';saveSeed();renderView()',context);
 assert.equal(vm.runInContext("readPlans().length",context),1);
 ids.get("returnEurope").onclick();assert.equal(ids.get("westernWorkspace").style.display,"none");
 assert.equal(vm.runInContext("westernLayer.style.display",context),"none");assert.equal(vm.runInContext("westernFocus.style.display",context),"none");
 const originalSeed=ids.get("seed").value;
 const originalHash=vm.runInContext("DATA.metadata.baseHash",context);
 await vm.runInContext("select(DATA.cells[20])",context);
 const originalSelection=vm.runInContext("selected.id",context);
 ids.get("theaterToggle").onchange({target:{checked:true}});
 assert.equal(vm.runInContext("theaterAreas.style.display",context),"");
 for(const id of ["western","eastern","mediterranean","northern"]){
  ids.get("theaterChoice").onchange({target:{value:id}});
  assert.equal(ids.get("enterTheater").disabled,false);
  ids.get("enterTheater").onclick();
  assert.equal(vm.runInContext("activeTheater.id",context),id);
  assert.equal(ids.get("returnEurope").disabled,false);
  assert.equal(vm.runInContext("railTiers[1].style.display",context),"");
  assert.equal(vm.runInContext("theaterAreas.style.display",context),"none");
  assert.equal(ids.get("seed").value,originalSeed);
  assert.equal(vm.runInContext("DATA.metadata.baseHash",context),originalHash);
  assert.equal(vm.runInContext("selected.id",context),originalSelection);
  ids.get("returnEurope").onclick();
  assert.equal(vm.runInContext("activeTheater",context),null);
  assert.equal(ids.get("map").attrs.viewBox,"-2400 -2500 5200 4600");
 }
 ids.get("theaterChoice").onchange({target:{value:""}});assert.equal(ids.get("enterTheater").disabled,true);
 vm.runInContext('theaterNodes.get("western").events.click()',context);
 assert.equal(ids.get("theaterChoice").value,"western");
 const token1=await vm.runInContext("localSeed(DATA.cells[100])",context);
 const token2=await vm.runInContext("localSeed(DATA.cells[100])",context);assert.equal(token1,token2);
 await vm.runInContext("select(DATA.cells[100])",context);assert.equal(ids.get("exportHex").disabled,false);
 await ids.get("exportHex").onclick();const cell=JSON.parse(await blobs.pop().text());assert.equal(cell.mapSeed,seed);assert.equal(cell.refinementSeed,token1);
 ids.get("newCampaign").onclick();assert.notEqual(ids.get("seed").value,seed);
 const token3=await vm.runInContext("localSeed(DATA.cells[100])",context);assert.notEqual(token1,token3);
 ids.get("exportCampaign").onclick();const out=JSON.parse(await blobs.pop().text());assert.equal(out.cells.length,2550);assert.equal(out.researchTransport,undefined);assert.equal(out.historicalPilot,undefined);assert.equal(out.historicalRailNetwork,undefined);assert.equal(out.westernTheater,undefined);assert.ok(Array.isArray(out.campaign.planningNotes));assert.ok(out.features.every(f=>!["roads","railways"].includes(f.properties.layer)));assert.equal(out.metadata.transportBaseline.baselineDate,"1939-09-01");
 assert.equal(out.campaign.mapSeed,ids.get("seed").value);assert.equal(out.campaign.theaterDefinitions.theaters.length,4);
 console.log("Viewer logic passed: initialization, zoom/reset, selection, stable seeds, new campaign and both exports.");
})().catch(e=>{console.error(e);process.exitCode=1;});
