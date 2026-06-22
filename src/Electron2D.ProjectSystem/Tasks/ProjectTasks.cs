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
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum ProjectTaskStatus
{
    Backlog,
    Ready,
    InProgress,
    Blocked,
    Review,
    AwaitingAcceptance,
    Done,
    Cancelled
}

internal enum TaskReadiness
{
    Ready,
    BlockedByDependencies,
    DependencyCancelled
}

internal enum TaskBlockingReason
{
    Dependency,
    Environment,
    Decision,
    External,
    Manual
}

internal enum ProjectTaskAcceptanceState
{
    Open,
    Submitted,
    Accepted,
    ChangesRequested,
    Reopened,
    Cancelled
}

internal enum AcceptanceCriterionState
{
    Open,
    Passed,
    Failed
}

internal enum TaskActivityKind
{
    Comment,
    Decision,
    Investigation,
    Blocker,
    TestResult,
    StatusChange,
    AgentSummary,
    AcceptanceResult
}

internal sealed class AcceptanceCriterion
{
    public AcceptanceCriterion(
        string criterionId,
        string description,
        AcceptanceCriterionState state,
        IEnumerable<string> evidenceLinks)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(evidenceLinks);

        CriterionId = criterionId;
        Description = description;
        State = state;
        EvidenceLinks = evidenceLinks.ToList();
    }

    public string CriterionId { get; }

    public string Description { get; }

    public AcceptanceCriterionState State { get; }

    public List<string> EvidenceLinks { get; }
}

internal sealed class TaskActivityEntry
{
    public TaskActivityEntry(
        string activityEntryId,
        string actorId,
        PrincipalKind actorKind,
        DateTimeOffset createdAt,
        TaskActivityKind kind,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(activityEntryId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        ActivityEntryId = activityEntryId;
        ActorId = actorId;
        ActorKind = actorKind;
        CreatedAt = createdAt;
        Kind = kind;
        Payload = payload;
    }

    public string ActivityEntryId { get; }

    public string ActorId { get; }

    public PrincipalKind ActorKind { get; }

    public DateTimeOffset CreatedAt { get; }

    public TaskActivityKind Kind { get; }

    public string Payload { get; }
}

internal sealed class ProjectTask
{
    public string TaskId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProjectTaskStatus Status { get; set; }

    public TaskReadiness Readiness { get; set; } = TaskReadiness.Ready;

    public List<TaskBlockingReason> BlockingReasons { get; } = [];

    public string Priority { get; set; } = string.Empty;

    public string Rank { get; set; } = string.Empty;

    public List<string> Labels { get; } = [];

    public string? Assignee { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public string? ParentTaskId { get; set; }

    public List<string> Dependencies { get; } = [];

    public List<AcceptanceCriterion> AcceptanceCriteria { get; } = [];

    public List<string> Subtasks { get; } = [];

    public List<TaskActivityEntry> Activity { get; } = [];

    public List<string> LinkedTransactions { get; } = [];

    public List<string> LinkedJobs { get; } = [];

    public List<string> LinkedDiagnostics { get; } = [];

    public List<string> LinkedArtifacts { get; } = [];

    public List<string> LinkedScenesResourcesAndNodes { get; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? SubmittedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? AcceptedAt { get; set; }

    public string? AcceptedBy { get; set; }

    public ProjectTaskAcceptanceState AcceptanceState { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public string? ArchivedBy { get; set; }

    public string? CancellationReason { get; set; }
}

internal sealed class TaskBoard
{
    public TaskBoard(string boardId, IEnumerable<TaskBoardColumn> columns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);
        ArgumentNullException.ThrowIfNull(columns);

        BoardId = boardId;
        Columns = columns.ToArray();
    }

    public string BoardId { get; }

    public IReadOnlyList<TaskBoardColumn> Columns { get; }
}

internal sealed class TaskBoardColumn
{
    public TaskBoardColumn(ProjectTaskStatus status, IEnumerable<string> taskIds)
    {
        ArgumentNullException.ThrowIfNull(taskIds);

        Status = status;
        TaskIds = taskIds.ToArray();
    }

    public ProjectTaskStatus Status { get; }

    public IReadOnlyList<string> TaskIds { get; }
}

internal static class ProjectTaskStorage
{
    public const string BoardDocumentPath = ".electron2d/tasks/board.e2tasks";

    public static string GetTaskDocumentPath(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        return $".electron2d/tasks/{taskId}.e2task";
    }
}

internal static class ProjectTaskSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true
    };

    public static string Serialize(ProjectTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        ValidateTask(task);

        var root = new JsonObject
        {
            ["format"] = "Electron2D.TaskFile",
            ["version"] = 1,
            ["taskId"] = task.TaskId,
            ["title"] = task.Title,
            ["description"] = task.Description,
            ["status"] = task.Status.ToString(),
            ["readiness"] = task.Readiness.ToString(),
            ["blockingReasons"] = WriteEnumArray(task.BlockingReasons),
            ["priority"] = task.Priority,
            ["rank"] = task.Rank,
            ["labels"] = WriteStringArray(task.Labels),
            ["assignee"] = task.Assignee,
            ["createdBy"] = task.CreatedBy,
            ["parentTaskId"] = task.ParentTaskId,
            ["dependencies"] = WriteStringArray(task.Dependencies),
            ["acceptanceCriteria"] = WriteAcceptanceCriteria(task.AcceptanceCriteria),
            ["subtasks"] = WriteStringArray(task.Subtasks),
            ["activity"] = WriteActivity(task.Activity),
            ["linkedTransactions"] = WriteStringArray(task.LinkedTransactions),
            ["linkedJobs"] = WriteStringArray(task.LinkedJobs),
            ["linkedDiagnostics"] = WriteStringArray(task.LinkedDiagnostics),
            ["linkedArtifacts"] = WriteStringArray(task.LinkedArtifacts),
            ["linkedScenesResourcesAndNodes"] = WriteStringArray(task.LinkedScenesResourcesAndNodes),
            ["createdAt"] = WriteDate(task.CreatedAt),
            ["updatedAt"] = WriteDate(task.UpdatedAt),
            ["submittedAt"] = WriteNullableDate(task.SubmittedAt),
            ["completedAt"] = WriteNullableDate(task.CompletedAt),
            ["acceptedAt"] = WriteNullableDate(task.AcceptedAt),
            ["acceptedBy"] = task.AcceptedBy,
            ["acceptanceState"] = task.AcceptanceState.ToString(),
            ["archivedAt"] = WriteNullableDate(task.ArchivedAt),
            ["archivedBy"] = task.ArchivedBy,
            ["cancellationReason"] = task.CancellationReason
        };

        return root.ToJsonString(IndentedOptions).ReplaceLineEndings("\n") + "\n";
    }

    public static ProjectTask DeserializeTask(string path, string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);

        var root = ReadRoot(text, "Task document");
        var format = ReadString(root, "format", "Task document format");
        if (format != "Electron2D.TaskFile")
        {
            throw new FormatException($"Task document format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Task document version");
        if (version != 1)
        {
            throw new FormatException($"Task document version '{version}' is not supported.");
        }

        var task = new ProjectTask
        {
            TaskId = ReadString(root, "taskId", "Task id"),
            Title = ReadString(root, "title", "Task title"),
            Description = ReadString(root, "description", "Task description"),
            Status = ReadEnum<ProjectTaskStatus>(root, "status", "Task status"),
            Readiness = ReadEnum<TaskReadiness>(root, "readiness", "Task readiness"),
            Priority = ReadString(root, "priority", "Task priority"),
            Rank = ReadString(root, "rank", "Task rank"),
            Assignee = ReadOptionalString(root, "assignee"),
            CreatedBy = ReadString(root, "createdBy", "Task creator"),
            ParentTaskId = ReadOptionalString(root, "parentTaskId"),
            CreatedAt = ReadDate(root, "createdAt", "Task created time"),
            UpdatedAt = ReadDate(root, "updatedAt", "Task updated time"),
            SubmittedAt = ReadOptionalDate(root, "submittedAt", "Task submitted time"),
            CompletedAt = ReadOptionalDate(root, "completedAt", "Task completed time"),
            AcceptedAt = ReadOptionalDate(root, "acceptedAt", "Task accepted time"),
            AcceptedBy = ReadOptionalString(root, "acceptedBy"),
            AcceptanceState = ReadEnum<ProjectTaskAcceptanceState>(root, "acceptanceState", "Task acceptance state"),
            ArchivedAt = ReadOptionalDate(root, "archivedAt", "Task archived time"),
            ArchivedBy = ReadOptionalString(root, "archivedBy"),
            CancellationReason = ReadOptionalString(root, "cancellationReason")
        };
        task.BlockingReasons.AddRange(ReadEnumArray<TaskBlockingReason>(root, "blockingReasons", "Task blocking reasons"));
        task.Labels.AddRange(ReadStringArray(root, "labels", "Task labels"));
        task.Dependencies.AddRange(ReadStringArray(root, "dependencies", "Task dependencies"));
        task.AcceptanceCriteria.AddRange(ReadAcceptanceCriteria(root));
        task.Subtasks.AddRange(ReadStringArray(root, "subtasks", "Task subtasks"));
        task.Activity.AddRange(ReadActivity(root));
        task.LinkedTransactions.AddRange(ReadStringArray(root, "linkedTransactions", "Task linked transactions"));
        task.LinkedJobs.AddRange(ReadStringArray(root, "linkedJobs", "Task linked jobs"));
        task.LinkedDiagnostics.AddRange(ReadStringArray(root, "linkedDiagnostics", "Task linked diagnostics"));
        task.LinkedArtifacts.AddRange(ReadStringArray(root, "linkedArtifacts", "Task linked artifacts"));
        task.LinkedScenesResourcesAndNodes.AddRange(ReadStringArray(
            root,
            "linkedScenesResourcesAndNodes",
            "Task linked scenes, resources and nodes"));

        var expectedPath = ProjectTaskStorage.GetTaskDocumentPath(task.TaskId);
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        if (!string.Equals(expectedPath, normalizedPath, StringComparison.Ordinal))
        {
            throw new FormatException($"Task document path '{normalizedPath}' does not match task id '{task.TaskId}'.");
        }

        return task;
    }

    public static string SerializeBoard(TaskBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var columns = new JsonArray();
        foreach (var column in board.Columns)
        {
            columns.Add((JsonNode)new JsonObject
            {
                ["status"] = column.Status.ToString(),
                ["taskIds"] = WriteStringArray(column.TaskIds)
            });
        }

        var root = new JsonObject
        {
            ["format"] = "Electron2D.TaskBoard",
            ["version"] = 1,
            ["boardId"] = board.BoardId,
            ["columns"] = columns
        };

        return root.ToJsonString(IndentedOptions).ReplaceLineEndings("\n") + "\n";
    }

    public static TaskBoard DeserializeBoard(string path, string text)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        if (!string.Equals(normalizedPath, ProjectTaskStorage.BoardDocumentPath, StringComparison.Ordinal))
        {
            throw new FormatException($"Task board path '{normalizedPath}' is not supported.");
        }

        var root = ReadRoot(text, "Task board document");
        var format = ReadString(root, "format", "Task board format");
        if (format != "Electron2D.TaskBoard")
        {
            throw new FormatException($"Task board format '{format}' is not supported.");
        }

        var version = ReadInt32(root, "version", "Task board version");
        if (version != 1)
        {
            throw new FormatException($"Task board version '{version}' is not supported.");
        }

        var columns = new List<TaskBoardColumn>();
        foreach (var node in ReadArray(root, "columns", "Task board columns"))
        {
            var column = ExpectObject(node, "Task board column");
            columns.Add(new TaskBoardColumn(
                ReadEnum<ProjectTaskStatus>(column, "status", "Task board column status"),
                ReadStringArray(column, "taskIds", "Task board column task ids")));
        }

        return new TaskBoard(ReadString(root, "boardId", "Task board id"), columns);
    }

    private static void ValidateTask(ProjectTask task)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(task.TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.CreatedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.Priority);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.Rank);
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

    private static JsonArray WriteEnumArray<T>(IEnumerable<T> values)
        where T : struct, Enum
    {
        var array = new JsonArray();
        foreach (var value in values.Select(value => value.ToString()).OrderBy(value => value, StringComparer.Ordinal))
        {
            array.Add(value);
        }

        return array;
    }

    private static JsonArray WriteAcceptanceCriteria(IEnumerable<AcceptanceCriterion> criteria)
    {
        var array = new JsonArray();
        foreach (var criterion in criteria.OrderBy(criterion => criterion.CriterionId, StringComparer.Ordinal))
        {
            array.Add((JsonNode)new JsonObject
            {
                ["criterionId"] = criterion.CriterionId,
                ["description"] = criterion.Description,
                ["state"] = criterion.State.ToString(),
                ["evidenceLinks"] = WriteStringArray(criterion.EvidenceLinks)
            });
        }

        return array;
    }

    private static JsonArray WriteActivity(IEnumerable<TaskActivityEntry> activity)
    {
        var array = new JsonArray();
        foreach (var entry in activity.OrderBy(entry => entry.CreatedAt).ThenBy(entry => entry.ActivityEntryId, StringComparer.Ordinal))
        {
            array.Add((JsonNode)new JsonObject
            {
                ["activityEntryId"] = entry.ActivityEntryId,
                ["actorId"] = entry.ActorId,
                ["actorKind"] = entry.ActorKind.ToString(),
                ["createdAt"] = WriteDate(entry.CreatedAt),
                ["kind"] = entry.Kind.ToString(),
                ["payload"] = entry.Payload
            });
        }

        return array;
    }

    private static string WriteDate(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static JsonNode? WriteNullableDate(DateTimeOffset? value)
    {
        return value is null ? null : JsonValue.Create(WriteDate(value.Value));
    }

    private static JsonObject ReadRoot(string text, string description)
    {
        try
        {
            return JsonNode.Parse(text) as JsonObject ??
                throw new FormatException($"{description} root must be a JSON object.");
        }
        catch (JsonException exception)
        {
            throw new FormatException($"{description} JSON is malformed.", exception);
        }
    }

    private static JsonObject ExpectObject(JsonNode? node, string description)
    {
        return node as JsonObject ?? throw new FormatException($"{description} must be a JSON object.");
    }

    private static JsonArray ReadArray(JsonObject root, string name, string description)
    {
        return root.TryGetPropertyValue(name, out var node) && node is JsonArray array
            ? array
            : throw new FormatException($"{description} must be a JSON array.");
    }

    private static string ReadString(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a non-empty JSON string.");
    }

    private static string? ReadOptionalString(JsonObject root, string name)
    {
        if (!root.TryGetPropertyValue(name, out var node) || node is null || node.GetValueKind() == JsonValueKind.Null)
        {
            return null;
        }

        return node is JsonValue jsonValue && jsonValue.TryGetValue<string>(out var value) ? value : null;
    }

    private static int ReadInt32(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<int>(out var value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a JSON integer.");
    }

    private static T ReadEnum<T>(JsonObject root, string name, string description)
        where T : struct, Enum
    {
        var text = ReadString(root, name, description);
        return Enum.TryParse<T>(text, ignoreCase: false, out var value)
            ? value
            : throw new FormatException($"{description} value '{text}' is not supported.");
    }

    private static DateTimeOffset ReadDate(JsonObject root, string name, string description)
    {
        var value = ReadString(root, name, description);
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : throw new FormatException($"{description} must be an ISO 8601 timestamp.");
    }

    private static DateTimeOffset? ReadOptionalDate(JsonObject root, string name, string description)
    {
        var value = ReadOptionalString(root, name);
        if (value is null)
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed)
            ? parsed
            : throw new FormatException($"{description} must be an ISO 8601 timestamp.");
    }

    private static IReadOnlyList<string> ReadStringArray(JsonObject root, string name, string description)
    {
        return ReadArray(root, name, description)
            .Select(node => node is JsonValue value && value.TryGetValue<string>(out var text)
                ? text
                : throw new FormatException($"{description} must contain only strings."))
            .ToArray();
    }

    private static IReadOnlyList<T> ReadEnumArray<T>(JsonObject root, string name, string description)
        where T : struct, Enum
    {
        return ReadArray(root, name, description)
            .Select(node =>
            {
                if (node is not JsonValue value || !value.TryGetValue<string>(out var text))
                {
                    throw new FormatException($"{description} must contain only strings.");
                }

                return Enum.TryParse<T>(text, ignoreCase: false, out var parsed)
                    ? parsed
                    : throw new FormatException($"{description} value '{text}' is not supported.");
            })
            .ToArray();
    }

    private static IReadOnlyList<AcceptanceCriterion> ReadAcceptanceCriteria(JsonObject root)
    {
        return ReadArray(root, "acceptanceCriteria", "Task acceptance criteria")
            .Select(node =>
            {
                var criterion = ExpectObject(node, "Task acceptance criterion");
                return new AcceptanceCriterion(
                    ReadString(criterion, "criterionId", "Acceptance criterion id"),
                    ReadString(criterion, "description", "Acceptance criterion description"),
                    ReadEnum<AcceptanceCriterionState>(criterion, "state", "Acceptance criterion state"),
                    ReadStringArray(criterion, "evidenceLinks", "Acceptance criterion evidence links"));
            })
            .ToArray();
    }

    private static IReadOnlyList<TaskActivityEntry> ReadActivity(JsonObject root)
    {
        return ReadArray(root, "activity", "Task activity")
            .Select(node =>
            {
                var activity = ExpectObject(node, "Task activity entry");
                return new TaskActivityEntry(
                    ReadString(activity, "activityEntryId", "Activity entry id"),
                    ReadString(activity, "actorId", "Activity actor id"),
                    ReadEnum<PrincipalKind>(activity, "actorKind", "Activity actor kind"),
                    ReadDate(activity, "createdAt", "Activity created time"),
                    ReadEnum<TaskActivityKind>(activity, "kind", "Activity kind"),
                    ReadString(activity, "payload", "Activity payload"));
            })
            .ToArray();
    }
}

internal sealed class ProjectTaskStatusChangeRequest
{
    public ProjectTaskStatusChangeRequest(
        string taskId,
        ProjectTaskStatus targetStatus,
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
        TargetStatus = targetStatus;
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
    }

    public string TaskId { get; }

    public ProjectTaskStatus TargetStatus { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }
}

internal sealed class ProjectTaskAcceptanceRequest
{
    public ProjectTaskAcceptanceRequest(
        string taskId,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        TaskId = taskId;
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
        Reason = reason;
    }

    public string TaskId { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }

    public string Reason { get; }
}

internal sealed class ProjectTaskReopenRequest
{
    public ProjectTaskReopenRequest(
        string taskId,
        ProjectTaskStatus targetStatus,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context,
        string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        TaskId = taskId;
        TargetStatus = targetStatus;
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
        Reason = reason;
    }

    public string TaskId { get; }

    public ProjectTaskStatus TargetStatus { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }

    public string Reason { get; }
}

internal sealed class ProjectTaskActivityRequest
{
    public ProjectTaskActivityRequest(
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

internal sealed class ProjectTaskExternalImportRequest
{
    public ProjectTaskExternalImportRequest(
        string path,
        string text,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        ArgumentNullException.ThrowIfNull(context);

        Path = path;
        Text = text.ReplaceLineEndings("\n");
        ExpectedRevision = expectedRevision;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        Context = context;
    }

    public string Path { get; }

    public string Text { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string OperationId { get; }

    public string UndoGroupId { get; }

    public OperationContext Context { get; }
}

internal sealed class ProjectTaskMutationResult
{
    private ProjectTaskMutationResult(
        bool succeeded,
        ProjectTask task,
        WorkspaceTransactionResult transactionResult,
        IEnumerable<StructuredDiagnostic> diagnostics)
    {
        Succeeded = succeeded;
        Task = task;
        TransactionResult = transactionResult;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public ProjectTask Task { get; }

    public WorkspaceTransactionResult TransactionResult { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public static ProjectTaskMutationResult FromTransaction(ProjectTask task, WorkspaceTransactionResult result)
    {
        return new ProjectTaskMutationResult(result.Succeeded, task, result, result.Diagnostics);
    }

    public static ProjectTaskMutationResult Rejected(
        ProjectTask task,
        string operationId,
        StructuredDiagnostic diagnostic,
        ProjectWorkspace workspace)
    {
        return new ProjectTaskMutationResult(
            succeeded: false,
            task,
            EmptyTransactionResult(operationId, workspace, [diagnostic]),
            [diagnostic]);
    }

    private static WorkspaceTransactionResult EmptyTransactionResult(
        string operationId,
        ProjectWorkspace workspace,
        IReadOnlyList<StructuredDiagnostic> diagnostics)
    {
        return new WorkspaceTransactionResult(
            succeeded: false,
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            operationId,
            undoGroupId: null,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            workspace.Documents.Documents.ToDictionary(document => document.Path, document => document.PersistedRevision, StringComparer.Ordinal),
            workspace.Revisions.DirtyDocuments,
            workspace.Revisions.PersistenceState,
            changedFiles: [],
            changedObjects: [],
            createdObjects: [],
            conflicts: [],
            diagnostics,
            backupFiles: []);
    }
}

internal sealed class TaskDependencyGraphResult
{
    public TaskDependencyGraphResult(bool succeeded, ProjectTask task, IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Succeeded = succeeded;
        Task = task;
        Diagnostics = diagnostics.ToArray();
    }

    public bool Succeeded { get; }

    public ProjectTask Task { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal static class TaskDependencyGraph
{
    public static TaskDependencyGraphResult ValidateAddingDependency(
        IEnumerable<ProjectTask> tasks,
        string taskId,
        string dependencyTaskId)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyTaskId);

        var taskById = tasks.ToDictionary(task => task.TaskId, StringComparer.Ordinal);
        if (!taskById.TryGetValue(taskId, out var task))
        {
            throw new ArgumentException($"Task '{taskId}' was not found.", nameof(taskId));
        }

        if (WouldCreateCycle(taskById, taskId, dependencyTaskId))
        {
            return new TaskDependencyGraphResult(
                succeeded: false,
                task,
                [TaskDiagnostic("E2D-TASK-0003", $"Adding dependency '{dependencyTaskId}' to task '{taskId}' creates a cycle.")]);
        }

        return new TaskDependencyGraphResult(succeeded: true, task, []);
    }

    public static TaskDependencyGraphResult RefreshReadiness(ProjectTask task, IEnumerable<ProjectTask> dependencies)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(dependencies);

        var refreshed = CloneTask(task);
        var dependencyById = dependencies.ToDictionary(dependency => dependency.TaskId, StringComparer.Ordinal);
        var diagnostics = new List<StructuredDiagnostic>();
        var hasOpenDependency = false;
        var hasCancelledDependency = false;
        foreach (var dependencyId in refreshed.Dependencies)
        {
            if (!dependencyById.TryGetValue(dependencyId, out var dependency) ||
                dependency.Status is not ProjectTaskStatus.Done)
            {
                hasOpenDependency = true;
            }

            if (dependencyById.TryGetValue(dependencyId, out dependency) &&
                dependency.Status == ProjectTaskStatus.Cancelled)
            {
                hasCancelledDependency = true;
                diagnostics.Add(TaskDiagnostic(
                    "E2D-TASK-0003",
                    $"Dependency '{dependencyId}' was cancelled; dependent task '{refreshed.TaskId}' needs a human decision."));
            }
        }

        refreshed.BlockingReasons.RemoveAll(reason => reason == TaskBlockingReason.Dependency);
        if (hasCancelledDependency)
        {
            refreshed.Readiness = TaskReadiness.DependencyCancelled;
            AddReason(refreshed, TaskBlockingReason.Dependency);
        }
        else if (hasOpenDependency)
        {
            refreshed.Readiness = TaskReadiness.BlockedByDependencies;
            AddReason(refreshed, TaskBlockingReason.Dependency);
            diagnostics.Add(TaskDiagnostic(
                "E2D-TASK-0003",
                $"Task '{refreshed.TaskId}' is blocked by unfinished dependencies."));
        }
        else
        {
            refreshed.Readiness = TaskReadiness.Ready;
        }

        return new TaskDependencyGraphResult(diagnostics.Count == 0, refreshed, diagnostics);
    }

    internal static ProjectTask CloneTask(ProjectTask task)
    {
        var clone = ProjectTaskSerializer.DeserializeTask(
            ProjectTaskStorage.GetTaskDocumentPath(task.TaskId),
            ProjectTaskSerializer.Serialize(task));
        return clone;
    }

    private static bool WouldCreateCycle(
        IReadOnlyDictionary<string, ProjectTask> taskById,
        string taskId,
        string dependencyTaskId)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        return Reaches(dependencyTaskId, taskId, visited);

        bool Reaches(string currentId, string targetId, HashSet<string> path)
        {
            if (string.Equals(currentId, targetId, StringComparison.Ordinal))
            {
                return true;
            }

            if (!path.Add(currentId) || !taskById.TryGetValue(currentId, out var current))
            {
                return false;
            }

            return current.Dependencies.Any(dependencyId => Reaches(dependencyId, targetId, path));
        }
    }

    private static void AddReason(ProjectTask task, TaskBlockingReason reason)
    {
        if (!task.BlockingReasons.Contains(reason))
        {
            task.BlockingReasons.Add(reason);
        }
    }

    private static StructuredDiagnostic TaskDiagnostic(string code, string message)
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

internal sealed class ProjectTaskManager
{
    private readonly ProjectWorkspace workspace;

    public ProjectTaskManager(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
    }

    public ProjectTask GetTask(string taskId)
    {
        var path = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        var document = workspace.Documents.GetByPath(path);
        return ProjectTaskSerializer.DeserializeTask(path, document.Text);
    }

    public ProjectTaskMutationResult ChangeStatus(ProjectTaskStatusChangeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = GetTask(request.TaskId);
        var validation = ValidateStatusChange(task, request.TargetStatus, request.Context);
        if (validation is not null)
        {
            return ProjectTaskMutationResult.Rejected(task, request.OperationId, validation, workspace);
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        ApplyStatusUpdate(updated, request.TargetStatus, request.Context, payload: $"Status changed to {request.TargetStatus}.");
        return ApplyTask(
            updated,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context,
            "task.change-status");
    }

    public ProjectTaskMutationResult RequestChanges(ProjectTaskAcceptanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = GetTask(request.TaskId);
        if (task.Status != ProjectTaskStatus.AwaitingAcceptance ||
            !request.Context.HasCapability(OperationCapability.TaskRequestChanges) ||
            request.Context.PrincipalKind != PrincipalKind.Human)
        {
            return ProjectTaskMutationResult.Rejected(
                task,
                request.OperationId,
                TaskDiagnostic("E2D-TASK-0002", "Request changes requires awaiting acceptance and a trusted human context."),
                workspace);
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        updated.Status = ProjectTaskStatus.InProgress;
        updated.UpdatedAt = DateTimeOffset.UtcNow;
        updated.AcceptanceState = ProjectTaskAcceptanceState.ChangesRequested;
        AppendActivity(updated, request.OperationId, request.Context, TaskActivityKind.AcceptanceResult, request.Reason);
        return ApplyTask(
            updated,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context,
            "task.request-changes");
    }

    public ProjectTaskMutationResult Reopen(ProjectTaskReopenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = GetTask(request.TaskId);
        if ((task.Status != ProjectTaskStatus.Done && task.Status != ProjectTaskStatus.Cancelled) ||
            !request.Context.HasCapability(OperationCapability.TaskReopen) ||
            request.Context.PrincipalKind != PrincipalKind.Human)
        {
            return ProjectTaskMutationResult.Rejected(
                task,
                request.OperationId,
                TaskDiagnostic("E2D-TASK-0002", "Reopen requires a closed task and a trusted human context."),
                workspace);
        }

        if (request.TargetStatus is not (ProjectTaskStatus.Backlog or ProjectTaskStatus.Ready or ProjectTaskStatus.InProgress))
        {
            return ProjectTaskMutationResult.Rejected(
                task,
                request.OperationId,
                TaskDiagnostic("E2D-TASK-0002", "Reopen target status must be Backlog, Ready or InProgress."),
                workspace);
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        updated.Status = request.TargetStatus;
        updated.UpdatedAt = DateTimeOffset.UtcNow;
        updated.AcceptanceState = ProjectTaskAcceptanceState.Reopened;
        AppendActivity(updated, request.OperationId, request.Context, TaskActivityKind.StatusChange, request.Reason);
        return ApplyTask(
            updated,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context,
            "task.reopen");
    }

    public ProjectTaskMutationResult AddActivity(ProjectTaskActivityRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var task = GetTask(request.TaskId);
        if (!request.Context.HasCapability(OperationCapability.TaskWrite))
        {
            return ProjectTaskMutationResult.Rejected(
                task,
                request.OperationId,
                TaskDiagnostic("E2D-TASK-0002", "Adding task activity requires task write capability."),
                workspace);
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        updated.UpdatedAt = DateTimeOffset.UtcNow;
        AppendActivity(updated, request.OperationId, request.Context, request.Kind, SanitizeActivityPayload(request.Payload));
        return ApplyTask(
            updated,
            request.ExpectedRevision,
            request.OperationId,
            request.UndoGroupId,
            request.Context,
            "task.add-activity");
    }

    public ProjectTaskMutationResult ImportExternalChange(ProjectTaskExternalImportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(request.Path);
        var current = ProjectTaskSerializer.DeserializeTask(normalizedPath, workspace.Documents.GetByPath(normalizedPath).Text);
        var incoming = ProjectTaskSerializer.DeserializeTask(normalizedPath, request.Text);
        var diagnostic = ValidateExternalImport(current, incoming);
        if (diagnostic is not null)
        {
            workspace.ImportState.SetState(normalizedPath, "pending-conflict");
            return ProjectTaskMutationResult.Rejected(current, request.OperationId, diagnostic, workspace);
        }

        var transaction = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            request.OperationId,
            ToWorkspaceActorKind(request.Context.PrincipalKind),
            "task.external-import",
            WorkspaceTransactionMode.ExternalImport,
            dryRun: false,
            request.UndoGroupId,
            [WorkspaceTransactionDocumentEdit.ReplaceText(normalizedPath, request.ExpectedRevision, request.Text)]));
        return ProjectTaskMutationResult.FromTransaction(
            transaction.Succeeded ? ProjectTaskSerializer.DeserializeTask(normalizedPath, workspace.Documents.GetByPath(normalizedPath).Text) : current,
            transaction);
    }

    private ProjectTaskMutationResult ApplyTask(
        ProjectTask task,
        ProjectDocumentRevision expectedRevision,
        string operationId,
        string undoGroupId,
        OperationContext context,
        string operationKind)
    {
        var path = ProjectTaskStorage.GetTaskDocumentPath(task.TaskId);
        var text = ProjectTaskSerializer.Serialize(task);
        var transaction = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            operationId,
            ToWorkspaceActorKind(context.PrincipalKind),
            operationKind,
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId,
            [WorkspaceTransactionDocumentEdit.ReplaceText(path, expectedRevision, text)]));

        var resultTask = transaction.Succeeded
            ? ProjectTaskSerializer.DeserializeTask(path, workspace.Documents.GetByPath(path).Text)
            : task;
        return ProjectTaskMutationResult.FromTransaction(resultTask, transaction);
    }

    private static StructuredDiagnostic? ValidateStatusChange(
        ProjectTask task,
        ProjectTaskStatus targetStatus,
        OperationContext context)
    {
        if (!IsTransitionAllowed(task.Status, targetStatus))
        {
            return TaskDiagnostic("E2D-TASK-0002", $"Task status transition '{task.Status}' -> '{targetStatus}' is not allowed.");
        }

        if (targetStatus == ProjectTaskStatus.AwaitingAcceptance &&
            !context.HasCapability(OperationCapability.TaskSubmitForAcceptance))
        {
            return TaskDiagnostic("E2D-TASK-0002", "Submitting a task for acceptance requires TaskSubmitForAcceptance capability.");
        }

        if (targetStatus == ProjectTaskStatus.Done &&
            (!context.HasCapability(OperationCapability.TaskAccept) || context.PrincipalKind != PrincipalKind.Human))
        {
            return TaskDiagnostic("E2D-TASK-0002", "Accepting a task requires trusted human TaskAccept capability.");
        }

        if (task.Status == ProjectTaskStatus.AwaitingAcceptance &&
            targetStatus == ProjectTaskStatus.InProgress &&
            (!context.HasCapability(OperationCapability.TaskRequestChanges) || context.PrincipalKind != PrincipalKind.Human))
        {
            return TaskDiagnostic("E2D-TASK-0002", "Request changes requires trusted human TaskRequestChanges capability.");
        }

        return null;
    }

    private static void ApplyStatusUpdate(
        ProjectTask task,
        ProjectTaskStatus targetStatus,
        OperationContext context,
        string payload)
    {
        task.Status = targetStatus;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        if (targetStatus == ProjectTaskStatus.AwaitingAcceptance)
        {
            task.SubmittedAt = task.UpdatedAt;
            task.AcceptanceState = ProjectTaskAcceptanceState.Submitted;
        }
        else if (targetStatus == ProjectTaskStatus.Done)
        {
            task.CompletedAt = task.UpdatedAt;
            task.AcceptedAt = task.UpdatedAt;
            task.AcceptedBy = context.PrincipalId;
            task.AcceptanceState = ProjectTaskAcceptanceState.Accepted;
        }
        else if (targetStatus == ProjectTaskStatus.InProgress && task.AcceptanceState == ProjectTaskAcceptanceState.Submitted)
        {
            task.AcceptanceState = ProjectTaskAcceptanceState.ChangesRequested;
        }

        AppendActivity(
            task,
            $"status-{targetStatus.ToString().ToLowerInvariant()}-{task.Activity.Count + 1}",
            context,
            targetStatus == ProjectTaskStatus.Done ? TaskActivityKind.AcceptanceResult : TaskActivityKind.StatusChange,
            payload);
    }

    private static bool IsTransitionAllowed(ProjectTaskStatus current, ProjectTaskStatus target)
    {
        return current switch
        {
            ProjectTaskStatus.Backlog => target is ProjectTaskStatus.Ready or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Ready => target is ProjectTaskStatus.Backlog or ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.InProgress => target is ProjectTaskStatus.Ready or ProjectTaskStatus.Blocked or ProjectTaskStatus.Review or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Blocked => target is ProjectTaskStatus.Ready or ProjectTaskStatus.InProgress or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Review => target is ProjectTaskStatus.InProgress or ProjectTaskStatus.AwaitingAcceptance or ProjectTaskStatus.Blocked,
            ProjectTaskStatus.AwaitingAcceptance => target is ProjectTaskStatus.InProgress or ProjectTaskStatus.Done,
            _ => false
        };
    }

    private static StructuredDiagnostic? ValidateExternalImport(ProjectTask current, ProjectTask incoming)
    {
        if (incoming.Status == ProjectTaskStatus.Done && current.Status != ProjectTaskStatus.Done)
        {
            return TaskDiagnostic("E2D-TASK-0002", "External task import cannot accept a task or set Done.");
        }

        if (!string.Equals(current.CreatedBy, incoming.CreatedBy, StringComparison.Ordinal) ||
            current.CreatedAt != incoming.CreatedAt ||
            current.UpdatedAt != incoming.UpdatedAt ||
            current.SubmittedAt != incoming.SubmittedAt ||
            current.CompletedAt != incoming.CompletedAt ||
            current.AcceptedAt != incoming.AcceptedAt ||
            !string.Equals(current.AcceptedBy, incoming.AcceptedBy, StringComparison.Ordinal) ||
            current.AcceptanceState != incoming.AcceptanceState ||
            current.ArchivedAt != incoming.ArchivedAt ||
            !string.Equals(current.ArchivedBy, incoming.ArchivedBy, StringComparison.Ordinal))
        {
            return TaskDiagnostic("E2D-TASK-0002", "External task import attempted to change privileged task audit fields.");
        }

        if (incoming.Activity.Any(entry =>
            current.Activity.All(currentEntry => currentEntry.ActivityEntryId != entry.ActivityEntryId) &&
            (entry.ActorKind is PrincipalKind.Human or PrincipalKind.Agent ||
                !string.IsNullOrWhiteSpace(entry.ActorId))))
        {
            return TaskDiagnostic("E2D-TASK-0002", "External task import attempted to add privileged activity audit fields.");
        }

        return null;
    }

    private static void AppendActivity(
        ProjectTask task,
        string operationId,
        OperationContext context,
        TaskActivityKind kind,
        string payload)
    {
        var activityId = operationId.StartsWith("op-", StringComparison.Ordinal)
            ? $"activity-{operationId[3..]}"
            : $"activity-{operationId}";
        task.Activity.Add(new TaskActivityEntry(
            activityId,
            context.PrincipalId,
            context.PrincipalKind,
            DateTimeOffset.UtcNow,
            kind,
            payload));
    }

    private static string SanitizeActivityPayload(string payload)
    {
        var result = new List<string>();
        foreach (var part in payload.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = part.IndexOf('=', StringComparison.Ordinal);
            if (separator < 0)
            {
                result.Add(part);
                continue;
            }

            var key = part[..separator].Trim();
            var value = part[(separator + 1)..].Trim();
            if (key.Equals("ActorId", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("ActorKind", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            result.Add(key.Equals("message", StringComparison.OrdinalIgnoreCase) ? value : $"{key}={value}");
        }

        return string.Join("; ", result);
    }

    private static ProjectWorkspaceActorKind ToWorkspaceActorKind(PrincipalKind principalKind)
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

    private static StructuredDiagnostic TaskDiagnostic(string code, string message)
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
