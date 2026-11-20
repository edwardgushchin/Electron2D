using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Electron2D;

internal sealed unsafe partial class WaylandInput
{
    private readonly Dictionary<uint, DensityOutput> _outputs = new();

    internal int ReadOutputDPI(nint output)
    {
        if (output == 0)
            return 0;

        uint global;
        try
        {
            global = GetOutputGlobal(output, GetProxyUserData(output));
        }
        catch (EntryPointNotFoundException)
        {
            return 0;
        }

        DispatchPending();
        return _outputs.TryGetValue(global, out var geometry) ? geometry.DPI : 0;
    }

    private void AddOutput(uint id, uint version)
    {
        var outputInterface = NativeLibrary.GetExport(_library, "wl_output_interface");
        var handle = RequireHandle(BindGlobal(id, outputInterface, Math.Min(version, 3u)));
        var output = new DensityOutput(handle);
        try
        {
            output.Listen();
            _outputs.Add(id, output);
        }
        catch
        {
            output.Dispose();
            throw;
        }
    }

    private sealed class DensityOutput(nint handle) : IDisposable
    {
        private GCHandle _listener;
        private int _width, _height, _widthMM, _heightMM;

        internal int DPI { get; private set; }

        internal void Listen()
        {
            _listener = GCHandle.Alloc(this);
            WaylandInput.Listen(handle, &OnEvent, _listener);
        }

        public void Dispose()
        {
            ReleaseProxy(handle, 0, 3);
            if (_listener.IsAllocated)
                _listener.Free();
        }

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int OnEvent(nint context, nint proxy, uint opcode, nint message, Argument* arguments)
        {
            var output = (DensityOutput)GCHandle.FromIntPtr(context).Target!;
            switch (opcode)
            {
                case 0: // geometry
                    output._widthMM = arguments[2].Int;
                    output._heightMM = arguments[3].Int;
                    break;
                case 1 when (arguments[0].UInt & 1) != 0: // current mode
                    output._width = arguments[1].Int;
                    output._height = arguments[2].Int;
                    break;
            }

            // Both mode and physical geometry use the panel's untransformed axes.
            if (opcode == 2 || ProxyVersion(proxy) == 1)
                output.DPI = DisplayServer.PhysicalScreenDPI(output._width, output._height, output._widthMM, output._heightMM);
            return 0;
        }
    }

    [LibraryImport(ClientLibrary, EntryPoint = "wl_proxy_get_user_data")]
    private static partial nint GetProxyUserData(nint output);

    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_wayland_output_global")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial uint GetOutputGlobal(nint output, nint data);
}
