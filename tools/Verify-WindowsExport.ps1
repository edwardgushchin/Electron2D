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
$cliProjectPath = Join-Path $repoRoot 'src/Electron2D.Cli/Electron2D.Cli.csproj'
$packageOutput = Join-Path $repoRoot '.temp/windows-export-package'
$workRoot = Join-Path $repoRoot '.temp/windows-export-check'
$createdProject = Join-Path $workRoot 'Electron2D.Empty'
$projectPath = Join-Path $createdProject 'Electron2D.Empty.csproj'
$packagesRoot = Join-Path $workRoot '.nuget-packages'
$nugetConfig = Join-Path $workRoot 'NuGet.Config'

if (-not (Test-Path -LiteralPath $templateRoot)) {
    throw 'Template directory data/templates/electron2d-empty was not found.'
}

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Assert-PackEntry([string]$packagePath, [string]$entryName) {
    if (-not (Test-Path -LiteralPath $packagePath -PathType Leaf)) {
        throw "Windows export package was not found: $packagePath"
    }

    $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $entries = @($archive.Entries | ForEach-Object { $_.FullName.Replace('\', '/') })
        if ($entries -notcontains $entryName) {
            throw "Windows export package '$packagePath' does not contain expected entry: $entryName"
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Assert-NoLooseOutput([string]$publishOutput) {
    foreach ($forbiddenPath in @('project.e2d.json', 'assets', 'resources', 'scenes', '.electron2d')) {
        if (Test-Path -LiteralPath (Join-Path $publishOutput $forbiddenPath)) {
            throw "Windows export output must not contain loose project source path: $forbiddenPath"
        }
    }
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

$env:NUGET_PACKAGES = $packagesRoot

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="electron2d-local" value="$packageOutput" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <config>
    <add key="globalPackagesFolder" value="$packagesRoot" />
  </config>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding UTF8

dotnet restore $projectPath --configfile $nugetConfig --packages $packagesRoot --runtime win-x64 | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$expectedRunOutput = @(
    'Mode=run',
    'Project=Electron2D.Empty',
    'WindowCreated=True',
    'FramePresented=True',
    'ScreenshotSaved=True',
    'RuntimeSucceeded=True'
)

foreach ($configuration in @('Debug', 'Release')) {
    $publishOutput = Join-Path $workRoot "publish-$configuration"
    dotnet run --project $cliProjectPath -- `
        export build-windows `
        --project $createdProject `
        --project-file $projectPath `
        --configuration $configuration `
        --output $publishOutput `
        --format json | Out-Host
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $executablePath = Join-Path $publishOutput 'Electron2D.Empty.exe'
    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "Windows export executable was not found: $executablePath"
    }

    $manifestPath = Join-Path $publishOutput 'electron2d.pack.json'
    if (-not (Test-Path -LiteralPath $manifestPath)) {
        throw "Windows export resource pack manifest was not found: $manifestPath"
    }

    $projectPackPath = Join-Path $publishOutput 'packs/project.e2dpkg'
    $scenePackPath = Join-Path $publishOutput 'packs/scenes/main.e2dpkg'
    Assert-PackEntry $projectPackPath 'project.e2d.json'
    Assert-PackEntry $scenePackPath 'scenes/main.scene.json'
    Assert-NoLooseOutput $publishOutput

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $packPaths = @($manifest.packs | ForEach-Object { [string]$_.path })
    foreach ($expectedPack in @('packs/project.e2dpkg', 'packs/scenes/main.e2dpkg')) {
        if ($packPaths -notcontains $expectedPack) {
            throw "Windows export manifest does not list expected package: $expectedPack"
        }
    }

    $screenshotPath = Join-Path $publishOutput 'empty-export.png'
    $runOutput = & $executablePath --screenshot $screenshotPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host $runOutput
        exit $LASTEXITCODE
    }

    if (-not (Test-Path -LiteralPath $screenshotPath -PathType Leaf)) {
        throw "Windows exported player did not write a screenshot: $screenshotPath"
    }

    $joinedOutput = $runOutput -join [Environment]::NewLine
    foreach ($expectedOutput in $expectedRunOutput) {
        if ($joinedOutput.IndexOf($expectedOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            Write-Host $runOutput
            throw "Windows export run output does not contain expected line: $expectedOutput"
        }
    }
}

Write-Host 'Windows export verification passed.'
