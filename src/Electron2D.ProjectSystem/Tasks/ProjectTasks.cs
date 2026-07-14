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
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum ProjectTaskStatus
{
    Ready,
    InProgress,
    Blocked,
    Review,
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

internal enum TaskBoardGroupKind
{
    Epoch,
    Milestone
}

internal sealed class TaskExecutionContract
{
    public string TaskType { get; set; } = "general";

    public List<string> ReadyToStart { get; } = [];

    public List<string> StopConditions { get; } = [];

    public List<string> AllowedChanges { get; } = [];

    public List<string> ForbiddenChanges { get; } = [];

    public List<string> RequiredOutputs { get; } = [];

    public List<string> RequiredCommands { get; } = [];

    public string ExternalAudit { get; set; } = "not-required";
}

internal sealed class TaskAttachment
{
    public string AttachmentId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string MediaType { get; set; } = "application/octet-stream";

    public long ByteLength { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public DateTimeOffset AddedAt { get; set; }

    public string AddedBy { get; set; } = string.Empty;
}

internal static class TaskAttachmentPreview
{
    public static bool IsRasterMediaType(string mediaType)
    {
        return mediaType.ToLowerInvariant() is
            "image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp";
    }

    public static TaskAttachment? Resolve(ProjectTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.PreviewAttachmentId is not null)
        {
            var selected = task.Attachments.SingleOrDefault(attachment =>
                string.Equals(attachment.AttachmentId, task.PreviewAttachmentId, StringComparison.Ordinal));
            if (selected is null)
            {
                throw new InvalidOperationException(
                    $"Preview attachment '{task.PreviewAttachmentId}' was not found on task '{task.TaskId}'.");
            }

            if (!IsRasterMediaType(selected.MediaType))
            {
                throw new InvalidOperationException(
                    $"Attachment '{selected.AttachmentId}' is not a supported raster preview.");
            }

            return selected;
        }

        return task.Attachments
            .Where(attachment => IsRasterMediaType(attachment.MediaType))
            .OrderBy(attachment => attachment.AttachmentId, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}

internal sealed class LegacySourceFragment
{
    public string SourcePath { get; set; } = string.Empty;

    public long ByteOffset { get; set; }

    public long ByteLength { get; set; }

    public string Encoding { get; set; } = "utf-8";

    public bool HasBom { get; set; }

    public string LineEnding { get; set; } = "lf";

    public string Sha256 { get; set; } = string.Empty;

    public string Markdown { get; set; } = string.Empty;
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
    public string TaskUid { get; set; } = string.Empty;

    public string TaskId { get; set; } = string.Empty;

    public List<string> LegacyAliases { get; } = [];

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public ProjectTaskStatus Status { get; set; }

    public TaskReadiness Readiness { get; set; } = TaskReadiness.Ready;

    public List<TaskBlockingReason> BlockingReasons { get; } = [];

    public string Priority { get; set; } = string.Empty;

    public string Rank { get; set; } = string.Empty;

    public List<string> Labels { get; } = [];

    public DateOnly? Deadline { get; set; }

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

    public TaskExecutionContract ExecutionContract { get; } = new();

    public List<TaskAttachment> Attachments { get; } = [];

    public string? PreviewAttachmentId { get; set; }

    public List<LegacySourceFragment> LegacySourceFragments { get; } = [];

    public long Revision { get; set; } = 1;

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
        Revision = 1;
        Groups = [];
        Placements = Columns
            .SelectMany(column => column.TaskIds)
            .Select((taskId, index) => new TaskBoardPlacement(taskId, groupId: null, ((index + 1) * 1000).ToString("D8", CultureInfo.InvariantCulture)))
            .ToArray();
    }

    public TaskBoard(
        string boardId,
        long revision,
        IEnumerable<TaskBoardGroup> groups,
        IEnumerable<TaskBoardPlacement> placements)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(boardId);
        ArgumentOutOfRangeException.ThrowIfLessThan(revision, 1);
        ArgumentNullException.ThrowIfNull(groups);
        ArgumentNullException.ThrowIfNull(placements);

        BoardId = boardId;
        Revision = revision;
        Groups = groups.ToArray();
        Placements = placements.ToArray();
        Columns = [];
    }

    public string BoardId { get; }

    public long Revision { get; }

    public IReadOnlyList<TaskBoardColumn> Columns { get; }

    public IReadOnlyList<TaskBoardGroup> Groups { get; }

    public IReadOnlyList<TaskBoardPlacement> Placements { get; }

    public List<TaskBoardTag> Tags { get; } = [];

    public TaskBoardIdPolicy IdPolicy { get; } = new();

    public TaskBoardAttachmentPolicy AttachmentPolicy { get; } = new();

    public TaskBoardMigrationMetadata Migration { get; } = new();
}

internal enum TaskBoardTagColor
{
    Gray,
    Blue,
    Green,
    Yellow,
    Orange,
    Red,
    Purple
}

internal sealed class TaskBoardTag
{
    public TaskBoardTag(string tagId, string name, TaskBoardTagColor color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        TagId = tagId;
        Name = name;
        Color = color;
    }

    public string TagId { get; }

    public string Name { get; }

    public TaskBoardTagColor Color { get; }
}

internal sealed class TaskBoardIdPolicy
{
    public string Prefix { get; set; } = "T-";

    public int Padding { get; set; } = 4;

    public long NextNumber { get; set; } = 1;
}

internal sealed class TaskBoardAttachmentPolicy
{
    public long PerFileByteLimit { get; set; } = 25L * 1024 * 1024;

    public long BoardByteLimit { get; set; } = 250L * 1024 * 1024;
}

internal sealed class TaskBoardMigrationMetadata
{
    public string? ReportSha256 { get; set; }

    public bool Finalized { get; set; }

    public SortedDictionary<string, string> SourceDigests { get; } = new(StringComparer.Ordinal);

    public List<string> Diagnostics { get; } = [];

    public List<LegacySourceFragment> LegacySourceFragments { get; } = [];
}

internal sealed class TaskBoardGroup
{
    public TaskBoardGroup(
        string groupId,
        TaskBoardGroupKind kind,
        string title,
        string description,
        string? parentGroupId,
        string rank)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(rank);

        GroupId = groupId;
        Kind = kind;
        Title = title;
        Description = description;
        ParentGroupId = parentGroupId;
        Rank = rank;
    }

    public string GroupId { get; }

    public TaskBoardGroupKind Kind { get; }

    public string Title { get; }

    public string Description { get; }

    public string? ParentGroupId { get; }

    public string Rank { get; }
}

internal sealed class TaskBoardPlacement
{
    public TaskBoardPlacement(string taskId, string? groupId, string rank)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rank);

        TaskId = taskId;
        GroupId = groupId;
        Rank = rank;
    }

    public string TaskId { get; }

    public string? GroupId { get; }

    public string Rank { get; }
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
    public const int CurrentVersion = 2;

    public const string RootDirectory = ".taskboard";

    public const string ActiveTasksDirectory = ".taskboard/tasks";

    public const string CompletedTasksDirectory = ".taskboard/completed";

    public const string AttachmentsDirectory = ".taskboard/attachments";

    public const string BoardDocumentPath = ".taskboard/board.e2tasks";

    public static string GetTaskDocumentPath(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        return $"{ActiveTasksDirectory}/{taskId}.e2task";
    }

    public static string GetCompletedTaskDocumentPath(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        return $"{CompletedTasksDirectory}/{taskId}.e2task";
    }
}

internal static class ProjectTaskSerializer
{
    private static readonly JsonSerializerOptions IndentedOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Serialize(ProjectTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        ValidateTask(task);

        var root = new JsonObject
        {
            ["format"] = "Electron2D.TaskFile",
            ["version"] = ProjectTaskStorage.CurrentVersion,
            ["taskUid"] = string.IsNullOrWhiteSpace(task.TaskUid) ? task.TaskId : task.TaskUid,
            ["revision"] = Math.Max(task.Revision, 1),
            ["taskId"] = task.TaskId,
            ["legacyAliases"] = WriteStringArray(task.LegacyAliases),
            ["title"] = task.Title,
            ["description"] = task.Description,
            ["status"] = task.Status.ToString(),
            ["manualBlockingReasons"] = WriteEnumArray(task.BlockingReasons.Where(reason => reason != TaskBlockingReason.Dependency)),
            ["priority"] = task.Priority,
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
            ["executionContract"] = WriteExecutionContract(task.ExecutionContract),
            ["attachments"] = WriteAttachments(task.Attachments),
            ["legacySourceFragments"] = WriteLegacySourceFragments(task.LegacySourceFragments),
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

        if (task.Deadline is not null)
        {
            root["deadline"] = task.Deadline.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        if (task.PreviewAttachmentId is not null)
        {
            root["previewAttachmentId"] = task.PreviewAttachmentId;
        }

        return FormatJson(root);
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
        if (version != ProjectTaskStorage.CurrentVersion)
        {
            throw new FormatException($"Task document version '{version}' is not supported.");
        }

        var legacyAwaitingAcceptance = string.Equals(
            ReadString(root, "status", "Task status"),
            "AwaitingAcceptance",
            StringComparison.Ordinal);
        var task = new ProjectTask
        {
            TaskUid = ReadString(root, "taskUid", "Task UID"),
            Revision = ReadInt64(root, "revision", "Task revision"),
            TaskId = ReadString(root, "taskId", "Task id"),
            Title = ReadString(root, "title", "Task title"),
            Description = ReadRequiredText(root, "description", "Task description"),
            Status = ReadTaskStatus(root),
            Priority = ReadString(root, "priority", "Task priority"),
            Deadline = ReadOptionalDateOnly(root, "deadline", "Task deadline"),
            PreviewAttachmentId = ReadOptionalString(root, "previewAttachmentId"),
            Rank = "1000",
            Assignee = ReadOptionalString(root, "assignee"),
            CreatedBy = ReadString(root, "createdBy", "Task creator"),
            ParentTaskId = ReadOptionalString(root, "parentTaskId"),
            CreatedAt = ReadDate(root, "createdAt", "Task created time"),
            UpdatedAt = ReadDate(root, "updatedAt", "Task updated time"),
            SubmittedAt = ReadOptionalDate(root, "submittedAt", "Task submitted time"),
            CompletedAt = ReadOptionalDate(root, "completedAt", "Task completed time"),
            AcceptedAt = ReadOptionalDate(root, "acceptedAt", "Task accepted time"),
            AcceptedBy = ReadOptionalString(root, "acceptedBy"),
            AcceptanceState = legacyAwaitingAcceptance
                ? ProjectTaskAcceptanceState.Submitted
                : ReadEnum<ProjectTaskAcceptanceState>(root, "acceptanceState", "Task acceptance state"),
            ArchivedAt = ReadOptionalDate(root, "archivedAt", "Task archived time"),
            ArchivedBy = ReadOptionalString(root, "archivedBy"),
            CancellationReason = ReadOptionalString(root, "cancellationReason")
        };
        task.LegacyAliases.AddRange(ReadStringArray(root, "legacyAliases", "Task legacy aliases"));
        task.BlockingReasons.AddRange(ReadEnumArray<TaskBlockingReason>(root, "manualBlockingReasons", "Task manual blocking reasons"));
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
        CopyExecutionContract(ReadExecutionContract(root), task.ExecutionContract);
        task.Attachments.AddRange(ReadAttachments(root));
        task.LegacySourceFragments.AddRange(ReadLegacySourceFragments(root));
        ValidateTask(task);

        var expectedPath = ProjectTaskStorage.GetTaskDocumentPath(task.TaskId);
        var expectedCompletedPath = ProjectTaskStorage.GetCompletedTaskDocumentPath(task.TaskId);
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        if (!string.Equals(expectedPath, normalizedPath, StringComparison.Ordinal) &&
            !string.Equals(expectedCompletedPath, normalizedPath, StringComparison.Ordinal))
        {
            throw new FormatException($"Task document path '{normalizedPath}' does not match task id '{task.TaskId}'.");
        }

        return task;
    }

    public static string SerializeBoard(TaskBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);

        var groups = new JsonArray();
        foreach (var group in board.Groups.OrderBy(group => group.Rank, StringComparer.Ordinal).ThenBy(group => group.GroupId, StringComparer.Ordinal))
        {
            groups.Add((JsonNode)new JsonObject
            {
                ["groupId"] = group.GroupId,
                ["kind"] = group.Kind.ToString(),
                ["title"] = group.Title,
                ["description"] = group.Description,
                ["parentGroupId"] = group.ParentGroupId,
                ["rank"] = group.Rank
            });
        }

        var placements = new JsonArray();
        foreach (var placement in board.Placements.OrderBy(placement => placement.Rank, StringComparer.Ordinal).ThenBy(placement => placement.TaskId, StringComparer.Ordinal))
        {
            placements.Add((JsonNode)new JsonObject
            {
                ["taskId"] = placement.TaskId,
                ["groupId"] = placement.GroupId,
                ["rank"] = placement.Rank
            });
        }

        var tags = new JsonArray();
        foreach (var tag in board.Tags.OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase).ThenBy(tag => tag.TagId, StringComparer.Ordinal))
        {
            tags.Add((JsonNode)new JsonObject
            {
                ["tagId"] = tag.TagId,
                ["name"] = tag.Name,
                ["color"] = tag.Color.ToString()
            });
        }

        var root = new JsonObject
        {
            ["format"] = "Electron2D.TaskBoard",
            ["version"] = ProjectTaskStorage.CurrentVersion,
            ["boardId"] = board.BoardId,
            ["revision"] = board.Revision,
            ["idPolicy"] = new JsonObject
            {
                ["prefix"] = board.IdPolicy.Prefix,
                ["padding"] = board.IdPolicy.Padding,
                ["nextNumber"] = board.IdPolicy.NextNumber
            },
            ["attachmentPolicy"] = new JsonObject
            {
                ["perFileByteLimit"] = board.AttachmentPolicy.PerFileByteLimit,
                ["boardByteLimit"] = board.AttachmentPolicy.BoardByteLimit
            },
            ["migration"] = WriteBoardMigration(board.Migration),
            ["tags"] = tags,
            ["groups"] = groups,
            ["placements"] = placements
        };

        return FormatJson(root);
    }

    private static string FormatJson(JsonNode root)
    {
        var json = root.ToJsonString(IndentedOptions).ReplaceLineEndings("\n");
        var readable = new StringBuilder(json.Length);
        var insideString = false;
        for (var index = 0; index < json.Length; index++)
        {
            var character = json[index];
            if (character == '"')
            {
                insideString = !insideString;
                readable.Append(character);
                continue;
            }

            if (!insideString || character != '\\')
            {
                readable.Append(character);
                continue;
            }

            if (TryReadUnicodeEscape(json, index, out var codeUnit))
            {
                if (char.IsHighSurrogate(codeUnit) &&
                    TryReadUnicodeEscape(json, index + 6, out var lowSurrogate) &&
                    char.IsLowSurrogate(lowSurrogate))
                {
                    readable.Append(codeUnit);
                    readable.Append(lowSurrogate);
                    index += 11;
                    continue;
                }

                if (!char.IsSurrogate(codeUnit) &&
                    !char.IsControl(codeUnit) &&
                    codeUnit is not ('"' or '\\'))
                {
                    readable.Append(codeUnit);
                    index += 5;
                    continue;
                }
            }

            readable.Append(character);
            if (index + 1 < json.Length)
            {
                readable.Append(json[++index]);
            }
        }

        return readable.Append('\n').ToString();
    }

    private static bool TryReadUnicodeEscape(string json, int index, out char codeUnit)
    {
        codeUnit = default;
        if (index < 0 ||
            index + 5 >= json.Length ||
            json[index] != '\\' ||
            json[index + 1] != 'u' ||
            !ushort.TryParse(
                json.AsSpan(index + 2, 4),
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var value))
        {
            return false;
        }

        codeUnit = (char)value;
        return true;
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
        if (version != ProjectTaskStorage.CurrentVersion)
        {
            throw new FormatException($"Task board version '{version}' is not supported.");
        }

        var groups = new List<TaskBoardGroup>();
        foreach (var node in ReadArray(root, "groups", "Task board groups"))
        {
            var group = ExpectObject(node, "Task board group");
            groups.Add(new TaskBoardGroup(
                ReadString(group, "groupId", "Task board group id"),
                ReadEnum<TaskBoardGroupKind>(group, "kind", "Task board group kind"),
                ReadString(group, "title", "Task board group title"),
                ReadRequiredText(group, "description", "Task board group description"),
                ReadOptionalString(group, "parentGroupId"),
                ReadString(group, "rank", "Task board group rank")));
        }

        var placements = new List<TaskBoardPlacement>();
        foreach (var node in ReadArray(root, "placements", "Task board placements"))
        {
            var placement = ExpectObject(node, "Task board placement");
            placements.Add(new TaskBoardPlacement(
                ReadString(placement, "taskId", "Task board placement task id"),
                ReadOptionalString(placement, "groupId"),
                ReadString(placement, "rank", "Task board placement rank")));
        }

        var board = new TaskBoard(
            ReadString(root, "boardId", "Task board id"),
            ReadInt64(root, "revision", "Task board revision"),
            groups,
            placements);
        if (root.TryGetPropertyValue("tags", out var tagsNode) && tagsNode is JsonArray tags)
        {
            foreach (var node in tags)
            {
                var tag = ExpectObject(node, "Task board tag");
                board.Tags.Add(new TaskBoardTag(
                    ReadString(tag, "tagId", "Task board tag id"),
                    ReadString(tag, "name", "Task board tag name"),
                    ReadEnum<TaskBoardTagColor>(tag, "color", "Task board tag color")));
            }
        }
        var idPolicy = ExpectObject(root["idPolicy"], "Task board id policy");
        board.IdPolicy.Prefix = ReadString(idPolicy, "prefix", "Task board id prefix");
        board.IdPolicy.Padding = ReadInt32(idPolicy, "padding", "Task board id padding");
        board.IdPolicy.NextNumber = ReadInt64(idPolicy, "nextNumber", "Task board next id number");
        var attachmentPolicy = ExpectObject(root["attachmentPolicy"], "Task board attachment policy");
        board.AttachmentPolicy.PerFileByteLimit = ReadInt64(attachmentPolicy, "perFileByteLimit", "Task board per-file attachment limit");
        board.AttachmentPolicy.BoardByteLimit = ReadInt64(attachmentPolicy, "boardByteLimit", "Task board attachment limit");
        ReadBoardMigration(ExpectObject(root["migration"], "Task board migration metadata"), board.Migration);
        return board;
    }

    private static void ValidateTask(ProjectTask task)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(task.TaskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.CreatedBy);
        ArgumentException.ThrowIfNullOrWhiteSpace(task.Priority);
        ArgumentOutOfRangeException.ThrowIfLessThan(task.Revision, 1);
        _ = TaskAttachmentPreview.Resolve(task);
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

    private static JsonObject WriteExecutionContract(TaskExecutionContract contract)
    {
        return new JsonObject
        {
            ["taskType"] = contract.TaskType,
            ["readyToStart"] = WriteOrderedStringArray(contract.ReadyToStart),
            ["stopConditions"] = WriteOrderedStringArray(contract.StopConditions),
            ["allowedChanges"] = WriteOrderedStringArray(contract.AllowedChanges),
            ["forbiddenChanges"] = WriteOrderedStringArray(contract.ForbiddenChanges),
            ["requiredOutputs"] = WriteOrderedStringArray(contract.RequiredOutputs),
            ["requiredCommands"] = WriteOrderedStringArray(contract.RequiredCommands),
            ["externalAudit"] = contract.ExternalAudit
        };
    }

    private static JsonArray WriteAttachments(IEnumerable<TaskAttachment> attachments)
    {
        var array = new JsonArray();
        foreach (var attachment in attachments.OrderBy(attachment => attachment.AttachmentId, StringComparer.Ordinal))
        {
            array.Add((JsonNode)new JsonObject
            {
                ["attachmentId"] = attachment.AttachmentId,
                ["displayName"] = attachment.DisplayName,
                ["relativePath"] = attachment.RelativePath,
                ["mediaType"] = attachment.MediaType,
                ["byteLength"] = attachment.ByteLength,
                ["sha256"] = attachment.Sha256,
                ["addedAt"] = WriteDate(attachment.AddedAt),
                ["addedBy"] = attachment.AddedBy
            });
        }

        return array;
    }

    private static JsonArray WriteLegacySourceFragments(IEnumerable<LegacySourceFragment> fragments)
    {
        var array = new JsonArray();
        foreach (var fragment in fragments
            .OrderBy(fragment => fragment.SourcePath, StringComparer.Ordinal)
            .ThenBy(fragment => fragment.ByteOffset))
        {
            array.Add((JsonNode)new JsonObject
            {
                ["sourcePath"] = fragment.SourcePath,
                ["byteOffset"] = fragment.ByteOffset,
                ["byteLength"] = fragment.ByteLength,
                ["encoding"] = fragment.Encoding,
                ["hasBom"] = fragment.HasBom,
                ["lineEnding"] = fragment.LineEnding,
                ["sha256"] = fragment.Sha256,
                ["markdown"] = fragment.Markdown
            });
        }

        return array;
    }

    private static JsonObject WriteBoardMigration(TaskBoardMigrationMetadata migration)
    {
        var sourceDigests = new JsonObject();
        foreach (var pair in migration.SourceDigests)
        {
            sourceDigests[pair.Key] = pair.Value;
        }

        return new JsonObject
        {
            ["reportSha256"] = migration.ReportSha256,
            ["finalized"] = migration.Finalized,
            ["sourceDigests"] = sourceDigests,
            ["diagnostics"] = WriteOrderedStringArray(migration.Diagnostics),
            ["legacySourceFragments"] = WriteLegacySourceFragments(migration.LegacySourceFragments)
        };
    }

    private static JsonArray WriteOrderedStringArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
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

    private static string ReadRequiredText(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<string>(out var value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a JSON string.");
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

    private static long ReadInt64(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<long>(out var value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a JSON integer.");
    }

    private static bool ReadBoolean(JsonObject root, string name, string description)
    {
        if (root.TryGetPropertyValue(name, out var node) &&
            node is JsonValue jsonValue &&
            jsonValue.TryGetValue<bool>(out var value))
        {
            return value;
        }

        throw new FormatException($"{description} must be a JSON boolean.");
    }

    private static T ReadEnum<T>(JsonObject root, string name, string description)
        where T : struct, Enum
    {
        var text = ReadString(root, name, description);
        return Enum.TryParse<T>(text, ignoreCase: false, out var value)
            ? value
            : throw new FormatException($"{description} value '{text}' is not supported.");
    }

    private static ProjectTaskStatus ReadTaskStatus(JsonObject root)
    {
        var text = ReadString(root, "status", "Task status");
        if (string.Equals(text, "Backlog", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.Ready;
        }

        if (string.Equals(text, "AwaitingAcceptance", StringComparison.Ordinal))
        {
            return ProjectTaskStatus.Review;
        }

        return Enum.TryParse<ProjectTaskStatus>(text, ignoreCase: false, out var value)
            ? value
            : throw new FormatException($"Task status value '{text}' is not supported.");
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

    private static DateOnly? ReadOptionalDateOnly(JsonObject root, string name, string description)
    {
        var value = ReadOptionalString(root, name);
        if (value is null)
        {
            return null;
        }

        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : throw new FormatException($"{description} must be an ISO 8601 calendar date.");
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

    private static TaskExecutionContract ReadExecutionContract(JsonObject root)
    {
        var value = root.TryGetPropertyValue("executionContract", out var node)
            ? ExpectObject(node, "Task execution contract")
            : throw new FormatException("Task execution contract must be a JSON object.");
        var contract = new TaskExecutionContract
        {
            TaskType = ReadString(value, "taskType", "Task execution contract task type"),
            ExternalAudit = ReadString(value, "externalAudit", "Task execution contract external audit")
        };
        contract.ReadyToStart.AddRange(ReadStringArray(value, "readyToStart", "Task execution contract ready-to-start rules"));
        contract.StopConditions.AddRange(ReadStringArray(value, "stopConditions", "Task execution contract stop conditions"));
        contract.AllowedChanges.AddRange(ReadStringArray(value, "allowedChanges", "Task execution contract allowed changes"));
        contract.ForbiddenChanges.AddRange(ReadStringArray(value, "forbiddenChanges", "Task execution contract forbidden changes"));
        contract.RequiredOutputs.AddRange(ReadStringArray(value, "requiredOutputs", "Task execution contract required outputs"));
        contract.RequiredCommands.AddRange(ReadStringArray(value, "requiredCommands", "Task execution contract required commands"));
        return contract;
    }

    private static void CopyExecutionContract(TaskExecutionContract source, TaskExecutionContract destination)
    {
        destination.TaskType = source.TaskType;
        destination.ExternalAudit = source.ExternalAudit;
        destination.ReadyToStart.AddRange(source.ReadyToStart);
        destination.StopConditions.AddRange(source.StopConditions);
        destination.AllowedChanges.AddRange(source.AllowedChanges);
        destination.ForbiddenChanges.AddRange(source.ForbiddenChanges);
        destination.RequiredOutputs.AddRange(source.RequiredOutputs);
        destination.RequiredCommands.AddRange(source.RequiredCommands);
    }

    private static IReadOnlyList<TaskAttachment> ReadAttachments(JsonObject root)
    {
        return ReadArray(root, "attachments", "Task attachments")
            .Select(node =>
            {
                var attachment = ExpectObject(node, "Task attachment");
                return new TaskAttachment
                {
                    AttachmentId = ReadString(attachment, "attachmentId", "Task attachment id"),
                    DisplayName = ReadString(attachment, "displayName", "Task attachment display name"),
                    RelativePath = ReadString(attachment, "relativePath", "Task attachment relative path"),
                    MediaType = ReadString(attachment, "mediaType", "Task attachment media type"),
                    ByteLength = ReadInt64(attachment, "byteLength", "Task attachment byte length"),
                    Sha256 = ReadString(attachment, "sha256", "Task attachment SHA-256"),
                    AddedAt = ReadDate(attachment, "addedAt", "Task attachment added time"),
                    AddedBy = ReadString(attachment, "addedBy", "Task attachment actor")
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<LegacySourceFragment> ReadLegacySourceFragments(JsonObject root)
    {
        return ReadArray(root, "legacySourceFragments", "Task legacy source fragments")
            .Select(node =>
            {
                var fragment = ExpectObject(node, "Task legacy source fragment");
                return new LegacySourceFragment
                {
                    SourcePath = ReadString(fragment, "sourcePath", "Legacy source path"),
                    ByteOffset = ReadInt64(fragment, "byteOffset", "Legacy source byte offset"),
                    ByteLength = ReadInt64(fragment, "byteLength", "Legacy source byte length"),
                    Encoding = ReadString(fragment, "encoding", "Legacy source encoding"),
                    HasBom = ReadBoolean(fragment, "hasBom", "Legacy source BOM flag"),
                    LineEnding = ReadString(fragment, "lineEnding", "Legacy source line ending"),
                    Sha256 = ReadString(fragment, "sha256", "Legacy source SHA-256"),
                    Markdown = ReadString(fragment, "markdown", "Legacy source Markdown")
                };
            })
            .ToArray();
    }

    private static void ReadBoardMigration(JsonObject root, TaskBoardMigrationMetadata migration)
    {
        migration.ReportSha256 = ReadOptionalString(root, "reportSha256");
        migration.Finalized = ReadBoolean(root, "finalized", "Task board migration finalized flag");
        var sourceDigests = ExpectObject(root["sourceDigests"], "Task board migration source digests");
        foreach (var pair in sourceDigests.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            migration.SourceDigests[pair.Key] = pair.Value?.GetValue<string>() ??
                throw new FormatException($"Task board source digest '{pair.Key}' must be a string.");
        }

        migration.Diagnostics.AddRange(ReadStringArray(root, "diagnostics", "Task board migration diagnostics"));
        migration.LegacySourceFragments.AddRange(ReadLegacySourceFragments(root));
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

        if (!taskById.ContainsKey(dependencyTaskId))
        {
            return new TaskDependencyGraphResult(
                succeeded: false,
                task,
                [TaskDiagnostic("E2D-TASK-0003", $"Dependency task '{dependencyTaskId}' was not found.")]);
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

    public static ProjectTaskStatus ResolveBoardStatus(ProjectTask task, IEnumerable<ProjectTask> dependencies)
    {
        var refreshed = RefreshReadiness(task, dependencies).Task;
        if (refreshed.Status != ProjectTaskStatus.Ready)
        {
            return refreshed.Status;
        }

        return refreshed.Readiness != TaskReadiness.Ready ||
            refreshed.BlockingReasons.Any(reason => reason != TaskBlockingReason.Dependency)
                ? ProjectTaskStatus.Blocked
                : ProjectTaskStatus.Ready;
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
        var isSubmission = task.Status == ProjectTaskStatus.Review && request.TargetStatus == ProjectTaskStatus.Review;
        ApplyStatusUpdate(
            updated,
            request.TargetStatus,
            request.Context,
            payload: isSubmission ? "Submitted for human acceptance." : $"Status changed to {request.TargetStatus}.");
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
        if (task.Status != ProjectTaskStatus.Review ||
            task.AcceptanceState != ProjectTaskAcceptanceState.Submitted ||
            !request.Context.HasCapability(OperationCapability.TaskRequestChanges) ||
            request.Context.PrincipalKind != PrincipalKind.Human)
        {
            return ProjectTaskMutationResult.Rejected(
                task,
                request.OperationId,
                TaskDiagnostic("E2D-TASK-0002", "Request changes requires a submitted Review task and a trusted human context."),
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

        if (request.TargetStatus is not (ProjectTaskStatus.Ready or ProjectTaskStatus.InProgress))
        {
            return ProjectTaskMutationResult.Rejected(
                task,
                request.OperationId,
                TaskDiagnostic("E2D-TASK-0002", "Reopen target status must be Ready or InProgress."),
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
        var isSubmission = task.Status == ProjectTaskStatus.Review && targetStatus == ProjectTaskStatus.Review;
        if (isSubmission)
        {
            if (task.AcceptanceState == ProjectTaskAcceptanceState.Submitted)
            {
                return TaskDiagnostic("E2D-TASK-0002", "Task is already submitted for human acceptance.");
            }

            return context.HasCapability(OperationCapability.TaskSubmitForAcceptance)
                ? null
                : TaskDiagnostic("E2D-TASK-0002", "Submitting a task for acceptance requires TaskSubmitForAcceptance capability.");
        }

        if (!IsTransitionAllowed(task.Status, targetStatus))
        {
            return TaskDiagnostic("E2D-TASK-0002", $"Task status transition '{task.Status}' -> '{targetStatus}' is not allowed.");
        }

        if (task.Status == ProjectTaskStatus.Review &&
            task.AcceptanceState == ProjectTaskAcceptanceState.Submitted &&
            targetStatus is not (ProjectTaskStatus.Done or ProjectTaskStatus.InProgress))
        {
            return TaskDiagnostic("E2D-TASK-0002", "A submitted Review task requires a human acceptance decision.");
        }

        if (targetStatus == ProjectTaskStatus.Done &&
            (task.Status != ProjectTaskStatus.Review ||
             task.AcceptanceState != ProjectTaskAcceptanceState.Submitted ||
             !context.HasCapability(OperationCapability.TaskAccept) ||
             context.PrincipalKind != PrincipalKind.Human))
        {
            return TaskDiagnostic("E2D-TASK-0002", "Accepting a task requires trusted human TaskAccept capability.");
        }

        if (task.Status == ProjectTaskStatus.Review &&
            task.AcceptanceState == ProjectTaskAcceptanceState.Submitted &&
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
        var previousStatus = task.Status;
        var previousAcceptanceState = task.AcceptanceState;
        task.Status = targetStatus;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        if (previousStatus == ProjectTaskStatus.Review && targetStatus == ProjectTaskStatus.Review)
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
        else if (targetStatus == ProjectTaskStatus.Review)
        {
            task.AcceptanceState = ProjectTaskAcceptanceState.Open;
        }
        else if (targetStatus == ProjectTaskStatus.InProgress && previousAcceptanceState == ProjectTaskAcceptanceState.Submitted)
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
            ProjectTaskStatus.Ready => target is ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.InProgress => target is ProjectTaskStatus.Ready or ProjectTaskStatus.Blocked or ProjectTaskStatus.Review or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Blocked => target is ProjectTaskStatus.Ready or ProjectTaskStatus.InProgress or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Review => target is ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked or ProjectTaskStatus.Done,
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
