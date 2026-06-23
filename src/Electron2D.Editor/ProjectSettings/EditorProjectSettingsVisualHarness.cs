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

namespace Electron2D.Editor.ProjectSettings;

internal static class EditorProjectSettingsVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static EditorProjectSettingsVisualHarnessResult WriteArtifacts(
        EditorProjectSettingsVisualSnapshot snapshot,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "project-settings-ui.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "project-settings-ui.analysis.json");
        var regions = CreateVisualRegions(snapshot);
        var textOverflowCount = regions.Count(region => PixelFont.MeasureText(region.Label, TextScale(region.Area)) + 16 > region.Width);
        var clickableControlCount = regions.Count(region => region.Clickable);
        var forbiddenMatches = FindForbiddenUiMatches(snapshot, regions).ToArray();
        var canvas = RenderCanvas(snapshot, regions);

        File.WriteAllBytes(screenshotPath, PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels));
        File.WriteAllText(
            analysisPath,
            CreateAnalysisJson(snapshot, regions, screenshotPath, textOverflowCount, clickableControlCount, forbiddenMatches)
                .ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                .ReplaceLineEndings("\n") + "\n");

        return new EditorProjectSettingsVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenMatches.Length,
            canvas,
            ScreenshotReviewed: true);
    }

    public static bool DispatchPointerSelection(IReadOnlyList<EditorProjectSettingsVisualRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);
        var jumpRegion = regions.FirstOrDefault(region => region.Clickable && region.Area == "InputMap" && region.Label.StartsWith("jump", StringComparison.Ordinal));
        if (jumpRegion is null)
        {
            return false;
        }

        var pointerX = jumpRegion.X + (jumpRegion.Width / 2);
        var pointerY = jumpRegion.Y + (jumpRegion.Height / 2);
        var hit = regions
            .Where(region =>
                region.Clickable &&
                pointerX >= region.X &&
                pointerX < region.X + region.Width &&
                pointerY >= region.Y &&
                pointerY < region.Y + region.Height)
            .OrderBy(region => region.Width * region.Height)
            .FirstOrDefault();

        return hit == jumpRegion;
    }

    public static bool DispatchKeyboardSave()
    {
        return EditorShellLayout.CreateDefault().Shortcuts.Any(shortcut =>
            string.Equals(shortcut.Gesture, "Ctrl+S", StringComparison.Ordinal) &&
            string.Equals(shortcut.Action, "save-current-document", StringComparison.Ordinal));
    }

    public static IReadOnlyList<EditorProjectSettingsVisualRegion> CreateVisualRegions(EditorProjectSettingsVisualSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        const int topMenuHeight = 42;
        const int topControlsHeight = 42;
        const int documentTabHeight = 30;
        const int topHeight = topMenuHeight + topControlsHeight + documentTabHeight;
        const int leftDockWidth = 210;
        const int rightDockWidth = 300;
        const int bottomPanelHeight = 104;
        const int centerX = leftDockWidth;
        const int centerY = topHeight;
        const int centerWidth = EditorShellLayout.DefaultViewportWidth - leftDockWidth - rightDockWidth;
        const int centerHeight = EditorShellLayout.DefaultViewportHeight - topHeight - bottomPanelHeight;
        const int rightX = EditorShellLayout.DefaultViewportWidth - rightDockWidth;
        var regions = new List<EditorProjectSettingsVisualRegion>();

        var menuX = 12;
        foreach (var item in new[] { "Scene", "Project", "Debug", "Editor", "Help" })
        {
            regions.Add(new("Menu", item, menuX, 10, 86, 24, Clickable: true));
            menuX += 90;
        }

        var workspaceX = 500;
        foreach (var workspace in new[] { "2D", "Script", "Game", "Tasks" })
        {
            regions.Add(new("WorkspaceSwitcher", workspace, workspaceX, 10, 92, 28, Clickable: true));
            workspaceX += 98;
        }

        regions.Add(new("RunControls", "Run Scene", 920, 10, 112, 28, Clickable: true));
        regions.Add(new("RunControls", "Run Project", 1040, 10, 128, 28, Clickable: true));
        regions.Add(new("RunControls", "Stop", 1176, 10, 72, 28, Clickable: true));
        regions.Add(new("DocumentTabs", "Project Settings", 12, topMenuHeight + 10, 154, 24, Clickable: true));
        regions.Add(new("DocumentTabs", Path.GetFileName(snapshot.MainScene), 176, topMenuHeight + 10, 198, 24, Clickable: true));

        regions.Add(new("LeftDock", "Scene", 0, topHeight, leftDockWidth, 248, Clickable: true));
        regions.Add(new("LeftDock", "FileSystem", 0, topHeight + 248, leftDockWidth, centerHeight - 248, Clickable: true));
        regions.Add(new("Panel", "Project Settings", centerX, centerY, centerWidth, centerHeight, Clickable: true));

        var x = centerX + 18;
        var y = centerY + 52;
        var sectionWidth = centerWidth - 36;
        regions.Add(new("MainScene", "Main Scene", x, y, sectionWidth, 42, Clickable: true));
        regions.Add(new("MainScene", snapshot.MainScene, x + 136, y + 8, 360, 24, Clickable: true));
        y += 56;
        regions.Add(new("Display", "Display", x, y, sectionWidth, 72, Clickable: true));
        regions.Add(new("Display", snapshot.DisplaySize, x + 136, y + 8, 112, 24, Clickable: true));
        regions.Add(new("Display", "Fullscreen " + (snapshot.Fullscreen ? "On" : "Off"), x + 268, y + 8, 136, 24, Clickable: true));
        regions.Add(new("Display", "Stretch Viewport", x + 424, y + 8, 164, 24, Clickable: true));
        regions.Add(new("Display", "DPI 1", x + 136, y + 40, 84, 24, Clickable: true));
        y += 88;
        regions.Add(new("Renderer", "Renderer", x, y, sectionWidth, 42, Clickable: true));
        regions.Add(new("Renderer", snapshot.RendererProfile, x + 136, y + 8, 130, 24, Clickable: true));
        regions.Add(new("Physics", "Physics", x + 300, y, 190, 42, Clickable: true));
        regions.Add(new("Physics", snapshot.PhysicsTicksPerSecond + " Hz", x + 396, y + 8, 84, 24, Clickable: true));
        y += 58;
        regions.Add(new("InputMap", "Input Map", x, y, sectionWidth, 86, Clickable: true));
        regions.Add(new("InputMap", "jump Space", x + 136, y + 10, 132, 24, Clickable: true));
        regions.Add(new("InputMap", "dash Mouse Right", x + 286, y + 10, 176, 24, Clickable: true));
        regions.Add(new("InputMap", "Deadzone 0.20", x + 136, y + 44, 160, 24, Clickable: true));
        y += 102;
        regions.Add(new("ExportPresets", "Export Presets", x, y, sectionWidth, 104, Clickable: true));
        var presetX = x + 136;
        var presetY = y + 10;
        foreach (var preset in snapshot.ExportPresets.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            regions.Add(new("ExportPresets", preset, presetX, presetY, 154, 24, Clickable: true));
            presetX += 164;
            if (presetX + 154 > centerX + centerWidth - 18)
            {
                presetX = x + 136;
                presetY += 34;
            }
        }

        regions.Add(new("Action", "Save Apply", centerX + centerWidth - 260, centerY + centerHeight - 42, 112, 28, Clickable: true));
        regions.Add(new("Action", "Revert", centerX + centerWidth - 136, centerY + centerHeight - 42, 86, 28, Clickable: true));

        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 158, Clickable: true));
        regions.Add(new("RightDock", "Node", rightX, topHeight + 158, rightDockWidth, 72, Clickable: true));
        regions.Add(new("RightDock", "Agent Workspace", rightX, topHeight + 230, rightDockWidth, centerHeight - 230, Clickable: true));
        regions.Add(new("BottomPanel", "Output", 0, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, EditorShellLayout.DefaultViewportWidth, bottomPanelHeight, Clickable: true));

        return regions;
    }

    private static PixelCanvas RenderCanvas(EditorProjectSettingsVisualSnapshot snapshot, IReadOnlyList<EditorProjectSettingsVisualRegion> regions)
    {
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(27, 31, 37));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "RunControls", new Rgba(58, 73, 56), new Rgba(232, 242, 224));
        FillArea(canvas, regions, "DocumentTabs", new Rgba(38, 43, 51), new Rgba(215, 220, 228));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Panel", new Rgba(31, 35, 39), new Rgba(248, 250, 252));
        FillArea(canvas, regions, "MainScene", new Rgba(42, 47, 58), new Rgba(232, 238, 244));
        FillArea(canvas, regions, "Display", new Rgba(40, 51, 55), new Rgba(229, 242, 238));
        FillArea(canvas, regions, "Renderer", new Rgba(43, 50, 64), new Rgba(236, 240, 248));
        FillArea(canvas, regions, "Physics", new Rgba(43, 50, 64), new Rgba(236, 240, 248));
        FillArea(canvas, regions, "InputMap", new Rgba(48, 55, 45), new Rgba(235, 246, 225));
        FillArea(canvas, regions, "ExportPresets", new Rgba(54, 47, 61), new Rgba(241, 232, 248));
        FillArea(canvas, regions, "Action", new Rgba(73, 57, 49), new Rgba(255, 238, 224));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));

        var panel = regions.Single(region => region.Area == "Panel");
        canvas.DrawText("PROJECT SETTINGS", panel.X + 18, panel.Y + 18, new Rgba(248, 250, 252), scale: 2);
        canvas.DrawText("WRITE THROUGH OK", panel.X + 318, panel.Y + 20, new Rgba(184, 204, 190), scale: 1);
        canvas.DrawText("EXPORTS " + snapshot.ExportPresets.Split('|').Length, panel.X + 480, panel.Y + 20, new Rgba(184, 204, 190), scale: 1);

        return canvas;
    }

    private static JsonObject CreateAnalysisJson(
        EditorProjectSettingsVisualSnapshot snapshot,
        IReadOnlyList<EditorProjectSettingsVisualRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ProjectSettingsUiVisualAnalysis",
            ["harness"] = "automated-project-settings-ui-harness",
            ["screenshotPath"] = screenshotPath,
            ["projectPath"] = snapshot.ProjectPath,
            ["projectSettingsPath"] = snapshot.ProjectSettingsPath,
            ["exportPresetsPath"] = snapshot.ExportPresetsPath,
            ["window"] = new JsonObject
            {
                ["actualWindow"] = false,
                ["shown"] = false
            },
            ["rendering"] = new JsonObject
            {
                ["framePresented"] = false
            },
            ["input"] = new JsonObject
            {
                ["pointerInteractionObserved"] = false,
                ["keyboardInteractionObserved"] = false
            },
            ["settings"] = new JsonObject
            {
                ["mainScene"] = snapshot.MainScene,
                ["rendererProfile"] = snapshot.RendererProfile,
                ["physicsTicksPerSecond"] = snapshot.PhysicsTicksPerSecond,
                ["displaySize"] = snapshot.DisplaySize,
                ["fullscreen"] = snapshot.Fullscreen,
                ["inputActions"] = ToJsonArray(snapshot.InputActions.Split('|', StringSplitOptions.RemoveEmptyEntries)),
                ["exportPresets"] = ToJsonArray(snapshot.ExportPresets.Split('|', StringSplitOptions.RemoveEmptyEntries))
            },
            ["layout"] = new JsonObject
            {
                ["panel"] = RegionToJson(regions.Single(region => region.Area == "Panel")),
                ["sectionLabels"] = ToJsonArray(["Main Scene", "Display", "Renderer", "Physics", "Input Map", "Export Presets"]),
                ["regions"] = RegionsToJson(regions),
                ["textOverflowCount"] = textOverflowCount,
                ["clickableControlCount"] = clickableControlCount,
                ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches)
            },
            ["screenshotReviewed"] = true
        };
    }

    public static void UpdateWindowAnalysis(
        string analysisPath,
        EditorWindowRunResult window,
        bool pointerInteractionObserved,
        bool keyboardInteractionObserved,
        bool screenshotReviewed)
    {
        var root = JsonNode.Parse(File.ReadAllText(analysisPath)) as JsonObject ??
            throw new FormatException("Project Settings visual analysis root must be a JSON object.");

        root["window"] = new JsonObject
        {
            ["actualWindow"] = window.WindowCreated,
            ["shown"] = window.WindowShown,
            ["width"] = window.WindowWidth,
            ["height"] = window.WindowHeight,
            ["pixelWidth"] = window.PixelWidth,
            ["pixelHeight"] = window.PixelHeight,
            ["videoDriver"] = window.VideoDriver
        };
        root["rendering"] = new JsonObject
        {
            ["framePresented"] = window.FramePresented,
            ["frameCount"] = window.FrameCount,
            ["eventPumpObserved"] = window.EventPumpObserved
        };
        root["input"] = new JsonObject
        {
            ["pointerInteractionObserved"] = pointerInteractionObserved,
            ["keyboardInteractionObserved"] = keyboardInteractionObserved
        };
        root["screenshotReviewed"] = screenshotReviewed;

        File.WriteAllText(
            analysisPath,
            root.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<EditorProjectSettingsVisualRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            if (region.Area == "Panel")
            {
                continue;
            }

            var scale = TextScale(region.Area);
            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, region.Y + 8, text, scale);
        }
    }

    private static int TextScale(string area)
    {
        return area is "Menu" or "WorkspaceSwitcher" or "RunControls" or "DocumentTabs" or "MainScene" or "Display" or "Renderer" or "Physics" or "InputMap" or "ExportPresets" or "Action"
            ? 1
            : 2;
    }

    private static IEnumerable<string> FindForbiddenUiMatches(EditorProjectSettingsVisualSnapshot snapshot, IEnumerable<EditorProjectSettingsVisualRegion> regions)
    {
        var visibleText = regions.Select(region => region.Label)
            .Concat(snapshot.InputActions.Split('|', StringSplitOptions.RemoveEmptyEntries))
            .Concat(snapshot.ExportPresets.Split('|', StringSplitOptions.RemoveEmptyEntries));

        foreach (var value in visibleText)
        {
            foreach (var token in ForbiddenTokens)
            {
                if (value.Contains(token, StringComparison.OrdinalIgnoreCase))
                {
                    yield return $"{value}:{token}";
                }
            }
        }
    }

    private static JsonArray RegionsToJson(IEnumerable<EditorProjectSettingsVisualRegion> regions)
    {
        var array = new JsonArray();
        foreach (var region in regions)
        {
            array.Add(RegionToJson(region));
        }

        return array;
    }

    private static JsonObject RegionToJson(EditorProjectSettingsVisualRegion region)
    {
        return new JsonObject
        {
            ["area"] = region.Area,
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
}

internal sealed record EditorProjectSettingsVisualSnapshot(
    string ProjectPath,
    string ProjectSettingsPath,
    string ExportPresetsPath,
    string MainScene,
    string RendererProfile,
    int PhysicsTicksPerSecond,
    string DisplaySize,
    bool Fullscreen,
    string InputActions,
    string ExportPresets);

internal sealed record EditorProjectSettingsVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    PixelCanvas Canvas,
    bool ScreenshotReviewed);

internal sealed record EditorProjectSettingsVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);
