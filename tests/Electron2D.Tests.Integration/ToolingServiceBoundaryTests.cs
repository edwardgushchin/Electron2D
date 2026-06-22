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
using Electron2D.Tooling;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class ToolingServiceBoundaryTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProjectServiceWrapsWorkspaceTransactionsWithStableResultShape()
    {
        using var workspace = CreateWorkspace("project-service-transactions", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        var host = new ProjectToolingHost(workspace);
        var scenePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");

        var changed = host.Project.ApplyTextEdit(new ToolingTextEditRequest(
            "op-scene-workspace",
            "scene.set-property",
            ToolingApplyMode.WorkspaceOnly,
            "scenes/main.scene.json",
            new ProjectDocumentRevision(1),
            SceneText(speed: 12),
            "undo-scene-workspace"),
            AgentContext(OperationCapability.TaskWrite));

        Assert.True(changed.Succeeded);
        Assert.Equal("op-scene-workspace", changed.OperationId);
        Assert.Equal("scene.set-property", changed.OperationKind);
        Assert.Equal("undo-scene-workspace", changed.UndoGroupId);
        Assert.Equal(new ProjectDocumentRevision(2), changed.DocumentRevisions["scenes/main.scene.json"]);
        Assert.Equal(new ProjectDocumentRevision(1), changed.PersistedRevision);
        Assert.Equal(["scenes/main.scene.json"], changed.DirtyDocuments);
        Assert.Equal(ProjectWorkspacePersistenceState.Dirty, changed.PersistenceState);
        Assert.Contains(changed.ChangedObjects, value => value.Contains("scene-node:1", StringComparison.Ordinal));
        Assert.Contains("undo-scene-workspace", workspace.UndoRedo.UndoGroups);
        Assert.Contains("\"value\": 10", File.ReadAllText(scenePath), StringComparison.Ordinal);

        var saved = host.Project.SaveAffectedDocuments(
            "op-save-scene",
            AgentContext(OperationCapability.TaskWrite),
            dryRun: false);

        Assert.True(saved.Succeeded);
        Assert.Equal("workspace.save-affected-documents", saved.OperationKind);
        Assert.Equal(["scenes/main.scene.json"], saved.ChangedFiles);
        Assert.Empty(saved.DirtyDocuments);
        Assert.Equal(ProjectWorkspacePersistenceState.Clean, saved.PersistenceState);
        Assert.Contains("\"value\": 12", File.ReadAllText(scenePath), StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectServiceSupportsHeadlessCommitExternalImportAndFailClosedErrors()
    {
        using var workspace = CreateWorkspace("project-service-modes", SceneText(speed: 10, health: 100), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        var host = new ProjectToolingHost(workspace);
        var scenePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");

        var headless = host.Project.ApplyTextEdit(new ToolingTextEditRequest(
            "op-headless-scene",
            "scene.set-property",
            ToolingApplyMode.HeadlessCommit,
            "scenes/main.scene.json",
            new ProjectDocumentRevision(1),
            SceneText(speed: 14, health: 100),
            undoGroupId: null),
            CliContext());

        Assert.True(headless.Succeeded);
        Assert.Equal(ProjectWorkspacePersistenceState.Clean, headless.PersistenceState);
        Assert.Equal(["scenes/main.scene.json"], headless.ChangedFiles);
        Assert.Contains("\"value\": 14", File.ReadAllText(scenePath), StringComparison.Ordinal);

        workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-human-dirty-health",
            ProjectWorkspaceActorKind.Human,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-human-health",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(2),
                    SceneText(speed: 14, health: 80))
            ]));

        var imported = host.Project.ApplyTextEdit(new ToolingTextEditRequest(
            "op-external-speed",
            "external.import",
            ToolingApplyMode.ExternalImport,
            "scenes/main.scene.json",
            new ProjectDocumentRevision(3),
            SceneText(speed: 16, health: 100),
            "undo-external-speed"),
            ExternalFileContext());

        Assert.True(imported.Succeeded);
        var mergedText = workspace.Documents.GetByPath("scenes/main.scene.json").Text;
        Assert.Contains("\"value\": 16", mergedText, StringComparison.Ordinal);
        Assert.Contains("\"value\": 80", mergedText, StringComparison.Ordinal);

        var stale = host.Project.ApplyTextEdit(new ToolingTextEditRequest(
            "op-stale-scene",
            "scene.set-property",
            ToolingApplyMode.WorkspaceOnly,
            "scenes/main.scene.json",
            new ProjectDocumentRevision(2),
            SceneText(speed: 18, health: 80),
            "undo-stale-scene"),
            AgentContext(OperationCapability.TaskWrite));

        Assert.False(stale.Succeeded);
        Assert.Contains(stale.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.DoesNotContain("undo-stale-scene", workspace.UndoRedo.UndoGroups);

        var unsafePath = host.Project.ApplyTextEdit(new ToolingTextEditRequest(
            "op-unsafe-cache",
            "scene.set-property",
            ToolingApplyMode.WorkspaceOnly,
            ".electron2d/import-cache/generated.json",
            new ProjectDocumentRevision(0),
            "{\"format\":\"Electron2D.Generated\",\"version\":1}",
            "undo-unsafe-cache"),
            AgentContext(OperationCapability.TaskWrite));

        Assert.False(unsafePath.Succeeded);
        Assert.Contains(unsafePath.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
    }

    [Fact]
    public void TaskServiceUsesProjectTaskManagerAndRejectsAgentAcceptance()
    {
        using var workspace = CreateWorkspace("task-service-guard", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.Review));
        var host = new ProjectToolingHost(workspace);

        Assert.Contains("task_create", host.Tasks.SupportedCommands);
        Assert.Contains("task_accept", host.Tasks.SupportedCommands);
        Assert.Contains("task_request_changes", host.Tasks.SupportedCommands);

        var submitted = host.Tasks.SubmitForAcceptance(new ToolingTaskStatusRequest(
            "task-alpha",
            new ProjectDocumentRevision(1),
            "op-tooling-submit",
            "undo-tooling-submit",
            AgentContext(OperationCapability.TaskSubmitForAcceptance)));

        Assert.True(submitted.Succeeded);
        Assert.Equal("task-alpha", submitted.TaskId);
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, workspace.Tasks.GetTask("task-alpha").Status);
        Assert.Equal([".electron2d/tasks/task-alpha.e2task"], submitted.DirtyDocuments);

        var agentAccept = host.Tasks.Accept(new ToolingTaskStatusRequest(
            "task-alpha",
            new ProjectDocumentRevision(2),
            "op-agent-accept",
            "undo-agent-accept",
            AgentContext(OperationCapability.TaskSubmitForAcceptance)));

        Assert.False(agentAccept.Succeeded);
        Assert.Contains(agentAccept.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0002");
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, workspace.Tasks.GetTask("task-alpha").Status);

        var humanAccept = host.Tasks.Accept(new ToolingTaskStatusRequest(
            "task-alpha",
            new ProjectDocumentRevision(2),
            "op-human-accept",
            "undo-human-accept",
            HumanContext(OperationCapability.TaskAccept)));

        Assert.True(humanAccept.Succeeded);
        Assert.Equal(ProjectTaskStatus.Done, workspace.Tasks.GetTask("task-alpha").Status);
        Assert.Equal("user-1", workspace.Tasks.GetTask("task-alpha").AcceptedBy);
    }

    [Fact]
    public void TaskServiceAppendsActivityAndLinksArtifactsThroughTransactions()
    {
        using var workspace = CreateWorkspace("task-service-links", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        var host = new ProjectToolingHost(workspace);

        var activity = host.Tasks.AppendActivity(new ToolingTaskActivityRequest(
            "task-alpha",
            TaskActivityKind.AgentSummary,
            "message=Implemented ProjectService wrapper.",
            new ProjectDocumentRevision(1),
            "op-task-activity",
            "undo-task-activity",
            AgentContext(OperationCapability.TaskWrite)));

        Assert.True(activity.Succeeded);
        Assert.Equal(ProjectWorkspacePersistenceState.Dirty, activity.PersistenceState);
        Assert.Contains("undo-task-activity", workspace.UndoRedo.UndoGroups);

        var linkTransaction = host.Tasks.LinkTransaction(new ToolingTaskLinkRequest(
            "task-alpha",
            "transaction://op-task-activity",
            new ProjectDocumentRevision(2),
            "op-link-transaction",
            "undo-link-transaction",
            AgentContext(OperationCapability.TaskWrite)));
        var linkJob = host.Tasks.LinkJob(new ToolingTaskLinkRequest(
            "task-alpha",
            "job://op-build",
            new ProjectDocumentRevision(3),
            "op-link-job",
            "undo-link-job",
            AgentContext(OperationCapability.TaskWrite)));
        var linkArtifact = host.Tasks.LinkArtifact(new ToolingTaskLinkRequest(
            "task-alpha",
            "artifact://screenshot",
            new ProjectDocumentRevision(4),
            "op-link-artifact",
            "undo-link-artifact",
            AgentContext(OperationCapability.TaskWrite)));

        Assert.True(linkTransaction.Succeeded);
        Assert.True(linkJob.Succeeded);
        Assert.True(linkArtifact.Succeeded);
        var task = workspace.Tasks.GetTask("task-alpha");
        Assert.Contains("transaction://op-task-activity", task.LinkedTransactions);
        Assert.Contains("job://op-build", task.LinkedJobs);
        Assert.Contains("artifact://screenshot", task.LinkedArtifacts);
        Assert.Contains(task.Activity, entry => entry.Kind == TaskActivityKind.AgentSummary);
    }

    [Fact]
    public void LongRunningServicesCreateWorkspaceJobsInsteadOfBooleanResults()
    {
        using var workspace = CreateWorkspace("job-backed-services", SceneText(speed: 10), TaskText("task-alpha", ProjectTaskStatus.InProgress));
        var host = new ProjectToolingHost(workspace);

        var build = host.Build.Queue(new ToolingJobRequest("op-build", "sha256:debug"));
        var test = host.Tests.Queue(new ToolingJobRequest("op-test", "sha256:test"));
        var export = host.Export.Queue(new ToolingJobRequest("op-export", "sha256:export"));
        var import = host.Import.Queue(new ToolingJobRequest("op-import", "sha256:import"));
        var run = host.Runtime.Queue(new ToolingJobRequest("op-run", "sha256:run"));

        Assert.Equal(WorkspaceJobKind.Build, build.JobKind);
        Assert.Equal(WorkspaceJobKind.Test, test.JobKind);
        Assert.Equal(WorkspaceJobKind.Export, export.JobKind);
        Assert.Equal(WorkspaceJobKind.Import, import.JobKind);
        Assert.Equal(WorkspaceJobKind.Run, run.JobKind);
        Assert.All([build, test, export, import, run], result =>
        {
            Assert.True(result.Succeeded);
            Assert.Equal(WorkspaceJobState.Queued, result.JobState);
            Assert.False(string.IsNullOrWhiteSpace(result.JobId));
            Assert.False(string.IsNullOrWhiteSpace(result.InputSnapshotId));
            Assert.Contains("scenes/main.scene.json", result.InputDocumentRevisions.Keys);
        });
        Assert.Equal(5, workspace.Jobs.Jobs.Count);
    }

    private static ProjectWorkspace CreateWorkspace(string name, string sceneText, string taskText)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D-ToolingServiceBoundaryTests", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        Directory.CreateDirectory(Path.Combine(root, ".electron2d", "tasks"));
        File.WriteAllText(Path.Combine(root, "scenes", "main.scene.json"), sceneText);
        File.WriteAllText(Path.Combine(root, ".electron2d", "tasks", "task-alpha.e2task"), taskText);

        var workspace = ProjectWorkspace.CreateHeadless(root, $"owner-{name}");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            sceneText,
            1,
            ProjectWorkspaceOperationContext.ForTest($"open-scene-{name}"));
        workspace.CommandBus.OpenTextDocument(
            ".electron2d/tasks/task-alpha.e2task",
            taskText,
            1,
            ProjectWorkspaceOperationContext.ForTest($"open-task-{name}"));
        return workspace;
    }

    private static string SceneText(int speed, int health = 100)
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
                },
                "health": {
                  "type": "Int",
                  "value": {{health}}
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
            Description = "Exercise Tooling task service.",
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
            "criterion-tooling",
            "Tooling service operation is covered by focused tests.",
            AcceptanceCriterionState.Open,
            []));
        return ProjectTaskSerializer.Serialize(task);
    }

    private static OperationContext AgentContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "agent-1",
            PrincipalKind.Agent,
            "agent-session-1",
            capabilities,
            "tooling-test");
    }

    private static OperationContext HumanContext(params OperationCapability[] capabilities)
    {
        return new OperationContext(
            "user-1",
            PrincipalKind.Human,
            "editor-session-1",
            capabilities,
            "editor-test");
    }

    private static OperationContext CliContext()
    {
        return new OperationContext(
            "cli",
            PrincipalKind.Cli,
            "cli-session-1",
            [OperationCapability.TaskWrite],
            "cli-test");
    }

    private static OperationContext ExternalFileContext()
    {
        return new OperationContext(
            "file-watcher",
            PrincipalKind.ExternalFile,
            "external-session-1",
            [],
            "file-test");
    }
}
