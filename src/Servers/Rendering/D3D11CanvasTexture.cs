using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe class D3D11CanvasTexture : IDisposable
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
    private readonly RenderHandle _texture;
    private readonly Description _description;
    private readonly bool _BGRA;
    internal CompatibilityCanvasBackend Backend { get; }
    internal nint Native { get; }
    internal nint Handle => _texture.DangerousGetHandle();
    internal Vector2i Size => new((int)_description.Width, (int)_description.Height);

    internal D3D11CanvasTexture(CompatibilityCanvasBackend backend, RenderHandle renderer, nint native, int width, int height)
    {
        var rendererHandle = renderer.DangerousGetHandle();
        var rendererProperties = SDL.GetRendererProperties(rendererHandle);
        var rendererDevice = SDL.GetPointerProperty(rendererProperties, SDL.Props.RendererD3D11DevicePointer, 0);
        if (rendererDevice == 0 || SDL.GetNumberProperty(rendererProperties, SDL.Props.RendererOutputColorspaceNumber, 0) != (long)SDL.Colorspace.SRGB)
            throw new NotSupportedException("Native texture import requires a Direct3D 11 SDR renderer.");

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

        // ID3D11Texture2D::GetDesc.
        Description description;
        ((delegate* unmanaged[Stdcall]<nint, Description*, void>)Methods(native)[10])(native, &description);
        if (description.Width != width || description.Height != height)
            throw new ArgumentException("The declared dimensions differ from the native texture.", "nativeHandle");
        if (description.Mips != 1 || description.Layers != 1 || description.Samples != 1 ||
            description.Usage != 0 || (description.Bind & 8) == 0 || description.Misc != 0 ||
            description.Format is not (28 or 87))
            throw new NotSupportedException("Import requires a single-mip, single-layer, single-sample RGBA8 or BGRA8 UNORM shader resource with default usage and no miscellaneous flags.");

        _BGRA = description.Format == 87;
        _description = description;
        Backend = backend;
        Native = native;
        var properties = SDL.CreateProperties();
        if (properties == 0) throw CanvasBackend.Failure("allocate native texture properties");
        try
        {
            CanvasBackend.Check(SDL.SetNumberProperty(properties, SDL.Props.TextureCreateFormatNumber,
                (long)(_BGRA ? SDL.PixelFormat.ARGB8888 : SDL.PixelFormat.ABGR8888)) &&
                SDL.SetNumberProperty(properties, SDL.Props.TextureCreateWidthNumber, width) &&
                SDL.SetNumberProperty(properties, SDL.Props.TextureCreateHeightNumber, height) &&
                SDL.SetPointerProperty(properties, SDL.Props.TextureCreateD3D11TexturePointer, native), "configure native texture import");
            _texture = new(SDL.CreateTextureWithProperties(rendererHandle, properties), SDL.DestroyTexture, renderer);
            try
            {
                if (SDL.GetPointerProperty(SDL.GetTextureProperties(Handle), SDL.Props.TextureD3D11TexturePointer, 0) != native)
                    throw new NotSupportedException("The renderer cannot sample the imported allocation directly.");
            }
            catch { _texture.Dispose(); throw; }
        }
        finally { SDL.DestroyProperties(properties); }
    }

    private static nint* Methods(nint value) => *(nint**)value;
    private static nint GetDevice(nint texture)
    {
        nint device;
        // ID3D11DeviceChild::GetDevice returns a retained reference.
        ((delegate* unmanaged[Stdcall]<nint, nint*, void>)Methods(texture)[3])(texture, &device);
        return device;
    }

    internal Image Read()
    {
        Backend.FlushImportedTexture();
        var device = GetDevice(Native);
        try
        {
            var description = _description;
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
                    ((delegate* unmanaged[Stdcall]<nint, nint, nint, void>)methods[47])(context, staging, Native);
                    Mapping mapping;
                    Marshal.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, uint, uint, uint, Mapping*, int>)methods[14])
                        (context, staging, 0, 1, 0, &mapping));
                    try
                    {
                        var size = Size;
                        var pitch = checked(size.X * 4);
                        var pixels = new byte[checked(pitch * size.Y)];
                        for (var row = 0; row < size.Y; row++)
                            new ReadOnlySpan<byte>(mapping.Data + row * (long)mapping.RowPitch, pitch).CopyTo(pixels.AsSpan(row * pitch, pitch));
                        SwapChannels(pixels);
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

    internal void Update(Image image)
    {
        ArgumentNullException.ThrowIfNull(image);
        var size = Size;
        if (image.Width != size.X || image.Height != size.Y || image.PixelFormat != Image.Format.Rgba8 || image.HasMIPMaps)
            throw new ArgumentException("Native texture updates require matching RGBA8 dimensions without mipmaps.", nameof(image));
        var pixels = image.GetData();
        SwapChannels(pixels);
        fixed (byte* data = pixels)
            CanvasBackend.Check(SDL.UpdateTexture(Handle, 0, (nint)data, checked(size.X * 4)), "update imported texture pixels");
    }

    private void SwapChannels(Span<byte> pixels)
    {
        if (!_BGRA) return;
        for (var i = 0; i < pixels.Length; i += 4) (pixels[i], pixels[i + 2]) = (pixels[i + 2], pixels[i]);
    }

    public void Dispose() => _texture.Dispose();
}
