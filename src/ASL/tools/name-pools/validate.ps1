param(
    [string]$Directory = (Join-Path $PSScriptRoot '../../units/names'),
    [string]$Python = 'python'
)

# PowerShell 7 supplies Test-Json; the Python integrity check uses only its standard library.
$ErrorActionPreference = 'Stop'
$nameRoot = (Resolve-Path -LiteralPath $Directory).Path
$manifestPath = Join-Path $nameRoot 'manifest.json'
$manifestSchema = Join-Path $nameRoot 'name-manifest.schema.json'
$poolSchema = Join-Path $nameRoot 'name-pool.schema.json'
if (-not (Test-Json -Json (Get-Content -LiteralPath $manifestPath -Raw) -SchemaFile $manifestSchema)) {
    throw 'Manifest does not conform to its schema.'
}
foreach ($nameFile in Get-ChildItem -LiteralPath $nameRoot -Filter '*.names.json' -File) {
    if (-not (Test-Json -Json (Get-Content -LiteralPath $nameFile.FullName -Raw) -SchemaFile $poolSchema)) {
        throw "Pool does not conform to its schema: $($nameFile.Name)"
    }
}
& $Python (Join-Path $PSScriptRoot 'validate.py') --directory $nameRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Name-pool integrity checks failed.'
}
Write-Output 'JSON Schema and cross-file integrity checks passed.'
