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

public sealed class ProjectWorkspaceTests
{
    [Fact]
    public void OwnershipRegistryProvidesSinglePrimaryEditorAndHeadlessWorkspace()
    {
        var now = new DateTimeOffset(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
        var registry = new ProjectWorkspaceLeaseRegistry(TimeSpan.FromSeconds(30));
        var projectRoot = CreateProjectRoot("Ownership");

        using var primary = registry.OpenEditor(projectRoot, "editor-1", now).Workspace;
        var secondEditor = registry.OpenEditor(projectRoot, "editor-2", now.AddSeconds(1));
        using var headless = registry.OpenHeadless(projectRoot, "ci", now.AddSeconds(2)).Workspace;

        Assert.Equal(ProjectWorkspaceOpenMode.EditorPrimary, primary.OpenMode);
        Assert.True(primary.CommandBus.CanExecuteMutatingCommands);
        Assert.Equal(ProjectWorkspaceOpenMode.EditorReadOnly, secondEditor.Workspace.OpenMode);
        Assert.False(secondEditor.Workspace.CommandBus.CanExecuteMutatingCommands);
        Assert.Contains("editor-1", secondEditor.Message, StringComparison.Ordinal);
        Assert.Equal(ProjectWorkspaceOpenMode.Headless, headless.OpenMode);
        Assert.True(headless.CommandBus.CanExecuteMutatingCommands);

        primary.OwnerLease.Touch(now.AddSeconds(10));
        Assert.False(primary.OwnerLease.IsStale(now.AddSeconds(20)));
        Assert.True(primary.OwnerLease.IsStale(now.AddSeconds(41)));

        var staleReplacement = registry.OpenEditor(projectRoot, "editor-3", now.AddSeconds(42));
        using var replacement = staleReplacement.Workspace;

        Assert.Equal(ProjectWorkspaceOpenMode.EditorPrimary, replacement.OpenMode);
        Assert.Equal("editor-3", replacement.OwnerLease.OwnerId);
        secondEditor.Workspace.Dispose();
    }

    [Fact]
    public void DocumentCommandsTrackDirtyStateRevisionsAndEvents()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("Documents"), "test-runner");
        var events = new List<ProjectWorkspaceEvent>();
        using var subscription = workspace.Events.Subscribe(events.Add);

        var openResult = workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 7,
            ProjectWorkspaceOperationContext.ForTest("op-open"));
        var changeResult = workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            expectedRevision: new ProjectDocumentRevision(7),
            ProjectWorkspaceOperationContext.ForTest("op-change"));
        var staleResult = workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 14),
            expectedRevision: new ProjectDocumentRevision(7),
            ProjectWorkspaceOperationContext.ForTest("op-stale"));
        var persistResult = workspace.CommandBus.MarkDocumentPersisted(
            "scenes/main.scene.json",
            expectedRevision: new ProjectDocumentRevision(8),
            ProjectWorkspaceOperationContext.ForTest("op-save"));

        var document = workspace.Documents.GetByPath("scenes/main.scene.json");

        Assert.True(openResult.Succeeded);
        Assert.True(changeResult.Succeeded);
        Assert.False(staleResult.Succeeded);
        Assert.Contains("expected revision", staleResult.Message, StringComparison.OrdinalIgnoreCase);
        Assert.True(persistResult.Succeeded);
        Assert.False(document.IsDirty);
        Assert.Equal(8, document.PersistedRevision.Value);
        Assert.Equal(8, document.InMemoryRevision.Value);
        Assert.Empty(workspace.Revisions.DirtyDocuments);
        Assert.Equal(ProjectWorkspacePersistenceState.Clean, workspace.Revisions.PersistenceState);
        Assert.Equal(3, workspace.Revisions.WorkspaceRevision.Value);
        Assert.Equal(1, workspace.Revisions.ContentRevision.Value);
        Assert.Equal(8, workspace.Revisions.DocumentRevisions["scenes/main.scene.json"].Value);

        Assert.Equal(
            [
                ProjectWorkspaceEventKind.DocumentOpened,
                ProjectWorkspaceEventKind.DocumentChanged,
                ProjectWorkspaceEventKind.DocumentPersisted
            ],
            events.Select(projectEvent => projectEvent.Kind));
        Assert.Equal("op-change", workspace.OperationJournal.Entries.Single(entry => entry.OperationId == "op-change").OperationId);
        Assert.True(workspace.OperationJournal.Entries.All(entry => entry.CompletedAt is not null));
    }

    [Fact]
    public void DiagnosticsStorePublishesEventsWithoutEditorDependencies()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("Diagnostics"), "test-runner");
        var events = new List<ProjectWorkspaceEvent>();
        using var subscription = workspace.Events.Subscribe(events.Add);
        var diagnostic = StructuredDiagnostic.Create(
            "E2D-PROJECT-0003",
            DiagnosticSeverity.Warning,
            DiagnosticCategory.Project,
            "Project file can be migrated safely.",
            new DiagnosticLocation("project.e2project.json", line: 1, column: 1),
            relatedLocations: [],
            suggestedFixes:
            [
                new DiagnosticSuggestedFix(
                    "Add version.",
                    [
                        DiagnosticFixAction.UpdateJsonProperty(
                            "project.e2project.json",
                            "/version",
                            expectedValue: null,
                            newValue: "1")
                    ])
            ]);

        workspace.Diagnostics.SetDiagnostics("project-validation", [diagnostic], ProjectWorkspaceOperationContext.ForTest("op-diag"));

        Assert.Equal(diagnostic, Assert.Single(workspace.Diagnostics.GetDiagnostics("project-validation")));
        var diagnosticsEvent = Assert.Single(events);
        Assert.Equal(ProjectWorkspaceEventKind.DiagnosticsUpdated, diagnosticsEvent.Kind);
        Assert.Equal("op-diag", diagnosticsEvent.OperationId);
        Assert.Equal("project-validation", diagnosticsEvent.Source);
        Assert.Equal(diagnostic, Assert.Single(diagnosticsEvent.Diagnostics));
        Assert.Equal(1, workspace.Revisions.WorkspaceRevision.Value);
        Assert.Equal(0, workspace.Revisions.ContentRevision.Value);
    }

    [Fact]
    public void OperationJournalRecordsRecoverySnapshotForUnfinishedTransaction()
    {
        using var workspace = ProjectWorkspace.CreateHeadless(CreateProjectRoot("Recovery"), "test-runner");
        workspace.CommandBus.OpenTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 10),
            persistedRevision: 1,
            ProjectWorkspaceOperationContext.ForTest("op-open"));
        workspace.CommandBus.ReplaceTextDocument(
            "scenes/main.scene.json",
            SceneText(speed: 12),
            expectedRevision: new ProjectDocumentRevision(1),
            ProjectWorkspaceOperationContext.ForTest("op-change"));

        workspace.OperationJournal.BeginTransaction(
            "op-agent-transaction",
            ProjectWorkspaceActorKind.Agent,
            "scene.batch-edit",
            ["scenes/main.scene.json"]);
        workspace.OperationJournal.RecordRecoverySnapshot(
            workspace.Documents.GetDirtyDocuments(),
            "Recovered dirty documents after unfinished transaction.");

        Assert.True(workspace.OperationJournal.HasUnfinishedTransaction);
        var recovery = workspace.OperationJournal.RecoverySnapshot;
        Assert.NotNull(recovery);
        Assert.Equal("Recovered dirty documents after unfinished transaction.", recovery.Message);
        var recoveredDocument = Assert.Single(recovery.Documents);
        Assert.Equal("scenes/main.scene.json", recoveredDocument.Path);
        Assert.Equal(1, recoveredDocument.PersistedRevision.Value);
        Assert.Equal(2, recoveredDocument.InMemoryRevision.Value);
        Assert.Contains("\"value\":12", recoveredDocument.Text, StringComparison.Ordinal);

        workspace.OperationJournal.CompleteTransaction("op-agent-transaction");

        Assert.False(workspace.OperationJournal.HasUnfinishedTransaction);
        Assert.True(workspace.OperationJournal.Entries.Single(entry => entry.OperationId == "op-agent-transaction").CompletedAt is not null);
    }

    private static string CreateProjectRoot(string name)
    {
        return Path.Combine(Path.GetTempPath(), "Electron2D.WorkspaceTests", name, Guid.NewGuid().ToString("N"));
    }

    private static string SceneText(int speed)
    {
        return "{" +
            "\"format\":\"Electron2D.SceneFile\"," +
            "\"version\":1," +
            "\"external\":[]," +
            "\"internal\":[]," +
            "\"nodes\":[" +
            "{\"id\":1,\"type\":\"Electron2D.Node2D\",\"name\":\"Root\",\"parent\":null,\"owner\":null,\"groups\":[],\"properties\":{" +
            "\"speed\":{\"type\":\"Int\",\"value\":" + speed.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}" +
            "}}" +
            "]}";
    }
}
