using System.Runtime.InteropServices;
using System.Text;
using Electron2D;
using static Electron2D.MaterialLightingProgram;
using static TextureArrayTests;

internal static partial class SamplerArrayTests
{
    private static byte[] Rename(byte[] code)
    {
        var module = new Module(code); var name = module.Instructions.Single(i => Op(i) == 5 && Encoding.UTF8.GetString(MemoryMarshal.AsBytes(i.AsSpan(2))).TrimEnd('\0') == "colorMaps");
        var bytes = Encoding.UTF8.GetBytes("vertexMaps\0"); Array.Resize(ref bytes, (bytes.Length + 3) & ~3);
        var replacement = Instruction(5, [name[1], .. MemoryMarshal.Cast<byte, uint>(bytes).ToArray()]); module.Instructions[module.Instructions.IndexOf(name)] = replacement;
        return MemoryMarshal.AsBytes(module.Header.Concat(module.Instructions.SelectMany(i => i)).ToArray().AsSpan()).ToArray();
    }
    private sealed class GlyphPatch(Texture texture, bool lcd) : Entity
    {
        protected override void OnDraw()
        {
            if (lcd) DrawLCDTextureRectRegion(texture, new(0, 0, 40, 24), new(0, 0, 2, 2));
            else DrawMSDFTextureRectRegion(texture, new(0, 0, 40, 24), new(0, 0, 2, 2));
        }
    }
    private static void Stages(string profile)
    {
        var language = profile.Contains("hlsl", StringComparison.Ordinal) ? "HLSL" : "GLSL"; var split = profile.Contains("split", StringComparison.Ordinal); var lit = profile.Contains("lit", StringComparison.Ordinal); var instanced = profile.Contains("instanced", StringComparison.Ordinal);
        var originalVertex = Module(language, "SamplerVertex", "vert"); if (profile.Contains("modern", StringComparison.Ordinal)) originalVertex = Modern(originalVertex); var vertex = split ? Rename(originalVertex) : originalVertex;
        var fragment = profile.Contains("modern", StringComparison.Ordinal) ? Modern(Module(language)) : Module(language); using var shader = Shader.CreateFromSPIRV(vertex, fragment);
        Reject<ArgumentException>(() => shader.SetSPIRV(Length(originalVertex, 1), fragment));
        Check(shader.GetSPIRV(RenderingDevice.ShaderStage.Vertex).SequenceEqual(vertex), "Failed cross-stage shape replacement preserves the original stages.");
        Check(shader.GetShaderUniformList().Count == (split ? 3 : 2), "Shared and independent stage sampler arrays have one descriptor per name.");
        using var quarter = Solid(new(.25f, .25f, .25f, 1)); using var half = Solid(new(.5f, .5f, .5f, 1)); using var threeQuarters = Solid(new(.75f, .75f, .75f, 1));
        using var material = new ShaderMaterial { Shader = shader }; Texture?[] fragmentMaps = [quarter, threeQuarters]; Texture?[] vertexMaps = [half, quarter];
        material.SetShaderTextureParameterArray("colorMaps", fragmentMaps); if (split) material.SetShaderTextureParameterArray("vertexMaps", vertexMaps);
        using var referenceShader = Shader.CreateFromSPIRV(Module("GLSL", "SamplerReference")); using var reference = new ShaderMaterial { Shader = referenceShader };
        using var root = new Window { Size = new(96, 32), UseHDR2D = profile.Contains("hdr", StringComparison.Ordinal), MSAA2D = profile.Contains("msaa", StringComparison.Ordinal) ? Viewport.MSAA.Samples4X : Viewport.MSAA.Disabled };
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new() { Vertices = [new(0, 0), new(40, 0), new(40, 24), new(0, 24)], Indices = [0, 1, 2, 0, 2, 3] });
        using var instances = new MultiMesh { Mesh = mesh, InstanceCount = 1 }; instances.SetInstanceTransform2D(0, Transform.Identity);
        using var white = Solid(Colors.White); var glyph = profile.Contains("glyph", StringComparison.Ordinal); var lcd = profile.Contains("lcd", StringComparison.Ordinal);
        CanvasItem item = glyph ? new GlyphPatch(white, lcd) { Material = material, Position = new(0, 4) } : instanced ? new MultiMeshInstance { MultiMesh = instances, Material = material, Position = new(0, 4) } : new Patch { Material = material, Position = new(0, 4) };
        CanvasItem oracle = glyph ? new GlyphPatch(white, lcd) { Material = reference, Position = new(48, 4) } : new Patch { Material = reference, Position = new(48, 4) };
        if (profile.Contains("group", StringComparison.Ordinal))
        {
            var group = new CanvasGroup(); root.AddChild(group); group.AddChild(item); group.AddChild(oracle);
        }
        else { root.AddChild(item); root.AddChild(oracle); }
        if (lit) { root.AddChild(new CanvasModulate { Color = new(.25f, .25f, .25f, 1) }); root.AddChild(new DirectionalLight { Color = Colors.White, Energy = .25f }); }
        var first = 64f / 255; var second = 191f / 255; var factors = lcd ? new[] { first, second } : split ? new[] { first * (128f / 255), second * first } : new[] { first * first, second * second };
        var frames = 0; long before = 0, allocated = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            root.Tree!.ProcessFrameStarted += _ =>
            {
                before = GC.GetAllocatedBytesForCurrentThread(); var selected = frames % 2;
                material.SetShaderParameter("selected", selected); material.SetShaderTextureParameterArray("colorMaps", fragmentMaps); if (split) material.SetShaderTextureParameterArray("vertexMaps", vertexMaps);
                reference.SetShaderParameter("tint", new Color(factors[selected], factors[selected], factors[selected], 1));
            };
            RenderingServer.FramePostDraw += () =>
            {
                var delta = GC.GetAllocatedBytesForCurrentThread() - before; if (frames >= 24) allocated += delta;
                if (frames < 6 || frames == 87)
                {
                    using var image = root.GetTexture().GetImage()!; var y = 16 * image.Height / root.Size.Y;
                    var actual = image.GetPixel(16 * image.Width / root.Size.X, y); var expected = image.GetPixel(64 * image.Width / root.Size.X, y);
                    Check(Math.Abs(actual.R - expected.R) < .02 && Math.Abs(actual.G - expected.G) < .02 && Math.Abs(actual.B - expected.B) < .02, $"{profile} shared-stage sampler pixel {actual} vs scalar uniform {expected}.");
                }
                if (frames == 3) shader.SetSPIRV(vertex, fragment);
                if (++frames != 88) return;
                Check(allocated == 0, $"Prepared paired sampler frames allocated {allocated} managed bytes."); root.Tree!.Quit();
            };
        };
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); Engine.Run(root);
        Check(root.IsDisposed && frames == 88 && material.GetShaderTextureParameterArray("colorMaps").SequenceEqual(fragmentMaps), "Paired renderer shutdown and compatible reload retain each material element.");
        Console.WriteLine($"Sampler stages {profile}: shared/split descriptor mapping, original-byte rollback, reload and native pixel oracle; 64 active={allocated} managed bytes.");
    }
}
