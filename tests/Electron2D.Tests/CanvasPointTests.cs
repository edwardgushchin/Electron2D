using Electron2D;
using static Electron2D.MaterialLightingProgram;
using static TextureArrayTests;

internal static class CanvasPointTests
{
    internal static void Run(string folder)
    {
        CustomNodes.Register();
        using (var input = new VisualShaderNodeInput())
            foreach (var name in new[] { "point_size", "point_coord" })
            {
                input.InputName = name;
                foreach (var stage in Enum.GetValues<VisualShader.Type>().Where(s => s != VisualShader.Type.Max))
                {
                    var role = name == "point_coord" && stage is VisualShader.Type.Fragment or VisualShader.Type.Light ? VisualShaderNode.PortType.Vector2 : VisualShaderNode.PortType.Scalar;
                    Check(input.OutputsFor(stage).Single() == role, "Point inputs resolve exact borrowing-stage roles.");
                }
            }
        foreach (var bytes in new[] { BuiltInShaders.Vertex, BuiltInShaders.InstancedVertex, BuiltInShaders.LightingVertex, BuiltInShaders.LightingInstancedVertex, BuiltInShaders.DrawableVertex })
        {
            Check(!CanvasPointProgram.WritesSize(bytes), "Default one-pixel vertex programs require no optional point capability.");
            CanvasPointProgram.ValidateNative(bytes, false, "vulkan", false); CanvasPointProgram.ValidateNative(bytes, false, "direct3d12", false);
        }
        foreach (var file in Directory.GetFiles(folder, "point-*.e2dres").Order(StringComparer.Ordinal))
        {
            using var graph = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep);
            var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var fragment = graph.GetSPIRV();
            var isDefault = System.IO.Path.GetFileName(file) == "point-default.e2dres";
            Check(CanvasPointProgram.WritesSize(vertex) != isDefault, "Authored size capability distinguishes the one-pixel default.");
            CanvasPointProgram.ValidateNative(vertex, false, "vulkan", true); CanvasPointProgram.ValidateNative(fragment, true, "vulkan", true);
            if (!isDefault) Reject<NotSupportedException>(() => CanvasPointProgram.ValidateNative(vertex, false, "vulkan", false));
            Reject<NotSupportedException>(() => CanvasPointProgram.ValidateNative(fragment, true, "direct3d12", false));
            var stripped = new Module(vertex); stripped.Instructions.RemoveAll(i => Op(i) is 5 or 6);
            Check(CanvasPointProgram.WritesSize(stripped.Write()) != isDefault, "Point capabilities do not depend on debug names.");
            using var material = new ShaderMaterial { Shader = graph };
            Check(!graph.GetShaderUniformList().Any(p => p.Name is "POINT_SIZE" or "POINT_COORD"), "Point builtins do not leak material fields.");
            using var copy = (VisualShader)graph.Duplicate(true); Check(copy.GenerateSources().Vertex == graph.GenerateSources().Vertex, "Graph copies preserve point stage source.");
            using var independent = Shader.CreateFromSPIRV(vertex, fragment); vertex.AsSpan().Clear(); fragment.AsSpan().Clear();
            Check(independent.GetSPIRV().SequenceEqual(graph.GetSPIRV()), "Point programs own paired bytecode snapshots.");
        }
        Console.WriteLine("Point contract: borrowing roles, default/optional capabilities, names, graph copying, material ownership and paired bytecode passed.");
    }

    internal static void Export(string folder, string output)
    {
        CustomNodes.Register(); Directory.CreateDirectory(output); var count = 0;
        var files = Directory.GetFiles(folder, "point-*.e2dres").Concat(Directory.GetFiles(folder, "point-raw-*.vert.spv")).Order(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var paired = file.EndsWith(".vert.spv", StringComparison.Ordinal);
            using var shader = paired ? Shader.CreateFromSPIRV(File.ReadAllBytes(file), File.ReadAllBytes(file[..^9] + ".frag.spv")) : ResourceLoader.Load<Shader>(file, ResourceLoader.CacheMode.IgnoreDeep);
            var program = shader.GetProgram(); var name = System.IO.Path.GetFileName(file).Split('.')[0];
            for (var i = 0; i < 16; i++)
            {
                File.WriteAllBytes(System.IO.Path.Combine(output, name + "-vertex-" + i + ".spv"), program.VertexCode((i & 1) != 0, (i & 2) != 0, (i & 4) != 0, (i & 8) != 0)!); count++;
            }
            File.WriteAllBytes(System.IO.Path.Combine(output, name + "-unlit.spv"), program.NativeCode); count++;
            for (var normal = -1; normal < 3; normal++) for (var specular = -1; specular < 3; specular++) foreach (var mask in new[] { false, true })
                    {
                        File.WriteAllBytes(System.IO.Path.Combine(output, name + "-light-" + normal + "-" + specular + "-" + mask + ".spv"), Compose(program, normal, specular, mask)); count++;
                    }
        }
        Console.WriteLine($"Exported {count} native point vertex/fragment modules.");
    }
}
