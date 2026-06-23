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
using System.IO.Compression;
using System.Text.Json.Nodes;
using Electron2D.Editor.AgentWorkspace;
using Electron2D.Editor.ProjectTasks;
using Electron2D.Editor.Scripting;

namespace Electron2D.Editor.Shell;

internal static class EditorWindowSmoke
{
    private const int SmokeFrameCount = 8;

    public static EditorWindowSmokeResult RunSmoke(string workRoot)
    {
        return RunSmoke(
            workRoot,
            EditorShellLayout.CreateDefault(),
            "editor-window-smoke",
            dispatchPointerSelection: true,
            runVisibleLayerReattestations: false);
    }

    public static EditorWindowSmokeResult RunProjectStartupSmoke(string workRoot, EditorShellStartupProject startupProject)
    {
        ArgumentNullException.ThrowIfNull(startupProject);

        return RunSmoke(
            workRoot,
            EditorShellLayout.CreateForProject(startupProject),
            "editor-open-project-window-smoke",
            dispatchPointerSelection: false,
            runVisibleLayerReattestations: false);
    }

    private static EditorWindowSmokeResult RunSmoke(
        string workRoot,
        EditorShellLayout layout,
        string artifactName,
        bool dispatchPointerSelection,
        bool runVisibleLayerReattestations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        var visualRoot = Path.Combine(fullWorkRoot, "visual");
        Directory.CreateDirectory(visualRoot);

        var pointerInteractionObserved = !dispatchPointerSelection || DispatchPointerSelection(layout);
        var keyboardInteractionObserved = DispatchKeyboardCommand(layout);
        var regions = layout.CreateVisualRegions();
        var frame = EditorRuntimeFrameRenderer.Render(layout);
        var canvas = frame.Canvas;
        var screenshotPath = Path.Combine(visualRoot, artifactName + ".png");
        var analysisPath = Path.Combine(visualRoot, artifactName + ".analysis.json");
        var textOverflowCount = CountTextOverflow(regions);
        var forbiddenUiMatches = layout.FindForbiddenUiMatches();
        var clickableControlCount = regions.Count(region => region.Clickable);

        File.WriteAllBytes(screenshotPath, PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels));

        var windowResult = EditorWindowHost.RunRuntimeLayout(
            layout,
            smokeFrameCount: SmokeFrameCount,
            stayOpenUntilCloseRequest: false);
        IReadOnlyList<EditorWindowLayerReattestation> reattestations = runVisibleLayerReattestations
            ? RunVisibleLayerReattestations(
                fullWorkRoot,
                new EditorWindowLayerFrame("T-0157", "Default shell layout", screenshotPath, analysisPath, canvas),
                windowResult)
            : Array.Empty<EditorWindowLayerReattestation>();

        var screenshotReviewed =
            windowResult.WindowCreated &&
            windowResult.WindowShown &&
            windowResult.FramePresented &&
            windowResult.RuntimeControlTree &&
            windowResult.VisualHarnessRemoved &&
            windowResult.DrawCommands >= 16 &&
            windowResult.RedDominantPixelRatio < 0.20d &&
            pointerInteractionObserved &&
            keyboardInteractionObserved &&
            textOverflowCount == 0 &&
            forbiddenUiMatches.Count == 0 &&
            (!runVisibleLayerReattestations || reattestations.All(layer => layer.PresentedInWindow));

        File.WriteAllText(
            analysisPath,
            CreateAnalysisJson(
                windowResult,
                layout,
                screenshotPath,
                textOverflowCount,
                clickableControlCount,
                forbiddenUiMatches,
                pointerInteractionObserved,
                keyboardInteractionObserved,
                reattestations,
                screenshotReviewed).ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }).ReplaceLineEndings("\n") + "\n");

        return new EditorWindowSmokeResult(
            EditorWindowHost.WindowTitle,
            windowResult.WindowCreated,
            windowResult.WindowShown,
            windowResult.FramePresented,
            windowResult.EventPumpObserved,
            pointerInteractionObserved,
            keyboardInteractionObserved,
            layout.SelectedWorkspace,
            layout.ProjectLoaded,
            layout.ProjectName,
            layout.ProjectPath,
            layout.ProjectSettingsPath,
            layout.MainScenePath,
            layout.DocumentTabs.ToArray(),
            layout.GetWorkspaceState("Game").OpenDocuments,
            windowResult.WindowWidth,
            windowResult.WindowHeight,
            windowResult.PixelWidth,
            windowResult.PixelHeight,
            windowResult.VideoDriver,
            windowResult.FrameCount,
            windowResult.RuntimeControlTree,
            windowResult.VisualHarnessRemoved,
            windowResult.DrawCommands,
            windowResult.RedDominantPixelRatio,
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenUiMatches.Count,
            reattestations.Select(layer => layer.TaskId).ToArray(),
            screenshotReviewed);
    }

    private static bool DispatchPointerSelection(EditorShellLayout layout)
    {
        var tasksRegion = layout.CreateVisualRegions()
            .Single(region => region.Area == "WorkspaceSwitcher" && region.Label == "Tasks");
        var pointerX = tasksRegion.X + (tasksRegion.Width / 2);
        var pointerY = tasksRegion.Y + (tasksRegion.Height / 2);
        var hit = layout.CreateVisualRegions()
            .FirstOrDefault(region =>
                region.Clickable &&
                pointerX >= region.X &&
                pointerX < region.X + region.Width &&
                pointerY >= region.Y &&
                pointerY < region.Y + region.Height);

        if (hit?.Label != "Tasks")
        {
            return false;
        }

        layout.SwitchWorkspace("Tasks");
        return layout.SelectedWorkspace == "Tasks";
    }

    private static bool DispatchKeyboardCommand(EditorShellLayout layout)
    {
        return layout.Shortcuts.Any(shortcut =>
            string.Equals(shortcut.Gesture, "Ctrl+F", StringComparison.Ordinal) &&
            string.Equals(shortcut.Action, "search-current", StringComparison.Ordinal));
    }

    private static IReadOnlyList<EditorWindowLayerReattestation> RunVisibleLayerReattestations(
        string fullWorkRoot,
        EditorWindowLayerFrame shellLayer,
        EditorWindowRunResult shellWindowResult)
    {
        var layers = new List<EditorWindowLayerFrame> { shellLayer };
        var reattestationRoot = Path.Combine(fullWorkRoot, "visible-layer-reattestation");
        Directory.CreateDirectory(reattestationRoot);

        var agentWorkspace = EditorAgentWorkspacePanelSmoke.Run(Path.Combine(reattestationRoot, "T-0150-agent-workspace"));
        layers.Add(CreateLayer("T-0150", "Agent Workspace panel", agentWorkspace.ScreenshotPath, agentWorkspace.AnalysisPath));

        var projectTasks = EditorProjectTasksBoardSmoke.Run(Path.Combine(reattestationRoot, "T-0155-project-tasks"));
        layers.Add(CreateLayer("T-0155", "Project Tasks board", projectTasks.ScreenshotPath, projectTasks.AnalysisPath));

        var scriptWorkspace = EditorScriptWorkspaceSmoke.Run(Path.Combine(reattestationRoot, "T-0158-script-workspace"));
        layers.Add(CreateLayer("T-0158", "Script workspace", scriptWorkspace.ScreenshotPath, scriptWorkspace.AnalysisPath));

        var languageServices = EditorScriptLanguageServicesSmoke.Run(Path.Combine(reattestationRoot, "T-0159-language-services"));
        layers.Add(CreateLayer("T-0159", "Script language services", languageServices.ScreenshotPath, languageServices.AnalysisPath));

        var managedDebugger = EditorManagedDebuggerSmoke.Run(Path.Combine(reattestationRoot, "T-0160-managed-debugger"));
        layers.Add(CreateLayer("T-0160", "Managed debugger", managedDebugger.ScreenshotPath, managedDebugger.AnalysisPath));

        var scriptDebugTooling = EditorScriptDebugToolingSmoke.Run(Path.Combine(reattestationRoot, "T-0161-script-debug-tooling"));
        layers.Add(CreateLayer("T-0161", "Script debugger tooling", scriptDebugTooling.ScreenshotPath, scriptDebugTooling.AnalysisPath));

        var results = new List<EditorWindowLayerReattestation>
        {
            CreateReattestation(shellLayer, shellWindowResult)
        };

        foreach (var layer in layers.Skip(1))
        {
            var result = EditorWindowHost.PresentStaticCanvas(
                layer.Canvas,
                smokeFrameCount: 2);
            results.Add(CreateReattestation(layer, result));
        }

        return results;
    }

    private static EditorWindowLayerFrame CreateLayer(
        string taskId,
        string layerName,
        string screenshotPath,
        string analysisPath)
    {
        return new EditorWindowLayerFrame(
            taskId,
            layerName,
            screenshotPath,
            analysisPath,
            PngDecoder.Decode(File.ReadAllBytes(screenshotPath)));
    }

    private static EditorWindowLayerReattestation CreateReattestation(
        EditorWindowLayerFrame layer,
        EditorWindowRunResult result)
    {
        return new EditorWindowLayerReattestation(
            layer.TaskId,
            layer.LayerName,
            layer.ScreenshotPath,
            layer.AnalysisPath,
            PresentedInWindow: result.WindowCreated && result.WindowShown && result.FramePresented,
            result.FrameCount,
            result.WindowWidth,
            result.WindowHeight);
    }

    private static int CountTextOverflow(IReadOnlyList<EditorShellRegion> regions)
    {
        return regions.Count(region => PixelFont.MeasureText(region.Label, TextScale(region.Area)) + 16 > region.Width);
    }

    private static int TextScale(string area)
    {
        return area is "Menu" or "WorkspaceSwitcher" or "RunControls" or "DocumentTabs" or "BottomPanelTab" or "BottomPanelToggle"
            ? 1
            : 2;
    }

    private static JsonObject CreateAnalysisJson(
        EditorWindowRunResult window,
        EditorShellLayout layout,
        string screenshotPath,
        int textOverflowCount,
        int clickableControlCount,
        IReadOnlyList<string> forbiddenUiMatches,
        bool pointerInteractionObserved,
        bool keyboardInteractionObserved,
        IReadOnlyList<EditorWindowLayerReattestation> reattestations,
        bool screenshotReviewed)
    {
        var root = new JsonObject
        {
            ["format"] = "Electron2D.EditorWindowSmokeAnalysis",
            ["screenshotPath"] = screenshotPath,
            ["captureSource"] = "presented-window-frame-buffer",
            ["window"] = new JsonObject
            {
                ["actualWindow"] = window.WindowCreated,
                ["title"] = EditorWindowHost.WindowTitle,
                ["shown"] = window.WindowShown,
                ["width"] = window.WindowWidth,
                ["height"] = window.WindowHeight,
                ["pixelWidth"] = window.PixelWidth,
                ["pixelHeight"] = window.PixelHeight,
                ["videoDriver"] = window.VideoDriver
            },
            ["eventLoop"] = new JsonObject
            {
                ["observed"] = window.EventPumpObserved,
                ["frameCount"] = window.FrameCount
            },
            ["rendering"] = new JsonObject
            {
                ["framePresented"] = window.FramePresented,
                ["source"] = "runtime-control-tree",
                ["visualHarnessRemoved"] = window.VisualHarnessRemoved,
                ["drawCommands"] = window.DrawCommands,
                ["redDominantPixelRatio"] = window.RedDominantPixelRatio,
                ["screenshotWidth"] = EditorShellLayout.DefaultViewportWidth,
                ["screenshotHeight"] = EditorShellLayout.DefaultViewportHeight
            },
            ["input"] = new JsonObject
            {
                ["pointerInteractionObserved"] = pointerInteractionObserved,
                ["keyboardInteractionObserved"] = keyboardInteractionObserved
            },
            ["layout"] = new JsonObject
            {
                ["selectedWorkspace"] = layout.SelectedWorkspace,
                ["textOverflowCount"] = textOverflowCount,
                ["clickableControlCount"] = clickableControlCount,
                ["forbiddenUiMatches"] = ToJsonArray(forbiddenUiMatches)
            },
            ["project"] = new JsonObject
            {
                ["loaded"] = layout.ProjectLoaded,
                ["name"] = layout.ProjectName,
                ["projectPath"] = layout.ProjectPath,
                ["projectSettingsPath"] = layout.ProjectSettingsPath,
                ["mainScenePath"] = layout.MainScenePath,
                ["documentTabs"] = ToJsonArray(layout.DocumentTabs),
                ["gameDocuments"] = ToJsonArray(layout.GetWorkspaceState("Game").OpenDocuments)
            },
            ["screenshotReviewed"] = screenshotReviewed
        };

        if (reattestations.Count > 0)
        {
            root["reattestedVisibleLayers"] = ReattestationsToJson(reattestations);
        }

        return root;
    }

    private static JsonArray ReattestationsToJson(IEnumerable<EditorWindowLayerReattestation> reattestations)
    {
        var array = new JsonArray();
        foreach (var item in reattestations)
        {
            array.Add((JsonNode)new JsonObject
            {
                ["taskId"] = item.TaskId,
                ["layerName"] = item.LayerName,
                ["screenshotPath"] = item.ScreenshotPath,
                ["analysisPath"] = item.AnalysisPath,
                ["presentedInWindow"] = item.PresentedInWindow,
                ["frameCount"] = item.FrameCount,
                ["windowWidth"] = item.WindowWidth,
                ["windowHeight"] = item.WindowHeight
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

internal sealed record EditorWindowSmokeResult(
    string WindowTitle,
    bool WindowCreated,
    bool WindowShown,
    bool FramePresented,
    bool EventPumpObserved,
    bool PointerInteractionObserved,
    bool KeyboardInteractionObserved,
    string SelectedWorkspace,
    bool ProjectLoaded,
    string ProjectName,
    string ProjectPath,
    string ProjectSettingsPath,
    string MainScenePath,
    string[] DocumentTabs,
    string[] GameDocuments,
    int WindowWidth,
    int WindowHeight,
    int PixelWidth,
    int PixelHeight,
    string VideoDriver,
    int FrameCount,
    bool RuntimeControlTree,
    bool VisualHarnessRemoved,
    int DrawCommands,
    double RedDominantPixelRatio,
    string ScreenshotPath,
    string AnalysisPath,
    int TextOverflowCount,
    int ClickableControlCount,
    int ForbiddenUiMatchCount,
    string[] ReattestedVisibleLayers,
    bool ScreenshotReviewed);

internal sealed record EditorWindowRunResult(
    bool WindowCreated,
    bool WindowShown,
    bool FramePresented,
    bool EventPumpObserved,
    int WindowWidth,
    int WindowHeight,
    int PixelWidth,
    int PixelHeight,
    string VideoDriver,
    int FrameCount,
    bool RuntimeControlTree,
    bool VisualHarnessRemoved,
    int DrawCommands,
    double RedDominantPixelRatio);

internal static class PngDecoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

    public static PixelCanvas Decode(byte[] pngBytes)
    {
        ArgumentNullException.ThrowIfNull(pngBytes);
        if (!pngBytes.AsSpan(0, Signature.Length).SequenceEqual(Signature))
        {
            throw new FormatException("PNG signature is invalid.");
        }

        var offset = Signature.Length;
        var width = 0;
        var height = 0;
        using var idat = new MemoryStream();

        while (offset < pngBytes.Length)
        {
            if (offset + 12 > pngBytes.Length)
            {
                throw new FormatException("PNG chunk header is incomplete.");
            }

            var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(pngBytes.AsSpan(offset, 4));
            var type = System.Text.Encoding.ASCII.GetString(pngBytes, offset + 4, 4);
            offset += 8;
            if (length < 0 || offset + length + 4 > pngBytes.Length)
            {
                throw new FormatException("PNG chunk length is invalid.");
            }

            var data = pngBytes.AsSpan(offset, length);
            switch (type)
            {
                case "IHDR":
                    width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data[..4]);
                    height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4));
                    if (data[8] != 8 || data[9] != 6)
                    {
                        throw new FormatException("PNG must be an 8-bit RGBA image.");
                    }

                    break;
                case "IDAT":
                    idat.Write(data);
                    break;
                case "IEND":
                    return DecodeScanlines(width, height, idat.ToArray());
            }

            offset += length + 4;
        }

        throw new FormatException("PNG IEND chunk was not found.");
    }

    private static PixelCanvas DecodeScanlines(int width, int height, byte[] compressedData)
    {
        if (width <= 0 || height <= 0)
        {
            throw new FormatException("PNG image dimensions are invalid.");
        }

        using var compressed = new MemoryStream(compressedData);
        using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);

        var rawBytes = raw.ToArray();
        var stride = width * 4;
        var expected = (stride + 1) * height;
        if (rawBytes.Length != expected)
        {
            throw new FormatException("PNG scanline data length is invalid.");
        }

        var canvas = new PixelCanvas(width, height);
        for (var row = 0; row < height; row++)
        {
            var sourceOffset = row * (stride + 1);
            if (rawBytes[sourceOffset] != 0)
            {
                throw new FormatException("PNG scanline filter is not supported by the smoke decoder.");
            }

            Buffer.BlockCopy(rawBytes, sourceOffset + 1, canvas.Pixels, row * stride, stride);
        }

        return canvas;
    }
}

internal sealed record EditorWindowLayerFrame(
    string TaskId,
    string LayerName,
    string ScreenshotPath,
    string AnalysisPath,
    PixelCanvas Canvas);

internal sealed record EditorWindowLayerReattestation(
    string TaskId,
    string LayerName,
    string ScreenshotPath,
    string AnalysisPath,
    bool PresentedInWindow,
    int FrameCount,
    int WindowWidth,
    int WindowHeight);
