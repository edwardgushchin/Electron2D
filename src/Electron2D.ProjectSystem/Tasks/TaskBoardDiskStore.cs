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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal sealed record TaskBoardCreateRequest(
    string Title,
    string Description,
    string Priority,
    DateOnly? Deadline,
    string ActorId,
    DateTimeOffset CreatedAt,
    bool DryRun);

internal sealed record TaskBoardCreateResult(
    ProjectTask Task,
    TaskBoard Board,
    IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardTaskMutationResult(
    ProjectTask Task,
    IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardVerificationResult(
    int ActiveTaskCount,
    int CompletedTaskCount);

internal sealed record TaskBoardNormalizationResult(
    TaskBoard Board,
    IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardMutationResult(
    TaskBoard Board,
    IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardTagMutationResult(
    TaskBoard Board,
    ProjectTask? Task,
    TaskBoardTag? Tag,
    IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardFileChange(string Path, string? Text);

internal sealed record TaskBoardBinaryChange(string Path, byte[]? Content);

internal sealed record TaskBoardOperationIdentity(
    string OperationId,
    string Command,
    string Fingerprint);

internal sealed record TaskBoardWriteOptions(
    TimeSpan LockTimeout,
    TimeSpan InitialBackoff,
    TimeSpan MaximumBackoff,
    CancellationToken CancellationToken = default,
    TaskBoardOperationIdentity? Operation = null)
{
    public static TaskBoardWriteOptions Default { get; } = new(
        TimeSpan.FromSeconds(10),
        TimeSpan.FromMilliseconds(25),
        TimeSpan.FromMilliseconds(250));
}

internal class TaskBoardWriteException : InvalidOperationException
{
    public TaskBoardWriteException(
        string code,
        string message,
        bool isRetryable,
        long? actualTaskRevision = null,
        long? actualBoardRevision = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        IsRetryable = isRetryable;
        ActualTaskRevision = actualTaskRevision;
        ActualBoardRevision = actualBoardRevision;
    }

    public string Code { get; }

    public bool IsRetryable { get; }

    public long? ActualTaskRevision { get; }

    public long? ActualBoardRevision { get; }
}

internal sealed class TaskBoardOperationReplayedException : TaskBoardWriteException
{
    public TaskBoardOperationReplayedException(
        string taskId,
        long? taskRevision,
        long? boardRevision,
        IReadOnlyList<string> changedFiles)
        : base(
            "E2D-TASK-OPERATION-REPLAYED",
            $"Taskboard operation for '{taskId}' was already committed.",
            isRetryable: false,
            taskRevision,
            boardRevision)
    {
        TaskId = taskId;
        ChangedFiles = changedFiles;
    }

    public string TaskId { get; }

    public IReadOnlyList<string> ChangedFiles { get; }
}

internal sealed record TaskBoardDeleteResult(
    string TaskId,
    TaskBoard Board,
    IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardMigrationApplyResult(IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardMigrationFinalizeResult(IReadOnlyList<string> ChangedFiles);

internal sealed record TaskBoardV3MigrationApplyResult(
    IReadOnlyList<string> ChangedFiles,
    string ReportSha256,
    long SourceBoardRevision,
    bool DryRun);

internal sealed partial class TaskBoardDiskStore
{
    internal const string TaskboardGitIgnoreText = "/.lock\n/.staging/\n/.transactions/\n/.operations/\n/.cache/\n";

    private readonly string projectRoot;
    private readonly TaskBoardWriteOptions writeOptions;

    public TaskBoardDiskStore(string projectRoot, TaskBoardWriteOptions? writeOptions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        this.projectRoot = Path.GetFullPath(projectRoot);
        this.writeOptions = writeOptions ?? TaskBoardWriteOptions.Default;
        ValidateWriteOptions(this.writeOptions);
        if (Directory.Exists(FullPath(ProjectTaskStorage.RootDirectory)))
        {
            using var writeLock = AcquireWriteLock();
            RecoverTransactions();
        }
    }

    public TaskBoard LoadBoard()
    {
        var path = FullPath(ProjectTaskStorage.BoardDocumentPath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Taskboard does not exist at '{ProjectTaskStorage.BoardDocumentPath}'. Run `e2d tasks init` first.");
        }

        return ProjectTaskSerializer.DeserializeBoard(ProjectTaskStorage.BoardDocumentPath, File.ReadAllText(path));
    }

    internal void RecoverPendingTransactions()
    {
        RecoverTransactions();
    }

    public IReadOnlyList<ProjectTask> LoadActiveTasks()
    {
        return LoadTasks(ProjectTaskStorage.ActiveTasksDirectory);
    }

    public IReadOnlyList<ProjectTask> LoadCompletedTasks()
    {
        return LoadTasks(ProjectTaskStorage.CompletedTasksDirectory);
    }

    private IReadOnlyList<ProjectTask> LoadTasks(string relativeDirectory)
    {
        var root = FullPath(relativeDirectory);
        if (!Directory.Exists(root))
        {
            return [];
        }

        return Directory.EnumerateFiles(root, "*.e2task", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => ProjectTaskSerializer.DeserializeTask(
                ProjectDocumentPaths.NormalizeRelativePath(Path.GetRelativePath(projectRoot, path)),
                File.ReadAllText(path)))
            .ToArray();
    }

    public ProjectTask LoadActiveTask(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        var path = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        var fullPath = FullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new InvalidOperationException($"Task '{taskId}' was not found.");
        }

        return ProjectTaskSerializer.DeserializeTask(path, File.ReadAllText(fullPath));
    }

    public ProjectTask LoadTask(string taskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        var activePath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        if (File.Exists(FullPath(activePath)))
        {
            return ProjectTaskSerializer.DeserializeTask(activePath, File.ReadAllText(FullPath(activePath)));
        }

        var completedPath = ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        if (File.Exists(FullPath(completedPath)))
        {
            return ProjectTaskSerializer.DeserializeTask(completedPath, File.ReadAllText(FullPath(completedPath)));
        }

        throw new InvalidOperationException($"Task '{taskId}' was not found.");
    }

    public TaskBoardCreateResult Create(TaskBoardCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Title);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Priority);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ActorId);

        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        var tasks = LoadActiveTasks();
        var taskId = NextTaskId(tasks);
        var rank = NextRank(board.Placements);
        var task = new ProjectTask
        {
            TaskUid = $"task-{Guid.NewGuid():N}",
            TaskId = taskId,
            Title = request.Title,
            Description = request.Description,
            Status = ProjectTaskStatus.Ready,
            Readiness = TaskReadiness.Ready,
            Priority = request.Priority,
            Deadline = request.Deadline,
            Rank = rank,
            CreatedBy = request.ActorId,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.CreatedAt,
            AcceptanceState = ProjectTaskAcceptanceState.Open,
            Revision = 1
        };
        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements.Concat([new TaskBoardPlacement(taskId, groupId: null, rank)]));
        var changedFiles = new[]
        {
            ProjectTaskStorage.GetTaskDocumentPath(taskId),
            ProjectTaskStorage.BoardDocumentPath
        };

        if (!request.DryRun)
        {
            ApplyTransaction(
            [
                new TaskBoardFileChange(changedFiles[0], ProjectTaskSerializer.Serialize(task)),
                new TaskBoardFileChange(changedFiles[1], ProjectTaskSerializer.SerializeBoard(updatedBoard))
            ]);
        }

        return new TaskBoardCreateResult(task, updatedBoard, request.DryRun ? [] : changedFiles);
    }

    public TaskBoardTaskMutationResult Update(
        string taskId,
        long expectedRevision,
        string? title,
        string? description,
        string? priority,
        string? assignee,
        DateOnly? deadline,
        bool clearDeadline,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                if (title is not null)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(title);
                    task.Title = title.Trim();
                }

                if (description is not null)
                {
                    task.Description = description;
                }

                if (priority is not null)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(priority);
                    task.Priority = priority.Trim();
                }

                if (assignee is not null)
                {
                    task.Assignee = string.IsNullOrWhiteSpace(assignee) ? null : assignee.Trim();
                }

                if (deadline is not null && clearDeadline)
                {
                    throw new InvalidOperationException("A deadline cannot be set and cleared in the same update.");
                }

                if (deadline is not null)
                {
                    task.Deadline = deadline;
                }
                else if (clearDeadline)
                {
                    task.Deadline = null;
                }

                return task;
            });
    }

    public TaskBoardTaskMutationResult AddDependency(
        string taskId,
        string dependencyTaskId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyTaskId);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, tasks) =>
            {
                if (task.Dependencies.Contains(dependencyTaskId, StringComparer.Ordinal))
                {
                    throw new InvalidOperationException($"Task '{taskId}' already depends on '{dependencyTaskId}'.");
                }

                var validation = TaskDependencyGraph.ValidateAddingDependency(tasks, taskId, dependencyTaskId);
                if (!validation.Succeeded)
                {
                    throw new InvalidOperationException(validation.Diagnostics[0].Message);
                }

                task.Dependencies.Add(dependencyTaskId);
                return task;
            });
    }

    public TaskBoardTaskMutationResult AddCriterion(
        string taskId,
        string criterionId,
        string description,
        AcceptanceCriterionState state,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                var normalizedId = criterionId.Trim();
                if (task.AcceptanceCriteria.Any(criterion =>
                    string.Equals(criterion.CriterionId, normalizedId, StringComparison.Ordinal)))
                {
                    throw new InvalidOperationException(
                        $"Task '{taskId}' already has acceptance criterion '{normalizedId}'.");
                }

                task.AcceptanceCriteria.Add(new AcceptanceCriterion(
                    normalizedId,
                    description.Trim(),
                    state,
                    []));
                return task;
            });
    }

    public TaskBoardTaskMutationResult UpdateCriterion(
        string taskId,
        string criterionId,
        string description,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        return MutateCriterion(
            taskId,
            criterionId,
            expectedRevision,
            now,
            dryRun,
            criterion => new AcceptanceCriterion(
                criterion.CriterionId,
                description.Trim(),
                criterion.State,
                criterion.EvidenceLinks));
    }

    public TaskBoardTaskMutationResult SetCriterionState(
        string taskId,
        string criterionId,
        AcceptanceCriterionState state,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateCriterion(
            taskId,
            criterionId,
            expectedRevision,
            now,
            dryRun,
            criterion => new AcceptanceCriterion(
                criterion.CriterionId,
                criterion.Description,
                state,
                criterion.EvidenceLinks));
    }

    public TaskBoardTaskMutationResult RemoveCriterion(
        string taskId,
        string criterionId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                var index = FindCriterionIndex(task, criterionId);
                task.AcceptanceCriteria.RemoveAt(index);
                return task;
            });
    }

    public TaskBoardTaskMutationResult RemoveDependency(
        string taskId,
        string dependencyTaskId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyTaskId);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                if (!task.Dependencies.Remove(dependencyTaskId))
                {
                    throw new InvalidOperationException($"Task '{taskId}' does not depend on '{dependencyTaskId}'.");
                }

                return task;
            });
    }

    public TaskBoardTagMutationResult CreateTag(
        string name,
        TaskBoardTagColor color,
        string? assignToTaskId,
        long? expectedTaskRevision,
        long expectedBoardRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        var normalizedName = name.Trim();
        EnsureUniqueTagName(board, normalizedName, exceptTagId: null);
        var tag = new TaskBoardTag(NextTagId(board.Tags), normalizedName, color);
        ProjectTask? updatedTask = null;
        if (assignToTaskId is not null)
        {
            if (expectedTaskRevision is null)
            {
                throw new InvalidOperationException("Task revision is required when a new tag is assigned to a task.");
            }

            var task = LoadActiveTask(assignToTaskId);
            EnsureRevision(task, expectedTaskRevision.Value);
            updatedTask = TaskDependencyGraph.CloneTask(task);
            if (!updatedTask.Labels.Contains(tag.TagId, StringComparer.Ordinal))
            {
                updatedTask.Labels.Add(tag.TagId);
            }

            updatedTask.Revision++;
            updatedTask.UpdatedAt = now;
        }

        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements,
            board.Tags.Concat([tag]));
        var changes = new List<TaskBoardFileChange>
        {
            new(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updatedBoard))
        };
        if (updatedTask is not null)
        {
            changes.Add(new TaskBoardFileChange(
                ProjectTaskStorage.GetTaskDocumentPath(updatedTask.TaskId),
                ProjectTaskSerializer.Serialize(updatedTask)));
        }

        if (!dryRun)
        {
            ApplyTransaction(changes);
        }

        return new TaskBoardTagMutationResult(
            updatedBoard,
            updatedTask,
            tag,
            dryRun ? [] : changes.Select(change => change.Path).ToArray());
    }

    public TaskBoardTagMutationResult UpdateTag(
        string tagId,
        string? name,
        TaskBoardTagColor? color,
        long expectedBoardRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        var current = board.Tags.SingleOrDefault(tag => string.Equals(tag.TagId, tagId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Taskboard tag '{tagId}' was not found.");
        var nextName = name?.Trim() ?? current.Name;
        ArgumentException.ThrowIfNullOrWhiteSpace(nextName);
        EnsureUniqueTagName(board, nextName, tagId);
        var replacement = new TaskBoardTag(tagId, nextName, color ?? current.Color);
        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements,
            board.Tags.Select(tag => string.Equals(tag.TagId, tagId, StringComparison.Ordinal) ? replacement : tag));
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updatedBoard))]);
        }

        return new TaskBoardTagMutationResult(updatedBoard, null, replacement, dryRun ? [] : [ProjectTaskStorage.BoardDocumentPath]);
    }

    public TaskBoardTagMutationResult DeleteTag(string tagId, long expectedBoardRevision, bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        var tag = board.Tags.SingleOrDefault(candidate => string.Equals(candidate.TagId, tagId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Taskboard tag '{tagId}' was not found.");
        var owner = LoadActiveTasks().Concat(LoadCompletedTasks())
            .FirstOrDefault(task => task.Labels.Contains(tagId, StringComparer.Ordinal));
        if (owner is not null)
        {
            throw new InvalidOperationException($"Taskboard tag '{tagId}' is still used by task '{owner.TaskId}'.");
        }

        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements,
            board.Tags.Where(candidate => !string.Equals(candidate.TagId, tagId, StringComparison.Ordinal)));
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updatedBoard))]);
        }

        return new TaskBoardTagMutationResult(updatedBoard, null, tag, dryRun ? [] : [ProjectTaskStorage.BoardDocumentPath]);
    }

    public TaskBoardTagMutationResult AssignTag(
        string taskId,
        string tagId,
        long expectedTaskRevision,
        long expectedBoardRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        return ChangeTaskTag(taskId, tagId, expectedTaskRevision, expectedBoardRevision, now, assign: true, dryRun);
    }

    public TaskBoardTagMutationResult UnassignTag(
        string taskId,
        string tagId,
        long expectedTaskRevision,
        long expectedBoardRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        return ChangeTaskTag(taskId, tagId, expectedTaskRevision, expectedBoardRevision, now, assign: false, dryRun);
    }

    private TaskBoardTagMutationResult ChangeTaskTag(
        string taskId,
        string tagId,
        long expectedTaskRevision,
        long expectedBoardRevision,
        DateTimeOffset now,
        bool assign,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tagId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        var tag = board.Tags.SingleOrDefault(candidate => string.Equals(candidate.TagId, tagId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Taskboard tag '{tagId}' was not found.");
        var task = LoadActiveTask(taskId);
        EnsureRevision(task, expectedTaskRevision);
        var updatedTask = TaskDependencyGraph.CloneTask(task);
        var changed = assign
            ? AddUnique(updatedTask.Labels, tagId)
            : updatedTask.Labels.Remove(tagId);
        if (!changed)
        {
            throw new InvalidOperationException(assign
                ? $"Task '{taskId}' already has tag '{tagId}'."
                : $"Task '{taskId}' does not have tag '{tagId}'.");
        }

        updatedTask.Revision++;
        updatedTask.UpdatedAt = now;
        var taskPath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(taskPath, ProjectTaskSerializer.Serialize(updatedTask))]);
        }

        return new TaskBoardTagMutationResult(board, updatedTask, tag, dryRun ? [] : [taskPath]);
    }

    public TaskBoardMutationResult AddGroup(
        TaskBoardGroupKind kind,
        string title,
        string description,
        string? parentGroupId,
        long expectedBoardRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        if (kind == TaskBoardGroupKind.Epoch && parentGroupId is not null)
        {
            throw new InvalidOperationException("Epoch groups cannot have a parent group.");
        }

        if (kind == TaskBoardGroupKind.Milestone)
        {
            if (string.IsNullOrWhiteSpace(parentGroupId))
            {
                throw new InvalidOperationException("Milestone groups require an Epoch parent.");
            }

            var parent = board.Groups.SingleOrDefault(group => string.Equals(group.GroupId, parentGroupId, StringComparison.Ordinal));
            if (parent is null || parent.Kind != TaskBoardGroupKind.Epoch)
            {
                throw new InvalidOperationException($"Milestone parent '{parentGroupId}' is not an Epoch group.");
            }
        }

        var group = new TaskBoardGroup(
            NextGroupId(board.Groups),
            kind,
            title.Trim(),
            description,
            parentGroupId,
            NextGroupRank(board.Groups));
        var updated = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups.Concat([group]),
            board.Placements);
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updated))]);
        }

        return new TaskBoardMutationResult(updated, dryRun ? [] : [ProjectTaskStorage.BoardDocumentPath]);
    }

    public TaskBoardMutationResult UpdateGroup(
        string groupId,
        string? title,
        string? description,
        string? parentGroupId,
        string? rank,
        long expectedBoardRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        var current = board.Groups.SingleOrDefault(group => string.Equals(group.GroupId, groupId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Taskboard group '{groupId}' was not found.");
        var nextParentGroupId = parentGroupId ?? current.ParentGroupId;
        if (current.Kind == TaskBoardGroupKind.Epoch && nextParentGroupId is not null)
        {
            throw new InvalidOperationException("Epoch groups cannot have a parent group.");
        }

        if (current.Kind == TaskBoardGroupKind.Milestone)
        {
            var parent = board.Groups.SingleOrDefault(group => string.Equals(group.GroupId, nextParentGroupId, StringComparison.Ordinal));
            if (parent is null || parent.Kind != TaskBoardGroupKind.Epoch)
            {
                throw new InvalidOperationException($"Milestone parent '{nextParentGroupId}' is not an Epoch group.");
            }
        }

        var replacement = new TaskBoardGroup(
            current.GroupId,
            current.Kind,
            title ?? current.Title,
            description ?? current.Description,
            nextParentGroupId,
            rank ?? current.Rank);
        var updated = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups.Select(group => string.Equals(group.GroupId, groupId, StringComparison.Ordinal) ? replacement : group),
            board.Placements);
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updated))]);
        }

        return new TaskBoardMutationResult(updated, dryRun ? [] : [ProjectTaskStorage.BoardDocumentPath]);
    }

    public TaskBoardMutationResult RemoveGroup(
        string groupId,
        long expectedBoardRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        if (board.Groups.All(group => !string.Equals(group.GroupId, groupId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Taskboard group '{groupId}' was not found.");
        }

        if (board.Placements.Any(placement => string.Equals(placement.GroupId, groupId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Taskboard group '{groupId}' still owns task placements.");
        }

        if (board.Groups.Any(group => string.Equals(group.ParentGroupId, groupId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Taskboard group '{groupId}' still owns child groups.");
        }

        var updated = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups.Where(group => !string.Equals(group.GroupId, groupId, StringComparison.Ordinal)),
            board.Placements);
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updated))]);
        }

        return new TaskBoardMutationResult(updated, dryRun ? [] : [ProjectTaskStorage.BoardDocumentPath]);
    }

    public TaskBoardMutationResult Move(
        string taskId,
        string? groupId,
        string rank,
        long expectedBoardRevision,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(rank);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        _ = LoadActiveTask(taskId);
        if (groupId is not null && board.Groups.All(group => !string.Equals(group.GroupId, groupId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Taskboard group '{groupId}' was not found.");
        }

        if (board.Placements.All(placement => !string.Equals(placement.TaskId, taskId, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Task placement '{taskId}' was not found.");
        }

        var placements = board.Placements
            .Select(placement => string.Equals(placement.TaskId, taskId, StringComparison.Ordinal)
                ? new TaskBoardPlacement(taskId, groupId, rank)
                : placement)
            .ToArray();
        var updated = EvolveBoard(board, board.Revision + 1, board.Groups, placements);
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updated))]);
        }

        return new TaskBoardMutationResult(updated, dryRun ? [] : [ProjectTaskStorage.BoardDocumentPath]);
    }

    public TaskBoardTaskMutationResult SetParent(
        string taskId,
        string parentTaskId,
        long expectedRevision,
        long expectedParentRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentTaskId);
        if (string.Equals(taskId, parentTaskId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("A task cannot be its own parent.");
        }

        using var writeLock = AcquireWriteLock();
        var tasks = LoadActiveTasks();
        var task = tasks.SingleOrDefault(item => string.Equals(item.TaskId, taskId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Task '{taskId}' was not found.");
        var parent = tasks.SingleOrDefault(item => string.Equals(item.TaskId, parentTaskId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Parent task '{parentTaskId}' was not found.");
        EnsureRevision(task, expectedRevision);
        EnsureRevision(parent, expectedParentRevision);
        EnsureParentDoesNotCreateCycle(tasks, taskId, parentTaskId);
        if (task.ParentTaskId is not null)
        {
            throw new InvalidOperationException($"Task '{taskId}' already has parent '{task.ParentTaskId}'. Clear it first.");
        }

        var updatedTask = TaskDependencyGraph.CloneTask(task);
        var updatedParent = TaskDependencyGraph.CloneTask(parent);
        updatedTask.ParentTaskId = parentTaskId;
        updatedTask.Revision++;
        updatedTask.UpdatedAt = now;
        if (!updatedParent.Subtasks.Contains(taskId, StringComparer.Ordinal))
        {
            updatedParent.Subtasks.Add(taskId);
        }

        updatedParent.Revision++;
        updatedParent.UpdatedAt = now;
        var taskPath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        var parentPath = ProjectTaskStorage.GetTaskDocumentPath(parentTaskId);
        if (!dryRun)
        {
            ApplyTransaction(
            [
                new TaskBoardFileChange(taskPath, ProjectTaskSerializer.Serialize(updatedTask)),
                new TaskBoardFileChange(parentPath, ProjectTaskSerializer.Serialize(updatedParent))
            ]);
        }

        return new TaskBoardTaskMutationResult(updatedTask, dryRun ? [] : [taskPath, parentPath]);
    }

    public TaskBoardTaskMutationResult ClearParent(
        string taskId,
        long expectedRevision,
        long expectedParentRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        using var writeLock = AcquireWriteLock();
        var tasks = LoadActiveTasks();
        var task = tasks.SingleOrDefault(item => string.Equals(item.TaskId, taskId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Task '{taskId}' was not found.");
        EnsureRevision(task, expectedRevision);
        if (task.ParentTaskId is null)
        {
            throw new InvalidOperationException($"Task '{taskId}' does not have a parent.");
        }

        var parent = tasks.SingleOrDefault(item => string.Equals(item.TaskId, task.ParentTaskId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Parent task '{task.ParentTaskId}' was not found.");
        EnsureRevision(parent, expectedParentRevision);
        var updatedTask = TaskDependencyGraph.CloneTask(task);
        var updatedParent = TaskDependencyGraph.CloneTask(parent);
        updatedTask.ParentTaskId = null;
        updatedTask.Revision++;
        updatedTask.UpdatedAt = now;
        updatedParent.Subtasks.Remove(taskId);
        updatedParent.Revision++;
        updatedParent.UpdatedAt = now;
        var taskPath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        var parentPath = ProjectTaskStorage.GetTaskDocumentPath(parent.TaskId);
        if (!dryRun)
        {
            ApplyTransaction(
            [
                new TaskBoardFileChange(taskPath, ProjectTaskSerializer.Serialize(updatedTask)),
                new TaskBoardFileChange(parentPath, ProjectTaskSerializer.Serialize(updatedParent))
            ]);
        }

        return new TaskBoardTaskMutationResult(updatedTask, dryRun ? [] : [taskPath, parentPath]);
    }

    public TaskBoardTaskMutationResult AddComment(
        string taskId,
        string text,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                task.Activity.Add(new TaskActivityEntry(
                    $"activity-{Guid.NewGuid():N}",
                    actorId,
                    PrincipalKind.Cli,
                    now,
                    TaskActivityKind.Comment,
                    text.Trim()));
                return task;
            });
    }

    public TaskBoardTaskMutationResult AddAttachment(
        string taskId,
        string sourcePath,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        const long perFileLimit = 25L * 1024 * 1024;
        const long boardLimit = 250L * 1024 * 1024;
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        var fullSourcePath = Path.GetFullPath(sourcePath);
        if (!File.Exists(fullSourcePath))
        {
            throw new InvalidOperationException($"Attachment source '{sourcePath}' was not found or is not a regular file.");
        }

        var attributes = File.GetAttributes(fullSourcePath);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
        {
            throw new InvalidOperationException("Attachment source must be a regular file and cannot be a reparse point.");
        }

        var sourceLength = new FileInfo(fullSourcePath).Length;
        if (sourceLength > perFileLimit)
        {
            throw new InvalidOperationException("Attachment exceeds the default 25 MiB per-file limit.");
        }

        using var writeLock = AcquireWriteLock();
        var task = LoadActiveTask(taskId);
        EnsureRevision(task, expectedRevision);
        var attachmentRoot = FullPath(ProjectTaskStorage.AttachmentsDirectory);
        var currentBoardBytes = Directory.Exists(attachmentRoot)
            ? Directory.EnumerateFiles(attachmentRoot, "*", SearchOption.AllDirectories).Sum(path => new FileInfo(path).Length)
            : 0L;
        if (currentBoardBytes + sourceLength > boardLimit)
        {
            throw new InvalidOperationException("Attachment exceeds the default 250 MiB taskboard limit.");
        }

        var attachmentId = NextAttachmentId(task.Attachments);
        var safeName = SafeAttachmentName(Path.GetFileName(fullSourcePath));
        var relativePath = $"{ProjectTaskStorage.AttachmentsDirectory}/{taskId}/{attachmentId}/{safeName}";
        var bytes = File.ReadAllBytes(fullSourcePath);
        var updated = TaskDependencyGraph.CloneTask(task);
        updated.Attachments.Add(new TaskAttachment
        {
            AttachmentId = attachmentId,
            DisplayName = Path.GetFileName(fullSourcePath),
            RelativePath = relativePath,
            MediaType = AttachmentMediaType(safeName),
            ByteLength = bytes.LongLength,
            Sha256 = HashBytes(bytes),
            AddedAt = now,
            AddedBy = actorId
        });
        updated.Revision++;
        updated.UpdatedAt = now;
        var taskPath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        if (!dryRun)
        {
            ApplyBinaryTransaction(
            [
                new TaskBoardBinaryChange(relativePath, bytes),
                new TaskBoardBinaryChange(taskPath, new UTF8Encoding(false).GetBytes(ProjectTaskSerializer.Serialize(updated)))
            ]);
        }

        return new TaskBoardTaskMutationResult(updated, dryRun ? [] : [relativePath, taskPath]);
    }

    public TaskBoardTaskMutationResult RemoveAttachment(
        string taskId,
        string attachmentId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attachmentId);
        using var writeLock = AcquireWriteLock();
        var task = LoadActiveTask(taskId);
        EnsureRevision(task, expectedRevision);
        var attachment = task.Attachments.SingleOrDefault(item => string.Equals(item.AttachmentId, attachmentId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Attachment '{attachmentId}' was not found on task '{taskId}'.");
        var expectedPrefix = $"{ProjectTaskStorage.AttachmentsDirectory}/{taskId}/{attachmentId}/";
        if (!attachment.RelativePath.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Attachment '{attachmentId}' metadata path is unsafe.");
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        updated.Attachments.RemoveAll(item => string.Equals(item.AttachmentId, attachmentId, StringComparison.Ordinal));
        if (string.Equals(updated.PreviewAttachmentId, attachmentId, StringComparison.Ordinal))
        {
            updated.PreviewAttachmentId = null;
        }
        updated.Revision++;
        updated.UpdatedAt = now;
        var taskPath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        if (!dryRun)
        {
            ApplyBinaryTransaction(
            [
                new TaskBoardBinaryChange(taskPath, new UTF8Encoding(false).GetBytes(ProjectTaskSerializer.Serialize(updated))),
                new TaskBoardBinaryChange(attachment.RelativePath, Content: null)
            ]);
            var attachmentDirectory = Path.GetDirectoryName(FullPath(attachment.RelativePath));
            if (attachmentDirectory is not null && Directory.Exists(attachmentDirectory) && !Directory.EnumerateFileSystemEntries(attachmentDirectory).Any())
            {
                Directory.Delete(attachmentDirectory);
            }
        }

        return new TaskBoardTaskMutationResult(updated, dryRun ? [] : [taskPath, attachment.RelativePath]);
    }

    public TaskBoardTaskMutationResult SetAttachmentPreview(
        string taskId,
        string? attachmentId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun)
    {
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (updated, _) =>
            {
                if (attachmentId is null)
                {
                    if (updated.PreviewAttachmentId is null)
                    {
                        throw new InvalidOperationException($"Task '{taskId}' already uses automatic attachment preview selection.");
                    }

                    updated.PreviewAttachmentId = null;
                    return updated;
                }

                var attachment = updated.Attachments.SingleOrDefault(candidate =>
                    string.Equals(candidate.AttachmentId, attachmentId, StringComparison.Ordinal)) ??
                    throw new InvalidOperationException($"Attachment '{attachmentId}' was not found on task '{taskId}'.");
                if (!TaskAttachmentPreview.IsRasterMediaType(attachment.MediaType))
                {
                    throw new InvalidOperationException($"Attachment '{attachmentId}' is not a supported raster preview.");
                }

                if (string.Equals(updated.PreviewAttachmentId, attachmentId, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Attachment '{attachmentId}' is already selected as the task preview.");
                }

                updated.PreviewAttachmentId = attachmentId;
                return updated;
            });
    }

    public TaskBoardDeleteResult Delete(
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

        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        var active = LoadActiveTasks();
        var completed = LoadCompletedTasks();
        var task = active.Concat(completed).SingleOrDefault(item => string.Equals(item.TaskId, taskId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Task '{taskId}' was not found.");
        EnsureRevision(task, expectedRevision);
        foreach (var other in active.Concat(completed).Where(item => !string.Equals(item.TaskId, taskId, StringComparison.Ordinal)))
        {
            if (other.Dependencies.Contains(taskId, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Task '{taskId}' has incoming dependency from '{other.TaskId}'.");
            }

            if (string.Equals(other.ParentTaskId, taskId, StringComparison.Ordinal) ||
                other.Subtasks.Contains(taskId, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Task '{taskId}' has an incoming containment reference from '{other.TaskId}'.");
            }
        }

        if (task.LinkedTransactions.Count + task.LinkedJobs.Count + task.LinkedDiagnostics.Count +
            task.LinkedArtifacts.Count + task.LinkedScenesResourcesAndNodes.Count > 0)
        {
            throw new InvalidOperationException($"Task '{taskId}' has external links and cannot be hard deleted.");
        }

        if (task.Attachments.Count > 0 && !deleteAttachments)
        {
            throw new InvalidOperationException("Hard delete requires `--delete-attachments true` when attachments exist.");
        }

        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements.Where(placement => !string.Equals(placement.TaskId, taskId, StringComparison.Ordinal)));
        var taskPath = active.Any(item => string.Equals(item.TaskId, taskId, StringComparison.Ordinal))
            ? ProjectTaskStorage.GetTaskDocumentPath(taskId)
            : ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var changes = new List<TaskBoardBinaryChange>
        {
            new(ProjectTaskStorage.BoardDocumentPath, new UTF8Encoding(false).GetBytes(ProjectTaskSerializer.SerializeBoard(updatedBoard))),
            new(taskPath, Content: null)
        };
        changes.AddRange(task.Attachments.Select(attachment => new TaskBoardBinaryChange(attachment.RelativePath, Content: null)));
        if (!dryRun)
        {
            ApplyBinaryTransaction(changes);
            var taskAttachmentRoot = FullPath($"{ProjectTaskStorage.AttachmentsDirectory}/{taskId}");
            if (Directory.Exists(taskAttachmentRoot) && !Directory.EnumerateFileSystemEntries(taskAttachmentRoot, "*", SearchOption.AllDirectories).Any())
            {
                Directory.Delete(taskAttachmentRoot, recursive: true);
            }
        }

        return new TaskBoardDeleteResult(taskId, updatedBoard, dryRun ? [] : changes.Select(change => change.Path).ToArray());
    }

    public TaskBoardMigrationApplyResult ApplyMigration(
        IReadOnlyList<ProjectTask> tasks,
        TaskBoard board,
        string reportSha256)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(board);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportSha256);
        board.Migration.ReportSha256 = reportSha256;
        board.Migration.Finalized = false;
        var boardText = ProjectTaskSerializer.SerializeBoard(board);
        var taskDocuments = tasks
            .Select(task => new TaskBoardFileChange(
                task.ArchivedAt is null
                    ? ProjectTaskStorage.GetTaskDocumentPath(task.TaskId)
                    : ProjectTaskStorage.GetCompletedTaskDocumentPath(task.TaskId),
                ProjectTaskSerializer.Serialize(task)))
            .OrderBy(change => change.Path, StringComparer.Ordinal)
            .ToArray();
        var allChanges = new List<TaskBoardFileChange>(taskDocuments.Length + 2)
        {
            new(ProjectTaskStorage.BoardDocumentPath, boardText),
            new($"{ProjectTaskStorage.RootDirectory}/.gitignore", TaskboardGitIgnoreText)
        };
        allChanges.AddRange(taskDocuments);

        using var writeLock = AcquireWriteLock();
        var boardPath = FullPath(ProjectTaskStorage.BoardDocumentPath);
        if (File.Exists(boardPath))
        {
            if (allChanges.All(change =>
                change.Text is not null &&
                File.Exists(FullPath(change.Path)) &&
                string.Equals(File.ReadAllText(FullPath(change.Path)), change.Text, StringComparison.Ordinal)))
            {
                return new TaskBoardMigrationApplyResult([]);
            }

            throw new InvalidOperationException("Canonical `.taskboard` already exists and does not match this migration report.");
        }

        Directory.CreateDirectory(FullPath(ProjectTaskStorage.ActiveTasksDirectory));
        Directory.CreateDirectory(FullPath(ProjectTaskStorage.CompletedTasksDirectory));
        Directory.CreateDirectory(FullPath(ProjectTaskStorage.AttachmentsDirectory));
        ApplyTransaction(allChanges);
        return new TaskBoardMigrationApplyResult(allChanges.Select(change => change.Path).ToArray());
    }

    public TaskBoardV3MigrationApplyResult ApplyV3Migration(
        string expectedReportSha256,
        long expectedBoardRevision,
        DateTimeOffset migratedAt,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReportSha256);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedBoardRevision, 1);
        using var writeLock = AcquireWriteLock();
        RecoverTransactions();
        var plan = TaskBoardV3Migration.BuildPlan(projectRoot, migratedAt);
        if (!string.Equals(plan.ReportSha256, expectedReportSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Reviewed migration report SHA-256 '{expectedReportSha256}' does not match current source report '{plan.ReportSha256}'.");
        }

        if (plan.SourceBoardRevision != expectedBoardRevision)
        {
            throw new InvalidOperationException(
                $"Taskboard revision conflict: expected {expectedBoardRevision}, actual {plan.SourceBoardRevision}.");
        }

        TaskBoardV3SemanticValidator.Validate(
            projectRoot,
            plan.Board,
            plan.ActiveTasks,
            plan.CompletedTasks,
            validateAttachmentBlobs: false);

        var changes = BuildV3MigrationChanges(plan);
        if (!dryRun)
        {
            ApplyBinaryTransaction(changes);
            TaskBoardV3SemanticValidator.Validate(
                projectRoot,
                plan.Board,
                plan.ActiveTasks,
                plan.CompletedTasks);
        }

        return new TaskBoardV3MigrationApplyResult(
            dryRun ? [] : changes.Select(change => change.Path).ToArray(),
            plan.ReportSha256,
            plan.SourceBoardRevision,
            dryRun);
    }

    private IReadOnlyList<TaskBoardBinaryChange> BuildV3MigrationChanges(TaskBoardV3MigrationPlan plan)
    {
        var changes = new List<TaskBoardBinaryChange>();
        AddV2Snapshot(ProjectTaskStorage.BoardDocumentPath);
        foreach (var path in EnumerateTaskFiles(ProjectTaskStorage.ActiveTasksDirectory)
            .Concat(EnumerateTaskFiles(ProjectTaskStorage.CompletedTasksDirectory))
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            AddV2Snapshot(ProjectDocumentPaths.NormalizeRelativePath(Path.GetRelativePath(projectRoot, path)));
        }

        changes.Add(new TaskBoardBinaryChange(
            ProjectTaskStorage.BoardDocumentPath,
            new UTF8Encoding(false).GetBytes(TaskBoardV3Migration.Serialize(plan.Board))));
        changes.Add(new TaskBoardBinaryChange(
            ".taskboard/.migration/v2/report.json",
            new UTF8Encoding(false).GetBytes(TaskBoardV3Migration.Serialize(plan.Report))));
        changes.AddRange(plan.ActiveTasks.Select(task => new TaskBoardBinaryChange(
            ProjectTaskStorage.GetTaskDocumentPath(task["taskId"]!.GetValue<string>()),
            new UTF8Encoding(false).GetBytes(TaskBoardV3Migration.Serialize(task)))));
        changes.AddRange(plan.CompletedTasks.Select(task => new TaskBoardBinaryChange(
            ProjectTaskStorage.GetCompletedTaskDocumentPath(task["taskId"]!.GetValue<string>()),
            new UTF8Encoding(false).GetBytes(TaskBoardV3Migration.Serialize(task)))));
        foreach (var blob in plan.BlobMigrations)
        {
            changes.Add(new TaskBoardBinaryChange(blob.TargetPath, File.ReadAllBytes(FullPath(blob.SourcePath))));
        }

        var gitIgnorePath = FullPath($"{ProjectTaskStorage.RootDirectory}/.gitignore");
        var gitIgnore = File.Exists(gitIgnorePath) ? File.ReadAllText(gitIgnorePath).ReplaceLineEndings("\n") : string.Empty;
        if (!gitIgnore.Split('\n').Contains("/.migration/", StringComparer.Ordinal))
        {
            gitIgnore = gitIgnore.TrimEnd('\n') + "\n/.migration/\n";
        }

        changes.Add(new TaskBoardBinaryChange(
            $"{ProjectTaskStorage.RootDirectory}/.gitignore",
            new UTF8Encoding(false).GetBytes(gitIgnore)));
        return changes;

        void AddV2Snapshot(string relativePath)
        {
            var sourcePath = FullPath(relativePath);
            var suffix = relativePath.StartsWith($"{ProjectTaskStorage.RootDirectory}/", StringComparison.Ordinal)
                ? relativePath[(ProjectTaskStorage.RootDirectory.Length + 1)..]
                : relativePath;
            changes.Add(new TaskBoardBinaryChange(
                $"{ProjectTaskStorage.RootDirectory}/.migration/v2/{suffix}",
                File.ReadAllBytes(sourcePath)));
        }

        IEnumerable<string> EnumerateTaskFiles(string relativeDirectory)
        {
            var directory = FullPath(relativeDirectory);
            return Directory.Exists(directory)
                ? Directory.EnumerateFiles(directory, "*.e2task", SearchOption.TopDirectoryOnly)
                : [];
        }
    }

    public TaskBoardMigrationFinalizeResult FinalizeMigration(
        IReadOnlyList<ProjectTask> tasks,
        TaskBoard expectedBoard,
        IReadOnlyCollection<string> legacySourcePaths,
        string reportSha256)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        ArgumentNullException.ThrowIfNull(expectedBoard);
        ArgumentNullException.ThrowIfNull(legacySourcePaths);
        ArgumentException.ThrowIfNullOrWhiteSpace(reportSha256);
        using var writeLock = AcquireWriteLock();
        expectedBoard.Migration.ReportSha256 = reportSha256;
        expectedBoard.Migration.Finalized = false;
        var expectedBoardText = ProjectTaskSerializer.SerializeBoard(expectedBoard);
        var boardPath = FullPath(ProjectTaskStorage.BoardDocumentPath);
        if (!File.Exists(boardPath) || !string.Equals(File.ReadAllText(boardPath), expectedBoardText, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Canonical taskboard drifted from the reviewed migration report; finalize is blocked.");
        }

        foreach (var task in tasks)
        {
            var relativePath = task.ArchivedAt is null
                ? ProjectTaskStorage.GetTaskDocumentPath(task.TaskId)
                : ProjectTaskStorage.GetCompletedTaskDocumentPath(task.TaskId);
            var fullPath = FullPath(relativePath);
            var expectedText = ProjectTaskSerializer.Serialize(task);
            if (!File.Exists(fullPath) || !string.Equals(File.ReadAllText(fullPath), expectedText, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Canonical task '{task.TaskId}' drifted from the reviewed migration report; finalize is blocked.");
            }
        }

        _ = Verify();
        foreach (var sourcePath in legacySourcePaths)
        {
            if (!File.Exists(FullPath(sourcePath)))
            {
                throw new InvalidOperationException($"Legacy migration source '{sourcePath}' is missing before finalize.");
            }
        }

        expectedBoard.Migration.Finalized = true;
        var changes = new List<TaskBoardBinaryChange>
        {
            new(ProjectTaskStorage.BoardDocumentPath, new UTF8Encoding(false).GetBytes(ProjectTaskSerializer.SerializeBoard(expectedBoard)))
        };
        changes.AddRange(legacySourcePaths.OrderBy(path => path, StringComparer.Ordinal).Select(path => new TaskBoardBinaryChange(path, Content: null)));
        ApplyBinaryTransaction(changes);
        return new TaskBoardMigrationFinalizeResult(changes.Select(change => change.Path).ToArray());
    }

    public TaskBoardTaskMutationResult SetStatus(
        string taskId,
        ProjectTaskStatus targetStatus,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, tasks) =>
            {
                if (!IsCliTransitionAllowed(task.Status, targetStatus))
                {
                    throw new InvalidOperationException($"Task status transition '{task.Status}' -> '{targetStatus}' is not allowed.");
                }

                var readiness = TaskDependencyGraph.RefreshReadiness(task, tasks);
                if (targetStatus == ProjectTaskStatus.InProgress && readiness.Task.Readiness != TaskReadiness.Ready)
                {
                    throw new InvalidOperationException($"Task '{taskId}' cannot start while it has unfinished dependencies.");
                }

                task.Status = targetStatus;
                if (task.Status == ProjectTaskStatus.Review &&
                    task.AcceptanceState == ProjectTaskAcceptanceState.Submitted)
                {
                    throw new InvalidOperationException("A submitted Review task requires a human acceptance decision.");
                }

                if (targetStatus == ProjectTaskStatus.Review)
                {
                    task.AcceptanceState = ProjectTaskAcceptanceState.Open;
                }
                else if (targetStatus == ProjectTaskStatus.Cancelled)
                {
                    task.AcceptanceState = ProjectTaskAcceptanceState.Cancelled;
                    task.CancellationReason ??= "Cancelled through CLI.";
                }

                task.Activity.Add(new TaskActivityEntry(
                    $"activity-{Guid.NewGuid():N}",
                    actorId,
                    PrincipalKind.Cli,
                    now,
                    TaskActivityKind.StatusChange,
                    $"Status changed to {targetStatus}."));
                return task;
            });
    }

    public TaskBoardTaskMutationResult Submit(
        string taskId,
        long expectedRevision,
        string actorId,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, tasks) =>
            {
                if (task.Status != ProjectTaskStatus.Review)
                {
                    throw new InvalidOperationException("Only a task in Review can be submitted for acceptance.");
                }

                var readiness = TaskDependencyGraph.RefreshReadiness(task, tasks);
                if (readiness.Task.Readiness != TaskReadiness.Ready)
                {
                    throw new InvalidOperationException($"Task '{taskId}' cannot be submitted while it has unfinished dependencies.");
                }

                task.SubmittedAt = now;
                task.AcceptanceState = ProjectTaskAcceptanceState.Submitted;
                task.Activity.Add(new TaskActivityEntry(
                    $"activity-{Guid.NewGuid():N}",
                    actorId,
                    PrincipalKind.Cli,
                    now,
                    TaskActivityKind.AcceptanceResult,
                    string.IsNullOrWhiteSpace(reason) ? "Submitted for human acceptance." : reason.Trim()));
                return task;
            });
    }

    public TaskBoardTaskMutationResult RecordHumanDecision(
        string taskId,
        long expectedRevision,
        bool accept,
        string actorId,
        string reason,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                if (task.Status != ProjectTaskStatus.Review ||
                    task.AcceptanceState != ProjectTaskAcceptanceState.Submitted)
                {
                    throw new InvalidOperationException("Human acceptance decisions require a submitted task in Review.");
                }

                task.Status = accept ? ProjectTaskStatus.Done : ProjectTaskStatus.InProgress;
                task.AcceptanceState = accept
                    ? ProjectTaskAcceptanceState.Accepted
                    : ProjectTaskAcceptanceState.ChangesRequested;
                if (accept)
                {
                    task.CompletedAt = now;
                    task.AcceptedAt = now;
                    task.AcceptedBy = actorId;
                }

                task.Activity.Add(new TaskActivityEntry(
                    $"activity-{Guid.NewGuid():N}",
                    actorId,
                    PrincipalKind.Human,
                    now,
                    TaskActivityKind.AcceptanceResult,
                    reason.Trim()));
                return task;
            });
    }

    public TaskBoardTaskMutationResult Reopen(
        string taskId,
        ProjectTaskStatus targetStatus,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        if (targetStatus is not (ProjectTaskStatus.Ready or ProjectTaskStatus.InProgress))
        {
            throw new InvalidOperationException("Reopen target status must be Ready or InProgress.");
        }

        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, tasks) =>
            {
                if (task.Status is not (ProjectTaskStatus.Done or ProjectTaskStatus.Cancelled))
                {
                    throw new InvalidOperationException("Only Done or Cancelled tasks can be reopened.");
                }

                var readiness = TaskDependencyGraph.RefreshReadiness(task, tasks.Concat(LoadCompletedTasks()));
                if (targetStatus == ProjectTaskStatus.InProgress && readiness.Task.Readiness != TaskReadiness.Ready)
                {
                    throw new InvalidOperationException($"Task '{taskId}' cannot start while it has unfinished dependencies.");
                }

                task.Status = targetStatus;
                task.AcceptanceState = ProjectTaskAcceptanceState.Reopened;
                task.Activity.Add(new TaskActivityEntry(
                    $"activity-{Guid.NewGuid():N}",
                    actorId,
                    PrincipalKind.Cli,
                    now,
                    TaskActivityKind.StatusChange,
                    $"Task reopened as {targetStatus}."));
                return task;
            });
    }

    public TaskBoardTaskMutationResult Archive(
        string taskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        var task = LoadActiveTask(taskId);
        EnsureRevision(task, expectedRevision);
        if (task.Status is not (ProjectTaskStatus.Done or ProjectTaskStatus.Cancelled))
        {
            throw new InvalidOperationException("Only Done or Cancelled tasks can be archived.");
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        updated.Revision++;
        updated.UpdatedAt = now;
        updated.ArchivedAt = now;
        updated.ArchivedBy = actorId;
        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements.Where(placement => !string.Equals(placement.TaskId, taskId, StringComparison.Ordinal)));
        var activePath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        var completedPath = ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        var changedFiles = new[] { activePath, completedPath, ProjectTaskStorage.BoardDocumentPath };
        if (!dryRun)
        {
            ApplyTransaction(
            [
                new TaskBoardFileChange(completedPath, ProjectTaskSerializer.Serialize(updated)),
                new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updatedBoard)),
                new TaskBoardFileChange(activePath, Text: null)
            ]);
        }

        return new TaskBoardTaskMutationResult(updated, dryRun ? [] : changedFiles);
    }

    public TaskBoardTaskMutationResult Unarchive(
        string taskId,
        long expectedRevision,
        string actorId,
        DateTimeOffset now,
        bool dryRun)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        var completedPath = ProjectTaskStorage.GetCompletedTaskDocumentPath(taskId);
        if (!File.Exists(FullPath(completedPath)))
        {
            throw new InvalidOperationException($"Archived task '{taskId}' was not found.");
        }

        var task = ProjectTaskSerializer.DeserializeTask(completedPath, File.ReadAllText(FullPath(completedPath)));
        EnsureRevision(task, expectedRevision);
        var activePath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        if (File.Exists(FullPath(activePath)))
        {
            throw new InvalidOperationException($"Active task '{taskId}' already exists.");
        }

        var updated = TaskDependencyGraph.CloneTask(task);
        updated.Revision++;
        updated.UpdatedAt = now;
        updated.ArchivedAt = null;
        updated.ArchivedBy = null;
        var updatedBoard = EvolveBoard(
            board,
            board.Revision + 1,
            board.Groups,
            board.Placements.Concat([new TaskBoardPlacement(taskId, groupId: null, NextRank(board.Placements))]));
        var changedFiles = new[] { completedPath, activePath, ProjectTaskStorage.BoardDocumentPath };
        if (!dryRun)
        {
            ApplyTransaction(
            [
                new TaskBoardFileChange(activePath, ProjectTaskSerializer.Serialize(updated)),
                new TaskBoardFileChange(ProjectTaskStorage.BoardDocumentPath, ProjectTaskSerializer.SerializeBoard(updatedBoard)),
                new TaskBoardFileChange(completedPath, Text: null)
            ]);
        }

        return new TaskBoardTaskMutationResult(updated, dryRun ? [] : changedFiles);
    }

    public TaskBoardVerificationResult Verify()
    {
        return VerifyCore(allowLegacyTagReferences: false);
    }

    private TaskBoardVerificationResult VerifyCore(bool allowLegacyTagReferences)
    {
        var board = LoadBoard();
        var active = LoadActiveTasks();
        var completed = LoadCompletedTasks();
        var activeIds = active.Select(task => task.TaskId).ToHashSet(StringComparer.Ordinal);
        var completedIds = completed.Select(task => task.TaskId).ToHashSet(StringComparer.Ordinal);
        if (activeIds.Overlaps(completedIds))
        {
            throw new InvalidOperationException("A task id exists in both active and completed storage.");
        }

        var placementIds = board.Placements.Select(placement => placement.TaskId).ToArray();
        if (placementIds.Distinct(StringComparer.Ordinal).Count() != placementIds.Length)
        {
            throw new InvalidOperationException("Taskboard contains duplicate placements.");
        }

        if (!activeIds.SetEquals(placementIds))
        {
            throw new InvalidOperationException("Taskboard placements do not match active task files.");
        }

        var all = active.Concat(completed).ToDictionary(task => task.TaskId, StringComparer.Ordinal);
        var tagIds = board.Tags.Select(tag => tag.TagId).ToArray();
        if (tagIds.Distinct(StringComparer.Ordinal).Count() != tagIds.Length)
        {
            throw new InvalidOperationException("Taskboard contains duplicate tag ids.");
        }

        if (board.Tags.Select(tag => tag.Name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != board.Tags.Count)
        {
            throw new InvalidOperationException("Taskboard contains duplicate tag names.");
        }

        var tagIdSet = tagIds.ToHashSet(StringComparer.Ordinal);
        foreach (var task in all.Values)
        {
            if (!allowLegacyTagReferences)
            {
                var missingTag = task.Labels.FirstOrDefault(tagId => !tagIdSet.Contains(tagId));
                if (missingTag is not null)
                {
                    throw new InvalidOperationException($"Taskboard tag '{missingTag}' referenced by '{task.TaskId}' was not found.");
                }
            }

            foreach (var dependency in task.Dependencies)
            {
                if (!all.ContainsKey(dependency))
                {
                    throw new InvalidOperationException($"Dependency task '{dependency}' referenced by '{task.TaskId}' was not found.");
                }
            }
        }

        EnsureAcyclic(all);
        return new TaskBoardVerificationResult(active.Count, completed.Count);
    }

    public TaskBoardNormalizationResult Normalize(long expectedBoardRevision, bool dryRun)
    {
        using var writeLock = AcquireWriteLock();
        var board = LoadBoard();
        EnsureBoardRevision(board, expectedBoardRevision);
        _ = VerifyCore(allowLegacyTagReferences: true);

        var activeTasks = LoadActiveTasks();
        var completedTasks = LoadCompletedTasks();
        var knownTagIds = board.Tags.Select(tag => tag.TagId).ToHashSet(StringComparer.Ordinal);
        var missingTags = activeTasks.Concat(completedTasks)
            .SelectMany(task => task.Labels)
            .Where(tagId => !knownTagIds.Contains(tagId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(tagId => tagId, StringComparer.Ordinal)
            .Select(tagId => new TaskBoardTag(tagId, tagId, TaskBoardTagColor.Gray))
            .ToArray();
        if (missingTags.Length > 0)
        {
            board = EvolveBoard(board, board.Revision, board.Groups, board.Placements, board.Tags.Concat(missingTags));
        }

        var changes = new List<TaskBoardFileChange>();
        AddNormalizationChange(
            changes,
            ProjectTaskStorage.BoardDocumentPath,
            ProjectTaskSerializer.SerializeBoard(board));

        foreach (var task in activeTasks)
        {
            NormalizeLegacyLinkedArtifacts(task);
            AddNormalizationChange(
                changes,
                ProjectTaskStorage.GetTaskDocumentPath(task.TaskId),
                ProjectTaskSerializer.Serialize(task));
        }

        foreach (var task in completedTasks)
        {
            NormalizeLegacyLinkedArtifacts(task);
            AddNormalizationChange(
                changes,
                ProjectTaskStorage.GetCompletedTaskDocumentPath(task.TaskId),
                ProjectTaskSerializer.Serialize(task));
        }

        if (!dryRun)
        {
            ApplyTransaction(changes);
        }

        return new TaskBoardNormalizationResult(
            board,
            changes.Select(change => change.Path).ToArray());
    }

    private static void NormalizeLegacyLinkedArtifacts(ProjectTask task)
    {
        var normalizedCommands = new List<string>();
        var seenCommands = new HashSet<string>(StringComparer.Ordinal);
        foreach (var command in task.ExecutionContract.RequiredCommands)
        {
            AddCommand(command, NormalizeMarkdownCode(command), normalizedCommands, seenCommands);
        }

        var retainedArtifacts = new List<string>();
        foreach (var artifact in task.LinkedArtifacts)
        {
            if (TryReadClearCommand(artifact, out var command))
            {
                AddCommand(command, command, normalizedCommands, seenCommands);
            }
            else
            {
                retainedArtifacts.Add(artifact);
            }
        }

        task.LinkedArtifacts.Clear();
        task.LinkedArtifacts.AddRange(retainedArtifacts);
        task.ExecutionContract.RequiredCommands.Clear();
        task.ExecutionContract.RequiredCommands.AddRange(normalizedCommands);
    }

    private static void AddCommand(
        string command,
        string identity,
        ICollection<string> destination,
        ISet<string> seen)
    {
        if (identity.Length > 0 && seen.Add(identity))
        {
            destination.Add(command);
        }
    }

    private static bool TryReadClearCommand(string value, out string command)
    {
        command = NormalizeMarkdownCode(value);
        var separator = command.IndexOf(' ', StringComparison.Ordinal);
        if (separator <= 0)
        {
            return false;
        }

        var executable = command[..separator];
        return executable is
            "dotnet" or "git" or "e2d" or "npm" or "npx" or "pnpm" or "yarn" or
            "node" or "python" or "python3" or "pwsh" or "powershell" or "bash" or
            "sh" or "cmake" or "msbuild" or "cargo" or "java" or "javac" or "xcodebuild" or
            "gradle" or "gradlew" or "./gradlew";
    }

    private static string NormalizeMarkdownCode(string value)
    {
        var normalized = value.Trim();
        if (normalized.Length >= 2 && normalized[0] == '`' && normalized[^1] == '`')
        {
            normalized = normalized[1..^1].Trim();
        }

        return normalized;
    }

    private void AddNormalizationChange(
        ICollection<TaskBoardFileChange> changes,
        string relativePath,
        string normalizedText)
    {
        if (!string.Equals(File.ReadAllText(FullPath(relativePath)), normalizedText, StringComparison.Ordinal))
        {
            changes.Add(new TaskBoardFileChange(relativePath, normalizedText));
        }
    }

    private TaskBoardTaskMutationResult MutateTask(
        string taskId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun,
        Func<ProjectTask, IReadOnlyList<ProjectTask>, ProjectTask> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedRevision, 1);
        ArgumentNullException.ThrowIfNull(mutation);

        using var writeLock = AcquireWriteLock();
        var tasks = LoadActiveTasks();
        var current = tasks.SingleOrDefault(task => string.Equals(task.TaskId, taskId, StringComparison.Ordinal)) ??
            throw new InvalidOperationException($"Task '{taskId}' was not found.");
        if (current.Revision != expectedRevision)
        {
            throw new InvalidOperationException(
                $"Task '{taskId}' revision conflict: expected {expectedRevision}, actual {current.Revision}.");
        }

        var updated = mutation(TaskDependencyGraph.CloneTask(current), tasks);
        updated.Revision = current.Revision + 1;
        updated.UpdatedAt = now;
        var relativePath = ProjectTaskStorage.GetTaskDocumentPath(taskId);
        if (!dryRun)
        {
            ApplyTransaction([new TaskBoardFileChange(relativePath, ProjectTaskSerializer.Serialize(updated))]);
        }

        return new TaskBoardTaskMutationResult(updated, dryRun ? [] : [relativePath]);
    }

    private TaskBoardTaskMutationResult MutateCriterion(
        string taskId,
        string criterionId,
        long expectedRevision,
        DateTimeOffset now,
        bool dryRun,
        Func<AcceptanceCriterion, AcceptanceCriterion> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(criterionId);
        ArgumentNullException.ThrowIfNull(mutation);
        return MutateTask(
            taskId,
            expectedRevision,
            now,
            dryRun,
            (task, _) =>
            {
                var index = FindCriterionIndex(task, criterionId);
                task.AcceptanceCriteria[index] = mutation(task.AcceptanceCriteria[index]);
                return task;
            });
    }

    private static int FindCriterionIndex(ProjectTask task, string criterionId)
    {
        var normalizedId = criterionId.Trim();
        var index = task.AcceptanceCriteria.FindIndex(criterion =>
            string.Equals(criterion.CriterionId, normalizedId, StringComparison.Ordinal));
        return index >= 0
            ? index
            : throw new InvalidOperationException(
                $"Task '{task.TaskId}' acceptance criterion '{normalizedId}' was not found.");
    }

    private static bool IsCliTransitionAllowed(ProjectTaskStatus current, ProjectTaskStatus target)
    {
        if (target == ProjectTaskStatus.Done)
        {
            return false;
        }

        return current switch
        {
            ProjectTaskStatus.Ready => target is ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.InProgress => target is ProjectTaskStatus.Ready or ProjectTaskStatus.Blocked or ProjectTaskStatus.Review or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Blocked => target is ProjectTaskStatus.Ready or ProjectTaskStatus.InProgress or ProjectTaskStatus.Cancelled,
            ProjectTaskStatus.Review => target is ProjectTaskStatus.InProgress or ProjectTaskStatus.Blocked,
            _ => false
        };
    }

    private static void EnsureRevision(ProjectTask task, long expectedRevision)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedRevision, 1);
        if (task.Revision != expectedRevision)
        {
            throw new InvalidOperationException(
                $"Task '{task.TaskId}' revision conflict: expected {expectedRevision}, actual {task.Revision}.");
        }
    }

    private static void EnsureAcyclic(IReadOnlyDictionary<string, ProjectTask> tasks)
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var taskId in tasks.Keys)
        {
            Visit(taskId);
        }

        void Visit(string taskId)
        {
            if (visited.Contains(taskId))
            {
                return;
            }

            if (!visiting.Add(taskId))
            {
                throw new InvalidOperationException($"Dependency cycle detected at task '{taskId}'.");
            }

            foreach (var dependency in tasks[taskId].Dependencies)
            {
                Visit(dependency);
            }

            visiting.Remove(taskId);
            visited.Add(taskId);
        }
    }

    private static void EnsureBoardRevision(TaskBoard board, long expectedRevision)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(expectedRevision, 1);
        if (board.Revision != expectedRevision)
        {
            throw new InvalidOperationException(
                $"Taskboard revision conflict: expected {expectedRevision}, actual {board.Revision}.");
        }
    }

    private static string NextGroupId(IEnumerable<TaskBoardGroup> groups)
    {
        var maximum = 0;
        foreach (var group in groups)
        {
            if (group.GroupId.StartsWith("G-", StringComparison.Ordinal) &&
                int.TryParse(group.GroupId.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                maximum = Math.Max(maximum, value);
            }
        }

        return $"G-{maximum + 1:D4}";
    }

    private static TaskBoard EvolveBoard(
        TaskBoard board,
        long revision,
        IEnumerable<TaskBoardGroup> groups,
        IEnumerable<TaskBoardPlacement> placements,
        IEnumerable<TaskBoardTag>? tags = null)
    {
        var updated = new TaskBoard(board.BoardId, revision, groups, placements);
        updated.IdPolicy.Prefix = board.IdPolicy.Prefix;
        updated.IdPolicy.Padding = board.IdPolicy.Padding;
        updated.IdPolicy.NextNumber = board.IdPolicy.NextNumber;
        updated.AttachmentPolicy.PerFileByteLimit = board.AttachmentPolicy.PerFileByteLimit;
        updated.AttachmentPolicy.BoardByteLimit = board.AttachmentPolicy.BoardByteLimit;
        updated.Migration.ReportSha256 = board.Migration.ReportSha256;
        updated.Migration.Finalized = board.Migration.Finalized;
        foreach (var pair in board.Migration.SourceDigests)
        {
            updated.Migration.SourceDigests[pair.Key] = pair.Value;
        }

        updated.Migration.Diagnostics.AddRange(board.Migration.Diagnostics);
        updated.Migration.LegacySourceFragments.AddRange(board.Migration.LegacySourceFragments);
        updated.Tags.AddRange(tags ?? board.Tags);
        return updated;
    }

    private static void EnsureUniqueTagName(TaskBoard board, string name, string? exceptTagId)
    {
        if (board.Tags.Any(tag =>
            !string.Equals(tag.TagId, exceptTagId, StringComparison.Ordinal) &&
            string.Equals(tag.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Taskboard tag name '{name}' is already used.");
        }
    }

    private static string NextTagId(IEnumerable<TaskBoardTag> tags)
    {
        var maximum = 0;
        foreach (var tag in tags)
        {
            if (tag.TagId.StartsWith("tag-", StringComparison.Ordinal) &&
                int.TryParse(tag.TagId.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                maximum = Math.Max(maximum, value);
            }
        }

        return $"tag-{maximum + 1:D4}";
    }

    private static bool AddUnique(List<string> values, string value)
    {
        if (values.Contains(value, StringComparer.Ordinal))
        {
            return false;
        }

        values.Add(value);
        return true;
    }

    private static string NextAttachmentId(IEnumerable<TaskAttachment> attachments)
    {
        var maximum = 0;
        foreach (var attachment in attachments)
        {
            if (attachment.AttachmentId.StartsWith("A-", StringComparison.Ordinal) &&
                int.TryParse(attachment.AttachmentId.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                maximum = Math.Max(maximum, value);
            }
        }

        return $"A-{maximum + 1:D4}";
    }

    private static string SafeAttachmentName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var safe = new string(name.Select(character => invalid.Contains(character) || char.IsControl(character) ? '_' : character).ToArray()).Trim();
        if (safe is "" or "." or "..")
        {
            safe = "attachment.bin";
        }

        return safe.Length <= 120 ? safe : safe[..120];
    }

    private static string AttachmentMediaType(string name)
    {
        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }

    private static string NextGroupRank(IEnumerable<TaskBoardGroup> groups)
    {
        var maximum = 0;
        foreach (var group in groups)
        {
            if (int.TryParse(group.Rank, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                maximum = Math.Max(maximum, value);
            }
        }

        return (maximum + 1000).ToString("D8", CultureInfo.InvariantCulture);
    }

    private static void EnsureParentDoesNotCreateCycle(
        IReadOnlyList<ProjectTask> tasks,
        string taskId,
        string parentTaskId)
    {
        var byId = tasks.ToDictionary(task => task.TaskId, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var current = parentTaskId;
        while (byId.TryGetValue(current, out var parent) && parent.ParentTaskId is not null)
        {
            if (!visited.Add(current) || string.Equals(parent.ParentTaskId, taskId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Setting parent '{parentTaskId}' for '{taskId}' creates a containment cycle.");
            }

            current = parent.ParentTaskId;
        }
    }

    internal FileStream AcquireWriteLock()
    {
        var root = FullPath(ProjectTaskStorage.RootDirectory);
        Directory.CreateDirectory(root);
        var started = System.Diagnostics.Stopwatch.StartNew();
        var backoff = writeOptions.InitialBackoff;
        IOException? lastException = null;
        while (true)
        {
            if (writeOptions.CancellationToken.IsCancellationRequested)
            {
                throw new TaskBoardWriteException(
                    "E2D-TASK-0005",
                    "Taskboard writer wait was cancelled.",
                    isRetryable: true,
                    innerException: new OperationCanceledException(writeOptions.CancellationToken));
            }

            try
            {
                var stream = new FileStream(
                    Path.Combine(root, ".lock"),
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None);
                try
                {
                    ThrowIfOperationWasCommitted();
                    return stream;
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
            }
            catch (IOException exception)
            {
                lastException = exception;
            }

            if (started.Elapsed >= writeOptions.LockTimeout)
            {
                throw new TaskBoardWriteException(
                    "E2D-TASK-0004",
                    $"Taskboard writer lock was not acquired within {writeOptions.LockTimeout.TotalMilliseconds:0} ms.",
                    isRetryable: true,
                    innerException: lastException);
            }

            var remaining = writeOptions.LockTimeout - started.Elapsed;
            var delay = remaining < backoff ? remaining : backoff;
            if (delay > TimeSpan.Zero)
            {
                if (writeOptions.CancellationToken.WaitHandle.WaitOne(delay))
                {
                    throw new TaskBoardWriteException(
                        "E2D-TASK-0005",
                        "Taskboard writer wait was cancelled.",
                        isRetryable: true,
                        innerException: new OperationCanceledException(writeOptions.CancellationToken));
                }
            }

            backoff = TimeSpan.FromMilliseconds(Math.Min(
                writeOptions.MaximumBackoff.TotalMilliseconds,
                Math.Max(writeOptions.InitialBackoff.TotalMilliseconds, backoff.TotalMilliseconds * 2)));
        }
    }

    private static void ValidateWriteOptions(TaskBoardWriteOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.LockTimeout <= TimeSpan.Zero || options.LockTimeout > TimeSpan.FromSeconds(60))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Taskboard lock timeout must be between 1 ms and 60 seconds.");
        }

        if (options.InitialBackoff <= TimeSpan.Zero || options.MaximumBackoff < options.InitialBackoff)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Taskboard lock backoff must be positive and ordered.");
        }

        if (options.Operation is { } operation)
        {
            if (operation.OperationId.Length is < 1 or > 128 ||
                operation.OperationId.Any(character =>
                    !char.IsAsciiLetterOrDigit(character) && character is not ('.' or '_' or ':' or '-')))
            {
                throw new ArgumentException("Taskboard operation id must contain 1-128 ASCII letters, digits, '.', '_', ':' or '-'.", nameof(options));
            }

            if (string.IsNullOrWhiteSpace(operation.Command) ||
                operation.Fingerprint.Length != 64 ||
                operation.Fingerprint.Any(character => !char.IsAsciiHexDigit(character)))
            {
                throw new ArgumentException("Taskboard operation identity is incomplete.", nameof(options));
            }
        }
    }

    private void ThrowIfOperationWasCommitted()
    {
        if (writeOptions.Operation is not { } operation)
        {
            return;
        }

        var receiptPath = FullPath(OperationReceiptPath(operation.OperationId));
        if (!File.Exists(receiptPath))
        {
            return;
        }

        var receipt = JsonNode.Parse(File.ReadAllText(receiptPath))?.AsObject() ??
            throw new TaskBoardWriteException(
                "E2D-TASK-0007",
                $"Taskboard operation receipt for '{operation.OperationId}' is invalid.",
                isRetryable: false);
        var storedId = receipt["operationId"]?.GetValue<string>();
        var storedCommand = receipt["command"]?.GetValue<string>();
        var storedFingerprint = receipt["fingerprint"]?.GetValue<string>();
        if (!string.Equals(storedId, operation.OperationId, StringComparison.Ordinal) ||
            !string.Equals(storedCommand, operation.Command, StringComparison.Ordinal) ||
            !string.Equals(storedFingerprint, operation.Fingerprint, StringComparison.OrdinalIgnoreCase))
        {
            throw new TaskBoardWriteException(
                "E2D-TASK-0007",
                $"Taskboard operation id '{operation.OperationId}' was already used for a different command payload.",
                isRetryable: false);
        }

        var taskId = receipt["taskId"]?.GetValue<string>() ?? string.Empty;
        var changedFiles = receipt["changedFiles"]?.AsArray()
            .Select(value => value?.GetValue<string>() ?? string.Empty)
            .Where(value => value.Length > 0)
            .ToArray() ?? [];
        throw new TaskBoardOperationReplayedException(
            taskId,
            receipt["taskRevision"]?.GetValue<long?>(),
            receipt["boardRevision"]?.GetValue<long?>(),
            changedFiles);
    }

    private static string OperationReceiptPath(string operationId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(operationId))).ToLowerInvariant();
        return $"{ProjectTaskStorage.RootDirectory}/.operations/{hash}.json";
    }

    internal string FullPath(string relativePath)
    {
        var normalized = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var path = Path.GetFullPath(Path.Combine(projectRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Taskboard path '{relativePath}' escapes the project root.");
        }

        return path;
    }

    private static string NextTaskId(IEnumerable<ProjectTask> tasks)
    {
        var maximum = 0;
        foreach (var task in tasks)
        {
            if (task.TaskId.Length > 2 &&
                task.TaskId.StartsWith("T-", StringComparison.Ordinal) &&
                int.TryParse(task.TaskId.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                maximum = Math.Max(maximum, value);
            }
        }

        return $"T-{maximum + 1:D4}";
    }

    private static string NextRank(IEnumerable<TaskBoardPlacement> placements)
    {
        var maximum = 0;
        foreach (var placement in placements)
        {
            if (int.TryParse(placement.Rank, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
            {
                maximum = Math.Max(maximum, value);
            }
        }

        return (maximum + 1000).ToString("D8", CultureInfo.InvariantCulture);
    }

    private void ApplyTransaction(IReadOnlyList<TaskBoardFileChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ApplyBinaryTransaction(changes
            .Select(change => new TaskBoardBinaryChange(
                change.Path,
                change.Text is null ? null : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(change.Text)))
            .ToArray());
    }

    internal void ApplyBinaryTransaction(IReadOnlyList<TaskBoardBinaryChange> changes)
    {
        ArgumentNullException.ThrowIfNull(changes);
        if (changes.Count == 0)
        {
            return;
        }

        var effectiveChanges = changes.ToList();
        if (writeOptions.Operation is { } operation)
        {
            var changedFiles = changes.Select(change => ProjectDocumentPaths.NormalizeRelativePath(change.Path)).ToArray();
            var taskChange = changes.LastOrDefault(change =>
                change.Path.EndsWith(".e2task", StringComparison.OrdinalIgnoreCase) &&
                (change.Path.StartsWith(ProjectTaskStorage.ActiveTasksDirectory + "/", StringComparison.Ordinal) ||
                 change.Path.StartsWith(ProjectTaskStorage.CompletedTasksDirectory + "/", StringComparison.Ordinal)));
            var taskId = taskChange is null ? string.Empty : Path.GetFileNameWithoutExtension(taskChange.Path);
            long? taskRevision = null;
            if (taskChange?.Content is { } taskBytes)
            {
                taskRevision = JsonNode.Parse(taskBytes)?["revision"]?.GetValue<long>();
            }

            var boardChange = changes.LastOrDefault(change =>
                string.Equals(change.Path, ProjectTaskStorage.BoardDocumentPath, StringComparison.Ordinal));
            long? boardRevision = null;
            if (boardChange?.Content is { } boardBytes)
            {
                boardRevision = JsonNode.Parse(boardBytes)?["revision"]?.GetValue<long>();
            }
            else
            {
                var boardPath = FullPath(ProjectTaskStorage.BoardDocumentPath);
                if (File.Exists(boardPath))
                {
                    boardRevision = JsonNode.Parse(File.ReadAllText(boardPath))?["revision"]?.GetValue<long>();
                }
            }

            var receipt = new JsonObject
            {
                ["format"] = "Electron2D.TaskOperationReceipt",
                ["version"] = 1,
                ["operationId"] = operation.OperationId,
                ["command"] = operation.Command,
                ["fingerprint"] = operation.Fingerprint.ToLowerInvariant(),
                ["taskId"] = taskId,
                ["taskRevision"] = taskRevision,
                ["boardRevision"] = boardRevision,
                ["changedFiles"] = new JsonArray(changedFiles.Select(path => (JsonNode)path).ToArray()),
                ["committedAt"] = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };
            effectiveChanges.Add(new TaskBoardBinaryChange(
                OperationReceiptPath(operation.OperationId),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(
                    receipt.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n")));
        }

        var transactionId = $"tx-{Guid.NewGuid():N}";
        var stagingRelativeRoot = $"{ProjectTaskStorage.RootDirectory}/.staging/{transactionId}";
        var transactionsRelativeRoot = $"{ProjectTaskStorage.RootDirectory}/.transactions";
        Directory.CreateDirectory(FullPath(stagingRelativeRoot));
        Directory.CreateDirectory(FullPath(transactionsRelativeRoot));
        var operations = new JsonArray();
        for (var index = 0; index < effectiveChanges.Count; index++)
        {
            var change = effectiveChanges[index];
            var targetPath = FullPath(change.Path);
            var beforeHash = File.Exists(targetPath) ? HashFile(targetPath) : null;
            if (change.Content is null)
            {
                operations.Add(new JsonObject
                {
                    ["kind"] = "delete",
                    ["path"] = ProjectDocumentPaths.NormalizeRelativePath(change.Path),
                    ["stagedPath"] = null,
                    ["beforeSha256"] = beforeHash,
                    ["afterSha256"] = null
                });
                continue;
            }

            var stagedRelativePath = $"{stagingRelativeRoot}/{index:D4}.stage";
            var bytes = change.Content;
            File.WriteAllBytes(FullPath(stagedRelativePath), bytes);
            operations.Add(new JsonObject
            {
                ["kind"] = "replace",
                ["path"] = ProjectDocumentPaths.NormalizeRelativePath(change.Path),
                ["stagedPath"] = stagedRelativePath,
                ["beforeSha256"] = beforeHash,
                ["afterSha256"] = HashBytes(bytes)
            });
        }

        var manifest = new JsonObject
        {
            ["format"] = "Electron2D.TaskTransaction",
            ["version"] = 1,
            ["transactionId"] = transactionId,
            ["state"] = "prepared",
            ["operations"] = operations
        };
        var manifestPath = $"{transactionsRelativeRoot}/{transactionId}.json";
        WriteTextAtomically(FullPath(manifestPath), manifest.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");
        ReplayTransaction(FullPath(manifestPath));
    }

    private void RecoverTransactions()
    {
        var transactionsRoot = FullPath($"{ProjectTaskStorage.RootDirectory}/.transactions");
        if (!Directory.Exists(transactionsRoot))
        {
            return;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(transactionsRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            ReplayTransaction(manifestPath);
        }
    }

    private void ReplayTransaction(string manifestPath)
    {
        var root = JsonNode.Parse(File.ReadAllText(manifestPath))?.AsObject() ??
            throw new InvalidOperationException($"Task transaction manifest '{manifestPath}' is not a JSON object.");
        if (root["format"]?.GetValue<string>() != "Electron2D.TaskTransaction" ||
            root["version"]?.GetValue<int>() != 1 ||
            root["state"]?.GetValue<string>() != "prepared")
        {
            throw new InvalidOperationException($"Task transaction manifest '{manifestPath}' has an unsupported contract.");
        }

        var transactionId = root["transactionId"]?.GetValue<string>() ??
            throw new InvalidOperationException("Task transaction id is missing.");
        if (transactionId.Length == 0 || transactionId.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new InvalidOperationException($"Task transaction id '{transactionId}' is unsafe.");
        }

        var operations = root["operations"]?.AsArray() ??
            throw new InvalidOperationException($"Task transaction '{transactionId}' has no operations.");
        foreach (var operationNode in operations)
        {
            var operation = operationNode?.AsObject() ??
                throw new InvalidOperationException($"Task transaction '{transactionId}' contains an invalid operation.");
            var kind = operation["kind"]?.GetValue<string>() ?? string.Empty;
            var relativePath = operation["path"]?.GetValue<string>() ??
                throw new InvalidOperationException($"Task transaction '{transactionId}' operation path is missing.");
            var targetPath = FullPath(relativePath);
            var beforeHash = operation["beforeSha256"]?.GetValue<string>();
            var afterHash = operation["afterSha256"]?.GetValue<string>();
            var currentHash = File.Exists(targetPath) ? HashFile(targetPath) : null;

            if (kind == "replace")
            {
                if (afterHash is null)
                {
                    throw new InvalidOperationException($"Task transaction '{transactionId}' replace hash is missing.");
                }

                if (string.Equals(currentHash, afterHash, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.Equals(currentHash, beforeHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Task transaction '{transactionId}' conflicts with '{relativePath}'.");
                }

                var stagedRelativePath = operation["stagedPath"]?.GetValue<string>() ??
                    throw new InvalidOperationException($"Task transaction '{transactionId}' staged path is missing.");
                var expectedPrefix = $"{ProjectTaskStorage.RootDirectory}/.staging/{transactionId}/";
                if (!stagedRelativePath.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Task transaction '{transactionId}' staged path is unsafe.");
                }

                var stagedPath = FullPath(stagedRelativePath);
                if (!File.Exists(stagedPath) || !string.Equals(HashFile(stagedPath), afterHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Task transaction '{transactionId}' staged payload hash mismatch.");
                }

                WriteBytesAtomically(targetPath, File.ReadAllBytes(stagedPath));
            }
            else if (kind == "delete")
            {
                if (currentHash is null)
                {
                    continue;
                }

                if (!string.Equals(currentHash, beforeHash, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Task transaction '{transactionId}' conflicts with '{relativePath}'.");
                }

                File.Delete(targetPath);
            }
            else
            {
                throw new InvalidOperationException($"Task transaction '{transactionId}' operation kind '{kind}' is unsupported.");
            }
        }

        File.Delete(manifestPath);
        var stagingRoot = FullPath($"{ProjectTaskStorage.RootDirectory}/.staging/{transactionId}");
        if (Directory.Exists(stagingRoot))
        {
            Directory.Delete(stagingRoot, recursive: true);
        }
    }

    private static string HashFile(string path)
    {
        return HashBytes(File.ReadAllBytes(path));
    }

    private static string HashBytes(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static void WriteBytesAtomically(string path, byte[] bytes)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path '{path}' has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static void WriteTextAtomically(string path, string text)
    {
        var directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException($"Path '{path}' has no parent directory.");
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            File.WriteAllText(temporaryPath, text);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
