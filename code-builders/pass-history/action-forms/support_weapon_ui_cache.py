"""Pass the immutable state token to each SW workflow and reuse the page's location index."""
from pathlib import Path
path = Path(__file__).resolve().parents[3] / 'src/ASL/LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor'
text = path.read_text(encoding='utf-8')
text = text.replace('<SupportWeaponWorkflow ', '<SupportWeaponWorkflow Snapshot="@state" ')
text = text.replace('Locations="@(swShown.ToDictionary(item => item.Id, item => state.Location(item.Id)?.Location.ToString()))"',
                    'Locations="@WeaponLocations"')
text = text.replace('Locations="@(repairShown.Where(item => !item.Id.Contains(\'|\')).ToDictionary(item => item.Id, item => state.Location(item.Id)?.Location.ToString()))"',
                    'Locations="@WeaponLocations"')
text = text.replace('Locations="@(available.ToDictionary(item => item.Id, item => state.Location(item.Id)?.Location.ToString()))"',
                    'Locations="@WeaponLocations"')
path.write_text(text, encoding='utf-8', newline='\n')
