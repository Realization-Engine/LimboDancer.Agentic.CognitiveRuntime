param([int]$Port = 8096, [int]$Runs = 3)
$ErrorActionPreference = 'Stop'
# Refuse an occupied port, so a benchmark can never accidentally measure another host.
$probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, $Port)
try { $probe.Start() } finally { $probe.Stop() }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$dll = Join-Path $repo 'src/Campaign/LimboDancer.Campaign.PilotHost/bin/Release/net10.0/LimboDancer.Campaign.PilotHost.dll'
if (!(Test-Path $dll)) { throw 'Build Campaign.slnx in Release before benchmarking.' }
$root = Join-Path $repo ('artifacts/pilot-review-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $root | Out-Null
$base = "http://127.0.0.1:$Port/api/asl-pilot"
$source = Get-Content (Join-Path $repo 'prototypes/europe-hex-map/sources/situations/panzer-leader-05.json') -Raw | ConvertFrom-Json
$context = @{campaignId='benchmark';formationSignature='source-fixture';alliedParents=@($source.deployment.allowedHexes.'uk-rifle-01' | ForEach-Object { $_.boardId+'/'+$_.hexId });germanParents=@($source.deployment.allowedHexes.'de-rifle-01' | ForEach-Object { $_.boardId+'/'+$_.hexId })}
function Post($route, $body) { Invoke-RestMethod ($base+$route) -Method Post -ContentType 'application/json' -Body ($body | ConvertTo-Json -Depth 100 -Compress) }
function Launch($packages, $log) {
    $p = Start-Process dotnet -ArgumentList @(('"'+$dll+'"'),'--urls',"http://127.0.0.1:$Port",('--Campaign:PackagesRoot="'+$packages+'"'),'--Logging:LogLevel:Default','Warning') -PassThru -WindowStyle Hidden -RedirectStandardOutput "$log.out" -RedirectStandardError "$log.err"
    $deadline = [datetime]::UtcNow.AddSeconds(30)
    do { try { $null=Invoke-RestMethod $base; return $p } catch { if($p.HasExited -or [datetime]::UtcNow -gt $deadline){if(!$p.HasExited){Stop-Process -Id $p.Id};throw 'Benchmark host did not start.'}; Start-Sleep -Milliseconds 50 } } while($true)
}
$rows = @()
for($i=0;$i -lt $Runs;$i++) {
    $seed = $i.ToString('x32'); $packages=Join-Path $root "run-$i"; $p=$null
    try {
        $start=[Diagnostics.Stopwatch]::StartNew();$p=Launch $packages (Join-Path $root "generate-$i");$startup=$start.Elapsed.TotalSeconds
        $watch=[Diagnostics.Stopwatch]::StartNew();$null=Post '/generate' @{seed=$seed}
        do { $status=Invoke-RestMethod "$base/generate/$seed";if($status.status -eq 'failed'){throw $status.error};if($watch.Elapsed.TotalSeconds -gt 30){throw 'Generation timed out'};if($status.status -eq 'generating'){Start-Sleep -Milliseconds 20} } while($status.status -eq 'generating')
        $generated=$watch.Elapsed.TotalSeconds
        $opened=Post '/open' @{seed=$seed;context=$context}
        $ready=$watch.Elapsed.TotalSeconds
        $setup=Post '/edit' @{session=$opened.session;setup=$opened.setup;action='deploy'}
        $request=@{seed=$seed;context=$context;bundle=$opened.bundle;setup=$setup}
        $json=$request | ConvertTo-Json -Depth 100 -Compress
        [IO.File]::WriteAllText((Join-Path $root "accepted-$i.json"),$json)
        $p.Refresh();$genPeak=$p.PeakWorkingSet64
        Stop-Process -Id $p.Id; $p.WaitForExit(); $p=$null
        $p=Launch $packages (Join-Path $root "reload-$i")
        $watch.Restart();$loaded=Post '/open' $request;$reload=$watch.Elapsed.TotalSeconds
        if($loaded.bundle.packageVersion -ne $opened.bundle.packageVersion -or $loaded.svg -ne $opened.svg -or $loaded.setup.placements.Count -ne 8){throw 'Reload content differs.'}
        $p.Refresh();$reloadPeak=$p.PeakWorkingSet64
        $rows += [pscustomobject]@{seed=$seed;startupSeconds=$startup;generationSeconds=$generated;generationAndOpenSeconds=$ready;reloadSeconds=$reload;generationPeakWorkingSetBytes=$genPeak;reloadPeakWorkingSetBytes=$reloadPeak;packageBytes=(Get-ChildItem $packages -Recurse -File | Measure-Object Length -Sum).Sum;savedRequestBytes=[Text.Encoding]::UTF8.GetByteCount($json);svgBytes=[Text.Encoding]::UTF8.GetByteCount($opened.svg)}
    } finally { if($p -and !$p.HasExited){Stop-Process -Id $p.Id; $p.WaitForExit()} }
}
$report=@{date=[datetime]::UtcNow.ToString('o');runtime=(dotnet --version);cpu=(Get-CimInstance Win32_Processor | Select-Object Name,NumberOfLogicalProcessors);os=(Get-CimInstance Win32_OperatingSystem | Select-Object Caption,TotalVisibleMemorySize);notes='Release; fresh process per generation and reload; unique package roots; OS disk cache not flushed; HTTP serialization included; browser paint excluded; process peak working set, not managed heap.';runs=$rows}
$report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $root 'results.json')
$report | ConvertTo-Json -Depth 10
Write-Output "Evidence: $root"
