<#
    生产 DWS 授权文件。

    此脚本不再调用授权服务端，而是调用仓库内的授权生成工具直接产出 .key 文件。
    机器码必须来自目标 DWS 设备，不能使用 GitHub runner 或开发机自己的机器码。
#>
[CmdletBinding()]
param(
    [Parameter()]
    [AllowEmptyString()]
    [string] $LicenseCode = '',

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Fa-f0-9]{64}$')]
    [string] $MachineCode,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $CustomerName,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string] $ExpirationDate,

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
$isAvailableValue = [System.Convert]::ToBoolean($IsAvailable)

if ([string]::IsNullOrWhiteSpace($LicenseCode)) {
    $alphabet = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    $builder = [System.Text.StringBuilder]::new(32)
    $randomNumberGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $randomByte = New-Object byte[] 1
        for ($index = 0; $index -lt 32; $index++) {
            do {
                $randomNumberGenerator.GetBytes($randomByte)
                $randomValue = [int]$randomByte[0]
            } while ($randomValue -ge 252)

            $charIndex = $randomValue % $alphabet.Length
            [void]$builder.Append($alphabet[$charIndex])
        }
    }
    finally {
        $randomNumberGenerator.Dispose()
    }

    $LicenseCode = $builder.ToString()
    Write-Host "::add-mask::$LicenseCode"
    Write-Host '授权码未填写，已自动生成（内容已隐藏）。'
}

if ([string]::IsNullOrWhiteSpace($outputDirectory)) {
    throw '授权文件输出目录不能为空。'
}

New-Item -Path $outputDirectory -ItemType Directory -Force | Out-Null

Write-Host '正在离线生成 DWS 生产授权文件。'
Write-Host ('目标机器码：{0}' -f $MachineCode.Trim().ToUpperInvariant())
Write-Host ('输出路径：{0}' -f $outputFullPath)

dotnet run `
    --project $toolProjectPath `
    --configuration $Configuration `
    --framework net10.0-windows `
    -p:Platform=x64 `
    -- `
    --license-code $LicenseCode `
    --machine-code $MachineCode `
    --customer-name $CustomerName `
    --expiration-date $ExpirationDate `
    --max-binding-scanner-count $MaxBindingScannerCount `
    --applied-template-name $AppliedTemplateName `
    --remarks $Remarks `
    --is-available $isAvailableValue `
    --output $outputFullPath

if ($LASTEXITCODE -ne 0) {
    throw ('授权生成工具执行失败，退出码：{0}' -f $LASTEXITCODE)
}

$licenseFile = Get-Item -LiteralPath $outputFullPath -ErrorAction Stop
if ($licenseFile.Length -le 0) {
    throw ('授权文件生成成功但内容为空：{0}' -f $outputFullPath)
}

Write-Host ('授权文件已生成：{0}' -f $outputFullPath)
