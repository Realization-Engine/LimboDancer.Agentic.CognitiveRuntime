"use strict";
window.SquadRepository = {
 raw(d){const raw=localStorage.getItem(this.key(d));return raw?JSON.parse(raw):null;},
 archive(d,s){localStorage.setItem(this.key(d)+":archive:"+Date.now(),JSON.stringify(s));},
 key:d=>"campaign-atlas:"+d.id+":squad-setup:v1",
 load(d,plan){const raw=localStorage.getItem(this.key(d));return raw?SquadDeployment.validate(JSON.parse(raw),d,plan):null;},
 save(d,plan,state){SquadDeployment.validate(state,d,plan);localStorage.setItem(this.key(d),JSON.stringify(state));},
 scoped(storage){return {raw(){return storage.load();},archive(d,s){localStorage.setItem("campaign-atlas:"+d.id+":squad-archive:"+Date.now(),JSON.stringify(s));},load(d,plan){const s=storage.load();return s?SquadDeployment.validate(s,d,plan):null;},save(d,plan,s){SquadDeployment.validate(s,d,plan);storage.save(JSON.parse(JSON.stringify(s)));},clear(){storage.clear();}};},
 clear(d){localStorage.removeItem(this.key(d));}
};
