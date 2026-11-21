using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private const string CaptionX11 = "libX11.so.6";
    private const nuint MotifFunctions = 1, MotifDecorations = 2, MotifAllFunctions = 1;
    private const nuint MotifMinimize = 8, MotifMaximize = 16;
    private const nuint MotifStandardFunctions = 2 | 4 | MotifMinimize | MotifMaximize | 32;
    private const int MotifHintLength = 5;
    private nuint _x11DisabledCaptionButtons;
    private nuint _x11OriginalCaptionFunctions;
    private bool _x11HadFunctionHint;
    internal bool UsesX11CaptionButtons => _videoDriver == "x11" && !IsNativePopup;

    private static nuint X11CaptionFunction(WindowFlag flag) =>
        flag == WindowFlag.MinimizeDisabled ? MotifMinimize : MotifMaximize;

    private unsafe void SetX11CaptionButtonDisabled(WindowFlag flag, bool disabled)
    {
        var display = WindowGetNativeHandleCore(HandleType.DisplayHandle);
        var window = (nuint)WindowGetNativeHandleCore(HandleType.WindowHandle);
        var atom = InternCaptionAtom(display, "_MOTIF_WM_HINTS", 0);
        var hints = stackalloc nuint[MotifHintLength];
        ReadMotifHints(display, window, atom, hints);

        var function = X11CaptionFunction(flag);
        if (_x11DisabledCaptionButtons == 0)
        {
            _x11HadFunctionHint = (hints[0] & MotifFunctions) != 0;
            _x11OriginalCaptionFunctions = hints[1];
        }
        var functions = (hints[0] & MotifFunctions) != 0 ? hints[1] : MotifAllFunctions;
        var mask = disabled ? _x11DisabledCaptionButtons | function : _x11DisabledCaptionButtons & ~function;

        // Normalize the Motif ALL-with-exceptions form before masking individual functions.
        var originalAllowed = _x11HadFunctionHint
            ? AllowedMotifFunctions(_x11OriginalCaptionFunctions)
            : MotifStandardFunctions;
        var allowed = AllowedMotifFunctions(functions) & ~mask;
        if (!disabled)
            allowed |= originalAllowed & function;
        hints[1] = allowed;
        hints[0] |= MotifFunctions;
        // Keep the function field valid on reset. Xfwm4 applies explicit functions
        // to an existing frame but does not restore them when the field becomes invalid.
        WriteMotifHints(display, window, atom, hints);
        _x11DisabledCaptionButtons = mask;
    }

    private static nuint AllowedMotifFunctions(nuint functions) =>
        (functions & MotifAllFunctions) != 0 ? MotifStandardFunctions & ~functions : functions;

    private unsafe bool SetWindowBordered(nint window, bool bordered)
    {
        if (_videoDriver != "x11" || IsNativePopup)
            return SDL.SetWindowBordered(window, bordered);

        var display = WindowGetNativeHandleCore(HandleType.DisplayHandle);
        var xid = (nuint)WindowGetNativeHandleCore(HandleType.WindowHandle);
        var atom = InternCaptionAtom(display, "_MOTIF_WM_HINTS", 0);
        var hints = stackalloc nuint[MotifHintLength];
        ReadMotifHints(display, xid, atom, hints);
        if (!SDL.SetWindowBordered(window, bordered))
            return false;

        // SDL replaces the Motif property when toggling borders.
        hints[0] |= MotifDecorations;
        hints[2] = bordered ? 1u : 0u;
        WriteMotifHints(display, xid, atom, hints);
        return true;
    }

    private static unsafe void ReadMotifHints(nint display, nuint window, nuint atom, nuint* hints)
    {
        var status = GetCaptionProperty(display, window, atom, 0, MotifHintLength, 0, atom,
            out var type, out var format, out var count, out var remaining, out var data);
        try
        {
            if (status != 0 || type != 0 &&
                (type != atom || format != 32 || count != MotifHintLength || remaining != 0))
                throw new IOException("Cannot read the X11 window's Motif hints.");

            for (var index = 0; index < MotifHintLength; index++)
                hints[index] = type == 0 ? 0 : data[index];
        }
        finally
        {
            if (data != null)
                FreeCaptionProperty(data);
        }
    }

    private static unsafe void WriteMotifHints(nint display, nuint window, nuint atom, nuint* hints)
    {
        // Xlib expects native unsigned longs even though each wire value is 32 bits.
        ChangeCaptionProperty(display, window, atom, atom, 32, 0, hints, MotifHintLength);
        FlushCaptionProperties(display);
    }

    [LibraryImport(CaptionX11, EntryPoint = "XInternAtom", StringMarshalling = StringMarshalling.Utf8)]
    private static partial nuint InternCaptionAtom(nint display, string name, int onlyIfExists);

    [LibraryImport(CaptionX11, EntryPoint = "XGetWindowProperty")]
    private static unsafe partial int GetCaptionProperty(nint display, nuint window, nuint property, nint offset,
        nint length, int delete, nuint requestedType, out nuint actualType, out int format, out nuint count,
        out nuint bytesAfter, out nuint* data);

    [LibraryImport(CaptionX11, EntryPoint = "XChangeProperty")]
    private static unsafe partial int ChangeCaptionProperty(nint display, nuint window, nuint property, nuint type,
        int format, int mode, nuint* data, int count);

    [LibraryImport(CaptionX11, EntryPoint = "XFree")]
    private static unsafe partial int FreeCaptionProperty(void* data);

    [LibraryImport(CaptionX11, EntryPoint = "XFlush")]
    private static partial int FlushCaptionProperties(nint display);
}
