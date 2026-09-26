#Requires -Version 7
# Read-only survey of terrain bytes and XML elements, excluding artwork and comments.
param([Parameter(Mandatory)][string] $VaslRoot)
$ErrorActionPreference = 'Stop'

# Decode Java block data and count pixels in C# to avoid a PowerShell loop per pixel.
if (-not ('LosBoardSurvey' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.IO.Compression;
public static class LosBoardSurvey
{
    private static int ReadInt(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length != 4) throw new EndOfStreamException("Truncated Java integer");
        return checked((int)(((uint)bytes[0] << 24) | ((uint)bytes[1] << 16) |
                             ((uint)bytes[2] << 8) | bytes[3]));
    }
    public static int[] Count(byte[] data)
    {
        using var compressed = new MemoryStream(data);
        using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var stream = new MemoryStream();
        gzip.CopyTo(stream);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        if (reader.ReadByte() != 0xac || reader.ReadByte() != 0xed ||
            reader.ReadByte() != 0 || reader.ReadByte() != 5)
            throw new InvalidDataException("Not a Java object stream");
        using var payload = new MemoryStream();
        while (stream.Position < stream.Length)
        {
            byte tag = reader.ReadByte();
            int size = tag == 0x77 ? reader.ReadByte() : tag == 0x7a ? ReadInt(reader) :
                throw new InvalidDataException($"Unexpected Java stream tag {tag:x}");
            byte[] block = reader.ReadBytes(size);
            if (block.Length != size) throw new EndOfStreamException("Truncated Java block");
            payload.Write(block, 0, size);
        }
        payload.Position = 0;
        using var raw = new BinaryReader(payload);
        var result = new int[260];
        for (int i = 0; i < 4; i++) result[i] = ReadInt(raw);
        long count = (long)result[2] * result[3];
        if (payload.Length < 16 + 2 * count)
            throw new EndOfStreamException("Truncated terrain grid");
        for (long i = 0; i < count; i++)
        {
            raw.ReadByte(); // Elevation precedes each terrain code.
            result[4 + raw.ReadByte()]++;
        }
        return result;
    }
}
'@
}

function Get-ContentBlob([byte[]] $Data) {
    $header = [Text.Encoding]::UTF8.GetBytes("blob $($Data.Length)`0")
    [Convert]::ToHexString([Security.Cryptography.SHA1]::HashData([byte[]]($header + $Data))).ToLowerInvariant()
}

$catalogBytes = [IO.File]::ReadAllBytes((Join-Path $VaslRoot 'dist/boardData/SharedBoardMetadata.xml'))
$catalogXml = [xml][Text.Encoding]::UTF8.GetString($catalogBytes)
$catalog = @{}
foreach ($terrain in $catalogXml.SelectNodes('//terrainType')) {
    $catalog[[int]$terrain.typeCode] = $terrain
}
$totals = [ordered]@{ directories = 0; decodedGrids = 0; candidateBoards = 0 }
$boards = @(foreach ($directory in Get-ChildItem (Join-Path $VaslRoot 'boards/src') -Directory -Filter 'bd*' | Sort-Object Name -CaseSensitive) {
    $entry = [ordered]@{ board = $directory.Name }
    $totals.directories++
    $metadata = Join-Path $directory.FullName 'BoardMetadata.xml'
    if (Test-Path -LiteralPath $metadata) {
        $data = [IO.File]::ReadAllBytes($metadata)
        $entry.metadataContentBlob = Get-ContentBlob $data
        try {
            # Some VASL files declare XML 1.1 but use only XML 1.0 constructs.
            # Normalize that declaration for .NET without changing the hashed bytes.
            $metadataText = [Text.Encoding]::UTF8.GetString($data).TrimStart([char]0xfeff) -creplace '^<\?xml version="1\.1"', '<?xml version="1.0"'
            $xml = [System.Xml.XmlDocument]::new()
            $xml.LoadXml($metadataText)
            $geometry = [ordered]@{}
            foreach ($attribute in $xml.DocumentElement.Attributes) {
                if ($attribute.Name -cin @('width', 'height', 'hexWidth', 'hexHeight', 'A1CenterX', 'A1CenterY', 'altHexGrain', 'HexGridConfig')) {
                    $geometry[$attribute.Name] = $attribute.Value
                }
            }
            $entry.geometry = $geometry
            $entry.annotations = [ordered]@{}
            foreach ($name in @('partialorchard', 'rrembankment')) {
                $entry.annotations[$name] = $xml.SelectNodes("//$name").Count
            }
        }
        catch { $entry.metadataError = $_.Exception.GetBaseException().Message }
    }
    else { $entry.metadataError = 'missing' }
    $los = Join-Path $directory.FullName 'LOSData'
    if (Test-Path -LiteralPath $los) {
        $data = [IO.File]::ReadAllBytes($los)
        $entry.losDataBlob = Get-ContentBlob $data
        try {
            $counts = [LosBoardSurvey]::Count($data)
            $entry.header = @($counts[0..3])
            $entry.terrainPixels = [ordered]@{}
            $unknown = @()
            for ($code = 0; $code -lt 256; $code++) {
                if ($counts[4 + $code] -eq 0) { continue }
                if (-not $catalog.ContainsKey($code)) { $unknown += $code; continue }
                $terrain = $catalog[$code]
                if ($terrain.LOSCategory -cin @('ENTRENCHMENT', 'TUNNEL') -or
                    $terrain.name -cmatch 'Roofless|Gutted|Embankment|Rrembankment|PartialOrchard') {
                    $entry.terrainPixels[$terrain.name] = $counts[4 + $code]
                }
            }
            $entry.unknownCodes = $unknown
            $totals.decodedGrids++
        }
        catch { $entry.losError = $_.Exception.GetBaseException().Message }
    }
    else { $entry.losError = 'missing' }
    if ($entry.terrainPixels.Count -gt 0 -or ($entry.annotations.Values | Where-Object { $_ -gt 0 })) {
        $totals.candidateBoards++
    }
    $entry
})
$commit = & git -C $VaslRoot rev-parse HEAD
if ($LASTEXITCODE -ne 0) { throw 'Reading the VASL commit failed.' }
[ordered]@{
    vaslCommit = $commit.Trim()
    sharedMetadataContentBlob = Get-ContentBlob $catalogBytes
    summary = $totals
    boards = $boards
} | ConvertTo-Json -Depth 10
