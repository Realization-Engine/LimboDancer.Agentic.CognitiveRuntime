$ErrorActionPreference = 'Stop'
$env:ASL_GATE_WORK = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$taskRoot = (Resolve-Path -LiteralPath $env:ASL_GATE_WORK).ProviderPath
$removed = @()
foreach ($name in @('studio-6680', 'test-classes-bin')) {
    $target = [IO.Path]::GetFullPath((Join-Path $taskRoot $name))
    if (-not $target.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Outside Codex output root.' }
    if (Test-Path -LiteralPath $target) {
        $resolved = (Resolve-Path -LiteralPath $target).ProviderPath
        if ($resolved -ne $target) { throw 'Unexpected resolved output path.' }
        $running = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($target.Replace('\','/') + '/') }
        if ($running) { throw "Output still in use: $target" }
        Write-Output "Removing verified owned output: $target"
        Remove-Item -LiteralPath $target -Recurse -Force
    }
    $removed += $target.Replace('\','/')
}
$register = Join-Path $taskRoot 'outside-outputs.txt'
$kept = @(Get-Content -LiteralPath $register | Where-Object { $removed -notcontains $_.Replace('\','/') })
[IO.File]::WriteAllText($register, ($kept -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
Write-Output 'Build binaries removed; logs, results and isolated board evidence retained.'
