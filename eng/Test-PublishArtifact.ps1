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
    Where-Object Count -gt 1 |
    Where-Object {
        # 自包含 .NET 运行时携带同内容的 DAC 原名和带版本别名，供不同诊断工具定位。
        # 只允许发布根目录下这一对文件，其他同内容二进制仍按重复资产拒绝。
        $duplicatePaths = @($_.Group.Path)
        $rootFileNames = @($duplicatePaths |
            Where-Object { [System.IO.Path]::GetDirectoryName($_) -eq $publishRoot } |
            ForEach-Object { [System.IO.Path]::GetFileName($_) })
        $isRuntimeDiagnosticAlias = $duplicatePaths.Count -eq 2 -and
            $rootFileNames.Count -eq 2 -and
            $rootFileNames -contains 'mscordaccore.dll' -and
            @($rootFileNames | Where-Object { $_ -match '^mscordaccore_amd64_amd64_\d+\.\d+\.\d+\.\d+\.dll$' }).Count -eq 1
        -not $isRuntimeDiagnosticAlias
    })
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

Write-Host ('Publish smoke check passed: {0} native entry files, no PDB or unexpected duplicate binaries.' -f $manifest.assets.Count)
