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
$metricsPath = Join-Path $repoRoot 'data/quality/performance-reference-metrics.json'
$scratchRoot = Join-Path $repoRoot '.temp/reference-performance'
$requiredScenarioIds = @(
    'empty-scene',
    'sprite-scene',
    'reference-platformer'
)

function Assert-Property([object]$target, [string]$name, [string]$context) {
    if ($null -eq $target -or -not ($target.PSObject.Properties.Name -contains $name)) {
        throw "$context is missing required property: $name"
    }

    return $target.PSObject.Properties[$name].Value
}

function Assert-RepositoryPath([string]$relativePath, [string]$context) {
    if ([System.String]::IsNullOrWhiteSpace($relativePath)) {
        throw "$context path must not be empty."
    }

    if ($relativePath -match '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "$context path must be repository-local, not a URL: $relativePath"
    }

    if ($relativePath -match '\\') {
        throw "$context path must use forward slashes: $relativePath"
    }

    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
    $rootPath = [System.IO.Path]::GetFullPath($repoRoot)
    if (-not $fullPath.StartsWith($rootPath, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$context path escapes repository root: $relativePath"
    }

    if (-not (Test-Path -LiteralPath $fullPath)) {
        throw "$context path does not exist: $relativePath"
    }

    return $fullPath
}

function Assert-EqualSet([string[]]$actual, [string[]]$expected, [string]$context) {
    $actualSorted = @($actual | Sort-Object)
    $expectedSorted = @($expected | Sort-Object)
    if (($actualSorted -join ',') -ne ($expectedSorted -join ',')) {
        throw "$context mismatch. Expected $($expectedSorted -join ','), got $($actualSorted -join ',')."
    }
}

function Assert-NumberLessOrEqual([double]$actual, [double]$max, [string]$context) {
    if ([double]::IsNaN($actual) -or $actual -lt 0 -or $actual -gt $max) {
        throw "$context exceeds budget. Actual: $actual, max: $max."
    }
}

function Invoke-RequiredVerifier([string]$fileName) {
    $path = Join-Path $PSScriptRoot $fileName
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required reference verifier was not found: $fileName"
    }

    & $path
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Invoke-RequiredVerifier 'Verify-ReferencePlatformer.ps1'

Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $scratchRoot | Out-Null

if (-not (Test-Path -LiteralPath $metricsPath -PathType Leaf)) {
    throw "Reference performance metrics artifact was not found: $metricsPath"
}

$metrics = Get-Content -LiteralPath $metricsPath -Raw | ConvertFrom-Json
if ((Assert-Property $metrics 'format' 'metrics') -ne 'Electron2D.ReferencePerformanceMetrics') {
    throw 'Reference performance metrics format is invalid.'
}

if ((Assert-Property $metrics 'version' 'metrics') -ne 1) {
    throw 'Reference performance metrics version must be 1.'
}

if ((Assert-Property $metrics 'release' 'metrics') -ne '0.1.0-preview') {
    throw 'Reference performance metrics release must be 0.1.0-preview.'
}

$budgets = Assert-Property $metrics 'budgets' 'metrics'
$targetFps = [int](Assert-Property $budgets 'targetFps' 'budgets')
$minimumWarmupFrames = [int](Assert-Property $budgets 'minimumWarmupFrames' 'budgets')
$minimumMeasuredFrames = [int](Assert-Property $budgets 'minimumMeasuredFrames' 'budgets')
$maxSteadyManagedAllocatedBytesPerFrame = [int64](Assert-Property $budgets 'maxSteadyManagedAllocatedBytesPerFrame' 'budgets')
$scenarioBudgets = Assert-Property $budgets 'scenarios' 'budgets'

if ($targetFps -ne 60) {
    throw "Performance target FPS must be 60, got $targetFps."
}

if ($minimumWarmupFrames -lt 120 -or $minimumMeasuredFrames -lt 600) {
    throw 'Reference performance verifier requires at least 120 warm-up frames and 600 measured frames.'
}

if ($maxSteadyManagedAllocatedBytesPerFrame -ne 0) {
    throw 'Reference performance verifier requires 0 steady managed allocations per frame.'
}

$devices = @((Assert-Property $metrics 'devices' 'metrics'))
if ($devices.Count -eq 0) {
    throw 'Reference performance metrics must contain at least one documented device.'
}

$deviceIds = @($devices | ForEach-Object { [string](Assert-Property $_ 'deviceId' 'device') })
if ($deviceIds -notcontains 'local-windows-x64') {
    throw 'Reference performance metrics must document local-windows-x64.'
}

foreach ($device in $devices) {
    foreach ($propertyName in @('deviceId', 'platform', 'cpuClass', 'memoryGb', 'graphicsClass', 'notes')) {
        [void](Assert-Property $device $propertyName 'device')
    }
}

$scenarios = @((Assert-Property $metrics 'scenarios' 'metrics'))
Assert-EqualSet @($scenarios | ForEach-Object { [string](Assert-Property $_ 'scenarioId' 'scenario') }) $requiredScenarioIds 'Reference performance scenarios'

foreach ($scenario in $scenarios) {
    $scenarioId = [string](Assert-Property $scenario 'scenarioId' 'scenario')
    $projectPath = [string](Assert-Property $scenario 'projectPath' "scenario $scenarioId")
    $scenePath = [string](Assert-Property $scenario 'scenePath' "scenario $scenarioId")
    $deviceId = [string](Assert-Property $scenario 'deviceId' "scenario $scenarioId")
    $warmupFrames = [int](Assert-Property $scenario 'warmupFrames' "scenario $scenarioId")
    $measuredFrames = [int](Assert-Property $scenario 'measuredFrames' "scenario $scenarioId")
    $scenarioTargetFps = [int](Assert-Property $scenario 'targetFps' "scenario $scenarioId")
    $p95 = [double](Assert-Property $scenario 'p95FrameTimeMs' "scenario $scenarioId")
    $p99 = [double](Assert-Property $scenario 'p99FrameTimeMs' "scenario $scenarioId")
    $average = [double](Assert-Property $scenario 'averageFrameTimeMs' "scenario $scenarioId")
    $steadyAllocations = [int64](Assert-Property $scenario 'steadyManagedAllocatedBytesPerFrame' "scenario $scenarioId")
    $evidence = @((Assert-Property $scenario 'evidence' "scenario $scenarioId"))

    [void](Assert-RepositoryPath $projectPath "scenario $scenarioId project")
    [void](Assert-RepositoryPath "$projectPath/$scenePath" "scenario $scenarioId scene")

    if ($deviceIds -notcontains $deviceId) {
        throw "Scenario $scenarioId references unknown device: $deviceId"
    }

    if ($warmupFrames -lt $minimumWarmupFrames -or $measuredFrames -lt $minimumMeasuredFrames) {
        throw "Scenario $scenarioId does not meet warm-up or measured frame minimums."
    }

    if ($scenarioTargetFps -ne $targetFps) {
        throw "Scenario $scenarioId target FPS mismatch. Expected $targetFps, got $scenarioTargetFps."
    }

    $budget = $scenarioBudgets.PSObject.Properties[$scenarioId].Value
    if ($null -eq $budget) {
        throw "Scenario $scenarioId is missing a budget entry."
    }

    Assert-NumberLessOrEqual $p95 ([double](Assert-Property $budget 'maxP95FrameTimeMs' "budget $scenarioId")) "Scenario $scenarioId p95 frame time"
    Assert-NumberLessOrEqual $p99 ([double](Assert-Property $budget 'maxP99FrameTimeMs' "budget $scenarioId")) "Scenario $scenarioId p99 frame time"
    Assert-NumberLessOrEqual $average ([double](Assert-Property $budget 'maxP95FrameTimeMs' "budget $scenarioId")) "Scenario $scenarioId average frame time"

    if ($steadyAllocations -ne $maxSteadyManagedAllocatedBytesPerFrame) {
        throw "Scenario $scenarioId steady managed allocations must be 0 B/frame, got $steadyAllocations."
    }

    if ($evidence.Count -eq 0) {
        throw "Scenario $scenarioId must contain at least one evidence path."
    }

    foreach ($evidencePath in $evidence) {
        [void](Assert-RepositoryPath ([string]$evidencePath) "scenario $scenarioId evidence")
    }
}

$platformerEvidence = @($scenarios | Where-Object { $_.scenarioId -eq 'reference-platformer' } | ForEach-Object { $_.evidence })
if ($platformerEvidence -notcontains 'tools/Verify-ReferencePlatformer.ps1') {
    throw 'reference-platformer metrics must cite tools/Verify-ReferencePlatformer.ps1.'
}

$batching = Assert-Property $metrics 'drawCallBatching' 'metrics'
if ((Assert-Property $batching 'scenarioId' 'drawCallBatching') -ne 'sprite-scene') {
    throw 'drawCallBatching must measure sprite-scene.'
}

$commandCount = [int](Assert-Property $batching 'commandCount' 'drawCallBatching')
$drawCallCount = [int](Assert-Property $batching 'drawCallCount' 'drawCallBatching')
$reductionRatio = [double](Assert-Property $batching 'reductionRatio' 'drawCallBatching')
$batchEvidence = @((Assert-Property $batching 'evidence' 'drawCallBatching'))
$minReductionRatio = [double](Assert-Property (Assert-Property $budgets 'drawCallBatching' 'budgets') 'minReductionRatio' 'drawCallBatching budget')

if ($commandCount -le $drawCallCount) {
    throw "Batching must reduce draw calls. commandCount=$commandCount, drawCallCount=$drawCallCount."
}

if ($reductionRatio -lt $minReductionRatio) {
    throw "Batching reduction ratio is too low. Actual: $reductionRatio, minimum: $minReductionRatio."
}

if ($batchEvidence.Count -eq 0) {
    throw 'drawCallBatching must contain evidence.'
}

foreach ($evidencePath in $batchEvidence) {
    [void](Assert-RepositoryPath ([string]$evidencePath) 'drawCallBatching evidence')
}

$planPath = Join-Path $scratchRoot 'verification-plan.json'
$plan = [ordered]@{
    format = 'Electron2D.ReferencePerformanceVerificationPlan'
    version = 1
    release = $metrics.release
    scenarios = $requiredScenarioIds
    metricsArtifact = 'data/quality/performance-reference-metrics.json'
    referenceGameValidators = @(
        'tools/Verify-ReferencePlatformer.ps1'
    )
}
$plan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $planPath -Encoding UTF8

Write-Host "Reference performance verification passed. Scenarios: $($requiredScenarioIds.Count), devices: $($devices.Count), batching: $commandCount->$drawCallCount."
