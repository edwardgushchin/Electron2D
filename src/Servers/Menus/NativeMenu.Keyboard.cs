using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class NativeMenu
{
    [ThreadStatic] private static NativeMenu? _keyboardOwner;
    private Menu? _keyboardRoot;
    private Item? _keyboardSelection;
    private KeyboardMessage? _forwardKey;

    private static Key NormalizeAccelerator(Key key)
    {
        if ((key & (Key)KeyModifierMask.CommandOrControl) != 0)
            key = (key & ~(Key)KeyModifierMask.CommandOrControl) | (Key)KeyModifierMask.Control;
        return key;
    }

    private static Item? FindAccelerator(Menu menu, Key key, bool echo = false)
    {
        foreach (var item in menu.Items)
        {
            if (!item.Alive || item.Hidden || item.Disabled || item.Separator || item.ShortcutDisabled || echo && !item.AllowEcho) continue;
            if (key != Key.None && NormalizeAccelerator(item.Accelerator) == key) return item;
            if (item.Submenu is { } child && FindAccelerator(child, key, echo) is { } found) return found;
        }
        return null;
    }

    private static bool IsLiveSelection(Item item)
    {
        for (Item? current = item; current is not null; current = current.Owner.Parent)
            if (!current.Alive || !current.Owner.Alive || current.Hidden || current.Disabled || current.Separator) return false;
        return true;
    }

    private static bool HasAltAccelerator(Menu menu)
    {
        foreach (var item in menu.Items)
            if (IsLiveSelection(item) && !item.ShortcutDisabled && ((NormalizeAccelerator(item.Accelerator) & (Key)KeyModifierMask.Alt) != 0 || item.Submenu is { } child && HasAltAccelerator(child))) return true;
        return false;
    }

    internal bool DispatchAccelerator(InputEventKey key)
    {
        Check();
        if (!key.Pressed || _tracking) return false;
        var code = key.GetKeycodeWithModifiers();
        Item? selected = null;
        foreach (var menu in _menus.Values)
            if (menu.Parent is null && FindAccelerator(menu, code, key.Echo) is { } item) { selected = item; break; }
        if (selected?.KeyCallback is not { } callback) return false;
        callback.Invoke(selected.Tag);
        return true;
    }

    private void SetAccelerator(RID rid, int index, Key key)
    {
        Writable(); var item = At(rid, index); var old = item.Accelerator; item.Accelerator = key;
        try { Update(item); } catch { item.Accelerator = old; throw; }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardMessage
    {
        internal nint Window;
        internal uint Message;
        internal nuint WParam;
        internal nint LParam;
        internal uint Time;
        internal int X, Y;
        internal uint Private;
    }

    private static Key NativeKey(nuint value, nint details)
    {
        var key = WindowsKeycode(value, details);
        if (key == Key.None) return key;
        if (key != Key.Shift && Win.GetKeyState(0x10) < 0) key |= (Key)KeyModifierMask.Shift;
        if (key != Key.Control && Win.GetKeyState(0x11) < 0) key |= (Key)KeyModifierMask.Control;
        if (key != Key.Alt && Win.GetKeyState(0x12) < 0) key |= (Key)KeyModifierMask.Alt;
        if (key != Key.Meta && (Win.GetKeyState(0x5b) < 0 || Win.GetKeyState(0x5c) < 0)) key |= (Key)KeyModifierMask.Meta;
        return key;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint FilterKeyboard(int code, nuint wParam, nint lParam)
    {
        var owner = _keyboardOwner;
        if (code == 2 && owner?._keyboardRoot is { } menu)
        {
            try
            {
                var message = (KeyboardMessage*)lParam;
                if (message->Message is 0x100 or 0x104 && FindAccelerator(menu, NativeKey(message->WParam, message->LParam), (message->LParam & 0x40000000) != 0) is { } item)
                {
                    // Without a key callback the engine must receive the original key.
                    if (item.KeyCallback is null) owner._forwardKey = *message;
                    owner._keyboardSelection = item;
                    if (Win.EndMenu() == 0) throw Failure("close the accelerator menu");
                    return 1;
                }
                // Otherwise Alt closes the native menu before its shortcut key arrives.
                if (message->Message is 0x100 or 0x104 && message->WParam is 0x12 or 0xa4 or 0xa5 && HasAltAccelerator(menu)) return 1;
            }
            catch (Exception error)
            {
                owner._keyboardSelection = null; owner._forwardKey = null;
                owner._callbackFailure = owner._callbackFailure is null ? error : new AggregateException(owner._callbackFailure, error);
                Win.EndMenu(); return 1;
            }
        }
        return Win.CallNextHookEx(0, code, wParam, lParam);
    }

    private static partial class Win
    {
        [LibraryImport("user32.dll", EntryPoint = "MapVirtualKeyW")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial uint MapVirtualKey(uint code, uint kind);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial short GetKeyState(int key);
        [LibraryImport("kernel32.dll", EntryPoint = "GetCurrentThreadId")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial uint GetCurrentThreadID();
        [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint SetWindowsHookEx(int kind, delegate* unmanaged[Stdcall]<int, nuint, nint, nint> callback, nint module, uint thread);
        [LibraryImport("user32.dll", SetLastError = true)][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int UnhookWindowsHookEx(nint hook);
        [LibraryImport("user32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint CallNextHookEx(nint hook, int code, nuint wParam, nint lParam);
    }
}
