param([Parameter(Mandatory)][string]$Executable, [Parameter(Mandatory)][string]$Output)
$ErrorActionPreference = 'Stop'
$Executable = (Resolve-Path $Executable).Path
New-Item -ItemType Directory -Force $Output | Out-Null
$Output = (Resolve-Path $Output).Path
$cdb = "${env:ProgramFiles(x86)}\Windows Kits\10\Debuggers\arm64\cdb.exe"
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
if (!(Test-Path $cdb)) { throw 'Native ARM64 CDB is unavailable.' }
# Keep only exception context, symbolic stacks and module identities, never a heap/key dump.
$commands = Join-Path $Output 'commands.txt'
@'
sxd -c2 ".ecxr; k; lm; q" av
sxd -c2 ".ecxr; k; lm; q" ii
sxd -c2 ".ecxr; k; lm; q" clr
sxd -c2 ".ecxr; k; lm; q" sbo
sxd -c2 ".ecxr; k; lm; q" *
g
'@ | Set-Content -Encoding ascii $commands
$log = Join-Path $Output 'dtls-cdb.log'
$debugger = Start-Process -FilePath $cdb -ArgumentList @('-G', '-cf', "`"$commands`"", '-logo', "`"$log`"", "`"$Executable`"") -PassThru -NoNewWindow
if (!$debugger.WaitForExit(120000)) { & taskkill /PID $debugger.Id /T /F; throw 'Native DTLS debugger deadline.' }
if (!(Test-Path $log)) { throw 'CDB produced no diagnostic log.' }
Get-Content $log
