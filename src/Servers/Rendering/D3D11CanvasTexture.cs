using System.Runtime.InteropServices;

namespace Electron2D;

internal static unsafe class D3D11CanvasTexture
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Description
    {
        internal uint Width, Height, Mips, Layers, Format, Samples, Quality, Usage, Bind, CPUAccess, Misc;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Mapping
    {
        internal byte* Data;
        internal uint RowPitch, DepthPitch;
    }

    private static readonly Guid DeviceID = new("db6f6ddb-ac77-4e88-8253-819df9bbf140");
    private static readonly Guid TextureID = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
    internal static bool Validate(nint native, nint rendererDevice, Vector2i size)
    {
        if (Marshal.QueryInterface(native, in TextureID, out var typed) < 0)
            throw new ArgumentException("A live ID3D11Texture2D pointer is required.", "nativeHandle");
        Marshal.Release(typed);
        if (typed != native)
            throw new ArgumentException("Pass the ID3D11Texture2D interface pointer.", "nativeHandle");

        var device = GetDevice(native);
        try
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface(rendererDevice, in DeviceID, out var expected));
            try
            {
                if (device != expected) throw new ArgumentException("The texture belongs to another Direct3D 11 device.", "nativeHandle");
            }
            finally { Marshal.Release(expected); }
        }
        finally { Marshal.Release(device); }

        var description = GetDescription(native);
        if (description.Width != size.X || description.Height != size.Y)
            throw new ArgumentException("The declared dimensions differ from the native texture.", "nativeHandle");
        if (description.Mips != 1 || description.Layers != 1 || description.Samples != 1 ||
            description.Usage != 0 || (description.Bind & 8) == 0 || description.Misc != 0 ||
            description.Format is not (28 or 87))
            throw new NotSupportedException("Import requires a single-mip, single-layer, single-sample RGBA8 or BGRA8 UNORM shader resource with default usage and no miscellaneous flags.");

        return description.Format == 87;
    }

    private static Description GetDescription(nint native)
    {
        // ID3D11Texture2D::GetDesc.
        Description description;
        ((delegate* unmanaged[Stdcall]<nint, Description*, void>)Methods(native)[10])(native, &description);
        return description;
    }

    private static nint* Methods(nint value) => *(nint**)value;
    private static nint GetDevice(nint texture)
    {
        nint device;
        // ID3D11DeviceChild::GetDevice returns a retained reference.
        ((delegate* unmanaged[Stdcall]<nint, nint*, void>)Methods(texture)[3])(texture, &device);
        return device;
    }

    internal static Image Read(NativeCanvasTexture texture)
    {
        texture.Backend.FlushImportedTexture();
        var device = GetDevice(texture.Native);
        try
        {
            var description = GetDescription(texture.Native);
            description.Usage = 3; // D3D11_USAGE_STAGING
            description.Bind = 0;
            description.CPUAccess = 0x20000; // D3D11_CPU_ACCESS_READ
            nint staging, context;
            // ID3D11Device::CreateTexture2D and GetImmediateContext.
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, Description*, nint, nint*, int>)Methods(device)[5])
                (device, &description, 0, &staging));
            try
            {
                ((delegate* unmanaged[Stdcall]<nint, nint*, void>)Methods(device)[40])(device, &context);
                try
                {
                    // ID3D11DeviceContext::CopyResource, Map and Unmap.
                    var methods = Methods(context);
                    ((delegate* unmanaged[Stdcall]<nint, nint, nint, void>)methods[47])(context, staging, texture.Native);
                    Mapping mapping;
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, uint, uint, uint, Mapping*, int>)methods[14])
                        (context, staging, 0, 1, 0, &mapping));
                    try
                    {
                        var size = texture.Size;
                        var pitch = checked(size.X * 4);
                        var pixels = new byte[checked(pitch * size.Y)];
                        for (var row = 0; row < size.Y; row++)
                            new ReadOnlySpan<byte>(mapping.Data + row * (long)mapping.RowPitch, pitch).CopyTo(pixels.AsSpan(row * pitch, pitch));
                        texture.SwapChannels(pixels);
                        return Image.CreateFromData(size.X, size.Y, false, Image.Format.Rgba8, pixels);
                    }
                    finally { ((delegate* unmanaged[Stdcall]<nint, nint, uint, void>)methods[15])(context, staging, 0); }
                }
                finally { Marshal.Release(context); }
            }
            finally { Marshal.Release(staging); }
        }
        finally { Marshal.Release(device); }
    }

}
