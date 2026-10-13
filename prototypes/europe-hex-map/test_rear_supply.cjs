const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict'),path=require('path');
const c=vm.createContext({window:{},console,Map,Set});function load(f){vm.runInContext(fs.readFileSync(path.join(__dirname,f),'utf8'),c);Object.assign(c,c.window);}
for(const f of JSON.parse(fs.readFileSync(path.join(__dirname,'situation-data/scripts.json'))))load(f);
for(const f of ['asl-counter-definitions','organization-profiles','library-profiles','source-composition','tactical-estimates','spatial-frame'])load('tactical/'+f+'.js');
for(const f of ['campaign/geography.js','campaign/rear-network.js','campaign/rear-supply.js','campaign/logistics.js','campaign/supply-placement.js','campaign/supply-spatial.js','campaign/supply-state.js','normandy/command-state.js','campaign/source-command.js','ardennes-command-state.js','map-data.js','campaign/interstitial-terrain.js'])load(f);
const N=vm.runInContext('NormandyCommand',c),A=vm.runInContext('ArdennesCommand',c);let serial=0;const act=(M,s,type,data={})=>M.execute(s,{id:'rear-'+ ++serial,revision:s.revision,type,...data});
for(const d of c.PANZER_SITUATION_LIBRARY){const n=d.situation.number,M=n===4?N:[14,15].includes(n)?A:c.SourceCommand.create(d);let s=M.initial('rear-'+n);const profile=s.supplyProfile,route=profile.rear,b=profile.routes[route.sourceNumber].binding,depot=b.sides.Allied.nodes.find(n=>n.kind==='depot');const end=c.CampaignGeography.world(b,depot.position);assert.ok(Math.hypot(end.x-route.points.at(-1).x,end.y-route.points.at(-1).y)<1e-7);assert.ok(route.km>0&&route.travelMinutes>0);
 if(route.available===false){assert.throws(()=>act(M,s,'logistics:enable',{challenge:'normal',rear:true}),/isolated/);console.log('PASS',n,'isolated rear access refused');continue;}
 s=act(M,s,'logistics:enable',{challenge:'normal',rear:true});assert.ok(Object.values(s.stock).every(v=>v==='rear'));
 s=act(M,s,'allocate',{plan:M===A?{main:'M14',reserve:'M14',delivery:'M14'}:n===4?{supply:'send',reserve:'attach'}:{delivery:'standard'},sourceHashes:{M04:'x',M14:'x',M15:'x'}});
 const arrival=s.rearSupply.arrival;assert.ok(s.queue.filter(e=>e.kind==='dispatch').every(e=>e.at>arrival));let guard=0;
 while(s.queue.length&&guard++<40){s=act(M,s,'advance');if(s.time<arrival)assert.ok(Object.values(s.stock).every(v=>['rear','rear-transit'].includes(v)));}
 assert.ok(guard<40);assert.equal(s.rearSupply.status,'complete');assert.ok(Object.values(s.stock).every(v=>['M04','M14','M15'].includes(v)));assert.equal(JSON.stringify(M.restore(s)),JSON.stringify(s));console.log('PASS',n,route.km.toFixed(1)+' km',N.clock(s.time));
}
const terrain=c.CampaignInterstitialTerrain,a=terrain.sample(-700.5,180.2),b=terrain.sample(-700.1,180.8);assert.equal(a.id,b.id);assert.equal(a.type,b.type);assert.equal(terrain.sample(-700.5,180.2).type,a.type);assert.equal(N.clock(1440),'Day +1 06:00');console.log('PASS shared continental grid, stable refinement and multi-day clock.');
