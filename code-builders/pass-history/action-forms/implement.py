"""Initial action-form migration; run once in the source worktree."""
from pathlib import Path
root = Path(__file__).resolve().parents[3]
studio = root / 'src/ASL/LimboDancer.Domains.Asl.MapStudio'
panels = studio / 'Components/Play'
def save(p, s): p.write_text(s, encoding='utf-8', newline='\n')
p = panels/'SmallArmsFirePanel.razor'
s=p.read_text(encoding='utf-8').replace('<div class="toolbar">','<div class="action-form">\n<ActionFormGroup Legend="Firing group">',1)
a=s.index('    <label>\n        Target'); b=s.index('    @* Pass 60',a)
s=s[:a]+'''</ActionFormGroup>
<ActionFormGroup Legend="Target">
    <ActionLocationChoice Id="fire-target" Value="@Target" Choices="@Targets" Unavailable="@TargetsOut" Notes="@TargetNotes"
        ResetToken="@ResetToken" OnSuggested="OnTarget" OnFree="OnFreeTarget" OnEditingChanged="OnTargetEditing" />
'''+s[b:]
a=s.index('    @if (Phase == "mph")')
s=s[:a]+'</ActionFormGroup>\n<ActionFormGroup Legend="Additional choices">\n'+s[a:]
a=s.index('        <LocationField Id="fire-spray"');b=s.index('\n    }',a)
s=s[:a]+'''        @if (sprayOpen || Spray.Length > 0)
        {
'''+s[a:b]+'''
            <button type="button" class="link" @onclick="RemoveSpray">Remove spraying fire</button>
        }
        else
        {
            <button type="button" class="link" @onclick="@(() => sprayOpen = true)">Add spraying fire</button>
        }'''+s[b:]
a=s.index('    @if (FtWithOthers)')
s=s[:a]+'</ActionFormGroup>\n<ActionProposalArea Feedback="@ReadyFeedback">\n'+s[a:]
s=s.replace('(FreeTarget.Trim().Length == 0 && Target.Length == 0)','(TargetEditing || Target.Length == 0)')
s=s.replace('</div>\n</ActionSection>','</ActionProposalArea>\n</div>\n</ActionSection>')
s=s.replace('public string FreeTarget { get; set; } = string.Empty;','''public bool TargetEditing { get; set; }
    [Parameter] public int ResetToken { get; set; }
    [Parameter] public EventCallback<bool> OnTargetEditing { get; set; }
    private bool sprayOpen;
    private int lastReset;
    protected override void OnParametersSet()
    {
        if (lastReset != ResetToken) { sprayOpen = false; lastReset = ResetToken; }
    }
    private async Task RemoveSpray() { sprayOpen = false; await OnSpray.InvokeAsync(string.Empty); }
    private string? ReadyFeedback => TargetEditing ? "Finish or clear the location edit before proposing fire."
        : SelectedFirers.Count == 0 ? "Choose the firing location and select the units or weapons that fire."
        : Target.Length == 0 ? "Choose a target or pick it on the map." : $"Target: {Say(Target)}";''')
save(p,s)
p=studio/'Components/Pages/Play.razor';s=p.read_text(encoding='utf-8')
s=s.replace('FreeTarget="@fireFreeTarget"','TargetEditing="@fireTargetEditing" ResetToken="@fireResetToken" OnTargetEditing="@(pending => fireTargetEditing = pending)"')
s=s.replace('OnTarget="@(at => fireTarget = at)" OnFreeTarget="@(at => fireFreeTarget = at)"','OnTarget="@(at => SetFireTarget(at, false))" OnFreeTarget="@(at => SetFireTarget(at, true))"')
s=s.replace('private string fireFreeTarget = string.Empty;','''private bool fireTargetFree;
    private bool fireTargetEditing;
    private int fireResetToken;
    private void SetFireTarget(string value, bool free)
    {
        fireTarget = value;
        fireTargetFree = free;
        fireVehicle = string.Empty;
    }''')
s=s.replace('private string FireTarget => fireFreeTarget.Trim() is { Length: > 0 } typed ? typed : fireTarget;','private string FireTarget => fireTarget;')
s=s.replace('fireFreeTarget = string.Empty;','fireTargetFree = fireTargetEditing = false;\n        fireResetToken++;')
s=s.replace('if (fireTarget.Length > 0 && !FireTargets(side)','if (!fireTargetFree && fireTarget.Length > 0 && !FireTargets(side)')
s=s.replace('private Task ProposeFire()\n    {','private Task ProposeFire()\n    {\n        if (fireTargetEditing) return Task.CompletedTask;')
s=s.replace('private void SelectInspectorTab(string tab)\n    {','private void SelectInspectorTab(string tab)\n    {\n        if (tab != PlayTab) Picking.Arm(null, null);')
s=s.replace('private void MapKey(string key)\n    {','private void MapKey(string key)\n    {\n        if (key == "Escape" && Picking.Armed is not null) { Picking.Arm(null, null); return; }')
save(p,s)
for file,legend,button in [('OrdnanceFirePanel.razor','Weapon and assistance','ordnance'),('RallyActionPanel.razor','Unit and assistance','rally')]:
    p=panels/file;s=p.read_text(encoding='utf-8').replace('<div class="toolbar">',f'<div class="action-form">\n<ActionFormGroup Legend="{legend}">',1)
    if button=='ordnance':
        s=s.replace('    @Target','</ActionFormGroup>\n<ActionFormGroup Legend="Target and ammunition">\n    @Target',1)
    s=s.replace(f'    <button id="propose-{button}"',f'</ActionFormGroup>\n<ActionProposalArea>\n    <button id="propose-{button}"',1)
    s=s.replace('</div>','</ActionProposalArea>\n</div>',1)
    save(p,s)
p=studio/'wwwroot/site.css'
save(p,p.read_text(encoding='utf-8')+'''
/* Action entry: scoped to the forms, never the global toolbar. */
.action-form { min-width: 0; }
.action-form-group { border: 0; border-top: 1px solid #ddd6c8; margin: 1rem 0 0; padding: .65rem 0 .25rem; min-width: 0; }
.action-form-group > legend { font-weight: 700; padding: 0 .5rem 0 0; font-size: .95rem; }
.action-fields { display: flex; flex-wrap: wrap; align-items: flex-start; gap: .75rem 1rem; min-width: 0; }
.action-fields > label:has(select), .action-fields > label:has(input:not([type=checkbox])) { display: flex; flex-direction: column; align-items: flex-start; gap: .3rem; }
.action-form select, .action-form input { max-width: 100%; min-width: 0; }
.action-fields > *, .action-location-choice { min-width: 0; max-width: 100%; }
.action-location-choice { display: flex; flex-wrap: wrap; gap: .5rem; align-items: center; width: 100%; }
.action-location-choice > label, .action-location-choice > div { flex-basis: 100%; }
.action-location-choice > select { flex: 1 1 12rem; }
.action-proposal { border-top: 1px solid #ddd6c8; padding-top: .75rem; margin-top: 1rem; }
.action-buttons { display: flex; flex-wrap: wrap; gap: .5rem 1rem; align-items: center; }
.action-feedback { margin: 0 0 .5rem; font-size: .9rem; }
.action-form .pick-on-map { font-weight: 600; }
.action-form .location-field { max-width: 100%; }
.action-form .toolbar { align-items: flex-start; }
.action-form [hidden] { display: none !important; }
''')
