<#
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
#>
[CmdletBinding()]
param(
    [string]$EditorExePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($EditorExePath)) {
    $EditorExePath = Join-Path $repositoryRoot 'src\Electron2D.Editor\bin\Release\net10.0\Electron2D.Editor.exe'
}

$resolvedEditorExePath = [System.IO.Path]::GetFullPath($EditorExePath)
if (-not (Test-Path -LiteralPath $resolvedEditorExePath -PathType Leaf)) {
    throw "Electron2D.Editor.exe was not found: $resolvedEditorExePath. Build the editor first, for example: dotnet build src\Electron2D.Editor\Electron2D.Editor.csproj -c Release"
}

$openCommand = '"' + $resolvedEditorExePath + '" "%1"'
$classesRoot = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey('Software\Classes')
if ($null -eq $classesRoot) {
    throw 'Could not open HKCU\Software\Classes.'
}

function Set-RegistryDefaultValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SubKey,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Value
    )

    $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($SubKey)
    if ($null -eq $key) {
        throw "Could not create HKCU:\$SubKey."
    }

    try {
        $key.SetValue('', $Value, [Microsoft.Win32.RegistryValueKind]::String)
    }
    finally {
        $key.Dispose()
    }
}

function Set-RegistryStringValue {
    param(
        [Parameter(Mandatory = $true)]
        [string]$SubKey,

        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [string]$Value
    )

    $key = [Microsoft.Win32.Registry]::CurrentUser.CreateSubKey($SubKey)
    if ($null -eq $key) {
        throw "Could not create HKCU:\$SubKey."
    }

    try {
        $key.SetValue($Name, $Value, [Microsoft.Win32.RegistryValueKind]::String)
    }
    finally {
        $key.Dispose()
    }
}

try {
    Set-RegistryDefaultValue -SubKey 'Software\Classes\.e2d' -Value 'Electron2D.Project'
    Set-RegistryStringValue -SubKey 'Software\Classes\.e2d' -Name 'Content Type' -Value 'application/x-electron2d-project'
    Set-RegistryDefaultValue -SubKey 'Software\Classes\Electron2D.Project' -Value 'Electron2D Project'
    Set-RegistryStringValue -SubKey 'Software\Classes\Electron2D.Project' -Name 'FriendlyTypeName' -Value 'Electron2D Project'
    Set-RegistryDefaultValue -SubKey 'Software\Classes\Electron2D.Project\DefaultIcon' -Value ($resolvedEditorExePath + ',0')
    Set-RegistryDefaultValue -SubKey 'Software\Classes\Electron2D.Project\shell\open\command' -Value $openCommand
    Set-RegistryStringValue -SubKey 'Software\Classes\Applications\Electron2D.Editor.exe' -Name 'ApplicationName' -Value 'Electron2D.Editor'
    Set-RegistryStringValue -SubKey 'Software\Classes\Applications\Electron2D.Editor.exe' -Name 'FriendlyAppName' -Value 'Electron2D.Editor'
    Set-RegistryStringValue -SubKey 'Software\Classes\Applications\Electron2D.Editor.exe\SupportedTypes' -Name '.e2d' -Value ''
    Set-RegistryDefaultValue -SubKey 'Software\Classes\Applications\Electron2D.Editor.exe\shell\open\command' -Value $openCommand
}
finally {
    $classesRoot.Dispose()
}

$signature = @'
using System;
using System.Runtime.InteropServices;

public static class Electron2DFileAssociationShellNotify
{
    [DllImport("shell32.dll")]
    public static extern void SHChangeNotify(int eventId, uint flags, IntPtr item1, IntPtr item2);
}
'@
Add-Type -TypeDefinition $signature -ErrorAction SilentlyContinue
[Electron2DFileAssociationShellNotify]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)

Write-Output 'Electron2D .e2d file association registered'
Write-Output "EditorExePath=$resolvedEditorExePath"
Write-Output 'Extension=.e2d'
Write-Output 'ProgId=Electron2D.Project'
Write-Output "OpenCommand=$openCommand"
