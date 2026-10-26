namespace Electron2D;

public sealed partial class SceneTree
{
    internal ulong AccessibilityDragRevision { get; private set; }
    internal void InvalidateAccessibilityDrag() => AccessibilityDragRevision++;
    internal void BeginAccessibilityDrag(Control source)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(source.GetViewport());
        if (_gui.Section.GuiDragPreparing || _gui.Section.GuiDragPayload is not null || _gui.Section.GuiDragCompleting) throw new InvalidOperationException("A GUI drag is already active.");
        _gui.Section.GuiDragPreparing = true; _gui.Section.GuiDragSource = source;
        List<Exception>? errors = null;
        try
        {
            var payload = source.GetAccessibilityDragData();
            if (payload is not null && !source.IsDisposed && !source.IsDepartingTree && !source.IsQueuedForDeletion && ReferenceEquals(source.Tree, this) && ReferenceEquals(GUIState(source.GetViewport()!).Section, _gui.Section))
            {
                _gui.Section.GuiDragPreparing = false;
                ForceGUIDrag(source, payload, null);
                if (_gui.Section.GuiDragPayload == payload)
                {
                    var description = (_gui.Section.Viewport ?? source.GetViewport()!).GetGUIDragDescription();
                    AccessibilityServer.Announce(source, string.Format(source.Tr("{0} grabbed. Select target and use {1} to drop, use {2} to cancel."), description, source.Tr("Keyboard drag and drop"), source.Tr("Cancel")));
                    source.QueueAccessibilityUpdate();
                }
            }
        }
        catch (Exception error) { CollectException(ref errors, error); }
        if (_gui.Section.GuiDragPayload is null)
        {
            _gui.Section.GuiDragPreparing = false; _gui.Section.GuiDragSource = null;
            try { ClearGUIDragPreview(); } catch (Exception error) { CollectException(ref errors, error); }
            _gui.Section.Viewport?.ClearGUIDragDescription();
        }
        ThrowCollected("Accessible GUI drag callbacks failed.", errors);
    }
    internal void CompleteAccessibilityDrag(Control target)
    {
        EnsureOwnerThread(); using var scope = SelectGUI(target.GetViewport());
        if (_gui.Section.GuiDragPayload is null || _gui.Section.GuiDragCompleting) throw new InvalidOperationException("No GUI drag can be dropped in this viewport section.");
        List<Exception>? errors = null;
        CompleteGUIDrag(target.GetViewport()!, _gui.Section.GuiDragPointer, true, ref errors, target);
        if (!target.IsDisposed && ReferenceEquals(target.Tree, this)) target.QueueAccessibilityUpdate();
        ThrowCollected("Accessible GUI drop callbacks failed.", errors);
    }
    internal string? GetAccessibilityDragDescription(Control target, out DragPayload? payload)
    {
        using var scope = SelectGUI(target.GetViewport()); payload = _gui.Section.GuiDragPayload;
        return payload is null ? null : (_gui.Section.Viewport ?? target.GetViewport()!).GetGUIDragDescription();
    }
    internal bool IsAccessibilityDragPayload(Control target, DragPayload payload)
    { using var scope = SelectGUI(target.GetViewport()); return ReferenceEquals(_gui.Section.GuiDragPayload, payload); }
}
