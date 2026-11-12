using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe class D3D12CanvasResource
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Description
    {
        internal uint Dimension;
        internal ulong Alignment, Width;
        internal uint Height;
        internal ushort DepthOrArraySize, MipLevels;
        internal uint Format, Samples, Quality, Layout, Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HeapProperties
    {
        internal uint Type, CPUPageProperty, MemoryPoolPreference, CreationNodeMask, VisibleNodeMask;
    }

    private static readonly Guid DeviceID = new("189819f1-1db6-4b57-be54-1821339b85f7");
    private static readonly Guid ResourceID = new("696442be-a72e-4059-bc79-5b5c98040fad");

    internal static bool Validate(nint native, nint rendererDevice, Vector2i size)
    {
        if (!Environment.Is64BitProcess)
            throw new NotSupportedException("Direct3D 12 texture import requires a 64-bit Windows process.");
        if (Marshal.QueryInterface(native, in ResourceID, out var typed) < 0)
            throw new ArgumentException("A live ID3D12Resource pointer is required.", "nativeHandle");
        Marshal.Release(typed);
        if (typed != native)
            throw new ArgumentException("Pass the ID3D12Resource interface pointer.", "nativeHandle");

        var methods = *(nint**)native;
        nint device;
        var deviceID = DeviceID;
        // ID3D12DeviceChild::GetDevice returns a retained reference.
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Guid*, nint*, int>)methods[7])(native, &deviceID, &device));
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(rendererDevice, in DeviceID, out var expected));
            try
            {
                if (device != expected) throw new ArgumentException("The texture belongs to another Direct3D 12 device.", "nativeHandle");
            }
            finally { Marshal.Release(expected); }
        }
        finally { Marshal.Release(device); }

        // The Windows C ABI returns D3D12_RESOURCE_DESC through an explicit caller buffer.
        Description description;
        _ = ((delegate* unmanaged[Stdcall]<nint, Description*, nint>)methods[10])(native, &description);
        if (description.Dimension != 3 || description.DepthOrArraySize != 1 || description.MipLevels != 1 ||
            description.Samples != 1 || description.Format is not (28 or 87) || description.Layout != 0 || (description.Flags & 8) != 0)
            throw new NotSupportedException("Import requires a single-mip, single-layer, single-sample RGBA8 or BGRA8 UNORM shader resource.");
        if (description.Width != (ulong)size.X || description.Height != (uint)size.Y)
            throw new ArgumentException("The declared dimensions differ from the native texture.", "nativeHandle");

        HeapProperties heap;
        uint flags;
        // ID3D12Resource::GetHeapProperties. A reserved resource has no usable heap properties.
        if (((delegate* unmanaged[Stdcall]<nint, HeapProperties*, uint*, int>)methods[14])(native, &heap, &flags) < 0 ||
            heap.Type != 1 || (flags & (1u | 32u)) != 0)
            throw new NotSupportedException("Import requires a non-shared Direct3D 12 default heap.");
        return description.Format == 87;
    }
}
