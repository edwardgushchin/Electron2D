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
$reportPath = Join-Path $repoRoot 'data/quality/leak-verification-report.json'
$scratchRoot = Join-Path $repoRoot '.temp/leak-verification'
$integrationProject = Join-Path $repoRoot 'tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj'
$cycleTestName = 'LeakVerificationTests.LeakVerificationCyclesReleaseSubsystemResourcesAndDoNotGrowMonotonically'
$requiredScenarioIds = @(
    'gpu-texture-render-target-cycles',
    'audio-voice-cycles',
    'physics-rid-cycles',
    'scene-load-unload-cycles'
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

    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) {
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

Remove-Item -LiteralPath $scratchRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $scratchRoot | Out-Null

dotnet test $integrationProject --filter "FullyQualifiedName~$cycleTestName" | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw "Leak verification report was not found: $reportPath"
}

$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ((Assert-Property $report 'format' 'report') -ne 'Electron2D.LeakVerificationReport') {
    throw 'Leak verification report format is invalid.'
}

if ((Assert-Property $report 'version' 'report') -ne 1) {
    throw 'Leak verification report version must be 1.'
}

if ((Assert-Property $report 'release' 'report') -ne '0.1.0-preview') {
    throw 'Leak verification report release must be 0.1.0-preview.'
}

$budgets = Assert-Property $report 'budgets' 'report'
$minimumIterations = [int](Assert-Property $budgets 'minimumIterations' 'budgets')
$maxManagedGrowthBytes = [int64](Assert-Property $budgets 'maxManagedGrowthBytes' 'budgets')
$maxNativeHandleDelta = [int](Assert-Property $budgets 'maxNativeHandleDelta' 'budgets')
$maxActiveResourceCount = [int](Assert-Property $budgets 'maxActiveResourceCount' 'budgets')
$allowMonotonicGrowth = [bool](Assert-Property $budgets 'allowMonotonicGrowth' 'budgets')

if ($minimumIterations -lt 64) {
    throw 'Leak verification requires at least 64 iterations per scenario.'
}

if ($maxManagedGrowthBytes -gt 1048576 -or $maxNativeHandleDelta -ne 0 -or $maxActiveResourceCount -ne 0 -or $allowMonotonicGrowth) {
    throw 'Leak verification budgets are less strict than the T-0103 contract.'
}

$scenarios = @((Assert-Property $report 'scenarios' 'report'))
Assert-EqualSet @($scenarios | ForEach-Object { [string](Assert-Property $_ 'scenarioId' 'scenario') }) $requiredScenarioIds 'Leak verification scenarios'

foreach ($scenario in $scenarios) {
    $scenarioId = [string](Assert-Property $scenario 'scenarioId' 'scenario')
    $subsystem = [string](Assert-Property $scenario 'subsystem' "scenario $scenarioId")
    $iterations = [int](Assert-Property $scenario 'iterations' "scenario $scenarioId")
    $managedGrowthBytes = [int64](Assert-Property $scenario 'managedGrowthBytes' "scenario $scenarioId")
    $nativeHandleDelta = [int](Assert-Property $scenario 'nativeHandleDelta' "scenario $scenarioId")
    $activeResourceCount = [int](Assert-Property $scenario 'activeResourceCount' "scenario $scenarioId")
    $monotonicGrowthDetected = [bool](Assert-Property $scenario 'monotonicGrowthDetected' "scenario $scenarioId")
    $evidence = @((Assert-Property $scenario 'evidence' "scenario $scenarioId"))

    if ([System.String]::IsNullOrWhiteSpace($subsystem)) {
        throw "Scenario $scenarioId subsystem must not be empty."
    }

    if ($iterations -lt $minimumIterations) {
        throw "Scenario $scenarioId has too few iterations: $iterations."
    }

    if ($managedGrowthBytes -lt 0 -or $managedGrowthBytes -gt $maxManagedGrowthBytes) {
        throw "Scenario $scenarioId managed growth exceeds budget: $managedGrowthBytes."
    }

    if ($nativeHandleDelta -ne $maxNativeHandleDelta) {
        throw "Scenario $scenarioId native handle delta must be 0, got $nativeHandleDelta."
    }

    if ($activeResourceCount -ne $maxActiveResourceCount) {
        throw "Scenario $scenarioId active resource count must be 0, got $activeResourceCount."
    }

    if ($monotonicGrowthDetected) {
        throw "Scenario $scenarioId reports monotonic growth."
    }

    if ($evidence.Count -eq 0) {
        throw "Scenario $scenarioId must contain at least one evidence path."
    }

    foreach ($evidencePath in $evidence) {
        [void](Assert-RepositoryPath ([string]$evidencePath) "scenario $scenarioId evidence")
    }
}

$planPath = Join-Path $scratchRoot 'verification-plan.json'
$plan = [ordered]@{
    format = 'Electron2D.LeakVerificationPlan'
    version = 1
    release = $report.release
    focusedTest = $cycleTestName
    report = 'data/quality/leak-verification-report.json'
    scenarios = $requiredScenarioIds
}
$plan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $planPath -Encoding UTF8

Write-Host "Leak verification passed. Scenarios: $($requiredScenarioIds.Count), iterations: $minimumIterations."
