"use strict";
// Illustration only. Hex terrain and source route coordinates remain authoritative inputs.
const PanzerMapArt=(()=>{
 const NS="http://www.w3.org/2000/svg",W=1130,H=3138;
 const node=(tag,attrs={},text)=>{const n=document.createElementNS(NS,tag);for(const [k,v] of Object.entries(attrs))n.setAttribute(k,String(v));if(text!==undefined)n.textContent=text;return n;};
 const points=ps=>ps.map(([u,v])=>`${u*W},${v*H}`).join(" ");
 function random(seed){let n=2166136261;for(const c of seed)n=Math.imul(n^c.charCodeAt(0),16777619);return()=>{n+=0x6D2B79F5;let t=Math.imul(n^(n>>>15),1|n);t^=t+Math.imul(t^(t>>>7),61|t);return((t^(t>>>14))>>>0)/4294967296;};}
 function inside(x,y,ps){let hit=false;for(let i=0,j=ps.length-1;i<ps.length;j=i++){const [a,b]=ps[i],[c,e]=ps[j];if((b>y)!==(e>y)&&x<(c-a)*(y-b)/(e-b)+a)hit=!hit;}return hit;}
 function seamWoods(d){
  const a=d.boards.find(b=>b.id==="A"),c=d.boards.find(b=>b.id==="C");
  if(!a||!c)return [];
  const edge=b=>b.hexes.filter(h=>h.imageCenter.v*H<4);
  return edge(a).flatMap(h=>{const mate=edge(c).find(k=>Math.abs(h.imageCenter.u-(1-k.imageCenter.u))*W<1);
   return mate&&(h.terrain.base==="woods"||mate.terrain.base==="woods")?[{id:h.id+":"+mate.id,y:h.imageCenter.u*W}]:[];});
 }
 function draw(svg,d,bd,selected,details,joined=false){
  const features=d.illustration.features.filter(f=>f.boardId===bd.id),woods=features.filter(f=>f.kind==="woodland");
  const defs=node("defs"),pattern=node("pattern",{id:"panzer-ground-"+bd.id,width:37,height:41,patternUnits:"userSpaceOnUse"});pattern.append(node("path",{d:"M4 8h2 M22 27h1 M12 36h2",stroke:"#837e50","stroke-width":.7,opacity:.17}));defs.append(pattern);svg.append(defs,node("rect",{width:W,height:H,fill:"#eee8bd"}));
  for(const h of bd.hexes){const fill=h.terrain.base==="marsh"?"#bfd0ac":h.terrain.elevationLevel===2?"#cec899":h.terrain.elevationLevel===1?"#ded7ab":h.terrain.base==="town"?"#e9ddb9":"#eee8bd";svg.append(node("polygon",{points:points(h.imagePolygon.map(p=>[p.u,p.v])),fill}));}
  svg.append(node("rect",{width:W,height:H,fill:"url(#panzer-ground-"+bd.id+")","pointer-events":"none"}));
  for(const h of bd.hexes.filter(h=>h.terrain.base==="woods")){
   if(joined&&h.imageCenter.v*H<4)continue;
   if(woods.some(f=>inside(h.imageCenter.u,h.imageCenter.v,f.polygon)))continue;
   const rng=random(d.illustration.seed+bd.id+h.id),cx=h.imageCenter.u,cy=h.imageCenter.v;
   woods.push({id:h.id,polygon:Array.from({length:14},(_,i)=>{const a=i*Math.PI/7,r=39+rng()*15;return[cx+Math.cos(a)*r/W,cy+Math.sin(a)*r/H];}),color:"#9caa73"});
  }
  for(const f of woods){svg.append(node("polygon",{points:points(f.polygon),fill:f.color,stroke:"#7c915b","stroke-width":1.2,"stroke-linejoin":"round"}));const rng=random(d.illustration.seed+bd.id+f.id),xs=f.polygon.map(p=>p[0]*W),ys=f.polygon.map(p=>p[1]*H),xmin=Math.min(...xs),ymin=Math.min(...ys),width=Math.max(...xs)-xmin,height=Math.max(...ys)-ymin;
   for(let i=0;i<Math.min(500,width*height/130);i++){const x=xmin+rng()*width,y=ymin+rng()*height;if(!inside(x/W,y/H,f.polygon))continue;const r=2+rng()*3;svg.append(node("circle",{cx:x,cy:y,r,fill:i%3?"#718b51":"#b7bd7b",opacity:.65}));}
  }
  if(joined){
   // Each side renders the same complete patch in seam coordinates. SVG clipping
   // exposes its own half, avoiding mismatched shapes or a painted seam line.
   const seam=node("g",{transform:bd.id==="A"?"matrix(0 -1 1 0 0 0)":"matrix(0 1 -1 0 1130 0)","data-seam-woods":bd.id});
   for(const patch of seamWoods(d)){const rng=random(d.illustration.seed+":seam:"+patch.id),poly=Array.from({length:18},(_,i)=>{const a=i*Math.PI/9,r=44+rng()*7;return [Math.cos(a)*r,patch.y+Math.sin(a)*r];});
    seam.append(node("polygon",{points:poly.map(p=>p.join(",")).join(" "),fill:"#9caa73",stroke:"#7c915b","stroke-width":1.2,"data-joined-wood":patch.id}));
    for(let i=0;i<90;i++){const x=(rng()-.5)*104,y=patch.y+(rng()-.5)*104;if(!inside(x,y,poly))continue;seam.append(node("circle",{cx:x,cy:y,r:2+rng()*3,fill:i%3?"#718b51":"#b7bd7b",opacity:.65}));}
   }svg.append(seam);
  }
  const routes=d.mapModel.routes.filter(r=>r.geometry.boardId===bd.id),layer=node("g",{"class":"panzer-routes","pointer-events":"none"});
  const stroke=(r,color,width)=>layer.append(node("path",{d:r.geometry.coordinates.map(([u,v],i)=>(i?"L":"M")+(u*W).toFixed(2)+","+(v*H).toFixed(2)).join(" "),fill:"none",stroke:color,"stroke-width":width,"stroke-linecap":"round","stroke-linejoin":"round","data-route-id":r.id}));
  for(const r of routes.filter(r=>r.kind==="stream")){stroke(r,"#b9b78c",15);stroke(r,"#4a7781",9);stroke(r,"#79bac5",5);}
  for(const r of routes.filter(r=>r.kind==="road"))stroke(r,"#65644f",10);
  for(const r of routes.filter(r=>r.kind==="road"))stroke(r,"#fcf8df",6);
  svg.append(layer);
  for(const f of features.filter(f=>f.kind==="building")){svg.append(node("polygon",{points:points(f.polygon.map(([u,v])=>[u+2/W,v+3/H])),fill:"#6b654e",opacity:.25}),node("polygon",{points:points(f.polygon),fill:f.color,stroke:"#51584e","stroke-width":1.3,"data-illustration":"building"}));const [a,b,c,e]=f.polygon;svg.append(node("path",{d:`M${(a[0]+b[0])*W/2},${(a[1]+b[1])*H/2} L${(c[0]+e[0])*W/2},${(c[1]+e[1])*H/2}`,stroke:"#e0dac4","stroke-width":1,opacity:.7}));}
  for(const f of features.filter(f=>f.kind==="bridge")){svg.append(node("polygon",{points:points(f.polygon),fill:f.color,"data-illustration":"bridge"}));for(const [i,j] of [[0,1],[2,3]])svg.append(node("path",{d:`M${f.polygon[i][0]*W},${f.polygon[i][1]*H} L${f.polygon[j][0]*W},${f.polygon[j][1]*H}`,stroke:"#625e50","stroke-width":2}));}
  // The grid stays legible without overpowering the terrain drawing.
  for(const h of bd.hexes){const poly=node("polygon",{points:points(h.imagePolygon.map(p=>[p.u,p.v])),fill:"none",stroke:h.id===selected?"#b77725":"#746f52","stroke-opacity":h.id===selected?1:.27,"stroke-width":h.id===selected?4:.8});poly.append(node("title",{},h.label+": "+h.terrain.base));svg.append(poly);
   if(details||h.id===selected){svg.append(node("text",{x:Math.max(20,Math.min(1105,h.imageCenter.u*W)),y:Math.max(18,Math.min(3125,h.imageCenter.v*H)),"text-anchor":"middle","font-size":17,"paint-order":"stroke",stroke:"#fffbe8","stroke-width":3,"data-hex-label":h.id},h.id));if(details)svg.append(node("text",{x:h.imageCenter.u*W,y:h.imageCenter.v*H+20,"text-anchor":"middle","font-size":11},h.terrain.features.join(" / ")));}
  }
  if(!details)for(const label of d.illustration.labels.filter(l=>l.boardId===bd.id))svg.append(node("text",{x:label.position[0]*W,y:label.position[1]*H,"text-anchor":"middle","font-family":"Barlow Condensed, sans-serif","font-weight":700,"font-size":28.75,"transform":joined?`rotate(${bd.id==="A"?-90:90} ${label.position[0]*W} ${label.position[1]*H})`:"rotate(0)","data-place-label":label.text,"letter-spacing":1,"paint-order":"stroke",stroke:"#eee8bd","stroke-width":5},label.text));
 }
 function northRose(d,boardId,joined=false){
  const source=d.illustration.northIndicators.find(n=>n.boardId===boardId);
  const bearing=source&&{...source,clockwiseDegreesFromSheetUp:joined?0:source.clockwiseDegreesFromSheetUp};
  if(!bearing)return null;
  const svg=node("svg",{viewBox:"0 0 80 80",class:"panzer-north-rose",role:"img","aria-label":"North, board "+boardId+", "+bearing.clockwiseDegreesFromSheetUp+" degrees clockwise from sheet top"});
  svg.append(node("title",{},"North according to Situation 4 mapboard orientation"),node("circle",{cx:40,cy:40,r:37,fill:"#f7f5ee","fill-opacity":.7,stroke:"#9b9b83"}));
  const g=node("g",{transform:`rotate(${bearing.clockwiseDegreesFromSheetUp} 40 40)`});
  g.append(node("path",{d:"M40 21 L32 53 L40 48 Z",fill:"#1e2719"}),node("path",{d:"M40 21 L48 53 L40 48 Z",fill:"#c3c8b4",stroke:"#1e2719","stroke-width":.8}));svg.append(g);
  const a=bearing.clockwiseDegreesFromSheetUp*Math.PI/180;
  svg.append(node("text",{x:40+29*Math.sin(a),y:40-29*Math.cos(a)+4,"text-anchor":"middle","font-family":"sans-serif","font-size":13,"font-weight":700,fill:"#1e2719"},"N"));return svg;
 }
 const project=(rotation,u,v)=>rotation===90?{x:1-v,y:u}:{x:v,y:1-u};
 const unproject=(rotation,x,y)=>rotation===90?{u:y,v:1-x}:{u:1-y,v:x};
 return {draw,northRose,project,unproject,seamWoods};
})();
