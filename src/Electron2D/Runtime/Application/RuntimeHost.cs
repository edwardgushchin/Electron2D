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
using System.Buffers.Binary;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using SDL3;

namespace Electron2D;

internal static class RuntimeHost
{
    /// <summary>
    /// Runs a main scene node in a visible Electron2D runtime window.
    /// </summary>
    ///
    /// <param name="mainScene">The root node for the game scene.</param>
    /// <param name="options">Optional runtime host settings.</param>
    ///
    /// <returns>
    /// A result describing the created window, presented frames and optional
    /// screenshot.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// This overload creates a new <see cref="SceneTree"/>, adds
    /// <paramref name="mainScene"/> under the root viewport and then delegates to
    /// <see cref="Run(SceneTree, RuntimeHostOptions?)"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="mainScene"/> is <c>null</c>.
    /// </exception>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options"/> contains invalid values.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main game thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Run(SceneTree, RuntimeHostOptions?)"/>
    public static RuntimeHostResult Run(Node mainScene, RuntimeHostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(mainScene);

        var runOptions = options ?? new RuntimeHostOptions();
        ValidateOptions(runOptions);

        var tree = new SceneTree();
        SetRootViewportSize(tree, runOptions.WindowSize);
        tree.Root.AddChild(mainScene);
        return Run(tree, runOptions);
    }

    /// <summary>
    /// Runs an existing scene tree in a visible Electron2D runtime window.
    /// </summary>
    ///
    /// <param name="sceneTree">The scene tree to advance and present.</param>
    /// <param name="options">Optional runtime host settings.</param>
    ///
    /// <returns>
    /// A result describing the created window, presented frames and optional
    /// screenshot.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// A positive <see cref="RuntimeHostOptions.FrameLimit"/> runs a bounded
    /// smoke loop and exits automatically. A frame limit of <c>0</c> keeps the
    /// game running until the user closes the window or, when enabled, presses
    /// Escape.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="sceneTree"/> is <c>null</c>.
    /// </exception>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="options"/> contains invalid values.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main game thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Run(Node, RuntimeHostOptions?)"/>
    public static RuntimeHostResult Run(SceneTree sceneTree, RuntimeHostOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(sceneTree);
        var runOptions = options ?? new RuntimeHostOptions();
        ValidateOptions(runOptions);
        SetRootViewportSize(sceneTree, runOptions.WindowSize);

        var initialized = SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events);
        if (!initialized)
        {
            return CreateFailureResult(runOptions, "Runtime window initialization failed: " + SDL.GetError());
        }

        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                runOptions.WindowTitle,
                runOptions.WindowSize.X,
                runOptions.WindowSize.Y,
                SDL.WindowFlags.Resizable);
            if (window == IntPtr.Zero)
            {
                return CreateFailureResult(runOptions, "Runtime window creation failed: " + SDL.GetError());
            }

            var shown = SDL.ShowWindow(window);
            var targetFrames = runOptions.FrameLimit == 0 ? int.MaxValue : runOptions.FrameLimit;
            var closeRequested = false;
            var eventPumpObserved = false;
            var framePresented = false;
            var inputEventsDispatched = 0;
            var frameCount = 0;
            var finalDrawCommands = 0;
            RuntimePixelCanvas? lastCanvas = null;

            while (!closeRequested && frameCount < targetFrames)
            {
                eventPumpObserved = true;
                inputEventsDispatched += DispatchPendingInput(sceneTree, runOptions.QuitOnEscape, ref closeRequested);

                sceneTree.PhysicsFrame(runOptions.FixedDelta);
                sceneTree.ProcessFrame(runOptions.FixedDelta);
                inputEventsDispatched += DispatchScriptedInput(
                    sceneTree,
                    runOptions,
                    frameCount,
                    runOptions.WindowSize.X,
                    runOptions.WindowSize.Y);

                var plan = new CanvasSubmissionContext().BuildPlan(sceneTree.Root);
                finalDrawCommands = plan.Commands.Count;
                lastCanvas = RuntimePreviewFrameRasterizer.Render(
                    plan,
                    runOptions.WindowSize.X,
                    runOptions.WindowSize.Y,
                    runOptions.ClearColor);
                framePresented = PresentFrame(window, lastCanvas) || framePresented;
                frameCount++;

                if (runOptions.FrameLimit == 0)
                {
                    Thread.Sleep(16);
                }
            }

            var screenshotSaved = SaveScreenshotIfRequested(runOptions.ScreenshotPath, lastCanvas);
            var windowWidth = runOptions.WindowSize.X;
            var windowHeight = runOptions.WindowSize.Y;
            _ = SDL.GetWindowSize(window, out windowWidth, out windowHeight);

            var pixelWidth = runOptions.WindowSize.X;
            var pixelHeight = runOptions.WindowSize.Y;
            _ = SDL.GetWindowSizeInPixels(window, out pixelWidth, out pixelHeight);

            return new RuntimeHostResult(
                succeeded: framePresented,
                windowCreated: true,
                windowShown: shown,
                framePresented,
                eventPumpObserved,
                inputEventsDispatched,
                frameCount,
                finalDrawCommands,
                windowWidth,
                windowHeight,
                pixelWidth,
                pixelHeight,
                SDL.GetCurrentVideoDriver() ?? "unknown",
                string.IsNullOrWhiteSpace(runOptions.ScreenshotPath) ? null : runOptions.ScreenshotPath,
                screenshotSaved,
                framePresented ? string.Empty : "Runtime host did not present a frame.");
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

    private static void ValidateOptions(RuntimeHostOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(options.WindowTitle);
        if (options.WindowSize.X <= 0 || options.WindowSize.Y <= 0)
        {
            throw new ArgumentException("Runtime window size must be positive.", nameof(options));
        }

        if (options.FrameLimit < 0)
        {
            throw new ArgumentException("Runtime frame limit cannot be negative.", nameof(options));
        }

        if (options.FixedDelta <= 0d || !double.IsFinite(options.FixedDelta))
        {
            throw new ArgumentException("Runtime fixed delta must be a positive finite value.", nameof(options));
        }

        if (!float.IsFinite(options.ClearColor.R) ||
            !float.IsFinite(options.ClearColor.G) ||
            !float.IsFinite(options.ClearColor.B) ||
            !float.IsFinite(options.ClearColor.A))
        {
            throw new ArgumentException("Runtime clear color must contain finite components.", nameof(options));
        }
    }

    private static void SetRootViewportSize(SceneTree sceneTree, Vector2I size)
    {
        if (sceneTree.Root is Viewport viewport)
        {
            viewport.Size = size;
        }
    }

    private static RuntimeHostResult CreateFailureResult(RuntimeHostOptions options, string message)
    {
        return new RuntimeHostResult(
            succeeded: false,
            windowCreated: false,
            windowShown: false,
            framePresented: false,
            eventPumpObserved: false,
            inputEventsDispatched: 0,
            frameCount: 0,
            drawCommands: 0,
            options.WindowSize.X,
            options.WindowSize.Y,
            options.WindowSize.X,
            options.WindowSize.Y,
            SDL.GetCurrentVideoDriver() ?? "unknown",
            string.IsNullOrWhiteSpace(options.ScreenshotPath) ? null : options.ScreenshotPath,
            screenshotSaved: false,
            message);
    }

    private static int DispatchPendingInput(SceneTree tree, bool quitOnEscape, ref bool closeRequested)
    {
        SDL.PumpEvents();
        var dispatched = 0;
        while (SDL.PollEvent(out var sdlEvent))
        {
            SdlInputEventMapper.ProcessDeviceState(sdlEvent);
            var eventType = (SDL.EventType)sdlEvent.Type;
            if (eventType is SDL.EventType.Quit or SDL.EventType.WindowCloseRequested)
            {
                closeRequested = true;
                continue;
            }

            foreach (var inputEvent in SdlInputEventMapper.Map(sdlEvent))
            {
                if (quitOnEscape && inputEvent is InputEventKey { Pressed: true, Keycode: Key.Escape })
                {
                    closeRequested = true;
                }

                tree.DispatchInput(inputEvent);
                dispatched++;
            }
        }

        return dispatched;
    }

    private static int DispatchScriptedInput(
        SceneTree tree,
        RuntimeHostOptions options,
        int frameIndex,
        int windowWidth,
        int windowHeight)
    {
        if (options.ScriptedInputProvider is null)
        {
            return 0;
        }

        var events = options.ScriptedInputProvider(new RuntimeHostScriptedInputContext(
            tree,
            frameIndex,
            new Vector2I(windowWidth, windowHeight)));
        if (events is null || events.Count == 0)
        {
            return 0;
        }

        foreach (var inputEvent in events)
        {
            tree.DispatchInput(inputEvent);
        }

        return events.Count;
    }

    private static bool PresentFrame(IntPtr window, RuntimePixelCanvas canvas)
    {
        var windowSurface = SDL.GetWindowSurface(window);
        if (windowSurface == IntPtr.Zero)
        {
            throw new InvalidOperationException("Runtime window surface was not available: " + SDL.GetError());
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
                throw new InvalidOperationException("Runtime frame surface creation failed: " + SDL.GetError());
            }

            if (!SDL.BlitSurface(frameSurface, IntPtr.Zero, windowSurface, IntPtr.Zero))
            {
                throw new InvalidOperationException("Runtime frame blit failed: " + SDL.GetError());
            }

            if (!SDL.UpdateWindowSurface(window))
            {
                throw new InvalidOperationException("Runtime window frame presentation failed: " + SDL.GetError());
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

    private static bool SaveScreenshotIfRequested(string? screenshotPath, RuntimePixelCanvas? canvas)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath) || canvas is null)
        {
            return false;
        }

        var fullPath = Path.GetFullPath(screenshotPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(fullPath, RuntimePngEncoder.Encode(canvas.Width, canvas.Height, canvas.Pixels));
        return true;
    }
}

internal static class RuntimePreviewFrameRasterizer
{
    public static RuntimePixelCanvas Render(CanvasItemRenderPlan plan, int width, int height, Color clearColor)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var canvas = new RuntimePixelCanvas(width, height);
        canvas.Clear(ToRgba(clearColor));
        foreach (var command in plan.Commands)
        {
            if (!command.Visible)
            {
                continue;
            }

            var color = ToRgba(command.EffectiveModulate);
            switch (command.Kind)
            {
                case CanvasItemRenderCommandKind.Rect:
                    DrawRect(canvas, command, color);
                    break;
                case CanvasItemRenderCommandKind.Line:
                    DrawLine(canvas, command, color);
                    break;
                case CanvasItemRenderCommandKind.Circle:
                    DrawCircle(canvas, command, color);
                    break;
                case CanvasItemRenderCommandKind.Polygon:
                    DrawPolygon(canvas, command, color);
                    break;
                case CanvasItemRenderCommandKind.String:
                    DrawString(canvas, command, color);
                    break;
                case CanvasItemRenderCommandKind.Texture:
                    DrawTexture(canvas, command, color);
                    break;
                default:
                    DrawTextureFallback(canvas, command, color);
                    break;
            }
        }

        return canvas;
    }

    private static void DrawRect(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba color)
    {
        var rect = Transform(command, command.DestinationRect);
        if (command.Filled)
        {
            canvas.FillRectangle(ToInt(rect.Position.X), ToInt(rect.Position.Y), ToInt(rect.Size.X), ToInt(rect.Size.Y), color);
        }
        else
        {
            canvas.DrawRectangle(ToInt(rect.Position.X), ToInt(rect.Position.Y), ToInt(rect.Size.X), ToInt(rect.Size.Y), color);
        }
    }

    private static void DrawTextureFallback(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba color)
    {
        var rect = Transform(command, command.DestinationRect);
        var fill = color.A == 0 ? new RuntimeRgba(180, 190, 210, 255) : color;
        canvas.FillRectangle(ToInt(rect.Position.X), ToInt(rect.Position.Y), Math.Max(1, ToInt(rect.Size.X)), Math.Max(1, ToInt(rect.Size.Y)), fill);
        canvas.DrawRectangle(ToInt(rect.Position.X), ToInt(rect.Position.Y), Math.Max(1, ToInt(rect.Size.X)), Math.Max(1, ToInt(rect.Size.Y)), new RuntimeRgba(255, 255, 255, 120));
    }

    private static void DrawTexture(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba modulate)
    {
        if (command.Texture is null || !TryResolveImageTexture(command.Texture, command.SourceRect, out var image, out var sourceRect))
        {
            DrawTextureFallback(canvas, command, modulate);
            return;
        }

        var destination = Transform(command, command.DestinationRect);
        var destinationX = ToInt(destination.Position.X);
        var destinationY = ToInt(destination.Position.Y);
        var destinationWidth = Math.Max(1, ToInt(destination.Size.X));
        var destinationHeight = Math.Max(1, ToInt(destination.Size.Y));
        var sourceX = sourceRect.Position.X;
        var sourceY = sourceRect.Position.Y;
        var sourceWidth = Math.Max(1f, sourceRect.Size.X);
        var sourceHeight = Math.Max(1f, sourceRect.Size.Y);

        for (var y = 0; y < destinationHeight; y++)
        {
            var v = (y + 0.5f) / destinationHeight;
            var sampledY = ToInt(sourceY + (command.FlipV ? (1f - v) * sourceHeight : v * sourceHeight) - 0.5f);
            for (var x = 0; x < destinationWidth; x++)
            {
                var u = (x + 0.5f) / destinationWidth;
                var sampledX = ToInt(sourceX + (command.FlipH ? (1f - u) * sourceWidth : u * sourceWidth) - 0.5f);
                if (image.TryGetPixel(sampledX, sampledY, out var pixel))
                {
                    canvas.BlendPixel(destinationX + x, destinationY + y, Modulate(pixel, modulate));
                }
            }
        }
    }

    private static bool TryResolveImageTexture(Texture2D texture, Rect2 requestedSource, out ImageTexture image, out Rect2 sourceRect)
    {
        switch (texture)
        {
            case ImageTexture imageTexture:
                image = imageTexture;
                sourceRect = NormalizeSourceRect(requestedSource, imageTexture.GetWidth(), imageTexture.GetHeight());
                return true;
            case AtlasTexture { Atlas: ImageTexture atlasImage } atlas:
                image = atlasImage;
                var atlasRegion = atlas.GetSourceRegion();
                var requested = NormalizeSourceRect(requestedSource, atlas.GetWidth(), atlas.GetHeight());
                sourceRect = new Rect2(
                    atlasRegion.Position + requested.Position,
                    requested.Size);
                return true;
            default:
                image = null!;
                sourceRect = default;
                return false;
        }
    }

    private static Rect2 NormalizeSourceRect(Rect2 sourceRect, int textureWidth, int textureHeight)
    {
        var width = sourceRect.Size.X > 0f ? sourceRect.Size.X : textureWidth;
        var height = sourceRect.Size.Y > 0f ? sourceRect.Size.Y : textureHeight;
        return new Rect2(sourceRect.Position, new Vector2(width, height));
    }

    private static void DrawLine(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba color)
    {
        if (command.Points.Count < 2)
        {
            return;
        }

        var from = Transform(command, command.Points[0]);
        var to = Transform(command, command.Points[1]);
        canvas.DrawLine(ToInt(from.X), ToInt(from.Y), ToInt(to.X), ToInt(to.Y), color);
    }

    private static void DrawCircle(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba color)
    {
        var center = Transform(command, command.Position);
        canvas.FillCircle(ToInt(center.X), ToInt(center.Y), Math.Max(1, ToInt(command.Radius)), color);
    }

    private static void DrawPolygon(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba color)
    {
        if (command.Points.Count == 0)
        {
            return;
        }

        var points = command.Points.Select(point => Transform(command, point)).ToArray();
        var left = points.Min(point => point.X);
        var top = points.Min(point => point.Y);
        var right = points.Max(point => point.X);
        var bottom = points.Max(point => point.Y);
        canvas.FillRectangle(ToInt(left), ToInt(top), Math.Max(1, ToInt(right - left)), Math.Max(1, ToInt(bottom - top)), color);
    }

    private static void DrawString(RuntimePixelCanvas canvas, CanvasItemRenderCommand command, RuntimeRgba color)
    {
        var position = Transform(command, command.Position);
        var scale = Math.Clamp(command.FontSize / 8, 1, 4);
        canvas.DrawText(command.Text, ToInt(position.X), ToInt(position.Y - (7 * scale)), color, scale);
    }

    private static Rect2 Transform(CanvasItemRenderCommand command, Rect2 rect)
    {
        return new Rect2(Transform(command, rect.Position), rect.Size);
    }

    private static Vector2 Transform(CanvasItemRenderCommand command, Vector2 point)
    {
        return command.Transform.Xform(point);
    }

    private static int ToInt(float value)
    {
        return (int)MathF.Round(value, MidpointRounding.AwayFromZero);
    }

    private static RuntimeRgba ToRgba(Color color)
    {
        return new RuntimeRgba(
            ToByte(color.R),
            ToByte(color.G),
            ToByte(color.B),
            ToByte(color.A));
    }

    private static RuntimeRgba Modulate(RuntimeRgba pixel, RuntimeRgba modulate)
    {
        return new RuntimeRgba(
            Multiply(pixel.R, modulate.R),
            Multiply(pixel.G, modulate.G),
            Multiply(pixel.B, modulate.B),
            Multiply(pixel.A, modulate.A));
    }

    private static byte Multiply(byte left, byte right)
    {
        return (byte)((left * right + 127) / 255);
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp((int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f, MidpointRounding.AwayFromZero), 0, 255);
    }
}

internal readonly record struct RuntimeRgba(byte R, byte G, byte B, byte A = 255);

internal sealed class RuntimePixelCanvas
{
    public RuntimePixelCanvas(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new byte[width * height * 4];
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }

    public void Clear(RuntimeRgba color)
    {
        for (var index = 0; index < Pixels.Length; index += 4)
        {
            Pixels[index] = color.R;
            Pixels[index + 1] = color.G;
            Pixels[index + 2] = color.B;
            Pixels[index + 3] = color.A;
        }
    }

    public void FillRectangle(int x, int y, int width, int height, RuntimeRgba color)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (var row = Math.Max(0, y); row < Math.Min(Height, y + height); row++)
        {
            for (var column = Math.Max(0, x); column < Math.Min(Width, x + width); column++)
            {
                SetPixel(column, row, color);
            }
        }
    }

    public void DrawRectangle(int x, int y, int width, int height, RuntimeRgba color)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        for (var column = x; column < x + width; column++)
        {
            SetPixel(column, y, color);
            SetPixel(column, y + height - 1, color);
        }

        for (var row = y; row < y + height; row++)
        {
            SetPixel(x, row, color);
            SetPixel(x + width - 1, row, color);
        }
    }

    public void DrawLine(int x0, int y0, int x1, int y1, RuntimeRgba color)
    {
        var dx = Math.Abs(x1 - x0);
        var sx = x0 < x1 ? 1 : -1;
        var dy = -Math.Abs(y1 - y0);
        var sy = y0 < y1 ? 1 : -1;
        var error = dx + dy;

        while (true)
        {
            SetPixel(x0, y0, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            var e2 = 2 * error;
            if (e2 >= dy)
            {
                error += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                error += dx;
                y0 += sy;
            }
        }
    }

    public void FillCircle(int centerX, int centerY, int radius, RuntimeRgba color)
    {
        var radiusSquared = radius * radius;
        for (var y = centerY - radius; y <= centerY + radius; y++)
        {
            for (var x = centerX - radius; x <= centerX + radius; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                if ((dx * dx) + (dy * dy) <= radiusSquared)
                {
                    SetPixel(x, y, color);
                }
            }
        }
    }

    public void DrawText(string text, int x, int y, RuntimeRgba color, int scale)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (scale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(scale), scale, "Pixel font scale must be positive.");
        }

        var cursor = x;
        foreach (var character in text.ToUpperInvariant())
        {
            RuntimePixelFont.DrawCharacter(this, character, cursor, y, color, scale);
            cursor += 6 * scale;
        }
    }

    public void BlendPixel(int x, int y, RuntimeRgba color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || color.A == 0)
        {
            return;
        }

        if (color.A == byte.MaxValue)
        {
            SetPixel(x, y, color);
            return;
        }

        var index = ((y * Width) + x) * 4;
        var inverseAlpha = byte.MaxValue - color.A;
        Pixels[index] = BlendChannel(color.R, color.A, Pixels[index], inverseAlpha);
        Pixels[index + 1] = BlendChannel(color.G, color.A, Pixels[index + 1], inverseAlpha);
        Pixels[index + 2] = BlendChannel(color.B, color.A, Pixels[index + 2], inverseAlpha);
        Pixels[index + 3] = (byte)Math.Min(byte.MaxValue, color.A + ((Pixels[index + 3] * inverseAlpha + 127) / 255));
    }

    private void SetPixel(int x, int y, RuntimeRgba color)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return;
        }

        var index = ((y * Width) + x) * 4;
        Pixels[index] = color.R;
        Pixels[index + 1] = color.G;
        Pixels[index + 2] = color.B;
        Pixels[index + 3] = color.A;
    }

    private static byte BlendChannel(byte source, byte sourceAlpha, byte destination, int inverseAlpha)
    {
        return (byte)Math.Clamp(((source * sourceAlpha) + (destination * inverseAlpha) + 127) / 255, 0, 255);
    }
}

internal static class RuntimePixelFont
{
    public static void DrawCharacter(RuntimePixelCanvas canvas, char character, int x, int y, RuntimeRgba color, int scale)
    {
        var glyph = GetGlyph(character);
        for (var row = 0; row < glyph.Length; row++)
        {
            for (var column = 0; column < glyph[row].Length; column++)
            {
                if (glyph[row][column] == '1')
                {
                    canvas.FillRectangle(x + (column * scale), y + (row * scale), scale, scale, color);
                }
            }
        }
    }

    private static string[] GetGlyph(char character)
    {
        return character switch
        {
            'A' => ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
            'B' => ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
            'C' => ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
            'D' => ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
            'E' => ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
            'F' => ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
            'G' => ["01111", "10000", "10000", "10011", "10001", "10001", "01111"],
            'H' => ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
            'I' => ["11111", "00100", "00100", "00100", "00100", "00100", "11111"],
            'J' => ["00111", "00010", "00010", "00010", "10010", "10010", "01100"],
            'K' => ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
            'L' => ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
            'M' => ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
            'N' => ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
            'O' => ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
            'P' => ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
            'Q' => ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
            'R' => ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
            'S' => ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
            'T' => ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
            'U' => ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
            'V' => ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
            'W' => ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
            'X' => ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
            'Y' => ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
            'Z' => ["11111", "00001", "00010", "00100", "01000", "10000", "11111"],
            '0' => ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
            '1' => ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
            '2' => ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
            '3' => ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
            '4' => ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
            '5' => ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
            '6' => ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
            '7' => ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
            '8' => ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
            '9' => ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
            ':' => ["00000", "00100", "00100", "00000", "00100", "00100", "00000"],
            '.' => ["00000", "00000", "00000", "00000", "00000", "01100", "01100"],
            ',' => ["00000", "00000", "00000", "00000", "00100", "00100", "01000"],
            '-' => ["00000", "00000", "00000", "11111", "00000", "00000", "00000"],
            '/' => ["00001", "00010", "00010", "00100", "01000", "01000", "10000"],
            '+' => ["00000", "00100", "00100", "11111", "00100", "00100", "00000"],
            '|' => ["00100", "00100", "00100", "00100", "00100", "00100", "00100"],
            ' ' => ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
            _ => ["11111", "10001", "00010", "00100", "00000", "00100", "00100"]
        };
    }
}

internal static class RuntimePngEncoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];
    private static readonly uint[] CrcTable = CreateCrcTable();

    public static byte[] Encode(int width, int height, byte[] rgbaPixels)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), width, "PNG dimensions must be positive.");
        }

        if (rgbaPixels.Length != width * height * 4)
        {
            throw new ArgumentException("RGBA pixel buffer length does not match PNG dimensions.", nameof(rgbaPixels));
        }

        using var stream = new MemoryStream();
        stream.Write(Signature);

        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(4, 4), height);
        ihdr[8] = 8;
        ihdr[9] = 6;
        ihdr[10] = 0;
        ihdr[11] = 0;
        ihdr[12] = 0;
        WriteChunk(stream, "IHDR", ihdr);

        WriteChunk(stream, "IDAT", CompressScanlines(width, height, rgbaPixels));
        WriteChunk(stream, "IEND", ReadOnlySpan<byte>.Empty);
        return stream.ToArray();
    }

    private static byte[] CompressScanlines(int width, int height, byte[] rgbaPixels)
    {
        using var raw = new MemoryStream();
        var stride = width * 4;
        for (var row = 0; row < height; row++)
        {
            raw.WriteByte(0);
            raw.Write(rgbaPixels.AsSpan(row * stride, stride));
        }

        using var compressed = new MemoryStream();
        raw.Position = 0;
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            raw.CopyTo(zlib);
        }

        return compressed.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = UpdateCrc(UpdateCrc(0xffffffffu, typeBytes), data) ^ 0xffffffffu;
        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, crc);
        stream.Write(crcBytes);
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            crc = CrcTable[(crc ^ value) & 0xff] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] CreateCrcTable()
    {
        var table = new uint[256];
        for (uint index = 0; index < table.Length; index++)
        {
            var crc = index;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            }

            table[index] = crc;
        }

        return table;
    }
}
