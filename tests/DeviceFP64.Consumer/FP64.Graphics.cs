using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class FP64
{
    private static RID Graphics(RenderingDevice device, string language)
    {
        byte[] Read(string stage) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "graphics." + stage + "." + language + ".spv"));
        using var code = new RDShaderSPIRV { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") };
        var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling };
        using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using RDPipelineSpecializationConstant factor = language == "hlsl" ? new RDPipelineSpecializationConstant<float> { ConstantID = 0, Value = 3 } : new RDPipelineSpecializationConstant<double> { ConstantID = 0, Value = 3 + 1 / Scale };
        var ordinary = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var custom = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]);
        factor.Dispose();
        double[] values = [1 + 1 / Scale, 1 + 2 / Scale]; var buffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(values.AsSpan()));
        using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; uniform.AddID(buffer); var set = device.UniformSetCreate([uniform], shader, 0);
        double[] push = [.5, .25]; var pixels = new byte[4 * 4 * 16];
        void Draw(RID pipeline)
        {
            var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0);
            device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 16); push[0] = 999; push[1] = 999;
            device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan()); push[0] = .5; push[1] = .25;
        }
        void Verify(double multiplier)
        {
            var data = MemoryMarshal.Cast<byte, float>(pixels.AsSpan());
            for (var i = 0; i < data.Length; i += 4) Check(data[i] == (float)(1.5 * multiplier / 16) && data[i + 1] == (float)(2.25 * multiplier / 16) && data[i + 2] == (float)(multiplier / 8) && data[i + 3] == 1, language + " FP64 vertex/fragment precision and push/specialization snapshots reach every native pixel.");
        }
        Draw(ordinary); Verify(2); Draw(custom); Verify(language == "hlsl" ? 3 : 3 + 1 / Scale);
        for (var i = 0; i < 16; i++) Draw(custom);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Draw(custom);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared FP64 graphics/readback allocates zero owner-thread bytes."); Verify(language == "hlsl" ? 3 : 3 + 1 / Scale);
        Console.WriteLine("PASS " + language + ": FP64 vertex/fragment arithmetic, raw uniform/push, typed specialization, packaged bytecode and native pixels; zero owner bytes.");
        return image;
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = Graphics(RenderingServer.GetRenderingDevice()!, "glsl"); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured;
            using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8);
            Check(Math.Abs(pixel.R - .28125f) < .01f && Math.Abs(pixel.G - .421875f) < .01f && Math.Abs(pixel.B - .375f) < .01f, "FP64-produced native texture reaches retained canvas: " + pixel);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_FP64_OUTPUT") is { } directory) image.SavePNG(System.IO.Path.Combine(directory, "fp64-canvas.png"));
            Console.WriteLine("PASS FP64 renderer-owned texture reaches actual retained canvas pixels."); root.Tree!.Quit();
        }
        Engine.Run(root);
    }
}
