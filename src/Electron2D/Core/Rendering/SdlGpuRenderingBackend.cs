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
namespace Electron2D;

internal sealed class SdlGpuRenderingBackend : RenderingBackend
{
    private static readonly RenderingServer.RenderingFeature[] Features =
    {
        RenderingServer.RenderingFeature.Sprites,
        RenderingServer.RenderingFeature.Animation,
        RenderingServer.RenderingFeature.TileMap,
        RenderingServer.RenderingFeature.Ui,
        RenderingServer.RenderingFeature.Text,
        RenderingServer.RenderingFeature.Primitives,
        RenderingServer.RenderingFeature.Camera,
        RenderingServer.RenderingFeature.Clipping,
        RenderingServer.RenderingFeature.StandardBlendModes,
        RenderingServer.RenderingFeature.RenderTargets,
        RenderingServer.RenderingFeature.CustomShaders,
        RenderingServer.RenderingFeature.ShaderMaterial,
        RenderingServer.RenderingFeature.MultiPass,
        RenderingServer.RenderingFeature.AdvancedBlending,
        RenderingServer.RenderingFeature.PostProcessing
    };

    private readonly ISdlGpuApi _api;
    private readonly bool _debugMode;
    private readonly List<SdlGpuLifecycleEvent> _events = new();
    private SdlGpuDeviceHandle _device;
    private ulong _nextFrameSequence;

    public SdlGpuRenderingBackend(ISdlGpuApi api, bool debugMode)
        : base("SDL_GPU", RenderingServer.RenderingProfile.Standard, Features)
    {
        ArgumentNullException.ThrowIfNull(api);

        _api = api;
        _debugMode = debugMode;
        State = SdlGpuLifecycleState.NotInitialized;
    }

    public SdlGpuLifecycleState State { get; private set; }

    public SdlGpuWindowInfo Window { get; private set; }

    public IReadOnlyList<SdlGpuLifecycleEvent> Events => _events;

    public void Initialize(SdlGpuWindowInfo window)
    {
        if (State is not SdlGpuLifecycleState.NotInitialized and not SdlGpuLifecycleState.Shutdown)
        {
            ThrowInvalidState("initialize SDL_GPU backend");
        }

        Window = window;
        _events.Clear();
        _nextFrameSequence = 0;

        _device = _api.CreateDevice(_debugMode, out var createError);
        if (!_device.IsValid || createError is not null)
        {
            Fail(createError ?? "SDL_GPU device creation returned an invalid handle.");
        }

        State = SdlGpuLifecycleState.DeviceCreated;
        Log(SdlGpuLifecycleEventKind.DeviceCreated, "SDL_GPU device created.");

        if (!_api.ClaimWindow(_device, window, out var claimError))
        {
            Fail(claimError ?? "SDL_GPU window claim failed.");
        }

        State = SdlGpuLifecycleState.WindowClaimed;
        Log(SdlGpuLifecycleEventKind.WindowClaimed, "SDL_GPU window claimed.");
    }

    public SdlGpuFrame BeginFrame()
    {
        if (State != SdlGpuLifecycleState.WindowClaimed)
        {
            ThrowInvalidState("begin an SDL_GPU frame");
        }

        var commandBuffer = _api.AcquireCommandBuffer(_device, out var acquireError);
        if (!commandBuffer.IsValid || acquireError is not null)
        {
            Fail(acquireError ?? "SDL_GPU command buffer acquire returned an invalid handle.");
        }

        State = SdlGpuLifecycleState.FrameOpen;
        var frame = new SdlGpuFrame(commandBuffer, ++_nextFrameSequence);
        Log(SdlGpuLifecycleEventKind.FrameBegan, "SDL_GPU frame began.");
        return frame;
    }

    public void EndFrame(SdlGpuFrame frame)
    {
        if (State != SdlGpuLifecycleState.FrameOpen)
        {
            ThrowInvalidState("submit an SDL_GPU frame");
        }

        if (!frame.IsValid || frame.Sequence != _nextFrameSequence)
        {
            ThrowInvalidState("submit this SDL_GPU frame");
        }

        if (!_api.SubmitCommandBuffer(frame.CommandBuffer, out var submitError))
        {
            Fail(submitError ?? "SDL_GPU command buffer submit failed.");
        }

        State = SdlGpuLifecycleState.WindowClaimed;
        Log(SdlGpuLifecycleEventKind.FrameSubmitted, "SDL_GPU frame submitted.");
    }

    public void Resize(int width, int height, float dpiScale)
    {
        EnsureWindowClaimed("resize SDL_GPU window");

        Window = Window.WithSize(width, height, dpiScale);
        Log(SdlGpuLifecycleEventKind.Resized, "SDL_GPU window resized.");
    }

    public void SetFullscreen(bool fullscreen)
    {
        EnsureWindowClaimed("change SDL_GPU fullscreen state");

        Window = Window.WithFullscreen(fullscreen);
        Log(SdlGpuLifecycleEventKind.FullscreenChanged, "SDL_GPU fullscreen state changed.");
    }

    public void Shutdown()
    {
        if (State == SdlGpuLifecycleState.Shutdown)
        {
            return;
        }

        _api.DestroyDevice(_device);
        _device = default;
        State = SdlGpuLifecycleState.Shutdown;
        Log(SdlGpuLifecycleEventKind.Shutdown, "SDL_GPU backend shut down.");
    }

    private void EnsureWindowClaimed(string operation)
    {
        if (State is not SdlGpuLifecycleState.WindowClaimed and not SdlGpuLifecycleState.FrameOpen)
        {
            ThrowInvalidState(operation);
        }
    }

    private void Fail(string error)
    {
        State = SdlGpuLifecycleState.Failed;
        Log(SdlGpuLifecycleEventKind.DeviceError, "SDL_GPU lifecycle error.", error);
        throw new InvalidOperationException(error);
    }

    private void ThrowInvalidState(string operation)
    {
        throw new InvalidOperationException(
            $"Cannot {operation} while SDL_GPU backend is in the {State} state.");
    }

    private void Log(SdlGpuLifecycleEventKind kind, string message, string? error = null)
    {
        _events.Add(new SdlGpuLifecycleEvent(kind, message, Window, error));
    }
}
