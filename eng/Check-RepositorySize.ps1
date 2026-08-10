param(
    [long]$MaximumFileBytes = 100MB,
    [long]$MaximumTrackedBytes = 2GB
)

$ErrorActionPreference = 'Stop'
$workspacePath = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$trackedOutput = & git -C $workspacePath ls-files -z
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Git 跟踪文件列表。'
}

$trackedPaths = ($trackedOutput -join "`n") -split "`0" | Where-Object { $_ }
$oversizedFiles = [System.Collections.Generic.List[string]]::new()
$generatedFiles = [System.Collections.Generic.List[string]]::new()
[long]$totalBytes = 0

foreach ($relativePath in $trackedPaths) {
    $normalizedPath = $relativePath.Replace('\', '/')
    if ($normalizedPath -match '(^|/)(bin|obj)/') {
        $generatedFiles.Add($normalizedPath)
    }

    $absolutePath = Join-Path $workspacePath $relativePath
    if (-not (Test-Path -LiteralPath $absolutePath -PathType Leaf)) {
        continue
    }

    $length = (Get-Item -LiteralPath $absolutePath).Length
    $totalBytes += $length
    if ($length -gt $MaximumFileBytes) {
        $oversizedFiles.Add("$normalizedPath ($([Math]::Round($length / 1MB, 1)) MB)")
    }
}

if ($generatedFiles.Count -gt 0) {
    throw "仓库跟踪了生成目录中的文件：`n$($generatedFiles -join "`n")"
}

if ($oversizedFiles.Count -gt 0) {
    throw "存在超过 $([Math]::Round($MaximumFileBytes / 1MB)) MB 的跟踪文件：`n$($oversizedFiles -join "`n")"
}

if ($totalBytes -gt $MaximumTrackedBytes) {
    throw "跟踪文件总量为 $([Math]::Round($totalBytes / 1GB, 2)) GB，超过 $([Math]::Round($MaximumTrackedBytes / 1GB, 2)) GB 上限。"
}

Write-Host "仓库体积检查通过：$($trackedPaths.Count) 个文件，共 $([Math]::Round($totalBytes / 1GB, 2)) GB。"
