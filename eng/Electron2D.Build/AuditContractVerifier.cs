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
using System.Text;

namespace Electron2D.Build;

internal sealed class AuditContractVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private const string Step = "verify audit-contracts";
    private static readonly string[] AuditRequestRequiredMarkers =
    [
        "VERDICT: ACCEPT",
        "VERDICT: NEEDS_FIXES",
        "TASK_ASSESSMENT",
        "BLOCKERS",
        "EVIDENCE_REVIEW",
        "RISKS_AND_NOTES",
        "CLOSURE_DECISION",
        "metadata.taskId",
        "metadata.iteration",
        "metadata.scopeTaskIds",
        "metadata.scopeSummary",
        "combined scope",
        "metadata.previousVerdictChain",
        "metadata.blockerClosureList",
        "previous verdict files",
        "verbatim preservation",
        "previous blockers closure",
        "metadata/repo-file-snapshots.json",
        "repo-after/",
        "repo-before/",
        "implementation content review",
        "test coverage review",
        "documentation review",
        "task compliance review",
        "secret scanning",
        "scope scanning",
        "evidence gap",
        "patch-only inspection",
        "single final report",
        "no intermediate VERDICT",
        "full current-scope engineering review",
        "FOLLOW_UP_FINDING",
        "OUT_OF_SCOPE_NOTE",
        "ACCEPTED_RISK",
        "INFO_NOTE",
        "global safety blocker",
        "architecture coherence"
    ];

    public int Verify()
    {
        var stopwatch = Stopwatch.StartNew();
        var checks = new List<AuditContractCheckResult>();
        var documents = ReadDocuments(checks);

        Check(
            checks,
            "audit-request-required-markers",
            documents.AuditRequestPath,
            documents.AuditRequestText is not null &&
                AuditRequestRequiredMarkers.All(marker => documents.AuditRequestText.Contains(marker, StringComparison.Ordinal)),
            "External audit request must keep all required final-report and package-inspection markers.");
        CheckContainsAll(
            checks,
            "audit-request-full-current-scope-review",
            documents.AuditRequestPath,
            documents.AuditRequestText,
            ["full current-scope engineering review", "primary audit", "control audit"]);
        CheckContainsAll(
            checks,
            "audit-request-risk-classes",
            documents.AuditRequestPath,
            documents.AuditRequestText,
            ["current-task blocker", "follow-up finding", "global safety blocker"]);

        CheckContainsAll(
            checks,
            "domain-fast-medium-heavy",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["`Fast`", "`Medium`", "`Heavy`"]);
        CheckContainsAll(
            checks,
            "domain-fast-command",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["verify audit-contracts", "AuditTier=Fast", "30 секунд"]);
        CheckContainsAll(
            checks,
            "domain-fast-forbids-heavy-work",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["не создаёт audit ZIP", "не создаёт чистую копию репозитория", "не вызывает дочерние `dotnet run`"]);
        CheckContainsAll(
            checks,
            "domain-stop-loss",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["Правило остановки", "failure class", "не увеличивать `rNN`"]);

        CheckContainsAll(
            checks,
            "goal-loop-fast-command",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["verify audit-contracts", "Fast", "Medium", "Heavy"]);
        CheckContainsAll(
            checks,
            "goal-loop-numeric-diary-line",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["Fast: 3 теста / 1m13s", "Medium: 4 теста / 3m18s", "Heavy: не запускался"]);
        CheckContainsAll(
            checks,
            "goal-loop-stop-loss",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["Два повтора failure class", "не увеличивай `rNN`"]);

        CheckContainsAll(
            checks,
            "agents-audit-package-command",
            documents.AgentsPath,
            documents.AgentsText,
            ["audit package", "audit package verify", "audit submit"]);
        CheckContainsAll(
            checks,
            "agents-no-manual-audit-zip",
            documents.AgentsPath,
            documents.AgentsText,
            ["Do not assemble, edit, repair, or verify audit ZIP files manually"]);
        CheckContainsAll(
            checks,
            "agents-pre-package-focused-checks",
            documents.AgentsPath,
            documents.AgentsText,
            ["Before `audit package`", "focused", "--no-build", "--no-restore"]);

        CheckContainsAll(
            checks,
            "test-command-audit-tier-slice",
            documents.TestCommandPath,
            documents.TestCommandText,
            ["AuditTier(AuditTierMedium)", "AuditTier(AuditTierHeavy)", "NotAuditTier(AuditTierFast)", "IntegrationSliceAuditPackage", "IntegrationSliceAuditMedium", "IntegrationSliceAuditHeavy"]);
        CheckContainsAll(
            checks,
            "integration-audit-tier-guards",
            documents.IntegrationTestsPath,
            documents.IntegrationTestsText,
            [
                "AuditWorkflowAuditTestsDeclareAuditTierTraits",
                "AuditWorkflowFastAuditTierDoesNotUseHeavyHelpers",
                "Trait(\"AuditTier\", \"Fast\")",
                "Trait(\"AuditTier\", \"Medium\")",
                "Trait(\"AuditTier\", \"Heavy\")"
            ]);

        VerifyFollowupParser(checks);
        VerifyFinalReportExtractor(checks);

        stopwatch.Stop();
        var failed = checks.Count(check => !check.Passed && !check.Skipped);
        foreach (var check in checks.Where(check => !check.Passed && !check.Skipped))
        {
            diagnostics.Write(new BuildDiagnostic(
                "verify",
                Step,
                "error",
                "E2D-BUILD-AUDIT-CONTRACT-FAILED",
                $"{check.Name}: {check.Message}",
                Path: check.Path));
        }

        var passed = checks.Count(check => check.Passed);
        var skipped = checks.Count(check => check.Skipped);
        var summaryCode = failed == 0
            ? "E2D-BUILD-AUDIT-CONTRACTS-PASSED"
            : "E2D-BUILD-AUDIT-CONTRACTS-FAILED";
        diagnostics.Write(new BuildDiagnostic(
            "verify",
            Step,
            failed == 0 ? "info" : "error",
            summaryCode,
            $"Audit contract fast verification {(failed == 0 ? "passed" : "failed")}. AuditTier=Fast; checks={checks.Count}; passed={passed}; failed={failed}; skipped={skipped}; elapsed={FormatDuration(stopwatch.Elapsed)}; budget=30s; Heavy: not-run."));

        return failed == 0 ? RepositoryBuildExitCodes.Success : RepositoryBuildExitCodes.Failed;
    }

    private AuditContractDocuments ReadDocuments(List<AuditContractCheckResult> checks)
    {
        var auditRequestPath = "docs/release-management/AUDIT-REQUEST.md";
        var domainDocumentPath = "docs/release-management/audit-package.md";
        var goalLoopPath = ".codex/prompts/goal-task-loop.md";
        var agentsPath = "AGENTS.md";
        var testCommandPath = "eng/Electron2D.Build/TestCommand.cs";
        var integrationTestsPath = "tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs";
        return new AuditContractDocuments(
            auditRequestPath,
            ReadRequiredText(checks, auditRequestPath),
            domainDocumentPath,
            ReadRequiredText(checks, domainDocumentPath),
            goalLoopPath,
            ReadRequiredText(checks, goalLoopPath),
            agentsPath,
            ReadRequiredText(checks, agentsPath),
            testCommandPath,
            ReadRequiredText(checks, testCommandPath),
            integrationTestsPath,
            ReadRequiredText(checks, integrationTestsPath));
    }

    private string? ReadRequiredText(List<AuditContractCheckResult> checks, string relativePath)
    {
        var fullPath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var exists = File.Exists(fullPath);
        Check(
            checks,
            $"file-exists:{relativePath}",
            relativePath,
            exists,
            $"Required audit contract file is missing: {relativePath}");
        if (!exists)
        {
            return null;
        }

        var text = File.ReadAllText(fullPath, Encoding.UTF8);
        Check(
            checks,
            $"file-not-empty:{relativePath}",
            relativePath,
            !string.IsNullOrWhiteSpace(text),
            $"Required audit contract file is empty: {relativePath}");
        return text;
    }

    private static void VerifyFollowupParser(List<AuditContractCheckResult> checks)
    {
        const string report = """
        VERDICT: ACCEPT

        TASK_ASSESSMENT:
        Checked.

        BLOCKERS:
        No blockers found.

        EVIDENCE_REVIEW:
        Evidence checked.

        RISKS_AND_NOTES:
        - FOLLOW_UP_FINDING F1
          - Problem: Must be actionable.
        - OUT_OF_SCOPE_NOTE N1
          - Problem: Informational note is not actionable by default.
        - INFO_NOTE I1 [actionable]
          - Problem: Explicit marker must make the note actionable.

        CLOSURE_DECISION:
        Task can be closed.
        """;

        var findings = AuditFollowupReportParser.ExtractActionableFindings(
            "docs/verdicts/release-management/t-0001-audit-r01.md",
            report);
        Check(
            checks,
            "followup-parser-finds-actionable-risk-classes",
            null,
            findings.Count == 2 &&
                findings.Any(finding => finding.Kind == "FOLLOW_UP_FINDING" && finding.FindingId == "F1") &&
                findings.Any(finding => finding.Kind == "INFO_NOTE" && finding.FindingId == "I1"),
            "Follow-up parser must detect default and explicit actionable report notes.");
        Check(
            checks,
            "followup-parser-ignores-passive-out-of-scope-note",
            null,
            findings.All(finding => finding.FindingId != "N1"),
            "Follow-up parser must ignore passive out-of-scope notes.");
    }

    private static void VerifyFinalReportExtractor(List<AuditContractCheckResult> checks)
    {
        const string validReport = """
        VERDICT: ACCEPT

        TASK_ASSESSMENT:
        Checked.

        BLOCKERS:
        No blockers found.

        EVIDENCE_REVIEW:
        Evidence checked.

        RISKS_AND_NOTES:
        None.

        CLOSURE_DECISION:
        Task can be closed.
        """;
        const string invalidReport = """
        VERDICT: ACCEPT

        TASK_ASSESSMENT:
        Checked.

        BLOCKERS:
        B1: blocker.

        EVIDENCE_REVIEW:
        Evidence checked.

        RISKS_AND_NOTES:
        None.

        CLOSURE_DECISION:
        Do not close.
        """;

        var valid = AuditSubmitReportExtractor.Extract(
            [new AuditSubmitReportCandidate(validReport, AuditSubmitReportCandidateSource.OpenedReportCard)]);
        var invalid = AuditSubmitReportExtractor.Extract(
            [new AuditSubmitReportCandidate(invalidReport, AuditSubmitReportCandidateSource.OpenedReportCard)]);
        Check(
            checks,
            "final-report-extractor-accepts-strict-accept",
            null,
            valid.Ready && valid.Report is not null,
            "Final report extractor must accept a strict ACCEPT report with required headings.");
        Check(
            checks,
            "final-report-extractor-rejects-accept-with-blocker",
            null,
            !invalid.Ready && invalid.FailureReason?.Contains("numbered blocker", StringComparison.OrdinalIgnoreCase) == true,
            "Final report extractor must reject ACCEPT reports that still contain numbered blockers.");
    }

    private static void CheckContainsAll(
        List<AuditContractCheckResult> checks,
        string name,
        string? path,
        string? text,
        string[] requiredMarkers)
    {
        var missing = text is null
            ? requiredMarkers
            : requiredMarkers.Where(marker => !text.Contains(marker, StringComparison.Ordinal)).ToArray();
        Check(
            checks,
            name,
            path,
            missing.Length == 0,
            missing.Length == 0
                ? "Required markers are present."
                : $"Missing required markers: {string.Join(", ", missing)}.");
    }

    private static void Check(
        List<AuditContractCheckResult> checks,
        string name,
        string? path,
        bool passed,
        string message)
    {
        checks.Add(new AuditContractCheckResult(name, path, passed, Skipped: false, message));
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds < 1
            ? $"{Math.Max(1, (int)Math.Round(duration.TotalMilliseconds))}ms"
            : duration.ToString(@"m\:ss\.fff");
    }
}

internal sealed record AuditContractDocuments(
    string AuditRequestPath,
    string? AuditRequestText,
    string DomainDocumentPath,
    string? DomainDocumentText,
    string GoalLoopPath,
    string? GoalLoopText,
    string AgentsPath,
    string? AgentsText,
    string TestCommandPath,
    string? TestCommandText,
    string IntegrationTestsPath,
    string? IntegrationTestsText);

internal sealed record AuditContractCheckResult(
    string Name,
    string? Path,
    bool Passed,
    bool Skipped,
    string Message);
