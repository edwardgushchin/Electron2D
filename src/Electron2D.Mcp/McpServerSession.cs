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

    public static McpToolResult FromRuntime(
        string toolName,
        McpRoute route,
        bool succeeded,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        ToolingJobResult? job,
        JsonObject content)
    {
        McpJobEvent[] jobEvents = job is null
            ? []
            :
            [
                new McpJobEvent(
                    "operation.queued",
                    job.JobKind.ToString(),
                    job.JobState.ToString(),
                    job.InputSnapshotId,
                    job.InputWorkspaceRevision,
                    job.InputContentRevision,
                    job.InputDocumentRevisions,
                    job.InputBuildConfigurationHash,
                    stale: false)
            ];
        return new McpToolResult(
            toolName,
            succeeded,
            route,
            diagnostics,
            operation: null,
            jobEvents,
            content);
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
        new("electron2d://editor/capabilities", "Editor capability manifest."),
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
        "runtime_resume",
        "runtime_step",
        "runtime_inject_input",
        "runtime_capture_frame",
        "runtime_get_scene_tree",
        "runtime_get_diagnostics",
        "runtime_highlight_node",
        "runtime_report_crash",
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
        "task_cancel",
        "script_create",
        "script_open",
        "script_read",
        "script_rename",
        "script_delete",
        "script_search_text",
        "script_apply_text_edits",
        "script_save",
        "script_format",
        "script_get_diagnostics",
        "script_get_completions",
        "script_get_signature_help",
        "script_get_hover",
        "script_get_definition",
        "script_get_document_symbols",
        "script_find_references",
        "script_rename_symbol",
        "script_get_code_actions",
        "script_apply_code_action",
        "debug_set_breakpoint",
        "debug_update_breakpoint",
        "debug_remove_breakpoint",
        "debug_start",
        "debug_attach",
        "debug_restart",
        "debug_pause",
        "debug_continue",
        "debug_step_into",
        "debug_step_over",
        "debug_step_out",
        "debug_get_threads",
        "debug_get_stack",
        "debug_get_locals",
        "debug_get_arguments",
        "debug_get_watches",
        "debug_evaluate_watches",
        "debug_add_watch",
        "debug_update_watch",
        "debug_remove_watch",
        "debug_stop"
    ];

    public static IReadOnlyList<string> DefaultToolNames => ToolNames;

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
        var taskTools = Tooling.Tasks.SupportedCommands.ToHashSet(StringComparer.Ordinal);
        return ToolNames
            .Where(name => !name.StartsWith("task_", StringComparison.Ordinal) || taskTools.Contains(name))
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
            "electron2d://runtime/session" => new McpResourceResult(uri, RuntimeSession()),
            "electron2d://editor/capabilities" => new McpResourceResult(uri, EditorCapabilities()),
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
            "runtime_start" => RuntimeStart(request),
            "runtime_stop" => RuntimeCommand(request, Tooling.Runtime.Stop),
            "runtime_pause" => RuntimeCommand(request, Tooling.Runtime.Pause),
            "runtime_resume" => RuntimeCommand(request, Tooling.Runtime.Resume),
            "runtime_step" => RuntimeStep(request),
            "runtime_inject_input" => RuntimeInjectInput(request),
            "runtime_capture_frame" => RuntimeCaptureFrame(request),
            "runtime_get_scene_tree" => RuntimeSceneTree(request),
            "runtime_get_diagnostics" => RuntimeCommand(request, Tooling.Runtime.GetDiagnostics),
            "runtime_highlight_node" => RuntimeHighlightNode(request),
            "runtime_report_crash" => RuntimeReportCrash(request),
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
            "script_create" => ScriptCreate(request),
            "script_open" => ScriptRead(request),
            "script_read" => ScriptRead(request),
            "script_rename" => ScriptRename(request),
            "script_delete" => ScriptDelete(request),
            "script_search_text" => ScriptSearchText(request),
            "script_apply_text_edits" => ScriptApplyTextEdits(request),
            "script_save" => ScriptSave(request),
            "script_format" => ScriptFormat(request),
            "script_get_diagnostics" => ScriptIde(request, Tooling.Script.GetDiagnostics),
            "script_get_completions" => ScriptIde(request, Tooling.Script.GetCompletions),
            "script_get_signature_help" => ScriptIde(request, Tooling.Script.GetSignatureHelp),
            "script_get_hover" => ScriptIde(request, Tooling.Script.GetHover),
            "script_get_definition" => ScriptIde(request, Tooling.Script.GetDefinition),
            "script_get_document_symbols" => ScriptIde(request, Tooling.Script.GetDocumentSymbols),
            "script_find_references" => ScriptIde(request, Tooling.Script.FindReferences),
            "script_rename_symbol" => ScriptRenameSymbol(request),
            "script_get_code_actions" => ScriptIde(request, Tooling.Script.GetCodeActions),
            "script_apply_code_action" => ScriptApplyCodeAction(request),
            "debug_set_breakpoint" => DebugSetBreakpoint(request),
            "debug_update_breakpoint" => DebugUpdateBreakpoint(request),
            "debug_remove_breakpoint" => DebugRemoveBreakpoint(request),
            "debug_start" => DebugStart(request),
            "debug_attach" => DebugAttach(request),
            "debug_restart" => DebugRestart(request),
            "debug_pause" => DebugCommand(request, Tooling.Debug.Pause),
            "debug_continue" => DebugCommand(request, Tooling.Debug.Continue),
            "debug_step_into" => DebugCommand(request, Tooling.Debug.StepInto),
            "debug_step_over" => DebugCommand(request, Tooling.Debug.StepOver),
            "debug_step_out" => DebugCommand(request, Tooling.Debug.StepOut),
            "debug_get_threads" => McpToolResult.ContentOnly(request.ToolName, Route, new JsonObject
            {
                ["threads"] = WriteDebugThreads(Tooling.Debug.GetThreads())
            }),
            "debug_get_stack" => DebugGetStack(request),
            "debug_get_locals" => DebugVariables(request, Tooling.Debug.GetLocals),
            "debug_get_arguments" => DebugVariables(request, Tooling.Debug.GetArguments),
            "debug_get_watches" => DebugGetWatches(request),
            "debug_evaluate_watches" => DebugEvaluateWatches(request),
            "debug_add_watch" => DebugAddWatch(request),
            "debug_update_watch" => DebugUpdateWatch(request),
            "debug_remove_watch" => DebugRemoveWatch(request),
            "debug_stop" => DebugCommand(request, Tooling.Debug.Stop),
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
            ["tools"] = WriteTools(ListTools()),
            ["editorCapabilityManifest"] = EditorCapabilitySummary()
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

    private McpToolResult QueueJob(McpToolRequest request, ToolingRuntimeService service, string defaultBuildConfigurationHash)
    {
        var hash = Get(request, "inputBuildConfigurationHash") ?? defaultBuildConfigurationHash;
        var result = service.Queue(new ToolingJobRequest(OperationId(request.ToolName), hash));
        return McpToolResult.FromJob(request.ToolName, Route, result, hash, RouteDiagnostics);
    }

    private McpToolResult RuntimeStart(McpToolRequest request)
    {
        var hash = Get(request, "inputBuildConfigurationHash") ?? "sha256:mcp-runtime";
        var result = Tooling.Runtime.StartEditorAttached(new ToolingRuntimeStartRequest(
            OperationId(request.ToolName),
            Get(request, "scene") ?? "scenes/main.scene.json",
            hash,
            RuntimeVisibleMode.SeparateWindow));
        return McpToolResult.FromRuntime(
            request.ToolName,
            Route,
            result.Succeeded,
            result.Diagnostics,
            result.Job,
            RuntimeSession());
    }

    private McpToolResult RuntimeCommand(McpToolRequest request, Func<ToolingRuntimeCommandResult> command)
    {
        var result = command();
        return McpToolResult.FromRuntime(
            request.ToolName,
            Route,
            result.Succeeded,
            result.Diagnostics,
            job: null,
            RuntimeSession(result));
    }

    private McpToolResult RuntimeStep(McpToolRequest request)
    {
        var kind = (Get(request, "kind") ?? "frame").Equals("physics", StringComparison.OrdinalIgnoreCase)
            ? RuntimeStepKind.Physics
            : RuntimeStepKind.Frame;
        var count = int.TryParse(Get(request, "count") ?? "1", out var parsedCount) ? parsedCount : 1;
        var fixedDelta = double.TryParse(
            Get(request, "fixedDelta") ?? "0.0166667",
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out var parsedDelta)
                ? parsedDelta
                : 0.0166667;
        var result = Tooling.Runtime.Step(kind, count, fixedDelta);
        return McpToolResult.FromRuntime(request.ToolName, Route, result.Succeeded, result.Diagnostics, job: null, RuntimeSession(result));
    }

    private McpToolResult RuntimeInjectInput(McpToolRequest request)
    {
        var state = Get(request, "state") ?? "pressed";
        var pressed = state.Equals("pressed", StringComparison.OrdinalIgnoreCase);
        var result = Tooling.Runtime.InjectInput(Require(request, "action"), pressed);
        return McpToolResult.FromRuntime(request.ToolName, Route, result.Succeeded, result.Diagnostics, job: null, RuntimeSession(result));
    }

    private McpToolResult RuntimeCaptureFrame(McpToolRequest request)
    {
        var result = Tooling.Runtime.CaptureFrame();
        return McpToolResult.FromRuntime(request.ToolName, Route, result.Succeeded, result.Diagnostics, job: null, RuntimeSession(result));
    }

    private McpToolResult RuntimeSceneTree(McpToolRequest request)
    {
        var result = Tooling.Runtime.GetSceneTree();
        return McpToolResult.FromRuntime(request.ToolName, Route, result.Succeeded, result.Diagnostics, job: null, RuntimeSession(result));
    }

    private McpToolResult RuntimeHighlightNode(McpToolRequest request)
    {
        var result = Tooling.Runtime.HighlightNode(Require(request, "nodePath"));
        return McpToolResult.FromRuntime(request.ToolName, Route, result.Succeeded, result.Diagnostics, job: null, RuntimeSession(result));
    }

    private McpToolResult RuntimeReportCrash(McpToolRequest request)
    {
        var exitCode = int.TryParse(Get(request, "exitCode") ?? "1", out var parsed) ? parsed : 1;
        var result = Tooling.Runtime.ReportProcessCrash(exitCode, Get(request, "stderr") ?? string.Empty);
        return McpToolResult.FromRuntime(request.ToolName, Route, result.Succeeded, result.Diagnostics, job: null, RuntimeSession(result));
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

    private McpToolResult ScriptCreate(McpToolRequest request)
    {
        var result = Tooling.Script.Create(
            OperationId(request.ToolName),
            Get(request, "path") ?? "Scripts/GeneratedAgentScript.cs",
            Get(request, "text") ?? "using Electron2D;\n\npublic sealed class GeneratedAgentScript : Node\n{\n}\n",
            AgentContext(OperationCapability.TaskWrite),
            ScriptApplyMode());
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteScriptDocument(result));
    }

    private McpToolResult ScriptRead(McpToolRequest request)
    {
        return McpToolResult.ContentOnly(
            request.ToolName,
            Route,
            WriteScriptDocument(Tooling.Script.Read(Get(request, "path") ?? "Scripts/HeroController.cs")));
    }

    private McpToolResult ScriptRename(McpToolRequest request)
    {
        var result = Tooling.Script.Rename(
            OperationId(request.ToolName),
            Require(request, "oldPath"),
            Require(request, "newPath"),
            Revision(request),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private McpToolResult ScriptDelete(McpToolRequest request)
    {
        var result = Tooling.Script.Delete(
            OperationId(request.ToolName),
            Require(request, "path"),
            Revision(request),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private McpToolResult ScriptSearchText(McpToolRequest request)
    {
        var result = Tooling.Script.SearchText(Get(request, "query") ?? string.Empty);
        var matches = new JsonArray();
        foreach (var match in result.Matches)
        {
            matches.Add(new JsonObject
            {
                ["path"] = match.Path,
                ["line"] = match.Line,
                ["column"] = match.Column
            });
        }

        return McpToolResult.ContentOnly(request.ToolName, Route, new JsonObject
        {
            ["matches"] = matches
        });
    }

    private McpToolResult ScriptApplyTextEdits(McpToolRequest request)
    {
        var result = Tooling.Script.ApplyTextEdits(
            new ToolingScriptApplyTextEditsRequest(
                OperationId(request.ToolName),
                Require(request, "path"),
                Revision(request),
                [ToolingScriptTextEdit.ReplaceAll(Require(request, "text"))],
                UndoGroupId(request.ToolName),
                ScriptApplyMode()),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private McpToolResult ScriptSave(McpToolRequest request)
    {
        var revision = Get(request, "agentBaseRevision") ?? Get(request, "expectedRevision") ?? "0";
        var baseRevision = long.TryParse(revision, out var parsed)
            ? new ProjectDocumentRevision(parsed)
            : new ProjectDocumentRevision(0);
        var result = Tooling.Script.Save(
            new ToolingScriptSaveRequest(
                OperationId(request.ToolName),
                Require(request, "path"),
                baseRevision,
                dryRun: false),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private McpToolResult ScriptFormat(McpToolRequest request)
    {
        var result = Tooling.Script.Format(
            ScriptIdeRequest(request),
            OperationId(request.ToolName),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private McpToolResult ScriptIde(McpToolRequest request, Func<ToolingScriptIdeRequest, ToolingScriptIdeResult> command)
    {
        var result = command(ScriptIdeRequest(request));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteScriptIdeResult(result));
    }

    private McpToolResult ScriptRenameSymbol(McpToolRequest request)
    {
        var result = Tooling.Script.RenameSymbol(
            ScriptIdeRequest(request),
            OperationId(request.ToolName),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private McpToolResult ScriptApplyCodeAction(McpToolRequest request)
    {
        var result = Tooling.Script.ApplyCodeAction(
            ScriptIdeRequest(request),
            OperationId(request.ToolName),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.FromOperation(request.ToolName, Route, result.Operation, RouteDiagnostics);
    }

    private ToolingScriptIdeRequest ScriptIdeRequest(McpToolRequest request)
    {
        return new ToolingScriptIdeRequest(
            Get(request, "path") ?? "Scripts/HeroController.cs",
            IntArgument(request, "completionPosition", -1),
            IntArgument(request, "signatureHelpPosition", -1),
            IntArgument(request, "hoverPosition", -1),
            IntArgument(request, "definitionPosition", -1),
            Get(request, "renameTo") ?? "RenamedSymbol",
            IntArgument(request, "responseDocumentRevision", -1));
    }

    private ToolingApplyMode ScriptApplyMode()
    {
        return Route == McpRoute.ActiveEditor ? ToolingApplyMode.WorkspaceOnly : ToolingApplyMode.HeadlessCommit;
    }

    private McpToolResult DebugSetBreakpoint(McpToolRequest request)
    {
        var result = Tooling.Debug.SetBreakpoint(
            new ToolingDebugSetBreakpointRequest(
                OperationId(request.ToolName),
                Get(request, "documentId") ?? "doc-hero",
                Get(request, "path") ?? "Scripts/HeroController.cs",
                IntArgument(request, "line", 10),
                IntArgument(request, "column", 17)),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugCommand(result));
    }

    private McpToolResult DebugUpdateBreakpoint(McpToolRequest request)
    {
        var result = Tooling.Debug.UpdateBreakpoint(
            new ToolingDebugUpdateBreakpointRequest(
                OperationId(request.ToolName),
                Get(request, "breakpointId") ?? "breakpoint-hero-update",
                BoolArgument(request, "enabled", true),
                IntArgument(request, "line", 10),
                IntArgument(request, "column", 17)),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugCommand(result));
    }

    private McpToolResult DebugRemoveBreakpoint(McpToolRequest request)
    {
        var result = Tooling.Debug.RemoveBreakpoint(Get(request, "breakpointId") ?? "breakpoint-hero-update");
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugCommand(result));
    }

    private McpToolResult DebugStart(McpToolRequest request)
    {
        var result = Tooling.Debug.Start(
            new ToolingDebugStartRequest(
                OperationId(request.ToolName),
                Get(request, "inputBuildConfigurationHash") ?? "sha256:mcp-debug",
                Environment.ProcessId),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugSession(result));
    }

    private McpToolResult DebugAttach(McpToolRequest request)
    {
        var result = Tooling.Debug.Attach(
            new ToolingDebugAttachRequest(
                OperationId(request.ToolName),
                IntArgument(request, "processId", Environment.ProcessId),
                IntArgument(request, "activeEditorGameProcessId", Environment.ProcessId),
                BoolArgument(request, "interactiveApproved", false),
                Get(request, "inputBuildConfigurationHash") ?? "sha256:mcp-debug-attach"),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugSession(result));
    }

    private McpToolResult DebugRestart(McpToolRequest request)
    {
        var result = Tooling.Debug.Restart(
            new ToolingDebugStartRequest(
                OperationId(request.ToolName),
                Get(request, "inputBuildConfigurationHash") ?? "sha256:mcp-debug-restart",
                Environment.ProcessId),
            AgentContext(OperationCapability.TaskWrite));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugSession(result));
    }

    private McpToolResult DebugCommand(McpToolRequest request, Func<ToolingDebugCommandResult> command)
    {
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugCommand(command()));
    }

    private McpToolResult DebugGetStack(McpToolRequest request)
    {
        var result = Tooling.Debug.GetStack();
        return McpToolResult.ContentOnly(request.ToolName, Route, new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics),
            ["threads"] = WriteDebugThreads(result.Threads),
            ["stacksByThread"] = WriteStacksByThread(result.StacksByThread)
        });
    }

    private McpToolResult DebugVariables(McpToolRequest request, Func<ToolingDebugFrameRequest, ToolingDebugVariablesResult> command)
    {
        var result = command(new ToolingDebugFrameRequest(IntArgument(request, "frameId", 101)));
        return McpToolResult.ContentOnly(request.ToolName, Route, new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics),
            ["variables"] = WriteDebugVariables(result.Variables)
        });
    }

    private McpToolResult DebugGetWatches(McpToolRequest request)
    {
        var result = Tooling.Debug.GetWatches();
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugWatches(result));
    }

    private McpToolResult DebugEvaluateWatches(McpToolRequest request)
    {
        var result = Tooling.Debug.EvaluateWatches(new ToolingDebugFrameRequest(IntArgument(request, "frameId", 101)));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugWatches(result));
    }

    private McpToolResult DebugAddWatch(McpToolRequest request)
    {
        var result = Tooling.Debug.AddWatch(new ToolingDebugWatchRequest(OperationId(request.ToolName), Get(request, "expression") ?? "hero.Health"));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugWatches(result));
    }

    private McpToolResult DebugUpdateWatch(McpToolRequest request)
    {
        var result = Tooling.Debug.UpdateWatch(new ToolingDebugWatchUpdateRequest(
            OperationId(request.ToolName),
            Require(request, "watchId"),
            Get(request, "expression") ?? "hero.Health"));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugWatches(result));
    }

    private McpToolResult DebugRemoveWatch(McpToolRequest request)
    {
        var result = Tooling.Debug.RemoveWatch(new ToolingDebugRemoveWatchRequest(OperationId(request.ToolName), Require(request, "watchId")));
        return McpToolResult.ContentOnly(request.ToolName, Route, WriteDebugCommand(result));
    }

    private static JsonObject WriteScriptDocument(ToolingScriptDocumentResult result)
    {
        return new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["path"] = result.Path,
            ["text"] = result.Text,
            ["documentId"] = result.DocumentId,
            ["documentRevision"] = result.DocumentRevision.Value,
            ["persistedRevision"] = result.PersistedRevision.Value,
            ["semanticVersion"] = result.SemanticVersion,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics)
        };
    }

    private static JsonObject WriteScriptIdeResult(ToolingScriptIdeResult result)
    {
        var completions = new JsonArray();
        foreach (var item in result.CompletionItems)
        {
            completions.Add(new JsonObject
            {
                ["displayText"] = item.DisplayText,
                ["isSelected"] = item.IsSelected
            });
        }

        var symbols = new JsonArray();
        foreach (var symbol in result.Symbols)
        {
            symbols.Add(new JsonObject
            {
                ["name"] = symbol.Name,
                ["kind"] = symbol.Kind,
                ["line"] = symbol.Line,
                ["column"] = symbol.Column
            });
        }

        var references = new JsonArray();
        foreach (var reference in result.References)
        {
            references.Add(new JsonObject
            {
                ["path"] = reference.Path,
                ["line"] = reference.Line,
                ["column"] = reference.Column
            });
        }

        var codeActions = new JsonArray();
        foreach (var action in result.CodeActions)
        {
            codeActions.Add(new JsonObject
            {
                ["title"] = action.Title,
                ["editCount"] = action.Edits.Count
            });
        }

        return new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["commandName"] = result.CommandName,
            ["documentRevision"] = result.DocumentRevision.Value,
            ["semanticVersion"] = result.SemanticVersion,
            ["roslynSemanticModel"] = result.RoslynSemanticModel,
            ["workspaceSnapshotUsedForIde"] = result.WorkspaceSnapshotUsedForIde,
            ["completionItems"] = completions,
            ["signatureHelp"] = result.SignatureHelp?.Display,
            ["hover"] = result.Hover?.SymbolDisplay,
            ["diagnosticCode"] = result.Diagnostic?.Code,
            ["definition"] = result.Definition?.ToString(),
            ["references"] = references,
            ["symbols"] = symbols,
            ["codeActions"] = codeActions,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics)
        };
    }

    private static JsonObject WriteDebugCommand(ToolingDebugCommandResult result)
    {
        return new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics),
            ["breakpoint"] = result.Breakpoint is null ? null : WriteDebugBreakpoint(result.Breakpoint)
        };
    }

    private static JsonObject WriteDebugSession(ToolingDebugSessionResult result)
    {
        return new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["operationId"] = result.OperationId,
            ["inputSnapshotId"] = result.InputSnapshotId,
            ["inputWorkspaceRevision"] = result.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = result.InputContentRevision.Value,
            ["inputDocumentRevisions"] = WriteRevisions(result.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = result.InputBuildConfigurationHash,
            ["debugBuildPortablePdb"] = result.DebugBuildPortablePdb,
            ["threads"] = WriteDebugThreads(result.Threads),
            ["stackFrames"] = WriteDebugStackFrames(result.StackFrames),
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics)
        };
    }

    private static JsonObject WriteDebugBreakpoint(ToolingDebugBreakpoint breakpoint)
    {
        return new JsonObject
        {
            ["breakpointId"] = breakpoint.BreakpointId,
            ["documentId"] = breakpoint.DocumentId,
            ["path"] = breakpoint.SourceAnchor.Path,
            ["line"] = breakpoint.SourceAnchor.Line,
            ["column"] = breakpoint.SourceAnchor.Column,
            ["enabled"] = breakpoint.Enabled,
            ["verified"] = breakpoint.Verified,
            ["adapterMessage"] = breakpoint.AdapterMessage
        };
    }

    private static JsonArray WriteDebugThreads(IEnumerable<ToolingDebugThread> threads)
    {
        var array = new JsonArray();
        foreach (var thread in threads.OrderBy(thread => thread.ThreadId))
        {
            array.Add(new JsonObject
            {
                ["threadId"] = thread.ThreadId,
                ["name"] = thread.Name,
                ["isSelected"] = thread.IsSelected
            });
        }

        return array;
    }

    private static JsonArray WriteDebugStackFrames(IEnumerable<ToolingDebugStackFrame> frames)
    {
        var array = new JsonArray();
        foreach (var frame in frames.OrderBy(frame => frame.ThreadId).ThenBy(frame => frame.FrameId))
        {
            array.Add(new JsonObject
            {
                ["frameId"] = frame.FrameId,
                ["threadId"] = frame.ThreadId,
                ["display"] = frame.Display,
                ["path"] = frame.Source.Path,
                ["line"] = frame.Source.Line,
                ["column"] = frame.Source.Column
            });
        }

        return array;
    }

    private static JsonObject WriteStacksByThread(IReadOnlyDictionary<int, IReadOnlyList<ToolingDebugStackFrame>> stacksByThread)
    {
        var root = new JsonObject();
        foreach (var pair in stacksByThread.OrderBy(pair => pair.Key))
        {
            root[pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)] = WriteDebugStackFrames(pair.Value);
        }

        return root;
    }

    private static JsonArray WriteDebugVariables(IEnumerable<ToolingDebugVariable> variables)
    {
        var array = new JsonArray();
        foreach (var variable in variables.OrderBy(variable => variable.Name, StringComparer.Ordinal))
        {
            array.Add(new JsonObject
            {
                ["name"] = variable.Name,
                ["value"] = variable.Value,
                ["kind"] = variable.Kind,
                ["frameId"] = variable.FrameId
            });
        }

        return array;
    }

    private static JsonObject WriteDebugWatches(ToolingDebugWatchesResult result)
    {
        var watches = new JsonArray();
        foreach (var watch in result.Watches.OrderBy(watch => watch.WatchId, StringComparer.Ordinal))
        {
            watches.Add(new JsonObject
            {
                ["watchId"] = watch.WatchId,
                ["expression"] = watch.Expression,
                ["value"] = watch.Value,
                ["frameId"] = watch.FrameId
            });
        }

        return new JsonObject
        {
            ["succeeded"] = result.Succeeded,
            ["diagnostics"] = WriteDiagnostics(result.Diagnostics),
            ["watches"] = watches
        };
    }

    private static int IntArgument(McpToolRequest request, string key, int fallback)
    {
        var value = Get(request, key);
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool BoolArgument(McpToolRequest request, string key, bool fallback)
    {
        var value = Get(request, key);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
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

    private JsonObject RuntimeSession(ToolingRuntimeCommandResult? result = null)
    {
        var session = result?.Session ?? Workspace.Runtime.ActiveSession;
        if (session is null)
        {
            return new JsonObject
            {
                ["route"] = RouteName(Route),
                ["active"] = false,
                ["diagnostics"] = result is null ? new JsonArray() : WriteDiagnostics(result.Diagnostics)
            };
        }

        var root = new JsonObject
        {
            ["route"] = RouteName(Route),
            ["active"] = true,
            ["inputSnapshotId"] = session.InputSnapshotId,
            ["inputWorkspaceRevision"] = session.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = session.InputContentRevision.Value,
            ["inputDocumentRevisions"] = WriteRevisions(session.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = session.InputBuildConfigurationHash,
            ["visibleMode"] = session.VisibleMode.ToString(),
            ["isProcessIsolated"] = session.IsProcessIsolated,
            ["highlightedNodePath"] = session.HighlightedNodePath,
            ["session"] = WriteRuntimeSession(session),
            ["metrics"] = RuntimeDebugJsonSerializer.ToJson(session.GetMetrics()),
            ["diagnostics"] = WriteDiagnostics(result?.Diagnostics ?? session.Diagnostics)
        };

        if (result?.SceneTree is not null)
        {
            root["sceneTree"] = RuntimeDebugJsonSerializer.ToJson(result.SceneTree);
        }

        if (result?.Screenshot is not null)
        {
            root["screenshot"] = RuntimeDebugJsonSerializer.ToJson(result.Screenshot, path: null);
        }

        return root;
    }

    private static JsonObject WriteRuntimeSession(ProjectWorkspaceRuntimeSession session)
    {
        var inputActions = new JsonObject();
        foreach (var (action, pressed) in session.InputActions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            inputActions[action] = pressed;
        }

        return new JsonObject
        {
            ["sessionId"] = session.SessionId,
            ["sessionKind"] = session.SessionKind.ToString(),
            ["scene"] = session.ScenePath,
            ["state"] = session.State.ToString(),
            ["currentFrame"] = session.CurrentFrame,
            ["currentPhysicsFrame"] = session.CurrentPhysicsFrame,
            ["inputActions"] = inputActions,
            ["buildConfigurationHash"] = session.InputBuildConfigurationHash
        };
    }

    private JsonObject EditorCapabilities()
    {
        return EditorCapabilityManifestSerializer.ToJson(EditorCapabilityManifestFactory.CreateDefault());
    }

    private JsonObject EditorCapabilitySummary()
    {
        var manifest = EditorCapabilityManifestFactory.CreateDefault();
        var verification = EditorCapabilityManifestVerifier.Verify(
            manifest,
            new EditorCapabilityManifestVerificationInput(
                DefaultToolNames,
                ProjectToolingHost.SupportedCommandNames,
                ResolveRepositoryRoot()));
        return new JsonObject
        {
            ["path"] = "data/editor/electron2d-editor-capabilities.json",
            ["capabilities"] = manifest.Capabilities.Count,
            ["releaseRequired"] = manifest.Capabilities.Count(capability => capability.ReleaseRequired),
            ["succeeded"] = verification.Succeeded,
            ["diagnostics"] = WriteDiagnostics(verification.Diagnostics)
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

    private static string ResolveRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "data", "api", "electron2d-api-manifest.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return AppContext.BaseDirectory;
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
