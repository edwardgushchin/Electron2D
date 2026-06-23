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

namespace Electron2D.Editor.AgentWorkspace;

internal enum EditorAgentWorkspaceConnectionState
{
    Starting,
    Connected,
    Disconnected,
    HandshakeError,
    TokenExpired
}

internal enum EditorAgentWorkspaceChangedObjectKind
{
    Scene,
    Node,
    Resource,
    Script,
    Setting
}

internal enum EditorAgentWorkspaceActionKind
{
    SendToAwaitingAcceptance,
    CancelOperation,
    StopRuntime,
    GroupedUndo
}

internal sealed class EditorAgentWorkspacePanel
{
    private static readonly string[] Sections =
    [
        "Session",
        "Current Task",
        "Changeset",
        "Diagnostics",
        "Artifacts",
        "Runtime",
        "Jobs",
        "Actions"
    ];

    public EditorAgentWorkspacePanel(EditorAgentWorkspacePanelSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    public EditorAgentWorkspacePanelSnapshot Snapshot { get; }

    public static EditorAgentWorkspacePanelSnapshot CreateSmokeSnapshot()
    {
        var inputIdentity = new WorkspaceJobInputIdentity(
            "snap-run-001",
            new ProjectWorkspaceRevision(42),
            new ProjectWorkspaceRevision(17),
            new Dictionary<string, ProjectDocumentRevision>(StringComparer.Ordinal)
            {
                ["Scripts/PlayerController.cs"] = new(4),
                ["scenes/main.e2scene.json"] = new(8)
            },
            "build-hash-001");

        var diagnostic = StructuredDiagnostic.Create(
            "E2D-RUNTIME-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Runtime,
            "Runtime snapshot capture reported a stale runtime tree.",
            new DiagnosticLocation(
                file: "scenes/main.e2scene.json",
                line: 12,
                column: 5,
                sceneUid: "scene-main",
                nodePath: "/root/Player"),
            relatedLocations:
            [
                new DiagnosticRelatedLocation(
                    new DiagnosticLocation(file: "Scripts/PlayerController.cs", line: 27, column: 13),
                    "Agent changed the runtime controller before the capture.")
            ],
            suggestedFixes:
            [
                new DiagnosticSuggestedFix(
                    "Refresh runtime tree after the next frame.",
                    [
                        DiagnosticFixAction.UpdateJsonProperty(
                            "project.e2d.json",
                            "/runtime/refreshTree",
                            "false",
                            "true")
                    ])
            ]);

        var artifacts = new[]
        {
            new EditorAgentWorkspaceArtifactSnapshot(
                "screenshot",
                "artifact://screenshots/frame-0001.png",
                inputIdentity,
                Stale: false),
            new EditorAgentWorkspaceArtifactSnapshot(
                "runtime-tree",
                "artifact://runtime/tree.json",
                inputIdentity,
                Stale: true)
        };

        return new EditorAgentWorkspacePanelSnapshot(
            new EditorAgentWorkspaceSessionSnapshot(
                "agent-session-t0150",
                "codex",
                EditorAgentWorkspaceConnectionState.Connected,
                HandshakeState: "Connected",
                Route: "activeEditor",
                LastAction: "Captured runtime screenshot",
                StartedAtUtc: new DateTimeOffset(2026, 6, 23, 1, 0, 0, TimeSpan.Zero),
                ConnectedAtUtc: new DateTimeOffset(2026, 6, 23, 1, 0, 5, TimeSpan.Zero)),
            new EditorAgentWorkspaceTaskSnapshot(
                "T-0150",
                ProjectTaskStatus.InProgress,
                acceptanceState: "Working",
                linkedTransactions: ["transaction://txn-agent-001"],
                linkedJobs: ["job://job-run-001"],
                linkedDiagnostics: [diagnostic.Code],
                linkedArtifacts: artifacts.Select(artifact => artifact.Uri).ToArray(),
                humanAcceptanceRequired: true),
            [
                new EditorAgentWorkspaceChangedObject(
                    EditorAgentWorkspaceChangedObjectKind.Scene,
                    "Main scene",
                    "res://scenes/main.e2scene.json"),
                new EditorAgentWorkspaceChangedObject(
                    EditorAgentWorkspaceChangedObjectKind.Node,
                    "Player node",
                    "/root/Player"),
                new EditorAgentWorkspaceChangedObject(
                    EditorAgentWorkspaceChangedObjectKind.Resource,
                    "Player texture",
                    "res://textures/player.png"),
                new EditorAgentWorkspaceChangedObject(
                    EditorAgentWorkspaceChangedObjectKind.Script,
                    "Player controller",
                    "Scripts/PlayerController.cs"),
                new EditorAgentWorkspaceChangedObject(
                    EditorAgentWorkspaceChangedObjectKind.Setting,
                    "Project settings",
                    "project.e2d.json")
            ],
            [diagnostic],
            artifacts,
            new EditorAgentWorkspaceJobSnapshot(
                "job-run-001",
                WorkspaceJobKind.Run,
                WorkspaceJobState.Running,
                progress: 0.65d,
                canCancel: true,
                inputIdentity,
                staleMarkers: ["runtime-tree"],
                artifacts),
            [
                new EditorAgentWorkspaceAction(
                    EditorAgentWorkspaceActionKind.SendToAwaitingAcceptance,
                    "Send Review",
                    Enabled: true,
                    Clickable: true),
                new EditorAgentWorkspaceAction(
                    EditorAgentWorkspaceActionKind.GroupedUndo,
                    "Undo AI",
                    Enabled: true,
                    Clickable: true,
                    UndoGroupId: "undo-agent-001"),
                new EditorAgentWorkspaceAction(
                    EditorAgentWorkspaceActionKind.CancelOperation,
                    "Cancel",
                    Enabled: true,
                    Clickable: true),
                new EditorAgentWorkspaceAction(
                    EditorAgentWorkspaceActionKind.StopRuntime,
                    "Stop",
                    Enabled: true,
                    Clickable: true)
            ],
            new EditorAgentWorkspaceDockState(
                placement: "RightBelowInspectorNode",
                persisted: true,
                visibleWorkspaces: ["2D", "Script", "Game", "Tasks"],
                dockable: true,
                resizable: true,
                hideable: true,
                movable: true,
                maximizable: true),
            Sections);
    }
}

internal sealed class EditorAgentWorkspacePanelSnapshot
{
    public EditorAgentWorkspacePanelSnapshot(
        EditorAgentWorkspaceSessionSnapshot session,
        EditorAgentWorkspaceTaskSnapshot task,
        IReadOnlyList<EditorAgentWorkspaceChangedObject> changedObjects,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IReadOnlyList<EditorAgentWorkspaceArtifactSnapshot> artifacts,
        EditorAgentWorkspaceJobSnapshot activeJob,
        IReadOnlyList<EditorAgentWorkspaceAction> actions,
        EditorAgentWorkspaceDockState dockState,
        IReadOnlyList<string> sections)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(changedObjects);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(activeJob);
        ArgumentNullException.ThrowIfNull(actions);
        ArgumentNullException.ThrowIfNull(dockState);
        ArgumentNullException.ThrowIfNull(sections);

        Session = session;
        Task = task;
        ChangedObjects = changedObjects.ToArray();
        Diagnostics = diagnostics.ToArray();
        Artifacts = artifacts.ToArray();
        ActiveJob = activeJob;
        Actions = actions.ToArray();
        DockState = dockState;
        Sections = sections.ToArray();
    }

    public EditorAgentWorkspaceSessionSnapshot Session { get; }

    public EditorAgentWorkspaceTaskSnapshot Task { get; }

    public IReadOnlyList<EditorAgentWorkspaceChangedObject> ChangedObjects { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public IReadOnlyList<EditorAgentWorkspaceArtifactSnapshot> Artifacts { get; }

    public EditorAgentWorkspaceJobSnapshot ActiveJob { get; }

    public IReadOnlyList<EditorAgentWorkspaceAction> Actions { get; }

    public EditorAgentWorkspaceDockState DockState { get; }

    public IReadOnlyList<string> Sections { get; }

    public IReadOnlyList<string> DiagnosticFields { get; } =
    [
        "code",
        "severity",
        "message",
        "location",
        "relatedLocations",
        "suggestedFixes"
    ];

    public bool AwaitingAcceptanceActionAvailable =>
        Actions.Any(action => action.Kind == EditorAgentWorkspaceActionKind.SendToAwaitingAcceptance && action.Enabled);

    public bool DoneActionAvailable =>
        Actions.Any(action => string.Equals(action.Label, "Done", StringComparison.OrdinalIgnoreCase) && action.Enabled);

    public bool GroupedUndoAvailable =>
        Actions.Any(action => action.Kind == EditorAgentWorkspaceActionKind.GroupedUndo && action.Enabled && action.UndoGroupId is not null);

    public string? UndoGroupId =>
        Actions.FirstOrDefault(action => action.Kind == EditorAgentWorkspaceActionKind.GroupedUndo)?.UndoGroupId;
}

internal sealed record EditorAgentWorkspaceSessionSnapshot(
    string AgentSessionId,
    string ProfileId,
    EditorAgentWorkspaceConnectionState ConnectionState,
    string HandshakeState,
    string Route,
    string LastAction,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? ConnectedAtUtc);

internal sealed class EditorAgentWorkspaceTaskSnapshot
{
    public EditorAgentWorkspaceTaskSnapshot(
        string taskId,
        ProjectTaskStatus status,
        string acceptanceState,
        IReadOnlyList<string> linkedTransactions,
        IReadOnlyList<string> linkedJobs,
        IReadOnlyList<string> linkedDiagnostics,
        IReadOnlyList<string> linkedArtifacts,
        bool humanAcceptanceRequired)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);
        ArgumentException.ThrowIfNullOrWhiteSpace(acceptanceState);
        ArgumentNullException.ThrowIfNull(linkedTransactions);
        ArgumentNullException.ThrowIfNull(linkedJobs);
        ArgumentNullException.ThrowIfNull(linkedDiagnostics);
        ArgumentNullException.ThrowIfNull(linkedArtifacts);

        TaskId = taskId;
        Status = status;
        AcceptanceState = acceptanceState;
        LinkedTransactions = linkedTransactions.ToArray();
        LinkedJobs = linkedJobs.ToArray();
        LinkedDiagnostics = linkedDiagnostics.ToArray();
        LinkedArtifacts = linkedArtifacts.ToArray();
        HumanAcceptanceRequired = humanAcceptanceRequired;
    }

    public string TaskId { get; }

    public ProjectTaskStatus Status { get; }

    public string AcceptanceState { get; }

    public IReadOnlyList<string> LinkedTransactions { get; }

    public IReadOnlyList<string> LinkedJobs { get; }

    public IReadOnlyList<string> LinkedDiagnostics { get; }

    public IReadOnlyList<string> LinkedArtifacts { get; }

    public bool HumanAcceptanceRequired { get; }
}

internal sealed record EditorAgentWorkspaceChangedObject(
    EditorAgentWorkspaceChangedObjectKind Kind,
    string DisplayName,
    string NavigationTarget);

internal sealed record EditorAgentWorkspaceArtifactSnapshot(
    string Kind,
    string Uri,
    WorkspaceJobInputIdentity InputIdentity,
    bool Stale);

internal sealed class EditorAgentWorkspaceJobSnapshot
{
    public EditorAgentWorkspaceJobSnapshot(
        string jobId,
        WorkspaceJobKind kind,
        WorkspaceJobState state,
        double progress,
        bool canCancel,
        WorkspaceJobInputIdentity inputIdentity,
        IReadOnlyList<string> staleMarkers,
        IReadOnlyList<EditorAgentWorkspaceArtifactSnapshot> artifacts)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobId);
        ArgumentNullException.ThrowIfNull(inputIdentity);
        ArgumentNullException.ThrowIfNull(staleMarkers);
        ArgumentNullException.ThrowIfNull(artifacts);
        if (double.IsNaN(progress) || progress < 0 || progress > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Agent Workspace job progress must be in the 0..1 range.");
        }

        JobId = jobId;
        Kind = kind;
        State = state;
        Progress = progress;
        CanCancel = canCancel;
        InputIdentity = inputIdentity;
        StaleMarkers = staleMarkers.ToArray();
        Artifacts = artifacts.ToArray();
    }

    public string JobId { get; }

    public WorkspaceJobKind Kind { get; }

    public WorkspaceJobState State { get; }

    public double Progress { get; }

    public int ProgressPercent => (int)Math.Round(Progress * 100d, MidpointRounding.AwayFromZero);

    public bool CanCancel { get; }

    public WorkspaceJobInputIdentity InputIdentity { get; }

    public IReadOnlyList<string> StaleMarkers { get; }

    public IReadOnlyList<EditorAgentWorkspaceArtifactSnapshot> Artifacts { get; }
}

internal sealed record EditorAgentWorkspaceAction(
    EditorAgentWorkspaceActionKind Kind,
    string Label,
    bool Enabled,
    bool Clickable,
    string? UndoGroupId = null);

internal sealed class EditorAgentWorkspaceDockState
{
    public EditorAgentWorkspaceDockState(
        string placement,
        bool persisted,
        IReadOnlyList<string> visibleWorkspaces,
        bool dockable,
        bool resizable,
        bool hideable,
        bool movable,
        bool maximizable)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(placement);
        ArgumentNullException.ThrowIfNull(visibleWorkspaces);

        Placement = placement;
        Persisted = persisted;
        VisibleWorkspaces = visibleWorkspaces.ToArray();
        Dockable = dockable;
        Resizable = resizable;
        Hideable = hideable;
        Movable = movable;
        Maximizable = maximizable;
    }

    public string Placement { get; }

    public bool Persisted { get; }

    public IReadOnlyList<string> VisibleWorkspaces { get; }

    public bool Dockable { get; }

    public bool Resizable { get; }

    public bool Hideable { get; }

    public bool Movable { get; }

    public bool Maximizable { get; }
}
