using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class WideInteger
{
    private static RID Graphics(RenderingDevice device, string language = "glsl")
    {
        byte[] Read(string stage) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "graphics." + stage + "." + language + ".spv"));
        using var code = new RDShaderSPIRV { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") }; var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using RDPipelineSpecializationConstant unsigned = language == "hlsl" ? new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 7 } : new RDPipelineSpecializationConstant<ulong> { ConstantID = 0, Value = (1UL << 40) + 7 };
        using RDPipelineSpecializationConstant signed = language == "hlsl" ? new RDPipelineSpecializationConstant<int> { ConstantID = 1, Value = 19 } : new RDPipelineSpecializationConstant<long> { ConstantID = 1, Value = -(1L << 42) + 19 };
        var ordinary = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var custom = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [unsigned, signed]); unsigned.Dispose(); signed.Dispose();
        ulong[] values = [(1UL << 52) + 21, (1UL << 40) + 17]; var buffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(values.AsSpan())); using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; uniform.AddID(buffer); var set = device.UniformSetCreate([uniform], shader, 0);
        ulong[] push = [1UL << 52, unchecked((ulong)-(1L << 42)), 0xffff, 42]; var snapshot = push.ToArray(); var pixels = new byte[256];
        void Draw(RID pipeline)
        { var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 32); push.AsSpan().Clear(); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan()); snapshot.CopyTo(push, 0); }
        void Verify(float red, float green)
        { var data = MemoryMarshal.Cast<byte, float>(pixels.AsSpan()); for (var i = 0; i < data.Length; i += 4) Check(data[i] == red / 64 && data[i + 1] == green / 64 && data[i + 2] == 59f / 64 && data[i + 3] == 1, "64-bit vertex/fragment arithmetic and flat wide vec4/vec2 transfer reach every native pixel."); }
        Draw(ordinary); Verify(26, 17); Draw(custom); Verify(28, 19);
        using var mismatch = new RDShaderSPIRV { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") }; var changed = mismatch.BytecodeFragment; var words = MemoryMarshal.Cast<byte, uint>(changed.AsSpan());
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16)) if ((words[i] & 65535) == 71 && (words[i] >> 16) == 4 && words[i + 2] == 30 && words[i + 3] == 2) words[i + 3] = 1;
        mismatch.BytecodeFragment = changed; Reject<ArgumentException>(() => { var invalid = device.ShaderCreateFromSPIRV(mismatch); try { device.RenderPipelineCreate(invalid, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); } finally { device.FreeRID(invalid); } });
        for (var i = 0; i < 16; i++) Draw(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Draw(custom); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared wide graphics/readback allocates zero owner bytes."); Verify(28, 19);
        Console.WriteLine("PASS " + language + ": native wide graphics, interstage 64-bit values/location footprints, specialization, push snapshots, pixels and zero owner bytes."); return image;
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = Graphics(RenderingServer.GetRenderingDevice()!); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8);
            Check(Math.Abs(pixel.R - 28f / 64) < .01f && Math.Abs(pixel.G - 19f / 64) < .01f && Math.Abs(pixel.B - 59f / 64) < .01f, "Native Int64 graphics reaches retained canvas pixels.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_INT64_OUTPUT") is { } directory) image.SavePNG(System.IO.Path.Combine(directory, "int64-canvas.png"));
            Console.WriteLine("PASS Int64 renderer-owned texture reaches actual retained canvas."); root.Tree!.Quit();
        }
        Engine.Run(root);
    }
}
