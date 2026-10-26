using Element = Electron2D.AccessibilityServer.Element;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
namespace Electron2D;

public partial class ItemList
{
    internal void AccessibilitySyncCollection(Element owner)
    {
        RebuildListLayout(); if (!AccessibilityServer.BeginCollection(owner, _layoutGeneration, _inputRevision)) return;
        AccessibilityServer.CollectionList(owner, _items.Count, true); Element? active = null;
        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i]; var e = AccessibilityServer.CollectionElement(owner, item, Role.ListBoxOption, i);
            var name = item.AutoTranslate switch { NodeAutoTranslateMode.Always => Tr(item.Text), NodeAutoTranslateMode.Disabled => item.Text, _ => Atr(item.Text) };
            AccessibilityServer.CollectionIdentity(e, name, false, item.Disabled, item.Selectable && !item.Disabled);
            AccessibilityServer.CollectionEntry(e, i, 0, item.Selected, null);
            if (_current == i && item.Selectable && !item.Disabled) active = e;
        }
        AccessibilityServer.EndCollection(owner, _layoutGeneration, _inputRevision, active);
    }
    private int AccessibilityCollectionIndex(Element e)
    {
        if (e.CollectionItem is not Item item) return -1; var index = e.CollectionSourceIndex;
        return (uint)index < (uint)_items.Count && _items[index] == item ? index : _items.IndexOf(item);
    }
    internal bool AccessibilityCollectionLive(Element e) => AccessibilityCollectionIndex(e) >= 0;
    internal Rect2 AccessibilityCollectionRect(Element e)
    {
        var index = AccessibilityCollectionIndex(e); if (index < 0) return default;
        var rect = GetItemRect(index); var rtl = IsLayoutRTL();
        rect.Position += new Vector2(rtl ? (float)_hBar.Value : -(float)_hBar.Value, -(float)_vBar.Value);
        if (rtl) { var panel = GetThemeStyleBox("panel")!; rect.Position = new(Size.X - rect.Position.X - rect.Size.X + panel.GetMargin(Side.Left) - panel.GetMargin(Side.Right), rect.Position.Y); }
        return rect;
    }
    internal bool AccessibilitySelectCollection(Element e, bool selected)
    {
        if (IsDisposed || !IsInsideTree) return false; var index = AccessibilityCollectionIndex(e);
        if (index < 0 || _items[index].Disabled || selected && !_items[index].Selectable) return false;
        if (selected) Select(index, _selectMode == SelectMode.Single); else Deselect(index);
        return true;
    }
    internal bool AccessibilitySelectCollection(bool all)
    {
        if (all)
        {
            if (_selectMode == SelectMode.Single && _items.Count(item => item.Selectable && !item.Disabled) > 1) return false;
            var items = _items.ToArray();
            foreach (var item in items)
            {
                if (IsDisposed || !IsInsideTree) break; var index = _items.IndexOf(item); if (index < 0 || !CanSelect(index) || item.Selected) continue;
                Select(index, _selectMode == SelectMode.Single);
            }
        }
        else
        {
            DeselectAll();
        }
        return true;
    }
    internal bool AccessibilityFocusCollection(Element e)
    {
        if (IsDisposed || !IsInsideTree) return false; var index = AccessibilityCollectionIndex(e); if (index < 0 || !CanSelect(index)) return false;
        Select(index); return true;
    }
    internal void AccessibilityActivateCollection(Element e)
    {
        if (IsDisposed || !IsInsideTree) return; var index = AccessibilityCollectionIndex(e); if (index < 0 || _items[index].Disabled) return;
        if (_items[index].Selectable) Select(index);
        if (IsDisposed || !IsInsideTree) return; index = AccessibilityCollectionIndex(e);
        if (index >= 0 && !_items[index].Disabled) BeginEventEmission(_publisherItemActivated.Handlers)?.Invoke(index);
    }
}
