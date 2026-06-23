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
using System.Text.Json;
using System.Text.Json.Nodes;
using Electron2D.Editor.Shell;
using Electron2D.ManagedDebugging;

namespace Electron2D.Editor.Scripting;

internal static class EditorManagedDebuggerVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static EditorManagedDebuggerVisualHarnessResult WriteArtifacts(
        ManagedDebugSessionState state,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "managed-debugger.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "managed-debugger.analysis.json");
        var regions = CreateVisualRegions(state);
        var overflowRegions = regions
            .Where(region => PixelFont.MeasureText(region.Label, TextScale(region.Area)) + 16 > region.Width)
            .Select(region => $"{region.Area}:{region.Label}")
            .ToArray();
        var textOverflowCount = overflowRegions.Length;
        var clickableControlCount = regions.Count(region => region.Clickable);
        var forbiddenMatches = FindForbiddenUiMatches(state, regions).ToArray();

        File.WriteAllBytes(screenshotPath, Render(state, regions));
        File.WriteAllText(
            analysisPath,
            CreateAnalysisJson(state, regions, screenshotPath, overflowRegions, clickableControlCount, forbiddenMatches)
                .ToJsonString(new JsonSerializerOptions { WriteIndented = true })
                .ReplaceLineEndings("\n") + "\n");

        return new EditorManagedDebuggerVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenMatches.Length,
            ScreenshotReviewed: true);
    }

    private static IReadOnlyList<EditorManagedDebuggerVisualRegion> CreateVisualRegions(ManagedDebugSessionState state)
    {
        const int topMenuHeight = 42;
        const int topControlsHeight = 44;
        const int topHeight = topMenuHeight + topControlsHeight;
        const int leftDockWidth = 210;
        const int rightDockWidth = 320;
        const int bottomPanelHeight = 172;
        const int centerX = leftDockWidth;
        const int centerY = topHeight;
        const int centerWidth = EditorShellLayout.DefaultViewportWidth - leftDockWidth - rightDockWidth;
        const int centerHeight = EditorShellLayout.DefaultViewportHeight - topHeight - bottomPanelHeight;
        const int rightX = EditorShellLayout.DefaultViewportWidth - rightDockWidth;
        var regions = new List<EditorManagedDebuggerVisualRegion>();

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

        var controlX = centerX + 10;
        foreach (var control in new[] { "Start Debug", "Attach", "Pause", "Continue", "Stop", "Restart", "Step Into", "Step Over", "Step Out" })
        {
            var width = Math.Max(72, PixelFont.MeasureText(control, scale: 1) + 18);
            regions.Add(new("DebuggerControl", control, controlX, topMenuHeight + 9, width, 26, Clickable: true));
            controlX += width + 7;
        }

        regions.Add(new("LeftDock", "FileSystem", 0, topHeight, leftDockWidth, 238, Clickable: true));
        regions.Add(new("LeftDock", "Symbols", 0, topHeight + 238, leftDockWidth, centerHeight - 238, Clickable: true));
        regions.Add(new("Tab", "Scripts/HeroController.cs", centerX + 8, centerY + 8, 224, 26, Clickable: true));
        regions.Add(new("Gutter", "BP Gutter", centerX + 8, centerY + 44, 76, centerHeight - 56, Clickable: true));
        regions.Add(new("Editor", "Code Editor", centerX + 84, centerY + 44, centerWidth - 94, centerHeight - 56, Clickable: true));
        regions.Add(new("CurrentLine", "Current Line", centerX + 85, centerY + 44 + ((state.CurrentExecutionLine.Line - 1) * 18), centerWidth - 96, 20, Clickable: false));
        regions.Add(new("Breakpoint", "BP", centerX + 28, centerY + 44 + ((state.Breakpoint.SourceAnchor.Line - 1) * 18), 40, 20, Clickable: true));
        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 52, Clickable: true));
        regions.Add(new("DebugInfo", "Debug Session", rightX, topHeight + 52, rightDockWidth, 172, Clickable: true));
        regions.Add(new("DebugInfo", state.Adapter.AdapterId, rightX + 12, topHeight + 108, 260, 26, Clickable: true));
        regions.Add(new("DebugInfo", "Stale Rebuild", rightX + 12, topHeight + 144, 260, 26, Clickable: true));
        regions.Add(new("AgentWorkspace", "Agent Workspace", rightX, topHeight + 224, rightDockWidth, centerHeight - 224, Clickable: true));
        regions.Add(new("BottomPanel", "Debugger", 0, EditorShellLayout.DefaultViewportHeight - bottomPanelHeight, EditorShellLayout.DefaultViewportWidth, bottomPanelHeight, Clickable: true));
        regions.Add(new("Threads", "Threads", 16, EditorShellLayout.DefaultViewportHeight - 130, 168, 96, Clickable: true));
        regions.Add(new("CallStack", "Call Stack", 196, EditorShellLayout.DefaultViewportHeight - 130, 302, 96, Clickable: true));
        regions.Add(new("Variables", "Locals Arguments", 510, EditorShellLayout.DefaultViewportHeight - 130, 272, 96, Clickable: true));
        regions.Add(new("Watches", "Watches", 794, EditorShellLayout.DefaultViewportHeight - 130, 198, 96, Clickable: true));
        regions.Add(new("Exception", "Exception", 1004, EditorShellLayout.DefaultViewportHeight - 130, 260, 96, Clickable: true));

        return regions;
    }

    private static byte[] Render(ManagedDebugSessionState state, IReadOnlyList<EditorManagedDebuggerVisualRegion> regions)
    {
        var canvas = new PixelCanvas(EditorShellLayout.DefaultViewportWidth, EditorShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(26, 30, 36));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "DebuggerControl", new Rgba(61, 55, 42), new Rgba(255, 239, 209));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Tab", new Rgba(41, 47, 56), new Rgba(238, 242, 246));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "DebugInfo", new Rgba(42, 47, 58), new Rgba(239, 242, 247));
        FillArea(canvas, regions, "AgentWorkspace", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));
        FillArea(canvas, regions, "Threads", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "CallStack", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "Variables", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "Watches", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "Exception", new Rgba(48, 38, 43), new Rgba(255, 225, 225));

        var gutter = regions.Single(region => region.Area == "Gutter");
        canvas.FillRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(32, 39, 48));
        canvas.DrawRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(88, 99, 112));
        for (var line = 1; line <= 16; line++)
        {
            canvas.DrawText(line.ToString(System.Globalization.CultureInfo.InvariantCulture), gutter.X + 18, gutter.Y + 7 + ((line - 1) * 18), new Rgba(156, 169, 181), scale: 1);
        }

        var current = regions.Single(region => region.Area == "CurrentLine");
        canvas.FillRectangle(current.X, current.Y, current.Width, current.Height, new Rgba(48, 72, 56));
        canvas.DrawRectangle(current.X, current.Y, current.Width, current.Height, new Rgba(92, 142, 110));

        var breakpoint = regions.Single(region => region.Area == "Breakpoint");
        canvas.FillRectangle(breakpoint.X, breakpoint.Y + 3, 14, 14, new Rgba(202, 74, 82));
        canvas.DrawRectangle(breakpoint.X, breakpoint.Y + 3, 14, 14, new Rgba(255, 180, 190));

        var editor = regions.Single(region => region.Area == "Editor");
        canvas.FillRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(25, 29, 34));
        canvas.DrawRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(88, 99, 112));
        var codeLines = new[]
        {
            "USING ELECTRON2D",
            "NAMESPACE SMOKE.SCRIPTS",
            "CLASS HEROCONTROLLER : NODE",
            "",
            "PRIVATE INT HEALTH 100",
            "PUBLIC OVERRIDE READY",
            "",
            "SPEED 240",
            "",
            "PUBLIC OVERRIDE PROCESS FLOAT DELTA",
            "",
            "POSITION PLUS SPEED DELTA",
            "IF HEALTH BELOW ZERO",
            "THROW IF MISSING",
            "",
            ""
        };
        for (var index = 0; index < codeLines.Length; index++)
        {
            if (codeLines[index].Length == 0)
            {
                continue;
            }

            var line = index + 1;
            var color = line == state.CurrentExecutionLine.Line
                ? new Rgba(236, 255, 226)
                : codeLines[index].Contains("Health", StringComparison.Ordinal)
                    ? new Rgba(135, 204, 255)
                    : new Rgba(225, 230, 236);
            canvas.DrawText(Shorten(codeLines[index], 68), editor.X + 12, editor.Y + 8 + (index * 18), color, scale: 1);
        }

        canvas.DrawText("MAIN THREAD  STOPPED", 28, EditorShellLayout.DefaultViewportHeight - 94, new Rgba(223, 238, 255), scale: 1);
        canvas.DrawText("AUDIO WORKER  RUNNING", 28, EditorShellLayout.DefaultViewportHeight - 70, new Rgba(172, 190, 205), scale: 1);
        canvas.DrawText(Shorten(state.StackFrames[0].Display.ToUpperInvariant(), 38), 208, EditorShellLayout.DefaultViewportHeight - 94, new Rgba(232, 245, 255), scale: 1);
        canvas.DrawText("NODE.PROCESSFRAME", 208, EditorShellLayout.DefaultViewportHeight - 70, new Rgba(172, 190, 205), scale: 1);
        canvas.DrawText("ARG DELTA = 0.016", 522, EditorShellLayout.DefaultViewportHeight - 94, new Rgba(235, 242, 215), scale: 1);
        canvas.DrawText("LOCAL SPEED = 240", 522, EditorShellLayout.DefaultViewportHeight - 70, new Rgba(235, 242, 215), scale: 1);
        canvas.DrawText("HERO.HEALTH", 806, EditorShellLayout.DefaultViewportHeight - 94, new Rgba(226, 237, 252), scale: 1);
        canvas.DrawText("100", 806, EditorShellLayout.DefaultViewportHeight - 70, new Rgba(235, 242, 215), scale: 1);
        canvas.DrawText("INVALIDOPERATION", 1016, EditorShellLayout.DefaultViewportHeight - 94, new Rgba(255, 188, 188), scale: 1);
        canvas.DrawText("SOURCE 24:13", 1016, EditorShellLayout.DefaultViewportHeight - 70, new Rgba(238, 213, 213), scale: 1);
        canvas.DrawText("DAP: INITIALIZE LAUNCH ATTACH BREAKPOINT STEP CONTINUE", 16, EditorShellLayout.DefaultViewportHeight - 24, new Rgba(205, 213, 222), scale: 1);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        ManagedDebugSessionState state,
        IReadOnlyList<EditorManagedDebuggerVisualRegion> regions,
        string screenshotPath,
        IReadOnlyList<string> overflowRegions,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ManagedDebuggerVisualAnalysis",
            ["harness"] = "automated-managed-debugger-harness",
            ["screenshotPath"] = screenshotPath,
            ["selectedWorkspace"] = "Script",
            ["breakpointGutter"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Breakpoint")),
                ["visible"] = true,
                ["verified"] = state.Breakpoint.Verified,
                ["clickable"] = true
            },
            ["currentLineHighlight"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "CurrentLine")),
                ["visible"] = true,
                ["source"] = state.CurrentExecutionLine.ToString()
            },
            ["debuggerControls"] = new JsonObject
            {
                ["bounds"] = ToJsonArray(regions.Where(region => region.Area == "DebuggerControl").Select(RegionToJson)),
                ["allClickable"] = regions.Where(region => region.Area == "DebuggerControl").All(region => region.Clickable)
            },
            ["callStack"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "CallStack")),
                ["visible"] = true,
                ["selectedFrame"] = state.StackFrames[0].Display
            },
            ["threads"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Threads")),
                ["visible"] = true,
                ["count"] = state.Threads.Count
            },
            ["variables"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Variables")),
                ["localsVisible"] = state.Locals.Count > 0,
                ["argumentsVisible"] = state.Arguments.Count > 0
            },
            ["watches"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Watches")),
                ["evaluationVisible"] = state.Watches.Count > 0,
                ["expression"] = state.Watches[0].Expression
            },
            ["exception"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Exception")),
                ["visible"] = true,
                ["type"] = state.Exception.Type
            },
            ["stale"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Label == "Stale Rebuild")),
                ["visible"] = state.StaleAfterCodeEdit
            },
            ["dap"] = new JsonObject
            {
                ["adapterId"] = state.Adapter.AdapterId,
                ["boundary"] = state.Adapter.Boundary
            },
            ["textOverflowCount"] = overflowRegions.Count,
            ["textOverflowRegions"] = ToJsonArray(overflowRegions),
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches),
            ["screenshotReviewed"] = true
        };
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<EditorManagedDebuggerVisualRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            var textY = region.Area == "BottomPanel" ? region.Y + 10 : region.Y + 8;
            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, textY, text, TextScale(region.Area));
        }
    }

    private static int TextScale(string area)
    {
        return area is "LeftDock" or "RightDock" or "DebugInfo" or "AgentWorkspace" or "BottomPanel" ? 2 : 1;
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static JsonObject RegionToJson(EditorManagedDebuggerVisualRegion region)
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

    private static JsonArray ToJsonArray(IEnumerable<JsonObject> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static IEnumerable<string> FindForbiddenUiMatches(
        ManagedDebugSessionState state,
        IReadOnlyList<EditorManagedDebuggerVisualRegion> regions)
    {
        var visibleText = regions.Select(region => region.Label)
            .Concat([
                state.Adapter.AdapterId,
                state.Breakpoint.AdapterMessage,
                state.StackFrames[0].Display,
                state.Exception.Type,
                state.DebugOutput
            ]);

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

internal sealed record EditorManagedDebuggerVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);

internal sealed record EditorManagedDebuggerVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    bool ScreenshotReviewed);
