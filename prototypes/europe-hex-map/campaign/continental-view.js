"use strict";
// The atlas renders the same local board geometry through an affine continental placement.
window.CampaignContinentalView=(()=>{
 const ns='http://www.w3.org/2000/svg',cache=new Map();
 const node=(tag,a={})=>{const n=document.createElementNS(ns,tag);for(const[k,v]of Object.entries(a))n.setAttribute(k,v);return n;};
 function matrix(b){const p=[{x:0,y:0},{x:1,y:0},{x:0,y:1}].map(p=>CampaignGeography.atlas(CampaignGeography.world(b,p)));return[p[1].x-p[0].x,p[1].y-p[0].y,p[2].x-p[0].x,p[2].y-p[0].y,p[0].x,p[0].y];}
 function draw(map,layer,scale,focus){layer.replaceChildren();const view=map.getAttribute('viewBox').split(/[ ,]+/).map(Number);const footprints=PANZER_SITUATION_LIBRARY.map(d=>{const points=CampaignGeography.footprint(CampaignSupplySpatial.build(d)).map(CampaignGeography.atlas);return[Math.min(...points.map(p=>p.x)),Math.min(...points.map(p=>p.y)),Math.max(...points.map(p=>p.x)),Math.max(...points.map(p=>p.y))];});CampaignInterstitialTerrain.draw(layer,view,scale,footprints);
  for(const d of window.PANZER_SITUATION_LIBRARY.filter(d=>Number.isInteger(d.situation.number))){const number=d.situation.number,saved=CampaignGeographicStore.read(localStorage,number),b=CampaignSupplySpatial.resolve(d,saved?.binding),corners=CampaignGeography.footprint(b).map(CampaignGeography.atlas),xs=corners.map(p=>p.x),ys=corners.map(p=>p.y);if(Math.max(...xs)<view[0]||Math.min(...xs)>view[0]+view[2]||Math.max(...ys)<view[1]||Math.min(...ys)>view[1]+view[3])continue;
   const outline=node('polygon',{points:corners.map(p=>p.x+','+p.y).join(' '),fill:'#52734422',stroke:'#527344','stroke-width':Math.max(.035,scale*.8),'data-geographic-footprint':number});const title=node('title');title.textContent=d.situation.title+' | registered authored footprint';outline.append(title);layer.append(outline);
   if(focus!==number||scale>.08)continue;
   const g=node('g',{transform:'matrix('+matrix(b).join(' ')+')','data-continental-source':number});
   if(!cache.has(number)){const terrain=node('g');CampaignSupplyView.terrain(terrain,d);cache.set(number,terrain);}g.append(cache.get(number).cloneNode(true));
   const mission=[14,15].includes(number)?'M'+number:'M04';CampaignSupplyView.overlay(g,d,{operation:saved?.operation,mission,size:Math.max(65,scale*1000/b.metresPerUnit*22),host:map.parentElement,onSelect:item=>{if(['hq','command'].includes(item.kind))CampaignNavigation.open(number);}});layer.append(g);
  }
  CampaignRearView.draw(map,layer,scale,focus);
 }
 function focus(map,n){const d=window.PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===n);if(!d)return;const b=CampaignSupplySpatial.build(d),p=CampaignGeography.footprint(b).map(CampaignGeography.atlas),xs=p.map(p=>p.x),ys=p.map(p=>p.y),w=Math.max(...xs)-Math.min(...xs),h=Math.max(...ys)-Math.min(...ys);const box=[Math.min(...xs)-w*.15,Math.min(...ys)-h*.4,w*1.3,h*1.8];if(typeof view!=='undefined'&&typeof renderView==='function'){view=box;renderView();}else map.setAttribute('viewBox',box.join(' '));document.getElementById('mapHeading').textContent=d.situation.title;document.getElementById('breadcrumb').textContent='Europe / '+d.parentCampaign.title+' / Registered Situation footprint';document.getElementById('viewLevel').textContent='SITUATION FOOTPRINT | CONTINENTAL ATLAS';}
 return{draw,focus};
})();
