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
$specPath = Join-Path $repoRoot 'docs/release-management/performance-budgets.md'
$docPath = Join-Path $repoRoot 'docs/release-management/performance-budgets.md'

foreach ($path in @($specPath, $docPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Performance budget document was not found: $path"
    }
}

$combined = (Get-Content -LiteralPath $specPath -Raw) + "`n" + (Get-Content -LiteralPath $docPath -Raw)
$requiredFragments = @(
    'Windows',
    'Linux',
    'macOS',
    'Android',
    'iOS',
    '60 FPS',
    '16.67 ms',
    'memory budget',
    '30-minute',
    'soak',
    'background/foreground',
    'mobile'
)

foreach ($fragment in $requiredFragments) {
    if ($combined.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Performance budget docs are missing required fragment: $fragment"
    }
}

Write-Host 'Performance budget documentation verification passed.'
