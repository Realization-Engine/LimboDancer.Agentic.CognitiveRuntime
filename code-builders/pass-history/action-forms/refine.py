"""Second action-form pass: compact forms and distinct movement groups."""
from pathlib import Path
root=Path(__file__).resolve().parents[3]
studio=root/'src/ASL/LimboDancer.Domains.Asl.MapStudio'
p=studio/'Components/Play'
def write(f,s): f.write_text(s,encoding='utf-8',newline='\n')
for name,legend,button in [('RepairActionPanel','Weapon and participant','propose-repair'),('DeployActionPanel','Squad and leader','propose-deploy'),('RecombineActionPanel','Units and leader','propose-recombine')]:
    f=p/(name+'.razor');s=f.read_text(encoding='utf-8')
    if f'id="{button}"' not in s: continue
    s=s.replace('<div class="toolbar">',f'<div class="action-form">\n<ActionFormGroup Legend="{legend}">',1)
    s=s.replace(f'    <button id="{button}"',f'</ActionFormGroup>\n<ActionProposalArea>\n    <button id="{button}"',1)
    s=s.replace('</div>','</ActionProposalArea>\n</div>',1)
    write(f,s)
f=p/'AdvanceActionPanel.razor';s=f.read_text(encoding='utf-8').replace('<div class="toolbar">','<div class="action-form">',1)
s=s.replace('    <label>\n        To','<ActionFormGroup Legend="Destination">\n    <label>\n        To',1)
s=s.replace('    <button id="propose-advance-units"','</ActionFormGroup>\n<ActionProposalArea>\n    <button id="propose-advance-units"',1)
s=s.replace('    <MapExitAction','</ActionProposalArea>\n    <MapExitAction',1);write(f,s)
f=p/'InfantryMovementAction.razor';s=f.read_text(encoding='utf-8')
s=s.replace('<LocationField Id="move-to"','<ActionFormGroup Legend="Destination">\n<LocationField Id="move-to"',1)
end=s.index('\n',s.index('<LocationField Id="move-to"'))
s=s[:end]+'\n</ActionFormGroup>\n<ActionFormGroup Legend="Movement choices">'+s[end:]
s=s.replace('<button id="propose-move"','</ActionFormGroup>\n<ActionProposalArea>\n<button id="propose-move"',1)
s=s.replace('</details>','</ActionProposalArea>\n</details>',1);write(f,s)
f=p/'RoutActionPanel.razor';s=f.read_text(encoding='utf-8')
a=s.index('<div class="toolbar">',s.index('id="rout-obligations"'))
s=s[:a]+s[a:].replace('<div class="toolbar">','<div class="action-form">\n<ActionFormGroup Legend="Unit and assistance">',1)
s=s.replace('    <LocationField Id="rout-route"','</ActionFormGroup>\n<ActionFormGroup Legend="Route">\n    <LocationField Id="rout-route"',1)
s=s.replace('    <button id="propose-rout"','</ActionFormGroup>\n<ActionProposalArea>\n    <button id="propose-rout"',1)
idx=s.index('</div>',s.index('<ActionProposalArea>'))
s=s[:idx]+'</ActionProposalArea>\n'+s[idx:];write(f,s)
f=studio/'wwwroot/site.css'
write(f,f.read_text(encoding='utf-8')+'''
.action-form .toolbar > label:has(select), .action-form .toolbar > label:has(input:not([type=checkbox])) { display: flex; flex-direction: column; gap: .3rem; max-width: 100%; }
.action-form .toolbar > .location-field, .action-form .toolbar > fieldset { flex-basis: 100%; }
.action-form .toolbar > button[id^="propose-"] { margin-top: .5rem; }
.action-form .rule-help { margin: .5rem 0; }
.action-form .fire-range { width: 100%; }
''')
tests=root/'src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests'
f=tests/'CloseCombatComponentTests.cs';s=f.read_text(encoding='utf-8').replace('item => item.FreeTarget','item => item.Target');write(f,s)
for name in ['LocationWordsTests.cs','PlayPagePass31cTests.cs']:
    f=tests/name;s=f.read_text(encoding='utf-8').replace('Assert.Equal("Pick on the map",','Assert.Equal("Pick \\"To\\" on map",'.replace('\\"','\\"'))
    s=s.replace('Assert.Equal("Stop picking",','Assert.Equal("Cancel picking",')
    write(f,s)
