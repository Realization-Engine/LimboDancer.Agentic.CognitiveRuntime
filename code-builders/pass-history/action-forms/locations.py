"""Migrate restricted destination selectors without allowing arbitrary input."""
from pathlib import Path
root=Path(__file__).resolve().parents[3]
p=root/'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Play'
for name,start,end,replacement in [
    ('AdvanceActionPanel.razor','    <label>\n        To','    <PickedOption For="advance-to"',
     '    <ActionLocationChoice Id="advance-to" Label="Destination" AllowFreeEntry="false" Value="@To" Choices="@Destinations" OnSuggested="OnTo" />'),
    ('OrdnanceTargetFields.razor','<label>\n    Target','<PickedOption For="ordnance-target"',
     '<ActionLocationChoice Id="ordnance-target" AllowFreeEntry="false" Value="@Target" Choices="@Targets" OnSuggested="OnTarget" />')]:
    f=p/name;s=f.read_text(encoding='utf-8');a=s.index(start);b=s.index('\n',s.index(end,a))
    s=s[:a]+replacement+s[b:];f.write_text(s,encoding='utf-8',newline='\n')
