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
    [string]$OutputPath,
    [string]$WikiPath = '.github/wiki',
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$workRoot = Join-Path $repoRoot '.temp/api-manifest'
$xmlPath = Join-Path $workRoot 'Electron2D.xml'
$generatorProject = Join-Path $repoRoot 'tools/Electron2D.ApiManifestGenerator/Electron2D.ApiManifestGenerator.csproj'
$defaultManifestPath = Join-Path $repoRoot 'data/api/electron2d-api-manifest.json'

function Resolve-RepositoryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

$targetManifestPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $defaultManifestPath
}
else {
    Resolve-RepositoryPath -Path $OutputPath
}

$resolvedWikiPath = Resolve-RepositoryPath -Path $WikiPath
$compatibilityPath = if ((Test-Path -LiteralPath $resolvedWikiPath -PathType Leaf) -and
    [System.IO.Path]::GetFileName($resolvedWikiPath).EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)) {
    $resolvedWikiPath
}
else {
    Join-Path $resolvedWikiPath 'API-Compatibility.md'
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

if (-not (Test-Path -LiteralPath $generatorProject)) {
    throw 'API manifest generator project was not found.'
}

if (-not (Test-Path -LiteralPath $compatibilityPath)) {
    throw "GitHub Wiki compatibility page was not found: $compatibilityPath"
}

New-Item -ItemType Directory -Force -Path $workRoot | Out-Null

$buildOutput = & dotnet build $projectPath --no-restore `
    '-p:GenerateDocumentationFile=true' `
    "-p:DocumentationFile=$xmlPath" 2>&1
$buildExitCode = $LASTEXITCODE
if ($buildExitCode -ne 0) {
    Write-Host ($buildOutput -join [Environment]::NewLine)
    exit $buildExitCode
}

$assemblyPath = Join-Path $repoRoot 'src/Electron2D/bin/Debug/net10.0/Electron2D.dll'
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw "Built assembly was not found: $assemblyPath"
}

if (-not (Test-Path -LiteralPath $xmlPath)) {
    throw "XML documentation file was not generated: $xmlPath"
}

$expectedManifestPath = if ($Check) {
    Join-Path $workRoot 'expected/electron2d-api-manifest.json'
}
else {
    $targetManifestPath
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $expectedManifestPath) | Out-Null

$generatorOutput = & dotnet run --project $generatorProject -- `
    --repo-root $repoRoot `
    --assembly $assemblyPath `
    --xml $xmlPath `
    --compatibility $compatibilityPath `
    --output $expectedManifestPath 2>&1
$generatorExitCode = $LASTEXITCODE
if ($generatorExitCode -ne 0) {
    Write-Host ($generatorOutput -join [Environment]::NewLine)
    exit $generatorExitCode
}

if ($Check) {
    if (-not (Test-Path -LiteralPath $targetManifestPath)) {
        throw "API manifest was not found: $targetManifestPath"
    }

    $expectedText = [System.IO.File]::ReadAllText($expectedManifestPath).Replace("`r`n", "`n").Replace("`r", "`n")
    $actualText = [System.IO.File]::ReadAllText($targetManifestPath).Replace("`r`n", "`n").Replace("`r", "`n")
    if (-not [System.String]::Equals($expectedText, $actualText, [System.StringComparison]::Ordinal)) {
        throw "API manifest is out of date: $targetManifestPath"
    }

    Write-Host "API manifest verification passed: $targetManifestPath"
    exit 0
}

Write-Host "API manifest updated: $targetManifestPath"
