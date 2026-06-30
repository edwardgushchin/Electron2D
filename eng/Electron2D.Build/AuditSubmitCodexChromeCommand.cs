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
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Electron2D.Build;

internal sealed class AuditSubmitCodexChromeAutomation
{
    private static readonly TimeSpan UiActionTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan ReportHydrationDelay = TimeSpan.FromSeconds(10);
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
            var screenshots = AuditSubmitCodexChromeScreenshotRecorder.Create(repoRoot, options.ScreenshotsDirectory);
            var downloadsDirectory = CreateDownloadsDirectory(repoRoot, options);
            var tabId = await browser.CreateTabAsync(linked.Token).ConfigureAwait(false);
            var completed = false;
            try
            {
                await InitializeTabAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                var downloadDirectoryConfigured = await ConfigureDownloadsAsync(browser, tabId, downloadsDirectory, linked.Token).ConfigureAwait(false);
                await NavigateAsync(browser, tabId, options.ProjectUrl, linked.Token).ConfigureAwait(false);
                await BringTabToFrontBestEffortAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "open-project", linked.Token).ConfigureAwait(false);
                await WaitForComposerAsync(browser, tabId, TimeSpan.FromMinutes(options.LoginTimeoutMinutes), linked.Token).ConfigureAwait(false);
                await ScrollConversationToBottomAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                var ignoredDeepResearchTargetIds = await SnapshotDeepResearchTargetIdsAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "composer-ready", linked.Token).ConfigureAwait(false);
                await EnableDeepResearchAsync(browser, tabId, screenshots, linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "deep-research", linked.Token).ConfigureAwait(false);
                await AttachFilesAsync(browser, tabId, [zipPath], linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "files-attached", linked.Token).ConfigureAwait(false);
                await FillPromptAsync(browser, tabId, message, linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "prompt-filled", linked.Token).ConfigureAwait(false);
                var messageCountBeforeSend = await ReadConversationMessageCountAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await ClickSendAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "sent", linked.Token).ConfigureAwait(false);
                await WaitForConversationMessagesAsync(browser, tabId, messageCountBeforeSend + 1, TimeSpan.FromMinutes(2), linked.Token).ConfigureAwait(false);
                var conversationUrl = await WaitForConcreteConversationUrlAsync(browser, tabId, TimeSpan.FromSeconds(30), linked.Token).ConfigureAwait(false);
                await WriteConversationUrlSidecarAsync(repoRoot, zipPath, conversationUrl, linked.Token).ConfigureAwait(false);

                var report = await WaitForReportAsync(browser, tabId, options, screenshots, downloadsDirectory, includeUserDownloadsFallback: !downloadDirectoryConfigured, ignoredDeepResearchTargetIds, linked.Token).ConfigureAwait(false);
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
            var screenshots = AuditSubmitCodexChromeScreenshotRecorder.Create(repoRoot, options.ScreenshotsDirectory);
            var downloadsDirectory = CreateDownloadsDirectory(repoRoot, options);
            var tabId = await browser.CreateTabAsync(linked.Token).ConfigureAwait(false);
            var completed = false;
            try
            {
                await InitializeTabAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                _ = await ConfigureDownloadsAsync(browser, tabId, downloadsDirectory, linked.Token).ConfigureAwait(false);
                var ignoredDeepResearchTargetIds = await SnapshotDeepResearchTargetIdsAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await NavigateAsync(browser, tabId, options.ProjectUrl, linked.Token).ConfigureAwait(false);
                await BringTabToFrontBestEffortAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                await ScrollConversationToBottomAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                _ = await WaitForDeepResearchFrameContentAsync(browser, tabId, screenshots, TimeSpan.FromSeconds(90), linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "open-report-page-bottom", linked.Token).ConfigureAwait(false);

                var report = await DownloadReadyReportAsync(browser, tabId, options, screenshots, downloadsDirectory, includeUserDownloadsFallback: true, ignoredDeepResearchTargetIds, linked.Token).ConfigureAwait(false);
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
            var screenshots = AuditSubmitCodexChromeScreenshotRecorder.Create(repoRoot, options.ScreenshotsDirectory);
            var dumpDirectory = ResolveDomDumpDirectory(repoRoot, options);
            var tabId = await browser.CreateTabAsync(linked.Token).ConfigureAwait(false);
            var completed = false;
            try
            {
                await InitializeTabAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await NavigateAsync(browser, tabId, options.ProjectUrl, linked.Token).ConfigureAwait(false);
                await BringTabToFrontBestEffortAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                await ScrollConversationToBottomAsync(browser, tabId, linked.Token).ConfigureAwait(false);
                await WaitForReportHydrationAsync(linked.Token).ConfigureAwait(false);
                _ = await WaitForDeepResearchFrameContentAsync(browser, tabId, screenshots, TimeSpan.FromSeconds(90), linked.Token).ConfigureAwait(false);
                await screenshots.CaptureAsync(browser, tabId, "dom-dump-page-bottom", linked.Token).ConfigureAwait(false);

                var frameTree = await browser.ExecuteCdpAsync(tabId, "Page.getFrameTree", EmptyObject(), TimeSpan.FromSeconds(15), linked.Token).ConfigureAwait(false);
                await WriteJsonFileAsync(Path.Combine(dumpDirectory, "frame-tree.json"), frameTree, linked.Token).ConfigureAwait(false);
                var targetInfo = await browser.ExecuteCdpAsync(tabId, "Target.getTargets", EmptyObject(), TimeSpan.FromSeconds(15), linked.Token).ConfigureAwait(false);
                await WriteJsonFileAsync(Path.Combine(dumpDirectory, "target-info.json"), targetInfo, linked.Token).ConfigureAwait(false);
                JsonElement? accessibilityTree = null;
                try
                {
                    accessibilityTree = await browser.ExecuteCdpAsync(tabId, "Accessibility.getFullAXTree", EmptyObject(), TimeSpan.FromSeconds(30), linked.Token).ConfigureAwait(false);
                    await WriteJsonFileAsync(Path.Combine(dumpDirectory, "accessibility-tree.json"), accessibilityTree.Value, linked.Token).ConfigureAwait(false);
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

                await DumpDeepResearchTargetsAsync(browser, tabId, dumpDirectory, targets, summaries, linked.Token).ConfigureAwait(false);

                var index = 0;
                foreach (var entry in entries)
                {
                    linked.Token.ThrowIfCancellationRequested();
                    index++;
                    var prefix = $"{index:00}-{SanitizeDumpFileName(entry.Url)}";
                    try
                    {
                        JsonElement dump;
                        if (entry.IsRoot)
                        {
                            dump = await browser.EvaluateAsync(tabId, DomDumpExpression, TimeSpan.FromSeconds(45), linked.Token).ConfigureAwait(false);
                        }
                        else
                        {
                            var contextId = await CreateFrameExecutionContextAsync(browser, tabId, entry.FrameId, linked.Token).ConfigureAwait(false);
                            dump = await browser.EvaluateInContextAsync(tabId, contextId, DomDumpExpression, TimeSpan.FromSeconds(45), linked.Token).ConfigureAwait(false);
                        }

                        await WriteDomDumpFilesAsync(dumpDirectory, prefix, entry, dump, linked.Token).ConfigureAwait(false);
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
                await File.WriteAllTextAsync(Path.Combine(dumpDirectory, "summary.txt"), summary + Environment.NewLine, Encoding.UTF8, linked.Token).ConfigureAwait(false);
                completed = true;
                return summary;
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

    private static async Task InitializeTabAsync(AuditSubmitCodexChromeClient browser, long tabId, CancellationToken cancellationToken)
    {
        await browser.AttachAsync(tabId, cancellationToken).ConfigureAwait(false);
        await BringTabToFrontBestEffortAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await browser.ExecuteCdpAsync(tabId, "Page.enable", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
        await browser.ExecuteCdpAsync(tabId, "Runtime.enable", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
        await browser.ExecuteCdpAsync(tabId, "DOM.enable", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
        await EnableOopifAutoAttachAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
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

    private static string CreateDownloadsDirectory(string repoRoot, AuditSubmitOptions options)
    {
        var baseDirectory = string.IsNullOrWhiteSpace(options.ScreenshotsDirectory)
            ? Path.Combine(repoRoot, ".temp", "audit-submit-downloads")
            : Path.Combine(ResolvePath(repoRoot, options.ScreenshotsDirectory), "downloads");
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
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(20);
        var menuScreenshotCaptured = false;
        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await browser.EvaluateBoolAsync(tabId, DeepResearchSelectedExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
            {
                return;
            }

            var menuPoint = await browser.EvaluatePointAsync(tabId, DeepResearchMenuPointExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
            if (menuPoint is null)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
                continue;
            }

            await browser.ClickAtAsync(tabId, menuPoint.Value, cancellationToken).ConfigureAwait(false);
            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken).ConfigureAwait(false);
            if (!menuScreenshotCaptured)
            {
                await screenshots.CaptureAsync(browser, tabId, "deep-research-menu", cancellationToken).ConfigureAwait(false);
                menuScreenshotCaptured = true;
            }

            var itemPoint = await browser.EvaluatePointAsync(tabId, DeepResearchItemPointExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false).ConfigureAwait(false);
            if (itemPoint is not null)
            {
                await browser.ClickAtAsync(tabId, itemPoint.Value, cancellationToken).ConfigureAwait(false);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        }

        if (await browser.EvaluateBoolAsync(tabId, DeepResearchSelectedExpression, UiActionTimeout, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-DEEP-RESEARCH-MISSING",
            "Could not select the Deep Research control from the ChatGPT composer plus menu.");
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
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(options.PollSeconds);
        var poll = 1;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _ = await DismissRateLimitDialogAsync(browser, tabId, screenshots, cancellationToken).ConfigureAwait(false);
            var decision = await CapturePollingDecisionAsync(browser, tabId, screenshots, downloadsDirectory, includeUserDownloadsFallback, ignoredDeepResearchTargetIds, cancellationToken).ConfigureAwait(false);
            if (decision.Action == AuditSubmitPollingAction.ReturnReport && decision.Report is not null)
            {
                await screenshots.CaptureAsync(browser, tabId, "report-ready", cancellationToken).ConfigureAwait(false);
                return decision.Report;
            }

            await screenshots.CaptureAsync(browser, tabId, AuditSubmitPollingPolicy.CreateScreenshotName(decision, poll), cancellationToken).ConfigureAwait(false);
            if (options.DownloadReportOnly && decision.Action == AuditSubmitPollingAction.Reload)
            {
                throw new AuditSubmitCodexChromeException(
                    "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING",
                    "The ready report page did not expose a unique Deep Research export button or Markdown blob. See the saved screenshots for the page state.");
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            if (options.DownloadReportOnly)
            {
                poll++;
                continue;
            }

            await browser.ExecuteCdpAsync(tabId, "Page.reload", EmptyObject(), UiActionTimeout, cancellationToken).ConfigureAwait(false);
            await WaitForPageReadyAsync(browser, tabId, UiActionTimeout, cancellationToken).ConfigureAwait(false);
            await WaitForConversationMessagesAsync(browser, tabId, minimumMessageCount: 1, timeout: TimeSpan.FromMinutes(1), cancellationToken: cancellationToken).ConfigureAwait(false);
            await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
            _ = await DismissRateLimitDialogAsync(browser, tabId, screenshots, cancellationToken).ConfigureAwait(false);
            await screenshots.CaptureAsync(browser, tabId, $"reloaded-{poll:000}", cancellationToken).ConfigureAwait(false);
            poll++;
        }
    }

    private static async Task<string> DownloadReadyReportAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitOptions options,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        await ScrollConversationToBottomAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
        _ = await DismissRateLimitDialogAsync(browser, tabId, screenshots, cancellationToken).ConfigureAwait(false);

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
            _ = await DismissRateLimitDialogAsync(browser, tabId, screenshots, cancellationToken).ConfigureAwait(false);
        }

        var candidates = await DownloadReportCandidatesAsync(
            browser,
            tabId,
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback: true,
            ignoredDeepResearchTargetIds,
            cancellationToken).ConfigureAwait(false);
        if (candidates.Length == 0)
        {
            await screenshots.CaptureAsync(browser, tabId, "report-download-missing", cancellationToken).ConfigureAwait(false);
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-MISSING",
                "The ready report page did not produce a Markdown export in one deterministic download-report-only attempt.");
        }

        var report = ExtractDownloadedReportOrThrow(candidates);
        await screenshots.CaptureAsync(browser, tabId, "report-ready", cancellationToken).ConfigureAwait(false);
        return report;
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

    private static async Task<AuditSubmitPollingDecision> CapturePollingDecisionAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
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
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback,
            ignoredDeepResearchTargetIds,
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
        var path = Path.Combine(directory, $"conversation-url-{iteration}.txt");
        await File.WriteAllTextAsync(
            path,
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
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        var diagnosticCaptured = false;
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

            if (!diagnosticCaptured)
            {
                diagnosticCaptured = true;
                await screenshots.CaptureAsync(browser, tabId, "deep-research-iframe-waiting", cancellationToken).ConfigureAwait(false);
            }

            return true;
        }

        return false;
    }

    private static async Task<AuditSubmitReportCandidate[]> DownloadReportCandidatesAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        _ = await DismissRateLimitDialogAsync(browser, tabId, screenshots, cancellationToken).ConfigureAwait(false);
        await ScrollConversationToBottomAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        await WaitForReportHydrationAsync(cancellationToken).ConfigureAwait(false);
        _ = await DismissRateLimitDialogAsync(browser, tabId, screenshots, cancellationToken).ConfigureAwait(false);

        var frameResult = await DownloadReportCandidatesFromDeepResearchFrameAsync(
            browser,
            tabId,
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback,
            cancellationToken).ConfigureAwait(false);
        if (frameResult.SurfaceSelected)
        {
            return frameResult.Candidates;
        }

        var targetResult = await DownloadReportCandidatesFromDeepResearchTargetAsync(
            browser,
            tabId,
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback,
            ignoredDeepResearchTargetIds,
            cancellationToken).ConfigureAwait(false);
        if (targetResult.SurfaceSelected)
        {
            return targetResult.Candidates;
        }

        return await ClickReportExportAndReadDownloadedMarkdownAsync(
            browser,
            tabId,
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback,
            AuditSubmitExportSurfaceScope.Page,
            () => browser.EvaluateBoolAsync(tabId, ReportExportButtonClickExpression, UiActionTimeout, cancellationToken),
            () => Task.FromResult(false),
            () => browser.EvaluateBoolAsync(tabId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken),
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<AuditSubmitReportCandidate[]> ClickReportExportAndReadDownloadedMarkdownAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
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
        var clickedMarkdown = await ClickMarkdownMenuItemAsync(surfaceScope, clickScopedMarkdownMenuItemAsync, clickPageMarkdownMenuItemAsync).ConfigureAwait(false);
        if (!clickedMarkdown)
        {
            if (!await clickExportButtonAsync().ConfigureAwait(false))
            {
                await screenshots.CaptureAsync(browser, tabId, "report-export-point-missing", cancellationToken).ConfigureAwait(false);
                return [];
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
            await screenshots.CaptureAsync(browser, tabId, "report-export-menu", cancellationToken).ConfigureAwait(false);
            clickedMarkdown = await ClickMarkdownMenuItemAsync(surfaceScope, clickScopedMarkdownMenuItemAsync, clickPageMarkdownMenuItemAsync).ConfigureAwait(false);
        }

        if (!clickedMarkdown)
        {
            return [];
        }

        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken).ConfigureAwait(false);
        await screenshots.CaptureAsync(browser, tabId, "report-markdown-clicked", cancellationToken).ConfigureAwait(false);

        var reportPath = await WaitForMarkdownDownloadAsync(observedDownloadDirectories, acceptedDownloadDirectories, knownFiles, TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
        var report = string.IsNullOrWhiteSpace(reportPath)
            ? string.Empty
            : await File.ReadAllTextAsync(reportPath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(report)
            ? []
            : [new AuditSubmitReportCandidate(report, AuditSubmitReportCandidateSource.OpenedReportCard)];
    }

    private static async Task<bool> ClickMarkdownMenuItemAsync(
        AuditSubmitExportSurfaceScope surfaceScope,
        Func<Task<bool>> clickScopedMarkdownMenuItemAsync,
        Func<Task<bool>> clickPageMarkdownMenuItemAsync)
    {
        if (ResolveMarkdownMenuItemClickResult(surfaceScope, scopedMenuItemClicked: await clickScopedMarkdownMenuItemAsync().ConfigureAwait(false), pageMenuItemClicked: false))
        {
            return true;
        }

        if (!CanUsePageLevelMarkdownMenu(surfaceScope))
        {
            return false;
        }

        return ResolveMarkdownMenuItemClickResult(surfaceScope, scopedMenuItemClicked: false, pageMenuItemClicked: await clickPageMarkdownMenuItemAsync().ConfigureAwait(false));
    }

    private static bool ResolveMarkdownMenuItemClickResult(AuditSubmitExportSurfaceScope surfaceScope, bool scopedMenuItemClicked, bool pageMenuItemClicked)
    {
        return scopedMenuItemClicked || (CanUsePageLevelMarkdownMenu(surfaceScope) && pageMenuItemClicked);
    }

    private static bool CanUsePageLevelMarkdownMenu(AuditSubmitExportSurfaceScope surfaceScope)
    {
        return surfaceScope == AuditSubmitExportSurfaceScope.Page;
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromDeepResearchFrameAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        CancellationToken cancellationToken)
    {
        var hasVisibleIframe = await browser.EvaluateBoolAsync(tabId, DeepResearchIframeVisibleExpression, TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
        if (!hasVisibleIframe)
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        var frame = await TryCreateDeepResearchFrameContextAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        if (!CanUseDeepResearchFrameSurface(hasVisibleIframe, frame is not null) || frame is null)
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        var candidates = await ClickReportExportAndReadDownloadedMarkdownAsync(
            browser,
            tabId,
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback,
            AuditSubmitExportSurfaceScope.DeepResearchFrame,
            () => browser.EvaluateBoolInContextAsync(tabId, frame.Value.ContextId, ReportExportButtonClickExpression, TimeSpan.FromSeconds(10), cancellationToken),
            () => browser.EvaluateBoolInContextAsync(tabId, frame.Value.ContextId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken),
            () => Task.FromResult(false),
            cancellationToken).ConfigureAwait(false);
        return new AuditSubmitReportCandidateResult(true, candidates);
    }

    private static bool CanUseDeepResearchFrameSurface(bool hasVisibleIframe, bool hasFrameContext)
    {
        return hasVisibleIframe && hasFrameContext;
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromDeepResearchTargetAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        string downloadsDirectory,
        bool includeUserDownloadsFallback,
        IReadOnlySet<string> ignoredDeepResearchTargetIds,
        CancellationToken cancellationToken)
    {
        var targetId = await TryFindSingleReadyDeepResearchTargetIdAsync(browser, tabId, ignoredDeepResearchTargetIds, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return AuditSubmitReportCandidateResult.NoSurface;
        }

        await browser.AttachTargetWithRecoveryAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
        await browser.EnsureTargetAttachedForReadAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
        try
        {
            await InitializeDeepResearchTargetAsync(browser, tabId, targetId, cancellationToken).ConfigureAwait(false);

            var frameResult = await DownloadReportCandidatesFromDeepResearchTargetFrameAsync(
                browser,
                tabId,
                targetId,
                screenshots,
                downloadsDirectory,
                includeUserDownloadsFallback,
                cancellationToken).ConfigureAwait(false);
            if (frameResult.SurfaceSelected)
            {
                return frameResult;
            }

            var candidates = await ClickReportExportAndReadDownloadedMarkdownAsync(
                browser,
                tabId,
                screenshots,
                downloadsDirectory,
                includeUserDownloadsFallback,
                AuditSubmitExportSurfaceScope.DeepResearchTarget,
                () => browser.EvaluateBoolOnTargetAsync(tabId, targetId, ReportExportButtonClickExpression, TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false),
                () => browser.EvaluateBoolOnTargetAsync(tabId, targetId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false),
                () => Task.FromResult(false),
                cancellationToken).ConfigureAwait(false);
            return new AuditSubmitReportCandidateResult(true, candidates);
        }
        finally
        {
            await browser.DetachTargetAsync(tabId, targetId, CancellationToken.None).ConfigureAwait(false);
        }
    }

    private static async Task<AuditSubmitReportCandidateResult> DownloadReportCandidatesFromDeepResearchTargetFrameAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string targetId,
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
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
        var candidates = await ClickReportExportAndReadDownloadedMarkdownAsync(
            browser,
            tabId,
            screenshots,
            downloadsDirectory,
            includeUserDownloadsFallback,
            AuditSubmitExportSurfaceScope.DeepResearchTargetFrame,
            () => browser.EvaluateBoolInContextOnTargetAsync(tabId, targetId, context.Value.ContextId, ReportExportButtonClickExpression, TimeSpan.FromSeconds(10), cancellationToken, allowTransientRecovery: false),
            () => browser.EvaluateBoolInContextOnTargetAsync(tabId, targetId, context.Value.ContextId, ExportReportMarkdownMenuItemClickExpression, UiActionTimeout, cancellationToken, allowTransientRecovery: false),
            () => Task.FromResult(false),
            cancellationToken).ConfigureAwait(false);
        return new AuditSubmitReportCandidateResult(true, candidates);
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
        CancellationToken cancellationToken)
    {
        var targets = await ReadDeepResearchTargetsAsync(browser, tabId, cancellationToken).ConfigureAwait(false);
        var readyTargetIds = new List<string>();
        foreach (var target in targets)
        {
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

        if (readyTargetIds.Count == 0)
        {
            return null;
        }

        if (readyTargetIds.Count > 1)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-REPORT-EXPORT-AMBIGUOUS",
                "The page exposed multiple ready Deep Research targets after the current-submit baseline.");
        }

        return readyTargetIds[0];
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
        AuditSubmitCodexChromeScreenshotRecorder screenshots,
        CancellationToken cancellationToken)
    {
        var point = await browser.EvaluatePointAsync(tabId, RateLimitDialogDismissPointExpression, TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
        if (point is null)
        {
            return false;
        }

        await screenshots.CaptureAsync(browser, tabId, "rate-limit-dialog", cancellationToken).ConfigureAwait(false);
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
          const hasExportButton = documents()
            .flatMap((doc) => Array.from(doc === document ? rootButtons : doc.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]')))
            .some((button) => {
              const label = normalize(`${button.getAttribute('aria-label') || ''}\n${button.getAttribute('title') || ''}`);
              return visible(button) &&
                (label === 'экспорт' || label === 'export' || label.includes('экспорт') || label.includes('export')) &&
                hasDownloadIcon(button);
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
          const hasDownloadIcon = (element) => Array
            .from(element.querySelectorAll('path'))
            .map((path) => path.getAttribute('d') || '')
            .some((path) =>
              path.includes('M2.66821 12.6663') &&
              path.includes('9.33521 3.33333'));
          const labels = ['экспорт', 'export'];
          const rootButtons = document.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]');
          const candidates = documents()
            .flatMap((doc) => Array.from(doc === document ? rootButtons : doc.querySelectorAll('button[aria-haspopup="menu"],button[aria-label]')))
            .filter((button) => {
              const label = normalize(`${button.getAttribute('aria-label') || ''}\n${button.getAttribute('title') || ''}`);
              return visible(button) &&
                labels.some((candidate) => label === candidate || label.includes(candidate)) &&
                hasDownloadIcon(button);
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
          button.scrollIntoView({ block: 'nearest', inline: 'nearest' });
          button.focus();
          button.click();
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
            'export to markdown'
          ];
          const rootButtons = document.querySelectorAll('button');
          const buttons = documents()
            .flatMap((doc) => Array.from(doc === document ? rootButtons : doc.querySelectorAll('button')))
            .filter((button) => {
              const text = normalize(`${button.getAttribute('aria-label') || ''}\n${button.innerText || button.textContent || ''}`);
              return visible(button) &&
                !button.disabled &&
                button.getAttribute('aria-disabled') !== 'true' &&
                labels.some((label) => text === label || text.includes(label));
            })
            .sort((left, right) => {
              const top = left.getBoundingClientRect().top - right.getBoundingClientRect().top;
              if (Math.abs(top) > 4) return top;
              return left.getBoundingClientRect().left - right.getBoundingClientRect().left;
            });
          if (buttons.length !== 1) {
            return false;
          }

          const button = buttons[0];
          button.focus();
          button.click();
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
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const prompt = Array
            .from(document.querySelectorAll('#prompt-textarea,[data-testid="composer-text-input"],textarea,[contenteditable="true"][role="textbox"],[contenteditable="true"]'))
            .find(visible);
          if (!prompt) return false;
          return prompt.querySelector('[data-id="connector:connector_openai_deep_research"],[data-system-hint-type="connector:connector_openai_deep_research"],[data-keyword="Глубокое исследование"][data-inline-selection-pill]') !== null;
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

    private const string DeepResearchItemPointExpression =
        """
        (() => {
          const normalize = (text) => (text || '').replace(/\s+/g, ' ').trim().toLowerCase();
          const visible = (element) => {
            const style = getComputedStyle(element);
            const rect = element.getBoundingClientRect();
            return style.display !== 'none' && style.visibility !== 'hidden' && rect.width > 0 && rect.height > 0;
          };
          const plus = document.querySelector('button[data-testid="composer-plus-btn"]');
          const plusRect = plus?.getBoundingClientRect();
          const labels = ['глубокое исследование', 'deep research'];
          const inComposerMenu = (element) => {
            if (!plusRect) return true;
            const rect = element.getBoundingClientRect();
            return rect.y >= plusRect.bottom - 12 &&
              rect.y <= plusRect.bottom + 520 &&
              rect.x >= plusRect.x - 80 &&
              rect.x <= plusRect.x + 900;
          };
          const isHistoryOrMessage = (element) =>
            element.closest('[data-message-author-role]') !== null ||
            element.closest('[data-testid^="project-conversation"]') !== null ||
            element.closest('a[href]') !== null;
          const candidates = Array
            .from(document.querySelectorAll('button,[role="menuitem"],[role="option"],[role="button"],div,span'))
            .filter((element) => visible(element) && inComposerMenu(element) && !isHistoryOrMessage(element));
          const matches = candidates.filter((element) => {
            const text = normalize(`${element.getAttribute('aria-label') || ''}\n${element.innerText || element.textContent || ''}`);
            return labels.some((label) => text === label || text.startsWith(`${label} `));
          });
          const target = matches
            .map((element) => ({ element, rect: element.getBoundingClientRect() }))
            .filter((entry) => entry.rect.width >= 250 || entry.element.closest('button,[role="menuitem"],[role="option"],[role="button"]') !== null)
            .sort((left, right) => (right.rect.width * right.rect.height) - (left.rect.width * left.rect.height))
            .map((entry) => entry.element)[0] || matches[0];
          if (!target) return false;
          const clickable = target.closest('button,[role="menuitem"],[role="option"],[role="button"]') || target;
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
          const deepResearchPill = element.querySelector('[data-id="connector:connector_openai_deep_research"],[data-system-hint-type="connector:connector_openai_deep_research"],[data-keyword="Глубокое исследование"][data-inline-selection-pill]');
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
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                await AttachTargetCoreAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
                return;
            }
            catch (AuditSubmitCodexChromeException ex) when (attempt < 2 && IsRecoverableCdpFailure(ex))
            {
                await ReattachCdpAsync(tabId, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Target attach retry loop ended without returning or throwing a protocol error.");
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
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await ExecuteCdpCoreAsync(tabId, method, commandParameters, timeout, cancellationToken).ConfigureAwait(false);
            }
            catch (AuditSubmitCodexChromeException ex) when (attempt < 2 && allowTransientRecovery && IsRecoverableCdpFailure(ex))
            {
                await ReattachCdpAsync(tabId, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("CDP retry loop ended without returning or throwing a protocol error.");
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
        for (var attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                return await ExecuteCdpCoreAsync(
                    new Dictionary<string, object?>
                    {
                        ["tabId"] = tabId,
                        ["targetId"] = targetId
                    },
                    method,
                    commandParameters,
                    timeout,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (AuditSubmitCodexChromeException ex) when (attempt < 2 && allowTransientRecovery && IsRecoverableCdpFailure(ex))
            {
                await EnsureTargetAttachedForReadAsync(tabId, targetId, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Target CDP retry loop ended without returning or throwing a protocol error.");
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

    public async Task<byte[]> CapturePngAsync(long tabId, CancellationToken cancellationToken)
    {
        List<string> failures = [];
        foreach (var parameters in await CreateScreenshotParameterVariantsAsync(tabId, cancellationToken).ConfigureAwait(false))
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    return await CapturePngWithParametersAsync(tabId, parameters, cancellationToken).ConfigureAwait(false);
                }
                catch (AuditSubmitCodexChromeException ex) when (attempt == 0 && IsRecoverableScreenshotFailure(ex))
                {
                    failures.Add(ex.Message);
                    await ReattachCdpAsync(tabId, cancellationToken).ConfigureAwait(false);
                }
                catch (AuditSubmitCodexChromeException ex)
                {
                    failures.Add(ex.Message);
                    break;
                }
            }
        }

        throw new AuditSubmitCodexChromeException(
            "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
            "Page.captureScreenshot did not return image data. Attempts: " + string.Join("; ", failures));
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

                foreach (var method in new[] { "Page.enable", "Runtime.enable", "DOM.enable" })
                {
                    await ExecuteCdpCoreAsync(tabId, method, [], TimeSpan.FromSeconds(45), cancellationToken).ConfigureAwait(false);
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

    private static bool IsRecoverableScreenshotFailure(AuditSubmitCodexChromeException exception)
    {
        return IsRecoverableCdpFailure(exception) ||
            exception.Message.Contains("Page.captureScreenshot", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRecoverableCdpFailure(AuditSubmitCodexChromeException exception)
    {
        return exception.Message.Contains("Debugger unattached", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Debugger is not attached", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Timed out", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Cannot find context", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("Target closed", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<Dictionary<string, object?>>> CreateScreenshotParameterVariantsAsync(
        long tabId,
        CancellationToken cancellationToken)
    {
        var variants = new List<Dictionary<string, object?>>();
        var viewportClip = await TryCreateViewportClipAsync(tabId, cancellationToken).ConfigureAwait(false);
        if (viewportClip is not null)
        {
            variants.Add(new Dictionary<string, object?>
            {
                ["format"] = "png",
                ["clip"] = viewportClip,
                ["captureBeyondViewport"] = true
            });
        }

        variants.Add(new Dictionary<string, object?>
        {
            ["format"] = "png",
            ["fromSurface"] = false
        });
        variants.Add(new Dictionary<string, object?> { ["format"] = "png" });
        return variants;
    }

    private async Task<Dictionary<string, object?>?> TryCreateViewportClipAsync(long tabId, CancellationToken cancellationToken)
    {
        try
        {
            var metrics = await ExecuteCdpAsync(
                tabId,
                "Page.getLayoutMetrics",
                [],
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
            if (!metrics.TryGetProperty("cssVisualViewport", out var viewport) ||
                !TryReadFiniteDouble(viewport, "pageX", out var x) ||
                !TryReadFiniteDouble(viewport, "pageY", out var y) ||
                !TryReadFiniteDouble(viewport, "clientWidth", out var width) ||
                !TryReadFiniteDouble(viewport, "clientHeight", out var height) ||
                width <= 0 ||
                height <= 0)
            {
                return null;
            }

            var devicePixelRatio = await TryReadDevicePixelRatioAsync(tabId, cancellationToken).ConfigureAwait(false);
            return new Dictionary<string, object?>
            {
                ["x"] = x,
                ["y"] = y,
                ["width"] = width,
                ["height"] = height,
                ["scale"] = devicePixelRatio > 0 ? 1 / devicePixelRatio : 1
            };
        }
        catch (AuditSubmitCodexChromeException)
        {
            return null;
        }
    }

    private async Task<double> TryReadDevicePixelRatioAsync(long tabId, CancellationToken cancellationToken)
    {
        try
        {
            var value = await EvaluateAsync(
                tabId,
                "(() => window.devicePixelRatio)()",
                TimeSpan.FromSeconds(10),
                cancellationToken).ConfigureAwait(false);
            return value.ValueKind == JsonValueKind.Number ? value.GetDouble() : 1;
        }
        catch (AuditSubmitCodexChromeException)
        {
            return 1;
        }
    }

    private async Task<byte[]> CapturePngWithParametersAsync(
        long tabId,
        Dictionary<string, object?> parameters,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteCdpAsync(
            tabId,
            "Page.captureScreenshot",
            parameters,
            TimeSpan.FromSeconds(30),
            cancellationToken).ConfigureAwait(false);
        if (!result.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.String)
        {
            throw new AuditSubmitCodexChromeException(
                "E2D-BUILD-AUDIT-SUBMIT-CODEX-CHROME-PROTOCOL",
                "Page.captureScreenshot did not return image data.");
        }

        return Convert.FromBase64String(data.GetString() ?? string.Empty);
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

internal sealed class AuditSubmitCodexChromeScreenshotRecorder
{
    private static readonly TimeSpan ScreenshotSettleDelay = TimeSpan.FromSeconds(2);
    private readonly string? directory;
    private int nextIndex;

    private AuditSubmitCodexChromeScreenshotRecorder(string? directory)
    {
        this.directory = directory;
    }

    public static AuditSubmitCodexChromeScreenshotRecorder Create(string repoRoot, string? configuredDirectory)
    {
        if (string.IsNullOrWhiteSpace(configuredDirectory))
        {
            return new AuditSubmitCodexChromeScreenshotRecorder(null);
        }

        var directory = Path.GetFullPath(Path.IsPathRooted(configuredDirectory)
            ? configuredDirectory
            : Path.Combine(repoRoot, configuredDirectory));
        Directory.CreateDirectory(directory);
        return new AuditSubmitCodexChromeScreenshotRecorder(directory);
    }

    public async Task CaptureAsync(
        AuditSubmitCodexChromeClient browser,
        long tabId,
        string stage,
        CancellationToken cancellationToken)
    {
        if (directory is null)
        {
            return;
        }

        nextIndex++;
        var path = Path.Combine(directory, $"{nextIndex:00}-{SanitizeStageName(stage)}.png");
        await Task.Delay(ScreenshotSettleDelay, cancellationToken).ConfigureAwait(false);
        var bytes = await browser.CapturePngAsync(tabId, cancellationToken).ConfigureAwait(false);
        await File.WriteAllBytesAsync(path, bytes, cancellationToken).ConfigureAwait(false);
    }

    private static string SanitizeStageName(string stage)
    {
        var builder = new StringBuilder(stage.Length);
        foreach (var ch in stage)
        {
            builder.Append(char.IsAsciiLetterOrDigit(ch) || ch == '-' ? ch : '-');
        }

        return builder.ToString().Trim('-');
    }
}

internal readonly record struct AuditSubmitDomPoint(double X, double Y);

internal readonly record struct AuditSubmitDomRect(double X, double Y, double Width, double Height);

internal readonly record struct AuditSubmitDeepResearchFrame(int ContextId, AuditSubmitDomRect Rect);

internal readonly record struct AuditSubmitReportCandidateResult(bool SurfaceSelected, AuditSubmitReportCandidate[] Candidates)
{
    public static AuditSubmitReportCandidateResult NoSurface { get; } = new(false, []);
}

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

internal sealed class AuditSubmitCodexChromeException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
