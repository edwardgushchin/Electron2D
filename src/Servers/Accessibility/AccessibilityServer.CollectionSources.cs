namespace Electron2D;

public sealed partial class AccessibilityServer
{
    private readonly Dictionary<object, Element> _collectionSources = [];
    private readonly List<Element> _collectionRetirements = [];
    internal static bool BeginCollection(Element owner, long version, long stateVersion = 0)
    {
        if (owner.SourceCollectionVersion == version && owner.SourceCollectionStateVersion == stateVersion) return false;
        owner.SourceCollectionElements ??= []; owner.SourceCollectionExpected ??= []; owner.SourceCollectionRemoved ??= []; owner.SourceCollectionOrder ??= [];
        owner.SourceCollectionExpected.Clear(); owner.SourceCollectionOrder.Clear(); return true;
    }
    internal static Element CollectionElement(Element owner, object key, AccessibilityRole role, int index, int column = -1, Control? geometrySource = null)
    {
        var r = Runtime;
        if (r._collectionSources.TryGetValue(key, out var previous) && previous.WindowID != owner.WindowID) r.Free(previous);
        if (!r._collectionSources.TryGetValue(key, out var e) || e.Retired)
        {
            e = r.Add(owner.WindowID, role); e.OwnedSubelement = true; e.CollectionItem = key;
            r._collectionSources[key] = e;
            BindCollectionActions(e, owner.Source!, role);
        }
        e.CollectionSource = geometrySource ?? (Control)owner.Source!; e.CollectionOwner = owner; e.CollectionSourceIndex = index; e.CollectionSourceColumn = column;
        owner.SourceCollectionElements![key] = e; owner.SourceCollectionExpected!.Add(key); owner.SourceCollectionOrder!.Add(e);
        if (e.Parent != owner) r.Attach(owner, e, -1);
        if (e.Role != role) UpdateSetRole(e.ID, role);
        return e;
    }
    private static void BindCollectionActions(Element e, Node source, AccessibilityRole role)
    {
        e.SourceFocusAction = () => FocusCollectionItem(e);
        if (source is ItemList or PopupMenu || source is Tree && role != AccessibilityRole.ColumnHeader)
        { e.SourceBlurAction = () => SelectCollectionItem(e, false); e.Actions[AccessibilityAction.Blur] = e.SourceBlurAction; }
        if (source is ItemList or TabBar or PopupMenu) { e.SourceClickAction = () => ActivateCollectionItem(e); e.Actions[AccessibilityAction.Click] = e.SourceClickAction; }
    }
    internal static void CollectionIdentity(Element e, string name, bool hidden, bool disabled, bool selectable)
    {
        if (!e.SourcePrepared || name != e.SourceName) UpdateSetName(e.ID, name);
        var flags = (hidden ? 1UL << (int)AccessibilityFlags.Hidden : 0) | (disabled ? 1UL << (int)AccessibilityFlags.Disabled : 0);
        var changed = e.SourcePrepared ? e.SourceFlags ^ flags : (1UL << 10) - 1;
        for (var flag = 0; flag < 10; flag++) if ((changed & 1UL << flag) != 0) UpdateSetFlag(e.ID, (AccessibilityFlags)flag, (flags & 1UL << flag) != 0);
        if (e.CollectionSelectable != selectable) { e.CollectionSelectable = selectable; Runtime._native!.StateChanged(e, "selectable", selectable); }
        if (!e.SourcePrepared || e.SourceCanFocus != selectable)
        {
            if (selectable) e.Actions[AccessibilityAction.Focus] = e.SourceFocusAction!; else e.Actions.Remove(AccessibilityAction.Focus);
            e.SourceCanFocus = selectable; Runtime._native!.ActionsChanged(e);
        }
        e.SourceName = name; e.SourceFlags = flags; e.SourcePrepared = true;
    }
    private static void SourceCollectionNumber(Element e, int value, CollectionNumber field)
    {
        var source = e.SourceCollectionSnapshot ??= new(); ref var previous = ref CollectionNumberField(source, field);
        if (previous == value) return; SetCollectionNumber(e.ID, value, field); previous = value;
    }
    private static void SourceCollectionFlag(Element e, bool? value, CollectionFlag field)
    {
        var source = e.SourceCollectionSnapshot ??= new();
        var previous = field == CollectionFlag.Selected ? source.Selected : field == CollectionFlag.Expanded ? source.Expanded : source.Vertical;
        if (previous == value) return;
        if (value is { } known) SetCollectionFlag(e.ID, known, field);
        else
        {
            var c = e.Collection ??= new();
            if (field == CollectionFlag.Selected) c.Selected = null; else if (field == CollectionFlag.Expanded) c.Expanded = null; else c.Vertical = null;
            Runtime._native!.CollectionChanged(e);
        }
        if (field == CollectionFlag.Selected) source.Selected = value; else if (field == CollectionFlag.Expanded) source.Expanded = value; else source.Vertical = value;
    }
    internal static void CollectionList(Element owner, int count, bool vertical)
    { SourceCollectionNumber(owner, count, CollectionNumber.Count); SourceCollectionFlag(owner, vertical, CollectionFlag.Vertical); }
    internal static void CollectionTable(Element owner, int rows, int columns)
    { SourceCollectionNumber(owner, rows, CollectionNumber.Rows); SourceCollectionNumber(owner, columns, CollectionNumber.Columns); }
    internal static void CollectionEntry(Element e, int index, int level, bool selected, bool? expanded, int row = -1, int column = -1, int columnSpan = 1)
    {
        SourceCollectionNumber(e, index, CollectionNumber.Index); SourceCollectionNumber(e, level, CollectionNumber.Level);
        SourceCollectionFlag(e, selected, CollectionFlag.Selected); SourceCollectionFlag(e, expanded, CollectionFlag.Expanded);
        var source = e.SourceCollectionSnapshot!; int? r = row < 0 ? null : row, col = column < 0 ? null : column;
        if (source.CellRow != r || source.CellColumn != col)
        {
            if (r is { } known && col is { } knownColumn) UpdateSetTableCellPosition(e.ID, known, knownColumn);
            else { var c = e.Collection!; c.CellRow = c.CellColumn = null; Runtime._native!.CollectionChanged(e); }
            source.CellRow = r; source.CellColumn = col;
        }
        if (column >= 0 && (!source.HasSpan || source.ColumnSpan != columnSpan)) { UpdateSetTableCellSpan(e.ID, 1, columnSpan); source.HasSpan = true; source.ColumnSpan = columnSpan; }
    }
    internal static void CollectionColumnHeader(Element e, int index) => SourceCollectionNumber(e, index, CollectionNumber.Column);
    internal static void CollectionBranch(Element e, TreeItem item)
    {
        var actionable = item.Children.Count != 0 && !item.DisableFolding;
        if (actionable && !e.Actions.ContainsKey(AccessibilityAction.Expand))
        {
            BindCollectionBranch(e, item);
            Runtime._native!.ActionsChanged(e);
        }
        else if (!actionable && e.Actions.Remove(AccessibilityAction.Expand)) { e.Actions.Remove(AccessibilityAction.Collapse); Runtime._native!.ActionsChanged(e); }
    }
    private static void BindCollectionBranch(Element e, TreeItem item)
    {
        e.Actions[AccessibilityAction.Expand] = () => { if (!item.IsDisposed && item.Owner == e.CollectionSource) item.Collapsed = false; };
        e.Actions[AccessibilityAction.Collapse] = () => { if (!item.IsDisposed && item.Owner == e.CollectionSource) item.Collapsed = true; };
    }
    internal static void CollectionHierarchy(Element e, Element? parent)
    { if (e.CollectionParent == parent) return; var old = e.CollectionParent; e.CollectionParent = parent; Runtime._native!.RelationsChanged(e); if (old is { Retired: false }) Runtime._native.RelationsChanged(old); if (parent is not null) Runtime._native.RelationsChanged(parent); }
    internal static void EndCollection(Element owner, long version, long stateVersion, Element? active)
    {
        var r = Runtime; var removed = owner.SourceCollectionRemoved!; removed.Clear();
        foreach (var pair in owner.SourceCollectionElements!) if (!owner.SourceCollectionExpected!.Contains(pair.Key)) removed.Add(pair.Key);
        foreach (var key in removed) { var e = owner.SourceCollectionElements[key]; owner.SourceCollectionElements.Remove(key); if (e.CollectionOwner == owner && !e.Retired && !e.CollectionRetirementQueued) { e.CollectionRetirementQueued = true; r._collectionRetirements.Add(e); } }
        var nonSource = 0; foreach (var child in owner.Children) if (child.CollectionOwner != owner) nonSource++;
        var ordinal = nonSource;
        foreach (var child in owner.SourceCollectionOrder!)
        {
            if (child.Retired) continue;
            if (child.Parent != owner || (uint)ordinal >= (uint)owner.Children.Count || owner.Children[ordinal] != child) r.Attach(owner, child, ordinal);
            ordinal++;
        }
        if (owner.SourceCollectionActive != active) { UpdateSetActiveDescendant(owner.ID, active?.ID ?? default); owner.SourceCollectionActive = active; }
        owner.SourceCollectionVersion = version; owner.SourceCollectionStateVersion = stateVersion;
    }
    internal static void FinishSourceCollections()
    {
        var r = Runtime;
        foreach (var e in r._collectionRetirements)
        {
            e.CollectionRetirementQueued = false; if (e.Retired) continue;
            var owner = e.CollectionOwner;
            if (e.CollectionItem is { } key && owner?.SourceCollectionElements is { } items && items.TryGetValue(key, out var current) && current == e) continue;
            r.Free(e);
        }
        r._collectionRetirements.Clear();
    }
    internal static void UpdateSourceCollection(Element e, Control c)
    {
        switch (c) { case ItemList list: list.AccessibilitySyncCollection(e); break; case Tree tree: tree.AccessibilitySyncCollection(e); break; case TabBar tabs: tabs.AccessibilitySyncCollection(e); break; }
    }
    internal static bool CollectionItemLive(Element e) => e.CollectionSource switch
    {
        null => true,
        { IsDisposed: true } or { IsInsideTree: false } or { IsDepartingTree: true } or { IsQueuedForDeletion: true } or { IsVisibleInTree: false } => false,
        ItemList list => list.AccessibilityCollectionLive(e),
        Tree tree => tree.AccessibilityCollectionLive(e),
        TabBar tabs => tabs.AccessibilityCollectionLive(e),
        PopupMenu.MenuItems items => items.MenuOwner.AccessibilityCollectionLive(e),
        _ => false,
    };
    internal static Rect2 CollectionBounds(Element e)
    {
        var source = e.CollectionSource!; if (source.IsDisposed || !source.IsInsideTree) return default;
        var local = source switch { ItemList list => list.AccessibilityCollectionRect(e), Tree tree => tree.AccessibilityCollectionRect(e), TabBar tabs => tabs.AccessibilityCollectionRect(e), PopupMenu.MenuItems items => items.MenuOwner.AccessibilityCollectionRect(e), _ => default };
        return (source.GetViewport()!.GetScreenTransform() * source.GetGlobalTransformWithCanvas()) * local;
    }
    internal static bool SelectCollectionItem(Element e, bool selected) => e.CollectionSource switch
    { ItemList list => list.AccessibilitySelectCollection(e, selected), Tree tree => tree.AccessibilitySelectCollection(e, selected), TabBar tabs => tabs.AccessibilitySelectCollection(e, selected), PopupMenu.MenuItems items => items.MenuOwner.AccessibilitySelectCollection(e, selected), _ => false };
    internal static bool SelectCollection(Element owner, bool all) => owner.Source switch
    { ItemList list => list.AccessibilitySelectCollection(all), Tree tree => tree.AccessibilitySelectCollection(all), TabBar tabs => tabs.AccessibilitySelectCollection(all), PopupMenu menu => menu.AccessibilitySelectCollection(all), _ => false };
    internal static bool SelectCollectionAxis(Element owner, int index, bool row, bool selected) => owner.Source is Tree tree && tree.AccessibilitySelectCollectionAxis(index, row, selected);
    private static void FocusCollectionItem(Element e)
    {
        var selected = e.CollectionSource is ItemList list ? list.AccessibilityFocusCollection(e) : SelectCollectionItem(e, true);
        if (selected && e.CollectionSource is { IsDisposed: false, IsInsideTree: true } control) control.GrabFocus();
    }
    private static void ActivateCollectionItem(Element e)
    {
        if (e.CollectionSource is ItemList list) list.AccessibilityActivateCollection(e);
        else if (e.CollectionSource is TabBar tabs) tabs.AccessibilitySelectCollection(e, true);
        else if (e.CollectionSource is PopupMenu.MenuItems items) items.MenuOwner.AccessibilityActivateCollection(e);
    }
    private void ClearCollectionSource(Element e)
    {
        if (e.CollectionItem is { } key && _collectionSources.TryGetValue(key, out var current) && current == e) _collectionSources.Remove(key);
        e.SourceCollectionElements?.Clear(); e.SourceCollectionExpected?.Clear(); e.SourceCollectionRemoved?.Clear(); e.SourceCollectionOrder?.Clear();
        e.CollectionSource = null; e.CollectionOwner = null; e.CollectionItem = null; e.CollectionTreeItem = null; e.CollectionParent = e.SourceCollectionActive = null;
    }
}
