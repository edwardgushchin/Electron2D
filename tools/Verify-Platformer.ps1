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
    '.electron2d/tasks/platformer-acceptance.e2task',
    '.electron2d/tasks/T-0166.e2task',
    '.electron2d/tasks/T-0221.e2task',
    '.electron2d/tasks/T-0222.e2task',
    '.electron2d/tasks/T-0223.e2task',
    '.electron2d/tasks/T-0225.e2task'
)) {
    Assert-File $relativePath
}

foreach ($forbiddenPath in @('TASKS.md', 'dev-diary', 'completed-tasks')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $forbiddenPath)) {
        throw "Platformer must not contain repository workflow path: $forbiddenPath"
    }
}

function Read-Utf8Json([string]$path) {
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8) | ConvertFrom-Json
}

$settings = Read-Utf8Json (Join-Path $projectRoot 'Platformer.e2d')
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

$taskRoot = Join-Path $projectRoot '.electron2d/tasks'
$board = Read-Utf8Json (Join-Path $taskRoot 'board.e2tasks')
if ($board.format -ne 'Electron2D.TaskBoard' -or $board.version -ne 1) {
    throw 'Platformer task metadata is invalid.'
}

function Decode-Utf8Base64([string]$value) {
    return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($value))
}

$expectedTaskMetadata = [ordered]@{
    'T-0222' = @{
        Status = 'Ready'
        Title = (Decode-Utf8Base64 '0J/QtdGA0LXRgdC+0LHRgNCw0YLRjCBQbGF0Zm9ybWVyINC60LDQuiDQt9Cw0LrQvtC90YfQtdC90L3Rg9GOINC/0YDQuNGR0LzQvtGH0L3Rg9GOINC40LPRgNGD')
        Priority = 'P0'
        Dependencies = @()
        Subtask = (Decode-Utf8Base64 '0J7RgtC60YDRi9GC0L46INCf0LXRgNC10YHQvtCx0YDQsNGC0Ywgc2NlbmUgZGF0YSDQuCBnYW1lcGxheSBzY3JpcHQg0LHQtdC3INGA0YPRh9C90YvRhSB2ZXJpZmllciBzaG9ydGN1dHMu')
        Activity = (Decode-Utf8Base64 'MjAyNi0wNi0yNFQxOToyMDowMCswMzowMCAtINCh0L7Qt9C00LDQvdC+INC/0L4g0L/QvtC70YzQt9C+0LLQsNGC0LXQu9GM0YHQutC+0LzRgyBhdWRpdCByZWplY3Rpb24u')
    }
    'T-0223' = @{
        Status = 'Blocked'
        Title = (Decode-Utf8Base64 '0J/QtdGA0LXQv9C40YHQsNGC0YwgUGxhdGZvcm1lciBhY2NlcHRhbmNlINCx0LXQtyDRgdCw0LzQvtC/0YDQvtCy0LXRgNC60Lg=')
        Priority = 'P0'
        Dependencies = @('T-0222')
        Subtask = (Decode-Utf8Base64 '0J7RgtC60YDRi9GC0L46INCf0LXRgNC10L/QuNGB0LDRgtGMIHBsYXktc2NyaXB0L3ZlcmlmaWVyIHBhdGgg0L3QsCDQvdCw0LHQu9GO0LTQtdC90LjQtSBnYW1lcGxheSBldmVudHMu')
        Activity = (Decode-Utf8Base64 'MjAyNi0wNi0yNFQxOToyMDowMCswMzowMCAtINCh0L7Qt9C00LDQvdC+INC/0L4g0L/QvtC70YzQt9C+0LLQsNGC0LXQu9GM0YHQutC+0LzRgyBhdWRpdCByZWplY3Rpb24u')
    }
    'T-0225' = @{
        Status = 'Blocked'
        Title = (Decode-Utf8Base64 '0JLRi9C90LXRgdGC0LggUGxhdGZvcm1lciB2aXN1YWwgZ2F0ZSDQsiDQvtGC0LTQtdC70YzQvdGL0LUgc2NyZWVuc2hvdHMg0LggcnVudGltZSBwcm9iZXM=')
        Priority = 'P0'
        Dependencies = @('T-0222', 'T-0223')
        Subtask = (Decode-Utf8Base64 '0J7RgtC60YDRi9GC0L46INCg0LXQsNC70LjQt9C+0LLQsNGC0Ywg0YHQsdC+0YAgcHJvYmVzINC4IHBpeGVsIGFuYWx5c2lzINC/0L7QstC10YDRhSBwcm9iZXMu')
        Activity = (Decode-Utf8Base64 'MjAyNi0wNi0yNFQxOTo0NTowMCswMzowMCAtINCh0L7Qt9C00LDQvdC+INC/0L4g0LfQsNC80LXRh9Cw0L3QuNGOINC/0L7Qu9GM0LfQvtCy0LDRgtC10LvRjzogc2NyZWVuc2hvdCBnYXRlINC90YPQttC90L4g0L7RgtC00LXQu9C40YLRjCDQvtGCIGBULTAyMjNg')
    }
    'T-0221' = @{
        Status = 'Blocked'
        Title = (Decode-Utf8Base64 '0JLQvtGB0YHRgtCw0L3QvtCy0LjRgtGMIHBlcmZvcm1hbmNlIGdhdGUg0LTQu9GPINC90LDRgdGC0L7Rj9GJ0LXQs9C+IFBsYXRmb3JtZXIg0LggUnVudGltZUhvc3Q=')
        Priority = 'P0'
        Dependencies = @('T-0215', 'T-0223', 'T-0225')
        Subtask = (Decode-Utf8Base64 '0J7RgtC60YDRi9GC0L46INCf0L7QtNC60LvRjtGH0LjRgtGMIGBQbGF0Zm9ybWVyYCBzY2VuYXJpbyDQuiBnZW5lcmljIHJ1bm5lci9zY2hlbWEg0LjQtyBgVC0wMjE1YC4=')
        Activity = (Decode-Utf8Base64 'MjAyNi0wNi0yNFQxOTo0NTowMCswMzowMCAtIE93bmVyc2hpcCDRg9GC0L7Rh9C90ZHQvSDQv9C+INCw0YPQtNC40YLRgyDQv9C+0LvRjNC30L7QstCw0YLQtdC70Y8=')
    }
    'T-0166' = @{
        Status = 'Blocked'
        Title = (Decode-Utf8Base64 '0KPQttC10YHRgtC+0YfQuNGC0YwgUGxhdGZvcm1lciDQv9C+0YHQu9C1INCw0YPQtNC40YLQsCBHb2RvdC3Qv9C10YDQtdC90L7RgdC40LzQvtGB0YLQuA==')
        Priority = 'P0'
        Dependencies = @('T-0221', 'T-0222', 'T-0223', 'T-0225')
        Subtask = (Decode-Utf8Base64 '0J7RgtC60YDRi9GC0L46INCS0YvQv9C+0LvQvdC40YLRjCBnYW1lcGxheSwgYWNjZXB0YW5jZSwgdmlzdWFsINC4IHBlcmZvcm1hbmNlINC00L7Rh9C10YDQvdC40LUg0LfQsNC00LDRh9C4Lg==')
        Activity = (Decode-Utf8Base64 'MjAyNi0wNi0yM1QyMTowMjowMCswMzowMCAtINCX0LDQtNCw0YfQsCDRgdC+0LfQtNCw0L3QsCDQv9C+INGC0LXQutGD0YnQtdC80YMg0LDRg9C00LjRgtGDINC/0L7Qu9GM0LfQvtCy0LDRgtC10LvRjy4=')
    }
    'platformer-acceptance' = @{
        Status = 'Blocked'
        Title = 'Verify the 0.1.0 Preview Platformer'
        Priority = 'P0'
        Dependencies = @('T-0166')
        Subtask = $null
        Activity = 'Initial acceptance task created with the Platformer project files.'
    }
}

$taskDocuments = @{}
foreach ($taskId in $expectedTaskMetadata.Keys) {
    $expectedTask = $expectedTaskMetadata[$taskId]
    $taskPath = Join-Path $taskRoot "$taskId.e2task"
    $taskDocument = Read-Utf8Json $taskPath
    if ($taskDocument.format -ne 'Electron2D.TaskFile' -or $taskDocument.version -ne 1 -or $taskDocument.taskId -ne $taskId) {
        throw "Platformer task document is invalid: $taskId"
    }

    if ($taskDocument.status -ne $expectedTask.Status -or
        $taskDocument.title -ne $expectedTask.Title -or
        $taskDocument.priority -ne $expectedTask.Priority) {
        throw "Platformer task '$taskId' metadata mismatch."
    }

    $actualDependencies = @($taskDocument.dependencies)
    if (($actualDependencies -join ',') -ne ($expectedTask.Dependencies -join ',')) {
        throw "Platformer task '$taskId' dependency mismatch. Expected $($expectedTask.Dependencies -join ','), got $($actualDependencies -join ',')."
    }

    if ($expectedTask.Subtask) {
        $subtasks = @($taskDocument.subtasks)
        if (-not ($subtasks -contains $expectedTask.Subtask)) {
            throw "Platformer task '$taskId' lost expected subtask: $($expectedTask.Subtask)"
        }
    }

    $activityPayloads = @($taskDocument.activity | ForEach-Object { [string]$_.payload })
    if (-not ($activityPayloads | Where-Object { $_.IndexOf($expectedTask.Activity, [System.StringComparison]::Ordinal) -ge 0 })) {
        throw "Platformer task '$taskId' lost expected historical activity: $($expectedTask.Activity)"
    }

    $migrationActivityText = Decode-Utf8Base64 '0JfQsNC00LDRh9CwINC/0LXRgNC10L3QtdGB0LXQvdCwINC40Lcg0LrQvtGA0L3QtdCy0L7Qs9C+IFRBU0tTLm1k'
    if ($taskId -ne 'platformer-acceptance' -and
        -not ($activityPayloads | Where-Object { $_.IndexOf($migrationActivityText, [System.StringComparison]::Ordinal) -ge 0 })) {
        throw "Platformer task '$taskId' is missing migration activity."
    }

    $taskDocuments[$taskId] = $taskDocument
}

$readyTaskIds = @($board.columns | Where-Object { $_.status -eq 'Ready' } | Select-Object -ExpandProperty taskIds)
if (($readyTaskIds -join ',') -ne 'T-0222') {
    throw "Platformer task board Ready column mismatch: $($readyTaskIds -join ',')"
}

$blockedTaskIds = @($board.columns | Where-Object { $_.status -eq 'Blocked' } | Select-Object -ExpandProperty taskIds)
$expectedBlockedTaskIds = @('T-0223', 'T-0225', 'T-0221', 'T-0166', 'platformer-acceptance')
if (($blockedTaskIds -join ',') -ne ($expectedBlockedTaskIds -join ',')) {
    throw "Platformer task board Blocked column mismatch. Expected $($expectedBlockedTaskIds -join ','), got $($blockedTaskIds -join ',')."
}

$awaitingAcceptanceTaskIds = @($board.columns | Where-Object { $_.status -eq 'AwaitingAcceptance' } | Select-Object -ExpandProperty taskIds)
if ($awaitingAcceptanceTaskIds.Count -ne 0) {
    throw "Platformer task board must not keep stale AwaitingAcceptance tasks: $($awaitingAcceptanceTaskIds -join ',')"
}

$acceptanceTask = $taskDocuments['platformer-acceptance']
if ($acceptanceTask.readiness -ne 'BlockedByDependencies' -or
    $acceptanceTask.acceptanceState -ne 'ChangesRequested' -or
    ((@($acceptanceTask.dependencies) -join ',') -ne 'T-0166')) {
    throw 'Platformer acceptance task must be blocked by the migrated Platformer tracking task.'
}

$resourceManifest = Read-Utf8Json (Join-Path $projectRoot 'resources/platformer.manifest.json')
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
