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
using Electron2D.ProjectSystem;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ProjectTaskManagerTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AcceptanceGuardAllowsAgentSubmitButRequiresHumanDone()
    {
        using var workspace = CreateWorkspace("acceptance-guard", TaskDocument("task-alpha", ProjectTaskStatus.Review));

        var submitted = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            "task-alpha",
            ProjectTaskStatus.AwaitingAcceptance,
            new ProjectDocumentRevision(1),
            "op-agent-submit",
            "undo-agent-submit",
            AgentContext(OperationCapability.TaskSubmitForAcceptance)));

        Assert.True(submitted.Succeeded);
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, submitted.Task.Status);
        Assert.NotNull(submitted.Task.SubmittedAt);
        Assert.Equal(ProjectTaskAcceptanceState.Submitted, submitted.Task.AcceptanceState);
        Assert.Contains("undo-agent-submit", workspace.UndoRedo.UndoGroups);
        Assert.Equal([".electron2d/tasks/task-alpha.e2task"], submitted.TransactionResult.DirtyDocuments);
        Assert.Contains(submitted.Task.Activity, activity =>
            activity.Kind == TaskActivityKind.StatusChange &&
            activity.ActorId == "agent-1" &&
            activity.ActorKind == PrincipalKind.Agent);

        var rejected = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            "task-alpha",
            ProjectTaskStatus.Done,
            new ProjectDocumentRevision(2),
            "op-agent-done",
            "undo-agent-done",
            AgentContext(OperationCapability.TaskSubmitForAcceptance)));

        Assert.False(rejected.Succeeded);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0002");
        Assert.DoesNotContain("undo-agent-done", workspace.UndoRedo.UndoGroups);
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, workspace.Tasks.GetTask("task-alpha").Status);

        var accepted = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            "task-alpha",
            ProjectTaskStatus.Done,
            new ProjectDocumentRevision(2),
            "op-human-accept",
            "undo-human-accept",
            HumanContext(OperationCapability.TaskAccept)));

        Assert.True(accepted.Succeeded);
        Assert.Equal(ProjectTaskStatus.Done, accepted.Task.Status);
        Assert.Equal("user-1", accepted.Task.AcceptedBy);
        Assert.NotNull(accepted.Task.CompletedAt);
        Assert.NotNull(accepted.Task.AcceptedAt);
        Assert.Equal(ProjectTaskAcceptanceState.Accepted, accepted.Task.AcceptanceState);
        Assert.Contains(accepted.Task.Activity, activity => activity.Kind == TaskActivityKind.AcceptanceResult);
    }

    [Fact]
    public void RequestChangesAndReopenPreserveAcceptanceHistory()
    {
        using var changesWorkspace = CreateWorkspace(
            "request-changes",
            TaskDocument("task-review", ProjectTaskStatus.AwaitingAcceptance, ProjectTaskAcceptanceState.Submitted),
            "task-review");

        var returned = changesWorkspace.Tasks.RequestChanges(new ProjectTaskAcceptanceRequest(
            "task-review",
            new ProjectDocumentRevision(1),
            "op-request-changes",
            "undo-request-changes",
            HumanContext(OperationCapability.TaskRequestChanges),
            "Tests need coverage for cancelled dependencies."));

        Assert.True(returned.Succeeded);
        Assert.Equal(ProjectTaskStatus.InProgress, returned.Task.Status);
        Assert.Equal(ProjectTaskAcceptanceState.ChangesRequested, returned.Task.AcceptanceState);
        Assert.Contains(returned.Task.Activity, activity =>
            activity.Kind == TaskActivityKind.AcceptanceResult &&
            activity.Payload.Contains("Tests need coverage", StringComparison.Ordinal));

        using var reopenWorkspace = CreateWorkspace(
            "reopen",
            TaskDocument("task-done", ProjectTaskStatus.Done, ProjectTaskAcceptanceState.Accepted),
            "task-done");
        var doneBefore = reopenWorkspace.Tasks.GetTask("task-done");
        Assert.NotNull(doneBefore.CompletedAt);
        Assert.NotNull(doneBefore.AcceptedAt);

        var reopened = reopenWorkspace.Tasks.Reopen(new ProjectTaskReopenRequest(
            "task-done",
            ProjectTaskStatus.Ready,
            new ProjectDocumentRevision(1),
            "op-human-reopen",
            "undo-human-reopen",
            HumanContext(OperationCapability.TaskReopen),
            "Follow-up compatibility issue."));

        Assert.True(reopened.Succeeded);
        Assert.Equal(ProjectTaskStatus.Ready, reopened.Task.Status);
        Assert.Equal(doneBefore.CompletedAt, reopened.Task.CompletedAt);
        Assert.Equal(doneBefore.AcceptedAt, reopened.Task.AcceptedAt);
        Assert.Equal(doneBefore.AcceptedBy, reopened.Task.AcceptedBy);
        Assert.Equal(ProjectTaskAcceptanceState.Reopened, reopened.Task.AcceptanceState);
        Assert.Contains(reopened.Task.Activity, activity =>
            activity.Kind == TaskActivityKind.StatusChange &&
            activity.Payload.Contains("Follow-up compatibility", StringComparison.Ordinal));
    }

    [Fact]
    public void ActivityStoreFillsAuditFieldsFromTrustedOperationContext()
    {
        using var workspace = CreateWorkspace("activity-audit", TaskDocument("task-alpha", ProjectTaskStatus.InProgress));
        var before = DateTimeOffset.UtcNow;

        var result = workspace.Tasks.AddActivity(new ProjectTaskActivityRequest(
            "task-alpha",
            TaskActivityKind.Comment,
            "ActorId=human-spoof;ActorKind=Human;CreatedAt=1999-01-01T00:00:00Z;message=Need deterministic task audit.",
            new ProjectDocumentRevision(1),
            "op-agent-comment",
            "undo-agent-comment",
            AgentContext(OperationCapability.TaskWrite)));

        var after = DateTimeOffset.UtcNow;

        Assert.True(result.Succeeded);
        var entry = Assert.Single(result.Task.Activity);
        Assert.Equal("activity-agent-comment", entry.ActivityEntryId);
        Assert.Equal("agent-1", entry.ActorId);
        Assert.Equal(PrincipalKind.Agent, entry.ActorKind);
        Assert.InRange(entry.CreatedAt, before, after);
        Assert.Contains("Need deterministic task audit", entry.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("human-spoof", entry.Payload, StringComparison.Ordinal);
        Assert.DoesNotContain("1999-01-01", entry.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public void StorageRoundTripsTaskAndBoardDocumentsAsEditorMetadata()
    {
        var task = CreateTask("task-alpha", ProjectTaskStatus.Ready);
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            "criterion-red-green",
            "Focused tests fail before implementation and pass after implementation.",
            AcceptanceCriterionState.Open,
            ["job://focused-tests"]));
        task.Activity.Add(new TaskActivityEntry(
            "activity-initial",
            "user-1",
            PrincipalKind.Human,
            FixedInstant,
            TaskActivityKind.Decision,
            "Use first-class task documents."));

        var taskPath = ProjectTaskStorage.GetTaskDocumentPath(task.TaskId);
        var taskText = ProjectTaskSerializer.Serialize(task);
        var restoredTask = ProjectTaskSerializer.DeserializeTask(taskPath, taskText);
        var board = new TaskBoard(
            "board-main",
            [
                new TaskBoardColumn(ProjectTaskStatus.Backlog, []),
                new TaskBoardColumn(ProjectTaskStatus.Ready, ["task-alpha"]),
                new TaskBoardColumn(ProjectTaskStatus.InProgress, []),
                new TaskBoardColumn(ProjectTaskStatus.Blocked, []),
                new TaskBoardColumn(ProjectTaskStatus.Review, []),
                new TaskBoardColumn(ProjectTaskStatus.AwaitingAcceptance, []),
                new TaskBoardColumn(ProjectTaskStatus.Done, []),
                new TaskBoardColumn(ProjectTaskStatus.Cancelled, [])
            ]);
        var boardText = ProjectTaskSerializer.SerializeBoard(board);
        var restoredBoard = ProjectTaskSerializer.DeserializeBoard(ProjectTaskStorage.BoardDocumentPath, boardText);
        var taskClassification = ProjectDocumentClassifier.Classify(taskPath, taskText);
        var boardClassification = ProjectDocumentClassifier.Classify(ProjectTaskStorage.BoardDocumentPath, boardText);

        Assert.Equal(".electron2d/tasks/task-alpha.e2task", taskPath);
        Assert.EndsWith("\n", taskText);
        Assert.Contains("\"format\": \"Electron2D.TaskFile\"", taskText, StringComparison.Ordinal);
        Assert.Contains("\"version\": 1", taskText, StringComparison.Ordinal);
        Assert.Equal("task-alpha", restoredTask.TaskId);
        Assert.Contains(restoredTask.AcceptanceCriteria, criterion => criterion.CriterionId == "criterion-red-green");
        Assert.Equal("activity-initial", Assert.Single(restoredTask.Activity).ActivityEntryId);
        Assert.Equal(ProjectDocumentKind.EditorMetadata, taskClassification.Kind);
        Assert.Equal(ProjectDocumentContentKind.Json, taskClassification.ContentKind);
        Assert.Equal(ProjectTaskStorage.BoardDocumentPath, ".electron2d/tasks/board.e2tasks");
        Assert.Contains("\"format\": \"Electron2D.TaskBoard\"", boardText, StringComparison.Ordinal);
        Assert.Contains(restoredBoard.Columns, column =>
            column.Status == ProjectTaskStatus.Ready &&
            column.TaskIds.SequenceEqual(["task-alpha"]));
        Assert.Equal(ProjectDocumentKind.EditorMetadata, boardClassification.Kind);
        Assert.Equal(ProjectDocumentContentKind.Json, boardClassification.ContentKind);
    }

    [Fact]
    public void DependencyGraphRejectsCyclesAndKeepsManualBlockers()
    {
        var taskA = CreateTask("task-a", ProjectTaskStatus.Ready);
        var taskB = CreateTask("task-b", ProjectTaskStatus.Ready);
        taskB.Dependencies.Add("task-a");

        var cycle = TaskDependencyGraph.ValidateAddingDependency([taskA, taskB], "task-a", "task-b");

        Assert.False(cycle.Succeeded);
        Assert.Contains(cycle.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0003");

        var dependencyDone = CreateTask("task-done", ProjectTaskStatus.Done);
        var blocked = CreateTask("task-blocked", ProjectTaskStatus.Blocked);
        blocked.Dependencies.Add("task-done");
        blocked.Readiness = TaskReadiness.BlockedByDependencies;
        blocked.BlockingReasons.Add(TaskBlockingReason.Dependency);
        blocked.BlockingReasons.Add(TaskBlockingReason.Manual);

        var refreshed = TaskDependencyGraph.RefreshReadiness(blocked, [dependencyDone]);

        Assert.Equal(ProjectTaskStatus.Blocked, refreshed.Task.Status);
        Assert.Equal(TaskReadiness.Ready, refreshed.Task.Readiness);
        Assert.DoesNotContain(TaskBlockingReason.Dependency, refreshed.Task.BlockingReasons);
        Assert.Contains(TaskBlockingReason.Manual, refreshed.Task.BlockingReasons);

        var cancelledDependency = CreateTask("task-cancelled", ProjectTaskStatus.Cancelled);
        var dependent = CreateTask("task-dependent", ProjectTaskStatus.Ready);
        dependent.Dependencies.Add("task-cancelled");

        var cancelled = TaskDependencyGraph.RefreshReadiness(dependent, [cancelledDependency]);

        Assert.Equal(TaskReadiness.DependencyCancelled, cancelled.Task.Readiness);
        Assert.Contains(TaskBlockingReason.Dependency, cancelled.Task.BlockingReasons);
        Assert.Contains(cancelled.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0003");
    }

    [Fact]
    public void TaskMutationsUseTransactionsAndExternalImportRejectsPrivilegedFields()
    {
        using var workspace = CreateWorkspace(
            "external-guard",
            TaskDocument("task-alpha", ProjectTaskStatus.AwaitingAcceptance, ProjectTaskAcceptanceState.Submitted));

        var incomingTask = CreateTask("task-alpha", ProjectTaskStatus.Done);
        incomingTask.AcceptedBy = "agent-spoof";
        incomingTask.AcceptedAt = FixedInstant.AddHours(2);
        incomingTask.CompletedAt = FixedInstant.AddHours(2);
        incomingTask.AcceptanceState = ProjectTaskAcceptanceState.Accepted;
        incomingTask.Activity.Add(new TaskActivityEntry(
            "activity-spoof",
            "human-spoof",
            PrincipalKind.Human,
            FixedInstant.AddYears(-20),
            TaskActivityKind.AcceptanceResult,
            "spoofed direct file edit"));

        var rejected = workspace.Tasks.ImportExternalChange(new ProjectTaskExternalImportRequest(
            ProjectTaskStorage.GetTaskDocumentPath("task-alpha"),
            ProjectTaskSerializer.Serialize(incomingTask),
            new ProjectDocumentRevision(1),
            "op-external-task-import",
            "undo-external-task-import",
            ExternalFileContext()));

        Assert.False(rejected.Succeeded);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0002");
        Assert.Equal("pending-conflict", workspace.ImportState.States[ProjectTaskStorage.GetTaskDocumentPath("task-alpha")]);
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, workspace.Tasks.GetTask("task-alpha").Status);
        Assert.Null(workspace.Tasks.GetTask("task-alpha").AcceptedBy);
        Assert.DoesNotContain("undo-external-task-import", workspace.UndoRedo.UndoGroups);

        var changed = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            "task-alpha",
            ProjectTaskStatus.InProgress,
            new ProjectDocumentRevision(1),
            "op-human-request-through-status",
            "undo-human-request-through-status",
            HumanContext(OperationCapability.TaskRequestChanges)));

        Assert.True(changed.Succeeded);
        Assert.Equal(ProjectWorkspacePersistenceState.Dirty, changed.TransactionResult.PersistenceState);
        Assert.Equal([".electron2d/tasks/task-alpha.e2task"], changed.TransactionResult.DirtyDocuments);
        Assert.Contains("undo-human-request-through-status", workspace.UndoRedo.UndoGroups);

        var taskFile = Path.Combine(workspace.ProjectRoot, ".electron2d", "tasks", "task-alpha.e2task");
        Assert.Contains("\"status\": \"AwaitingAcceptance\"", File.ReadAllText(taskFile), StringComparison.Ordinal);

        var saved = workspace.Transactions.Apply(WorkspaceTransactionRequest.SaveAffectedDocuments(
            "op-save-task-documents",
            ProjectWorkspaceActorKind.Human,
            dryRun: false));

        Assert.True(saved.Succeeded);
        Assert.Contains("\"status\": \"InProgress\"", File.ReadAllText(taskFile), StringComparison.Ordinal);
        Assert.Empty(saved.DirtyDocuments);
    }

    private static ProjectWorkspace CreateWorkspace(string name, string taskText, string taskId = "task-alpha")
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-ProjectTaskManagerTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".electron2d", "tasks"));
        var taskPath = Path.Combine(root, ".electron2d", "tasks", $"{taskId}.e2task");
        File.WriteAllText(taskPath, taskText);

        var workspace = ProjectWorkspace.CreateHeadless(root, $"owner-{name}");
        workspace.CommandBus.OpenTextDocument(
            ProjectTaskStorage.GetTaskDocumentPath(taskId),
            taskText,
            1,
            ProjectWorkspaceOperationContext.ForTest($"open-{name}"));
        return workspace;
    }

    private static string TaskDocument(
        string taskId,
        ProjectTaskStatus status,
        ProjectTaskAcceptanceState acceptanceState = ProjectTaskAcceptanceState.Open)
    {
        return ProjectTaskSerializer.Serialize(CreateTask(taskId, status, acceptanceState));
    }

    private static ProjectTask CreateTask(
        string taskId,
        ProjectTaskStatus status,
        ProjectTaskAcceptanceState acceptanceState = ProjectTaskAcceptanceState.Open)
    {
        var task = new ProjectTask
        {
            TaskId = taskId,
            Title = $"Task {taskId}",
            Description = "Exercise ProjectTaskManager core behavior.",
            Status = status,
            Readiness = TaskReadiness.Ready,
            Priority = "P0",
            Rank = "1000",
            Assignee = "agent-1",
            CreatedBy = "user-1",
            CreatedAt = FixedInstant,
            UpdatedAt = FixedInstant,
            SubmittedAt = acceptanceState == ProjectTaskAcceptanceState.Submitted ? FixedInstant.AddHours(1) : null,
            CompletedAt = status == ProjectTaskStatus.Done ? FixedInstant.AddHours(2) : null,
            AcceptedAt = status == ProjectTaskStatus.Done ? FixedInstant.AddHours(2) : null,
            AcceptedBy = status == ProjectTaskStatus.Done ? "user-1" : null,
            AcceptanceState = acceptanceState
        };
        task.Labels.Add("release");
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            $"criterion-{taskId}",
            "The task has a focused test and implementation documentation.",
            AcceptanceCriterionState.Open,
            []));
        task.LinkedTransactions.Add("transaction://baseline");
        task.LinkedJobs.Add("job://baseline");
        task.LinkedDiagnostics.Add("diagnostic://baseline");
        task.LinkedArtifacts.Add("artifact://baseline");
        task.LinkedScenesResourcesAndNodes.Add("scene://main");
        return task;
    }

    private static OperationContext AgentContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "agent-1",
            PrincipalKind.Agent,
            "agent-session-1",
            capabilities,
            "mcp");
    }

    private static OperationContext HumanContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "user-1",
            PrincipalKind.Human,
            "editor-session-1",
            capabilities,
            "editor");
    }

    private static OperationContext ExternalFileContext()
    {
        return new OperationContext(
            "file-watcher",
            PrincipalKind.ExternalFile,
            "external-session-1",
            [],
            "file-system");
    }
}
