"""Write the approved Batch 1 contract adapters. Decisions remain in calculators."""
from pathlib import Path

root = Path(__file__).resolve().parents[3] / 'src/ASL/LimboDancer.Domains.Asl.Rules'

def field(name, kind='Flag', choices=None, public=False, unknown=True, description=None):
    suffix = [] if public else ['Concerns = "subjectId"']
    if choices:
        suffix.append('Choices = [' + ', '.join('"' + value + '"' for value in choices) + ']')
    result = f'new("{name}", ContractFactType.{kind}, {str(unknown).lower()}, ContractDisclosure.{"All" if public else "Owner"}, "{description or name}")'
    return result + (' { ' + ', '.join(suffix) + ' }' if suffix else '')

subject = field('subjectId', 'Unit', unknown=False, description='The subject, selected by its owning side')
nationality = field('nationality', 'Text', description='Actual nationality, including eritrean; never a derived Italian flag')
kind = field('kind', 'Choice', ['squad', 'half-squad', 'crew', 'leader', 'other'])

def write(family, identifier, title, citations, fields, outcomes, events, phase, step, body, rolls='[]'):
    source = f'''namespace LimboDancer.Domains.Asl.Rules;

/// <summary>{title}: a facts-only adapter, RN1, with no caller or serialized-game changes.</summary>
internal static class ScenarioA1{family}Contract
{{
    [RuleContract]
    private static RuleContract Contract() => new("{identifier}", "{title}",
        [{', '.join(f'new("{r}", {p})' for r, p in citations)}],
        [
            {(',' + chr(10) + '            ').join(fields)},
        ],
        [{', '.join('"' + s + '"' for s in outcomes + ['not-evaluated'])}],
        [new("{events[0]}", "{events[1]}")], new("{phase}", "{step}"))
    {{
        Decide = Evaluate,
    }};

    private static ContractVerdict Evaluate(ContractFacts facts)
    {{
{body}
    }}
}}
'''
    (root / f'ScenarioA1{family}Contract.cs').write_text(source, encoding='utf-8', newline='\n')

write('ItalianDeployment', 'a25.61-italian-deployment', 'Italian deployment nationality restriction',
      [('A25.61', 97), ('A25.66', 97), ('A20.5', 88), ('A21.22', 88)],
      [subject, nationality, kind, field('elite'), field('context', 'Choice', ['setup', 'ordinary', 'guard', 'unarmed', 'temporary-crew'], True, False),
       field('guard'), field('unarmed'), field('broken')], ['unrestricted', 'restricted', 'not-applicable'],
      ('unit-deployed', 'If the remaining rules allow: deployment roll and resulting unit lineage; no new event shape'), 'Any', 'none',
      '''        var verdict = ScenarioA1ItalianDeploymentCalculator.Evaluate(new ItalianDeploymentFacts(facts.Text("nationality"), facts.Text("kind"),
            facts.Flag("elite"), facts.Text("context")!, facts.Flag("guard"), facts.Flag("unarmed"), facts.Flag("broken")));
        return new(verdict.Outcome, [new(verdict.Reason, "A25.61", 97)], []);''')

write('FinnishOrganization', 'a25.71-organization', 'Finnish organization requirements', [('A25.71', 97), ('A1.31', 45), ('A1.32', 45), ('A20.5', 88)],
      [subject, nationality, kind, field('action', 'Choice', ['deploy', 'recombine'], True, False),
       field('mode', 'Choice', ['directed', 'leaderless', 'not-applicable'], True, False), field('guard'), field('unarmed'), field('broken'), field('carrierHalfSquad')],
      ['directed-ntc', 'leaderless-ntc', 'leaderless-1tc', 'automatic', 'no-leader-required', 'not-applicable'],
      ('unit-organization', 'If other rules allow: deployment roll and deployment or recombination lineage'), 'RPh', '1.13B / 1.21A',
      '''        var verdict = ScenarioA1FinnishOrganizationCalculator.Evaluate(new FinnishOrganizationFacts(facts.Text("nationality"), facts.Text("kind"),
            facts.Text("action")!, facts.Text("mode")!, facts.Flag("guard"), facts.Flag("unarmed"), facts.Flag("broken"), facts.Flag("carrierHalfSquad")));
        IReadOnlyList<ContractRoll> rolls = verdict.BaseTcDrm is { } drm
            ? [new("tc", null, drm == 1 ? "1TC: add +1 to the deployment DR" : "NTC: add the directing leader's DRM only if directed")]
            : [];
        return new(verdict.Outcome, [new(verdict.Reason, "A25.71", 97)], rolls);''')

write('ItalianCapture', 'a25.63-capture-modifier', 'Per-defender capture term', [('A25.63', 97), ('A25.66', 97), ('A19.35', 86), ('A20.22', 87)],
      [subject, nationality, field('elite'), field('inexperienced')], ['minus-one', 'zero', 'plus-one'],
      ('close-combat-resolved', 'This defender ordinary capture term, independently combined with Passenger, leadership and other modifiers'), 'CCPh', '8.21B',
      '''        var verdict = ScenarioA1ItalianCaptureCalculator.CaptureModifier(new ItalianCaptureFacts(facts.Text("nationality"), facts.Flag("elite"), facts.Flag("inexperienced")));
        var outcome = verdict.CaptureDrm switch { -1 => "minus-one", 0 => "zero", 1 => "plus-one", _ => ContractVerdict.NotEvaluated };
        var rule = verdict.CaptureDrm switch { -1 => "A19.35", 1 => "A20.22", _ => "A25.63" };
        var page = verdict.CaptureDrm switch { -1 => 86, 1 => 87, _ => 97 };
        return new(outcome, [new(verdict.Reason, rule, page)], []);''')

write('CapturedEquipmentUse', 'a25.75-equipment-use', 'Finnish captured equipment use', [('A25.75', 97), ('A25.77', 97), ('E1.76', 225)],
      [subject, field('userNationality', 'Text'), field('personnel'), field('printedClass', 'Choice', ['elite', 'first-line', 'second-line', 'green', 'conscript', 'other']),
       field('weaponNationality', 'Text'), field('weaponKind', 'Choice', ['lmg', 'mmg', 'hmg', 'hmg50', 'psk', 'other']),
       field('originMark', 'Choice', ['r', 'none', 'not-applicable']),
       field('scenarioDate', 'Text', public=True, unknown=False, description='YYYY-MM, or not-applicable only for a date-independent branch'), field('physicallyCaptured')],
      ['captured-use', 'exempt', 'refused-use', 'not-applicable'],
      ('weapon-use', 'Captured Use and night MG treatment; no change to ownership, repair, non-qualified use or ammunition'), 'Any', 'none',
      '''        var verdict = ScenarioA1CapturedEquipmentUseCalculator.Evaluate(new CapturedEquipmentUseFacts(facts.Text("userNationality"), facts.Flag("personnel"),
            facts.Text("printedClass"), facts.Text("weaponNationality"), facts.Text("weaponKind"), facts.Text("originMark"), facts.Text("scenarioDate")!, facts.Flag("physicallyCaptured")));
        return new(verdict.Outcome, [new(verdict.Reason, verdict.UseRefusal is null ? "A25.75" : "A25.77", 97)], []);''')

# Preserve the original public prisoner contract, including its choices and disclosure.
path = root / 'ScenarioA1ItalianCaptureContract.cs'
text = path.read_text(encoding='utf-8')
existing = (root / 'ScenarioA1PrisonerContract.cs').read_text(encoding='utf-8')
start = existing.index('    [RuleContract]')
end = existing.index('    [RuleContract]', start + 1)
factory = existing[start:end].replace('"a20.55-prisoner-escape"', '"a25.63-prisoner-escape"').replace('new("A25.63", 97)', 'new("A25.63", 97), new("A25.66", 97)')
start = existing.index('    private static ContractVerdict EscapeVerdict')
end = existing.index('    private static ContractVerdict KiaVerdict', start)
evaluate = existing[start:end].replace('facts.Flag("meleeInLocation")!.Value));', 'facts.Flag("meleeInLocation")!.Value), ItalianEscapeContext.WithEritreanInheritance);')
start = existing.index('    private static readonly string[] Nationalities')
end = existing.index('    private static readonly string[] CvpStatuses', start)
choices = existing[start:end].replace('"italian",', '"italian", "eritrean",')
page = '''    private static int Page(string rule) => rule switch
    {
        "A25.63" or "A25.66" => 97,
        "A25.82" => 98,
        "G1.621" => 280,
        _ => 88,
    };
'''
path.write_text(text.rstrip()[:-1] + '\n' + choices + factory + evaluate + page + '}\n', encoding='utf-8', newline='\n')
