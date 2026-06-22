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
$releaseSpecPath = Join-Path $repoRoot 'docs/specifications/releases/0.1.0-preview.md'
$aiWorkflowSpecPath = Join-Path $repoRoot 'docs/specifications/architecture/ai-friendly-workflow.md'
$engineStackSpecPath = Join-Path $repoRoot 'docs/specifications/architecture/engine-platform-stack.md'

foreach ($path in @($releaseSpecPath, $aiWorkflowSpecPath, $engineStackSpecPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required canonical goal document was not found: $path"
    }
}

$releaseSpec = [System.IO.File]::ReadAllText($releaseSpecPath, [System.Text.Encoding]::UTF8)
$aiWorkflowSpec = [System.IO.File]::ReadAllText($aiWorkflowSpecPath, [System.Text.Encoding]::UTF8)
$engineStackSpec = [System.IO.File]::ReadAllText($engineStackSpecPath, [System.Text.Encoding]::UTF8)

function Assert-Contains {
    param(
        [Parameter(Mandatory = $true)][string] $Content,
        [Parameter(Mandatory = $true)][string] $Fragment,
        [Parameter(Mandatory = $true)][string] $Description
    )

    if ($Content.IndexOf($Fragment, [System.StringComparison]::Ordinal) -lt 0) {
        throw "$Description is missing required fragment: $Fragment"
    }
}

Assert-Contains -Content $releaseSpec -Fragment 'Windows, Linux, macOS, Android' -Description 'Release canonical contract'
Assert-Contains -Content $releaseSpec -Fragment 'iOS' -Description 'Release canonical contract'
Assert-Contains -Content $releaseSpec -Fragment 'node/resource' -Description 'Release canonical contract'
Assert-Contains -Content $releaseSpec -Fragment 'SpriteRenderer' -Description 'Release legacy exclusion record'
Assert-Contains -Content $aiWorkflowSpec -Fragment '`Node2D`' -Description 'AI-friendly canonical architecture'
Assert-Contains -Content $aiWorkflowSpec -Fragment 'transform' -Description 'AI-friendly canonical architecture'
Assert-Contains -Content $aiWorkflowSpec -Fragment '`scene_attach_script`' -Description 'AI-friendly canonical architecture'
Assert-Contains -Content $aiWorkflowSpec -Fragment 'Windows, Linux, macOS, Android' -Description 'AI-friendly platform contract'
Assert-Contains -Content $aiWorkflowSpec -Fragment 'iOS' -Description 'AI-friendly platform contract'

Assert-Contains -Content $engineStackSpec -Fragment 'Synchronized with `docs/specifications/releases/0.1.0-preview.md`' -Description 'Engine platform stack canonical status'
Assert-Contains -Content $engineStackSpec -Fragment 'Windows, Linux, macOS, Android' -Description 'Engine platform stack canonical status'
Assert-Contains -Content $engineStackSpec -Fragment 'iOS' -Description 'Engine platform stack canonical status'
Assert-Contains -Content $engineStackSpec -Fragment 'specialized node/resource model' -Description 'Engine platform stack canonical status'
Assert-Contains -Content $engineStackSpec -Fragment '`Node2D` transform' -Description 'Engine platform stack canonical status'
Assert-Contains -Content $engineStackSpec -Fragment '`scene_attach_script`' -Description 'Engine platform stack canonical status'

$forbiddenEnginePatterns = @(
    '\bSDL\b',
    '\bSDL3\b',
    'SDL3-CS',
    'SDL_GPU',
    'SDL_Renderer',
    'SDL_ttf',
    'SDL_mixer',
    'SDL_shadercross',
    '\bbgfx\b',
    '\bminiaudio\b',
    '\bPhysicsFS\b'
)

foreach ($pattern in $forbiddenEnginePatterns) {
    if ($engineStackSpec -match $pattern) {
        throw "Engine platform stack must describe canonical internal backends, not old backend-specific public wording: $pattern"
    }
}

$trackedGoalFiles = git -C $repoRoot ls-files GOAL.md GOAL-0.1.0.md 2>$null
if ($trackedGoalFiles) {
    throw "Root goal files must not return as tracked canonical sources: $($trackedGoalFiles -join ', ')"
}

Write-Host 'Canonical goal alignment verification passed.'
