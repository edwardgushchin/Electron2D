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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.ProjectSystem;

internal static partial class Electron2DCommandLine
{
    private static int RunTasks(CliOptions options, TextWriter output, TextWriter error, CliExecutionContext context)
    {
        if (options.Values.Count == 0)
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("tasks", options),
                    options,
                    "Unknown tasks command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "Use a documented `e2d tasks` command.")),
                output,
                error);
        }

        var commandRoot = options.Values[0].ToLowerInvariant();
        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        var boardPath = Path.Combine(projectRoot, ProjectTaskStorage.BoardDocumentPath.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(boardPath) && !TaskBoardV3DiskStore.IsV3(projectRoot) &&
            commandRoot is not ("board" or "list" or "get" or "verify" or "migrate" or "export"))
        {
            return WriteTaskFailure(
                BuildCommandName("tasks", options),
                "tasks.v2-read-only",
                options,
                projectRoot,
                new InvalidOperationException("TaskBoard v2 is read-only. Run `e2d tasks migrate --to-version 3 --dry-run` and apply the reviewed migration before mutating tasks."),
                output,
                error);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "init", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksInit(options, output, error);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "create", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksCreate(options, output, error, context);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "list", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksList(options, output, error);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "board", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksBoard(options, output, error);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "move", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksMove(options, output, error);
        }

        if (options.Values.Count == 2 &&
            string.Equals(options.Values[0], "group", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "add", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksGroupAdd(options, output, error);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "group", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "update", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksGroupUpdate(options, output, error);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "group", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "remove", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksGroupRemove(options, output, error);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "parent", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "set", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksParentSet(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "parent", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "clear", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksParentClear(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "get", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksGet(options, output, error);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "update", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksUpdate(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "set-status", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksSetStatus(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "submit", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksSubmit(options, output, error, context);
        }

        if (options.Values.Count == 2 &&
            (string.Equals(options.Values[0], "accept", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(options.Values[0], "request-changes", StringComparison.OrdinalIgnoreCase)))
        {
            return RunTasksHumanDecisionUnavailable(options, output, error);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "__human-decision", StringComparison.Ordinal))
        {
            return RunTasksHumanDecision(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "__human-message", StringComparison.Ordinal))
        {
            return RunTasksHumanMessage(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "cancel", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksCancel(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "archive", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksArchive(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "unarchive", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksUnarchive(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "reopen", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksReopen(options, output, error, context);
        }

        if (options.Values.Count == 2 && string.Equals(options.Values[0], "delete", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksDelete(options, output, error);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "verify", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksVerify(options, output, error);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "normalize", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksNormalize(options, output, error);
        }

        if (options.Values.Count == 1 && string.Equals(options.Values[0], "migrate", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksMigrate(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "dependency", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "add", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksDependencyAdd(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "dependency", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "remove", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksDependencyRemove(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "comment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "add", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksCommentAdd(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "context", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "checkpoint", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksContextCheckpoint(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "criterion", StringComparison.OrdinalIgnoreCase))
        {
            return options.Values[1].ToLowerInvariant() switch
            {
                "add" => RunTasksCriterionAdd(options, output, error, context),
                "update" => RunTasksCriterionUpdate(options, output, error, context),
                "add-evidence" => RunTasksCriterionAddEvidence(options, output, error, context),
                "set-state" => RunTasksCriterionSetState(options, output, error, context),
                "remove" => RunTasksCriterionRemove(options, output, error, context),
                _ => WriteResult(
                    CliResult.Blocked(
                        BuildCommandName("tasks", options),
                        options,
                        "Unknown tasks criterion command.",
                        CreateCliDiagnostic("E2D-CLI-0001", $"Criterion command '{options.Values[1]}' is not implemented.")),
                    output,
                    error)
            };
        }

        if (options.Values.Count == 2 &&
            string.Equals(options.Values[0], "tag", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "create", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksTagCreate(options, output, error, context);
        }

        if (options.Values.Count == 2 &&
            string.Equals(options.Values[0], "tag", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "apply", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksTagApply(options, output, error, context);
        }

        if (options.Values.Count == 3 && string.Equals(options.Values[0], "tag", StringComparison.OrdinalIgnoreCase))
        {
            return options.Values[1].ToLowerInvariant() switch
            {
                "update" => RunTasksTagUpdate(options, output, error),
                "delete" => RunTasksTagDelete(options, output, error),
                "assign" => RunTasksTagAssign(options, output, error, context, assign: true),
                "unassign" => RunTasksTagAssign(options, output, error, context, assign: false),
                _ => WriteResult(
                    CliResult.Blocked(
                        BuildCommandName("tasks", options),
                        options,
                        "Unknown tasks tag command.",
                        CreateCliDiagnostic("E2D-CLI-0001", $"Tag command '{options.Values[1]}' is not implemented.")),
                    output,
                    error)
            };
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "attachment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "add", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksAttachmentAdd(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "attachment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "read", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksAttachmentRead(options, output, error);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "attachment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "remove", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksAttachmentRemove(options, output, error, context);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "attachment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "set-preview", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksAttachmentSetPreview(options, output, error, context, clear: false);
        }

        if (options.Values.Count == 3 &&
            string.Equals(options.Values[0], "attachment", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(options.Values[1], "clear-preview", StringComparison.OrdinalIgnoreCase))
        {
            return RunTasksAttachmentSetPreview(options, output, error, context, clear: true);
        }

        if (options.Values.Count != 1 || !string.Equals(options.Values[0], "export", StringComparison.OrdinalIgnoreCase))
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("tasks", options),
                    options,
                    "Unknown tasks command.",
                    CreateCliDiagnostic("E2D-CLI-0001", $"Tasks command '{options.Values[0]}' is not implemented.")),
                output,
                error);
        }

        projectRoot = NormalizeProjectRoot(options.ProjectRoot);
        if (options.Format is not (CliOutputFormat.Text or CliOutputFormat.Markdown))
        {
            return WriteResult(
                CliResult.Failure(
                    "tasks export",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Project Tasks export supports Markdown output only.",
                    CreateCliDiagnostic("E2D-CLI-0002", "Use `--format markdown` for `e2d tasks export`."),
                    new JsonObject
                    {
                        ["mode"] = "tasks.export"
                    }),
                output,
                error);
        }

        try
        {
            output.Write(ProjectTasksMarkdownReport.Build(projectRoot, options));
            return 0;
        }
        catch (CliCommandException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException or JsonException)
        {
            return WriteResult(
                CliResult.Failure(
                    "tasks export",
                    options,
                    projectRoot,
                    CliRoute.None,
                    "Project Tasks report export failed.",
                    CreateCliDiagnostic("E2D-CLI-0002", exception.Message),
                    new JsonObject
                    {
                        ["mode"] = "tasks.export"
                    }),
                output,
                error);
        }
    }
}

internal static class ProjectTasksMarkdownReport
{
    private static readonly ProjectTaskStatus[] StatusOrder =
    [
        ProjectTaskStatus.Ready,
        ProjectTaskStatus.InProgress,
        ProjectTaskStatus.Blocked,
        ProjectTaskStatus.Review,
        ProjectTaskStatus.Done,
        ProjectTaskStatus.Cancelled
    ];

    public static string Build(string projectRoot, CliOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(options);

        var query = ProjectTaskReportQuery.FromOptions(options);
        var tasks = LoadTasks(projectRoot);
        var boardOrder = LoadBoardOrder(projectRoot);
        var filtered = tasks.Where(query.Matches).ToArray();
        return WriteMarkdown(filtered, boardOrder, query);
    }

    private static IReadOnlyList<ProjectTask> LoadTasks(string projectRoot)
    {
        if (TaskBoardV3DiskStore.IsV3(projectRoot))
        {
            var snapshot = new TaskBoardV3DiskStore(projectRoot).Verify();
            var all = snapshot.ActiveTasks.Concat(snapshot.CompletedTasks).ToArray();
            return snapshot.ActiveTasks.Select(task => TaskBoardV3DiskStore.CreateCompatibilityTask(task, all)).ToArray();
        }

        var tasksRoot = Path.Combine(projectRoot, ProjectTaskStorage.ActiveTasksDirectory.Replace('/', Path.DirectorySeparatorChar));
        if (!Directory.Exists(tasksRoot))
        {
            return [];
        }

        var tasks = new List<ProjectTask>();
        foreach (var path in Directory.EnumerateFiles(tasksRoot, "*.e2task", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            tasks.Add(ProjectTaskSerializer.DeserializeTask(ToTaskRelativePath(path), File.ReadAllText(path)));
        }

        return tasks;
    }

    private static IReadOnlyDictionary<string, int> LoadBoardOrder(string projectRoot)
    {
        if (TaskBoardV3DiskStore.IsV3(projectRoot))
        {
            var snapshot = new TaskBoardV3DiskStore(projectRoot).Verify();
            var projection = TaskBoardV3DiskStore.CreateBoardProjection(snapshot);
            return projection["placements"]!.AsArray().Select(node => node!.AsObject())
                .OrderBy(placement => placement["rank"]!.GetValue<string>(), StringComparer.Ordinal)
                .Select((placement, index) => (TaskId: placement["taskId"]!.GetValue<string>(), Index: index))
                .ToDictionary(item => item.TaskId, item => item.Index, StringComparer.Ordinal);
        }

        var boardPath = Path.Combine(projectRoot, ProjectTaskStorage.BoardDocumentPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(boardPath))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        var board = ProjectTaskSerializer.DeserializeBoard(ProjectTaskStorage.BoardDocumentPath, File.ReadAllText(boardPath));
        var order = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var taskId in board.Placements
            .OrderBy(placement => placement.Rank, StringComparer.Ordinal)
            .ThenBy(placement => placement.TaskId, StringComparer.Ordinal)
            .Select(placement => placement.TaskId))
        {
            if (!order.ContainsKey(taskId))
            {
                order[taskId] = index++;
            }
        }

        return order;
    }

    private static ProjectTask ToReportTask(JsonObject task)
    {
        var acceptanceState = task["acceptanceState"]!.GetValue<string>() switch
        {
            "Submitted" => ProjectTaskAcceptanceState.Submitted,
            "Accepted" => ProjectTaskAcceptanceState.Accepted,
            "ChangesRequested" => ProjectTaskAcceptanceState.ChangesRequested,
            "Cancelled" => ProjectTaskAcceptanceState.Cancelled,
            _ => ProjectTaskAcceptanceState.Open
        };
        var result = new ProjectTask
        {
            TaskUid = task["taskUid"]!.GetValue<string>(),
            Revision = task["revision"]!.GetValue<long>(),
            TaskId = task["taskId"]!.GetValue<string>(),
            Title = task["title"]!.GetValue<string>(),
            Description = task["description"]!.GetValue<string>(),
            Status = Enum.Parse<ProjectTaskStatus>(task["status"]!.GetValue<string>(), ignoreCase: false),
            Priority = task["priority"]!.GetValue<string>(),
            CreatedBy = task["createdBy"]!.GetValue<string>(),
            CreatedAt = task["createdAt"]!.GetValue<DateTimeOffset>(),
            UpdatedAt = task["updatedAt"]!.GetValue<DateTimeOffset>(),
            SubmittedAt = ReadOptionalTimestamp(task, "submittedAt"),
            CompletedAt = ReadOptionalTimestamp(task, "completedAt"),
            AcceptedAt = ReadOptionalTimestamp(task, "acceptedAt"),
            AcceptedBy = task["acceptedBy"]?.GetValue<string>(),
            AcceptanceState = acceptanceState,
            ArchivedAt = ReadOptionalTimestamp(task, "archivedAt"),
            ArchivedBy = task["archivedBy"]?.GetValue<string>(),
            CancellationReason = task["cancellationReason"]?.GetValue<string>(),
            ParentTaskId = task["parentTaskId"]?.GetValue<string>(),
            Deadline = task["deadline"] is null
                ? null
                : DateOnly.ParseExact(task["deadline"]!.GetValue<string>(), "yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
        result.Labels.AddRange(task["labels"]!.AsArray().Select(node => node!.GetValue<string>()));
        result.Dependencies.AddRange(task["dependencies"]!.AsArray().Select(node => node!.GetValue<string>()));
        result.Subtasks.AddRange(task["subtasks"]!.AsArray().Select(node => node!.GetValue<string>()));
        foreach (var criterionNode in task["acceptanceCriteria"]!.AsArray())
        {
            var criterion = criterionNode!.AsObject();
            result.AcceptanceCriteria.Add(new AcceptanceCriterion(
                criterion["criterionId"]!.GetValue<string>(),
                criterion["description"]!.GetValue<string>(),
                Enum.Parse<AcceptanceCriterionState>(criterion["state"]!.GetValue<string>(), ignoreCase: false),
                criterion["evidenceLinks"]!.AsArray().Select(node => node!.GetValue<string>()).ToArray()));
        }

        foreach (var activityNode in task["activity"]!.AsArray())
        {
            var activity = activityNode!.AsObject();
            var kindText = activity["kind"]!.GetValue<string>();
            result.Activity.Add(new TaskActivityEntry(
                activity["activityEntryId"]!.GetValue<string>(),
                activity["actorId"]!.GetValue<string>(),
                Enum.Parse<PrincipalKind>(activity["actorKind"]!.GetValue<string>(), ignoreCase: false),
                activity["createdAt"]!.GetValue<DateTimeOffset>(),
                Enum.TryParse<TaskActivityKind>(kindText, ignoreCase: false, out var kind) ? kind : TaskActivityKind.Decision,
                activity["payload"]!.GetValue<string>()));
        }

        return result;
    }

    private static DateTimeOffset? ReadOptionalTimestamp(JsonObject value, string propertyName)
    {
        return value[propertyName] is null ? null : value[propertyName]!.GetValue<DateTimeOffset>();
    }

    private static string WriteMarkdown(
        IReadOnlyList<ProjectTask> tasks,
        IReadOnlyDictionary<string, int> boardOrder,
        ProjectTaskReportQuery query)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Project Tasks Report");
        builder.AppendLine();
        builder.AppendLine("> Markdown report only. Canonical task storage stays in `.taskboard/tasks/*.e2task` and `.taskboard/board.e2tasks`.");
        builder.AppendLine();
        builder.AppendLine("- Source: `.taskboard/tasks/*.e2task`");
        builder.AppendLine($"- Filters: {query.FiltersText}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Task count: {tasks.Count}");
        builder.AppendLine();

        foreach (var group in tasks
            .GroupBy(task => task.Status)
            .OrderBy(group => StatusIndex(group.Key)))
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"## {group.Key}");
            builder.AppendLine();
            var orderedTasks = OrderGroup(group, boardOrder).ToArray();
            for (var index = 0; index < orderedTasks.Length; index++)
            {
                WriteTask(builder, orderedTasks[index]);
                if (index + 1 < orderedTasks.Length)
                {
                    builder.AppendLine();
                }
            }
        }

        if (tasks.Count == 0)
        {
            builder.AppendLine("No tasks matched the selected filters.");
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static IEnumerable<ProjectTask> OrderGroup(
        IGrouping<ProjectTaskStatus, ProjectTask> group,
        IReadOnlyDictionary<string, int> boardOrder)
    {
        if (group.Key == ProjectTaskStatus.Done)
        {
            return group
                .OrderByDescending(task => task.CompletedAt ?? DateTimeOffset.MinValue)
                .ThenBy(task => task.TaskId, StringComparer.Ordinal);
        }

        return group
            .OrderBy(task => boardOrder.TryGetValue(task.TaskId, out var order) ? order : int.MaxValue)
            .ThenBy(task => task.Rank, StringComparer.Ordinal)
            .ThenBy(task => task.CreatedAt)
            .ThenBy(task => task.TaskId, StringComparer.Ordinal);
    }

    private static void WriteTask(StringBuilder builder, ProjectTask task)
    {
        builder.AppendLine(CultureInfo.InvariantCulture, $"### {Escape(task.TaskId)} - {Escape(task.Title)}");
        builder.AppendLine();
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Status: {task.Status}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Priority: {Escape(task.Priority)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Assignee: {Escape(task.Assignee ?? "unassigned")}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Labels: {LabelsText(task)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Created: {FormatDate(task.CreatedAt)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Completed: {FormatOptionalDate(task.CompletedAt)}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Accepted: {AcceptedText(task)}");
        WriteCriteria(builder, task);
        WriteActivity(builder, task);
    }

    private static void WriteCriteria(StringBuilder builder, ProjectTask task)
    {
        builder.AppendLine("- Criteria:");
        if (task.AcceptanceCriteria.Count == 0)
        {
            builder.AppendLine("  - none");
            return;
        }

        foreach (var criterion in task.AcceptanceCriteria.OrderBy(criterion => criterion.CriterionId, StringComparer.Ordinal))
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  - [{CriterionMarker(criterion.State)}] {Escape(criterion.CriterionId)}: {Escape(criterion.Description)}");
        }
    }

    private static void WriteActivity(StringBuilder builder, ProjectTask task)
    {
        builder.AppendLine("- Activity:");
        var entries = task.Activity
            .OrderBy(entry => entry.CreatedAt)
            .ThenBy(entry => entry.ActivityEntryId, StringComparer.Ordinal)
            .TakeLast(3)
            .ToArray();
        if (entries.Length == 0)
        {
            builder.AppendLine("  - none");
            return;
        }

        foreach (var entry in entries)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"  - {FormatDate(entry.CreatedAt)} {entry.ActorKind} {Escape(entry.ActorId)}: {entry.Kind} - {Escape(entry.Payload)}");
        }
    }

    private static string ToTaskRelativePath(string path)
    {
        return ProjectTaskStorage.ActiveTasksDirectory + "/" + Path.GetFileName(path).Replace('\\', '/');
    }

    private static int StatusIndex(ProjectTaskStatus status)
    {
        var index = Array.IndexOf(StatusOrder, status);
        return index < 0 ? int.MaxValue : index;
    }

    private static string LabelsText(ProjectTask task)
    {
        return task.Labels.Count == 0
            ? "none"
            : string.Join(", ", task.Labels.OrderBy(label => label, StringComparer.Ordinal).Select(Escape));
    }

    private static string AcceptedText(ProjectTask task)
    {
        return task.AcceptedAt is null
            ? "n/a"
            : $"{FormatDate(task.AcceptedAt.Value)} by {Escape(task.AcceptedBy ?? "unknown")}";
    }

    private static string FormatOptionalDate(DateTimeOffset? value)
    {
        return value is null ? "n/a" : FormatDate(value.Value);
    }

    private static string FormatDate(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
    }

    private static string CriterionMarker(AcceptanceCriterionState state)
    {
        return state switch
        {
            AcceptanceCriterionState.Passed => "x",
            AcceptanceCriterionState.Failed => "!",
            _ => " "
        };
    }

    private static string Escape(string value)
    {
        return value.ReplaceLineEndings(" ").Trim();
    }

    private sealed class ProjectTaskReportQuery
    {
        private ProjectTaskReportQuery(
            ProjectTaskStatus? status,
            string? milestone,
            string? version,
            string? epic,
            string? assignee,
            string? agentSession)
        {
            Status = status;
            Milestone = milestone;
            Version = version;
            Epic = epic;
            Assignee = assignee;
            AgentSession = agentSession;
        }

        private ProjectTaskStatus? Status { get; }

        private string? Milestone { get; }

        private string? Version { get; }

        private string? Epic { get; }

        private string? Assignee { get; }

        private string? AgentSession { get; }

        public string FiltersText
        {
            get
            {
                var filters = new List<string>();
                if (Status is not null)
                {
                    filters.Add($"status={Status}");
                }

                AddFilter(filters, "milestone", Milestone);
                AddFilter(filters, "version", Version);
                AddFilter(filters, "epic", Epic);
                AddFilter(filters, "assignee", Assignee);
                AddFilter(filters, "agent-session", AgentSession);
                return filters.Count == 0 ? "none" : string.Join(", ", filters);
            }
        }

        public static ProjectTaskReportQuery FromOptions(CliOptions options)
        {
            var statusText = options.GetOption("--status");
            ProjectTaskStatus? status = null;
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                if (!Enum.TryParse<ProjectTaskStatus>(statusText, ignoreCase: true, out var parsedStatus))
                {
                    throw new CliCommandException(
                        "tasks export",
                        options,
                        $"Task status '{statusText}' is not supported.",
                        Electron2DCommandLine.CreateCliDiagnostic("E2D-CLI-0002", $"Task status '{statusText}' is not supported."));
                }

                status = parsedStatus;
            }

            return new ProjectTaskReportQuery(
                status,
                NormalizeFilter(options.GetOption("--milestone")),
                NormalizeFilter(options.GetOption("--version")),
                NormalizeFilter(options.GetOption("--epic")),
                NormalizeFilter(options.GetOption("--assignee")),
                NormalizeFilter(options.GetOption("--agent-session")));
        }

        public bool Matches(ProjectTask task)
        {
            return (Status is null || task.Status == Status.Value) &&
                MatchesAssignee(task) &&
                MatchesLabel(task, "milestone", Milestone) &&
                MatchesLabel(task, "version", Version) &&
                MatchesLabel(task, "epic", Epic) &&
                MatchesAgentSession(task);
        }

        private static void AddFilter(ICollection<string> filters, string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                filters.Add($"{name}={value}");
            }
        }

        private static string? NormalizeFilter(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private bool MatchesAssignee(ProjectTask task)
        {
            return Assignee is null ||
                string.Equals(task.Assignee, Assignee, StringComparison.OrdinalIgnoreCase);
        }

        private static bool MatchesLabel(ProjectTask task, string prefix, string? value)
        {
            return value is null || task.Labels.Any(label =>
                string.Equals(label, $"{prefix}:{value}", StringComparison.OrdinalIgnoreCase));
        }

        private bool MatchesAgentSession(ProjectTask task)
        {
            return AgentSession is null ||
                MatchesLabel(task, "agent-session", AgentSession) ||
                task.Activity.Any(entry => ActivityPayloadMatchesAgentSession(entry.Payload, AgentSession));
        }

        private static bool ActivityPayloadMatchesAgentSession(string payload, string agentSession)
        {
            foreach (var part in payload.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var separator = part.IndexOf('=', StringComparison.Ordinal);
                if (separator < 0)
                {
                    continue;
                }

                var key = part[..separator].Trim();
                var value = part[(separator + 1)..].Trim();
                if ((key.Equals("AgentSessionId", StringComparison.OrdinalIgnoreCase) ||
                    key.Equals("agentSession", StringComparison.OrdinalIgnoreCase)) &&
                    value.Equals(agentSession, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
