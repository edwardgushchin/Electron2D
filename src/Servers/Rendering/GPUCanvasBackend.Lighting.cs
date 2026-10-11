using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _lightAtlas;
    private Vector2i _lightAtlasSize;
    private Func<Texture, Vector2i>? _lightMaskSize;
    private byte[] BatchCode(CanvasBatch batch)
    {
        if (batch.MaskShader) { if (ShaderTexture.ResourcePixelType(batch.Texture) != 0) throw new NotSupportedException("Integer canvas masks need scalar-aware alpha composition."); return BuiltInShaders.Clip; }
        if (batch.Lighting is not null)
        {
            if (ShaderTexture.ResourcePixelType(batch.Texture) != 0) throw new NotSupportedException("Integer light receivers need scalar-aware lighting texture composition.");
            if (batch.Material is not null) throw new NotSupportedException("Programmable canvas materials require an explicit lighting shader interface before receiving scene lights.");
            return BuiltInShaders.Lighting;
        }
        if (batch.ShaderCode is { } code) return code;
        return ShaderTexture.ResourcePixelType(batch.Texture) switch { 1 => BuiltInShaders.SignedFragment, 2 => BuiltInShaders.UnsignedFragment, _ => _defaultFragment };
    }

    private static CanvasTexture? Channels(Texture? texture) => (texture is AtlasTexture atlas ? atlas.RenderingTexture : texture) as CanvasTexture;
    private void PrepareLightingChannels(CanvasBatch batch)
    {
        var channels = Channels(batch.Texture);
        _ = CanvasBinding(batch);
        _ = CanvasBinding(batch with { Texture = null });
        if (channels?.NormalTexture is { } normal) { if (ShaderTexture.ResourcePixelType(normal) != 0) throw new NotSupportedException("Integer normal channels require typed lighting composition."); _ = PrepareTexture(normal); }
        if (channels?.SpecularTexture is { } specular) { if (ShaderTexture.ResourcePixelType(specular) != 0) throw new NotSupportedException("Integer specular channels require typed lighting composition."); _ = PrepareTexture(specular); }
    }

    private void PrepareLightingAtlas(CanvasLightingFrame frame)
    {
        foreach (var mask in frame.Masks) _ = CanvasBinding(new(0, 0, null, mask.Texture));
        frame.PackMasks(_lightMaskSize ??= LightMaskSize, initialize: false);
        _ = Sampler(TextureFilter.Linear, TextureRepeat.Disabled);
        if (_lightAtlas is not null && _lightAtlasSize == frame.AtlasSize) return;
        var size = frame.AtlasSize;
        var usage = SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ColorTarget;
        if (!SDL.GPUTextureSupportsFormat(Device, SDL.GPUTextureFormat.R32G32B32A32Float, SDL.GPUTextureType.TextureType2D, usage))
            throw new NotSupportedException("Canvas light masks require a sampleable floating-point color target on this GPU.");
        var info = new SDL.GPUTextureCreateInfo { Type = SDL.GPUTextureType.TextureType2D, Format = SDL.GPUTextureFormat.R32G32B32A32Float, Width = (uint)size.X, Height = (uint)size.Y, LayerCountOrDepth = 1, NumLevels = 1, Usage = usage };
        var replacement = new RenderHandle(SDL.CreateGPUTexture(Device, in info), h => SDL.ReleaseGPUTexture(Device, h), _device);
        _lightAtlas?.Dispose(); _lightAtlas = replacement; _lightAtlasSize = size;
    }

    private Vector2i LightMaskSize(Texture texture)
    {
        if (texture is AtlasTexture atlas) texture = atlas.RenderingTexture ?? texture;
        if (texture is CanvasTexture canvas) texture = canvas.DiffuseTexture ?? _whiteTexture!;
        if (texture is ViewportTexture view) return FindTarget(view.Bound!)!.Size;
        var pixels = texture.CapturePixels() ?? RenderingTextureRegistry.PlaceholderPixels;
        return new(pixels.Source.Width, pixels.Source.Height);
    }

    private void CopyLightingAtlas(nint command, CanvasLightingFrame frame)
    {
        var first = true;
        foreach (var mask in frame.Masks)
        {
            var source = CanvasBinding(new(0, 0, null, mask.Texture)).Texture; var region = mask.Region;
            var blit = new SDL.GPUBlitInfo
            {
                Source = new() { Texture = source, W = (uint)region.Size.X, H = (uint)region.Size.Y },
                Destination = new() { Texture = _lightAtlas!.DangerousGetHandle(), X = (uint)region.Position.X, Y = (uint)region.Position.Y, W = (uint)region.Size.X, H = (uint)region.Size.Y },
                LoadOp = first ? SDL.GPULoadOp.Clear : SDL.GPULoadOp.Load,
                Cycle = first,
                Filter = SDL.GPUFilter.Nearest,
            };
            SDL.BlitGPUTexture(command, in blit); first = false;
        }
        if (first)
        {
            var target = new SDL.GPUColorTargetInfo { Texture = _lightAtlas!.DangerousGetHandle(), LoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store, Cycle = true };
            var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
            if (pass == 0) throw Failure("clear the light-mask atlas"); SDL.EndGPURenderPass(pass);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct LightingUniforms(Vector4 Ambient, Vector4 Specular, Vector4i Item, Vector4i Counts, Vector4 Atlas);

    private void BindLighting(nint command, nint pass, CanvasBatch batch, CanvasLightingFrame frame)
    {
        var item = batch.Lighting!.Value; var channels = Channels(batch.Texture);
        var specular = channels?.SpecularColor ?? Colors.White;
        var uniform = new LightingUniforms(new(item.Ambient.R, item.Ambient.G, item.Ambient.B, item.Ambient.A),
            new(specular.R, specular.G, specular.B, channels?.SpecularShininess ?? 1),
            new(unchecked((int)item.Mask), item.Z, item.Layer, (int)item.Mode),
            new(frame.LightCount, frame.SegmentCount, channels?.NormalTexture is null ? 0 : 1, channels?.SpecularTexture is null ? 0 : 1), new(1f / _lightAtlasSize.X, 1f / _lightAtlasSize.Y, 0, 0));
        SDL.PushGPUFragmentUniformData(command, 0, (nint)(&uniform), (uint)sizeof(LightingUniforms));
        var white = CanvasBinding(batch with { Texture = null });
        var sampler = Sampler(batch.Filter, batch.Repeat, batch.MaxAnisotropy);
        var bindings = stackalloc SDL.GPUTextureSamplerBinding[4];
        bindings[0] = CanvasBinding(batch);
        bindings[1] = channels?.NormalTexture is { } normal ? new() { Texture = PrepareTexture(normal), Sampler = sampler } : white;
        bindings[2] = channels?.SpecularTexture is { } reflection ? new() { Texture = PrepareTexture(reflection), Sampler = sampler } : white;
        bindings[3] = new() { Texture = _lightAtlas!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
        SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(bindings, 4), 4);
        var buffer = _vertexBuffer!.DangerousGetHandle();
        SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint)(&buffer), 1);
    }
}
