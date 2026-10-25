namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly TextureSwizzleCache _sampledSwizzles = new();
    private readonly Dictionary<MaterialState, uint[]> _materialSwizzles = [];
    private uint MaterialSwizzle(MaterialState material, int index) => _materialSwizzles.TryGetValue(material, out var values) ? values[index] : 0;
    private uint CanvasSwizzle(CanvasBatch batch, ShaderTexture texture, int index)
    {
        if (texture.IsCanvasTexture) return TextureSwizzles.Source(batch.Texture);
        if (texture.IsNormalTexture) return TextureSwizzles.Source(Channels(batch.Texture)?.NormalTexture);
        if (texture.IsSpecularTexture) return TextureSwizzles.Source(Channels(batch.Texture)?.SpecularTexture);
        return texture.IsEngineTexture ? 0 : MaterialSwizzle(batch.Material!, index);
    }
    private bool CanvasFragmentSwizzles(CanvasBatch batch, Span<uint> maps)
    {
        maps.Clear(); var used = false;
        var channels = Channels(batch.Texture);
        if (batch.Material is { } material && !batch.MaskShader && !batch.GroupShader)
        {
            for (var i = 0; i < material.Program.FragmentTextureCount; i++)
            { maps[i] = CanvasSwizzle(batch, material.Program.Textures[i], i); used |= maps[i] != 0; }
            if (batch.Lighting is not null)
            {
                var slot = material.Program.FragmentTextureCount;
                if (channels?.NormalTexture is { } normal) { maps[slot] = TextureSwizzles.Source(normal); used |= maps[slot++] != 0; }
                if (channels?.SpecularTexture is { } specular) { maps[slot] = TextureSwizzles.Source(specular); used |= maps[slot++] != 0; }
            }
            if (batch.Glyph.Mode != CanvasGlyphMode.None && !FragmentUsesCanvasTexture(material.Program))
            {
                var slot = material.Program.FragmentTextureCount;
                if (batch.Lighting is not null)
                {
                    MaterialVariant(batch, out var normal, out var specular);
                    slot += (normal >= 0 ? 1 : 0) + (specular >= 0 ? 1 : 0) + (_materialMasks ? 1 : 0);
                }
                maps[slot] = TextureSwizzles.Source(batch.Texture); used |= maps[slot] != 0;
            }
        }
        else
        {
            maps[0] = TextureSwizzles.Source(batch.Texture); used = maps[0] != 0;
            if (batch.Lighting is not null && !batch.MaskShader)
            { maps[1] = TextureSwizzles.Source(channels?.NormalTexture); maps[2] = TextureSwizzles.Source(channels?.SpecularTexture); used |= maps[1] != 0 || maps[2] != 0; }
        }
        return used;
    }
    private byte[] SwizzledFragment(CanvasBatch batch, byte[] code)
    {
        Span<uint> maps = stackalloc uint[16];
        return CanvasFragmentSwizzles(batch, maps) ? _sampledSwizzles.Get(code, maps) : code;
    }
    private byte[]? SwizzledVertex(CanvasBatch batch)
    {
        var code = (batch.GroupShader || batch.MaskShader) && !batch.Procedural ? null : batch.Material?.Program.VertexCode(batch.InstanceCount > 0, batch.Lighting is not null && !batch.MaskShader && !batch.GroupShader, batch.Procedural);
        if (code is null || batch.Material is not { } material) return code;
        Span<uint> maps = stackalloc uint[16]; maps.Clear(); var used = false;
        var indices = material.Program.VertexTextureIndices;
        for (var i = 0; i < indices.Length; i++) { var index = indices[i]; maps[i] = CanvasSwizzle(batch, material.Program.Textures[index], index); used |= maps[i] != 0; }
        return used ? _sampledSwizzles.Get(code, maps) : code;
    }
    private byte[] ParticleCode(MaterialState material)
    {
        var code = material.Program.NativeCode;
        if (!_materialSwizzles.TryGetValue(material, out var maps)) return code;
        return _sampledSwizzles.Get(code, maps);
    }
}
