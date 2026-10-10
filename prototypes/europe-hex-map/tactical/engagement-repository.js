"use strict";
window.SquadRepository = {
 key:d=>"campaign-atlas:"+d.id+":squad-setup:v1",
 load(d,plan){const raw=localStorage.getItem(this.key(d));return raw?SquadDeployment.validate(JSON.parse(raw),d,plan):null;},
 save(d,plan,state){SquadDeployment.validate(state,d,plan);localStorage.setItem(this.key(d),JSON.stringify(state));},
 clear(d){localStorage.removeItem(this.key(d));}
};
