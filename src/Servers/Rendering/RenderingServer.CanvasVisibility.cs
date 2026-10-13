namespace Electron2D;

public sealed partial class RenderingServer
{
    private readonly List<CanvasVisibilityNotification> _canvasVisibilityItems = [];
    private readonly List<(CanvasVisibilityNotification Notification, ulong Revision, bool Entered)> _canvasVisibilityChanges = [];

    /// <summary>Enables or removes render visibility callbacks on a retained canvas item.</summary>
    /// <param name="item">A live owned or borrowed canvas item in the active renderer.</param>
    /// <param name="enable">True to enable sampling; false to remove the notification silently.</param>
    /// <param name="area">Finite local bounds extending command/custom bounds; ignored when disabled.</param>
    /// <param name="entered">Optional callback after any submitted viewport first includes the item.</param>
    /// <param name="exited">Optional callback after a submitted frame includes the item in no viewport.</param>
    /// <remarks>Uses conservative bounds, parent visibility/modulation, cull layers and clipping, without occlusion
    /// testing. State starts false and commits once across all viewports after successful backend submission.
    /// Callbacks run on the renderer owner before FramePostDraw; later transitions are attempted after failures.
    /// Updating bounds/callbacks preserves current state and cancels stale queued delivery. Disable/free/disposal
    /// clear callbacks without an exit notification. This is runtime state and is not stored in packed scenes.</remarks>
    /// <exception cref="ArgumentException">Enabled bounds or their endpoint are nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The renderer is absent, the RID/owner is invalid, or mutation
    /// occurs during native submission, shutdown or generated tile ownership.</exception>
    public static void CanvasItemSetVisibilityNotifier(RID item, bool enable, Rect2 area, Action? entered, Action? exited) =>
        RequireService().CanvasItemSetVisibilityNotifierCore(item, enable, area, entered, exited);

    private void CanvasItemSetVisibilityNotifierCore(RID rid, bool enable, Rect2 area, Action? entered, Action? exited)
    {
        if (enable && (!area.IsFinite() || !area.End.IsFinite())) throw new ArgumentException("Visibility bounds must be finite.", nameof(area));
        var state = Change(rid, out var item);
        if (!enable)
        {
            if (state.VisibilityNotifier is { } previous) { previous.Cancel(); _canvasVisibilityItems.Remove(previous); state.VisibilityNotifier = null; }
            return;
        }
        if (state.VisibilityNotifier is { } existing)
        {
            existing.Area = area; existing.Entered = entered; existing.Exited = exited; existing.Revision++;
            return;
        }
        var notifier = new CanvasVisibilityNotification(item) { Area = area, Entered = entered, Exited = exited };
        state.VisibilityNotifier = notifier; _canvasVisibilityItems.Add(notifier);
    }

    private void BeginCanvasVisibilityFrame()
    {
        foreach (var notifier in _canvasVisibilityItems) notifier.Seen = false;
    }
    private void SampleCanvasVisibility(CanvasItem item, Transform transform, Rect2i? clip, Vector2i pixels)
    {
        if (item.ServerState?.VisibilityNotifier is not { Enabled: true, Seen: false } notifier || item.RenderInheritedModulate.A < 0.007f) return;
        var bounds = transform * item.RenderBounds();
        if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new InvalidOperationException("Transformed visibility bounds exceed the finite canvas range.");
        var viewport = new Rect2(Vector2.Zero, pixels);
        if (clip is { } clipped)
        {
            var clipRect = new Rect2(clipped.Position, clipped.Size);
            if (!viewport.Intersects(clipRect, includeBorders: true)) return;
            viewport = viewport.Intersection(clipRect);
        }
        notifier.Seen = bounds.Intersects(viewport, includeBorders: true);
    }
    private void CaptureCanvasVisibilityChanges()
    {
        _canvasVisibilityChanges.Clear();
        for (var i = 0; i < _canvasVisibilityItems.Count; i++)
        {
            var notifier = _canvasVisibilityItems[i];
            if (!notifier.IsCurrent)
            {
                notifier.Cancel(); _canvasVisibilityItems.RemoveAt(i--); continue;
            }
            if (notifier.OnScreen == notifier.Seen) continue;
            notifier.OnScreen = notifier.Seen;
            _canvasVisibilityChanges.Add((notifier, notifier.Revision, notifier.OnScreen));
        }
    }
    private void DeliverCanvasVisibilityChanges(ref Exception? error)
    {
        try
        {
            foreach (var change in _canvasVisibilityChanges)
                if (change.Notification.IsCurrent && change.Notification.Revision == change.Revision)
                    try { (change.Entered ? change.Notification.Entered : change.Notification.Exited)?.Invoke(); }
                    catch (Exception failure) { AddCanvasVisibilityError(ref error, failure); }
        }
        finally { _canvasVisibilityChanges.Clear(); }
    }
    private static void AddCanvasVisibilityError(ref Exception? error, Exception next) =>
        error = error is null ? next : new AggregateException("Canvas frame callbacks failed.", error, next);
}

internal sealed class CanvasVisibilityNotification(CanvasItem item)
{
    private readonly WeakReference<CanvasItem> _item = new(item);
    internal Rect2 Area;
    internal Action? Entered, Exited;
    internal bool Enabled = true, Seen, OnScreen;
    internal ulong Revision;
    internal bool IsCurrent => Enabled && _item.TryGetTarget(out var current) && !current.IsDisposed && ReferenceEquals(current.ServerState?.VisibilityNotifier, this);
    internal void Cancel() { Enabled = Seen = OnScreen = false; Entered = Exited = null; Revision++; }
}
