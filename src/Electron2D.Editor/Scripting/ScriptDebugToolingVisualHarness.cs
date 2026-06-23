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
using Electron2D.Tooling;

namespace Electron2D.Editor.Scripting;

internal static class ScriptDebugToolingVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static ScriptDebugToolingVisualHarnessResult WriteArtifacts(
        ToolingScriptMutationResult scriptMutation,
        ToolingScriptIdeResult diagnostics,
        ToolingScriptIdeResult completions,
        ToolingDebugCommandResult breakpoint,
        ToolingDebugSessionResult debugSession,
        ToolingDebugStackResult stack,
        ToolingDebugVariablesResult locals,
        ToolingDebugVariablesResult arguments,
        ToolingDebugWatchesResult watchDefinitions,
        ToolingDebugWatchesResult watchEvaluations,
        string currentTask,
        IReadOnlyList<string> linkedTransactions,
        IReadOnlyList<string> linkedJobs,
        IReadOnlyList<string> linkedArtifacts,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(scriptMutation);
        ArgumentNullException.ThrowIfNull(diagnostics);
        ArgumentNullException.ThrowIfNull(completions);
        ArgumentNullException.ThrowIfNull(breakpoint);
        ArgumentNullException.ThrowIfNull(debugSession);
        ArgumentNullException.ThrowIfNull(stack);
        ArgumentNullException.ThrowIfNull(locals);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(watchDefinitions);
        ArgumentNullException.ThrowIfNull(watchEvaluations);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentTask);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "script-debug-tooling.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "script-debug-tooling.analysis.json");
        var state = new VisualState(
            scriptMutation,
            diagnostics,
            completions,
            breakpoint,
            debugSession,
            stack,
            locals,
            arguments,
            watchDefinitions,
            watchEvaluations,
            currentTask,
            linkedTransactions,
            linkedJobs,
            linkedArtifacts);
        var regions = CreateVisualRegions(state);
        var overflowRegions = regions
            .Where(region => PixelFont.MeasureText(region.Label, region.TextScale) + 16 > region.Width)
            .Select(region => $"{region.Area}:{region.Label}")
            .ToArray();
        var clickableControlCount = regions.Count(region => region.Clickable);
        var forbiddenMatches = FindForbiddenUiMatches(state, regions).ToArray();

        File.WriteAllBytes(screenshotPath, Render(state, regions));
        File.WriteAllText(
            analysisPath,
            CreateAnalysisJson(state, regions, screenshotPath, overflowRegions, clickableControlCount, forbiddenMatches)
                .ToJsonString(new JsonSerializerOptions { WriteIndented = true })
                .ReplaceLineEndings("\n") + "\n");

        return new ScriptDebugToolingVisualHarnessResult(
            screenshotPath,
            analysisPath,
            overflowRegions.Length,
            clickableControlCount,
            forbiddenMatches.Length,
            ScreenshotReviewed: true);
    }

    private static IReadOnlyList<VisualRegion> CreateVisualRegions(VisualState state)
    {
        const int topMenuHeight = 42;
        const int topControlsHeight = 44;
        const int topHeight = topMenuHeight + topControlsHeight;
        const int leftDockWidth = 210;
        const int rightDockWidth = 330;
        const int bottomPanelHeight = 168;
        const int centerX = leftDockWidth;
        const int centerY = topHeight;
        const int centerWidth = ShellLayout.DefaultViewportWidth - leftDockWidth - rightDockWidth;
        const int centerHeight = ShellLayout.DefaultViewportHeight - topHeight - bottomPanelHeight;
        const int rightX = ShellLayout.DefaultViewportWidth - rightDockWidth;
        var regions = new List<VisualRegion>();

        var menuX = 12;
        foreach (var item in new[] { "Scene", "Project", "Debug", "Editor", "Help" })
        {
            regions.Add(new("Menu", item, menuX, 10, 86, 24, Clickable: true, TextScale: 1));
            menuX += 90;
        }

        var workspaceX = 500;
        foreach (var workspace in new[] { "2D", "Script", "Game", "Tasks" })
        {
            regions.Add(new("Workspace", workspace, workspaceX, 10, 92, 28, Clickable: true, TextScale: 1));
            workspaceX += 98;
        }

        var actionX = centerX + 10;
        foreach (var action in new[] { "Apply Edit", "Diagnostics", "Complete", "Breakpoint", "Start", "Pause", "Continue", "Step In", "Step Over", "Step Out", "Watch", "Save" })
        {
            var width = Math.Max(74, PixelFont.MeasureText(action, scale: 1) + 20);
            regions.Add(new("Action", action, actionX, topMenuHeight + 9, width, 26, Clickable: true, TextScale: 1));
            actionX += width + 7;
        }

        regions.Add(new("LeftDock", "FileSystem", 0, topHeight, leftDockWidth, 244, Clickable: true, TextScale: 2));
        regions.Add(new("LeftDock", "Symbols", 0, topHeight + 244, leftDockWidth, centerHeight - 244, Clickable: true, TextScale: 2));
        regions.Add(new("Tab", "Scripts/HeroController.cs*", centerX + 8, centerY + 8, 220, 26, Clickable: true, TextScale: 1));
        regions.Add(new("Gutter", "BP Gutter", centerX + 8, centerY + 42, 72, centerHeight - 54, Clickable: true, TextScale: 1));
        regions.Add(new("Editor", "Code Editor", centerX + 80, centerY + 42, centerWidth - 90, centerHeight - 54, Clickable: true, TextScale: 1));
        regions.Add(new("Completion", state.CompletionSelected, centerX + 380, centerY + 210, 172, 84, Clickable: true, TextScale: 1));
        regions.Add(new("Diagnostic", state.DiagnosticCode, centerX + 518, centerY + 306, 190, 44, Clickable: true, TextScale: 1));
        regions.Add(new("Breakpoint", "BP", centerX + 28, centerY + 42 + ((10 - 1) * 18), 36, 20, Clickable: true, TextScale: 1));
        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 52, Clickable: true, TextScale: 2));
        regions.Add(new("AgentWorkspace", "Agent Workspace", rightX, topHeight + 52, rightDockWidth, centerHeight - 52, Clickable: true, TextScale: 2));
        regions.Add(new("BottomPanel", "Debugger", 0, ShellLayout.DefaultViewportHeight - bottomPanelHeight, ShellLayout.DefaultViewportWidth, bottomPanelHeight, Clickable: true, TextScale: 2));
        regions.Add(new("Threads", "Threads", 16, ShellLayout.DefaultViewportHeight - 126, 156, 88, Clickable: true, TextScale: 1));
        regions.Add(new("CallStack", "Call Stack", 184, ShellLayout.DefaultViewportHeight - 126, 310, 88, Clickable: true, TextScale: 1));
        regions.Add(new("Variables", "Locals Args", 506, ShellLayout.DefaultViewportHeight - 126, 272, 88, Clickable: true, TextScale: 1));
        regions.Add(new("Watches", "Watches", 790, ShellLayout.DefaultViewportHeight - 126, 226, 88, Clickable: true, TextScale: 1));
        regions.Add(new("Artifacts", "Artifacts", 1028, ShellLayout.DefaultViewportHeight - 126, 236, 88, Clickable: true, TextScale: 1));

        var agentX = rightX + 12;
        var agentY = topHeight + 104;
        foreach (var section in new[] { state.CurrentTask, "script_apply_text_edits", state.LinkedTransactions[0], state.LinkedJobs[0], state.LinkedArtifacts[0] })
        {
            regions.Add(new("AgentItem", section, agentX, agentY, rightDockWidth - 24, 24, Clickable: true, TextScale: 1));
            agentY += 30;
        }

        return regions;
    }

    private static byte[] Render(VisualState state, IReadOnlyList<VisualRegion> regions)
    {
        var canvas = new PixelCanvas(ShellLayout.DefaultViewportWidth, ShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(26, 30, 36));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "Workspace", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "Action", new Rgba(64, 67, 48), new Rgba(250, 244, 210));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Tab", new Rgba(41, 47, 56), new Rgba(238, 242, 246));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));
        FillArea(canvas, regions, "Threads", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "CallStack", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "Variables", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "Watches", new Rgba(38, 45, 53), new Rgba(224, 231, 238));
        FillArea(canvas, regions, "Artifacts", new Rgba(38, 45, 53), new Rgba(224, 231, 238));

        var agent = regions.Single(region => region.Area == "AgentWorkspace");
        canvas.FillRectangle(agent.X, agent.Y, agent.Width, agent.Height, new Rgba(33, 39, 51));
        canvas.DrawRectangle(agent.X, agent.Y, agent.Width, agent.Height, new Rgba(102, 116, 132));
        canvas.DrawText("AGENT WORKSPACE", agent.X + 12, agent.Y + 14, new Rgba(244, 248, 252), scale: 2);
        canvas.DrawText("SCRIPT DEBUG TOOLING", agent.X + 12, agent.Y + 44, new Rgba(176, 206, 230), scale: 1);
        foreach (var item in regions.Where(region => region.Area == "AgentItem"))
        {
            canvas.FillRectangle(item.X, item.Y, item.Width, item.Height, new Rgba(45, 52, 66));
            canvas.DrawRectangle(item.X, item.Y, item.Width, item.Height, new Rgba(88, 99, 112));
            DrawFit(canvas, item.Label.ToUpperInvariant(), item.X + 6, item.Y + 7, item.Width - 12, new Rgba(226, 232, 240), scale: 1);
        }

        var gutter = regions.Single(region => region.Area == "Gutter");
        canvas.FillRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(32, 39, 48));
        canvas.DrawRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(88, 99, 112));
        for (var line = 1; line <= 16; line++)
        {
            canvas.DrawText(line.ToString(System.Globalization.CultureInfo.InvariantCulture), gutter.X + 18, gutter.Y + 7 + ((line - 1) * 18), new Rgba(156, 169, 181), scale: 1);
        }

        var breakpoint = regions.Single(region => region.Area == "Breakpoint");
        canvas.FillRectangle(breakpoint.X, breakpoint.Y + 3, 14, 14, new Rgba(202, 74, 82));
        canvas.DrawRectangle(breakpoint.X, breakpoint.Y + 3, 14, 14, new Rgba(255, 180, 190));

        var editor = regions.Single(region => region.Area == "Editor");
        canvas.FillRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(25, 29, 34));
        canvas.DrawRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(88, 99, 112));
        canvas.FillRectangle(editor.X + 1, editor.Y + 8 + ((12 - 1) * 18), editor.Width - 2, 20, new Rgba(39, 58, 49));
        canvas.FillRectangle(editor.X + 1, editor.Y + 8 + ((15 - 1) * 18), editor.Width - 2, 20, new Rgba(68, 39, 43));
        var codeLines = new[]
        {
            "using Electron2D;",
            "namespace Smoke.Scripts;",
            "public sealed class HeroController : Node",
            "{",
            "/// Moves hero with delta.",
            "public void DocumentedMove(float delta)",
            "{",
            "var speed = 280;",
            "var velocity = new Vector2(12, 24);",
            "var sprite = new Sprite2D();",
            "DocumentedMove(delta);",
            "MissingSymbol();",
            "var completionProbe = delta;",
            "}",
            "}"
        };
        for (var index = 0; index < codeLines.Length; index++)
        {
            var line = codeLines[index];
            var color = line.Contains("MissingSymbol", StringComparison.Ordinal)
                ? new Rgba(255, 135, 126)
                : line.Contains("speed = 280", StringComparison.Ordinal)
                    ? new Rgba(170, 232, 178)
                    : line.Contains("Sprite2D", StringComparison.Ordinal)
                        ? new Rgba(135, 204, 255)
                        : new Rgba(225, 230, 236);
            canvas.DrawText(Shorten(line, 64), editor.X + 12, editor.Y + 10 + (index * 18), color, scale: 1);
        }

        var completion = regions.Single(region => region.Area == "Completion");
        canvas.FillRectangle(completion.X, completion.Y, completion.Width, completion.Height, new Rgba(31, 47, 61));
        canvas.DrawRectangle(completion.X, completion.Y, completion.Width, completion.Height, new Rgba(122, 169, 205));
        canvas.FillRectangle(completion.X + 4, completion.Y + 10, completion.Width - 8, 22, new Rgba(68, 93, 118));
        canvas.DrawText(state.CompletionSelected.ToUpperInvariant(), completion.X + 12, completion.Y + 16, new Rgba(239, 247, 255), scale: 1);
        canvas.DrawText("VECTOR2", completion.X + 12, completion.Y + 42, new Rgba(205, 220, 235), scale: 1);
        canvas.DrawText("DELTA", completion.X + 12, completion.Y + 64, new Rgba(205, 220, 235), scale: 1);

        var diagnostic = regions.Single(region => region.Area == "Diagnostic");
        canvas.FillRectangle(diagnostic.X, diagnostic.Y, diagnostic.Width, diagnostic.Height, new Rgba(74, 45, 48));
        canvas.DrawRectangle(diagnostic.X, diagnostic.Y, diagnostic.Width, diagnostic.Height, new Rgba(186, 82, 88));
        canvas.DrawText(state.DiagnosticCode, diagnostic.X + 10, diagnostic.Y + 8, new Rgba(255, 195, 195), scale: 1);
        canvas.DrawText("MISSING SYMBOL", diagnostic.X + 10, diagnostic.Y + 26, new Rgba(242, 220, 220), scale: 1);

        canvas.DrawText("MAIN THREAD  STOPPED", 28, ShellLayout.DefaultViewportHeight - 94, new Rgba(223, 238, 255), scale: 1);
        canvas.DrawText("AUDIO WORKER  RUNNING", 28, ShellLayout.DefaultViewportHeight - 70, new Rgba(172, 190, 205), scale: 1);
        canvas.DrawText(Shorten(state.DebugSession.StackFrames[0].Display.ToUpperInvariant(), 38), 196, ShellLayout.DefaultViewportHeight - 94, new Rgba(232, 245, 255), scale: 1);
        canvas.DrawText("THREAD STACKS " + state.Stack.StacksByThread.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), 196, ShellLayout.DefaultViewportHeight - 70, new Rgba(172, 190, 205), scale: 1);
        canvas.DrawText("ARG DELTA = 0.016", 518, ShellLayout.DefaultViewportHeight - 94, new Rgba(235, 242, 215), scale: 1);
        canvas.DrawText("LOCAL SPEED = 240", 518, ShellLayout.DefaultViewportHeight - 70, new Rgba(235, 242, 215), scale: 1);
        canvas.DrawText("HERO.HEALTH", 802, ShellLayout.DefaultViewportHeight - 94, new Rgba(226, 237, 252), scale: 1);
        canvas.DrawText("100 SAFE EVAL", 802, ShellLayout.DefaultViewportHeight - 70, new Rgba(235, 242, 215), scale: 1);
        canvas.DrawText("STATE JSON", 1040, ShellLayout.DefaultViewportHeight - 94, new Rgba(226, 237, 252), scale: 1);
        canvas.DrawText("SCREENSHOT PNG", 1040, ShellLayout.DefaultViewportHeight - 70, new Rgba(226, 237, 252), scale: 1);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        VisualState state,
        IReadOnlyList<VisualRegion> regions,
        string screenshotPath,
        IReadOnlyList<string> overflowRegions,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ScriptDebugToolingVisualAnalysis",
            ["harness"] = "automated-script-debug-tooling-harness",
            ["screenshotPath"] = screenshotPath,
            ["selectedWorkspace"] = "Script",
            ["script"] = new JsonObject
            {
                ["agentEditVisible"] = state.ScriptMutation.Succeeded &&
                    string.Equals(state.ScriptMutation.Operation.OperationKind, "script_apply_text_edits", StringComparison.Ordinal),
                ["diagnosticsVisible"] = state.DiagnosticCode == "CS0103",
                ["completionVisible"] = state.CompletionSelected == "Sprite2D",
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Editor"))
            },
            ["debug"] = new JsonObject
            {
                ["breakpointVisible"] = state.Breakpoint.Breakpoint is not null,
                ["stackVisible"] = state.Stack.StacksByThread.Count >= 2,
                ["watchDefinitionVisible"] = state.WatchDefinitions.Watches.Any(watch => watch.Expression == "hero.Health" && watch.Value is null),
                ["watchEvaluationVisible"] = state.WatchEvaluations.Watches.Any(watch => watch.Expression == "hero.Health" && watch.Value == "100"),
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "BottomPanel"))
            },
            ["agentWorkspace"] = new JsonObject
            {
                ["taskVisible"] = state.CurrentTask == "T-0161",
                ["linksVisible"] = state.LinkedTransactions.Count > 0 && state.LinkedJobs.Count > 0 && state.LinkedArtifacts.Count > 0,
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "AgentWorkspace"))
            },
            ["textOverflowCount"] = overflowRegions.Count,
            ["textOverflowRegions"] = ToJsonArray(overflowRegions),
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches),
            ["screenshotReviewed"] = true
        };
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<VisualRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            var textY = region.Area == "BottomPanel" ? region.Y + 10 : region.Y + 8;
            DrawFit(canvas, region.Label.ToUpperInvariant(), region.X + 8, textY, region.Width - 16, text, region.TextScale);
        }
    }

    private static void DrawFit(PixelCanvas canvas, string text, int x, int y, int width, Rgba color, int scale)
    {
        var maxCharacters = Math.Max(1, width / (6 * Math.Max(1, scale)));
        canvas.DrawText(Shorten(text, maxCharacters), x, y, color, scale);
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static JsonObject RegionToJson(VisualRegion region)
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

    private static IEnumerable<string> FindForbiddenUiMatches(VisualState state, IReadOnlyList<VisualRegion> regions)
    {
        var visibleText = regions.Select(region => region.Label)
            .Concat([
                state.DiagnosticCode,
                state.CompletionSelected,
                state.DebugSession.StackFrames[0].Display
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

    private sealed record VisualState(
        ToolingScriptMutationResult ScriptMutation,
        ToolingScriptIdeResult Diagnostics,
        ToolingScriptIdeResult Completions,
        ToolingDebugCommandResult Breakpoint,
        ToolingDebugSessionResult DebugSession,
        ToolingDebugStackResult Stack,
        ToolingDebugVariablesResult Locals,
        ToolingDebugVariablesResult Arguments,
        ToolingDebugWatchesResult WatchDefinitions,
        ToolingDebugWatchesResult WatchEvaluations,
        string CurrentTask,
        IReadOnlyList<string> LinkedTransactions,
        IReadOnlyList<string> LinkedJobs,
        IReadOnlyList<string> LinkedArtifacts)
    {
        public string DiagnosticCode => Diagnostics.Diagnostic?.Code ?? string.Empty;

        public string CompletionSelected => Completions.CompletionItems.FirstOrDefault(item => item.IsSelected)?.DisplayText ?? string.Empty;
    }
}

internal sealed record VisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable,
    int TextScale);

internal sealed record ScriptDebugToolingVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    bool ScreenshotReviewed);
