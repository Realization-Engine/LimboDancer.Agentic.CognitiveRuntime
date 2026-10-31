"""One-time migration of the Play/Replay composition to shared outcome cards."""
from pathlib import Path

root = Path(__file__).resolve().parents[3]
studio = root / 'src/ASL/LimboDancer.Domains.Asl.MapStudio'

def replace_between(path, start, end, replacement):
    text = path.read_text(encoding='utf-8')
    left = text.index(start)
    right = text.index(end, left)
    path.write_text(text[:left] + replacement + text[right:], encoding='utf-8', newline='\n')

play = studio / 'Components/Pages/Play.razor'
replace_between(play, '                <DiceRollHistory Rolls="@RollRows"', '            </text>;', '''                @if (Records is { } actionRecords)
                {
                    <ActionOutcomeHistory @key="(gameId, viewAs)" Outcomes="@actionRecords.Outcomes" Replay="@ReplayFrom" ViewKey="@(gameId + ":" + viewAs)" />
                }
                <details><summary>Recent raw rolls</summary><DiceRollHistory Rolls="@RollRows" Scripted="@(Scripted is not null)" /></details>
''')
replace_between(play, '                @if (Records is { } latestRecords', '            </div>\n            @* Pass 31c (designs D15', '''                @if (Records?.LatestOutcome is { } latestOutcome)
                {
                    <ActionOutcomeCard @key="(gameId, viewAs, latestOutcome.Key)" Outcome="@latestOutcome" Expanded="@(!latestCollapsed)" Replay="@ReplayFrom" />
                }
                else
                {
                    <p id="play-latest">No record yet.</p>
                }
''')
replace_between(play, '                    <ol id="play-since">', '                </div>\n            }\n            @* With the map', '''                    @if (Records is { } sinceRecords && sinceLooked is { } afterRevision)
                    {
                        <ActionOutcomeHistory @key="(gameId, viewAs, afterRevision)" Outcomes="@sinceRecords.OutcomesIn(afterRevision + 1, sinceThrough)" Replay="@ReplayFrom" ViewKey="@(gameId + ":" + viewAs)" />
                    }
''')
replay = studio / 'Components/Replay/ReplayStepPanel.razor'
replace_between(replay, '    <ActionRecordList Heading="Ordnance"', '    @if (Rolls.Count', '''    @foreach (var outcome in Outcomes)
    {
        <ActionOutcomeCard @key="outcome.Key" Outcome="@outcome" Expanded="true" />
    }
''')
text = replay.read_text(encoding='utf-8').replace('@code {', '''@code {
    [Parameter]
    public IReadOnlyList<ActionOutcomeView> Outcomes { get; set; } = [];
''')
replay.write_text(text, encoding='utf-8', newline='\n')
page = studio / 'Components/Pages/Replay.razor'
text = page.read_text(encoding='utf-8').replace('<ReplayStepPanel Step="@step"', '<ReplayStepPanel Outcomes="@(records?.OutcomesIn(step.First, step.Last) ?? [])" Step="@step"')
page.write_text(text, encoding='utf-8', newline='\n')
