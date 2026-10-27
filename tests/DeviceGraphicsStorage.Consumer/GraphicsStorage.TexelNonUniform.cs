using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void TexelNonUniform(RenderingDevice device, string language)
    {
        var shader = Shader(device, "texel-nonuniform", language, false); using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var first = device.TextureBufferCreate(2, DataFormat.R32UINT); var second = device.TextureBufferCreate(2, DataFormat.R32UINT); var set = TexelSet(device, shader, 3, 7, UniformType.ImageBuffer, first, second); var a = new uint[2]; var b = new uint[2]; var pixels = new byte[64];
        void Draw()
        {
            device.BufferClear(first, 0, 8); device.BufferClear(second, 0, 8); var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 3); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.BufferGetData(first, MemoryMarshal.AsBytes(a.AsSpan())); device.BufferGetData(second, MemoryMarshal.AsBytes(b.AsSpan())); device.TextureGetData(image, 0, pixels.AsSpan());
        }
        Draw(); Check(a[0] == 2 && b[0] == 1 && a[1] == 8 && b[1] == 8, "Each invocation selects its actual texel descriptor, independently in vertex and fragment stages."); for (var i = 0; i < 16; i++) Check(pixels[i * 4] == (i % 2 == 0 ? 255 : 0) && pixels[i * 4 + 1] == (i % 2 == 0 ? 0 : 255), "Nonuniform branches preserve actual fragment choices.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared nonuniform graphics texel choices allocate zero owner bytes.");
        foreach (var resource in new[] { set, pipeline, shader, first, second, target, image }) device.FreeRID(resource); Console.WriteLine("PASS " + language + ": per-invocation nonuniform texel choices, stage atomics, exact pixels and zero owner bytes.");
    }
}
