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
    'docs/export/export-guide.md',
    'docs/export/export-guide.md',
    'docs/export/export-preset-model.md',
    'docs/export/windows-x64-export.md',
    'docs/export/linux-x64-export.md',
    'docs/export/macos-arm64-export.md',
    'docs/export/android-arm64-export.md',
    'docs/export/ios-arm64-export.md',
    'docs/export/webassembly-browser-export.md',
    'docs/documentation/user-guide.md',
    'docs/README.md'
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

function New-Text([int[]]$codePoints) {
    $characters = foreach ($codePoint in $codePoints) {
        [char]$codePoint
    }

    return -join $characters
}

$guide = Read-Doc 'docs/export/export-guide.md'
$windows = Read-Doc 'docs/export/windows-x64-export.md'
$linux = Read-Doc 'docs/export/linux-x64-export.md'
$macos = Read-Doc 'docs/export/macos-arm64-export.md'
$android = Read-Doc 'docs/export/android-arm64-export.md'
$ios = Read-Doc 'docs/export/ios-arm64-export.md'
$web = Read-Doc 'docs/export/webassembly-browser-export.md'
$userGuide = Read-Doc 'docs/documentation/user-guide.md'
$documentationIndex = Read-Doc 'docs/README.md'

$iosStatusHeading = New-Text @(0x0421, 0x0442, 0x0430, 0x0442, 0x0443, 0x0441)
$iosSdkHeading = New-Text @(0x0053, 0x0044, 0x004B, 0x0020, 0x0438, 0x0020, 0x0074, 0x006F, 0x006F, 0x006C, 0x0063, 0x0068, 0x0061, 0x0069, 0x006E)
$iosSigningHeading = New-Text @(0x0053, 0x0069, 0x0067, 0x006E, 0x0069, 0x006E, 0x0067, 0x0020, 0x0438, 0x0020, 0x0063, 0x0072, 0x0065, 0x0064, 0x0065, 0x006E, 0x0074, 0x0069, 0x0061, 0x006C, 0x0073)
$iosLimitationsHeading = New-Text @(0x0418, 0x0437, 0x0432, 0x0435, 0x0441, 0x0442, 0x043D, 0x044B, 0x0435, 0x0020, 0x043E, 0x0433, 0x0440, 0x0430, 0x043D, 0x0438, 0x0447, 0x0435, 0x043D, 0x0438, 0x044F)
$iosNotReadyReleasePath = New-Text @(0x043D, 0x0435, 0x0020, 0x0433, 0x043E, 0x0442, 0x043E, 0x0432, 0x044B, 0x0439, 0x0020, 0x0072, 0x0065, 0x006C, 0x0065, 0x0061, 0x0073, 0x0065, 0x0020, 0x0070, 0x0061, 0x0074, 0x0068)

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
    'export plan-ios',
    'export build-ios',
    'export run-ios',
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
    $iosStatusHeading,
    $iosSdkHeading,
    $iosSigningHeading,
    $iosLimitationsHeading,
    $iosNotReadyReleasePath,
    'export plan-ios',
    'export build-ios',
    'export run-ios',
    'Electron2D.IosDeviceSmokeArtifact',
    'E2D-EXPORT-IOS-0011'
)

Assert-ContainsAll 'WebAssembly browser export documentation' $web @(
    'WebAssemblyBrowser',
    'browser-wasm',
    'WebAssemblyBuildToolsAvailable',
    'Electron2DWebAssemblyExportPlanner.CreatePlan',
    'Electron2DWebAssemblyPackageBuilder.Build',
    'Electron2D.WebAssemblySmokeArtifact',
    'window.Electron2DWebRuntimeSmoke.run()',
    'CLI route `plan-web`',
    'CLI route `build-web`',
    'CLI route `run-web`',
    'E2D-EXPORT-WEB-0013',
    'e2d export plan-web',
    'e2d export build-web',
    'e2d export run-web'
)

Assert-ContainsAll 'User guide export section' $userGuide @(
    'export/export-guide.md',
    'android-arm64-export.md',
    'ios-arm64-export.md',
    'webassembly-browser-export.md',
    'export build-web',
    'export run-web',
    'export plan-ios',
    'export build-ios',
    'export run-ios',
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
