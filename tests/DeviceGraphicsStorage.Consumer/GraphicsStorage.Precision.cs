using System.Buffers.Binary;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Precision(RenderingDevice device)
    {
        var shader = Shader(device, "precision", "glsl", false); var buffer = device.StorageBufferCreate(24); var set = Set(device, shader, buffer);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var result = device.BufferGetData(buffer);
        Check(result[0] == 0xa5 && BinaryPrimitives.ReadUInt16LittleEndian(result.AsSpan(4)) == 0xbeef && BinaryPrimitives.ReadInt64LittleEndian(result.AsSpan(8)) == -1234567 && BitConverter.ToDouble(result.AsSpan(16)) == 1.0001220703125, "Exact byte/Int16/Int64/FP64 vertex and fragment writes preserve original mixed buffer layout.");
        Console.WriteLine("PASS exact mixed 8/16/64-bit integer and FP64 graphics storage writes.");
    }
}
