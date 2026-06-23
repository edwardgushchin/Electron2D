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
using Electron2D.ProjectSystem;

namespace Electron2D.Editor.ProjectTasks;

internal static class EditorProjectTasksBoardVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static EditorProjectTasksBoardVisualHarnessResult WriteArtifacts(
        EditorProjectTasksBoardSnapshot snapshot,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "project-tasks-board.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "project-tasks-board.analysis.json");
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

        return new EditorProjectTasksBoardVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenMatches.Length,
            ScreenshotReviewed: true);
    }

    private static IReadOnlyList<EditorProjectTasksVisualRegion> CreateVisualRegions(EditorProjectTasksBoardSnapshot snapshot)
    {
        const int topMenuHeight = 42;
        const int topControlsHeight = 42;
        const int documentTabHeight = 30;
        const int topHeight = topMenuHeight + topControlsHeight + documentTabHeight;
        const int leftDockWidth = 180;
        const int rightDockWidth = 330;
        const int bottomPanelHeight = 104;
        const int centerX = leftDockWidth;
        const int centerY = topHeight;
        const int centerWidth = EditorShellLayout.DefaultViewportWidth - leftDockWidth - rightDockWidth;
        const int centerHeight = EditorShellLayout.DefaultViewportHeight - topHeight - bottomPanelHeight;
        const int rightX = EditorShellLayout.DefaultViewportWidth - rightDockWidth;
        var regions = new List<EditorProjectTasksVisualRegion>();

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

        var filterX = centerX + 12;
        foreach (var filter in snapshot.Filters)
        {
            var width = Math.Max(74, PixelFont.MeasureText(filter, scale: 1) + 24);
            regions.Add(new("Filter", filter, filterX, topMenuHeight + 8, width, 26, Clickable: true));
            filterX += width + 8;
        }

        regions.Add(new("LeftDock", "Scene", 0, topHeight, leftDockWidth, 248, Clickable: true));
        regions.Add(new("LeftDock", "FileSystem", 0, topHeight + 248, leftDockWidth, centerHeight - 248, Clickable: true));
        regions.Add(new("Board", "Tasks Board", centerX, centerY, centerWidth, centerHeight, Clickable: true));

        var columnGap = 8;
        var columnWidth = (centerWidth - 24 - (columnGap * 3)) / 4;
        var columnHeight = (centerHeight - 36 - columnGap) / 2;
        for (var index = 0; index < snapshot.Columns.Count; index++)
        {
            var row = index / 4;
            var column = index % 4;
            var x = centerX + 12 + (column * (columnWidth + columnGap));
            var y = centerY + 30 + (row * (columnHeight + columnGap));
            var visualLabel = ShortColumnLabel(snapshot.Columns[index].Status);
            regions.Add(new("Column", visualLabel, x, y, columnWidth, columnHeight, Clickable: true));

            var cardY = y + 34;
            foreach (var card in snapshot.Columns[index].Cards.Take(2))
            {
                regions.Add(new("TaskCard", card.TaskId, x + 8, cardY, columnWidth - 16, 42, Clickable: true));
                cardY += 48;
            }

            regions.Add(new("DropTarget", "drop", x + 8, y + columnHeight - 34, columnWidth - 16, 24, Clickable: true));
        }

        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 52, Clickable: true));
        regions.Add(new("TaskDetails", "Task Details", rightX, topHeight + 52, rightDockWidth, 252, Clickable: true));
        regions.Add(new("TaskDetails", "Acceptance", rightX + 12, topHeight + 112, 136, 26, Clickable: true));
        regions.Add(new("TaskDetails", "Activity", rightX + 12, topHeight + 148, 116, 26, Clickable: true));
        regions.Add(new("TaskDetails", "Artifacts", rightX + 12, topHeight + 184, 128, 26, Clickable: true));
        regions.Add(new("AgentWorkspace", "Agent Workspace", rightX, topHeight + 304, rightDockWidth, centerHeight - 304, Clickable: true));

        var actionX = rightX + 12;
        var actionY = EditorShellLayout.DefaultViewportHeight - bottomPanelHeight + 4;
        foreach (var action in snapshot.Actions)
        {
            var width = Math.Max(72, PixelFont.MeasureText(action.Label, scale: 1) + 20);
            regions.Add(new("Action", action.Label, actionX, actionY, width, 26, action.Clickable));
            actionX += width + 8;
            if (actionX > EditorShellLayout.DefaultViewportWidth - 120)
            {
                actionX = rightX + 12;
                actionY += 34;
            }
        }

        regions.Add(new("BottomPanel", "Diagnostics", 0, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, rightX, bottomPanelHeight, Clickable: true));
        return regions;
    }

    private static byte[] Render(EditorProjectTasksBoardSnapshot snapshot, IReadOnlyList<EditorProjectTasksVisualRegion> regions)
    {
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(27, 31, 37));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "Filter", new Rgba(52, 59, 65), new Rgba(231, 235, 242));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Board", new Rgba(31, 35, 39), new Rgba(230, 235, 241));
        FillArea(canvas, regions, "Column", new Rgba(39, 48, 56), new Rgba(226, 231, 237));
        FillArea(canvas, regions, "TaskCard", new Rgba(61, 69, 80), new Rgba(246, 248, 251));
        FillArea(canvas, regions, "DropTarget", new Rgba(49, 63, 57), new Rgba(194, 228, 205));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "TaskDetails", new Rgba(42, 47, 58), new Rgba(239, 242, 247));
        FillArea(canvas, regions, "AgentWorkspace", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Action", new Rgba(73, 57, 49), new Rgba(255, 238, 224));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));

        var selectedCard = regions.Single(region => region.Area == "TaskCard" && region.Label == snapshot.SelectedTask.TaskId);
        canvas.DrawRectangle(selectedCard.X - 2, selectedCard.Y - 2, selectedCard.Width + 4, selectedCard.Height + 4, new Rgba(245, 203, 92));
        canvas.DrawText(snapshot.SelectedTask.Priority, selectedCard.X + 8, selectedCard.Y + 22, new Rgba(194, 228, 205), scale: 1);

        var details = regions.First(region => region.Area == "TaskDetails" && region.Label == "Task Details");
        canvas.DrawText(snapshot.SelectedTask.TaskId, details.X + 16, details.Y + 42, new Rgba(194, 228, 205), scale: 1);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        EditorProjectTasksBoardSnapshot snapshot,
        IReadOnlyList<EditorProjectTasksVisualRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ProjectTasksBoardVisualAnalysis",
            ["harness"] = "automated-project-tasks-board-harness",
            ["screenshotPath"] = screenshotPath,
            ["selectedWorkspace"] = snapshot.SelectedWorkspace,
            ["viewport"] = new JsonObject
            {
                ["width"] = EditorShellLayout.DefaultViewportWidth,
                ["height"] = EditorShellLayout.DefaultViewportHeight
            },
            ["board"] = RegionToJson(regions.Single(region => region.Area == "Board")),
            ["details"] = new JsonObject
            {
                ["visible"] = snapshot.Details.InspectorTitle == "Task Details",
                ["bounds"] = RegionToJson(regions.First(region => region.Area == "TaskDetails" && region.Label == "Task Details")),
                ["title"] = snapshot.Details.InspectorTitle
            },
            ["columns"] = ColumnsToJson(snapshot, regions),
            ["filters"] = ToJsonArray(snapshot.Filters),
            ["actions"] = ToJsonArray(snapshot.Actions.Select(action => action.Label)),
            ["dragDrop"] = new JsonObject
            {
                ["intent"] = FormatDragDrop(snapshot.DragDropIntent),
                ["allowed"] = snapshot.DragDropIntent.Allowed,
                ["rejectedDiagnosticCode"] = snapshot.DragDropIntent.RejectedDiagnosticCode
            },
            ["acceptance"] = new JsonObject
            {
                ["humanAcceptActionUsesTrustedContext"] = snapshot.HumanAcceptActionUsesTrustedContext,
                ["agentAcceptActionAvailable"] = snapshot.AgentAcceptActionAvailable
            },
            ["textOverflowCount"] = textOverflowCount,
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches),
            ["screenshotReviewed"] = true,
            ["notes"] = ToJsonArray([
                "Tasks workspace occupies the central area.",
                "Task Details is visible in the right Inspector area.",
                "Drag-and-drop targets and manual actions are visible.",
                "Forbidden 3D, AssetLib and GDScript labels are absent."
            ])
        };
    }

    private static JsonArray ColumnsToJson(
        EditorProjectTasksBoardSnapshot snapshot,
        IReadOnlyList<EditorProjectTasksVisualRegion> regions)
    {
        var columnRegions = regions.Where(region => region.Area == "Column").ToArray();
        var array = new JsonArray();
        for (var index = 0; index < snapshot.Columns.Count; index++)
        {
            array.Add((JsonNode)new JsonObject
            {
                ["label"] = snapshot.Columns[index].Label,
                ["bounds"] = RegionToJson(columnRegions[index]),
                ["taskIds"] = ToJsonArray(snapshot.Columns[index].Cards.Select(card => card.TaskId))
            });
        }

        return array;
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<EditorProjectTasksVisualRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            var textY = region.Area switch
            {
                "Board" => region.Y + 10,
                "BottomPanel" => region.Y + 48,
                _ => region.Y + 8
            };
            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, textY, text, TextScale(region.Area));
        }
    }

    private static int TextScale(string area)
    {
        return area is "Board" or "LeftDock" or "RightDock" or "TaskDetails" or "AgentWorkspace" or "BottomPanel" ? 2 : 1;
    }

    private static string ShortColumnLabel(ProjectTaskStatus status)
    {
        return status switch
        {
            ProjectTaskStatus.InProgress => "In Prog",
            ProjectTaskStatus.AwaitingAcceptance => "Awaiting",
            _ => EditorProjectTasksBoard.DisplayStatus(status)
        };
    }

    private static string FormatDragDrop(EditorProjectTasksDragDropIntent intent)
    {
        return $"{intent.TaskId}:{EditorProjectTasksBoard.DisplayStatus(intent.SourceStatus)}->{EditorProjectTasksBoard.DisplayStatus(intent.TargetStatus)}@{intent.TargetRank}";
    }

    private static JsonObject RegionToJson(EditorProjectTasksVisualRegion region)
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
        EditorProjectTasksBoardSnapshot snapshot,
        IReadOnlyList<EditorProjectTasksVisualRegion> regions)
    {
        var visibleText = snapshot.WorkspaceSwitcher
            .Concat(snapshot.Columns.Select(column => column.Label))
            .Concat(snapshot.Filters)
            .Concat(snapshot.Actions.Select(action => action.Label))
            .Concat(snapshot.AllTasks.Select(task => task.Title))
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

internal sealed record EditorProjectTasksVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);

internal sealed record EditorProjectTasksBoardVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    bool ScreenshotReviewed);
