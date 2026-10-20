using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceSamplerTests
{
    private static (RID Target, Action<bool> Draw) Producer(RenderingDevice device, string language)
    {
        var first = Image(device, 2, 2, [0, 0, 0, 255, 255, 0, 0, 255, 0, 0, 0, 255, 255, 0, 0, 255]); var second = Image(device, 1, 1, [0, 255, 0, 255]);
        using var point = new RDSamplerState(); using var linear = new RDSamplerState { MinFilter = SamplerFilter.Linear, MagFilter = SamplerFilter.Linear }; var a = device.SamplerCreate(point); var b = device.SamplerCreate(linear);
        using var module = RenderingDeviceGraphicsTests.Load("DeviceSamplers" + language); Export(module, language); var shader = device.ShaderCreateFromSPIRV(module);
        using var color = new RDTextureFormat { Width = 8, Height = 8, ArrayLayers = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var target = device.TextureCreate(color, view); var framebuffer = device.FramebufferCreate([target], viewCount: 2);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        RID Combined(RID image) { using var descriptor = new RDUniform { Binding = 7, UniformType = UniformType.SamplerWithTexture }; descriptor.AddID(a); descriptor.AddID(image); return device.UniformSetCreate([descriptor], shader, 3); }
        var source = Combined(first); var alternate = Combined(second); var filter = Set(device, shader, 5, 13, UniformType.Sampler, b);
        return (target, Draw);
        void Draw(bool green)
        {
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, green ? alternate : source, 3); device.DrawListBindUniformSet(list, filter, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
    }
    private static void Graphics(RenderingDevice device, string language)
    {
        var producer = Producer(device, language); var bytes = new byte[8 * 8 * 4];
        void Render(bool green)
        {
            producer.Draw(green);
            for (uint layer = 0; layer < 2; layer++)
            {
                device.TextureGetData(producer.Target, layer, bytes.AsSpan());
                for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++)
                    {
                        var at = (y * 8 + x) * 4; var red = green ? 0 : (int)MathF.Round((.5f + .5f * Math.Clamp((x + .5f) / 4 - .5f, 0, 1)) * 255);
                        if (Math.Abs(bytes[at] - red) > 1 || bytes[at + 1] != (green ? 255 : 0) || bytes[at + 2] != 0 || bytes[at + 3] != 255) throw new InvalidOperationException($"{language}: layer {layer}, pixel {x},{y}: {bytes[at]},{bytes[at + 1]}.");
                    }
            }
        }
        Render(false); Render(true); for (var i = 0; i < 8; i++) Render(i % 2 == 0); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Render(i % 2 == 0); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared mixed-stage, changing linked sets and layered readback use zero managed owner bytes.");
        Console.WriteLine(language + ": mixed combined/independent stages, derivatives, changing linked images, two layers and 32 zero-allocation cycles passed.");
    }
    private sealed class Patch : Entity { protected override void OnDraw() => DrawRect(new(0, 0, 16, 16), Colors.White); }
    internal static void Canvas(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var array = new TextureArrayRD(); using var glsl = Load("ArrayGLSL"); using var hlsl = Load("ArrayHLSL"); using var white = Electron2D.Image.CreateEmpty(1, 1, false, Electron2D.Image.Format.Rgba8); white.Fill(Colors.White); using var ordinary = ImageTexture.CreateFromImage(white); glsl.SetDefaultTextureParameter("detailMap", ordinary); hlsl.SetDefaultTextureParameter("detailMap", ordinary);
        var root = new Window { Size = new(64, 64) }; var materials = new List<ShaderMaterial>(); RenderingDevice device = null!; Action<bool> draw = null!; var frames = 0; long before = 0;
        try
        {
            root.Ready += _ =>
            {
                device = RenderingServer.GetRenderingDevice()!; var producer = Producer(device, language); array.TextureRDRID = producer.Target; draw = producer.Draw;
                for (var i = 0; i < 2; i++) { var material = new ShaderMaterial { Shader = i == 0 ? glsl : hlsl }; materials.Add(material); material.SetShaderLayeredParameter("layers", array); material.SetShaderParameter("selectedLayer", (float)i); root.AddChild(new Patch { Position = new(i * 16, 0), Material = material }); }
                RenderingServer.FramePreDraw += Generate; RenderingServer.FramePostDraw += Captured;
            };
            Engine.Run(root); Check(root.IsDisposed && device.IsDisposed && RenderingServer.GetRenderingDevice() is null, "Renderer shutdown releases linked programs and their borrowed images.");
        }
        finally { foreach (var material in materials) material.Dispose(); }
        Console.WriteLine(language + ": linked generation reached GLSL/HLSL retained canvas, 32 changing full owner frames=0 managed bytes.");
        void Generate() { if (frames >= 8) before = GC.GetAllocatedBytesForCurrentThread(); draw(frames % 2 != 0); }
        void Captured()
        {
            if (frames >= 8) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Changing linked device generation and ordinary canvas submission allocate zero managed owner bytes.");
            if (frames is 0 or 39)
            {
                using var pixels = root.GetTexture().GetImage()!; for (var layer = 0; layer < 2; layer++) { var pixel = pixels.GetPixel(layer * 16 + 1, 8); var green = frames % 2 != 0; Check(Math.Abs(pixel.R - (green ? 0 : .5f)) < .015f && Math.Abs(pixel.G - (green ? 1 : 0)) < .015f && pixel.B < .01f, "Both canvas languages sample the current linked device layer."); }
            }
            if (++frames != 40) return; RenderingServer.FramePreDraw -= Generate; RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
    }
    private static void Export(RDShaderSPIRV module, string name)
    {
        var folder = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_SAMPLER_OUTPUT"); if (folder is null) return; Directory.CreateDirectory(folder); var method = typeof(RenderingDevice).GetMethod("Remap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        foreach (var stage in new[] { ShaderStage.Compute, ShaderStage.Vertex, ShaderStage.Fragment }) { var source = module.GetStageBytecode(stage); if (source.Length == 0) continue; var result = method.Invoke(null, [source, stage])!; File.WriteAllBytes(System.IO.Path.Combine(folder, name + "." + stage + ".spv"), (byte[])result.GetType().GetField("Item1")!.GetValue(result)!); }
    }
}
