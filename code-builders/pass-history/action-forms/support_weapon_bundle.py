"""Write the manufactured, owner-disclosed Support Weapons contract checks."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[3]
folder = root / 'src/ASL/units/minigames/support-weapon-scope'
folder.mkdir(exist_ok=True)
card = json.loads((root / 'src/ASL/units/minigames/a7-encirclement/a7-encirclement.scenario-card.json').read_text(encoding='utf-8'))
card.update(id='support-weapon-scope', title='Support Weapons phase scope', minigame='support-weapon-scope',
            introduction='Manufactured fact positions for recovery, transfer, repair, configuration, and withdrawal equipment.')
card['source'] = dict(basis='Manufactured under R0.3 to prove the Support Weapons phase-scope calculators using supplied facts.',
                      legacy='None: a manufactured minigame (R0.3), adapting no legacy card',
                      adaptation=['The Rule Workbench checks phase opportunities, unknown information, use budgets, and retained equipment load.'])
card['boards'][0]['board'] = 'bd01'
checks = []


def check(name, contract, facts, outcome='allowed'):
    facts = dict(facts)
    if 'phase' in facts:
        facts['phase'] = {'rph': 'RPh', 'pfph': 'PFPh', 'mph': 'MPh', 'aph': 'APh'}[facts['phase']]
    elif contract == 'a11.21-retained-load':
        facts['phase'] = 'CCPh'
    checks.append(dict(name=name, position='setup', viewer='german', contract=contract,
                       facts=dict(unitId='G1', **facts), expect=dict(outcome=outcome)))


repair = dict(phase='rph', active=True, goodOrder=True, possessed=True, weaponActive=True,
              malfunctioned=True, captured=False, repairNumber=3, otherRallyAction=False,
              attemptedThisPlayerTurn=False, attemptsThisPhase=1, capacity=2, gun=False)
check('Second repair uses the remaining squad allowance', 'a9.72-repair', repair)
check('Third repair refused', 'a9.72-repair', dict(repair, attemptsThisPhase=2), 'refused')
check('Captured status unknown', 'a9.72-repair', dict(repair, captured=None), 'not-evaluated')
configuration = dict(phase='pfph', phasing=True, active=True, goodOrder=True, temporarilyImmobilized=False,
                     possessed=True, weaponActive=True, supportedConfiguration=True, weaponUsed=False,
                     usesThisPhase=1, capacity=2, typePermitted=True, inherentFireUsed=False, aboard=False)
check('Second configuration uses the remaining squad allowance', 'a9.8-configuration', configuration)
check('Inherent fire exhausts the remaining allowance', 'a9.8-configuration', dict(configuration, inherentFireUsed=True), 'refused')
check('Vehicle configuration is unsupported', 'a9.8-configuration', dict(configuration, aboard=True), 'not-evaluated')
load = dict(smc=False, wounded=False, cx=False, retainedPp=1)
check('Leave HMG and retain LMG', 'a11.21-retained-load', load)
check('Both weapons exceed withdrawal capacity', 'a11.21-retained-load', dict(load, retainedPp=6), 'refused')
check('Retained load unknown', 'a11.21-retained-load', dict(load, retainedPp=None), 'not-evaluated')
recovery = dict(phase='mph', phasing=True, active=True, goodOrder=True, pinned=False, infantry=True,
                unarmed=False, bypassing=False, weaponActive=True, sameLocation=True, held=False,
                friendlyHolder=False, brokenHolder=False, recoveringSmc=False, armedKnownEnemy=False,
                attempted=False, rallyActionTaken=False, movementEnded=False, temporarilyImmobilized=False,
                halfMfRemaining=2, rallyRecoveryWindowOpen=True, reactionPending=False, anotherMoverActive=False,
                terrain='open', aboard=False, conditionsKnown=True, knownWirePresent=False)
check('Recover during movement with one MF', 'a4.44-recovery', recovery)
check('No recovery while another mover is active', 'a4.44-recovery', dict(recovery, anotherMoverActive=True), 'refused')
transfer = dict(phase='aph', giver='G1', recipient='G2', weapon='LMG', differentUnits=True,
                giverGoodOrder=True, recipientGoodOrder=True, giverPinned=False, recipientPinned=False,
                sameSide=True, sameLocation=True, bothInfantry=True, giverVehicle='none', recipientVehicle='none',
                recipientUnarmed=False, possessed=True, recoveredThisPhase=False, giverRallyAction=False,
                recipientRallyAction=False, phasing=True, advanceStarted=False, vehicleInMotion=False, knownWirePresent=False)
check('Transfer before advance', 'a4.431-transfer', transfer)
check('No transfer after advance begins', 'a4.431-transfer', dict(transfer, advanceStarted=True), 'refused')
manifest = dict(id='support-weapon-scope', title='Support Weapons phase scope', card='support-weapon-scope',
                rules=['A4.43', 'A4.431', 'A4.44', 'A9.72', 'A9.8', 'A11.21'],
                contracts=list(dict.fromkeys(c['contract'] for c in checks)),
                units={'G1': 'german', 'G2': 'german'},
                example=dict(source='Manufactured fact positions under R0.3; quantities and states are supplied explicitly.', page=50),
                checks=checks)
for suffix, data in [('scenario-card', card), ('minigame', manifest)]:
    (folder / f'support-weapon-scope.{suffix}.json').write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8', newline='\n')
