using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly Dictionary<(byte[] Code, bool LCD), byte[]> _glyphPrograms = [];
    private readonly List<(byte[] Code, bool LCD)> _unusedGlyphPrograms = [];
    private readonly HashSet<(byte[] Code, bool LCD)> _usedGlyphPrograms = [];
    private byte[] GlyphCode(CanvasBatch batch)
    {
        var source = BatchCode(batch);
        if (batch.Glyph.Mode == CanvasGlyphMode.None) return source;
        if (ShaderTexture.ResourcePixelType(batch.Texture) != 0) throw new NotSupportedException("Glyph coverage requires floating sampled texture values.");
        var key = (source, batch.Glyph.Mode == CanvasGlyphMode.LCD); _usedGlyphPrograms.Add(key);
        if (_glyphPrograms.TryGetValue(key, out var prepared)) return prepared;
        var samplers = ShaderCompiler.SamplerCount(source);
        var textures = new ShaderTexture[samplers];
        for (var i = 0; i < textures.Length; i++) textures[i] = new(i == 0 && (batch.Material is null || FragmentUsesCanvasTexture(batch.Material.Program)) ? "TEXTURE" : "glyph_source_" + i, i);
        var program = new ShaderProgram(source, [], new(StringComparer.Ordinal), textures);
        var decoded = MaterialLightingProgram.ComposeGlyph(program, false);
        if (key.Item2 && !batch.MaskShader)
        {
            var transformedTextures = program.Textures.Any(texture => texture.IsCanvasTexture) ? program.Textures : [.. program.Textures, new ShaderTexture("TEXTURE", program.Textures.Length)];
            var transformed = new ShaderProgram(decoded, [], new(StringComparer.Ordinal), transformedTextures);
            decoded = MaterialLightingProgram.ComposeGlyph(transformed, true);
        }
        _glyphPrograms.Add(key, decoded); return decoded;
    }
    private void BindGlyphTexture(nint pass, CanvasBatch batch)
    {
        if (batch.Glyph.Mode == CanvasGlyphMode.None || batch.Material is null || FragmentUsesCanvasTexture(batch.Material.Program)) return;
        var slot = batch.Material.Program.FragmentTextureCount;
        if (batch.Lighting is not null)
        {
            MaterialVariant(batch, out var normal, out var specular);
            slot += (normal >= 0 ? 1 : 0) + (specular >= 0 ? 1 : 0) + (_materialMasks ? 1 : 0);
        }
        var binding = CanvasBinding(batch with { Material = null });
        SDL.BindGPUFragmentSamplers(pass, (uint)slot, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&binding, 1), 1);
    }

    private static bool FragmentUsesCanvasTexture(ShaderProgram program) => program.FragmentTextureCount != 0 && program.Textures[0].IsCanvasTexture;
}
