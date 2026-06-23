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
    [string]$RuntimeIdentifier = '',
    [int]$WarmupTicks = 60,
    [int]$Ticks = 240
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'tests/Electron2D.Tests.PhysicsBox2DSmoke/Electron2D.Tests.PhysicsBox2DSmoke.csproj'
$specPath = Join-Path $repoRoot 'docs/physics/box2d-net-validation.md'
$docPath = Join-Path $repoRoot 'docs/physics/box2d-net-validation.md'
$publishRoot = Join-Path $repoRoot '.temp/box2d-physics-candidate'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Box2D.NET smoke project was not found: $projectPath"
}

foreach ($path in @($specPath, $docPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Box2D.NET validation document was not found: $path"
    }
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

function Invoke-SmokeOutput([string[]]$CommandLine) {
    $output = & $CommandLine[0] $CommandLine[1..($CommandLine.Length - 1)] 2>&1
    if ($LASTEXITCODE -ne 0) {
        $output | ForEach-Object { Write-Host $_ }
        exit $LASTEXITCODE
    }

    $text = $output -join "`n"
    if ($text.IndexOf('Box2D.NET physics candidate smoke passed.', [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Box2D.NET smoke did not report a passing result.'
    }

    if ($text.IndexOf('AllocatedBytesPerTick=', [System.StringComparison]::Ordinal) -lt 0) {
        throw 'Box2D.NET smoke did not report AllocatedBytesPerTick.'
    }

    $output | ForEach-Object { Write-Host $_ }
}

function Invoke-SmokeExecutable([string]$directory) {
    $name = 'Electron2D.Tests.PhysicsBox2DSmoke'
    if ([System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)) {
        $name += '.exe'
    }

    $executable = Join-Path $directory $name
    if (-not (Test-Path -LiteralPath $executable)) {
        throw "Published Box2D.NET smoke executable was not found: $executable"
    }

    Invoke-SmokeOutput @($executable, '--warmup', $WarmupTicks.ToString(), '--ticks', $Ticks.ToString())
}

$combined = (Get-Content -LiteralPath $specPath -Raw) + "`n" + (Get-Content -LiteralPath $docPath -Raw)
$requiredFragments = @(
    'Box2D.NET 3.1.654',
    'Windows x64',
    'Linux x64',
    'macOS',
    'Android arm64',
    'iOS arm64',
    'NativeAOT',
    'Release/AOT',
    'mobile',
    'gap',
    'allocations per tick',
    'AllocatedBytesPerTick'
)

foreach ($fragment in $requiredFragments) {
    if ($combined.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Box2D.NET validation docs are missing required fragment: $fragment"
    }
}

if ([string]::IsNullOrWhiteSpace($RuntimeIdentifier)) {
    $RuntimeIdentifier = Get-CurrentRuntimeIdentifier
}

Invoke-SmokeOutput @(
    'dotnet',
    'run',
    '--configuration',
    'Release',
    '--project',
    $projectPath,
    '--',
    '--warmup',
    $WarmupTicks.ToString(),
    '--ticks',
    $Ticks.ToString())

if ($NativeAot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force -ErrorAction SilentlyContinue

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

Write-Host "Box2D.NET physics candidate verification passed. RuntimeIdentifier: $RuntimeIdentifier. NativeAot: $NativeAot."
