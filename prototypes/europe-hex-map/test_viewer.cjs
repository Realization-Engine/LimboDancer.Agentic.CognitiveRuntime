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
 for(const id of ["mountainToggle","forestToggle","roadToggle","railToggle"]){assert.ok(ids.has(id));ids.get(id).onchange({target:{checked:true}});ids.get(id).onchange({target:{checked:false}});}
 const seed=ids.get("seed").value;assert.match(seed,/^[a-f0-9]{32}$/);
 const before=ids.get("map").attrs.viewBox;ids.get("zoomIn").onclick();assert.notEqual(ids.get("map").attrs.viewBox,before);
 ids.get("reset").onclick();assert.equal(ids.get("map").attrs.viewBox,before);
 const token1=await vm.runInContext("localSeed(DATA.cells[100])",context);
 const token2=await vm.runInContext("localSeed(DATA.cells[100])",context);assert.equal(token1,token2);
 await vm.runInContext("select(DATA.cells[100])",context);assert.equal(ids.get("exportHex").disabled,false);
 await ids.get("exportHex").onclick();const cell=JSON.parse(await blobs.pop().text());assert.equal(cell.mapSeed,seed);assert.equal(cell.refinementSeed,token1);
 ids.get("newCampaign").onclick();assert.notEqual(ids.get("seed").value,seed);
 const token3=await vm.runInContext("localSeed(DATA.cells[100])",context);assert.notEqual(token1,token3);
 ids.get("exportCampaign").onclick();const out=JSON.parse(await blobs.pop().text());assert.equal(out.cells.length,2550);
 assert.equal(out.campaign.mapSeed,ids.get("seed").value);
 console.log("Viewer logic passed: initialization, zoom/reset, selection, stable seeds, new campaign and both exports.");
})().catch(e=>{console.error(e);process.exitCode=1;});
