"use strict";
(()=>{
 const $=id=>document.getElementById(id),NS="http://www.w3.org/2000/svg",storageKey="campaign-atlas:terrain-colors:v1";
 // Semantic tokens wrap the existing renderer in this utility only.
 const tokens=[
 ["open","Open ground","#eee8bd"],["marsh","Marsh","#bfd0ac"],
 ["hill1","Elevation 1","#ded7ab"],["hill2","Elevation 2","#cec899"],["town","Town ground","#e9ddb9"],
 ["groundTexture","Ground texture","#837e50"],["woods","Woods: individual hex fill","#9caa73"],
 ["woodland","Woods: continuous woodland fill","#96a765"],["woodsEdge","Woods: outline","#7c915b"],
 ["trees","Woods: dark tree texture","#718b51"],["treesLight","Woods: light tree texture","#b7bd7b"],
 ["road","Road: center","#fcf8df"],["roadEdge","Road: edge","#65644f"],
 ["water","Stream: water","#79bac5"],["waterEdge","Stream: edge","#4a7781"],["bank","Stream: bank","#b9b78c"],
 ["roofBlue","Town: blue-gray roofs","#80929a"],["roofRed","Town: red roofs","#a24d32"],
 ["buildingEdge","Building: outline","#51584e"],["roofLine","Building: roof ridge","#e0dac4"],["shadow","Building: shadow","#6b654e"],
 ["bridge","Bridge: deck","#d6caaa"],["bridgeEdge","Bridge: edge","#625e50"],
 ["grid","Hex grid","#746f52"],["labels","Place names and hex text","#000000"],["labelHalo","Place name background","#eee8bd"],["hexHalo","Hex label background","#fffbe8"]
 ];
 tokens.push(...TerrainDemo.tokens);
 const defaults={...window.TERRAIN_PALETTE_DEFAULTS.colors},defaultGridOpacity=window.TERRAIN_PALETTE_DEFAULTS.gridOpacity,validColor=v=>typeof v==="string"&&/^#[0-9a-f]{6}$/i.test(v);
 let colors={...defaults},gridOpacity=defaultGridOpacity;
 try{const saved=JSON.parse(localStorage.getItem(storageKey)||"{}");for(const [k]of tokens)if(validColor(saved.colors?.[k]))colors[k]=saved.colors[k];if(Number.isFinite(saved.gridOpacity)&&saved.gridOpacity>=0&&saved.gridOpacity<=1)gridOpacity=saved.gridOpacity;}catch{}
 const save=()=>{try{localStorage.setItem(storageKey,JSON.stringify({colors,gridOpacity}));$("status").textContent="Terrain palette saved locally. Campaign maps are unchanged.";}catch{$("status").textContent="Local saving is unavailable. Export JSON to keep your palette.";}};
 const node=(tag,attrs={},text)=>{const n=document.createElementNS(NS,tag);for(const [k,v]of Object.entries(attrs))n.setAttribute(k,v);if(text!==undefined)n.textContent=text;return n;};
 for(const [k,label]of tokens){const o=document.createElement("option");o.value=k;o.textContent=label;$("feature").append(o);}
 let selected="open",view={x:0,y:0,w:3138,h:1130},space=false,drag=null,moved=false;
 const counterPositions=new Map();
 const positionKey=id=>$("board").value+":"+id;
 const updateCounter=(id,pos)=>{counterPositions.set(positionKey(id),pos);for(const svg of panes)for(const g of svg.querySelectorAll("[data-counter-id]"))if(g.getAttribute("data-counter-id")===id)g.setAttribute("transform",`translate(${pos.x} ${pos.y}) scale(${$("board").value==="demo"?.5:1})`);};
 const panes=[$("current"),$("edited")],records=new Map(),byColor=new Map(tokens.filter(([k])=>k!=="labelHalo").map(([k,,v])=>[v,k]));
 const data=window.PANZER_SITUATION_DATA;
 function apply(){
  for(const svg of panes){const edited=svg.id==="edited";for(const r of records.get(svg)||[]){r.node.setAttribute(r.attr,(edited?colors:defaults)[r.key]);if(r.key==="grid"&&r.attr==="stroke")r.node.setAttribute("stroke-opacity",edited?gridOpacity:defaultGridOpacity);}}
  $("opacityValue").textContent=Math.round(gridOpacity*100)+"%";
 }
 function selection(){
  $("feature").value=selected;$("color").value=colors[selected];$("hex").value=colors[selected];
  const label=tokens.find(t=>t[0]===selected)[1];$("description").textContent=label+". Current: "+defaults[selected]+". Edited: "+colors[selected]+".";
 }
 function render(){
  const demo=$("board").value==="demo",bd=demo?null:data.boards.find(b=>b.id===$("board").value),rot=demo?0:bd.id==="A"?90:270;
  for(const svg of panes){
   svg.replaceChildren();const scene=node("g",{transform:demo?"":rot===90?"matrix(0 1 -1 0 3138 0)":"matrix(0 -1 1 0 0 1130)"});
   if(demo)TerrainDemo.draw(scene,node,$("hexLabels").checked);else PanzerMapArt.draw(scene,data,bd,null,$("hexLabels").checked,true);svg.append(scene);
   // Each preview needs its own pattern ID so the edited texture never leaks into current.
   const prefix=svg.id+"-";
   for(const n of scene.querySelectorAll("[id]"))n.setAttribute("id",prefix+n.getAttribute("id"));
   for(const n of scene.querySelectorAll("*"))for(const attr of ["fill","stroke"]){const v=n.getAttribute(attr);if(v?.startsWith("url(#"))n.setAttribute(attr,v.replace("url(#","url(#"+prefix));}
   const bindings=[];
   for(const n of scene.querySelectorAll("*")){
    if(n.getAttribute("class")==="panzer-routes")n.setAttribute("pointer-events","auto");
    for(const attr of ["fill","stroke"]){
     const value=n.getAttribute(attr);let key=byColor.get(value);
     if(n.tagName.toLowerCase()==="text"&&attr==="fill"&&!value){key="labels";}
     if(n.hasAttribute("data-place-label")&&attr==="stroke")key="labelHalo";
     if(key){bindings.push({node:n,attr,key});if(svg.id==="edited"){const previous=n.getAttribute("data-edit-key");if(!previous||attr==="fill")n.setAttribute("data-edit-key",key);}}
    }
    if(!demo&&$("hexLabels").checked&&n.tagName.toLowerCase()==="text"){
     const x=n.getAttribute("x"),y=n.getAttribute("y");n.setAttribute("transform",`rotate(${rot===90?-90:90} ${x} ${y})`);
    }
   }
   records.set(svg,bindings);
   if($("counters").checked){
    const layer=node("g",{"aria-label":"Draggable approved ASL palette samples"});
    window.COUNTER_PALETTE_DEFAULTS.palettes.forEach((p,i)=>{
     const x=demo?350+(i%7)*400:210+(i%7)*450,y=demo?390+Math.floor(i/7)*430:350+Math.floor(i/7)*400,size=100,fill=`hsl(${p.hsl[0]} ${p.hsl[1]}% ${p.hsl[2]}%)`,ink=p.id==="ss"?"#ffffff":"#111a14";
     const pos=counterPositions.get(positionKey(p.id))||{x,y};counterPositions.set(positionKey(p.id),pos);
     const g=node("g",{transform:`translate(${pos.x} ${pos.y}) scale(${demo?.5:1})`,"data-counter-id":p.id,tabindex:0,role:"button","aria-label":p.name+". Drag to compare terrain; arrow keys move, Shift moves farther."});
     g.append(node("title",{},p.name+" - drag over terrain"));
     g.addEventListener("keydown",e=>{const steps={ArrowLeft:[-1,0],ArrowRight:[1,0],ArrowUp:[0,-1],ArrowDown:[0,1]},step=steps[e.key];if(!step)return;e.preventDefault();const a=counterPositions.get(positionKey(p.id)),amount=e.shiftKey?50:10;updateCounter(p.id,{x:Math.max(50,Math.min(3088,a.x+step[0]*amount)),y:Math.max(50,Math.min(1080,a.y+step[1]*amount))});});
     g.append(node("rect",{x:-50,y:-50,width:size,height:size,rx:3,fill:p.id==="cn"?window.COUNTER_PALETTE_DEFAULTS.palettes.find(q=>q.id==="su").css:fill,stroke:"#fffdf3","stroke-width":4}));
     if(p.id==="cn")g.append(node("rect",{x:-39,y:-39,width:78,height:78,fill}));
     g.append(node("rect",{x:-47,y:-47,width:94,height:94,fill:"none",stroke:"#263126"}),node("text",{x:0,y:-25,"text-anchor":"middle","font-size":18,fill:ink},p.id.toUpperCase()),node("path",{d:"M-22 -15H22V13H-22ZM-22 -15L22 13M22 -15L-22 13",fill:"none",stroke:ink,"stroke-width":2}),node("text",{x:0,y:36,"text-anchor":"middle","font-size":19,fill:ink},"6  4  6"));layer.append(g);
    });svg.append(layer);
   }
  }
  apply();applyView();
 }
 const applyView=()=>panes.forEach(svg=>svg.setAttribute("viewBox",`${view.x} ${view.y} ${view.w} ${view.h}`));
 const point=(svg,e)=>{const p=svg.createSVGPoint();p.x=e.clientX;p.y=e.clientY;return p.matrixTransform(svg.getScreenCTM().inverse());};
 function zoom(f,cx=view.x+view.w/2,cy=view.y+view.h/2){const w=Math.max(450,Math.min(5000,view.w*f)),r=w/view.w;view={x:cx+(view.x-cx)*r,y:cy+(view.y-cy)*r,w,h:view.h*r};applyView();}
 const stop=()=>{const active=drag;drag=null;if(active&&active.svg.hasPointerCapture(active.id))active.svg.releasePointerCapture(active.id);panes.forEach(svg=>{svg.setAttribute("data-panning","false");svg.setAttribute("data-counter-dragging","false");});};
 for(const svg of panes){
  svg.addEventListener("wheel",e=>{e.preventDefault();const p=point(svg,e);zoom(Math.exp(e.deltaY*.001),p.x,p.y);},{passive:false});
  svg.addEventListener("pointerdown",e=>{
   moved=false;const target=e.target.closest("[data-counter-id]");
   if(e.button===1||(e.button===0&&space)){e.preventDefault();drag={svg,id:e.pointerId,p:point(svg,e),startX:e.clientX,startY:e.clientY};svg.setPointerCapture(e.pointerId);svg.setAttribute("data-panning","true");}
   else if(e.button===0&&target){e.preventDefault();const id=target.getAttribute("data-counter-id"),p=point(svg,e),pos=counterPositions.get(positionKey(id));drag={svg,id:e.pointerId,counter:id,offset:{x:p.x-pos.x,y:p.y-pos.y}};svg.setPointerCapture(e.pointerId);svg.setAttribute("data-counter-dragging","true");}
  });
  svg.addEventListener("pointermove",e=>{if(!drag||drag.svg!==svg)return;if(!drag.counter&&!moved&&Math.hypot(e.clientX-drag.startX,e.clientY-drag.startY)<4)return;e.preventDefault?.();const p=point(svg,e);moved=true;if(drag.counter){updateCounter(drag.counter,{x:Math.max(50,Math.min(3088,p.x-drag.offset.x)),y:Math.max(50,Math.min(1080,p.y-drag.offset.y))});}else{view.x+=drag.p.x-p.x;view.y+=drag.p.y-p.y;applyView();}});
  svg.addEventListener("pointerup",stop);svg.addEventListener("pointercancel",stop);svg.addEventListener("lostpointercapture",()=>{if(drag?.svg===svg)stop();});svg.addEventListener("auxclick",e=>{if(e.button===1)e.preventDefault();});
 }
 $("edited").addEventListener("click",e=>{if(space||moved)return;const n=e.target.closest("[data-edit-key]");if(n){selected=n.getAttribute("data-edit-key");selection();}});
 document.addEventListener("keydown",e=>{if(e.code==="Space"&&!e.target.closest("input,select,button,a,summary")){e.preventDefault();space=true;}});
 document.addEventListener("keyup",e=>{if(e.code==="Space")space=false;});window.addEventListener("blur",()=>{space=false;stop();});
 $("feature").onchange=()=>{selected=$("feature").value;selection();};
 const change=value=>{if(!validColor(value)){$("status").textContent="Enter a six-digit hex color, such as #eee8bd.";return;}colors[selected]=value.toLowerCase();save();selection();apply();};
 $("color").oninput=()=>change($("color").value);$("hex").onchange=()=>change($("hex").value);
 $("gridOpacity").value=Math.round(gridOpacity*100);$("gridOpacity").oninput=()=>{gridOpacity=+$("gridOpacity").value/100;save();apply();};
 $("resetOne").onclick=()=>{colors[selected]=defaults[selected];save();selection();apply();};
 $("resetAll").onclick=()=>{colors={...defaults};gridOpacity=defaultGridOpacity;$("gridOpacity").value=Math.round(defaultGridOpacity*100);save();selection();apply();};
 $("resetCounters").onclick=()=>{stop();for(const p of window.COUNTER_PALETTE_DEFAULTS.palettes)counterPositions.delete(positionKey(p.id));render();};
 for(const id of ["board","counters","hexLabels"])$(id).onchange=render;
 $("compare").onchange=()=>{$("currentPanel").hidden=!$("compare").checked;$("previews").classList.toggle("single",!$("compare").checked);};
 $("fit").onclick=()=>{view={x:0,y:0,w:3138,h:1130};applyView();};$("in").onclick=()=>zoom(.8);$("out").onclick=()=>zoom(1.25);
 $("export").onclick=()=>{const result={schemaVersion:1,purpose:"Campaign Atlas terrain appearance",renderer:"PanzerMapArt",colors,gridOpacity,counterPaletteReference:"sources/counter-palette-defaults.json"};const url=URL.createObjectURL(new Blob([JSON.stringify(result,null,2)],{type:"application/json"})),a=document.createElement("a");a.href=url;a.download="campaign-terrain-palette.json";a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);};
 selection();render();
})();
