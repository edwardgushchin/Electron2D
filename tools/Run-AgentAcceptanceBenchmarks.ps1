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
[CmdletBinding()]
param(
    [Alias("Suite")]
    [string]$SuiteId,
    [string]$OutputDirectory = ".temp\agent-acceptance-benchmarks",
    [switch]$List,
    [switch]$DryRun,
    [switch]$ContinueOnFailure
)

$ErrorActionPreference = "Stop"

$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$manifestPath = Join-Path $repositoryRoot "data\quality\agent-acceptance-benchmarks.json"

function Resolve-OutputDirectory {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $Path))
}

function Resolve-RepositoryPath {
    param([string]$RelativePath)

    $normalized = $RelativePath.Replace("/", [System.IO.Path]::DirectorySeparatorChar)
    $fullPath = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot $normalized))
    if (-not $fullPath.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Benchmark file reference escapes repository root: $RelativePath"
    }

    return $fullPath
}

function Assert-RepositoryFileExists {
    param([string]$RelativePath)

    $filePath = Resolve-RepositoryPath $RelativePath
    if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
        throw "Benchmark referenced file does not exist: $RelativePath"
    }
}

function Get-SelectedSuites {
    param($Manifest)

    $suites = @($Manifest.suites)
    if ([string]::IsNullOrWhiteSpace($SuiteId)) {
        return $suites
    }

    $selected = @($suites | Where-Object { $_.id -eq $SuiteId })
    if ($selected.Count -eq 0) {
        $available = ($suites | ForEach-Object { $_.id }) -join ", "
        throw "Unknown benchmark suite '$SuiteId'. Available suites: $available"
    }

    return $selected
}

function Test-HeadlessManualHarnessDocumented {
    param($Manifest)

    foreach ($suite in @($Manifest.suites)) {
        if ($suite.id -ne "headless-ai") {
            continue
        }

        $reference = [string]$suite.successConditions.documentedManualHarness
        if ([string]::IsNullOrWhiteSpace($reference)) {
            return $false
        }

        $path = $reference.Split("#", 2)[0]
        $fullPath = Resolve-RepositoryPath $path
        return Test-Path -LiteralPath $fullPath -PathType Leaf
    }

    return $false
}

function Assert-ManifestReferences {
    param($Manifest)

    foreach ($suite in @($Manifest.suites)) {
        foreach ($path in @($suite.documentation)) {
            Assert-RepositoryFileExists $path
        }

        foreach ($evidence in @($suite.evidence)) {
            foreach ($path in @($evidence.sourceFiles)) {
                Assert-RepositoryFileExists $path
            }

            if ($null -ne $evidence.visualEvidence) {
                Assert-RepositoryFileExists ([string]$evidence.visualEvidence.reference)
            }
        }

        if ($null -ne $suite.successConditions -and $null -ne $suite.successConditions.documentedManualHarness) {
            $path = ([string]$suite.successConditions.documentedManualHarness).Split("#", 2)[0]
            Assert-RepositoryFileExists $path
        }
    }
}

function New-BenchmarkPlan {
    param($Manifest, $Suites, [bool]$IsDryRun)

    $requiredEvidence = 0
    $visualEvidence = 0
    $suitePlans = @()

    foreach ($suite in @($Suites)) {
        $evidencePlans = @()
        foreach ($evidence in @($suite.evidence)) {
            if ($evidence.required) {
                $requiredEvidence++
            }

            if ($null -ne $evidence.visualEvidence) {
                $visualEvidence++
            }

            $evidencePlans += [ordered]@{
                id = $evidence.id
                kind = $evidence.kind
                required = [bool]$evidence.required
                covers = @($evidence.covers)
                command = $evidence.command
                arguments = @($evidence.arguments)
            }
        }

        $suitePlans += [ordered]@{
            id = $suite.id
            mode = $suite.mode
            releaseRequired = [bool]$suite.releaseRequired
            targetSuccessRatio = [double]$suite.targetSuccessRatio
            scenarioCount = @($suite.scenarios).Count
            evidence = $evidencePlans
        }
    }

    return [ordered]@{
        format = "Electron2D.AgentAcceptanceBenchmarkPlan"
        manifestVersion = [int]$Manifest.version
        release = $Manifest.release
        dryRun = $IsDryRun
        generatedAtUtc = [System.DateTimeOffset]::UtcNow.ToString("O")
        suites = $suitePlans
        requiredEvidenceCount = $requiredEvidence
        visualEvidenceCount = $visualEvidence
        headlessManualHarnessDocumented = (Test-HeadlessManualHarnessDocumented $Manifest)
    }
}

function Write-JsonFile {
    param($Value, [string]$Path)

    $directory = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $directory -PathType Container)) {
        New-Item -ItemType Directory -Path $directory | Out-Null
    }

    $Value | ConvertTo-Json -Depth 64 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Invoke-BenchmarkEvidence {
    param($Evidence, [string]$LogsDirectory)

    $logPath = Join-Path $LogsDirectory "$($Evidence.id).log"

    if ($Evidence.kind -eq "documentedManualHarness") {
        $message = "Documented manual harness evidence accepted for this release gate step."
        $message | Set-Content -LiteralPath $logPath -Encoding UTF8
        return [ordered]@{
            id = $Evidence.id
            kind = $Evidence.kind
            required = [bool]$Evidence.required
            status = "documented"
            succeeded = $true
            exitCode = 0
            logPath = $logPath
            covers = @($Evidence.covers)
        }
    }

    $command = [string]$Evidence.command
    $arguments = @($Evidence.arguments | ForEach-Object { [string]$_ })
    $startedAt = [System.DateTimeOffset]::UtcNow

    try {
        Push-Location $repositoryRoot
        $global:LASTEXITCODE = 0
        $output = & $command @arguments 2>&1
        $exitCode = if ($null -eq $global:LASTEXITCODE) { 0 } else { [int]$global:LASTEXITCODE }
    }
    catch {
        $output = @($_.Exception.Message)
        $exitCode = 1
    }
    finally {
        Pop-Location
    }

    $completedAt = [System.DateTimeOffset]::UtcNow
    $logLines = @(
        "id=$($Evidence.id)"
        "kind=$($Evidence.kind)"
        "command=$command"
        "arguments=$($arguments -join ' ')"
        "startedAtUtc=$($startedAt.ToString("O"))"
        "completedAtUtc=$($completedAt.ToString("O"))"
        "exitCode=$exitCode"
        ""
    )
    $logLines += @($output | ForEach-Object { $_.ToString() })
    $logLines | Set-Content -LiteralPath $logPath -Encoding UTF8

    return [ordered]@{
        id = $Evidence.id
        kind = $Evidence.kind
        required = [bool]$Evidence.required
        status = $(if ($exitCode -eq 0) { "passed" } else { "failed" })
        succeeded = ($exitCode -eq 0)
        exitCode = $exitCode
        logPath = $logPath
        covers = @($Evidence.covers)
    }
}

if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "Agent acceptance benchmark manifest was not found: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.format -ne "Electron2D.AgentAcceptanceBenchmarkManifest") {
    throw "Unexpected benchmark manifest format: $($manifest.format)"
}

Assert-ManifestReferences $manifest
$selectedSuites = @(Get-SelectedSuites $manifest)
$resolvedOutputDirectory = Resolve-OutputDirectory $OutputDirectory
$planPath = Join-Path $resolvedOutputDirectory "benchmark-plan.json"

if ($List) {
    foreach ($suite in $selectedSuites) {
        Write-Output "$($suite.id) [$($suite.mode)] target=$($suite.targetSuccessRatio)"
        foreach ($scenario in @($suite.scenarios)) {
            Write-Output "  - $($scenario.id)"
        }
    }

    exit 0
}

$plan = New-BenchmarkPlan $manifest $selectedSuites ([bool]$DryRun)
Write-JsonFile $plan $planPath

if ($DryRun) {
    Write-Output "Agent acceptance benchmark dry run passed"
    Write-Output "PlanPath=$planPath"
    exit 0
}

$logsDirectory = Join-Path $resolvedOutputDirectory "logs"
$artifactsDirectory = Join-Path $resolvedOutputDirectory "artifacts"
New-Item -ItemType Directory -Force -Path $logsDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $artifactsDirectory | Out-Null

$suiteResults = @()
$overallSucceeded = $true

foreach ($suite in $selectedSuites) {
    $evidenceResults = @()
    foreach ($evidence in @($suite.evidence)) {
        $result = Invoke-BenchmarkEvidence $evidence $logsDirectory
        $evidenceResults += $result
        if (-not $result.succeeded -and -not $ContinueOnFailure) {
            break
        }
    }

    $required = @($evidenceResults | Where-Object { $_.required })
    $passedRequired = @($required | Where-Object { $_.succeeded })
    $ratio = if ($required.Count -eq 0) { 1.0 } else { [double]$passedRequired.Count / [double]$required.Count }
    $suiteSucceeded = $ratio -ge [double]$suite.targetSuccessRatio
    if (-not $suiteSucceeded) {
        $overallSucceeded = $false
    }

    $suiteResults += [ordered]@{
        id = $suite.id
        mode = $suite.mode
        releaseRequired = [bool]$suite.releaseRequired
        targetSuccessRatio = [double]$suite.targetSuccessRatio
        successRatio = $ratio
        succeeded = $suiteSucceeded
        evidence = $evidenceResults
    }
}

$benchmarkResult = [ordered]@{
    format = "Electron2D.AgentAcceptanceBenchmarkResult"
    manifestVersion = [int]$manifest.version
    release = $manifest.release
    generatedAtUtc = [System.DateTimeOffset]::UtcNow.ToString("O")
    succeeded = $overallSucceeded
    planPath = $planPath
    artifactsDirectory = $artifactsDirectory
    suites = $suiteResults
}

$resultPath = Join-Path $resolvedOutputDirectory "benchmark-result.json"
Write-JsonFile $benchmarkResult $resultPath

Write-Output "Agent acceptance benchmark completed"
Write-Output "ResultPath=$resultPath"

if (-not $overallSucceeded) {
    exit 1
}

exit 0
