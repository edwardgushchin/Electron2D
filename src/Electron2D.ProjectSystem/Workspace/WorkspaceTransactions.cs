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
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal enum WorkspaceTransactionMode
{
    WorkspaceOnly,
    SaveAffectedDocuments,
    HeadlessCommit,
    ExternalImport
}

internal enum WorkspaceTransactionConflictKind
{
    PropertyConflict,
    DeletedChangedObject,
    UnsupportedChange
}

internal sealed class WorkspaceTransactionDocumentEdit
{
    private WorkspaceTransactionDocumentEdit(
        string path,
        ProjectDocumentRevision expectedRevision,
        string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);

        Path = path;
        ExpectedRevision = expectedRevision;
        Text = text.ReplaceLineEndings("\n");
    }

    public string Path { get; }

    public ProjectDocumentRevision ExpectedRevision { get; }

    public string Text { get; }

    public static WorkspaceTransactionDocumentEdit ReplaceText(
        string path,
        ProjectDocumentRevision expectedRevision,
        string text)
    {
        return new WorkspaceTransactionDocumentEdit(path, expectedRevision, text);
    }
}

internal sealed class WorkspaceTransactionRequest
{
    public WorkspaceTransactionRequest(
        string operationId,
        ProjectWorkspaceActorKind actorKind,
        string operationKind,
        WorkspaceTransactionMode mode,
        bool dryRun,
        string? undoGroupId,
        IEnumerable<WorkspaceTransactionDocumentEdit> edits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationKind);
        ArgumentNullException.ThrowIfNull(edits);

        OperationId = operationId;
        ActorKind = actorKind;
        OperationKind = operationKind;
        Mode = mode;
        DryRun = dryRun;
        UndoGroupId = undoGroupId;
        Edits = edits.ToArray();
    }

    public string OperationId { get; }

    public ProjectWorkspaceActorKind ActorKind { get; }

    public string OperationKind { get; }

    public WorkspaceTransactionMode Mode { get; }

    public bool DryRun { get; }

    public string? UndoGroupId { get; }

    public IReadOnlyList<WorkspaceTransactionDocumentEdit> Edits { get; }

    public static WorkspaceTransactionRequest SaveAffectedDocuments(
        string operationId,
        ProjectWorkspaceActorKind actorKind,
        bool dryRun)
    {
        return new WorkspaceTransactionRequest(
            operationId,
            actorKind,
            "workspace.save-affected-documents",
            WorkspaceTransactionMode.SaveAffectedDocuments,
            dryRun,
            undoGroupId: null,
            edits: []);
    }
}

internal sealed class WorkspaceTransactionConflict
{
    public WorkspaceTransactionConflict(
        WorkspaceTransactionConflictKind kind,
        string documentPath,
        string objectUid,
        string? propertyPath,
        string message)
    {
        Kind = kind;
        DocumentPath = ProjectDocumentPaths.NormalizeRelativePath(documentPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectUid);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        ObjectUid = objectUid;
        PropertyPath = propertyPath;
        Message = message;
    }

    public WorkspaceTransactionConflictKind Kind { get; }

    public string DocumentPath { get; }

    public string ObjectUid { get; }

    public string? PropertyPath { get; }

    public string Message { get; }
}

internal sealed class WorkspaceTransactionResult
{
    public WorkspaceTransactionResult(
        bool succeeded,
        WorkspaceTransactionMode mode,
        bool dryRun,
        string operationId,
        string? undoGroupId,
        ProjectWorkspaceRevision workspaceRevision,
        ProjectWorkspaceRevision contentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> documentRevisions,
        IReadOnlyDictionary<string, ProjectDocumentRevision> persistedRevisions,
        IReadOnlyList<string> dirtyDocuments,
        ProjectWorkspacePersistenceState persistenceState,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> changedObjects,
        IReadOnlyList<string> createdObjects,
        IReadOnlyList<WorkspaceTransactionConflict> conflicts,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IReadOnlyList<string> backupFiles)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentNullException.ThrowIfNull(documentRevisions);
        ArgumentNullException.ThrowIfNull(persistedRevisions);
        ArgumentNullException.ThrowIfNull(dirtyDocuments);
        ArgumentNullException.ThrowIfNull(changedFiles);
        ArgumentNullException.ThrowIfNull(changedObjects);
        ArgumentNullException.ThrowIfNull(createdObjects);
        ArgumentNullException.ThrowIfNull(conflicts);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(backupFiles);

        Succeeded = succeeded;
        Mode = mode;
        DryRun = dryRun;
        OperationId = operationId;
        UndoGroupId = undoGroupId;
        WorkspaceRevision = workspaceRevision;
        ContentRevision = contentRevision;
        DocumentRevisions = CopyDictionary(documentRevisions);
        PersistedRevisions = CopyDictionary(persistedRevisions);
        DirtyDocuments = dirtyDocuments.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        PersistenceState = persistenceState;
        ChangedFiles = changedFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        ChangedObjects = changedObjects.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        CreatedObjects = createdObjects.OrderBy(value => value, StringComparer.Ordinal).ToArray();
        Conflicts = conflicts.ToArray();
        Diagnostics = diagnostics.ToArray();
        BackupFiles = backupFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    public bool Succeeded { get; }

    public WorkspaceTransactionMode Mode { get; }

    public bool DryRun { get; }

    public string OperationId { get; }

    public string? UndoGroupId { get; }

    public ProjectWorkspaceRevision WorkspaceRevision { get; }

    public ProjectWorkspaceRevision ContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> DocumentRevisions { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> PersistedRevisions { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }

    public ProjectWorkspacePersistenceState PersistenceState { get; }

    public IReadOnlyList<string> ChangedFiles { get; }

    public IReadOnlyList<string> ChangedObjects { get; }

    public IReadOnlyList<string> CreatedObjects { get; }

    public IReadOnlyList<WorkspaceTransactionConflict> Conflicts { get; }

    public IReadOnlyList<StructuredDiagnostic> Diagnostics { get; }

    public IReadOnlyList<string> BackupFiles { get; }

    private static IReadOnlyDictionary<string, ProjectDocumentRevision> CopyDictionary(
        IReadOnlyDictionary<string, ProjectDocumentRevision> source)
    {
        return new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            source.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }
}

internal sealed class WorkspaceTransactionEngine
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ProjectWorkspace workspace;

    public WorkspaceTransactionEngine(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        this.workspace = workspace;
    }

    public WorkspaceTransactionResult Apply(WorkspaceTransactionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return request.Mode switch
            {
                WorkspaceTransactionMode.WorkspaceOnly => ApplyWorkspaceOnly(request),
                WorkspaceTransactionMode.SaveAffectedDocuments => ApplySaveAffectedDocuments(request),
                WorkspaceTransactionMode.HeadlessCommit => ApplyHeadlessCommit(request),
                WorkspaceTransactionMode.ExternalImport => ApplyExternalImport(request),
                _ => Failure(request, $"Workspace transaction mode '{request.Mode}' is not supported.")
            };
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException or IOException or UnauthorizedAccessException)
        {
            return Failure(request, exception.Message);
        }
    }

    private WorkspaceTransactionResult ApplyWorkspaceOnly(WorkspaceTransactionRequest request)
    {
        var plan = ValidateTextEdits(request);
        if (!plan.Succeeded)
        {
            return Failure(request, plan.Message, changedObjects: plan.ChangedObjects, createdObjects: plan.CreatedObjects);
        }

        if (request.DryRun)
        {
            return Success(request, changedObjects: plan.ChangedObjects, createdObjects: plan.CreatedObjects);
        }

        var beforeStates = CaptureUndoStates(plan.Edits.Select(edit => edit.Path));
        var context = CreateContext(request);
        foreach (var edit in plan.Edits)
        {
            var result = workspace.CommandBus.ReplaceTextDocument(
                edit.Path,
                edit.Text,
                edit.ExpectedRevision,
                context);
            if (!result.Succeeded)
            {
                return Failure(request, result.Message, changedObjects: plan.ChangedObjects, createdObjects: plan.CreatedObjects);
            }
        }

        AddUndoGroup(request, beforeStates, CaptureUndoStates(plan.Edits.Select(edit => edit.Path)));
        return Success(request, changedObjects: plan.ChangedObjects, createdObjects: plan.CreatedObjects);
    }

    private WorkspaceTransactionResult ApplySaveAffectedDocuments(WorkspaceTransactionRequest request)
    {
        if (request.Edits.Count != 0)
        {
            return Failure(request, "SaveAffectedDocuments transaction does not accept document edits.");
        }

        var dirtyDocuments = workspace.Documents.GetDirtyDocuments();
        var changedFiles = dirtyDocuments.Select(document => document.Path).ToArray();
        if (request.DryRun)
        {
            return Success(request, changedFiles: changedFiles);
        }

        var backupFiles = new List<string>();
        foreach (var document in dirtyDocuments)
        {
            var writeResult = WriteTextAtomically(request.OperationId, document.Path, document.Text);
            backupFiles.AddRange(writeResult.BackupFiles);
            var persistResult = workspace.CommandBus.MarkDocumentPersisted(
                document.Path,
                document.InMemoryRevision,
                CreateContext(request));
            if (!persistResult.Succeeded)
            {
                return Failure(request, persistResult.Message, changedFiles: changedFiles, backupFiles: backupFiles);
            }
        }

        return Success(request, changedFiles: changedFiles, backupFiles: backupFiles);
    }

    private WorkspaceTransactionResult ApplyHeadlessCommit(WorkspaceTransactionRequest request)
    {
        var plan = ValidateTextEdits(request);
        var changedFiles = plan.Edits.Select(edit => edit.Path).ToArray();
        if (!plan.Succeeded)
        {
            return Failure(
                request,
                plan.Message,
                changedFiles: changedFiles,
                changedObjects: plan.ChangedObjects,
                createdObjects: plan.CreatedObjects);
        }

        if (request.DryRun)
        {
            return Success(
                request,
                changedFiles: changedFiles,
                changedObjects: plan.ChangedObjects,
                createdObjects: plan.CreatedObjects);
        }

        var backupFiles = new List<string>();
        foreach (var edit in plan.Edits)
        {
            var writeResult = WriteTextAtomically(request.OperationId, edit.Path, edit.Text);
            backupFiles.AddRange(writeResult.BackupFiles);
        }

        var context = CreateContext(request);
        foreach (var edit in plan.Edits)
        {
            var changeResult = workspace.CommandBus.ReplaceTextDocument(
                edit.Path,
                edit.Text,
                edit.ExpectedRevision,
                context);
            if (!changeResult.Succeeded)
            {
                return Failure(request, changeResult.Message, changedFiles: changedFiles, backupFiles: backupFiles);
            }

            var changedDocument = workspace.Documents.GetByPath(edit.Path);
            var persistResult = workspace.CommandBus.MarkDocumentPersisted(
                edit.Path,
                changedDocument.InMemoryRevision,
                context);
            if (!persistResult.Succeeded)
            {
                return Failure(request, persistResult.Message, changedFiles: changedFiles, backupFiles: backupFiles);
            }
        }

        return Success(
            request,
            changedFiles: changedFiles,
            changedObjects: plan.ChangedObjects,
            createdObjects: plan.CreatedObjects,
            backupFiles: backupFiles);
    }

    private WorkspaceTransactionResult ApplyExternalImport(WorkspaceTransactionRequest request)
    {
        if (request.Edits.Count != 1)
        {
            return Failure(request, "ExternalImport transaction requires exactly one changed document.");
        }

        var edit = NormalizeEdit(request.Edits[0]);
        var document = workspace.Documents.GetByPath(edit.Path);
        if (document.InMemoryRevision != edit.ExpectedRevision)
        {
            return Failure(
                request,
                $"Document expected revision '{edit.ExpectedRevision.Value}' does not match current revision '{document.InMemoryRevision.Value}'.");
        }

        var baseSnapshot = ProjectDocumentParser.ParseText(
            document.Path,
            document.PersistedText,
            new ProjectDocumentRevisionState(document.PersistedRevision, document.PersistedRevision));
        var incomingSnapshot = ProjectDocumentParser.ParseText(
            document.Path,
            edit.Text,
            new ProjectDocumentRevisionState(document.PersistedRevision, document.PersistedRevision));
        var currentChanges = ProjectDocumentStructuralDiff.Compare(baseSnapshot, document.Snapshot).Changes;
        var incomingChanges = ProjectDocumentStructuralDiff.Compare(baseSnapshot, incomingSnapshot).Changes;
        var conflicts = DetectConflicts(document.Path, currentChanges, incomingChanges);
        var changedObjects = DescribeChanges(document.Path, incomingChanges);
        var createdObjects = DescribeCreatedObjects(document.Path, incomingChanges);
        if (conflicts.Count > 0)
        {
            return Failure(
                request,
                "External import conflicts with current dirty workspace state.",
                changedObjects: changedObjects,
                createdObjects: createdObjects,
                conflicts: conflicts);
        }

        if (request.DryRun)
        {
            return Success(
                request,
                changedObjects: changedObjects,
                createdObjects: createdObjects);
        }

        var beforeStates = CaptureUndoStates([document.Path]);
        var mergedText = MergeIncomingChanges(document.Text, document.Path, incomingChanges);
        var nextPersistedRevision = document.PersistedRevision.Next();
        var nextInMemoryRevision = document.IsDirty
            ? new ProjectDocumentRevision(Math.Max(document.InMemoryRevision.Value, nextPersistedRevision.Value) + 1)
            : nextPersistedRevision;
        var changed = workspace.Documents.ApplyTextDocumentState(
            document.Path,
            mergedText,
            nextPersistedRevision,
            nextInMemoryRevision,
            edit.Text);
        workspace.Revisions.RecordDocumentChanged(changed);
        workspace.OperationJournal.RecordCompleted(
            CreateContext(request),
            [changed.Path],
            DateTimeOffset.UtcNow);
        workspace.Events.Publish(new ProjectWorkspaceEvent(
            ProjectWorkspaceEventKind.DocumentChanged,
            workspace.Revisions.WorkspaceRevision,
            changed.DocumentId,
            changed.Path,
            request.OperationId,
            source: null,
            diagnostics: []));

        AddUndoGroup(request, beforeStates, CaptureUndoStates([changed.Path]));
        return Success(
            request,
            changedObjects: changedObjects,
            createdObjects: createdObjects);
    }

    private TransactionPlan ValidateTextEdits(WorkspaceTransactionRequest request)
    {
        if (request.Edits.Count == 0)
        {
            return TransactionPlan.Failure("Workspace transaction requires at least one document edit.");
        }

        var edits = new List<WorkspaceTransactionDocumentEdit>();
        var changedObjects = new List<string>();
        var createdObjects = new List<string>();
        foreach (var originalEdit in request.Edits)
        {
            var edit = NormalizeEdit(originalEdit);
            ProjectDocumentParser.ParseText(
                edit.Path,
                edit.Text,
                new ProjectDocumentRevisionState(edit.ExpectedRevision, edit.ExpectedRevision));

            var document = workspace.Documents.GetByPath(edit.Path);
            if (document.InMemoryRevision != edit.ExpectedRevision)
            {
                return TransactionPlan.Failure(
                    $"Document expected revision '{edit.ExpectedRevision.Value}' does not match current revision '{document.InMemoryRevision.Value}'.");
            }

            var after = ProjectDocumentParser.ParseText(
                edit.Path,
                edit.Text,
                new ProjectDocumentRevisionState(document.PersistedRevision, document.InMemoryRevision.Next()));
            var diff = ProjectDocumentStructuralDiff.Compare(document.Snapshot, after);
            changedObjects.AddRange(DescribeChanges(edit.Path, diff.Changes));
            createdObjects.AddRange(DescribeCreatedObjects(edit.Path, diff.Changes));
            edits.Add(edit);
        }

        return TransactionPlan.Success(edits, changedObjects, createdObjects);
    }

    private WorkspaceTransactionDocumentEdit NormalizeEdit(WorkspaceTransactionDocumentEdit edit)
    {
        var normalizedPath = NormalizeEditablePath(edit.Path);
        return WorkspaceTransactionDocumentEdit.ReplaceText(normalizedPath, edit.ExpectedRevision, edit.Text);
    }

    private string NormalizeEditablePath(string path)
    {
        var normalizedPath = ProjectDocumentPaths.NormalizeRelativePath(path);
        if (IsProtectedPath(normalizedPath))
        {
            throw new InvalidOperationException($"Workspace transaction cannot edit generated or cache path '{normalizedPath}'.");
        }

        return normalizedPath;
    }

    private WorkspaceAtomicWriteResult WriteTextAtomically(string operationId, string relativePath, string text)
    {
        var normalizedPath = NormalizeEditablePath(relativePath);
        var projectRoot = Path.GetFullPath(workspace.ProjectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var targetPath = Path.Combine(projectRoot, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
        EnsureChildPath(projectRoot, targetPath);

        var targetDirectory = Path.GetDirectoryName(targetPath) ??
            throw new InvalidOperationException("Workspace transaction target path has no parent directory.");
        Directory.CreateDirectory(targetDirectory);

        var tempPath = Path.Combine(
            targetDirectory,
            $".{Path.GetFileName(targetPath)}.{Guid.NewGuid():N}.tmp");
        var backupFiles = new List<string>();
        try
        {
            File.WriteAllText(tempPath, text.ReplaceLineEndings("\n"), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (File.Exists(targetPath))
            {
                var backupRelativePath = BuildBackupRelativePath(operationId, normalizedPath);
                var backupPath = Path.Combine(projectRoot, backupRelativePath.Replace('/', Path.DirectorySeparatorChar));
                EnsureChildPath(projectRoot, backupPath);
                Directory.CreateDirectory(Path.GetDirectoryName(backupPath)!);
                File.Replace(tempPath, targetPath, backupPath, ignoreMetadataErrors: true);
                backupFiles.Add(backupRelativePath);
            }
            else
            {
                File.Move(tempPath, targetPath);
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }

        return new WorkspaceAtomicWriteResult(backupFiles);
    }

    private static void EnsureChildPath(string rootPath, string candidatePath)
    {
        var root = Path.GetFullPath(rootPath);
        var candidate = Path.GetFullPath(candidatePath);
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == "." ||
            relative.StartsWith("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"Workspace transaction path escapes project root: {candidate}");
        }
    }

    private static bool IsProtectedPath(string normalizedPath)
    {
        return normalizedPath.StartsWith(".electron2d/import-cache/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/workspaces/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/context/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith(".electron2d/session/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("bin/", StringComparison.Ordinal) ||
            normalizedPath.StartsWith("obj/", StringComparison.Ordinal) ||
            normalizedPath.Contains("/generated/", StringComparison.Ordinal);
    }

    private static string BuildBackupRelativePath(string operationId, string normalizedPath)
    {
        var safeOperationId = new string(operationId.Select(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.'
                ? character
                : '_').ToArray());
        return $".electron2d/backups/{safeOperationId}/{normalizedPath}.bak";
    }

    private static IReadOnlyList<string> DescribeChanges(
        string documentPath,
        IEnumerable<ProjectDocumentChange> changes)
    {
        return changes
            .Where(change => change.Kind != ProjectDocumentChangeKind.Added)
            .Select(change => DescribeChange(documentPath, change))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<string> DescribeCreatedObjects(
        string documentPath,
        IEnumerable<ProjectDocumentChange> changes)
    {
        return changes
            .Where(change => change.Kind == ProjectDocumentChangeKind.Added)
            .Select(change => DescribeChange(documentPath, change))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
    }

    private static string DescribeChange(string documentPath, ProjectDocumentChange change)
    {
        var suffix = change.PropertyPath is null
            ? change.Kind.ToString()
            : $"{change.Kind}.{change.PropertyPath}";
        return $"{documentPath}#{change.ObjectUid.Value}.{suffix}";
    }

    private static IReadOnlyList<WorkspaceTransactionConflict> DetectConflicts(
        string documentPath,
        IReadOnlyList<ProjectDocumentChange> currentChanges,
        IReadOnlyList<ProjectDocumentChange> incomingChanges)
    {
        var conflicts = new List<WorkspaceTransactionConflict>();
        foreach (var incoming in incomingChanges)
        {
            foreach (var current in currentChanges)
            {
                if (incoming.Kind == ProjectDocumentChangeKind.PropertyChanged &&
                    current.Kind == ProjectDocumentChangeKind.PropertyChanged &&
                    incoming.ObjectUid == current.ObjectUid &&
                    string.Equals(incoming.PropertyPath, current.PropertyPath, StringComparison.Ordinal) &&
                    !string.Equals(incoming.NewValue, current.NewValue, StringComparison.Ordinal))
                {
                    conflicts.Add(new WorkspaceTransactionConflict(
                        WorkspaceTransactionConflictKind.PropertyConflict,
                        documentPath,
                        incoming.ObjectUid.Value,
                        incoming.PropertyPath,
                        "Incoming external change edits a property that is already dirty in the workspace."));
                }

                if (incoming.Kind == ProjectDocumentChangeKind.Deleted &&
                    incoming.ObjectUid == current.ObjectUid &&
                    current.Kind != ProjectDocumentChangeKind.Deleted)
                {
                    conflicts.Add(new WorkspaceTransactionConflict(
                        WorkspaceTransactionConflictKind.DeletedChangedObject,
                        documentPath,
                        incoming.ObjectUid.Value,
                        current.PropertyPath,
                        "Incoming external change deletes an object that is already dirty in the workspace."));
                }

                if (current.Kind == ProjectDocumentChangeKind.Deleted &&
                    incoming.ObjectUid == current.ObjectUid &&
                    incoming.Kind != ProjectDocumentChangeKind.Deleted)
                {
                    conflicts.Add(new WorkspaceTransactionConflict(
                        WorkspaceTransactionConflictKind.DeletedChangedObject,
                        documentPath,
                        incoming.ObjectUid.Value,
                        incoming.PropertyPath,
                        "Incoming external change edits an object deleted in the workspace."));
                }
            }

            if (incoming.Kind is not ProjectDocumentChangeKind.PropertyChanged and not ProjectDocumentChangeKind.Deleted)
            {
                conflicts.Add(new WorkspaceTransactionConflict(
                    WorkspaceTransactionConflictKind.UnsupportedChange,
                    documentPath,
                    incoming.ObjectUid.Value,
                    incoming.PropertyPath,
                    "Incoming external change is not supported by the first transaction merge core."));
            }
        }

        return conflicts
            .GroupBy(conflict => (conflict.Kind, conflict.DocumentPath, conflict.ObjectUid, conflict.PropertyPath), conflict => conflict)
            .Select(group => group.First())
            .ToArray();
    }

    private static string MergeIncomingChanges(
        string currentText,
        string documentPath,
        IReadOnlyList<ProjectDocumentChange> incomingChanges)
    {
        var root = JsonNode.Parse(currentText) as JsonObject ??
            throw new FormatException("Workspace transaction merge requires a JSON object root.");

        foreach (var incoming in incomingChanges.Where(change => change.Kind == ProjectDocumentChangeKind.PropertyChanged))
        {
            if (!ApplyPropertyChange(root, incoming))
            {
                throw new InvalidOperationException(
                    $"Workspace transaction cannot safely merge property '{incoming.PropertyPath}' in '{documentPath}'.");
            }
        }

        return root.ToJsonString(IndentedJsonOptions).ReplaceLineEndings("\n");
    }

    private static bool ApplyPropertyChange(JsonObject root, ProjectDocumentChange change)
    {
        if (change.PropertyPath is null || change.NewValue is null)
        {
            return false;
        }

        var value = JsonNode.Parse(change.NewValue);
        if (change.ObjectUid.Value.StartsWith("scene-node:", StringComparison.Ordinal))
        {
            var idText = change.ObjectUid.Value["scene-node:".Length..];
            if (!int.TryParse(idText, NumberStyles.None, CultureInfo.InvariantCulture, out var nodeId))
            {
                return false;
            }

            var nodes = root["nodes"] as JsonArray;
            if (nodes is null)
            {
                return false;
            }

            foreach (var node in nodes.OfType<JsonObject>())
            {
                if (node.TryGetPropertyValue("id", out var idNode) &&
                    idNode is JsonValue idValue &&
                    idValue.TryGetValue<int>(out var id) &&
                    id == nodeId)
                {
                    var properties = node["properties"] as JsonObject;
                    if (properties is null)
                    {
                        return false;
                    }

                    properties[change.PropertyPath] = value;
                    return true;
                }
            }

            return false;
        }

        if (change.ObjectUid.Value is "json:root" or "settings:root")
        {
            root[change.PropertyPath] = value;
            return true;
        }

        if (change.ObjectUid.Value is "resource:main")
        {
            if (root["properties"] is not JsonObject properties)
            {
                return false;
            }

            properties[change.PropertyPath] = value;
            return true;
        }

        return false;
    }

    private IReadOnlyList<ProjectWorkspaceUndoDocumentState> CaptureUndoStates(IEnumerable<string> paths)
    {
        return paths
            .Select(ProjectDocumentPaths.NormalizeRelativePath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => workspace.Documents.TryGetByPath(path, out var document)
                ? ProjectWorkspaceUndoDocumentState.FromDocument(document)
                : ProjectWorkspaceUndoDocumentState.Missing(path))
            .ToArray();
    }

    private void AddUndoGroup(
        WorkspaceTransactionRequest request,
        IReadOnlyList<ProjectWorkspaceUndoDocumentState> beforeStates,
        IReadOnlyList<ProjectWorkspaceUndoDocumentState> afterStates)
    {
        if (!string.IsNullOrWhiteSpace(request.UndoGroupId))
        {
            workspace.UndoRedo.AddUndoGroup(
                request.UndoGroupId,
                request.OperationId,
                request.ActorKind,
                request.OperationKind,
                beforeStates,
                afterStates);
        }
    }

    private ProjectWorkspaceOperationContext CreateContext(WorkspaceTransactionRequest request)
    {
        return new ProjectWorkspaceOperationContext(
            request.OperationId,
            request.ActorKind,
            request.OperationKind);
    }

    private WorkspaceTransactionResult Success(
        WorkspaceTransactionRequest request,
        IReadOnlyList<string>? changedFiles = null,
        IReadOnlyList<string>? changedObjects = null,
        IReadOnlyList<string>? createdObjects = null,
        IReadOnlyList<string>? backupFiles = null)
    {
        return BuildResult(
            request,
            succeeded: true,
            diagnostics: [],
            conflicts: [],
            changedFiles: changedFiles ?? [],
            changedObjects: changedObjects ?? [],
            createdObjects: createdObjects ?? [],
            backupFiles: backupFiles ?? []);
    }

    private WorkspaceTransactionResult Failure(
        WorkspaceTransactionRequest request,
        string message,
        IReadOnlyList<string>? changedFiles = null,
        IReadOnlyList<string>? changedObjects = null,
        IReadOnlyList<string>? createdObjects = null,
        IReadOnlyList<WorkspaceTransactionConflict>? conflicts = null,
        IReadOnlyList<string>? backupFiles = null)
    {
        return BuildResult(
            request,
            succeeded: false,
            diagnostics: [CreateTransactionDiagnostic(message)],
            conflicts: conflicts ?? [],
            changedFiles: changedFiles ?? [],
            changedObjects: changedObjects ?? [],
            createdObjects: createdObjects ?? [],
            backupFiles: backupFiles ?? []);
    }

    private WorkspaceTransactionResult BuildResult(
        WorkspaceTransactionRequest request,
        bool succeeded,
        IReadOnlyList<StructuredDiagnostic> diagnostics,
        IReadOnlyList<WorkspaceTransactionConflict> conflicts,
        IReadOnlyList<string> changedFiles,
        IReadOnlyList<string> changedObjects,
        IReadOnlyList<string> createdObjects,
        IReadOnlyList<string> backupFiles)
    {
        return new WorkspaceTransactionResult(
            succeeded,
            request.Mode,
            request.DryRun,
            request.OperationId,
            request.UndoGroupId,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            GetPersistedRevisions(),
            workspace.Revisions.DirtyDocuments,
            workspace.Revisions.PersistenceState,
            changedFiles,
            changedObjects,
            createdObjects,
            conflicts,
            diagnostics,
            backupFiles);
    }

    private IReadOnlyDictionary<string, ProjectDocumentRevision> GetPersistedRevisions()
    {
        return new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            workspace.Documents.Documents
                .OrderBy(document => document.Path, StringComparer.Ordinal)
                .ToDictionary(document => document.Path, document => document.PersistedRevision, StringComparer.Ordinal));
    }

    private static StructuredDiagnostic CreateTransactionDiagnostic(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return StructuredDiagnostic.Create(
            "E2D-TOOLING-0002",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Tooling,
            message,
            location: null,
            relatedLocations: [],
            suggestedFixes: []);
    }

    private sealed class TransactionPlan
    {
        private TransactionPlan(
            bool succeeded,
            string message,
            IReadOnlyList<WorkspaceTransactionDocumentEdit> edits,
            IReadOnlyList<string> changedObjects,
            IReadOnlyList<string> createdObjects)
        {
            Succeeded = succeeded;
            Message = message;
            Edits = edits;
            ChangedObjects = changedObjects;
            CreatedObjects = createdObjects;
        }

        public bool Succeeded { get; }

        public string Message { get; }

        public IReadOnlyList<WorkspaceTransactionDocumentEdit> Edits { get; }

        public IReadOnlyList<string> ChangedObjects { get; }

        public IReadOnlyList<string> CreatedObjects { get; }

        public static TransactionPlan Success(
            IReadOnlyList<WorkspaceTransactionDocumentEdit> edits,
            IReadOnlyList<string> changedObjects,
            IReadOnlyList<string> createdObjects)
        {
            return new TransactionPlan(true, string.Empty, edits, changedObjects, createdObjects);
        }

        public static TransactionPlan Failure(string message)
        {
            return new TransactionPlan(false, message, [], [], []);
        }
    }

    private sealed class WorkspaceAtomicWriteResult
    {
        public WorkspaceAtomicWriteResult(IEnumerable<string> backupFiles)
        {
            BackupFiles = backupFiles.ToArray();
        }

        public IReadOnlyList<string> BackupFiles { get; }
    }
}
