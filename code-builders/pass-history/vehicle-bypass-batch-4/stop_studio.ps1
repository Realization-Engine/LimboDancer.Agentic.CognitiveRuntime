$ErrorActionPreference = 'Stop'
$taskWork = 'E:/Archive/GitHub/dlandi/asl-gate-work-codex'
$pidFile = "$taskWork/vehicle-bypass-batch4-studio.pid"
if (Test-Path -LiteralPath $pidFile) {
    $studioPid = [int](Get-Content -LiteralPath $pidFile)
    $owned = Get-CimInstance Win32_Process -Filter "ProcessId=$studioPid"
    if ($owned) {
        if ($owned.CommandLine -notlike '*asl-gate-work-codex/studio-6680/LimboDancer.Domains.Asl.MapStudio.dll*' -or $owned.CommandLine -notlike '*localhost:6680*') {
            throw 'Saved PID no longer identifies this task Studio.'
        }
        Stop-Process -Id $studioPid
    }
}
