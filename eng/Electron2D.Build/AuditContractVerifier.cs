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
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class AuditContractVerifier(string repositoryRoot, JsonDiagnosticSink diagnostics)
{
    private const string Step = "verify audit-contracts";
    private const int FastCheckCountMinimum = 10;
    private const int FastCheckCountMaximum = 15;
    private static readonly string FastCheckCountBudgetMarker = $"Ожидаемый объём - {FastCheckCountMinimum}-{FastCheckCountMaximum} лёгких проверок";
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

        VerifyRequiredDocuments(checks, documents);
        VerifyAuditRequestContract(checks, documents);
        VerifyDomainDocumentContract(checks, documents);
        VerifyGoalPromptContract(checks, documents);
        VerifyGoalWorkflowContract(checks, documents);
        VerifyAgentsContract(checks, documents);
        VerifyAgentWorkflowContract(checks, documents);
        VerifyToolingSourceContracts(checks, documents);
        VerifyIntegrationTestsContract(checks, documents);

        VerifyFollowupParser(checks);
        VerifyFinalReportExtractor(checks);
        VerifyFastCheckCountBudget(checks, documents);

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
        if (!File.Exists(fullPath))
        {
            return null;
        }

        var text = File.ReadAllText(fullPath, Encoding.UTF8);
        return text;
    }

    private static void VerifyRequiredDocuments(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var missing = RequiredDocumentTexts(documents)
            .Where(document => string.IsNullOrWhiteSpace(document.Text))
            .Select(document => document.Path)
            .ToArray();
        Check(
            checks,
            "required-audit-contract-files",
            null,
            missing.Length == 0,
            missing.Length == 0
                ? "All required audit contract files exist and are non-empty."
                : $"Required audit contract files are missing or empty: {string.Join(", ", missing)}.");
    }

    private static void VerifyAuditRequestContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var markers = AuditRequestRequiredMarkers
            .Concat(
            [
                "full current-scope engineering review",
                "primary audit",
                "control audit",
                "current-task blocker",
                "follow-up finding",
                "global safety blocker",
                "unsupported concern",
                "Правило доказанного blocker-а",
                "Проверка опровержения",
                "evidence-backed blocker",
                "blocker disproof",
                "путь прошлого отчёта",
                "идентификатор blocker-а",
                "каждого найденного blocker-а",
                "картой проверки закрытий"
            ])
            .ToArray();
        CheckContainsAll(checks, "audit-request-contract", documents.AuditRequestPath, documents.AuditRequestText, markers);
    }

    private static void VerifyDomainDocumentContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        CheckContainsAll(
            checks,
            "domain-audit-package-contract",
            documents.DomainDocumentPath,
            documents.DomainDocumentText,
            [
                "`Fast`",
                "`Medium`",
                "`Heavy`",
                "verify audit-contracts",
                "AuditTier=Fast",
                FastCheckCountBudgetMarker,
                "30 секунд",
                "не создаёт audit ZIP",
                "не создаёт чистую копию репозитория",
                "не вызывает дочерние `dotnet run`",
                "Правило остановки",
                "failure class",
                "не увеличивать `rNN`",
                "audit-loop-stabilization",
                "metadata.previousVerdictChain",
                "metadata.blockerClosureList",
                "checks[].name",
                "preflightChecks[].name",
                "путь прошлого отчёта",
                "идентификатор blocker-а",
                "configured/preflight check",
                "каждый найденный blocker",
                "распознаваемые `B*` blocker ids",
                "матрицу закрытия прошлых blocker-ов",
                "blocker id",
                "preflightChecks",
                "audit package` и `audit package verify` не должны повторно запускать тестовые наборы",
                "любые команды тестового раннера внутри `checks[]` запрещены",
                ".temp/audit-evidence/...",
                "archiveOnlyEvidenceGlobs",
                "preflightChecks[].evidenceGlobs",
                "checks[].trxGlobs",
                "Агент не выбирает route",
                "submit-attempt-rNN.json",
                "Тот же ZIP и тот же `rNN` повторно не отправляются",
                "прикрепляет только основной ZIP",
                "не вставляет в composer текст `AUDIT-REQUEST.md`",
                "Явный `--message` для автоматически выведенного reuse отклоняется",
                "При любом исходе команда в `finally` закрывает только вкладку, созданную текущим запуском"
            ]);
    }

    private static void VerifyGoalPromptContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var missing = MissingMarkers(
            documents.GoalPromptText,
            [
                ".codex/prompts/goal-task-workflow.md",
                "VERDICT: ACCEPT",
                "RISKS_AND_NOTES",
                "verify audit-followups",
                "git status",
                "Conventional Commit"
            ]).ToList();
        if (documents.GoalPromptText is null ||
            !documents.GoalPromptText.Contains("`.codex/prompts/goal-task-workflow.md`", StringComparison.Ordinal) ||
            documents.GoalPromptText.Contains("\\Electron2D\\", StringComparison.Ordinal))
        {
            missing.Add("repo-relative workflow path");
        }

        if (documents.GoalPromptText is null || documents.GoalPromptText.Length > 3500)
        {
            missing.Add("<=3500 characters");
        }

        if (ContainsAny(
            documents.GoalPromptText,
            StringComparison.Ordinal,
            "PREFLIGHT:",
            "WORKER:",
            "CHECKS:",
            "LOCAL LOOP:",
            "PACKAGE:",
            "VERIFY:",
            "SUBMIT:",
            "ACCEPT/DONE:"))
        {
            missing.Add("no duplicated workflow sections");
        }

        if (!ContainsAll(documents.GitIgnoreText, ["!.codex/prompts/goal-prompt.md", "!.codex/prompts/goal-task-workflow.md"]))
        {
            missing.Add(".gitignore prompt exceptions");
        }

        Check(
            checks,
            "goal-prompt-contract",
            documents.GoalPromptPath,
            missing.Count == 0,
            missing.Count == 0
                ? "Goal prompt contract is satisfied."
                : $"Goal prompt contract failed: {string.Join(", ", missing)}.");
    }

    private static void VerifyGoalWorkflowContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var missing = MissingMarkers(
            documents.GoalLoopText,
            [
                "verify audit-contracts",
                "fast path",
                "focused integration tests",
                "Fast: 3 теста / 1m13s",
                "Medium: 4 теста / 3m18s",
                "Heavy: не запускался",
                "docs/release-management/audit-package.md",
                "audit-loop-stabilization",
                "previousVerdictChain",
                "blockerClosureList",
                "Goal Task Workflow",
                ".codex/prompts/goal-prompt.md",
                "лимит 3500 символов"
            ]).ToList();
        if (ContainsAny(
            documents.GoalLoopText,
            StringComparison.OrdinalIgnoreCase,
            "## Docker",
            "## Worktree",
            "## Feature Gate",
            "## Development Diary",
            "public C#",
            "Manual ZIP assembly"))
        {
            missing.Add("no duplicated AGENTS.md or agent-workflow sections");
        }

        Check(
            checks,
            "goal-workflow-contract",
            documents.GoalLoopPath,
            missing.Count == 0,
            missing.Count == 0
                ? "Goal workflow contract is satisfied."
                : $"Goal workflow contract failed: {string.Join(", ", missing)}.");
    }

    private static void VerifyAgentsContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var missing = MissingMarkers(
            documents.AgentsText,
            [
                "audit package",
                "audit package verify",
                "audit submit",
                "Manual ZIP assembly",
                "manual submit",
                "manual page verdict reading",
                "docs/repository/agent-workflow.md",
                "docs/release-management/audit-package.md",
                "--no-build",
                "--no-restore",
                "goal-task-workflow.md",
                "## Repository Map",
                "## Core Commands",
                "## Guardrails",
                "verify audit-followups",
                "AGENTS.override.md",
                "closest applicable file",
                "only on explicit user request",
                "never push by default",
                "current harness and user request explicitly allow",
                "scoped to read-heavy work",
                "main agent owns final edits"
            ]).ToList();
        if (documents.AgentsText is null || Encoding.UTF8.GetByteCount(documents.AgentsText) > 8_192)
        {
            missing.Add("<=8192 bytes");
        }

        if (ContainsAny(
            documents.AgentsText,
            StringComparison.OrdinalIgnoreCase,
            "## Карта",
            "Перед содержательной",
            "Соблюдай",
            "Веди `TASKS.md`",
            "Проверки"))
        {
            missing.Add("English-only root AGENTS.md");
        }

        if (ContainsAny(
            documents.AgentsText,
            StringComparison.OrdinalIgnoreCase,
            "## Docker, Deployment",
            "## Worktree And Commit Hygiene",
            "## External Audit Workflow",
            "Subagent prompt must include",
            "Every repository-work session updates"))
        {
            missing.Add("no detailed agent-workflow duplication");
        }

        Check(
            checks,
            "agents-root-contract",
            documents.AgentsPath,
            missing.Count == 0,
            missing.Count == 0
                ? "Root AGENTS.md contract is satisfied."
                : $"Root AGENTS.md contract failed: {string.Join(", ", missing)}.");
    }

    private static void VerifyAgentWorkflowContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var missing = MissingMarkers(
            documents.AgentWorkflowText,
            ["Feature Gate", "Task Workflow", "Development Diary", "External Audit Workflow", "Subagents"]).ToList();
        if (ContainsAny(
            documents.AgentWorkflowText,
            StringComparison.OrdinalIgnoreCase,
            "# Рабочий протокол",
            "## Приоритет",
            "## Язык",
            "## Дневник",
            "## Проверки",
            "Правила:"))
        {
            missing.Add("English-only agent workflow");
        }

        Check(
            checks,
            "agent-workflow-contract",
            documents.AgentWorkflowPath,
            missing.Count == 0,
            missing.Count == 0
                ? "Agent workflow contract is satisfied."
                : $"Agent workflow contract failed: {string.Join(", ", missing)}.");
    }

    private static void VerifyToolingSourceContracts(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var missing = new List<string>();
        missing.AddRange(MissingMarkers(
            documents.TestCommandText,
            [
                "AuditTier(AuditTierMedium)",
                "AuditTier(AuditTierHeavy)",
                "NotAuditTier(AuditTierFast)",
                "IntegrationSliceAuditPackage",
                "IntegrationSliceAuditMedium",
                "IntegrationSliceAuditHeavy",
                "E2D-BUILD-TEST-SLICE-SUMMARY",
                "TryParseDotnetTestCounts",
                "childProcesses",
                "failure={failure}",
                "ELECTRON2D_BUILD_TOOL_NO_BUILD",
                "CreateDotnetTestEnvironment",
                "BuildToolNoBuildEnvironmentVariable"
            ]).Select(marker => $"{documents.TestCommandPath}: {marker}"));
        missing.AddRange(MissingMarkers(
            documents.AuditPackageCommandText,
            [
                "E2D-BUILD-AUDIT-OPERATOR-WORKFLOW-SUMMARY",
                "Operator workflow sidecar summary",
                "audit-package-message",
                "audit-package-verify",
                "Previous Blocker Closure Matrix",
                "CreatePreviousBlockerClosureMatrix",
                "PreviousBlockerClosureMatrixRow"
            ]).Select(marker => $"{documents.AuditPackageCommandPath}: {marker}"));

        Check(
            checks,
            "tooling-source-contracts",
            null,
            missing.Count == 0,
            missing.Count == 0
                ? "Tooling source contracts are satisfied."
                : $"Tooling source contracts failed: {string.Join(", ", missing)}.");
    }

    private static void VerifyIntegrationTestsContract(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        CheckContainsAll(
            checks,
            "integration-tests-contract",
            documents.IntegrationTestsPath,
            documents.IntegrationTestsText,
            [
                "AuditWorkflowAuditTestsDeclareAuditTierTraits",
                "AuditWorkflowFastAuditTierDoesNotUseHeavyHelpers",
                "AuditWorkflowMediumAuditTierDoesNotUseHeavyPackagingHelpers",
                "TestCommandAuditTierMediumIntegrationSlicesPrintSummary",
                "TestCommandAuditTierHeavyIntegrationSlicesPrintSummary",
                "AuditPackageWritesPreviousBlockerClosureMatrixIntoManifest",
                "AuditSubmitReuseConversationRejectsExplicitMessageBeforeBrowserLaunch",
                "AuditSubmitPromptSubmissionSkipsPromptFillForEmptyReuseMessage",
                "AuditSubmitPromptPayloadReadyAllowsEmptyPromptOnlyForZipOnlyReuse",
                "AuditWorkflowVerifyAuditContractsRejectsStaleFastCheckCountBudget",
                "env ELECTRON2D_BUILD_TOOL_NO_BUILD=",
                "Trait(\"AuditTier\", \"Fast\")",
                "Trait(\"AuditTier\", \"Medium\")",
                "Trait(\"AuditTier\", \"Heavy\")"
            ]);
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
        var findsActionableRiskClasses = findings.Count == 2 &&
            findings.Any(finding => finding.Kind == "FOLLOW_UP_FINDING" && finding.FindingId == "F1") &&
            findings.Any(finding => finding.Kind == "INFO_NOTE" && finding.FindingId == "I1");
        var ignoresPassiveOutOfScopeNote = findings.All(finding => finding.FindingId != "N1");
        Check(
            checks,
            "followup-parser-contract",
            null,
            findsActionableRiskClasses && ignoresPassiveOutOfScopeNote,
            findsActionableRiskClasses && ignoresPassiveOutOfScopeNote
                ? "Follow-up parser detects actionable notes and ignores passive out-of-scope notes."
                : "Follow-up parser contract failed.");
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
        var acceptsStrictAccept = valid.Ready && valid.Report is not null;
        var rejectsAcceptWithBlocker = !invalid.Ready &&
            invalid.FailureReason?.Contains("numbered blocker", StringComparison.OrdinalIgnoreCase) == true;
        Check(
            checks,
            "final-report-extractor-contract",
            null,
            acceptsStrictAccept && rejectsAcceptWithBlocker,
            acceptsStrictAccept && rejectsAcceptWithBlocker
                ? "Final report extractor accepts strict ACCEPT and rejects ACCEPT with numbered blockers."
                : "Final report extractor contract failed.");
    }

    private static void VerifyFastCheckCountBudget(List<AuditContractCheckResult> checks, AuditContractDocuments documents)
    {
        var finalCheckCount = checks.Count + 1;
        var documentedRange = ExtractFastCheckCountBudgetRange(documents.DomainDocumentText);
        var documentedBudgetMatches = string.Equals(
            documentedRange,
            $"{FastCheckCountMinimum}-{FastCheckCountMaximum}",
            StringComparison.Ordinal);
        var countIsWithinBudget = finalCheckCount >= FastCheckCountMinimum && finalCheckCount <= FastCheckCountMaximum;

        Check(
            checks,
            "fast-check-count-budget",
            documents.DomainDocumentPath,
            documentedBudgetMatches && countIsWithinBudget,
            documentedBudgetMatches && countIsWithinBudget
                ? $"Fast check count budget is documented and satisfied: checks={finalCheckCount}; expected={FastCheckCountMinimum}-{FastCheckCountMaximum}."
                : $"Fast check count budget mismatch: checks={finalCheckCount}; expected={FastCheckCountMinimum}-{FastCheckCountMaximum}; documented={documentedRange ?? "<missing>"}; required marker={FastCheckCountBudgetMarker}.");
    }

    private static string? ExtractFastCheckCountBudgetRange(string? text)
    {
        if (text is null)
        {
            return null;
        }

        var match = Regex.Match(
            text,
            @"Ожидаемый объём\s*-\s*(?<range>\d+-\d+)\s+лёгких проверок",
            RegexOptions.CultureInvariant);
        return match.Success ? match.Groups["range"].Value : null;
    }

    private static (string Path, string? Text)[] RequiredDocumentTexts(AuditContractDocuments documents)
    {
        return
        [
            (documents.AuditRequestPath, documents.AuditRequestText),
            (documents.DomainDocumentPath, documents.DomainDocumentText),
            (documents.AgentWorkflowPath, documents.AgentWorkflowText),
            (documents.GoalPromptPath, documents.GoalPromptText),
            (documents.GoalLoopPath, documents.GoalLoopText),
            (documents.GitIgnorePath, documents.GitIgnoreText),
            (documents.AgentsPath, documents.AgentsText),
            (documents.TestCommandPath, documents.TestCommandText),
            (documents.AuditPackageCommandPath, documents.AuditPackageCommandText),
            (documents.IntegrationTestsPath, documents.IntegrationTestsText)
        ];
    }

    private static bool ContainsAll(string? text, string[] requiredMarkers)
    {
        return !MissingMarkers(text, requiredMarkers).Any();
    }

    private static IEnumerable<string> MissingMarkers(string? text, string[] requiredMarkers)
    {
        return text is null
            ? requiredMarkers
            : requiredMarkers.Where(marker => !text.Contains(marker, StringComparison.Ordinal));
    }

    private static bool ContainsAny(string? text, StringComparison comparison, params string[] markers)
    {
        return text is not null && markers.Any(marker => text.Contains(marker, comparison));
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
