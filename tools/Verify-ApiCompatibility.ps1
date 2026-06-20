$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'src/Electron2D/Electron2D.csproj'
$wikiPath = Join-Path $repoRoot '.github/wiki/API-Compatibility.md'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw 'Runtime project src/Electron2D/Electron2D.csproj was not found.'
}

if (-not (Test-Path -LiteralPath $wikiPath)) {
    throw 'GitHub Wiki source .github/wiki/API-Compatibility.md was not found.'
}

dotnet build $projectPath | Out-Host
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$assemblyPath = Join-Path $repoRoot 'src/Electron2D/bin/Debug/net10.0/Electron2D.dll'
if (-not (Test-Path -LiteralPath $assemblyPath)) {
    throw 'Runtime assembly was not produced.'
}

$assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
$publicTypes = $assembly.GetExportedTypes() | ForEach-Object { $_.FullName } | Sort-Object
$wiki = Get-Content -LiteralPath $wikiPath -Raw
$allowedStatuses = @('Supported', 'Partial', 'Experimental', 'Planned', 'Not planned')

foreach ($typeName in $publicTypes) {
    if ($wiki.IndexOf("| ``$typeName`` |", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Public type is missing from GitHub Wiki compatibility table: $typeName"
    }
}

foreach ($status in $allowedStatuses) {
    if ($wiki.IndexOf("| $status |", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Compatibility table does not contain status: $status"
    }
}

$forbiddenTypes = @(
    'Electron2D.IComponent',
    'Electron2D.SpriteRenderer',
    'Electron2D.SpriteAnimator',
    'Electron2D.AudioSource',
    'Electron2D.Rigidbody',
    'Electron2D.Collider',
    'Electron2D.BoxCollider',
    'Electron2D.CircleCollider',
    'Electron2D.PolygonCollider',
    'Electron2D.PhysicsBodyType'
)

foreach ($forbiddenType in $forbiddenTypes) {
    if ($publicTypes -contains $forbiddenType) {
        throw "Forbidden legacy type is exported: $forbiddenType"
    }
}

if (Test-Path -LiteralPath (Join-Path $repoRoot 'mkdocs.yml')) {
    throw 'Local documentation site configuration mkdocs.yml is not allowed for the GitHub Wiki table.'
}

if (Test-Path -LiteralPath (Join-Path $repoRoot 'site')) {
    throw 'Local generated site directory is not allowed for the GitHub Wiki table.'
}

Write-Host "API compatibility verification passed. Public types: $($publicTypes.Count)."
