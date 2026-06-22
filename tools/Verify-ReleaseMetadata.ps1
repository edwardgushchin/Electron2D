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
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$readmePath = Join-Path $repoRoot 'README.md'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

[xml]$project = Get-Content -LiteralPath $projectPath
$propertyGroups = @($project.Project.PropertyGroup)

function Get-ProjectProperty([string]$name) {
    foreach ($group in $propertyGroups) {
        $node = $group.SelectSingleNode($name)
        if ($node -and -not [string]::IsNullOrWhiteSpace($node.InnerText)) {
            return $node.InnerText.Trim()
        }
    }

    return $null
}

$expectedProperties = @{
    Version = '0.1.0-preview'
    PackageVersion = '0.1.0-preview'
    AssemblyVersion = '0.1.0.0'
    FileVersion = '0.1.0.0'
    InformationalVersion = '0.1.0-preview'
    PackageId = 'Electron2D'
    Authors = 'Electron2D Team'
    PackageLicenseExpression = 'MIT'
    PackageReadmeFile = 'README.md'
    RepositoryType = 'git'
}

foreach ($entry in $expectedProperties.GetEnumerator()) {
    $actual = Get-ProjectProperty $entry.Key
    if ($actual -ne $entry.Value) {
        throw "Project metadata mismatch for $($entry.Key): expected '$($entry.Value)', got '$actual'."
    }
}

foreach ($path in @($readmePath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required release metadata file was not found: $path"
    }
}

$readme = Get-Content -LiteralPath $readmePath -Raw

if ($readme.IndexOf('0.1.0 Preview', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'README.md does not mention 0.1.0 Preview.'
}

$trackedDrafts = & git -C $repoRoot ls-files -- 'CHANGELOG*' 'RELEASE-NOTES*' 'TASKS.md'
if ($LASTEXITCODE -ne 0) {
    throw 'git ls-files failed while checking local-only release draft files.'
}

if (-not [System.String]::IsNullOrWhiteSpace($trackedDrafts)) {
    throw "Local-only release draft or task files are tracked by Git: $trackedDrafts"
}

Write-Host 'Release metadata verification passed.'
