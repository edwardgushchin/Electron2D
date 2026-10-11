using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _lightAtlas;
    private readonly Dictionary<(ShaderProgram Program, int Variant), byte[]> _litPrograms = [];
    private readonly HashSet<(ShaderProgram Program, int Variant)> _usedLitPrograms = [];
    private int[] _lightingOffsets = [];
    private bool _materialMasks;
    private int MaterialVariant(CanvasBatch batch, out int normal, out int specular)
    {
        var channels = Channels(batch.Texture);
        normal = channels?.NormalTexture is { } n ? ShaderTexture.ResourcePixelType(n) : -1;
        specular = channels?.SpecularTexture is { } s ? ShaderTexture.ResourcePixelType(s) : -1;
        return ((normal + 1) * 4 + specular + 1) * 2 + (_materialMasks ? 1 : 0);
    }
    private Vector2i _lightAtlasSize;
    private readonly RenderHandle?[] _lightMaskPipelines = new RenderHandle?[3];
    private Func<Texture, Vector2i>? _lightMaskSize;
    private byte[] BatchCode(CanvasBatch batch)
    {
        if (batch.MaskShader) return BuiltInShaders.Clip(ShaderTexture.ResourcePixelType(batch.Texture));
        if (batch.Lighting is not null)
        {
            if (batch.Material is { } material)
            {
                var key = (material.Program, MaterialVariant(batch, out var normal, out var specular));
                _usedLitPrograms.Add(key);
                if (!_litPrograms.TryGetValue(key, out var composed)) _litPrograms.Add(key, composed = MaterialLightingProgram.Compose(material.Program, normal, specular, _materialMasks));
                return composed;
            }
            var channels = Channels(batch.Texture);
            return BuiltInShaders.Lighting(ShaderTexture.ResourcePixelType(batch.Texture), ShaderTexture.ResourcePixelType(channels?.NormalTexture), ShaderTexture.ResourcePixelType(channels?.SpecularTexture));
        }
        if (batch.ShaderCode is { } code) return code;
        return ShaderTexture.ResourcePixelType(batch.Texture) switch { 1 => BuiltInShaders.SignedFragment, 2 => BuiltInShaders.UnsignedFragment, _ => _defaultFragment };
    }

    private static CanvasTexture? Channels(Texture? texture)
    {
        if (texture is ServerTexture { IsProxy: true } proxy) texture = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget);
        return (texture is AtlasTexture atlas ? atlas.RenderingTexture : texture) as CanvasTexture;
    }
    private void PrepareLightingChannels(CanvasBatch batch)
    {
        var channels = Channels(batch.Texture);
        if (UsesCanvasTexture(batch)) _ = CanvasBinding(batch);
        var plain = batch with { Material = null };
        _ = CanvasBinding(plain with { Texture = null });
        if (channels?.NormalTexture is { } normal) _ = CanvasBinding(plain with { Texture = normal });
        if (channels?.SpecularTexture is { } specular) _ = CanvasBinding(plain with { Texture = specular });
    }

    private void PrepareLightingAtlas(CanvasLightingFrame frame)
    {
        foreach (var mask in frame.Masks)
        {
            _ = CanvasBinding(new(0, 0, null, mask.Texture));
            var scalar = ShaderTexture.ResourcePixelType(mask.Texture);
            var code = scalar switch { 1 => BuiltInShaders.SignedFragment, 2 => BuiltInShaders.UnsignedFragment, _ => _defaultFragment };
            _lightMaskPipelines[scalar] ??= CreatePipeline(code, BlendMode.Mix, overwrite: true, format: SDL.GPUTextureFormat.R32G32B32A32Float);
        }
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
        if (texture is ServerTexture { IsProxy: true } proxy) texture = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget) ?? RenderingTextureRegistry.PlaceholderTexture;
        if (texture is CanvasTexture channels) texture = channels.DiffuseTexture ?? _whiteTexture!;
        if (texture is AtlasTexture) return new(texture.GetWidth(), texture.GetHeight());
        texture = (Texture?)ShaderTexture.Source(texture) ?? _whiteTexture!;
        if (texture is ViewportTexture view) return FindTarget(view.Bound!)!.Size;
        if (texture is TextureRD { DeviceInfo: { } info }) return new((int)info.Width, (int)info.Height);
        var pixels = texture.CapturePixels() ?? RenderingTextureRegistry.PlaceholderPixels;
        return new(pixels.Source.Width, pixels.Source.Height);
    }

    private void WriteLightingMaskVertices(Span<CanvasVertex> vertices, CanvasLightingFrame frame)
    {
        for (var index = 0; index < frame.Masks.Count; index++)
        {
            var mask = frame.Masks[index]; var texture = mask.Texture;
            if (texture is ServerTexture { IsProxy: true } proxy) texture = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget) ?? RenderingTextureRegistry.PlaceholderTexture;
            if (texture is CanvasTexture channels) texture = channels.DiffuseTexture ?? _whiteTexture!;
            var destination = new Rect2(mask.Region.Position, mask.Region.Size);
            var source = new Rect2(Vector2.Zero, LightMaskSize(texture));
            while (texture is AtlasTexture atlas)
            {
                texture = atlas.ResolveDrawRegion(ref destination, ref source)!;
                if (texture is null) break;
            }
            var size = texture is null ? Vector2.One : (Vector2)LightMaskSize(texture);
            for (var corner = 0; corner < 6; corner++)
            {
                var uv = corner switch { 0 or 3 => Vector2.Zero, 1 => new Vector2(1, 0), 2 or 4 => Vector2.One, _ => new Vector2(0, 1) };
                vertices[index * 6 + corner] = new(destination.Position + destination.Size * uv, texture is null ? Colors.Transparent : Colors.White, (source.Position + source.Size * uv) / size);
            }
        }
    }

    private void CopyLightingAtlas(nint command, CanvasLightingFrame frame, int lightingBytes, int first)
    {
        var target = new SDL.GPUColorTargetInfo { Texture = _lightAtlas!.DangerousGetHandle(), LoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store, Cycle = true };
        var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
        if (pass == 0) throw Failure("begin the light-mask atlas");
        try
        {
            var dimensions = new Vector4(frame.AtlasSize.X, frame.AtlasSize.Y, 0, 0);
            SDL.PushGPUVertexUniformData(command, 0, (nint)(&dimensions), (uint)sizeof(Vector4));
            var binding = new SDL.GPUBufferBinding { Buffer = _vertexBuffer!.DangerousGetHandle(), Offset = (uint)lightingBytes };
            SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(&binding, 1), 1);
            for (var index = 0; index < frame.Masks.Count; index++)
            {
                var mask = frame.Masks[index]; var region = mask.Region;
                var scissor = new SDL.Rect { X = region.Position.X, Y = region.Position.Y, W = region.Size.X, H = region.Size.Y }; SDL.SetGPUScissor(pass, in scissor);
                SDL.BindGPUGraphicsPipeline(pass, _lightMaskPipelines[ShaderTexture.ResourcePixelType(mask.Texture)]!.DangerousGetHandle());
                var sampler = CanvasBinding(new(0, 0, null, mask.Texture));
                SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&sampler, 1), 1);
                SDL.DrawGPUPrimitives(pass, 6, 1, (uint)(first + index * 6), 0);
            }
        }
        finally { SDL.EndGPURenderPass(pass); }
    }

    private void PrepareMaterialLighting(ReadOnlySpan<CanvasBatch> batches, CanvasLightingFrame frame)
    {
        var baseCount = checked(frame.LightCount * 9 + frame.SegmentCount * 2);
        if (frame.Data.Count > baseCount) frame.Data.RemoveRange(baseCount, frame.Data.Count - baseCount);
        if (_lightingOffsets.Length < batches.Length) Array.Resize(ref _lightingOffsets, Math.Max(32, batches.Length));
        for (var index = 0; index < batches.Length; index++)
        {
            _lightingOffsets[index] = 0;
            var batch = batches[index]; if (batch.Lighting is not { } item || batch.Material is null || batch.MaskShader || batch.GroupShader) continue;
            var channels = Channels(batch.Texture); var specular = channels?.SpecularColor ?? Colors.White;
            _lightingOffsets[index] = frame.Data.Count;
            frame.Data.Add(new(item.Ambient.R, item.Ambient.G, item.Ambient.B, item.Ambient.A));
            frame.Data.Add(new(specular.R, specular.G, specular.B, channels?.SpecularShininess ?? 1));
            frame.Data.Add(new(BitConverter.UInt32BitsToSingle(item.Mask), BitConverter.Int32BitsToSingle(item.Z), BitConverter.Int32BitsToSingle(item.Layer), BitConverter.Int32BitsToSingle((int)item.Mode)));
            frame.Data.Add(new(BitConverter.Int32BitsToSingle(frame.LightCount), BitConverter.Int32BitsToSingle(frame.SegmentCount), BitConverter.Int32BitsToSingle(channels?.NormalTexture is null ? 0 : 1), BitConverter.Int32BitsToSingle(channels?.SpecularTexture is null ? 0 : 1)));
            frame.Data.Add(new(1f / frame.AtlasSize.X, 1f / frame.AtlasSize.Y, 0, 0));
        }
    }

    private void BindMaterialLighting(nint pass, CanvasBatch batch)
    {
        var bindings = stackalloc SDL.GPUTextureSamplerBinding[3]; var count = 0; var plain = batch with { Material = null }; var channels = Channels(batch.Texture);
        if (channels?.NormalTexture is { } normal) bindings[count++] = CanvasBinding(plain with { Texture = normal });
        if (channels?.SpecularTexture is { } specular) bindings[count++] = CanvasBinding(plain with { Texture = specular });
        if (_materialMasks) bindings[count++] = new() { Texture = _lightAtlas!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
        if (count != 0) SDL.BindGPUFragmentSamplers(pass, (uint)batch.Material!.Program.Textures.Length, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(bindings, count), (uint)count);
        var buffer = _vertexBuffer!.DangerousGetHandle(); SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint)(&buffer), 1);
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
        var bindings = stackalloc SDL.GPUTextureSamplerBinding[4];
        bindings[0] = CanvasBinding(batch);
        bindings[1] = channels?.NormalTexture is { } normal ? CanvasBinding(batch with { Texture = normal }) : white;
        bindings[2] = channels?.SpecularTexture is { } reflection ? CanvasBinding(batch with { Texture = reflection }) : white;
        bindings[3] = new() { Texture = _lightAtlas!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
        SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(bindings, 4), 4);
        var buffer = _vertexBuffer!.DangerousGetHandle();
        SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint)(&buffer), 1);
    }
}
