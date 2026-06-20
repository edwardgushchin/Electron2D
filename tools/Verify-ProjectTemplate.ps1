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

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateRoot = Join-Path $repoRoot 'templates/electron2d-empty'
$packageOutput = Join-Path $repoRoot '.temp/template-package'
$workRoot = Join-Path $repoRoot '.temp/template-check'
$createdProject = Join-Path $workRoot 'Electron2D.Empty'
$projectPath = Join-Path $createdProject 'Electron2D.Empty.csproj'

if (-not (Test-Path -LiteralPath $templateRoot)) {
    throw 'Template directory templates/electron2d-empty was not found.'
}

$requiredFiles = @(
    '.template.config/template.json',
    'Electron2D.Empty.csproj',
    'Program.cs',
    'project.e2d.json',
    'scenes/main.scene.json',
    'README.md'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $templateRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Template file was not found: $relativePath"
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

dotnet restore $projectPath --source $packageOutput | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet build $projectPath --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$runOutput = dotnet run --project $projectPath --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host $runOutput
    exit $LASTEXITCODE
}

$expectedOutput = 'Electron2D empty scene loaded: scenes/main.scene.json'
if (($runOutput -join [Environment]::NewLine).IndexOf($expectedOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    Write-Host $runOutput
    throw "Template run output does not contain expected line: $expectedOutput"
}

Write-Host 'Project template verification passed.'
