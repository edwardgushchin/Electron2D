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
using Electron2D.Mcp;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class Electron2DMcpServerTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void McpSessionPublishesRequiredResourcesAndTools()
    {
        using var session = McpServerSession.Open(CreateProjectRoot("manifest", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress)), registry: null, FixedInstant);

        var resources = session.ListResources().Select(resource => resource.Uri).ToArray();
        var tools = session.ListTools().Select(tool => tool.Name).ToArray();

        Assert.Contains("electron2d://project/summary", resources);
        Assert.Contains("electron2d://workspace/open-documents", resources);
        Assert.Contains("electron2d://editor/capabilities", resources);
        Assert.Contains("electron2d://runtime/capabilities", resources);
        Assert.Contains("workspace_apply_transaction", tools);
        Assert.Contains("project_build", tools);
        Assert.Contains("resource_import", tools);
        Assert.Contains("runtime_start", tools);
        Assert.Contains("runtime_resume", tools);
        Assert.Contains("runtime_highlight_node", tools);
        Assert.Contains("task_submit_for_acceptance", tools);
        Assert.Contains("task_accept", tools);
        Assert.All(session.ListTools(), tool => Assert.False(string.IsNullOrWhiteSpace(tool.Description)));
    }

    [Fact]
    public void WorkspaceResourcesReadActiveEditorDirtyStateAndMutationsRouteToActiveWorkspace()
    {
        var projectRoot = CreateProjectRoot("active-editor", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        var registry = new EditorSessionRegistry(TimeSpan.FromSeconds(30));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-mcp",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-mcp-active"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-mcp-scene"));
        editor.Workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-human-dirty",
            ProjectWorkspaceActorKind.Human,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-human-dirty",
            [WorkspaceTransactionDocumentEdit.ReplaceText("scenes/main.scene.json", new ProjectDocumentRevision(1), SceneText(speed: 11))]));

        using var session = McpServerSession.Open(projectRoot, registry, FixedInstant.AddSeconds(1));
        var openDocuments = session.ReadResource("electron2d://workspace/open-documents");

        Assert.Equal(McpRoute.ActiveEditor, session.Route);
        Assert.Contains("scenes/main.scene.json", openDocuments.Content["dirtyDocuments"]!.AsArray().Select(item => item!.GetValue<string>()));

        var result = session.CallTool(new McpToolRequest("workspace_apply_transaction", new Dictionary<string, string>
        {
            ["path"] = "scenes/main.scene.json",
            ["expectedRevision"] = "2",
            ["text"] = SceneText(speed: 12)
        }));

        Assert.True(result.Succeeded);
        Assert.Equal(McpRoute.ActiveEditor, result.Route);
        Assert.Contains("scenes/main.scene.json", result.Operation!["dirtyDocuments"]!.AsArray().Select(item => item!.GetValue<string>()));
        Assert.Contains("\"value\": 12", editor.Workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.Contains("\"value\": 10", File.ReadAllText(Path.Combine(projectRoot, "scenes", "main.scene.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void HeadlessFallbackAndJobToolsExposeSnapshotIdentityEvents()
    {
        var projectRoot = CreateProjectRoot("headless-job", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        using var session = McpServerSession.Open(projectRoot, registry: null, FixedInstant);

        var result = session.CallTool(new McpToolRequest("project_build", new Dictionary<string, string>
        {
            ["inputBuildConfigurationHash"] = "sha256:mcp-build"
        }));

        Assert.True(result.Succeeded);
        Assert.Equal(McpRoute.Headless, result.Route);
        var queued = Assert.Single(result.JobEvents);
        Assert.Equal("operation.queued", queued.EventName);
        Assert.Equal("Build", queued.JobKind);
        Assert.Equal("Queued", queued.JobState);
        Assert.Equal("sha256:mcp-build", queued.InputBuildConfigurationHash);
        Assert.False(queued.Stale);
        Assert.Equal(1, queued.InputDocumentRevisions["scenes/main.scene.json"].Value);
    }

    [Fact]
    public void RuntimeToolsControlActiveEditorSessionAndExposeRuntimeResource()
    {
        var projectRoot = CreateProjectRoot("runtime-tools", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        var registry = new EditorSessionRegistry(TimeSpan.FromSeconds(30));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-runtime",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-mcp-runtime"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-runtime-scene"));
        using var session = McpServerSession.Open(projectRoot, registry, FixedInstant.AddSeconds(1));

        var started = session.CallTool(new McpToolRequest("runtime_start", new Dictionary<string, string>
        {
            ["scene"] = "scenes/main.scene.json",
            ["inputBuildConfigurationHash"] = "sha256:mcp-runtime"
        }));
        var paused = session.CallTool(new McpToolRequest("runtime_pause", new Dictionary<string, string>()));
        var stepped = session.CallTool(new McpToolRequest("runtime_step", new Dictionary<string, string>
        {
            ["kind"] = "frame",
            ["count"] = "2",
            ["fixedDelta"] = "0.25"
        }));
        var input = session.CallTool(new McpToolRequest("runtime_inject_input", new Dictionary<string, string>
        {
            ["action"] = "jump",
            ["state"] = "pressed"
        }));
        var highlighted = session.CallTool(new McpToolRequest("runtime_highlight_node", new Dictionary<string, string>
        {
            ["nodePath"] = "/Player"
        }));
        var tree = session.CallTool(new McpToolRequest("runtime_get_scene_tree", new Dictionary<string, string>()));
        var captured = session.CallTool(new McpToolRequest("runtime_capture_frame", new Dictionary<string, string>()));
        var resource = session.ReadResource("electron2d://runtime/session");

        Assert.True(started.Succeeded);
        Assert.True(paused.Succeeded);
        Assert.True(stepped.Succeeded);
        Assert.True(input.Succeeded);
        Assert.True(highlighted.Succeeded);
        Assert.True(tree.Succeeded);
        Assert.True(captured.Succeeded);
        Assert.Equal(McpRoute.ActiveEditor, started.Route);
        Assert.Single(started.JobEvents);
        Assert.Equal("EditorAttachedPreview", resource.Content["session"]!["sessionKind"]!.GetValue<string>());
        Assert.Equal("Paused", resource.Content["session"]!["state"]!.GetValue<string>());
        Assert.Equal(2, resource.Content["metrics"]!["currentFrame"]!.GetValue<int>());
        Assert.Equal("/Player", resource.Content["highlightedNodePath"]!.GetValue<string>());
        Assert.True(resource.Content["inputSnapshotId"]!.GetValue<string>().Length > 0);
        Assert.Contains(tree.Content["sceneTree"]!["nodes"]!.AsArray(), node => node!["path"]!.GetValue<string>() == "/Player");
        Assert.Equal("image/png", captured.Content["screenshot"]!["contentType"]!.GetValue<string>());

        var crashed = session.CallTool(new McpToolRequest("runtime_report_crash", new Dictionary<string, string>
        {
            ["exitCode"] = "13",
            ["stderr"] = "boom"
        }));
        var diagnostics = session.CallTool(new McpToolRequest("runtime_get_diagnostics", new Dictionary<string, string>()));

        Assert.True(crashed.Succeeded);
        Assert.True(diagnostics.Succeeded);
        Assert.Equal("Crashed", session.ReadResource("electron2d://runtime/session").Content["session"]!["state"]!.GetValue<string>());
        Assert.Contains(diagnostics.Diagnostics, diagnostic => diagnostic.Code == "E2D-RUNTIME-0001");
    }

    [Fact]
    public void TaskToolsUseTaskServiceAndRejectAgentAcceptance()
    {
        var projectRoot = CreateProjectRoot("tasks", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.Review));
        var registry = new EditorSessionRegistry(TimeSpan.FromSeconds(30));
        using var editor = registry.OpenEditorSession(
            projectRoot,
            "editor-task",
            EditorSessionEndpoint.NamedPipe(@"\\.\pipe\electron2d-mcp-task"),
            FixedInstant);
        editor.Workspace.CommandBus.OpenTextDocument(
            ".electron2d/tasks/task-alpha.e2task",
            TaskText("task-alpha", ProjectTaskStatus.Review),
            1,
            ProjectWorkspaceOperationContext.ForTest("open-mcp-task"));
        using var session = McpServerSession.Open(projectRoot, registry, FixedInstant.AddSeconds(1));

        var submitted = session.CallTool(new McpToolRequest("task_submit_for_acceptance", new Dictionary<string, string>
        {
            ["taskId"] = "task-alpha",
            ["expectedRevision"] = "1"
        }));
        var accepted = session.CallTool(new McpToolRequest("task_accept", new Dictionary<string, string>
        {
            ["taskId"] = "task-alpha",
            ["expectedRevision"] = "2"
        }));

        Assert.True(submitted.Succeeded);
        Assert.False(accepted.Succeeded);
        Assert.Contains(accepted.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0002");
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, editor.Workspace.Tasks.GetTask("task-alpha").Status);
    }

    [Fact]
    public void CliMcpServeOutputsManifestWithoutCloudProvider()
    {
        var projectRoot = CreateProjectRoot("cli-mcp", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        using var output = new StringWriter();
        using var error = new StringWriter();

        var exitCode = Electron2DCommandLine.Run(
            ["mcp", "serve", "--project", projectRoot, "--format", "json"],
            output,
            error,
            CliExecutionContext.ForTests(FixedInstant));

        Assert.Equal(0, exitCode);
        Assert.Empty(error.ToString());
        using var json = JsonDocument.Parse(output.ToString());
        var root = json.RootElement;
        Assert.True(root.GetProperty("succeeded").GetBoolean());
        Assert.Equal("mcp serve", root.GetProperty("command").GetString());
        Assert.Equal("headless", root.GetProperty("route").GetString());
        Assert.Contains(root.GetProperty("data").GetProperty("tools").EnumerateArray(), tool => tool.GetProperty("name").GetString() == "workspace_apply_transaction");
        Assert.Contains(root.GetProperty("data").GetProperty("resources").EnumerateArray(), resource => resource.GetProperty("uri").GetString() == "electron2d://project/summary");
        Assert.False(root.GetProperty("data").GetProperty("cloudProviderRequired").GetBoolean());
        var capabilities = root.GetProperty("data").GetProperty("editorCapabilityManifest");
        Assert.True(capabilities.GetProperty("succeeded").GetBoolean());
        Assert.Equal("data/editor/electron2d-editor-capabilities.json", capabilities.GetProperty("path").GetString());
        Assert.True(capabilities.GetProperty("capabilities").GetInt32() >= 18);
    }

    private static string CreateProjectRoot(string name, string sceneText, string taskText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-McpServerTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        Directory.CreateDirectory(Path.Combine(root, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), sceneText);
        File.WriteAllText(Path.Combine(root, ".electron2d", "tasks", "task-alpha.e2task"), taskText);
        return root;
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

    private static string TaskText(string taskId, ProjectTaskStatus status)
    {
        var task = new ProjectTask
        {
            TaskId = taskId,
            Title = $"Task {taskId}",
            Description = "Exercise MCP task tools.",
            Status = status,
            Readiness = TaskReadiness.Ready,
            Priority = "P0",
            Rank = "1000",
            Assignee = "agent-1",
            CreatedBy = "user-1",
            CreatedAt = FixedInstant,
            UpdatedAt = FixedInstant,
            AcceptanceState = ProjectTaskAcceptanceState.Open
        };
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            "criterion-mcp",
            "MCP task tool is covered by focused tests.",
            AcceptanceCriterionState.Open,
            []));
        return ProjectTaskSerializer.Serialize(task);
    }
}
