<# 使用测试密钥验证默认新版、显式旧版和 2025 客户端二进制读取兼容性。 #>
[CmdletBinding()]
param([string] $Configuration = 'Release')

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$toolPath = Join-Path $repositoryRoot ('JayTom.Dws.LicenseTool\bin\x64\{0}\net10.0-windows\JayTom.Dws.LicenseTool.dll' -f $Configuration)
if (-not (Test-Path -LiteralPath $toolPath -PathType Leaf)) { throw '请先构建 Release/x64 授权工具。' }
$temporaryRoot = if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { [System.IO.Path]::GetTempPath() } else { $env:RUNNER_TEMP }
$temporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot).TrimEnd('\')
$testDirectory = Join-Path $temporaryRoot ('dws-license-compatibility-' + [Guid]::NewGuid().ToString('N'))
[void][System.IO.Directory]::CreateDirectory($testDirectory)
$script:checkCount = 0

function Invoke-LicenseTool([string[]] $ToolArguments, [bool] $ShouldSucceed = $true) {
    $output = & dotnet $toolPath @ToolArguments 2>&1
    $exitCode = $LASTEXITCODE
    if ($ShouldSucceed -and $exitCode -ne 0) { throw ($output -join [Environment]::NewLine) }
    if (-not $ShouldSucceed -and $exitCode -eq 0) { throw '无效的授权输入未被拒绝。' }
    $script:checkCount++
}

function Read-UsingLegacyClient([string] $Path) {
    # 读取顺序、AES 参数、XML 私钥和 PKCS#1 分块解密来自 2025 客户端。
    $stream = [System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Path))
    $reader = [System.IO.BinaryReader]::new($stream)
    $aes = [System.Security.Cryptography.Aes]::Create()
    $rsa = [System.Security.Cryptography.RSA]::Create()
    $decrypted = [System.IO.MemoryStream]::new()
    try {
        $encryptedPrivateKey = $reader.ReadBytes($reader.ReadInt32())
        $aes.Key = $reader.ReadBytes($reader.ReadInt32())
        $aes.IV = $reader.ReadBytes($reader.ReadInt32())
        $encryptedData = $reader.ReadBytes($reader.ReadInt32())
        if ($stream.Position -ne $stream.Length) { throw '旧版文件存在多余数据。' }
        $decryptor = $aes.CreateDecryptor()
        try {
            $privateBytes = $decryptor.TransformFinalBlock($encryptedPrivateKey, 0, $encryptedPrivateKey.Length)
            $rsa.FromXmlString([System.Text.Encoding]::UTF8.GetString($privateBytes))
        }
        finally { $decryptor.Dispose() }
        $blockSize = $rsa.KeySize / 8
        for ($offset = 0; $offset -lt $encryptedData.Length; $offset += $blockSize) {
            $block = [byte[]]::new([Math]::Min($blockSize, $encryptedData.Length - $offset))
            [System.Buffer]::BlockCopy($encryptedData, $offset, $block, 0, $block.Length)
            $plainBlock = $rsa.Decrypt($block, [System.Security.Cryptography.RSAEncryptionPadding]::Pkcs1)
            $decrypted.Write($plainBlock, 0, $plainBlock.Length)
        }
        return @{
            Claims = [System.Text.Encoding]::UTF8.GetString($decrypted.ToArray()) | ConvertFrom-Json
            PublicKey = [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo())
        }
    }
    finally {
        $decrypted.Dispose()
        $rsa.Dispose()
        $aes.Dispose()
        $reader.Dispose()
        $stream.Dispose()
    }
}

try {
    $privatePath = Join-Path $testDirectory 'test-private.pem'
    $publicPath = Join-Path $testDirectory 'test-public.pem'
    Invoke-LicenseTool @('--generate-key-pair', '--private-key-output', $privatePath, '--public-key-output', $publicPath)
    $machineV2 = 'A' * 64
    $machineLegacy = 'B' * 32
    $remarks = '旧版中文分块兼容测试' * 100
    $common = @('--license-code', 'COMPATIBILITY-TEST-ONLY', '--customer-name', '旧版项目测试', '--expiration-date', (Get-Date).AddDays(30).ToString('yyyy-MM-dd'), '--max-binding-scanner-count', '3', '--remarks', $remarks)
    $v2Path = Join-Path $testDirectory 'v2.key'
    $legacyPath = Join-Path $testDirectory 'legacy.key'
    $secondLegacyPath = Join-Path $testDirectory 'legacy-second.key'

    Invoke-LicenseTool ($common + @('--machine-code', $machineV2, '--private-key', $privatePath, '--output', $v2Path))
    Invoke-LicenseTool @('--validate-file', $v2Path, '--public-key', $publicPath, '--machine-code', $machineV2)
    if ((Get-Content -LiteralPath $v2Path -Raw | ConvertFrom-Json).formatVersion -ne 2) { throw '默认格式不是 v2。' }

    Invoke-LicenseTool ($common + @('--format', 'legacy-v1', '--machine-code', $machineLegacy, '--output', $legacyPath))
    Invoke-LicenseTool @('--format', 'legacy-v1', '--validate-file', $legacyPath, '--machine-code', $machineLegacy)
    $legacy = Read-UsingLegacyClient $legacyPath
    if ($legacy.Claims.LicenseCode -ne 'COMPATIBILITY-TEST-ONLY' -or $legacy.Claims.MachineCode -ne $machineLegacy -or $legacy.Claims.UserName -ne '旧版项目测试' -or $legacy.Claims.Remarks -ne $remarks -or $legacy.Claims.MaxBindingScannerCount -ne 3) { throw '2025 客户端协议读取结果与输入不一致。' }
    Invoke-LicenseTool ($common + @('--format', 'legacy-v1', '--machine-code', $machineLegacy, '--output', $secondLegacyPath))
    $secondLegacy = Read-UsingLegacyClient $secondLegacyPath
    if ($legacy.PublicKey -eq $secondLegacy.PublicKey) { throw '旧版文件复用了 RSA 密钥。' }
    $productionRsa = [System.Security.Cryptography.RSA]::Create()
    try {
        $productionRsa.ImportFromPem([System.IO.File]::ReadAllText($publicPath))
        if ($legacy.PublicKey -eq [Convert]::ToBase64String($productionRsa.ExportSubjectPublicKeyInfo())) { throw '旧版文件使用了生产签名密钥。' }
    }
    finally { $productionRsa.Dispose() }

    Invoke-LicenseTool ($common + @('--machine-code', $machineLegacy, '--output', (Join-Path $testDirectory 'invalid-default.key'))) $false
    Invoke-LicenseTool ($common + @('--format', 'legacy-v1', '--machine-code', $machineV2, '--output', (Join-Path $testDirectory 'invalid-machine.key'))) $false
    Invoke-LicenseTool ($common + @('--format', 'legacy-v1', '--machine-code', $machineLegacy, '--private-key', $privatePath, '--output', (Join-Path $testDirectory 'invalid-key.key'))) $false
    Invoke-LicenseTool @('--format', 'legacy-v1', '--validate-file', $legacyPath, '--machine-code', ('C' * 32)) $false
    $brokenPath = Join-Path $testDirectory 'broken.key'
    [System.IO.File]::WriteAllBytes($brokenPath, [byte[]]@(255, 255, 255, 255))
    Invoke-LicenseTool @('--format', 'legacy-v1', '--validate-file', $brokenPath, '--machine-code', $machineLegacy) $false
    $frozenPath = Join-Path $testDirectory 'frozen.key'
    Invoke-LicenseTool ($common + @('--format', 'legacy-v1', '--machine-code', $machineLegacy, '--is-available', 'false', '--output', $frozenPath))
    Invoke-LicenseTool @('--format', 'legacy-v1', '--validate-file', $frozenPath, '--machine-code', $machineLegacy) $false
    Write-Host ('新旧授权兼容性检查通过：{0} 项命令检查，含独立 2025 协议解密及密钥隔离。' -f $script:checkCount)
}
finally {
    $resolvedTestDirectory = [System.IO.Path]::GetFullPath($testDirectory)
    if (-not $resolvedTestDirectory.StartsWith($temporaryRoot + '\dws-license-compatibility-', [StringComparison]::OrdinalIgnoreCase)) { throw '测试清理目录越界。' }
    if (Test-Path -LiteralPath $resolvedTestDirectory) { Remove-Item -LiteralPath $resolvedTestDirectory -Recurse -Force }
}
