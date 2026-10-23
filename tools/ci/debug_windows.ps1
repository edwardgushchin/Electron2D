param([Parameter(Mandatory)][string]$Executable, [Parameter(Mandatory)][string]$Output,
      [ValidateSet('arm64', 'x64', 'x86')][string]$Architecture = 'arm64')
$ErrorActionPreference = 'Stop'
function Invoke-OwnedRuntimeBreak($Debugger, [string]$Executable) {
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $($Debugger.Id)" -ErrorAction Stop |
        Where-Object { $_.ParentProcessId -eq $Debugger.Id -and [string]::Equals($_.ExecutablePath, $Executable, [StringComparison]::OrdinalIgnoreCase) })
    if ($children.Count -ne 1) { throw 'Cannot identify one owned runtime to interrupt.' }
    $runtime = Get-Process -Id $children[0].ProcessId -ErrorAction Stop
    try {
        if (![string]::Equals($runtime.MainModule.FileName, $Executable, [StringComparison]::OrdinalIgnoreCase)) {
            throw 'Owned runtime identity changed before interruption.'
        }
        if (!("Electron2DNativeDebug" -as [type])) {
            Add-Type @'
using System;
using System.Runtime.InteropServices;
public static class Electron2DNativeDebug {
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DebugBreakProcess(IntPtr process);
}
'@
        }
        if (![Electron2DNativeDebug]::DebugBreakProcess($runtime.Handle)) {
            throw "Owned runtime interruption failed: $([Runtime.InteropServices.Marshal]::GetLastWin32Error())."
        }
        Write-Host "Interrupted owned runtime $($runtime.Id) for all-thread stacks."
    }
    finally { $runtime.Dispose() }
}
$Executable = (Resolve-Path $Executable).Path
New-Item -ItemType Directory -Force $Output | Out-Null
$Output = (Resolve-Path $Output).Path
$cdb = "${env:ProgramFiles(x86)}\Windows Kits\10\Debuggers\$Architecture\cdb.exe"
if (!(Test-Path $cdb)) {
    $sdk = Get-ItemProperty 'HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall\*', 'HKLM:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*' -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName -like 'Windows Software Development Kit*' -and $_.BundleCachePath } |
        Sort-Object DisplayVersion -Descending | Select-Object -First 1
    $installer = $sdk.BundleCachePath
    if (!$installer -or !(Test-Path $installer)) {
        $installer = Join-Path $env:RUNNER_TEMP 'winsdksetup-debuggers.exe'
        Invoke-WebRequest 'https://go.microsoft.com/fwlink/?linkid=2349110' -OutFile $installer
    }
    $signature = Get-AuthenticodeSignature $installer
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch 'O=Microsoft Corporation') { throw 'Windows SDK installer must have a valid Microsoft signature.' }
    $setup = Start-Process -FilePath $installer -ArgumentList '/features OptionId.WindowsDesktopDebuggers /q /norestart' -PassThru
    if (!$setup.WaitForExit(360000)) { Stop-Process -Id $setup.Id -Force; throw 'Debugger installation deadline.' }
    if ($setup.ExitCode -notin 0, 3010) { throw "Debugger installation failed: $($setup.ExitCode)." }
}
if (!(Test-Path $cdb)) { throw "Native $Architecture CDB is unavailable." }
# Keep only exception context, symbolic stacks and module identities, never a heap/key dump.
$commands = Join-Path $Output 'commands.txt'
@'
sxe -c ".ecxr; k; lm; q" 0xc0000374
sxe -c "~* k; lm; q" bpe
sxe -c ".ecxr; k; lm; q" av
sxd -c2 ".ecxr; k; lm; q" ii
sxd -c2 ".ecxr; k; lm; q" clr
sxd -c2 ".ecxr; k; lm; q" sbo
g
'@ | Set-Content -Encoding ascii $commands
$log = Join-Path $Output 'runtime-cdb.log'
$debugger = Start-Process -FilePath $cdb -ArgumentList @('-G', '-cf', "`"$commands`"", '-logo', "`"$log`"", "`"$Executable`"") -PassThru -NoNewWindow
$watch = [Diagnostics.Stopwatch]::StartNew()
try {
    if (!$debugger.WaitForExit(360000)) {
        if (!$debugger.HasExited) { Invoke-OwnedRuntimeBreak $debugger $Executable }
        $remaining = [int][Math]::Max(1, 480000 - $watch.ElapsedMilliseconds)
        if (!$debugger.WaitForExit($remaining)) { throw 'Native runtime debugger deadline.' }
    }
}
finally { if (!$debugger.HasExited) { & taskkill /PID $debugger.Id /T /F } }
if (!(Test-Path $log)) { throw 'CDB produced no diagnostic log.' }
Get-Content $log
