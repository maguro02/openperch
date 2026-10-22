<#
.SYNOPSIS
Runs the Unity test suites that CI cannot run, on the Windows development machine.

.DESCRIPTION
Suites:
  app-editmode, app-playmode   app/ on Unity 2022.3.62f3
  harness-editmode             fixtures/ExporterHarness on Unity 2022.3.22f1
  harness-6000.3-compile       fixtures/ExporterHarness compiled on Unity 6000.3 (skipped when no 6000.3 is installed)

Each editor comes from an environment variable (UNITY_2022_3_62F3, UNITY_2022_3_22F1, UNITY_6000_3) holding either
Unity.exe or the folder that contains it, or else from Unity Hub's default install folder. A variable that is set must
name an editor; the suites that need it fail otherwise.
NUnit XML results and editor logs are written to out/test-results/. The exit code is non-zero when a suite fails or a
required editor is missing.

Works with Windows PowerShell 5.1 and PowerShell 7. From Git Bash:
  powershell -ExecutionPolicy Bypass -File tools/run-unity-tests.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

$Root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$Out = Join-Path $Root 'out\test-results'
$HubEditors = Join-Path $env:ProgramFiles 'Unity\Hub\Editor'
$script:Status = 0
$script:Summary = New-Object System.Collections.Generic.List[string]

function Add-Result([string]$Name, [string]$Text) {
    $script:Summary.Add(('{0,-24} {1}' -f $Name, $Text))
}

function Add-Failure([string]$Name, [string]$Reason) {
    Add-Result $Name ('FAIL: ' + $Reason)
    $script:Status = 1
}

# Accepts Unity.exe itself or a folder holding it, either directly or under Editor\ as Unity Hub lays it out.
function Get-UnityBinary([string]$Candidate) {
    if ([string]::IsNullOrWhiteSpace($Candidate)) { return $null }
    $paths = @($Candidate, (Join-Path $Candidate 'Unity.exe'), (Join-Path $Candidate 'Editor\Unity.exe'))
    foreach ($p in $paths) {
        if ((Test-Path -LiteralPath $p -PathType Leaf) -and ($p -like '*.exe')) { return (Resolve-Path -LiteralPath $p).Path }
    }
    return $null
}

# Returns @{ Path; Problem }: the editor named by the variable, or, when the variable is not set, the one of that
# version in Unity Hub's default location.
function Resolve-Unity([string]$Variable, [string]$Version) {
    $override = [Environment]::GetEnvironmentVariable($Variable)
    if (-not [string]::IsNullOrEmpty($override)) {
        $binary = Get-UnityBinary $override
        if ($binary) { return @{ Path = $binary; Problem = $null } }
        return @{ Path = $null; Problem = "$Variable is set but is not a Unity editor: $override" }
    }
    $binary = Get-UnityBinary (Join-Path $HubEditors $Version)
    if ($binary) { return @{ Path = $binary; Problem = $null } }
    return @{ Path = $null; Problem = "Unity $Version not found; install it with Unity Hub or set $Variable" }
}

# Any 6000.3 patch in Unity Hub's default location will do for a compile check.
function Find-HubUnity60003 {
    if (-not (Test-Path -LiteralPath $HubEditors)) { return $null }
    $found = Get-ChildItem -LiteralPath $HubEditors -Directory -Filter '6000.3.*' | Sort-Object Name | Select-Object -Last 1
    if (-not $found) { return $null }
    return Get-UnityBinary $found.FullName
}

function Format-Argument([string]$Value) {
    return '"' + $Value.Replace('"', '\"') + '"'
}

# Unity.exe is a GUI-subsystem program, so the call operator would not wait for it. Start-Process -Wait is not used
# either: it also waits for child processes such as the licensing client, which can outlive the editor.
function Invoke-Unity([string]$Unity, [string[]]$Arguments) {
    $info = New-Object System.Diagnostics.ProcessStartInfo
    $info.FileName = $Unity
    $info.Arguments = $Arguments -join ' '
    $info.UseShellExecute = $false
    $process = [System.Diagnostics.Process]::Start($info)
    $process.WaitForExit()
    return $process.ExitCode
}

function Invoke-Tests([string]$Name, [string]$Unity, [string]$Project, [string]$Platform) {
    if (-not (Test-Path -LiteralPath $Project -PathType Container)) {
        Add-Failure $Name "project not found: $Project"
        return
    }
    $results = Join-Path $Out "$Name.xml"
    $log = Join-Path $Out "$Name.log"
    Remove-Item -LiteralPath $results -Force -ErrorAction SilentlyContinue
    Write-Host "==> ${Name}: $Platform tests of $Project"
    # -runTests quits the editor by itself; adding -quit would stop it before the tests run.
    $code = Invoke-Unity $Unity @('-batchmode', '-projectPath', (Format-Argument $Project), '-runTests',
        '-testPlatform', $Platform, '-testResults', (Format-Argument $results), '-logFile', (Format-Argument $log))
    if ($code -eq 0 -and (Test-Path -LiteralPath $results -PathType Leaf)) {
        Add-Result $Name 'PASS'
    } else {
        Add-Failure $Name "exit code $code, see $log"
    }
}

function Remove-Tree([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return }
    Remove-Item -LiteralPath $Path -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path -LiteralPath $Path) {
        # Library paths can exceed MAX_PATH, which Windows PowerShell 5.1 cannot delete.
        & cmd.exe /d /c "rmdir /s /q `"$Path`"" | Out-Null
    }
}

function Copy-Tree([string]$Source, [string]$Destination, [string[]]$ExcludeDirectories) {
    $arguments = @($Source, $Destination, '/E', '/NFL', '/NDL', '/NJH', '/NJS', '/NP')
    if ($ExcludeDirectories.Count -gt 0) { $arguments += '/XD'; $arguments += $ExcludeDirectories }
    & robocopy.exe @arguments | Out-Null
    # robocopy reports success with exit codes 0-7.
    if ($LASTEXITCODE -ge 8) { throw "robocopy failed with exit code $LASTEXITCODE copying $Source" }
}

function Invoke-CompileCheck([string]$Name, [string]$Unity, [string]$ProjectRelative) {
    $project = Join-Path $Root $ProjectRelative
    if (-not (Test-Path -LiteralPath $project -PathType Container)) {
        Add-Failure $Name "project not found: $project"
        return
    }
    $log = Join-Path $Out "$Name.log"
    $work = Join-Path ([System.IO.Path]::GetTempPath()) ('openperch-compile-' + [guid]::NewGuid().ToString('N'))
    try {
        # A newer editor upgrades the project it opens, so it gets a copy. packages\ is copied next to it so that the
        # manifest's relative file: paths still resolve.
        $copy = Join-Path $work $ProjectRelative
        $excluded = @('Library', 'Temp', 'Logs', 'obj', 'UserSettings') | ForEach-Object { Join-Path $project $_ }
        Copy-Tree $project $copy $excluded
        Copy-Tree (Join-Path $Root 'packages') (Join-Path $work 'packages') @()
        Write-Host "==> ${Name}: compiling $ProjectRelative"
        $code = Invoke-Unity $Unity @('-batchmode', '-nographics', '-quit', '-projectPath', (Format-Argument $copy),
            '-logFile', (Format-Argument $log))
        if ($code -eq 0) { Add-Result $Name 'PASS' } else { Add-Failure $Name "exit code $code, see $log" }
    } catch {
        Add-Failure $Name $_.Exception.Message
    } finally {
        # The copy grows a Library of several GB.
        Remove-Tree $work
    }
}

New-Item -ItemType Directory -Force -Path $Out | Out-Null

$u62 = Resolve-Unity 'UNITY_2022_3_62F3' '2022.3.62f3'
if ($u62.Path) {
    Invoke-Tests 'app-editmode' $u62.Path (Join-Path $Root 'app') 'EditMode'
    Invoke-Tests 'app-playmode' $u62.Path (Join-Path $Root 'app') 'PlayMode'
} else {
    Add-Failure 'app-editmode' $u62.Problem
    Add-Failure 'app-playmode' $u62.Problem
}

$u22 = Resolve-Unity 'UNITY_2022_3_22F1' '2022.3.22f1'
if ($u22.Path) {
    Invoke-Tests 'harness-editmode' $u22.Path (Join-Path $Root 'fixtures\ExporterHarness') 'EditMode'
} else {
    Add-Failure 'harness-editmode' $u22.Problem
}

# Unlike the 2022.3 editors this one is optional, but an editor asked for by UNITY_6000_3 has to exist.
$requested6000 = [Environment]::GetEnvironmentVariable('UNITY_6000_3')
if (-not [string]::IsNullOrEmpty($requested6000)) {
    $u6000 = Get-UnityBinary $requested6000
    if ($u6000) {
        Invoke-CompileCheck 'harness-6000.3-compile' $u6000 'fixtures\ExporterHarness'
    } else {
        Add-Failure 'harness-6000.3-compile' "UNITY_6000_3 is set but is not a Unity editor: $requested6000"
    }
} else {
    $u6000 = Find-HubUnity60003
    if ($u6000) {
        Invoke-CompileCheck 'harness-6000.3-compile' $u6000 'fixtures\ExporterHarness'
    } else {
        Add-Result 'harness-6000.3-compile' 'SKIP: no Unity 6000.3 found; install one or set UNITY_6000_3'
    }
}

Write-Host ''
Write-Host "Results and logs: $Out"
foreach ($line in $script:Summary) { Write-Host "  $line" }
exit $script:Status
