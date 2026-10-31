"""Give existing contextual action groups consistent field flow, retaining their commands."""
from pathlib import Path
import re
root=Path(__file__).resolve().parents[3]
panels=root/'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play'
names=['AmbushWithdrawalActions','BerserkOverrunCloseCombat','CloseCombatAttackBuilder','CloseCombatInfiltrationChoices',
       'CloseCombatStacking','CloseCombatWithdrawals','CrewExposureActions','DmRetentionChoices','GunArcAction','MoppingUpAction',
       'OpportunityFireAction','PlacedChargeDetonationActions','PrisonerCustodyActions','ReactionFireAction','ShockRecoveryAction',
       'SpotterDesignationPanel','StarshellActionPanel','SupportWeaponActionPanel','ThrowDemolitionChargeAction',
       'VehicleCrewActions','VehicleMovementPanel','VehicleScroungeActions']
for name in names:
    p=panels/(name+'.razor');s=p.read_text(encoding='utf-8')
    s=s.replace('class="toolbar', 'class="toolbar action-fields')
    p.write_text(s,encoding='utf-8',newline='\n')
# Keep the individual side's attackers and defenders visibly separate.
p=panels/'CloseCombatAttackBuilder.razor';s=p.read_text(encoding='utf-8')
s=s.replace('        <span>attack with</span>', '<ActionFormGroup Legend="Attackers">')
s=s.replace('        <span>attack</span>', '</ActionFormGroup>\n<ActionFormGroup Legend="Defenders">')
s=s.replace('    </div>\n}', '</ActionFormGroup>\n    </div>\n}',1)
p.write_text(s,encoding='utf-8',newline='\n')
