"""Apply the five approved PR 121 review corrections, once, to the reviewed tree."""
from pathlib import Path

root = Path(__file__).resolve().parents[3]
rules = root / 'src/ASL/LimboDancer.Domains.Asl.Rules'
tests = root / 'src/ASL/tests/LimboDancer.Domains.Asl.Rules.Tests'

def save(path, text):
    path.write_text(text, encoding='utf-8', newline='\n')

def replace(path, old, new):
    text = path.read_text(encoding='utf-8')
    assert old in text, (path, old)
    save(path, text.replace(old, new))

path = rules / 'ScenarioA1ItalianCaptureContract.cs'
text = path.read_text(encoding='utf-8').split('    private static readonly string[] Nationalities')[0].rstrip() + '\n}\n'
text = text.replace('ContractDisclosure.Owner', 'ContractDisclosure.All')
text = text.replace('The subject, selected by its owning side', 'The defender selected for CC attack declaration; supply only facts public at that step')
text = text.replace('new("A20.22", 87)', 'new("A20.22", 87), new("A11.19", 73)')
save(path, text)

path = rules / 'ScenarioA1PrisonerContract.cs'
replace(path, '"italian", "japanese"', '"italian", "eritrean", "japanese"')
replace(path, 'new("A25.63", 97),', 'new("A25.63", 97), new("A25.66", 97),')
replace(path, 'facts.Flag("meleeInLocation")!.Value));', 'facts.Flag("meleeInLocation")!.Value), ItalianEscapeContext.WithEritreanInheritance);')
replace(path, '"A25.63" => 97,', '"A25.63" or "A25.66" => 97,')

replace(rules / 'ScenarioA1FinnishOrganizationModels.cs', ', bool? CarrierHalfSquad);', ');')
path = rules / 'ScenarioA1FinnishOrganizationContract.cs'
replace(path, '            new("carrierHalfSquad", ContractFactType.Flag, true, ContractDisclosure.Owner, "carrierHalfSquad") { Concerns = "subjectId" },\n', '')
replace(path, ', facts.Flag("carrierHalfSquad")', '')
path = tests / 'ScenarioA1FinnishOrganizationTests.cs'
replace(path, 'false, false, false, null)', 'false, false, false)')
replace(path, 'true, null, false, null)', 'true, null, false)')
replace(rules / 'ScenarioA1ItalianDeploymentCalculator.cs', '            true => Unrestricted("Elite Italian-treatment squads pass the nationality restriction (A25.61, A25.66)."),\n', '')

# Carry the deciding rule with the verdict instead of deciding PSK class again in the adapter.
replace(rules / 'ScenarioA1CapturedEquipmentUseModels.cs', 'string? UseRefusal, string Reason);', 'string? UseRefusal, string Reason, string Rule = "A25.75");')
path = rules / 'ScenarioA1CapturedEquipmentUseCalculator.cs'
replace(path, 'facts.PhysicallyCaptured, refusal, refusal);', 'facts.PhysicallyCaptured, refusal, refusal, "A25.77");')
replace(path, 'use every PSK with Captured Use penalties (A25.77).");', 'use every PSK with Captured Use penalties (A25.77).", "A25.77");')
replace(path, 'bool penalties, string reason) =>', 'bool penalties, string reason, string rule = "A25.75") =>')
replace(path, 'facts.PhysicallyCaptured, null, reason);', 'facts.PhysicallyCaptured, null, reason, rule);')
replace(rules / 'ScenarioA1CapturedEquipmentUseContract.cs', 'verdict.UseRefusal is null ? "A25.75" : "A25.77"', 'verdict.Rule')

# Keep the original generation recipes aligned with the consolidated contracts.
path = root / 'code-builders/pass-history/nationality-batch-1/write_contracts.py'
text = path.read_text(encoding='utf-8').split('# Preserve the original public prisoner contract')[0]
text = text.replace(", field('carrierHalfSquad')", '').replace(', facts.Flag("carrierHalfSquad")', '')
text = text.replace("[subject, nationality, field('elite'), field('inexperienced')]", "[field('subjectId', 'Unit', public=True, unknown=False, description='The defender selected for CC attack declaration; supply only facts public at that step'), field('nationality', 'Text', public=True), field('elite', public=True), field('inexperienced', public=True)]")
text = text.replace("('A20.22', 87)]", "('A20.22', 87), ('A11.19', 73)]")
text = text.replace('verdict.UseRefusal is null ? "A25.75" : "A25.77"', 'verdict.Rule')
save(path, text + '# Escape is maintained once, in ScenarioA1PrisonerContract.cs.\n')

path = root / 'code-builders/pass-history/nationality-batch-1/write_bundles.py'
replace(path, ', carrierHalfSquad=False', '')
replace(path, "check('Opponent cannot read hidden defender class', contract, hidden(base), 'not-evaluated', 'german', {'subjectId':'italian'})", "check('Attacker uses the publicly revealed defender facts at CC declaration', contract, base, 'zero', 'german'),\n          check('Unavailable defender class remains unknown', contract, dict(base, elite=None, inexperienced=None), 'not-evaluated', 'german')")
replace(path, "for national, old in [('italian', 'refused-nationality'), ('eritrean', 'not-evaluated')]:\n    checks.append(check(national + ' in unchanged legacy contract', 'a20.55-prisoner-escape', dict(escape, nationality=national), old, 'german'))\n    checks.append(check(national + ' in current inheritance contract', 'a25.63-prisoner-escape', dict(escape, nationality=national), 'refused-nationality', 'german'))", "for national in ['italian', 'eritrean']:\n    checks.append(check(national + ' in the single escape contract', 'a20.55-prisoner-escape', dict(escape, nationality=national), 'refused-nationality', 'german'))")
replace(path, "['A25.63', 'A25.66'], checks, 'italian')", "['A25.63', 'A25.66', 'A11.19'], checks, 'italian')")
print('Applied five review corrections and updated generation recipes.')
