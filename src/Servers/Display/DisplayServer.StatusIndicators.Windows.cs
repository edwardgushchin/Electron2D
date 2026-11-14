using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const uint StatusCallbackMessage = 0x8000 + 0x2d1;
    private const nuint StatusSubclassID = 0x45324453;
    private GCHandle _statusPin;
    private nint _statusWindow;
    private uint _taskbarCreated;
    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct StatusInfo
    {
        internal uint Size; internal nint Window; internal uint ID, Flags, Message; internal nint Icon;
        internal fixed char Tip[128]; internal uint State, StateMask; internal fixed char Info[256];
        internal uint Version; internal fixed char Title[64]; internal uint InfoFlags; internal Guid Guid; internal nint Balloon;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct StatusIdentifier { internal uint Size; internal nint Window; internal uint ID; internal Guid Guid; }
    [StructLayout(LayoutKind.Sequential)]
    private struct StatusRectangle { internal int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)]
    private struct StatusPoint { internal int X, Y; }
    [StructLayout(LayoutKind.Sequential)]
    private struct IconDescription { internal int Icon; internal uint X, Y; internal nint Mask, Color; }
    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapDescription { internal int Type, Width, Height, Stride; internal ushort Planes, Bits; internal nint Pixels; }
    private bool HasStatusIndicatorHost() => OperatingSystem.IsWindows() && _videoDriver == "windows" && StatusWin.FindWindow("Shell_TrayWnd", null) != 0;
    private unsafe bool NotifyStatus(StatusIndicator value, uint operation, uint flags)
    {
        var data = new StatusInfo { Size = (uint)sizeof(StatusInfo), Window = _statusWindow, ID = (uint)value.ID, Flags = flags, Message = StatusCallbackMessage, Icon = value.Icon, Version = 3 };
        var length = Math.Min(127, value.Tooltip.Length); if (length > 0 && length < value.Tooltip.Length && char.IsHighSurrogate(value.Tooltip[length - 1])) length--;
        for (var i = 0; i < length; i++) data.Tip[i] = value.Tooltip[i];
        return StatusWin.NotifyIcon(operation, in data) != 0;
    }
    private bool SetStatusVersion(StatusIndicator value) => NotifyStatus(value, 4, 0);
    private unsafe Rect2 GetStatusRect(StatusIndicator value)
    {
        var id = new StatusIdentifier { Size = (uint)sizeof(StatusIdentifier), Window = _statusWindow, ID = (uint)value.ID };
        return StatusWin.NotifyIconGetRect(in id, out var rect) == 0 ? new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top) : default;
    }
    private static unsafe nint CopyStatusIcon(Texture source)
    {
        var color = NativeMenu.CopyNativeBitmap(source); nint mask = 0;
        try
        {
            if (StatusWin.GetObject(color, sizeof(BitmapDescription), out var bitmap) == 0) throw StatusFailure("inspect copied icon pixels");
            var stride = checked((bitmap.Width + 15) / 16 * 2);
            var bytes = new byte[checked(stride * bitmap.Height)];
            // Windows falls back to the AND mask when every source alpha is zero.
            for (var y = 0; y < bitmap.Height; y++)
                for (var x = 0; x < bitmap.Width; x++)
                    if (((byte*)bitmap.Pixels)[y * bitmap.Stride + x * 4 + 3] == 0)
                        bytes[y * stride + x / 8] |= (byte)(0x80 >> (x % 8));
            fixed (byte* data = bytes) mask = StatusWin.CreateBitmap(bitmap.Width, bitmap.Height, 1, 1, (nint)data);
            if (mask == 0) throw StatusFailure("create the icon transparency mask");
            var info = new IconDescription { Icon = 1, Color = color, Mask = mask }; var icon = StatusWin.CreateIconIndirect(in info);
            if (icon == 0) throw StatusFailure("copy status icon pixels"); return icon;
        }
        finally { if (mask != 0) StatusWin.DeleteObject(mask); StatusWin.DeleteObject(color); }
    }
    private unsafe void EnsureStatusHook()
    {
        if (_statusWindow != 0) return;
        var window = WindowGetNativeHandleCore(HandleType.WindowHandle); var taskbar = StatusWin.RegisterWindowMessage("TaskbarCreated");
        if (taskbar == 0) throw StatusFailure("observe notification-area recovery");
        var pin = GCHandle.Alloc(this);
        if (WindowSubclassNative.SetWindowSubclass(window, &StatusSubclass, StatusSubclassID, (nuint)GCHandle.ToIntPtr(pin)) == 0) { pin.Free(); throw StatusFailure("observe status-indicator input"); }
        _statusPin = pin; _statusWindow = window; _taskbarCreated = taskbar;
    }
    private unsafe void ReleaseStatusHook()
    {
        if (_statusWindow == 0) return;
        if (WindowSubclassNative.RemoveWindowSubclass(_statusWindow, &StatusSubclass, StatusSubclassID) != 0) { if (_statusPin.IsAllocated) _statusPin.Free(); _statusPin = default; _statusWindow = 0; }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe nint StatusSubclass(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint data)
    {
        var owner = (DisplayServer)GCHandle.FromIntPtr((nint)data).Target!;
        if (message == 0x82)
        {
            var result = WindowSubclassNative.DefSubclassProc(window, message, wParam, lParam); GCHandle.FromIntPtr((nint)data).Free(); owner._statusPin = default; owner._statusWindow = 0; return result;
        }
        try
        {
            if (message == owner._taskbarCreated) owner._recreateStatus = true;
            else if (message == StatusCallbackMessage && !owner._dropStatusInput && owner._statusIndicators.TryGetValue((int)wParam, out var value))
            {
                var button = (uint)lParam switch { 0x201 => MouseButton.Left, 0x204 => MouseButton.Right, 0x207 => MouseButton.Middle, 0x20b => MouseButton.XButton1, _ => MouseButton.None };
                if (button != MouseButton.None && StatusWin.GetCursorPos(out var position) != 0)
                    owner._statusPresses.Enqueue(new(value, button, new(position.X, position.Y), owner.GetStatusRect(value), value.Callback, value.MenuOwner, value.Menu));
            }
        }
        catch (Exception error) { owner._statusNativeFailure ??= error; }
        return WindowSubclassNative.DefSubclassProc(window, message, wParam, lParam);
    }
    private static unsafe partial class StatusWin
    {
        [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconW")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int NotifyIcon(uint operation, in StatusInfo data);
        [LibraryImport("shell32.dll", EntryPoint = "Shell_NotifyIconGetRect")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int NotifyIconGetRect(in StatusIdentifier id, out StatusRectangle rect);
        [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint FindWindow(string name, string? title);
        [LibraryImport("user32.dll", EntryPoint = "RegisterWindowMessageW", StringMarshalling = StringMarshalling.Utf16)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial uint RegisterWindowMessage(string name);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int GetCursorPos(out StatusPoint position);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint CreateIconIndirect(in IconDescription info);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int DestroyIcon(nint icon);
        [LibraryImport("gdi32.dll", EntryPoint = "GetObjectW")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int GetObject(nint value, int size, out BitmapDescription bitmap);
        [LibraryImport("gdi32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint CreateBitmap(int width, int height, uint planes, uint bits, nint pixels);
        [LibraryImport("gdi32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int DeleteObject(nint value);
    }
}
