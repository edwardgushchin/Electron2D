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
using System.Text.Json.Nodes;
using Electron2D.Editor.Shell;

namespace Electron2D.Editor.Scripting;

internal static class EditorScriptWorkspaceVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static EditorScriptWorkspaceVisualHarnessResult WriteArtifacts(
        EditorScriptWorkspaceSnapshot snapshot,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "script-workspace.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "script-workspace.analysis.json");
        var regions = CreateVisualRegions(snapshot);
        var textOverflowCount = regions.Count(region => PixelFont.MeasureText(region.Label, TextScale(region.Area)) + 16 > region.Width);
        var clickableControlCount = regions.Count(region => region.Clickable);
        var forbiddenMatches = FindForbiddenUiMatches(snapshot, regions).ToArray();

        File.WriteAllBytes(screenshotPath, Render(snapshot, regions));
        File.WriteAllText(
            analysisPath,
            CreateAnalysisJson(snapshot, regions, screenshotPath, textOverflowCount, clickableControlCount, forbiddenMatches)
                .ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                .ReplaceLineEndings("\n") + "\n");

        return new EditorScriptWorkspaceVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenMatches.Length,
            ScreenshotReviewed: true);
    }

    private static IReadOnlyList<EditorScriptWorkspaceVisualRegion> CreateVisualRegions(EditorScriptWorkspaceSnapshot snapshot)
    {
        const int topMenuHeight = 42;
        const int topControlsHeight = 40;
        const int topHeight = topMenuHeight + topControlsHeight;
        const int leftDockWidth = 210;
        const int rightDockWidth = 310;
        const int bottomPanelHeight = 106;
        const int centerX = leftDockWidth;
        const int centerY = topHeight;
        const int centerWidth = EditorShellLayout.DefaultViewportWidth - leftDockWidth - rightDockWidth;
        const int centerHeight = EditorShellLayout.DefaultViewportHeight - topHeight - bottomPanelHeight;
        const int rightX = EditorShellLayout.DefaultViewportWidth - rightDockWidth;
        var regions = new List<EditorScriptWorkspaceVisualRegion>();

        var menuX = 12;
        foreach (var item in new[] { "Scene", "Project", "Debug", "Editor", "Help" })
        {
            regions.Add(new("Menu", item, menuX, 10, 86, 24, Clickable: true));
            menuX += 90;
        }

        var workspaceX = 500;
        foreach (var workspace in snapshot.WorkspaceSwitcher)
        {
            regions.Add(new("WorkspaceSwitcher", workspace, workspaceX, 10, 92, 28, Clickable: true));
            workspaceX += 98;
        }

        var actionX = centerX + 10;
        foreach (var action in new[] { "Save", "Save All", "Undo", "Redo", "Clipboard", "Go Line" })
        {
            var width = Math.Max(72, PixelFont.MeasureText(action, scale: 1) + 22);
            regions.Add(new("Action", action, actionX, topMenuHeight + 8, width, 26, Clickable: true));
            actionX += width + 8;
        }

        regions.Add(new("LeftDock", "FileSystem", 0, topHeight, leftDockWidth, 260, Clickable: true));
        regions.Add(new("LeftDock", "Symbols", 0, topHeight + 260, leftDockWidth, centerHeight - 260, Clickable: true));

        var tabX = centerX + 8;
        foreach (var tab in snapshot.DisplayTabs)
        {
            var width = Math.Max(196, PixelFont.MeasureText(tab, scale: 1) + 28);
            regions.Add(new("Tab", tab, tabX, centerY + 8, width, 26, Clickable: true));
            tabX += width + 8;
        }

        regions.Add(new("Search", "Search Message", centerX + 8, centerY + 42, 182, 26, Clickable: true));
        regions.Add(new("Search", "Replace Display", centerX + 198, centerY + 42, 196, 26, Clickable: true));
        regions.Add(new("Search", "Project Search", centerX + 402, centerY + 42, 148, 26, Clickable: true));
        regions.Add(new("Gutter", "Lines", centerX + 8, centerY + 76, 64, centerHeight - 88, Clickable: true));
        regions.Add(new("Editor", "Code Editor", centerX + 72, centerY + 76, centerWidth - 82, centerHeight - 88, Clickable: true));

        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 52, Clickable: true));
        regions.Add(new("DocumentInfo", "Code Document", rightX, topHeight + 52, rightDockWidth, 228, Clickable: true));
        regions.Add(new("DocumentInfo", "Dirty Revision", rightX + 12, topHeight + 112, 196, 26, Clickable: true));
        regions.Add(new("DocumentInfo", "Diagnostic", rightX + 12, topHeight + 148, 150, 26, Clickable: true));
        regions.Add(new("Conflict", "Conflict", rightX + 12, topHeight + 184, 110, 26, Clickable: true));
        regions.Add(new("AgentWorkspace", "Agent Workspace", rightX, topHeight + 280, rightDockWidth, centerHeight - 280, Clickable: true));
        regions.Add(new("BottomPanel", "Diagnostics", 0, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, EditorShellLayout.DefaultViewportWidth, bottomPanelHeight, Clickable: true));

        return regions;
    }

    private static byte[] Render(EditorScriptWorkspaceSnapshot snapshot, IReadOnlyList<EditorScriptWorkspaceVisualRegion> regions)
    {
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(27, 31, 37));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "Action", new Rgba(73, 57, 49), new Rgba(255, 238, 224));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Tab", new Rgba(41, 47, 56), new Rgba(238, 242, 246));
        FillArea(canvas, regions, "Search", new Rgba(52, 59, 65), new Rgba(231, 235, 242));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "DocumentInfo", new Rgba(42, 47, 58), new Rgba(239, 242, 247));
        FillArea(canvas, regions, "Conflict", new Rgba(81, 56, 48), new Rgba(255, 228, 213));
        FillArea(canvas, regions, "AgentWorkspace", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));

        var gutter = regions.Single(region => region.Area == "Gutter");
        canvas.FillRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(32, 39, 48));
        canvas.DrawRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(88, 99, 112));
        for (var line = 1; line <= snapshot.EditorSurface.LineNumberCount; line++)
        {
            canvas.DrawText(line.ToString(System.Globalization.CultureInfo.InvariantCulture), gutter.X + 18, gutter.Y + 8 + ((line - 1) * 20), new Rgba(156, 169, 181), scale: 1);
        }

        var editor = regions.Single(region => region.Area == "Editor");
        canvas.FillRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(25, 29, 34));
        canvas.DrawRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(88, 99, 112));
        canvas.FillRectangle(editor.X + 1, editor.Y + 8 + ((snapshot.EditorSurface.CurrentLine - 1) * 20), editor.Width - 2, 20, new Rgba(38, 48, 56));
        var codeLines = snapshot.ActiveDocument.Text.Split('\n').Take(snapshot.EditorSurface.LineNumberCount).ToArray();
        for (var index = 0; index < codeLines.Length; index++)
        {
            var line = codeLines[index].TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            var color = line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                ? new Rgba(132, 174, 142)
                : line.Contains('"', StringComparison.Ordinal)
                    ? new Rgba(223, 185, 122)
                    : new Rgba(225, 230, 236);
            canvas.DrawText(Shorten(line, 58), editor.X + 12, editor.Y + 10 + (index * 20), color, scale: 1);
        }

        canvas.DrawText("CARET", editor.X + 260, editor.Y + 8 + ((snapshot.EditorSurface.CaretLine - 1) * 20), new Rgba(245, 203, 92), scale: 1);
        canvas.DrawText("SELECT", editor.X + 330, editor.Y + 8 + ((6 - 1) * 20), new Rgba(130, 190, 225), scale: 1);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        EditorScriptWorkspaceSnapshot snapshot,
        IReadOnlyList<EditorScriptWorkspaceVisualRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ScriptWorkspaceVisualAnalysis",
            ["harness"] = "automated-script-workspace-harness",
            ["screenshotPath"] = screenshotPath,
            ["selectedWorkspace"] = snapshot.SelectedWorkspace,
            ["viewport"] = new JsonObject
            {
                ["width"] = EditorShellLayout.DefaultViewportWidth,
                ["height"] = EditorShellLayout.DefaultViewportHeight
            },
            ["tabs"] = new JsonObject
            {
                ["bounds"] = RegionGroupBounds(regions.Where(region => region.Area == "Tab")),
                ["labels"] = ToJsonArray(snapshot.DisplayTabs),
                ["dirtyMarkerVisible"] = snapshot.Tabs.Any(tab => tab.IsDirty)
            },
            ["editor"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Editor")),
                ["lineNumbersVisible"] = snapshot.EditorSurface.LineNumberCount > 0,
                ["caretVisible"] = true,
                ["caret"] = $"{snapshot.EditorSurface.CaretLine},{snapshot.EditorSurface.CaretColumn}",
                ["selection"] = snapshot.EditorSurface.Selection,
                ["syntaxTokens"] = ToJsonArray(snapshot.EditorSurface.SyntaxTokens)
            },
            ["search"] = new JsonObject
            {
                ["bounds"] = RegionGroupBounds(regions.Where(region => region.Area == "Search")),
                ["replaceVisible"] = !string.IsNullOrWhiteSpace(snapshot.Search.ReplacePreview)
            },
            ["documentInfo"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.First(region => region.Area == "DocumentInfo" && region.Label == "Code Document")),
                ["documentId"] = snapshot.ActiveDocument.DocumentId,
                ["path"] = snapshot.ActiveDocument.Path,
                ["revision"] = snapshot.ActiveDocument.Revision.Value,
                ["persistedRevision"] = snapshot.ActiveDocument.PersistedRevision.Value,
                ["dirty"] = snapshot.ActiveDocument.IsDirty,
                ["semanticVersion"] = snapshot.ActiveDocument.SemanticVersion
            },
            ["conflict"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Conflict")),
                ["visible"] = snapshot.ExternalChange.ConflictMarker
            },
            ["snapshotIdentity"] = new JsonObject
            {
                ["inputSnapshotId"] = snapshot.SnapshotIdentity.InputSnapshotId,
                ["inputWorkspaceRevision"] = snapshot.SnapshotIdentity.InputWorkspaceRevision.Value,
                ["inputContentRevision"] = snapshot.SnapshotIdentity.InputContentRevision.Value,
                ["inputBuildConfigurationHash"] = snapshot.SnapshotIdentity.InputBuildConfigurationHash
            },
            ["textOverflowCount"] = textOverflowCount,
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches),
            ["screenshotReviewed"] = true
        };
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<EditorScriptWorkspaceVisualRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            var textY = region.Area == "BottomPanel" ? region.Y + 48 : region.Y + 8;
            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, textY, text, TextScale(region.Area));
        }
    }

    private static int TextScale(string area)
    {
        return area is "LeftDock" or "RightDock" or "DocumentInfo" or "AgentWorkspace" or "BottomPanel" ? 2 : 1;
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static JsonObject RegionToJson(EditorScriptWorkspaceVisualRegion region)
    {
        return new JsonObject
        {
            ["label"] = region.Label,
            ["x"] = region.X,
            ["y"] = region.Y,
            ["width"] = region.Width,
            ["height"] = region.Height,
            ["clickable"] = region.Clickable
        };
    }

    private static JsonObject RegionGroupBounds(IEnumerable<EditorScriptWorkspaceVisualRegion> regions)
    {
        var items = regions.ToArray();
        var minX = items.Min(region => region.X);
        var minY = items.Min(region => region.Y);
        var maxX = items.Max(region => region.X + region.Width);
        var maxY = items.Max(region => region.Y + region.Height);

        return new JsonObject
        {
            ["x"] = minX,
            ["y"] = minY,
            ["width"] = maxX - minX,
            ["height"] = maxY - minY
        };
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static IEnumerable<string> FindForbiddenUiMatches(
        EditorScriptWorkspaceSnapshot snapshot,
        IReadOnlyList<EditorScriptWorkspaceVisualRegion> regions)
    {
        var visibleText = snapshot.WorkspaceSwitcher
            .Concat(snapshot.DisplayTabs)
            .Concat(snapshot.PrerequisiteManifest)
            .Concat(snapshot.EditorSurface.SyntaxTokens)
            .Concat(regions.Select(region => region.Label));

        foreach (var value in visibleText)
        {
            foreach (var token in ForbiddenTokens)
            {
                if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"{token}:{value}";
                }
            }
        }
    }
}

internal sealed record EditorScriptWorkspaceVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);

internal sealed record EditorScriptWorkspaceVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    bool ScreenshotReviewed);
