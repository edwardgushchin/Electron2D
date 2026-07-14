/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <edwardgushchin@yandex.ru>
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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Electron2D.ProjectSystem;

internal sealed record TaskBoardV3MutationResult(
    JsonObject Task,
    JsonObject Board,
    IReadOnlyList<JsonObject> ActiveTasks,
    IReadOnlyList<JsonObject> CompletedTasks,
    IReadOnlyList<string> ChangedFiles,
    bool DryRun);

internal sealed record TaskBoardV3BoardMutationResult(
    JsonObject Board,
    IReadOnlyList<JsonObject> ActiveTasks,
    IReadOnlyList<JsonObject> CompletedTasks,
    IReadOnlyList<string> ChangedFiles,
    bool DryRun);

internal sealed record TaskBoardV3TagUpdate(
    string TaskId,
    long ExpectedRevision,
    IReadOnlyList<string> TagIds);

internal sealed partial class TaskBoardV3DiskStore
{
    public TaskBoardV3MutationResult Create(
        string title,
        string description,
        string priority,
        DateOnly? deadline,
        JsonObject? structuredInput,
        long? expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(priority);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var boardRevision = RequiredLong(snapshot.Board, "revision");
        if (expectedBoardRevision is { } expected && boardRevision != expected)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict: expected {expected}, actual {boardRevision}.",
                isRetryable: false,
                actualBoardRevision: boardRevision);
        }

        var board = snapshot.Board.DeepClone().AsObject();
        var idPolicy = RequiredObject(board["idPolicy"]);
        var nextNumber = RequiredLong(idPolicy, "nextNumber");
        var padding = checked((int)RequiredLong(idPolicy, "padding"));
        var taskId = RequiredString(idPolicy, "prefix") + nextNumber.ToString($"D{padding}", CultureInfo.InvariantCulture);
        if (snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).Any(task =>
            string.Equals(RequiredString(task, "taskId"), taskId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Allocated task id '{taskId}' already exists.");
        }

        var taskUid = $"task-{Guid.NewGuid():N}";
        var task = new JsonObject
        {
            ["format"] = "Electron2D.TaskFile",
            ["version"] = 3,
            ["boardId"] = RequiredString(board, "boardId"),
            ["taskUid"] = taskUid,
            ["revision"] = 1,
            ["taskId"] = taskId,
            ["legacyAliases"] = new JsonArray(),
            ["title"] = title.Trim(),
            ["description"] = description,
            ["status"] = "Ready",
            ["acceptanceState"] = "NotSubmitted",
            ["priority"] = priority.Trim(),
            ["tagIds"] = new JsonArray(),
            ["deadline"] = deadline?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["createdBy"] = actorId,
            ["assignee"] = null,
            ["parentTaskUid"] = null,
            ["relations"] = new JsonArray(),
            ["acceptanceCriteria"] = new JsonArray(),
            ["blockers"] = new JsonArray(),
            ["lastActivitySequence"] = 0,
            ["activity"] = new JsonArray(),
            ["auditRuns"] = new JsonArray(),
            ["conversation"] = new JsonObject
            {
                ["lastMessageSequence"] = 0,
                ["messages"] = new JsonArray(),
                ["contextCheckpoints"] = new JsonArray()
            },
            ["contextSnapshot"] = null,
            ["workspaceChanges"] = new JsonObject
            {
                ["baseRevision"] = null,
                ["currentRevision"] = null,
                ["files"] = new JsonArray()
            },
            ["links"] = new JsonArray(),
            ["executionContract"] = new JsonObject
            {
                ["taskType"] = "general",
                ["readyToStart"] = new JsonArray(),
                ["stopConditions"] = new JsonArray(),
                ["allowedChanges"] = new JsonArray(),
                ["forbiddenChanges"] = new JsonArray(),
                ["requiredOutputs"] = new JsonArray(),
                ["commands"] = new JsonArray(),
                ["externalAudit"] = TaskBoardV3Migration.ConvertExternalAudit(null)
            },
            ["attachments"] = new JsonArray(),
            ["previewAttachmentId"] = null,
            ["legacySourceFragments"] = new JsonArray(),
            ["createdAt"] = FormatDate(now),
            ["updatedAt"] = FormatDate(now),
            ["submittedAt"] = null,
            ["completedAt"] = null,
            ["acceptedAt"] = null,
            ["acceptedBy"] = null,
            ["cancelledAt"] = null,
            ["cancellationReason"] = null,
            ["archivedAt"] = null,
            ["archivedBy"] = null
        };
        ApplyStructuredTaskInput(task, structuredInput);
        TaskBoardV3Compatibility.UpgradeTask(task);

        board["revision"] = boardRevision + 1;
        idPolicy["nextNumber"] = nextNumber + 1;
        var placements = RequiredArray(board, "placements");
        placements.Add(new JsonObject
        {
            ["taskUid"] = taskUid,
            ["groupId"] = null,
            ["rank"] = NextRootRank(placements)
        });

        var active = snapshot.ActiveTasks.Concat([task]).ToArray();
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, active, snapshot.CompletedTasks);
        var changes = new[]
        {
            new TaskBoardBinaryChange(
                ProjectTaskStorage.BoardDocumentPath,
                Utf8(TaskBoardV3Migration.Serialize(board))),
            new TaskBoardBinaryChange(
                ProjectTaskStorage.GetTaskDocumentPath(taskId),
                Utf8(TaskBoardV3Migration.Serialize(task)))
        };
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
        }

        return new TaskBoardV3MutationResult(
            task,
            board,
            active,
            snapshot.CompletedTasks,
            dryRun ? [] : changes.Select(change => change.Path).ToArray(),
            dryRun);
    }

    public TaskBoardV3BoardMutationResult Normalize(long expectedBoardRevision, bool dryRun)
    {
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var revision = RequiredLong(snapshot.Board, "revision");
        if (revision != expectedBoardRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict: expected {expectedBoardRevision}, actual {revision}.",
                isRetryable: false,
                actualBoardRevision: revision);
        }

        var board = snapshot.Board;
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, snapshot.ActiveTasks, snapshot.CompletedTasks);
        var changes = new List<TaskBoardBinaryChange>();
        AddCanonicalChange(changes, ProjectTaskStorage.BoardDocumentPath, board);
        foreach (var task in snapshot.ActiveTasks)
        {
            AddCanonicalChange(changes, ProjectTaskStorage.GetTaskDocumentPath(RequiredString(task, "taskId")), task);
        }

        foreach (var task in snapshot.CompletedTasks)
        {
            AddCanonicalChange(changes, ProjectTaskStorage.GetCompletedTaskDocumentPath(RequiredString(task, "taskId")), task);
        }

        var gitIgnorePath = $"{ProjectTaskStorage.RootDirectory}/.gitignore";
        var gitIgnoreBytes = Utf8(TaskBoardDiskStore.TaskboardGitIgnoreText);
        var currentGitIgnorePath = Path.Combine(projectRoot, gitIgnorePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(currentGitIgnorePath) || !File.ReadAllBytes(currentGitIgnorePath).SequenceEqual(gitIgnoreBytes))
        {
            changes.Add(new TaskBoardBinaryChange(gitIgnorePath, gitIgnoreBytes));
        }

        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
        }

        return new TaskBoardV3BoardMutationResult(
            board,
            snapshot.ActiveTasks,
            snapshot.CompletedTasks,
            dryRun ? [] : changes.Select(change => change.Path).ToArray(),
            dryRun);
    }

    public TaskBoardV3BoardMutationResult Delete(
        string taskId,
        string confirmation,
        long expectedRevision,
        long expectedBoardRevision,
        bool deleteAttachments,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        if (!string.Equals(taskId, confirmation, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Hard delete requires exact confirmation '{taskId}'.");
        }

        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var boardRevision = RequiredLong(snapshot.Board, "revision");
        if (boardRevision != expectedBoardRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict: expected {expectedBoardRevision}, actual {boardRevision}.",
                isRetryable: false,
                actualBoardRevision: boardRevision);
        }

        var task = FindTask(snapshot, taskId);
        var taskRevision = RequiredLong(task, "revision");
        if (taskRevision != expectedRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Task revision conflict for '{taskId}': expected {expectedRevision}, actual {taskRevision}.",
                isRetryable: false,
                actualTaskRevision: taskRevision);
        }

        var taskUid = RequiredString(task, "taskUid");
        foreach (var other in snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).Where(candidate => !ReferenceEquals(candidate, task)))
        {
            if (string.Equals(NullableString(other, "parentTaskUid"), taskUid, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Task '{taskId}' has an incoming containment reference from '{RequiredString(other, "taskId")}'.");
            }

            if (RequiredArray(other, "relations").Select(RequiredObject).Any(relation =>
                string.Equals(RequiredString(relation, "targetTaskUid"), taskUid, StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Task '{taskId}' has an incoming dependency or relation from '{RequiredString(other, "taskId")}'.");
            }
        }

        if (RequiredArray(task, "links").Count > 0)
        {
            throw new InvalidOperationException($"Task '{taskId}' has typed external links and cannot be hard deleted.");
        }

        var attachments = RequiredArray(task, "attachments").Select(RequiredObject).ToArray();
        if (attachments.Length > 0 && !deleteAttachments)
        {
            throw new InvalidOperationException("Hard delete requires `--delete-attachments true` when attachments exist.");
        }

        var board = snapshot.Board.DeepClone().AsObject();
        board["revision"] = boardRevision + 1;
        var placements = RequiredArray(board, "placements");
        var placement = placements.Select(RequiredObject).SingleOrDefault(item =>
            string.Equals(RequiredString(item, "taskUid"), taskUid, StringComparison.Ordinal));
        if (placement is not null)
        {
            placements.Remove(placement);
        }

        var active = snapshot.ActiveTasks.Where(candidate => !ReferenceEquals(candidate, task)).ToArray();
        var completed = snapshot.CompletedTasks.Where(candidate => !ReferenceEquals(candidate, task)).ToArray();
        TaskBoardV3TransitionValidator.ValidateBoard(
            snapshot.Board,
            board,
            new TaskBoardV3MutationContext("cli", TaskBoardV3Capability.EditBoard));
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, active, completed, validateAttachmentBlobs: !dryRun);
        var taskPath = snapshot.ActiveTasks.Contains(task)
            ? ProjectTaskStorage.GetTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var changes = new List<TaskBoardBinaryChange>
        {
            new(ProjectTaskStorage.BoardDocumentPath, Utf8(TaskBoardV3Migration.Serialize(board))),
            new(taskPath, Content: null)
        };
        changes.AddRange(attachments.Select(attachment =>
            new TaskBoardBinaryChange(RequiredString(attachment, "relativePath"), Content: null)));
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
            var attachmentRoot = Path.Combine(
                projectRoot,
                ProjectTaskStorage.AttachmentsDirectory.Replace('/', Path.DirectorySeparatorChar),
                taskUid);
            if (Directory.Exists(attachmentRoot) && !Directory.EnumerateFileSystemEntries(attachmentRoot, "*", SearchOption.AllDirectories).Any())
            {
                Directory.Delete(attachmentRoot, recursive: true);
            }
        }

        return new TaskBoardV3BoardMutationResult(
            board,
            active,
            completed,
            dryRun ? [] : changes.Select(change => change.Path).ToArray(),
            dryRun);
    }

    public TaskBoardV3BoardMutationResult Move(
        string taskId,
        string? groupId,
        string rank,
        long expectedBoardRevision,
        bool dryRun)
    {
        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var snapshot = LoadSnapshotCore();
            var taskUid = RequiredString(FindTask(snapshot, taskId), "taskUid");
            var placement = RequiredArray(board, "placements").Select(RequiredObject).Single(item =>
                RequiredString(item, "taskUid") == taskUid);
            placement["groupId"] = groupId;
            placement["rank"] = NormalizeRank(rank);
        });
    }

    public TaskBoardV3BoardMutationResult CreateTag(
        string name,
        string color,
        long expectedBoardRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedColor = NormalizeTagColor(color);
        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var tags = RequiredArray(board, "tags");
            if (tags.Select(RequiredObject).Any(tag => string.Equals(RequiredString(tag, "name"), name, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Taskboard tag '{name}' already exists.");
            }

            tags.Add(new JsonObject
            {
                ["tagId"] = NextChildId(tags.Select(RequiredObject), "tagId", "tag"),
                ["name"] = name.Trim(),
                ["color"] = normalizedColor
            });
        });
    }

    public TaskBoardV3MutationResult CreateTagAndAssign(
        string name,
        string color,
        string taskId,
        long expectedTaskRevision,
        long expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var normalizedColor = NormalizeTagColor(color);
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var boardRevision = RequiredLong(snapshot.Board, "revision");
        if (boardRevision != expectedBoardRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict: expected {expectedBoardRevision}, actual {boardRevision}.",
                isRetryable: false,
                actualBoardRevision: boardRevision);
        }

        var previous = FindTask(snapshot, taskId);
        var taskRevision = RequiredLong(previous, "revision");
        if (taskRevision != expectedTaskRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Task revision conflict for '{taskId}': expected {expectedTaskRevision}, actual {taskRevision}.",
                isRetryable: false,
                actualTaskRevision: taskRevision);
        }

        var board = snapshot.Board.DeepClone().AsObject();
        var tags = RequiredArray(board, "tags");
        if (tags.Select(RequiredObject).Any(tag => string.Equals(RequiredString(tag, "name"), name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Taskboard tag '{name}' already exists.");
        }

        var tagId = NextChildId(tags.Select(RequiredObject), "tagId", "tag");
        tags.Add(new JsonObject { ["tagId"] = tagId, ["name"] = name.Trim(), ["color"] = normalizedColor });
        board["revision"] = boardRevision + 1;
        var next = previous.DeepClone().AsObject();
        RequiredArray(next, "tagIds").Add(tagId);
        TaskPatchV3.AppendIfRequired(previous, next, actorId, "Agent", now);
        next["revision"] = expectedTaskRevision + 1;
        next["updatedAt"] = FormatDate(now);
        TaskBoardV3TransitionValidator.ValidateBoard(
            snapshot.Board,
            board,
            new TaskBoardV3MutationContext(actorId, TaskBoardV3Capability.EditBoard));
        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext(actorId, TaskBoardV3Capability.EditTask));
        var active = Replace(snapshot.ActiveTasks, previous, next);
        var completed = Replace(snapshot.CompletedTasks, previous, next);
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, active, completed);
        var taskPath = snapshot.ActiveTasks.Contains(previous)
            ? ProjectTaskStorage.GetTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var changes = new[]
        {
            new TaskBoardBinaryChange(ProjectTaskStorage.BoardDocumentPath, Utf8(TaskBoardV3Migration.Serialize(board))),
            new TaskBoardBinaryChange(taskPath, Utf8(TaskBoardV3Migration.Serialize(next)))
        };
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
        }

        return new TaskBoardV3MutationResult(next, board, active, completed, dryRun ? [] : changes.Select(change => change.Path).ToArray(), dryRun);
    }

    public TaskBoardV3BoardMutationResult UpdateTag(
        string tagId,
        string? name,
        string? color,
        long expectedBoardRevision,
        bool dryRun)
    {
        var normalizedColor = color is null ? null : NormalizeTagColor(color);
        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var tag = RequiredArray(board, "tags").Select(RequiredObject).SingleOrDefault(item =>
                RequiredString(item, "tagId") == tagId) ?? throw new FileNotFoundException($"Tag '{tagId}' was not found.");
            if (name is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(name);
                tag["name"] = name.Trim();
            }

            if (normalizedColor is not null)
            {
                tag["color"] = normalizedColor;
            }
        });
    }

    public TaskBoardV3BoardMutationResult DeleteTag(string tagId, long expectedBoardRevision, bool dryRun)
    {
        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var snapshot = LoadSnapshotCore();
            if (snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).Any(task =>
                RequiredArray(task, "tagIds").Any(value => value?.GetValue<string>() == tagId)))
            {
                throw new InvalidOperationException($"Tag '{tagId}' is still assigned to a task.");
            }

            var tags = RequiredArray(board, "tags");
            var tag = tags.Select(RequiredObject).SingleOrDefault(item => RequiredString(item, "tagId") == tagId) ??
                throw new FileNotFoundException($"Tag '{tagId}' was not found.");
            tags.Remove(tag);
        });
    }

    public TaskBoardV3MutationResult AssignTag(
        string taskId,
        string tagId,
        long expectedTaskRevision,
        long expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool assign,
        bool dryRun)
    {
        return MutateTaskWithSnapshot(taskId, expectedTaskRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, (task, snapshot) =>
        {
            var boardRevision = RequiredLong(snapshot.Board, "revision");
            if (boardRevision != expectedBoardRevision)
            {
                throw new TaskBoardWriteException(
                    "E2D-TASK-0006",
                    $"Taskboard revision conflict while assigning tag: expected {expectedBoardRevision}, actual {boardRevision}.",
                    isRetryable: false,
                    actualBoardRevision: boardRevision);
            }

            if (!RequiredArray(snapshot.Board, "tags").Select(RequiredObject).Any(tag => RequiredString(tag, "tagId") == tagId))
            {
                throw new FileNotFoundException($"Tag '{tagId}' was not found.");
            }

            var tags = RequiredArray(task, "tagIds");
            var existing = tags.SingleOrDefault(value => value?.GetValue<string>() == tagId);
            if (assign && existing is null)
            {
                tags.Add(tagId);
            }
            else if (!assign && existing is not null)
            {
                tags.Remove(existing);
            }
        });
    }

    public TaskBoardV3BoardMutationResult ApplyTagUpdates(
        IReadOnlyList<TaskBoardV3TagUpdate> updates,
        long expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentNullException.ThrowIfNull(updates);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        if (updates.Count == 0)
        {
            throw new InvalidOperationException("Batch tag plan must contain at least one update.");
        }

        var duplicateTaskId = updates.GroupBy(update => update.TaskId, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)?.Key;
        if (duplicateTaskId is not null)
        {
            throw new InvalidOperationException($"Batch tag plan contains duplicate task '{duplicateTaskId}'.");
        }

        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var boardRevision = RequiredLong(snapshot.Board, "revision");
        if (boardRevision != expectedBoardRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict while applying batch tag plan: expected {expectedBoardRevision}, actual {boardRevision}.",
                isRetryable: false,
                actualBoardRevision: boardRevision);
        }

        var knownTags = RequiredArray(snapshot.Board, "tags").Select(RequiredObject)
            .Select(tag => RequiredString(tag, "tagId"))
            .ToHashSet(StringComparer.Ordinal);
        var active = snapshot.ActiveTasks.ToArray();
        var completed = snapshot.CompletedTasks.ToArray();
        var changes = new List<TaskBoardBinaryChange>(updates.Count);
        foreach (var update in updates)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(update.TaskId);
            var duplicateTagId = update.TagIds.GroupBy(tagId => tagId, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1)?.Key;
            if (duplicateTagId is not null)
            {
                throw new InvalidOperationException($"Task '{update.TaskId}' contains duplicate tag '{duplicateTagId}'.");
            }

            var unknownTagId = update.TagIds.FirstOrDefault(tagId => !knownTags.Contains(tagId));
            if (unknownTagId is not null)
            {
                throw new FileNotFoundException($"Tag '{unknownTagId}' was not found for task '{update.TaskId}'.");
            }

            var previous = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).SingleOrDefault(task =>
                string.Equals(RequiredString(task, "taskId"), update.TaskId, StringComparison.Ordinal)) ??
                throw new FileNotFoundException($"Task '{update.TaskId}' was not found.");
            var taskRevision = RequiredLong(previous, "revision");
            if (taskRevision != update.ExpectedRevision)
            {
                throw new TaskBoardWriteException(
                    "E2D-TASK-0006",
                    $"Task revision conflict for '{update.TaskId}': expected {update.ExpectedRevision}, actual {taskRevision}.",
                    isRetryable: false,
                    actualTaskRevision: taskRevision);
            }

            var next = previous.DeepClone().AsObject();
            next["tagIds"] = new JsonArray(update.TagIds
                .Select(tagId => (JsonNode)JsonValue.Create(tagId)!).ToArray());
            TaskPatchV3.AppendIfRequired(previous, next, actorId, "Agent", now);
            next["revision"] = update.ExpectedRevision + 1;
            next["updatedAt"] = FormatDate(now);
            TaskBoardV3TransitionValidator.ValidateTask(
                previous,
                next,
                new TaskBoardV3MutationContext(actorId, TaskBoardV3Capability.EditTask));
            active = Replace(active, previous, next).ToArray();
            completed = Replace(completed, previous, next).ToArray();
            var relativePath = snapshot.ActiveTasks.Contains(previous)
                ? ProjectTaskStorage.GetTaskDocumentPath(update.TaskId)
                : ProjectTaskStorage.GetCompletedTaskDocumentPath(update.TaskId);
            changes.Add(new TaskBoardBinaryChange(relativePath, Utf8(TaskBoardV3Migration.Serialize(next))));
        }

        TaskBoardV3SemanticValidator.Validate(projectRoot, snapshot.Board, active, completed);
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
        }

        return new TaskBoardV3BoardMutationResult(
            snapshot.Board,
            active,
            completed,
            dryRun ? [] : changes.Select(change => change.Path).ToArray(),
            dryRun);
    }

    public TaskBoardV3BoardMutationResult AddGroup(
        string kind,
        string title,
        string description,
        string? parentGroupId,
        long expectedBoardRevision,
        bool dryRun)
    {
        if (kind is not ("Epoch" or "Milestone"))
        {
            throw new InvalidOperationException($"Group kind '{kind}' is not supported.");
        }

        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var groups = RequiredArray(board, "groups");
            groups.Add(new JsonObject
            {
                ["groupId"] = NextChildId(groups.Select(RequiredObject), "groupId", "G"),
                ["kind"] = kind,
                ["title"] = title,
                ["description"] = description,
                ["parentGroupId"] = parentGroupId,
                ["rank"] = NextGroupRank(groups, parentGroupId)
            });
        });
    }

    public TaskBoardV3BoardMutationResult UpdateGroup(
        string groupId,
        string? title,
        string? description,
        string? parentGroupId,
        string? rank,
        long expectedBoardRevision,
        bool dryRun)
    {
        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var group = RequiredArray(board, "groups").Select(RequiredObject).SingleOrDefault(item =>
                RequiredString(item, "groupId") == groupId) ?? throw new FileNotFoundException($"Group '{groupId}' was not found.");
            if (title is not null) group["title"] = title;
            if (description is not null) group["description"] = description;
            if (parentGroupId is not null) group["parentGroupId"] = parentGroupId;
            if (rank is not null) group["rank"] = NormalizeRank(rank);
        });
    }

    public TaskBoardV3BoardMutationResult RemoveGroup(string groupId, long expectedBoardRevision, bool dryRun)
    {
        return MutateBoard(expectedBoardRevision, dryRun, board =>
        {
            var groups = RequiredArray(board, "groups");
            if (groups.Select(RequiredObject).Any(group => NullableString(group, "parentGroupId") == groupId) ||
                RequiredArray(board, "placements").Select(RequiredObject).Any(placement => NullableString(placement, "groupId") == groupId))
            {
                throw new InvalidOperationException($"Group '{groupId}' is not empty.");
            }

            var group = groups.Select(RequiredObject).SingleOrDefault(item => RequiredString(item, "groupId") == groupId) ??
                throw new FileNotFoundException($"Group '{groupId}' was not found.");
            groups.Remove(group);
        });
    }

    public TaskBoardV3MutationResult Update(
        string taskId,
        long expectedRevision,
        string? title,
        string? description,
        string? priority,
        DateOnly? deadline,
        bool clearDeadline,
        JsonObject? structuredInput,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(
            taskId,
            expectedRevision,
            actorId,
            now,
            dryRun,
            TaskBoardV3Capability.EditTask,
            task =>
            {
                if (title is not null)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(title);
                    task["title"] = title.Trim();
                }

                if (description is not null)
                {
                    task["description"] = description;
                }

                if (priority is not null)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(priority);
                    task["priority"] = priority.Trim();
                }

                if (deadline is not null)
                {
                    task["deadline"] = deadline.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
                else if (clearDeadline)
                {
                    task["deadline"] = null;
                }

                ApplyStructuredTaskInput(task, structuredInput);
            });
    }

    private static void ApplyStructuredTaskInput(JsonObject task, JsonObject? structuredInput)
    {
        if (structuredInput is null)
        {
            return;
        }

        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "executionContract",
            "acceptanceCriteria",
            "links",
            "tagIds",
            "parentTaskUid",
            "relations",
            "assignee"
        };
        foreach (var property in structuredInput)
        {
            if (!allowed.Contains(property.Key))
            {
                throw new InvalidOperationException($"Structured task field '{property.Key}' is not mutable.");
            }

            if (property.Value is null && property.Key != "assignee")
            {
                throw new InvalidOperationException($"Structured task field '{property.Key}' cannot be null.");
            }

            task[property.Key] = property.Value?.DeepClone();
        }

        TaskBoardV3Compatibility.UpgradeTask(task);
    }

    public TaskBoardV3MutationResult AddComment(
        string taskId,
        string markdown,
        long expectedRevision,
        string actorId,
        string actorKind,
        DateTimeOffset now,
        bool dryRun,
        string? agentRunId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markdown);
        if (actorKind != "Agent" && agentRunId is not null)
        {
            throw new InvalidOperationException("Only an agent message may declare an agent run id.");
        }

        return MutateTask(
            taskId,
            expectedRevision,
            actorId,
            now,
            dryRun,
            TaskBoardV3Capability.EditTask,
            task =>
            {
                var conversation = RequiredObject(task["conversation"]);
                var nextSequence = RequiredLong(conversation, "lastMessageSequence") + 1;
                conversation["lastMessageSequence"] = nextSequence;
                RequiredArray(conversation, "messages").Add(new JsonObject
                {
                    ["messageId"] = $"message-{Guid.NewGuid():N}",
                    ["sequence"] = nextSequence,
                    ["author"] = new JsonObject
                    {
                        ["actorId"] = actorId,
                        ["actorKind"] = actorKind,
                        ["role"] = actorKind == "Human" ? "Owner" : "Worker"
                    },
                    ["createdAt"] = FormatDate(now),
                    ["replyToMessageId"] = null,
                    ["agentRunId"] = actorKind == "Agent" ? agentRunId ?? $"run-{Guid.NewGuid():N}" : null,
                    ["content"] = new JsonArray(new JsonObject { ["kind"] = "Markdown", ["markdown"] = markdown })
                });
            },
            role: actorKind == "Human" ? TaskBoardV3Role.Owner : TaskBoardV3Role.Worker,
            actorKind: actorKind);
    }

    internal TaskBoardV3MutationResult RecordAgentContext(
        string taskId,
        string agentRunId,
        string actorId,
        TaskBoardV3Role role,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun,
        string? rebaseOfCheckpointId = null)
    {
        return MutateTask(
            taskId,
            expectedRevision,
            actorId,
            now,
            dryRun,
            TaskBoardV3Capability.EditTask,
            task =>
            {
                var checkpoint = AgentContextBuilderV3.BuildCheckpoint(task, agentRunId, actorId, role, rebaseOfCheckpointId);
                checkpoint["createdAt"] = FormatDate(now);
                checkpoint["taskRevision"] = expectedRevision + 1;
                RequiredArray(RequiredObject(task["conversation"]), "contextCheckpoints").Add(checkpoint);
            },
            role,
            role == TaskBoardV3Role.Owner ? "Human" : "Agent");
    }

    public TaskBoardV3MutationResult AddCriterion(
        string taskId,
        string criterionId,
        string description,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return MutateTask(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, task =>
        {
            var criteria = RequiredArray(task, "acceptanceCriteria");
            if (criteria.Select(RequiredObject).Any(criterion => RequiredString(criterion, "criterionId") == criterionId))
            {
                throw new InvalidOperationException($"Acceptance criterion '{criterionId}' already exists.");
            }

            criteria.Add(new JsonObject
            {
                ["criterionId"] = criterionId,
                ["description"] = description,
                ["state"] = "Open",
                ["evidence"] = new JsonArray()
            });
        });
    }

    public TaskBoardV3MutationResult UpdateCriterion(
        string taskId,
        string criterionId,
        string description,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return MutateCriterion(taskId, criterionId, expectedRevision, actorId, now, dryRun, criterion =>
            criterion["description"] = description);
    }

    public TaskBoardV3MutationResult SetCriterionState(
        string taskId,
        string criterionId,
        string state,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        if (state is not ("Open" or "Passed" or "Failed"))
        {
            throw new InvalidOperationException($"Acceptance criterion state '{state}' is not supported.");
        }

        return MutateCriterion(taskId, criterionId, expectedRevision, actorId, now, dryRun, criterion =>
        {
            if (state == "Passed" && RequiredArray(criterion, "evidence").Count == 0)
            {
                throw new InvalidOperationException("Passed acceptance criterion requires at least one evidence link.");
            }

            criterion["state"] = state;
        });
    }

    public TaskBoardV3MutationResult AddCriterionEvidence(
        string taskId,
        string criterionId,
        string kind,
        string value,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        var field = kind switch
        {
            "File" => "path",
            "Uri" => "uri",
            "Attachment" => "attachmentId",
            _ => throw new InvalidOperationException($"Evidence kind '{kind}' is not supported.")
        };
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return MutateCriterion(taskId, criterionId, expectedRevision, actorId, now, dryRun, criterion =>
        {
            var evidence = RequiredArray(criterion, "evidence");
            var candidate = new JsonObject { ["kind"] = kind, [field] = value };
            if (evidence.Any(item => JsonNode.DeepEquals(item, candidate)))
            {
                throw new InvalidOperationException("The same evidence link is already attached to this criterion.");
            }

            evidence.Add(candidate);
        });
    }

    public TaskBoardV3MutationResult RemoveCriterion(
        string taskId,
        string criterionId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, task =>
        {
            var criteria = RequiredArray(task, "acceptanceCriteria");
            var criterion = criteria.Select(RequiredObject).SingleOrDefault(item =>
                RequiredString(item, "criterionId") == criterionId) ??
                throw new FileNotFoundException($"Acceptance criterion '{criterionId}' was not found.");
            criteria.Remove(criterion);
        });
    }

    public TaskBoardV3MutationResult AddDependency(
        string taskId,
        string dependencyTaskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTaskWithSnapshot(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, (task, snapshot) =>
        {
            var target = FindTask(snapshot, dependencyTaskId);
            var targetUid = RequiredString(target, "taskUid");
            var relations = RequiredArray(task, "relations");
            if (relations.Select(RequiredObject).Any(relation =>
                RequiredString(relation, "kind") == "DependsOn" &&
                RequiredString(relation, "targetTaskUid") == targetUid))
            {
                throw new InvalidOperationException($"Task '{taskId}' already depends on '{dependencyTaskId}'.");
            }

            relations.Add(new JsonObject
            {
                ["relationId"] = NextChildId(relations.Select(RequiredObject), "relationId", "relation"),
                ["kind"] = "DependsOn",
                ["targetTaskUid"] = targetUid
            });
        });
    }

    public TaskBoardV3MutationResult RemoveDependency(
        string taskId,
        string dependencyTaskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTaskWithSnapshot(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, (task, snapshot) =>
        {
            var targetUid = RequiredString(FindTask(snapshot, dependencyTaskId), "taskUid");
            var relations = RequiredArray(task, "relations");
            var relation = relations.Select(RequiredObject).SingleOrDefault(item =>
                RequiredString(item, "kind") == "DependsOn" &&
                RequiredString(item, "targetTaskUid") == targetUid) ??
                throw new FileNotFoundException($"Dependency '{dependencyTaskId}' was not found on '{taskId}'.");
            relations.Remove(relation);
        });
    }

    public TaskBoardV3MutationResult SetParent(
        string taskId,
        string parentTaskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTaskWithSnapshot(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, (task, snapshot) =>
            task["parentTaskUid"] = RequiredString(FindTask(snapshot, parentTaskId), "taskUid"));
    }

    public TaskBoardV3MutationResult ClearParent(
        string taskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, task =>
            task["parentTaskUid"] = null);
    }

    public TaskBoardV3MutationResult SetStatus(
        string taskId,
        string targetStatus,
        long expectedRevision,
        string actorId,
        string actorKind,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        if (targetStatus is not ("Ready" or "InProgress" or "Blocked" or "Review" or "Cancelled"))
        {
            throw new InvalidOperationException($"Task status '{targetStatus}' is not supported by set-status; use accept for Done.");
        }

        return MutateTaskWithSnapshot(taskId, expectedRevision, actorId, now, dryRun,
            TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus,
            (task, snapshot) =>
            {
                if (targetStatus is "InProgress" or "Review" && HasUnfinishedDependencies(task, snapshot))
                {
                    throw new InvalidOperationException("Task cannot enter an active workflow state while it has unfinished dependencies.");
                }

                if (targetStatus == "InProgress")
                {
                    WorkspaceChangesBuilderV3.EnsureBaseline(projectRoot, task, actorId, actorKind, now);
                }
                else if (targetStatus == "Review")
                {
                    WorkspaceChangesBuilderV3.Refresh(projectRoot, task, actorId, actorKind, now);
                }

                ApplyStatus(task, targetStatus, actorId, actorKind, reason, now);
            },
            actorKind: actorKind);
    }

    public TaskBoardV3MutationResult Submit(
        string taskId,
        long expectedRevision,
        string actorId,
        string actorKind,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(taskId, expectedRevision, actorId, now, dryRun,
            TaskBoardV3Capability.EditTask | TaskBoardV3Capability.SubmitForAcceptance,
            task =>
            {
                if (RequiredString(task, "status") != "Review")
                {
                    throw new InvalidOperationException("Only a task in Review can be submitted for acceptance.");
                }

                WorkspaceChangesBuilderV3.Refresh(projectRoot, task, actorId, actorKind, now);

                task["acceptanceState"] = "Submitted";
                task["submittedAt"] = FormatDate(now);
                AppendStatusActivity(task, "Review", "Review", actorId, actorKind, reason, now);
            },
            actorKind: actorKind);
    }

    public TaskBoardV3MutationResult Accept(
        string taskId,
        long expectedRevision,
        string humanActorId,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(taskId, expectedRevision, humanActorId, now, dryRun,
            TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.AcceptanceDecision,
            task =>
            {
                if (RequiredString(task, "status") != "Review" || RequiredString(task, "acceptanceState") != "Submitted")
                {
                    throw new InvalidOperationException("Only a submitted Review task can be accepted.");
                }

                if (RequiredArray(task, "acceptanceCriteria").Select(RequiredObject)
                    .Any(criterion => RequiredString(criterion, "state") != "Passed" || RequiredArray(criterion, "evidence").Count == 0))
                {
                    throw new InvalidOperationException("All acceptance criteria must be Passed before acceptance.");
                }

                WorkspaceChangesBuilderV3.Refresh(projectRoot, task, humanActorId, "Human", now);

                task["status"] = "Done";
                task["acceptanceState"] = "Accepted";
                task["completedAt"] = FormatDate(now);
                task["acceptedAt"] = FormatDate(now);
                task["acceptedBy"] = humanActorId;
                var activitySequence = TaskActivitySequenceV3.Next(task);
                TaskActivitySequenceV3.Append(task, new JsonObject
                {
                    ["activityEntryId"] = $"activity-{Guid.NewGuid():N}",
                    ["sequence"] = activitySequence,
                    ["actorId"] = humanActorId,
                    ["actorKind"] = "Human",
                    ["createdAt"] = FormatDate(now),
                    ["kind"] = "AcceptanceResult",
                    ["payload"] = new JsonObject
                    {
                        ["decision"] = "Accepted",
                        ["reason"] = reason,
                        ["authorityActorId"] = humanActorId,
                        ["authorityRole"] = "Owner",
                        ["auditRunId"] = AcceptanceAuditRunId(task)
                    }
                });
            },
            TaskBoardV3Role.Owner,
            "Human");
    }

    public TaskBoardV3MutationResult RequestChanges(
        string taskId,
        long expectedRevision,
        string humanActorId,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(taskId, expectedRevision, humanActorId, now, dryRun,
            TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.AcceptanceDecision,
            task =>
            {
                if (RequiredString(task, "status") != "Review" || RequiredString(task, "acceptanceState") != "Submitted")
                {
                    throw new InvalidOperationException("Only a submitted Review task can be returned for changes.");
                }

                task["status"] = "InProgress";
                task["acceptanceState"] = "ChangesRequested";
                task["submittedAt"] = null;
                var activitySequence = TaskActivitySequenceV3.Next(task);
                TaskActivitySequenceV3.Append(task, new JsonObject
                {
                    ["activityEntryId"] = $"activity-{Guid.NewGuid():N}",
                    ["sequence"] = activitySequence,
                    ["actorId"] = humanActorId,
                    ["actorKind"] = "Human",
                    ["createdAt"] = FormatDate(now),
                    ["kind"] = "AcceptanceResult",
                    ["payload"] = new JsonObject
                    {
                        ["decision"] = "ChangesRequested",
                        ["reason"] = reason,
                        ["authorityActorId"] = humanActorId,
                        ["authorityRole"] = "Owner",
                        ["auditRunId"] = AcceptanceAuditRunId(task)
                    }
                });
            },
            TaskBoardV3Role.Owner,
            "Human");
    }

    internal TaskBoardV3MutationResult AcceptAsAuditor(
        string taskId,
        long expectedRevision,
        TaskBoardV3MutationContext trustedAuditor,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentNullException.ThrowIfNull(trustedAuditor);
        if (trustedAuditor.Role != TaskBoardV3Role.Auditor ||
            !trustedAuditor.Has(TaskBoardV3Capability.AcceptanceDecision))
        {
            throw new InvalidOperationException("Trusted auditor acceptance requires Auditor role and AcceptanceDecision capability.");
        }

        return MutateTask(
            taskId,
            expectedRevision,
            trustedAuditor.ActorId,
            now,
            dryRun,
            trustedAuditor.Capabilities,
            task =>
            {
                if (RequiredString(task, "status") != "Review" || RequiredString(task, "acceptanceState") != "Submitted")
                {
                    throw new InvalidOperationException("Only a submitted Review task can be accepted.");
                }

                if (RequiredArray(task, "acceptanceCriteria").Select(RequiredObject)
                    .Any(criterion => RequiredString(criterion, "state") != "Passed" || RequiredArray(criterion, "evidence").Count == 0))
                {
                    throw new InvalidOperationException("All acceptance criteria must be Passed with evidence before acceptance.");
                }

                task["status"] = "Done";
                task["acceptanceState"] = "Accepted";
                task["completedAt"] = FormatDate(now);
                task["acceptedAt"] = FormatDate(now);
                task["acceptedBy"] = trustedAuditor.ActorId;
                var activitySequence = TaskActivitySequenceV3.Next(task);
                TaskActivitySequenceV3.Append(task, new JsonObject
                {
                    ["activityEntryId"] = $"activity-{Guid.NewGuid():N}",
                    ["sequence"] = activitySequence,
                    ["actorId"] = trustedAuditor.ActorId,
                    ["actorKind"] = trustedAuditor.ActorKind,
                    ["createdAt"] = FormatDate(now),
                    ["kind"] = "AcceptanceResult",
                    ["payload"] = new JsonObject
                    {
                        ["decision"] = "Accepted",
                        ["reason"] = reason,
                        ["authorityActorId"] = trustedAuditor.ActorId,
                        ["authorityRole"] = "Auditor",
                        ["auditRunId"] = AcceptanceAuditRunId(task)
                    }
                });
            },
            trustedAuditor.Role,
            trustedAuditor.ActorKind);
    }

    public TaskBoardV3MutationResult Reopen(
        string taskId,
        long expectedRevision,
        string actorId,
        string actorKind,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTaskWithSnapshot(taskId, expectedRevision, actorId, now, dryRun,
            TaskBoardV3Capability.EditTask | TaskBoardV3Capability.ChangeStatus | TaskBoardV3Capability.TrustedReopen,
            (task, snapshot) =>
            {
                if (!snapshot.ActiveTasks.Any(item => ReferenceEquals(item, FindTask(snapshot, taskId))))
                {
                    throw new InvalidOperationException("Archived task must be unarchived before reopen.");
                }

                var previous = RequiredString(task, "status");
                if (previous is not ("Done" or "Cancelled"))
                {
                    throw new InvalidOperationException("Only a terminal task can be reopened.");
                }

                task["status"] = "Ready";
                task["acceptanceState"] = "NotSubmitted";
                task["submittedAt"] = null;
                task["completedAt"] = null;
                task["acceptedAt"] = null;
                task["acceptedBy"] = null;
                task["cancelledAt"] = null;
                task["cancellationReason"] = null;
                task["archivedAt"] = null;
                task["archivedBy"] = null;
                AppendStatusActivity(task, previous, "Ready", actorId, actorKind, reason, now);
            },
            actorKind: actorKind);
    }

    public TaskBoardV3MutationResult Archive(
        string taskId,
        long expectedRevision,
        long expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return ChangeArchiveState(taskId, expectedRevision, expectedBoardRevision, actorId, now, archive: true, dryRun);
    }

    public TaskBoardV3MutationResult Unarchive(
        string taskId,
        long expectedRevision,
        long expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return ChangeArchiveState(taskId, expectedRevision, expectedBoardRevision, actorId, now, archive: false, dryRun);
    }

    public TaskBoardV3MutationResult AddAttachment(
        string taskId,
        string sourceFilePath,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFilePath);
        var sourcePath = Path.GetFullPath(Path.IsPathRooted(sourceFilePath)
            ? sourceFilePath
            : Path.Combine(projectRoot, sourceFilePath));
        if (!File.Exists(sourcePath))
        {
            throw new InvalidOperationException("Attachment source must be an existing regular file.");
        }

        EnsureNoSourceReparsePoints(sourcePath);

        var bytes = File.ReadAllBytes(sourcePath);
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var previous = FindTask(snapshot, taskId);
        var taskRevision = RequiredLong(previous, "revision");
        if (taskRevision != expectedRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Task revision conflict while adding attachment: expected {expectedRevision}, actual {taskRevision}.",
                isRetryable: false,
                actualTaskRevision: taskRevision);
        }

        var policy = RequiredObject(snapshot.Board["attachmentPolicy"]);
        var perFileLimit = RequiredLong(policy, "perFileByteLimit");
        var perTaskLimit = RequiredLong(policy, "perTaskByteLimit");
        var boardLimit = RequiredLong(policy, "boardByteLimit");
        if (bytes.LongLength > perFileLimit)
        {
            throw new InvalidOperationException("Attachment exceeds the current board per-file limit.");
        }

        var boardBytes = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks)
            .SelectMany(task => RequiredArray(task, "attachments").Select(RequiredObject))
            .Sum(attachment => RequiredLong(attachment, "byteLength") +
                RequiredArray(attachment, "derivatives").Select(RequiredObject).Sum(ReadyDerivativeLength));
        if (checked(boardBytes + bytes.LongLength) > boardLimit)
        {
            throw new InvalidOperationException("Attachment exceeds the current board-wide limit.");
        }

        var next = previous.DeepClone().AsObject();
        var attachments = RequiredArray(next, "attachments");
        var taskBytes = attachments.Select(RequiredObject).Sum(attachment => RequiredLong(attachment, "byteLength") +
            RequiredArray(attachment, "derivatives").Select(RequiredObject).Sum(ReadyDerivativeLength));
        if (checked(taskBytes + bytes.LongLength) > perTaskLimit)
        {
            throw new InvalidOperationException("Attachment exceeds the current per-task limit.");
        }

        var attachmentId = NextChildId(attachments.Select(RequiredObject), "attachmentId", "A");
        var displayName = Path.GetFileName(sourcePath);
        if (string.IsNullOrWhiteSpace(displayName) || displayName is "." or ".." || displayName.Any(char.IsControl))
        {
            throw new InvalidOperationException("Attachment file name is not safe.");
        }

        var sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var safeName = sha256 + Path.GetExtension(displayName).ToLowerInvariant();
        var relativePath = $".taskboard/attachments/{RequiredString(next, "taskUid")}/{attachmentId}/{safeName}";
        attachments.Add(new JsonObject
        {
            ["attachmentId"] = attachmentId,
            ["displayName"] = displayName,
            ["relativePath"] = relativePath,
            ["mediaType"] = InferMediaType(safeName),
            ["byteLength"] = bytes.LongLength,
            ["sha256"] = sha256,
            ["addedAt"] = FormatDate(now),
            ["addedBy"] = actorId,
            ["derivatives"] = CreatePendingDerivativeSlots(sha256, now)
        });
        next["revision"] = expectedRevision + 1;
        next["updatedAt"] = FormatDate(now);
        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext(actorId, TaskBoardV3Capability.EditTask));
        var active = Replace(snapshot.ActiveTasks, previous, next);
        var completed = Replace(snapshot.CompletedTasks, previous, next);
        TaskBoardV3SemanticValidator.Validate(projectRoot, snapshot.Board, active, completed, validateAttachmentBlobs: false);
        var taskPath = snapshot.ActiveTasks.Contains(previous)
            ? ProjectTaskStorage.GetTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var changes = new[]
        {
            new TaskBoardBinaryChange(taskPath, Utf8(TaskBoardV3Migration.Serialize(next))),
            new TaskBoardBinaryChange(relativePath, bytes)
        };
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
            TaskBoardV3SemanticValidator.Validate(projectRoot, snapshot.Board, active, completed);
        }

        return new TaskBoardV3MutationResult(next, snapshot.Board, active, completed, dryRun ? [] : changes.Select(change => change.Path).ToArray(), dryRun);
    }

    public TaskBoardV3MutationResult RemoveAttachment(
        string taskId,
        string attachmentId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId);
        throw new InvalidOperationException("TaskBoard v3 original attachments are lossless and cannot be removed; append a superseding message or attachment instead.");
    }

    public TaskBoardV3MutationResult SetAttachmentPreview(
        string taskId,
        string? attachmentId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, task =>
        {
            if (attachmentId is not null)
            {
                var attachment = RequiredArray(task, "attachments").Select(RequiredObject).SingleOrDefault(item =>
                    RequiredString(item, "attachmentId") == attachmentId) ??
                    throw new FileNotFoundException($"Attachment '{attachmentId}' was not found.");
                if (!IsRasterMediaType(RequiredString(attachment, "mediaType")))
                {
                    throw new InvalidOperationException("Only a raster image attachment can be selected as card preview.");
                }
            }

            task["previewAttachmentId"] = attachmentId;
        });
    }

    private TaskBoardV3MutationResult ChangeArchiveState(
        string taskId,
        long expectedRevision,
        long expectedBoardRevision,
        string actorId,
        DateTimeOffset now,
        bool archive,
        bool dryRun)
    {
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var sourceTasks = archive ? snapshot.ActiveTasks : snapshot.CompletedTasks;
        var previous = sourceTasks.SingleOrDefault(task => RequiredString(task, "taskId") == taskId) ??
            throw new FileNotFoundException($"Task '{taskId}' was not found in the expected archive state.");
        if (RequiredString(previous, "status") is not ("Done" or "Cancelled"))
        {
            throw new InvalidOperationException("Only Done or Cancelled tasks can change archive state.");
        }

        var taskRevision = RequiredLong(previous, "revision");
        if (taskRevision != expectedRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Task revision conflict during archive mutation: expected {expectedRevision}, actual {taskRevision}.",
                isRetryable: false,
                actualTaskRevision: taskRevision);
        }

        var boardRevision = RequiredLong(snapshot.Board, "revision");
        if (boardRevision != expectedBoardRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict during archive mutation: expected {expectedBoardRevision}, actual {boardRevision}.",
                isRetryable: false,
                actualBoardRevision: boardRevision);
        }

        var next = previous.DeepClone().AsObject();
        next["revision"] = expectedRevision + 1;
        next["updatedAt"] = FormatDate(now);
        next["archivedAt"] = archive ? FormatDate(now) : null;
        next["archivedBy"] = archive ? actorId : null;
        TaskBoardV3TransitionValidator.ValidateTask(
            previous,
            next,
            new TaskBoardV3MutationContext(actorId, TaskBoardV3Capability.EditTask | TaskBoardV3Capability.Archive));

        var board = snapshot.Board.DeepClone().AsObject();
        board["revision"] = expectedBoardRevision + 1;
        var placements = RequiredArray(board, "placements");
        if (archive)
        {
            var placement = placements.Select(RequiredObject).Single(item =>
                RequiredString(item, "taskUid") == RequiredString(previous, "taskUid"));
            placements.Remove(placement);
        }
        else
        {
            placements.Add(new JsonObject
            {
                ["taskUid"] = RequiredString(previous, "taskUid"),
                ["groupId"] = null,
                ["rank"] = NextRootRank(placements)
            });
        }

        TaskBoardV3TransitionValidator.ValidateBoard(
            snapshot.Board,
            board,
            new TaskBoardV3MutationContext(actorId, TaskBoardV3Capability.EditBoard));
        var active = archive
            ? snapshot.ActiveTasks.Where(task => !ReferenceEquals(task, previous)).ToArray()
            : snapshot.ActiveTasks.Concat([next]).ToArray();
        var completed = archive
            ? snapshot.CompletedTasks.Concat([next]).ToArray()
            : snapshot.CompletedTasks.Where(task => !ReferenceEquals(task, previous)).ToArray();
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, active, completed);
        var sourcePath = archive
            ? ProjectTaskStorage.GetTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var targetPath = archive
            ? ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetTaskDocumentPath(taskId);
        var changes = new[]
        {
            new TaskBoardBinaryChange(ProjectTaskStorage.BoardDocumentPath, Utf8(TaskBoardV3Migration.Serialize(board))),
            new TaskBoardBinaryChange(sourcePath, null),
            new TaskBoardBinaryChange(targetPath, Utf8(TaskBoardV3Migration.Serialize(next)))
        };
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
        }

        return new TaskBoardV3MutationResult(next, board, active, completed, dryRun ? [] : changes.Select(change => change.Path).ToArray(), dryRun);
    }

    private TaskBoardV3MutationResult MutateCriterion(
        string taskId,
        string criterionId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun,
        Action<JsonObject> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        return MutateTask(taskId, expectedRevision, actorId, now, dryRun, TaskBoardV3Capability.EditTask, task =>
        {
            var criterion = RequiredArray(task, "acceptanceCriteria").Select(RequiredObject).SingleOrDefault(item =>
                RequiredString(item, "criterionId") == criterionId) ??
                throw new FileNotFoundException($"Acceptance criterion '{criterionId}' was not found.");
            mutation(criterion);
        });
    }

    private TaskBoardV3MutationResult MutateTask(
        string taskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun,
        TaskBoardV3Capability capabilities,
        Action<JsonObject> mutation,
        TaskBoardV3Role role = TaskBoardV3Role.Worker,
        string actorKind = "Agent")
    {
        return MutateTaskWithSnapshot(
            taskId,
            expectedRevision,
            actorId,
            now,
            dryRun,
            capabilities,
            (task, _) => mutation(task),
            role,
            actorKind);
    }

    private TaskBoardV3BoardMutationResult MutateBoard(
        long expectedBoardRevision,
        bool dryRun,
        Action<JsonObject> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var revision = RequiredLong(snapshot.Board, "revision");
        if (revision != expectedBoardRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Taskboard revision conflict: expected {expectedBoardRevision}, actual {revision}.",
                isRetryable: false,
                actualBoardRevision: revision);
        }

        var board = snapshot.Board.DeepClone().AsObject();
        mutation(board);
        board["revision"] = revision + 1;
        TaskBoardV3TransitionValidator.ValidateBoard(
            snapshot.Board,
            board,
            new TaskBoardV3MutationContext("cli", TaskBoardV3Capability.EditBoard));
        TaskBoardV3SemanticValidator.Validate(projectRoot, board, snapshot.ActiveTasks, snapshot.CompletedTasks);
        var change = new TaskBoardBinaryChange(
            ProjectTaskStorage.BoardDocumentPath,
            Utf8(TaskBoardV3Migration.Serialize(board)));
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction([change]);
        }

        return new TaskBoardV3BoardMutationResult(
            board,
            snapshot.ActiveTasks,
            snapshot.CompletedTasks,
            dryRun ? [] : [change.Path],
            dryRun);
    }

    private TaskBoardV3MutationResult MutateTaskWithSnapshot(
        string taskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun,
        TaskBoardV3Capability capabilities,
        Action<JsonObject, TaskBoardV3Snapshot> mutation,
        TaskBoardV3Role role = TaskBoardV3Role.Worker,
        string actorKind = "Agent")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentNullException.ThrowIfNull(mutation);
        var transactions = CreateTransactionStore();
        using var writeLock = transactions.AcquireWriteLock();
        transactions.RecoverPendingTransactions();
        var snapshot = LoadSnapshotCore();
        var previous = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).SingleOrDefault(task =>
            string.Equals(RequiredString(task, "taskId"), taskId, StringComparison.Ordinal)) ??
            throw new FileNotFoundException($"Task '{taskId}' was not found.");
        var revision = RequiredLong(previous, "revision");
        if (revision != expectedRevision)
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0006",
                $"Task revision conflict: expected {expectedRevision}, actual {revision}.",
                isRetryable: false,
                actualTaskRevision: revision);
        }

        var next = previous.DeepClone().AsObject();
        mutation(next, snapshot);
        TaskPatchV3.AppendIfRequired(previous, next, actorId, actorKind, now);
        next["revision"] = revision + 1;
        next["updatedAt"] = FormatDate(now);
        FinalizeAppendedContextCheckpoints(previous, next);
        var context = new TaskBoardV3MutationContext(
            actorId,
            capabilities | TaskBoardV3Capability.EditTask,
            role,
            actorKind,
            ExpectedRevision: expectedRevision,
            ExpectedLastMessageSequence: RequiredLong(RequiredObject(previous["conversation"]), "lastMessageSequence"),
            ExpectedLastActivitySequence: RequiredLong(previous, "lastActivitySequence"));
        TaskBoardV3TransitionValidator.ValidateTask(previous, next, context);

        var active = Replace(snapshot.ActiveTasks, previous, next);
        var completed = Replace(snapshot.CompletedTasks, previous, next);
        TaskBoardV3SemanticValidator.Validate(projectRoot, snapshot.Board, active, completed);
        var activeTask = snapshot.ActiveTasks.Contains(previous);
        var relativePath = activeTask
            ? ProjectTaskStorage.GetTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var changes = new[]
        {
            new TaskBoardBinaryChange(relativePath, Utf8(TaskBoardV3Migration.Serialize(next)))
        };
        if (!dryRun)
        {
            transactions.ApplyBinaryTransaction(changes);
        }

        return new TaskBoardV3MutationResult(
            next,
            snapshot.Board,
            active,
            completed,
            dryRun ? [] : [relativePath],
            dryRun);
    }

    private static JsonObject FindTask(TaskBoardV3Snapshot snapshot, string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        return snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).SingleOrDefault(task =>
            string.Equals(RequiredString(task, "taskId"), taskId, StringComparison.Ordinal)) ??
            throw new FileNotFoundException($"Task '{taskId}' was not found.");
    }

    private static bool HasUnfinishedDependencies(JsonObject task, TaskBoardV3Snapshot snapshot)
    {
        var byUid = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks)
            .ToDictionary(candidate => RequiredString(candidate, "taskUid"), StringComparer.Ordinal);
        return RequiredArray(task, "relations").Select(RequiredObject)
            .Where(relation => RequiredString(relation, "kind") == "DependsOn")
            .Select(relation => byUid[RequiredString(relation, "targetTaskUid")])
            .Any(dependency => RequiredString(dependency, "status") != "Done");
    }

    private void AddCanonicalChange(List<TaskBoardBinaryChange> changes, string relativePath, JsonObject value)
    {
        var canonical = Utf8(TaskBoardV3Migration.Serialize(value));
        var fullPath = Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath) || !File.ReadAllBytes(fullPath).AsSpan().SequenceEqual(canonical))
        {
            changes.Add(new TaskBoardBinaryChange(relativePath, canonical));
        }
    }

    private static long ReadyDerivativeLength(JsonObject derivative)
    {
        return RequiredString(derivative, "status") == "Ready" ? RequiredLong(derivative, "byteLength") : 0;
    }

    private static void FinalizeAppendedContextCheckpoints(JsonObject previous, JsonObject next)
    {
        var previousConversation = RequiredObject(previous["conversation"]);
        var nextConversation = RequiredObject(next["conversation"]);
        var previousCount = RequiredArray(previousConversation, "contextCheckpoints").Count;
        var checkpoints = RequiredArray(nextConversation, "contextCheckpoints").Skip(previousCount).Select(RequiredObject).ToArray();
        if (checkpoints.Length == 0)
        {
            return;
        }

        foreach (var checkpoint in checkpoints)
        {
            checkpoint["taskRevision"] = RequiredLong(next, "revision");
            checkpoint["lastMessageSequence"] = RequiredLong(nextConversation, "lastMessageSequence");
            checkpoint["lastActivitySequence"] = RequiredLong(next, "lastActivitySequence");
        }

        var digest = AgentContextBuilderV3.ComputeDigest(next);
        foreach (var checkpoint in checkpoints)
        {
            checkpoint["contextDigest"] = digest;
        }
    }

    private static string? AcceptanceAuditRunId(JsonObject task)
    {
        var audit = RequiredObject(RequiredObject(task["executionContract"])["externalAudit"]);
        var mode = RequiredString(audit, "mode");
        if (mode == "None")
        {
            return null;
        }

        var requiredStage = mode == "PrimaryControl" ? "Control" : "Primary";
        return RequiredArray(task, "auditRuns").Select(RequiredObject).LastOrDefault(run =>
            RequiredString(run, "stage") == requiredStage && RequiredString(run, "decision") == "Accepted")?["runId"]?.GetValue<string>();
    }

    private static JsonArray CreatePendingDerivativeSlots(string sourceSha256, DateTimeOffset createdAt)
    {
        return new JsonArray(new[] { "ExtractedText", "Ocr", "Preview" }.Select(kind => (JsonNode)new JsonObject
        {
            ["derivativeId"] = $"derivative-{kind.ToLowerInvariant()}",
            ["kind"] = kind,
            ["status"] = "Pending",
            ["failureReason"] = null,
            ["relativePath"] = null,
            ["mediaType"] = null,
            ["byteLength"] = null,
            ["sha256"] = null,
            ["sourceSha256"] = sourceSha256,
            ["extractor"] = null,
            ["createdAt"] = FormatDate(createdAt)
        }).ToArray());
    }

    private static string NextChildId(IEnumerable<JsonObject> values, string propertyName, string prefix)
    {
        var existing = values.Select(value => RequiredString(value, propertyName)).ToHashSet(StringComparer.Ordinal);
        for (var number = 1; number < int.MaxValue; number++)
        {
            var candidate = $"{prefix}-{number:D4}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"No {prefix} identifiers remain.");
    }

    private static void ApplyStatus(
        JsonObject task,
        string targetStatus,
        string actorId,
        string actorKind,
        string reason,
        DateTimeOffset now)
    {
        var previousStatus = RequiredString(task, "status");
        if (targetStatus == "Blocked")
        {
            var blockers = RequiredArray(task, "blockers");
            if (!blockers.Select(RequiredObject).Any(blocker => RequiredString(blocker, "state") == "Active"))
            {
                blockers.Add(new JsonObject
                {
                    ["blockerId"] = NextChildId(blockers.Select(RequiredObject), "blockerId", "blocker"),
                    ["kind"] = "Manual",
                    ["reason"] = reason,
                    ["state"] = "Active",
                    ["createdAt"] = FormatDate(now),
                    ["createdBy"] = actorId,
                    ["resolvedAt"] = null,
                    ["resolvedBy"] = null
                });
            }
        }
        else if (previousStatus == "Blocked")
        {
            foreach (var blocker in RequiredArray(task, "blockers").Select(RequiredObject)
                .Where(blocker => RequiredString(blocker, "state") == "Active"))
            {
                blocker["state"] = "Resolved";
                blocker["resolvedAt"] = FormatDate(now);
                blocker["resolvedBy"] = actorId;
            }
        }

        task["status"] = targetStatus;
        task["submittedAt"] = null;
        task["completedAt"] = null;
        task["acceptedAt"] = null;
        task["acceptedBy"] = null;
        task["cancelledAt"] = null;
        task["cancellationReason"] = null;
        task["archivedAt"] = null;
        task["archivedBy"] = null;
        task["acceptanceState"] = targetStatus switch
        {
            "Review" => "InternalReview",
            "Cancelled" => "Cancelled",
            _ => RequiredString(task, "acceptanceState") == "ChangesRequested" ? "ChangesRequested" : "NotSubmitted"
        };
        if (targetStatus == "Cancelled")
        {
            task["cancelledAt"] = FormatDate(now);
            task["cancellationReason"] = reason;
        }

        AppendStatusActivity(task, previousStatus, targetStatus, actorId, actorKind, reason, now);
    }

    private static void AppendStatusActivity(
        JsonObject task,
        string previous,
        string next,
        string actorId,
        string actorKind,
        string reason,
        DateTimeOffset now)
    {
        var sequence = TaskActivitySequenceV3.Next(task);
        TaskActivitySequenceV3.Append(task, new JsonObject
        {
            ["activityEntryId"] = $"activity-{Guid.NewGuid():N}",
            ["sequence"] = sequence,
            ["actorId"] = actorId,
            ["actorKind"] = actorKind,
            ["createdAt"] = FormatDate(now),
            ["kind"] = "StatusChange",
            ["payload"] = new JsonObject
            {
                ["previous"] = previous,
                ["next"] = next,
                ["reason"] = reason
            }
        });
    }

    private static IReadOnlyList<JsonObject> Replace(
        IReadOnlyList<JsonObject> source,
        JsonObject previous,
        JsonObject next)
    {
        return source.Select(item => ReferenceEquals(item, previous) ? next : item).ToArray();
    }

    private static string NextRootRank(JsonArray placements)
    {
        var maximum = placements.Select(RequiredObject)
            .Where(placement => placement["groupId"] is null)
            .Select(placement => long.Parse(RequiredString(placement, "rank"), CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();
        var next = checked(maximum + 1000);
        if (next > 999_999_999_999L)
        {
            throw new InvalidOperationException("Root placement ranks are exhausted; rebalance is required.");
        }

        return next.ToString("D12", CultureInfo.InvariantCulture);
    }

    private static string NormalizeRank(string rank)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rank);
        if (rank.Length > 12 || rank.Any(character => character is < '0' or > '9'))
        {
            throw new InvalidOperationException("Placement rank must contain at most 12 decimal digits.");
        }

        return rank.PadLeft(12, '0');
    }

    private static string NextGroupRank(JsonArray groups, string? parentGroupId)
    {
        var maximum = groups.Select(RequiredObject)
            .Where(group => string.Equals(NullableString(group, "parentGroupId"), parentGroupId, StringComparison.Ordinal))
            .Select(group => long.Parse(RequiredString(group, "rank"), CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();
        return checked(maximum + 1000).ToString("D12", CultureInfo.InvariantCulture);
    }

    private static string NormalizeTagColor(string color)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(color);
        foreach (var legacy in new[] { "Gray", "Blue", "Green", "Yellow", "Orange", "Red", "Purple" })
        {
            if (string.Equals(color, legacy, StringComparison.OrdinalIgnoreCase))
            {
                return legacy;
            }
        }

        if (Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant))
        {
            return color.ToUpperInvariant();
        }

        throw new InvalidOperationException($"Taskboard tag color '{color}' is not supported; use a legacy name or #RRGGBB.");
    }

    private static byte[] Utf8(string text)
    {
        return new UTF8Encoding(false).GetBytes(text);
    }

    private static string InferMediaType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static bool IsRasterMediaType(string mediaType)
    {
        return mediaType.ToLowerInvariant() is "image/png" or "image/jpeg" or "image/gif" or "image/webp" or "image/bmp";
    }

    private static void EnsureNoSourceReparsePoints(string sourcePath)
    {
        if ((File.GetAttributes(sourcePath) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidOperationException("Attachment source cannot be a reparse point.");
        }

        var directory = new FileInfo(sourcePath).Directory;
        while (directory is not null)
        {
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidOperationException("Attachment source path cannot traverse a reparse point.");
            }

            directory = directory.Parent;
        }
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToString("O", CultureInfo.InvariantCulture);
    }
}
