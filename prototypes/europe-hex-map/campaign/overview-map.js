"use strict";
(()=>{
 if(new URLSearchParams(location.search).has('exercise'))return;
 const map=document.getElementById('map'),N=window.CampaignNavigation,ns='http://www.w3.org/2000/svg';
 const node=(tag,a={},text)=>{const n=document.createElementNS(ns,tag);for(const[k,v]of Object.entries(a))n.setAttribute(k,v);if(text)n.textContent=text;return n;};
 const geoLayer=node('g',{'aria-label':'Registered Situation geography'});map.append(geoLayer);let focus=Number(new URLSearchParams(location.search).get('focusSituation')||new URLSearchParams(location.search).get('supplySituation'))||null;
 const layer=node('g',{'aria-label':'Campaign Situations'});map.append(layer);
 const popup=document.createElement('section');popup.className='campaign-map-picker';popup.hidden=true;popup.setAttribute('aria-label','Selected map Situations');document.body.append(popup);
 function show(group){if(group.length===1){focus=group[0].sourceNumber;CampaignContinentalView.focus(map,focus);}
  popup.replaceChildren();popup.hidden=false;
  const close=document.createElement('button');close.textContent='Close';close.onclick=()=>popup.hidden=true;popup.append(close);
  const heading=document.createElement('h2');heading.textContent=group.length===1?'Campaign Situation':'Situations in this area';popup.append(heading);
  const explanation=document.createElement('p');explanation.className='campaign-numbering-hint';explanation.textContent='Situation numbers restart within each campaign.';popup.append(explanation);
  const campaigns=new Map();
  for(const entry of group){if(!campaigns.has(entry.campaignId))campaigns.set(entry.campaignId,[]);campaigns.get(entry.campaignId).push(entry);}
  for(const entries of [...campaigns.values()].sort((a,b)=>a[0].campaign.localeCompare(b[0].campaign))){
   const section=document.createElement('section');section.className='campaign-picker-group';section.setAttribute('aria-label',entries[0].campaign);
   const title=document.createElement('h3');title.textContent=entries[0].campaign;section.append(title);
   for(const e of entries.sort((a,b)=>a.number-b.number)){
    const b=document.createElement('button');b.textContent=String(e.number).padStart(2,'0')+' | '+e.title;
    b.title=e.date+' | '+e.mode;b.onclick=()=>N.open(e.sourceNumber);section.append(b);
   }
   popup.append(section);
  }
  const note=document.createElement('p');note.textContent='Select a Situation to open its command operation. Board footprints are authored geographic placements; card orientation and setup rules are preserved.';popup.append(note);position();close.focus();
 }
 function position(){const r=map.getBoundingClientRect();popup.style.left=Math.max(r.left+16,16)+'px';popup.style.top=Math.max(r.top+16,16)+'px';popup.style.maxWidth=Math.max(230,Math.min(520,r.width-32))+'px';popup.style.maxHeight=Math.max(200,r.height-32)+'px';}
 function draw(){
  const v=map.getAttribute('viewBox').split(/[ ,]+/).map(Number),r=map.getBoundingClientRect(),scale=Math.max(v[2]/(r.width||1200),v[3]/(r.height||900));
  layer.replaceChildren();CampaignContinentalView.draw(map,geoLayer,scale,focus);
  const detailed=geoLayer.querySelector('[data-continental-source]'),groups=[];
  for(const e of N.entries){
   const focused=!!detailed&&e.sourceNumber===focus;
   let g=focused?null:groups.find(g=>g[0].sourceNumber!==focus&&Math.hypot(g[0].anchor.x-e.anchor.x,g[0].anchor.y-e.anchor.y)<42*scale);
   if(!g)groups.push(g=[]);g.push(e);
  }
  for(const group of groups){
   const e=group[0],focused=!!detailed&&e.sourceNumber===focus,label=group.length>1?group.length+' Situations in this area':e.campaign+' | '+String(e.number).padStart(2,'0')+' | '+e.title;
   let anchor=e.anchor;
   if(focused){
    const source=PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===focus),saved=CampaignGeographicStore.read(localStorage,focus),b=saved?.binding||CampaignSupplySpatial.build(source),corners=CampaignGeography.footprint(b).map(CampaignGeography.atlas);
    anchor={x:Math.min(...corners.map(p=>p.x))+32*scale,y:Math.min(...corners.map(p=>p.y))+32*scale};
   }
   const g=node('g',{transform:`translate(${anchor.x} ${anchor.y}) scale(${scale})`,role:'button',tabindex:0,'aria-label':label,class:'campaign-situation-marker','data-situation-count':group.length,'data-footprint-label':focused?'true':'false'});
   if(focused)g.append(node('rect',{x:-24,y:-24,width:62+e.title.length*8,height:48,rx:5,fill:'#ffffff','fill-opacity':.25}));
   g.append(node('title',{},group.map(x=>x.campaign+' / '+String(x.number).padStart(2,'0')+' '+x.title+' / '+x.date+' / '+x.mode).join('\n')),node('circle',{r:17,fill:focused?'none':'#f7f5ee',stroke:group.length>1?'#9d772f':'#4a6326','stroke-width':3}),node('text',{'text-anchor':'middle',y:5,'font-size':13,'font-weight':700},group.length>1?'+'+group.length:String(e.number).padStart(2,'0')));
   if(scale<=.1&&group.length===1)g.append(node('text',{x:24,y:5,'font-size':13,'paint-order':'stroke',stroke:'#ffffff','stroke-opacity':focused?.25:1,'stroke-width':3,fill:'#1e2719'},e.title));
   g.onclick=ev=>{ev.stopPropagation();if(typeof dragged==='undefined'||!dragged)show(group);};g.onkeydown=ev=>{if(['Enter',' '].includes(ev.key)){ev.preventDefault();show(group);}};layer.append(g);
  }
  map.append(layer);position();
 }

 window.addEventListener('campaign-atlas-logistics-changed',()=>{map.parentElement.querySelector('.supply-map-card')?.remove();draw();});window.addEventListener('storage',event=>{if(event.key===CampaignGeographicStore.KEY)draw();});
 new MutationObserver(draw).observe(map,{attributes:true,attributeFilter:['viewBox']});new ResizeObserver(draw).observe(map);if(focus){if(new URLSearchParams(location.search).has('supplySituation'))CampaignRearView.focus(map,focus);else CampaignContinentalView.focus(map,focus);}draw();
})();
