"""Generate the approved Support Weapons calculators' fact-only workbench adapters."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[3]
RULES = ROOT / 'src/ASL/LimboDancer.Domains.Asl.Rules'


def adapter(name, rule, page, entries):
    methods = []
    for method, record, call, phase, step in entries:
        rule = {'Recovery': 'A4.44', 'Transfer': 'A4.431'}.get(method, rule)
        source = (RULES / f'ScenarioA1{name}Rules.cs').read_text(encoding='utf-8')
        signature = re.search(r'public sealed record ' + record + r'\((.*?)\);', source, re.S).group(1)
        fields = [re.match(r'\s*(\w+\??)\s+(\w+)', part).groups() for part in signature.split(',')]
        declarations = ['            new("unitId", ContractFactType.Unit, true, ContractDisclosure.Owner, "The acting unit") { Concerns = "unitId" },']
        guards = ['facts.Text("unitId") is null']
        args = []
        for kind, field in fields:
            key = field[0].lower() + field[1:]
            primitive = kind.rstrip('?')
            facttype, getter = {'bool': ('Flag', 'Flag'), 'int': ('Number', 'Number'), 'string': ('Text', 'Text')}[primitive]
            desc = re.sub(r'(?<!^)(?=[A-Z])', ' ', field) + (' (none when not aboard)' if field.endswith('Vehicle') else '')
            declarations.append(f'            new("{key}", ContractFactType.{facttype}, true, ContractDisclosure.Owner, "{desc}") {{ Concerns = "unitId" }},')
            # Nullable carriage IDs represent not aboard with an empty string in the adapter.
            guards.append(f'facts.{getter}("{key}") is null')
            if primitive == 'int':
                guards.append(f'facts.Number("{key}") != decimal.Truncate(facts.Number("{key}")!.Value)')
                guards.append(f'facts.Number("{key}") < 0 || facts.Number("{key}") > int.MaxValue')
                args.append(f'checked((int)facts.Number("{key}")!.Value)')
            elif primitive == 'bool':
                args.append(f'facts.Flag("{key}")!.Value')
            elif field.endswith('Vehicle'):
                args.append(f'facts.Text("{key}") == "none" ? null : facts.Text("{key}")')
            else:
                args.append(f'facts.Text("{key}")!')
        contractid = rule.lower() + '-' + method.lower()
        verdict = (f'var refusal = ScenarioA1{name}Rules.{call}(input);\n'
                   '        var outcome = refusal is null ? "allowed" : "refused";\n'
                   f'        return new(outcome, refusal is null ? [] : [new(refusal, "{rule}", {page})], []);') if call.endswith('Refusal') else (
                   f'var readiness = ScenarioA1{name}Rules.{call}(input);\n'
                   '        var outcome = readiness.Status switch\n        {\n'
                   '            SupportWeaponReadinessStatus.Allowed => "allowed",\n'
                   '            SupportWeaponReadinessStatus.Refused => "refused",\n'
                   '            _ => ContractVerdict.NotEvaluated,\n        };\n'
                   f'        return new(outcome, readiness.Reason is null ? [] : [new(readiness.Reason, "{rule}", {page})], []);')
        methods.append(f'''    [RuleContract]
    private static RuleContract {method}() => new("{contractid}", "Support Weapons: {method}", [new("{rule}", {page})],
        [
{chr(10).join(declarations)}
        ],
        ["allowed", "refused", ContractVerdict.NotEvaluated], [], new("{phase}", "{step}"))
    {{
        Decide = {method}Verdict,
    }};

    private static ContractVerdict {method}Verdict(ContractFacts facts)
    {{
        if ({(' ||' + chr(10) + '            ').join(guards)})
        {{
            return new(ContractVerdict.NotEvaluated, [new("Supply the acting side's known facts; integer quantities must be whole numbers.", "{rule}", {page})], []);
        }}

        var input = new {record}(
            {(',' + chr(10) + '            ').join(args)});
        {verdict}
    }}''')
    text = f'''namespace LimboDancer.Domains.Asl.Rules;

/// <summary>Owner-scoped adapters for the approved {rule} delivery. Callers gather facts; the calculators decide readiness.</summary>
internal static class ScenarioA1{name}Contract
{{
{(chr(10) + chr(10)).join(methods)}
}}
'''
    (RULES / f'ScenarioA1{name}Contract.cs').write_text(text, encoding='utf-8', newline='\n')


adapter('SupportWeaponPossession', 'A4.43', 50, [
    ('Recovery', 'SupportWeaponRecoveryFacts', 'RecoveryRefusal', 'RPh/MPh', '1.14B/3.32A'),
    ('Transfer', 'SupportWeaponTransferFacts', 'TransferRefusal', 'RPh/APh', '1.22B/7.11A'),
])
adapter('SupportWeaponRepair', 'A9.72', 65, [('Repair', 'SupportWeaponRepairFacts', 'Evaluate', 'RPh', '1.22B')])
adapter('SupportWeaponConfiguration', 'A9.8', 65, [('Configuration', 'SupportWeaponConfigurationFacts', 'Evaluate', 'PFPh/DFPh', '2.22A/4.21D')])
