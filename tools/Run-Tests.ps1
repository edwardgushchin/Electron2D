param(
    [switch]$IncludeBaseline
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$projects = @(
    'tests/Electron2D.Tests.Unit/Electron2D.Tests.Unit.csproj',
    'tests/Electron2D.Tests.Integration/Electron2D.Tests.Integration.csproj',
    'tests/Electron2D.Tests.RuntimeSmoke/Electron2D.Tests.RuntimeSmoke.csproj',
    'tests/Electron2D.Tests.GoldenData/Electron2D.Tests.GoldenData.csproj'
)

$filterArgs = @()
if (-not $IncludeBaseline) {
    $filterArgs = @('--filter', 'Category!=Baseline')
}

foreach ($project in $projects) {
    $projectPath = Join-Path $repoRoot $project
    dotnet test $projectPath @filterArgs
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}
