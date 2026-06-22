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
    [string]$WikiPath
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

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

$resolvedWikiPath = if ([string]::IsNullOrWhiteSpace($WikiPath)) {
    Join-Path $repoRoot '.github/wiki'
}
else {
    Resolve-RepositoryPath -Path $WikiPath
}

$wikiRoot = if ((Test-Path -LiteralPath $resolvedWikiPath -PathType Leaf) -and
    [System.IO.Path]::GetFileName($resolvedWikiPath).EndsWith('.md', [System.StringComparison]::OrdinalIgnoreCase)) {
    Split-Path -Parent $resolvedWikiPath
}
else {
    $resolvedWikiPath
}

$categoryPath = Join-Path $wikiRoot 'API-UI-and-Text.md'
$compatibilityPath = Join-Path $wikiRoot 'API-Compatibility.md'

if (-not (Test-Path -LiteralPath $categoryPath)) {
    throw "Generated UI and Text Wiki category page was not found: $categoryPath"
}

if (-not (Test-Path -LiteralPath $compatibilityPath)) {
    throw "GitHub Wiki compatibility page was not found: $compatibilityPath"
}

$uiApiNames = New-Object 'System.Collections.Generic.List[string]'
foreach ($line in [System.IO.File]::ReadLines($categoryPath)) {
    if ($line -match '^\|\s*\[([^\]]+)\]\(([^)]+)\)\s*\|\s*(class|struct|enum|interface|delegate)\s*\|') {
        $displayName = $Matches[1].Trim()
        $apiName = 'Electron2D.' + $displayName.Replace('.', '+')
        $uiApiNames.Add($apiName)
    }
}

if ($uiApiNames.Count -eq 0) {
    throw "Generated UI and Text Wiki category page does not contain public type rows: $categoryPath"
}

$statusByApi = @{}
foreach ($line in [System.IO.File]::ReadLines($compatibilityPath)) {
    if ($line -match '^\|\s*`([^`]+)`\s*\|\s*[^|]+\|\s*([A-Za-z]+)\s*\|') {
        $statusByApi[$Matches[1].Trim()] = $Matches[2].Trim()
    }
}

$missingRows = New-Object 'System.Collections.Generic.List[string]'
$nonSupportedRows = New-Object 'System.Collections.Generic.List[string]'

foreach ($apiName in ($uiApiNames | Sort-Object -Unique)) {
    if (-not $statusByApi.ContainsKey($apiName)) {
        $missingRows.Add($apiName)
        continue
    }

    $status = [string]$statusByApi[$apiName]
    if (-not $status.Equals('Supported', [System.StringComparison]::Ordinal)) {
        $nonSupportedRows.Add("$apiName ($status)")
    }
}

if ($missingRows.Count -gt 0 -or $nonSupportedRows.Count -gt 0) {
    if ($missingRows.Count -gt 0) {
        Write-Host 'UI public API rows missing from API-Compatibility.md:'
        foreach ($missingRow in $missingRows) {
            Write-Host "  - $missingRow"
        }
    }

    if ($nonSupportedRows.Count -gt 0) {
        Write-Host 'UI public API rows that are not Supported:'
        foreach ($nonSupportedRow in $nonSupportedRows) {
            Write-Host "  - $nonSupportedRow"
        }
    }

    throw 'UI public API gate failed. All UI and Text API rows must be Supported before editor work starts.'
}

Write-Host "UI public API gate verification passed. UI/Text public types: $($uiApiNames.Count)."
