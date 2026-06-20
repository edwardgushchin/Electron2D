$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$workflowPath = Join-Path $repoRoot '.github/workflows/ci.yml'

if (-not (Test-Path -LiteralPath $workflowPath)) {
    throw 'CI workflow .github/workflows/ci.yml was not found.'
}

$workflow = Get-Content -LiteralPath $workflowPath -Raw
$requiredFragments = @(
    'windows-latest',
    'ubuntu-latest',
    'macos-latest',
    'actions/checkout',
    'actions/setup-dotnet',
    '10.0.x',
    'src/Electron2D.sln',
    'tools/Run-Tests.ps1',
    'tools/Verify-ProjectTemplate.ps1',
    'tools/Verify-PerformanceBudgets.ps1',
    'mobile-export-status',
    'Android/iOS/export'
)

foreach ($fragment in $requiredFragments) {
    if ($workflow.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "CI workflow is missing required fragment: $fragment"
    }
}

if ($workflow.IndexOf('-IncludeBaseline', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'CI green path must not run -IncludeBaseline by default.'
}

Write-Host 'CI matrix verification passed.'
