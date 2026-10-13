"use strict";
// Authored gameplay corridors. Geographic waypoints are not a claim of historical road alignment.
window.CampaignRearNetwork=(()=>{
 const version='rear-corridors-2';
 const hubs={normandy:{name:'Normandy coastal supply base',points:[[-.7,49.34],[-.85,49.28]]},lorraine:{name:'Reims rear supply base',points:[[4.03,49.25],[4.94,49.16],[5.38,49.16]]},north:{name:'Brussels rear supply base',points:[[4.35,50.85],[4.94,51.16],[5.3,51.35]]},ardennes:{name:'Liege rear supply base',points:[[5.58,50.63],[5.7,50.52]]},rhine:{name:'Aachen rear supply base',points:[[6.08,50.78],[6.65,50.63]]}};
 function build(routes){
  const number=Number(Object.keys(routes)[0]),b=routes[number].binding,key=number<=5?'normandy':[7,8,12].includes(number)?'lorraine':[6,9,10,11].includes(number)?'north':number===20?'rhine':'ardennes',hub=hubs[key];
  const depot=b.sides.Allied.nodes.find(n=>n.kind==='depot'),entry=b.sides.Allied.nodes.find(n=>n.kind==='entry'),end=CampaignGeography.world(b,depot.position),edge=CampaignGeography.world(b,entry.position),waypoints=hub.points.map(p=>CampaignGeography.project(...p));
  const entryPath=(b.sides.Allied.entryPath||[entry.position,depot.position]).map(p=>CampaignGeography.world(b,p));
  const points=[...waypoints,...entryPath],km=CampaignSupplySpatial.length(points)/1000,travel=Math.ceil(km/20*60/5)*5;
  return{version,available:!b.sides.Allied.isolated,id:key+':PL'+number,sourceNumber:number,hubId:key+':rear',hubName:hub.name,destinationId:depot.id,entryId:entry.id,entryPosition:edge,points,km,travelMinutes:travel,handlingMinutes:30,capacity:3,provenance:'Authored rear-area road corridor and source-board connector. 20 km/h; 30 min depot receipt. Not a surveyed or historical convoy route.'};
 }
 return{version,build};
})();
