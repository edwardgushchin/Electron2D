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
$projectRoot = Join-Path $repoRoot 'examples/ui-heavy-reference'
$projectPath = Join-Path $projectRoot 'Electron2D.UiHeavyReference.csproj'
$cliProjectPath = Join-Path $repoRoot 'src/Electron2D.Cli/Electron2D.Cli.csproj'
$workRoot = Join-Path $repoRoot '.temp/ui-heavy-reference'
$progressPath = Join-Path $workRoot 'progress.json'
$webOutput = Join-Path $workRoot 'web'

function Assert-File([string]$relativePath) {
    $path = Join-Path $projectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "UI-heavy reference required file was not found: $relativePath"
    }
}

function Assert-LocalProjectPath([string]$relativePath) {
    if ([System.String]::IsNullOrWhiteSpace($relativePath)) {
        throw 'UI-heavy reference resource path must not be empty.'
    }

    if ($relativePath -match '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "UI-heavy reference resource path must be local, not a URL: $relativePath"
    }

    if ($relativePath -match '\\') {
        throw "UI-heavy reference resource path must use forward slashes: $relativePath"
    }

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
    $rootPath = [System.IO.Path]::GetFullPath($projectRoot)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "UI-heavy reference resource path escapes project root: $relativePath"
    }

    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "UI-heavy reference resource file was not found: $relativePath"
    }

    return $fullPath
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
        $buffer = New-Object byte[] 4
        [void]$stream.Read($buffer, 0, 4)
        if ([System.Text.Encoding]::ASCII.GetString($buffer) -ne 'OggS') {
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
        $buffer = New-Object byte[] 4
        [void]$stream.Read($buffer, 0, 4)
        $tag = [System.Text.Encoding]::ASCII.GetString($buffer)
        $isTrueType = $buffer[0] -eq 0x00 -and $buffer[1] -eq 0x01 -and $buffer[2] -eq 0x00 -and $buffer[3] -eq 0x00
        if (-not $isTrueType -and $tag -ne 'OTTO') {
            throw "TTF signature is invalid: $path"
        }
    }
    finally {
        $stream.Dispose()
    }
}

foreach ($relativePath in @(
    'Electron2D.UiHeavyReference.csproj',
    'Program.cs',
    'Scripts/CardPuzzleGame.cs',
    'project.e2d.json',
    'electron2d.lock.json',
    'global.json',
    'export_presets.e2export.json',
    'scenes/menu.scene.json',
    'scenes/game.scene.json',
    'scenes/result.scene.json',
    'resources/ui-heavy-reference.manifest.json',
    '.electron2d/tasks/board.e2tasks',
    '.electron2d/tasks/ui-heavy-acceptance.e2task'
)) {
    Assert-File $relativePath
}

foreach ($forbiddenPath in @('TASKS.md', 'dev-diary', 'completed-tasks')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $forbiddenPath)) {
        throw "UI-heavy reference must not contain repository workflow path: $forbiddenPath"
    }
}

$settings = Get-Content -LiteralPath (Join-Path $projectRoot 'project.e2d.json') -Raw | ConvertFrom-Json
if ($settings.format -ne 'Electron2D.ProjectSettings' -or $settings.name -ne 'Electron2D.UiHeavyReference') {
    throw 'UI-heavy reference project settings are invalid.'
}

$actionNames = @($settings.input.actions | ForEach-Object { [string]$_.name } | Sort-Object)
$expectedActions = @('accept', 'cancel', 'next_card', 'previous_card', 'switch_locale')
if (($actionNames -join ',') -ne ($expectedActions -join ',')) {
    throw "UI-heavy reference Input Map mismatch. Expected $($expectedActions -join ','), got $($actionNames -join ',')."
}

$presets = Get-Content -LiteralPath (Join-Path $projectRoot 'export_presets.e2export.json') -Raw | ConvertFrom-Json
if ($presets.format -ne 'Electron2D.ExportPresets') {
    throw 'UI-heavy reference export presets format is invalid.'
}

$targets = @($presets.presets | ForEach-Object { [string]$_.target } | Sort-Object)
$expectedTargets = @('AndroidArm64', 'IosArm64', 'LinuxX64', 'MacOSArm64', 'WebAssemblyBrowser', 'WindowsX64')
if (($targets -join ',') -ne ($expectedTargets -join ',')) {
    throw "UI-heavy reference export target mismatch. Expected $($expectedTargets -join ','), got $($targets -join ',')."
}

$androidPreset = @($presets.presets | Where-Object { $_.target -eq 'AndroidArm64' })[0]
if ($null -eq $androidPreset -or $androidPreset.rendererProfile -ne 'Compatibility') {
    throw 'UI-heavy reference Android preset must use renderer profile Compatibility.'
}

$board = Get-Content -LiteralPath (Join-Path $projectRoot '.electron2d/tasks/board.e2tasks') -Raw | ConvertFrom-Json
$task = Get-Content -LiteralPath (Join-Path $projectRoot '.electron2d/tasks/ui-heavy-acceptance.e2task') -Raw | ConvertFrom-Json
if ($board.format -ne 'Electron2D.TaskBoard' -or $task.format -ne 'Electron2D.TaskFile') {
    throw 'UI-heavy reference task metadata is invalid.'
}

if ($task.status -ne 'AwaitingAcceptance' -or $task.acceptanceState -ne 'AwaitingHumanAcceptance') {
    throw 'UI-heavy reference acceptance task must wait for human acceptance.'
}

$resourceManifest = Get-Content -LiteralPath (Join-Path $projectRoot 'resources/ui-heavy-reference.manifest.json') -Raw | ConvertFrom-Json
if ($resourceManifest.format -ne 'Electron2D.UiHeavyReference.Resources' -or $resourceManifest.networkRequiredDuringBuild -ne $false) {
    throw 'UI-heavy reference resource manifest is invalid.'
}

$roles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($resource in @($resourceManifest.resources)) {
    $fullPath = Assert-LocalProjectPath ([string]$resource.path)
    foreach ($role in @($resource.roles)) {
        [void]$roles.Add([string]$role)
    }

    switch ([System.IO.Path]::GetExtension([string]$resource.path).ToLowerInvariant()) {
        '.png' { Assert-Png $fullPath }
        '.ogg' { Assert-Ogg $fullPath }
        '.ttf' { Assert-Ttf $fullPath }
        '.json' { [void](Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json) }
        default { throw "Unsupported UI-heavy reference resource extension: $($resource.path)" }
    }
}

foreach ($requiredRole in @(
    'button-texture',
    'card-data',
    'localization',
    'locale-en',
    'locale-ru',
    'ui-font',
    'card-flip-audio',
    'reward-audio',
    'shared-checkbox',
    'slider-texture',
    'reward-icon'
)) {
    if (-not $roles.Contains($requiredRole)) {
        throw "UI-heavy reference resource manifest is missing role: $requiredRole"
    }
}

Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null

dotnet build $projectPath | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$previousSavePath = [System.Environment]::GetEnvironmentVariable('ELECTRON2D_UI_HEAVY_REFERENCE_SAVE')
[System.Environment]::SetEnvironmentVariable('ELECTRON2D_UI_HEAVY_REFERENCE_SAVE', $progressPath)
try {
    $runOutput = dotnet run --project $projectPath --no-build -- --verify
    if ($LASTEXITCODE -ne 0) {
        Write-Host $runOutput
        exit $LASTEXITCODE
    }
}
finally {
    [System.Environment]::SetEnvironmentVariable('ELECTRON2D_UI_HEAVY_REFERENCE_SAVE', $previousSavePath)
}

$joinedRunOutput = $runOutput -join [Environment]::NewLine
foreach ($requiredText in @(
    'UI-heavy reference scene loaded: scenes/menu.scene.json',
    'control=True',
    'containers=True',
    'basicControls=True',
    'structuredList=True',
    'localization=True',
    'resolutions=True',
    'touch=True',
    'text=True',
    'save=True',
    'sceneTransition=True',
    'androidCompatibility=True',
    'audio=True',
    'scene=menu,locale=en,score='
)) {
    if ($joinedRunOutput.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
        Write-Host $runOutput
        throw "UI-heavy reference run output does not contain expected text: $requiredText"
    }
}

if (-not (Test-Path -LiteralPath $progressPath -PathType Leaf)) {
    throw 'UI-heavy reference did not write progress save artifact.'
}

$validateOutput = dotnet run --project $cliProjectPath -- validate --project $projectRoot --format json
if ($LASTEXITCODE -ne 0) {
    Write-Host $validateOutput
    exit $LASTEXITCODE
}

$validateJson = $validateOutput -join [Environment]::NewLine | ConvertFrom-Json
if ($validateJson.succeeded -ne $true -or $validateJson.command -ne 'validate') {
    Write-Host $validateOutput
    throw 'UI-heavy reference e2d validate route did not succeed.'
}

$webBuildOutput = dotnet run --project $cliProjectPath -- export build-web --project $projectRoot --output $webOutput --skip-publish true --format json
if ($LASTEXITCODE -ne 0) {
    Write-Host $webBuildOutput
    exit $LASTEXITCODE
}

$webRoot = Join-Path $webOutput 'wwwroot'
if (-not (Test-Path -LiteralPath $webRoot -PathType Container)) {
    throw 'UI-heavy reference WebAssembly package did not create wwwroot.'
}

$forbiddenPackagedFiles = Get-ChildItem -LiteralPath $webRoot -Recurse -File |
    Where-Object { $_.FullName.Replace('\', '/').IndexOf('/.electron2d/tasks/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($forbiddenPackagedFiles) {
    throw 'UI-heavy reference WebAssembly package contains Editor task metadata.'
}

Write-Host "UI-heavy reference verification passed. Assets: $($resourceManifest.resources.Count), export targets: $($targets.Count)."
