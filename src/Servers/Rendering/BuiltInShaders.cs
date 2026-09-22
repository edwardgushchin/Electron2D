namespace Electron2D;

internal static class BuiltInShaders
{
    internal static readonly byte[] Vertex = Read("Canvas.vert.spv");
    internal static readonly byte[] Fragment = Read("Canvas.frag.spv");

    private static byte[] Read(string name)
    {
        using var input = typeof(BuiltInShaders).Assembly.GetManifestResourceStream("Electron2D.Shaders." + name)
            ?? throw new InvalidOperationException("A built-in shader is missing: " + name);
        using var output = new MemoryStream();
        input.CopyTo(output);
        return output.ToArray();
    }
}
