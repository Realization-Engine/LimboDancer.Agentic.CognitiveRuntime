const fs=require('node:fs'),vm=require('node:vm'),assert=require('node:assert/strict');
const code=fs.readFileSync(__dirname+'/image-export.js','utf8');
async function run(fail=false){
 const revoked=[],blobs=[],downloads=[],draws=[];let copied;
 const make=()=>({attrs:{viewBox:'10 20 300 200'},style:{values:{},setProperty(k,v){this.values[k]=v;}},setAttribute(k,v){this.attrs[k]=v;},querySelectorAll(){return [];}});
 const svg=make();svg.clientWidth=3000;svg.clientHeight=2000;svg.cloneNode=()=>copied=make();
 const canvas={getContext:()=>({fillRect(...x){draws.push(x);},drawImage(...x){draws.push(x.slice(1));}}),toBlob(cb,type){assert.equal(type,'image/png');cb(new Blob(['png'],{type}));}};
 const context=vm.createContext({Blob,getComputedStyle:()=>({getPropertyValue:p=>p==='display'?'none':p==='fill'?'rgb(1, 2, 3)':''}),
 XMLSerializer:class{serializeToString(c){assert.equal(c.attrs.viewBox,'10 20 300 200');assert.equal(c.style.values.display,'none');assert.equal(c.style.values.fill,'rgb(1, 2, 3)');return '<svg/>'; }},
 Image:class{set src(v){fail?this.onerror():this.onload();}},URL:{createObjectURL(b){blobs.push(b);return 'blob:'+blobs.length;},revokeObjectURL:u=>revoked.push(u)},setTimeout:f=>f(),
 document:{body:{append(){}},createElement:t=>t==='canvas'?canvas:{click(){downloads.push(this.download);},remove(){}}}});
 vm.runInContext(code,context);context.svg=svg;
 if(fail){await assert.rejects(vm.runInContext('exportMapPng(svg,"map.png")',context),/Could not render/);assert.deepEqual(revoked,['blob:1']);assert.equal(downloads.length,0);}
 else{await vm.runInContext('exportMapPng(svg,"map.png")',context);assert.equal(canvas.width,4096);assert.equal(canvas.height,2731);assert.deepEqual(downloads,['map.png']);assert.equal(blobs[1].type,'image/png');assert.deepEqual(new Set(revoked),new Set(['blob:1','blob:2']));assert.equal(draws.length,2);}
}
(async()=>{await run();await run(true);console.log('Image export checks passed: viewBox and styles, output cap, PNG download, failure cleanup.');})().catch(e=>{console.error(e);process.exitCode=1;});
