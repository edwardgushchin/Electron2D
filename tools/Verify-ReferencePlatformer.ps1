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
$projectRoot = Join-Path $repoRoot 'examples/reference-platformer'
$projectPath = Join-Path $projectRoot 'Electron2D.ReferencePlatformer.csproj'
$cliProjectPath = Join-Path $repoRoot 'src/Electron2D.Cli/Electron2D.Cli.csproj'
$workRoot = Join-Path $repoRoot '.temp/reference-platformer'
$progressPath = Join-Path $workRoot 'progress.json'
$webOutput = Join-Path $workRoot 'web'

function Assert-File([string]$relativePath) {
    $path = Join-Path $projectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Reference platformer required file was not found: $relativePath"
    }
}

function Assert-LocalProjectPath([string]$relativePath) {
    if ([System.String]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Reference platformer resource path must not be empty.'
    }

    if ($relativePath -match '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "Reference platformer resource path must be local, not a URL: $relativePath"
    }

    if ($relativePath -match '\\') {
        throw "Reference platformer resource path must use forward slashes: $relativePath"
    }

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
    $rootPath = [System.IO.Path]::GetFullPath($projectRoot)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Reference platformer resource path escapes project root: $relativePath"
    }

    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Reference platformer resource file was not found: $relativePath"
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
    'Electron2D.ReferencePlatformer.csproj',
    'Program.cs',
    'Scripts/PlatformerGame.cs',
    'project.e2d.json',
    'electron2d.lock.json',
    'global.json',
    'export_presets.e2export.json',
    'scenes/main.scene.json',
    'resources/reference-platformer.manifest.json',
    '.electron2d/tasks/board.e2tasks',
    '.electron2d/tasks/reference-platformer-acceptance.e2task'
)) {
    Assert-File $relativePath
}

foreach ($forbiddenPath in @('TASKS.md', 'dev-diary', 'completed-tasks')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $forbiddenPath)) {
        throw "Reference platformer must not contain repository workflow path: $forbiddenPath"
    }
}

$settings = Get-Content -LiteralPath (Join-Path $projectRoot 'project.e2d.json') -Raw | ConvertFrom-Json
if ($settings.format -ne 'Electron2D.ProjectSettings' -or $settings.name -ne 'Electron2D.ReferencePlatformer') {
    throw 'Reference platformer project settings are invalid.'
}

$actionNames = @($settings.input.actions | ForEach-Object { [string]$_.name } | Sort-Object)
$expectedActions = @('jump', 'move_left', 'move_right', 'pause')
if (($actionNames -join ',') -ne ($expectedActions -join ',')) {
    throw "Reference platformer Input Map mismatch. Expected $($expectedActions -join ','), got $($actionNames -join ',')."
}

$presets = Get-Content -LiteralPath (Join-Path $projectRoot 'export_presets.e2export.json') -Raw | ConvertFrom-Json
if ($presets.format -ne 'Electron2D.ExportPresets') {
    throw 'Reference platformer export presets format is invalid.'
}

$targets = @($presets.presets | ForEach-Object { [string]$_.target } | Sort-Object)
$expectedTargets = @('AndroidArm64', 'IosArm64', 'LinuxX64', 'MacOSArm64', 'WebAssemblyBrowser', 'WindowsX64')
if (($targets -join ',') -ne ($expectedTargets -join ',')) {
    throw "Reference platformer export target mismatch. Expected $($expectedTargets -join ','), got $($targets -join ',')."
}

$board = Get-Content -LiteralPath (Join-Path $projectRoot '.electron2d/tasks/board.e2tasks') -Raw | ConvertFrom-Json
$task = Get-Content -LiteralPath (Join-Path $projectRoot '.electron2d/tasks/reference-platformer-acceptance.e2task') -Raw | ConvertFrom-Json
if ($board.format -ne 'Electron2D.TaskBoard' -or $task.format -ne 'Electron2D.TaskFile') {
    throw 'Reference platformer task metadata is invalid.'
}

if ($task.status -ne 'AwaitingAcceptance' -or $task.acceptanceState -ne 'AwaitingHumanAcceptance') {
    throw 'Reference platformer acceptance task must wait for human acceptance.'
}

$resourceManifest = Get-Content -LiteralPath (Join-Path $projectRoot 'resources/reference-platformer.manifest.json') -Raw | ConvertFrom-Json
if ($resourceManifest.format -ne 'Electron2D.ReferencePlatformer.Resources' -or $resourceManifest.networkRequiredDuringBuild -ne $false) {
    throw 'Reference platformer resource manifest is invalid.'
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
        '.tmx' { [void]([xml](Get-Content -LiteralPath $fullPath -Raw)) }
        '.tsx' { [void]([xml](Get-Content -LiteralPath $fullPath -Raw)) }
        '.json' { [void](Get-Content -LiteralPath $fullPath -Raw | ConvertFrom-Json) }
        default { throw "Unsupported reference platformer resource extension: $($resource.path)" }
    }
}

foreach ($requiredRole in @(
    'tileset',
    'tilemap',
    'character-atlas',
    'animation',
    'jump-audio',
    'walk-audio',
    'checkpoint-audio',
    'source-level',
    'ui-font',
    'pause-menu-ui'
)) {
    if (-not $roles.Contains($requiredRole)) {
        throw "Reference platformer resource manifest is missing role: $requiredRole"
    }
}

Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null

dotnet build $projectPath | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$previousSavePath = [System.Environment]::GetEnvironmentVariable('ELECTRON2D_REFERENCE_PLATFORMER_SAVE')
[System.Environment]::SetEnvironmentVariable('ELECTRON2D_REFERENCE_PLATFORMER_SAVE', $progressPath)
try {
    $runOutput = dotnet run --project $projectPath --no-build -- --verify
    if ($LASTEXITCODE -ne 0) {
        Write-Host $runOutput
        exit $LASTEXITCODE
    }
}
finally {
    [System.Environment]::SetEnvironmentVariable('ELECTRON2D_REFERENCE_PLATFORMER_SAVE', $previousSavePath)
}

$joinedRunOutput = $runOutput -join [Environment]::NewLine
foreach ($requiredText in @(
    'Reference platformer scene loaded: scenes/main.scene.json',
    'tilemap=True',
    'oneWay=True',
    'character=True',
    'camera=True',
    'animation=True',
    'audio=True',
    'keyboard=True',
    'gamepad=True',
    'touch=True',
    'pause=True',
    'save=True',
    'checkpoint=checkpoint-01,coins=1'
)) {
    if ($joinedRunOutput.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
        Write-Host $runOutput
        throw "Reference platformer run output does not contain expected text: $requiredText"
    }
}

if (-not (Test-Path -LiteralPath $progressPath -PathType Leaf)) {
    throw 'Reference platformer did not write progress save artifact.'
}

$validateOutput = dotnet run --project $cliProjectPath -- validate --project $projectRoot --format json
if ($LASTEXITCODE -ne 0) {
    Write-Host $validateOutput
    exit $LASTEXITCODE
}

$validateJson = $validateOutput -join [Environment]::NewLine | ConvertFrom-Json
if ($validateJson.succeeded -ne $true -or $validateJson.command -ne 'validate') {
    Write-Host $validateOutput
    throw 'Reference platformer e2d validate route did not succeed.'
}

$webBuildOutput = dotnet run --project $cliProjectPath -- export build-web --project $projectRoot --output $webOutput --skip-publish true --format json
if ($LASTEXITCODE -ne 0) {
    Write-Host $webBuildOutput
    exit $LASTEXITCODE
}

$webRoot = Join-Path $webOutput 'wwwroot'
if (-not (Test-Path -LiteralPath $webRoot -PathType Container)) {
    throw 'Reference platformer WebAssembly package did not create wwwroot.'
}

$forbiddenPackagedFiles = Get-ChildItem -LiteralPath $webRoot -Recurse -File |
    Where-Object { $_.FullName.Replace('\', '/').IndexOf('/.electron2d/tasks/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($forbiddenPackagedFiles) {
    throw 'Reference platformer WebAssembly package contains Editor task metadata.'
}

Write-Host "Reference platformer verification passed. Assets: $($resourceManifest.resources.Count), export targets: $($targets.Count)."
