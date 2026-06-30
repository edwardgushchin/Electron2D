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
    'tools/Verify-SourceDomainLayout.ps1',
    'dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600',
    'tools/Verify-Box2DPhysicsCandidate.ps1',
    'tools/Verify-ProjectTemplate.ps1',
    'tools/Verify-UserDocumentation.ps1',
    'tools/Verify-LocalDocumentation.ps1',
    'tools/Verify-CanonicalGoalAlignment.ps1',
    'tools/Verify-ExportDocumentation.ps1',
    'tools/Verify-PublicApiXmlDocs.ps1',
    'tools/Verify-PublicApiDocumentationAudit.ps1',
    'tools/Update-ApiWiki.ps1',
    'tools/Update-ApiManifest.ps1',
    'Electron2D.wiki.git',
    'tools/Verify-WindowsExport.ps1',
    'tools/Verify-LinuxExport.ps1',
    'tools/Verify-MacOSExport.ps1',
    'dotnet run --project eng/Electron2D.Build -- verify performance-budgets',
    'dotnet run --project eng/Electron2D.Build -- verify performance',
    'mobile-export-status',
    'Android/iOS/mobile export'
)

foreach ($fragment in $requiredFragments) {
    if ($workflow.IndexOf($fragment, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
        throw "CI workflow is missing required fragment: $fragment"
    }
}

if ($workflow.IndexOf('-IncludeBaseline', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'CI green path must not run -IncludeBaseline by default.'
}

if ($workflow.IndexOf('tools/Run-Tests.ps1', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'CI workflow must use the C# test runner instead of tools/Run-Tests.ps1.'
}

if ($workflow.IndexOf('tools/Verify-PerformanceBudgets.ps1', [System.StringComparison]::OrdinalIgnoreCase) -ge 0) {
    throw 'CI workflow must use C# performance verification instead of tools/Verify-PerformanceBudgets.ps1.'
}

if ($workflow.IndexOf('Verify-Box2DPhysicsCandidate.ps1 -NativeAot', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run Box2D.NET physics candidate validation with -NativeAot.'
}

if ($workflow.IndexOf('Verify-PublicApiXmlDocs.ps1 -FailOnIssues', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run public API XML documentation validation with -FailOnIssues.'
}

if ($workflow.IndexOf('Verify-PublicApiDocumentationAudit.ps1 -WikiPath .github/wiki', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run the consolidated public API documentation audit against the GitHub Wiki clone.'
}

if ($workflow.IndexOf('Verify-CanonicalGoalAlignment.ps1', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run canonical goal alignment verification.'
}

if ($workflow.IndexOf('Verify-LocalDocumentation.ps1', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run local documentation verification.'
}

if ($workflow.IndexOf('Update-ApiWiki.ps1 -OutputPath .github/wiki -Check', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run GitHub Wiki API reference validation against the GitHub Wiki clone with -Check.'
}

if ($workflow.IndexOf('Update-ApiManifest.ps1 -WikiPath .github/wiki -Check', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run API manifest validation against the GitHub Wiki clone with -Check.'
}

if ($workflow.IndexOf("if: matrix.os == 'windows-latest'", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run Windows export verification only on windows-latest.'
}

if ($workflow.IndexOf("if: matrix.os == 'ubuntu-latest'", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run Linux export verification only on ubuntu-latest.'
}

if ($workflow.IndexOf("if: matrix.os == 'macos-latest'", [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
    throw 'CI workflow must run macOS export verification only on macos-latest.'
}

Write-Host 'CI matrix verification passed.'
