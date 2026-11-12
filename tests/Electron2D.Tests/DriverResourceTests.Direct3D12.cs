using System.Runtime.InteropServices;
using Electron2D;
using DriverResource = Electron2D.RenderingDevice.DriverResource;

internal static unsafe partial class DriverResourceTests
{
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct Direct3DResources(ulong Factory, ulong Adapter, ulong Device, ulong Queue,
        ulong Image, ulong View, ulong Sampler, ulong Buffer, ulong Render, ulong Compute);

    private static void VerifyDirect3DHandles(RenderingDevice device, ulong image, ulong view, ulong sampler,
        RID buffer, ulong render, ulong compute)
    {
        var library = LoadDirect3DProbe();
        try
        {
            var resources = new Direct3DResources(
                device.GetDriverResource(DriverResource.TopmostObject, default, 0),
                device.GetDriverResource(DriverResource.PhysicalDevice, default, 0),
                device.GetDriverResource(DriverResource.LogicalDevice, default, 0),
                device.GetDriverResource(DriverResource.CommandQueue, default, 0),
                image, view, sampler, device.GetDriverResource(DriverResource.Buffer, buffer, 0), render, compute);
            var verify = (delegate* unmanaged[Cdecl]<Direct3DResources*, int>)NativeLibrary.GetExport(library, "VerifyDriverResources");
            var result = verify(&resources);
            Check(result == 0, $"Independent Direct3D 12 device, resource, descriptor and queue checks returned {result}.");
        }
        finally { NativeLibrary.Free(library); }
    }

    internal static void InspectDirect3DTexture(ulong handle, uint width, uint height, ushort layers)
    {
        var library = LoadDirect3DProbe();
        try
        {
            var inspect = (delegate* unmanaged[Cdecl]<ulong, uint, uint, ushort, int>)NativeLibrary.GetExport(library, "InspectTexture");
            Check(inspect(handle, width, height, layers) == 0, "Direct3D 12 reports the expected texture description.");
        }
        finally { NativeLibrary.Free(library); }
    }

    private static nint LoadDirect3DProbe() => NativeLibrary.Load(
        Environment.GetEnvironmentVariable("ELECTRON2D_D3D12_DRIVER_FIXTURE") ??
        throw new InvalidOperationException("Set ELECTRON2D_D3D12_DRIVER_FIXTURE."));
}
