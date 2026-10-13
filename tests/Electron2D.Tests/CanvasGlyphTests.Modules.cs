using Electron2D;

internal static partial class CanvasGlyphTests
{
    internal static void RunModules()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_GLYPH_EXPORT_DIR") ?? throw new InvalidOperationException("Specify the module export directory.");
        Directory.CreateDirectory(directory); var count = 0;
        void Export(byte[] source, bool canvas, string name)
        {
            var textures = new ShaderTexture[ShaderCompiler.SamplerCount(source)];
            for (var i = 0; i < textures.Length; i++) textures[i] = new(i == 0 && canvas ? "TEXTURE" : "resource" + i, i);
            var program = new ShaderProgram(source, [], new(StringComparer.Ordinal), textures);
            var decoded = MaterialLightingProgram.ComposeGlyph(program, false);
            File.WriteAllBytes(System.IO.Path.Combine(directory, name + "-msdf.spv"), decoded); count++;
            if (!canvas) textures = [.. textures, new("TEXTURE", textures.Length)];
            var lcd = MaterialLightingProgram.ComposeGlyph(new ShaderProgram(decoded, [], new(StringComparer.Ordinal), textures), true);
            File.WriteAllBytes(System.IO.Path.Combine(directory, name + "-lcd.spv"), lcd); count++;
        }
        Export(ShaderProgram.Default.Code, true, "default");
        Export(BuiltInShaders.Clip(0), true, "clip");
        Export(BuiltInShaders.Lighting(0, 0, 0), true, "lighting");
        foreach (var name in new[] { "CanvasGLSL", "CanvasHLSL", "MaterialLightGLSL", "MaterialLightHLSL", "MaterialLightNew", "ItemUniformsGLSL", "ItemUniformsHLSL", "ItemUniformTypes", "GlyphColorGLSL", "GlyphColorHLSL", "GlyphScalar" })
        {
            using var shader = TextureArrayTests.Load(name); var program = shader.GetProgram();
            var canvas = program.Textures.Any(texture => texture.IsCanvasTexture);
            Export(program.Code, canvas, name);
            for (var n = -1; n < 3; n++) for (var s = -1; s < 3; s++) foreach (var masks in new[] { false, true })
                        Export(MaterialLightingProgram.Compose(program, n, s, masks), canvas, $"{name}-{n}-{s}-{masks}");
        }
        using var many = TextureArrayTests.Load("MaterialLightMany");
        Reject<NotSupportedException>(() => MaterialLightingProgram.ComposeGlyph(many.GetProgram(), false));
        Check(count == 732, "All glyph/library descriptor and language variants were exported.");
        Console.WriteLine($"{count} composed glyph modules exported; sixteen user samplers reject the required extra glyph sampler.");
    }
}
