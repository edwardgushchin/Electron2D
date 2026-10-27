namespace Electron2D;

internal static class BuiltInShaders
{
    internal static readonly byte[] LightingVertex = Read("CanvasLighting.vert.spv");
    internal static readonly byte[] LightingInstancedVertex = Read("CanvasLightingInstanced.vert.spv");
    internal static readonly byte[] DrawableVertex = Read("Drawable.vert.spv");
    private static readonly byte[][] DrawableFragments = [Read("Drawable.frag.spv"), Read("Drawable2.frag.spv"), Read("Drawable3.frag.spv"), Read("Drawable4.frag.spv")];
    internal static byte[] DrawableFragment(int count) => DrawableFragments[count - 1];
    internal static readonly byte[] Vertex = Read("Canvas.vert.spv");
    internal static readonly byte[] InstancedVertex = Read("CanvasInstanced.vert.spv");
    internal static readonly byte[] SignedFragment = Read("CanvasSigned.frag.spv");
    internal static readonly byte[] UnsignedFragment = Read("CanvasUnsigned.frag.spv");
    internal static readonly byte[] Fragment = Read("Canvas.frag.spv");
    internal static readonly byte[] ColorArrayFragment = Read("CanvasColorArray.frag.spv");
    internal static readonly byte[] ColorFragment = Read("CanvasColor.frag.spv");
    internal static readonly byte[] OutputFragment = Read("CanvasOutput.frag.spv");

    internal static readonly byte[] SDF = Read("SDF.frag.spv");

    private static readonly byte[][] LightingVariants = ReadLighting();
    internal static byte[] Lighting(int diffuse, int normal, int specular) => LightingVariants[diffuse * 9 + normal * 3 + specular];
    private static byte[][] ReadLighting()
    {
        var result = new byte[27][];
        for (var d = 0; d < 3; d++) for (var n = 0; n < 3; n++) for (var s = 0; s < 3; s++)
                    result[d * 9 + n * 3 + s] = Read(d + n + s == 0 ? "Lighting.frag.spv" : $"Lighting{d}{n}{s}.frag.spv");
        return result;
    }

    private static readonly byte[][] ClipVariants = [Read("Clip.frag.spv"), Read("Clip1.frag.spv"), Read("Clip2.frag.spv")];
    internal static byte[] Clip(int scalar) => ClipVariants[scalar];

    internal static byte[] Read(string name)
    {
        using var input = typeof(BuiltInShaders).Assembly.GetManifestResourceStream("Electron2D.Shaders." + name)
            ?? throw new InvalidOperationException("A built-in shader is missing: " + name);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
