using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Edge = Electron2D.DisplayServer.WindowResizeEdge;

namespace Electron2D;

internal sealed unsafe partial class WaylandWindowGestures : IDisposable
{
    private const string ClientLibrary = "libwayland-client.so.0";
    private const uint DestroyProxy = 1;
    private const uint LeftButton = 0x110;
    private const int TryAgain = 11;

    private readonly nint _display;
    private readonly nint _library;
    private readonly Dictionary<uint, Seat> _seats = new();
    private GCHandle _listener;
    private nint _queue;
    private nint _registry;
    private Exception? _listenerFailure;

    private WaylandWindowGestures(nint display, nint library)
    {
        _display = display;
        _library = library;
    }

    internal static WaylandWindowGestures? Open(nint display)
    {
        if (display == 0 || !NativeLibrary.TryLoad(ClientLibrary, out var library))
            return null;
        if (!NativeLibrary.TryGetExport(library, "wl_proxy_marshal_array_flags", out _))
        {
            NativeLibrary.Free(library);
            return null;
        }

        var source = new WaylandWindowGestures(display, library);
        try
        {
            source.Initialize();
            return source;
        }
        catch
        {
            source.Dispose();
            throw;
        }
    }

    private void Initialize()
    {
        _queue = RequireHandle(CreateQueue(_display));
        var wrapper = RequireHandle(CreateWrapper(_display));
        try
        {
            // Set the queue before the registry can receive events on SDL's connection.
            SetQueue(wrapper, _queue);
            Argument newID = default;
            _registry = RequireHandle(MarshalRequest(wrapper, 1,
                NativeLibrary.GetExport(_library, "wl_registry_interface"), ProxyVersion(wrapper), 0, &newID));
        }
        finally { DestroyWrapper(wrapper); }
        _listener = GCHandle.Alloc(this);
        Listen(_registry, &RegistryEvent, _listener);

        // Read the initial registry, then the capabilities of any seats it announced.
        for (var pass = 0; pass < 2; pass++)
            CheckDispatch(Roundtrip(_display, _queue));
    }

    internal void DispatchPending() => CheckDispatch(DispatchQueuePending(_display, _queue));

    internal bool TryStart(nint surface, nint toplevel, Edge? edge)
    {
        if (surface == 0 || toplevel == 0)
            return false;

        DispatchPending();
        Seat? source = null;
        foreach (var seat in _seats.Values)
        {
            if (seat.Focus != surface || !seat.Eligible)
                continue;
            if (source is not null)
                return false;
            source = seat;
        }
        if (source is null)
            return false;

        Argument* arguments = stackalloc Argument[3];
        arguments[0] = new Argument { Object = source.Handle };
        arguments[1] = new Argument { UInt = source.Serial };
        arguments[2] = new Argument
        {
            UInt = edge switch
            {
                Edge.TopLeft => 5,
                Edge.Top => 1,
                Edge.TopRight => 9,
                Edge.Left => 4,
                Edge.Right => 8,
                Edge.BottomLeft => 6,
                Edge.Bottom => 2,
                Edge.BottomRight => 10,
                _ => 0,
            },
        };
        // xdg_toplevel.move and resize use the press serial on the same Wayland seat.
        MarshalRequest(toplevel, edge is null ? 5u : 6u, 0, ProxyVersion(toplevel), 0, arguments);
        source.Eligible = false;
        if (Flush(_display) < 0 && Marshal.GetLastPInvokeError() != TryAgain)
            throw new InvalidOperationException("Could not submit the Wayland window gesture.");
        return true;
    }

    internal void Forget(nint surface)
    {
        foreach (var seat in _seats.Values)
            if (seat.Focus == surface)
                seat.ResetFocus(0);
    }

    public void Dispose()
    {
        foreach (var seat in _seats.Values)
            seat.Dispose();
        _seats.Clear();
        if (_registry != 0) DestroyProxyLocal(_registry);
        if (_listener.IsAllocated) _listener.Free();
        if (_queue != 0) DestroyQueue(_queue);
        NativeLibrary.Free(_library);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int RegistryEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
    {
        var owner = (WaylandWindowGestures)GCHandle.FromIntPtr(context).Target!;
        try
        {
            var id = arguments[0].UInt;
            if (opcode == 0 && Marshal.PtrToStringUTF8(arguments[1].Object) == "wl_seat")
                owner.AddSeat(id, arguments[2].UInt);
            else if (opcode == 1 && owner._seats.Remove(id, out var seat))
                seat.Dispose();
        }
        catch (Exception error) { owner._listenerFailure ??= error; }
        return 0;
    }

    private void AddSeat(uint id, uint version)
    {
        var seatInterface = NativeLibrary.GetExport(_library, "wl_seat_interface");
        var bindVersion = Math.Min(version, 5u);
        Argument* arguments = stackalloc Argument[4];
        arguments[0] = new Argument { UInt = id };
        arguments[1] = new Argument { Object = *(nint*)seatInterface };
        arguments[2] = new Argument { UInt = bindVersion };
        arguments[3] = default;
        var handle = RequireHandle(MarshalRequest(_registry, 0, seatInterface, bindVersion, 0, arguments));
        var seat = new Seat(this, handle);
        try
        {
            seat.Listen();
            _seats.Add(id, seat);
        }
        catch
        {
            seat.Dispose();
            throw;
        }
    }

    private sealed class Seat(WaylandWindowGestures owner, nint handle) : IDisposable
    {
        private readonly WaylandWindowGestures _owner = owner;
        private GCHandle _listener;
        private nint _pointer;
        private int _buttons;

        internal readonly nint Handle = handle;
        internal nint Focus;
        internal bool Eligible;
        internal uint Serial;

        internal void Listen()
        {
            _listener = GCHandle.Alloc(this);
            WaylandWindowGestures.Listen(Handle, &SeatEvent, _listener);
        }

        private void UpdatePointer(bool available)
        {
            if (available && _pointer == 0)
            {
                Argument newID = default;
                _pointer = RequireHandle(MarshalRequest(Handle, 0, NativeLibrary.GetExport(_owner._library,
                    "wl_pointer_interface"), ProxyVersion(Handle), 0, &newID));
                try { WaylandWindowGestures.Listen(_pointer, &PointerEvent, _listener); }
                catch { ReleasePointer(); throw; }
            }
            else if (!available)
                ReleasePointer();
        }

        private void ReleasePointer()
        {
            if (_pointer == 0)
                return;
            ReleaseProxy(_pointer, opcode: 1, minimumVersion: 3);
            _pointer = 0;
            ResetFocus(0);
        }

        public void Dispose()
        {
            ReleasePointer();
            ReleaseProxy(Handle, opcode: 3, minimumVersion: 5);
            if (_listener.IsAllocated) _listener.Free();
        }

        internal void ResetFocus(nint focus)
        {
            Focus = focus;
            _buttons = 0;
            Eligible = false;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int SeatEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
        {
            if (opcode == 0)
            {
                var seat = (Seat)GCHandle.FromIntPtr(context).Target!;
                try { seat.UpdatePointer((arguments[0].UInt & 1) != 0); }
                catch (Exception error) { seat._owner._listenerFailure ??= error; }
            }
            return 0;
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int PointerEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
        {
            var seat = (Seat)GCHandle.FromIntPtr(context).Target!;
            switch (opcode)
            {
                case 0: // enter
                    seat.ResetFocus(arguments[1].Object);
                    break;
                case 1: // leave
                    seat.ResetFocus(0);
                    break;
                case 3: // button
                    if (arguments[3].UInt == 1)
                    {
                        seat.Eligible = seat._buttons++ == 0 && arguments[2].UInt == LeftButton;
                        seat.Serial = arguments[0].UInt;
                    }
                    else
                    {
                        seat._buttons = Math.Max(0, seat._buttons - 1);
                        seat.Eligible = false;
                    }
                    break;
            }
            return 0;
        }
    }

    private static nint RequireHandle(nint handle) => handle != 0 ? handle :
        throw new InvalidOperationException("Could not create a Wayland window gesture observer.");

    private void CheckDispatch(int result)
    {
        if (_listenerFailure is { } failure)
        {
            _listenerFailure = null;
            throw new InvalidOperationException("Could not observe Wayland pointer input.", failure);
        }
        if (result < 0)
            throw new InvalidOperationException("Could not dispatch Wayland pointer input.");
    }

    private static void Listen(nint proxy, delegate* unmanaged[Cdecl]<nint, nint, uint, nint, Argument*, int> listener,
        GCHandle context)
    {
        if (AddDispatcher(proxy, listener, GCHandle.ToIntPtr(context), 0) != 0)
            throw new InvalidOperationException("Could not subscribe to Wayland pointer input.");
    }

    private static void ReleaseProxy(nint proxy, uint opcode, uint minimumVersion)
    {
        var version = ProxyVersion(proxy);
        if (version >= minimumVersion)
            MarshalRequest(proxy, opcode, 0, version, DestroyProxy, null);
        else
            DestroyProxyLocal(proxy);
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Argument
    {
        [FieldOffset(0)] internal uint UInt;
        [FieldOffset(0)] internal nint Object;
    }

    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_create_queue")]
    private static partial nint CreateQueue(nint display);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_event_queue_destroy")]
    private static partial void DestroyQueue(nint queue);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_create_wrapper")]
    private static partial nint CreateWrapper(nint proxy);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_wrapper_destroy")]
    private static partial void DestroyWrapper(nint wrapper);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_set_queue")]
    private static partial void SetQueue(nint proxy, nint queue);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_get_version")]
    private static partial uint ProxyVersion(nint proxy);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_destroy")]
    private static partial void DestroyProxyLocal(nint proxy);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_marshal_array_flags")]
    private static partial nint MarshalRequest(nint proxy, uint opcode, nint @interface, uint version, uint flags, Argument* arguments);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_add_dispatcher")]
    private static partial int AddDispatcher(nint proxy, delegate* unmanaged[Cdecl]<nint, nint, uint, nint, Argument*, int> listener,
        nint implementation, nint data);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_roundtrip_queue")]
    private static partial int Roundtrip(nint display, nint queue);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_dispatch_queue_pending")]
    private static partial int DispatchQueuePending(nint display, nint queue);
    [LibraryImport(ClientLibrary, EntryPoint = "wl_display_flush", SetLastError = true)]
    private static partial int Flush(nint display);
}
