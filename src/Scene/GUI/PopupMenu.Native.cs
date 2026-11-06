namespace Electron2D;

public partial class PopupMenu
{
    private bool _preferNativeMenu, _nativeMenu;
    private RID _nativeMenuRID;
    /// <summary>Gets or sets whether eligible popup trees prefer the operating-system menu.</summary>
    /// <value>False initially; stored in scenes.</value>
    /// <remarks>The Windows adapter supports plain, check/radio, icon, separator and submenu items.
    /// Search controls, shortcuts, nonclosing selection policies and custom item presentation use the canvas path.</remarks>
    public bool PreferNativeMenu { get { CheckMenu(); return _preferNativeMenu; } set { EnsureMutable(); _preferNativeMenu = value; } }
    /// <summary>Reports whether this popup currently participates in an operating-system menu presentation.</summary>
    /// <returns>True during the modal native call, including its close notification.</returns>
    public bool IsNativeMenu() { CheckMenu(); return _nativeMenu; }
    internal override bool TryNativeVisibility(bool visible)
    {
        if (!_nativeMenu) return false;
        if (!visible) NativeMenu.CancelPopup(_nativeMenuRID);
        return true;
    }
    private bool CanUseNativeMenu(HashSet<PopupMenu> visited)
    {
        if (!visited.Add(this) || Visible || _searchEnabled || GraphScaleFromOwner() != 1) return false;
        foreach (var item in _items)
        {
            if (item.Text.Contains('\0') || item.Separator && item.Text.Length != 0 || !item.Separator && !HidePolicy(item) || item.Shortcut is not null || item.Accelerator != Key.None || item.Indent != 0 || item.Tooltip.Length != 0 || item.IconMaxWidth != 0 || item.IconModulate != Colors.White || item.Language.Length != 0 || item.Direction != TextDirection.Auto) return false;
            if (item.Submenu is { } child && (child.IsDisposed || !child.CanUseNativeMenu(visited))) return false;
        }
        foreach (var child in GetChildren()) if (child is not PopupMenu submenu || !_items.Any(item => item.Submenu == submenu)) return false;
        return true;
    }
    internal override bool TryNativePopup(Rect2i rect, bool nativeRelative)
    {
        if (!_preferNativeMenu || !NativeMenu.IsAvailable || !NativeMenu.HasFeature(NativeMenu.Feature.PopupMenu) || !CanUseNativeMenu([])) return false;
        if (rect != default && (rect.Size.X <= 0 || rect.Size.Y <= 0)) throw new ArgumentOutOfRangeException(nameof(rect), "Popup dimensions must be positive.");
        var position = NativeMenuPosition(rect, nativeRelative);
        var owner = NativeMenu.BorrowOwner();
        var menus = new List<(PopupMenu Source, RID RID)>(); List<Exception>? errors = null;
        try { var root = BuildNativeMenu(menus); NativeMenu.PopupForDisplay(root, position, NativeMenuParent()); }
        catch (Exception error) { CollectException(ref errors, error); }
        finally
        {
            foreach (var pair in menus) { pair.Source._nativeMenu = false; pair.Source._nativeMenuRID = default; }
            try { owner.RetirePopupMenus(menus.Select(pair => pair.RID).ToArray()); } catch (Exception error) { CollectException(ref errors, error); }
        }
        ThrowCollected("Native popup callbacks or retirement failed.", errors); return true;
    }
    private void NativeItemFocus(Item item, bool signal)
    {
        if (IsDisposed) return; var index = _items.IndexOf(item);
        if (!Eligible(index) || _focused == index) return; _focused = index;
        if (signal) BeginEventEmission(_publisherIDFocused.Handlers)?.Invoke(item.ID);
    }
    private RID BuildNativeMenu(List<(PopupMenu Source, RID RID)> menus)
    {
        var rid = NativeMenu.CreateMenu(); menus.Add((this, rid)); _nativeMenuRID = rid; _nativeMenu = true; _focused = -1;
        NativeMenu.SetInterfaceDirection(rid, IsLayoutRTL());
        NativeMenu.SetPopupCloseCallback(rid, () => { if (!IsDisposed) NotifyNativePopupHide(); });
        foreach (var item in _items)
        {
            var label = item.AutoTranslate == NodeAutoTranslateMode.Disabled ? item.Text : item.AutoTranslate == NodeAutoTranslateMode.Always ? Tr(item.Text) : Atr(item.Text);
            int index;
            if (item.Separator) index = NativeMenu.AddSeparator(rid);
            else if (item.Submenu is { } child) index = NativeMenu.AddSubmenuItem(rid, label, child.BuildNativeMenu(menus));
            else index = NativeMenu.AddItem(rid, label, () => { if (IsDisposed) return; var current = _items.IndexOf(item); if (current >= 0 && Eligible(current)) Activate(current); });
            NativeMenu.ObserveItemFocus(rid, index, () => NativeItemFocus(item, true));
            NativeMenu.SetItemHoverCallbacks(rid, index, () => NativeItemFocus(item, false));
            NativeMenu.SetItemCheckable(rid, index, item.Checkable == 1);
            if (item.Checkable == 2) NativeMenu.SetItemRadioCheckable(rid, index, true);
            NativeMenu.SetItemChecked(rid, index, item.Checkable != 0 && item.Checked); NativeMenu.SetItemDisabled(rid, index, item.Disabled);
            NativeMenu.SetItemHidden(rid, index, !item.Visible); NativeMenu.SetItemIcon(rid, index, item.Icon);
        }
        return rid;
    }
}
