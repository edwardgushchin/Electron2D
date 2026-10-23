$ErrorActionPreference = 'Stop'
$errors = $null; $tokens = $null
$path = Join-Path $PSScriptRoot 'debug_windows.ps1'
$ast = [Management.Automation.Language.Parser]::ParseFile($path, [ref]$tokens, [ref]$errors)
if ($errors.Count) { throw ($errors | Out-String) }
$mapping = $ast.Find({ param($node) $node -is [Management.Automation.Language.AssignmentStatementAst] -and $node.Left.VariablePath.UserPath -eq 'throwObject' }, $true)
if (!$mapping) { throw 'Compiler exception argument mapping is missing.' }
foreach ($case in @(@('arm64', '@x0'), @('x64', '@rcx'), @('x86', 'poi(@esp+4)'))) {
    $Architecture = $case[0]
    . ([scriptblock]::Create($mapping.Extent.Text))
    if ($throwObject -ne $case[1]) { throw "Incorrect $Architecture exception argument location." }
}
$definition = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq 'Invoke-OwnedRuntimeBreak' }, $true)
if (!$definition) { throw 'Owned runtime interruption function is missing.' }
$native = $definition.Find({ param($node) $node -is [Management.Automation.Language.StringConstantExpressionAst] -and $node.Value.Contains('[DllImport') }, $true)
Add-Type ($native.Value.Replace('Electron2DNativeDebug', 'Electron2DNativeDebugSyntax'))
Add-Type @'
using System;
public static class Electron2DNativeDebug {
    public static int Calls;
    public static bool Success = true;
    public static bool DebugBreakProcess(IntPtr handle) {
        if (handle != (IntPtr)42) throw new Exception("Unexpected runtime handle.");
        Calls++; return Success;
    }
}
'@
. ([scriptblock]::Create($definition.Extent.Text))
$script:rows = @(); $script:opened = 0; $script:disposed = 0
$script:module = 'C:\owned\Electron2D.Tests.exe'
function Get-CimInstance {
    [CmdletBinding()] param([string]$ClassName, [string]$Filter)
    if ($ClassName -ne 'Win32_Process' -or $Filter -ne 'ParentProcessId = 123') { throw 'Incorrect child-process query.' }
    $script:rows
}
function Get-Process {
    [CmdletBinding()] param([int]$Id)
    if ($Id -ne 456) { throw 'Incorrect runtime process.' }
    $script:opened++
    $process = [pscustomobject]@{ Id = $Id; Handle = [IntPtr]42; MainModule = [pscustomobject]@{FileName = $script:module} }
    $process | Add-Member ScriptMethod Dispose { $script:disposed++ }
    $process
}
function Check([bool]$Valid, [string]$Message) { if (!$Valid) { throw $Message } }
$debugger = [pscustomobject]@{Id = 123}
$owned = [pscustomobject]@{ParentProcessId = 123; ProcessId = 456; ExecutablePath = $script:module}
$script:rows = @($owned)
Invoke-OwnedRuntimeBreak $debugger $script:module.ToUpperInvariant()
Check ([Electron2DNativeDebug]::Calls -eq 1 -and $script:opened -eq 1 -and $script:disposed -eq 1) 'Exact owned process must be interrupted and released.'
foreach ($rows in @(@(), @($owned, $owned), @([pscustomobject]@{ParentProcessId = 999; ProcessId = 456; ExecutablePath = $script:module}), @([pscustomobject]@{ParentProcessId = 123; ProcessId = 456; ExecutablePath = 'C:\foreign.exe'}))) {
    $script:rows = $rows; $rejected = $false
    try { Invoke-OwnedRuntimeBreak $debugger $script:module } catch { $rejected = $true }
    Check ($rejected -and [Electron2DNativeDebug]::Calls -eq 1 -and $script:opened -eq 1) 'Missing, ambiguous and foreign targets must reject before opening a process.'
}
$script:rows = @($owned); $script:module = 'C:\reused-pid.exe'; $rejected = $false
try { Invoke-OwnedRuntimeBreak $debugger $owned.ExecutablePath } catch { $rejected = $true }
Check ($rejected -and [Electron2DNativeDebug]::Calls -eq 1 -and $script:disposed -eq 2) 'Changed process identity must reject and release the handle.'
$script:module = $owned.ExecutablePath; [Electron2DNativeDebug]::Success = $false; $rejected = $false
try { Invoke-OwnedRuntimeBreak $debugger $script:module } catch { $rejected = $true }
Check ($rejected -and [Electron2DNativeDebug]::Calls -eq 2 -and $script:disposed -eq 3) 'Native interruption failure must propagate and release the handle.'
Write-Output 'Owned native interruption: syntax, exact identity, missing/ambiguous/foreign/reused targets, native failure and disposal passed.'
