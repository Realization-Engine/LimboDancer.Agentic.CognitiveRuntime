"use strict";
// Authored appearance samples, not historical or playable map data.
const TerrainDemo=(()=>{
 const base=["unknown","open","woods","forest","town","village","orchard","marsh","water","desert","rough"];
 const features=["bocage","hedge","wall","building","rubble","fortification","minefield","sand","dunes","wadi","escarpment","depression","field","scrub","road","stream","bridge","slope","hilltop"];
 const entries=[...base.map(type=>({type,category:"base"})),...features.map(type=>({type,category:"feature"})),{type:"elevation1",category:"elevation"},{type:"elevation2",category:"elevation"}];
 const tokens=[["unknown","Unknown terrain","#d2d0c8"],["forest","Forest fill","#809758"],["village","Village ground","#e8d9b8"],["orchard","Orchard ground","#dbe2ae"],["desert","Desert ground","#ead29d"],["rough","Rough ground","#bcb396"],["bocage","Bocage","#4d693c"],["hedge","Hedge","#6e864a"],["wall","Wall / stone","#898175"],["rubble","Rubble","#9a8271"],["fortification","Fortification","#777564"],["minefield","Minefield marking","#904936"],["sand","Sand","#ebd6a3"],["dunes","Dune ridges","#c3a265"],["wadi","Wadi","#ac875c"],["escarpment","Escarpment","#8e7957"],["depression","Depression","#b7a786"],["field","Field crops","#c0b568"],["scrub","Scrub","#969c69"]];
 const label=s=>s==="elevation1"?"Elevation 1":s==="elevation2"?"Elevation 2":s[0].toUpperCase()+s.slice(1);
 const R=60,DY=Math.sqrt(3)*R;
 const cells=[];
 for(let q=-1;q<36;q++)for(let r=-1;r<12;r++){
  const x=60+90*q,y=52+DY*(r+(q%2!==0?.5:0));
  let baseType="open",elevation=0,fs=[];
  if(q<8&&r<5)baseType=q<4?"forest":"woods";
  if(q>=6&&q<=10&&r>=5&&r<=8)baseType="orchard";
  if(q>=13&&q<=17&&r>=4&&r<=6)baseType="town";
  if(q>=23&&q<=25&&r>=3&&r<=4)baseType="village";
  if(q>=18&&q<=21&&r<=2)baseType="water";
  if(q>=17&&q<=22&&r===3)baseType="marsh";
  if(q>=26&&r<=6){elevation=q>=29?2:1;if(q>=30&&r<4)baseType="rough";if(q===27&&r<4)baseType="woods";}
  if(q>=24&&r>=8){baseType="desert";fs.push("sand");if(q<29)fs.push("dunes");else fs.push("scrub");}
  if(q===33&&r===0)baseType="unknown";
  if(q>=10&&q<=12&&r>=6&&r<=8)fs.push("field",q===12?"hedge":"bocage");
  if(baseType==="town"||baseType==="village")fs.push("building","road");
  if(q===17&&r===5)fs.push("rubble","wall");
  if(q===26&&r===5)fs.push("fortification");
  if(q>=26&&q<=28&&r===6)fs.push("minefield");
  if(q===28&&r===4)fs.push("escarpment");
  if(q===24&&r===7)fs.push("depression");
  if(q===26&&r===8)fs.push("wadi");
  if(q===26&&r===2)fs.push("slope");
  if(q===30&&r===4)fs.push("hilltop");
  if(q===20&&r===5)fs.push("road","stream","bridge");
  if(q===21&&r===6)fs.push("stream");
  cells.push({id:"D"+q+"-"+r,q,r,x,y,base:baseType,elevation,features:fs});
 }
 function draw(svg,n,ids=false){
  const layers={};
  for(const key of ["ground","detail","routes","grid","labels"]){layers[key]=n("g");svg.append(layers[key]);}
  const path=(layer,d,color,width=3,attrs={})=>layer.append(n("path",{d,fill:"none",stroke:color,"stroke-width":width,"stroke-linecap":"round","stroke-linejoin":"round",...attrs}));
  const fills={unknown:"#d2d0c8",open:"#eee8bd",woods:"#9caa73",forest:"#809758",town:"#e9ddb9",village:"#e8d9b8",orchard:"#dbe2ae",marsh:"#bfd0ac",water:"#79bac5",desert:"#ead29d",rough:"#bcb396"};
  for(const c of cells){
   const {x,y,q,r}=c,vs=Array.from({length:6},(_,i)=>[x+R*Math.cos(i*Math.PI/3),y+R*Math.sin(i*Math.PI/3)]),points=vs.map(p=>p.join(",")).join(" ");
   const g=n("g",{"data-demo-hex":c.id,"data-base":c.base,"data-features":c.features.join(" "),"data-elevation":c.elevation});
   const fill=c.base==="open"&&c.elevation?c.elevation===1?"#ded7ab":"#cec899":fills[c.base];
   const hex=n("polygon",{points,fill});hex.append(n("title",{},c.id+": "+c.base+", elevation "+c.elevation+(c.features.length?", "+c.features.join(", "):"")));layers.ground.append(hex);layers.detail.append(g);
   let seed=((q+9)*73856093^(r+7)*19349663)>>>0;
   const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
   if(["woods","forest","orchard"].includes(c.base)){
    for(let i=0;i<(c.base==="forest"?25:15);i++){
     const a=random()*Math.PI*2,rad=Math.sqrt(random())*44,px=c.base==="orchard"?x-30+(i%5)*15:x+Math.cos(a)*rad,py=c.base==="orchard"?y-26+Math.floor(i/5)*26:y+Math.sin(a)*rad;
     g.append(n("circle",{cx:px,cy:py,r:4+random()*3,fill:i%3?"#718b51":"#b7bd7b",stroke:c.base==="orchard"?"#7c915b":"none","stroke-width":1}));
    }
   }
   if(c.features.includes("building")){
    for(let i=0;i<(c.base==="town"?7:3);i++){const bx=x-30+random()*52,by=y-34+random()*57;
     g.append(n("rect",{x:bx+2,y:by+2,width:11,height:17,fill:"#6b654e",opacity:.25}),n("rect",{x:bx,y:by,width:11,height:17,fill:i%2?"#a24d32":"#80929a",stroke:"#51584e","stroke-width":1}));
     path(g,`M${bx+5} ${by}v17`,"#e0dac4",1);
    }
   }
   if(c.base==="marsh")for(let i=0;i<7;i++){const px=x-30+random()*60,py=y-25+random()*50;path(g,`M${px-5} ${py}h12m-6 0v-12m0 12l-5 -8m5 8l5 -8`,"#718b51",1.5);}
   if(c.base==="rough"||c.features.includes("rubble"))for(let i=0;i<10;i++){const px=x-35+random()*64,py=y-33+random()*60;g.append(n("path",{d:`M${px} ${py}l7 -4 5 8 -10 3Z`,fill:c.base==="rough"?"#898175":"#9a8271"}));}
   for(const f of c.features){
    if(["bocage","hedge","wall","escarpment"].includes(f)){
     const col={bocage:"#4d693c",hedge:"#6e864a",wall:"#898175",escarpment:"#8e7957"}[f],a=vs[0],b=vs[1];
     path(g,`M${a}L${b}`,col,f==="bocage"?7:4);
     if(f==="escarpment")for(let t=.1;t<1;t+=.2)path(g,`M${a[0]+(b[0]-a[0])*t} ${a[1]+(b[1]-a[1])*t}l-8 -3`,col,2);
    }
    if(f==="field")for(let i=0;i<7;i++)path(g,`M${x-30} ${y-30+i*10}h60`,"#c0b568",3);
    if(f==="minefield"){path(g,`M${x-25} ${y-24}h50v48h-50Z`,"#904936",2,{"stroke-dasharray":"4 4"});path(g,`M${x-7} ${y-7}l14 14m0 -14l-14 14`,"#904936",3);}
    if(f==="fortification")g.append(n("path",{d:`M${x-25} ${y+20}v-35l8 -8h34l8 8v35Z`,fill:"#777564"}),n("rect",{x:x-12,y:y-4,width:24,height:5,fill:"#000000"}));
    if(f==="depression")g.append(n("ellipse",{cx:x,cy:y,rx:36,ry:25,fill:"#b7a786"}));
    if(f==="dunes")for(let i=0;i<3;i++)path(g,`M${x-30} ${y-22+i*22}q30 -15 60 0`,"#c3a265",3);
    if(f==="sand")for(let i=0;i<12;i++)g.append(n("circle",{cx:x-40+random()*80,cy:y-35+random()*70,r:1.5,fill:"#ebd6a3"}));
    if(f==="scrub")for(let i=0;i<8;i++)g.append(n("circle",{cx:x-35+random()*70,cy:y-30+random()*60,r:3,fill:"#969c69"}));
    if(f==="slope"||f==="hilltop")path(g,`M${x-40} ${y+20}Q${x} ${y-35} ${x+40} ${y+20}`,"#cec899",5);
   }
   if(c.base==="unknown")g.append(n("text",{x,y:y+10,"font-size":28,"text-anchor":"middle"},"?"));
   layers.grid.append(n("polygon",{points,fill:"none",stroke:"#746f52","stroke-width":1,"stroke-opacity":.27,"pointer-events":"none"}));
   if(ids)layers.labels.append(n("text",{x,y:y+40,"text-anchor":"middle","font-size":12,stroke:"#fffbe8","stroke-width":2,"paint-order":"stroke","data-hex-label":c.id},c.id));
  }
  const river="M1880 245 C1850 350 2010 430 1905 520 S1740 690 1815 815 S1870 980 1740 1130";
  for(const [col,w]of [["#b9b78c",22],["#4a7781",15],["#79bac5",10]])path(layers.routes,river,col,w);
  const roads=["M0 600 C420 600 620 500 870 550 S1270 600 1510 552 L1880 552 C2140 552 2300 470 2500 410 S2860 440 3138 350","M870 550 Q920 790 1060 1130","M1510 552 Q1530 270 1630 0"];
  for(const col of ["#65644f","#fcf8df"])for(const road of roads)path(layers.routes,road,col,col==="#65644f"?12:7);
  layers.routes.append(n("rect",{x:1860,y:541,width:45,height:22,fill:"#d6caaa",stroke:"#625e50","stroke-width":3}));
  path(layers.routes,"M2350 885 Q2420 910 2500 960T2750 1100","#ac875c",13);path(layers.routes,"M2350 885 Q2420 910 2500 960T2750 1100","#ebd6a3",7);
  for(const [name,x,y]of [["Deep forest",260,230],["Woodland",600,350],["Orchards & bocage",860,880],["Town & rubble",1430,725],["Lake & marsh",1900,160],["Bridge crossing",1890,600],["Village",2240,335],["Wooded slopes",2490,180],["Ridge & fortifications",2820,700],["Dunes, scrub & wadi",2610,1090],["Unsurveyed",3030,65]]){
   layers.labels.append(n("text",{x,y,"text-anchor":"middle","font-size":23,"font-weight":700,stroke:"#eee8bd","stroke-width":5,"paint-order":"stroke","data-place-label":name},name));
  }
 }
 return {entries,tokens,cells,draw};
})();
