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
$manifestPath = Join-Path $repoRoot 'assets/reference-games/manifest.json'
$licensesPath = Join-Path $repoRoot 'assets/reference-games/LICENSES.md'
$readmePath = Join-Path $repoRoot 'assets/reference-games/README.md'

function Resolve-RepositoryPath([string]$relativePath) {
    if ([System.String]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Manifest contains an empty path.'
    }

    if ($relativePath -match '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "Asset path must be local, not a URL: $relativePath"
    }

    $normalized = $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $normalized))
    $rootPath = [System.IO.Path]::GetFullPath($repoRoot)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Asset path escapes repository root: $relativePath"
    }

    return $fullPath
}

function Assert-HashString([string]$value, [string]$label) {
    if ($value -notmatch '^[a-f0-9]{64}$') {
        throw "$label must be a lowercase SHA-256 hex string."
    }
}

function Assert-Png([string]$path) {
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 8 -or
        $bytes[0] -ne 0x89 -or $bytes[1] -ne 0x50 -or $bytes[2] -ne 0x4E -or $bytes[3] -ne 0x47 -or
        $bytes[4] -ne 0x0D -or $bytes[5] -ne 0x0A -or $bytes[6] -ne 0x1A -or $bytes[7] -ne 0x0A) {
        throw "PNG signature is invalid: $path"
    }
}

function Assert-Ogg([string]$path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        if ($stream.Length -lt 4) {
            throw "OGG file is too small: $path"
        }

        $buffer = New-Object byte[] 4
        [void]$stream.Read($buffer, 0, 4)
        $signature = [System.Text.Encoding]::ASCII.GetString($buffer)
        if ($signature -ne 'OggS') {
            throw "OGG signature is invalid: $path"
        }
    }
    finally {
        $stream.Dispose()
    }
}

function Assert-Ttf([string]$path) {
    $stream = [System.IO.File]::OpenRead($path)
    try {
        if ($stream.Length -lt 4) {
            throw "TTF file is too small: $path"
        }

        $buffer = New-Object byte[] 4
        [void]$stream.Read($buffer, 0, 4)
        $tag = [System.Text.Encoding]::ASCII.GetString($buffer)
        $isTrueType = $buffer[0] -eq 0x00 -and $buffer[1] -eq 0x01 -and $buffer[2] -eq 0x00 -and $buffer[3] -eq 0x00
        if (-not $isTrueType -and $tag -ne 'OTTO') {
            throw "Font signature is invalid: $path"
        }
    }
    finally {
        $stream.Dispose()
    }
}

if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "Reference game asset manifest was not found: $manifestPath"
}

if (-not (Test-Path -LiteralPath $licensesPath)) {
    throw "Reference game asset licenses file was not found: $licensesPath"
}

if (-not (Test-Path -LiteralPath $readmePath)) {
    throw "Reference game asset README was not found: $readmePath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.schemaVersion -ne 1) {
    throw "Unexpected reference asset manifest schema version: $($manifest.schemaVersion)"
}

if ($manifest.release -ne '0.1.0-preview') {
    throw "Unexpected reference asset manifest release: $($manifest.release)"
}

if ($manifest.assetRoot -ne 'assets/reference-games') {
    throw "Unexpected reference asset root: $($manifest.assetRoot)"
}

if ($manifest.networkRequiredDuringBuild -ne $false) {
    throw 'Reference game assets must not require network access during build.'
}

$sourceIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($source in @($manifest.sources)) {
    if (-not $sourceIds.Add([string]$source.id)) {
        throw "Duplicate source id in reference asset manifest: $($source.id)"
    }

    if ([System.String]::IsNullOrWhiteSpace([string]$source.author)) {
        throw "Source is missing author: $($source.id)"
    }

    if (@('CC0-1.0', 'MIT') -notcontains [string]$source.license) {
        throw "Source has unsupported license '$($source.license)': $($source.id)"
    }

    if ([System.String]::IsNullOrWhiteSpace([string]$source.licenseUrl)) {
        throw "Source is missing license URL/path: $($source.id)"
    }

    if ([System.String]::IsNullOrWhiteSpace([string]$source.sourceUrl)) {
        throw "Source is missing source URL: $($source.id)"
    }

    if ($null -ne $source.sourceArchiveSha256 -and -not [System.String]::IsNullOrWhiteSpace([string]$source.sourceArchiveSha256)) {
        Assert-HashString ([string]$source.sourceArchiveSha256) "Source archive hash for $($source.id)"
    }
}

foreach ($requiredSource in @('kenney-pixel-platformer', 'kenney-ui-pack', 'kenney-rpg-sounds', 'electron2d')) {
    if (-not $sourceIds.Contains($requiredSource)) {
        throw "Reference game asset manifest is missing required source: $requiredSource"
    }
}

$assetIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$assetPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$rolesByGame = @{}
$allowedExtensions = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($extension in @('.png', '.ogg', '.ttf', '.tmx', '.tsx', '.json')) {
    [void]$allowedExtensions.Add($extension)
}

foreach ($asset in @($manifest.assets)) {
    $assetId = [string]$asset.id
    if (-not $assetIds.Add($assetId)) {
        throw "Duplicate asset id in reference asset manifest: $assetId"
    }

    $assetPath = [string]$asset.path
    if (-not $assetPath.StartsWith('assets/reference-games/', [System.StringComparison]::Ordinal)) {
        throw "Reference asset path must stay under assets/reference-games: $assetPath"
    }

    if ($assetPath -match '\\') {
        throw "Reference asset path must use forward slashes: $assetPath"
    }

    if (-not $assetPaths.Add($assetPath)) {
        throw "Duplicate asset path in reference asset manifest: $assetPath"
    }

    if ([System.IO.Path]::GetExtension($assetPath) -in @('.url', '.sfk', '.tmp', '.cache')) {
        throw "Forbidden generated/source-helper file is listed as an asset: $assetPath"
    }

    $extension = [System.IO.Path]::GetExtension($assetPath)
    if (-not $allowedExtensions.Contains($extension)) {
        throw "Unsupported reference asset extension '$extension': $assetPath"
    }

    if (-not $sourceIds.Contains([string]$asset.source)) {
        throw "Asset references unknown source '$($asset.source)': $assetId"
    }

    Assert-HashString ([string]$asset.sha256) "Asset hash for $assetId"

    $fullPath = Resolve-RepositoryPath $assetPath
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Reference asset file was not found: $assetPath"
    }

    $file = Get-Item -LiteralPath $fullPath
    if ($file.Length -ne [int64]$asset.bytes) {
        throw "Reference asset byte length mismatch for ${assetPath}: expected $($asset.bytes), got $($file.Length)"
    }

    $actualHash = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne [string]$asset.sha256) {
        throw "Reference asset hash mismatch for ${assetPath}: expected $($asset.sha256), got $actualHash"
    }

    switch ($extension.ToLowerInvariant()) {
        '.png' { Assert-Png $fullPath }
        '.ogg' { Assert-Ogg $fullPath }
        '.ttf' { Assert-Ttf $fullPath }
        '.json' { [void](Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json) }
        '.tmx' { [void]([xml](Get-Content -LiteralPath $fullPath -Raw)) }
        '.tsx' { [void]([xml](Get-Content -LiteralPath $fullPath -Raw)) }
    }

    foreach ($game in @($asset.games)) {
        $gameId = [string]$game
        if (-not $rolesByGame.ContainsKey($gameId)) {
            $rolesByGame[$gameId] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
        }

        foreach ($role in @($asset.roles)) {
            [void]$rolesByGame[$gameId].Add([string]$role)
        }
    }
}

$manifestDirectory = Split-Path -Parent $manifestPath
$unexpectedFiles = Get-ChildItem -LiteralPath $manifestDirectory -Recurse -File |
    Where-Object {
        $relative = Resolve-Path -LiteralPath $_.FullName -Relative
        $normalized = ($relative -replace '^\.([\\/])', '').Replace('\', '/')
        $normalized -notin @(
            'assets/reference-games/manifest.json',
            'assets/reference-games/README.md',
            'assets/reference-games/LICENSES.md'
        ) -and -not $assetPaths.Contains($normalized)
    }

if ($unexpectedFiles) {
    $names = ($unexpectedFiles | ForEach-Object { Resolve-Path -LiteralPath $_.FullName -Relative }) -join ', '
    throw "Reference asset directory contains files not listed in manifest: $names"
}

foreach ($requirement in $manifest.requirements.PSObject.Properties) {
    $gameId = $requirement.Name
    if (-not $rolesByGame.ContainsKey($gameId)) {
        throw "Reference asset manifest has no assets for required game: $gameId"
    }

    foreach ($role in @($requirement.Value)) {
        if (-not $rolesByGame[$gameId].Contains([string]$role)) {
            throw "Reference game '$gameId' is missing required asset role: $role"
        }
    }
}

Write-Host "Reference game assets verification passed. Files: $($assetIds.Count), sources: $($sourceIds.Count)."
