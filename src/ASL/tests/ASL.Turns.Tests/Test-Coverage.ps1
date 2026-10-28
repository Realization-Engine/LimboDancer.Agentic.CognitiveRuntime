param()

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'ASL.Turns.Tests.csproj'
$settings = Join-Path $PSScriptRoot 'coverage.runsettings'
$results = Join-Path $PSScriptRoot ('TestResults/' + [Guid]::NewGuid().ToString('N'))

& dotnet test $project --settings $settings --collect 'XPlat Code Coverage' --results-directory $results
if ($LASTEXITCODE -ne 0) { throw 'ASL.Turns tests failed.' }

$reports = @(Get-ChildItem -LiteralPath $results -Recurse -Filter 'coverage.cobertura.xml')
if ($reports.Count -ne 1) { throw 'Expected exactly one fresh coverage report.' }
[xml]$report = Get-Content -LiteralPath $reports[0].FullName -Raw
$packages = @($report.coverage.packages.package)
if ($packages.Count -ne 1 -or $packages[0].name -ne 'ASL.Turns') {
    throw 'Coverage must measure exactly the ASL.Turns library.'
}
$coverage = $report.coverage
if ([int]$coverage.'lines-valid' -le 0 -or [int]$coverage.'branches-valid' -le 0 -or
    [int]$coverage.'lines-covered' -ne [int]$coverage.'lines-valid' -or
    [int]$coverage.'branches-covered' -ne [int]$coverage.'branches-valid') {
    throw "ASL.Turns requires 100% line and branch coverage. See $($reports[0].FullName)"
}
Write-Output "ASL.Turns: 100% lines ($($coverage.'lines-covered')/$($coverage.'lines-valid')); 100% branches ($($coverage.'branches-covered')/$($coverage.'branches-valid'))."
Write-Output "Report: $($reports[0].FullName)"
