using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private SDL.GPUSampleCount ExactSamples(DataFormat format, TextureSamples samples)
    {
        if (samples is < TextureSamples.Samples1 or >= TextureSamples.Max) throw new ArgumentOutOfRangeException(nameof(samples));
        if (samples > TextureSamples.Samples8) throw new NotSupportedException("This native backend exposes sample counts up to eight.");
        var native = (SDL.GPUSampleCount)samples;
        if (samples == TextureSamples.Samples1) return native;
        if (!SDL.GPUTextureSupportsSampleCount(Device, NativeFormat(format), native)) throw new NotSupportedException("The requested exact sample count is unavailable for this format.");
        return native;
    }
    private void ClearMultisample(Entry entry, Color color, uint layer = 0)
    {
        var target = new SDL.GPUColorTargetInfo { Texture = entry.NativeHandle, LayerOrDepthPlane = layer, ClearColor = new() { R = color.R, G = color.G, B = color.B, A = color.A }, LoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store };
        var pass = SDL.BeginGPURenderPass(Commands(), (nint)(&target), 1, 0);
        if (pass == 0) throw GPUPhysicsDevice.Failure("clear multisample target");
        SDL.EndGPURenderPass(pass);
    }
    private static void ResolvePair(TextureInfo source, TextureInfo destination)
    {
        var marked = destination.Resolve || DepthFormat(destination.Format) && (destination.Usage & TextureUsageBits.DepthResolveAttachment) != 0;
        if (source.Samples == TextureSamples.Samples1 || destination.Samples != TextureSamples.Samples1 || !marked || source.Format != destination.Format || source.Width != destination.Width || source.Height != destination.Height || source.Type != destination.Type || source.Layers != destination.Layers || source.Mips != 1 || destination.Mips != 1) throw new ArgumentException("Resolve requires a matching multisample source and marked single-sample destination.");
    }
    /// <summary>Resolves a complete multisample color or depth/stencil image into its marked single-sample destination on the GPU timeline.</summary>
    /// <param name="fromTexture">A multisample image or array with CanCopyFrom usage.</param>
    /// <param name="toTexture">A matching marked resolve target with CanCopyTo usage.</param>
    /// <remarks>Resolves every layer and preserves the source samples for later passes or resolves. Color uses native color resolve; depth and stencil each select sample zero through their enabled native adapter. Results are available to subsequent sampling, copies and readback. A depth destination is marked with IsResolveBuffer or DepthResolveAttachment.</remarks>
    /// <exception cref="ArgumentException">Dimensions, formats, roles or sample counts disagree.</exception>
    /// <exception cref="InvalidOperationException">A transfer role, owner thread or device state is invalid.</exception>
    public void TextureResolveMultisample(RID fromTexture, RID toTexture)
    {
        IdleList(); var source = Get(fromTexture, Kind.Texture); var destination = Get(toTexture, Kind.Texture);
        ResolvePair(source.Texture!, destination.Texture!); TextureRole(source, TextureUsageBits.CanCopyFrom); TextureRole(destination, TextureUsageBits.CanCopyTo);
        if (DepthFormat(source.Texture!.Format)) { ResolveDepth(source, destination); return; }
        var target = new SDL.GPUColorTargetInfo { Texture = source.NativeHandle, LoadOp = SDL.GPULoadOp.Load, StoreOp = SDL.GPUStoreOp.ResolveAndStore, ResolveTexture = destination.NativeHandle };
        for (uint layer = 0; layer < source.Texture!.Layers; layer++)
        {
            target.LayerOrDepthPlane = target.ResolveLayer = layer;
            var pass = SDL.BeginGPURenderPass(Commands(), (nint)(&target), 1, 0);
            if (pass == 0) throw GPUPhysicsDevice.Failure("resolve multisample color target");
            SDL.EndGPURenderPass(pass);
        }
    }
}
