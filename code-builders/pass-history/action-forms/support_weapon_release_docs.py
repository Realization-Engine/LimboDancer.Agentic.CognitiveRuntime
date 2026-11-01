"""Record the combined delivery without claiming acceptance of deferred rule coverage."""
from pathlib import Path

root = Path(__file__).resolve().parents[3]
path = root / 'src/ASL/docs/ASL Rule Inventory A to E.md'
text = path.read_text(encoding='utf-8')
lines = text.splitlines()
for index, line in enumerate(lines):
    if 'codex/support-weapons-implementation' in line and line.startswith('|'):
        line = line.replace('| claimed |', '| built |')
        line = line.replace('full Studio demo required and merge remains unapproved.',
                            'all eight phase demos approved; user authorized merge after green CI on 2026-10-31.')
        if 'Final read-only' not in line:
            line = line.rstrip(' |') + ' Final read-only Rules review completed by sw_rules_review; contracts and focused boundary checks added. Combined wiring is delivered under the explicit exception; remaining coverage stays in backlog 93. |'
        lines[index] = line
    elif any(f'| {claim} (Codex) | claimed |' in line for claim in ['A4.43', 'A9.72', 'A9.8', 'A11.21']):
        lines[index] = line.replace('| claimed |', '| built |')
text = '\n'.join(lines) + '\n'
rows = [
    '| W-A4.43 | A4.43 | movement | Delivered under the combined exception: GamePlanner.SupportWeapons and shared RecoveryRead/TransferRead ask SupportWeaponPossessionRules; versioned GameProjector uses the same readers. UI delegates choices. | Legacy DropPhase/RecoverPhase/TransferPhase remain only for historical readings and existing independent paths. | Parent actor scope, recovery opportunities and paid-MF defensive-fire window, complete transfer relationships; recorded readings preserved. Formal track acceptance remains separate. |  | open |',
    '| W-A9.72 | A9.72 | rally | Delivered under the combined exception: GamePlanner.Rally, SupportWeaponRepairRead and versioned GameProjector ask SupportWeaponRepairRules. | Legacy repair verification retained for null-version events. | Per-weapon and per-holder Player Turn repair budgets; explicit unknown/captured/unsupported results; scoped malfunctioned choices. Formal track acceptance remains separate. |  | open |',
    '| W-A9.8 | A9.8 | fire | Delivered under the combined exception: shared ConfigurationRead, GamePlanner.SupportWeapons, fire/ordnance planning and current projection ask ConfigurationRules/UseRules. | Legacy dismantle answers remain for records without ConfiguredBy. | Configuration shares weapon/inherent-fire use capacity, including MPh/DFPh and night history; aboard configuration unsupported. Formal track acceptance remains separate. |  | open |',
    '| W-A11.21 | A11.21 | close combat | Delivered under the combined exception: GamePlanner.Withdrawal and the Melee, Ambush and infiltration components consume WithdrawalEquipmentRules readiness. | Existing withdrawal resolution and historical projection remain. | Drop choices belong to the withdrawal draft; retained load controls destinations and invalidates stale proposals. Formal track acceptance remains separate. |  | open |',
]
anchor = '| Wiring | Rule claim | Capability | Sites to rewire | Old members to remove | Behavior changes | Owner | State |\n|---|---|---|---|---|---|---|---|\n'
if '| W-A4.43 |' not in text:
    text = text.replace(anchor, anchor + '\n'.join(rows) + '\n')
path.write_text(text, encoding='utf-8', newline='\n')
