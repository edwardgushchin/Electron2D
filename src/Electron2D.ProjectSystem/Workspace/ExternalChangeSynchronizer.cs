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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum ExternalFileChangeKind
{
    Created,
    Changed,
    Deleted,
    Renamed,
    Overflow,
    Resume
}

internal sealed class ExternalChangeSynchronizerOptions
{
    public ExternalChangeSynchronizerOptions(TimeSpan debounceDelay, TimeSpan selfWriteSuppressionWindow)
    {
        if (debounceDelay <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(debounceDelay), debounceDelay, "External change debounce delay must be positive.");
        }

        if (selfWriteSuppressionWindow <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(selfWriteSuppressionWindow),
                selfWriteSuppressionWindow,
                "External change self-write suppression window must be positive.");
        }

        DebounceDelay = debounceDelay;
        SelfWriteSuppressionWindow = selfWriteSuppressionWindow;
    }

    public TimeSpan DebounceDelay { get; }

    public TimeSpan SelfWriteSuppressionWindow { get; }

    public static ExternalChangeSynchronizerOptions Default { get; } = new(
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromSeconds(2));
}

internal sealed class ExternalFileChangeEvent
{
    private ExternalFileChangeEvent(
        ExternalFileChangeKind kind,
        DateTimeOffset timestampUtc,
        string? path,
        string? oldPath,
        string? directoryPath)
    {
        Kind = kind;
        TimestampUtc = timestampUtc;
        Path = path;
        OldPath = oldPath;
        DirectoryPath = directoryPath;
    }

    public ExternalFileChangeKind Kind { get; }

    public DateTimeOffset TimestampUtc { get; }

    public string? Path { get; }

    public string? OldPath { get; }

    public string? DirectoryPath { get; }

    public static ExternalFileChangeEvent Created(string path, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new ExternalFileChangeEvent(ExternalFileChangeKind.Created, timestampUtc, path, oldPath: null, directoryPath: null);
    }

    public static ExternalFileChangeEvent Changed(string path, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new ExternalFileChangeEvent(ExternalFileChangeKind.Changed, timestampUtc, path, oldPath: null, directoryPath: null);
    }

    public static ExternalFileChangeEvent Deleted(string path, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return new ExternalFileChangeEvent(ExternalFileChangeKind.Deleted, timestampUtc, path, oldPath: null, directoryPath: null);
    }

    public static ExternalFileChangeEvent Renamed(string oldPath, string newPath, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(oldPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPath);
        return new ExternalFileChangeEvent(ExternalFileChangeKind.Renamed, timestampUtc, newPath, oldPath, directoryPath: null);
    }

    public static ExternalFileChangeEvent Overflow(string directoryPath, DateTimeOffset timestampUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        return new ExternalFileChangeEvent(ExternalFileChangeKind.Overflow, timestampUtc, path: null, oldPath: null, directoryPath);
    }

    public static ExternalFileChangeEvent Resume(DateTimeOffset timestampUtc)
    {
        return new ExternalFileChangeEvent(ExternalFileChangeKind.Resume, timestampUtc, path: null, oldPath: null, directoryPath: null);
    }
}

internal sealed class ExternalMovedPath
{
    public ExternalMovedPath(string oldPath, string newPath)
    {
        OldPath = ProjectDocumentPaths.NormalizeRelativePath(oldPath);
        NewPath = ProjectDocumentPaths.NormalizeRelativePath(newPath);
    }

    public string OldPath { get; }

    public string NewPath { get; }
}

internal sealed class ExternalChangeImportRecord
{
    public ExternalChangeImportRecord(
        string path,
        string operationId,
        PrincipalKind principalKind,
        IReadOnlySet<OperationCapability> capabilities,
        string origin,
        bool routedThroughTaskManager,
        bool usedWorkspaceTransaction,
        string fileSystemDockStatus)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);

        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        OperationId = operationId;
        PrincipalKind = principalKind;
        Capabilities = capabilities.ToArray();
        Origin = origin;
        RoutedThroughTaskManager = routedThroughTaskManager;
        UsedWorkspaceTransaction = usedWorkspaceTransaction;
        FileSystemDockStatus = fileSystemDockStatus;
    }

    public string Path { get; }

    public string OperationId { get; }

    public PrincipalKind PrincipalKind { get; }

    public IReadOnlyList<OperationCapability> Capabilities { get; }

    public string Origin { get; }

    public bool RoutedThroughTaskManager { get; }

    public bool UsedWorkspaceTransaction { get; }

    public string FileSystemDockStatus { get; }
}

internal sealed class ExternalChangeSynchronizerResult
{
    public ExternalChangeSynchronizerResult(
        IEnumerable<string> processedPaths,
        IEnumerable<string> ignoredPaths,
        IEnumerable<string> suppressedPaths,
        IEnumerable<string> changedFiles,
        IEnumerable<string> deletedPaths,
        IEnumerable<ExternalMovedPath> movedPaths,
        IEnumerable<string> changedObjects,
        IEnumerable<string> createdObjects,
        IEnumerable<StructuredDiagnostic> diagnostics,
        IEnumerable<WorkspaceTransactionConflict> conflicts,
        int directoryScanCount,
        IEnumerable<string> scannedDirectories,
        bool fullProjectRescan,
        TimeSpan maxAppliedDelay,
        IEnumerable<ExternalChangeImportRecord> importRecords)
    {
        ProcessedPaths = CopyPaths(processedPaths);
        IgnoredPaths = CopyPaths(ignoredPaths);
        SuppressedPaths = CopyPaths(suppressedPaths);
        ChangedFiles = CopyPaths(changedFiles);
        DeletedPaths = CopyPaths(deletedPaths);
        MovedPaths = movedPaths.OrderBy(path => path.OldPath, StringComparer.Ordinal).ThenBy(path => path.NewPath, StringComparer.Ordinal).ToArray();
        ChangedObjects = changedObjects.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        CreatedObjects = createdObjects.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Diagnostics = diagnostics.ToArray();
        Conflicts = conflicts.ToArray();
        DirectoryScanCount = directoryScanCount;
        ScannedDirectories = scannedDirectories.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        FullProjectRescan = fullProjectRescan;
        MaxAppliedDelay = maxAppliedDelay;
        ImportRecords = importRecords.OrderBy(record => record.Path, StringComparer.Ordinal).ToArray();
    }

    public IReadOnlyList<string> ProcessedPaths { get; }

    public IReadOnlyList<string> IgnoredPaths { get; }

    public IReadOnlyList<string> SuppressedPaths { get; }

    public IReadOnlyList<string> ChangedFiles { get; }

    public IReadOnlyList<string> DeletedPaths { get; }

    public IReadOnlyList<ExternalMovedPath> MovedPaths { get; }

    public IReadOnlyList<string> ChangedObjects { get; }

    public IReadOnlyList<string> CreatedObjects { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public IReadOnlyList<WorkspaceTransactionConflict> Conflicts { get; }

    public int DirectoryScanCount { get; }

    public IReadOnlyList<string> ScannedDirectories { get; }

    public bool FullProjectRescan { get; }

    public TimeSpan MaxAppliedDelay { get; }

    public IReadOnlyList<ExternalChangeImportRecord> ImportRecords { get; }

    private static IReadOnlyList<string> CopyPaths(IEnumerable<string> paths)
    {
        return paths.Distinct(StringComparer.Ordinal).OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }
}

internal sealed class ExternalChangeFileWatcher : IDisposable
{
    private readonly FileSystemWatcher watcher;
    private readonly ExternalChangeSynchronizer synchronizer;

    public ExternalChangeFileWatcher(string projectRoot, ExternalChangeSynchronizer synchronizer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(synchronizer);

        this.synchronizer = synchronizer;
        watcher = new FileSystemWatcher(projectRoot)
        {
            IncludeSubdirectories = true,
            EnableRaisingEvents = false,
            NotifyFilter = NotifyFilters.FileName |
                NotifyFilters.DirectoryName |
                NotifyFilters.LastWrite |
                NotifyFilters.Size |
                NotifyFilters.CreationTime
        };

        watcher.Created += OnCreated;
        watcher.Changed += OnChanged;
        watcher.Deleted += OnDeleted;
        watcher.Renamed += OnRenamed;
        watcher.Error += OnError;
    }

    public bool IncludeSubdirectories => watcher.IncludeSubdirectories;

    public bool IsRunning => watcher.EnableRaisingEvents;

    public void Start()
    {
        watcher.EnableRaisingEvents = true;
    }

    public void Dispose()
    {
        watcher.Dispose();
    }

    private void OnCreated(object sender, FileSystemEventArgs args)
    {
        synchronizer.Notify(ExternalFileChangeEvent.Created(ToRelativePath(args.FullPath), DateTimeOffset.UtcNow));
    }

    private void OnChanged(object sender, FileSystemEventArgs args)
    {
        synchronizer.Notify(ExternalFileChangeEvent.Changed(ToRelativePath(args.FullPath), DateTimeOffset.UtcNow));
    }

    private void OnDeleted(object sender, FileSystemEventArgs args)
    {
        synchronizer.Notify(ExternalFileChangeEvent.Deleted(ToRelativePath(args.FullPath), DateTimeOffset.UtcNow));
    }

    private void OnRenamed(object sender, RenamedEventArgs args)
    {
        synchronizer.Notify(ExternalFileChangeEvent.Renamed(ToRelativePath(args.OldFullPath), ToRelativePath(args.FullPath), DateTimeOffset.UtcNow));
    }

    private void OnError(object sender, ErrorEventArgs args)
    {
        synchronizer.Notify(ExternalFileChangeEvent.Overflow(".", DateTimeOffset.UtcNow));
    }

    private string ToRelativePath(string fullPath)
    {
        return Path.GetRelativePath(watcher.Path, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }
}

internal sealed class ExternalChangeSynchronizer
{
    private readonly ProjectWorkspace workspace;
    private readonly ExternalChangeSynchronizerOptions options;
    private readonly Dictionary<string, PendingExternalChange> pendingChanges = new(StringComparer.Ordinal);
    private readonly Dictionary<string, FileFingerprint> knownFiles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, DateTimeOffset> suppressedOwnWrites = new(StringComparer.Ordinal);
    private readonly List<string> ignoredSinceLastDrain = [];
    private readonly List<string> suppressedSinceLastDrain = [];

    public ExternalChangeSynchronizer(ProjectWorkspace workspace, ExternalChangeSynchronizerOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        this.workspace = workspace;
        this.options = options ?? ExternalChangeSynchronizerOptions.Default;
        foreach (var document in workspace.Documents.Documents)
        {
            knownFiles[document.Path] = FileFingerprint.FromText(document.Text);
        }
    }

    public ExternalChangeFileWatcher CreateWatcher()
    {
        return new ExternalChangeFileWatcher(workspace.ProjectRoot, this);
    }

    public IDisposable SuppressOwnWrite(string path, DateTimeOffset timestampUtc)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        suppressedOwnWrites[normalizedPath] = timestampUtc.Add(options.SelfWriteSuppressionWindow);
        return new OwnWriteSuppressionToken();
    }

    public void Notify(ExternalFileChangeEvent changeEvent)
    {
        ArgumentNullException.ThrowIfNull(changeEvent);
        PruneExpiredSuppressions(changeEvent.TimestampUtc);

        if (changeEvent.Kind == ExternalFileChangeKind.Resume)
        {
            pendingChanges["$resume"] = PendingExternalChange.From(changeEvent);
            return;
        }

        if (changeEvent.Kind == ExternalFileChangeKind.Overflow)
        {
            var directory = NormalizeDirectory(changeEvent.DirectoryPath ?? ".");
            if (ShouldIgnoreDirectory(directory))
            {
                ignoredSinceLastDrain.Add(directory);
                return;
            }

            pendingChanges[$"$overflow:{directory}"] = PendingExternalChange.From(changeEvent, directory);
            return;
        }

        var path = ProjectDocumentPaths.NormalizeRelativePath(changeEvent.Path ?? throw new ArgumentException("External file change path is required."));
        if (ShouldIgnorePath(path))
        {
            ignoredSinceLastDrain.Add(path);
            return;
        }

        if (suppressedOwnWrites.TryGetValue(path, out var suppressedUntil) && changeEvent.TimestampUtc <= suppressedUntil)
        {
            suppressedSinceLastDrain.Add(path);
            return;
        }

        var oldPath = changeEvent.OldPath is null ? null : ProjectDocumentPaths.NormalizeRelativePath(changeEvent.OldPath);
        if (oldPath is not null && ShouldIgnorePath(oldPath))
        {
            ignoredSinceLastDrain.Add(oldPath);
            return;
        }

        pendingChanges[BuildPendingKey(changeEvent.Kind, path)] = PendingExternalChange.From(changeEvent, path, oldPath);
    }

    public ExternalChangeSynchronizerResult Drain(DateTimeOffset nowUtc)
    {
        PruneExpiredSuppressions(nowUtc);
        var builder = new ExternalChangeResultBuilder(ignoredSinceLastDrain, suppressedSinceLastDrain);
        ignoredSinceLastDrain.Clear();
        suppressedSinceLastDrain.Clear();

        var dueChanges = pendingChanges
            .Where(pair => nowUtc - pair.Value.TimestampUtc >= options.DebounceDelay)
            .OrderBy(pair => pair.Value.TimestampUtc)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();

        foreach (var (key, change) in dueChanges)
        {
            pendingChanges.Remove(key);
            builder.RecordDelay(nowUtc - change.TimestampUtc);
            Process(change, builder);
        }

        return builder.Build();
    }

    private void Process(PendingExternalChange change, ExternalChangeResultBuilder builder)
    {
        switch (change.Kind)
        {
            case ExternalFileChangeKind.Created:
            case ExternalFileChangeKind.Changed:
                ProcessUpsert(change.Path!, builder);
                break;
            case ExternalFileChangeKind.Deleted:
                ProcessDelete(change.Path!, builder);
                break;
            case ExternalFileChangeKind.Renamed:
                ProcessRename(change.OldPath!, change.Path!, builder);
                break;
            case ExternalFileChangeKind.Overflow:
                ProcessDirectoryScan(change.DirectoryPath!, builder);
                break;
            case ExternalFileChangeKind.Resume:
                ProcessResume(builder);
                break;
            default:
                builder.AddDiagnostic(CreateDiagnostic($"External change kind '{change.Kind}' is not supported."));
                break;
        }
    }

    private void ProcessUpsert(string relativePath, ExternalChangeResultBuilder builder)
    {
        if (ShouldIgnorePath(relativePath))
        {
            builder.AddIgnored(relativePath);
            return;
        }

        var fullPath = ResolveProjectPath(relativePath);
        if (!File.Exists(fullPath))
        {
            builder.AddDiagnostic(CreateDiagnostic($"External file '{relativePath}' was not found."));
            return;
        }

        if (IsBinaryAsset(relativePath))
        {
            workspace.ImportState.SetState(relativePath, "Importing");
            knownFiles[relativePath] = FileFingerprint.FromFile(fullPath);
            builder.AddProcessed(relativePath);
            builder.AddChangedFile(relativePath);
            builder.AddImportRecord(CreateImportRecord(relativePath, "external.asset-import", routedThroughTaskManager: false, usedWorkspaceTransaction: false, "Importing"));
            return;
        }

        string text;
        try
        {
            text = File.ReadAllText(fullPath, Encoding.UTF8).ReplaceLineEndings("\n");
        }
        catch (IOException exception)
        {
            builder.AddDiagnostic(CreateDiagnostic($"External file '{relativePath}' could not be read: {exception.Message}"));
            return;
        }

        if (!workspace.Documents.TryGetByPath(relativePath, out var document))
        {
            OpenCreatedTextDocument(relativePath, text, builder);
            return;
        }

        if (IsTaskFile(relativePath))
        {
            ImportTaskDocument(relativePath, text, document.InMemoryRevision, builder);
            return;
        }

        var transaction = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            BuildOperationId("external-import", relativePath),
            ProjectWorkspaceActorKind.ExternalFile,
            "external.import",
            WorkspaceTransactionMode.ExternalImport,
            dryRun: false,
            undoGroupId: BuildUndoGroupId("external-import", relativePath),
            [WorkspaceTransactionDocumentEdit.ReplaceText(relativePath, document.InMemoryRevision, text)]));
        builder.AddProcessed(relativePath);
        builder.AddChangedFile(relativePath);
        builder.AddTransactionResult(transaction);
        builder.AddImportRecord(CreateImportRecord(relativePath, transaction.OperationId, routedThroughTaskManager: false, usedWorkspaceTransaction: true, string.Empty));
        if (transaction.Succeeded)
        {
            knownFiles[relativePath] = FileFingerprint.FromText(text);
        }
        else
        {
            workspace.ImportState.SetState(relativePath, "pending-conflict");
        }
    }

    private void OpenCreatedTextDocument(string relativePath, string text, ExternalChangeResultBuilder builder)
    {
        if (IsTaskFile(relativePath))
        {
            ValidateTaskDocumentForCreate(relativePath, text, builder);
        }

        var operationId = BuildOperationId("external-create", relativePath);
        var context = new ProjectWorkspaceOperationContext(
            operationId,
            ProjectWorkspaceActorKind.ExternalFile,
            "external.create");
        var result = workspace.CommandBus.OpenTextDocument(relativePath, text, persistedRevision: 1, context);
        builder.AddProcessed(relativePath);
        builder.AddChangedFile(relativePath);
        if (!result.Succeeded)
        {
            builder.AddDiagnostic(CreateDiagnostic(result.Message));
            return;
        }

        knownFiles[relativePath] = FileFingerprint.FromText(text);
        builder.AddImportRecord(CreateImportRecord(relativePath, operationId, IsTaskFile(relativePath), usedWorkspaceTransaction: false, string.Empty));
    }

    private void ValidateTaskDocumentForCreate(string relativePath, string text, ExternalChangeResultBuilder builder)
    {
        try
        {
            if (relativePath.EndsWith(".e2task", StringComparison.Ordinal))
            {
                ProjectTaskSerializer.DeserializeTask(relativePath, text);
            }
            else if (relativePath.EndsWith(".e2tasks", StringComparison.Ordinal))
            {
                ProjectTaskSerializer.DeserializeBoard(relativePath, text);
            }
        }
        catch (FormatException exception)
        {
            workspace.ImportState.SetState(relativePath, "pending-conflict");
            builder.AddDiagnostic(CreateDiagnostic(exception.Message));
        }
    }

    private void ImportTaskDocument(
        string relativePath,
        string text,
        ProjectDocumentRevision expectedRevision,
        ExternalChangeResultBuilder builder)
    {
        var context = CreateExternalTaskContext();
        var operationId = BuildOperationId("external-task-import", relativePath);
        var undoGroupId = BuildUndoGroupId("external-task-import", relativePath);

        if (relativePath.EndsWith(".e2tasks", StringComparison.Ordinal))
        {
            var transaction = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
                operationId,
                ProjectWorkspaceActorKind.ExternalFile,
                "task-board.external-import",
                WorkspaceTransactionMode.ExternalImport,
                dryRun: false,
                undoGroupId,
                [WorkspaceTransactionDocumentEdit.ReplaceText(relativePath, expectedRevision, text)]));
            builder.AddProcessed(relativePath);
            builder.AddChangedFile(relativePath);
            builder.AddTransactionResult(transaction);
            builder.AddImportRecord(CreateImportRecord(relativePath, operationId, routedThroughTaskManager: false, usedWorkspaceTransaction: true, string.Empty));
            if (transaction.Succeeded)
            {
                knownFiles[relativePath] = FileFingerprint.FromText(text);
            }
            else
            {
                workspace.ImportState.SetState(relativePath, "pending-conflict");
            }

            return;
        }

        var taskResult = workspace.Tasks.ImportExternalChange(new ProjectTaskExternalImportRequest(
            relativePath,
            text,
            expectedRevision,
            operationId,
            undoGroupId,
            context));
        builder.AddProcessed(relativePath);
        builder.AddChangedFile(relativePath);
        builder.AddTaskResult(taskResult);
        builder.AddImportRecord(new ExternalChangeImportRecord(
            relativePath,
            operationId,
            context.PrincipalKind,
            context.Capabilities,
            context.Origin,
            routedThroughTaskManager: true,
            usedWorkspaceTransaction: taskResult.TransactionResult.Mode == WorkspaceTransactionMode.ExternalImport,
            fileSystemDockStatus: string.Empty));
        if (taskResult.Succeeded)
        {
            knownFiles[relativePath] = FileFingerprint.FromText(text);
        }
        else
        {
            workspace.ImportState.SetState(relativePath, "pending-conflict");
        }
    }

    private void ProcessRename(string oldPath, string newPath, ExternalChangeResultBuilder builder)
    {
        if (ShouldIgnorePath(newPath))
        {
            builder.AddIgnored(newPath);
            return;
        }

        var fullPath = ResolveProjectPath(newPath);
        if (!File.Exists(fullPath))
        {
            builder.AddDiagnostic(CreateDiagnostic($"External rename target '{newPath}' was not found."));
            return;
        }

        var reconciledOldPath = workspace.Documents.TryGetByPath(oldPath, out var oldDocument)
            ? oldPath
            : ReconcileOldPath(newPath, fullPath, builder);
        if (reconciledOldPath is null)
        {
            ProcessUpsert(newPath, builder);
            return;
        }

        oldDocument = workspace.Documents.GetByPath(reconciledOldPath);
        if (oldDocument.IsDirty)
        {
            workspace.ImportState.SetState(reconciledOldPath, "pending-conflict");
            builder.AddDiagnostic(CreateDiagnostic($"External rename '{reconciledOldPath}' -> '{newPath}' conflicts with a dirty document."));
            return;
        }

        if (IsBinaryAsset(newPath))
        {
            workspace.ImportState.SetState(newPath, "Importing");
            knownFiles.Remove(reconciledOldPath);
            knownFiles[newPath] = FileFingerprint.FromFile(fullPath);
            builder.AddProcessed(newPath);
            builder.AddChangedFile(newPath);
            builder.AddMovedPath(reconciledOldPath, newPath);
            builder.AddImportRecord(CreateImportRecord(newPath, "external.asset-rename", routedThroughTaskManager: false, usedWorkspaceTransaction: false, "Importing"));
            return;
        }

        var text = File.ReadAllText(fullPath, Encoding.UTF8).ReplaceLineEndings("\n");
        workspace.Documents.RemoveTextDocument(reconciledOldPath, out _);
        var nextRevision = oldDocument.InMemoryRevision.Next();
        var movedDocument = workspace.Documents.PutTextDocumentState(newPath, text, nextRevision, nextRevision, text);
        workspace.Revisions.RecordDocumentMoved(reconciledOldPath, movedDocument);
        var operationId = BuildOperationId("external-rename", newPath);
        workspace.OperationJournal.RecordCompleted(
            new ProjectWorkspaceOperationContext(operationId, ProjectWorkspaceActorKind.ExternalFile, "external.rename"),
            [reconciledOldPath, newPath],
            DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DocumentMoved,
            workspace.Revisions.WorkspaceRevision,
            movedDocument.DocumentId,
            movedDocument.Path,
            operationId,
            source: null,
            diagnostics: []));

        knownFiles.Remove(reconciledOldPath);
        knownFiles[newPath] = FileFingerprint.FromText(text);
        builder.AddProcessed(newPath);
        builder.AddChangedFile(newPath);
        builder.AddMovedPath(reconciledOldPath, newPath);
        builder.AddImportRecord(CreateImportRecord(newPath, operationId, IsTaskFile(newPath), usedWorkspaceTransaction: false, string.Empty));
    }

    private void ProcessDelete(string relativePath, ExternalChangeResultBuilder builder)
    {
        if (ShouldIgnorePath(relativePath))
        {
            builder.AddIgnored(relativePath);
            return;
        }

        if (!workspace.Documents.TryGetByPath(relativePath, out var document))
        {
            knownFiles.Remove(relativePath);
            builder.AddProcessed(relativePath);
            builder.AddDeletedPath(relativePath);
            return;
        }

        if (document.IsDirty)
        {
            workspace.ImportState.SetState(relativePath, "pending-conflict");
            builder.AddDiagnostic(CreateDiagnostic($"External delete of '{relativePath}' conflicts with a dirty document."));
            return;
        }

        workspace.Documents.RemoveTextDocument(relativePath, out _);
        workspace.Revisions.RecordDocumentDeleted(relativePath);
        var operationId = BuildOperationId("external-delete", relativePath);
        workspace.OperationJournal.RecordCompleted(
            new ProjectWorkspaceOperationContext(operationId, ProjectWorkspaceActorKind.ExternalFile, "external.delete"),
            [relativePath],
            DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DocumentDeleted,
            workspace.Revisions.WorkspaceRevision,
            document.DocumentId,
            relativePath,
            operationId,
            source: null,
            diagnostics: []));
        knownFiles.Remove(relativePath);
        builder.AddProcessed(relativePath);
        builder.AddDeletedPath(relativePath);
    }

    private void ProcessDirectoryScan(string directoryPath, ExternalChangeResultBuilder builder)
    {
        var directory = NormalizeDirectory(directoryPath);
        builder.AddDirectoryScan(directory);
        var currentFiles = EnumerateTrackedFiles(directory)
            .ToDictionary(path => path, path => FileFingerprint.FromFile(ResolveProjectPath(path)), StringComparer.Ordinal);

        foreach (var (path, fingerprint) in currentFiles.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!knownFiles.TryGetValue(path, out var known) || !known.Equals(fingerprint))
            {
                ProcessUpsert(path, builder);
            }
        }

        foreach (var path in knownFiles.Keys
            .Where(path => IsInDirectory(path, directory) && !currentFiles.ContainsKey(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray())
        {
            ProcessDelete(path, builder);
        }
    }

    private void ProcessResume(ExternalChangeResultBuilder builder)
    {
        var directories = knownFiles.Keys
            .Concat(workspace.Documents.Documents.Select(document => document.Path))
            .Select(GetDirectoryName)
            .Where(path => !ShouldIgnoreDirectory(path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var directory in directories.Length == 0 ? ["."] : directories)
        {
            ProcessDirectoryScan(directory, builder);
        }
    }

    private string? ReconcileOldPath(string newPath, string fullPath, ExternalChangeResultBuilder builder)
    {
        var fingerprint = FileFingerprint.FromFile(fullPath);
        var byUid = !string.IsNullOrWhiteSpace(fingerprint.Uid)
            ? knownFiles.Where(pair => string.Equals(pair.Value.Uid, fingerprint.Uid, StringComparison.Ordinal)).Select(pair => pair.Key).ToArray()
            : [];
        if (byUid.Length == 1)
        {
            return byUid[0];
        }

        var byHash = knownFiles.Where(pair => string.Equals(pair.Value.Hash, fingerprint.Hash, StringComparison.Ordinal)).Select(pair => pair.Key).ToArray();
        if (byHash.Length == 1)
        {
            return byHash[0];
        }

        if (byUid.Length > 1 || byHash.Length > 1)
        {
            workspace.ImportState.SetState(newPath, "pending-conflict");
            builder.AddDiagnostic(CreateDiagnostic($"External rename target '{newPath}' has ambiguous UID or hash reconciliation."));
        }

        return null;
    }

    private IReadOnlyList<string> EnumerateTrackedFiles(string directoryPath)
    {
        var directory = ResolveDirectoryPath(directoryPath);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .Select(ToRelativePath)
            .Where(path => !ShouldIgnorePath(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private string ResolveProjectPath(string relativePath)
    {
        var projectRoot = Path.GetFullPath(workspace.ProjectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var candidate = Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(projectRoot, candidate);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"External change path escapes project root: {candidate}");
        }

        return candidate;
    }

    private string ResolveDirectoryPath(string directoryPath)
    {
        if (directoryPath == ".")
        {
            return Path.GetFullPath(workspace.ProjectRoot);
        }

        return ResolveProjectPath(directoryPath);
    }

    private string ToRelativePath(string fullPath)
    {
        return Path.GetRelativePath(workspace.ProjectRoot, fullPath)
            .Replace(Path.DirectorySeparatorChar, '/')
            .Replace(Path.AltDirectorySeparatorChar, '/');
    }

    private void PruneExpiredSuppressions(DateTimeOffset nowUtc)
    {
        foreach (var path in suppressedOwnWrites
            .Where(pair => pair.Value < nowUtc)
            .Select(pair => pair.Key)
            .ToArray())
        {
            suppressedOwnWrites.Remove(path);
        }
    }

    private static OperationContext CreateExternalTaskContext()
    {
        return new OperationContext(
            "file-watcher",
            PrincipalKind.ExternalFile,
            "external-import",
            [OperationCapability.TaskEditUnprivilegedFields],
            "ExternalImport");
    }

    private ExternalChangeImportRecord CreateImportRecord(
        string path,
        string operationId,
        bool routedThroughTaskManager,
        bool usedWorkspaceTransaction,
        string fileSystemDockStatus)
    {
        return new ExternalChangeImportRecord(
            path,
            operationId,
            PrincipalKind.ExternalFile,
            new HashSet<OperationCapability> { OperationCapability.TaskEditUnprivilegedFields },
            "ExternalImport",
            routedThroughTaskManager,
            usedWorkspaceTransaction,
            fileSystemDockStatus);
    }

    private static StructuredDiagnostic CreateDiagnostic(string message)
    {
        return StructuredDiagnostic.Create(
            "E2D-TOOLING-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static string BuildPendingKey(ExternalFileChangeKind kind, string path)
    {
        return kind == ExternalFileChangeKind.Deleted ? $"delete:{path}" : path;
    }

    private static string BuildOperationId(string prefix, string path)
    {
        var safePath = new string(ProjectDocumentPaths.NormalizeRelativePath(path)
            .Select(character => char.IsAsciiLetterOrDigit(character) ? character : '-')
            .ToArray())
            .Trim('-');
        return $"op-{prefix}-{safePath}";
    }

    private static string BuildUndoGroupId(string prefix, string path)
    {
        return BuildOperationId(prefix, path).Replace("op-", "undo-", StringComparison.Ordinal);
    }

    private static bool IsTaskFile(string path)
    {
        return path.EndsWith(".e2task", StringComparison.Ordinal) || path.EndsWith(".e2tasks", StringComparison.Ordinal);
    }

    private static bool IsBinaryAsset(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".wav", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ogg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mp3", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ttf", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".otf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldIgnorePath(string path)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        var fileName = ProjectDocumentPaths.GetFileName(normalizedPath);
        return normalizedPath.StartsWith(".git/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/import-cache/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/workspaces/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/context/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/session/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("bin/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("obj/", StringComparison.Ordinal) ||
            normalizedPath.Contains("/generated/", StringComparison.Ordinal) ||
            fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".swp", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
            (fileName.StartsWith(".", StringComparison.Ordinal) && fileName.Contains(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldIgnoreDirectory(string directory)
    {
        var normalized = NormalizeDirectory(directory);
        return normalized == ".git" ||
            normalized.StartsWith(".git/", StringComparison.Ordinal) ||
            normalized == ".electron2d/import-cache" ||
            normalized.StartsWith(".electron2d/import-cache/", StringComparison.Ordinal) ||
            normalized == ".electron2d/workspaces" ||
            normalized.StartsWith(".electron2d/workspaces/", StringComparison.Ordinal) ||
            normalized == ".electron2d/context" ||
            normalized.StartsWith(".electron2d/context/", StringComparison.Ordinal) ||
            normalized == ".electron2d/session" ||
            normalized.StartsWith(".electron2d/session/", StringComparison.Ordinal) ||
            normalized == "bin" ||
            normalized.StartsWith("bin/", StringComparison.Ordinal) ||
            normalized == "obj" ||
            normalized.StartsWith("obj/", StringComparison.Ordinal);
    }

    private static string NormalizeDirectory(string directoryPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        var normalized = directoryPath.Replace('\\', '/').Trim('/');
        if (normalized.Length == 0 || normalized == ".")
        {
            return ".";
        }

        return ProjectDocumentPaths.NormalizeRelativePath(normalized);
    }

    private static string GetDirectoryName(string relativePath)
    {
        var index = relativePath.LastIndexOf('/');
        return index < 0 ? "." : relativePath[..index];
    }

    private static bool IsInDirectory(string path, string directory)
    {
        return directory == "." || path.StartsWith(directory + "/", StringComparison.Ordinal) || string.Equals(path, directory, StringComparison.Ordinal);
    }

    private sealed class PendingExternalChange
    {
        private PendingExternalChange(
            ExternalFileChangeKind kind,
            DateTimeOffset timestampUtc,
            string? path,
            string? oldPath,
            string? directoryPath)
        {
            Kind = kind;
            TimestampUtc = timestampUtc;
            Path = path;
            OldPath = oldPath;
            DirectoryPath = directoryPath;
        }

        public ExternalFileChangeKind Kind { get; }

        public DateTimeOffset TimestampUtc { get; }

        public string? Path { get; }

        public string? OldPath { get; }

        public string? DirectoryPath { get; }

        public static PendingExternalChange From(ExternalFileChangeEvent changeEvent)
        {
            return new PendingExternalChange(changeEvent.Kind, changeEvent.TimestampUtc, changeEvent.Path, changeEvent.OldPath, changeEvent.DirectoryPath);
        }

        public static PendingExternalChange From(ExternalFileChangeEvent changeEvent, string directoryPath)
        {
            return new PendingExternalChange(changeEvent.Kind, changeEvent.TimestampUtc, path: null, oldPath: null, directoryPath);
        }

        public static PendingExternalChange From(ExternalFileChangeEvent changeEvent, string path, string? oldPath)
        {
            return new PendingExternalChange(changeEvent.Kind, changeEvent.TimestampUtc, path, oldPath, directoryPath: null);
        }
    }

    private sealed class ExternalChangeResultBuilder
    {
        private readonly List<string> processedPaths = [];
        private readonly List<string> ignoredPaths;
        private readonly List<string> suppressedPaths;
        private readonly List<string> changedFiles = [];
        private readonly List<string> deletedPaths = [];
        private readonly List<ExternalMovedPath> movedPaths = [];
        private readonly List<string> changedObjects = [];
        private readonly List<string> createdObjects = [];
        private readonly List<StructuredDiagnostic> diagnostics = [];
        private readonly List<WorkspaceTransactionConflict> conflicts = [];
        private readonly List<string> scannedDirectories = [];
        private readonly List<ExternalChangeImportRecord> importRecords = [];
        private TimeSpan maxAppliedDelay = TimeSpan.Zero;

        public ExternalChangeResultBuilder(IEnumerable<string> ignoredPaths, IEnumerable<string> suppressedPaths)
        {
            this.ignoredPaths = ignoredPaths.ToList();
            this.suppressedPaths = suppressedPaths.ToList();
        }

        public int DirectoryScanCount { get; private set; }

        public bool FullProjectRescan { get; private set; }

        public void RecordDelay(TimeSpan delay)
        {
            if (delay > maxAppliedDelay)
            {
                maxAppliedDelay = delay;
            }
        }

        public void AddProcessed(string path)
        {
            processedPaths.Add(ProjectDocumentPaths.NormalizeRelativePath(path));
        }

        public void AddIgnored(string path)
        {
            ignoredPaths.Add(ProjectDocumentPaths.NormalizeRelativePath(path));
        }

        public void AddChangedFile(string path)
        {
            changedFiles.Add(ProjectDocumentPaths.NormalizeRelativePath(path));
        }

        public void AddDeletedPath(string path)
        {
            deletedPaths.Add(ProjectDocumentPaths.NormalizeRelativePath(path));
        }

        public void AddMovedPath(string oldPath, string newPath)
        {
            movedPaths.Add(new ExternalMovedPath(oldPath, newPath));
        }

        public void AddDiagnostic(StructuredDiagnostic diagnostic)
        {
            diagnostics.Add(diagnostic);
        }

        public void AddDirectoryScan(string directory)
        {
            DirectoryScanCount++;
            var normalized = NormalizeDirectory(directory);
            if (normalized == ".")
            {
                FullProjectRescan = true;
            }

            scannedDirectories.Add(normalized);
        }

        public void AddTransactionResult(WorkspaceTransactionResult result)
        {
            changedObjects.AddRange(result.ChangedObjects);
            createdObjects.AddRange(result.CreatedObjects);
            diagnostics.AddRange(result.Diagnostics);
            conflicts.AddRange(result.Conflicts);
        }

        public void AddTaskResult(ProjectTaskMutationResult result)
        {
            AddTransactionResult(result.TransactionResult);
            diagnostics.AddRange(result.Diagnostics);
        }

        public void AddImportRecord(ExternalChangeImportRecord record)
        {
            importRecords.Add(record);
        }

        public ExternalChangeSynchronizerResult Build()
        {
            return new ExternalChangeSynchronizerResult(
                processedPaths,
                ignoredPaths,
                suppressedPaths,
                changedFiles,
                deletedPaths,
                movedPaths,
                changedObjects,
                createdObjects,
                diagnostics.DistinctBy(diagnostic => diagnostic.Code + diagnostic.Message),
                conflicts,
                DirectoryScanCount,
                scannedDirectories,
                FullProjectRescan,
                maxAppliedDelay,
                importRecords);
        }
    }

    private sealed class OwnWriteSuppressionToken : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private readonly record struct FileFingerprint(long Length, string Hash, string? Uid)
    {
        public static FileFingerprint FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            return new FileFingerprint(bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)), TryReadUid(bytes));
        }

        public static FileFingerprint FromText(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text.ReplaceLineEndings("\n"));
            return new FileFingerprint(bytes.Length, Convert.ToHexString(SHA256.HashData(bytes)), TryReadUid(bytes));
        }

        private static string? TryReadUid(byte[] bytes)
        {
            try
            {
                var root = JsonNode.Parse(Encoding.UTF8.GetString(bytes)) as JsonObject;
                return root is not null &&
                    root.TryGetPropertyValue("uid", out var uidNode) &&
                    uidNode is JsonValue value &&
                    value.TryGetValue<string>(out var uid) &&
                    !string.IsNullOrWhiteSpace(uid)
                        ? uid
                        : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
