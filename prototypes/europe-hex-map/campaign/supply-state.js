"use strict";
// Read-only adapter for existing command ledgers. No extra stock is created on view changes.
window.CampaignSupplyState=(()=>{
 function inspect(d,operation,mission='M04',side='Allied'){
  const binding=CampaignSupplySpatial.resolve(d,operation?.supplyProfile?.routes[d.situation.number]?.binding),network=binding.sides[side],n=Number(d.situation.number),s=side==='Allied'?operation:null;
  const basic=!s||side!=='Allied',ammoKey=mission==='M14'?'A14':mission==='M15'?'A15':'ammunition';
  function location(value,key){
   if(value==='rear')return 'at rear supply base';if(value==='rear-transit')return 'in rear convoy';
   if(value==='depot'){
    const reserved=s?.trips?s.trips.some(t=>t.cargo.includes(key)&&['scheduled','waiting-transport'].includes(t.status)):s?.plan&&(key==='ammunition'?s.plan.supply!=='retain':s.plan.reserve==='attach'||!!s.plan.delivery);
    return reserved?'reserved at depot':'retained at depot';
   }
   if(value==='transit'||/^T\d/.test(value||''))return 'in transit';
   if(String(value).startsWith('consumed:')||String(value).startsWith('issued'))return value.includes(':')&&!value.endsWith(mission)?'issued to '+value.split(':')[1]:'issued to force';
   if(value===mission)return 'at receiving point';
   return /^M\d/.test(value||'')?'at '+value+' receiving point':'unavailable';
  }
  const packages=network.packages.map(p=>({...p,status:basic?'issued at scenario start':location(s.stock?.[p.commodity==='fuel'&&s.trips?'F1':p.commodity==='ammunition'?ammoKey:p.commodity],p.commodity==='fuel'&&s.trips?'F1':p.commodity==='ammunition'?ammoKey:p.commodity)}));
  const trip=s?.trips?.find(t=>[mission,'mixed'].includes(t.route)&&['transit','held','returning'].includes(t.status));
  const transport=s?.transport||trip;let progress=0;
  if(transport){const end=transport.arrival,start=transport.depart;progress=end>start?(s.time-start)/(s.supplyProfile?network.minutes:end-start):0;if(transport.status==='returning')progress=1-(s.time-end)/(s.queue.find(e=>e.kind==='return'&&e.data?.id===transport.id)?.at-end||30);if(transport.status==='delivered')progress=1;}
  if(transport&&window.CampaignLogistics)progress=CampaignLogistics.progress(s,transport,progress);
  return{binding,network,mission,side,packages,logistics:s?.logistics||null,routeStatus:s?.logistics?.routes[mission]?.status||'open',transport:transport?{...transport,detourPoints:operation?.supplyProfile?.version!==CampaignSupplySpatial.version?null:['returning','complete'].includes(transport.status)?transport.detourReturnPoints||transport.detourPoints:transport.detourPoints,status:transport.status==='transit'&&s.supplyProfile&&s.time>=transport.arrival-5?'receiving':transport.status,progress:Math.max(0,Math.min(1,progress))}:null,
   legacy:!!s&&!s.supplyProfile,readiness:s?.missions?.[mission]?.state||'Source setup',closed:!!s?.closed?.includes(mission)||s?.logistics?.routes[mission]?.status==='blocked',
   note:(operation?.supplyProfile?.version&&operation.supplyProfile.version!==CampaignSupplySpatial.version?'Placement corrected; recorded delivery times retained. ':'')+(basic?'Initial mission allowance. Resupply and combat expenditure are not simulated.':s.supplyProfile?'Route-based delivery schedule. Issued allowances are not per-shot consumption.':'Legacy operation: original delivery schedule retained; locations now use source-board geography.')};
 }
 function attach(d,operation,mission,campaignId){const binding=CampaignSupplySpatial.resolve(d,operation?.supplyProfile?.routes[d.situation.number]?.binding);d.supplyContext={campaignId,mission,operation:JSON.parse(JSON.stringify(operation)),version:CampaignSupplySpatial.version,binding:JSON.parse(JSON.stringify(window.CampaignGeography?{...binding,crs:'EPSG:3035',geography:CampaignGeography.register(binding)}:binding))};return d;}
 const forData=(d,side='Allied')=>inspect(d,d.supplyContext?.operation,d.supplyContext?.mission||'M04',side);
 return{inspect,attach,forData};
})();
