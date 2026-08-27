[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $PublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$publishRoot = (Resolve-Path -LiteralPath $PublishDirectory).Path
$manifestPath = Join-Path -Path $publishRoot -ChildPath 'native-assets.win-x64.json'
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw 'The publish artifact is missing the win-x64 native dependency manifest.'
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8 | ConvertFrom-Json
if ($manifest.rid -ne 'win-x64') {
    throw ('Unsupported publish manifest RID: {0}' -f $manifest.rid)
}

foreach ($asset in $manifest.assets) {
    $assetPath = [System.IO.Path]::GetFullPath((Join-Path -Path $publishRoot -ChildPath $asset.relativePath))
    if (-not $assetPath.StartsWith($publishRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw ('Native dependency path escapes the publish root: {0}' -f $asset.name)
    }

    $assetFile = Get-Item -LiteralPath $assetPath -ErrorAction Stop
    if ($assetFile.Length -ne [long]$asset.length) {
        throw ('Native dependency length mismatch: {0}' -f $asset.name)
    }

    $actualHash = (Get-FileHash -LiteralPath $assetPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $asset.sha256) {
        throw ('Native dependency SHA-256 mismatch: {0}' -f $asset.name)
    }
}

$debugSymbols = @(Get-ChildItem -LiteralPath $publishRoot -Recurse -File -Filter '*.pdb')
if ($debugSymbols.Count -gt 0) {
    throw ('The Release publish artifact contains debug symbols: {0}' -f ($debugSymbols.FullName -join ', '))
}

$nativeFiles = Get-ChildItem -LiteralPath $publishRoot -Recurse -File |
    Where-Object { $_.Extension -in '.dll', '.exe' }
$duplicateHashes = @($nativeFiles |
    Group-Object Length |
    Where-Object Count -gt 1 |
    ForEach-Object { $_.Group } |
    ForEach-Object {
        [pscustomobject]@{
            Path = $_.FullName
            Hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
        }
    } |
    Group-Object Hash |
    Where-Object Count -gt 1)
if ($duplicateHashes.Count -gt 0) {
    $duplicates = $duplicateHashes | ForEach-Object {
        $_.Group.Path -join ', '
    }
    throw ('The publish artifact contains byte-identical binaries: {0}' -f ($duplicates -join '; '))
}

$applicationPath = Join-Path -Path $publishRoot -ChildPath 'JayTom.Dws.Client.exe'
if (-not (Test-Path -LiteralPath $applicationPath -PathType Leaf)) {
    throw 'The publish artifact is missing JayTom.Dws.Client.exe.'
}

Write-Host ('Publish smoke check passed: {0} native entry files, no PDB or duplicate binaries.' -f $manifest.assets.Count)
