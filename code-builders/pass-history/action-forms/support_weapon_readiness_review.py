"""Convert the two new possession verdicts to structured readiness without changing refusal text."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[3] / 'src/ASL'
path = root / 'LimboDancer.Domains.Asl.Rules/ScenarioA1SupportWeaponPossessionRules.cs'
text = path.read_text(encoding='utf-8')
for method, end in [('TransferRefusal', '    /// <summary>A4.43:'), ('RecoveryRefusal', '\n}\n')]:
    start = text.index('    public static string? ' + method)
    stop = text.index(end, start)
    part = text[start:stop].replace('public static string? ', 'public static SupportWeaponReadiness ', 1)
    part = part.replace('        ArgumentNullException.ThrowIfNull(facts);',
                        '        ArgumentNullException.ThrowIfNull(facts);\n'
                        '        if (facts.Phase is not ("rph" or "pfph" or "mph" or "dfph" or "afph" or "rtph" or "aph" or "ccph"))\n'
                        '        {\n            return SupportWeaponReadiness.NotEvaluated("The phase is not known.");\n        }')
    def wrap(match):
        value = match.group(1)
        kind = 'NotEvaluated' if any(word in value for word in ('cannot be evaluated', 'not supported')) else 'Refused'
        return 'return SupportWeaponReadiness.' + kind + '(' + value + ');'
    part = re.sub(r'return (\$?"[^\n]+?");', wrap, part)
    part = part.replace('return aboardBar;', 'return SupportWeaponReadiness.NotEvaluated(aboardBar);')
    part = part.replace('return facts.ReactionPending ? "Resolve defensive fire first." : "Finish the current unit\'s move first.";',
                        'return SupportWeaponReadiness.Refused(facts.ReactionPending ? "Resolve defensive fire first." : "Finish the current unit\'s move first.");')
    part = part.replace('? "play.transfer-phase: transfer before any unit advances (A4.431)" : null;',
                        '? SupportWeaponReadiness.Refused("play.transfer-phase: transfer before any unit advances (A4.431)") : SupportWeaponReadiness.Allowed;')
    part = part.replace('return null;', 'return SupportWeaponReadiness.Allowed;')
    text = text[:start] + part + text[stop:]
path.write_text(text, encoding='utf-8', newline='\n')
