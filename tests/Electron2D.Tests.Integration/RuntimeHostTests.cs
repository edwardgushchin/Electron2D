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
using SDL3;
using System.Runtime.InteropServices;
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(RuntimeWindowCollection.Name)]
public sealed class RuntimeHostTests
{
    private const string RgbaPng2x2Base64 = "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAFUlEQVR4nGP4z8DwHwhB4D8QMDQAAD1VB3peF7pjAAAAAElFTkSuQmCC";

    [Fact]
    [Trait("Category", "Baseline")]
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
        Assert.True(GetResultProperty<long>(result, "CapturePresenterManagedBytesAllocated") > 0);
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
    [Trait("Category", "Baseline")]
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
    [Trait("Category", "Baseline")]
    public void RuntimeHostRendersTextAsGlyphsInScreenshot()
    {
        var screenshotPath = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"),
            "runtime-host-text-glyph.png");

        var result = Electron2D.RuntimeHost.Run(
            new TextGlyphScene(),
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D Runtime Host Glyph Test",
                WindowSize = new Electron2D.Vector2I(32, 24),
                FrameLimit = 1,
                FixedDelta = 1d / 60d,
                ScreenshotPath = screenshotPath,
                ClearColor = Electron2D.Color.Black
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);
        Assert.True(result.ScreenshotSaved);

        var (width, _, rgba) = DecodeRuntimePngRgba(File.ReadAllBytes(screenshotPath));
        Assert.Equal((255, 255, 255, 255), GetPixel(rgba, width, 4, 5));
        Assert.Equal((0, 0, 0, 255), GetPixel(rgba, width, 6, 5));
        Assert.Equal((255, 255, 255, 255), GetPixel(rgba, width, 8, 5));
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeHostRealPresenterReportsZeroTextAllocationsAcrossMeasuredSteadyFrames()
    {
        var result = Electron2D.RuntimeHost.Run(
            new TextGlyphScene(),
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D Runtime Host Text Allocation Test",
                WindowSize = new Electron2D.Vector2I(32, 24),
                FrameLimit = 720,
                FixedDelta = 1d / 60d,
                ClearColor = Electron2D.Color.Black
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);
        Assert.Equal("SDL_GPU", GetResultProperty<string>(result, "PresentationBackend"));
        Assert.False(GetResultProperty<bool>(result, "UsedFallbackPresenter"));
        Assert.Equal(0, GetResultProperty<long>(result, "MaxPresenterManagedBytesPerFrame"));
        Assert.Equal(600, GetResultProperty<int>(result, "PresenterMeasuredFrames"));
        Assert.Equal(0, GetResultProperty<long>(result, "CapturePresenterManagedBytesAllocated"));
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeHostVisualFixtureCoversTextureFlipTransformsCirclePolygonAndGlyphs()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var texturePath = Path.Combine(directory, "visual-fixture-texture.png");
        var screenshotPath = Path.Combine(directory, "visual-fixture-frame.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));

        var result = Electron2D.RuntimeHost.Run(
            new VisualFixtureScene(Electron2D.ImageTexture.LoadFromFile(texturePath)),
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D Runtime Host Visual Fixture Test",
                WindowSize = new Electron2D.Vector2I(64, 64),
                FrameLimit = 1,
                FixedDelta = 1d / 60d,
                ScreenshotPath = screenshotPath,
                ClearColor = Electron2D.Color.Black
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);
        Assert.True(result.ScreenshotSaved);
        Assert.Equal("SDL_GPU", GetResultProperty<string>(result, "PresentationBackend"));
        Assert.False(GetResultProperty<bool>(result, "UsedFallbackPresenter"));

        var (width, _, rgba) = DecodeRuntimePngRgba(File.ReadAllBytes(screenshotPath));
        AssertVisualFixturePixels(width, rgba);
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeSdlRendererFallbackVisualFixtureCoversTextureFlipTransformsCirclePolygonAndGlyphs()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var texturePath = Path.Combine(directory, "fallback-visual-fixture-texture.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));
        var tree = new Electron2D.SceneTree();
        tree.Root.AddChild(new VisualFixtureScene(Electron2D.ImageTexture.LoadFromFile(texturePath)));
        tree.ProcessFrame(1d / 60d);
        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root);

        Assert.True(SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events), SDL.GetError());
        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                "Electron2D Runtime SDL Renderer Visual Fixture Test",
                64,
                64,
                SDL.WindowFlags.Hidden);
            Assert.NotEqual(IntPtr.Zero, window);

            using var presenter = new Electron2D.RuntimeSdlRendererFramePresenter(window, new Electron2D.Vector2I(64, 64));
            var frame = presenter.Present(plan, new Electron2D.Vector2I(64, 64), Electron2D.Color.Black, captureFrame: true);

            Assert.NotNull(frame.Screenshot);
            Assert.Equal(64, frame.Screenshot.Width);
            Assert.Equal(64, frame.Screenshot.Height);
            AssertVisualFixturePixels(frame.Screenshot.Width, frame.Screenshot.RgbaPixels);
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

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeSdlRendererFallbackTextureCacheReloadsWhenAtlasVersionChanges()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var texturePath = Path.Combine(directory, "fallback-cache-texture.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));
        var atlas = new Electron2D.AtlasTexture
        {
            Atlas = Electron2D.ImageTexture.LoadFromFile(texturePath),
            Region = new Electron2D.Rect2(0f, 0f, 2f, 2f)
        };
        var tree = new Electron2D.SceneTree();
        tree.Root.AddChild(new Electron2D.Sprite2D
        {
            Texture = atlas,
            Centered = false,
            Position = new Electron2D.Vector2(2f, 2f)
        });
        tree.ProcessFrame(1d / 60d);
        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root);

        Assert.True(SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events), SDL.GetError());
        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                "Electron2D Runtime SDL Renderer Texture Cache Test",
                16,
                16,
                SDL.WindowFlags.Hidden);
            Assert.NotEqual(IntPtr.Zero, window);

            using var presenter = new Electron2D.RuntimeSdlRendererFramePresenter(window, new Electron2D.Vector2I(16, 16));
            var first = presenter.Present(plan, new Electron2D.Vector2I(16, 16), Electron2D.Color.Black, captureFrame: false);
            var second = presenter.Present(plan, new Electron2D.Vector2I(16, 16), Electron2D.Color.Black, captureFrame: false);
            atlas.Region = new Electron2D.Rect2(1f, 1f, 1f, 1f);
            var third = presenter.Present(plan, new Electron2D.Vector2I(16, 16), Electron2D.Color.Black, captureFrame: false);

            Assert.Equal(1, first.Diagnostics.TextureUploads);
            Assert.Equal(1, second.Diagnostics.TextureUploads);
            Assert.Equal(1, second.Diagnostics.TextureCacheHits);
            Assert.Equal(2, third.Diagnostics.TextureUploads);
            Assert.Equal(1, third.Diagnostics.TextureCacheHits);
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

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeSdlRendererFallbackReportsObservedResize()
    {
        Assert.True(SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events), SDL.GetError());
        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                "Electron2D Runtime SDL Renderer Resize Test",
                16,
                16,
                SDL.WindowFlags.Hidden | SDL.WindowFlags.Resizable);
            Assert.NotEqual(IntPtr.Zero, window);

            using var presenter = new Electron2D.RuntimeSdlRendererFramePresenter(window, new Electron2D.Vector2I(16, 16));
            var plan = new Electron2D.CanvasItemRenderPlan(
                Array.Empty<Electron2D.CanvasItemRenderCommand>(),
                Array.Empty<Electron2D.CanvasItemRenderBatch>());
            _ = presenter.Present(plan, new Electron2D.Vector2I(16, 16), Electron2D.Color.Black, captureFrame: false);

            SDL.SetWindowSize(window, 24, 20);
            var resized = presenter.Present(plan, new Electron2D.Vector2I(24, 20), Electron2D.Color.Black, captureFrame: false);

            Assert.Equal(0, resized.Diagnostics.PresentationResourcesRecreated);
            Assert.Equal(1, resized.Diagnostics.ObservedPresentationResizes);
            Assert.Equal(0, resized.Diagnostics.PresentationBackendReconfigurations);
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

    [Fact]
    public void RuntimeSdlRendererFallbackThrowsForUnsupportedTextureResource()
    {
        Assert.True(SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events), SDL.GetError());
        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                "Electron2D Runtime SDL Renderer Unsupported Texture Test",
                16,
                16,
                SDL.WindowFlags.Hidden);
            Assert.NotEqual(IntPtr.Zero, window);

            using var presenter = new Electron2D.RuntimeSdlRendererFramePresenter(window, new Electron2D.Vector2I(16, 16));
            var plan = CreateUnsupportedTexturePlan();

            Assert.Throws<Electron2D.UnsupportedTextureResourceException>(() => presenter.Present(
                plan,
                new Electron2D.Vector2I(16, 16),
                Electron2D.Color.Black,
                captureFrame: false));
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

    [Fact]
    public void RuntimeSdlRendererFallbackThrowsForUnknownRenderCommandKind()
    {
        Assert.True(SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events), SDL.GetError());
        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                "Electron2D Runtime SDL Renderer Unknown Command Test",
                16,
                16,
                SDL.WindowFlags.Hidden);
            Assert.NotEqual(IntPtr.Zero, window);

            using var presenter = new Electron2D.RuntimeSdlRendererFramePresenter(window, new Electron2D.Vector2I(16, 16));
            var plan = CreateUnknownCommandPlan();

            Assert.Throws<InvalidOperationException>(() => presenter.Present(
                plan,
                new Electron2D.Vector2I(16, 16),
                Electron2D.Color.Black,
                captureFrame: false));
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

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeGpuPresenterRollsBackStagedTextureUploadWhenFrameFailsBeforeCommit()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var texturePath = Path.Combine(directory, "gpu-staged-texture.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));
        var texture = Electron2D.ImageTexture.LoadFromFile(texturePath);

        var terminalApi = new PresenterFaultSdlGpuApi();
        var backend = CreateInitializedGpuBackend(terminalApi);
        var gpuApi = new PresenterFaultGpuApi(RuntimeGpuPresenterFailurePoint.None);
        using var presenter = new Electron2D.RuntimeGpuFramePresenter(
            new IntPtr(52),
            new Electron2D.Vector2I(16, 16),
            backend,
            gpuApi,
            presentationResourcesReady: true);

        var plan = CreateTextureThenUnknownCommandPlan(texture);

        Assert.Throws<InvalidOperationException>(() => presenter.Present(
            plan,
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame: false));

        Assert.Equal(0, GetPrivateCollectionCount(presenter, "textureCache"));
        Assert.Equal(0, GetPrivateCollectionCount(presenter, "stagedTextureCache"));
        Assert.Equal(1, terminalApi.SubmitCommandBufferCalls);
        Assert.Equal(0, gpuApi.CancelCommandBufferCalls);
        Assert.Equal(1, gpuApi.GetReleaseCount(gpuApi.LastCreatedTexture));
        Assert.Equal(1, gpuApi.GetTransferReleaseCount(gpuApi.LastCreatedTransferBuffer));
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeHostResultExposesReusableRendererDiagnostics()
    {
        var scene = new HostSmokeScene();
        var result = Electron2D.RuntimeHost.Run(
            scene,
            new Electron2D.RuntimeHostOptions
            {
                WindowTitle = "Electron2D Runtime Host Diagnostics Test",
                WindowSize = new Electron2D.Vector2I(96, 54),
                FrameLimit = 2,
                FixedDelta = 1d / 60d,
                ClearColor = Electron2D.Color.Black
            });

        Assert.True(result.Succeeded, result.DiagnosticMessage);

        Assert.Equal("RenderingServer", GetResultProperty<string>(result, "RenderSource"));
        Assert.Equal("SDL_GPU", GetResultProperty<string>(result, "PresentationBackend"));
        Assert.False(GetResultProperty<bool>(result, "UsedFallbackPresenter"));
        Assert.Equal(string.Empty, GetResultProperty<string>(result, "FallbackReason"));
        Assert.True(GetResultProperty<int>(result, "RenderBatches") > 0);
        Assert.True(GetResultProperty<int>(result, "TextureSwitches") >= 0);
        Assert.True(GetResultProperty<int>(result, "PipelineSwitches") >= 0);
        Assert.Equal(0, GetResultProperty<int>(result, "TextureUploads"));
        Assert.True(GetResultProperty<int>(result, "TextureCacheHits") >= 0);
        Assert.Equal(1, GetResultProperty<int>(result, "PresentationResourcesCreated"));
        Assert.Equal(0, GetResultProperty<int>(result, "PresentationResourcesRecreated"));
        Assert.Equal(0, GetResultProperty<int>(result, "ObservedPresentationResizes"));
        Assert.Equal(0, GetResultProperty<int>(result, "PresentationBackendReconfigurations"));
        Assert.Equal(0, GetResultProperty<long>(result, "MaxPresenterManagedBytesPerFrame"));
        Assert.Equal(0, GetResultProperty<int>(result, "PresenterMeasuredFrames"));
        Assert.Equal(0, GetResultProperty<long>(result, "CapturePresenterManagedBytesAllocated"));
    }

    [Fact]
    public void RuntimeHostResultExposesActualDrawCallsDiagnostic()
    {
        Assert.NotNull(typeof(Electron2D.RuntimeHostResult).GetProperty("ActualDrawCalls"));
        Assert.NotNull(typeof(Electron2D.RuntimeHostResult).GetProperty("PipelineSwitches"));
        Assert.NotNull(typeof(Electron2D.RuntimeHostResult).GetProperty("ObservedPresentationResizes"));
        Assert.NotNull(typeof(Electron2D.RuntimeHostResult).GetProperty("PresentationBackendReconfigurations"));
    }

    [Fact]
    public void RuntimeFrameDiagnosticsCountsTextureAndPipelineBarriersSeparately()
    {
        var textureKey = new Electron2D.CanvasItemBatchKey(
            new Electron2D.Rid(10),
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix);
        var solidKey = new Electron2D.CanvasItemBatchKey(
            texture: default,
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix,
            Electron2D.CanvasItemRenderCommandKind.Rect);
        var plan = new Electron2D.CanvasItemRenderPlan(
            new[]
            {
                CreateDiagnosticCommand(textureKey, Electron2D.CanvasItemRenderCommandKind.Texture, treeOrder: 0),
                CreateDiagnosticCommand(solidKey, Electron2D.CanvasItemRenderCommandKind.Rect, treeOrder: 1),
                CreateDiagnosticCommand(textureKey, Electron2D.CanvasItemRenderCommandKind.Texture, treeOrder: 2)
            },
            new[]
            {
                new Electron2D.CanvasItemRenderBatch(textureKey, startIndex: 0, count: 1),
                new Electron2D.CanvasItemRenderBatch(solidKey, startIndex: 1, count: 1),
                new Electron2D.CanvasItemRenderBatch(textureKey, startIndex: 2, count: 1)
            });

        var presenterType = typeof(Electron2D.RuntimeGpuFramePresenter);
        var countTextures = presenterType.GetMethod(
            "CountTextureSwitches",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var countPipelines = presenterType.GetMethod(
            "CountPipelineSwitches",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        Assert.NotNull(countTextures);
        Assert.NotNull(countPipelines);
        Assert.Equal(2, (int)countTextures.Invoke(null, new object[] { plan })!);
        Assert.Equal(3, (int)countPipelines.Invoke(null, new object[] { plan })!);
    }

    [Fact]
    public void RuntimeTextureResolverSupportsNestedAtlasTextures()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        var texturePath = Path.Combine(directory, "nested-atlas.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));
        var image = Electron2D.ImageTexture.LoadFromFile(texturePath);
        var innerAtlas = new Electron2D.AtlasTexture
        {
            Atlas = image,
            Region = new Electron2D.Rect2(0f, 0f, 2f, 2f)
        };
        var outerAtlas = new Electron2D.AtlasTexture
        {
            Atlas = innerAtlas,
            Region = new Electron2D.Rect2(1f, 1f, 1f, 1f)
        };

        Assert.True(Electron2D.RuntimeTextureResolver.TryCreateTexturePixels(outerAtlas, out var width, out var height, out var pixels));
        Assert.True(image.TryGetPixel(1, 1, out var expected));
        Assert.Equal(1, width);
        Assert.Equal(1, height);
        Assert.Equal(new[] { expected.R, expected.G, expected.B, expected.A }, pixels);
    }

    [Fact]
    public void AtlasTextureRenderContentVersionTracksNestedAtlasChanges()
    {
        var innerAtlas = new Electron2D.AtlasTexture
        {
            Atlas = new EmptyTexture2D(16, 16),
            Region = new Electron2D.Rect2(0f, 0f, 8f, 8f)
        };
        var outerAtlas = new Electron2D.AtlasTexture
        {
            Atlas = innerAtlas,
            Region = new Electron2D.Rect2(1f, 1f, 4f, 4f)
        };

        var version = outerAtlas.RenderContentVersion;
        innerAtlas.Region = new Electron2D.Rect2(2f, 2f, 8f, 8f);

        Assert.NotEqual(version, outerAtlas.RenderContentVersion);
    }

    [Fact]
    [Trait("Category", "Baseline")]
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
    public void RuntimeHostDoesNotUseWindowSurfacePixelUploadForInteractiveFrames()
    {
        var root = FindRepositoryRoot();
        var hostPath = Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeHost.cs");
        var hostSource = File.ReadAllText(hostPath);

        Assert.Contains("RuntimeFramePresenter", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.PixelFormat.ABGR8888", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.PixelFormat.RGBA8888", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.CreateSurfaceFrom", hostSource, StringComparison.Ordinal);
        Assert.Contains("internal static class RuntimeHost", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("public static class Electron2DApplication", hostSource, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeHostInteractivePathDoesNotUsePerFramePreviewFramebuffer()
    {
        var root = FindRepositoryRoot();
        var hostPath = Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeHost.cs");
        var presenterPath = Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeFramePresenter.cs");
        var fallbackPresenterPath = Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeSdlRendererFramePresenter.cs");
        var shaderCrossServicePath = Path.Combine(root, "src", "Electron2D", "Runtime", "Application", "RuntimeShaderCrossService.cs");
        var runtimeCliProgramPath = Path.Combine(root, "src", "Electron2D.Cli", "Program.cs");
        var editorProgramPath = Path.Combine(root, "src", "Electron2D.Editor", "Program.cs");
        var hostSource = File.ReadAllText(hostPath);
        var presenterSource = File.ReadAllText(presenterPath);
        var fallbackPresenterSource = File.ReadAllText(fallbackPresenterPath);
        var shaderCrossServiceSource = File.ReadAllText(shaderCrossServicePath);
        var runtimeCliProgramSource = File.ReadAllText(runtimeCliProgramPath);
        var editorProgramSource = File.ReadAllText(editorProgramPath);

        Assert.Contains("RuntimeFramePresenter", hostSource, StringComparison.Ordinal);
        Assert.Contains("RuntimeGpuFramePresenter", presenterSource, StringComparison.Ordinal);
        Assert.Contains("RuntimeSdlRendererFramePresenter", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimePreviewFrameRasterizer.Render(\r\n                    plan,", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimePreviewFrameRasterizer.Render(\n                    plan,", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimePreviewFrameRasterizer.Render", hostSource, StringComparison.Ordinal);
        Assert.Contains("presentedFrame.Screenshot", hostSource, StringComparison.Ordinal);
        Assert.Contains("ShouldCaptureFrame", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("GCHandle.Alloc(canvas.Pixels", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.CreateSurfaceFrom(\r\n                canvas.Width", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.CreateSurfaceFrom(\n                canvas.Width", hostSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.CreateRenderer", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("SDL.RenderPresent", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.WaitAndAcquireGPUSwapchainTexture", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.BeginGPURenderPass", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.CreateGPUGraphicsPipeline", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.DrawGPUPrimitives", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.DownloadFromGPUTexture", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.MapGPUTransferBuffer", presenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.CreateRenderer", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.RenderPresent", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.RenderReadPixels", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.Contains("SDL.RenderGeometry", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.Contains("command.FlipH", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.Contains("command.FlipV", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.Contains("RuntimePixelFont.GetGlyph", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain(".Select(point => Transform(command, point)).ToArray()", fallbackPresenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("var colorTargets = new[]", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("var vertexBinding = new[]", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("var textureBinding = new[]", presenterSource, StringComparison.Ordinal);
        Assert.Contains("RuntimeGpuCommandBufferFinalizer.CompleteAfterFailure", presenterSource, StringComparison.Ordinal);
        Assert.Contains("RollbackStagedTextureUploads();", presenterSource, StringComparison.Ordinal);
        Assert.Contains("CommitStagedTextureUploads();", presenterSource, StringComparison.Ordinal);
        Assert.Contains("stagedTextureCache.Add(texture, resource);", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("textureCache.Add(texture, resource);", presenterSource, StringComparison.Ordinal);
        Assert.Contains("ReleaseSubmittedTransferBuffers();", presenterSource, StringComparison.Ordinal);
        Assert.Contains("GetNullTerminatedByteLength(code)", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ShaderCross.Init()", presenterSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ShaderCross.Quit()", presenterSource, StringComparison.Ordinal);
        Assert.Contains("ShaderCross.Init()", shaderCrossServiceSource, StringComparison.Ordinal);
        Assert.Contains("ShaderCross.Quit()", shaderCrossServiceSource, StringComparison.Ordinal);
        Assert.Contains("internal static void ShutdownOnRenderThread()", shaderCrossServiceSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RuntimeShaderCrossService.ShutdownOnRenderThread();", hostSource, StringComparison.Ordinal);
        Assert.Contains("RuntimeApplicationServices.ShutdownOnRenderThread();", runtimeCliProgramSource, StringComparison.Ordinal);
        Assert.Contains("RuntimeApplicationServices.ShutdownOnRenderThread();", editorProgramSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ProcessExit", shaderCrossServiceSource, StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeShaderCrossServiceKeepsCompilerAliveUntilApplicationShutdown()
    {
        var api = new FakeRuntimeShaderCrossApi();
        using var scope = Electron2D.RuntimeShaderCrossService.UseApiForTests(api);

        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        Assert.Equal(1, api.InitCalls);
        Assert.Equal(0, api.QuitCalls);

        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();
        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();

        Assert.Equal(1, api.InitCalls);
        Assert.Equal(1, api.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceRejectsShutdownFromNonOwningThread()
    {
        var api = new FakeRuntimeShaderCrossApi();
        using var scope = Electron2D.RuntimeShaderCrossService.UseApiForTests(api);
        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        Exception? shutdownException = null;
        var shutdownThread = new Thread(() =>
        {
            try
            {
                Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();
            }
            catch (Exception exception)
            {
                shutdownException = exception;
            }
        });
        shutdownThread.Start();
        shutdownThread.Join();

        Assert.IsType<InvalidOperationException>(shutdownException);
        Assert.Equal(0, api.QuitCalls);

        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();
        Assert.Equal(1, api.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceRejectsShutdownWhileLeaseIsActive()
    {
        var api = new FakeRuntimeShaderCrossApi();
        using var scope = Electron2D.RuntimeShaderCrossService.UseApiForTests(api);

        using var lease = Electron2D.RuntimeShaderCrossService.Acquire();

        Assert.Throws<InvalidOperationException>(() => Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread());
        Assert.Equal(0, api.QuitCalls);

        lease.Dispose();
        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();
        Assert.Equal(1, api.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceRejectsAcquireAfterApplicationShutdown()
    {
        var api = new FakeRuntimeShaderCrossApi();
        using var scope = Electron2D.RuntimeShaderCrossService.UseApiForTests(api);

        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();

        Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => Electron2D.RuntimeShaderCrossService.Acquire());
        Assert.Equal(1, api.InitCalls);
        Assert.Equal(1, api.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceRejectsAcquireAfterShutdownBeforeFirstAcquire()
    {
        var api = new FakeRuntimeShaderCrossApi();
        using var scope = Electron2D.RuntimeShaderCrossService.UseApiForTests(api);

        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();
        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();

        Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => Electron2D.RuntimeShaderCrossService.Acquire());
        Assert.Equal(0, api.InitCalls);
        Assert.Equal(0, api.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceTestApiScopeRestoresTerminalState()
    {
        var terminalApi = new FakeRuntimeShaderCrossApi();
        using (Electron2D.RuntimeShaderCrossService.UseApiForTests(terminalApi))
        {
            Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();

            Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => Electron2D.RuntimeShaderCrossService.Acquire());
        }

        var nextApi = new FakeRuntimeShaderCrossApi();
        using var nextScope = Electron2D.RuntimeShaderCrossService.UseApiForTests(nextApi);
        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        Assert.Equal(1, nextApi.InitCalls);
        Assert.Equal(0, nextApi.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceTestApiScopePreservesInitializedOuterState()
    {
        var outerApi = new FakeRuntimeShaderCrossApi();
        using var outerScope = Electron2D.RuntimeShaderCrossService.UseApiForTests(outerApi);
        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        var innerApi = new FakeRuntimeShaderCrossApi();
        using (Electron2D.RuntimeShaderCrossService.UseApiForTests(innerApi))
        {
            using (Electron2D.RuntimeShaderCrossService.Acquire())
            {
            }

            Assert.Equal(1, innerApi.InitCalls);
            Assert.Equal(0, innerApi.QuitCalls);
        }

        Assert.Equal(1, outerApi.InitCalls);
        Assert.Equal(0, outerApi.QuitCalls);
        using (Electron2D.RuntimeShaderCrossService.Acquire())
        {
        }

        Assert.Equal(1, outerApi.InitCalls);
        Assert.Equal(0, outerApi.QuitCalls);
    }

    [Fact]
    public void RuntimeShaderCrossServiceTestApiScopeRestoresPreexistingTerminalState()
    {
        var outerApi = new FakeRuntimeShaderCrossApi();
        using var outerScope = Electron2D.RuntimeShaderCrossService.UseApiForTests(outerApi);
        Electron2D.RuntimeApplicationServices.ShutdownOnRenderThread();

        var innerApi = new FakeRuntimeShaderCrossApi();
        using (Electron2D.RuntimeShaderCrossService.UseApiForTests(innerApi))
        {
            using (Electron2D.RuntimeShaderCrossService.Acquire())
            {
            }
        }

        var acquireException = Record.Exception(() =>
        {
            using (Electron2D.RuntimeShaderCrossService.Acquire())
            {
            }
        });

        Assert.IsType<Electron2D.GpuPresenterUnavailableException>(acquireException);
        Assert.Equal(0, outerApi.InitCalls);
        Assert.Equal(0, outerApi.QuitCalls);
    }

    [Theory]
    [InlineData(RuntimeGpuPresenterFailurePoint.SwapchainAcquire, false, 1, 0, 0, 0, 0)]
    [InlineData(RuntimeGpuPresenterFailurePoint.PresentationResources, false, 0, 1, 0, 0, 0)]
    [InlineData(RuntimeGpuPresenterFailurePoint.CopyPass, false, 0, 1, 0, 0, 0)]
    [InlineData(RuntimeGpuPresenterFailurePoint.RenderPass, false, 0, 1, 0, 0, 0)]
    [InlineData(RuntimeGpuPresenterFailurePoint.Submit, false, 0, 1, 0, 0, 0)]
    [InlineData(RuntimeGpuPresenterFailurePoint.FenceSubmit, true, 0, 0, 1, 0, 0)]
    [InlineData(RuntimeGpuPresenterFailurePoint.FenceWait, true, 0, 0, 1, 1, 1)]
    public void RuntimeGpuPresenterFailurePhasesUseValidTerminalPathFromPresent(
        RuntimeGpuPresenterFailurePoint failurePoint,
        bool captureFrame,
        int expectedCancelCalls,
        int expectedSubmitCalls,
        int expectedFenceSubmitCalls,
        int expectedFenceWaitCalls,
        int expectedFenceReleaseCalls)
    {
        var terminalApi = new PresenterFaultSdlGpuApi
        {
            SubmitCommandBufferError = failurePoint == RuntimeGpuPresenterFailurePoint.Submit ? "submit failed" : null,
            SubmitFenceError = failurePoint == RuntimeGpuPresenterFailurePoint.FenceSubmit ? "fence submit failed" : null,
            WaitForFenceError = failurePoint == RuntimeGpuPresenterFailurePoint.FenceWait ? "fence wait failed" : null
        };
        var backend = CreateInitializedGpuBackend(terminalApi);
        var gpuApi = new PresenterFaultGpuApi(failurePoint);
        using var presenter = new Electron2D.RuntimeGpuFramePresenter(
            new IntPtr(50),
            new Electron2D.Vector2I(16, 16),
            backend,
            gpuApi,
            presentationResourcesReady: failurePoint != RuntimeGpuPresenterFailurePoint.PresentationResources);

        var plan = failurePoint == RuntimeGpuPresenterFailurePoint.CopyPass
            ? CreateTexturePlan(CreateRuntimeImageTexture())
            : CreateSolidRectPlan();

        Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => presenter.Present(
            plan,
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame));

        Assert.Equal(expectedCancelCalls, gpuApi.CancelCommandBufferCalls);
        Assert.Equal(expectedSubmitCalls, terminalApi.SubmitCommandBufferCalls);
        Assert.Equal(expectedFenceSubmitCalls, terminalApi.SubmitCommandBufferAndAcquireFenceCalls);
        Assert.Equal(expectedFenceWaitCalls, terminalApi.WaitForFenceCalls);
        Assert.Equal(expectedFenceReleaseCalls, terminalApi.ReleaseFenceCalls);
        Assert.InRange(
            gpuApi.CancelCommandBufferCalls + terminalApi.SubmitCommandBufferCalls + terminalApi.SubmitCommandBufferAndAcquireFenceCalls,
            0,
            1);
    }

    [Fact]
    public void RuntimeGpuPresenterSubmitFailureRollsBackStagedUploadAndCounters()
    {
        var terminalApi = new PresenterFaultSdlGpuApi
        {
            SubmitCommandBufferError = "submit failed"
        };
        var backend = CreateInitializedGpuBackend(terminalApi);
        var gpuApi = new PresenterFaultGpuApi(RuntimeGpuPresenterFailurePoint.None);
        using var presenter = new Electron2D.RuntimeGpuFramePresenter(
            new IntPtr(51),
            new Electron2D.Vector2I(16, 16),
            backend,
            gpuApi,
            presentationResourcesReady: true);

        Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => presenter.Present(
            CreateTexturePlan(CreateRuntimeImageTexture()),
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame: false));

        Assert.Equal(1, terminalApi.SubmitCommandBufferCalls);
        Assert.Equal(0, gpuApi.CancelCommandBufferCalls);
        Assert.Equal(0, GetPrivateCollectionCount(presenter, "textureCache"));
        Assert.Equal(0, GetPrivateCollectionCount(presenter, "stagedTextureCache"));
        Assert.Equal(0, GetPrivateField<int>(presenter, "textureUploads"));
        Assert.Equal(1, gpuApi.GetReleaseCount(gpuApi.LastCreatedTexture));
        Assert.Equal(1, gpuApi.GetTransferReleaseCount(gpuApi.LastCreatedTransferBuffer));
    }

    [Fact]
    public void RuntimeHostCapturesInteractiveScreenshotOnFirstFrameOnly()
    {
        var method = typeof(Electron2D.RuntimeHost).GetMethod(
            "ShouldCaptureFrame",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);

        Assert.True(InvokeShouldCaptureFrame(method!, "frame.png", frameLimit: 0, frameIndex: 0, targetFrames: int.MaxValue));
        Assert.False(InvokeShouldCaptureFrame(method!, "frame.png", frameLimit: 0, frameIndex: 1, targetFrames: int.MaxValue));
        Assert.False(InvokeShouldCaptureFrame(method!, "frame.png", frameLimit: 2, frameIndex: 0, targetFrames: 2));
        Assert.True(InvokeShouldCaptureFrame(method!, "frame.png", frameLimit: 2, frameIndex: 1, targetFrames: 2));
        Assert.False(InvokeShouldCaptureFrame(method!, null, frameLimit: 0, frameIndex: 0, targetFrames: int.MaxValue));
    }

    [Fact]
    public void RuntimeFramePresenterUsesSdlRendererFallbackWhenGpuPresenterCreationFails()
    {
        var fallback = new FakeRuntimeFramePresenter(new Electron2D.RuntimeFrameDiagnostics(
            "RenderingServer",
            "SDL_Renderer",
            UsedFallbackPresenter: false,
            FallbackReason: string.Empty,
            RenderBatches: 0,
            ActualDrawCalls: 0,
            TextureSwitches: 0,
            PipelineSwitches: 0,
            TextureUploads: 0,
            TextureCacheHits: 0,
            PresentationResourcesCreated: 1,
            PresentationResourcesRecreated: 0,
            ObservedPresentationResizes: 0,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            CapturePresenterManagedBytesAllocated: 0));

        using var presenter = new Electron2D.RuntimeFramePresenter(
            new IntPtr(1),
            new Electron2D.Vector2I(16, 16),
            (_, _) => throw new Electron2D.GpuPresenterUnavailableException("GPU unavailable for test."),
            (_, _) => fallback);

        var presentedFrame = presenter.Present(
            new Electron2D.CanvasItemRenderPlan(
                Array.Empty<Electron2D.CanvasItemRenderCommand>(),
                Array.Empty<Electron2D.CanvasItemRenderBatch>()),
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame: false);

        var diagnostics = presentedFrame.Diagnostics;
        Assert.Equal("RenderingServer", diagnostics.RenderSource);
        Assert.Equal("SDL_Renderer", diagnostics.PresentationBackend);
        Assert.True(diagnostics.UsedFallbackPresenter);
        Assert.Contains("GPU unavailable for test.", diagnostics.FallbackReason, StringComparison.Ordinal);
        Assert.Equal(1, fallback.PresentCalls);
        Assert.Null(presentedFrame.Screenshot);
    }

    [Fact]
    public void RuntimeFramePresenterDoesNotUseFallbackForProgrammingErrors()
    {
        var fallback = new FakeRuntimeFramePresenter(new Electron2D.RuntimeFrameDiagnostics(
            "RenderingServer",
            "SDL_Renderer",
            UsedFallbackPresenter: false,
            FallbackReason: string.Empty,
            RenderBatches: 0,
            ActualDrawCalls: 0,
            TextureSwitches: 0,
            PipelineSwitches: 0,
            TextureUploads: 0,
            TextureCacheHits: 0,
            PresentationResourcesCreated: 1,
            PresentationResourcesRecreated: 0,
            ObservedPresentationResizes: 0,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            CapturePresenterManagedBytesAllocated: 0));

        using var presenter = new Electron2D.RuntimeFramePresenter(
            new IntPtr(1),
            new Electron2D.Vector2I(16, 16),
            (_, _) => new ThrowingRuntimeFramePresenter(new ArgumentException("Bad render plan.")),
            (_, _) => fallback);

        var plan = new Electron2D.CanvasItemRenderPlan(
            Array.Empty<Electron2D.CanvasItemRenderCommand>(),
            Array.Empty<Electron2D.CanvasItemRenderBatch>());

        var exception = Assert.Throws<ArgumentException>(() => presenter.Present(
            plan,
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame: false));
        Assert.Contains("Bad render plan.", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, fallback.PresentCalls);
    }

    [Fact]
    public void RuntimeFramePresenterDoesNotUseFallbackForUnsupportedTextureResources()
    {
        var fallback = new FakeRuntimeFramePresenter(new Electron2D.RuntimeFrameDiagnostics(
            "RenderingServer",
            "SDL_Renderer",
            UsedFallbackPresenter: false,
            FallbackReason: string.Empty,
            RenderBatches: 0,
            ActualDrawCalls: 0,
            TextureSwitches: 0,
            PipelineSwitches: 0,
            TextureUploads: 0,
            TextureCacheHits: 0,
            PresentationResourcesCreated: 1,
            PresentationResourcesRecreated: 0,
            ObservedPresentationResizes: 0,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            CapturePresenterManagedBytesAllocated: 0));
        var unsupported = new UnsupportedTexture2D(4, 4);

        using var presenter = new Electron2D.RuntimeFramePresenter(
            new IntPtr(1),
            new Electron2D.Vector2I(16, 16),
            (_, _) => new ThrowingRuntimeFramePresenter(new Electron2D.UnsupportedTextureResourceException(unsupported)),
            (_, _) => fallback);

        var exception = Assert.Throws<Electron2D.UnsupportedTextureResourceException>(() => presenter.Present(
            CreateUnsupportedTexturePlan(unsupported),
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame: false));
        Assert.Same(unsupported, exception.Texture);
        Assert.Equal(0, fallback.PresentCalls);
    }

    [Fact]
    public void RuntimeFramePresenterDoesNotUseFallbackForUnknownRenderCommandKind()
    {
        var fallback = new FakeRuntimeFramePresenter(new Electron2D.RuntimeFrameDiagnostics(
            "RenderingServer",
            "SDL_Renderer",
            UsedFallbackPresenter: true,
            FallbackReason: "unexpected",
            RenderBatches: 0,
            ActualDrawCalls: 0,
            TextureSwitches: 0,
            PipelineSwitches: 0,
            TextureUploads: 0,
            TextureCacheHits: 0,
            PresentationResourcesCreated: 0,
            PresentationResourcesRecreated: 0,
            ObservedPresentationResizes: 0,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            CapturePresenterManagedBytesAllocated: 0));
        using var presenter = new Electron2D.RuntimeFramePresenter(
            new IntPtr(1),
            new Electron2D.Vector2I(16, 16),
            (_, _) => new ThrowingRuntimeFramePresenter(new InvalidOperationException("Unsupported command.")),
            (_, _) => fallback);

        Assert.Throws<InvalidOperationException>(() => presenter.Present(
            CreateUnknownCommandPlan(),
            new Electron2D.Vector2I(16, 16),
            Electron2D.Color.Black,
            captureFrame: false));
        Assert.Equal(0, fallback.PresentCalls);
    }

    [Fact]
    [Trait("Category", "Baseline")]
    public void RuntimeSdlRendererFallbackRealPresenterReportsZeroTextAllocationsAcrossMeasuredSteadyFrames()
    {
        var tree = new Electron2D.SceneTree();
        tree.Root.AddChild(new TextGlyphScene());
        tree.ProcessFrame(1d / 60d);
        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root);

        Assert.True(SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Events), SDL.GetError());
        var window = IntPtr.Zero;
        try
        {
            window = SDL.CreateWindow(
                "Electron2D Runtime SDL Renderer Text Allocation Test",
                32,
                24,
                SDL.WindowFlags.Hidden);
            Assert.NotEqual(IntPtr.Zero, window);

            using var presenter = new Electron2D.RuntimeFramePresenter(
                window,
                new Electron2D.Vector2I(32, 24),
                (_, _) => new Electron2D.RuntimeSdlRendererFramePresenter(window, new Electron2D.Vector2I(32, 24)),
                (_, _) => throw new InvalidOperationException("Fallback should not be used."));

            Electron2D.RuntimePresentedFrame frame = default;
            for (var index = 0; index < 720; index++)
            {
                frame = presenter.Present(plan, new Electron2D.Vector2I(32, 24), Electron2D.Color.Black, captureFrame: false);
            }

            Assert.Equal(0, frame.Diagnostics.MaxPresenterManagedBytesPerFrame);
            Assert.Equal(600, frame.Diagnostics.PresenterMeasuredFrames);
            Assert.Equal(0, frame.Diagnostics.CapturePresenterManagedBytesAllocated);
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

    [Fact]
    public void RuntimeFramePresenterReportsActualManagedAllocationsAfterWarmup()
    {
        using var presenter = new Electron2D.RuntimeFramePresenter(
            new IntPtr(1),
            new Electron2D.Vector2I(16, 16),
            (_, _) => new AllocatingRuntimeFramePresenter(),
            (_, _) => throw new InvalidOperationException("Fallback should not be used."));

        var plan = new Electron2D.CanvasItemRenderPlan(
            Array.Empty<Electron2D.CanvasItemRenderCommand>(),
            Array.Empty<Electron2D.CanvasItemRenderBatch>());

        Electron2D.RuntimePresentedFrame secondFrame = default;
        for (var index = 0; index <= 120; index++)
        {
            secondFrame = presenter.Present(plan, new Electron2D.Vector2I(16, 16), Electron2D.Color.Black, captureFrame: false);
        }

        Assert.True(secondFrame.Diagnostics.MaxPresenterManagedBytesPerFrame >= 64);
        Assert.True(secondFrame.Diagnostics.PresenterMeasuredFrames > 0);
        Assert.Equal(0, secondFrame.Diagnostics.CapturePresenterManagedBytesAllocated);
    }

    [Fact]
    public void RuntimeFramePresenterReportsZeroManagedAllocationsAcrossMeasuredSteadyFrames()
    {
        var diagnostics = new Electron2D.RuntimeFrameDiagnostics(
            "RenderingServer",
            "SDL_GPU",
            UsedFallbackPresenter: false,
            FallbackReason: string.Empty,
            RenderBatches: 0,
            ActualDrawCalls: 0,
            TextureSwitches: 0,
            PipelineSwitches: 0,
            TextureUploads: 0,
            TextureCacheHits: 0,
            PresentationResourcesCreated: 1,
            PresentationResourcesRecreated: 0,
            ObservedPresentationResizes: 0,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            CapturePresenterManagedBytesAllocated: 0);
        using var presenter = new Electron2D.RuntimeFramePresenter(
            new IntPtr(1),
            new Electron2D.Vector2I(16, 16),
            (_, _) => new FakeRuntimeFramePresenter(diagnostics),
            (_, _) => throw new InvalidOperationException("Fallback should not be used."));
        var plan = new Electron2D.CanvasItemRenderPlan(
            Array.Empty<Electron2D.CanvasItemRenderCommand>(),
            Array.Empty<Electron2D.CanvasItemRenderBatch>());

        Electron2D.RuntimePresentedFrame frame = default;
        for (var index = 0; index < 720; index++)
        {
            frame = presenter.Present(plan, new Electron2D.Vector2I(16, 16), Electron2D.Color.Black, captureFrame: false);
        }

        Assert.Equal(0, frame.Diagnostics.MaxPresenterManagedBytesPerFrame);
        Assert.Equal(600, frame.Diagnostics.PresenterMeasuredFrames);
        Assert.Equal(0, frame.Diagnostics.CapturePresenterManagedBytesAllocated);
    }

    private static T GetResultProperty<T>(Electron2D.RuntimeHostResult result, string propertyName)
    {
        var property = typeof(Electron2D.RuntimeHostResult).GetProperty(propertyName);
        Assert.NotNull(property);
        var value = property.GetValue(result);
        Assert.IsType<T>(value);
        return (T)value;
    }

    private static Electron2D.CanvasItemRenderCommand CreateDiagnosticCommand(
        Electron2D.CanvasItemBatchKey key,
        Electron2D.CanvasItemRenderCommandKind kind,
        long treeOrder)
    {
        return new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(1),
            key,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: kind);
    }

    private static Electron2D.CanvasItemRenderPlan CreateUnsupportedTexturePlan(Electron2D.Texture2D? texture = null)
    {
        var textureKey = new Electron2D.CanvasItemBatchKey(
            new Electron2D.Rid(20),
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix);
        var command = new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(21),
            textureKey,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 0,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: Electron2D.CanvasItemRenderCommandKind.Texture,
            sourceRect: new Electron2D.Rect2(0f, 0f, 4f, 4f),
            destinationRect: new Electron2D.Rect2(1f, 1f, 4f, 4f),
            texture: texture ?? new UnsupportedTexture2D(4, 4));

        return new Electron2D.CanvasItemRenderPlan(
            new[] { command },
            new[] { new Electron2D.CanvasItemRenderBatch(textureKey, startIndex: 0, count: 1) });
    }

    private static Electron2D.CanvasItemRenderPlan CreateUnknownCommandPlan()
    {
        var key = new Electron2D.CanvasItemBatchKey(
            texture: default,
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix,
            kind: (Electron2D.CanvasItemRenderCommandKind)999);
        var command = new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(22),
            key,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 0,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: (Electron2D.CanvasItemRenderCommandKind)999,
            destinationRect: new Electron2D.Rect2(1f, 1f, 4f, 4f));

        return new Electron2D.CanvasItemRenderPlan(
            new[] { command },
            new[] { new Electron2D.CanvasItemRenderBatch(key, startIndex: 0, count: 1) });
    }

    private static Electron2D.CanvasItemRenderPlan CreateTextureThenUnknownCommandPlan(Electron2D.Texture2D texture)
    {
        var textureKey = new Electron2D.CanvasItemBatchKey(
            new Electron2D.Rid(30),
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix,
            Electron2D.CanvasItemRenderCommandKind.Texture);
        var unknownKey = new Electron2D.CanvasItemBatchKey(
            texture: default,
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix,
            kind: (Electron2D.CanvasItemRenderCommandKind)999);
        var textureCommand = new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(31),
            textureKey,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 0,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: Electron2D.CanvasItemRenderCommandKind.Texture,
            sourceRect: new Electron2D.Rect2(0f, 0f, 2f, 2f),
            destinationRect: new Electron2D.Rect2(1f, 1f, 2f, 2f),
            texture: texture);
        var unknownCommand = new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(32),
            unknownKey,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 1,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: (Electron2D.CanvasItemRenderCommandKind)999,
            destinationRect: new Electron2D.Rect2(4f, 4f, 2f, 2f));

        return new Electron2D.CanvasItemRenderPlan(
            new[] { textureCommand, unknownCommand },
            new[]
            {
                new Electron2D.CanvasItemRenderBatch(textureKey, startIndex: 0, count: 1),
                new Electron2D.CanvasItemRenderBatch(unknownKey, startIndex: 1, count: 1)
            });
    }

    private static Electron2D.SdlGpuRenderingBackend CreateInitializedGpuBackend(PresenterFaultSdlGpuApi api)
    {
        var backend = new Electron2D.SdlGpuRenderingBackend(api, debugMode: false);
        backend.Initialize(new Electron2D.SdlGpuWindowInfo(16, 16, 1f, fullscreen: false, nativeWindowHandle: new IntPtr(50)));
        return backend;
    }

    private static Electron2D.CanvasItemRenderPlan CreateSolidRectPlan()
    {
        var key = new Electron2D.CanvasItemBatchKey(
            texture: default,
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix,
            Electron2D.CanvasItemRenderCommandKind.Rect);
        var command = new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(40),
            key,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 0,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: Electron2D.CanvasItemRenderCommandKind.Rect,
            destinationRect: new Electron2D.Rect2(1f, 1f, 4f, 4f));

        return new Electron2D.CanvasItemRenderPlan(
            new[] { command },
            new[] { new Electron2D.CanvasItemRenderBatch(key, startIndex: 0, count: 1) });
    }

    private static Electron2D.CanvasItemRenderPlan CreateTexturePlan(Electron2D.Texture2D texture)
    {
        var key = new Electron2D.CanvasItemBatchKey(
            new Electron2D.Rid(41),
            material: default,
            clip: default,
            Electron2D.CanvasItemBlendMode.Mix,
            Electron2D.CanvasItemRenderCommandKind.Texture);
        var command = new Electron2D.CanvasItemRenderCommand(
            new Electron2D.Rid(42),
            key,
            layer: 0,
            zIndex: 0,
            ySortEnabled: false,
            ySortPosition: 0f,
            treeOrder: 0,
            visible: true,
            Electron2D.Color.White,
            Electron2D.Color.White,
            kind: Electron2D.CanvasItemRenderCommandKind.Texture,
            sourceRect: new Electron2D.Rect2(0f, 0f, 2f, 2f),
            destinationRect: new Electron2D.Rect2(1f, 1f, 2f, 2f),
            texture: texture);

        return new Electron2D.CanvasItemRenderPlan(
            new[] { command },
            new[] { new Electron2D.CanvasItemRenderBatch(key, startIndex: 0, count: 1) });
    }

    private static Electron2D.ImageTexture CreateRuntimeImageTexture()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "Electron2D-RuntimeHostTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var texturePath = Path.Combine(directory, "presenter-fault-texture.png");
        File.WriteAllBytes(texturePath, Convert.FromBase64String(RgbaPng2x2Base64));
        return Electron2D.ImageTexture.LoadFromFile(texturePath);
    }

    private static int GetPrivateCollectionCount(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        var collection = Assert.IsAssignableFrom<System.Collections.ICollection>(field!.GetValue(instance));
        return collection.Count;
    }

    private static T GetPrivateField<T>(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        var value = field!.GetValue(instance);
        Assert.IsType<T>(value);
        return (T)value;
    }

    private static bool InvokeShouldCaptureFrame(
        System.Reflection.MethodInfo method,
        string? screenshotPath,
        int frameLimit,
        int frameIndex,
        int targetFrames)
    {
        var value = method.Invoke(null, [screenshotPath, frameLimit, frameIndex, targetFrames]);
        Assert.IsType<bool>(value);
        return (bool)value;
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

    private static void AssertVisualFixturePixels(int width, byte[] rgba)
    {
        Assert.Equal((0, 0, 0, 255), GetPixel(rgba, width, 5, 5));
        Assert.Equal((255, 0, 0, 255), GetPixel(rgba, width, 6, 5));
        Assert.Equal((255, 0, 0, 255), GetPixel(rgba, width, 48, 26));
        Assert.Equal((0, 0, 0, 255), GetPixel(rgba, width, 43, 21));
        Assert.Equal((0, 0, 255, 255), GetPixel(rgba, width, 12, 42));
        Assert.Equal((0, 255, 0, 255), GetPixel(rgba, width, 33, 9));
        Assert.Equal((0, 0, 0, 255), GetPixel(rgba, width, 44, 20));
        Assert.Equal((255, 255, 255, 255), GetPixel(rgba, width, 52, 51));
        Assert.Equal((0, 0, 0, 255), GetPixel(rgba, width, 54, 51));
        Assert.Equal((255, 255, 255, 255), GetPixel(rgba, width, 56, 51));
        Assert.Equal((255, 0, 255, 255), GetPixel(rgba, width, 12, 54));
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

    private sealed class TextGlyphScene : Electron2D.Node2D
    {
        private readonly HostSmokeFont font = new();

        public override void _Process(double delta)
        {
            _ = delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawString(font, new Electron2D.Vector2(4f, 12f), "H", fontSize: 8);
        }
    }

    private sealed class VisualFixtureScene(Electron2D.Texture2D texture) : Electron2D.Node2D
    {
        private readonly HostSmokeFont font = new();

        public override void _Ready()
        {
            AddChild(new Electron2D.Sprite2D
            {
                Texture = texture,
                Centered = false,
                FlipH = true,
                Position = new Electron2D.Vector2(5f, 5f)
            });
            AddChild(new RotatedRectFixtureNode
            {
                Position = new Electron2D.Vector2(48f, 20f),
                RotationDegrees = 45f
            });
        }

        public override void _Process(double delta)
        {
            _ = delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawCircle(new Electron2D.Vector2(12f, 42f), 6f, new Electron2D.Color(0f, 0f, 1f));
            DrawPolygon(
                new[]
                {
                    new Electron2D.Vector2(30f, 6f),
                    new Electron2D.Vector2(45f, 6f),
                    new Electron2D.Vector2(30f, 21f)
                },
                new[]
                {
                    new Electron2D.Color(0f, 1f, 0f),
                    new Electron2D.Color(0f, 1f, 0f),
                    new Electron2D.Color(0f, 1f, 0f)
                });
            DrawString(font, new Electron2D.Vector2(52f, 58f), "H", fontSize: 8);
            DrawLine(
                new Electron2D.Vector2(4f, 56f),
                new Electron2D.Vector2(22f, 56f),
                new Electron2D.Color(1f, 0f, 1f),
                width: 5f);
        }
    }

    private sealed class RotatedRectFixtureNode : Electron2D.Node2D
    {
        public override void _Process(double delta)
        {
            _ = delta;
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawRect(new Electron2D.Rect2(0f, 0f, 8f, 8f), new Electron2D.Color(1f, 0f, 0f));
        }
    }

    private sealed class HostSmokeFont : Electron2D.Font;

    private sealed class FakeRuntimeFramePresenter(Electron2D.RuntimeFrameDiagnostics diagnostics)
        : Electron2D.IRuntimeFramePresenter
    {
        public int PresentCalls { get; private set; }

        public Electron2D.RuntimePresentedFrame Present(
            Electron2D.CanvasItemRenderPlan renderPlan,
            Electron2D.Vector2I windowSize,
            Electron2D.Color clearColor,
            bool captureFrame)
        {
            _ = renderPlan;
            _ = windowSize;
            _ = clearColor;
            _ = captureFrame;
            PresentCalls++;
            return new Electron2D.RuntimePresentedFrame(diagnostics, null);
        }

        public void Dispose()
        {
        }
    }

    private sealed class ThrowingRuntimeFramePresenter(Exception exception) : Electron2D.IRuntimeFramePresenter
    {
        public Electron2D.RuntimePresentedFrame Present(
            Electron2D.CanvasItemRenderPlan renderPlan,
            Electron2D.Vector2I windowSize,
            Electron2D.Color clearColor,
            bool captureFrame)
        {
            _ = renderPlan;
            _ = windowSize;
            _ = clearColor;
            _ = captureFrame;
            throw exception;
        }

        public void Dispose()
        {
        }
    }

    private sealed class AllocatingRuntimeFramePresenter : Electron2D.IRuntimeFramePresenter
    {
        private static readonly Electron2D.RuntimeFrameDiagnostics Diagnostics = new(
            "RenderingServer",
            "SDL_GPU",
            UsedFallbackPresenter: false,
            FallbackReason: string.Empty,
            RenderBatches: 0,
            ActualDrawCalls: 0,
            TextureSwitches: 0,
            PipelineSwitches: 0,
            TextureUploads: 0,
            TextureCacheHits: 0,
            PresentationResourcesCreated: 1,
            PresentationResourcesRecreated: 0,
            ObservedPresentationResizes: 0,
            PresentationBackendReconfigurations: 0,
            MaxPresenterManagedBytesPerFrame: 0,
            PresenterMeasuredFrames: 0,
            CapturePresenterManagedBytesAllocated: 0);

        public Electron2D.RuntimePresentedFrame Present(
            Electron2D.CanvasItemRenderPlan renderPlan,
            Electron2D.Vector2I windowSize,
            Electron2D.Color clearColor,
            bool captureFrame)
        {
            _ = renderPlan;
            _ = windowSize;
            _ = clearColor;
            _ = captureFrame;
            var allocation = new byte[64];
            allocation[0] = 1;
            return new Electron2D.RuntimePresentedFrame(Diagnostics, null);
        }

        public void Dispose()
        {
        }
    }

    public enum RuntimeGpuPresenterFailurePoint
    {
        None,
        SwapchainAcquire,
        PresentationResources,
        CopyPass,
        RenderPass,
        Submit,
        FenceSubmit,
        FenceWait
    }

    private sealed class PresenterFaultGpuApi(RuntimeGpuPresenterFailurePoint failurePoint) : Electron2D.IRuntimeGpuPresenterApi
    {
        private readonly Dictionary<IntPtr, int> textureReleaseCounts = new();
        private readonly Dictionary<IntPtr, int> transferReleaseCounts = new();
        private readonly List<IntPtr> mappedPointers = new();
        private int nextHandle = 200;

        public int CancelCommandBufferCalls { get; private set; }

        public IntPtr LastCreatedTexture { get; private set; }

        public IntPtr LastCreatedTransferBuffer { get; private set; }

        public bool SetSwapchainParameters(
            IntPtr device,
            IntPtr window,
            SDL.GPUSwapchainComposition composition,
            SDL.GPUPresentMode presentMode)
        {
            _ = device;
            _ = window;
            _ = composition;
            _ = presentMode;
            return true;
        }

        public bool WaitAndAcquireSwapchainTexture(
            IntPtr commandBuffer,
            IntPtr window,
            out IntPtr texture,
            out uint width,
            out uint height)
        {
            _ = commandBuffer;
            _ = window;
            texture = failurePoint == RuntimeGpuPresenterFailurePoint.SwapchainAcquire ? IntPtr.Zero : new IntPtr(201);
            width = 16;
            height = 16;
            return failurePoint != RuntimeGpuPresenterFailurePoint.SwapchainAcquire;
        }

        public bool CancelCommandBuffer(IntPtr commandBuffer)
        {
            _ = commandBuffer;
            CancelCommandBufferCalls++;
            return true;
        }

        public void BeforePresentationResources()
        {
            if (failurePoint == RuntimeGpuPresenterFailurePoint.PresentationResources)
            {
                throw new Electron2D.GpuPresenterUnavailableException("presentation resources failed");
            }
        }

        public SDL.GPUTextureFormat GetSwapchainTextureFormat(IntPtr device, IntPtr window)
        {
            _ = device;
            _ = window;
            return SDL.GPUTextureFormat.R8G8B8A8Unorm;
        }

        public IntPtr CreateGraphicsPipeline(IntPtr device, in SDL.GPUGraphicsPipelineCreateInfo createInfo)
        {
            _ = device;
            _ = createInfo;
            return NextHandle();
        }

        public IntPtr CreateSampler(IntPtr device, in SDL.GPUSamplerCreateInfo createInfo)
        {
            _ = device;
            _ = createInfo;
            return NextHandle();
        }

        public IntPtr CreateBuffer(IntPtr device, in SDL.GPUBufferCreateInfo createInfo)
        {
            _ = device;
            _ = createInfo;
            return NextHandle();
        }

        public IntPtr CreateTexture(IntPtr device, in SDL.GPUTextureCreateInfo createInfo)
        {
            _ = device;
            _ = createInfo;
            LastCreatedTexture = NextHandle();
            return LastCreatedTexture;
        }

        public IntPtr CreateTransferBuffer(IntPtr device, in SDL.GPUTransferBufferCreateInfo createInfo)
        {
            _ = device;
            _ = createInfo;
            LastCreatedTransferBuffer = NextHandle();
            return LastCreatedTransferBuffer;
        }

        public IntPtr MapTransferBuffer(IntPtr device, IntPtr transferBuffer, bool cycle)
        {
            _ = device;
            _ = transferBuffer;
            _ = cycle;
            var pointer = Marshal.AllocHGlobal(4096);
            mappedPointers.Add(pointer);
            return pointer;
        }

        public void UnmapTransferBuffer(IntPtr device, IntPtr transferBuffer)
        {
            _ = device;
            _ = transferBuffer;
            if (mappedPointers.Count == 0)
            {
                return;
            }

            var pointer = mappedPointers[^1];
            mappedPointers.RemoveAt(mappedPointers.Count - 1);
            Marshal.FreeHGlobal(pointer);
        }

        public IntPtr BeginCopyPass(IntPtr commandBuffer)
        {
            _ = commandBuffer;
            return failurePoint == RuntimeGpuPresenterFailurePoint.CopyPass ? IntPtr.Zero : NextHandle();
        }

        public void EndCopyPass(IntPtr copyPass)
        {
            _ = copyPass;
        }

        public void UploadToBuffer(
            IntPtr copyPass,
            in SDL.GPUTransferBufferLocation source,
            in SDL.GPUBufferRegion destination,
            bool cycle)
        {
            _ = copyPass;
            _ = source;
            _ = destination;
            _ = cycle;
        }

        public void UploadToTexture(
            IntPtr copyPass,
            in SDL.GPUTextureTransferInfo source,
            in SDL.GPUTextureRegion destination,
            bool cycle)
        {
            _ = copyPass;
            _ = source;
            _ = destination;
            _ = cycle;
        }

        public void DownloadFromTexture(
            IntPtr copyPass,
            in SDL.GPUTextureRegion source,
            in SDL.GPUTextureTransferInfo destination)
        {
            _ = copyPass;
            _ = source;
            _ = destination;
        }

        public IntPtr BeginRenderPass(IntPtr commandBuffer, SDL.GPUColorTargetInfo[] colorTargets)
        {
            _ = commandBuffer;
            _ = colorTargets;
            return failurePoint == RuntimeGpuPresenterFailurePoint.RenderPass ? IntPtr.Zero : NextHandle();
        }

        public void EndRenderPass(IntPtr renderPass)
        {
            _ = renderPass;
        }

        public void BindVertexBuffers(IntPtr renderPass, uint firstSlot, SDL.GPUBufferBinding[] bindings)
        {
            _ = renderPass;
            _ = firstSlot;
            _ = bindings;
        }

        public void BindGraphicsPipeline(IntPtr renderPass, IntPtr pipeline)
        {
            _ = renderPass;
            _ = pipeline;
        }

        public void BindFragmentSamplers(IntPtr renderPass, uint firstSlot, SDL.GPUTextureSamplerBinding[] bindings)
        {
            _ = renderPass;
            _ = firstSlot;
            _ = bindings;
        }

        public void DrawPrimitives(IntPtr renderPass, uint vertexCount, uint numInstances, uint firstVertex, uint firstInstance)
        {
            _ = renderPass;
            _ = vertexCount;
            _ = numInstances;
            _ = firstVertex;
            _ = firstInstance;
        }

        public bool GetWindowSize(IntPtr window, out int width, out int height)
        {
            _ = window;
            width = 16;
            height = 16;
            return true;
        }

        public string GetError()
        {
            return failurePoint + " failure";
        }

        public void ReleaseGraphicsPipeline(IntPtr device, IntPtr pipeline)
        {
            _ = device;
            _ = pipeline;
        }

        public void ReleaseSampler(IntPtr device, IntPtr sampler)
        {
            _ = device;
            _ = sampler;
        }

        public void ReleaseBuffer(IntPtr device, IntPtr buffer)
        {
            _ = device;
            _ = buffer;
        }

        public void ReleaseTexture(IntPtr device, IntPtr texture)
        {
            _ = device;
            textureReleaseCounts.TryGetValue(texture, out var count);
            textureReleaseCounts[texture] = count + 1;
        }

        public void ReleaseTransferBuffer(IntPtr device, IntPtr transferBuffer)
        {
            _ = device;
            transferReleaseCounts.TryGetValue(transferBuffer, out var count);
            transferReleaseCounts[transferBuffer] = count + 1;
        }

        public void ReleaseWindowFromDevice(IntPtr device, IntPtr window)
        {
            _ = device;
            _ = window;
        }

        public int GetReleaseCount(IntPtr texture)
        {
            return textureReleaseCounts.TryGetValue(texture, out var count) ? count : 0;
        }

        public int GetTransferReleaseCount(IntPtr transferBuffer)
        {
            return transferReleaseCounts.TryGetValue(transferBuffer, out var count) ? count : 0;
        }

        private IntPtr NextHandle()
        {
            return new IntPtr(nextHandle++);
        }
    }

    private sealed class PresenterFaultSdlGpuApi : Electron2D.ISdlGpuApi
    {
        public int SubmitCommandBufferCalls { get; private set; }

        public int SubmitCommandBufferAndAcquireFenceCalls { get; private set; }

        public int WaitForFenceCalls { get; private set; }

        public int ReleaseFenceCalls { get; private set; }

        public string? SubmitCommandBufferError { get; init; }

        public string? SubmitFenceError { get; init; }

        public string? WaitForFenceError { get; init; }

        public Electron2D.SdlGpuDeviceHandle CreateDevice(Electron2D.SdlGpuDeviceCreateInfo createInfo, out string? error)
        {
            _ = createInfo;
            error = null;
            return new Electron2D.SdlGpuDeviceHandle(10);
        }

        public bool ClaimWindow(Electron2D.SdlGpuDeviceHandle device, Electron2D.SdlGpuWindowInfo window, out string? error)
        {
            _ = device;
            _ = window;
            error = null;
            return true;
        }

        public Electron2D.SdlGpuDeviceInfo GetDeviceInfo(Electron2D.SdlGpuDeviceHandle device)
        {
            _ = device;
            return new Electron2D.SdlGpuDeviceInfo("Fault GPU", "test", "1", "test 1");
        }

        public bool ValidateTextureSmoke(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            _ = device;
            error = null;
            return true;
        }

        public bool ValidatePipelineSmoke(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            _ = device;
            error = null;
            return true;
        }

        public Electron2D.SdlGpuCommandBufferHandle AcquireCommandBuffer(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            _ = device;
            error = null;
            return new Electron2D.SdlGpuCommandBufferHandle(11);
        }

        public bool SubmitCommandBuffer(Electron2D.SdlGpuCommandBufferHandle commandBuffer, out string? error)
        {
            _ = commandBuffer;
            SubmitCommandBufferCalls++;
            error = SubmitCommandBufferError;
            return error is null;
        }

        public Electron2D.SdlGpuFenceHandle SubmitCommandBufferAndAcquireFence(
            Electron2D.SdlGpuCommandBufferHandle commandBuffer,
            out string? error)
        {
            _ = commandBuffer;
            SubmitCommandBufferAndAcquireFenceCalls++;
            error = SubmitFenceError;
            return error is null ? new Electron2D.SdlGpuFenceHandle(12) : default;
        }

        public bool WaitForFence(
            Electron2D.SdlGpuDeviceHandle device,
            Electron2D.SdlGpuFenceHandle fence,
            out string? error)
        {
            _ = device;
            _ = fence;
            WaitForFenceCalls++;
            error = WaitForFenceError;
            return error is null;
        }

        public void ReleaseFence(Electron2D.SdlGpuDeviceHandle device, Electron2D.SdlGpuFenceHandle fence)
        {
            _ = device;
            _ = fence;
            ReleaseFenceCalls++;
        }

        public void DestroyDevice(Electron2D.SdlGpuDeviceHandle device)
        {
            _ = device;
        }
    }

    private sealed class FakeRuntimeShaderCrossApi : Electron2D.IRuntimeShaderCrossApi
    {
        public int InitCalls { get; private set; }

        public int QuitCalls { get; private set; }

        public bool Init()
        {
            InitCalls++;
            return true;
        }

        public void Quit()
        {
            QuitCalls++;
        }

        public string GetError()
        {
            return string.Empty;
        }
    }

    private sealed class EmptyTexture2D(int width, int height) : Electron2D.Texture2D
    {
        public override int GetWidth()
        {
            return width;
        }

        public override int GetHeight()
        {
            return height;
        }

        public override bool HasAlpha()
        {
            return false;
        }
    }

    private sealed class UnsupportedTexture2D(int width, int height) : Electron2D.Texture2D
    {
        public override int GetWidth()
        {
            return width;
        }

        public override int GetHeight()
        {
            return height;
        }

        public override bool HasAlpha()
        {
            return true;
        }
    }
}
