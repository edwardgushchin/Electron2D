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
namespace Electron2D.ProjectSystem;

internal enum WorkspaceJobKind
{
    Import,
    Build,
    Test,
    Export,
    Run
}

internal enum WorkspaceJobState
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled
}

internal sealed class WorkspaceJobStore
{
    private readonly Dictionary<string, WorkspaceJob> jobs = new(StringComparer.Ordinal);
    private readonly ProjectWorkspace workspace;

    public WorkspaceJobStore(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
        Events = new WorkspaceJobEventStream();
    }

    public WorkspaceJobEventStream Events { get; }

    public IReadOnlyList<WorkspaceJob> Jobs =>
        jobs.Values.OrderBy(job => job.OperationId, StringComparer.Ordinal).ToArray();

    public WorkspaceJob Enqueue(
        string operationId,
        WorkspaceJobKind kind,
        WorkspaceJobInputIdentity inputIdentity,
        bool canCancel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(inputIdentity);
        if (jobs.ContainsKey(operationId))
        {
            throw new InvalidOperationException($"Workspace job '{operationId}' already exists.");
        }

        var job = new WorkspaceJob(workspace, Events, operationId, kind, inputIdentity, canCancel);
        jobs.Add(operationId, job);
        return job;
    }
}

internal sealed class WorkspaceJob
{
    private readonly List<WorkspaceJobArtifact> artifacts = [];
    private readonly List<StructuredDiagnostic> diagnostics = [];
    private readonly WorkspaceJobEventStream events;
    private readonly ProjectWorkspace workspace;

    public WorkspaceJob(
        ProjectWorkspace workspace,
        WorkspaceJobEventStream events,
        string operationId,
        WorkspaceJobKind kind,
        WorkspaceJobInputIdentity inputIdentity,
        bool canCancel)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(inputIdentity);

        this.workspace = workspace;
        this.events = events;
        OperationId = operationId;
        Kind = kind;
        InputIdentity = inputIdentity;
        CanCancel = canCancel;
    }

    public string OperationId { get; }

    public WorkspaceJobKind Kind { get; }

    public WorkspaceJobInputIdentity InputIdentity { get; }

    public WorkspaceJobState State { get; private set; }

    public double Progress { get; private set; }

    public bool CanCancel { get; private set; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics => diagnostics.ToArray();

    public IReadOnlyList<WorkspaceJobArtifact> Artifacts => artifacts.ToArray();

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public bool Stale { get; private set; }

    public void Start(DateTimeOffset startedAt)
    {
        EnsureState(WorkspaceJobState.Queued, "Only queued workspace jobs can start.");
        State = WorkspaceJobState.Running;
        StartedAt = startedAt;
        Publish("operation.started");
    }

    public void ReportProgress(double progress)
    {
        EnsureState(WorkspaceJobState.Running, "Only running workspace jobs can report progress.");
        ValidateProgress(progress);
        if (progress < Progress)
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Workspace job progress must be monotonic.");
        }

        Progress = progress;
        Publish("operation.progress");
    }

    public void AddArtifact(WorkspaceJobArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        EnsureNotTerminal("Terminal workspace jobs cannot produce artifacts.");
        artifact.SetStale(Stale);
        artifacts.Add(artifact);
        Publish("operation.artifactProduced", artifact: artifact);
    }

    public void CompleteSucceeded(DateTimeOffset completedAt)
    {
        EnsureCanComplete();
        State = WorkspaceJobState.Succeeded;
        Progress = 1;
        CanCancel = false;
        CompletedAt = completedAt;
        Publish("operation.completed");
    }

    public void CompleteFailed(StructuredDiagnostic diagnostic, DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        EnsureCanComplete();
        AddDiagnostic(diagnostic);
        State = WorkspaceJobState.Failed;
        CanCancel = false;
        CompletedAt = completedAt;
        Publish("operation.completed");
    }

    public WorkspaceJobCancelResult Cancel(string message, DateTimeOffset cancelledAt)
    {
        var diagnostic = CreateCancellationDiagnostic(message);
        if (!CanCancel || IsTerminal)
        {
            return new WorkspaceJobCancelResult(false, diagnostic);
        }

        diagnostics.Add(diagnostic);
        Publish("operation.diagnostic", diagnostic: diagnostic);
        State = WorkspaceJobState.Cancelled;
        CanCancel = false;
        CompletedAt = cancelledAt;
        Publish("operation.completed");
        return new WorkspaceJobCancelResult(true, diagnostic);
    }

    public bool RefreshStale(string currentBuildConfigurationHash)
    {
        Stale = WorkspaceSnapshotStalenessEvaluator.IsStale(
            InputIdentity,
            workspace,
            currentBuildConfigurationHash);
        foreach (var artifact in artifacts)
        {
            artifact.SetStale(Stale);
        }

        return Stale;
    }

    private bool IsTerminal =>
        State is WorkspaceJobState.Succeeded or WorkspaceJobState.Failed or WorkspaceJobState.Cancelled;

    private void AddDiagnostic(StructuredDiagnostic diagnostic)
    {
        EnsureNotTerminal("Terminal workspace jobs cannot add diagnostics.");
        diagnostics.Add(diagnostic);
        Publish("operation.diagnostic", diagnostic: diagnostic);
    }

    private void EnsureCanComplete()
    {
        if (State != WorkspaceJobState.Running)
        {
            throw new InvalidOperationException("Only running workspace jobs can complete.");
        }
    }

    private void EnsureNotTerminal(string message)
    {
        if (IsTerminal)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void EnsureState(WorkspaceJobState expectedState, string message)
    {
        if (State != expectedState)
        {
            throw new InvalidOperationException(message);
        }
    }

    private void Publish(
        string eventName,
        StructuredDiagnostic? diagnostic = null,
        WorkspaceJobArtifact? artifact = null)
    {
        events.Publish(new WorkspaceJobEvent(
            eventName,
            OperationId,
            Kind,
            State,
            Progress,
            CanCancel,
            InputIdentity,
            StartedAt,
            CompletedAt,
            Stale,
            diagnostic,
            artifact));
    }

    private static void ValidateProgress(double progress)
    {
        if (double.IsNaN(progress) || progress < 0 || progress > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(progress), progress, "Workspace job progress must be between 0 and 1.");
        }
    }

    private static StructuredDiagnostic CreateCancellationDiagnostic(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return StructuredDiagnostic.Create(
            "E2D-TOOLING-0001",
            DiagnosticSeverity.Info,
            DiagnosticCategory.Tooling,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }
}

internal sealed class WorkspaceJobCancelResult
{
    public WorkspaceJobCancelResult(bool succeeded, StructuredDiagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        Succeeded = succeeded;
        Diagnostic = diagnostic;
    }

    public bool Succeeded { get; }

    public StructuredDiagnostic Diagnostic { get; }
}

internal sealed class WorkspaceJobEvent
{
    public WorkspaceJobEvent(
        string eventName,
        string operationId,
        WorkspaceJobKind kind,
        WorkspaceJobState state,
        double progress,
        bool canCancel,
        WorkspaceJobInputIdentity inputIdentity,
        DateTimeOffset? startedAt,
        DateTimeOffset? completedAt,
        bool stale,
        StructuredDiagnostic? diagnostic,
        WorkspaceJobArtifact? artifact)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(inputIdentity);

        EventName = eventName;
        OperationId = operationId;
        Kind = kind;
        State = state;
        Progress = progress;
        CanCancel = canCancel;
        InputIdentity = inputIdentity;
        StartedAt = startedAt;
        CompletedAt = completedAt;
        Stale = stale;
        Diagnostic = diagnostic;
        Artifact = artifact;
    }

    public string EventName { get; }

    public string OperationId { get; }

    public WorkspaceJobKind Kind { get; }

    public WorkspaceJobState State { get; }

    public double Progress { get; }

    public bool CanCancel { get; }

    public WorkspaceJobInputIdentity InputIdentity { get; }

    public DateTimeOffset? StartedAt { get; }

    public DateTimeOffset? CompletedAt { get; }

    public bool Stale { get; }

    public StructuredDiagnostic? Diagnostic { get; }

    public WorkspaceJobArtifact? Artifact { get; }
}

internal sealed class WorkspaceJobEventStream
{
    private readonly List<Action<WorkspaceJobEvent>> subscribers = [];

    public IDisposable Subscribe(Action<WorkspaceJobEvent> subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        subscribers.Add(subscriber);
        return new Subscription(subscribers, subscriber);
    }

    public void Publish(WorkspaceJobEvent jobEvent)
    {
        ArgumentNullException.ThrowIfNull(jobEvent);
        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber(jobEvent);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly List<Action<WorkspaceJobEvent>> subscribers;
        private Action<WorkspaceJobEvent>? subscriber;

        public Subscription(List<Action<WorkspaceJobEvent>> subscribers, Action<WorkspaceJobEvent> subscriber)
        {
            this.subscribers = subscribers;
            this.subscriber = subscriber;
        }

        public void Dispose()
        {
            if (subscriber is null)
            {
                return;
            }

            subscribers.Remove(subscriber);
            subscriber = null;
        }
    }
}
