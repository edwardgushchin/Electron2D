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
using System.Diagnostics;
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
        Action<long> onTabCreated,
        Action<string> onConversationCreated,
        CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(options.TimeoutMinutes));
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
            await using var browser = await AuditSubmitCodexChromeClient.ConnectAsync(options, linked.Token).ConfigureAwait(false);
            return await RunOwnedTabOperationAsync(
                new AuditSubmitOwnedTabDriver(browser),
                onTabCreated,
                async tabId =>
                {
                    await PrepareProjectForPromptSubmissionAsync(
                        new AuditSubmitProjectPreparationDriver(browser, tabId),
                        options.ProjectUrl,
                        TimeSpan.FromMinutes(options.LoginTimeoutMinutes),
                        linked.Token).ConfigureAwait(false);
                    var messageCountBeforeSend = await SubmitPromptAsync(
                        new AuditSubmitPromptSubmissionDriver(browser, tabId),
                        [zipPath],
                        message,
                        linked.Token).ConfigureAwait(false);
                    await WaitForConversationMessagesAsync(browser, tabId, messageCountBeforeSend + 1, TimeSpan.FromMinutes(2), linked.Token).ConfigureAwait(false);
                    var conversationUrl = await WaitForConcreteConversationUrlAsync(browser, tabId, TimeSpan.FromSeconds(30), linked.Token).ConfigureAwait(false);
                    await WriteConversationUrlSidecarAsync(repoRoot, zipPath, conversationUrl, options.ControlAudit, linked.Token).ConfigureAwait(false);
                    onConversationCreated(conversationUrl);

                    var report = await WaitForOrdinaryChatReportAsync(browser, tabId, options, messageCountBeforeSend, linked.Token).ConfigureAwait(false);
                    return report;
                },
                linked.Token).ConfigureAwait(false);
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
            return await RunOwnedTabOperationAsync(
                new AuditSubmitOwnedTabDriver(browser),
                onTabCreated: null,
                async tabId =>
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
                    return report;
                },
                linked.Token).ConfigureAwait(false);
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

    private static async Task<string> RunOwnedTabOperationAsync(
        IAuditSubmitOwnedTabDriver driver,
        Action<long>? onTabCreated,
        Func<long, Task<string>> operation,
        CancellationToken cancellationToken)
    {
        try
        {
            var tabId = await driver.CreateTabAsync(cancellationToken).ConfigureAwait(false);
            onTabCreated?.Invoke(tabId);
            return await operation(tabId).ConfigureAwait(false);
        }
        finally
        {
            await driver.FinalizeTabsAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private interface IAuditSubmitOwnedTabDriver
    {
        Task<long> CreateTabAsync(CancellationToken cancellationToken);

        Task FinalizeTabsAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitOwnedTabDriver(AuditSubmitCodexChromeClient browser) : IAuditSubmitOwnedTabDriver
    {
        public Task<long> CreateTabAsync(CancellationToken cancellationToken)
        {
            return browser.CreateTabAsync(cancellationToken);
        }

        public Task FinalizeTabsAsync(CancellationToken cancellationToken)
        {
            return browser.FinalizeTabsAsync(cancellationToken);
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
        try
        {
            var tabId = await driver.CreateTabAsync(cancellationToken).ConfigureAwait(false);
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
            return summary;
        }
        finally
        {
            await driver.FinalizeTabsAsync(CancellationToken.None).ConfigureAwait(false);
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
        await InstallClipboardWriteCapturePreloadAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await EnableOopifAutoAttachAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
    }

    private static async Task InstallClipboardWriteCapturePreloadAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
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

    private static async Task PrepareProjectForPromptSubmissionAsync(
        IAuditSubmitProjectPreparationDriver driver,
        string projectUrl,
        TimeSpan loginTimeout,
        CancellationToken cancellationToken)
    {
        await driver.InitializeTabAsync(cancellationToken).ConfigureAwait(false);
        await driver.NavigateAsync(projectUrl, cancellationToken).ConfigureAwait(false);
        await driver.BringTabToFrontBestEffortAsync(cancellationToken).ConfigureAwait(false);
        await driver.WaitForComposerAsync(loginTimeout, cancellationToken).ConfigureAwait(false);
        await driver.WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
    }

    private interface IAuditSubmitProjectPreparationDriver
    {
        Task InitializeTabAsync(CancellationToken cancellationToken);

        Task NavigateAsync(string projectUrl, CancellationToken cancellationToken);

        Task BringTabToFrontBestEffortAsync(CancellationToken cancellationToken);

        Task WaitForComposerAsync(TimeSpan loginTimeout, CancellationToken cancellationToken);

        Task WaitForReportHydrationAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitProjectPreparationDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitProjectPreparationDriver
    {
        public async Task InitializeTabAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.InitializeTabAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
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
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            await driver.ClearPromptAsync(cancellationToken).ConfigureAwait(false);
        }

        await driver.AttachFilesAsync(zipPaths, message, cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(message))
        {
            await driver.FillPromptAsync(message, cancellationToken).ConfigureAwait(false);
        }

        await driver.RequirePromptPayloadReadyAsync(message, zipPaths, cancellationToken).ConfigureAwait(false);
        var messageCountBeforeSend = await driver.ReadConversationMessageCountAsync(cancellationToken).ConfigureAwait(false);
        await driver.ClickSendAsync(cancellationToken).ConfigureAwait(false);
        return messageCountBeforeSend;
    }

    private interface IAuditSubmitPromptSubmissionDriver
    {
        Task ClearPromptAsync(CancellationToken cancellationToken);

        Task AttachFilesAsync(string[] paths, string message, CancellationToken cancellationToken);

        Task FillPromptAsync(string message, CancellationToken cancellationToken);

        Task RequirePromptPayloadReadyAsync(string message, string[] paths, CancellationToken cancellationToken);

        Task<int> ReadConversationMessageCountAsync(CancellationToken cancellationToken);

        Task ClickSendAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitPromptSubmissionDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitPromptSubmissionDriver
    {
        public async Task ClearPromptAsync(CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.ClearPromptAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        }

        public async Task AttachFilesAsync(string[] paths, string message, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.AttachFilesAsync(
                browser,
                tabId,
                paths,
                message,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task FillPromptAsync(string message, CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.FillPromptAsync(browser, tabId, message, cancellationToken).ConfigureAwait(false);
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
                $"selectedFileCount={ReadProperty(status, "selectedFileCount")}",
                $"filenameMatchCount={ReadProperty(status, "filenameMatchCount")}",
                $"attachmentRootCount={ReadProperty(status, "attachmentRootCount")}",
                $"allAttachmentRootCount={ReadProperty(status, "allAttachmentRootCount")}"
            ]);
    }

    private static async Task AttachFilesAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string[] paths,
        string message,
        CancellationToken cancellationToken)
    {
        await AttachFilesAsync(
            new AuditSubmitAttachmentUploadDriver(browser, tabId),
            paths,
            message,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task AttachFilesAsync(
        IAuditSubmitAttachmentUploadDriver driver,
        string[] paths,
        string message,
        CancellationToken cancellationToken)
    {
        var expectedFileNames = paths.Select(path => Path.GetFileName(path)!).ToArray();
        var composerState = await driver.InspectComposerUploadStateAsync(
            message,
            expectedFileNames,
            cancellationToken).ConfigureAwait(false);
        if (composerState.Disposition == AuditSubmitComposerUploadDisposition.Ready)
        {
            return;
        }

        if (composerState.Disposition != AuditSubmitComposerUploadDisposition.UploadAllowed)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-COMPOSER-STATE",
                $"The ChatGPT composer is not safe for a new audit ZIP upload: {composerState.Reason}.");
        }

        var attachmentInput = await driver.QueryFileInputBackendNodeIdAsync(cancellationToken).ConfigureAwait(false);
        if (attachmentInput is null)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-ATTACHMENT-MISSING",
                "Could not find exactly one file attachment input in the ChatGPT composer.");
        }

        await driver.SetFileInputFilesAsync(
            attachmentInput.Value.BackendNodeId,
            paths,
            cancellationToken).ConfigureAwait(false);
        var committed = await driver.CommitAttachmentInputAsync(
            attachmentInput.Value.MarkerToken,
            expectedFileNames,
            cancellationToken).ConfigureAwait(false);
        if (!committed)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-ATTACHMENT-MISSING",
                "The selected ChatGPT composer input did not retain exactly the expected audit ZIP.");
        }

        await driver.WaitForAttachmentChipAsync(
            expectedFileNames,
            message,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AuditSubmitComposerUploadState> InspectComposerUploadStateAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string message,
        string[] expectedFileNames,
        CancellationToken cancellationToken)
    {
        var fileNamesJson = JsonSerializer.Serialize(expectedFileNames);
        var statuses = new List<JsonElement>();
        foreach (var candidateMessage in new[] { message, string.Empty }.Distinct(StringComparer.Ordinal))
        {
            var status = await browser.EvaluateAsync(
                    tabId,
                    PromptPayloadStatusExpression(JsonSerializer.Serialize(candidateMessage), fileNamesJson),
                    UiActionTimeout,
                    cancellationToken,
                    allowTransientRecovery: false).ConfigureAwait(false);
            statuses.Add(status);
            if (PromptPayloadStatusReady(status))
            {
                return new AuditSubmitComposerUploadState(AuditSubmitComposerUploadDisposition.Ready, "expected payload is already attached");
            }
        }

        var allowedStatus = statuses.FirstOrDefault(status =>
            (PromptPayloadStatusBool(status, "promptIsEmpty") ||
             PromptPayloadStatusBool(status, "promptHasExpectedMessage")) &&
            status.TryGetProperty("selectedFileCount", out _));
        if (allowedStatus.ValueKind != JsonValueKind.Object)
        {
            return new AuditSubmitComposerUploadState(
                AuditSubmitComposerUploadDisposition.Rejected,
                statuses.Count == 0 ? "composer state is unavailable" : DescribePromptPayloadStatus(statuses[0]));
        }

        var reason = PromptPayloadStatusString(allowedStatus, "reason");
        if (string.Equals(reason, "prompt-missing", StringComparison.Ordinal) ||
            string.Equals(reason, "unexpected-file-count", StringComparison.Ordinal))
        {
            return new AuditSubmitComposerUploadState(
                AuditSubmitComposerUploadDisposition.Rejected,
                DescribePromptPayloadStatus(allowedStatus));
        }

        if (PromptPayloadStatusInt32(allowedStatus, "selectedFileCount") > 0 ||
            PromptPayloadStatusInt32(allowedStatus, "allAttachmentRootCount") > 0 ||
            PromptPayloadStatusInt32(allowedStatus, "nearbyArchiveRootCount") > 0)
        {
            return new AuditSubmitComposerUploadState(
                AuditSubmitComposerUploadDisposition.Rejected,
                $"foreign or ambiguous attachment state; {DescribePromptPayloadStatus(allowedStatus)}");
        }

        return new AuditSubmitComposerUploadState(
            AuditSubmitComposerUploadDisposition.UploadAllowed,
            DescribePromptPayloadStatus(allowedStatus));
    }

    private static bool PromptPayloadStatusBool(JsonElement status, string name)
    {
        return status.ValueKind == JsonValueKind.Object &&
            status.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.True;
    }

    private static int PromptPayloadStatusInt32(JsonElement status, string name)
    {
        return status.ValueKind == JsonValueKind.Object &&
            status.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.Number &&
            value.TryGetInt32(out var result)
                ? result
                : 0;
    }

    private static string PromptPayloadStatusString(JsonElement status, string name)
    {
        return status.ValueKind == JsonValueKind.Object &&
            status.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;
    }

    private enum AuditSubmitComposerUploadDisposition
    {
        Ready,
        UploadAllowed,
        Rejected
    }

    private readonly record struct AuditSubmitComposerUploadState(
        AuditSubmitComposerUploadDisposition Disposition,
        string Reason);

    private interface IAuditSubmitAttachmentUploadDriver
    {
        Task<AuditSubmitComposerUploadState> InspectComposerUploadStateAsync(
            string message,
            string[] expectedFileNames,
            CancellationToken cancellationToken);

        Task<AuditSubmitAttachmentInputTarget?> QueryFileInputBackendNodeIdAsync(CancellationToken cancellationToken);

        Task SetFileInputFilesAsync(long backendNodeId, string[] paths, CancellationToken cancellationToken);

        Task<bool> CommitAttachmentInputAsync(
            string markerToken,
            string[] expectedFileNames,
            CancellationToken cancellationToken);

        Task WaitForAttachmentChipAsync(string[] expectedFileNames, string message, CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitAttachmentUploadDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitAttachmentUploadDriver
    {
        public async Task<AuditSubmitComposerUploadState> InspectComposerUploadStateAsync(
            string message,
            string[] expectedFileNames,
            CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.InspectComposerUploadStateAsync(
                browser,
                tabId,
                message,
                expectedFileNames,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuditSubmitAttachmentInputTarget?> QueryFileInputBackendNodeIdAsync(CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.QueryFileInputBackendNodeIdAsync(
                browser,
                tabId,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task SetFileInputFilesAsync(long backendNodeId, string[] paths, CancellationToken cancellationToken)
        {
            await browser.ExecuteCdpAsync(
                tabId,
                "DOM.setFileInputFiles",
                new Dictionary<string, object?>
                {
                    ["backendNodeId"] = backendNodeId,
                    ["files"] = paths
                },
                TimeSpan.FromMinutes(2),
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
        }

        public async Task<bool> CommitAttachmentInputAsync(
            string markerToken,
            string[] expectedFileNames,
            CancellationToken cancellationToken)
        {
            return await AuditSubmitCodexChromeAutomation.CommitAttachmentInputAsync(
                browser,
                tabId,
                markerToken,
                expectedFileNames,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task WaitForAttachmentChipAsync(
            string[] expectedFileNames,
            string message,
            CancellationToken cancellationToken)
        {
            await AuditSubmitCodexChromeAutomation.WaitForAttachmentChipAsync(
                browser,
                tabId,
                expectedFileNames,
                message,
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<AuditSubmitAttachmentInputTarget?> QueryFileInputBackendNodeIdAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var markerToken = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        var selected = await browser.EvaluateBoolAsync(
            tabId,
            AttachmentInputSelectionExpression(markerToken),
            UiActionTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        if (!selected)
        {
            return null;
        }

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
                ["selector"] = $"[{AttachmentInputMarkerAttribute}=\"{markerToken}\"]"
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
            ? new AuditSubmitAttachmentInputTarget(backendNodeId.GetInt64(), markerToken)
            : null;
    }

    private static async Task<bool> CommitAttachmentInputAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string markerToken,
        string[] expectedFileNames,
        CancellationToken cancellationToken)
    {
        return await browser.EvaluateBoolAsync(
            tabId,
            AttachmentInputCommitExpression(markerToken, expectedFileNames),
            UiActionTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
    }

    private static async Task WaitForAttachmentChipAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string[] expectedFileNames,
        string message,
        CancellationToken cancellationToken)
    {
        var fileNamesJson = JsonSerializer.Serialize(expectedFileNames);
        var expressions = new[] { message, string.Empty }
            .Distinct(StringComparer.Ordinal)
            .Select(candidateMessage => PromptPayloadReadyExpression(
                JsonSerializer.Serialize(candidateMessage),
                fileNamesJson))
            .ToArray();
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(90);
        while (DateTimeOffset.UtcNow < deadline)
        {
            foreach (var expression in expressions)
            {
                if (await browser.EvaluateBoolAsync(
                        tabId,
                        expression,
                        UiActionTimeout,
                        cancellationToken,
                        allowTransientRecovery: false).ConfigureAwait(false))
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-ATTACHMENT-MISSING",
            "The ChatGPT composer did not publish the selected audit ZIP attachment with either an empty or exact current prompt draft.");
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

    private static async Task ClearPromptAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var expression = FillPromptExpression(JsonSerializer.Serialize(string.Empty));
        var cleared = await browser.EvaluateBoolAsync(
            tabId,
            expression,
            UiActionTimeout,
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        if (!cleared)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-PROMPT-MISSING",
                "Could not clear the restored prompt draft before a ZIP-only reuse submission.");
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
        catch (AuditSubmitCodexChromeException ex) when (
            string.Equals(ex.Code, "E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE", StringComparison.Ordinal))
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
        var ordinaryCopyButtonMissing = false;
        var copySurfaceReloaded = false;
        DateTimeOffset ordinaryCopyButtonMissingFirstSeenUtc = default;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool isGenerating;
            try
            {
                isGenerating = await driver.IsGeneratingAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (AuditSubmitCodexChromeException ex) when (AuditSubmitCodexChromeClient.IsRecoverableCdpFailure(ex))
            {
                await driver.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
                continue;
            }

            string? clipboardMarkdown = null;
            if (!isGenerating)
            {
                var copyResult = await driver.CopyLatestAssistantMessageMarkdownAsync(minimumMessageCount, cancellationToken).ConfigureAwait(false);
                if (copyResult.Status == AuditSubmitOrdinaryCopyStatus.CopiedMarkdown &&
                    !string.IsNullOrWhiteSpace(copyResult.Markdown))
                {
                    clipboardMarkdown = copyResult.Markdown;
                    ordinaryCopyButtonMissing = false;
                    ordinaryCopyButtonMissingFirstSeenUtc = default;
                }
                else if (copyResult.Status == AuditSubmitOrdinaryCopyStatus.CopyActionUnavailable)
                {
                    if (!copySurfaceReloaded)
                    {
                        await driver.ReloadConversationAsync(minimumMessageCount, cancellationToken).ConfigureAwait(false);
                        copySurfaceReloaded = true;
                        ordinaryCopyButtonMissing = false;
                        ordinaryCopyButtonMissingFirstSeenUtc = default;
                        continue;
                    }

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
                    return extraction.Report;
                }

                throw new AuditSubmitCodexChromeException(
                    "E2D-BUILD-AUDIT-SUBMIT-REPORT-INVALID",
                    string.IsNullOrWhiteSpace(extraction.FailureReason)
                        ? "The copied ordinary ChatGPT assistant response does not match the strict final report contract."
                        : $"The copied ordinary ChatGPT assistant response does not match the strict final report contract: {extraction.FailureReason}");
            }

            await driver.DelayAsync(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private interface IAuditSubmitOrdinaryReportDriver
    {
        DateTimeOffset UtcNow { get; }

        Task<bool> IsGeneratingAsync(CancellationToken cancellationToken);

        Task<AuditSubmitOrdinaryCopyResult> CopyLatestAssistantMessageMarkdownAsync(int minimumMessageCount, CancellationToken cancellationToken);

        Task ReloadConversationAsync(int minimumMessageCount, CancellationToken cancellationToken);

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

        public async Task ReloadConversationAsync(int minimumMessageCount, CancellationToken cancellationToken)
        {
            await browser.ExecuteCdpAsync(
                tabId,
                "Page.reload",
                new Dictionary<string, object?>(),
                UiActionTimeout,
                cancellationToken).ConfigureAwait(false);
            await WaitForPageReadyAsync(browser, tabId, UiActionTimeout, cancellationToken).ConfigureAwait(false);
            await WaitForConversationMessagesAsync(
                browser,
                tabId,
                minimumMessageCount,
                TimeSpan.FromMinutes(2),
                cancellationToken).ConfigureAwait(false);
            await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
            await ScrollConversationToBottomAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
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
        return await CopyLatestAssistantMessageMarkdownAsync(
            new AuditSubmitOrdinaryCopyDriver(browser, tabId),
            minimumMessageCount,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AuditSubmitOrdinaryCopyResult> CopyLatestAssistantMessageMarkdownAsync(
        IAuditSubmitOrdinaryCopyDriver driver,
        int minimumMessageCount,
        CancellationToken cancellationToken)
    {
        await driver.RequireCaptureAsync(cancellationToken).ConfigureAwait(false);
        await driver.ResetCaptureAsync(cancellationToken).ConfigureAwait(false);
        var buttonState = await driver.ReadButtonStateAsync(minimumMessageCount, cancellationToken).ConfigureAwait(false);
        if (buttonState.Status == AuditSubmitAssistantCopyButtonStatus.NoCurrentAssistantYet)
        {
            return AuditSubmitOrdinaryCopyResult.NoCurrentAssistantYet();
        }

        if (buttonState.Status != AuditSubmitAssistantCopyButtonStatus.Ready || buttonState.Point is null)
        {
            return AuditSubmitOrdinaryCopyResult.CopyActionUnavailable();
        }

        var clipboardSequence = driver.GetClipboardSequenceNumber();
        await driver.ClickAsync(buttonState.Point.Value, cancellationToken).ConfigureAwait(false);
        await driver.DelayAsync(ClipboardSettleDelay, cancellationToken).ConfigureAwait(false);
        var systemClipboardText = await driver.ReadSystemClipboardTextAsync(
            clipboardSequence,
            cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(systemClipboardText))
        {
            return AuditSubmitOrdinaryCopyResult.CopiedMarkdown(systemClipboardText);
        }

        var clipboardText = await driver.ReadCapturedClipboardTextAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(clipboardText))
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE",
                "The single ordinary ChatGPT response copy action did not produce Markdown.");
        }

        return AuditSubmitOrdinaryCopyResult.CopiedMarkdown(clipboardText);
    }

    private interface IAuditSubmitOrdinaryCopyDriver
    {
        Task RequireCaptureAsync(CancellationToken cancellationToken);

        Task ResetCaptureAsync(CancellationToken cancellationToken);

        Task<AuditSubmitAssistantCopyButtonState> ReadButtonStateAsync(int minimumMessageCount, CancellationToken cancellationToken);

        uint? GetClipboardSequenceNumber();

        Task ClickAsync(AuditSubmitDomPoint point, CancellationToken cancellationToken);

        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);

        Task<string?> ReadSystemClipboardTextAsync(uint? previousSequenceNumber, CancellationToken cancellationToken);

        Task<string> ReadCapturedClipboardTextAsync(CancellationToken cancellationToken);
    }

    private sealed class AuditSubmitOrdinaryCopyDriver(
        AuditSubmitCodexChromeClient browser,
        long tabId) : IAuditSubmitOrdinaryCopyDriver
    {
        public Task RequireCaptureAsync(CancellationToken cancellationToken)
        {
            return RequireClipboardWriteCaptureAsync(browser, tabId, cancellationToken);
        }

        public Task ResetCaptureAsync(CancellationToken cancellationToken)
        {
            return ResetClipboardWriteCaptureAsync(browser, tabId, cancellationToken);
        }

        public async Task<AuditSubmitAssistantCopyButtonState> ReadButtonStateAsync(
            int minimumMessageCount,
            CancellationToken cancellationToken)
        {
            var value = await browser.EvaluateAsync(
                tabId,
                LastAssistantCopyButtonStateExpression(minimumMessageCount),
                UiActionTimeout,
                cancellationToken,
                allowTransientRecovery: false).ConfigureAwait(false);
            return ReadAssistantCopyButtonState(value);
        }

        public uint? GetClipboardSequenceNumber()
        {
            return SystemClipboardTextAccess.TryGetSequenceNumber();
        }

        public Task ClickAsync(AuditSubmitDomPoint point, CancellationToken cancellationToken)
        {
            return browser.ClickAtAsync(tabId, point, cancellationToken);
        }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }

        public Task<string?> ReadSystemClipboardTextAsync(
            uint? previousSequenceNumber,
            CancellationToken cancellationToken)
        {
            return ReadSystemClipboardTextAfterCopyAsync(previousSequenceNumber, cancellationToken);
        }

        public Task<string> ReadCapturedClipboardTextAsync(CancellationToken cancellationToken)
        {
            return AuditSubmitCodexChromeAutomation.ReadCapturedClipboardTextAsync(browser, tabId, cancellationToken);
        }
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

    private static async Task<string?> ReadSystemClipboardTextAfterCopyAsync(
        uint? previousSequenceNumber,
        CancellationToken cancellationToken)
    {
        if (previousSequenceNumber is null)
        {
            return null;
        }

        var deadline = DateTimeOffset.UtcNow + SystemClipboardReadTimeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var currentSequenceNumber = SystemClipboardTextAccess.TryGetSequenceNumber();
            if (currentSequenceNumber is not null &&
                currentSequenceNumber != previousSequenceNumber)
            {
                var text = SystemClipboardTextAccess.TryGetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            await Task.Delay(ClipboardSettleDelay, cancellationToken).ConfigureAwait(false);
        }

        return null;
    }

    private static async Task RequireClipboardWriteCaptureAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var ready = await browser.EvaluateBoolAsync(
            tabId,
            ClipboardWriteCaptureInstallExpression,
            TimeSpan.FromSeconds(10),
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        if (!ready)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE",
                "The source-scoped clipboard capture was not ready before the single ordinary copy action.");
        }
    }

    private static async Task ResetClipboardWriteCaptureAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var reset = await browser.EvaluateBoolAsync(
            tabId,
            ClipboardWriteCaptureResetExpression,
            TimeSpan.FromSeconds(10),
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        if (!reset)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-ORDINARY-COPY-UNAVAILABLE",
                "The source-scoped clipboard capture could not be reset before the single ordinary copy action.");
        }
    }

    private static async Task<string> ReadCapturedClipboardTextAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        CancellationToken cancellationToken)
    {
        var captured = await browser.EvaluateAsync(
            tabId,
            ClipboardCapturedWriteTextExpression,
            TimeSpan.FromSeconds(10),
            cancellationToken,
            allowTransientRecovery: false).ConfigureAwait(false);
        if (TryReadClipboardResult(captured, staleClipboardText: null, out var capturedText, out var capturedError))
        {
            return capturedText;
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CLIPBOARD-UNAVAILABLE",
            "The ordinary ChatGPT response copy action completed, but source-scoped captured Markdown was unavailable. " +
            $"Captured copy action Markdown: {capturedError}");
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

    private const string AttachmentInputMarkerAttribute = "data-electron2d-audit-attachment-input";

    private static string AttachmentInputSelectionExpression(string markerToken)
    {
        var markerAttributeJson = JsonSerializer.Serialize(AttachmentInputMarkerAttribute);
        var markerTokenJson = JsonSerializer.Serialize(markerToken);
        return $$"""
        (() => {
          const markerAttribute = {{markerAttributeJson}};
          const markerToken = {{markerTokenJson}};
          const registryKey = '__electron2dAuditAttachmentInputs';
          const previousRegistry = globalThis[registryKey];
          if (previousRegistry instanceof Map) {
            for (const previousInput of previousRegistry.values()) {
              previousInput?.removeAttribute?.(markerAttribute);
            }
          }
          const registry = new Map();
          globalThis[registryKey] = registry;
          const imageExtensions = new Set([
            '.apng', '.avif', '.bmp', '.gif', '.heic', '.heif', '.ico',
            '.jfif', '.jpeg', '.jpg', '.png', '.svg', '.tif', '.tiff', '.webp'
          ]);
          const isAuditFileInput = (element) => {
            if (!(element instanceof HTMLInputElement) || element.type !== 'file') return false;
            const acceptTokens = String(element.accept || element.getAttribute('accept') || '')
              .split(',')
              .map((token) => token.trim().toLowerCase())
              .filter(Boolean);
            const imageOnly = acceptTokens.length > 0 && acceptTokens.every((token) =>
              token.startsWith('image/') || imageExtensions.has(token));
            return !imageOnly;
          };
          const exact = document.querySelector('#upload-files');
          const promptSelectors = [
            '#prompt-textarea',
            '[data-testid="composer-text-input"]',
            'textarea',
            '[contenteditable="true"][role="textbox"]'
          ];
          const prompt = promptSelectors
            .map((selector) => document.querySelector(selector))
            .find((element) => element?.closest('form'));
          const composer = prompt?.closest('form');
          const fallbackInputs = composer
            ? Array.from(composer.querySelectorAll('input[type="file"]')).filter(isAuditFileInput)
            : [];
          const candidates = isAuditFileInput(exact)
            ? [exact]
            : fallbackInputs;
          if (candidates.length !== 1) return false;
          const input = candidates[0];

          for (const previous of document.querySelectorAll(`[${markerAttribute}]`)) {
            previous.removeAttribute(markerAttribute);
          }
          input.setAttribute(markerAttribute, markerToken);
          registry.set(markerToken, input);
          return true;
        })()
        """;
    }

    private static string AttachmentInputCommitExpression(string markerToken, string[] expectedFileNames)
    {
        var markerTokenJson = JsonSerializer.Serialize(markerToken);
        var expectedFileNamesJson = JsonSerializer.Serialize(expectedFileNames);
        return $$"""
        (() => {
          const markerToken = {{markerTokenJson}};
          const expectedFileNames = {{expectedFileNamesJson}};
          const registry = globalThis.__electron2dAuditAttachmentInputs;
          const input = registry instanceof Map ? registry.get(markerToken) : null;
          const cleanup = () => {
            input?.removeAttribute?.({{JsonSerializer.Serialize(AttachmentInputMarkerAttribute)}});
            registry?.delete?.(markerToken);
          };
          if (!(input instanceof HTMLInputElement) || input.type !== 'file' || !input.isConnected) {
            cleanup();
            return false;
          }
          const actualFileNames = Array.from(input.files || [], (file) => file.name);
          if (actualFileNames.length !== expectedFileNames.length ||
              actualFileNames.some((name, index) => name !== expectedFileNames[index])) {
            cleanup();
            return false;
          }

          cleanup();
          return true;
        })()
        """;
    }

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
          const normalizePrompt = (value) => String(value || '').replace(/\s+/g, ' ').trim();
          const normalizeFileName = (value) => String(value || '').trim();
          const normalizeMetadata = (value) => String(value || '').replace(/\s+/g, ' ').trim().toLowerCase();
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

          const expectedMessage = normalizePrompt(message);
          const promptText = typeof prompt.value === 'string'
            ? prompt.value
            : (typeof prompt.innerText === 'string' && prompt.innerText.length > 0
              ? prompt.innerText
              : (prompt.textContent || ''));
          const normalizedPromptText = normalizePrompt(promptText);
          const expectsMessage = expectedMessage.length > 0;
          const promptHasExpectedMessage = expectsMessage && normalizedPromptText === expectedMessage;
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
            ? fileNames.map(normalizeFileName).filter(Boolean)
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

          const composerRoot = prompt.closest('form') || prompt.parentElement?.parentElement || prompt.parentElement;
          const composerFileInputs = composerRoot && typeof composerRoot.querySelectorAll === 'function'
            ? Array.from(composerRoot.querySelectorAll('input[type="file"]'))
            : [];
          const selectedFileCount = composerFileInputs.reduce(
            (count, input) => count + Array.from(input.files || []).length,
            0);
          const inputFilesMatch = composerFileInputs.some((input) => {
            const selectedFiles = Array.from(input.files || [], (file) => normalizeFileName(file.name));
            return selectedFiles.length === expectedFiles.length &&
              selectedFiles.every((fileName, index) => fileName === expectedFiles[index]);
          });

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
          const textOf = (element) => normalizeMetadata([
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
          const metadataOf = (element) => normalizeMetadata([
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
          const archiveTypeWords = ['zip-архив', 'zip архив', 'zip archive', 'zip file', 'application/zip'];
          const candidateElements = Array
            .from(document.querySelectorAll('button,[role="button"],div,span,li,[aria-label],[title],[data-testid]'))
            .filter((element) => {
              if (element === prompt || prompt.contains(element)) return false;
              return visible(element) && nearPrompt(element) && !isHistoryOrMessage(element);
            });
          const fileNameValuesOf = (element) => [
            element.getAttribute('data-file-name') || '',
            element.getAttribute('data-filename') || '',
            element.getAttribute('aria-label') || '',
            element.getAttribute('title') || '',
            element.innerText || '',
            element.textContent || ''
          ].map(normalizeFileName).filter(Boolean);
          const hasFileNameEvidence = (element) =>
            fileNameValuesOf(element).some((value) => /^[^\\/\r\n]+\.[a-z0-9]{1,16}$/i.test(value)) ||
            /(?:^|\s)[^\s\\/]+\.[a-z0-9]{1,16}(?:\s|$)/i.test(textOf(element));
          const filenameMatches = candidateElements.filter((element) =>
            expectedFiles.some((fileName) => fileNameValuesOf(element).some((value) => value === fileName)));
          const archiveFilenameMatches = candidateElements.filter((element) =>
            fileNameValuesOf(element).some((value) => /^[^\\/\r\n]+\.zip$/i.test(value)));
          const hasUploadedArchiveEvidence = (element) => {
            const text = textOf(element);
            return archiveFilenameMatches.some((match) => element === match || element.contains(match)) &&
              archiveTypeWords.some((word) => text.includes(word));
          };
          const attachmentRootFor = (element) => {
            let current = element;
            for (let depth = 0; current && depth < 5; depth++) {
              if (current !== prompt && !prompt.contains(current) &&
                  (hasAttachmentMetadata(current) || hasUploadedArchiveEvidence(current))) {
                return current;
              }

              current = current.parentElement;
            }

            return inputFilesMatch ? element : null;
          };
          const rootsFor = (matches) => {
            const candidates = matches
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
            return uniqueCandidates.filter((element) =>
              !uniqueCandidates.some((candidate) => candidate !== element && candidate.contains(element)));
          };
          const roots = rootsFor(filenameMatches);
          const archiveRoots = rootsFor(archiveFilenameMatches);
          const allAttachmentRoots = rootsFor(candidateElements.filter((element) =>
            hasFileNameEvidence(element) &&
            (hasAttachmentMetadata(element) || hasUploadedArchiveEvidence(element))));
          const ready = roots.length === 1 &&
            archiveRoots.length === 1 &&
            roots[0] === archiveRoots[0];
          return {
            ready,
            reason: ready
              ? 'ready'
              : (roots.length === 0
                ? 'attachment-root-missing'
                : (archiveRoots.length !== 1 ? 'attachment-root-ambiguous' : 'attachment-root-mismatch')),
            promptFound: true,
            promptHasExpectedMessage: !expectsMessage || promptHasExpectedMessage,
            promptIsEmpty,
            expectedFileCount: expectedFiles.length,
            inputFilesMatch,
            selectedFileCount,
            filenameMatchCount: filenameMatches.length,
            attachmentRootCount: roots.length,
            nearbyArchiveRootCount: archiveRoots.length,
            allAttachmentRootCount: allAttachmentRoots.length
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
            return element.getAttribute('data-testid') === 'copy-turn-action-button';
          };
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
          const assistantTurn = assistant.closest('[data-turn="assistant"]') ||
            assistant.closest('[data-testid^="conversation-turn-"]') ||
            assistant.closest('[data-testid*="conversation-turn"]') ||
            assistant.closest('article');
          if (!assistantTurn) {
            {{copyButtonMissingResultScript}}
          }

          const ownedAssistants = Array
            .from(assistantTurn.querySelectorAll('[data-message-author-role="assistant"]'))
            .filter(visible);
          if (ownedAssistants.length !== 1 || ownedAssistants[0] !== assistant) {
            {{copyButtonMissingResultScript}}
          }

          const candidates = Array
            .from(assistantTurn.querySelectorAll('button[data-testid="copy-turn-action-button"]'))
            .filter((button) => visible(button) && isCopyButton(button))
            .filter((button) => {
              const owner = button.closest('[data-turn="assistant"]') ||
                button.closest('[data-testid^="conversation-turn-"]') ||
                button.closest('[data-testid*="conversation-turn"]') ||
                button.closest('article');
              return owner === assistantTurn;
            });
          if (candidates.length !== 1) {
            {{copyButtonMissingResultScript}}
          }
          const button = candidates[0];

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

    private const string ClipboardWriteCapturePreloadExpression =
        """
        (() => {
          const key = '__electron2dAuditClipboardPreloadCapture';
          const current = window[key];
          if (current && current.installed) {
            current.text = null;
            current.error = null;
            current.pending = null;
            return current.captureReady === true;
          }

          const state = { installed: true, captureReady: false, text: null, error: null, pending: null, dataTransferSetDataPatched: false };
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

              state.captureReady = patched;
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

            state.captureReady = patched;
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
          const preload = window.__electron2dAuditClipboardPreloadCapture;
          if (!preload || !preload.installed || preload.captureReady !== true) {
            return false;
          }

          const key = '__electron2dAuditClipboardWriteCapture';
          const current = window[key];
          if (current && current.installed && current.copyEventInstalled) {
            current.text = null;
            current.error = null;
            current.pending = null;
            return Boolean(current.dataTransferSetDataPatched || current.originalWriteText || current.originalWrite);
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

            return Boolean(
              state.copyEventInstalled &&
              (state.dataTransferSetDataPatched || originalWriteText || originalWrite));
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
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-TAB-FINALIZATION",
                "Codex Chrome Extension pipe disconnected before the owned audit tab could be finalized.");
        }

        await RequestAsync(
            "finalizeTabs",
            new Dictionary<string, object?> { ["keep"] = Array.Empty<object>() },
            TimeSpan.FromSeconds(10),
            cancellationToken).ConfigureAwait(false);
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

                var successfulDomainEnableCount = 0;
                foreach (var method in new[] { "Runtime.enable", "DOM.enable", "Page.enable" })
                {
                    try
                    {
                        await ExecuteCdpCoreAsync(tabId, method, [], TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                        successfulDomainEnableCount++;
                    }
                    catch (AuditSubmitCodexChromeException ex) when (IsRecoverableCdpFailure(ex))
                    {
                        failures.Add(ex.Message);
                    }
                }

                if (successfulDomainEnableCount > 0)
                {
                    return;
                }
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

    internal static bool IsRecoverableCdpFailure(AuditSubmitCodexChromeException exception)
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

internal readonly record struct AuditSubmitAttachmentInputTarget(long BackendNodeId, string MarkerToken);

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

    public static uint? TryGetSequenceNumber()
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        var sequenceNumber = GetClipboardSequenceNumber();
        return sequenceNumber == 0 ? null : sequenceNumber;
    }

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

    [DllImport("user32.dll")]
    private static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsClipboardFormatAvailable(uint format);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr hMem);

}
