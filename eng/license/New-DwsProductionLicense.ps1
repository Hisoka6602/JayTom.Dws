<#
    生成并验证 DWS 新版 v2 或旧版 legacy-v1 生产授权文件，默认新版。

    v2 私钥仅用于离线签发；公钥用于签发后验签，并作为客户端信任根随产物输出。
    legacy-v1 使用每个文件独立生成的密钥，兼容 2025 旧客户端，不使用生产签名密钥。
    机器码必须来自目标 DWS 设备，不能使用 GitHub runner 或开发机自己的机器码。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $LicenseCode,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^(?:[A-Fa-f0-9]{32}|[A-Fa-f0-9]{64})$')]
    [string] $MachineCode,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $CustomerName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ExpirationDate,

    [Parameter()]
    [ValidateSet('v2', 'legacy-v1')]
    [string] $LicenseFormat = 'v2',

    [Parameter()]
    [string] $PrivateKeyPath = '',

    [Parameter()]
    [string] $PublicKeyPath = '',

    [Parameter()]
    [ValidateRange(1, 100000)]
    [int] $MaxBindingScannerCount = 1,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $AppliedTemplateName = 'DWS',

    [Parameter()]
    [AllowEmptyString()]
    [string] $Remarks = '',

    [Parameter()]
    [ValidateSet('true', 'false', 'True', 'False', '1', '0')]
    [string] $IsAvailable = 'true',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $OutputPath = (Join-Path -Path (Get-Location) -ChildPath 'License.key'),

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $TrustOutputDirectory,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $ManifestPath,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$engDirectory = Split-Path -Parent $scriptDirectory
$repositoryRoot = Split-Path -Parent $engDirectory
$toolProjectPath = Join-Path -Path $repositoryRoot -ChildPath 'JayTom.Dws.LicenseTool\JayTom.Dws.LicenseTool.csproj'
$outputFullPath = [System.IO.Path]::GetFullPath($OutputPath)
$outputDirectory = [System.IO.Path]::GetDirectoryName($outputFullPath)
$normalizedMachineCode = $MachineCode.Trim().ToUpperInvariant()
$LicenseFormat = $LicenseFormat.ToLowerInvariant()
$isAvailableValue = $IsAvailable -in @('true', 'True', '1')
$expectedMachineCodeLength = if ($LicenseFormat -eq 'v2') { 64 } else { 32 }
if ($normalizedMachineCode.Length -ne $expectedMachineCodeLength) {
    throw ('{0} 格式需要 {1} 位十六进制机器码。' -f $LicenseFormat, $expectedMachineCodeLength)
}

if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    throw '授权文件输出目录不能为空。'
}

if ($LicenseFormat -eq 'v2') {
    if ([string]::IsNullOrWhiteSpace($PrivateKeyPath) -or [string]::IsNullOrWhiteSpace($PublicKeyPath)) {
        throw 'v2 格式必须提供 PrivateKeyPath 和 PublicKeyPath。'
    }

    $privateKeyFullPath = [System.IO.Path]::GetFullPath($PrivateKeyPath)
    $publicKeyFullPath = [System.IO.Path]::GetFullPath($PublicKeyPath)
    foreach ($keyPath in @($privateKeyFullPath, $publicKeyFullPath)) {
        if (-not (Test-Path -LiteralPath $keyPath -PathType Leaf)) {
            throw ('找不到授权签名密钥文件：{0}' -f $keyPath)
        }

        $keyFile = Get-Item -LiteralPath $keyPath -ErrorAction Stop
        if ($keyFile.Length -le 0 -or $keyFile.Length -gt 65536) {
            throw ('授权签名密钥文件大小无效：{0}' -f $keyPath)
        }
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($PrivateKeyPath) -or -not [string]::IsNullOrWhiteSpace($PublicKeyPath)) {
    throw 'legacy-v1 使用独立的一次性密钥，不能提供生产签名密钥。'
}

if ([string]::IsNullOrWhiteSpace($TrustOutputDirectory)) {
    $TrustOutputDirectory = Join-Path -Path $outputDirectory -ChildPath 'license-trust'
}
if ([string]::IsNullOrWhiteSpace($ManifestPath)) {
    $ManifestPath = Join-Path -Path $outputDirectory -ChildPath 'license-manifest.json'
}

$trustOutputFullPath = [System.IO.Path]::GetFullPath($TrustOutputDirectory)
$manifestFullPath = [System.IO.Path]::GetFullPath($ManifestPath)
New-Item -Path $outputDirectory -ItemType Directory -Force | Out-Null

Write-Host ('正在生成 DWS {0} 生产授权文件。' -f $LicenseFormat)
Write-Host ('目标机器码：{0}' -f $normalizedMachineCode)
Write-Host ('输出路径：{0}' -f $outputFullPath)

$commonDotnetArguments = @(
    'run',
    '--project', $toolProjectPath,
    '--configuration', $Configuration,
    '--framework', 'net10.0-windows',
    '--no-build',
    '--no-restore',
    '-p:Platform=x64'
)
$generationArguments = @(
    $commonDotnetArguments
    '--'
    '--format', $LicenseFormat
    '--license-code', $LicenseCode
    '--machine-code', $normalizedMachineCode
    '--customer-name', $CustomerName
    '--expiration-date', $ExpirationDate
    '--max-binding-scanner-count', [string]$MaxBindingScannerCount
    '--applied-template-name', $AppliedTemplateName
    '--is-available', $isAvailableValue.ToString().ToLowerInvariant()
    '--output', $outputFullPath
)
if ($LicenseFormat -eq 'v2') {
    $generationArguments += @('--private-key', $privateKeyFullPath)
}
if (-not [string]::IsNullOrWhiteSpace($Remarks)) {
    $generationArguments += @('--remarks', $Remarks)
}

& dotnet @generationArguments
if ($LASTEXITCODE -ne 0) {
    throw ('授权生成工具执行失败，退出码：{0}' -f $LASTEXITCODE)
}

$licenseFile = Get-Item -LiteralPath $outputFullPath -ErrorAction Stop
if ($licenseFile.Length -le 0 -or $licenseFile.Length -gt 1048576) {
    throw ('授权文件大小无效：{0}' -f $outputFullPath)
}

$validationArguments = @(
    $commonDotnetArguments
    '--'
    '--format', $LicenseFormat
    '--validate-file', $outputFullPath
    '--machine-code', $normalizedMachineCode
)
if ($LicenseFormat -eq 'v2') {
    $envelope = Get-Content -LiteralPath $outputFullPath -Raw -Encoding utf8 | ConvertFrom-Json
    if ($envelope.formatVersion -ne 2 -or $envelope.algorithm -ne 'PS256') {
        throw '授权文件不是受支持的 DWS v2/PS256 签名包络。'
    }
    if ([string]$envelope.keyId -notmatch '^dws-[A-Za-z0-9_-]{16}$') {
        throw '授权文件包含无效的签名密钥标识。'
    }

    $validationArguments += @('--public-key', $publicKeyFullPath)
}
& dotnet @validationArguments
if ($LASTEXITCODE -ne 0) {
    throw ('授权文件生成后校验失败，退出码：{0}' -f $LASTEXITCODE)
}

$manifest = [ordered]@{
    schemaVersion = 1
    licenseFormat = $LicenseFormat
    licenseVersion = 1
    signatureAlgorithm = $null
    keyId = $null
    machineCode = $normalizedMachineCode
    licenseFile = [System.IO.Path]::GetFileName($outputFullPath)
    licenseSha256 = (Get-FileHash -LiteralPath $outputFullPath -Algorithm SHA256).Hash
    trustFile = $null
    trustFileSha256 = $null
}
if ($LicenseFormat -eq 'v2') {
    New-Item -Path $trustOutputFullPath -ItemType Directory -Force | Out-Null
    $trustFilePath = Join-Path -Path $trustOutputFullPath -ChildPath ('{0}.pem' -f $envelope.keyId)
    Copy-Item -LiteralPath $publicKeyFullPath -Destination $trustFilePath -Force
    $manifest.licenseVersion = 2
    $manifest.signatureAlgorithm = 'PS256'
    $manifest.keyId = [string]$envelope.keyId
    $manifest.trustFile = ('license-trust/{0}.pem' -f $envelope.keyId)
    $manifest.trustFileSha256 = (Get-FileHash -LiteralPath $trustFilePath -Algorithm SHA256).Hash
}
$manifestJson = $manifest | ConvertTo-Json
$utf8WithoutBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($manifestFullPath, $manifestJson, $utf8WithoutBom)

# 授权文件只允许当前生成账户、LOCAL SYSTEM 和本机管理员访问，移除继承的普通用户权限。
$currentSid = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
$systemSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-18')
$administratorsSid = [System.Security.Principal.SecurityIdentifier]::new('S-1-5-32-544')
$licenseAcl = [System.Security.AccessControl.FileSecurity]::new()
$licenseAcl.SetAccessRuleProtection($true, $false)
$licenseAcl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new(
    $currentSid,
    [System.Security.AccessControl.FileSystemRights]::Read,
    [System.Security.AccessControl.AccessControlType]::Allow))
$licenseAcl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new(
    $systemSid,
    [System.Security.AccessControl.FileSystemRights]::FullControl,
    [System.Security.AccessControl.AccessControlType]::Allow))
$licenseAcl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new(
    $administratorsSid,
    [System.Security.AccessControl.FileSystemRights]::FullControl,
    [System.Security.AccessControl.AccessControlType]::Allow))
Set-Acl -LiteralPath $outputFullPath -AclObject $licenseAcl

Write-Host ('授权文件已生成并校验：{0}' -f $outputFullPath)
if ($LicenseFormat -eq 'v2') {
    Write-Host ('签名密钥标识：{0}' -f $envelope.keyId)
    Write-Host ('客户端信任根：{0}' -f $trustFilePath)
}
Write-Host ('完整性清单：{0}' -f $manifestFullPath)
