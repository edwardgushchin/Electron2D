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

namespace Electron2D.Editor.Shell;

internal static class EditorShellSmoke
{
    public static EditorShellSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);

        var layout = EditorShellLayout.CreateDefault();
        layout.SwitchWorkspace("Script");
        layout.SwitchWorkspace("Game");
        layout.SwitchWorkspace("Tasks");

        layout.ToggleBottomPanel();
        var collapsed = layout.BottomPanelCollapsed;
        layout.ToggleBottomPanel();
        var bottomPanelCollapseRoundTrip = collapsed && !layout.BottomPanelCollapsed;

        var statePath = Path.Combine(fullWorkRoot, "editor-shell-layout.state.json");
        EditorShellLayoutPersistence.Save(statePath, layout.CaptureState());
        var persistenceRoundTripStable = EditorShellLayoutPersistence.IsRoundTripStable(statePath);
        var restored = EditorShellLayout.FromState(EditorShellLayoutPersistence.Load(statePath));
        var workspaceStateRoundTripStable = HasStableWorkspaceState(restored);

        var visual = EditorShellVisualHarness.WriteArtifacts(restored, Path.Combine(fullWorkRoot, "visual"));
        var twoDState = restored.GetWorkspaceState("2D");

        return new EditorShellSmokeResult(
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
            visual.ScreenshotReviewed,
            twoDState.Selection,
            FormatScroll(twoDState),
            twoDState.Zoom.ToString("0.##", CultureInfo.InvariantCulture),
            restored.GetWorkspaceState("Script").OpenDocuments,
            restored.GetWorkspaceState("Game").OpenDocuments,
            restored.GetWorkspaceState("Tasks").OpenDocuments,
            statePath,
            visual.ScreenshotPath,
            visual.AnalysisPath);
    }

    private static bool HasStableWorkspaceState(EditorShellLayout layout)
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

    private static string FormatScroll(EditorShellWorkspaceState state)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{state.ScrollX},{state.ScrollY}");
    }
}
