using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class NativeMenu
{
    private readonly List<Action> _closed = [];
    private const nuint SubclassID = 0x4532444d;
    [StructLayout(LayoutKind.Sequential)]
    private struct MenuInfo
    {
        internal uint Size, Mask, Type, State, ID;
        internal nint Submenu, CheckedBitmap, UncheckedBitmap;
        internal nuint Data;
        internal nint Text;
        internal uint TextLength;
        internal nint Bitmap;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        internal uint Size;
        internal int Width, Height;
        internal ushort Planes, Bits;
        internal uint Compression, Bytes;
        internal int XScale, YScale;
        internal uint Colors, Important;
    }
    private static MenuInfo Describe(Item item) => new()
    {
        Size = (uint)sizeof(MenuInfo),
        Mask = 0x1ef,
        Type = item.Separator ? 0x800u : item.Checkable == 2 ? 0x200u : 0,
        State = (item.Checked ? 8u : 0) | (item.Disabled ? 3u : 0),
        ID = item.Command,
        Submenu = item.Submenu?.Handle ?? 0,
        Bitmap = item.Bitmap
    };
    private static void InsertNative(Menu menu, Item item, int index)
    {
        var info = Describe(item);
        fixed (char* text = item.Text) { info.Text = (nint)text; if (Win.InsertMenuItem(menu.Handle, (uint)index, 1, in info) == 0) throw Failure("insert a native menu item"); }
    }
    private static void SetNative(Menu menu, Item item, int index)
    {
        var info = Describe(item); info.Mask &= ~4u;
        fixed (char* text = item.Text) { info.Text = (nint)text; if (Win.SetMenuItemInfo(menu.Handle, (uint)index, 1, in info) == 0) throw Failure("update a native menu item"); }
    }
    internal static nint CopyNativeBitmap(Texture texture)
    {
        using var image = texture.GetImage() ?? throw new NotSupportedException("Native icons require readable image pixels.");
        if ((long)image.Width * image.Height > 16_777_216) throw new NotSupportedException("Native icons must fit the 64 MiB pixel budget.");
        image.Decompress(); image.Convert(Image.Format.Rgba8); var data = image.GetData();
        var info = new BitmapInfo { Size = (uint)sizeof(BitmapInfo), Width = image.Width, Height = -image.Height, Planes = 1, Bits = 32, Bytes = checked((uint)(image.Width * image.Height * 4)) };
        var bitmap = Win.CreateDIBSection(0, in info, 0, out var pixels, 0, 0);
        if (bitmap == 0 || pixels == 0) { if (bitmap != 0) Win.DeleteObject(bitmap); throw Failure("copy native menu icon pixels"); }
        var output = new Span<byte>((void*)pixels, (int)info.Bytes);
        for (var i = 0; i < output.Length; i += 4)
        {
            var alpha = data[i + 3]; output[i] = (byte)((data[i + 2] * alpha + 127) / 255);
            output[i + 1] = (byte)((data[i + 1] * alpha + 127) / 255); output[i + 2] = (byte)((data[i] * alpha + 127) / 255); output[i + 3] = alpha;
        }
        return bitmap;
    }
    private void Measure(Menu menu)
    {
        var width = 0; var height = 0; var visible = 0;
        foreach (var item in menu.Items)
        {
            if (item.Hidden) continue;
            if (Win.GetMenuItemRect(0, menu.Handle, (uint)visible++, out var rect) == 0) continue;
            width = Math.Max(width, rect.Right - rect.Left); height = checked(height + rect.Bottom - rect.Top);
        }
        if (width != 0 || height != 0) menu.Size = new(width, height);
    }
    private Menu? FromNative(nint handle)
    { foreach (var menu in _menus.Values) if (menu.Handle == handle) return menu; return null; }
    private void Message(uint message, nuint wParam, nint lParam)
    {
        if (message == 0x117 && FromNative((nint)wParam) is { } opening)
        {
            opening.Opened = true; _opened.Add(opening); _initializing = true;
            try { opening.Open?.Invoke(); RefreshIcons(opening); } finally { _initializing = false; }
        }
        else if (message == 0x125 && FromNative((nint)wParam) is { } closing)
        { Measure(closing); closing.Opened = false; if (closing.Close is { } callback) _closed.Add(callback); }
        else if (message == 0x121)
        { foreach (var menu in _opened) if (menu.Opened) Measure(menu); }
        else if (message == 0x11f)
        {
            var menu = FromNative(lParam); if (menu is not null) Measure(menu);
            var flags = (uint)(wParam >> 16) & 0xffff;
            if (menu is null || flags == 0xffff) { _hoverMenu = null; _hoverItem = null; return; }
            Item? item = null; var value = (uint)wParam & 0xffff;
            if ((flags & 0x10) != 0)
            { var visible = 0; foreach (var candidate in menu.Items) if (!candidate.Hidden && visible++ == value) { item = candidate; break; } }
            else
            {
                var visible = 0;
                foreach (var candidate in menu.Items)
                {
                    if (candidate.Hidden) continue;
                    if ((candidate.Command & 0xffff) == value && (Win.GetMenuState(menu.Handle, (uint)visible, 0x400) & 0x80) != 0) { item = candidate; break; }
                    visible++;
                }
            }
            var mouse = (flags & 0x8000) != 0;
            if (menu == _hoverMenu && item == _hoverItem && mouse == _hoverMouse) return;
            _hoverMenu = menu; _hoverItem = item; _hoverMouse = mouse;
            if (item is { Alive: true, Disabled: false }) { if (mouse) item.Hover?.Invoke(item.Tag); else item.Focus?.Invoke(); }
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint Subclass(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        var owner = (NativeMenu)GCHandle.FromIntPtr((nint)data).Target!;
        if (message == 0x82) { var result = Win.DefSubclassProc(window, message, wParam, lParam); GCHandle.FromIntPtr((nint)data).Free(); return result; }
        try { if (owner._tracking) owner.Message(message, wParam, lParam); }
        catch (Exception error) { owner._callbackFailure = owner._callbackFailure is null ? error : new AggregateException(owner._callbackFailure, error); Win.EndMenu(); }
        return Win.DefSubclassProc(window, message, wParam, lParam);
    }
    internal static void CancelPopup(RID rid)
    { var owner = Require(); var menu = owner.Get(rid); if (owner._tracking && menu.Opened && Win.EndMenu() == 0) throw Failure("cancel the native popup tree"); }
    internal static void ObserveItemFocus(RID rid, int index, Action callback) { var owner = Require(); owner.Writable(); owner.At(rid, index).Focus = callback; }
    internal static void PopupForDisplay(RID rid, Vector2i position, DisplayServer display) => Require().Present(rid, position, display);
    private void Present(RID rid, Vector2i position, DisplayServer? display = null)
    {
        Supported(); if (_tracking) throw new InvalidOperationException("Native menu presentation cannot reenter.");
        var menu = Get(rid); if (menu.Parent is not null) throw new InvalidOperationException("Present the root of a native menu tree.");
        display ??= _display; var window = _display.BeginNativeMenu(display);
        var pin = default(GCHandle); var hooked = false; uint command = 0; Exception? failure = null;
        _tracking = true; _callbackFailure = null; _opened.Clear(); _closed.Clear();
        try
        {
            RefreshIcons(menu);
            pin = GCHandle.Alloc(this);
            if (Win.SetWindowSubclass(window, &Subclass, SubclassID, (nuint)GCHandle.ToIntPtr(pin)) == 0) throw Failure("observe the native menu loop");
            hooked = true; Win.SetForegroundWindow(window);
            command = Win.TrackPopupMenuEx(menu.Handle, 0x100u | (menu.RTL ? 0x8000u : 0), position.X, position.Y, window, 0);
            Win.PostMessage(window, 0, 0, 0);
        }
        catch (Exception error) { failure = error; }
        finally
        {
            var detached = !hooked || Win.RemoveWindowSubclass(window, &Subclass, SubclassID) != 0;
            if (detached) { if (pin.IsAllocated) pin.Free(); }
            else failure = failure is null ? Failure("detach the native menu observer") : new AggregateException(failure, Failure("detach the native menu observer"));
            foreach (var opened in _opened)
                if (opened.Opened) { Measure(opened); opened.Opened = false; if (opened.Close is { } close) _closed.Add(close); }
            _tracking = _initializing = false; _hoverItem = null; _hoverMenu = null; _display.EndNativeMenu(display);
        }
        var callbacks = _closed.ToArray(); _closed.Clear(); _opened.Clear(); var nativeError = _callbackFailure; _callbackFailure = null;
        if (nativeError is not null) failure = failure is null ? nativeError : new AggregateException(failure, nativeError);
        Item? selected = null;
        if (command != 0) foreach (var candidate in _menus.Values) foreach (var item in candidate.Items) if (item.Command == command) selected = item;
        foreach (var callback in callbacks) try { callback(); } catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        if (selected is { Alive: true, Disabled: false, Separator: false } && selected.Owner.Alive)
            try { selected.Callback?.Invoke(selected.Tag); } catch (Exception error) { failure = failure is null ? error : new AggregateException(failure, error); }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
    private static partial class Win
    {
        [LibraryImport("user32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint CreatePopupMenu();
        [LibraryImport("user32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int DestroyMenu(nint menu);
        [LibraryImport("user32.dll", EntryPoint = "InsertMenuItemW", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int InsertMenuItem(nint menu, uint index, int byPosition, in MenuInfo item);
        [LibraryImport("user32.dll", EntryPoint = "SetMenuItemInfoW", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int SetMenuItemInfo(nint menu, uint index, int byPosition, in MenuInfo item);
        [LibraryImport("user32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int RemoveMenu(nint menu, uint index, uint flags);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial uint GetMenuState(nint menu, uint item, uint flags);
        [LibraryImport("user32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int GetMenuItemRect(nint window, nint menu, uint index, out NativeRect rectangle);
        [LibraryImport("user32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial uint TrackPopupMenuEx(nint menu, uint flags, int x, int y, nint owner, nint parameters);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int SetForegroundWindow(nint window);
        [LibraryImport("user32.dll", EntryPoint = "PostMessageW")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int PostMessage(nint window, uint message, nuint wParam, nint lParam);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int EndMenu();
        [LibraryImport("gdi32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint CreateDIBSection(nint dc, in BitmapInfo info, uint usage, out nint pixels, nint section, uint offset);
        [LibraryImport("gdi32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int DeleteObject(nint value);
        [LibraryImport("comctl32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int SetWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id, nuint data);
        [LibraryImport("comctl32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int RemoveWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id);
        [LibraryImport("comctl32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
    }
}
