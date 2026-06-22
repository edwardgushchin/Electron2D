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

public sealed class WorkspaceJobTests
{
    [Fact]
    public void JobLifecyclePublishesProgressAndCompletionWithInputIdentity()
    {
        using var workspace = CreateWorkspace("job-lifecycle");
        var input = CreateJobInput(workspace, "snap-lifecycle", "hash:debug");
        var events = new List<WorkspaceJobEvent>();
        using var subscription = workspace.Jobs.Events.Subscribe(events.Add);

        var job = workspace.Jobs.Enqueue("op-build-001", WorkspaceJobKind.Build, input, canCancel: true);

        Assert.Equal("op-build-001", job.OperationId);
        Assert.Equal(WorkspaceJobKind.Build, job.Kind);
        Assert.Equal(WorkspaceJobState.Queued, job.State);
        Assert.Equal(0, job.Progress);
        Assert.True(job.CanCancel);
        Assert.Equal("snap-lifecycle", job.InputIdentity.InputSnapshotId);
        Assert.Null(job.StartedAt);
        Assert.Null(job.CompletedAt);

        var startedAt = new DateTimeOffset(2026, 6, 22, 17, 0, 0, TimeSpan.Zero);
        job.Start(startedAt);
        job.ReportProgress(0.25);
        job.ReportProgress(0.75);

        var completedAt = startedAt.AddSeconds(5);
        job.CompleteSucceeded(completedAt);

        Assert.Equal(WorkspaceJobState.Succeeded, job.State);
        Assert.Equal(1, job.Progress);
        Assert.False(job.CanCancel);
        Assert.Equal(startedAt, job.StartedAt);
        Assert.Equal(completedAt, job.CompletedAt);
        Assert.False(job.Stale);

        Assert.Equal(
            ["operation.started", "operation.progress", "operation.progress", "operation.completed"],
            events.Select(jobEvent => jobEvent.EventName));
        Assert.All(events, jobEvent =>
        {
            Assert.Equal("op-build-001", jobEvent.OperationId);
            Assert.Equal(WorkspaceJobKind.Build, jobEvent.Kind);
            Assert.Equal("snap-lifecycle", jobEvent.InputIdentity.InputSnapshotId);
        });
        Assert.Equal(WorkspaceJobState.Succeeded, events[^1].State);
        Assert.Equal(1, events[^1].Progress);
        Assert.Equal(completedAt, events[^1].CompletedAt);
    }

    [Fact]
    public void FailedCompletionAddsStructuredDiagnosticAndKeepsLastProgress()
    {
        using var workspace = CreateWorkspace("job-failed");
        var input = CreateJobInput(workspace, "snap-failed", "hash:debug");
        var events = new List<WorkspaceJobEvent>();
        using var subscription = workspace.Jobs.Events.Subscribe(events.Add);
        var job = workspace.Jobs.Enqueue("op-build-002", WorkspaceJobKind.Build, input, canCancel: true);

        job.Start(new DateTimeOffset(2026, 6, 22, 17, 5, 0, TimeSpan.Zero));
        job.ReportProgress(0.40);

        var diagnostic = CreateProjectDiagnostic("Project build graph is malformed.");
        var completedAt = new DateTimeOffset(2026, 6, 22, 17, 5, 3, TimeSpan.Zero);
        job.CompleteFailed(diagnostic, completedAt);

        Assert.Equal(WorkspaceJobState.Failed, job.State);
        Assert.Equal(0.40, job.Progress);
        Assert.False(job.CanCancel);
        Assert.Equal(completedAt, job.CompletedAt);
        Assert.Same(diagnostic, Assert.Single(job.Diagnostics));

        Assert.Equal(
            ["operation.started", "operation.progress", "operation.diagnostic", "operation.completed"],
            events.Select(jobEvent => jobEvent.EventName));
        Assert.Same(diagnostic, events.Single(jobEvent => jobEvent.EventName == "operation.diagnostic").Diagnostic);
        Assert.Equal(WorkspaceJobState.Failed, events[^1].State);
    }

    [Fact]
    public void CancellationUsesStructuredDiagnosticsAndRefusesInvalidCancelRequests()
    {
        using var workspace = CreateWorkspace("job-cancel");
        var input = CreateJobInput(workspace, "snap-cancel", "hash:debug");
        var events = new List<WorkspaceJobEvent>();
        using var subscription = workspace.Jobs.Events.Subscribe(events.Add);
        var job = workspace.Jobs.Enqueue("op-run-001", WorkspaceJobKind.Run, input, canCancel: true);

        job.Start(new DateTimeOffset(2026, 6, 22, 17, 10, 0, TimeSpan.Zero));
        job.ReportProgress(0.15);

        var cancelledAt = new DateTimeOffset(2026, 6, 22, 17, 10, 2, TimeSpan.Zero);
        var result = job.Cancel("Stopped by user request.", cancelledAt);

        Assert.True(result.Succeeded);
        Assert.Equal("E2D-TOOLING-0001", result.Diagnostic.Code);
        Assert.Equal(DiagnosticCategory.Tooling, result.Diagnostic.Category);
        Assert.Equal(WorkspaceJobState.Cancelled, job.State);
        Assert.Equal(0.15, job.Progress);
        Assert.False(job.CanCancel);
        Assert.Equal(cancelledAt, job.CompletedAt);
        Assert.Same(result.Diagnostic, Assert.Single(job.Diagnostics));

        Assert.Equal(
            ["operation.started", "operation.progress", "operation.diagnostic", "operation.completed"],
            events.Select(jobEvent => jobEvent.EventName));
        Assert.Equal(WorkspaceJobState.Cancelled, events[^1].State);

        var terminalResult = job.Cancel("Second cancel should be refused.", cancelledAt.AddSeconds(1));

        Assert.False(terminalResult.Succeeded);
        Assert.Equal("E2D-TOOLING-0001", terminalResult.Diagnostic.Code);
        Assert.Single(job.Diagnostics);
        Assert.Equal(4, events.Count);

        var fixedJob = workspace.Jobs.Enqueue("op-test-001", WorkspaceJobKind.Test, input, canCancel: false);
        var refused = fixedJob.Cancel("Non-cancellable test job.", cancelledAt);

        Assert.False(refused.Succeeded);
        Assert.Equal(WorkspaceJobState.Queued, fixedJob.State);
        Assert.Empty(fixedJob.Diagnostics);
        Assert.Equal(4, events.Count);
    }

    [Fact]
    public void ArtifactsAndStaleMarkersFollowSnapshotInputs()
    {
        using var workspace = CreateWorkspace("job-stale");
        var input = CreateJobInput(workspace, "snap-stale-job", "hash:debug");
        var events = new List<WorkspaceJobEvent>();
        using var subscription = workspace.Jobs.Events.Subscribe(events.Add);
        var job = workspace.Jobs.Enqueue("op-run-002", WorkspaceJobKind.Run, input, canCancel: true);

        job.Start(new DateTimeOffset(2026, 6, 22, 17, 15, 0, TimeSpan.Zero));
        var artifact = new WorkspaceJobArtifact("screenshot", input);
        job.AddArtifact(artifact);

        Assert.False(job.RefreshStale("hash:debug"));
        Assert.False(job.Stale);
        Assert.False(artifact.Stale);
        Assert.Same(artifact, Assert.Single(job.Artifacts));
        Assert.Same(artifact, events.Single(jobEvent => jobEvent.EventName == "operation.artifactProduced").Artifact);

        workspace.Diagnostics.SetDiagnostics(
            "editor-selection",
            [],
            ProjectWorkspaceOperationContext.ForTest("metadata-only"));

        Assert.False(job.RefreshStale("hash:debug"));
        Assert.False(job.Stale);
        Assert.False(artifact.Stale);

        Assert.True(job.RefreshStale("hash:release"));
        Assert.True(job.Stale);
        Assert.True(artifact.Stale);

        using var changedWorkspace = CreateWorkspace("job-stale-content");
        var changedInput = CreateJobInput(changedWorkspace, "snap-stale-content", "hash:debug");
        var contentJob = changedWorkspace.Jobs.Enqueue("op-run-003", WorkspaceJobKind.Run, changedInput, canCancel: true);
        var runtimeTree = new WorkspaceJobArtifact("runtime-tree", changedInput);
        contentJob.AddArtifact(runtimeTree);
        changedWorkspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            new ProjectDocumentRevision(1),
            ProjectWorkspaceOperationContext.ForTest("change-scene"));

        Assert.True(contentJob.RefreshStale("hash:debug"));
        Assert.True(runtimeTree.Stale);
    }

    private static ProjectWorkspace CreateWorkspace(string name)
    {
        var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot(name), "test-runner");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest($"open-{name}"));
        return workspace;
    }

    private static WorkspaceJobInputIdentity CreateJobInput(
        ProjectWorkspace workspace,
        string snapshotId,
        string buildConfigurationHash)
    {
        var snapshot = WorkspaceSnapshot.Create(
            workspace,
            new WorkspaceSnapshotId(snapshotId),
            new DateTimeOffset(2026, 6, 22, 17, 0, 0, TimeSpan.Zero));
        return WorkspaceJobInputIdentity.FromSnapshot(snapshot, buildConfigurationHash);
    }

    private static StructuredDiagnostic CreateProjectDiagnostic(string message)
    {
        return StructuredDiagnostic.Create(
            "E2D-PROJECT-0001",
            DiagnosticSeverity.Error,
            DiagnosticCategory.Project,
            message,
            new DiagnosticLocation(file: "scenes/main.scene.json", line: 1, column: 1),
            relatedLocations: [],
            suggestedFixes: []);
    }

    private static string CreateProjectRoot(string name)
    {
        var root = Path.Combine(Path.GetTempPath(), "Electron2D.Tests", "WorkspaceJobs", name, Guid.NewGuid().ToString("N"));
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
}
