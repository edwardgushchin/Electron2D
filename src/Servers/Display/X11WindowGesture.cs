using System.Runtime.InteropServices;

namespace Electron2D;

internal readonly partial struct X11WindowGesture(nint display, nuint window, nuint root, nuint atom)
{
    private const string X11 = "libX11.so.6";
    private const uint Button1Mask = 1 << 8, PointerButtons = 0x1f << 8;
    private const int ButtonRelease = 5, ClientMessage = 33;
    private const int SubstructureEvents = (1 << 19) | (1 << 20);
    private const nuint AtomType = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct ClientEvent
    {
        internal int Type;
        internal nuint Serial;
        internal int SendEvent;
        internal nint Display;
        internal nuint Window, MessageType;
        internal int Format;
        internal nint X, Y, Direction, Button, Source;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ButtonEvent
    {
        internal int Type;
        internal nuint Serial;
        internal int SendEvent;
        internal nint Display;
        internal nuint Window, Root, Child, Time;
        internal int X, Y, RootX, RootY;
        internal uint State, Button;
        internal int SameScreen;
    }

    internal static X11WindowGesture? Start(nint display, nuint window, DisplayServer.WindowResizeEdge? edge)
    {
        if (display == 0 || window == 0)
            throw new NotSupportedException("The window has no X11 connection or native identity.");
        if (XQueryPointer(display, window, out var root, out _, out var x, out var y, out _, out _, out var buttons) == 0 ||
            (buttons & PointerButtons) != Button1Mask)
            return null;

        var atom = XInternAtom(display, "_NET_WM_MOVERESIZE", 1);
        if (atom == 0 || !Supports(display, root, atom))
            throw new NotSupportedException("The X11 window manager does not advertise interactive move and resize.");
        var gesture = new X11WindowGesture(display, window, root, atom);
        var direction = edge switch
        {
            DisplayServer.WindowResizeEdge.TopLeft => 0,
            DisplayServer.WindowResizeEdge.Top => 1,
            DisplayServer.WindowResizeEdge.TopRight => 2,
            DisplayServer.WindowResizeEdge.Right => 3,
            DisplayServer.WindowResizeEdge.BottomRight => 4,
            DisplayServer.WindowResizeEdge.Bottom => 5,
            DisplayServer.WindowResizeEdge.BottomLeft => 6,
            DisplayServer.WindowResizeEdge.Left => 7,
            _ => 8,
        };

        XUngrabPointer(display, 0);
        if (!gesture.Send(x, y, direction))
            throw new InvalidOperationException("X11 could not send the interactive window gesture request.");

        // Order the native request before input can arrive from another X11 client.
        XSync(display, 0);
        return gesture;
    }

    internal unsafe bool TryFinish(bool missingRelease, bool cancel)
    {
        var sameScreen = XQueryPointer(display, window, out _, out _, out var x, out var y, out var localX,
            out var localY, out var buttons);
        if ((buttons & Button1Mask) != 0)
            return false;

        if (cancel)
            Cancel();
        if (missingRelease)
        {
            // The manager may consume the hardware release. Feed SDL's own X11 queue so its
            // button state and automatic capture reset together. Wait for the physical release:
            // Openbox can replay the original press while it acquires the grab.
            var buffer = stackalloc nint[24];
            *(ButtonEvent*)buffer = new()
            {
                Type = ButtonRelease,
                Serial = XNextRequest(display),
                Display = display,
                Window = window,
                Root = root,
                X = localX,
                Y = localY,
                RootX = x,
                RootY = y,
                State = buttons | Button1Mask,
                Button = 1,
                SameScreen = sameScreen,
            };
            XPutBackEvent(display, buffer);
        }
        return true;
    }

    // A button released before the manager acquires the pointer may otherwise leave the gesture active.
    internal void Cancel() => Send(0, 0, 11);

    private unsafe bool Send(int x, int y, int direction)
    {
        // XEvent is a union of 24 native longs. The server reads five longs from the client-message variant.
        var buffer = stackalloc nint[24];
        *(ClientEvent*)buffer = new()
        {
            Type = ClientMessage,
            Display = display,
            Window = window,
            MessageType = atom,
            Format = 32,
            X = x,
            Y = y,
            Direction = direction,
            Button = 1,
            Source = 1,
        };
        var sent = XSendEvent(display, root, 0, SubstructureEvents, buffer) != 0;
        XFlush(display);
        return sent;
    }

    private static unsafe bool Supports(nint display, nuint root, nuint operation)
    {
        var property = XInternAtom(display, "_NET_SUPPORTED", 1);
        if (property == 0)
            return false;
        var status = XGetWindowProperty(display, root, property, 0, 4096, 0, AtomType,
            out var type, out var format, out var count, out _, out var atoms);
        try
        {
            if (status != 0 || type != AtomType || format != 32 || atoms == null)
                return false;
            for (nuint i = 0; i < count; i++)
                if (atoms[i] == operation)
                    return true;
            return false;
        }
        finally { if (atoms != null) XFree(atoms); }
    }

    [LibraryImport(X11, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nuint XInternAtom(nint display, string name, int onlyIfExists);
    [LibraryImport(X11)]
    private static unsafe partial int XGetWindowProperty(nint display, nuint window, nuint property, nint offset,
        nint length, int delete, nuint requestedType, out nuint actualType, out int format, out nuint count,
        out nuint bytesAfter, out nuint* data);
    [LibraryImport(X11)]
    private static unsafe partial int XFree(void* data);
    [LibraryImport(X11)]
    private static partial int XQueryPointer(nint display, nuint window, out nuint root, out nuint child, out int rootX,
        out int rootY, out int windowX, out int windowY, out uint mask);
    [LibraryImport(X11)]
    private static partial int XUngrabPointer(nint display, nuint time);
    [LibraryImport(X11)]
    private static unsafe partial int XSendEvent(nint display, nuint window, int propagate, nint eventMask, nint* value);
    [LibraryImport(X11)]
    private static partial int XFlush(nint display);
    [LibraryImport(X11)]
    private static partial int XSync(nint display, int discard);
    [LibraryImport(X11)]
    private static partial nuint XNextRequest(nint display);
    [LibraryImport(X11)]
    private static unsafe partial int XPutBackEvent(nint display, nint* value);
}
