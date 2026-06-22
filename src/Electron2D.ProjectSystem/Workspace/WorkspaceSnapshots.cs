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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.ProjectSystem;

internal readonly record struct WorkspaceSnapshotId
{
    public WorkspaceSnapshotId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!IsSafeSegment(value))
        {
            throw new ArgumentException("Workspace snapshot id must be a safe path segment.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString()
    {
        return Value;
    }

    internal static bool IsSafeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "." or "..")
        {
            return false;
        }

        return value.All(character =>
            char.IsAsciiLetterOrDigit(character) ||
            character is '-' or '_' or '.');
    }
}

internal sealed class WorkspaceSnapshot
{
    private WorkspaceSnapshot(
        WorkspaceSnapshotId snapshotId,
        ProjectWorkspaceRevision workspaceRevision,
        ProjectWorkspaceRevision contentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> documentRevisions,
        IReadOnlyList<string> dirtyDocuments,
        IReadOnlyList<WorkspaceSnapshotCodeBuffer> openCodeBuffers,
        DateTimeOffset createdAt,
        IReadOnlyDictionary<string, WorkspaceSnapshotDocument> documents)
    {
        SnapshotId = snapshotId;
        WorkspaceRevision = workspaceRevision;
        ContentRevision = contentRevision;
        DocumentRevisions = CopyDictionary(documentRevisions);
        DirtyDocuments = dirtyDocuments.OrderBy(path => path, StringComparer.Ordinal).ToArray();
        OpenCodeBuffers = openCodeBuffers.OrderBy(buffer => buffer.Path, StringComparer.Ordinal).ToArray();
        CreatedAt = createdAt;
        Documents = CopyDictionary(documents);
    }

    public WorkspaceSnapshotId SnapshotId { get; }

    public ProjectWorkspaceRevision WorkspaceRevision { get; }

    public ProjectWorkspaceRevision ContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> DocumentRevisions { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }

    public IReadOnlyList<WorkspaceSnapshotCodeBuffer> OpenCodeBuffers { get; }

    public DateTimeOffset CreatedAt { get; }

    public IReadOnlyDictionary<string, WorkspaceSnapshotDocument> Documents { get; }

    public static WorkspaceSnapshot Create(ProjectWorkspace workspace, WorkspaceSnapshotId snapshotId, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var documents = workspace.Documents.Documents
            .Select(document => new WorkspaceSnapshotDocument(
                document.Path,
                document.Text,
                document.PersistedRevision,
                document.InMemoryRevision,
                document.Snapshot.Classification.Kind,
                document.Snapshot.Classification.ContentKind))
            .ToDictionary(document => document.Path, document => document, StringComparer.Ordinal);
        var openCodeBuffers = documents.Values
            .Where(document => document.ContentKind == ProjectDocumentContentKind.CSharp)
            .Select(document => new WorkspaceSnapshotCodeBuffer(document.Path, document.Text, document.InMemoryRevision))
            .ToArray();

        return new WorkspaceSnapshot(
            snapshotId,
            workspace.Revisions.WorkspaceRevision,
            workspace.Revisions.ContentRevision,
            workspace.Revisions.DocumentRevisions,
            workspace.Revisions.DirtyDocuments,
            openCodeBuffers,
            createdAt,
            documents);
    }

    private static IReadOnlyDictionary<string, T> CopyDictionary<T>(IReadOnlyDictionary<string, T> source)
    {
        return new ReadOnlyDictionary<string, T>(
            source.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }
}

internal sealed class WorkspaceSnapshotDocument
{
    public WorkspaceSnapshotDocument(
        string path,
        string text,
        ProjectDocumentRevision persistedRevision,
        ProjectDocumentRevision inMemoryRevision,
        ProjectDocumentKind documentKind,
        ProjectDocumentContentKind contentKind)
    {
        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        Text = text.ReplaceLineEndings("\n");
        PersistedRevision = persistedRevision;
        InMemoryRevision = inMemoryRevision;
        DocumentKind = documentKind;
        ContentKind = contentKind;
    }

    public string Path { get; }

    public string Text { get; }

    public ProjectDocumentRevision PersistedRevision { get; }

    public ProjectDocumentRevision InMemoryRevision { get; }

    public ProjectDocumentKind DocumentKind { get; }

    public ProjectDocumentContentKind ContentKind { get; }
}

internal sealed class WorkspaceSnapshotCodeBuffer
{
    public WorkspaceSnapshotCodeBuffer(string path, string text, ProjectDocumentRevision revision)
    {
        Path = ProjectDocumentPaths.NormalizeRelativePath(path);
        Text = text.ReplaceLineEndings("\n");
        Revision = revision;
    }

    public string Path { get; }

    public string Text { get; }

    public ProjectDocumentRevision Revision { get; }
}

internal static class WorkspaceSnapshotMaterializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public static WorkspaceSnapshotMaterialization Materialize(
        string projectRoot,
        string sessionId,
        WorkspaceSnapshot snapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectRoot);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!WorkspaceSnapshotId.IsSafeSegment(sessionId))
        {
            throw new ArgumentException("Workspace session id must be a safe path segment.", nameof(sessionId));
        }

        var normalizedRoot = Path.GetFullPath(projectRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var workspacesRoot = Path.Combine(normalizedRoot, ".electron2d", "workspaces");
        var materializedRoot = Path.Combine(workspacesRoot, sessionId, snapshot.SnapshotId.Value);
        EnsureChildPath(workspacesRoot, materializedRoot);

        if (Directory.Exists(materializedRoot))
        {
            Directory.Delete(materializedRoot, recursive: true);
        }

        Directory.CreateDirectory(materializedRoot);
        foreach (var document in snapshot.Documents.Values.OrderBy(document => document.Path, StringComparer.Ordinal))
        {
            var targetPath = Path.Combine(
                materializedRoot,
                document.Path.Replace('/', Path.DirectorySeparatorChar));
            EnsureChildPath(materializedRoot, targetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllText(targetPath, document.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        var manifestPath = Path.Combine(materializedRoot, "workspace-snapshot.json");
        File.WriteAllText(
            manifestPath,
            SerializeSnapshotManifest(snapshot),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new WorkspaceSnapshotMaterialization(workspacesRoot, materializedRoot, manifestPath);
    }

    internal static void EnsureChildPath(string rootPath, string candidatePath)
    {
        var root = Path.GetFullPath(rootPath);
        var candidate = Path.GetFullPath(candidatePath);
        var relative = Path.GetRelativePath(root, candidate);
        if (relative == "." ||
            relative.StartsWith("..", StringComparison.Ordinal) ||
            Path.IsPathRooted(relative))
        {
            throw new InvalidOperationException($"Path escapes workspace snapshot root: {candidate}");
        }
    }

    private static string SerializeSnapshotManifest(WorkspaceSnapshot snapshot)
    {
        var documentRevisions = new JsonObject();
        foreach (var (path, revision) in snapshot.DocumentRevisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            documentRevisions[path] = revision.Value;
        }

        var dirtyDocuments = new JsonArray();
        foreach (var path in snapshot.DirtyDocuments)
        {
            dirtyDocuments.Add(path);
        }

        var openCodeBuffers = new JsonArray();
        foreach (var buffer in snapshot.OpenCodeBuffers)
        {
            openCodeBuffers.Add((JsonNode)new JsonObject
            {
                ["path"] = buffer.Path,
                ["revision"] = buffer.Revision.Value
            });
        }

        var root = new JsonObject
        {
            ["format"] = "Electron2D.WorkspaceSnapshot",
            ["version"] = 1,
            ["snapshotId"] = snapshot.SnapshotId.Value,
            ["workspaceRevision"] = snapshot.WorkspaceRevision.Value,
            ["contentRevision"] = snapshot.ContentRevision.Value,
            ["createdAt"] = snapshot.CreatedAt.ToString("O"),
            ["documentRevisions"] = documentRevisions,
            ["dirtyDocuments"] = dirtyDocuments,
            ["openCodeBuffers"] = openCodeBuffers
        };

        return root.ToJsonString(JsonOptions).ReplaceLineEndings("\n") + "\n";
    }
}

internal sealed class WorkspaceSnapshotMaterialization
{
    public WorkspaceSnapshotMaterialization(string workspacesRoot, string rootPath, string manifestPath)
    {
        WorkspacesRoot = Path.GetFullPath(workspacesRoot);
        RootPath = Path.GetFullPath(rootPath);
        ManifestPath = Path.GetFullPath(manifestPath);
    }

    public string WorkspacesRoot { get; }

    public string RootPath { get; }

    public string ManifestPath { get; }

    public void Cleanup()
    {
        WorkspaceSnapshotMaterializer.EnsureChildPath(WorkspacesRoot, RootPath);
        if (Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, recursive: true);
        }
    }
}

internal sealed class WorkspaceJobInputIdentity
{
    public WorkspaceJobInputIdentity(
        string inputSnapshotId,
        ProjectWorkspaceRevision inputWorkspaceRevision,
        ProjectWorkspaceRevision inputContentRevision,
        IReadOnlyDictionary<string, ProjectDocumentRevision> inputDocumentRevisions,
        string inputBuildConfigurationHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputSnapshotId);
        ArgumentNullException.ThrowIfNull(inputDocumentRevisions);
        ArgumentException.ThrowIfNullOrWhiteSpace(inputBuildConfigurationHash);

        InputSnapshotId = inputSnapshotId;
        InputWorkspaceRevision = inputWorkspaceRevision;
        InputContentRevision = inputContentRevision;
        InputDocumentRevisions = new ReadOnlyDictionary<string, ProjectDocumentRevision>(
            inputDocumentRevisions.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        InputBuildConfigurationHash = inputBuildConfigurationHash;
    }

    public string InputSnapshotId { get; }

    public ProjectWorkspaceRevision InputWorkspaceRevision { get; }

    public ProjectWorkspaceRevision InputContentRevision { get; }

    public IReadOnlyDictionary<string, ProjectDocumentRevision> InputDocumentRevisions { get; }

    public string InputBuildConfigurationHash { get; }

    public static WorkspaceJobInputIdentity FromSnapshot(WorkspaceSnapshot snapshot, string inputBuildConfigurationHash)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return new WorkspaceJobInputIdentity(
            snapshot.SnapshotId.Value,
            snapshot.WorkspaceRevision,
            snapshot.ContentRevision,
            snapshot.DocumentRevisions,
            inputBuildConfigurationHash);
    }
}

internal sealed class WorkspaceJobArtifact
{
    public WorkspaceJobArtifact(string artifactKind, WorkspaceJobInputIdentity inputIdentity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactKind);
        ArgumentNullException.ThrowIfNull(inputIdentity);

        ArtifactKind = artifactKind;
        InputIdentity = inputIdentity;
    }

    public string ArtifactKind { get; }

    public WorkspaceJobInputIdentity InputIdentity { get; }

    public bool Stale { get; private set; }

    internal void SetStale(bool stale)
    {
        Stale = stale;
    }
}

internal static class WorkspaceSnapshotStalenessEvaluator
{
    public static bool IsStale(
        WorkspaceJobInputIdentity inputIdentity,
        ProjectWorkspace workspace,
        string currentBuildConfigurationHash)
    {
        ArgumentNullException.ThrowIfNull(inputIdentity);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentBuildConfigurationHash);

        if (!string.Equals(inputIdentity.InputBuildConfigurationHash, currentBuildConfigurationHash, StringComparison.Ordinal))
        {
            return true;
        }

        if (inputIdentity.InputContentRevision != workspace.Revisions.ContentRevision)
        {
            return true;
        }

        var currentDocumentRevisions = workspace.Revisions.DocumentRevisions;
        foreach (var (path, inputRevision) in inputIdentity.InputDocumentRevisions)
        {
            if (!currentDocumentRevisions.TryGetValue(path, out var currentRevision) ||
                currentRevision != inputRevision)
            {
                return true;
            }
        }

        return false;
    }
}

internal enum WorkspaceExportInputKind
{
    CleanPersistedState,
    DirtySnapshot,
    RejectedDirtyWorkspace
}

internal sealed class WorkspaceExportPlan
{
    public WorkspaceExportPlan(
        bool succeeded,
        WorkspaceExportInputKind inputKind,
        bool requiresSnapshot,
        string message,
        string? inputSnapshotId,
        IReadOnlyList<string> dirtyDocuments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentNullException.ThrowIfNull(dirtyDocuments);

        Succeeded = succeeded;
        InputKind = inputKind;
        RequiresSnapshot = requiresSnapshot;
        Message = message;
        InputSnapshotId = inputSnapshotId;
        DirtyDocuments = dirtyDocuments.OrderBy(path => path, StringComparer.Ordinal).ToArray();
    }

    public bool Succeeded { get; }

    public WorkspaceExportInputKind InputKind { get; }

    public bool RequiresSnapshot { get; }

    public string Message { get; }

    public string? InputSnapshotId { get; }

    public IReadOnlyList<string> DirtyDocuments { get; }
}

internal static class WorkspaceExportSnapshotPolicy
{
    public static WorkspaceExportPlan PlanCleanPersistedState(ProjectWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var dirtyDocuments = workspace.Revisions.DirtyDocuments;
        if (dirtyDocuments.Count > 0)
        {
            return new WorkspaceExportPlan(
                succeeded: false,
                WorkspaceExportInputKind.RejectedDirtyWorkspace,
                requiresSnapshot: false,
                "Export uses clean persisted state by default. The workspace is dirty; request an explicit dirty snapshot export.",
                inputSnapshotId: null,
                dirtyDocuments);
        }

        return new WorkspaceExportPlan(
            succeeded: true,
            WorkspaceExportInputKind.CleanPersistedState,
            requiresSnapshot: false,
            "Export will use clean persisted project state.",
            inputSnapshotId: null,
            dirtyDocuments);
    }

    public static WorkspaceExportPlan PlanDirtySnapshot(ProjectWorkspace workspace, WorkspaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(snapshot);
        return new WorkspaceExportPlan(
            succeeded: true,
            WorkspaceExportInputKind.DirtySnapshot,
            requiresSnapshot: true,
            "Export will use an explicit dirty WorkspaceSnapshot without saving project source files.",
            snapshot.SnapshotId.Value,
            workspace.Revisions.DirtyDocuments);
    }
}
