param(
    [string]$RequestFile,
    [int]$Port = 8097,
    [int]$Runs = 3,
    [switch]$DisableTiering
)
$ErrorActionPreference = 'Stop'
if (!$RequestFile) { throw 'Supply an accepted open-request JSON file, for example accepted-0.json from Measure-Pilot.ps1.' }
$probe = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,$Port)
try { $probe.Start() } finally { $probe.Stop() }
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$dll = Join-Path $repo 'src/Campaign/LimboDancer.Campaign.PilotHost/bin/Release/net10.0/LimboDancer.Campaign.PilotHost.dll'
$body = [IO.File]::ReadAllText((Resolve-Path $RequestFile).Path)
$root = Join-Path $repo ('artifacts/pilot-open-profile-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory $root | Out-Null
$base = "http://127.0.0.1:$Port/api/asl-pilot"
$rows = @()
for($run=0;$run -lt $Runs;$run++) {
    $process=$null
    try {
        $options = @{
            FilePath='dotnet'
            ArgumentList=@(('"'+$dll+'"'),'--urls',"http://127.0.0.1:$Port",'--Logging:LogLevel:Default','Warning')
            PassThru=$true
            WindowStyle='Hidden'
            RedirectStandardOutput=(Join-Path $root "$run.out")
            RedirectStandardError=(Join-Path $root "$run.err")
        }
        if($DisableTiering) { $options.Environment = @{ DOTNET_TieredCompilation='0' } }
        $startup=[Diagnostics.Stopwatch]::StartNew()
        $process=Start-Process @options
        $deadline=[datetime]::UtcNow.AddSeconds(30)
        do {
            try { $null=Invoke-RestMethod $base; break }
            catch { if($process.HasExited -or [datetime]::UtcNow -gt $deadline){throw 'Host failed to start'}; Start-Sleep -Milliseconds 50 }
        } while($true)
        $startupMs=$startup.Elapsed.TotalMilliseconds
        for($open=0;$open -lt 3;$open++) {
            $watch=[Diagnostics.Stopwatch]::StartNew()
            $response=Invoke-WebRequest "$base/open" -Method Post -ContentType 'application/json' -Body $body
            $requestMs=$watch.Elapsed.TotalMilliseconds
            $loaded=$response.Content | ConvertFrom-Json
            # Ignore diagnostics/session ID; compare every returned map cell, SVG and setup field.
            $content=[ordered]@{svg=$loaded.svg;cells=$loaded.cells;setup=$loaded.setup;bundle=$loaded.bundle} | ConvertTo-Json -Depth 100 -Compress
            $hash=[Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($content)))
            if($rows.Count -gt 0 -and $hash -ne $rows[0].contentSha256) { throw 'Returned map content changed between opens.' }
            $process.Refresh()
            $rows += [pscustomobject]@{run=$run;open=$open;startupMs=$startupMs;requestMs=$requestMs;serverTiming=($response.Headers['Server-Timing'] -join ',');stagesMs=$loaded.timingsMs;contentSha256=$hash;peakWorkingSetBytes=$process.PeakWorkingSet64}
        }
    } finally { if($process -and !$process.HasExited){Stop-Process -Id $process.Id; $process.WaitForExit()} }
}
$report=@{date=[datetime]::UtcNow.ToString('o');disableTiering=[bool]$DisableTiering;requestFile=[IO.Path]::GetFileName($RequestFile);notes='Release, fresh host per run, three consecutive opens of identical accepted request; HTTP elapsed excludes PowerShell JSON parse; health probe before open; OS caches not flushed. Server-Timing ends when response headers start, not at completion of transfer.';runs=$rows}
$report | ConvertTo-Json -Depth 10 | Set-Content (Join-Path $root 'results.json')
$report | ConvertTo-Json -Depth 10
Write-Output "Evidence: $root"