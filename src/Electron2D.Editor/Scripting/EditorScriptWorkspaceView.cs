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

namespace Electron2D.Editor.Scripting;

internal sealed class EditorScriptWorkspaceView
{
    private static readonly string[] WorkspaceSwitcher = ["2D", "Script", "Game", "Tasks"];

    private static readonly string[] Prerequisites =
    [
        "TextEdit",
        "CodeEdit",
        "SyntaxHighlighter",
        "CodeHighlighter",
        "PopupMenu",
        "TabContainer",
        "Tree",
        "ItemList",
        "SplitContainer",
        "ScrollBar",
        "LineEdit",
        "Label",
        "Button",
        "IME",
        "Clipboard",
        "Selection",
        "CaretNavigation",
        "Unicode",
        "MonospaceFont",
        "LargeDocuments",
        "Scrolling",
        "GutterDrawing",
        "MouseHitTesting"
    ];

    public EditorScriptWorkspaceView(EditorScriptWorkspaceSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Snapshot = snapshot;
    }

    public EditorScriptWorkspaceSnapshot Snapshot { get; }

    public static EditorScriptWorkspaceSnapshot CreateSmokeSnapshot()
    {
        var activeDocument = new EditorScriptCodeDocumentSnapshot(
            "doc-script-hero",
            "Scripts/HeroController.cs",
            """
            using Electron2D;

            namespace ScriptWorkspaceSmoke.Scripts;

            // Basic player controller.
            public sealed class HeroController : Node
            {
                public string Message => "ready";
            }
            """,
            revision: new ProjectDocumentRevision(5),
            persistedRevision: new ProjectDocumentRevision(4),
            isDirty: true,
            diagnostics: ["E2D-SCRIPT-0001"],
            semanticVersion: 3);

        return new EditorScriptWorkspaceSnapshot(
            WorkspaceSwitcher,
            "Script",
            Prerequisites,
            prerequisiteManifestClosed: true,
            new EditorScriptFileOperationSnapshot(
                CreatedFile: "Scripts/PlayerController.cs",
                RenamedFile: "Scripts/HeroController.cs",
                DeletedFile: "Scripts/OldController.cs"),
            [
                new EditorScriptDocumentTab("Scripts/HeroController.cs", IsActive: true, IsDirty: true),
                new EditorScriptDocumentTab("Scripts/EnemyController.cs", IsActive: false, IsDirty: false)
            ],
            activeDocument,
            new EditorScriptEditorSurfaceSnapshot(
                LineNumberCount: 8,
                SyntaxTokens: ["keyword", "type", "string", "comment"],
                AutoIndentation: true,
                TabsSpaces: "Spaces:4",
                BracketMatching: true,
                QuoteMatching: true,
                CodeFolding: true,
                CurrentLine: 7,
                CaretLine: 7,
                CaretColumn: 22,
                Selection: "6,8-6,15"),
            new EditorScriptSearchSnapshot(
                SearchQuery: "Message",
                ReplacePreview: "Message->DisplayMessage",
                ProjectSearchResults: 2,
                GoToLine: 7),
            new EditorScriptCommandSnapshot(
                ClipboardRoundTrip: true,
                UndoRedoRoundTrip: true,
                SaveFile: true,
                SaveAll: true),
            new EditorScriptTextBufferSnapshot(
                CodeDocumentChangedEvents: 1,
                OperationJournalEntriesForTyping: 0,
                TextBufferUndoAvailable: true,
                WorkspaceUndoGroupId: "undo-script-ai-001",
                AgentSaveConflictDiagnostic: "E2D-SCRIPT-0002"),
            new EditorScriptExternalChangeSnapshot(
                MergeResult: "merged-non-overlap",
                ConflictMarker: true),
            new WorkspaceJobInputIdentity(
                "snap-script-001",
                new ProjectWorkspaceRevision(42),
                new ProjectWorkspaceRevision(18),
                new Dictionary<string, ProjectDocumentRevision>(StringComparer.Ordinal)
                {
                    ["Scripts/HeroController.cs"] = new(5),
                    ["Scripts/EnemyController.cs"] = new(2)
                },
                "script-build-hash"));
    }
}

internal sealed class EditorScriptWorkspaceSnapshot
{
    public EditorScriptWorkspaceSnapshot(
        IReadOnlyList<string> workspaceSwitcher,
        string selectedWorkspace,
        IReadOnlyList<string> prerequisiteManifest,
        bool prerequisiteManifestClosed,
        EditorScriptFileOperationSnapshot fileOperations,
        IReadOnlyList<EditorScriptDocumentTab> tabs,
        EditorScriptCodeDocumentSnapshot activeDocument,
        EditorScriptEditorSurfaceSnapshot editorSurface,
        EditorScriptSearchSnapshot search,
        EditorScriptCommandSnapshot commands,
        EditorScriptTextBufferSnapshot textBuffer,
        EditorScriptExternalChangeSnapshot externalChange,
        WorkspaceJobInputIdentity snapshotIdentity)
    {
        ArgumentNullException.ThrowIfNull(workspaceSwitcher);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedWorkspace);
        ArgumentNullException.ThrowIfNull(prerequisiteManifest);
        ArgumentNullException.ThrowIfNull(fileOperations);
        ArgumentNullException.ThrowIfNull(tabs);
        ArgumentNullException.ThrowIfNull(activeDocument);
        ArgumentNullException.ThrowIfNull(editorSurface);
        ArgumentNullException.ThrowIfNull(search);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(textBuffer);
        ArgumentNullException.ThrowIfNull(externalChange);
        ArgumentNullException.ThrowIfNull(snapshotIdentity);

        WorkspaceSwitcher = workspaceSwitcher.ToArray();
        SelectedWorkspace = selectedWorkspace;
        PrerequisiteManifest = prerequisiteManifest.ToArray();
        PrerequisiteManifestClosed = prerequisiteManifestClosed;
        FileOperations = fileOperations;
        Tabs = tabs.ToArray();
        ActiveDocument = activeDocument;
        EditorSurface = editorSurface;
        Search = search;
        Commands = commands;
        TextBuffer = textBuffer;
        ExternalChange = externalChange;
        SnapshotIdentity = snapshotIdentity;
    }

    public IReadOnlyList<string> WorkspaceSwitcher { get; }

    public string SelectedWorkspace { get; }

    public IReadOnlyList<string> PrerequisiteManifest { get; }

    public bool PrerequisiteManifestClosed { get; }

    public EditorScriptFileOperationSnapshot FileOperations { get; }

    public IReadOnlyList<EditorScriptDocumentTab> Tabs { get; }

    public EditorScriptCodeDocumentSnapshot ActiveDocument { get; }

    public EditorScriptEditorSurfaceSnapshot EditorSurface { get; }

    public EditorScriptSearchSnapshot Search { get; }

    public EditorScriptCommandSnapshot Commands { get; }

    public EditorScriptTextBufferSnapshot TextBuffer { get; }

    public EditorScriptExternalChangeSnapshot ExternalChange { get; }

    public WorkspaceJobInputIdentity SnapshotIdentity { get; }

    public IReadOnlyList<string> DisplayTabs => Tabs.Select(tab => tab.IsDirty ? tab.Path + "*" : tab.Path).ToArray();
}

internal sealed record EditorScriptFileOperationSnapshot(
    string CreatedFile,
    string RenamedFile,
    string DeletedFile);

internal sealed record EditorScriptDocumentTab(
    string Path,
    bool IsActive,
    bool IsDirty);

internal sealed class EditorScriptCodeDocumentSnapshot
{
    public EditorScriptCodeDocumentSnapshot(
        string documentId,
        string path,
        string text,
        ProjectDocumentRevision revision,
        ProjectDocumentRevision persistedRevision,
        bool isDirty,
        IReadOnlyList<string> diagnostics,
        int semanticVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(diagnostics);

        DocumentId = documentId;
        Path = path;
        Text = text;
        Revision = revision;
        PersistedRevision = persistedRevision;
        IsDirty = isDirty;
        Diagnostics = diagnostics.ToArray();
        SemanticVersion = semanticVersion;
    }

    public string DocumentId { get; }

    public string Path { get; }

    public string Text { get; }

    public ProjectDocumentRevision Revision { get; }

    public ProjectDocumentRevision PersistedRevision { get; }

    public bool IsDirty { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public int SemanticVersion { get; }
}

internal sealed record EditorScriptEditorSurfaceSnapshot(
    int LineNumberCount,
    IReadOnlyList<string> SyntaxTokens,
    bool AutoIndentation,
    string TabsSpaces,
    bool BracketMatching,
    bool QuoteMatching,
    bool CodeFolding,
    int CurrentLine,
    int CaretLine,
    int CaretColumn,
    string Selection);

internal sealed record EditorScriptSearchSnapshot(
    string SearchQuery,
    string ReplacePreview,
    int ProjectSearchResults,
    int GoToLine);

internal sealed record EditorScriptCommandSnapshot(
    bool ClipboardRoundTrip,
    bool UndoRedoRoundTrip,
    bool SaveFile,
    bool SaveAll);

internal sealed record EditorScriptTextBufferSnapshot(
    int CodeDocumentChangedEvents,
    int OperationJournalEntriesForTyping,
    bool TextBufferUndoAvailable,
    string WorkspaceUndoGroupId,
    string AgentSaveConflictDiagnostic);

internal sealed record EditorScriptExternalChangeSnapshot(
    string MergeResult,
    bool ConflictMarker);
