$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$changelogPath = Join-Path $repoRoot 'CHANGELOG.md'
$releaseNotesPath = Join-Path $repoRoot 'RELEASE-NOTES.md'
$readmePath = Join-Path $repoRoot 'README.md'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

[xml]$project = Get-Content -LiteralPath $projectPath
$propertyGroups = @($project.Project.PropertyGroup)

function Get-ProjectProperty([string]$name) {
    foreach ($group in $propertyGroups) {
        $node = $group.SelectSingleNode($name)
        if ($node -and -not [string]::IsNullOrWhiteSpace($node.InnerText)) {
            return $node.InnerText.Trim()
        }
    }

    return $null
}

$expectedProperties = @{
    Version = '0.1.0-preview'
    PackageVersion = '0.1.0-preview'
    AssemblyVersion = '0.1.0.0'
    FileVersion = '0.1.0.0'
    InformationalVersion = '0.1.0-preview'
    PackageId = 'Electron2D'
    Authors = 'Electron2D Team'
    PackageLicenseExpression = 'MIT'
    PackageReadmeFile = 'README.md'
    RepositoryType = 'git'
}

foreach ($entry in $expectedProperties.GetEnumerator()) {
    $actual = Get-ProjectProperty $entry.Key
    if ($actual -ne $entry.Value) {
        throw "Project metadata mismatch for $($entry.Key): expected '$($entry.Value)', got '$actual'."
    }
}

foreach ($path in @($changelogPath, $releaseNotesPath, $readmePath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required release metadata file was not found: $path"
    }
}

$changelog = Get-Content -LiteralPath $changelogPath -Raw
$releaseNotes = Get-Content -LiteralPath $releaseNotesPath -Raw
$readme = Get-Content -LiteralPath $readmePath -Raw

if ($changelog.IndexOf('0.1.0-preview', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CHANGELOG.md does not mention 0.1.0-preview.'
}

if ($releaseNotes.IndexOf('0.1.0 Preview', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'RELEASE-NOTES.md does not mention 0.1.0 Preview.'
}

if ($readme.IndexOf('0.1.0 Preview', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'README.md does not mention 0.1.0 Preview.'
}

if ($changelog.IndexOf('Breaking changes policy', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CHANGELOG.md does not describe the breaking changes policy.'
}

if ($releaseNotes.IndexOf('Breaking changes policy', [System.StringComparison]::OrdinalIgnoreCase) -lt 0 -or
    $releaseNotes.IndexOf('0.x', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'RELEASE-NOTES.md does not describe the 0.x breaking changes policy.'
}

Write-Host 'Release metadata verification passed.'
