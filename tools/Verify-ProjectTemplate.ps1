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
$templateRoot = Join-Path $repoRoot 'data/templates/electron2d-empty'
$packageOutput = Join-Path $repoRoot '.temp/template-package'
$workRoot = Join-Path $repoRoot '.temp/template-check'
$createdProject = Join-Path $workRoot 'Electron2D.Empty'
$projectPath = Join-Path $createdProject 'Electron2D.Empty.csproj'
$packagesRoot = Join-Path $workRoot '.nuget-packages'
$nugetConfig = Join-Path $workRoot 'NuGet.Config'

if (-not (Test-Path -LiteralPath $templateRoot)) {
    throw 'Template directory data/templates/electron2d-empty was not found.'
}

$requiredFiles = @(
    '.template.config/template.json',
    'Electron2D.Empty.csproj',
    'global.json',
    'electron2d.lock.json',
    'Program.cs',
    'Scripts/MainScene.cs',
    'project.e2d.json',
    'scenes/main.scene.json',
    'README.md',
    '.gitignore',
    'AGENTS.md',
    '.codex/skills/electron2d-scene/SKILL.md',
    '.codex/skills/electron2d-gameplay-code/SKILL.md',
    '.codex/skills/electron2d-resource-import/SKILL.md',
    '.codex/skills/electron2d-run-test/SKILL.md',
    '.codex/skills/electron2d-export/SKILL.md',
    '.electron2d/tasks/board.e2tasks',
    '.electron2d/tasks/welcome.e2task'
)

foreach ($relativePath in $requiredFiles) {
    $path = Join-Path $templateRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Template file was not found: $relativePath"
    }
}

Remove-Item -LiteralPath $packageOutput -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $workRoot -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $packageOutput | Out-Null
New-Item -ItemType Directory -Force -Path $createdProject | Out-Null

dotnet pack (Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj') --no-restore -o $packageOutput | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Copy-Item -Path (Join-Path $templateRoot '*') -Destination $createdProject -Recurse -Force
Remove-Item -LiteralPath (Join-Path $createdProject '.template.config') -Recurse -Force

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="electron2d-local" value="$packageOutput" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -LiteralPath $nugetConfig -Encoding UTF8

dotnet restore $projectPath --configfile $nugetConfig --packages $packagesRoot | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

dotnet build $projectPath --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$runOutput = dotnet run --project $projectPath --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host $runOutput
    exit $LASTEXITCODE
}

$joinedOutput = $runOutput -join [Environment]::NewLine
$expectedOutput = 'Electron2D empty scene loaded: scenes/main.scene.json'
if ($joinedOutput.IndexOf($expectedOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    Write-Host $runOutput
    throw "Template run output does not contain expected line: $expectedOutput"
}

$expectedLifecycleOutput = 'Electron2D C# script lifecycle: _EnterTree,_Ready'
if ($joinedOutput.IndexOf($expectedLifecycleOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    Write-Host $runOutput
    throw "Template run output does not contain expected line: $expectedLifecycleOutput"
}

$expectedServiceOutput = 'Electron2D C# script services: tree=True,text=True'
if ($joinedOutput.IndexOf($expectedServiceOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    Write-Host $runOutput
    throw "Template run output does not contain expected line: $expectedServiceOutput"
}

$forbiddenProjectFiles = @(
    'TASKS.md',
    'completed-tasks',
    'dev-diary'
)

foreach ($relativePath in $forbiddenProjectFiles) {
    $path = Join-Path $createdProject $relativePath
    if (Test-Path -LiteralPath $path) {
        throw "Template must not create repository workflow file or directory: $relativePath"
    }
}

$agentInstructionsPath = Join-Path $createdProject 'AGENTS.md'
$agentInstructions = Get-Content -Raw -LiteralPath $agentInstructionsPath
foreach ($requiredText in @(
    'Electron2D 0.1.0-preview',
    '.NET 10.0.101',
    'e2d validate',
    'e2d api compare-godot <type>',
    'ProjectTaskManager',
    'task_submit_for_acceptance'
)) {
    if ($agentInstructions.IndexOf($requiredText, [System.StringComparison]::Ordinal) -lt 0) {
        throw "AGENTS.md does not contain required text: $requiredText"
    }
}

if ($agentInstructions.IndexOf('TASKS.md', [System.StringComparison]::Ordinal) -ge 0 -or
    $agentInstructions.IndexOf('completed-tasks', [System.StringComparison]::Ordinal) -ge 0 -or
    $agentInstructions.IndexOf('dev-diary', [System.StringComparison]::Ordinal) -ge 0) {
    throw 'AGENTS.md must not point user projects at repository-local Markdown workflow files.'
}

$gitIgnoreLines = Get-Content -LiteralPath (Join-Path $createdProject '.gitignore')
foreach ($requiredLine in @(
    '.electron2d/import-cache/',
    '.electron2d/workspaces/',
    '.electron2d/context/',
    '.electron2d/session/',
    '.electron2d/user/'
)) {
    if ($gitIgnoreLines -notcontains $requiredLine) {
        throw ".gitignore is missing required line: $requiredLine"
    }
}

if ($gitIgnoreLines -contains '.electron2d/' -or $gitIgnoreLines -contains '.electron2d/tasks/') {
    throw '.gitignore must not hide .electron2d/tasks/.'
}

$skillFiles = Get-ChildItem -LiteralPath (Join-Path $createdProject '.codex/skills') -Recurse -Filter 'SKILL.md'
if ($skillFiles.Count -ne 5) {
    throw "Expected 5 starter skills, found $($skillFiles.Count)."
}

$board = Get-Content -Raw -LiteralPath (Join-Path $createdProject '.electron2d/tasks/board.e2tasks') | ConvertFrom-Json
if ($board.format -ne 'Electron2D.TaskBoard') {
    throw 'Task board format is invalid.'
}

$welcomeTask = Get-Content -Raw -LiteralPath (Join-Path $createdProject '.electron2d/tasks/welcome.e2task') | ConvertFrom-Json
if ($welcomeTask.format -ne 'Electron2D.TaskFile' -or $welcomeTask.status -ne 'Backlog') {
    throw 'Starter task document is invalid.'
}

Write-Host 'Project template verification passed.'
