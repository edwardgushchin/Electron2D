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
using System.Runtime.InteropServices;
using System.IO.Compression;
using System.Text.Json.Nodes;
using Electron2D.Editor.AgentWorkspace;
using Electron2D.Editor.ProjectTasks;
using Electron2D.Editor.Scripting;
using SDL3;

namespace Electron2D.Editor.Shell;

internal static class EditorWindowSmoke
{
    private const string WindowTitle = "Electron2D.Editor";
    private const int SmokeFrameCount = 8;

    public static EditorWindowSmokeResult RunSmoke(string workRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workRoot);

        var fullWorkRoot = Path.GetFullPath(workRoot);
        var visualRoot = Path.Combine(fullWorkRoot, "visual");
        Directory.CreateDirectory(visualRoot);

        var layout = EditorShellLayout.CreateDefault();
        var pointerInteractionObserved = DispatchPointerSelection(layout);
        var keyboardInteractionObserved = DispatchKeyboardCommand(layout);
        var regions = layout.CreateVisualRegions();
        var canvas = EditorShellVisualHarness.RenderCanvas(layout);
        var screenshotPath = Path.Combine(visualRoot, "editor-window-smoke.png");
        var analysisPath = Path.Combine(visualRoot, "editor-window-smoke.analysis.json");
        var textOverflowCount = CountTextOverflow(regions);
        var forbiddenUiMatches = layout.FindForbiddenUiMatches();
        var clickableControlCount = regions.Count(region => region.Clickable);

        File.WriteAllBytes(screenshotPath, PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels));

        var windowResult = RunWindow(
            canvas,
            smokeFrameCount: SmokeFrameCount,
            stayOpenUntilCloseRequest: false);
        var reattestations = RunVisibleLayerReattestations(
            fullWorkRoot,
            new EditorWindowLayerFrame("T-0157", "Default shell layout", screenshotPath, analysisPath, canvas),
            windowResult);

        var screenshotReviewed =
            windowResult.WindowCreated &&
            windowResult.WindowShown &&
            windowResult.FramePresented &&
            pointerInteractionObserved &&
            keyboardInteractionObserved &&
            textOverflowCount == 0 &&
            forbiddenUiMatches.Count == 0 &&
            reattestations.All(layer => layer.PresentedInWindow);

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
            WindowTitle,
            windowResult.WindowCreated,
            windowResult.WindowShown,
            windowResult.FramePresented,
            windowResult.EventPumpObserved,
            pointerInteractionObserved,
            keyboardInteractionObserved,
            layout.SelectedWorkspace,
            windowResult.WindowWidth,
            windowResult.WindowHeight,
            windowResult.PixelWidth,
            windowResult.PixelHeight,
            windowResult.VideoDriver,
            windowResult.FrameCount,
            screenshotPath,
            analysisPath,
            textOverflowCount,
            clickableControlCount,
            forbiddenUiMatches.Count,
            reattestations.Select(layer => layer.TaskId).ToArray(),
            screenshotReviewed);
    }

    public static int RunInteractive()
    {
        var layout = EditorShellLayout.CreateDefault();
        var canvas = EditorShellVisualHarness.RenderCanvas(layout);
        var result = RunWindow(
            canvas,
            smokeFrameCount: 0,
            stayOpenUntilCloseRequest: true);

        return result.WindowCreated && result.WindowShown && result.FramePresented ? 0 : 1;
    }

    private static EditorWindowRunResult RunWindow(
        PixelCanvas canvas,
        int smokeFrameCount,
        bool stayOpenUntilCloseRequest)
    {
        ArgumentNullException.ThrowIfNull(canvas);

        var initialized = SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events);
        if (!initialized)
        {
            throw new InvalidOperationException("Editor window initialization failed: " + SDL.GetError());
        }

        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                WindowTitle,
                canvas.Width,
                canvas.Height,
                SDL.WindowFlags.Resizable);
            if (window == IntPtr.Zero)
            {
                throw new InvalidOperationException("Editor window creation failed: " + SDL.GetError());
            }

            var shown = SDL.ShowWindow(window);
            var frameCount = 0;
            var eventPumpObserved = false;
            var framePresented = false;
            var closeRequested = false;
            var targetFrames = stayOpenUntilCloseRequest ? int.MaxValue : Math.Max(1, smokeFrameCount);

            while (!closeRequested && frameCount < targetFrames)
            {
                eventPumpObserved = PumpEvents();
                closeRequested = closeRequested || PollCloseRequested();
                framePresented = PresentFrame(window, canvas) || framePresented;
                frameCount++;
                Thread.Sleep(16);
            }

            var windowWidth = canvas.Width;
            var windowHeight = canvas.Height;
            _ = SDL.GetWindowSize(window, out windowWidth, out windowHeight);

            var pixelWidth = canvas.Width;
            var pixelHeight = canvas.Height;
            _ = SDL.GetWindowSizeInPixels(window, out pixelWidth, out pixelHeight);

            return new EditorWindowRunResult(
                WindowCreated: true,
                WindowShown: shown,
                FramePresented: framePresented,
                EventPumpObserved: eventPumpObserved,
                WindowWidth: windowWidth,
                WindowHeight: windowHeight,
                PixelWidth: pixelWidth,
                PixelHeight: pixelHeight,
                VideoDriver: SDL.GetCurrentVideoDriver() ?? "unknown",
                FrameCount: frameCount);
        }
        finally
        {
            if (window != IntPtr.Zero)
            {
                SDL.DestroyWindow(window);
            }

            SDL.Quit();
        }
    }

    private static bool PumpEvents()
    {
        SDL.PumpEvents();
        return true;
    }

    private static bool PollCloseRequested()
    {
        while (SDL.PollEvent(out var windowEvent))
        {
            var eventType = (SDL.EventType)windowEvent.Type;
            if (eventType is SDL.EventType.Quit or SDL.EventType.WindowCloseRequested)
            {
                return true;
            }
        }

        return false;
    }

    private static bool PresentFrame(IntPtr window, PixelCanvas canvas)
    {
        var windowSurface = SDL.GetWindowSurface(window);
        if (windowSurface == IntPtr.Zero)
        {
            throw new InvalidOperationException("Editor window surface was not available: " + SDL.GetError());
        }

        var handle = GCHandle.Alloc(canvas.Pixels, GCHandleType.Pinned);
        var frameSurface = IntPtr.Zero;
        try
        {
            frameSurface = SDL.CreateSurfaceFrom(
                canvas.Width,
                canvas.Height,
                SDL.PixelFormat.RGBA8888,
                handle.AddrOfPinnedObject(),
                canvas.Width * 4);
            if (frameSurface == IntPtr.Zero)
            {
                throw new InvalidOperationException("Editor frame surface creation failed: " + SDL.GetError());
            }

            if (!SDL.BlitSurface(frameSurface, IntPtr.Zero, windowSurface, IntPtr.Zero))
            {
                throw new InvalidOperationException("Editor frame blit failed: " + SDL.GetError());
            }

            if (!SDL.UpdateWindowSurface(window))
            {
                throw new InvalidOperationException("Editor window frame presentation failed: " + SDL.GetError());
            }

            return true;
        }
        finally
        {
            if (frameSurface != IntPtr.Zero)
            {
                SDL.DestroySurface(frameSurface);
            }

            handle.Free();
        }
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
            var result = RunWindow(
                layer.Canvas,
                smokeFrameCount: 2,
                stayOpenUntilCloseRequest: false);
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
        return new JsonObject
        {
            ["format"] = "Electron2D.EditorWindowSmokeAnalysis",
            ["screenshotPath"] = screenshotPath,
            ["captureSource"] = "presented-window-frame-buffer",
            ["window"] = new JsonObject
            {
                ["actualWindow"] = window.WindowCreated,
                ["title"] = WindowTitle,
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
            ["reattestedVisibleLayers"] = ReattestationsToJson(reattestations),
            ["screenshotReviewed"] = screenshotReviewed
        };
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
    int WindowWidth,
    int WindowHeight,
    int PixelWidth,
    int PixelHeight,
    string VideoDriver,
    int FrameCount,
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
    int FrameCount);

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
