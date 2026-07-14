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
    private static readonly Regex SubmitAttemptFileNameExpression = new(
        @"^submit-attempt-(?<iteration>r\d+)\.json$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex SavedVerdictReportReferenceExpression = new(
        @"(?<path>(?:repo-after/|repo-before/)?docs/verdicts/[^\s`'""<>*?]+\.md)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly JsonSerializerOptions SubmitAttemptJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

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
            var submitOptions = ResolveAutomaticSubmitRoute(options, repoRoot, zipPath, zipIdentity);
            var message = await ResolveSubmitMessageAsync(submitOptions, repoRoot, zipPath, cancellationToken).ConfigureAwait(false);
            if (submitOptions.ReuseConversation)
            {
                submitOptions = submitOptions with
                {
                    ProjectUrl = await ResolveStoredConversationUrlAsync(repoRoot, zipPath, zipIdentity, cancellationToken).ConfigureAwait(false)
                };
            }

            ReserveSubmitAttempt(repoRoot, zipPath, zipIdentity, submitOptions);
            report = await ExecuteReservedSubmitAsync(
                repoRoot,
                zipPath,
                options.OutputPath,
                zipIdentity,
                async () => await new AuditSubmitCodexChromeAutomation()
                    .SubmitAndWaitForReportAsync(
                        submitOptions,
                        repoRoot,
                        zipPath,
                        message,
                        tabId => UpdateSubmitAttempt(
                            repoRoot,
                            zipIdentity,
                            reservation => reservation with { Status = "browser-started", TabId = tabId }),
                        conversationUrl => UpdateSubmitAttempt(
                            repoRoot,
                            zipIdentity,
                            reservation => reservation with { Status = "submitted", ConversationUrl = conversationUrl }),
                        cancellationToken)
                    .ConfigureAwait(false),
                cancellationToken).ConfigureAwait(false);

            WriteReportToConsole(report);
            return;
        }

        var expectedReportIdentity = zipIdentity ?? (options.DownloadReportOnly ? ResolveAuditReportOutputIdentity(options.OutputPath) : null);
        if (expectedReportIdentity is not null)
        {
            ValidateReportMatchesSubmitIteration(report, expectedReportIdentity, options.ZipPath ?? options.OutputPath);
        }

        await WriteReportAsync(repoRoot, options.OutputPath, report, cancellationToken).ConfigureAwait(false);

        WriteReportToConsole(report);
    }

    private static async Task<string> ExecuteReservedSubmitAsync(
        string repoRoot,
        string zipPath,
        string outputPath,
        AuditSubmitZipIdentity zipIdentity,
        Func<Task<string>> receiveReportAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await receiveReportAsync().ConfigureAwait(false);
            UpdateSubmitAttempt(
                repoRoot,
                zipIdentity,
                reservation => reservation with { Status = "report-received" });
            ValidateReportMatchesSubmitIteration(report, zipIdentity, zipPath);
            await WriteReportAsync(repoRoot, outputPath, report, cancellationToken).ConfigureAwait(false);
            UpdateSubmitAttempt(
                repoRoot,
                zipIdentity,
                reservation => reservation with
                {
                    Status = "completed",
                    CompletedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                });
            return report;
        }
        catch (Exception exception)
        {
            var failure = exception as AuditPackageFailure ?? new AuditPackageFailure(
                "audit submit",
                exception is IOException or UnauthorizedAccessException
                    ? "E2D-BUILD-AUDIT-SUBMIT-REPORT-WRITE"
                    : "E2D-BUILD-AUDIT-SUBMIT-FAILED",
                exception.Message,
                ZipPath: zipPath);
            TryMarkSubmitAttemptFailed(repoRoot, zipIdentity, failure);
            if (ReferenceEquals(failure, exception))
            {
                throw;
            }

            throw failure;
        }
    }

    private static async Task WriteReportAsync(
        string repoRoot,
        string outputPathOption,
        string report,
        CancellationToken cancellationToken)
    {
        var outputPath = ResolvePath(repoRoot, outputPathOption);
        var outputDirectory = Path.GetDirectoryName(outputPath) ?? repoRoot;
        Directory.CreateDirectory(outputDirectory);
        if (File.Exists(outputPath))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-WRITE",
                $"Audit verdict already exists and is immutable: {outputPathOption}.");
        }

        var temporaryPath = Path.Combine(
            outputDirectory,
            $".{Path.GetFileName(outputPath)}.{Guid.NewGuid():N}.tmp");
        var payload = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
            .GetBytes(report + Environment.NewLine);
        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 16 * 1024,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, outputPath, overwrite: false);
        }
        catch (IOException ex) when (File.Exists(outputPath))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-WRITE",
                $"Audit verdict already exists and is immutable: {outputPathOption}. {ex.Message}");
        }
        finally
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }

    private static void WriteReportToConsole(string report)
    {
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
            allowedFlags: ["--allow-fast-poll", "--download-report-only", "--dump-dom-only", "--reuse-conversation", "--control-audit", "--new-conversation"]);

        ValidateBrowserBackend(values);
        var downloadReportOnly = values.ContainsKey("--download-report-only");
        var dumpDomOnly = values.ContainsKey("--dump-dom-only");
        var reuseConversation = values.ContainsKey("--reuse-conversation");
        var requestedControlAudit = values.ContainsKey("--control-audit");
        var controlAudit = requestedControlAudit;
        var newConversation = values.ContainsKey("--new-conversation");
        if (downloadReportOnly && dumpDomOnly)
        {
            throw InvalidArguments("--download-report-only is not accepted together with --dump-dom-only.");
        }

        if (reuseConversation && requestedControlAudit)
        {
            throw InvalidArguments("--reuse-conversation is not accepted together with --control-audit.");
        }

        if (newConversation && (reuseConversation || requestedControlAudit))
        {
            throw InvalidArguments("--new-conversation is not accepted together with --reuse-conversation or --control-audit.");
        }

        if ((downloadReportOnly || dumpDomOnly) &&
            (reuseConversation || newConversation || (requestedControlAudit && dumpDomOnly)))
        {
            throw InvalidArguments("--reuse-conversation and --new-conversation are accepted only for a submit that sends a ZIP; --control-audit is accepted with --download-report-only but not --dump-dom-only.");
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
            if (outputIdentity is null)
            {
                throw InvalidArguments("--download-report-only requires --out to use docs/verdicts/<domain>/<task-id>-audit-rNN.md or docs/verdicts/<domain>/<task-id>-audit-control-rNN.md so the current metadata.taskId and metadata.iteration can be validated.");
            }

            if (requestedControlAudit && !outputIdentity.Control)
            {
                throw InvalidArguments("--control-audit does not match the primary verdict filename selected by --out.");
            }

            controlAudit = outputIdentity.Control;
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
        if (!downloadReportOnly && !dumpDomOnly && hasProjectUrl)
        {
            throw InvalidArguments("A ZIP submit does not accept --project-url; the deterministic audit state machine selects the project root or stored conversation URL.");
        }

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

        if (controlAudit && !downloadReportOnly && AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(projectUrl))
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
            reuseConversation,
            controlAudit,
            newConversation);
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
        var latestPreviousVerdict = ReadSavedAuditVerdicts(repoRoot, zipIdentity.TaskId)
            .Where(verdict => verdict.IterationNumber < zipIdentity.IterationNumber)
            .OrderByDescending(verdict => verdict.IterationNumber)
            .ThenByDescending(verdict => verdict.Control)
            .FirstOrDefault();
        var sidecarFileName = latestPreviousVerdict is
        {
            Control: true,
            FirstLine: "VERDICT: NEEDS_FIXES"
        }
            ? "control-conversation-url.txt"
            : "conversation-url.txt";
        var relativePath = $".temp/audit/{zipIdentity.TaskId}/{sidecarFileName}";
        var path = Path.Combine(repoRoot, ".temp", "audit", zipIdentity.TaskId, sidecarFileName);
        if (!File.Exists(path))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
                $"Stored audit conversation URL was not found: {relativePath}",
                ZipPath: zipPath);
        }

        var url = (await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken).ConfigureAwait(false)).Trim();
        if (!AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(url))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
                $"Stored audit conversation URL is not a concrete ChatGPT conversation URL: {relativePath}",
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

    private static AuditSubmitOptions ResolveAutomaticSubmitRoute(
        AuditSubmitOptions options,
        string repoRoot,
        string zipPath,
        AuditSubmitZipIdentity zipIdentity)
    {
        var latestVerdict = ReadSavedAuditVerdicts(repoRoot, zipIdentity.TaskId)
            .OrderByDescending(verdict => verdict.IterationNumber)
            .ThenByDescending(verdict => verdict.Control)
            .FirstOrDefault();
        var reservations = ReadSubmitAttemptReservations(repoRoot, zipIdentity.TaskId);
        var latestReservation = reservations
            .OrderByDescending(reservation => reservation.IterationNumber)
            .FirstOrDefault();
        var latestVerdictReservation = latestVerdict is null
            ? null
            : reservations.SingleOrDefault(reservation => reservation.IterationNumber == latestVerdict.IterationNumber);
        var latestAcceptedControlIsClean = latestVerdict is
            {
                Control: true,
                FirstLine: "VERDICT: ACCEPT"
            } && latestVerdictReservation is
            {
                Route: "clean-control",
                Status: "completed"
            };

        if (latestAcceptedControlIsClean)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                $"The latest clean control for {zipIdentity.TaskId} is already VERDICT: ACCEPT; no further audit submission is allowed.",
                ZipPath: zipPath);
        }

        var latestKnownIterationNumber = Math.Max(
            latestVerdict?.IterationNumber ?? 0,
            latestReservation?.IterationNumber ?? 0);
        var expectedIterationNumber = latestKnownIterationNumber + 1;
        var legacyEmergencySkip = options.NewConversation &&
            zipIdentity.IterationNumber == expectedIterationNumber + 1;
        if (zipIdentity.IterationNumber != expectedIterationNumber && !legacyEmergencySkip)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                $"Audit ZIP iteration must be the next unreserved global submission number r{expectedIterationNumber:00} for {zipIdentity.TaskId}; received {zipIdentity.Iteration}.",
                ZipPath: zipPath);
        }

        if (options.NewConversation)
        {
            ValidateAuditSubmitOutputPath(options.OutputPath, zipIdentity, controlAudit: false);
            return options with
            {
                ProjectUrl = DefaultProjectUrl,
                ReuseConversation = false,
                ControlAudit = false
            };
        }

        var latestKnownIterationHasVerdict = latestVerdict is not null &&
            latestVerdict.IterationNumber == latestKnownIterationNumber;
        var latestReservationAllowsRouteContinuation = latestReservation is
        {
            Status: "failed",
            TabId: null,
            ConversationUrl: null
        } || latestReservation is
        {
            Status: "failed",
            FailureCode: "E2D-BUILD-AUDIT-SUBMIT-COMPOSER-STATE",
            ConversationUrl: null
        };
        if (!latestKnownIterationHasVerdict && !latestReservationAllowsRouteContinuation)
        {
            if (options.ControlAudit || options.ReuseConversation)
            {
                throw new AuditPackageFailure(
                    "audit submit",
                    "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                    "An unfinished reserved audit attempt requires the next global iteration to start as a fresh primary conversation.",
                    ZipPath: zipPath);
            }

            ValidateAuditSubmitOutputPath(options.OutputPath, zipIdentity, controlAudit: false);
            return options with
            {
                ProjectUrl = DefaultProjectUrl,
                ReuseConversation = false,
                ControlAudit = false,
                NewConversation = false
            };
        }

        var cleanControlAudit = latestVerdict is
        {
            Control: false,
            FirstLine: "VERDICT: ACCEPT"
        } || latestVerdict is
        {
            Control: true,
            FirstLine: "VERDICT: ACCEPT"
        } && !latestAcceptedControlIsClean;
        var correctiveControlAudit = latestVerdict is
        {
            Control: true,
            FirstLine: "VERDICT: NEEDS_FIXES"
        };
        var controlAudit = cleanControlAudit || correctiveControlAudit;
        var reuseConversation = latestVerdict?.FirstLine == "VERDICT: NEEDS_FIXES";
        if (options.ControlAudit && !controlAudit)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                "--control-audit does not match the route derived from the latest saved verdict.",
                ZipPath: zipPath);
        }

        if (options.ReuseConversation && !reuseConversation)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                "--reuse-conversation does not match the route derived from the latest saved verdict.",
                ZipPath: zipPath);
        }

        ValidateAuditSubmitOutputPath(options.OutputPath, zipIdentity, controlAudit);

        if (reuseConversation && options.MessagePath is not null)
        {
            throw InvalidArguments("A reused audit conversation does not accept --message; the original prompt is already present in the chat history.");
        }

        if (cleanControlAudit)
        {
            ValidateControlAuditCleanContext(zipPath);
        }

        return options with
        {
            ProjectUrl = reuseConversation ? string.Empty : DefaultProjectUrl,
            ReuseConversation = reuseConversation,
            ControlAudit = controlAudit,
            NewConversation = false
        };
    }

    private static void ReserveSubmitAttempt(
        string repoRoot,
        string zipPath,
        AuditSubmitZipIdentity zipIdentity,
        AuditSubmitOptions options)
    {
        var directory = Path.Combine(repoRoot, ".temp", "audit", zipIdentity.TaskId);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"submit-attempt-{zipIdentity.Iteration}.json");
        var route = options.ReuseConversation
            ? "reuse"
            : options.ControlAudit
                ? "clean-control"
                : "primary";
        var reservation = new AuditSubmitAttemptReservation(
            2,
            zipIdentity.TaskId,
            zipIdentity.Iteration,
            zipIdentity.IterationNumber,
            Path.GetFileName(zipPath),
            route,
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            "reserved",
            options.CodexSessionId,
            options.CodexTurnId,
            null,
            null,
            null,
            null,
            null);
        var json = JsonSerializer.Serialize(reservation, SubmitAttemptJsonOptions) + Environment.NewLine;

        try
        {
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(json);
        }
        catch (IOException) when (File.Exists(path))
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                $"Audit submit attempt {zipIdentity.TaskId} {zipIdentity.Iteration} is already reserved and cannot be repeated.",
                ZipPath: zipPath);
        }
    }

    private static void UpdateSubmitAttempt(
        string repoRoot,
        AuditSubmitZipIdentity zipIdentity,
        Func<AuditSubmitAttemptReservation, AuditSubmitAttemptReservation> update)
    {
        var reservation = ReadSubmitAttemptReservations(repoRoot, zipIdentity.TaskId)
            .SingleOrDefault(candidate => candidate.IterationNumber == zipIdentity.IterationNumber)
            ?? throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                $"Audit submit reservation {zipIdentity.TaskId} {zipIdentity.Iteration} was not found.");
        var path = GetSubmitAttemptPath(repoRoot, zipIdentity);
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(update(reservation), SubmitAttemptJsonOptions) + Environment.NewLine;
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void TryMarkSubmitAttemptFailed(
        string repoRoot,
        AuditSubmitZipIdentity zipIdentity,
        AuditPackageFailure failure)
    {
        try
        {
            UpdateSubmitAttempt(
                repoRoot,
                zipIdentity,
                reservation => reservation with
                {
                    Status = "failed",
                    CompletedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                    FailureCode = failure.Code,
                    FailureMessage = failure.Message
                });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or AuditPackageFailure)
        {
        }
    }

    private static string GetSubmitAttemptPath(string repoRoot, AuditSubmitZipIdentity zipIdentity)
    {
        return Path.Combine(
            repoRoot,
            ".temp",
            "audit",
            zipIdentity.TaskId,
            $"submit-attempt-{zipIdentity.Iteration}.json");
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
        return relativePath.StartsWith(".taskboard/", StringComparison.Ordinal) ||
            string.Equals(relativePath, "TASKS.md", StringComparison.Ordinal) ||
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

            var report = File.ReadAllText(path, Encoding.UTF8);
            var extraction = AuditSubmitReportExtractor.Extract(
                [new AuditSubmitReportCandidate(report, AuditSubmitReportCandidateSource.AssistantMessage)],
                generationComplete: true);
            if (!extraction.Ready || extraction.Report is null)
            {
                continue;
            }

            var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
            var identity = new AuditSubmitZipIdentity(taskId, iteration, ParseIterationNumber(iteration));
            try
            {
                ValidateReportMatchesSubmitIteration(extraction.Report, identity, path);
            }
            catch (AuditPackageFailure)
            {
                continue;
            }

            var firstLine = extraction.Report
                .Split('\n', StringSplitOptions.TrimEntries)
                .First(line => !string.IsNullOrWhiteSpace(line));
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

    private static IReadOnlyList<AuditSubmitAttemptReservation> ReadSubmitAttemptReservations(string repoRoot, string taskId)
    {
        var directory = Path.Combine(repoRoot, ".temp", "audit", taskId);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var reservations = new List<AuditSubmitAttemptReservation>();
        foreach (var path in Directory.EnumerateFiles(directory, "submit-attempt-r*.json", SearchOption.TopDirectoryOnly))
        {
            var match = SubmitAttemptFileNameExpression.Match(Path.GetFileName(path));
            if (!match.Success)
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8));
                var root = document.RootElement;
                var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
                var expectedIterationNumber = ParseIterationNumber(iteration);
                var schemaVersion = root.GetProperty("schemaVersion").GetInt32();
                var savedTaskId = root.GetProperty("taskId").GetString();
                var savedIteration = root.GetProperty("iteration").GetString();
                var iterationNumber = root.GetProperty("iterationNumber").GetInt32();
                var zipFileName = root.GetProperty("zipFileName").GetString();
                var route = root.GetProperty("route").GetString();
                var startedAtUtc = root.GetProperty("startedAtUtc").GetString();
                var status = schemaVersion == 1
                    ? "reserved"
                    : root.GetProperty("status").GetString();
                var browserSessionId = schemaVersion == 1
                    ? null
                    : root.GetProperty("browserSessionId").GetString();
                var browserTurnId = schemaVersion == 1
                    ? null
                    : root.GetProperty("browserTurnId").GetString();
                var tabId = ReadOptionalInt64(root, "tabId");
                var conversationUrl = ReadOptionalString(root, "conversationUrl");
                var completedAtUtc = ReadOptionalString(root, "completedAtUtc");
                var failureCode = ReadOptionalString(root, "failureCode");
                var failureMessage = ReadOptionalString(root, "failureMessage");
                var expectedZipFileName = $"{taskId}-audit-{iteration}.zip";
                var valid = schemaVersion is 1 or 2 &&
                    string.Equals(savedTaskId, taskId, StringComparison.Ordinal) &&
                    string.Equals(savedIteration, iteration, StringComparison.Ordinal) &&
                    iterationNumber == expectedIterationNumber &&
                    string.Equals(zipFileName, expectedZipFileName, StringComparison.OrdinalIgnoreCase) &&
                    route is "primary" or "reuse" or "clean-control" &&
                    IsRoundtripTimestamp(startedAtUtc) &&
                    (schemaVersion == 1 ||
                        (!string.IsNullOrWhiteSpace(browserSessionId) &&
                         !string.IsNullOrWhiteSpace(browserTurnId) &&
                         status is "reserved" or "browser-started" or "submitted" or "report-received" or "completed" or "failed" &&
                         (tabId is null || tabId > 0) &&
                         (conversationUrl is null || AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(conversationUrl)) &&
                         (completedAtUtc is null || IsRoundtripTimestamp(completedAtUtc)) &&
                         (status != "failed" ||
                            (!string.IsNullOrWhiteSpace(failureCode) &&
                             !string.IsNullOrWhiteSpace(failureMessage) &&
                             completedAtUtc is not null))));
                if (!valid)
                {
                    throw new InvalidDataException("The reservation fields do not match its filename and task identity.");
                }

                reservations.Add(new AuditSubmitAttemptReservation(
                    schemaVersion,
                    savedTaskId!,
                    savedIteration!,
                    iterationNumber,
                    zipFileName!,
                    route!,
                    startedAtUtc!,
                    status!,
                    browserSessionId,
                    browserTurnId,
                    tabId,
                    conversationUrl,
                    completedAtUtc,
                    failureCode,
                    failureMessage));
            }
            catch (Exception exception) when (exception is JsonException or InvalidOperationException or KeyNotFoundException or InvalidDataException)
            {
                throw new AuditPackageFailure(
                    "audit submit",
                    "E2D-BUILD-AUDIT-SUBMIT-VERDICT-STATE",
                    $"Audit submit reservation is invalid: {Path.GetFileName(path)}. {exception.Message}");
            }
        }

        return reservations;
    }

    private static string? ReadOptionalString(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : throw new InvalidDataException($"Reservation property {propertyName} must be a string or null.");
    }

    private static long? ReadOptionalInt64(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var number)
            ? number
            : throw new InvalidDataException($"Reservation property {propertyName} must be an integer or null.");
    }

    private static bool IsRoundtripTimestamp(string? value)
    {
        return DateTimeOffset.TryParseExact(
            value,
            "O",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind,
            out _);
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

internal sealed record AuditSubmitAttemptReservation(
    int SchemaVersion,
    string TaskId,
    string Iteration,
    int IterationNumber,
    string ZipFileName,
    string Route,
    string StartedAtUtc,
    string Status,
    string? BrowserSessionId,
    string? BrowserTurnId,
    long? TabId,
    string? ConversationUrl,
    string? CompletedAtUtc,
    string? FailureCode,
    string? FailureMessage);

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
    bool ReuseConversation,
    bool ControlAudit,
    bool NewConversation) : IAuditSubmitBrowserOptions;
