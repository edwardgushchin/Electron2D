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

if (-not [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform(
    [System.Runtime.InteropServices.OSPlatform]::OSX)) {
    throw 'macOS export verification requires a macOS host.'
}

$architecture = (uname -m).Trim()
if ($architecture -ne 'arm64') {
    throw "macOS export verification requires an arm64 host. Current architecture: $architecture"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateRoot = Join-Path $repoRoot 'templates/electron2d-empty'
$packageOutput = Join-Path $repoRoot '.temp/macos-export-package'
$workRoot = Join-Path $repoRoot '.temp/macos-export-check'
$createdProject = Join-Path $workRoot 'Electron2D.Empty'
$projectPath = Join-Path $createdProject 'Electron2D.Empty.csproj'
$packagesRoot = Join-Path $workRoot '.nuget-packages'
$nugetConfig = Join-Path $workRoot 'NuGet.Config'

if (-not (Test-Path -LiteralPath $templateRoot)) {
    throw 'Template directory templates/electron2d-empty was not found.'
}

Remove-Item -LiteralPath $packageOutput, $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packageOutput, $createdProject | Out-Null

dotnet restore (Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj')
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet pack (Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj') --no-restore -o $packageOutput
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Copy-Item -Path (Join-Path $templateRoot '*') -Destination $createdProject -Recurse -Force
Remove-Item -LiteralPath (Join-Path $createdProject '.template.config') -Recurse -Force -ErrorAction SilentlyContinue

$nugetConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="electron2d-local" value="$packageOutput" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@
Set-Content -LiteralPath $nugetConfig -Value $nugetConfigContent -Encoding UTF8

dotnet restore $projectPath --configfile $nugetConfig --packages $packagesRoot --runtime osx-arm64
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
        --runtime osx-arm64 `
        --self-contained true `
        --output $publishOutput
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $appBundlePath = Join-Path $workRoot "Electron2D.Empty-$configuration.app"
    $contentsDirectory = Join-Path $appBundlePath 'Contents'
    $macOSDirectory = Join-Path $contentsDirectory 'MacOS'
    $resourcesDirectory = Join-Path $contentsDirectory 'Resources'
    New-Item -ItemType Directory -Force -Path $macOSDirectory, $resourcesDirectory | Out-Null

    Copy-Item -Path (Join-Path $publishOutput '*') -Destination $macOSDirectory -Recurse -Force

    $infoPlistPath = Join-Path $contentsDirectory 'Info.plist'
    $infoPlist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleExecutable</key>
  <string>Electron2D.Empty</string>
  <key>CFBundleIdentifier</key>
  <string>dev.electron2d.empty.$($configuration.ToLowerInvariant())</string>
  <key>CFBundleName</key>
  <string>Electron2D.Empty</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>0.1.0</string>
  <key>CFBundleVersion</key>
  <string>0.1.0-preview</string>
  <key>LSMinimumSystemVersion</key>
  <string>13.0</string>
</dict>
</plist>
"@
    Set-Content -LiteralPath $infoPlistPath -Value $infoPlist -Encoding UTF8

    $executablePath = Join-Path $macOSDirectory 'Electron2D.Empty'
    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "macOS export executable was not found: $executablePath"
    }

    chmod +x $executablePath

    $projectSettingsPath = Join-Path $macOSDirectory 'project.e2d.json'
    if (-not (Test-Path -LiteralPath $projectSettingsPath)) {
        throw "macOS export project settings were not found: $projectSettingsPath"
    }

    $scenePath = Join-Path $macOSDirectory 'scenes/main.scene.json'
    if (-not (Test-Path -LiteralPath $scenePath)) {
        throw "macOS export reference scene was not found: $scenePath"
    }

    if (-not (Test-Path -LiteralPath $infoPlistPath)) {
        throw "macOS export Info.plist was not found: $infoPlistPath"
    }

    $runOutput = & $executablePath
    if ($LASTEXITCODE -ne 0) {
        Write-Host $runOutput
        exit $LASTEXITCODE
    }

    foreach ($expectedOutput in @($expectedSceneOutput, $expectedLifecycleOutput, $expectedServiceOutput)) {
        if (-not ($runOutput -like "*$expectedOutput*")) {
            Write-Host $runOutput
            throw "macOS export run output does not contain expected line: $expectedOutput"
        }
    }
}

Write-Host 'macOS export verification passed.'
