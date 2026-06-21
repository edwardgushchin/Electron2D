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

$violations = New-Object System.Collections.Generic.List[string]

function Get-WikiPage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $path = Join-Path $resolvedWikiPath $Name
    if (-not (Test-Path -LiteralPath $path)) {
        $violations.Add("GitHub Wiki page is missing: $Name")
        return $null
    }

    return Get-Item -LiteralPath $path
}

function Assert-ContentMatches {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if ($Content -notmatch $Pattern) {
        $violations.Add("$RelativePath is missing required Wiki structure: $Description")
    }
}

function Assert-ContentDoesNotMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string]$RelativePath,

        [Parameter(Mandatory = $true)]
        [string]$Content,

        [Parameter(Mandatory = $true)]
        [string]$Pattern,

        [Parameter(Mandatory = $true)]
        [string]$Description
    )

    if ($Content -match $Pattern) {
        $violations.Add("$RelativePath contains forbidden Wiki structure: $Description")
    }
}

$requiredWikiPages = @(
    'Home.md',
    '_Sidebar.md',
    '_Footer.md',
    'API-by-Category.md',
    'API-Reference.md',
    'API-Compatibility.md'
)

foreach ($pageName in $requiredWikiPages) {
    [void](Get-WikiPage -Name $pageName)
}

$wikiFiles = Get-ChildItem -LiteralPath $resolvedWikiPath -File -Filter '*.md'
foreach ($file in $wikiFiles) {
    $relative = Get-RelativeRepositoryPath -Path $file.FullName
    $content = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
    Assert-ContentDoesNotMatch -RelativePath $relative -Content $content -Pattern '\]\([^)\s]+\.md(?:#[^)]+)?\)' -Description 'links must use GitHub Wiki page names without .md extensions.'
}

$homePage = Get-WikiPage -Name 'Home.md'
if ($null -ne $homePage) {
    $relative = Get-RelativeRepositoryPath -Path $homePage.FullName
    $content = [System.IO.File]::ReadAllText($homePage.FullName, [System.Text.Encoding]::UTF8)
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\[API by Category\]\(API-by-Category\)' -Description 'Home links to category API navigation.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\[Complete API Index\]\(API-Reference\)' -Description 'Home links to the complete API index.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\[API Compatibility\]\(API-Compatibility\)' -Description 'Home links to compatibility status.'
}

$apiReferencePage = Get-WikiPage -Name 'API-Reference.md'
if ($null -ne $apiReferencePage) {
    $relative = Get-RelativeRepositoryPath -Path $apiReferencePage.FullName
    $content = [System.IO.File]::ReadAllText($apiReferencePage.FullName, [System.Text.Encoding]::UTF8)
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\[Home\]\(Home\) \| \[API by Category\]\(API-by-Category\) \| \[Complete API Index\]\(API-Reference\) \| \[API Compatibility\]\(API-Compatibility\)' -Description 'complete top navigation.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '## Type Index' -Description 'complete public type index.'
}

$apiCompatibilityPage = Get-WikiPage -Name 'API-Compatibility.md'
if ($null -ne $apiCompatibilityPage) {
    $relative = Get-RelativeRepositoryPath -Path $apiCompatibilityPage.FullName
    $content = [System.IO.File]::ReadAllText($apiCompatibilityPage.FullName, [System.Text.Encoding]::UTF8)
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\[Home\]\(Home\) \| \[API by Category\]\(API-by-Category\) \| \[Complete API Index\]\(API-Reference\) \| \[API Compatibility\]\(API-Compatibility\)' -Description 'complete top navigation.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '## Status Legend' -Description 'status legend.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '## Current Public Runtime Surface' -Description 'current public API status table.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '## Planned 2D Surface' -Description 'planned preview surface table.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\| Supported \| Implemented, tested and documented \|' -Description 'supported status definition.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\| Partial \| Implemented only for the described subset \|' -Description 'partial status definition.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\| Experimental \| Implemented but allowed to change before stable release \|' -Description 'experimental status definition.'
    Assert-ContentMatches -RelativePath $relative -Content $content -Pattern '\| Planned \| Required by `0\.1\.0 Preview`, not implemented yet \|' -Description 'planned status definition.'
    Assert-ContentDoesNotMatch -RelativePath $relative -Content $content -Pattern '(?i)explicitly[- ]not[- ]planned|removed legacy|legacy API' -Description 'removed/legacy API block must not be published.'
}

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
