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
using System.Xml.Linq;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SdlGpuLifecycleTests
{
    [Fact]
    public void RuntimeProjectPinsManagedSdl3CsDependency()
    {
        var projectPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Electron2D", "Electron2D.csproj"));
        var document = XDocument.Load(projectPath);
        var package = document.Descendants("PackageReference")
            .SingleOrDefault(element => string.Equals((string?)element.Attribute("Include"), "SDL3-CS", StringComparison.Ordinal));

        Assert.NotNull(package);
        Assert.Equal("3.4.10.3", (string?)package!.Attribute("Version"));
    }

    [Fact]
    public void SdlGpuBackendRunsSuccessfulFrameLifecycleAndLogsEvents()
    {
        var api = new FakeSdlGpuApi();
        var backend = new Electron2D.SdlGpuRenderingBackend(api, debugMode: true);

        backend.Initialize(new Electron2D.SdlGpuWindowInfo(1280, 720, 1.5f, fullscreen: false));
        var frame = backend.BeginFrame();
        backend.EndFrame(frame);
        backend.Shutdown();

        Assert.Equal(Electron2D.RenderingServer.RenderingProfile.Standard, backend.Profile);
        Assert.True(backend.HasFeature(Electron2D.RenderingServer.RenderingFeature.CustomShaders));
        Assert.Equal(Electron2D.SdlGpuLifecycleState.Shutdown, backend.State);
        Assert.Equal(1, api.CreateDeviceCalls);
        Assert.Equal(1, api.ClaimWindowCalls);
        Assert.Equal(1, api.AcquireCommandBufferCalls);
        Assert.Equal(1, api.SubmitCommandBufferCalls);
        Assert.Equal(1, api.DestroyDeviceCalls);
        Assert.Equal(
            new[]
            {
                Electron2D.SdlGpuLifecycleEventKind.DeviceCreated,
                Electron2D.SdlGpuLifecycleEventKind.WindowClaimed,
                Electron2D.SdlGpuLifecycleEventKind.FrameBegan,
                Electron2D.SdlGpuLifecycleEventKind.FrameSubmitted,
                Electron2D.SdlGpuLifecycleEventKind.Shutdown
            },
            backend.Events.Select(item => item.Kind).ToArray());
    }

    [Fact]
    public void SdlGpuBackendLogsResizeFullscreenAndHighDpiChanges()
    {
        var backend = new Electron2D.SdlGpuRenderingBackend(new FakeSdlGpuApi(), debugMode: false);

        backend.Initialize(new Electron2D.SdlGpuWindowInfo(800, 600, 1f, fullscreen: false));
        backend.Resize(1920, 1080, 2f);
        backend.SetFullscreen(true);

        Assert.Equal(1920, backend.Window.Width);
        Assert.Equal(1080, backend.Window.Height);
        Assert.Equal(2f, backend.Window.DpiScale);
        Assert.True(backend.Window.Fullscreen);

        var resize = Assert.Single(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.Resized);
        Assert.Equal(1920, resize.Width);
        Assert.Equal(1080, resize.Height);
        Assert.Equal(2f, resize.DpiScale);

        var fullscreen = Assert.Single(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.FullscreenChanged);
        Assert.True(fullscreen.Fullscreen);
    }

    [Fact]
    public void SdlGpuBackendLogsDeviceCreationFailures()
    {
        var api = new FakeSdlGpuApi
        {
            CreateDeviceError = "No SDL_GPU driver is available."
        };
        var backend = new Electron2D.SdlGpuRenderingBackend(api, debugMode: true);

        var exception = Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => backend.Initialize(new Electron2D.SdlGpuWindowInfo(640, 480, 1f, fullscreen: false)));

        Assert.Equal(Electron2D.SdlGpuLifecycleState.Failed, backend.State);
        Assert.Contains("No SDL_GPU driver", exception.Message, StringComparison.Ordinal);
        var error = Assert.Single(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.DeviceError);
        Assert.Equal("No SDL_GPU driver is available.", error.Error);
    }

    [Fact]
    public void SdlGpuBackendLogsCommandBufferFailures()
    {
        var api = new FakeSdlGpuApi
        {
            AcquireCommandBufferError = "Could not acquire command buffer."
        };
        var backend = new Electron2D.SdlGpuRenderingBackend(api, debugMode: false);
        backend.Initialize(new Electron2D.SdlGpuWindowInfo(640, 480, 1f, fullscreen: false));

        var exception = Assert.Throws<Electron2D.GpuPresenterUnavailableException>(() => backend.BeginFrame());

        Assert.Equal(Electron2D.SdlGpuLifecycleState.Failed, backend.State);
        Assert.Contains("command buffer", exception.Message, StringComparison.OrdinalIgnoreCase);
        var error = Assert.Single(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.DeviceError);
        Assert.Equal("Could not acquire command buffer.", error.Error);
    }

    [Fact]
    public void SdlGpuBackendKeepsLifecycleEventsBoundedAcrossSteadyFrames()
    {
        var backend = new Electron2D.SdlGpuRenderingBackend(new FakeSdlGpuApi(), debugMode: false);
        backend.Initialize(new Electron2D.SdlGpuWindowInfo(640, 480, 1f, fullscreen: false));

        for (var index = 0; index < 700; index++)
        {
            var frame = backend.BeginFrame();
            backend.EndFrame(frame);
        }

        Assert.InRange(backend.Events.Count, 1, 128);
        Assert.Contains(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.WindowClaimed);
        Assert.Contains(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.FrameSubmitted);
    }

    [Fact]
    public void SdlGpuBackendKeepsLifecycleEventsBoundedAcrossResizeAndFullscreenTransitions()
    {
        var backend = new Electron2D.SdlGpuRenderingBackend(new FakeSdlGpuApi(), debugMode: false);
        backend.Initialize(new Electron2D.SdlGpuWindowInfo(640, 480, 1f, fullscreen: false));

        for (var index = 0; index < 600; index++)
        {
            backend.Resize(640 + index, 480 + index, 1f + (index % 3));
            backend.SetFullscreen((index & 1) == 0);
        }

        Assert.InRange(backend.Events.Count, 1, Electron2D.SdlGpuRenderingBackend.MaxLifecycleEventCount);
        Assert.True(backend.DroppedEventCount > 0);
        Assert.Contains(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.Resized);
        Assert.Contains(backend.Events, item => item.Kind == Electron2D.SdlGpuLifecycleEventKind.FullscreenChanged);
        Assert.Equal(1239, backend.Window.Width);
        Assert.Equal(1079, backend.Window.Height);
        Assert.False(backend.Window.Fullscreen);
    }

    [Fact]
    public void SdlGpuBackendRejectsInvalidFrameOrdering()
    {
        var backend = new Electron2D.SdlGpuRenderingBackend(new FakeSdlGpuApi(), debugMode: false);

        Assert.Throws<InvalidOperationException>(() => backend.BeginFrame());

        backend.Initialize(new Electron2D.SdlGpuWindowInfo(640, 480, 1f, fullscreen: false));
        _ = backend.BeginFrame();

        Assert.Throws<InvalidOperationException>(() => backend.BeginFrame());
    }

    private sealed class FakeSdlGpuApi : Electron2D.ISdlGpuApi
    {
        public int CreateDeviceCalls { get; private set; }

        public int ClaimWindowCalls { get; private set; }

        public int AcquireCommandBufferCalls { get; private set; }

        public int SubmitCommandBufferCalls { get; private set; }

        public int DestroyDeviceCalls { get; private set; }

        public Electron2D.SdlGpuDeviceCreateInfo? LastCreateInfo { get; private set; }

        public string? CreateDeviceError { get; init; }

        public string? ClaimWindowError { get; init; }

        public string? AcquireCommandBufferError { get; init; }

        public string? SubmitCommandBufferError { get; init; }

        public Electron2D.SdlGpuDeviceHandle CreateDevice(Electron2D.SdlGpuDeviceCreateInfo createInfo, out string? error)
        {
            CreateDeviceCalls++;
            LastCreateInfo = createInfo;
            error = CreateDeviceError;
            return error is null ? new Electron2D.SdlGpuDeviceHandle(1) : default;
        }

        public bool ClaimWindow(Electron2D.SdlGpuDeviceHandle device, Electron2D.SdlGpuWindowInfo window, out string? error)
        {
            ClaimWindowCalls++;
            error = ClaimWindowError;
            return error is null;
        }

        public Electron2D.SdlGpuDeviceInfo GetDeviceInfo(Electron2D.SdlGpuDeviceHandle device)
        {
            return new Electron2D.SdlGpuDeviceInfo("Test GPU", "test-driver", "1.0", "test-driver 1.0");
        }

        public bool ValidateTextureSmoke(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            error = null;
            return true;
        }

        public bool ValidatePipelineSmoke(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            error = null;
            return true;
        }

        public Electron2D.SdlGpuCommandBufferHandle AcquireCommandBuffer(Electron2D.SdlGpuDeviceHandle device, out string? error)
        {
            AcquireCommandBufferCalls++;
            error = AcquireCommandBufferError;
            return error is null ? new Electron2D.SdlGpuCommandBufferHandle(2) : default;
        }

        public bool SubmitCommandBuffer(Electron2D.SdlGpuCommandBufferHandle commandBuffer, out string? error)
        {
            SubmitCommandBufferCalls++;
            error = SubmitCommandBufferError;
            return error is null;
        }

        public Electron2D.SdlGpuFenceHandle SubmitCommandBufferAndAcquireFence(
            Electron2D.SdlGpuCommandBufferHandle commandBuffer,
            out string? error)
        {
            SubmitCommandBufferCalls++;
            error = SubmitCommandBufferError;
            return error is null ? new Electron2D.SdlGpuFenceHandle(3) : default;
        }

        public bool WaitForFence(
            Electron2D.SdlGpuDeviceHandle device,
            Electron2D.SdlGpuFenceHandle fence,
            out string? error)
        {
            error = null;
            return true;
        }

        public void ReleaseFence(Electron2D.SdlGpuDeviceHandle device, Electron2D.SdlGpuFenceHandle fence)
        {
        }

        public void DestroyDevice(Electron2D.SdlGpuDeviceHandle device)
        {
            DestroyDeviceCalls++;
        }
    }
}
