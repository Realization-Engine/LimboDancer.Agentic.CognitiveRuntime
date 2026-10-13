"use strict";
// World-grid refinement stays identical when panning or changing Situation views.
window.CampaignInterstitialTerrain=(()=>{
 const version='europe-refinement-1',seed='europe-1944',ns='http://www.w3.org/2000/svg',cache=new Map();
 function hash(text){let h=2166136261;for(const c of text)h=Math.imul(h^c.charCodeAt(0),16777619);return h>>>0;}
 let regions=null;
 function inside(x,y,ring){let hit=false;for(let i=0,j=ring.length-1;i<ring.length;j=i++){const a=ring[i],b=ring[j];if((a[1]>y)!==(b[1]>y)&&x<(b[0]-a[0])*(y-a[1])/(b[1]-a[1])+a[0])hit=!hit;}return hit;}
 function inLayer(x,y,layer){if(!regions)regions=(window.CAMPAIGN_MAP_DATA?.features||[]).filter(f=>['land','lakes','forest'].includes(f.properties.layer)).flatMap(f=>(f.geometry.type==='MultiPolygon'?f.geometry.coordinates:[f.geometry.coordinates]).map(rings=>{const ring=rings[0];return{layer:f.properties.layer,rings,b:[Math.min(...ring.map(p=>p[0])),Math.min(...ring.map(p=>p[1])),Math.max(...ring.map(p=>p[0])),Math.max(...ring.map(p=>p[1]))]};}));return regions.some(f=>f.layer===layer&&x>=f.b[0]&&x<=f.b[2]&&y>=f.b[1]&&y<=f.b[3]&&inside(x,y,f.rings[0])&&!f.rings.slice(1).some(r=>inside(x,y,r)));}
 function sample(x,y){const q=Math.floor(x),r=Math.floor(y),id=version+':'+q+':'+r;if(cache.has(id))return cache.get(id);x=q+.5;y=r+.5;
  const cells=window.CAMPAIGN_MAP_DATA?.cells||[],base=cells.reduce((best,c)=>!best||Math.hypot(c.x-x,c.y-y)<Math.hypot(best.x-x,best.y-y)?c:best,null),h=hash(seed+'|'+id),land=inLayer(q+.5,r+.5,'land')&&!inLayer(q+.5,r+.5,'lakes');
  const type=!land?'water':inLayer(q+.5,r+.5,'forest')?'woods':hash(seed+'|grove|'+Math.floor(q/3)+':'+Math.floor(r/3))%100<Math.round((base.forestReferenceFraction??.18)*100)?'woods':'open',v={id,x:q+.5,y:r+.5,type,seed,version};cache.set(id,v);return v;
 }
 function draw(layer,view,scale,footprints){if(scale>.25)return;const rect=layer.ownerSVGElement.getBoundingClientRect(),width=Math.max(view[2],view[3]*rect.width/rect.height),height=Math.max(view[3],view[2]*rect.height/rect.width);view=[view[0]-(width-view[2])/2,view[1]-(height-view[3])/2,width,height];const step=Math.max(1,Math.ceil(Math.sqrt(view[2]*view[3]/4500))),g=document.createElementNS(ns,'g');g.setAttribute('data-interstitial-terrain',version);g.setAttribute('pointer-events','none');
  const dy=step*Math.sqrt(3)/2,radius=step/Math.sqrt(3);
  for(let row=Math.floor(view[1]/dy)-1;row<=Math.ceil((view[1]+view[3])/dy)+1;row++)for(let col=Math.floor(view[0]/step)-1;col<=Math.ceil((view[0]+view[2])/step)+1;col++){
   const x=(col+(row%2?0.5:0))*step,y=row*dy;if(footprints.some(b=>x+radius>b[0]&&x-radius<b[2]&&y+radius>b[1]&&y-radius<b[3]))continue;
   const cell=sample(x,y);if(cell.type==='water')continue;const n=document.createElementNS(ns,'polygon'),points=Array.from({length:6},(_,i)=>{const a=(30+60*i)*Math.PI/180;return(x+radius*Math.cos(a))+','+(y+radius*Math.sin(a));}).join(' ');
   for(const[k,v]of Object.entries({points,fill:cell.type==='woods'?'#6e8a50':'#cfdbac','fill-opacity':.35,stroke:'#718764','stroke-opacity':.2,'stroke-width':scale*.6}))n.setAttribute(k,v);g.append(n);
  }

  layer.append(g);
 }
 return{version,seed,sample,draw};
})();
