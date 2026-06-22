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

public sealed class WorkspaceSnapshotTests
{
    [Fact]
    public void SnapshotCapturesImmutableRevisionsDirtyDocumentsAndOpenCodeBuffers()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("snapshot-identity"), "test-runner");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest("open-scene"));
        workspace.CommandBus.OpenTextDocument(
            "Scripts/Player.cs",
            CodeText(speed: 120),
            persistedRevision: 3,
            ProjectWorkspaceOperationContext.ForTest("open-code"));

        workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            new ProjectDocumentRevision(1),
            ProjectWorkspaceOperationContext.ForTest("change-scene"));
        workspace.CommandBus.ReplaceTextDocument(
            "Scripts/Player.cs",
            CodeText(speed: 240),
            new ProjectDocumentRevision(3),
            ProjectWorkspaceOperationContext.ForTest("change-code"));

        var createdAt = new DateTimeOffset(2026, 6, 22, 16, 0, 0, TimeSpan.Zero);
        var snapshot = WorkspaceSnapshot.Create(workspace, new WorkspaceSnapshotId("snap-identity"), createdAt);

        Assert.Equal("snap-identity", snapshot.SnapshotId.Value);
        Assert.Equal(workspace.Revisions.WorkspaceRevision, snapshot.WorkspaceRevision);
        Assert.Equal(workspace.Revisions.ContentRevision, snapshot.ContentRevision);
        Assert.Equal(createdAt, snapshot.CreatedAt);
        Assert.Equal(new ProjectDocumentRevision(2), snapshot.DocumentRevisions["scenes/main.scene.json"]);
        Assert.Equal(new ProjectDocumentRevision(4), snapshot.DocumentRevisions["Scripts/Player.cs"]);
        Assert.Equal(["Scripts/Player.cs", "scenes/main.scene.json"], snapshot.DirtyDocuments);

        var codeBuffer = Assert.Single(snapshot.OpenCodeBuffers);
        Assert.Equal("Scripts/Player.cs", codeBuffer.Path);
        Assert.Equal(new ProjectDocumentRevision(4), codeBuffer.Revision);
        Assert.Contains("240", codeBuffer.Text, StringComparison.Ordinal);

        workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 14),
            new ProjectDocumentRevision(2),
            ProjectWorkspaceOperationContext.ForTest("change-scene-again"));

        Assert.Contains("\"value\": 12", snapshot.Documents["scenes/main.scene.json"].Text, StringComparison.Ordinal);
        Assert.Equal(new ProjectDocumentRevision(2), snapshot.DocumentRevisions["scenes/main.scene.json"]);
    }

    [Fact]
    public void MaterializationWritesOnlyWorkspaceDirectoryAndKeepsSourceDirtyState()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("snapshot-materialization"), "test-runner");
        var sourceScenePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");
        Directory.CreateDirectory(Path.GetDirectoryName(sourceScenePath)!);
        File.WriteAllText(sourceScenePath, SceneText(speed: 10).ReplaceLineEndings("\n"));

        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest("open-scene"));
        workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            new ProjectDocumentRevision(1),
            ProjectWorkspaceOperationContext.ForTest("change-scene"));

        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId("snap-materialized"),
            new DateTimeOffset(2026, 6, 22, 16, 5, 0, TimeSpan.Zero));
        var materialization = WorkspaceSnapshotMaterializer.Materialize(
            workspace.ProjectRoot,
            "session-001",
            snapshot);

        Assert.StartsWith(
            Path.Combine(workspace.ProjectRoot, ".electron2d", "workspaces"),
            materialization.RootPath,
            StringComparison.OrdinalIgnoreCase);
        Assert.True(File.Exists(Path.Combine(materialization.RootPath, "scenes", "main.scene.json")));
        Assert.True(File.Exists(Path.Combine(materialization.RootPath, "workspace-snapshot.json")));
        Assert.Contains("\"value\": 12", File.ReadAllText(Path.Combine(materialization.RootPath, "scenes", "main.scene.json")), StringComparison.Ordinal);
        Assert.Contains("snap-materialized", File.ReadAllText(Path.Combine(materialization.RootPath, "workspace-snapshot.json")), StringComparison.Ordinal);
        Assert.Contains("\"value\": 10", File.ReadAllText(sourceScenePath), StringComparison.Ordinal);
        Assert.Equal(new ProjectDocumentRevision(1), workspace.Documents.GetByPath("scenes/main.scene.json").PersistedRevision);
        Assert.True(workspace.Documents.GetByPath("scenes/main.scene.json").IsDirty);

        materialization.Cleanup();

        Assert.False(Directory.Exists(materialization.RootPath));
        Assert.True(File.Exists(sourceScenePath));
    }

    [Fact]
    public void JobInputStalenessIgnoresWorkspaceOnlyMetadataAndTracksContentInputs()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("snapshot-stale"), "test-runner");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest("open-scene"));

        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId("snap-stale"),
            new DateTimeOffset(2026, 6, 22, 16, 10, 0, TimeSpan.Zero));
        var input = WorkspaceJobInputIdentity.FromSnapshot(snapshot, "hash:debug");

        workspace.Diagnostics.SetDiagnostics(
            "editor-selection",
            [],
            ProjectWorkspaceOperationContext.ForTest("metadata-only"));

        Assert.False(WorkspaceSnapshotStalenessEvaluator.IsStale(input, workspace, "hash:debug"));
        Assert.True(WorkspaceSnapshotStalenessEvaluator.IsStale(input, workspace, "hash:release"));

        workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            new ProjectDocumentRevision(1),
            ProjectWorkspaceOperationContext.ForTest("change-scene"));

        Assert.True(WorkspaceSnapshotStalenessEvaluator.IsStale(input, workspace, "hash:debug"));
    }

    [Fact]
    public void ExportPolicyDefaultsToCleanPersistedStateAndRequiresExplicitDirtySnapshot()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("snapshot-export"), "test-runner");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest("open-scene"));

        var cleanPlan = WorkspaceExportSnapshotPolicy.PlanCleanPersistedState(workspace);

        Assert.True(cleanPlan.Succeeded);
        Assert.Equal(WorkspaceExportInputKind.CleanPersistedState, cleanPlan.InputKind);
        Assert.False(cleanPlan.RequiresSnapshot);

        workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            new ProjectDocumentRevision(1),
            ProjectWorkspaceOperationContext.ForTest("change-scene"));

        var rejected = WorkspaceExportSnapshotPolicy.PlanCleanPersistedState(workspace);

        Assert.False(rejected.Succeeded);
        Assert.Equal(WorkspaceExportInputKind.RejectedDirtyWorkspace, rejected.InputKind);
        Assert.Contains("dirty", rejected.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new ProjectDocumentRevision(1), workspace.Documents.GetByPath("scenes/main.scene.json").PersistedRevision);
        Assert.True(workspace.Documents.GetByPath("scenes/main.scene.json").IsDirty);

        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId("snap-export"),
            new DateTimeOffset(2026, 6, 22, 16, 15, 0, TimeSpan.Zero));
        var dirtyPlan = WorkspaceExportSnapshotPolicy.PlanDirtySnapshot(workspace, snapshot);

        Assert.True(dirtyPlan.Succeeded);
        Assert.True(dirtyPlan.RequiresSnapshot);
        Assert.Equal(WorkspaceExportInputKind.DirtySnapshot, dirtyPlan.InputKind);
        Assert.Equal("snap-export", dirtyPlan.InputSnapshotId);
        Assert.Equal(new ProjectDocumentRevision(1), workspace.Documents.GetByPath("scenes/main.scene.json").PersistedRevision);
        Assert.True(workspace.Documents.GetByPath("scenes/main.scene.json").IsDirty);
    }

    private static string CreateProjectRoot(string name)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D.Tests", "WorkspaceSnapshot", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static string SceneText(int speed)
    {
        return $$"""
            {
              "format": "Electron2D.SceneFile",
              "version": 1,
              "nodes": [
                {
                  "id": 1,
                  "parent": null,
                  "type": "Node2D",
                  "name": "Player",
                  "properties": {
                    "speed": {
                      "kind": "Int",
                      "value": {{speed}}
                    }
                  }
                }
              ],
              "external": [],
              "internal": []
            }
            """;
    }

    private static string CodeText(int speed)
    {
        return $$"""
            using Electron2D;

            public sealed class Player : CharacterBody2D
            {
                public const int Speed = {{speed}};
            }
            """;
    }
}
