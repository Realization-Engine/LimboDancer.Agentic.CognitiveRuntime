$ErrorActionPreference = 'Stop'
if ($env:ASL_GATE_WORK -ne 'E:/Archive/GitHub/dlandi/asl-gate-work-codex') { throw 'Expected Codex output root.' }
$taskRoot = [IO.Path]::GetFullPath("$env:ASL_GATE_WORK/action-form-design")
$removed = @()
foreach ($name in @('studio-6680', 'studio-next', 'studio-final', 'studio-movement', 'studio-map-movement', 'studio-fire', 'studio-rout', 'studio-advance', 'studio-advance-final', 'studio-cc', 'studio-cc-final', 'studio-rally-scope', 'studio-rally-workflows', 'checks')) {
    $target = [IO.Path]::GetFullPath((Join-Path $taskRoot $name))
    if (-not $target.StartsWith($taskRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { throw 'Outside task output root.' }
    $running = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | Where-Object { $_.CommandLine -and $_.CommandLine.Replace('\','/').Contains($target.Replace('\','/') + '/') }
    if ($running) { throw "Output still in use: $target" }
    if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Recurse -Force }
    $removed += $target.Replace('\','/')
}
$register = "$env:ASL_GATE_WORK/outside-outputs.txt"
if (Test-Path -LiteralPath $register) {
    $kept = @(Get-Content -LiteralPath $register | Where-Object { $removed -notcontains $_.Replace('\','/') })
    [IO.File]::WriteAllText($register, ($kept -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
}
Write-Output 'Removed only superseded task builds and test binaries. Active demo, logs, and results retained.'
