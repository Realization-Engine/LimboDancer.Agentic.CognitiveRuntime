$ErrorActionPreference = 'Stop'
if (-not $env:ASL_GATE_WORK) { throw 'Set ASL_GATE_WORK to the Codex working folder first.' }
$taskFolder = [IO.Path]::GetFullPath((Join-Path $env:ASL_GATE_WORK 'action-outcome'))
$target = [IO.Path]::GetFullPath((Join-Path $taskFolder 'checks'))
if ([IO.Path]::GetDirectoryName($target) -ne $taskFolder -or [IO.Path]::GetFileName($target) -ne 'checks') {
    throw 'The test output is outside the expected task folder.'
}
if (Test-Path -LiteralPath $target) {
    $item = Get-Item -LiteralPath $target
    if ($item.LinkType) { throw 'Refusing to remove a linked output folder.' }
    Write-Output "Removing verified test output: $target"
    Remove-Item -LiteralPath $target -Recurse -Force
}
$registry = Join-Path $env:ASL_GATE_WORK 'outside-outputs.txt'
if (Test-Path -LiteralPath $registry) {
    $remaining = @(Get-Content -LiteralPath $registry | Where-Object { $_.Replace('/', '\') -ne $target })
    [IO.File]::WriteAllText($registry, ($remaining -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
}
Write-Output 'Test output removed. Studio, logs, results, and other registered outputs are retained.'
