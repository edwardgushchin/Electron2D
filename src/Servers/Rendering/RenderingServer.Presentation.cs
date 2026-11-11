using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<CanvasPresentation> _presentations = [];
    private readonly HashSet<int> _presentedWindows = [];
    internal bool WasNativeWindowPresented(int windowID) => _presentedWindows.Contains(windowID);
    private readonly List<CanvasFrame> _presentationFrames = [], _presentationQueue = [];

    private bool ScreenAttachment(Viewport view, out Rect2 rect, out int windowID)
    {
        if (_serverViewports.TryGetValue(view, out var state) && state.Screen is { } screen)
        { rect = state.ScreenRect; windowID = screen; return NativeWindow(screen) is not null; }
        if (view is Window window && window.NativeDisplay is { } display)
        { rect = window.ContentLayout.ScreenRect; windowID = display.WindowID; return true; }
        rect = default; windowID = -1; return false;
    }
    private bool ScreenVisible(Viewport view) => ScreenAttachment(view, out var rect, out var id) && rect != default && NativeWindow(id) is { Visible: true };
    private void ViewportAttachToScreenCore(RID rid, Rect2 rect, int screen)
    {
        EnsureOwner();
        if (screen < -1) throw new ArgumentOutOfRangeException(nameof(screen));
        if (screen >= 0 && NativeWindow(screen) is null) throw new ArgumentOutOfRangeException(nameof(screen), "The native window is absent or retired.");
        if (!rect.Position.IsFinite() || !rect.Size.IsFinite() || !rect.End.IsFinite() || rect.Size.X < 0 || rect.Size.Y < 0)
            throw new ArgumentException("Screen rectangles require finite coordinates and nonnegative dimensions.", nameof(rect));
        var state = ChangeViewport(rid, out _); state.Screen = screen; state.ScreenRect = screen == -1 ? default : rect;
    }
    /// <summary>Attaches a viewport's completed image to a rectangle in a selected native window, or detaches it.</summary>
    /// <param name="viewport">A live scene or caller-owned viewport identity.</param>
    /// <param name="rect">Destination in native client pixels. A zero rectangle removes screen visibility;
    /// if another update policy still draws the viewport, it copies at its full size from the client origin.</param>
    /// <param name="screen">Active native window identity: zero selects main, positive IDs select children, and minus one detaches.</param>
    /// <remarks>Attachment schedules active non-disabled viewports with a nonzero rectangle. Only images drawn in
    /// the current frame are copied, with rendering children before parents and sibling activation order retained.
    /// Copies replace destination pixels, clip to the client and preserve viewport texture identity and dimensions.
    /// Presentation happens once after target drawing and is skipped by ForceDraw(false). Window content layout
    /// supplies the initial attachment until explicitly replaced. <see cref="ViewportSetRenderDirectToScreen"/>
    /// instead uses a nonempty attachment as an unscaled native draw region. Attachment is renderer-session state and does
    /// not change scene authoring, input coordinates or movie capture of the root viewport.</remarks>
    /// <exception cref="ArgumentException">The identity or destination rectangle is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The window identity is below minus one or is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner, capture or submission rules are violated.</exception>
    public static void ViewportAttachToScreen(RID viewport, Rect2 rect = default, int screen = 0) => RequireService().ViewportAttachToScreenCore(viewport, rect, screen);

    private bool ViewportDirectToScreen(Viewport view) => _serverViewports.GetValueOrDefault(view)?.DirectToScreen == true;

    private bool TryDirectRegion(Viewport view, Vector2i size, out Rect2 region)
    {
        region = default;
        if (!_swapBuffers || !ScreenAttachment(view, out var rect, out var id) || !rect.HasArea() ||
            NativeWindow(id) is not { Visible: true }) return false;
        var backend = id == 0 ? _backend : _nativeWindows[id].Backend;
        var requested = new Rect2(rect.Position, rect.Size.Min(size));
        if (!requested.Intersects(new Rect2(Vector2.Zero, backend.GetPixelSize()))) return false;
        if (id != 0 && ViewportTiming(view) is not null)
            throw new NotSupportedException("Direct rendering to a child window cannot use timing queries owned by the main context.");
        region = requested;
        return true;
    }

    /// <summary>Draws a viewport directly into its attached native window in the compatibility renderer.</summary>
    /// <param name="viewport">A live scene or caller-owned viewport.</param>
    /// <param name="enabled">True to draw in client pixels, false to restore a viewport texture.</param>
    /// <remarks>The attachment rectangle positions and clips the viewport without scaling. A direct viewport needs a
    /// visible window and a nonempty attachment to draw. Its viewport texture keeps its identity but has no readable
    /// or sampleable image. Screen copies, screen-dependent masks and groups are unavailable. Rendering without
    /// presenting leaves pending Once policies intact. Clear modes affect only this frame's composition.
    /// Changes are scoped to this renderer session.</remarks>
    /// <exception cref="ArgumentException">The viewport identity is invalid or retired.</exception>
    /// <exception cref="InvalidOperationException">Owner, capture or submission guards reject the change.</exception>
    /// <exception cref="NotSupportedException">The current renderer cannot draw directly to a native window.</exception>
    public static void ViewportSetRenderDirectToScreen(RID viewport, bool enabled)
    {
        var service = RequireService();
        var view = service.ServerViewport(viewport);
        service.EnsureServerViewportChange();
        if (enabled && service._backend is not CompatibilityCanvasBackend)
            throw new NotSupportedException("Direct viewport rendering requires the compatibility renderer.");
        if (enabled == service.ViewportDirectToScreen(view)) return;
        if (enabled) service.ReleaseNativeTarget(view);
        service.ChangeViewport(viewport, out _).DirectToScreen = enabled;
    }

    private bool AddPresentation(CanvasFrame frame, CanvasBackend backend, int windowID)
    {
        if (!frame.Drawn && !frame.DirectRegion.HasArea()) return false;
        if (!ScreenAttachment(frame.Viewport, out var rect, out var id) || id != windowID) return false;
        if (frame.DirectRegion.HasArea()) return true;
        if (frame.Drawn && backend.FindTarget(frame.Viewport) is { } target)
            _presentations.Insert(0, new(target, rect == default ? new(Vector2.Zero, target.Size) : rect, ReferenceEquals(frame.Viewport, _window) && ViewportDebanding(frame.Viewport)));
        return false;
    }

    private void PresentViewports()
    {
        _presentedWindows.Clear();
        if (!_swapBuffers) return;
        _presentationFrames.Clear();
        _presentationFrames.AddRange(_activeFrames);
        _presentationFrames.Sort(static (a, b) => a.Viewport.RenderActivationOrder.CompareTo(b.Viewport.RenderActivationOrder));
        if (_window.Visible) PresentWindowViewports(_window, 0, _backend, null);
        foreach (var pair in _nativeWindows)
            if (pair.Value.Window.Visible)
                PresentWindowViewports(pair.Value.Window, pair.Key, pair.Value.Backend, pair.Value.Presentation);
    }

    private void PresentWindowViewports(Window window, int windowID, CanvasBackend backend, GPUCanvasBackend.ChildPresentation? presentation)
    {
        _presentations.Clear(); _presentationQueue.Clear();
        var direct = false;
        // Stable topological order: children first, retaining activation order among siblings.
        for (var i = _presentationFrames.Count - 1; i >= 0; i--)
            if (RenderParent(_presentationFrames[i].Viewport) is null)
            { _presentationQueue.Add(_presentationFrames[i]); direct |= AddPresentation(_presentationFrames[i], backend, windowID); }
        for (var index = 0; index < _presentationQueue.Count; index++)
            for (var i = _presentationFrames.Count - 1; i >= 0; i--)
                if (ReferenceEquals(RenderParent(_presentationFrames[i].Viewport), _presentationQueue[index].Viewport))
                { _presentationQueue.Add(_presentationFrames[i]); direct |= AddPresentation(_presentationFrames[i], backend, windowID); }
        var clearEmptyWindow = (!_serverViewports.TryGetValue(window, out var state) || state.Screen is null)
            && ViewportActive(window) && UpdateMode(window) != ViewportUpdateMode.Disabled
            && (window.ContentLayout.TargetSize.X == 0 || window.ContentLayout.TargetSize.Y == 0);
        var screen = _renderingDevice?.ScreenPresentation(windowID) ?? default;
        if (direct || _presentations.Count != 0 || clearEmptyWindow || screen.Texture != 0)
        {
            _submittingTextures = true;
            try
            {
                var presented = true;
                if (direct) PresentDirectViewports((CompatibilityCanvasBackend)backend, windowID);
                else if (presentation is not null) presented = ((GPUCanvasBackend)_backend).Present(presentation, CollectionsMarshal.AsSpan(_presentations), screen.Texture, screen.Size, screen.HDR);
                else if (backend is GPUCanvasBackend gpu) presented = gpu.Present(CollectionsMarshal.AsSpan(_presentations), screen.Texture, screen.Size, screen.HDR);
                else backend.Present(CollectionsMarshal.AsSpan(_presentations));
                if (presented) { _presentedWindows.Add(windowID); _renderingDevice?.ScreenPresented(windowID); }
            }
            finally { _submittingTextures = false; }
            if (direct)
                foreach (var frame in _presentationQueue)
                    if (frame.DirectRegion.HasArea() && ScreenAttachment(frame.Viewport, out _, out var id) && id == windowID)
                    {
                        ViewportSubmitted(frame.Viewport);
                        frame.State = 2;
                        frame.Drawn = true;
                        _submittedFrames.Add(frame);
                    }
        }
        _presentationQueue.Clear();
    }

    private void PresentDirectViewports(CompatibilityCanvasBackend backend, int windowID)
    {
        var client = backend.GetPixelSize();
        backend.BeginDirectPresentation();
        for (var i = _presentationQueue.Count - 1; i >= 0; i--)
        {
            var frame = _presentationQueue[i];
            if (!ScreenAttachment(frame.Viewport, out var rect, out var id) || id != windowID) continue;
            if (!frame.DirectRegion.HasArea())
            {
                if (frame.Drawn && backend.FindTarget(frame.Viewport) is { } target)
                    backend.CopyPresentation(new(target, rect == default ? new(Vector2.Zero, target.Size) : rect), client);
                continue;
            }

            var timing = ViewportTiming(frame.Viewport);
            var start = timing is null ? 0 : Stopwatch.GetTimestamp();
            try
            {
                backend.DrawDirect(frame.DirectRegion, CollectionsMarshal.AsSpan(frame.Vertices),
                    CollectionsMarshal.AsSpan(frame.Batches), frame.Clear, frame.ClearEnabled, timing);
                frame.RenderObjects = backend.RenderObjects;
                frame.RenderPrimitives = backend.RenderPrimitives;
                frame.RenderDrawCalls = backend.RenderDrawCalls;
                timing?.AddCPU(Stopwatch.GetTimestamp() - start);
                timing?.EndFrame();
            }
            catch (Exception failure)
            {
                try { timing?.Abort(); }
                catch (Exception cleanup) { throw new AggregateException("Viewport timing cleanup failed.", failure, cleanup); }
                throw;
            }
        }
        backend.EndDirectPresentation();
    }
}
