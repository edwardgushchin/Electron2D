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

public sealed class ExternalChangeSynchronizerTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 6, 22, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void WatcherIsRecursiveAndDebounceCoalescesStableTextChangesWithin250Milliseconds()
    {
        using var workspace = CreateWorkspace("debounce", ("scenes/main.scene.json", SceneText(speed: 10, health: 100)));
        var synchronizer = new ExternalChangeSynchronizer(workspace, new ExternalChangeSynchronizerOptions(
            debounceDelay: TimeSpan.FromMilliseconds(200),
            selfWriteSuppressionWindow: TimeSpan.FromSeconds(2)));
        using var watcher = synchronizer.CreateWatcher();
        var scenePath = Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json");

        Assert.True(watcher.IncludeSubdirectories);
        Assert.False(watcher.IsRunning);

        File.WriteAllText(scenePath, SceneText(speed: 12, health: 100));
        synchronizer.Notify(ExternalFileChangeEvent.Changed("scenes/main.scene.json", FixedInstant));
        synchronizer.Notify(ExternalFileChangeEvent.Changed("scenes/main.scene.json", FixedInstant.AddMilliseconds(50)));

        var early = synchronizer.Drain(FixedInstant.AddMilliseconds(249));
        Assert.Empty(early.ProcessedPaths);
        Assert.Empty(early.ChangedFiles);

        var applied = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Equal(["scenes/main.scene.json"], applied.ProcessedPaths);
        Assert.Equal(["scenes/main.scene.json"], applied.ChangedFiles);
        Assert.True(applied.MaxAppliedDelay <= TimeSpan.FromMilliseconds(250));
        Assert.False(applied.FullProjectRescan);
        Assert.Contains("\"value\": 12", workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.Contains(applied.ChangedObjects, changed => changed.Contains("scene-node:1", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateMoveDeleteIgnoreRulesAndSelfWriteSuppressionUpdateWorkspaceWithoutFullRescan()
    {
        using var workspace = CreateWorkspace("lifecycle", ("assets/player.e2res", ResourceText("res://assets/player.e2res")));
        var synchronizer = CreateSynchronizer(workspace);
        var originalDocumentId = workspace.Documents.GetByPath("assets/player.e2res").DocumentId;
        Directory.CreateDirectory(Path.Combine(workspace.ProjectRoot, "scripts"));

        File.WriteAllText(Path.Combine(workspace.ProjectRoot, "scripts", "Player.cs"), "public sealed class Player {}\n");
        synchronizer.Notify(ExternalFileChangeEvent.Created("scripts/Player.cs", FixedInstant));
        var created = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Equal(["scripts/Player.cs"], created.ProcessedPaths);
        Assert.Equal(ProjectDocumentKind.Code, workspace.Documents.GetByPath("scripts/Player.cs").Snapshot.Classification.Kind);

        Directory.CreateDirectory(Path.Combine(workspace.ProjectRoot, "assets", "characters"));
        File.Move(
            Path.Combine(workspace.ProjectRoot, "assets", "player.e2res"),
            Path.Combine(workspace.ProjectRoot, "assets", "characters", "player.e2res"));
        File.WriteAllText(
            Path.Combine(workspace.ProjectRoot, "assets", "characters", "player.e2res"),
            ResourceText("res://assets/characters/player.e2res"));
        synchronizer.Notify(ExternalFileChangeEvent.Renamed(
            "assets/player.e2res",
            "assets/characters/player.e2res",
            FixedInstant.AddMilliseconds(300)));
        var moved = synchronizer.Drain(FixedInstant.AddMilliseconds(550));

        var movedPath = Assert.Single(moved.MovedPaths);
        Assert.Equal("assets/player.e2res", movedPath.OldPath);
        Assert.Equal("assets/characters/player.e2res", movedPath.NewPath);
        Assert.Equal(originalDocumentId, workspace.Documents.GetByPath("assets/characters/player.e2res").DocumentId);
        Assert.False(workspace.Documents.TryGetByPath("assets/player.e2res", out _));

        File.Delete(Path.Combine(workspace.ProjectRoot, "scripts", "Player.cs"));
        synchronizer.Notify(ExternalFileChangeEvent.Deleted("scripts/Player.cs", FixedInstant.AddMilliseconds(600)));
        var deleted = synchronizer.Drain(FixedInstant.AddMilliseconds(850));

        Assert.Equal(["scripts/Player.cs"], deleted.DeletedPaths);
        Assert.False(workspace.Documents.TryGetByPath("scripts/Player.cs", out _));

        Directory.CreateDirectory(Path.Combine(workspace.ProjectRoot, ".git"));
        Directory.CreateDirectory(Path.Combine(workspace.ProjectRoot, "obj"));
        File.WriteAllText(Path.Combine(workspace.ProjectRoot, ".git", "index"), "ignored");
        File.WriteAllText(Path.Combine(workspace.ProjectRoot, "obj", "generated.cs"), "ignored");
        synchronizer.Notify(ExternalFileChangeEvent.Changed(".git/index", FixedInstant.AddMilliseconds(900)));
        synchronizer.Notify(ExternalFileChangeEvent.Changed("obj/generated.cs", FixedInstant.AddMilliseconds(900)));
        using (synchronizer.SuppressOwnWrite("assets/self-write.e2res", FixedInstant.AddMilliseconds(900)))
        {
            File.WriteAllText(Path.Combine(workspace.ProjectRoot, "assets", "self-write.e2res"), ResourceText("res://assets/self-write.e2res"));
            synchronizer.Notify(ExternalFileChangeEvent.Changed("assets/self-write.e2res", FixedInstant.AddMilliseconds(950)));
        }

        var ignored = synchronizer.Drain(FixedInstant.AddMilliseconds(1200));

        Assert.Contains(".git/index", ignored.IgnoredPaths);
        Assert.Contains("obj/generated.cs", ignored.IgnoredPaths);
        Assert.Contains("assets/self-write.e2res", ignored.SuppressedPaths);
        Assert.False(ignored.FullProjectRescan);
        Assert.Equal(0, ignored.DirectoryScanCount);
    }

    [Fact]
    public void OverflowAndResumeScanOnlyAffectedDirectoriesAndAvoidFullProjectRescan()
    {
        using var workspace = CreateWorkspace("overflow", ("scenes/main.scene.json", SceneText(speed: 10, health: 100)));
        var synchronizer = CreateSynchronizer(workspace);

        File.WriteAllText(Path.Combine(workspace.ProjectRoot, "scenes", "enemy.scene.json"), SceneText(speed: 6, health: 40));
        synchronizer.Notify(ExternalFileChangeEvent.Overflow("scenes", FixedInstant));
        var overflow = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Equal(1, overflow.DirectoryScanCount);
        Assert.Equal(["scenes"], overflow.ScannedDirectories);
        Assert.False(overflow.FullProjectRescan);
        Assert.Contains("scenes/enemy.scene.json", overflow.ProcessedPaths);
        Assert.True(workspace.Documents.TryGetByPath("scenes/enemy.scene.json", out _));

        File.WriteAllText(Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json"), SceneText(speed: 18, health: 100));
        synchronizer.Notify(ExternalFileChangeEvent.Resume(FixedInstant.AddMilliseconds(300)));
        var resume = synchronizer.Drain(FixedInstant.AddMilliseconds(550));

        Assert.False(resume.FullProjectRescan);
        Assert.True(resume.DirectoryScanCount <= 2);
        Assert.Contains("scenes/main.scene.json", resume.ProcessedPaths);
        Assert.Contains("\"value\": 18", workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
    }

    [Fact]
    public void TaskDocumentsUseExternalFileContextAndRejectPrivilegedChangesWithoutOverwritingDirtyState()
    {
        using var workspace = CreateWorkspace(
            "tasks",
            (ProjectTaskStorage.GetTaskDocumentPath("task-alpha"), TaskText("task-alpha", ProjectTaskStatus.AwaitingAcceptance)));
        var synchronizer = CreateSynchronizer(workspace);
        var incoming = CreateTask("task-alpha", ProjectTaskStatus.Done);
        incoming.CompletedAt = FixedInstant.AddHours(1);
        incoming.AcceptedAt = FixedInstant.AddHours(1);
        incoming.AcceptedBy = "agent-spoof";
        incoming.AcceptanceState = ProjectTaskAcceptanceState.Accepted;
        File.WriteAllText(
            Path.Combine(workspace.ProjectRoot, ".electron2d", "tasks", "task-alpha.e2task"),
            ProjectTaskSerializer.Serialize(incoming));

        synchronizer.Notify(ExternalFileChangeEvent.Changed(
            ProjectTaskStorage.GetTaskDocumentPath("task-alpha"),
            FixedInstant));
        var rejected = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "E2D-TASK-0002");
        Assert.Equal("pending-conflict", workspace.ImportState.States[ProjectTaskStorage.GetTaskDocumentPath("task-alpha")]);
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, workspace.Tasks.GetTask("task-alpha").Status);
        var rejectedImport = Assert.Single(rejected.ImportRecords);
        Assert.Equal(PrincipalKind.ExternalFile, rejectedImport.PrincipalKind);
        Assert.Contains(OperationCapability.TaskEditUnprivilegedFields, rejectedImport.Capabilities);
        Assert.Equal("ExternalImport", rejectedImport.Origin);
        Assert.True(rejectedImport.RoutedThroughTaskManager);

        var dirty = workspace.Tasks.AddActivity(new ProjectTaskActivityRequest(
            "task-alpha",
            TaskActivityKind.Comment,
            "Local editor note.",
            new ProjectDocumentRevision(1),
            "op-local-task-dirty",
            "undo-local-task-dirty",
            new OperationContext(
                "user-1",
                PrincipalKind.Human,
                "editor-session",
                [OperationCapability.TaskWrite],
                "editor-test")));
        Assert.True(dirty.Succeeded);
        File.WriteAllText(
            Path.Combine(workspace.ProjectRoot, ".electron2d", "tasks", "task-alpha.e2task"),
            TaskText("task-alpha", ProjectTaskStatus.InProgress));

        synchronizer.Notify(ExternalFileChangeEvent.Changed(
            ProjectTaskStorage.GetTaskDocumentPath("task-alpha"),
            FixedInstant.AddMilliseconds(300)));
        var conflict = synchronizer.Drain(FixedInstant.AddMilliseconds(550));

        Assert.Contains(conflict.Diagnostics, diagnostic => diagnostic.Code is "E2D-TOOLING-0002" or "E2D-TASK-0002");
        Assert.Equal(ProjectTaskStatus.AwaitingAcceptance, workspace.Tasks.GetTask("task-alpha").Status);
        Assert.Contains("Local editor note.", workspace.Documents.GetByPath(ProjectTaskStorage.GetTaskDocumentPath("task-alpha")).Text, StringComparison.Ordinal);
    }

    [Fact]
    public void AssetCreationPublishesImportingStateForFileSystemDock()
    {
        using var workspace = CreateWorkspace("asset-state", ("scenes/main.scene.json", SceneText(speed: 10, health: 100)));
        var synchronizer = CreateSynchronizer(workspace);
        var assetPath = Path.Combine(workspace.ProjectRoot, "assets", "logo.png");
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
        File.WriteAllBytes(assetPath, [137, 80, 78, 71, 13, 10, 26, 10]);

        synchronizer.Notify(ExternalFileChangeEvent.Created("assets/logo.png", FixedInstant));
        var result = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Equal(["assets/logo.png"], result.ProcessedPaths);
        Assert.Equal(["assets/logo.png"], result.ChangedFiles);
        Assert.Equal("Importing", workspace.ImportState.States["assets/logo.png"]);
        Assert.Contains(result.ImportRecords, record =>
            record.Path == "assets/logo.png" &&
            record.FileSystemDockStatus == "Importing");
    }

    [Fact]
    public void ExternalTextChangesInOneDebounceBatchShareSingleUndoGroup()
    {
        using var workspace = CreateWorkspace(
            "external-batch-undo",
            ("scenes/main.scene.json", SceneText(speed: 10, health: 100)),
            ("scenes/enemy.scene.json", SceneText(speed: 4, health: 50)));
        var synchronizer = CreateSynchronizer(workspace);

        File.WriteAllText(Path.Combine(workspace.ProjectRoot, "scenes", "main.scene.json"), SceneText(speed: 12, health: 100));
        File.WriteAllText(Path.Combine(workspace.ProjectRoot, "scenes", "enemy.scene.json"), SceneText(speed: 6, health: 50));
        synchronizer.Notify(ExternalFileChangeEvent.Changed("scenes/main.scene.json", FixedInstant));
        synchronizer.Notify(ExternalFileChangeEvent.Changed("scenes/enemy.scene.json", FixedInstant));

        var result = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Equal(["scenes/enemy.scene.json", "scenes/main.scene.json"], result.ChangedFiles);
        var undoGroup = Assert.Single(workspace.UndoRedo.UndoGroups, group => group.StartsWith("undo-external-batch-", StringComparison.Ordinal));
        Assert.All(result.ImportRecords, record => Assert.Equal(undoGroup.Replace("undo-", "op-", StringComparison.Ordinal), record.OperationId));

        var undo = workspace.UndoRedo.UndoLast(new ProjectWorkspaceOperationContext(
            "op-undo-external-batch",
            ProjectWorkspaceActorKind.Human,
            "workspace.undo"));

        Assert.True(undo.Succeeded);
        Assert.Equal(["scenes/enemy.scene.json", "scenes/main.scene.json"], undo.ChangedDocuments);
        Assert.Contains("\"value\": 10", workspace.Documents.GetByPath("scenes/main.scene.json").Text, StringComparison.Ordinal);
        Assert.Contains("\"value\": 4", workspace.Documents.GetByPath("scenes/enemy.scene.json").Text, StringComparison.Ordinal);
    }

    [Fact]
    public void BinaryReplaceAndDeleteOfUsedResourceBecomePendingConflicts()
    {
        using var workspace = CreateWorkspace("binary-conflict", ("scenes/main.scene.json", SceneTextWithExternalAsset("res://assets/logo.png")));
        var synchronizer = CreateSynchronizer(workspace);
        var assetPath = Path.Combine(workspace.ProjectRoot, "assets", "logo.png");
        Directory.CreateDirectory(Path.GetDirectoryName(assetPath)!);
        File.WriteAllBytes(assetPath, [137, 80, 78, 71, 1]);

        synchronizer.Notify(ExternalFileChangeEvent.Created("assets/logo.png", FixedInstant));
        var created = synchronizer.Drain(FixedInstant.AddMilliseconds(250));

        Assert.Equal(["assets/logo.png"], created.ProcessedPaths);
        Assert.Equal("Importing", workspace.ImportState.States["assets/logo.png"]);

        File.WriteAllBytes(assetPath, [137, 80, 78, 71, 2]);
        synchronizer.Notify(ExternalFileChangeEvent.Changed("assets/logo.png", FixedInstant.AddMilliseconds(300)));
        var replaced = synchronizer.Drain(FixedInstant.AddMilliseconds(550));

        Assert.Contains(replaced.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.Equal("pending-conflict", workspace.ImportState.States["assets/logo.png"]);
        Assert.Empty(workspace.UndoRedo.UndoGroups);

        File.Delete(assetPath);
        synchronizer.Notify(ExternalFileChangeEvent.Deleted("assets/logo.png", FixedInstant.AddMilliseconds(600)));
        var deleted = synchronizer.Drain(FixedInstant.AddMilliseconds(850));

        Assert.Contains(deleted.Diagnostics, diagnostic => diagnostic.Code == "E2D-TOOLING-0002");
        Assert.Equal("pending-conflict", workspace.ImportState.States["assets/logo.png"]);
        Assert.DoesNotContain("assets/logo.png", deleted.DeletedPaths);
    }

    private static ExternalChangeSynchronizer CreateSynchronizer(ProjectWorkspace workspace)
    {
        return new ExternalChangeSynchronizer(workspace, new ExternalChangeSynchronizerOptions(
            debounceDelay: TimeSpan.FromMilliseconds(250),
            selfWriteSuppressionWindow: TimeSpan.FromSeconds(2)));
    }

    private static ProjectWorkspace CreateWorkspace(string name, params (string Path, string Text)[] documents)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D.Tests", "ExternalChangeSynchronizer", name, Guid.NewGuid().ToString("N"));
        var workspace = ProjectWorkspace.CreateHeadless(root, $"owner-{name}");
        foreach (var (relativePath, text) in documents)
        {
            var fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, text.ReplaceLineEndings("\n"));
            workspace.CommandBus.OpenTextDocument(
                relativePath,
                text,
                persistedRevision: 1,
                ProjectWorkspaceOperationContext.ForTest($"open-{name}-{relativePath.Replace('/', '-')}"));
        }

        return workspace;
    }

    private static string SceneText(int speed, int health)
    {
        return $$"""
            {
              "format": "Electron2D.SceneFile",
              "version": 1,
              "external": [],
              "internal": [],
              "nodes": [
                {
                  "id": 1,
                  "type": "Electron2D.Node2D",
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
              ]
            }
            """;
    }

    private static string SceneTextWithExternalAsset(string assetPath)
    {
        return $$"""
            {
              "format": "Electron2D.SceneFile",
              "version": 1,
              "external": [
                {
                  "path": "{{assetPath}}"
                }
              ],
              "internal": [],
              "nodes": [
                {
                  "id": 1,
                  "type": "Electron2D.Sprite2D",
                  "name": "Logo",
                  "parent": null,
                  "properties": {
                    "texture": {
                      "kind": "ResourcePath",
                      "value": "{{assetPath}}"
                    }
                  }
                }
              ]
            }
            """;
    }

    private static string ResourceText(string resourcePath)
    {
        return $$"""
            {
              "format": "Electron2D.ResourceFile",
              "version": 1,
              "uid": "uid://123456789",
              "type": "Electron2D.Texture2D",
              "path": "{{resourcePath}}",
              "external": [],
              "internal": [],
              "properties": {
                "resource_name": "player"
              }
            }
            """;
    }

    private static string TaskText(string taskId, ProjectTaskStatus status)
    {
        return ProjectTaskSerializer.Serialize(CreateTask(taskId, status));
    }

    private static ProjectTask CreateTask(string taskId, ProjectTaskStatus status)
    {
        var task = new ProjectTask
        {
            TaskId = taskId,
            Title = $"Task {taskId}",
            Description = "External synchronizer test task.",
            Status = status,
            Readiness = TaskReadiness.Ready,
            Priority = "P0",
            Rank = "1000",
            CreatedBy = "user-1",
            CreatedAt = FixedInstant,
            UpdatedAt = FixedInstant,
            SubmittedAt = status == ProjectTaskStatus.AwaitingAcceptance ? FixedInstant.AddMinutes(5) : null,
            AcceptanceState = status == ProjectTaskStatus.AwaitingAcceptance
                ? ProjectTaskAcceptanceState.Submitted
                : ProjectTaskAcceptanceState.Open
        };
        task.AcceptanceCriteria.Add(new AcceptanceCriterion(
            $"criterion-{taskId}",
            "External changes are synchronized through ProjectWorkspace.",
            AcceptanceCriterionState.Open,
            []));
        return task;
    }
}
