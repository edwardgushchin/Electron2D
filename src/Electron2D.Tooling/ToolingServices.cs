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
using Electron2D.ProjectSystem;

namespace Electron2D.Tooling;

internal enum ToolingApplyMode
{
    WorkspaceOnly,
    HeadlessCommit,
    ExternalImport
}

internal sealed class ProjectToolingHost
{
    public static IReadOnlyList<string> SupportedCommandNames => ProjectToolingCommandCatalog.SupportedCommandNames;

    public ProjectToolingHost(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        Project = new ProjectService(workspace);
        Tasks = new TaskService(workspace);
        Build = new ToolingJobService(workspace, WorkspaceJobKind.Build);
        Tests = new ToolingJobService(workspace, WorkspaceJobKind.Test);
        Export = new ToolingJobService(workspace, WorkspaceJobKind.Export);
        Import = new ToolingJobService(workspace, WorkspaceJobKind.Import);
        Runtime = new ToolingRuntimeService(workspace);
        Script = new ToolingScriptService(workspace, Project);
        Debug = new ToolingDebugService(workspace);
    }

    public ProjectService Project { get; }

    public TaskService Tasks { get; }

    public ToolingJobService Build { get; }

    public ToolingJobService Tests { get; }

    public ToolingJobService Export { get; }

    public ToolingJobService Import { get; }

    public ToolingRuntimeService Runtime { get; }

    public ToolingScriptService Script { get; }

    public ToolingDebugService Debug { get; }
}

internal sealed class ToolingTextEditRequest
{
    public ToolingTextEditRequest(
        string operationId,
        string operationKind,
        ToolingApplyMode mode,
        string path,
        ProjectDocumentRevision expectedRevision,
        string text,
        string? undoGroupId,
        bool dryRun = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);

        OperationId = operationId;
        OperationKind = operationKind;
        Mode = mode;
        Path = path;
        ExpectedRevision = expectedRevision;
        Text = text.ReplaceLineEndings("\n");
        UndoGroupId = undoGroupId;
        DryRun = dryRun;
    }

    public string OperationId { get; }

    public string OperationKind { get; }

    public ToolingApplyMode Mode { get; }

    public string Path { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string Text { get; }

    public string? UndoGroupId { get; }

    public bool DryRun { get; }
}

internal sealed class ToolingOperationResult
{
    private ToolingOperationResult(
        bool succeeded,
        string operationId,
        string operationKind,
        ProjectWorkspaceRevision workspaceRevision,
        ProjectWorkspaceRevision contentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> documentRevisions,
        ProjectDocumentRevision persistedRevision,
        IReadOnlyList<string> dirtyDocuments,
        ProjectWorkspacePersistenceState persistenceState,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> changedObjects,
        IReadOnlyList<string> createdObjects,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        string? undoGroupId,
        string? taskId,
        string? jobId)
    {
        Succeeded = succeeded;
        OperationId = operationId;
        OperationKind = operationKind;
        WorkspaceRevision = workspaceRevision;
        ContentRevision = contentRevision;
        DocumentRevisions = CopyDictionary(documentRevisions);
        PersistedRevision = persistedRevision;
        DirtyDocuments = dirtyDocuments.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        PersistenceState = persistenceState;
        ChangedFiles = changedFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        ChangedObjects = changedObjects.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        CreatedObjects = createdObjects.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Diagnostics = diagnostics.ToArray();
        UndoGroupId = undoGroupId;
        TaskId = taskId;
        JobId = jobId;
    }

    public bool Succeeded { get; }

    public string OperationId { get; }

    public string OperationKind { get; }

    public ProjectWorkspaceRevision WorkspaceRevision { get; }

    public ProjectWorkspaceRevision ContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> DocumentRevisions { get; }

    public ProjectDocumentRevision PersistedRevision { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }

    public ProjectWorkspacePersistenceState PersistenceState { get; }

    public IReadOnlyList<string> ChangedFiles { get; }

    public IReadOnlyList<string> ChangedObjects { get; }

    public IReadOnlyList<string> CreatedObjects { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public string? UndoGroupId { get; }

    public string? TaskId { get; }

    public string? JobId { get; }

    public static ToolingOperationResult FromTransaction(
        WorkspaceTransactionResult result,
        string operationKind,
        string? taskId = null,
        string? jobId = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);

        return new ToolingOperationResult(
            result.Succeeded,
            result.OperationId,
            operationKind,
            result.WorkspaceRevision,
            result.ContentRevision,
            result.DocumentRevisions,
            SelectPersistedRevision(result),
            result.DirtyDocuments,
            result.PersistenceState,
            result.ChangedFiles,
            result.ChangedObjects,
            result.CreatedObjects,
            result.Diagnostics,
            result.UndoGroupId,
            taskId,
            jobId);
    }

    public static ToolingOperationResult FromTask(ProjectTaskMutationResult result, string operationKind)
    {
        ArgumentNullException.ThrowIfNull(result);
        return FromTransaction(result.TransactionResult, operationKind, result.Task.TaskId);
    }

    public static ToolingOperationResult FromWorkspaceState(
        bool succeeded,
        string operationId,
        string operationKind,
        ProjectWorkspace workspace,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> changedObjects,
        IReadOnlyList<string> createdObjects,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        string? undoGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(changedFiles);
        ArgumentNullException.ThrowIfNull(changedObjects);
        ArgumentNullException.ThrowIfNull(createdObjects);
        ArgumentNullException.ThrowIfNull(diagnostics);

        return new ToolingOperationResult(
            succeeded,
            operationId,
            operationKind,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            new ProjectDocumentRevision(0),
            workspace.Revisions.DirtyDocuments,
            workspace.Revisions.PersistenceState,
            changedFiles,
            changedObjects,
            createdObjects,
            diagnostics,
            undoGroupId,
            taskId: null,
            jobId: null);
    }

    public static ToolingOperationResult Failure(
        string operationId,
        string operationKind,
        ProjectWorkspace workspace,
        StructuredDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        return FromWorkspaceState(
            succeeded: false,
            operationId,
            operationKind,
            workspace,
            changedFiles: [],
            changedObjects: [],
            createdObjects: [],
            diagnostics: [diagnostic],
            undoGroupId: null);
    }

    private static ProjectDocumentRevision SelectPersistedRevision(WorkspaceTransactionResult result)
    {
        if (result.PersistedRevisions.Count == 0)
        {
            return new ProjectDocumentRevision(0);
        }

        if (result.ChangedFiles.Count == 1 &&
            result.PersistedRevisions.TryGetValue(result.ChangedFiles[0], out var changedFileRevision))
        {
            return changedFileRevision;
        }

        if (result.DirtyDocuments.Count == 1 &&
            result.PersistedRevisions.TryGetValue(result.DirtyDocuments[0], out var dirtyRevision))
        {
            return dirtyRevision;
        }

        return result.PersistedRevisions
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .First()
            .Value;
    }

    private static IReadOnlyDictionary<string, ProjectDocumentRevision> CopyDictionary(
        IReadOnlyDictionary<string, ProjectDocumentRevision> source)
    {
        return new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            source.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }
}

internal sealed class ProjectService
{
    private readonly ProjectWorkspace workspace;

    public ProjectService(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
    }

    public ToolingOperationResult ApplyTextEdit(ToolingTextEditRequest request, OperationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var result = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            request.OperationId,
            ToWorkspaceActorKind(context.PrincipalKind),
            request.OperationKind,
            ToTransactionMode(request.Mode),
            request.DryRun,
            request.UndoGroupId,
            [WorkspaceTransactionDocumentEdit.ReplaceText(request.Path, request.ExpectedRevision, request.Text)]));
        return ToolingOperationResult.FromTransaction(result, request.OperationKind);
    }

    public ToolingOperationResult SaveAffectedDocuments(string operationId, OperationContext context, bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(context);

        var result = workspace.Transactions.Apply(WorkspaceTransactionRequest.SaveAffectedDocuments(
            operationId,
            ToWorkspaceActorKind(context.PrincipalKind),
            dryRun));
        return ToolingOperationResult.FromTransaction(result, "workspace.save-affected-documents");
    }

    internal static ProjectWorkspaceActorKind ToWorkspaceActorKind(PrincipalKind principalKind)
    {
        return principalKind switch
        {
            PrincipalKind.Human => ProjectWorkspaceActorKind.Human,
            PrincipalKind.Agent => ProjectWorkspaceActorKind.Agent,
            PrincipalKind.Cli => ProjectWorkspaceActorKind.Cli,
            PrincipalKind.ExternalFile => ProjectWorkspaceActorKind.ExternalFile,
            _ => ProjectWorkspaceActorKind.Test
        };
    }

    private static WorkspaceTransactionMode ToTransactionMode(ToolingApplyMode mode)
    {
        return mode switch
        {
            ToolingApplyMode.WorkspaceOnly => WorkspaceTransactionMode.WorkspaceOnly,
            ToolingApplyMode.HeadlessCommit => WorkspaceTransactionMode.HeadlessCommit,
            ToolingApplyMode.ExternalImport => WorkspaceTransactionMode.ExternalImport,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Tooling apply mode is not supported.")
        };
    }
}

internal sealed class ToolingTaskStatusRequest
{
    public ToolingTaskStatusRequest(
        string taskId,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        ArgumentNullException.ThrowIfNull(context);

        TaskId = taskId;
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
    }

    public string TaskId { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }
}

internal sealed class ToolingTaskActivityRequest
{
    public ToolingTaskActivityRequest(
        string taskId,
        TaskActivityKind kind,
        string payload,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        ArgumentNullException.ThrowIfNull(context);

        TaskId = taskId;
        Kind = kind;
        Payload = payload;
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
    }

    public string TaskId { get; }

    public TaskActivityKind Kind { get; }

    public string Payload { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }
}

internal sealed class ToolingTaskLinkRequest
{
    public ToolingTaskLinkRequest(
        string taskId,
        string link,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(link);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        ArgumentNullException.ThrowIfNull(context);

        TaskId = taskId;
        Link = link;
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
    }

    public string TaskId { get; }

    public string Link { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }
}

internal sealed class TaskService
{
    private static readonly string[] Commands =
    [
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

    private readonly ProjectWorkspace workspace;

    public TaskService(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
    }

    public IReadOnlyList<string> SupportedCommands => Commands;

    public IReadOnlyList<ProjectTask> List()
    {
        return workspace.Documents.Documents
            .Where(document => document.Path.StartsWith(".electron2d/tasks/", StringComparison.Ordinal) &&
                document.Path.EndsWith(".e2task", StringComparison.Ordinal))
            .Select(document => ProjectTaskSerializer.DeserializeTask(document.Path, document.Text))
            .OrderBy(task => task.Rank, StringComparer.Ordinal)
            .ThenBy(task => task.TaskId, StringComparer.Ordinal)
            .ToArray();
    }

    public ProjectTask Get(string taskId)
    {
        return workspace.Tasks.GetTask(taskId);
    }

    public ToolingOperationResult SubmitForAcceptance(ToolingTaskStatusRequest request)
    {
        var result = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            request.TaskId,
            ProjectTaskStatus.AwaitingAcceptance,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context));
        return ToolingOperationResult.FromTask(result, "task_submit_for_acceptance");
    }

    public ToolingOperationResult Accept(ToolingTaskStatusRequest request)
    {
        var result = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            request.TaskId,
            ProjectTaskStatus.Done,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context));
        return ToolingOperationResult.FromTask(result, "task_accept");
    }

    public ToolingOperationResult RequestChanges(ToolingTaskStatusRequest request, string reason)
    {
        var result = workspace.Tasks.RequestChanges(new ProjectTaskAcceptanceRequest(
            request.TaskId,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context,
            reason));
        return ToolingOperationResult.FromTask(result, "task_request_changes");
    }

    public ToolingOperationResult Cancel(ToolingTaskStatusRequest request)
    {
        var result = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            request.TaskId,
            ProjectTaskStatus.Cancelled,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context));
        return ToolingOperationResult.FromTask(result, "task_cancel");
    }

    public ToolingOperationResult AppendActivity(ToolingTaskActivityRequest request)
    {
        var result = workspace.Tasks.AddActivity(new ProjectTaskActivityRequest(
            request.TaskId,
            request.Kind,
            request.Payload,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context));
        return ToolingOperationResult.FromTask(result, "task_append_activity");
    }

    public ToolingOperationResult LinkTransaction(ToolingTaskLinkRequest request)
    {
        return Link(request, "task_link_transaction", task => AddDistinct(task.LinkedTransactions, request.Link));
    }

    public ToolingOperationResult LinkJob(ToolingTaskLinkRequest request)
    {
        return Link(request, "task_link_job", task => AddDistinct(task.LinkedJobs, request.Link));
    }

    public ToolingOperationResult LinkArtifact(ToolingTaskLinkRequest request)
    {
        return Link(request, "task_link_artifact", task => AddDistinct(task.LinkedArtifacts, request.Link));
    }

    private ToolingOperationResult Link(
        ToolingTaskLinkRequest request,
        string operationKind,
        Action<ProjectTask> mutate)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(mutate);

        if (!request.Context.HasCapability(OperationCapability.TaskWrite))
        {
            var rejected = ProjectTaskMutationResult.Rejected(
                workspace.Tasks.GetTask(request.TaskId),
                request.OperationId,
                CreateTaskDiagnostic("E2D-TASK-0002", "Task link operations require task write capability."),
                workspace);
            return ToolingOperationResult.FromTask(rejected, operationKind);
        }

        var task = TaskDependencyGraph.CloneTask(workspace.Tasks.GetTask(request.TaskId));
        mutate(task);
        var path = ProjectTaskStorage.GetTaskDocumentPath(task.TaskId);
        var result = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            request.OperationId,
            ProjectService.ToWorkspaceActorKind(request.Context.PrincipalKind),
            operationKind,
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            request.UndoGroupId,
            [WorkspaceTransactionDocumentEdit.ReplaceText(path, request.ExpectedRevision, ProjectTaskSerializer.Serialize(task))]));
        return ToolingOperationResult.FromTransaction(result, operationKind, request.TaskId);
    }

    private static void AddDistinct(List<string> values, string value)
    {
        if (!values.Contains(value, StringComparer.Ordinal))
        {
            values.Add(value);
        }
    }

    private static StructuredDiagnostic CreateTaskDiagnostic(string code, string message)
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
}

internal sealed class ToolingJobRequest
{
    public ToolingJobRequest(string operationId, string inputBuildConfigurationHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        OperationId = operationId;
        InputBuildConfigurationHash = inputBuildConfigurationHash;
    }

    public string OperationId { get; }

    public string InputBuildConfigurationHash { get; }
}

internal sealed class ToolingRuntimeStartRequest
{
    public ToolingRuntimeStartRequest(
        string operationId,
        string scenePath,
        string inputBuildConfigurationHash,
        RuntimeVisibleMode visibleMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scenePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        OperationId = operationId;
        ScenePath = ProjectDocumentPaths.NormalizeRelativePath(scenePath);
        InputBuildConfigurationHash = inputBuildConfigurationHash;
        VisibleMode = visibleMode;
    }

    public string OperationId { get; }

    public string ScenePath { get; }

    public string InputBuildConfigurationHash { get; }

    public RuntimeVisibleMode VisibleMode { get; }
}

internal sealed class ToolingRuntimeSessionResult
{
    private ToolingRuntimeSessionResult(
        bool succeeded,
        ProjectWorkspaceRuntimeSession? session,
        ToolingJobResult? job,
        IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Succeeded = succeeded;
        Session = session;
        Job = job;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public ProjectWorkspaceRuntimeSession? Session { get; }

    public ToolingJobResult? Job { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static ToolingRuntimeSessionResult Success(ProjectWorkspaceRuntimeSession session, ToolingJobResult job)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(job);
        return new ToolingRuntimeSessionResult(true, session, job, []);
    }

    public static ToolingRuntimeSessionResult Failure(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        return new ToolingRuntimeSessionResult(false, null, null, diagnostics);
    }
}

internal sealed class ToolingRuntimeCommandResult
{
    private ToolingRuntimeCommandResult(
        bool succeeded,
        ProjectWorkspaceRuntimeSession? session,
        RuntimeDebugSceneTreeSnapshot? sceneTree,
        RuntimeDebugScreenshot? screenshot,
        IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);

        Succeeded = succeeded;
        Session = session;
        SceneTree = sceneTree;
        Screenshot = screenshot;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public ProjectWorkspaceRuntimeSession? Session { get; }

    public RuntimeDebugSceneTreeSnapshot? SceneTree { get; }

    public RuntimeDebugScreenshot? Screenshot { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static ToolingRuntimeCommandResult Success(
        ProjectWorkspaceRuntimeSession session,
        RuntimeDebugSceneTreeSnapshot? sceneTree = null,
        RuntimeDebugScreenshot? screenshot = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new ToolingRuntimeCommandResult(true, session, sceneTree, screenshot, session.Diagnostics);
    }

    public static ToolingRuntimeCommandResult Failure(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        return new ToolingRuntimeCommandResult(false, null, null, null, diagnostics);
    }
}

internal sealed class ToolingJobResult
{
    public ToolingJobResult(bool succeeded, WorkspaceJob job)
    {
        ArgumentNullException.ThrowIfNull(job);

        Succeeded = succeeded;
        OperationId = job.OperationId;
        JobId = job.OperationId;
        JobKind = job.Kind;
        JobState = job.State;
        InputSnapshotId = job.InputIdentity.InputSnapshotId;
        InputWorkspaceRevision = job.InputIdentity.InputWorkspaceRevision;
        InputContentRevision = job.InputIdentity.InputContentRevision;
        InputDocumentRevisions = new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            job.InputIdentity.InputDocumentRevisions.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        InputBuildConfigurationHash = job.InputIdentity.InputBuildConfigurationHash;
        Diagnostics = job.Diagnostics;
        Artifacts = job.Artifacts;
    }

    public bool Succeeded { get; }

    public string OperationId { get; }

    public string JobId { get; }

    public WorkspaceJobKind JobKind { get; }

    public WorkspaceJobState JobState { get; }

    public string InputSnapshotId { get; }

    public ProjectWorkspaceRevision InputWorkspaceRevision { get; }

    public ProjectWorkspaceRevision InputContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> InputDocumentRevisions { get; }

    public string InputBuildConfigurationHash { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public IReadOnlyList<WorkspaceJobArtifact> Artifacts { get; }
}

internal sealed class ToolingJobService
{
    private readonly WorkspaceJobKind kind;
    private readonly ProjectWorkspace workspace;

    public ToolingJobService(ProjectWorkspace workspace, WorkspaceJobKind kind)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
        this.kind = kind;
    }

    public ToolingJobResult Queue(ToolingJobRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId($"snapshot-{request.OperationId}"),
            DateTimeOffset.UtcNow);
        var job = workspace.Jobs.Enqueue(
            request.OperationId,
            kind,
            WorkspaceJobInputIdentity.FromSnapshot(snapshot, request.InputBuildConfigurationHash),
            canCancel: true);
        return new ToolingJobResult(succeeded: true, job);
    }
}

internal sealed class ToolingRuntimeService
{
    private readonly ProjectWorkspace workspace;

    public ToolingRuntimeService(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
    }

    public ToolingJobResult Queue(ToolingJobRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var job = EnqueueRunJob(request.OperationId, request.InputBuildConfigurationHash, out _);
        return new ToolingJobResult(succeeded: true, job);
    }

    public ToolingRuntimeSessionResult StartEditorAttached(ToolingRuntimeStartRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (workspace.Runtime.ActiveSession is { State: RuntimeDebugSessionState.Running or RuntimeDebugSessionState.Paused })
        {
            return ToolingRuntimeSessionResult.Failure(
            [
                RuntimeDebugBridge.CreateDiagnostic("An editor-attached runtime session is already active.", file: request.ScenePath)
            ]);
        }

        var job = EnqueueRunJob(request.OperationId, request.InputBuildConfigurationHash, out var snapshot);
        var materialization = WorkspaceSnapshotMaterializer.Materialize(
            workspace.ProjectRoot,
            $"runtime-{request.OperationId}",
            snapshot);
        var start = RuntimeDebugBridge.Start(new RuntimeDebugStartRequest(
            materialization.RootPath,
            request.ScenePath,
            RuntimeDebugSessionKind.EditorAttachedPreview,
            developmentMode: true,
            request.InputBuildConfigurationHash));
        if (!start.Succeeded)
        {
            return ToolingRuntimeSessionResult.Failure(start.Diagnostics);
        }

        var session = workspace.Runtime.StartEditorAttached(start.Session!, job, request.VisibleMode);
        return ToolingRuntimeSessionResult.Success(session, new ToolingJobResult(succeeded: true, job));
    }

    public ToolingRuntimeCommandResult Pause()
    {
        return Execute(session =>
        {
            session.Pause();
            return ToolingRuntimeCommandResult.Success(session);
        });
    }

    public ToolingRuntimeCommandResult Resume()
    {
        return Execute(session =>
        {
            session.Resume();
            return ToolingRuntimeCommandResult.Success(session);
        });
    }

    public ToolingRuntimeCommandResult Stop()
    {
        return Execute(session =>
        {
            session.Stop();
            workspace.Runtime.ClearActiveSession(session);
            return ToolingRuntimeCommandResult.Success(session);
        }, allowCrashed: true);
    }

    public ToolingRuntimeCommandResult Step(RuntimeStepKind kind, int count, double fixedDelta)
    {
        return Execute(session =>
        {
            session.Step(kind, count, fixedDelta);
            return ToolingRuntimeCommandResult.Success(session);
        });
    }

    public ToolingRuntimeCommandResult InjectInput(string action, bool pressed)
    {
        return Execute(session =>
        {
            session.InjectInput(action, pressed);
            return ToolingRuntimeCommandResult.Success(session);
        });
    }

    public ToolingRuntimeCommandResult CaptureFrame()
    {
        return Execute(session => ToolingRuntimeCommandResult.Success(session, screenshot: session.CaptureFrame()));
    }

    public ToolingRuntimeCommandResult GetSceneTree()
    {
        return Execute(session => ToolingRuntimeCommandResult.Success(session, sceneTree: session.GetSceneTree()), allowCrashed: true);
    }

    public ToolingRuntimeCommandResult GetDiagnostics()
    {
        var session = workspace.Runtime.ActiveSession;
        return session is null
            ? ToolingRuntimeCommandResult.Failure([RuntimeDebugBridge.CreateDiagnostic("There is no active editor-attached runtime session.")])
            : ToolingRuntimeCommandResult.Success(session);
    }

    public ToolingRuntimeCommandResult HighlightNode(string nodePath)
    {
        return Execute(session =>
        {
            var result = session.HighlightNode(nodePath);
            return result.Succeeded
                ? ToolingRuntimeCommandResult.Success(session)
                : ToolingRuntimeCommandResult.Failure(result.Diagnostics);
        });
    }

    public ToolingRuntimeCommandResult ReportProcessCrash(int exitCode, string stderr)
    {
        return Execute(session =>
        {
            session.ReportProcessCrash(exitCode, stderr);
            return ToolingRuntimeCommandResult.Success(session);
        }, allowCrashed: true);
    }

    private WorkspaceJob EnqueueRunJob(
        string operationId,
        string inputBuildConfigurationHash,
        out WorkspaceSnapshot snapshot)
    {
        snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId($"snapshot-{operationId}"),
            DateTimeOffset.UtcNow);
        return workspace.Jobs.Enqueue(
            operationId,
            WorkspaceJobKind.Run,
            WorkspaceJobInputIdentity.FromSnapshot(snapshot, inputBuildConfigurationHash),
            canCancel: true);
    }

    private ToolingRuntimeCommandResult Execute(
        Func<ProjectWorkspaceRuntimeSession, ToolingRuntimeCommandResult> action,
        bool allowCrashed = false)
    {
        ArgumentNullException.ThrowIfNull(action);

        var session = workspace.Runtime.ActiveSession;
        if (session is null)
        {
            return ToolingRuntimeCommandResult.Failure([RuntimeDebugBridge.CreateDiagnostic("There is no active editor-attached runtime session.")]);
        }

        if (!allowCrashed && session.State == RuntimeDebugSessionState.Crashed)
        {
            return ToolingRuntimeCommandResult.Failure(session.Diagnostics);
        }

        try
        {
            return action(session);
        }
        catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
        {
            return ToolingRuntimeCommandResult.Failure([RuntimeDebugBridge.CreateDiagnostic(exception.Message, file: session.ScenePath)]);
        }
    }
}
