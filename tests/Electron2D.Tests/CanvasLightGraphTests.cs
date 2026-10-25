using Electron2D;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using static Electron2D.MaterialLightingProgram;

internal static partial class CanvasLightGraphTests
{
    internal static void ModernSources(string folder, string output, bool surface = false)
    {
        Directory.CreateDirectory(output);
        foreach (var name in surface ? ["surface-normal", "surface-matrix", "surface-light-vertex"] : new[] { "light-builtins", "light-context", "light-vertex-matrix" })
        {
            var file = System.IO.Path.Combine(folder, name + ".e2dres");
            using var graph = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep); var sources = graph.GenerateSources();
            File.WriteAllText(System.IO.Path.Combine(output, name + ".vert.glsl"), sources.Vertex);
            File.WriteAllText(System.IO.Path.Combine(output, name + ".frag.glsl"), sources.Fragment);
            File.WriteAllText(System.IO.Path.Combine(output, name + ".light.json"), JsonSerializer.Serialize(new
            {
                source = file,
                function = sources.Light?.Function,
                state = sources.Light?.State,
                surfaceFunction = sources.Surface?.Function,
                surfaceState = sources.Surface?.State
            }));
        }
    }

    internal static void InstallModern(string folder, string output)
    {
        Directory.CreateDirectory(output);
        foreach (var file in Directory.GetFiles(folder, "*.light.json"))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(file)); var fields = json.RootElement;
            var name = System.IO.Path.GetFileName(file)[..^11];
            using var graph = ResourceLoader.Load<VisualShader>(fields.GetProperty("source").GetString()!, ResourceLoader.CacheMode.IgnoreDeep);
            var fragment = File.ReadAllBytes(System.IO.Path.Combine(folder, name + ".frag.spv"));
            if (fields.GetProperty("function").GetString() is { } function)
                fragment = CanvasLightProgram.Annotate(fragment, new(function, fields.GetProperty("state").GetString()!));
            if (fields.TryGetProperty("surfaceFunction", out var surface) && surface.GetString() is { } surfaceFunction)
                fragment = CanvasSurfaceProgram.Annotate(fragment, new(surfaceFunction, fields.GetProperty("surfaceState").GetString()!));
            graph.SetSPIRV(File.ReadAllBytes(System.IO.Path.Combine(folder, name + ".vert.spv")), fragment);
            ResourceSaver.Save(graph, System.IO.Path.Combine(output, name + ".e2dres"), SaverFlags.BundleResources | SaverFlags.Compress);
        }
    }

    internal static void Metadata(string folder, bool surface = false)
    {
        using var graph = ResourceLoader.Load<VisualShader>(System.IO.Path.Combine(folder, surface ? "surface-normal.e2dres" : "light-constant.e2dres"), ResourceLoader.CacheMode.IgnoreDeep);
        var original = graph.GetSPIRV(); var vertex = graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex); var light = surface ? graph.GetProgram().Surface! : graph.GetProgram().AuthoredLight!;
        var changed = 0; graph.Changed += _ => changed++;
        var role = surface ? "Electron2D:canvas_surface:" : "Electron2D:canvas_light:"; var tag = Encoding.UTF8.GetBytes(role);
        bool Tag(uint[] instruction) => Op(instruction) == 7 && MemoryMarshal.AsBytes(instruction.AsSpan(2)).StartsWith(tag);
        void ReplaceTag(Module module, string value)
        {
            var old = module.Instructions.Single(Tag); var bytes = Encoding.UTF8.GetBytes(value); var padded = new byte[(bytes.Length + 4) & ~3]; bytes.CopyTo(padded, 0);
            module.Instructions[module.Instructions.IndexOf(old)] = Instruction(7, [old[1], .. MemoryMarshal.Cast<byte, uint>(padded).ToArray()]);
        }
        var prefix = role + "1:";
        var failures = new Dictionary<string, Action<Module>>
        {
            ["duplicate"] = m => { var copy = (uint[])m.Instructions.Single(Tag).Clone(); copy[1] = m.Header[3]++; m.Instructions.Insert(m.Instructions.FindIndex(Tag), copy); },
            ["version"] = m => ReplaceTag(m, role + "2:" + light.Function + ":" + light.State),
            ["noncanonical"] = m => ReplaceTag(m, prefix + "0" + light.Function + ":" + light.State),
            ["missing-function"] = m => ReplaceTag(m, prefix + light.State + ":" + light.State),
            ["state-role"] = m => m.Instructions.Single(i => Op(i) == 59 && i[2] == light.State)[3] = 7,
            ["state-shape"] = m => { var type = m.Instructions.Single(i => Op(i) == 30 && i[1] == light.StateType); type[4] = type[2]; },
            ["marker-missing"] = m => m.Instructions.RemoveAll(i => Op(i) == 57 && i[3] == light.Function),
            ["function-result"] = m => { var function = m.Instructions.Single(i => Op(i) == 54 && i[2] == light.Function); function[1] = m.Instructions.First(i => Op(i) == 22)[1]; },
            ["function-signature"] = m => { var function = m.Instructions.Single(i => Op(i) == 54 && i[2] == light.Function); m.Instructions.Single(i => Op(i) == 33 && i[1] == function[4])[2] = m.Instructions.First(i => Op(i) == 22)[1]; },
        };
        foreach (var (name, mutate) in failures)
        {
            var module = new Module(original); mutate(module);
            try { graph.SetSPIRV(vertex, module.Write()); throw new Exception("Malformed light metadata was accepted: " + name); }
            catch (Exception error) when (error is ArgumentException or NotSupportedException) { }
            if (changed != 0 || !graph.GetSPIRV().SequenceEqual(original) || !graph.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex))
                throw new Exception("Rejected light metadata replaced a retained program: " + name);
        }
        var stripped = new Module(original); stripped.Instructions.RemoveAll(i => Op(i) == 5 && (i[1] == light.Function || i[1] == light.State));
        using var independent = Shader.CreateFromSPIRV(vertex, stripped.Write());
        if ((surface ? independent.GetProgram().Surface : independent.GetProgram().AuthoredLight) is null) throw new Exception("Stage identity incorrectly depends on debug names.");
        _ = MaterialLightingProgram.Compose(independent.GetProgram(), -1, -1, false);
        Console.WriteLine($"Canvas {(surface ? "surface" : "light")} metadata: {failures.Count} atomic rejections and debug-name-independent external modules passed.");
        if (surface) SurfaceContract(graph);
    }

    internal static void Export(string folder, string output)
    {
        CustomNodes.Register();
        Directory.CreateDirectory(output); var count = 0;
        foreach (var file in Directory.GetFiles(folder, "*.e2dres").Order(StringComparer.Ordinal))
        {
            using var graph = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep);
            var program = graph.GetProgram();
            if (program.AuthoredLight is null && program.Surface is null) throw new Exception("The imported program lost its authored surface/light entry.");
            var name = System.IO.Path.GetFileNameWithoutExtension(file);
            File.WriteAllBytes(System.IO.Path.Combine(output, name + "-unlit.spv"), program.NativeCode); count++;
            for (var normal = -1; normal < 3; normal++)
                for (var specular = -1; specular < 3; specular++)
                    foreach (var masks in new[] { false, true })
                    {
                        var code = MaterialLightingProgram.Compose(program, normal, specular, masks);
                        File.WriteAllBytes(System.IO.Path.Combine(output, name + "-" + normal + "-" + specular + "-" + masks + ".spv"), code); count++;
                    }
        }
        Console.WriteLine($"Exported {count} canvas surface/light native modules for independent validation.");
    }
}
