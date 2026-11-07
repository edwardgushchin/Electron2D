using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Electron2D;

internal sealed unsafe partial class X11ColorSampler : SafeHandleZeroOrMinusOneIsInvalid, IDesktopColorSampler
{
    private const string XCB = "libxcb.so.1";
    private const ushort ButtonEvents = (1 << 2) | (1 << 3);
    private const byte ButtonPress = 4, ButtonRelease = 5, Async = 1;
    private bool _grabbed, _leftDown;

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct ButtonEvent
    {
        [FieldOffset(0)] internal byte Type;
        [FieldOffset(1)] internal byte Button;
        [FieldOffset(20)] internal short RootX;
        [FieldOffset(22)] internal short RootY;
        [FieldOffset(30)] internal byte SameScreen;
    }

    [StructLayout(LayoutKind.Explicit, Size = 32)]
    private struct GrabReply
    {
        [FieldOffset(1)] internal byte Status;
    }

    private X11ColorSampler(nint connection) : base(ownsHandle: true) => SetHandle(connection);

    internal static X11ColorSampler Open(nint displayName, uint root)
    {
        // Own the modal grab and its events on a separate connection so SDL's input state stays intact.
        var sampler = new X11ColorSampler(Connect(displayName, 0));
        try
        {
            if (sampler.IsInvalid || ConnectionHasError(sampler.handle) != 0)
                throw new IOException("Could not connect to the X11 desktop for color sampling.");

            var cookie = GrabPointer(sampler.handle, 0, root, ButtonEvents, Async, Async, 0, 0, 0);
            var reply = GrabPointerReply(sampler.handle, cookie, out var error);
            try
            {
                if (reply == null || error != null || reply->Status != 0)
                    throw new IOException("Could not acquire the X11 pointer for color sampling.");
                sampler._grabbed = true;
                return sampler;
            }
            finally
            {
                NativeMemory.Free(reply);
                NativeMemory.Free(error);
            }
        }
        catch
        {
            sampler.Dispose();
            throw;
        }
    }

    public bool TryReadSelection(out Vector2i point, out bool accepted)
    {
        if (ConnectionHasError(handle) != 0)
            throw new IOException("The X11 color sampling connection closed.");
        for (var input = PollForEvent(handle); input != null; input = PollForEvent(handle))
        {
            try
            {
                if (input->SameScreen == 0) continue;
                if (input->Type == ButtonPress && input->Button == 1) _leftDown = true;
                if (input->Type != ButtonRelease || !(input->Button == 3 || input->Button == 1 && _leftDown)) continue;
                point = new(input->RootX, input->RootY);
                accepted = input->Button == 1;
                return true;
            }
            finally { NativeMemory.Free(input); }
        }
        point = default;
        accepted = false;
        return false;
    }

    protected override bool ReleaseHandle()
    {
        if (_grabbed && ConnectionHasError(handle) == 0)
            NativeMemory.Free(RequestCheck(handle, UngrabPointer(handle, 0)));
        Disconnect(handle);
        return true;
    }

    [LibraryImport(XCB, EntryPoint = "xcb_connect")]
    private static partial nint Connect(nint displayName, nint screen);
    [LibraryImport(XCB, EntryPoint = "xcb_connection_has_error")]
    private static partial int ConnectionHasError(nint connection);
    [LibraryImport(XCB, EntryPoint = "xcb_disconnect")]
    private static partial void Disconnect(nint connection);
    [LibraryImport(XCB, EntryPoint = "xcb_grab_pointer")]
    private static partial uint GrabPointer(nint connection, byte ownerEvents, uint window, ushort eventMask,
        byte pointerMode, byte keyboardMode, uint confineTo, uint cursor, uint time);
    [LibraryImport(XCB, EntryPoint = "xcb_grab_pointer_reply")]
    private static partial GrabReply* GrabPointerReply(nint connection, uint cookie, out byte* error);
    [LibraryImport(XCB, EntryPoint = "xcb_ungrab_pointer_checked")]
    private static partial uint UngrabPointer(nint connection, uint time);
    [LibraryImport(XCB, EntryPoint = "xcb_request_check")]
    private static partial byte* RequestCheck(nint connection, uint cookie);
    [LibraryImport(XCB, EntryPoint = "xcb_poll_for_event")]
    private static partial ButtonEvent* PollForEvent(nint connection);
}
