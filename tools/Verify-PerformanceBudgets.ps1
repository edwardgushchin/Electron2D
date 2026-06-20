$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$specPath = Join-Path $repoRoot 'docs/specifications/release-management/performance-budgets.md'
$docPath = Join-Path $repoRoot 'docs/documentation/release-management/performance-budgets.md'

foreach ($path in @($specPath, $docPath)) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Performance budget document was not found: $path"
    }
}

$combined = (Get-Content -LiteralPath $specPath -Raw) + "`n" + (Get-Content -LiteralPath $docPath -Raw)
$requiredFragments = @(
    'Windows',
    'Linux',
    'macOS',
    'Android',
    'iOS',
    '60 FPS',
    '16.67 ms',
    'memory budget',
    '30-minute',
    'soak',
    'background/foreground',
    'mobile'
)

foreach ($fragment in $requiredFragments) {
    if ($combined.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "Performance budget docs are missing required fragment: $fragment"
    }
}

Write-Host 'Performance budget documentation verification passed.'
