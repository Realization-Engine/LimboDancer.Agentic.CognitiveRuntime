param(
    [string]$Directory = (Join-Path $PSScriptRoot '../../units/names')
)

# PowerShell 7 supplies Test-Json; the C# integrity check uses the .NET 10 SDK.
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
& dotnet run --project (Join-Path $PSScriptRoot 'NamePoolValidation.csproj') --configuration Release --disable-build-servers -- $nameRoot
if ($LASTEXITCODE -ne 0) {
    throw 'Name-pool integrity checks failed.'
}
Write-Output 'JSON Schema and cross-file integrity checks passed.'
