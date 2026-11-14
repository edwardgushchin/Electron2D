using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const nuint SystemMenuSubclassID = 0x4532444D;
    private const uint SystemCommand = 0x0112;
    private const uint NonClientDestroy = 0x0082;
    private const nuint KeyMenu = 0xF100;
    private const nint Space = 0x20;
    private bool? _enableAltSpaceMenu;

    internal unsafe void ConfigureSystemMenu(bool enabled)
    {
        if (_videoDriver != "windows") return;
        var window = WindowGetNativeHandleCore(HandleType.WindowHandle);
        if (WindowSubclassNative.SetWindowSubclass(window, &SystemMenuSubclass, SystemMenuSubclassID, enabled ? 1u : 0u) == 0)
            throw new InvalidOperationException("Unable to configure the native Windows system menu.");
        _enableAltSpaceMenu = enabled;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe nint SystemMenuSubclass(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint enabled)
    {
        if (message == NonClientDestroy)
            WindowSubclassNative.RemoveWindowSubclass(window, &SystemMenuSubclass, id);
        if (message == SystemCommand && (wParam & 0xFFF0) == KeyMenu && lParam == Space)
        {
            // SDL suppresses keyboard menus by default. Dispatch only the native Alt+Space command.
            return enabled != 0 ? WindowSubclassNative.DefWindowProc(window, message, wParam, lParam) : 0;
        }
        return WindowSubclassNative.DefSubclassProc(window, message, wParam, lParam);
    }

    private static unsafe partial class WindowSubclassNative
    {
        [LibraryImport("user32.dll", EntryPoint = "DefWindowProcW")]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static partial nint DefWindowProc(nint window, uint message, nuint wParam, nint lParam);

        [LibraryImport("comctl32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int SetWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id, nuint data);
        [LibraryImport("comctl32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial int RemoveWindowSubclass(nint window, delegate* unmanaged[Stdcall]<nint, uint, nuint, nint, nuint, nuint, nint> callback, nuint id);
        [LibraryImport("comctl32.dll")][DefaultDllImportSearchPaths(DllImportSearchPath.System32)] internal static partial nint DefSubclassProc(nint window, uint message, nuint wParam, nint lParam);
    }
}
