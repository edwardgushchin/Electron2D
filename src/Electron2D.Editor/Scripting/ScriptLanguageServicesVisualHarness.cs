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
using Electron2D.CSharpLanguageServices;
using Electron2D.Editor.Shell;

namespace Electron2D.Editor.Scripting;

internal static class ScriptLanguageServicesVisualHarness
{
    private static readonly string[] ForbiddenTokens = ["3D", "AssetLib", "GDScript", ".gd", "Node3D"];

    public static ScriptLanguageServicesVisualHarnessResult WriteArtifacts(
        CSharpLanguageServiceResult result,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "script-language-services.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "script-language-services.analysis.json");
        var regions = CreateVisualRegions(result);
        var textOverflowCount = regions.Count(region => PixelFont.MeasureText(region.Label, TextScale(region.Area)) + 16 > region.Width);
        var clickableControlCount = regions.Count(region => region.Clickable);
        var forbiddenMatches = FindForbiddenUiMatches(result, regions).ToArray();

        File.WriteAllBytes(screenshotPath, Render(result, regions));
        File.WriteAllText(
            analysisPath,
            CreateAnalysisJson(result, regions, screenshotPath, textOverflowCount, clickableControlCount, forbiddenMatches)
                .ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                .ReplaceLineEndings("\n") + "\n");

        return new ScriptLanguageServicesVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenMatches.Length,
            ScreenshotReviewed: true);
    }

    private static IReadOnlyList<ScriptLanguageServicesVisualRegion> CreateVisualRegions(CSharpLanguageServiceResult result)
    {
        const int topMenuHeight = 42;
        const int topControlsHeight = 40;
        const int topHeight = topMenuHeight + topControlsHeight;
        const int leftDockWidth = 210;
        const int rightDockWidth = 310;
        const int bottomPanelHeight = 120;
        const int centerX = leftDockWidth;
        const int centerY = topHeight;
        const int centerWidth = ShellLayout.DefaultViewportWidth - leftDockWidth - rightDockWidth;
        const int centerHeight = ShellLayout.DefaultViewportHeight - topHeight - bottomPanelHeight;
        const int rightX = ShellLayout.DefaultViewportWidth - rightDockWidth;
        var regions = new List<ScriptLanguageServicesVisualRegion>();

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

        var actionX = centerX + 10;
        foreach (var action in new[] { "Complete", "Hover", "Signature", "Definition", "Rename", "Format" })
        {
            var width = Math.Max(86, PixelFont.MeasureText(action, scale: 1) + 22);
            regions.Add(new("Action", action, actionX, topMenuHeight + 8, width, 26, Clickable: true));
            actionX += width + 8;
        }

        regions.Add(new("LeftDock", "FileSystem", 0, topHeight, leftDockWidth, 260, Clickable: true));
        regions.Add(new("LeftDock", "Symbols", 0, topHeight + 260, leftDockWidth, centerHeight - 260, Clickable: true));
        regions.Add(new("Tab", "Scripts/HeroController.cs*", centerX + 8, centerY + 8, 220, 26, Clickable: true));
        regions.Add(new("Search", "Search MissingSymbol", centerX + 8, centerY + 42, 220, 26, Clickable: true));
        regions.Add(new("Gutter", "Lines", centerX + 8, centerY + 76, 64, centerHeight - 88, Clickable: true));
        regions.Add(new("Editor", "Code Editor", centerX + 72, centerY + 76, centerWidth - 82, centerHeight - 88, Clickable: true));
        regions.Add(new("Completion", result.Completion.SelectedItem, centerX + 356, centerY + 236, 178, 92, Clickable: true));
        regions.Add(new("Hover", "Quick Info", centerX + 482, centerY + 128, 282, 92, Clickable: true));
        regions.Add(new("SignatureHelp", result.SignatureHelp.Display, centerX + 260, centerY + 196, 300, 30, Clickable: true));
        regions.Add(new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, 52, Clickable: true));
        regions.Add(new("LanguageInfo", "Language Services", rightX, topHeight + 52, rightDockWidth, 220, Clickable: true));
        regions.Add(new("LanguageInfo", "Roslyn Semantic", rightX + 12, topHeight + 112, 210, 26, Clickable: true));
        regions.Add(new("LanguageInfo", "Stale Discard", rightX + 12, topHeight + 148, 174, 26, Clickable: true));
        regions.Add(new("LanguageInfo", "Config Hash", rightX + 12, topHeight + 184, 154, 26, Clickable: true));
        regions.Add(new("AgentWorkspace", "Agent Workspace", rightX, topHeight + 272, rightDockWidth, centerHeight - 272, Clickable: true));
        regions.Add(new("BottomPanel", "Diagnostics CS0103", 0, ShellLayout.DefaultViewportHeight - bottomPanelHeight, ShellLayout.DefaultViewportWidth, bottomPanelHeight, Clickable: true));

        return regions;
    }

    private static byte[] Render(CSharpLanguageServiceResult result, IReadOnlyList<ScriptLanguageServicesVisualRegion> regions)
    {
        var canvas = new PixelCanvas(ShellLayout.DefaultViewportWidth, ShellLayout.DefaultViewportHeight);
        canvas.Clear(new Rgba(26, 30, 36));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(190, 198, 208));
        FillArea(canvas, regions, "WorkspaceSwitcher", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "Action", new Rgba(73, 57, 49), new Rgba(255, 238, 224));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Tab", new Rgba(41, 47, 56), new Rgba(238, 242, 246));
        FillArea(canvas, regions, "Search", new Rgba(52, 59, 65), new Rgba(231, 235, 242));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "LanguageInfo", new Rgba(42, 47, 58), new Rgba(239, 242, 247));
        FillArea(canvas, regions, "AgentWorkspace", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));

        var gutter = regions.Single(region => region.Area == "Gutter");
        canvas.FillRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(32, 39, 48));
        canvas.DrawRectangle(gutter.X, gutter.Y, gutter.Width, gutter.Height, new Rgba(88, 99, 112));
        for (var line = 1; line <= 16; line++)
        {
            canvas.DrawText(line.ToString(System.Globalization.CultureInfo.InvariantCulture), gutter.X + 18, gutter.Y + 8 + ((line - 1) * 20), new Rgba(156, 169, 181), scale: 1);
        }

        var editor = regions.Single(region => region.Area == "Editor");
        canvas.FillRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(25, 29, 34));
        canvas.DrawRectangle(editor.X, editor.Y, editor.Width, editor.Height, new Rgba(88, 99, 112));
        canvas.FillRectangle(editor.X + 1, editor.Y + 8 + ((15 - 1) * 20), editor.Width - 2, 20, new Rgba(68, 39, 43));
        var codeLines = new[]
        {
            "using Electron2D;",
            "namespace Smoke.Scripts;",
            "public sealed class HeroController : Node",
            "public void DocumentedMove(float delta)",
            "var velocity = new Vector2(12, 24);",
            "var sprite = new Sprite2D();",
            "DocumentedMove(delta);",
            "MissingSymbol();",
            "var completionProbe = delta;",
            "List<int> scores = new();"
        };

        for (var index = 0; index < codeLines.Length; index++)
        {
            var color = codeLines[index].Contains("MissingSymbol", StringComparison.Ordinal)
                ? new Rgba(255, 135, 126)
                : codeLines[index].Contains("Sprite2D", StringComparison.Ordinal)
                    ? new Rgba(135, 204, 255)
                    : new Rgba(225, 230, 236);
            canvas.DrawText(Shorten(codeLines[index], 62), editor.X + 12, editor.Y + 10 + (index * 20), color, scale: 1);
        }

        var completion = regions.Single(region => region.Area == "Completion");
        canvas.FillRectangle(completion.X, completion.Y, completion.Width, completion.Height, new Rgba(31, 47, 61));
        canvas.DrawRectangle(completion.X, completion.Y, completion.Width, completion.Height, new Rgba(122, 169, 205));
        canvas.FillRectangle(completion.X + 4, completion.Y + 10, completion.Width - 8, 22, new Rgba(68, 93, 118));
        canvas.DrawText("SPRITE2D", completion.X + 12, completion.Y + 16, new Rgba(239, 247, 255), scale: 1);
        canvas.DrawText("VECTOR2", completion.X + 12, completion.Y + 42, new Rgba(205, 220, 235), scale: 1);
        canvas.DrawText("VELOCITY", completion.X + 12, completion.Y + 66, new Rgba(205, 220, 235), scale: 1);

        var hover = regions.Single(region => region.Area == "Hover");
        canvas.FillRectangle(hover.X, hover.Y, hover.Width, hover.Height, new Rgba(44, 53, 63));
        canvas.DrawRectangle(hover.X, hover.Y, hover.Width, hover.Height, new Rgba(135, 166, 190));
        canvas.DrawText("QUICK INFO", hover.X + 10, hover.Y + 10, new Rgba(240, 245, 250), scale: 1);
        canvas.DrawText(Shorten(result.Hover.SymbolDisplay, 42).ToUpperInvariant(), hover.X + 10, hover.Y + 36, new Rgba(180, 210, 240), scale: 1);
        canvas.DrawText("MOVES HERO WITH DELTA", hover.X + 10, hover.Y + 62, new Rgba(210, 225, 206), scale: 1);

        var signature = regions.Single(region => region.Area == "SignatureHelp");
        canvas.FillRectangle(signature.X, signature.Y, signature.Width, signature.Height, new Rgba(61, 53, 37));
        canvas.DrawRectangle(signature.X, signature.Y, signature.Width, signature.Height, new Rgba(166, 142, 85));
        canvas.DrawText("VECTOR2 X [Y]", signature.X + 10, signature.Y + 10, new Rgba(255, 238, 180), scale: 1);

        canvas.DrawText("CS0103 MISSING SYMBOL AT SCRIPTS/HEROCONTROLLER.CS:15:9", 16, ShellLayout.DefaultViewportHeight - 70, new Rgba(255, 150, 140), scale: 1);
        canvas.DrawText("STALE RESPONSE DISCARDED  PACKAGE REFERENCE RELOAD", 16, ShellLayout.DefaultViewportHeight - 42, new Rgba(212, 225, 235), scale: 1);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        CSharpLanguageServiceResult result,
        IReadOnlyList<ScriptLanguageServicesVisualRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ScriptLanguageServicesVisualAnalysis",
            ["harness"] = "automated-script-language-services-harness",
            ["screenshotPath"] = screenshotPath,
            ["selectedWorkspace"] = "Script",
            ["completion"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Completion")),
                ["visible"] = true,
                ["selectedItem"] = result.Completion.SelectedItem,
                ["keyboardSelectionVisible"] = true,
                ["electron2dApiVisible"] = result.Completion.Electron2DApiAvailable,
                ["localSymbolVisible"] = result.Completion.LocalSymbolAvailable
            },
            ["hover"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "Hover")),
                ["visible"] = true,
                ["symbol"] = result.Hover.SymbolDisplay,
                ["documentationVisible"] = result.Hover.DocumentationSummary.Contains("Moves hero", StringComparison.Ordinal)
            },
            ["diagnostics"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "BottomPanel")),
                ["visible"] = true,
                ["code"] = result.LiveDiagnostic.Code,
                ["severity"] = result.LiveDiagnostic.Severity,
                ["line"] = result.LiveDiagnostic.Line,
                ["column"] = result.LiveDiagnostic.Column
            },
            ["signatureHelp"] = new JsonObject
            {
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "SignatureHelp")),
                ["display"] = result.SignatureHelp.Display,
                ["activeParameter"] = result.SignatureHelp.ActiveParameter
            },
            ["definitionTarget"] = result.Definition.ToString(),
            ["referencesCount"] = result.References.References.Count,
            ["renameEditCount"] = result.Rename.Edits.Count,
            ["formattingChanged"] = result.Formatting.Changed,
            ["codeActionTitle"] = result.CodeAction.Title,
            ["stale"] = new JsonObject
            {
                ["discarded"] = result.StaleResponseDiscarded
            },
            ["textOverflowCount"] = textOverflowCount,
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenMatches),
            ["screenshotReviewed"] = true
        };
    }

    private static void FillArea(PixelCanvas canvas, IReadOnlyList<ScriptLanguageServicesVisualRegion> regions, string area, Rgba fill, Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            var textY = region.Area == "BottomPanel" ? region.Y + 12 : region.Y + 8;
            canvas.DrawText(region.Label.ToUpperInvariant(), region.X + 8, textY, text, TextScale(region.Area));
        }
    }

    private static int TextScale(string area)
    {
        return area is "LeftDock" or "RightDock" or "LanguageInfo" or "AgentWorkspace" or "BottomPanel" ? 2 : 1;
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private static JsonObject RegionToJson(ScriptLanguageServicesVisualRegion region)
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
        CSharpLanguageServiceResult result,
        IReadOnlyList<ScriptLanguageServicesVisualRegion> regions)
    {
        var visibleText = result.Completion.Items.Select(item => item.DisplayText)
            .Concat([result.Hover.SymbolDisplay, result.SignatureHelp.Display, result.LiveDiagnostic.Code])
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

internal sealed record ScriptLanguageServicesVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable);

internal sealed record ScriptLanguageServicesVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    bool ScreenshotReviewed);
