[CmdletBinding()]
param(
    [ValidateRange(5000, 60000)][int]$Count = 5000,
    [ValidateSet('dense-off', 'dense-on', 'dual-window-off', 'dual-window-on', 'lower-removal-on', 'realtime-on', 'reply-faults-off', 'reply-faults-on', 'query-stress-off', 'query-stress-on')]
    [string[]]$Scenario = @('dense-off', 'dense-on', 'dual-window-off', 'dual-window-on', 'lower-removal-on', 'realtime-on'),
    [string]$RuntimeDirectory,
    [string]$PythonExecutable,
    [switch]$SkipBuild,
    [switch]$KeepContainers
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$simulationWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$simulationStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$simulationOutput = Join-Path $simulationWorkspace "output/simulation/runs/$simulationStamp"
$simulationCompose = Join-Path $PSScriptRoot 'compose.yaml'
$simulationProject = Join-Path $PSScriptRoot 'RuntimeHost/RuntimeHost.csproj'
$simulationHost = Join-Path $PSScriptRoot 'RuntimeHost/bin/x64/Release/net10.0-windows/RuntimeHost.dll'
$simulationStarted = $false
$simulationReports = [Collections.Generic.List[object]]::new()
$simulationPreviousProcessorCount = [Environment]::GetEnvironmentVariable('DOTNET_PROCESSOR_COUNT', 'Process')

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw 'The DWS application must run on the local Windows computer; only device fixtures run in Docker.'
}
if (-not $PythonExecutable) {
    $simulationPythonCommand = Get-Command python, python3 -ErrorAction SilentlyContinue |
        Where-Object { $_.Source -and $_.Source -notlike '*WindowsApps*' } | Select-Object -First 1
    if ($simulationPythonCommand) { $PythonExecutable = $simulationPythonCommand.Source }
    else {
        $simulationBundledPython = Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
        if (Test-Path -LiteralPath $simulationBundledPython -PathType Leaf) { $PythonExecutable = $simulationBundledPython }
        else { throw 'A local Python 3.10+ interpreter is required for independent audits. Specify -PythonExecutable.' }
    }
}
& $PythonExecutable -X utf8 -c 'import sys; assert sys.version_info >= (3, 10), "Python 3.10+ required"'
if ($LASTEXITCODE -ne 0) { throw 'The local Python audit interpreter failed its startup check.' }
if ($RuntimeDirectory) {
    $simulationRuntimeRoot = (Resolve-Path -LiteralPath $RuntimeDirectory).Path
    $simulationHost = Join-Path $simulationRuntimeRoot 'RuntimeHost.exe'
    if (-not (Test-Path -LiteralPath $simulationHost -PathType Leaf)) {
        throw "Published Windows RuntimeHost.exe is missing: $simulationHost"
    }
}

# Refuse to recreate fixtures used by another active run, including the long-test service.
foreach ($simulationPort in @(18086, 18096)) {
    $simulationHealth = $null
    try { $simulationHealth = Invoke-RestMethod "http://127.0.0.1:$simulationPort/health" -TimeoutSec 2 }
    catch { }
    if ($simulationHealth -and ($simulationHealth.plc_connected -or $simulationHealth.camera_connected)) {
        throw "An active DWS simulation is using fixture port $simulationPort. Wait for it to finish."
    }
}

New-Item -ItemType Directory -Path $simulationOutput -Force | Out-Null
Push-Location $simulationWorkspace
try {
    if (-not $SkipBuild -and -not $RuntimeDirectory) {
        & dotnet build $simulationProject -c Release -p:Platform=x64 --disable-build-servers -m:1 -nr:false --verbosity quiet 2>&1 |
            Tee-Object -FilePath (Join-Path $simulationOutput 'build.log')
        if ($LASTEXITCODE -ne 0) { throw 'RuntimeHost build failed.' }
    }
    & docker compose -p jaytom-dws-simulation -f $simulationCompose up -d --build devices
    if ($LASTEXITCODE -ne 0) { throw 'Docker fixture startup failed.' }
    $simulationStarted = $true
    for ($simulationAttempt = 0; $simulationAttempt -lt 30; $simulationAttempt++) {
        try {
            $simulationReady = Invoke-RestMethod 'http://127.0.0.1:18086/health' -TimeoutSec 2
            if ($simulationReady.ready) { break }
        } catch { }
        Start-Sleep -Seconds 1
    }
    if (-not $simulationReady.ready) { throw 'Python fixture did not become ready.' }
    foreach ($simulationCase in $Scenario) {
        $simulationConfig = [ordered]@{
            name = "$simulationStamp-$simulationCase-$Count"
            count = $Count; interval_ms = 1; cameras = 1; use_noread = -not $simulationCase.EndsWith('-off')
            window = $false; removal = 'fill'; api_faults = $true; duplicate_callbacks = $true
            batch_size = 16; image_width = 96; image_height = 64
        }
        if ($simulationCase.StartsWith('reply-faults')) { $simulationConfig.reply_faults = $true }
        if ($simulationCase.StartsWith('query-stress')) {
            $simulationConfig.query_stress = $true; $simulationConfig.seed_history_count = 50000
            $simulationConfig.interval_ms = 10; $simulationConfig.batch_size = 1
            [Environment]::SetEnvironmentVariable('DOTNET_PROCESSOR_COUNT', '1', 'Process')
        } else {
            [Environment]::SetEnvironmentVariable('DOTNET_PROCESSOR_COUNT', $simulationPreviousProcessorCount, 'Process')
        }
        if ($simulationCase.StartsWith('dual-window')) {
            $simulationConfig.cameras = 2; $simulationConfig.window = $true
            $simulationConfig.use_noread = $simulationCase.EndsWith('-on')
            $simulationConfig.interval_ms = 10; $simulationConfig.batch_size = 1
        }
        if ($simulationCase -eq 'lower-removal-on') { $simulationConfig.removal = 'lower' }
        if ($simulationCase -eq 'realtime-on') {
            $simulationConfig.interval_ms = 300; $simulationConfig.batch_size = 1
            $simulationConfig.image_width = 1280; $simulationConfig.image_height = 720
        }
        $simulationCaseRoot = Join-Path $simulationOutput $simulationCase
        $simulationConfigPath = Join-Path $simulationOutput "$simulationCase.json"
        $simulationConfig | ConvertTo-Json | Set-Content -LiteralPath $simulationConfigPath -Encoding utf8
        if ($env:DWS_SIMULATION_CAPTURE_STACKS -eq '1' -and $simulationCase.StartsWith('query-stress')) {
            New-Item -ItemType Directory -Path $simulationCaseRoot -Force | Out-Null
            Start-Process -FilePath (Get-Process -Id $PID).Path -WindowStyle Hidden -ArgumentList @(
                '-NoProfile', '-File', (Join-Path $PSScriptRoot 'Capture-QueryHang.ps1'), '-RunRoot', $simulationCaseRoot
            ) -RedirectStandardOutput (Join-Path $simulationCaseRoot 'stack-watcher.log')
        }
        if ($RuntimeDirectory) {
            & $simulationHost $simulationConfigPath $simulationCaseRoot 2>&1 |
                Tee-Object -FilePath (Join-Path $simulationOutput "$simulationCase-console.log")
        } else {
            & dotnet $simulationHost $simulationConfigPath $simulationCaseRoot 2>&1 |
                Tee-Object -FilePath (Join-Path $simulationOutput "$simulationCase-console.log")
        }
        $simulationExit = $LASTEXITCODE
        $simulationReportPath = Join-Path $simulationCaseRoot 'report.json'
        if (-not (Test-Path -LiteralPath $simulationReportPath)) { throw "$simulationCase did not produce a report." }
        $simulationReport = Get-Content -LiteralPath $simulationReportPath -Raw | ConvertFrom-Json
        $simulationCaseSummary = [ordered]@{
            scenario = $simulationCase; passed = $false; runtime_passed = $simulationReport.passed
            offline_audit_passed = $false; pipeline_audit_passed = $false; count = $Count
            accepted = $simulationReport.counts.Packages; seconds = $simulationReport.elapsed_seconds
            peak_working_set_mb = $simulationReport.peak_working_set_mb; report = $simulationReportPath
        }
        $simulationReports.Add($simulationCaseSummary)
        $simulationReports | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $simulationOutput 'summary.json') -Encoding utf8
        if ($simulationExit -ne 0 -or -not $simulationReport.passed) { throw "$simulationCase failed. Inspect $simulationReportPath" }
        & $PythonExecutable -X utf8 (Join-Path $PSScriptRoot 'Audit-Reports.py') $simulationReportPath --output (Join-Path $simulationCaseRoot 'offline-audit.json') > (Join-Path $simulationCaseRoot 'offline-audit-console.log') 2>&1
        if ($LASTEXITCODE -ne 0) { throw "$simulationCase independent event/SQLite/image audit failed." }
        $simulationCaseSummary.offline_audit_passed = $true
        $simulationReports | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $simulationOutput 'summary.json') -Encoding utf8
        & $PythonExecutable -X utf8 (Join-Path $PSScriptRoot 'audit-pipeline.py') $simulationCaseRoot > (Join-Path $simulationCaseRoot 'pipeline-audit-console.log') 2>&1
        if ($LASTEXITCODE -ne 0) { throw "$simulationCase pipeline log correlation audit failed." }
        $simulationCaseSummary.pipeline_audit_passed = $true
        $simulationCaseSummary.passed = $true
        $simulationReports | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $simulationOutput 'summary.json') -Encoding utf8
    }
    Write-Host "All requested scenarios passed. Reports: $simulationOutput"
}
finally {
    [Environment]::SetEnvironmentVariable('DOTNET_PROCESSOR_COUNT', $simulationPreviousProcessorCount, 'Process')
    if ($simulationStarted -and -not $KeepContainers) {
        & docker compose -p jaytom-dws-simulation -f $simulationCompose down
    }
    Pop-Location
}
