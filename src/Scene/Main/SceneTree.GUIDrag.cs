namespace Electron2D;

public sealed partial class SceneTree
{
    private DragPayload? _guiDragPayload;
    private Control? _guiDragSource;
    private Control? _guiDragHovered;
    private Control? _guiDragPreview;
    private CanvasLayer? _guiDragPreviewLayer;
    private Vector2 _guiDragTravel, _guiDragPointer;
    private bool _guiDragAttempted, _guiDragPreparing, _guiDragCompleting, _guiDragPossible, _guiDragSuccessful;
    private uint _guiDragPreviewSerial;

    internal DragPayload? GetGUIDragData(Viewport viewport)
    {
        EnsureOwnerThread(); return ReferenceEquals(Root, viewport) ? _guiDragPayload : null;
    }

    internal bool IsGUIDragging(Viewport viewport)
    {
        EnsureOwnerThread(); return ReferenceEquals(Root, viewport) && (_guiDragPreparing || _guiDragPayload is not null);
    }

    internal bool IsGUIDragSuccessful(Viewport viewport)
    {
        EnsureOwnerThread(); return ReferenceEquals(Root, viewport) && _guiDragSuccessful;
    }

    internal void ForceGUIDrag(Control source, DragPayload payload, Control? preview)
    {
        EnsureOwnerThread();
        if (Root is not Viewport viewport || !ReferenceEquals(source.Tree, this) ||
            !ReferenceEquals(source.GetViewport(), viewport))
            throw new InvalidOperationException("The drag source must belong to the root viewport.");
        if (_guiDragPreparing || _guiDragPayload is not null || _guiDragCompleting)
            throw new InvalidOperationException("A GUI drag is already active.");
        if (preview is not null) ValidateGUIDragPreview(preview);
        _guiDragPreparing = true; _guiDragSource = source;
        try
        {
            if (preview is not null) SetGUIDragPreview(source, preview);
            _guiDragPayload = payload;
        }
        catch
        {
            _guiDragPreparing = false; _guiDragSource = null;
            ClearGUIDragPreview();
            throw;
        }
        _guiDragPreparing = false;
        _guiMouseCapture = null; _guiMouseCaptureMask = 0; _guiDragAttempted = true;
        Root.PropagateNotification(Node.NotificationDragBegin);
    }

    internal void SetGUIDragPreview(Control caller, Control preview)
    {
        EnsureOwnerThread();
        if (Root is not Viewport viewport || !ReferenceEquals(caller.Tree, this) ||
            !ReferenceEquals(caller.GetViewport(), viewport) || !IsGUIDragging(viewport))
            throw new InvalidOperationException("A preview requires a drag in the root viewport.");
        ValidateGUIDragPreview(preview);
        ClearGUIDragPreview();
        var layer = new CanvasLayer { Layer = int.MaxValue };
        while (Root.GetNodeOrNull("_DragPreview" + _guiDragPreviewSerial) is not null) _guiDragPreviewSerial++;
        layer.Name = "_DragPreview" + _guiDragPreviewSerial++;
        try
        {
            Root.AddChild(layer, Node.InternalMode.Front);
            preview.MouseFilter = MouseFilter.Ignore;
            preview.Position = _guiDragPointer;
            layer.AddChild(preview, Node.InternalMode.Front);
            _guiDragPreviewLayer = layer; _guiDragPreview = preview;
        }
        catch
        {
            layer.Dispose();
            throw;
        }
    }

    private static void ValidateGUIDragPreview(Control preview)
    {
        ObjectDisposedException.ThrowIf(preview.IsDisposed, preview);
        if (preview.Parent is not null || preview.Tree is not null || preview.IsQueuedForDeletion)
            throw new ArgumentException("A drag preview must be a detached, parentless live control.", nameof(preview));
    }

    private void ClearGUIDragPreview()
    {
        var layer = _guiDragPreviewLayer;
        _guiDragPreviewLayer = null; _guiDragPreview = null;
        layer?.Dispose();
    }

    private void AbortGUIDragDuringRollback(Viewport viewport)
    {
        if (_guiDragPayload is not null) { CancelGUIDrag(viewport); return; }
        _guiDragPreparing = false; _guiDragSource = null; _guiDragHovered = null;
        _guiDragPossible = false; _guiDragAttempted = false;
        viewport.ClearGUIDragDescription();
        ClearGUIDragPreview();
    }

    internal void CancelGUIDrag(Viewport viewport)
    {
        EnsureOwnerThread();
        if (!ReferenceEquals(Root, viewport) || _guiDragCompleting || _guiDragPayload is null) return;
        List<Exception>? errors = null;
        CompleteGUIDrag(viewport, _guiDragPointer, attemptDrop: false, ref errors);
        ThrowCollected("GUI drag cancellation callbacks failed.", errors);
    }

    internal void ReleaseGUIDrag(Control control)
    {
        EnsureOwnerThread();
        if (ReferenceEquals(control, _guiDragSource) && Root is Viewport viewport && _guiDragPayload is not null)
            CancelGUIDrag(viewport);
        else if (ReferenceEquals(control, _guiDragHovered))
            _guiDragHovered = null;
        else if (ReferenceEquals(control, _guiDragPreview))
            _guiDragPreview = null;
    }

    private void BeginAutomaticGUIDrag(Viewport viewport, Control captured, Vector2 origin, ref List<Exception>? errors)
    {
        _guiDragPreparing = true;
        for (CanvasItem? item = captured; item is not null;)
        {
            var next = item.TopLevel ? null : item.GetParentItem();
            if (item is Control candidate && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                candidate.EffectiveMouseFilter != MouseFilter.Ignore)
            {
                _guiDragSource = candidate;
                DragPayload? payload = null;
                try { payload = candidate.GetDragData(candidate.MakeCanvasPositionLocal(origin)); }
                catch (Exception error) { CollectException(ref errors, error); }
                if (payload is not null && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                    candidate.IsVisibleInTree && candidate.EffectiveMouseFilter != MouseFilter.Ignore)
                {
                    _guiDragPayload = payload; _guiDragPreparing = false;
                    _guiMouseCapture = null; _guiMouseCaptureMask = 0;
                    try { Root.PropagateNotification(Node.NotificationDragBegin); }
                    catch (Exception error) { CollectException(ref errors, error); }
                    return;
                }
                try { ClearGUIDragPreview(); } catch (Exception error) { CollectException(ref errors, error); }
                if (candidate.EffectiveMouseFilter == MouseFilter.Stop) break;
            }
            item = next;
        }
        _guiDragPreparing = false; _guiDragSource = null;
        viewport.ClearGUIDragDescription();
    }

    private Control? FindGUIDropTarget(Control? target, Viewport viewport, Vector2 point, DragPayload payload,
        ref List<Exception>? errors)
    {
        for (CanvasItem? item = target; item is not null;)
        {
            var next = item.TopLevel ? null : item.GetParentItem();
            if (item is Control candidate && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                ReferenceEquals(candidate.GetViewport(), viewport) && candidate.IsVisibleInTree &&
                candidate.EffectiveMouseFilter != MouseFilter.Ignore)
            {
                try
                {
                    var local = candidate.MakeCanvasPositionLocal(point);
                    if (candidate.CanDropData(local, payload) && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                        ReferenceEquals(candidate.GetViewport(), viewport) && candidate.IsVisibleInTree &&
                        candidate.EffectiveMouseFilter != MouseFilter.Ignore)
                        return candidate;
                }
                catch (Exception error) { CollectException(ref errors, error); }
                if (candidate.EffectiveMouseFilter == MouseFilter.Stop) break;
            }
            item = next;
        }
        return null;
    }

    private void UpdateGUIDrag(Viewport viewport, Vector2 point, ref List<Exception>? errors)
    {
        if (_guiDragPayload is not { } payload) return;
        _guiDragPointer = point;
        if (_guiDragPreview is { IsDisposed: false } preview)
            try { preview.Position = point; } catch (Exception error) { CollectException(ref errors, error); }
        var target = FindMouseControl(viewport, point, ref errors);
        var accepted = FindGUIDropTarget(target, viewport, point, payload, ref errors);
        if (!ReferenceEquals(_guiDragPayload, payload)) return;
        _guiDragHovered = accepted; _guiDragPossible = accepted is not null;
        ApplyGUICursor(ref errors);
    }

    private void CompleteGUIDrag(Viewport viewport, Vector2 point, bool attemptDrop, ref List<Exception>? errors)
    {
        if (_guiDragPayload is not { } payload || _guiDragCompleting) return;
        _guiDragCompleting = true;
        var successful = false;
        try
        {
            if (attemptDrop)
            {
                var target = FindMouseControl(viewport, point, ref errors);
                var accepted = FindGUIDropTarget(target, viewport, point, payload, ref errors);
                if (accepted is not null)
                    try { accepted.DropData(accepted.MakeCanvasPositionLocal(point), payload); successful = true; }
                    catch (Exception error) { CollectException(ref errors, error); }
            }
            try { ClearGUIDragPreview(); } catch (Exception error) { CollectException(ref errors, error); }
            _guiDragPayload = null; _guiDragSource = null; _guiDragHovered = null;
            _guiDragPreparing = false; _guiDragAttempted = false; _guiDragPossible = false;
            _guiDragTravel = Vector2.Zero; _guiDragSuccessful = successful;
            viewport.ClearGUIDragDescription();
            try { Root.PropagateNotification(Node.NotificationDragEnd); }
            catch (Exception error) { CollectException(ref errors, error); }
            ApplyGUICursor(ref errors);
        }
        finally { _guiDragCompleting = false; }
    }
}
