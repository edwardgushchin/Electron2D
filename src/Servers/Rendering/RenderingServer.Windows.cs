using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private sealed class NativeWindowSurface(Window window, DisplayServer display, CanvasBackend backend,
        GPUCanvasBackend.ChildPresentation? presentation)
    {
        internal readonly Window Window = window;
        internal readonly DisplayServer Display = display;
        internal readonly CanvasBackend Backend = backend;
        internal readonly GPUCanvasBackend.ChildPresentation? Presentation = presentation;
        internal bool NeedsSeed = presentation is null;
    }

    private readonly Dictionary<int, NativeWindowSurface> _nativeWindows = [];
    private readonly List<CanvasFrame> _submittedFrames = [];
    private bool _nativeFrameStarted;
    internal bool NativeWindowMutationBlocked => _submittingTextures || _nativeFrameStarted;

    internal void RegisterNativeWindow(Window window, DisplayServer display)
    {
        EnsureNativeWindowMutation();
        if (window.Transparent && _backend is GPUCanvasBackend)
            throw new NotSupportedException("The SDL GPU API cannot present transparent native windows; use the compatibility renderer or configure an opaque child before showing it.");
        var handle = display.AcquireRenderingWindow();
        CanvasBackend? compatibility = null;
        GPUCanvasBackend.ChildPresentation? presentation = null;
        try
        {
            if (_backend is GPUCanvasBackend gpu)
                presentation = gpu.ClaimChildWindow(handle, display.RequestedVSync);
            else
            {
                compatibility = new CompatibilityCanvasBackend(handle, _backend.Driver);
                display.ApplyVSyncPolicy(compatibility);
            }
            var surface = new NativeWindowSurface(window, display, compatibility ?? _backend, presentation);
            _nativeWindows.Add(display.WindowID, surface);
            if (_serverViewports.TryGetValue(window, out var state)) state.Screen = null;
            display.SetGraphicsHandleQuery(surface.Backend.GetNativeHandle);
            MarkChanged();
        }
        catch
        {
            try { presentation?.Dispose(); compatibility?.Dispose(); }
            finally { display.ReleaseRenderingWindow(); }
            throw;
        }
    }

    internal void UnregisterNativeWindow(DisplayServer display)
    {
        EnsureNativeWindowMutation();
        if (!_nativeWindows.Remove(display.WindowID, out var surface)) return;
        try
        {
            if (surface.Presentation is { } presentation)
            { try { _backend.Sync(null); } finally { presentation.Dispose(); } }
            else surface.Backend.Dispose();
        }
        finally { display.ReleaseRenderingWindow(); MarkChanged(); }
    }

    internal void EnsureNativeWindowMutation()
    {
        EnsureOwner();
        if (_submittingTextures || _nativeFrameStarted)
            throw new InvalidOperationException("Native windows cannot change during native frame submission.");
    }

    private void BeginNativeFrames()
    {
        foreach (var surface in _nativeWindows.Values)
            if (surface.Presentation is null)
            {
                if (surface.NeedsSeed) { surface.Backend.SeedHistory(_backend); surface.NeedsSeed = false; }
                surface.Backend.BeginFrame();
            }
        _backend.BeginFrame();
    }

    private void ReplayNativeFrames()
    {
        foreach (var surface in _nativeWindows.Values)
            if (surface.Presentation is null)
                foreach (var frame in _submittedFrames)
                    surface.Backend.Draw(surface.Backend.FindTarget(frame.Viewport)!,
                        CollectionsMarshal.AsSpan(frame.Vertices), CollectionsMarshal.AsSpan(frame.Batches),
                        frame.Clear, frame.ClearEnabled, CanvasTime, CollectionsMarshal.AsSpan(frame.Instances), frame.Lighting);
    }

    private void EndNativeFrames()
    {
        foreach (var surface in _nativeWindows.Values)
            if (surface.Presentation is null) surface.Backend.EndFrame();
        _backend.EndFrame();
    }

    private void ReleaseNativeTarget(Viewport viewport)
    {
        _backend.ReleaseTarget(viewport);
        foreach (var surface in _nativeWindows.Values)
            if (surface.Presentation is null) surface.Backend.ReleaseTarget(viewport);
    }

    private Window? NativeWindow(int id) => id == 0 ? _window : _nativeWindows.GetValueOrDefault(id)?.Window;

    internal Image ReadbackNativeWindow(int id)
    {
        EnsureOwner();
        if (id == 0) return Readback();
        var surface = _nativeWindows[id];
        return surface.Presentation is { Target: { HasFrame: true } target }
            ? _backend.Readback(target) : surface.Backend.ReadbackScreen(surface.Backend.FindTarget(surface.Window));
    }

    private void CloseNativeWindows(ref List<Exception>? errors)
    {
        foreach (var surface in _nativeWindows.Values.ToArray())
            try { surface.Window.CloseNative(); } catch (Exception error) { (errors ??= []).Add(error); }
    }

    internal DisplayServer.VSyncMode GetVSync(int windowID)
    {
        EnsureOwner();
        if (windowID == 0) return _backend.VSync;
        var surface = _nativeWindows[windowID];
        return surface.Presentation?.VSync ?? surface.Backend.VSync;
    }

    internal void SetVSync(DisplayServer.VSyncMode mode, int windowID)
    {
        EnsureNativeWindowMutation();
        if (windowID == 0) { _backend.SetVSync(mode); return; }
        var surface = _nativeWindows[windowID];
        if (surface.Presentation is { } presentation) presentation.SetVSync(mode);
        else surface.Backend.SetVSync(mode);
    }
}
