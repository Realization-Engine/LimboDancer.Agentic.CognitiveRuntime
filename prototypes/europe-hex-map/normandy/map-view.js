"use strict";
window.NormandyMap={draw(svg,world,state,select,options={}){
 const d=options.source||window.PANZER_SITUATION_DATA;
 CampaignSupplyView.command(svg,d,state,select,options);
}};
