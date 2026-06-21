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
    [string]$WikiRepositoryUrl = 'https://github.com/edwardgushchin/Electron2D.wiki.git',
    [string]$WikiPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$wikiCloneRoot = Join-Path $repoRoot '.github/wiki'
$inspectorRoot = Join-Path $repoRoot '.temp/api-compatibility-inspector'
$inspectorProject = Join-Path $inspectorRoot 'ApiCompatibilityInspector.csproj'

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & git @Arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ($output -join [Environment]::NewLine)
        exit $LASTEXITCODE
    }

    return $output
}

if ([string]::IsNullOrWhiteSpace($WikiPath)) {
    if (-not (Test-Path -LiteralPath (Join-Path $wikiCloneRoot '.git'))) {
        New-Item -ItemType Directory -Force -Path (Split-Path -Parent $wikiCloneRoot) | Out-Null
        Invoke-Git -Arguments @('clone', '--depth', '1', $WikiRepositoryUrl, $wikiCloneRoot) | Out-Null
    }
    else {
        $status = @(Invoke-Git -Arguments @('-C', $wikiCloneRoot, 'status', '--short'))
        if ($status.Count -eq 0) {
            Invoke-Git -Arguments @('-C', $wikiCloneRoot, 'pull', '--ff-only') | Out-Null
        }
        else {
            Write-Host "Using existing GitHub Wiki clone with local changes: $wikiCloneRoot"
        }
    }

    $wikiPath = Join-Path $wikiCloneRoot 'API-Compatibility.md'
}
else {
    $wikiPath = if ([System.IO.Path]::IsPathRooted($WikiPath)) {
        [System.IO.Path]::GetFullPath($WikiPath)
    }
    else {
        [System.IO.Path]::GetFullPath((Join-Path $repoRoot $WikiPath))
    }
}

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

if (-not (Test-Path -LiteralPath $wikiPath)) {
    throw "GitHub Wiki compatibility page was not found: $wikiPath"
}

Remove-Item -LiteralPath $inspectorRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $inspectorRoot | Out-Null

@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="$projectPath" />
  </ItemGroup>
</Project>
"@ | Set-Content -LiteralPath $inspectorProject -Encoding UTF8

@"
using System;
using System.Linq;

var assembly = typeof(Electron2D.Object).Assembly;
foreach (var typeName in assembly.GetExportedTypes().Select(type => type.FullName).OrderBy(typeName => typeName, StringComparer.Ordinal))
{
    Console.WriteLine(typeName);
}
"@ | Set-Content -LiteralPath (Join-Path $inspectorRoot 'Program.cs') -Encoding UTF8

$publicTypes = @(dotnet run --project $inspectorProject --no-launch-profile)
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$publicTypes = @($publicTypes | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
$wiki = Get-Content -LiteralPath $wikiPath -Raw
$requiredStatuses = @('Supported', 'Partial', 'Experimental', 'Planned')

foreach ($typeName in $publicTypes) {
    if ($wiki.IndexOf("| ``$typeName`` |", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Public type is missing from GitHub Wiki compatibility table: $typeName"
    }
}

foreach ($status in $requiredStatuses) {
    if ($wiki.IndexOf("| $status |", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Compatibility table does not contain status: $status"
    }
}

$forbiddenTypes = @(
    'Electron2D.IComponent',
    'Electron2D.SpriteRenderer',
    'Electron2D.SpriteAnimator',
    'Electron2D.AudioSource',
    'Electron2D.Rigidbody',
    'Electron2D.Collider',
    'Electron2D.BoxCollider',
    'Electron2D.CircleCollider',
    'Electron2D.PolygonCollider',
    'Electron2D.PhysicsBodyType'
)

foreach ($forbiddenType in $forbiddenTypes) {
    if ($publicTypes -contains $forbiddenType) {
        throw "Forbidden legacy type is exported: $forbiddenType"
    }
}

if (Test-Path -LiteralPath (Join-Path $repoRoot 'mkdocs.yml')) {
    throw 'Local documentation site configuration mkdocs.yml is not allowed for the GitHub Wiki table.'
}

if (Test-Path -LiteralPath (Join-Path $repoRoot 'site')) {
    throw 'Local generated site directory is not allowed for the GitHub Wiki table.'
}

Write-Host "API compatibility verification passed. Public types: $($publicTypes.Count)."
