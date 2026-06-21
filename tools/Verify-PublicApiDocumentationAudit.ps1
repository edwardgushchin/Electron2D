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
param(
    [string]$WikiPath = '.github/wiki'
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot

function Resolve-RepositoryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $repoRoot $Path))
}

function Get-RelativeRepositoryPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $rootPath = [System.IO.Path]::GetFullPath($repoRoot)
    $candidatePath = [System.IO.Path]::GetFullPath($Path)
    if (-not $rootPath.EndsWith([System.IO.Path]::DirectorySeparatorChar) -and
        -not $rootPath.EndsWith([System.IO.Path]::AltDirectorySeparatorChar)) {
        $rootPath += [System.IO.Path]::DirectorySeparatorChar
    }

    $rootUri = [System.Uri]::new($rootPath)
    $candidateUri = [System.Uri]::new($candidatePath)
    $relativeUri = $rootUri.MakeRelativeUri($candidateUri)
    return [System.Uri]::UnescapeDataString($relativeUri.ToString()).Replace('\', '/')
}

function Invoke-CheckedScript {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ScriptPath,

        [string[]]$Arguments = @()
    )

    $powerShellCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($null -eq $powerShellCommand) {
        $powerShellCommand = Get-Command powershell -ErrorAction SilentlyContinue
    }

    if ($null -eq $powerShellCommand) {
        throw 'PowerShell executable was not found.'
    }

    $powerShellArguments = @('-NoProfile')
    if (($PSVersionTable.PSEdition -ne 'Core') -or ($IsWindows -eq $true)) {
        $powerShellArguments += @('-ExecutionPolicy', 'Bypass')
    }

    $powerShellArguments += @('-File', $ScriptPath)

    & $powerShellCommand.Source @powerShellArguments @Arguments
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

$resolvedWikiPath = Resolve-RepositoryPath -Path $WikiPath
if (-not (Test-Path -LiteralPath $resolvedWikiPath)) {
    throw "GitHub Wiki local clone was not found: $resolvedWikiPath"
}

$publicApiXmlDocsScript = Join-Path $PSScriptRoot 'Verify-PublicApiXmlDocs.ps1'
$apiWikiScript = Join-Path $PSScriptRoot 'Update-ApiWiki.ps1'

Invoke-CheckedScript -ScriptPath $publicApiXmlDocsScript -Arguments @('-FailOnIssues')
Invoke-CheckedScript -ScriptPath $apiWikiScript -Arguments @('-OutputPath', $resolvedWikiPath, '-Check')

$documentationRoots = @(
    (Join-Path $repoRoot 'docs/specifications/documentation'),
    (Join-Path $repoRoot 'docs/documentation/documentation'),
    $resolvedWikiPath
)

$forbiddenPatterns = @(
    @{ Pattern = '\bSDL\b'; Description = 'SDL family name' },
    @{ Pattern = 'SDL3'; Description = 'SDL family name' },
    @{ Pattern = 'SDL_GPU'; Description = 'backend library name' },
    @{ Pattern = 'SDL_Renderer'; Description = 'backend library name' },
    @{ Pattern = 'SDL_ttf'; Description = 'backend library name' },
    @{ Pattern = 'SDL_mixer'; Description = 'backend library name' },
    @{ Pattern = 'SDL_shadercross'; Description = 'backend library name' },
    @{ Pattern = 'Simple DirectMedia'; Description = 'backend library name' },
    @{ Pattern = 'Godot-like'; Description = 'promotional Godot comparison' },
    @{ Pattern = 'Godot-подоб'; Description = 'promotional Godot comparison' }
)

$scannedFiles = 0
$violations = New-Object System.Collections.Generic.List[string]

foreach ($root in $documentationRoots) {
    if (-not (Test-Path -LiteralPath $root)) {
        throw "Public API documentation audit root was not found: $root"
    }

    $files = Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.md' |
        Where-Object { $_.Name -ne 'README.md' -and $_.FullName -notmatch '\\\.git\\' }

    foreach ($file in $files) {
        $scannedFiles++
        $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        foreach ($rule in $forbiddenPatterns) {
            if ($content -match $rule.Pattern) {
                $relative = Get-RelativeRepositoryPath -Path $file.FullName
                $violations.Add("$relative contains forbidden public wording: $($rule.Description) / $($rule.Pattern)")
            }
        }

        if ($content -match '(?i)\b(todo|tbd)\b') {
            $relative = Get-RelativeRepositoryPath -Path $file.FullName
            $violations.Add("$relative contains TODO/TBD placeholder text.")
        }
    }
}

if ($violations.Count -gt 0) {
    foreach ($violation in $violations) {
        Write-Host $violation
    }

    throw "Public API documentation audit found $($violations.Count) issue(s)."
}

Write-Host "Public API documentation audit passed. Scanned Markdown files: $scannedFiles."
