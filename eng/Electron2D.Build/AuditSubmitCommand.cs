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
using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class AuditSubmitCommand
{
    private const string DefaultProjectUrl = "https://chatgpt.com/g/g-p-6950376d4d8c8191a0fe600e98389912-electro2d/project";
    private const int DefaultPollSeconds = 60;
    private const int MinimumOperationalPollSeconds = 60;
    private const int DefaultTimeoutMinutes = 180;
    private const int DefaultLoginTimeoutMinutes = 10;
    private const string AuditPackageInputEntryName = "metadata/audit-package.input.json";
    private const string RepoFileHashesEntryName = "repo-file-hashes.json";
    private const string RepoFileSnapshotsEntryName = "metadata/repo-file-snapshots.json";
    private const string VerdictDocsPrefix = "docs/verdicts/";
    private static readonly Regex AuditZipFileNameExpression = new(
        @"^(?<task>T-\d+)-audit-(?<iteration>r\d+)\.zip$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex AuditVerdictFileNameExpression = new(
        @"^(?<task>T-\d+)-audit-(?:(?<control>control)-)?(?<iteration>r\d+)\.md$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex SavedVerdictReportReferenceExpression = new(
        @"(?<path>(?:repo-after/|repo-before/)?docs/verdicts/[^\s`'""<>*?]+\.md)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = ParseOptions(args);
        ValidatePolling(options);

        var repoRoot = Directory.GetCurrentDirectory();
        string report;
        AuditSubmitZipIdentity? zipIdentity = null;
        if (options.DumpDomOnly)
        {
            report = await new AuditSubmitCodexChromeAutomation()
                .DumpDomFromUrlAsync(options, repoRoot, cancellationToken)
                .ConfigureAwait(false);
        }
        else if (options.DownloadReportOnly)
        {
            report = await new AuditSubmitCodexChromeAutomation()
                .DownloadReportFromUrlAsync(options, repoRoot, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            var zipPathOption = options.ZipPath ?? throw InvalidArguments("Missing required option: --zip.");
            var zipPath = ResolvePath(repoRoot, zipPathOption);
            if (!File.Exists(zipPath))
            {
                throw new AuditPackageFailure(
                    "audit submit",
                    "E2D-BUILD-AUDIT-SUBMIT-ZIP-MISSING",
                    $"Audit ZIP was not found: {zipPathOption}",
                    ZipPath: zipPath);
            }

            zipIdentity = ResolveAuditZipIdentity(zipPath);
            ValidateAuditSubmitState(options, repoRoot, zipPath, zipIdentity);
            var message = await ResolveSubmitMessageAsync(options, repoRoot, zipPath, cancellationToken).ConfigureAwait(false);
            ValidateAuditSubmitOutputPath(options.OutputPath, zipIdentity, options.ControlAudit);
            var submitOptions = options.ReuseConversation && string.IsNullOrWhiteSpace(options.ProjectUrl)
                ? options with { ProjectUrl = await ResolveStoredConversationUrlAsync(repoRoot, zipPath, zipIdentity, cancellationToken).ConfigureAwait(false) }
                : options;
            report = await new AuditSubmitCodexChromeAutomation()
                .SubmitAndWaitForReportAsync(submitOptions, repoRoot, zipPath, message, cancellationToken)
                .ConfigureAwait(false);
        }

        var expectedReportIdentity = zipIdentity ?? (options.DownloadReportOnly ? ResolveAuditReportOutputIdentity(options.OutputPath) : null);
        if (expectedReportIdentity is not null)
        {
            ValidateReportMatchesSubmitIteration(report, expectedReportIdentity, options.ZipPath ?? options.OutputPath);
        }

        var outputPath = ResolvePath(repoRoot, options.OutputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? repoRoot);
        await File.WriteAllTextAsync(
            outputPath,
            report + Environment.NewLine,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
        Console.Out.Write(report);
        if (!report.EndsWith('\n'))
        {
            Console.Out.WriteLine();
        }
    }

    private static void ValidateReportMatchesSubmitIteration(string report, AuditSubmitZipIdentity zipIdentity, string? zipPath)
    {
        var taskIdPattern = Regex.Escape(zipIdentity.TaskId);
        var evidenceIterationMatches = Regex.Matches(
                report,
                $@"\bevidence/{taskIdPattern}-(?<iteration>r\d{{2}})/checks/",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            .Select(match => match.Groups["iteration"].Value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (evidenceIterationMatches.Length > 0 &&
            !evidenceIterationMatches.Contains(zipIdentity.Iteration, StringComparer.Ordinal))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE",
                $"Downloaded report references evidence for {zipIdentity.TaskId} {FormatIterationList(evidenceIterationMatches)} but not current {zipIdentity.Iteration}.",
                ZipPath: zipPath);
        }

        var zipIterationMatches = Regex.Matches(
                report,
                $@"\b{taskIdPattern}-audit-(?<iteration>r\d{{2}})\.zip\b",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)
            .Select(match => match.Groups["iteration"].Value.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (zipIterationMatches.Length > 0 &&
            !zipIterationMatches.Contains(zipIdentity.Iteration, StringComparer.Ordinal))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-STALE",
                $"Downloaded report references audit ZIP for {zipIdentity.TaskId} {FormatIterationList(zipIterationMatches)} but not current {zipIdentity.Iteration}.",
                ZipPath: zipPath);
        }

        if (!ReportMentionsCurrentMetadataIdentity(report, zipIdentity))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID",
                $"Downloaded report does not explicitly identify current {zipIdentity.TaskId} {zipIdentity.Iteration} with metadata.taskId and metadata.iteration.",
                ZipPath: zipPath);
        }
    }

    private static bool ReportMentionsCurrentMetadataIdentity(string report, AuditSubmitZipIdentity zipIdentity)
    {
        var taskAssessment = ExtractFinalReportSection(report, "TASK_ASSESSMENT:", "BLOCKERS:");
        return ReportMentionsMetadataValue(taskAssessment, "metadata.taskId", zipIdentity.TaskId) &&
            ReportMentionsMetadataValue(taskAssessment, "metadata.iteration", zipIdentity.Iteration);
    }

    private static bool ReportMentionsMetadataValue(string report, string metadataName, string expectedValue)
    {
        var pattern = $@"(?<![\w.])[`""']?{Regex.Escape(metadataName)}[`""']?(?![\w.])\s*[:=]\s*[`""']?{Regex.Escape(expectedValue)}[`""']?(?![\w-])";
        return Regex.IsMatch(report, pattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static string ExtractFinalReportSection(string report, string heading, string nextHeading)
    {
        var lines = report
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');
        var start = Array.FindIndex(lines, line => string.Equals(line.Trim(), heading, StringComparison.Ordinal));
        if (start < 0)
        {
            return string.Empty;
        }

        var end = Array.FindIndex(lines, start + 1, line => string.Equals(line.Trim(), nextHeading, StringComparison.Ordinal));
        if (end < 0)
        {
            end = lines.Length;
        }

        return string.Join('\n', lines.Skip(start + 1).Take(end - start - 1));
    }

    private static string FormatIterationList(IReadOnlyCollection<string> iterations)
    {
        return string.Join(", ", iterations.Order(StringComparer.Ordinal));
    }

    private static AuditSubmitOptions ParseOptions(string[] args)
    {
        var values = ParseNamedArguments(
            args,
            startIndex: 2,
            allowedValueOptions:
            [
                "--zip",
                "--out",
                "--message",
                "--project-url",
                "--dom-dump-dir",
                "--poll-seconds",
                "--timeout-minutes",
                "--login-timeout-minutes",
                "--browser-backend",
                "--codex-chrome-pipe",
                "--codex-session-id",
                "--codex-turn-id"
            ],
            allowedFlags: ["--allow-fast-poll", "--download-report-only", "--dump-dom-only", "--keep-tab-open-on-error", "--reuse-conversation", "--control-audit", "--new-conversation", "--deep-research"]);

        ValidateBrowserBackend(values);
        var downloadReportOnly = values.ContainsKey("--download-report-only");
        var dumpDomOnly = values.ContainsKey("--dump-dom-only");
        var reuseConversation = values.ContainsKey("--reuse-conversation");
        var controlAudit = values.ContainsKey("--control-audit");
        var newConversation = values.ContainsKey("--new-conversation");
        var deepResearch = values.ContainsKey("--deep-research");
        if (downloadReportOnly && dumpDomOnly)
        {
            throw InvalidArguments("--download-report-only is not accepted together with --dump-dom-only.");
        }

        if (reuseConversation && controlAudit)
        {
            throw InvalidArguments("--reuse-conversation is not accepted together with --control-audit.");
        }

        if (newConversation && (reuseConversation || controlAudit))
        {
            throw InvalidArguments("--new-conversation is not accepted together with --reuse-conversation or --control-audit.");
        }

        if ((downloadReportOnly || dumpDomOnly) && (reuseConversation || controlAudit || newConversation))
        {
            throw InvalidArguments("--reuse-conversation, --control-audit, and --new-conversation are accepted only for a submit that sends a ZIP.");
        }

        if ((downloadReportOnly || dumpDomOnly) && deepResearch)
        {
            throw InvalidArguments("--deep-research is accepted only for a submit that sends a ZIP.");
        }

        if ((downloadReportOnly || dumpDomOnly) && values.ContainsKey("--zip"))
        {
            throw InvalidArguments("--zip is not accepted together with --download-report-only or --dump-dom-only.");
        }

        if ((downloadReportOnly || dumpDomOnly) && values.ContainsKey("--message"))
        {
            throw InvalidArguments("--message is not accepted together with --download-report-only or --dump-dom-only.");
        }

        if ((downloadReportOnly || dumpDomOnly) && !values.ContainsKey("--project-url"))
        {
            throw InvalidArguments("--download-report-only and --dump-dom-only require --project-url.");
        }

        if (dumpDomOnly && !values.ContainsKey("--dom-dump-dir"))
        {
            throw InvalidArguments("--dump-dom-only requires --dom-dump-dir.");
        }

        var zipPath = downloadReportOnly || dumpDomOnly ? null : Require(values, "--zip");
        var outputPath = Require(values, "--out");
        if (downloadReportOnly)
        {
            var outputIdentity = ResolveCanonicalAuditVerdictOutputPathIdentity(outputPath);
            if (outputIdentity is null || outputIdentity.Control)
            {
                throw InvalidArguments("--download-report-only requires --out to use docs/verdicts/<domain>/<task-id>-audit-rNN.md so the current metadata.taskId and metadata.iteration can be validated.");
            }
        }

        if (dumpDomOnly && IsAuditVerdictOutputPath(outputPath))
        {
            throw InvalidArguments("--dump-dom-only must not write diagnostics to docs/verdicts; keep verdict paths only for saved audit reports and use --dom-dump-dir for DOM diagnostics.");
        }

        var messagePath = values.TryGetValue("--message", out var configuredMessagePath) && !string.IsNullOrWhiteSpace(configuredMessagePath)
            ? configuredMessagePath
            : null;
        if (reuseConversation && messagePath is not null)
        {
            throw InvalidArguments("--message is not accepted together with --reuse-conversation; repeated audit submit sends only the audit ZIP into the existing conversation.");
        }

        var hasProjectUrl = values.TryGetValue("--project-url", out var configuredProjectUrl) && !string.IsNullOrWhiteSpace(configuredProjectUrl);
        var projectUrl = hasProjectUrl
            ? configuredProjectUrl!
            : DefaultProjectUrl;
        if (downloadReportOnly && !AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(projectUrl))
        {
            throw InvalidArguments("--download-report-only requires --project-url to be a concrete ChatGPT conversation URL containing /c/<conversation-id>.");
        }

        if (reuseConversation)
        {
            if (hasProjectUrl && !AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(projectUrl))
            {
                throw InvalidArguments("--reuse-conversation requires --project-url to be a concrete ChatGPT conversation URL containing /c/<conversation-id>, or no --project-url so the stored task conversation URL can be used.");
            }

            if (!hasProjectUrl)
            {
                projectUrl = string.Empty;
            }
        }

        if (controlAudit && AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(projectUrl))
        {
            throw InvalidArguments("--control-audit must start from the ChatGPT project URL, not an existing conversation URL.");
        }

        if (newConversation && hasProjectUrl)
        {
            throw InvalidArguments("--new-conversation uses the configured Electron2D audit project URL and does not accept --project-url.");
        }

        var domDumpDirectory = values.TryGetValue("--dom-dump-dir", out var configuredDomDumpDirectory) && !string.IsNullOrWhiteSpace(configuredDomDumpDirectory)
            ? configuredDomDumpDirectory
            : null;
        var pollSeconds = values.TryGetValue("--poll-seconds", out var configuredPollSeconds) && !string.IsNullOrWhiteSpace(configuredPollSeconds)
            ? ParsePositiveInt(configuredPollSeconds, "--poll-seconds")
            : DefaultPollSeconds;
        var timeoutMinutes = values.TryGetValue("--timeout-minutes", out var configuredTimeoutMinutes) && !string.IsNullOrWhiteSpace(configuredTimeoutMinutes)
            ? ParsePositiveInt(configuredTimeoutMinutes, "--timeout-minutes")
            : DefaultTimeoutMinutes;
        var loginTimeoutMinutes = values.TryGetValue("--login-timeout-minutes", out var configuredLoginTimeoutMinutes) && !string.IsNullOrWhiteSpace(configuredLoginTimeoutMinutes)
            ? ParsePositiveInt(configuredLoginTimeoutMinutes, "--login-timeout-minutes")
            : DefaultLoginTimeoutMinutes;
        var codexChromePipe = values.TryGetValue("--codex-chrome-pipe", out var configuredCodexChromePipe) && !string.IsNullOrWhiteSpace(configuredCodexChromePipe)
            ? configuredCodexChromePipe
            : null;
        var codexBrowserIdentity = ResolveCodexBrowserIdentity(values);
        var keepTabOpenOnError = values.ContainsKey("--keep-tab-open-on-error");

        return new AuditSubmitOptions(
            zipPath,
            outputPath,
            messagePath,
            projectUrl,
            pollSeconds,
            timeoutMinutes,
            loginTimeoutMinutes,
            values.ContainsKey("--allow-fast-poll"),
            downloadReportOnly,
            dumpDomOnly,
            domDumpDirectory,
            codexChromePipe,
            codexBrowserIdentity.SessionId,
            codexBrowserIdentity.TurnId,
            keepTabOpenOnError,
            reuseConversation,
            controlAudit,
            newConversation,
            deepResearch);
    }

    private static void ValidatePolling(AuditSubmitOptions options)
    {
        if (options.PollSeconds < MinimumOperationalPollSeconds && !options.AllowFastPoll)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-POLL-INTERVAL",
                $"--poll-seconds must be at least {MinimumOperationalPollSeconds} unless --allow-fast-poll is supplied.");
        }
    }

    private static void ValidateBrowserBackend(IReadOnlyDictionary<string, string?> values)
    {
        if (!values.TryGetValue("--browser-backend", out var value) || string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!string.Equals(value, "codex-chrome", StringComparison.Ordinal))
        {
            throw InvalidArguments("--browser-backend supports only codex-chrome.");
        }
    }

    private static async Task<string> ResolveSubmitMessageAsync(
        AuditSubmitOptions options,
        string repoRoot,
        string zipPath,
        CancellationToken cancellationToken)
    {
        if (options.ReuseConversation)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(options.MessagePath))
        {
            return AuditPackageCommand.CreatePackageMessage(new AuditMessageOptions(zipPath), repoRoot);
        }

        var messagePath = ResolvePath(repoRoot, options.MessagePath);
        if (!File.Exists(messagePath))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-MESSAGE-MISSING",
                $"Audit submit message file was not found: {options.MessagePath}",
                ZipPath: zipPath);
        }

        var message = await File.ReadAllTextAsync(messagePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-MESSAGE-EMPTY",
                $"Audit submit message file is empty: {options.MessagePath}",
                ZipPath: zipPath);
        }

        return message;
    }

    private static async Task<string> ResolveStoredConversationUrlAsync(
        string repoRoot,
        string zipPath,
        AuditSubmitZipIdentity zipIdentity,
        CancellationToken cancellationToken)
    {
        var path = Path.Combine(repoRoot, ".temp", "audit", zipIdentity.TaskId, "conversation-url.txt");
        if (!File.Exists(path))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
                $"Stored audit conversation URL was not found: .temp/audit/{zipIdentity.TaskId}/conversation-url.txt",
                ZipPath: zipPath);
        }

        var url = (await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false)).Trim();
        if (!AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(url))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
                $"Stored audit conversation URL is not a concrete ChatGPT conversation URL: .temp/audit/{zipIdentity.TaskId}/conversation-url.txt",
                ZipPath: zipPath);
        }

        return url;
    }

    private static AuditSubmitZipIdentity ResolveAuditZipIdentity(string zipPath)
    {
        var fileName = Path.GetFileName(zipPath);
        var match = AuditZipFileNameExpression.Match(fileName);
        if (!match.Success)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-ZIP-NAME-INVALID",
                $"Audit ZIP name must use <task-id>-audit-rNN.zip: {fileName}",
                ZipPath: zipPath);
        }

        var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
        return new AuditSubmitZipIdentity(
            match.Groups["task"].Value.ToUpperInvariant(),
            iteration,
            ParseIterationNumber(iteration));
    }

    private static AuditSubmitZipIdentity? ResolveAuditReportOutputIdentity(string outputPath)
    {
        var fileName = Path.GetFileName(outputPath);
        var match = AuditVerdictFileNameExpression.Match(fileName);
        if (!match.Success)
        {
            return null;
        }

        var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
        return new AuditSubmitZipIdentity(
            match.Groups["task"].Value.ToUpperInvariant(),
            iteration,
            ParseIterationNumber(iteration));
    }

    private static bool IsAuditVerdictOutputPath(string outputPath)
    {
        var fileName = Path.GetFileName(outputPath);
        if (!AuditVerdictFileNameExpression.IsMatch(fileName))
        {
            return false;
        }

        var normalized = outputPath.Replace('\\', '/');
        return normalized.StartsWith(VerdictDocsPrefix, StringComparison.OrdinalIgnoreCase) ||
            normalized.Contains("/" + VerdictDocsPrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCanonicalAuditVerdictOutputPath(string outputPath)
    {
        return ResolveCanonicalAuditVerdictOutputPathIdentity(outputPath) is not null;
    }

    private static AuditSubmitOutputPathIdentity? ResolveCanonicalAuditVerdictOutputPathIdentity(string outputPath)
    {
        if (Path.IsPathRooted(outputPath))
        {
            return null;
        }

        var normalized = outputPath.Replace('\\', '/');
        var segments = normalized.Split('/', StringSplitOptions.None);
        if (segments.Length != 4 ||
            !string.Equals(segments[0], "docs", StringComparison.Ordinal) ||
            !string.Equals(segments[1], "verdicts", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(segments[2]) ||
            !segments[3].EndsWith(".md", StringComparison.Ordinal) ||
            segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".."))
        {
            return null;
        }

        var match = AuditVerdictFileNameExpression.Match(segments[3]);
        if (!match.Success)
        {
            return null;
        }

        var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
        var zipIdentity = new AuditSubmitZipIdentity(
            match.Groups["task"].Value.ToUpperInvariant(),
            iteration,
            ParseIterationNumber(iteration));
        return new AuditSubmitOutputPathIdentity(zipIdentity, match.Groups["control"].Success);
    }

    private static void ValidateAuditSubmitOutputPath(string outputPath, AuditSubmitZipIdentity zipIdentity, bool controlAudit)
    {
        var outputIdentity = ResolveCanonicalAuditVerdictOutputPathIdentity(outputPath);
        if (outputIdentity is null)
        {
            throw InvalidArguments("--out must use docs/verdicts/<domain>/<task-id>-audit-rNN.md for primary audits or docs/verdicts/<domain>/<task-id>-audit-control-rNN.md for control audits.");
        }

        if (outputIdentity.Control != controlAudit)
        {
            var expected = controlAudit ? "control verdict filename" : "primary verdict filename";
            throw InvalidArguments($"--out must use a {expected} for the selected audit submit mode.");
        }

        if (!string.Equals(outputIdentity.ZipIdentity.TaskId, zipIdentity.TaskId, StringComparison.Ordinal) ||
            !string.Equals(outputIdentity.ZipIdentity.Iteration, zipIdentity.Iteration, StringComparison.Ordinal))
        {
            throw InvalidArguments($"--out filename must match current audit ZIP identity {zipIdentity.TaskId} {zipIdentity.Iteration}.");
        }
    }

    private static void ValidateAuditSubmitState(
        AuditSubmitOptions options,
        string repoRoot,
        string zipPath,
        AuditSubmitZipIdentity zipIdentity)
    {
        var verdicts = ReadSavedAuditVerdicts(repoRoot, zipIdentity.TaskId);
        if (options.ControlAudit)
        {
            var primaryAccept = verdicts.Any(verdict =>
                !verdict.Control &&
                verdict.IterationNumber == zipIdentity.IterationNumber &&
                string.Equals(verdict.FirstLine, "VERDICT: ACCEPT", StringComparison.Ordinal));
            if (!primaryAccept)
            {
                throw new AuditPackageFailure(
                    "audit submit",
                    "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                    $"--control-audit requires a saved primary VERDICT: ACCEPT report for {zipIdentity.TaskId} {zipIdentity.Iteration}.",
                    ZipPath: zipPath);
            }

            ValidateControlAuditCleanContext(zipPath);
            return;
        }

        var latestPreviousNeedsFixes = verdicts
            .Where(verdict => verdict.IterationNumber < zipIdentity.IterationNumber)
            .OrderByDescending(verdict => verdict.IterationNumber)
            .ThenByDescending(verdict => verdict.Control)
            .FirstOrDefault()
            ?.FirstLine == "VERDICT: NEEDS_FIXES";
        if (latestPreviousNeedsFixes &&
            !options.ReuseConversation &&
            !options.NewConversation &&
            !AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(options.ProjectUrl))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-REQUIRED",
                $"Primary audit iteration {zipIdentity.Iteration} must reuse the saved task conversation after a saved VERDICT: NEEDS_FIXES report.",
                ZipPath: zipPath);
        }
    }

    private static void ValidateControlAuditCleanContext(string zipPath)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zipPath);
            ValidateControlAuditMetadata(zipPath, archive);
            ValidateControlAuditEntryPaths(zipPath, archive);
            ValidateControlAuditProcessHistoryEntries(zipPath, archive);
            ValidateControlAuditRepoFileList(zipPath, archive);
            ValidateControlAuditSnapshotIndex(zipPath, archive);
            ValidateControlAuditTextArtifactContent(zipPath, archive);
        }
        catch (InvalidDataException exception)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-ZIP-INVALID",
                $"--control-audit requires a readable audit ZIP: {exception.Message}",
                ZipPath: zipPath);
        }
        catch (JsonException exception)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-ZIP-INVALID",
                $"--control-audit requires readable JSON metadata inside the audit ZIP: {exception.Message}",
                ZipPath: zipPath);
        }
    }

    private static void ValidateControlAuditMetadata(string zipPath, ZipArchive archive)
    {
        var entry = archive.GetEntry(AuditPackageInputEntryName)
            ?? throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-ZIP-INVALID",
                $"--control-audit requires audit package metadata entry {AuditPackageInputEntryName}.",
                ZipPath: zipPath);
        using var stream = entry.Open();
        using var document = JsonDocument.Parse(stream);
        ValidateControlAuditMetadataArrayEmpty(zipPath, document.RootElement, "previousVerdictChain");
        ValidateControlAuditMetadataArrayEmpty(zipPath, document.RootElement, "blockerClosureList");
    }

    private static void ValidateControlAuditMetadataArrayEmpty(string zipPath, JsonElement metadata, string propertyName)
    {
        if (!metadata.TryGetProperty(propertyName, out var chain))
        {
            throw ControlContextFailure(zipPath, $"metadata.{propertyName} must be present as an empty array for --control-audit.");
        }

        if (chain.ValueKind != JsonValueKind.Array)
        {
            throw ControlContextFailure(zipPath, $"metadata.{propertyName} must be an empty array for --control-audit.");
        }

        var itemCount = chain.GetArrayLength();
        if (itemCount > 0)
        {
            throw ControlContextFailure(
                zipPath,
                $"metadata.{propertyName} must be empty for --control-audit; found {itemCount.ToString(CultureInfo.InvariantCulture)} element(s).");
        }
    }

    private static void ValidateControlAuditEntryPaths(string zipPath, ZipArchive archive)
    {
        var verdictSnapshots = archive.Entries
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Where(path =>
                path.StartsWith("repo-after/" + VerdictDocsPrefix, StringComparison.Ordinal) ||
                path.StartsWith("repo-before/" + VerdictDocsPrefix, StringComparison.Ordinal))
            .ToArray();
        if (verdictSnapshots.Length > 0)
        {
            throw ControlContextFailure(
                zipPath,
                $"--control-audit ZIP must not include saved verdict report snapshots; found {FormatPathList(verdictSnapshots)}.");
        }
    }

    private static void ValidateControlAuditRepoFileList(string zipPath, ZipArchive archive)
    {
        var entry = archive.GetEntry(RepoFileHashesEntryName);
        if (entry is null)
        {
            return;
        }

        using var stream = entry.Open();
        using var document = JsonDocument.Parse(stream);
        var verdictRepoFiles = FindVerdictContextStrings(document.RootElement).ToArray();
        if (verdictRepoFiles.Length > 0)
        {
            throw ControlContextFailure(
                zipPath,
                $"--control-audit ZIP repo file model must not include saved verdict reports; found {FormatPathList(verdictRepoFiles)}.");
        }
    }

    private static void ValidateControlAuditProcessHistoryEntries(string zipPath, ZipArchive archive)
    {
        var processHistoryEntries = archive.Entries
            .Select(entry => entry.FullName.Replace('\\', '/'))
            .Where(IsControlAuditProcessHistoryEntry)
            .ToArray();
        if (processHistoryEntries.Length > 0)
        {
            throw ControlContextFailure(
                zipPath,
                $"--control-audit ZIP must not include mutable process-history files; found {FormatPathList(processHistoryEntries)}.");
        }
    }

    private static void ValidateControlAuditSnapshotIndex(string zipPath, ZipArchive archive)
    {
        var entry = archive.GetEntry(RepoFileSnapshotsEntryName);
        if (entry is null)
        {
            return;
        }

        using var stream = entry.Open();
        using var document = JsonDocument.Parse(stream);
        var verdictSnapshotPaths = FindVerdictContextStrings(document.RootElement).ToArray();
        if (verdictSnapshotPaths.Length > 0)
        {
            throw ControlContextFailure(
                zipPath,
                $"--control-audit ZIP snapshot index must not include saved verdict reports; found {FormatPathList(verdictSnapshotPaths)}.");
        }
    }

    private static void ValidateControlAuditTextArtifactContent(string zipPath, ZipArchive archive)
    {
        var references = new List<string>();
        foreach (var entry in archive.Entries)
        {
            var entryPath = entry.FullName.Replace('\\', '/');
            if (!ShouldScanControlAuditTextEntry(entryPath, entry))
            {
                continue;
            }

            using var stream = entry.Open();
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var text = reader.ReadToEnd().Replace('\\', '/');
            var matches = entryPath.EndsWith(".patch", StringComparison.Ordinal)
                ? FindSavedVerdictReportReferencesInPatchAdditions(text)
                : SavedVerdictReportReferenceExpression.Matches(text)
                    .Cast<Match>()
                    .Select(match => match.Groups["path"].Value);
            foreach (var match in matches)
            {
                references.Add($"{entryPath}: {match}");
            }
        }

        if (references.Count > 0)
        {
            var distinctReferences = references.Distinct(StringComparer.Ordinal).ToArray();
            throw ControlContextFailure(
                zipPath,
                $"--control-audit ZIP active text artifacts must not reference saved verdict reports; found {FormatPathList(distinctReferences)}.");
        }
    }

    private static bool ShouldScanControlAuditTextEntry(string entryPath, ZipArchiveEntry entry)
    {
        if (entry.Length == 0 || entryPath.EndsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        if (entryPath.StartsWith("repo-before/", StringComparison.Ordinal))
        {
            return false;
        }

        if (entryPath.StartsWith("repo-after/data/documentation/", StringComparison.Ordinal) ||
            entryPath.StartsWith("evidence/", StringComparison.Ordinal) ||
            entryPath.StartsWith("metadata/", StringComparison.Ordinal))
        {
            return IsLikelyTextArchivePath(entryPath);
        }

        return string.Equals(entryPath, "AUDIT-MANIFEST.md", StringComparison.Ordinal) ||
            string.Equals(entryPath, "AUDIT-REQUEST.md", StringComparison.Ordinal) ||
            string.Equals(entryPath, "repo-file-hashes.json", StringComparison.Ordinal) ||
            string.Equals(entryPath, "SHA256SUMS.txt", StringComparison.Ordinal) ||
            entryPath.EndsWith(".patch", StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindSavedVerdictReportReferencesInPatchAdditions(string patchText)
    {
        string? currentPath = null;
        foreach (var rawLine in patchText.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.StartsWith("+++ ", StringComparison.Ordinal))
            {
                currentPath = ParsePatchNewPath(line);
                continue;
            }

            if (!line.StartsWith("+", StringComparison.Ordinal) ||
                line.StartsWith("+++", StringComparison.Ordinal))
            {
                continue;
            }

            if (!ShouldScanControlAuditPatchAdditionsForPath(currentPath))
            {
                continue;
            }

            foreach (Match match in SavedVerdictReportReferenceExpression.Matches(line))
            {
                yield return match.Groups["path"].Value;
            }
        }
    }

    private static string? ParsePatchNewPath(string line)
    {
        const string prefix = "+++ b/";
        return line.StartsWith(prefix, StringComparison.Ordinal)
            ? line[prefix.Length..]
            : null;
    }

    private static bool ShouldScanControlAuditPatchAdditionsForPath(string? relativePath)
    {
        return relativePath is not null &&
            (relativePath.StartsWith("data/documentation/", StringComparison.Ordinal) ||
            string.Equals(relativePath, "docs/release-management/AUDIT-REQUEST.md", StringComparison.Ordinal));
    }

    private static bool IsControlAuditProcessHistoryEntry(string entryPath)
    {
        const string afterPrefix = "repo-after/";
        const string beforePrefix = "repo-before/";
        if (entryPath.StartsWith(afterPrefix, StringComparison.Ordinal))
        {
            return IsControlAuditProcessHistoryPath(entryPath[afterPrefix.Length..]);
        }

        if (entryPath.StartsWith(beforePrefix, StringComparison.Ordinal))
        {
            return IsControlAuditProcessHistoryPath(entryPath[beforePrefix.Length..]);
        }

        return false;
    }

    private static bool IsControlAuditProcessHistoryPath(string relativePath)
    {
        return string.Equals(relativePath, "TASKS.md", StringComparison.Ordinal) ||
            relativePath.StartsWith("data/dev-diary/", StringComparison.Ordinal);
    }

    private static bool IsLikelyTextArchivePath(string entryPath)
    {
        var extension = Path.GetExtension(entryPath);
        return string.Equals(extension, ".cs", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".csproj", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".ndjson", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".txt", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".xml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".trx", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".log", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".csv", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".yml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".yaml", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".props", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".targets", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".config", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".editorconfig", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".gitignore", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(extension, ".patch", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> FindVerdictContextStrings(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    foreach (var path in FindVerdictContextStrings(property.Value))
                    {
                        yield return path;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var path in FindVerdictContextStrings(item))
                    {
                        yield return path;
                    }
                }

                break;
            case JsonValueKind.String:
                var normalized = element.GetString()?.Replace('\\', '/');
                if (normalized is not null &&
                    (normalized.StartsWith(VerdictDocsPrefix, StringComparison.Ordinal) ||
                    normalized.StartsWith("repo-after/" + VerdictDocsPrefix, StringComparison.Ordinal) ||
                    normalized.StartsWith("repo-before/" + VerdictDocsPrefix, StringComparison.Ordinal)))
                {
                    yield return normalized;
                }

                break;
        }
    }

    private static AuditPackageFailure ControlContextFailure(string zipPath, string detail)
    {
        return new AuditPackageFailure(
            "audit submit",
            "E2D-BUILD-AUDIT-SUBMIT-CONTROL-CONTEXT",
            detail,
            ZipPath: zipPath);
    }

    private static string FormatPathList(IReadOnlyCollection<string> paths)
    {
        const int maxShownPaths = 5;
        var shown = paths.Take(maxShownPaths).ToArray();
        var suffix = paths.Count > shown.Length
            ? $" and {paths.Count - shown.Length} more"
            : string.Empty;
        return string.Join(", ", shown) + suffix;
    }

    private static IReadOnlyList<SavedAuditVerdict> ReadSavedAuditVerdicts(string repoRoot, string taskId)
    {
        var verdictsRoot = Path.Combine(repoRoot, "docs", "verdicts");
        if (!Directory.Exists(verdictsRoot))
        {
            return [];
        }

        var reports = new List<SavedAuditVerdict>();
        foreach (var path in Directory.EnumerateFiles(verdictsRoot, "*.md", SearchOption.AllDirectories))
        {
            var match = AuditVerdictFileNameExpression.Match(Path.GetFileName(path));
            if (!match.Success ||
                !string.Equals(match.Groups["task"].Value, taskId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var firstLine = ReadFirstNonEmptyLine(path);
            if (firstLine is not ("VERDICT: ACCEPT" or "VERDICT: NEEDS_FIXES"))
            {
                continue;
            }

            var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
            reports.Add(new SavedAuditVerdict(
                taskId,
                iteration,
                ParseIterationNumber(iteration),
                match.Groups["control"].Success,
                firstLine,
                path));
        }

        return reports;
    }

    private static string? ReadFirstNonEmptyLine(string path)
    {
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                return line.Trim();
            }
        }

        return null;
    }

    private static int ParseIterationNumber(string iteration)
    {
        return int.Parse(iteration[1..], NumberStyles.None, CultureInfo.InvariantCulture);
    }

    private static (string SessionId, string TurnId) ResolveCodexBrowserIdentity(IReadOnlyDictionary<string, string?> values)
    {
        var configuredSession = values.TryGetValue("--codex-session-id", out var sessionValue) && !string.IsNullOrWhiteSpace(sessionValue)
            ? sessionValue
            : null;
        var configuredTurn = values.TryGetValue("--codex-turn-id", out var turnValue) && !string.IsNullOrWhiteSpace(turnValue)
            ? turnValue
            : null;
        if (configuredSession is not null || configuredTurn is not null)
        {
            return configuredSession is not null && configuredTurn is not null
                ? (configuredSession, configuredTurn)
                : throw InvalidArguments("--codex-session-id and --codex-turn-id must be supplied together.");
        }

        var environmentSession = Environment.GetEnvironmentVariable("CODEX_SESSION_ID");
        var environmentTurn = Environment.GetEnvironmentVariable("CODEX_TURN_ID");
        if (!string.IsNullOrWhiteSpace(environmentSession) && !string.IsNullOrWhiteSpace(environmentTurn))
        {
            return (environmentSession, environmentTurn);
        }

        return (
            Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture),
            Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture));
    }

    private static string ResolvePath(string root, string path)
    {
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
    }

    private static Dictionary<string, string?> ParseNamedArguments(
        string[] args,
        int startIndex,
        string[] allowedValueOptions,
        string[] allowedFlags)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        var valueOptions = allowedValueOptions.ToHashSet(StringComparer.Ordinal);
        var flags = allowedFlags.ToHashSet(StringComparer.Ordinal);

        for (var i = startIndex; i < args.Length; i++)
        {
            var current = args[i];
            if (flags.Contains(current))
            {
                if (!values.TryAdd(current, null))
                {
                    throw InvalidArguments($"Duplicate option: {current}");
                }

                continue;
            }

            if (!valueOptions.Contains(current))
            {
                throw InvalidArguments($"Unexpected option: {current}");
            }

            if (i + 1 >= args.Length || string.IsNullOrWhiteSpace(args[i + 1]) || args[i + 1].StartsWith("--", StringComparison.Ordinal))
            {
                throw InvalidArguments($"Missing value for {current}.");
            }

            if (!values.TryAdd(current, args[++i]))
            {
                throw InvalidArguments($"Duplicate option: {current}");
            }
        }

        return values;
    }

    private static string Require(IReadOnlyDictionary<string, string?> values, string key)
    {
        return values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw InvalidArguments($"Missing required option: {key}.");
    }

    private static int ParsePositiveInt(string value, string option)
    {
        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw InvalidArguments($"{option} must be a positive integer.");
        }

        return parsed;
    }

    private static AuditPackageFailure InvalidArguments(string message)
    {
        return new AuditPackageFailure("audit submit", "E2D-BUILD-CLI-INVALID-ARGUMENTS", message);
    }
}

internal static class AuditSubmitUrlRules
{
    public static bool IsConcreteChatGptConversationUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(uri.Host, "chatgpt.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 0; index < segments.Length - 1; index++)
        {
            if (string.Equals(segments[index], "c", StringComparison.Ordinal) &&
                !string.IsNullOrWhiteSpace(segments[index + 1]))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed record AuditSubmitZipIdentity(string TaskId, string Iteration, int IterationNumber);

internal sealed record AuditSubmitOutputPathIdentity(AuditSubmitZipIdentity ZipIdentity, bool Control);

internal sealed record SavedAuditVerdict(
    string TaskId,
    string Iteration,
    int IterationNumber,
    bool Control,
    string FirstLine,
    string Path);

internal static class AuditSubmitReportExtractor
{
    private static readonly string[] RequiredFinalReportHeadings =
    [
        "TASK_ASSESSMENT:",
        "BLOCKERS:",
        "EVIDENCE_REVIEW:",
        "RISKS_AND_NOTES:",
        "CLOSURE_DECISION:"
    ];

    public static AuditSubmitReportExtraction Extract(IReadOnlyList<AuditSubmitReportCandidate> reportCandidates, bool generationComplete = true)
    {
        if (!generationComplete)
        {
            return Invalid("Report generation is still active; a final Markdown export is not ready.");
        }

        if (reportCandidates.Count != 1)
        {
            return Invalid($"Expected exactly one Markdown report candidate, but found {reportCandidates.Count}.");
        }

        var candidate = reportCandidates[0];
        if (candidate.Source is not AuditSubmitReportCandidateSource.OpenedReportCard and not AuditSubmitReportCandidateSource.AssistantMessage)
        {
            return Invalid("The Markdown candidate must come from AuditSubmitReportCandidateSource.OpenedReportCard or AuditSubmitReportCandidateSource.AssistantMessage.");
        }

        var report = NormalizeNewlines(candidate.Text).Trim();
        if (string.IsNullOrWhiteSpace(report))
        {
            return Invalid("The Markdown report is empty.");
        }

        if (report.StartsWith("Вы сказали", StringComparison.OrdinalIgnoreCase) ||
            report.StartsWith("You said", StringComparison.OrdinalIgnoreCase))
        {
            return Invalid("The Markdown report looks like a prompt echo, not the final audit report.");
        }

        if (LooksLikePromptTemplate(report))
        {
            return Invalid("The Markdown report looks like the prompt template, not the final audit report.");
        }

        var firstLine = report
            .Split('\n', StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (firstLine is "VERDICT: ACCEPT" or "VERDICT: NEEDS_FIXES")
        {
            if (firstLine == "VERDICT: ACCEPT" && !AcceptHasNoNumberedBlockers(report))
            {
                return Invalid("VERDICT: ACCEPT report contains a numbered blocker in BLOCKERS.");
            }

            return TryValidateRequiredFinalReportHeadings(report, out var headingFailure)
                ? new AuditSubmitReportExtraction(true, report, null)
                : Invalid(headingFailure);
        }

        return Invalid("The first non-empty line must be exactly VERDICT: ACCEPT or VERDICT: NEEDS_FIXES.");
    }

    private static AuditSubmitReportExtraction Invalid(string reason)
    {
        return new AuditSubmitReportExtraction(false, null, reason);
    }

    private static bool LooksLikePromptTemplate(string report)
    {
        return report.Contains("Перечислите все доказуемые blocker-ы", StringComparison.Ordinal) ||
            report.Contains("Какие файлы, тесты, документация и доказательства были проверены", StringComparison.Ordinal) ||
            report.Contains("For each blocker", StringComparison.OrdinalIgnoreCase) ||
            report.Contains("Which files, tests, documentation, and evidence were reviewed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateRequiredFinalReportHeadings(string report, out string failureReason)
    {
        var lines = report.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var previousIndex = -1;
        foreach (var heading in RequiredFinalReportHeadings)
        {
            var index = Array.FindIndex(lines, previousIndex + 1, line => string.Equals(line, heading, StringComparison.Ordinal));
            if (index < 0)
            {
                failureReason = $"The Markdown report is missing required heading {heading} after the previous heading.";
                return false;
            }

            previousIndex = index;
        }

        failureReason = string.Empty;
        return true;
    }

    private static bool AcceptHasNoNumberedBlockers(string report)
    {
        var blockers = ExtractSection(report, "BLOCKERS:");
        return !Regex.IsMatch(blockers, @"(^|[\s`'""*-])B[1-9]\d*\b", RegexOptions.IgnoreCase);
    }

    private static string ExtractSection(string report, string heading)
    {
        var lines = report.Split('\n');
        var start = Array.FindIndex(lines, line => string.Equals(line.Trim(), heading, StringComparison.Ordinal));
        if (start < 0)
        {
            return string.Empty;
        }

        var headingIndex = Array.IndexOf(RequiredFinalReportHeadings, heading);
        var end = lines.Length;
        if (headingIndex >= 0)
        {
            foreach (var nextHeading in RequiredFinalReportHeadings.Skip(headingIndex + 1))
            {
                var next = Array.FindIndex(
                    lines,
                    start + 1,
                    line => string.Equals(line.Trim(), nextHeading, StringComparison.Ordinal));
                if (next >= 0)
                {
                    end = next;
                    break;
                }
            }
        }

        return string.Join('\n', lines.Skip(start + 1).Take(end - start - 1));
    }

    private static string NormalizeNewlines(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}

internal sealed class AuditSubmitReportStabilityTracker
{
    private static readonly TimeSpan MinimumStableReportAge = TimeSpan.FromSeconds(5);
    private readonly Func<DateTimeOffset> utcNow;
    private string? candidateReport;
    private DateTimeOffset candidateFirstSeenUtc;

    public AuditSubmitReportStabilityTracker()
        : this(() => DateTimeOffset.UtcNow)
    {
    }

    internal AuditSubmitReportStabilityTracker(Func<DateTimeOffset> utcNow)
    {
        this.utcNow = utcNow;
    }

    public AuditSubmitPollingDecision Decide(IReadOnlyList<AuditSubmitReportCandidate> reportCandidates, bool isGenerating)
    {
        var decision = AuditSubmitPollingPolicy.Decide(reportCandidates, isGenerating);
        if (decision.Action != AuditSubmitPollingAction.ReturnReport || decision.Report is null)
        {
            candidateReport = null;
            candidateFirstSeenUtc = default;
            return decision;
        }

        var now = utcNow();
        if (!string.Equals(candidateReport, decision.Report, StringComparison.Ordinal))
        {
            candidateReport = decision.Report;
            candidateFirstSeenUtc = now;
            return new AuditSubmitPollingDecision(AuditSubmitPollingAction.Wait, null, AuditSubmitPollingReason.Stabilizing);
        }

        if (now - candidateFirstSeenUtc < MinimumStableReportAge)
        {
            return new AuditSubmitPollingDecision(AuditSubmitPollingAction.Wait, null, AuditSubmitPollingReason.Stabilizing);
        }

        return decision;
    }
}

internal static class AuditSubmitPollingPolicy
{
    public static AuditSubmitPollingDecision Decide(IReadOnlyList<AuditSubmitReportCandidate> reportCandidates, bool isGenerating)
    {
        var extraction = AuditSubmitReportExtractor.Extract(reportCandidates, generationComplete: !isGenerating);
        if (extraction.Ready && extraction.Report is not null)
        {
            return new AuditSubmitPollingDecision(AuditSubmitPollingAction.ReturnReport, extraction.Report);
        }

        return isGenerating
            ? new AuditSubmitPollingDecision(AuditSubmitPollingAction.Wait, null, AuditSubmitPollingReason.Generating)
            : new AuditSubmitPollingDecision(AuditSubmitPollingAction.Reload, null);
    }

}

internal enum AuditSubmitPollingAction
{
    Wait,
    Reload,
    ReturnReport
}

internal enum AuditSubmitPollingReason
{
    None,
    Generating,
    Stabilizing
}

internal readonly record struct AuditSubmitPollingDecision(AuditSubmitPollingAction Action, string? Report, AuditSubmitPollingReason Reason = AuditSubmitPollingReason.None);

internal sealed record AuditSubmitReportExtraction(bool Ready, string? Report, string? FailureReason);

internal enum AuditSubmitReportCandidateSource
{
    Other,
    OpenedReportCard,
    AssistantMessage
}

internal readonly record struct AuditSubmitReportCandidate(string Text, AuditSubmitReportCandidateSource Source);

internal interface IAuditSubmitBrowserOptions
{
    string ProjectUrl { get; }

    int LoginTimeoutMinutes { get; }

    string? CodexChromePipe { get; }

    string CodexSessionId { get; }

    string CodexTurnId { get; }

    bool KeepTabOpenOnError { get; }

    bool NewConversation { get; }
}

internal sealed record AuditSubmitOptions(
    string? ZipPath,
    string OutputPath,
    string? MessagePath,
    string ProjectUrl,
    int PollSeconds,
    int TimeoutMinutes,
    int LoginTimeoutMinutes,
    bool AllowFastPoll,
    bool DownloadReportOnly,
    bool DumpDomOnly,
    string? DomDumpDirectory,
    string? CodexChromePipe,
    string CodexSessionId,
    string CodexTurnId,
    bool KeepTabOpenOnError,
    bool ReuseConversation,
    bool ControlAudit,
    bool NewConversation,
    bool DeepResearch) : IAuditSubmitBrowserOptions;
