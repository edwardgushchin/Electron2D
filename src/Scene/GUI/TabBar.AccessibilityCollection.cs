using Element = Electron2D.AccessibilityServer.Element;
using Role = Electron2D.AccessibilityServer.AccessibilityRole;
namespace Electron2D;

public partial class TabBar
{
    internal void AccessibilitySyncCollection(Element owner)
    {
        RebuildTabs(); var version = unchecked((long)_accessibilityScrollVersion);
        if (!AccessibilityServer.BeginCollection(owner, version, _selectionRevision)) return;
        AccessibilityServer.CollectionList(owner, _tabs.Count, false); Element? active = null;
        for (var i = 0; i < _tabs.Count; i++)
        {
            var tab = _tabs[i]; var e = AccessibilityServer.CollectionElement(owner, tab, Role.Tab, i);
            AccessibilityServer.CollectionIdentity(e, Atr(tab.Title), tab.Hidden, tab.Disabled, !tab.Hidden && !tab.Disabled);
            AccessibilityServer.CollectionEntry(e, i, 0, _current == i, null);
            if (_current == i && !tab.Hidden && !tab.Disabled) active = e;
        }
        AccessibilityServer.EndCollection(owner, version, _selectionRevision, active);
    }
    private int AccessibilityCollectionIndex(Element e)
    {
        if (e.CollectionItem is not Tab tab) return -1; var index = e.CollectionSourceIndex;
        return (uint)index < (uint)_tabs.Count && _tabs[index] == tab ? index : _tabs.IndexOf(tab);
    }
    internal bool AccessibilityCollectionLive(Element e) => AccessibilityCollectionIndex(e) >= 0;
    internal void AccessibilityRevealCollection(Element e)
    {
        var index = AccessibilityCollectionIndex(e); if (index >= 0 && !_tabs[index].Hidden && !_tabs[index].Rect.HasArea()) EnsureTabVisible(index);
    }
    internal bool AccessibilityCollectionDrawn(Element e) { var index = AccessibilityCollectionIndex(e); return index >= 0 && _tabs[index].Rect.HasArea(); }
    internal Rect2 AccessibilityCollectionRect(Element e)
    {
        RebuildTabs(); var index = AccessibilityCollectionIndex(e); return index < 0 || _tabs[index].Hidden ? default : _tabs[index].AccessibilityRect;
    }
    internal bool AccessibilitySelectCollection(Element e, bool selected)
    {
        if (IsDisposed || !IsInsideTree) return false; var index = AccessibilityCollectionIndex(e);
        if (index < 0 || _tabs[index].Hidden || _tabs[index].Disabled) return false;
        if (selected) CurrentTab = index;
        else if (_current == index) { if (!_deselectEnabled) return false; CurrentTab = -1; }
        return true;
    }
    internal bool AccessibilitySelectCollection(bool all)
    {
        if (all)
        {
            var index = -1; for (var i = 0; i < _tabs.Count; i++) if (!_tabs[i].Hidden && !_tabs[i].Disabled) { if (index >= 0) return false; index = i; }
            if (index >= 0 && _current != index) CurrentTab = index; return true;
        }
        if (_current < 0) return true; if (!_deselectEnabled) return false; CurrentTab = -1; return true;
    }
}
