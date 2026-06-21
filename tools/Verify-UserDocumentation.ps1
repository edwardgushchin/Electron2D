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
$specPath = Join-Path $repoRoot 'docs/specifications/documentation/user-documentation.md'
$rendererSpecPath = Join-Path $repoRoot 'docs/specifications/documentation/renderer-profiles-user-documentation.md'
$troubleshootingSpecPath = Join-Path $repoRoot 'docs/specifications/documentation/troubleshooting-release-checklist.md'
$guidePath = Join-Path $repoRoot 'docs/documentation/documentation/user-guide.md'
$rendererGuidePath = Join-Path $repoRoot 'docs/documentation/documentation/renderer-profiles.md'
$troubleshootingGuidePath = Join-Path $repoRoot 'docs/documentation/documentation/troubleshooting-release-checklist.md'

foreach ($path in @($specPath, $rendererSpecPath, $troubleshootingSpecPath, $guidePath, $rendererGuidePath, $troubleshootingGuidePath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required user documentation file was not found: $path"
    }
}

$guide = [System.IO.File]::ReadAllText($guidePath, [System.Text.Encoding]::UTF8)
$spec = [System.IO.File]::ReadAllText($specPath, [System.Text.Encoding]::UTF8)
$rendererGuide = [System.IO.File]::ReadAllText($rendererGuidePath, [System.Text.Encoding]::UTF8)
$rendererSpec = [System.IO.File]::ReadAllText($rendererSpecPath, [System.Text.Encoding]::UTF8)
$troubleshootingGuide = [System.IO.File]::ReadAllText($troubleshootingGuidePath, [System.Text.Encoding]::UTF8)
$troubleshootingSpec = [System.IO.File]::ReadAllText($troubleshootingSpecPath, [System.Text.Encoding]::UTF8)

$requiredSectionMarkers = @(
    'user-doc:installation',
    'user-doc:first-project',
    'user-doc:first-scene',
    'user-doc:scripting',
    'user-doc:resources',
    'user-doc:physics',
    'user-doc:ui',
    'user-doc:animation',
    'user-doc:input-map',
    'user-doc:renderer-profiles',
    'user-doc:export',
    'user-doc:troubleshooting',
    'user-doc:release-checklist'
)

foreach ($marker in $requiredSectionMarkers) {
    if ($guide.IndexOf($marker, [System.StringComparison]::Ordinal) -lt 0) {
        throw "User guide is missing required section marker: $marker"
    }
}

$requiredFragments = @(
    'tools\Verify-ProjectTemplate.ps1',
    'tools\Verify-UserDocumentation.ps1',
    'dotnet run',
    'project.e2d.json',
    'scenes/main.scene.json',
    'Scripts/MainScene.cs',
    'InputMap',
    'renderer-profiles.md',
    'RenderingServer.CurrentProfile',
    'RenderingServer.HasFeature',
    'troubleshooting-release-checklist.md',
    'tools\Verify-WindowsExport.ps1',
    'tools\Verify-LinuxExport.ps1',
    'tools\Verify-MacOSExport.ps1',
    'tools\Run-Tests.ps1'
)

foreach ($fragment in $requiredFragments) {
    if ($guide.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
        throw "User guide is missing required fragment: $fragment"
    }
}

$requiredRendererFragments = @(
    'Compatibility',
    'Standard',
    'feature flags',
    'Automatic',
    'FailIfUnavailable',
    'fail_if_unavailable',
    'Android fallback',
    'RenderingServer.CurrentProfile',
    'RenderingServer.HasFeature'
)

foreach ($fragment in $requiredRendererFragments) {
    if ($rendererGuide.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Renderer profile documentation is missing required fragment: $fragment"
    }
}

$requiredTroubleshootingFragments = @(
    'import',
    'build',
    'shader',
    'export',
    'mobile lifecycle',
    'runtime diagnostics',
    'release checklist',
    'tools\Verify-ProjectTemplate.ps1',
    'tools\Run-Tests.ps1',
    'tools\Verify-UserDocumentation.ps1',
    'tools\Verify-WindowsExport.ps1',
    'tools\Verify-LinuxExport.ps1',
    'tools\Verify-MacOSExport.ps1',
    'GitHub Release'
)

foreach ($fragment in $requiredTroubleshootingFragments) {
    if ($troubleshootingGuide.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Troubleshooting documentation is missing required fragment: $fragment"
    }
}

$combined = $guide + "`n" + $spec + "`n" + $rendererGuide + "`n" + $rendererSpec + "`n" + $troubleshootingGuide + "`n" + $troubleshootingSpec
$forbiddenPatterns = @(
    '\bSDL\b',
    'Godot',
    'Godot-like',
    'godot-like'
)

foreach ($pattern in $forbiddenPatterns) {
    if ($combined -match $pattern) {
        throw "User documentation contains forbidden public wording pattern: $pattern"
    }
}

if ($combined -match '(?i)\b(todo|tbd)\b') {
    throw 'User documentation contains TODO/TBD placeholder text.'
}

$imageMatches = [regex]::Matches($guide, '!\[[^\]]*\]\(([^)]+)\)')
foreach ($match in $imageMatches) {
    $target = $match.Groups[1].Value.Trim()
    if ($target.StartsWith('http://', [System.StringComparison]::OrdinalIgnoreCase) -or
        $target.StartsWith('https://', [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    $resolved = Join-Path (Split-Path -Parent $guidePath) $target
    if (-not (Test-Path -LiteralPath $resolved)) {
        throw "User guide image link points to a missing file: $target"
    }
}

Write-Host 'User documentation verification passed.'
