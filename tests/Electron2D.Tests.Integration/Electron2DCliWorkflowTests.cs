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
using System.Text.Json;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class Electron2DCliWorkflowTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RootAndGroupHelpExposeRequiredGroupsAndCommonFlags()
    {
        var root = RunCli(CliExecutionContext.ForTests(FixedInstant), "--help");

        Assert.Equal(0, root.ExitCode);
        foreach (var group in RequiredGroups)
        {
            Assert.Contains(group, root.Output, StringComparison.Ordinal);
        }

        foreach (var group in RequiredGroups)
        {
            var help = RunCli(CliExecutionContext.ForTests(FixedInstant), group, "--help");
            var expectedFormats = string.Equals(group, "docs", StringComparison.Ordinal)
                ? "--format text|json"
                : string.Equals(group, "tasks", StringComparison.Ordinal)
                    ? "--format text|markdown"
                : string.Equals(group, "context", StringComparison.Ordinal)
                    ? "--format text|json"
                : "--format text|json|jsonl|sarif";

            Assert.Equal(0, help.ExitCode);
            Assert.Contains("--project <path>", help.Output, StringComparison.Ordinal);
            Assert.Contains(expectedFormats, help.Output, StringComparison.Ordinal);
            Assert.Contains("--quiet", help.Output, StringComparison.Ordinal);
            Assert.Contains("--verbose", help.Output, StringComparison.Ordinal);
            if (MutatingOrJobGroups.Contains(group, StringComparer.Ordinal))
            {
                Assert.Contains("--dry-run", help.Output, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void TasksExportWritesStableMarkdownReportWithoutCreatingWorkflowFiles()
    {
        var projectRoot = CreateProjectRoot("tasks-export-markdown", SceneText(speed: 10));
        WriteTaskDocuments(
            projectRoot,
            CreateReportTask(
                "task-alpha",
                "Ship alpha feature",
                ProjectTaskStatus.Done,
                rank: "0200",
                completedAt: FixedInstant.AddHours(1)),
            CreateReportTask(
                "task-beta",
                "Ship beta feature",
                ProjectTaskStatus.Done,
                rank: "0100",
                completedAt: FixedInstant.AddHours(2)),
            CreateReportTask(
                "task-ready",
                "Prepare ready feature",
                ProjectTaskStatus.Ready,
                rank: "0300",
                completedAt: null));

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "tasks",
            "export",
            "--project",
            projectRoot,
            "--status",
            "done",
            "--milestone",
            "preview",
            "--version",
            "0.1-preview",
            "--epic",
            "editor",
            "--assignee",
            "agent-1",
            "--agent-session",
            "agent-session-1",
            "--format",
            "markdown");

        const string expected = """
        # Project Tasks Report

        > Markdown report only. Canonical task storage stays in `.electron2d/tasks/*.e2task` and `.electron2d/tasks/board.e2tasks`.

        - Source: `.electron2d/tasks/*.e2task`
        - Filters: status=Done, milestone=preview, version=0.1-preview, epic=editor, assignee=agent-1, agent-session=agent-session-1
        - Task count: 2

        ## Done

        ### task-beta - Ship beta feature

        - Status: Done
        - Priority: P0
        - Rank: 0100
        - Assignee: agent-1
        - Labels: agent-session:agent-session-1, epic:editor, milestone:preview, version:0.1-preview
        - Created: 2026-06-22T12:00:00.0000000+00:00
        - Completed: 2026-06-22T14:00:00.0000000+00:00
        - Accepted: 2026-06-22T14:00:00.0000000+00:00 by user-1
        - Criteria:
          - [x] criterion-task-beta: Golden output is stable.
        - Activity:
          - 2026-06-22T13:30:00.0000000+00:00 Agent agent-1: TestResult - AgentSessionId=agent-session-1; focused tests green.

        ### task-alpha - Ship alpha feature

        - Status: Done
        - Priority: P0
        - Rank: 0200
        - Assignee: agent-1
        - Labels: agent-session:agent-session-1, epic:editor, milestone:preview, version:0.1-preview
        - Created: 2026-06-22T12:00:00.0000000+00:00
        - Completed: 2026-06-22T13:00:00.0000000+00:00
        - Accepted: 2026-06-22T13:00:00.0000000+00:00 by user-1
        - Criteria:
          - [x] criterion-task-alpha: Golden output is stable.
        - Activity:
          - 2026-06-22T12:30:00.0000000+00:00 Agent agent-1: TestResult - AgentSessionId=agent-session-1; focused tests green.

        """;

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        Assert.Equal(expected.ReplaceLineEndings(Environment.NewLine), result.Output);
        Assert.False(File.Exists(Path.Combine(projectRoot, "TASKS.md")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "completed-tasks")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "dev-diary")));
    }

    [Fact]
    public void ContextBuildCreatesCompactSnapshotWithoutSecretsOrBinaryPayloads()
    {
        var projectRoot = CreateContextProjectRoot("context-build");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "context",
            "build",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);

        using var envelopeJson = JsonDocument.Parse(result.Output);
        var envelope = envelopeJson.RootElement;
        Assert.Equal("context build", envelope.GetProperty("command").GetString());
        Assert.True(envelope.GetProperty("succeeded").GetBoolean());
        Assert.Equal("none", envelope.GetProperty("route").GetString());

        var contextRoot = Path.Combine(projectRoot, ".electron2d", "context");
        var expectedFiles = new[]
        {
            "context-manifest.json",
            "project-summary.json",
            "api-surface.json",
            "godot-differences.json",
            "scene-index.json",
            "resource-graph.json",
            "diagnostics.json",
            "conventions.md"
        };
        foreach (var file in expectedFiles)
        {
            Assert.True(File.Exists(Path.Combine(contextRoot, file)), file);
            Assert.Contains(
                ".electron2d/context/" + file,
                envelope.GetProperty("changedFiles").EnumerateArray().Select(item => item.GetString()));
        }

        var data = envelope.GetProperty("data");
        Assert.Equal("context.build", data.GetProperty("mode").GetString());
        Assert.Equal(".electron2d/context", data.GetProperty("outputPath").GetString());
        Assert.Contains("snapshot", data.GetProperty("snapshotWarning").GetString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(expectedFiles.Length, data.GetProperty("files").GetArrayLength());
        Assert.InRange(data.GetProperty("totalBytes").GetInt64(), 1, 64 * 1024);

        using var summaryJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "project-summary.json")));
        var summary = summaryJson.RootElement;
        Assert.Equal("ContextGame", summary.GetProperty("project").GetProperty("name").GetString());
        Assert.Equal("0.1-preview", summary.GetProperty("engineVersion").GetString());
        Assert.False(string.IsNullOrWhiteSpace(summary.GetProperty("dotnetVersion").GetString()));
        Assert.Equal("Standard", summary.GetProperty("rendererProfile").GetString());
        Assert.Equal("scenes/main.scene.json", summary.GetProperty("mainScene").GetString());
        Assert.Contains(
            "jump",
            summary.GetProperty("inputMap").GetProperty("actions").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Contains(
            "Game.PlayerController",
            summary.GetProperty("customClasses").EnumerateArray().Select(item => item.GetProperty("type").GetString()));
        Assert.Contains(
            "e2d validate --project <project> --format json",
            summary.GetProperty("checkCommands").EnumerateArray().Select(item => item.GetString()));

        using var sceneIndexJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "scene-index.json")));
        var scene = sceneIndexJson.RootElement.GetProperty("scenes")[0];
        Assert.Equal("scenes/main.scene.json", scene.GetProperty("path").GetString());
        Assert.Contains(
            "Player",
            scene.GetProperty("nodes").EnumerateArray().Select(item => item.GetProperty("name").GetString()));
        Assert.Contains(
            "res://assets/player.e2res",
            scene.GetProperty("externalReferences").EnumerateArray().Select(item => item.GetProperty("path").GetString()));

        using var resourceGraphJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "resource-graph.json")));
        Assert.Contains(
            "assets/player.e2res",
            resourceGraphJson.RootElement.GetProperty("resources").EnumerateArray().Select(item => item.GetProperty("path").GetString()));
        Assert.Contains(
            "res://assets/player.e2res",
            resourceGraphJson.RootElement.GetProperty("sceneReferences").EnumerateArray().Select(item => item.GetProperty("target").GetString()));

        using var apiSurfaceJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(contextRoot, "api-surface.json")));
        Assert.True(apiSurfaceJson.RootElement.GetProperty("typeCount").GetInt32() > 0);

        var combinedContext = string.Join(
            "\n",
            expectedFiles.Select(file => File.ReadAllText(Path.Combine(contextRoot, file))));
        Assert.DoesNotContain("<redacted>", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain(".git", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKS.md", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("dev-diary", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("completed-tasks", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("import-cache", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("huge.log", combinedContext, StringComparison.Ordinal);
        Assert.DoesNotContain("player.png", combinedContext, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceTransactionDryRunUsesExplicitHeadlessFallbackAndStableJsonEnvelope()
    {
        var projectRoot = CreateProjectRoot("headless-dry-run", SceneText(speed: 10));
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "workspace",
            "transaction",
            "--project",
            projectRoot,
            "--path",
            "scenes/main.scene.json",
            "--expected-revision",
            "1",
            "--text",
            SceneText(speed: 12),
            "--dry-run",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("workspace transaction", root.GetProperty("command").GetString());
        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("headless", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("dryRun").GetBoolean());
        Assert.Contains("scenes/main.scene.json", root.GetProperty("changedFiles").EnumerateArray().Select(item => item.GetString()));
        Assert.Equal("workspace.transaction", root.GetProperty("operation").GetProperty("operationKind").GetString());
        Assert.Contains("\"value\": 10", File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void WorkspaceTransactionRoutesToActiveEditorWorkspaceWithoutTouchingDisk()
    {
        var projectRoot = CreateProjectRoot("active-editor-route", SceneText(speed: 10));
        var registry = new EditorSessionRegistry(TimeSpan.FromSeconds(30));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-cli-route",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-cli-route"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-cli-route-scene"));

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant, registry),
            "workspace",
            "transaction",
            "--project",
            projectRoot,
            "--path",
            "scenes/main.scene.json",
            "--expected-revision",
            "1",
            "--text",
            SceneText(speed: 14),
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;

        Assert.Equal("activeEditor", root.GetProperty("route").GetString());
        Assert.Contains("scenes/main.scene.json", root.GetProperty("dirtyDocuments").EnumerateArray().Select(item => item.GetString()));
        Assert.Empty(root.GetProperty("changedFiles").EnumerateArray());
        Assert.Contains("\"value\": 14", editor.Workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.Contains("\"value\": 10", File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectCreateCreatesAgentReadyTemplateAndStableJsonEnvelope()
    {
        var projectsRoot = CreateTemporaryDirectory("electron2d-cli-project-create-");

        try
        {
            var result = RunCli(
                CliExecutionContext.ForTests(FixedInstant),
                "project",
                "create",
                "CliAgentGame",
                "--output",
                projectsRoot,
                "--renderer-profile",
                "Compatibility",
                "--format",
                "json");

            Assert.Equal(0, result.ExitCode);
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            var data = root.GetProperty("data");
            var projectRoot = data.GetProperty("projectPath").GetString() ?? string.Empty;

            Assert.True(root.GetProperty("succeeded").GetBoolean());
            Assert.Equal("project create", root.GetProperty("command").GetString());
            Assert.Equal("headless", root.GetProperty("route").GetString());
            Assert.Equal("CliAgentGame", data.GetProperty("projectName").GetString());
            Assert.Equal("Compatibility", data.GetProperty("rendererProfile").GetString());
            Assert.True(data.GetProperty("gitInitialized").GetBoolean());
            Assert.Equal(5, data.GetProperty("starterSkillCount").GetInt32());
            Assert.EndsWith("AGENTS.md", data.GetProperty("agentInstructionsPath").GetString(), StringComparison.Ordinal);
            Assert.EndsWith(".electron2d/tasks/board.e2tasks", data.GetProperty("taskBoardPath").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
            Assert.True(File.Exists(data.GetProperty("projectSettingsPath").GetString()));
            Assert.True(File.Exists(data.GetProperty("mainScenePath").GetString()));

            AssertAgentReadyProject(projectRoot, "Compatibility");
        }
        finally
        {
            Directory.Delete(projectsRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("import", "Import")]
    [InlineData("build", "Build")]
    [InlineData("run", "Run")]
    [InlineData("test", "Test")]
    [InlineData("export", "Export")]
    public void JobCommandsEmitStableJsonlWithSnapshotIdentity(string command, string expectedKind)
    {
        var projectRoot = CreateProjectRoot($"job-{command}", SceneText(speed: 10));
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            command,
            "--project",
            projectRoot,
            "--format",
            "jsonl",
            "--input-build-configuration-hash",
            "sha256:test");

        Assert.Equal(0, result.ExitCode);
        var line = Assert.Single(result.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
        using var json = JsonDocument.Parse(line);
        var root = json.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(command, root.GetProperty("command").GetString());
        Assert.Equal("operation.queued", root.GetProperty("event").GetString());
        Assert.Equal(expectedKind, root.GetProperty("jobKind").GetString());
        Assert.Equal("Queued", root.GetProperty("jobState").GetString());
        Assert.Equal("sha256:test", root.GetProperty("inputBuildConfigurationHash").GetString());
        Assert.False(root.GetProperty("stale").GetBoolean());
        Assert.Equal(1, root.GetProperty("inputDocumentRevisions").GetProperty("scenes/main.scene.json").GetInt64());
    }

    [Fact]
    public void ApiCompareGodotReturnsManifestBackedParityJsonForProfileType()
    {
        var docsRoot = CreateApiCompareDocsRoot("api-compare-approved", "approved");
        var result = RunCliWithDocsRoot(
            CliExecutionContext.ForTests(FixedInstant),
            docsRoot,
            "api",
            "compare-godot",
            "Control",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var type = data.GetProperty("type");
        var profile = type.GetProperty("profile");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("api compare-godot", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("api.compareGodot", data.GetProperty("mode").GetString());
        Assert.Equal("data/api/electron2d-api-manifest.json", data.GetProperty("sourcePath").GetString());
        Assert.Equal("Electron2D.Control", type.GetProperty("fullName").GetString());
        Assert.Equal("electron2d://api/type/Electron2D.Control", type.GetProperty("id").GetString());
        Assert.Equal("supported", profile.GetProperty("status").GetString());
        Assert.Equal("parity_verified", profile.GetProperty("parity").GetString());
        Assert.False(profile.GetProperty("outOfProfile").GetBoolean());
        Assert.Equal("parity_verified", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal(0, root.GetProperty("diagnostics").GetArrayLength());
        AssertParityCountersAreZero(data.GetProperty("strictParity"));
    }

    [Fact]
    public void ApiCompareGodotRejectsOutOfProfileTypeWithStableDiagnostic()
    {
        var docsRoot = CreateApiCompareDocsRoot("api-compare-deferred", "deferred");
        var result = RunCliWithDocsRoot(
            CliExecutionContext.ForTests(FixedInstant),
            docsRoot,
            "api",
            "compare-godot",
            "Control",
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var type = data.GetProperty("type");
        var profile = type.GetProperty("profile");
        var diagnostic = root.GetProperty("diagnostics")[0];

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("api compare-godot", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("out_of_profile", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("Electron2D.Control", type.GetProperty("fullName").GetString());
        Assert.True(profile.GetProperty("outOfProfile").GetBoolean());
        Assert.Equal("E2D-CLI-0002", diagnostic.GetProperty("code").GetString());
        Assert.Contains("outside the Electron2D 0.1-preview 2D profile", diagnostic.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.DoesNotContain("workaround", result.Output, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alternative", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExportPlanWebReturnsWebAssemblyBrowserPlanWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("web-plan-cli");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "plan-web",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var plan = data.GetProperty("plan");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export plan-web", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("export.web.plan", data.GetProperty("mode").GetString());
        Assert.Equal("WebAssemblyBrowser", data.GetProperty("target").GetString());
        Assert.Equal("browser-wasm", data.GetProperty("runtimeIdentifier").GetString());
        Assert.Equal("browser-wasm", plan.GetProperty("runtimeIdentifier").GetString());
        Assert.EndsWith("exports/web/wwwroot", plan.GetProperty("webRootDirectory").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.Contains("renderingReadiness", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public void ExportBuildWebCreatesBrowserPackageWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("web-build-cli");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets"));
        File.WriteAllText(Path.Combine(projectRoot, "assets", "sprite.txt"), "sprite");
        Directory.CreateDirectory(Path.Combine(projectRoot, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-web",
            "--project",
            projectRoot,
            "--output",
            "exports/web",
            "--skip-publish",
            "true",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var package = data.GetProperty("package");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export build-web", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.web.build", data.GetProperty("mode").GetString());
        Assert.Equal("packaged", data.GetProperty("result").GetProperty("status").GetString());
        Assert.True(data.GetProperty("result").GetProperty("publishSkipped").GetBoolean());
        Assert.Contains("index.html", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("electron2d.loader.js", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("electron2d.webmanifest.json", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("assets/sprite.txt", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "index.html")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "electron2d.loader.js")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "electron2d.webmanifest.json")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", "assets", "sprite.txt")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "exports", "web", "wwwroot", ".electron2d")));
    }

    [Fact]
    public void ExportRunWebWritesBrowserSmokeArtifactWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("web-run-cli");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-web",
            "--project",
            projectRoot,
            "--output",
            "exports/web",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-web",
            "--project",
            projectRoot,
            "--output",
            "exports/web",
            "--url",
            "http://127.0.0.1:8080/index.html",
            "--smoke-output",
            ".electron2d/export-smoke/web-smoke.json",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-web", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.web.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-passed", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("http://127.0.0.1:8080/index.html", smoke.GetProperty("launchUrl").GetString());
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "export-smoke", "web-smoke.json")));
        Assert.Contains("startup", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("sceneLoad", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("renderingReadiness", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("inputEventPath", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("audioPolicyState", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("resourceLoading", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("saveDataPolicy", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public void ExportPlanAndroidReturnsAndroidArm64PlanWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("android-plan-cli");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "plan-android",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var plan = data.GetProperty("plan");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export plan-android", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.Equal("export.android.plan", data.GetProperty("mode").GetString());
        Assert.Equal("AndroidArm64", data.GetProperty("target").GetString());
        Assert.Equal("android-arm64", data.GetProperty("runtimeIdentifier").GetString());
        Assert.Equal("apk", plan.GetProperty("packageFormat").GetString());
        Assert.Equal("arm64-v8a", plan.GetProperty("abi").GetString());
        Assert.EndsWith("exports/android/debug/android", plan.GetProperty("stagingDirectory").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.Contains("pauseResume", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
    }

    [Fact]
    public void ExportBuildAndroidCreatesStagingProjectWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("android-build-cli");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets"));
        File.WriteAllText(Path.Combine(projectRoot, "assets", "sprite.txt"), "sprite");
        Directory.CreateDirectory(Path.Combine(projectRoot, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var package = data.GetProperty("package");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export build-android", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.android.build", data.GetProperty("mode").GetString());
        Assert.Equal("staged", data.GetProperty("result").GetProperty("status").GetString());
        Assert.True(data.GetProperty("result").GetProperty("publishSkipped").GetBoolean());
        Assert.Contains("Electron2D.Android.csproj", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("MainActivity.cs", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("AndroidManifest.xml", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Assets/electron2d/assets/sprite.txt", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "Electron2D.Android.csproj")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "MainActivity.cs")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "AndroidManifest.xml")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "exports", "android", "debug", "android", "Assets", "electron2d", ".electron2d")));
    }

    [Fact]
    public void ExportRunAndroidWithoutDeviceWritesBlockedSmokeArtifactWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("android-run-cli");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);
        var adbPath = CreateFakeAdbWithoutDevices(projectRoot);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--smoke-output",
            ".electron2d/export-smoke/android-smoke.json",
            "--adb-path",
            adbPath,
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");
        var diagnostic = root.GetProperty("diagnostics")[0];

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-android", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.android.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-blocked", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("E2D-CLI-0002", diagnostic.GetProperty("code").GetString());
        Assert.Contains("E2D-EXPORT-ANDROID-0014", diagnostic.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "export-smoke", "android-smoke.json")));
        Assert.Contains("pauseResume", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("render", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("input", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("audio", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("resources", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("filesystem", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public void ExportRunAndroidWithDeviceInstallsLaunchesAndWritesPassedSmokeArtifact()
    {
        var projectRoot = CreateExportProjectRoot("android-run-cli-device");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);

        var apkDirectory = Path.Combine(
            projectRoot,
            "exports",
            "android",
            "debug",
            "android",
            "bin",
            "Debug",
            "net10.0-android",
            "android-arm64");
        Directory.CreateDirectory(apkDirectory);
        File.WriteAllText(Path.Combine(apkDirectory, "electron2d.androidexport-Signed.apk"), "fake apk");
        var adbPath = CreateFakeAdb(projectRoot);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--smoke-output",
            ".electron2d/export-smoke/android-smoke.json",
            "--adb-path",
            adbPath,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-android", root.GetProperty("command").GetString());
        Assert.Equal("export.android.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-passed", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("emulator-5554", smoke.GetProperty("deviceSerial").GetString());
        Assert.All(
            smoke.GetProperty("criteria").EnumerateObject(),
            criterion => Assert.True(criterion.Value.GetProperty("passed").GetBoolean(), criterion.Name));
        Assert.Contains(
            "logcat -d -s Electron2D:I *:S",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell input keyevent KEYCODE_WAKEUP",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell wm dismiss-keyguard",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell input tap",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell am start -n dev.electron2d.referencegame/crc644abc767ad8be2900.MainActivity",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        Assert.Contains(
            "shell monkey -p dev.electron2d.referencegame --pct-touch 100",
            File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt")),
            StringComparison.Ordinal);
        var artifactPath = Path.Combine(projectRoot, ".electron2d", "export-smoke", "android-smoke.json");
        Assert.True(File.Exists(artifactPath));
        Assert.Contains("\"status\": \"passed\"", File.ReadAllText(artifactPath), StringComparison.Ordinal);
    }

    [Fact]
    public void ExportRunAndroidUsesRequestedAdbSerialWhenMultipleDevicesAreAvailable()
    {
        var projectRoot = CreateExportProjectRoot("android-run-cli-adb-serial");
        var build = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");
        Assert.Equal(0, build.ExitCode);

        var apkDirectory = Path.Combine(
            projectRoot,
            "exports",
            "android",
            "debug",
            "android",
            "bin",
            "Debug",
            "net10.0-android",
            "android-arm64");
        Directory.CreateDirectory(apkDirectory);
        File.WriteAllText(Path.Combine(apkDirectory, "electron2d.androidexport-Signed.apk"), "fake apk");
        var adbPath = CreateFakeAdbWithPhoneAndEmulator(projectRoot);

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-android",
            "--project",
            projectRoot,
            "--output",
            "exports/android/debug",
            "--smoke-output",
            ".electron2d/export-smoke/android-smoke.json",
            "--adb-path",
            adbPath,
            "--adb-serial",
            "emulator-5554",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var smoke = root.GetProperty("data").GetProperty("smoke");
        var adbLog = File.ReadAllText(Path.Combine(projectRoot, "fake-adb-args.txt"));

        Assert.Equal("emulator-5554", smoke.GetProperty("deviceSerial").GetString());
        Assert.Contains("-s emulator-5554 shell getprop ro.product.cpu.abi", adbLog, StringComparison.Ordinal);
        Assert.Contains("-s emulator-5554 install -r -t", adbLog, StringComparison.Ordinal);
        Assert.DoesNotContain("-s 641d225b0510 install -r -t", adbLog, StringComparison.Ordinal);
    }

    [Fact]
    public void ExportPlanIosReturnsIosArm64PlanWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("ios-plan-cli");
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "plan-ios",
            "--project",
            projectRoot,
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var plan = data.GetProperty("plan");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export plan-ios", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.ios.plan", data.GetProperty("mode").GetString());
        Assert.Equal("IosArm64", data.GetProperty("target").GetString());
        Assert.Equal("ios-arm64", data.GetProperty("runtimeIdentifier").GetString());
        Assert.Equal("metal", plan.GetProperty("graphicsBackend").GetString());
        Assert.EndsWith("exports/ios/debug/ios", plan.GetProperty("stagingDirectory").GetString()?.Replace('\\', '/'), StringComparison.Ordinal);
        Assert.Contains("safeArea", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("precompiledArtifacts", plan.GetProperty("smokeCriteria").EnumerateArray().Select(item => item.GetString()));
    }

    [Fact]
    public void ExportBuildIosCreatesStagingProjectWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("ios-build-cli");
        Directory.CreateDirectory(Path.Combine(projectRoot, "assets"));
        File.WriteAllText(Path.Combine(projectRoot, "assets", "sprite.txt"), "sprite");
        Directory.CreateDirectory(Path.Combine(projectRoot, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task"), "local task metadata");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "build-ios",
            "--project",
            projectRoot,
            "--output",
            "exports/ios/debug",
            "--skip-publish",
            "true",
            "--format",
            "json");

        Assert.Equal(0, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var package = data.GetProperty("package");

        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export build-ios", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.ios.build", data.GetProperty("mode").GetString());
        Assert.Equal("staged", data.GetProperty("result").GetProperty("status").GetString());
        Assert.True(data.GetProperty("result").GetProperty("publishSkipped").GetBoolean());
        Assert.Contains("Electron2D.iOS.csproj", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("AppDelegate.cs", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Info.plist", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Entitlements.plist", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("ExportMetadata.json", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.Contains("Assets/electron2d/assets/sprite.txt", package.GetProperty("files").EnumerateArray().Select(item => item.GetString()));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "ios", "debug", "ios", "Electron2D.iOS.csproj")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "exports", "ios", "debug", "ios", "Electron2D.iOS.xcodeproj", "project.pbxproj")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "exports", "ios", "debug", "ios", "Assets", "electron2d", ".electron2d")));
    }

    [Fact]
    public void ExportRunIosWithoutSimulatorOrDeviceWritesBlockedSmokeArtifactWithoutQueueingJob()
    {
        var projectRoot = CreateExportProjectRoot("ios-run-cli");

        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "export",
            "run-ios",
            "--project",
            projectRoot,
            "--output",
            "exports/ios/debug",
            "--smoke-output",
            ".electron2d/export-smoke/ios-smoke.json",
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var root = json.RootElement;
        var data = root.GetProperty("data");
        var smoke = data.GetProperty("smoke");
        var diagnostic = root.GetProperty("diagnostics")[0];

        Assert.False(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("export run-ios", root.GetProperty("command").GetString());
        Assert.Equal("none", root.GetProperty("route").GetString());
        Assert.True(root.GetProperty("job").ValueKind is JsonValueKind.Null);
        Assert.Equal("export.ios.run", data.GetProperty("mode").GetString());
        Assert.Equal("smoke-blocked", data.GetProperty("result").GetProperty("status").GetString());
        Assert.Equal("E2D-CLI-0002", diagnostic.GetProperty("code").GetString());
        Assert.Contains("E2D-EXPORT-IOS-0011", diagnostic.GetProperty("message").GetString(), StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "export-smoke", "ios-smoke.json")));
        Assert.Contains("safeArea", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("input", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("audio", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("resources", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("filesystem", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
        Assert.Contains("precompiledArtifacts", smoke.GetProperty("criteria").EnumerateObject().Select(item => item.Name));
    }

    [Fact]
    public void UnknownCommandGroupReturnsStableJsonDiagnostic()
    {
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "unknown",
            "command",
            "--format",
            "json");

        Assert.Equal(1, result.ExitCode);
        Assert.Empty(result.Error);
        using var json = JsonDocument.Parse(result.Output);
        var diagnostic = json.RootElement.GetProperty("diagnostics")[0];

        Assert.False(json.RootElement.GetProperty("succeeded").GetBoolean());
        Assert.Equal("blocked", json.RootElement.GetProperty("route").GetString());
        Assert.Equal("E2D-CLI-0001", diagnostic.GetProperty("code").GetString());
    }

    private static void AssertParityCountersAreZero(JsonElement strictParity)
    {
        Assert.Equal(0, strictParity.GetProperty("missingTypes").GetInt32());
        Assert.Equal(0, strictParity.GetProperty("missingMembers").GetInt32());
        Assert.Equal(0, strictParity.GetProperty("signatureMismatches").GetInt32());
        Assert.Equal(0, strictParity.GetProperty("inheritanceMismatches").GetInt32());
        Assert.Equal(0, strictParity.GetProperty("defaultMismatches").GetInt32());
        Assert.Equal(0, strictParity.GetProperty("unexpectedChanges").GetInt32());
    }

    private static CliRunResult RunCli(CliExecutionContext context, params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(args, output, error, context);

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
    }

    private static CliRunResult RunCliWithDocsRoot(CliExecutionContext context, string docsRoot, params string[] args)
    {
        var previousDocsRoot = Environment.GetEnvironmentVariable("ELECTRON2D_DOCS_ROOT");
        Environment.SetEnvironmentVariable("ELECTRON2D_DOCS_ROOT", docsRoot);
        try
        {
            return RunCli(context, args);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ELECTRON2D_DOCS_ROOT", previousDocsRoot);
        }
    }

    private static string CreateApiCompareDocsRoot(string name, string decision)
    {
        var root = CreateTemporaryDirectory("electron2d-cli-" + name + "-");
        var isApproved = string.Equals(decision, "approved", StringComparison.Ordinal);
        var status = isApproved ? "supported" : decision;
        var parity = isApproved ? "parity_verified" : "not_verified";
        var outOfProfile = isApproved ? "false" : "true";
        Directory.CreateDirectory(Path.Combine(root, "data", "api"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "documentation"));
        File.WriteAllText(
            Path.Combine(root, "data", "api", "electron2d-public-api-profile.json"),
            $$"""
            {
              "schemaVersion": 1,
              "release": "0.1-preview",
              "godotBaseline": "4.7-stable",
              "approvalAuthority": "project-owner",
              "types": [
                {
                  "fullName": "Electron2D.Control",
                  "godotReference": "Control",
                  "decision": "{{decision}}",
                  "rationale": "Fixture decision for CLI profile behavior."
                }
              ]
            }
            """);
        File.WriteAllText(
            Path.Combine(root, "data", "api", "electron2d-api-manifest.json"),
            $$"""
            {
              "schemaVersion": 1,
              "manifestVersion": "0.1-preview",
              "engineVersion": "0.1-preview",
              "profileName": "Electron2D 0.1-preview",
              "godotBaseline": "4.7-stable",
              "generatedFrom": {
                "compiledAssembly": "src/Electron2D/bin/Debug/net10.0/Electron2D.dll",
                "xmlDocumentation": ".temp/api-manifest/Electron2D.xml",
                "publicApiProfile": "data/api/electron2d-public-api-profile.json"
              },
              "strictParitySummary": {
                "missingTypes": 0,
                "missingMembers": 0,
                "signatureMismatches": 0,
                "inheritanceMismatches": 0,
                "defaultMismatches": 0,
                "unexpectedChanges": 0
              },
              "types": [
                {
                  "id": "electron2d://api/type/Electron2D.Control",
                  "fullName": "Electron2D.Control",
                  "name": "Control",
                  "profile": {
                    "status": "{{status}}",
                    "parity": "{{parity}}",
                    "outOfProfile": {{outOfProfile}},
                    "godotReference": "Control",
                    "notes": "Fixture decision for CLI profile behavior."
                  }
                }
              ]
            }
            """);
        return root;
    }

    private static string CreateProjectRoot(string name, string sceneText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-CliWorkflowTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), sceneText);
        return root;
    }

    private static string CreateExportProjectRoot(string name)
    {
        var root = CreateProjectRoot(name, SceneText(speed: 10));
        var settings = Electron2D.Electron2DProjectSettings.Capture(
            "ReferenceGame",
            "0.1.0",
            "0.1-preview",
            "scenes/main.scene.json");
        Electron2D.Electron2DSettingsStore.SaveProject(Path.Combine(root, "project.e2d.json"), settings);
        File.WriteAllText(
            Path.Combine(root, "Electron2D.Empty.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
        return root;
    }

    private static string CreateContextProjectRoot(string name)
    {
        var root = CreateExportProjectRoot(name);
        var resourceDirectory = Path.Combine(root, "assets");
        var scriptDirectory = Path.Combine(root, "Scripts");
        Directory.CreateDirectory(resourceDirectory);
        Directory.CreateDirectory(scriptDirectory);

        var resourcePath = "res://assets/player.e2res";
        var resourceUid = Electron2D.ResourceUid.CreateIdForPath(resourcePath);
        var resourceDocument = new Electron2D.ResourceFileDocument(
            resourceUid,
            "Electron2D.Texture2D",
            resourcePath);
        File.WriteAllText(
            Path.Combine(resourceDirectory, "player.e2res"),
            Electron2D.ResourceFileTextSerializer.Serialize(resourceDocument));
        File.WriteAllBytes(Path.Combine(resourceDirectory, "player.png"), [0x89, 0x50, 0x4E, 0x47]);

        var sceneDocument = new Electron2D.SceneFileDocument(
            [
                new Electron2D.ResourceFileExternalReference(
                    1,
                    resourceUid,
                    resourcePath,
                    "Electron2D.Texture2D")
            ],
            [],
            [
                new Electron2D.SceneFileNode(1, "Electron2D.Node2D", "Root", null, null, ["gameplay"]),
                new Electron2D.SceneFileNode(2, "Game.PlayerController", "Player", 1, 1, ["player"]),
                new Electron2D.SceneFileNode(3, "Electron2D.Sprite2D", "Sprite", 2, 1)
            ]);
        File.WriteAllText(
            Path.Combine(root, "scenes", "main.scene.json"),
            Electron2D.SceneFileTextSerializer.Serialize(sceneDocument));

        File.WriteAllText(
            Path.Combine(scriptDirectory, "PlayerController.cs"),
            """
            namespace Game;

            public sealed class PlayerController : Electron2D.Node2D
            {
            }
            """);

        Electron2D.InputMap.ClearForTests();
        try
        {
            Electron2D.InputMap.AddAction("jump", 0.25f);
            Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });
            var settings = Electron2D.Electron2DProjectSettings.Capture(
                "ContextGame",
                "0.1.0",
                "0.1-preview",
                "scenes/main.scene.json");
            settings.RendererProfile = Electron2D.Electron2DRendererProfileSetting.Standard;
            Electron2D.Electron2DSettingsStore.SaveProject(Path.Combine(root, "project.e2d.json"), settings);
        }
        finally
        {
            Electron2D.InputMap.ClearForTests();
        }

        Directory.CreateDirectory(Path.Combine(root, ".git"));
        File.WriteAllText(Path.Combine(root, ".git", "config"), "token=<redacted>");
        Directory.CreateDirectory(Path.Combine(root, ".electron2d", "import-cache"));
        File.WriteAllBytes(Path.Combine(root, ".electron2d", "import-cache", "cached-texture.bin"), [1, 2, 3, 4]);
        Directory.CreateDirectory(Path.Combine(root, "dev-diary"));
        Directory.CreateDirectory(Path.Combine(root, "completed-tasks"));
        File.WriteAllText(Path.Combine(root, "TASKS.md"), "password=<redacted>");
        File.WriteAllText(Path.Combine(root, "huge.log"), new string('x', 70 * 1024) + "<redacted>");

        return root;
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteTaskDocuments(string projectRoot, params ProjectTask[] tasks)
    {
        var tasksRoot = Path.Combine(projectRoot, ".electron2d", "tasks");
        Directory.CreateDirectory(tasksRoot);
        foreach (var task in tasks)
        {
            File.WriteAllText(
                Path.Combine(tasksRoot, $"{task.TaskId}.e2task"),
                ProjectTaskSerializer.Serialize(task));
        }

        var board = new TaskBoard(
            "board-main",
            [
                new TaskBoardColumn(ProjectTaskStatus.Backlog, []),
                new TaskBoardColumn(ProjectTaskStatus.Ready, tasks.Where(task => task.Status == ProjectTaskStatus.Ready).Select(task => task.TaskId)),
                new TaskBoardColumn(ProjectTaskStatus.InProgress, []),
                new TaskBoardColumn(ProjectTaskStatus.Blocked, []),
                new TaskBoardColumn(ProjectTaskStatus.Review, []),
                new TaskBoardColumn(ProjectTaskStatus.AwaitingAcceptance, []),
                new TaskBoardColumn(ProjectTaskStatus.Done, tasks.Where(task => task.Status == ProjectTaskStatus.Done).Select(task => task.TaskId)),
                new TaskBoardColumn(ProjectTaskStatus.Cancelled, [])
            ]);
        File.WriteAllText(
            Path.Combine(tasksRoot, "board.e2tasks"),
            ProjectTaskSerializer.SerializeBoard(board));
    }

    private static ProjectTask CreateReportTask(
        string taskId,
        string title,
        ProjectTaskStatus status,
        string rank,
        DateTimeOffset? completedAt)
    {
        var task = new ProjectTask
        {
            TaskId = taskId,
            Title = title,
            Description = "Exercise Project Tasks report export.",
            Status = status,
            Readiness = TaskReadiness.Ready,
            Priority = "P0",
            Rank = rank,
            Assignee = "agent-1",
            CreatedBy = "user-1",
            CreatedAt = FixedInstant,
            UpdatedAt = FixedInstant,
            CompletedAt = completedAt,
            AcceptedAt = completedAt,
            AcceptedBy = completedAt is null ? null : "user-1",
            AcceptanceState = completedAt is null
                ? ProjectTaskAcceptanceState.Open
                : ProjectTaskAcceptanceState.Accepted
        };
        task.Labels.Add("milestone:preview");
        task.Labels.Add("version:0.1-preview");
        task.Labels.Add("epic:editor");
        task.Labels.Add("agent-session:agent-session-1");
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            $"criterion-{taskId}",
            "Golden output is stable.",
            completedAt is null ? AcceptanceCriterionState.Open : AcceptanceCriterionState.Passed,
            []));
        task.Activity.Add(new TaskActivityEntry(
            $"activity-{taskId}",
            "agent-1",
            PrincipalKind.Agent,
            completedAt?.AddMinutes(-30) ?? FixedInstant.AddMinutes(10),
            TaskActivityKind.TestResult,
            "AgentSessionId=agent-session-1; focused tests green."));
        return task;
    }

    private static string CreateFakeAdb(string projectRoot)
    {
        var logPath = Path.Combine(projectRoot, "fake-adb-args.txt");
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(projectRoot, "fake-adb.cmd");
            File.WriteAllText(
                path,
                $$"""
                @echo off
                echo %*>>"{{logPath}}"
                set ARGS=%*
                if "%1"=="devices" (
                  echo List of devices attached
                  echo emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:1
                  exit /b 0
                )
                if "%1"=="-s" (
                  if "%3"=="install" (
                    echo Success
                    exit /b 0
                  )
                  if "%3"=="logcat" (
                    echo I/Electron2D: E2D_SMOKE_LAUNCH_READY
                    echo I/Electron2D: E2D_SMOKE_RENDER_READY
                    echo I/Electron2D: E2D_SMOKE_TOUCH_READY
                    echo I/Electron2D: E2D_SMOKE_PAUSE_READY
                    echo I/Electron2D: E2D_SMOKE_RESUME_READY
                    echo I/Electron2D: E2D_SMOKE_ORIENTATION_READY
                    echo I/Electron2D: E2D_SMOKE_SAFE_AREA_READY
                    echo I/Electron2D: E2D_SMOKE_AUDIO_READY
                    echo I/Electron2D: E2D_SMOKE_RESOURCES_READY
                    echo I/Electron2D: E2D_SMOKE_FILESYSTEM_READY
                    echo I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY
                    echo I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY
                    echo I/Electron2D: E2D_SMOKE_SHUTDOWN_READY
                    exit /b 0
                  )
                  if "%3"=="shell" (
                    if "%4"=="getprop" (
                      echo x86_64
                      exit /b 0
                    )
                    if "%4"=="input" (
                      exit /b 1
                    )
                    exit /b 0
                  )
                )
                echo OK
                exit /b 0
                """,
                System.Text.Encoding.ASCII);
            return path;
        }

        var unixPath = Path.Combine(projectRoot, "fake-adb.sh");
        File.WriteAllText(
            unixPath,
            $$"""
            #!/usr/bin/env sh
            printf '%s\n' "$*" >> "{{logPath}}"
            if [ "$1" = "devices" ]; then
              echo "List of devices attached"
              echo "emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:1"
              exit 0
            fi
            if [ "$1" = "-s" ]; then
              if [ "$3" = "install" ]; then
                echo "Success"
                exit 0
              fi
              if [ "$3" = "logcat" ]; then
                echo "I/Electron2D: E2D_SMOKE_LAUNCH_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDER_READY"
                echo "I/Electron2D: E2D_SMOKE_TOUCH_READY"
                echo "I/Electron2D: E2D_SMOKE_PAUSE_READY"
                echo "I/Electron2D: E2D_SMOKE_RESUME_READY"
                echo "I/Electron2D: E2D_SMOKE_ORIENTATION_READY"
                echo "I/Electron2D: E2D_SMOKE_SAFE_AREA_READY"
                echo "I/Electron2D: E2D_SMOKE_AUDIO_READY"
                echo "I/Electron2D: E2D_SMOKE_RESOURCES_READY"
                echo "I/Electron2D: E2D_SMOKE_FILESYSTEM_READY"
                echo "I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY"
                echo "I/Electron2D: E2D_SMOKE_SHUTDOWN_READY"
                exit 0
              fi
              if [ "$3" = "shell" ]; then
                if [ "$4" = "getprop" ]; then
                  echo "x86_64"
                  exit 0
                fi
                if [ "$4" = "input" ]; then
                  exit 1
                fi
                exit 0
              fi
            fi
            echo "OK"
            exit 0
            """,
            System.Text.Encoding.ASCII);
        MakeExecutable(unixPath);
        return unixPath;
    }

    private static string CreateFakeAdbWithPhoneAndEmulator(string projectRoot)
    {
        var logPath = Path.Combine(projectRoot, "fake-adb-args.txt");
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(projectRoot, "fake-adb-multiple.cmd");
            File.WriteAllText(
                path,
                $$"""
                @echo off
                echo %*>>"{{logPath}}"
                if "%1"=="devices" (
                  echo List of devices attached
                  echo 641d225b0510 device product:eos model:22011119UY device:eos transport_id:1
                  echo emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:2
                  exit /b 0
                )
                if "%1"=="-s" (
                  if "%3"=="install" (
                    echo Success
                    exit /b 0
                  )
                  if "%3"=="logcat" (
                    echo I/Electron2D: E2D_SMOKE_LAUNCH_READY
                    echo I/Electron2D: E2D_SMOKE_RENDER_READY
                    echo I/Electron2D: E2D_SMOKE_TOUCH_READY
                    echo I/Electron2D: E2D_SMOKE_PAUSE_READY
                    echo I/Electron2D: E2D_SMOKE_RESUME_READY
                    echo I/Electron2D: E2D_SMOKE_ORIENTATION_READY
                    echo I/Electron2D: E2D_SMOKE_SAFE_AREA_READY
                    echo I/Electron2D: E2D_SMOKE_AUDIO_READY
                    echo I/Electron2D: E2D_SMOKE_RESOURCES_READY
                    echo I/Electron2D: E2D_SMOKE_FILESYSTEM_READY
                    echo I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY
                    echo I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY
                    echo I/Electron2D: E2D_SMOKE_SHUTDOWN_READY
                    exit /b 0
                  )
                  if "%3"=="shell" (
                    if "%4"=="getprop" (
                      if "%2"=="emulator-5554" (
                        echo x86_64
                      ) else (
                        echo arm64-v8a
                      )
                      exit /b 0
                    )
                    exit /b 0
                  )
                )
                echo OK
                exit /b 0
                """,
                System.Text.Encoding.ASCII);
            return path;
        }

        var unixPath = Path.Combine(projectRoot, "fake-adb-multiple.sh");
        File.WriteAllText(
            unixPath,
            $$"""
            #!/usr/bin/env sh
            printf '%s\n' "$*" >> "{{logPath}}"
            if [ "$1" = "devices" ]; then
              echo "List of devices attached"
              echo "641d225b0510 device product:eos model:22011119UY device:eos transport_id:1"
              echo "emulator-5554 device product:sdk_gphone64_x86_64 model:sdk_gphone64_x86_64 device:emu64xa transport_id:2"
              exit 0
            fi
            if [ "$1" = "-s" ]; then
              if [ "$3" = "install" ]; then
                echo "Success"
                exit 0
              fi
              if [ "$3" = "logcat" ]; then
                echo "I/Electron2D: E2D_SMOKE_LAUNCH_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDER_READY"
                echo "I/Electron2D: E2D_SMOKE_TOUCH_READY"
                echo "I/Electron2D: E2D_SMOKE_PAUSE_READY"
                echo "I/Electron2D: E2D_SMOKE_RESUME_READY"
                echo "I/Electron2D: E2D_SMOKE_ORIENTATION_READY"
                echo "I/Electron2D: E2D_SMOKE_SAFE_AREA_READY"
                echo "I/Electron2D: E2D_SMOKE_AUDIO_READY"
                echo "I/Electron2D: E2D_SMOKE_RESOURCES_READY"
                echo "I/Electron2D: E2D_SMOKE_FILESYSTEM_READY"
                echo "I/Electron2D: E2D_SMOKE_LOGO_BLACK_READY"
                echo "I/Electron2D: E2D_SMOKE_RENDERER_FALLBACK_READY"
                echo "I/Electron2D: E2D_SMOKE_SHUTDOWN_READY"
                exit 0
              fi
              if [ "$3" = "shell" ]; then
                if [ "$4" = "getprop" ]; then
                  if [ "$2" = "emulator-5554" ]; then
                    echo "x86_64"
                  else
                    echo "arm64-v8a"
                  fi
                  exit 0
                fi
                exit 0
              fi
            fi
            echo "OK"
            exit 0
            """,
            System.Text.Encoding.ASCII);
        MakeExecutable(unixPath);
        return unixPath;
    }

    private static string CreateFakeAdbWithoutDevices(string projectRoot)
    {
        if (OperatingSystem.IsWindows())
        {
            var path = Path.Combine(projectRoot, "fake-adb-empty.cmd");
            File.WriteAllText(
                path,
                """
                @echo off
                if "%1"=="devices" (
                  echo List of devices attached
                  exit /b 0
                )
                exit /b 0
                """,
                System.Text.Encoding.ASCII);
            return path;
        }

        var unixPath = Path.Combine(projectRoot, "fake-adb-empty.sh");
        File.WriteAllText(
            unixPath,
            """
            #!/usr/bin/env sh
            if [ "$1" = "devices" ]; then
              echo "List of devices attached"
              exit 0
            fi
            exit 0
            """,
            System.Text.Encoding.ASCII);
        MakeExecutable(unixPath);
        return unixPath;
    }

    private static void MakeExecutable(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute |
                UnixFileMode.GroupRead |
                UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead |
                UnixFileMode.OtherExecute);
        }
    }

    private static void AssertAgentReadyProject(string projectRoot, string rendererProfile)
    {
        Assert.True(Directory.Exists(Path.Combine(projectRoot, ".git")));
        Assert.True(File.Exists(Path.Combine(projectRoot, "AGENTS.md")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".gitignore")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "tasks", "board.e2tasks")));
        Assert.True(File.Exists(Path.Combine(projectRoot, ".electron2d", "tasks", "welcome.e2task")));
        Assert.False(File.Exists(Path.Combine(projectRoot, "TASKS.md")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "completed-tasks")));
        Assert.False(Directory.Exists(Path.Combine(projectRoot, "dev-diary")));

        var agents = File.ReadAllText(Path.Combine(projectRoot, "AGENTS.md"));
        Assert.Contains("Electron2D 0.1-preview", agents, StringComparison.Ordinal);
        Assert.Contains($"Renderer profile: `{rendererProfile}`", agents, StringComparison.Ordinal);
        Assert.Contains("task_submit_for_acceptance", agents, StringComparison.Ordinal);
        Assert.DoesNotContain("TASKS.md", agents, StringComparison.Ordinal);

        var gitIgnoreLines = File.ReadAllLines(Path.Combine(projectRoot, ".gitignore"));
        Assert.Contains(".electron2d/import-cache/", gitIgnoreLines);
        Assert.Contains(".electron2d/workspaces/", gitIgnoreLines);
        Assert.Contains(".electron2d/context/", gitIgnoreLines);
        Assert.Contains(".electron2d/session/", gitIgnoreLines);
        Assert.Contains(".electron2d/user/", gitIgnoreLines);
        Assert.DoesNotContain(".electron2d/", gitIgnoreLines);
        Assert.DoesNotContain(".electron2d/tasks/", gitIgnoreLines);

        var skillFiles = Directory.EnumerateFiles(Path.Combine(projectRoot, ".codex", "skills"), "SKILL.md", SearchOption.AllDirectories)
            .ToArray();
        Assert.Equal(5, skillFiles.Length);
    }

    private static string SceneText(int speed)
    {
        return $$"""
        {
          "format": "Electron2D.SceneFile",
          "version": 1,
          "external": [],
          "internal": [],
          "nodes": [
            {
              "id": 1,
              "type": "Electron2D.Node2D",
              "name": "Player",
              "parent": null,
              "owner": null,
              "groups": [],
              "properties": {
                "speed": {
                  "type": "Int",
                  "value": {{speed}}
                }
              }
            }
          ]
        }
        """;
    }

    private static readonly string[] RequiredGroups =
    [
        "project",
        "scene",
        "resource",
        "workspace",
        "import",
        "build",
        "run",
        "test",
        "export",
        "validate",
        "docs",
        "api",
        "mcp",
        "tasks",
        "context",
        "doctor"
    ];

    private static readonly string[] MutatingOrJobGroups =
    [
        "project",
        "scene",
        "resource",
        "workspace",
        "import",
        "build",
        "run",
        "test",
        "export"
    ];

    private sealed record CliRunResult(int ExitCode, string Output, string Error);
}
