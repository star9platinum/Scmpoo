param(
    [string]$Executable = "$PSScriptRoot/../build/native-x64/Scmpoo/Release/Scmpoo.exe",
    [int]$Seconds = 12
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class NativeStress {
    public delegate bool EnumProc(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback, IntPtr data);
    [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern IntPtr GetDlgItem(IntPtr window, int id);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern bool SetWindowText(IntPtr window, string text);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] public static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr window, uint message, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] public static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr w, IntPtr l, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageTimeoutW")] public static extern IntPtr SendText(IntPtr window, uint message, IntPtr w, StringBuilder text, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll", CharSet=CharSet.Unicode, EntryPoint="SendMessageTimeoutW")] public static extern IntPtr SetTextMessage(IntPtr window, uint message, IntPtr w, string text, uint flags, uint timeout, out IntPtr result);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr window, int show);
    public static IntPtr[] Windows(int[] pids, string className) {
        var found = new List<IntPtr>();
        EnumWindows(delegate(IntPtr w, IntPtr d) {
            uint pid; GetWindowThreadProcessId(w, out pid);
            if (Array.IndexOf(pids, (int)pid) >= 0) {
                var text = new StringBuilder(64); GetClassName(w, text, text.Capacity);
                if (text.ToString() == className) found.Add(w);
            }
            return true;
        }, IntPtr.Zero);
        return found.ToArray();
    }
    public static long Send(IntPtr w, uint message, int value, int other) {
        IntPtr result;
        if (SendMessageTimeout(w, message, new IntPtr(value), new IntPtr(other), 2, 1000, out result) == IntPtr.Zero)
            throw new Exception("Unresponsive test window " + w);
        return result.ToInt64();
    }
    public static string Text(IntPtr window) {
        var text = new StringBuilder(128); IntPtr result;
        if (SendText(window, 13, new IntPtr(text.Capacity), text, 2, 1000, out result) == IntPtr.Zero)
            throw new Exception("Cannot read settings control");
        return text.ToString();
    }
    public static void SetText(IntPtr window, string text) {
        IntPtr result;
        if (SetTextMessage(window, 12, IntPtr.Zero, text, 2, 1000, out result) == IntPtr.Zero)
            throw new Exception("Cannot update settings control");
    }
}
'@
$exe = (Resolve-Path -LiteralPath $Executable).Path
if (Get-Process Scmpoo -ErrorAction SilentlyContinue) { throw 'Close existing Scmpoo instances before the isolated stress test.' }
$settings = Join-Path $env:LOCALAPPDATA 'Scmpoo/settings.ini'
$original = if (Test-Path -LiteralPath $settings) { [IO.File]::ReadAllBytes($settings) } else { $null }
$processes = [Collections.Generic.List[Diagnostics.Process]]::new()
try {
    for ($i = 0; $i -lt 32; $i++) {
        $processes.Add((Start-Process -FilePath $exe -WindowStyle Hidden -PassThru))
    }
    $ids = [int[]]$processes.Id
    $deadline = [DateTime]::UtcNow.AddSeconds(20)
    do {
        Start-Sleep -Milliseconds 200
        $windows = [NativeStress]::Windows($ids, 'ScreenMatePoo')
    } while ($windows.Count -lt 32 -and [DateTime]::UtcNow -lt $deadline)
    if ($windows.Count -ne 32) { throw "Expected 32 sheep, got $($windows.Count)." }
    foreach ($window in $windows) { [void][NativeStress]::PostMessage($window, 1026, [IntPtr]::Zero, [IntPtr]::Zero) }
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 200
        $dialogs = [NativeStress]::Windows($ids, '#32770')
    } while ($dialogs.Count -lt 32 -and [DateTime]::UtcNow -lt $deadline)
    if ($dialogs.Count -ne 32) { throw "Expected 32 settings dialogs, got $($dialogs.Count)." }
    foreach ($dialog in $dialogs) { [void][NativeStress]::ShowWindow($dialog, 0) }
    [NativeStress]::SetText([NativeStress]::GetDlgItem($dialogs[0], 1008), '175')
    [NativeStress]::SetText([NativeStress]::GetDlgItem($dialogs[0], 1011), '350')
    [NativeStress]::SetText([NativeStress]::GetDlgItem($dialogs[0], 1007), 'Flock regression')
    [void][NativeStress]::Send([NativeStress]::GetDlgItem($dialogs[0], 1003), 241, 1, 0)
    [void][NativeStress]::Send($dialogs[0], 273, 1013, 0)
    Start-Sleep -Milliseconds 800
    foreach ($dialog in $dialogs) {
        if ([NativeStress]::Text([NativeStress]::GetDlgItem($dialog, 1008)) -ne '175' -or
            [NativeStress]::Text([NativeStress]::GetDlgItem($dialog, 1011)) -ne '350' -or
            [NativeStress]::Text([NativeStress]::GetDlgItem($dialog, 1007)) -ne 'Flock regression') {
            $actualSpeed = [NativeStress]::Text([NativeStress]::GetDlgItem($dialog, 1008))
            $actualFrequency = [NativeStress]::Text([NativeStress]::GetDlgItem($dialog, 1011))
            $actualOwner = [NativeStress]::Text([NativeStress]::GetDlgItem($dialog, 1007))
            throw "Apply-all mismatch: speed=$actualSpeed frequency=$actualFrequency owner=$actualOwner; file=$([IO.File]::ReadAllText($settings))"
        }
        [void][NativeStress]::Send($dialog, 273, 2, 0)
    }
    $cpuStart = ($processes | ForEach-Object { $_.Refresh(); $_.TotalProcessorTime.TotalMilliseconds } | Measure-Object -Sum).Sum
    $timer = [Diagnostics.Stopwatch]::StartNew()
    $samples = 0
    while ($timer.Elapsed.TotalSeconds -lt $Seconds) {
        foreach ($window in $windows) { [void][NativeStress]::Send($window, 0, 0, 0) }
        if ($samples -eq 1 -or $samples -eq 6) {
            foreach ($window in $windows) { [void][NativeStress]::PostMessage($window, 273, [IntPtr]1102, [IntPtr]::Zero) }
        }
        Start-Sleep -Milliseconds 250
        $samples++
    }
    $cpuEnd = ($processes | ForEach-Object { $_.Refresh(); $_.TotalProcessorTime.TotalMilliseconds } | Measure-Object -Sum).Sum
    $workingSet = ($processes | ForEach-Object { $_.WorkingSet64 } | Measure-Object -Sum).Sum
    [pscustomobject]@{
        Instances = $windows.Count; SettingsUpdated = $dialogs.Count; ResponsiveSamples = $samples
        CpuMilliseconds = [Math]::Round($cpuEnd - $cpuStart)
        ElapsedMilliseconds = $timer.ElapsedMilliseconds
        CpuPercentOfOneCore = [Math]::Round(($cpuEnd - $cpuStart) / $timer.Elapsed.TotalMilliseconds * 100, 2)
        AggregateWorkingSetMiB = [Math]::Round($workingSet / 1MB, 1)
    } | ConvertTo-Json
} finally {
    if ($processes.Count) {
        $windows = [NativeStress]::Windows([int[]]$processes.Id, 'ScreenMatePoo')
        foreach ($window in $windows) { [void][NativeStress]::PostMessage($window, 16, [IntPtr]::Zero, [IntPtr]::Zero) }
        foreach ($process in $processes) {
            if (-not $process.WaitForExit(3000)) { $process.Kill(); $process.WaitForExit() }
            $process.Dispose()
        }
    }
    if ($null -ne $original) { [IO.File]::WriteAllBytes($settings, $original) }
    elseif (Test-Path -LiteralPath $settings) { Remove-Item -LiteralPath $settings }
}
