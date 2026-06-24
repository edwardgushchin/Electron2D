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
$projectRoot = Join-Path $repoRoot 'examples/platformer'
$projectPath = Join-Path $projectRoot 'Platformer.csproj'
$cliProjectPath = Join-Path $repoRoot 'src/Electron2D.Cli/Electron2D.Cli.csproj'
$workRoot = Join-Path $repoRoot '.temp/platformer'
$progressPath = Join-Path $workRoot 'progress.json'
$webOutput = Join-Path $workRoot 'web'

function Assert-File([string]$relativePath) {
    $path = Join-Path $projectRoot $relativePath
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Platformer required file was not found: $relativePath"
    }
}

function Assert-LocalProjectPath([string]$relativePath) {
    if ([System.String]::IsNullOrWhiteSpace($relativePath)) {
        throw 'Platformer resource path must not be empty.'
    }

    if ($relativePath -match '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "Platformer resource path must be local, not a URL: $relativePath"
    }

    if ($relativePath -match '\\') {
        throw "Platformer resource path must use forward slashes: $relativePath"
    }

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $projectRoot $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
    $rootPath = [System.IO.Path]::GetFullPath($projectRoot)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Platformer resource path escapes project root: $relativePath"
    }

    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
        throw "Platformer resource file was not found: $relativePath"
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

function Assert-PngMinDimensions([string]$path, [int]$minWidth, [int]$minHeight) {
    Assert-Png $path
    $bytes = [System.IO.File]::ReadAllBytes($path)
    if ($bytes.Length -lt 24) {
        throw "PNG file is too small: $path"
    }

    $width = ([int]$bytes[16] -shl 24) -bor ([int]$bytes[17] -shl 16) -bor ([int]$bytes[18] -shl 8) -bor [int]$bytes[19]
    $height = ([int]$bytes[20] -shl 24) -bor ([int]$bytes[21] -shl 16) -bor ([int]$bytes[22] -shl 8) -bor [int]$bytes[23]
    if ($width -lt $minWidth -or $height -lt $minHeight) {
        throw "PNG dimensions are too small: $path ($width x $height), expected at least $minWidth x $minHeight."
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
    'Platformer.csproj',
    'scripts/PlatformerGame.cs',
    'Platformer.e2d',
    'global.json',
    'scenes/main.scene.json',
    'resources/platformer.manifest.json',
    '.electron2d/tasks/board.e2tasks',
    '.electron2d/tasks/platformer-acceptance.e2task'
)) {
    Assert-File $relativePath
}

foreach ($forbiddenPath in @('TASKS.md', 'dev-diary', 'completed-tasks')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $forbiddenPath)) {
        throw "Platformer must not contain repository workflow path: $forbiddenPath"
    }
}

$settings = Get-Content -LiteralPath (Join-Path $projectRoot 'Platformer.e2d') -Raw | ConvertFrom-Json
if ($settings.format -ne 'Electron2D.ProjectSettings' -or $settings.name -ne 'Platformer') {
    throw 'Platformer project settings are invalid.'
}

$actionNames = @($settings.input.actions | ForEach-Object { [string]$_.name } | Sort-Object)
$expectedActions = @('jump', 'move_left', 'move_right', 'pause')
if (($actionNames -join ',') -ne ($expectedActions -join ',')) {
    throw "Platformer Input Map mismatch. Expected $($expectedActions -join ','), got $($actionNames -join ',')."
}

$presets = $settings.exportPresets
if ($presets.format -ne 'Electron2D.ExportPresets') {
    throw 'Platformer export presets format is invalid.'
}

$targets = @($presets.presets | ForEach-Object { [string]$_.target } | Sort-Object)
$expectedTargets = @('AndroidArm64', 'IosArm64', 'LinuxX64', 'MacOSArm64', 'WebAssemblyBrowser', 'WindowsX64')
if (($targets -join ',') -ne ($expectedTargets -join ',')) {
    throw "Platformer export target mismatch. Expected $($expectedTargets -join ','), got $($targets -join ',')."
}

$board = Get-Content -LiteralPath (Join-Path $projectRoot '.electron2d/tasks/board.e2tasks') -Raw | ConvertFrom-Json
$task = Get-Content -LiteralPath (Join-Path $projectRoot '.electron2d/tasks/platformer-acceptance.e2task') -Raw | ConvertFrom-Json
if ($board.format -ne 'Electron2D.TaskBoard' -or $task.format -ne 'Electron2D.TaskFile') {
    throw 'Platformer task metadata is invalid.'
}

if ($task.status -ne 'AwaitingAcceptance' -or $task.acceptanceState -ne 'AwaitingHumanAcceptance') {
    throw 'Platformer acceptance task must wait for human acceptance.'
}

$resourceManifest = Get-Content -LiteralPath (Join-Path $projectRoot 'resources/platformer.manifest.json') -Raw | ConvertFrom-Json
if ($resourceManifest.format -ne 'Platformer.Resources' -or $resourceManifest.networkRequiredDuringBuild -ne $false) {
    throw 'Platformer resource manifest is invalid.'
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
        default { throw "Unsupported Platformer resource extension: $($resource.path)" }
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
        throw "Platformer resource manifest is missing role: $requiredRole"
    }
}

$programPath = Join-Path $projectRoot 'Program.cs'
if (Test-Path -LiteralPath $programPath) {
    throw 'Platformer must not provide Program.cs; editor/dev run and export own the launch flow.'
}

$source = Get-Content -LiteralPath (Join-Path $projectRoot 'scripts/PlatformerGame.cs') -Raw
foreach ($forbiddenText in @(
    'Console.ReadKey',
    'FRAME platformer',
    'SDL.',
    'SDL3',
    'CreateWindow',
    'RuntimeHost.Run',
    'ProjectRuntimeRunner'
)) {
    if ($source.IndexOf($forbiddenText, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Platformer project script must use only public Electron2D game API. Forbidden text: $forbiddenText"
    }
}

foreach ($forbiddenApi in @('Electron2DApplication', 'Electron2DRunOptions', 'Electron2DRunResult')) {
    if ($source.IndexOf($forbiddenApi, [System.StringComparison]::Ordinal) -ge 0) {
        throw "Platformer must not use out-of-profile public bootstrap API '$forbiddenApi'."
    }
}

Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null

dotnet build $projectPath | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$previousSavePath = [System.Environment]::GetEnvironmentVariable('ELECTRON2D_PLATFORMER_SAVE')
[System.Environment]::SetEnvironmentVariable('ELECTRON2D_PLATFORMER_SAVE', $progressPath)
try {
    $runOutput = dotnet run --project $cliProjectPath -- run --project $projectRoot --play-script "right,right,save,quit"
    if ($LASTEXITCODE -ne 0) {
        Write-Host $runOutput
        exit $LASTEXITCODE
    }
}
finally {
    [System.Environment]::SetEnvironmentVariable('ELECTRON2D_PLATFORMER_SAVE', $previousSavePath)
}

$joinedRunOutput = $runOutput -join [Environment]::NewLine
foreach ($requiredText in @(
    'Mode=playable',
    'Playable=True',
    'CommandsApplied=4',
    'Checkpoint=checkpoint-01',
    'Coins=1',
    'WindowCreated=True',
    'WindowShown=True',
    'FramePresented=True'
)) {
    if ($joinedRunOutput.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
        Write-Host $runOutput
        throw "Platformer run output does not contain expected text: $requiredText"
    }
}

if (-not (Test-Path -LiteralPath $progressPath -PathType Leaf)) {
    throw 'Platformer did not write progress save artifact.'
}

$playableSavePath = Join-Path $workRoot 'playable-progress.json'
$playableScreenshotPath = Join-Path $workRoot 'platformer-playable.png'
$previousSavePath = [System.Environment]::GetEnvironmentVariable('ELECTRON2D_PLATFORMER_SAVE')
[System.Environment]::SetEnvironmentVariable('ELECTRON2D_PLATFORMER_SAVE', $playableSavePath)
try {
    $playableOutput = dotnet run --project $cliProjectPath -- run --project $projectRoot --play-script "right,jump,right,pause,save,quit" --screenshot $playableScreenshotPath
    if ($LASTEXITCODE -ne 0) {
        Write-Host $playableOutput
        exit $LASTEXITCODE
    }
}
finally {
    [System.Environment]::SetEnvironmentVariable('ELECTRON2D_PLATFORMER_SAVE', $previousSavePath)
}

$joinedPlayableOutput = $playableOutput -join [Environment]::NewLine
foreach ($requiredText in @(
    'Mode=playable',
    'Playable=True',
    'FramesAdvanced=5',
    'CommandsApplied=6',
    'Checkpoint=checkpoint-01',
    'Coins=1',
    'Paused=True',
    'WindowCreated=True',
    'WindowShown=True',
    'FramePresented=True',
    'InputEventsDispatched=',
    'DrawCommands=',
    "ScreenshotPath=$playableScreenshotPath"
)) {
    if ($joinedPlayableOutput.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
        Write-Host $playableOutput
        throw "Platformer playable output does not contain expected text: $requiredText"
    }
}

if ($joinedPlayableOutput.IndexOf('FRAME ', [System.StringComparison]::Ordinal) -ge 0) {
    Write-Host $playableOutput
    throw 'Platformer playable output must not contain ASCII frame output.'
}

$drawCommandsMatch = [System.Text.RegularExpressions.Regex]::Match($joinedPlayableOutput, 'DrawCommands=(\d+)')
if (-not $drawCommandsMatch.Success -or [int]$drawCommandsMatch.Groups[1].Value -le 0) {
    Write-Host $playableOutput
    throw 'Platformer playable output must report DrawCommands greater than zero.'
}

if (-not (Test-Path -LiteralPath $playableSavePath -PathType Leaf)) {
    throw 'Platformer playable mode did not write progress save artifact.'
}

if (-not (Test-Path -LiteralPath $playableScreenshotPath -PathType Leaf)) {
    throw 'Platformer playable mode did not write PNG screenshot artifact.'
}

Assert-PngMinDimensions $playableScreenshotPath 640 360

$validateOutput = dotnet run --project $cliProjectPath -- validate --project $projectRoot --format json
if ($LASTEXITCODE -ne 0) {
    Write-Host $validateOutput
    exit $LASTEXITCODE
}

$validateJson = $validateOutput -join [Environment]::NewLine | ConvertFrom-Json
if ($validateJson.succeeded -ne $true -or $validateJson.command -ne 'validate') {
    Write-Host $validateOutput
    throw 'Platformer e2d validate route did not succeed.'
}

$webBuildOutput = dotnet run --project $cliProjectPath -- export build-web --project $projectRoot --output $webOutput --skip-publish true --format json
if ($LASTEXITCODE -ne 0) {
    Write-Host $webBuildOutput
    exit $LASTEXITCODE
}

$webRoot = Join-Path $webOutput 'wwwroot'
if (-not (Test-Path -LiteralPath $webRoot -PathType Container)) {
    throw 'Platformer WebAssembly package did not create wwwroot.'
}

$forbiddenPackagedFiles = Get-ChildItem -LiteralPath $webRoot -Recurse -File |
    Where-Object { $_.FullName.Replace('\', '/').IndexOf('/.electron2d/tasks/', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 }
if ($forbiddenPackagedFiles) {
    throw 'Platformer WebAssembly package contains Editor task metadata.'
}

Write-Host "Platformer verification passed. Assets: $($resourceManifest.resources.Count), export targets: $($targets.Count)."
