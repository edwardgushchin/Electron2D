namespace Electron2D;

public sealed partial class SceneTree
{

    internal DragPayload? GetGUIDragData(Viewport viewport)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(viewport); return _gui.Section.GuiDragPayload;
    }

    internal bool IsGUIDragging(Viewport viewport)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(viewport); return _gui.Section.GuiDragPreparing || _gui.Section.GuiDragPayload is not null;
    }

    internal bool IsGUIDragSuccessful(Viewport viewport)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(viewport); return _gui.Section.GuiDragSuccessful;
    }

    internal void ForceGUIDrag(Control source, DragPayload payload, Control? preview)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(source.GetViewport());
        if (_gui.Section.Viewport is not { } viewport || !ReferenceEquals(source.Tree, this) ||
            !ReferenceEquals(GUIState(source.GetViewport()!).Section.Viewport, viewport))
            throw new InvalidOperationException("The drag source must belong to the viewport section.");
        if (_gui.Section.GuiDragPreparing || _gui.Section.GuiDragPayload is not null || _gui.Section.GuiDragCompleting)
            throw new InvalidOperationException("A GUI drag is already active.");
        if (preview is not null) ValidateGUIDragPreview(preview);
        _gui.Section.GuiDragPreparing = true; _gui.Section.GuiDragSource = source;
        try
        {
            if (preview is not null) SetGUIDragPreview(source, preview);
            _gui.Section.GuiDragPayload = payload; _gui.Section.DragViewport = source.GetViewport();
        }
        catch
        {
            _gui.Section.GuiDragPreparing = false; _gui.Section.GuiDragSource = null;
            ClearGUIDragPreview();
            throw;
        }
        _gui.Section.GuiDragPreparing = false;
        _gui.GuiMouseCapture = null; _gui.GuiMouseCaptureMask = 0; _gui.GuiDragAttempted = true;
        (_gui.Section.Viewport ?? viewport).PropagateNotification(Node.NotificationDragBegin);
    }

    internal void SetGUIDragPreview(Control caller, Control preview)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(caller.GetViewport());
        if (_gui.Section.Viewport is not { } viewport || !ReferenceEquals(caller.Tree, this) ||
            !ReferenceEquals(GUIState(caller.GetViewport()!).Section.Viewport, viewport) || !IsGUIDragging(viewport))
            throw new InvalidOperationException("A preview requires a drag in the viewport section.");
        ValidateGUIDragPreview(preview);
        ClearGUIDragPreview();
        var layer = new CanvasLayer { Layer = int.MaxValue };
        while (viewport.GetNodeOrNull("_DragPreview" + _gui.Section.GuiDragPreviewSerial) is not null) _gui.Section.GuiDragPreviewSerial++;
        layer.Name = "_DragPreview" + _gui.Section.GuiDragPreviewSerial++;
        try
        {
            viewport.AddChild(layer, Node.InternalMode.Front);
            preview.MouseFilter = MouseFilter.Ignore;
            preview.Position = _gui.Section.GuiDragPointer;
            layer.AddChild(preview, Node.InternalMode.Front);
            _gui.Section.GuiDragPreviewLayer = layer; _gui.Section.GuiDragPreview = preview;
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
        var layer = _gui.Section.GuiDragPreviewLayer;
        _gui.Section.GuiDragPreviewLayer = null; _gui.Section.GuiDragPreview = null;
        layer?.Dispose();
    }

    private void AbortGUIDragDuringRollback(Viewport viewport)
    {
        if (_gui.Section.GuiDragPayload is not null) { CancelGUIDrag(viewport); return; }
        _gui.Section.GuiDragPreparing = false; _gui.Section.GuiDragSource = null; _gui.Section.GuiDragHovered = null;
        _gui.Section.GuiDragPossible = false; _gui.GuiDragAttempted = false;
        (_gui.Section.Viewport ?? viewport).ClearGUIDragDescription();
        ClearGUIDragPreview();
    }

    internal void CancelGUIDrag(Viewport viewport)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(viewport);
        if (_gui.Section.GuiDragCompleting || _gui.Section.GuiDragPayload is null) return;
        List<Exception>? errors = null;
        CompleteGUIDrag(viewport, _gui.Section.GuiDragPointer, attemptDrop: false, ref errors);
        ThrowCollected("GUI drag cancellation callbacks failed.", errors);
    }

    internal void ReleaseGUIDrag(Control control)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(control.GetViewport());
        if (ReferenceEquals(control, _gui.Section.GuiDragSource) && _gui.Section.Viewport is { } viewport && _gui.Section.GuiDragPayload is not null)
            CancelGUIDrag(viewport);
        else if (ReferenceEquals(control, _gui.Section.GuiDragHovered))
            _gui.Section.GuiDragHovered = null;
        else if (ReferenceEquals(control, _gui.Section.GuiDragPreview))
            _gui.Section.GuiDragPreview = null;
    }

    private void BeginAutomaticGUIDrag(Viewport viewport, Control captured, Vector2 origin, ref List<Exception>? errors)
    {
        _gui.Section.GuiDragPreparing = true;
        for (CanvasItem? item = captured; item is not null;)
        {
            var next = item.TopLevel ? null : item.GetParentItem();
            if (item is Control candidate && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                candidate.EffectiveMouseFilter != MouseFilter.Ignore)
            {
                _gui.Section.GuiDragSource = candidate;
                DragPayload? payload = null;
                try { payload = candidate.GetDragData(candidate.MakeCanvasPositionLocal(origin)); }
                catch (Exception error) { CollectException(ref errors, error); }
                if (payload is not null && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                    candidate.IsVisibleInTree && candidate.EffectiveMouseFilter != MouseFilter.Ignore)
                {
                    _gui.Section.GuiDragPayload = payload; _gui.Section.DragViewport = viewport; _gui.Section.GuiDragPreparing = false;
                    _gui.GuiMouseCapture = null; _gui.GuiMouseCaptureMask = 0;
                    try { (_gui.Section.Viewport ?? viewport).PropagateNotification(Node.NotificationDragBegin); }
                    catch (Exception error) { CollectException(ref errors, error); }
                    return;
                }
                try { ClearGUIDragPreview(); } catch (Exception error) { CollectException(ref errors, error); }
                if (candidate.EffectiveMouseFilter == MouseFilter.Stop) break;
            }
            item = next;
        }
        _gui.Section.GuiDragPreparing = false; _gui.Section.GuiDragSource = null;
        (_gui.Section.Viewport ?? viewport).ClearGUIDragDescription();
    }

    private Control? FindGUIDropTarget(Control? target, Viewport viewport, Vector2 point, DragPayload payload,
        ref List<Exception>? errors)
    {
        for (CanvasItem? item = target; item is not null;)
        {
            var next = item.TopLevel ? null : item.GetParentItem();
            if (item is Control candidate && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                ReferenceEquals(GUIState(candidate.GetViewport()!).Section, _gui.Section) && candidate.IsVisibleInTree &&
                candidate.EffectiveMouseFilter != MouseFilter.Ignore)
            {
                try
                {
                    var local = candidate.MakeCanvasPositionLocal(PointInViewport(viewport, candidate.GetViewport()!, point));
                    if (candidate.CanDropData(local, payload) && !candidate.IsDisposed && ReferenceEquals(candidate.Tree, this) &&
                        ReferenceEquals(GUIState(candidate.GetViewport()!).Section, _gui.Section) && candidate.IsVisibleInTree &&
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
        if (_gui.Section.GuiDragPayload is not { } payload) return;
        _gui.Section.GuiDragPointer = PointInViewport(viewport, _gui.Section.Viewport ?? viewport, point);
        if (_gui.Section.GuiDragPreview is { IsDisposed: false } preview)
            try { preview.Position = _gui.Section.GuiDragPointer; } catch (Exception error) { CollectException(ref errors, error); }
        var target = MouseTargetControl(viewport) ?? FindMouseControl(viewport, point, ref errors);
        var accepted = FindGUIDropTarget(target, viewport, point, payload, ref errors);
        if (!ReferenceEquals(_gui.Section.GuiDragPayload, payload)) return;
        _gui.Section.GuiDragHovered = accepted; _gui.Section.GuiDragPossible = accepted is not null;
        ApplyGUICursor(ref errors);
    }

    private void CompleteGUIDrag(Viewport viewport, Vector2 point, bool attemptDrop, ref List<Exception>? errors)
    {
        if (_gui.Section.GuiDragPayload is not { } payload || _gui.Section.GuiDragCompleting) return;
        _gui.Section.GuiDragCompleting = true;
        var successful = false;
        try
        {
            if (attemptDrop)
            {
                var target = MouseTargetControl(viewport) ?? FindMouseControl(viewport, point, ref errors);
                var accepted = FindGUIDropTarget(target, viewport, point, payload, ref errors);
                if (accepted is not null)
                    try { accepted.DropData(accepted.MakeCanvasPositionLocal(PointInViewport(viewport, accepted.GetViewport()!, point)), payload); successful = true; }
                    catch (Exception error) { CollectException(ref errors, error); }
            }
            try { ClearGUIDragPreview(); } catch (Exception error) { CollectException(ref errors, error); }
            _gui.Section.GuiDragPayload = null; _gui.Section.DragViewport = null; _gui.Section.GuiDragSource = null; _gui.Section.GuiDragHovered = null;
            _gui.Section.GuiDragPreparing = false; _gui.GuiDragAttempted = false; _gui.Section.GuiDragPossible = false;
            _gui.GuiDragTravel = Vector2.Zero; _gui.Section.GuiDragSuccessful = successful;
            (_gui.Section.Viewport ?? viewport).ClearGUIDragDescription();
            try { (_gui.Section.Viewport ?? viewport).PropagateNotification(Node.NotificationDragEnd); }
            catch (Exception error) { CollectException(ref errors, error); }
            ApplyGUICursor(ref errors);
        }
        finally { _gui.Section.GuiDragCompleting = false; }
    }
}
