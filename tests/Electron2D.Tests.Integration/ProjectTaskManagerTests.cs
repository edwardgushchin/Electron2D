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

    [Theory]
    [InlineData("task-file-v3.schema.json", "Electron2D task file v3 schema")]
    [InlineData("task-board-v3.schema.json", "Electron2D task board v3 schema")]
    public void TaskboardV3JsonSchemasArePublished(string fileName, string title)
    {
        var schemaPath = Path.Combine(FindRepositoryRoot(), "data", "schemas", "project-system", fileName);

        Assert.True(File.Exists(schemaPath), $"Taskboard schema '{fileName}' must be published.");
        using var schema = System.Text.Json.JsonDocument.Parse(File.ReadAllText(schemaPath));
        Assert.Equal("https://json-schema.org/draft/2020-12/schema", schema.RootElement.GetProperty("$schema").GetString());
        Assert.Equal(title, schema.RootElement.GetProperty("title").GetString());
        Assert.Equal("object", schema.RootElement.GetProperty("type").GetString());
        Assert.False(schema.RootElement.GetProperty("additionalProperties").GetBoolean());
    }

    [Fact]
    public void LegacyTaskboardV2JsonSchemasAreNotPublishedAfterCutover()
    {
        var schemaRoot = Path.Combine(FindRepositoryRoot(), "data", "schemas", "project-system");

        Assert.False(File.Exists(Path.Combine(schemaRoot, "task-file-v2.schema.json")));
        Assert.False(File.Exists(Path.Combine(schemaRoot, "task-board-v2.schema.json")));
    }

    [Fact]
    public void TaskboardV3SchemasUseOnlyCanonicalDescriptionField()
    {
        var taskSchemaPath = Path.Combine(FindRepositoryRoot(), "data", "schemas", "project-system", "task-file-v3.schema.json");
        var boardSchemaPath = Path.Combine(FindRepositoryRoot(), "data", "schemas", "project-system", "task-board-v3.schema.json");
        using var taskSchema = System.Text.Json.JsonDocument.Parse(File.ReadAllText(taskSchemaPath));
        using var boardSchema = System.Text.Json.JsonDocument.Parse(File.ReadAllText(boardSchemaPath));

        var taskProperties = taskSchema.RootElement.GetProperty("properties");
        var groupProperties = boardSchema.RootElement.GetProperty("properties").GetProperty("groups")
            .GetProperty("items").GetProperty("properties");
        Assert.True(taskProperties.TryGetProperty("description", out _));
        Assert.False(taskProperties.TryGetProperty("descriptionMarkdown", out _));
        Assert.True(groupProperties.TryGetProperty("description", out _));
        Assert.False(groupProperties.TryGetProperty("descriptionMarkdown", out _));
    }

    [Fact]
    public void TaskboardV3StatusContractHasSingleReviewStatus()
    {
        var taskSchemaPath = Path.Combine(FindRepositoryRoot(), "data", "schemas", "project-system", "task-file-v3.schema.json");
        using var taskSchema = System.Text.Json.JsonDocument.Parse(File.ReadAllText(taskSchemaPath));

        var schemaStatuses = taskSchema.RootElement.GetProperty("properties").GetProperty("status")
            .GetProperty("enum").EnumerateArray().Select(item => item.GetString()!).ToArray();

        Assert.Equal(
            ["Ready", "InProgress", "Blocked", "Review", "Done", "Cancelled"],
            schemaStatuses);
        Assert.DoesNotContain("Backlog", Enum.GetNames<ProjectTaskStatus>());
        Assert.DoesNotContain("AwaitingAcceptance", Enum.GetNames<ProjectTaskStatus>());
    }

    [Fact]
    public void TaskboardV2ReaderRejectsLegacyDescriptionField()
    {
        var task = CreateTask("T-0001", ProjectTaskStatus.Ready);
        var legacyTaskText = ProjectTaskSerializer.Serialize(task)
            .Replace("\"description\":", "\"descriptionMarkdown\":", StringComparison.Ordinal);
        var board = new TaskBoard(
            "main",
            revision: 1,
            groups: [new TaskBoardGroup("epoch-1", TaskBoardGroupKind.Epoch, "Epoch", string.Empty, null, "00001000")],
            placements: []);
        var legacyBoardText = ProjectTaskSerializer.SerializeBoard(board)
            .Replace("\"description\":", "\"descriptionMarkdown\":", StringComparison.Ordinal);

        Assert.Throws<FormatException>(() => ProjectTaskSerializer.DeserializeTask(
            ProjectTaskStorage.GetTaskDocumentPath(task.TaskId),
            legacyTaskText));
        Assert.Throws<FormatException>(() => ProjectTaskSerializer.DeserializeBoard(
            ProjectTaskStorage.BoardDocumentPath,
            legacyBoardText));
    }

    [Fact]
    public void TaskBoardDiskStoreRecoversPreparedTransactionBeforeReadingSnapshot()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-TaskRecovery-" + Guid.NewGuid().ToString("N"));
        var taskboardRoot = Path.Combine(projectRoot, ".taskboard");
        var transactionsRoot = Path.Combine(taskboardRoot, ".transactions");
        var stagingRoot = Path.Combine(taskboardRoot, ".staging", "tx-recovery");
        Directory.CreateDirectory(transactionsRoot);
        Directory.CreateDirectory(stagingRoot);
        Directory.CreateDirectory(Path.Combine(taskboardRoot, "tasks"));
        Directory.CreateDirectory(Path.Combine(taskboardRoot, "completed"));

        var beforeText = ProjectTaskSerializer.SerializeBoard(new TaskBoard("main", 1, [], []));
        var afterText = ProjectTaskSerializer.SerializeBoard(new TaskBoard("main", 2, [], []));
        var boardPath = Path.Combine(taskboardRoot, "board.e2tasks");
        var stagedPath = Path.Combine(stagingRoot, "0000.stage");
        File.WriteAllText(boardPath, beforeText);
        File.WriteAllText(stagedPath, afterText);
        var beforeHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(beforeText))).ToLowerInvariant();
        var afterHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(afterText))).ToLowerInvariant();
        File.WriteAllText(
            Path.Combine(transactionsRoot, "tx-recovery.json"),
            $$"""
            {
              "format": "Electron2D.TaskTransaction",
              "version": 1,
              "transactionId": "tx-recovery",
              "state": "prepared",
              "operations": [
                {
                  "kind": "replace",
                  "path": ".taskboard/board.e2tasks",
                  "stagedPath": ".taskboard/.staging/tx-recovery/0000.stage",
                  "beforeSha256": "{{beforeHash}}",
                  "afterSha256": "{{afterHash}}"
                }
              ]
            }
            """);

        var store = new TaskBoardDiskStore(projectRoot);
        var recovered = store.LoadBoard();

        Assert.Equal(2, recovered.Revision);
        Assert.Empty(Directory.EnumerateFiles(transactionsRoot));
        Assert.False(Directory.Exists(stagingRoot));
    }

    [Fact]
    public void AcceptanceGuardAllowsAgentSubmitButRequiresHumanDone()
    {
        var task = CreateTask("task-alpha", ProjectTaskStatus.Review);
        using var workspace = CreateWorkspace("acceptance-guard", ProjectTaskSerializer.Serialize(task));

        var submitted = workspace.Tasks.ChangeStatus(new ProjectTaskStatusChangeRequest(
            "task-alpha",
            ProjectTaskStatus.Review,
            new ProjectDocumentRevision(1),
            "op-agent-submit",
            "undo-agent-submit",
            AgentContext(OperationCapability.TaskSubmitForAcceptance)));

        Assert.True(submitted.Succeeded);
        Assert.Equal(ProjectTaskStatus.Review, submitted.Task.Status);
        Assert.NotNull(submitted.Task.SubmittedAt);
        Assert.Equal(ProjectTaskAcceptanceState.Submitted, submitted.Task.AcceptanceState);
        Assert.Contains("undo-agent-submit", workspace.UndoRedo.UndoGroups);
        Assert.Equal([".taskboard/tasks/task-alpha.e2task"], submitted.TransactionResult.DirtyDocuments);
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
        Assert.Equal(ProjectTaskStatus.Review, workspace.Tasks.GetTask("task-alpha").Status);

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
        Assert.All(accepted.Task.AcceptanceCriteria, criterion =>
            Assert.Equal(AcceptanceCriterionState.Open, criterion.State));
        Assert.Contains(accepted.Task.Activity, activity => activity.Kind == TaskActivityKind.AcceptanceResult);
    }

    [Fact]
    public void RequestChangesAndReopenPreserveAcceptanceHistory()
    {
        using var changesWorkspace = CreateWorkspace(
            "request-changes",
            TaskDocument("task-review", ProjectTaskStatus.Review, ProjectTaskAcceptanceState.Submitted),
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
    public void StorageRoundTripsCanonicalTaskboardV2DocumentsAsEditorMetadata()
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
                new TaskBoardColumn(ProjectTaskStatus.Ready, ["task-alpha"]),
                new TaskBoardColumn(ProjectTaskStatus.InProgress, []),
                new TaskBoardColumn(ProjectTaskStatus.Blocked, []),
                new TaskBoardColumn(ProjectTaskStatus.Review, []),
                new TaskBoardColumn(ProjectTaskStatus.Done, []),
                new TaskBoardColumn(ProjectTaskStatus.Cancelled, [])
            ]);
        var boardText = ProjectTaskSerializer.SerializeBoard(board);
        var restoredBoard = ProjectTaskSerializer.DeserializeBoard(ProjectTaskStorage.BoardDocumentPath, boardText);
        var taskClassification = ProjectDocumentClassifier.Classify(taskPath, taskText);
        var boardClassification = ProjectDocumentClassifier.Classify(ProjectTaskStorage.BoardDocumentPath, boardText);

        Assert.Equal(".taskboard/tasks/task-alpha.e2task", taskPath);
        Assert.EndsWith("\n", taskText);
        Assert.Contains("\"format\": \"Electron2D.TaskFile\"", taskText, StringComparison.Ordinal);
        Assert.Contains("\"version\": 2", taskText, StringComparison.Ordinal);
        Assert.Contains("\"taskUid\":", taskText, StringComparison.Ordinal);
        Assert.Contains("\"revision\":", taskText, StringComparison.Ordinal);
        Assert.Contains("\"description\":", taskText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"descriptionMarkdown\":", taskText, StringComparison.Ordinal);
        Assert.Contains("\"executionContract\":", taskText, StringComparison.Ordinal);
        Assert.Contains("\"attachments\":", taskText, StringComparison.Ordinal);
        Assert.Contains("\"legacySourceFragments\":", taskText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"readiness\":", taskText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"rank\":", taskText, StringComparison.Ordinal);
        Assert.Equal("task-alpha", restoredTask.TaskId);
        Assert.Contains(restoredTask.AcceptanceCriteria, criterion => criterion.CriterionId == "criterion-red-green");
        Assert.Equal("activity-initial", Assert.Single(restoredTask.Activity).ActivityEntryId);
        Assert.Equal(ProjectDocumentKind.EditorMetadata, taskClassification.Kind);
        Assert.Equal(ProjectDocumentContentKind.Json, taskClassification.ContentKind);
        Assert.Equal(ProjectTaskStorage.BoardDocumentPath, ".taskboard/board.e2tasks");
        Assert.Contains("\"format\": \"Electron2D.TaskBoard\"", boardText, StringComparison.Ordinal);
        Assert.Contains("\"version\": 2", boardText, StringComparison.Ordinal);
        Assert.Contains("\"revision\":", boardText, StringComparison.Ordinal);
        Assert.Contains("\"idPolicy\":", boardText, StringComparison.Ordinal);
        Assert.Contains("\"attachmentPolicy\":", boardText, StringComparison.Ordinal);
        Assert.Contains("\"migration\":", boardText, StringComparison.Ordinal);
        Assert.Contains("\"legacySourceFragments\":", boardText, StringComparison.Ordinal);
        Assert.Contains("\"groups\":", boardText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"descriptionMarkdown\":", boardText, StringComparison.Ordinal);
        Assert.Contains("\"placements\":", boardText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"columns\":", boardText, StringComparison.Ordinal);
        Assert.Equal("board-main", restoredBoard.BoardId);
        Assert.Equal(ProjectDocumentKind.EditorMetadata, boardClassification.Kind);
        Assert.Equal(ProjectDocumentContentKind.Json, boardClassification.ContentKind);
    }

    [Fact]
    public void TaskboardV2RoundTripPreservesTagCatalogAndOptionalDeadline()
    {
        var task = CreateTask("T-0001", ProjectTaskStatus.Ready);
        task.Labels.Clear();
        task.Labels.Add("tag-ui");
        task.Deadline = new DateOnly(2026, 8, 26);
        var board = new TaskBoard(
            "main",
            revision: 7,
            groups: [],
            placements: [new TaskBoardPlacement(task.TaskId, groupId: null, "00001000")]);
        board.Tags.Add(new TaskBoardTag("tag-ui", "Интерфейс", TaskBoardTagColor.Blue));

        var restoredTask = ProjectTaskSerializer.DeserializeTask(
            ProjectTaskStorage.GetTaskDocumentPath(task.TaskId),
            ProjectTaskSerializer.Serialize(task));
        var restoredBoard = ProjectTaskSerializer.DeserializeBoard(
            ProjectTaskStorage.BoardDocumentPath,
            ProjectTaskSerializer.SerializeBoard(board));

        Assert.Equal(new DateOnly(2026, 8, 26), restoredTask.Deadline);
        Assert.Equal("tag-ui", Assert.Single(restoredTask.Labels));
        var tag = Assert.Single(restoredBoard.Tags);
        Assert.Equal("tag-ui", tag.TagId);
        Assert.Equal("Интерфейс", tag.Name);
        Assert.Equal(TaskBoardTagColor.Blue, tag.Color);

        var taskWithoutDeadline = CreateTask("T-0002", ProjectTaskStatus.Ready);
        taskWithoutDeadline.Deadline = null;
        Assert.DoesNotContain("\"deadline\"", ProjectTaskSerializer.Serialize(taskWithoutDeadline), StringComparison.Ordinal);
    }

    [Fact]
    public void TaskboardV2RoundTripPreservesSelectedRasterPreviewAndRejectsInvalidSelection()
    {
        var task = CreateTask("T-0001", ProjectTaskStatus.Ready);
        task.Attachments.Add(new TaskAttachment
        {
            AttachmentId = "A-0001",
            DisplayName = "cover.png",
            RelativePath = ".taskboard/attachments/T-0001/A-0001/cover.png",
            MediaType = "image/png",
            ByteLength = 8,
            Sha256 = new string('a', 64),
            AddedAt = FixedInstant,
            AddedBy = "test"
        });
        task.PreviewAttachmentId = "A-0001";

        var text = ProjectTaskSerializer.Serialize(task);
        var restored = ProjectTaskSerializer.DeserializeTask(ProjectTaskStorage.GetTaskDocumentPath(task.TaskId), text);

        Assert.Contains("\"previewAttachmentId\": \"A-0001\"", text, StringComparison.Ordinal);
        Assert.Equal("A-0001", restored.PreviewAttachmentId);

        task.PreviewAttachmentId = "A-9999";
        Assert.Throws<InvalidOperationException>(() => ProjectTaskSerializer.Serialize(task));
        task.PreviewAttachmentId = null;
        Assert.DoesNotContain("\"previewAttachmentId\"", ProjectTaskSerializer.Serialize(task), StringComparison.Ordinal);
    }

    [Fact]
    public void TaskboardVerifyRejectsUnknownGlobalTagReferences()
    {
        var projectRoot = Path.Combine(Path.GetTempPath(), "Electron2D-TaskTags-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "tasks"));
        Directory.CreateDirectory(Path.Combine(projectRoot, ".taskboard", "completed"));
        var task = CreateTask("T-0001", ProjectTaskStatus.Ready);
        task.Labels.Clear();
        task.Labels.Add("tag-missing");
        var board = new TaskBoard(
            "main",
            revision: 1,
            groups: [],
            placements: [new TaskBoardPlacement(task.TaskId, null, "00001000")]);
        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "board.e2tasks"), ProjectTaskSerializer.SerializeBoard(board));
        File.WriteAllText(Path.Combine(projectRoot, ".taskboard", "tasks", "T-0001.e2task"), ProjectTaskSerializer.Serialize(task));

        var exception = Assert.Throws<InvalidOperationException>(() => new TaskBoardDiskStore(projectRoot).Verify());

        Assert.Contains("tag-missing", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TaskboardSerializerWritesUnicodeLettersAsReadableUtf8()
    {
        var task = CreateTask("T-0001", ProjectTaskStatus.Ready);
        task.Title = "Привести сериализацию задач к читаемому UTF-8";
        task.Description = "Этап `0.1-preview`, поэтому `T-0093` остаётся заблокирован 🕓.";
        var board = new TaskBoard(
            "main",
            revision: 1,
            groups: [new TaskBoardGroup("epoch-1", TaskBoardGroupKind.Epoch, "Эпоха интерфейса", string.Empty, null, "00001000")],
            placements: [new TaskBoardPlacement(task.TaskId, "epoch-1", "00001000")]);

        var taskText = ProjectTaskSerializer.Serialize(task);
        var boardText = ProjectTaskSerializer.SerializeBoard(board);

        Assert.Contains("Привести сериализацию задач", taskText, StringComparison.Ordinal);
        Assert.Contains("Этап `0.1-preview`", taskText, StringComparison.Ordinal);
        Assert.Contains("заблокирован 🕓", taskText, StringComparison.Ordinal);
        Assert.Contains("Эпоха интерфейса", boardText, StringComparison.Ordinal);
        Assert.Contains("\"description\":", boardText, StringComparison.Ordinal);
        Assert.DoesNotContain("\"descriptionMarkdown\":", boardText, StringComparison.Ordinal);
        Assert.DoesNotContain("\\u041F", taskText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u0060", taskText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\uD83D", taskText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\\u042D", boardText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DependencyGraphRejectsCyclesAndKeepsManualBlockers()
    {
        var taskA = CreateTask("task-a", ProjectTaskStatus.Ready);
        var taskB = CreateTask("task-b", ProjectTaskStatus.Ready);
        taskB.Dependencies.Add("task-a");

        var missing = TaskDependencyGraph.ValidateAddingDependency([taskA, taskB], "task-a", "task-missing");

        Assert.False(missing.Succeeded);
        Assert.Contains(missing.Diagnostics, diagnostic =>
            diagnostic.Code == "E2D-TASK-0003" &&
            diagnostic.Message.Contains("was not found", StringComparison.Ordinal));

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
            TaskDocument("task-alpha", ProjectTaskStatus.Review, ProjectTaskAcceptanceState.Submitted));

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
        Assert.Equal(ProjectTaskStatus.Review, workspace.Tasks.GetTask("task-alpha").Status);
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
        Assert.Equal([".taskboard/tasks/task-alpha.e2task"], changed.TransactionResult.DirtyDocuments);
        Assert.Contains("undo-human-request-through-status", workspace.UndoRedo.UndoGroups);

        var taskFile = Path.Combine(workspace.ProjectRoot, ".taskboard", "tasks", "task-alpha.e2task");
        Assert.Contains("\"status\": \"Review\"", File.ReadAllText(taskFile), StringComparison.Ordinal);

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
        Directory.CreateDirectory(Path.Combine(root, ".taskboard", "tasks"));
        var taskPath = Path.Combine(root, ".taskboard", "tasks", $"{taskId}.e2task");
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

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "src", "Electron2D.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root could not be found.");
    }
}
