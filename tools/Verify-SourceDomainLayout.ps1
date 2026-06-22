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
$sourceRoot = Join-Path $repoRoot 'src/Electron2D'
$coreRoot = Join-Path $sourceRoot 'Core'
$exportPresetRoot = Join-Path $sourceRoot 'Export/Presets'

if (-not (Test-Path -LiteralPath $sourceRoot)) {
    throw 'Runtime source root src/Electron2D was not found.'
}

if (-not (Test-Path -LiteralPath $coreRoot)) {
    throw 'Core source domain src/Electron2D/Core was not found.'
}

$allowedCoreDomains = @(
    'Collections',
    'Identity',
    'Math',
    'ObjectModel',
    'Random',
    'SceneTree',
    'Variant'
)

$requiredRootDomains = @(
    'Assets',
    'Core',
    'Export',
    'Graphics',
    'Physics',
    'Runtime'
)

$ignoredRootDirectories = @(
    'bin',
    'obj',
    'Properties'
)

$requiredNestedDomains = @{
    'Assets' = @(
        'Resources'
    )
    'Graphics' = @(
        'Display',
        'Rendering',
        'Text',
        'UI'
    )
    'Runtime' = @(
        'Animation',
        'Audio',
        'Input',
        'Localization',
        'Scripting',
        'Settings'
    )
}

$allowedNamespaces = @(
    'Electron2D',
    'Electron2D.Collections'
)

$coreDirectories = @(Get-ChildItem -LiteralPath $coreRoot -Directory | Select-Object -ExpandProperty Name)
foreach ($directory in $coreDirectories) {
    if ($allowedCoreDomains -notcontains $directory) {
        throw "Non-core domain is not allowed under src/Electron2D/Core: $directory"
    }
}

foreach ($domain in $allowedCoreDomains) {
    if (-not (Test-Path -LiteralPath (Join-Path $coreRoot $domain))) {
        throw "Required core domain was not found: src/Electron2D/Core/$domain"
    }
}

$rootDirectories = @(Get-ChildItem -LiteralPath $sourceRoot -Directory | Select-Object -ExpandProperty Name)
foreach ($directory in $rootDirectories) {
    if (($ignoredRootDirectories -notcontains $directory) -and ($requiredRootDomains -notcontains $directory)) {
        throw "Unexpected root source domain src/Electron2D/$directory. Use the coarse root domains: $($requiredRootDomains -join ', ')."
    }
}

foreach ($domain in $requiredRootDomains) {
    if (-not (Test-Path -LiteralPath (Join-Path $sourceRoot $domain))) {
        throw "Required root source domain was not found: src/Electron2D/$domain"
    }
}

foreach ($rootDomain in $requiredNestedDomains.Keys) {
    foreach ($nestedDomain in $requiredNestedDomains[$rootDomain]) {
        $nestedPath = Join-Path (Join-Path $sourceRoot $rootDomain) $nestedDomain
        if (-not (Test-Path -LiteralPath $nestedPath)) {
            throw "Required nested source domain was not found: src/Electron2D/$rootDomain/$nestedDomain"
        }
    }
}

if (-not (Test-Path -LiteralPath $exportPresetRoot)) {
    throw 'Export presets must live in src/Electron2D/Export/Presets.'
}

$sourceFiles = Get-ChildItem -LiteralPath $sourceRoot -Recurse -File -Filter '*.cs' |
    Where-Object {
        $_.FullName -notmatch '[\\/](bin|obj)[\\/]'
    }

$namespacePattern = '^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]'
foreach ($file in $sourceFiles) {
    $lineNumber = 0
    foreach ($line in Get-Content -LiteralPath $file.FullName) {
        $lineNumber++
        if ($line -match $namespacePattern) {
            $namespace = $Matches[1]
            if ($allowedNamespaces -notcontains $namespace) {
                $relativePath = [System.IO.Path]::GetRelativePath($repoRoot, $file.FullName).Replace('\', '/')
                throw "Unsupported namespace '$namespace' in ${relativePath}:$lineNumber. Source folders do not define namespaces."
            }
        }
    }
}

Write-Host "Source domain layout verification passed. Source files scanned: $($sourceFiles.Count)."
