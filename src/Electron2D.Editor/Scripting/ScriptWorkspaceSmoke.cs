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

namespace Electron2D.Editor.Scripting;

internal static class ScriptWorkspaceSmoke
{
    public static ScriptWorkspaceSmokeResult Run(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        Directory.CreateDirectory(fullWorkRoot);

        var snapshot = ScriptWorkspaceView.CreateSmokeSnapshot();
        var view = new ScriptWorkspaceView(snapshot);
        var statePath = Path.Combine(fullWorkRoot, "script-workspace.state.json");
        File.WriteAllText(
            statePath,
            WriteState(view.Snapshot).ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        var visual = ScriptWorkspaceVisualHarness.WriteArtifacts(
            view.Snapshot,
            Path.Combine(fullWorkRoot, "visual"));

        return new ScriptWorkspaceSmokeResult(
            view.Snapshot,
            statePath,
            visual.ScreenshotPath,
            visual.AnalysisPath,
            visual.TextOverflowCount,
            visual.ClickableControlCount,
            visual.ForbiddenUiMatchCount,
            visual.ScreenshotReviewed);
    }

    private static JsonObject WriteState(ScriptWorkspaceSnapshot snapshot)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.ScriptWorkspaceState",
            ["selectedWorkspace"] = snapshot.SelectedWorkspace,
            ["workspaceSwitcher"] = ToJsonArray(snapshot.WorkspaceSwitcher),
            ["prerequisiteManifestClosed"] = snapshot.PrerequisiteManifestClosed,
            ["prerequisiteManifest"] = ToJsonArray(snapshot.PrerequisiteManifest),
            ["openTabs"] = ToJsonArray(snapshot.DisplayTabs),
            ["activeDocument"] = new JsonObject
            {
                ["documentId"] = snapshot.ActiveDocument.DocumentId,
                ["path"] = snapshot.ActiveDocument.Path,
                ["revision"] = snapshot.ActiveDocument.Revision.Value,
                ["persistedRevision"] = snapshot.ActiveDocument.PersistedRevision.Value,
                ["dirty"] = snapshot.ActiveDocument.IsDirty,
                ["semanticVersion"] = snapshot.ActiveDocument.SemanticVersion
            },
            ["snapshotIdentity"] = new JsonObject
            {
                ["inputSnapshotId"] = snapshot.SnapshotIdentity.InputSnapshotId,
                ["inputWorkspaceRevision"] = snapshot.SnapshotIdentity.InputWorkspaceRevision.Value,
                ["inputContentRevision"] = snapshot.SnapshotIdentity.InputContentRevision.Value,
                ["inputBuildConfigurationHash"] = snapshot.SnapshotIdentity.InputBuildConfigurationHash
            }
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
