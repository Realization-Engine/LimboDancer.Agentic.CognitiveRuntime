"use strict";
// Atlas commands delegate to the same repositories and optimistic revisions as the command panes.
window.CampaignRearController={advance(number,expected){
 const source=PANZER_SITUATION_LIBRARY.find(d=>d.situation.number===number);let repo,model;
 if(number===4){repo=NormandyRepository.repository(localStorage);model=NormandyCommand;}
 else if([14,15].includes(number)){repo=window.CampaignRepository;model=ArdennesCommand;}
 else{model=SourceCommand.create(source);repo=SourceRepository.create(localStorage,source,model,CampaignNavigation.find(number));}
 const world=repo.load();if(!world||world.id!==expected.operationId||world.revision!==expected.revision)throw Error('Campaign changed. Review the refreshed map before advancing.');
 const command={id:crypto.randomUUID(),revision:world.operation.revision,type:'advance'};
 if([14,15].includes(number))repo.saveOperation(model.execute(world.operation,command));else repo.execute(command,source);
 window.dispatchEvent(new Event('campaign-atlas-logistics-changed'));
}};
