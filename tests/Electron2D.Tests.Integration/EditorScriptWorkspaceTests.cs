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
using System.Buffers.Binary;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class EditorScriptWorkspaceTests
{
    [Fact]
    public async Task ScriptWorkspaceSmokeRunWritesTextEditorModelAndVisualAcceptanceArtifacts()
    {
        var root = FindRepositoryRoot();
        var projectPath = Path.Combine(root, "src", "Electron2D.Editor", "Electron2D.Editor.csproj");
        var workRoot = CreateTemporaryDirectory("electron2d-script-workspace-");

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("run");
            startInfo.ArgumentList.Add("--project");
            startInfo.ArgumentList.Add(projectPath);
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--script-workspace-smoke");
            startInfo.ArgumentList.Add(workRoot);

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start dotnet run.");
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            var output = await outputTask;
            var error = await errorTask;

            Assert.True(
                process.ExitCode == 0,
                $"Editor Script workspace smoke run failed with exit code {process.ExitCode}.{Environment.NewLine}stdout:{Environment.NewLine}{output}{Environment.NewLine}stderr:{Environment.NewLine}{error}");

            var lines = ParseMachineReadableOutput(output);

            Assert.Contains("Electron2D.Editor script workspace smoke passed", output);
            Assert.Equal("2D|Script|Game|Tasks", lines["WorkspaceSwitcher"]);
            Assert.Equal("Script", lines["SelectedWorkspace"]);
            Assert.Equal("True", lines["PrerequisiteManifestClosed"]);
            Assert.Equal(
                "TextEdit|CodeEdit|SyntaxHighlighter|CodeHighlighter|PopupMenu|TabContainer|Tree|ItemList|SplitContainer|ScrollBar|LineEdit|Label|Button|IME|Clipboard|Selection|CaretNavigation|Unicode|MonospaceFont|LargeDocuments|Scrolling|GutterDrawing|MouseHitTesting",
                lines["PrerequisiteManifest"]);
            Assert.Equal("Scripts/PlayerController.cs", lines["CreatedFile"]);
            Assert.Equal("Scripts/HeroController.cs", lines["RenamedFile"]);
            Assert.Equal("Scripts/OldController.cs", lines["DeletedFile"]);
            Assert.Equal("Scripts/HeroController.cs*|Scripts/EnemyController.cs", lines["OpenTabs"]);
            Assert.Equal("Scripts/HeroController.cs", lines["ActiveTab"]);
            Assert.Equal("8", lines["LineNumberCount"]);
            Assert.Equal("keyword|type|string|comment", lines["SyntaxTokens"]);
            Assert.Equal("True", lines["AutoIndentation"]);
            Assert.Equal("Spaces:4", lines["TabsSpaces"]);
            Assert.Equal("True", lines["BracketMatching"]);
            Assert.Equal("True", lines["QuoteMatching"]);
            Assert.Equal("True", lines["CodeFolding"]);
            Assert.Equal("7", lines["CurrentLine"]);
            Assert.Equal("7,22", lines["Caret"]);
            Assert.Equal("6,8-6,15", lines["Selection"]);
            Assert.Equal("Message", lines["SearchQuery"]);
            Assert.Equal("Message->DisplayMessage", lines["ReplacePreview"]);
            Assert.Equal("2", lines["ProjectSearchResults"]);
            Assert.Equal("7", lines["GoToLine"]);
            Assert.Equal("True", lines["ClipboardRoundTrip"]);
            Assert.Equal("True", lines["UndoRedoRoundTrip"]);
            Assert.Equal("True", lines["SaveFile"]);
            Assert.Equal("True", lines["SaveAll"]);
            Assert.Equal("doc-script-hero", lines["DocumentId"]);
            Assert.Equal("Scripts/HeroController.cs", lines["DocumentPath"]);
            Assert.Equal("5", lines["DocumentRevision"]);
            Assert.Equal("4", lines["PersistedRevision"]);
            Assert.Equal("True", lines["DirtyState"]);
            Assert.Equal("1", lines["DiagnosticCount"]);
            Assert.Equal("3", lines["SemanticVersion"]);
            Assert.Equal("1", lines["CodeDocumentChangedEvents"]);
            Assert.Equal("0", lines["OperationJournalEntriesForTyping"]);
            Assert.Equal("True", lines["TextBufferUndoAvailable"]);
            Assert.Equal("undo-script-ai-001", lines["WorkspaceUndoGroupId"]);
            Assert.Equal("E2D-SCRIPT-0002", lines["AgentSaveConflictDiagnostic"]);
            Assert.Equal("merged-non-overlap", lines["ExternalMergeResult"]);
            Assert.Equal("True", lines["ExternalConflictMarker"]);
            Assert.Equal("snap-script-001", lines["InputSnapshotId"]);
            Assert.Equal("42", lines["InputWorkspaceRevision"]);
            Assert.Equal("18", lines["InputContentRevision"]);
            Assert.Equal("script-build-hash", lines["InputBuildConfigurationHash"]);
            Assert.Equal("True", lines["ScreenshotReviewed"]);

            var statePath = lines["StatePath"];
            var screenshotPath = lines["ScreenshotPath"];
            var analysisPath = lines["AnalysisPath"];

            Assert.True(File.Exists(statePath), $"Missing Script workspace state artifact: {statePath}");
            Assert.True(File.Exists(screenshotPath), $"Missing Script workspace screenshot artifact: {screenshotPath}");
            Assert.True(File.Exists(analysisPath), $"Missing Script workspace visual analysis artifact: {analysisPath}");

            var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
            Assert.Equal(1280, width);
            Assert.Equal(720, height);

            using var analysis = JsonDocument.Parse(File.ReadAllText(analysisPath));
            var data = analysis.RootElement;

            Assert.Equal("Electron2D.ScriptWorkspaceVisualAnalysis", data.GetProperty("format").GetString());
            Assert.Equal("automated-script-workspace-harness", data.GetProperty("harness").GetString());
            Assert.Equal("Script", data.GetProperty("selectedWorkspace").GetString());
            Assert.Equal(1280, data.GetProperty("viewport").GetProperty("width").GetInt32());
            Assert.Equal(720, data.GetProperty("viewport").GetProperty("height").GetInt32());
            Assert.Equal(0, data.GetProperty("textOverflowCount").GetInt32());
            Assert.Equal(0, data.GetProperty("forbiddenUiMatches").GetArrayLength());
            Assert.True(data.GetProperty("clickableControlCount").GetInt32() >= 16);
            Assert.True(data.GetProperty("screenshotReviewed").GetBoolean());
            Assert.True(data.GetProperty("tabs").GetProperty("dirtyMarkerVisible").GetBoolean());
            Assert.True(data.GetProperty("editor").GetProperty("lineNumbersVisible").GetBoolean());
            Assert.True(data.GetProperty("editor").GetProperty("caretVisible").GetBoolean());
            Assert.Equal("6,8-6,15", data.GetProperty("editor").GetProperty("selection").GetString());
            Assert.True(data.GetProperty("search").GetProperty("replaceVisible").GetBoolean());
            Assert.Equal("doc-script-hero", data.GetProperty("documentInfo").GetProperty("documentId").GetString());
            Assert.True(data.GetProperty("conflict").GetProperty("visible").GetBoolean());
        }
        finally
        {
            Directory.Delete(workRoot, recursive: true);
        }
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG must contain a signature and IHDR chunk.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47 }, bytes.Take(4).ToArray());
        Assert.Equal("IHDR", System.Text.Encoding.ASCII.GetString(bytes, 12, 4));

        return (
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)),
            BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    private static Dictionary<string, string> ParseMachineReadableOutput(string output)
    {
        return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.Ordinal);
    }

    private static string CreateTemporaryDirectory(string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), prefix + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }
}
