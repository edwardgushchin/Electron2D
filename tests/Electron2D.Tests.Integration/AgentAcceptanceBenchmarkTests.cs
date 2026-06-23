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
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class AgentAcceptanceBenchmarkTests
{
    private static readonly string[] EditorScenarios =
    [
        "active-editor-route",
        "created-script-visible",
        "scene-inspector-viewport-update",
        "concurrent-editing-conflict-panel",
        "visible-runtime-control",
        "snapshot-artifact-stale-policy",
        "task-metadata-not-stale",
        "project-task-manager-links",
        "agent-awaiting-acceptance-only",
        "trusted-human-acceptance",
        "project-tasks-board-manual-flow",
        "script-workspace-tooling",
        "editor-window-screenshot-analysis",
        "managed-debugger-tooling",
        "structured-diagnostics-ai-fix",
        "grouped-ai-undo",
        "agent-crash-read-and-staged-transaction",
        "editor-without-mcp"
    ];

    private static readonly string[] HeadlessTasks =
    [
        "create-project",
        "change-scene",
        "implement-mechanic",
        "fix-diagnostic",
        "verify-and-build"
    ];

    [Fact]
    public void AgentAcceptanceBenchmarkManifestCoversReleaseGateScenarios()
    {
        var root = FindRepositoryRoot();
        var manifestPath = Path.Combine(root, "data", "quality", "agent-acceptance-benchmarks.json");

        Assert.True(File.Exists(manifestPath), $"Missing agent acceptance benchmark manifest: {manifestPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
        var manifest = document.RootElement;

        Assert.Equal("Electron2D.AgentAcceptanceBenchmarkManifest", manifest.GetProperty("format").GetString());
        Assert.Equal(1, manifest.GetProperty("version").GetInt32());
        Assert.Equal("0.1.0-preview", manifest.GetProperty("release").GetString());

        var suites = manifest.GetProperty("suites").EnumerateArray().ToDictionary(
            suite => suite.GetProperty("id").GetString()!,
            StringComparer.Ordinal);
        Assert.Equal(["editor-co-development", "headless-ai"], suites.Keys.Order(StringComparer.Ordinal).ToArray());

        var editor = suites["editor-co-development"];
        Assert.Equal("activeEditor", editor.GetProperty("mode").GetString());
        Assert.True(editor.GetProperty("releaseRequired").GetBoolean());
        Assert.Equal(1.0, editor.GetProperty("targetSuccessRatio").GetDouble());

        var editorScenarioIds = editor.GetProperty("scenarios").EnumerateArray()
            .Select(scenario => scenario.GetProperty("id").GetString())
            .ToArray();
        Assert.Equal(EditorScenarios.Order(StringComparer.Ordinal), editorScenarioIds.Order(StringComparer.Ordinal));

        var editorEvidence = editor.GetProperty("evidence").EnumerateArray().ToArray();
        foreach (var scenarioId in EditorScenarios)
        {
            Assert.Contains(editorEvidence, evidence => Covers(evidence, scenarioId));
        }

        var visualEvidence = Assert.Single(editorEvidence, evidence =>
            evidence.GetProperty("kind").GetString() == "editorRealWindowSmoke");
        Assert.True(Covers(visualEvidence, "editor-window-screenshot-analysis"));
        Assert.Contains("--window-smoke", visualEvidence.GetProperty("arguments").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal(
            "docs/specifications/editor/godot4-editor-reference.md",
            visualEvidence.GetProperty("visualEvidence").GetProperty("reference").GetString());
        Assert.True(visualEvidence.GetProperty("visualEvidence").GetProperty("requiresActualWindow").GetBoolean());
        Assert.True(visualEvidence.GetProperty("visualEvidence").GetProperty("requiresPointerInteraction").GetBoolean());
        Assert.True(visualEvidence.GetProperty("visualEvidence").GetProperty("requiresKeyboardInteraction").GetBoolean());
        Assert.Equal(0, visualEvidence.GetProperty("visualEvidence").GetProperty("maxTextOverflowCount").GetInt32());

        Assert.Contains(editorEvidence, evidence => Covers(evidence, "snapshot-artifact-stale-policy") && Covers(evidence, "task-metadata-not-stale"));
        Assert.Contains(editorEvidence, evidence => Covers(evidence, "agent-crash-read-and-staged-transaction"));
        Assert.Contains(editorEvidence, evidence => Covers(evidence, "editor-without-mcp"));

        var headless = suites["headless-ai"];
        Assert.Equal("headless", headless.GetProperty("mode").GetString());
        Assert.True(headless.GetProperty("releaseRequired").GetBoolean());
        Assert.True(headless.GetProperty("targetSuccessRatio").GetDouble() >= 0.8);
        Assert.Equal(HeadlessTasks, headless.GetProperty("headlessTasks").EnumerateArray().Select(task => task.GetProperty("id").GetString()).ToArray());

        var success = headless.GetProperty("successConditions");
        Assert.True(success.GetProperty("forbidGeneratedCacheEdits").GetBoolean());
        Assert.True(success.GetProperty("forbidUnsupportedApi").GetBoolean());
        Assert.True(success.GetProperty("minimumAgentProfiles").GetInt32() >= 2);
        Assert.False(string.IsNullOrWhiteSpace(success.GetProperty("documentedManualHarness").GetString()));
        Assert.Contains(success.GetProperty("forbiddenGeneratedCachePaths").EnumerateArray(), item =>
            item.GetString() == ".electron2d/import-cache/");

        AssertAllReferencedFilesExist(root, manifest);
    }

    [Fact]
    public async Task AgentAcceptanceBenchmarkRunnerDryRunWritesPlanArtifact()
    {
        var root = FindRepositoryRoot();
        var runnerPath = Path.Combine(root, "tools", "Run-AgentAcceptanceBenchmarks.ps1");
        var outputRoot = CreateTemporaryDirectory("electron2d-agent-acceptance-benchmarks-");

        try
        {
            Assert.True(File.Exists(runnerPath), $"Missing agent acceptance benchmark runner: {runnerPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("-ExecutionPolicy");
            startInfo.ArgumentList.Add("Bypass");
            startInfo.ArgumentList.Add("-File");
            startInfo.ArgumentList.Add(runnerPath);
            startInfo.ArgumentList.Add("-DryRun");
            startInfo.ArgumentList.Add("-OutputDirectory");
            startInfo.ArgumentList.Add(outputRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start benchmark runner.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Agent acceptance benchmark dry run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var planPath = Path.Combine(outputRoot, "benchmark-plan.json");
            Assert.True(File.Exists(planPath), $"Missing benchmark dry-run plan: {planPath}");

            using var planDocument = JsonDocument.Parse(File.ReadAllText(planPath));
            var plan = planDocument.RootElement;
            Assert.Equal("Electron2D.AgentAcceptanceBenchmarkPlan", plan.GetProperty("format").GetString());
            Assert.True(plan.GetProperty("dryRun").GetBoolean());
            Assert.Equal("0.1.0-preview", plan.GetProperty("release").GetString());
            Assert.Equal(2, plan.GetProperty("suites").GetArrayLength());
            Assert.True(plan.GetProperty("requiredEvidenceCount").GetInt32() >= 10);
            Assert.True(plan.GetProperty("visualEvidenceCount").GetInt32() >= 1);
            Assert.True(plan.GetProperty("headlessManualHarnessDocumented").GetBoolean());
            Assert.Contains("Agent acceptance benchmark dry run passed", output, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(outputRoot, recursive: true);
        }
    }

    private static bool Covers(JsonElement evidence, string scenarioId)
    {
        return evidence.GetProperty("covers").EnumerateArray().Any(item => item.GetString() == scenarioId);
    }

    private static void AssertAllReferencedFilesExist(string root, JsonElement manifest)
    {
        foreach (var suite in manifest.GetProperty("suites").EnumerateArray())
        {
            if (suite.TryGetProperty("documentation", out var documentation))
            {
                foreach (var path in documentation.EnumerateArray().Select(item => item.GetString()))
                {
                    AssertRepositoryFileExists(root, path);
                }
            }

            foreach (var evidence in suite.GetProperty("evidence").EnumerateArray())
            {
                foreach (var path in evidence.GetProperty("sourceFiles").EnumerateArray().Select(item => item.GetString()))
                {
                    AssertRepositoryFileExists(root, path);
                }

                if (evidence.TryGetProperty("visualEvidence", out var visualEvidence))
                {
                    AssertRepositoryFileExists(root, visualEvidence.GetProperty("reference").GetString());
                }
            }
        }
    }

    private static void AssertRepositoryFileExists(string root, string? relativePath)
    {
        Assert.False(string.IsNullOrWhiteSpace(relativePath), "Referenced path must not be empty.");
        var path = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        Assert.StartsWith(root, path, StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(path), $"Referenced benchmark file does not exist: {relativePath}");
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
