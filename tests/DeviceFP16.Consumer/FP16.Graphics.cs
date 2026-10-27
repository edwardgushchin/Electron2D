using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class NarrowFloat
{
    private static RID Graphics(RenderingDevice device, string language = "glsl")
    {
        byte[] Read(string stage) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "graphics." + stage + "." + language + ".spv"));
        using var code = new RDShaderSPIRV { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") }; var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using RDPipelineSpecializationConstant factor = language == "hlsl" ? new RDPipelineSpecializationConstant<float> { ConstantID = 0, Value = .5f } : new RDPipelineSpecializationConstant<Half> { ConstantID = 0, Value = (Half).5f };
        var normal = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var custom = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]); factor.Dispose();
        var parameters = new Half[8]; parameters[0] = parameters[1] = (Half).5f; var buffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(parameters.AsSpan()));
        using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; uniform.AddID(buffer); var set = device.UniformSetCreate([uniform], shader, 0);
        Half[] push = [(Half).125f, (Half).25f]; var pixels = new float[64];
        void Draw(RID pipeline)
        {
            push[0] = (Half).125f; push[1] = (Half).25f; var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0);
            device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); push.AsSpan().Clear(); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        void Verify(float red, float green) { for (var i = 0; i < pixels.Length; i += 4) Check(pixels[i] == red && pixels[i + 1] == green && pixels[i + 2] == .125f && pixels[i + 3] == 1, "Native vertex/fragment Half arithmetic, compact UBO/push and copied specialization reach every actual pixel."); }
        Draw(normal); Verify(.25f, .25f); Draw(custom); Verify(.625f, .5f);
        for (var i = 0; i < 16; i++) Draw(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Draw(custom); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared half graphics/readback allocates zero owner bytes."); Verify(.625f, .5f);
        Console.WriteLine("PASS " + language + ": native Half graphics, 32-bit stage transfer, compact uniform/push, typed specialization, exact pixels and zero owner bytes."); return image;
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = Graphics(RenderingServer.GetRenderingDevice()!); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8);
            Check(Math.Abs(pixel.R - .625f) < .01f && Math.Abs(pixel.G - .5f) < .01f && Math.Abs(pixel.B - .125f) < .01f, "Native Half graphics reaches retained canvas pixels."); root.Tree!.Quit();
        }
        Engine.Run(root);
        Console.WriteLine("PASS retained canvas consumption of native Half graphics.");
    }
}
