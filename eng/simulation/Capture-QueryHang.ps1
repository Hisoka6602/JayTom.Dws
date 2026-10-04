param([Parameter(Mandatory)][string]$RunRoot)
$ErrorActionPreference = 'Stop'
$taskTool = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../output/simulation/tools/dotnet-dump.exe'))
$taskTimer = [Diagnostics.Stopwatch]::StartNew()
$taskCaptured = 0
$taskMap = $null
$taskView = $null
try {
    while ($taskTimer.Elapsed.TotalSeconds -lt 120) {
        $taskPidFile = Join-Path $RunRoot 'runtime.pid'
        if (Test-Path -LiteralPath (Join-Path $RunRoot 'report.json')) { break }
        if (-not (Test-Path -LiteralPath $taskPidFile)) { Start-Sleep -Milliseconds 100; continue }
        $taskRuntimePid = [int](Get-Content -LiteralPath $taskPidFile -Raw)
        if (-not $taskMap) {
            $taskMap = [IO.MemoryMappedFiles.MemoryMappedFile]::OpenExisting("DwsQuerySimulationHeartbeat-$taskRuntimePid", [IO.MemoryMappedFiles.MemoryMappedFileRights]::Read)
            $taskView = $taskMap.CreateViewAccessor(0, 16, [IO.MemoryMappedFiles.MemoryMappedFileAccess]::Read)
        }
        $taskLastBeat = $taskView.ReadInt64(0)
        $taskGap = ([Diagnostics.Stopwatch]::GetTimestamp() - $taskLastBeat) * 1000.0 / [Diagnostics.Stopwatch]::Frequency
        if ($taskLastBeat -ne 0 -and $taskGap -gt 500 -and $taskCaptured -lt 1) {
            $taskCaptured++
            $taskOutput = Join-Path $RunRoot "hang-stack-$taskCaptured.txt"
            Write-Host "Capturing stack $taskCaptured at gap $([int]$taskGap)ms"
            & $taskTool collect --process-id $taskRuntimePid --type Mini --output (Join-Path $RunRoot 'query-hang-mini.dmp') *> $taskOutput
            Write-Host "Diagnostic tool exited $LASTEXITCODE"
            Start-Sleep -Milliseconds 1000
        } else { Start-Sleep -Milliseconds 80 }
    }
} finally {
    if ($taskView) { $taskView.Dispose() }
    if ($taskMap) { $taskMap.Dispose() }
}
