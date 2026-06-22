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
    public void UnsupportedCommandReturnsStableJsonDiagnostic()
    {
        var result = RunCli(
            CliExecutionContext.ForTests(FixedInstant),
            "api",
            "compare-godot",
            "Node",
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

    private static CliRunResult RunCli(CliExecutionContext context, params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(args, output, error, context);

        return new CliRunResult(exitCode, output.ToString(), error.ToString());
    }

    private static string CreateProjectRoot(string name, string sceneText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-CliWorkflowTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), sceneText);
        return root;
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
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
        Assert.Contains("Electron2D 0.1.0-preview", agents, StringComparison.Ordinal);
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
