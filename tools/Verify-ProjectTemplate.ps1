$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateRoot = Join-Path $repoRoot 'templates/electron2d-empty'
$packageOutput = Join-Path $repoRoot '.temp/template-package'
$workRoot = Join-Path $repoRoot '.temp/template-check'
$createdProject = Join-Path $workRoot 'Electron2D.Empty'
$projectPath = Join-Path $createdProject 'Electron2D.Empty.csproj'

if (-not (Test-Path -LiteralPath $templateRoot)) {
    throw 'Template directory templates/electron2d-empty was not found.'
}

$requiredFiles = @(
    '.template.config/template.json',
    'Electron2D.Empty.csproj',
    'Program.cs',
    'project.e2d.json',
    'scenes/main.scene.json',
    'README.md'
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

dotnet restore $projectPath --source $packageOutput | Out-Host
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

$expectedOutput = 'Electron2D empty scene loaded: scenes/main.scene.json'
if (($runOutput -join [Environment]::NewLine).IndexOf($expectedOutput, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    Write-Host $runOutput
    throw "Template run output does not contain expected line: $expectedOutput"
}

Write-Host 'Project template verification passed.'
