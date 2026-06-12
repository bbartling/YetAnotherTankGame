param(
    [string]$UnityEditorPath = $env:UNITY_EDITOR_PATH
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$logs = Join-Path $repo "Logs"
New-Item -ItemType Directory -Force -Path $logs | Out-Null

if (-not $UnityEditorPath) {
    $versionLine = Get-Content (Join-Path $repo "ProjectSettings/ProjectVersion.txt") |
        Where-Object { $_ -like "m_EditorVersion:*" } |
        Select-Object -First 1
    $version = ($versionLine -split ":", 2)[1].Trim()
    $candidate = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path $candidate) {
        $UnityEditorPath = $candidate
    }
}

if (-not $UnityEditorPath -or -not (Test-Path $UnityEditorPath)) {
    throw "Unity Editor was not found. Set UNITY_EDITOR_PATH or pass -UnityEditorPath."
}

function Invoke-PatientUnity {
    param(
        [string]$Name,
        [string[]]$Arguments
    )

    $log = Join-Path $logs "$Name.log"
    $process = Start-Process -FilePath $UnityEditorPath -ArgumentList $Arguments -PassThru -WindowStyle Hidden
    while (-not $process.HasExited) {
        try {
            Wait-Process -Id $process.Id -Timeout 1200 -ErrorAction Stop
        }
        catch {
            Write-Host "$Name is still running after a 20-minute wait cycle. Inspecting the latest log lines."
            if (Test-Path $log) {
                Get-Content $log -Tail 40
            }
            $process.Refresh()
        }
    }

    if ($process.ExitCode -ne 0) {
        if (Test-Path $log) {
            Get-Content $log -Tail 120
        }
        throw "$Name failed with exit code $($process.ExitCode)."
    }
}

$testResults = Join-Path $logs "EditModeResults.xml"
Invoke-PatientUnity -Name "webgl-tests" -Arguments @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", $repo,
    "-runTests", "-testPlatform", "EditMode",
    "-testResults", $testResults,
    "-logFile", (Join-Path $logs "webgl-tests.log")
)

Invoke-PatientUnity -Name "webgl-build" -Arguments @(
    "-batchmode", "-nographics", "-quit",
    "-projectPath", $repo,
    "-executeMethod", "TankWebGLBuildPipeline.BuildFromCommandLine",
    "-logFile", (Join-Path $logs "webgl-build.log")
)

$zip = Join-Path $repo "tank_game_pythonanywhere.zip"
if (-not (Test-Path $zip)) {
    throw "Expected release ZIP was not created: $zip"
}

$item = Get-Item $zip
Write-Host "PythonAnywhere artifact: $($item.FullName)"
Write-Host "Artifact size: $($item.Length) bytes"

