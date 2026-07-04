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
        "evidence-backed blocker",
        "blocker disproof",
        "unsupported concern",
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
            ["current-task blocker", "follow-up finding", "global safety blocker", "unsupported concern"]);
        CheckContainsAll(
            checks,
            "audit-request-evidence-backed-blockers",
            documents.AuditRequestPath,
            documents.AuditRequestText,
            ["Правило доказанного blocker-а", "Проверка опровержения", "evidence-backed blocker", "blocker disproof"]);

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
            "domain-audit-loop-stabilization",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["audit-loop-stabilization", "metadata.previousVerdictChain", "metadata.blockerClosureList", "checks[].name"]);
        CheckContainsAll(
            checks,
            "domain-previous-blocker-closure-coverage",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["путь прошлого отчёта", "идентификатор blocker-а", "configured check", "каждый найденный blocker"]);
        CheckContainsAll(
            checks,
            "domain-previous-blocker-closure-matrix",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            ["матрицу закрытия прошлых blocker-ов", "путь отчёта", "blocker id", "configured check"]);
        CheckContainsAll(
            checks,
            "request-previous-blocker-closure-coverage",
            documents.AuditRequestPath,
            documents.AuditRequestText,
            ["путь прошлого отчёта", "идентификатор blocker-а", "каждого найденного blocker-а", "картой проверки закрытий"]);

        CheckContainsAll(
            checks,
            "goal-prompt-short-entrypoint",
            documents.GoalPromptPath,
            documents.GoalPromptText,
            ["goal-task-workflow.md", "VERDICT: ACCEPT", "RISKS_AND_NOTES", "verify audit-followups", "git status", "Conventional Commit"]);
        Check(
            checks,
            "goal-prompt-stays-compact",
            documents.GoalPromptPath,
            documents.GoalPromptText is not null && documents.GoalPromptText.Length <= 3500,
            "Active goal prompt must stay at or below 3500 characters; keep detailed workflow in .codex/prompts/goal-task-workflow.md.");
        Check(
            checks,
            "goal-prompt-does-not-duplicate-workflow",
            documents.GoalPromptPath,
            documents.GoalPromptText is not null &&
                !new[]
                {
                    "PREFLIGHT:",
                    "WORKER:",
                    "CHECKS:",
                    "LOCAL LOOP:",
                    "PACKAGE:",
                    "VERIFY:",
                    "SUBMIT:",
                    "ACCEPT/DONE:"
                }.Any(marker => documents.GoalPromptText.Contains(marker, StringComparison.Ordinal)),
            "Active goal prompt must not duplicate workflow sections from .codex/prompts/goal-task-workflow.md.");
        CheckContainsAll(
            checks,
            "goal-prompt-gitignore-exception",
            documents.GitIgnorePath,
            documents.GitIgnoreText,
            ["!.codex/prompts/goal-prompt.md", "!.codex/prompts/goal-task-workflow.md"]);
        CheckContainsAll(
            checks,
            "goal-workflow-fast-command",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["verify audit-contracts", "fast path", "focused integration tests"]);
        CheckContainsAll(
            checks,
            "goal-workflow-numeric-diary-line",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["Fast: 3 теста / 1m13s", "Medium: 4 теста / 3m18s", "Heavy: не запускался"]);
        CheckContainsAll(
            checks,
            "goal-workflow-delegates-audit-details",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["docs/release-management/audit-package.md", "audit-loop-stabilization", "previousVerdictChain", "blockerClosureList"]);
        CheckContainsAll(
            checks,
            "goal-workflow-points-to-active-prompt",
            documents.GoalLoopPath,
            documents.GoalLoopText,
            ["Goal Task Workflow", ".codex/prompts/goal-prompt.md", "лимит 3500 символов"]);
        Check(
            checks,
            "goal-workflow-does-not-duplicate-agent-workflow",
            documents.GoalLoopPath,
            documents.GoalLoopText is not null &&
                !new[]
                {
                    "## Docker",
                    "## Worktree",
                    "## Feature Gate",
                    "## Development Diary",
                    "public C#",
                    "Manual ZIP assembly"
                }.Any(marker => documents.GoalLoopText.Contains(marker, StringComparison.OrdinalIgnoreCase)),
            "Goal workflow must only orchestrate the goal loop and must not duplicate AGENTS.md or docs/repository/agent-workflow.md.");

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
            ["Manual ZIP assembly", "manual submit", "manual page verdict reading"]);
        CheckContainsAll(
            checks,
            "agents-pre-package-focused-checks",
            documents.AgentsPath,
            documents.AgentsText,
            ["docs/repository/agent-workflow.md", "docs/release-management/audit-package.md", "--no-build", "--no-restore"]);
        CheckContainsAll(
            checks,
            "agents-points-to-detailed-contracts",
            documents.AgentsPath,
            documents.AgentsText,
            ["docs/release-management/audit-package.md", "docs/repository/agent-workflow.md", "goal-task-workflow.md"]);
        Check(
            checks,
            "agents-root-stays-compact",
            documents.AgentsPath,
            documents.AgentsText is not null && Encoding.UTF8.GetByteCount(documents.AgentsText) <= 8_192,
            "Root AGENTS.md must stay compact and keep detailed rules in docs/repository/agent-workflow.md.");
        CheckContainsAll(
            checks,
            "agents-repo-map-core-commands-subagents",
            documents.AgentsPath,
            documents.AgentsText,
            ["## Repository Map", "## Core Commands", "## Guardrails", "docs/repository/agent-workflow.md"]);
        Check(
            checks,
            "agents-root-stays-english",
            documents.AgentsPath,
            documents.AgentsText is not null &&
                !new[]
                {
                    "## Карта",
                    "Перед содержательной",
                    "Соблюдай",
                    "Веди `TASKS.md`",
                    "Проверки"
                }.Any(marker => documents.AgentsText.Contains(marker, StringComparison.OrdinalIgnoreCase)),
            "Root AGENTS.md must stay in English.");
        Check(
            checks,
            "agents-does-not-duplicate-agent-workflow",
            documents.AgentsPath,
            documents.AgentsText is not null &&
                !new[]
                {
                    "## Docker, Deployment",
                    "## Worktree And Commit Hygiene",
                    "## External Audit Workflow",
                    "Subagent prompt must include",
                    "Every repository-work session updates"
                }.Any(marker => documents.AgentsText.Contains(marker, StringComparison.OrdinalIgnoreCase)),
            "Root AGENTS.md must stay an index and guardrail file, not duplicate docs/repository/agent-workflow.md.");
        CheckContainsAll(
            checks,
            "agent-workflow-detailed-rules",
            documents.AgentWorkflowPath,
            documents.AgentWorkflowText,
            ["Feature Gate", "Task Workflow", "Development Diary", "External Audit Workflow", "Subagents"]);
        Check(
            checks,
            "agent-workflow-stays-english",
            documents.AgentWorkflowPath,
            documents.AgentWorkflowText is not null &&
                !new[]
                {
                    "# Рабочий протокол",
                    "## Приоритет",
                    "## Язык",
                    "## Дневник",
                    "## Проверки",
                    "Правила:"
                }.Any(marker => documents.AgentWorkflowText.Contains(marker, StringComparison.OrdinalIgnoreCase)),
            "docs/repository/agent-workflow.md must stay in English.");

        CheckContainsAll(
            checks,
            "test-command-audit-tier-slice",
            documents.TestCommandPath,
            documents.TestCommandText,
            ["AuditTier(AuditTierMedium)", "AuditTier(AuditTierHeavy)", "NotAuditTier(AuditTierFast)", "IntegrationSliceAuditPackage", "IntegrationSliceAuditMedium", "IntegrationSliceAuditHeavy"]);
        CheckContainsAll(
            checks,
            "test-command-slice-summary",
            documents.TestCommandPath,
            documents.TestCommandText,
            ["E2D-BUILD-TEST-SLICE-SUMMARY", "TryParseDotnetTestCounts", "childProcesses", "failure={failure}"]);
        CheckContainsAll(
            checks,
            "test-command-no-build-environment",
            documents.TestCommandPath,
            documents.TestCommandText,
            ["ELECTRON2D_BUILD_TOOL_NO_BUILD", "CreateDotnetTestEnvironment", "BuildToolNoBuildEnvironmentVariable"]);
        CheckContainsAll(
            checks,
            "operator-workflow-summary",
            documents.AuditPackageCommandPath,
            documents.AuditPackageCommandText,
            ["E2D-BUILD-AUDIT-OPERATOR-WORKFLOW-SUMMARY", "Operator workflow sidecar summary", "audit-package-message", "audit-package-verify"]);
        CheckContainsAll(
            checks,
            "audit-package-previous-blocker-closure-matrix",
            documents.AuditPackageCommandPath,
            documents.AuditPackageCommandText,
            ["Previous Blocker Closure Matrix", "CreatePreviousBlockerClosureMatrix", "PreviousBlockerClosureMatrixRow"]);
        CheckContainsAll(
            checks,
            "integration-audit-tier-guards",
            documents.IntegrationTestsPath,
            documents.IntegrationTestsText,
            [
                "AuditWorkflowAuditTestsDeclareAuditTierTraits",
                "AuditWorkflowFastAuditTierDoesNotUseHeavyHelpers",
                "AuditWorkflowMediumAuditTierDoesNotUseHeavyPackagingHelpers",
                "TestCommandAuditTierIntegrationSlicesPrintSummary",
                "AuditPackageWritesPreviousBlockerClosureMatrixIntoManifest",
                "env ELECTRON2D_BUILD_TOOL_NO_BUILD=",
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
        var agentWorkflowPath = "docs/repository/agent-workflow.md";
        var goalPromptPath = ".codex/prompts/goal-prompt.md";
        var goalLoopPath = ".codex/prompts/goal-task-workflow.md";
        var gitIgnorePath = ".gitignore";
        var agentsPath = "AGENTS.md";
        var testCommandPath = "eng/Electron2D.Build/TestCommand.cs";
        var auditPackageCommandPath = "eng/Electron2D.Build/AuditPackageCommand.cs";
        var integrationTestsPath = "tests/Electron2D.Tests.Integration/RepositoryBuildToolTests.cs";
        return new AuditContractDocuments(
            auditRequestPath,
            ReadRequiredText(checks, auditRequestPath),
            domainDocumentPath,
            ReadRequiredText(checks, domainDocumentPath),
            agentWorkflowPath,
            ReadRequiredText(checks, agentWorkflowPath),
            goalPromptPath,
            ReadRequiredText(checks, goalPromptPath),
            goalLoopPath,
            ReadRequiredText(checks, goalLoopPath),
            gitIgnorePath,
            ReadRequiredText(checks, gitIgnorePath),
            agentsPath,
            ReadRequiredText(checks, agentsPath),
            testCommandPath,
            ReadRequiredText(checks, testCommandPath),
            auditPackageCommandPath,
            ReadRequiredText(checks, auditPackageCommandPath),
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
    string AgentWorkflowPath,
    string? AgentWorkflowText,
    string GoalPromptPath,
    string? GoalPromptText,
    string GoalLoopPath,
    string? GoalLoopText,
    string GitIgnorePath,
    string? GitIgnoreText,
    string AgentsPath,
    string? AgentsText,
    string TestCommandPath,
    string? TestCommandText,
    string AuditPackageCommandPath,
    string? AuditPackageCommandText,
    string IntegrationTestsPath,
    string? IntegrationTestsText);

internal sealed record AuditContractCheckResult(
    string Name,
    string? Path,
    bool Passed,
    bool Skipped,
    string Message);
