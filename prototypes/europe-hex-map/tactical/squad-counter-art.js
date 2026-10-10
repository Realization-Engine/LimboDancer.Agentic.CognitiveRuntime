"use strict";
// Reuse only the central Panzer illustration. No printed Panzer factors survive the crop.
window.SquadCounterArt = (()=>{
 const ns="http://www.w3.org/2000/svg";
 // Source bounds exclude the original Panzer numbers and captions. Different
 // illustration families occupy different portions of the printed counter.
 const sourceBounds={personnel:[33,39,62,51],vehicle:[10,40,112,47],weapon:[32,38,67,62]};
 const frame={x:-6,y:-3.3,width:12,height:6.6,padding:.45};
 function bounds(kind){return sourceBounds[kind==="vehicle"||kind==="transport"?"vehicle":kind==="gun"||kind==="mortar"?"weapon":"personnel"];}
 function fit(kind){
  const [x,y,width,height]=bounds(kind),scale=Math.min((frame.width-2*frame.padding)/width,(frame.height-2*frame.padding)/height);
  return {source:[x,y,width,height],x:frame.x+(frame.width-width*scale)/2,y:frame.y+(frame.height-height*scale)/2,width:width*scale,height:height*scale};
 }
 const node=(tag,attrs,text)=>{const n=document.createElementNS(ns,tag);for(const [k,v] of Object.entries(attrs))n.setAttribute(k,v);if(text)n.textContent=text;return n;};
 function draw(g,d,type,u,selected){
  const definition=SquadAslDefinitions.types[type.id],v=definition.values;
  const color=window.COUNTER_PALETTE_DEFAULTS.palettes.find(p=>p.id===(type.nationality||(type.side==="German"?"de":"us")))?.css||"#a9bb80";
  g.append(node("rect",{x:-7,y:-7,width:14,height:14,rx:.6,fill:color,stroke:selected?"#b25400":"#18221a","stroke-width":selected?1.2:.5}));
  const art=CampaignAppearance.counterImage((d.assetBase||"assets/panzer-leader-04/")+type.artwork,type.side,u.label,type.nationality);
  const box=fit(u.kind);
  art.setAttribute("viewBox",box.source.join(" "));art.setAttribute("preserveAspectRatio","xMidYMid meet");
  for(const key of ["x","y","width","height"])art.setAttribute(key,box[key]);
  art.style.width=box.width+"px";art.style.height=box.height+"px";art.style.pointerEvents="none";g.append(art);
  const factors=v.firepower!==undefined?[v.firepower,v.range,v.morale].filter(x=>x!==undefined).join("-"):v.caliber?v.caliber+(v['caliber-suffix']==='l'?'L':''):"MP "+v['movement-points'];
  g.append(node("text",{x:0,y:6,"text-anchor":"middle","font-size":2.7,fill:"#101810"},factors));
  const top=v['af-front']!==undefined?"AF "+v['af-front']+"/"+v['af-side']:v['rate-of-fire']!==undefined?"ROF "+v['rate-of-fire']:"";
  g.append(node("text",{x:-6,y:-4,"font-size":2.1,fill:"#101810"},top),node("text",{x:6,y:-4,"text-anchor":"end","font-size":2.3,fill:"#101810"},u.id.split("-").at(-1)));
  g.append(node("title",{},u.parentId+" | "+u.label+" | "+definition.status+" | "+Object.entries(v).map(([k,v])=>k+": "+v).join("; ")));
 }
 return {draw,fit};
})();
