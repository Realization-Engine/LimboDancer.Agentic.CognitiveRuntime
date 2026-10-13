"use strict";
window.SquadLifecycle={
 ready(state,d){return state.scope==='full'&&SquadDeployment.ground(d).every(i=>state.parents.some(p=>p.id===i.id));},
 launch(state,d,plan){if(!this.ready(state,d))throw Error('Deploy the full Situation before opening the squad game map.');SquadDeployment.validate(state,d,plan);return {...state,gameLaunched:true};}
};
