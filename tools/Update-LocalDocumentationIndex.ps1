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
    [string]$OutputPath,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$defaultIndexPath = Join-Path $repoRoot 'data/documentation/electron2d-local-docs-index.json'
$apiManifestPath = Join-Path $repoRoot 'data/api/electron2d-api-manifest.json'
$examplesPath = Join-Path $repoRoot 'data/documentation/electron2d-doc-examples.json'
$workRoot = Join-Path $repoRoot '.temp/local-documentation'

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

function Get-RelativeUnixPath {
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

function Get-FileHashRecord {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    [ordered]@{
        path = Get-RelativeUnixPath -Path $Path
        sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
    }
}

function Normalize-Whitespace([string]$Value) {
    return (($Value -replace '\s+', ' ').Trim())
}

function Get-ObjectSortValue([object]$Value, [string]$PropertyName) {
    if ([string]::IsNullOrEmpty($PropertyName)) {
        return [string]$Value
    }

    if ($Value -is [System.Collections.IDictionary]) {
        return [string]$Value[$PropertyName]
    }

    return [string]$Value.$PropertyName
}

function Sort-OrdinalUniqueStrings([object[]]$Values) {
    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($value in @($Values)) {
        if ($null -ne $value) {
            [void]$set.Add([string]$value)
        }
    }

    $items = [string[]]$set
    [array]::Sort($items, [System.StringComparer]::Ordinal)
    return $items
}

function Sort-OrdinalObjects([object[]]$Values, [string[]]$PropertyNames) {
    $items = [System.Collections.Generic.List[object]]::new()
    foreach ($value in @($Values)) {
        if ($null -ne $value) {
            $items.Add($value)
        }
    }

    $comparison = [System.Comparison[object]] {
        param($left, $right)

        foreach ($propertyName in $PropertyNames) {
            $result = [System.StringComparer]::Ordinal.Compare(
                (Get-ObjectSortValue -Value $left -PropertyName $propertyName),
                (Get-ObjectSortValue -Value $right -PropertyName $propertyName))
            if ($result -ne 0) {
                return $result
            }
        }

        return 0
    }
    $items.Sort($comparison)
    return $items.ToArray()
}

function Split-SearchWords([string]$Value) {
    $words = New-Object System.Collections.Generic.List[string]
    foreach ($part in [regex]::Matches($Value, '[A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+')) {
        $text = $part.Value.ToLowerInvariant()
        if (-not [string]::IsNullOrWhiteSpace($text)) {
            $words.Add($text)
        }
    }

    foreach ($part in [regex]::Matches($Value.ToLowerInvariant(), '[a-z0-9]+')) {
        if (-not $words.Contains($part.Value)) {
            $words.Add($part.Value)
        }
    }

    return Sort-OrdinalUniqueStrings $words
}

function Get-MarkdownSummary([string]$Text) {
    $withoutCodeBlocks = [regex]::Replace($Text, '(?s)```.*?```', ' ')
    $lines = $withoutCodeBlocks -split "`r?`n"
    foreach ($line in $lines) {
        $trimmed = $line.Trim()
        if ([string]::IsNullOrWhiteSpace($trimmed)) {
            continue
        }

        if ($trimmed.StartsWith('#', [System.StringComparison]::Ordinal)) {
            continue
        }

        if ($trimmed.StartsWith('<!--', [System.StringComparison]::Ordinal)) {
            continue
        }

        if ($trimmed.StartsWith('|', [System.StringComparison]::Ordinal)) {
            continue
        }

        return Normalize-Whitespace $trimmed
    }

    return ''
}

function Get-MarkdownTitle([string]$Text, [string]$Fallback) {
    $match = [regex]::Match($Text, '(?m)^#\s+(.+)$')
    if ($match.Success) {
        return Normalize-Whitespace $match.Groups[1].Value
    }

    return $Fallback
}

function New-DocumentationEntry {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $relative = Get-RelativeUnixPath -Path $Path
    $text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
    $stem = $relative -replace '^docs/', ''
    $stem = $stem -replace '\.md$', ''
    $id = 'doc:' + ($stem -replace '/', '.')
    if ($relative -eq 'docs/architecture/agent-native-workflow.md') {
        $id = 'doc:architecture.agent-native-workflow'
    }

    $title = Get-MarkdownTitle -Text $text -Fallback $stem
    $summary = Get-MarkdownSummary -Text $text
    $keywords = @(
        Split-SearchWords $title
        Split-SearchWords $summary
        Split-SearchWords $relative
    )
    $keywords = Sort-OrdinalUniqueStrings $keywords

    [ordered]@{
        id = $id
        kind = 'documentation'
        title = $title
        summary = $summary
        keywords = @($keywords)
        sourcePath = $relative
        sourceId = $id
        audiences = @('human', 'ai', 'cli', 'wiki')
    }
}

function New-TypeEntry {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Type
    )

    $keywords = @(
        Split-SearchWords $Type.fullName
        Split-SearchWords $Type.name
        Split-SearchWords $Type.category
        Split-SearchWords $Type.summary
    )
    $keywords = Sort-OrdinalUniqueStrings $keywords

    [ordered]@{
        id = "api-type:$($Type.fullName)"
        kind = 'api-type'
        title = $Type.name
        summary = $Type.summary
        keywords = @($keywords)
        sourcePath = 'data/api/electron2d-api-manifest.json'
        apiId = $Type.id
        audiences = @('ai', 'cli', 'ide', 'wiki', 'inspector', 'generator')
    }
}

function New-MemberEntry {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Type,

        [Parameter(Mandatory = $true)]
        [object]$Member,

        [Parameter(Mandatory = $true)]
        [int]$DuplicateIndex
    )

    $baseId = "api-member:$($Member.declaringType).$($Member.name)"
    $id = if ($DuplicateIndex -eq 0) {
        $baseId
    }
    else {
        "$baseId#$DuplicateIndex"
    }

    $shortType = $Type.name
    $title = "$shortType.$($Member.name)"
    $keywords = @(
        Split-SearchWords $title
        Split-SearchWords $Member.name
        Split-SearchWords $Member.signature
        Split-SearchWords $Member.summary
        Split-SearchWords $Type.category
    )
    $keywords = Sort-OrdinalUniqueStrings $keywords

    [ordered]@{
        id = $id
        kind = 'api-member'
        title = $title
        summary = $Member.summary
        keywords = @($keywords)
        sourcePath = 'data/api/electron2d-api-manifest.json'
        apiId = $Member.id
        audiences = @('ai', 'cli', 'ide', 'wiki', 'inspector', 'generator')
    }
}

function New-ExampleEntry {
    param(
        [Parameter(Mandatory = $true)]
        [object]$Example
    )

    $keywords = @(
        Split-SearchWords $Example.id
        Split-SearchWords $Example.title
        Split-SearchWords $Example.summary
        @($Example.keywords | ForEach-Object { Split-SearchWords $_ })
    )
    $keywords = Sort-OrdinalUniqueStrings $keywords

    [ordered]@{
        id = $Example.id
        kind = 'example'
        title = $Example.title
        summary = $Example.summary
        keywords = @($keywords)
        sourcePath = 'data/documentation/electron2d-doc-examples.json'
        sourceId = $Example.id
        apiIds = @($Example.apiIds)
        code = $Example.code
        audiences = @('human', 'ai', 'cli', 'ide')
    }
}

foreach ($path in @($apiManifestPath, $examplesPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required documentation source file was not found: $path"
    }
}

$targetIndexPath = if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $defaultIndexPath
}
else {
    Resolve-RepositoryPath -Path $OutputPath
}

if ($Check) {
    New-Item -ItemType Directory -Force -Path $workRoot | Out-Null
    $targetForGeneration = Join-Path $workRoot 'expected/electron2d-local-docs-index.json'
}
else {
    $targetForGeneration = $targetIndexPath
}

$apiManifest = Get-Content -LiteralPath $apiManifestPath -Raw | ConvertFrom-Json
$examples = Get-Content -LiteralPath $examplesPath -Raw | ConvertFrom-Json

$documentationFiles = New-Object System.Collections.Generic.List[string]
foreach ($file in (Sort-OrdinalObjects (Get-ChildItem -LiteralPath (Join-Path $repoRoot 'docs') -Recurse -File -Filter '*.md') @('FullName'))) {
    $documentationFiles.Add($file.FullName)
}

$entries = New-Object System.Collections.Generic.List[object]
foreach ($type in (Sort-OrdinalObjects $apiManifest.types @('fullName'))) {
    $entries.Add((New-TypeEntry -Type $type))

    $duplicateKeys = @{}
    foreach ($member in (Sort-OrdinalObjects $type.members @('name', 'id'))) {
        $key = "$($member.declaringType).$($member.name)"
        $duplicateIndex = if ($duplicateKeys.ContainsKey($key)) {
            $duplicateKeys[$key] + 1
        }
        else {
            0
        }
        $duplicateKeys[$key] = $duplicateIndex
        $entries.Add((New-MemberEntry -Type $type -Member $member -DuplicateIndex $duplicateIndex))
    }
}

foreach ($documentationFile in (Sort-OrdinalUniqueStrings $documentationFiles)) {
    if (-not (Test-Path -LiteralPath $documentationFile)) {
        throw "Documentation source file was not found: $documentationFile"
    }

    $entries.Add((New-DocumentationEntry -Path $documentationFile))
}

foreach ($example in (Sort-OrdinalObjects $examples.examples @('id'))) {
    $entries.Add((New-ExampleEntry -Example $example))
}

$documentationHashRecords = @(Sort-OrdinalUniqueStrings $documentationFiles | ForEach-Object { Get-FileHashRecord -Path $_ })

$index = [ordered]@{
    schemaVersion = 1
    manifestVersion = '0.1.0-preview'
    generatedFrom = [ordered]@{
        apiManifest = Get-FileHashRecord -Path $apiManifestPath
        documentation = @($documentationHashRecords)
        examples = Get-FileHashRecord -Path $examplesPath
    }
    audiences = @('human', 'ai', 'cli', 'ide', 'wiki', 'inspector', 'generator')
    commands = @(
        [ordered]@{
            name = 'docs search'
            description = 'Searches local API, documentation and examples index.'
            formats = @('text', 'json')
        },
        [ordered]@{
            name = 'docs type'
            description = 'Returns a public API type from the API manifest.'
            formats = @('text', 'json')
        },
        [ordered]@{
            name = 'docs member'
            description = 'Returns a public API member from the API manifest.'
            formats = @('text', 'json')
        },
        [ordered]@{
            name = 'docs example'
            description = 'Returns a local documentation example.'
            formats = @('text', 'json')
        }
    )
    sources = [ordered]@{
        apiManifest = [ordered]@{
            path = 'data/api/electron2d-api-manifest.json'
            contract = 'Public API metadata generated from compiled assembly, XML documentation and GitHub Wiki compatibility table.'
        }
        documentation = [ordered]@{
            paths = @($documentationHashRecords.path)
            contract = 'Current implementation documentation and Agent-native cross-platform 2D game engine architecture notes.'
        }
        examples = [ordered]@{
            path = 'data/documentation/electron2d-doc-examples.json'
            contract = 'Curated local examples for CLI and AI agents.'
        }
        wiki = [ordered]@{
            generator = 'tools/Update-ApiWiki.ps1'
            compatibilityPage = '.github/wiki/API-Compatibility.md'
        }
    }
    entries = @(Sort-OrdinalObjects $entries @('id'))
}

function ConvertTo-ComparableJson([string]$jsonText) {
    $document = $jsonText | ConvertFrom-Json
    return ($document | ConvertTo-Json -Depth 100 -Compress)
}

$json = ($index | ConvertTo-Json -Depth 100).Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd() + "`n"
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $targetForGeneration) | Out-Null
$utf8NoBom = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($targetForGeneration, $json, $utf8NoBom)

if ($Check) {
    if (-not (Test-Path -LiteralPath $targetIndexPath)) {
        throw "Local documentation index was not found: $targetIndexPath"
    }

    $expectedText = ConvertTo-ComparableJson ([System.IO.File]::ReadAllText($targetForGeneration))
    $actualText = ConvertTo-ComparableJson ([System.IO.File]::ReadAllText($targetIndexPath))
    if (-not [System.String]::Equals($expectedText, $actualText, [System.StringComparison]::Ordinal)) {
        throw "Local documentation index is out of date: $targetIndexPath"
    }

    Write-Host "Local documentation index verification passed: $targetIndexPath"
    exit 0
}

Write-Host "Local documentation index updated: $targetForGeneration"
