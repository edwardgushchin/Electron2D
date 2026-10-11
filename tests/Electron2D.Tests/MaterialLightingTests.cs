using Electron2D;
using static TextureArrayTests;

internal static class MaterialLightingTests
{
    private sealed class Patch(Texture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false);
    }
    internal static void Run()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_LIGHTING_EXPORT_DIR") is { } exportDirectory) Directory.CreateDirectory(exportDirectory);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_LIGHTING_COLD") == "1")
        {
            foreach (var name in new[] { "CanvasGLSL", "CanvasHLSL", "MaterialLightGLSL", "MaterialLightHLSL", "MaterialLightNew", "MaterialLightMany" })
            { using var shader = Load(name); Export(shader, name, all: name != "MaterialLightMany"); }
            Console.WriteLine("Composed modules exported without native drawing."); return;
        }
        Advanced();
        IntegerLightingTests.Run(programmable: true);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var firstShader = Load("CanvasGLSL"); using var secondShader = Load("CanvasHLSL");
        using var first = new ShaderMaterial { Shader = firstShader }; using var second = new ShaderMaterial { Shader = secondShader };
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_LIGHTING_EXPORT_DIR");
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
            foreach (var shader in new[] { firstShader, secondShader })
                for (var n = -1; n < 3; n++) for (var s = -1; s < 3; s++) foreach (var masks in new[] { false, true })
                            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, $"{(shader == firstShader ? "glsl" : "hlsl")}-{n}-{s}-{masks}.spv"), MaterialLightingProgram.Compose(shader.GetProgram(), n, s, masks));
        }
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var white = ImageTexture.CreateFromImage(image);
        var root = new Window { Size = new(256, 128), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        root.AddChild(new Patch(white) { Name = "glsl", Material = first, Position = new(4, 4) }); root.AddChild(new Patch(white) { Name = "hlsl", Material = second, Position = new(36, 4) });
        root.AddChild(new Patch(white) { Name = "reference", Position = new(68, 4) });
        var light = new DirectionalLight { Color = Colors.Red, Energy = .25f }; root.AddChild(light);
        var frames = 0; long before = 0, allocated = 0;
        root.Ready += _ =>
        {
            RenderingServer.CanvasSetModulate(root.FindWorld()!.Canvas, new(.1f, .1f, .1f)); RenderingServer.SetDefaultClearColor(Colors.Black);
            root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (frames >= 24) allocated += bytes;
                if (frames < 2 || frames == 55)
                {
                    using var pixels = root.GetTexture().GetImage()!; var reference = pixels.GetPixel(76, 12);
                    Equal(pixels.GetPixel(12, 12), reference); Equal(pixels.GetPixel(44, 12), reference);
                    Equal(reference, frames % 2 == 0 ? new(.35f, .1f, .1f) : new(.1f, .1f, .35f));
                }
                light.Color = frames % 2 == 0 ? Colors.Blue : Colors.Red;
                frames++; if (frames != 56) return; Check(allocated == 0, $"Prepared material lighting allocated {allocated} managed bytes."); root.Tree!.Quit();
            };
        };
        Check(Engine.Run(root) == 0 && frames == 56 && root.IsDisposed, "Material lighting lifecycle.");
        Console.WriteLine($"Material lighting: HLSL/GLSL versus built-in directional output, live edits, 24 warm +32 active frames, {allocated} managed bytes.");
    }
    private static void Advanced()
    {
        foreach (var name in new[] { "MaterialLightGLSL", "MaterialLightHLSL", "MaterialLightNew" })
        {
            using var shader = Load(name); using var material = new ShaderMaterial { Shader = shader };
            Check(shader.GetProgram().BufferSizes.Length == 4, "Lighting retains all four user uniform slots.");
            material.SetShaderParameter("tint", new Color(.6f, .8f, 1, .5f)); material.SetShaderParameter("threshold", .5f);
            material.SetShaderParameter("scaleB", .25f); material.SetShaderParameter("scaleC", .25f); material.SetShaderParameter("scaleD", .5f);
            Export(shader, name);
            using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.White); using var white = ImageTexture.CreateFromImage(image);
            var root = new Window { Size = new(256, 128), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
            root.AddChild(new Patch(white) { Material = material, Position = new(4, 4) });
            var light = new PointLight { Texture = white, Position = new(12, 12), TextureScale = 8, Energy = .25f, Color = Colors.Red }; root.AddChild(light);
            using var contour = new OccluderPolygon { Closed = false, Polygon = [new(14, 0), new(14, 24)] };
            root.AddChild(new LightOccluder { Occluder = contour }); var frame = 0;
            root.Ready += _ =>
            {
                RenderingServer.CanvasSetModulate(root.FindWorld()!.Canvas, new(.1f, .1f, .1f)); RenderingServer.SetDefaultClearColor(Colors.Black);
                RenderingServer.FramePostDraw += () =>
                {
                    using var pixels = root.GetTexture().GetImage()!;
                    Equal(pixels.GetPixel(7, 7), frame == 0 ? Colors.Black : new(.105f, .04f, .05f));
                    var expected = frame >= 2 ? new Color(.03f, .04f, .05f) : new Color(.105f, .04f, .05f);
                    Equal(pixels.GetPixel(17, 7), expected);
                    Equal(pixels.GetPixel(17, 17), frame >= 2 ? new(.015f, .02f, .025f) : new(.0525f, .02f, .025f));
                    if (frame == 0) material.SetShaderParameter("threshold", 0f);
                    if (frame == 1) light.ShadowEnabled = true;
                    if (frame == 2)
                    {
                        var original = shader.GetSPIRV(); shader.SetSPIRV(original); Check(material.GetShaderParameter<Color>("tint") == new Color(.6f, .8f, 1, .5f), "Reload retains typed values.");
                    }
                    if (++frame == 4) root.Tree!.Quit();
                };
            };
            Check(Engine.Run(root) == 0 && frame == 4, "Discard, early return, alpha, point shadows and reload.");
            Export(shader, name);
        }
        using var manyShader = Load("MaterialLightMany"); using var many = new ShaderMaterial { Shader = manyShader };
        using var pixelsSource = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); pixelsSource.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(pixelsSource);
        for (var index = 0; index < 16; index++) many.SetShaderParameter("value" + index, texture);
        var window = new Window { Size = new(256, 128) }; window.AddChild(new Patch(texture) { Material = many }); window.AddChild(new DirectionalLight { Energy = .25f, Color = Colors.Red });
        window.Ready += _ => RenderingServer.FramePostDraw += () => { using var image = window.GetTexture().GetImage()!; Equal(image.GetPixel(8, 8), new(1, 1, 1)); window.Tree!.Quit(); };
        Check(Engine.Run(window) == 0, "Sixteen user textures retain directional lighting without extra samplers.");
        var rejected = new Window { Size = new(256, 128) }; rejected.AddChild(new Patch(texture) { Material = many }); rejected.AddChild(new PointLight { Texture = texture, TextureScale = 32 });
        Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed && RenderingServer.Service is null, "Combined sampler capacity rejects before submission and tears down.");
        Export(manyShader, "many", all: false);
        using var mesh = new ArrayMesh(); mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, MeshTests.Quad(Colors.White));
        using var instances = new MultiMesh { Mesh = mesh, UseColors = true, InstanceCount = 2 }; instances.SetInstanceTransform2D(0, new(0, new(8, 8))); instances.SetInstanceTransform2D(1, new(0, new(40, 8))); instances.SetInstanceColor(0, Colors.Red); instances.SetInstanceColor(1, Colors.Blue);
        using var instanceShader = Load("CanvasGLSL"); using var instanceMaterial = new ShaderMaterial { Shader = instanceShader };
        var host = new Window { Size = new(256, 128) }; host.AddChild(new MultiMeshInstance { MultiMesh = instances, Material = instanceMaterial, Texture = texture }); host.AddChild(new DirectionalLight { Energy = .25f });
        host.Ready += _ =>
        {
            RenderingServer.CanvasSetModulate(host.FindWorld()!.Canvas, new(.1f, .1f, .1f));
            RenderingServer.FramePostDraw += () => { using var image = host.GetTexture().GetImage()!; Equal(image.GetPixel(12, 12), new(.35f, 0, 0)); Equal(image.GetPixel(44, 12), new(0, 0, .35f)); host.Tree!.Quit(); };
        };
        Check(Engine.Run(host) == 0, "Instanced lighting uses original instance color and transform.");
    }
    private static void Export(Shader shader, string name, bool all = true)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_LIGHTING_EXPORT_DIR"); if (directory is null) return;
        for (var n = -1; n < (all ? 3 : 0); n++) for (var s = -1; s < (all ? 3 : 0); s++) foreach (var masks in all ? new[] { false, true } : new[] { false })
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, $"{name}-{n}-{s}-{masks}.spv"), MaterialLightingProgram.Compose(shader.GetProgram(), n, s, masks));
    }
    private static void Equal(Color actual, Color expected) => Check(Math.Abs(actual.R - expected.R) < .01f && Math.Abs(actual.G - expected.G) < .01f && Math.Abs(actual.B - expected.B) < .01f, $"Material light pixel {actual} != {expected}.");
}
