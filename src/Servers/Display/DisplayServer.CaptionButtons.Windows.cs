using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const nuint CaptionSubclassID = 0x45324443;
    private const uint StyleChanging = 0x007C;
    private const int WindowStyle = -16;
    private const int MinimizeBox = 0x00020000;
    private const int MaximizeBox = 0x00010000;
    private int _disabledCaptionButtons;
    internal bool SupportsCaptionButtons => _videoDriver == "windows" && !IsNativePopup;

    private static int CaptionButton(WindowFlag flag) => flag == WindowFlag.MinimizeDisabled ? MinimizeBox : MaximizeBox;

    private bool GetCaptionButtonDisabled(WindowFlag flag)
    {
        if (!SupportsCaptionButtons)
            throw new NotSupportedException("Native caption button policy is unavailable on this display.");
        return (_disabledCaptionButtons & CaptionButton(flag)) != 0;
    }

    private unsafe void SetCaptionButtonDisabled(WindowFlag flag, bool disabled)
    {
        var button = CaptionButton(flag);
        if (GetCaptionButtonDisabled(flag) == disabled)
            return;
        var hwnd = WindowGetNativeHandleCore(HandleType.WindowHandle);
        var previousStyle = ReadCaptionStyle(hwnd);
        var previousMask = _disabledCaptionButtons;
        var mask = disabled ? previousMask | button : previousMask & ~button;
        var flags = SDL.GetWindowFlags(_window.DangerousGetHandle());
        var resizable = (flags & SDL.WindowFlags.Resizable) != 0 &&
            ((flags & SDL.WindowFlags.Borderless) == 0 || SDL.GetHintBoolean("SDL_BORDERLESS_RESIZABLE_STYLE", true));
        var defaultButton = button == MinimizeBox || (flags & SDL.WindowFlags.Fullscreen) == 0 &&
            (resizable || (flags & SDL.WindowFlags.Maximized) != 0);
        var style = disabled || !defaultButton ? previousStyle & ~button : previousStyle | button;

        SetMask(mask);
        try { WriteCaptionStyle(hwnd, style); }
        catch (Exception error)
        {
            try { SetMask(previousMask); WriteCaptionStyle(hwnd, previousStyle); }
            catch (Exception rollback) { throw new AggregateException(error, rollback); }
            throw;
        }
        _disabledCaptionButtons = mask;

        void SetMask(int value)
        {
            if (WindowSubclassNative.SetWindowSubclass(hwnd, &CaptionSubclass, CaptionSubclassID, (nuint)value) == 0)
                throw new InvalidOperationException("Unable to configure the native caption button policy.");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static unsafe nint CaptionSubclass(nint window, uint message, nuint wParam, nint lParam, nuint id, nuint mask)
    {
        if (message == NonClientDestroy)
            WindowSubclassNative.RemoveWindowSubclass(window, &CaptionSubclass, id);
        var result = WindowSubclassNative.DefSubclassProc(window, message, wParam, lParam);
        // SDL rebuilds these style bits when the frame, resize policy or fullscreen changes.
        if (message == StyleChanging && unchecked((int)wParam) == WindowStyle && lParam != 0)
            ((int*)lParam)[1] &= ~(int)mask;
        return result;
    }

    private static int ReadCaptionStyle(nint window)
    {
        var style = GetWindowLong(window, WindowStyle);
        if (style == 0 && Marshal.GetLastPInvokeError() != 0)
            throw CaptionStyleFailure("read the native window style");
        return style;
    }

    private static void WriteCaptionStyle(nint window, int style)
    {
        if (SetWindowLong(window, WindowStyle, style) == 0 && Marshal.GetLastPInvokeError() != 0)
            throw CaptionStyleFailure("set the native window style");
        const uint frameChanged = 0x0020, noActivate = 0x0010, noZOrder = 0x0004, noMove = 0x0002, noSize = 0x0001;
        if (SetWindowPos(window, 0, 0, 0, 0, 0, frameChanged | noActivate | noZOrder | noMove | noSize) == 0)
            throw CaptionStyleFailure("refresh the native window frame");
    }

    private static IOException CaptionStyleFailure(string operation) =>
        new($"Cannot {operation}.", new Win32Exception(Marshal.GetLastPInvokeError()));

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetWindowLong(nint window, int index);
    [LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int SetWindowLong(nint window, int index, int value);
    [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
}
