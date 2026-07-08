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
using System.Buffers.Binary;
using System.Globalization;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class AuditSubmitCodexChromeAutomation
{
    private static readonly TimeSpan UiActionTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan PromptPayloadReadyTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ReportHydrationDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ClipboardSettleDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan SystemClipboardReadTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan OrdinaryCopyFailureStableAge = TimeSpan.FromSeconds(30);
    private static readonly Regex AuditZipFileNameExpression = new(
        @"^(?<task>T-\d+)-audit-(?<iteration>r\d+)\.zip$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public async Task<string> SubmitAndWaitForReportAsync(
        AuditSubmitOptions options,
        string repoRoot,
        string zipPath,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            await using var browser = await AuditSubmitCodexChromeClient.ConnectAsync(options, linked.Token).ConfigureAwait(false);
            var downloadsDirectory = CreateDownloadsDirectory(repoRoot);
            var tabId = await browser.CreateTabAsync(linked.Token).ConfigureAwait(false);
            var completed = false;
            try
            {
                var downloadDirectoryConfigured = await PrepareProjectForPromptSubmissionAsync(
                    new AuditSubmitProjectPreparationDriver(browser, tabId, downloadsDirectory),
                    options.ProjectUrl,
                    TimeSpan.FromMinutes(options.LoginTimeoutMinutes),
                    linked.Token).ConfigureAwait(false);
                var ignoredDeepResearchTargetIds = options.DeepResearch
                    ? await SnapshotDeepResearchTargetIdsAsync(browser, tabId, linked.Token).ConfigureAwait(false)
                    : new HashSet<string>(StringComparer.Ordinal);
                var messageCountBeforeSend = await SubmitPromptAsync(
                    new AuditSubmitPromptSubmissionDriver(browser, tabId),
                    [zipPath],
                    message,
                    options.DeepResearch,
                    linked.Token).ConfigureAwait(false);
                await WaitForConversationMessagesAsync(browser, tabId, messageCountBeforeSend + 1, TimeSpan.FromMinutes(2), linked.Token).ConfigureAwait(false);
                var conversationUrl = await WaitForConcreteConversationUrlAsync(browser, tabId, TimeSpan.FromSeconds(30), linked.Token).ConfigureAwait(false);
                await WriteConversationUrlSidecarAsync(repoRoot, zipPath, conversationUrl, options.ControlAudit, linked.Token).ConfigureAwait(false);

                var report = options.DeepResearch
                    ? await WaitForReportAsync(browser, tabId, options, downloadsDirectory, includeUserDownloadsFallback: !downloadDirectoryConfigured, ignoredDeepResearchTargetIds, linked.Token).ConfigureAwait(false)
                    : await WaitForOrdinaryChatReportAsync(browser, tabId, options, messageCountBeforeSend, linked.Token).ConfigureAwait(false);
                completed = true;
                return report;
            }
            finally
            {
                if (completed || !options.KeepTabOpenOnError)
                {
                    await browser.FinalizeTabsAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-TIMEOUT",
                $"External audit report was not ready within {options.TimeoutMinutes} minutes.",
                ZipPath: zipPath);
        }
        catch (AuditSubmitCodexChromeException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                ex.Code,
                ex.Message,
                ZipPath: zipPath);
        }
        catch (IOException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                $"Codex Chrome Extension pipe failed: {ex.Message}",
                ZipPath: zipPath);
        }
        catch (JsonException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                $"Codex Chrome Extension response was invalid JSON: {ex.Message}",
                ZipPath: zipPath);
        }
    }

    public async Task<string> DownloadReportFromUrlAsync(
        AuditSubmitOptions options,
        string repoRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            await using var browser = await AuditSubmitCodexChromeClient.ConnectAsync(options, linked.Token).ConfigureAwait(false);
            var downloadsDirectory = CreateDownloadsDirectory(repoRoot);
            var tabId = await browser.CreateTabAsync(linked.Token).ConfigureAwait(false);
            var completed = false;
            try
            {
                await InitializeTabAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                _ = await ConfigureDownloadsAsync(browser, tabId, downloadsDirectory, linked.Token).ConfigureAwait(false);
                var ignoredDeepResearchTargetIds = new HashSet<string>(StringComparer.Ordinal);
                await NavigateAsync(browser, tabId, options.ProjectUrl, linked.Token).ConfigureAwait(false);
                await BringTabToFrontBestEffortAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                await ScrollConversationToBottomAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                _ = await WaitForDeepResearchFrameContentAsync(browser, tabId, TimeSpan.FromSeconds(90), linked.Token).ConfigureAwait(false);

                var report = await DownloadReadyReportAsync(
                    browser,
                    tabId,
                    options,
                    downloadsDirectory,
                    includeUserDownloadsFallback: true,
                    ignoredDeepResearchTargetIds,
                    linked.Token).ConfigureAwait(false);
                completed = true;
                return report;
            }
            finally
            {
                if (completed || !options.KeepTabOpenOnError)
                {
                    await browser.FinalizeTabsAsync(CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-TIMEOUT",
                $"External audit report was not ready within {options.TimeoutMinutes} minutes.");
        }
        catch (AuditSubmitCodexChromeException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                ex.Code,
                ex.Message);
        }
        catch (IOException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                $"Codex Chrome Extension pipe failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                $"Codex Chrome Extension response was invalid JSON: {ex.Message}");
        }
    }

    public async Task<string> DumpDomFromUrlAsync(
        AuditSubmitOptions options,
        string repoRoot,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            await using var browser = await AuditSubmitCodexChromeClient.ConnectAsync(options, linked.Token).ConfigureAwait(false);
            return await DumpDomFromUrlAsync(
                new AuditSubmitDomDumpDriver(browser),
                options,
                repoRoot,
                linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-TIMEOUT",
                $"DOM dump did not finish within {options.TimeoutMinutes} minutes.");
        }
        catch (AuditSubmitCodexChromeException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                ex.Code,
                ex.Message);
        }
        catch (IOException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                $"Codex Chrome Extension pipe failed: {ex.Message}");
        }
        catch (JsonException ex)
        {
            throw new AuditPackageFailure(
                "audit submit",
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
            $"Codex Chrome Extension response was invalid JSON: {ex.Message}");
        }
    }

    private static async Task<string> DumpDomFromUrlAsync(
        IAuditSubmitDomDumpDriver driver,
        AuditSubmitOptions options,
        string repoRoot,
        CancellationToken cancellationToken)
    {
        var dumpDirectory = ResolveDomDumpDirectory(repoRoot, options);
        var tabId = await driver.CreateTabAsync(cancellationToken).ConfigureAwait(false);
        var completed = false;
        try
        {
            await driver.InitializeTabAsync(tabId, cancellationToken).ConfigureAwait(false);
            await driver.NavigateAsync(tabId, options.ProjectUrl, cancellationToken).ConfigureAwait(false);
            await driver.BringTabToFrontAsync(tabId, cancellationToken).ConfigureAwait(false);
            await driver.WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
            await driver.ScrollConversationToBottomAsync(tabId, cancellationToken).ConfigureAwait(false);
            await driver.WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);

            var frameTree = await driver.ExecuteCdpAsync(tabId, "Page.getFrameTree", EmptyObject(), TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            await WriteJsonFileAsync(Path.Combine(dumpDirectory, "frame-tree.json"), frameTree, cancellationToken).ConfigureAwait(false);
            var targetInfo = await driver.ExecuteCdpAsync(tabId, "Target.getTargets", EmptyObject(), TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
            await WriteJsonFileAsync(Path.Combine(dumpDirectory, "target-info.json"), targetInfo, cancellationToken).ConfigureAwait(false);
            try
            {
                var selectedState = await driver.EvaluateAsync(tabId, DeepResearchSelectedExpression, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
                await WriteJsonFileAsync(Path.Combine(dumpDirectory, "deep-research-selected-result.json"), selectedState, cancellationToken).ConfigureAwait(false);
            }
            catch (AuditSubmitCodexChromeException)
            {
            }

            try
            {
                var selectedDiagnostics = await driver.EvaluateAsync(tabId, DeepResearchSelectedDiagnosticsExpression, TimeSpan.FromSeconds(15), cancellationToken).ConfigureAwait(false);
                await WriteJsonFileAsync(Path.Combine(dumpDirectory, "deep-research-selected-diagnostics.json"), selectedDiagnostics, cancellationToken).ConfigureAwait(false);
            }
            catch (AuditSubmitCodexChromeException)
            {
            }

            JsonElement? accessibilityTree = null;
            try
            {
                accessibilityTree = await driver.ExecuteCdpAsync(tabId, "Accessibility.getFullAXTree", EmptyObject(), TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                await WriteJsonFileAsync(Path.Combine(dumpDirectory, "accessibility-tree.json"), accessibilityTree.Value, cancellationToken).ConfigureAwait(false);
            }
            catch (AuditSubmitCodexChromeException)
            {
            }

            var entries = ReadFrameTreeEntries(frameTree);
            var targets = ReadTargetInfoEntries(targetInfo);
            var summaries = new List<string>
            {
                $"DOM dump: {DateTimeOffset.Now:O}",
                $"URL: {options.ProjectUrl}",
                $"Frames: {entries.Count}",
                $"Targets: {targets.Count}",
                $"Directory: {dumpDirectory}"
            };
            foreach (var target in targets.Take(50))
            {
                summaries.Add($"target: type={target.Type} id={target.TargetId} attached={target.Attached} url={target.Url} title={target.Title}");
            }
            if (accessibilityTree is { } axTree)
            {
                foreach (var axSummary in ReadAccessibilityTreeSummaries(axTree))
                {
                    summaries.Add(axSummary);
                }
            }

            await driver.DumpDeepResearchTargetsAsync(tabId, dumpDirectory, targets, summaries, cancellationToken).ConfigureAwait(false);

            var index = 0;
            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                index++;
                var prefix = $"{index:00}-{SanitizeDumpFileName(entry.Url)}";
                try
                {
                    JsonElement dump;
                    if (entry.IsRoot)
                    {
                        dump = await driver.EvaluateAsync(tabId, DomDumpExpression, TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        var contextId = await driver.CreateFrameExecutionContextAsync(tabId, entry.FrameId, cancellationToken).ConfigureAwait(false);
                        dump = await driver.EvaluateInContextAsync(tabId, contextId, DomDumpExpression, TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
                    }

                    await WriteDomDumpFilesAsync(dumpDirectory, prefix, entry, dump, cancellationToken).ConfigureAwait(false);
                    var htmlLength = TryReadStringProperty(dump, "html", out var html) ? html.Length : 0;
                    var textLength = TryReadStringProperty(dump, "text", out var text) ? text.Length : 0;
                    summaries.Add($"{index:00}: ok frameId={entry.FrameId} root={entry.IsRoot} url={entry.Url} html={htmlLength} text={textLength}");
                }
                catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException or IOException)
                {
                    summaries.Add($"{index:00}: failed frameId={entry.FrameId} root={entry.IsRoot} url={entry.Url} error={ex.Message}");
                }
            }

            var summary = string.Join(Environment.NewLine, summaries);
            await File.WriteAllTextAsync(Path.Combine(dumpDirectory, "summary.txt"), summary + Environment.NewLine, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            completed = true;
            return summary;
        }
        finally
        {
            if (completed || !options.KeepTabOpenOnError)
            {
                await driver.FinalizeTabsAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    private interface IAuditSubmitDomDumpDriver
    {
        Task<long> CreateTabAsync(CancellationToken cancellationToken);

        Task InitializeTabAsync(long tabId, CancellationToken cancellationToken);

        Task NavigateAsync(long tabId, string url, CancellationToken cancellationToken);

        Task BringTabToFrontAsync(long tabId, CancellationToken cancellationToken);

        Task WaitForReportHydrationAsync(CancellationToken cancellationToken);

        Task ScrollConversationToBottomAsync(long tabId, CancellationToken cancellationToken);

        Task<JsonElement> ExecuteCdpAsync(long tabId, string method, Dictionary<string, object?> parameters, TimeSpan timeout, CancellationToken cancellationToken);

        Task<JsonElement> EvaluateAsync(long tabId, string expression, TimeSpan timeout, CancellationToken cancellationToken);

        Task<int> CreateFrameExecutionContextAsync(long tabId, string frameId, CancellationToken cancellationToken);

        Task<JsonElement> EvaluateInContextAsync(long tabId, int contextId, string expression, TimeSpan timeout, CancellationToken cancellationToken);

        Task DumpDeepResearchTargetsAsync(
            long tabId,
            string dumpDirectory,
            IReadOnlyList<AuditSubmitTargetInfoEntry> targets,
            List<string> summaries,
            CancellationToken cancellationToken);

        Task FinalizeTabsAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitDomDumpDriver(AuditSubmitCodexChromeClient browser) : IAuditSubmitDomDumpDriver
    {
        public Task<long> CreateTabAsync(CancellationToken cancellationToken)
        {
            return browser.CreateTabAsync(cancellationToken);
        }

        public Task InitializeTabAsync(long tabId, CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.InitializeTabAsync(browser, tabId, cancellationToken);
        }

        public Task NavigateAsync(long tabId, string url, CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.NavigateAsync(browser, tabId, url, cancellationToken);
        }

        public Task BringTabToFrontAsync(long tabId, CancellationToken cancellationToken)
        {
            return BringTabToFrontBestEffortAsync(browser, tabId, cancellationToken);
        }

        public Task WaitForReportHydrationAsync(CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.WaitForReportHydrationAsync(cancellationToken);
        }

        public Task ScrollConversationToBottomAsync(long tabId, CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.ScrollConversationToBottomAsync(browser, tabId, cancellationToken);
        }

        public Task<JsonElement> ExecuteCdpAsync(long tabId, string method, Dictionary<string, object?> parameters, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return browser.ExecuteCdpAsync(tabId, method, parameters, timeout, cancellationToken);
        }

        public Task<JsonElement> EvaluateAsync(long tabId, string expression, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return browser.EvaluateAsync(tabId, expression, timeout, cancellationToken);
        }

        public Task<int> CreateFrameExecutionContextAsync(long tabId, string frameId, CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.CreateFrameExecutionContextAsync(browser, tabId, frameId, cancellationToken);
        }

        public Task<JsonElement> EvaluateInContextAsync(long tabId, int contextId, string expression, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return browser.EvaluateInContextAsync(tabId, contextId, expression, timeout, cancellationToken);
        }

        public Task DumpDeepResearchTargetsAsync(
            long tabId,
            string dumpDirectory,
            IReadOnlyList<AuditSubmitTargetInfoEntry> targets,
            List<string> summaries,
            CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.DumpDeepResearchTargetsAsync(browser, tabId, dumpDirectory, targets, summaries, cancellationToken);
        }

        public Task FinalizeTabsAsync(CancellationToken cancellationToken)
        {
            return browser.FinalizeTabsAsync(cancellationToken);
        }
    }

    private static async Task InitializeTabAsync(AuditSubmitCodexChromeClient browser, long tabId, CancellationToken cancellationToken)
    {
        await browser.AttachAsync(tabId, cancellationToken).ConfigureAwait(false);
        await BringTabToFrontBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await browser.ExecuteCdpAsync(tabId, "Page.enable", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
        await browser.ExecuteCdpAsync(tabId, "Runtime.enable", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
        await browser.ExecuteCdpAsync(tabId, "DOM.enable", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
        await InstallClipboardWriteCapturePreloadBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await EnableOopifAutoAttachAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InstallClipboardWriteCapturePreloadBestEffortAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            await browser.ExecuteCdpAsync(
                tabId,
                "Page.addScriptToEvaluateOnNewDocument",
                new Dictionary<string, object?>
                {
                    ["source"] = ClipboardWriteCapturePreloadExpression
                },
                TimeSpan.FromSeconds(10),
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException)
        {
        }
    }

    private static async Task EnableOopifAutoAttachAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, object?>
        {
            ["autoAttach"] = true,
            ["flatten"] = true,
            ["waitForDebuggerOnStart"] = false,
            ["filter"] = new[]
            {
                new Dictionary<string, object?>
                {
                    ["type"] = "iframe",
                    ["exclude"] = false
                }
            }
        };

        try
        {
            await browser.ExecuteCdpAsync(tabId, "Target.setAutoAttach", parameters, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (AuditSubmitCodexChromeException)
        {
        }

        parameters.Remove("filter");
        try
        {
            await browser.ExecuteCdpAsync(tabId, "Target.setAutoAttach", parameters, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        }
        catch (AuditSubmitCodexChromeException)
        {
        }
    }

    private static async Task BringTabToFrontBestEffortAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            await browser.ExecuteCdpAsync(tabId, "Page.bringToFront", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
        }
        catch (AuditSubmitCodexChromeException)
        {
        }
    }

    private static async Task<bool> ConfigureDownloadsAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(downloadsDirectory);
        var configured = false;
        foreach (var method in new[] { "Page.setDownloadBehavior", "Browser.setDownloadBehavior" })
        {
            var parameters = new Dictionary<string, object?>
            {
                ["behavior"] = "allow",
                ["downloadPath"] = downloadsDirectory
            };
            if (method.StartsWith("Browser.", StringComparison.Ordinal))
            {
                parameters["eventsEnabled"] = true;
            }

            try
            {
                await browser.ExecuteCdpAsync(tabId, method, parameters, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                configured = true;
            }
            catch (AuditSubmitCodexChromeException)
            {
            }
        }

        return configured;
    }

    private static string CreateDownloadsDirectory(string repoRoot)
    {
        var baseDirectory = Path.Combine(repoRoot, ".temp", "audit-submit-downloads");
        var stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(baseDirectory, $"{stamp}-{Guid.NewGuid():N}");
    }

    private static string ResolveDomDumpDirectory(string repoRoot, AuditSubmitOptions options)
    {
        var configured = options.DomDumpDirectory ?? throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-DOM-DUMP-MISSING",
            "--dump-dom-only requires --dom-dump-dir.");
        var directory = ResolvePath(repoRoot, configured);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static async Task<int> CreateFrameExecutionContextAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string frameId,
        CancellationToken cancellationToken)
    {
        var world = await browser.ExecuteCdpAsync(
            tabId,
            "Page.createIsolatedWorld",
            new Dictionary<string, object?>
            {
                ["frameId"] = frameId,
                ["worldName"] = "Electron2DAuditSubmitDomDump"
            },
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        if (!world.TryGetProperty("executionContextId", out var contextElement) ||
            contextElement.ValueKind != JsonValueKind.Number)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                "Page.createIsolatedWorld did not return executionContextId.");
        }

        return contextElement.GetInt32();
    }

    private static async Task<int> CreateFrameExecutionContextOnTargetAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string targetId,
        string frameId,
        CancellationToken cancellationToken)
    {
        var world = await browser.ExecuteCdpOnTargetAsync(
            tabId,
            targetId,
            "Page.createIsolatedWorld",
            new Dictionary<string, object?>
            {
                ["frameId"] = frameId,
                ["worldName"] = "Electron2DAuditSubmitDeepResearch"
            },
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        if (!world.TryGetProperty("executionContextId", out var contextElement) ||
            contextElement.ValueKind != JsonValueKind.Number)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                "Page.createIsolatedWorld did not return executionContextId for a deep research target frame.");
        }

        return contextElement.GetInt32();
    }

    private static async Task DumpDeepResearchTargetsAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string dumpDirectory,
        IReadOnlyList<AuditSubmitTargetInfoEntry> targets,
        List<string> summaries,
        CancellationToken cancellationToken)
    {
        var index = 0;
        foreach (var target in targets.Where(IsDeepResearchTarget))
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            var attached = false;
            var prefix = $"deep-target-{index:00}-{SanitizeDumpFileName(target.Url)}";
            try
            {
                await browser.AttachTargetWithRecoveryAsync(tabId, target.TargetId, cancellationToken).ConfigureAwait(false);
                attached = true;
                await browser.ExecuteCdpOnTargetAsync(tabId, target.TargetId, "Page.enable", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                await browser.ExecuteCdpOnTargetAsync(tabId, target.TargetId, "Runtime.enable", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                await browser.ExecuteCdpOnTargetAsync(tabId, target.TargetId, "DOM.enable", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                var targetFrameTree = await browser.ExecuteCdpOnTargetAsync(tabId, target.TargetId, "Page.getFrameTree", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                await WriteJsonFileAsync(Path.Combine(dumpDirectory, prefix + "-frame-tree.json"), targetFrameTree, cancellationToken).ConfigureAwait(false);
                var ready = await browser.EvaluateBoolOnTargetAsync(tabId, target.TargetId, DeepResearchReportTargetReadyExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                var dump = await browser.EvaluateOnTargetAsync(tabId, target.TargetId, DomDumpExpression, TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
                await WriteDomDumpFilesAsync(
                    dumpDirectory,
                    prefix,
                    new AuditSubmitFrameTreeEntry(target.TargetId, target.Url, target.Title, false),
                    dump,
                    cancellationToken).ConfigureAwait(false);
                var htmlLength = TryReadStringProperty(dump, "html", out var html) ? html.Length : 0;
                var textLength = TryReadStringProperty(dump, "text", out var text) ? text.Length : 0;
                summaries.Add($"deep-target-{index:00}: ok id={target.TargetId} ready={ready} url={target.Url} html={htmlLength} text={textLength}");
                foreach (var frame in ReadFrameTreeEntries(targetFrameTree)
                    .OrderBy(static frame => frame.IsRoot ? 1 : 0)
                    .ThenBy(static frame => string.Equals(frame.Url, "about:blank", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                    .ThenBy(static frame => frame.FrameId, StringComparer.Ordinal))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var contextId = await CreateFrameExecutionContextOnTargetAsync(browser, tabId, target.TargetId, frame.FrameId, cancellationToken).ConfigureAwait(false);
                        var frameReady = await browser.EvaluateBoolInContextOnTargetAsync(tabId, target.TargetId, contextId, DeepResearchReportTargetReadyExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                        var frameDump = await browser.EvaluateInContextOnTargetAsync(tabId, target.TargetId, contextId, DomDumpExpression, TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
                        await WriteDomDumpFilesAsync(
                            dumpDirectory,
                            $"{prefix}-frame-{SanitizeDumpFileName(frame.FrameId)}",
                            frame,
                            frameDump,
                            cancellationToken).ConfigureAwait(false);
                        var frameHtmlLength = TryReadStringProperty(frameDump, "html", out var frameHtml) ? frameHtml.Length : 0;
                        var frameTextLength = TryReadStringProperty(frameDump, "text", out var frameText) ? frameText.Length : 0;
                        summaries.Add($"deep-target-frame-{index:00}: ok id={target.TargetId} frame={frame.FrameId} ready={frameReady} url={frame.Url} html={frameHtmlLength} text={frameTextLength}");
                    }
                    catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException or IOException)
                    {
                        summaries.Add($"deep-target-frame-{index:00}: failed id={target.TargetId} frame={frame.FrameId} url={frame.Url} error={ex.Message}");
                    }
                }
            }
            catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException or IOException)
            {
                summaries.Add($"deep-target-{index:00}: failed id={target.TargetId} attached={attached} url={target.Url} error={ex.Message}");
            }
            finally
            {
                if (attached)
                {
                    await browser.DetachTargetAsync(tabId, target.TargetId, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }
    }

    private static List<string> ReadAccessibilityTreeSummaries(JsonElement accessibilityTree)
    {
        var summaries = new List<string>();
        if (accessibilityTree.ValueKind != JsonValueKind.Object ||
            !accessibilityTree.TryGetProperty("nodes", out var nodes) ||
            nodes.ValueKind != JsonValueKind.Array)
        {
            return summaries;
        }

        foreach (var node in nodes.EnumerateArray())
        {
            if (node.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var role = TryReadAccessibilityValue(node, "role", out var roleValue) ? roleValue : string.Empty;
            var name = TryReadAccessibilityValue(node, "name", out var nameValue) ? nameValue : string.Empty;
            var normalized = name.ToLowerInvariant();
            if (!normalized.Contains("экспорт", StringComparison.Ordinal) &&
                !normalized.Contains("markdown", StringComparison.Ordinal) &&
                !normalized.Contains("углубленный исследовательский отчет", StringComparison.Ordinal) &&
                !normalized.Contains("углублённый исследовательский отчёт", StringComparison.Ordinal) &&
                !normalized.Contains("deep research report", StringComparison.Ordinal))
            {
                continue;
            }

            var backendNodeId = node.TryGetProperty("backendDOMNodeId", out var backendElement) && backendElement.ValueKind == JsonValueKind.Number
                ? backendElement.GetInt64().ToString(CultureInfo.InvariantCulture)
                : string.Empty;
            var nodeId = TryReadStringProperty(node, "nodeId", out var nodeIdValue) ? nodeIdValue : string.Empty;
            summaries.Add($"ax: role={role} name={TruncateSummaryValue(name, 180)} nodeId={nodeId} backendDOMNodeId={backendNodeId}");
        }

        return summaries;
    }

    private static bool TryReadAccessibilityValue(JsonElement node, string propertyName, out string value)
    {
        value = string.Empty;
        if (!node.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Object ||
            !property.TryGetProperty("value", out var valueElement))
        {
            return false;
        }

        value = valueElement.ValueKind switch
        {
            JsonValueKind.String => valueElement.GetString() ?? string.Empty,
            JsonValueKind.Number => valueElement.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
        return !string.IsNullOrWhiteSpace(value);
    }

    private static string TruncateSummaryValue(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }

    private static List<AuditSubmitFrameTreeEntry> ReadFrameTreeEntries(JsonElement frameTreeResult)
    {
        var entries = new List<AuditSubmitFrameTreeEntry>();
        if (frameTreeResult.ValueKind == JsonValueKind.Object &&
            frameTreeResult.TryGetProperty("frameTree", out var root))
        {
            ReadFrameTreeEntries(root, isRoot: true, entries);
        }

        return entries;
    }

    private static List<AuditSubmitTargetInfoEntry> ReadTargetInfoEntries(JsonElement targetInfoResult)
    {
        var entries = new List<AuditSubmitTargetInfoEntry>();
        if (targetInfoResult.ValueKind != JsonValueKind.Object ||
            !targetInfoResult.TryGetProperty("targetInfos", out var targetInfos) ||
            targetInfos.ValueKind != JsonValueKind.Array)
        {
            return entries;
        }

        foreach (var targetInfo in targetInfos.EnumerateArray())
        {
            if (targetInfo.ValueKind != JsonValueKind.Object ||
                !TryReadTargetId(targetInfo, out var targetId))
            {
                continue;
            }

            var type = TryReadStringProperty(targetInfo, "type", out var targetType) ? targetType : string.Empty;
            var url = TryReadStringProperty(targetInfo, "url", out var targetUrl) ? targetUrl : string.Empty;
            var title = TryReadStringProperty(targetInfo, "title", out var targetTitle) ? targetTitle : string.Empty;
            var attached = targetInfo.TryGetProperty("attached", out var attachedElement) && attachedElement.ValueKind == JsonValueKind.True;
            entries.Add(new AuditSubmitTargetInfoEntry(targetId, type, url, title, attached));
        }

        return entries;
    }

    private static bool TryReadTargetId(JsonElement targetInfo, out string targetId)
    {
        targetId = string.Empty;
        foreach (var propertyName in new[] { "targetId", "id" })
        {
            if (targetInfo.TryGetProperty(propertyName, out var idElement) &&
                idElement.ValueKind == JsonValueKind.String)
            {
                targetId = idElement.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(targetId);
            }
        }

        return false;
    }

    private static void ReadFrameTreeEntries(JsonElement frameTree, bool isRoot, List<AuditSubmitFrameTreeEntry> entries)
    {
        if (frameTree.ValueKind != JsonValueKind.Object ||
            !frameTree.TryGetProperty("frame", out var frame) ||
            frame.ValueKind != JsonValueKind.Object ||
            !frame.TryGetProperty("id", out var idElement) ||
            idElement.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var frameId = idElement.GetString() ?? string.Empty;
        var url = frame.TryGetProperty("url", out var urlElement) && urlElement.ValueKind == JsonValueKind.String
            ? urlElement.GetString() ?? string.Empty
            : string.Empty;
        var name = frame.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? nameElement.GetString() ?? string.Empty
            : string.Empty;
        entries.Add(new AuditSubmitFrameTreeEntry(frameId, url, name, isRoot));

        if (!frameTree.TryGetProperty("childFrames", out var children) ||
            children.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var child in children.EnumerateArray())
        {
            ReadFrameTreeEntries(child, isRoot: false, entries);
        }
    }

    private static async Task WriteJsonFileAsync(string path, JsonElement value, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(
            path,
            JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteDomDumpFilesAsync(
        string dumpDirectory,
        string prefix,
        AuditSubmitFrameTreeEntry entry,
        JsonElement dump,
        CancellationToken cancellationToken)
    {
        var html = TryReadStringProperty(dump, "html", out var htmlValue) ? htmlValue : string.Empty;
        var text = TryReadStringProperty(dump, "text", out var textValue) ? textValue : string.Empty;
        await File.WriteAllTextAsync(Path.Combine(dumpDirectory, prefix + ".html"), html, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(Path.Combine(dumpDirectory, prefix + ".text.txt"), text, Encoding.UTF8, cancellationToken).ConfigureAwait(false);

        var meta = new Dictionary<string, object?>
        {
            ["frameId"] = entry.FrameId,
            ["frameTreeUrl"] = entry.Url,
            ["frameTreeName"] = entry.Name,
            ["isRoot"] = entry.IsRoot,
            ["url"] = TryReadStringProperty(dump, "url", out var url) ? url : string.Empty,
            ["title"] = TryReadStringProperty(dump, "title", out var title) ? title : string.Empty,
            ["readyState"] = TryReadStringProperty(dump, "readyState", out var readyState) ? readyState : string.Empty,
            ["htmlLength"] = html.Length,
            ["textLength"] = text.Length,
            ["buttons"] = TryReadJsonProperty(dump, "buttons", out var buttons) ? buttons : Array.Empty<object>(),
            ["links"] = TryReadJsonProperty(dump, "links", out var links) ? links : Array.Empty<object>(),
            ["iframes"] = TryReadJsonProperty(dump, "iframes", out var iframes) ? iframes : Array.Empty<object>()
        };
        await File.WriteAllTextAsync(
            Path.Combine(dumpDirectory, prefix + ".meta.json"),
            JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true }),
            Encoding.UTF8,
            cancellationToken).ConfigureAwait(false);
    }

    private static bool TryReadStringProperty(JsonElement value, string propertyName, out string text)
    {
        text = string.Empty;
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        text = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryReadJsonProperty(JsonElement value, string propertyName, out JsonElement property)
    {
        property = default;
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty(propertyName, out var found))
        {
            return false;
        }

        property = found.Clone();
        return true;
    }

    private static string SanitizeDumpFileName(string value)
    {
        var source = string.IsNullOrWhiteSpace(value) ? "blank-frame" : value;
        var builder = new StringBuilder(Math.Min(source.Length, 90));
        foreach (var ch in source)
        {
            if (builder.Length >= 90)
            {
                break;
            }

            builder.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
        }

        var name = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(name) ? "blank-frame" : name;
    }

    private static string ResolvePath(string root, string path)
    {
        return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(root, path));
    }

    private static async Task NavigateAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string url,
        CancellationToken cancellationToken)
    {
        await browser.ExecuteCdpAsync(
            tabId,
            "Page.navigate",
            new Dictionary<string, object?> { ["url"] = url },
            UiActionTimeout,
            cancellationToken).ConfigureAwait(false);
        await BringTabToFrontBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await WaitForPageReadyAsync(browser, tabId, UiActionTimeout, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitForPageReadyAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ready = await browser.EvaluateBoolAsync(
                tabId,
                "(() => document.readyState === 'interactive' || document.readyState === 'complete')()",
                UiActionTimeout,
                cancellationToken).ConfigureAwait(false);
            if (ready)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-BROWSER-FAILED",
            "Timed out waiting for the browser page to become ready.");
    }

    private static Task WaitForReportHydrationAsync(CancellationToken cancellationToken)
    {
        return Task.Delay(ReportHydrationDelay, cancellationToken);
    }

    private static async Task ScrollConversationToBottomAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 4; attempt++)
        {
            _ = await browser.EvaluateBoolAsync(tabId, ScrollConversationToBottomExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task WaitForComposerAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await browser.EvaluateBoolAsync(tabId, HasPromptExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-LOGIN-REQUIRED",
            "ChatGPT composer did not become available before the login timeout.");
    }

    private static async Task EnableDeepResearchAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        await EnableDeepResearchAsync(
            new AuditSubmitDeepResearchSelectionDriver(browser, tabId),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnableDeepResearchAsync(
        IAuditSubmitDeepResearchSelectionDriver driver,
        CancellationToken cancellationToken)
    {
        var selectedThroughMenu = false;
        var deadline = driver.UtcNow + TimeSpan.FromSeconds(20);
        var menuOpenRetrySuppressedUntil = DateTimeOffset.MinValue;
        while (driver.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await driver.IsSelectedAsync(cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            if (await driver.TryClickOpenItemAsync(cancellationToken).ConfigureAwait(false))
            {
                selectedThroughMenu = true;
                await driver.DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (await driver.IsComposerMenuOpenAsync(cancellationToken).ConfigureAwait(false))
            {
                await driver.DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (await driver.IsComposerMenuExpandedAsync(cancellationToken).ConfigureAwait(false))
            {
                await driver.DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (driver.UtcNow < menuOpenRetrySuppressedUntil)
            {
                await driver.DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                continue;
            }

            if (!await driver.TryOpenMenuAsync(cancellationToken).ConfigureAwait(false))
            {
                await driver.DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                continue;
            }

            menuOpenRetrySuppressedUntil = driver.UtcNow + TimeSpan.FromSeconds(3);
            await driver.DelayAsync(TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);

            if (await driver.TryClickMenuItemAsync(cancellationToken).ConfigureAwait(false))
            {
                selectedThroughMenu = true;
            }

            await driver.DelayAsync(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        if (selectedThroughMenu &&
            await driver.IsSelectedAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING",
            "Could not select the Deep Research control from the ChatGPT composer plus menu.");
    }

    private interface IAuditSubmitDeepResearchSelectionDriver
    {
        DateTimeOffset UtcNow { get; }

        Task<bool> IsSelectedAsync(CancellationToken cancellationToken);

        Task<bool> TryClickOpenItemAsync(CancellationToken cancellationToken);

        Task<bool> IsComposerMenuOpenAsync(CancellationToken cancellationToken);

        Task<bool> IsComposerMenuExpandedAsync(CancellationToken cancellationToken);

        Task<bool> TryOpenMenuAsync(CancellationToken cancellationToken);

        Task<bool> TryClickMenuItemAsync(CancellationToken cancellationToken);

        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitDeepResearchSelectionDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitDeepResearchSelectionDriver
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public async Task<bool> IsSelectedAsync(CancellationToken cancellationToken)
        {
            return await browser.EvaluateBoolAsync(tabId, DeepResearchSelectedExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryClickOpenItemAsync(CancellationToken cancellationToken)
        {
            return await TryClickPointAsync(DeepResearchItemPointExpression, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IsComposerMenuOpenAsync(CancellationToken cancellationToken)
        {
            return await browser.EvaluateBoolAsync(tabId, DeepResearchComposerMenuOpenExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> IsComposerMenuExpandedAsync(CancellationToken cancellationToken)
        {
            return await browser.EvaluateBoolAsync(tabId, DeepResearchComposerMenuExpandedExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryOpenMenuAsync(CancellationToken cancellationToken)
        {
            return await TryClickPointAsync(DeepResearchMenuPointExpression, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> TryClickMenuItemAsync(CancellationToken cancellationToken)
        {
            return await TryClickPointAsync(DeepResearchItemPointExpression, cancellationToken).ConfigureAwait(false);
        }

        public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        private async Task<bool> TryClickPointAsync(string expression, CancellationToken cancellationToken)
        {
            var point = await browser.EvaluatePointAsync(tabId, expression, UiActionTimeout, cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
            if (point is null)
            {
                return false;
            }

            await browser.ClickAtAsync(tabId, point.Value, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }

    private static async Task RequireDeepResearchSelectedAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        if (await browser.EvaluateBoolAsync(tabId, DeepResearchSelectedExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING",
            "Deep Research was not selected in the composer immediately before sending the audit ZIP.");
    }

    private static async Task<bool> PrepareProjectForPromptSubmissionAsync(
        IAuditSubmitProjectPreparationDriver driver,
        string projectUrl,
        TimeSpan loginTimeout,
        CancellationToken cancellationToken)
    {
        await driver.InitializeTabAsync(cancellationToken).ConfigureAwait(false);
        var downloadDirectoryConfigured = await driver.ConfigureDownloadsAsync(cancellationToken).ConfigureAwait(false);
        await driver.NavigateAsync(projectUrl, cancellationToken).ConfigureAwait(false);
        await driver.BringTabToFrontBestEffortAsync(cancellationToken).ConfigureAwait(false);
        await driver.WaitForComposerAsync(loginTimeout, cancellationToken).ConfigureAwait(false);
        await driver.WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
        return downloadDirectoryConfigured;
    }

    private interface IAuditSubmitProjectPreparationDriver
    {
        Task InitializeTabAsync(CancellationToken cancellationToken);

        Task<bool> ConfigureDownloadsAsync(CancellationToken cancellationToken);

        Task NavigateAsync(string projectUrl, CancellationToken cancellationToken);

        Task BringTabToFrontBestEffortAsync(CancellationToken cancellationToken);

        Task WaitForComposerAsync(TimeSpan loginTimeout, CancellationToken cancellationToken);

        Task WaitForReportHydrationAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitProjectPreparationDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory) : IAuditSubmitProjectPreparationDriver
    {
        public async Task InitializeTabAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.InitializeTabAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ConfigureDownloadsAsync(CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.ConfigureDownloadsAsync(browser, tabId, downloadsDirectory, cancellationToken).ConfigureAwait(false);
        }

        public async Task NavigateAsync(string projectUrl, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.NavigateAsync(browser, tabId, projectUrl, cancellationToken).ConfigureAwait(false);
        }

        public async Task BringTabToFrontBestEffortAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.BringTabToFrontBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        public async Task WaitForComposerAsync(TimeSpan loginTimeout, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.WaitForComposerAsync(browser, tabId, loginTimeout, cancellationToken).ConfigureAwait(false);
        }

        public Task WaitForReportHydrationAsync(CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.WaitForReportHydrationAsync(cancellationToken);
        }
    }

    private static async Task<int> SubmitPromptAsync(
        IAuditSubmitPromptSubmissionDriver driver,
        string[] zipPaths,
        string message,
        bool deepResearch,
        CancellationToken cancellationToken)
    {
        await driver.AttachFilesAsync(zipPaths, cancellationToken).ConfigureAwait(false);
        if (deepResearch)
        {
            await driver.EnableDeepResearchAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            await driver.FillPromptAsync(message, cancellationToken).ConfigureAwait(false);
        }

        if (deepResearch)
        {
            await driver.RequireDeepResearchSelectedAsync(cancellationToken).ConfigureAwait(false);
        }

        await driver.RequirePromptPayloadReadyAsync(message, zipPaths, cancellationToken).ConfigureAwait(false);
        var messageCountBeforeSend = await driver.ReadConversationMessageCountAsync(cancellationToken).ConfigureAwait(false);
        await driver.ClickSendAsync(cancellationToken).ConfigureAwait(false);
        return messageCountBeforeSend;
    }

    private interface IAuditSubmitPromptSubmissionDriver
    {
        Task AttachFilesAsync(string[] paths, CancellationToken cancellationToken);

        Task FillPromptAsync(string message, CancellationToken cancellationToken);

        Task EnableDeepResearchAsync(CancellationToken cancellationToken);

        Task RequireDeepResearchSelectedAsync(CancellationToken cancellationToken);

        Task RequirePromptPayloadReadyAsync(string message, string[] paths, CancellationToken cancellationToken);

        Task<int> ReadConversationMessageCountAsync(CancellationToken cancellationToken);

        Task ClickSendAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitPromptSubmissionDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitPromptSubmissionDriver
    {
        public async Task AttachFilesAsync(string[] paths, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.AttachFilesAsync(browser, tabId, paths, cancellationToken).ConfigureAwait(false);
        }

        public async Task FillPromptAsync(string message, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.FillPromptAsync(browser, tabId, message, cancellationToken).ConfigureAwait(false);
        }

        public async Task EnableDeepResearchAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.EnableDeepResearchAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        public async Task RequireDeepResearchSelectedAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.RequireDeepResearchSelectedAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        public async Task RequirePromptPayloadReadyAsync(string message, string[] paths, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.RequirePromptPayloadReadyAsync(browser, tabId, message, paths, cancellationToken).ConfigureAwait(false);
        }

        public async Task<int> ReadConversationMessageCountAsync(CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.ReadConversationMessageCountAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        public async Task ClickSendAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.ClickSendAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task RequirePromptPayloadReadyAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string message,
        string[] paths,
        CancellationToken cancellationToken)
    {
        var fileNames = paths.Select(Path.GetFileName).Where(name => !string.IsNullOrWhiteSpace(name)).ToArray();
        var expression = PromptPayloadStatusExpression(
            JsonSerializer.Serialize(message),
            JsonSerializer.Serialize(fileNames));
        var deadline = DateTimeOffset.UtcNow + PromptPayloadReadyTimeout;
        var lastStatus = "not evaluated";
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var status = await browser.EvaluateAsync(tabId, expression, TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
            if (PromptPayloadStatusReady(status))
            {
                return;
            }

            lastStatus = DescribePromptPayloadStatus(status);
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-PAYLOAD-MISSING",
            $"The ChatGPT composer lost the audit prompt or the main audit ZIP before sending. Last payload status: {lastStatus}.");
    }

    private static bool PromptPayloadStatusReady(JsonElement status)
    {
        return status.ValueKind == JsonValueKind.Object &&
            status.TryGetProperty("ready", out var ready) &&
            ready.ValueKind == JsonValueKind.True;
    }

    private static string DescribePromptPayloadStatus(JsonElement status)
    {
        if (status.ValueKind != JsonValueKind.Object)
        {
            return status.ValueKind.ToString();
        }

        static string ReadProperty(JsonElement status, string name)
        {
            if (!status.TryGetProperty(name, out var value))
            {
                return "missing";
            }

            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? string.Empty,
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => "null",
                _ => value.ValueKind.ToString()
            };
        }

        return string.Join(
            "; ",
            [
                $"reason={ReadProperty(status, "reason")}",
                $"promptFound={ReadProperty(status, "promptFound")}",
                $"promptHasExpectedMessage={ReadProperty(status, "promptHasExpectedMessage")}",
                $"promptIsEmpty={ReadProperty(status, "promptIsEmpty")}",
                $"expectedFileCount={ReadProperty(status, "expectedFileCount")}",
                $"filenameMatchCount={ReadProperty(status, "filenameMatchCount")}",
                $"attachmentRootCount={ReadProperty(status, "attachmentRootCount")}"
            ]);
    }

    private static async Task AttachFilesAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string[] paths,
        CancellationToken cancellationToken)
    {
        var backendNodeId = await QueryFileInputBackendNodeIdAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        if (backendNodeId is null)
        {
            _ = await browser.EvaluateBoolAsync(tabId, AttachmentClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
            backendNodeId = await QueryFileInputBackendNodeIdAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        if (backendNodeId is null)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-ATTACHMENT-MISSING",
                "Could not find the file attachment input in the ChatGPT composer.");
        }

        await browser.ExecuteCdpAsync(
            tabId,
            "DOM.setFileInputFiles",
            new Dictionary<string, object?>
            {
                ["backendNodeId"] = backendNodeId.Value,
                ["files"] = paths
            },
            TimeSpan.FromMinutes(2),
            cancellationToken).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<long?> QueryFileInputBackendNodeIdAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var document = await browser.ExecuteCdpAsync(
            tabId,
            "DOM.getDocument",
            new Dictionary<string, object?> { ["depth"] = 1, ["pierce"] = true },
            UiActionTimeout,
            cancellationToken).ConfigureAwait(false);
        if (!document.TryGetProperty("root", out var root) || !root.TryGetProperty("nodeId", out var rootNodeIdElement))
        {
            return null;
        }

        var query = await browser.ExecuteCdpAsync(
            tabId,
            "DOM.querySelector",
            new Dictionary<string, object?>
            {
                ["nodeId"] = rootNodeIdElement.GetInt64(),
                ["selector"] = "input[type=\"file\"]"
            },
            UiActionTimeout,
            cancellationToken).ConfigureAwait(false);
        if (!query.TryGetProperty("nodeId", out var nodeIdElement) || nodeIdElement.GetInt64() == 0)
        {
            return null;
        }

        var node = await browser.ExecuteCdpAsync(
            tabId,
            "DOM.describeNode",
            new Dictionary<string, object?> { ["nodeId"] = nodeIdElement.GetInt64() },
            UiActionTimeout,
            cancellationToken).ConfigureAwait(false);
        return node.TryGetProperty("node", out var described) && described.TryGetProperty("backendNodeId", out var backendNodeId)
            ? backendNodeId.GetInt64()
            : null;
    }

    private static async Task FillPromptAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string message,
        CancellationToken cancellationToken)
    {
        var expression = FillPromptExpression(JsonSerializer.Serialize(message));
        var filled = await browser.EvaluateBoolAsync(tabId, expression, TimeSpan.FromMinutes(2), cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
        if (!filled)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-PROMPT-MISSING",
                "Could not find the prompt input in the ChatGPT composer.");
        }
    }

    private static async Task ClickSendAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromMinutes(2);
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await browser.EvaluateBoolAsync(tabId, SendClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false).ConfigureAwait(false))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-SEND-MISSING",
            "Could not find an enabled send button in the ChatGPT composer.");
    }

    private static async Task<string> WaitForReportAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitOptions options,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(options.PollSeconds);
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
            var decision = await CapturePollingDecisionAsync(browser, tabId, downloadsDirectory, includeUserDownloadsFallback, ignoredDeepResearchTargetIds, cancellationToken).ConfigureAwait(false);
            if (decision.Action == AuditSubmitPollingAction.ReturnReport && decision.Report is not null)
            {
                return decision.Report;
            }

            if (options.DownloadReportOnly && decision.Action == AuditSubmitPollingAction.Reload)
            {
                throw new AuditSubmitCodexChromeException(
                    "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING",
                    "The ready report page did not expose a unique Deep Research export button or Markdown blob. Inspect DOM diagnostics or the live user-visible Chrome state.");
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            if (options.DownloadReportOnly)
            {
                continue;
            }

            await browser.ExecuteCdpAsync(tabId, "Page.reload", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
            await WaitForPageReadyAsync(browser, tabId, UiActionTimeout, cancellationToken).ConfigureAwait(false);
            await WaitForConversationMessagesAsync(browser, tabId, minimumMessageCount: 1, timeout: TimeSpan.FromMinutes(1), cancellationToken: cancellationToken).ConfigureAwait(false);
            await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
            _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<string> DownloadReadyReportAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitOptions options,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        await ScrollConversationToBottomAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
        _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);

        var generationDeadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(60);
        while (await browser.EvaluateBoolAsync(tabId, IsGeneratingExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
        {
            if (DateTimeOffset.UtcNow >= generationDeadline)
            {
                throw new AuditSubmitCodexChromeException(
                    "E2D-BUILD-AUDIT-SUBMIT-REPORT-STILL-GENERATING",
                    "The report page is still generating; download-report-only does not reload or resubmit the page.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, options.PollSeconds)), cancellationToken).ConfigureAwait(false);
            _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        var ordinaryReport = await TryDownloadReadyOrdinaryChatReportAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(ordinaryReport))
        {
            return ordinaryReport;
        }

        var candidates = await DownloadReportCandidatesAsync(
            browser,
            tabId,
            downloadsDirectory,
            includeUserDownloadsFallback: true,
            ignoredDeepResearchTargetIds,
            allowLatestReadyTargetFallback: true,
            cancellationToken).ConfigureAwait(false);
        if (candidates.Length == 0)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING",
                "The ready report page did not produce a Markdown export in one deterministic download-report-only attempt.");
        }

        var report = ExtractDownloadedReportOrThrow(candidates);
        return report;
    }

    private static async Task<string?> TryDownloadReadyOrdinaryChatReportAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var currentMessageCount = await ReadConversationMessageCountAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        return await TryDownloadReadyOrdinaryChatReportAsync(
            new AuditSubmitOrdinaryReportDriver(browser, tabId),
            currentMessageCount,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> TryDownloadReadyOrdinaryChatReportAsync(
        IAuditSubmitOrdinaryReportDriver driver,
        int currentMessageCount,
        CancellationToken cancellationToken)
    {
        if (currentMessageCount < 2)
        {
            return null;
        }

        try
        {
            var copyResult = await driver.CopyLatestAssistantMessageMarkdownAsync(currentMessageCount, cancellationToken).ConfigureAwait(false);
            if (copyResult.Status != AuditSubmitOrdinaryCopyStatus.CopiedMarkdown ||
                string.IsNullOrWhiteSpace(copyResult.Markdown))
            {
                return null;
            }

            var candidates = new[]
            {
                new AuditSubmitReportCandidate(copyResult.Markdown, AuditSubmitReportCandidateSource.AssistantMessage)
            };
            var extraction = AuditSubmitReportExtractor.Extract(candidates, generationComplete: true);
            return extraction.Ready ? extraction.Report : null;
        }
        catch (AuditSubmitCodexChromeException ex) when (IsTransientOrdinaryCopyFailure(ex))
        {
            return null;
        }
    }

    private static string ExtractDownloadedReportOrThrow(IReadOnlyList<AuditSubmitReportCandidate> candidates)
    {
        var extraction = AuditSubmitReportExtractor.Extract(candidates, generationComplete: true);
        if (extraction.Ready && extraction.Report is not null)
        {
            return extraction.Report;
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID",
            string.IsNullOrWhiteSpace(extraction.FailureReason)
                ? "The downloaded Markdown report does not match the strict final report contract."
                : $"The downloaded Markdown report does not match the strict final report contract: {extraction.FailureReason}");
    }

    private static async Task<string> WaitForOrdinaryChatReportAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitOptions options,
        int messageCountBeforeSend,
        CancellationToken cancellationToken)
    {
        var driver = new AuditSubmitOrdinaryReportDriver(browser, tabId);
        return await WaitForOrdinaryChatReportAsync(
            driver,
            options.PollSeconds,
            messageCountBeforeSend,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> WaitForOrdinaryChatReportAsync(
        IAuditSubmitOrdinaryReportDriver driver,
        int pollSeconds,
        int messageCountBeforeSend,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(pollSeconds);
        var minimumMessageCount = messageCountBeforeSend + 2;
        var validReportStability = new AuditSubmitReportStabilityTracker(() => driver.UtcNow);
        string? lastInvalidCandidate = null;
        string? lastInvalidReason = null;
        DateTimeOffset lastInvalidCandidateFirstSeenUtc = default;
        var invalidCandidateStableAge = TimeSpan.FromSeconds(30);
        string? lastOrdinaryCopyFailure = null;
        DateTimeOffset lastOrdinaryCopyFailureFirstSeenUtc = default;
        var ordinaryCopyButtonMissing = false;
        DateTimeOffset ordinaryCopyButtonMissingFirstSeenUtc = default;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isGenerating = await driver.IsGeneratingAsync(cancellationToken).ConfigureAwait(false);
            string? clipboardMarkdown = null;
            if (!isGenerating)
            {
                try
                {
                    var copyResult = await driver.CopyLatestAssistantMessageMarkdownAsync(minimumMessageCount, cancellationToken).ConfigureAwait(false);
                    lastOrdinaryCopyFailure = null;
                    lastOrdinaryCopyFailureFirstSeenUtc = default;
                    if (copyResult.Status == AuditSubmitOrdinaryCopyStatus.CopiedMarkdown &&
                        !string.IsNullOrWhiteSpace(copyResult.Markdown))
                    {
                        clipboardMarkdown = copyResult.Markdown;
                        ordinaryCopyButtonMissing = false;
                        ordinaryCopyButtonMissingFirstSeenUtc = default;
                    }
                    else if (copyResult.Status == AuditSubmitOrdinaryCopyStatus.CopyActionUnavailable)
                    {
                        if (!ordinaryCopyButtonMissing)
                        {
                            ordinaryCopyButtonMissing = true;
                            ordinaryCopyButtonMissingFirstSeenUtc = driver.UtcNow;
                        }
                        else if (driver.UtcNow - ordinaryCopyButtonMissingFirstSeenUtc >= OrdinaryCopyFailureStableAge)
                        {
                            throw new AuditSubmitCodexChromeException(
                                "E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE",
                                "The ordinary ChatGPT assistant response copy button was not available after generation completed.");
                        }
                    }
                    else
                    {
                        ordinaryCopyButtonMissing = false;
                        ordinaryCopyButtonMissingFirstSeenUtc = default;
                    }
                }
                catch (AuditSubmitCodexChromeException ex) when (IsTransientOrdinaryCopyFailure(ex))
                {
                    ordinaryCopyButtonMissing = false;
                    ordinaryCopyButtonMissingFirstSeenUtc = default;
                    if (!string.Equals(lastOrdinaryCopyFailure, ex.Message, StringComparison.Ordinal))
                    {
                        lastOrdinaryCopyFailure = ex.Message;
                        lastOrdinaryCopyFailureFirstSeenUtc = driver.UtcNow;
                    }
                    else if (driver.UtcNow - lastOrdinaryCopyFailureFirstSeenUtc >= OrdinaryCopyFailureStableAge)
                    {
                        throw new AuditSubmitCodexChromeException(
                            "E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE",
                            $"The ordinary ChatGPT response copy action repeatedly failed before a valid Markdown report could be read: {ex.Message}");
                    }
                }
            }
            else
            {
                ordinaryCopyButtonMissing = false;
                ordinaryCopyButtonMissingFirstSeenUtc = default;
            }

            var candidates = string.IsNullOrWhiteSpace(clipboardMarkdown)
                ? Array.Empty<AuditSubmitReportCandidate>()
                : [new AuditSubmitReportCandidate(clipboardMarkdown, AuditSubmitReportCandidateSource.AssistantMessage)];

            if (candidates.Length == 1)
            {
                var extraction = AuditSubmitReportExtractor.Extract(candidates, generationComplete: !isGenerating);
                if (extraction.Ready && extraction.Report is not null)
                {
                    var decision = validReportStability.Decide(candidates, isGenerating);
                    if (decision.Action == AuditSubmitPollingAction.ReturnReport && decision.Report is not null)
                    {
                        return decision.Report;
                    }
                }
                else if (!isGenerating)
                {
                    var candidateText = candidates[0].Text;
                    if (!string.Equals(lastInvalidCandidate, candidateText, StringComparison.Ordinal))
                    {
                        lastInvalidCandidate = candidateText;
                        lastInvalidReason = extraction.FailureReason;
                        lastInvalidCandidateFirstSeenUtc = driver.UtcNow;
                    }
                    else if (driver.UtcNow - lastInvalidCandidateFirstSeenUtc >= invalidCandidateStableAge)
                    {
                        throw new AuditSubmitCodexChromeException(
                            "E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID",
                            string.IsNullOrWhiteSpace(lastInvalidReason)
                                ? "The ordinary ChatGPT assistant response copied from the response action does not match the strict final report contract."
                                : $"The ordinary ChatGPT assistant response copied from the response action does not match the strict final report contract: {lastInvalidReason}");
                    }
                }
            }
            else
            {
                lastInvalidCandidate = null;
                lastInvalidReason = null;
                lastInvalidCandidateFirstSeenUtc = default;
            }

            await driver.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsTransientOrdinaryCopyFailure(AuditSubmitCodexChromeException exception)
    {
        return string.Equals(exception.Code, "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL", StringComparison.Ordinal) &&
            IsRecoverableOrdinaryCopyFailureMessage(exception.Message);
    }

    private static bool IsRecoverableOrdinaryCopyFailureMessage(string message)
    {
        return message.Contains("Debugger unattached", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Debugger is not attached", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Timed out", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Cannot find context", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("Target closed", StringComparison.OrdinalIgnoreCase);
    }

    private interface IAuditSubmitOrdinaryReportDriver
    {
        DateTimeOffset UtcNow { get; }

        Task<bool> IsGeneratingAsync(CancellationToken cancellationToken);

        Task<AuditSubmitOrdinaryCopyResult> CopyLatestAssistantMessageMarkdownAsync(int minimumMessageCount, CancellationToken cancellationToken);

        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }

    private enum AuditSubmitOrdinaryCopyStatus
    {
        NoCurrentAssistantYet,
        CopyActionUnavailable,
        CopiedMarkdown
    }

    private readonly record struct AuditSubmitOrdinaryCopyResult(AuditSubmitOrdinaryCopyStatus Status, string? Markdown)
    {
        public static AuditSubmitOrdinaryCopyResult NoCurrentAssistantYet() =>
            new(AuditSubmitOrdinaryCopyStatus.NoCurrentAssistantYet, null);

        public static AuditSubmitOrdinaryCopyResult CopyActionUnavailable() =>
            new(AuditSubmitOrdinaryCopyStatus.CopyActionUnavailable, null);

        public static AuditSubmitOrdinaryCopyResult CopiedMarkdown(string markdown) =>
            new(AuditSubmitOrdinaryCopyStatus.CopiedMarkdown, markdown);
    }

    private enum AuditSubmitAssistantCopyButtonStatus
    {
        NoCurrentAssistantYet,
        CopyButtonMissing,
        Ready
    }

    private readonly record struct AuditSubmitAssistantCopyButtonState(
        AuditSubmitAssistantCopyButtonStatus Status,
        AuditSubmitDomPoint? Point);

    private sealed class AuditSubmitOrdinaryReportDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitOrdinaryReportDriver
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public async Task<bool> IsGeneratingAsync(CancellationToken cancellationToken)
        {
            _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
            return await browser.EvaluateBoolAsync(tabId, IsGeneratingExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuditSubmitOrdinaryCopyResult> CopyLatestAssistantMessageMarkdownAsync(int minimumMessageCount, CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.CopyLatestAssistantMessageMarkdownAsync(
                browser,
                tabId,
                minimumMessageCount,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<AuditSubmitOrdinaryCopyResult> CopyLatestAssistantMessageMarkdownAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        int minimumMessageCount,
        CancellationToken cancellationToken)
    {
        var buttonStateValue = await browser.EvaluateAsync(
            tabId,
            LastAssistantCopyButtonStateExpression(minimumMessageCount),
            UiActionTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        var buttonState = ReadAssistantCopyButtonState(buttonStateValue);
        if (buttonState.Status == AuditSubmitAssistantCopyButtonStatus.NoCurrentAssistantYet)
        {
            return AuditSubmitOrdinaryCopyResult.NoCurrentAssistantYet();
        }

        if (buttonState.Status != AuditSubmitAssistantCopyButtonStatus.Ready || buttonState.Point is null)
        {
            return AuditSubmitOrdinaryCopyResult.CopyActionUnavailable();
        }

        var clipboardSentinel = CreateClipboardSentinel();
        var sentinelInstalled = SystemClipboardTextAccess.TrySetText(clipboardSentinel);
        await GrantClipboardReadPermissionBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await InstallClipboardWriteCaptureBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await ResetClipboardWriteCaptureBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await browser.ClickAtAsync(tabId, buttonState.Point.Value, cancellationToken).ConfigureAwait(false);
        await Task.Delay(ClipboardSettleDelay, cancellationToken).ConfigureAwait(false);
        var systemClipboardText = await ReadSystemClipboardTextAfterCopyAsync(
            clipboardSentinel,
            sentinelInstalled,
            cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(systemClipboardText))
        {
            return AuditSubmitOrdinaryCopyResult.CopiedMarkdown(systemClipboardText);
        }

        if (await ClickLatestAssistantCopyButtonDomBestEffortAsync(browser, tabId, minimumMessageCount, cancellationToken).ConfigureAwait(false))
        {
            await Task.Delay(ClipboardSettleDelay, cancellationToken).ConfigureAwait(false);
            systemClipboardText = await ReadSystemClipboardTextAfterCopyAsync(
                clipboardSentinel,
                sentinelInstalled,
                cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(systemClipboardText))
            {
                return AuditSubmitOrdinaryCopyResult.CopiedMarkdown(systemClipboardText);
            }
        }

        await GrantClipboardReadPermissionBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        var clipboardText = await ReadClipboardTextAsync(
            browser,
            tabId,
            sentinelInstalled ? clipboardSentinel : null,
            cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(clipboardText)
            ? AuditSubmitOrdinaryCopyResult.CopyActionUnavailable()
            : AuditSubmitOrdinaryCopyResult.CopiedMarkdown(clipboardText);
    }

    private static AuditSubmitAssistantCopyButtonState ReadAssistantCopyButtonState(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("status", out var statusElement) ||
            statusElement.ValueKind != JsonValueKind.String)
        {
            return new AuditSubmitAssistantCopyButtonState(AuditSubmitAssistantCopyButtonStatus.CopyButtonMissing, null);
        }

        return statusElement.GetString() switch
        {
            "no-current-assistant-yet" => new AuditSubmitAssistantCopyButtonState(AuditSubmitAssistantCopyButtonStatus.NoCurrentAssistantYet, null),
            "copy-button-ready" when TryReadAssistantCopyButtonPoint(value, out var point) => new AuditSubmitAssistantCopyButtonState(AuditSubmitAssistantCopyButtonStatus.Ready, point),
            _ => new AuditSubmitAssistantCopyButtonState(AuditSubmitAssistantCopyButtonStatus.CopyButtonMissing, null)
        };
    }

    private static bool TryReadAssistantCopyButtonPoint(JsonElement value, out AuditSubmitDomPoint point)
    {
        point = default;
        if (!TryReadFiniteDouble(value, "x", out var x) ||
            !TryReadFiniteDouble(value, "y", out var y))
        {
            return false;
        }

        point = new AuditSubmitDomPoint(x, y);
        return true;
    }

    private static string CreateClipboardSentinel()
    {
        return $"E2D-AUDIT-SUBMIT-CLIPBOARD-SENTINEL-{Guid.NewGuid():N}";
    }

    private static async Task<string?> ReadSystemClipboardTextAfterCopyAsync(
        string sentinel,
        bool sentinelInstalled,
        CancellationToken cancellationToken)
    {
        if (!sentinelInstalled)
        {
            return null;
        }

        var deadline = DateTimeOffset.UtcNow + SystemClipboardReadTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = SystemClipboardTextAccess.TryGetText();
            if (CanAcceptSystemClipboardText(text, sentinel, sentinelInstalled))
            {
                return text;
            }

            await Task.Delay(ClipboardSettleDelay, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static bool CanAcceptSystemClipboardText(string? text, string sentinel, bool sentinelInstalled)
    {
        return sentinelInstalled &&
            !string.IsNullOrWhiteSpace(text) &&
            !string.Equals(text, sentinel, StringComparison.Ordinal);
    }

    private static async Task<bool> ClickLatestAssistantCopyButtonDomBestEffortAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        int minimumMessageCount,
        CancellationToken cancellationToken)
    {
        try
        {
            return await browser.EvaluateBoolAsync(
                tabId,
                LastAssistantCopyButtonClickExpression(minimumMessageCount),
                TimeSpan.FromSeconds(10),
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException)
        {
            return false;
        }
    }

    private static async Task InstallClipboardWriteCaptureBestEffortAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await browser.EvaluateBoolAsync(
                tabId,
                ClipboardWriteCaptureInstallExpression,
                TimeSpan.FromSeconds(10),
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException)
        {
        }
    }

    private static async Task ResetClipboardWriteCaptureBestEffortAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            _ = await browser.EvaluateBoolAsync(
                tabId,
                ClipboardWriteCaptureResetExpression,
                TimeSpan.FromSeconds(10),
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException)
        {
        }
    }

    private static async Task GrantClipboardReadPermissionBestEffortAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            var origin = await browser.EvaluateAsync(
                tabId,
                "(() => location.origin)()",
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
            if (origin.ValueKind != JsonValueKind.String ||
                string.IsNullOrWhiteSpace(origin.GetString()))
            {
                return;
            }

            await browser.ExecuteCdpAsync(
                tabId,
                "Browser.grantPermissions",
                new Dictionary<string, object?>
                {
                    ["origin"] = origin.GetString(),
                    ["permissions"] = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" }
                },
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is AuditSubmitCodexChromeException or JsonException)
        {
        }
    }

    private static async Task<string> ReadClipboardTextAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string? staleClipboardText,
        CancellationToken cancellationToken)
    {
        var readValue = default(JsonElement);
        if (CanTrustBrowserClipboardReadText(staleClipboardText))
        {
            readValue = await browser.EvaluateAsync(
                tabId,
                ClipboardReadTextExpression,
                TimeSpan.FromSeconds(10),
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
            if (TryReadClipboardResult(readValue, staleClipboardText, out var clipboardText, out _))
            {
                return clipboardText;
            }
        }

        var captured = await browser.EvaluateAsync(
            tabId,
            ClipboardCapturedWriteTextExpression,
            TimeSpan.FromSeconds(10),
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        if (TrySelectClipboardText(readValue, captured, staleClipboardText, out var capturedText, out var readClipboardError, out var capturedError))
        {
            return capturedText;
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE",
            "The ordinary ChatGPT response copy action completed, but clipboard Markdown could not be read. " +
            $"navigator.clipboard.readText(): {readClipboardError}; captured copy action Markdown: {capturedError}");
    }

    private static bool CanTrustBrowserClipboardReadText(string? staleClipboardText)
    {
        return !string.IsNullOrEmpty(staleClipboardText);
    }

    private static bool TrySelectClipboardText(
        JsonElement browserReadValue,
        JsonElement capturedWriteValue,
        string? staleClipboardText,
        out string text,
        out string readClipboardError,
        out string capturedError)
    {
        text = string.Empty;
        readClipboardError = string.IsNullOrEmpty(staleClipboardText)
            ? "system clipboard sentinel was not installed; navigator.clipboard.readText cannot prove the current copy action."
            : string.Empty;
        if (CanTrustBrowserClipboardReadText(staleClipboardText) &&
            TryReadClipboardResult(browserReadValue, staleClipboardText, out text, out readClipboardError))
        {
            capturedError = string.Empty;
            return true;
        }

        if (TryReadClipboardResult(capturedWriteValue, staleClipboardText, out text, out capturedError))
        {
            return true;
        }

        return false;
    }

    private static bool TryReadClipboardResult(JsonElement value, string? staleClipboardText, out string text, out string error)
    {
        text = string.Empty;
        error = "clipboard expression did not return Markdown text.";
        if (value.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (value.TryGetProperty("ok", out var ok) &&
            ok.ValueKind == JsonValueKind.True &&
            value.TryGetProperty("text", out var textElement) &&
            textElement.ValueKind == JsonValueKind.String)
        {
            text = textElement.GetString() ?? string.Empty;
            if (!string.IsNullOrEmpty(staleClipboardText) &&
                string.Equals(text, staleClipboardText, StringComparison.Ordinal))
            {
                error = "clipboard text still contains the sentinel value.";
                text = string.Empty;
                return false;
            }

            error = string.Empty;
            return true;
        }

        if (value.TryGetProperty("error", out var errorElement) &&
            errorElement.ValueKind == JsonValueKind.String)
        {
            error = errorElement.GetString() ?? error;
        }

        return false;
    }

    private static async Task<AuditSubmitPollingDecision> CapturePollingDecisionAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        if (await browser.EvaluateBoolAsync(tabId, IsGeneratingExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
        {
            return AuditSubmitPollingPolicy.Decide([], isGenerating: true);
        }

        var candidates = await DownloadReportCandidatesAsync(
            browser,
            tabId,
            downloadsDirectory,
            includeUserDownloadsFallback,
            ignoredDeepResearchTargetIds,
            allowLatestReadyTargetFallback: true,
            cancellationToken).ConfigureAwait(false);
        if (await browser.EvaluateBoolAsync(tabId, IsGeneratingExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
        {
            return AuditSubmitPollingPolicy.Decide([], isGenerating: true);
        }

        return AuditSubmitPollingPolicy.Decide(candidates, isGenerating: false);
    }

    private static async Task WaitForConversationMessagesAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        int minimumMessageCount,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var messageCount = await ReadConversationMessageCountAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
            if (HasRequiredConversationMessageCount(messageCount, minimumMessageCount))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-MISSING",
            "ChatGPT did not show submitted conversation messages after sending or reloading the page.");
    }

    private static async Task<int> ReadConversationMessageCountAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var value = await browser.EvaluateAsync(tabId, ConversationMessageCountExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false);
        return TryReadNonNegativeInt32(value, out var count) ? count : 0;
    }

    private static bool HasRequiredConversationMessageCount(int currentMessageCount, int minimumMessageCount)
    {
        return currentMessageCount >= Math.Max(0, minimumMessageCount);
    }

    private static async Task<string> ReadCurrentLocationHrefAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            var value = await browser.EvaluateAsync(
                tabId,
                "(() => location.href)()",
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
            return value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (AuditSubmitCodexChromeException)
        {
            return string.Empty;
        }
    }

    private static async Task<string> WaitForConcreteConversationUrlAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var lastUrl = string.Empty;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            lastUrl = await ReadCurrentLocationHrefAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
            if (AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(lastUrl))
            {
                return lastUrl;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
            string.IsNullOrWhiteSpace(lastUrl)
                ? "ChatGPT did not expose a concrete conversation URL after the audit submit message was sent."
                : $"ChatGPT did not expose a concrete conversation URL after the audit submit message was sent. Last URL: {lastUrl}");
    }

    private static async Task WriteConversationUrlSidecarAsync(
        string repoRoot,
        string zipPath,
        string conversationUrl,
        bool controlAudit,
        CancellationToken cancellationToken)
    {
        RequireConcreteConversationUrlForSidecar(conversationUrl);

        var fileName = Path.GetFileName(zipPath);
        var match = AuditZipFileNameExpression.Match(fileName);
        if (!match.Success)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
                $"Cannot derive task and iteration for the conversation URL sidecar from audit ZIP name: {fileName}");
        }

        var taskId = match.Groups["task"].Value.ToUpperInvariant();
        var iteration = match.Groups["iteration"].Value.ToLowerInvariant();
        var directory = Path.Combine(repoRoot, ".temp", "audit", taskId);
        Directory.CreateDirectory(directory);
        var prefix = controlAudit ? "control-conversation-url" : "conversation-url";
        var path = Path.Combine(directory, $"{prefix}-{iteration}.txt");
        await File.WriteAllTextAsync(
            path,
            conversationUrl.Trim() + Environment.NewLine,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
        await File.WriteAllTextAsync(
            Path.Combine(directory, $"{prefix}.txt"),
            conversationUrl.Trim() + Environment.NewLine,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken).ConfigureAwait(false);
    }

    private static void RequireConcreteConversationUrlForSidecar(string conversationUrl)
    {
        if (!AuditSubmitUrlRules.IsConcreteChatGptConversationUrl(conversationUrl))
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CONVERSATION-URL-MISSING",
                "Cannot write audit submit conversation URL sidecar because ChatGPT did not provide a concrete /c/<conversation-id> URL.");
        }
    }

    private static async Task<bool> WaitForDeepResearchFrameContentAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hasIframe = await browser.EvaluateBoolAsync(tabId, DeepResearchIframeVisibleExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            if (!hasIframe)
            {
                return false;
            }

            var frameTree = await browser.ExecuteCdpAsync(tabId, "Page.getFrameTree", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            if (SelectSingleDeepResearchFrameId(ReadFrameTreeEntries(frameTree)) is not null)
            {
                return true;
            }

            return true;
        }

        return false;
    }

    private static async Task<AuditSubmitReportCandidate[]> DownloadReportCandidatesAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestReadyTargetFallback,
        CancellationToken cancellationToken)
    {
        _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await ScrollConversationToBottomAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
        _ = await DismissRateLimitDialogAsync(browser, tabId, cancellationToken).ConfigureAwait(false);

        return await DownloadReportCandidatesAsync(
            new AuditSubmitReportCandidateDownloadDriver(
                browser,
                tabId,
                downloadsDirectory,
                includeUserDownloadsFallback,
                ignoredDeepResearchTargetIds,
                allowLatestReadyTargetFallback),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AuditSubmitReportCandidate[]> DownloadReportCandidatesAsync(
        IAuditSubmitReportCandidateDownloadDriver driver,
        CancellationToken cancellationToken)
    {
        var frameResult = await driver.DownloadFromFrameAsync(cancellationToken).ConfigureAwait(false);
        if (frameResult.SurfaceSelected)
        {
            return frameResult.Candidates;
        }

        var targetSelection = await driver.SelectTargetAsync(cancellationToken).ConfigureAwait(false);
        if (targetSelection.WaitForNewerTarget)
        {
            return [];
        }

        if (!string.IsNullOrWhiteSpace(targetSelection.TargetId))
        {
            var targetResult = await driver.DownloadFromTargetAsync(targetSelection.TargetId, cancellationToken).ConfigureAwait(false);
            if (targetResult.SurfaceSelected)
            {
                return targetResult.Candidates;
            }
        }

        return await driver.DownloadFromPageAsync(cancellationToken).ConfigureAwait(false);
    }

    private interface IAuditSubmitReportCandidateDownloadDriver
    {
        Task<AuditSubmitReportCandidateResult> DownloadFromFrameAsync(CancellationToken cancellationToken);

        Task<AuditSubmitDeepResearchTargetSelection> SelectTargetAsync(CancellationToken cancellationToken);

        Task<AuditSubmitReportCandidateResult> DownloadFromTargetAsync(string targetId, CancellationToken cancellationToken);

        Task<AuditSubmitReportCandidate[]> DownloadFromPageAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitReportCandidateDownloadDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestReadyTargetFallback) : IAuditSubmitReportCandidateDownloadDriver
    {
        public async Task<AuditSubmitReportCandidateResult> DownloadFromFrameAsync(CancellationToken cancellationToken)
        {
            return await DownloadReportCandidatesFromDeepResearchFrameAsync(
                browser,
                tabId,
                downloadsDirectory,
                includeUserDownloadsFallback,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuditSubmitDeepResearchTargetSelection> SelectTargetAsync(CancellationToken cancellationToken)
        {
            return await SelectReadyDeepResearchTargetAsync(
                browser,
                tabId,
                ignoredDeepResearchTargetIds,
                allowLatestReadyTargetFallback,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuditSubmitReportCandidateResult> DownloadFromTargetAsync(string targetId, CancellationToken cancellationToken)
        {
            return await DownloadReportCandidatesFromDeepResearchTargetAsync(
                browser,
                tabId,
                targetId,
                downloadsDirectory,
                includeUserDownloadsFallback,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuditSubmitReportCandidate[]> DownloadFromPageAsync(CancellationToken cancellationToken)
        {
            return await ClickReportExportAndReadDownloadedMarkdownAsync(
                browser,
                tabId,
                downloadsDirectory,
                includeUserDownloadsFallback,
                AuditSubmitExportSurfaceScope.Page,
                () => browser.EvaluateBoolAsync(tabId, ReportExportButtonClickExpression, UiActionTimeout, cancellationToken),
                () => Task.FromResult(false),
                () => browser.EvaluateBoolAsync(tabId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private interface IAuditSubmitDeepResearchMarkdownExportDriver
    {
        Task<AuditSubmitReportCandidate[]> ClickReportExportAndReadDownloadedMarkdownAsync(
            AuditSubmitExportSurfaceScope surfaceScope,
            Func<Task<bool>> clickExportButtonAsync,
            Func<Task<bool>> clickScopedMarkdownMenuItemAsync,
            Func<Task<bool>> clickPageMarkdownMenuItemAsync,
            CancellationToken cancellationToken);

        Task<bool> ClickPageMarkdownMenuItemAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitDeepResearchMarkdownExportDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback) : IAuditSubmitDeepResearchMarkdownExportDriver
    {
        public async Task<AuditSubmitReportCandidate[]> ClickReportExportAndReadDownloadedMarkdownAsync(
            AuditSubmitExportSurfaceScope surfaceScope,
            Func<Task<bool>> clickExportButtonAsync,
            Func<Task<bool>> clickScopedMarkdownMenuItemAsync,
            Func<Task<bool>> clickPageMarkdownMenuItemAsync,
            CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.ClickReportExportAndReadDownloadedMarkdownAsync(
                browser,
                tabId,
                downloadsDirectory,
                includeUserDownloadsFallback,
                surfaceScope,
                clickExportButtonAsync,
                clickScopedMarkdownMenuItemAsync,
                clickPageMarkdownMenuItemAsync,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> ClickPageMarkdownMenuItemAsync(CancellationToken cancellationToken)
        {
            return await browser.EvaluateBoolAsync(
                tabId,
                ExportReportMarkdownMenuItemClickExpression,
                UiActionTimeout,
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
        }
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromSelectedDeepResearchSurfaceAsync(
        IAuditSubmitDeepResearchMarkdownExportDriver exportDriver,
        AuditSubmitExportSurfaceScope surfaceScope,
        Func<Task<bool>> clickExportButtonAsync,
        Func<Task<bool>> clickScopedMarkdownMenuItemAsync,
        CancellationToken cancellationToken)
    {
        var candidates = await exportDriver.ClickReportExportAndReadDownloadedMarkdownAsync(
            surfaceScope,
            clickExportButtonAsync,
            clickScopedMarkdownMenuItemAsync,
            () => exportDriver.ClickPageMarkdownMenuItemAsync(cancellationToken),
            cancellationToken).ConfigureAwait(false);
        return new AuditSubmitReportCandidateResult(true, candidates);
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromDeepResearchTargetAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string targetId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        CancellationToken cancellationToken)
    {
        await browser.AttachTargetWithRecoveryAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
        await browser.EnsureTargetAttachedForReadAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeDeepResearchTargetAsync(browser, tabId, targetId, cancellationToken).ConfigureAwait(false);

            var frameResult = await DownloadReportCandidatesFromDeepResearchTargetFrameAsync(
                browser,
                tabId,
                targetId,
                downloadsDirectory,
                includeUserDownloadsFallback,
                cancellationToken).ConfigureAwait(false);
            if (frameResult.SurfaceSelected)
            {
                return frameResult;
            }

            return await DownloadReportCandidatesFromSelectedDeepResearchSurfaceAsync(
                new AuditSubmitDeepResearchMarkdownExportDriver(
                    browser,
                    tabId,
                    downloadsDirectory,
                    includeUserDownloadsFallback),
                AuditSubmitExportSurfaceScope.DeepResearchTarget,
                () => browser.EvaluateBoolOnTargetAsync(tabId, targetId, ReportExportButtonClickExpression, TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false),
                () => browser.EvaluateBoolOnTargetAsync(tabId, targetId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await browser.DetachTargetAsync(tabId, targetId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromDeepResearchFrameAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        CancellationToken cancellationToken)
    {
        var hasVisibleIframe = await browser.EvaluateBoolAsync(tabId, DeepResearchIframeVisibleExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false);
        if (!hasVisibleIframe)
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        var frame = await TryCreateDeepResearchFrameContextAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        if (frame is null)
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        var hasReadyReport = await browser.EvaluateBoolInContextAsync(
            tabId,
            frame.Value.ContextId,
            DeepResearchReportTargetReadyExpression,
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        if (!CanUseDeepResearchFrameSurface(hasVisibleIframe, hasFrameContext: true, hasReadyReport))
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        return await DownloadReportCandidatesFromSelectedDeepResearchSurfaceAsync(
            new AuditSubmitDeepResearchMarkdownExportDriver(
                browser,
                tabId,
                downloadsDirectory,
                includeUserDownloadsFallback),
            AuditSubmitExportSurfaceScope.DeepResearchFrame,
            () => browser.EvaluateBoolInContextAsync(tabId, frame.Value.ContextId, ReportExportButtonClickExpression, TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false),
            () => browser.EvaluateBoolInContextAsync(tabId, frame.Value.ContextId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false),
            cancellationToken).ConfigureAwait(false);
    }

    private static bool CanUseDeepResearchFrameSurface(bool hasVisibleIframe, bool hasFrameContext, bool hasReadyReport)
    {
        return hasVisibleIframe && hasFrameContext && hasReadyReport;
    }

    private static async Task<AuditSubmitReportCandidate[]> ClickReportExportAndReadDownloadedMarkdownAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        AuditSubmitExportSurfaceScope surfaceScope,
        Func<Task<bool>> clickExportButtonAsync,
        Func<Task<bool>> clickScopedMarkdownMenuItemAsync,
        Func<Task<bool>> clickPageMarkdownMenuItemAsync,
        CancellationToken cancellationToken)
    {
        var acceptedDownloadDirectories = GetDownloadSearchDirectories(downloadsDirectory, includeUserDownloadsFallback);
        var observedDownloadDirectories = GetObservedDownloadSearchDirectories(downloadsDirectory);
        var knownFiles = SnapshotDownloadFiles(observedDownloadDirectories);
        var clickedMarkdown = await ClickMarkdownMenuItemAsync(
            surfaceScope,
            clickScopedMarkdownMenuItemAsync,
            clickPageMarkdownMenuItemAsync,
            selectedExportButtonClicked: false).ConfigureAwait(false);
        if (!clickedMarkdown)
        {
            if (!await clickExportButtonAsync().ConfigureAwait(false))
            {
                return [];
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
            var directCandidates = await ReadDirectMarkdownDownloadCandidatesAsync(
                observedDownloadDirectories,
                acceptedDownloadDirectories,
                knownFiles,
                TimeSpan.FromSeconds(3),
                cancellationToken).ConfigureAwait(false);
            if (directCandidates.Length > 0)
            {
                return directCandidates;
            }

            clickedMarkdown = await ClickMarkdownMenuItemAsync(
                surfaceScope,
                clickScopedMarkdownMenuItemAsync,
                clickPageMarkdownMenuItemAsync,
                selectedExportButtonClicked: true).ConfigureAwait(false);
        }

        if (!clickedMarkdown)
        {
            return [];
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);

        var reportPath = await WaitForMarkdownDownloadAsync(observedDownloadDirectories, acceptedDownloadDirectories, knownFiles, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        var report = string.IsNullOrWhiteSpace(reportPath)
            ? string.Empty
            : await File.ReadAllTextAsync(reportPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(report)
            ? []
            : [new AuditSubmitReportCandidate(report, AuditSubmitReportCandidateSource.OpenedReportCard)];
    }

    private static async Task<AuditSubmitReportCandidate[]> ReadDirectMarkdownDownloadCandidatesAsync(
        IReadOnlyList<string> observedDownloadDirectories,
        IReadOnlyList<string> acceptedDownloadDirectories,
        HashSet<string> knownFiles,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var directDownloadPath = await WaitForMarkdownDownloadAsync(
            observedDownloadDirectories,
            acceptedDownloadDirectories,
            knownFiles,
            timeout,
            cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(directDownloadPath))
        {
            return [];
        }

        var directReport = await File.ReadAllTextAsync(directDownloadPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(directReport)
            ? []
            : [new AuditSubmitReportCandidate(directReport, AuditSubmitReportCandidateSource.OpenedReportCard)];
    }

    private static async Task<bool> ClickMarkdownMenuItemAsync(
        AuditSubmitExportSurfaceScope surfaceScope,
        Func<Task<bool>> clickScopedMarkdownMenuItemAsync,
        Func<Task<bool>> clickPageMarkdownMenuItemAsync,
        bool selectedExportButtonClicked)
    {
        if (ResolveMarkdownMenuItemClickResult(
            surfaceScope,
            scopedMenuItemClicked: await clickScopedMarkdownMenuItemAsync().ConfigureAwait(false),
            pageMenuItemClicked: false,
            selectedExportButtonClicked))
        {
            return true;
        }

        if (!CanUsePageLevelMarkdownMenu(surfaceScope, selectedExportButtonClicked))
        {
            return false;
        }

        return ResolveMarkdownMenuItemClickResult(
            surfaceScope,
            scopedMenuItemClicked: false,
            pageMenuItemClicked: await clickPageMarkdownMenuItemAsync().ConfigureAwait(false),
            selectedExportButtonClicked);
    }

    private static bool ResolveMarkdownMenuItemClickResult(
        AuditSubmitExportSurfaceScope surfaceScope,
        bool scopedMenuItemClicked,
        bool pageMenuItemClicked,
        bool selectedExportButtonClicked)
    {
        return scopedMenuItemClicked || (CanUsePageLevelMarkdownMenu(surfaceScope, selectedExportButtonClicked) && pageMenuItemClicked);
    }

    private static bool CanUsePageLevelMarkdownMenu(AuditSubmitExportSurfaceScope surfaceScope, bool selectedExportButtonClicked)
    {
        return surfaceScope == AuditSubmitExportSurfaceScope.Page ||
            (selectedExportButtonClicked &&
            surfaceScope is
                AuditSubmitExportSurfaceScope.DeepResearchFrame or
                AuditSubmitExportSurfaceScope.DeepResearchTarget or
                AuditSubmitExportSurfaceScope.DeepResearchTargetFrame);
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromDeepResearchTargetFrameAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string targetId,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        CancellationToken cancellationToken)
    {
        var contexts = await ReadReadyDeepResearchTargetFrameContextsAsync(browser, tabId, targetId, cancellationToken).ConfigureAwait(false);
        var context = SelectSingleReadyDeepResearchTargetFrameContext(contexts);
        if (context is null)
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return await DownloadReportCandidatesFromSelectedDeepResearchSurfaceAsync(
            new AuditSubmitDeepResearchMarkdownExportDriver(
                browser,
                tabId,
                downloadsDirectory,
                includeUserDownloadsFallback),
            AuditSubmitExportSurfaceScope.DeepResearchTargetFrame,
            () => browser.EvaluateBoolInContextOnTargetAsync(tabId, targetId, context.Value.ContextId, ReportExportButtonClickExpression, TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false),
            () => browser.EvaluateBoolInContextOnTargetAsync(tabId, targetId, context.Value.ContextId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false),
            cancellationToken).ConfigureAwait(false);
    }

    private static AuditSubmitTargetFrameContext? SelectSingleReadyDeepResearchTargetFrameContext(IReadOnlyList<AuditSubmitTargetFrameContext> contexts)
    {
        if (contexts.Count == 0)
        {
            return null;
        }

        var nonRootContexts = contexts.Where(static context => !context.IsRoot).ToArray();
        if (nonRootContexts.Length == 1)
        {
            return nonRootContexts[0];
        }

        if (nonRootContexts.Length > 1 || contexts.Count > 1)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-AMBIGUOUS",
                "The Deep Research target exposed multiple ready report frame contexts.");
        }

        return contexts[0];
    }

    private static async Task InitializeDeepResearchTargetAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string targetId,
        CancellationToken cancellationToken)
    {
        foreach (var method in new[] { "Page.enable", "Runtime.enable", "DOM.enable" })
        {
            await browser.ExecuteCdpOnTargetAsync(tabId, targetId, method, EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<IReadOnlyList<AuditSubmitTargetFrameContext>> ReadReadyDeepResearchTargetFrameContextsAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string targetId,
        CancellationToken cancellationToken)
    {
        var frameTree = await browser.ExecuteCdpOnTargetAsync(tabId, targetId, "Page.getFrameTree", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        var contexts = new List<AuditSubmitTargetFrameContext>();
        foreach (var frame in ReadFrameTreeEntries(frameTree)
            .OrderBy(static frame => frame.IsRoot ? 1 : 0)
            .ThenBy(static frame => string.Equals(frame.Url, "about:blank", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(static frame => frame.FrameId, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var contextId = await CreateFrameExecutionContextOnTargetAsync(browser, tabId, targetId, frame.FrameId, cancellationToken).ConfigureAwait(false);
                if (await browser.EvaluateBoolInContextOnTargetAsync(tabId, targetId, contextId, DeepResearchReportTargetReadyExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false))
                {
                    contexts.Add(new AuditSubmitTargetFrameContext(targetId, frame.FrameId, contextId, frame.IsRoot));
                }
            }
            catch (AuditSubmitCodexChromeException)
            {
            }
        }

        return contexts;
    }

    private static async Task<string?> TryFindSingleReadyDeepResearchTargetIdAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestReadyTargetFallback,
        CancellationToken cancellationToken)
    {
        return (await SelectReadyDeepResearchTargetAsync(
            browser,
            tabId,
            ignoredDeepResearchTargetIds,
            allowLatestReadyTargetFallback,
            cancellationToken).ConfigureAwait(false)).TargetId;
    }

    private static async Task<AuditSubmitDeepResearchTargetSelection> SelectReadyDeepResearchTargetAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestReadyTargetFallback,
        CancellationToken cancellationToken)
    {
        var targets = await ReadDeepResearchTargetsAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        var targetIds = new List<string>();
        var readyTargetIds = new List<string>();
        foreach (var target in targets)
        {
            targetIds.Add(target.TargetId);
            if (ignoredDeepResearchTargetIds.Contains(target.TargetId))
            {
                continue;
            }

            var attached = false;
            try
            {
                await browser.AttachTargetWithRecoveryAsync(tabId, target.TargetId, cancellationToken).ConfigureAwait(false);
                attached = true;
                await InitializeDeepResearchTargetAsync(browser, tabId, target.TargetId, cancellationToken).ConfigureAwait(false);
                var rootReady = await browser.EvaluateBoolOnTargetAsync(tabId, target.TargetId, DeepResearchReportTargetReadyExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                var frameReady = (await ReadReadyDeepResearchTargetFrameContextsAsync(browser, tabId, target.TargetId, cancellationToken).ConfigureAwait(false)).Count > 0;
                if (rootReady || frameReady)
                {
                    readyTargetIds.Add(target.TargetId);
                }
            }
            catch (AuditSubmitCodexChromeException)
            {
            }
            finally
            {
                if (attached)
                {
                    await browser.DetachTargetAsync(tabId, target.TargetId, CancellationToken.None).ConfigureAwait(false);
                }
            }
        }

        return SelectReadyDeepResearchTarget(targetIds, readyTargetIds, ignoredDeepResearchTargetIds, allowLatestWhenMultiple: allowLatestReadyTargetFallback);
    }

    private static string? SelectSingleReadyDeepResearchTargetId(IReadOnlyList<string> readyTargetIds)
    {
        return SelectReadyDeepResearchTargetId(readyTargetIds, allowLatestWhenMultiple: false);
    }

    private static string? SelectReadyDeepResearchTargetId(IReadOnlyList<string> readyTargetIds, bool allowLatestWhenMultiple)
    {
        return readyTargetIds.Count switch
        {
            0 => null,
            1 => readyTargetIds[0],
            _ when allowLatestWhenMultiple => readyTargetIds[^1],
            _ => null
        };
    }

    private static string? SelectReadyDeepResearchTargetId(
        IReadOnlyList<string> readyTargetIds,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestWhenMultiple)
    {
        var filteredReadyTargetIds = readyTargetIds
            .Where(targetId => !ignoredDeepResearchTargetIds.Contains(targetId))
            .ToArray();
        return SelectReadyDeepResearchTargetId(filteredReadyTargetIds, allowLatestWhenMultiple);
    }

    private static string? SelectReadyDeepResearchTargetId(
        IReadOnlyList<string> targetIds,
        IReadOnlyList<string> readyTargetIds,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestWhenMultiple)
    {
        return SelectReadyDeepResearchTarget(targetIds, readyTargetIds, ignoredDeepResearchTargetIds, allowLatestWhenMultiple).TargetId;
    }

    private static AuditSubmitDeepResearchTargetSelection SelectReadyDeepResearchTarget(
        IReadOnlyList<string> targetIds,
        IReadOnlyList<string> readyTargetIds,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        bool allowLatestWhenMultiple)
    {
        var readyTargetIdSet = readyTargetIds.ToHashSet(StringComparer.Ordinal);
        var filteredTargetIds = targetIds
            .Where(targetId => !ignoredDeepResearchTargetIds.Contains(targetId))
            .ToArray();
        if (filteredTargetIds.Length == 0)
        {
            return new AuditSubmitDeepResearchTargetSelection(null, WaitForNewerTarget: false);
        }

        var filteredReadyTargetIds = filteredTargetIds
            .Where(readyTargetIdSet.Contains)
            .ToArray();
        if (allowLatestWhenMultiple &&
            filteredReadyTargetIds.Length > 0 &&
            !string.Equals(filteredTargetIds[^1], filteredReadyTargetIds[^1], StringComparison.Ordinal))
        {
            return new AuditSubmitDeepResearchTargetSelection(null, WaitForNewerTarget: true);
        }

        return new AuditSubmitDeepResearchTargetSelection(
            SelectReadyDeepResearchTargetId(filteredReadyTargetIds, allowLatestWhenMultiple),
            WaitForNewerTarget: false);
    }

    private static async Task<HashSet<string>> SnapshotDeepResearchTargetIdsAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var targets = await ReadDeepResearchTargetsAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        return targets.Select(static target => target.TargetId).ToHashSet(StringComparer.Ordinal);
    }

    private static async Task<IReadOnlyList<AuditSubmitTargetInfoEntry>> ReadDeepResearchTargetsAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        try
        {
            var targetInfo = await browser.ExecuteCdpAsync(tabId, "Target.getTargets", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            return ReadTargetInfoEntries(targetInfo)
                .Where(IsDeepResearchTarget)
                .ToArray();
        }
        catch (AuditSubmitCodexChromeException)
        {
            return [];
        }
    }

    private static bool IsDeepResearchTarget(AuditSubmitTargetInfoEntry target)
    {
        return target.Url.Contains("connector_openai_deep_research.web-sandbox.oaiusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<AuditSubmitDeepResearchFrame?> TryCreateDeepResearchFrameContextAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var rectValue = await browser.EvaluateAsync(tabId, DeepResearchIframeRectExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        if (!TryReadDomRect(rectValue, out var rect))
        {
            return null;
        }

        var frameTree = await browser.ExecuteCdpAsync(tabId, "Page.getFrameTree", EmptyObject(), TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        var frameId = SelectSingleDeepResearchFrameId(ReadFrameTreeEntries(frameTree));
        if (string.IsNullOrWhiteSpace(frameId))
        {
            return null;
        }

        var world = await browser.ExecuteCdpAsync(
            tabId,
            "Page.createIsolatedWorld",
            new Dictionary<string, object?>
            {
                ["frameId"] = frameId,
                ["worldName"] = "Electron2DAuditSubmitDeepResearch"
            },
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
        if (!world.TryGetProperty("executionContextId", out var contextElement) ||
            contextElement.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return new AuditSubmitDeepResearchFrame(contextElement.GetInt32(), rect);
    }

    private static string? SelectSingleDeepResearchFrameId(IReadOnlyList<AuditSubmitFrameTreeEntry> entries)
    {
        var frameIds = entries
            .Where(IsDeepResearchFrame)
            .Select(static entry => entry.FrameId)
            .Where(static frameId => !string.IsNullOrWhiteSpace(frameId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (frameIds.Length == 0)
        {
            return null;
        }

        if (frameIds.Length > 1)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-AMBIGUOUS",
                "The page exposed multiple Deep Research iframe frame ids.");
        }

        return frameIds[0];
    }

    private static bool IsDeepResearchFrame(AuditSubmitFrameTreeEntry entry)
    {
        return entry.Url.Contains("connector_openai_deep_research.web-sandbox.oaiusercontent.com", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> WaitForMarkdownDownloadAsync(
        string downloadsDirectory,
        HashSet<string> knownFiles,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var directories = GetDownloadSearchDirectories(downloadsDirectory, includeUserDownloadsFallback: false);
        return await WaitForMarkdownDownloadAsync(
            directories,
            directories,
            knownFiles,
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> WaitForMarkdownDownloadAsync(
        IReadOnlyList<string> observedDownloadDirectories,
        IReadOnlyList<string> acceptedDownloadDirectories,
        HashSet<string> knownFiles,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidates = EnumerateDownloadFiles(observedDownloadDirectories, "*.md")
                .Where(path => !knownFiles.Contains(path) && !HasActiveChromeDownload(path))
                .Select(static path => new FileInfo(path))
                .Where(file => file.Exists && file.Length > 0)
                .OrderByDescending(static file => file.LastWriteTimeUtc)
                .ToArray();
            var stableDownloads = new List<string>();
            foreach (var candidate in candidates)
            {
                if (await IsFileStableAsync(candidate.FullName, cancellationToken).ConfigureAwait(false))
                {
                    stableDownloads.Add(candidate.FullName);
                }
            }

            var selected = SelectSingleMarkdownDownloadOrThrow(stableDownloads, acceptedDownloadDirectories);
            if (!string.IsNullOrWhiteSpace(selected))
            {
                return selected;
            }

            if (stableDownloads.Count > 0)
            {
                throw new AuditSubmitCodexChromeException(
                    "E2D-BUILD-AUDIT-SUBMIT-REPORT-DOWNLOAD-FOREIGN",
                    "The export action created a Markdown file outside the accepted download directory.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken).ConfigureAwait(false);
        }

        return string.Empty;
    }

    private static string SelectSingleMarkdownDownloadOrThrow(IReadOnlyList<string> stableDownloads, IReadOnlyList<string> acceptedDirectories)
    {
        var downloads = stableDownloads
            .Where(static path => !string.IsNullOrWhiteSpace(path))
            .Where(path => IsPathInAcceptedDownloadDirectory(path, acceptedDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (downloads.Length == 0)
        {
            return string.Empty;
        }

        if (downloads.Length > 1)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-DOWNLOAD-AMBIGUOUS",
                "The export action produced multiple new Markdown downloads.");
        }

        return downloads[0];
    }

    private static bool IsPathInAcceptedDownloadDirectory(string path, IReadOnlyList<string> acceptedDirectories)
    {
        if (acceptedDirectories.Count == 0)
        {
            return false;
        }

        var fullPath = Path.GetFullPath(path);
        foreach (var directory in acceptedDirectories)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                continue;
            }

            var fullDirectory = Path.GetFullPath(directory);
            if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar) &&
                !fullDirectory.EndsWith(Path.AltDirectorySeparatorChar))
            {
                fullDirectory += Path.DirectorySeparatorChar;
            }

            if (fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static IReadOnlyList<string> GetDownloadSearchDirectories(string downloadsDirectory, bool includeUserDownloadsFallback)
    {
        List<string> directories = [downloadsDirectory];
        if (includeUserDownloadsFallback)
        {
            AddDownloadSearchDirectory(directories, GetKnownDownloadsDirectory());
            AddDownloadSearchDirectory(directories, GetUserProfileDownloadsDirectory());
        }

        return directories;
    }

    private static IReadOnlyList<string> GetObservedDownloadSearchDirectories(string downloadsDirectory)
    {
        List<string> directories = [downloadsDirectory];
        AddDownloadSearchDirectory(directories, GetKnownDownloadsDirectory());
        AddDownloadSearchDirectory(directories, GetUserProfileDownloadsDirectory());
        return directories;
    }

    private static void AddDownloadSearchDirectory(List<string> directories, string directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) &&
            !directories.Contains(directory, StringComparer.OrdinalIgnoreCase))
        {
            directories.Add(directory);
        }
    }

    private static string GetKnownDownloadsDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            return string.Empty;
        }

        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(UserShellFoldersRegistryPath);
            var value = key?.GetValue(KnownFolderDownloadsRegistryValueName, string.Empty, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames);
            var path = value as string;
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : Environment.ExpandEnvironmentVariables(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return string.Empty;
        }
    }

    private static string GetUserProfileDownloadsDirectory()
    {
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(profile)
            ? string.Empty
            : Path.Combine(profile, "Downloads");
    }

    private const string UserShellFoldersRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";
    private const string KnownFolderDownloadsRegistryValueName = "{374DE290-123F-4565-9164-39C4925E467B}";

    private static HashSet<string> SnapshotDownloadFiles(IReadOnlyList<string> downloadDirectories)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in EnumerateDownloadFiles(downloadDirectories, "*"))
        {
            files.Add(path);
        }

        return files;
    }

    private static IEnumerable<string> EnumerateDownloadFiles(IReadOnlyList<string> downloadDirectories, string searchPattern)
    {
        foreach (var directory in downloadDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                continue;
            }

            IEnumerable<string> paths;
            try
            {
                paths = Directory.EnumerateFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var path in paths)
            {
                yield return path;
            }
        }
    }

    private static async Task<bool> IsFileStableAsync(string path, CancellationToken cancellationToken)
    {
        var first = new FileInfo(path);
        if (!first.Exists || first.Length <= 0)
        {
            return false;
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        var second = new FileInfo(path);
        return second.Exists &&
            first.Length == second.Length &&
            first.LastWriteTimeUtc == second.LastWriteTimeUtc &&
            !HasActiveChromeDownload(path);
    }

    private static bool HasActiveChromeDownload(string path)
    {
        return File.Exists(path + ".crdownload") ||
            File.Exists(Path.ChangeExtension(path, Path.GetExtension(path) + ".crdownload"));
    }

    private static async Task<bool> DismissRateLimitDialogAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var point = await browser.EvaluatePointAsync(tabId, RateLimitDialogDismissPointExpression, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        if (point is null)
        {
            return false;
        }

        await browser.ClickAtAsync(tabId, point.Value, cancellationToken).ConfigureAwait(false);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static bool TryReadDomRect(JsonElement value, out AuditSubmitDomRect rect)
    {
        rect = default;
        if (value.ValueKind != JsonValueKind.Object ||
            !TryReadFiniteDouble(value, "x", out var x) ||
            !TryReadFiniteDouble(value, "y", out var y) ||
            !TryReadFiniteDouble(value, "width", out var width) ||
            !TryReadFiniteDouble(value, "height", out var height) ||
            width <= 0 ||
            height <= 0)
        {
            return false;
        }

        rect = new AuditSubmitDomRect(x, y, width, height);
        return true;
    }

    private static bool TryReadFiniteDouble(JsonElement value, string propertyName, out double number)
    {
        number = 0;
        return value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            double.IsFinite(number = property.GetDouble());
    }

    private static bool TryReadNonNegativeInt32(JsonElement value, out int number)
    {
        number = 0;
        return value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out number) &&
            number >= 0;
    }

    private static Dictionary<string, object?> EmptyObject()
    {
        return [];
    }

    private const string DomDumpExpression =
        """
        (() => {
          const normalize = (value) => (value || '').replace(/\s+/g, ' ').trim();
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0 &&
              rect.bottom >= 0 &&
              rect.right >= 0 &&
              rect.top <= window.innerHeight &&
              rect.left <= window.innerWidth;
          };
          const rectInfo = (element) => {
            const rect = element.getBoundingClientRect();
            return {
              x: rect.left,
              y: rect.top,
              width: rect.width,
              height: rect.height
            };
          };
          const elementText = (element) => normalize([
            element.getAttribute('aria-label') || '',
            element.getAttribute('title') || '',
            element.getAttribute('download') || '',
            element.innerText || element.textContent || ''
          ].join('\n')).slice(0, 500);
          const buttons = Array
            .from(document.querySelectorAll('button,[role="button"],[aria-label],[aria-haspopup="menu"]'))
            .filter(visible)
            .slice(0, 1000)
            .map((element) => ({
              tag: element.tagName,
              type: element.getAttribute('type') || '',
              role: element.getAttribute('role') || '',
              ariaLabel: element.getAttribute('aria-label') || '',
              ariaHasPopup: element.getAttribute('aria-haspopup') || '',
              ariaExpanded: element.getAttribute('aria-expanded') || '',
              text: elementText(element),
              rect: rectInfo(element)
            }));
          const links = Array
            .from(document.querySelectorAll('a[href],a[download]'))
            .slice(0, 1000)
            .map((element) => ({
              href: element.href || '',
              download: element.getAttribute('download') || '',
              text: elementText(element),
              visible: visible(element),
              rect: rectInfo(element)
            }));
          const iframes = Array
            .from(document.querySelectorAll('iframe'))
            .map((element) => ({
              title: element.getAttribute('title') || '',
              src: element.src || element.getAttribute('src') || '',
              visible: visible(element),
              rect: rectInfo(element)
            }));
          return {
            url: location.href,
            title: document.title,
            readyState: document.readyState,
            html: document.documentElement ? document.documentElement.outerHTML : '',
            text: document.body ? document.body.innerText || '' : '',
            buttons,
            links,
            iframes
          };
        })()
        """;

    private const string ScrollConversationToBottomExpression =
        """
        (() => {
          const candidates = [
            document.scrollingElement,
            document.documentElement,
            document.body,
            ...document.querySelectorAll('main,[role="main"],div,section,article')
          ]
            .filter((element) => element)
            .filter((element, index, array) => array.indexOf(element) === index);
          let moved = false;
          for (const element of candidates) {
            const max = Math.max(0, element.scrollHeight - element.clientHeight);
            if (max > 0) {
              element.scrollTop = max;
              moved = true;
            }
          }

          window.scrollTo(0, Math.max(document.documentElement.scrollHeight, document.body.scrollHeight));
          return moved || true;
        })()
        """;

    private const string DeepResearchIframeRectExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0 &&
              rect.bottom >= 0 &&
              rect.right >= 0 &&
              rect.top <= window.innerHeight &&
              rect.left <= window.innerWidth;
          };
          const frames = Array
            .from(document.querySelectorAll('iframe[title="internal://deep-research"],iframe[src*="connector_openai_deep_research"]'))
            .filter(visible);
          if (frames.length !== 1) {
            return false;
          }

          const frame = frames[0];
          const rect = frame.getBoundingClientRect();
          return {
            x: rect.left,
            y: rect.top,
            width: rect.width,
            height: rect.height
          };
        })()
        """;

    private const string DeepResearchIframeVisibleExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0 &&
              rect.bottom >= 0 &&
              rect.right >= 0 &&
              rect.top <= window.innerHeight &&
              rect.left <= window.innerWidth;
          };
          return Array
            .from(document.querySelectorAll('iframe[title="internal://deep-research"],iframe[src*="connector_openai_deep_research"]'))
            .some(visible);
        })()
        """;

    private const string DeepResearchReportTargetReadyExpression =
        """
        (() => {
          const documents = () => {
            const result = [];
            const visit = (doc) => {
              if (!doc || result.includes(doc)) return;
              result.push(doc);
              for (const frame of Array.from(doc.querySelectorAll('iframe'))) {
                try {
                  if (frame.contentDocument) visit(frame.contentDocument);
                } catch {
                }
              }
            };
            visit(document);
            return result;
          };
          const rendered = (element) => {
            const view = element.ownerDocument.defaultView || window;
            const style = view.getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0;
          };
          const normalize = (value) => (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const text = documents()
            .map((doc) => normalize(doc.body ? doc.body.innerText || doc.body.textContent || '' : ''))
            .join('\n');
          const hasReportLabel =
            text.includes('углубленный исследовательский отчет') ||
            text.includes('углублённый исследовательский отчёт') ||
            text.includes('deep research report');
          const hasCompletedResearch =
            text.includes('исследование завершено') ||
            text.includes('research completed');
          const hasMeaningfulReportText =
            text.length >= 1000 &&
            (
              text.includes('verdict') ||
              text.includes('вердикт') ||
              text.includes('audit') ||
              text.includes('аудит')
            );
          const hasDownloadIcon = (element) => Array
            .from(element.querySelectorAll('path'))
            .map((path) => path.getAttribute('d') || '')
            .some((path) =>
              path.includes('M2.66821 12.6663') &&
              path.includes('9.33521 3.33333'));
          const rootButtons = document.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]');
          const labels = ['экспорт', 'export', 'скач', 'download'];
          const hasExportButton = documents()
            .flatMap((doc) => Array.from(doc === document ? rootButtons : doc.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]')))
            .some((button) => {
              const label = normalize(`${button.getAttribute('aria-label') || ''}\n${button.getAttribute('title') || ''}`);
              const opensMenu = normalize(button.getAttribute('aria-haspopup') || '') === 'menu';
              return rendered(button) &&
                labels.some((candidate) => label === candidate || label.includes(candidate)) &&
                (hasDownloadIcon(button) || opensMenu);
            });
          return (hasReportLabel || hasCompletedResearch || hasMeaningfulReportText) && hasExportButton;
        })()
        """;

    private const string ReportExportButtonClickExpression =
        """
        (() => {
          const documents = () => {
            const result = [];
            const visit = (doc) => {
              if (!doc || result.includes(doc)) return;
              result.push(doc);
              for (const frame of Array.from(doc.querySelectorAll('iframe'))) {
                try {
                  if (frame.contentDocument) visit(frame.contentDocument);
                } catch {
                }
              }
            };
            visit(document);
            return result;
          };
          const rendered = (element) => {
            const view = element.ownerDocument.defaultView || window;
            const style = view.getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0;
          };
          const normalize = (value) => (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const hasDownloadIcon = (element) => Array
            .from(element.querySelectorAll('path'))
            .map((path) => path.getAttribute('d') || '')
            .some((path) =>
              path.includes('M2.66821 12.6663') &&
              path.includes('9.33521 3.33333'));
          const activate = (element) => {
            const fire = (type) => {
              const eventOptions = { bubbles: true, cancelable: true, button: 0, buttons: type.endsWith('down') ? 1 : 0, pointerType: 'mouse', isPrimary: true };
              const EventCtor = type.startsWith('pointer') && typeof PointerEvent === 'function'
                ? PointerEvent
                : (typeof MouseEvent === 'function' ? MouseEvent : Event);
              element.dispatchEvent(new EventCtor(type, eventOptions));
            };
            fire('pointerdown');
            fire('mousedown');
            fire('pointerup');
            fire('mouseup');
            element.click();
          };
          const labels = ['экспорт', 'export', 'скач', 'download'];
          const rootButtons = document.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]');
          const candidates = documents()
            .flatMap((doc) => Array.from(doc === document ? rootButtons : doc.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]')))
            .filter((button) => {
              const label = normalize(`${button.getAttribute('aria-label') || ''}\n${button.getAttribute('title') || ''}`);
              const opensMenu = normalize(button.getAttribute('aria-haspopup') || '') === 'menu';
              return rendered(button) &&
                labels.some((candidate) => label === candidate || label.includes(candidate)) &&
                (hasDownloadIcon(button) || opensMenu);
            })
            .sort((left, right) => {
              const expanded = (right.getAttribute('aria-expanded') === 'true' ? 1 : 0) -
                (left.getAttribute('aria-expanded') === 'true' ? 1 : 0);
              if (expanded !== 0) return expanded;
              const top = left.getBoundingClientRect().top - right.getBoundingClientRect().top;
              if (Math.abs(top) > 4) return top;
              return right.getBoundingClientRect().left - left.getBoundingClientRect().left;
            });
          if (candidates.length !== 1) {
            return false;
          }

          const button = candidates[0];
          button.scrollIntoView({ block: 'center', inline: 'center' });
          button.focus();
          activate(button);
          return true;
        })()
        """;

    private const string ExportReportMarkdownMenuItemClickExpression =
        """
        (() => {
          const documents = () => {
            const result = [];
            const visit = (doc) => {
              if (!doc || result.includes(doc)) return;
              result.push(doc);
              for (const frame of Array.from(doc.querySelectorAll('iframe'))) {
                try {
                  if (frame.contentDocument) visit(frame.contentDocument);
                } catch {
                }
              }
            };
            visit(document);
            return result;
          };
          const visible = (element) => {
            const view = element.ownerDocument.defaultView || window;
            const style = view.getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0 &&
              rect.bottom >= 0 &&
              rect.right >= 0 &&
              rect.top <= view.innerHeight &&
              rect.left <= view.innerWidth;
          };
          const normalize = (value) => (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const labels = [
            'экспортировать в markdown',
            'export as markdown',
            'export to markdown',
            'скачать markdown',
            'скачать в markdown',
            'скачать как markdown',
            'download markdown',
            'download as markdown'
          ];
          const activate = (element) => {
            const fire = (type) => {
              const eventOptions = { bubbles: true, cancelable: true, button: 0, buttons: type.endsWith('down') ? 1 : 0, pointerType: 'mouse', isPrimary: true };
              const EventCtor = type.startsWith('pointer') && typeof PointerEvent === 'function'
                ? PointerEvent
                : (typeof MouseEvent === 'function' ? MouseEvent : Event);
              element.dispatchEvent(new EventCtor(type, eventOptions));
            };
            fire('pointerdown');
            fire('mousedown');
            fire('pointerup');
            fire('mouseup');
            element.click();
          };
          const itemSelector = 'button,[role="menuitem"],[role="menuitemradio"],.__menu-item,[data-fill][tabindex]';
          const rootItems = document.querySelectorAll(itemSelector);
          const items = documents()
            .flatMap((doc) => Array.from(doc === document ? rootItems : doc.querySelectorAll(itemSelector)))
            .filter((item) => {
              const text = normalize(`${item.getAttribute('aria-label') || ''}\n${item.getAttribute('title') || ''}\n${item.innerText || item.textContent || ''}`);
              const isMarkdownDownload = text.includes('markdown') &&
                (text.includes('экспорт') ||
                  text.includes('export') ||
                  text.includes('скач') ||
                  text.includes('download'));
              return visible(item) &&
                !item.disabled &&
                item.getAttribute('aria-disabled') !== 'true' &&
                (labels.some((label) => text === label || text.includes(label)) || isMarkdownDownload);
            })
            .sort((left, right) => {
              const top = left.getBoundingClientRect().top - right.getBoundingClientRect().top;
              if (Math.abs(top) > 4) return top;
              return left.getBoundingClientRect().left - right.getBoundingClientRect().left;
            });
          if (items.length !== 1) {
            return false;
          }

          const item = items[0];
          if (typeof item.focus === 'function') item.focus();
          activate(item);
          return true;
        })()
        """;

    private const string HasPromptExpression =
        """
        (() => {
          const selectors = [
            '#prompt-textarea',
            '[data-testid="composer-text-input"]',
            'textarea',
            '[contenteditable="true"][role="textbox"]',
            '[contenteditable="true"]'
          ];
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          return selectors.some((selector) => Array.from(document.querySelectorAll(selector)).some(visible));
        })()
        """;

    private const string DeepResearchSelectedExpression =
        """
        (() => {
          const normalize = (text) => (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const prompt = Array
            .from(document.querySelectorAll('#prompt-textarea,[data-testid="composer-text-input"],textarea,[contenteditable="true"][role="textbox"],[contenteditable="true"]'))
            .find(visible);
          if (!prompt) return false;
          const connectorId = 'connector:connector_openai_deep_research';
          const connectorMetadataSelectors = [
            '[data-id="connector:connector_openai_deep_research"]',
            '[data-system-hint-type="connector:connector_openai_deep_research"]',
            '[data-keyword="Глубокое исследование"]',
            '[data-keyword="Deep Research"]'
          ].join(',');
          const inlineConnectorSelectors = [
            '[data-id="connector:connector_openai_deep_research"][data-inline-selection-pill]',
            '[data-system-hint-type="connector:connector_openai_deep_research"][data-inline-selection-pill]',
            '[data-keyword="Глубокое исследование"][data-inline-selection-pill]',
            '[data-keyword="Deep Research"][data-inline-selection-pill]'
          ].join(',');
          const promptRect = prompt.getBoundingClientRect();
          const nearPrompt = (element) => {
            const rect = element.getBoundingClientRect();
            const horizontallyNearPrompt = rect.right >= promptRect.left - 120 &&
              rect.left <= promptRect.right + 120;
            const verticallyNearPrompt = rect.bottom >= promptRect.top - 240 &&
              rect.top <= promptRect.bottom + 240;
            return horizontallyNearPrompt && verticallyNearPrompt;
          };
          const isHistoryOrMessage = (element) =>
            element.closest('[data-message-author-role]') !== null ||
            element.closest('[data-testid^="project-conversation"]') !== null ||
            element.closest('a[href]') !== null;
          const isMenuOrPlainButton = (element) => {
            const role = element.getAttribute('role') || '';
            return role === 'menuitem' ||
              role === 'option' ||
              role === 'button' ||
              element.tagName === 'BUTTON' ||
              element.closest('button,[role="button"],[role="menuitem"],[role="option"],[role="menu"],[role="listbox"],[data-radix-popper-content-wrapper],.__menu-item,[data-fill][tabindex]') !== null;
          };
          const isCompactDeepResearchPill = (element) => {
            const rect = element.getBoundingClientRect();
            const text = normalize(element.innerText || element.textContent || '');
            return rect.width > 0 &&
              rect.width <= 520 &&
              rect.height > 0 &&
              rect.height <= 96 &&
              (text === 'глубокое исследование' || text === 'deep research');
          };
          const hasConnectorMetadata = (element) => {
            const isSelectionPill = element.getAttribute('data-inline-selection-pill') !== null;
            const directConnector = element.getAttribute('data-id') === connectorId ||
              element.getAttribute('data-system-hint-type') === connectorId ||
              element.getAttribute('data-keyword') === 'Глубокое исследование' ||
              element.getAttribute('data-keyword') === 'Deep Research';
            const nestedConnector = typeof element.querySelectorAll === 'function' &&
              Array.from(element.querySelectorAll(connectorMetadataSelectors)).some(visible);
            if (isMenuOrPlainButton(element)) return false;
            if (isSelectionPill && (directConnector || nestedConnector)) return true;
            if (directConnector) return true;
            return nestedConnector && isCompactDeepResearchPill(element);
          };
          return Array
            .from(document.querySelectorAll(`button,[role="button"],div,span,${connectorMetadataSelectors},${inlineConnectorSelectors}`))
            .filter((element) => element !== prompt && visible(element) && nearPrompt(element) && !isHistoryOrMessage(element))
            .some(hasConnectorMetadata);
        })()
        """;

    private const string DeepResearchSelectedDiagnosticsExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const textOf = (element) => ((element.innerText || element.textContent || '').replace(/\s+/g, ' ').trim()).slice(0, 240);
          const attr = (element, name) => element.getAttribute(name);
          const rectOf = (element) => {
            const rect = element.getBoundingClientRect();
            return {
              left: Math.round(rect.left),
              top: Math.round(rect.top),
              right: Math.round(rect.right),
              bottom: Math.round(rect.bottom),
              width: Math.round(rect.width),
              height: Math.round(rect.height)
            };
          };
          const prompts = Array
            .from(document.querySelectorAll('#prompt-textarea,[data-testid="composer-text-input"],textarea,[contenteditable="true"][role="textbox"],[contenteditable="true"]'))
            .filter(visible);
          const prompt = prompts[0] || null;
          const connectorId = 'connector:connector_openai_deep_research';
          const connectorMetadataSelectors = [
            '[data-id="connector:connector_openai_deep_research"]',
            '[data-system-hint-type="connector:connector_openai_deep_research"]',
            '[data-keyword="Глубокое исследование"]',
            '[data-keyword="Deep Research"]'
          ].join(',');
          const inlineConnectorSelectors = [
            '[data-id="connector:connector_openai_deep_research"][data-inline-selection-pill]',
            '[data-system-hint-type="connector:connector_openai_deep_research"][data-inline-selection-pill]',
            '[data-keyword="Глубокое исследование"][data-inline-selection-pill]',
            '[data-keyword="Deep Research"][data-inline-selection-pill]'
          ].join(',');
          const promptRect = prompt ? prompt.getBoundingClientRect() : null;
          const nearPrompt = (element) => {
            if (!promptRect) return false;
            const rect = element.getBoundingClientRect();
            const horizontallyNearPrompt = rect.right >= promptRect.left - 120 &&
              rect.left <= promptRect.right + 120;
            const verticallyNearPrompt = rect.bottom >= promptRect.top - 240 &&
              rect.top <= promptRect.bottom + 240;
            return horizontallyNearPrompt && verticallyNearPrompt;
          };
          const isHistoryOrMessage = (element) =>
            element.closest('[data-message-author-role]') !== null ||
            element.closest('[data-testid^="project-conversation"]') !== null ||
            element.closest('a[href]') !== null;
          const isMenuOrPlainButton = (element) => {
            const role = element.getAttribute('role') || '';
            return role === 'menuitem' ||
              role === 'option' ||
              role === 'button' ||
              element.tagName === 'BUTTON' ||
              element.closest('button,[role="button"],[role="menuitem"],[role="option"],[role="menu"],[role="listbox"],[data-radix-popper-content-wrapper],.__menu-item,[data-fill][tabindex]') !== null;
          };
          const isCompactDeepResearchPill = (element) => {
            const rect = element.getBoundingClientRect();
            const text = textOf(element).toLowerCase();
            return rect.width > 0 &&
              rect.width <= 520 &&
              rect.height > 0 &&
              rect.height <= 96 &&
              (text === 'глубокое исследование' || text === 'deep research');
          };
          const hasDirectConnector = (element) =>
            element.getAttribute('data-id') === connectorId ||
            element.getAttribute('data-system-hint-type') === connectorId ||
            element.getAttribute('data-keyword') === 'Глубокое исследование' ||
            element.getAttribute('data-keyword') === 'Deep Research';
          const nestedConnectors = (element) => typeof element.querySelectorAll === 'function'
            ? Array.from(element.querySelectorAll(connectorMetadataSelectors)).filter(visible)
            : [];
          const acceptedByCurrentRule = (element) => {
            const isSelectionPill = element.getAttribute('data-inline-selection-pill') !== null;
            const directConnector = hasDirectConnector(element);
            const nestedConnector = nestedConnectors(element).length > 0;
            if (isMenuOrPlainButton(element)) return false;
            if (isSelectionPill && (directConnector || nestedConnector)) return true;
            if (directConnector) return true;
            return nestedConnector && isCompactDeepResearchPill(element);
          };
          const candidates = Array
            .from(document.querySelectorAll(`button,[role="button"],div,span,${connectorMetadataSelectors},${inlineConnectorSelectors}`))
            .filter((element) =>
              element === prompt ||
              hasDirectConnector(element) ||
              nestedConnectors(element).length > 0 ||
              /глубокое исследование|deep research|отслеживание/i.test(textOf(element)) ||
              nearPrompt(element))
            .slice(0, 120)
            .map((element, index) => ({
              index,
              tagName: element.tagName,
              role: attr(element, 'role'),
              dataId: attr(element, 'data-id'),
              dataSystemHintType: attr(element, 'data-system-hint-type'),
              dataKeyword: attr(element, 'data-keyword'),
              dataInlineSelectionPill: attr(element, 'data-inline-selection-pill'),
              dataTestId: attr(element, 'data-testid'),
              ariaLabel: attr(element, 'aria-label'),
              text: textOf(element),
              visible: visible(element),
              rect: rectOf(element),
              isPrompt: element === prompt,
              nearPrompt: nearPrompt(element),
              isHistoryOrMessage: isHistoryOrMessage(element),
              isMenuOrPlainButton: isMenuOrPlainButton(element),
              isCompactDeepResearchPill: isCompactDeepResearchPill(element),
              directConnector: hasDirectConnector(element),
              nestedConnectorCount: nestedConnectors(element).length,
              acceptedByCurrentRule: element !== prompt && visible(element) && nearPrompt(element) && !isHistoryOrMessage(element) && acceptedByCurrentRule(element)
            }));
          return {
            url: location.href,
            title: document.title,
            readyState: document.readyState,
            promptCount: prompts.length,
            prompts: prompts.slice(0, 10).map((element, index) => ({
              index,
              tagName: element.tagName,
              role: attr(element, 'role'),
              dataTestId: attr(element, 'data-testid'),
              text: textOf(element),
              rect: rectOf(element)
            })),
            acceptedCandidateCount: candidates.filter((entry) => entry.acceptedByCurrentRule).length,
            candidates
          };
        })()
        """;

    private const string DeepResearchMenuPointExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const button = Array
            .from(document.querySelectorAll('button[data-testid="composer-plus-btn"]'))
            .find((element) => visible(element) && !element.disabled && element.getAttribute('aria-disabled') !== 'true');
          if (!button) return false;
          button.scrollIntoView({ block: 'center', inline: 'center' });
          const rect = button.getBoundingClientRect();
          return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
        })()
        """;

    private const string DeepResearchComposerMenuOpenExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const plus = Array
            .from(document.querySelectorAll('button[data-testid="composer-plus-btn"]'))
            .find((element) => visible(element) && !element.disabled && element.getAttribute('aria-disabled') !== 'true');
          if (!plus) return false;
          const plusRect = plus.getBoundingClientRect();
          const inComposerMenu = (element) => {
            const rect = element.getBoundingClientRect();
            const horizontallyNearPlus = rect.x >= plusRect.x - 160 &&
              rect.x <= plusRect.x + 980;
            const belowComposer = rect.y >= plusRect.bottom - 20 &&
              rect.y <= plusRect.bottom + 560;
            const aboveComposer = rect.bottom >= plusRect.top - 560 &&
              rect.bottom <= plusRect.top + 20;
            return horizontallyNearPlus && (belowComposer || aboveComposer);
          };
          const isHistoryOrMessage = (element) =>
            element.closest('[data-message-author-role]') !== null ||
            element.closest('[data-testid^="project-conversation"]') !== null ||
            element.closest('a[href]') !== null;
          const menuLikeSelector = [
            '[role="menu"]',
            '[role="listbox"]',
            '[data-radix-popper-content-wrapper]',
            '.__menu-item',
            '[data-fill][tabindex]'
          ].join(',');
          const hasMenuRow = (element) =>
            element.matches('.__menu-item,[data-fill][tabindex],[role="menuitem"],[role="option"],[role="button"]') ||
            (typeof element.querySelector === 'function' &&
              element.querySelector('.__menu-item,[data-fill][tabindex],[role="menuitem"],[role="option"],[role="button"]') !== null);
          return Array
            .from(document.querySelectorAll(menuLikeSelector))
            .filter((element) => visible(element) && inComposerMenu(element) && !isHistoryOrMessage(element))
            .some(hasMenuRow);
        })()
        """;

    private const string DeepResearchComposerMenuExpandedExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const plus = Array
            .from(document.querySelectorAll('button[data-testid="composer-plus-btn"]'))
            .find((element) => visible(element) && !element.disabled && element.getAttribute('aria-disabled') !== 'true');
          return plus?.getAttribute('aria-expanded') === 'true';
        })()
        """;

    private const string DeepResearchItemPointExpression =
        """
        (() => {
          const normalize = (text) => (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const plus = Array
            .from(document.querySelectorAll('button[data-testid="composer-plus-btn"]'))
            .find((element) => visible(element) && !element.disabled && element.getAttribute('aria-disabled') !== 'true');
          const plusRect = plus?.getBoundingClientRect();
          const labels = ['глубокое исследование', 'deep research'];
          const connectorId = 'connector:connector_openai_deep_research';
          const interactiveSelector = 'button,[role="menuitem"],[role="option"],[role="button"],.__menu-item,[data-fill][tabindex]';
          const connectorSelectors = [
            '[data-id="connector:connector_openai_deep_research"]',
            '[data-system-hint-type="connector:connector_openai_deep_research"]',
            '[data-keyword="Глубокое исследование"]',
            '[data-keyword="Deep Research"]'
          ].join(',');
          const inComposerMenu = (element) => {
            if (!plusRect) return true;
            const rect = element.getBoundingClientRect();
            const horizontallyNearPlus = rect.x >= plusRect.x - 80 &&
              rect.x <= plusRect.x + 900;
            const belowComposer = rect.y >= plusRect.bottom - 12 &&
              rect.y <= plusRect.bottom + 520;
            const aboveComposer = rect.bottom >= plusRect.top - 520 &&
              rect.bottom <= plusRect.top + 12;
            return horizontallyNearPlus && (belowComposer || aboveComposer);
          };
          const isHistoryOrMessage = (element) =>
            element.closest('[data-message-author-role]') !== null ||
            element.closest('[data-testid^="project-conversation"]') !== null ||
            element.closest('a[href]') !== null;
          const hasConnectorMetadata = (element) => {
            const keyword = normalize(element.getAttribute('data-keyword') || '');
            return element.getAttribute('data-id') === connectorId ||
              element.getAttribute('data-system-hint-type') === connectorId ||
              labels.includes(keyword) ||
              (typeof element.querySelector === 'function' && element.querySelector(connectorSelectors) !== null);
          };
          const isInteractive = (element) => {
            const role = element.getAttribute('role') || '';
            return element.tagName === 'BUTTON' ||
              role === 'menuitem' ||
              role === 'option' ||
              role === 'button' ||
              element.closest('.__menu-item,[data-fill][tabindex]') === element;
          };
          const candidates = Array
            .from(document.querySelectorAll(`button,[role="menuitem"],[role="option"],[role="button"],.__menu-item,[data-fill][tabindex],div,span,${connectorSelectors}`))
            .filter((element) => visible(element) && inComposerMenu(element) && !isHistoryOrMessage(element));
          const matches = candidates.filter((element) => {
            if (hasConnectorMetadata(element)) return true;
            const text = normalize(`${element.getAttribute('aria-label') || ''}\n${element.innerText || element.textContent || ''}`);
            return labels.some((label) => text === label || text.startsWith(`${label} `));
          });
          const target = matches
            .map((element) => element.closest(interactiveSelector) || element)
            .filter((element, index, elements) => elements.indexOf(element) === index)
            .map((element) => ({ element, rect: element.getBoundingClientRect(), interactive: isInteractive(element) }))
            .filter((entry) => entry.interactive || entry.rect.width >= 250)
            .sort((left, right) =>
              Number(right.interactive) - Number(left.interactive) ||
              (right.rect.width * right.rect.height) - (left.rect.width * left.rect.height))
            .map((entry) => entry.element)[0] || matches[0];
          if (!target) return false;
          const clickable = target.closest(interactiveSelector) || target;
          clickable.scrollIntoView({ block: 'nearest', inline: 'nearest' });
          const rect = clickable.getBoundingClientRect();
          return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
        })()
        """;

    private const string AttachmentClickExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const needles = ['attach', 'upload', 'прикреп', 'загруз'];
          const candidates = Array.from(document.querySelectorAll('button,label,[role="button"]'));
          const match = candidates.find((element) => {
            const text = `${element.getAttribute('aria-label') || ''}\n${element.innerText || element.textContent || ''}`.toLowerCase();
            return visible(element) && needles.some((needle) => text.includes(needle));
          });
          if (!match) return false;
          match.click();
          return true;
        })()
        """;

    private static string FillPromptExpression(string messageJson)
    {
        return $$"""
        (() => {
          const message = {{messageJson}};
          const selectors = [
            '#prompt-textarea',
            '[data-testid="composer-text-input"]',
            'textarea',
            '[contenteditable="true"][role="textbox"]',
            '[contenteditable="true"]'
          ];
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const element = selectors.flatMap((selector) => Array.from(document.querySelectorAll(selector))).find(visible);
          if (!element) return false;
          element.focus();
          const deepResearchPill = element.querySelector('[data-id="connector:connector_openai_deep_research"],[data-system-hint-type="connector:connector_openai_deep_research"],[data-keyword="Глубокое исследование"],[data-keyword="Deep Research"]');
          if (deepResearchPill && element.isContentEditable) {
            const selection = window.getSelection();
            if (!selection) return false;
            const range = document.createRange();
            range.selectNodeContents(element);
            range.collapse(false);
            selection.removeAllRanges();
            selection.addRange(range);
            document.execCommand('insertText', false, message);
            element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: message }));
            return true;
          }
          if (element instanceof HTMLTextAreaElement || element instanceof HTMLInputElement) {
            const setter = Object.getOwnPropertyDescriptor(Object.getPrototypeOf(element), 'value')?.set;
            setter ? setter.call(element, message) : element.value = message;
            element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: message }));
            element.dispatchEvent(new Event('change', { bubbles: true }));
            return true;
          }
          element.textContent = '';
          document.execCommand('insertText', false, message);
          element.dispatchEvent(new InputEvent('input', { bubbles: true, inputType: 'insertText', data: message }));
          return true;
        })()
        """;
    }

    private static string PromptPayloadReadyExpression(string messageJson, string fileNamesJson)
    {
        return $$"""
        (() => {
          const status = {{PromptPayloadStatusExpression(messageJson, fileNamesJson)}};
          return status && status.ready === true;
        })()
        """;
    }

    private static string PromptPayloadStatusExpression(string messageJson, string fileNamesJson)
    {
        return $$"""
        (() => {
          const message = {{messageJson}};
          const fileNames = {{fileNamesJson}};
          const normalize = (value) => String(value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const withoutDeepResearchPrefix = (value) => normalize(value)
            .replace(/^@?\s*(глубокое исследование|deep research)\s*/, '')
            .trim();
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const prompts = Array.from(document.querySelectorAll([
            '#prompt-textarea',
            '[data-testid="composer-text-input"]',
            'textarea',
            '[contenteditable="true"][role="textbox"]',
            '[contenteditable="true"]'
          ].join(','))).filter(visible);
          const prompt = prompts[0] || null;
          if (!prompt) {
            return {
              ready: false,
              reason: 'prompt-missing',
              promptFound: false,
              promptHasExpectedMessage: false,
              promptIsEmpty: false,
              expectedFileCount: Array.isArray(fileNames) ? fileNames.length : -1,
              filenameMatchCount: 0,
              attachmentRootCount: 0
            };
          }

          const expectedMessage = normalize(message);
          const expectedMessageWithoutToolPrefix = withoutDeepResearchPrefix(message);
          const expectedMessageVariants = Array
            .from(new Set([expectedMessage, expectedMessageWithoutToolPrefix]))
            .filter(Boolean);
          const promptText = typeof prompt.value === 'string'
            ? prompt.value
            : `${prompt.innerText || ''}\n${prompt.textContent || ''}`;
          const normalizedPromptText = normalize(promptText);
          const expectsMessage = expectedMessageVariants.length > 0;
          const promptHasExpectedMessage = expectedMessageVariants.some((variant) => normalizedPromptText.includes(variant));
          const promptIsEmpty = normalizedPromptText.length === 0;
          if (!expectsMessage && !promptIsEmpty) {
            return {
              ready: false,
              reason: 'prompt-not-empty',
              promptFound: true,
              promptHasExpectedMessage: false,
              promptIsEmpty,
              expectedFileCount: Array.isArray(fileNames) ? fileNames.length : -1,
              filenameMatchCount: 0,
              attachmentRootCount: 0
            };
          }

          if (expectsMessage && !promptHasExpectedMessage) {
            return {
              ready: false,
              reason: 'prompt-message-missing',
              promptFound: true,
              promptHasExpectedMessage: false,
              promptIsEmpty,
              expectedFileCount: Array.isArray(fileNames) ? fileNames.length : -1,
              filenameMatchCount: 0,
              attachmentRootCount: 0
            };
          }

          const expectedFiles = Array.isArray(fileNames)
            ? fileNames.map(normalize).filter(Boolean)
            : [];
          if (expectedFiles.length !== 1) {
            return {
              ready: false,
              reason: 'unexpected-file-count',
              promptFound: true,
              promptHasExpectedMessage: !expectsMessage || promptHasExpectedMessage,
              promptIsEmpty,
              expectedFileCount: expectedFiles.length,
              filenameMatchCount: 0,
              attachmentRootCount: 0
            };
          }

          const promptRect = prompt.getBoundingClientRect();
          const nearPrompt = (element) => {
            const rect = element.getBoundingClientRect();
            const horizontallyNearPrompt = rect.right >= promptRect.left - 160 &&
              rect.left <= promptRect.right + 160;
            const verticallyNearPrompt = rect.bottom >= promptRect.top - 260 &&
              rect.top <= promptRect.bottom + 260;
            return horizontallyNearPrompt && verticallyNearPrompt;
          };
          const isHistoryOrMessage = (element) =>
            element.closest('[data-message-author-role]') !== null ||
            element.closest('[data-testid^="project-conversation"]') !== null ||
            element.closest('a[href]') !== null;
          const textOf = (element) => normalize([
            element.getAttribute('class') || '',
            element.getAttribute('data-testid') || '',
            element.getAttribute('data-test-id') || '',
            element.getAttribute('data-file-id') || '',
            element.getAttribute('data-attachment-id') || '',
            element.getAttribute('aria-label') || '',
            element.getAttribute('title') || '',
            element.innerText || '',
            element.textContent || ''
          ].join('\n'));
          const metadataOf = (element) => normalize([
            element.getAttribute('class') || '',
            element.getAttribute('data-testid') || '',
            element.getAttribute('data-test-id') || '',
            element.getAttribute('data-file-id') || '',
            element.getAttribute('data-attachment-id') || '',
            element.getAttribute('data-type') || '',
            element.getAttribute('data-role') || '',
            element.getAttribute('aria-label') || '',
            element.getAttribute('title') || ''
          ].join('\n'));
          const attachmentWords = [
            'attachment',
            'attached',
            'attach',
            'file',
            'upload',
            'прикреп',
            'вложен',
            'файл',
            'загруз'
          ];
          const removalWords = [
            'remove',
            'delete',
            'close',
            'clear',
            'удал',
            'закры',
            'очист',
            'откреп'
          ];
          const hasAttachmentMetadata = (element) =>
            attachmentWords.some((word) => metadataOf(element).includes(word)) ||
            (typeof element.querySelectorAll === 'function' &&
              Array.from(element.querySelectorAll('button,[role="button"],[aria-label],[title]'))
                .some((child) => removalWords.some((word) => metadataOf(child).includes(word))));
          const attachmentRootFor = (element) => {
            let current = element;
            for (let depth = 0; current && depth < 5; depth++) {
              if (current !== prompt && !prompt.contains(current) && hasAttachmentMetadata(current)) {
                return current;
              }

              current = current.parentElement;
            }

            return null;
          };
          const filenameMatches = Array
            .from(document.querySelectorAll('button,[role="button"],div,span,li,[aria-label],[title],[data-testid]'))
            .filter((element) => {
              if (element === prompt || prompt.contains(element)) return false;
              if (!visible(element) || !nearPrompt(element) || isHistoryOrMessage(element)) return false;
              const text = textOf(element);
              return expectedFiles.some((fileName) => text.includes(fileName));
            });
          const candidates = filenameMatches
            .map((element) => attachmentRootFor(element))
            .filter(Boolean)
            .filter((element) => {
              if (!visible(element) || !nearPrompt(element) || isHistoryOrMessage(element)) return false;
              const rect = element.getBoundingClientRect();
              return rect.height > 0 &&
                rect.height <= 180 &&
                rect.width > 0 &&
                rect.width <= Math.max(promptRect.width + 240, 520);
            });
          const uniqueCandidates = Array.from(new Set(candidates));
          const roots = uniqueCandidates.filter((element) =>
            !candidates.some((candidate) => candidate !== element && candidate.contains(element)));
          return {
            ready: roots.length === 1,
            reason: roots.length === 1 ? 'ready' : (roots.length === 0 ? 'attachment-root-missing' : 'attachment-root-ambiguous'),
            promptFound: true,
            promptHasExpectedMessage: !expectsMessage || promptHasExpectedMessage,
            promptIsEmpty,
            expectedFileCount: expectedFiles.length,
            filenameMatchCount: filenameMatches.length,
            attachmentRootCount: roots.length
          };
        })()
        """;
    }

    private const string SendClickExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const candidates = [
            ...document.querySelectorAll('button[data-testid="send-button"]'),
            ...document.querySelectorAll('button[aria-label*="Send" i]'),
            ...document.querySelectorAll('button[aria-label*="Отправ" i]')
          ];
          const button = candidates.find((element) =>
            visible(element) &&
            !element.disabled &&
            element.getAttribute('aria-disabled') !== 'true');
          if (!button) return false;
          button.click();
          return true;
        })()
        """;

    private const string ConversationMessageCountExpression =
        """
        (() => document.querySelectorAll('[data-message-author-role="user"],[data-message-author-role="assistant"]').length)()
        """;

    private static string LastAssistantCopyButtonPointExpression(int minimumMessageCount)
    {
        const string resultScript = "const rect = button.getBoundingClientRect(); return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };";
        return LastAssistantCopyButtonExpression(minimumMessageCount, "return null;", "return null;", resultScript);
    }

    private static string LastAssistantCopyButtonClickExpression(int minimumMessageCount)
    {
        const string resultScript = "button.click(); return true;";
        return LastAssistantCopyButtonExpression(minimumMessageCount, "return false;", "return false;", resultScript);
    }

    private static string LastAssistantCopyButtonStateExpression(int minimumMessageCount)
    {
        const string resultScript = "const rect = button.getBoundingClientRect(); return { status: 'copy-button-ready', x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };";
        return LastAssistantCopyButtonExpression(
            minimumMessageCount,
            "return { status: 'no-current-assistant-yet' };",
            "return { status: 'copy-button-missing' };",
            resultScript);
    }

    private static string LastAssistantCopyButtonExpression(
        int minimumMessageCount,
        string noCurrentAssistantResultScript,
        string copyButtonMissingResultScript,
        string resultScript)
    {
        return $$"""
        (() => {
          const minimumMessageCount = {{minimumMessageCount.ToString(CultureInfo.InvariantCulture)}};
          const visible = (element) => {
            if (!element || typeof getComputedStyle !== 'function') return false;
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' &&
              style.visibility !== 'hidden' &&
              rect.width > 0 &&
              rect.height > 0;
          };
          const normalize = (value) => String(value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const isCopyButton = (element) => {
            if (!element || element.tagName?.toLowerCase() !== 'button') return false;
            if (element.getAttribute('data-testid') !== 'copy-turn-action-button') return false;
            const label = normalize(`${element.getAttribute('aria-label') || ''}\n${element.getAttribute('title') || ''}\n${element.innerText || element.textContent || ''}`);
            return label.includes('копировать') || label.includes('copy');
          };
          const follows = (left, right) =>
            !!(left.compareDocumentPosition(right) & Node.DOCUMENT_POSITION_FOLLOWING);
          const messages = Array
            .from(document.querySelectorAll('[data-message-author-role="user"],[data-message-author-role="assistant"]'))
            .filter(visible);
          if (messages.length < minimumMessageCount) {
            {{noCurrentAssistantResultScript}}
          }

          const assistantEntries = messages
            .map((element, index) => ({ element, index }))
            .filter((entry) => entry.element.getAttribute('data-message-author-role') === 'assistant');
          const assistantEntry = assistantEntries[assistantEntries.length - 1] || null;
          if (!assistantEntry || assistantEntry.index < minimumMessageCount - 1) {
            {{noCurrentAssistantResultScript}}
          }

          const assistant = assistantEntry.element;
          const nextMessage = messages.slice(assistantEntry.index + 1)[0] || null;
          const turnRoots = [
            assistant,
            assistant.closest('article'),
            assistant.closest('[data-testid*="conversation-turn"]'),
            assistant.closest('[data-message-id]'),
            assistant.parentElement,
            assistant.parentElement?.parentElement
          ].filter(Boolean);
          const rootButtons = turnRoots.flatMap((root) => Array.from(root.querySelectorAll('button[data-testid="copy-turn-action-button"]')));
          const pageButtons = Array.from(document.querySelectorAll('button[data-testid="copy-turn-action-button"]'));
          const candidates = Array.from(new Set([...rootButtons, ...pageButtons]))
            .filter((button) => visible(button) && isCopyButton(button))
            .filter((button) =>
              assistant.contains(button) ||
              (
                follows(assistant, button) &&
                (!nextMessage || follows(button, nextMessage))
              ));
          const button = candidates[candidates.length - 1] || null;
          if (!button) {
            {{copyButtonMissingResultScript}}
          }

          try {
            window.__electron2dAuditCopyContext = {
              assistant,
              button,
              activeBefore: typeof document !== 'undefined' ? document.activeElement : null
            };
          } catch {
          }

          if (typeof button.scrollIntoView === 'function') {
            button.scrollIntoView({ block: 'center', inline: 'center' });
          }

          {{resultScript}}
        })()
        """;
    }

    private const string ClipboardReadTextExpression =
        """
        (async () => {
          if (!navigator.clipboard || typeof navigator.clipboard.readText !== 'function') {
            return { ok: false, error: 'navigator.clipboard.readText is unavailable' };
          }

          try {
            const timeout = new Promise((resolve) => {
              setTimeout(() => resolve({ ok: false, error: 'navigator.clipboard.readText timed out' }), 1500);
            });
            const read = navigator.clipboard.readText()
              .then((text) => ({ ok: true, text }))
              .catch((error) => ({ ok: false, error: String(error && (error.message || error)) }));
            return await Promise.race([read, timeout]);
          } catch (error) {
            return { ok: false, error: String(error && (error.message || error)) };
          }
        })()
        """;

    private const string ClipboardWriteCapturePreloadExpression =
        """
        (() => {
          const key = '__electron2dAuditClipboardPreloadCapture';
          const current = window[key];
          if (current && current.installed) {
            current.text = null;
            current.error = null;
            current.pending = null;
            return true;
          }

          const state = { installed: true, text: null, error: null, pending: null, dataTransferSetDataPatched: false };
          Object.defineProperty(window, key, {
            configurable: true,
            enumerable: false,
            writable: false,
            value: state
          });

          const captureText = (text) => {
            state.text = String(text ?? '');
            state.error = null;
          };
          const captureDataTransferText = (type, text) => {
            const normalizedType = String(type || '').toLowerCase();
            if (normalizedType === 'text/plain' || normalizedType === 'text') {
              captureText(text);
              return true;
            }

            return false;
          };
          const readClipboardItemText = async (item) => {
            if (!item || typeof item.getType !== 'function') {
              return null;
            }

            const types = Array.from(item.types || []);
            if (!types.includes('text/plain')) {
              return null;
            }

            const blob = await item.getType('text/plain');
            if (!blob || typeof blob.text !== 'function') {
              return null;
            }

            return await blob.text();
          };
          const captureClipboardItems = async (items) => {
            const list = Array.from(items || []);
            for (const item of list) {
              const text = await readClipboardItemText(item);
              if (typeof text === 'string') {
                captureText(text);
                return true;
              }
            }

            state.error = 'navigator.clipboard.write did not include text/plain Markdown';
            return false;
          };
          const isWrapped = (fn) => Boolean(fn && fn.__electron2dAuditPreloadClipboardCapture);
          const markWrapped = (fn) => {
            try {
              Object.defineProperty(fn, '__electron2dAuditPreloadClipboardCapture', {
                configurable: false,
                enumerable: false,
                value: true
              });
            } catch {
            }

            return fn;
          };
          const patchMethod = (owner, name, wrapperFactory) => {
            if (!owner || typeof owner[name] !== 'function' || isWrapped(owner[name])) {
              return false;
            }

            const original = owner[name];
            try {
              Object.defineProperty(owner, name, {
                configurable: true,
                writable: true,
                value: markWrapped(wrapperFactory(original))
              });
              return true;
            } catch {
              try {
                owner[name] = markWrapped(wrapperFactory(original));
                return true;
              } catch (error) {
                state.error = String(error && (error.message || error));
                return false;
              }
            }
          };
          const patchDataTransferSetData = () => {
            const dataTransferType = typeof DataTransfer !== 'undefined' ? DataTransfer : null;
            const prototype = dataTransferType && dataTransferType.prototype ? dataTransferType.prototype : null;
            return patchMethod(prototype, 'setData', (original) => function(type, text) {
              captureDataTransferText(type, text);
              try {
                return original.apply(this, arguments);
              } catch (error) {
                state.error = String(error && (error.message || error));
                throw error;
              }
            });
          };

          try {
            let patched = patchDataTransferSetData();
            state.dataTransferSetDataPatched = patched;

            const clipboard = typeof navigator !== 'undefined' && navigator.clipboard ? navigator.clipboard : null;
            if (!clipboard) {
              if (!patched) {
                state.error = 'navigator.clipboard is unavailable and DataTransfer.setData could not be patched';
              }

              return patched;
            }

            const owners = [Object.getPrototypeOf(clipboard), clipboard].filter(Boolean);
            for (const owner of owners) {
              patched = patchMethod(owner, 'writeText', (original) => function(text) {
                captureText(text);
                try {
                  return original.apply(this, arguments);
                } catch (error) {
                  state.error = String(error && (error.message || error));
                  throw error;
                }
              }) || patched;
              patched = patchMethod(owner, 'write', (original) => function(items) {
                state.error = null;
                const pending = captureClipboardItems(items).catch((error) => {
                  state.error = String(error && (error.message || error));
                  return false;
                });
                state.pending = pending;
                try {
                  return Promise.resolve(original.apply(this, arguments)).finally(() => pending.catch(() => {}));
                } catch (error) {
                  state.error = String(error && (error.message || error));
                  throw error;
                }
              }) || patched;
            }

            if (!patched) {
              state.error = 'navigator.clipboard.writeText/write could not be patched before page scripts';
            }

            return patched;
          } catch (error) {
            state.error = String(error && (error.message || error));
            return false;
          }
        })();
        """;

    private const string ClipboardWriteCaptureResetExpression =
        """
        (() => {
          let reset = false;
          for (const key of ['__electron2dAuditClipboardPreloadCapture', '__electron2dAuditClipboardWriteCapture']) {
            const state = window[key];
            if (state) {
              state.text = null;
              state.error = null;
              state.pending = null;
              reset = true;
            }
          }

          return reset;
        })()
        """;

    private const string ClipboardWriteCaptureInstallExpression =
        """
        (() => {
          const key = '__electron2dAuditClipboardWriteCapture';
          const current = window[key];
          if (current && current.installed && current.copyEventInstalled) {
            current.text = null;
            current.error = null;
            current.pending = null;
            return true;
          }

          const canCaptureClipboardApi = navigator.clipboard &&
            (typeof navigator.clipboard.writeText === 'function' || typeof navigator.clipboard.write === 'function');
          const canCaptureCopyEvent = typeof document !== 'undefined' && typeof document.addEventListener === 'function';
          const canCaptureDataTransferSetData = typeof DataTransfer !== 'undefined' &&
            DataTransfer.prototype &&
            typeof DataTransfer.prototype.setData === 'function';
          if (!canCaptureClipboardApi && !canCaptureCopyEvent && !canCaptureDataTransferSetData) {
            return false;
          }

          try {
            const originalWriteText = navigator.clipboard && typeof navigator.clipboard.writeText === 'function'
              ? navigator.clipboard.writeText.bind(navigator.clipboard)
              : null;
            const originalWrite = navigator.clipboard && typeof navigator.clipboard.write === 'function'
              ? navigator.clipboard.write.bind(navigator.clipboard)
              : null;
            const state = { installed: true, copyEventInstalled: false, dataTransferSetDataPatched: false, text: null, error: null, pending: null, originalWriteText, originalWrite };
            Object.defineProperty(window, key, {
              configurable: true,
              enumerable: false,
              writable: false,
              value: state
            });

            const captureText = (text) => {
              state.text = String(text ?? '');
              state.error = null;
            };
            const nodeWithin = (root, node) => {
              if (!root || !node) {
                return false;
              }

              let current = node;
              while (current && current.nodeType !== 1 && current.parentElement) {
                current = current.parentElement;
              }

              for (; current; current = current.parentElement) {
                if (current === root) {
                  return true;
                }
              }

              return false;
            };
            const copyContext = () => {
              const win = typeof window !== 'undefined' ? window : null;
              return win && win.__electron2dAuditCopyContext ? win.__electron2dAuditCopyContext : null;
            };
            const captureDataTransferText = (type, text) => {
              const normalizedType = String(type || '').toLowerCase();
              if (normalizedType === 'text/plain' || normalizedType === 'text') {
                captureText(text);
                return true;
              }

              return false;
            };
            const readClipboardItemText = async (item) => {
              if (!item || typeof item.getType !== 'function') {
                return null;
              }

              const types = Array.from(item.types || []);
              if (!types.includes('text/plain')) {
                return null;
              }

              const blob = await item.getType('text/plain');
              if (!blob || typeof blob.text !== 'function') {
                return null;
              }

              return await blob.text();
            };
            const readActiveElementText = () => {
              const doc = typeof document !== 'undefined' ? document : null;
              const active = doc && doc.activeElement ? doc.activeElement : null;
              if (!active) {
                return '';
              }

              if (typeof active.value === 'string') {
                const start = typeof active.selectionStart === 'number' ? active.selectionStart : 0;
                const end = typeof active.selectionEnd === 'number' ? active.selectionEnd : 0;
                if (end > start) {
                  const context = copyContext();
                  if (context && active === context.activeBefore) {
                    return '';
                  }

                  return active.value.substring(start, end);
                }

                return '';
              }

              return '';
            };
            const readSelectedText = () => {
              const doc = typeof document !== 'undefined' ? document : null;
              const win = typeof window !== 'undefined' ? window : null;
              const readSelection = (selection) => {
                if (!selection || typeof selection.toString !== 'function') {
                  return '';
                }

                const text = selection.toString();
                if (typeof text !== 'string' || text.length === 0) {
                  return '';
                }

                const context = copyContext();
                if (!context || !context.assistant) {
                  return '';
                }

                if (nodeWithin(context.assistant, selection.anchorNode) &&
                  nodeWithin(context.assistant, selection.focusNode)) {
                  return text;
                }

                return '';
              };
              if (doc && typeof doc.getSelection === 'function') {
                const text = readSelection(doc.getSelection());
                if (text.length > 0) {
                  return text;
                }
              }

              if (win && typeof win.getSelection === 'function') {
                const text = readSelection(win.getSelection());
                if (text.length > 0) {
                  return text;
                }
              }

              return '';
            };
            const readCopyEventText = (event) => {
              const clipboardData = event && event.clipboardData;
              if (clipboardData && typeof clipboardData.getData === 'function') {
                for (const type of ['text/plain', 'text']) {
                  const text = clipboardData.getData(type);
                  if (typeof text === 'string' && text.length > 0) {
                    return text;
                  }
                }
              }

              const selected = readSelectedText();
              if (typeof selected === 'string' && selected.length > 0) {
                return selected;
              }

              return readActiveElementText();
            };
            const captureCopyEvent = (event) => {
              const capture = () => {
                const text = readCopyEventText(event);
                if (typeof text === 'string' && text.length > 0) {
                  captureText(text);
                  return true;
                }

                return false;
              };
              if (!capture()) {
                setTimeout(capture, 0);
              }
            };
            const addCopyListener = (target) => {
              if (!target || typeof target.addEventListener !== 'function') {
                return false;
              }

              target.addEventListener('copy', captureCopyEvent, true);
              target.addEventListener('copy', captureCopyEvent, false);
              return true;
            };
            const captureClipboardItems = async (items) => {
              const list = Array.from(items || []);
              for (const item of list) {
                const text = await readClipboardItemText(item);
                if (typeof text === 'string') {
                  captureText(text);
                  return true;
                }
              }

              state.error = 'navigator.clipboard.write did not include text/plain Markdown';
              return false;
            };
            const isSetDataWrapped = (fn) => Boolean(fn &&
              (fn.__electron2dAuditClipboardCapture || fn.__electron2dAuditPreloadClipboardCapture));
            const markSetDataWrapped = (fn) => {
              try {
                Object.defineProperty(fn, '__electron2dAuditClipboardCapture', {
                  configurable: false,
                  enumerable: false,
                  value: true
                });
              } catch {
              }

              return fn;
            };
            const patchDataTransferSetData = () => {
              const dataTransferType = typeof DataTransfer !== 'undefined' ? DataTransfer : null;
              const prototype = dataTransferType && dataTransferType.prototype ? dataTransferType.prototype : null;
              if (!prototype || typeof prototype.setData !== 'function') {
                return false;
              }

              if (isSetDataWrapped(prototype.setData)) {
                return true;
              }

              const originalSetData = prototype.setData;
              const wrappedSetData = markSetDataWrapped(function(type, text) {
                captureDataTransferText(type, text);
                try {
                  return originalSetData.apply(this, arguments);
                } catch (error) {
                  state.error = String(error && (error.message || error));
                  throw error;
                }
              });

              try {
                Object.defineProperty(prototype, 'setData', {
                  configurable: true,
                  writable: true,
                  value: wrappedSetData
                });
                return true;
              } catch {
                try {
                  prototype.setData = wrappedSetData;
                  return true;
                } catch (error) {
                  state.error = String(error && (error.message || error));
                  return false;
                }
              }
            };

            if (originalWriteText) {
              navigator.clipboard.writeText = function(text) {
                captureText(text);
                try {
                  return originalWriteText(text);
                } catch (error) {
                  state.error = String(error && (error.message || error));
                  throw error;
                }
              };
            }

            state.dataTransferSetDataPatched = patchDataTransferSetData();
            const documentTarget = typeof document !== 'undefined' ? document : null;
            const windowTarget = typeof window !== 'undefined' ? window : null;
            state.copyEventInstalled = addCopyListener(documentTarget);
            if (windowTarget && windowTarget !== documentTarget) {
              state.copyEventInstalled = addCopyListener(windowTarget) || state.copyEventInstalled;
            }

            if (originalWrite) {
              navigator.clipboard.write = function(items) {
                state.error = null;
                const pending = captureClipboardItems(items).catch((error) => {
                  state.error = String(error && (error.message || error));
                  return false;
                });
                state.pending = pending;
                try {
                  return Promise.resolve(originalWrite(items)).finally(() => pending.catch(() => {}));
                } catch (error) {
                  state.error = String(error && (error.message || error));
                  throw error;
                }
              };
            }

            return true;
          } catch (error) {
            return false;
          }
        })()
        """;

    private const string ClipboardCapturedWriteTextExpression =
        """
        (async () => {
          const states = [
            window.__electron2dAuditClipboardPreloadCapture,
            window.__electron2dAuditClipboardWriteCapture
          ].filter(Boolean);
          for (const state of states) {
            if (state.pending) {
              const timeout = new Promise((resolve) => {
                setTimeout(() => resolve(false), 1500);
              });
              await Promise.race([
                Promise.resolve(state.pending).then(() => true, () => false),
                timeout
              ]);
            }
          }

          for (const state of states) {
            if (typeof state.text === 'string') {
              return { ok: true, text: state.text };
            }
          }

          const errors = states
            .map((state) => state && state.error ? state.error : '')
            .filter((error) => error.length > 0);
          return {
            ok: false,
            error: errors.length > 0 ? errors.join('; ') : 'copy action Markdown was not captured'
          };
        })()
        """;

    private const string IsGeneratingExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const normalize = (value) => (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const candidates = [
            ...document.querySelectorAll('button[data-testid="stop-button"]'),
            ...document.querySelectorAll('button[aria-label*="Stop" i]'),
            ...document.querySelectorAll('button[aria-label*="Останов" i]'),
            ...document.querySelectorAll('button[aria-label*="Прекрат" i]')
          ];
          const hasActiveStopButton = candidates.some((element) =>
            visible(element) &&
            !element.disabled &&
            element.getAttribute('aria-disabled') !== 'true');
          if (hasActiveStopButton) {
            return true;
          }

          const isReportLabel = (text) =>
            text.includes('углубленный исследовательский отчет') ||
            text.includes('углублённый исследовательский отчёт') ||
            text.includes('deep research report');
          const isCompletedResearch = (text) =>
            text.includes('исследование завершено') ||
            text.includes('research completed');
          const visibleTexts = Array
            .from(document.querySelectorAll('div,section,article'))
            .filter(visible)
            .map((element) => normalize(`${element.getAttribute('aria-label') || ''}\n${element.innerText || element.textContent || ''}`));
          const completedResearchVisible = visibleTexts.some(isCompletedResearch);
          const completedReportCardVisible = visibleTexts.some((text) => isReportLabel(text) && text.includes('verdict:'));
          if (completedResearchVisible && completedReportCardVisible) {
            return false;
          }

          return false;
        })()
        """;

    private const string RateLimitDialogDismissPointExpression =
        """
        (() => {
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const normalize = (value) => (value || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const dialogs = Array
            .from(document.querySelectorAll('[role="dialog"],[aria-modal="true"],div'))
            .filter((element) => visible(element))
            .map((element) => ({ element, rect: element.getBoundingClientRect(), text: normalize(element.innerText || element.textContent || '') }))
            .filter((entry) =>
              entry.rect.width >= 260 &&
              entry.rect.height >= 120 &&
              (
                entry.text.includes('слишком много запросов') ||
                entry.text.includes('вы отправляете запросы слишком часто') ||
                entry.text.includes('too many requests') ||
                entry.text.includes('sending requests too frequently')
              ));
          const dialog = dialogs
            .sort((left, right) => (left.rect.width * left.rect.height) - (right.rect.width * right.rect.height))[0];
          if (!dialog) return false;
          const buttons = Array
            .from(dialog.element.querySelectorAll('button,[role="button"]'))
            .filter((element) => visible(element))
            .map((element) => ({ element, rect: element.getBoundingClientRect(), text: normalize(`${element.getAttribute('aria-label') || ''}\n${element.innerText || element.textContent || ''}`) }))
            .filter((entry) => entry.text === 'понятно' || entry.text === 'ok' || entry.text === 'okay' || entry.text === 'got it');
          const target = (buttons.length > 0 ? buttons : Array
            .from(dialog.element.querySelectorAll('button,[role="button"]'))
            .filter((element) => visible(element))
            .map((element) => ({ element, rect: element.getBoundingClientRect() })))
            .sort((left, right) => (right.rect.top - left.rect.top) || (right.rect.left - left.rect.left))[0];
          if (!target) return false;
          return { x: target.rect.left + target.rect.width / 2, y: target.rect.top + target.rect.height / 2 };
        })()
        """;

}

internal sealed class AuditSubmitCodexChromeClient : IAsyncDisposable
{
    private const int MaxFrameBytes = 64 * 1024 * 1024;
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan InputDispatchTimeout = TimeSpan.FromSeconds(10);
    private readonly NamedPipeClientStream stream;
    private readonly string sessionId;
    private readonly string turnId;
    private readonly SemaphoreSlim requestLock = new(1, 1);
    private int nextRequestId;

    private AuditSubmitCodexChromeClient(NamedPipeClientStream stream, string sessionId, string turnId)
    {
        this.stream = stream;
        this.sessionId = sessionId;
        this.turnId = turnId;
    }

    public static async Task<AuditSubmitCodexChromeClient> ConnectAsync(
        IAuditSubmitBrowserOptions options,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                "Codex Chrome Extension pipe backend is currently supported only on Windows.");
        }

        if (!string.IsNullOrWhiteSpace(options.CodexChromePipe))
        {
            var explicitClient = await ConnectToPipeAsync(options.CodexChromePipe, options, cancellationToken).ConfigureAwait(false);
            if (!await explicitClient.IsExtensionBackendAsync(cancellationToken).ConfigureAwait(false))
            {
                await explicitClient.DisposeAsync().ConfigureAwait(false);
                throw new AuditSubmitCodexChromeException(
                    "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                    $"Codex browser pipe is not a Chrome extension backend: {options.CodexChromePipe}");
            }

            return explicitClient;
        }

        var failures = new List<string>();
        foreach (var pipe in EnumerateCodexBrowserUsePipes())
        {
            AuditSubmitCodexChromeClient? client = null;
            try
            {
                client = await ConnectToPipeAsync(pipe, options, cancellationToken).ConfigureAwait(false);
                if (await client.IsExtensionBackendAsync(cancellationToken).ConfigureAwait(false))
                {
                    return client;
                }
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or AuditSubmitCodexChromeException or JsonException)
            {
                failures.Add($"{pipe}: {ex.Message}");
            }

            if (client is not null)
            {
                await client.DisposeAsync().ConfigureAwait(false);
            }
        }

        var detail = failures.Count == 0
            ? "No codex-browser-use pipe was found."
            : string.Join("; ", failures.Take(4));
        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
            $"Codex Chrome Extension backend was not found. {detail}");
    }

    public async ValueTask DisposeAsync()
    {
        requestLock.Dispose();
        await stream.DisposeAsync().ConfigureAwait(false);
    }

    public async Task<long> CreateTabAsync(CancellationToken cancellationToken)
    {
        var result = await RequestAsync("createTab", [], TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        if (result.TryGetProperty("id", out var id))
        {
            return id.ValueKind == JsonValueKind.Number
                ? id.GetInt64()
                : long.Parse(id.GetString() ?? string.Empty, CultureInfo.InvariantCulture);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
            "Codex Chrome Extension createTab response did not contain a tab id.");
    }

    public async Task AttachAsync(long tabId, CancellationToken cancellationToken)
    {
        await RequestAsync("attach", new Dictionary<string, object?> { ["tabId"] = tabId }, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
    }

    public async Task DetachAsync(long tabId, CancellationToken cancellationToken)
    {
        await RequestAsync("detach", new Dictionary<string, object?> { ["tabId"] = tabId }, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
    }

    public async Task AttachTargetAsync(long tabId, string targetId, CancellationToken cancellationToken)
    {
        await AttachTargetCoreAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
    }

    public async Task AttachTargetWithRecoveryAsync(long tabId, string targetId, CancellationToken cancellationToken)
    {
        await AuditSubmitCdpRecoveryPolicy.ExecuteAsync(
            async (_, token) =>
            {
                await AttachTargetCoreAsync(tabId, targetId, token).ConfigureAwait(false);
                return true;
            },
            (_, token) => ReattachCdpAsync(tabId, token),
            static ex => ex is AuditSubmitCodexChromeException chromeException && IsRecoverableCdpFailure(chromeException),
            allowTransientRecovery: true,
            maxAttempts: 5,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task EnsureTargetAttachedForReadAsync(long tabId, string targetId, CancellationToken cancellationToken)
    {
        await AttachTargetWithRecoveryAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
    }

    private async Task AttachTargetCoreAsync(long tabId, string targetId, CancellationToken cancellationToken)
    {
        await RequestAsync(
            "attachTarget",
            new Dictionary<string, object?>
            {
                ["tabId"] = tabId,
                ["targetId"] = targetId
            },
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task DetachTargetAsync(long tabId, string targetId, CancellationToken cancellationToken)
    {
        if (!stream.IsConnected)
        {
            return;
        }

        try
        {
            await RequestAsync(
                "detachTarget",
                new Dictionary<string, object?>
                {
                    ["tabId"] = tabId,
                    ["targetId"] = targetId
                },
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or AuditSubmitCodexChromeException or JsonException or OperationCanceledException)
        {
        }
    }

    public async Task FinalizeTabsAsync(CancellationToken cancellationToken)
    {
        if (!stream.IsConnected)
        {
            return;
        }

        try
        {
            await RequestAsync(
                "finalizeTabs",
                new Dictionary<string, object?> { ["keep"] = Array.Empty<object>() },
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or AuditSubmitCodexChromeException or JsonException or OperationCanceledException)
            {
            }
    }

    public async Task<JsonElement> ExecuteCdpAsync(
        long tabId,
        string method,
        Dictionary<string, object?> commandParameters,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        return await AuditSubmitCdpRecoveryPolicy.ExecuteAsync(
            (_, token) => ExecuteCdpCoreAsync(tabId, method, commandParameters, timeout, token),
            (_, token) => ReattachCdpAsync(tabId, token),
            static ex => ex is AuditSubmitCodexChromeException chromeException && IsRecoverableCdpFailure(chromeException),
            allowTransientRecovery,
            maxAttempts: 5,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonElement> ExecuteCdpCoreAsync(
        long tabId,
        string method,
        Dictionary<string, object?> commandParameters,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return await ExecuteCdpCoreAsync(
            new Dictionary<string, object?> { ["tabId"] = tabId },
            method,
            commandParameters,
            timeout,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<JsonElement> ExecuteCdpOnTargetAsync(
        long tabId,
        string targetId,
        string method,
        Dictionary<string, object?> commandParameters,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        return await AuditSubmitCdpRecoveryPolicy.ExecuteAsync(
            (_, token) => ExecuteCdpCoreAsync(
                new Dictionary<string, object?>
                {
                    ["tabId"] = tabId,
                    ["targetId"] = targetId
                },
                method,
                commandParameters,
                timeout,
                token),
            async (_, token) =>
            {
                await ReattachCdpAsync(tabId, token).ConfigureAwait(false);
                await EnsureTargetAttachedForReadAsync(tabId, targetId, token).ConfigureAwait(false);
            },
            static ex => ex is AuditSubmitCodexChromeException chromeException && IsRecoverableCdpFailure(chromeException),
            allowTransientRecovery,
            maxAttempts: 5,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonElement> ExecuteCdpCoreAsync(
        Dictionary<string, object?> target,
        string method,
        Dictionary<string, object?> commandParameters,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        return await RequestAsync(
            "executeCdp",
            new Dictionary<string, object?>
            {
                ["target"] = target,
                ["method"] = method,
                ["commandParams"] = commandParameters,
                ["timeoutMs"] = (int)Math.Ceiling(timeout.TotalMilliseconds)
            },
            timeout + TimeSpan.FromSeconds(5),
            cancellationToken).ConfigureAwait(false);
    }

    private static bool TryReadPoint(JsonElement value, out AuditSubmitDomPoint point)
    {
        point = default;
        if (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("x", out var xElement) ||
            !value.TryGetProperty("y", out var yElement) ||
            xElement.ValueKind != JsonValueKind.Number ||
            yElement.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        var x = xElement.GetDouble();
        var y = yElement.GetDouble();
        if (!double.IsFinite(x) || !double.IsFinite(y))
        {
            return false;
        }

        point = new AuditSubmitDomPoint(x, y);
        return true;
    }

    public async Task<JsonElement> EvaluateAsync(
        long tabId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var result = await ExecuteCdpAsync(
            tabId,
            "Runtime.evaluate",
            new Dictionary<string, object?>
            {
                ["expression"] = expression,
                ["returnByValue"] = true,
                ["awaitPromise"] = true
            },
            timeout,
            cancellationToken,
            allowTransientRecovery).ConfigureAwait(false);
        if (result.TryGetProperty("exceptionDetails", out var exceptionDetails))
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-BROWSER-FAILED",
                $"Browser JavaScript failed: {DescribeRemoteException(exceptionDetails)}");
        }

        if (result.TryGetProperty("result", out var remoteObject) &&
            remoteObject.TryGetProperty("value", out var value))
        {
            return value.Clone();
        }

        return default;
    }

    public async Task<JsonElement> EvaluateInContextAsync(
        long tabId,
        int contextId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var result = await ExecuteCdpAsync(
            tabId,
            "Runtime.evaluate",
            new Dictionary<string, object?>
            {
                ["expression"] = expression,
                ["contextId"] = contextId,
                ["returnByValue"] = true,
                ["awaitPromise"] = true
            },
            timeout,
            cancellationToken,
            allowTransientRecovery).ConfigureAwait(false);
        if (result.TryGetProperty("exceptionDetails", out var exceptionDetails))
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-BROWSER-FAILED",
                $"Browser JavaScript failed: {DescribeRemoteException(exceptionDetails)}");
        }

        if (result.TryGetProperty("result", out var remoteObject) &&
            remoteObject.TryGetProperty("value", out var value))
        {
            return value.Clone();
        }

        return default;
    }

    public async Task<JsonElement> EvaluateOnTargetAsync(
        long tabId,
        string targetId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var result = await ExecuteCdpOnTargetAsync(
            tabId,
            targetId,
            "Runtime.evaluate",
            new Dictionary<string, object?>
            {
                ["expression"] = expression,
                ["returnByValue"] = true,
                ["awaitPromise"] = true
            },
            timeout,
            cancellationToken,
            allowTransientRecovery).ConfigureAwait(false);
        if (result.TryGetProperty("exceptionDetails", out var exceptionDetails))
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-BROWSER-FAILED",
                $"Browser JavaScript failed: {DescribeRemoteException(exceptionDetails)}");
        }

        if (result.TryGetProperty("result", out var remoteObject) &&
            remoteObject.TryGetProperty("value", out var value))
        {
            return value.Clone();
        }

        return default;
    }

    public async Task<JsonElement> EvaluateInContextOnTargetAsync(
        long tabId,
        string targetId,
        int contextId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var result = await ExecuteCdpOnTargetAsync(
            tabId,
            targetId,
            "Runtime.evaluate",
            new Dictionary<string, object?>
            {
                ["expression"] = expression,
                ["contextId"] = contextId,
                ["returnByValue"] = true,
                ["awaitPromise"] = true
            },
            timeout,
            cancellationToken,
            allowTransientRecovery).ConfigureAwait(false);
        if (result.TryGetProperty("exceptionDetails", out var exceptionDetails))
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-BROWSER-FAILED",
                $"Browser JavaScript failed: {DescribeRemoteException(exceptionDetails)}");
        }

        if (result.TryGetProperty("result", out var remoteObject) &&
            remoteObject.TryGetProperty("value", out var value))
        {
            return value.Clone();
        }

        return default;
    }

    public async Task<AuditSubmitDomPoint?> EvaluatePointAsync(
        long tabId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var value = await EvaluateAsync(tabId, expression, timeout, cancellationToken, allowTransientRecovery).ConfigureAwait(false);
        return TryReadPoint(value, out var point)
            ? point
            : null;
    }

    public async Task<bool> EvaluateBoolAsync(
        long tabId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var value = await EvaluateAsync(tabId, expression, timeout, cancellationToken, allowTransientRecovery).ConfigureAwait(false);
        return value.ValueKind == JsonValueKind.True;
    }

    public async Task<bool> EvaluateBoolInContextAsync(
        long tabId,
        int contextId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var value = await EvaluateInContextAsync(tabId, contextId, expression, timeout, cancellationToken, allowTransientRecovery).ConfigureAwait(false);
        return value.ValueKind == JsonValueKind.True;
    }

    public async Task<bool> EvaluateBoolOnTargetAsync(
        long tabId,
        string targetId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var value = await EvaluateOnTargetAsync(tabId, targetId, expression, timeout, cancellationToken, allowTransientRecovery).ConfigureAwait(false);
        return value.ValueKind == JsonValueKind.True;
    }

    public async Task<bool> EvaluateBoolInContextOnTargetAsync(
        long tabId,
        string targetId,
        int contextId,
        string expression,
        TimeSpan timeout,
        CancellationToken cancellationToken,
        bool allowTransientRecovery = true)
    {
        var value = await EvaluateInContextOnTargetAsync(tabId, targetId, contextId, expression, timeout, cancellationToken, allowTransientRecovery).ConfigureAwait(false);
        return value.ValueKind == JsonValueKind.True;
    }

    public async Task ClickAtAsync(long tabId, AuditSubmitDomPoint point, CancellationToken cancellationToken)
    {
        await ExecuteCdpAsync(
            tabId,
            "Input.dispatchMouseEvent",
            new Dictionary<string, object?>
            {
                ["type"] = "mouseMoved",
                ["x"] = point.X,
                ["y"] = point.Y
            },
            InputDispatchTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        await ExecuteCdpAsync(
            tabId,
            "Input.dispatchMouseEvent",
            new Dictionary<string, object?>
            {
                ["type"] = "mousePressed",
                ["x"] = point.X,
                ["y"] = point.Y,
                ["button"] = "left",
                ["buttons"] = 1,
                ["clickCount"] = 1
            },
            InputDispatchTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        await ExecuteCdpAsync(
            tabId,
            "Input.dispatchMouseEvent",
            new Dictionary<string, object?>
            {
                ["type"] = "mouseReleased",
                ["x"] = point.X,
                ["y"] = point.Y,
                ["button"] = "left",
                ["buttons"] = 0,
                ["clickCount"] = 1
            },
            InputDispatchTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
    }

    private async Task ReattachCdpAsync(long tabId, CancellationToken cancellationToken)
    {
        List<string> failures = [];
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                await DetachOwnedTabBestEffortAsync(tabId, cancellationToken).ConfigureAwait(false);
                await AttachAsync(tabId, cancellationToken).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromMilliseconds(500 * (attempt + 1)), cancellationToken).ConfigureAwait(false);

                foreach (var method in new[] { "Runtime.enable", "DOM.enable", "Page.enable" })
                {
                    try
                    {
                        await ExecuteCdpCoreAsync(tabId, method, [], TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                    }
                    catch (AuditSubmitCodexChromeException ex) when (IsRecoverableCdpFailure(ex))
                    {
                        failures.Add(ex.Message);
                    }
                }

                return;
            }
            catch (AuditSubmitCodexChromeException ex) when (IsRecoverableCdpFailure(ex))
            {
                failures.Add(ex.Message);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * (attempt + 1)), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
            "CDP reattach failed after recoverable detachment: " + string.Join("; ", failures));
    }

    private async Task DetachOwnedTabBestEffortAsync(long tabId, CancellationToken cancellationToken)
    {
        try
        {
            await DetachAsync(tabId, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or AuditSubmitCodexChromeException or JsonException or OperationCanceledException)
        {
        }
    }

    private static bool IsRecoverableCdpFailure(AuditSubmitCodexChromeException exception)
    {
        return exception.Message.Contains("Debugger unattached", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Debugger is not attached", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Timed out", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Cannot find context", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Target closed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryReadFiniteDouble(JsonElement value, string propertyName, out double number)
    {
        number = 0;
        return value.ValueKind == JsonValueKind.Object &&
            value.TryGetProperty(propertyName, out var property) &&
            property.ValueKind == JsonValueKind.Number &&
            double.IsFinite(number = property.GetDouble());
    }

    private static async Task<AuditSubmitCodexChromeClient> ConnectToPipeAsync(
        string pipePath,
        IAuditSubmitBrowserOptions options,
        CancellationToken cancellationToken)
    {
        var stream = new NamedPipeClientStream(
            ".",
            NormalizePipeName(pipePath),
            PipeDirection.InOut,
            PipeOptions.Asynchronous);
        try
        {
            using var timeout = new CancellationTokenSource(ConnectTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            await stream.ConnectAsync(linked.Token).ConfigureAwait(false);
            return new AuditSubmitCodexChromeClient(stream, options.CodexSessionId, options.CodexTurnId);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                $"Timed out connecting to Codex Chrome Extension pipe: {pipePath}");
        }
        catch
        {
            await stream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<bool> IsExtensionBackendAsync(CancellationToken cancellationToken)
    {
        var info = await RequestAsync("getInfo", [], TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        return info.TryGetProperty("type", out var type) &&
            string.Equals(type.GetString(), "extension", StringComparison.Ordinal);
    }

    private async Task<JsonElement> RequestAsync(
        string method,
        Dictionary<string, object?> parameters,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        await requestLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeoutSource = new CancellationTokenSource(timeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
            var requestId = Interlocked.Increment(ref nextRequestId);
            var fullParameters = new Dictionary<string, object?>(parameters, StringComparer.Ordinal)
            {
                ["session_id"] = sessionId,
                ["turn_id"] = turnId
            };
            var request = new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["method"] = method,
                ["params"] = fullParameters,
                ["id"] = requestId
            };
            await WriteFrameAsync(JsonSerializer.SerializeToUtf8Bytes(request), linked.Token).ConfigureAwait(false);

            while (true)
            {
                var frame = await ReadFrameAsync(linked.Token).ConfigureAwait(false);
                using var document = JsonDocument.Parse(frame);
                var root = document.RootElement;
                if (!root.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || id.GetInt32() != requestId)
                {
                    if (await TryHandleIncomingJsonRpcRequestAsync(root, linked.Token).ConfigureAwait(false))
                    {
                        continue;
                    }

                    continue;
                }

                if (root.TryGetProperty("error", out var error))
                {
                    throw new AuditSubmitCodexChromeException(
                        "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                        DescribeJsonRpcError(method, error));
                }

                return root.TryGetProperty("result", out var result)
                    ? result.Clone()
                    : default;
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-UNAVAILABLE",
                $"Timed out waiting for Codex Chrome Extension method {method}.");
        }
        finally
        {
            requestLock.Release();
        }
    }

    private async Task<bool> TryHandleIncomingJsonRpcRequestAsync(JsonElement root, CancellationToken cancellationToken)
    {
        if (root.ValueKind != JsonValueKind.Object ||
            !root.TryGetProperty("jsonrpc", out var jsonRpc) ||
            jsonRpc.ValueKind != JsonValueKind.String ||
            !string.Equals(jsonRpc.GetString(), "2.0", StringComparison.Ordinal) ||
            !root.TryGetProperty("method", out var method) ||
            method.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("id", out var id) ||
            id.ValueKind is not (JsonValueKind.Number or JsonValueKind.String))
        {
            return false;
        }

        var response = string.Equals(method.GetString(), "ping", StringComparison.Ordinal)
            ? new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = CloneJsonRpcId(id),
                ["result"] = "pong"
            }
            : new Dictionary<string, object?>
            {
                ["jsonrpc"] = "2.0",
                ["id"] = CloneJsonRpcId(id),
                ["error"] = new Dictionary<string, object?>
                {
                    ["code"] = -32601,
                    ["message"] = $"Unsupported client method: {method.GetString()}"
                }
            };
        await WriteFrameAsync(JsonSerializer.SerializeToUtf8Bytes(response), cancellationToken).ConfigureAwait(false);
        return true;
    }

    private static object CloneJsonRpcId(JsonElement id)
    {
        return id.ValueKind == JsonValueKind.String
            ? id.GetString() ?? string.Empty
            : id.GetInt64();
    }

    private async Task WriteFrameAsync(byte[] payload, CancellationToken cancellationToken)
    {
        if (payload.Length > MaxFrameBytes)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                "Codex Chrome Extension request frame is too large.");
        }

        var header = new byte[4];
        if (BitConverter.IsLittleEndian)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(header, (uint)payload.Length);
        }
        else
        {
            BinaryPrimitives.WriteUInt32BigEndian(header, (uint)payload.Length);
        }

        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<byte[]> ReadFrameAsync(CancellationToken cancellationToken)
    {
        var header = new byte[4];
        await stream.ReadExactlyAsync(header, cancellationToken).ConfigureAwait(false);
        var length = BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReadUInt32LittleEndian(header)
            : BinaryPrimitives.ReadUInt32BigEndian(header);
        if (length == 0 || length > MaxFrameBytes)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                $"Codex Chrome Extension returned an invalid frame length: {length}.");
        }

        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        return payload;
    }

    private static IEnumerable<string> EnumerateCodexBrowserUsePipes()
    {
        const string pipeRoot = @"\\.\pipe\";
        List<string> candidates = [];
        try
        {
            candidates.AddRange(Directory.EnumerateFileSystemEntries(pipeRoot)
                .Where(path => path.StartsWith(pipeRoot + "codex-browser-use", StringComparison.OrdinalIgnoreCase)));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        try
        {
            candidates.AddRange(Directory.EnumerateFileSystemEntries(pipeRoot + "codex-browser-use")
                .Where(path => path.StartsWith(pipeRoot + "codex-browser-use", StringComparison.OrdinalIgnoreCase)));
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizePipeName(string pipePath)
    {
        const string prefix = @"\\.\pipe\";
        return pipePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? pipePath[prefix.Length..]
            : pipePath;
    }

    private static string DescribeJsonRpcError(string method, JsonElement error)
    {
        if (error.ValueKind == JsonValueKind.Object &&
            error.TryGetProperty("message", out var message) &&
            message.ValueKind == JsonValueKind.String)
        {
            return $"Codex Chrome Extension method {method} failed: {message.GetString()}";
        }

        return $"Codex Chrome Extension method {method} failed.";
    }

    private static string DescribeRemoteException(JsonElement exceptionDetails)
    {
        if (exceptionDetails.TryGetProperty("exception", out var exception) &&
            exception.ValueKind == JsonValueKind.Object)
        {
            if (exception.TryGetProperty("description", out var description) && description.ValueKind == JsonValueKind.String)
            {
                return description.GetString() ?? "unknown browser exception";
            }

            if (exception.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "unknown browser exception";
            }
        }

        return exceptionDetails.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String
            ? text.GetString() ?? "unknown browser exception"
            : "unknown browser exception";
    }
}

internal readonly record struct AuditSubmitDomPoint(double X, double Y);

internal readonly record struct AuditSubmitDomRect(double X, double Y, double Width, double Height);

internal readonly record struct AuditSubmitDeepResearchFrame(int ContextId, AuditSubmitDomRect Rect);

internal readonly record struct AuditSubmitReportCandidateResult(bool SurfaceSelected, AuditSubmitReportCandidate[] Candidates)
{
    public static AuditSubmitReportCandidateResult NoSurface { get; } = new(false, []);
}

internal readonly record struct AuditSubmitDeepResearchTargetSelection(string? TargetId, bool WaitForNewerTarget);

internal enum AuditSubmitExportSurfaceScope
{
    Page,
    DeepResearchFrame,
    DeepResearchTarget,
    DeepResearchTargetFrame
}

internal readonly record struct AuditSubmitTargetFrameContext(string TargetId, string FrameId, int ContextId, bool IsRoot);

internal readonly record struct AuditSubmitFrameTreeEntry(string FrameId, string Url, string Name, bool IsRoot);

internal readonly record struct AuditSubmitTargetInfoEntry(string TargetId, string Type, string Url, string Title, bool Attached);

internal static class AuditSubmitCdpRecoveryPolicy
{
    public static async Task<T> ExecuteAsync<T>(
        Func<int, CancellationToken, Task<T>> executeAsync,
        Func<int, CancellationToken, Task> recoverAsync,
        Func<Exception, bool> isRecoverable,
        bool allowTransientRecovery,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), maxAttempts, "CDP recovery must allow at least one attempt.");
        }

        if (!allowTransientRecovery)
        {
            await recoverAsync(0, cancellationToken).ConfigureAwait(false);
        }

        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                return await executeAsync(attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (allowTransientRecovery && attempt < maxAttempts - 1 && isRecoverable(ex))
            {
                await recoverAsync(attempt + 1, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("CDP recovery loop ended without returning or throwing a protocol error.");
    }
}

internal sealed class AuditSubmitCodexChromeException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal static class SystemClipboardTextAccess
{
    private const uint CfUnicodeText = 13;
    private const uint GmemMoveable = 0x0002;

    public static string? TryGetText()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        if (!TryOpenClipboard())
        {
            return null;
        }

        try
        {
            if (!IsClipboardFormatAvailable(CfUnicodeText))
            {
                return null;
            }

            var handle = GetClipboardData(CfUnicodeText);
            if (handle == IntPtr.Zero)
            {
                return null;
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                _ = GlobalUnlock(handle);
            }
        }
        finally
        {
            _ = CloseClipboard();
        }
    }

    public static bool TrySetText(string text)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        if (!TryOpenClipboard())
        {
            return false;
        }

        IntPtr handle = IntPtr.Zero;
        try
        {
            if (!EmptyClipboard())
            {
                return false;
            }

            var bytes = Encoding.Unicode.GetBytes(text + '\0');
            handle = GlobalAlloc(GmemMoveable, (UIntPtr)bytes.Length);
            if (handle == IntPtr.Zero)
            {
                return false;
            }

            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero)
            {
                return false;
            }

            try
            {
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
            }
            finally
            {
                _ = GlobalUnlock(handle);
            }

            if (SetClipboardData(CfUnicodeText, handle) == IntPtr.Zero)
            {
                return false;
            }

            handle = IntPtr.Zero;
            return true;
        }
        finally
        {
            if (handle != IntPtr.Zero)
            {
                _ = GlobalFree(handle);
            }

            _ = CloseClipboard();
        }
    }

    private static bool TryOpenClipboard()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (OpenClipboard(IntPtr.Zero))
            {
                return true;
            }

            Thread.Sleep(50);
        }

        return false;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);
}
