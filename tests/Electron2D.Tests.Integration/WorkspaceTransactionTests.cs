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
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class WorkspaceTransactionTests
{
    [Fact]
    public void WorkspaceOnlyUsesExpectedRevisionAndCreatesGroupedUndoWithoutSaving()
    {
        using var workspace = CreateWorkspace("workspace-only", SceneText(speed: 10, health: 100));
        var sourcePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");

        var result = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-agent-speed",
            ProjectWorkspaceActorKind.Agent,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-agent-speed",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 12, health: 100))
            ]));

        Assert.True(result.Succeeded);
        Assert.Equal("op-agent-speed", result.OperationId);
        Assert.Equal("undo-agent-speed", result.UndoGroupId);
        Assert.Equal(new ProjectDocumentRevision(2), result.DocumentRevisions["scenes/main.scene.json"]);
        Assert.Equal(new ProjectDocumentRevision(1), result.PersistedRevisions["scenes/main.scene.json"]);
        Assert.Equal(["scenes/main.scene.json"], result.DirtyDocuments);
        Assert.Equal(ProjectWorkspacePersistenceState.Dirty, result.PersistenceState);
        Assert.Empty(result.ChangedFiles);
        Assert.Contains(result.ChangedObjects, changed => changed.Contains("scene-node:1", StringComparison.Ordinal));
        Assert.Contains("undo-agent-speed", workspace.UndoRedo.UndoGroups);
        Assert.Contains("\"value\": 10", File.ReadAllText(sourcePath), StringComparison.Ordinal);

        var staleResult = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-agent-stale",
            ProjectWorkspaceActorKind.Agent,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-agent-stale",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 14, health: 100))
            ]));

        Assert.False(staleResult.Succeeded);
        Assert.Contains(staleResult.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.Contains("\"value\": 12", workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.DoesNotContain("undo-agent-stale", workspace.UndoRedo.UndoGroups);
    }

    [Fact]
    public void SaveAffectedDocumentsSupportsDryRunAtomicReplaceAndBackups()
    {
        using var workspace = CreateWorkspace("save-affected", SceneText(speed: 10, health: 100));
        var sourcePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");
        workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-edit-before-save",
            ProjectWorkspaceActorKind.Agent,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-edit-before-save",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 16, health: 100))
            ]));

        var dryRun = workspace.Transactions.Apply(WorkspaceTransactionRequest.SaveAffectedDocuments(
            "op-save-dry-run",
            ProjectWorkspaceActorKind.Agent,
            dryRun: true));

        Assert.True(dryRun.Succeeded);
        Assert.True(dryRun.DryRun);
        Assert.Equal(["scenes/main.scene.json"], dryRun.ChangedFiles);
        Assert.Contains("\"value\": 10", File.ReadAllText(sourcePath), StringComparison.Ordinal);
        Assert.True(workspace.Documents.GetByPath("scenes/main.scene.json").IsDirty);

        var saved = workspace.Transactions.Apply(WorkspaceTransactionRequest.SaveAffectedDocuments(
            "op-save",
            ProjectWorkspaceActorKind.Agent,
            dryRun: false));

        Assert.True(saved.Succeeded);
        Assert.Equal(["scenes/main.scene.json"], saved.ChangedFiles);
        Assert.Single(saved.BackupFiles);
        Assert.True(File.Exists(Path.Combine(workspace.ProjectRoot, saved.BackupFiles[0])));
        Assert.Contains("\"value\": 16", File.ReadAllText(sourcePath), StringComparison.Ordinal);
        Assert.False(workspace.Documents.GetByPath("scenes/main.scene.json").IsDirty);
        Assert.Equal(new ProjectDocumentRevision(2), saved.PersistedRevisions["scenes/main.scene.json"]);
        Assert.Empty(Directory.GetFiles(Path.GetDirectoryName(sourcePath)!, "*.tmp", SearchOption.TopDirectoryOnly));
    }

    [Fact]
    public void HeadlessCommitDryRunDoesNotMutateAndValidationFailureRollsBack()
    {
        using var workspace = CreateWorkspace("headless", SceneText(speed: 10, health: 100));
        var sourcePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");

        var dryRun = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-headless-dry",
            ProjectWorkspaceActorKind.Cli,
            "scene.set-property",
            WorkspaceTransactionMode.HeadlessCommit,
            dryRun: true,
            undoGroupId: null,
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 20, health: 100))
            ]));

        Assert.True(dryRun.Succeeded);
        Assert.Equal(["scenes/main.scene.json"], dryRun.ChangedFiles);
        Assert.Contains("\"value\": 10", File.ReadAllText(sourcePath), StringComparison.Ordinal);
        Assert.Equal(new ProjectDocumentRevision(1), workspace.Documents.GetByPath("scenes/main.scene.json").InMemoryRevision);

        var committed = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-headless-commit",
            ProjectWorkspaceActorKind.Cli,
            "scene.set-property",
            WorkspaceTransactionMode.HeadlessCommit,
            dryRun: false,
            undoGroupId: null,
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 20, health: 100))
            ]));

        Assert.True(committed.Succeeded);
        Assert.Contains("\"value\": 20", File.ReadAllText(sourcePath), StringComparison.Ordinal);
        Assert.Equal(ProjectWorkspacePersistenceState.Clean, committed.PersistenceState);

        var failed = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-headless-invalid",
            ProjectWorkspaceActorKind.Cli,
            "scene.set-property",
            WorkspaceTransactionMode.HeadlessCommit,
            dryRun: false,
            undoGroupId: null,
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(2),
                    "{ invalid json")
            ]));

        Assert.False(failed.Succeeded);
        Assert.Contains(failed.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.Contains("\"value\": 20", File.ReadAllText(sourcePath), StringComparison.Ordinal);
        Assert.Equal(new ProjectDocumentRevision(2), workspace.Documents.GetByPath("scenes/main.scene.json").InMemoryRevision);
    }

    [Fact]
    public void ExternalImportMergesNonOverlappingChangesAndReportsConflicts()
    {
        using var workspace = CreateWorkspace("external-merge", SceneText(speed: 10, health: 100, includeUnknown: true));
        workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-human-speed",
            ProjectWorkspaceActorKind.Human,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-human-speed",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 12, health: 100, includeUnknown: true))
            ]));

        var imported = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-external-health",
            ProjectWorkspaceActorKind.ExternalFile,
            "external.import",
            WorkspaceTransactionMode.ExternalImport,
            dryRun: false,
            undoGroupId: "undo-external-health",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(2),
                    SceneText(speed: 10, health: 80, includeUnknown: true))
            ]));

        Assert.True(imported.Succeeded);
        Assert.Empty(imported.Conflicts);
        var mergedText = workspace.Documents.GetByPath("scenes/main.scene.json").Text;
        Assert.Contains("\"speed\"", mergedText, StringComparison.Ordinal);
        Assert.Contains("\"value\": 12", mergedText, StringComparison.Ordinal);
        Assert.Contains("\"health\"", mergedText, StringComparison.Ordinal);
        Assert.Contains("\"value\": 80", mergedText, StringComparison.Ordinal);
        Assert.Contains("\"customToolingData\"", mergedText, StringComparison.Ordinal);

        using var conflictWorkspace = CreateWorkspace("external-conflict", SceneText(speed: 10, health: 100));
        conflictWorkspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-human-conflict-speed",
            ProjectWorkspaceActorKind.Human,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-human-conflict-speed",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 12, health: 100))
            ]));

        var conflict = conflictWorkspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-external-conflict-speed",
            ProjectWorkspaceActorKind.ExternalFile,
            "external.import",
            WorkspaceTransactionMode.ExternalImport,
            dryRun: false,
            undoGroupId: "undo-external-conflict-speed",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(2),
                    SceneText(speed: 14, health: 100))
            ]));

        Assert.False(conflict.Succeeded);
        var propertyConflict = Assert.Single(conflict.Conflicts);
        Assert.Equal(WorkspaceTransactionConflictKind.PropertyConflict, propertyConflict.Kind);
        Assert.Equal("scene-node:1", propertyConflict.ObjectUid);
        Assert.Equal("speed", propertyConflict.PropertyPath);
        Assert.Contains("\"value\": 12", conflictWorkspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);

        var deleteConflict = conflictWorkspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-external-delete-conflict",
            ProjectWorkspaceActorKind.ExternalFile,
            "external.import",
            WorkspaceTransactionMode.ExternalImport,
            dryRun: false,
            undoGroupId: "undo-external-delete-conflict",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "scenes/main.scene.json",
                    new ProjectDocumentRevision(2),
                    SceneTextWithoutPlayer())
            ]));

        Assert.False(deleteConflict.Succeeded);
        Assert.Contains(deleteConflict.Conflicts, item => item.Kind == WorkspaceTransactionConflictKind.DeletedChangedObject);
    }

    [Fact]
    public void TransactionsRejectGeneratedCacheAndEscapingPaths()
    {
        using var workspace = CreateWorkspace("path-safety", SceneText(speed: 10, health: 100));

        var generated = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-generated",
            ProjectWorkspaceActorKind.Agent,
            "scene.set-property",
            WorkspaceTransactionMode.WorkspaceOnly,
            dryRun: false,
            undoGroupId: "undo-generated",
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    ".electron2d/import-cache/cache.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 12, health: 100))
            ]));

        Assert.False(generated.Succeeded);
        Assert.Contains(generated.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");

        var escaping = workspace.Transactions.Apply(new WorkspaceTransactionRequest(
            "op-escaping",
            ProjectWorkspaceActorKind.Agent,
            "scene.set-property",
            WorkspaceTransactionMode.HeadlessCommit,
            dryRun: false,
            undoGroupId: null,
            edits:
            [
                WorkspaceTransactionDocumentEdit.ReplaceText(
                    "../outside.scene.json",
                    new ProjectDocumentRevision(1),
                    SceneText(speed: 12, health: 100))
            ]));

        Assert.False(escaping.Succeeded);
        Assert.Contains(escaping.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.False(File.Exists(Path.Combine(workspace.ProjectRoot, "..", "outside.scene.json")));
    }

    private static ProjectWorkspace CreateWorkspace(string name, string text)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D.Tests", "WorkspaceTransactions", name, Guid.NewGuid().ToString("N"));
        var sourcePath = Path.Combine(root, "scenes", "main.scene.json");
        Directory.CreateDirectory(Path.GetDirectoryName(sourcePath)!);
        File.WriteAllText(sourcePath, text.ReplaceLineEndings("\n"));

        var workspace = ProjectWorkspace.CreateHeadless(root, "test-runner");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            text,
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest($"open-{name}"));
        return workspace;
    }

    private static string SceneText(int speed, int health, bool includeUnknown = false)
    {
        var unknown = includeUnknown
            ? """
              ,
              "customToolingData": {
                "keep": true
              }
              """
            : string.Empty;

        return $$"""
            {
              "format": "Electron2D.SceneFile",
              "version": 1,
              "external": [],
              "internal": [],
              "nodes": [
                {
                  "id": 1,
                  "type": "Node2D",
                  "name": "Player",
                  "parent": null,
                  "properties": {
                    "speed": {
                      "kind": "Int",
                      "value": {{speed}}
                    },
                    "health": {
                      "kind": "Int",
                      "value": {{health}}
                    }
                  }
                }
              ]{{unknown}}
            }
            """;
    }

    private static string SceneTextWithoutPlayer()
    {
        return """
            {
              "format": "Electron2D.SceneFile",
              "version": 1,
              "external": [],
              "internal": [],
              "nodes": []
            }
            """;
    }
}
