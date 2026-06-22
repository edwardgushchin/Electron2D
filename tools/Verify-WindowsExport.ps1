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
$ErrorActionPreference = 'Stop'

$isWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::Windows)
if (-not $isWindowsHost) {
    throw 'Windows export verification requires a Windows host.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateRoot = Join-Path $repoRoot 'data/templates/electron2d-empty'
$packageOutput = Join-Path $repoRoot '.temp/windows-export-package'
$workRoot = Join-Path $repoRoot '.temp/windows-export-check'
$createdProject = Join-Path $workRoot 'Electron2D.Empty'
$projectPath = Join-Path $createdProject 'Electron2D.Empty.csproj'
$packagesRoot = Join-Path $workRoot '.nuget-packages'
$nugetConfig = Join-Path $workRoot 'NuGet.Config'

if (-not (Test-Path -LiteralPath $templateRoot)) {
    throw 'Template directory data/templates/electron2d-empty was not found.'
}

Remove-Item -LiteralPath $packageOutput -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packageOutput | Out-Null
New-Item -ItemType Directory -Force -Path $createdProject | Out-Null

dotnet pack (Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj') --no-restore -o $packageOutput | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Copy-Item -Path (Join-Path $templateRoot '*') -Destination $createdProject -Recurse -Force
Remove-Item -LiteralPath (Join-Path $createdProject '.template.config') -Recurse -Force

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="electron2d-local" value="$packageOutput" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding UTF8

dotnet restore $projectPath --configfile $nugetConfig --packages $packagesRoot --runtime win-x64 | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$expectedSceneOutput = 'Electron2D empty scene loaded: scenes/main.scene.json'
$expectedLifecycleOutput = 'Electron2D C# script lifecycle: _EnterTree,_Ready'
$expectedServiceOutput = 'Electron2D C# script services: tree=True,text=True'

foreach ($configuration in @('Debug', 'Release')) {
    $publishOutput = Join-Path $workRoot "publish-$configuration"
    dotnet publish $projectPath `
        --no-restore `
        --configuration $configuration `
        --runtime win-x64 `
        --self-contained true `
        --output $publishOutput | Out-Host
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $executablePath = Join-Path $publishOutput 'Electron2D.Empty.exe'
    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "Windows export executable was not found: $executablePath"
    }

    $projectSettingsPath = Join-Path $publishOutput 'project.e2d.json'
    if (-not (Test-Path -LiteralPath $projectSettingsPath)) {
        throw "Windows export project settings were not found: $projectSettingsPath"
    }

    $scenePath = Join-Path $publishOutput 'scenes/main.scene.json'
    if (-not (Test-Path -LiteralPath $scenePath)) {
        throw "Windows export reference scene was not found: $scenePath"
    }

    $runOutput = & $executablePath
    if ($LASTEXITCODE -ne 0) {
        Write-Host $runOutput
        exit $LASTEXITCODE
    }

    $joinedOutput = $runOutput -join [Environment]::NewLine
    foreach ($expectedOutput in @($expectedSceneOutput, $expectedLifecycleOutput, $expectedServiceOutput)) {
        if ($joinedOutput.IndexOf($expectedOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Write-Host $runOutput
            throw "Windows export run output does not contain expected line: $expectedOutput"
        }
    }
}

Write-Host 'Windows export verification passed.'
