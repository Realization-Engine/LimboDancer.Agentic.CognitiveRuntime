"use strict";
// Versioned placement on the original joined boards, shared by every map scale.
window.CampaignSupplySpatial=(()=>{
 const version='board-supply-4',metresPerUnit=250/113,cache=new Map();
 const length=points=>points.slice(1).reduce((sum,p,i)=>sum+Math.hypot(p.x-points[i].x,p.y-points[i].y),0);
 function point(points,fraction){let remaining=length(points)*Math.max(0,Math.min(1,fraction));for(let i=1;i<points.length;i++){const a=points[i-1],b=points[i],n=Math.hypot(b.x-a.x,b.y-a.y);if(remaining<=n){const t=n?remaining/n:0;return{x:a.x+(b.x-a.x)*t,y:a.y+(b.y-a.y)*t};}remaining-=n;}return {...points.at(-1)};}
 function baseBuild(d){
  if(d.supplyContext?.binding?.version===version)return d.supplyContext.binding;
  const key=Number(d.situation.number);if(cache.has(key))return cache.get(key);
  const layout=SquadSpace.layout(d),sides={};
  for(const side of ['Allied','German']){
   const placement=CampaignSupplyPlacement.build(d,layout,side),{boardId,roadId,entry,depot,receipt,hq,command}=placement,route=placement.points;
   const prefix='PL'+key+':'+side,hasFuel=d.counters.some(c=>c.side===side&&window.SquadProfiles?.types[c.id]?.kind==='vehicle');
   const km=length(route)*metresPerUnit/1000,minutes=Math.max(5,Math.ceil(km/12*60/5)*5);
   sides[side]={side,boardId,roadId,routeId:prefix+':route',points:route,km,minutes,handlingMinutes:5,
    receivingArea:placement.receivingArea,provenance:placement.provenance,coverage:placement.coverage,isolated:placement.isolated,entryPath:placement.entryPath,
    nodes:[{id:prefix+':command',kind:'command',label:side+' operation HQ',position:command.p,hexId:command.hexId},{id:prefix+':entry',kind:'entry',label:side+' supply entry',position:entry},{id:prefix+':depot',kind:'depot',label:side+' forward depot',position:depot},{id:prefix+':supply',kind:'supply',label:side+' receiving point',position:receipt},{id:prefix+':hq',kind:'hq',label:side+' mission HQ',position:hq.p,hexId:hq.hexId}],
    packages:[{id:prefix+':ammo',commodity:'ammunition',quantity:1,effect:'One authored mission ammunition allowance for the assigned source force.'},...(hasFuel?[{id:prefix+':fuel',commodity:'fuel',quantity:1,effect:'One authored mission fuel allowance for assigned motorized forces.'}]:[])]};
  }
  if([14,15].includes(key))for(const n of sides.Allied.nodes)if(['command','depot','entry'].includes(n.kind))n.id='ardennes:Allied:'+n.kind;
  const result={version,capabilities:{basicSupply:true,advanced:['route-incidents','repair','escort','recall','connected-detours','transport-return']},referenceBoard:[14,15].includes(key)?'C':null,spaceId:[14,15].includes(key)?'ardennes:board-C':'PL'+key+':joined-boards',sourceNumber:key,crs:'authored-campaign-local-metres',metresPerUnit,origin:{x:0,y:0},layout,sides,width:Math.max(...layout.map(p=>p.x+p.w)),height:Math.max(...layout.map(p=>p.y+p.h))};if(window.CampaignGeography){result.geography=CampaignGeography.register(result);result.crs='EPSG:3035';}cache.set(key,result);return result;
 }
 function build(d){const b=baseBuild(d);return window.CampaignSupplyRelocation?CampaignSupplyRelocation.apply(d,b):b;}
 function toWorld(binding,p){if(window.CampaignGeography)return CampaignGeography.world(binding,p);if(binding.referenceBoard){const uv=SquadSpace.unproject(binding.layout.find(x=>x.boardId===binding.referenceBoard),p);return{x:uv.u*1130*metresPerUnit,y:uv.v*3138*metresPerUnit};}return{x:p.x*metresPerUnit,y:p.y*metresPerUnit};}
 function fromWorld(binding,p){if(window.CampaignGeography)return CampaignGeography.board(binding,p);if(binding.referenceBoard)return SquadSpace.project(binding.layout.find(x=>x.boardId===binding.referenceBoard),p.x/metresPerUnit/1130,p.y/metresPerUnit/3138);return{x:p.x/metresPerUnit,y:p.y/metresPerUnit};}
 function profile(numbers){const routes={};for(const n of numbers){const d=window.PANZER_SITUATION_LIBRARY?.find(d=>Number(d.situation.number)===n);if(!d)return null;const b=build(d);routes[n]={detour:window.CampaignRouteNetwork?.alternate(d,b)||null,binding:JSON.parse(JSON.stringify(b)),minutes:b.sides.Allied.minutes+b.sides.Allied.handlingMinutes,km:b.sides.Allied.km,roadId:b.sides.Allied.roadId};}return{version,routes,...(window.CampaignRearNetwork?{rear:CampaignRearNetwork.build(routes)}:{})};}
 function resolve(d,b){const base=b?.version===version?b:baseBuild(d);return window.CampaignSupplyRelocation?CampaignSupplyRelocation.apply(d,base):base;}
 return{resolve,version,metresPerUnit,build,point,length,toWorld,fromWorld,profile};
})();
