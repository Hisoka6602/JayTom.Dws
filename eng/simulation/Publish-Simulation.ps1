[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'Publish and run DWS locally on Windows; Docker is reserved for Python device fixtures.'
}
$simulationWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$simulationReleaseRoot = Join-Path $simulationWorkspace ('output/simulation/local-publish/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $simulationReleaseRoot | Out-Null
$simulationPublishOptions = @('-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
    '-p:Platform=x64', '-p:DebugType=none', '-p:DebugSymbols=false',
    '-p:CopyOutputSymbolsToPublishDirectory=false', '-p:SatelliteResourceLanguages=en-US%3Bzh-CN%3Bzh-Hans',
    '--disable-build-servers', '-m:1', '-nr:false', '--verbosity', 'quiet')

# Fresh, isolated staging directories avoid clearing PublishClean or overwriting a deployed program.
$simulationClientStaging = Join-Path $simulationReleaseRoot 'client-staging'
$simulationHostStaging = Join-Path $simulationReleaseRoot 'host-staging'
& dotnet publish (Join-Path $simulationWorkspace 'JayTom.Dws.Client/JayTom.Dws.Client.csproj') @simulationPublishOptions -o $simulationClientStaging 2>&1 |
    Tee-Object -FilePath (Join-Path $simulationReleaseRoot 'client-publish.log')
if ($LASTEXITCODE -ne 0) { throw 'Local Windows client publish failed.' }
& (Join-Path $simulationWorkspace 'eng/Test-PublishArtifact.ps1') -PublishDirectory $simulationClientStaging
& dotnet publish (Join-Path $PSScriptRoot 'RuntimeHost/RuntimeHost.csproj') @simulationPublishOptions -o $simulationHostStaging 2>&1 |
    Tee-Object -FilePath (Join-Path $simulationReleaseRoot 'host-publish.log')
if ($LASTEXITCODE -ne 0) { throw 'Local Windows simulation host publish failed.' }

# Verify the actual production managed assemblies in the test host, not merely their version text.
$simulationAssemblyHashes = [Collections.Generic.List[object]]::new()
foreach ($simulationClientAssembly in Get-ChildItem -LiteralPath $simulationClientStaging -File -Filter 'JayTom.Dws.*.dll') {
    $simulationHostAssembly = Join-Path $simulationHostStaging $simulationClientAssembly.Name
    if (-not (Test-Path -LiteralPath $simulationHostAssembly -PathType Leaf)) {
        throw "Published test host is missing production assembly $($simulationClientAssembly.Name)."
    }
    $simulationClientHash = (Get-FileHash -LiteralPath $simulationClientAssembly.FullName -Algorithm SHA256).Hash
    if ($simulationClientHash -ne (Get-FileHash -LiteralPath $simulationHostAssembly -Algorithm SHA256).Hash) {
        throw "Production/test-host assembly mismatch: $($simulationClientAssembly.Name)."
    }
    $simulationAssemblyHashes.Add([ordered]@{ name = $simulationClientAssembly.Name; sha256 = $simulationClientHash })
}

# Run copies of the published artifacts; preserve pristine staging evidence for inspection.
$simulationClientDeployment = Join-Path $simulationReleaseRoot 'client-deployment'
$simulationHostDeployment = Join-Path $simulationReleaseRoot 'runtime'
Copy-Item -LiteralPath $simulationClientStaging -Destination $simulationClientDeployment -Recurse
Copy-Item -LiteralPath $simulationHostStaging -Destination $simulationHostDeployment -Recurse
$simulationManifest = [ordered]@{
    published_on = 'local Windows'; application_execution = 'local Windows'; device_fixtures = 'Docker Python only'
    client_deployment = $simulationClientDeployment; runtime_directory = $simulationHostDeployment
    verified_production_assemblies = $simulationAssemblyHashes.ToArray()
}
$simulationManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $simulationReleaseRoot 'local-publish.json') -Encoding utf8
Write-Host "Local publish verified. RuntimeDirectory: $simulationHostDeployment"
