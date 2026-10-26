using Element = Electron2D.AccessibilityServer.Element;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
namespace Electron2D;

public partial class PopupMenu
{
    private long _accessibilityCollectionVersion;
    internal void AccessibilitySyncCollection(Element owner)
    {
        Measure(); if (!AccessibilityServer.BeginCollection(owner, _accessibilityCollectionVersion, _focused)) return;
        var count = 0; foreach (var item in _items) if (item.Visible && !item.Separator) count++;
        AccessibilityServer.CollectionList(owner, count, true); Element? active = null; var ordinal = 0;
        for (var index = 0; index < _items.Count; index++)
        {
            var item = _items[index]; var role = item.Checkable switch { 1 => Role.MenuItemCheckBox, 2 => Role.MenuItemRadio, _ => Role.MenuItem };
            var e = AccessibilityServer.CollectionElement(owner, item, role, index, geometrySource: _view);
            AccessibilityServer.CollectionIdentity(e, item.DisplayText, !item.Visible, item.Disabled, !item.Separator && !item.Disabled);
            AccessibilityServer.CollectionEntry(e, ordinal, 0, index == _focused && !item.Separator, item.Submenu is { IsDisposed: false } child ? child.Visible : null);
            if (e.SourceChecked != item.Checked) { AccessibilityServer.UpdateSetChecked(e.ID, item.Checked); e.SourceChecked = item.Checked; }
            if (e.SourceTooltip != item.Tooltip) { AccessibilityServer.UpdateSetTooltip(e.ID, item.Tooltip); e.SourceTooltip = item.Tooltip; }
            AccessibilityServer.SourcePopup(e, item.Submenu is { IsDisposed: false } ? AccessibilityServer.AccessibilityPopupType.Menu : null);
            var hadClick = e.Actions.ContainsKey(AccessibilityServer.AccessibilityAction.Click);
            if (item.Separator) e.Actions.Remove(AccessibilityServer.AccessibilityAction.Click); else e.Actions[AccessibilityServer.AccessibilityAction.Click] = e.SourceClickAction!;
            if (hadClick == item.Separator) AccessibilityServer.RuntimeActionsChanged(e);
            if (index == _focused && Eligible(index)) active = e;
            if (item.Visible && !item.Separator) ordinal++;
        }
        AccessibilityServer.EndCollection(owner, _accessibilityCollectionVersion, _focused, active);
    }
    private int AccessibilityCollectionIndex(Element e)
    {
        if (e.CollectionItem is not Item item) return -1; var index = e.CollectionSourceIndex;
        return (uint)index < (uint)_items.Count && _items[index] == item ? index : _items.IndexOf(item);
    }
    internal bool AccessibilityCollectionLive(Element e) => !IsDisposed && IsInsideTree && Visible && AccessibilityCollectionIndex(e) >= 0;
    internal Rect2 AccessibilityCollectionRect(Element e)
    { Measure(); var index = AccessibilityCollectionIndex(e); return index < 0 || !_items[index].Visible ? default : _items[index].Rect; }
    internal bool AccessibilitySelectCollection(Element e, bool selected)
    {
        var index = AccessibilityCollectionIndex(e); if (!AccessibilityCollectionLive(e) || !Eligible(index)) return false;
        if (selected) SetFocusedItem(index); else if (_focused == index) SetFocusedItem(-1); return true;
    }
    internal bool AccessibilitySelectCollection(bool all)
    {
        if (!all) { SetFocusedItem(-1); return true; }
        var first = -1; for (var i = 0; i < _items.Count; i++) if (Eligible(i)) { if (first >= 0) return false; first = i; }
        if (first >= 0) SetFocusedItem(first); return true;
    }
    internal void AccessibilityActivateCollection(Element e)
    {
        var index = AccessibilityCollectionIndex(e); if (!AccessibilityCollectionLive(e) || !Eligible(index)) return;
        if (_items[index].Submenu is not null) OpenSubmenu(index, true); else Activate(index);
    }
    private RID AccessibilityFocusedItem()
    {
        var owner = AccessibilityServer.SourceElement(this);
        return (uint)_focused < (uint)_items.Count && owner?.SourceCollectionElements is { } records && records.TryGetValue(_items[_focused], out var e) && !e.Retired ? e.ID : _view.GetAccessibilityElement();
    }
}
