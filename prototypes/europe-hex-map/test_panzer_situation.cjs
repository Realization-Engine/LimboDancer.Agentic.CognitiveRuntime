const fs=require("node:fs"),vm=require("node:vm"),assert=require("node:assert/strict"),path=require("node:path");
const ctx=vm.createContext({window:{}});for(const f of ["panzer-situation-data.js","panzer-situation-state.js"])vm.runInContext(fs.readFileSync(path.join(__dirname,f),"utf8"),ctx);
const d=ctx.window.PANZER_SITUATION_DATA,m=vm.runInContext("PanzerSituationModel",ctx),s=m.create(d);
assert.equal(d.instances.length,76);assert.equal(d.counters.length,21);assert.equal(d.turnLimit,15);assert.equal(d.situation.printedDate,"1944-06-29");
for(const side of ["Allied","German"])assert.equal(d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side===side).length,d.totals[side]);
for(const c of d.counters)assert.ok(fs.existsSync(path.join(__dirname,"assets/panzer-leader-04",c.artwork)));
assert.throws(()=>m.place(s,d,"de-rifle-01","A",.05,.001),/interior hex/);assert.throws(()=>m.next(s,d),/Place every/);assert.throws(()=>m.place(s,d,"us-rifle-01","C",.5,.5),/active side/);assert.throws(()=>m.place(s,d,"de-rifle-01","C",.5,.5),/assigned board/);assert.throws(()=>m.place(s,d,"de-rifle-01","A",NaN,.5),/point/);
for(const side of ["German","Allied"]){for(const i of d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side===side))m.place(s,d,i.id,d.setupBoards[side],.25,.75);m.next(s,d);}
assert.equal(s.stage,"ready");assert.ok(s.placements["us-rifle-01"].hexId);m.validate(JSON.parse(JSON.stringify(s)),d);
assert.throws(()=>m.draft(s,d,["us-rifle-01"],"Take crossing"),/both sides/);
const draft=m.draft(s,d,["us-rifle-01","de-rifle-01"],"Take crossing");assert.equal(draft.executable,false);assert.equal(draft.scenarioCard.format,"asl-scenario-card/1");assert.throws(()=>m.draft(s,d,["us-rifle-01","de-rifle-02"],"Take town"),/already held/);m.validate(s,d);
const bad=JSON.parse(JSON.stringify(s));bad.placements["de-rifle-01"].board="C";assert.throws(()=>m.validate(bad,d),/placement/);
const twice=JSON.parse(JSON.stringify(s));twice.engagements.push(twice.engagements[0]);assert.throws(()=>m.validate(twice,d),/duplicate/);
// Minimal UI double checks entry points, opening and return. Not browser rendering.
class E{constructor(tag){this.tag=tag;this.children=[];this.style={};this.attrs={};this.classList={add(){}};}addEventListener(name,fn,options){this["on"+name]=fn;this["options"+name]=options;}append(...x){this.children.push(...x);}prepend(...x){this.children.unshift(...x);}setAttribute(k,v){this.attrs[k]=v;}replaceChildren(){this.children=[];}showModal(){this.open=true;}close(){this.open=false;}}
const nodes={gameControls:new E("section"),workflowChoose:new E("section")},body=new E("body"),doc={body,createElement:t=>new E(t),createElementNS:(ns,t)=>new E(t),getElementById:id=>nodes[id]||(nodes[id]=new E("section"))};ctx.document=doc;ctx.localStorage={getItem:()=>null,setItem(){}};
vm.runInContext(fs.readFileSync(path.join(__dirname,"panzer-map-art.js"),"utf8"),ctx);
vm.runInContext(fs.readFileSync(path.join(__dirname,"panzer-situation-view.js"),"utf8"),ctx);
const descendants=e=>[e,...e.children.flatMap(descendants)];
assert.equal(nodes.workflowChoose.children.length,0);
assert.equal(nodes.gameControls.children.length,0);
const flow=ctx.window.CampaignSituationWorkflow;
flow.choose();assert.equal(flow.active,false); // Cannot open a child outside its parent.
ctx.activeRegion={id:d.parentCampaign.commandSnapshotId};ctx.regionalState={situations:[]};flow.renderParent();
const parentPanel=nodes.workflowInspector.children.find(e=>e.id==="campaignSituations");
const choose={onclick(){parentPanel.children.find(e=>e.attrs["data-situation-id"]===d.id).onclick();descendants(parentPanel).find(e=>e.textContent==="Review Situation Card").onclick();}};
const launcher={onclick(){choose.onclick();flow.openMap();}};
choose.onclick();assert.equal(flow.stage,"prepare");assert.equal(nodes["stage-maneuver"].disabled,true);flow.showStage("maneuver");assert.ok(!body.children[0].open);
flow.openMap();assert.equal(body.children[0].open,true);assert.equal(nodes["stage-maneuver"].disabled,false);
assert.ok(descendants(body.children[0]).some(e=>e.tag==="path"&&e.attrs["data-route-id"]));body.children[0].children[0].children.find(e=>e.textContent==="Return to Normandy campaign").onclick();assert.equal(body.children[0].open,false);assert.equal(flow.active,false);assert.equal(parentPanel.hidden,false);
console.log("Panzer Situation checks passed: roster, assets, setup order, saves, draft holds and UI entry/return.");

launcher.onclick();
assert.equal(descendants(body).filter(e=>e.attrs["data-hex-label"]).length,0);
assert.equal(descendants(body).filter(e=>e.attrs["data-illustration"]==="building").length,d.illustration.features.filter(f=>f.kind==="building").length);
assert.equal(descendants(body).filter(e=>e.className==="panzer-placed-count").length,21);
assert.ok(descendants(body).filter(e=>e.className==="panzer-placed-count").every(e=>e.textContent==="0 placed"));
const before=JSON.stringify(descendants(body).find(e=>e.tag==="svg"));
descendants(body).find(e=>e.textContent==="Show hex details").onclick();
assert.equal(descendants(body).filter(e=>e.attrs["data-hex-label"]).length,692);
descendants(body).find(e=>e.textContent==="Hide hex details").onclick();
assert.equal(JSON.stringify(descendants(body).find(e=>e.tag==="svg")),before);
console.log("Map illustration checks passed: buildings, optional annotations, deterministic redraw.");


const art=vm.runInContext("PanzerMapArt",ctx);
for(const angle of [90,270])for(const [u,v] of [[0,0],[1,1],[.25,.75]]){const out=art.project(angle,u,v),back=art.unproject(angle,out.x,out.y);assert.equal(back.u,u);assert.equal(back.v,v);}
assert.equal(descendants(body).filter(e=>e.attrs.class==="panzer-north-rose").length,1);
assert.ok(descendants(body).some(e=>e.attrs.transform==="translate(3138 0) rotate(90)"));
assert.ok(descendants(body).some(e=>e.attrs.transform==="translate(0 1130) rotate(-90)"));
const patches=art.seamWoods(d);assert.equal(patches.length,5);
for(const p of patches){const halves=descendants(body).filter(e=>e.attrs["data-joined-wood"]===p.id);assert.equal(halves.length,2);assert.equal(halves[0].attrs.points,halves[1].attrs.points);}
const roseNode=descendants(body).find(e=>e.attrs.class==="panzer-north-rose");assert.match(roseNode.attrs["aria-label"],/board A/);assert.equal(roseNode.children.find(e=>e.tag==="circle").attrs["fill-opacity"],"0.7");
const placeLabels=descendants(body).filter(e=>e.attrs["data-place-label"]);assert.equal(placeLabels.length,6);
for(const label of placeLabels){assert.equal(label.attrs["font-size"],"28.75");const angle=["Wiln","St. Athan"].includes(label.textContent)?90:-90;assert.ok(label.attrs.transform.startsWith("rotate("+angle+" "));}
descendants(body).find(e=>e.textContent==="Show source artwork").onclick();
assert.equal(descendants(body).filter(e=>e.tag==="image").length,2);
assert.ok(!descendants(body).some(e=>/^Inspect Board [AC]$/.test(e.textContent||"")));
console.log("Joined boards: rotations, coordinate round-trips, single rose and source images passed.");

for(const town of ["sambleu","caverge","kuhn","wiln","st-athan"]){assert.ok(d.illustration.features.filter(f=>f.kind==="building"&&f.id.startsWith(town+"-roof-")).length>=7,town+" needs its source-guided building artwork");}

// Exercise actual UI selection and click handlers, including occupied-hex placement.
let stored;ctx.localStorage.setItem=(key,value)=>{stored=JSON.parse(value);};
const clickText=text=>descendants(body).find(e=>e.tag==="button"&&e.textContent===text).onclick();
clickText("de-rifle-01");
const clickBoard=(index,x,y)=>{const map=descendants(body).filter(e=>e.className==="panzer-board panzer-board-joined")[index];map.children[0].getBoundingClientRect=()=>({left:100,top:200,width:1000,height:360});map.onclick({clientX:100+x*1000,clientY:200+y*360});};
clickBoard(0,.25,.25);assert.equal(stored.placements["de-rifle-01"].board,"A");
clickText("de-rifle-02");descendants(body).find(e=>e.className==="panzer-token"&&e.attrs["aria-label"]==="de-rifle-01").onclick({stopPropagation(){}});
assert.equal(stored.placements["de-rifle-02"].hexId,stored.placements["de-rifle-01"].hexId);
clickBoard(1,.25,.25);assert.match(descendants(body).find(e=>e.className==="panzer-placement-status").textContent,/assigned board/);
for(const i of d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side==="German")){clickText(i.id+(stored.placements[i.id]?" (placed)":""));clickBoard(0,.3,.4);}
clickText("Complete German setup");clickText("us-rifle-01");clickBoard(1,.4,.6);assert.equal(stored.placements["us-rifle-01"].board,"C");assert.ok(descendants(body).some(e=>e.className==="panzer-token"&&e.attrs["aria-label"]==="us-rifle-01"));
console.log("UI placement checks passed for both rotated boards, stacking and wrong-side feedback.");

// Wheel input changes the map size without rebuilding the selection or counter DOM.
const wheelMap=descendants(body).find(e=>e.className==="panzer-board panzer-board-joined"),zoomContent=descendants(body).find(e=>e.className==="panzer-joined"),zoomViewport=descendants(body).find(e=>e.className==="panzer-joined-scroll");
zoomViewport.scrollLeft=0;zoomViewport.scrollTop=0;zoomViewport.clientHeight=400;
wheelMap.children[0].getBoundingClientRect=()=>({left:100,top:200,width:parseFloat(zoomContent.style.width)*5,height:parseFloat(zoomContent.style.width)*1.8});
let prevented=0;const wheel=delta=>wheelMap.onwheel({clientX:350,clientY:290,deltaY:delta,deltaMode:0,preventDefault(){prevented++;},stopPropagation(){}});
wheel(-100);const factor=parseFloat(zoomContent.style.width)/100;assert.ok(factor>1);assert.ok(Math.abs(zoomViewport.scrollLeft-250*(factor-1))<1e-7);assert.ok(Math.abs(zoomViewport.scrollTop-90*(factor-1))<1e-7);assert.equal(wheelMap.optionswheel.passive,false);
for(let i=0;i<30;i++)wheel(-300);assert.equal(parseFloat(zoomContent.style.width),600);
for(let i=0;i<30;i++)wheel(300);assert.equal(parseFloat(zoomContent.style.width),100);assert.ok(prevented>0);
wheel(-100);clickText("Fit both boards");assert.equal(descendants(body).find(e=>e.className==="panzer-joined").style.width,"100%");assert.ok(stored.placements["us-rifle-01"]);
console.log("Wheel zoom checks passed: pointer anchor, limits, fit reset and retained placements.");

const panViewport=descendants(body).find(e=>e.className==="panzer-joined-scroll"),panDialog=body.children[0];
const event=extra=>({pointerId:7,clientX:300,clientY:250,button:0,preventDefault(){},stopPropagation(){},...extra});
panViewport.scrollLeft=200;panViewport.scrollTop=100;
panViewport.setPointerCapture=id=>{panViewport.capture=id;};panViewport.hasPointerCapture=id=>panViewport.capture===id;panViewport.releasePointerCapture=()=>{panViewport.capture=null;};
const beforePan=JSON.stringify(stored.placements);
panDialog.onkeydown(event({code:"Space"}));panViewport.onpointerdown(event({}));assert.equal(panViewport.capture,7);
panViewport.onpointermove(event({clientX:250,clientY:220}));assert.equal(panViewport.scrollLeft,250);assert.equal(panViewport.scrollTop,130);
panViewport.onpointerup(event({}));assert.equal(panViewport.capture,null);panDialog.onkeyup(event({code:"Space"}));
const boardForPan=descendants(body).find(e=>e.className==="panzer-board panzer-board-joined");boardForPan.onclick(event({}));assert.equal(JSON.stringify(stored.placements),beforePan);
panViewport.onpointerdown(event({button:1}));panViewport.onpointermove(event({clientX:280,clientY:240}));assert.equal(panViewport.scrollLeft,270);assert.equal(panViewport.scrollTop,140);panViewport.onpointercancel(event({}));assert.equal(panViewport.attrs["data-panning"],"false");
panDialog.onkeydown(event({code:"Space",target:{closest:()=>({})}}));assert.equal(panDialog.attrs["data-pan-ready"],"false");
panViewport.onpointerdown(event({button:0}));assert.equal(panViewport.capture,null);
console.log("Drag pan checks passed: both gestures, capture release, placement suppression and text-field Space.");

for(const type of d.counters){const count=d.instances.filter(i=>i.counterTypeId===type.id&&stored.placements[i.id]).length;const badge=descendants(body).find(e=>e.attrs["data-counter-type"]===type.id);assert.equal(badge.textContent,count+" placed");assert.equal(badge.attrs["data-complete"],String(count===type.quantity));assert.equal(badge.attrs["aria-label"],count+" of "+type.quantity+" placed");}
console.log("Collapsed roster counts passed for empty, partial and completed unit types.");

const sideGroup=side=>descendants(body).find(e=>e.className==="panzer-side-group"&&e.attrs["data-side"]===side);
assert.ok(sideGroup("German").open);assert.ok(sideGroup("Allied").open);
sideGroup("German").open=false;sideGroup("German").ontoggle();clickText("Enlarge boards");
assert.equal(sideGroup("German").open,false);assert.equal(sideGroup("Allied").open,true);
sideGroup("Allied").open=false;sideGroup("Allied").ontoggle();sideGroup("German").open=true;sideGroup("German").ontoggle();clickText("Fit both boards");
assert.equal(sideGroup("German").open,true);assert.equal(sideGroup("Allied").open,false);
assert.equal(sideGroup("German").children.filter(e=>e.tag==="details").length,d.counters.filter(c=>c.side==="German").length);
assert.equal(sideGroup("Allied").children[0].textContent,"American ("+d.totals.Allied+")");
console.log("Side roster groups independently collapse and retain their state across redraws.");

assert.throws(()=>m.launch(m.create(d),d),/Complete both sides/);
assert.equal(descendants(body).find(e=>e.textContent==="Launch situation game map").disabled,true);
for(const i of d.instances.filter(i=>d.counters.find(c=>c.id===i.counterTypeId).side==="Allied")){clickText(i.id+(stored.placements[i.id]?" (placed)":""));clickBoard(1,.4,.6);}
clickText("Complete Allied setup");assert.equal(descendants(body).find(e=>e.textContent==="Launch situation game map").disabled,false);
const deployment=JSON.stringify(stored.placements);clickText("Launch situation game map");assert.equal(body.children[0].attrs["data-map-mode"],"game");assert.equal(stored.gameLaunched,true);assert.ok(!descendants(body).some(e=>["Inspect Board A","Inspect Board C","Show source artwork","Show JSON terrain"].includes(e.textContent)));assert.equal(descendants(body).filter(e=>e.tag==="image").length,0);assert.ok(!descendants(body).find(e=>e.textContent==="Show hex details").disabled);assert.equal(descendants(body).filter(e=>e.className==="panzer-tray").length,0);assert.equal(descendants(body).filter(e=>e.className==="panzer-token").length,76);
clickBoard(0,.5,.5);assert.equal(JSON.stringify(stored.placements),deployment);
clickText("Review deployment");assert.equal(body.children[0].attrs["data-map-mode"],"setup");assert.ok(descendants(body).some(e=>e.textContent==="Show JSON terrain"));assert.equal(descendants(body).filter(e=>e.tag==="image").length,2);clickText("Resume game map");
flow.returnParent();flow.choose();flow.openMap();assert.equal(body.children[0].attrs["data-map-mode"],"game");assert.equal(JSON.stringify(stored.placements),deployment);
m.validate(stored,d);const invalidLaunch=m.create(d);invalidLaunch.gameLaunched=true;assert.throws(()=>m.validate(invalidLaunch,d),/Complete setup/);
console.log("Setup-to-game checks passed: launch gate, larger view, retained deployment, no placement in game, and resume.");

flow.returnParent();const finalPreview=descendants(parentPanel).find(e=>e.className==="campaign-situation-preview");assert.ok(finalPreview.children.some(e=>e.textContent==="Resume game map"));assert.ok(finalPreview.children.some(e=>e.textContent==="76 of 76 deployed"));assert.ok(nodes.map.children.find(e=>e.attrs.id==="campaignSituationMarker").attrs["aria-label"].includes("Game map opened"));
console.log("Situation preview resumes the saved game and reports the persisted deployment count.");

assert.equal(d.campaignAssignment.number,1);assert.ok(nodes.map.children.find(e=>e.attrs.id==="campaignSituationMarker").attrs["aria-label"].startsWith("Situation 01,"));assert.ok(descendants(parentPanel).some(e=>(e.textContent||"").startsWith("01 · St. Lo")));
