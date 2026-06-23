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
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(RuntimeWindowCollection.Name)]
public sealed class RuntimeHostTests
{
    private const string RgbaPng2x2Base64 = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFUlEQVR4nGP4z8DwHwhB4D8QMDQAAD1VB3peF7pjAAAAAElFTkSuQmCC";

    [Fact]
    public void RuntimeHostRunsSceneCallbacksPresentsWindowAndCapturesScreenshot()
    {
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"),
            "runtime-host-frame.png");

        var scene = new HostSmokeScene();
        var result = Electron2D.RuntimeHost.Run(
            scene,
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D Runtime Host Test",
                WindowSize = new Electron2D.Vector2I(320, 180),
                FrameLimit = 2,
                FixedDelta = 1d / 60d,
                ScreenshotPath = screenshotPath,
                ClearColor = new Electron2D.Color(0.02f, 0.03f, 0.04f, 1f)
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);
        Assert.True(result.WindowCreated);
        Assert.True(result.WindowShown);
        Assert.True(result.FramePresented);
        Assert.True(result.EventPumpObserved);
        Assert.Equal(2, result.FrameCount);
        Assert.True(result.DrawCommands >= 2);
        Assert.True(result.ScreenshotSaved);
        Assert.Equal(screenshotPath, result.ScreenshotPath);
        Assert.True(File.Exists(screenshotPath), $"Missing runtime host screenshot: {screenshotPath}");
        Assert.True(scene.ReadyCalled);
        Assert.True(scene.ProcessFrames >= 1);
        Assert.True(scene.PhysicsFrames >= 1);
        Assert.True(scene.DrawCalls >= 1);

        var (width, height) = ReadPngDimensions(File.ReadAllBytes(screenshotPath));
        Assert.Equal(320, width);
        Assert.Equal(180, height);
    }

    [Fact]
    public void RuntimeHostDoesNotExportOutOfProfileApplicationApi()
    {
        var exportedTypeNames = typeof(Electron2D.Object).Assembly.GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        Assert.DoesNotContain("Electron2D.Electron2DApplication", exportedTypeNames);
        Assert.DoesNotContain("Electron2D.Electron2DRunOptions", exportedTypeNames);
        Assert.DoesNotContain("Electron2D.Electron2DRunResult", exportedTypeNames);
    }

    [Fact]
    public void RuntimeHostRendersImageTexturePixelsInScreenshot()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var texturePath = Path.Combine(directory, "texture.png");
        var screenshotPath = Path.Combine(directory, "image-texture-frame.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));

        var scene = new ImageTextureSmokeScene(Electron2D.ImageTexture.LoadFromFile(texturePath));
        var result = Electron2D.RuntimeHost.Run(
            scene,
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D ImageTexture Host Test",
                WindowSize = new Electron2D.Vector2I(24, 18),
                FrameLimit = 1,
                FixedDelta = 1d / 60d,
                ScreenshotPath = screenshotPath,
                ClearColor = Electron2D.Color.Black
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);
        Assert.True(result.ScreenshotSaved);

        var (width, height, rgba) = DecodeRuntimePngRgba(File.ReadAllBytes(screenshotPath));
        Assert.Equal(24, width);
        Assert.Equal(18, height);
        Assert.Equal((255, 0, 0, 255), GetPixel(rgba, width, 8, 7));
        Assert.Equal((0, 0, 0, 255), GetPixel(rgba, width, 9, 7));
        Assert.Equal((0, 0, 255, 255), GetPixel(rgba, width, 8, 8));
        var yellowBlend = GetPixel(rgba, width, 9, 8);
        Assert.InRange(yellowBlend.R, 120, 135);
        Assert.InRange(yellowBlend.G, 120, 135);
        Assert.Equal(0, yellowBlend.B);
        Assert.Equal(255, yellowBlend.A);
    }

    [Fact]
    public void RuntimeHostSetsRootViewportSizeBeforeReady()
    {
        var scene = new ViewportSizeProbeScene();
        var result = Electron2D.RuntimeHost.Run(
            scene,
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D Viewport Size Host Test",
                WindowSize = new Electron2D.Vector2I(320, 180),
                FrameLimit = 1,
                FixedDelta = 1d / 60d,
                ClearColor = Electron2D.Color.Black
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);
        Assert.Equal(new Electron2D.Vector2I(320, 180), scene.ReadyViewportSize);
        Assert.Equal(new Electron2D.Vector2I(320, 180), scene.ProcessViewportSize);
    }

    [Fact]
    public void RuntimeHostUsesByteOrderedRgbaWindowSurface()
    {
        var root = FindRepositoryRoot();
        var hostPath = Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeHost.cs");
        var hostSource = File.ReadAllText(hostPath);

        Assert.Contains("SDL.PixelFormat.ABGR8888", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.PixelFormat.RGBA8888", hostSource, StringComparison.Ordinal);
        Assert.Contains("internal static class RuntimeHost", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("public static class Electron2DApplication", hostSource, StringComparison.Ordinal);
    }

    private static (int Width, int Height) ReadPngDimensions(byte[] bytes)
    {
        Assert.True(bytes.Length >= 24, "PNG is too small.");
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, bytes[..8]);
        var width = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        return (width, height);
    }

    private static (int Width, int Height, byte[] Rgba) DecodeRuntimePngRgba(byte[] bytes)
    {
        var (width, height) = ReadPngDimensions(bytes);
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset + 12 <= bytes.Length)
        {
            var length = System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4));
            var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4);
            offset += 8;
            if (type == "IDAT")
            {
                idat.Write(bytes, offset, length);
            }

            offset += length + 4;
            if (type == "IEND")
            {
                break;
            }
        }

        idat.Position = 0;
        using var zlib = new System.IO.Compression.ZLibStream(idat, System.IO.Compression.CompressionMode.Decompress);
        using var raw = new MemoryStream();
        zlib.CopyTo(raw);
        var scanlines = raw.ToArray();
        var stride = width * 4;
        Assert.Equal(height * (stride + 1), scanlines.Length);

        var rgba = new byte[width * height * 4];
        for (var row = 0; row < height; row++)
        {
            var scanlineOffset = row * (stride + 1);
            Assert.Equal(0, scanlines[scanlineOffset]);
            Buffer.BlockCopy(scanlines, scanlineOffset + 1, rgba, row * stride, stride);
        }

        return (width, height, rgba);
    }

    private static (int R, int G, int B, int A) GetPixel(byte[] rgba, int width, int x, int y)
    {
        var index = ((y * width) + x) * 4;
        return (rgba[index], rgba[index + 1], rgba[index + 2], rgba[index + 3]);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "src", "Electron2D.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Electron2D repository root was not found.");
    }

    private sealed class ViewportSizeProbeScene : Electron2D.Node2D
    {
        public Electron2D.Vector2I ReadyViewportSize { get; private set; }

        public Electron2D.Vector2I ProcessViewportSize { get; private set; }

        public override void _Ready()
        {
            ReadyViewportSize = ((Electron2D.Viewport)GetViewport()!).Size;
        }

        public override void _Process(double delta)
        {
            _ = delta;
            ProcessViewportSize = ((Electron2D.Viewport)GetViewport()!).Size;
            QueueFree();
        }
    }

    private sealed class HostSmokeScene : Electron2D.Node2D
    {
        private readonly HostSmokeFont font = new();

        public bool ReadyCalled { get; private set; }

        public int ProcessFrames { get; private set; }

        public int PhysicsFrames { get; private set; }

        public int DrawCalls { get; private set; }

        public override void _Ready()
        {
            ReadyCalled = true;
        }

        public override void _Process(double delta)
        {
            _ = delta;
            ProcessFrames++;
            QueueRedraw();
        }

        public override void _PhysicsProcess(double delta)
        {
            _ = delta;
            PhysicsFrames++;
        }

        public override void _Draw()
        {
            DrawCalls++;
            DrawRect(new Electron2D.Rect2(24f, 24f, 272f, 132f), new Electron2D.Color(0.16f, 0.28f, 0.48f, 1f));
            DrawRect(new Electron2D.Rect2(40f, 48f, 120f, 48f), new Electron2D.Color(0.95f, 0.80f, 0.32f, 1f));
            DrawString(font, new Electron2D.Vector2(48f, 126f), "RUNTIME HOST", fontSize: 16);
        }
    }

    private sealed class ImageTextureSmokeScene(Electron2D.Texture2D texture) : Electron2D.Node2D
    {
        public override void _Process(double delta)
        {
            _ = delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawTexture(texture, new Electron2D.Vector2(8f, 7f));
        }
    }

    private sealed class HostSmokeFont : Electron2D.Font;
}
