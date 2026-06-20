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
$wikiPath = Join-Path $repoRoot '.github/wiki/API-Compatibility.md'
$inspectorRoot = Join-Path $repoRoot '.temp/api-compatibility-inspector'
$inspectorProject = Join-Path $inspectorRoot 'ApiCompatibilityInspector.csproj'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

if (-not (Test-Path -LiteralPath $wikiPath)) {
    throw 'GitHub Wiki source .github/wiki/API-Compatibility.md was not found.'
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
$allowedStatuses = @('Supported', 'Partial', 'Experimental', 'Planned', 'Not planned')

foreach ($typeName in $publicTypes) {
    if ($wiki.IndexOf("| ``$typeName`` |", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Public type is missing from GitHub Wiki compatibility table: $typeName"
    }
}

foreach ($status in $allowedStatuses) {
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
