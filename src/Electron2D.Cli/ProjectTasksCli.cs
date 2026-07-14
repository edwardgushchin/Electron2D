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
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Electron2D.ProjectSystem;

internal static partial class Electron2DCommandLine
{
    private const string TaskboardGitIgnore = TaskBoardDiskStore.TaskboardGitIgnoreText;

    private static int RunTasksInit(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var taskboardRoot = Path.Combine(projectRoot, ProjectTaskStorage.RootDirectory);
        var boardPath = Path.Combine(projectRoot, ProjectTaskStorage.BoardDocumentPath.Replace('/', Path.DirectorySeparatorChar));
        var gitIgnorePath = Path.Combine(taskboardRoot, ".gitignore");
        if (File.Exists(boardPath))
        {
            return WriteResult(
                CliResult.Failure(
                    "tasks init",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Taskboard already exists.",
                    CreateCliDiagnostic("E2D-CLI-0002", $"Taskboard already exists at '{ProjectTaskStorage.BoardDocumentPath}'."),
                    new JsonObject
                    {
                        ["mode"] = "tasks.init",
                        ["taskboardPath"] = ProjectTaskStorage.RootDirectory
                    }),
                output,
                error);
        }

        var changedFiles = new[]
        {
            ProjectTaskStorage.BoardDocumentPath,
            $"{ProjectTaskStorage.RootDirectory}/.gitignore"
        };
        if (!options.DryRun)
        {
            Directory.CreateDirectory(Path.Combine(projectRoot, ProjectTaskStorage.ActiveTasksDirectory.Replace('/', Path.DirectorySeparatorChar)));
            Directory.CreateDirectory(Path.Combine(projectRoot, ProjectTaskStorage.CompletedTasksDirectory.Replace('/', Path.DirectorySeparatorChar)));
            Directory.CreateDirectory(Path.Combine(projectRoot, ProjectTaskStorage.AttachmentsDirectory.Replace('/', Path.DirectorySeparatorChar)));
            var board = TaskBoardV3DiskStore.CreateNativeBoard();
            File.WriteAllText(boardPath, TaskBoardV3Migration.Serialize(board));
            File.WriteAllText(gitIgnorePath, TaskboardGitIgnore);
        }

        return WriteResult(
            CliResult.Success(
                "tasks init",
                options,
                projectRoot,
                CliRoute.None,
                options.DryRun ? "Taskboard initialization validated." : "Taskboard initialized.",
                options.DryRun ? [] : changedFiles,
                dirtyDocuments: [],
                operation: null,
                job: null,
                new JsonObject
                {
                    ["mode"] = "tasks.init",
                    ["taskboardPath"] = ProjectTaskStorage.RootDirectory,
                    ["boardPath"] = ProjectTaskStorage.BoardDocumentPath,
                    ["version"] = 3,
                    ["dryRun"] = options.DryRun
                }),
            output,
            error);
    }

    private static int RunTasksCreate(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var title = options.GetOption("--title");
        if (string.IsNullOrWhiteSpace(title))
        {
            return WriteResult(
                CliResult.Failure(
                    "tasks create",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Task title is required.",
                    CreateCliDiagnostic("E2D-CLI-0002", "Use `--title <text>` for `e2d tasks create`."),
                    new JsonObject { ["mode"] = "tasks.create" }),
                output,
                error);
        }

        try
        {
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                var resultV3 = CreateTaskBoardV3Store(projectRoot, options, "tasks create").Create(
                    title.Trim(),
                    options.GetOption("--description")?.Trim() ?? string.Empty,
                    options.GetOption("--priority")?.Trim() ?? "P2",
                    ParseOptionalDeadline(options.GetOption("--deadline")),
                    options.GetStructuredTaskInput(),
                    ParseOptionalInt64(options.GetOption("--expected-board-revision"), "--expected-board-revision"),
                    "cli",
                    context.NowUtc,
                    options.DryRun);
                return WriteV3TaskMutationResult(
                    "tasks create",
                    "tasks.create",
                    options,
                    projectRoot,
                    resultV3,
                    output,
                    error);
            }

            var store = new TaskBoardDiskStore(projectRoot);
            var result = store.Create(new TaskBoardCreateRequest(
                title.Trim(),
                options.GetOption("--description")?.Trim() ?? string.Empty,
                options.GetOption("--priority")?.Trim() ?? "P2",
                ParseOptionalDeadline(options.GetOption("--deadline")),
                ActorId: "cli",
                context.NowUtc,
                options.DryRun));
            var taskData = JsonNode.Parse(ProjectTaskSerializer.Serialize(result.Task)) as JsonObject ??
                throw new InvalidOperationException("Serialized task did not produce a JSON object.");
            return WriteResult(
                CliResult.Success(
                    "tasks create",
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Task creation validated." : $"Task '{result.Task.TaskId}' created.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = "tasks.create",
                        ["task"] = taskData,
                        ["boardRevision"] = result.Board.Revision,
                        ["dryRun"] = options.DryRun
                    }),
                output,
                error);
        }
        catch (TaskBoardOperationReplayedException replay)
        {
            return WriteV3TaskReplayResult("tasks create", "tasks.create", options, projectRoot, replay, output, error);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException)
        {
            return WriteTaskFailure("tasks create", "tasks.create", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksList(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                var snapshot = new TaskBoardV3DiskStore(projectRoot).LoadSnapshot();
                var allTasks = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
                var v3Items = new JsonArray(snapshot.ActiveTasks
                    .OrderBy(task => task["taskId"]!.GetValue<string>(), StringComparer.Ordinal)
                    .Select(task => (JsonNode)TaskBoardV3DiskStore.CreateTaskProjection(task, allTasks)).ToArray());
                return WriteTaskReadResult(
                    "tasks list",
                    "tasks.list",
                    options,
                    projectRoot,
                    new JsonObject { ["tasks"] = v3Items, ["count"] = snapshot.ActiveTasks.Count },
                    output,
                    error);
            }

            var store = new TaskBoardDiskStore(projectRoot);
            var tasks = store.LoadActiveTasks();
            var items = new JsonArray();
            foreach (var task in tasks.OrderBy(task => task.TaskId, StringComparer.Ordinal))
            {
                items.Add(TaskSnapshot(task, tasks));
            }

            return WriteTaskReadResult(
                "tasks list",
                "tasks.list",
                options,
                projectRoot,
                new JsonObject { ["tasks"] = items, ["count"] = tasks.Count },
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure("tasks list", "tasks.list", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksBoard(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var includeArchivedText = options.GetOption("--include-archived");
            if (includeArchivedText is not null &&
                !bool.TryParse(includeArchivedText, out _))
            {
                throw new InvalidOperationException("--include-archived must be true or false.");
            }

            var includeArchived = bool.TryParse(includeArchivedText, out var parsedIncludeArchived) && parsedIncludeArchived;
            var compactText = options.GetOption("--compact");
            if (compactText is not null && !bool.TryParse(compactText, out _))
            {
                throw new InvalidOperationException("--compact must be true or false.");
            }

            var compact = bool.TryParse(compactText, out var parsedCompact) && parsedCompact;
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                var snapshot = new TaskBoardV3DiskStore(projectRoot).LoadSnapshot();
                var allTasks = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
                var selectedTasks = includeArchived ? allTasks : snapshot.ActiveTasks;
                var v3Items = new JsonArray(selectedTasks
                    .OrderBy(task => task["taskId"]!.GetValue<string>(), StringComparer.Ordinal)
                    .Select(task => (JsonNode)(compact
                        ? TaskBoardV3DiskStore.CreateCardProjection(task, allTasks)
                        : TaskBoardV3DiskStore.CreateTaskProjection(task, allTasks))).ToArray());
                return WriteTaskReadResult(
                    "tasks board",
                    "tasks.board",
                    options,
                    projectRoot,
                    new JsonObject
                    {
                        ["board"] = TaskBoardV3DiskStore.CreateBoardProjection(snapshot),
                        ["tasks"] = v3Items
                    },
                    output,
                    error);
            }

            var store = new TaskBoardDiskStore(projectRoot);
            var board = JsonNode.Parse(ProjectTaskSerializer.SerializeBoard(store.LoadBoard())) as JsonObject ??
                throw new InvalidOperationException("Serialized board did not produce a JSON object.");
            var tasks = includeArchived
                ? store.LoadActiveTasks().Concat(store.LoadCompletedTasks()).ToArray()
                : store.LoadActiveTasks();
            var taskItems = new JsonArray();
            foreach (var task in tasks.OrderBy(task => task.TaskId, StringComparer.Ordinal))
            {
                taskItems.Add(compact ? TaskCardSnapshot(task, tasks) : TaskSnapshot(task, tasks));
            }

            return WriteTaskReadResult(
                "tasks board",
                "tasks.board",
                options,
                projectRoot,
                new JsonObject { ["board"] = board, ["tasks"] = taskItems },
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure("tasks board", "tasks.board", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksGroupAdd(CliOptions options, TextWriter output, TextWriter error)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3BoardMutation(
                "tasks group add", "tasks.group.add", options, output, error,
                store => store.AddGroup(
                    options.RequireOption("--kind", "Group kind is required."),
                    options.RequireOption("--title", "Group title is required."),
                    options.GetOption("--description") ?? string.Empty,
                    options.GetOption("--parent"),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                    options.DryRun));
        }

        return RunBoardMutation(
            "tasks group add",
            "tasks.group.add",
            options,
            output,
            error,
            store =>
            {
                var kindText = options.RequireOption("--kind", "Group kind is required.");
                if (!Enum.TryParse<TaskBoardGroupKind>(kindText, ignoreCase: true, out var kind))
                {
                    throw new InvalidOperationException($"Taskboard group kind '{kindText}' is not supported.");
                }

                return store.AddGroup(
                    kind,
                    options.RequireOption("--title", "Group title is required."),
                    options.GetOption("--description") ?? string.Empty,
                    options.GetOption("--parent"),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                    options.DryRun);
            });
    }

    private static int RunTasksGroupUpdate(CliOptions options, TextWriter output, TextWriter error)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3BoardMutation(
                "tasks group update", "tasks.group.update", options, output, error,
                store => store.UpdateGroup(
                    options.Values[2], options.GetOption("--title"), options.GetOption("--description"),
                    options.GetOption("--parent"), options.GetOption("--rank"),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."), options.DryRun));
        }

        return RunBoardMutation(
            "tasks group update",
            "tasks.group.update",
            options,
            output,
            error,
            store => store.UpdateGroup(
                options.Values[2],
                options.GetOption("--title"),
                options.GetOption("--description"),
                options.GetOption("--parent"),
                options.GetOption("--rank"),
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                options.DryRun));
    }

    private static int RunTasksGroupRemove(CliOptions options, TextWriter output, TextWriter error)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3BoardMutation(
                "tasks group remove", "tasks.group.remove", options, output, error,
                store => store.RemoveGroup(
                    options.Values[2], options.RequireInt64("--expected-board-revision", "Taskboard revision is required."), options.DryRun));
        }

        return RunBoardMutation(
            "tasks group remove",
            "tasks.group.remove",
            options,
            output,
            error,
            store => store.RemoveGroup(
                options.Values[2],
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                options.DryRun));
    }

    private static int RunTasksMove(CliOptions options, TextWriter output, TextWriter error)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3BoardMutation(
                "tasks move", "tasks.move", options, output, error,
                store => store.Move(
                    options.Values[1],
                    options.GetOption("--group"),
                    options.RequireOption("--rank", "Placement rank is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                    options.DryRun));
        }

        return RunBoardMutation(
            "tasks move",
            "tasks.move",
            options,
            output,
            error,
            store => store.Move(
                options.Values[1],
                options.GetOption("--group"),
                options.RequireOption("--rank", "Placement rank is required."),
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                options.DryRun));
    }

    private static int RunTasksParentSet(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks parent set", "tasks.parent.set", options, output, error,
                store => store.SetParent(
                    options.Values[2],
                    options.RequireOption("--parent", "Parent task id is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks parent set",
            "tasks.parent.set",
            options,
            output,
            error,
            store => store.SetParent(
                options.Values[2],
                options.RequireOption("--parent", "Parent task id is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                options.RequireInt64("--expected-parent-revision", "Parent task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksParentClear(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks parent clear", "tasks.parent.clear", options, output, error,
                store => store.ClearParent(
                    options.Values[2],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks parent clear",
            "tasks.parent.clear",
            options,
            output,
            error,
            store => store.ClearParent(
                options.Values[2],
                options.RequireInt64("--expected-revision", "Task revision is required."),
                options.RequireInt64("--expected-parent-revision", "Parent task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksGet(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                var storeV3 = new TaskBoardV3DiskStore(projectRoot);
                var snapshot = storeV3.LoadSnapshot();
                var allTasks = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
                var taskV3 = allTasks.SingleOrDefault(task =>
                    string.Equals(task["taskId"]?.GetValue<string>(), options.Values[1], StringComparison.Ordinal)) ??
                    throw new FileNotFoundException($"Task '{options.Values[1]}' was not found.");
                return WriteTaskReadResult(
                    "tasks get",
                    "tasks.get",
                    options,
                    projectRoot,
                    new JsonObject
                    {
                        ["task"] = TaskBoardV3DiskStore.CreateTaskProjection(taskV3, allTasks),
                        ["canonicalTask"] = taskV3.DeepClone(),
                        ["agentContext"] = AgentContextBuilderV3.Build(taskV3)
                    },
                    output,
                    error);
            }

            var store = new TaskBoardDiskStore(projectRoot);
            var tasks = store.LoadActiveTasks().Concat(store.LoadCompletedTasks()).ToArray();
            var task = store.LoadTask(options.Values[1]);
            return WriteTaskReadResult(
                "tasks get",
                "tasks.get",
                options,
                projectRoot,
                new JsonObject { ["task"] = TaskSnapshot(task, tasks) },
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure("tasks get", "tasks.get", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksUpdate(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        if (TaskBoardV3DiskStore.IsV3(projectRoot))
        {
            if (options.GetOption("--assignee") is not null)
            {
                return WriteTaskFailure(
                    "tasks update",
                    "tasks.update",
                    options,
                    projectRoot,
                    new InvalidOperationException("TaskBoard v3 does not assign tasks; --assignee is not supported."),
                    output,
                    error);
            }

            return RunV3TaskMutation(
                "tasks update",
                "tasks.update",
                options,
                output,
                error,
                store => store.Update(
                    options.Values[1],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    options.GetOption("--title"),
                    options.GetOption("--description"),
                    options.GetOption("--priority"),
                    ParseOptionalDeadline(options.GetOption("--deadline")),
                    ParseBooleanOption(options.GetOption("--clear-deadline"), "--clear-deadline"),
                    options.GetStructuredTaskInput(),
                    "cli",
                    context.NowUtc,
                    options.DryRun));
        }

        return RunTaskMutation(
            "tasks update",
            "tasks.update",
            options,
            output,
            error,
            store => store.Update(
                options.Values[1],
                options.RequireInt64("--expected-revision", "Task revision is required."),
                options.GetOption("--title"),
                options.GetOption("--description"),
                options.GetOption("--priority"),
                options.GetOption("--assignee"),
                ParseOptionalDeadline(options.GetOption("--deadline")),
                ParseBooleanOption(options.GetOption("--clear-deadline"), "--clear-deadline"),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksTagCreate(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            var assignTo = options.GetOption("--assign-to");
            if (assignTo is not null)
            {
                return RunV3TaskMutation(
                    "tasks tag create", "tasks.tag.create", options, output, error,
                    store => store.CreateTagAndAssign(
                        options.RequireOption("--name", "Tag name is required."),
                        options.RequireOption("--color", "Tag color is required."),
                        assignTo,
                        RequireTaskRevision(options, "Task revision is required when assigning the new tag."),
                        options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                        "cli",
                        context.NowUtc,
                        options.DryRun));
            }

            return RunV3BoardMutation(
                "tasks tag create", "tasks.tag.create", options, output, error,
                store => store.CreateTag(
                    options.RequireOption("--name", "Tag name is required."),
                    options.RequireOption("--color", "Tag color is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."), options.DryRun));
        }

        return RunTagMutation(
            "tasks tag create",
            "tasks.tag.create",
            options,
            output,
            error,
            store => store.CreateTag(
                options.RequireOption("--name", "Tag name is required."),
                ParseTagColor(options.RequireOption("--color", "Tag color is required.")),
                options.GetOption("--assign-to"),
                ParseOptionalInt64(options.GetOption("--expected-task-revision"), "--expected-task-revision"),
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksTagUpdate(
        CliOptions options,
        TextWriter output,
        TextWriter error)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3BoardMutation(
                "tasks tag update", "tasks.tag.update", options, output, error,
                store => store.UpdateTag(
                    options.Values[2], options.GetOption("--name"), options.GetOption("--color"),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."), options.DryRun));
        }

        return RunTagMutation(
            "tasks tag update",
            "tasks.tag.update",
            options,
            output,
            error,
            store => store.UpdateTag(
                options.Values[2],
                options.GetOption("--name"),
                options.GetOption("--color") is { } color ? ParseTagColor(color) : null,
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                options.DryRun));
    }

    private static int RunTasksTagDelete(CliOptions options, TextWriter output, TextWriter error)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3BoardMutation(
                "tasks tag delete", "tasks.tag.delete", options, output, error,
                store => store.DeleteTag(
                    options.Values[2], options.RequireInt64("--expected-board-revision", "Taskboard revision is required."), options.DryRun));
        }

        return RunTagMutation(
            "tasks tag delete",
            "tasks.tag.delete",
            options,
            output,
            error,
            store => store.DeleteTag(
                options.Values[2],
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                options.DryRun));
    }

    private static int RunTasksTagAssign(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context,
        bool assign)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                assign ? "tasks tag assign" : "tasks tag remove",
                assign ? "tasks.tag.assign" : "tasks.tag.remove",
                options, output, error,
                store => store.AssignTag(
                    options.Values[2],
                    options.RequireOption("--tag", "Tag id is required."),
                    options.RequireInt64("--expected-task-revision", "Task revision is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                    "cli", context.NowUtc, assign, options.DryRun));
        }

        return RunTagMutation(
            assign ? "tasks tag assign" : "tasks tag unassign",
            assign ? "tasks.tag.assign" : "tasks.tag.unassign",
            options,
            output,
            error,
            store => assign
                ? store.AssignTag(
                    options.Values[2],
                    options.RequireOption("--tag", "Tag id is required."),
                    options.RequireInt64("--expected-task-revision", "Task revision is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                    context.NowUtc,
                    options.DryRun)
                : store.UnassignTag(
                    options.Values[2],
                    options.RequireOption("--tag", "Tag id is required."),
                    options.RequireInt64("--expected-task-revision", "Task revision is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                    context.NowUtc,
                    options.DryRun));
    }

    private static int RunTasksTagApply(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        const string command = "tasks tag apply";
        const string mode = "tasks.tag.apply";
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            if (!TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                throw new InvalidOperationException("Batch tag plans require TaskBoard v3.");
            }

            var structuredInput = options.GetStructuredTaskInput() ??
                throw new InvalidOperationException("Batch tag plan is required through --input <file|->.");
            if (structuredInput.Count != 1 || structuredInput["tagUpdates"] is not JsonArray updateNodes)
            {
                throw new InvalidOperationException("Batch tag input must contain only the tagUpdates array and expectedBoardRevision.");
            }

            var updates = new List<TaskBoardV3TagUpdate>(updateNodes.Count);
            foreach (var node in updateNodes)
            {
                if (node is not JsonObject update ||
                    update.Count != 3 ||
                    !update.ContainsKey("taskId") ||
                    !update.ContainsKey("expectedRevision") ||
                    update["tagIds"] is not JsonArray tagNodes)
                {
                    throw new InvalidOperationException("Each tag update must contain exactly taskId, expectedRevision and tagIds.");
                }

                var taskId = update["taskId"]?.GetValue<string>() ??
                    throw new InvalidOperationException("Tag update taskId is required.");
                var expectedRevision = update["expectedRevision"]?.GetValue<long>() ??
                    throw new InvalidOperationException("Tag update expectedRevision is required.");
                var tagIds = tagNodes.Select(tag => tag?.GetValue<string>() ??
                    throw new InvalidOperationException("Tag update tagIds cannot contain null.")).ToArray();
                updates.Add(new TaskBoardV3TagUpdate(taskId, expectedRevision, tagIds));
            }

            var result = CreateTaskBoardV3Store(projectRoot, options, command).ApplyTagUpdates(
                updates,
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required for a batch tag plan."),
                "cli",
                context.NowUtc,
                options.DryRun);
            var snapshot = new TaskBoardV3Snapshot(result.Board, result.ActiveTasks, result.CompletedTasks);
            return WriteResult(
                CliResult.Success(
                    command,
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Batch tag plan validated." : "Batch tag plan applied.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = mode,
                        ["board"] = TaskBoardV3DiskStore.CreateBoardProjection(snapshot),
                        ["updatedCount"] = updates.Count,
                        ["dryRun"] = options.DryRun
                    }),
                output,
                error);
        }
        catch (TaskBoardOperationReplayedException replay)
        {
            return WriteV3BoardReplayResult(command, mode, options, projectRoot, replay, output, error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(command, mode, options, projectRoot, exception, output, error);
        }
    }

    private static int RunTagMutation(
        string command,
        string mode,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        Func<TaskBoardDiskStore, TaskBoardTagMutationResult> mutation)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var result = mutation(new TaskBoardDiskStore(projectRoot));
            var data = new JsonObject
            {
                ["mode"] = mode,
                ["board"] = JsonNode.Parse(ProjectTaskSerializer.SerializeBoard(result.Board)),
                ["task"] = result.Task is null ? null : JsonNode.Parse(ProjectTaskSerializer.Serialize(result.Task)),
                ["tag"] = result.Tag is null
                    ? null
                    : new JsonObject
                    {
                        ["tagId"] = result.Tag.TagId,
                        ["name"] = result.Tag.Name,
                        ["color"] = result.Tag.Color.ToString()
                    },
                ["dryRun"] = options.DryRun
            };
            return WriteResult(
                CliResult.Success(command, options, projectRoot, CliRoute.None, "Task tag mutation completed.", result.ChangedFiles, [], null, null, data),
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(command, mode, options, projectRoot, exception, output, error);
        }
    }

    private static TaskBoardTagColor ParseTagColor(string value)
    {
        return Enum.TryParse<TaskBoardTagColor>(value, ignoreCase: true, out var color) && Enum.IsDefined(color)
            ? color
            : throw new InvalidOperationException($"Taskboard tag color '{value}' is not supported.");
    }

    private static DateOnly? ParseOptionalDeadline(string? value)
    {
        if (value is null)
        {
            return null;
        }

        return DateOnly.TryParseExact(value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var deadline)
            ? deadline
            : throw new InvalidOperationException("--deadline must use YYYY-MM-DD.");
    }

    private static bool ParseBooleanOption(string? value, string name)
    {
        if (value is null)
        {
            return false;
        }

        return bool.TryParse(value, out var parsed)
            ? parsed
            : throw new InvalidOperationException($"{name} must be true or false.");
    }

    private static long? ParseOptionalInt64(string? value, string name)
    {
        if (value is null)
        {
            return null;
        }

        return long.TryParse(value, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : throw new InvalidOperationException($"{name} must be a positive integer.");
    }

    private static int RunTasksSetStatus(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks set-status", "tasks.set-status", options, output, error,
                store => store.SetStatus(
                    options.Values[1],
                    options.RequireOption("--status", "Target status is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    context.TaskActorId,
                    context.TaskActorKind,
                    options.GetOption("--reason") ?? "Статус изменён через CLI.",
                    context.NowUtc,
                    options.DryRun));
        }

        return RunTaskMutation(
            "tasks set-status",
            "tasks.set-status",
            options,
            output,
            error,
            store =>
            {
                var statusText = options.RequireOption("--status", "Target status is required.");
                if (!Enum.TryParse<ProjectTaskStatus>(statusText, ignoreCase: true, out var status))
                {
                    throw new InvalidOperationException($"Task status '{statusText}' is not supported.");
                }

                return store.SetStatus(
                    options.Values[1],
                    status,
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli",
                    context.NowUtc,
                    options.DryRun);
            });
    }

    private static int RunTasksCriterionAdd(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            var requestedState = options.GetOption("--state") ?? nameof(AcceptanceCriterionState.Open);
            if (!string.Equals(requestedState, nameof(AcceptanceCriterionState.Open), StringComparison.OrdinalIgnoreCase))
            {
                return WriteTaskFailure(
                    "tasks criterion add", "tasks.criterion.add", options, NormalizeProjectRoot(options.ProjectRoot),
                    new InvalidOperationException("A new v3 acceptance criterion starts in Open; use set-state afterward."), output, error);
            }

            return RunV3TaskMutation(
                "tasks criterion add", "tasks.criterion.add", options, output, error,
                store => store.AddCriterion(
                    options.Values[2],
                    options.RequireOption("--criterion", "Criterion id is required."),
                    options.RequireOption("--description", "Criterion description is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks criterion add",
            "tasks.criterion.add",
            options,
            output,
            error,
            store => store.AddCriterion(
                options.Values[2],
                options.RequireOption("--criterion", "Criterion id is required."),
                options.RequireOption("--description", "Criterion description is required."),
                ParseCriterionState(options.GetOption("--state") ?? nameof(AcceptanceCriterionState.Open)),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksCriterionUpdate(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks criterion update", "tasks.criterion.update", options, output, error,
                store => store.UpdateCriterion(
                    options.Values[2],
                    options.RequireOption("--criterion", "Criterion id is required."),
                    options.RequireOption("--description", "Criterion description is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks criterion update",
            "tasks.criterion.update",
            options,
            output,
            error,
            store => store.UpdateCriterion(
                options.Values[2],
                options.RequireOption("--criterion", "Criterion id is required."),
                options.RequireOption("--description", "Criterion description is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksCriterionSetState(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks criterion set-state", "tasks.criterion.set-state", options, output, error,
                store => store.SetCriterionState(
                    options.Values[2],
                    options.RequireOption("--criterion", "Criterion id is required."),
                    options.RequireOption("--state", "Criterion state is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks criterion set-state",
            "tasks.criterion.set-state",
            options,
            output,
            error,
            store => store.SetCriterionState(
                options.Values[2],
                options.RequireOption("--criterion", "Criterion id is required."),
                ParseCriterionState(options.RequireOption("--state", "Criterion state is required.")),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksCriterionAddEvidence(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (!TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return WriteTaskFailure(
                "tasks criterion add-evidence",
                "tasks.criterion.add-evidence",
                options,
                NormalizeProjectRoot(options.ProjectRoot),
                new InvalidOperationException("Typed criterion evidence is available only for TaskBoard v3."),
                output,
                error);
        }

        return RunV3TaskMutation(
            "tasks criterion add-evidence",
            "tasks.criterion.add-evidence",
            options,
            output,
            error,
            store => store.AddCriterionEvidence(
                options.Values[2],
                options.RequireOption("--criterion", "Criterion id is required."),
                options.RequireOption("--kind", "Evidence kind File, Uri or Attachment is required."),
                options.RequireOption("--value", "Evidence value is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksCriterionRemove(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks criterion remove", "tasks.criterion.remove", options, output, error,
                store => store.RemoveCriterion(
                    options.Values[2],
                    options.RequireOption("--criterion", "Criterion id is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks criterion remove",
            "tasks.criterion.remove",
            options,
            output,
            error,
            store => store.RemoveCriterion(
                options.Values[2],
                options.RequireOption("--criterion", "Criterion id is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static AcceptanceCriterionState ParseCriterionState(string value)
    {
        if (!Enum.TryParse<AcceptanceCriterionState>(value, ignoreCase: true, out var state) ||
            !Enum.IsDefined(state))
        {
            throw new InvalidOperationException(
                $"Acceptance criterion state '{value}' is not supported. Use Open, Passed or Failed.");
        }

        return state;
    }

    private static int RunTasksSubmit(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks submit", "tasks.submit", options, output, error,
                store => store.Submit(
                    options.Values[1],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    context.TaskActorId,
                    context.TaskActorKind,
                    options.GetOption("--reason") ?? "Передано на приёмку.",
                    context.NowUtc,
                    options.DryRun));
        }

        return RunTaskMutation(
            "tasks submit",
            "tasks.submit",
            options,
            output,
            error,
            store => store.Submit(
                options.Values[1],
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                options.GetOption("--reason") ?? "Submitted for human acceptance.",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksHumanDecisionUnavailable(
        CliOptions options,
        TextWriter output,
        TextWriter error)
    {
        var command = $"tasks {options.Values[0]}";
        return WriteResult(
            CliResult.Blocked(
                command,
                options,
                "Trusted human confirmation is required.",
                CreateCliDiagnostic(
                    "E2D-TASK-0002",
                    "This decision is unavailable to noninteractive CLI and requires the trusted human stdio bridge.")),
            output,
            error);
    }

    private static int RunTasksHumanDecision(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        try
        {
            var bridge = ReadHumanDecisionBridge(context);
            if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
            {
                return RunV3TaskMutation(
                    "tasks human-decision", "tasks.human-decision", options, output, error,
                    store => string.Equals(bridge.Decision, "accept", StringComparison.Ordinal)
                        ? store.Accept(
                            options.Values[1],
                            options.RequireInt64("--expected-revision", "Task revision is required."),
                            context.HumanActorId,
                            bridge.Reason,
                            context.NowUtc,
                            options.DryRun)
                        : store.RequestChanges(
                            options.Values[1],
                            options.RequireInt64("--expected-revision", "Task revision is required."),
                            context.HumanActorId,
                            bridge.Reason,
                            context.NowUtc,
                            options.DryRun));
            }

            return RunTaskMutation(
                "tasks human-decision",
                "tasks.human-decision",
                options,
                output,
                error,
                store => store.RecordHumanDecision(
                    options.Values[1],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    string.Equals(bridge.Decision, "accept", StringComparison.Ordinal),
                    context.HumanActorId,
                    bridge.Reason,
                    context.NowUtc,
                    options.DryRun));
        }
        catch (Exception exception) when (exception is JsonException or IOException or FormatException or InvalidOperationException or ArgumentException)
        {
            return WriteTaskFailure(
                "tasks human-decision",
                "tasks.human-decision",
                options,
                NormalizeProjectRoot(options.ProjectRoot),
                new InvalidOperationException("Trusted human decision bridge validation failed.", exception),
                output,
                error);
        }
    }

    private static int RunTasksHumanMessage(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        try
        {
            if (!TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
            {
                throw new InvalidOperationException("Trusted human conversation append is available only for TaskBoard v3.");
            }

            var bridge = ReadHumanMessageBridge(context);
            return RunV3TaskMutation(
                "tasks human-message", "tasks.human-message", options, output, error,
                store => store.AddComment(
                    options.Values[1],
                    bridge.Text,
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    context.HumanActorId,
                    "Human",
                    context.NowUtc,
                    options.DryRun));
        }
        catch (Exception exception) when (exception is JsonException or IOException or FormatException or InvalidOperationException or ArgumentException)
        {
            return WriteTaskFailure(
                "tasks human-message",
                "tasks.human-message",
                options,
                NormalizeProjectRoot(options.ProjectRoot),
                new InvalidOperationException("Trusted human message bridge validation failed.", exception),
                output,
                error);
        }
    }

    private static TaskHumanDecisionBridge ReadHumanDecisionBridge(CliExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.HumanCapability) ||
            context.HumanCapability.Length is < 32 or > 256)
        {
            throw new InvalidOperationException("Human capability is unavailable.");
        }

        var buffer = new char[65_537];
        var count = context.Input.ReadBlock(buffer, 0, buffer.Length);
        if (count == buffer.Length)
        {
            throw new InvalidOperationException("Human decision payload exceeds 64 KiB.");
        }

        using var document = JsonDocument.Parse(new string(buffer, 0, count));
        var root = document.RootElement;
        var expectedProperties = new[] { "protocol", "capability", "decision", "reason" };
        if (root.ValueKind != JsonValueKind.Object ||
            root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(expectedProperties.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal) is false ||
            !root.TryGetProperty("protocol", out var protocol) ||
            protocol.ValueKind != JsonValueKind.String ||
            protocol.GetString() != "Electron2D.TaskHumanDecision/1")
        {
            throw new InvalidOperationException("Human decision protocol is invalid.");
        }

        var capability = root.GetProperty("capability").ValueKind == JsonValueKind.String
            ? root.GetProperty("capability").GetString() ?? string.Empty
            : string.Empty;
        var expectedBytes = Encoding.UTF8.GetBytes(context.HumanCapability);
        var actualBytes = Encoding.UTF8.GetBytes(capability);
        if (expectedBytes.Length != actualBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            throw new InvalidOperationException("Human capability does not match.");
        }

        var decisionElement = root.GetProperty("decision");
        var decision = decisionElement.ValueKind == JsonValueKind.String ? decisionElement.GetString() : null;
        if (decision is not ("accept" or "request-changes"))
        {
            throw new InvalidOperationException("Human decision is invalid.");
        }

        var reasonElement = root.GetProperty("reason");
        var reason = reasonElement.ValueKind == JsonValueKind.String ? reasonElement.GetString() : null;
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (reason.Length > 16_384)
        {
            throw new InvalidOperationException("Human decision reason exceeds 16384 characters.");
        }

        return new TaskHumanDecisionBridge(decision, reason);
    }

    private sealed record TaskHumanDecisionBridge(string Decision, string Reason);

    private static TaskHumanMessageBridge ReadHumanMessageBridge(CliExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(context.HumanCapability) ||
            context.HumanCapability.Length is < 32 or > 256)
        {
            throw new InvalidOperationException("Human capability is unavailable.");
        }

        var buffer = new char[65_537];
        var count = context.Input.ReadBlock(buffer, 0, buffer.Length);
        if (count == buffer.Length)
        {
            throw new InvalidOperationException("Human message payload exceeds 64 KiB.");
        }

        using var document = JsonDocument.Parse(new string(buffer, 0, count));
        var root = document.RootElement;
        var expectedProperties = new[] { "protocol", "capability", "text" };
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(expectedProperties.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal) ||
            !root.TryGetProperty("protocol", out var protocol) ||
            protocol.ValueKind != JsonValueKind.String ||
            protocol.GetString() != "Electron2D.TaskHumanMessage/1")
        {
            throw new InvalidOperationException("Human message protocol is invalid.");
        }

        var capability = root.GetProperty("capability").ValueKind == JsonValueKind.String
            ? root.GetProperty("capability").GetString() ?? string.Empty
            : string.Empty;
        var expectedBytes = Encoding.UTF8.GetBytes(context.HumanCapability);
        var actualBytes = Encoding.UTF8.GetBytes(capability);
        if (expectedBytes.Length != actualBytes.Length ||
            !CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes))
        {
            throw new InvalidOperationException("Human capability does not match.");
        }

        var textElement = root.GetProperty("text");
        var text = textElement.ValueKind == JsonValueKind.String ? textElement.GetString() : null;
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (text.Length > 16_384)
        {
            throw new InvalidOperationException("Human message exceeds 16384 characters.");
        }

        return new TaskHumanMessageBridge(text);
    }

    private sealed record TaskHumanMessageBridge(string Text);

    private static int RunTasksCancel(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks cancel", "tasks.cancel", options, output, error,
                store => store.SetStatus(
                    options.Values[1],
                    "Cancelled",
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    context.TaskActorId,
                    context.TaskActorKind,
                    options.GetOption("--reason") ?? "Задача отменена через CLI.",
                    context.NowUtc,
                    options.DryRun));
        }

        return RunTaskMutation(
            "tasks cancel",
            "tasks.cancel",
            options,
            output,
            error,
            store => store.SetStatus(
                options.Values[1],
                ProjectTaskStatus.Cancelled,
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksArchive(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks archive", "tasks.archive", options, output, error,
                store => store.Archive(
                    options.Values[1],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required for v3 archive."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks archive",
            "tasks.archive",
            options,
            output,
            error,
            store => store.Archive(
                options.Values[1],
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksUnarchive(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks unarchive", "tasks.unarchive", options, output, error,
                store => store.Unarchive(
                    options.Values[1],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    options.RequireInt64("--expected-board-revision", "Taskboard revision is required for v3 unarchive."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks unarchive",
            "tasks.unarchive",
            options,
            output,
            error,
            store => store.Unarchive(
                options.Values[1],
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksReopen(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            var statusText = options.GetOption("--status") ?? "Ready";
            if (!string.Equals(statusText, "Ready", StringComparison.OrdinalIgnoreCase))
            {
                return WriteTaskFailure(
                    "tasks reopen", "tasks.reopen", options, NormalizeProjectRoot(options.ProjectRoot),
                    new InvalidOperationException("TaskBoard v3 reopen always returns the task to Ready."), output, error);
            }

            return RunV3TaskMutation(
                "tasks reopen", "tasks.reopen", options, output, error,
                store => store.Reopen(
                    options.Values[1],
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    context.TaskActorId,
                    context.TaskActorKind,
                    options.GetOption("--reason") ?? "Задача открыта заново.",
                    context.NowUtc,
                    options.DryRun));
        }

        return RunTaskMutation(
            "tasks reopen",
            "tasks.reopen",
            options,
            output,
            error,
            store =>
            {
                var statusText = options.GetOption("--status") ?? nameof(ProjectTaskStatus.Ready);
                if (!Enum.TryParse<ProjectTaskStatus>(statusText, ignoreCase: true, out var status))
                {
                    throw new InvalidOperationException($"Task status '{statusText}' is not supported.");
                }

                return store.Reopen(
                    options.Values[1],
                    status,
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli",
                    context.NowUtc,
                    options.DryRun);
            });
    }

    private static int RunTasksVerify(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                var resultV3 = new TaskBoardV3DiskStore(projectRoot).Verify();
                return WriteTaskReadResult(
                    "tasks verify",
                    "tasks.verify",
                    options,
                    projectRoot,
                    new JsonObject
                    {
                        ["valid"] = true,
                        ["version"] = 3,
                        ["activeTaskCount"] = resultV3.ActiveTasks.Count,
                        ["completedTaskCount"] = resultV3.CompletedTasks.Count
                    },
                    output,
                    error);
            }

            var result = new TaskBoardDiskStore(projectRoot).Verify();
            return WriteTaskReadResult(
                "tasks verify",
                "tasks.verify",
                options,
                projectRoot,
                new JsonObject
                {
                    ["valid"] = true,
                    ["activeTaskCount"] = result.ActiveTaskCount,
                    ["completedTaskCount"] = result.CompletedTaskCount
                },
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure("tasks verify", "tasks.verify", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksNormalize(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                return WriteV3BoardMutationResult(
                    "tasks normalize",
                    "tasks.normalize",
                    options,
                    projectRoot,
                    CreateTaskBoardV3Store(projectRoot, options, "tasks normalize").Normalize(
                        options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                        options.DryRun),
                    output,
                    error);
            }

            var result = new TaskBoardDiskStore(projectRoot).Normalize(
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                options.DryRun);
            return WriteResult(
                CliResult.Success(
                    "tasks normalize",
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Task file normalization validated." : "Task files normalized.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = "tasks.normalize",
                        ["boardRevision"] = result.Board.Revision,
                        ["normalizedFileCount"] = result.ChangedFiles.Count,
                        ["dryRun"] = options.DryRun
                    }),
                output,
                error);
        }
        catch (TaskBoardOperationReplayedException replay)
        {
            return WriteV3BoardReplayResult("tasks normalize", "tasks.normalize", options, projectRoot, replay, output, error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure("tasks normalize", "tasks.normalize", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksDelete(CliOptions options, TextWriter output, TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var taskId = options.Values[1];
            if (TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                return WriteV3BoardMutationResult(
                    "tasks delete",
                    "tasks.delete",
                    options,
                    projectRoot,
                    CreateTaskBoardV3Store(projectRoot, options, "tasks delete").Delete(
                        taskId,
                        options.RequireOption("--confirm", "Exact task id confirmation is required."),
                        options.RequireInt64("--expected-revision", "Task revision is required."),
                        options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                        string.Equals(options.GetOption("--delete-attachments"), "true", StringComparison.OrdinalIgnoreCase),
                        options.DryRun),
                    output,
                    error);
            }

            var result = new TaskBoardDiskStore(projectRoot).Delete(
                taskId,
                options.RequireOption("--confirm", "Exact task id confirmation is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                options.RequireInt64("--expected-board-revision", "Taskboard revision is required."),
                string.Equals(options.GetOption("--delete-attachments"), "true", StringComparison.OrdinalIgnoreCase),
                options.DryRun);
            var board = JsonNode.Parse(ProjectTaskSerializer.SerializeBoard(result.Board)) as JsonObject ??
                throw new InvalidOperationException("Serialized board did not produce a JSON object.");
            return WriteResult(
                CliResult.Success(
                    "tasks delete",
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Hard delete validated." : $"Task '{taskId}' permanently deleted.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = "tasks.delete",
                        ["taskId"] = taskId,
                        ["board"] = board,
                        ["dryRun"] = options.DryRun
                    }),
                output,
                error);
        }
        catch (TaskBoardOperationReplayedException replay)
        {
            return WriteV3BoardReplayResult("tasks delete", "tasks.delete", options, projectRoot, replay, output, error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure("tasks delete", "tasks.delete", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksMigrate(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var targetVersion = options.GetOption("--to-version");
        if (targetVersion is not null)
        {
            if (!string.Equals(targetVersion, "3", StringComparison.Ordinal))
            {
                return WriteTaskFailure(
                    "tasks migrate",
                    "tasks.migrate",
                    options,
                    NormalizeProjectRoot(options.ProjectRoot),
                    new InvalidOperationException($"Taskboard target version '{targetVersion}' is not supported."),
                    output,
                    error);
            }

            return RunTasksMigrateToV3(options, output, error, context);
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var apply = string.Equals(options.GetOption("--apply"), "true", StringComparison.OrdinalIgnoreCase);
        var finalize = string.Equals(options.GetOption("--finalize"), "true", StringComparison.OrdinalIgnoreCase);
        if (apply && finalize)
        {
            return WriteTaskFailure(
                "tasks migrate",
                "tasks.migrate",
                options,
                projectRoot,
                new InvalidOperationException("Migration apply and finalize must be separate commands."),
                output,
                error);
        }

        if (!options.DryRun && !apply && !finalize)
        {
            return WriteTaskFailure(
                "tasks migrate",
                "tasks.migrate",
                options,
                projectRoot,
                new InvalidOperationException("Use `--dry-run` first or `--apply true --report-sha <sha256>` after reviewing the report."),
                output,
                error);
        }

        try
        {
            var sourcePaths = new[]
            {
                "TASKS.md",
                "data/completed-tasks/2026/06 Июнь.md",
                "data/completed-tasks/2026/07 Июль.md"
            };
            var sources = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var sourcePath in sourcePaths)
            {
                var fullPath = Path.Combine(projectRoot, sourcePath.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(fullPath))
                {
                    throw new InvalidOperationException($"Legacy migration source '{sourcePath}' was not found.");
                }

                sources[sourcePath] = File.ReadAllBytes(fullPath);
            }

            var plan = LegacyTaskboardMigration.ParseSources(sources, "TASKS.md");
            var tasks = plan.CreateProjectTasks();
            var board = plan.Board ?? throw new InvalidOperationException("Legacy migration did not produce a taskboard.");
            var sourceDigests = new JsonObject();
            foreach (var pair in plan.SourceDigests)
            {
                sourceDigests[pair.Key] = pair.Value;
            }

            var diagnostics = new JsonArray();
            foreach (var diagnostic in plan.Diagnostics.OrderBy(value => value, StringComparer.Ordinal))
            {
                diagnostics.Add(diagnostic);
            }

            var report = new JsonObject
            {
                ["format"] = "Electron2D.TaskMigrationReport",
                ["version"] = 1,
                ["taskCount"] = tasks.Count,
                ["activeTaskCount"] = tasks.Count(task => task.ArchivedAt is null),
                ["completedTaskCount"] = tasks.Count(task => task.ArchivedAt is not null),
                ["dependencyCount"] = tasks.Sum(task => task.Dependencies.Count),
                ["fragmentCount"] = plan.Tasks.Count + plan.BoardFragments.Count,
                ["groupCount"] = board.Groups.Count,
                ["placementCount"] = board.Placements.Count,
                ["sourceDigests"] = sourceDigests,
                ["diagnostics"] = diagnostics
            };
            var reportBytes = Encoding.UTF8.GetBytes(report.ToJsonString());
            var reportSha256 = Convert.ToHexString(SHA256.HashData(reportBytes)).ToLowerInvariant();
            report["reportSha256"] = reportSha256;
            if (apply || finalize)
            {
                var expectedReportSha = options.RequireOption("--report-sha", "Reviewed migration report SHA-256 is required.");
                if (!string.Equals(expectedReportSha, reportSha256, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Migration report digest mismatch: expected '{expectedReportSha}', current '{reportSha256}'. Sources may have changed.");
                }

                if (finalize)
                {
                    var finalizeResult = new TaskBoardDiskStore(projectRoot).FinalizeMigration(
                        tasks,
                        board,
                        plan.SourceDigests.Keys.ToArray(),
                        reportSha256);
                    return WriteResult(
                        CliResult.Success(
                            "tasks migrate",
                            options,
                            projectRoot,
                            CliRoute.None,
                            "Migration finalized and verified legacy sources removed.",
                            finalizeResult.ChangedFiles,
                            dirtyDocuments: [],
                            operation: null,
                            job: null,
                            new JsonObject { ["mode"] = "tasks.migrate", ["report"] = report, ["finalized"] = true }),
                        output,
                        error);
                }

                var result = new TaskBoardDiskStore(projectRoot).ApplyMigration(tasks, board, reportSha256);
                return WriteResult(
                    CliResult.Success(
                        "tasks migrate",
                        options,
                        projectRoot,
                        CliRoute.None,
                        result.ChangedFiles.Count == 0 ? "Migration already matches the verified report." : "Migration applied.",
                        result.ChangedFiles,
                        dirtyDocuments: [],
                        operation: null,
                        job: null,
                        new JsonObject { ["mode"] = "tasks.migrate", ["report"] = report, ["applied"] = true }),
                    output,
                    error);
            }

            return WriteTaskReadResult(
                "tasks migrate",
                "tasks.migrate",
                options,
                projectRoot,
                new JsonObject { ["report"] = report, ["dryRun"] = true },
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception) || exception is System.Text.DecoderFallbackException)
        {
            return WriteTaskFailure("tasks migrate", "tasks.migrate", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksMigrateToV3(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var apply = string.Equals(options.GetOption("--apply"), "true", StringComparison.OrdinalIgnoreCase);
        var finalize = string.Equals(options.GetOption("--finalize"), "true", StringComparison.OrdinalIgnoreCase);
        if (apply && finalize)
        {
            return WriteTaskFailure(
                "tasks migrate", "tasks.migrate.v3", options, projectRoot,
                new InvalidOperationException("Taskboard v3 apply and finalize must be separate commands."), output, error);
        }

        if (finalize)
        {
            try
            {
                var result = new TaskBoardDiskStore(projectRoot).FinalizeV3Migration(
                    options.RequireOption("--report-sha", "Reviewed migration report SHA-256 is required."),
                    options.RequireInt64("--expected-board-revision", "Current taskboard revision is required."),
                    context.NowUtc,
                    options.DryRun);
                return WriteResult(
                    CliResult.Success(
                        "tasks migrate", options, projectRoot, CliRoute.None,
                        options.DryRun ? "Taskboard v3 finalize validated." : "Taskboard v3 migration finalized.",
                        result.ChangedFiles, [], null, null,
                        new JsonObject
                        {
                            ["mode"] = "tasks.migrate.v3",
                            ["finalized"] = true,
                            ["boardRevision"] = result.Board["revision"]!.DeepClone(),
                            ["dryRun"] = options.DryRun
                        }),
                    output,
                    error);
            }
            catch (Exception exception) when (IsTaskboardException(exception))
            {
                return WriteTaskFailure("tasks migrate", "tasks.migrate.v3", options, projectRoot, exception, output, error);
            }
        }

        if (!options.DryRun && !apply)
        {
            return WriteTaskFailure(
                "tasks migrate",
                "tasks.migrate.v3",
                options,
                projectRoot,
                new InvalidOperationException(
                    "Use `--to-version 3 --dry-run` first or add `--apply true --report-sha <sha256> --expected-board-revision <revision>` after reviewing the report."),
                output,
                error);
        }

        try
        {
            var plan = TaskBoardV3Migration.BuildPlan(projectRoot, context.NowUtc);
            TaskBoardV3SemanticValidator.Validate(
                projectRoot,
                plan.Board,
                plan.ActiveTasks,
                plan.CompletedTasks,
                validateAttachmentBlobs: false);
            if (!apply)
            {
                return WriteTaskReadResult(
                    "tasks migrate",
                    "tasks.migrate.v3",
                    options,
                    projectRoot,
                    new JsonObject
                    {
                        ["report"] = plan.Report.DeepClone(),
                        ["dryRun"] = true
                    },
                    output,
                    error);
            }

            var expectedReportSha = options.RequireOption(
                "--report-sha",
                "Reviewed migration report SHA-256 is required.");
            var expectedBoardRevision = options.RequireInt64(
                "--expected-board-revision",
                "Source taskboard revision is required.");
            var result = new TaskBoardDiskStore(projectRoot).ApplyV3Migration(
                expectedReportSha,
                expectedBoardRevision,
                context.NowUtc,
                dryRun: false);
            return WriteResult(
                CliResult.Success(
                    "tasks migrate",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Taskboard v2 was migrated to v3 from the reviewed report.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = "tasks.migrate.v3",
                        ["report"] = plan.Report.DeepClone(),
                        ["applied"] = true
                    }),
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception) || exception is DecoderFallbackException)
        {
            return WriteTaskFailure("tasks migrate", "tasks.migrate.v3", options, projectRoot, exception, output, error);
        }
    }

    private static int RunTasksDependencyAdd(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks dependency add", "tasks.dependency.add", options, output, error,
                store => store.AddDependency(
                    options.Values[2],
                    options.RequireOption("--depends-on", "Dependency task id is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks dependency add",
            "tasks.dependency.add",
            options,
            output,
            error,
            store => store.AddDependency(
                options.Values[2],
                options.RequireOption("--depends-on", "Dependency task id is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksDependencyRemove(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks dependency remove", "tasks.dependency.remove", options, output, error,
                store => store.RemoveDependency(
                    options.Values[2],
                    options.RequireOption("--depends-on", "Dependency task id is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks dependency remove",
            "tasks.dependency.remove",
            options,
            output,
            error,
            store => store.RemoveDependency(
                options.Values[2],
                options.RequireOption("--depends-on", "Dependency task id is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksCommentAdd(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        if (TaskBoardV3DiskStore.IsV3(projectRoot))
        {
            return RunV3TaskMutation(
                "tasks comment add",
                "tasks.comment.add",
                options,
                output,
                error,
                store => store.AddComment(
                    options.Values[2],
                    options.RequireOption("--text", "Comment text is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    context.TaskActorId,
                    context.TaskActorKind,
                    context.NowUtc,
                    options.DryRun,
                    options.GetOption("--agent-run")));
        }

        return RunTaskMutation(
            "tasks comment add",
            "tasks.comment.add",
            options,
            output,
            error,
            store => store.AddComment(
                options.Values[2],
                options.RequireOption("--text", "Comment text is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksContextCheckpoint(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (!TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return WriteTaskFailure(
                "tasks context checkpoint",
                "tasks.context.checkpoint",
                options,
                NormalizeProjectRoot(options.ProjectRoot),
                new InvalidOperationException("Agent context checkpoints are available only for TaskBoard v3."),
                output,
                error);
        }

        return RunV3TaskMutation(
            "tasks context checkpoint",
            "tasks.context.checkpoint",
            options,
            output,
            error,
            store => store.RecordAgentContext(
                options.Values[2],
                options.RequireOption("--agent-run", "Agent run id is required."),
                context.TaskActorId,
                TaskBoardV3Role.Worker,
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun,
                options.GetOption("--rebase-of")));
    }

    private static int RunTasksAttachmentAdd(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks attachment add", "tasks.attachment.add", options, output, error,
                store => store.AddAttachment(
                    options.Values[2],
                    options.RequireOption("--file", "Attachment source file is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks attachment add",
            "tasks.attachment.add",
            options,
            output,
            error,
            store => store.AddAttachment(
                options.Values[2],
                options.RequireOption("--file", "Attachment source file is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                "cli",
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksAttachmentRead(
        CliOptions options,
        TextWriter output,
        TextWriter error)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            if (!TaskBoardV3DiskStore.IsV3(projectRoot))
            {
                throw new InvalidOperationException("Verified attachment retrieval is available only for TaskBoard v3.");
            }

            var retrieval = new TaskBoardV3DiskStore(projectRoot).RetrieveAttachment(
                options.Values[2],
                options.RequireOption("--attachment", "Attachment id is required."),
                options.GetOption("--derivative"));
            return WriteTaskReadResult(
                "tasks attachment read",
                "tasks.attachment.read",
                options,
                projectRoot,
                new JsonObject { ["retrieval"] = retrieval },
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(
                "tasks attachment read",
                "tasks.attachment.read",
                options,
                projectRoot,
                exception,
                output,
                error);
        }
    }

    private static int RunTasksAttachmentRemove(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                "tasks attachment remove", "tasks.attachment.remove", options, output, error,
                store => store.RemoveAttachment(
                    options.Values[2],
                    options.RequireOption("--attachment", "Attachment id is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            "tasks attachment remove",
            "tasks.attachment.remove",
            options,
            output,
            error,
            store => store.RemoveAttachment(
                options.Values[2],
                options.RequireOption("--attachment", "Attachment id is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTasksAttachmentSetPreview(
        CliOptions options,
        TextWriter output,
        TextWriter error,
        CliExecutionContext context,
        bool clear)
    {
        if (TaskBoardV3DiskStore.IsV3(NormalizeProjectRoot(options.ProjectRoot)))
        {
            return RunV3TaskMutation(
                clear ? "tasks attachment clear-preview" : "tasks attachment set-preview",
                clear ? "tasks.attachment.clear-preview" : "tasks.attachment.set-preview",
                options,
                output,
                error,
                store => store.SetAttachmentPreview(
                    options.Values[2],
                    clear ? null : options.RequireOption("--attachment", "Attachment id is required."),
                    options.RequireInt64("--expected-revision", "Task revision is required."),
                    "cli", context.NowUtc, options.DryRun));
        }

        return RunTaskMutation(
            clear ? "tasks attachment clear-preview" : "tasks attachment set-preview",
            clear ? "tasks.attachment.clear-preview" : "tasks.attachment.set-preview",
            options,
            output,
            error,
            store => store.SetAttachmentPreview(
                options.Values[2],
                clear ? null : options.RequireOption("--attachment", "Attachment id is required."),
                options.RequireInt64("--expected-revision", "Task revision is required."),
                context.NowUtc,
                options.DryRun));
    }

    private static int RunTaskMutation(
        string command,
        string mode,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        Func<TaskBoardDiskStore, TaskBoardTaskMutationResult> mutation)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var result = mutation(new TaskBoardDiskStore(projectRoot));
            return WriteResult(
                CliResult.Success(
                    command,
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Task mutation validated." : $"Task '{result.Task.TaskId}' updated.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = mode,
                        ["task"] = TaskSnapshot(result.Task, []),
                        ["dryRun"] = options.DryRun
                    }),
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(command, mode, options, projectRoot, exception, output, error);
        }
    }

    private static int RunV3TaskMutation(
        string command,
        string mode,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        Func<TaskBoardV3DiskStore, TaskBoardV3MutationResult> mutation)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var result = mutation(CreateTaskBoardV3Store(projectRoot, options, command));
            return WriteV3TaskMutationResult(command, mode, options, projectRoot, result, output, error);
        }
        catch (TaskBoardOperationReplayedException replay)
        {
            return WriteV3TaskReplayResult(command, mode, options, projectRoot, replay, output, error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(command, mode, options, projectRoot, exception, output, error);
        }
    }

    private static int WriteV3TaskMutationResult(
        string command,
        string mode,
        CliOptions options,
        string projectRoot,
        TaskBoardV3MutationResult result,
        TextWriter output,
        TextWriter error,
        bool replayed = false)
    {
        var allTasks = result.ActiveTasks.Concat(result.CompletedTasks).ToArray();
        return WriteResult(
            CliResult.Success(
                command,
                options,
                projectRoot,
                CliRoute.None,
                options.DryRun ? "Task mutation validated." : $"Task '{result.Task["taskId"]!.GetValue<string>()}' updated.",
                result.ChangedFiles,
                dirtyDocuments: [],
                operation: null,
                job: null,
                new JsonObject
                {
                    ["mode"] = mode,
                    ["task"] = TaskBoardV3DiskStore.CreateTaskProjection(result.Task, allTasks),
                    ["boardRevision"] = result.Board["revision"]!.DeepClone(),
                    ["replayed"] = replayed,
                    ["dryRun"] = options.DryRun
                }),
            output,
            error);
    }

    private static int RunBoardMutation(
        string command,
        string mode,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        Func<TaskBoardDiskStore, TaskBoardMutationResult> mutation)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var result = mutation(new TaskBoardDiskStore(projectRoot));
            var board = JsonNode.Parse(ProjectTaskSerializer.SerializeBoard(result.Board)) as JsonObject ??
                throw new InvalidOperationException("Serialized board did not produce a JSON object.");
            return WriteResult(
                CliResult.Success(
                    command,
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Taskboard mutation validated." : "Taskboard updated.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject { ["mode"] = mode, ["board"] = board, ["dryRun"] = options.DryRun }),
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(command, mode, options, projectRoot, exception, output, error);
        }
    }

    private static int RunV3BoardMutation(
        string command,
        string mode,
        CliOptions options,
        TextWriter output,
        TextWriter error,
        Func<TaskBoardV3DiskStore, TaskBoardV3BoardMutationResult> mutation)
    {
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        try
        {
            var result = mutation(CreateTaskBoardV3Store(projectRoot, options, command));
            var snapshot = new TaskBoardV3Snapshot(result.Board, result.ActiveTasks, result.CompletedTasks);
            return WriteResult(
                CliResult.Success(
                    command,
                    options,
                    projectRoot,
                    CliRoute.None,
                    options.DryRun ? "Taskboard mutation validated." : "Taskboard updated.",
                    result.ChangedFiles,
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = mode,
                        ["board"] = TaskBoardV3DiskStore.CreateBoardProjection(snapshot),
                        ["dryRun"] = options.DryRun
                    }),
                output,
                error);
        }
        catch (TaskBoardOperationReplayedException)
        {
            var snapshot = new TaskBoardV3DiskStore(projectRoot).LoadSnapshot();
            return WriteResult(
                CliResult.Success(
                    command,
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Taskboard operation was already committed.",
                    changedFiles: [],
                    dirtyDocuments: [],
                    operation: null,
                    job: null,
                    new JsonObject
                    {
                        ["mode"] = mode,
                        ["board"] = TaskBoardV3DiskStore.CreateBoardProjection(snapshot),
                        ["replayed"] = true,
                        ["dryRun"] = false
                    }),
                output,
                error);
        }
        catch (Exception exception) when (IsTaskboardException(exception))
        {
            return WriteTaskFailure(command, mode, options, projectRoot, exception, output, error);
        }
    }

    private static int WriteV3BoardMutationResult(
        string command,
        string mode,
        CliOptions options,
        string projectRoot,
        TaskBoardV3BoardMutationResult result,
        TextWriter output,
        TextWriter error)
    {
        var snapshot = new TaskBoardV3Snapshot(result.Board, result.ActiveTasks, result.CompletedTasks);
        return WriteResult(
            CliResult.Success(
                command,
                options,
                projectRoot,
                CliRoute.None,
                options.DryRun ? "Taskboard mutation validated." : "Taskboard updated.",
                result.ChangedFiles,
                dirtyDocuments: [],
                operation: null,
                job: null,
                new JsonObject
                {
                    ["mode"] = mode,
                    ["board"] = TaskBoardV3DiskStore.CreateBoardProjection(snapshot),
                    ["dryRun"] = options.DryRun
                }),
            output,
            error);
    }

    private static int WriteTaskReadResult(
        string command,
        string mode,
        CliOptions options,
        string projectRoot,
        JsonObject data,
        TextWriter output,
        TextWriter error)
    {
        data["mode"] = mode;
        return WriteResult(
            CliResult.Success(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Taskboard read completed.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                data),
            output,
            error);
    }

    private static long RequireTaskRevision(CliOptions options, string message)
    {
        var value = options.GetOption("--expected-revision") ?? options.GetOption("--expected-task-revision");
        if (value is null || !long.TryParse(value, out var revision))
        {
            throw new InvalidOperationException(message);
        }

        return revision;
    }

    private static int WriteTaskFailure(
        string command,
        string mode,
        CliOptions options,
        string projectRoot,
        Exception exception,
        TextWriter output,
        TextWriter error)
    {
        var writeException = exception as TaskBoardWriteException;
        var data = new JsonObject
        {
            ["mode"] = mode,
            ["retryable"] = writeException?.IsRetryable ?? false,
            ["actualTaskRevision"] = writeException?.ActualTaskRevision,
            ["actualBoardRevision"] = writeException?.ActualBoardRevision
        };
        return WriteResult(
            CliResult.Failure(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Task command failed.",
                CreateCliDiagnostic(writeException?.Code ?? "E2D-CLI-0002", exception.Message),
                data),
            output,
            error);
    }

    private static TaskBoardV3DiskStore CreateTaskBoardV3Store(string projectRoot, CliOptions options, string command)
    {
        return new TaskBoardV3DiskStore(projectRoot, options.CreateTaskBoardWriteOptions(command));
    }

    private static int WriteV3TaskReplayResult(
        string command,
        string mode,
        CliOptions options,
        string projectRoot,
        TaskBoardOperationReplayedException replay,
        TextWriter output,
        TextWriter error)
    {
        if (string.IsNullOrWhiteSpace(replay.TaskId))
        {
            return WriteTaskFailure(
                command,
                mode,
                options,
                projectRoot,
                new TaskBoardWriteException(
                    "E2D-TASK-0007",
                    "Committed operation receipt does not identify a task result.",
                    isRetryable: false),
                output,
                error);
        }

        var store = new TaskBoardV3DiskStore(projectRoot);
        var snapshot = store.LoadSnapshot();
        var task = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).Single(candidate =>
            string.Equals(candidate["taskId"]?.GetValue<string>(), replay.TaskId, StringComparison.Ordinal));
        var result = new TaskBoardV3MutationResult(
            task,
            snapshot.Board,
            snapshot.ActiveTasks,
            snapshot.CompletedTasks,
            ChangedFiles: [],
            DryRun: false);
        return WriteV3TaskMutationResult(command, mode, options, projectRoot, result, output, error, replayed: true);
    }

    private static int WriteV3BoardReplayResult(
        string command,
        string mode,
        CliOptions options,
        string projectRoot,
        TaskBoardOperationReplayedException replay,
        TextWriter output,
        TextWriter error)
    {
        var snapshot = new TaskBoardV3DiskStore(projectRoot).LoadSnapshot();
        return WriteResult(
            CliResult.Success(
                command,
                options,
                projectRoot,
                CliRoute.None,
                "Taskboard operation was already committed.",
                changedFiles: [],
                dirtyDocuments: [],
                operation: null,
                job: null,
                new JsonObject
                {
                    ["mode"] = mode,
                    ["taskId"] = string.IsNullOrWhiteSpace(replay.TaskId) ? null : replay.TaskId,
                    ["board"] = TaskBoardV3DiskStore.CreateBoardProjection(snapshot),
                    ["replayed"] = true,
                    ["dryRun"] = false
                }),
            output,
            error);
    }

    private static JsonObject TaskSnapshot(ProjectTask task, IReadOnlyList<ProjectTask> tasks)
    {
        var snapshot = JsonNode.Parse(ProjectTaskSerializer.Serialize(task)) as JsonObject ??
            throw new InvalidOperationException("Serialized task did not produce a JSON object.");
        var readiness = TaskDependencyGraph.RefreshReadiness(task, tasks);
        snapshot["readiness"] = readiness.Task.Readiness.ToString();
        snapshot["boardStatus"] = TaskDependencyGraph.ResolveBoardStatus(task, tasks).ToString();
        return snapshot;
    }

    private static JsonObject TaskCardSnapshot(ProjectTask task, IReadOnlyList<ProjectTask> tasks)
    {
        var readiness = TaskDependencyGraph.RefreshReadiness(task, tasks).Task.Readiness;
        var snapshot = new JsonObject
        {
            ["taskId"] = task.TaskId,
            ["revision"] = task.Revision,
            ["title"] = task.Title,
            ["status"] = task.Status.ToString(),
            ["boardStatus"] = TaskDependencyGraph.ResolveBoardStatus(task, tasks).ToString(),
            ["priority"] = task.Priority,
            ["labels"] = WriteTaskStringArray(task.Labels),
            ["deadline"] = task.Deadline is null
                ? null
                : JsonValue.Create(task.Deadline.Value.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)),
            ["acceptanceCriteriaProgress"] = new JsonObject
            {
                ["passed"] = task.AcceptanceCriteria.Count(criterion => criterion.State == AcceptanceCriterionState.Passed),
                ["total"] = task.AcceptanceCriteria.Count
            },
            ["attachmentCount"] = task.Attachments.Count,
            ["assignee"] = task.Assignee,
            ["parentTaskId"] = task.ParentTaskId,
            ["dependencies"] = WriteTaskStringArray(task.Dependencies),
            ["readiness"] = readiness.ToString(),
            ["acceptanceState"] = task.AcceptanceState.ToString(),
            ["archivedAt"] = task.ArchivedAt is null ? null : JsonValue.Create(task.ArchivedAt.Value)
        };
        var preview = TaskAttachmentPreview.Resolve(task);
        if (preview is not null)
        {
            snapshot["cardPreview"] = new JsonObject
            {
                ["attachmentId"] = preview.AttachmentId,
                ["displayName"] = preview.DisplayName,
                ["relativePath"] = preview.RelativePath,
                ["mediaType"] = preview.MediaType
            };
        }

        return snapshot;
    }

    private static JsonArray WriteTaskStringArray(IEnumerable<string> values)
    {
        var result = new JsonArray();
        foreach (var value in values)
        {
            result.Add(value);
        }

        return result;
    }

    private static bool IsTaskboardException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or FormatException or InvalidOperationException or ArgumentException;
    }
}
