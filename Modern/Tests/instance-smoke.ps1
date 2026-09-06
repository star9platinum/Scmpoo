param(
    [string]$Executable = (Join-Path $PSScriptRoot '..\..\artifacts\modern-win-x64\Scmpoo.Modern.exe'),
    [string]$OutputDirectory = (Join-Path $PSScriptRoot '..\..\artifacts\modern-instance-smoke')
)

$ErrorActionPreference = 'Stop'
$executablePath = (Resolve-Path -LiteralPath $Executable).Path
$reportDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

if (-not ('ScmpooInstanceSmoke.Native' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace ScmpooInstanceSmoke
{
    public sealed class WindowRecord
    {
        public IntPtr Handle;
        public uint ProcessId;
        public string Title;
    }

    public static class Native
    {
        private delegate bool EnumWindowsCallback(IntPtr window, IntPtr parameter);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr window, StringBuilder text, int maximum);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern IntPtr FindWindow(string className, string title);

        public static WindowRecord[] Snapshot(int targetProcessId)
        {
            List<WindowRecord> windows = new List<WindowRecord>();
            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                uint processId;
                GetWindowThreadProcessId(window, out processId);
                if (processId == targetProcessId)
                {
                    StringBuilder title = new StringBuilder(256);
                    GetWindowText(window, title, title.Capacity);
                    windows.Add(new WindowRecord { Handle = window, ProcessId = processId, Title = title.ToString() });
                }
                return true;
            }, IntPtr.Zero);
            return windows.ToArray();
        }
    }
}
'@
}

# Refuse to send test commands to a flock owned by the user or another test.
$existingMutex = $null
try { $existingMutex = [System.Threading.Mutex]::OpenExisting('Scmpoo.Modern.Flock.v1') }
catch [System.Threading.WaitHandleCannotBeOpenedException] { }
if ($null -ne $existingMutex) {
    $existingMutex.Dispose()
    throw 'A modern flock already exists. Close it before running the instance smoke test.'
}
if ([ScmpooInstanceSmoke.Native]::FindWindow($null, 'Scmpoo.Modern.Flock.Controller.v1') -ne [IntPtr]::Zero) {
    throw 'A flock controller already exists. No test process was started.'
}

function Wait-Condition {
    param([scriptblock]$Condition, [string]$Failure)
    $wait = [System.Diagnostics.Stopwatch]::StartNew()
    while ($wait.ElapsedMilliseconds -lt 10000) {
        if (& $Condition) { return }
        Start-Sleep -Milliseconds 50
    }
    throw $Failure
}

function Main-WindowCount {
    param([int]$TargetProcessId)
    return @([ScmpooInstanceSmoke.Native]::Snapshot($TargetProcessId) | Where-Object { $_.Title -eq 'Scmpoo Modern' }).Count
}

$null = New-Item -ItemType Directory -Path $reportDirectory -Force
$ownedProcesses = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()
$elapsed = [System.Diagnostics.Stopwatch]::StartNew()
$report = [ordered]@{
    Passed = $false
    Executable = $executablePath
    InitialCount = 0
    ForwardedAddition = 3
    FinalCount = 0
    SettingsWindows = 0
    ForwarderExitCodes = @()
    CleanupCompleted = $false
}

try {
    $initial = Start-Process -FilePath $executablePath -ArgumentList '--count 2' -WindowStyle Hidden -PassThru
    $ownedProcesses.Add($initial)
    Wait-Condition { -not $initial.HasExited -and (Main-WindowCount $initial.Id) -eq 2 } 'The first process did not create two main windows.'
    $report.InitialProcessId = $initial.Id
    $report.InitialCount = Main-WindowCount $initial.Id

    $addition = Start-Process -FilePath $executablePath -ArgumentList '--count 3' -WindowStyle Hidden -PassThru
    $ownedProcesses.Add($addition)
    if (-not $addition.WaitForExit(10000)) { throw 'The count forwarder did not exit.' }
    if ($addition.ExitCode -ne 0) { throw "The count forwarder exited with code $($addition.ExitCode)." }
    Wait-Condition { -not $initial.HasExited -and (Main-WindowCount $initial.Id) -eq 5 } 'The first process did not receive the forwarded addition of three sheep.'
    $report.FinalCount = Main-WindowCount $initial.Id
    $report.ForwarderExitCodes += $addition.ExitCode
    if ([ScmpooInstanceSmoke.Native]::Snapshot($addition.Id).Length -ne 0) { throw 'The count forwarder retained a window.' }

    $settings = Start-Process -FilePath $executablePath -ArgumentList '--settings' -WindowStyle Hidden -PassThru
    $ownedProcesses.Add($settings)
    if (-not $settings.WaitForExit(10000)) { throw 'The settings forwarder did not exit.' }
    if ($settings.ExitCode -ne 0) { throw "The settings forwarder exited with code $($settings.ExitCode)." }
    Wait-Condition {
        @([ScmpooInstanceSmoke.Native]::Snapshot($initial.Id) | Where-Object { $_.Title -eq '小羊设置' }).Count -eq 1
    } 'The original process did not open exactly one settings window.'
    $report.SettingsWindows = @([ScmpooInstanceSmoke.Native]::Snapshot($initial.Id) | Where-Object { $_.Title -eq '小羊设置' }).Count
    $report.ForwarderExitCodes += $settings.ExitCode
    if ((Main-WindowCount $initial.Id) -ne 5) { throw 'Opening settings unexpectedly changed the flock size.' }
    if ([ScmpooInstanceSmoke.Native]::Snapshot($settings.Id).Length -ne 0) { throw 'The settings forwarder retained a window.' }
    $report.Passed = $true
}
catch {
    $report.Error = $_.Exception.Message
    throw
}
finally {
    # Only terminate handles returned by our own Start-Process calls.
    foreach ($process in $ownedProcesses) {
        try {
            if (-not $process.HasExited) {
                $process.Kill()
                if (-not $process.WaitForExit(5000)) { throw 'An owned test process did not terminate.' }
            }
        }
        finally { $process.Dispose() }
    }
    $elapsed.Stop()
    $report.CleanupCompleted = $true
    $report.ElapsedMilliseconds = $elapsed.ElapsedMilliseconds
    $json = $report | ConvertTo-Json -Depth 3
    [System.IO.File]::WriteAllText((Join-Path $reportDirectory 'instance-smoke.json'), $json, [System.Text.UTF8Encoding]::new($false))
}

Write-Output 'PASS: one process owns 2 -> 5 sheep; both forwarded launchers exit 0; --settings opens one dialog in the original process; owned processes cleaned up.'
