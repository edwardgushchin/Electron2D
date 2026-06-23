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
    private static int RunTasks(CliOptions options, TextWriter output, TextWriter error)
    {
        if (options.Values.Count != 1 || !string.Equals(options.Values[0], "export", StringComparison.OrdinalIgnoreCase))
        {
            return WriteResult(
                CliResult.Blocked(
                    BuildCommandName("tasks", options),
                    options,
                    "Unknown tasks command.",
                    CreateCliDiagnostic("E2D-CLI-0001", "`e2d tasks export --format markdown` is the implemented Project Tasks report command.")),
                output,
                error);
        }

        var projectRoot = NormalizeProjectRoot(options.ProjectRoot);
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
        ProjectTaskStatus.Backlog,
        ProjectTaskStatus.Ready,
        ProjectTaskStatus.InProgress,
        ProjectTaskStatus.Blocked,
        ProjectTaskStatus.Review,
        ProjectTaskStatus.AwaitingAcceptance,
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
        var tasksRoot = Path.Combine(projectRoot, ".electron2d", "tasks");
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
        var boardPath = Path.Combine(projectRoot, ProjectTaskStorage.BoardDocumentPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(boardPath))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        var board = ProjectTaskSerializer.DeserializeBoard(ProjectTaskStorage.BoardDocumentPath, File.ReadAllText(boardPath));
        var order = new Dictionary<string, int>(StringComparer.Ordinal);
        var index = 0;
        foreach (var taskId in board.Columns.SelectMany(column => column.TaskIds))
        {
            if (!order.ContainsKey(taskId))
            {
                order[taskId] = index++;
            }
        }

        return order;
    }

    private static string WriteMarkdown(
        IReadOnlyList<ProjectTask> tasks,
        IReadOnlyDictionary<string, int> boardOrder,
        ProjectTaskReportQuery query)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Project Tasks Report");
        builder.AppendLine();
        builder.AppendLine("> Markdown report only. Canonical task storage stays in `.electron2d/tasks/*.e2task` and `.electron2d/tasks/board.e2tasks`.");
        builder.AppendLine();
        builder.AppendLine("- Source: `.electron2d/tasks/*.e2task`");
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
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Rank: {Escape(task.Rank)}");
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
        return ".electron2d/tasks/" + Path.GetFileName(path).Replace('\\', '/');
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
