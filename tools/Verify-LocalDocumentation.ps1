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
$updateScript = Join-Path $PSScriptRoot 'Update-LocalDocumentationIndex.ps1'
$solutionPath = Join-Path $repoRoot 'src/Electron2D.sln'
$cliProject = Join-Path $repoRoot 'src/Electron2D.Cli/Electron2D.Cli.csproj'
$indexPath = Join-Path $repoRoot 'data/documentation/electron2d-local-docs-index.json'
$implementationDocPath = Join-Path $repoRoot 'docs/documentation/documentation/local-documentation-pipeline.md'

if (-not (Test-Path -LiteralPath $updateScript)) {
    throw "Local documentation index updater was not found: $updateScript"
}

& $updateScript -Check
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

foreach ($path in @($solutionPath, $cliProject, $indexPath, $implementationDocPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required local documentation artifact was not found: $path"
    }
}

$solution = [System.IO.File]::ReadAllText($solutionPath, [System.Text.Encoding]::UTF8)
if ($solution.IndexOf('Electron2D.Cli', [System.StringComparison]::Ordinal) -lt 0) {
    throw 'Electron2D.Cli project is not part of src/Electron2D.sln.'
}

$index = Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json
if ($index.schemaVersion -ne 1) {
    throw 'Local documentation index schemaVersion must be 1.'
}

$requiredAudiences = @('human', 'ai', 'cli', 'ide', 'wiki', 'inspector', 'generator')
foreach ($audience in $requiredAudiences) {
    if ($index.audiences -notcontains $audience) {
        throw "Local documentation index is missing audience: $audience"
    }
}

$requiredCommands = @('docs search', 'docs type', 'docs member', 'docs example')
foreach ($command in $requiredCommands) {
    if (-not ($index.commands | Where-Object { $_.name -eq $command })) {
        throw "Local documentation index is missing command metadata: $command"
    }
}

$requiredEntryIds = @(
    'api-type:Electron2D.CharacterBody2D',
    'api-member:Electron2D.CharacterBody2D.MoveAndSlide',
    'doc:architecture.agent-native-workflow',
    'example:platformer-movement'
)
foreach ($entryId in $requiredEntryIds) {
    if (-not ($index.entries | Where-Object { $_.id -eq $entryId })) {
        throw "Local documentation index is missing required entry: $entryId"
    }
}

dotnet build $cliProject
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

function Invoke-E2DJson {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & dotnet run --project $cliProject --no-build -- @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = $output -join [Environment]::NewLine
    if ($exitCode -ne 0) {
        throw "e2d command failed: $($Arguments -join ' ')`n$text"
    }

    return $text | ConvertFrom-Json
}

function Invoke-E2DText {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    $output = & dotnet run --project $cliProject --no-build -- @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = $output -join [Environment]::NewLine
    if ($exitCode -ne 0) {
        throw "e2d command failed: $($Arguments -join ' ')`n$text"
    }

    return $text
}

$search = Invoke-E2DJson -Arguments @('docs', 'search', 'move and slide', '--format', 'json')
if (-not ($search.results | Where-Object { $_.kind -eq 'api-member' -and $_.title -eq 'CharacterBody2D.MoveAndSlide' })) {
    throw 'e2d docs search did not return CharacterBody2D.MoveAndSlide from the local index.'
}

$type = Invoke-E2DJson -Arguments @('docs', 'type', 'CharacterBody2D', '--format', 'json')
if ($type.type.fullName -ne 'Electron2D.CharacterBody2D') {
    throw 'e2d docs type CharacterBody2D did not return the manifest type entry.'
}

$member = Invoke-E2DJson -Arguments @('docs', 'member', 'CharacterBody2D.MoveAndSlide', '--format', 'json')
if ($member.member.declaringType -ne 'Electron2D.CharacterBody2D' -or $member.member.name -ne 'MoveAndSlide') {
    throw 'e2d docs member CharacterBody2D.MoveAndSlide did not return the manifest member entry.'
}

$example = Invoke-E2DJson -Arguments @('docs', 'example', 'platformer movement', '--format', 'json')
if ($example.example.id -ne 'example:platformer-movement') {
    throw 'e2d docs example did not return the platformer movement example.'
}

$textChecks = @(
    @{ Arguments = @('docs', 'search', 'move and slide', '--format', 'text'); Fragment = 'CharacterBody2D.MoveAndSlide' },
    @{ Arguments = @('docs', 'type', 'CharacterBody2D', '--format', 'text'); Fragment = 'Electron2D.CharacterBody2D' },
    @{ Arguments = @('docs', 'member', 'CharacterBody2D.MoveAndSlide', '--format', 'text'); Fragment = 'MoveAndSlide' },
    @{ Arguments = @('docs', 'example', 'platformer movement', '--format', 'text'); Fragment = 'PlayerController' }
)

foreach ($check in $textChecks) {
    $text = Invoke-E2DText -Arguments $check.Arguments
    if ($text.IndexOf($check.Fragment, [System.StringComparison]::Ordinal) -lt 0) {
        throw "e2d text command did not contain expected fragment '$($check.Fragment)': $($check.Arguments -join ' ')"
    }
}

$implementationDoc = [System.IO.File]::ReadAllText($implementationDocPath, [System.Text.Encoding]::UTF8)
$requiredDocFragments = @(
    'ProjectWorkspace',
    'ProjectTaskManager',
    'TaskActivity',
    'MCP/IPC',
    'Agent Workspace panel',
    'external change synchronizer',
    'conflict panel',
    'grouped Undo/Redo',
    'visible runtime control',
    'Editor Capability Manifest',
    'MCP resources',
    'TASKS.md',
    'completed-tasks/',
    'dev-diary/',
    'Verify-LocalDocumentation.ps1',
    'Update-LocalDocumentationIndex.ps1'
)

foreach ($fragment in $requiredDocFragments) {
    if ($implementationDoc.IndexOf($fragment, [System.StringComparison]::Ordinal) -lt 0) {
        throw "Local documentation implementation doc is missing required fragment: $fragment"
    }
}

Write-Host 'Local documentation verification passed.'
