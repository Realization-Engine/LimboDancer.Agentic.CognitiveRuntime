"use strict";
// Optional deterministic logistics rules layered on the existing finite command ledger.
window.CampaignLogistics=(()=>{
 const check=(x,m)=>{if(!x)throw Error(m);};
 const handles=c=>c.type.startsWith('logistics:');
 const vehicles=s=>s.trips|| (s.transport?[s.transport]:[]);
 const keys=t=>t.route==='mixed'?['M14','M15']:[t.route||'M04'];
 const log=(s,text)=>s.events.push({at:s.time,text});
 function schedule(s,at,kind,data={}){s.queue.push({at,kind,data,seq:s.serial=(s.serial||0)+1});}
 const minutes=(s,t)=>Math.max(...keys(t).map(k=>s.supplyProfile.routes[k==='M04'?Object.keys(s.supplyProfile.routes)[0]:k.slice(1)].minutes));
 const cargo=(s,t)=>t.cargo||Object.keys(s.stock).filter(k=>s.stock[k]==='transit');
 const eventMatches=(e,t)=>['arrival','return','logistics:return'].includes(e.kind)&&(!e.data?.id||e.data.id===t.id);
 function freeze(s,t){
  if(!['transit','returning'].includes(t.status))return;
  t.logisticsCargo=cargo(s,t);t.resumeStatus=t.status;t.remaining=Math.max(1,(s.queue.find(e=>eventMatches(e,t))?.at||t.arrival)-s.time);
  t.frozenProgress=t.status==='returning'&&t.returnStarted!==undefined?progress(s,t,0):t.progressStart===undefined?Math.max(0,Math.min(1,(s.time-t.depart)/Math.max(1,t.arrival-t.depart-(t.resumeStatus==='transit'?5:0)))):t.progressStart+(1-t.progressStart)*Math.max(0,(s.time-t.resumeAt)/(t.arrival-t.resumeAt));
  t.status='held';s.queue=s.queue.filter(e=>!eventMatches(e,t));log(s,'Transport held. Cargo remains assigned to its vehicle.');
 }
 function release(s,t){
  if(t.status!=='held')return;t.status=t.resumeStatus||'transit';t.resumeAt=s.time;t.progressStart=t.frozenProgress||0;t.arrival=s.time+t.remaining;
  if(t.status==='returning'){t.returnStarted=s.time;t.returnDuration=t.remaining;t.returnProgress=t.frozenProgress;}
  schedule(s,t.arrival,t.status==='returning'?(s.trips?'return':'logistics:return'):'arrival',{id:t.id});log(s,'Transport released; arrival forecast updated.');
 }
 function blocked(s,t){if(t.detour)return false;return keys(t).some(k=>['blocked','threatened'].includes(s.logistics.routes[k]?.status));}
 function apply(s,c){
  if(c.type==='logistics:enable'){
   check(!s.plan&&!s.logistics,'Choose advanced logistics before issuing orders.');check(s.supplyProfile,'Start a new operation to enable advanced logistics.');
   check(['normal','roadblock','threat'].includes(c.challenge),'Choose a logistics condition.');
   s.logistics={version:1,challenge:c.challenge,capacity:2,engineersAvailable:1,escortsAvailable:1,routes:Object.fromEntries(Object.keys(s.missions).map(k=>[k,{status:'open',repairAt:null}]))};if(c.rear)window.CampaignRearSupply.enable(s);log(s,'Advanced logistics enabled: '+c.challenge+'. Two cargo slots, one repair team and one escort.');return;
  }
  check(s.logistics,'Advanced logistics is not enabled.');const r=s.logistics.routes[c.route];
  if(c.type==='logistics:repair'){
   check(r?.status==='blocked'&&!r.repairAt,'Select a blocked route without a repair in progress.');check(s.logistics.engineersAvailable>0,'Repair team is already assigned.');
   s.logistics.engineersAvailable--;r.repairAt=s.time+20;schedule(s,r.repairAt,'logistics:repair',{route:c.route});log(s,'Repair team dispatched. Route reopens in 20 campaign minutes.');
  }else if(c.type==='logistics:escort'){
   check(r?.status==='threatened','Escort requires a threatened route.');check(s.logistics.escortsAvailable>0,'Escort already assigned.');s.logistics.escortsAvailable--;r.escort=true;
   schedule(s,s.time+10,'logistics:escort',{route:c.route});log(s,'Escort assembling; escorted passage available in 10 minutes.');
  }else if(c.type==='logistics:reroute'){
   const t=vehicles(s).find(t=>(t.id||'transport')===c.trip);check(t?.status==='held'&&t.resumeStatus==='transit'&&keys(t).length===1,'Rerouting requires one held outbound destination.');
   const key=keys(t)[0],profile=s.supplyProfile.routes[key==='M04'?Object.keys(s.supplyProfile.routes)[0]:key.slice(1)],alt=profile.detour;check(alt,'No connected alternate road is available. Repair or recall instead.');
   const main=profile.binding.sides.Allied.points,f=t.frozenProgress||0,at=window.CampaignSupplySpatial.point(main,f),travelled=[main[0]];let distance=0,target=window.CampaignSupplySpatial.length(main)*f;for(let i=1;i<main.length;i++){distance+=Math.hypot(main[i].x-main[i-1].x,main[i].y-main[i-1].y);if(distance>=target)break;travelled.push(main[i]);}travelled.push(at);
   t.detourPoints=[...travelled.reverse(),...alt.points.slice(1)];t.detour=true;t.detourReturnPoints=alt.points;t.detourReturnMinutes=alt.minutes;t.detourMinutes=Math.ceil(window.CampaignSupplySpatial.length(t.detourPoints)*profile.binding.metresPerUnit/1000/12*60/5)*5+5;t.status='transit';t.depart=s.time;t.arrival=s.time+t.detourMinutes;delete t.progressStart;delete t.frozenProgress;schedule(s,t.arrival,'arrival',{id:t.id});log(s,'Convoy rerouted on a connected source-road alternative, including backtracking to the depot junction.');
  }else if(c.type==='logistics:recall'){
   const t=vehicles(s).find(t=>(t.id||'transport')===c.trip);check(t?.status==='held'&&t.resumeStatus!=='returning','Only held outbound cargo can be recalled.');
   t.status='returning';t.recalled=true;t.logisticsCargo=cargo(s,t).length?cargo(s,t):t.logisticsCargo;
   t.returnStarted=s.time;t.returnDuration=Math.max(5,Math.ceil(minutes(s,t)*(t.frozenProgress||0)));t.returnProgress=t.frozenProgress||0;
   schedule(s,s.time+t.returnDuration,s.trips?'return':'logistics:return',{id:t.id});log(s,'Convoy recalled. Cargo becomes available only on depot receipt.');
  }else if(c.type==='logistics:redispatch'){
   const t=vehicles(s).find(t=>(t.id||'transport')===c.trip);check(t?.recalled&&t.status==='complete','Wait for recalled cargo to return.');check(!blocked(s,t),'Route still unavailable.');
   check(!vehicles(s).some(v=>v!==t&&['transit','returning','held'].includes(v.status)),'Shared transport is busy.');
   for(const key of t.logisticsCargo){check(s.stock[key]==='depot','Cargo unavailable.');s.stock[key]=t.id||'transit';}
   t.recalled=false;t.status='transit';t.actualDeparture=s.time;t.depart=s.time;t.arrival=s.time+minutes(s,t);delete t.progressStart;delete t.frozenProgress;delete t.returnStarted;
   schedule(s,t.arrival,'arrival',{id:t.id});log(s,'Returned cargo dispatched again.');
  }else throw Error('Unknown logistics decision.');
 }
 function process(s,e){
  if(window.CampaignRearSupply?.process(s,e))return true;if(!e.kind.startsWith('logistics:'))return false;const L=s.logistics;check(L,'Missing logistics state.');
  if(e.kind==='logistics:incident'){
   const r=L.routes[e.data.route];r.status=L.challenge==='threat'?'threatened':'blocked';log(s,'Authored logistics condition: '+e.data.route+' '+r.status+'. No combat outcome has been simulated.');
   for(const t of vehicles(s))if(keys(t).includes(e.data.route))freeze(s,t);
  }
  if(e.kind==='logistics:repair'||e.kind==='logistics:escort'){
   const r=L.routes[e.data.route];r.status=e.kind==='logistics:repair'?'reopened':'escorted';r.repairAt=null;if(e.kind==='logistics:repair')L.engineersAvailable++;
   for(const t of vehicles(s))if(!blocked(s,t))release(s,t);log(s,e.data.route+' '+r.status+'.');
  }
  if(e.kind==='logistics:return'){
   const t=s.transport;check(t?.status==='returning','No returning transport.');if(t.recalled)for(const key of t.logisticsCargo)s.stock[key]='depot';t.status='complete';log(s,'Transport returned to depot.');
   for(const r of Object.values(L.routes))if(r.escort){r.escort=false;r.status='open';L.escortsAvailable++;}
  }
  return true;
 }
 function after(s,c){if(!s.logistics)return;if(s.trips&&s.trips.every(t=>t.status==='complete'))for(const r of Object.values(s.logistics.routes))if(r.escort){r.escort=false;r.status='open';s.logistics.escortsAvailable++;}
  if(c.type==='allocate'&&s.logistics.challenge!=='normal'){const route=s.plan.main||'M04';schedule(s,s.staff+5,'logistics:incident',{route});}
  window.CampaignRearSupply?.after(s,c);
  for(const t of vehicles(s)){
   if(t.status==='returning'&&t.detour&&!t.detourReturn){t.detourReturn=true;t.returnStarted=s.time;t.returnDuration=t.detourReturnMinutes;t.returnProgress=1;for(const e of s.queue)if(eventMatches(e,t))e.at=s.time+t.detourReturnMinutes;}
   if(t.status==='transit'){if(t.actualDeparture===undefined){t.actualDeparture=s.time;t.depart=s.time;}check(cargo(s,t).length<=s.logistics.capacity,'Convoy cargo capacity exceeded.');if(blocked(s,t))freeze(s,t);}
   if(!s.trips&&t.status==='delivered'){t.status='returning';t.returnStarted=s.time;t.returnDuration=t.detour?t.detourReturnMinutes:minutes(s,t);if(t.detour)t.detourReturn=true;t.returnProgress=1;schedule(s,s.time+t.returnDuration,'logistics:return');}
  }
 }
 function progress(s,t,normal){if(!s.logistics)return normal;if(t.status==='complete')return 0;if(t.detour&&t.status==='transit')return(s.time-t.depart)/(t.arrival-t.depart);if(t.status==='held')return t.frozenProgress||0;if(t.status==='returning'&&t.returnStarted!==undefined)return t.returnProgress*(1-(s.time-t.returnStarted)/t.returnDuration);if(t.progressStart!==undefined&&t.status==='transit')return t.progressStart+(1-t.progressStart)*(s.time-t.resumeAt)/(t.arrival-t.resumeAt);return normal;}
 return{handles,apply,process,after,progress};
})();
