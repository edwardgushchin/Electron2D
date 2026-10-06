namespace Electron2D;

public partial class TabContainer
{
    /// <summary>Binds a borrowed Popup, clearing the binding for null or a non-popup node.</summary><param name="popup">A live node; a popup must be parented by the consumer before opening.</param>
    public void SetPopup(Node? popup)
    {
        EnsureMutable(); if (popup?.IsDisposed == true) throw new ObjectDisposedException(nameof(popup)); var current = GetPopup(); var next = popup as Popup; if (current == next) return;
        if (current is not null) current.Disposed -= PopupDisposed; _popup = next is null ? null : new(next); if (next is not null) next.Disposed += PopupDisposed;
        _popupButton.Visible = _tabsVisible && next is not null; PopupHover(false); LayoutChanged();
    }
    /// <summary>Gets the live borrowed popup binding.</summary><returns>The popup, or null after disposal/collection.</returns>
    public Popup? GetPopup() { CheckTabs(); return _popup is not null && _popup.TryGetTarget(out var popup) && !popup.IsDisposed ? popup : null; }
    private void PopupDisposed(ElectronObject _)
    { if (Tree is { IsOwnerThread: false } tree) { if (!tree.IsClosing) tree.Defer(() => PopupDisposedOnOwner()); return; } PopupDisposedOnOwner(); }
    private void PopupDisposedOnOwner() { if (IsDisposed || _disposing) return; _popup = null; _popupButton.Hide(); LayoutChanged(); }
    private void PopupHover(bool hover) { if (_popupButton is null || _popupButton.IsDisposed) return; _popupButton.Icon = GetThemeIcon(hover ? "menu_highlight" : "menu"); }
    private void OpenPopup()
    {
        var popup = GetPopup(); if (popup is null) return; PrePopupPressed?.Invoke(); if (IsDisposed || popup.IsDisposed) return;
        var at = _popupButton.GetGlobalRect(); var position = at.Position;
        if (GetViewport() is Window { Embedder: not null } window) position += (Vector2)window.Position;
        position.X += IsLayoutRTL() ? 0 : at.Size.X - popup.Size.X; position.Y += _position == TabPosition.Bottom ? -popup.Size.Y : at.Size.Y;
        popup.Position = (Vector2i)position; popup.Popup();
    }
    private sealed class PageDrag(TabContainer source, Control control, DragPayload stripPayload) : DragPayload
    { internal readonly TabContainer Source = source; internal readonly Control Control = control; internal readonly DragPayload StripPayload = stripPayload; }
    private sealed class ContainerTabBar(TabContainer owner) : TabBar
    {
        protected override DragPayload? OnGetDragData(Vector2 point)
        { var index = GetTabIdxAtPoint(point); var page = owner.GetTabControl(index); if (page is null) return null; var payload = base.OnGetDragData(point); return payload is null ? null : new PageDrag(owner, page, payload); }
        protected override bool OnCanDropData(Vector2 point, DragPayload payload)
        { if (payload is not PageDrag page) return base.OnCanDropData(point, payload); return !page.Source.IsDisposed && !page.Control.IsDisposed && page.Control.Parent == page.Source && !page.Control.TopLevel && page.Source.FindPage(page.Control) >= 0 && base.OnCanDropData(point, page.StripPayload); }
        protected override void OnDropData(Vector2 point, DragPayload payload)
        {
            if (payload is not PageDrag data || !OnCanDropData(point, payload)) return;
            var sourceIndex = data.Source.FindPage(data.Control); var destination = DropInsertionIndex(point); if (data.Source == owner && destination > sourceIndex) destination--;
            owner.MovePage(data.Source, data.Control, Math.Clamp(destination, 0, data.Source == owner ? owner._pages.Count - 1 : owner._pages.Count));
        }
    }
    private void MovePage(TabContainer source, Control control, int destination)
    {
        EnsureMutable(); source.EnsureMutable(); var from = source.FindPage(control); if (from < 0) return; List<Exception>? errors = null;
        if (source == this)
        {
            if (from == destination) return; var target = _pages[destination].Control; try { MoveChild(control, target.GetIndex()); } catch (Exception e) { CollectException(ref errors, e); }
            var index = FindPage(control); if (index >= 0 && !IsDisposed) { try { ActiveTabRearranged?.Invoke(index); } catch (Exception e) { CollectException(ref errors, e); } if (control.Parent == this && !control.IsDisposed) try { _bar.CurrentTab = FindPage(control); } catch (Exception e) { CollectException(ref errors, e); } }
        }
        else
        {
            if (_pages.Count == 65536) throw new InvalidOperationException("Tab capacity exceeded."); var record = source._bar.BorrowTabRecord(from);
            try { source.RemoveChild(control); } catch (Exception e) { CollectException(ref errors, e); }
            if (!IsDisposed && !control.IsDisposed && control.Parent is null)
            {
                try { AddChild(control); } catch (Exception e) { CollectException(ref errors, e); }
                var index = FindPage(control); if (index >= 0)
                {
                    try { _bar.ReplaceTabRecord(index, record); _pages[index].Title = record.Title == control.Name ? null : record.Title; if (destination < index) MoveChild(control, _pages[destination].Control.GetIndex()); Paint(); LayoutChanged(); } catch (Exception e) { CollectException(ref errors, e); }
                    index = FindPage(control); if (index >= 0 && !record.Disabled) try { _bar.CurrentTab = index; } catch (Exception e) { CollectException(ref errors, e); }
                }
            }
        }
        ThrowCollected("Tab page transfer callbacks failed.", errors);
    }
}
