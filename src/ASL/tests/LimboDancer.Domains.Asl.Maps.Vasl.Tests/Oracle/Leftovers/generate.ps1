#Requires -Version 7
param([Parameter(Mandatory)][string] $VaslRoot)
$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) ('los-leftovers-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $work, "$work/classes", "$work/out" | Out-Null
$harness = (Resolve-Path "$PSScriptRoot/../../../../tools/vasl-hexfact-oracle/src/HexFactOracle.java").Path
Push-Location $VaslRoot
try {
    & ./mvnw.cmd -B -q dependency:build-classpath "-Dmdep.outputFile=$work/classpath.txt"
    if ($LASTEXITCODE -ne 0) { throw 'Resolving VASL dependencies failed.' }
}
finally { Pop-Location }
$dependencies = (Get-Content "$work/classpath.txt" -Raw).Trim()
& javac -encoding UTF-8 -nowarn -Xlint:none -cp $dependencies -sourcepath "$VaslRoot/src" -d "$work/classes" $harness "$PSScriptRoot/LeftoverLosOracle.java"
if ($LASTEXITCODE -ne 0) { throw 'Compiling the oracle failed.' }
'<configuration><root level="OFF" /></configuration>' | Set-Content "$work/logback.xml"
& java '-Djava.awt.headless=true' "-Dlogback.configurationFile=$work/logback.xml" -cp ("$work/classes" + [IO.Path]::PathSeparator + $dependencies) LeftoverLosOracle $VaslRoot "$PSScriptRoot/maps.xml" "$work/out"
if ($LASTEXITCODE -ne 0) { throw 'Generating controlled LOS fixtures failed.' }
foreach ($json in Get-ChildItem "$work/out" -Filter '*.json') {
    $inputStream = [IO.File]::OpenRead($json.FullName)
    $outputStream = [IO.File]::Create((Join-Path $PSScriptRoot ($json.BaseName + '.los.json.gz')))
    $gzip = [IO.Compression.GZipStream]::new($outputStream, [IO.Compression.CompressionLevel]::Optimal)
    try { $inputStream.CopyTo($gzip) }
    finally { $gzip.Dispose(); $outputStream.Dispose(); $inputStream.Dispose() }
}
Write-Output "Generated fixtures. Temporary compilation files: $work"
