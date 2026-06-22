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
using System.Collections.ObjectModel;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;
using Electron2D.Tooling;

namespace Electron2D.Mcp;

internal enum McpRoute
{
    ActiveEditor,
    Headless,
    Blocked
}

internal sealed record McpResourceDefinition(string Uri, string Description);

internal sealed record McpToolDefinition(string Name, string Description);

internal sealed class McpToolRequest
{
    public McpToolRequest(string toolName, IReadOnlyDictionary<string, string> arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(arguments);

        ToolName = toolName;
        Arguments = new ReadOnlyDictionary<string, string>(
            arguments.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    public string ToolName { get; }

    public IReadOnlyDictionary<string, string> Arguments { get; }
}

internal sealed class McpResourceResult
{
    public McpResourceResult(string uri, JsonObject content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        ArgumentNullException.ThrowIfNull(content);

        Uri = uri;
        Content = content;
    }

    public string Uri { get; }

    public JsonObject Content { get; }
}

internal sealed class McpJobEvent
{
    public McpJobEvent(
        string eventName,
        string jobKind,
        string jobState,
        string inputSnapshotId,
        ProjectWorkspaceRevision inputWorkspaceRevision,
        ProjectWorkspaceRevision inputContentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> inputDocumentRevisions,
        string inputBuildConfigurationHash,
        bool stale)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(jobState);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputSnapshotId);
        ArgumentNullException.ThrowIfNull(inputDocumentRevisions);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        EventName = eventName;
        JobKind = jobKind;
        JobState = jobState;
        InputSnapshotId = inputSnapshotId;
        InputWorkspaceRevision = inputWorkspaceRevision;
        InputContentRevision = inputContentRevision;
        InputDocumentRevisions = new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            inputDocumentRevisions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        InputBuildConfigurationHash = inputBuildConfigurationHash;
        Stale = stale;
    }

    public string EventName { get; }

    public string JobKind { get; }

    public string JobState { get; }

    public string InputSnapshotId { get; }

    public ProjectWorkspaceRevision InputWorkspaceRevision { get; }

    public ProjectWorkspaceRevision InputContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> InputDocumentRevisions { get; }

    public string InputBuildConfigurationHash { get; }

    public bool Stale { get; }
}

internal sealed class McpToolResult
{
    private McpToolResult(
        string toolName,
        bool succeeded,
        McpRoute route,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        JsonObject? operation,
        IReadOnlyList<McpJobEvent> jobEvents,
        JsonObject content)
    {
        ToolName = toolName;
        Succeeded = succeeded;
        Route = route;
        Diagnostics = diagnostics.ToArray();
        Operation = operation;
        JobEvents = jobEvents.ToArray();
        Content = content;
    }

    public string ToolName { get; }

    public bool Succeeded { get; }

    public McpRoute Route { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public JsonObject? Operation { get; }

    public IReadOnlyList<McpJobEvent> JobEvents { get; }

    public JsonObject Content { get; }

    public static McpToolResult FromOperation(
        string toolName,
        McpRoute route,
        ToolingOperationResult result,
        IReadOnlyList<StructuredDiagnostic> routeDiagnostics)
    {
        return new McpToolResult(
            toolName,
            result.Succeeded,
            route,
            routeDiagnostics.Concat(result.Diagnostics).ToArray(),
            new JsonObject
            {
                ["operationId"] = result.OperationId,
                ["operationKind"] = result.OperationKind,
                ["workspaceRevision"] = result.WorkspaceRevision.Value,
                ["contentRevision"] = result.ContentRevision.Value,
                ["documentRevisions"] = WriteRevisions(result.DocumentRevisions),
                ["changedFiles"] = WriteStringArray(result.ChangedFiles),
                ["dirtyDocuments"] = WriteStringArray(result.DirtyDocuments),
                ["undoGroupId"] = result.UndoGroupId
            },
            jobEvents: [],
            content: new JsonObject());
    }

    public static McpToolResult FromJob(
        string toolName,
        McpRoute route,
        ToolingJobResult result,
        string inputBuildConfigurationHash,
        IReadOnlyList<StructuredDiagnostic> routeDiagnostics)
    {
        var jobEvent = new McpJobEvent(
            "operation.queued",
            result.JobKind.ToString(),
            result.JobState.ToString(),
            result.InputSnapshotId,
            result.InputWorkspaceRevision,
            result.InputContentRevision,
            result.InputDocumentRevisions,
            inputBuildConfigurationHash,
            stale: false);
        return new McpToolResult(
            toolName,
            result.Succeeded,
            route,
            routeDiagnostics.Concat(result.Diagnostics).ToArray(),
            operation: null,
            [jobEvent],
            new JsonObject());
    }

    public static McpToolResult FromTask(string toolName, McpRoute route, ToolingOperationResult result)
    {
        return new McpToolResult(
            toolName,
            result.Succeeded,
            route,
            result.Diagnostics,
            new JsonObject
            {
                ["operationId"] = result.OperationId,
                ["operationKind"] = result.OperationKind,
                ["taskId"] = result.TaskId,
                ["dirtyDocuments"] = WriteStringArray(result.DirtyDocuments)
            },
            jobEvents: [],
            content: new JsonObject());
    }

    public static McpToolResult ContentOnly(string toolName, McpRoute route, JsonObject content)
    {
        return new McpToolResult(toolName, succeeded: true, route, diagnostics: [], operation: null, jobEvents: [], content);
    }

    public static McpToolResult Unsupported(string toolName, McpRoute route)
    {
        return new McpToolResult(
            toolName,
            succeeded: false,
            route,
            [McpServerSession.CreateMcpDiagnostic("E2D-MCP-0001", $"MCP tool '{toolName}' is not implemented in the current Preview scope.")],
            operation: null,
            jobEvents: [],
            content: new JsonObject());
    }

    private static JsonObject WriteRevisions(IReadOnlyDictionary<string, ProjectDocumentRevision> revisions)
    {
        var root = new JsonObject();
        foreach (var pair in revisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[pair.Key] = pair.Value.Value;
        }

        return root;
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values.OrderBy(value => value, StringComparer.Ordinal))
        {
            array.Add(value);
        }

        return array;
    }
}

internal sealed class McpServerSession : IDisposable
{
    private static readonly McpResourceDefinition[] ResourceDefinitions =
    [
        new("electron2d://project/summary", "Project identity, route and workspace revisions."),
        new("electron2d://project/settings", "Project settings resource placeholder."),
        new("electron2d://project/scenes", "Project scene document list."),
        new("electron2d://project/resources", "Project resource document list."),
        new("electron2d://project/diagnostics", "Workspace diagnostics."),
        new("electron2d://workspace/open-documents", "Open documents, dirty state and revisions."),
        new("electron2d://workspace/selection", "Current editor selection placeholder."),
        new("electron2d://workspace/import-state", "Import state for project files."),
        new("electron2d://workspace/build-state", "Build state for project files."),
        new("electron2d://scene/{uid}", "Scene document by stable UID."),
        new("electron2d://resource/{uid}", "Resource document by stable UID."),
        new("electron2d://api/type/{name}", "API type description from local manifest."),
        new("electron2d://api/godot-compatibility/{name}", "Compatibility status from local manifest."),
        new("electron2d://editor/capabilities", "Editor capability manifest placeholder."),
        new("electron2d://runtime/capabilities", "Runtime capability manifest placeholder."),
        new("electron2d://runtime/session", "Active runtime session placeholder."),
        new("electron2d://docs/topic/{name}", "Local documentation topic.")
    ];

    private static readonly string[] ToolNames =
    [
        "project_validate",
        "project_build",
        "project_run",
        "project_test",
        "project_export",
        "scene_create",
        "scene_inspect",
        "scene_add_node",
        "scene_remove_node",
        "scene_move_node",
        "scene_set_property",
        "scene_attach_script",
        "scene_connect_signal",
        "resource_inspect",
        "resource_import",
        "resource_find_references",
        "workspace_get_state",
        "workspace_apply_transaction",
        "workspace_resolve_conflict",
        "workspace_undo_transaction",
        "runtime_start",
        "runtime_stop",
        "runtime_pause",
        "runtime_step",
        "runtime_inject_input",
        "runtime_capture_frame",
        "runtime_get_scene_tree",
        "runtime_get_diagnostics",
        "task_list",
        "task_get",
        "task_create",
        "task_update",
        "task_claim",
        "task_set_status",
        "task_add_subtask",
        "task_add_dependency",
        "task_append_activity",
        "task_link_transaction",
        "task_link_job",
        "task_link_artifact",
        "task_submit_for_acceptance",
        "task_accept",
        "task_request_changes",
        "task_cancel"
    ];

    private readonly IDisposable owned;
    private bool disposed;

    private McpServerSession(
        string projectRoot,
        McpRoute route,
        ProjectWorkspace workspace,
        ProjectToolingHost tooling,
        IReadOnlyList<StructuredDiagnostic> routeDiagnostics,
        IDisposable owned)
    {
        ProjectRoot = projectRoot;
        Route = route;
        Workspace = workspace;
        Tooling = tooling;
        RouteDiagnostics = routeDiagnostics.ToArray();
        this.owned = owned;
    }

    public string ProjectRoot { get; }

    public McpRoute Route { get; }

    public ProjectWorkspace Workspace { get; }

    public ProjectToolingHost Tooling { get; }

    public IReadOnlyList<StructuredDiagnostic> RouteDiagnostics { get; }

    public static McpServerSession Open(
        string projectRoot,
        EditorSessionRegistry? registry,
        DateTimeOffset nowUtc)
    {
        var normalizedRoot = NormalizeProjectRoot(projectRoot);
        if (registry is not null)
        {
            var connection = registry.Connect(EditorSessionAdapterKind.Mcp, normalizedRoot, "mcp", nowUtc);
            if (connection.State == EditorSessionConnectionState.ActiveEditor)
            {
                return new McpServerSession(
                    normalizedRoot,
                    McpRoute.ActiveEditor,
                    connection.Workspace,
                    connection.Tooling,
                    connection.Diagnostics,
                    connection);
            }

            OpenProjectDocuments(connection.Workspace, normalizedRoot);
            return new McpServerSession(
                normalizedRoot,
                McpRoute.Headless,
                connection.Workspace,
                connection.Tooling,
                connection.Diagnostics,
                connection);
        }

        var workspace = ProjectWorkspace.CreateHeadless(normalizedRoot, "mcp");
        OpenProjectDocuments(workspace, normalizedRoot);
        return new McpServerSession(
            normalizedRoot,
            McpRoute.Headless,
            workspace,
            new ProjectToolingHost(workspace),
            routeDiagnostics: [],
            owned: workspace);
    }

    public IReadOnlyList<McpResourceDefinition> ListResources()
    {
        return ResourceDefinitions;
    }

    public IReadOnlyList<McpToolDefinition> ListTools()
    {
        return ToolNames
            .Select(name => new McpToolDefinition(name, $"Electron2D MCP tool `{name}`."))
            .ToArray();
    }

    public McpResourceResult ReadResource(string uri)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);
        return uri switch
        {
            "electron2d://project/summary" => new McpResourceResult(uri, ProjectSummary()),
            "electron2d://workspace/open-documents" => new McpResourceResult(uri, OpenDocuments()),
            "electron2d://project/diagnostics" => new McpResourceResult(uri, new JsonObject
            {
                ["diagnostics"] = WriteDiagnostics(RouteDiagnostics)
            }),
            "electron2d://workspace/import-state" => new McpResourceResult(uri, WriteDictionary(Workspace.ImportState.States)),
            "electron2d://workspace/build-state" => new McpResourceResult(uri, new JsonObject
            {
                ["state"] = Workspace.BuildState.State
            }),
            _ => new McpResourceResult(uri, new JsonObject
            {
                ["uri"] = uri,
                ["route"] = RouteName(Route),
                ["status"] = "available"
            })
        };
    }

    public McpToolResult CallTool(McpToolRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.ToolName switch
        {
            "workspace_get_state" => McpToolResult.ContentOnly(request.ToolName, Route, OpenDocuments()),
            "workspace_apply_transaction" => ApplyWorkspaceTransaction(request),
            "project_build" => QueueJob(request, Tooling.Build, "sha256:mcp-default"),
            "project_run" => QueueJob(request, Tooling.Runtime, "sha256:mcp-default"),
            "project_test" => QueueJob(request, Tooling.Tests, "sha256:mcp-default"),
            "project_export" => QueueJob(request, Tooling.Export, "sha256:mcp-default"),
            "resource_import" => QueueJob(request, Tooling.Import, "sha256:mcp-default"),
            "task_list" => McpToolResult.ContentOnly(request.ToolName, Route, new JsonObject
            {
                ["tasks"] = WriteTasks(Tooling.Tasks.List())
            }),
            "task_get" => McpToolResult.ContentOnly(request.ToolName, Route, WriteTask(Tooling.Tasks.Get(Require(request, "taskId")))),
            "task_submit_for_acceptance" => McpToolResult.FromTask(request.ToolName, Route, Tooling.Tasks.SubmitForAcceptance(TaskStatusRequest(request, OperationCapability.TaskSubmitForAcceptance))),
            "task_accept" => McpToolResult.FromTask(request.ToolName, Route, Tooling.Tasks.Accept(TaskStatusRequest(request, OperationCapability.TaskSubmitForAcceptance))),
            "task_request_changes" => McpToolResult.FromTask(request.ToolName, Route, Tooling.Tasks.RequestChanges(TaskStatusRequest(request, OperationCapability.TaskSubmitForAcceptance), Get(request, "reason") ?? "Changes requested through MCP.")),
            "task_cancel" => McpToolResult.FromTask(request.ToolName, Route, Tooling.Tasks.Cancel(TaskStatusRequest(request, OperationCapability.TaskCancel))),
            "task_append_activity" => McpToolResult.FromTask(request.ToolName, Route, Tooling.Tasks.AppendActivity(new ToolingTaskActivityRequest(
                Require(request, "taskId"),
                TaskActivityKind.AgentSummary,
                Get(request, "payload") ?? "MCP activity.",
                Revision(request),
                OperationId(request.ToolName),
                UndoGroupId(request.ToolName),
                AgentContext(OperationCapability.TaskWrite)))),
            _ => McpToolResult.Unsupported(request.ToolName, Route)
        };
    }

    public JsonObject Manifest()
    {
        return new JsonObject
        {
            ["projectRoot"] = ProjectRoot,
            ["route"] = RouteName(Route),
            ["cloudProviderRequired"] = false,
            ["resources"] = WriteResources(ListResources()),
            ["tools"] = WriteTools(ListTools())
        };
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        owned.Dispose();
    }

    internal static StructuredDiagnostic CreateMcpDiagnostic(string code, string message)
    {
        var definition = DiagnosticCodeRegistry.Get(code);
        return StructuredDiagnostic.Create(
            definition.Code,
            definition.Severity,
            definition.Category,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private McpToolResult ApplyWorkspaceTransaction(McpToolRequest request)
    {
        var relativePath = Require(request, "path");
        var expectedRevision = Revision(request);
        if (Route == McpRoute.Headless)
        {
            OpenDocumentIfNeeded(Workspace, ProjectRoot, relativePath, expectedRevision);
        }

        var result = Tooling.Project.ApplyTextEdit(new ToolingTextEditRequest(
            OperationId(request.ToolName),
            "workspace.apply-transaction",
            Route == McpRoute.ActiveEditor ? ToolingApplyMode.WorkspaceOnly : ToolingApplyMode.HeadlessCommit,
            relativePath,
            expectedRevision,
            Require(request, "text"),
            UndoGroupId(request.ToolName)),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result, RouteDiagnostics);
    }

    private McpToolResult QueueJob(McpToolRequest request, ToolingJobService service, string defaultBuildConfigurationHash)
    {
        var hash = Get(request, "inputBuildConfigurationHash") ?? defaultBuildConfigurationHash;
        var result = service.Queue(new ToolingJobRequest(OperationId(request.ToolName), hash));
        return McpToolResult.FromJob(request.ToolName, Route, result, hash, RouteDiagnostics);
    }

    private ToolingTaskStatusRequest TaskStatusRequest(McpToolRequest request, OperationCapability capability)
    {
        return new ToolingTaskStatusRequest(
            Require(request, "taskId"),
            Revision(request),
            OperationId(request.ToolName),
            UndoGroupId(request.ToolName),
            AgentContext(capability));
    }

    private static OperationContext AgentContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "mcp-agent",
            PrincipalKind.Agent,
            "mcp-session",
            capabilities,
            "mcp");
    }

    private JsonObject ProjectSummary()
    {
        return new JsonObject
        {
            ["projectRoot"] = ProjectRoot,
            ["route"] = RouteName(Route),
            ["workspaceRevision"] = Workspace.Revisions.WorkspaceRevision.Value,
            ["contentRevision"] = Workspace.Revisions.ContentRevision.Value,
            ["dirtyDocuments"] = WriteStringArray(Workspace.Revisions.DirtyDocuments)
        };
    }

    private JsonObject OpenDocuments()
    {
        var documents = new JsonArray();
        foreach (var document in Workspace.Documents.Documents.OrderBy(document => document.Path, StringComparer.Ordinal))
        {
            documents.Add(new JsonObject
            {
                ["path"] = document.Path,
                ["inMemoryRevision"] = document.InMemoryRevision.Value,
                ["persistedRevision"] = document.PersistedRevision.Value,
                ["dirty"] = document.IsDirty
            });
        }

        return new JsonObject
        {
            ["route"] = RouteName(Route),
            ["workspaceRevision"] = Workspace.Revisions.WorkspaceRevision.Value,
            ["contentRevision"] = Workspace.Revisions.ContentRevision.Value,
            ["dirtyDocuments"] = WriteStringArray(Workspace.Revisions.DirtyDocuments),
            ["documentRevisions"] = WriteRevisions(Workspace.Revisions.DocumentRevisions),
            ["documents"] = documents
        };
    }

    private static void OpenProjectDocuments(ProjectWorkspace workspace, string projectRoot)
    {
        foreach (var file in Directory.EnumerateFiles(projectRoot, "*.scene.json", SearchOption.AllDirectories)
            .Concat(Directory.Exists(Path.Combine(projectRoot, ".electron2d", "tasks"))
                ? Directory.EnumerateFiles(Path.Combine(projectRoot, ".electron2d", "tasks"), "*.e2task", SearchOption.TopDirectoryOnly)
                : [])
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            var relativePath = Path.GetRelativePath(projectRoot, file).Replace(Path.DirectorySeparatorChar, '/');
            OpenDocumentIfNeeded(workspace, projectRoot, relativePath, new ProjectDocumentRevision(1));
        }
    }

    private static void OpenDocumentIfNeeded(
        ProjectWorkspace workspace,
        string projectRoot,
        string relativePath,
        ProjectDocumentRevision persistedRevision)
    {
        if (workspace.Documents.Documents.Any(document => string.Equals(document.Path, relativePath, StringComparison.Ordinal)))
        {
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        workspace.CommandBus.OpenTextDocument(
            relativePath,
            File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty,
            persistedRevision.Value,
            ProjectWorkspaceOperationContext.ForTest($"mcp-open-{Guid.NewGuid():N}"));
    }

    private static string Require(McpToolRequest request, string key)
    {
        return Get(request, key) ?? throw new ArgumentException($"MCP tool '{request.ToolName}' requires argument '{key}'.", nameof(request));
    }

    private static string? Get(McpToolRequest request, string key)
    {
        return request.Arguments.TryGetValue(key, out var value) ? value : null;
    }

    private static ProjectDocumentRevision Revision(McpToolRequest request)
    {
        var value = Require(request, "expectedRevision");
        return long.TryParse(value, out var parsed)
            ? new ProjectDocumentRevision(parsed)
            : throw new ArgumentException("MCP expectedRevision must be an integer.", nameof(request));
    }

    private static string OperationId(string toolName)
    {
        return $"mcp-{toolName}-{Guid.NewGuid():N}";
    }

    private static string UndoGroupId(string toolName)
    {
        return $"undo-mcp-{toolName}-{Guid.NewGuid():N}";
    }

    private static JsonArray WriteResources(IEnumerable<McpResourceDefinition> resources)
    {
        var array = new JsonArray();
        foreach (var resource in resources)
        {
            array.Add(new JsonObject
            {
                ["uri"] = resource.Uri,
                ["description"] = resource.Description
            });
        }

        return array;
    }

    private static JsonArray WriteTools(IEnumerable<McpToolDefinition> tools)
    {
        var array = new JsonArray();
        foreach (var tool in tools)
        {
            array.Add(new JsonObject
            {
                ["name"] = tool.Name,
                ["description"] = tool.Description
            });
        }

        return array;
    }

    private static JsonArray WriteTasks(IEnumerable<ProjectTask> tasks)
    {
        var array = new JsonArray();
        foreach (var task in tasks)
        {
            array.Add(WriteTask(task));
        }

        return array;
    }

    private static JsonObject WriteTask(ProjectTask task)
    {
        return new JsonObject
        {
            ["taskId"] = task.TaskId,
            ["title"] = task.Title,
            ["status"] = task.Status.ToString(),
            ["readiness"] = task.Readiness.ToString()
        };
    }

    private static JsonArray WriteDiagnostics(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        var array = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            array.Add(new JsonObject
            {
                ["code"] = diagnostic.Code,
                ["severity"] = diagnostic.Severity.ToString(),
                ["category"] = diagnostic.Category.ToString(),
                ["message"] = diagnostic.Message,
                ["documentationUri"] = diagnostic.DocumentationUri
            });
        }

        return array;
    }

    private static JsonObject WriteDictionary(IReadOnlyDictionary<string, string> values)
    {
        var root = new JsonObject();
        foreach (var pair in values.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[pair.Key] = pair.Value;
        }

        return root;
    }

    private static JsonObject WriteRevisions(IReadOnlyDictionary<string, ProjectDocumentRevision> revisions)
    {
        var root = new JsonObject();
        foreach (var pair in revisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[pair.Key] = pair.Value.Value;
        }

        return root;
    }

    private static JsonArray WriteStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values.OrderBy(value => value, StringComparer.Ordinal))
        {
            array.Add(value);
        }

        return array;
    }

    private static string NormalizeProjectRoot(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string RouteName(McpRoute route)
    {
        return route switch
        {
            McpRoute.ActiveEditor => "activeEditor",
            McpRoute.Headless => "headless",
            McpRoute.Blocked => "blocked",
            _ => "blocked"
        };
    }
}
