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
    /// supplies the initial attachment until explicitly replaced. Attachment is renderer-session state and does
    /// not change scene authoring, input coordinates or movie capture of the root viewport.</remarks>
    /// <exception cref="ArgumentException">The identity or destination rectangle is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The window identity is below minus one or is absent or retired.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner, capture or submission rules are violated.</exception>
    public static void ViewportAttachToScreen(RID viewport, Rect2 rect = default, int screen = 0) => RequireService().ViewportAttachToScreenCore(viewport, rect, screen);

    private void AddPresentation(CanvasFrame frame, CanvasBackend backend, int windowID)
    {
        if (frame.Drawn && ScreenAttachment(frame.Viewport, out var rect, out var id) && id == windowID && backend.FindTarget(frame.Viewport) is { } target)
            _presentations.Insert(0, new(target, rect == default ? new(Vector2.Zero, target.Size) : rect, ReferenceEquals(frame.Viewport, _window) && ViewportDebanding(frame.Viewport)));
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
        // Stable topological order: children first, retaining activation order among siblings.
        for (var i = _presentationFrames.Count - 1; i >= 0; i--)
            if (RenderParent(_presentationFrames[i].Viewport) is null)
            { _presentationQueue.Add(_presentationFrames[i]); AddPresentation(_presentationFrames[i], backend, windowID); }
        for (var index = 0; index < _presentationQueue.Count; index++)
            for (var i = _presentationFrames.Count - 1; i >= 0; i--)
                if (ReferenceEquals(RenderParent(_presentationFrames[i].Viewport), _presentationQueue[index].Viewport))
                { _presentationQueue.Add(_presentationFrames[i]); AddPresentation(_presentationFrames[i], backend, windowID); }
        var clearEmptyWindow = (!_serverViewports.TryGetValue(window, out var state) || state.Screen is null)
            && ViewportActive(window) && UpdateMode(window) != ViewportUpdateMode.Disabled
            && (window.ContentLayout.TargetSize.X == 0 || window.ContentLayout.TargetSize.Y == 0);
        var screen = _renderingDevice?.ScreenPresentation(windowID) ?? default;
        if (_presentations.Count != 0 || clearEmptyWindow || screen.Texture != 0)
        {
            _submittingTextures = true;
            try
            {
                var presented = true;
                if (presentation is not null) presented = ((GPUCanvasBackend)_backend).Present(presentation, CollectionsMarshal.AsSpan(_presentations), screen.Texture, screen.Size);
                else if (backend is GPUCanvasBackend gpu) presented = gpu.Present(CollectionsMarshal.AsSpan(_presentations), screen.Texture, screen.Size);
                else backend.Present(CollectionsMarshal.AsSpan(_presentations));
                if (presented) { _presentedWindows.Add(windowID); _renderingDevice?.ScreenPresented(windowID); }
            }
            finally { _submittingTextures = false; }
        }
        _presentationQueue.Clear();
    }
}
