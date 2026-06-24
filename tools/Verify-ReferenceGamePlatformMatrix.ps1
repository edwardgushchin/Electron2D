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
$artifactPath = Join-Path $repoRoot 'data/quality/reference-game-platform-matrix.json'
$workRoot = Join-Path $repoRoot '.temp/reference-game-platform-matrix'
$summaryPath = Join-Path $workRoot 'summary.json'
$expectedRuntimeTargets = @('AndroidArm64', 'IosArm64', 'LinuxX64', 'MacOSArm64', 'WebAssemblyBrowser', 'WindowsX64')
$expectedEditorTargets = @('Linux', 'Windows', 'macOS')
$expectedReleaseVerificationDecisionId = 'all-runtime-targets-for-0.1.0-preview'
$allowedDifferences = @(
    'export preset target/configuration/runtime identifier/output directory',
    'renderer profile',
    'application icon and branding metadata',
    'signing references without secrets',
    'storefront metadata',
    'browser hosting metadata'
)
$platformNames = @(
    'Android',
    'AndroidArm64',
    'Ios',
    'iOS',
    'IosArm64',
    'Linux',
    'LinuxX64',
    'MacOS',
    'MacOSArm64',
    'Windows',
    'WindowsX64',
    'WebAssembly',
    'WebAssemblyBrowser',
    'browser-wasm'
)
$forbiddenWorkflowPaths = @('TASKS.md', 'dev-diary', 'completed-tasks')
$editorTaskMetadataRootName = '.electron2d/tasks'

function Get-RelativeRepositoryPath([string]$path) {
    if ([System.String]::IsNullOrWhiteSpace($path)) {
        throw 'Repository-relative path must not be empty.'
    }

    if ($path -match '^[a-zA-Z][a-zA-Z0-9+.-]*://') {
        throw "Repository-relative path must not be a URL: $path"
    }

    if ([System.IO.Path]::IsPathRooted($path)) {
        throw "Repository-relative path must not be rooted: $path"
    }

    if ($path -match '\\') {
        throw "Repository-relative path must use forward slashes: $path"
    }

    $candidate = [System.IO.Path]::GetFullPath((Join-Path $repoRoot $path.Replace('/', [System.IO.Path]::DirectorySeparatorChar)))
    $root = [System.IO.Path]::GetFullPath($repoRoot)
    if (-not $candidate.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Repository-relative path escapes repository root: $path"
    }

    return $candidate
}

function Assert-File([string]$path, [string]$message) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw $message
    }
}

function Assert-Directory([string]$path, [string]$message) {
    if (-not (Test-Path -LiteralPath $path -PathType Container)) {
        throw $message
    }
}

function Get-SortedStrings($values) {
    return @($values | ForEach-Object { [string]$_ } | Sort-Object)
}

function Assert-StringSet([string[]]$actual, [string[]]$expected, [string]$context) {
    $actualSorted = @(Get-SortedStrings $actual)
    $expectedSorted = @(Get-SortedStrings $expected)
    if (($actualSorted -join ',') -ne ($expectedSorted -join ',')) {
        throw "$context mismatch. Expected $($expectedSorted -join ','), got $($actualSorted -join ',')."
    }
}

function Assert-NoProperty([object]$value, [string]$propertyName, [string]$context) {
    if ($value.PSObject.Properties.Name -contains $propertyName) {
        throw "$context must not declare legacy property '$propertyName'."
    }
}

function Assert-ReleaseVerificationTargets([object[]]$releaseVerificationTargets, [string[]]$expectedTargets) {
    $targets = @($releaseVerificationTargets)
    if ($targets.Count -ne $expectedTargets.Count) {
        throw "releaseVerificationTargets must contain one entry per runtime target. Expected $($expectedTargets.Count), got $($targets.Count)."
    }

    $targetNames = @($targets | ForEach-Object { [string]$_.target })
    Assert-StringSet $targetNames $expectedTargets 'Reference game platform matrix releaseVerificationTargets'

    foreach ($target in $targets) {
        $targetName = [string]$target.target
        if ($target.realSmokeSoakRequired -ne $true) {
            throw "$targetName releaseVerificationTarget must require real smoke/soak for 0.1.0 Preview."
        }

        if ($target.blockedEnvironmentArtifactAllowed -ne $true) {
            throw "$targetName releaseVerificationTarget must allow blocked-environment artifact diagnostics."
        }

        $releaseGateBlocker = [string]$target.releaseGateBlocker
        if ([System.String]::IsNullOrWhiteSpace($releaseGateBlocker)) {
            throw "$targetName releaseVerificationTarget must describe the release gate blocker."
        }
    }
}

function Get-ProjectRelativePath([string]$projectRoot, [string]$fullPath) {
    $root = [System.IO.Path]::GetFullPath($projectRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $root = $root + [System.IO.Path]::DirectorySeparatorChar
    $candidate = [System.IO.Path]::GetFullPath($fullPath)
    if (-not $candidate.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Path escapes project root: $fullPath"
    }

    return $candidate.Substring($root.Length).Replace('\', '/')
}

function Assert-NoForbiddenWorkflowReferences([string]$projectRoot, [object]$jsonDocument, [string]$context) {
    foreach ($forbiddenPath in $forbiddenWorkflowPaths) {
        if (Test-Path -LiteralPath (Join-Path $projectRoot $forbiddenPath)) {
            throw "$context must not contain repository workflow path: $forbiddenPath"
        }

        $jsonText = $jsonDocument | ConvertTo-Json -Depth 100
        if ($jsonText.IndexOf($forbiddenPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "$context references repository workflow path: $forbiddenPath"
        }
    }
}

function Assert-SafeSigningReference([object]$preset, [string]$projectId) {
    if ($null -eq $preset.signing) {
        throw "$projectId preset $($preset.name) must declare signing policy."
    }

    $identity = [string]$preset.signing.identity
    $credentialReference = [string]$preset.signing.credentialReference
    if ($identity.IndexOf('-----BEGIN', [System.StringComparison]::OrdinalIgnoreCase) -ge 0 -or
        $credentialReference.IndexOf('-----BEGIN', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$projectId preset $($preset.name) appears to contain signing secret material."
    }

    if ($preset.signing.required -eq $true -and -not $credentialReference.StartsWith('env:', [System.StringComparison]::Ordinal)) {
        throw "$projectId preset $($preset.name) must use an env: credentialReference for required signing."
    }
}

function Assert-NoConditionalGameplayCompile([string]$projectFile, [string]$projectId) {
    $projectText = Get-Content -LiteralPath $projectFile -Raw
    if ($projectText.IndexOf('Condition=', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
        throw "$projectId project file must not use conditional compile or platform-specific item conditions."
    }

    foreach ($platformName in $platformNames) {
        $pattern = "Scripts\*$platformName"
        if ($projectText.IndexOf($pattern, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
            throw "$projectId project file contains platform-specific gameplay include: $pattern"
        }
    }
}

function Assert-NoPlatformSpecificGameForks([string]$projectRoot, [string[]]$scanRoots, [string]$projectId) {
    foreach ($scanRoot in $scanRoots) {
        $fullRoot = Join-Path $projectRoot $scanRoot.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $fullRoot -PathType Container)) {
            continue
        }

        foreach ($item in Get-ChildItem -LiteralPath $fullRoot -Recurse -Force) {
            $relativePath = Get-ProjectRelativePath $projectRoot $item.FullName
            $segments = @($relativePath -split '/')
            foreach ($segment in $segments) {
                $stem = [System.IO.Path]::GetFileNameWithoutExtension($segment)
                foreach ($platformName in $platformNames) {
                    if ($stem.Equals($platformName, [System.StringComparison]::OrdinalIgnoreCase) -or
                        $stem.StartsWith("$platformName.", [System.StringComparison]::OrdinalIgnoreCase) -or
                        $stem.StartsWith("$platformName-", [System.StringComparison]::OrdinalIgnoreCase) -or
                        $stem.EndsWith(".$platformName", [System.StringComparison]::OrdinalIgnoreCase) -or
                        $stem.EndsWith("-$platformName", [System.StringComparison]::OrdinalIgnoreCase)) {
                        throw "$projectId contains platform-specific gameplay/resource path: $relativePath"
                    }
                }
            }
        }
    }
}

function Assert-ForbiddenPlatformRootsDoNotExist([string]$projectRoot, [string[]]$forbiddenPlatformSpecificRoots, [string]$projectId) {
    foreach ($relativePath in $forbiddenPlatformSpecificRoots) {
        $fullPath = Join-Path $projectRoot $relativePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (Test-Path -LiteralPath $fullPath) {
            throw "$projectId contains forbidden platform-specific root: $relativePath"
        }
    }
}

function Assert-EditorMetadataNotRuntimeResource([string]$projectRoot, [string[]]$resourceRoots, [string[]]$editorMetadataRoots, [string]$projectId) {
    foreach ($metadataRoot in $editorMetadataRoots) {
        $metadataPath = Join-Path $projectRoot $metadataRoot.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        Assert-Directory $metadataPath "$projectId is missing Editor metadata root: $metadataRoot"
    }

    foreach ($resourceRoot in $resourceRoots) {
        $resourcePath = Join-Path $projectRoot $resourceRoot.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
        if (-not (Test-Path -LiteralPath $resourcePath)) {
            continue
        }

        foreach ($file in Get-ChildItem -LiteralPath $resourcePath -Recurse -File -Force) {
            $relativePath = Get-ProjectRelativePath $projectRoot $file.FullName
            if ($relativePath.StartsWith('.electron2d/', [System.StringComparison]::OrdinalIgnoreCase)) {
                throw "$projectId exposes Editor metadata as runtime resource: $relativePath"
            }
        }
    }
}

Assert-File $artifactPath "Reference game platform matrix artifact was not found: $artifactPath"
$artifact = Get-Content -LiteralPath $artifactPath -Raw | ConvertFrom-Json
if ($artifact.format -ne 'Electron2D.ReferenceGamePlatformMatrix' -or $artifact.version -ne 2 -or $artifact.release -ne '0.1.0-preview') {
    throw 'Reference game platform matrix artifact has invalid identity.'
}

Assert-NoProperty $artifact 'targetSet' 'Reference game platform matrix artifact'
Assert-StringSet @($artifact.runtimeTargets) $expectedRuntimeTargets 'Reference game platform matrix runtimeTargets'
Assert-StringSet @($artifact.editorTargets) $expectedEditorTargets 'Reference game platform matrix editorTargets'
Assert-ReleaseVerificationTargets @($artifact.releaseVerificationTargets) $expectedRuntimeTargets
if ($null -eq $artifact.releaseVerificationDecision -or
    [string]$artifact.releaseVerificationDecision.id -ne $expectedReleaseVerificationDecisionId -or
    [string]$artifact.releaseVerificationDecision.source -ne 'docs/releases/0.1.0-preview.md') {
    throw 'Reference game platform matrix releaseVerificationDecision is missing or invalid.'
}

Assert-StringSet @($artifact.allowedDifferences) $allowedDifferences 'Reference game platform matrix allowedDifferences'

Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $workRoot | Out-Null

$projectSummaries = @()
foreach ($project in @($artifact.projects)) {
    $projectId = [string]$project.id
    $projectRoot = Get-RelativeRepositoryPath ([string]$project.projectPath)
    Assert-Directory $projectRoot "Reference project was not found: $projectId"

    $verifierPath = Get-RelativeRepositoryPath ([string]$project.verifier)
    Assert-File $verifierPath "$projectId verifier was not found: $($project.verifier)"
    & $verifierPath
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $projectFile = Join-Path $projectRoot ([string]$project.projectFile)
    $settingsFile = Join-Path $projectRoot ([string]$project.settingsFile)
    $presetFile = Join-Path $projectRoot ([string]$project.exportPresetFile)
    $mainScene = Join-Path $projectRoot ([string]$project.mainScene)
    Assert-File $projectFile "$projectId project file was not found."
    Assert-File $settingsFile "$projectId settings file was not found."
    Assert-File $presetFile "$projectId export preset file was not found."
    Assert-File $mainScene "$projectId main scene was not found."

    $settings = Get-Content -LiteralPath $settingsFile -Raw | ConvertFrom-Json
    if ($settings.format -ne 'Electron2D.ProjectSettings' -or $settings.name -ne [string]$project.name -or $settings.mainScene -ne [string]$project.mainScene) {
        throw "$projectId project settings do not match the platform matrix artifact."
    }

    Assert-NoForbiddenWorkflowReferences $projectRoot $settings $projectId

    $presetsSource = Get-Content -LiteralPath $presetFile -Raw | ConvertFrom-Json
    $presets = if ($null -ne $presetsSource.exportPresets) { $presetsSource.exportPresets } else { $presetsSource }
    if ($presets.format -ne 'Electron2D.ExportPresets') {
        throw "$projectId export preset format is invalid."
    }

    $targets = @($presets.presets | ForEach-Object { [string]$_.target })
    Assert-StringSet $targets $expectedRuntimeTargets "$projectId export runtimeTargets"
    Assert-NoProperty $project 'expectedTargets' "$projectId artifact"
    Assert-StringSet @($project.expectedRuntimeTargets) $expectedRuntimeTargets "$projectId artifact expectedRuntimeTargets"

    foreach ($preset in @($presets.presets)) {
        Assert-SafeSigningReference $preset $projectId
    }

    $scriptRoots = @($project.scriptRoots | ForEach-Object { [string]$_ })
    $sceneRoots = @($project.sceneRoots | ForEach-Object { [string]$_ })
    $resourceRoots = @($project.resourceRoots | ForEach-Object { [string]$_ })
    $editorMetadataRoots = @($project.editorMetadataRoots | ForEach-Object { [string]$_ })
    $forbiddenPlatformSpecificRoots = @($project.forbiddenPlatformSpecificRoots | ForEach-Object { [string]$_ })

    foreach ($scriptRoot in $scriptRoots) {
        Assert-Directory (Join-Path $projectRoot $scriptRoot) "$projectId is missing script root: $scriptRoot"
    }

    foreach ($sceneRoot in $sceneRoots) {
        Assert-Directory (Join-Path $projectRoot $sceneRoot) "$projectId is missing scene root: $sceneRoot"
    }

    foreach ($resourceRoot in $resourceRoots) {
        Assert-Directory (Join-Path $projectRoot $resourceRoot) "$projectId is missing resource root: $resourceRoot"
    }

    Assert-NoConditionalGameplayCompile $projectFile $projectId
    Assert-ForbiddenPlatformRootsDoNotExist $projectRoot $forbiddenPlatformSpecificRoots $projectId
    Assert-NoPlatformSpecificGameForks $projectRoot ($scriptRoots + $sceneRoots + $resourceRoots) $projectId
    Assert-EditorMetadataNotRuntimeResource $projectRoot $resourceRoots $editorMetadataRoots $projectId

    $projectSummaries += [ordered]@{
        id = $projectId
        projectPath = [string]$project.projectPath
        verifier = [string]$project.verifier
        targets = @(Get-SortedStrings $targets)
        sharedCodebaseChecked = $true
    }
}

$summary = [ordered]@{
    format = 'Electron2D.ReferenceGamePlatformMatrix.VerificationSummary'
    version = 1
    release = '0.1.0-preview'
    verifiedAtUtc = [System.DateTimeOffset]::UtcNow.ToString('O')
    projects = $projectSummaries
    runtimeTargets = @(Get-SortedStrings $expectedRuntimeTargets)
    editorTargets = @(Get-SortedStrings $expectedEditorTargets)
    releaseVerificationTargets = @($artifact.releaseVerificationTargets | Sort-Object target)
    releaseVerificationDecision = $artifact.releaseVerificationDecision
    allowedDifferences = @(Get-SortedStrings $allowedDifferences)
}
$summary | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $summaryPath -Encoding UTF8

Write-Host "Reference game platform matrix verification passed. Projects: $($projectSummaries.Count), runtime targets: $($expectedRuntimeTargets.Count), editor targets: $($expectedEditorTargets.Count)."
