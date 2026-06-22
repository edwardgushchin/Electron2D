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

namespace Electron2D.ProjectSystem;

internal enum ProjectWorkspaceOpenMode
{
    EditorPrimary,
    EditorReadOnly,
    Headless
}

internal enum ProjectWorkspaceActorKind
{
    Human,
    Agent,
    Cli,
    ExternalFile,
    Test
}

internal enum ProjectWorkspaceEventKind
{
    DocumentOpened,
    DocumentChanged,
    DocumentPersisted,
    DiagnosticsUpdated
}

internal enum ProjectWorkspacePersistenceState
{
    Clean,
    Dirty
}

internal readonly record struct ProjectWorkspaceRevision
{
    public ProjectWorkspaceRevision(long value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Workspace revision must be non-negative.");
        }

        Value = value;
    }

    public long Value { get; }

    public ProjectWorkspaceRevision Next()
    {
        return new ProjectWorkspaceRevision(checked(Value + 1));
    }
}

internal sealed class ProjectWorkspaceOpenResult
{
    public ProjectWorkspaceOpenResult(ProjectWorkspace workspace, string message)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Workspace = workspace;
        Message = message;
    }

    public ProjectWorkspace Workspace { get; }

    public string Message { get; }
}

internal sealed class ProjectWorkspaceLeaseRegistry
{
    private readonly Dictionary<string, ProjectWorkspaceOwnerLease> primaryLeases;
    private readonly TimeSpan staleTimeout;

    public ProjectWorkspaceLeaseRegistry(TimeSpan staleTimeout)
    {
        if (staleTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(staleTimeout), staleTimeout, "Workspace lease timeout must be positive.");
        }

        this.staleTimeout = staleTimeout;
        primaryLeases = new Dictionary<string, ProjectWorkspaceOwnerLease>(StringComparer.OrdinalIgnoreCase);
    }

    public ProjectWorkspaceOpenResult OpenEditor(string projectRoot, string ownerId, DateTimeOffset nowUtc)
    {
        var normalizedRoot = NormalizeProjectRoot(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        if (primaryLeases.TryGetValue(normalizedRoot, out var existingLease) && !existingLease.IsStale(nowUtc))
        {
            var readOnlyLease = new ProjectWorkspaceOwnerLease(
                normalizedRoot,
                ownerId,
                ProjectWorkspaceOpenMode.EditorReadOnly,
                staleTimeout,
                nowUtc);
            return new ProjectWorkspaceOpenResult(
                ProjectWorkspace.Create(normalizedRoot, ProjectWorkspaceOpenMode.EditorReadOnly, readOnlyLease, release: null),
                $"Project workspace is already owned by '{existingLease.OwnerId}'. Opened read-only workspace.");
        }

        var lease = new ProjectWorkspaceOwnerLease(
            normalizedRoot,
            ownerId,
            ProjectWorkspaceOpenMode.EditorPrimary,
            staleTimeout,
            nowUtc);
        primaryLeases[normalizedRoot] = lease;
        return new ProjectWorkspaceOpenResult(
            ProjectWorkspace.Create(normalizedRoot, ProjectWorkspaceOpenMode.EditorPrimary, lease, () => Release(normalizedRoot, ownerId)),
            "Opened primary project workspace.");
    }

    public ProjectWorkspaceOpenResult OpenHeadless(string projectRoot, string ownerId, DateTimeOffset nowUtc)
    {
        var normalizedRoot = NormalizeProjectRoot(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var lease = new ProjectWorkspaceOwnerLease(
            normalizedRoot,
            ownerId,
            ProjectWorkspaceOpenMode.Headless,
            staleTimeout,
            nowUtc);

        return new ProjectWorkspaceOpenResult(
            ProjectWorkspace.Create(normalizedRoot, ProjectWorkspaceOpenMode.Headless, lease, release: null),
            "Opened headless project workspace.");
    }

    private void Release(string projectRoot, string ownerId)
    {
        if (primaryLeases.TryGetValue(projectRoot, out var lease) &&
            string.Equals(lease.OwnerId, ownerId, StringComparison.Ordinal))
        {
            primaryLeases.Remove(projectRoot);
        }
    }

    private static string NormalizeProjectRoot(string projectRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        return Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}

internal sealed class ProjectWorkspaceOwnerLease
{
    private readonly TimeSpan staleTimeout;

    public ProjectWorkspaceOwnerLease(
        string projectRoot,
        string ownerId,
        ProjectWorkspaceOpenMode openMode,
        TimeSpan staleTimeout,
        DateTimeOffset lastHeartbeatUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        ProjectRoot = projectRoot;
        OwnerId = ownerId;
        OpenMode = openMode;
        this.staleTimeout = staleTimeout;
        LastHeartbeatUtc = lastHeartbeatUtc;
    }

    public string ProjectRoot { get; }

    public string OwnerId { get; }

    public ProjectWorkspaceOpenMode OpenMode { get; }

    public DateTimeOffset LastHeartbeatUtc { get; private set; }

    public void Touch(DateTimeOffset heartbeatUtc)
    {
        if (heartbeatUtc < LastHeartbeatUtc)
        {
            throw new ArgumentOutOfRangeException(nameof(heartbeatUtc), heartbeatUtc, "Workspace lease heartbeat must be monotonic.");
        }

        LastHeartbeatUtc = heartbeatUtc;
    }

    public bool IsStale(DateTimeOffset nowUtc)
    {
        return nowUtc - LastHeartbeatUtc > staleTimeout;
    }
}

internal sealed class ProjectWorkspace : IDisposable
{
    private readonly Action? release;
    private bool disposed;

    private ProjectWorkspace(
        string projectRoot,
        ProjectWorkspaceOpenMode openMode,
        ProjectWorkspaceOwnerLease ownerLease,
        Action? release)
    {
        ProjectRoot = projectRoot;
        OpenMode = openMode;
        OwnerLease = ownerLease;
        this.release = release;

        Events = new ProjectWorkspaceChangeEventStream();
        Revisions = new ProjectWorkspaceRevisionStore();
        OperationJournal = new ProjectWorkspaceOperationJournal();
        Documents = new ProjectWorkspaceDocumentStore();
        UndoRedo = new ProjectWorkspaceUndoRedoStore();
        ImportState = new ProjectWorkspaceImportStateStore();
        BuildState = new ProjectWorkspaceBuildStateStore();
        Diagnostics = new ProjectWorkspaceDiagnosticsStore(this);
        CommandBus = new ProjectWorkspaceCommandBus(this);
    }

    public string ProjectRoot { get; }

    public ProjectWorkspaceOpenMode OpenMode { get; }

    public ProjectWorkspaceOwnerLease OwnerLease { get; }

    public ProjectWorkspaceDocumentStore Documents { get; }

    public ProjectWorkspaceCommandBus CommandBus { get; }

    public ProjectWorkspaceChangeEventStream Events { get; }

    public ProjectWorkspaceRevisionStore Revisions { get; }

    public ProjectWorkspaceOperationJournal OperationJournal { get; }

    public ProjectWorkspaceUndoRedoStore UndoRedo { get; }

    public ProjectWorkspaceImportStateStore ImportState { get; }

    public ProjectWorkspaceBuildStateStore BuildState { get; }

    public ProjectWorkspaceDiagnosticsStore Diagnostics { get; }

    public static ProjectWorkspace CreateHeadless(string projectRoot, string ownerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        var lease = new ProjectWorkspaceOwnerLease(
            Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
            ownerId,
            ProjectWorkspaceOpenMode.Headless,
            TimeSpan.FromMinutes(5),
            DateTimeOffset.UtcNow);
        return Create(lease.ProjectRoot, ProjectWorkspaceOpenMode.Headless, lease, release: null);
    }

    internal static ProjectWorkspace Create(
        string projectRoot,
        ProjectWorkspaceOpenMode openMode,
        ProjectWorkspaceOwnerLease ownerLease,
        Action? release)
    {
        return new ProjectWorkspace(projectRoot, openMode, ownerLease, release);
    }

    internal bool CanMutate => OpenMode != ProjectWorkspaceOpenMode.EditorReadOnly;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        release?.Invoke();
    }
}

internal sealed class ProjectWorkspaceOperationContext
{
    public ProjectWorkspaceOperationContext(
        string operationId,
        ProjectWorkspaceActorKind actorKind,
        string operationKind,
        DateTimeOffset? startedAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);

        OperationId = operationId;
        ActorKind = actorKind;
        OperationKind = operationKind;
        StartedAtUtc = startedAtUtc ?? DateTimeOffset.UtcNow;
    }

    public string OperationId { get; }

    public ProjectWorkspaceActorKind ActorKind { get; }

    public string OperationKind { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public static ProjectWorkspaceOperationContext ForTest(string operationId)
    {
        return new ProjectWorkspaceOperationContext(operationId, ProjectWorkspaceActorKind.Test, "test.operation");
    }
}

internal sealed class ProjectWorkspaceCommandBus
{
    private readonly ProjectWorkspace workspace;

    public ProjectWorkspaceCommandBus(ProjectWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public bool CanExecuteMutatingCommands => workspace.CanMutate;

    public ProjectWorkspaceCommandResult OpenTextDocument(
        string relativePath,
        string text,
        long persistedRevision,
        ProjectWorkspaceOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!CanExecuteMutatingCommands)
        {
            return ProjectWorkspaceCommandResult.Failure("Read-only workspace cannot open mutable documents.", workspace);
        }

        var document = workspace.Documents.OpenTextDocument(
            relativePath,
            text,
            ProjectDocumentRevisionState.Clean(persistedRevision));
        workspace.Revisions.RecordDocumentOpened(document);
        workspace.OperationJournal.RecordCompleted(
            context,
            [document.Path],
            completedAtUtc: DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DocumentOpened,
            workspace.Revisions.WorkspaceRevision,
            document.DocumentId,
            document.Path,
            context.OperationId,
            source: null,
            diagnostics: []));

        return ProjectWorkspaceCommandResult.Success(workspace, "Document opened.");
    }

    public ProjectWorkspaceCommandResult ReplaceTextDocument(
        string relativePath,
        string text,
        ProjectDocumentRevision expectedRevision,
        ProjectWorkspaceOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!CanExecuteMutatingCommands)
        {
            return ProjectWorkspaceCommandResult.Failure("Read-only workspace cannot change documents.", workspace);
        }

        var existing = workspace.Documents.GetByPath(relativePath);
        if (existing.InMemoryRevision != expectedRevision)
        {
            return ProjectWorkspaceCommandResult.Failure(
                $"Document expected revision '{expectedRevision.Value}' does not match current revision '{existing.InMemoryRevision.Value}'.",
                workspace);
        }

        var changed = workspace.Documents.ReplaceTextDocument(relativePath, text);
        workspace.Revisions.RecordDocumentChanged(changed);
        workspace.OperationJournal.RecordCompleted(
            context,
            [changed.Path],
            completedAtUtc: DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DocumentChanged,
            workspace.Revisions.WorkspaceRevision,
            changed.DocumentId,
            changed.Path,
            context.OperationId,
            source: null,
            diagnostics: []));

        return ProjectWorkspaceCommandResult.Success(workspace, "Document changed.");
    }

    public ProjectWorkspaceCommandResult MarkDocumentPersisted(
        string relativePath,
        ProjectDocumentRevision expectedRevision,
        ProjectWorkspaceOperationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!CanExecuteMutatingCommands)
        {
            return ProjectWorkspaceCommandResult.Failure("Read-only workspace cannot persist documents.", workspace);
        }

        var existing = workspace.Documents.GetByPath(relativePath);
        if (existing.InMemoryRevision != expectedRevision)
        {
            return ProjectWorkspaceCommandResult.Failure(
                $"Document expected revision '{expectedRevision.Value}' does not match current revision '{existing.InMemoryRevision.Value}'.",
                workspace);
        }

        var persisted = workspace.Documents.MarkPersisted(relativePath);
        workspace.Revisions.RecordDocumentPersisted(persisted);
        workspace.OperationJournal.RecordCompleted(
            context,
            [persisted.Path],
            completedAtUtc: DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DocumentPersisted,
            workspace.Revisions.WorkspaceRevision,
            persisted.DocumentId,
            persisted.Path,
            context.OperationId,
            source: null,
            diagnostics: []));

        return ProjectWorkspaceCommandResult.Success(workspace, "Document persisted.");
    }
}

internal sealed class ProjectWorkspaceCommandResult
{
    private ProjectWorkspaceCommandResult(
        bool succeeded,
        string message,
        ProjectWorkspaceRevision workspaceRevision,
        ProjectWorkspaceRevision contentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> documentRevisions,
        IReadOnlyList<string> dirtyDocuments,
        ProjectWorkspacePersistenceState persistenceState)
    {
        Succeeded = succeeded;
        Message = message;
        WorkspaceRevision = workspaceRevision;
        ContentRevision = contentRevision;
        DocumentRevisions = documentRevisions;
        DirtyDocuments = dirtyDocuments;
        PersistenceState = persistenceState;
    }

    public bool Succeeded { get; }

    public string Message { get; }

    public ProjectWorkspaceRevision WorkspaceRevision { get; }

    public ProjectWorkspaceRevision ContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> DocumentRevisions { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }

    public ProjectWorkspacePersistenceState PersistenceState { get; }

    public static ProjectWorkspaceCommandResult Success(ProjectWorkspace workspace, string message)
    {
        return new ProjectWorkspaceCommandResult(
            succeeded: true,
            message,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            workspace.Revisions.DirtyDocuments,
            workspace.Revisions.PersistenceState);
    }

    public static ProjectWorkspaceCommandResult Failure(string message, ProjectWorkspace workspace)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return new ProjectWorkspaceCommandResult(
            succeeded: false,
            message,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            workspace.Revisions.DirtyDocuments,
            workspace.Revisions.PersistenceState);
    }
}

internal sealed class ProjectWorkspaceDocument
{
    public ProjectWorkspaceDocument(string path, string text, ProjectDocumentSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(snapshot);

        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        Text = text.ReplaceLineEndings("\n");
        Snapshot = snapshot;
    }

    public string Path { get; }

    public string Text { get; }

    public ProjectDocumentSnapshot Snapshot { get; }

    public string DocumentId => Snapshot.Identity.DocumentId;

    public ProjectDocumentRevision PersistedRevision => Snapshot.PersistedRevision;

    public ProjectDocumentRevision InMemoryRevision => Snapshot.InMemoryRevision;

    public bool IsDirty => Snapshot.IsDirty;
}

internal sealed class ProjectWorkspaceDocumentStore
{
    private readonly Dictionary<string, ProjectWorkspaceDocument> documents = new(StringComparer.Ordinal);

    public IReadOnlyList<ProjectWorkspaceDocument> Documents =>
        documents.Values.OrderBy(document => document.Path, StringComparer.Ordinal).ToArray();

    public ProjectWorkspaceDocument OpenTextDocument(
        string relativePath,
        string text,
        ProjectDocumentRevisionState revisionState)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        var snapshot = ProjectDocumentParser.ParseText(normalizedPath, text, revisionState);
        var document = new ProjectWorkspaceDocument(normalizedPath, text, snapshot);
        documents[normalizedPath] = document;
        return document;
    }

    public ProjectWorkspaceDocument ReplaceTextDocument(string relativePath, string text)
    {
        var existing = GetByPath(relativePath);
        var nextRevision = existing.InMemoryRevision.Next();
        var snapshot = ProjectDocumentParser.ParseText(
            existing.Path,
            text,
            new ProjectDocumentRevisionState(existing.PersistedRevision, nextRevision));
        var changed = new ProjectWorkspaceDocument(existing.Path, text, snapshot);
        documents[existing.Path] = changed;
        return changed;
    }

    public ProjectWorkspaceDocument MarkPersisted(string relativePath)
    {
        var existing = GetByPath(relativePath);
        var persisted = new ProjectWorkspaceDocument(
            existing.Path,
            existing.Text,
            existing.Snapshot.MarkPersisted());
        documents[existing.Path] = persisted;
        return persisted;
    }

    public ProjectWorkspaceDocument GetByPath(string relativePath)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(relativePath);
        return documents.TryGetValue(normalizedPath, out var document)
            ? document
            : throw new KeyNotFoundException($"Project workspace document '{normalizedPath}' is not open.");
    }

    public IReadOnlyList<ProjectWorkspaceDocument> GetDirtyDocuments()
    {
        return documents.Values
            .Where(document => document.IsDirty)
            .OrderBy(document => document.Path, StringComparer.Ordinal)
            .ToArray();
    }
}

internal sealed class ProjectWorkspaceRevisionStore
{
    private readonly Dictionary<string, ProjectDocumentRevision> documentRevisions = new(StringComparer.Ordinal);
    private readonly SortedSet<string> dirtyDocuments = new(StringComparer.Ordinal);

    public ProjectWorkspaceRevision WorkspaceRevision { get; private set; }

    public ProjectWorkspaceRevision ContentRevision { get; private set; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> DocumentRevisions =>
        new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            documentRevisions.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));

    public IReadOnlyList<string> DirtyDocuments => dirtyDocuments.ToArray();

    public ProjectWorkspacePersistenceState PersistenceState =>
        dirtyDocuments.Count == 0 ? ProjectWorkspacePersistenceState.Clean : ProjectWorkspacePersistenceState.Dirty;

    public void RecordDocumentOpened(ProjectWorkspaceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        WorkspaceRevision = WorkspaceRevision.Next();
        documentRevisions[document.Path] = document.InMemoryRevision;
        UpdateDirtyState(document);
    }

    public void RecordDocumentChanged(ProjectWorkspaceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        WorkspaceRevision = WorkspaceRevision.Next();
        ContentRevision = ContentRevision.Next();
        documentRevisions[document.Path] = document.InMemoryRevision;
        UpdateDirtyState(document);
    }

    public void RecordDocumentPersisted(ProjectWorkspaceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        WorkspaceRevision = WorkspaceRevision.Next();
        documentRevisions[document.Path] = document.InMemoryRevision;
        UpdateDirtyState(document);
    }

    public void RecordDiagnosticsUpdated()
    {
        WorkspaceRevision = WorkspaceRevision.Next();
    }

    private void UpdateDirtyState(ProjectWorkspaceDocument document)
    {
        if (document.IsDirty)
        {
            dirtyDocuments.Add(document.Path);
        }
        else
        {
            dirtyDocuments.Remove(document.Path);
        }
    }
}

internal sealed class ProjectWorkspaceEvent
{
    public ProjectWorkspaceEvent(
        ProjectWorkspaceEventKind kind,
        ProjectWorkspaceRevision workspaceRevision,
        string? documentId,
        string? documentPath,
        string operationId,
        string? source,
        IEnumerable<StructuredDiagnostic> diagnostics)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(diagnostics);

        Kind = kind;
        WorkspaceRevision = workspaceRevision;
        DocumentId = documentId;
        DocumentPath = documentPath;
        OperationId = operationId;
        Source = source;
        Diagnostics = diagnostics.ToArray();
    }

    public ProjectWorkspaceEventKind Kind { get; }

    public ProjectWorkspaceRevision WorkspaceRevision { get; }

    public string? DocumentId { get; }

    public string? DocumentPath { get; }

    public string OperationId { get; }

    public string? Source { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }
}

internal sealed class ProjectWorkspaceChangeEventStream
{
    private readonly List<Action<ProjectWorkspaceEvent>> subscribers = [];

    public IDisposable Subscribe(Action<ProjectWorkspaceEvent> subscriber)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        subscribers.Add(subscriber);
        return new Subscription(subscribers, subscriber);
    }

    public void Publish(ProjectWorkspaceEvent projectEvent)
    {
        ArgumentNullException.ThrowIfNull(projectEvent);
        foreach (var subscriber in subscribers.ToArray())
        {
            subscriber(projectEvent);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly List<Action<ProjectWorkspaceEvent>> subscribers;
        private Action<ProjectWorkspaceEvent>? subscriber;

        public Subscription(List<Action<ProjectWorkspaceEvent>> subscribers, Action<ProjectWorkspaceEvent> subscriber)
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

internal sealed class ProjectWorkspaceOperationJournal
{
    private readonly List<ProjectWorkspaceOperationEntry> entries = [];

    public IReadOnlyList<ProjectWorkspaceOperationEntry> Entries => entries.ToArray();

    public bool HasUnfinishedTransaction { get; private set; }

    public string? UnfinishedTransactionOperationId { get; private set; }

    public ProjectWorkspaceRecoverySnapshot? RecoverySnapshot { get; private set; }

    public void RecordCompleted(
        ProjectWorkspaceOperationContext context,
        IEnumerable<string> affectedDocuments,
        DateTimeOffset completedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(affectedDocuments);
        entries.Add(new ProjectWorkspaceOperationEntry(
            context.OperationId,
            context.ActorKind,
            context.OperationKind,
            affectedDocuments,
            context.StartedAtUtc,
            completedAtUtc));
    }

    public void BeginTransaction(
        string operationId,
        ProjectWorkspaceActorKind actorKind,
        string operationKind,
        IEnumerable<string> affectedDocuments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);
        ArgumentNullException.ThrowIfNull(affectedDocuments);

        if (HasUnfinishedTransaction)
        {
            throw new InvalidOperationException("A workspace transaction is already unfinished.");
        }

        HasUnfinishedTransaction = true;
        UnfinishedTransactionOperationId = operationId;
        entries.Add(new ProjectWorkspaceOperationEntry(
            operationId,
            actorKind,
            operationKind,
            affectedDocuments,
            DateTimeOffset.UtcNow,
            completedAtUtc: null));
    }

    public void CompleteTransaction(string operationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        var entry = entries.SingleOrDefault(entry => entry.OperationId == operationId) ??
            throw new InvalidOperationException($"Workspace transaction '{operationId}' was not found.");
        entry.MarkCompleted(DateTimeOffset.UtcNow);

        if (string.Equals(UnfinishedTransactionOperationId, operationId, StringComparison.Ordinal))
        {
            HasUnfinishedTransaction = false;
            UnfinishedTransactionOperationId = null;
        }
    }

    public void RecordRecoverySnapshot(IEnumerable<ProjectWorkspaceDocument> dirtyDocuments, string message)
    {
        ArgumentNullException.ThrowIfNull(dirtyDocuments);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        RecoverySnapshot = new ProjectWorkspaceRecoverySnapshot(
            message,
            dirtyDocuments.Select(document => new ProjectWorkspaceRecoveryDocument(
                document.Path,
                document.Text,
                document.PersistedRevision,
                document.InMemoryRevision)));
    }
}

internal sealed class ProjectWorkspaceOperationEntry
{
    public ProjectWorkspaceOperationEntry(
        string operationId,
        ProjectWorkspaceActorKind actorKind,
        string operationKind,
        IEnumerable<string> affectedDocuments,
        DateTimeOffset startedAtUtc,
        DateTimeOffset? completedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);
        ArgumentNullException.ThrowIfNull(affectedDocuments);

        OperationId = operationId;
        ActorKind = actorKind;
        OperationKind = operationKind;
        AffectedDocuments = affectedDocuments
            .Select(ProjectDocumentPaths.NormalizeRelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        StartedAtUtc = startedAtUtc;
        CompletedAt = completedAtUtc;
    }

    public string OperationId { get; }

    public ProjectWorkspaceActorKind ActorKind { get; }

    public string OperationKind { get; }

    public IReadOnlyList<string> AffectedDocuments { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        CompletedAt = completedAtUtc;
    }
}

internal sealed class ProjectWorkspaceRecoverySnapshot
{
    public ProjectWorkspaceRecoverySnapshot(string message, IEnumerable<ProjectWorkspaceRecoveryDocument> documents)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(documents);

        Message = message;
        Documents = documents.OrderBy(document => document.Path, StringComparer.Ordinal).ToArray();
    }

    public string Message { get; }

    public IReadOnlyList<ProjectWorkspaceRecoveryDocument> Documents { get; }
}

internal sealed class ProjectWorkspaceRecoveryDocument
{
    public ProjectWorkspaceRecoveryDocument(
        string path,
        string text,
        ProjectDocumentRevision persistedRevision,
        ProjectDocumentRevision inMemoryRevision)
    {
        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        Text = text.ReplaceLineEndings("\n");
        PersistedRevision = persistedRevision;
        InMemoryRevision = inMemoryRevision;
    }

    public string Path { get; }

    public string Text { get; }

    public ProjectDocumentRevision PersistedRevision { get; }

    public ProjectDocumentRevision InMemoryRevision { get; }
}

internal sealed class ProjectWorkspaceDiagnosticsStore
{
    private readonly Dictionary<string, StructuredDiagnostic[]> diagnosticsBySource = new(StringComparer.Ordinal);
    private readonly ProjectWorkspace workspace;

    public ProjectWorkspaceDiagnosticsStore(ProjectWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public void SetDiagnostics(
        string source,
        IEnumerable<StructuredDiagnostic> diagnostics,
        ProjectWorkspaceOperationContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(context);

        var copy = diagnostics.ToArray();
        diagnosticsBySource[source] = copy;
        workspace.Revisions.RecordDiagnosticsUpdated();
        workspace.OperationJournal.RecordCompleted(
            context,
            affectedDocuments: [],
            completedAtUtc: DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DiagnosticsUpdated,
            workspace.Revisions.WorkspaceRevision,
            documentId: null,
            documentPath: null,
            context.OperationId,
            source,
            copy));
    }

    public IReadOnlyList<StructuredDiagnostic> GetDiagnostics(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        return diagnosticsBySource.TryGetValue(source, out var diagnostics) ? diagnostics : [];
    }
}

internal sealed class ProjectWorkspaceUndoRedoStore
{
    private readonly List<string> undoGroups = [];

    public IReadOnlyList<string> UndoGroups => undoGroups.ToArray();

    public void AddUndoGroup(string undoGroupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(undoGroupId);
        undoGroups.Add(undoGroupId);
    }
}

internal sealed class ProjectWorkspaceImportStateStore
{
    private readonly Dictionary<string, string> states = new(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> States =>
        new ReadOnlyDictionary<string, string>(
            states.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));

    public void SetState(string path, string state)
    {
        states[ProjectDocumentPaths.NormalizeRelativePath(path)] = state;
    }
}

internal sealed class ProjectWorkspaceBuildStateStore
{
    public string State { get; private set; } = "idle";

    public void SetState(string state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        State = state;
    }
}
