using Electron2D;

internal static class CanvasMatrixTests
{
    internal static void WriteVariants(string folder, string output)
    {
        Directory.CreateDirectory(output); var count = 0;
        foreach (var name in new[] { "matrix-nodes", "matrix-expression", "matrix-no-id", "matrix-glsl", "matrix-hlsl", "matrix-spv13", "matrix-spv15" })
        {
            using var shader = File.Exists(System.IO.Path.Combine(folder, name + ".vert.spv"))
                ? Shader.CreateFromSPIRV(File.ReadAllBytes(System.IO.Path.Combine(folder, name + ".vert.spv")), File.ReadAllBytes(System.IO.Path.Combine(folder, "matrix.frag.spv")))
                : ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(folder, name + ".e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
            var program = shader.GetProgram();
            for (var i = 0; i < 16; i++)
            {
                var code = program.VertexCode((i & 1) != 0, (i & 2) != 0, (i & 4) != 0, (i & 8) != 0)!;
                File.WriteAllBytes(System.IO.Path.Combine(output, name + "-" + i + ".spv"), code); count++;
            }
        }
        Console.WriteLine($"Exported {count} matrix vertex variants for independent validation.");
    }
}
