$ErrorActionPreference = 'Stop'
Set-Location 'E:\Archive\GitHub\dlandi\LimboDancer.MCP'
$utf8 = New-Object Text.UTF8Encoding $false

function Cut([string]$path, [string]$startMarker, [string]$endMarker) {
    $text = ((git show "HEAD:$path") -join "`n") + "`n"
    $start = $text.IndexOf($startMarker)
    $end = $text.IndexOf($endMarker)
    if ($start -lt 0 -or $end -le $start -or $text.IndexOf($startMarker, $start + 1) -ge 0 -or $text.IndexOf($endMarker, $end + 1) -ge 0) { throw "markers not unique in $path" }
    $text = $text.Substring(0, $start) + $text.Substring($end)
    [IO.File]::WriteAllText((Join-Path (Get-Location) $path), $text, $utf8)
}

Cut 'src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1VehicleMovementCalculator.cs' `
    '    /// <summary>Whether a Location is Concealment Terrain for a vehicle' `
    "    /// <summary>`n    /// The steps a moving vehicle may try now"
Cut 'src/ASL/LimboDancer.Domains.Asl.Rules/ScenarioA1VehicleTerrainCosts.cs' `
    "    /// <summary>`n    /// Whether an AFV or wreck gives cover and Hindrance now" `
    '    /// <summary>The extra MP a vehicle pays to enter a hex'
'done'
