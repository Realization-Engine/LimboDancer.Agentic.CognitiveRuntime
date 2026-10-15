param(
    [uri]$BaseUrl = 'http://127.0.0.1:8098',
    [Parameter(Mandatory)][ValidatePattern('^[a-f0-9]{64}$')][string]$SaveId
)
$ErrorActionPreference = 'Stop'
if (!$BaseUrl.IsLoopback -or $BaseUrl.Scheme -ne 'http') { throw 'Use an explicitly chosen local acceptance host.' }
$api = $BaseUrl.AbsoluteUri.TrimEnd('/') + '/api/asl-situation'
$before = Invoke-RestMethod "$api/saves/$SaveId"
if ($before.Setup.Positions.Count -ne 624) { throw 'Expected the complete Goodwood deployment.' }
$accepted = $before | ConvertTo-Json -Depth 100 -Compress
function Measure-Job([string]$Route) {
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $started = Invoke-RestMethod "$api/$Route" -Method Post -ContentType 'application/json' -Body '{}'
    do {
        $status = Invoke-RestMethod "$api/jobs/$($started.job)"
        if ($status.status -eq 'failed' -or $status.status -eq 'cancelled') { throw ($status | ConvertTo-Json -Compress) }
        if ($watch.Elapsed.TotalMinutes -gt 10) { throw "Job $($started.job) is still running; inspect it before retrying." }
        if ($status.status -eq 'working') { Start-Sleep -Milliseconds 250 }
    } while ($status.status -eq 'working')
    [pscustomobject]@{ seconds = $watch.Elapsed.TotalSeconds; result = $status.result; job = $started.job }
}
$prepared = Measure-Job "saves/$SaveId/prepare"
$preparePid = (Get-NetTCPConnection -State Listen -LocalPort $BaseUrl.Port).OwningProcess | Select-Object -Unique
$preparePeak = (Get-Process -Id $preparePid).PeakWorkingSet64
$handoff = Invoke-RestMethod "$api/handoffs/$($prepared.result)"
if (($handoff.Setup | ConvertTo-Json -Depth 100 -Compress) -ne ($before.Setup | ConvertTo-Json -Depth 100 -Compress)) { throw 'Prepared positions changed.' }
$launched = Measure-Job "handoffs/$($prepared.result)/launch"
$after = Invoke-RestMethod "$api/saves/$SaveId"
if (($after | ConvertTo-Json -Depth 100 -Compress) -ne $accepted) { throw 'The accepted campaign save changed.' }
[pscustomobject]@{
    date = [datetime]::UtcNow.ToString('o')
    baseUrl = $BaseUrl.AbsoluteUri
    save = $SaveId
    prepare = $prepared
    launch = $launched
    counters = $handoff.Setup.Positions.Count
    boardVersion = $handoff.BoardVersion
    sourceHash = $handoff.SourceHash
    campaignPeakWorkingSetAfterPrepareBytes = $preparePeak
    acceptedSaveUnchanged = $true
    notes = 'Existing accepted terrain; prepare includes review, assembly, compile, validation, planner, replay and publication. Launch imports into the configured Studio. Durations include HTTP and polling up to 250 ms. Memory is process lifetime peak, not isolated stage allocation. Browser display is excluded.'
} | ConvertTo-Json -Depth 8
