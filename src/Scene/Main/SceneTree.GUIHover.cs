namespace Electron2D;

public sealed partial class SceneTree
{

    internal void EnterGUIViewport(Viewport viewport)
    {
        EnsureOwnerThread();
        using var scope = SelectGUI(viewport);
        var point = viewport.GetMousePosition();
        RefreshGUIHoverAt(viewport, point);
    }

    internal void RefreshGUIHover()
    {
        EnsureOwnerThread();
        if (!_isDispatchingInput && !_gui.UpdatingGUIHover) { RefreshAllGUI(hover: true); return; }
        if (!_gui.GuiHoverKnown || _gui.GuiHoverViewport is not { } viewport) return;
        if (_gui.UpdatingGUIHover) { _gui.GuiHoverRefreshPending = true; return; }
        if (_isDispatchingInput) { _gui.GuiHoverRefreshPending = true; return; }
        RefreshGUIHoverAt(viewport, _gui.GuiHoverPosition);
    }

    internal void ReleaseGUIHover(Control control)
    {
        EnsureOwnerThread();
        using var scope = SelectGUI(control.GetViewport());
        ReleaseGUITouchFocus(control);
        if (ReferenceEquals(control, _gui.TooltipControl) || ReferenceEquals(control, _gui.TooltipOwner)) CancelTooltip();
        if (!_gui.GuiHoverChain.Contains(control)) return;
        ClearGUIHover();
    }

    internal void ClearGUIHover()
    {
        EnsureOwnerThread();
        CancelTooltip();
        _gui.GuiHoverKnown = false;
        if (_gui.UpdatingGUIHover) { _gui.GuiHoverRefreshPending = true; return; }
        List<Exception>? errors = null;
        ChangeGUIHover(null, null, ref errors);
        ApplyGUICursor(ref errors);
        ThrowCollected("GUI pointer-exit callbacks failed.", errors);
    }

    internal void RefreshGUICursor(Control? changed = null)
    {
        EnsureOwnerThread();
        using var scope = SelectGUI(changed?.GetViewport());
        if (changed is not null && !_gui.GuiHoverChain.Contains(changed)) return;
        if (changed is SubViewportContainer && _gui.GuiHoverKnown)
        {
            if (_isDispatchingInput) { List<Exception>? hoverErrors = null; UpdateGUIHover(_gui.Viewport!, _gui.GuiHoverPosition, ref hoverErrors); ThrowCollected("Embedded cursor refresh failed.", hoverErrors); }
            else RefreshGUIHoverAt(_gui.Viewport!, _gui.GuiHoverPosition);
        }
        List<Exception>? errors = null;
        ApplyGUICursor(ref errors);
        ThrowCollected("GUI cursor update failed.", errors);
    }

    private void RefreshGUIHoverAt(Viewport viewport, Vector2 point)
    {
        if (_gui.UpdatingGUIHover || _isDispatchingInput)
        {
            _gui.GuiHoverViewport = viewport;
            _gui.GuiHoverPosition = point;
            _gui.GuiHoverKnown = true;
            _gui.GuiHoverRefreshPending = true;
            return;
        }
        CaptureInputNodes();
        List<Exception>? errors = null;
        try { UpdateGUIHover(viewport, point, ref errors); }
        finally { _gui.InputTraversal.Clear(); }
        ThrowCollected("GUI hover callbacks failed.", errors);
    }

    private void UpdateGUIHover(Viewport viewport, Vector2 position, ref List<Exception>? errors)
    {
        _gui.GuiHoverViewport = viewport;
        _gui.GuiHoverPosition = position;
        _gui.GuiHoverKnown = true;
        if (_gui.UpdatingGUIHover) { _gui.GuiHoverRefreshPending = true; return; }

        _gui.UpdatingGUIHover = true;
        try
        {
            for (var pass = 0; pass < 8; pass++)
            {
                _gui.GuiHoverRefreshPending = false;
                var currentViewport = _gui.GuiHoverViewport ?? viewport;
                var target = _gui.GuiHoverKnown ? FindMouseControl(currentViewport, _gui.GuiHoverPosition, ref errors) : null;
                _gui.Section.MouseTarget = target;
                var chain = target is null ? null : BuildGUIHoverChain(target, currentViewport);
                ChangeGUIHover(target, chain, ref errors);
                if (target is SubViewportContainer container)
                {
                    container.Tree!.UpdateEmbeddedHover(container, _gui.GuiHoverPosition, ref errors);
                    if (container.MouseTarget) _gui.Section.MouseTarget = container;
                }
                ApplyGUICursor(ref errors);
                if (!_gui.GuiHoverRefreshPending) break;
            }
        }
        finally { _gui.GuiHoverScratch.Clear(); _gui.UpdatingGUIHover = false; }
    }

    private List<Control> BuildGUIHoverChain(Control target, Viewport viewport)
    {
        var chain = _gui.GuiHoverScratch; chain.Clear();
        for (CanvasItem? item = target; item is not null; item = item.GetParentItem())
        {
            if (item is Control control && !control.IsDisposed && control.IsVisibleInTree &&
                control.EffectiveMouseFilter != MouseFilter.Ignore && ReferenceEquals(control.GetViewport(), viewport))
            {
                chain.Add(control);
                if (control.EffectiveMouseFilter == MouseFilter.Stop) break;
            }
            if (item.TopLevel) break;
        }
        chain.Reverse();
        return chain;
    }

    private void ChangeGUIHover(Control? target, List<Control>? chain, ref List<Exception>? errors)
    {
        var count = chain?.Count ?? 0;
        var common = 0;
        while (common < _gui.GuiHoverChain.Count && common < count &&
               ReferenceEquals(_gui.GuiHoverChain[common], chain![common])) common++;
        if (common == _gui.GuiHoverChain.Count && common == count && ReferenceEquals(target, _gui.GuiHoverTarget)) return;

        var previousTarget = _gui.GuiHoverTarget;
        if (target is null && ReferenceEquals(_gui.Section.MouseTarget, previousTarget)) _gui.Section.MouseTarget = null;
        if (previousTarget is SubViewportContainer previousContainer && !ReferenceEquals(previousTarget, target))
            for (var i = 0; i < previousContainer.GetChildCount(); i++)
                if (previousContainer.GetChild(i) is SubViewport child)
                    try { child.NotifyMouseExited(); } catch (Exception error) { CollectException(ref errors, error); }
        var depth = _gui.GuiHoverChangeDepth++;
        if (depth == _gui.GuiHoverPrevious.Count) _gui.GuiHoverPrevious.Add([]);
        var previousChain = _gui.GuiHoverPrevious[depth]; previousChain.AddRange(_gui.GuiHoverChain);
        try
        {
            _gui.GuiHoverTarget = target;
            _gui.GuiHoverChain.Clear();
            if (chain is not null) _gui.GuiHoverChain.AddRange(chain);

            if (!ReferenceEquals(previousTarget, target) && previousTarget is { IsDisposed: false })
                try { previousTarget.DispatchNotification(Control.NotificationMouseExitSelf); }
                catch (Exception error) { CollectException(ref errors, error); }

            for (var index = previousChain.Count - 1; index >= common; index--)
            {
                var control = previousChain[index];
                if (control.IsDisposed) continue;
                try { control.DispatchNotification(Control.NotificationMouseExit); }
                catch (Exception error) { CollectException(ref errors, error); }
                try { control.NotifyMouseExited(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }

            for (var index = common; index < count; index++)
            {
                var control = chain![index];
                if (control.IsDisposed || !ReferenceEquals(control.Tree, this) || !control.IsVisibleInTree) continue;
                try { control.DispatchNotification(Control.NotificationMouseEnter); }
                catch (Exception error) { CollectException(ref errors, error); }
                try { control.NotifyMouseEntered(); }
                catch (Exception error) { CollectException(ref errors, error); }
            }

            if (!ReferenceEquals(previousTarget, target) && target is { IsDisposed: false } && ReferenceEquals(target.Tree, this))
                try { target.DispatchNotification(Control.NotificationMouseEnterSelf); }
                catch (Exception error) { CollectException(ref errors, error); }
        }
        finally { previousChain.Clear(); _gui.GuiHoverChangeDepth--; }
    }

    private void ApplyGUICursor(ref List<Exception>? errors)
    {
        if (_gui.GuiHoverViewport is not Window || DisplayServer.Service is not { } display) return;
        var shape = Input.Service.DefaultCursorShape;
        if (_gui.Section.GuiDragPayload is not null)
            shape = _gui.Section.GuiDragPossible ? CursorShape.CanDrop : CursorShape.Forbidden;
        else if (_gui.GuiHoverKnown && _gui.GuiHoverChain.Count != 0)
        {
            shape = CursorShape.Arrow;
            if (_gui.Viewport is { } viewport && MouseTargetControl(viewport) is { } nested && !ReferenceEquals(nested.GetViewport(), viewport))
            {
                try { shape = nested.GetCursorShape(nested.MakeCanvasPositionLocal(PointInViewport(viewport, nested.GetViewport()!, _gui.GuiHoverPosition))); }
                catch (Exception error) { CollectException(ref errors, error); }
                try { if (display.CursorGetShapeCore() != shape) display.CursorSetShapeCore(shape); } catch (Exception error) { CollectException(ref errors, error); }
                return;
            }
            for (var index = _gui.GuiHoverChain.Count - 1; index >= 0; index--)
            {
                var control = _gui.GuiHoverChain[index];
                if (control.IsDisposed || !ReferenceEquals(control.Tree, this) || !control.IsVisibleInTree) continue;
                try
                {
                    var local = control.MakeCanvasPositionLocal(_gui.GuiHoverPosition);
                    shape = control.GetCursorShape(local);
                    if (shape != CursorShape.Arrow || control.EffectiveMouseFilter == MouseFilter.Stop) break;
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        try
        {
            if (display.CursorGetShapeCore() != shape)
                display.CursorSetShapeCore(shape);
        }
        catch (Exception error) { CollectException(ref errors, error); }
    }
}
