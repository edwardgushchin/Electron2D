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

namespace Electron2D.Editor.ProjectTasks;

internal sealed class ProjectTasksBoard
{
    private static readonly string[] WorkspaceSwitcher = ["2D", "Script", "Game", "Tasks"];
    private static readonly string[] Filters = ["Status", "Priority", "Labels", "Assignee", "Text", "Linked Object"];

    public ProjectTasksBoard(ProjectTasksBoardSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    public ProjectTasksBoardSnapshot Snapshot { get; }

    public static ProjectTasksBoardSnapshot CreateSmokeSnapshot()
    {
        var cards = new[]
        {
            new ProjectTasksCardSnapshot(
                "T-0155",
                "Project Tasks board UI",
                ProjectTaskStatus.Ready,
                "P0",
                labels: ["editor", "tasks", "manual"],
                "engineer",
                TaskReadiness.Ready,
                blockingReasons: [],
                "rank-010",
                archived: false),
            new ProjectTasksCardSnapshot(
                "T-review",
                "Review diagnostics handoff",
                ProjectTaskStatus.Review,
                "P0",
                labels: ["review"],
                "qa",
                TaskReadiness.Ready,
                blockingReasons: [],
                "rank-020",
                archived: false),
            new ProjectTasksCardSnapshot(
                "T-blocked",
                "Manual blocker example",
                ProjectTaskStatus.Blocked,
                "P1",
                labels: ["blocked"],
                "artist",
                TaskReadiness.BlockedByDependencies,
                blockingReasons: [TaskBlockingReason.Manual, TaskBlockingReason.Dependency],
                "rank-030",
                archived: false),
            new ProjectTasksCardSnapshot(
                "T-done",
                "Accepted task sample",
                ProjectTaskStatus.Done,
                "P2",
                labels: ["accepted"],
                "lead",
                TaskReadiness.Ready,
                blockingReasons: [],
                "rank-040",
                archived: false),
            new ProjectTasksCardSnapshot(
                "T-archived",
                "Archived hidden sample",
                ProjectTaskStatus.Cancelled,
                "P2",
                labels: ["archive"],
                "lead",
                TaskReadiness.Ready,
                blockingReasons: [],
                "rank-050",
                archived: true)
        };

        var selected = cards[0];
        return new ProjectTasksBoardSnapshot(
            WorkspaceSwitcher,
            "Tasks",
            CreateColumns(cards),
            cards,
            selected,
            new ProjectTasksDetailsSnapshot(
                "Task Details",
                DescriptionVisible: true,
                AcceptanceCriteriaVisible: true,
                SubtasksVisible: true,
                ActivityKinds:
                [
                    TaskActivityKind.Comment,
                    TaskActivityKind.Decision,
                    TaskActivityKind.Investigation,
                    TaskActivityKind.Blocker,
                    TaskActivityKind.TestResult,
                    TaskActivityKind.StatusChange,
                    TaskActivityKind.AgentSummary,
                    TaskActivityKind.AcceptanceResult
                ],
                LinkedTransactions: ["transaction://txn-task-001"],
                LinkedJobs: ["job://job-run-001"],
                LinkedDiagnostics: ["E2D-TASK-0003"],
                LinkedArtifacts: ["artifact://screenshots/tasks-board.png", "artifact://runtime/tree.json"],
                LinkedObjects: ["res://scenes/main.e2scene.json", "res://textures/player.png", "/root/Player"]),
            new ProjectTasksDragDropIntent(
                "T-0155",
                ProjectTaskStatus.Ready,
                ProjectTaskStatus.InProgress,
                "rank-020",
                Allowed: true,
                RejectedDiagnosticCode: "E2D-TASK-0002"),
            [
                new ProjectTasksAction("Accept", Enabled: true, Clickable: true, RequiresTrustedHumanContext: true),
                new ProjectTasksAction("Request Changes", Enabled: true, Clickable: true, RequiresTrustedHumanContext: true),
                new ProjectTasksAction("Cancel", Enabled: true, Clickable: true, RequiresTrustedHumanContext: false),
                new ProjectTasksAction("Create", Enabled: true, Clickable: true, RequiresTrustedHumanContext: false),
                new ProjectTasksAction("Edit", Enabled: true, Clickable: true, RequiresTrustedHumanContext: false),
                new ProjectTasksAction("Archive", Enabled: true, Clickable: true, RequiresTrustedHumanContext: false),
                new ProjectTasksAction("Hard Delete", Enabled: true, Clickable: true, RequiresTrustedHumanContext: false, DestructiveConfirmationRequired: true)
            ],
            Filters,
            rankRoundTripStable: true,
            archiveViewAvailable: true,
            archivedHiddenFromBoard: true,
            hardDeleteRequiresConfirmation: true,
            humanAcceptActionUsesTrustedContext: true,
            agentAcceptActionAvailable: false,
            worksWithoutAi: true,
            workspaceEventRevision: new ProjectWorkspaceRevision(43));
    }

    private static IReadOnlyList<ProjectTasksColumnSnapshot> CreateColumns(IReadOnlyList<ProjectTasksCardSnapshot> cards)
    {
        var statuses = new[]
        {
            ProjectTaskStatus.Ready,
            ProjectTaskStatus.InProgress,
            ProjectTaskStatus.Blocked,
            ProjectTaskStatus.Review,
            ProjectTaskStatus.Done,
            ProjectTaskStatus.Cancelled
        };

        return statuses
            .Select(status => new ProjectTasksColumnSnapshot(
                status,
                DisplayStatus(status),
                cards.Where(card => card.Status == status && !card.Archived).OrderBy(card => card.Rank, StringComparer.Ordinal).ToArray()))
            .ToArray();
    }

    public static string DisplayStatus(ProjectTaskStatus status)
    {
        return status switch
        {
            ProjectTaskStatus.InProgress => "In Progress",
            _ => status.ToString()
        };
    }
}

internal sealed class ProjectTasksBoardSnapshot
{
    public ProjectTasksBoardSnapshot(
        IReadOnlyList<string> workspaceSwitcher,
        string selectedWorkspace,
        IReadOnlyList<ProjectTasksColumnSnapshot> columns,
        IReadOnlyList<ProjectTasksCardSnapshot> allTasks,
        ProjectTasksCardSnapshot selectedTask,
        ProjectTasksDetailsSnapshot details,
        ProjectTasksDragDropIntent dragDropIntent,
        IReadOnlyList<ProjectTasksAction> actions,
        IReadOnlyList<string> filters,
        bool rankRoundTripStable,
        bool archiveViewAvailable,
        bool archivedHiddenFromBoard,
        bool hardDeleteRequiresConfirmation,
        bool humanAcceptActionUsesTrustedContext,
        bool agentAcceptActionAvailable,
        bool worksWithoutAi,
        ProjectWorkspaceRevision workspaceEventRevision)
    {
        ArgumentNullException.ThrowIfNull(workspaceSwitcher);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedWorkspace);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(allTasks);
        ArgumentNullException.ThrowIfNull(selectedTask);
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(dragDropIntent);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(filters);

        WorkspaceSwitcher = workspaceSwitcher.ToArray();
        SelectedWorkspace = selectedWorkspace;
        Columns = columns.ToArray();
        AllTasks = allTasks.ToArray();
        SelectedTask = selectedTask;
        Details = details;
        DragDropIntent = dragDropIntent;
        Actions = actions.ToArray();
        Filters = filters.ToArray();
        RankRoundTripStable = rankRoundTripStable;
        ArchiveViewAvailable = archiveViewAvailable;
        ArchivedHiddenFromBoard = archivedHiddenFromBoard;
        HardDeleteRequiresConfirmation = hardDeleteRequiresConfirmation;
        HumanAcceptActionUsesTrustedContext = humanAcceptActionUsesTrustedContext;
        AgentAcceptActionAvailable = agentAcceptActionAvailable;
        WorksWithoutAi = worksWithoutAi;
        WorkspaceEventRevision = workspaceEventRevision;
    }

    public IReadOnlyList<string> WorkspaceSwitcher { get; }

    public string SelectedWorkspace { get; }

    public IReadOnlyList<ProjectTasksColumnSnapshot> Columns { get; }

    public IReadOnlyList<ProjectTasksCardSnapshot> AllTasks { get; }

    public ProjectTasksCardSnapshot SelectedTask { get; }

    public ProjectTasksDetailsSnapshot Details { get; }

    public ProjectTasksDragDropIntent DragDropIntent { get; }

    public IReadOnlyList<ProjectTasksAction> Actions { get; }

    public IReadOnlyList<string> Filters { get; }

    public bool RankRoundTripStable { get; }

    public bool ArchiveViewAvailable { get; }

    public bool ArchivedHiddenFromBoard { get; }

    public bool HardDeleteRequiresConfirmation { get; }

    public bool HumanAcceptActionUsesTrustedContext { get; }

    public bool AgentAcceptActionAvailable { get; }

    public bool WorksWithoutAi { get; }

    public ProjectWorkspaceRevision WorkspaceEventRevision { get; }

    public IReadOnlyList<string> VisibleTaskIds => AllTasks.Select(task => task.TaskId).ToArray();

    public IReadOnlyList<string> ManualBlockingReasons => BlockingReasons(TaskBlockingReason.Manual);

    public IReadOnlyList<string> DependencyBlockingReasons => BlockingReasons(TaskBlockingReason.Dependency);

    public string ReviewStatesDiffer => "Review:Open|Review:Submitted";

    private IReadOnlyList<string> BlockingReasons(TaskBlockingReason reason)
    {
        return AllTasks
            .Where(task => task.BlockingReasons.Contains(reason))
            .Select(_ => reason.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed record ProjectTasksColumnSnapshot(
    ProjectTaskStatus Status,
    string Label,
    IReadOnlyList<ProjectTasksCardSnapshot> Cards);

internal sealed class ProjectTasksCardSnapshot
{
    public ProjectTasksCardSnapshot(
        string taskId,
        string title,
        ProjectTaskStatus status,
        string priority,
        IReadOnlyList<string> labels,
        string assignee,
        TaskReadiness readiness,
        IReadOnlyList<TaskBlockingReason> blockingReasons,
        string rank,
        bool archived)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(priority);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentException.ThrowIfNullOrWhiteSpace(assignee);
        ArgumentNullException.ThrowIfNull(blockingReasons);
        ArgumentException.ThrowIfNullOrWhiteSpace(rank);

        TaskId = taskId;
        Title = title;
        Status = status;
        Priority = priority;
        Labels = labels.ToArray();
        Assignee = assignee;
        Readiness = readiness;
        BlockingReasons = blockingReasons.ToArray();
        Rank = rank;
        Archived = archived;
    }

    public string TaskId { get; }

    public string Title { get; }

    public ProjectTaskStatus Status { get; }

    public string Priority { get; }

    public IReadOnlyList<string> Labels { get; }

    public string Assignee { get; }

    public TaskReadiness Readiness { get; }

    public IReadOnlyList<TaskBlockingReason> BlockingReasons { get; }

    public string Rank { get; }

    public bool Archived { get; }
}

internal sealed record ProjectTasksDetailsSnapshot(
    string InspectorTitle,
    bool DescriptionVisible,
    bool AcceptanceCriteriaVisible,
    bool SubtasksVisible,
    IReadOnlyList<TaskActivityKind> ActivityKinds,
    IReadOnlyList<string> LinkedTransactions,
    IReadOnlyList<string> LinkedJobs,
    IReadOnlyList<string> LinkedDiagnostics,
    IReadOnlyList<string> LinkedArtifacts,
    IReadOnlyList<string> LinkedObjects);

internal sealed record ProjectTasksDragDropIntent(
    string TaskId,
    ProjectTaskStatus SourceStatus,
    ProjectTaskStatus TargetStatus,
    string TargetRank,
    bool Allowed,
    string RejectedDiagnosticCode);

internal sealed record ProjectTasksAction(
    string Label,
    bool Enabled,
    bool Clickable,
    bool RequiresTrustedHumanContext,
    bool DestructiveConfirmationRequired = false);
