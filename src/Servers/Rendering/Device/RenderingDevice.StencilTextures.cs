using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private bool StencilSupported(TextureUsageBits usage, TextureSamples samples = TextureSamples.Samples1)
    {
        var native = NativeUsage(usage) | SDL.GPUTextureUsageFlags.DepthStencilTarget;
        try { return NativeStencilSupport(Device, 1, native, (uint)samples) == 1; }
        catch (EntryPointNotFoundException) { return false; }
    }
    private RenderHandle CreateStencilTexture(SDL.GPUTextureCreateInfo create)
    {
        var properties = SDL.CreateProperties();
        if (properties == 0) throw GPUPhysicsDevice.Failure("create stencil texture properties");
        try
        {
            if (!SDL.SetBooleanProperty(properties, "Electron2D.render.stencil_only", true)) throw GPUPhysicsDevice.Failure("configure stencil texture");
            create.Props = properties; var format = create.Format;
            return new(GPUTextureViews.Create(Device, 3, in create, (nint)(&format), 1), h => GPUTextureViews.Destroy(Device, 1, h), _context.Handle);
        }
        finally { SDL.DestroyProperties(properties); }
    }
    private nint BeginGraphicsPass(nint commands, nint colors, uint count, nint depth, DataFormat format) => format == DataFormat.S8UINT
        ? NativeStencilBegin(Device, 1, commands, colors, count, depth)
        : SDL.BeginGPURenderPass(commands, colors, count, depth);
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_stencil_support")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial int NativeStencilSupport(nint device, uint abi, SDL.GPUTextureUsageFlags usage, uint samples);
    [LibraryImport(NativeLibraries.EngineLibrary, EntryPoint = "e2d_render_stencil_begin")]
    [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
    private static partial nint NativeStencilBegin(nint device, uint abi, nint commands, nint colors, uint count, nint depth);
}
