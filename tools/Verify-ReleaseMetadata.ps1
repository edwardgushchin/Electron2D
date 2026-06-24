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
$editorProjectPath = Join-Path $repoRoot 'src/Electron2D.Editor/Electron2D.Editor.csproj'
$readmePath = Join-Path $repoRoot 'README.md'
$packageIconPath = Join-Path $repoRoot 'data/assets/branding/icon/electron2d_windows_icon_128.png'
$editorIconPath = Join-Path $repoRoot 'data/assets/branding/icon/electron2d.ico'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

if (-not (Test-Path -LiteralPath $editorProjectPath)) {
    throw 'Editor project src/Electron2D.Editor/Electron2D.Editor.csproj was not found.'
}

[xml]$project = Get-Content -LiteralPath $projectPath
$propertyGroups = @($project.Project.PropertyGroup)
[xml]$editorProject = Get-Content -LiteralPath $editorProjectPath
$editorPropertyGroups = @($editorProject.Project.PropertyGroup)

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
    PackageIcon = 'electron2d_windows_icon_128.png'
    RepositoryType = 'git'
}

foreach ($entry in $expectedProperties.GetEnumerator()) {
    $actual = Get-ProjectProperty $entry.Key
    if ($actual -ne $entry.Value) {
        throw "Project metadata mismatch for $($entry.Key): expected '$($entry.Value)', got '$actual'."
    }
}

function Get-EditorProjectProperty([string]$name) {
    foreach ($group in $editorPropertyGroups) {
        $node = $group.SelectSingleNode($name)
        if ($node -and -not [string]::IsNullOrWhiteSpace($node.InnerText)) {
            return $node.InnerText.Trim()
        }
    }

    return $null
}

$editorApplicationIcon = Get-EditorProjectProperty 'ApplicationIcon'
if ($editorApplicationIcon -ne '..\..\data\assets\branding\icon\electron2d.ico') {
    throw "Editor application icon mismatch: expected '..\..\data\assets\branding\icon\electron2d.ico', got '$editorApplicationIcon'."
}

foreach ($path in @($readmePath, $packageIconPath, $editorIconPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required release metadata file was not found: $path"
    }
}

$readme = Get-Content -LiteralPath $readmePath -Raw

if ($readme.IndexOf('0.1.0-preview', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'README.md does not mention 0.1.0-preview.'
}

foreach ($brandAssetPath in @(
    'data/assets/branding/readme/electron2d_readme_dark.svg',
    'data/assets/branding/readme/electron2d_readme_light.svg'
)) {
    if ($readme.IndexOf($brandAssetPath, [System.StringComparison]::Ordinal) -lt 0) {
        throw "README.md does not reference brand asset: $brandAssetPath"
    }
}

$packageIconItem = $project.Project.ItemGroup.None |
    Where-Object { $_.Include -eq '..\..\data\assets\branding\icon\electron2d_windows_icon_128.png' } |
    Select-Object -First 1

if (-not $packageIconItem) {
    throw 'Runtime project does not pack the Electron2D package icon.'
}

if ($packageIconItem.Pack -ne 'true' -or $packageIconItem.PackagePath -ne '\') {
    throw 'Runtime package icon must be packed at the package root.'
}

$trackedDrafts = & git -C $repoRoot ls-files -- 'CHANGELOG*' 'RELEASE-NOTES*' 'TASKS.md'
if ($LASTEXITCODE -ne 0) {
    throw 'git ls-files failed while checking local-only release draft files.'
}

if (-not [System.String]::IsNullOrWhiteSpace($trackedDrafts)) {
    throw "Local-only release draft or task files are tracked by Git: $trackedDrafts"
}

Write-Host 'Release metadata verification passed.'
