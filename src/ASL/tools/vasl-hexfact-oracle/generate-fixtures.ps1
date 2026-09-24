#Requires -Version 7
<#
.SYNOPSIS
Regenerates the F2 oracle fixtures from a local VASL checkout.

.DESCRIPTION
Resolves VASL's dependency classpath with the checkout's Maven wrapper, compiles HexFactOracle.java against
VASL's own sources, runs it for the given boards, and writes gzipped fixtures to
src/ASL/tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle. Nothing is written into the VASL checkout.

VASL's pom compiles for Java 11, which cannot read VASSAL 3.7's Java 17 classes, so VASL is not built with Maven
here; javac compiles only the VASL classes the harness uses, with the installed JDK (17 or later).

.PARAMETER VaslRoot
The VASL checkout. Defaults to the AslMaps__VaslRoot environment variable.

.PARAMETER Boards
VASL board names such as 01 or BFPA, as separate arguments or a comma-separated list. Quote them within
PowerShell ('01','02'), where an unquoted 01 is read as the number 1. Defaults to the boards
that already have fixtures.
#>
[CmdletBinding(PositionalBinding = $false)]
param(
    [Parameter(Position = 0, ValueFromRemainingArguments)]
    [string[]] $Boards,
    [string] $VaslRoot = $env:AslMaps__VaslRoot
)

$ErrorActionPreference = 'Stop'
if (-not $VaslRoot -or -not (Test-Path (Join-Path $VaslRoot 'boards'))) {
    throw 'Set -VaslRoot or AslMaps__VaslRoot to a VASL checkout.'
}

$VaslRoot = (Resolve-Path $VaslRoot).Path
$Boards = @($Boards | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$fixtures = Join-Path $PSScriptRoot '../../tests/LimboDancer.Domains.Asl.Maps.Vasl.Tests/Oracle'
$fixtures = (Resolve-Path $fixtures).Path
if (-not $Boards) {
    $Boards = Get-ChildItem $fixtures -Filter 'bd*.hexfacts.json.gz' |
        ForEach-Object { $_.Name.Substring(2, $_.Name.IndexOf('.') - 2) }
}

$work = Join-Path ([IO.Path]::GetTempPath()) ('vasl-hexfact-oracle-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work, "$work/classes", "$work/out" | Out-Null
try {
    $classpathFile = Join-Path $work 'classpath.txt'
    $mvnw = if ($IsWindows) { Join-Path $VaslRoot 'mvnw.cmd' } else { Join-Path $VaslRoot 'mvnw' }
    Push-Location $VaslRoot
    try {
        & $mvnw -B -q dependency:build-classpath "-Dmdep.outputFile=$classpathFile"
        if ($LASTEXITCODE -ne 0) { throw 'Resolving the VASL classpath failed.' }
    }
    finally {
        Pop-Location
    }

    $dependencies = (Get-Content $classpathFile -Raw).Trim()
    & javac -encoding UTF-8 -nowarn -Xlint:none -cp $dependencies -sourcepath (Join-Path $VaslRoot 'src') `
        -d "$work/classes" (Join-Path $PSScriptRoot 'src/HexFactOracle.java')
    if ($LASTEXITCODE -ne 0) { throw 'Compiling the oracle failed.' }

    $classpath = "$work/classes" + [IO.Path]::PathSeparator + $dependencies
    & java '-Djava.awt.headless=true' -cp $classpath HexFactOracle $VaslRoot "$work/out" @Boards
    if ($LASTEXITCODE -ne 0) { throw 'The oracle reported failures.' }

    foreach ($json in Get-ChildItem "$work/out" -Filter '*.hexfacts.json') {
        $target = Join-Path $fixtures ($json.Name + '.gz')
        $source = [IO.File]::OpenRead($json.FullName)
        $output = [IO.File]::Create($target)
        try {
            $gzip = [IO.Compression.GZipStream]::new($output, [IO.Compression.CompressionLevel]::SmallestSize)
            try { $source.CopyTo($gzip) } finally { $gzip.Dispose() }
        }
        finally {
            $source.Dispose()
            $output.Dispose()
        }
    }

    Write-Host "Wrote $((Get-ChildItem "$work/out").Count) fixtures to $fixtures"
}
finally {
    Remove-Item -Recurse -Force $work
}
