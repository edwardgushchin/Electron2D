namespace Electron2D;

public sealed partial class SceneTree
{
    private readonly Dictionary<Viewport, ViewportGUIState> _guiStates = [];
    private ViewportGUIState _gui = new(null);
    private ViewportGUIState GUIState(Viewport viewport)
    {
        if (!_guiStates.TryGetValue(viewport, out var state)) { state = new(viewport); state.Section = state; _guiStates.Add(viewport, state); }
        var section = viewport;
        while (section.Parent is SubViewportContainer container && container.GetViewport() is { } parent) section = parent;
        state.Section = ReferenceEquals(section, viewport) ? state : GUIState(section);
        return state;
    }
    private GUIScope SelectGUI(Viewport? viewport)
    {
        var previous = _gui;
        if (viewport is not null) _gui = GUIState(viewport);
        return new(this, previous);
    }
    private readonly struct GUIScope(SceneTree tree, ViewportGUIState previous) : IDisposable
    {
        public void Dispose() => tree._gui = previous;
    }
    internal void RegisterGUIViewport(Viewport viewport) => _ = GUIState(viewport);
    internal void ReleaseGUIViewport(Viewport viewport)
    {
        using var scope = SelectGUI(viewport);
        List<Exception>? errors = null;
        try { ClearGUIHover(); } catch (Exception error) { CollectException(ref errors, error); }
        try { ClearPhysicsPicking(viewport); } catch (Exception error) { CollectException(ref errors, error); }
        try { ReleaseGUIFocus(viewport); } catch (Exception error) { CollectException(ref errors, error); }
        if (ReferenceEquals(_gui.Section, _gui))
            try { CancelGUIDrag(viewport); } catch (Exception error) { CollectException(ref errors, error); }
        _gui.InputTraversal.Clear(); _gui.GuiTouchCapture.Clear(); _gui.GuiTouchSlots.Clear();
        ReleaseEmbeddedHost(viewport);
        _guiStates.Remove(viewport);
        ThrowCollected("Viewport GUI cleanup callbacks failed.", errors);
    }
    private readonly List<ViewportGUIState> _guiTooltipStates = [];
    private void ProcessGUITooltips(double delta, ref List<Exception>? errors)
    {
        _guiTooltipStates.AddRange(_guiStates.Values);
        try
        {
            foreach (var state in _guiTooltipStates)
                if (state.Viewport is { IsDisposed: false } viewport && ReferenceEquals(viewport.Tree, this))
                { using var scope = SelectGUI(viewport); ProcessTooltip(delta, ref errors); }
        }
        finally { _guiTooltipStates.Clear(); }
    }
    internal PanelContainer? ViewportTooltipPanel(Viewport viewport) { using var scope = SelectGUI(viewport); return _gui.TooltipPanel; }
    private readonly List<ViewportGUIState> _guiRefreshStates = [];
    private bool _refreshingGUI, _refreshGUIAgain;
    private void RefreshAllGUI(bool hover)
    {
        if (_refreshingGUI) { _refreshGUIAgain = true; return; }
        _refreshingGUI = true;
        List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 8; pass++)
            {
                _refreshGUIAgain = false; _guiRefreshStates.AddRange(_guiStates.Values);
                foreach (var state in _guiRefreshStates)
                {
                    if (state.Viewport is not { IsDisposed: false } viewport || !ReferenceEquals(viewport.Tree, this)) continue;
                    using var scope = SelectGUI(viewport);
                    try
                    {
                        if (hover && state.GuiHoverKnown) RefreshGUIHoverAt(viewport, state.GuiHoverPosition);
                        else if (!hover && state.GuiFocus is { } focused && !focused.CanReceiveGUIFocus) ReleaseGUIFocus(focused);
                    }
                    catch (Exception error) { CollectException(ref errors, error); }
                }
                _guiRefreshStates.Clear(); if (!_refreshGUIAgain) break;
            }
        }
        finally { _guiRefreshStates.Clear(); _refreshingGUI = false; }
        ThrowCollected("Viewport GUI refresh callbacks failed.", errors);
    }
    internal void ExitGUIViewport(Viewport viewport)
    {
        using var scope = SelectGUI(viewport); List<Exception>? errors = null;
        try { ClearGUIHover(); } catch (Exception error) { CollectException(ref errors, error); }
        try { ClearPhysicsPicking(viewport); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Viewport pointer-exit callbacks failed.", errors);
    }
    internal void DisableGUIViewport(Viewport viewport)
    {
        using var scope = SelectGUI(viewport); List<Exception>? errors = null;
        try { ExitGUIViewport(viewport); } catch (Exception error) { CollectException(ref errors, error); }
        try { viewport.DispatchNotification(Node.NotificationVPMouseExit); } catch (Exception error) { CollectException(ref errors, error); }
        _gui.GuiMouseCapture = null; _gui.GuiMouseCaptureMask = 0;
        ThrowCollected("Viewport input-disable callbacks failed.", errors);
    }
    internal Control? GetGUIHoveredControl(Viewport viewport) { EnsureOwnerThread(); using var scope = SelectGUI(viewport); return _gui.GuiHoverTarget; }
    private void UpdateEmbeddedHover(SubViewportContainer container, Vector2 parentPoint, ref List<Exception>? errors)
    {
        Vector2 local;
        try { local = container.ViewportPoint(container.MakeCanvasPositionLocal(parentPoint)); }
        catch (Exception error) { CollectException(ref errors, error); return; }
        for (var i = 0; i < container.GetChildCount(); i++)
            if (container.GetChild(i) is SubViewport { GUIDisableInput: false } viewport && ReferenceEquals(viewport.Tree, this))
            {
                using var scope = SelectGUI(viewport);
                try
                {
                    if (!_gui.GuiHoverKnown) viewport.NotifyMouseEntered();
                    var point = viewport.GetFinalTransform().AffineInverse() * local;
                    CaptureInputNodes(); UpdateGUIHover(viewport, point, ref errors);
                }
                catch (Exception error) { CollectException(ref errors, error); }
                finally { _gui.InputTraversal.Clear(); }
            }
    }
    private Control? MouseTargetControl(Viewport viewport) => GUIState(viewport).Section.MouseTarget;
    private Vector2 PointInViewport(Viewport source, Viewport destination, Vector2 point)
    {
        if (ReferenceEquals(source, destination)) return point;
        return destination.GetScreenTransform().AffineInverse() * (source.GetScreenTransform() * point);
    }
    private ViewportGUIState InputGUI(Viewport? viewport)
    {
        if (viewport is null) return _gui;
        if (!viewport.HandleInputLocally)
            while (viewport is not Window && viewport.Parent?.GetViewport() is { } parent) viewport = parent;
        return GUIState(viewport);
    }
    internal void SetViewportInputAsHandled(Viewport viewport) { EnsureOwnerThread(); if (!_isDispatchingInput) throw new InvalidOperationException("No input is being dispatched."); InputGUI(viewport).InputHandled = true; }
    internal bool IsViewportInputHandled(Viewport viewport) { EnsureOwnerThread(); if (!_isDispatchingInput) throw new InvalidOperationException("No input is being dispatched."); return InputGUI(viewport).InputHandled; }
    internal Viewport GUISectionViewport(Viewport viewport) => GUIState(viewport).Section.Viewport ?? viewport;
}
