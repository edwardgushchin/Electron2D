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
using System.Text;
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class AuditSubmitCommand
{
    private const string DefaultProjectUrl = "https://chatgpt.com/g/g-p-6950376d4d8c8191a0fe600e98389912-electro2d/project";
    private const int DefaultPollSeconds = 60;
    private const int MinimumOperationalPollSeconds = 60;
    private const int DefaultTimeoutMinutes = 180;
    private const int DefaultLoginTimeoutMinutes = 10;

    public async Task RunAsync(string[] args, CancellationToken cancellationToken)
    {
        var options = ParseOptions(args);
        ValidatePolling(options);

        var repoRoot = Directory.GetCurrentDirectory();
        string report;
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

            var message = await ResolveSubmitMessageAsync(options, repoRoot, zipPath, cancellationToken).ConfigureAwait(false);
            report = await new AuditSubmitCodexChromeAutomation()
                .SubmitAndWaitForReportAsync(options, repoRoot, zipPath, message, cancellationToken)
                .ConfigureAwait(false);
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
                "--screenshots-dir",
                "--dom-dump-dir",
                "--poll-seconds",
                "--timeout-minutes",
                "--login-timeout-minutes",
                "--browser-backend",
                "--codex-chrome-pipe",
                "--codex-session-id",
                "--codex-turn-id"
            ],
            allowedFlags: ["--allow-fast-poll", "--download-report-only", "--dump-dom-only", "--keep-tab-open-on-error"]);

        ValidateBrowserBackend(values);
        var downloadReportOnly = values.ContainsKey("--download-report-only");
        var dumpDomOnly = values.ContainsKey("--dump-dom-only");
        if (downloadReportOnly && dumpDomOnly)
        {
            throw InvalidArguments("--download-report-only is not accepted together with --dump-dom-only.");
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
        var messagePath = values.TryGetValue("--message", out var configuredMessagePath) && !string.IsNullOrWhiteSpace(configuredMessagePath)
            ? configuredMessagePath
            : null;
        var projectUrl = values.TryGetValue("--project-url", out var configuredProjectUrl) && !string.IsNullOrWhiteSpace(configuredProjectUrl)
            ? configuredProjectUrl
            : DefaultProjectUrl;
        var screenshotsDirectory = values.TryGetValue("--screenshots-dir", out var configuredScreenshotsDirectory) && !string.IsNullOrWhiteSpace(configuredScreenshotsDirectory)
            ? configuredScreenshotsDirectory
            : null;
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
            screenshotsDirectory,
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
            keepTabOpenOnError);
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
        if (!generationComplete || reportCandidates.Count != 1)
        {
            return new AuditSubmitReportExtraction(false, null);
        }

        var candidate = reportCandidates[0];
        if (candidate.Source != AuditSubmitReportCandidateSource.OpenedReportCard)
        {
            return new AuditSubmitReportExtraction(false, null);
        }

        var report = NormalizeNewlines(candidate.Text).Trim();
        if (string.IsNullOrWhiteSpace(report) ||
            report.StartsWith("Вы сказали", StringComparison.OrdinalIgnoreCase) ||
            report.StartsWith("You said", StringComparison.OrdinalIgnoreCase) ||
            LooksLikePromptTemplate(report))
        {
            return new AuditSubmitReportExtraction(false, null);
        }

        var firstLine = report
            .Split('\n', StringSplitOptions.TrimEntries)
            .FirstOrDefault(line => !string.IsNullOrWhiteSpace(line));
        if (firstLine is "VERDICT: ACCEPT" or "VERDICT: NEEDS_FIXES")
        {
            if (firstLine == "VERDICT: ACCEPT" && (!AcceptHasNoNumberedBlockers(report) || !ClosureDecisionAllowsClose(report)))
            {
                return new AuditSubmitReportExtraction(false, null);
            }

            return HasRequiredFinalReportHeadings(report)
                ? new AuditSubmitReportExtraction(true, report)
                : new AuditSubmitReportExtraction(false, null);
        }

        return new AuditSubmitReportExtraction(false, null);
    }

    private static bool LooksLikePromptTemplate(string report)
    {
        return report.Contains("Перечислите все доказуемые blocker-ы", StringComparison.Ordinal) ||
            report.Contains("Какие файлы, тесты, документация и доказательства были проверены", StringComparison.Ordinal) ||
            report.Contains("For each blocker", StringComparison.OrdinalIgnoreCase) ||
            report.Contains("Which files, tests, documentation, and evidence were reviewed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasRequiredFinalReportHeadings(string report)
    {
        var lines = report.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var previousIndex = -1;
        foreach (var heading in RequiredFinalReportHeadings)
        {
            var index = Array.FindIndex(lines, previousIndex + 1, line => string.Equals(line, heading, StringComparison.Ordinal));
            if (index < 0)
            {
                return false;
            }

            previousIndex = index;
        }

        return true;
    }

    private static bool AcceptHasNoNumberedBlockers(string report)
    {
        var blockers = ExtractSection(report, "BLOCKERS:");
        return !Regex.IsMatch(blockers, @"(^|[\s`'""*-])B[1-9]\d*\b", RegexOptions.IgnoreCase);
    }

    private static bool ClosureDecisionAllowsClose(string report)
    {
        var closure = ExtractSection(report, "CLOSURE_DECISION:");
        if (string.IsNullOrWhiteSpace(closure))
        {
            return false;
        }

        var normalized = closure.Replace('ё', 'е');
        var forbidden = new[]
        {
            "do not close",
            "cannot close",
            "must remain open",
            "remains open",
            "not accepted",
            "не закры",
            "нельзя закры",
            "не может быть закры",
            "остается откры",
            "задача остается откры",
            "задачу нельзя"
        };
        if (forbidden.Any(pattern => normalized.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var allowed = new[]
        {
            @"\b(?:the\s+)?task\s+can\s+be\s+closed\b",
            @"\bready\s+to\s+close\b",
            @"\bclosure\s+approved\b",
            @"\bclose\s+the\s+task\b",
            @"задач[ау]\s+можно\s+закрыт[ьи]\b",
            @"задача\s+может\s+быть\s+закрыта\b",
            @"закрытие\s+задачи\s+разрешено\b",
            @"разрешено\s+закрыть\s+задачу\b"
        };

        return allowed.Any(pattern => Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
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

    public static string CreateScreenshotName(AuditSubmitPollingDecision decision, int poll)
    {
        return decision.Action == AuditSubmitPollingAction.Wait
            ? decision.Reason == AuditSubmitPollingReason.Stabilizing ? $"stabilizing-{poll:000}" : $"generating-{poll:000}"
            : $"waiting-{poll:000}";
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

internal sealed record AuditSubmitReportExtraction(bool Ready, string? Report);

internal enum AuditSubmitReportCandidateSource
{
    Other,
    OpenedReportCard
}

internal readonly record struct AuditSubmitReportCandidate(string Text, AuditSubmitReportCandidateSource Source);

internal interface IAuditSubmitBrowserOptions
{
    string ProjectUrl { get; }

    string? ScreenshotsDirectory { get; }

    int LoginTimeoutMinutes { get; }

    string? CodexChromePipe { get; }

    string CodexSessionId { get; }

    string CodexTurnId { get; }

    bool KeepTabOpenOnError { get; }
}

internal sealed record AuditSubmitOptions(
    string? ZipPath,
    string OutputPath,
    string? MessagePath,
    string ProjectUrl,
    string? ScreenshotsDirectory,
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
    bool KeepTabOpenOnError) : IAuditSubmitBrowserOptions;
