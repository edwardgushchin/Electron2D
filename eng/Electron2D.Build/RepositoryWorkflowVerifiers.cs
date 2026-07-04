/*
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
*/

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Electron2D.Build;

internal sealed class CiMatrixVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private static readonly string[] RequiredFragments =
    [
        "windows-latest",
        "ubuntu-latest",
        "macos-latest",
        "actions/checkout@v4",
        "actions/setup-dotnet@v4",
        "10.0.x",
        "dotnet restore src/Electron2D.sln",
        "dotnet build src/Electron2D.sln --no-restore",
        "dotnet run --project eng/Electron2D.Build -- verify no-powershell-workflows",
        "dotnet run --project eng/Electron2D.Build -- verify ci-matrix",
        "dotnet run --project eng/Electron2D.Build -- verify licenses",
        "dotnet run --project eng/Electron2D.Build -- verify source-domain-layout",
        "dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600 --integration-slice fast --no-build --no-restore",
        "integration-slices",
        "integration-slice:",
        "repository-tooling",
        "audit-medium",
        "audit-heavy",
        "external-process",
        "slow",
        "dotnet run --project eng/Electron2D.Build -- test --timeout-seconds 3600 --integration-slice ${{ matrix.integration-slice }} --no-build --no-restore",
        "dotnet run --project eng/Electron2D.Build -- verify box2d-physics-candidate --native-aot",
        "dotnet run --project eng/Electron2D.Build -- verify project-template",
        "dotnet run --project eng/Electron2D.Build -- verify user-documentation",
        "dotnet run --project eng/Electron2D.Build -- verify docs",
        "dotnet run --project eng/Electron2D.Build -- verify canonical-goal-alignment",
        "dotnet run --project eng/Electron2D.Build -- verify export-documentation",
        "dotnet run --project eng/Electron2D.Build -- verify reference-game-assets",
        "dotnet run --project eng/Electron2D.Build -- verify public-api-xml-docs --fail-on-issues",
        "Electron2D.wiki.git",
        "dotnet run --project eng/Electron2D.Build -- update api-manifest --wiki-path .github/wiki --check",
        "dotnet run --project eng/Electron2D.Build -- update wiki --output .github/wiki --check",
        "dotnet run --project eng/Electron2D.Build -- verify api-compatibility --wiki-path .github/wiki",
        "dotnet run --project eng/Electron2D.Build -- verify ui-public-api-gate --wiki-path .github/wiki",
        "dotnet run --project eng/Electron2D.Build -- verify public-api-documentation --wiki-path .github/wiki",
        "if: matrix.os == 'windows-latest'",
        "dotnet run --project eng/Electron2D.Build -- package --rid win-x64",
        "if: matrix.os == 'ubuntu-latest'",
        "dotnet run --project eng/Electron2D.Build -- package --rid linux-x64",
        "if: matrix.os == 'macos-latest'",
        "dotnet run --project eng/Electron2D.Build -- package --rid osx-arm64",
        "dotnet run --project eng/Electron2D.Build -- verify performance-budgets",
        "dotnet run --project eng/Electron2D.Build -- verify performance",
        "mobile-export-status",
        "Android/iOS/mobile export"
    ];

    private static readonly string[] ForbiddenFragments =
    [
        "-IncludeBaseline",
        "tools/Run-Tests.ps1",
        "tools\\Run-Tests.ps1",
        "tools/Verify-PerformanceBudgets.ps1",
        "tools\\Verify-PerformanceBudgets.ps1",
        "Verify-Box2DPhysicsCandidate.ps1",
        "Verify-PublicApiXmlDocs.ps1",
        "Verify-PublicApiDocumentationAudit.ps1",
        "Verify-CanonicalGoalAlignment.ps1",
        "Verify-LocalDocumentation.ps1",
        "Update-ApiWiki.ps1",
        "Update-ApiManifest.ps1",
        "Verify-WindowsExport.ps1",
        "Verify-LinuxExport.ps1",
        "Verify-MacOSExport.ps1"
    ];

    public int Verify()
    {
        var workflowPath = Path.Combine(repositoryRoot, ".github", "workflows", "ci.yml");
        if (!File.Exists(workflowPath))
        {
            diagnostics.Write(Error("E2D-BUILD-CI-MATRIX-WORKFLOW-MISSING", "CI workflow .github/workflows/ci.yml was not found.", ".github/workflows/ci.yml"));
            return RepositoryBuildExitCodes.Failed;
        }

        var workflow = File.ReadAllText(workflowPath, Encoding.UTF8);
        var errors = new List<BuildDiagnostic>();
        foreach (var fragment in RequiredFragments)
        {
            if (!workflow.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error(
                    "E2D-BUILD-CI-MATRIX-FRAGMENT-MISSING",
                    $"CI workflow is missing required fragment: {fragment}",
                    ".github/workflows/ci.yml"));
            }
        }

        foreach (var fragment in ForbiddenFragments)
        {
            if (workflow.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error(
                    "E2D-BUILD-CI-MATRIX-FORBIDDEN-FRAGMENT",
                    $"CI workflow contains forbidden fragment: {fragment}",
                    ".github/workflows/ci.yml"));
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            "verify ci-matrix",
            "info",
            "E2D-BUILD-CI-MATRIX-PASSED",
            "CI workflow matrix, local verification routes, export gates, API documentation gates and performance gates are valid.",
            Path: ".github/workflows/ci.yml"));
        return RepositoryBuildExitCodes.Success;
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify ci-matrix", "error", code, message, Path: path);
    }
}

internal sealed class NoPowerShellWorkflowVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string VerifyStep = "verify no-powershell-workflows";
    private const string AllowedMentionsStep = "verify no-powershell-workflows allowed-mentions";
    private static readonly NoPowerShellWorkflowPathRule[] ProductionPathAllowlist =
    [
        new("GitHub workflow definitions", relativePath =>
            relativePath.StartsWith(".github/workflows/", StringComparison.Ordinal) &&
            (relativePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) || relativePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))),
        new("root agent and task instructions", relativePath => relativePath is "AGENTS.md" or "TASKS.md"),
        new("local Codex skill documentation", relativePath =>
            relativePath.StartsWith(".codex/skills/", StringComparison.Ordinal) &&
            relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase)),
        new("generated documentation metadata", relativePath => relativePath.StartsWith("data/documentation/", StringComparison.Ordinal)),
        new("quality data", relativePath => relativePath.StartsWith("data/quality/", StringComparison.Ordinal)),
        new("reference game asset documentation", relativePath => relativePath.Equals("data/assets/reference-games/README.md", StringComparison.Ordinal)),
        new("legacy root tools path", relativePath => relativePath.StartsWith("tools/", StringComparison.Ordinal)),
        new("active domain documentation", relativePath =>
            relativePath.StartsWith("docs/", StringComparison.Ordinal) &&
            relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) &&
            IsActiveDocumentationPath(relativePath))
    ];

    private static readonly NoPowerShellAllowedMentionRule[] AllowedMentionRules =
    [
        new(
            "Spec-Kit installation exact note",
            ".codex/skills/spec-driven-development/references/sdd_install.md",
            [
                "7fd6ba565287008cc1ce7ef651c2ee1620669bb4b699b713b818664b250aad91",
                "edc678d7a18155801ea355c083f424592c5b28aa093b3cca2520f18b76b33dac"
            ]),
        new(
            "CI command exact route",
            ".github/workflows/ci.yml",
            [
                "402ad514e068f4c18feebf6681bfbe38efc3d1340c23dfb7bb1a71c7246d6e0e"
            ]),
        new(
            "README migration exact line",
            "docs/documentation/repository-readme.md",
            [
                "6f9b0668eacb6f11c73f86ef1ff679b147fb974599b80189b12ea0f55615ebab",
                "9c7b2092fcbb18d3f709faf107d014b947bed3e701dad27b4041b5c618995a45",
                "b1c6d4456902e9eb56e9ffec33972b3f6b8d6b928044fc93902a2318f4b499bd",
                "2fedd1f950171abcc1c4d0c67ed846891ec96852b55e67fceb42ddddcec50a0e",
                "204566306b705e6ee361061080e60e458f90a77937743164bf9ad4df87f4b037",
                "7ee4cbff2199589f37f47d536ff697d8c620a360c1e6ccf9adbc8f6412704857",
                "e3c694c0bb0609ecdbe09bb2e8f9acdd5f360b5fc9518d660beee51c460937bb"
            ]),
        new(
            "export package policy exact line",
            "docs/export/export-guide.md",
            [
                "223185a731ab3ba0d6eef62d7e53d7e11399f907216acf734dec214a3d3f90c2"
            ]),
        new(
            "export package policy exact line",
            "docs/export/linux-x64-export.md",
            [
                "acf4b447e6c3f0dffdbb74d5e25a0f40fd6e8ed961915f10ac122209feed3464",
                "d3373566960caa317001bfa3f2da86d217ce6f7fd9e50fb1e6f78693563af830"
            ]),
        new(
            "export package policy exact line",
            "docs/export/macos-arm64-export.md",
            [
                "44b21a877d0e2a8e5f63b684fd88f98590910bb36105c06daed2b2be26a4f0d4",
                "d3373566960caa317001bfa3f2da86d217ce6f7fd9e50fb1e6f78693563af830"
            ]),
        new(
            "export package policy exact line",
            "docs/export/windows-x64-export.md",
            [
                "6339abc7eb890321cc9337f32e005a93f3edc2ecefc62d5500a9eca379602e55",
                "3362bbd5dc47651451dd5aec7ad8344e070e0741d63c56d84dfa6e7bb955643f"
            ]),
        new(
            "release-management exact line",
            "docs/release-management/audit-package.md",
            [
                "635a7a84eed7c51aebddeb41e5dbf2e888971a212ffb4a80a508a926a3541fce"
            ]),
        new(
            "release-management exact line",
            "docs/release-management/ci-matrix.md",
            [
                "654f642f5abe50f826c77d89a209300d0403336102b60c430006fbab453f39d5",
                "8523fb343e151aaa4f235b6908975af51edb30a45e1629d4ef5a77013c439621",
                "793c646debfddebd8a7f820eadd1ae59086f1a0474ccde04c8321055786fe8a2",
                "6a6df5d844c94cba4fed4830baed051e34a19e58f306f703ca6fa27301854fdc",
                "caca31f86a73645dcad500da9d753edd3980bedf4023203bf82b1de95529868e",
                "ace61f82fc467b65334b1bcddb68a68510b0766275a4c4c8e117727b90a64174",
                "a5a9a034a81356535351b67765625f9632033d73f10506a682d73ccbf020a412",
                "723b5a4609acb26050b8c4810acc5c081e6fc6944bcdddca9108c41392b5de0f",
                "aeecb6127dce7edc189e189232f9ae0cd4595500be46e5143eeccfe7c9251324",
                "6a6df5d844c94cba4fed4830baed051e34a19e58f306f703ca6fa27301854fdc",
                "ba0e1e4091bf718086cfdee3c84ef73af9538d41d9f9bd14cc5932a30679d2d1",
                "edc532052907c4de9e7000e3e977656b6772702a8ac2cd4c7b6c73003659656e",
                "ac717611b5c2de31499df4efc25c9036717a3b58329f710456d26ec9f9344fbf",
                "b6ad253421701556b3185b4b7a83149123f0ce2c343e24da22ce6070e380edfe"
            ]),
        new(
            "release-management exact line",
            "docs/release-management/release-packaging.md",
            [
                "b1f6c183538f497c3aa4936c24f90dd5342cda43e390c69f79a05e14870d053e",
                "5f877cf0eb096f37d58efd303a4b6e6f8e64ba796656e5146394ae0928a2d24a",
                "411fa4a68d4adace4cb1f253d1e8e9530341fc49c879b9ddf0238dface88a7dc",
                "75a2c024f0e42fcdbffff0eaa2279d3f5f8c37dc4bc5ea1bb2415e020bf7fcac",
                "cd506d236d8bdfce68fd3baa67bc4b3f53eafd625ebdd3967ccb0446a84ea77f",
                "6a6df5d844c94cba4fed4830baed051e34a19e58f306f703ca6fa27301854fdc",
                "7b76d2fc6fbc866b6d75357d5304b272a6d7a402eb8207b2a982453c1c2a07b4",
                "6d274cc035849f431d0c25791f0f6c26e6931114b7176f7950635efa2d0660b9",
                "f978100964396068d9a53fcc1e1d5f3d74f384c38f34c71912a695cc7b545eea",
                "c526c7397e048af8744f80bb72c90056fd65b055e59642619aebb3b2c14fea8d"
            ]),
        new(
            "repository license policy exact line",
            "docs/repository/license-policy.md",
            [
                "709b7765c44c3ba9188fd326d887e9ce00d50f9ec5dbb23818a2e0ebb2cb5d24"
            ]),
        new(
            "task history exact line",
            "TASKS.md",
            [
                "0dafb838db411a69149e66b27a9de4f553fc032b48bad07cd9e19edacd26245d",
                "8bf15528db996db31ac8a5b0eae7fe3329406569075c5ddcd1a526eaf5276605",
                "39c657e88edefd9a3bb015980006c91adbf7e639d65ade88c36c856c83e82af2",
                "11c16e746226d89ad569f200049538cbef8e16bd520ed5e33c514120c56a8edf",
                "ea01ffeed61aa7265db1dbf15ed2496a09842de8be7438793e8b422d9075d284",
                "21d94633803262124c13ba8b309938d3ef01a717b50bc67c655e01e6834a2b82",
                "733f44bf76ae825b196d24daab35effa8e06be796b42c30a05953b5e916d8ecc",
                "3845473b4de85b4f2a79562ffd165694c1680a5e21174ce8125b73b50b4f1f8f",
                "f25ab06ca87d6e7c76048e5d0901ec960faf3026d1057c1442840ca9b8ab7f17",
                "2f57f477a1b8acd13f5f310f86792b12156391f4f268ea939c0951ed60c74766",
                "4fd7d35931680c3b58e02bbf088083e18891449c8856a1253a58d9b254f137d7"
            ])
    ];

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var scan = await ScanAsync(VerifyStep, cancellationToken).ConfigureAwait(false);
        if (scan.EnumerationFailed)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        foreach (var error in scan.ActiveFindings)
        {
            diagnostics.Write(error);
        }

        if (scan.ActiveFindings.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            VerifyStep,
            "info",
            "E2D-BUILD-NO-POWERSHELL-WORKFLOWS-PASSED",
            $"No active PowerShell workflow was found in {scan.Paths.Count} tracked production paths."));
        return RepositoryBuildExitCodes.Success;
    }

    public async Task<int> ReportAllowedMentionsAsync(CancellationToken cancellationToken)
    {
        var scan = await ScanAsync(AllowedMentionsStep, cancellationToken).ConfigureAwait(false);
        if (scan.EnumerationFailed)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        foreach (var mention in scan.AllowedMentions)
        {
            diagnostics.Write(mention);
        }

        foreach (var error in scan.ActiveFindings)
        {
            diagnostics.Write(error);
        }

        if (scan.ActiveFindings.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic(
            "verify",
            AllowedMentionsStep,
            "info",
            "E2D-BUILD-NO-POWERSHELL-ALLOWED-MENTIONS-REPORTED",
            $"Reported {scan.AllowedMentions.Count} allowlisted PowerShell, pwsh or .ps1 mentions in {scan.Paths.Count} tracked production paths."));
        return RepositoryBuildExitCodes.Success;
    }

    private async Task<NoPowerShellWorkflowScanResult> ScanAsync(string step, CancellationToken cancellationToken)
    {
        var activeFindings = new List<BuildDiagnostic>();
        var allowedMentions = new List<BuildDiagnostic>();
        var paths = await EnumerateCandidatePathsAsync(step, cancellationToken).ConfigureAwait(false);
        if (paths is null)
        {
            return new NoPowerShellWorkflowScanResult([], [], [], EnumerationFailed: true);
        }

        foreach (var relativePath in paths.Order(StringComparer.Ordinal))
        {
            var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                continue;
            }

            if (IsToolsPowerShellScript(relativePath))
            {
                activeFindings.Add(Error(
                    step,
                    "E2D-BUILD-NO-POWERSHELL-TOOLS-PS1",
                    $"Tracked active PowerShell script must be deleted or replaced by an eng/Electron2D.Build route: {relativePath}.",
                    relativePath));
                continue;
            }

            if (!IsTextFile(relativePath))
            {
                continue;
            }

            var lines = File.ReadAllLines(fullPath, Encoding.UTF8);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (!ContainsPowerShellWorkflowToken(line))
                {
                    continue;
                }

                var lineNumber = i + 1;
                var allowed = FindAllowlistedMentionRule(relativePath, line);
                if (allowed is not null)
                {
                    allowedMentions.Add(new BuildDiagnostic(
                        "verify",
                        step,
                        "info",
                        "E2D-BUILD-NO-POWERSHELL-ALLOWED-MENTION",
                        $"Allowed {allowed.Description} mention in {relativePath}:{lineNumber}: {line.Trim()}",
                        Path: relativePath,
                        LineNumber: lineNumber));
                    continue;
                }

                activeFindings.Add(Error(
                    step,
                    "E2D-BUILD-NO-POWERSHELL-ACTIVE-WORKFLOW",
                    $"Active PowerShell workflow token found in {relativePath}:{lineNumber}: {line.Trim()}",
                    relativePath,
                    lineNumber));
            }
        }

        return new NoPowerShellWorkflowScanResult(paths, activeFindings, allowedMentions, EnumerationFailed: false);
    }

    private async Task<IReadOnlyList<string>?> EnumerateCandidatePathsAsync(string step, CancellationToken cancellationToken)
    {
        var paths = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(Path.Combine(repositoryRoot, ".git")))
        {
            var result = await processRunner.RunAsync(
                new ProcessRunRequest(
                    step + " git ls-files",
                    "git",
                    ["ls-files", "-z"],
                    repositoryRoot,
                    TimeSpan.FromSeconds(30)),
                cancellationToken).ConfigureAwait(false);
            if (result.ExitCode != 0)
            {
                diagnostics.Write(Error(
                    step,
                    "E2D-BUILD-NO-POWERSHELL-GIT-LS-FILES",
                    $"Failed to enumerate tracked files with git ls-files. Exit code: {result.ExitCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unknown"}."));
                return null;
            }

            foreach (var path in result.StandardOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var normalized = path.Replace('\\', '/');
                if (IsCandidatePath(normalized))
                {
                    paths.Add(normalized);
                }
            }

            return paths.ToArray();
        }

        foreach (var path in Directory.EnumerateFiles(repositoryRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
            if (IsCandidatePath(relativePath))
            {
                paths.Add(relativePath);
            }
        }

        return paths.ToArray();
    }

    private static bool IsCandidatePath(string relativePath)
    {
        if (relativePath.Split('/').Any(part => part is ".git" or "bin" or "obj" or ".temp" or "artifacts" or "TestResults"))
        {
            return false;
        }

        return ProductionPathAllowlist.Any(rule => rule.Matches(relativePath));
    }

    private static bool IsActiveDocumentationPath(string relativePath)
    {
        if (relativePath.StartsWith("docs/verdicts/", StringComparison.Ordinal))
        {
            return false;
        }

        return relativePath.StartsWith("docs/release-management/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/repository/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/documentation/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/export/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/architecture/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/examples/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/quality/", StringComparison.Ordinal) ||
            relativePath.StartsWith("docs/testing/", StringComparison.Ordinal);
    }

    private static bool IsToolsPowerShellScript(string relativePath)
    {
        return relativePath.StartsWith("tools/", StringComparison.Ordinal) &&
            relativePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTextFile(string relativePath)
    {
        return relativePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
            relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
            relativePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase) ||
            relativePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) ||
            relativePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
            relativePath.Equals("AGENTS.md", StringComparison.Ordinal) ||
            relativePath.Equals("TASKS.md", StringComparison.Ordinal);
    }

    private static bool ContainsPowerShellWorkflowToken(string line)
    {
        return line.Contains("pwsh", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("PowerShell", StringComparison.OrdinalIgnoreCase) ||
            line.Contains(".ps1", StringComparison.OrdinalIgnoreCase) ||
            Regex.IsMatch(line, @"tools[/\\][^\s`""]+\.ps1", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static NoPowerShellAllowedMentionRule? FindAllowlistedMentionRule(string relativePath, string line)
    {
        var lineHash = ComputeNormalizedLineSha256(line);
        return AllowedMentionRules.FirstOrDefault(rule => rule.Matches(relativePath, lineHash));
    }

    private static string ComputeNormalizedLineSha256(string line)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(line.Trim()))).ToLowerInvariant();
    }

    private static BuildDiagnostic Error(string step, string code, string message, string? path = null, int? lineNumber = null)
    {
        return new BuildDiagnostic("verify", step, "error", code, message, Path: path, LineNumber: lineNumber);
    }

    private sealed record NoPowerShellWorkflowPathRule(string Description, Func<string, bool> Matches);

    private sealed record NoPowerShellAllowedMentionRule(string Description, string RelativePath, string[] NormalizedLineSha256)
    {
        public bool Matches(string relativePath, string normalizedLineSha256)
        {
            return relativePath.Equals(RelativePath, StringComparison.Ordinal) &&
                NormalizedLineSha256.Contains(normalizedLineSha256, StringComparer.Ordinal);
        }
    }

    private sealed record NoPowerShellWorkflowScanResult(
        IReadOnlyList<string> Paths,
        IReadOnlyList<BuildDiagnostic> ActiveFindings,
        IReadOnlyList<BuildDiagnostic> AllowedMentions,
        bool EnumerationFailed);
}

internal sealed class SourceDomainLayoutVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private static readonly HashSet<string> AllowedCoreDomains = new(StringComparer.Ordinal)
    {
        "Collections",
        "Identity",
        "Math",
        "ObjectModel",
        "Random",
        "SceneTree",
        "Variant"
    };

    private static readonly HashSet<string> RequiredRootDomains = new(StringComparer.Ordinal)
    {
        "Assets",
        "Core",
        "Export",
        "Graphics",
        "Physics",
        "Runtime"
    };

    private static readonly HashSet<string> IgnoredRootDirectories = new(StringComparer.Ordinal)
    {
        ".temp",
        "bin",
        "obj",
        "Properties"
    };

    private static readonly IReadOnlyDictionary<string, string[]> RequiredNestedDomains = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["Assets"] = ["Resources"],
        ["Graphics"] = ["Display", "Rendering", "Text", "UI"],
        ["Runtime"] = ["Application", "Animation", "Audio", "Input", "Localization", "Scripting", "Settings"]
    };

    public int Verify()
    {
        var errors = new List<BuildDiagnostic>();
        var sourceRoot = Path.Combine(repositoryRoot, "src", "Electron2D");
        var coreRoot = Path.Combine(sourceRoot, "Core");
        if (!Directory.Exists(sourceRoot))
        {
            errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-ROOT-MISSING", "Runtime source root is missing.", "src/Electron2D"));
        }
        else if (!Directory.Exists(coreRoot))
        {
            errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-CORE-MISSING", "Core source domain is missing.", "src/Electron2D/Core"));
        }
        else
        {
            foreach (var directory in Directory.EnumerateDirectories(coreRoot))
            {
                var name = Path.GetFileName(directory);
                if (!AllowedCoreDomains.Contains(name))
                {
                    errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-CORE-UNEXPECTED", $"Non-core source domain is not allowed under Core: {name}.", ToRepositoryPath(directory)));
                }
            }

            foreach (var domain in AllowedCoreDomains)
            {
                var requiredPath = Path.Combine(coreRoot, domain);
                if (!Directory.Exists(requiredPath))
                {
                    errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-CORE-MISSING", $"Required core source domain is missing: {domain}.", ToRepositoryPath(requiredPath)));
                }
            }

            foreach (var directory in Directory.EnumerateDirectories(sourceRoot))
            {
                var name = Path.GetFileName(directory);
                if (IgnoredRootDirectories.Contains(name))
                {
                    continue;
                }

                if (!RequiredRootDomains.Contains(name))
                {
                    errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-UNEXPECTED", $"Unexpected runtime source domain: {name}.", ToRepositoryPath(directory)));
                }
            }

            foreach (var domain in RequiredRootDomains)
            {
                var requiredPath = Path.Combine(sourceRoot, domain);
                if (!Directory.Exists(requiredPath))
                {
                    errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-MISSING", $"Required root source domain is missing: {domain}.", ToRepositoryPath(requiredPath)));
                }
            }

            foreach (var (rootDomain, nestedDomains) in RequiredNestedDomains)
            {
                foreach (var nestedDomain in nestedDomains)
                {
                    var requiredPath = Path.Combine(sourceRoot, rootDomain, nestedDomain);
                    if (!Directory.Exists(requiredPath))
                    {
                        errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-NESTED-MISSING", $"Required nested source domain is missing: {rootDomain}/{nestedDomain}.", ToRepositoryPath(requiredPath)));
                    }
                }
            }

            var presetPath = Path.Combine(sourceRoot, "Export", "Presets");
            if (!Directory.Exists(presetPath))
            {
                errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-EXPORT-PRESETS", "Export presets must live under src/Electron2D/Export/Presets.", "src/Electron2D/Export/Presets"));
            }

            foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).Where(IsRuntimeSourceFile))
            {
                var text = File.ReadAllText(sourcePath, Encoding.UTF8);
                var match = Regex.Match(text, @"^\s*namespace\s+([A-Za-z_][A-Za-z0-9_.]*)\s*[;{]", RegexOptions.CultureInvariant | RegexOptions.Multiline);
                if (match.Success && match.Groups[1].Value is not ("Electron2D" or "Electron2D.Collections"))
                {
                    errors.Add(Error("E2D-BUILD-SOURCE-DOMAIN-NAMESPACE", $"Runtime source file uses an unsupported namespace: {ToRepositoryPath(sourcePath)}.", ToRepositoryPath(sourcePath)));
                }
            }
        }

        return Complete("verify source-domain-layout", "E2D-BUILD-SOURCE-DOMAIN-LAYOUT-PASSED", "Source domain layout verification passed.", errors);
    }

    private int Complete(string step, string successCode, string successMessage, List<BuildDiagnostic> errors)
    {
        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic("verify", step, "info", successCode, successMessage));
        return RepositoryBuildExitCodes.Success;
    }

    private BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify source-domain-layout", "error", code, message, Path: path);
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static bool IsRuntimeSourceFile(string path)
    {
        var segments = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return !segments.Any(segment => segment is ".temp" or "bin" or "obj");
    }
}

internal sealed class StaticRepositoryVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    public int VerifyUserDocumentation()
    {
        return VerifyRequiredFragments(
            "verify user-documentation",
            "E2D-BUILD-USER-DOCUMENTATION-PASSED",
            "User documentation verification passed.",
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["docs/documentation/user-guide.md"] = ["verify user-documentation", "Первый проект", "Desktop export baseline"],
                ["docs/documentation/renderer-profiles.md"] = ["verify user-documentation", "renderer profile"],
                ["docs/documentation/troubleshooting-release-checklist.md"] = ["verify user-documentation", "release checklist"]
            },
            forbidden: ["tools\\Verify-UserDocumentation.ps1", "tools/Verify-UserDocumentation.ps1", "powershell -ExecutionPolicy"]);
    }

    public int VerifyCanonicalGoalAlignment()
    {
        return VerifyRequiredFragments(
            "verify canonical-goal-alignment",
            "E2D-BUILD-CANONICAL-GOAL-ALIGNMENT-PASSED",
            "Canonical goal alignment verification passed.",
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["docs/releases/0.1.0-preview.md"] = ["Agent-native cross-platform 2D game engine", "0.1.0 Preview"],
                ["docs/architecture/agent-native-workflow.md"] = ["Agent-native", "ProjectTaskManager"],
                ["docs/architecture/engine-platform-stack.md"] = ["verify canonical-goal-alignment", "C#"]
            },
            forbidden: ["tools\\Verify-CanonicalGoalAlignment.ps1", "tools/Verify-CanonicalGoalAlignment.ps1"]);
    }

    public int VerifyExportDocumentation()
    {
        return VerifyRequiredFragments(
            "verify export-documentation",
            "E2D-BUILD-EXPORT-DOCUMENTATION-PASSED",
            "Export documentation verification passed.",
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["docs/export/export-guide.md"] = ["package --rid", "AndroidArm64", "WebAssemblyBrowser"],
                ["docs/export/windows-x64-export.md"] = ["package --rid win-x64", "без PowerShell"],
                ["docs/export/linux-x64-export.md"] = ["package --rid linux-x64", "без PowerShell"],
                ["docs/export/macos-arm64-export.md"] = ["package --rid osx-arm64", "без PowerShell"],
                ["docs/documentation/user-guide.md"] = ["verify export-documentation", "Desktop export baseline"]
            },
            forbidden: ["tools\\Verify-ExportDocumentation.ps1", "tools/Verify-ExportDocumentation.ps1", "powershell -ExecutionPolicy"]);
    }

    public int VerifyPlatformer()
    {
        var errors = new List<BuildDiagnostic>();
        var projectRoot = ResolveRepositoryPath(Path.Combine("examples", "platformer"));
        var projectPath = Path.Combine(projectRoot, "Platformer.csproj");
        var cliProjectPath = Path.Combine("src", "Electron2D.Cli", "Electron2D.Cli.csproj");
        var workRoot = Path.Combine(repositoryRoot, ".temp", "platformer");
        var progressPath = Path.Combine(workRoot, "progress.json");
        var webOutput = Path.Combine(workRoot, "web");

        foreach (var relativePath in new[]
        {
            "Platformer.csproj",
            "scripts/PlatformerGame.cs",
            "Platformer.e2d",
            "global.json",
            "scenes/main.scene.json",
            "resources/platformer.manifest.json",
            ".electron2d/tasks/board.e2tasks",
            ".electron2d/tasks/platformer-acceptance.e2task",
            ".electron2d/tasks/T-0166.e2task",
            ".electron2d/tasks/T-0221.e2task",
            ".electron2d/tasks/T-0222.e2task",
            ".electron2d/tasks/T-0223.e2task",
            ".electron2d/tasks/T-0225.e2task"
        })
        {
            if (!File.Exists(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-FILE-MISSING", $"Platformer required file was not found: {relativePath}.", "examples/platformer/" + relativePath));
            }
        }

        foreach (var forbiddenPath in new[] { "TASKS.md", "dev-diary", "completed-tasks" })
        {
            if (File.Exists(Path.Combine(projectRoot, forbiddenPath)) || Directory.Exists(Path.Combine(projectRoot, forbiddenPath)))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-WORKFLOW-PATH", $"Platformer must not contain repository workflow path: {forbiddenPath}.", "examples/platformer/" + forbiddenPath));
            }
        }

        VerifyPlatformerSettings(projectRoot, errors);
        VerifyPlatformerTasks(projectRoot, errors);
        VerifyPlatformerResources(projectRoot, errors);
        VerifyPlatformerScriptSurface(projectRoot, errors);
        if (errors.Count > 0)
        {
            return Complete("verify platformer", "E2D-BUILD-PLATFORMER-PASSED", "Platformer project verification passed.", errors);
        }

        if (Directory.Exists(workRoot))
        {
            Directory.Delete(workRoot, recursive: true);
        }

        Directory.CreateDirectory(workRoot);
        var build = RunProcess("dotnet", ["build", projectPath], repositoryRoot, environment: null, timeout: TimeSpan.FromMinutes(5));
        if (build.ExitCode != 0)
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-BUILD-FAILED", "Platformer project build failed.", "examples/platformer/Platformer.csproj"));
            return Complete("verify platformer", "E2D-BUILD-PLATFORMER-PASSED", "Platformer project verification passed.", errors);
        }

        var runEnvironment = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ELECTRON2D_PLATFORMER_SAVE"] = progressPath
        };
        var run = RunProcess("dotnet", ["run", "--project", cliProjectPath, "--", "run", "--project", projectRoot, "--play-script", "right,right,save,quit"], repositoryRoot, runEnvironment, TimeSpan.FromMinutes(5));
        if (run.ExitCode != 0 || !ContainsAll(run.StandardOutput, ["Mode=playable", "Playable=True", "CommandsApplied=4", "Checkpoint=checkpoint-01", "Coins=1", "WindowCreated=True", "WindowShown=True", "FramePresented=True"]) || !File.Exists(progressPath))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RUN-FAILED", "Platformer playable CLI run did not produce the expected output and save artifact.", "examples/platformer"));
        }

        var playableSavePath = Path.Combine(workRoot, "playable-progress.json");
        var playableScreenshotPath = Path.Combine(workRoot, "platformer-playable.png");
        runEnvironment["ELECTRON2D_PLATFORMER_SAVE"] = playableSavePath;
        var playable = RunProcess("dotnet", ["run", "--project", cliProjectPath, "--", "run", "--project", projectRoot, "--play-script", "right,jump,right,pause,save,quit", "--screenshot", playableScreenshotPath], repositoryRoot, runEnvironment, TimeSpan.FromMinutes(5));
        if (playable.ExitCode != 0 ||
            !ContainsAll(playable.StandardOutput, ["Mode=playable", "Playable=True", "FramesAdvanced=5", "CommandsApplied=6", "Checkpoint=checkpoint-01", "Coins=1", "Paused=True", "WindowCreated=True", "WindowShown=True", "FramePresented=True", "InputEventsDispatched=", "DrawCommands=", "ScreenshotPath="]) ||
            playable.StandardOutput.Contains("FRAME ", StringComparison.Ordinal) ||
            !File.Exists(playableSavePath) ||
            !File.Exists(playableScreenshotPath) ||
            !TryAssertPngMinDimensions(playableScreenshotPath, 640, 360))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-PLAYABLE-FAILED", "Platformer playable mode did not produce expected output, save artifact and screenshot.", "examples/platformer"));
        }

        var drawCommands = Regex.Match(playable.StandardOutput, "DrawCommands=(\\d+)");
        if (!drawCommands.Success || int.Parse(drawCommands.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture) <= 0)
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-DRAW-COMMANDS", "Platformer playable output must report DrawCommands greater than zero.", "examples/platformer"));
        }

        var validate = RunProcess("dotnet", ["run", "--project", cliProjectPath, "--", "validate", "--project", projectRoot, "--format", "json"], repositoryRoot, environment: null, timeout: TimeSpan.FromMinutes(5));
        if (validate.ExitCode != 0 || !TryValidatePlatformerJson(validate.StandardOutput))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-VALIDATE-FAILED", "Platformer e2d validate route did not succeed.", "examples/platformer"));
        }

        var webBuild = RunProcess("dotnet", ["run", "--project", cliProjectPath, "--", "export", "build-web", "--project", projectRoot, "--output", webOutput, "--skip-publish", "true", "--format", "json"], repositoryRoot, environment: null, timeout: TimeSpan.FromMinutes(5));
        var webRoot = Path.Combine(webOutput, "wwwroot");
        if (webBuild.ExitCode != 0 || !Directory.Exists(webRoot))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-WEB-EXPORT-FAILED", "Platformer WebAssembly package did not create wwwroot.", "examples/platformer"));
        }
        else if (Directory.EnumerateFiles(webRoot, "*", SearchOption.AllDirectories).Any(path => path.Replace('\\', '/').Contains("/.electron2d/tasks/", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-EDITOR-METADATA-PACKAGED", "Platformer WebAssembly package contains Editor task metadata.", ".temp/platformer/web"));
        }

        return Complete("verify platformer", "E2D-BUILD-PLATFORMER-PASSED", "Platformer project verification passed.", errors);
    }

    public int VerifyAgentAcceptanceBenchmarks(string[] args)
    {
        var parse = ParseAgentBenchmarkArguments(args);
        if (!parse.Succeeded)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify agent-acceptance-benchmarks", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", parse.ErrorMessage));
            return RepositoryBuildExitCodes.Failed;
        }

        var manifestPath = Path.Combine(repositoryRoot, "data", "quality", "agent-acceptance-benchmarks.json");
        var errors = new List<BuildDiagnostic>();
        var manifest = ReadJsonObject(manifestPath, "verify agent-acceptance-benchmarks", errors);
        if (manifest is not null)
        {
            if (!TryGetString(manifest.Value, "format", out var format) || format != "Electron2D.AgentAcceptanceBenchmarkManifest")
            {
                errors.Add(Error("verify agent-acceptance-benchmarks", "E2D-BUILD-AGENT-BENCHMARK-FORMAT", "Agent acceptance benchmark manifest has an invalid format.", "data/quality/agent-acceptance-benchmarks.json"));
            }

            if (!manifest.Value.TryGetProperty("suites", out var suites) || suites.ValueKind != JsonValueKind.Array)
            {
                errors.Add(Error("verify agent-acceptance-benchmarks", "E2D-BUILD-AGENT-BENCHMARK-SUITES", "Agent acceptance benchmark manifest must contain suites array.", "data/quality/agent-acceptance-benchmarks.json"));
            }
            else
            {
                AssertAgentManifestReferences(manifest.Value, errors);
            }
        }

        if (errors.Count > 0 || manifest is null)
        {
            return Complete("verify agent-acceptance-benchmarks", "E2D-BUILD-AGENT-BENCHMARK-PASSED", "Agent acceptance benchmark manifest verification passed.", errors);
        }

        var selectedSuites = SelectAgentSuites(manifest.Value, parse, errors);
        if (errors.Count > 0)
        {
            return Complete("verify agent-acceptance-benchmarks", "E2D-BUILD-AGENT-BENCHMARK-PASSED", "Agent acceptance benchmark manifest verification passed.", errors);
        }

        if (parse.List)
        {
            foreach (var suite in selectedSuites)
            {
                Console.WriteLine($"{suite.GetProperty("id").GetString()} [{suite.GetProperty("mode").GetString()}] target={suite.GetProperty("targetSuccessRatio").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture)}");
                foreach (var scenario in suite.GetProperty("scenarios").EnumerateArray())
                {
                    Console.WriteLine("  - " + scenario.GetProperty("id").GetString());
                }
            }

            diagnostics.Write(new BuildDiagnostic("verify", "verify agent-acceptance-benchmarks", "info", "E2D-BUILD-AGENT-BENCHMARK-LISTED", "Agent acceptance benchmark suites were listed."));
            return RepositoryBuildExitCodes.Success;
        }

        var outputDirectory = parse.OutputDirectory ?? Path.Combine(".temp", "agent-acceptance-benchmarks");
        var fullOutputDirectory = ResolveRepositoryPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);
        var planPath = Path.Combine(fullOutputDirectory, "benchmark-plan.json");
        WriteJsonFile(CreateAgentPlan(manifest.Value, selectedSuites, parse.DryRun), planPath);

        if (parse.DryRun)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify agent-acceptance-benchmarks", "info", "E2D-BUILD-AGENT-BENCHMARK-DRY-RUN-PASSED", "Agent acceptance benchmark dry run passed.", OutputPath: outputDirectory.Replace('\\', '/')));
            return RepositoryBuildExitCodes.Success;
        }

        var logsDirectory = Path.Combine(fullOutputDirectory, "logs");
        var artifactsDirectory = Path.Combine(fullOutputDirectory, "artifacts");
        Directory.CreateDirectory(logsDirectory);
        Directory.CreateDirectory(artifactsDirectory);
        var suiteResults = new List<object>();
        var overallSucceeded = true;
        foreach (var suite in selectedSuites)
        {
            var evidenceResults = new List<AgentEvidenceResult>();
            foreach (var evidence in suite.GetProperty("evidence").EnumerateArray())
            {
                var result = InvokeAgentEvidence(evidence, logsDirectory);
                evidenceResults.Add(result);
                if (!result.Succeeded && !parse.ContinueOnFailure)
                {
                    break;
                }
            }

            var required = evidenceResults.Where(result => result.Required).ToArray();
            var passedRequired = required.Count(result => result.Succeeded);
            var ratio = required.Length == 0 ? 1.0 : (double)passedRequired / required.Length;
            var target = suite.GetProperty("targetSuccessRatio").GetDouble();
            var suiteSucceeded = ratio >= target;
            overallSucceeded &= suiteSucceeded;
            suiteResults.Add(new
            {
                id = suite.GetProperty("id").GetString(),
                mode = suite.GetProperty("mode").GetString(),
                releaseRequired = suite.GetProperty("releaseRequired").GetBoolean(),
                targetSuccessRatio = target,
                successRatio = ratio,
                succeeded = suiteSucceeded,
                evidence = evidenceResults.Select(result => new
                {
                    id = result.Id,
                    kind = result.Kind,
                    required = result.Required,
                    status = result.Status,
                    succeeded = result.Succeeded,
                    exitCode = result.ExitCode,
                    logPath = ToRepositoryPath(result.LogPath),
                    covers = result.Covers
                }).ToArray()
            });
        }

        var resultPath = Path.Combine(fullOutputDirectory, "benchmark-result.json");
        WriteJsonFile(new
        {
            format = "Electron2D.AgentAcceptanceBenchmarkResult",
            manifestVersion = manifest.Value.GetProperty("version").GetInt32(),
            release = manifest.Value.GetProperty("release").GetString(),
            generatedAtUtc = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            succeeded = overallSucceeded,
            planPath = ToRepositoryPath(planPath),
            artifactsDirectory = ToRepositoryPath(artifactsDirectory),
            suites = suiteResults
        }, resultPath);
        Console.WriteLine("Agent acceptance benchmark completed");
        Console.WriteLine("ResultPath=" + ToRepositoryPath(resultPath));

        if (!overallSucceeded)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify agent-acceptance-benchmarks", "error", "E2D-BUILD-AGENT-BENCHMARK-FAILED", "Agent acceptance benchmark failed.", OutputPath: ToRepositoryPath(resultPath)));
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic("verify", "verify agent-acceptance-benchmarks", "info", "E2D-BUILD-AGENT-BENCHMARK-PASSED", "Agent acceptance benchmark completed.", OutputPath: ToRepositoryPath(resultPath)));
        return RepositoryBuildExitCodes.Success;
    }

    private JsonElement[] SelectAgentSuites(JsonElement manifest, AgentBenchmarkArguments parse, List<BuildDiagnostic> errors)
    {
        var suites = manifest.GetProperty("suites").EnumerateArray().ToArray();
        if (parse.Suite is null)
        {
            return suites;
        }

        var selected = suites.Where(suite => suite.GetProperty("id").GetString() == parse.Suite).ToArray();
        if (selected.Length == 0)
        {
            errors.Add(Error("verify agent-acceptance-benchmarks", "E2D-BUILD-AGENT-BENCHMARK-SUITE", $"Unknown benchmark suite: {parse.Suite}.", "data/quality/agent-acceptance-benchmarks.json"));
        }

        return selected;
    }

    private void AssertAgentManifestReferences(JsonElement manifest, List<BuildDiagnostic> errors)
    {
        foreach (var suite in manifest.GetProperty("suites").EnumerateArray())
        {
            foreach (var path in GetStringArray(suite, "documentation"))
            {
                AssertRepositoryFile(path, "verify agent-acceptance-benchmarks", errors);
            }

            foreach (var evidence in suite.GetProperty("evidence").EnumerateArray())
            {
                foreach (var path in GetStringArray(evidence, "sourceFiles"))
                {
                    AssertRepositoryFile(path, "verify agent-acceptance-benchmarks", errors);
                }

                if (evidence.TryGetProperty("visualEvidence", out var visualEvidence) &&
                    visualEvidence.ValueKind == JsonValueKind.Object &&
                    visualEvidence.TryGetProperty("reference", out var reference) &&
                    reference.ValueKind == JsonValueKind.String)
                {
                    AssertRepositoryFile(reference.GetString() ?? string.Empty, "verify agent-acceptance-benchmarks", errors);
                }
            }

            if (suite.TryGetProperty("successConditions", out var successConditions) &&
                successConditions.ValueKind == JsonValueKind.Object &&
                successConditions.TryGetProperty("documentedManualHarness", out var manualHarness) &&
                manualHarness.ValueKind == JsonValueKind.String)
            {
                AssertRepositoryFile((manualHarness.GetString() ?? string.Empty).Split('#', 2)[0], "verify agent-acceptance-benchmarks", errors);
            }
        }
    }

    private void AssertRepositoryFile(string relativePath, string step, List<BuildDiagnostic> errors)
    {
        if (!TryResolveRepositoryFile(relativePath, out var fullPath) || !File.Exists(fullPath))
        {
            errors.Add(Error(step, "E2D-BUILD-STATIC-FILE-MISSING", $"Required file is missing: {relativePath}.", relativePath));
        }
    }

    private bool TryResolveRepositoryFile(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) ||
            Regex.IsMatch(relativePath, "^[a-zA-Z][a-zA-Z0-9+.-]*://", RegexOptions.CultureInvariant))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(repositoryRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private object CreateAgentPlan(JsonElement manifest, IReadOnlyList<JsonElement> suites, bool dryRun)
    {
        var suitePlans = suites.Select(suite => new
        {
            id = suite.GetProperty("id").GetString(),
            mode = suite.GetProperty("mode").GetString(),
            releaseRequired = suite.GetProperty("releaseRequired").GetBoolean(),
            targetSuccessRatio = suite.GetProperty("targetSuccessRatio").GetDouble(),
            scenarioCount = suite.GetProperty("scenarios").GetArrayLength(),
            evidence = suite.GetProperty("evidence").EnumerateArray().Select(evidence => new
            {
                id = evidence.GetProperty("id").GetString(),
                kind = evidence.GetProperty("kind").GetString(),
                required = evidence.GetProperty("required").GetBoolean(),
                covers = GetStringArray(evidence, "covers"),
                command = evidence.TryGetProperty("command", out var command) ? command.GetString() : null,
                arguments = GetStringArray(evidence, "arguments")
            }).ToArray()
        }).ToArray();
        var requiredEvidenceCount = suitePlans.Sum(suite => suite.evidence.Count(evidence => evidence.required));
        var visualEvidenceCount = suites.Sum(suite => suite.GetProperty("evidence").EnumerateArray().Count(evidence => evidence.TryGetProperty("visualEvidence", out _)));
        var headlessManualHarnessDocumented = suites.Any(suite =>
            suite.GetProperty("id").GetString() == "headless-ai" &&
            suite.TryGetProperty("successConditions", out var successConditions) &&
            successConditions.TryGetProperty("documentedManualHarness", out var documentedManualHarness) &&
            TryResolveRepositoryFile((documentedManualHarness.GetString() ?? string.Empty).Split('#', 2)[0], out var harnessPath) &&
            File.Exists(harnessPath));
        return new
        {
            format = "Electron2D.AgentAcceptanceBenchmarkPlan",
            manifestVersion = manifest.GetProperty("version").GetInt32(),
            release = manifest.GetProperty("release").GetString(),
            dryRun,
            generatedAtUtc = DateTimeOffset.UtcNow.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            suites = suitePlans,
            requiredEvidenceCount,
            visualEvidenceCount,
            headlessManualHarnessDocumented
        };
    }

    private AgentEvidenceResult InvokeAgentEvidence(JsonElement evidence, string logsDirectory)
    {
        var id = evidence.GetProperty("id").GetString() ?? "unknown";
        var kind = evidence.GetProperty("kind").GetString() ?? "unknown";
        var required = evidence.GetProperty("required").GetBoolean();
        var covers = GetStringArray(evidence, "covers");
        var logPath = Path.Combine(logsDirectory, id + ".log");
        if (kind == "documentedManualHarness")
        {
            File.WriteAllText(logPath, "Documented manual harness evidence accepted for this release gate step." + Environment.NewLine, Encoding.UTF8);
            return new AgentEvidenceResult(id, kind, required, "documented", Succeeded: true, ExitCode: 0, logPath, covers);
        }

        var command = evidence.GetProperty("command").GetString() ?? string.Empty;
        var arguments = GetStringArray(evidence, "arguments");
        var startedAt = DateTimeOffset.UtcNow;
        var result = RunProcess(command, arguments, repositoryRoot, environment: null, timeout: TimeSpan.FromMinutes(15));
        var completedAt = DateTimeOffset.UtcNow;
        var logLines = new List<string>
        {
            "id=" + id,
            "kind=" + kind,
            "command=" + command,
            "arguments=" + string.Join(' ', arguments),
            "startedAtUtc=" + startedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            "completedAtUtc=" + completedAt.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            "exitCode=" + result.ExitCode.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "timedOut=" + result.TimedOut.ToString(System.Globalization.CultureInfo.InvariantCulture),
            string.Empty,
            result.StandardOutput,
            result.StandardError
        };
        File.WriteAllLines(logPath, logLines, Encoding.UTF8);
        var succeeded = result.ExitCode == 0 && !result.TimedOut;
        return new AgentEvidenceResult(id, kind, required, succeeded ? "passed" : "failed", succeeded, result.ExitCode, logPath, covers);
    }

    private static void WriteJsonFile(object value, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine, Encoding.UTF8);
    }

    private static string[] GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private sealed record AgentEvidenceResult(string Id, string Kind, bool Required, string Status, bool Succeeded, int ExitCode, string LogPath, string[] Covers);

    private void VerifyPlatformerSettings(string projectRoot, List<BuildDiagnostic> errors)
    {
        var settingsPath = Path.Combine(projectRoot, "Platformer.e2d");
        if (!File.Exists(settingsPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath, Encoding.UTF8));
        var settings = document.RootElement;
        if (settings.GetProperty("format").GetString() != "Electron2D.ProjectSettings" || settings.GetProperty("name").GetString() != "Platformer")
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-SETTINGS", "Platformer project settings are invalid.", "examples/platformer/Platformer.e2d"));
        }

        var actions = settings.GetProperty("input").GetProperty("actions").EnumerateArray()
            .Select(action => action.GetProperty("name").GetString())
            .Where(name => name is not null)
            .Select(name => name!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expectedActions = new[] { "jump", "move_left", "move_right", "pause" };
        if (!actions.SequenceEqual(expectedActions, StringComparer.Ordinal))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-INPUT", "Platformer Input Map mismatch.", "examples/platformer/Platformer.e2d"));
        }

        var presets = settings.GetProperty("exportPresets");
        if (presets.GetProperty("format").GetString() != "Electron2D.ExportPresets")
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-EXPORT-PRESETS", "Platformer export presets format is invalid.", "examples/platformer/Platformer.e2d"));
        }

        var targets = presets.GetProperty("presets").EnumerateArray()
            .Select(preset => preset.GetProperty("target").GetString())
            .Where(target => target is not null)
            .Select(target => target!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var expectedTargets = new[] { "AndroidArm64", "IosArm64", "LinuxX64", "MacOSArm64", "WebAssemblyBrowser", "WindowsX64" }.Order(StringComparer.Ordinal).ToArray();
        if (!targets.SequenceEqual(expectedTargets, StringComparer.Ordinal))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-EXPORT-TARGETS", "Platformer export target mismatch.", "examples/platformer/Platformer.e2d"));
        }
    }

    private void VerifyPlatformerTasks(string projectRoot, List<BuildDiagnostic> errors)
    {
        var taskRoot = Path.Combine(projectRoot, ".electron2d", "tasks");
        var boardPath = Path.Combine(taskRoot, "board.e2tasks");
        if (!File.Exists(boardPath))
        {
            return;
        }

        using var boardDocument = JsonDocument.Parse(File.ReadAllText(boardPath, Encoding.UTF8));
        var board = boardDocument.RootElement;
        if (board.GetProperty("format").GetString() != "Electron2D.TaskBoard" || board.GetProperty("version").GetInt32() != 1)
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-BOARD", "Platformer task metadata is invalid.", "examples/platformer/.electron2d/tasks/board.e2tasks"));
        }

        var expectedTasks = new Dictionary<string, (string Status, string Priority, string[] Dependencies)>(StringComparer.Ordinal)
        {
            ["T-0222"] = ("Ready", "P0", []),
            ["T-0223"] = ("Blocked", "P0", ["T-0222"]),
            ["T-0225"] = ("Blocked", "P0", ["T-0222", "T-0223"]),
            ["T-0221"] = ("Blocked", "P0", ["T-0215", "T-0223", "T-0225"]),
            ["T-0166"] = ("Blocked", "P0", ["T-0221", "T-0222", "T-0223", "T-0225"]),
            ["platformer-acceptance"] = ("Blocked", "P0", ["T-0166"])
        };
        foreach (var (taskId, expected) in expectedTasks)
        {
            var taskPath = Path.Combine(taskRoot, taskId + ".e2task");
            if (!File.Exists(taskPath))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-FILE", $"Platformer task document is missing: {taskId}.", "examples/platformer/.electron2d/tasks/" + taskId + ".e2task"));
                continue;
            }

            using var taskDocument = JsonDocument.Parse(File.ReadAllText(taskPath, Encoding.UTF8));
            var task = taskDocument.RootElement;
            if (task.GetProperty("format").GetString() != "Electron2D.TaskFile" ||
                task.GetProperty("version").GetInt32() != 1 ||
                task.GetProperty("taskId").GetString() != taskId ||
                task.GetProperty("status").GetString() != expected.Status ||
                task.GetProperty("priority").GetString() != expected.Priority)
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-METADATA", $"Platformer task '{taskId}' metadata mismatch.", "examples/platformer/.electron2d/tasks/" + taskId + ".e2task"));
            }

            var dependencies = task.GetProperty("dependencies").EnumerateArray().Select(item => item.GetString()).Where(item => item is not null).Select(item => item!).ToArray();
            if (!dependencies.SequenceEqual(expected.Dependencies, StringComparer.Ordinal))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-DEPENDENCIES", $"Platformer task '{taskId}' dependency mismatch.", "examples/platformer/.electron2d/tasks/" + taskId + ".e2task"));
            }

            if (taskId == "platformer-acceptance" &&
                (task.GetProperty("readiness").GetString() != "BlockedByDependencies" || task.GetProperty("acceptanceState").GetString() != "ChangesRequested"))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-ACCEPTANCE-TASK", "Platformer acceptance task must be blocked by migrated dependencies.", "examples/platformer/.electron2d/tasks/platformer-acceptance.e2task"));
            }
        }

        var readyTaskIds = GetBoardColumnTaskIds(board, "Ready");
        if (!readyTaskIds.SequenceEqual(["T-0222"], StringComparer.Ordinal))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-BOARD", "Platformer task board Ready column mismatch.", "examples/platformer/.electron2d/tasks/board.e2tasks"));
        }

        var blockedTaskIds = GetBoardColumnTaskIds(board, "Blocked");
        var expectedBlocked = new[] { "T-0223", "T-0225", "T-0221", "T-0166", "platformer-acceptance" };
        if (!blockedTaskIds.SequenceEqual(expectedBlocked, StringComparer.Ordinal))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-BOARD", "Platformer task board Blocked column mismatch.", "examples/platformer/.electron2d/tasks/board.e2tasks"));
        }

        if (GetBoardColumnTaskIds(board, "AwaitingAcceptance").Length != 0)
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-TASK-BOARD", "Platformer task board must not keep stale AwaitingAcceptance tasks.", "examples/platformer/.electron2d/tasks/board.e2tasks"));
        }
    }

    private static string[] GetBoardColumnTaskIds(JsonElement board, string status)
    {
        return board.GetProperty("columns").EnumerateArray()
            .Where(column => column.GetProperty("status").GetString() == status)
            .SelectMany(column => column.GetProperty("taskIds").EnumerateArray())
            .Select(item => item.GetString())
            .Where(item => item is not null)
            .Select(item => item!)
            .ToArray();
    }

    private void VerifyPlatformerResources(string projectRoot, List<BuildDiagnostic> errors)
    {
        var manifestPath = Path.Combine(projectRoot, "resources", "platformer.manifest.json");
        if (!File.Exists(manifestPath))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
        var manifest = document.RootElement;
        if (manifest.GetProperty("format").GetString() != "Platformer.Resources" ||
            manifest.GetProperty("networkRequiredDuringBuild").ValueKind != JsonValueKind.False)
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-MANIFEST", "Platformer resource manifest is invalid.", "examples/platformer/resources/platformer.manifest.json"));
        }

        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var resource in manifest.GetProperty("resources").EnumerateArray())
        {
            var relativePath = resource.GetProperty("path").GetString() ?? string.Empty;
            if (!TryResolvePlatformerResource(projectRoot, relativePath, out var fullPath))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-PATH", $"Platformer resource path is invalid: {relativePath}.", "examples/platformer/resources/platformer.manifest.json"));
                continue;
            }

            if (!File.Exists(fullPath))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-MISSING", $"Platformer resource file was not found: {relativePath}.", "examples/platformer/" + relativePath));
                continue;
            }

            foreach (var role in resource.GetProperty("roles").EnumerateArray().Select(role => role.GetString()).Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role!))
            {
                roles.Add(role);
            }

            VerifyPlatformerResourceContent(fullPath, Path.GetExtension(relativePath), relativePath, errors);
        }

        foreach (var requiredRole in new[] { "tileset", "tilemap", "character-atlas", "animation", "jump-audio", "walk-audio", "checkpoint-audio", "source-level", "ui-font", "pause-menu-ui" })
        {
            if (!roles.Contains(requiredRole))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-ROLE", $"Platformer resource manifest is missing role: {requiredRole}.", "examples/platformer/resources/platformer.manifest.json"));
            }
        }
    }

    private void VerifyPlatformerScriptSurface(string projectRoot, List<BuildDiagnostic> errors)
    {
        if (File.Exists(Path.Combine(projectRoot, "Program.cs")))
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-PROGRAM", "Platformer must not provide Program.cs; editor/dev run and export own the launch flow.", "examples/platformer/Program.cs"));
        }

        var sourcePath = Path.Combine(projectRoot, "scripts", "PlatformerGame.cs");
        if (!File.Exists(sourcePath))
        {
            return;
        }

        var source = File.ReadAllText(sourcePath, Encoding.UTF8);
        foreach (var forbiddenText in new[] { "Console.ReadKey", "FRAME platformer", "SDL.", "SDL3", "CreateWindow", "RuntimeHost.Run", "ProjectRuntimeRunner", "Electron2DApplication", "Electron2DRunOptions", "Electron2DRunResult" })
        {
            if (source.Contains(forbiddenText, StringComparison.Ordinal))
            {
                errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-FORBIDDEN-API", $"Platformer project script must use only public Electron2D game API. Forbidden text: {forbiddenText}.", "examples/platformer/scripts/PlatformerGame.cs"));
            }
        }
    }

    private bool TryResolvePlatformerResource(string projectRoot, string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) ||
            Regex.IsMatch(relativePath, "^[a-zA-Z][a-zA-Z0-9+.-]*://", RegexOptions.CultureInvariant))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(projectRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private void VerifyPlatformerResourceContent(string fullPath, string extension, string relativePath, List<BuildDiagnostic> errors)
    {
        try
        {
            switch (extension.ToLowerInvariant())
            {
                case ".png":
                    if (!TryAssertPng(fullPath))
                    {
                        errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-SIGNATURE", $"PNG signature is invalid: {relativePath}.", "examples/platformer/" + relativePath));
                    }

                    break;
                case ".ogg":
                    if (!File.ReadAllBytes(fullPath).Take(4).SequenceEqual(new byte[] { 0x4F, 0x67, 0x67, 0x53 }))
                    {
                        errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-SIGNATURE", $"OGG signature is invalid: {relativePath}.", "examples/platformer/" + relativePath));
                    }

                    break;
                case ".ttf":
                    var bytes = File.ReadAllBytes(fullPath);
                    var tag = bytes.Length >= 4 ? Encoding.ASCII.GetString(bytes, 0, 4) : string.Empty;
                    var isTrueType = bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00;
                    if (!isTrueType && tag != "OTTO")
                    {
                        errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-SIGNATURE", $"TTF signature is invalid: {relativePath}.", "examples/platformer/" + relativePath));
                    }

                    break;
                case ".tmx":
                case ".tsx":
                    XDocument.Load(fullPath);
                    break;
                case ".json":
                    using (JsonDocument.Parse(File.ReadAllText(fullPath, Encoding.UTF8)))
                    {
                    }

                    break;
                default:
                    errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-EXTENSION", $"Unsupported Platformer resource extension: {relativePath}.", "examples/platformer/" + relativePath));
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or System.Xml.XmlException)
        {
            errors.Add(Error("verify platformer", "E2D-BUILD-PLATFORMER-RESOURCE-CONTENT", $"Platformer resource content is invalid for {relativePath}: {ex.Message}", "examples/platformer/" + relativePath));
        }
    }

    private static bool ContainsAll(string text, IEnumerable<string> fragments)
    {
        return fragments.All(fragment => text.Contains(fragment, StringComparison.Ordinal));
    }

    private static bool TryValidatePlatformerJson(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            return root.GetProperty("succeeded").ValueKind == JsonValueKind.True &&
                root.GetProperty("command").GetString() == "validate";
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryAssertPng(string path)
    {
        var bytes = File.ReadAllBytes(path);
        return bytes.Length >= 8 && bytes.Take(8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
    }

    private static bool TryAssertPngMinDimensions(string path, int minWidth, int minHeight)
    {
        var bytes = File.ReadAllBytes(path);
        if (!TryAssertPng(path) || bytes.Length < 24)
        {
            return false;
        }

        var width = ((int)bytes[16] << 24) | ((int)bytes[17] << 16) | ((int)bytes[18] << 8) | bytes[19];
        var height = ((int)bytes[20] << 24) | ((int)bytes[21] << 16) | ((int)bytes[22] << 8) | bytes[23];
        return width >= minWidth && height >= minHeight;
    }

    private static ProcessResult RunProcess(string fileName, IReadOnlyList<string> arguments, string workingDirectory, IReadOnlyDictionary<string, string?>? environment, TimeSpan timeout)
    {
        using var process = new Process();
        process.StartInfo.FileName = fileName;
        foreach (var argument in arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.StartInfo.WorkingDirectory = workingDirectory;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                if (value is null)
                {
                    process.StartInfo.Environment.Remove(key);
                }
                else
                {
                    process.StartInfo.Environment[key] = value;
                }
            }
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)Math.Min(int.MaxValue, timeout.TotalMilliseconds)))
        {
            process.Kill(entireProcessTree: true);
            return new ProcessResult(RepositoryBuildExitCodes.Failed, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult(), TimedOut: true);
        }

        return new ProcessResult(process.ExitCode, stdoutTask.GetAwaiter().GetResult(), stderrTask.GetAwaiter().GetResult(), TimedOut: false);
    }

    private sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);

    private int VerifyRequiredFragments(string step, string successCode, string successMessage, IReadOnlyDictionary<string, string[]> requiredFragments, IReadOnlyList<string> forbidden)
    {
        var errors = new List<BuildDiagnostic>();
        foreach (var pair in requiredFragments)
        {
            var path = ResolveRepositoryPath(pair.Key);
            if (!File.Exists(path))
            {
                errors.Add(Error(step, "E2D-BUILD-STATIC-FILE-MISSING", $"Required file is missing: {pair.Key}.", pair.Key));
                continue;
            }

            var content = File.ReadAllText(path, Encoding.UTF8);
            foreach (var fragment in pair.Value)
            {
                if (!content.Contains(fragment, StringComparison.Ordinal))
                {
                    errors.Add(Error(step, "E2D-BUILD-STATIC-FRAGMENT-MISSING", $"Required fragment is missing in {pair.Key}: {fragment}.", pair.Key));
                }
            }

            foreach (var fragment in forbidden)
            {
                if (content.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(Error(step, "E2D-BUILD-STATIC-FORBIDDEN-FRAGMENT", $"Forbidden fragment is present in {pair.Key}: {fragment}.", pair.Key));
                }
            }
        }

        return Complete(step, successCode, successMessage, errors);
    }

    private int Complete(string step, string successCode, string successMessage, List<BuildDiagnostic> errors)
    {
        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic("verify", step, "info", successCode, successMessage));
        return RepositoryBuildExitCodes.Success;
    }

    private JsonElement? ReadJsonObject(string fullPath, string step, List<BuildDiagnostic> errors)
    {
        if (!File.Exists(fullPath))
        {
            errors.Add(Error(step, "E2D-BUILD-STATIC-FILE-MISSING", $"Required JSON file is missing: {ToRepositoryPath(fullPath)}.", ToRepositoryPath(fullPath)));
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(fullPath, Encoding.UTF8));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                errors.Add(Error(step, "E2D-BUILD-STATIC-JSON-ROOT", $"JSON root must be an object: {ToRepositoryPath(fullPath)}.", ToRepositoryPath(fullPath)));
                return null;
            }

            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            errors.Add(Error(step, "E2D-BUILD-STATIC-JSON", $"JSON file could not be parsed: {ToRepositoryPath(fullPath)}: {ex.Message}", ToRepositoryPath(fullPath)));
            return null;
        }
    }

    private static bool TryGetString(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = property.GetString() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(value);
    }

    private string ResolveRepositoryPath(string relativeOrAbsolutePath)
    {
        return Path.IsPathRooted(relativeOrAbsolutePath)
            ? Path.GetFullPath(relativeOrAbsolutePath)
            : Path.GetFullPath(Path.Combine(repositoryRoot, relativeOrAbsolutePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private BuildDiagnostic Error(string step, string code, string message, string path)
    {
        return new BuildDiagnostic("verify", step, "error", code, message, Path: path);
    }

    private static AgentBenchmarkArguments ParseAgentBenchmarkArguments(string[] args)
    {
        var list = false;
        var dryRun = false;
        var continueOnFailure = false;
        string? suite = null;
        string? output = null;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--list":
                    list = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--continue-on-failure":
                    continueOnFailure = true;
                    break;
                case "--suite" when i + 1 < args.Length:
                    suite = args[++i];
                    break;
                case "--output" when i + 1 < args.Length:
                    output = args[++i];
                    break;
                default:
                    return new AgentBenchmarkArguments(false, false, false, false, null, null, "Expected: verify agent-acceptance-benchmarks [--list] [--dry-run] [--suite <id>] [--output <path>] [--continue-on-failure].");
            }
        }

        if (list && dryRun)
        {
            return new AgentBenchmarkArguments(false, false, false, false, null, null, "--list and --dry-run cannot be combined.");
        }

        if (suite is not null && suite is not "editor-co-development" and not "headless-ai")
        {
            return new AgentBenchmarkArguments(false, false, false, false, null, null, "Unknown benchmark suite. Expected editor-co-development or headless-ai.");
        }

        return new AgentBenchmarkArguments(true, list, dryRun, continueOnFailure, suite, output, string.Empty);
    }

    private sealed record AgentBenchmarkArguments(bool Succeeded, bool List, bool DryRun, bool ContinueOnFailure, string? Suite, string? OutputDirectory, string ErrorMessage);
}

internal sealed class ReferenceGameAssetsVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".ogg",
        ".ttf",
        ".tmx",
        ".tsx",
        ".json"
    };

    private static readonly HashSet<string> ForbiddenExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".url",
        ".sfk",
        ".tmp",
        ".cache"
    };

    public int Verify()
    {
        var errors = new List<BuildDiagnostic>();
        var manifestPath = Path.Combine(repositoryRoot, "data", "assets", "reference-games", "manifest.json");
        var licensesPath = Path.Combine(repositoryRoot, "data", "assets", "reference-games", "LICENSES.md");
        var readmePath = Path.Combine(repositoryRoot, "data", "assets", "reference-games", "README.md");
        if (!File.Exists(licensesPath))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-LICENSES", "Reference game asset licenses file is missing.", "data/assets/reference-games/LICENSES.md"));
        }

        if (!File.Exists(readmePath))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-README", "Reference game asset README is missing.", "data/assets/reference-games/README.md"));
        }

        if (!File.Exists(manifestPath))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-MANIFEST-MISSING", "Reference game asset manifest is missing.", "data/assets/reference-games/manifest.json"));
        }
        else
        {
            using var document = JsonDocument.Parse(File.ReadAllText(manifestPath, Encoding.UTF8));
            var root = document.RootElement;
            AssertString(root, "release", "0.1.0-preview", "manifest", errors);
            AssertString(root, "assetRoot", "data/assets/reference-games", "manifest", errors);
            AssertInt(root, "schemaVersion", 1, "manifest", errors);
            if (!TryGetBoolean(root, "networkRequiredDuringBuild", "manifest", errors, out var networkRequiredDuringBuild) || networkRequiredDuringBuild)
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-NETWORK", "Reference game assets must not require network during build.", "data/assets/reference-games/manifest.json"));
            }

            var sourceIds = new HashSet<string>(StringComparer.Ordinal);
            if (!root.TryGetProperty("sources", out var sources) || sources.ValueKind != JsonValueKind.Array)
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SOURCES", "Reference asset manifest must contain sources array.", "data/assets/reference-games/manifest.json"));
            }
            else
            {
                foreach (var source in sources.EnumerateArray())
                {
                    var sourceId = GetString(source, "id", "source", errors);
                    if (!sourceIds.Add(sourceId))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SOURCE-DUPLICATE", $"Duplicate source id in reference asset manifest: {sourceId}.", "data/assets/reference-games/manifest.json"));
                    }

                    AssertNonEmpty(source, "author", $"source {sourceId}", errors);
                    var license = GetString(source, "license", $"source {sourceId}", errors);
                    if (license is not ("CC0-1.0" or "MIT"))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-LICENSE", $"Source has unsupported license '{license}': {sourceId}.", "data/assets/reference-games/manifest.json"));
                    }

                    AssertNonEmpty(source, "licenseUrl", $"source {sourceId}", errors);
                    AssertNonEmpty(source, "sourceUrl", $"source {sourceId}", errors);
                    if (source.TryGetProperty("sourceArchiveSha256", out var archiveHash) &&
                        archiveHash.ValueKind == JsonValueKind.String &&
                        !string.IsNullOrWhiteSpace(archiveHash.GetString()) &&
                        !IsLowercaseSha256(archiveHash.GetString()!))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SHA256-FORMAT", $"Source archive hash for {sourceId} must be a lowercase SHA-256 hex string.", "data/assets/reference-games/manifest.json"));
                    }
                }
            }

            foreach (var requiredSource in new[] { "kenney-pixel-platformer", "kenney-ui-pack", "kenney-rpg-sounds" })
            {
                if (!sourceIds.Contains(requiredSource))
                {
                    errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SOURCE-MISSING", $"Reference game asset manifest is missing required source: {requiredSource}.", "data/assets/reference-games/manifest.json"));
                }
            }

            var assetIds = new HashSet<string>(StringComparer.Ordinal);
            var declaredPaths = new HashSet<string>(StringComparer.Ordinal);
            var assetPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rolesByGame = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-ASSETS", "Reference asset manifest must contain assets array.", "data/assets/reference-games/manifest.json"));
            }
            else
            {
                foreach (var asset in assets.EnumerateArray())
                {
                    var assetId = GetString(asset, "id", "asset", errors);
                    if (!assetIds.Add(assetId))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-DUPLICATE", $"Duplicate asset id in reference asset manifest: {assetId}.", "data/assets/reference-games/manifest.json"));
                    }

                    var relativePath = GetString(asset, "path", $"asset {assetId}", errors);
                    declaredPaths.Add(relativePath);
                    if (!relativePath.StartsWith("data/assets/reference-games/", StringComparison.Ordinal) || relativePath.Contains('\\', StringComparison.Ordinal))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-PATH", $"Reference asset path must stay under data/assets/reference-games and use forward slashes: {relativePath}.", "data/assets/reference-games/manifest.json"));
                    }

                    if (!assetPaths.Add(relativePath))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-DUPLICATE-PATH", $"Duplicate asset path in reference asset manifest: {relativePath}.", "data/assets/reference-games/manifest.json"));
                    }

                    var extension = Path.GetExtension(relativePath);
                    if (ForbiddenExtensions.Contains(extension))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-FORBIDDEN-EXTENSION", $"Forbidden generated/source-helper file is listed as an asset: {relativePath}.", "data/assets/reference-games/manifest.json"));
                    }

                    if (!AllowedExtensions.Contains(extension))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-EXTENSION", $"Unsupported reference asset extension '{extension}': {relativePath}.", "data/assets/reference-games/manifest.json"));
                    }

                    var source = GetString(asset, "source", $"asset {assetId}", errors);
                    if (!sourceIds.Contains(source))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SOURCE-UNKNOWN", $"Asset references unknown source '{source}': {assetId}.", "data/assets/reference-games/manifest.json"));
                    }

                    var expectedHash = GetString(asset, "sha256", $"asset {assetId}", errors);
                    if (!IsLowercaseSha256(expectedHash))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SHA256-FORMAT", $"Asset hash for {assetId} must be a lowercase SHA-256 hex string.", "data/assets/reference-games/manifest.json"));
                    }

                    if (!TryResolveRepositoryPath(relativePath, out var fullPath))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-PATH", $"Reference asset path escapes repository root or is not local: {relativePath}.", "data/assets/reference-games/manifest.json"));
                        continue;
                    }

                    if (!File.Exists(fullPath))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-FILE-MISSING", $"Reference asset is missing: {relativePath}.", relativePath));
                        continue;
                    }

                    var expectedBytes = GetInt64(asset, "bytes", $"asset {assetId}", errors);
                    var actualBytes = new FileInfo(fullPath).Length;
                    if (expectedBytes != actualBytes)
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-BYTES", $"Reference asset byte size mismatch for {relativePath}.", relativePath));
                    }

                    var actualHash = Sha256File(fullPath);
                    if (!string.Equals(expectedHash, actualHash, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SHA256", $"Reference asset SHA-256 mismatch for {relativePath}.", relativePath));
                    }

                    VerifyAssetContent(fullPath, extension, relativePath, errors);
                    foreach (var game in GetStringArray(asset, "games"))
                    {
                        if (!rolesByGame.TryGetValue(game, out var roles))
                        {
                            roles = new HashSet<string>(StringComparer.Ordinal);
                            rolesByGame.Add(game, roles);
                        }

                        foreach (var role in GetStringArray(asset, "roles"))
                        {
                            roles.Add(role);
                        }
                    }
                }
            }

            foreach (var file in Directory.EnumerateFiles(Path.Combine(repositoryRoot, "data", "assets", "reference-games"), "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(repositoryRoot, file).Replace('\\', '/');
                if (relativePath.EndsWith("manifest.json", StringComparison.Ordinal) ||
                    relativePath.EndsWith("README.md", StringComparison.Ordinal) ||
                    relativePath.EndsWith("LICENSES.md", StringComparison.Ordinal) ||
                    declaredPaths.Contains(relativePath))
                {
                    continue;
                }

                errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-UNDECLARED", $"Reference asset file is not declared in manifest: {relativePath}.", relativePath));
            }

            if (root.TryGetProperty("requirements", out var requirements) && requirements.ValueKind == JsonValueKind.Object)
            {
                foreach (var requirement in requirements.EnumerateObject())
                {
                    if (!rolesByGame.TryGetValue(requirement.Name, out var roles))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-REQUIREMENTS", $"Reference asset manifest has no assets for required game: {requirement.Name}.", "data/assets/reference-games/manifest.json"));
                        continue;
                    }

                    foreach (var role in requirement.Value.EnumerateArray().Select(item => item.GetString()).Where(role => !string.IsNullOrWhiteSpace(role)).Select(role => role!))
                    {
                        if (!roles.Contains(role))
                        {
                            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-REQUIREMENTS", $"Reference game '{requirement.Name}' is missing required asset role: {role}.", "data/assets/reference-games/manifest.json"));
                        }
                    }
                }
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic("verify", "verify reference-game-assets", "info", "E2D-BUILD-REFERENCE-GAME-ASSETS-PASSED", "Reference game asset verification passed."));
        return RepositoryBuildExitCodes.Success;
    }

    private static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private bool TryResolveRepositoryPath(string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) ||
            Regex.IsMatch(relativePath, "^[a-zA-Z][a-zA-Z0-9+.-]*://", RegexOptions.CultureInvariant))
        {
            return false;
        }

        var candidate = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(repositoryRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        fullPath = candidate;
        return true;
    }

    private static void VerifyAssetContent(string fullPath, string extension, string relativePath, List<BuildDiagnostic> errors)
    {
        try
        {
            switch (extension.ToLowerInvariant())
            {
                case ".png":
                    AssertPng(fullPath, relativePath, errors);
                    break;
                case ".ogg":
                    AssertSignature(fullPath, [0x4F, 0x67, 0x67, 0x53], "OGG", relativePath, errors);
                    break;
                case ".ttf":
                    AssertTtf(fullPath, relativePath, errors);
                    break;
                case ".json":
                    using (JsonDocument.Parse(File.ReadAllText(fullPath, Encoding.UTF8)))
                    {
                    }

                    break;
                case ".tmx":
                case ".tsx":
                    XDocument.Load(fullPath);
                    break;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or System.Xml.XmlException)
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-CONTENT", $"Reference asset content is invalid for {relativePath}: {ex.Message}", relativePath));
        }
    }

    private static void AssertPng(string fullPath, string relativePath, List<BuildDiagnostic> errors)
    {
        AssertSignature(fullPath, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "PNG", relativePath, errors);
    }

    private static void AssertTtf(string fullPath, string relativePath, List<BuildDiagnostic> errors)
    {
        var bytes = File.ReadAllBytes(fullPath);
        if (bytes.Length < 4)
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SIGNATURE", $"Font file is too small: {relativePath}.", relativePath));
            return;
        }

        var tag = Encoding.ASCII.GetString(bytes, 0, 4);
        var isTrueType = bytes[0] == 0x00 && bytes[1] == 0x01 && bytes[2] == 0x00 && bytes[3] == 0x00;
        if (!isTrueType && tag != "OTTO")
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SIGNATURE", $"Font signature is invalid: {relativePath}.", relativePath));
        }
    }

    private static void AssertSignature(string fullPath, byte[] signature, string label, string relativePath, List<BuildDiagnostic> errors)
    {
        var bytes = File.ReadAllBytes(fullPath);
        if (bytes.Length < signature.Length || !bytes.Take(signature.Length).SequenceEqual(signature))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-SIGNATURE", $"{label} signature is invalid: {relativePath}.", relativePath));
        }
    }

    private static void AssertString(JsonElement element, string propertyName, string expected, string context, List<BuildDiagnostic> errors)
    {
        var actual = GetString(element, propertyName, context, errors);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-JSON", $"{context} property {propertyName} must be {expected}.", "data/assets/reference-games/manifest.json"));
        }
    }

    private static void AssertInt(JsonElement element, string propertyName, int expected, string context, List<BuildDiagnostic> errors)
    {
        var actual = GetInt64(element, propertyName, context, errors);
        if (actual != expected)
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-JSON", $"{context} property {propertyName} must be {expected}.", "data/assets/reference-games/manifest.json"));
        }
    }

    private static void AssertNonEmpty(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (string.IsNullOrWhiteSpace(GetString(element, propertyName, context, errors)))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-JSON", $"{context} property {propertyName} must not be empty.", "data/assets/reference-games/manifest.json"));
        }
    }

    private static string GetString(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-JSON", $"{context} is missing string property: {propertyName}.", "data/assets/reference-games/manifest.json"));
        return string.Empty;
    }

    private static long GetInt64(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var result))
        {
            return result;
        }

        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-JSON", $"{context} is missing integer property: {propertyName}.", "data/assets/reference-games/manifest.json"));
        return 0;
    }

    private static bool TryGetBoolean(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors, out bool result)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            result = value.GetBoolean();
            return true;
        }

        errors.Add(Error("E2D-BUILD-REFERENCE-ASSETS-JSON", $"{context} is missing boolean property: {propertyName}.", "data/assets/reference-games/manifest.json"));
        result = false;
        return false;
    }

    private static string[] GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private static bool IsLowercaseSha256(string value)
    {
        return Regex.IsMatch(value, "^[a-f0-9]{64}$", RegexOptions.CultureInvariant);
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify reference-game-assets", "error", code, message, Path: path);
    }
}

internal sealed class ReferenceGamePlatformMatrixVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, StaticRepositoryVerifier staticVerifier)
{
    private static readonly string[] ExpectedRuntimeTargets =
    [
        "AndroidArm64",
        "IosArm64",
        "LinuxX64",
        "MacOSArm64",
        "WebAssemblyBrowser",
        "WindowsX64"
    ];

    private static readonly string[] ExpectedEditorTargets =
    [
        "Linux",
        "Windows",
        "macOS"
    ];

    private static readonly string[] AllowedDifferences =
    [
        "export preset target/configuration/runtime identifier/output directory",
        "renderer profile",
        "application icon and branding metadata",
        "signing references without secrets",
        "storefront metadata",
        "browser hosting metadata"
    ];

    private static readonly string[] PlatformNames =
    [
        "Android",
        "AndroidArm64",
        "Ios",
        "iOS",
        "IosArm64",
        "Linux",
        "LinuxX64",
        "MacOS",
        "MacOSArm64",
        "Windows",
        "WindowsX64",
        "WebAssembly",
        "WebAssemblyBrowser",
        "browser-wasm"
    ];

    public int Verify()
    {
        var platformer = staticVerifier.VerifyPlatformer();
        if (platformer != RepositoryBuildExitCodes.Success)
        {
            return platformer;
        }

        var errors = new List<BuildDiagnostic>();
        var artifactPath = Path.Combine(repositoryRoot, "data", "quality", "reference-game-platform-matrix.json");
        if (!File.Exists(artifactPath))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-MISSING", "Reference game platform matrix artifact is missing.", "data/quality/reference-game-platform-matrix.json"));
        }
        else
        {
            using var document = JsonDocument.Parse(File.ReadAllText(artifactPath, Encoding.UTF8));
            var root = document.RootElement;
            if (root.GetProperty("format").GetString() != "Electron2D.ReferenceGamePlatformMatrix")
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-FORMAT", "Reference game platform matrix has an invalid format.", "data/quality/reference-game-platform-matrix.json"));
            }

            if (root.TryGetProperty("targetSet", out _))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-LEGACY", "Reference game platform matrix must not declare legacy targetSet.", "data/quality/reference-game-platform-matrix.json"));
            }

            if (!root.TryGetProperty("version", out var version) || version.GetInt32() != 2 ||
                !root.TryGetProperty("release", out var release) || release.GetString() != "0.1.0-preview")
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-IDENTITY", "Reference game platform matrix has invalid version or release.", "data/quality/reference-game-platform-matrix.json"));
            }

            AssertArray(root, "runtimeTargets", ExpectedRuntimeTargets, errors);
            AssertArray(root, "editorTargets", ExpectedEditorTargets, errors);
            AssertArray(root, "releaseVerificationTargets", ExpectedRuntimeTargets, errors, propertyName: "target");
            VerifyReleaseVerificationTargets(root, errors);
            VerifyReleaseDecision(root, errors);
            AssertArray(root, "allowedDifferences", AllowedDifferences, errors);

            var project = root.GetProperty("projects").EnumerateArray().SingleOrDefault(project => project.GetProperty("id").GetString() == "platformer");
            if (project.ValueKind != JsonValueKind.Object)
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PROJECT", "Reference game platform matrix must declare platformer project.", "data/quality/reference-game-platform-matrix.json"));
            }
            else
            {
                foreach (var required in new[] { "projectPath", "projectFile", "settingsFile", "mainScene", "verifier" })
                {
                    if (!project.TryGetProperty(required, out var value) || value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
                    {
                        errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PROJECT-FIELD", $"Platformer matrix project is missing field: {required}.", "data/quality/reference-game-platform-matrix.json"));
                    }
                }

                VerifyProject(project, errors);
            }
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var summaryRoot = Path.Combine(repositoryRoot, ".temp", "reference-game-platform-matrix");
        Directory.CreateDirectory(summaryRoot);
        File.WriteAllText(Path.Combine(summaryRoot, "summary.json"), JsonSerializer.Serialize(new
        {
            format = "Electron2D.ReferenceGamePlatformMatrix.VerificationSummary",
            version = 1,
            release = "0.1.0-preview",
            projects = new[]
            {
                new
                {
                    id = "platformer",
                    projectPath = "examples/platformer",
                    verifier = "dotnet run --project eng/Electron2D.Build -- verify platformer",
                    targets = ExpectedRuntimeTargets.Order(StringComparer.Ordinal).ToArray(),
                    sharedCodebaseChecked = true
                }
            },
            runtimeTargets = ExpectedRuntimeTargets.Order(StringComparer.Ordinal).ToArray(),
            editorTargets = ExpectedEditorTargets.Order(StringComparer.Ordinal).ToArray(),
            releaseVerificationTargets = ExpectedRuntimeTargets.Order(StringComparer.Ordinal).ToArray(),
            allowedDifferences = AllowedDifferences.Order(StringComparer.Ordinal).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine, Encoding.UTF8);
        diagnostics.Write(new BuildDiagnostic("verify", "verify reference-game-platform-matrix", "info", "E2D-BUILD-REFERENCE-GAME-PLATFORM-MATRIX-PASSED", "Reference game platform matrix verification passed.", OutputPath: ".temp/reference-game-platform-matrix/summary.json"));
        return RepositoryBuildExitCodes.Success;
    }

    private static void AssertArray(JsonElement root, string property, IReadOnlyList<string> expected, List<BuildDiagnostic> errors, string? propertyName = null)
    {
        if (!root.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-ARRAY", $"Reference game platform matrix is missing array: {property}.", "data/quality/reference-game-platform-matrix.json"));
            return;
        }

        var actual = propertyName is null
            ? element.EnumerateArray().Select(item => item.GetString()).Where(value => value is not null).Select(value => value!).Order(StringComparer.Ordinal).ToArray()
            : element.EnumerateArray().Select(item => item.GetProperty(propertyName).GetString()).Where(value => value is not null).Select(value => value!).Order(StringComparer.Ordinal).ToArray();
        if (!actual.SequenceEqual(expected.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-TARGETS", $"Reference game platform matrix target mismatch for {property}.", "data/quality/reference-game-platform-matrix.json"));
        }
    }

    private void VerifyReleaseVerificationTargets(JsonElement root, List<BuildDiagnostic> errors)
    {
        if (!root.TryGetProperty("releaseVerificationTargets", out var targets) || targets.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var target in targets.EnumerateArray())
        {
            var targetName = target.GetProperty("target").GetString() ?? "<unknown>";
            if (!target.TryGetProperty("realSmokeSoakRequired", out var smoke) || smoke.ValueKind != JsonValueKind.True)
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-RELEASE-TARGET", $"{targetName} releaseVerificationTarget must require real smoke/soak.", "data/quality/reference-game-platform-matrix.json"));
            }

            if (!target.TryGetProperty("blockedEnvironmentArtifactAllowed", out var blocked) || blocked.ValueKind != JsonValueKind.True)
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-RELEASE-TARGET", $"{targetName} releaseVerificationTarget must allow blocked-environment artifact diagnostics.", "data/quality/reference-game-platform-matrix.json"));
            }

            if (!target.TryGetProperty("releaseGateBlocker", out var blocker) || blocker.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(blocker.GetString()))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-RELEASE-TARGET", $"{targetName} releaseVerificationTarget must describe the release gate blocker.", "data/quality/reference-game-platform-matrix.json"));
            }
        }
    }

    private static void VerifyReleaseDecision(JsonElement root, List<BuildDiagnostic> errors)
    {
        if (!root.TryGetProperty("releaseVerificationDecision", out var decision) ||
            decision.ValueKind != JsonValueKind.Object ||
            decision.GetProperty("id").GetString() != "all-runtime-targets-for-0.1.0-preview" ||
            decision.GetProperty("source").GetString() != "docs/releases/0.1.0-preview.md")
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-RELEASE-DECISION", "Reference game platform matrix releaseVerificationDecision is missing or invalid.", "data/quality/reference-game-platform-matrix.json"));
        }
    }

    private void VerifyProject(JsonElement project, List<BuildDiagnostic> errors)
    {
        var projectId = project.GetProperty("id").GetString() ?? "platformer";
        var projectRoot = ResolveRepositoryPath(project.GetProperty("projectPath").GetString() ?? string.Empty, errors);
        if (projectRoot is null || !Directory.Exists(projectRoot))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PROJECT", $"{projectId} project root was not found.", "data/quality/reference-game-platform-matrix.json"));
            return;
        }

        var projectFile = Path.Combine(projectRoot, project.GetProperty("projectFile").GetString() ?? string.Empty);
        var settingsFile = Path.Combine(projectRoot, project.GetProperty("settingsFile").GetString() ?? string.Empty);
        var presetFile = Path.Combine(projectRoot, project.GetProperty("exportPresetFile").GetString() ?? string.Empty);
        var mainScene = Path.Combine(projectRoot, project.GetProperty("mainScene").GetString() ?? string.Empty);
        foreach (var path in new[] { projectFile, settingsFile, presetFile, mainScene })
        {
            if (!File.Exists(path))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PROJECT-FILE", $"{projectId} file was not found: {ToRepositoryPath(path)}.", ToRepositoryPath(path)));
            }
        }

        if (errors.Count > 0)
        {
            return;
        }

        using var settingsDocument = JsonDocument.Parse(File.ReadAllText(settingsFile, Encoding.UTF8));
        var settings = settingsDocument.RootElement;
        if (settings.GetProperty("format").GetString() != "Electron2D.ProjectSettings" ||
            settings.GetProperty("name").GetString() != project.GetProperty("name").GetString() ||
            settings.GetProperty("mainScene").GetString() != project.GetProperty("mainScene").GetString())
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-SETTINGS", $"{projectId} project settings do not match the platform matrix artifact.", ToRepositoryPath(settingsFile)));
        }

        foreach (var forbiddenPath in new[] { "TASKS.md", "dev-diary", "completed-tasks" })
        {
            if (File.Exists(Path.Combine(projectRoot, forbiddenPath)) || Directory.Exists(Path.Combine(projectRoot, forbiddenPath)) ||
                settings.GetRawText().Contains(forbiddenPath, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-WORKFLOW-PATH", $"{projectId} must not contain or reference repository workflow path: {forbiddenPath}.", ToRepositoryPath(settingsFile)));
            }
        }

        using var presetsDocument = JsonDocument.Parse(File.ReadAllText(presetFile, Encoding.UTF8));
        var presetsRoot = presetsDocument.RootElement.TryGetProperty("exportPresets", out var nestedPresets) ? nestedPresets : presetsDocument.RootElement;
        if (presetsRoot.GetProperty("format").GetString() != "Electron2D.ExportPresets")
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PRESETS", $"{projectId} export preset format is invalid.", ToRepositoryPath(presetFile)));
        }

        var targets = presetsRoot.GetProperty("presets").EnumerateArray().Select(preset => preset.GetProperty("target").GetString()).Where(value => value is not null).Select(value => value!).ToArray();
        if (!targets.Order(StringComparer.Ordinal).SequenceEqual(ExpectedRuntimeTargets.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PRESETS", $"{projectId} export targets do not match runtimeTargets.", ToRepositoryPath(presetFile)));
        }

        foreach (var preset in presetsRoot.GetProperty("presets").EnumerateArray())
        {
            VerifySigningReference(preset, projectId, ToRepositoryPath(presetFile), errors);
        }

        AssertProjectDirectories(projectRoot, project, "scriptRoots", projectId, errors);
        AssertProjectDirectories(projectRoot, project, "sceneRoots", projectId, errors);
        AssertProjectDirectories(projectRoot, project, "resourceRoots", projectId, errors);
        AssertProjectDirectories(projectRoot, project, "editorMetadataRoots", projectId, errors);
        AssertNoConditionalGameplayCompile(projectFile, projectId, errors);
        AssertForbiddenPlatformRootsDoNotExist(projectRoot, GetStringArray(project, "forbiddenPlatformSpecificRoots"), projectId, errors);
        AssertNoPlatformSpecificGameForks(projectRoot, GetStringArray(project, "scriptRoots").Concat(GetStringArray(project, "sceneRoots")).Concat(GetStringArray(project, "resourceRoots")), projectId, errors);
        AssertEditorMetadataNotRuntimeResource(projectRoot, GetStringArray(project, "resourceRoots"), GetStringArray(project, "editorMetadataRoots"), projectId, errors);
    }

    private void VerifySigningReference(JsonElement preset, string projectId, string path, List<BuildDiagnostic> errors)
    {
        if (!preset.TryGetProperty("signing", out var signing) || signing.ValueKind != JsonValueKind.Object)
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-SIGNING", $"{projectId} preset must declare signing policy.", path));
            return;
        }

        var identity = signing.TryGetProperty("identity", out var identityProperty) ? identityProperty.GetString() ?? string.Empty : string.Empty;
        var credentialReference = signing.TryGetProperty("credentialReference", out var credentialProperty) ? credentialProperty.GetString() ?? string.Empty : string.Empty;
        if (identity.Contains("-----BEGIN", StringComparison.OrdinalIgnoreCase) || credentialReference.Contains("-----BEGIN", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-SIGNING", $"{projectId} preset appears to contain signing secret material.", path));
        }

        if (signing.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.True &&
            !credentialReference.StartsWith("env:", StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-SIGNING", $"{projectId} required signing preset must use an env: credentialReference.", path));
        }
    }

    private void AssertProjectDirectories(string projectRoot, JsonElement project, string propertyName, string projectId, List<BuildDiagnostic> errors)
    {
        foreach (var relativePath in GetStringArray(project, propertyName))
        {
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(fullPath))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PROJECT-DIRECTORY", $"{projectId} is missing {propertyName} entry: {relativePath}.", ToRepositoryPath(fullPath)));
            }
        }
    }

    private void AssertNoConditionalGameplayCompile(string projectFile, string projectId, List<BuildDiagnostic> errors)
    {
        var text = File.ReadAllText(projectFile, Encoding.UTF8);
        if (text.Contains("Condition=", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-CONDITIONAL-COMPILE", $"{projectId} project file must not use conditional compile or platform-specific item conditions.", ToRepositoryPath(projectFile)));
        }
    }

    private void AssertForbiddenPlatformRootsDoNotExist(string projectRoot, IEnumerable<string> forbiddenRoots, string projectId, List<BuildDiagnostic> errors)
    {
        foreach (var relativePath in forbiddenRoots)
        {
            var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PLATFORM-ROOT", $"{projectId} contains forbidden platform-specific root: {relativePath}.", ToRepositoryPath(fullPath)));
            }
        }
    }

    private void AssertNoPlatformSpecificGameForks(string projectRoot, IEnumerable<string> scanRoots, string projectId, List<BuildDiagnostic> errors)
    {
        foreach (var scanRoot in scanRoots)
        {
            var fullRoot = Path.Combine(projectRoot, scanRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(fullRoot))
            {
                continue;
            }

            foreach (var item in Directory.EnumerateFileSystemEntries(fullRoot, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(projectRoot, item).Replace('\\', '/');
                foreach (var segment in relativePath.Split('/'))
                {
                    var stem = Path.GetFileNameWithoutExtension(segment);
                    foreach (var platformName in PlatformNames)
                    {
                        if (stem.Equals(platformName, StringComparison.OrdinalIgnoreCase) ||
                            stem.StartsWith(platformName + ".", StringComparison.OrdinalIgnoreCase) ||
                            stem.StartsWith(platformName + "-", StringComparison.OrdinalIgnoreCase) ||
                            stem.EndsWith("." + platformName, StringComparison.OrdinalIgnoreCase) ||
                            stem.EndsWith("-" + platformName, StringComparison.OrdinalIgnoreCase))
                        {
                            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PLATFORM-FORK", $"{projectId} contains platform-specific gameplay/resource path: {relativePath}.", ToRepositoryPath(item)));
                        }
                    }
                }
            }
        }
    }

    private void AssertEditorMetadataNotRuntimeResource(string projectRoot, IEnumerable<string> resourceRoots, IEnumerable<string> editorMetadataRoots, string projectId, List<BuildDiagnostic> errors)
    {
        foreach (var metadataRoot in editorMetadataRoots)
        {
            var metadataPath = Path.Combine(projectRoot, metadataRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(metadataPath))
            {
                errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-EDITOR-METADATA", $"{projectId} is missing editor metadata root: {metadataRoot}.", ToRepositoryPath(metadataPath)));
            }
        }

        foreach (var resourceRoot in resourceRoots)
        {
            var resourcePath = Path.Combine(projectRoot, resourceRoot.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(resourcePath))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(resourcePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
                if (relativePath.StartsWith(".electron2d/", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-EDITOR-METADATA", $"{projectId} exposes editor metadata as runtime resource: {relativePath}.", ToRepositoryPath(file)));
                }
            }
        }
    }

    private string? ResolveRepositoryPath(string relativePath, List<BuildDiagnostic> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Path.IsPathRooted(relativePath) ||
            Regex.IsMatch(relativePath, "^[a-zA-Z][a-zA-Z0-9+.-]*://", RegexOptions.CultureInvariant))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PATH", $"Repository-relative path is invalid: {relativePath}.", "data/quality/reference-game-platform-matrix.json"));
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(repositoryRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        if (!candidate.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error("E2D-BUILD-REFERENCE-MATRIX-PATH", $"Repository-relative path escapes repository root: {relativePath}.", "data/quality/reference-game-platform-matrix.json"));
            return null;
        }

        return candidate;
    }

    private static string[] GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
            .Select(item => item.GetString()!)
            .ToArray();
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify reference-game-platform-matrix", "error", code, message, Path: path);
    }
}

internal sealed class LeakChecksVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    private const string CycleTestName = "LeakVerificationTests.LeakVerificationCyclesReleaseSubsystemResourcesAndDoNotGrowMonotonically";
    private static readonly string[] RequiredScenarioIds =
    [
        "gpu-texture-render-target-cycles",
        "audio-voice-cycles",
        "physics-rid-cycles",
        "scene-load-unload-cycles"
    ];

    public async Task<int> VerifyAsync(CancellationToken cancellationToken)
    {
        var test = await processRunner.RunAsync(
            new ProcessRunRequest(
                "verify leak-checks focused test",
                "dotnet",
                [
                    "test",
                    Path.Combine("tests", "Electron2D.Tests.Integration", "Electron2D.Tests.Integration.csproj"),
                    "--filter",
                    "FullyQualifiedName~" + CycleTestName,
                    "--no-build",
                    "--no-restore",
                    "-v:minimal"
                ],
                repositoryRoot,
                TimeSpan.FromMinutes(5)),
            cancellationToken).ConfigureAwait(false);
        if (test.ExitCode != 0)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify leak-checks", "error", "E2D-BUILD-LEAK-CHECKS-TEST-FAILED", "Leak verification focused test failed.", ProcessExitCode: test.ExitCode, TimedOut: test.TimedOut));
            return test.ExitCode ?? RepositoryBuildExitCodes.Failed;
        }

        var errors = new List<BuildDiagnostic>();
        VerifyReport(errors);
        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        WritePlan();
        diagnostics.Write(new BuildDiagnostic("verify", "verify leak-checks", "info", "E2D-BUILD-LEAK-CHECKS-PASSED", "Leak verification passed.", OutputPath: ".temp/leak-verification/verification-plan.json"));
        return RepositoryBuildExitCodes.Success;
    }

    private void VerifyReport(List<BuildDiagnostic> errors)
    {
        var reportPath = Path.Combine(repositoryRoot, "data", "quality", "leak-verification-report.json");
        if (!File.Exists(reportPath))
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-REPORT-MISSING", "Leak verification report is missing.", "data/quality/leak-verification-report.json"));
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(reportPath, Encoding.UTF8));
        var report = document.RootElement;
        AssertString(report, "format", "Electron2D.LeakVerificationReport", "report", errors);
        AssertInt(report, "version", 1, "report", errors);
        AssertString(report, "release", "0.1.0-preview", "report", errors);

        if (!TryGetObject(report, "budgets", "report", errors, out var budgets))
        {
            return;
        }

        var minimumIterations = GetInt(budgets, "minimumIterations", "budgets", errors);
        var maxManagedGrowthBytes = GetInt64(budgets, "maxManagedGrowthBytes", "budgets", errors);
        var maxNativeHandleDelta = GetInt(budgets, "maxNativeHandleDelta", "budgets", errors);
        var maxActiveResourceCount = GetInt(budgets, "maxActiveResourceCount", "budgets", errors);
        var allowMonotonicGrowth = GetBoolean(budgets, "allowMonotonicGrowth", "budgets", errors);
        if (minimumIterations < 64)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-BUDGET", "Leak verification requires at least 64 iterations per scenario.", "data/quality/leak-verification-report.json"));
        }

        if (maxManagedGrowthBytes > 1_048_576 || maxNativeHandleDelta != 0 || maxActiveResourceCount != 0 || allowMonotonicGrowth)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-BUDGET", "Leak verification budgets are less strict than the leak contract.", "data/quality/leak-verification-report.json"));
        }

        if (!report.TryGetProperty("scenarios", out var scenariosElement) || scenariosElement.ValueKind != JsonValueKind.Array)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-SCENARIOS", "Leak verification report must contain a scenarios array.", "data/quality/leak-verification-report.json"));
            return;
        }

        var scenarios = scenariosElement.EnumerateArray().ToArray();
        var actualIds = scenarios.Select(scenario => GetString(scenario, "scenarioId", "scenario", errors))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (!actualIds.SequenceEqual(RequiredScenarioIds.Order(StringComparer.Ordinal), StringComparer.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-SCENARIO-SET", "Leak verification scenarios do not match the required set.", "data/quality/leak-verification-report.json"));
        }

        foreach (var scenario in scenarios)
        {
            VerifyScenario(scenario, minimumIterations, maxManagedGrowthBytes, maxNativeHandleDelta, maxActiveResourceCount, errors);
        }
    }

    private void VerifyScenario(JsonElement scenario, int minimumIterations, long maxManagedGrowthBytes, int maxNativeHandleDelta, int maxActiveResourceCount, List<BuildDiagnostic> errors)
    {
        var scenarioId = GetString(scenario, "scenarioId", "scenario", errors);
        var context = "scenario " + (string.IsNullOrWhiteSpace(scenarioId) ? "<unknown>" : scenarioId);
        var subsystem = GetString(scenario, "subsystem", context, errors);
        if (string.IsNullOrWhiteSpace(subsystem))
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-SUBSYSTEM", $"{context} subsystem must not be empty.", "data/quality/leak-verification-report.json"));
        }

        var iterations = GetInt(scenario, "iterations", context, errors);
        var managedGrowthBytes = GetInt64(scenario, "managedGrowthBytes", context, errors);
        var nativeHandleDelta = GetInt(scenario, "nativeHandleDelta", context, errors);
        var activeResourceCount = GetInt(scenario, "activeResourceCount", context, errors);
        var monotonicGrowthDetected = GetBoolean(scenario, "monotonicGrowthDetected", context, errors);
        if (iterations < minimumIterations)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-ITERATIONS", $"{context} has too few iterations.", "data/quality/leak-verification-report.json"));
        }

        if (managedGrowthBytes < 0 || managedGrowthBytes > maxManagedGrowthBytes)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-MANAGED-GROWTH", $"{context} managed growth exceeds budget.", "data/quality/leak-verification-report.json"));
        }

        if (nativeHandleDelta != maxNativeHandleDelta)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-NATIVE-HANDLES", $"{context} native handle delta must be 0.", "data/quality/leak-verification-report.json"));
        }

        if (activeResourceCount != maxActiveResourceCount)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-ACTIVE-RESOURCES", $"{context} active resource count must be 0.", "data/quality/leak-verification-report.json"));
        }

        if (monotonicGrowthDetected)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-MONOTONIC-GROWTH", $"{context} reports monotonic growth.", "data/quality/leak-verification-report.json"));
        }

        if (!scenario.TryGetProperty("evidence", out var evidence) || evidence.ValueKind != JsonValueKind.Array || !evidence.EnumerateArray().Any())
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-EVIDENCE", $"{context} must contain at least one evidence path.", "data/quality/leak-verification-report.json"));
            return;
        }

        foreach (var item in evidence.EnumerateArray())
        {
            var relativePath = item.GetString() ?? string.Empty;
            VerifyRepositoryPath(relativePath, context + " evidence", errors);
        }
    }

    private void VerifyRepositoryPath(string relativePath, string context, List<BuildDiagnostic> errors)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            Regex.IsMatch(relativePath, "^[a-zA-Z][a-zA-Z0-9+.-]*://", RegexOptions.CultureInvariant))
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-EVIDENCE-PATH", $"{context} path must be repository-local and use forward slashes: {relativePath}.", "data/quality/leak-verification-report.json"));
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(repositoryRoot);
        if (!root.EndsWith(Path.DirectorySeparatorChar))
        {
            root += Path.DirectorySeparatorChar;
        }

        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-EVIDENCE-PATH", $"{context} path does not exist or escapes repository root: {relativePath}.", "data/quality/leak-verification-report.json"));
        }
    }

    private void WritePlan()
    {
        var outputPath = Path.Combine(repositoryRoot, ".temp", "leak-verification", "verification-plan.json");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var plan = new
        {
            format = "Electron2D.LeakVerificationPlan",
            version = 1,
            release = "0.1.0-preview",
            focusedTest = CycleTestName,
            report = "data/quality/leak-verification-report.json",
            scenarios = RequiredScenarioIds
        };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine, Encoding.UTF8);
    }

    private static bool TryGetObject(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors, out JsonElement value)
    {
        if (element.TryGetProperty(propertyName, out value) && value.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} is missing object property: {propertyName}.", "data/quality/leak-verification-report.json"));
        return false;
    }

    private static void AssertString(JsonElement element, string propertyName, string expected, string context, List<BuildDiagnostic> errors)
    {
        var actual = GetString(element, propertyName, context, errors);
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} property {propertyName} must be {expected}.", "data/quality/leak-verification-report.json"));
        }
    }

    private static void AssertInt(JsonElement element, string propertyName, int expected, string context, List<BuildDiagnostic> errors)
    {
        var actual = GetInt(element, propertyName, context, errors);
        if (actual != expected)
        {
            errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} property {propertyName} must be {expected}.", "data/quality/leak-verification-report.json"));
        }
    }

    private static string GetString(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }

        errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} is missing string property: {propertyName}.", "data/quality/leak-verification-report.json"));
        return string.Empty;
    }

    private static int GetInt(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var result))
        {
            return result;
        }

        errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} is missing integer property: {propertyName}.", "data/quality/leak-verification-report.json"));
        return 0;
    }

    private static long GetInt64(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var result))
        {
            return result;
        }

        errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} is missing integer property: {propertyName}.", "data/quality/leak-verification-report.json"));
        return 0;
    }

    private static bool GetBoolean(JsonElement element, string propertyName, string context, List<BuildDiagnostic> errors)
    {
        if (element.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetBoolean();
        }

        errors.Add(Error("E2D-BUILD-LEAK-CHECKS-JSON", $"{context} is missing boolean property: {propertyName}.", "data/quality/leak-verification-report.json"));
        return false;
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify leak-checks", "error", code, message, Path: path);
    }
}

internal sealed class Box2DPhysicsCandidateVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    public async Task<int> VerifyAsync(string[] args, CancellationToken cancellationToken)
    {
        var parse = Parse(args);
        if (!parse.Succeeded)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify box2d-physics-candidate", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", parse.ErrorMessage));
            return RepositoryBuildExitCodes.Failed;
        }

        var errors = VerifyDocumentationAndProject();
        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var smokeProject = Path.Combine("tests", "Electron2D.Tests.PhysicsBox2DSmoke", "Electron2D.Tests.PhysicsBox2DSmoke.csproj");
        var jit = await processRunner.RunAsync(
            new ProcessRunRequest(
                "verify box2d-physics-candidate jit",
                "dotnet",
                ["run", "-c", "Release", "--project", smokeProject, "--", "--warmup-ticks", parse.WarmupTicks.ToString(System.Globalization.CultureInfo.InvariantCulture), "--ticks", parse.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)],
                repositoryRoot,
                TimeSpan.FromMinutes(5)),
            cancellationToken).ConfigureAwait(false);
        if (jit.ExitCode != 0)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify box2d-physics-candidate", "error", "E2D-BUILD-BOX2D-SMOKE-FAILED", "Box2D.NET Release/JIT smoke failed.", ProcessExitCode: jit.ExitCode, TimedOut: jit.TimedOut));
            return jit.ExitCode ?? RepositoryBuildExitCodes.Failed;
        }

        if (parse.NativeAot)
        {
            var rid = parse.RuntimeIdentifier ?? CurrentRuntimeIdentifier();
            var output = Path.Combine(".temp", "box2d-nativeaot", rid);
            var publish = await processRunner.RunAsync(
                new ProcessRunRequest(
                    "verify box2d-physics-candidate nativeaot publish",
                    "dotnet",
                    ["publish", smokeProject, "-c", "Release", "-r", rid, "-p:PublishAot=true", "--self-contained", "true", "-o", output],
                    repositoryRoot,
                    TimeSpan.FromMinutes(10)),
                cancellationToken).ConfigureAwait(false);
            if (publish.ExitCode != 0)
            {
                diagnostics.Write(new BuildDiagnostic("verify", "verify box2d-physics-candidate", "error", "E2D-BUILD-BOX2D-NATIVEAOT-PUBLISH-FAILED", "Box2D.NET NativeAOT publish failed.", ProcessExitCode: publish.ExitCode, TimedOut: publish.TimedOut, RuntimeIdentifier: rid));
                return publish.ExitCode ?? RepositoryBuildExitCodes.Failed;
            }

            var executable = Path.Combine(repositoryRoot, output, OperatingSystem.IsWindows() ? "Electron2D.Tests.PhysicsBox2DSmoke.exe" : "Electron2D.Tests.PhysicsBox2DSmoke");
            var native = await processRunner.RunAsync(
                new ProcessRunRequest(
                    "verify box2d-physics-candidate nativeaot run",
                    executable,
                    ["--warmup-ticks", parse.WarmupTicks.ToString(System.Globalization.CultureInfo.InvariantCulture), "--ticks", parse.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture)],
                    repositoryRoot,
                    TimeSpan.FromMinutes(5)),
                cancellationToken).ConfigureAwait(false);
            if (native.ExitCode != 0)
            {
                diagnostics.Write(new BuildDiagnostic("verify", "verify box2d-physics-candidate", "error", "E2D-BUILD-BOX2D-NATIVEAOT-RUN-FAILED", "Box2D.NET NativeAOT smoke failed.", ProcessExitCode: native.ExitCode, TimedOut: native.TimedOut, RuntimeIdentifier: rid));
                return native.ExitCode ?? RepositoryBuildExitCodes.Failed;
            }
        }

        diagnostics.Write(new BuildDiagnostic("verify", "verify box2d-physics-candidate", "info", "E2D-BUILD-BOX2D-PHYSICS-CANDIDATE-PASSED", "Box2D.NET physics candidate verification passed."));
        return RepositoryBuildExitCodes.Success;
    }

    private List<BuildDiagnostic> VerifyDocumentationAndProject()
    {
        var errors = new List<BuildDiagnostic>();
        var documentPath = Path.Combine(repositoryRoot, "docs", "physics", "box2d-net-validation.md");
        var projectPath = Path.Combine(repositoryRoot, "tests", "Electron2D.Tests.PhysicsBox2DSmoke", "Electron2D.Tests.PhysicsBox2DSmoke.csproj");
        if (!File.Exists(documentPath))
        {
            errors.Add(Error("E2D-BUILD-BOX2D-DOC-MISSING", "Box2D validation document is missing.", "docs/physics/box2d-net-validation.md"));
        }
        else
        {
            var text = File.ReadAllText(documentPath, Encoding.UTF8);
            foreach (var fragment in new[] { "Box2D.NET 3.1.654", "NativeAOT", "Mobile gaps", "AllocatedBytesPerTick" })
            {
                if (!text.Contains(fragment, StringComparison.Ordinal))
                {
                    errors.Add(Error("E2D-BUILD-BOX2D-DOC-FRAGMENT", $"Box2D validation document is missing fragment: {fragment}.", "docs/physics/box2d-net-validation.md"));
                }
            }
        }

        if (!File.Exists(projectPath))
        {
            errors.Add(Error("E2D-BUILD-BOX2D-PROJECT-MISSING", "Box2D smoke project is missing.", "tests/Electron2D.Tests.PhysicsBox2DSmoke/Electron2D.Tests.PhysicsBox2DSmoke.csproj"));
        }
        else if (!File.ReadAllText(projectPath, Encoding.UTF8).Contains("PackageReference Include=\"Box2D.NET\" Version=\"3.1.654\"", StringComparison.Ordinal))
        {
            errors.Add(Error("E2D-BUILD-BOX2D-PACKAGE", "Box2D smoke project must pin Box2D.NET 3.1.654.", "tests/Electron2D.Tests.PhysicsBox2DSmoke/Electron2D.Tests.PhysicsBox2DSmoke.csproj"));
        }

        var runtimeProject = Path.Combine(repositoryRoot, "src", "Electron2D", "Electron2D.csproj");
        if (File.Exists(runtimeProject) && File.ReadAllText(runtimeProject, Encoding.UTF8).Contains("Box2D.NET", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(Error("E2D-BUILD-BOX2D-RUNTIME-DEPENDENCY", "Runtime project must not reference Box2D.NET while it is only a candidate backend.", "src/Electron2D/Electron2D.csproj"));
        }

        return errors;
    }

    private static Box2DArguments Parse(string[] args)
    {
        var nativeAot = false;
        string? rid = null;
        var warmupTicks = 60;
        var ticks = 240;
        for (var i = 2; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--native-aot":
                    nativeAot = true;
                    break;
                case "--runtime" when i + 1 < args.Length:
                case "--runtime-identifier" when i + 1 < args.Length:
                    rid = args[++i];
                    break;
                case "--warmup-ticks" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedWarmup):
                    warmupTicks = parsedWarmup;
                    i++;
                    break;
                case "--ticks" when i + 1 < args.Length && int.TryParse(args[i + 1], out var parsedTicks):
                    ticks = parsedTicks;
                    i++;
                    break;
                default:
                    return new Box2DArguments(false, false, null, 0, 0, "Expected: verify box2d-physics-candidate [--native-aot] [--runtime <rid>] [--warmup-ticks <n>] [--ticks <n>].");
            }
        }

        return new Box2DArguments(true, nativeAot, rid, warmupTicks, ticks, string.Empty);
    }

    private static string CurrentRuntimeIdentifier()
    {
        if (OperatingSystem.IsWindows())
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "win-arm64" : "win-x64";
        }

        if (OperatingSystem.IsMacOS())
        {
            return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
        }

        return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify box2d-physics-candidate", "error", code, message, Path: path);
    }

    private sealed record Box2DArguments(bool Succeeded, bool NativeAot, string? RuntimeIdentifier, int WarmupTicks, int Ticks, string ErrorMessage);
}

internal sealed class PublicApiXmlDocsVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, ProcessRunner processRunner)
{
    public async Task<int> VerifyAsync(string[] args, CancellationToken cancellationToken)
    {
        if (args.Length is not 2 and not 3 || (args.Length == 3 && args[2] != "--fail-on-issues"))
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify public-api-xml-docs", "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", "Expected: verify public-api-xml-docs [--fail-on-issues]."));
            return RepositoryBuildExitCodes.Failed;
        }

        var failOnIssues = args.Length == 3;
        var build = await processRunner.RunAsync(
            new ProcessRunRequest(
                "verify public-api-xml-docs build",
                "dotnet",
                ["build", Path.Combine("src", "Electron2D", "Electron2D.csproj"), "-p:GenerateDocumentationFile=true", "-v:minimal"],
                repositoryRoot,
                TimeSpan.FromMinutes(5)),
            cancellationToken).ConfigureAwait(false);
        if (build.ExitCode != 0)
        {
            diagnostics.Write(new BuildDiagnostic("verify", "verify public-api-xml-docs", "error", "E2D-BUILD-PUBLIC-API-XML-BUILD-FAILED", "Runtime project build failed before XML documentation verification.", ProcessExitCode: build.ExitCode, TimedOut: build.TimedOut));
            return build.ExitCode ?? RepositoryBuildExitCodes.Failed;
        }

        var assemblyPath = Path.Combine(repositoryRoot, "src", "Electron2D", "bin", "Debug", "net10.0", "Electron2D.dll");
        var xmlPath = Path.Combine(repositoryRoot, "src", "Electron2D", "bin", "Debug", "net10.0", "Electron2D.xml");
        var errors = new List<BuildDiagnostic>();
        if (!File.Exists(assemblyPath))
        {
            errors.Add(Error("E2D-BUILD-PUBLIC-API-XML-ASSEMBLY", "Runtime assembly was not found after build.", ToRepositoryPath(assemblyPath)));
        }

        if (!File.Exists(xmlPath))
        {
            errors.Add(Error("E2D-BUILD-PUBLIC-API-XML-MISSING", "Runtime XML documentation file was not found after build.", ToRepositoryPath(xmlPath)));
        }

        if (errors.Count == 0)
        {
            VerifyPublicApiDocs(assemblyPath, xmlPath, errors);
        }

        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return failOnIssues ? RepositoryBuildExitCodes.Failed : RepositoryBuildExitCodes.Success;
        }

        diagnostics.Write(new BuildDiagnostic("verify", "verify public-api-xml-docs", "info", "E2D-BUILD-PUBLIC-API-XML-DOCS-PASSED", "Public API XML documentation verification passed."));
        return RepositoryBuildExitCodes.Success;
    }

    private void VerifyPublicApiDocs(string assemblyPath, string xmlPath, List<BuildDiagnostic> errors)
    {
        var docs = XDocument.Load(xmlPath).Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .ToDictionary(member => member.Attribute("name")!.Value, StringComparer.Ordinal);
        var assembly = Assembly.LoadFrom(assemblyPath);
        foreach (var type in assembly.GetExportedTypes()
            .Where(type => type.Namespace is not null && type.Namespace.StartsWith("Electron2D", StringComparison.Ordinal))
            .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            var typeId = TypeId(type);
            AssertDoc(docs, typeId, "type", requireThreadSafety: !type.IsEnum, errors);
            AssertTypeParameters(docs, typeId, type.GetGenericArguments(), errors);

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(field => !field.IsSpecialName)
                .OrderBy(field => field.Name, StringComparer.Ordinal))
            {
                var fieldId = "F:" + TypeName(type) + "." + field.Name;
                AssertDoc(docs, fieldId, field.IsLiteral && type.IsEnum ? "enum value" : "field", requireThreadSafety: false, errors);
            }

            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                var propertyId = "P:" + TypeName(type) + "." + property.Name;
                var element = AssertDoc(docs, propertyId, "property", requireThreadSafety: true, errors);
                AssertValue(propertyId, element, errors);
                AssertParameters(propertyId, element, property.GetIndexParameters(), errors);
            }

            foreach (var @event in type.GetEvents(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .OrderBy(@event => @event.Name, StringComparer.Ordinal))
            {
                AssertDoc(docs, "E:" + TypeName(type) + "." + @event.Name, "event", requireThreadSafety: true, errors);
            }

            foreach (var constructor in type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .OrderBy(constructor => constructor.ToString(), StringComparer.Ordinal))
            {
                var constructorId = MethodId(type, constructor, "#ctor");
                var element = AssertDoc(docs, constructorId, "constructor", requireThreadSafety: true, errors);
                AssertParameters(constructorId, element, constructor.GetParameters(), errors);
            }

            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(method => !method.IsSpecialName)
                .OrderBy(method => method.Name, StringComparer.Ordinal)
                .ThenBy(method => method.ToString(), StringComparer.Ordinal))
            {
                var methodId = MethodId(type, method, method.Name);
                var element = AssertDoc(docs, methodId, "method", requireThreadSafety: true, errors);
                AssertParameters(methodId, element, method.GetParameters(), errors);
                AssertTypeParameters(docs, methodId, method.GetGenericArguments(), errors);
                if (method.ReturnType != typeof(void))
                {
                    AssertReturns(methodId, element, errors);
                }
            }
        }

        foreach (var member in docs)
        {
            var text = NormalizeText(member.Value.Value);
            if (ContainsForbiddenPublicWording(text))
            {
                errors.Add(Issue("forbidden-wording", member.Key, "XML documentation contains forbidden public wording."));
            }

            if (ContainsPlaceholder(text))
            {
                errors.Add(Issue("placeholder", member.Key, "XML documentation contains TODO/TBD placeholder text."));
            }
        }
    }

    private static XElement? AssertDoc(
        IReadOnlyDictionary<string, XElement> docs,
        string id,
        string kind,
        bool requireThreadSafety,
        List<BuildDiagnostic> errors)
    {
        var element = FindMember(docs, id);
        if (element is null)
        {
            errors.Add(Issue("missing-doc", id, $"Missing XML documentation for public {kind}."));
            return null;
        }

        if (IsBlank(element.Element("summary")))
        {
            errors.Add(Issue("missing-summary", id, "Missing non-empty <summary>."));
        }

        if (requireThreadSafety && IsBlank(element.Element("threadsafety")))
        {
            errors.Add(Issue("missing-threadsafety", id, "Missing non-empty <threadsafety>."));
        }

        if (IsBlank(element.Element("since")))
        {
            errors.Add(Issue("missing-since", id, "Missing non-empty <since>."));
        }

        AssertSummaryShape(id, element.Element("summary"), errors);
        AssertOptionalTextElement(id, "remarks", element.Element("remarks"), errors);
        AssertOptionalTextElement(id, "threadsafety", element.Element("threadsafety"), errors);
        AssertOptionalTextElement(id, "since", element.Element("since"), errors);
        AssertSeeAlsoReferences(id, element, errors);
        AssertExceptionReferences(id, element, errors);
        AssertInheritDoc(id, element, errors);

        return element;
    }

    private static void AssertParameters(string id, XElement? element, ParameterInfo[] parameters, List<BuildDiagnostic> errors)
    {
        if (element is null)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            var documented = element.Elements("param")
                .Any(item => string.Equals(item.Attribute("name")?.Value, parameter.Name, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Value));
            if (!documented)
            {
                errors.Add(Issue("missing-param", id, $"Missing <param> for parameter '{parameter.Name}'."));
            }
        }
    }

    private static void AssertTypeParameters(IReadOnlyDictionary<string, XElement> docs, string id, Type[] genericArguments, List<BuildDiagnostic> errors)
    {
        var element = FindMember(docs, id);
        if (element is null)
        {
            return;
        }

        foreach (var argument in genericArguments)
        {
            var documented = element.Elements("typeparam")
                .Any(item => string.Equals(item.Attribute("name")?.Value, argument.Name, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.Value));
            if (!documented)
            {
                errors.Add(Issue("missing-typeparam", id, $"Missing <typeparam> for type parameter '{argument.Name}'."));
            }
        }
    }

    private static void AssertReturns(string id, XElement? element, List<BuildDiagnostic> errors)
    {
        if (element is not null && IsBlank(element.Element("returns")))
        {
            errors.Add(Issue("missing-returns", id, "Missing non-empty <returns>."));
        }

        AssertOptionalTextElement(id, "returns", element?.Element("returns"), errors);
    }

    private static void AssertValue(string id, XElement? element, List<BuildDiagnostic> errors)
    {
        if (element is not null && IsBlank(element.Element("value")))
        {
            errors.Add(Issue("missing-value", id, "Missing non-empty <value>."));
        }

        AssertOptionalTextElement(id, "value", element?.Element("value"), errors);
    }

    private static void AssertSummaryShape(string id, XElement? summary, List<BuildDiagnostic> errors)
    {
        if (summary is null)
        {
            return;
        }

        AssertOptionalTextElement(id, "summary", summary, errors);
        var normalized = NormalizeText(summary.Value);
        var sentenceCount = normalized.Split(". ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
        if (sentenceCount > 1 && !summary.Elements("para").Any())
        {
            errors.Add(Issue("summary-missing-para", id, "Multi-sentence <summary> must use <para> blocks."));
        }
    }

    private static void AssertOptionalTextElement(string id, string tagName, XElement? element, List<BuildDiagnostic> errors)
    {
        if (element is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(element.Value))
        {
            errors.Add(Issue("empty-" + tagName, id, $"Element <{tagName}> must not be empty when present."));
        }

        var text = NormalizeText(element.Value);
        if (ContainsForbiddenPublicWording(text))
        {
            errors.Add(Issue("forbidden-wording", id, $"Element <{tagName}> contains forbidden public wording."));
        }

        if (ContainsPlaceholder(text))
        {
            errors.Add(Issue("placeholder", id, $"Element <{tagName}> contains TODO/TBD placeholder text."));
        }
    }

    private static void AssertSeeAlsoReferences(string id, XElement element, List<BuildDiagnostic> errors)
    {
        foreach (var seeAlso in element.Elements("seealso"))
        {
            var cref = seeAlso.Attribute("cref")?.Value;
            var href = seeAlso.Attribute("href")?.Value;
            if (string.IsNullOrWhiteSpace(cref) && string.IsNullOrWhiteSpace(href))
            {
                errors.Add(Issue("missing-seealso-reference", id, "<seealso> must include a cref or href target."));
            }
        }
    }

    private static void AssertExceptionReferences(string id, XElement element, List<BuildDiagnostic> errors)
    {
        foreach (var exception in element.Elements("exception"))
        {
            if (string.IsNullOrWhiteSpace(exception.Attribute("cref")?.Value))
            {
                errors.Add(Issue("missing-exception-cref", id, "<exception> must include a cref target."));
            }

            if (string.IsNullOrWhiteSpace(exception.Value))
            {
                errors.Add(Issue("empty-exception", id, "<exception> must explain when the exception is thrown."));
            }
        }
    }

    private static void AssertInheritDoc(string id, XElement element, List<BuildDiagnostic> errors)
    {
        if (element.Descendants("inheritdoc").Any() || element.Elements("inheritdoc").Any())
        {
            errors.Add(Issue("inheritdoc", id, "Public API documentation must not rely on bare <inheritdoc /> in generated XML output."));
        }
    }

    private static XElement? FindMember(IReadOnlyDictionary<string, XElement> docs, string id)
    {
        if (docs.TryGetValue(id, out var exact))
        {
            return exact;
        }

        var parameterIndex = id.IndexOf('(', StringComparison.Ordinal);
        if (parameterIndex > 0)
        {
            var prefix = id[..parameterIndex];
            return docs.FirstOrDefault(item => item.Key.StartsWith(prefix + "(", StringComparison.Ordinal)).Value;
        }

        return docs.FirstOrDefault(item => item.Key.StartsWith(id + "(", StringComparison.Ordinal)).Value;
    }

    private static string TypeId(Type type)
    {
        return "T:" + TypeName(type);
    }

    private static string TypeName(Type type)
    {
        var name = type.FullName ?? type.Name;
        return name.Replace('+', '.');
    }

    private static string MethodId(Type declaringType, MethodBase method, string methodName)
    {
        var name = methodName;
        if (method.IsGenericMethod)
        {
            name += "``" + method.GetGenericArguments().Length.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var parameters = method.GetParameters();
        var id = "M:" + TypeName(declaringType) + "." + name;
        if (parameters.Length == 0)
        {
            return id;
        }

        return id + "(" + string.Join(",", parameters.Select(parameter => ParameterTypeName(parameter.ParameterType))) + ")";
    }

    private static string ParameterTypeName(Type type)
    {
        if (type.IsByRef)
        {
            return ParameterTypeName(type.GetElementType()!) + "@";
        }

        if (type.IsArray)
        {
            return ParameterTypeName(type.GetElementType()!) + "[]";
        }

        if (type.IsGenericParameter)
        {
            return "`" + type.GenericParameterPosition.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        if (type.IsGenericType)
        {
            var definitionName = (type.GetGenericTypeDefinition().FullName ?? type.Name).Replace('+', '.');
            var tickIndex = definitionName.IndexOf('`', StringComparison.Ordinal);
            if (tickIndex >= 0)
            {
                definitionName = definitionName[..tickIndex];
            }

            return definitionName + "{" + string.Join(",", type.GetGenericArguments().Select(ParameterTypeName)) + "}";
        }

        return (type.FullName ?? type.Name).Replace('+', '.');
    }

    private static bool IsBlank(XElement? element)
    {
        return element is null || string.IsNullOrWhiteSpace(element.Value);
    }

    private static string NormalizeText(string value)
    {
        return value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
    }

    private static bool ContainsPlaceholder(string value)
    {
        return value.Contains("TODO", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("TBD", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsForbiddenPublicWording(string value)
    {
        return value.Contains("SDL", StringComparison.Ordinal) ||
            value.Contains("SDL3", StringComparison.Ordinal) ||
            value.Contains("SDL_GPU", StringComparison.Ordinal) ||
            value.Contains("SDL_Renderer", StringComparison.Ordinal) ||
            value.Contains("SDL_ttf", StringComparison.Ordinal) ||
            value.Contains("SDL_mixer", StringComparison.Ordinal) ||
            value.Contains("SDL_shadercross", StringComparison.Ordinal) ||
            value.Contains("Simple DirectMedia", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Godot-like", StringComparison.OrdinalIgnoreCase) ||
            value.Contains("Godot-подоб", StringComparison.OrdinalIgnoreCase);
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static BuildDiagnostic Issue(string issueCode, string symbol, string message)
    {
        var diagnosticCode = "E2D-BUILD-PUBLIC-API-XML-" + issueCode.Replace('-', '_').ToUpperInvariant();
        return Error(diagnosticCode, $"{issueCode}: {symbol} - {message}", "src/Electron2D");
    }

    private static BuildDiagnostic Error(string code, string message, string path)
    {
        return new BuildDiagnostic("verify", "verify public-api-xml-docs", "error", code, message, Path: path);
    }
}

internal sealed class PublicApiWikiVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics, PublicApiXmlDocsVerifier xmlDocsVerifier)
{
    public int VerifyUiPublicApiGate(string[] args)
    {
        var wikiPath = ParseWikiPath(args, "verify ui-public-api-gate");
        if (wikiPath is null)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var errors = new List<BuildDiagnostic>();
        var uiPath = Path.Combine(wikiPath, "API-UI-and-Text.md");
        var compatibilityPath = Path.Combine(wikiPath, "API-Compatibility.md");
        if (!File.Exists(uiPath))
        {
            errors.Add(Error("verify ui-public-api-gate", "E2D-BUILD-UI-PUBLIC-API-WIKI-MISSING", "GitHub Wiki API-UI-and-Text.md page is missing.", ToRepositoryPath(uiPath)));
        }

        if (!File.Exists(compatibilityPath))
        {
            errors.Add(Error("verify ui-public-api-gate", "E2D-BUILD-UI-PUBLIC-API-COMPATIBILITY-MISSING", "GitHub Wiki API-Compatibility.md page is missing.", ToRepositoryPath(compatibilityPath)));
        }

        if (errors.Count == 0)
        {
            var uiApiNames = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(uiPath, Encoding.UTF8))
            {
                var match = Regex.Match(line, @"^\|\s*\[([^\]]+)\]\(([^)]+)\)\s*\|\s*(class|struct|enum|interface|delegate)\s*\|", RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    uiApiNames.Add("Electron2D." + match.Groups[1].Value.Trim().Replace('.', '+'));
                }
            }

            if (uiApiNames.Count == 0)
            {
                errors.Add(Error("verify ui-public-api-gate", "E2D-BUILD-UI-PUBLIC-API-EMPTY", "Generated UI and Text Wiki category page does not contain public type rows.", ToRepositoryPath(uiPath)));
            }

            var statusByApi = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var line in File.ReadLines(compatibilityPath, Encoding.UTF8))
            {
                var match = Regex.Match(line, @"^\|\s*`([^`]+)`\s*\|\s*[^|]+\|\s*([A-Za-z]+)\s*\|", RegexOptions.CultureInvariant);
                if (match.Success)
                {
                    statusByApi[match.Groups[1].Value.Trim()] = match.Groups[2].Value.Trim();
                }
            }

            foreach (var apiName in uiApiNames)
            {
                if (!statusByApi.TryGetValue(apiName, out var status))
                {
                    errors.Add(Error("verify ui-public-api-gate", "E2D-BUILD-UI-PUBLIC-API-MISSING", $"UI/Text API row is missing from compatibility table: {apiName}.", "API-Compatibility.md"));
                }
                else if (!string.Equals(status, "Supported", StringComparison.Ordinal))
                {
                    errors.Add(Error("verify ui-public-api-gate", "E2D-BUILD-UI-PUBLIC-API-NOT-SUPPORTED", $"UI/Text API type is not marked Supported in compatibility table: {apiName} ({status}).", "API-Compatibility.md"));
                }
            }
        }

        return Complete("verify ui-public-api-gate", "E2D-BUILD-UI-PUBLIC-API-GATE-PASSED", "UI public API gate verification passed.", errors);
    }

    public async Task<int> VerifyPublicApiDocumentationAsync(string[] args, CancellationToken cancellationToken)
    {
        var wikiPath = ParseWikiPath(args, "verify public-api-documentation");
        if (wikiPath is null)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        var xml = await xmlDocsVerifier.VerifyAsync(["verify", "public-api-xml-docs", "--fail-on-issues"], cancellationToken).ConfigureAwait(false);
        if (xml != RepositoryBuildExitCodes.Success)
        {
            return xml;
        }

        var errors = new List<BuildDiagnostic>();
        foreach (var requiredPage in new[] { "Home.md", "_Sidebar.md", "_Footer.md", "API-by-Category.md", "API-Reference.md", "API-Compatibility.md" })
        {
            var path = Path.Combine(wikiPath, requiredPage);
            if (!File.Exists(path))
            {
                errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-WIKI-MISSING", $"Required GitHub Wiki page is missing: {requiredPage}.", ToRepositoryPath(path)));
            }
        }

        if (Directory.Exists(wikiPath))
        {
            foreach (var file in Directory.EnumerateFiles(wikiPath, "*.md", SearchOption.TopDirectoryOnly))
            {
                var content = File.ReadAllText(file, Encoding.UTF8);
                if (Regex.IsMatch(content, @"\]\([^)\s]+\.md(?:#[^)]+)?\)", RegexOptions.CultureInvariant))
                {
                    errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-WIKI-MD-LINK", "GitHub Wiki links must use page names without .md extensions.", ToRepositoryPath(file)));
                }
            }
        }

        AssertWikiContent(
            wikiPath,
            "Home.md",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [@"\[API by Category\]\(API-by-Category\)"] = "Home links to category API navigation.",
                [@"\[Complete API Index\]\(API-Reference\)"] = "Home links to the complete API index.",
                [@"\[API Compatibility\]\(API-Compatibility\)"] = "Home links to compatibility status."
            },
            new Dictionary<string, string>(StringComparer.Ordinal),
            errors);
        AssertWikiContent(
            wikiPath,
            "API-Reference.md",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [@"\[Home\]\(Home\) \| \[API by Category\]\(API-by-Category\) \| \[Complete API Index\]\(API-Reference\) \| \[API Compatibility\]\(API-Compatibility\)"] = "complete top navigation.",
                ["## Type Index"] = "complete public type index."
            },
            new Dictionary<string, string>(StringComparer.Ordinal),
            errors);
        AssertWikiContent(
            wikiPath,
            "API-Compatibility.md",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [@"\[Home\]\(Home\) \| \[API by Category\]\(API-by-Category\) \| \[Complete API Index\]\(API-Reference\) \| \[API Compatibility\]\(API-Compatibility\)"] = "complete top navigation.",
                ["## Status Legend"] = "status legend.",
                ["## Current Public Runtime Surface"] = "current public API status table.",
                ["## Planned 2D Surface"] = "planned preview surface table.",
                [@"\| Supported \| Implemented, tested and documented \|"] = "supported status definition.",
                [@"\| Partial \| Implemented only for the described subset \|"] = "partial status definition.",
                [@"\| Experimental \| Implemented but allowed to change before stable release \|"] = "experimental status definition.",
                [@"\| Planned \| Required by `0\.1\.0 Preview`, not implemented yet \|"] = "planned status definition."
            },
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [@"explicitly[- ]not[- ]planned|removed legacy|legacy API"] = "removed/legacy API block must not be published."
            },
            errors);
        AssertPublicDocumentationWording(wikiPath, errors);
        AssertPublicDocumentationWording(Path.Combine(repositoryRoot, "docs", "documentation"), errors);

        return Complete("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCUMENTATION-PASSED", "Public API documentation verification passed.", errors);
    }

    private void AssertWikiContent(
        string wikiPath,
        string pageName,
        IReadOnlyDictionary<string, string> requiredPatterns,
        IReadOnlyDictionary<string, string> forbiddenPatterns,
        List<BuildDiagnostic> errors)
    {
        var path = Path.Combine(wikiPath, pageName);
        if (!File.Exists(path))
        {
            return;
        }

        var content = File.ReadAllText(path, Encoding.UTF8);
        foreach (var required in requiredPatterns)
        {
            if (!Regex.IsMatch(content, required.Key, RegexOptions.CultureInvariant))
            {
                errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-WIKI-STRUCTURE", $"{pageName} is missing required Wiki structure: {required.Value}", ToRepositoryPath(path)));
            }
        }

        foreach (var forbidden in forbiddenPatterns)
        {
            if (Regex.IsMatch(content, forbidden.Key, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-WIKI-FORBIDDEN", $"{pageName} contains forbidden Wiki structure: {forbidden.Value}", ToRepositoryPath(path)));
            }
        }
    }

    private void AssertPublicDocumentationWording(string root, List<BuildDiagnostic> errors)
    {
        if (!Directory.Exists(root))
        {
            errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-ROOT-MISSING", $"Public API documentation audit root was not found: {ToRepositoryPath(root)}.", ToRepositoryPath(root)));
            return;
        }

        var forbiddenPatterns = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [@"\bSDL\b"] = "SDL family name",
            ["SDL3"] = "SDL family name",
            ["SDL_GPU"] = "backend library name",
            ["SDL_Renderer"] = "backend library name",
            ["SDL_ttf"] = "backend library name",
            ["SDL_mixer"] = "backend library name",
            ["SDL_shadercross"] = "backend library name",
            ["Simple DirectMedia"] = "backend library name",
            ["Godot-like"] = "promotional Godot comparison",
            ["Godot-подоб"] = "promotional Godot comparison"
        };

        foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(file => !Path.GetFileName(file).Equals("README.md", StringComparison.OrdinalIgnoreCase) &&
                !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains(".git")))
        {
            var content = File.ReadAllText(file, Encoding.UTF8);
            foreach (var forbidden in forbiddenPatterns)
            {
                if (Regex.IsMatch(content, forbidden.Key, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                {
                    errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-FORBIDDEN-WORDING", $"Public API documentation contains forbidden wording: {forbidden.Value} / {forbidden.Key}.", ToRepositoryPath(file)));
                }
            }

            if (Regex.IsMatch(content, @"\b(todo|tbd)\b", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
            {
                errors.Add(Error("verify public-api-documentation", "E2D-BUILD-PUBLIC-API-DOCS-PLACEHOLDER", "Public API documentation contains TODO/TBD placeholder text.", ToRepositoryPath(file)));
            }
        }
    }

    private string? ParseWikiPath(string[] args, string step)
    {
        if (args.Length == 4 && args[2] == "--wiki-path" && !string.IsNullOrWhiteSpace(args[3]))
        {
            return Path.IsPathRooted(args[3])
                ? Path.GetFullPath(args[3])
                : Path.GetFullPath(Path.Combine(repositoryRoot, args[3].Replace('/', Path.DirectorySeparatorChar)));
        }

        diagnostics.Write(new BuildDiagnostic("verify", step, "error", "E2D-BUILD-CLI-INVALID-ARGUMENTS", $"Expected: {step} --wiki-path <path>."));
        return null;
    }

    private int Complete(string step, string successCode, string successMessage, List<BuildDiagnostic> errors)
    {
        foreach (var error in errors)
        {
            diagnostics.Write(error);
        }

        if (errors.Count > 0)
        {
            return RepositoryBuildExitCodes.Failed;
        }

        diagnostics.Write(new BuildDiagnostic("verify", step, "info", successCode, successMessage));
        return RepositoryBuildExitCodes.Success;
    }

    private string ToRepositoryPath(string path)
    {
        return Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');
    }

    private static BuildDiagnostic Error(string step, string code, string message, string path)
    {
        return new BuildDiagnostic("verify", step, "error", code, message, Path: path);
    }
}
