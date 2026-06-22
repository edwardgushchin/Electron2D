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
    public ProjectToolingHost(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        Project = new ProjectService(workspace);
        Tasks = new TaskService(workspace);
        Build = new ToolingJobService(workspace, WorkspaceJobKind.Build);
        Tests = new ToolingJobService(workspace, WorkspaceJobKind.Test);
        Export = new ToolingJobService(workspace, WorkspaceJobKind.Export);
        Import = new ToolingJobService(workspace, WorkspaceJobKind.Import);
        Runtime = new ToolingJobService(workspace, WorkspaceJobKind.Run);
    }

    public ProjectService Project { get; }

    public TaskService Tasks { get; }

    public ToolingJobService Build { get; }

    public ToolingJobService Tests { get; }

    public ToolingJobService Export { get; }

    public ToolingJobService Import { get; }

    public ToolingJobService Runtime { get; }
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
        string? undoGroupId)
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
    }

    public string OperationId { get; }

    public string OperationKind { get; }

    public ToolingApplyMode Mode { get; }

    public string Path { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string Text { get; }

    public string? UndoGroupId { get; }
}

internal sealed class ToolingOperationResult
{
    private ToolingOperationResult(
        bool succeeded,
        string operationId,
        string operationKind,
        ProjectWorkspaceRevision workspaceRevision,
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
            dryRun: false,
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
