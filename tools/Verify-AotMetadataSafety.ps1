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
param(
    [switch]$NativeAot,
    [string]$RuntimeIdentifier = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'tests/Electron2D.Tests.AotSmoke/Electron2D.Tests.AotSmoke.csproj'
$publishRoot = Join-Path $repoRoot '.temp/aot-metadata-smoke'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'AOT metadata smoke project was not found.'
}

function Get-CurrentRuntimeIdentifier {
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        if ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
            return 'win-arm64'
        }

        return 'win-x64'
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::OSX)) {
        if ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
            return 'osx-arm64'
        }

        return 'osx-x64'
    }

    if ([System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) {
        return 'linux-arm64'
    }

    return 'linux-x64'
}

function Invoke-SmokeExecutable([string]$directory) {
    $name = 'Electron2D.Tests.AotSmoke'
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $name += '.exe'
    }

    $executable = Join-Path $directory $name
    if (-not (Test-Path -LiteralPath $executable)) {
        throw "Published smoke executable was not found: $executable"
    }

    & $executable
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
    $RuntimeIdentifier = Get-CurrentRuntimeIdentifier
}

Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue

$trimmedOutput = Join-Path $publishRoot 'trimmed'
dotnet publish $projectPath `
    --configuration Release `
    --runtime $RuntimeIdentifier `
    --self-contained true `
    --output $trimmedOutput `
    -p:PublishTrimmed=true `
    -p:TrimMode=partial
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Invoke-SmokeExecutable $trimmedOutput

if ($NativeAot) {
    $nativeOutput = Join-Path $publishRoot "nativeaot-$RuntimeIdentifier"
    dotnet publish $projectPath `
        --configuration Release `
        --runtime $RuntimeIdentifier `
        --self-contained true `
        --output $nativeOutput `
        -p:PublishAot=true
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    Invoke-SmokeExecutable $nativeOutput
}

Write-Host "AOT metadata safety verification passed. RuntimeIdentifier: $RuntimeIdentifier. NativeAot: $NativeAot."
