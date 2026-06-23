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

$requiredFiles = @(
    'docs/specifications/export/export-user-documentation.md',
    'docs/documentation/export/export-guide.md',
    'docs/documentation/export/export-preset-model.md',
    'docs/documentation/export/windows-x64-export.md',
    'docs/documentation/export/linux-x64-export.md',
    'docs/documentation/export/macos-arm64-export.md',
    'docs/documentation/export/android-arm64-export.md',
    'docs/documentation/export/ios-arm64-export.md',
    'docs/documentation/export/webassembly-browser-export.md',
    'docs/documentation/documentation/user-guide.md',
    'docs/documentation/README.md'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required export documentation file was not found: $relativePath"
    }
}

function Read-Doc([string]$relativePath) {
    return [System.IO.File]::ReadAllText((Join-Path $repoRoot $relativePath), [System.Text.Encoding]::UTF8)
}

function Assert-ContainsAll([string]$name, [string]$content, [string[]]$fragments) {
    foreach ($fragment in $fragments) {
        if ($content.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
            throw "$name is missing required fragment: $fragment"
        }
    }
}

$guide = Read-Doc 'docs/documentation/export/export-guide.md'
$windows = Read-Doc 'docs/documentation/export/windows-x64-export.md'
$linux = Read-Doc 'docs/documentation/export/linux-x64-export.md'
$macos = Read-Doc 'docs/documentation/export/macos-arm64-export.md'
$android = Read-Doc 'docs/documentation/export/android-arm64-export.md'
$ios = Read-Doc 'docs/documentation/export/ios-arm64-export.md'
$web = Read-Doc 'docs/documentation/export/webassembly-browser-export.md'
$userGuide = Read-Doc 'docs/documentation/documentation/user-guide.md'
$documentationIndex = Read-Doc 'docs/documentation/README.md'

Assert-ContainsAll 'Export guide' $guide @(
    '<!-- export-doc:overview -->',
    '<!-- export-doc:target-matrix -->',
    '<!-- export-doc:preset-file -->',
    '<!-- export-doc:desktop-verification -->',
    '<!-- export-doc:mobile-status -->',
    '<!-- export-doc:signing-credentials -->',
    '<!-- export-doc:known-limitations -->',
    'WindowsX64',
    'LinuxX64',
    'MacOSArm64',
    'AndroidArm64',
    'IosArm64',
    'WebAssemblyBrowser',
    'browser-wasm',
    'export-doc:web-status',
    'export build-web',
    'export run-web',
    'tools\Verify-WindowsExport.ps1',
    'tools\Verify-LinuxExport.ps1',
    'tools\Verify-MacOSExport.ps1'
)

Assert-ContainsAll 'Windows export documentation' $windows @(
    'WindowsX64',
    'win-x64',
    'Host requirements',
    'SDK and toolchain',
    'Signing and credentials',
    'Known limitations',
    'tools\Verify-WindowsExport.ps1'
)

Assert-ContainsAll 'Linux export documentation' $linux @(
    'LinuxX64',
    'linux-x64',
    'Host requirements',
    'SDK and toolchain',
    'Signing and credentials',
    'Known limitations',
    'tools\Verify-LinuxExport.ps1'
)

Assert-ContainsAll 'macOS export documentation' $macos @(
    'MacOSArm64',
    'osx-arm64',
    'Host requirements',
    'SDK and toolchain',
    'Signing and credentials',
    'Known limitations',
    'tools\Verify-MacOSExport.ps1'
)

Assert-ContainsAll 'Android export documentation' $android @(
    'AndroidArm64',
    'android-arm64',
    'Status',
    'SDK and toolchain',
    'Signing and credentials',
    'Known limitations',
    'not a ready release path'
)

Assert-ContainsAll 'iOS export documentation' $ios @(
    'IosArm64',
    'ios-arm64',
    'Status',
    'SDK and toolchain',
    'Signing and credentials',
    'Known limitations',
    'not a ready release path'
)

Assert-ContainsAll 'WebAssembly browser export documentation' $web @(
    'WebAssemblyBrowser',
    'browser-wasm',
    'Host requirements',
    'SDK and toolchain',
    'Signing and credentials',
    'Package layout',
    'Browser runtime policy',
    'CLI plan',
    'CLI build',
    'CLI run',
    'Known limitations',
    'e2d export plan-web',
    'e2d export build-web',
    'e2d export run-web',
    'Electron2D.WebAssemblySmokeArtifact'
)

Assert-ContainsAll 'User guide export section' $userGuide @(
    'export/export-guide.md',
    'android-arm64-export.md',
    'ios-arm64-export.md',
    'webassembly-browser-export.md',
    'export build-web',
    'export run-web',
    'tools\Verify-ExportDocumentation.ps1'
)

Assert-ContainsAll 'Documentation index' $documentationIndex @(
    'export/export-guide.md',
    'export/android-arm64-export.md',
    'export/ios-arm64-export.md',
    'export/webassembly-browser-export.md'
)

$combined = $guide + "`n" + $windows + "`n" + $linux + "`n" + $macos + "`n" + $android + "`n" + $ios + "`n" + $web + "`n" + $userGuide
$forbiddenPatterns = @(
    '(?i)password\s*[:=]\s*["''][^"'']+["'']',
    '(?i)token\s*[:=]\s*["''][^"'']+["'']',
    '(?i)private[_ -]?key\s*[:=]\s*["''][^"'']+["'']',
    '(?i)BEGIN (RSA |EC |OPENSSH )?PRIVATE KEY',
    '(?i)BEGIN CERTIFICATE',
    '(?i)Android.*is a ready release path',
    '(?i)iOS.*is a ready release path',
    '(?i)WebAssembly.*is a ready release path',
    '\bSDL\b',
    'Godot-like',
    'godot-like'
)

foreach ($pattern in $forbiddenPatterns) {
    if ($combined -match $pattern) {
        throw "Export documentation contains forbidden pattern: $pattern"
    }
}

if ($combined -match '(?i)\b(todo|tbd)\b') {
    throw 'Export documentation contains TODO/TBD placeholder text.'
}

Write-Host 'Export documentation verification passed.'
