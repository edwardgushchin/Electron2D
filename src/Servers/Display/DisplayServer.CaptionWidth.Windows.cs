using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private bool _keepNativeTitleVisible;
    private int _captionMinimumWidth;

    internal void SetNativeTitleVisibility(bool enabled)
    {
        EnsureOwner();
        if (_keepNativeTitleVisible == enabled)
            return;
        if (enabled && !SupportsCaptionButtons)
            throw new NotSupportedException("Native title measurement requires a top-level Win32 window.");

        ApplyNativeCaptionMinimum(enabled ? MeasureWindowsCaptionWidth() : 0);
        _keepNativeTitleVisible = enabled;
    }

    private void RefreshNativeCaptionMinimum()
    {
        if (_keepNativeTitleVisible)
            ApplyNativeCaptionMinimum(MeasureWindowsCaptionWidth());
    }

    private void ApplyNativeCaptionMinimum(int width)
    {
        if (width != _captionMinimumWidth)
            ApplyWindowLimits(WindowGetMinSizeCore(), WindowGetMaxSizeCore(), _contentMinimumSize, captionWidth: width);
    }

    private int MeasureWindowsCaptionWidth()
    {
        const int caption = 0x00C00000, systemMenu = 0x00080000;
        const int captionButtonBounds = 5, getNonClientMetrics = 0x0029;
        const int captionButtonWidth = 30, smallIconWidth = 49, edgeWidth = 45;
        var flags = SDL.GetWindowFlags(_window.DangerousGetHandle());
        if ((flags & SDL.WindowFlags.Fullscreen) != 0)
            return _captionMinimumWidth;

        var hwnd = WindowGetNativeHandleCore(HandleType.WindowHandle);
        var style = ReadCaptionStyle(hwnd);
        if ((flags & SDL.WindowFlags.Borderless) != 0 || (style & caption) != caption)
            return 0;

        var title = WindowGetTitle();
        if (title.Length == 0)
            return 0;

        var dpi = GetDpiForWindow(hwnd);
        var metrics = new CaptionMetrics { Size = (uint)Marshal.SizeOf<CaptionMetrics>() };
        if (dpi == 0 || SystemParametersInfoForDpi(getNonClientMetrics, metrics.Size, ref metrics, 0, dpi) == 0)
            throw CaptionStyleFailure("read the native caption metrics");

        // DWM leaves a text inset beside the icon and a drag gap before the button group.
        // Reserve those gaps separately from the icon and button bounds.
        var chrome = 16 * GetSystemMetricsForDpi(edgeWidth, dpi);
        if ((style & systemMenu) != 0)
        {
            var buttons = (style & (MinimizeBox | MaximizeBox)) != 0 ? 3 : 1;
            var width = buttons * GetSystemMetricsForDpi(captionButtonWidth, dpi);
            // DWM supplies the themed button group width once the frame is visible.
            if ((flags & (SDL.WindowFlags.Hidden | SDL.WindowFlags.Minimized)) == 0 &&
                GetCaptionButtonBounds(hwnd, captionButtonBounds, out CaptionRect bounds, 16) == 0)
                width = Math.Max(width, bounds.Right - bounds.Left);
            chrome += width + GetSystemMetricsForDpi(smallIconWidth, dpi);
        }
        return checked(MeasureCaptionText(hwnd, title, ref metrics.CaptionFont) + chrome);
    }

    private static int MeasureCaptionText(nint hwnd, string title, ref CaptionFont font)
    {
        const uint singleLine = 0x0020, calculateRect = 0x0400, noPrefix = 0x0800;
        var dc = GetDC(hwnd);
        if (dc == 0)
            throw CaptionStyleFailure("get a caption measurement context");
        var handle = nint.Zero;
        var previous = nint.Zero;
        try
        {
            handle = CreateFontIndirect(ref font);
            if (handle == 0)
                throw CaptionStyleFailure("create the native caption font");
            previous = SelectCaptionObject(dc, handle);
            if (previous == 0 || previous == -1)
                throw CaptionStyleFailure("select the native caption font");
            var rect = new CaptionRect();
            if (DrawCaptionText(dc, title, title.Length, ref rect, singleLine | calculateRect | noPrefix) == 0)
                throw CaptionStyleFailure("measure the native caption text");
            return rect.Right - rect.Left;
        }
        finally
        {
            if (previous != 0 && previous != -1)
                SelectCaptionObject(dc, previous);
            if (handle != 0)
                CursorDeleteObject(handle);
            ReleaseDC(hwnd, dc);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CaptionRect
    {
        internal int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct CaptionFont
    {
        internal int Height, Width, Escapement, Orientation, Weight;
        internal byte Italic, Underline, StrikeOut, CharSet, OutPrecision, ClipPrecision, Quality, PitchAndFamily;
        internal fixed char FaceName[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CaptionMetrics
    {
        internal uint Size;
        internal int BorderWidth, ScrollWidth, ScrollHeight, CaptionWidth, CaptionHeight;
        internal CaptionFont CaptionFont;
        internal int SmallCaptionWidth, SmallCaptionHeight;
        internal CaptionFont SmallCaptionFont;
        internal int MenuWidth, MenuHeight;
        internal CaptionFont MenuFont, StatusFont, MessageFont;
        internal int PaddedBorderWidth;
    }

    [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int SystemParametersInfoForDpi(uint action, uint parameter, ref CaptionMetrics metrics, uint flags, uint dpi);
    [LibraryImport("dwmapi.dll", EntryPoint = "DwmGetWindowAttribute"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int GetCaptionButtonBounds(nint window, int attribute, out CaptionRect bounds, uint size);
    [LibraryImport("user32.dll", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint GetDC(nint window);
    [LibraryImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int ReleaseDC(nint window, nint dc);
    [LibraryImport("gdi32.dll", EntryPoint = "CreateFontIndirectW", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint CreateFontIndirect(ref CaptionFont font);
    [LibraryImport("gdi32.dll", EntryPoint = "SelectObject", SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial nint SelectCaptionObject(nint dc, nint value);
    [LibraryImport("user32.dll", EntryPoint = "DrawTextW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static partial int DrawCaptionText(nint dc, string title, int length, ref CaptionRect rect, uint flags);
}
