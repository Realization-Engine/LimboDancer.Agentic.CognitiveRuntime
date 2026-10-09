"use strict";
(()=>{
 const NS="http://www.w3.org/2000/svg",key="campaign-atlas:counter-color-test:v1",$=id=>document.getElementById(id);
 // User-approved screen defaults from sources/counter-palette-defaults.json.
 // Family bounds are proposed screen ranges, not historical print standards.
 const rows=[
 ["de","German","Light blue",[212,70,79],[195,215,30,70,65,85]],
 ["su","Soviet","Ochre brown",[34,60,55],[25,43,35,65,48,72]],
 ["us","American / US Marines","Yellow-green",[82,48,57],[72,100,30,65,45,70]],
 ["uk","British / Commonwealth","Tan / khaki",[38,58,73],[30,46,25,60,62,82]],
 ["fr","French","Medium blue",[207,57,59],[198,217,38,72,48,68]],
 ["it","Italian","Neutral gray",[210,4,67],[195,220,0,10,55,77]],
 ["fi","Finnish","Light gray",[210,7,82],[195,220,0,12,73,90]],
 ["am","Allied Minors","Mint green: Poland, Belgium, Netherlands, Norway, Denmark, Yugoslavia, Greece",[151,43,75],[135,165,28,58,66,83]],
 ["ax","Axis Minors","Medium green: Hungary, Romania, Bulgaria, Slovakia, Croatia",[129,36,56],[115,145,25,55,43,64]],
 ["jp","Japanese","Golden yellow",[49,76,65],[43,56,55,88,56,76]],
 ["cn","Nationalist Chinese","Blue center / brown surround",[207,52,69],[198,216,35,68,57,80]],
 ["cc","Communist Chinese","Soviet brown",[34,59,62],[25,43,35,65,48,72]],
 ["pa","Partisans","Soviet brown",[34,64,71],[25,43,35,65,48,72]],
 ["ss","SS, black alternative","Black with white symbols; German blue is also applicable",[210,3,18],[195,220,0,8,10,28]],
 ];
 const palettes=rows.map(([id,name,family,hsl,bounds])=>({id,name,family,hsl:[...hsl],initial:[...hsl],bounds}));
 const node=(tag,attrs={},text)=>{const n=document.createElementNS(NS,tag);Object.entries(attrs).forEach(([k,v])=>n.setAttribute(k,v));if(text!==undefined)n.textContent=text;return n;};
 const color=p=>`hsl(${p.hsl[0]} ${p.hsl[1]}% ${p.hsl[2]}%)`;
 const valid=(v,p)=>Array.isArray(v)&&v.length===3&&v.every((n,i)=>Number.isFinite(n)&&n>=p.bounds[i*2]&&n<=p.bounds[i*2+1]);
 try{const saved=JSON.parse(localStorage.getItem(key)||"{}");for(const p of palettes)if(valid(saved[p.id],p))p.hsl=saved[p.id];}catch{}
 const save=()=>{try{localStorage.setItem(key,JSON.stringify(Object.fromEntries(palettes.map(p=>[p.id,p.hsl]))));$("status").textContent="Palette saved locally.";}catch{$("status").textContent="Local saving unavailable. Export the palette to keep your choices.";}};
 for(const p of palettes){const o=document.createElement("option");o.value=p.id;o.textContent=p.name;$("nationality").append(o);}
 let selected="de",view={x:0,y:0,w:3138,h:1130},space=false,drag=null;
 const svg=$("testMap"),terrain=node("g",{transform:"matrix(0 1 -1 0 3138 0)"}),d=window.PANZER_SITUATION_DATA,bd=d.boards.find(b=>b.id==="A");
 PanzerMapArt.draw(terrain,d,bd,null,false,true);svg.append(terrain);
 const defs=node("defs"),filter=node("filter",{id:"counter-shadow",x:"-30%",y:"-30%",width:"170%",height:"170%"});
 filter.append(node("feDropShadow",{dx:2,dy:3,stdDeviation:2,"flood-opacity":.4}));defs.append(filter);svg.append(defs);
 const layer=node("g");svg.append(layer);
 // Stable, geographically spread samples, snapped to actual interior board hexes.
 const used=new Set(),positions=palettes.map((p,i)=>{
  const tx=230+(i%6)*530,ty=230+Math.floor(i/6)*340;
  const choices=bd.hexes.filter(h=>h.imageCenter.u>.1&&h.imageCenter.u<.9&&h.imageCenter.v>.04&&h.imageCenter.v<.96&&!used.has(h.id));
  choices.sort((a,b)=>{const score=h=>{const q=PanzerMapArt.project(90,h.imageCenter.u,h.imageCenter.v);return(q.x*3138-tx)**2+(q.y*1130-ty)**2;};return score(a)-score(b);});
  const h=choices[0];used.add(h.id);const q=PanzerMapArt.project(90,h.imageCenter.u,h.imageCenter.v);return{x:q.x*3138,y:q.y*1130,hex:h};
 });
 function counter(p,x,y,size,interactive=true){
  const g=node("g",{transform:`translate(${x} ${y})`,class:"sample"});
  if(interactive){g.setAttribute("tabindex","0");g.setAttribute("role","button");g.setAttribute("aria-label",p.name+", select color");const choose=()=>{selected=p.id;$("nationality").value=selected;controls();draw();};g.addEventListener("click",()=>{if(!space&&!moved)choose();});g.addEventListener("keydown",e=>{if(e.key==="Enter"||e.key===" "){e.preventDefault();choose();}});}
  g.append(node("title",{},p.name+" | "+color(p)+" | Dummy infantry sample"));
  const s=size,white=p.id==="ss",ink=white?"#fff":"#111a14",body=node("g",$("edge").checked?{filter:"url(#counter-shadow)"}:{});
  body.append(node("rect",{x:-s/2,y:-s/2,width:s,height:s,rx:3,fill:p.id==="cn"?color(palettes.find(q=>q.id==="su")):color(p),stroke:$("edge").checked?"#fffdf3":"#263126","stroke-width":$("edge").checked?4:1}));
  if(p.id==="cn")body.append(node("rect",{x:-s*.39,y:-s*.39,width:s*.78,height:s*.78,fill:color(p)}));
  body.append(node("rect",{x:-s/2+3,y:-s/2+3,width:s-6,height:s-6,rx:2,fill:"none",stroke:"#263126","stroke-width":1}));
  body.append(node("text",{x:0,y:-s*.27,"text-anchor":"middle","font-size":s*.16,fill:ink,"font-family":"monospace"},p.id.toUpperCase()));
  body.append(node("rect",{x:-s*.23,y:-s*.16,width:s*.46,height:s*.28,fill:"none",stroke:ink,"stroke-width":2}),node("path",{d:`M${-s*.23} ${-s*.16} L${s*.23} ${s*.12} M${s*.23} ${-s*.16} L${-s*.23} ${s*.12}`,stroke:ink,"stroke-width":2}));
  body.append(node("text",{x:0,y:s*.33,"text-anchor":"middle","font-size":s*.17,fill:ink,"font-family":"monospace"},"6  4  6"));
  g.append(body,node("rect",{x:-s/2-5,y:-s/2-5,width:s+10,height:s+10,rx:5,fill:"none",class:"selection",stroke:interactive&&p.id===selected?"#b77725":"none","stroke-width":4}));
  return g;
 }
 function draw(){
  layer.replaceChildren();const size=+$("size").value;
  palettes.forEach((p,i)=>{const q=positions[i],g=counter(p,q.x,q.y,size);if($("names").checked)g.append(node("text",{x:0,y:size/2+30,"text-anchor":"middle","font-size":22,fill:"#1e2719",stroke:"#fffbee","stroke-width":5,"paint-order":"stroke"},p.name));layer.append(g);});
  const p=palettes.find(p=>p.id===selected);$("value").textContent=color(p);$("proof").replaceChildren();
  for(const [label,bg]of[["Open","#eee8bd"],["Woods","#879b61"],["Town","#e9ddb9"]]){
   const proof=node("svg",{viewBox:"0 0 100 115",role:"img","aria-label":p.name+" on "+label});
   proof.append(node("rect",{width:100,height:115,fill:bg}));
   if(label==="Woods")for(let i=0;i<16;i++)proof.append(node("circle",{cx:(i*29)%100,cy:(i*47)%100,r:7,fill:"#718b51"}));
   if(label==="Town")for(let i=0;i<9;i++)proof.append(node("rect",{x:(i%3)*36,y:Math.floor(i/3)*35,width:17,height:24,fill:"#9a7153",stroke:"#555"}));
   proof.append(counter(p,50,48,62,false),node("text",{x:50,y:106,"text-anchor":"middle","font-size":12},label));$("proof").append(proof);
  }
 }
 function controls(){
  const p=palettes.find(p=>p.id===selected);$("family").textContent=p.family;$("adjustments").replaceChildren();
  ["Hue","Saturation","Lightness"].forEach((name,i)=>{const label=document.createElement("label"),input=document.createElement("input");label.textContent=name;input.type="range";input.min=p.bounds[i*2];input.max=p.bounds[i*2+1];input.value=p.hsl[i];input.setAttribute("aria-label",name);input.oninput=()=>{p.hsl[i]=+input.value;save();draw();};label.append(input);$("adjustments").append(label);});
 }
 const applyView=()=>svg.setAttribute("viewBox",`${view.x} ${view.y} ${view.w} ${view.h}`);
 function zoom(f,cx=view.x+view.w/2,cy=view.y+view.h/2){const w=Math.max(550,Math.min(5000,view.w*f)),r=w/view.w;view={x:cx+(view.x-cx)*r,y:cy+(view.y-cy)*r,w,h:view.h*r};applyView();}
 const point=e=>{const p=svg.createSVGPoint();p.x=e.clientX;p.y=e.clientY;return p.matrixTransform(svg.getScreenCTM().inverse());};
 svg.addEventListener("wheel",e=>{e.preventDefault();const p=point(e);zoom(Math.exp(e.deltaY*.001),p.x,p.y);},{passive:false});
 let moved=false;
 document.addEventListener("keydown",e=>{if(e.code==="Space"&&!e.target.closest("input,select,button,a")){space=true;e.preventDefault();}});
 document.addEventListener("keyup",e=>{if(e.code==="Space")space=false;});
 svg.addEventListener("pointerdown",e=>{moved=false;if(e.button===1||(e.button===0&&space)){e.preventDefault();drag={id:e.pointerId,p:point(e)};svg.setPointerCapture(e.pointerId);svg.setAttribute("data-panning","true");}});
 svg.addEventListener("pointermove",e=>{if(!drag)return;const p=point(e);view.x+=drag.p.x-p.x;view.y+=drag.p.y-p.y;moved=true;applyView();});
 const stop=()=>{if(drag&&svg.hasPointerCapture(drag.id))svg.releasePointerCapture(drag.id);drag=null;svg.setAttribute("data-panning","false");};
 svg.addEventListener("pointerup",stop);svg.addEventListener("pointercancel",stop);window.addEventListener("blur",()=>{space=false;stop();});svg.addEventListener("auxclick",e=>{if(e.button===1)e.preventDefault();});
 $("fit").onclick=()=>{view={x:0,y:0,w:3138,h:1130};applyView();};$("in").onclick=()=>zoom(.8);$("out").onclick=()=>zoom(1.25);
 $("nationality").onchange=()=>{selected=$("nationality").value;controls();draw();};
 ["edge","size","names"].forEach(id=>$(id).oninput=draw);
 $("resetOne").onclick=()=>{const p=palettes.find(p=>p.id===selected);p.hsl=[...p.initial];save();controls();draw();};
 $("resetAll").onclick=()=>{for(const p of palettes)p.hsl=[...p.initial];save();controls();draw();};
 $("export").onclick=()=>{const data={schemaVersion:1,purpose:"Campaign Atlas ASL counter palette",officialPrintSpecification:false,edge:$("edge").checked,counterSize:+$("size").value,palettes:palettes.map(p=>({id:p.id,name:p.name,family:p.family,hsl:p.hsl,css:color(p),...(p.id==="cn"?{surroundPaletteId:"su"}:{})}))};const url=URL.createObjectURL(new Blob([JSON.stringify(data,null,2)],{type:"application/json"})),a=document.createElement("a");a.href=url;a.download="campaign-counter-palette.json";a.click();setTimeout(()=>URL.revokeObjectURL(url),1000);};
 controls();draw();applyView();
})();
