namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly List<Control> _guiHoverChain = [];
    private Control? _guiHoverTarget;
    private Viewport? _guiHoverViewport;
    private Vector2 _guiHoverPosition;
    private bool _guiHoverKnown;
    private bool _updatingGUIHover;
    private bool _guiHoverRefreshPending;

    internal void EnterGUIViewport(Viewport viewport)
    {
        EnsureOwnerThread();
        if (!ReferenceEquals(Root, viewport)) return;
        var point = viewport.GetMousePosition();
        RefreshGUIHoverAt(viewport, point);
    }

    internal void RefreshGUIHover()
    {
        EnsureOwnerThread();
        if (!_guiHoverKnown || _guiHoverViewport is not { } viewport) return;
        if (_updatingGUIHover) { _guiHoverRefreshPending = true; return; }
        if (_isDispatchingInput) { _guiHoverRefreshPending = true; return; }
        RefreshGUIHoverAt(viewport, _guiHoverPosition);
    }

    internal void ReleaseGUIHover(Control control)
    {
        EnsureOwnerThread();
        if (!_guiHoverChain.Contains(control)) return;
        ClearGUIHover();
    }

    internal void ClearGUIHover()
    {
        EnsureOwnerThread();
        _guiHoverKnown = false;
        if (_updatingGUIHover) { _guiHoverRefreshPending = true; return; }
        List<Exception>? errors = null;
        ChangeGUIHover(null, [], ref errors);
        ApplyGUICursor(ref errors);
        ThrowCollected("GUI pointer-exit callbacks failed.", errors);
    }

    internal void RefreshGUICursor(Control? changed = null)
    {
        EnsureOwnerThread();
        if (changed is not null && !_guiHoverChain.Contains(changed)) return;
        List<Exception>? errors = null;
        ApplyGUICursor(ref errors);
        ThrowCollected("GUI cursor update failed.", errors);
    }

    private void RefreshGUIHoverAt(Viewport viewport, Vector2 point)
    {
        if (_updatingGUIHover || _isDispatchingInput)
        {
            _guiHoverViewport = viewport;
            _guiHoverPosition = point;
            _guiHoverKnown = true;
            _guiHoverRefreshPending = true;
            return;
        }
        CaptureInputNodes();
        List<Exception>? errors = null;
        try { UpdateGUIHover(viewport, point, ref errors); }
        finally { _inputTraversal.Clear(); }
        ThrowCollected("GUI hover callbacks failed.", errors);
    }

    private void UpdateGUIHover(Viewport viewport, Vector2 position, ref List<Exception>? errors)
    {
        _guiHoverViewport = viewport;
        _guiHoverPosition = position;
        _guiHoverKnown = true;
        if (_updatingGUIHover) { _guiHoverRefreshPending = true; return; }

        _updatingGUIHover = true;
        try
        {
            for (var pass = 0; pass < 8; pass++)
            {
                _guiHoverRefreshPending = false;
                var currentViewport = _guiHoverViewport ?? viewport;
                var target = _guiHoverKnown ? FindMouseControl(currentViewport, _guiHoverPosition, ref errors) : null;
                var chain = target is null ? [] : BuildGUIHoverChain(target, currentViewport);
                ChangeGUIHover(target, chain, ref errors);
                ApplyGUICursor(ref errors);
                if (!_guiHoverRefreshPending) break;
            }
        }
        finally { _updatingGUIHover = false; }
    }

    private static List<Control> BuildGUIHoverChain(Control target, Viewport viewport)
    {
        var chain = new List<Control>();
        for (CanvasItem? item = target; item is not null; item = item.GetParentItem())
        {
            if (item is Control control && !control.IsDisposed && control.IsVisibleInTree &&
                control.EffectiveMouseFilter != ControlMouseFilter.Ignore && ReferenceEquals(control.GetViewport(), viewport))
            {
                chain.Add(control);
                if (control.EffectiveMouseFilter == ControlMouseFilter.Stop) break;
            }
            if (item.TopLevel) break;
        }
        chain.Reverse();
        return chain;
    }

    private void ChangeGUIHover(Control? target, List<Control> chain, ref List<Exception>? errors)
    {
        var common = 0;
        while (common < _guiHoverChain.Count && common < chain.Count &&
               ReferenceEquals(_guiHoverChain[common], chain[common])) common++;
        if (common == _guiHoverChain.Count && common == chain.Count && ReferenceEquals(target, _guiHoverTarget)) return;

        var previousTarget = _guiHoverTarget;
        var previousChain = _guiHoverChain.ToArray();
        _guiHoverTarget = target;
        _guiHoverChain.Clear();
        _guiHoverChain.AddRange(chain);

        if (!ReferenceEquals(previousTarget, target) && previousTarget is { IsDisposed: false })
            try { previousTarget.DispatchNotification(Control.NotificationMouseExitSelf); }
            catch (Exception error) { CollectException(ref errors, error); }

        for (var index = previousChain.Length - 1; index >= common; index--)
        {
            var control = previousChain[index];
            if (control.IsDisposed) continue;
            try { control.DispatchNotification(Control.NotificationMouseExit); }
            catch (Exception error) { CollectException(ref errors, error); }
            try { control.NotifyMouseExited(); }
            catch (Exception error) { CollectException(ref errors, error); }
        }

        for (var index = common; index < chain.Count; index++)
        {
            var control = chain[index];
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

    private void ApplyGUICursor(ref List<Exception>? errors)
    {
        if (_guiHoverViewport is not Window || DisplayServer.Instance is not { } display) return;
        var shape = Input.Instance.DefaultCursorShape;
        if (_guiHoverKnown && _guiHoverChain.Count != 0)
        {
            shape = Input.CursorShape.Arrow;
            for (var index = _guiHoverChain.Count - 1; index >= 0; index--)
            {
                var control = _guiHoverChain[index];
                if (control.IsDisposed || !ReferenceEquals(control.Tree, this) || !control.IsVisibleInTree) continue;
                try
                {
                    var local = control.MakeCanvasPositionLocal(_guiHoverPosition);
                    shape = (Input.CursorShape)control.GetCursorShape(local);
                    if (shape != Input.CursorShape.Arrow || control.EffectiveMouseFilter == ControlMouseFilter.Stop) break;
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        try
        {
            if (display.CursorGetShape() != (DisplayServer.CursorShape)shape)
                display.CursorSetShape((DisplayServer.CursorShape)shape);
        }
        catch (Exception error) { CollectException(ref errors, error); }
    }
}
