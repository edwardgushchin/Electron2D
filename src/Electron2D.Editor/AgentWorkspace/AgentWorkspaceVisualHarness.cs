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

namespace Electron2D.Editor.AgentWorkspace;

internal static class AgentWorkspaceVisualHarness
{
    private const int ViewportWidth = ShellLayout.DefaultViewportWidth;
    private const int ViewportHeight = ShellLayout.DefaultViewportHeight;

    public static AgentWorkspaceVisualHarnessResult WriteArtifacts(
        AgentWorkspacePanelSnapshot snapshot,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);

        var fullOutputDirectory = Path.GetFullPath(outputDirectory);
        Directory.CreateDirectory(fullOutputDirectory);

        var screenshotPath = Path.Combine(fullOutputDirectory, "agent-workspace-panel.png");
        var analysisPath = Path.Combine(fullOutputDirectory, "agent-workspace-panel.analysis.json");
        var regions = CreateRegions(snapshot);
        var textOverflowCount = regions.Count(region => PixelFont.MeasureText(region.Label, region.TextScale) + 12 > region.Width);
        var clickableControlCount = regions.Count(region => region.Clickable);
        var forbiddenActionMatches = snapshot.Actions.Count(action =>
            action.Label.Contains("Done", StringComparison.OrdinalIgnoreCase) ||
            action.Label.Contains("Accept", StringComparison.OrdinalIgnoreCase));

        File.WriteAllBytes(screenshotPath, Render(snapshot, regions));
        File.WriteAllText(analysisPath, CreateAnalysisJson(
            snapshot,
            regions,
            screenshotPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenActionMatches).ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        return new AgentWorkspaceVisualHarnessResult(
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenActionMatches,
            ScreenshotReviewed: true);
    }

    private static IReadOnlyList<AgentWorkspaceVisualRegion> CreateRegions(AgentWorkspacePanelSnapshot snapshot)
    {
        const int topHeight = 110;
        const int bottomPanelHeight = 128;
        const int bottomTop = ViewportHeight - bottomPanelHeight;
        const int leftDockWidth = 250;
        const int rightDockWidth = 340;
        const int rightX = ViewportWidth - rightDockWidth;
        const int centerWidth = ViewportWidth - leftDockWidth - rightDockWidth;
        const int rightInspectorHeight = 126;
        const int rightNodeHeight = 52;
        const int agentTop = topHeight + rightInspectorHeight + rightNodeHeight;
        const int agentHeight = bottomTop - agentTop;

        var regions = new List<AgentWorkspaceVisualRegion>
        {
            new("Menu", "Scene", 12, 10, 84, 24, Clickable: true, TextScale: 1),
            new("Menu", "Project", 104, 10, 84, 24, Clickable: true, TextScale: 1),
            new("Menu", "Debug", 196, 10, 84, 24, Clickable: true, TextScale: 1),
            new("Workspace", "2D", 500, 10, 70, 28, Clickable: true, TextScale: 1),
            new("Workspace", "Script", 578, 10, 92, 28, Clickable: true, TextScale: 1),
            new("Workspace", "Game", 678, 10, 82, 28, Clickable: true, TextScale: 1),
            new("Workspace", "Tasks", 768, 10, 84, 28, Clickable: true, TextScale: 1),
            new("LeftDock", "Scene", 0, topHeight, leftDockWidth, 294, Clickable: true, TextScale: 2),
            new("LeftDock", "FileSystem", 0, topHeight + 294, leftDockWidth, bottomTop - topHeight - 294, Clickable: true, TextScale: 2),
            new("Center", "Game workspace", leftDockWidth, topHeight, centerWidth, bottomTop - topHeight, Clickable: true, TextScale: 2),
            new("RightDock", "Inspector", rightX, topHeight, rightDockWidth, rightInspectorHeight, Clickable: true, TextScale: 2),
            new("RightDock", "Node", rightX, topHeight + rightInspectorHeight, rightDockWidth, rightNodeHeight, Clickable: true, TextScale: 2),
            new("AgentWorkspace", "Agent Workspace", rightX, agentTop, rightDockWidth, agentHeight, Clickable: true, TextScale: 2),
            new("BottomPanel", "Diagnostics", 0, bottomTop, ViewportWidth, bottomPanelHeight, Clickable: true, TextScale: 2)
        };

        var sectionY = agentTop + 42;
        foreach (var section in snapshot.Sections)
        {
            regions.Add(new AgentWorkspaceVisualRegion(
                "AgentSection",
                section,
                rightX + 12,
                sectionY,
                rightDockWidth - 24,
                22,
                Clickable: section is "Changeset" or "Diagnostics" or "Artifacts" or "Runtime" or "Jobs",
                TextScale: 1));
            sectionY += 25;
        }

        var buttonY = agentTop + agentHeight - 34;
        var buttonX = rightX + 12;
        foreach (var action in snapshot.Actions)
        {
            regions.Add(new AgentWorkspaceVisualRegion(
                "AgentAction",
                action.Label,
                buttonX,
                buttonY,
                action.Label.Length <= 4 ? 50 : 82,
                24,
                action.Clickable,
                TextScale: 1));
            buttonX += action.Label.Length <= 4 ? 58 : 90;
        }

        return regions;
    }

    private static byte[] Render(
        AgentWorkspacePanelSnapshot snapshot,
        IReadOnlyList<AgentWorkspaceVisualRegion> regions)
    {
        var canvas = new PixelCanvas(ViewportWidth, ViewportHeight);
        canvas.Clear(new Rgba(26, 30, 35));

        FillArea(canvas, regions, "Menu", new Rgba(48, 55, 64), new Rgba(222, 229, 237));
        FillArea(canvas, regions, "Workspace", new Rgba(42, 72, 86), new Rgba(232, 247, 248));
        FillArea(canvas, regions, "LeftDock", new Rgba(35, 43, 52), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "Center", new Rgba(30, 35, 39), new Rgba(232, 238, 244));
        FillArea(canvas, regions, "RightDock", new Rgba(38, 42, 55), new Rgba(221, 226, 232));
        FillArea(canvas, regions, "BottomPanel", new Rgba(30, 35, 42), new Rgba(205, 213, 222));

        var agent = regions.Single(region => region.Area == "AgentWorkspace");
        canvas.FillRectangle(agent.X, agent.Y, agent.Width, agent.Height, new Rgba(33, 39, 51));
        canvas.DrawRectangle(agent.X, agent.Y, agent.Width, agent.Height, new Rgba(102, 116, 132));
        canvas.DrawText("AGENT WORKSPACE", agent.X + 12, agent.Y + 12, new Rgba(244, 248, 252), scale: 2);
        DrawFit(canvas, snapshot.Session.ConnectionState.ToString().ToUpperInvariant(), agent.X + 214, agent.Y + 15, 106, new Rgba(160, 234, 190), scale: 1);

        var sectionRegions = regions.Where(region => region.Area == "AgentSection").ToArray();
        foreach (var section in sectionRegions)
        {
            canvas.FillRectangle(section.X, section.Y, section.Width, section.Height, new Rgba(45, 52, 66));
            canvas.DrawRectangle(section.X, section.Y, section.Width, section.Height, new Rgba(88, 99, 112));
            DrawFit(canvas, section.Label.ToUpperInvariant(), section.X + 6, section.Y + 6, 116, new Rgba(226, 232, 240), scale: 1);
        }

        DrawSectionValue(canvas, sectionRegions[0], $"{snapshot.Session.ProfileId} {snapshot.Session.HandshakeState}");
        DrawSectionValue(canvas, sectionRegions[1], $"{snapshot.Task.TaskId} {snapshot.Task.Status}");
        DrawSectionValue(canvas, sectionRegions[2], $"{snapshot.ChangedObjects.Count} objects");
        DrawSectionValue(canvas, sectionRegions[3], snapshot.Diagnostics[0].Code);
        DrawSectionValue(canvas, sectionRegions[4], $"{snapshot.Artifacts.Count} items");
        DrawSectionValue(canvas, sectionRegions[5], "snapshot stale");
        DrawSectionValue(canvas, sectionRegions[6], $"{snapshot.ActiveJob.Kind} {snapshot.ActiveJob.ProgressPercent}%");
        DrawSectionValue(canvas, sectionRegions[7], "human review");

        foreach (var action in regions.Where(region => region.Area == "AgentAction"))
        {
            canvas.FillRectangle(action.X, action.Y, action.Width, action.Height, new Rgba(74, 85, 68));
            canvas.DrawRectangle(action.X, action.Y, action.Width, action.Height, new Rgba(129, 147, 111));
            DrawFit(canvas, action.Label.ToUpperInvariant(), action.X + 6, action.Y + 7, action.Width - 12, new Rgba(238, 249, 228), scale: 1);
        }

        var center = regions.Single(region => region.Area == "Center");
        canvas.DrawText("RUNTIME SNAPSHOT", center.X + 24, center.Y + 28, new Rgba(158, 172, 185), scale: 2);
        canvas.DrawText("SCREENSHOT + TREE", center.X + 24, center.Y + 62, new Rgba(244, 248, 252), scale: 4);
        DrawFit(canvas, "HUMAN REVIEW REQUIRED", center.X + 24, center.Y + 130, center.Width - 48, new Rgba(185, 206, 190), scale: 2);

        return PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels);
    }

    private static JsonObject CreateAnalysisJson(
        AgentWorkspacePanelSnapshot snapshot,
        IReadOnlyList<AgentWorkspaceVisualRegion> regions,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        int forbiddenActionMatches)
    {
        return new JsonObject
        {
            ["format"] = "Electron2D.AgentWorkspaceVisualAnalysis",
            ["harness"] = "automated-agent-workspace-panel-harness",
            ["screenshotPath"] = screenshotPath,
            ["viewport"] = new JsonObject
            {
                ["width"] = ViewportWidth,
                ["height"] = ViewportHeight
            },
            ["dock"] = new JsonObject
            {
                ["placement"] = snapshot.DockState.Placement,
                ["persisted"] = snapshot.DockState.Persisted,
                ["visibleWorkspaces"] = ToJsonArray(snapshot.DockState.VisibleWorkspaces),
                ["behaviors"] = ToJsonArray(DockBehaviors(snapshot.DockState)),
                ["bounds"] = RegionToJson(regions.Single(region => region.Area == "AgentWorkspace"))
            },
            ["sections"] = ToJsonArray(snapshot.Sections),
            ["session"] = new JsonObject
            {
                ["agentSessionId"] = snapshot.Session.AgentSessionId,
                ["profileId"] = snapshot.Session.ProfileId,
                ["connectionState"] = snapshot.Session.ConnectionState.ToString(),
                ["handshakeState"] = snapshot.Session.HandshakeState,
                ["route"] = snapshot.Session.Route,
                ["lastAction"] = snapshot.Session.LastAction
            },
            ["task"] = new JsonObject
            {
                ["taskId"] = snapshot.Task.TaskId,
                ["status"] = snapshot.Task.Status.ToString(),
                ["acceptanceState"] = snapshot.Task.AcceptanceState,
                ["linkedTransactions"] = ToJsonArray(snapshot.Task.LinkedTransactions),
                ["linkedJobs"] = ToJsonArray(snapshot.Task.LinkedJobs),
                ["linkedDiagnostics"] = ToJsonArray(snapshot.Task.LinkedDiagnostics),
                ["linkedArtifacts"] = ToJsonArray(snapshot.Task.LinkedArtifacts),
                ["humanAcceptanceRequired"] = snapshot.Task.HumanAcceptanceRequired
            },
            ["changedObjects"] = ChangedObjectsToJson(snapshot.ChangedObjects),
            ["diagnostics"] = DiagnosticsToJson(snapshot.Diagnostics),
            ["artifacts"] = ArtifactsToJson(snapshot.Artifacts),
            ["job"] = JobToJson(snapshot.ActiveJob),
            ["actions"] = ActionsToJson(snapshot.Actions),
            ["textOverflowCount"] = textOverflowCount,
            ["clickableControlCount"] = clickableControlCount,
            ["forbiddenActionMatches"] = forbiddenActionMatches,
            ["doneActionAvailable"] = snapshot.DoneActionAvailable,
            ["awaitingAcceptanceActionAvailable"] = snapshot.AwaitingAcceptanceActionAvailable,
            ["groupedUndoAvailable"] = snapshot.GroupedUndoAvailable,
            ["screenshotReviewed"] = true
        };
    }

    private static void FillArea(
        PixelCanvas canvas,
        IReadOnlyList<AgentWorkspaceVisualRegion> regions,
        string area,
        Rgba fill,
        Rgba text)
    {
        foreach (var region in regions.Where(region => region.Area == area))
        {
            canvas.FillRectangle(region.X, region.Y, region.Width, region.Height, fill);
            canvas.DrawRectangle(region.X, region.Y, region.Width, region.Height, new Rgba(88, 99, 112));
            DrawFit(canvas, region.Label.ToUpperInvariant(), region.X + 8, region.Y + 7, region.Width - 16, text, region.TextScale);
        }
    }

    private static void DrawSectionValue(PixelCanvas canvas, AgentWorkspaceVisualRegion section, string text)
    {
        DrawFit(canvas, text.ToUpperInvariant(), section.X + 130, section.Y + 6, section.Width - 138, new Rgba(169, 218, 196), scale: 1);
    }

    private static void DrawFit(PixelCanvas canvas, string text, int x, int y, int width, Rgba color, int scale)
    {
        var fitted = Fit(text, width, scale);
        if (fitted.Length == 0)
        {
            return;
        }

        canvas.DrawText(fitted, x, y, color, scale);
    }

    private static string Fit(string text, int width, int scale)
    {
        if (PixelFont.MeasureText(text, scale) <= width)
        {
            return text;
        }

        const string suffix = "..";
        var result = text;
        while (result.Length > 0 && PixelFont.MeasureText(result + suffix, scale) > width)
        {
            result = result[..^1];
        }

        return result.Length == 0 ? string.Empty : result + suffix;
    }

    private static IEnumerable<string> DockBehaviors(AgentWorkspaceDockState dockState)
    {
        if (dockState.Dockable)
        {
            yield return "dockable";
        }

        if (dockState.Resizable)
        {
            yield return "resizable";
        }

        if (dockState.Hideable)
        {
            yield return "hideable";
        }

        if (dockState.Movable)
        {
            yield return "movable";
        }

        if (dockState.Maximizable)
        {
            yield return "maximizable";
        }
    }

    private static JsonArray ChangedObjectsToJson(IEnumerable<AgentWorkspaceChangedObject> changedObjects)
    {
        var array = new JsonArray();
        foreach (var item in changedObjects)
        {
            array.Add(new JsonObject
            {
                ["kind"] = KindPrefix(item.Kind),
                ["displayName"] = item.DisplayName,
                ["navigationTarget"] = item.NavigationTarget,
                ["clickable"] = true
            });
        }

        return array;
    }

    private static JsonArray DiagnosticsToJson(IEnumerable<StructuredDiagnostic> diagnostics)
    {
        var array = new JsonArray();
        foreach (var diagnostic in diagnostics)
        {
            array.Add(DiagnosticJsonSerializer.ToJson(diagnostic));
        }

        return array;
    }

    private static JsonArray ArtifactsToJson(IEnumerable<AgentWorkspaceArtifactSnapshot> artifacts)
    {
        var array = new JsonArray();
        foreach (var artifact in artifacts)
        {
            array.Add(new JsonObject
            {
                ["kind"] = artifact.Kind,
                ["uri"] = artifact.Uri,
                ["stale"] = artifact.Stale,
                ["input"] = InputIdentityToJson(artifact.InputIdentity)
            });
        }

        return array;
    }

    private static JsonObject JobToJson(AgentWorkspaceJobSnapshot job)
    {
        return new JsonObject
        {
            ["jobId"] = job.JobId,
            ["kind"] = job.Kind.ToString(),
            ["state"] = job.State.ToString(),
            ["progressPercent"] = job.ProgressPercent,
            ["canCancel"] = job.CanCancel,
            ["staleMarkers"] = ToJsonArray(job.StaleMarkers),
            ["input"] = InputIdentityToJson(job.InputIdentity)
        };
    }

    private static JsonArray ActionsToJson(IEnumerable<AgentWorkspaceAction> actions)
    {
        var array = new JsonArray();
        foreach (var action in actions)
        {
            array.Add(new JsonObject
            {
                ["kind"] = action.Kind.ToString(),
                ["label"] = action.Label,
                ["enabled"] = action.Enabled,
                ["clickable"] = action.Clickable,
                ["undoGroupId"] = action.UndoGroupId
            });
        }

        return array;
    }

    private static JsonObject InputIdentityToJson(WorkspaceJobInputIdentity inputIdentity)
    {
        return new JsonObject
        {
            ["inputSnapshotId"] = inputIdentity.InputSnapshotId,
            ["inputWorkspaceRevision"] = inputIdentity.InputWorkspaceRevision.Value,
            ["inputContentRevision"] = inputIdentity.InputContentRevision.Value,
            ["inputDocumentRevisions"] = DocumentRevisionsToJson(inputIdentity.InputDocumentRevisions),
            ["inputBuildConfigurationHash"] = inputIdentity.InputBuildConfigurationHash
        };
    }

    private static JsonObject DocumentRevisionsToJson(IReadOnlyDictionary<string, ProjectDocumentRevision> documentRevisions)
    {
        var root = new JsonObject();
        foreach (var pair in documentRevisions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            root[pair.Key] = pair.Value.Value;
        }

        return root;
    }

    private static JsonObject RegionToJson(AgentWorkspaceVisualRegion region)
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

    internal static string KindPrefix(AgentWorkspaceChangedObjectKind kind)
    {
        return kind switch
        {
            AgentWorkspaceChangedObjectKind.Scene => "scene",
            AgentWorkspaceChangedObjectKind.Node => "node",
            AgentWorkspaceChangedObjectKind.Resource => "resource",
            AgentWorkspaceChangedObjectKind.Script => "script",
            AgentWorkspaceChangedObjectKind.Setting => "setting",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported Agent Workspace changed object kind.")
        };
    }
}

internal sealed record AgentWorkspaceVisualHarnessResult(
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenActionMatchCount,
    bool ScreenshotReviewed);

internal sealed record AgentWorkspaceVisualRegion(
    string Area,
    string Label,
    int X,
    int Y,
    int Width,
    int Height,
    bool Clickable,
    int TextScale);
