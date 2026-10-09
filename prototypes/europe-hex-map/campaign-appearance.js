"use strict";
const CampaignAppearance=(()=>{
 const NS="http://www.w3.org/2000/svg";
 const sourceColors={"#eee8bd": "open", "#bfd0ac": "marsh", "#ded7ab": "hill1", "#cec899": "hill2", "#e9ddb9": "town", "#837e50": "groundTexture", "#9caa73": "woods", "#96a765": "woodland", "#7c915b": "woodsEdge", "#718b51": "trees", "#b7bd7b": "treesLight", "#fcf8df": "road", "#65644f": "roadEdge", "#79bac5": "water", "#4a7781": "waterEdge", "#b9b78c": "bank", "#80929a": "roofBlue", "#a24d32": "roofRed", "#51584e": "buildingEdge", "#e0dac4": "roofLine", "#6b654e": "shadow", "#d6caaa": "bridge", "#625e50": "bridgeEdge", "#746f52": "grid", "#000000": "labels", "#fffbe8": "hexHalo"};
 let sequence=0;
 const node=(tag,attrs={})=>{const n=document.createElementNS(NS,tag);for(const [k,v]of Object.entries(attrs))n.setAttribute(k,String(v));return n;};
 function terrain(root){
  const theme=window.TERRAIN_PALETTE_DEFAULTS;
  const visit=n=>{
   const read=k=>n.getAttribute?n.getAttribute(k):n.attrs?.[k];
   const tag=(n.tagName||n.tag||"").toLowerCase();
   for(const attr of ["fill","stroke"]){
    let key=sourceColors[read(attr)];
    if(tag==="text"&&attr==="fill"&&!read(attr))key="labels";
    if(attr==="stroke"&&read("data-place-label"))key="labelHalo";
    if(key&&theme.colors[key])n.setAttribute(attr,theme.colors[key]);
    if(key==="grid"&&attr==="stroke")n.setAttribute("stroke-opacity",theme.gridOpacity);
   }
   for(const child of n.children||[])visit(child);
  };visit(root);
 }
 function counterImage(href,side,label,nationality){
  const id=nationality||(side==="German"?"de":"us"),p=window.COUNTER_PALETTE_DEFAULTS.palettes.find(p=>p.id===(id==="ca"?"uk":id));
  const [h,s,l]=p.hsl.map((v,i)=>i?v/100:v/30);
  const a=s*Math.min(l,1-l),channel=n=>{const k=(n+h)%12;return l-a*Math.max(-1,Math.min(k-3,9-k,1));};
  const rgb=[channel(0),channel(8),channel(4)],filterId="asl-counter-palette-"+(++sequence);
  const svg=node("svg",{viewBox:"0 0 128 128",width:128,height:128,role:"img","aria-label":label,class:"panzer-counter-art","data-nationality":id});
  const defs=node("defs"),filter=node("filter",{id:filterId,"color-interpolation-filters":"sRGB",x:0,y:0,width:1,height:1,filterUnits:"objectBoundingBox"});
  filter.append(node("feColorMatrix",{type:"saturate",values:0}));
  const transfer=node("feComponentTransfer");
  // Preserve dark printed detail while replacing the lighter paper/background.
  for(const [i,c]of ["R","G","B"].entries())transfer.append(node("feFunc"+c,{type:"table",tableValues:[0,rgb[i]*.5,rgb[i],rgb[i],rgb[i]].join(" ")}));
  filter.append(transfer);defs.append(filter);
  svg.append(defs,node("rect",{width:128,height:128,fill:p.css}),node("image",{href,x:0,y:0,width:128,height:128,filter:"url(#"+filterId+")"}));
  return svg;
 }
 return {terrain,counterImage};
})();
