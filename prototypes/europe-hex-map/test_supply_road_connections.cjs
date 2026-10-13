const fs=require('fs'),vm=require('vm'),assert=require('node:assert/strict');
const c={window:{},SquadSpace:{project:(_,x,y)=>({x,y})},CampaignSupplySpatial:{point:()=>({x:100,y:0})}};vm.createContext(c);vm.runInContext(fs.readFileSync('campaign/route-network.js','utf8'),c);
const road=ps=>({kind:'road',geometry:{boardId:'C',coordinates:ps}}),binding={metresPerUnit:1,layout:[{boardId:'C'}],sides:{German:{boardId:'C',points:[{x:0,y:0},{x:100,y:0}]}}};
// The closest segment is disconnected. The connected road is still within the allowed final approach.
const d={mapModel:{routes:[road([[0,0],[200,0]]),road([[150,30],[200,30]])]}};
const p=c.window.CampaignRouteNetwork.to(d,binding,'German',{x:175,y:30});assert.ok(p);assert.ok(p.points.some(p=>p.x===175&&p.y===0));assert.deepEqual(JSON.parse(JSON.stringify(p.points.at(-1))),{x:175,y:30});
// Collinear overlapping traces must form one connected road.
const overlap={mapModel:{routes:[road([[0,0],[100,0]]),road([[50,0],[250,0]])]}};
assert.ok(c.window.CampaignRouteNetwork.to(overlap,binding,'German',{x:250,y:0}));
assert.equal(c.window.CampaignRouteNetwork.to(d,binding,'German',{x:500,y:500}),null);
console.log('PASS reachable road approaches, collinear junctions, and truly disconnected targets');
