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
namespace Electron2D.Editor.Shell;

internal static class WindowHost
{
    internal const string WindowTitle = "Electron2D.Editor";
    internal const string RenderSource = "runtime-control-tree";
    internal const string InputDispatchSource = "RuntimeHost";

    public static int RunInteractive(ShellStartupProject? startupProject = null)
    {
        var layout = startupProject is null
            ? ShellLayout.CreateDefault()
            : ShellLayout.CreateForProject(startupProject);
        var result = RunRuntimeLayout(
            layout,
            smokeFrameCount: 0,
            stayOpenUntilCloseRequest: true);

        return result.WindowCreated && result.WindowShown && result.FramePresented ? 0 : 1;
    }

    public static WindowRunResult RunRuntimeLayout(
        ShellLayout layout,
        int smokeFrameCount,
        bool stayOpenUntilCloseRequest,
        string? screenshotPath = null,
        Func<Electron2D.RuntimeHostScriptedInputContext, IReadOnlyList<Electron2D.InputEvent>>? scriptedInputProvider = null)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!stayOpenUntilCloseRequest && smokeFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(smokeFrameCount), smokeFrameCount, "Smoke frame count must be positive.");
        }

        var runtimeUi = Application.CreateRuntimeShell(layout);
        var options = new Electron2D.RuntimeHostOptions
        {
            WindowTitle = WindowTitle,
            WindowSize = new Electron2D.Vector2I(ShellLayout.DefaultViewportWidth, ShellLayout.DefaultViewportHeight),
            FrameLimit = stayOpenUntilCloseRequest ? 0 : smokeFrameCount,
            ClearColor = new Electron2D.Color(24f / 255f, 28f / 255f, 33f / 255f, 1f),
            ScreenshotPath = screenshotPath,
            ScriptedInputProvider = scriptedInputProvider
        };

        var result = Electron2D.RuntimeHost.Run(runtimeUi.Tree, options);
        var redDominantPixelRatio = result.ScreenshotSaved && !string.IsNullOrWhiteSpace(result.ScreenshotPath) && File.Exists(result.ScreenshotPath)
            ? CalculateRedDominantPixelRatio(PngDecoder.Decode(File.ReadAllBytes(result.ScreenshotPath)))
            : 0d;

        return new WindowRunResult(
            result.WindowCreated,
            result.WindowShown,
            result.FramePresented,
            result.EventPumpObserved,
            result.WindowWidth,
            result.WindowHeight,
            result.PixelWidth,
            result.PixelHeight,
            result.VideoDriver,
            result.FrameCount,
            RuntimeControlTree: true,
            VisualHarnessRemoved: true,
            RuntimeUiRendering: true,
            RuntimeUiInputDispatch: true,
            RenderSource,
            InputDispatchSource,
            result.DrawCommands,
            redDominantPixelRatio);
    }

    public static WindowRunResult PresentStaticCanvas(PixelCanvas canvas, int smokeFrameCount)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (smokeFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(smokeFrameCount), smokeFrameCount, "Smoke frame count must be positive.");
        }

        var tempFile = Path.Combine(Path.GetTempPath(), "electron2d-editor-static-canvas-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            File.WriteAllBytes(tempFile, PngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels));
            var texture = Electron2D.ImageTexture.LoadFromFile(tempFile);
            var tree = new Electron2D.SceneTree();
            var viewport = (Electron2D.Viewport)tree.Root;
            viewport.Size = new Electron2D.Vector2I(canvas.Width, canvas.Height);
            viewport.AddChild(new Electron2D.TextureRect
            {
                Name = "EditorStaticCanvas",
                Texture = texture,
                Position = Electron2D.Vector2.Zero,
                Size = new Electron2D.Vector2(canvas.Width, canvas.Height),
                MouseFilter = Electron2D.MouseFilter.Ignore
            });

            var result = Electron2D.RuntimeHost.Run(
                tree,
                new Electron2D.RuntimeHostOptions
                {
                    WindowTitle = WindowTitle,
                    WindowSize = new Electron2D.Vector2I(canvas.Width, canvas.Height),
                    FrameLimit = smokeFrameCount,
                    ClearColor = new Electron2D.Color(0f, 0f, 0f, 1f)
                });

            return new WindowRunResult(
                result.WindowCreated,
                result.WindowShown,
                result.FramePresented,
                result.EventPumpObserved,
                result.WindowWidth,
                result.WindowHeight,
                result.PixelWidth,
                result.PixelHeight,
                result.VideoDriver,
                result.FrameCount,
                RuntimeControlTree: false,
                VisualHarnessRemoved: false,
                RuntimeUiRendering: false,
                RuntimeUiInputDispatch: false,
                RenderSource: "static-canvas-texture",
                InputDispatchSource: "none",
                result.DrawCommands,
                CalculateRedDominantPixelRatio(canvas));
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    private static double CalculateRedDominantPixelRatio(PixelCanvas canvas)
    {
        var redDominant = 0;
        var total = canvas.Pixels.Length / 4;
        for (var index = 0; index < canvas.Pixels.Length; index += 4)
        {
            var red = canvas.Pixels[index];
            var green = canvas.Pixels[index + 1];
            var blue = canvas.Pixels[index + 2];
            if (red > 200 && red > green * 2 && red > blue * 2)
            {
                redDominant++;
            }
        }

        return (double)redDominant / total;
    }
}
