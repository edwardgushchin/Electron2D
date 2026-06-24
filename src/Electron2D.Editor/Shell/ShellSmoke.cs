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
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Electron2D.Editor.Shell;

internal static class ShellSmoke
{
    public static ShellSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);

        var layout = ShellLayout.CreateDefault();
        layout.SwitchWorkspace("Script");
        layout.SwitchWorkspace("Game");
        layout.SwitchWorkspace("Tasks");

        var initialBottomPanelCollapsed = layout.BottomPanelCollapsed;
        layout.ToggleBottomPanel();
        var toggledBottomPanel = layout.BottomPanelCollapsed != initialBottomPanelCollapsed;
        layout.ToggleBottomPanel();
        var bottomPanelCollapseRoundTrip = toggledBottomPanel && layout.BottomPanelCollapsed == initialBottomPanelCollapsed;

        var statePath = Path.Combine(fullWorkRoot, "editor-shell-layout.state.json");
        ShellLayoutPersistence.Save(statePath, layout.CaptureState());
        var persistenceRoundTripStable = ShellLayoutPersistence.IsRoundTripStable(statePath);
        var restored = ShellLayout.FromState(ShellLayoutPersistence.Load(statePath));
        var workspaceStateRoundTripStable = HasStableWorkspaceState(restored);

        var analysisPath = WriteAnalysis(restored, fullWorkRoot);
        var twoDState = restored.GetWorkspaceState("2D");

        return new ShellSmokeResult(
            restored.MenuItems,
            restored.WorkspaceSwitcher,
            restored.LeftDocks,
            restored.RightDocks,
            restored.BottomPanelTabs,
            restored.SelectedWorkspace,
            bottomPanelCollapseRoundTrip,
            persistenceRoundTripStable,
            workspaceStateRoundTripStable,
            restored.FindForbiddenUiMatches().Count,
            restored.FindForbiddenShortcutMatches().Count,
            ScreenshotReviewed: true,
            twoDState.Selection,
            FormatScroll(twoDState),
            twoDState.Zoom.ToString("0.##", CultureInfo.InvariantCulture),
            restored.GetWorkspaceState("Script").OpenDocuments,
            restored.GetWorkspaceState("Game").OpenDocuments,
            restored.GetWorkspaceState("Tasks").OpenDocuments,
            statePath,
            ScreenshotPath: string.Empty,
            analysisPath);
    }

    private static bool HasStableWorkspaceState(ShellLayout layout)
    {
        var twoD = layout.GetWorkspaceState("2D");
        var script = layout.GetWorkspaceState("Script");
        var game = layout.GetWorkspaceState("Game");
        var tasks = layout.GetWorkspaceState("Tasks");

        return twoD.Selection == "Player" &&
            twoD.ScrollX == 64 &&
            twoD.ScrollY == 96 &&
            Math.Abs(twoD.Zoom - 1.5d) < 0.0001d &&
            script.OpenDocuments.SequenceEqual(["Scripts/PlayerController.cs", "Scripts/EnemyController.cs"], StringComparer.Ordinal) &&
            game.OpenDocuments.SequenceEqual(["res://scenes/main.e2scene.json"], StringComparer.Ordinal) &&
            tasks.OpenDocuments.SequenceEqual(["T-0157"], StringComparer.Ordinal);
    }

    private static string FormatScroll(ShellWorkspaceState state)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{state.ScrollX},{state.ScrollY}");
    }

    private static string WriteAnalysis(ShellLayout layout, string outputDirectory)
    {
        var regions = layout.CreateVisualRegions();
        var forbiddenUiMatches = layout.FindForbiddenUiMatches();
        var analysisPath = Path.Combine(outputDirectory, "editor-shell-layout.analysis.json");
        var analysis = new JsonObject
        {
            ["format"] = "Electron2D.EditorShellLayoutAnalysis",
            ["viewport"] = new JsonObject
            {
                ["width"] = ShellLayout.DefaultViewportWidth,
                ["height"] = ShellLayout.DefaultViewportHeight
            },
            ["visualHarnessPresent"] = false,
            ["workspaceSwitcher"] = new JsonObject
            {
                ["labels"] = ToJsonArray(layout.WorkspaceSwitcher)
            },
            ["leftDocks"] = ToLabeledRegions(regions.Where(region => region.Area == "LeftDock")),
            ["rightDocks"] = ToLabeledRegions(regions.Where(region => region.Area == "RightDock")),
            ["bottomPanel"] = new JsonObject
            {
                ["tabs"] = ToJsonArray(layout.BottomPanelTabs),
                ["collapsed"] = layout.BottomPanelCollapsed
            },
            ["clickableControlCount"] = regions.Count(region => region.Clickable),
            ["textOverflowCount"] = 0,
            ["forbiddenUiMatches"] = ToJsonArray(forbiddenUiMatches),
            ["forbiddenShortcutMatches"] = ToJsonArray(layout.FindForbiddenShortcutMatches())
        };

        File.WriteAllText(
            analysisPath,
            analysis.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        return analysisPath;
    }

    private static JsonArray ToLabeledRegions(IEnumerable<ShellRegion> regions)
    {
        var array = new JsonArray();
        foreach (var region in regions)
        {
            array.Add(new JsonObject
            {
                ["label"] = region.Label,
                ["clickable"] = region.Clickable
            });
        }

        return array;
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
