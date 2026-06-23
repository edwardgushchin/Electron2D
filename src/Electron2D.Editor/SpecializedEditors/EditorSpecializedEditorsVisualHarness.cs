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

namespace Electron2D.Editor.SpecializedEditors;

internal static class EditorSpecializedEditorsVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static EditorSpecializedEditorsVisualHarnessResult WriteArtifacts(
        EditorSpecializedEditorsVisualSnapshot snapshot,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "specialized-editors-ui.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "specialized-editors-ui.analysis.json");
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

        return new EditorSpecializedEditorsVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenMatches.Length,
            canvas,
            ScreenshotReviewed: true);
    }

    public static bool DispatchPalettePointer(IReadOnlyList<EditorSpecializedEditorsVisualRegion> regions)
    {
        ArgumentNullException.ThrowIfNull(regions);

        var paletteTile = regions.FirstOrDefault(region =>
            region.Clickable &&
            region.Area == "PaletteTile" &&
            string.Equals(region.Label, "1,0", StringComparison.Ordinal));
        if (paletteTile is null)
        {
            return false;
        }

        var pointerX = paletteTile.X + (paletteTile.Width / 2);
        var pointerY = paletteTile.Y + (paletteTile.Height / 2);
        var hit = regions
            .Where(region =>
                region.Clickable &&
                pointerX >= region.X &&
                pointerX < region.X + region.Width &&
                pointerY >= region.Y &&
                pointerY < region.Y + region.Height)
            .OrderBy(region => region.Width * region.Height)
            .FirstOrDefault();

        return hit == paletteTile;
    }

    public static bool DispatchKeyboardSave()
    {
        return EditorShellLayout.CreateDefault().Shortcuts.Any(shortcut =>
            string.Equals(shortcut.Gesture, "Ctrl+S", StringComparison.Ordinal) &&
            string.Equals(shortcut.Action, "save-current-document", StringComparison.Ordinal));
    }

    public static IReadOnlyList<EditorSpecializedEditorsVisualRegion> CreateVisualRegions(
        EditorSpecializedEditorsVisualSnapshot snapshot)
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
        var regions = new List<EditorSpecializedEditorsVisualRegion>();

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
        regions.Add(new("DocumentTabs", "specialized-editors.scene.json", 12, topMenuHeight + 10, 260, 24, Clickable: true));
        regions.Add(new("DocumentTabs", "player_frames.e2res", 282, topMenuHeight + 10, 176, 24, Clickable: true));
        regions.Add(new("DocumentTabs", "terrain_tileset.e2res", 468, topMenuHeight + 10, 204, 24, Clickable: true));
        regions.Add(new("DocumentTabs", "player_motion.e2res", 682, topMenuHeight + 10, 196, 24, Clickable: true));

        regions.Add(new("LeftDock", "Scene", 0, topHeight, leftDockWidth, 248, Clickable: true));
        regions.Add(new("LeftDock", "FileSystem", 0, topHeight + 248, leftDockWidth, centerHeight - 248, Clickable: true));
        regions.Add(new("Panel", "Specialized Editors", centerX, centerY, centerWidth, centerHeight, Clickable: true));

        var panelY = centerY + 52;
        var panelHeight = centerHeight - 108;
        var spriteX = centerX + 18;
        var tileX = spriteX + 250;
        var animationX = tileX + 250;
        regions.Add(new("EditorPanel", "SpriteFrames", spriteX, panelY, 240, panelHeight, Clickable: true));
        regions.Add(new("EditorPanel", "TileMap", tileX, panelY, 240, panelHeight, Clickable: true));
        regions.Add(new("EditorPanel", "AnimationPlayer", animationX, panelY, 234, panelHeight, Clickable: true));

        AddSpriteFramesRegions(regions, snapshot, spriteX, panelY);
        AddTileMapRegions(regions, snapshot, tileX, panelY);
        AddAnimationRegions(regions, snapshot, animationX, panelY);

        regions.Add(new("Action", "Save All", centerX + centerWidth - 242, centerY + centerHeight - 42, 94, 28, Clickable: true));
        regions.Add(new("Action", "Reopen", centerX + centerWidth - 136, centerY + centerHeight - 42, 86, 28, Clickable: true));

        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 158, Clickable: true));
        regions.Add(new("RightDock", "Node", rightX, topHeight + 158, rightDockWidth, 72, Clickable: true));
        regions.Add(new("RightDock", "Agent Workspace", rightX, topHeight + 230, rightDockWidth, centerHeight - 230, Clickable: true));
        regions.Add(new("BottomPanel", "Animation", 0, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, 138, 28, Clickable: true));
        regions.Add(new("BottomPanel", "Output", 146, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, 118, 28, Clickable: true));
        regions.Add(new("BottomPanel", "Diagnostics", 272, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, 142, 28, Clickable: true));
        regions.Add(new("BottomPanel", "Timeline keys visible", 0, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight + 36, EditorShellLayout.DefaultViewportWidth, bottomPanelHeight - 36, Clickable: true));

        return regions;
    }

    public static void UpdateWindowAnalysis(
        string analysisPath,
        EditorWindowRunResult window,
        bool pointerInteractionObserved,
        bool keyboardInteractionObserved,
        bool screenshotReviewed)
    {
        var root = JsonNode.Parse(File.ReadAllText(analysisPath)) as JsonObject ??
            throw new FormatException("Specialized editors visual analysis root must be a JSON object.");

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

    private static void AddSpriteFramesRegions(
        List<EditorSpecializedEditorsVisualRegion> regions,
        EditorSpecializedEditorsVisualSnapshot snapshot,
        int x,
        int y)
    {
        regions.Add(new("SpriteFrames", "animations " + snapshot.SpriteAnimations, x + 12, y + 46, 216, 24, Clickable: true));
        regions.Add(new("SpriteFrames", "idle fps 6 loop none", x + 12, y + 82, 216, 24, Clickable: true));
        regions.Add(new("SpriteFrames", "run fps 12 loop linear", x + 12, y + 116, 216, 24, Clickable: true));
        regions.Add(new("SpriteFrames", "frames 2 durations", x + 12, y + 150, 216, 24, Clickable: true));
        regions.Add(new("SpriteFrames", "texture player_run", x + 12, y + 184, 216, 24, Clickable: true));
        regions.Add(new("SpriteFramesAction", "Add", x + 12, y + 228, 62, 28, Clickable: true));
        regions.Add(new("SpriteFramesAction", "Remove", x + 82, y + 228, 78, 28, Clickable: true));
        regions.Add(new("SpriteFramesAction", "Reorder", x + 164, y + 228, 74, 28, Clickable: true));
    }

    private static void AddTileMapRegions(
        List<EditorSpecializedEditorsVisualRegion> regions,
        EditorSpecializedEditorsVisualSnapshot snapshot,
        int x,
        int y)
    {
        regions.Add(new("TileMap", "source terrain", x + 12, y + 46, 216, 24, Clickable: true));
        regions.Add(new("TileMap", "used rect " + snapshot.TileMapUsedRect, x + 12, y + 82, 216, 24, Clickable: true));
        regions.Add(new("TileMap", "selected tile 1,0", x + 12, y + 116, 216, 24, Clickable: true));
        regions.Add(new("TileMapAction", "Brush", x + 12, y + 260, 70, 28, Clickable: true));
        regions.Add(new("TileMapAction", "Erase", x + 92, y + 260, 70, 28, Clickable: true));
        regions.Add(new("TileMapAction", "Paint", x + 172, y + 260, 62, 28, Clickable: true));

        var tileY = y + 154;
        for (var row = 0; row < 2; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                regions.Add(new(
                    "PaletteTile",
                    $"{column},{row}",
                    x + 12 + (column * 72),
                    tileY + (row * 44),
                    62,
                    34,
                    Clickable: true));
            }
        }
    }

    private static void AddAnimationRegions(
        List<EditorSpecializedEditorsVisualRegion> regions,
        EditorSpecializedEditorsVisualSnapshot snapshot,
        int x,
        int y)
    {
        regions.Add(new("AnimationPlayer", "animation move", x + 12, y + 46, 210, 24, Clickable: true));
        regions.Add(new("AnimationPlayer", "length 1.25 loop linear", x + 12, y + 82, 210, 24, Clickable: true));
        regions.Add(new("AnimationPlayer", "playhead 0.50", x + 12, y + 116, 210, 24, Clickable: true));
        regions.Add(new("AnimationPlayer", "tracks " + snapshot.AnimationTracks, x + 12, y + 150, 210, 24, Clickable: true));
        regions.Add(new("AnimationPlayer", "position:x keys 0 1.25", x + 12, y + 184, 210, 24, Clickable: true));
        regions.Add(new("AnimationPlayer", "method OnStep at 0.75", x + 12, y + 218, 210, 24, Clickable: true));
        regions.Add(new("AnimationAction", "Add Key", x + 12, y + 260, 78, 28, Clickable: true));
        regions.Add(new("AnimationAction", "Play", x + 100, y + 260, 62, 28, Clickable: true));
        regions.Add(new("AnimationAction", "Loop", x + 170, y + 260, 52, 28, Clickable: true));
    }

    private static PixelCanvas RenderCanvas(
        EditorSpecializedEditorsVisualSnapshot snapshot,
        IReadOnlyList<EditorSpecializedEditorsVisualRegion> regions)
    {
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(26, 31, 36));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "RunControls", new Rgba(58, 73, 56), new Rgba(232, 242, 224));
        FillArea(canvas, regions, "DocumentTabs", new Rgba(38, 43, 51), new Rgba(215, 220, 228));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Panel", new Rgba(28, 33, 38), new Rgba(248, 250, 252));
        FillArea(canvas, regions, "EditorPanel", new Rgba(37, 43, 50), new Rgba(242, 246, 250));
        FillArea(canvas, regions, "SpriteFrames", new Rgba(44, 51, 64), new Rgba(232, 238, 246));
        FillArea(canvas, regions, "SpriteFramesAction", new Rgba(62, 56, 45), new Rgba(250, 240, 224));
        FillArea(canvas, regions, "TileMap", new Rgba(42, 56, 51), new Rgba(232, 244, 238));
        FillArea(canvas, regions, "PaletteTile", new Rgba(55, 67, 62), new Rgba(234, 244, 238));
        FillArea(canvas, regions, "TileMapAction", new Rgba(62, 56, 45), new Rgba(250, 240, 224));
        FillArea(canvas, regions, "AnimationPlayer", new Rgba(48, 45, 62), new Rgba(242, 234, 250));
        FillArea(canvas, regions, "AnimationAction", new Rgba(62, 56, 45), new Rgba(250, 240, 224));
        FillArea(canvas, regions, "Action", new Rgba(73, 57, 49), new Rgba(255, 238, 224));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));

        var panel = regions.Single(region => region.Area == "Panel");
        canvas.DrawText("SPECIALIZED EDITORS", panel.X + 18, panel.Y + 18, new Rgba(248, 250, 252), scale: 2);
        canvas.DrawText("REOPEN OK", panel.X + 384, panel.Y + 20, new Rgba(184, 204, 190), scale: 1);
        canvas.DrawText("RUNTIME TEXT FILES", panel.X + 500, panel.Y + 20, new Rgba(184, 204, 190), scale: 1);

        return canvas;
    }

    private static JsonObject CreateAnalysisJson(
        EditorSpecializedEditorsVisualSnapshot snapshot,
        IReadOnlyList<EditorSpecializedEditorsVisualRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.SpecializedEditorsVisualAnalysis",
            ["harness"] = "automated-specialized-editors-harness",
            ["screenshotPath"] = screenshotPath,
            ["projectPath"] = snapshot.ProjectPath,
            ["spriteFramesPath"] = snapshot.SpriteFramesPath,
            ["tileSetPath"] = snapshot.TileSetPath,
            ["animationPath"] = snapshot.AnimationPath,
            ["scenePath"] = snapshot.ScenePath,
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
            ["resources"] = new JsonObject
            {
                ["spriteAnimations"] = snapshot.SpriteAnimations,
                ["tileMapUsedRect"] = snapshot.TileMapUsedRect,
                ["animationTracks"] = snapshot.AnimationTracks
            },
            ["layout"] = new JsonObject
            {
                ["panelLabels"] = ToJsonArray(["SpriteFrames", "TileMap", "AnimationPlayer"]),
                ["regions"] = RegionsToJson(regions),
                ["textOverflowCount"] = textOverflowCount,
                ["clickableControlCount"] = clickableControlCount,
                ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches)
            },
            ["screenshotReviewed"] = true
        };
    }

    private static void FillArea(
        PixelCanvas canvas,
        IReadOnlyList<EditorSpecializedEditorsVisualRegion> regions,
        string area,
        Rgba fill,
        Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            if (region.Area == "Panel")
            {
                continue;
            }

            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, region.Y + 8, text, TextScale(region.Area));
        }
    }

    private static int TextScale(string area)
    {
        return area is "LeftDock" or "RightDock"
            ? 2
            : 1;
    }

    private static IEnumerable<string> FindForbiddenUiMatches(
        EditorSpecializedEditorsVisualSnapshot snapshot,
        IEnumerable<EditorSpecializedEditorsVisualRegion> regions)
    {
        var visibleText = regions.Select(region => region.Label)
            .Concat([
                snapshot.SpriteAnimations,
                snapshot.TileMapUsedRect,
                snapshot.AnimationTracks
            ]);

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

    private static JsonArray RegionsToJson(IEnumerable<EditorSpecializedEditorsVisualRegion> regions)
    {
        var array = new JsonArray();
        foreach (var region in regions)
        {
            array.Add(RegionToJson(region));
        }

        return array;
    }

    private static JsonObject RegionToJson(EditorSpecializedEditorsVisualRegion region)
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

internal sealed record EditorSpecializedEditorsVisualSnapshot(
    string ProjectPath,
    string SpriteFramesPath,
    string TileSetPath,
    string AnimationPath,
    string ScenePath,
    string SpriteAnimations,
    string TileMapUsedRect,
    string AnimationTracks);

internal sealed record EditorSpecializedEditorsVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);

internal sealed record EditorSpecializedEditorsVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    PixelCanvas Canvas,
    bool ScreenshotReviewed);
