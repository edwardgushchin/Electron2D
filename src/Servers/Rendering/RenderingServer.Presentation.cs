using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<CanvasPresentation> _presentations = [];
    private readonly List<CanvasFrame> _presentationFrames = [], _presentationQueue = [];

    private bool ScreenAttachment(Viewport view, out Rect2 rect)
    {
        if (_serverViewports.TryGetValue(view, out var state) && state.Screen is { } screen)
        { rect = state.ScreenRect; return screen == 0; }
        rect = ReferenceEquals(view, _window) ? _window.ContentLayout.ScreenRect : default;
        return ReferenceEquals(view, _window);
    }
    private bool ScreenVisible(Viewport view) => _window.Visible && ScreenAttachment(view, out var rect) && rect != default;
    private void ViewportAttachToScreenCore(RID rid, Rect2 rect, int screen)
    {
        if (screen < -1) throw new ArgumentOutOfRangeException(nameof(screen));
        if (screen > 0) throw new NotSupportedException("Only the main native window is available for viewport presentation.");
        if (!rect.Position.IsFinite() || !rect.Size.IsFinite() || !rect.End.IsFinite() || rect.Size.X < 0 || rect.Size.Y < 0)
            throw new ArgumentException("Screen rectangles require finite coordinates and nonnegative dimensions.", nameof(rect));
        var state = ChangeViewport(rid, out _); state.Screen = screen; state.ScreenRect = screen == -1 ? default : rect;
    }
    /// <summary>Attaches a viewport's completed image to a rectangle in the main native window, or detaches it.</summary>
    /// <param name="viewport">A live scene or caller-owned viewport identity.</param>
    /// <param name="rect">Destination in native client pixels. A zero rectangle removes screen visibility;
    /// if another update policy still draws the viewport, it copies at its full size from the client origin.</param>
    /// <param name="screen">Native window identity: zero selects the main window; minus one detaches.</param>
    /// <remarks>Attachment schedules active non-disabled viewports with a nonzero rectangle. Only images drawn in
    /// the current frame are copied, with rendering children before parents and sibling activation order retained.
    /// Copies replace destination pixels, clip to the client and preserve viewport texture identity and dimensions.
    /// Presentation happens once after target drawing and is skipped by ForceDraw(false). Root content layout
    /// supplies the initial attachment until explicitly replaced. Attachment is renderer-session state and does
    /// not change scene authoring, input coordinates or movie capture of the root viewport.</remarks>
    /// <exception cref="ArgumentException">The identity or destination rectangle is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The window identity is below minus one.</exception>
    /// <exception cref="NotSupportedException">The window identity requires independent native windows.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner, capture or submission rules are violated.</exception>
    public static void ViewportAttachToScreen(RID viewport, Rect2 rect = default, int screen = 0) => RequireService().ViewportAttachToScreenCore(viewport, rect, screen);

    private void AddPresentation(CanvasFrame frame)
    {
        if (frame.Drawn && ScreenAttachment(frame.Viewport, out var rect) && _backend.FindTarget(frame.Viewport) is { } target)
            _presentations.Insert(0, new(target, rect == default ? new(Vector2.Zero, target.Size) : rect));
    }

    private void PresentViewports()
    {
        if (!_swapBuffers || !_window.Visible) return;
        _presentations.Clear(); _presentationFrames.Clear(); _presentationQueue.Clear();
        // Stable topological order: children first, retaining activation order among siblings.
        _presentationFrames.AddRange(_activeFrames);
        _presentationFrames.Sort(static (a, b) => a.Viewport.RenderActivationOrder.CompareTo(b.Viewport.RenderActivationOrder));
        for (var i = _presentationFrames.Count - 1; i >= 0; i--)
            if (RenderParent(_presentationFrames[i].Viewport) is null)
            { _presentationQueue.Add(_presentationFrames[i]); AddPresentation(_presentationFrames[i]); }
        for (var index = 0; index < _presentationQueue.Count; index++)
            for (var i = _presentationFrames.Count - 1; i >= 0; i--)
                if (ReferenceEquals(RenderParent(_presentationFrames[i].Viewport), _presentationQueue[index].Viewport))
                { _presentationQueue.Add(_presentationFrames[i]); AddPresentation(_presentationFrames[i]); }
        var clearEmptyRoot = (!_serverViewports.TryGetValue(_window, out var rootState) || rootState.Screen is null)
            && ViewportActive(_window) && UpdateMode(_window) != ViewportUpdateMode.Disabled
            && (_window.ContentLayout.TargetSize.X == 0 || _window.ContentLayout.TargetSize.Y == 0);
        if (_presentations.Count != 0 || clearEmptyRoot)
        {
            _submittingTextures = true;
            try { _backend.Present(CollectionsMarshal.AsSpan(_presentations)); }
            finally { _submittingTextures = false; }
        }
        _presentationQueue.Clear();
    }
}
