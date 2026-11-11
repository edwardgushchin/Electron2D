using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class WaylandInput
{
    private uint _systemBellGlobal;
    private nint _systemBell;

    private static class BellProtocol
    {
        internal const string Name = "xdg_system_bell_v1";
        internal static readonly ProtocolInterface Interface = new(Name, [("destroy", ""), ("ring", "?o")]);
    }

    internal void RingSystemBell()
    {
        DispatchPending();
        if (_systemBellGlobal == 0)
            return;

        if (_systemBell == 0)
        {
            BellProtocol.Interface.SetType(1, 0, NativeLibrary.GetExport(_library, "wl_surface_interface"));
            _systemBell = RequireHandle(BindGlobal(_systemBellGlobal, BellProtocol.Interface.Handle, 1));
        }

        // A null surface requests the desktop bell without associating it with a window.
        Argument surface = default;
        MarshalRequest(_systemBell, 1, 0, 1, 0, &surface);
        if (Flush(_display) < 0 && Marshal.GetLastPInvokeError() != TryAgain)
            throw new InvalidOperationException("Could not submit the Wayland system bell.");
    }
}
