using System.Runtime.InteropServices;

namespace Electron2D;

/// <summary>Owns operating-system popup menus for the current native display session.</summary>
/// <remarks>Static calls require the display owner thread. Menus own native storage and borrow typed callbacks,
/// tags and icon sources. Native presentation is modal; item selection follows native closure.</remarks>
public sealed partial class NativeMenu : ElectronObject
{
    private readonly DisplayServer _display;
    private readonly Dictionary<RID, Menu> _menus = [];
    private uint _nextCommand;
    private bool _retiring, _tracking, _initializing, _preparing, _hoverMouse;
    private Exception? _callbackFailure;
    private readonly List<Menu> _opened = [];
    private Menu? _hoverMenu;
    private Item? _hoverItem;
    private NativeMenu(DisplayServer display) => _display = display;
    internal static NativeMenu CreateOwner(DisplayServer display) => new(display);
    internal static NativeMenu BorrowOwner() => Require();
    internal void RetirePopupMenus(ReadOnlySpan<RID> identities)
    {
        if (IsDisposed) return; Writable(); List<Exception>? errors = null;
        foreach (var rid in identities)
        {
            if (_menus.TryGetValue(rid, out var menu))
            {
                try { ClearCore(menu); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        foreach (var rid in identities)
        {
            if (_menus.ContainsKey(rid))
            {
                try { FreeCore(rid); } catch (Exception error) { (errors ??= []).Add(error); }
            }
        }
        if (errors is not null) throw new AggregateException("Native popup tree retirement failed.", errors);
    }
    private static NativeMenu Require() => (DisplayServer.Service ?? throw new InvalidOperationException("No native display is active.")).GetNativeMenus();
    private void Check() { ThrowIfDisposed(); _display.EnsureNativeMenuOwner(); }
    private void Writable() { Check(); if (_preparing || _tracking && !_initializing) throw new InvalidOperationException("Native menu items cannot change while their popup is being tracked."); }
    private void Supported() { Check(); if (!_display.SupportsNativeMenus) throw new NotSupportedException("This display has no native popup-menu adapter."); }
    private Menu Get(RID rid) { Check(); return _menus.TryGetValue(rid, out var menu) ? menu : throw new ArgumentException("The menu identity is empty, retired or belongs to another session.", nameof(rid)); }
    private Item At(RID rid, int index) { var menu = Get(rid); if ((uint)index >= (uint)menu.Items.Count) throw new ArgumentOutOfRangeException(nameof(index)); return menu.Items[index]; }
    private sealed class Menu(RID rid, nint handle)
    {
        internal readonly RID RID = rid;
        internal readonly nint Handle = handle;
        internal readonly List<Item> Items = [];
        internal Item? Parent;
        internal Action? Open, Close;
        internal bool RTL, Opened, Alive = true;
        internal Vector2 Size;
    }
    private sealed class Item(Menu owner, uint command)
    {
        internal readonly Menu Owner = owner;
        internal readonly uint Command = command;
        internal string Text = "";
        internal bool Checked, Disabled, Hidden, Separator, Alive = true;
        internal int Checkable, State, MaxStates;
        internal Menu? Submenu;
        internal Texture? Icon;
        internal nint Bitmap;
        internal int IconDirty;
        internal Action<Resource>? Changed;
        internal Action<ElectronObject>? Disposed;
        internal object? Tag;
        internal ICallback? Callback, Hover;
        internal Action? Focus;
    }
    private interface ITag { Type Type { get; } }
    private sealed record TagValue<T>(T Value) : ITag { public Type Type => typeof(T); }
    private interface ICallback { Type? TagType { get; } void Invoke(object? tag); }
    private sealed record Callback(Action Value) : ICallback
    { public Type? TagType => null; public void Invoke(object? tag) => Value(); }
    private sealed record Callback<T>(Action<T> Value) : ICallback
    { public Type? TagType => typeof(T); public void Invoke(object? tag) => Value(((TagValue<T>)tag!).Value); }
    private static void ValidateTag(Item item, Type? type)
    {
        if (item.Callback?.TagType is { } first && first != type || item.Hover?.TagType is { } second && second != type)
            throw new ArgumentException("The tag must preserve its callbacks' exact generic type.");
    }
    private static void ValidateCallback(Item item, ICallback? callback)
    { if (callback?.TagType is { } type && (item.Tag is not ITag tag || tag.Type != type)) throw new ArgumentException("Set a tag of the callback's exact generic type first."); }
    private static void Text(string value) { ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Native menu text cannot contain a zero character.", nameof(value)); }
    private RID CreateCore()
    {
        Supported(); Writable(); var handle = Win.CreatePopupMenu(); if (handle == 0) throw Failure("create a popup menu");
        try { var rid = RID.Allocate(); _menus.Add(rid, new(rid, handle)); return rid; }
        catch { Win.DestroyMenu(handle); throw; }
    }
    private int Add(RID rid, string text, ICallback? callback, object? tag, int checkable, bool separator, Texture? icon, RID submenu, int states, int state, int index)
    {
        Writable(); Text(text); if (icon is { IsDisposed: true }) throw new ObjectDisposedException(nameof(icon));
        var menu = Get(rid); var child = submenu.IsValid() ? Get(submenu) : null; ValidateChild(menu, child, null);
        var target = index == -1 ? menu.Items.Count : Math.Clamp(index, 0, menu.Items.Count);
        if (_nextCommand == uint.MaxValue) throw new InvalidOperationException("Native menu command identities are exhausted.");
        var item = new Item(menu, ++_nextCommand) { Text = text, Callback = callback, Tag = tag, Checkable = checkable, Separator = separator, Submenu = child, State = state, MaxStates = states };
        menu.Items.EnsureCapacity(menu.Items.Count + 1);
        try { SetIconSource(item, icon); InsertNative(menu, item, VisibleIndex(menu, target)); }
        catch { RetireItem(item); throw; }
        menu.Items.Insert(target, item); if (child is not null) child.Parent = item; return target;
    }
    private static int VisibleIndex(Menu menu, int index) { var visible = 0; for (var i = 0; i < index; i++) if (!menu.Items[i].Hidden) visible++; return visible; }
    private static void ValidateChild(Menu parent, Menu? child, Item? replacing)
    {
        if (child is null) return;
        if (child.Parent is not null && child.Parent != replacing) throw new InvalidOperationException("A native submenu can have only one parent item.");
        for (Menu? current = parent; current is not null; current = current.Parent?.Owner)
            if (current == child) throw new ArgumentException("Native submenu cycles are not allowed.");
    }
    private void RemoveCore(Menu menu, int index)
    {
        var item = menu.Items[index];
        if (!item.Hidden && Win.RemoveMenu(menu.Handle, (uint)VisibleIndex(menu, index), 0x400) == 0) throw Failure("detach a menu item");
        menu.Items.RemoveAt(index); if (item.Submenu is { } child) child.Parent = null; RetireItem(item);
    }
    private void ClearCore(Menu menu) { for (var i = menu.Items.Count - 1; i >= 0; i--) RemoveCore(menu, i); }
    private void FreeCore(RID rid)
    {
        Writable(); if (_tracking) throw new InvalidOperationException("A native menu cannot retire during presentation.");
        var menu = Get(rid); if (menu.Parent is not null) throw new InvalidOperationException("Detach a submenu before freeing it.");
        ClearCore(menu); if (Win.DestroyMenu(menu.Handle) == 0) throw Failure("free a menu"); menu.Alive = false; _menus.Remove(rid);
    }
    private void Update(Item item)
    {
        if (item.Hidden) return;
        var index = item.Owner.Items.IndexOf(item);
        SetNative(item.Owner, item, VisibleIndex(item.Owner, index));
    }
    private void SetBool(RID rid, int index, bool value, int role)
    {
        Writable(); var item = At(rid, index); var old = role == 0 ? item.Checked : item.Disabled;
        if (old == value) return; if (role == 0) item.Checked = value; else item.Disabled = value;
        try { Update(item); } catch { if (role == 0) item.Checked = old; else item.Disabled = old; throw; }
    }
    private void SetCheck(RID rid, int index, bool value, int kind)
    {
        Writable(); var item = At(rid, index); var old = item.Checkable; item.Checkable = value ? kind : 0;
        try { Update(item); } catch { item.Checkable = old; throw; }
    }
    private void SetTextCore(RID rid, int index, string value)
    {
        Writable(); Text(value); var item = At(rid, index); var old = item.Text; item.Text = value;
        try { Update(item); } catch { item.Text = old; throw; }
    }
    private void HideCore(RID rid, int index, bool hidden)
    {
        Writable(); var item = At(rid, index); if (item.Hidden == hidden) return;
        var nativeIndex = VisibleIndex(item.Owner, index);
        if (hidden) { if (Win.RemoveMenu(item.Owner.Handle, (uint)nativeIndex, 0x400) == 0) throw Failure("hide a menu item"); }
        else InsertNative(item.Owner, item, nativeIndex);
        item.Hidden = hidden;
    }
    private void SubmenuCore(RID rid, int index, RID childRID)
    {
        Writable(); var item = At(rid, index); var child = childRID.IsValid() ? Get(childRID) : null;
        if (item.Submenu == child) return; ValidateChild(item.Owner, child, item); var old = item.Submenu; item.Submenu = child;
        if (!item.Hidden)
        {
            var position = VisibleIndex(item.Owner, index);
            if (Win.RemoveMenu(item.Owner.Handle, (uint)position, 0x400) == 0) { item.Submenu = old; throw Failure("detach a submenu item"); }
            try { InsertNative(item.Owner, item, position); }
            catch { item.Submenu = old; InsertNative(item.Owner, item, position); throw; }
        }
        if (old is not null) old.Parent = null; if (child is not null) child.Parent = item;
    }
    private void Reorder(RID rid, int index, int target)
    {
        Writable(); var menu = Get(rid); _ = At(rid, index); if ((uint)target >= (uint)menu.Items.Count) throw new ArgumentOutOfRangeException(nameof(target));
        if (index == target) return; var item = menu.Items[index];
        if (!item.Hidden && Win.RemoveMenu(menu.Handle, (uint)VisibleIndex(menu, index), 0x400) == 0) throw Failure("move a menu item");
        menu.Items.RemoveAt(index);
        try { if (!item.Hidden) InsertNative(menu, item, VisibleIndex(menu, target)); }
        catch { menu.Items.Insert(index, item); if (!item.Hidden) InsertNative(menu, item, VisibleIndex(menu, index)); throw; }
        menu.Items.Insert(target, item);
    }
    private void SetIconSource(Item item, Texture? icon)
    {
        Action<Resource>? changed = null; Action<ElectronObject>? disposed = null;
        if (icon is not null)
        {
            changed = _ => Interlocked.Exchange(ref item.IconDirty, 1);
            disposed = _ => Interlocked.Exchange(ref item.IconDirty, 1);
            icon.Changed += changed;
            try { icon.Disposed += disposed; } catch { icon.Changed -= changed; throw; }
        }
        Unwatch(item); item.Icon = icon; item.Changed = changed; item.Disposed = disposed; item.IconDirty = 1;
    }
    private static void Unwatch(Item item)
    { if (item.Icon is { } icon) { icon.Changed -= item.Changed; icon.Disposed -= item.Disposed; } item.Changed = null; item.Disposed = null; }
    private static void RetireItem(Item item)
    { item.Alive = false; Unwatch(item); if (item.Bitmap != 0) Win.DeleteObject(item.Bitmap); item.Bitmap = 0; item.Icon = null; item.Callback = item.Hover = null; item.Focus = null; item.Tag = null; }
    private void IconCore(RID rid, int index, Texture? icon)
    {
        Writable(); if (icon is { IsDisposed: true }) throw new ObjectDisposedException(nameof(icon)); var item = At(rid, index);
        if (ReferenceEquals(item.Icon, icon)) return; SetIconSource(item, icon); Interlocked.Exchange(ref item.IconDirty, 1);
    }
    private void RefreshIcons(Menu menu)
    {
        _preparing = true;
        try
        {
            RefreshMenuIcons(menu);
        }
        finally { _preparing = false; }
    }
    private void RefreshMenuIcons(Menu menu)
    {
        foreach (var item in menu.Items)
        {
            if (item.Hidden) continue;
            if (item.Submenu is { } child) RefreshMenuIcons(child);
            if (Interlocked.Exchange(ref item.IconDirty, 0) == 0) continue;
            try
            {
                var replacement = item.Icon is { IsDisposed: false } icon ? Bitmap(icon) : 0; var old = item.Bitmap; item.Bitmap = replacement;
                try { Update(item); } catch { item.Bitmap = old; if (replacement != 0) Win.DeleteObject(replacement); throw; }
                if (old != 0) Win.DeleteObject(old);
            }
            catch { Interlocked.Exchange(ref item.IconDirty, 1); throw; }
        }
    }
    internal void Retire() { _retiring = true; Dispose(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { Check(); if (!_retiring) throw new InvalidOperationException("The display owns the native-menu service lifetime."); if (_tracking) throw new InvalidOperationException("A native popup is active."); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;
        List<Exception>? errors = null;
        foreach (var menu in _menus.Values)
            for (var i = menu.Items.Count - 1; i >= 0; i--)
                try { RemoveCore(menu, i); } catch (Exception error) { (errors ??= []).Add(error); }
        // A failed detach leaves native recursive ownership with its parent.
        foreach (var menu in _menus.Values)
            if (menu.Parent is null && Win.DestroyMenu(menu.Handle) == 0) (errors ??= []).Add(Failure("free a menu"));
        foreach (var menu in _menus.Values) { menu.Alive = false; foreach (var item in menu.Items) RetireItem(item); menu.Items.Clear(); }
        _menus.Clear();
        try { base.Dispose(disposing); } catch (Exception error) { (errors ??= []).Add(error); }
        if (errors is not null) throw new AggregateException("Native menu retirement failed.", errors);
    }
    private static IOException Failure(string operation) => new($"Could not {operation}: {new System.ComponentModel.Win32Exception(Marshal.GetLastPInvokeError()).Message}");
}
