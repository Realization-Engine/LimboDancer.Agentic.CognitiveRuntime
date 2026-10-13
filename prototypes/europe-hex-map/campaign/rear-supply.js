"use strict";
// Finite rear transport uses the existing command clock, stock and replay log.
window.CampaignRearSupply=(()=>{
 const check=(x,m)=>{if(!x)throw Error(m);},schedule=(s,at,kind)=>s.queue.push({at,kind,seq:s.serial=(s.serial||0)+1});
 function enable(s){
  const network=s.supplyProfile?.rear;check(network,'Start a new operation to include rear-area supply.');check(network.available!==false,'This isolated force has no established rear supply corridor.');check(!s.plan&&!s.rearSupply,'Choose rear supply before orders.');
  const cargo=Object.keys(s.stock).filter(k=>s.stock[k]==='depot');check(cargo.length<=network.capacity,'Rear transport capacity exceeded.');
  s.rearSupply={network,status:'at rear base',cargo};for(const key of cargo)s.stock[key]='rear';
  s.events.push({at:s.time,text:'Mission packages assigned to '+network.hubName+'. Forward depot starts empty.'});
 }
 function after(s,c){const r=s.rearSupply;if(!r||c.type!=='allocate')return;
  r.depart=s.time+s.staff;r.arrival=r.depart+r.network.travelMinutes+r.network.handlingMinutes;r.status='scheduled';
  // Forward dispatch waits for actual depot receipt and five minutes of handover.
  const delay=r.arrival+5-(s.time+s.staff);
  for(const e of s.queue)if(['dispatch','logistics:incident'].includes(e.kind))e.at+=delay;
  for(const t of s.trips||[])t.depart+=delay;
  schedule(s,r.depart,'logistics:rear-dispatch');for(const fraction of [.25,.5,.75])schedule(s,r.depart+Math.ceil(r.network.travelMinutes*fraction),'logistics:rear-progress');schedule(s,r.depart+r.network.travelMinutes,'logistics:rear-unloading');schedule(s,r.arrival,'logistics:rear-receipt');
  s.events.push({at:s.time,text:'Forward delivery waits for rear convoy receipt; readiness forecasts revised.'});
 }
 function process(s,e){if(!e.kind.startsWith('logistics:rear-'))return false;const r=s.rearSupply;check(r,'Missing rear convoy.');
  if(e.kind==='logistics:rear-dispatch'){check(r.status==='scheduled','Rear convoy already dispatched.');for(const k of r.cargo){check(s.stock[k]==='rear','Rear package unavailable.');s.stock[k]='rear-transit';}r.status='outbound';}
  else if(e.kind==='logistics:rear-progress'){check(r.status==='outbound','Rear convoy is not moving.');}
  else if(e.kind==='logistics:rear-unloading'){check(r.status==='outbound','Rear convoy is not arriving.');r.status='unloading';}
  else if(e.kind==='logistics:rear-receipt'){check(r.status==='unloading','No inbound rear convoy.');for(const k of r.cargo){check(s.stock[k]==='rear-transit','Cargo custody mismatch.');s.stock[k]='depot';}r.status='returning';r.returnAt=s.time+r.network.travelMinutes;schedule(s,r.returnAt,'logistics:rear-return');}
  else if(e.kind==='logistics:rear-return'){check(r.status==='returning','Rear convoy is not returning.');r.status='complete';}
  else throw Error('Unknown rear transport event.');
  s.events.push({at:s.time,text:e.kind==='logistics:rear-receipt'?'Rear convoy received at forward depot. Mission packages available.':e.kind==='logistics:rear-dispatch'?'Rear convoy departed; cargo unavailable to forward forces.':e.kind==='logistics:rear-unloading'?'Rear convoy unloading. Packages remain unavailable until depot receipt.':e.kind==='logistics:rear-progress'?'Rear convoy progress reported on the continental map.':'Rear transport returned to base.'});return true;
 }
 function progress(s){const r=s.rearSupply;if(!r)return 0;if(r.status==='unloading')return 1;if(r.status==='outbound')return Math.max(0,Math.min(1,(s.time-r.depart)/r.network.travelMinutes));if(r.status==='returning')return Math.max(0,1-(s.time-r.arrival)/r.network.travelMinutes);return 0;}
 return{enable,after,process,progress};
})();
