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
using SDL3;

namespace Electron2D.Editor.Shell;

internal static class EditorWindowHost
{
    internal const string WindowTitle = "Electron2D.Editor";

    public static int RunInteractive(EditorShellStartupProject? startupProject = null)
    {
        var layout = startupProject is null
            ? EditorShellLayout.CreateDefault()
            : EditorShellLayout.CreateForProject(startupProject);
        var result = RunRuntimeLayout(
            layout,
            smokeFrameCount: 0,
            stayOpenUntilCloseRequest: true);

        return result.WindowCreated && result.WindowShown && result.FramePresented ? 0 : 1;
    }

    public static EditorWindowRunResult RunRuntimeLayout(
        EditorShellLayout layout,
        int smokeFrameCount,
        bool stayOpenUntilCloseRequest)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (!stayOpenUntilCloseRequest && smokeFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(smokeFrameCount), smokeFrameCount, "Smoke frame count must be positive.");
        }

        return RunWindow(
            layout,
            staticCanvas: null,
            smokeFrameCount,
            stayOpenUntilCloseRequest);
    }

    public static EditorWindowRunResult PresentStaticCanvas(PixelCanvas canvas, int smokeFrameCount)
    {
        ArgumentNullException.ThrowIfNull(canvas);
        if (smokeFrameCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(smokeFrameCount), smokeFrameCount, "Smoke frame count must be positive.");
        }

        return RunWindow(
            layout: null,
            canvas,
            smokeFrameCount,
            stayOpenUntilCloseRequest: false);
    }

    private static EditorWindowRunResult RunWindow(
        EditorShellLayout? layout,
        PixelCanvas? staticCanvas,
        int smokeFrameCount,
        bool stayOpenUntilCloseRequest)
    {
        var currentFrame = CreateFrame(layout, staticCanvas);
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
                currentFrame.Canvas.Width,
                currentFrame.Canvas.Height,
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
            var targetFrames = Math.Max(1, smokeFrameCount);

            while (!closeRequested && (stayOpenUntilCloseRequest || frameCount < targetFrames))
            {
                eventPumpObserved = PollEvents(layout, out var closeFromEvents) || eventPumpObserved;
                closeRequested = closeRequested || closeFromEvents;
                currentFrame = CreateFrame(layout, staticCanvas);
                framePresented = PresentFrame(window, currentFrame.Canvas) || framePresented;
                frameCount++;
                Thread.Sleep(16);
            }

            var windowWidth = currentFrame.Canvas.Width;
            var windowHeight = currentFrame.Canvas.Height;
            _ = SDL.GetWindowSize(window, out windowWidth, out windowHeight);

            var pixelWidth = currentFrame.Canvas.Width;
            var pixelHeight = currentFrame.Canvas.Height;
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
                FrameCount: frameCount,
                RuntimeControlTree: layout is not null,
                VisualHarnessRemoved: layout is not null,
                DrawCommands: currentFrame.DrawCommands,
                RedDominantPixelRatio: currentFrame.RedDominantPixelRatio);
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

    private static EditorRuntimeFrame CreateFrame(EditorShellLayout? layout, PixelCanvas? staticCanvas)
    {
        if (layout is not null)
        {
            return EditorRuntimeFrameRenderer.Render(layout);
        }

        ArgumentNullException.ThrowIfNull(staticCanvas);
        return new EditorRuntimeFrame(
            staticCanvas,
            DrawCommands: 0,
            RedDominantPixelRatio: CalculateRedDominantPixelRatio(staticCanvas),
            ControlCount: 0);
    }

    private static bool PollEvents(EditorShellLayout? layout, out bool closeRequested)
    {
        SDL.PumpEvents();
        closeRequested = false;
        while (SDL.PollEvent(out var windowEvent))
        {
            var eventType = (SDL.EventType)windowEvent.Type;
            if (eventType is SDL.EventType.Quit or SDL.EventType.WindowCloseRequested)
            {
                closeRequested = true;
            }
            else if (layout is not null &&
                eventType is SDL.EventType.MouseButtonDown &&
                windowEvent.Button.Button == 1)
            {
                DispatchPointer(layout, windowEvent.Button.X, windowEvent.Button.Y);
            }
        }

        return true;
    }

    private static void DispatchPointer(EditorShellLayout layout, float pointerX, float pointerY)
    {
        var hit = layout.CreateVisualRegions()
            .FirstOrDefault(region =>
                region.Clickable &&
                pointerX >= region.X &&
                pointerX < region.X + region.Width &&
                pointerY >= region.Y &&
                pointerY < region.Y + region.Height);

        if (hit?.Area == "WorkspaceSwitcher")
        {
            layout.SwitchWorkspace(hit.Label);
        }
        else if (hit?.Area == "BottomPanelToggle")
        {
            layout.ToggleBottomPanel();
        }
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
                SDL.PixelFormat.ABGR8888,
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
